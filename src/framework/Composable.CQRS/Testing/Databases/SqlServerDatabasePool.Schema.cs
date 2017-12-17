﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using Composable.Contracts;
using Composable.System;
using Composable.System.Data.SqlClient;
using Composable.System.Linq;
using Composable.System.Threading;

namespace Composable.Testing.Databases
{
    static class DatabaseExtensions
    {
        internal static string Name(this SqlServerDatabasePool.Database @this) => $"{SqlServerDatabasePool.PoolDatabaseNamePrefix}{@this.Id:0000}";
        internal static string ConnectionString(this SqlServerDatabasePool.Database @this, SqlServerDatabasePool pool) => pool.ConnectionStringForDbNamed(@this.Name());
    }

    sealed partial class SqlServerDatabasePool
    {
        [Serializable]
        internal class Database : IBinarySerializeMySelf
        {
            internal int Id { get; private set; }
            internal bool IsReserved { get; private set; }
            internal bool IsClean { get; private set; } = true;
            public DateTime ReservationDate { get; private set; } = DateTime.MaxValue;
            internal string ReservationName { get; private set; } = string.Empty;
            internal Guid ReservedByPoolId { get; private set; } = Guid.Empty;

            internal Database() { }
            internal Database(int id) => Id = id;
            internal Database(string name) : this(IdFromName(name)) { }

            internal bool ReservationIsExpired => ReservationDate < DateTime.UtcNow - 10.Minutes();
            internal bool ShouldBeReleased => IsReserved && ReservationIsExpired;
            internal bool ShouldBeCleaned => IsFree && IsDirty;
            internal bool EligibleForGarbageCollection => ShouldBeReleased || ShouldBeCleaned;
            internal bool IsFree => !IsReserved;
            internal bool IsDirty => !IsClean;
            internal bool FreeAndClean => IsFree && IsClean;

            static int IdFromName(string name)
            {
                var nameIndex = name.Replace(PoolDatabaseNamePrefix, "");
                return int.Parse(nameIndex);
            }

            internal Database Release()
            {
                OldContract.Assert.That(IsReserved, "IsReserved");
                IsReserved = false;
                IsClean = false;
                ReservationDate = DateTime.UtcNow;//We reuse this value to hold the release time.
                ReservationName = string.Empty;
                ReservedByPoolId = Guid.Empty;
                return this;
            }

            internal Database Clean()
            {
                OldContract.Assert.That(!IsClean, "!IsClean");
                IsClean = true;
                return this;
            }

            internal Database Reserve(string reservationName, Guid poolId)
            {
                OldContract.Assert.That(!IsReserved, "!IsReserved");
                OldContract.Assert.That(poolId != Guid.Empty, "poolId != Guid.Empty");

                IsReserved = true;
                ReservationName = reservationName;
                ReservationDate = DateTime.UtcNow;
                ReservedByPoolId = poolId;
                return this;
            }

            public void Deserialize(BinaryReader reader)
            {
                Id = reader.ReadInt32();
                IsReserved = reader.ReadBoolean();
                ReservationDate = DateTime.FromBinary(reader.ReadInt64());
                ReservationName = reader.ReadString();
                ReservedByPoolId = new Guid(reader.ReadBytes(16));
                IsClean = reader.ReadBoolean();
            }

            public void Serialize(BinaryWriter writer)
            {
                writer.Write(Id);
                writer.Write(IsReserved);
                writer.Write(ReservationDate.ToBinary());
                writer.Write(ReservationName);
                writer.Write(ReservedByPoolId.ToByteArray());
                writer.Write(IsClean);
            }
        }

        void CreateDatabase(string databaseName)
        {
            var createDatabaseCommand = $@"CREATE DATABASE [{databaseName}]";
            if(!DatabaseRootFolderOverride.IsNullOrWhiteSpace())
            {
                createDatabaseCommand += $@"
ON      ( NAME = {databaseName}_data, FILENAME = '{DatabaseRootFolderOverride}\{databaseName}.mdf') 
LOG ON  ( NAME = {databaseName}_log, FILENAME = '{DatabaseRootFolderOverride}\{databaseName}.ldf');";
            }

            createDatabaseCommand += $@"
ALTER DATABASE [{databaseName}] SET RECOVERY SIMPLE;
ALTER DATABASE[{ databaseName}] SET READ_COMMITTED_SNAPSHOT ON";

            _masterConnection.ExecuteNonQuery(createDatabaseCommand);

            //SafeConsole.WriteLine($"Created: {databaseName}");
        }

        void RebootPool(SharedState machineWide)
        {
            RebootedMasterConnections.Add(_masterConnectionString);

            _log.Warning("Rebooting database pool");

            ResetInMemoryData(machineWide);

            var dbsToDrop = ListPoolDatabases();

            _log.Warning("Dropping databases");
            foreach(var db in dbsToDrop)
            {
                //Clear connection pool
                using(var connection = new SqlConnection(db.ConnectionString(this)))
                {
                    SqlConnection.ClearPool(connection);
                }

                var dropCommand = $"drop database [{db.Name()}]";
                _log.Info(dropCommand);
                _masterConnection.ExecuteNonQuery(dropCommand);
            }

            _log.Warning("Creating new databases");

            1.Through(30)
             .ForEach(_ => InsertDatabase(machineWide));
        }

        void ResetInMemoryData(SharedState machineWide)
        {
            var reservedDatabases = machineWide.DatabasesReservedBy(_poolId);
            if(reservedDatabases.Any())
            {
                foreach(var reservedDatabase in reservedDatabases)
                {
                    reservedDatabase.Release();
                }
            }
            machineWide.Reset();

            if(_transientCache.Any())
            {
                _transientCache = new List<Database>();
            }
        }

        IReadOnlyList<Database> ListPoolDatabases()
        {
            var databases = new List<string>();
            _masterConnection.UseCommand(
                action: command =>
                        {
                            command.CommandText = "select name from sysdatabases";
                            using(var reader = command.ExecuteReader())
                            {
                                while(reader.Read())
                                {
                                    var dbName = reader.GetString(i: 0);
                                    if(dbName.StartsWith(PoolDatabaseNamePrefix))
                                        databases.Add(dbName);
                                }
                            }
                        });

            return databases.Select(name => new Database(name))
                            .ToList();
        }
    }
}
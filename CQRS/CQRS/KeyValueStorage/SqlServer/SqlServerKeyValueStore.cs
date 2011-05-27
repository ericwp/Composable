#region usings

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Transactions;
using Composable.DDD;
using Composable.NewtonSoft;
using Composable.System;
using Newtonsoft.Json;

#endregion

namespace Composable.KeyValueStorage.SqlServer
{
    public class SqlServerKeyValueStore : IKeyValueStore
    {
        private readonly string _connectionString;
        private readonly SqlServerKeyValueStoreConfig _config;

        public SqlServerKeyValueStore(string connectionString, SqlServerKeyValueStoreConfig config = SqlServerKeyValueStoreConfig.Default)
        {
            _connectionString = connectionString;
            _config = config;
        }

        public IKeyValueSession OpenSession()
        {
            return new SessionDisposeWrapper(new SqlServerKeyValueSession(this, _config));
        }

        private class SqlServerKeyValueSession : IEnlistmentNotification, IKeyValueSession
        {
            private readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
                                                                       {
                                                                           ContractResolver = new IncludeMembersWithPrivateSettersResolver()
                                                                       };

            private readonly SqlServerKeyValueStore _store;
            private readonly SqlServerKeyValueStoreConfig _config;
            private readonly Dictionary<Guid, object> _idMap = new Dictionary<Guid, object>();
            private readonly HashSet<Guid> _persistentValues = new HashSet<Guid>();
            private readonly SqlConnection _connection;
            private bool TableVerifiedToExist;
            private bool _enlisted;
            private const int UniqueConstraintViolationErrorNumber = 2627;
            private int SqlBatchSize = 10;

            private static int instances;
            public SqlServerKeyValueSession(SqlServerKeyValueStore store, SqlServerKeyValueStoreConfig config)
            {
                Console.WriteLine("{0}: {1}", GetType().Name, ++instances);
                _store = store;
                _config = config;
                _connection = new SqlConnection(store._connectionString);
                _connection.Open();
                EnsureTableExists();

                if(config.HasFlag(SqlServerKeyValueStoreConfig.NoBatching))
                {
                    SqlBatchSize = 1;
                }
            }

            private void EnsureTableExists()
            {
                if(!TableVerifiedToExist)
                {
                    using (var checkForTableCommand = _connection.CreateCommand())
                    {
                        checkForTableCommand.CommandText = "select count(*) from sys.tables where name = 'Store'";
                        var exists = (int)checkForTableCommand.ExecuteScalar();
                        if(exists == 0)
                        {
                            using (var createTableCommand = _connection.CreateCommand())
                            {

                                createTableCommand.CommandText =
                                    @"
CREATE TABLE [dbo].[Store](
	[Id] [uniqueidentifier] NOT NULL,
    [ValueType] [varchar](500) NOT NULL,
	[Value] [nvarchar](max) NOT NULL,
 CONSTRAINT [PK_Store] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
) ON [PRIMARY]
";
                                createTableCommand.ExecuteNonQuery();
                            }
                        }
                        TableVerifiedToExist = true;
                    }
                }
            }

            public TValue Get<TValue>(Guid key)
            {
                EnlistInAmbientTransaction();

                object value;
                if(_idMap.TryGetValue(key, out value))
                {
                    return (TValue)value;
                }

                using(var loadCommand = _connection.CreateCommand())
                {
                    loadCommand.CommandText = "SELECT Value, ValueType FROM Store WHERE Id=@Id";
                    loadCommand.Parameters.Add(new SqlParameter("Id", key));
                    value = loadCommand.ExecuteScalar();
                    if(value == null)
                    {
                        throw new NoSuchKeyException(key, typeof(TValue));
                    }
                    value = JsonConvert.DeserializeObject((String)value, typeof(TValue));
                }
                _persistentValues.Add(key);
                _idMap.Add(key, value);
                return (TValue)value;
            }

            public void Save<TValue>(Guid key, TValue value)
            {
                EnlistInAmbientTransaction();

                object existing;
                if(_idMap.TryGetValue(key, out existing))
                {
                    throw new AttemptToSaveAlreadyPersistedValueException(key, value);
                }
                _idMap.Add(key, value);
            }

            public void Save<TEntity>(TEntity entity) where TEntity : IPersistentEntity<Guid>
            {
                Save(entity.Id, entity);
            }

            public void SaveChanges()
            {
                InsertValues(_idMap.Where(entry => !_persistentValues.Contains(entry.Key)));
                UpdateValues(_idMap.Where(entry => _persistentValues.Contains(entry.Key)));
            }

            private Guid ManagerGuid = Guid.Parse("84165A58-1EAE-49DD-9324-EEFE5D7D00DD");
            private readonly HashSet<Transaction> enlistedIn = new HashSet<Transaction>();

            private void EnlistInAmbientTransaction()
            {
                //&& !enlistedIn.Contains(Transaction.Current)
                if (Transaction.Current != null )
                {
                    Transaction.Current.EnlistVolatile(this, EnlistmentOptions.EnlistDuringPrepareRequired);
                    enlistedIn.Add(Transaction.Current);
                }
            }

            private void UpdateValues(IEnumerable<KeyValuePair<Guid, object>> values)
            {
                var handled = 0;
                var eventCount = values.Count();
                while(handled < eventCount)
                {
                    using(var command = _connection.CreateCommand())
                    {
                        command.CommandType = CommandType.Text;
                        for(var handledInBatch = 0; handledInBatch < SqlBatchSize && handled < eventCount; handledInBatch++, handled++)
                        {
                            var entry = values.ElementAt(handledInBatch);

                            command.CommandText += "UPDATE Store SET Value = @Value{0} WHERE Id = @Id{0}"
                                .FormatWith(handledInBatch);

                            command.Parameters.Add(new SqlParameter("Id" + handledInBatch, entry.Key));
                            command.Parameters.Add(new SqlParameter("Value" + handledInBatch,
                                                                    JsonConvert.SerializeObject(entry.Value, Formatting.None, JsonSettings)));
                        }
                        command.ExecuteNonQuery();
                    }
                }
            }

            private void InsertValues(IEnumerable<KeyValuePair<Guid, object>> values)
            {
                var handled = 0;
                var eventCount = values.Count();
                while(handled < eventCount)
                {
                    using(var command = _connection.CreateCommand())
                    {
                        command.CommandType = CommandType.Text;
                        KeyValuePair<Guid, object> entry = new KeyValuePair<Guid, object>();
                        for(var handledInBatch = 0; handledInBatch < SqlBatchSize && handled < eventCount; handledInBatch++, handled++)
                        {
                            entry = values.ElementAt(handledInBatch);

                            command.CommandText += "INSERT Store(Id, ValueType, Value) VALUES(@Id{0}, @ValueType{0}, @Value{0})"
                                .FormatWith(handledInBatch);

                            command.Parameters.Add(new SqlParameter("Id" + handledInBatch, entry.Key));
                            command.Parameters.Add(new SqlParameter("ValueType" + handledInBatch, entry.Value.GetType().FullName));
                            command.Parameters.Add(new SqlParameter("Value" + handledInBatch,
                                                                    JsonConvert.SerializeObject(entry.Value, Formatting.None, JsonSettings)));
                        }
                        try
                        {
                            command.ExecuteNonQuery();
                        }
                        catch(SqlException e)
                        {
                            if(e.Number == UniqueConstraintViolationErrorNumber)
                            {
                                if (SqlBatchSize == 1)
                                {
                                    throw new AttemptToSaveAlreadyPersistedValueException(entry.Key, entry.Value);
                                }
                                
                                throw new AttemptToSaveAlreadyPersistedValueException(Guid.Empty, "Batched insert cannot extract value try with SqlServerKeyValueStoreConfig.NoBatching...");
                            }
                            throw;
                        }
                    }
                }
            }

            public void Dispose()
            {
                Console.WriteLine("{0}: {1}", GetType().Name, --instances);
                _connection.Dispose();
                _idMap.Clear();
            }

            void IEnlistmentNotification.Prepare(PreparingEnlistment preparingEnlistment)
            {
                SaveChanges();
                preparingEnlistment.Prepared();
            }

            void IEnlistmentNotification.Commit(Enlistment enlistment)
            {
                _enlisted = false;
                enlistment.Done();
                HandleScheduledDispose();
            }

            void IEnlistmentNotification.Rollback(Enlistment enlistment)
            {
                _enlisted = false;
                HandleScheduledDispose();
            }

            public void InDoubt(Enlistment enlistment)
            {
                _enlisted = false;
                enlistment.Done();
                HandleScheduledDispose();
            }


            private void HandleScheduledDispose()
            {
                if(_scheduledForDisposeAfterTransactionDone)
                {
                    _scheduledForDisposeAfterTransactionDone = false;
                    Dispose();
                }
            }

            private bool _scheduledForDisposeAfterTransactionDone;

            public void DisposeIfNotEnlisted()
            {
                if(_enlisted)
                {
                    _scheduledForDisposeAfterTransactionDone = true;
                }
                else
                {
                    Dispose();
                }
            }

            public void PurgeDB()
            {
                using (var dropCommand = _connection.CreateCommand())
                {
                    dropCommand.CommandText =
                        @"IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Store]') AND type in (N'U'))
DROP TABLE [dbo].[Store]";

                    dropCommand.ExecuteNonQuery();
                    TableVerifiedToExist = false;
                }
            }
        }

        private class SessionDisposeWrapper : IKeyValueSession
        {
            private readonly SqlServerKeyValueSession _session;

            public SessionDisposeWrapper(SqlServerKeyValueSession session)
            {
                _session = session;
            }

            public void Dispose()
            {
                _session.DisposeIfNotEnlisted();
            }

            public TValue Get<TValue>(Guid key)
            {
                return _session.Get<TValue>(key);
            }

            public void Save<TValue>(Guid key, TValue value)
            {
                _session.Save(key, value);
            }

            public void Save<TEntity>(TEntity entity) where TEntity : IPersistentEntity<Guid>
            {
                _session.Save(entity);
            }

            public void SaveChanges()
            {
                _session.SaveChanges();
            }
        }

        public static void ResetDB(string connectionString)
        {
            var me = new SqlServerKeyValueStore(connectionString);
            using (var session = new SqlServerKeyValueSession(me, SqlServerKeyValueStoreConfig.Default))
            {
                session.PurgeDB();
            }
        }
    }

    [Flags]
    public enum SqlServerKeyValueStoreConfig
    {
        Default = 0x0,
        NoBatching = 0x2
    }
}
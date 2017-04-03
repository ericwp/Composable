using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Composable.Logging;
using Composable.System;
using Composable.System.Data.SqlClient;
using Composable.System.Linq;

namespace Composable.Testing
{
    sealed partial class SqlServerDatabasePool : StrictlyManagedResourceBase<SqlServerDatabasePool>
    {
        readonly string _masterConnectionString;
        readonly SqlServerConnectionUtilities _masterConnection;
        readonly SqlServerConnectionUtilities _managerConnection;

        static readonly ILogger Log = Logger.For<SqlServerDatabasePool>();

        static readonly string ManagerDbName = $"{nameof(SqlServerDatabasePool)}";

        public SqlServerDatabasePool(string masterConnectionString)
        {
            _masterConnectionString = masterConnectionString;
            _masterConnection = new SqlServerConnectionUtilities(_masterConnectionString);


            var sqlConnectionStringBuilder = new SqlConnectionStringBuilder(_masterConnectionString) {InitialCatalog = ManagerDbName};
            _managerConnection = new SqlServerConnectionUtilities(sqlConnectionStringBuilder.ConnectionString);

            EnsureManagerDbExists();
        }

        readonly Dictionary<string, Database> _reservedDatabases = new Dictionary<string, Database>();
        bool _disposed;

        public string ConnectionStringFor(string requestedDbName)
        {
            if(_disposed)
                throw new InvalidOperationException(message: "Attempt to use disposed object");

            Database database;
            if(_reservedDatabases.TryGetValue(requestedDbName, out database))
                return database.ConnectionString;

            var newDatabase = false;
            RunInIsolatedTransaction(action: () =>
                                     {
                                         if(TryReserveDatabase(out database))
                                         {
                                             _reservedDatabases.Add(requestedDbName, database);
                                         } else
                                         {
                                             ReleaseOldLocks();
                                             if(TryReserveDatabase(out database))
                                             {
                                                 _reservedDatabases.Add(requestedDbName, database);
                                             } else
                                             {
                                                 newDatabase = true;
                                                 database = InsertDatabase();
                                                 using (new TransactionScope(TransactionScopeOption.Suppress))
                                                 {
                                                     CreateDatabase(database.Name);
                                                 }

                                                 _reservedDatabases.Add(requestedDbName, database);
                                             }
                                         }
                                     });

            if(!newDatabase)
                CleanDatabase(database);
            return database.ConnectionString;
        }

        string ConnectionStringForDbNamed(string dbName)
        {
            var sqlConnectionStringBuilder = new SqlConnectionStringBuilder(_masterConnectionString) {InitialCatalog = dbName};
            return sqlConnectionStringBuilder.ConnectionString;
        }

        bool TryReserveDatabase(out Database database)
        {
            var command = $@"declare @reservedId integer

SET @reservedId = (select top 1 {ManagerTableSchema.Id} from {ManagerTableSchema.TableName} {LockingHint}
WHERE {ManagerTableSchema.IsFree} = 1
order by {ManagerTableSchema.ReservationDate} asc)

if( @reservedId is not null)
	update {ManagerTableSchema.TableName} set {ManagerTableSchema.IsFree} = 0 where Id = @reservedId

select @reservedId";

            var idObject = _managerConnection.ExecuteScalar(command);
            if(!(idObject is DBNull))
            {
                database = new Database(pool:this, id:(int)idObject, isFree:false);
                return true;
            }

            database = null;
            return false;
        }

        void CleanDatabase(Database db)
        {
            new SqlServerConnectionUtilities(ConnectionStringForDbNamed(db.Name))
                .UseConnection(action: connection => connection.DropAllObjects());
        }

        Database InsertDatabase()
        {
            var value = _managerConnection.ExecuteScalar(
                $@"
                set nocount on
                insert {ManagerTableSchema.TableName} ({ManagerTableSchema.IsFree}, {ManagerTableSchema.ReservationDate},  {ManagerTableSchema.ReservationCallStack}) 
                                                   values(                0      ,                     getdate()       ,                     '{Environment.StackTrace}')
                select @@IDENTITY");
            var id = (int)(decimal)value;
            var database = new Database(pool: this, id: id, isFree: false);
            return database;
        }


        void ReleaseDatabases(IReadOnlyList<Database> database)
        {
            database.ForEach(action: db => _reservedDatabases.Remove(db.Name));

            var idList = database.Select(selector: db => "'" + db.Id + "'").Join(separator: ",");

            Task.Run(
                action: () => RunInIsolatedTransaction(
                    action: () => _managerConnection.ExecuteNonQuery(
                        $"update {ManagerTableSchema.TableName} set {ManagerTableSchema.IsFree} = 1  where {ManagerTableSchema.Id} in ({idList})")));
        }

        static readonly string LockingHint = "With(TABLOCKX)";

        IEnumerable<Database> GetDatabases()
        {
            return _managerConnection.UseCommand(
                action: command =>
                {
                    var names = new List<Database>();
                    command.CommandText =
                        $"select {ManagerTableSchema.Id}, {ManagerTableSchema.IsFree}, {ManagerTableSchema.ReservationDate} from {ManagerTableSchema.TableName} {LockingHint}";
                    using(var reader = command.ExecuteReader())
                    {
                        while(reader.Read())
                            names.Add(
                                new Database(
                                    this,
                                    reader.GetInt32(0),
                                    reader.GetBoolean(1)));
                    }
                    return names;
                });
        }

        void ReleaseOldLocks()
        {
                    RunInIsolatedTransaction(action: () =>
                                             {
                                                 var count = _managerConnection.ExecuteNonQuery(
                                                     $"update {ManagerTableSchema.TableName} {LockingHint} set {ManagerTableSchema.IsFree} = 1 where {ManagerTableSchema.ReservationDate} < dateadd(minute, -10, getdate()) and {ManagerTableSchema.IsFree} = 0");
                                                 if(count > 0)
                                                 {
                                                     //SafeConsole.WriteLine($"Released {count} garbage reservations.");
                                                 }
                                             });
        }

        protected override void InternalDispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                ReleaseDatabases(_reservedDatabases.Values.ToList());
            }
        }

        ~SqlServerDatabasePool()
        {
            InternalDispose();
        }
    }
}

using System;
using System.Data.SqlClient;
using System.Transactions;
using Composable.Logging.Log4Net;
using Composable.System.Data.SqlClient;

namespace Composable.Persistence.EventStore.MicrosoftSQLServer
{
    class SqlServerEventStoreConnectionManager
    {
        readonly ISqlConnectionProvider _connectionManager;
        public SqlServerEventStoreConnectionManager(ISqlConnectionProvider connectionString) => _connectionManager = connectionString;

        void UseConnection(Action<SqlConnection> action, bool suppressTransactionWarning = false)
        {
            using(var connection = OpenConnection(suppressTransactionWarning))
            {
                action(connection);
            }
        }

        public void UseCommand(Action<SqlCommand> action, bool suppressTransactionWarning = false)
        {
            UseConnection(connection =>
                          {
                              using(var command = connection.CreateCommand())
                              {
                                  action(command);
                              }
                          }, suppressTransactionWarning);
        }

        public SqlConnection OpenConnection(bool suppressTransactionWarning = false)
        {
            if (!suppressTransactionWarning && Transaction.Current == null)
            {
                this.Log().Warn($@"No ambient transaction. This is dangerous:
AT: 

{Environment.StackTrace}");
            }
            return _connectionManager.OpenConnection();
        }
    }
}
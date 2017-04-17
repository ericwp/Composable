using System;
using System.Data.SqlClient;
using System.Transactions;
using Composable.Logging.Log4Net;

namespace Composable.Persistence.EventStore.MicrosoftSQLServer
{
    class SqlServerEventStoreConnectionManager
    {
        Lazy<string> _connectionString;
        string ConnectionString => _connectionString.Value;
        public SqlServerEventStoreConnectionManager(Lazy<string> connectionString) => _connectionString = connectionString;

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
                          });
        }

        public SqlConnection OpenConnection(bool suppressTransactionWarning = false)
        {
            var connection = new SqlConnection(ConnectionString);
            connection.Open();
            if (!suppressTransactionWarning && Transaction.Current == null)
            {
                this.Log().Warn($@"No ambient transaction. This is dangerous:
AT: 

{Environment.StackTrace}");
            }
            return connection;
        }
    }
}
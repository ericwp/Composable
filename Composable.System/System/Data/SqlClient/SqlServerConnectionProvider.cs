using System;
using System.Data.SqlClient;
using System.Transactions;

namespace Composable.System.Data.SqlClient
{
    class SqlServerConnectionProvider : ISqlConnectionProvider
    {
        public string ConnectionString { get; }

        public SqlServerConnectionProvider(string connectionString) => ConnectionString = connectionString;

        public SqlConnection OpenConnection()
        {
            var transactionInformationDistributedIdentifierBefore = Transaction.Current?.TransactionInformation.DistributedIdentifier;
            var connection = new SqlConnection(ConnectionString);
            connection.Open();
            if(transactionInformationDistributedIdentifierBefore != null && transactionInformationDistributedIdentifierBefore.Value == Guid.Empty)
            {
                if (Transaction.Current.TransactionInformation.DistributedIdentifier != Guid.Empty)
                {
                    throw new Exception("Opening connection escalated transaction to distributed. For now this is disallowed");
                }
            }
            return connection;
        }
    }

    static class SqlConnectionProviderExtensions
    {
        public static int ExecuteNonQuery(this ISqlConnectionProvider @this, string commandText)
        {
            return @this.UseCommand(
                command =>
                {
                    command.CommandText = commandText;
                    return command.ExecuteNonQuery();
                });
        }

        public static object ExecuteScalar(this ISqlConnectionProvider @this, string commandText)
        {
            return @this.UseCommand(
                command =>
                {
                    command.CommandText = commandText;
                    return command.ExecuteScalar();
                });
        }

        public static void UseConnection(this ISqlConnectionProvider @this, Action<SqlConnection> action)
        {
            using (var connection = @this.OpenConnection())
            {
                action(connection);
            }
        }

        static TResult UseConnection<TResult>(this ISqlConnectionProvider @this, Func<SqlConnection, TResult> action)
        {
            using (var connection = @this.OpenConnection())
            {
                return action(connection);
            }
        }

        public static void UseCommand(this ISqlConnectionProvider @this, Action<SqlCommand> action)
        {
            @this.UseConnection(connection =>
                          {
                              using (var command = connection.CreateCommand())
                              {
                                  action(command);
                              }
                          });
        }

        static TResult UseCommand<TResult>(this ISqlConnectionProvider @this, Func<SqlCommand, TResult> action)
        {
            return @this.UseConnection(connection =>
                                 {
                                     using (var command = connection.CreateCommand())
                                     {
                                         return action(command);
                                     }
                                 });
        }
    }

    interface ISqlConnectionProvider
    {
        SqlConnection OpenConnection();
        string ConnectionString { get; }
    }
}
#region usings

using System.Transactions;
using Newtonsoft.Json;

#endregion

namespace Composable.KeyValueStorage.SqlServer
{
    public class SqlServerKeyValueStore : IKeyValueStore
    {
        public string ConnectionString { get; private set; }
        public SqlServerKeyValueStoreConfig Config { get; private set; }

        public SqlServerKeyValueStore(string connectionString, SqlServerKeyValueStoreConfig config = null)
        {
            if(config == null)
            {
                config = SqlServerKeyValueStoreConfig.Default;
            }
            ConnectionString = connectionString;
            Config = config;
        }

        public IKeyValueStoreSession OpenSession()
        {
            return new SqlServerKeyValueSession(this);
        }

        public static void ResetDB(string connectionString)
        {
            var me = new SqlServerKeyValueStore(connectionString);
            using (var session = new SqlServerKeyValueSession(me))
            {
                session.PurgeDB();
            }
        }
    }

    public class SqlServerKeyValueStoreConfig
    {
        public static readonly SqlServerKeyValueStoreConfig Default = new SqlServerKeyValueStoreConfig
                                                                          {};

        public bool Batching = true;
        public Formatting JSonFormatting = Formatting.None;
    }
}
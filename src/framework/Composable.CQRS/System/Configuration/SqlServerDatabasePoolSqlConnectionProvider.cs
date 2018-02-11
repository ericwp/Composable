﻿using System;
using Composable.System.Data.SqlClient;
using Composable.Testing.Databases;

namespace Composable.System.Configuration
{
    class SqlServerDatabasePoolSqlConnectionProvider : ISqlConnectionProvider, IDisposable
    {
        readonly SqlServerDatabasePool _pool;
        public SqlServerDatabasePoolSqlConnectionProvider() => _pool = new SqlServerDatabasePool();
        public ISqlConnection GetConnectionProvider(string parameterName) => new LazySqlServerConnection(() => _pool.ConnectionProviderFor(parameterName).ConnectionString);
        public void Dispose() => _pool.Dispose();
    }
}
﻿using System.Configuration;
using Composable.KeyValueStorage;
using Composable.KeyValueStorage.SqlServer;
using NUnit.Framework;

namespace CQRS.Tests.KeyValueStorage.Sql
{
    [TestFixture]
    class SqlDocumentDbTests : DocumentDbTests
    {
        private static string connectionString = ConfigurationManager.ConnectionStrings["KeyValueStore"].ConnectionString;
        
        [SetUp]
        public static void Setup()
        {
            SqlServerDocumentDb.ResetDB(connectionString);
        }

        protected override IDocumentDb CreateStore()
        {
            return new SqlServerDocumentDb(connectionString);
        }
    }
}
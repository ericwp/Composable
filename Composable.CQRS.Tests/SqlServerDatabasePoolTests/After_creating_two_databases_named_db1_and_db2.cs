using System;
using System.Configuration;
using Composable.System;
using Composable.System.Data.SqlClient;
using Composable.Testing;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.CQRS.Tests.SqlServerDatabasePoolTests
{
    [TestFixture]
    public class After_creating_two_databases_named_db1_and_db2
    {
        string _masterConnectionString;
        SqlServerDatabasePool _manager;
        string _dB1ConnectionString;
        string _dB2ConnectionString;
        const string Db1 = "LocalDBManagerTests_After_creating_connection_Db1";
        const string Db2 = "LocalDBManagerTests_After_creating_connection_Db2";


        [OneTimeSetUp] public void OneTimeSetup()
        {
            _masterConnectionString = ConfigurationManager.ConnectionStrings["MasterDB"].ConnectionString;
            //SqlServerDatabasePool.DropAllAndStartOver(_masterConnectionString);
        }

        [SetUp]
        public void SetupTask()
        {
            _manager = new SqlServerDatabasePool(_masterConnectionString);
            _dB1ConnectionString = _manager.ConnectionStringFor(Db1).Value;
            _dB2ConnectionString = _manager.ConnectionStringFor(Db2).Value;

        }

        [Test]
        public void Connection_to_Db1_can_be_opened_and_used()
        {
            new SqlServerConnectionProvider(_manager.ConnectionStringFor(Db1).Value).ExecuteScalar("select 1")
                                                                               .Should().Be(1);
        }

        [Test]
        public void Connection_to_Db2_can_be_opened_and_used()
        {
            new SqlServerConnectionProvider(_manager.ConnectionStringFor(Db2).Value).ExecuteScalar("select 1")
                                                                               .Should().Be(1);
        }

        [Test]
        public void The_same_connection_string_is_returned_by_each_call_to_CreateOrGetLocalDb_Db1()
        {
            _manager.ConnectionStringFor(Db1).Value
                    .Should().Be(_dB1ConnectionString);
        }

        [Test]
        public void The_same_connection_string_is_returned_by_each_call_to_CreateOrGetLocalDb_Db2()
        {
            _manager.ConnectionStringFor(Db2).Value
                    .Should().Be(_dB2ConnectionString);
        }

        [Test]
        public void The_Db1_connectionstring_is_different_from_the_Db2_connection_string()
        {
            _dB1ConnectionString.Should().NotBe(_dB2ConnectionString);
        }

        [TearDown]
        public void TearDownTask()
        {
            _manager.Dispose();

            _manager.Invoking(man => man.ConnectionStringFor(Db1).TouchValue())
                    .ShouldThrow<Exception>()
                    .Where(exception => exception.Message.ToLower().Contains("disposed"));
        }
    }
}
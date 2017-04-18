﻿using System;
using System.Configuration;
using Composable.System;
using Composable.Testing;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.CQRS.Tests.SqlServerDatabasePoolTests
{
    [TestFixture, Performance]
    public class PerformanceTests
    {
        static readonly string MasterConnectionString = ConfigurationManager.ConnectionStrings["MasterDB"].ConnectionString;

        [SetUp]
        public void WarmUpCache()
        {
            using(new SqlServerDatabasePool(MasterConnectionString)) { }
        }

        [Test]
        public void Single_thread_can_reserve_and_release_10_identically_named_databases_in_30_milliseconds()
        {
            var dbName = "74EA37DF-03CE-49C4-BDEC-EAD40FAFB3A1";

            TimeAsserter.Execute(
                action:
                () =>
                {
                    using(var manager = new SqlServerDatabasePool(MasterConnectionString))
                    {
                        manager.ConnectionStringFor(dbName).TouchValue();
                    }
                },
                iterations: 10,
                maxTotal: TimeSpanConversionExtensions.Milliseconds(30));
        }

        [Test]
        public void Multiple_threads_can_reserve_and_release_10_identically_named_databases_in_50_milliseconds()
        {
            var dbName = "EB82270F-E0BA-49F7-BC09-79AE95BA109F";

            TimeAsserter.ExecuteThreaded(
                action:
                () =>
                {
                    using(var manager = new SqlServerDatabasePool(MasterConnectionString))
                    {
                        manager.ConnectionStringFor(dbName).TouchValue();
                    }
                },
                iterations: 10,
                timeIndividualExecutions: true,
                maxTotal: TimeSpanConversionExtensions.Milliseconds(50));
        }

        [Test]
        public void Multiple_threads_can_reserve_and_release_10_differently_named_databases_in_20_milliseconds()
        {
            SqlServerDatabasePool manager = null;

            TimeAsserter.ExecuteThreaded(
                setup: () =>
                       {
                           manager = new SqlServerDatabasePool(MasterConnectionString);
                           manager.ConnectionStringFor("fake_to_force_creation_of_manager_database").TouchValue();
                       },
                tearDown: () => manager.Dispose(),
                action: () => manager.ConnectionStringFor(Guid.NewGuid().ToString()).TouchValue(),
                iterations: 10,
                maxTotal: TimeSpanConversionExtensions.Milliseconds(20)
            );
        }

        [Test]
        public void Single_thread_can_reserve_and_release_10_differently_named_databases_in_15_milliseconds()
        {
            SqlServerDatabasePool manager = null;

            TimeAsserter.Execute(
                setup: () =>
                       {
                           manager = new SqlServerDatabasePool(MasterConnectionString);
                           manager.ConnectionStringFor("fake_to_force_creation_of_manager_database").TouchValue();
                       },
                tearDown: () => manager.Dispose(),
                action: () => manager.ConnectionStringFor(Guid.NewGuid().ToString()).TouchValue(),
                iterations: 10,
                maxTotal: TimeSpanConversionExtensions.Milliseconds(15)
            );
        }

        [Test]
        public void Repeated_fetching_of_same_connection_runs_200_times_in_ten_milliseconds()
        {
            var dbName = "4669B59A-E0AC-4E76-891C-7A2369AE0F2F";
            using(var manager = new SqlServerDatabasePool(MasterConnectionString))
            {
                manager.ConnectionStringFor(dbName).TouchValue();

                TimeAsserter.Execute(
                    action: () => manager.ConnectionStringFor(dbName).TouchValue(),
                    iterations: 200,
                    maxTotal: TimeSpanConversionExtensions.Milliseconds(10)
                );
            }
        }
    }
}

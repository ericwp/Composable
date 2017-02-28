﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Castle.Windsor;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.MicrosoftSQLServer;
using Composable.CQRS.EventSourcing.Refactoring.Migrations;
using Composable.CQRS.Testing;
using Composable.CQRS.Testing.Windsor;
using Composable.GenericAbstractions.Time;
using Composable.ServiceBus;
using Composable.SystemExtensions.Threading;
using CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations;
using FluentAssertions;
using NUnit.Framework;
using TestAggregates;

namespace CQRS.Tests.CQRS.EventSourcing.Sql
{
    [TestFixture]
    class MigratedSqlServerEventStoreSessionTests : NoSqlTest
    {
        string _connectionString;
        WindsorContainer _windsorContainer;

        protected TestingOnlyServiceBus Bus { get; private set; }

        [SetUp]
        public void Setup()
        {
            _windsorContainer = new WindsorContainer();
            Bus = new TestingOnlyServiceBus(DummyTimeSource.Now);
            var masterConnectionString = ConfigurationManager.ConnectionStrings["MasterDb"].ConnectionString;
            _connectionString = _windsorContainer.RegisterSqlServerDatabasePool(masterConnectionString)
                .ConnectionStringFor("MigratedSqlServerEventStoreSessionTests_EventStore");

            SqlServerEventStore.ClearAllCache();
        }

        protected IEventStoreSession OpenSession(IEventStore store)
        {
            return new EventStoreSession(Bus, store, new SingleThreadUseGuard(), DateTimeNowTimeSource.Instance);
        }

        protected IEventStore CreateStore(bool withMigrations = true)
        {
            var migrations = withMigrations ? (IEnumerable<IEventMigration>)new EventMigration<IRootEvent>[]
                                                           {
                                                               Before<UserRegistered>.Insert<MigratedBeforeUserRegisteredEvent>(),
                                                               After<UserChangedEmail>.Insert<MigratedAfterUserChangedEmailEvent>()
                                                           }: new List<IEventMigration>();
            return new SqlServerEventStore(_connectionString, new SingleThreadUseGuard(), nameMapper: null, migrations: migrations);

        }

        [Test]
        public void After_migrated_should_get_ordinal_events()
        {
            var user = new User();
            user.Register("email@email.se", "password", Guid.NewGuid());
            using (var session = OpenSession(CreateStore(withMigrations:false)))
            {
                session.Save(user);
                user.ChangeEmail($"newemail@somewhere.not");
                user.ChangeEmail($"newemail1@somewhere.not");
                user.ChangeEmail($"newemail2@somewhere.not");
                session.SaveChanges();
            }

            using (var session = OpenSession(CreateStore()))
            {
                var reader = session as IEventStoreReader;
                var history = reader.GetHistory(user.Id);
                user = session.Get<User>(user.Id);
                user.ChangePassword("NewPassword");
                user.ChangePassword("NewPassword1");
                user.ChangePassword("NewPassword2");
                user.ChangePassword("NewPassword3");
                session.SaveChanges();

            }

            using (var session = OpenSession(CreateStore()))
            {
                var reader = session as IEventStoreReader;
      
                var history1 = reader.GetHistory(user.Id);
                var history2 = reader.GetHistory(user.Id);
                var history3 = reader.GetHistory(user.Id);

                history1.Count().Should().Be(history2.Count());
                history2.Count().Should().Be(history3.Count());
            }
        }

        [TearDown]
        public void TearDownTask() { _windsorContainer.Dispose(); }
    }
}
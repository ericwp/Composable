﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Composable.DependencyInjection;
using Composable.DependencyInjection.Testing;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Serialization.NewtonSoft;
using Composable.System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.CQRS.Tests.CQRS.EventSourcing.Sql
{
    [TestFixture]
    class SqlServerEventStoreSessionTests : EventStoreSessionTests
    {
        string _connectionString;
        [SetUp]
        public void Setup()
        {
            _connectionString = ServiceLocator.EventStoreConnectionString();
        }

        protected override IServiceLocator CreateServiceLocator() => TestWiringHelper.SetupTestingServiceLocator(TestingMode.RealComponents);

        protected override IEventStore CreateStore() => new EventStore(_connectionString, serializer: new NewtonSoftEventStoreEventSerializer());

        [Test]
        public void Serializes_access_to_an_aggregate_so_that_concurrent_transactions_succeed_even_if_history_has_been_read_outside_of_modifying_transactions()
        {
            var user = new User();
            user.Register("email@email.se", "password", Guid.NewGuid());
            UseInTransactionalScope(session =>
                                    {
                                        session.Save(user);
                                        user.ChangeEmail("newemail@somewhere.not");
                                    });

            void UpdateEmail()
            {
                UseInTransactionalScope(session =>
                                        {
                                            ((IEventStoreReader)session).GetHistory(user.Id);
                                            var userToUpdate = session.Get<User>(user.Id);
                                            userToUpdate.ChangeEmail($"newemail_{userToUpdate.Version}@somewhere.not");
                                            Thread.Sleep(100);
                                        }); //Sql duplicate key (AggregateId, Version) Exception would be thrown here if history was not serialized
            }

            var tasks = 1.Through(20).Select(_ => Task.Factory.StartNew(UpdateEmail)).ToArray();
            Task.WaitAll(tasks);

            UseInScope(
                session =>
                {
                    var userHistory = ((IEventStoreReader)session).GetHistory(user.Id)
                                                                  .ToArray(); //Reading the aggregate will throw an exception if the history is invalid.
                    userHistory.Length.Should()
                               .Be(22); //Make sure that all of the transactions completed
                });
        }

        [Test]
        public void Serializes_access_to_an_aggregate_so_that_concurrent_transactions_succeed()
        {
            var user = new User();
            user.Register("email@email.se", "password", Guid.NewGuid());
            UseInTransactionalScope(session =>
                                    {
                                        session.Save(user);
                                        user.ChangeEmail("newemail@somewhere.not");
                                    });

            void UpdateEmail()
            {
                UseInTransactionalScope(session =>
                                        {
                                            var userToUpdate = session.Get<User>(user.Id);
                                            userToUpdate.ChangeEmail($"newemail_{userToUpdate.Version}@somewhere.not");
                                            Thread.Sleep(100);
                                        }); //Sql duplicate key (AggregateId, Version) Exception would be thrown here if history was not serialized
            }

            var tasks = 1.Through(20).Select(_ => Task.Factory.StartNew(UpdateEmail)).ToArray();


            Task.WaitAll(tasks);

            UseInScope(session => ((IEventStoreReader)session).GetHistory(user.Id));
        }

        [Test] public void InsertNewEventType_should_not_throw_exception_if_the_event_type_has_been_inserted_by_something_else()
        {
            User otherUser = null;
            User user = null;
            void ChangeAnotherUsersEmailInOtherInstance()
            {
                using(var clonedServiceLocator = ServiceLocator.Clone())
                {
                    clonedServiceLocator.ExecuteUnitOfWorkInIsolatedScope(() =>
                                                                          {
                                                                              // ReSharper disable once AccessToDisposedClosure
                                                                              var session = clonedServiceLocator.Resolve<ITestingEventstoreSession>();
                                                                              otherUser = User.Register(session,
                                                                                                            "email@email.se",
                                                                                                            "password",
                                                                                                            Guid.NewGuid());
                                                                              otherUser.ChangeEmail("otheruser@email.new");
                                                                          });

                }
            }

            UseInTransactionalScope(session => user = User.Register(session, "email@email.se", "password", Guid.NewGuid()));

            ChangeAnotherUsersEmailInOtherInstance();
            UseInScope(session => session.Get<User>(otherUser.Id).Email.Should().Be("otheruser@email.new"));

            UseInTransactionalScope(session => user.ChangeEmail("some@email.new"));
        }
    }
}
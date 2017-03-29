﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Transactions;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses;
using Composable.Persistence.EventSourcing;
using Composable.Persistence.EventStore;
using Composable.System;
using Composable.System.Diagnostics;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;
using Composable.Testing;
using Composable.UnitsOfWork;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.CQRS.Tests.CQRS.EventSourcing
{
    [TestFixture]
    public abstract class EventStoreSessionTests : NoSqlTest
    {
        TestingOnlyServiceBus Bus { get; set; }

        protected EventStoreSessionTests()
        {
            SetupBus();
        }

        [SetUp] public void SetupBus()
        {
            Bus = new TestingOnlyServiceBus(DummyTimeSource.Now, new MessageHandlerRegistry());
        }

        protected IEventStoreSession OpenSession(IEventStore store) => new EventStoreSession(Bus, store, new SingleThreadUseGuard(), DateTimeNowTimeSource.Instance);

        [Test]
        public void WhenFetchingAggregateThatDoesNotExistNoSuchAggregateExceptionIsThrown()
        {
            using (var session = OpenSession(CreateStore()))
            {
                Assert.Throws<AggregateRootNotFoundException>(() => session.Get<User>(Guid.NewGuid()));
            }
        }

        [Test]
        public void CanSaveAndLoadAggregate()
        {
            var store = CreateStore();

            var user = new User();
            user.Register("email@email.se", "password", Guid.NewGuid());
            user.ChangePassword("NewPassword");
            user.ChangeEmail("NewEmail");

            using (var session = OpenSession(store))
            {
                session.Save(user);
                session.SaveChanges();
            }

            using (var session = OpenSession(store))
            {
                var loadedUser = session.Get<User>(user.Id);

                Assert.That(loadedUser.Id, Is.EqualTo(user.Id));
                Assert.That(loadedUser.Email, Is.EqualTo(user.Email));
                Assert.That(loadedUser.Password, Is.EqualTo(user.Password));

            }
        }

        [Test]
        public void ThrowsIfUsedByMultipleThreads()
        {
            var store = CreateStore();
            IEventStoreSession session = null;
            var wait = new ManualResetEventSlim();
            ThreadPool.QueueUserWorkItem(state =>
                                             {
                                                 session = OpenSession(store);
                                                 wait.Set();
                                             });
            wait.Wait();

            User user;

            Assert.Throws<MultiThreadedUseException>(() => session.Get<User>(Guid.NewGuid()));
            Assert.Throws<MultiThreadedUseException>(() => session.Dispose());
            Assert.Throws<MultiThreadedUseException>(() => session.LoadSpecificVersion<User>(Guid.NewGuid(), 1));
            Assert.Throws<MultiThreadedUseException>(() => session.Save(new User()));
            Assert.Throws<MultiThreadedUseException>(() => session.SaveChanges());
            Assert.Throws<MultiThreadedUseException>(() => session.TryGet(Guid.NewGuid(), out user));

        }

        protected abstract IEventStore CreateStore();

        [Test]
        public void CanLoadSpecificVersionOfAggregate()
        {
            var store = CreateStore();

            var user = new User();
            user.Register("email@email.se", "password", Guid.NewGuid());
            user.ChangePassword("NewPassword");
            user.ChangeEmail("NewEmail");

            using (var session = OpenSession(store))
            {
                session.Save(user);
                session.SaveChanges();
            }

            using (var session = OpenSession(store))
            {
                var loadedUser = session.LoadSpecificVersion<User>(user.Id, 1);
                Assert.That(loadedUser.Id, Is.EqualTo(user.Id));
                Assert.That(loadedUser.Email, Is.EqualTo("email@email.se"));
                Assert.That(loadedUser.Password, Is.EqualTo("password"));

                loadedUser = session.LoadSpecificVersion<User>(user.Id, 2);
                Assert.That(loadedUser.Id, Is.EqualTo(user.Id));
                Assert.That(loadedUser.Email, Is.EqualTo("email@email.se"));
                Assert.That(loadedUser.Password, Is.EqualTo("NewPassword"));

                loadedUser = session.LoadSpecificVersion<User>(user.Id, 3);
                Assert.That(loadedUser.Id, Is.EqualTo(user.Id));
                Assert.That(loadedUser.Email, Is.EqualTo("NewEmail"));
                Assert.That(loadedUser.Password, Is.EqualTo("NewPassword"));
            }
        }

        [Test]
        public void ReturnsSameInstanceOnRepeatedLoads()
        {
            var store = CreateStore();

            var user = new User();
            user.Register("email@email.se", "password", Guid.NewGuid());

            using (var session = OpenSession(store))
            {
                session.Save(user);
                session.SaveChanges();
            }

            using (var session = OpenSession(store))
            {
                var loaded1 = session.Get<User>(user.Id);
                var loaded2 = session.Get<User>(user.Id);
                Assert.That(loaded1, Is.SameAs(loaded2));
            }
        }

        [Test]
        public void ReturnsSameInstanceOnLoadAfterSave()
        {
            var store = CreateStore();

            var user = new User();
            user.Register("email@email.se", "password", Guid.NewGuid());

            using (var session = OpenSession(store))
            {
                session.Save(user);

                var loaded1 = session.Get<User>(user.Id);
                var loaded2 = session.Get<User>(user.Id);
                Assert.That(loaded1, Is.SameAs(loaded2));
                Assert.That(loaded1, Is.SameAs(user));

                session.SaveChanges();
            }
        }

        [Test]
        public void TracksAndUpdatesLoadedAggregates()
        {
            var store = CreateStore();

            var user = new User();
            user.Register("email@email.se", "password", Guid.NewGuid());

            using (var session = OpenSession(store))
            {
                session.Save(user);
                session.SaveChanges();
            }

            using (var session = OpenSession(store))
            {
                var loadedUser = session.Get<User>(user.Id);
                loadedUser.ChangePassword("NewPassword");
                session.SaveChanges();
            }

            using (var session = OpenSession(store))
            {
                var loadedUser = session.Get<User>(user.Id);
                Assert.That(loadedUser.Password, Is.EqualTo("NewPassword"));
            }
        }

        [Test]
        public void DoesNotUpdateAggregatesLoadedViaSpecificVersion()
        {
            var store = CreateStore();

            var user = new User();
            user.Register("OriginalEmail", "password", Guid.NewGuid());

            using (var session = OpenSession(store))
            {
                session.Save(user);
                session.SaveChanges();
            }

            using (var session = OpenSession(store))
            {
                var loadedUser = session.LoadSpecificVersion<User>(user.Id, 1);
                loadedUser.ChangeEmail("NewEmail");
                session.SaveChanges();
            }

            using (var session = OpenSession(store))
            {
                var loadedUser = session.Get<User>(user.Id);
                Assert.That(loadedUser.Email, Is.EqualTo("OriginalEmail"));
            }
        }

        [Test]
        public void ResetsAggregatesAfterSaveChanges()
        {
            var store = CreateStore();

            var user = new User();
            user.Register("OriginalEmail", "password", Guid.NewGuid());

            using (var session = OpenSession(store))
            {
                session.Save(user);
                session.SaveChanges();
                Assert.That((user as IEventStored).GetChanges(), Is.Empty);
            }
        }

        [Test]
        public void ThrowsWhenAttemptingToSaveExistingAggregate()
        {
            var store = CreateStore();

            var user = new User();
            user.Register("OriginalEmail", "password", Guid.NewGuid());

            using (var session = OpenSession(store))
            {
                session.Save(user);
                session.SaveChanges();
            }

            Assert.Throws<AttemptToSaveAlreadyPersistedAggregateException>(() =>
                                                                               {
                                                                                   using (var session = OpenSession(store))
                                                                                   {
                                                                                       session.Save(user);
                                                                                       session.SaveChanges();
                                                                                   }
                                                                               });
        }

        [Test]
        public void DoesNotExplodeWhenSavingMoreThan10Events()
        {
            var store = CreateStore();

            var user = new User();
            user.Register("OriginalEmail", "password", Guid.NewGuid());
            1.Through(100).ForEach(index => user.ChangeEmail("email" + index));

            using (var session = OpenSession(store))
            {
                session.Save(user);
                session.SaveChanges();
            }
        }


        //todo:remove this
        class MockEventStore : IEventStore
        {
            public readonly List<IAggregateRootEvent> SavedEvents = new List<IAggregateRootEvent>();
            public readonly List<Guid> DeletedAggregates = new List<Guid>();

            public void Dispose() { }
            public IEnumerable<IAggregateRootEvent> GetAggregateHistoryForUpdate(Guid id) => throw new NotImplementedException();
            public IEnumerable<IAggregateRootEvent> GetAggregateHistory(Guid id) => throw new NotSupportedException();
            public void SaveEvents(IEnumerable<IAggregateRootEvent> events) { SavedEvents.AddRange(events); }
            public void StreamEvents(int batchSize, Action<IReadOnlyList<IAggregateRootEvent>> handleEvents) { throw new NotImplementedException(); }
            public void DeleteEvents(Guid aggregateId) { DeletedAggregates.Add(aggregateId); }
            public void PersistMigrations() { throw new NotImplementedException(); }
            public IEnumerable<Guid> StreamAggregateIdsInCreationOrder(Type eventBaseType = null) => throw new NotImplementedException();
        }

        [Test]
        public void EventsArePublishedOnSaveChangesAndThisInteractsWithUnitOfWorkParticipations()
        {
            var bus = new TestingOnlyServiceBus(DummyTimeSource.Now, new MessageHandlerRegistry());
            var store = new MockEventStore();

            var users = 1.Through(9).Select(i => { var u = new User(); u.Register(i + "@test.com", "abcd", Guid.NewGuid()); u.ChangeEmail("new" + i + "@test.com"); return u; }).ToList();

            using (var session = new EventStoreSession(bus, store, new SingleThreadUseGuard(), DateTimeNowTimeSource.Instance))
            {
                var uow = UnitOfWork.Create(new SingleThreadUseGuard());
                uow.AddParticipant(session);

                users.Take(3).ForEach(u => session.Save(u));
                Assert.That(bus.DispatchedMessages.Count, Is.EqualTo(0));
                session.SaveChanges();
                Assert.That(bus.DispatchedMessages.Count, Is.EqualTo(6));

                users.Skip(3).Take(3).ForEach(u => session.Save(u));
                Assert.That(bus.DispatchedMessages.Count, Is.EqualTo(6));
                session.SaveChanges();
                Assert.That(bus.DispatchedMessages.Count, Is.EqualTo(12));

                users.Skip(6).Take(3).ForEach(u => session.Save(u));

                Assert.That(bus.DispatchedMessages.Count, Is.EqualTo(12));
                Assert.That(store.SavedEvents.Count, Is.EqualTo(0));
                uow.Commit();
                Assert.That(bus.DispatchedMessages.Count, Is.EqualTo(18));

                Assert.That(bus.DispatchedMessages.OfType<IAggregateRootEvent>().Select(e => e.EventId).Distinct().Count(), Is.EqualTo(18));
                Assert.That(bus.DispatchedMessages, Is.EquivalentTo(store.SavedEvents));
            }
        }

        [Test]
        public void EventsAreDeletedWhenNotAUnitOfWorkParticipant()
        {
            var bus = new TestingOnlyServiceBus(DummyTimeSource.Now, new MessageHandlerRegistry());
            var store = new MockEventStore();

            using (var session = new EventStoreSession(bus, store, new SingleThreadUseGuard(), DateTimeNowTimeSource.Instance))
            {
                var aggregate1 = new Guid("92EC4FE2-26A8-4274-8674-DC5D95513C83");
                var aggregate2 = new Guid("F08200E4-8790-4ECC-9F06-A3D3BAC9E21C");

                session.Delete(aggregate1);
                session.Delete(aggregate2);

                session.SaveChanges();
                session.SaveChanges();  // Verify that SaveChanges() does not delete the events twice.

                Assert.That(store.DeletedAggregates, Is.EquivalentTo(new[] { aggregate1, aggregate2 }));
            }
        }

        [Test]
        public void EventsAreDeletedWhenUnitOfWorkIsCommitted()
        {
            var bus = new TestingOnlyServiceBus(DummyTimeSource.Now, new MessageHandlerRegistry());
            var store = new MockEventStore();

            using (var session = new EventStoreSession(bus, store, new SingleThreadUseGuard(), DateTimeNowTimeSource.Instance))
            {
                var uow = UnitOfWork.Create(new SingleThreadUseGuard());
                uow.AddParticipant(session);

                var aggregate1 = new Guid("92EC4FE2-26A8-4274-8674-DC5D95513C83");
                var aggregate2 = new Guid("F08200E4-8790-4ECC-9F06-A3D3BAC9E21C");

                session.Delete(aggregate1);
                session.Delete(aggregate2);

                session.SaveChanges();

                Assert.That(store.DeletedAggregates, Is.Empty);

                uow.Commit();

                Assert.That(store.DeletedAggregates, Is.EquivalentTo(new[] { aggregate1, aggregate2 }));
            }
        }

        [Test]
        public void AggregateCannotBeRetreivedAfterBeingDeleted()
        {
            var store = CreateStore();

            var user1 = new User();
            user1.Register("email1@email.se", "password", Guid.NewGuid());

            var user2 = new User();
            user2.Register("email2@email.se", "password", Guid.NewGuid());

            using (var session = OpenSession(store))
            {
                session.Save(user1);
                session.Save(user2);
                session.SaveChanges();
            }

            using (var session = OpenSession(store))
            {
                session.Delete(user1.Id);

                User loadedUser1;
                Assert.IsFalse(session.TryGet(user1.Id, out loadedUser1));

                var loadedUser2 = session.Get<User>(user2.Id);
                Assert.That(loadedUser2.Id, Is.EqualTo(user2.Id));
                Assert.That(loadedUser2.Email, Is.EqualTo(user2.Email));
                Assert.That(loadedUser2.Password, Is.EqualTo(user2.Password));
            }
        }

        [Test]
        public void DeletingAnAggregateDoesNotPreventEventsForItFromBeingRaised()
        {
            var store = CreateStore();

            var user1 = new User();
            user1.Register("email1@email.se", "password", Guid.NewGuid());

            var user2 = new User();
            user2.Register("email2@email.se", "password", Guid.NewGuid());

            using (var session = OpenSession(store))
            {
                session.Save(user1);
                session.Save(user2);
                session.SaveChanges();
            }

            Bus.DispatchedMessages.Count().Should().Be(2);

            using (var session = OpenSession(store))
            {
                user1 = session.Get<User>(user1.Id);

                user1.ChangeEmail("new_email");

                session.Delete(user1.Id);

                session.SaveChanges();

                var published = Bus.DispatchedMessages.ToList();
                Bus.DispatchedMessages.Count().Should().Be(3);
                Assert.That(published.Last(), Is.InstanceOf<UserChangedEmail>());
            }
        }

        [Test]
        public void When_fetching_history_from_the_same_instance_after_updating_an_aggregate_the_fetched_history_includes_the_new_events()
        {
            var store = CreateStore();
            var userId = Guid.NewGuid();
            using (var session = OpenSession(store))
            {
                var user = new User();
                user.Register("test@email.com", "Password1", userId);
                session.Save(user);
                session.SaveChanges();
            }

            using (var session = OpenSession(store))
            {

                var user = session.Get<User>(userId);
                user.ChangeEmail("new_email@email.com");
                session.SaveChanges();

                var history = ((IEventStoreReader)session).GetHistory(user.Id);
                Assert.That(history.Count(), Is.EqualTo(2));
            }
        }

        [Test]
        public void When_deleting_and_then_fetching_an_aggregates_history_the_history_should_be_gone()
        {
            var store = CreateStore();
            var userId = Guid.NewGuid();
            using (var session = OpenSession(store))
            {
                var user = new User();
                user.Register("test@email.com", "Password1", userId);
                session.Save(user);
                session.SaveChanges();
            }

            using (var session = OpenSession(store))
            {
                session.Delete(userId);
                session.SaveChanges();

                var history = ((IEventStoreReader)session).GetHistory(userId);
                Assert.That(history.Count(), Is.EqualTo(0));
            }
        }

        [Test]
        public void When_fetching_and_deleting_an_aggregate_then_fetching_history_again_the_history_should_be_gone()
        {
            var store = CreateStore();
            var userId = Guid.NewGuid();
            using (var session = OpenSession(store))
            {
                var user = new User();
                user.Register("test@email.com", "Password1", userId);
                session.Save(user);
                session.SaveChanges();
            }

            using (var session = OpenSession(store))
            {
                session.Get<User>(userId);
                session.Delete(userId);
                session.SaveChanges();

                var history = ((IEventStoreReader)session).GetHistory(userId);
                Assert.That(history.Count(), Is.EqualTo(0));
            }
        }


        [Test]
        public void Concurrent_read_only_access_to_aggregate_history_can_occur_in_paralell()
        {
            var user = new User();
            user.Register("email@email.se", "password", Guid.NewGuid());
            using (var session = OpenSession(CreateStore()))
            {
                session.Save(user);
            }

            var iterations = 20;
            var delayEachTransactionByMilliseconds = 100;

            void ReadUserHistory()
            {
                using(var session = OpenSession(CreateStore()))
                {
                    using(var transaction = new TransactionScope())
                    {
                        ((IEventStoreReader)session).GetHistory(user.Id);
                        Thread.Sleep(TimeSpanExtensions.Milliseconds(delayEachTransactionByMilliseconds));
                        transaction.Complete();
                    }
                }
            }

            ReadUserHistory();//one warmup to get consistent times later.
            var timeForSingleTransactionalRead = (int)StopwatchExtensions.TimeExecution(ReadUserHistory).TotalMilliseconds;

            var timingsSummary = TimeAsserter.ExecuteThreaded(
                action: ReadUserHistory,
                iterations: iterations,
                timeIndividualExecutions:true,
                maxTotal: TimeSpanExtensions.Milliseconds(((iterations * timeForSingleTransactionalRead) / 2)),
                description: $"If access is serialized the time will be approximately {iterations * timeForSingleTransactionalRead} milliseconds. If parelellized it should be far below this value.");

            timingsSummary.Average.Should().BeLessThan(TimeSpanExtensions.Milliseconds(delayEachTransactionByMilliseconds));

            timingsSummary.IndividualExecutionTimes.Sum().Should().BeGreaterThan(timingsSummary.Total);
        }

        //[Test]
        //public void SaveChangesAndCommitWhenTransientTransactionDoesSo()
        //{
        //    var store = CreateStore();

        //    var user = new User();
        //    user.Register("email@email.se", "password", Guid.NewGuid());
        //    user.ChangePassword("NewPassword");
        //    user.ChangeEmail("NewEmail");

        //    using(var session = OpenSession(store))
        //    {
        //        using (var transaction = new TransactionScope())
        //        {
        //            session.Save(user);
        //            transaction.Complete();
        //        }
        //    }

        //    using(var session = OpenSession(store))
        //    {
        //        var loadedUser = session.Get<User>(user.Id);

        //        OldObsoletedAssert.That(loadedUser.Id, Is.EqualTo(user.Id));
        //        OldObsoletedAssert.That(loadedUser.Email, Is.EqualTo(user.Email));
        //        OldObsoletedAssert.That(loadedUser.Password, Is.EqualTo(user.Password));

        //    }
        //}

        //[Test]
        //public void SaveChangesAndCommitWhenTransientTransactionDoesSoWhenDisposedWithinTransaction()
        //{
        //    var store = CreateStore();

        //    var user = new User();
        //    user.Register("email@email.se", "password", Guid.NewGuid());
        //    user.ChangePassword("NewPassword");
        //    user.ChangeEmail("NewEmail");

        //    using (var transaction = new TransactionScope())
        //    {
        //        using(var session = OpenSession(store))
        //        {
        //            session.Save(user);
        //            transaction.Complete();
        //        }
        //    }

        //    using (var session = OpenSession(store))
        //    {
        //        var loadedUser = session.Get<User>(user.Id);

        //        OldObsoletedAssert.That(loadedUser.Id, Is.EqualTo(user.Id));
        //        OldObsoletedAssert.That(loadedUser.Email, Is.EqualTo(user.Email));
        //        OldObsoletedAssert.That(loadedUser.Password, Is.EqualTo(user.Password));

        //    }
        //}
    }

    public class NoSqlTest
    {
        static NoSqlTest()
        {
            log4net.Config.XmlConfigurator.Configure();
        }
    }
}
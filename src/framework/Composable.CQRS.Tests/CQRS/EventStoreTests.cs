﻿using System;
using System.Linq;
using Composable.DependencyInjection;
using Composable.Persistence.EventStore;
using Composable.Refactoring.Naming;
using Composable.System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.CQRS
{
    [TypeId("F3A9976D-BB41-4C86-AAE9-D47CC23392BE")]interface ISomeEvent : IAggregateRootEvent {}

    [TypeId("E8040D3F-61BA-414A-937E-2DF0A356ED78")]class SomeEvent : AggregateRootEvent, ISomeEvent
    {
        public SomeEvent(Guid aggregateRootId, int version) : base(aggregateRootId)
        {
            AggregateRootVersion = version;
            UtcTimeStamp = new DateTime(UtcTimeStamp.Year, UtcTimeStamp.Month, UtcTimeStamp.Day, UtcTimeStamp.Hour, UtcTimeStamp.Minute, UtcTimeStamp.Second);
        }
    }

    [TestFixture] public abstract class EventStoreTests
    {
        IDisposable _scope;
        protected abstract IServiceLocator CreateServiceLocator();

        IEventStore _eventStore;

        IServiceLocator _serviceLocator;

        [SetUp] public void SetupTask()
        {
            _serviceLocator = CreateServiceLocator();
            _serviceLocator.Resolve<ITypeMappingRegistar>()
                           .Map<Composable.Tests.CQRS.SomeEvent>("9e71c8cb-397a-489c-8ff7-15805a7509e8");
            _scope = _serviceLocator.BeginScope();
            _eventStore = _serviceLocator.EventStore();
        }

        [TearDown] public void TearDownTask()
        {
            _scope.Dispose();
            _serviceLocator.Dispose();
        }

        [Test] public void StreamEventsSinceReturnsWholeEventLogWhenFromEventIdIsNull()
        {
            var aggregateId = Guid.NewGuid();
            _eventStore.SaveEvents(1.Through(10)
                                   .Select(i => new SomeEvent(aggregateId, i)));
            var stream = _eventStore.ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize();

            stream.Should()
                  .HaveCount(10);
        }

        [Test] public void StreamEventsSinceReturnsWholeEventLogWhenFetchingALargeNumberOfEvents_EnsureBatchingDoesNotBreakThings()
        {
            const int batchSize = 100;
            const int moreEventsThanTheBatchSizeForStreamingEvents = batchSize + 10;
            var aggregateId = Guid.NewGuid();
            _eventStore.SaveEvents(1.Through(moreEventsThanTheBatchSizeForStreamingEvents)
                                   .Select(i => new SomeEvent(aggregateId, i)));
            var stream = _eventStore.ListAllEventsForTestingPurposesAbsolutelyNotUsableForARealEventStoreOfAnySize(batchSize: batchSize)
                                   .ToList();

            var currentEventNumber = 0;
            stream.Should()
                  .HaveCount(moreEventsThanTheBatchSizeForStreamingEvents);
            foreach(var aggregateRootEvent in stream)
            {
                aggregateRootEvent.AggregateRootVersion.Should()
                                  .Be(++currentEventNumber, "Incorrect event version detected");
            }
        }

        [Test] public void DeleteEventsDeletesTheEventsForOnlyTheSpecifiedAggregate()
        {
            var aggregatesWithEvents = 1.Through(10)
                                        .ToDictionary(i => i,
                                                      i =>
                                                      {
                                                          var aggregateId = Guid.NewGuid();
                                                          return 1.Through(10)
                                                                  .Select(j => new SomeEvent(aggregateId, j))
                                                                  .ToList();
                                                      });

            _eventStore.SaveEvents(aggregatesWithEvents.SelectMany(x => x.Value));
            var toRemove = aggregatesWithEvents[2][0]
                .AggregateRootId;
            aggregatesWithEvents.Remove(2);

            _eventStore.DeleteAggregate(toRemove);

            foreach(var kvp in aggregatesWithEvents)
            {
                var stream = _eventStore.GetAggregateHistory(kvp.Value[0]
                                                               .AggregateRootId);
                stream.Should()
                      .HaveCount(10);
            }
            _eventStore.GetAggregateHistory(toRemove)
                      .Should()
                      .BeEmpty();
        }

        [Test] public void GetListOfAggregateIds()
        {
            var aggregatesWithEvents = 1.Through(10)
                                        .ToDictionary(i => i,
                                                      i =>
                                                      {
                                                          var aggregateId = Guid.NewGuid();
                                                          return 1.Through(10)
                                                                  .Select(j => new SomeEvent(aggregateId, j))
                                                                  .ToList();
                                                      });

            _eventStore.SaveEvents(aggregatesWithEvents.SelectMany(x => x.Value));
            var allAggregateIds = _eventStore.StreamAggregateIdsInCreationOrder()
                                            .ToList();
            Assert.AreEqual(aggregatesWithEvents.Count, allAggregateIds.Count);
        }

        [Test] public void GetListOfAggregateIdsUsingBaseEventType()
        {
            var aggregatesWithEvents = 1.Through(10)
                                        .ToDictionary(i => i,
                                                      i =>
                                                      {
                                                          var aggregateId = Guid.NewGuid();
                                                          return 1.Through(10)
                                                                  .Select(j => new SomeEvent(aggregateId, j))
                                                                  .ToList();
                                                      });

            _eventStore.SaveEvents(aggregatesWithEvents.SelectMany(x => x.Value));
            var allAggregateIds = _eventStore.StreamAggregateIdsInCreationOrder<ISomeEvent>()
                                            .ToList();
            Assert.AreEqual(aggregatesWithEvents.Count, allAggregateIds.Count);
        }
    }

    [TestFixture] public class InMemoryEventStoreTests : EventStoreTests
    {
        protected override IServiceLocator CreateServiceLocator() => TestWiringHelper.SetupTestingServiceLocator(TestingMode.InMemory);
    }

    [TestFixture] public class SqlServerEventStoreTests : EventStoreTests
    {
        protected override IServiceLocator CreateServiceLocator() => TestWiringHelper.SetupTestingServiceLocator(TestingMode.DatabasePool);
    }
}

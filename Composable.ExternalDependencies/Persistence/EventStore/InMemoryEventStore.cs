﻿using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Contracts;
using Composable.Persistence.EventStore.Refactoring.Migrations;
using Composable.System.Linq;

namespace Composable.Persistence.EventStore
{
    //todo: Refactor to use the same serialization code as the sql server event store so that tests actually tests roundtrip serialization
#pragma warning disable 618
    class InMemoryEventStore : IEventStore
#pragma warning restore 618
    {
        IReadOnlyList<IEventMigration> _migrationFactories;

        IList<AggregateRootEvent> _events = new List<AggregateRootEvent>();
        int _insertionOrder;

        public void Dispose()
        {
        }

        readonly object _lockObject = new object();

        public InMemoryEventStore(IEnumerable<IEventMigration> migrations = null ) => _migrationFactories = migrations?.ToList() ?? new List<IEventMigration>();

        public IReadOnlyList<IAggregateRootEvent> GetAggregateHistoryForUpdate(Guid id) => GetAggregateHistory(id);

        public IReadOnlyList<IAggregateRootEvent> GetAggregateHistory(Guid id)
        {
            lock(_lockObject)
            {
                return SingleAggregateInstanceEventStreamMutator.MutateCompleteAggregateHistory(_migrationFactories, _events.Where(e => e.AggregateRootId == id).ToArray())
                    .ToList();
            }
        }

        public void SaveEvents(IEnumerable<IAggregateRootEvent> events)
        {
            lock(_lockObject)
            {
                events.Cast<AggregateRootEvent>().ForEach(
                    @event =>
                    {
                        @event.InsertionOrder = ++_insertionOrder;
                        _events.Add(@event);
                    });
            }
        }

        IEnumerable<IAggregateRootEvent> StreamEvents()
        {
            lock(_lockObject)
            {
                var streamMutator = CompleteEventStoreStreamMutator.Create(_migrationFactories);
                return streamMutator.Mutate(_events).ToList();
            }
        }

        public void StreamEvents(int batchSize, Action<IReadOnlyList<IAggregateRootEvent>> handleEvents)
        {
            var batches = StreamEvents()
                .ChopIntoSizesOf(batchSize)
                .Select(batch => batch.ToList());
            foreach(var batch in batches)
            {
                handleEvents(batch);
            }
        }

        public void DeleteEvents(Guid aggregateId)
        {
            lock(_lockObject)
            {
                for(var i = 0; i < _events.Count; i++)
                {
                    if(_events[i].AggregateRootId == aggregateId)
                    {
                        _events.RemoveAt(i);
                        i--;
                    }
                }
            }
        }

        public void PersistMigrations() { _events = StreamEvents().Cast<AggregateRootEvent>().ToList(); }

        public IEnumerable<Guid> StreamAggregateIdsInCreationOrder(Type eventBaseType = null)
        {
            Contract.Assert.That(eventBaseType == null || eventBaseType.IsInterface && typeof(IAggregateRootEvent).IsAssignableFrom(eventBaseType),
                                 "eventBaseType == null || eventBaseType.IsInterface && typeof(IAggregateRootEvent).IsAssignableFrom(eventBaseType)");

            lock (_lockObject)
            {
                return _events
                    .Where(e => eventBaseType == null || eventBaseType.IsInstanceOfType(e))
                    .OrderBy(e => e.UtcTimeStamp)
                    .Select(e => e.AggregateRootId)
                    .Distinct()
                    .ToList();
            }
        }

        public void TestingOnlyReplaceMigrations(IReadOnlyList<IEventMigration> migrations) { _migrationFactories = migrations; }
    }
}
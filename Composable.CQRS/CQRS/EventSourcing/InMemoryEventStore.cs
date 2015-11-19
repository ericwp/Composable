﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Composable.CQRS.EventSourcing.Refactoring.Migrations;
using Composable.System.Linq;

namespace Composable.CQRS.EventSourcing
{
    //todo: Refactor to use the same serialization code as the sql server event store so that tests actually tests roundtrip serialization
    public class InMemoryEventStore : IEventStore
    {
        private IReadOnlyList<IEventMigration> _migrationFactories;

        private IList<AggregateRootEvent> _events = new List<AggregateRootEvent>();
        private int InsertionOrder;

        public void Dispose()
        {
        }

        private object _lockObject = new object();

        public InMemoryEventStore(IEnumerable<IEventMigration> migrationFactories = null )
        {
            _migrationFactories = migrationFactories?.ToList() ?? new List<IEventMigration>();
        }

        public IEnumerable<IAggregateRootEvent> GetAggregateHistory(Guid id)
        {
            lock(_lockObject)
            {
                return SingleAggregateInstanceEventStreamMutator.MutateCompleteAggregateHistory(_migrationFactories, _events.Where(e => e.AggregateRootId == id).ToList())
                    .ToList();;
            }
        }

        public void SaveEvents(IEnumerable<IAggregateRootEvent> events)
        {
            lock(_lockObject)
            {
                events.Cast<AggregateRootEvent>().ForEach(
                    @event =>
                    {
                        ((AggregateRootEvent)@event).InsertionOrder = ++InsertionOrder;
                        _events.Add(@event);
                    });
            }
        }

        public IEnumerable<IAggregateRootEvent> StreamEvents()
        {
            lock(_lockObject)
            {
                var streamMutator = CompleteEventStoreStreamMutator.Create(_migrationFactories);
                return streamMutator.Mutate(_events).ToList();
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
            Contract.Requires(eventBaseType == null || (eventBaseType.IsInterface && typeof(IAggregateRootEvent).IsAssignableFrom(eventBaseType)));

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
        public IEnumerable<IAggregateRootEvent> StreamEventsAfterEventWithId(Guid? startAfterEventId) { throw new NotImplementedException(); }

        public void Reset()
        {
            lock(_lockObject)
            {
                _events = new List<AggregateRootEvent>();
            }
        }


        internal void TestingOnlyReplaceMigrations(IReadOnlyList<IEventMigration> migrations) { _migrationFactories = migrations; }
    }
}
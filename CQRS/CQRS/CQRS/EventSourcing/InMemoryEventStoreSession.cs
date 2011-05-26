﻿using System;
using System.Collections.Generic;
using System.Linq;
using Composable.System.Linq;

namespace Composable.CQRS.EventSourcing
{
    public class InMemoryEventStoreSession : EventStoreSession, IEventStoreSession
    {
        private readonly InMemoryEventStore _store;

        public InMemoryEventStoreSession(InMemoryEventStore store)
        {
            _store = store;
        }

        public override void Dispose()
        {
            _idMap.Clear();
        }

        protected override IEnumerable<IAggregateRootEvent> GetHistoryUnSafe(Guid id)
        {
            return _store.Events.Where(e => e.AggregateRootId == id);
        }

        protected override void SaveEvents(IEnumerable<IAggregateRootEvent> events)
        {
            events.ForEach(e => _store.Events.Add(e));
        }
    }
}
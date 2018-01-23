﻿using System;
using Composable.Persistence.EventStore.Aggregates;

namespace Composable.Persistence.EventStore
{
    public class AggregateRepository<TAggregate, TBaseEventClass, TBaseEventInterface> : IAggregateRepository<TAggregate>
        where TAggregate : Aggregate<TAggregate, TBaseEventClass, TBaseEventInterface>, IEventStored
        where TBaseEventClass : AggregateEvent, TBaseEventInterface
        where TBaseEventInterface : class, IAggregateEvent
    {
        readonly IEventStoreReader _reader;
        readonly IEventStoreUpdater _aggregates;

        protected AggregateRepository(IEventStoreUpdater aggregates, IEventStoreReader reader)
        {
            _aggregates = aggregates;
            _reader = reader;
        }

        public virtual TAggregate Get(Guid id) => _aggregates.Get<TAggregate>(id);

        public virtual void Add(TAggregate aggregate)
        {
            _aggregates.Save(aggregate);
        }

        //todo: readonly copy should throw exception if trying to publish events.
        public TAggregate GetReadonlyCopy(Guid aggregateRootId) => GetReadonlyCopyOfVersion(aggregateRootId, int.MaxValue);

        public virtual TAggregate GetReadonlyCopyOfVersion(Guid aggregateRootId, int version) => _reader.GetReadonlyCopyOfVersion<TAggregate>(aggregateRootId, version);
    }
}

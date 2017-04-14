﻿using System;
using System.Collections.Generic;

namespace Composable.Persistence.EventStore
{
    interface IEventStoreSchemaManager
    {
        IEventTypeToIdMapper IdMapper { get; }
        void SetupSchemaIfDatabaseUnInitialized();
    }

    interface IEventStoreEventReader
    {
        IReadOnlyList<AggregateRootEvent> GetAggregateHistory(Guid aggregateId, bool takeWriteLock, int startAfterInsertedVersion = 0);
        IEnumerable<AggregateRootEvent> StreamEvents(int batchSize);
        IEnumerable<Guid> StreamAggregateIdsInCreationOrder(Type eventBaseType = null);
    }

    interface IEventStoreEventWriter
    {
        void Insert(IEnumerable<AggregateRootEvent> events);
        void InsertRefactoringEvents(IEnumerable<AggregateRootEvent> events);
        void DeleteAggregate(Guid aggregateId);
        void FixManualVersions(Guid aggregateId);
    }


    interface IEventstorePersistenceLayer
    {
        IEventStoreSchemaManager SchemaManager { get; }
        IEventStoreEventReader EventReader { get; }
        IEventStoreEventWriter EventWriter { get; }
    }
}
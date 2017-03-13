using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using Composable.Contracts;
using Composable.CQRS.CQRS.EventSourcing.Refactoring.Migrations;
using Composable.CQRS.CQRS.EventSourcing.Refactoring.Naming;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.Logging.Log4Net;
using Composable.System;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;
using log4net;

namespace Composable.CQRS.CQRS.EventSourcing.MicrosoftSQLServer
{
    partial class SqlServerEventStore : IEventStore
    {
        static readonly ILog Log = LogManager.GetLogger(typeof(SqlServerEventStore));

        public readonly string ConnectionString;
        readonly ISingleContextUseGuard _usageGuard;

        readonly SqlServerEventStoreEventReader _eventReader;
        readonly SqlServerEventStoreEventWriter _eventWriter;
        readonly SqlServerEventStoreEventsCache _cache;
        readonly SqlServerEventStoreSchemaManager _schemaManager;
        readonly IReadOnlyList<IEventMigration> _migrationFactories;

        readonly HashSet<Guid> _aggregatesWithEventsAddedByThisInstance = new HashSet<Guid>();

        public SqlServerEventStore(string connectionString, ISingleContextUseGuard usageGuard, IEventNameMapper nameMapper = null, IEnumerable<IEventMigration> migrations = null)
        {
            Log.Debug("Constructor called");

            _migrationFactories = migrations?.ToList() ?? new List<IEventMigration>();
            nameMapper = nameMapper ?? new DefaultEventNameMapper();

            ConnectionString = connectionString;
            _usageGuard = usageGuard;
            _cache = SqlServerEventStoreEventsCache.ForConnectionString(connectionString);
            var connectionMananger = new SqlServerEventStoreConnectionManager(connectionString);
            _schemaManager = new SqlServerEventStoreSchemaManager(connectionString, nameMapper);
            _eventReader = new SqlServerEventStoreEventReader(connectionMananger, _schemaManager);
            _eventWriter = new SqlServerEventStoreEventWriter(connectionMananger, _schemaManager);
        }

        public IEnumerable<IAggregateRootEvent> GetAggregateHistoryForUpdate(Guid aggregateId) => GetAggregateHistoryInternal(aggregateId: aggregateId, takeWriteLock: true);

        public IEnumerable<IAggregateRootEvent> GetAggregateHistory(Guid aggregateId) => GetAggregateHistoryInternal(aggregateId, takeWriteLock: false);

        IEnumerable<IAggregateRootEvent> GetAggregateHistoryInternal(Guid aggregateId, bool takeWriteLock)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            _schemaManager.SetupSchemaIfDatabaseUnInitialized();
            lock(AggregateLockManager.GetAggregateLockObject(aggregateId))
            {
                var cachedAggregateHistory = _cache.GetCopy(aggregateId);

                var newEventsFromDatabase = _eventReader.GetAggregateHistory(
                    aggregateId: aggregateId,
                    startAfterInsertedVersion: cachedAggregateHistory.MaxSeenInsertedVersion,
                    takeWriteLock: takeWriteLock);

                var containsRefactoringEvents = newEventsFromDatabase.Where(IsRefactoringEvent).Any();
                if(containsRefactoringEvents && cachedAggregateHistory.MaxSeenInsertedVersion > 0)
                {
                    _cache.Remove(aggregateId);
                    return GetAggregateHistoryInternal(aggregateId: aggregateId, takeWriteLock: takeWriteLock);
                }

                var currentHistory = cachedAggregateHistory.Events.Count == 0
                                                   ? SingleAggregateInstanceEventStreamMutator.MutateCompleteAggregateHistory(_migrationFactories, newEventsFromDatabase)
                                                   : cachedAggregateHistory.Events.Concat(newEventsFromDatabase).ToList();

                //Should within a transaction a process write events, read them, then fail to commit we will have cached events that are not persisted unless we refuse to cache them here.
                if (!_aggregatesWithEventsAddedByThisInstance.Contains(aggregateId))
                {
                    var maxSeenInsertedVersion = newEventsFromDatabase.Any()
                                                     ? newEventsFromDatabase.Max(@event => @event.InsertedVersion)
                                                     : cachedAggregateHistory.MaxSeenInsertedVersion;

                    _cache.Store(
                        aggregateId,
                        new SqlServerEventStoreEventsCache.Entry(events: currentHistory, maxSeenInsertedVersion: maxSeenInsertedVersion));
                }

                return currentHistory;
            }
        }

        static bool IsRefactoringEvent(AggregateRootEvent @event) => @event.InsertBefore.HasValue || @event.InsertAfter.HasValue || @event.Replaces.HasValue;

        public const int StreamEventsBatchSize = 10000;

        IEnumerable<IAggregateRootEvent> StreamEvents()
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            _schemaManager.SetupSchemaIfDatabaseUnInitialized();

            var streamMutator = CompleteEventStoreStreamMutator.Create(_migrationFactories);
            // ReSharper disable once InconsistentlySynchronizedField
            return streamMutator.Mutate(_eventReader.StreamEvents(StreamEventsBatchSize));
        }

        public void StreamEvents(int batchSize, Action<IReadOnlyList<IAggregateRootEvent>> handleEvents)
        {
            var batches = StreamEvents()
                .ChopIntoSizesOf(batchSize)
                .Select(batch => batch.ToList());
            foreach (var batch in batches)
            {
                handleEvents(batch);
            }
        }

        public void SaveEvents(IEnumerable<IAggregateRootEvent> events)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            _schemaManager.SetupSchemaIfDatabaseUnInitialized();
            events = events.ToList();
            var updatedAggregates = events.Select(@event => @event.AggregateRootId).Distinct();
            _aggregatesWithEventsAddedByThisInstance.AddRange(updatedAggregates);
            _eventWriter.Insert(events.Cast<AggregateRootEvent>());
            //todo: move this to the event store session.
            foreach (var aggregateId in updatedAggregates)
            {
                var completeAggregateHistory = _cache.GetCopy(aggregateId).Events.Concat(events.Where(@event => @event.AggregateRootId == aggregateId)).Cast<AggregateRootEvent>().ToArray();
                SingleAggregateInstanceEventStreamMutator.AssertMigrationsAreIdempotent(_migrationFactories, completeAggregateHistory);
            }
        }

        public void DeleteEvents(Guid aggregateId)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            _schemaManager.SetupSchemaIfDatabaseUnInitialized();
            _cache.Remove(aggregateId);
            _eventWriter.DeleteAggregate(aggregateId);
        }



        public void PersistMigrations()
        {
            this.Log().Warn("Starting to persist migrations");

            long migratedAggregates = 0;
            long updatedAggregates = 0;
            long newEventCount = 0;
            var logInterval = 1.Minutes();
            var lastLogTime = DateTime.Now;

            const int recoverableErrorRetriesToMake = 5;

            var aggregateIdsInCreationOrder = StreamAggregateIdsInCreationOrder().ToList();

            foreach (var aggregateId in aggregateIdsInCreationOrder)
            {
                try
                {
                    var succeeded = false;
                    int retries = 0;
                    while(!succeeded)
                    {
                        try
                        {
                            //todo: Look at batching the inserting of events in a way that let's us avoid taking a lock for a long time as we do now. This might be a problem in production.
                            using(var transaction = new TransactionScope(TransactionScopeOption.Required, scopeTimeout: 10.Minutes()))
                            {
                                lock(AggregateLockManager.GetAggregateLockObject(aggregateId))
                                {
                                    var updatedThisAggregate = false;
                                    var original = _eventReader.GetAggregateHistory(aggregateId: aggregateId, takeWriteLock: true).ToList();

                                    var startInsertingWithVersion = original.Max(@event => @event.InsertedVersion) + 1;

                                    var updatedAggregatesBeforeMigrationOfThisAggregate = updatedAggregates;

                                    SingleAggregateInstanceEventStreamMutator.MutateCompleteAggregateHistory(
                                        _migrationFactories,
                                        original,
                                        newEvents =>
                                        {
                                            //Make sure we don't try to insert into an occupied InsertedVersion
                                            newEvents.ForEach(@event => @event.InsertedVersion = startInsertingWithVersion++);
                                            //Save all new events so they get an InsertionOrder for the next refactoring to work with in case it acts relative to any of these events
                                            _eventWriter.InsertRefactoringEvents(newEvents);
                                            updatedAggregates = updatedAggregatesBeforeMigrationOfThisAggregate + 1;
                                            newEventCount += newEvents.Count;
                                            updatedThisAggregate = true;
                                        });

                                    if(updatedThisAggregate)
                                    {
                                        _eventWriter.FixManualVersions(aggregateId);
                                    }

                                    transaction.Complete();
                                    _cache.Remove(aggregateId);
                                }
                                migratedAggregates++;
                                succeeded = true;
                            }
                        }
                        catch(Exception e) when(IsRecoverableSqlException(e) && ++retries <= recoverableErrorRetriesToMake)
                        {
                            this.Log().Warn($"Failed to persist migrations for aggregate: {aggregateId}. Exception appers to be recoverable so running retry {retries} out of {recoverableErrorRetriesToMake}", e);
                        }
                    }
                }
                catch(Exception exception)
                {
                    this.Log().Error($"Failed to persist migrations for aggregate: {aggregateId}", exception: exception);
                }

                if(logInterval < DateTime.Now - lastLogTime)
                {
                    lastLogTime = DateTime.Now;
                    // ReSharper disable once AccessToModifiedClosure
                    Func<int> percentDone = () => (int)(((double)migratedAggregates / aggregateIdsInCreationOrder.Count) * 100);
                    this.Log().Info($"{percentDone()}% done. Inspected: {migratedAggregates} / {aggregateIdsInCreationOrder.Count}, Updated: {updatedAggregates}, New Events: {newEventCount}");
                }
            }

            this.Log().Warn("Done persisting migrations.");
            this.Log().Info($"Inspected: {migratedAggregates} , Updated: {updatedAggregates}, New Events: {newEventCount}");

        }

        static bool IsRecoverableSqlException(Exception exception)
        {
            var message = exception.Message.ToLower();
            return message.Contains("timeout") || message.Contains("deadlock");
        }

        public IEnumerable<Guid> StreamAggregateIdsInCreationOrder(Type eventBaseType = null)
        {
            Contract.Assert.That(eventBaseType == null || eventBaseType.IsInterface && typeof(IAggregateRootEvent).IsAssignableFrom(eventBaseType),
                "eventBaseType == null || eventBaseType.IsInterface && typeof(IAggregateRootEvent).IsAssignableFrom(eventBaseType)");
            _usageGuard.AssertNoContextChangeOccurred(this);

            _schemaManager.SetupSchemaIfDatabaseUnInitialized();

            // ReSharper disable once InconsistentlySynchronizedField
            return _eventReader.StreamAggregateIdsInCreationOrder(eventBaseType);
        }

        public void ClearCache()
        {
            _cache.Clear();
        }


        public void Dispose()
        {
        }

        public static void ClearAllCache()
        {
            SqlServerEventStoreSchemaManager.ClearAllCache();
            SqlServerEventStoreEventsCache.ClearAll();
        }
    }
}
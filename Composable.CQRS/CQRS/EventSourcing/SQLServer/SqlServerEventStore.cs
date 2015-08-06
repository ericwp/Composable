using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using Composable.System.Linq;
using log4net;

namespace Composable.CQRS.EventSourcing.SQLServer
{
    public class SqlServerEventStore : IEventStore
    {               
        private static readonly ILog Log = LogManager.GetLogger(typeof(SqlServerEventStore));        

        public readonly string ConnectionString;

        private readonly SqlServerEventStoreConnectionManager _connectionMananger;
        private readonly SqlServerEventStoreEventReader _eventReader;        
        private readonly SqlServerEventStoreEventWriter _eventWriter;
        private readonly SqlServerEventStoreEventsCache _cache;
        private readonly SqlServerEventStoreSchemaManager _schemaManager;

        public SqlServerEventStore(string connectionString)
        {
            Log.Debug("Constructor called");
            ConnectionString = connectionString;            
            var eventSerializer = new SqlServerEvestStoreEventSerializer();
            _schemaManager =  new SqlServerEventStoreSchemaManager(connectionString);
            _cache = SqlServerEventStoreEventsCache.ForConnectionString(connectionString);
            _connectionMananger = new SqlServerEventStoreConnectionManager(connectionString);
            _eventReader = new SqlServerEventStoreEventReader(_connectionMananger);
            _eventWriter = new SqlServerEventStoreEventWriter(_connectionMananger, eventSerializer);
        }


        public IEnumerable<IAggregateRootEvent> GetAggregateHistory(Guid aggregateId)
        {
            _schemaManager.SetupSchemaIfDatabaseUnInitialized();
            var cachedAggregateHistory = _cache.Get(aggregateId);

            cachedAggregateHistory.AddRange(
                _eventReader.GetAggregateHistory(
                    aggregateId: aggregateId,
                    startAtVersion: cachedAggregateHistory.Count,
                    suppressTransactionWarning: true));

            //Should within a transaction a process write events, read them, then fail to commit we will have cached events that are not persisted unless we refuse to cache them here.
            if(!_aggregatesWithEventsAddedByThisInstance.Contains(aggregateId))
            {
                _cache.Store(aggregateId, cachedAggregateHistory);
            }

            return cachedAggregateHistory;
        }

        private byte[] GetEventTimestamp(Guid eventId)
        {
            return _connectionMananger.UseCommand(
                loadCommand =>
                {
                    loadCommand.CommandText = $"SELECT {EventTable.Columns.SqlTimeStamp} FROM {EventTable.Name} WHERE {EventTable.Columns.EventId} = @{EventTable.Columns.EventId}";
                    loadCommand.Parameters.Add(new SqlParameter(EventTable.Columns.EventId, eventId));
                    return (byte[])loadCommand.ExecuteScalar();
                });
        }

        public const int StreamEventsAfterEventWithIdBatchSize = 10000;
       
        public IEnumerable<IAggregateRootEvent> StreamEventsAfterEventWithId(Guid? startAfterEventId)
        {
            _schemaManager.SetupSchemaIfDatabaseUnInitialized();

            return _eventReader.StreamEventsAfterEventWithId(startAfterEventId, StreamEventsAfterEventWithIdBatchSize);
        }


        private readonly HashSet<Guid> _aggregatesWithEventsAddedByThisInstance = new HashSet<Guid>(); 
        public void SaveEvents(IEnumerable<IAggregateRootEvent> events)
        {
            _schemaManager.SetupSchemaIfDatabaseUnInitialized();
            events = events.ToList();
            _aggregatesWithEventsAddedByThisInstance.AddRange(events.Select(e => e.AggregateRootId));
            _eventWriter.Insert(events);
        }

        public void DeleteEvents(Guid aggregateId)
        {
            _schemaManager.SetupSchemaIfDatabaseUnInitialized();
            _cache.Remove(aggregateId);
            _eventWriter.DeleteEvents(aggregateId);            
        }

        public IEnumerable<Guid> StreamAggregateIdsInCreationOrder()
        {
            _schemaManager.SetupSchemaIfDatabaseUnInitialized();

            using (var connection = _connectionMananger.OpenConnection())
            {
                using (var loadCommand = connection.CreateCommand())
                {
                    loadCommand.CommandText = $"SELECT {EventTable.Columns.AggregateId} FROM {EventTable.Name} WHERE {EventTable.Columns.AggregateVersion} = 1 ORDER BY {EventTable.Columns.SqlTimeStamp} ASC";

                    using (var reader = loadCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            yield return (Guid)reader[0];
                        }
                    }
                }
            }
        }        

        public static void ResetDB(string connectionString)
        {
            new SqlServerEventStoreSchemaManager(connectionString).ResetDB();
        }

        public void ResetDB()
        {
            _cache.Clear();
            _schemaManager.ResetDB();           
        }


        public void Dispose()
        {
        }
    }
}
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace Composable.CQRS.EventSourcing.SQLServer
{
    internal class SqlServerEventStoreEventReader
    {
        private readonly SqlServerEventStoreConnectionManager _connectionMananger;
        private readonly SqlServerEventStoreSchemaManager _schemaManager;
        public IEventTypeToIdMapper EventTypeToIdMapper => _schemaManager.IdMapper;

        public string SelectClause => InternalSelect();
        public string SelectTopClause(int top) => InternalSelect(top);

        private string InternalSelect(int? top = null)
        {
            var topClause = top.HasValue ? $"TOP {top.Value} " : "";
            return $@"
SELECT {topClause} {EventTable.Columns.EventType}, {EventTable.Columns.Event}, {EventTable.Columns.AggregateId}, {EventTable.Columns.AggregateVersion}, {EventTable.Columns.EventId}, {EventTable.Columns.TimeStamp} 
FROM {EventTable.Name} With(UPDLOCK, READCOMMITTED, ROWLOCK) ";
        }

        private static readonly SqlServerEvestStoreEventSerializer EventSerializer = new SqlServerEvestStoreEventSerializer();

        public SqlServerEventStoreEventReader(SqlServerEventStoreConnectionManager connectionManager, SqlServerEventStoreSchemaManager schemaManager)
        {
            _connectionMananger = connectionManager;
            _schemaManager = schemaManager;
        }

        public IAggregateRootEvent Read(SqlDataReader eventReader)
        {
            var @event = EventSerializer.Deserialize( eventType: EventTypeToIdMapper.GetType(eventReader.GetInt32(0)) , eventData: eventReader.GetString(1));
            @event.AggregateRootId = eventReader.GetGuid(2);
            @event.AggregateRootVersion = eventReader.GetInt32(3);
            @event.EventId = eventReader.GetGuid(4);
            @event.TimeStamp = eventReader.GetDateTime(5);

            return @event;
        }

        public IEnumerable<IAggregateRootEvent> GetAggregateHistory(Guid aggregateId, int startAfterVersion = 0, bool suppressTransactionWarning = false)
        {
            using(var connection = _connectionMananger.OpenConnection(suppressTransactionWarning: suppressTransactionWarning))
            {
                using (var loadCommand = connection.CreateCommand()) 
                {
                    loadCommand.CommandText = $"{SelectClause} WHERE {EventTable.Columns.AggregateId} = @{EventTable.Columns.AggregateId}";
                    loadCommand.Parameters.Add(new SqlParameter($"{EventTable.Columns.AggregateId}", aggregateId));

                    if (startAfterVersion > 0)
                    {
                        loadCommand.CommandText += $" AND {EventTable.Columns.AggregateVersion} > @CachedVersion";
                        loadCommand.Parameters.Add(new SqlParameter("CachedVersion", startAfterVersion));
                    }

                    loadCommand.CommandText += $" ORDER BY {EventTable.Columns.AggregateVersion} ASC";

                    using (var reader = loadCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            yield return Read(reader);
                        }
                    }
                }
            }
        }

        public IEnumerable<IAggregateRootEvent> StreamEventsAfterEventWithId(Guid? startAfterEventId, int batchSize)
        {
            using (var connection = _connectionMananger.OpenConnection())
            {
                var done = false;
                while (!done)
                {
                    using (var loadCommand = connection.CreateCommand())
                    {
                        if (startAfterEventId.HasValue)
                        {
                            loadCommand.CommandText = $"{SelectTopClause(batchSize)} WHERE {EventTable.Columns.InsertionOrder} > @{EventTable.Columns.InsertionOrder} {InsertionOrderSortOrder}";
                            loadCommand.Parameters.Add(new SqlParameter(EventTable.Columns.InsertionOrder, GetEventInsertionOrder(startAfterEventId.Value)));
                        }
                        else
                        {
                            loadCommand.CommandText = SelectTopClause(batchSize) + InsertionOrderSortOrder;
                        }

                        var fetchedInThisBatch = 0;
                        using (var reader = loadCommand.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var @event = Read(reader);
                                startAfterEventId = @event.EventId;
                                fetchedInThisBatch++;
                                yield return @event;
                            }
                        }
                        done = fetchedInThisBatch < batchSize;
                    }
                }
            }
        }

        private long GetEventInsertionOrder(Guid eventId)
        {
            return _connectionMananger.UseCommand(
                loadCommand =>
                {
                    loadCommand.CommandText = $"SELECT {EventTable.Columns.InsertionOrder} FROM {EventTable.Name} WHERE {EventTable.Columns.EventId} = @{EventTable.Columns.EventId}";
                    loadCommand.Parameters.Add(new SqlParameter(EventTable.Columns.EventId, eventId));
                    return (long)loadCommand.ExecuteScalar();
                });
        }

        public IEnumerable<Guid> StreamAggregateIdsInCreationOrder()
        {
            using (var connection = _connectionMananger.OpenConnection())
            {
                using (var loadCommand = connection.CreateCommand())
                {
                    loadCommand.CommandText = $"SELECT {EventTable.Columns.AggregateId} FROM {EventTable.Name} WHERE {EventTable.Columns.AggregateVersion} = 1 {InsertionOrderSortOrder}";

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


        private static readonly string InsertionOrderSortOrder = $" ORDER BY {EventTable.Columns.InsertionOrder} ASC";        
    }
}
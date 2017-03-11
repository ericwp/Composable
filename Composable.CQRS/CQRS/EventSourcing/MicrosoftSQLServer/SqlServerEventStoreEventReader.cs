using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using Composable.CQRS.EventSourcing;

namespace Composable.CQRS.CQRS.EventSourcing.MicrosoftSQLServer
{
    class SqlServerEventStoreEventReader
    {
        readonly SqlServerEventStoreConnectionManager _connectionMananger;
        readonly SqlServerEventStoreSchemaManager _schemaManager;
        IEventTypeToIdMapper EventTypeToIdMapper => _schemaManager.IdMapper;

        string GetSelectClause(bool takeWriteLock) => InternalSelect(takeWriteLock: takeWriteLock);
        string SelectTopClause(int top, bool takeWriteLock) => InternalSelect(top: top, takeWriteLock: takeWriteLock);

        string InternalSelect(bool takeWriteLock, int? top = null)
        {
            var topClause = top.HasValue ? $"TOP {top.Value} " : "";
            var lockHint = takeWriteLock ? "With(UPDLOCK, READCOMMITTED, ROWLOCK)" : "With(READCOMMITTED, ROWLOCK)";

            return $@"
SELECT {topClause} 
    {EventTable.Columns.EventType}, 
    {EventTable.Columns.Event}, 
    {EventTable.Columns.AggregateId}, 
    {EventTable.Columns.EffectiveVersion}, 
    {EventTable.Columns.EventId}, 
    {EventTable.Columns.UtcTimeStamp}, 
    {EventTable.Columns.InsertionOrder}, 
    {EventTable.Columns.InsertAfter}, 
    {EventTable.Columns.InsertBefore}, 
    {EventTable.Columns.Replaces}, 
    {EventTable.Columns.InsertedVersion}, 
    {EventTable.Columns.ManualVersion}, 
    {EventTable.Columns.EffectiveReadOrder}
FROM {EventTable.Name} {lockHint} ";
        }

        static readonly SqlServerEvestStoreEventSerializer EventSerializer = new SqlServerEvestStoreEventSerializer();

        public SqlServerEventStoreEventReader(SqlServerEventStoreConnectionManager connectionManager, SqlServerEventStoreSchemaManager schemaManager)
        {
            _connectionMananger = connectionManager;
            _schemaManager = schemaManager;
        }

        AggregateRootEvent HydrateEvent(EventDataRow eventDataRowRow)
        {
            var @event = (AggregateRootEvent)EventSerializer.Deserialize(eventType: EventTypeToIdMapper.GetType(eventDataRowRow.EventType), eventData: eventDataRowRow.EventJson);
            @event.AggregateRootId = eventDataRowRow.AggregateRootId;
            @event.AggregateRootVersion = eventDataRowRow.AggregateRootVersion;
            @event.EventId = eventDataRowRow.EventId;
            @event.UtcTimeStamp = eventDataRowRow.UtcTimeStamp;
            @event.InsertionOrder = eventDataRowRow.InsertionOrder;
            @event.InsertAfter = eventDataRowRow.InsertAfter;
            @event.InsertBefore = eventDataRowRow.InsertBefore;
            @event.Replaces = eventDataRowRow.Replaces;
            @event.InsertedVersion = eventDataRowRow.InsertedVersion;
            @event.ManualVersion = eventDataRowRow.ManualVersion;
            @event.EffectiveVersion = eventDataRowRow.EffectiveVersion;

            return @event;
        }

        EventDataRow ReadDataRow(SqlDataReader eventReader)
        {
            return new EventDataRow
                   {
                       EventJson = eventReader.GetString(1),
                       EventType = eventReader.GetInt32(0),
                       AggregateRootId = eventReader.GetGuid(2),
                       AggregateRootVersion = eventReader[3] as int? ?? eventReader.GetInt32(10),
                       EventId = eventReader.GetGuid(4),
                       UtcTimeStamp = DateTime.SpecifyKind(eventReader.GetDateTime(5), DateTimeKind.Utc),
                       //Without this the datetime will be DateTimeKind.Unspecified and will not convert correctly into Local time....
                       InsertionOrder = eventReader.GetInt64(6),
                       InsertAfter = eventReader[7] as long?,
                       InsertBefore = eventReader[8] as long?,
                       Replaces = eventReader[9] as long?,
                       InsertedVersion = eventReader.GetInt32(10),
                       ManualVersion = eventReader[11] as int?,
                       EffectiveVersion = eventReader[3] as int?
            };
        }

        class EventDataRow : AggregateRootEvent
        {
            public int EventType { get; set; }
            public string EventJson { get; set; }
        }

        public IReadOnlyList<AggregateRootEvent> GetAggregateHistory(Guid aggregateId, bool takeWriteLock, int startAfterInsertedVersion = 0)
        {
            var historyData = new List<EventDataRow>();
            using(var connection = _connectionMananger.OpenConnection(suppressTransactionWarning: !takeWriteLock))
            {
                using (var loadCommand = connection.CreateCommand())
                {
                    loadCommand.CommandText = $"{GetSelectClause(takeWriteLock)} WHERE {EventTable.Columns.AggregateId} = @{EventTable.Columns.AggregateId}";
                    loadCommand.Parameters.Add(new SqlParameter($"{EventTable.Columns.AggregateId}", aggregateId));

                    if (startAfterInsertedVersion > 0)
                    {
                        loadCommand.CommandText += $" AND {EventTable.Columns.InsertedVersion} > @CachedVersion";
                        loadCommand.Parameters.Add(new SqlParameter("CachedVersion", startAfterInsertedVersion ));
                    }

                    loadCommand.CommandText += $" ORDER BY {EventTable.Columns.EffectiveReadOrder} ASC";

                    using (var reader = loadCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var eventDataRow = ReadDataRow(reader);
                            if(eventDataRow.EffectiveVersion > 0)
                            {
                                historyData.Add(eventDataRow);
                            }
                        }
                    }
                }
            }

            return historyData.Select(HydrateEvent).ToList();
        }

        public IEnumerable<AggregateRootEvent> StreamEvents(int batchSize)
        {

            SqlDecimal lastReadEventReadOrder = 0;
            using(var connection = _connectionMananger.OpenConnection())
            {
                var done = false;
                while(!done)
                {
                    var historyData = new List<EventDataRow>();
                    using (var loadCommand = connection.CreateCommand())
                    {

                        loadCommand.CommandText = SelectTopClause(batchSize, takeWriteLock: false) + $"WHERE {EventTable.Columns.EffectiveReadOrder} > 0 AND {EventTable.Columns.EffectiveReadOrder}  > @{EventTable.Columns.EffectiveReadOrder}" + ReadSortOrder;

                        loadCommand.Parameters.Add(new SqlParameter(EventTable.Columns.EffectiveReadOrder, lastReadEventReadOrder));

                        var fetchedInThisBatch = 0;
                        using(var reader = loadCommand.ExecuteReader())
                        {
                            while(reader.Read())
                            {
                                historyData.Add(ReadDataRow(reader));
                                fetchedInThisBatch++;
                                lastReadEventReadOrder = reader.GetSqlDecimal(12);
                            }
                        }
                        done = fetchedInThisBatch < batchSize;
                    }

                    foreach(var eventDataRow in historyData)
                    {
                        yield return HydrateEvent(eventDataRow);
                    }
                }
            }
        }

        public IEnumerable<Guid> StreamAggregateIdsInCreationOrder(Type eventBaseType = null)
        {
            var ids = new List<Guid>();
            using (var connection = _connectionMananger.OpenConnection(suppressTransactionWarning:true))
            {
                using (var loadCommand = connection.CreateCommand())
                {
                    loadCommand.CommandText = $"SELECT {EventTable.Columns.AggregateId}, {EventTable.Columns.EventType} FROM {EventTable.Name} WHERE {EventTable.Columns.EffectiveVersion} = 1 AND {EventTable.Columns.EffectiveReadOrder} > 0 {ReadSortOrder}";

                    using (var reader = loadCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if(eventBaseType == null || eventBaseType.IsAssignableFrom(EventTypeToIdMapper.GetType(reader.GetInt32(1))))
                            {
                                ids.Add((Guid)reader[0]);
                            }
                        }
                    }
                }
            }
            return ids;
        }

        string ReadSortOrder => $" ORDER BY {EventTable.Columns.EffectiveReadOrder} ASC";
    }
}
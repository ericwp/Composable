using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Composable.System;

namespace Composable.CQRS.EventSourcing.SQLServer
{
    internal class SqlServerEventStoreEventWriter
    {
        private readonly SqlServerEventStoreConnectionManager _connectionMananger;
        private readonly SqlServerEvestStoreEventSerializer _eventSerializer;
        private IEventTypeToIdMapper IdMapper => _schemaManager.IdMapper;
        private readonly SqlServerEventStoreSchemaManager _schemaManager;

        public SqlServerEventStoreEventWriter(SqlServerEventStoreConnectionManager connectionMananger, SqlServerEvestStoreEventSerializer eventSerializer, SqlServerEventStoreSchemaManager schemaManager)
        {
            _connectionMananger = connectionMananger;
            _eventSerializer = eventSerializer;
            _schemaManager = schemaManager;
        }

        //Review:catch primary key violation errors and rethrow in an optimistic concurrency failure exception.: 
        public void Insert(IEnumerable<IAggregateRootEvent> events)
        {
            using(var connection = _connectionMananger.OpenConnection())
            {
                foreach(var @event in events)
                {
                    using(var command = connection.CreateCommand())
                    {
                        command.CommandType = CommandType.Text;

                        command.CommandText +=
                            $@"
INSERT {_schemaManager.EventTableName} With(READCOMMITTED, ROWLOCK) 
(       {EventTable.Columns.AggregateId},  {EventTable.Columns.InsertedVersion},  {EventTable.Columns.EventType},  {EventTable.Columns.EventId},  {EventTable.Columns.TimeStamp},  {EventTable.Columns.Event},  {EventTable.Columns.InsertAfter}, {EventTable.Columns.InsertBefore},  {EventTable.Columns.Replaces}) 
VALUES(@{EventTable.Columns.AggregateId}, @{EventTable.Columns.InsertedVersion}, @{EventTable.Columns.EventType}, @{EventTable.Columns.EventId}, @{EventTable.Columns.TimeStamp}, @{EventTable.Columns.Event}, @{EventTable.Columns.InsertAfter},@{EventTable.Columns.InsertBefore}, @{EventTable.Columns.Replaces})
SET @{EventTable.Columns.InsertionOrder} = SCOPE_IDENTITY();";

                        command.Parameters.Add(new SqlParameter(EventTable.Columns.AggregateId, @event.AggregateRootId));
                        command.Parameters.Add(new SqlParameter(EventTable.Columns.InsertedVersion, @event.AggregateRootVersion));
                        command.Parameters.Add(new SqlParameter(EventTable.Columns.EventType, IdMapper.GetId(@event.GetType())));
                        command.Parameters.Add(new SqlParameter(EventTable.Columns.EventId, @event.EventId));
                        command.Parameters.Add(new SqlParameter(EventTable.Columns.TimeStamp, @event.TimeStamp));

                        command.Parameters.Add(new SqlParameter(EventTable.Columns.Event, _eventSerializer.Serialize(@event)));

                        command.Parameters.Add(Nullable(new SqlParameter(EventTable.Columns.InsertAfter, @event.InsertAfter)));
                        command.Parameters.Add(Nullable(new SqlParameter(EventTable.Columns.InsertBefore, @event.InsertBefore)));
                        command.Parameters.Add(Nullable(new SqlParameter(EventTable.Columns.Replaces, @event.Replaces)));

                        var identityParameter = new SqlParameter(EventTable.Columns.InsertionOrder, SqlDbType.BigInt);
                        identityParameter.Direction = ParameterDirection.Output;

                        command.Parameters.Add(identityParameter);

                        command.ExecuteNonQuery();

                        ((AggregateRootEvent)@event).InsertionOrder = (long)identityParameter.Value;
                    }
                }
            }
        }

        private static SqlParameter Nullable(SqlParameter @this)
        {
            @this.IsNullable = true;
            @this.Direction = ParameterDirection.Input;
            if (@this.Value == null)
            {
                @this.Value = DBNull.Value;
            }
            return @this;
        }

        public void DeleteAggregate(Guid aggregateId)
        {
            _connectionMananger.UseCommand(
                command =>
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText +=
                        $"DELETE {_schemaManager.EventTableName} With(ROWLOCK) WHERE {EventTable.Columns.AggregateId} = @{EventTable.Columns.AggregateId}";
                    command.Parameters.Add(new SqlParameter(EventTable.Columns.AggregateId, aggregateId));
                    command.ExecuteNonQuery();
                });
        }
    }
}
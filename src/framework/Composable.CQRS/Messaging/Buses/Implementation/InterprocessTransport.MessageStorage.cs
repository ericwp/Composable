﻿using Composable.Contracts;
using Composable.NewtonSoft;
using Composable.Refactoring.Naming;
using Composable.System.Data.SqlClient;
using Composable.System.Linq;
using Newtonsoft.Json;

namespace Composable.Messaging.Buses.Implementation
{
    partial class InterprocessTransport
    {
        public partial class MessageStorage
        {
            readonly ISqlConnection _connectionFactory;
            readonly ITypeMapper _typeMapper;

            public MessageStorage(ISqlConnection connectionFactory, ITypeMapper typeMapper)
            {
                _connectionFactory = connectionFactory;
                _typeMapper = typeMapper;
            }

            public void SaveMessage(BusApi.Remotable.ExactlyOnce.IMessage message, params EndpointId[] receiverEndpointIds) =>
                _connectionFactory.UseCommand(
                    command =>
                    {
                        command
                            .SetCommandText(
                                $@"
INSERT {OutboxMessages.TableName} 
            ({OutboxMessages.MessageId},  {OutboxMessages.TypeIdGuidValue}, {OutboxMessages.Body}) 
    VALUES (@{OutboxMessages.MessageId}, @{OutboxMessages.TypeIdGuidValue}, @{OutboxMessages.Body})
")
                            .AddParameter(OutboxMessages.MessageId, message.MessageId)
                            .AddParameter(OutboxMessages.TypeIdGuidValue, _typeMapper.GetId(message.GetType()).GuidValue)
                            //todo: Like with the event store, keep all framework properties out of the JSON and put it into separate columns instead. For events. Reuse a pre-serialized instance from the persisting to the event store.
                            .AddNVarcharMaxParameter(OutboxMessages.Body, JsonConvert.SerializeObject(message, JsonSettings.JsonSerializerSettings));

                        receiverEndpointIds.ForEach(
                            (endpointId, index)
                                => command.AppendCommandText(
                                              $@"
INSERT {MessageDispatching.TableName} 
            ({MessageDispatching.MessageId},  {MessageDispatching.EndpointId},          {MessageDispatching.IsReceived}) 
    VALUES (@{MessageDispatching.MessageId}, @{MessageDispatching.EndpointId}_{index}, @{MessageDispatching.IsReceived})
")
                                          .AddParameter($"{MessageDispatching.EndpointId}_{index}", endpointId.GuidValue)
                                          .AddParameter(MessageDispatching.IsReceived, 0));

                        command.ExecuteNonQuery();
                    });

            internal void MarkAsReceived(TransportMessage.Response.Incoming response, EndpointId endpointId) =>
                 _connectionFactory.UseCommand(
                    command =>
                    {
                        var affectedRows = command
                                                 .SetCommandText(
                                                     $@"
UPDATE {MessageDispatching.TableName} 
    SET {MessageDispatching.IsReceived} = 1
WHERE {MessageDispatching.MessageId} = @{MessageDispatching.MessageId}
    AND {MessageDispatching.EndpointId} = @{MessageDispatching.EndpointId}
    AND {MessageDispatching.IsReceived} = 0
")
                                                 .AddParameter(MessageDispatching.MessageId, response.RespondingToMessageId)
                                                 .AddParameter(MessageDispatching.EndpointId, endpointId.GuidValue)
                                                 .ExecuteNonQuery();

                        Assert.Result.Assert(affectedRows == 1);
                        return affectedRows;
                    });

            public void Start() => SchemaManager.EnsureTablesExist(_connectionFactory);
        }
    }
}

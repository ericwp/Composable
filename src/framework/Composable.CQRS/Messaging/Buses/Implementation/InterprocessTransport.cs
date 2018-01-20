﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.GenericAbstractions.Time;
using Composable.Refactoring.Naming;
using Composable.System;
using Composable.System.Data.SqlClient;
using Composable.System.Linq;
using Composable.System.Threading.ResourceAccess;
using NetMQ;

namespace Composable.Messaging.Buses.Implementation
{
    partial class InterprocessTransport : IInterprocessTransport, IDisposable
    {
        class State
        {
            internal bool Running;
            public IGlobalBusStateTracker GlobalBusStateTracker;
            internal readonly Dictionary<EndpointId, ClientConnection> EndpointConnections = new Dictionary<EndpointId, ClientConnection>();
            internal HandlerStorage HandlerStorage;
            internal readonly NetMQPoller Poller = new NetMQPoller();
            public IUtcTimeTimeSource TimeSource { get; set; }
            public MessageStorage MessageStorage { get; set; }
            public ITypeMapper TypeMapper { get; set; }
        }

        readonly IThreadShared<State> _state = ThreadShared<State>.WithTimeout(10.Seconds());

        public InterprocessTransport(IGlobalBusStateTracker globalBusStateTracker, IUtcTimeTimeSource timeSource, ISqlConnection connectionFactory, ITypeMapper typeMapper) => _state.WithExclusiveAccess(@this =>
        {
            @this.HandlerStorage = new HandlerStorage(typeMapper);
            @this.TypeMapper = typeMapper;
            @this.MessageStorage = new MessageStorage(connectionFactory, typeMapper);
            @this.TimeSource = timeSource;
            @this.GlobalBusStateTracker = globalBusStateTracker;
        });

        public void Connect(IEndpoint endpoint) => _state.WithExclusiveAccess(@this =>
        {
            @this.EndpointConnections.Add(endpoint.Id, new ClientConnection(@this.GlobalBusStateTracker, endpoint, @this.Poller, @this.TimeSource, @this.MessageStorage, @this.TypeMapper));
            @this.HandlerStorage.AddRegistrations(endpoint.Id, endpoint.ServiceLocator.Resolve<IMessageHandlerRegistry>().HandledTypeIds());
        });

        public void Stop() => _state.WithExclusiveAccess(state =>
        {
            Contract.State.Assert(state.Running);
            state.Running = false;
            state.Poller.Dispose();
            state.EndpointConnections.Values.ForEach(socket => socket.Dispose());
        });

        public void Start() => _state.WithExclusiveAccess(@this =>
        {
            Contract.State.Assert(!@this.Running);
            @this.Running = true;
            @this.MessageStorage.Start();
            @this.Poller.RunAsync();
        });

        public void DispatchIfTransactionCommits(ITransactionalExactlyOnceDeliveryEvent @event) => _state.WithExclusiveAccess(state =>
        {
            var eventHandlerEndpointIds = state.HandlerStorage.GetEventHandlerEndpoints(@event);

            var connections = eventHandlerEndpointIds.Select(endpointId => state.EndpointConnections[endpointId]).ToArray();

            if(connections.Any())//Don't waste time persisting if there are no receivers
            {
                state.MessageStorage.SaveMessage(@event, eventHandlerEndpointIds.ToArray());
                connections.ForEach(receiver => receiver.DispatchIfTransactionCommits(@event));
            }
        });

        public void DispatchIfTransactionCommits(ITransactionalExactlyOnceDeliveryCommand command) => _state.WithExclusiveAccess(state =>
        {
            var endPointId = state.HandlerStorage.GetCommandHandlerEndpoint(command);
            var connection = state.EndpointConnections[endPointId];
            state.MessageStorage.SaveMessage(command, endPointId);
            connection.DispatchIfTransactionCommits(command);
        });

        public async Task<TCommandResult> DispatchIfTransactionCommitsAsync<TCommandResult>(ITransactionalExactlyOnceDeliveryCommand<TCommandResult> command) => await _state.WithExclusiveAccess(async state =>
        {
            var endPointId = state.HandlerStorage.GetCommandHandlerEndpoint(command);
            var connection = state.EndpointConnections[endPointId];

            state.MessageStorage.SaveMessage(command, endPointId);
            return await connection.DispatchIfTransactionCommitsAsync(command);
        });

        public async Task<TQueryResult> DispatchAsync<TQueryResult>(IQuery<TQueryResult> query) => await _state.WithExclusiveAccess(async state =>
        {
            var endPointId = state.HandlerStorage.GetQueryHandlerEndpoint(query);
            var connection = state.EndpointConnections[endPointId];
            return await connection.DispatchAsync(query);
        });

        public void Dispose() => _state.WithExclusiveAccess(state =>
        {
            if(state.Running)
            {
                Stop();
            }
        });
    }
}

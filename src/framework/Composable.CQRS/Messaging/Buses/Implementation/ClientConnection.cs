﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Transactions;
using Composable.Contracts;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.NetMQCE;
using Composable.System;
using Composable.System.Collections.Collections;
using Composable.System.Threading.ResourceAccess;
using Composable.SystemExtensions.TransactionsCE;
using NetMQ;
using NetMQ.Sockets;

namespace Composable.Messaging.Buses.Implementation
{
    class ClientConnection : IClientConnection
    {
        public void DispatchIfTransactionCommits(ITransactionalExactlyOnceDeliveryEvent @event) => Transaction.Current.OnCommit(() => _state.WithExclusiveAccess(state => DispatchMessage(state, TransportMessage.OutGoing.Create(@event))));

        public void DispatchIfTransactionCommits(ITransactionalExactlyOnceDeliveryCommand command) => Transaction.Current.OnCommit(() => _state.WithExclusiveAccess(state => DispatchMessage(state, TransportMessage.OutGoing.Create(command))));

        public async Task<TCommandResult> DispatchIfTransactionCommitsAsync<TCommandResult>(ITransactionalExactlyOnceDeliveryCommand<TCommandResult> command) => (TCommandResult)await _state.WithExclusiveAccess(async state =>
        {
            var taskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            var outGoingMessage = TransportMessage.OutGoing.Create(command);

            Transaction.Current.OnCommit(() => _state.WithExclusiveAccess(innerState =>
            {
                innerState.ExpectedResponseTasks.Add(outGoingMessage.MessageId, taskCompletionSource);
                DispatchMessage(innerState, outGoingMessage);
            }));

            Transaction.Current.OnAbort(() => taskCompletionSource.SetException(new TransactionAbortedException("Transaction aborted so command was never dispatched")));

            return await taskCompletionSource.Task;
        });

        public async Task<TQueryResult> DispatchAsync<TQueryResult>(IQuery<TQueryResult> query) => (TQueryResult)await _state.WithExclusiveAccess(state =>
        {
            var taskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

            var outGoingMessage = TransportMessage.OutGoing.Create(query);

            state.ExpectedResponseTasks.Add(outGoingMessage.MessageId, taskCompletionSource);
            state.GlobalBusStateTracker.SendingMessageOnTransport(outGoingMessage);
            state.DispatchQueue.Enqueue(outGoingMessage);

            return taskCompletionSource.Task;
        });

        static void DispatchMessage(State @this, TransportMessage.OutGoing outGoingMessage)
        {
            //todo: after transaction succeeds...
            @this.PendingDeliveryNotifications.Add(outGoingMessage.MessageId, @this.TimeSource.UtcNow);

            @this.GlobalBusStateTracker.SendingMessageOnTransport(outGoingMessage);
            @this.DispatchQueue.Enqueue(outGoingMessage);
        }

        public ClientConnection(IGlobalBusStateTracker globalBusStateTracker,
                                IEndpoint endpoint,
                                NetMQPoller poller,
                                IUtcTimeTimeSource timeSource,
                                InterprocessTransport.MessageStorage messageStorage)
        {
            _state.WithExclusiveAccess(state =>
            {
                state.TimeSource = timeSource;

                state.MessageStorage = messageStorage;

                state.GlobalBusStateTracker = globalBusStateTracker;

                state.Poller = poller;

                state.Poller.Add(state.DispatchQueue);

                state.DispatchQueue.ReceiveReady += DispatchQueuedMessages;

                state.Socket = new DealerSocket();

                //Should we screw up with the pipelining we prefer performance problems (memory usage) to lost messages or blocking
                state.Socket.Options.SendHighWatermark = int.MaxValue;
                state.Socket.Options.ReceiveHighWatermark = int.MaxValue;

                //We guarantee delivery upon restart in other ways. When we shut down, just do it.
                state.Socket.Options.Linger = 0.Milliseconds();

                state.Socket.ReceiveReady += ReceiveResponse;

                state.RemoteEndpointId = endpoint.Id;

                state.Socket.Connect(endpoint.Address);
                poller.Add(state.Socket);
            });
        }

        void DispatchQueuedMessages(object sender,NetMQQueueEventArgs<TransportMessage.OutGoing> netMQQueueEventArgs) => _state.WithExclusiveAccess(state =>
        {
            while(netMQQueueEventArgs.Queue.TryDequeue(out var message, TimeSpan.Zero))
            {
                state.Socket.Send(message);
            }
        });

        public void Dispose() => _state.WithExclusiveAccess(state =>
        {
            state.Socket.Dispose();
            state.DispatchQueue.Dispose();
        });

        class State
        {
            internal IGlobalBusStateTracker GlobalBusStateTracker;
            internal readonly Dictionary<Guid, TaskCompletionSource<object>> ExpectedResponseTasks = new Dictionary<Guid, TaskCompletionSource<object>>();
            internal readonly Dictionary<Guid, DateTime> PendingDeliveryNotifications = new Dictionary<Guid, DateTime>();
            internal DealerSocket Socket;
            internal NetMQPoller Poller;
            internal readonly NetMQQueue<TransportMessage.OutGoing> DispatchQueue = new NetMQQueue<TransportMessage.OutGoing>();
            internal IUtcTimeTimeSource TimeSource { get; set; }
            internal InterprocessTransport.MessageStorage MessageStorage { get; set; }
            public EndpointId RemoteEndpointId { get; set; }
        }

        readonly IThreadShared<State> _state = ThreadShared<State>.WithTimeout(10.Seconds());

        //Runs on poller thread so NO BLOCKING HERE!
        void ReceiveResponse(object sender, NetMQSocketEventArgs e)
        {
            var responseBatch = TransportMessage.Response.Incoming.ReceiveBatch(e.Socket, batchMaximum: 100);

            _state.WithExclusiveAccess(state =>
            {
                foreach(var response in responseBatch)
                {
                    switch(response.ResponseType)
                    {
                        case TransportMessage.Response.ResponseType.Success:
                            var successResponse = state.ExpectedResponseTasks.GetAndRemove(response.RespondingToMessageId);
                            Task.Run(() =>
                            {
                                try
                                {
                                    successResponse.SetResult(response.DeserializeResult());
                                }
                                catch(Exception exception)
                                {
                                    successResponse.SetException(exception);
                                }
                            });
                            break;
                        case TransportMessage.Response.ResponseType.Failure:
                            var failureResponse = state.ExpectedResponseTasks.GetAndRemove(response.RespondingToMessageId);
                            failureResponse.SetException(new MessageDispatchingFailedException());
                            break;
                        case TransportMessage.Response.ResponseType.Received:
                            Contract.Result.Assert(state.PendingDeliveryNotifications.Remove(response.RespondingToMessageId));
                            Task.Run(() => state.MessageStorage.MarkAsReceived(response, state.RemoteEndpointId));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            });
        }
    }
}

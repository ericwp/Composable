﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.System;
using Composable.System.Data.SqlClient;
using Composable.System.Linq;
using Composable.System.Threading;
using Composable.System.Threading.ResourceAccess;
using Composable.System.Transactions;
using NetMQ;
using NetMQ.Sockets;

namespace Composable.Messaging.Buses.Implementation
{
    partial class Inbox : IInbox, IDisposable
    {
        readonly IServiceLocator _serviceLocator;
        readonly IGlobalBusStateTracker _globalStateTracker;
        readonly IMessageHandlerRegistry _handlerRegistry;

        readonly IResourceGuard _resourceGuard = ResourceGuard.WithTimeout(1.Seconds());

        CancellationTokenSource _cancellationTokenSource;

        readonly IReadOnlyList<IMessageDispatchingRule> _dispatchingRules = new List<IMessageDispatchingRule>()
                                                                            {
                                                                                new QueriesExecuteAfterAllCommandsAndEventsAreDone(),
                                                                                new CommandsAndEventHandlersDoNotRunInParallelWithEachOtherInTheSameEndpoint()
                                                                            };
        bool _running;
        Thread _messagePumpThread;
        string _address;

        readonly NetMQQueue<TransportMessage.Response.Outgoing> _responseQueue = new NetMQQueue<TransportMessage.Response.Outgoing>();

        RouterSocket _responseSocket;

        public IReadOnlyList<Exception> ThrownExceptions => _globalStateTracker.GetExceptionsFor(this);
        NetMQPoller _poller;
        MessageStorage _storage;

        public Inbox(IServiceLocator serviceLocator, IGlobalBusStateTracker globalStateTracker, IMessageHandlerRegistry handlerRegistry, EndpointConfiguration configuration, ISqlConnection connectionFactory)
        {
            _address = configuration.Address;
            _serviceLocator = serviceLocator;
            _globalStateTracker = globalStateTracker;
            _handlerRegistry = handlerRegistry;
            _storage = new MessageStorage(connectionFactory);
        }

        public EndPointAddress Address => new EndPointAddress(_address);

        public void Start() => _resourceGuard.Update(action: () =>
        {
            Contract.Invariant.Assert(!_running);
            _running = true;

            _responseSocket = new RouterSocket();
            //Should we screw up with the pipelining we prefer performance problems (memory usage) to lost messages or blocking
            _responseSocket.Options.SendHighWatermark = int.MaxValue;
            _responseSocket.Options.ReceiveHighWatermark = int.MaxValue;

            //We guarantee delivery upon restart in other ways. When we shut down, just do it.
            _responseSocket.Options.Linger = 0.Milliseconds();

            _address = _responseSocket.BindAndReturnActualAddress(_address);
            _responseSocket.ReceiveReady += HandleIncomingMessage;

            _responseQueue.ReceiveReady += SendResponseMessage;

            _poller = new NetMQPoller() {_responseSocket, _responseQueue};
            _poller.RunAsync();

            _cancellationTokenSource = new CancellationTokenSource();
            _messagePumpThread = new Thread(MessagePumpThread)
                                 {
                                     Name = "_MessagePump",
                                     Priority = ThreadPriority.AboveNormal
                                 };
            _storage.Start();
            _messagePumpThread.Start();
        });

        void SendResponseMessage(object sender, NetMQQueueEventArgs<TransportMessage.Response.Outgoing> e)
        {
            while(e.Queue.TryDequeue(out var response, TimeSpan.Zero))
            {
                _responseSocket.Send(response);
            }
        }

        void HandleIncomingMessage(object sender, NetMQSocketEventArgs e)
        {
            Contract.Argument.Assert(e.IsReadyToReceive);
            var transportMessage = TransportMessage.InComing.Receive(_responseSocket);

            var dispatchTask = DispatchAsync(transportMessage);

            dispatchTask.ContinueWith(dispatchResult =>
            {
                var message = transportMessage.DeserializeMessageAndCacheForNextCall();
                if(message.RequiresResponse())
                {
                    if(dispatchResult.IsFaulted)
                    {
                        _responseQueue.Enqueue(transportMessage.CreateFailureResponse(dispatchResult.Exception));
                    } else if(dispatchResult.IsCompleted)
                    {
                        _responseQueue.Enqueue(transportMessage.CreateSuccessResponse(dispatchResult.Result));
                    }
                }
            });
        }

        public void Stop()
        {
            Contract.Invariant.Assert(_running);
            _running = false;
            _cancellationTokenSource.Cancel();
            _poller.Dispose();
            _responseSocket.Close();
            _responseSocket.Dispose();
            _messagePumpThread.InterruptAndJoin();
        }

        void MessagePumpThread()
        {
            while(!_cancellationTokenSource.Token.IsCancellationRequested)
                try
                {
                    var dispatchableMessage = _globalStateTracker.AwaitDispatchableMessage(this, _dispatchingRules);
                    dispatchableMessage.Run();
                }
                catch(Exception exception) when(exception is OperationCanceledException || exception is ThreadInterruptedException)
                {
                    return;
                }
        }

        void EnqueueTransactionalTask(TransportMessage.InComing message, Action action)
            => EnqueueNonTransactionalTask(message, action: () => TransactionScopeCe.Execute(action));

        void EnqueueNonTransactionalTask(TransportMessage.InComing message, Action action)
            => _globalStateTracker.EnqueueMessageTask(this, message, messageTask: () => _serviceLocator.ExecuteInIsolatedScope(action));

        async Task<object> DispatchAsync(TransportMessage.InComing message) => await Task.Run(async () =>
        {
            var innerMessage = message.DeserializeMessageAndCacheForNextCall();
            if(innerMessage is ITransactionalExactlyOnceDeliveryMessage)
            {
                _storage.SaveMessage(message);
                _responseQueue.Enqueue(message.CreatePersistedResponse());
            }

            switch(innerMessage)
            {
                case ITransactionalExactlyOnceDeliveryCommand command:
                    return await DispatchAsync(command, message);
                case ITransactionalExactlyOnceDeliveryEvent @event:
                    return await DispatchAsync(@event, message);
                case IQuery query:
                    return await DispatchAsync(query, message);
                default:
                    throw new Exception($"Unsupported message type: {message.GetType()}");
            }
        });

        async Task<object> DispatchAsync(IQuery query, TransportMessage.InComing message)
        {
            var handler = _handlerRegistry.GetQueryHandler(query.GetType());

            var taskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            using(_resourceGuard.AwaitUpdateLock())
            {
                EnqueueNonTransactionalTask(message,
                                            action: () =>
                                            {
                                                try
                                                {
                                                    var result = handler(query);
                                                    taskCompletionSource.SetResult(result);
                                                }
                                                catch(Exception exception)
                                                {
                                                    taskCompletionSource.SetException(exception);
                                                    throw;
                                                }
                                            });
            }
            return await taskCompletionSource.Task.NoMarshalling();
        }

        async Task<object> DispatchAsync(ITransactionalExactlyOnceDeliveryEvent @event, TransportMessage.InComing message)
        {
            var handler = _handlerRegistry.GetEventHandlers(@event.GetType());
            var taskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            using(_resourceGuard.AwaitUpdateLock())
            {
                EnqueueTransactionalTask(message,
                                         action: () =>
                                         {
                                             try
                                             {
                                                 handler.ForEach(action: @this => @this(@event));
                                                 _storage.MarkAsHandled(message);
                                                 taskCompletionSource.SetResult(result: null);
                                             }
                                             catch(Exception exception)
                                             {
                                                 taskCompletionSource.SetException(exception);
                                                 throw;
                                             }
                                         });
            }
            return await taskCompletionSource.Task.NoMarshalling();
        }

        async Task<object> DispatchAsync(ITransactionalExactlyOnceDeliveryCommand command, TransportMessage.InComing message)
        {
            var handler = _handlerRegistry.GetCommandHandler(command.GetType());

            var taskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            using(_resourceGuard.AwaitUpdateLock())
            {
                EnqueueTransactionalTask(message,
                                         action: () =>
                                         {
                                             try
                                             {
                                                 var result = handler(command);
                                                 _storage.MarkAsHandled(message);
                                                 taskCompletionSource.SetResult(result);
                                             }
                                             catch(Exception exception)
                                             {
                                                 taskCompletionSource.SetException(exception);
                                                 throw;
                                             }
                                         });
            }
            return await taskCompletionSource.Task.NoMarshalling();
        }

        public void Dispose()
        {
            if(_running)
                Stop();
        }
    }
}

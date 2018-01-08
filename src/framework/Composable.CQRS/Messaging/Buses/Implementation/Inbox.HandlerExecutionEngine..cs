﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Composable.DependencyInjection;
using Composable.System.Linq;
using Composable.System.Threading;
using Composable.System.Transactions;

namespace Composable.Messaging.Buses.Implementation
{
    partial class Inbox
    {
        partial class HandlerExecutionEngine
        {
            readonly IMessageHandlerRegistry _handlerRegistry;
            readonly IServiceLocator _serviceLocator;
            readonly MessageStorage _storage;
            readonly Thread _messagePumpThread;
            CancellationTokenSource _cancellationTokenSource;

            readonly IReadOnlyList<IMessageDispatchingRule> _dispatchingRules = new List<IMessageDispatchingRule>()
                                                                                {
                                                                                    new QueriesExecuteAfterAllCommandsAndEventsAreDone(),
                                                                                    new CommandsAndEventHandlersDoNotRunInParallelWithEachOtherInTheSameEndpoint()
                                                                                };
            readonly Coordinator _coordinator;

            public HandlerExecutionEngine(IGlobalBusStateTracker globalStateTracker,
                                          IMessageHandlerRegistry handlerRegistry,
                                          IServiceLocator serviceLocator,
                                          MessageStorage storage)
            {
                _handlerRegistry = handlerRegistry;
                _serviceLocator = serviceLocator;
                _storage = storage;
                _coordinator =  new Coordinator(globalStateTracker);

                _messagePumpThread = new Thread(AwaitDispatchableMessageThread)
                                     {
                                         Name = nameof(AwaitDispatchableMessageThread),
                                         Priority = ThreadPriority.AboveNormal
                                     };
            }


            internal async Task<object> Enqueue(TransportMessage.InComing message)
            {
                var innerMessage = message.DeserializeMessageAndCacheForNextCall();

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
            }

            void AwaitDispatchableMessageThread()
            {
                while(!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        var dispatchableMessage = _coordinator.AwaitDispatchableMessage(_dispatchingRules);
                        dispatchableMessage.Run();
                    }
                    catch(Exception exception) when(exception is OperationCanceledException || exception is ThreadInterruptedException)
                    {
                        return;
                    }
                }
            }

            async Task<object> DispatchAsync(IQuery query, TransportMessage.InComing message)
            {
                var handler = _handlerRegistry.GetQueryHandler(query.GetType());

                var taskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

                _coordinator.EnqueueMessageTask(message, messageTask: () => _serviceLocator.ExecuteInIsolatedScope(() =>
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
                }));

                return await taskCompletionSource.Task.NoMarshalling();
            }

            async Task<object> DispatchAsync(ITransactionalExactlyOnceDeliveryEvent @event, TransportMessage.InComing message)
            {
                var handler = _handlerRegistry.GetEventHandlers(@event.GetType());
                var taskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

                _coordinator.EnqueueMessageTask(message, messageTask: () => _serviceLocator.ExecuteTransactionInIsolatedScope(() => TransactionScopeCe.Execute(() =>
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
                })));

                return await taskCompletionSource.Task.NoMarshalling();
            }

            async Task<object> DispatchAsync(ITransactionalExactlyOnceDeliveryCommand command, TransportMessage.InComing message)
            {
                var handler = _handlerRegistry.GetCommandHandler(command.GetType());

                var taskCompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);

                _coordinator.EnqueueMessageTask(message, messageTask: () => _serviceLocator.ExecuteTransactionInIsolatedScope(() =>
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
                }));

                return await taskCompletionSource.Task.NoMarshalling();
            }

            public void Start()
            {
                _cancellationTokenSource = new CancellationTokenSource();
                _messagePumpThread.Start();
            }

            public void Stop()
            {
                _cancellationTokenSource.Cancel();
                _messagePumpThread.InterruptAndJoin();
            }
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Composable.DependencyInjection;
using Composable.System.Threading;
using Composable.System.Threading.ResourceAccess;

namespace Composable.Messaging.Buses.Implementation
{
    partial class Inbox
    {
        partial class HandlerExecutionEngine
        {
            partial class Coordinator
            {
                readonly ITaskRunner _taskRunner;
                readonly MessageStorage _messageStorage;
                readonly IServiceLocator _serviceLocator;
                readonly AwaitableOptimizedThreadShared<NonThreadsafeImplementation> _implementation;

                public Coordinator(IGlobalBusStateTracker globalStateTracker, ITaskRunner taskRunner, MessageStorage messageStorage, IServiceLocator serviceLocator)
                {
                    _taskRunner = taskRunner;
                    _messageStorage = messageStorage;
                    _serviceLocator = serviceLocator;
                    _implementation = new AwaitableOptimizedThreadShared<NonThreadsafeImplementation>(new NonThreadsafeImplementation(globalStateTracker));
                }

                internal QueuedHandlerExecutionTask AwaitDispatchableMessage(IReadOnlyList<IMessageDispatchingRule> dispatchingRules)
                {
                    QueuedHandlerExecutionTask message = null;
                    _implementation.Await(implementation => implementation.TryGetDispatchableMessage(dispatchingRules, out message));
                    return message;
                }

                public Task<object> EnqueueMessageTask(TransportMessage.InComing message, Func<object, object> messageTask) => _implementation.Update(implementation =>
                {
                    var inflightMessage = new QueuedHandlerExecutionTask(message, this, messageTask, _taskRunner, _messageStorage, _serviceLocator);
                    implementation.EnqueueMessageTask(inflightMessage);
                    return inflightMessage._taskCompletionSource.Task;
                });

                void Succeeded(QueuedHandlerExecutionTask queuedMessageInformation) => _implementation.Update(implementation => implementation.Succeeded(queuedMessageInformation));

                void Failed(QueuedHandlerExecutionTask queuedMessageInformation, Exception exception) => _implementation.Update(implementation => implementation.Failed(queuedMessageInformation, exception));

                class NonThreadsafeImplementation : IExecutingMessagesSnapshot
                {
                    const int MaxConcurrentlyExecutingHandlers = 20;
                    readonly IGlobalBusStateTracker _globalStateTracker;


                    //performance: Split waiting messages into prioritized categories: Exactly once event/command, At most once event/command,  NonTransactional query
                    //don't postpone checking if mutations are allowed to run because we have a ton of queries queued up. Also the queries are likely not allowed to run due to the commands and events!
                    //performance: Use static type caching trick to ensure that we know which rules need to be applied to which messages. Don't check rules that don't apply. (Double dispatching might be required.)
                    public IReadOnlyList<TransportMessage.InComing> AtMostOnceCommands => _executingAtMostOnceCommands;
                    public IReadOnlyList<TransportMessage.InComing> ExactlyOnceCommands => _executingExactlyOnceCommands;
                    public IReadOnlyList<TransportMessage.InComing> ExactlyOnceEvents => _executingExactlyOnceEvents;
                    public IReadOnlyList<TransportMessage.InComing> ExecutingNonTransactionalQueries => _executingNonTransactionalQueries;

                    readonly List<QueuedHandlerExecutionTask> _messagesWaitingToExecute = new List<QueuedHandlerExecutionTask>();
                    public NonThreadsafeImplementation(IGlobalBusStateTracker globalStateTracker) => _globalStateTracker = globalStateTracker;

                    internal bool TryGetDispatchableMessage(IReadOnlyList<IMessageDispatchingRule> dispatchingRules, out QueuedHandlerExecutionTask dispatchable)
                    {
                        dispatchable = null;
                        if(_executingMessages >= MaxConcurrentlyExecutingHandlers)
                        {
                            return false;
                        }

                        dispatchable = _messagesWaitingToExecute
                           .FirstOrDefault(queuedTask => dispatchingRules.All(rule => rule.CanBeDispatched(this, queuedTask.TransportMessage)));

                        if (dispatchable == null)
                        {
                            return false;
                        }

                        Dispatching(dispatchable);
                        return true;
                    }

                    public void EnqueueMessageTask(QueuedHandlerExecutionTask message) => _messagesWaitingToExecute.Add(message);

                    internal void Succeeded(QueuedHandlerExecutionTask queuedMessageInformation) => DoneDispatching(queuedMessageInformation);

                    internal void Failed(QueuedHandlerExecutionTask queuedMessageInformation, Exception exception) => DoneDispatching(queuedMessageInformation, exception);


                    void Dispatching(QueuedHandlerExecutionTask dispatchable)
                    {
                        _executingMessages++;

                        switch(dispatchable.TransportMessage.MessageTypeEnum)
                        {
                            case TransportMessage.TransportMessageType.ExactlyOnceEvent:
                                _executingExactlyOnceEvents.Add(dispatchable.TransportMessage);
                                break;
                            case TransportMessage.TransportMessageType.AtMostOnceCommand:
                                _executingAtMostOnceCommands.Add(dispatchable.TransportMessage);
                                break;
                            case TransportMessage.TransportMessageType.ExactlyOnceCommand:
                                _executingExactlyOnceCommands.Add(dispatchable.TransportMessage);
                                break;
                            case TransportMessage.TransportMessageType.NonTransactionalQuery:
                                _executingNonTransactionalQueries.Add(dispatchable.TransportMessage);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        _messagesWaitingToExecute.Remove(dispatchable);
                    }

                    void DoneDispatching(QueuedHandlerExecutionTask doneExecuting, Exception exception = null)
                    {
                        _executingMessages--;

                        switch(doneExecuting.TransportMessage.MessageTypeEnum)
                        {
                            case TransportMessage.TransportMessageType.ExactlyOnceEvent:
                                _executingExactlyOnceEvents.Remove(doneExecuting.TransportMessage);
                                break;
                            case TransportMessage.TransportMessageType.AtMostOnceCommand:
                                _executingAtMostOnceCommands.Remove(doneExecuting.TransportMessage);
                                break;
                            case TransportMessage.TransportMessageType.ExactlyOnceCommand:
                                _executingExactlyOnceCommands.Remove(doneExecuting.TransportMessage);
                                break;
                            case TransportMessage.TransportMessageType.NonTransactionalQuery:
                                _executingNonTransactionalQueries.Remove(doneExecuting.TransportMessage);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        _globalStateTracker.DoneWith(doneExecuting.MessageId, exception);
                    }

                    int _executingMessages;
                    readonly List<TransportMessage.InComing> _executingExactlyOnceCommands = new List<TransportMessage.InComing>();
                    readonly List<TransportMessage.InComing> _executingAtMostOnceCommands = new List<TransportMessage.InComing>();
                    readonly List<TransportMessage.InComing> _executingExactlyOnceEvents = new List<TransportMessage.InComing>();
                    readonly List<TransportMessage.InComing> _executingNonTransactionalQueries = new List<TransportMessage.InComing>();
                }
            }
        }
    }
}

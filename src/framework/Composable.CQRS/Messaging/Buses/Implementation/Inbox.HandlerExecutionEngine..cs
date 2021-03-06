﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Composable.DependencyInjection;
using Composable.System.Linq;
using Composable.System.Threading;

namespace Composable.Messaging.Buses.Implementation
{
    partial class Inbox
    {
        partial class HandlerExecutionEngine
        {
            Thread _awaitDispatchableMessageThread;

            readonly IReadOnlyList<IMessageDispatchingRule> _dispatchingRules = new List<IMessageDispatchingRule>()
                                                                                {
                                                                                    new QueriesExecuteAfterAllCommandsAndEventsAreDone(),
                                                                                    new CommandsAndEventHandlersDoNotRunInParallelWithEachOtherInTheSameEndpoint()
                                                                                };
            readonly Coordinator _coordinator;

            public HandlerExecutionEngine(IGlobalBusStateTracker globalStateTracker,
                                          IMessageHandlerRegistry handlerRegistry,
                                          IServiceLocator serviceLocator,
                                          MessageStorage storage,
                                          ITaskRunner taskRunner) =>
                _coordinator = new Coordinator(globalStateTracker, taskRunner, storage, serviceLocator, handlerRegistry);

            internal Task<object> Enqueue(TransportMessage.InComing transportMessage) => _coordinator.EnqueueMessageTask(transportMessage);

            void AwaitDispatchableMessageThread()
            {
                try
                {
                    while(true)
                    {
                        var task = _coordinator.AwaitExecutableHandlerExecutionTask(_dispatchingRules);
                        task.Execute();
                    }
                }
                catch(Exception exception) when(exception is OperationCanceledException || exception is ThreadInterruptedException) {}
            }

            public void Start()
            {
                _awaitDispatchableMessageThread = new Thread(AwaitDispatchableMessageThread)
                                                  {
                                                      Name = nameof(AwaitDispatchableMessageThread),
                                                      Priority = ThreadPriority.AboveNormal
                                                  };
                _awaitDispatchableMessageThread.Start();
            }

            public void Stop()
            {
                _awaitDispatchableMessageThread.InterruptAndJoin();
                _awaitDispatchableMessageThread = null;
            }
        }
    }
}

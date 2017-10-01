﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.GenericAbstractions.Time;
using Composable.System;
using Composable.System.Threading;
using Composable.System.Threading.ResourceAccess;
using Composable.System.Transactions;

namespace Composable.Messaging.Buses
{
    class ServiceBus : IServiceBus
    {
        readonly string _name;
        readonly IUtcTimeTimeSource _timeSource;
        readonly IInProcessServiceBus _inProcessServiceBus;
        readonly IGlobalBusStrateTracker _globalStateTracker;
        readonly List<ScheduledCommand> _scheduledMessages = new List<ScheduledCommand>();

        readonly IExclusiveResourceAccessGuard _guard = ResourceAccessGuard.ExclusiveWithTimeout(1.Seconds());

        readonly CancellationTokenSource _cancellationTokenSource;

        readonly IReadOnlyList<IMessageDispatchingRule> _dispatchingRules = new List<IMessageDispatchingRule>()
                                                                            {
                                                                                new QueriesExecuteAfterAllCommandsAndEventsAreDone(),
                                                                                new CommandsAndEventHandlersDoNotRunInParallelWithEachOtherInTheSameEndpoint()
                                                                            };
        bool _running;
        readonly Thread _messagePumpThread;

        readonly Timer _scheduledMessagesTimer;

        public IReadOnlyList<Exception> ThrownExceptions => _globalStateTracker.GetExceptionsFor(this);

        public ServiceBus(string name, IUtcTimeTimeSource timeSource, IInProcessServiceBus inProcessServiceBus, IGlobalBusStrateTracker globalStateTracker)
        {
            _name = name;
            _timeSource = timeSource;
            _cancellationTokenSource = new CancellationTokenSource();
            _inProcessServiceBus = inProcessServiceBus;
            _globalStateTracker = globalStateTracker;

            _messagePumpThread = new Thread(MessagePumpThread)
                                 {
                                     Name = $"{_name}_MessagePump"
                                 };

            _scheduledMessagesTimer = new Timer(_ => SendDueMessages(), null, 0.Seconds(), 100.Milliseconds());
        }

        public void Start()
        {
            Contract.Assert.That(!_running, message: "!_running");
            _running = true;
            _messagePumpThread.Start();
        }

        public void Stop()
        {
            Contract.Assert.That(_running, message: "_running");
            _running = false;
            _scheduledMessagesTimer.Dispose();
            _cancellationTokenSource.Cancel();
            _messagePumpThread.Interrupt();
            _messagePumpThread.Join();
        }

        public void SendAtTime(DateTime sendAt, ICommand message)
        {
            using(_guard.AwaitExclusiveLock())
            {
                if(_timeSource.UtcNow > sendAt.ToUniversalTime())
                    throw new InvalidOperationException(message: "You cannot schedule a queuedMessageInformation to be sent in the past.");

                _scheduledMessages.Add(new ScheduledCommand(sendAt, message));
            }
        }

        public void Send(ICommand command) => EnqueueTransactionalTask(command, () => _inProcessServiceBus.Send(command));

        public void Publish(IEvent anEvent) => EnqueueTransactionalTask(anEvent, () => _inProcessServiceBus.Publish(anEvent));

        public async Task<TResult> SendAsync<TResult>(ICommand<TResult> command) where TResult : IMessage
        {
            var taskCompletionSource = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            EnqueueTransactionalTask(command, () => taskCompletionSource.SetResult(_inProcessServiceBus.Send(command)));
            return await taskCompletionSource.Task.IgnoreSynchronizationContext();
        }

        public async Task<TResult> QueryAsync<TResult>(IQuery<TResult> query) where TResult : IQueryResult
        {
            TaskCompletionSource<TResult> taskCompletionSource = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            EnqueueNonTransactionalTask(query, () => taskCompletionSource.SetResult(_inProcessServiceBus.Get(query)));
            return await taskCompletionSource.Task.IgnoreSynchronizationContext();
        }

        public TResult Query<TResult>(IQuery<TResult> query) where TResult : IQueryResult => QueryAsync(query).Result;

        void EnqueueTransactionalTask(IMessage message, Action action) { EnqueueNonTransactionalTask(message, () => TransactionScopeCe.Execute(action)); }

        void EnqueueNonTransactionalTask(IMessage message, Action action)
            => _globalStateTracker.EnqueueMessageTask(this, message, messageTask: action);

        void SendDueMessages()
            => _guard.ExecuteWithResourceExclusivelyLocked(() =>
            {
                var dueMessages = _scheduledMessages.Where(predicate: message => message.SendAt <= _timeSource.UtcNow)
                                                    .ToList();
                dueMessages.ForEach(action: scheduledCommand => Send(scheduledCommand.Command));
                dueMessages.ForEach(action: message => _scheduledMessages.Remove(message));
            });

        void MessagePumpThread()
        {
            while(!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    _globalStateTracker.AwaitDispatchableMessage(this, _dispatchingRules).Run();
                }
                catch(Exception exception) when(exception is OperationCanceledException || exception is ThreadInterruptedException)
                {
                    return;
                }
            }
        }

        class ScheduledCommand
        {
            public DateTime SendAt { get; }
            public ICommand Command { get; }

            public ScheduledCommand(DateTime sendAt, ICommand command)
            {
                SendAt = sendAt.SafeToUniversalTime();
                Command = command;
            }
        }


        public override string ToString() => _name;

        public void Dispose()
        {
            if(_running)
            {
                Stop();
            }
        }
    }
}

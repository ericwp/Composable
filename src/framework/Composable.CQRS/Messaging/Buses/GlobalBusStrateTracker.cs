﻿using System;
using System.Collections.Generic;
using System.Linq;
using Composable.System;
using Composable.System.Linq;
using Composable.System.Threading.ResourceAccess;
using JetBrains.Annotations;

namespace Composable.Messaging.Buses
{
    class GlobalBusStrateTracker : IGlobalBusStrateTracker
    {
        readonly List<IInflightMessage> _inflightMessages = new List<IInflightMessage>();

        //It is never OK for this class to block. So make that explicit with a really strict timeout on all operations waiting for access.
        readonly IExclusiveResourceAccessGuard _guard = ResourceAccessGuard.ExclusiveWithTimeout(10.Milliseconds());

        public IExclusiveResourceAccessGuard ResourceGuard => _guard;

        public IGlobalBusStateSnapshot CreateSnapshot()
            => _guard.ExecuteWithResourceExclusivelyLocked(
                () => new GlobalBusStateSnapshot(_inflightMessages.ToList()));

        public IMessageDispatchingTracker QueuedMessage(IMessage message, [CanBeNull] IMessage triggeringMessage)
            => _guard.ExecuteWithResourceExclusivelyLockedAndNotifyWaitingThreadsAboutUpdate(
                () =>
                {
                    var inflightMessage = new InflightMessage(message, triggeringMessage, this);
                    _inflightMessages.Add(inflightMessage);
                    return inflightMessage;
                });

        public void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride)
            => _guard.ExecuteWithResourceExclusivelyLockedWhen(
                timeout: timeoutOverride ?? 30.Seconds(),
                condition: () => _inflightMessages.None(),
                action: () => {});

        void DoneWith(IInflightMessage message) => _guard.ExecuteWithResourceExclusivelyLockedAndNotifyWaitingThreadsAboutUpdate(() => _inflightMessages.Remove(message));

        class GlobalBusStateSnapshot : IGlobalBusStateSnapshot
        {
            public GlobalBusStateSnapshot(IReadOnlyList<IInflightMessage> inflightMessages) => InflightMessages = inflightMessages;
            public IReadOnlyList<IInflightMessage> InflightMessages { get; }
        }

        class InflightMessage : IInflightMessage, IMessageDispatchingTracker
        {
            readonly GlobalBusStrateTracker _globalBusStrateTracker;
            public InflightMessage(IMessage message, IMessage triggeringMessage, GlobalBusStrateTracker globalBusStrateTracker)
            {
                _globalBusStrateTracker = globalBusStrateTracker;
                Message = message;
                TriggeringMessage = triggeringMessage;
            }
            public IMessage Message { get; }
            public IMessage TriggeringMessage { get; }

            public void Succeeded() => _globalBusStrateTracker.DoneWith(this);

            public void Failed() => _globalBusStrateTracker.DoneWith(this);
        }
    }
}

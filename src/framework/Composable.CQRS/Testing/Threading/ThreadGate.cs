﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.System.Threading.ResourceAccess;

namespace Composable.Testing.Threading
{
    class ThreadGate : IThreadGate
    {
        public static IThreadGate CreateClosedWithTimeout(TimeSpan timeout) => new ThreadGate(timeout);
        public static IThreadGate CreateOpenWithTimeout(TimeSpan timeout) => new ThreadGate(timeout).Open();

        public TimeSpan DefaultTimeout => _defaultTimeout;
        public bool IsOpen => _isOpen;
        public long Queued => _resourceGuard.ExecuteWithResourceExclusivelyLocked(() => _queuedThreads.Count);
        public long Passed => _resourceGuard.ExecuteWithResourceExclusivelyLocked(() => _passedThreads.Count);
        public long Requested => _resourceGuard.ExecuteWithResourceExclusivelyLocked(() => _requestsThreads.Count);

        public IReadOnlyList<ThreadSnapshot> RequestedThreads => _resourceGuard.ExecuteWithResourceExclusivelyLocked(() => _requestsThreads.ToList());
        public IReadOnlyList<ThreadSnapshot> QueuedThreads => _resourceGuard.ExecuteWithResourceExclusivelyLocked(() => _queuedThreads.ToList());
        public IReadOnlyList<ThreadSnapshot> PassedThrough => _resourceGuard.ExecuteWithResourceExclusivelyLocked(() => _passedThreads.ToList());
        public Action<ThreadSnapshot> PassThroughAction => _resourceGuard.ExecuteWithResourceExclusivelyLocked(() => _passThroughAction);

        public IThreadGate Open()
        {
            using(var ownedLock = _resourceGuard.AwaitExclusiveLock())
            {
                _isOpen = true;
                _lockOnNextPass = false;
                ownedLock.SendUpdateNotificationToAllThreadsAwaitingUpdateNotification();
            }
            return this;
        }

        public IThreadGate AwaitLetOneThreadPassthrough()
        {
            using(var ownedLock = _resourceGuard.AwaitExclusiveLock())
            {
                Contract.Assert.That(!_isOpen, "Gate must be closed to call this method.");
                _isOpen = true;
                _lockOnNextPass = true;
                ownedLock.SendUpdateNotificationToAllThreadsAwaitingUpdateNotification();
                return this.AwaitClosed();
            }
        }

        public bool TryAwait(TimeSpan timeout, Func<bool> condition) => _resourceGuard.TryAwait(timeout, condition);

        public IThreadGate SetPassThroughAction(Action<ThreadSnapshot> action) => _resourceGuard.ExecuteWithResourceExclusivelyLockedAndReturn(() => _passThroughAction = action, this);

        public IThreadGate ExecuteWithExclusiveLockWhen(TimeSpan timeout, Func<bool> condition, Action<IThreadGate, IExclusiveResourceLock> action)
        {
            using(var ownedLock = _resourceGuard.AwaitExclusiveLockWhen(timeout, condition))
            {
                action(this, ownedLock);
            }
            return this;
        }

        public IThreadGate Close()
        {
            _resourceGuard.ExecuteWithResourceExclusivelyLocked(() => _isOpen = false);
            return this;
        }

        public void AwaitPassthrough() => AwaitPassthrough(_defaultTimeout);

        public void AwaitPassthrough(TimeSpan timeout)
        {
            using(var ownedLock = _resourceGuard.AwaitExclusiveLock(_defaultTimeout))
            {
                var currentThread = new ThreadSnapshot();
                _requestsThreads.Add(currentThread);
                _queuedThreads.AddLast(currentThread);
                ownedLock.SendUpdateNotificationToAllThreadsAwaitingUpdateNotification();
                while(!_isOpen)
                {
                    ownedLock.ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(_defaultTimeout);
                }

                if(_lockOnNextPass)
                {
                    _lockOnNextPass = false;
                    _isOpen = false;
                }

                _queuedThreads.Remove(currentThread);
                _passedThreads.Add(currentThread);
                ownedLock.SendUpdateNotificationToAllThreadsAwaitingUpdateNotification();
                _passThroughAction?.Invoke(currentThread);
            }
        }

        ThreadGate(TimeSpan defaultTimeout)
        {
            _resourceGuard = ResourceAccessGuard.ExclusiveWithTimeout(defaultTimeout);
            _defaultTimeout = defaultTimeout;
        }

        readonly TimeSpan _defaultTimeout;
        readonly IExclusiveResourceAccessGuard _resourceGuard;
        bool _lockOnNextPass;
        Action<ThreadSnapshot> _passThroughAction;
        bool _isOpen;
        readonly List<ThreadSnapshot> _requestsThreads = new List<ThreadSnapshot>();
        readonly LinkedList<ThreadSnapshot> _queuedThreads = new LinkedList<ThreadSnapshot>();
        readonly List<ThreadSnapshot> _passedThreads = new List<ThreadSnapshot>();
    }
}

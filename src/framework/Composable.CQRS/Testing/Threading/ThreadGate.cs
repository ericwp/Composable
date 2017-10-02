﻿using System;
using System.Collections.Generic;
using System.Linq;
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
        public long Queued => _guardedResource.Read(() => _queuedThreads.Count);
        public long Passed => _guardedResource.Read(() => _passedThreads.Count);
        public long Requested => _guardedResource.Read(() => _requestsThreads.Count);

        public IReadOnlyList<ThreadSnapshot> RequestedThreads => _guardedResource.Read(() => _requestsThreads.ToList());
        public IReadOnlyList<ThreadSnapshot> QueuedThreads => _guardedResource.Read(() => _queuedThreads.ToList());
        public IReadOnlyList<ThreadSnapshot> PassedThrough => _guardedResource.Read(() => _passedThreads.ToList());
        public Action<ThreadSnapshot> PassThroughAction => _guardedResource.Read(() => _passThroughAction);

        public IThreadGate Open()
        {
            using(var ownedLock = _guardedResource.AwaitExclusiveLock())
            {
                _isOpen = true;
                _lockOnNextPass = false;
                ownedLock.NotifyWaitingThreadsAboutUpdate();
            }
            return this;
        }

        public IThreadGate AwaitLetOneThreadPassthrough()
        {
            using(var ownedLock = _guardedResource.AwaitExclusiveLock())
            {
                Contract.Assert.That(!_isOpen, "Gate must be closed to call this method.");
                _isOpen = true;
                _lockOnNextPass = true;
                ownedLock.NotifyWaitingThreadsAboutUpdate();
                return this.AwaitClosed();
            }
        }

        public bool TryAwait(TimeSpan timeout, Func<bool> condition) => _guardedResource.TryAwaitCondition(timeout, condition);

        public IThreadGate SetPassThroughAction(Action<ThreadSnapshot> action) => _guardedResource.UpdateAndReturn(() => _passThroughAction = action, this);

        public IThreadGate ExecuteWithExclusiveLockWhen(TimeSpan timeout, Func<bool> condition, Action action)
        {
            using (_guardedResource.AwaitUpdateLockWhen(timeout, condition))
            {
                action();
            }
            return this;
        }

        public IThreadGate Close()
        {
            _guardedResource.Update(() => _isOpen = false);
            return this;
        }

        public void AwaitPassthrough() => AwaitPassthrough(_defaultTimeout);

        public void AwaitPassthrough(TimeSpan timeout)
        {
            var currentThread = new ThreadSnapshot();

            _guardedResource.Update(() =>
            {
                _requestsThreads.Add(currentThread);
                _queuedThreads.AddLast(currentThread);
            });


            using(_guardedResource.AwaitUpdateLockWhen(() => _isOpen))
            {
                if(_lockOnNextPass)
                {
                    _lockOnNextPass = false;
                    _isOpen = false;
                }

                _queuedThreads.Remove(currentThread);
                _passedThreads.Add(currentThread);
                _passThroughAction?.Invoke(currentThread);
            }
        }

        ThreadGate(TimeSpan defaultTimeout)
        {
            _guardedResource = GuardedResource.WithTimeout(defaultTimeout);
            _defaultTimeout = defaultTimeout;
        }

        readonly TimeSpan _defaultTimeout;
        readonly IGuardedResource _guardedResource;
        bool _lockOnNextPass;
        Action<ThreadSnapshot> _passThroughAction;
        bool _isOpen;
        readonly List<ThreadSnapshot> _requestsThreads = new List<ThreadSnapshot>();
        readonly LinkedList<ThreadSnapshot> _queuedThreads = new LinkedList<ThreadSnapshot>();
        readonly List<ThreadSnapshot> _passedThreads = new List<ThreadSnapshot>();
    }
}

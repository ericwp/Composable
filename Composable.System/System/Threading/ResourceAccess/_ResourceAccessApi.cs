﻿using System;

namespace Composable.System.Threading.ResourceAccess
{
    interface IExclusiveResourceLockManager
    {
        IExclusiveResourceLock AwaitExclusiveLock(TimeSpan? timeoutOverride = null);
    }

    interface IResourceLock : IDisposable {}

    interface IExclusiveResourceLock : IResourceLock
    {
        void SendUpdateNotificationToOneThreadAwaitingUpdateNotification();
        void SendUpdateNotificationToAllThreadsAwaitingUpdateNotification();
        void ReleaseLockAwaitUpdateNotificationAndAwaitExclusiveLock(TimeSpan? timeout = null);
    }
}

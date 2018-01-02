﻿using System;
using Composable.Messaging;

namespace Composable.Persistence.EventStore
{
    interface IEventStoreEventSerializer
    {
        string Serialize(object @event);
        IAggregateRootEvent Deserialize(Type eventType, string eventData);
    }
}
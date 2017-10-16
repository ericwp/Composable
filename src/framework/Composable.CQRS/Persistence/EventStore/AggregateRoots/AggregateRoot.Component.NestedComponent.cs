﻿using System;
using Composable.GenericAbstractions.Time;
using Composable.Messaging;
using Composable.Messaging.Events;
using JetBrains.Annotations;

namespace Composable.Persistence.EventStore.AggregateRoots
{
    public abstract partial class AggregateRoot<TAggregateRoot, TAggregateRootBaseEventClass, TAggregateRootBaseEventInterface>
        where TAggregateRoot : AggregateRoot<TAggregateRoot, TAggregateRootBaseEventClass, TAggregateRootBaseEventInterface>
        where TAggregateRootBaseEventInterface : class, IDomainEvent
        where TAggregateRootBaseEventClass : DomainEvent, TAggregateRootBaseEventInterface
    {
        public abstract partial class Component<TComponent, TComponentBaseEventClass, TComponentBaseEventInterface>
            where TComponentBaseEventInterface : class, TAggregateRootBaseEventInterface
            where TComponentBaseEventClass : TAggregateRootBaseEventClass, TComponentBaseEventInterface
            where TComponent : Component<TComponent, TComponentBaseEventClass, TComponentBaseEventInterface>
        {
            [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
            public abstract class NestedComponent<TNestedComponent, TNestedComponentBaseEventClass, TNestedComponentBaseEventInterface> :
                AggregateRoot<TAggregateRoot, TAggregateRootBaseEventClass, TAggregateRootBaseEventInterface>.
                    Component<TNestedComponent, TNestedComponentBaseEventClass, TNestedComponentBaseEventInterface>
                where TNestedComponentBaseEventInterface : class, TComponentBaseEventInterface
                where TNestedComponentBaseEventClass : TComponentBaseEventClass, TNestedComponentBaseEventInterface
                where TNestedComponent : NestedComponent<TNestedComponent, TNestedComponentBaseEventClass, TNestedComponentBaseEventInterface>
            {
                protected NestedComponent(TComponent parent)
                    : base(parent.TimeSource, parent.RaiseEvent, parent.RegisterEventAppliers(), registerEventAppliers: true) {}

                protected NestedComponent
                    (IUtcTimeTimeSource timeSource,
                     Action<TNestedComponentBaseEventClass> raiseEventThroughParent,
                     IEventHandlerRegistrar<TNestedComponentBaseEventInterface> appliersRegistrar,
                     bool registerEventAppliers) : base(timeSource, raiseEventThroughParent, appliersRegistrar, registerEventAppliers) {}
            }
        }
    }
}

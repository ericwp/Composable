﻿using Composable.Messaging.Events;
using Composable.Persistence.EventStore.Aggregates;

namespace Composable.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels
{
    public abstract partial class SelfGeneratingQueryModel<TAggregate, TAggregateEvent>
        where TAggregate : SelfGeneratingQueryModel<TAggregate, TAggregateEvent>
        where TAggregateEvent : class, IAggregateEvent
    {
        public abstract partial class Component<TComponent, TComponentEvent>
            where TComponentEvent : class, TAggregateEvent
            where TComponent : Component<TComponent, TComponentEvent>
        {
            public abstract class NestedEntity<TEntity,
                                               TEntityId,
                                               TEntityEvent,
                                               TEntityCreatedEvent,
                                               TEventEntityIdGetter> : NestedComponent<TEntity,
                                                                                 TEntityEvent>
                where TEntityEvent : class, TComponentEvent
                where TEntityCreatedEvent : TEntityEvent
                where TEntity : NestedEntity<TEntity,
                                    TEntityId,
                                    TEntityEvent,
                                    TEntityCreatedEvent,
                                    TEventEntityIdGetter>
                where TEventEntityIdGetter : IGeTAggregateEntityEventEntityId<TEntityEvent, TEntityId>, new()
            {
                static readonly TEventEntityIdGetter IdGetter = new TEventEntityIdGetter();

                protected NestedEntity(TComponent parent): this(parent.RegisterEventAppliers()) { }

                protected NestedEntity(IEventHandlerRegistrar<TEntityEvent> appliersRegistrar) : base(appliersRegistrar, registerEventAppliers: false)
                {
                    RegisterEventAppliers()
                        .For<TEntityCreatedEvent>(e => Id = IdGetter.GetId(e));
                }

                internal TEntityId Id { get; private set; }

                public  static CollectionManager CreateSelfManagingCollection(TComponent parent)//todo:tests
                  =>
                      new CollectionManager(
                          parent: parent,
                          appliersRegistrar: parent.RegisterEventAppliers());

                public class CollectionManager : QueryModelEntityCollectionManager<TComponent,
                                                         TEntity,
                                                         TEntityId,
                                                         TEntityEvent,
                                                         TEntityCreatedEvent,
                                                         TEventEntityIdGetter>
                {
                    internal CollectionManager
                        (TComponent parent,
                         IEventHandlerRegistrar<TEntityEvent> appliersRegistrar)
                        : base(parent, appliersRegistrar)
                    { }
                }
            }
        }
    }
}

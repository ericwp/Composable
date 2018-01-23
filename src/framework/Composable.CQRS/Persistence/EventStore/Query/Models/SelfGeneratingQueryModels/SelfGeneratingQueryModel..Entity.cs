﻿using Composable.Messaging.Events;
using Composable.Persistence.EventStore.AggregateRoots;
using JetBrains.Annotations;

namespace Composable.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels
{
    public abstract partial class SelfGeneratingQueryModel<TAggregateRoot, TAggregateRootBaseEventInterface>
        where TAggregateRoot : SelfGeneratingQueryModel<TAggregateRoot, TAggregateRootBaseEventInterface>
        where TAggregateRootBaseEventInterface : class, IAggregateRootEvent
    {
        [UsedImplicitly(ImplicitUseKindFlags.InstantiatedWithFixedConstructorSignature)]
        public abstract class Entity<TEntity,
                                     TEntityId,
                                     TEntityBaseEventInterface,
                                     TEntityCreatedEventInterface,
                                     TEventEntityIdSetterGetter> : Component<TEntity, TEntityBaseEventInterface>
            where TEntityBaseEventInterface : class, TAggregateRootBaseEventInterface
            where TEntityCreatedEventInterface : TEntityBaseEventInterface
            where TEntity : Entity<TEntity,
                                TEntityId,
                                TEntityBaseEventInterface,
                                TEntityCreatedEventInterface,
                                TEventEntityIdSetterGetter>
            where TEventEntityIdSetterGetter : IGetAggregateRootEntityEventEntityId<TEntityBaseEventInterface, TEntityId>,
                new()
        {
            static readonly TEventEntityIdSetterGetter IdGetterSetter = new TEventEntityIdSetterGetter();

            public TEntityId Id { get; private set; }

            protected Entity(TAggregateRoot aggregateRoot) : this(aggregateRoot.RegisterEventAppliers()) {}

            Entity
                (IEventHandlerRegistrar<TEntityBaseEventInterface> appliersRegistrar) : base(appliersRegistrar, registerEventAppliers: false)
            {
                RegisterEventAppliers()
                    .For<TEntityCreatedEventInterface>(e => Id = IdGetterSetter.GetId(e));
            }

            // ReSharper disable once UnusedMember.Global todo: write tests.
            public static CollectionManager CreateSelfManagingCollection(TAggregateRoot parent)
                => new CollectionManager(parent, parent.RegisterEventAppliers());

            public class CollectionManager : QueryModelEntityCollectionManager<
                                                 TAggregateRoot,
                                                 TEntity,
                                                 TEntityId,
                                                 TEntityBaseEventInterface,
                                                 TEntityCreatedEventInterface,
                                                 TEventEntityIdSetterGetter>
            {
                internal CollectionManager
                    (TAggregateRoot parent, IEventHandlerRegistrar<TEntityBaseEventInterface> appliersRegistrar) : base(parent, appliersRegistrar) {}
            }
        }
    }
}

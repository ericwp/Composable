using System;
using Composable.GenericAbstractions.Time;
using Composable.Persistence.EventStore.AggregateRoots;
using Composable.Tests.CQRS.AggregateRoot.NestedEntitiesTests.GuidId.Domain.Events;

namespace Composable.Tests.CQRS.AggregateRoot.NestedEntitiesTests.GuidId.Domain
{
    class Root : AggregateRoot<Root, RootEvent.Implementation.Root, RootEvent.IRoot>
    {
        public string Name { get; private set; }
        readonly Entity.CollectionManager _entities;
#pragma warning disable 108,114
        public Component Component { get; private set; }
#pragma warning restore 108,114

        public Root(string name) : base(new DateTimeNowTimeSource())
        {
            Component = new Component(this);
            _entities = Entity.CreateSelfManagingCollection(this);

            RegisterEventAppliers()
                .For<RootEvent.PropertyUpdated.Name>(e => Name = e.Name);

            Publish(new RootEvent.Implementation.Created(Guid.NewGuid(), name));
        }

        public IReadOnlyEntityCollection<Entity, Guid> Entities => _entities.Entities;
        public Entity AddEntity(string name) => _entities.AddByPublishing(new RootEvent.Entity.Implementation.Created(Guid.NewGuid(), name));
    }
}

using System;
using Composable.CQRS.EventSourcing;
using CQRS.Tests.CQRS.EventSourcing.AggregateRoot.NestedEntitiesTests.GuidId.Domain.Events;

namespace CQRS.Tests.CQRS.EventSourcing.AggregateRoot.NestedEntitiesTests.GuidId.Domain
{
    public partial class Component : Root.Component<Component, RootEvent.Component.Implementation.Root, RootEvent.Component.IRoot>
    {        
        public Component(Root root) : base(root)
        {
            _entities = Entity.CreateSelfManagingCollection(this);
            InnerComponent = new  NestedComponent(this);
            RegisterEventAppliers()
                .For<RootEvent.Component.PropertyUpdated.Name>(e => Name = e.Name);
        }

        public NestedComponent InnerComponent { get; }
        public string Name { get; private set; }
        public IReadOnlyEntityCollection<Entity, Guid> Entities => _entities.Entities;        
        public void Rename(string name) { RaiseEvent(new RootEvent.Component.Implementation.Renamed(name)); }
        public Entity AddEntity(string name) { return _entities.Add(new RootEvent.Component.Entity.Implementation.Created(Guid.NewGuid(), name)); }

        private readonly Entity.CollectionManager _entities;
    }
}
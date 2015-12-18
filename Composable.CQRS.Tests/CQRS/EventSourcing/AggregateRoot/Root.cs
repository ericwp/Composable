using System;
using Composable.CQRS.EventSourcing;
using Composable.GenericAbstractions.Time;
using JetBrains.Annotations;

namespace CQRS.Tests.CQRS.EventSourcing.AggregateRoot
{
    public class Root : AggregateRoot<Root, RootEvent.Implementation.Root, RootEvent.IRoot>
    {
        public string Name { get; private set; }
        public Entity.Collection Entities { get; }
        public new Component Component { get; private set; }

        public Root(string name) : base(new DateTimeNowTimeSource())
        {
            Component = new Component(this);
            Entities = Entity.CreateSelfManagingCollection(this);

            RegisterEventAppliers()
                .For<RootEvent.PropertyUpdated.Name>(e => Name = e.Name);

            RaiseEvent(new RootEvent.Implementation.Created(Guid.NewGuid(), name));
        }

        public Entity AddEntity(string name) { return Entities.Add(new RootEvent.Entity.Implementation.Created(Guid.NewGuid(), name)); }
    }

    public class Component : Root.Component<Component, RootEvent.Component.Implementation.Root, RootEvent.Component.IRoot>
    {
        public string Name { get; private set; }
        public Component(Root root) : base(root)
        {
            Entities = Entity.CreateSelfManagingCollection(this);

            RegisterEventAppliers()
                .For<RootEvent.Component.PropertyUpdated.Name>(e => Name = e.Name);
        }

        public Entity.Collection Entities { get; private set; }

        public void Rename(string name) { RaiseEvent(new RootEvent.Component.Implementation.Renamed(name)); }
        public Entity AddEntity(string name) { return Entities.Add(new RootEvent.Component.Entity.Implementation.Created(Guid.NewGuid(), name)); }

        [UsedImplicitly]
        public class Entity : NestedEntity<Entity,
                                  RootEvent.Component.Entity.Implementation.Root,
                                  RootEvent.Component.Entity.IRoot,
                                  RootEvent.Component.Entity.Created,
                                  RootEvent.Component.Entity.Implementation.IdGetterSetter>
        {
            public string Name { get; private set; }
            public Entity()
            {
                RegisterEventAppliers()
                    .For<RootEvent.Component.Entity.PropertyUpdated.Name>(e => Name = e.Name);
            }

            public void Rename(string name) { RaiseEvent(new RootEvent.Component.Entity.Implementation.Renamed(name, Id)); }
        }
    }

    [UsedImplicitly]
    public class Entity : Root.Entity<Entity, RootEvent.Entity.Implementation.Root, RootEvent.Entity.IRoot, RootEvent.Entity.Created, RootEvent.Entity.Implementation.IdGetterSetter>
    {
        public string Name { get; private set; }
        public Entity()
        {
            Entities = NestedEntity.CreateSelfManagingCollection(this);
            RegisterEventAppliers()
                .For<RootEvent.Entity.PropertyUpdated.Name>(e => Name = e.Name);
        }

        public NestedEntity.Collection Entities { get; private set; }

        public void Rename(string name) { RaiseEvent(new RootEvent.Entity.Implementation.Renamed(name, Id)); }

        public class NestedEntity : NestedEntity<NestedEntity, RootEvent.Entity.NestedEntity.Implementation.Root, RootEvent.Entity.NestedEntity.IRoot, RootEvent.Entity.NestedEntity.Created, RootEvent.Entity.NestedEntity.Implementation.IdGetterSetter>
        {
            public string Name { get; private set; }
            public NestedEntity()
            {
                RegisterEventAppliers()
                    .For<RootEvent.Entity.NestedEntity.PropertyUpdated.Name>(e => Name = e.Name);
            }

            public void Rename(string name) { RaiseEvent(new RootEvent.Entity.NestedEntity.Implementation.Renamed(name, Id)); }
        }

        public NestedEntity AddEntity(string name) => Entities.Add(new RootEvent.Entity.NestedEntity.Implementation.Created(innerEntityId: Guid.NewGuid(), outerEntityId: Id, name: name));
    }
}

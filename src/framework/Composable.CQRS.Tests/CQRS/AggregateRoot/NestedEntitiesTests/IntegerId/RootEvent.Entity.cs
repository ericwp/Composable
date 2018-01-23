using Composable.Persistence.EventStore.Aggregates;

// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace Composable.Tests.CQRS.AggregateRoot.NestedEntitiesTests.IntegerId
{
    static partial class RootEvent
    {
        public static partial class Entity
        {
            public interface IRoot : RootEvent.IRoot
            {
                int EntityId { get; }
            }

            internal interface Created : IRoot, PropertyUpdated.Name {}

            interface Renamed : IRoot, PropertyUpdated.Name {}

            internal interface Removed : IRoot {}

            internal static class PropertyUpdated
            {
                public interface Name : IRoot
                {
                    string Name { get; }
                }
            }

            internal static class Implementation
            {
                public abstract class Root : RootEvent.Implementation.Root, Entity.IRoot
                {
                    public int EntityId { get; protected set; }

                    public class IdGetterSetter : IGetSeTAggregateEntityEventEntityId<int, Root, IRoot>
                    {
                        public void SetEntityId(Root @event, int id) => @event.EntityId = id;
                        public int GetId(IRoot @event) => @event.EntityId;
                    }
                }

                public class Created : Root, Entity.Created
                {
                    public Created(int entityId, string name)
                    {
                        EntityId = entityId;
                        Name = name;
                    }
                    public string Name { get; }
                }

                public class Renamed : Root, Entity.Renamed
                {
                    public Renamed(string name) => Name = name;
                    public string Name { get; }
                }

                public class Removed : Root, Entity.Removed {}
            }
        }
    }
}

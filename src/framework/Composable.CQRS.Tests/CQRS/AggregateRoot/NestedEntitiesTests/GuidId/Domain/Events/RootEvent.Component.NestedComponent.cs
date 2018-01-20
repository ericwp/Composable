 // ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier
// ReSharper disable InconsistentNaming
namespace Composable.Tests.CQRS.AggregateRoot.NestedEntitiesTests.GuidId.Domain.Events
{
    static partial class RootEvent
    {
        public static partial class Component
        {
            internal static class NestedComponent
            {
                internal interface IRoot : Component.IRoot { }

                internal static class PropertyUpdated
                {
                    public interface Name : NestedComponent.IRoot
                    {
                    }
                }

                internal static class Implementation
                {
                    public abstract class Root : Component.Implementation.Root, NestedComponent.IRoot { }
                }
            }
        }
    }
}

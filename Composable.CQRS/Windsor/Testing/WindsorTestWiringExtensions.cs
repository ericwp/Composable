using Castle.MicroKernel.Registration;
using Composable.CQRS.CQRS.EventSourcing;
using Composable.CQRS.KeyValueStorage;
using Composable.Persistence.KeyValueStorage;
using Composable.Windsor.Testing;

namespace Composable.CQRS.Windsor.Testing
{

    static class RewiringHelperExtensions
    {
        internal static IExecuteActionsWhenRewiringForTesting ReplaceEventStore(this IExecuteActionsWhenRewiringForTesting @this, string name, string replacementName = null)
        {
            return @this.ReplaceComponent(
                componentName: name,
#pragma warning disable 618
                replacement: Component.For<IEventStore>()
#pragma warning restore 618
                    .ImplementedBy<InMemoryEventStore>()
                    .LifestyleSingleton(),
                    replacementName: replacementName);
        }

        public static IExecuteActionsWhenRewiringForTesting ReplaceDocumentDb(this IExecuteActionsWhenRewiringForTesting @this, string dbToReplace, string replacementName = null)
        {
            return @this.ReplaceComponent(
                componentName: dbToReplace,
#pragma warning disable 618
                replacement: Component.For<IDocumentDb>()
#pragma warning restore 618
                    .ImplementedBy<InMemoryDocumentDb>()
                    .LifestyleSingleton(),
                replacementName: replacementName);
        }
    }
}

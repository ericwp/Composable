using Composable.CQRS.EventSourcing;

namespace Composable.CQRS.CQRS.EventSourcing.Refactoring.Migrations
{
    class AssertMigrationsAreIdempotentEventModifier : IEventModifier
    {
        public static readonly IEventModifier Instance = new AssertMigrationsAreIdempotentEventModifier();
        AssertMigrationsAreIdempotentEventModifier() { }

        public void Replace(params AggregateRootEvent[] events) { throw new NonIdempotentMigrationDetectedException(); }

        public void InsertBefore(params AggregateRootEvent[] insert) { throw new NonIdempotentMigrationDetectedException(); }
    }
}

using System;

using Composable.Contracts;
using Composable.System;

namespace Composable.CQRS.EventSourcing.Refactoring.Migrations
{
    public abstract class EventMigration<TMigratedAggregateEventHierarchyRootInterface> : IEventMigration
        where TMigratedAggregateEventHierarchyRootInterface : IAggregateRootEvent
    {
        protected EventMigration(Guid id, string name, string description)
        {
            Contract.Argument(() => id)
                        .NotNullOrDefault();

            Contract.Argument(() => description, () => name)
                        .NotNullEmptyOrWhiteSpace();


            Contract.Assert(typeof(TMigratedAggregateEventHierarchyRootInterface).IsInterface, $"{nameof(TMigratedAggregateEventHierarchyRootInterface)} must be an interface.");

            Id = id;
            Name = name;
            Description = description;
            Done = false;
        }

        public Guid Id { get; }
        public string Name { get; }
        public string Description { get; }
        public bool Done { get; }
        public Type MigratedAggregateEventHierarchyRootInterface => typeof(TMigratedAggregateEventHierarchyRootInterface);
        public abstract ISingleAggregateInstanceHandlingEventMigrator CreateSingleAggregateInstanceHandlingMigrator();
    }
}
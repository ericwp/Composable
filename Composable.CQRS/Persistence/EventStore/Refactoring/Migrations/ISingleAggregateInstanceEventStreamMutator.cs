using System.Collections.Generic;
using Composable.Persistence.EventSourcing;

namespace Composable.Persistence.EventStore.Refactoring.Migrations
{
    ///<summary>Implementations are responsible for mutating the events of one aggregate instance. Callers are required to call <see cref="Mutate"/> with each event in order and to end by calling <see cref="EndOfAggregate"/></summary>
    interface ISingleAggregateInstanceEventStreamMutator
    {
        IEnumerable<AggregateRootEvent> Mutate(AggregateRootEvent @event);
        IEnumerable<AggregateRootEvent> EndOfAggregate();
    }
}

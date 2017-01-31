﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Composable.CQRS.EventSourcing;
using Composable.GenericAbstractions.Time;
using Composable.System.Linq;
using JetBrains.Annotations;

namespace TestAggregates
{
    public interface IRootEvent : IAggregateRootEvent { }

    public abstract class RootEvent : AggregateRootEvent, IRootEvent
    {}

    namespace Events
    {
        public abstract class ECAbstract : RootEvent, IAggregateRootCreatedEvent
        {}

        public class Ec1 : ECAbstract{}

        public class Ec2 : ECAbstract{}

        public class Ec3 : ECAbstract{}

        public class E1 : RootEvent { }
        public class E2 : RootEvent { }
        public class E3 : RootEvent { }
        public class E4 : RootEvent { }
        public class E5 : RootEvent { }
        public class E6 : RootEvent { }
        public class E7 : RootEvent { }
        public class E8 : RootEvent { }
        public class E9 : RootEvent { }
        public class Ef : RootEvent { }
    }


    public class TestAggregate : AggregateRoot<TestAggregate, RootEvent, IRootEvent>
    {
        public void RaiseEvents(params RootEvent[] events)
        {
            if (GetIdBypassContractValidation() == Guid.Empty && events.First().AggregateRootId == Guid.Empty)
            {
                SetIdBeVerySureYouKnowWhatYouAreDoing(Guid.NewGuid());
                events.Cast<AggregateRootEvent>().First().AggregateRootId = Id;
            }

            foreach (var @event in events)
            {
                RaiseEvent(@event);
            }
        }


        [Obsolete("For serialization only", error: true), UsedImplicitly]
        public TestAggregate()
        {
            SetupAppliers();
        }

        TestAggregate(IUtcTimeTimeSource timeSource):base(timeSource)
        {
            SetupAppliers();
        }

        void SetupAppliers()
        {
            RegisterEventAppliers()
                .For<IRootEvent>(e => _history.Add(e));
        }

        public TestAggregate(IUtcTimeTimeSource timeSource, params RootEvent[] events):this(timeSource)
        {
            Contract.Requires(events.First() is IAggregateRootCreatedEvent);

            RaiseEvents(events);
        }

        public static TestAggregate FromEvents(IUtcTimeTimeSource timeSource, Guid? id, IEnumerable<Type> events)
        {
            var rootEvents = events.ToEvents();
            rootEvents.Cast<AggregateRootEvent>().First().AggregateRootId = id ?? Guid.NewGuid();
            return new TestAggregate(timeSource, rootEvents);
        }

        readonly List<IRootEvent> _history = new List<IRootEvent>();
        public IReadOnlyList<IAggregateRootEvent> History => _history;
    }

    public static class EventSequenceGenerator
    {
        public static RootEvent[] ToEvents(this IEnumerable<Type> types)
        {
            return types.Select(Activator.CreateInstance).Cast<RootEvent>().ToArray();
        }
    }
}
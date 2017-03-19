﻿using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Contracts;
using Composable.CQRS.EventSourcing;
using Composable.GenericAbstractions.Time;
using JetBrains.Annotations;

namespace Composable.CQRS.Tests.CQRS.EventSourcing.EventRefactoring.Migrations
{
    interface IRootEvent : IAggregateRootEvent { }

    abstract class RootEvent : AggregateRootEvent, IRootEvent
    {}

    namespace Events
    {
        abstract class EcAbstract : RootEvent, IAggregateRootCreatedEvent
        {}

        // ReSharper disable ClassNeverInstantiated.Global
        class Ec1 : EcAbstract{}
        class Ec2 : EcAbstract{}
        class Ec3 : EcAbstract{}
        // ReSharper restore ClassNeverInstantiated.Global

        class E1 : RootEvent { }
        class E2 : RootEvent { }
        class E3 : RootEvent { }
        class E4 : RootEvent { }
        class E5 : RootEvent { }
        class E6 : RootEvent { }
        class E7 : RootEvent { }
        class E8 : RootEvent { }
        class E9 : RootEvent { }
        class Ef : RootEvent { }
    }


    class TestAggregate : AggregateRoot<TestAggregate, RootEvent, IRootEvent>
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
            Contract.Assert.That(events.First() is IAggregateRootCreatedEvent, "events.First() is IAggregateRootCreatedEvent");

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

    static class EventSequenceGenerator
    {
        public static RootEvent[] ToEvents(this IEnumerable<Type> types) => types.Select(Activator.CreateInstance).Cast<RootEvent>().ToArray();
    }
}
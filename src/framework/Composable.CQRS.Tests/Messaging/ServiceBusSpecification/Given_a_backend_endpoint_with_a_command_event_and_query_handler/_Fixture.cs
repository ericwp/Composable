﻿using System;
using System.Collections.Generic;
using Composable.DependencyInjection;
using Composable.DependencyInjection.Persistence;
using Composable.GenericAbstractions.Time;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Aggregates;
using Composable.System.Linq;
using Composable.Testing.Threading;
using FluentAssertions;

namespace Composable.Tests.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler
{
    public class Fixture : IDisposable
    {
        internal readonly ITestingEndpointHost Host;
        internal readonly IThreadGate CommandHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());
        internal readonly IThreadGate CommandHandlerWithResultThreadGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());
        internal readonly IThreadGate MyCreateAggregateCommandHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());
        internal readonly IThreadGate MyUpdateAggregateCommandHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());
        internal readonly IThreadGate MyRemoteAggregateEventHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());
        internal readonly IThreadGate MyLocalAggregateEventHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());
        internal readonly IThreadGate EventHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(1.Seconds());
        internal readonly IThreadGate QueryHandlerThreadGate = ThreadGate.CreateOpenWithTimeout(5.Seconds());

        internal readonly IReadOnlyList<IThreadGate> AllGates;

        protected readonly TestingTaskRunner TaskRunner = TestingTaskRunner.WithTimeout(1.Seconds());
        protected readonly IEndpoint ClientEndpoint;

        protected Fixture()
        {
            Host = EndpointHost.Testing.CreateHost(DependencyInjectionContainer.Create);
            Host.RegisterAndStartEndpoint(
                "Backend",
                new EndpointId(Guid.Parse("DDD0A67C-D2A2-4197-9AF8-38B6AEDF8FA6")),
                builder =>
                {
                    builder.Container.RegisterSqlServerEventStore("Backend")
                           .HandleAggregate<MyAggregate, MyAggregateEvent.IRoot>(builder.RegisterHandlers);

                    builder.RegisterHandlers
                           .ForCommand((MyExactlyOnceCommand command) => CommandHandlerThreadGate.AwaitPassthrough())
                           .ForCommand((MyCreateAggregateCommand command, ILocalApiNavigatorSession navigator) => MyCreateAggregateCommandHandlerThreadGate.AwaitPassthroughAndExecute(() => MyAggregate.Create(command.AggregateId, navigator)))
                           .ForCommand((MyUpdateAggregateCommand command, ILocalApiNavigatorSession navigator) => MyUpdateAggregateCommandHandlerThreadGate.AwaitPassthroughAndExecute(() => navigator.Execute(new ComposableApi().EventStore.Queries.GetForUpdate<MyAggregate>(command.AggregateId)).Update()))
                           .ForEvent((MyExactlyOnceEvent myEvent) => EventHandlerThreadGate.AwaitPassthrough())
                           .ForEvent((MyAggregateEvent.IRoot myAggregateEvent) => MyLocalAggregateEventHandlerThreadGate.AwaitPassthrough())
                           .ForQuery((MyQuery query) => QueryHandlerThreadGate.AwaitPassthroughAndReturn(new MyQueryResult()))
                           .ForCommandWithResult((MyAtMostOnceCommandWithResult command) => CommandHandlerWithResultThreadGate.AwaitPassthroughAndReturn(new MyCommandResult()));

                    builder.TypeMapper.Map<MyExactlyOnceCommand>("0ddefcaa-4d4d-48b2-9e1a-762c0b835275")
                           .Map<MyAtMostOnceCommandWithResult>("24248d03-630b-4909-a6ea-e7fdaf82baa2")
                           .Map<MyExactlyOnceEvent>("2fdde21f-c6d4-46a2-95e5-3429b820dfc3")
                           .Map<MyQuery>("b9d62f22-514b-4e3c-9ac1-66940a7a8144")
                           .Map<MyCreateAggregateCommand>("86bf04d8-8e6d-4e21-a95e-8af237f69f0f")
                           .Map<MyUpdateAggregateCommand>("c4ce3662-d068-4ec1-9c02-8d8f08640414")
                           .Map<MyAggregateEvent.IRoot>("8b19a261-b74b-4c05-91e3-d062dc879635")
                           .Map<MyAggregate>("8b7df016-3763-4033-8240-f46fa836ebfb")
                           .Map<MyAggregateEvent.Created>("41f96e37-657f-464a-a4d1-004eba4e8e7b")
                           .Map<MyAggregateEvent.Implementation.Created>("0ea2f548-0d24-4bb0-a59a-820bc35f3935")
                           .Map<MyAggregateEvent.Implementation.Root>("5a792961-3fbc-4d50-b06e-77fc35cb6edf")
                           .Map<MyAggregateEvent.Implementation.Updated>("bead75b3-9ecf-4f6b-b8c6-973a02168256")
                           .Map<MyAggregateEvent.Updated>("2a8b19f0-20df-480d-b120-71ed5151b174")
                           .Map<MyCommandResult>("4b2f17d2-2997-4532-9296-689495ed6958")
                           .Map<MyQueryResult>("9f3c69f0-0886-483c-a726-b79fb1c56120");
                });

            Host.RegisterAndStartEndpoint("Remote",
                new EndpointId(Guid.Parse("E72924D3-5279-44B5-B20D-D682E537672B")),
                                          builder => builder.RegisterHandlers.ForEvent((MyAggregateEvent.IRoot myAggregateEvent) => MyRemoteAggregateEventHandlerThreadGate.AwaitPassthrough()));

            ClientEndpoint = Host.ClientEndpoint;

            AllGates = new List<IThreadGate>()
                       {
                           CommandHandlerThreadGate,
                           CommandHandlerWithResultThreadGate,
                           MyCreateAggregateCommandHandlerThreadGate,
                           MyUpdateAggregateCommandHandlerThreadGate,
                           MyRemoteAggregateEventHandlerThreadGate,
                           MyLocalAggregateEventHandlerThreadGate,
                           EventHandlerThreadGate,
                           QueryHandlerThreadGate
                       };
        }

        public virtual void Dispose()
        {
            OpenGates();
            TaskRunner.Dispose();
            Host.Dispose();
        }

        protected void CloseGates() => AllGates.ForEach(gate => gate.Close());

        protected void OpenGates() => AllGates.ForEach(gate => gate.Open());

        protected static class MyAggregateEvent
        {
            public interface IRoot : IAggregateEvent{}
            public interface Created : IRoot, IAggregateCreatedEvent {}
            public interface Updated : IRoot{}
            public class Implementation
            {
                public class Root : AggregateEvent, IRoot
                {
                    protected Root() {}
                    protected Root(Guid aggregateId) : base(aggregateId) {}
                }

                // ReSharper disable once MemberHidesStaticFromOuterClass
                public class Created : Root, MyAggregateEvent.Created
                {
                    public Created(Guid aggregateId) : base(aggregateId) {}
                }

                // ReSharper disable once MemberHidesStaticFromOuterClass
                public class Updated : Root, MyAggregateEvent.Updated
                {
                }
            }
        }

        protected class MyAggregate : Aggregate<MyAggregate, MyAggregateEvent.Implementation.Root, MyAggregateEvent.IRoot>
        {
            public MyAggregate() : base(new DateTimeNowTimeSource())
            {
                RegisterEventAppliers()
                   .IgnoreUnhandled<MyAggregateEvent.IRoot>();
            }

            internal void Update() => Publish(new MyAggregateEvent.Implementation.Updated());

            internal static void Create(Guid id, ILocalApiNavigatorSession bus)
            {
                var created = new MyAggregate();
                created.Publish(new MyAggregateEvent.Implementation.Created(id));
                bus.Execute(new ComposableApi().EventStore.Commands.Save(created));
            }
        }

        protected class MyCreateAggregateCommand : BusApi.Remotable.AtMostOnce.ICommand
        {
            public Guid AggregateId { get; private set; } = Guid.NewGuid();
        }

        protected class MyUpdateAggregateCommand : BusApi.Remotable.AtMostOnce.ICommand
        {
            public MyUpdateAggregateCommand(Guid aggregateId) => AggregateId = aggregateId;
            public Guid AggregateId { get; private set; }
        }


        protected class MyExactlyOnceCommand : BusApi.Remotable.ExactlyOnce.Command {}
        protected class MyExactlyOnceEvent : AggregateEvent {}
        protected class MyQuery : BusApi.Remotable.NonTransactional.Queries.Query<MyQueryResult> {}
        protected class MyQueryResult {}
        protected class MyAtMostOnceCommand : BusApi.Remotable.AtMostOnce.Command<MyCommandResult> {}
        protected class MyAtMostOnceCommandWithResult : BusApi.Remotable.AtMostOnce.Command<MyCommandResult> {}
        protected class MyCommandResult {}
    }
}

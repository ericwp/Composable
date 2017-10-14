﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Composable.DependencyInjection;
using Composable.DependencyInjection.Persistence;
using Composable.GenericAbstractions.Time;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.Messaging.Commands;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.AggregateRoots;
using Composable.System.Linq;
using Composable.Testing.Performance;
using Composable.Testing.Threading;
using FluentAssertions;
using Xunit;

namespace Composable.Tests.Messaging.ServiceBusSpecification.Given_a_backend_endpoint_with_a_command_event_and_query_handler
{
    using Composable.System;

    public class Experiment_with_unifying_events_and_commands_test : IDisposable
    {
        readonly ITestingEndpointHost _host;

        readonly TestingTaskRunner _taskRunner = TestingTaskRunner.WithTimeout(1.Seconds());
        IEndpoint _userManagementDomainEndpoint;
        readonly IServiceLocator _userDomainServiceLocator;

        public Experiment_with_unifying_events_and_commands_test()
        {
            _host = EndpointHost.Testing.BuildHost(
                DependencyInjectionContainer.Create,
                buildHost => _userManagementDomainEndpoint = buildHost.RegisterAndStartEndpoint(
                                 "UserManagement.Domain",
                                 builder =>
                                 {
                                     builder.Container.RegisterSqlServerEventStore<IUserEventStoreUpdater, IUserEventStoreReader>("Someconnectionname");

                                     builder.RegisterHandlers
                                            .ForEvent((UserEvent.Implementation.UserRegisteredEvent myEvent) => {})
                                            .ForQuery((GetUserQuery query, IUserEventStoreReader eventReader) => new UserResource(eventReader.GetHistory(query.UserId)))
                                            .ForCommand((UserRegistrarCommand.RegisterUserCommand command, IUserEventStoreUpdater store) =>
                                            {
                                                store.Save(UserAggregate.Register(command));
                                                return new RegisterUserResult(command.UserId);
                                            });
                                 }));

            _userDomainServiceLocator = _userManagementDomainEndpoint.ServiceLocator;
            _userDomainServiceLocator.ExecuteTransactionInIsolatedScope(() => _userDomainServiceLocator.Resolve<IUserEventStoreUpdater>().Save(UserRegistrarAggregate.Create()));
        }

        [Fact] async Task Can_register_user_and_fetch_user_resource()
        {
            var registrationResult = await _userDomainServiceLocator.ExecuteTransactionInIsolatedScope(
                () => UserRegistrarAggregate.RegisterUser(_userDomainServiceLocator.Resolve<IServiceBus>()));

            var user = _host.ClientBus.Query(registrationResult.UserLink);
            user.Should().NotBe(null);
        }

        [Fact]
        void PerformanceTest2()
        {
            TimeAsserter.Execute(() =>
                                 {
                                     var queries = 1.Through(100).Select(_ => TaskRegisterUserAndGetUserResource()).ToArray();
                                     Task.WaitAll(queries);
                                 },
                                 maxTotal: 500.Milliseconds());
        }

        async Task TaskRegisterUserAndGetUserResource()
        {
            var registrationResult = await _userDomainServiceLocator.ExecuteTransactionInIsolatedScope(
                                                     () => UserRegistrarAggregate.RegisterUser(_userDomainServiceLocator.Resolve<IServiceBus>()));

            await _host.ClientBus.QueryAsync(registrationResult.UserLink);
        }

        public void Dispose()
        {
            _taskRunner.Dispose();
            _host.Dispose();
        }

        public interface IUserEventStoreUpdater : IEventStoreUpdater {}

        public interface IUserEventStoreReader : IEventStoreReader {}

        public static class UserEvent
        {
            public interface IRoot : IAggregateRootEvent {}

            public interface UserRegistered : IRoot, IAggregateRootCreatedEvent {}

            public static class Implementation
            {
                public class Root : AggregateRootEvent, IRoot
                {
                    protected Root() {}
                    protected Root(Guid aggregateRootId) : base(aggregateRootId) {}
                }

                public class UserRegisteredEvent : Root, UserEvent.UserRegistered
                {
                    public UserRegisteredEvent() : base(Guid.NewGuid()) {}
                }
            }
        }

        public static class UserRegistrarCommand
        {
            public interface IRoot : ICommand {}

            public class Root : Command, IRoot {}
            public class Root<TResult> : Command<TResult>, IRoot where TResult : IMessage {}

            public class RegisterUserCommand : Root<RegisterUserResult>
            {
                public Guid UserId { get; private set; } = Guid.NewGuid();
            }
        }

        public static class UserRegistrarEvent
        {
            public interface IRoot : IAggregateRootEvent {}
            public static class Implementation
            {
                public class Root : AggregateRootEvent, IRoot
                {
                    protected Root() {}
                    protected Root(Guid aggregateRootId) : base(aggregateRootId) {}
                }

                public class Created : Root, IAggregateRootCreatedEvent
                {
                    public Created() : base(UserRegistrarAggregate.SingleId) {}
                }
            }
        }

        public class UserRegistrarAggregate : AggregateRoot<UserRegistrarAggregate, UserRegistrarEvent.Implementation.Root, UserRegistrarEvent.IRoot>
        {
            internal static Guid SingleId = Guid.Parse("5C400DD9-50FB-40C7-8A13-265005588AED");
            internal static UserRegistrarAggregate Create()
            {
                var registrar = new UserRegistrarAggregate();
                registrar.RaiseEvent(new UserRegistrarEvent.Implementation.Created());
                return registrar;
            }

            UserRegistrarAggregate() : base(DateTimeNowTimeSource.Instance)
                => RegisterEventAppliers()
                    .IgnoreUnhandled<UserRegistrarEvent.IRoot>();

            internal static async Task<RegisterUserResult> RegisterUser(IServiceBus bus) => await bus.SendAsync(new UserRegistrarCommand.RegisterUserCommand());
        }

        public class UserAggregate : AggregateRoot<UserAggregate, UserEvent.Implementation.Root, UserEvent.IRoot>
        {
            UserAggregate() : base(DateTimeNowTimeSource.Instance)
                => RegisterEventAppliers()
                    .IgnoreUnhandled<UserEvent.IRoot>();

            internal static IEventStored Register(UserRegistrarCommand.RegisterUserCommand command)
            {
                var registered = new UserAggregate();
                registered.RaiseEvent(new UserEvent.Implementation.UserRegisteredEvent());
                return registered;
            }
        }

        public class GetUserQuery : Query<UserResource>
        {
            public Guid UserId { get; private set; }
            public GetUserQuery(Guid userId) => UserId = userId;
        }

        public class UserResource : QueryResult {
            public UserResource(IEnumerable<IAggregateRootEvent> getHistory)
            {
            }
        }

        public class RegisterUserResult : Message
        {
            public GetUserQuery UserLink { get; private set; }
            public RegisterUserResult(Guid userId) => UserLink = new GetUserQuery(userId);
        }
    }
}

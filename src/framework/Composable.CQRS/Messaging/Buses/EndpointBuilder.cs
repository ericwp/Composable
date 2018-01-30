﻿using System;
using System.Configuration;
using Composable.DependencyInjection;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Aggregates;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.System.Configuration;
using Composable.System.Data.SqlClient;
using Composable.System.Threading;
using Composable.SystemExtensions.Threading;

namespace Composable.Messaging.Buses
{
    class EndpointBuilder : IEndpointBuilder
    {
        static readonly ISqlConnection MasterDbConnection = new AppConfigSqlConnectionProvider().GetConnectionProvider(parameterName: "MasterDB");

        readonly IDependencyInjectionContainer _container;
        readonly string _name;
        readonly TypeMapper _typeMapper;
        readonly EndpointId _endpointId;

        public IDependencyInjectionContainer Container => _container;
        public ITypeMappingRegistar TypeMapper => _typeMapper;
        public EndpointConfiguration Configuration { get; }

        public MessageHandlerRegistrarWithDependencyInjectionSupport RegisterHandlers { get; }

        public IEndpoint Build()
        {
            SetupInternalTypeMap();
            return new Endpoint(_container.CreateServiceLocator(), _endpointId, _name);
        }

        void SetupInternalTypeMap()
        {
            EventStoreApi.MapTypes(TypeMapper);
            BusApi.MapTypes(TypeMapper);
        }

        public EndpointBuilder(IGlobalBusStateTracker globalStateTracker, IDependencyInjectionContainer container, string name, EndpointId endpointId)
        {
            _container = container;
            _name = name;
            _endpointId = endpointId;

            Configuration = new EndpointConfiguration(name);

            var endpointSqlConnection = container.RunMode.IsTesting
                                            ? new LazySqlServerConnection(new Lazy<string>(() => container.CreateServiceLocator().Resolve<ISqlConnectionProvider>().GetConnectionProvider(Configuration.ConnectionStringName).ConnectionString))
                                            : new SqlServerConnection(ConfigurationManager.ConnectionStrings[Configuration.ConnectionStringName].ConnectionString);

            _typeMapper = new TypeMapper(endpointSqlConnection);

            var registry = new MessageHandlerRegistry(_typeMapper);
            RegisterHandlers = new MessageHandlerRegistrarWithDependencyInjectionSupport(registry, new Lazy<IServiceLocator>(() => _container.CreateServiceLocator()));

            _container.Register(
                Component.For<ITaskRunner>().ImplementedBy<TaskRunner>().LifestyleSingleton(),
                Component.For<EndpointId>().UsingFactoryMethod(() => endpointId).LifestyleSingleton(),
                Component.For<EndpointConfiguration>()
                         .UsingFactoryMethod(() => Configuration)
                         .LifestyleSingleton(),
                Component.For<ITypeMappingRegistar, ITypeMapper, TypeMapper>()
                         .UsingFactoryMethod(() => _typeMapper)
                         .LifestyleSingleton()
                         .DelegateToParentServiceLocatorWhenCloning(),
                Component.For<IAggregateTypeValidator>()
                         .ImplementedBy<AggregateTypeValidator>()
                         .LifestyleSingleton(),
                Component.For<IInterprocessTransport>()
                         .UsingFactoryMethod((IUtcTimeTimeSource timeSource, ISqlConnectionProvider connectionProvider, EndpointId id, ITaskRunner taskRunner, IRemotableMessageSerializer serializer) =>
                                                 new InterprocessTransport(globalStateTracker, timeSource, endpointSqlConnection, _typeMapper, id, taskRunner, serializer))
                         .LifestyleSingleton(),
                Component.For<ISingleContextUseGuard>()
                         .ImplementedBy<SingleThreadUseGuard>()
                         .LifestyleScoped(),
                Component.For<IGlobalBusStateTracker>()
                         .UsingFactoryMethod(() => globalStateTracker)
                         .LifestyleSingleton(),
                Component.For<IMessageHandlerRegistry, IMessageHandlerRegistrar, MessageHandlerRegistry>()
                         .UsingFactoryMethod(() => registry)
                         .LifestyleSingleton(),
                Component.For<IEventStoreSerializer>()
                         .ImplementedBy<EventStoreSerializer>()
                         .LifestyleSingleton(),
                Component.For<IDocumentDbSerializer>()
                         .ImplementedBy<DocumentDbSerializer>()
                         .LifestyleSingleton(),
                Component.For<IRemotableMessageSerializer>()
                         .ImplementedBy<RemotableMessageSerializer>()
                         .LifestyleSingleton(),
                Component.For<IInbox>()
                         .UsingFactoryMethod((IServiceLocator serviceLocator, IGlobalBusStateTracker stateTracker, EndpointConfiguration endpointConfiguration, ITaskRunner taskRunner, IRemotableMessageSerializer serializer) =>
                                                 new Inbox(serviceLocator, stateTracker, registry, endpointConfiguration, endpointSqlConnection, _typeMapper, taskRunner, serializer))
                         .LifestyleSingleton(),
                Component.For<CommandScheduler>()
                         .UsingFactoryMethod((IInterprocessTransport transport, IUtcTimeTimeSource timeSource) => new CommandScheduler(transport, timeSource))
                         .LifestyleSingleton(),
                Component.For<IServiceBusControl>()
                         .ImplementedBy<ServiceBusControl>()
                         .LifestyleSingleton(),
                Component.For<IServiceBusSession, IRemoteApiNavigatorSession, ILocalApiNavigatorSession>()
                         .ImplementedBy<ApiNavigatorSession>()
                         .LifestyleScoped(),
                Component.For<IEventstoreEventPublisher>()
                         .ImplementedBy<EventstoreEventPublisher>()
                         .LifestyleScoped(),
                Component.For<ISqlConnectionProvider>()
                         .UsingFactoryMethod(() => new SqlServerDatabasePoolSqlConnectionProvider(MasterDbConnection.ConnectionString))
                         .LifestyleSingleton()
                         .DelegateToParentServiceLocatorWhenCloning());

            if(_container.RunMode == RunMode.Production)
            {
                _container.Register(Component.For<IUtcTimeTimeSource>()
                                             .UsingFactoryMethod(() => new DateTimeNowTimeSource())
                                             .LifestyleSingleton()
                                             .DelegateToParentServiceLocatorWhenCloning());
            } else
            {
                _container.Register(Component.For<IUtcTimeTimeSource, TestingTimeSource>()
                                             .UsingFactoryMethod(() => TestingTimeSource.FollowingSystemClock)
                                             .LifestyleSingleton()
                                             .DelegateToParentServiceLocatorWhenCloning());
            }
        }
    }
}

using System;
using System.Collections.Generic;
using Castle.DynamicProxy;
using Composable.Contracts;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses;
using Composable.Messaging.Buses.Implementation;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.MicrosoftSQLServer;
using Composable.Persistence.EventStore.Refactoring.Migrations;
using Composable.Refactoring.Naming;
using Composable.Serialization;
using Composable.System;
using Composable.System.Configuration;
using Composable.System.Data.SqlClient;
using Composable.System.Linq;
using Composable.System.Reflection;
using Composable.SystemExtensions.Threading;
using JetBrains.Annotations;

// ReSharper disable UnusedTypeParameter the type parameters allow non-ambiguous registrations in the container. They are in fact used.

namespace Composable.DependencyInjection.Persistence
{
    interface IEventStore<TSessionInterface, TReaderInterface> : IEventStore {}

    public static class SqlServerEventStoreRegistrationExtensions
    {
        interface IEventStoreUpdater<TSessionInterface, TReaderInterface> : IEventStoreUpdater {}

        class EventStore<TSessionInterface, TReaderInterface> : EventStore, IEventStore<TSessionInterface, TReaderInterface>
        {
            public EventStore(IEventStoreSerializer serializer,
                              IEventStorePersistenceLayer persistenceLayer,
                              ISingleContextUseGuard usageGuard,
                              EventCache<TSessionInterface> cache,
                              IEnumerable<IEventMigration> migrations) : base(persistenceLayer, serializer, usageGuard, cache, migrations:migrations) {}
        }

        class InMemoryEventStore<TSessionInterface, TReaderInterface> : InMemoryEventStore, IEventStore<TSessionInterface, TReaderInterface>
        {
            public InMemoryEventStore(IEnumerable<IEventMigration> migrations = null) : base(migrations) {}
        }

        [UsedImplicitly] class EventStoreUpdater<TSessionInterface, TReaderInterface> : EventStoreUpdater, IEventStoreUpdater<TSessionInterface, TReaderInterface>
        {
            public EventStoreUpdater(IEventstoreEventPublisher eventPublisher,
                                     IEventStore<TSessionInterface, TReaderInterface> store,
                                     ISingleContextUseGuard usageGuard,
                                     IUtcTimeTimeSource timeSource,
                                     IAggregateTypeValidator aggregateTypeValidator) : base(eventPublisher, store, usageGuard, timeSource, aggregateTypeValidator) {}
        }

        interface IEventStorePersistenceLayer<TUpdater> : IEventStorePersistenceLayer
        {
        }

        class EventStorePersistenceLayer<TUpdaterType> : IEventStorePersistenceLayer<TUpdaterType>
        {
            public EventStorePersistenceLayer(IEventStoreSchemaManager schemaManager, IEventStoreEventReader eventReader, IEventStoreEventWriter eventWriter)
            {
                SchemaManager = schemaManager;
                EventReader = eventReader;
                EventWriter = eventWriter;
            }
            public IEventStoreSchemaManager SchemaManager { get; }
            public IEventStoreEventReader EventReader { get; }
            public IEventStoreEventWriter EventWriter { get; }
        }

        [UsedImplicitly] internal class EventCache<TUpdaterType> : EventCache
        {}

        public static SqlServerEventStoreRegistrationBuilder RegisterSqlServerEventStore(this IDependencyInjectionContainer @this,
                                                                                            string connectionName,
                                                                                            IReadOnlyList<IEventMigration> migrations = null)
        {
              Contract.Argument(() => connectionName)
                    .NotNullEmptyOrWhiteSpace();

            migrations = migrations ?? new List<IEventMigration>();

            @this.Register(Component.For<IEnumerable<IEventMigration>>()
                                    .UsingFactoryMethod(() => migrations)
                                    .LifestyleSingleton());

            @this.Register(Component.For<EventCache>()
                                    .UsingFactoryMethod(() => new EventCache())
                                    .LifestyleSingleton());

            if (@this.RunMode.IsTesting && @this.RunMode.TestingMode == TestingMode.InMemory)
            {
                @this.Register(Component.For<IEventStore>()
                                        .UsingFactoryMethod(() => new InMemoryEventStore(migrations: migrations))
                                        .LifestyleSingleton()
                                        .DelegateToParentServiceLocatorWhenCloning());
            } else
            {
                @this.Register(
                    Component.For<IEventStorePersistenceLayer>()
                                .UsingFactoryMethod((ISqlConnectionProvider connectionProvider1, ITypeMapper typeIdMapper) =>
                                                    {
                                                        var connectionProvider = new LazySqlServerConnection(new OptimizedLazy<string>(() => connectionProvider1.GetConnectionProvider(connectionName).ConnectionString));
                                                        var connectionManager = new SqlServerEventStoreConnectionManager(connectionProvider);
                                                        var schemaManager = new SqlServerEventStoreSchemaManager(connectionProvider, typeIdMapper);
                                                        var eventReader = new SqlServerEventStoreEventReader(connectionManager, schemaManager);
                                                        var eventWriter = new SqlServerEventStoreEventWriter(connectionManager, schemaManager);
                                                        return new EventStorePersistenceLayer<IEventStoreUpdater>(schemaManager, eventReader, eventWriter);
                                                    })
                                .LifestyleSingleton());


                @this.Register(Component.For<IEventStore>()
                                        .UsingFactoryMethod((IEventStorePersistenceLayer persistenceLayer, IEventStoreSerializer serializer, ISingleContextUseGuard singleContextUseGuard, EventCache eventCache) => new EventStore(persistenceLayer, serializer, singleContextUseGuard, eventCache, migrations))
                                        .LifestyleScoped());
            }

            @this.Register(Component.For<IEventStoreUpdater, IEventStoreReader>()
                                    .UsingFactoryMethod((IEventstoreEventPublisher eventPublisher, IEventStore eventStore, ISingleContextUseGuard usageGuard, IUtcTimeTimeSource timeSource, IAggregateTypeValidator aggregateTypeValidator) =>
                                                            new EventStoreUpdater(eventPublisher, eventStore, usageGuard, timeSource, aggregateTypeValidator))
                                    .LifestyleScoped());

            return new SqlServerEventStoreRegistrationBuilder();
        }

        public static void RegisterSqlServerEventStore<TSessionInterface, TReaderInterface>(this IDependencyInjectionContainer @this,
                                                                                            string connectionName,
                                                                                            IReadOnlyList<IEventMigration> migrations = null)
            where TSessionInterface : class, IEventStoreUpdater
            where TReaderInterface : IEventStoreReader
            => @this.RegisterSqlServerEventStoreForFlexibleTesting<TSessionInterface, TReaderInterface>(
                connectionName,
                migrations != null
                    ? (Func<IReadOnlyList<IEventMigration>>)(() => migrations)
                    : (() => EmptyMigrationsArray));

        static readonly IEventMigration[] EmptyMigrationsArray = new IEventMigration[0];
        internal static void RegisterSqlServerEventStoreForFlexibleTesting<TSessionInterface, TReaderInterface>(this IDependencyInjectionContainer @this,
                                                                                                                string connectionName,
                                                                                                                Func<IReadOnlyList<IEventMigration>> migrations)
            where TSessionInterface : class, IEventStoreUpdater
            where TReaderInterface : IEventStoreReader
        {
            Contract.Argument(() => connectionName)
                    .NotNullEmptyOrWhiteSpace();
            migrations = migrations ?? (() => EmptyMigrationsArray);

            GeneratedLowLevelInterfaceInspector.InspectInterfaces(Seq.OfTypes<TSessionInterface, TReaderInterface>());


            @this.Register(Component.For<EventCache<TSessionInterface>>()
                                    .UsingFactoryMethod(() => new EventCache<TSessionInterface>())
                                    .LifestyleSingleton());

            if (@this.RunMode.IsTesting && @this.RunMode.TestingMode == TestingMode.InMemory)
            {
                @this.Register(Component.For<InMemoryEventStore<TSessionInterface, TReaderInterface>>()
                                        .UsingFactoryMethod(() => new InMemoryEventStore<TSessionInterface, TReaderInterface>(migrations: migrations()))
                                        .LifestyleSingleton()
                                        .DelegateToParentServiceLocatorWhenCloning());

                @this.Register(Component.For<IEventStore<TSessionInterface, TReaderInterface>>()
                                        .UsingFactoryMethod((InMemoryEventStore<TSessionInterface, TReaderInterface> store) =>
                                                            {
                                                                store.TestingOnlyReplaceMigrations(migrations());
                                                                return store;
                                                            })
                                        .LifestyleScoped());
            } else
            {
                @this.Register(
                    Component.For<IEventStorePersistenceLayer<TSessionInterface>>()
                                .UsingFactoryMethod((ISqlConnectionProvider connectionProvider1, ITypeMapper typeIdMapper) =>
                                                    {
                                                        var connectionProvider = connectionProvider1.GetConnectionProvider(connectionName);
                                                        var connectionManager = new SqlServerEventStoreConnectionManager(connectionProvider);
                                                        var schemaManager = new SqlServerEventStoreSchemaManager(connectionProvider, typeIdMapper);
                                                        var eventReader = new SqlServerEventStoreEventReader(connectionManager, schemaManager);
                                                        var eventWriter = new SqlServerEventStoreEventWriter(connectionManager, schemaManager);
                                                        return new EventStorePersistenceLayer<TSessionInterface>(schemaManager, eventReader, eventWriter);
                                                    })
                                .LifestyleSingleton());


                @this.Register(Component.For<IEventStore<TSessionInterface, TReaderInterface>>()
                                        .UsingFactoryMethod(
                                            (IEventStorePersistenceLayer<TSessionInterface> persistenceLayer, IEventStoreSerializer serializer, EventCache<TSessionInterface> cache) =>
                                                new EventStore<TSessionInterface, TReaderInterface>(
                                                    persistenceLayer: persistenceLayer,
                                                    serializer: serializer,
                                                    migrations: migrations(),
                                                    cache: cache,
                                                    usageGuard: new SingleThreadUseGuard()))
                                        .LifestyleScoped());
            }

            @this.Register(Component.For<IEventStoreUpdater<TSessionInterface, TReaderInterface>>()
                                    .UsingFactoryMethod((IEventstoreEventPublisher eventPublisher, IEventStore<TSessionInterface, TReaderInterface> eventStore, ISingleContextUseGuard usageGuard, IUtcTimeTimeSource timeSource, IAggregateTypeValidator aggregateTypeValidator) =>
                                                            new EventStoreUpdater<TSessionInterface, TReaderInterface>(eventPublisher, eventStore, usageGuard, timeSource, aggregateTypeValidator))
                                    .LifestyleScoped());

            var sessionType = EventStoreSessionProxyFactory<TSessionInterface, TReaderInterface>.ProxyType;
            var constructor = (Func<IInterceptor[], IEventStoreUpdater, TSessionInterface>)Constructor.Compile.ForReturnType(sessionType).WithArguments<IInterceptor[], IEventStoreUpdater>();
            var emptyInterceptorArray = new IInterceptor[0];

            @this.Register(Component.For<TSessionInterface>(Seq.OfTypes<TReaderInterface>())
                                    .UsingFactoryMethod(EventStoreSessionProxyFactory<TSessionInterface, TReaderInterface>.ProxyType, locator => constructor(emptyInterceptorArray, locator.Resolve<IEventStoreUpdater<TSessionInterface, TReaderInterface>>()))
                                    .LifestyleScoped());
        }

        //Using a generic class this way allows us to bypass any need for dictionary lookups or similar giving us excellent performance.
        static class EventStoreSessionProxyFactory<TSessionInterface, TReaderInterface>
            where TSessionInterface : IEventStoreUpdater
            where TReaderInterface : IEventStoreReader
        {
            internal static readonly Type ProxyType = new DefaultProxyBuilder().CreateInterfaceProxyTypeWithTargetInterface(
                interfaceToProxy: typeof(IEventStoreUpdater),
                additionalInterfacesToProxy: new[]
                                             {
                                                 typeof(TSessionInterface),
                                                 typeof(TReaderInterface)
                                             },
                options: ProxyGenerationOptions.Default);
        }
    }

    public class SqlServerEventStoreRegistrationBuilder
    {
        public SqlServerEventStoreRegistrationBuilder HandleAggregate<TAggregate, TEvent>(MessageHandlerRegistrarWithDependencyInjectionSupport registrar)
            where TAggregate : IEventStored<TEvent>
            where TEvent : IAggregateEvent
        {
           EventStoreApi.RegisterHandlersForAggregate<TAggregate, TEvent>(registrar);
            return this;
        }
    }
}

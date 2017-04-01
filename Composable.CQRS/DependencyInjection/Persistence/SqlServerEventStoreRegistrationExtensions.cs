using System;
using System.Collections.Generic;
using Castle.DynamicProxy;
using Composable.Contracts;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.MicrosoftSQLServer;
using Composable.Persistence.EventStore.Refactoring.Migrations;
using Composable.Persistence.EventStore.Refactoring.Naming;
using Composable.System.Configuration;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;
using Composable.UnitsOfWork;
using JetBrains.Annotations;

// ReSharper disable UnusedTypeParameter the type parameters allow non-ambiguous registrations in the container. They are in fact used.

namespace Composable.DependencyInjection.Persistence
{
    interface IEventStore<TSessionInterface, TReaderInterface> : IEventStore {}

    public static class SqlServerEventStoreRegistrationExtensions
    {
        interface IEventStoreSession<TSessionInterface, TReaderInterface> : IEventStoreSession {}

        class EventStore<TSessionInterface, TReaderInterface> : EventStore, IEventStore<TSessionInterface, TReaderInterface>
        {
            public EventStore(string connectionString,
                                       IEventStoreEventSerializer serializer,
                                       ISingleContextUseGuard usageGuard = null,
                                       EventCache cache = null,
                                       IEventNameMapper nameMapper = null,
                                       IEnumerable<IEventMigration> migrations = null) : base(connectionString, serializer, usageGuard, cache, nameMapper, migrations) {}
        }

        class InMemoryEventStore<TSessionInterface, TReaderInterface> : InMemoryEventStore, IEventStore<TSessionInterface, TReaderInterface>
        {
            public InMemoryEventStore(IEnumerable<IEventMigration> migrations = null) : base(migrations) {}
        }

        [UsedImplicitly] class EventStoreSession<TSessionInterface, TReaderInterface> : EventStoreSession, IEventStoreSession<TSessionInterface, TReaderInterface>
        {
            public EventStoreSession(IServiceBus bus,
                                     IEventStore<TSessionInterface, TReaderInterface> store,
                                     ISingleContextUseGuard usageGuard,
                                     IUtcTimeTimeSource timeSource) : base(bus, store, usageGuard, timeSource) {}
        }

        public static void RegisterSqlServerEventStore<TSessionInterface, TReaderInterface>(this IDependencyInjectionContainer @this,
                                                                                            string connectionName,
                                                                                            IEnumerable<IEventMigration> migrations = null)
            where TSessionInterface : IEventStoreSession
            where TReaderInterface : IEventStoreReader
        {
            Contract.Argument(() => connectionName)
                    .NotNullEmptyOrWhiteSpace();

            GeneratedLowLevelInterfaceInspector.InspectInterfaces(Seq.OfTypes<TSessionInterface, TReaderInterface>());

            var cache = new EventCache();

            if(@this.RunMode().IsTesting)
            {
                @this.Register(Component.For<IEventStore<TSessionInterface, TReaderInterface>>()
                                               .UsingFactoryMethod(sl => new InMemoryEventStore<TSessionInterface, TReaderInterface>(migrations: migrations))
                                               .LifestyleSingleton());
            } else
            {
                @this.Register(Component.For<IEventStore<TSessionInterface, TReaderInterface>>()
                                        .UsingFactoryMethod(sl => new EventStore<TSessionInterface, TReaderInterface>(
                                                                                                                               connectionString: sl.Resolve<IConnectionStringProvider>()
                                                                                                                                                   .GetConnectionString(connectionName)
                                                                                                                                                   .ConnectionString,
                                                                                                                               serializer: sl.Resolve<IEventStoreEventSerializer>(),
                                                                                                                               migrations: migrations,
                                                                                                                               cache: cache))
                                        .LifestyleScoped());
            }

            @this.Register(Component.For<IEventStoreSession<TSessionInterface, TReaderInterface>, IUnitOfWorkParticipant>()
                                           .ImplementedBy<EventStoreSession<TSessionInterface, TReaderInterface>>()
                                           .LifestyleScoped());

            @this.Register(Component.For<TSessionInterface>(Seq.OfTypes<TReaderInterface>())
                                           .UsingFactoryMethod(locator => CreateProxyFor<TSessionInterface, TReaderInterface>(locator.Resolve<IEventStoreSession<TSessionInterface, TReaderInterface>>()))
                                           .LifestyleScoped());
        }

        static TSessionInterface CreateProxyFor<TSessionInterface, TReaderInterface>(IEventStoreSession session)
            where TSessionInterface : IEventStoreSession
            where TReaderInterface : IEventStoreReader
        {
            var sessionType = EventStoreSessionProxyFactory<TSessionInterface, TReaderInterface>.ProxyType;
            return (TSessionInterface)Activator.CreateInstance(sessionType, new IInterceptor[] {}, session);
        }

        //Using a generic class this way allows us to bypass any need for dictionary lookups or similar giving us excellent performance.
        static class EventStoreSessionProxyFactory<TSessionInterface, TReaderInterface>
            where TSessionInterface : IEventStoreSession
            where TReaderInterface : IEventStoreReader
        {
            internal static readonly Type ProxyType = new DefaultProxyBuilder().CreateInterfaceProxyTypeWithTargetInterface(interfaceToProxy: typeof(IEventStoreSession),
                                                                                                                            additionalInterfacesToProxy: new[]
                                                                                                                                                         {
                                                                                                                                                             typeof(TSessionInterface),
                                                                                                                                                             typeof(TReaderInterface)
                                                                                                                                                         },
                                                                                                                            options: ProxyGenerationOptions.Default);
        }
    }
}

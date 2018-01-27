using System;
using Castle.DynamicProxy;
using Composable.Contracts;
using Composable.DDD;
using Composable.Functional;
using Composable.GenericAbstractions.Time;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.Persistence.DocumentDb;
using Composable.Persistence.DocumentDb.SqlServer;
using Composable.Persistence.EventStore;
using Composable.System.Configuration;
using Composable.System.Data.SqlClient;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;
using JetBrains.Annotations;
// ReSharper disable UnusedTypeParameter the type parameters allow non-ambiguous registrations in the container. They are in fact used.

namespace Composable.DependencyInjection.Persistence
{
    public static class DocumentDbRegistrationExtensions
    {
        internal interface IDocumentDb<TUpdater, TReader, TBulkReader> : IDocumentDb
        {
        }

        [UsedImplicitly] class SqlServerDocumentDb<TUpdater, TReader, TBulkReader> : SqlServerDocumentDb, IDocumentDb<TUpdater, TReader, TBulkReader>
        {
            public SqlServerDocumentDb(ISqlConnection connection, IUtcTimeTimeSource timeSource) : base(connection, timeSource)
            {
            }
        }

        [UsedImplicitly] class InMemoryDocumentDb<TUpdater, TReader, TBulkReader> : InMemoryDocumentDb, IDocumentDb<TUpdater, TReader, TBulkReader>
        {
        }

        internal interface IDocumentDbSession<TUpdater, TReader, TBulkReader> : IDocumentDbSession { }

        [UsedImplicitly] class DocumentDbSession<TUpdater, TReader, TBulkReader> : DocumentDbSession, IDocumentDbSession<TUpdater, TReader, TBulkReader>
        {
            public DocumentDbSession(IDocumentDb<TUpdater, TReader, TBulkReader> backingStore, ISingleContextUseGuard usageGuard) : base(backingStore, usageGuard)
            {
            }
        }

        public static DocumentDbRegistrationBuilder RegisterSqlServerDocumentDb(this IDependencyInjectionContainer @this, string connectionName)
        {
            Contract.Argument(() => connectionName).NotNullEmptyOrWhiteSpace();

            if(@this.RunMode.IsTesting && @this.RunMode.TestingMode == TestingMode.InMemory)
            {
                @this.Register(Component.For<IDocumentDb>()
                                         .ImplementedBy<InMemoryDocumentDb>()
                                         .LifestyleSingleton()
                                         .DelegateToParentServiceLocatorWhenCloning());

            } else
            {
                @this.Register(Component.For<IDocumentDb>()
                                         .UsingFactoryMethod((ISqlConnectionProvider connectionProvider, IUtcTimeTimeSource timeSource) => new SqlServerDocumentDb(connectionProvider.GetConnectionProvider(connectionName), timeSource))
                                         .LifestyleSingleton());
            }


            @this.Register(Component.For<IDocumentDbSession>()
                                     .ImplementedBy<DocumentDbSession>()
                                     .LifestyleScoped());
            @this.Register(Component.For<IDocumentDbUpdater, IDocumentDbReader, IDocumentDbBulkReader>()
                                    .ImplementedBy<DocumentDbSession>()
                                    .LifestyleScoped()
                          );

            return new DocumentDbRegistrationBuilder();
        }

        public static void RegisterSqlServerDocumentDb<TUpdater, TReader, TBulkReader>(this IDependencyInjectionContainer @this,
                                                                                                 string connectionName)
            where TUpdater : class, IDocumentDbUpdater
            where TReader : IDocumentDbReader
            where TBulkReader : IDocumentDbBulkReader
        {
            Contract.Argument(() => connectionName)
                    .NotNullEmptyOrWhiteSpace();

            GeneratedLowLevelInterfaceInspector.InspectInterfaces(Seq.OfTypes<TUpdater, TReader, TBulkReader>());

            if(@this.RunMode.IsTesting && @this.RunMode.TestingMode == TestingMode.InMemory)
            {
                @this.Register(Component.For<IDocumentDb<TUpdater, TReader, TBulkReader>>()
                                         .ImplementedBy<InMemoryDocumentDb<TUpdater, TReader, TBulkReader>>()
                                         .LifestyleSingleton()
                                         .DelegateToParentServiceLocatorWhenCloning());

            } else
            {
                @this.Register(Component.For<IDocumentDb<TUpdater, TReader, TBulkReader>>()
                                         .UsingFactoryMethod((ISqlConnectionProvider connectionProvider, IUtcTimeTimeSource timeSource) => new SqlServerDocumentDb<TUpdater, TReader, TBulkReader>(connectionProvider.GetConnectionProvider(connectionName), timeSource))
                                         .LifestyleSingleton());
            }


            @this.Register(Component.For<IDocumentDbSession<TUpdater, TReader, TBulkReader>>()
                                     .ImplementedBy<DocumentDbSession<TUpdater, TReader, TBulkReader>>()
                                     .LifestyleScoped());
            @this.Register(Component.For<TUpdater, TReader, TBulkReader>()
                                    .UsingFactoryMethod(EventStoreSessionProxyFactory<TUpdater, TReader, TBulkReader>.ProxyType,
                                                        kernel => CreateProxyFor<TUpdater, TReader, TBulkReader>(kernel.Resolve<IDocumentDbSession<TUpdater, TReader, TBulkReader>>()))
                                    .LifestyleScoped()
                          );
        }

        static TUpdater CreateProxyFor<TUpdater, TReader, TBulkReader>(IDocumentDbSession session)
            where TUpdater : IDocumentDbUpdater
            where TReader : IDocumentDbReader
            where TBulkReader : IDocumentDbBulkReader
        {
            var sessionType = EventStoreSessionProxyFactory<TUpdater, TReader, TBulkReader>.ProxyType;
            return (TUpdater)Activator.CreateInstance(sessionType, new IInterceptor[] {}, session);
        }

        //Using a generic class this way allows us to bypass any need for dictionary lookups or similar giving us excellent performance.
        static class EventStoreSessionProxyFactory<TUpdater, TReader, TBulkReader>
            where TUpdater : IDocumentDbUpdater
            where TReader : IDocumentDbReader
            where TBulkReader : IDocumentDbBulkReader
        {
            internal static readonly Type ProxyType =
                new DefaultProxyBuilder()
                    .CreateInterfaceProxyTypeWithTargetInterface(interfaceToProxy: typeof(IDocumentDbSession),
                                                                 additionalInterfacesToProxy: new[]
                                                                                              {
                                                                                                  typeof(TUpdater),
                                                                                                  typeof(TReader),
                                                                                                  typeof(TBulkReader)
                                                                                              },
                                                                 options: ProxyGenerationOptions.Default);
        }
    }

    public class DocumentDbRegistrationBuilder
    {
        public DocumentDbRegistrationBuilder HandleDocumentType<TDocument>(MessageHandlerRegistrarWithDependencyInjectionSupport registrar)
        {
            TryGet<TDocument>(registrar);
            Get<TDocument>(registrar);
            Save<TDocument>(registrar);
            GetForUpdate<TDocument>(registrar);
            return this;
        }

        static void Save<TDocument>(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForCommand(
            (DocumentDbApi.Command.SaveDocument<TDocument> command, IDocumentDbUpdater updater) => updater.Save(command.Key, command.Entity));

        static void GetForUpdate<TDocument>(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
            (DocumentDbApi.Query.GetDocumentForUpdate<TDocument> query, IDocumentDbUpdater updater) => updater.GetForUpdate<TDocument>(query.Id));

        static void TryGet<TDocument>(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
            (DocumentDbApi.Query.TryGetDocument<TDocument> query, IDocumentDbReader updater) => updater.TryGet<TDocument>(query.Id, out var document) ? Option.Some(document) : Option.None<TDocument>());

        static void Get<TDocument>(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
            (DocumentDbApi.Query.GetReadonlyCopyOfDocument<TDocument> query, IDocumentDbReader reader) => reader.Get<TDocument>(query.Id));
    }
}

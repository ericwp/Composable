﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.EventSourcing.MicrosoftSQLServer;
using Composable.CQRS.EventSourcing.Refactoring.Migrations;
using Composable.CQRS.EventSourcing.Refactoring.Naming;
using Composable.System;
using Composable.System.Configuration;
using Composable.System.Linq;
using Composable.UnitsOfWork;
using Composable.Windsor.Testing;

namespace Composable.CQRS.Windsor
{
    public abstract class SqlServerEventStoreRegistration
    {        
        protected SqlServerEventStoreRegistration(string description, Type sessionImplementor, Type sessionType, Type readerType)
        {
            Contract.Requires(!description.IsNullOrWhiteSpace());

            SessionType = sessionType;
            ReaderType = readerType;
            SessionImplementor = sessionImplementor;
            StoreName = $"{description}.Store";
            SessionName = $"{description}.Session";
        }

        internal Type ReaderType { get; }
        internal Type SessionType { get; }
        internal Type SessionImplementor { get; private set; }
        internal string StoreName { get; }
        internal string SessionName { get; }
        public virtual Dependency Store => Dependency.OnComponent(typeof(IEventStore), componentName: StoreName);
        public virtual Dependency Session => Dependency.OnComponent(SessionType, componentName: SessionName);
        public virtual Dependency Reader => Dependency.OnComponent(ReaderType, componentName: SessionName);

    }

    public class SqlServerEventStoreRegistration<TFactory> : SqlServerEventStoreRegistration
    {
        public SqlServerEventStoreRegistration() : base(typeof(TFactory).FullName,sessionImplementor: typeof(EventStoreSession), sessionType: typeof(IEventStoreSession), readerType: typeof(IEventStoreReader)) {}
    }

    public class SqlServerEventStoreRegistration<TSessionClass, TSessionInterface, TReaderInterface> : SqlServerEventStoreRegistration
        where TSessionClass : EventStoreSession
        where TSessionInterface : IEventStoreSession
        where TReaderInterface : IEventStoreReader
    {
        public SqlServerEventStoreRegistration() : base(typeof(TSessionClass).FullName, sessionImplementor: typeof(TSessionClass), sessionType: typeof(TSessionInterface), readerType: typeof(TReaderInterface)) { }
    }

    public static class SqlServerEventStoreRegistrationExtensions
    {
        public static SqlServerEventStoreRegistration RegisterSqlServerEventStore
            (this IWindsorContainer @this,
             SqlServerEventStoreRegistration registration,
             string connectionName,
             Dependency nameMapper = null,
             Dependency migrations = null)
        {
            Contract.Requires(registration != null);
            Contract.Requires(!connectionName.IsNullOrWhiteSpace());

            nameMapper = nameMapper ?? Dependency.OnValue<IEventNameMapper>(null);//We don't want to get any old name mapper that might have been registered by someone else.
            migrations = migrations ?? Dependency.OnValue<IEnumerable<IEventMigration>>(null); //We don't want to get any old migrations array that might have been registered by someone else.

            var connectionString = Dependency.OnValue(typeof(string),@this.Resolve<IConnectionStringProvider>().GetConnectionString(connectionName).ConnectionString);

            @this.Register(
                Component.For<IEventStore>()
                         .ImplementedBy<SqlServerEventStore>()
                         .DependsOn(connectionString, nameMapper, migrations)
                    .LifestylePerWebRequest()
                    .Named(registration.StoreName),
                Component.For(Seq.Create(registration.SessionType, registration.ReaderType, typeof(IUnitOfWorkParticipant)))
                         .ImplementedBy(registration.SessionImplementor)
                         .DependsOn(registration.Store)
                         .LifestylePerWebRequest()
                         .Named(registration.SessionName)
                );

            @this.WhenTesting()
                 .ReplaceEventStore(registration.StoreName);

            return registration;
        }
    }
}

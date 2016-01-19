﻿using AccountManagement.Domain.Events.EventStore.ContainerInstallers;
using AccountManagement.Domain.Services;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Composable.CQRS.Windsor;
using Composable.KeyValueStorage;
using Composable.KeyValueStorage.SqlServer;
using Composable.System.Configuration;
using JetBrains.Annotations;

namespace AccountManagement.Domain.ContainerInstallers
{
    [UsedImplicitly]
    public class AccountManagementDomainQuerymodelsSessionInstaller : IWindsorInstaller
    {
        public static string ConnectionStringName { get { return AccountManagementDomainEventStoreInstaller.ConnectionStringName; } }

        public static readonly SqlServerDocumentDbRegistration Registration = new SqlServerDocumentDbRegistration<AccountManagementDomainQuerymodelsSessionInstaller>();

        public void Install(
            IWindsorContainer container,
            IConfigurationStore store)
        {
            container.RegisterSqlServerDocumentDb(Registration, ConnectionStringName);

            container.Register(
                Component.For<IAccountManagementDomainQueryModelSession>()
                    .ImplementedBy<AccountManagementDomainQueryModelSession>()
                    .DependsOn(
                        Registration.DocumentDb,
                        Dependency.OnValue<IDocumentDbSessionInterceptor>(NullOpDocumentDbSessionInterceptor.Instance))
                    .LifestylePerWebRequest()
                );
        }

        private static string GetConnectionStringFromConfiguration(string key)
        {
            return new ConnectionStringConfigurationParameterProvider().GetConnectionString(key).ConnectionString;
        }
    }
}

﻿using AccountManagement.Domain.Events.EventStore.Services;
using Composable.DependencyInjection;
using Composable.Windsor;
using Composable.Windsor.Persistence;

namespace AccountManagement.Domain.Events.EventStore.ContainerInstallers
{
    static class AccountManagementDomainEventStoreInstaller
    {
        internal const string ConnectionStringName = "AccountManagementDomain";

        internal static void Install(IDependencyInjectionContainer container)
        {
            container.Unsupported().RegisterSqlServerEventStore<
                IAccountManagementEventStoreSession,
                IAccountManagementEventStoreReader>(ConnectionStringName);
        }
    }
}

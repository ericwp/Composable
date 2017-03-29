﻿using AccountManagement.Domain.Events.EventStore.ContainerInstallers;
using AccountManagement.Domain.Services;
using Composable.DependencyInjection;
using Composable.DependencyInjection.Persistence;

namespace AccountManagement.Domain.ContainerInstallers
{
    static class AccountManagementDomainQuerymodelsSessionInstaller
    {
        internal static void Install(IDependencyInjectionContainer container)
        {
            container
                .RegisterSqlServerDocumentDb<
                    IAccountManagementDomainDocumentDbUpdater,
                    IAccountManagementDomainDocumentDbReader,
                    IAccountManagementDomainDocumentDbBulkReader>(AccountManagementDomainEventStoreInstaller
                                                                .ConnectionStringName);
        }
    }
}

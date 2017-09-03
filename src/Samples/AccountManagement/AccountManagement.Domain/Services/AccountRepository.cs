﻿using Composable.Persistence.DocumentDb;
using Composable.Persistence.EventStore;
using JetBrains.Annotations;
using AccountEvent = AccountManagement.Domain.Events.AccountEvent;

namespace AccountManagement.Domain.Services
{
    interface IAccountManagementDomainDocumentDbUpdater : IDocumentDbUpdater { }

    interface IAccountManagementDomainDocumentDbReader : IDocumentDbReader { }

    interface IAccountManagementDomainDocumentDbBulkReader : IDocumentDbBulkReader { }

    [UsedImplicitly] class AccountRepository : AggregateRepository<Account, AccountEvent.Implementation.Root, AccountEvent.Root>, IAccountRepository
    {
        public AccountRepository(IAccountManagementEventStoreUpdater aggregates, IAccountManagementEventStoreReader reader) : base(aggregates, reader) {}
    }
}

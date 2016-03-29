﻿using AccountManagement.Domain.Events;
using AccountManagement.Domain.Events.EventStore.Services;
using AccountManagement.Domain.Events.Implementation;
using Composable.CQRS;
using JetBrains.Annotations;

namespace AccountManagement.Domain.Services
{
    [UsedImplicitly]
    internal class AccountRepository : AggregateRepository<Account, AccountEvent, IAccountEvent>, IAccountRepository
    {
        public AccountRepository(IAccountManagementEventStoreSession aggregates) : base(aggregates) {}
    }
}

﻿using AccountManagement.Domain.Events.PropertyUpdated;
using AccountManagement.Domain.Services;
using AccountManagement.Domain.Shared;
using NServiceBus;

namespace AccountManagement.Domain.QueryModels.Updaters
{
    public class EmailToAccountUpdater : IHandleMessages<IAccountEmailPropertyUpdatedEvent>
    {
        private readonly IAccountManagementDomainQueryModelSession _querymodels;
        private readonly IAccountManagementEventStoreSession _aggregates;

        public EmailToAccountUpdater(IAccountManagementDomainQueryModelSession querymodels, IAccountManagementEventStoreSession aggregates)
        {
            _querymodels = querymodels;
            _aggregates = aggregates;
        }

        public void Handle(IAccountEmailPropertyUpdatedEvent message)
        {
            var previousAccountVersion = _aggregates.LoadSpecificVersion<Account>(message.AggregateRootId, message.AggregateRootVersion - 1);
            var previousEmail = previousAccountVersion.Email;
            var newEmail = message.Email;

            if(previousEmail != null)
            {
                GetOrCreateAccountToEmailMap(previousEmail).RemoveAccount(message.AggregateRootId);
            }
            GetOrCreateAccountToEmailMap(newEmail).AddAccount(message.AggregateRootId);
        }

        private EmailToAccountMap GetOrCreateAccountToEmailMap(Email email)
        {
            EmailToAccountMap found;
            if(_querymodels.TryGet(email, out found))
            {
                return found;
            }
            
            found = new EmailToAccountMap(email);
            _querymodels.Save(email, found);

            return found;
        }
    }
}
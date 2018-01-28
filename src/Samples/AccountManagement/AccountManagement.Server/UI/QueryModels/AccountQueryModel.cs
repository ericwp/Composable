﻿using System;
using System.Collections.Generic;
using System.Linq;
using AccountManagement.API;
using AccountManagement.Domain;
using AccountManagement.Domain.Events;
using AccountManagement.Domain.Passwords;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Query.Models.SelfGeneratingQueryModels;

namespace AccountManagement.UI.QueryModels
{
    class AccountQueryModel : SelfGeneratingQueryModel<AccountQueryModel, AccountEvent.Root>, IAccountResourceData
    {
        public Password Password { get; private set; }
        public Email Email { get; private set; }

        public AccountQueryModel(IEnumerable<AccountEvent.Root> events)
        {
            RegisterEventAppliers()
               .For<AccountEvent.PropertyUpdated.Email>(@event => Email = @event.Email)
               .For<AccountEvent.PropertyUpdated.Password>(@event => Password = @event.Password);

            LoadFromHistory(events);
        }


        public static void GetById(MessageHandlerRegistrarWithDependencyInjectionSupport registrar) => registrar.ForQuery(
            (BusApi.Local.Queries.EntityQuery<AccountQueryModel> query, IEventStoreReader eventStoreReader) =>
                new AccountQueryModel(eventStoreReader.GetHistory(query.EntityId).Cast<AccountEvent.Root>()));


        internal class Api
        {
            internal Query Queries => new Query();
            internal class Query
            {
                public BusApi.Local.Queries.EntityQuery<AccountQueryModel> Get(Guid id) => new BusApi.Local.Queries.EntityQuery<AccountQueryModel>(id);
            }
        }
    }
}

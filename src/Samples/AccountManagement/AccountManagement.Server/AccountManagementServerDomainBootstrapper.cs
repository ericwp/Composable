﻿using System;
using AccountManagement.Domain;
using AccountManagement.Domain.QueryModels;
using AccountManagement.UI.QueryModels;
using AccountManagement.UI.QueryModels.Services;
using AccountManagement.UI.QueryModels.Services.Implementation;
using Composable.DependencyInjection;
using Composable.DependencyInjection.Persistence;
using Composable.Messaging.Buses;
using Composable.Messaging.Buses.Implementation;
using Composable.Refactoring.Naming;

namespace AccountManagement
{
    public static class AccountManagementServerDomainBootstrapper
    {

        public static IEndpoint RegisterWith(IEndpointHost host)
        {
            return host.RegisterAndStartEndpoint("AccountManagement",
                                                 new EndpointId(Guid.Parse("1A1BE9C8-C8F6-4E38-ABFB-F101E5EDB00D")),
                                                 builder =>
                                                 {
                                                     MapTypes(builder.TypeMapper);
                                                     RegisterDomainComponents(builder.Container, builder.Configuration);
                                                     RegisterUserInterfaceComponents(builder.Container, builder.Configuration);

                                                     RegisterHandlers(builder.RegisterHandlers);
                                                 });
        }

        static void RegisterDomainComponents(IDependencyInjectionContainer container, EndpointConfiguration configuration)
        {
            container.RegisterSqlServerEventStore(configuration.ConnectionStringName);
            container.RegisterSqlServerDocumentDb(configuration.ConnectionStringName);
        }

        static void RegisterUserInterfaceComponents(IDependencyInjectionContainer container, EndpointConfiguration configuration)
        {
            container.RegisterSqlServerDocumentDb<IAccountManagementUiDocumentDbUpdater, IAccountManagementUiDocumentDbReader, IAccountManagementUiDocumentDbBulkReader>(configuration.ConnectionStringName);

            AccountManagementQueryModelReader.RegisterWith(container);
            AccountQueryModel.Generator.RegisterWith(container);
        }

        static void RegisterHandlers(MessageHandlerRegistrarWithDependencyInjectionSupport registrar)
        {
            Account.UIAdapter.GetById(registrar);
            Account.UIAdapter.Register(registrar);
            Account.UIAdapter.ChangeEmail(registrar);
            Account.UIAdapter.ChangePassword(registrar);
            Account.UIAdapter.Login(registrar);

            Account.InternalServices.GetById(registrar);
            Account.InternalServices.Save(registrar);
            Account.InternalServices.GetReadonlyCopyOfLatestVersion(registrar);
            Account.InternalServices.GetReadonlyCopyOfSpecificVersion(registrar);

            EmailToAccountMapper.UpdateMappingWhenEmailChanges(registrar);
            EmailToAccountMapper.TryGetAccountByEmail(registrar);
        }


        //In order to allow you to rename types when you need to composable does not use type names in the persisted data in the event store, document database or the service bus.
        //In order to enable you to freely rename and move types you must map each concrete type to a unique Guid.
        //To make this as easy as possible for you Composable will detect missing mappings and throw an exception telling exactly which lines of code you need to paste into the method below. 
        //The lines you see here are pasted directly from the message in such an exception.
        static void MapTypes(ITypeMappingRegistar typeMapper)
        {
            typeMapper
               .Map<AccountManagement.API.AccountResource.Commands.ChangeEmail>("f38f0473-e0cc-4ef7-9ff6-4e99da03a39e")
               .Map<AccountManagement.API.AccountResource.Commands.Register>("1C8342B3-1302-40D1-BD54-1333A47F756F")
               .Map<AccountManagement.API.AccountResource.Commands.ChangePassword>("077F075B-64A3-4E02-B435-F04B19F6C98D")
               .Map<AccountManagement.API.AccountResource.Commands.LogIn.UI>("90689406-de88-43da-be17-0fb93692f6ad")
               .Map<Composable.Messaging.EntityByIdQuery<AccountManagement.API.AccountResource>>("bc9a5aa6-bbbc-4e0f-841f-ad77d40a483f")
               .Map<AccountManagement.PrivateAccountApi.Query.TryGetByEmailQuery>("4cf7d647-e5cf-4961-989c-e9f128207a9e")
               .Map<AccountManagement.Domain.Account>("c2ca53e0-ee6d-4725-8bf8-c13b680d0ac5")
               .Map<AccountManagement.Domain.Events.AccountEvent.Created>("3eb16cfa-ee90-4bec-a4fd-d6c52ebe0bbf")
               .Map<AccountManagement.Domain.Events.AccountEvent.Implementation.LoggedIn>("e4cb1903-4e51-44f2-b866-43891d86cf94")
               .Map<AccountManagement.Domain.Events.AccountEvent.Implementation.LoginFailed>("a659a369-584c-41e1-99ae-782b8a053b38")
               .Map<AccountManagement.Domain.Events.AccountEvent.Implementation.Root>("7a98ea5a-aa91-43d2-b1bc-1b0d28842750")
               .Map<AccountManagement.Domain.Events.AccountEvent.Implementation.UserChangedEmail>("4cc87a2c-3149-4748-87fe-1fde17b7473d")
               .Map<AccountManagement.Domain.Events.AccountEvent.Implementation.UserChangedPassword>("eea04c7c-b51e-4669-947f-beb0f6b3fad6")
               .Map<AccountManagement.Domain.Events.AccountEvent.Implementation.UserRegistered>("14d51523-1ede-41b4-aaef-6fde43f45d28")
               .Map<AccountManagement.Domain.Events.AccountEvent.LoggedIn>("86761d19-29a5-4e88-9e02-6b17ce5d7be0")
               .Map<AccountManagement.Domain.Events.AccountEvent.LoginFailed>("45db94ed-7114-47cd-82a5-3ea4cfdad975")
               .Map<AccountManagement.Domain.Events.AccountEvent.PropertyUpdated.Email>("426f6b93-7af0-43b2-96ff-ddb613442e95")
               .Map<AccountManagement.Domain.Events.AccountEvent.PropertyUpdated.Password>("d5666a12-33ab-489d-8b94-fddf4d2e7a15")
               .Map<AccountManagement.Domain.Events.AccountEvent.Root>("86a2ff9c-c558-43ce-8a87-efaf49915275")
               .Map<AccountManagement.Domain.Events.AccountEvent.UserChangedEmail>("1fe10abb-25b5-4243-b148-439b435002a5")
               .Map<AccountManagement.Domain.Events.AccountEvent.UserChangedPassword>("0f7e4685-20d6-4f3e-ab32-9d153bbdbfee")
               .Map<AccountManagement.Domain.Events.AccountEvent.UserRegistered>("2c648c9f-4860-46e3-a672-6d81ea35cd3f")

               //todo: Having to do this for interal stuff makes no sense at all.
               .Map<Composable.Messaging.EntityByIdQuery<AccountManagement.Domain.Account>>("e5648606-0d8b-450e-8eec-a66899b7e12a")
               .Map<Composable.Messaging.PersistEntityCommand<AccountManagement.Domain.Account>>("01b66bcb-1a39-4bb0-8344-444349ee0790")
               .Map<Composable.Messaging.ReadonlyCopyOfEntityByIdQuery<AccountManagement.Domain.Account>>("b57e938c-254e-4f0a-9afd-b4cfef228189")
               .Map<Composable.Messaging.ReadonlyCopyOfEntityVersionByIdQuery<AccountManagement.Domain.Account>>("9b5ef7b2-cbc6-46a5-a1d0-2400b1cc0951");
        }
    }
}

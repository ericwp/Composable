﻿using AccountManagement.API;
using AccountManagement.Domain.Services;
using Composable.Messaging;
using Composable.Messaging.Buses;

namespace AccountManagement.Domain
{
    partial class Account
    {
        internal static class MessageHandlers
        {
            public static void RegisterHandlers(MessageHandlerRegistrarWithDependencyInjectionSupport registrar)
            {
                registrar.ForQuery((EntityQuery<AccountResource> accountQuery, IAccountRepository repository) => new AccountResource(repository.GetReadonlyCopy(accountQuery.Id)));

                registrar.ForCommandWithResult((AccountResource.Command.Register.DomainCommand command, IDuplicateAccountChecker duplicateChecker, IAccountRepository repository) => new AccountResource(Account.Register(command, repository, duplicateChecker)));

                registrar.ForCommand((AccountResource.Command.ChangeEmail.UI command, IAccountRepository repository) => repository.Get(command.AccountId).ChangeEmail(command.ToDomainCommand()))
                    .ForCommand((AccountResource.Command.ChangePassword.Domain command, IAccountRepository repository) => repository.Get(command.AccountId).ChangePassword(command));
            }
        }
    }
}

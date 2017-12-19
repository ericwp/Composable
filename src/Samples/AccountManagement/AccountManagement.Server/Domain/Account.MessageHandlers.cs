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
                registrar
                    .ForQuery((EntityQuery<AccountResource> accountQuery, IAccountRepository repository) => new AccountResource(repository.GetReadonlyCopy(accountQuery.Id)))
                    .ForCommandWithResult((AccountResource.RegisterAccountUICommand command, IDuplicateAccountChecker duplicateChecker, IAccountRepository repository) => Register(command, repository, duplicateChecker))
                    .ForCommand((AccountResource.ChangeEmailCommand command, IAccountRepository repository) => repository.Get(command.AccountId).ChangeEmail(Email.Parse(command.Email)))
                    .ForCommand((AccountResource.ChangePasswordCommand command, IAccountRepository repository) =>
                                    repository.Get(command.AccountId).ChangePassword(command.OldPassword, new Password(command.NewPassword)));
            }

            static AccountResource Register(AccountResource.RegisterAccountUICommand uiCommand, IAccountRepository repository, IDuplicateAccountChecker duplicateChecker)
                => new AccountResource(Account.Register(Email.Parse(uiCommand.Email), new Password(uiCommand.Password), uiCommand.AccountId, repository, duplicateChecker));
        }
    }
}

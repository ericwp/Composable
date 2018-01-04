﻿using System;
using AccountManagement.API;
using Composable;
using Composable.Contracts;
using Composable.Messaging.Commands;
using JetBrains.Annotations;

namespace AccountManagement.Domain
{
    partial class Account
    {
        internal static class Command
        {
            [TypeId("B0CAD429-295D-43E7-8441-566B7887C7F0")]internal class Register : TransactionalExactlyOnceDeliveryCommand<AccountResource.Command.Register.RegistrationAttemptResult>
            {
                [UsedImplicitly] Register() { }
                public Register(Guid accountId, Password password, Email email)
                {
                    OldContract.Argument(() => accountId, () => password, () => email).NotNullOrDefault();
                    AccountId = accountId;
                    Password = password;
                    Email = email;
                }

                public Guid AccountId { get; private set; }
                public Password Password { get; private set; }
                public Email Email { get; private set; }
            }

            [TypeId("0EAC052B-3185-4AAA-9267-5073EEE15D5A")]internal class ChangeEmail : TransactionalExactlyOnceDeliveryCommand
            {
                [UsedImplicitly] ChangeEmail() {}
                public ChangeEmail(Guid accountId, Email email)
                {
                    OldContract.Argument(() => accountId, () => email).NotNullOrDefault();

                    AccountId = accountId;
                    Email = email;
                }
                public ChangeEmail(AccountResource.Command.ChangeEmail uiCommand):this(uiCommand.AccountId, Email.Parse(uiCommand.Email)){}

                public Guid AccountId { get; private set; }
                public Email Email { get; private set; }
            }

            [TypeId("F9074BCF-39B3-4C76-993A-9C27F3E71279")]internal class ChangePassword : TransactionalExactlyOnceDeliveryCommand
            {
                [UsedImplicitly] ChangePassword() {}
                public ChangePassword(Guid accountId, string oldPassword, Password newPassword)
                {
                    OldContract.Argument(() => accountId, () => newPassword).NotNullOrDefault();
                    OldContract.Argument(() => oldPassword).NotNullEmptyOrWhiteSpace();

                    AccountId = accountId;
                    OldPassword = oldPassword;
                    NewPassword = newPassword;
                }

                public Guid AccountId { get; private set; }
                public string OldPassword { get; private set; }
                public Password NewPassword { get; private set; }
            }

            [TypeId("14B6DD28-205B-42ED-9CF4-20D6B0299632")]internal class Login : TransactionalExactlyOnceDeliveryCommand<AccountResource.Command.LogIn.LoginAttemptResult>
            {
                internal Login(Email email, string password)
                {
                    OldContract.Argument(() => email).NotNullOrDefault();
                    OldContract.Argument(() => password).NotNullEmptyOrWhiteSpace();

                    Email = email;
                    Password = password;
                }

                public Email Email { get; }
                public string Password { get; }
            }
        }
    }
}
﻿using System;
using System.ComponentModel.DataAnnotations;
using AccountManagement.API.ValidationAttributes;
using AccountManagement.Domain;
using Composable;
using Composable.Contracts;
using Composable.Messaging.Commands;
using JetBrains.Annotations;

namespace AccountManagement.API
{
    public partial class AccountResource
    {
        public static partial class Command
        {
            public static class ChangeEmail
            {
                [TypeId("0EAC052B-3185-4AAA-9267-5073EEE15D5A")]internal class Domain : TransactionalExactlyOnceDeliveryCommand
                {
                    [UsedImplicitly] Domain() {}
                    public Domain(Guid accountId, Email email)
                    {
                        OldContract.Argument(() => accountId, () => email).NotNullOrDefault();

                        AccountId = accountId;
                        Email = email;
                    }

                    public Guid AccountId { get; private set; }
                    public Email Email { get; private set; }
                }

                [TypeId("746695CC-B3CB-4620-A622-F6831AA4C5F3")]public class UI : TransactionalExactlyOnceDeliveryCommand
                {
                    [UsedImplicitly] UI() {}
                    public UI(Guid accountId) => AccountId = accountId;

                    [Required] [EntityId] public Guid AccountId { get; set; }
                    [Required] [Email] public string Email { get; set; }

                    internal Domain ToDomainCommand() => new Domain(AccountId, AccountManagement.Domain.Email.Parse(Email));
                }
            }
        }
    }
}
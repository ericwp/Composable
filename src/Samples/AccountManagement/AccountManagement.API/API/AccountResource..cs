﻿using AccountManagement.Domain;
using AccountManagement.Domain.Passwords;
using Composable.Messaging;
using JetBrains.Annotations;

namespace AccountManagement.API
{
    public partial class AccountResource : BusApi.Response.Entity<AccountResource>
    {
        [UsedImplicitly] AccountResource() {}

        internal AccountResource(IAccountResourceData account) : base(account.Id)
        {
            Commands = new CommandsCollection(this);
            Email = account.Email;
            Password = account.Password;
        }

        public Email Email { get; private set; }
        public Password Password { get; private set; }

        public CommandsCollection Commands { get; private set; }
    }
}

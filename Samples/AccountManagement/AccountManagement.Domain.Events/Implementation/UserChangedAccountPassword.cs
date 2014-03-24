﻿using AccountManagement.Domain.Shared;
using Composable.CQRS.EventSourcing;

namespace AccountManagement.Domain.Events.Implementation
{
    public class UserChangedAccountPassword : AggregateRootEvent, IUserChangedAccountPasswordEvent
    {
        public UserChangedAccountPassword(Password password)
        {
            Password = password;
        }

        public Password Password { get; private set; }
    }
}
﻿using AccountManagement.Domain.Shared;
using JetBrains.Annotations;

namespace AccountManagement.Domain.Events.Implementation
{
    public class UserChangedAccountEmailEvent : AccountEvent, IUserChangedAccountEmailEvent
    {
        [UsedImplicitly] //ncrunch: no coverage
        public UserChangedAccountEmailEvent() {} //ncrunch: no coverage

        public UserChangedAccountEmailEvent(Email email)
        {
            Email = email;
        }

        public Email Email { get; private set; }
    }
}

﻿using System;
using AccountManagement.Domain.Shared;
using JetBrains.Annotations;

namespace AccountManagement.Domain.Events.Implementation
{
    public class UserChangedAccountPassword : AccountEvent, IUserChangedAccountPasswordEvent
    {
        [Obsolete("NServicebus requires this constructor to exist.", true), UsedImplicitly] //ncrunch: no coverage
        public UserChangedAccountPassword() {} //ncrunch: no coverage

        public UserChangedAccountPassword(Password password)
        {
            Password = password;
        }

        public Password Password { get; private set; }
    }
}

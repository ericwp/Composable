﻿using System;
using Composable.Contracts;
using JetBrains.Annotations;

namespace AccountManagement.Domain.QueryModels
{
    //todo: Hmm, does not use the account id, so what exactly is this for? Does not seem to match the name.
    class EmailExistsQueryModel
    {
        EmailExistsQueryModel() { }

        public EmailExistsQueryModel(Email email, Guid accountId)
        {
            OldContract.Argument(() => email, () => accountId).NotNullOrDefault();

            Email = email;
        }

        Email Email { [UsedImplicitly] get; set; }
    }
}

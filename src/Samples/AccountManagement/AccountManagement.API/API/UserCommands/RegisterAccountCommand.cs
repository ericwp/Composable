﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AccountManagement.API.ValidationAttributes;
using Composable.Messaging.Commands;
using JetBrains.Annotations;

namespace AccountManagement.API.UserCommands
{
    public class RegisterAccountCommand : DomainCommand<AccountResource>, IValidatableObject
    {
        public RegisterAccountCommand() {}
        public RegisterAccountCommand(Guid accountId, string email, string password)
        {
            AccountId = accountId;
            Email = email;
            Password = password;
        }

        //Note the use of a custom validation attribute.
        [Required(ErrorMessageResourceType = typeof(RegisterAccountCommandResources), ErrorMessageResourceName = "IdInvalid")]
        [EntityId(ErrorMessageResourceType = typeof(RegisterAccountCommandResources), ErrorMessageResourceName = "IdMissing")]
        public Guid AccountId { [UsedImplicitly] get; set; } = Guid.NewGuid();

        //Note the use of a custom validation attribute.
        [Email(ErrorMessageResourceType = typeof(RegisterAccountCommandResources), ErrorMessageResourceName = "EmailInvalid")]
        [Required(ErrorMessageResourceType = typeof(RegisterAccountCommandResources), ErrorMessageResourceName = "EmailMissing")]
        public string Email { [UsedImplicitly] get; set; }

        [Required(ErrorMessageResourceType = typeof(RegisterAccountCommandResources), ErrorMessageResourceName = "PasswordMissing")]
        // ReSharper disable once MemberCanBePrivate.Global
        public string Password { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext) => Domain.Password.Validate(Password, this, () => Password);

    }
}

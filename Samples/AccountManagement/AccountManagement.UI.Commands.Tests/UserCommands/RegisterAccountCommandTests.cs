﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AccountManagement.UI.Commands.UserCommands;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.UI.Commands.Tests.UserCommands
{
    [TestFixture]
    public class RegisterAccountCommandTests
    {
        private RegisterAccountCommand _registerAccountCommand;

        [SetUp]
        public void CreateValidCommand()
        {
            _registerAccountCommand = new RegisterAccountCommand()
                       {
                           Email = "valid.email@google.com"
                       };
        }
        [Test]
        public void IsInvalidIfEmailIsNull()
        {
            _registerAccountCommand.Email = null;
            Validate(_registerAccountCommand).Should().NotBeEmpty();
        }

        [Test]
        public void IsInvalidIfEmailIsIncorrectFormat()
        {
            _registerAccountCommand.Email = "invalid";
            Validate(_registerAccountCommand).Should().NotBeEmpty();
        }

        private IEnumerable<ValidationResult> Validate(object command)
        {
            var context = new ValidationContext(command, serviceProvider: null, items: null);
            var results = new List<ValidationResult>();

            var isValid = Validator.TryValidateObject(command, context, results, validateAllProperties: true);
            return results;
        }
    }
}
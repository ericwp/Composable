﻿using System.Linq;
using AccountManagement.API;
using AccountManagement.Domain.Events;
using AccountManagement.Tests.Scenarios;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.Tests.Domain.After_a_user_has_registered_an_account
{
    [TestFixture]
    public class Then_ : DomainTestBase
    {
        AccountResource _registeredAccount;
        RegisterAccountScenario _registerAccountScenario;

        [SetUp]
        public void RegisterAccount()
        {
            _registerAccountScenario = new RegisterAccountScenario(ClientBus);
            _registeredAccount = _registerAccountScenario.Execute();
        }

        [Test]
        public void An_IUserRegisteredAccountEvent_is_published()
        {
            MessageSpy.DispatchedMessages.OfType<AccountEvent.UserRegistered>().ToList().Should().HaveCount(1);
        }

        [Test]
        public void AccountEmail_is_the_one_used_for_registration()
        {
            Assert.That(_registeredAccount.Email.ToString(), Is.EqualTo(_registerAccountScenario.UiCommand.Email));
        }

        [Test]
        public void AccountPassword_is_the_one_used_for_registration()
        {
            Assert.True(_registeredAccount.Password.IsCorrectPassword(_registerAccountScenario.UiCommand.Password));
        }
    }
}

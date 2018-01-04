﻿using System.Linq;
using AccountManagement.Domain.Events;
using AccountManagement.Tests.Scenarios;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.Tests.Domain.After_a_user_has_registered_an_account
{
    public class After_they_change_their_password : DomainTestBase
    {
        ChangePasswordScenario _changePasswordScenario;

        [SetUp]
        public void RegisterAccount()
        {
            _changePasswordScenario = ChangePasswordScenario.Create(ClientBus);
            _changePasswordScenario.Execute();
        }

        [Test]
        public void An_IUserChangedAccountPasswordEvent_is_published_on_the_bus()
        {
            MessageSpy.DispatchedMessages
                .OfType<AccountEvent.UserChangedPassword>()
                .Should().HaveCount(1);
        }

        [Test]
        public void Event_password_should_accept_the_used_password_as_valid()
        {
            MessageSpy.DispatchedMessages.OfType<AccountEvent.UserChangedPassword>()
                .Single().Password.AssertIsCorrectPassword(_changePasswordScenario.NewPasswordAsString);
        }

        [Test]
        public void Account_password_should_accept_the_new_password_as_valid()
        {
            _changePasswordScenario.Account.Password.AssertIsCorrectPassword(_changePasswordScenario.NewPasswordAsString);
        }
    }
}

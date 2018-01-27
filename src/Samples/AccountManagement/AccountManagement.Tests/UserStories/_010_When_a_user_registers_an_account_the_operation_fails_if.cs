﻿using System;
using AccountManagement.Scenarios;
using Composable.Messaging.Buses;
using Composable.System.Linq;
using Composable.Testing;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.UserStories
{
    public class _010_When_a_user_registers_an_account_the_operation_fails_if : UserStoryTest
    {
        RegisterAccountScenario _registerAccountScenario;

        [SetUp] public void SetupWiringAndCreateRepositoryAndScope() { _registerAccountScenario = new RegisterAccountScenario(ClientEndpoint); }


        [Test, TestCaseSource(typeof(TestData.Password.Invalid), nameof(TestData.Password.Invalid.All))]
        public void Password_does_not_meet_policy(string newPassword) =>
            _registerAccountScenario.Mutate(@this => @this.Password = newPassword).Invoking(@this => @this.Execute()).ShouldThrow<Exception>();

        [Test] public void Email_is_null()
            => AssertThrows.Exception<CommandValidationFailureException>(() => _registerAccountScenario.Mutate(@this => @this.Email = null).Execute());

        [Test] public void Email_is_empty_string()
            => AssertThrows.Exception<CommandValidationFailureException>(() => _registerAccountScenario.Mutate(@this => @this.Email = "").Execute());

        [Test] public void AccountId_is_empty()
            => AssertThrows.Exception<CommandValidationFailureException>(() => _registerAccountScenario.Mutate(@this => @this.AccountId = Guid.Empty).Execute());
    }
}

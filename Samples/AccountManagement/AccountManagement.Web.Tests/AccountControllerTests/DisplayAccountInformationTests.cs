﻿using System.Web.Mvc;
using AccountManagement.Domain;
using AccountManagement.TestHelpers.Scenarios;
using AccountManagement.Web.Views.Account;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace AccountManagement.Web.Tests.AccountControllerTests
{
    public class DisplayAccountInformationTests : AccountControllerTest
    {
        private Account _registeredAccount;

        [SetUp]
        public void RegisterAccount()
        {
            _registeredAccount = new RegisterAccountScenario(Container).Execute();
            AuthenticationContext.AccountId = _registeredAccount.Id;
        }

        [Test]
        public void CanDisplayView()
        {
            InvokeControllerAndGetViewModel();
        }

        [Test]
        public void EmailIsSameAsForTheAccount()
        {
            Controller.ControllerContext = new ControllerContext();
            InvokeControllerAndGetViewModel()
                .Email
                .Should().Be(_registeredAccount.Email.ToString());
        }

        private DisplayAccountDetailsViewModel InvokeControllerAndGetViewModel()
        {
            return (DisplayAccountDetailsViewModel)Controller.DisplayAccountDetails().Model;
        }
    }
}
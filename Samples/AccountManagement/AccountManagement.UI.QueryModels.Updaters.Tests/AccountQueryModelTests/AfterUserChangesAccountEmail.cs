﻿using AccountManagement.Domain.Shared;
using Castle.MicroKernel.Lifestyle;
using Composable.CQRS.Windsor;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.UI.QueryModels.DocumentDB.Updaters.Tests.AccountQueryModelTests
{
    [TestFixture]
    public class AfterUserChangesAccountEmail : RegistersAccountDuringSetupAccountQueryModelTestBase
    {
        private readonly Email _newEmail = Email.Parse("valid.email@domain.com");

        [SetUp]
        public void RegisterAccountAndChangeEmail()
        {
            Container.ExecuteUnitOfWork(() => RegisteredAccount.ChangeEmail(_newEmail));
        }

        [Test]
        public void EmailIsTheOneFromTheEvent()
        {
            using(Container.BeginScope())
            {
                GetQueryModel().Email.Should().Be(_newEmail);
            }
        }
    }
}

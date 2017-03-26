﻿using AccountManagement.UI.QueryModels.Services;
using Composable.DependencyInjection;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.UI.QueryModels.Tests.FetchingAccountByEmailTests
{
    [TestFixture]
    public class AfterAccountIsRegistered : RegistersAccountDuringSetupAccountQueryModelTestBase
    {
        [Test]
        public void YouCanGetTheAccountViaTheEmail()
        {
            AccountQueryModel account;
            Container.Resolve<IAccountManagementQueryModelsReader>()
                .TryGetAccountByEmail(RegisteredAccount.Email, out account)
                .Should().Be(true);

            account.Should().NotBe(null);
            account.Email.Should().Be(RegisteredAccount.Email);
            account.Id.Should().Be(RegisteredAccount.Id);
        }
    }
}

﻿using AccountManagement.Domain;
using AccountManagement.TestHelpers.Scenarios;
using NUnit.Framework;

namespace AccountManagement.UI.QueryModels.Tests.FetchingAccountByEmailTests
{
    public class RegistersAccountDuringSetupAccountQueryModelTestBase : QueryModelsTestsBase
    {
        protected Account RegisteredAccount;
        RegisterAccountScenario RegisterAccountScenario;

        [SetUp]
        public void RegisterAccount()
        {
            RegisterAccountScenario = new RegisterAccountScenario(Container);
            RegisteredAccount = RegisterAccountScenario.Execute();
        }
    }
}

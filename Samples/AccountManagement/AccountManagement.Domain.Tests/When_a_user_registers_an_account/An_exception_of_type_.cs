﻿using System;
using AccountManagement.Domain.Services;
using AccountManagement.TestHelpers.Scenarios;
using FluentAssertions;
using NUnit.Framework;

namespace AccountManagement.Domain.Tests.When_a_user_registers_an_account
{
    [TestFixture]
    public class An_exception_of_type_ : DomainTestBase
    {
        IDuplicateAccountChecker _duplicateAccountChecker;
        RegisterAccountScenario _registerAccountScenario;
        IAccountRepository _repository;

        [SetUp]
        public void SetupWiringAndCreateRepositoryAndScope()
        {
            _repository = Container.Resolve<IAccountRepository>();
            _duplicateAccountChecker = Container.Resolve<IDuplicateAccountChecker>();
            _registerAccountScenario = new RegisterAccountScenario(Container);
        }

        [Test]
        public void DuplicateAccountException_is_thrown_if_email_is_already_registered()
        {
            _registerAccountScenario.Execute();
            Assert.Throws<DuplicateAccountException>(() => _registerAccountScenario.Execute());
        }

        [Test]
        public void ObjectIsNullContractViolationException_is_thrown_if_Password_is_null()
        {
            _registerAccountScenario.Password = null;
            this.Invoking(_ =>_registerAccountScenario.Execute())
                .ShouldThrow<Exception>();
        }

        [Test]
        public void ObjectIsNullContractViolationException_is_thrown_if_Email_is_null()
        {
            _registerAccountScenario.Email = null;
            this.Invoking(_ => _registerAccountScenario.Execute())
                .ShouldThrow<Exception>(); ;
        }

        [Test]
        public void ObjectIsDefaultContractViolationException_is_thrown_if_AccountId_is_empty()
        {
            _registerAccountScenario.AccountId = Guid.Empty;
            this.Invoking(_ => _registerAccountScenario.Execute())
                .ShouldThrow<Exception>(); ;
        }

        [Test]
        public void ObjectIsDefaultContractViolationException_is_thrown_if_repository_is_null()
        {
            this.Invoking(_ => Account.Register(_registerAccountScenario.Email, _registerAccountScenario.Password, Guid.Empty, _repository, _duplicateAccountChecker))
                .ShouldThrow<Exception>(); ;
        }
    }
}

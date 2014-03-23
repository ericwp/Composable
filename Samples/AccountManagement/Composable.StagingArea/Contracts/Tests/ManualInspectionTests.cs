﻿using FluentAssertions;
using NUnit.Framework;

namespace Composable.Contracts.Tests
{
    [TestFixture]
    public class ManualInspectionTests
    {
        [Test]
        public void ThrownContractExceptionIfTestDoesNotPass()
        {
            var nameArgument = "bad";
            Assert.Throws<ContractException>(() => Contract.Optimized.Argument(nameArgument, "nameargument").Inspect(value => value != nameArgument));

            Assert.Throws<ContractException>(() => Contract.Arguments(() => nameArgument).Inspect(value => value != nameArgument));
        }

        [Test]
        public void ThrowsExceptionMatchingParameterNameIfTestDoesNotPass()
        {
            var nameargument = "bad";
            Assert.Throws<ContractException>(() => Contract.Optimized.Argument(nameargument, "nameargument").Inspect(value => value != nameargument))
                .Message.Should().Contain("nameargument");

            Assert.Throws<ContractException>(() => Contract.Arguments(() => nameargument).Inspect(value => value != nameargument))
                .Message.Should().Contain("nameargument");
        }
    }
}

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
            Assert.Throws<ContractException>(() => Contract.Optimized.Argument("bad", "nameargument").Inspect(value => value != "bad"));
        }

        [Test]
        public void ThrowsExceptionMatchingParameterNameIfTestDoesNotPass()
        {
            Assert.Throws<ContractException>(() => Contract.Optimized.Argument("bad", "nameargument").Inspect(value => value != "bad"))
                .Message.Should().Contain("nameargument");
        }
    }
}

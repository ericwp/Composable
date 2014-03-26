﻿using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Contracts.Tests
{
    [TestFixture]
    public class StringNotNullOrEmptyTests
    {
        [Test]
        public void NotEmptyThrowsStringIsEmptyArgumentExceptionForEmptyString()
        {
            string emptyString = "";
            Assert.Throws<StringIsEmptyContractViolationException>(() => Contract.Optimized.Arguments(emptyString).NotNullOrEmpty());

            Assert.Throws<StringIsEmptyContractViolationException>(() => Contract.Arguments(() => emptyString).NotNullOrEmpty());
        }

        [Test]
        public void UsesArgumentNameForExceptionMessage()
        {
            string emptyString = "";
            Assert.Throws<StringIsEmptyContractViolationException>(() => Contract.Optimized.Argument(emptyString, "emptyString").NotNullOrEmpty())
                .Message.Should().Contain("emptyString");

            Assert.Throws<StringIsEmptyContractViolationException>(() => Contract.Arguments(() => emptyString).NotNullOrEmpty())
                .Message.Should().Contain("emptyString");
        }

        [Test]
        public void ThrowsStringIsEmptyForEmptyStrings()
        {
            InspectionTestHelper.BatchTestInspection<StringIsEmptyContractViolationException, string>(
                inspected => inspected.NotNullOrEmpty(),
                badValues: new List<string> {"", ""},
                goodValues: new List<string> {"a", "aa", "aaa"});
        }

        [Test]
        public void ThrowsObjectIsNullForNullStrings()
        {
            InspectionTestHelper.BatchTestInspection<ObjectIsNullContractViolationException, string>(
                inspected => inspected.NotNullOrEmpty(),
                badValues: new List<string> {null, null},
                goodValues: new List<string> {"a", "aa", "aaa"});
        }
    }
}

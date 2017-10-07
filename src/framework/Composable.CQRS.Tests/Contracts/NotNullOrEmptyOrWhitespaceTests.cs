﻿using System;
using System.Collections.Generic;
using Composable.Contracts;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.Contracts
{
    [TestFixture]
    public class NotNullOrEmptyOrWhitespaceTests
    {
        [Test]
        public void ThrowsArgumentNullForNullArguments()
        {
            String aNullString = null;
            // ReSharper disable ExpressionIsAlwaysNull
            Assert.Throws<ObjectIsNullContractViolationException>(() => OldContract.Argument(() => aNullString).NotNullEmptyOrWhiteSpace());
            // ReSharper restore ExpressionIsAlwaysNull
        }

        [Test]
        public void ThrowsStringIsEmptyArgumentExceptionForEmptyStrings()
        {
            Assert.Throws<StringIsEmptyContractViolationException>(() => OldContract.Argument(() => string.Empty).NotNullEmptyOrWhiteSpace());
        }

        [Test]
        public void ThrowsStringIsWhiteSpaceExceptionForStringConsistingOfTabsSpacesOrLineBreaks()
        {
            var space = " ";
            var tab = "\t";
            var lineBreak = "\n";
            var newLine = "\r\n";
            var environmentNewLine = Environment.NewLine;
            Assert.Throws<StringIsWhitespaceContractViolationException>(() => OldContract.Argument(() => space).NotNullEmptyOrWhiteSpace());
            Assert.Throws<StringIsWhitespaceContractViolationException>(() => OldContract.Argument(() => tab).NotNullEmptyOrWhiteSpace());
            Assert.Throws<StringIsWhitespaceContractViolationException>(() => OldContract.Argument(() => lineBreak).NotNullEmptyOrWhiteSpace());
            Assert.Throws<StringIsWhitespaceContractViolationException>(() => OldContract.Argument(() => newLine).NotNullEmptyOrWhiteSpace());
            Assert.Throws<StringIsWhitespaceContractViolationException>(() => OldContract.Argument(() => environmentNewLine).NotNullEmptyOrWhiteSpace());

            Assert.Throws<StringIsWhitespaceContractViolationException>(() => OldContract.Argument(() => environmentNewLine, () => space).NotNullEmptyOrWhiteSpace());


            var badValues = new List<string> {space, tab, lineBreak, newLine, environmentNewLine};
            var goodValues = new List<string> {"aoeu", "lorem"};

            InspectionTestHelper.BatchTestInspection<StringIsWhitespaceContractViolationException, string>(
                assert: inspected => inspected.NotNullEmptyOrWhiteSpace(),
                badValues: badValues,
                goodValues: goodValues);
        }

        [Test]
        public void ShouldUseArgumentNameForException()
        {
            var newLine = Environment.NewLine;
            Assert.Throws<StringIsWhitespaceContractViolationException>(() => OldContract.Argument(()  => newLine).NotNullEmptyOrWhiteSpace())
                .Message.Should().Contain("newLine");
        }
    }
}

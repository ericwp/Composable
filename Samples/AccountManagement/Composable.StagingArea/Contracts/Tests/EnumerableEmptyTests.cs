﻿using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Contracts.Tests
{
    [TestFixture]
    public class EnumerableEmptyTests
    {
        [Test]
        public void ThrowsEnumerableIsEmptyException()
        {
            var list = new List<string>();
            Assert.Throws<EnumerableIsEmptyException>(() => Contract.Optimized.Argument(list).NotNullOrEmpty());

            var exception = Assert.Throws<EnumerableIsEmptyException>(() => Contract.Arguments(() => list).NotNullOrEmpty());
            exception.BadValue.Type.Should().Be(InspectionType.Argument);

            exception = Assert.Throws<EnumerableIsEmptyException>(() => Contract.Invariant(() => list).NotNullOrEmpty());
            exception.BadValue.Type.Should().Be(InspectionType.Invariant);

            exception = Assert.Throws<EnumerableIsEmptyException>(() =>  ReturnValueContractHelper.Return(list, inspected => inspected.NotNullOrEmpty()));
            exception.BadValue.Type.Should().Be(InspectionType.ReturnValue);


            InspectionTestHelper.BatchTestInspection<EnumerableIsEmptyException, IEnumerable<string>>(
                assert: inspected => inspected.NotNullOrEmpty(),
                badValues: new List<IEnumerable<string>>() {new List<string>(), new List<string>()},
                goodValues: new List<IEnumerable<string>>() {new List<string>() {""}, new List<string>() {""} });
        }
    }
}

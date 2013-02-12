﻿using Composable.KeyValueStorage;
using NUnit.Framework;
using FluentAssertions;

namespace CQRS.Tests.KeyValueStorage
{
    [TestFixture]
    public class DocumentDBSession_DocumentKeyTests
    {
        private class Base
        {}

        private class Inheritor : Base
        {}

        private class Unrelated{}


        [Test]
        public void TwoInstancesOfTheSameTypeWithTheSameIdAreEqual()
        {
            var lhs = new DocumentDbSession.DocumentKey<Base>("theId");
            var rhs = new DocumentDbSession.DocumentKey<Base>("theId");

            lhs.Should().Be(rhs);
            rhs.Should().Be(lhs);
        }

        [Test]
        public void TwoInstancesOfTheSameTypeWithDifferentIdsAreNotEqual()
        {
            var lhs = new DocumentDbSession.DocumentKey<Base>("theFirstId");
            var rhs = new DocumentDbSession.DocumentKey<Base>("theSecondId");

            lhs.Should().NotBe(rhs);
            rhs.Should().NotBe(lhs);
        }

        [Test]
        public void TwoInstancesWithInheritingTypesAndTheSameIdAreEqual()
        {
            var lhs = new DocumentDbSession.DocumentKey<Base>("theId");
            var rhs = new DocumentDbSession.DocumentKey<Inheritor>("theId");

            lhs.Should().Be(rhs);
            rhs.Should().Be(lhs);
        }

        [Test]
        public void TwoInstancesWithInheritingTypesAndDifferingIdsAreNotEqual()
        {
            var lhs = new DocumentDbSession.DocumentKey<Base>("theFirstId");
            var rhs = new DocumentDbSession.DocumentKey<Inheritor>("theSecondId");

            lhs.Should().NotBe(rhs);
            rhs.Should().NotBe(lhs);
        }

        [Test]
        public void TwoInstancesOfUnrelatedTypesAndSameIdAreNotEqual()
        {
            var lhs = new DocumentDbSession.DocumentKey<Base>("theId");
            var rhs = new DocumentDbSession.DocumentKey<Unrelated>("theId");

            lhs.Should().NotBe(rhs);
            rhs.Should().NotBe(lhs);
        }
    }
}
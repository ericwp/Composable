﻿using System.Diagnostics;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Contracts.Tests
{
    [TestFixture]
    public class ObjectNotNullOrDefaultTests
    {
        [Test]
        public void ThrowsArgumentNullExceptionIfAnyValueIsNull()
        {
            var anObject = new object();
            object nullObject = null;
            string emptyString = "";

            Assert.Throws<ObjectIsNullException>(() => Contract.Optimized.Argument(nullObject).NotNullOrDefault());            
            Assert.Throws<ObjectIsNullException>(() => Contract.Optimized.Arguments(anObject, nullObject).NotNullOrDefault());            
            Assert.Throws<ObjectIsNullException>(() => Contract.Optimized.Arguments(emptyString, nullObject, anObject).NotNullOrDefault());

            Assert.Throws<ObjectIsNullException>(() => Contract.Argument(() => nullObject).NotNullOrDefault());
            Assert.Throws<ObjectIsNullException>(() => Contract.Arguments(() => anObject, () => nullObject).NotNullOrDefault());
            Assert.Throws<ObjectIsNullException>(() => Contract.Arguments(() => emptyString, () => nullObject, () => anObject).NotNullOrDefault());
        }

        [Test]
        public void ThrowsObjectIsDefaultExceptionIfAnyValueIsDefault()
        {
            var anObject = new object();
            string emptyString = "";
            var zero = 0;
            var defaultMyStructure = new MyStructure();
            var aMyStructure = new MyStructure(1);

            Assert.Throws<ObjectIsDefaultException>(() => Contract.Optimized.Argument(zero).NotNullOrDefault());
            Assert.Throws<ObjectIsDefaultException>(() => Contract.Optimized.Arguments(anObject, zero).NotNullOrDefault());
            Assert.Throws<ObjectIsDefaultException>(() => Contract.Optimized.Arguments(emptyString, anObject, defaultMyStructure).NotNullOrDefault());
            Contract.Optimized.Arguments(emptyString, anObject, aMyStructure).NotNullOrDefault();

            Assert.Throws<ObjectIsDefaultException>(() => Contract.Argument(() => zero).NotNullOrDefault());
            Assert.Throws<ObjectIsDefaultException>(() => Contract.Arguments(() => anObject, () => zero).NotNullOrDefault());
            Assert.Throws<ObjectIsDefaultException>(() => Contract.Arguments(() => emptyString, () => anObject, () => defaultMyStructure).NotNullOrDefault());
            Contract.Arguments(() => emptyString, () => anObject, () => aMyStructure).NotNullOrDefault();
        }

        [Test]
        public void ShouldRun10TestsInOneMillisecond() //The Activator.CreateInstance stuff in the default check had me a bit worried. Seems I had no reason to be.
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            for(int i = 0; i < 100; i++)
            {
                Contract.Optimized.Argument(1).NotNullOrDefault();
            }
            stopWatch.Elapsed.Should().BeLessOrEqualTo(10.Milliseconds());
        }

        private struct MyStructure
        {
            public int Value;

            public MyStructure(int value)
            {
                Value = value;
            }
        }
    }
}

using System;
using Composable.CQRS.EventSourcing;
using NUnit.Framework;

namespace Composable.CQRS.Tests.CQRS.EventSourcing
{
    [TestFixture]
    public class AggregateRootTests
    {
        [Test]
        public void VersionIncreasesWithEachAppliedEvent()
        {
            var user = new User();
            Assert.That(user.Version, Is.EqualTo(0));

            user.Register("email", "password", Guid.NewGuid());
            Assert.That(user.Version, Is.EqualTo(1));

            user.ChangeEmail("NewEmail");
            Assert.That(user.Version, Is.EqualTo(2));

            user.ChangePassword("NewPassword");
            Assert.That(user.Version, Is.EqualTo(3));

        }

        [Test]
        public void GetChangesReturnsEmptyListAfterAcceptChangesCalled()
        {
            var user = new User();
            var userAseventStored = user as IEventStored;
            Assert.That(user.Version, Is.EqualTo(0));

            user.Register("email", "password", Guid.NewGuid());
            userAseventStored.AcceptChanges();
            Assert.That(userAseventStored.GetChanges(), Is.Empty);

            user.ChangeEmail("NewEmail");
            userAseventStored.AcceptChanges();
            Assert.That(userAseventStored.GetChanges(), Is.Empty);

            user.ChangePassword("NewPassword");
            userAseventStored.AcceptChanges();
            Assert.That(userAseventStored.GetChanges(), Is.Empty);
        }
    }
}
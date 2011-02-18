#region usings

using System.Linq;
using NUnit.Framework;

#endregion

namespace Composable.Data.ORM.Repositories.Tests.Linq
{
    [TestFixture]
    public class SimpleQueries : RepositoryTest
    {
        [Test]
        public void ShouldHandleQueryById()
        {
            var instance = GetInstance();
            Repository.SaveOrUpdate(instance);

            var loaded = Repository.Where(me => me.Id == instance.Id).Single();
            Assert.That(instance, Is.EqualTo(loaded));
        }
    }
}
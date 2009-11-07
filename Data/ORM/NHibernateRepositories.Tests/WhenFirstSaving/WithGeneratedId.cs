using System;
using NHibernate.ByteCode.LinFu;
using NUnit.Framework;
using Void.Data.ORM.NHibernate;
using Void.Data.ORM.NHibernateRepositories.Tests.Domain;
using Void.Data.ORM.Repositories.Tests.Domain;

namespace Void.Data.ORM.NHibernateRepositories.Tests.WhenFirstSaving
{
    [TestFixture]
    public class WithGeneratedId : Repositories.Tests.WhenFirstSaving.WithGeneratedId
    {
        protected override IPersistanceSession GetPersistanceSession()
        {
            return NhibernateRepositoryTest.GetPersistanceSession();
        }

        protected override TypeWithGeneratedId GetInstance()
        {
            return new TypewithGeneratedId();
        }
    }
}
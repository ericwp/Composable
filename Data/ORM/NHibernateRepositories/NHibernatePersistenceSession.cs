#region usings

using System.Linq;
using NHibernate;
using NHibernate.Linq;

#endregion

namespace Composable.Data.ORM.NHibernate
{
    public class NHibernatePersistenceSession : IPersistenceSession
    {
        public NHibernatePersistenceSession(ISession session)
        {
            Session = session;
        }

        public ISession Session { get; private set; }

        public IQueryable<T> Query<T>()
        {
            return Session.Query<T>();
        }

        public T Get<T>(object id)
        {
            return Session.Load<T>(id);
        }

        public void Save(object instance)
        {
            Session.Save(instance);
        }

        public void Delete(object instance)
        {
            Session.Delete(instance);
        }

        public void Clear()
        {
            Session.Clear();
        }


        #region Implementation of IDisposable


        ~NHibernatePersistenceSession()
        {
            if(!_disposed)
            {
                //todo:Log.For(this).ErrorMessage("{0} helper instance was not disposed!");
            }   
        }

        private bool _disposed;

        public void Dispose()
        {
            _disposed = true;
            Session.Dispose();
        }

        #endregion
    }
}
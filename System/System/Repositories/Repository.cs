using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Composable.System.Linq;

namespace Composable.Data.ORM
{
    public class Repository<TInstance, TKey> : Repository<TInstance, TKey, IPersistenceSession>
    {
        public Repository(IPersistenceSession session) : base(session)
        {
        }
    }

    public class Repository<TInstance, TKey, TPersistenceSession> : IRepository<TInstance, TKey> where TPersistenceSession : IPersistenceSession
    {
        protected TPersistenceSession Session { get; private set; }

        public Repository(TPersistenceSession persistenceSession)
        {
            Session = persistenceSession;
        }

        public virtual TInstance Get(TKey id)
        {
            return Session.Get<TInstance>(id);
        }

        public IList<TInstance> GetAll(IEnumerable<TKey> ids)
        {
            return ids.Select(Get).ToList();
        }

        public virtual TInstance TryGet(TKey id)
        {
            return Session.TryGet<TInstance>(id);
        }

        public virtual bool TryGet(TKey id, out TInstance result)
        {
            result = Session.TryGet<TInstance>(id);
            return !ReferenceEquals(result, null);
        }

        public IList<TInstance> TryGetAll(IEnumerable<TKey> ids)
        {
            return ids.Select(TryGet).Where(instance => !ReferenceEquals(instance, null)).ToList();
        }

        public virtual void SaveOrUpdate(TInstance instance)
        {
            Session.SaveOrUpdate(instance);
        }

        public virtual void SaveOrUpdate(IEnumerable<TInstance> instances)
        {
            instances.ForEach(SaveOrUpdate);
        }

        public virtual void Delete(TInstance instance)
        {
            Session.Delete(instance);
        }


        public IQueryable<TInstance> Find(IFilter<TInstance> criteria)
        {
            return this.Where(criteria);
        }

        #region Implementation of IQueryable

        private IQueryable<TInstance> _query;
        private IQueryable<TInstance> Query
        {
            get { return _query ?? (_query = Session.Query<TInstance>()); }
        }

        public IEnumerator<TInstance> GetEnumerator()
        {
            return Query.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Expression Expression { get { return Query.Expression; } }
        public Type ElementType { get { return Query.ElementType; } }
        public IQueryProvider Provider { get { return Query.Provider; } }

        #endregion
    }
}
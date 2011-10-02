﻿#region usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using Composable.CQRS.EventSourcing.SQLServer;
using Composable.ServiceBus;
using Composable.StuffThatDoesNotBelongHere;
using Composable.System.Linq;
using Composable.System;
using Composable.UnitsOfWork;
using log4net;

#endregion

namespace Composable.CQRS.EventSourcing
{
    public interface IEventSomethingOrOther : IDisposable
    {
        IEnumerable<IAggregateRootEvent> GetHistoryUnSafe(Guid id);
        void SaveEvents(IEnumerable<IAggregateRootEvent> events);
        IEnumerable<IAggregateRootEvent> StreamEventsAfterEventWithId(Guid? startAfterEventId);
    }

    public class EventStoreSession : IEventStoreSession, IUnitOfWorkParticipant
    {
        private readonly IServiceBus _bus;
        private readonly IEventSomethingOrOther _storage;
        private static ILog Log = LogManager.GetLogger(typeof(EventStoreSession));
        protected readonly IDictionary<Guid, IEventStored> _idMap = new Dictionary<Guid, IEventStored>();

        public EventStoreSession(IServiceBus bus, IEventSomethingOrOther storage)
        {
            _bus = bus;
            _storage = storage;
        }

        private IEnumerable<IAggregateRootEvent> GetHistory(Guid aggregateId)
        {
            var history = _storage.GetHistoryUnSafe(aggregateId);

            int version = 1;
            foreach(var aggregateRootEvent in history)
            {
                if(aggregateRootEvent.AggregateRootVersion != version++)
                {
                    throw new InvalidHistoryException();
                }
            }

            return history;
        }

        private void ThrowAggregateMissingException(Guid aggregateId)
        {
            throw new Exception(string.Format("Aggregate root with Id: {0} not found", aggregateId));
        }

        private bool DoTryGet<TAggregate>(Guid aggregateId, out TAggregate aggregate) where TAggregate : IEventStored
        {
            IEventStored es;
            if (_idMap.TryGetValue(aggregateId, out es))
            {
                aggregate = (TAggregate)es;
                return true;
            }

            var history = GetHistory(aggregateId);
            if (history.Any())
            {
                aggregate = Activator.CreateInstance<TAggregate>();
                aggregate.LoadFromHistory(GetHistory(aggregateId));
                _idMap.Add(aggregateId, aggregate);
                return true;
            }
            else
            {
                aggregate = default(TAggregate);
                return false;
            }
        }

        public TAggregate Get<TAggregate>(Guid aggregateId) where TAggregate : IEventStored
        {
            TAggregate result;
            if (!DoTryGet(aggregateId, out result))
                ThrowAggregateMissingException(aggregateId);
            return result;
        }

        public bool TryGet<TAggregate>(Guid aggregateId, out TAggregate aggregate) where TAggregate : IEventStored
        {
            return DoTryGet(aggregateId, out aggregate);
        }

        public TAggregate LoadSpecificVersion<TAggregate>(Guid aggregateId, int version) where TAggregate : IEventStored
        {
            var aggregate = Activator.CreateInstance<TAggregate>();
            var history = GetHistory(aggregateId);
            if (history.None())
                ThrowAggregateMissingException(aggregateId);
            aggregate.LoadFromHistory(history.Where(e => e.AggregateRootVersion <= version));
            return aggregate;
        }

        public void Save<TAggregate>(TAggregate aggregate) where TAggregate : IEventStored
        {
            var changes = aggregate.GetChanges();
            if(aggregate.Version > 0 && changes.None() || changes.Any() && changes.Min(e => e.AggregateRootVersion) > 1)
            {
                throw new AttemptToSaveAlreadyPersistedAggregateException(aggregate);
            }
            if(aggregate.Version == 0 && changes.None())
            {
                throw new AttemptToSaveEmptyAggregate(aggregate);
            }
            _idMap.Add(aggregate.Id, aggregate);
        }

        public void SaveChanges()
        {
            if(_unitOfWork == null)
            {                
                InternalSaveChanges();
            }else
            {
                Log.DebugFormat("{0} ignored call to SaveChanges since participating in a unit of work", _id);
            }
        }

        private void InternalSaveChanges()
        {
            Log.DebugFormat("{0} saving changes with {1} changes from transaction within unit of work {2}", _id, _idMap.Count, _unitOfWork ?? (object)"null");
            var newEvents = _idMap.SelectMany(p => p.Value.GetChanges()).ToList();
            _storage.SaveEvents(newEvents);
            newEvents.ForEach(_bus.Publish);
            _idMap.Select(p => p.Value).ForEach(p => p.AcceptChanges());
        }

        public void Dispose()
        {
            _storage.Dispose();
        }

        private IUnitOfWork _unitOfWork;
        private readonly Guid _id = Guid.NewGuid();

        IUnitOfWork IUnitOfWorkParticipant.UnitOfWork { get { return _unitOfWork; } }
        Guid IUnitOfWorkParticipant.Id { get { return _id; }  }

        void IUnitOfWorkParticipant.Join(IUnitOfWork unit)
        {
            _unitOfWork = unit;
        }

        void IUnitOfWorkParticipant.Commit(IUnitOfWork unit)
        {
            InternalSaveChanges();
            _unitOfWork = null;
        }

        void IUnitOfWorkParticipant.Rollback(IUnitOfWork unit)
        {
            _unitOfWork = null;
        }
    }

    public class AttemptToSaveEmptyAggregate : Exception
    {
        public AttemptToSaveEmptyAggregate(object value):base("Attempting to save an: {0} that Version=0 and no history to persist.".FormatWith(value.GetType().FullName))
        {
        }
    }
}
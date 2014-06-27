﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Composable.DDD;
using Composable.NewtonSoft;
using Composable.System.Collections.Collections;
using Composable.System.Reactive;
using Newtonsoft.Json;

namespace Composable.KeyValueStorage
{
    public class InMemoryDocumentDb : InMemoryObjectStore, IDocumentDb
    {
        private readonly ThreadSafeObservable<IDocumentUpdated> _documentUpdated = new ThreadSafeObservable<IDocumentUpdated>(); 

        public InMemoryDocumentDb()
        {
        }

        public IObservable<IDocumentUpdated> DocumentUpdated { get { return _documentUpdated; }}

        private readonly Dictionary<Type, Dictionary<string, string>> _persistentValues = new Dictionary<Type, Dictionary<string, string>>();

        public bool TryGet<T>(object id, out T value, Dictionary<Type, Dictionary<string, string>> persistentValues)
        {
            return TryGet<T>(id, out value);
        }

        public void Add<T>(object id, T value, Dictionary<Type, Dictionary<string, string>> persistentValues)
        {
            lock(_lockObject)
            {
                var idString = GetIdString(id);
                var stringValue = JsonConvert.SerializeObject(value, JsonSettings.JsonSerializerSettings);
                SetPersistedValue(value, idString, stringValue);
                base.Add(id, value);
                _documentUpdated.OnNext(new DocumentUpdated(idString, value));
            }
        }

        public IEnumerable<T> GetAll<T>(IEnumerable<Guid> ids) where T : IHasPersistentIdentity<Guid>
        {
            return GetAll<T>().Where(document => ids.Contains(document.Id));
        }

        public IEnumerable<Guid> GetAllIds<T>() where T : IHasPersistentIdentity<Guid>
        {
            return GetAll<T>().Select(document => document.Id);
        }

        private void SetPersistedValue<T>(T value, string idString, string stringValue)
        {
            _persistentValues.GetOrAddDefault(value.GetType())[idString] = stringValue;
        }

        override public void Update(object key, object value)
        {
            lock(_lockObject)
            {
                string oldValue;
                string idString = GetIdString(key);
                var stringValue = JsonConvert.SerializeObject(value, JsonSettings.JsonSerializerSettings);
                var needsUpdate = !_persistentValues
                    .GetOrAdd(value.GetType(), () => new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase))
                    .TryGetValue(idString, out oldValue) || stringValue != oldValue;

                if(!needsUpdate)
                {
                    object existingValue;
                    base.TryGet(value.GetType(), key, out existingValue);
                    needsUpdate = !(ReferenceEquals(existingValue, value));
                }

                if(needsUpdate)
                {
                    base.Update(key, value);
                    SetPersistedValue(value, idString, stringValue);
                    _documentUpdated.OnNext(new DocumentUpdated(idString, value));
                }
            }
        }
    }
}

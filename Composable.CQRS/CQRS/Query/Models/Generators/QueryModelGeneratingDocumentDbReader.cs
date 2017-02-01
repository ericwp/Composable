﻿using System;
using System.Collections.Generic;
using System.Linq;
using Composable.DDD;
using Composable.KeyValueStorage;
using Composable.SystemExtensions.Threading;

namespace Composable.CQRS.Query.Models.Generators
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class QueryModelGeneratingDocumentDbReader : IVersioningDocumentDbReader
    {
        readonly ISingleContextUseGuard _usageGuard;
        readonly IDocumentDbSessionInterceptor _interceptor;
        readonly IEnumerable<IQueryModelGenerator> _documentGenerators;
        readonly InMemoryObjectStore _idMap = new InMemoryObjectStore();
        //Review:mlidbo: Always requiring an interceptor causes a lot of unneeded complexity for clients. Consider creating a virtual void OnFirstLoad(T document) method instead. This would allow for inheriting this class to create "interceptable" sessions. Alternatively maybe an observable/event could be used somehow.
        public QueryModelGeneratingDocumentDbReader(ISingleContextUseGuard usageGuard, IDocumentDbSessionInterceptor interceptor, IEnumerable<IQueryModelGenerator> documentGenerators )
        {
            _usageGuard = usageGuard;
            _interceptor = interceptor;
            _documentGenerators = documentGenerators;
        }

        public void Dispose()
        {
        }

        public virtual TValue Get<TValue>(object key)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            TValue value;
            if (TryGet(key, out value))
            {
                return value;
            }

            throw new NoSuchDocumentException(key, typeof(TValue));
        }

        public virtual TValue GetVersion<TValue>(object key, int version)
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            TValue value;
            if (TryGetVersion(key, out value, version))
            {
                return value;
            }

            throw new NoSuchDocumentException(key, typeof(TValue));
        }

        public virtual bool TryGet<TDocument>(object key, out TDocument document)
        {
            return TryGetVersion(key, out document);
        }

        public virtual bool TryGetVersion<TDocument>(object key, out TDocument document, int version = -1)
        {
            var requiresVersioning = version > 0;
            _usageGuard.AssertNoContextChangeOccurred(this);

            if (!HandlesDocumentType<TDocument>(requireVersioningSupport: requiresVersioning))
            {
                document = default(TDocument);
                return false;
            }

            var documentType = typeof(TDocument);

            if (documentType.IsInterface)
            {
                throw new ArgumentException("You cannot query by id for an interface type. There is no guarantee of uniqueness");
            }

            if (!requiresVersioning && _idMap.TryGet(key, out document) && documentType.IsInstanceOfType(document))
            {
                return true;
            }
            
            document = TryGenerateModel<TDocument>(key, version);
            if (!Equals(document, default(TDocument)))
            {
                _interceptor.AfterLoad(document);
                if(!requiresVersioning)
                {
                    _idMap.Add(key, document);
                }
                return true;
            }
            return false;
        }

        TDocument TryGenerateModel<TDocument>(object key, int version)
        {
            if(version < 0)
            {
                return GetGeneratorsForDocumentType<TDocument>()
                    .Select(generator => generator.TryGenerate((Guid)key))
                    .Where(foundDocument => !Equals(foundDocument, default(TDocument)))
                    .SingleOrDefault();
            }

            return VersionedGeneratorsForDocumentType<TDocument>()
                    .Select(generator => generator.TryGenerate((Guid)key, version))
                    .Where(foundDocument => !Equals(foundDocument, default(TDocument)))
                    .SingleOrDefault();
        }

        bool HandlesDocumentType<TDocument>(bool requireVersioningSupport)
        {
            return requireVersioningSupport
                ? VersionedGeneratorsForDocumentType<TDocument>().Any()
                : GetGeneratorsForDocumentType<TDocument>().Any();
        }

        public virtual IEnumerable<TValue> Get<TValue>(IEnumerable<Guid> ids) where TValue : IHasPersistentIdentity<Guid>
        {
            _usageGuard.AssertNoContextChangeOccurred(this);
            return ids.Select(id => Get<TValue>(id)).ToList();
        }

        IEnumerable<IVersioningQueryModelGenerator<TDocument>> VersionedGeneratorsForDocumentType<TDocument>()
        {
            return _documentGenerators.OfType<IVersioningQueryModelGenerator<TDocument>>().ToList();
        }

        IEnumerable<IQueryModelGenerator<TDocument>> GetGeneratorsForDocumentType<TDocument>()
        {
            return _documentGenerators.OfType<IQueryModelGenerator<TDocument>>().ToList();
        }
    }
}
#region usings

using System;
using Composable.DDD;
using Composable.StuffThatDoesNotBelongHere;
using Composable.StuffThatDoesNotBelongHere.Translation;
using Composable.System;

#endregion

namespace Composable.CQRS
{
    public class UnNamedEntityReference<TReferencedType, TKey> :
        IdEqualityObject<UnNamedEntityReference<TReferencedType, TKey>, TKey>,
        IUnNamedEntityReference<TReferencedType, TKey>
        where TReferencedType : IHasPersistentIdentity<TKey>
    {
        protected UnNamedEntityReference() : base(default(TKey)) {}

        public UnNamedEntityReference(TKey id) : base(id){}
        public UnNamedEntityReference(TReferencedType referenced) : base(referenced.Id){}

        public override string ToString()
        {
            return "RefTo:{0}, Id:{1}".FormatWith(typeof (TReferencedType).Name, Id);
        }
    }

    public class UnNamedEntityReference<TReferencedType> :
        UnNamedEntityReference<TReferencedType, Guid>
        where TReferencedType : IHasPersistentIdentity<Guid>
    {
        protected UnNamedEntityReference() {}
        public UnNamedEntityReference(Guid id) : base(id) {}
        public UnNamedEntityReference(TReferencedType referenced) : base(referenced) {}
    }

    public class EntityReference<TReferencedType, TKey> :
        UnNamedEntityReference<TReferencedType, TKey>,
        IEntityReference<TReferencedType, TKey>
        where TReferencedType : IHasPersistentIdentity<TKey>, INamed
    {
        protected EntityReference() {}
        public EntityReference(TKey id, string name) : base(id)
        {
            Name = name;
        }

        public EntityReference(TKey id) : base(id) {}
        public EntityReference(TReferencedType referenced) : base(referenced) {}
        public EntityReference(TReferencedType referenced, string name) : this(referenced.Id, name) {}

        [Translate]
        public virtual string Name { get; protected set; }

        public override string ToString()
        {
            return "{0}, Name:{1}".FormatWith(base.ToString(), Name);
        }
    }

    public class EntityReference<TReferencedType> :        
        EntityReference<TReferencedType, Guid>,
        IEntityReference<TReferencedType>,
        IComparable<EntityReference<TReferencedType>> where TReferencedType : IHasPersistentIdentity<Guid>, INamed
    {
        protected EntityReference() { }
        public EntityReference(Guid id) : base(id) { }
        public EntityReference(Guid id, string name) : base(id, name) {}
        public EntityReference(TReferencedType referenced) : base(referenced) {}
        public EntityReference(TReferencedType referenced, string name) : this(referenced.Id, name) {}
        public int CompareTo(EntityReference<TReferencedType> other)
        {
            return String.Compare(Name, other.Name);
        }
    }

    public class MaterializableUnNamedEntityReference<TReferencedType, TKeyType> :
        UnNamedEntityReference<TReferencedType, TKeyType>,
        IMaterializableUnNamedEntityReference<TReferencedType, TKeyType>
        where
            TReferencedType : IHasPersistentIdentity<TKeyType>
    {
        protected MaterializableUnNamedEntityReference() { }
        public TReferencedType Referenced { get; private set; }

        public MaterializableUnNamedEntityReference(TKeyType id) : base(id) {}
        public MaterializableUnNamedEntityReference(TReferencedType referenced) : base(referenced) {}
    }

    public class MaterializableEntityReference<TReferencedType, TKeyType> :
        EntityReference<TReferencedType, TKeyType>,
        IMaterializableEntityReference<TReferencedType, TKeyType>
        where TReferencedType : IHasPersistentIdentity<TKeyType>, INamed
    {
        public TReferencedType Referenced { get; private set; }

        protected MaterializableEntityReference() {}
        public MaterializableEntityReference(TKeyType id) : base(id) {}
        public MaterializableEntityReference(TKeyType id, string name) : base(id, name) { }
        public MaterializableEntityReference(TReferencedType referenced) : base(referenced) {}        
        public MaterializableEntityReference(TReferencedType referenced, string name) : base(referenced, name) {}
    }
}
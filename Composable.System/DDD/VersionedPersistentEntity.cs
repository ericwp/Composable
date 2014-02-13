#region usings

using System;
using System.Reflection;

#endregion

namespace Composable.DDD
{
    ///<summary>Base class for persistent entities with versioning information</summary>
    [Serializable]
    public class VersionedPersistentEntity<T> : PersistentEntity<T> where T : VersionedPersistentEntity<T>
    {
        /// <summary>Creates an instance using the supplied <paramref name="id"/> as the Id.</summary>
        protected VersionedPersistentEntity(Guid id) : base(id)
        {
        }

        /// <summary> Creates an instance using a newly generated Id</summary>
        protected VersionedPersistentEntity()
        {
        }

        //This is an ugly hack to keep nhibernate from choking when adding instance without going through an nhibernate session...
        public static T FakePersistentInstance(Guid id)
        {            
            var result = (T)Activator.CreateInstance(typeof(T));
            result.Version = 1;
            result.SetIdBeVerySureYouKnowWhatYouAreDoing(id);
            return result;
        }

        ///<summary>Contains the current version of the entity</summary>
        public virtual int Version { get; protected set; }
    }
}
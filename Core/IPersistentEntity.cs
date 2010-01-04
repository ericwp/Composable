using System;
using System.Diagnostics.Contracts;

namespace Void
{
/// <summary>
/// Should be implemented by persistent* classes the represents entities in the Domain Driven Design sense of the word.
/// 
/// The vital distinction about Persistent Entities is that equality is defined by Identity, 
/// and as such they must guarantee that they have a non-default identity at all times.  
/// 
/// * Classes that have a lifecycle longer than an application run. Often persisted in databases.
/// </summary>
/// <typeparam name="TKeyType"></typeparam>
    [ContractClass(typeof(PersistentEntityContract<>))]
    public interface IPersistentEntity<TKeyType>
    {
        /// <summary>The unique identifier for this instance.</summary>
        TKeyType Id { get;}
    }

    [ContractClassFor(typeof(IPersistentEntity<>))]
    internal class PersistentEntityContract<T> : IPersistentEntity<T>
    {
        T IPersistentEntity<T>.Id { get
        {
            Contract.Ensures(!Equals(Contract.Result<T>(), default(T)));
            throw new NotImplementedException();
        } }
    }
}
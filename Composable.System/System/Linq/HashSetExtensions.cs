#region usings

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Composable.Contracts;

#endregion

namespace Composable.System.Linq
{
    /// <summary>A collection of extensions to work with <see cref="HashSet{T}"/></summary>
    public static class HashSetExtensions
    {
        /// <returns>A set containing all the items in <paramref name="me"/></returns>
        public static HashSet<T> ToSet<T>(this IEnumerable<T> me)
        {
            ContractTemp.Argument(() => me).NotNull();
            return new HashSet<T>(me);
        }

        ///<summary>
        /// Removes all of the items in the supplied enumerable from the set.
        /// Simply forwards to ExceptWith but providing a name that is not utterly unreadable </summary>
        public static void RemoveRange<T>(this ISet<T> me, IEnumerable<T> toRemove)
        {
            ContractTemp.Argument(() => me, () => toRemove).NotNull();
            me.ExceptWith(toRemove);
        }

        ///<summary>Adds all the supplied <paramref name="toAdd"/> instances to the set.</summary>
        public static void AddRange<T>(this ISet<T> me, IEnumerable<T> toAdd)
        {
            ContractTemp.Argument(() => me, () => toAdd).NotNull();
            toAdd.ForEach(addMe => me.Add(addMe));
        }
    }
}
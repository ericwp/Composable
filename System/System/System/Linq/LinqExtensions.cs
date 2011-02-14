using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Composable.System.Linq
{
    /// <summary/>
    [Pure]
    public static class LinqExtensions
    {
        public static T SingleNotNull<T>(this IEnumerable<T> me)
        {
            Contract.Requires(me!= null);
            Contract.Ensures(Contract.Result<T>() != null);
            var result = me.Single();
            if(result == null)
            {
                throw new Exception("Return value null");
            }
            return result;
        }

        /// <summary>
        /// Binds <paramref name="me"/> to the parameter in <paramref name="selector"/>.
        /// Enables you to perform operations that require you to alias the sequence being 
        /// manipulated without having to create temporary variables.
        /// </summary>
        public static IEnumerable<TResult> Let<TSource, TResult>(this IEnumerable<TSource> me, Func<IEnumerable<TSource>, IEnumerable<TResult>> selector)
        {
            Contract.Requires(selector != null);
            return selector(me);
        }

        /// <summary>
        /// Adds <paramref name="instances"/> to the end of <paramref name="source"/>
        /// </summary>
        public static IEnumerable<T> Append<T>(this IEnumerable<T> source, params T[] instances)
        {
            Contract.Requires(source != null && instances != null);
            return source.Concat(instances);
        }

        /// <summary>
        /// <para>The inversion of Enumerable.Any(Func&lt;T, bool&gt; predicate) </para>
        /// <para>Returns true if <paramref name="me"/> contains no elements matching <paramref name="predicate"/></para>
        /// </summary>
        /// <returns>true if <paramref name="me"/> contains no objects matching <paramref name="predicate"/>. Otherwise false.</returns>
        public static bool None<T>(this IEnumerable<T> me, Func<T, bool> predicate)
        {
            Contract.Requires(me != null && predicate != null);
            return !me.Any(predicate);
        }

        /// <summary>
        /// <para>The inversion of Enumerable.Any() .</para>
        /// <para>Returns true if <paramref name="me"/> contains no elements.</para>
        /// </summary>
        /// <returns>true if <paramref name="me"/> contains no objects. Otherwise false.</returns>
        public static bool None<T>(this IEnumerable<T> me)
        {
            Contract.Requires(me != null);
            return !me.Any();
        }

        /// <summary>
        /// Chops an IEnumerable up into <paramref name="size"/> sized chunks.
        /// </summary>
        public static IEnumerable<IEnumerable<T>> ChopIntoSizesOf<T>(this IEnumerable<T> me, int size)
        {
            using (var enumerator = me.GetEnumerator())
            {
                int yielded = size;
                while (yielded == size)
                {
                    yielded = 0;
                    T[] next = new T[size];
                    while (yielded < size && enumerator.MoveNext())
                    {
                        next[yielded++] = enumerator.Current;
                    }
                    if (yielded == 0)
                    {
                        yield break;
                    }
                    yield return yielded == size ? next : next.Take(yielded);
                }
            }

            //todo: figure out why simple implementation based on Skip and Take had horrible performance
        }


        //todo: Figure out why this method does not resolve as an extension method.
        //is the type inference in C# to weak?
        /// <summary>
        /// Acting on an <see cref="IEnumerable{T}"/> <paramref name="me"/> where T is an <see cref="IEnumerable{TChild}"/>
        /// returns an <see cref="IEnumerable{TChild}"/> aggregating all the TChild instances
        /// 
        /// Using SelectMany(x=>x) is ugly and unintuitive.
        /// This method provides an intuitively named alternative.
        /// </summary>
        /// <typeparam name="T">A type implementing <see cref="IEnumerable{TChild}"/></typeparam>
        /// <typeparam name="TChild">The type contained in the nested enumerables.</typeparam>
        /// <param name="me">the collection to act upon</param>
        /// <returns>All the objects in all the nested collections </returns>
        public static IEnumerable<TChild> Flatten<T, TChild>(IEnumerable<T> me) where T : IEnumerable<TChild>
        {
            Contract.Requires(me != null);
            return me.SelectMany(obj => obj);
        }
    }
}
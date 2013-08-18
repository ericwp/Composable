﻿using System;
using System.Collections.Generic;
using System.Linq;
using Composable.System.Linq;

namespace Composable.System.Collections.Collections
{
    public static class CollectionExtensions
    {
        public static int RemoveWhere<T>(this ICollection<T> me, Func<T, bool> condition)
        {
            var toRemove = me.Where(condition).ToList();
            toRemove.ForEach(removeMe => me.Remove(removeMe));
            return toRemove.Count;
        }


        public static void RemoveRange<T>(this ICollection<T> me, IEnumerable<T> toRemove)
        {
            toRemove.ForEach(removeMe => me.Remove(removeMe));
        }

        public static void AddRange<T>(this ICollection<T> me, IEnumerable<T> toRemove)
        {
            toRemove.ForEach(me.Add);
        }
    }
}

﻿using System.Collections.Generic;

using System.Linq;
using Composable.Contracts;

namespace Composable.System.Collections.Collections
{
    ///<summary>Adds some convenience features to linked list</summary>
    public static class LinkedListExtensions
    {
        ///<summary>Enumerates this and all following nodes.</summary>
        public static IEnumerable<LinkedListNode<T>> NodesFrom<T>(this LinkedListNode<T> @this)
        {
            var node = @this;
            while(node != null)
            {
                yield return node;
                node = node.Next;
            }
        }

        ///<summary>Enumerates this and all following node values.</summary>
        public static IEnumerable<T> ValuesFrom<T>(this LinkedListNode<T> @this) { return @this.NodesFrom().Select(node => node.Value); }

        ///<summary>Inserts <paramref name="items"/> after the <paramref name="this"/>  node and returns the nodes that were inserted.</summary>
        public static IReadOnlyList<LinkedListNode<T>> AddAfter<T>(this LinkedListNode<T> @this, IEnumerable<T> items)
        {
            Contract.Argument(() => items, () => @this).NotNull();

            return items
                .Reverse()
                .Select(@event => @this.List.AddAfter(@this, @event))
                .Reverse()
                .ToList();
        }

        ///<summary>Inserts <paramref name="items"/> after the <paramref name="this"/>  node and returns the nodes that were inserted.</summary>
        public static IReadOnlyList<LinkedListNode<T>> AddBefore<T>(this LinkedListNode<T> @this, IEnumerable<T> items)
        {
            Contract.Argument(() => items, () => @this).NotNull();

            return items
                .Select(@event => @this.List.AddBefore(@this, @event))
                .ToList();
        }

        ///<summary>Replaces <paramref name="this"/> and returns the nodes that were inserted.</summary>
        public static IReadOnlyList<LinkedListNode<T>> Replace<T>(this LinkedListNode<T> @this, IEnumerable<T> items)
        {
            Contract.Argument(() => items, () => @this).NotNull();

            var newNodes = @this.AddAfter(items);
            @this.List.Remove(@this);
            return newNodes;
        }
    }
}

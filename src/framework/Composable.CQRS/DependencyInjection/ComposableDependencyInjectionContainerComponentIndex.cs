﻿using System;
using System.Collections.Generic;
using Composable.System.Collections.Collections;
using Composable.System.Threading.ResourceAccess;

namespace Composable.DependencyInjection
{
    partial class ComposableDependencyInjectionContainer
    {
        static class ComponentIndex
        {
            static int CurrentIndex;
            static readonly OptimizedThreadShared<Dictionary<Type, int>> Map = new OptimizedThreadShared<Dictionary<Type, int>>(new Dictionary<Type, int>());

            internal static int For(Type type) => Map.WithExclusiveAccess(map => map.GetOrAdd(type, () => CurrentIndex++));
            internal static int For<TType>() => CacheForType<TType>.Index;

            static class CacheForType<TType>
            {
                internal static readonly int Index = ComponentIndex.For(typeof(TType));
            }
        }
    }
}
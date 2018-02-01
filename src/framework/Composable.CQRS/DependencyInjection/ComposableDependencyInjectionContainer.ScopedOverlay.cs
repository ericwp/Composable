﻿
using System;
using System.Collections.Generic;

namespace Composable.DependencyInjection
{
    partial class ComposableDependencyInjectionContainer
    {
        class Scope
        {
            readonly List<IDisposable> _disposables = new List<IDisposable>();
            readonly Dictionary<Guid, object> _instantiatedComponents = new Dictionary<Guid, object>();
            internal ComponentCache _cache;
            internal bool IsDisposed { get; private set; }

            public Scope(ComposableDependencyInjectionContainer container) => _cache = container._singletonCache.Clone();

            public void Dispose()
            {
                if(!IsDisposed)
                {
                    IsDisposed = true;
                    foreach(var disposable in _disposables)
                    {
                        disposable.Dispose();
                    }
                }
            }

            public object ResolveInstance(ComponentRegistration registration, IServiceLocatorKernel parent)
            {
                if(_instantiatedComponents.TryGetValue(registration.Id, out var cachedInstance))
                {
                    return cachedInstance;
                } else
                {
                    cachedInstance = registration.CreateInstance(parent);
                    _cache.Set(cachedInstance, registration);
                    _instantiatedComponents.Add(registration.Id, cachedInstance);
                    if(cachedInstance is IDisposable disposable)
                    {
                        _disposables.Add(disposable);
                    }

                    return cachedInstance;
                }
            }
        }
    }
}
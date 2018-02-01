﻿using System;
using System.Collections.Generic;
using System.Linq;
using Composable.System.Linq;

namespace Composable.DependencyInjection
{
    partial class ComposableDependencyInjectionContainer
    {
        internal class ComponentCache : IDisposable
        {
            internal bool IsDisposed;
            readonly ComponentRegistration[][] _components;
            readonly int[] _typeIndexToComponentIndex;
            readonly object[] _instances;
            readonly LinkedList<IDisposable> _disposables = new LinkedList<IDisposable>();

            internal ComponentCache(IReadOnlyList<ComponentRegistration> registrations) : this(CreateComponentArray(registrations), CreateTypeToComponentIndex(registrations))
            {
            }

            internal ComponentCache Clone() => new ComponentCache(_components, _typeIndexToComponentIndex);

            public void Set(object instance, ComponentRegistration registration)
            {
                _instances[registration.ComponentIndex] = instance;
                if(instance is IDisposable disposable)
                {
                    _disposables.AddLast(disposable);
                }
            }

            internal TService TryGet<TService>() => (TService)_instances[_typeIndexToComponentIndex[ServiceTypeIndex.ForService<TService>.Index]];

            internal ComponentRegistration[] GetRegistration<TService>() => _components[ServiceTypeIndex.ForService<TService>.Index];

            ComponentCache(ComponentRegistration[][] components, int[] typeIndexToComponentIndex)
            {
                _components = components;
                _typeIndexToComponentIndex = typeIndexToComponentIndex;
                _instances = new object[_components.Length];
            }

            static ComponentRegistration[][] CreateComponentArray(IReadOnlyList<ComponentRegistration> registrations)
            {
               var componentArray = new ComponentRegistration[ServiceTypeIndex.ComponentCount][];

                registrations.SelectMany(registration => registration.ServiceTypeIndexes.Select(typeIndex => new {registration, typeIndex}))
                             .GroupBy(registrationPerTypeIndex => registrationPerTypeIndex.typeIndex)
                             .ForEach(registrationsOnTypeindex => componentArray[registrationsOnTypeindex.Key] = registrationsOnTypeindex.Select(regs => regs.registration).ToArray());

                return componentArray;
            }

            static int[] CreateTypeToComponentIndex(IReadOnlyList<ComponentRegistration> registrations)
            {
                var typeToComponentIndex = new int[ServiceTypeIndex.ComponentCount];
                foreach (var registration in registrations)
                {
                    foreach (var serviceTypeIndex in registration.ServiceTypeIndexes)
                    {
                        typeToComponentIndex[serviceTypeIndex] = registration.ComponentIndex;
                    }
                }

                return typeToComponentIndex;
            }

            public void Dispose()
            {
                if(!IsDisposed)
                {
                    IsDisposed = true;
                    foreach (var disposable in _disposables)
                    {
                        disposable.Dispose();
                    }
                }
            }
        }
    }
}
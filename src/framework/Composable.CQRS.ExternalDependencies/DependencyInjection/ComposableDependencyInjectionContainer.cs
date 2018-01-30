﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Composable.System;
using Composable.System.Collections.Collections;
using Composable.System.Reflection;
using Composable.System.Threading.ResourceAccess;

namespace Composable.DependencyInjection
{
    public class ComposableDependencyInjectionContainer : IDependencyInjectionContainer, IServiceLocator, IServiceLocatorKernel
    {
        internal ComposableDependencyInjectionContainer(IRunMode runMode)
        {
            RunMode = runMode;
            _state = new OptimizedThreadShared<NonThreadSafeImplementation>(new NonThreadSafeImplementation(this));
        }

        readonly IThreadShared<NonThreadSafeImplementation> _state;

        public IRunMode RunMode { get; }
        public void Register(params ComponentRegistration[] registrations) => _state.WithExclusiveAccess(state => state.Register(registrations));

        public IEnumerable<ComponentRegistration> RegisteredComponents() => _state.WithExclusiveAccess(state => state.RegisteredComponents);

        bool _verified;

        IServiceLocator IDependencyInjectionContainer.CreateServiceLocator() => _state.WithExclusiveAccess(state => state.CreateServiceLocator());

        IComponentLease<TComponent> IServiceLocator.Lease<TComponent>() => new ComponentLease<TComponent>(_state.WithExclusiveAccess(state => state.Resolve<TComponent>()));
        IMultiComponentLease<TComponent> IServiceLocator.LeaseAll<TComponent>() => new MultiComponentLease<TComponent>(_state.WithExclusiveAccess(state => state.ResolveAll<TComponent>()));
        IDisposable IServiceLocator.BeginScope() => _state.WithExclusiveAccess(state => state.BeginScope());


        void IDisposable.Dispose() {}

        sealed class ComponentLease<T> : IComponentLease<T>
        {
            readonly T _instance;

            internal ComponentLease(T component) => _instance = component;

            T IComponentLease<T>.Instance => _instance;
            void IDisposable.Dispose() {}
        }

        sealed class MultiComponentLease<T> : IMultiComponentLease<T>
        {
            readonly T[] _instances;

            internal MultiComponentLease(T[] components) => _instances = components;

            T[] IMultiComponentLease<T>.Instances => _instances;
            void IDisposable.Dispose() {}
        }

        TComponent IServiceLocatorKernel.Resolve<TComponent>() => _state.WithExclusiveAccess(state => state.Resolve<TComponent>());


        IEnumerable<T> GetAllInstances<T>() { throw new NotImplementedException(); }

        class NonThreadSafeImplementation : IServiceLocatorKernel
        {
            readonly ComposableDependencyInjectionContainer _parent;
            internal readonly List<ComponentRegistration> RegisteredComponents = new List<ComponentRegistration>();
            readonly IDictionary<Type, List<ComponentRegistration>> ServiceToRegistrationDictionary = new Dictionary<Type, List<ComponentRegistration>>();

            readonly Dictionary<Guid, object> _singletonOverlay = new Dictionary<Guid, object>();
            readonly AsyncLocal<Dictionary<Guid, object>> _scopedOverlay = new AsyncLocal<Dictionary<Guid, object>>();

            public NonThreadSafeImplementation(ComposableDependencyInjectionContainer parent) => _parent = parent;

            public void Register(ComponentRegistration[] registrations)
            {
                RegisteredComponents.AddRange(registrations);
                foreach(var registration in registrations)
                {
                    foreach(var registrationServiceType in registration.ServiceTypes)
                    {
                        ServiceToRegistrationDictionary.GetOrAdd(registrationServiceType, () => new List<ComponentRegistration>()).Add(registration);
                    }
                }
            }

            public TService Resolve<TService>() where TService : class
            {
                if(!ServiceToRegistrationDictionary.TryGetValue(typeof(TService), out var registrations))
                {
                    throw new Exception($"No service of type: {typeof(TService).GetFullNameCompilable()} is registered.");
                }

                if(registrations.Count > 1)
                {
                    throw new Exception($"Requested single instance for service:{typeof(TService)}, but there were multiple services registered.");
                }

                var registration = registrations.Single();
                switch(registration.Lifestyle)
                {
                    case Lifestyle.Singleton:
                        return (TService)ResolveSingletonInstance(registration);
                    case Lifestyle.Scoped:
                        return (TService)ResolveScopedInstance(registration);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            public TService[] ResolveAll<TService>() where TService : class
            {
                if(!ServiceToRegistrationDictionary.TryGetValue(typeof(TService), out var registrations))
                {
                    throw new Exception($"No service of type: {typeof(TService).GetFullNameCompilable()} is registered.");
                }

                var lifestyle = registrations.GroupBy(registration => registration.Lifestyle).Single().Key;

                switch(lifestyle)
                {
                    case Lifestyle.Singleton:
                        return (TService[])ResolveSingletonInstances(registrations);
                    case Lifestyle.Scoped:
                        return (TService[])ResolveScopedInstances(registrations);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            object[] ResolveScopedInstances(List<ComponentRegistration> registration) => registration.Select(ResolveScopedInstance).ToArray();

            object[] ResolveSingletonInstances(List<ComponentRegistration> registration)=> registration.Select(ResolveSingletonInstance).ToArray();

            object ResolveScopedInstance(ComponentRegistration registration)
            {
                if(_scopedOverlay.Value.TryGetValue(registration.Id, out var instance))
                {
                    return instance;
                } else
                {
                    instance = registration.InstantiationSpec.FactoryMethod.Invoke(this);
                    _scopedOverlay.Value.Add(registration.Id, instance);
                    return instance;
                }
            }

            object ResolveSingletonInstance(ComponentRegistration registration)
            {
                if(_singletonOverlay.TryGetValue(registration.Id, out var instance))
                {
                    return instance;
                } else
                {
                    instance = registration.InstantiationSpec.FactoryMethod.Invoke(this);
                    _singletonOverlay.Add(registration.Id, instance);
                    return instance;
                }
            }

            object CreateInstanceForComponent<TService>(ComponentRegistration registration) => registration.InstantiationSpec.FactoryMethod.Invoke(this);

            internal void Verify()
            {
                //todo: Implement some validation here?
            }

            bool _verified;

            internal IServiceLocator CreateServiceLocator()
            {
                if(!_verified)
                {
                    _verified = true;
                    Verify();
                }

                return _parent;
            }

            public IDisposable BeginScope()
            {
                if(_scopedOverlay.Value != null)
                {
                    throw new Exception("Already has scope....");
                }

                _scopedOverlay.Value = new Dictionary<Guid, object>();

                return Disposable.Create(() => _parent._state.WithExclusiveAccess(state => state._scopedOverlay.Value = null));
            }
        }
    }
}

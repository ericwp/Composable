﻿using System;
using System.Collections.Generic;
using System.Linq;
using Composable.Contracts;
using Composable.System;
using Composable.System.Linq;
using JetBrains.Annotations;

// ReSharper disable UnusedMember.Global

namespace Composable.DependencyInjection
{
    ///<summary>A lease to use a component.
    /// <para>Should be disposed as soon as the component is no longer in use.</para>
    /// <para>An exception is thrown if dispose fails to be called. </para>
    /// <para>should inherit from <see cref="StrictlyManagedResourceBase{TInheritor}"/> or have a member field of type: <see cref="StrictlyManagedResource{TManagedResource}"/></para>
    /// </summary>
    public interface IComponentLease<out TComponent> : IDisposable
    {
        TComponent Instance { get; }
    }

    public interface IMultiComponentLease<out TComponent> : IDisposable
    {
        TComponent[] Instances { get; }
    }

    public interface IDependencyInjectionContainer : IDisposable
    {
        IRunMode RunMode { get; }
        void Register(params ComponentRegistration[] registrations);
        IEnumerable<ComponentRegistration> RegisteredComponents();
        IServiceLocator CreateServiceLocator();
    }

    public interface IRunMode
    {
        bool IsTesting { get; }
        TestingMode Mode { get; }
    }

    class RunMode : IRunMode
    {
        readonly bool _isTesting;
        bool IRunMode.IsTesting => _isTesting;
        public TestingMode Mode { get; }

        public static readonly IRunMode Production = new RunMode(isTesting:false, mode: TestingMode.DatabasePool);

        public RunMode(bool isTesting, TestingMode mode)
        {
            Mode = mode;
            _isTesting = isTesting;
        }
    }

    ///<summary></summary>
    public interface IServiceLocator : IDisposable
    {
        IComponentLease<TComponent> Lease<TComponent>() where TComponent : class;
        IMultiComponentLease<TComponent> LeaseAll<TComponent>() where TComponent : class;
        IDisposable BeginScope();
    }

    interface IServiceLocatorKernel
    {
        TComponent Resolve<TComponent>() where TComponent : class;
    }

    public static class ServiceLocator
    {
        internal static TComponent Resolve<TComponent>(this IServiceLocator @this) where TComponent : class => @this.Lease<TComponent>()
                                                                                         .Instance;

        internal static TComponent[] ResolveAll<TComponent>(this IServiceLocator @this) where TComponent : class => @this.LeaseAll<TComponent>()
                                                                                         .Instances;

        public static void Use<TComponent>(this IServiceLocator @this,[InstantHandle] Action<TComponent> useComponent) where TComponent : class
        {
            using (var lease = @this.Lease<TComponent>())
            {
                useComponent(lease.Instance);
            }
        }

        public static TResult Use<TComponent, TResult>(this IServiceLocator @this, Func<TComponent, TResult> useComponent) where TComponent : class
        {
            using (var lease = @this.Lease<TComponent>())
            {
                return useComponent(lease.Instance);
            }
        }

        internal static void UseAll<TComponent>(this IServiceLocator @this, [InstantHandle] Action<TComponent[]> useComponent) where TComponent : class
        {
            using (var lease = @this.LeaseAll<TComponent>())
            {
                useComponent(lease.Instances);
            }
        }

        public static TResult UseAll<TComponent, TResult>(this IServiceLocator @this, Func<TComponent[], TResult> useComponent) where TComponent : class
        {
            using (var lease = @this.LeaseAll<TComponent>())
            {
                return useComponent(lease.Instances);
            }
        }
    }

    public static class Component
    {
        internal static ComponentRegistrationBuilderInitial<TService1> For<TService1, TService2, TService3>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2, TService3>());

        internal static ComponentRegistrationBuilderInitial<TService1> For<TService1, TService2>() where TService1 : class => For<TService1>(Seq.OfTypes<TService2>());

        public static ComponentRegistrationBuilderInitial<TService> For<TService>() where TService : class => For<TService>(new List<Type>());

        internal static ComponentRegistrationBuilderInitial<TService> For<TService>(IEnumerable<Type> additionalServices) where TService : class => new ComponentRegistrationBuilderInitial<TService>(additionalServices);

        public class ComponentRegistrationBuilderInitialBase
        {
            protected IEnumerable<Type> ServiceTypes { get; }
            protected ComponentRegistrationBuilderInitialBase(IEnumerable<Type> serviceTypes) => ServiceTypes = serviceTypes;
        }

        public class ComponentRegistrationBuilderInitial<TService> : ComponentRegistrationBuilderInitialBase where TService : class
        {
            internal ComponentRegistrationBuilderInitial(IEnumerable<Type> serviceTypes) : base(serviceTypes.Concat(new List<Type>() {typeof(TService)})) {}

            public ComponentRegistrationBuilderWithInstantiationSpec<TService> ImplementedBy<TImplementation>()
            {
                Contract.Arguments.That(ServiceTypes.All(serviceType => serviceType.IsAssignableFrom(typeof(TImplementation))), "The implementing type must implement all the service interfaces.");
                return new ComponentRegistrationBuilderWithInstantiationSpec<TService>(ServiceTypes, InstantiationSpec.ImplementedBy(typeof(TImplementation)));
            }

            internal ComponentRegistrationBuilderWithInstantiationSpec<TService> UsingFactoryMethod<TImplementation>(Func<IServiceLocatorKernel, TImplementation> factoryMethod)
                where TImplementation : TService
            {
                return new ComponentRegistrationBuilderWithInstantiationSpec<TService>(ServiceTypes, InstantiationSpec.FromFactoryMethod(serviceLocator => factoryMethod(serviceLocator)));
            }

        }

        public class ComponentRegistrationBuilderWithInstantiationSpec<TService> where TService : class
        {
            readonly IEnumerable<Type> _serviceTypes;
            readonly InstantiationSpec _instantInstatiationSpec;

            internal ComponentRegistrationBuilderWithInstantiationSpec(IEnumerable<Type> serviceTypes, InstantiationSpec instantInstatiationSpec)
            {
                _serviceTypes = serviceTypes;
                _instantInstatiationSpec = instantInstatiationSpec;
            }

            internal ComponentRegistration<TService> LifestyleSingleton() => new ComponentRegistration<TService>(Lifestyle.Singleton, _serviceTypes, _instantInstatiationSpec);
            public ComponentRegistration<TService> LifestyleScoped() => new ComponentRegistration<TService>(Lifestyle.Scoped, _serviceTypes, _instantInstatiationSpec);

        }
    }

    enum Lifestyle
    {
        Singleton,
        Scoped
    }

    class InstantiationSpec
    {
        internal object Instance { get; }
        internal Type ImplementationType { get; }
        internal Func<IServiceLocatorKernel, object> FactoryMethod { get; }

        internal static InstantiationSpec FromInstance(object instance) => new InstantiationSpec(instance);

        internal static InstantiationSpec ImplementedBy(Type implementationType) => new InstantiationSpec(implementationType);

        internal static InstantiationSpec FromFactoryMethod(Func<IServiceLocatorKernel, object> factoryMethod) => new InstantiationSpec(factoryMethod);

        InstantiationSpec(Type implementationType) => ImplementationType = implementationType;

        InstantiationSpec(Func<IServiceLocatorKernel, object> factoryMethod) => FactoryMethod = factoryMethod;

        InstantiationSpec(object instance) => Instance = instance;
    }

    public abstract class ComponentRegistration
    {
        internal IEnumerable<Type> ServiceTypes { get; }
        internal InstantiationSpec InstantiationSpec { get; }
        internal Lifestyle Lifestyle { get; }

        internal ComponentRegistration(Lifestyle lifestyle, IEnumerable<Type> serviceTypes, InstantiationSpec instantiationSpec)
        {
            serviceTypes = serviceTypes.ToList();

            Contract.Arguments.That(lifestyle == Lifestyle.Singleton || instantiationSpec.Instance == null, $"{nameof(InstantiationSpec.Instance)} registrations must be {nameof(Lifestyle.Singleton)}s");

            ServiceTypes = serviceTypes;
            InstantiationSpec = instantiationSpec;
            Lifestyle = lifestyle;
        }

        internal abstract ComponentRegistration CreateCloneRegistration(IServiceLocator currentLocator);
    }

    public class ComponentRegistration<TService> : ComponentRegistration where TService : class
    {
        bool ShouldDelegateToParentWhenCloning { get; set; }

        internal ComponentRegistration<TService> DelegateToParentServiceLocatorWhenCloning()
        {
            Contract.Assert.That(Lifestyle == Lifestyle.Singleton, "Only singletons can be delegated to parent container since disposal concern handling becomes very confused for any other lifestyle");
            ShouldDelegateToParentWhenCloning = true;
            return this;
        }

        internal override ComponentRegistration CreateCloneRegistration(IServiceLocator currentLocator)
        {
            if(!ShouldDelegateToParentWhenCloning)
            {
                return this;
            }

            //We must use singleton instance registrations when delegating because otherwise the containers will both attempt to dispose the service.
            //Instance registrations are not disposed.
            return new ComponentRegistration<TService>(
                lifestyle: Lifestyle.Singleton,
                serviceTypes: ServiceTypes,
                instantiationSpec: InstantiationSpec.FromInstance(currentLocator.Resolve<TService>())
            );
        }

        internal ComponentRegistration(Lifestyle lifestyle, IEnumerable<Type> serviceTypes, InstantiationSpec instantiationSpec)
            :base(lifestyle, serviceTypes, instantiationSpec)
        {}
    }
}
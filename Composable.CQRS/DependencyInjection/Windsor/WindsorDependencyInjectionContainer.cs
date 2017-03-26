﻿using System;
using System.Linq;
using Castle.Core.Internal;
using Castle.MicroKernel.Lifestyle;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Resolvers.SpecializedResolvers;
using Castle.Windsor;
using Composable.DependencyInjection.Testing;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses;
using Composable.System.Configuration;
using JetBrains.Annotations;

namespace Composable.DependencyInjection.Windsor
{
    static class WindsorDependencyInjectionContainerExtensions
    {
        internal static IDependencyInjectionContainer AsDependencyInjectionContainer(this IWindsorContainer @this) => new WindsorDependencyInjectionContainer(@this);
        internal static IWindsorContainer Unsupported(this IServiceLocator @this) => ((WindsorDependencyInjectionContainer)@this).WindsorContainer;
        internal static IServiceLocator AsServiceLocator(this IWindsorContainer @this) => new WindsorDependencyInjectionContainer(@this);
    }

    public static class WindsorDependencyInjectionContainerFactory
    {
        public static IServiceLocator SetupForTesting([InstantHandle]Action<IDependencyInjectionContainer> setup)
        {
            var @this = new WindsorDependencyInjectionContainer();


            @this.ConfigureWiringForTestsCallBeforeAllOtherWiring();

            var dummyTimeSource = DummyTimeSource.Now;
            var registry = new MessageHandlerRegistry();
            var bus = new TestingOnlyServiceBus(dummyTimeSource, registry);

            @this.Register(
                           CComponent.For<IUtcTimeTimeSource, DummyTimeSource>()
                                     .Instance(dummyTimeSource)
                                     .LifestyleSingleton(),
                           CComponent.For<IMessageHandlerRegistrar>()
                                     .Instance(registry)
                                     .LifestyleSingleton(),
                           CComponent.For<IServiceBus, IMessageSpy>()
                                     .Instance(bus)
                                     .LifestyleSingleton(),
                           CComponent.For<IConnectionStringProvider>()
                                     .Instance(new DummyConnectionStringProvider())
                                     .LifestyleSingleton()
                          );

            setup(@this);

            @this.ConfigureWiringForTestsCallAfterAllOtherWiring();

            return @this.CreateServiceLocator();
        }
    }

    class WindsorDependencyInjectionContainer : IDependencyInjectionContainer, IServiceLocator
    {
        internal readonly IWindsorContainer WindsorContainer;
        public WindsorDependencyInjectionContainer(IWindsorContainer windsorContainer = null)
        {
            WindsorContainer = windsorContainer;
            if(WindsorContainer == null)
            {
                WindsorContainer = new WindsorContainer();
                WindsorContainer.Kernel.Resolver.AddSubResolver(new CollectionResolver(WindsorContainer.Kernel));
            }
        }
        public IDependencyInjectionContainer Register(params CComponentRegistration[] registration)
        {
            var windsorRegistrations = registration.Select(ToWindsorRegistration)
                                                   .ToArray();

            WindsorContainer.Register(windsorRegistrations);
            return this;
        }
        public IServiceLocator CreateServiceLocator() => this;

        public bool IsTestMode => WindsorContainer.Kernel.HasComponent(typeof(TestModeMarker));

        IRegistration ToWindsorRegistration(CComponentRegistration componentRegistration)
        {
            ComponentRegistration<object> registration = Component.For(componentRegistration.ServiceTypes);

            if(componentRegistration.InstantiationSpec.Instance != null)
            {
                registration.Instance(componentRegistration.InstantiationSpec.Instance);
            }else if(componentRegistration.InstantiationSpec.ImplementationType != null)
            {
                registration.ImplementedBy(componentRegistration.InstantiationSpec.ImplementationType);
            } else if(componentRegistration.InstantiationSpec.FactoryMethod != null)
            {
                registration.UsingFactoryMethod(() => componentRegistration.InstantiationSpec.FactoryMethod(CreateServiceLocator()));
            }else
            {
                throw new Exception($"Invalid {nameof(InstantiationSpec)}");
            }


            if(!componentRegistration.Name.IsNullOrEmpty())
            {
                registration = registration.Named(componentRegistration.Name);
            }

            switch(componentRegistration.Lifestyle)
            {
                case Lifestyle.Singleton:
                    return registration.LifestyleSingleton();
                case Lifestyle.Scoped:
                    return registration.LifestyleScoped();
                default:
                    throw new ArgumentOutOfRangeException(nameof(componentRegistration.Lifestyle));
            }
        }

        public IComponentLease<TComponent> Lease<TComponent>(string componentName) => new WindsorComponentLease<TComponent>(WindsorContainer.Resolve<TComponent>(componentName), WindsorContainer);
        public IComponentLease<TComponent> Lease<TComponent>() => new WindsorComponentLease<TComponent>(WindsorContainer.Resolve<TComponent>(), WindsorContainer);
        public IMultiComponentLease<TComponent> LeaseAll<TComponent>() => new WindsorMultiComponentLease<TComponent>(WindsorContainer.ResolveAll<TComponent>().ToArray(), WindsorContainer);
        public IDisposable BeginScope() => WindsorContainer.BeginScope();
        public void Dispose() => WindsorContainer.Dispose();
    }
}
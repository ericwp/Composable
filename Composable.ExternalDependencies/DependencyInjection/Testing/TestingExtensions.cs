﻿using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Serialization.NewtonSoft;
using Composable.System.Configuration;
using Composable.System.Data.SqlClient;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;

namespace Composable.DependencyInjection.Testing
{
    static class TestingExtensions
    {

        static readonly ISqlConnectionProvider MasterDbConnectionProvider = new AppConfigConnectionStringProvider().GetConnectionProvider(parameterName: "MasterDB");
        /// <summary>
        /// <para>SingleThreadUseGuard is registered for the component ISingleContextUseGuard</para>
        /// </summary>
        public static void ConfigureWiringForTestsCallBeforeAllOtherWiring(this IDependencyInjectionContainer @this, TestingMode mode = TestingMode.RealComponents)
        {
            MasterDbConnectionProvider.UseConnection(action: _ => {});//evaluate lazy here in order to not pollute profiler timings of component resolution or registering.
            var dummyTimeSource = DummyTimeSource.Now;
            var registry = new MessageHandlerRegistry();
            var bus = new TestingOnlyServiceBus(dummyTimeSource, registry);
            var runMode = new RunMode(isTesting:true, mode:mode);

            @this.Register(Component.For<IRunMode>()
                                    .UsingFactoryMethod(factoryMethod: _ => runMode)
                                    .LifestyleSingleton(),
                           Component.For<ISingleContextUseGuard>()
                                    .ImplementedBy<SingleThreadUseGuard>()
                                    .LifestyleScoped(),
                           Component.For<IEventStoreEventSerializer>()
                                    .ImplementedBy<NewtonSoftEventStoreEventSerializer>()
                                    .LifestyleScoped(),
                           Component.For<IUtcTimeTimeSource, DummyTimeSource>()
                                    .UsingFactoryMethod(factoryMethod: _ => dummyTimeSource)
                                    .LifestyleSingleton()
                                    .DelegateToParentServiceLocatorWhenCloning(),
                           Component.For<IMessageHandlerRegistrar>()
                                    .UsingFactoryMethod(factoryMethod: _ => registry)
                                    .LifestyleSingleton(),
                           Component.For<IServiceBus, IMessageSpy>()
                                    .UsingFactoryMethod(factoryMethod: _ => bus)
                                    .LifestyleSingleton(),
                           Component.For<IConnectionStringProvider>()
                                    .UsingFactoryMethod(factoryMethod: locator => new SqlServerDatabasePoolConnectionStringProvider(MasterDbConnectionProvider.ConnectionString))
                                    .LifestyleSingleton()
                                    .DelegateToParentServiceLocatorWhenCloning()
            );
        }


        public static IServiceLocator Clone(this IServiceLocator @this)
        {
            var sourceContainer = (IDependencyInjectionContainer)@this;

            var cloneContainer = DependencyInjectionContainer.Create();

            sourceContainer.RegisteredComponents()
                           .ForEach(action: componentRegistration => cloneContainer.Register(componentRegistration.CreateCloneRegistration(@this)));

            return cloneContainer.CreateServiceLocator();
        }
    }
}

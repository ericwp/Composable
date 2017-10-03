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
        static readonly ISqlConnection MasterDbConnection = new AppConfigSqlConnectionProvider().GetConnectionProvider(parameterName: "MasterDB");
        /// <summary>
        /// <para>SingleThreadUseGuard is registered for the component ISingleContextUseGuard</para>
        /// </summary>
        public static void ConfigureWiringForTestsCallBeforeAllOtherWiring(this IDependencyInjectionContainer @this, TestingMode mode = TestingMode.DatabasePool)
        {
            if(mode == TestingMode.DatabasePool)
            {
                MasterDbConnection.UseConnection(action: _ => {}); //evaluate lazy here in order to not pollute profiler timings of component resolution or registering.
            }

            @this.Register(Component.For<IRunMode>()
                                    .UsingFactoryMethod(factoryMethod: _ => new RunMode(isTesting: true, mode: mode))
                                    .LifestyleSingleton(),
                           Component.For<IGlobalBusStrateTracker>()
                                    .UsingFactoryMethod(_ => new GlobalBusStrateTracker())
                                    .LifestyleSingleton(),
                           Component.For<IMessageHandlerRegistry, IMessageHandlerRegistrar>()
                                    .UsingFactoryMethod(_ => new MessageHandlerRegistry())
                                    .LifestyleSingleton(),
                           Component.For<ISingleContextUseGuard>()
                                    .ImplementedBy<SingleThreadUseGuard>()
                                    .LifestyleScoped(),
                           Component.For<IEventStoreEventSerializer>()
                                    .ImplementedBy<NewtonSoftEventStoreEventSerializer>()
                                    .LifestyleScoped(),
                           Component.For<IUtcTimeTimeSource, DummyTimeSource>()
                                    .UsingFactoryMethod(factoryMethod: _ => DummyTimeSource.Now)
                                    .LifestyleSingleton()
                                    .DelegateToParentServiceLocatorWhenCloning(),
                           Component.For<IInProcessServiceBus, IMessageSpy>()
                                    .UsingFactoryMethod(_ => new InProcessServiceBus(_.Resolve<IMessageHandlerRegistry>()))
                                    .LifestyleSingleton(),
                           Component.For<IServiceBus, ServiceBus>()
                                    .UsingFactoryMethod(factoryMethod: kernel =>
                                                            new ServiceBus("testendpoint",
                                                                           kernel.Resolve<DummyTimeSource>(),
                                                                           kernel.Resolve<IInProcessServiceBus>(),
                                                                           kernel.Resolve<IGlobalBusStrateTracker>()))
                                    .LifestyleSingleton(),
                           Component.For<ISqlConnectionProvider>()
                                    .UsingFactoryMethod(factoryMethod: locator => new SqlServerDatabasePoolSqlConnectionProvider(MasterDbConnection.ConnectionString))
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

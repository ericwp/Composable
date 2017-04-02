﻿using System.Linq;
using Composable.Contracts;
using Composable.DependencyInjection.Windsor;
using Composable.GenericAbstractions.Time;
using Composable.Messaging.Buses;
using Composable.Persistence.EventStore;
using Composable.Persistence.EventStore.Serialization.NewtonSoft;
using Composable.System.Configuration;
using Composable.SystemExtensions.Threading;

namespace Composable.DependencyInjection.Testing
{
    static class TestingExtensions
    {
        /// <summary>
        /// <para>SingleThreadUseGuard is registered for the component ISingleContextUseGuard</para>
        /// </summary>
        public static void ConfigureWiringForTestsCallBeforeAllOtherWiring(this IDependencyInjectionContainer @this, TestingMode mode = TestingMode.RealComponents)
        {
            var dummyTimeSource = DummyTimeSource.Now;
            var registry = new MessageHandlerRegistry();
            var bus = new TestingOnlyServiceBus(dummyTimeSource, registry);
            var runMode = new RunMode(isTesting:true, mode:mode);

            var masterConnectionString = new ConnectionStringConfigurationParameterProvider().GetConnectionString("MasterDB")
                                                                                             .ConnectionString;
            var connectionStringProvider = new SqlServerDatabasePoolConnectionStringProvider(masterConnectionString);

            @this.Register(Component.For<IRunMode>()
                                    .UsingFactoryMethod(_ => runMode)
                                    .LifestyleSingleton(),
                           Component.For<ISingleContextUseGuard>()
                                    .ImplementedBy<SingleThreadUseGuard>()
                                    .LifestyleScoped(),
                           Component.For<IEventStoreEventSerializer>()
                                    .ImplementedBy<NewtonSoftEventStoreEventSerializer>()
                                    .LifestyleScoped(),
                           Component.For<IUtcTimeTimeSource, DummyTimeSource>()
                                    .UsingFactoryMethod(_ => dummyTimeSource)
                                    .LifestyleSingleton(),
                           Component.For<IMessageHandlerRegistrar>()
                                    .UsingFactoryMethod(_ => registry)
                                    .LifestyleSingleton(),
                           Component.For<IServiceBus, IMessageSpy>()
                                    .UsingFactoryMethod(_ => bus)
                                    .LifestyleSingleton(),
                           Component.For<IConnectionStringProvider>()
                                    .UsingFactoryMethod(locator => connectionStringProvider)
                                    .LifestyleSingleton()
                          );
            @this.CreateServiceLocator()
                 .Resolve<IConnectionStringProvider>();//Trigger resolving the dabasepool so that it will be properly disposed with the container
        }


        public static IServiceLocator Clone(this IServiceLocator @this)
        {
            var sourceContainer = ((WindsorDependencyInjectionContainer)@this);
            var components = sourceContainer.RegisteredComponents().ToList();
            Contract.Assert.That(components.Count( component => component.ServiceTypes.Contains(typeof(IConnectionStringProvider))) == 1,
                                $"We can only handle the case with a single {nameof(IConnectionStringProvider)}");

            var toRegister = components.Where(component => !component.ServiceTypes.Contains(typeof(IConnectionStringProvider)))
                                                                    .ToList();

            var cloneContainer = DependencyInjectionContainer.Create();

            cloneContainer.Register(
                Component.For<IConnectionStringProvider>()
                    .UsingFactoryMethod(_ => sourceContainer.Resolve<IConnectionStringProvider>())
                    .LifestyleScoped()
                );

            toRegister.ForEach(component => cloneContainer.Register(component));

            return cloneContainer.CreateServiceLocator();
        }
    }
}

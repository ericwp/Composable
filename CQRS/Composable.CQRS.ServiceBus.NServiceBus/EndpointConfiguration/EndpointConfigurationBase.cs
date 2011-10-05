﻿#region usings

using System.Configuration;
using System.Runtime.Serialization;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Releasers;
using Castle.Windsor;
using Composable.CQRS.EventSourcing;
using Composable.CQRS.ServiceBus.NServiceBus.Windsor;
using Composable.CQRS.Windsor;
using NServiceBus;
using NServiceBus.Unicast.Config;
using log4net.Config;

#endregion

namespace Composable.CQRS.ServiceBus.NServiceBus.EndpointConfiguration
{
    public abstract class NServicebusEndpointConfigurationBase<TInheritor> : IWantCustomInitialization
        where TInheritor : IConfigureThisEndpoint
    {
        private WindsorContainer _container;

        protected virtual void StartNServiceBus(WindsorContainer windsorContainer)
        {
            var config = Configure.With()
                .CastleWindsorBuilder(container: windsorContainer)
                .Log4Net();

            var config2 = ConfigureSubscriptionStorage(config);


            var busConfig = config2.XmlSerializer()
                .MsmqTransport()
                .IsTransactional(true)
                .PurgeOnStartup(false)
                .UnicastBus();

            var busConfig2 = LoadMessageHandlers(busConfig, First<EmptyHandler>.Then<CatchSerializationErrors>());

            busConfig2.ImpersonateSender(false)
                .CreateBus()
                .Start();
        }

        protected virtual ConfigUnicastBus LoadMessageHandlers(ConfigUnicastBus busConfig, First<EmptyHandler> required)
        {
            var busConfig2 = busConfig.LoadMessageHandlers(required);
            return busConfig2;
        }

        protected virtual Configure ConfigureSubscriptionStorage(Configure config)
        {
            return config.DBSubcriptionStorage();
        }


        public void Init()
        {
            XmlConfigurator.Configure();
            _container = new WindsorContainer();

            _container.Register(Component.For<IWindsorContainer, WindsorContainer>().Instance(_container));

            //Forget this and you leak memory like CRAZY!
            _container.Kernel.ReleasePolicy = new NoTrackingReleasePolicy();

            ConfigureContainer(_container);

            StartNServiceBus(_container);

            _container.AssertConfigurationValid();
        }

        protected abstract void ConfigureContainer(IWindsorContainer container);

        protected static string GetConnectionStringFromConfiguration(string key)
        {
            var connectionString = ConfigurationManager.ConnectionStrings[key];
            if (connectionString == null)
                throw new ConfigurationErrorsException(string.Format("Missing connectionstring for '{0}'", key));
            return connectionString.ConnectionString;
        }
    }

    public class WillNeverBeUsed : IMessage
    {
    }

    public class EmptyHandler : IMessageHandler<WillNeverBeUsed>
    {
        public void Handle(WillNeverBeUsed message)
        {
        }
    }

    public class CatchSerializationErrors : IMessageHandler<IMessage>
    {
        private readonly IBus _bus;

        public CatchSerializationErrors(IBus bus)
        {
            _bus = bus;
        }

        public void Handle(IMessage message)
        {
            if (message.GetType() == typeof (IMessage) || message.GetType() == typeof (AggregateRootEvent))
            {
                throw new SerializationException("Message failed to serialize correctly");
            }
        }
    }
}
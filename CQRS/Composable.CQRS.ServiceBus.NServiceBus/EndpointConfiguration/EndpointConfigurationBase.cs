﻿using System.Configuration;
using Castle.MicroKernel.Releasers;
using Castle.Windsor;
using Composable.CQRS.ServiceBus.NServiceBus.Windsor;
using Composable.CQRS.Windsor;
using NServiceBus;
using log4net.Config;

namespace Composable.CQRS.ServiceBus.NServiceBus.EndpointConfiguration
{
    public abstract class NservicebusEndpointConfigurationBase<TInheritor> : IWantCustomInitialization
        where TInheritor : IConfigureThisEndpoint
    {
        private WindsorContainer _container;

        private void StartNServiceBus(WindsorContainer windsorContainer)
        {
            Configure.With()
                .CastleWindsorBuilder(container: windsorContainer)
                .Log4Net()
                .DBSubcriptionStorage()
                .XmlSerializer()
                .MsmqTransport()
                .IsTransactional(true)
                .PurgeOnStartup(false)
                .UnicastBus()
                .LoadMessageHandlers()
                .ImpersonateSender(false)
                .CreateBus()
                .Start();
        }


        public void Init()
        {
            XmlConfigurator.Configure();
            _container = new WindsorContainer();

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
}
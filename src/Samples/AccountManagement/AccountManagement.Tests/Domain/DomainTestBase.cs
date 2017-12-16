﻿using AccountManagement.Domain;
using Composable.DependencyInjection;
using Composable.Messaging.Buses;
using Composable.System;
using NUnit.Framework;

namespace AccountManagement.Tests.Domain
{
    [TestFixture] public abstract class DomainTestBase
    {
        protected IServiceLocator ServiceLocator { get; private set; }
        protected IMessageSpy MessageSpy => ServiceLocator.Lease<IMessageSpy>().Instance;

        StrictAggregateDisposable _managedResources;
        ITestingEndpointHost _host;
        IEndpoint _domainEndpoint;

        [SetUp] public void SetupContainerAndBeginScope()
        {
            _host = EndpointHost.Testing.CreateHost(DependencyInjectionContainer.Create);
            _domainEndpoint = _host.RegisterAndStartEndpoint("UserManagement.Domain",
                                                             builder =>
                                                             {
                                                                 AccountManagementDomainBootstrapper.SetupContainer(builder.Container);
                                                                 AccountManagementDomainBootstrapper.RegisterHandlers(builder.RegisterHandlers, builder.Container.CreateServiceLocator());
                                                             });

            ServiceLocator = _domainEndpoint.ServiceLocator;

            _managedResources = StrictAggregateDisposable.Create(ServiceLocator.BeginScope(), _host);
        }

        [TearDown] public void DisposeScopeAndContainer() { _managedResources.Dispose(); }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Composable.Contracts;
using Composable.DependencyInjection;
using Composable.Messaging.Buses.Implementation;
using Composable.Refactoring.Naming;
using Composable.System.Linq;

namespace Composable.Messaging.Buses
{
    class Endpoint : IEndpoint
    {
        readonly EndpointConfiguration _configuration;
        public bool IsRunning { get; private set; }
        public Endpoint(IServiceLocator serviceLocator, EndpointConfiguration configuration)
        {
            Assert.Argument.Assert(serviceLocator != null, configuration != null);
            ServiceLocator = serviceLocator;
            _configuration = configuration;
        }
        public EndpointId Id => _configuration.Id;
        public IServiceLocator ServiceLocator { get; }

        public EndPointAddress Address => _inbox.Address;
        IGlobalBusStateTracker _globalStateTracker;
        IInbox _inbox;

        IServiceBusControl BusControl => ServiceLocator.Resolve<IServiceBusControl>();

        public async Task InitAsync()
        {
            Assert.State.Assert(!IsRunning);

            IsRunning = true;

            _globalStateTracker = ServiceLocator.Resolve<IGlobalBusStateTracker>();
            _inbox = ServiceLocator.Resolve<IInbox>();

            RunSanityChecks();

            await BusControl.StartAsync();
        }

        public async Task ConnectAsync(IEnumerable<EndPointAddress> knownEndpointAddresses)
        {
            var endpointTransport = ServiceLocator.Resolve<IInterprocessTransport>();
            await Task.WhenAll(knownEndpointAddresses.Select(address => endpointTransport.ConnectAsync(address)));
        }

        static void RunSanityChecks()
        {
            AssertAllTypesNeedingMappingsAreMapped();
        }

        //todo: figure out how to do this sanely
        static void AssertAllTypesNeedingMappingsAreMapped()
        {
        }

        public void Stop()
        {
            Assert.State.Assert(IsRunning);
            IsRunning = false;
            BusControl.Stop();
        }

        public void AwaitNoMessagesInFlight(TimeSpan? timeoutOverride) => _globalStateTracker?.AwaitNoMessagesInFlight(timeoutOverride);

        public void Dispose()
        {
            if(IsRunning) Stop();
            ServiceLocator.Dispose();
        }
    }
}

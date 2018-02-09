﻿using System;
using System.Collections.Generic;
using System.Linq;
using Composable.DependencyInjection;

namespace Composable.Messaging.Buses
{
    class TestingEndpointHost : EndpointHost, ITestingEndpointHost
    {
        public TestingEndpointHost(IRunMode mode, Func<IRunMode, IDependencyInjectionContainer> containerFactory, bool createClientEndpoint = true) : base(mode, containerFactory)
        {
            if(createClientEndpoint)
            {
                RegisterClientEndpoint();
            }
        }

        public void WaitForEndpointsToBeAtRest(TimeSpan? timeoutOverride = null) { Endpoints.ForEach(endpoint => endpoint.AwaitNoMessagesInFlight(timeoutOverride)); }


        public IEndpoint RegisterTestingEndpoint(string name = null, EndpointId id = null, Action<IEndpointBuilder> setup = null)
        {
            var endpointId  = id ?? new EndpointId(Guid.NewGuid());
            name = name ?? $"TestingEndpoint-{endpointId.GuidValue}";
            setup = setup ?? (builder => {});
            return RegisterEndpoint(name, endpointId, setup);
        }

        public TException AssertThrown<TException>() where TException : Exception
        {
            WaitForEndpointsToBeAtRest();
            var matchingException = GetThrownExceptions().OfType<TException>().SingleOrDefault();
            if(matchingException == null)
            {
                throw new Exception("Matching exception not thrown.");
            }
            _handledExceptions.Add(matchingException);
            return matchingException;
        }

        protected override void InternalDispose()
        {
            WaitForEndpointsToBeAtRest();

            var unHandledExceptions = GetThrownExceptions().Except(_handledExceptions).ToList();

            base.InternalDispose();


            if(unHandledExceptions.Any())
            {
                throw new AggregateException("Unhandled exceptions thrown in bus", unHandledExceptions.ToArray());
            }
        }

        readonly List<Exception> _handledExceptions = new List<Exception>();

        List<Exception> GetThrownExceptions() => GlobalBusStateTracker.GetExceptions().ToList();
    }
}

﻿using Composable.DependencyInjection;
using Composable.Messaging;
using Composable.System.Diagnostics;
using Composable.Testing.Performance;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.Messaging.ServiceBusSpecification.Performance
{
    [TestFixture] public class RemoteQueryPerformanceTests : PerformanceTestBase
    {
        [Test] public void Given_30_client_threads_Runs_100_remote_queries_in_30_milliSecond()
        {
            var navigationSpecification = NavigationSpecification.GetRemote(new MyQuery());

            //Warmup
            StopwatchExtensions.TimeExecutionThreaded(action: () => ClientEndpoint.ServiceLocator.ExecuteInIsolatedScope(() => ClientBusSession.Execute(navigationSpecification)), iterations: 10, maxDegreeOfParallelism: 30);

            TimeAsserter.ExecuteThreaded(action: () =>ClientEndpoint.ServiceLocator.ExecuteInIsolatedScope(() => ClientBusSession.Execute(navigationSpecification)), iterations: 100, maxTotal: 30.Milliseconds(), maxDegreeOfParallelism: 30);
        }

        [Test] public void Given_1_client_thread_Runs_100_remote_queries_in_100_milliseconds()
        {
            var navigationSpecification = NavigationSpecification.GetRemote(new MyQuery());

            //Warmup
            StopwatchExtensions.TimeExecutionThreaded(action: () => ClientEndpoint.ServiceLocator.ExecuteInIsolatedScope(() => ClientBusSession.Execute(navigationSpecification)), iterations: 10, maxDegreeOfParallelism: 30);

            TimeAsserter.Execute(action: () => ClientEndpoint.ServiceLocator.ExecuteInIsolatedScope(() => ClientBusSession.Execute(navigationSpecification)), iterations: 100, maxTotal: 100.Milliseconds());
        }
    }
}

﻿using System;
using Composable.DependencyInjection;
using Composable.System.Diagnostics;
using Composable.Testing.Performance;
using FluentAssertions;
using NCrunch.Framework;
using NUnit.Framework;

namespace Composable.Tests.Messaging.ServiceBusSpecification.Performance
{
    [TestFixture, Performance, Isolated, Serial]
    public class Local_Query_performance_tests : PerformanceTestBase
    {
        [Test, Isolated, Serial] public void Runs_100_000__MultiThreaded_local_requests_making_1_local_query_each_in_100_milliseconds() =>
            RunScenario(threaded: true, requests: 100_000, queriesPerRequest: 1, maxTotal: 100.Milliseconds());

        [Test, Isolated, Serial] public void Runs_100_000_SingleThreaded_local_requests_making_a_single_local_query_in_260_milliseconds() =>
            RunScenario(threaded: false, requests: 100_000, queriesPerRequest: 1, maxTotal: 260.Milliseconds());

        [Test, Isolated, Serial] public void Runs_10_000__MultiThreaded_local_requests_making_10_local_queríes_each_in_70_milliseconds() =>
            RunScenario(threaded: true, requests: 10_000, queriesPerRequest: 10, maxTotal: 70.Milliseconds());

        [Test, Isolated, Serial] public void Runs_10_000__SingleThreaded_local_requests_making_10_local_queríes_each_in_100_milliseconds() =>
            RunScenario(threaded: false, requests: 10_000, queriesPerRequest: 10, maxTotal: 100.Milliseconds());

        void RunScenario(bool threaded, int requests, int queriesPerRequest, TimeSpan maxTotal)
        {
            void RunRequest()
            {
                ServerEndpoint.ServiceLocator.ExecuteInIsolatedScope(() =>
                {
                    for(int i = 0; i < queriesPerRequest; i++)
                    {
                        ServerBusSession.Execute(new MyLocalQuery());
                    }
                });
            }

            if(threaded)
            {
                StopwatchExtensions.TimeExecutionThreaded(RunRequest, iterations: requests);
                TimeAsserter.ExecuteThreaded(RunRequest, iterations: requests, maxTotal: maxTotal);
            } else
            {
                StopwatchExtensions.TimeExecution(RunRequest, iterations: requests);
                TimeAsserter.Execute(RunRequest, iterations: requests, maxTotal: maxTotal);
            }
        }
    }
}

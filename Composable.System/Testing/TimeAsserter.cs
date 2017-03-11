﻿using System;
using System.Diagnostics;
using System.Threading;
using Composable.System;
using Composable.System.Diagnostics;
using JetBrains.Annotations;

namespace Composable.Testing
{
    static class TimeAsserter
    {
        const string DefaultTimeFormat = "ss\\.fff";

        static PerformanceCounter _totalCpu;

        static void WaitUntilCpuLoadIsBelowPercent(int percent)
        {
            const int waitMilliseconds = 20;
            if (_totalCpu == null)
            {
                _totalCpu = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            }

            var currentValue = (int)_totalCpu.NextValue();
            while (currentValue > percent || currentValue == 0)
            {
                Console.WriteLine($"Waiting {waitMilliseconds} milliseconds for CPU to drop below {percent} percent");
                Thread.Sleep(waitMilliseconds);
                currentValue = (int)_totalCpu.NextValue();
            }
        }

        public static StopwatchExtensions.TimedExecutionSummary Execute
            ([InstantHandle]Action action,
             int iterations = 1,
             TimeSpan? maxAverage = null,
             TimeSpan? maxTotal = null,
             string description = "",
             string timeFormat = DefaultTimeFormat,
             int maxTries = 1)
        {
            var waitForCpuLoadToDropBelowPercent = 50;
            maxAverage = maxAverage != default(TimeSpan) ? maxAverage : TimeSpan.MaxValue;
            maxTotal = maxTotal != default(TimeSpan) ? maxTotal : TimeSpan.MaxValue;

            Func<TimeSpan?, string> format = date => date?.ToString(timeFormat) ?? "";
            StopwatchExtensions.TimedExecutionSummary executionSummary = null;
            for(int tries = 1; tries <= maxTries; tries++)
            {
                WaitUntilCpuLoadIsBelowPercent(waitForCpuLoadToDropBelowPercent);
                executionSummary = StopwatchExtensions.TimeExecution(action: action, iterations: iterations);
                try
                {
                    RunAsserts(maxAverage: maxAverage, maxTotal: maxTotal, executionSummary: executionSummary, format:format);
                }
                catch(Exception e)
                {
                    Console.WriteLine($"Try: {tries} {e.GetType().FullName}: {e.Message}");
                    if(tries >= maxTries)
                    {
                        PrintSummary(iterations, maxAverage, maxTotal, description, format, executionSummary);
                        throw;
                    }
                    continue;
                }
                PrintSummary(iterations, maxAverage, maxTotal, description, format, executionSummary);
                break;
            }

            return executionSummary;
        }

        public static StopwatchExtensions.TimedThreadedExecutionSummary ExecuteThreaded
            ([InstantHandle]Action action,
             int iterations = 1,
             TimeSpan? maxAverage = null,
             TimeSpan? maxTotal = null,
             bool timeIndividualExecutions = false,
             string description = "",
             string timeFormat = DefaultTimeFormat,
             int maxTries = 1)
        {
            maxAverage = maxAverage != default(TimeSpan) ? maxAverage : TimeSpan.MaxValue;
            maxTotal = maxTotal != default(TimeSpan) ? maxTotal : TimeSpan.MaxValue;

            StopwatchExtensions.TimedThreadedExecutionSummary executionSummary = null;

            // ReSharper disable AccessToModifiedClosure

            Func<TimeSpan?, string> format = date => date?.ToString(timeFormat) ?? "";

            Action printResults = () =>
                                  {
                                      PrintSummary(iterations, maxAverage, maxTotal, description, format, executionSummary);

                                      if(timeIndividualExecutions)
                                      {
                                          Console.WriteLine(
                                              $@"  
    Individual execution times    
    Average: {format(executionSummary.IndividualExecutionTimes.Average())}
    Min:     {format(executionSummary.IndividualExecutionTimes.Min())}
    Max:     {format(executionSummary.IndividualExecutionTimes.Max())}
    Sum:     {format(executionSummary.IndividualExecutionTimes.Sum())}");
                                      }
                                  };
            // ReSharper restore AccessToModifiedClosure

            for (int tries = 1; tries <= maxTries; tries++)
            {
                executionSummary = StopwatchExtensions.TimeExecutionThreaded(action: action, iterations: iterations, timeIndividualExecutions: timeIndividualExecutions);
                try
                {
                    RunAsserts(maxAverage, maxTotal, executionSummary, format);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Try: {tries} {e.GetType().FullName}: {e.Message}");
                    if (tries >= maxTries)
                    {
                        printResults();
                        throw;
                    }
                    continue;
                }
                printResults();
                break;
            }

            return executionSummary;
        }

        static void RunAsserts(TimeSpan? maxAverage, TimeSpan? maxTotal, StopwatchExtensions.TimedExecutionSummary executionSummary, [InstantHandle]Func<TimeSpan?, string> format)
        {
            if(maxTotal.HasValue && executionSummary.Total > maxTotal.Value)
            {
                throw new Exception($"{nameof(maxTotal)}: {format(maxTotal)} exceeded. Was: {format(executionSummary.Total)}");
            }

            if(maxAverage.HasValue && executionSummary.Average > maxAverage.Value)
            {
                throw new Exception($"{nameof(maxAverage)} exceeded");
            }
        }
        static void PrintSummary
            (int iterations, TimeSpan? maxAverage, TimeSpan? maxTotal, string description, [InstantHandle]Func<TimeSpan?, string> format, StopwatchExtensions.TimedExecutionSummary executionSummary)
        {
            if(iterations > 1)
            {
                Console.WriteLine(
                    $@"Executed {iterations} iterations of {description}  
    Total:   {format(executionSummary.Total)} Limit: {format(maxTotal)} 
    Average: {format
                        (executionSummary.Average)} Limit: {format(maxAverage)}");
            }
            else
            {
                Console.WriteLine(
                    $@"Executed {iterations} iterations of {description}  
    Total:   {format(executionSummary.Total)} Limit: {format(maxTotal)}");
            }
        }
    }
}
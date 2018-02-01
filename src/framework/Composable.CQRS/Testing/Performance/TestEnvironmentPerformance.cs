﻿using System;
using Composable.System;
using Composable.System.Diagnostics;

namespace Composable.Testing.Performance
{
    static class TestEnvironment
    {
        public static TimeSpan NCrunchSlowdownFactor(this TimeSpan original, double nCrunchSlowdownFactor)
        {
            if(IsInstrumented)
            {
                return ((int)(original.TotalMilliseconds * nCrunchSlowdownFactor)).Milliseconds();
            } else
            {
                return original;
            }
        }

        static readonly bool IsInstrumented = CheckIfInstrumented();
        static bool CheckIfInstrumented()
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if(!IsRunningUnderNCrunch) return false;

            var time = StopwatchExtensions.TimeExecution(() =>
                                                         {
                                                             for(int i = 0; i < 100; i++)
                                                             {
                                                                 // ReSharper disable once UnusedVariable
                                                                 int something = i;
                                                             }
                                                         },
                                                         iterations: 500);

            return time.Total.TotalMilliseconds > 1;
        }

        static bool IsRunningUnderNCrunch
        {
            get
            {
#if NCRUNCH
    return true;
#else
    return false;
#endif
            }
        }
    }
}

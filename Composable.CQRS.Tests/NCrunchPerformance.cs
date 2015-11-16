﻿using System;
using Composable.System;

namespace CQRS.Tests
{
    public static class NCrunchPerformance
    {
        private static bool IsRunningInNcrunch => NCrunch.Framework.NCrunchEnvironment.NCrunchIsResident();
        private static double NCRunchSlowDownFactor = 5.0;

        public static TimeSpan AdjustRuntime(TimeSpan original, double boost = 1.0)
        {
            if (IsRunningInNcrunch)
            {
                return ((int)(original.TotalMilliseconds * (NCRunchSlowDownFactor + boost))).Milliseconds();
            }
            return original;
        }

        public static int AdjustIterations(int original, double boost = 1.0)
        {
            if (IsRunningInNcrunch)
            {
                return (int)(original / (NCRunchSlowDownFactor + boost));
            }
            return original;
        }
    }

    public static class NCrunchPerformanceExtensions
    {
        
        public static TimeSpan AdjustRuntimeForNCrunch(this TimeSpan @this) => NCrunchPerformance.AdjustRuntime(@this);        
        public static int AdjustIterationsForNCrunch(this int @this) => NCrunchPerformance.AdjustIterations(@this);
    }
}
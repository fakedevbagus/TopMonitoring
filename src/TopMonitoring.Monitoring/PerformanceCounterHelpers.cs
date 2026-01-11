using System;
using System.Diagnostics;

namespace TopMonitoring.Monitoring
{
    internal static class PerformanceCounterHelpers
    {
        public static double? SafeNextValue(PerformanceCounter counter)
        {
            try { return counter.NextValue(); }
            catch { return null; }
        }
    }
}

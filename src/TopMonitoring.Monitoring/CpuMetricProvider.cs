using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TopMonitoring.Core;

namespace TopMonitoring.Monitoring
{
    /// <summary>
    /// Placeholder CPU provider. This implementation demonstrates structure only.
    /// For a production-ready provider, integrate with LibreHardwareMonitor or Windows Performance Counters.
    /// </summary>
    public class CpuMetricProvider : IMetricProvider
    {
        public string Id => "cpu-total";
        public MetricCategory Category => MetricCategory.CPU;
        public TimeSpan PollInterval { get; init; } = TimeSpan.FromMilliseconds(500);

        // Example: lightweight CPU usage via Process.TotalProcessorTime is NOT accurate for system-wide usage.
        // Replace with proper integration (LibreHardwareMonitor, PerformanceCounter, etc.)
        public async ValueTask<MetricSnapshot?> PollAsync(CancellationToken ct)
        {
            await Task.Yield();
            try
            {
                // Return null to indicate unavailable in this placeholder
                // Real implementation should return measured percent value
                return new MetricSnapshot(Id, Category, DateTime.UtcNow, null, "placeholder - implement LibreHardwareMonitor");
            }
            catch (Exception ex)
            {
                return new MetricSnapshot(Id, Category, DateTime.UtcNow, null, $"ERROR: {ex.Message}");
            }
        }

        public ValueTask DisposeAsync()
        {
            // dispose native resources if any
            return ValueTask.CompletedTask;
        }
    }
}

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TopMonitoring.Core;

namespace TopMonitoring.Monitoring
{
    public sealed class RamFreeMetricProvider : IMetricProvider
    {
        public string Id => "ram-free";
        public MetricCategory Category => MetricCategory.Memory;
        public TimeSpan PollInterval { get; init; } = TimeSpan.FromMilliseconds(1000);
        private readonly PerformanceCounter _memAvailableMb = new("Memory", "Available MBytes");

        public ValueTask<MetricSnapshot?> PollAsync(CancellationToken ct)
        {
            var avail = PerformanceCounterHelpers.SafeNextValue(_memAvailableMb);
            // Value in GB
            double? gb = avail.HasValue ? avail.Value / 1024d : null;
            return ValueTask.FromResult<MetricSnapshot?>(new MetricSnapshot(Id, Category, DateTime.UtcNow, gb, avail.HasValue ? $"{avail:F0}MB" : null));
        }

        public ValueTask DisposeAsync()
        {
            _memAvailableMb.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}

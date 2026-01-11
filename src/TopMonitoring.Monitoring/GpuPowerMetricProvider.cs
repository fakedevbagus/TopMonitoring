using System;
using System.Threading;
using System.Threading.Tasks;
using TopMonitoring.Core;

namespace TopMonitoring.Monitoring
{
    public sealed class GpuPowerMetricProvider : IMetricProvider
    {
        public string Id => "gpu-power";
        public MetricCategory Category => MetricCategory.GPU;
        public TimeSpan PollInterval { get; init; } = TimeSpan.FromMilliseconds(1000);
        public ValueTask<MetricSnapshot?> PollAsync(CancellationToken ct)
            => ValueTask.FromResult<MetricSnapshot?>(new MetricSnapshot(Id, Category, DateTime.UtcNow, null, "N/A"));
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

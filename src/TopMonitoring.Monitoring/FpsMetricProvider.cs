using System;
using System.Threading;
using System.Threading.Tasks;
using TopMonitoring.Core;

namespace TopMonitoring.Monitoring
{
    public sealed class FpsMetricProvider : IMetricProvider
    {
        public string Id => "fps";
        public MetricCategory Category => MetricCategory.FPS;
        public TimeSpan PollInterval { get; init; } = TimeSpan.FromMilliseconds(250);

        public ValueTask<MetricSnapshot?> PollAsync(CancellationToken ct)
            => ValueTask.FromResult<MetricSnapshot?>(new MetricSnapshot(Id, Category, DateTime.UtcNow, null, "--"));

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

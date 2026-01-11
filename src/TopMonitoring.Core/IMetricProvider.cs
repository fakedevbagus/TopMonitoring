using System;
using System.Threading;
using System.Threading.Tasks;

namespace TopMonitoring.Core
{
    public enum MetricCategory { CPU, GPU, Memory, Disk, Network, FPS, Other }

    public record MetricSnapshot(string Id, MetricCategory Category, DateTime Timestamp, double? Value, string? Raw = null);

    public interface IMetricProvider : IAsyncDisposable
    {
        string Id { get; }
        MetricCategory Category { get; }
        TimeSpan PollInterval { get; }

        /// <summary>
        /// Poll current metric value. Return null if unavailable.
        /// </summary>
        ValueTask<MetricSnapshot?> PollAsync(CancellationToken ct);
    }
}

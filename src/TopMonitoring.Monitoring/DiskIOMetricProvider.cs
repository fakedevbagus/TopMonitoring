using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using TopMonitoring.Core;

namespace TopMonitoring.Monitoring
{
    /// <summary>
    /// Disk read/write (KB/s) using PhysicalDisk counters (_Total).
    /// Raw: ReadKBps=.. WriteKBps=..
    /// </summary>
    public sealed class DiskIOMetricProvider : IMetricProvider
    {
        public string Id => "disk-io";
        public MetricCategory Category => MetricCategory.Disk;
        public TimeSpan PollInterval { get; init; } = TimeSpan.FromMilliseconds(1000);

        private readonly PerformanceCounter? _read;
        private readonly PerformanceCounter? _write;

        public DiskIOMetricProvider()
        {
            try
            {
                _read = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total");
                _write = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total");
                _ = _read.NextValue();
                _ = _write.NextValue();
            }
            catch { }
        }

        public ValueTask<MetricSnapshot?> PollAsync(CancellationToken ct)
        {
            if (_read == null || _write == null)
                return ValueTask.FromResult<MetricSnapshot?>(new MetricSnapshot(Id, Category, DateTime.UtcNow, null, "Unavailable"));

            var r = PerformanceCounterHelpers.SafeNextValue(_read);
            var w = PerformanceCounterHelpers.SafeNextValue(_write);
            var raw = $"ReadKBps={(r ?? 0)/1024:0} WriteKBps={(w ?? 0)/1024:0}";
            return ValueTask.FromResult<MetricSnapshot?>(new MetricSnapshot(Id, Category, DateTime.UtcNow, null, raw));
        }

        public ValueTask DisposeAsync()
        {
            _read?.Dispose();
            _write?.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}

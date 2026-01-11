using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TopMonitoring.Core;

namespace TopMonitoring.Monitoring
{
    /// <summary>
    /// Network throughput via PerformanceCounter. Chooses first non-loopback interface.
    /// Raw contains "DownKbps=.. UpKbps=..".
    /// </summary>
    public sealed class NetworkMetricProvider : IMetricProvider
    {
        public string Id => "net-throughput";
        public MetricCategory Category => MetricCategory.Network;
        public TimeSpan PollInterval { get; init; } = TimeSpan.FromMilliseconds(1000);

        private PerformanceCounter? _recv;
        private PerformanceCounter? _sent;

        public NetworkMetricProvider()
        {
            try
            {
                var category = new PerformanceCounterCategory("Network Interface");
                var instance = category.GetInstanceNames()
                    .FirstOrDefault(n => !n.ToLowerInvariant().Contains("loopback") && !n.ToLowerInvariant().Contains("pseudo"));
                if (instance != null)
                {
                    _recv = new PerformanceCounter("Network Interface", "Bytes Received/sec", instance);
                    _sent = new PerformanceCounter("Network Interface", "Bytes Sent/sec", instance);
                    // warm up
                    _ = _recv.NextValue();
                    _ = _sent.NextValue();
                }
            }
            catch { }
        }

        public ValueTask<MetricSnapshot?> PollAsync(CancellationToken ct)
        {
            if (_recv == null || _sent == null)
                return ValueTask.FromResult<MetricSnapshot?>(new MetricSnapshot(Id, Category, DateTime.UtcNow, null, "Unavailable"));

            var downBps = PerformanceCounterHelpers.SafeNextValue(_recv);
            var upBps = PerformanceCounterHelpers.SafeNextValue(_sent);

            double? value = null; // not a single number; UI can parse raw
            var raw = $"DownKbps={(downBps ?? 0)/1024:0} UpKbps={(upBps ?? 0)/1024:0}";
            return ValueTask.FromResult<MetricSnapshot?>(new MetricSnapshot(Id, Category, DateTime.UtcNow, value, raw));
        }

        public ValueTask DisposeAsync()
        {
            _recv?.Dispose();
            _sent?.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}

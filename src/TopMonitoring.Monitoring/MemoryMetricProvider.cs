using System;
using System.Diagnostics;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using TopMonitoring.Core;

namespace TopMonitoring.Monitoring
{
    public sealed class MemoryMetricProvider : IMetricProvider
    {
        public string Id => "mem-used-percent";
        public MetricCategory Category => MetricCategory.Memory;
        public TimeSpan PollInterval { get; init; } = TimeSpan.FromMilliseconds(1000);

        private readonly PerformanceCounter _memAvailableMb = new("Memory", "Available MBytes");
        private readonly Lazy<double> _totalMb = new(GetTotalPhysicalMemoryMb);

        public ValueTask<MetricSnapshot?> PollAsync(CancellationToken ct)
        {
            var availMb = PerformanceCounterHelpers.SafeNextValue(_memAvailableMb);
            var totalMb = _totalMb.Value;
            double? usedPercent = null;

            if (availMb.HasValue && totalMb > 0)
            {
                var usedMb = Math.Max(0, totalMb - availMb.Value);
                usedPercent = (usedMb / totalMb) * 100.0;
            }

            var raw = availMb.HasValue ? $"AvailableMB={availMb:F0} TotalMB={totalMb:F0}" : $"TotalMB={totalMb:F0}";
            return ValueTask.FromResult<MetricSnapshot?>(new MetricSnapshot(Id, Category, DateTime.UtcNow, usedPercent, raw));
        }

        private static double GetTotalPhysicalMemoryMb()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
                foreach (var obj in searcher.Get())
                {
                    var bytesObj = obj["TotalPhysicalMemory"];
                    if (bytesObj != null && double.TryParse(bytesObj.ToString(), out var bytes))
                        return bytes / 1024d / 1024d;
                }
            }
            catch
            {
                // fallback: unknown
            }
            return 0;
        }

        public ValueTask DisposeAsync()
        {
            _memAvailableMb.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}

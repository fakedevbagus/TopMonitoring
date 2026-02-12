using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TopMonitoring.Core;

namespace TopMonitoring.Monitoring
{
    public sealed class InternetMetricProvider : IMetricProvider
    {
        public string Id => "internet";
        public MetricCategory Category => MetricCategory.Network;
        public TimeSpan PollInterval { get; init; } = TimeSpan.FromMilliseconds(2000);

        private string? _instance;
        private PerformanceCounter? _down;
        private PerformanceCounter? _up;
        private int _reselectTick;

        public ValueTask<MetricSnapshot?> PollAsync(CancellationToken ct)
        {
            try
            {
                if (_instance == null || _reselectTick++ % 10 == 0)
                    SelectBestInterface();

                double? downB = _down != null ? PerformanceCounterHelpers.SafeNextValue(_down) : null;
                double? upB = _up != null ? PerformanceCounterHelpers.SafeNextValue(_up) : null;

                if (_instance != null && downB.GetValueOrDefault() == 0 && upB.GetValueOrDefault() == 0)
                {
                    SelectBestInterface();
                    downB = _down != null ? PerformanceCounterHelpers.SafeNextValue(_down) : null;
                    upB = _up != null ? PerformanceCounterHelpers.SafeNextValue(_up) : null;
                }

                var raw = $"↓{Format(downB)} ↑{Format(upB)}";
                return ValueTask.FromResult<MetricSnapshot?>(new MetricSnapshot(Id, Category, DateTime.UtcNow, null, raw));
            }
            catch
            {
                return ValueTask.FromResult<MetricSnapshot?>(new MetricSnapshot(Id, Category, DateTime.UtcNow, null, "↓-- ↑--"));
            }
        }

        private void SelectBestInterface()
        {
            try
            {
                var category = new PerformanceCounterCategory("Network Interface");
                var names = category.GetInstanceNames()
                    .Where(n => !n.Contains("Loopback", StringComparison.OrdinalIgnoreCase))
                    .ToArray();

                string? best = null;
                float bestScore = -1;

                foreach (var name in names)
                {
                    try
                    {
                        using var rx = new PerformanceCounter("Network Interface", "Bytes Received/sec", name);
                        using var tx = new PerformanceCounter("Network Interface", "Bytes Sent/sec", name);

                        _ = rx.NextValue();
                        _ = tx.NextValue();
                        Thread.Sleep(60);
                        var score = rx.NextValue() + tx.NextValue();

                        if (score > bestScore)
                        {
                            bestScore = score;
                            best = name;
                        }
                    }
                    catch { }
                }

                if (best == null) return;
                if (best == _instance) return;

                _down?.Dispose();
                _up?.Dispose();

                _instance = best;
                _down = new PerformanceCounter("Network Interface", "Bytes Received/sec", _instance);
                _up = new PerformanceCounter("Network Interface", "Bytes Sent/sec", _instance);

                _ = _down.NextValue();
                _ = _up.NextValue();
            }
            catch { }
        }

        private static string Format(double? bytesPerSec)
        {
            if (!bytesPerSec.HasValue) return "--";
            var kb = bytesPerSec.Value / 1024d;
            if (kb >= 1000) return $"{kb / 1024d:F1}MB/s";
            return $"{kb:F0}KB/s";
        }

        public ValueTask DisposeAsync()
        {
            _down?.Dispose();
            _up?.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}

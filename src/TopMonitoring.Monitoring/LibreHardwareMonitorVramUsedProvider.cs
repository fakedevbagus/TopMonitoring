using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibreHardwareMonitor.Hardware;
using TopMonitoring.Core;

namespace TopMonitoring.Monitoring
{
    public sealed class LibreHardwareMonitorVramUsedProvider : IMetricProvider
    {
        public string Id => "vram-used";
        public MetricCategory Category => MetricCategory.GPU;
        public TimeSpan PollInterval { get; init; } = TimeSpan.FromMilliseconds(1000);

        private readonly Computer _computer;
        private IHardware? _gpu;

        public LibreHardwareMonitorVramUsedProvider()
        {
            _computer = new Computer { IsGpuEnabled = true };
            _computer.Open();
            _gpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.GpuNvidia
                                                      || h.HardwareType == HardwareType.GpuAmd
                                                      || h.HardwareType == HardwareType.GpuIntel);
        }

        public ValueTask<MetricSnapshot?> PollAsync(CancellationToken ct)
        {
            try
            {
                _gpu?.Update();

                ISensor? sensor =
                    _gpu?.Sensors.FirstOrDefault(s => (s.SensorType == SensorType.SmallData || s.SensorType == SensorType.Data)
                                                     && s.Name.Contains("Memory Used", StringComparison.OrdinalIgnoreCase))
                    ?? _gpu?.Sensors.FirstOrDefault(s => (s.SensorType == SensorType.SmallData || s.SensorType == SensorType.Data)
                                                     && s.Name.Contains("VRAM", StringComparison.OrdinalIgnoreCase));

                var val = sensor?.Value;
                string raw = val.HasValue ? $"{val.Value:F0}MB" : "N/A";
                // Some systems report in GB already; if < 64 assume GB
                if (val.HasValue && val.Value < 64) raw = $"{val.Value:F1}GB";

                return ValueTask.FromResult<MetricSnapshot?>(new MetricSnapshot(Id, Category, DateTime.UtcNow, null, raw));
            }
            catch
            {
                return ValueTask.FromResult<MetricSnapshot?>(new MetricSnapshot(Id, Category, DateTime.UtcNow, null, "N/A"));
            }
        }

        public ValueTask DisposeAsync()
        {
            _computer.Close();
            return ValueTask.CompletedTask;
        }
    }
}

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibreHardwareMonitor.Hardware;
using TopMonitoring.Core;

namespace TopMonitoring.Monitoring
{
    public sealed class LibreHardwareMonitorGpuPowerProvider : IMetricProvider
    {
        public string Id => "gpu-power";
        public MetricCategory Category => MetricCategory.GPU;
        public TimeSpan PollInterval { get; init; } = TimeSpan.FromMilliseconds(1000);

        private readonly Computer _computer;
        private IHardware? _gpu;

        public LibreHardwareMonitorGpuPowerProvider()
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
                var pwr = _gpu?.Sensors
                    .Where(s => s.SensorType == SensorType.Power)
                    .FirstOrDefault();

                var val = pwr?.Value;
                var raw = val.HasValue ? $"{val.Value:F0}W" : "N/A";
                return ValueTask.FromResult<MetricSnapshot?>(new MetricSnapshot(Id, Category, DateTime.UtcNow, val, raw));
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

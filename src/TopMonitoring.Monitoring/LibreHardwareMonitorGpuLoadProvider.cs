using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibreHardwareMonitor.Hardware;
using TopMonitoring.Core;

namespace TopMonitoring.Monitoring
{
    public sealed class LibreHardwareMonitorGpuLoadProvider : IMetricProvider
    {
        public string Id => "gpu-load";
        public MetricCategory Category => MetricCategory.GPU;
        public TimeSpan PollInterval { get; init; } = TimeSpan.FromMilliseconds(1000);

        private readonly Computer _computer;
        private IHardware? _gpu;

        public LibreHardwareMonitorGpuLoadProvider()
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
                var load = _gpu?.Sensors
                    .Where(s => s.SensorType == SensorType.Load)
                    .FirstOrDefault(s => s.Name.Contains("Core", StringComparison.OrdinalIgnoreCase))
                    ?? _gpu?.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load);

                var val = load?.Value;
                var raw = _gpu != null ? _gpu.Name : "NoGPU";
                return ValueTask.FromResult<MetricSnapshot?>(new MetricSnapshot(Id, Category, DateTime.UtcNow, val, raw));
            }
            catch (Exception ex)
            {
                return ValueTask.FromResult<MetricSnapshot?>(new MetricSnapshot(Id, Category, DateTime.UtcNow, null, $"ERROR: {ex.Message}"));
            }
        }

        public ValueTask DisposeAsync()
        {
            _computer.Close();
            return ValueTask.CompletedTask;
        }
    }
}

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibreHardwareMonitor.Hardware;
using TopMonitoring.Core;

namespace TopMonitoring.Monitoring
{
    public sealed class LibreHardwareMonitorGpuTempProvider : IMetricProvider
    {
        public string Id => "gpu-temp";
        public MetricCategory Category => MetricCategory.GPU;
        public TimeSpan PollInterval { get; init; } = TimeSpan.FromMilliseconds(1000);

        private readonly Computer _computer;
        private IHardware? _gpu;

        public LibreHardwareMonitorGpuTempProvider()
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
                var temp = _gpu?.Sensors
                    .Where(s => s.SensorType == SensorType.Temperature)
                    .OrderByDescending(s => s.Name.Contains("Hot Spot", StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault();

                var val = temp?.Value;
                return ValueTask.FromResult<MetricSnapshot?>(new MetricSnapshot(Id, Category, DateTime.UtcNow, val, temp?.Name));
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

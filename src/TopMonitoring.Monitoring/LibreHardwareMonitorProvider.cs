using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibreHardwareMonitor.Hardware;
using TopMonitoring.Core;

namespace TopMonitoring.Monitoring
{
    /// <summary>
    /// Minimal LibreHardwareMonitor based provider example (CPU total load).
    /// Extend this to GPU, temps, fans, etc.
    /// </summary>
    public sealed class LibreHardwareMonitorCpuLoadProvider : IMetricProvider
    {
        public string Id => "cpu-total-load";
        public MetricCategory Category => MetricCategory.CPU;
        public TimeSpan PollInterval { get; init; } = TimeSpan.FromMilliseconds(1000);

        private readonly Computer _computer;
        private IHardware? _cpu;

        public LibreHardwareMonitorCpuLoadProvider()
        {
            _computer = new Computer { IsCpuEnabled = true };
            _computer.Open();
            _cpu = _computer.Hardware.FirstOrDefault(h => h.HardwareType == HardwareType.Cpu);
        }

        public ValueTask<MetricSnapshot?> PollAsync(CancellationToken ct)
        {
            try
            {
                _cpu?.Update();
                var loadSensor = _cpu?.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load && s.Name.Contains("Total"));
                var val = loadSensor?.Value;
                return ValueTask.FromResult<MetricSnapshot?>(new MetricSnapshot(Id, Category, DateTime.UtcNow, val, null));
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

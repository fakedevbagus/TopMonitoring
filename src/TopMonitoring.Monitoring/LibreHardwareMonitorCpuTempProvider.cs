using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibreHardwareMonitor.Hardware;
using TopMonitoring.Core;

namespace TopMonitoring.Monitoring
{
    public sealed class LibreHardwareMonitorCpuTempProvider : IMetricProvider
    {
        public string Id => "cpu-temp";
        public MetricCategory Category => MetricCategory.CPU;
        public TimeSpan PollInterval { get; init; } = TimeSpan.FromMilliseconds(1000);

        private readonly Computer _computer;
        private IHardware? _cpu;

        public LibreHardwareMonitorCpuTempProvider()
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
                var temp = _cpu?.Sensors
                    .Where(s => s.SensorType == SensorType.Temperature)
                    .OrderByDescending(s => s.Name.Contains("Package", StringComparison.OrdinalIgnoreCase))
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

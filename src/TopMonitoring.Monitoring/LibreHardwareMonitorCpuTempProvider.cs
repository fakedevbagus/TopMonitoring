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
        public TimeSpan PollInterval { get; init; } = TimeSpan.FromMilliseconds(2000);

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
                // CPU Package temperature is used for consistency across platforms.
                var temp = _cpu?.Sensors
                    .FirstOrDefault(s => s.SensorType == SensorType.Temperature
                                         && s.Name.Contains("Package", StringComparison.OrdinalIgnoreCase));

                temp ??= _cpu?.Sensors
                    .FirstOrDefault(s => s.SensorType == SensorType.Temperature);

                var val = temp?.Value;
                if (!val.HasValue)
                    return ValueTask.FromResult<MetricSnapshot?>(new MetricSnapshot(Id, Category, DateTime.UtcNow, null, null));

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

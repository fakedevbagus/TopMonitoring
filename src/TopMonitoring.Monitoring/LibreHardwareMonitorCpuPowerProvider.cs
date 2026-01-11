using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibreHardwareMonitor.Hardware;
using TopMonitoring.Core;

namespace TopMonitoring.Monitoring
{
    public sealed class LibreHardwareMonitorCpuPowerProvider : IMetricProvider
    {
        public string Id => "cpu-power";
        public MetricCategory Category => MetricCategory.CPU;
        public TimeSpan PollInterval { get; init; } = TimeSpan.FromMilliseconds(1000);

        private readonly Computer _computer;
        private IHardware? _cpu;

        public LibreHardwareMonitorCpuPowerProvider()
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
                var pwr = _cpu?.Sensors
                    .Where(s => s.SensorType == SensorType.Power)
                    .OrderByDescending(s => s.Name.Contains("Package", StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault();

                var val = pwr?.Value;
                var raw = val.HasValue ? $"{val.Value:F0}W" : "N/A";
                return ValueTask.FromResult<MetricSnapshot?>(new MetricSnapshot(Id, Category, DateTime.UtcNow, val, raw));
            }
            catch (Exception ex)
            {
                return ValueTask.FromResult<MetricSnapshot?>(new MetricSnapshot(Id, Category, DateTime.UtcNow, null, $"N/A"));
            }
        }

        public ValueTask DisposeAsync()
        {
            _computer.Close();
            return ValueTask.CompletedTask;
        }
    }
}

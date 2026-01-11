using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibreHardwareMonitor.Hardware;
using TopMonitoring.Core;

namespace TopMonitoring.Monitoring
{
    public sealed class LibreHardwareMonitorFanRpmProvider : IMetricProvider
    {
        public string Id => "fan-rpm";
        public MetricCategory Category => MetricCategory.Other;
        public TimeSpan PollInterval { get; init; } = TimeSpan.FromMilliseconds(1500);

        private readonly Computer _computer;

        public LibreHardwareMonitorFanRpmProvider()
        {
            _computer = new Computer { IsMotherboardEnabled = true, IsControllerEnabled = true, IsCpuEnabled = true };
            _computer.Open();
        }

        public ValueTask<MetricSnapshot?> PollAsync(CancellationToken ct)
        {
            try
            {
                foreach (var hw in _computer.Hardware) hw.Update();
                var fan = _computer.Hardware
                    .SelectMany(h => h.Sensors)
                    .FirstOrDefault(s => s.SensorType == SensorType.Fan);

                var val = fan?.Value;
                return ValueTask.FromResult<MetricSnapshot?>(new MetricSnapshot(Id, Category, DateTime.UtcNow, val, fan?.Name));
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

using System.Collections.Generic;
using System.Threading.Tasks;
using TopMonitoring.Core;
using TopMonitoring.Monitoring;

namespace TopMonitoring.App
{
    public sealed class MonitoringService : IAsyncDisposable
    {
        private readonly MetricEngine _engine;

        public MonitoringService()
        {
            var providers = new List<IMetricProvider>
            {
                new FpsMetricProvider(),
                new LibreHardwareMonitorCpuLoadProvider(),
                new LibreHardwareMonitorCpuTempProvider(),
                new LibreHardwareMonitorCpuPowerProvider(),
                new LibreHardwareMonitorGpuLoadProvider(),
                new LibreHardwareMonitorGpuTempProvider(),
                new LibreHardwareMonitorGpuPowerProvider(),
                new LibreHardwareMonitorVramUsedProvider(),
                new MemoryMetricProvider(), // ram used %
                new RamFreeMetricProvider(),
                new DriveFreeMetricProvider("C:"),
                new DriveFreeMetricProvider("D:"),
                new DriveFreeMetricProvider("E:"),
                new DriveFreeMetricProvider("F:"),
                new DriveFreeMetricProvider("G:"),
                new InternetMetricProvider()
            };

            _engine = new MetricEngine(providers);
        }

        public void Start() => _engine.Start();

        public IAsyncEnumerable<MetricSnapshot> GetSnapshotsAsync() => _engine.GetSnapshotsAsync();

        public ValueTask DisposeAsync() => _engine.DisposeAsync();
    }
}

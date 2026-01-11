using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TopMonitoring.Core;

namespace TopMonitoring.Monitoring
{
    public sealed class DriveFreeMetricProvider : IMetricProvider
    {
        private readonly string _drive;
        private readonly string _id;
        public DriveFreeMetricProvider(string driveLetter)
        {
            _drive = driveLetter.EndsWith(":") ? driveLetter + "\\" : driveLetter;
            _id = driveLetter.ToLowerInvariant().Contains("c") ? "drive-c" : "drive-e";
        }

        public string Id => _id;
        public MetricCategory Category => MetricCategory.Disk;
        public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(5);

        public ValueTask<MetricSnapshot?> PollAsync(CancellationToken ct)
        {
            try
            {
                var drive = DriveInfo.GetDrives().FirstOrDefault(d => d.IsReady && d.Name.StartsWith(_drive, StringComparison.OrdinalIgnoreCase));
                if (drive == null) return ValueTask.FromResult<MetricSnapshot?>(new MetricSnapshot(Id, Category, DateTime.UtcNow, null, "N/A"));
                var freeGb = drive.AvailableFreeSpace / 1024d / 1024d / 1024d;
                return ValueTask.FromResult<MetricSnapshot?>(new MetricSnapshot(Id, Category, DateTime.UtcNow, freeGb, drive.Name));
            }
            catch
            {
                return ValueTask.FromResult<MetricSnapshot?>(new MetricSnapshot(Id, Category, DateTime.UtcNow, null, "N/A"));
            }
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

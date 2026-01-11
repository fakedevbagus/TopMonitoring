using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TopMonitoring.Core;

namespace TopMonitoring.Monitoring
{
    public sealed class DiskFreeMetricProvider : IMetricProvider
    {
        public string Id => "disk-system-free-gb";
        public MetricCategory Category => MetricCategory.Disk;
        public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(5);

        private readonly string _driveLetter;

        public DiskFreeMetricProvider(string? driveLetter = null)
        {
            _driveLetter = string.IsNullOrWhiteSpace(driveLetter)
                ? Path.GetPathRoot(Environment.SystemDirectory) ?? "C:\\"
                : driveLetter!;
        }

        public ValueTask<MetricSnapshot?> PollAsync(CancellationToken ct)
        {
            try
            {
                var drive = DriveInfo.GetDrives()
                    .FirstOrDefault(d => d.IsReady && d.RootDirectory.FullName.Equals(_driveLetter, StringComparison.OrdinalIgnoreCase))
                    ?? new DriveInfo(_driveLetter);

                if (!drive.IsReady)
                    return ValueTask.FromResult<MetricSnapshot?>(new MetricSnapshot(Id, Category, DateTime.UtcNow, null, "DriveNotReady"));

                var freeGb = drive.AvailableFreeSpace / 1024d / 1024d / 1024d;
                return ValueTask.FromResult<MetricSnapshot?>(new MetricSnapshot(Id, Category, DateTime.UtcNow, freeGb, drive.Name));
            }
            catch (Exception ex)
            {
                return ValueTask.FromResult<MetricSnapshot?>(new MetricSnapshot(Id, Category, DateTime.UtcNow, null, $"ERROR: {ex.Message}"));
            }
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}

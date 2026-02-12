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
            var trimmed = driveLetter?.Trim() ?? "C";
            var letter = trimmed.TrimEnd(':', '\\').FirstOrDefault();
            var idLetter = char.IsLetter(letter) ? char.ToLowerInvariant(letter) : 'c';
            _drive = $"{char.ToUpperInvariant(idLetter)}:\\";
            _id = $"drive-{idLetter}";
        }

        public string Id => _id;
        public MetricCategory Category => MetricCategory.Disk;
        public TimeSpan PollInterval { get; init; } = TimeSpan.FromSeconds(3);

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

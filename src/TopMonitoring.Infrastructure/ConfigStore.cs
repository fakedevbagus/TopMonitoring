using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace TopMonitoring.Infrastructure
{
    public static class ConfigStore
    {
        private static readonly string Path = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TopMonitoring", "config.json");

        private static readonly JsonSerializerOptions Opts = new() { WriteIndented = true };

        public static async Task<AppSettings> LoadAsync()
        {
            try
            {
                if (!File.Exists(Path)) return new AppSettings();
                using var s = File.OpenRead(Path);
                return await JsonSerializer.DeserializeAsync<AppSettings>(s, Opts) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        public static async Task SaveAsync(AppSettings settings)
        {
            try
            {
                var dir = System.IO.Path.GetDirectoryName(Path);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);
                using var s = File.Create(Path);
                await JsonSerializer.SerializeAsync(s, settings, Opts);
            }
            catch { /* swallow errors for safety; in production log them */ }
        }
    }
}

using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TopMonitoring.Infrastructure
{
    public static class ConfigStore
    {
        private static readonly string Path = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TopMonitoring", "config.json");

        private static readonly JsonSerializerOptions Opts = new() { WriteIndented = true };
        private static readonly SemaphoreSlim IoLock = new(1, 1);

        public static async Task<AppSettings> LoadAsync()
        {
            var settings = await LoadFromFileAsync(Path);
            if (settings != null) return settings;

            // If load fails (corrupted/missing), return defaults and recreate file.
            var defaults = new AppSettings();
            await SaveToFileAsync(Path, defaults);
            return defaults;
        }

        public static async Task SaveAsync(AppSettings settings)
        {
            await SaveToFileAsync(Path, settings);
        }

        public static async Task<AppSettings?> LoadFromFileAsync(string filePath)
        {
            try
            {
                await IoLock.WaitAsync();
                if (!File.Exists(filePath)) return null;
                using var s = File.OpenRead(filePath);
                return await JsonSerializer.DeserializeAsync<AppSettings>(s, Opts);
            }
            catch
            {
                return null;
            }
            finally
            {
                IoLock.Release();
            }
        }

        public static async Task SaveToFileAsync(string filePath, AppSettings settings)
        {
            try
            {
                await IoLock.WaitAsync();
                var dir = System.IO.Path.GetDirectoryName(filePath);
                if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir!);
                using var s = File.Create(filePath);
                await JsonSerializer.SerializeAsync(s, settings, Opts);
            }
            catch { /* swallow errors for safety; in production log them */ }
            finally
            {
                IoLock.Release();
            }
        }
    }
}

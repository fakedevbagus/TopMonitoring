using System;
using System.Threading;
using System.Threading.Tasks;

namespace TopMonitoring.Infrastructure
{
    public sealed class SettingsService : IDisposable
    {
        private readonly TimeSpan _saveDebounce = TimeSpan.FromMilliseconds(400);
        private CancellationTokenSource? _saveDebounceCts;

        public AppSettings Current { get; private set; } = AppSettings.Normalize(new AppSettings());

        public event Action<AppSettings>? SettingsChanged;

        public async Task LoadAsync()
        {
            var loaded = await ConfigStore.LoadAsync();
            Update(loaded, save: false);
        }

        public void Update(AppSettings settings, bool save = true)
        {
            Current = AppSettings.Normalize(settings);
            SettingsChanged?.Invoke(Current);
            if (save) DebouncedSave(Current);
        }

        public async Task<AppSettings?> ImportAsync(string filePath)
        {
            var loaded = await ConfigStore.LoadFromFileAsync(filePath);
            if (loaded == null) return null;
            Update(loaded, save: true);
            return Current;
        }

        public Task ExportAsync(string filePath)
        {
            return ConfigStore.SaveToFileAsync(filePath, Current);
        }

        private void DebouncedSave(AppSettings s)
        {
            _saveDebounceCts?.Cancel();
            _saveDebounceCts = new CancellationTokenSource();
            var token = _saveDebounceCts.Token;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(_saveDebounce, token);
                    await ConfigStore.SaveAsync(s);
                }
                catch { }
            }, token);
        }

        public void Dispose()
        {
            _saveDebounceCts?.Cancel();
            _saveDebounceCts?.Dispose();
            _saveDebounceCts = null;
        }
    }
}

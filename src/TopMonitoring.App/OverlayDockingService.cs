using System;
using Microsoft.Win32;
using TopMonitoring.Infrastructure;

namespace TopMonitoring.App
{
    public sealed class OverlayDockingService : IDisposable
    {
        private const int BaseHeight = 24;
        private readonly AppBar _appBar;
        private AppSettings _settings = new();

        public OverlayDockingService(System.Windows.Window window)
        {
            _appBar = new AppBar(window);
            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        }

        public void Apply(AppSettings settings)
        {
            _settings = settings;
            UpdateDocking();
        }

        private void OnDisplaySettingsChanged(object? sender, EventArgs e) => UpdateDocking();

        private void UpdateDocking()
        {
            var screen = MonitorService.ResolveTarget(_settings.TargetMonitor);
            var height = (int)Math.Round(BaseHeight * _settings.UIScale);
            if (height < 16) height = 16;
            if (height > 120) height = 120;

            // Ensure the appbar is registered and then snap to the selected monitor's top edge.
            _appBar.Register(screen, height);
            _appBar.SetPosition(screen, height);
        }

        public void Dispose()
        {
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            _appBar.Dispose();
        }
    }
}

using System;
using System.Windows;
using TopMonitoring.Infrastructure;

namespace TopMonitoring.App
{
    public sealed class OverlayInputService : IDisposable
    {
        private readonly GlobalHotkeyService _hotkeys;
        private readonly Window _window;

        public OverlayInputService(Window window)
        {
            _window = window;
            _hotkeys = new GlobalHotkeyService(window);
        }

        public void ApplySettings(
            AppSettings settings,
            Action toggleVisibility,
            Action toggleClickThrough,
            Action cyclePreset)
        {
            ClickThroughHelper.Apply(_window, settings.IsClickThroughEnabled);

            _hotkeys.Clear();
            _hotkeys.Register(settings.ToggleVisibilityHotkey?.Gesture, toggleVisibility);
            _hotkeys.Register(settings.ToggleClickThroughHotkey?.Gesture, toggleClickThrough);
            _hotkeys.Register(settings.CyclePresetHotkey?.Gesture, cyclePreset);
        }

        public void Dispose() => _hotkeys.Dispose();
    }
}

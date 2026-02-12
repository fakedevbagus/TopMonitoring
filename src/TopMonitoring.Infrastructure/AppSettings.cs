using System;
using System.Linq;

namespace TopMonitoring.Infrastructure
{
    public record AppSettings
    {
        public const string CustomPresetId = "custom";

        public int UiUpdateIntervalMs { get; init; } = 250;
        public double UiOpacity { get; init; } = 0.92;
        public double UIScale { get; init; } = 1.0;

        // Overlay behavior
        public bool IsClickThroughEnabled { get; init; } = false;
        public string TargetMonitor { get; init; } = "Primary";

        // Alerts
        public double CpuAlertThreshold { get; init; } = 90;
        public double RamAlertThreshold { get; init; } = 90;
        public double GpuAlertThreshold { get; init; } = 90;
        public bool AlertBlinkEnabled { get; init; } = false;

        // Presets
        public string ActivePresetId { get; init; } = CustomPresetId;
        public PresetProfile[] Presets { get; init; } = DefaultPresets();

        // Global hotkeys
        public HotkeyBinding ToggleVisibilityHotkey { get; init; } = new() { Gesture = "Ctrl+Alt+O" };
        public HotkeyBinding ToggleClickThroughHotkey { get; init; } = new() { Gesture = "Ctrl+Alt+T" };
        public HotkeyBinding CyclePresetHotkey { get; init; } = new() { Gesture = "Ctrl+Alt+P" };

        // Settings window theme: "Dark" or "Light"
        public string SettingsTheme { get; init; } = "Dark";

        // Visual
        public string BackgroundHex { get; init; } = "#DD111111";

        // Labels (prefixes)
        public string LabelFps { get; init; } = "FPS";
        public string LabelCpuLoad { get; init; } = "CPU";
        public string LabelCpuTemp { get; init; } = "CT";
        public string LabelCpuPower { get; init; } = "CPW";
        public string LabelGpuLoad { get; init; } = "GPU";
        public string LabelGpuTemp { get; init; } = "GT";
        public string LabelGpuPower { get; init; } = "GPW";
        public string LabelVramUsed { get; init; } = "VRAM";
        public string LabelRamUsed { get; init; } = "RAM";
        public string LabelRamFree { get; init; } = "FREE";
        public string LabelDriveC { get; init; } = "C";
        public string LabelDriveD { get; init; } = "D";
        public string LabelDriveE { get; init; } = "E";
        public string LabelDriveF { get; init; } = "F";
        public string LabelDriveG { get; init; } = "G";
        public string LabelInternet { get; init; } = "NET";

        // Order of metric IDs as displayed
        public string[] MetricOrder { get; init; } = new[]
        {
            "fps",
            "cpu-load","cpu-temp","cpu-power",
            "gpu-load","gpu-temp","gpu-power",
            "vram-used",
            "ram-used","ram-free",
            "drive-c","drive-d","drive-e","drive-f","drive-g",
            "internet"
        };
        // Metrics enable/disable (checked in Settings). Default = all enabled.
        public string[] EnabledMetrics { get; init; } = new[]
        {
            "fps",
            "cpu-load",
            "cpu-temp",
            "cpu-power",
            "gpu-load",
            "gpu-temp",
            "gpu-power",
            "vram-used",
            "ram-used",
            "ram-free",
            "drive-c",
            "drive-d",
            "drive-e",
            "drive-f",
            "drive-g",
            "internet"
        };

        public static PresetProfile[] DefaultPresets() => new[]
        {
            new PresetProfile
            {
                Id = "gaming",
                Name = "Gaming",
                SettingsTheme = "Dark",
                UiOpacity = 0.92,
                UIScale = 1.0,
                UiUpdateIntervalMs = 200,
                EnabledMetrics = new[] { "fps", "cpu-load", "cpu-temp", "gpu-load", "gpu-temp", "gpu-power", "vram-used", "internet" }
            },
            new PresetProfile
            {
                Id = "work",
                Name = "Work",
                SettingsTheme = "Dark",
                UiOpacity = 0.92,
                UIScale = 1.0,
                UiUpdateIntervalMs = 500,
                EnabledMetrics = new[] { "cpu-load", "ram-used", "ram-free", "drive-c", "internet" }
            },
            new PresetProfile
            {
                Id = "minimal",
                Name = "Minimal",
                SettingsTheme = "Dark",
                UiOpacity = 0.9,
                UIScale = 1.0,
                UiUpdateIntervalMs = 1000,
                EnabledMetrics = new[] { "cpu-load", "ram-used", "gpu-load" }
            }
        };

        public static AppSettings ApplyPreset(AppSettings current, string presetId)
        {
            if (presetId.Equals(CustomPresetId, StringComparison.OrdinalIgnoreCase))
                return current with { ActivePresetId = CustomPresetId };

            var preset = (current.Presets ?? DefaultPresets())
                .FirstOrDefault(p => p.Id.Equals(presetId, StringComparison.OrdinalIgnoreCase));
            if (preset == null) return current with { ActivePresetId = CustomPresetId };

            return current with
            {
                ActivePresetId = preset.Id,
                EnabledMetrics = preset.EnabledMetrics,
                SettingsTheme = preset.SettingsTheme,
                UiOpacity = preset.UiOpacity,
                UIScale = preset.UIScale,
                UiUpdateIntervalMs = preset.UiUpdateIntervalMs
            };
        }

        public static AppSettings Normalize(AppSettings s)
        {
            var defaults = new AppSettings();
            var metricOrder = (s.MetricOrder != null && s.MetricOrder.Length > 0) ? s.MetricOrder : defaults.MetricOrder;
            var orderList = metricOrder.ToList();
            var existing = orderList.ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var id in defaults.MetricOrder)
            {
                if (existing.Add(id)) orderList.Add(id);
            }
            var enabled = (s.EnabledMetrics != null && s.EnabledMetrics.Length > 0) ? s.EnabledMetrics : defaults.EnabledMetrics;
            var presets = (s.Presets != null && s.Presets.Length > 0) ? s.Presets : DefaultPresets();
            var activePreset = string.IsNullOrWhiteSpace(s.ActivePresetId) ? CustomPresetId : s.ActivePresetId;

            return s with
            {
                UiOpacity = Math.Clamp(s.UiOpacity, 0.4, 1.0),
                UIScale = Math.Clamp(s.UIScale, 0.75, 1.75),
                UiUpdateIntervalMs = Math.Clamp(s.UiUpdateIntervalMs, 200, 2000),
                CpuAlertThreshold = Math.Clamp(s.CpuAlertThreshold, 1, 100),
                RamAlertThreshold = Math.Clamp(s.RamAlertThreshold, 1, 100),
                GpuAlertThreshold = Math.Clamp(s.GpuAlertThreshold, 1, 100),
                TargetMonitor = string.IsNullOrWhiteSpace(s.TargetMonitor) ? "Primary" : s.TargetMonitor,
                MetricOrder = orderList.ToArray(),
                EnabledMetrics = enabled,
                Presets = presets,
                ActivePresetId = activePreset,
                ToggleVisibilityHotkey = s.ToggleVisibilityHotkey ?? defaults.ToggleVisibilityHotkey,
                ToggleClickThroughHotkey = s.ToggleClickThroughHotkey ?? defaults.ToggleClickThroughHotkey,
                CyclePresetHotkey = s.CyclePresetHotkey ?? defaults.CyclePresetHotkey
            };
        }

    }
}

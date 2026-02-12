namespace TopMonitoring.Infrastructure
{
    public record PresetProfile
    {
        public string Id { get; init; } = "custom";
        public string Name { get; init; } = "Custom";
        public string SettingsTheme { get; init; } = "Dark";
        public double UiOpacity { get; init; } = 0.92;
        public double UIScale { get; init; } = 1.0;
        public int UiUpdateIntervalMs { get; init; } = 250;
        public string[] EnabledMetrics { get; init; } = System.Array.Empty<string>();
    }

    public record HotkeyBinding
    {
        // Example format: "Ctrl+Alt+O"
        public string Gesture { get; init; } = string.Empty;
    }
}

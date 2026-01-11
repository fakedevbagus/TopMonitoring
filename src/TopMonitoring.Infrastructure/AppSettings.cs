namespace TopMonitoring.Infrastructure
{
    public record AppSettings
    {
        public int UiUpdateIntervalMs { get; init; } = 250;
        public double UiOpacity { get; init; } = 0.92;

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
        public string LabelDriveE { get; init; } = "E";
        public string LabelInternet { get; init; } = "NET";

        // Order of metric IDs as displayed
        public string[] MetricOrder { get; init; } = new[]
        {
            "fps",
            "cpu-load","cpu-temp","cpu-power",
            "gpu-load","gpu-temp","gpu-power",
            "vram-used",
            "ram-used","ram-free",
            "drive-c","drive-e",
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
            "drive-e",
            "internet"
        };

    }
}

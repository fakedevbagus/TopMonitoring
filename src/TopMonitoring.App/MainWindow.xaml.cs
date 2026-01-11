using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TopMonitoring.Core;
using TopMonitoring.Infrastructure;
using TopMonitoring.Monitoring;

namespace TopMonitoring.App
{
    public partial class MainWindow : Window
    {
        private MetricEngine? _engine;
        private AppBar? _appBar;
        private TrayService? _tray;
        private AppSettings _settings = new();
        private bool _isExitRequested;

        private readonly Dictionary<string, TextBlock> _blocks = new();

        // cached values
        private double? _fps, _cpuLoad, _cpuTemp, _gpuLoad, _gpuTemp, _ramUsed, _ramFreeGb, _driveCGb, _driveEGb;
        private string _cpuPower = "N/A", _gpuPower = "N/A", _vramUsed = "N/A", _internet = "--";

        private CancellationTokenSource? _saveDebounce;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
            Closed += MainWindow_Closed;
            RegisterBlocks();
            MouseRightButtonUp += (_, __) => ShowContextMenu();
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Prevent accidental exit; hide to tray unless user explicitly exits
            if (!_isExitRequested)
            {
                e.Cancel = true;
                Hide();
            }
        }

        private void RegisterBlocks()
        {
            _blocks["fps"] = FpsText;
            _blocks["cpu-load"] = CpuLoadText;
            _blocks["cpu-temp"] = CpuTempText;
            _blocks["cpu-power"] = CpuPowerText;
            _blocks["gpu-load"] = GpuLoadText;
            _blocks["gpu-temp"] = GpuTempText;
            _blocks["gpu-power"] = GpuPowerText;
            _blocks["vram-used"] = VramUsedText;
            _blocks["ram-used"] = RamUsedText;
            _blocks["ram-free"] = RamFreeText;
            _blocks["drive-c"] = DriveCText;
            _blocks["drive-e"] = DriveEText;
            _blocks["internet"] = InternetText;
        }

        
        private void ForceShow()
        {
            try
            {
                Show();
                WindowState = WindowState.Normal;
                Topmost = true;
                ShowActivated = true;
                Activate();
            }
            catch { }
        }

        private async void MainWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            _settings = await ConfigStore.LoadAsync();
            ApplySettings(_settings);
            // Ensure bar is visible even if AppBar quirks happen
            ForceShow();

            _appBar = new AppBar(this);
            _appBar.Register();

            _tray = new TrayService(
                showSettings: async () => await OpenSettingsAsync(),
                showBar: () => Dispatcher.Invoke(() => { Show(); Activate(); Topmost = true; }),
                hideBar: () => Dispatcher.Invoke(() => Hide()),
                exit: () => Dispatcher.Invoke(() =>
                {
                    _isExitRequested = true;
                    Close();
                    System.Windows.Application.Current.Shutdown();
                }),
                getAutoStart: AutoStart.IsEnabled,
                setAutoStart: AutoStart.SetEnabled
            );

            var providers = new List<IMetricProvider>
            {
                new FpsMetricProvider(),
                new LibreHardwareMonitorCpuLoadProvider(),
                new LibreHardwareMonitorCpuTempProvider(),
                new LibreHardwareMonitorCpuPowerProvider(),
                new LibreHardwareMonitorGpuLoadProvider(),
                new LibreHardwareMonitorGpuTempProvider(),
                new LibreHardwareMonitorGpuPowerProvider(),
                new LibreHardwareMonitorVramUsedProvider(),
                new MemoryMetricProvider(), // ram used %
                new RamFreeMetricProvider(),
                new DriveFreeMetricProvider("C:"),
                new DriveFreeMetricProvider("E:"),
                new InternetMetricProvider()
            };

            _engine = new MetricEngine(providers);
            _engine.Start();

            await Task.Run(async () =>
            {
                await foreach (var snap in _engine.GetSnapshotsAsync())
                {
                    Dispatcher.Invoke(() =>
                    {
                        switch (snap.Id)
                        {
                            case "fps": _fps = snap.Value; break;

                            case "cpu-total-load": _cpuLoad = snap.Value; break;
                            case "cpu-temp": _cpuTemp = snap.Value; break;
                            case "cpu-power": _cpuPower = snap.Raw ?? "N/A"; break;

                            case "gpu-load": _gpuLoad = snap.Value; break;
                            case "gpu-temp": _gpuTemp = snap.Value; break;
                            case "gpu-power": _gpuPower = snap.Raw ?? "N/A"; break;

                            case "vram-used": _vramUsed = snap.Raw ?? "N/A"; break;

                            case "mem-used-percent": _ramUsed = snap.Value; break;
                            case "ram-free": _ramFreeGb = snap.Value; break;

                            case "drive-c": _driveCGb = snap.Value; break;
                            case "drive-e": _driveEGb = snap.Value; break;

                            case "internet": _internet = snap.Raw ?? "--"; break;
                        }
                        Render();
                    });
                }
            });
        }

        private void Render()
        {
            FpsText.Text = _fps.HasValue ? $"{_settings.LabelFps} {_fps.Value:F0}" : $"{_settings.LabelFps} --";
            CpuLoadText.Text = _cpuLoad.HasValue ? $"{_settings.LabelCpuLoad} {_cpuLoad.Value:F0}%" : $"{_settings.LabelCpuLoad} --";
            CpuTempText.Text = _cpuTemp.HasValue ? $"{_settings.LabelCpuTemp} {_cpuTemp.Value:F0}°C" : $"{_settings.LabelCpuTemp} --";
            CpuPowerText.Text = $"{_settings.LabelCpuPower} {_cpuPower}";

            GpuLoadText.Text = _gpuLoad.HasValue ? $"{_settings.LabelGpuLoad} {_gpuLoad.Value:F0}%" : $"{_settings.LabelGpuLoad} --";
            GpuTempText.Text = _gpuTemp.HasValue ? $"{_settings.LabelGpuTemp} {_gpuTemp.Value:F0}°C" : $"{_settings.LabelGpuTemp} --";
            GpuPowerText.Text = $"{_settings.LabelGpuPower} {_gpuPower}";

            VramUsedText.Text = $"{_settings.LabelVramUsed} {_vramUsed}";
            RamUsedText.Text = _ramUsed.HasValue ? $"{_settings.LabelRamUsed} {_ramUsed.Value:F0}%" : $"{_settings.LabelRamUsed} --";
            RamFreeText.Text = _ramFreeGb.HasValue ? $"{_settings.LabelRamFree} {_ramFreeGb.Value:F1}GB" : $"{_settings.LabelRamFree} --";

            DriveCText.Text = _driveCGb.HasValue ? $"{_settings.LabelDriveC} {_driveCGb.Value:F0}GB" : $"{_settings.LabelDriveC} --";
            DriveEText.Text = _driveEGb.HasValue ? $"{_settings.LabelDriveE} {_driveEGb.Value:F0}GB" : $"{_settings.LabelDriveE} --";

            InternetText.Text = $"{_settings.LabelInternet} {_internet}";
        }

        private void ApplySettings(AppSettings s)
        {
            _settings = s;
            Opacity = s.UiOpacity;
            RootBorder.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString(s.BackgroundHex)!;
            ReorderBySettings();
            Render();
        }

        private void ReorderBySettings()
        {
            MetricsPanel.Children.Clear();

            var enabled = (_settings.EnabledMetrics ?? Array.Empty<string>())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var id in _settings.MetricOrder)
            {
                if (!enabled.Contains(id)) continue;
                if (_blocks.TryGetValue(id, out var b)) MetricsPanel.Children.Add(b);
            }
        }

        private void DebouncedSave(AppSettings s)
        {
            _saveDebounce?.Cancel();
            _saveDebounce = new CancellationTokenSource();
            var token = _saveDebounce.Token;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(400, token);
                    await ConfigStore.SaveAsync(s);
                }
                catch { }
            }, token);
        }

        private async Task OpenSettingsAsync()
        {
            var win = new SettingsWindow(_settings,
                liveApply: (ns) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        ApplySettings(ns);
                        DebouncedSave(ns);
                    });
                },
                exitApp: () => Dispatcher.Invoke(() =>
                {
                    _isExitRequested = true;
                    Close();
                    System.Windows.Application.Current.Shutdown();
                }))
            { Owner = this };

            win.ShowDialog();
        }

        private void ShowContextMenu()
        {
            var menu = new ContextMenu();
            var settings = new MenuItem { Header = "Settings..." };
            settings.Click += async (_, __) => await OpenSettingsAsync();
            var show = new MenuItem { Header = "Show" };
            show.Click += (_, __) => { Show(); Activate(); Topmost = true; };
            var hide = new MenuItem { Header = "Hide" };
            hide.Click += (_, __) => Hide();
            var exit = new MenuItem { Header = "Exit" };
            exit.Click += (_, __) =>
            {
                _isExitRequested = true;
                Close();
                System.Windows.Application.Current.Shutdown();
            };

            menu.Items.Add(settings);
            menu.Items.Add(new Separator());
            menu.Items.Add(show);
            menu.Items.Add(hide);
            menu.Items.Add(new Separator());
            menu.Items.Add(exit);
            menu.IsOpen = true;
        }

        private async void MainWindow_Closed(object? sender, EventArgs e)
        {
            _saveDebounce?.Cancel();
            _tray?.Dispose();
            _appBar?.Dispose();
            if (_engine != null) await _engine.DisposeAsync();
        }
    }
}

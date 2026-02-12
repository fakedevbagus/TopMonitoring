using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using TopMonitoring.Infrastructure;
using TopMonitoring.App.ViewModels;

namespace TopMonitoring.App
{
    public partial class MainWindow : Window
    {
        private readonly MonitoringService _monitoring;
        private OverlayDockingService? _docking;
        private TrayService? _tray;
        private readonly SettingsService _settingsService;
        private readonly MainViewModel _vm = new();
        private AppSettings _settings = new();
        private bool _isExitRequested;
        private OverlayInputService? _input;
        private bool _restoreClickThroughAfterMenu;
        private MouseHookService? _mouseHook;

        private DispatcherTimer? _renderTimer;
        private DispatcherTimer? _alertBlinkTimer;
        private bool _alertBlinkOn;
        private bool _pendingRender;

        private readonly Dictionary<string, string> _lastRendered = new();

        // cached values
        private double? _fps, _cpuLoad, _cpuTemp, _gpuLoad, _gpuTemp, _ramUsed, _ramFreeGb, _driveCGb, _driveDGb, _driveEGb, _driveFGb, _driveGGb;
        private string _cpuPower = "N/A", _gpuPower = "N/A", _vramUsed = "N/A", _internet = "--";

        public MainWindow(SettingsService settingsService, MonitoringService monitoringService)
        {
            InitializeComponent();
            _settingsService = settingsService;
            _monitoring = monitoringService;
            DataContext = _vm;
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
            Closed += MainWindow_Closed;
            SourceInitialized += (_, __) =>
            {
                _input = new OverlayInputService(this);
                _input.ApplySettings(_settings, ToggleVisibility, ToggleClickThrough, CyclePreset);
            };
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
            _settingsService.SettingsChanged += ApplySettings;
            await _settingsService.LoadAsync();
            _settings = _settingsService.Current;
            ApplySettings(_settings);
            // Ensure bar is visible even if AppBar quirks happen
            ForceShow();

            _mouseHook = new MouseHookService(OnGlobalRightClick);

            _docking = new OverlayDockingService(this);
            _docking.Apply(_settings);

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

            _monitoring.Start();

            await Task.Run(async () =>
            {
                await foreach (var snap in _monitoring.GetSnapshotsAsync())
                {
                    _ = Dispatcher.BeginInvoke(new Action(() =>
                    {
                        switch (snap.Id)
                        {
                            case "fps": if (ShouldUpdate(_fps, snap.Value, 1)) _fps = snap.Value; break;

                            case "cpu-total-load": if (ShouldUpdate(_cpuLoad, snap.Value, 0.5)) _cpuLoad = snap.Value; break;
                            case "cpu-temp": if (ShouldUpdate(_cpuTemp, snap.Value, 0.5)) _cpuTemp = snap.Value; break;
                            case "cpu-power": if (ShouldUpdate(_cpuPower, snap.Raw)) _cpuPower = snap.Raw ?? "N/A"; break;

                            case "gpu-load": if (ShouldUpdate(_gpuLoad, snap.Value, 0.5)) _gpuLoad = snap.Value; break;
                            case "gpu-temp": if (ShouldUpdate(_gpuTemp, snap.Value, 0.5)) _gpuTemp = snap.Value; break;
                            case "gpu-power": if (ShouldUpdate(_gpuPower, snap.Raw)) _gpuPower = snap.Raw ?? "N/A"; break;

                            case "vram-used": if (ShouldUpdate(_vramUsed, snap.Raw)) _vramUsed = snap.Raw ?? "N/A"; break;

                            case "mem-used-percent": if (ShouldUpdate(_ramUsed, snap.Value, 0.5)) _ramUsed = snap.Value; break;
                            case "ram-free": if (ShouldUpdate(_ramFreeGb, snap.Value, 0.1)) _ramFreeGb = snap.Value; break;

                            case "drive-c": if (ShouldUpdate(_driveCGb, snap.Value, 0.1)) _driveCGb = snap.Value; break;
                            case "drive-d": if (ShouldUpdate(_driveDGb, snap.Value, 0.1)) _driveDGb = snap.Value; break;
                            case "drive-e": if (ShouldUpdate(_driveEGb, snap.Value, 0.1)) _driveEGb = snap.Value; break;
                            case "drive-f": if (ShouldUpdate(_driveFGb, snap.Value, 0.1)) _driveFGb = snap.Value; break;
                            case "drive-g": if (ShouldUpdate(_driveGGb, snap.Value, 0.1)) _driveGGb = snap.Value; break;

                            case "internet": if (ShouldUpdate(_internet, snap.Raw)) _internet = snap.Raw ?? "--"; break;
                        }
                        // Mark render pending; timer will batch UI updates.
                        _pendingRender = true;
                    }), DispatcherPriority.Background);
                }
            });
        }

        private void Render()
        {
            // Avoid touching UI when nothing changed since the last render tick.
            if (!_pendingRender) return;
            _pendingRender = false;

            SetTextIfChanged("fps", _fps.HasValue ? $"{_settings.LabelFps} {_fps.Value:F0}" : $"{_settings.LabelFps} --");
            SetTextIfChanged("cpu-load", _cpuLoad.HasValue ? $"{_settings.LabelCpuLoad} {_cpuLoad.Value:F0}%" : $"{_settings.LabelCpuLoad} --");
            SetTextIfChanged("cpu-temp", _cpuTemp.HasValue ? $"{_settings.LabelCpuTemp} {_cpuTemp.Value:F0}°C" : $"{_settings.LabelCpuTemp} --");
            SetTextIfChanged("cpu-power", $"{_settings.LabelCpuPower} {_cpuPower}");

            SetTextIfChanged("gpu-load", _gpuLoad.HasValue ? $"{_settings.LabelGpuLoad} {_gpuLoad.Value:F0}%" : $"{_settings.LabelGpuLoad} --");
            SetTextIfChanged("gpu-temp", _gpuTemp.HasValue ? $"{_settings.LabelGpuTemp} {_gpuTemp.Value:F0}°C" : $"{_settings.LabelGpuTemp} --");
            SetTextIfChanged("gpu-power", $"{_settings.LabelGpuPower} {_gpuPower}");

            SetTextIfChanged("vram-used", $"{_settings.LabelVramUsed} {_vramUsed}");
            SetTextIfChanged("ram-used", _ramUsed.HasValue ? $"{_settings.LabelRamUsed} {_ramUsed.Value:F0}%" : $"{_settings.LabelRamUsed} --");
            SetTextIfChanged("ram-free", _ramFreeGb.HasValue ? $"{_settings.LabelRamFree} {_ramFreeGb.Value:F1}GB" : $"{_settings.LabelRamFree} --");

            SetTextIfChanged("drive-c", _driveCGb.HasValue ? $"{_settings.LabelDriveC} {_driveCGb.Value:F0}GB" : $"{_settings.LabelDriveC} --");
            SetTextIfChanged("drive-d", _driveDGb.HasValue ? $"{_settings.LabelDriveD} {_driveDGb.Value:F0}GB" : $"{_settings.LabelDriveD} --");
            SetTextIfChanged("drive-e", _driveEGb.HasValue ? $"{_settings.LabelDriveE} {_driveEGb.Value:F0}GB" : $"{_settings.LabelDriveE} --");
            SetTextIfChanged("drive-f", _driveFGb.HasValue ? $"{_settings.LabelDriveF} {_driveFGb.Value:F0}GB" : $"{_settings.LabelDriveF} --");
            SetTextIfChanged("drive-g", _driveGGb.HasValue ? $"{_settings.LabelDriveG} {_driveGGb.Value:F0}GB" : $"{_settings.LabelDriveG} --");

            SetTextIfChanged("internet", $"{_settings.LabelInternet} {_internet}");

            ApplyAlertFlags();
        }

        private void ApplySettings(AppSettings s)
        {
            _settings = AppSettings.Normalize(s);
            Opacity = _settings.UiOpacity;
            RootBorder.Background = (System.Windows.Media.Brush)new BrushConverter().ConvertFromString(_settings.BackgroundHex)!;
            RootBorder.LayoutTransform = new ScaleTransform(_settings.UIScale, _settings.UIScale);
            ThemeService.ApplyTheme(_settings.SettingsTheme);
            ReorderBySettings();
            UpdateRenderTimer();
            UpdateAlertBlinkTimer();
            _input?.ApplySettings(_settings, ToggleVisibility, ToggleClickThrough, CyclePreset);
            _docking?.Apply(_settings);
            _pendingRender = true;
        }

        private void ReorderBySettings()
        {
            var order = _settings.MetricOrder ?? Array.Empty<string>();
            var enabled = (_settings.EnabledMetrics ?? Array.Empty<string>()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            _vm.ApplyOrder(order, enabled);
        }

        private Task OpenSettingsAsync()
        {
            var restoreClickThrough = _settings.IsClickThroughEnabled;
            if (restoreClickThrough)
                ClickThroughHelper.Apply(this, false);

            try
            {
                var win = new SettingsWindow(_settings,
                    liveApply: (ns) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            _settingsService.Update(ns);
                        });
                    },
                    importSettings: async (path) => await _settingsService.ImportAsync(path),
                    exportSettings: async (path) => await _settingsService.ExportAsync(path),
                    exitApp: () => Dispatcher.Invoke(() =>
                    {
                        _isExitRequested = true;
                        Close();
                        System.Windows.Application.Current.Shutdown();
                    }))
                {
                    Owner = this,
                    Topmost = true,
                    ShowInTaskbar = true,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                win.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString(), "SETTINGS ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (restoreClickThrough)
                    ClickThroughHelper.Apply(this, true);
            }
            return Task.CompletedTask;
        }

        private void OpenContextMenu()
        {
            if (SettingsContextMenu == null) return;

            if (_settings.IsClickThroughEnabled)
            {
                _restoreClickThroughAfterMenu = true;
                ClickThroughHelper.Apply(this, false);
            }

            SettingsContextMenu.PlacementTarget = RootGrid;
            SettingsContextMenu.IsOpen = true;
            SettingsContextMenu.Closed -= SettingsContextMenu_Closed;
            SettingsContextMenu.Closed += SettingsContextMenu_Closed;
        }

        private void OnGlobalRightClick(int screenX, int screenY)
        {
            if (!_settings.IsClickThroughEnabled) return;
            if (!IsVisible) return;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!_settings.IsClickThroughEnabled) return;
                var local = PointFromScreen(new System.Windows.Point(screenX, screenY));
                if (local.X < 0 || local.Y < 0 || local.X > ActualWidth || local.Y > ActualHeight) return;
                OpenContextMenu();
            }), DispatcherPriority.Input);
        }

        private void SettingsContextMenu_Closed(object? sender, RoutedEventArgs e)
        {
            if (_restoreClickThroughAfterMenu)
            {
                _restoreClickThroughAfterMenu = false;
                ClickThroughHelper.Apply(this, true);
            }
        }

        private void OnRightClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;
            OpenContextMenu();
        }

        private async void SettingsMenu_Click(object sender, RoutedEventArgs e)
        {
            await OpenSettingsAsync();
        }

        private void ShowMenu_Click(object sender, RoutedEventArgs e)
        {
            Show();
            Activate();
            Topmost = true;
        }

        private void HideMenu_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void ExitMenu_Click(object sender, RoutedEventArgs e)
        {
            _isExitRequested = true;
            Close();
            System.Windows.Application.Current.Shutdown();
        }

        private async void MainWindow_Closed(object? sender, EventArgs e)
        {
            _settingsService.SettingsChanged -= ApplySettings;
            _tray?.Dispose();
            _docking?.Dispose();
            _input?.Dispose();
            _mouseHook?.Dispose();
            _renderTimer?.Stop();
            _alertBlinkTimer?.Stop();
            await _monitoring.DisposeAsync();
        }


        private void UpdateRenderTimer()
        {
            _renderTimer ??= new DispatcherTimer(DispatcherPriority.Background, Dispatcher);
            _renderTimer.Stop();
            _renderTimer.Interval = TimeSpan.FromMilliseconds(_settings.UiUpdateIntervalMs);
            _renderTimer.Tick -= RenderTimer_Tick;
            _renderTimer.Tick += RenderTimer_Tick;
            _renderTimer.Start();
        }

        private void RenderTimer_Tick(object? sender, EventArgs e) => Render();

        private void UpdateAlertBlinkTimer()
        {
            if (_settings.AlertBlinkEnabled)
            {
                _alertBlinkTimer ??= new DispatcherTimer(DispatcherPriority.Background, Dispatcher);
                _alertBlinkTimer.Interval = TimeSpan.FromMilliseconds(500);
                _alertBlinkTimer.Tick -= AlertBlinkTimer_Tick;
                _alertBlinkTimer.Tick += AlertBlinkTimer_Tick;
                _alertBlinkTimer.Start();
            }
            else
            {
                _alertBlinkTimer?.Stop();
                _alertBlinkOn = false;
                _pendingRender = true;
            }
        }

        private void AlertBlinkTimer_Tick(object? sender, EventArgs e)
        {
            _alertBlinkOn = !_alertBlinkOn;
            _pendingRender = true;
        }

        private void ApplyAlertFlags()
        {
            ApplyThresholdFlag("cpu-load", _cpuLoad, _settings.CpuAlertThreshold);
            ApplyThresholdFlag("ram-used", _ramUsed, _settings.RamAlertThreshold);
            ApplyThresholdFlag("gpu-load", _gpuLoad, _settings.GpuAlertThreshold);
        }

        private void ApplyThresholdFlag(string id, double? value, double threshold)
        {
            var inAlert = value.HasValue && value.Value >= threshold;
            var showAlert = inAlert && (!_settings.AlertBlinkEnabled || _alertBlinkOn);
            _vm.SetAlert(id, showAlert);
        }

        private static bool ShouldUpdate(double? current, double? next, double delta)
        {
            if (!next.HasValue) return current.HasValue;
            if (!current.HasValue) return true;
            // Ignore tiny changes to reduce UI churn.
            return Math.Abs(next.Value - current.Value) >= delta;
        }

        private static bool ShouldUpdate(string current, string? next)
        {
            if (next == null) return false;
            return !string.Equals(current, next, StringComparison.Ordinal);
        }

        private void SetTextIfChanged(string id, string text)
        {
            if (_lastRendered.TryGetValue(id, out var last) && string.Equals(last, text, StringComparison.Ordinal))
                return;
            _vm.SetText(id, text);
            _lastRendered[id] = text;
        }


        private void ToggleVisibility()
        {
            if (IsVisible) Hide();
            else { Show(); Activate(); Topmost = true; }
        }

        private void ToggleClickThrough()
        {
            var ns = _settings with { IsClickThroughEnabled = !_settings.IsClickThroughEnabled };
            _settingsService.Update(ns);
        }

        private void CyclePreset()
        {
            var presetList = _settings.Presets ?? AppSettings.DefaultPresets();
            var ids = new List<string> { AppSettings.CustomPresetId };
            ids.AddRange(presetList.Select(p => p.Id));

            var idx = ids.FindIndex(id => id.Equals(_settings.ActivePresetId, StringComparison.OrdinalIgnoreCase));
            var next = (idx < 0 || idx == ids.Count - 1) ? ids[0] : ids[idx + 1];
            var ns = AppSettings.ApplyPreset(_settings, next);
            _settingsService.Update(ns);
        }
    }
}

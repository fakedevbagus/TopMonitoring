using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using TopMonitoring.Infrastructure;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace TopMonitoring.App
{
    public partial class SettingsWindow : Window
    {
        private bool _initializing = true;

        private readonly Action<AppSettings> _liveApply;
        private readonly Func<string, Task<AppSettings?>> _importSettings;
        private readonly Func<string, Task> _exportSettings;
        private readonly Action _exitApp;
        private AppSettings _original;
        private bool _closing;

        public AppSettings Settings { get; private set; }

        private sealed record PresetOption(string Id, string Name);

        public SettingsWindow(
            AppSettings current,
            Action<AppSettings> liveApply,
            Func<string, Task<AppSettings?>> importSettings,
            Func<string, Task> exportSettings,
            Action exitApp)
        {
            InitializeComponent();
            _original = current;
            Settings = current;
            DataContext = Settings;
            _liveApply = liveApply;
            _importSettings = importSettings;
            _exportSettings = exportSettings;
            _exitApp = exitApp;

            Loaded += OnLoaded;
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            try
            {
                DarkModeCheck.IsChecked = !Settings.SettingsTheme.Equals("Light", StringComparison.OrdinalIgnoreCase);
                ThemeService.ApplyTheme(DarkModeCheck.IsChecked == true ? "Dark" : "Light");

                if (TryFindResource("BackgroundBrush") == null)
                {
                    var theme = Settings.SettingsTheme.Equals("Light", StringComparison.OrdinalIgnoreCase)
                        ? "Themes/LightTheme.xaml"
                        : "Themes/DarkTheme.xaml";
                    Resources.MergedDictionaries.Add(new ResourceDictionary
                    {
                        Source = new Uri(theme, UriKind.Relative)
                    });
                }

                OpacitySlider.Value = Settings.UiOpacity;
                UpdateOpacityPct();

                ClickThroughCheck.IsChecked = Settings.IsClickThroughEnabled;

                UIScaleSlider.Value = Settings.UIScale;
                UpdateScalePct();

                UpdateIntervalSlider.Value = Settings.UiUpdateIntervalMs;
                UpdateUpdateIntervalText();

                CpuAlertSlider.Value = Settings.CpuAlertThreshold;
                RamAlertSlider.Value = Settings.RamAlertThreshold;
                GpuAlertSlider.Value = Settings.GpuAlertThreshold;
                AlertBlinkCheck.IsChecked = Settings.AlertBlinkEnabled;
                UpdateAlertPct();

                HotkeyToggleVisibilityText.Text = Settings.ToggleVisibilityHotkey?.Gesture ?? string.Empty;
                HotkeyToggleClickThroughText.Text = Settings.ToggleClickThroughHotkey?.Gesture ?? string.Empty;
                HotkeyCyclePresetText.Text = Settings.CyclePresetHotkey?.Gesture ?? string.Empty;

                LoadMonitors(Settings.TargetMonitor);
                LoadPresets(Settings);

                HexText.Text = Settings.BackgroundHex;
                HexText.TextChanged += (_, __) =>
                {
                    if (_closing) return;

                    var hex = HexText.Text?.Trim();
                    if (string.IsNullOrWhiteSpace(hex)) return;

                    if (hex.StartsWith("#") && (hex.Length == 7 || hex.Length == 9))
                    {
                        Settings = Settings with { BackgroundHex = hex };
                        _liveApply(Settings);
                    }
                };

                // labels
                LblFps.Text = Settings.LabelFps;
                LblCpuLoad.Text = Settings.LabelCpuLoad;
                LblCpuTemp.Text = Settings.LabelCpuTemp;
                LblCpuPower.Text = Settings.LabelCpuPower;
                LblGpuLoad.Text = Settings.LabelGpuLoad;
                LblGpuTemp.Text = Settings.LabelGpuTemp;
                LblGpuPower.Text = Settings.LabelGpuPower;
                LblVramUsed.Text = Settings.LabelVramUsed;
                LblRamUsed.Text = Settings.LabelRamUsed;
                LblRamFree.Text = Settings.LabelRamFree;
                LblDriveC.Text = Settings.LabelDriveC;
                LblDriveD.Text = Settings.LabelDriveD;
                LblDriveE.Text = Settings.LabelDriveE;
                LblDriveF.Text = Settings.LabelDriveF;
                LblDriveG.Text = Settings.LabelDriveG;
                LblInternet.Text = Settings.LabelInternet;

                var enabled = (Settings.EnabledMetrics ?? Array.Empty<string>()).ToHashSet(StringComparer.OrdinalIgnoreCase);
                CbFps.IsChecked = enabled.Contains("fps");
                CbCpuLoad.IsChecked = enabled.Contains("cpu-load");
                CbCpuTemp.IsChecked = enabled.Contains("cpu-temp");
                CbCpuPower.IsChecked = enabled.Contains("cpu-power");
                CbGpuLoad.IsChecked = enabled.Contains("gpu-load");
                CbGpuTemp.IsChecked = enabled.Contains("gpu-temp");
                CbGpuPower.IsChecked = enabled.Contains("gpu-power");
                CbVramUsed.IsChecked = enabled.Contains("vram-used");
                CbRamUsed.IsChecked = enabled.Contains("ram-used");
                CbRamFree.IsChecked = enabled.Contains("ram-free");
                CbDriveC.IsChecked = enabled.Contains("drive-c");
                CbDriveD.IsChecked = enabled.Contains("drive-d");
                CbDriveE.IsChecked = enabled.Contains("drive-e");
                CbDriveF.IsChecked = enabled.Contains("drive-f");
                CbDriveG.IsChecked = enabled.Contains("drive-g");
                CbInternet.IsChecked = enabled.Contains("internet");

                OrderList.Items.Clear();
                var order = EnsureOrderHasDrives(Settings.MetricOrder);
                foreach (var id in order)
                    OrderList.Items.Add(ToDisplay(id));

                HookEvents();
                HookLabelEvents();
                HookMetricsEnableEvents();
                HookHotkeyEvents();

                _initializing = false;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString(), "SETTINGS LOAD ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string[] EnsureOrderHasDrives(string[]? order)
        {
            var baseOrder = (order != null && order.Length > 0) ? order : new AppSettings().MetricOrder;
            var list = baseOrder.ToList();
            var set = list.ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var id in new[] { "drive-d", "drive-f", "drive-g" })
            {
                if (set.Add(id)) list.Add(id);
            }
            if (!baseOrder.SequenceEqual(list, StringComparer.OrdinalIgnoreCase))
            {
                Settings = Settings with { MetricOrder = list.ToArray() };
                if (Settings.ActivePresetId.Equals(AppSettings.CustomPresetId, StringComparison.OrdinalIgnoreCase))
                {
                    _liveApply(Settings);
                }
            }
            return list.ToArray();
        }

        private void HookEvents()
        {
            OpacitySlider.ValueChanged += (_, __) =>
            {
                if (_closing || _initializing) return;
                UpdateOpacityPct();
                Settings = Settings with { UiOpacity = OpacitySlider.Value, ActivePresetId = AppSettings.CustomPresetId };
                EnsureCustomPresetSelected();
                _liveApply(Settings);
            };

            UIScaleSlider.ValueChanged += (_, __) =>
            {
                if (_closing || _initializing) return;
                UpdateScalePct();
                Settings = Settings with { UIScale = UIScaleSlider.Value, ActivePresetId = AppSettings.CustomPresetId };
                EnsureCustomPresetSelected();
                _liveApply(Settings);
            };

            UpdateIntervalSlider.ValueChanged += (_, __) =>
            {
                if (_closing || _initializing) return;
                UpdateUpdateIntervalText();
                Settings = Settings with { UiUpdateIntervalMs = (int)UpdateIntervalSlider.Value, ActivePresetId = AppSettings.CustomPresetId };
                EnsureCustomPresetSelected();
                _liveApply(Settings);
            };

            ClickThroughCheck.Checked += (_, __) => ApplyClickThroughFromUi();
            ClickThroughCheck.Unchecked += (_, __) => ApplyClickThroughFromUi();

            TargetMonitorCombo.SelectionChanged += (_, __) => ApplyTargetMonitorFromUi();

            PresetCombo.SelectionChanged += (_, __) => ApplyPresetFromUi();

            CpuAlertSlider.ValueChanged += (_, __) => ApplyAlertThresholdsFromUi();
            RamAlertSlider.ValueChanged += (_, __) => ApplyAlertThresholdsFromUi();
            GpuAlertSlider.ValueChanged += (_, __) => ApplyAlertThresholdsFromUi();
            AlertBlinkCheck.Checked += (_, __) => ApplyAlertThresholdsFromUi();
            AlertBlinkCheck.Unchecked += (_, __) => ApplyAlertThresholdsFromUi();
        }

        private void UpdateOpacityPct() => OpacityPct.Text = $"{(int)(OpacitySlider.Value * 100)}%";

        private void UpdateScalePct() => UIScalePct.Text = $"{(int)(UIScaleSlider.Value * 100)}%";

        private void UpdateUpdateIntervalText() => UpdateIntervalText.Text = $"{(int)UpdateIntervalSlider.Value}";

        private void UpdateAlertPct()
        {
            CpuAlertPct.Text = $"{(int)CpuAlertSlider.Value}%";
            RamAlertPct.Text = $"{(int)RamAlertSlider.Value}%";
            GpuAlertPct.Text = $"{(int)GpuAlertSlider.Value}%";
        }

        private void LoadMonitors(string targetMonitor)
        {
            TargetMonitorCombo.SelectedValuePath = "Id";
            TargetMonitorCombo.ItemsSource = MonitorService.GetMonitors();
            TargetMonitorCombo.SelectedValue = targetMonitor;
        }

        private void LoadPresets(AppSettings current)
        {
            var list = new List<PresetOption>
            {
                new PresetOption(AppSettings.CustomPresetId, "Custom")
            };

            var presets = current.Presets ?? AppSettings.DefaultPresets();
            list.AddRange(presets.Select(p => new PresetOption(p.Id, p.Name)));

            PresetCombo.SelectedValuePath = "Id";
            PresetCombo.ItemsSource = list;
            PresetCombo.SelectedValue = current.ActivePresetId;
        }

        private void ApplyLabelsFromUI()
        {
            if (_closing) return;

            Settings = Settings with
            {
                LabelFps = LblFps.Text,
                LabelCpuLoad = LblCpuLoad.Text,
                LabelCpuTemp = LblCpuTemp.Text,
                LabelCpuPower = LblCpuPower.Text,
                LabelGpuLoad = LblGpuLoad.Text,
                LabelGpuTemp = LblGpuTemp.Text,
                LabelGpuPower = LblGpuPower.Text,
                LabelVramUsed = LblVramUsed.Text,
                LabelRamUsed = LblRamUsed.Text,
                LabelRamFree = LblRamFree.Text,
                LabelDriveC = LblDriveC.Text,
                LabelDriveD = LblDriveD.Text,
                LabelDriveE = LblDriveE.Text,
                LabelDriveF = LblDriveF.Text,
                LabelDriveG = LblDriveG.Text,
                LabelInternet = LblInternet.Text
            };

            _liveApply(Settings);
        }

        private void HookLabelEvents()
        {
            void hook(System.Windows.Controls.TextBox tb)
            {
                tb.TextChanged += (_, __) => ApplyLabelsFromUI();
                tb.LostFocus += (_, __) => ApplyLabelsFromUI();
                tb.KeyUp += (_, e) =>
                {
                    if (e.Key == Key.Enter) ApplyLabelsFromUI();
                };
            }

            hook(LblFps);
            hook(LblCpuLoad);
            hook(LblCpuTemp);
            hook(LblCpuPower);
            hook(LblGpuLoad);
            hook(LblGpuTemp);
            hook(LblGpuPower);
            hook(LblVramUsed);
            hook(LblRamUsed);
            hook(LblRamFree);
            hook(LblDriveC);
            hook(LblDriveD);
            hook(LblDriveE);
            hook(LblDriveF);
            hook(LblDriveG);
            hook(LblInternet);
        }

        private void HookHotkeyEvents()
        {
            void hook(System.Windows.Controls.TextBox tb, Action<string> setter)
            {
                tb.LostFocus += (_, __) => setter(tb.Text);
                tb.KeyUp += (_, e) =>
                {
                    if (e.Key == Key.Enter) setter(tb.Text);
                };
            }

            hook(HotkeyToggleVisibilityText, s => UpdateHotkey(s, hk => Settings with { ToggleVisibilityHotkey = hk }));
            hook(HotkeyToggleClickThroughText, s => UpdateHotkey(s, hk => Settings with { ToggleClickThroughHotkey = hk }));
            hook(HotkeyCyclePresetText, s => UpdateHotkey(s, hk => Settings with { CyclePresetHotkey = hk }));
        }

        private void UpdateHotkey(string text, Func<HotkeyBinding, AppSettings> apply)
        {
            if (_closing) return;
            var binding = new HotkeyBinding { Gesture = text?.Trim() ?? string.Empty };
            Settings = apply(binding) with { ActivePresetId = AppSettings.CustomPresetId };
            EnsureCustomPresetSelected();
            _liveApply(Settings);
        }

        private void ApplyClickThroughFromUi()
        {
            if (_closing || _initializing) return;
            Settings = Settings with { IsClickThroughEnabled = ClickThroughCheck.IsChecked == true, ActivePresetId = AppSettings.CustomPresetId };
            EnsureCustomPresetSelected();
            _liveApply(Settings);
        }

        private void ApplyTargetMonitorFromUi()
        {
            if (_closing || _initializing) return;
            if (TargetMonitorCombo.SelectedValue is not string id) return;
            Settings = Settings with { TargetMonitor = id, ActivePresetId = AppSettings.CustomPresetId };
            EnsureCustomPresetSelected();
            _liveApply(Settings);
        }

        private void ApplyPresetFromUi()
        {
            if (_closing || _initializing) return;
            if (PresetCombo.SelectedValue is not string id) return;

            Settings = AppSettings.ApplyPreset(Settings, id);
            _liveApply(Settings);
            SyncUiFromSettings(Settings, refreshPresetSelection: false);
        }

        private void ApplyAlertThresholdsFromUi()
        {
            if (_closing || _initializing) return;
            UpdateAlertPct();
            Settings = Settings with
            {
                CpuAlertThreshold = CpuAlertSlider.Value,
                RamAlertThreshold = RamAlertSlider.Value,
                GpuAlertThreshold = GpuAlertSlider.Value,
                AlertBlinkEnabled = AlertBlinkCheck.IsChecked == true,
                ActivePresetId = AppSettings.CustomPresetId
            };
            EnsureCustomPresetSelected();
            _liveApply(Settings);
        }

        private void EnsureCustomPresetSelected()
        {
            if (PresetCombo.SelectedValue is string id && id.Equals(AppSettings.CustomPresetId, StringComparison.OrdinalIgnoreCase))
                return;

            _initializing = true;
            PresetCombo.SelectedValue = AppSettings.CustomPresetId;
            _initializing = false;
        }

        private void SyncUiFromSettings(AppSettings current, bool refreshPresetSelection)
        {
            _initializing = true;

            DarkModeCheck.IsChecked = !current.SettingsTheme.Equals("Light", StringComparison.OrdinalIgnoreCase);
            ThemeService.ApplyTheme(DarkModeCheck.IsChecked == true ? "Dark" : "Light");

            OpacitySlider.Value = current.UiOpacity;
            UpdateOpacityPct();

            UIScaleSlider.Value = current.UIScale;
            UpdateScalePct();

            UpdateIntervalSlider.Value = current.UiUpdateIntervalMs;
            UpdateUpdateIntervalText();

            ClickThroughCheck.IsChecked = current.IsClickThroughEnabled;
            TargetMonitorCombo.SelectedValue = current.TargetMonitor;

            CpuAlertSlider.Value = current.CpuAlertThreshold;
            RamAlertSlider.Value = current.RamAlertThreshold;
            GpuAlertSlider.Value = current.GpuAlertThreshold;
            AlertBlinkCheck.IsChecked = current.AlertBlinkEnabled;
            UpdateAlertPct();

            var enabled = (current.EnabledMetrics ?? Array.Empty<string>()).ToHashSet(StringComparer.OrdinalIgnoreCase);
            CbFps.IsChecked = enabled.Contains("fps");
            CbCpuLoad.IsChecked = enabled.Contains("cpu-load");
            CbCpuTemp.IsChecked = enabled.Contains("cpu-temp");
            CbCpuPower.IsChecked = enabled.Contains("cpu-power");
            CbGpuLoad.IsChecked = enabled.Contains("gpu-load");
            CbGpuTemp.IsChecked = enabled.Contains("gpu-temp");
            CbGpuPower.IsChecked = enabled.Contains("gpu-power");
            CbVramUsed.IsChecked = enabled.Contains("vram-used");
            CbRamUsed.IsChecked = enabled.Contains("ram-used");
            CbRamFree.IsChecked = enabled.Contains("ram-free");
            CbDriveC.IsChecked = enabled.Contains("drive-c");
            CbDriveD.IsChecked = enabled.Contains("drive-d");
            CbDriveE.IsChecked = enabled.Contains("drive-e");
            CbDriveF.IsChecked = enabled.Contains("drive-f");
            CbDriveG.IsChecked = enabled.Contains("drive-g");
            CbInternet.IsChecked = enabled.Contains("internet");

            if (refreshPresetSelection)
            {
                PresetCombo.SelectedValue = current.ActivePresetId;
            }

            _initializing = false;
        }

        private static string ToDisplay(string id) => id switch
        {
            "fps" => "FPS",
            "cpu-load" => "CPU Load",
            "cpu-temp" => "CPU Temp",
            "cpu-power" => "CPU Power",
            "gpu-load" => "GPU Load",
            "gpu-temp" => "GPU Temp",
            "gpu-power" => "GPU Power",
            "vram-used" => "VRAM Used",
            "ram-used" => "RAM Used",
            "ram-free" => "RAM Free",
            "drive-c" => "Drive C",
            "drive-d" => "Drive D",
            "drive-e" => "Drive E",
            "drive-f" => "Drive F",
            "drive-g" => "Drive G",
            "internet" => "Internet",
            _ => id
        };

        private static string FromDisplay(string s) => s switch
        {
            "FPS" => "fps",
            "CPU Load" => "cpu-load",
            "CPU Temp" => "cpu-temp",
            "CPU Power" => "cpu-power",
            "GPU Load" => "gpu-load",
            "GPU Temp" => "gpu-temp",
            "GPU Power" => "gpu-power",
            "VRAM Used" => "vram-used",
            "RAM Used" => "ram-used",
            "RAM Free" => "ram-free",
            "Drive C" => "drive-c",
            "Drive D" => "drive-d",
            "Drive E" => "drive-e",
            "Drive F" => "drive-f",
            "Drive G" => "drive-g",
            "Internet" => "internet",
            _ => s
        };

        private void Up_Click(object sender, RoutedEventArgs e)
        {
            var idx = OrderList.SelectedIndex;
            if (idx <= 0) return;
            var item = OrderList.Items[idx];
            OrderList.Items.RemoveAt(idx);
            OrderList.Items.Insert(idx - 1, item);
            OrderList.SelectedIndex = idx - 1;
            CommitOrder();
        }

        private void Down_Click(object sender, RoutedEventArgs e)
        {
            var idx = OrderList.SelectedIndex;
            if (idx < 0 || idx >= OrderList.Items.Count - 1) return;
            var item = OrderList.Items[idx];
            OrderList.Items.RemoveAt(idx);
            OrderList.Items.Insert(idx + 1, item);
            OrderList.SelectedIndex = idx + 1;
            CommitOrder();
        }

        private void CommitOrder()
        {
            if (_closing) return;
            Settings = Settings with { MetricOrder = OrderList.Items.Cast<string>().Select(FromDisplay).ToArray() };
            _liveApply(Settings);
        }

        private void HookMetricsEnableEvents()
        {
            void hook(System.Windows.Controls.CheckBox cb)
            {
                cb.Checked += (_, __) => ApplyEnabledMetricsFromUI();
                cb.Unchecked += (_, __) => ApplyEnabledMetricsFromUI();
            }

            hook(CbFps);
            hook(CbCpuLoad);
            hook(CbCpuTemp);
            hook(CbCpuPower);
            hook(CbGpuLoad);
            hook(CbGpuTemp);
            hook(CbGpuPower);
            hook(CbVramUsed);
            hook(CbRamUsed);
            hook(CbRamFree);
            hook(CbDriveC);
            hook(CbDriveD);
            hook(CbDriveE);
            hook(CbDriveF);
            hook(CbDriveG);
            hook(CbInternet);
        }

        private void ApplyEnabledMetricsFromUI()
        {
            if (_closing) return;

            var list = new System.Collections.Generic.List<string>();
            if (CbFps.IsChecked == true) list.Add("fps");
            if (CbCpuLoad.IsChecked == true) list.Add("cpu-load");
            if (CbCpuTemp.IsChecked == true) list.Add("cpu-temp");
            if (CbCpuPower.IsChecked == true) list.Add("cpu-power");
            if (CbGpuLoad.IsChecked == true) list.Add("gpu-load");
            if (CbGpuTemp.IsChecked == true) list.Add("gpu-temp");
            if (CbGpuPower.IsChecked == true) list.Add("gpu-power");
            if (CbVramUsed.IsChecked == true) list.Add("vram-used");
            if (CbRamUsed.IsChecked == true) list.Add("ram-used");
            if (CbRamFree.IsChecked == true) list.Add("ram-free");
            if (CbDriveC.IsChecked == true) list.Add("drive-c");
            if (CbDriveD.IsChecked == true) list.Add("drive-d");
            if (CbDriveE.IsChecked == true) list.Add("drive-e");
            if (CbDriveF.IsChecked == true) list.Add("drive-f");
            if (CbDriveG.IsChecked == true) list.Add("drive-g");
            if (CbInternet.IsChecked == true) list.Add("internet");
            if (list.Count == 0) list.Add("cpu-load");

            Settings = Settings with { EnabledMetrics = list.ToArray(), ActivePresetId = AppSettings.CustomPresetId };
            EnsureCustomPresetSelected();
            _liveApply(Settings);
        }
        private void DarkMode_Changed(object sender, RoutedEventArgs e)
        {
            if (_initializing) return;
            if (_closing) return;

            var newTheme = (DarkModeCheck.IsChecked == true) ? "Dark" : "Light";
            Settings = Settings with { SettingsTheme = newTheme, ActivePresetId = AppSettings.CustomPresetId };
            EnsureCustomPresetSelected();
            _liveApply(Settings);
        }


        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _closing = true;
            _liveApply(_original);
            DialogResult = false;
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            _closing = true;
            var def = new AppSettings();
            _liveApply(def);
            DialogResult = true;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            _closing = true;
            try { _exitApp(); }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString(), "Exit Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            try { Close(); }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString(), "Close Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PickColor_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new ColorDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var color = System.Windows.Media.Color.FromRgb(dialog.Color.R, dialog.Color.G, dialog.Color.B);
                HexText.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
                // Update settings
                Settings = Settings with { BackgroundHex = HexText.Text };
                _liveApply(Settings);
            }
        }

        private async void Export_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json",
                FileName = "TopMonitoring.settings.json"
            };

            if (dialog.ShowDialog() == true)
            {
                await _exportSettings(dialog.FileName);
            }
        }

        private async void Import_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "JSON Files (*.json)|*.json"
            };

            if (dialog.ShowDialog() == true)
            {
                var imported = await _importSettings(dialog.FileName);
                if (imported == null)
                {
                    System.Windows.MessageBox.Show("Invalid settings file.", "Import Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Settings = imported;
                _original = imported;
                LoadPresets(imported);
                LoadMonitors(imported.TargetMonitor);
                SyncUiFromSettings(imported, refreshPresetSelection: true);
            }
        }
    }
}

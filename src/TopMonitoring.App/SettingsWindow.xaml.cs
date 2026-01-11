using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TopMonitoring.Infrastructure;

namespace TopMonitoring.App
{
    public partial class SettingsWindow : Window
    {
        private bool _initializing = true;

        private readonly Action<AppSettings> _liveApply;
        private readonly Action _exitApp;
        private readonly AppSettings _original;
        private bool _closing;

        public AppSettings Settings { get; private set; }

        private void ApplyTheme(string theme)
        {
            if (theme.Equals("Light", StringComparison.OrdinalIgnoreCase))
            {
                Resources["BgBrush"] = new SolidColorBrush(Colors.White);
                Resources["FgBrush"] = new SolidColorBrush(Colors.Black);

                Resources["ControlBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(245, 245, 245));
                Resources["BorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(190, 190, 190));
                Resources["SeparatorBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(210, 210, 210));
                Resources["AccentBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 120, 215));
            }
            else
            {
                Resources["BgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(20, 20, 20));
                Resources["FgBrush"] = new SolidColorBrush(Colors.White);

                Resources["ControlBgBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30));
                Resources["BorderBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(68, 68, 68));
                Resources["SeparatorBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(64, 64, 64));
                Resources["AccentBrush"] = new SolidColorBrush(System.Windows.Media.Color.FromRgb(106, 160, 255));
            }
        }

        private readonly (string hex, string name)[] _palette = new[]
        {
            ("#DD111111","Dark"),
            ("#DD000000","Black"),
            ("#DD1E3A8A","Blue"),
            ("#DD0F766E","Teal"),
            ("#DD166534","Green"),
            ("#DD92400E","Orange"),
            ("#DD9F1239","Rose"),
            ("#DD6B21A8","Purple"),
            ("#DDA1A1AA","Gray"),
            ("#DDFFFFFF","White")
        };

        public SettingsWindow(AppSettings current, Action<AppSettings> liveApply, Action exitApp)
        {
            InitializeComponent();

            DarkModeCheck.IsChecked = !current.SettingsTheme.Equals("Light", StringComparison.OrdinalIgnoreCase);
            ApplyTheme(DarkModeCheck.IsChecked == true ? "Dark" : "Light");
            _original = current;
            Settings = current;
            _liveApply = liveApply;
            _exitApp = exitApp;

            OpacitySlider.Value = current.UiOpacity;
            UpdateOpacityPct();

            BuildPalette();
            HexText.Text = current.BackgroundHex;HexText.TextChanged += (_, __) =>
            {
                if (_closing) return;

                var hex = HexText.Text?.Trim();
                if (string.IsNullOrWhiteSpace(hex)) return;

                if (hex.StartsWith("#") && (hex.Length == 7 || hex.Length == 9))
                {
                    Settings = Settings with { BackgroundHex = hex };_liveApply(Settings);
                }
            };


            // labels
            LblFps.Text = current.LabelFps;
            LblCpuLoad.Text = current.LabelCpuLoad;
            LblCpuTemp.Text = current.LabelCpuTemp;
            LblCpuPower.Text = current.LabelCpuPower;
            LblGpuLoad.Text = current.LabelGpuLoad;
            LblGpuTemp.Text = current.LabelGpuTemp;
            LblGpuPower.Text = current.LabelGpuPower;
            LblVramUsed.Text = current.LabelVramUsed;
            LblRamUsed.Text = current.LabelRamUsed;
            LblRamFree.Text = current.LabelRamFree;
            LblDriveC.Text = current.LabelDriveC;
            LblDriveE.Text = current.LabelDriveE;
            LblInternet.Text = current.LabelInternet;

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
            CbDriveE.IsChecked = enabled.Contains("drive-e");
            CbInternet.IsChecked = enabled.Contains("internet");

            foreach (var id in current.MetricOrder)
                OrderList.Items.Add(ToDisplay(id));

            ApplyTheme(current.SettingsTheme);

            HookEvents();
            HookLabelEvents();
            HookMetricsEnableEvents();
        
            _initializing = false;
        }

        private void HookEvents()
        {
            OpacitySlider.ValueChanged += (_, __) =>
            {
                if (_closing) return;
                UpdateOpacityPct();
                Settings = Settings with { UiOpacity = OpacitySlider.Value };
                _liveApply(Settings);
            };
        }

        private void UpdateOpacityPct() => OpacityPct.Text = $"{(int)(OpacitySlider.Value * 100)}%";

        private void BuildPalette()
        {
            PalettePanel.Children.Clear();
            foreach (var (hex, name) in _palette)
            {
                var btn = new System.Windows.Controls.Button
                {
                    Width = 32,
                    Height = 22,
                    Margin = new Thickness(4, 0, 4, 6),
                    ToolTip = $"{name} ({hex})",
                    Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString(hex)!
                };
                btn.Click += (_, __) =>
                {
                    if (_closing) return;
                    Settings = Settings with { BackgroundHex = hex };
                    HexText.Text = hex;
                    _liveApply(Settings);
                };
                PalettePanel.Children.Add(btn);
            }
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
                LabelDriveE = LblDriveE.Text,
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
            hook(LblDriveE);
            hook(LblInternet);
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
            "drive-e" => "Drive E",
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
            "Drive E" => "drive-e",
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

        private void Theme_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_closing) return;
            ApplyTheme(Settings.SettingsTheme);
            Settings = Settings with { SettingsTheme = Settings.SettingsTheme };
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
            hook(CbDriveE);
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
            if (CbDriveE.IsChecked == true) list.Add("drive-e");
            if (CbInternet.IsChecked == true) list.Add("internet");
            if (list.Count == 0) list.Add("cpu-load");

            Settings = Settings with { EnabledMetrics = list.ToArray() };
            _liveApply(Settings);
        }
        private void DarkMode_Changed(object sender, RoutedEventArgs e)
        {
            if (_initializing) return;
            if (_closing) return;

            var newTheme = (DarkModeCheck.IsChecked == true) ? "Dark" : "Light";
            ApplyTheme(newTheme);

            Settings = Settings with { SettingsTheme = newTheme };
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
            try { _exitApp(); } catch { }
            try { Close(); } catch { }
        }
    }
}

using System;
using System.Drawing;
using System.Windows.Forms;

namespace TopMonitoring.App
{
    public sealed class TrayService : IDisposable
    {
        private readonly NotifyIcon _icon;

        public TrayService(
            Action showSettings,
            Action showBar,
            Action hideBar,
            Action exit,
            Func<bool> getAutoStart,
            Action<bool> setAutoStart)
        {
            _icon = new NotifyIcon
            {
                Text = "TopMonitoring",
                Icon = new Icon("TopMonitoring.ico"),
                Visible = true
            };

            var menu = new ContextMenuStrip();

            var showItem = new ToolStripMenuItem("Show Bar");
            showItem.Click += (_, __) => showBar();

            var hideItem = new ToolStripMenuItem("Hide Bar");
            hideItem.Click += (_, __) => hideBar();

            var settingsItem = new ToolStripMenuItem("Settings...");
            settingsItem.Click += (_, __) => showSettings();

            var autostartItem = new ToolStripMenuItem("Start with Windows") { Checked = getAutoStart() };
            autostartItem.Click += (_, __) =>
            {
                var newVal = !autostartItem.Checked;
                setAutoStart(newVal);
                autostartItem.Checked = getAutoStart();
            };

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (_, __) => exit();

            menu.Items.Add(showItem);
            menu.Items.Add(hideItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(settingsItem);
            menu.Items.Add(autostartItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(exitItem);

            _icon.ContextMenuStrip = menu;

            try
            {
                _icon.BalloonTipTitle = "TopMonitoring";
                _icon.BalloonTipText = "Running in tray. Use tray menu to Show Bar / Settings.";
                _icon.ShowBalloonTip(1500);
            }
            catch { }
            _icon.DoubleClick += (_, __) => showSettings();
        }

        public void Dispose()
        {
            _icon.Visible = false;
            _icon.Dispose();
        }
    }
}

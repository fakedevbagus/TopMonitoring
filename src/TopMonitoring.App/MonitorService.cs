using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace TopMonitoring.App
{
    public sealed record MonitorInfo(string Id, string DisplayName, Screen Screen);

    public static class MonitorService
    {
        public const string PrimaryId = "Primary";

        public static IReadOnlyList<MonitorInfo> GetMonitors()
        {
            var screens = Screen.AllScreens;
            var list = new List<MonitorInfo>
            {
                new MonitorInfo(PrimaryId, "Primary", Screen.PrimaryScreen ?? screens.First())
            };

            for (var i = 0; i < screens.Length; i++)
            {
                var s = screens[i];
                var label = $"Monitor {i + 1} ({s.Bounds.Width}x{s.Bounds.Height})";
                list.Add(new MonitorInfo(s.DeviceName, label, s));
            }

            return list;
        }

        public static Screen ResolveTarget(string? targetMonitor)
        {
            var screens = Screen.AllScreens;
            if (screens.Length == 0) return Screen.PrimaryScreen ?? Screen.FromPoint(new System.Drawing.Point(0, 0));

            if (string.IsNullOrWhiteSpace(targetMonitor) || targetMonitor.Equals(PrimaryId, StringComparison.OrdinalIgnoreCase))
                return Screen.PrimaryScreen ?? screens[0];

            var byName = screens.FirstOrDefault(s => s.DeviceName.Equals(targetMonitor, StringComparison.OrdinalIgnoreCase));
            if (byName != null) return byName;

            return Screen.PrimaryScreen ?? screens[0];
        }
    }
}

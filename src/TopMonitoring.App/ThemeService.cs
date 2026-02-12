using System;
using System.Linq;
using System.Windows;

namespace TopMonitoring.App
{
    public static class ThemeService
    {
        private const string DarkThemePath = "Themes/DarkTheme.xaml";
        private const string LightThemePath = "Themes/LightTheme.xaml";

        public static void ApplyTheme(string themeName)
        {
            var app = System.Windows.Application.Current;
            if (app == null) return;

            var target = themeName.Equals("Light", StringComparison.OrdinalIgnoreCase)
                ? LightThemePath
                : DarkThemePath;

            var existing = app.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Equals(target, StringComparison.OrdinalIgnoreCase));
            if (existing != null) return;

            var toRemove = app.Resources.MergedDictionaries
                .Where(d => d.Source != null && (d.Source.OriginalString.Contains("Themes/DarkTheme.xaml") || d.Source.OriginalString.Contains("Themes/LightTheme.xaml")))
                .ToList();

            foreach (var d in toRemove)
                app.Resources.MergedDictionaries.Remove(d);

            app.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri(target, UriKind.Relative)
            });
        }
    }
}

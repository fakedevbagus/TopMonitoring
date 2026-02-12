using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Threading.Tasks;
using TopMonitoring.Infrastructure;

namespace TopMonitoring.App
{
    public partial class App : System.Windows.Application
    {
        public static IHost? Host { get; private set; }
        private static System.Threading.Mutex? _singleInstanceMutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            _singleInstanceMutex = new System.Threading.Mutex(true, "TopMonitoring.SingleInstance", out var createdNew);
            if (!createdNew)
            {
                Shutdown();
                return;
            }

            var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TopMonitoring", "logs");
            Directory.CreateDirectory(logDir);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(Path.Combine(logDir, "app-.log"), rollingInterval: RollingInterval.Day)
                .CreateLogger();

            // Global exception logging (prevents silent exit)
            DispatcherUnhandledException += (_, ex) =>
            {
                Log.Error(ex.Exception, "DispatcherUnhandledException");
                ex.Handled = true;
            };

            AppDomain.CurrentDomain.UnhandledException += (_, ex) =>
            {
                Log.Fatal(ex.ExceptionObject as Exception, "UnhandledException");
            };

            TaskScheduler.UnobservedTaskException += (_, ex) =>
            {
                Log.Error(ex.Exception, "UnobservedTaskException");
                ex.SetObserved();
            };

            Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                .UseSerilog()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<SettingsService>();
                    services.AddSingleton<MonitoringService>();
                    services.AddSingleton<MainWindow>();
                })
                .Build();

            Host.Start();

            var mw = Host.Services.GetRequiredService<MainWindow>();
            mw.Show();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            try
            {
                if (Host != null)
                {
                    await Host.StopAsync(TimeSpan.FromSeconds(2));
                    Host.Dispose();
                }
            }
            catch { }
            finally
            {
                Log.CloseAndFlush();
                try
                {
                    _singleInstanceMutex?.ReleaseMutex();
                    _singleInstanceMutex?.Dispose();
                }
                catch { }
            }

            base.OnExit(e);
        }
    }
}

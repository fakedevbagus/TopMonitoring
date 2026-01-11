using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Threading.Tasks;

namespace TopMonitoring.App
{
    public partial class App : System.Windows.Application
    {
        public static IHost? Host { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
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
            }

            base.OnExit(e);
        }
    }
}

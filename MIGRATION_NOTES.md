# Migration Notes from original repo

- Configuration moved to AppSettings & ConfigStore (JSON in %AppData%).
- Providers are pluggable via IMetricProvider; avoid direct hardware calls from UI.
- UI now subscribes to MetricEngine instead of polling sensors directly.
- You will need to add real Sensor implementations (LibreHardwareMonitor) in TopMonitoring.Monitoring.

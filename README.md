# TopMonitoring

[![Build Status](https://img.shields.io/badge/build-passing-lightgrey?style=flat-square)](#)
[![.NET Version](https://img.shields.io/badge/.NET-8.0-512bd4?style=flat-square)](https://dotnet.microsoft.com)
[![Windows](https://img.shields.io/badge/Windows-10%2F11-0078d4?style=flat-square)](#)
[![License](https://img.shields.io/badge/license-MIT-green?style=flat-square)](LICENSE)

**TopMonitoring** is a lightweight Windows overlay that displays real-time system metrics in a compact top bar. It is optimized for stability, low CPU usage, and minimal distraction.

---

## Overview
- Real-time CPU/GPU/RAM/VRAM/Disk/Network monitoring
- Compact always-on-top overlay for gaming or productivity
- Designed for stability, low overhead, and safe UI updates

---

## Features
- Real-time CPU/GPU/RAM monitoring
- CPU Package temperature support
- Preset system: **Gaming**, **Work**, **Minimal**, **Custom**
- Dark & Light theme switching (safe DynamicResource usage)
- Click-through mode for unobstructed input
- Multi-monitor docking and target selection
- Alert thresholds with optional blink
- Settings export/import
- Global hotkeys

---

## Preset Behavior
- Built-in presets can be previewed safely.
- **Custom** preset activates only when the user edits settings manually.
- Presets do **not** automatically overwrite the user’s custom layout.

---

## Theme System
- Uses **DynamicResource** brushes for all UI colors.
- No ControlTemplate overrides for ComboBox.
- Dark & Light resource dictionaries are kept in sync.

---

## Performance Notes
- Fast vs. slow polling based on preset update interval.
- Delta filtering reduces unnecessary UI updates.
- Low CPU overhead and stable rendering loop.

---

## Known Limitations
- Some sensors require admin privileges.
- FPS accuracy depends on integration method.
- Hardware support varies by device and driver.

---

## Troubleshooting
- **Settings window not opening:** check the “SETTINGS ERROR” dialog for details.
- **ComboBox styling issues:** avoid custom ControlTemplate overrides.
- **Click-through blocking interaction:** toggle click-through hotkey or disable it in settings.
- **Corrupted settings file:** delete the settings JSON to regenerate defaults.

---

## Build & Run
```bash
dotnet restore
dotnet build -c Release
dotnet run --project src/TopMonitoring.App -c Release
```

---

## Development Notes
### Architecture
- **App layer:** UI, overlay, window lifecycle, and input handling.
- **Monitoring layer:** metric providers and polling engine.
- **Infrastructure layer:** settings, presets, and persistence.

### Preset Logic
- Presets apply for preview without forcing **Custom**.
- **Custom** activates only on manual user edits.

### Theme Rules
- Keep all brushes in DarkTheme and LightTheme.
- Use DynamicResource for all UI colors.

### ComboBox Styling
- Do not override ControlTemplate.
- Only set brushes and item styling.

### SettingsWindow Safety
- Heavy initialization runs in Loaded event.
- Exceptions are surfaced via MessageBox.

### Disposal & Lifecycle
- Unsubscribe from SystemEvents.
- Dispose timers, hotkeys, and monitoring services on shutdown.

---

## License
MIT License. See [LICENSE](LICENSE).

## Links
- [Changelog](CHANGELOG.md)
- [Code of Conduct](CODE_OF_CONDUCT.MD)
- [Security Policy](SECURITY.MD)

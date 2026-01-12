![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)
![Windows](https://img.shields.io/badge/Windows-10%2F11-0078D6?logo=windows&logoColor=white)
![WPF](https://img.shields.io/badge/WPF-Desktop-5C2D91?logo=windows&logoColor=white)
![C#](https://img.shields.io/badge/C%23-Programming-239120?logo=csharp&logoColor=white)
![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)

![Stars](https://img.shields.io/github/stars/fakedevbagus/Top-Monitoring?style=flat)
![Forks](https://img.shields.io/github/forks/fakedevbagus/Top-Monitoring?style=flat)
![Issues](https://img.shields.io/github/issues/fakedevbagus/Top-Monitoring)
![Last Commit](https://img.shields.io/github/last-commit/fakedevbagus/Top-Monitoring)

![Build](https://github.com/fakedevbagus/Top-Monitoring/actions/workflows/dotnet.yml/badge.svg)

---
# Top Monitoring

**Top Monitoring** is a lightweight Windows top-bar overlay to display real-time hardware metrics (CPU/GPU/RAM/VRAM/Disk/Network) using **LibreHardwareMonitor** + **WPF**.

Designed for minimal distraction: always-on-top, compact, customizable, and fast.

> Repo: https://github.com/fakedevbagus/TopMonitoring

---

## ✨ Features

### Overlay / Dock
- **Always-on-top top dock** (overlay)
- Transparent dock background support
- **Fixed metric order** (MSI Afterburner-style layout)
- Metric value alignment centered (clean look)
- Auto N/A fallback when metric not available

### Metrics (Real-time)
- FPS *(depends on source / limitations explained below)*
- CPU Load / Temp / Power
- GPU Load / Temp / Power *(GPU power depends on hardware/driver)*
- VRAM Used
- RAM Used / RAM Free
- Disk C / Disk E usage
- Network (Down/Up)

### Settings
- **Dark/Light mode toggle**
- Background color palette + **HEX input** (custom color)
- Opacity slider with percentage indicator
- Metric label renaming (prefix text)
- **Enable/Disable metrics (realtime + autosaved)**
- Metric order editor (Up/Down reorder)

### Quality
- Stable, optimized update loop
- Autosave settings (no “Save” button needed)
- Safe fallback: missing sensor = `N/A`

---

## 📸 Screenshots

<img width="1920" height="104" alt="dock" src="https://github.com/user-attachments/assets/6a92f60d-2f06-401e-b428-311eec81d4ea" />
<img width="1066" height="698" alt="darktheme" src="https://github.com/user-attachments/assets/69fc692f-3802-4767-9224-43ca3260104d" />
<img width="1064" height="680" alt="lighttheme" src="https://github.com/user-attachments/assets/ae3c3249-69e1-4d8d-95bf-f841e25fb9cb" />

---

## ✅ Requirements
- Windows 10/11
- .NET SDK 8 (for building from source)
- LibreHardwareMonitor (already included as dependency)

---

## 🚀 Build & Run (Developer)

### 1) Clone
```bash
git clone https://github.com/fakedevbagus/TopMonitoring.git
cd TopMonitoring
```

### 2) Restore & Build
```bash
dotnet restore
dotnet build -c Release
```

### 3) Run
```bash
dotnet run --project src/TopMonitoring.App -c Release
```

---

## ⚙️ Configuration / Settings
All settings are saved automatically (no manual save).

Settings include:
- Theme (Dark/Light)
- Opacity
- Background Color (palette + HEX)
- Metric Labels (prefix)
- Enabled metrics
- Metric order

---

## 🧠 Notes / Limitations (Important)

### FPS
FPS is **not always reliable** because Windows does not provide a universal system-wide FPS counter by default.
For accurate FPS like MSI Afterburner/RivaTuner, a dedicated overlay/hook method is required.

### GPU Power
GPU Power read depends on:
- GPU model
- driver support
- exposed sensors from LibreHardwareMonitor

Some GPUs may return N/A.

---

## ✅ Advantages
- Lightweight WPF overlay
- Clean centered UI layout
- Fully customizable labels and ordering
- Real-time enable/disable metrics
- Dark/Light theme support
- Autosave settings

## ⚠️ Known Drawbacks
- FPS accuracy depends on source availability
- Some GPU metrics depend on vendor sensor availability
- Only built for Windows (WPF)
- if you close the app and window other apps won't fullscreen after that, you should restart File Explorer in the taskbar     find the File Explorer-right click-restart-done

---

## 🛠 Roadmap
Planned improvements:
- Export portable single-file `.exe` release (Publish)
- Better FPS support (optional plugin / overlay integration)
- Multi-monitor docking and edge snapping
- Presets (Gaming / Work / Minimal)
- Click-through mode

---

## 🤝 Contributing
PRs and issues are welcome.
Please:
- use clear PR title
- include screenshots for UI changes
- keep code style consistent

See: [CONTRIBUTING.md](CONTRIBUTING.md)

---

## 📄 License
MIT License — free to use, modify, and distribute.

📚 Documentation: https://fakedevbagus.github.io/TopMonitoring/

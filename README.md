# TibiaSmartScreen

A production-ready Windows screen-overlay application built with **C# / WPF / .NET 8** and MVVM architecture. It mirrors selected regions of your screen into movable, resizable, borderless overlay windows — great for gamers, streamers, or anyone who needs a multi-region picture-in-picture setup.

---

## Features

| Feature | Detail |
|---|---|
| **Screen Capture** | GDI+ `CopyFromScreen` with bitmap reuse — targets 30–60 FPS with low CPU overhead |
| **Overlay windows** | Borderless, transparent, always-on-top; drag title-bar to move |
| **8-direction resize** | Handles on all corners and edges; smooth resize with cursor feedback |
| **Click-through mode** | Win32 `WS_EX_TRANSPARENT` — when locked the overlay is fully interactive with windows below |
| **Lock/Unlock** | Per-overlay or global lock; locks propagate to Win32 layer immediately |
| **Opacity control** | Global slider (5 % → 100 %); applies to all overlays in real time |
| **FPS limiter** | Slider 5–60 fps; updates all overlay timers dynamically |
| **Region selector** | Fullscreen dark overlay + drag-to-select with live dimension indicator |
| **Layout persistence** | JSON save/load in `%APPDATA%\TibiaSmartScreen\Layouts\` |
| **Session restore** | Last layout reloads automatically on startup |
| **Global hotkeys** | F9 = Add Region, F10 = Lock/Unlock All, F11 = Hide/Show All |
| **Multi-monitor / DPI** | `PerMonitorV2` DPI-aware; coordinate mapping via `PresentationSource` transform |
| **Dark theme** | `#0D0D0D` background · `#FF1A1A` red accent · rounded corners |
| **MVVM** | `ViewModelBase`, `RelayCommand`, full data binding |

---

## Requirements

- Windows 10 / 11
- [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

---

## Build

```bash
dotnet build -c Release
```

or open `TibiaSmartScreen.csproj` in Visual Studio 2022 (v17.8+).

---

## Project Structure

```
TibiaSmartScreen/
├── Models/
│   ├── ScreenRegion.cs      # Region data (capture bounds + overlay position)
│   ├── AppLayout.cs         # Named collection of regions
│   └── AppSettings.cs       # Persisted user preferences
├── Services/
│   ├── ScreenCaptureService.cs  # High-perf GDI+ capture, bitmap reuse
│   └── LayoutService.cs         # JSON save/load
├── ViewModels/
│   ├── ViewModelBase.cs     # INotifyPropertyChanged base
│   ├── RelayCommand.cs      # ICommand implementation
│   ├── MainViewModel.cs     # Main window state + layout management
│   └── OverlayViewModel.cs  # Per-overlay capture timer + state
├── Views/
│   ├── MainWindow.xaml/.cs       # Control panel
│   ├── OverlayWindow.xaml/.cs    # Capture display window
│   └── RegionSelectorWindow.xaml/.cs  # Fullscreen region picker
├── Infrastructure/
│   └── Win32Api.cs          # SetWindowLong, hotkey registration
└── Themes/
    └── DarkTheme.xaml       # Colour palette + control styles
```

---

## Licence

MIT

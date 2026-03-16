# PhotoBooth

A photobooth application available as both a **web app (PWA)** and a **cross-platform desktop app (Avalonia)**.

Select a camera, see a live preview, press the button to start a 3-second countdown, and capture a photo. The image is saved automatically and optionally sent to the printer.


## Desktop App (Avalonia)

A cross-platform .NET desktop application in the `PhotoBoothApp/` folder.

### Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (or later)
- A webcam

### Quick Start

```bash
cd PhotoBoothApp
dotnet run
```

### Build

```bash
cd PhotoBoothApp
dotnet build -c Release
```

The output binary is in `PhotoBoothApp/bin/Release/net10.0/`.

### Tech Stack

| Component | Technology |
|---|---|
| UI framework | Avalonia 11 |
| Camera capture | OpenCvSharp4 |
| MVVM toolkit | CommunityToolkit.Mvvm |
| Target framework | .NET 10 |

### Features

- Camera selection (probes available devices on startup)
- Live camera preview via OpenCvSharp frame capture
- 3-second countdown with glow effect
- Flash overlay on capture
- Saves photos to the system Pictures folder as `photobooth-{timestamp}.png`
- Toggle to enable/disable the print dialog
- Responsive layout — works at any window size
- Cross-platform print support (Windows, Linux, macOS)

### Project Structure

```
PhotoBoothApp/
  Program.cs                          # Entry point
  App.axaml / App.axaml.cs            # Application shell
  Views/
    MainWindow.axaml / .axaml.cs      # UI layout
  ViewModels/
    ViewModelBase.cs                  # Base MVVM class
    MainWindowViewModel.cs            # App logic and state
  Services/
    CameraService.cs                  # Camera enumeration and frame capture
    PrintService.cs                   # Cross-platform print invocation
```

---

## License

MIT

# Wisp2Win

Native Windows dictation powered by local Whisper models.

Wisp2Win is a Windows-native version of the macOS dictation workflow:

1. Press the global hotkey (`\` by default).
2. Dictate.
3. Press the hotkey again.
4. The transcript is pasted into the active application.

The app does not require Python, Homebrew-style tooling, or a manually installed Whisper binary. On first launch it downloads the selected Whisper model into `%LOCALAPPDATA%\Wisp2Win\Models`.

## Current Status

This is an initial Windows scaffold:

- WPF desktop app targeting `net8.0-windows`.
- Tray icon with quick actions.
- Global hotkey via Win32 `RegisterHotKey`.
- Microphone recording via NAudio.
- Local transcription via `Whisper.net` and bundled `Whisper.net.Runtime`.
- Model auto-download from the official `whisper.cpp` Hugging Face model bucket.
- Clipboard insertion via Win32 `SendInput`.

## Build

Requirements:

- Windows 10/11 x64
- .NET 8 SDK

```powershell
dotnet restore
dotnet build -c Release
```

Publish a self-contained executable folder:

```powershell
dotnet publish .\src\Wisp2Win\Wisp2Win.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Installer

The `installer/wisp2win.iss` script is intended for Inno Setup. Build the app first, then compile the installer script on Windows.

## Model Storage

Models are stored under:

```text
%LOCALAPPDATA%\Wisp2Win\Models
```

The default model is `base`, which is a practical starting point for dictation. Users can switch to `small` or `medium` for better quality at the cost of download size and CPU time.

## Roadmap

- Hotkey editor in the settings window.
- Startup with Windows option.
- Real-time recording level HUD.
- Model checksum verification.
- Signed installer and auto-update channel.

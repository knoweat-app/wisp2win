# Wisp2Win

Native local dictation for Windows and macOS, powered by Whisper models.

Wisp2Win started as a Windows-native version of the macOS dictation workflow and now ships both platform apps from one repository:

1. Press the global hotkey.
2. Dictate.
3. Press the hotkey again.
4. The transcript is pasted into the active application.

The apps do not require Python, Homebrew-style tooling, or a manually installed Whisper binary. On first launch they download the selected Whisper model into the platform model directory.
Speaker diarization is not advertised as a Whisper-only feature; local diarization remains an experimental research track documented in `docs/diarization-experiment.md`.

## Current Status

Release `v0.4.0` publishes Windows and macOS artifacts:

- `Wisp2Win-Setup-v0.4.0-win-x64.exe`
- `Wisp2Win-Portable-v0.4.0-win-x64.zip`
- `Wisp2Mac-v0.4.0-macos-universal.zip`
- `Wisp2Mac-v0.4.0-macos-universal.tar.gz`

Windows:

- WPF desktop app targeting `net8.0-windows`.
- Tray icon with quick actions.
- Configurable global hotkey via Win32 `RegisterHotKey`.
- Microphone recording via NAudio.
- Local transcription via `Whisper.net` and bundled `Whisper.net.Runtime`.
- Audio file import for plain local transcription.
- TXT export for the latest transcript.
- Model auto-download from the official `whisper.cpp` Hugging Face model bucket.
- Clipboard insertion via Win32 `SendInput`.
- Direct text typing fallback for apps that reject clipboard paste.
- Optional offline transcript polishing for punctuation and sentence casing.

macOS:

- Native Swift/SwiftUI menu bar app.
- Configurable global hotkey.
- Microphone recording and local transcription through bundled `whisper-cli`.
- Model auto-download with install status in the UI.
- Audio file import for plain local transcription.
- TXT export for the latest transcript.
- Clipboard paste and direct text typing modes.
- Recording overlay, logs, and settings window.
- Ad-hoc codesigned app bundle so Gatekeeper can show "Open Anyway".

## Build

Windows requirements:

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

macOS requirements:

- macOS 13+
- Xcode command line tools

Build the macOS app bundle:

```bash
./macos/build_whisper.sh
./macos/package_app.sh
```

The macOS bundle includes its own Whisper runtime under `Contents/Resources/whisper` so end users install only one app. The build script currently pins `whisper.cpp` to `v1.7.6` for reproducible CI builds. Full Developer ID signing and notarization are still future work.

## Installer

The `installer/wisp2win.iss` script is intended for Inno Setup. GitHub Actions builds the Windows installer and uploads it to the matching GitHub release.

## Model Storage

Windows models are stored under:

```text
%LOCALAPPDATA%\Wisp2Win\Models
```

macOS models are stored under:

```text
~/Library/Application Support/Wisp2Win/Models
```

The default model is `small`, which gives noticeably better Russian dictation quality than `base` at the cost of a larger first download. Users can switch to `tiny` or `base` for faster setup, or `medium` for higher quality with more CPU and disk usage.

## Roadmap

- Startup with Windows option.
- Real-time recording level HUD.
- Model checksum verification.
- Signed installer and auto-update channel.
- Developer ID signed and notarized macOS DMG.
- Shared `Wisp2Core` for common model metadata and transcript polishing.
- Experimental local speaker diarization research path.

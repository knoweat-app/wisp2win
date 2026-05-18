# Release Test Checklist

Use this checklist before promoting a Wisp2Win/Wisp2Mac release as the current recommended build.

## Windows

- Download the latest `Wisp2Win-Setup-vX.Y.Z-win-x64.exe` from GitHub Releases.
- Install on Windows 10/11 x64 without a local development checkout.
- Launch from the Start menu.
- Download `Wisp2Win-Portable-vX.Y.Z-win-x64.zip`, extract it into a user-writable folder, and launch `Wisp2Win.exe` without running the installer.
- Confirm the tray icon appears.
- Confirm the selected model downloads into `%LOCALAPPDATA%\Wisp2Win\Models`.
- Start and stop dictation with the configured global hotkey.
- Confirm transcript insertion in a normal text field.
- Confirm insertion behavior in Termius or another terminal-like app.
- Check that the direct typing fallback works when clipboard paste is unreliable.
- Open logs from the UI and confirm useful startup, recording, model, transcription, and insertion entries.

## macOS

- Download the latest `Wisp2Mac-vX.Y.Z-macos-universal.zip` from GitHub Releases.
- If Archive Utility cannot expand the zip, test `Wisp2Mac-vX.Y.Z-macos-universal.tar.gz`.
- Extract and move `Wisp2Mac.app` to `/Applications`.
- Launch the app on macOS 13+.
- If Gatekeeper blocks the first launch, use System Settings or the context menu flow to allow opening.
- Grant Microphone permission when prompted.
- Grant Accessibility permission for text insertion.
- Confirm the menu bar item appears.
- Confirm the selected model downloads into `~/Library/Application Support/Wisp2Win/Models`.
- Start and stop dictation with the configured global hotkey.
- Confirm transcript insertion in a normal text field.
- Confirm insertion behavior in Terminal, iTerm2, Warp, VS Code terminal, or Termius.
- Check that direct typing mode works when clipboard paste is unreliable.
- Open logs from the UI and confirm useful startup, recording, model, transcription, and insertion entries.

## Release Metadata

- Confirm `.github/workflows/build.yml`, `src/Wisp2Win/Wisp2Win.csproj`, `installer/wisp2win.iss`, and `macos/package_app.sh` all use the intended version.
- Confirm the latest GitHub Actions run on `main` is green.
- Confirm the GitHub release contains the Windows installer, Windows portable zip, macOS zip, and macOS tar.gz assets.
- Confirm README release notes match the published artifacts.

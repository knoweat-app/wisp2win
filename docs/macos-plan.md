# Wisp2Win macOS Plan

Goal: ship a macOS-native dictation app in the same repository with the same user workflow:

1. Press a global hotkey.
2. Record audio.
3. Transcribe with a locally downloaded Whisper model.
4. Insert text into the previously active application.

The macOS app should not require Python, Homebrew, a manually installed Whisper binary, or command-line setup.

## Product Scope

The first macOS version should match the current Windows feature set:

- Local Whisper transcription.
- Model selector with automatic model downloads.
- Global hotkey.
- Menu bar app behavior.
- Last transcript view.
- Insert methods equivalent to Windows:
  - normal paste through clipboard;
  - direct typing fallback for apps that reject clipboard paste.
- Offline transcript polishing.
- File logging and an "Open logs" action.

## Repository Layout

Keep one repo and split platform apps from shared domain logic:

```text
src/
  Wisp2Win/              Windows WPF app
  Wisp2Core/             shared model catalog, settings contracts, post-processing
  Wisp2Mac/              macOS native app
installer/
  wisp2win.iss
macos/
  packaging/
docs/
  macos-plan.md
```

`Wisp2Core` should stay UI-free. It can contain:

- `ModelProfile`
- model download and validation logic
- transcript post-processing
- settings schema
- log path conventions

Platform-specific code should remain in each app:

- audio capture
- hotkeys
- active window tracking
- insertion strategy
- installer/package creation
- tray/menu bar UI

## macOS Technology Choice

Recommended implementation:

- Swift + SwiftUI for the app UI.
- AppKit for menu bar behavior, global hotkeys, Accessibility APIs, and insertion.
- whisper.cpp native library or a Swift wrapper around whisper.cpp.
- Sparkle later for auto-update, not in the first build.

Reasoning:

- macOS permissions, hotkeys, and Accessibility insertion are AppKit-native problems.
- SwiftUI is enough for settings and transcript UI.
- A .NET MAUI app would add complexity around hotkeys, Accessibility permissions, packaging, and notarization without giving much benefit here.

## Model Storage

Use:

```text
~/Library/Application Support/Wisp2Win/Models
```

Logs:

```text
~/Library/Logs/Wisp2Win/wisp2win.log
```

Settings:

```text
~/Library/Application Support/Wisp2Win/settings.json
```

Use the same model IDs as Windows:

- `tiny`
- `base`
- `small`
- `medium`

A later release can add `large-v3-turbo` if CPU performance is acceptable.

## Audio Capture

Use `AVAudioEngine` or `AVAudioRecorder`.

Required permission:

- Microphone

First-run behavior:

- Request microphone permission.
- If denied, show a compact settings state with a button that opens macOS Privacy settings.

## Global Hotkey

Use a small AppKit bridge:

- `RegisterEventHotKey` for global hotkeys, or
- an event tap if we need more flexibility later.

Required behavior:

- Same start/stop toggle as Windows.
- Detect hotkey conflicts.
- Store selected hotkey in settings.

macOS-specific constraints:

- Avoid default shortcuts that collide with Spotlight, input sources, Mission Control, screenshot tools, or app menu shortcuts.
- The first build uses `Ctrl+Shift+Space` because it is close to the Windows workflow and usually less overloaded than `Cmd+Space` or `Ctrl+Space`.
- A Shortcuts.app integration should be added as a separate entry point later, so users can trigger dictation from Apple Shortcuts without replacing the native global hotkey.

## Active App Tracking

Use `NSWorkspace.shared.frontmostApplication` before recording starts.

Capture:

- bundle identifier
- localized app name
- process ID

This replaces the Windows `WindowTargetService`.

## Text Insertion

Two insertion strategies are needed.

### Clipboard Paste

Use `NSPasteboard.general`:

1. Save previous clipboard content when practical.
2. Put transcript as plain text.
3. Reactivate target app.
4. Send `Cmd+V` through Accessibility/CGEvent.
5. Restore clipboard after a short delay when safe.

Required permission:

- Accessibility

### Direct Typing

Use `CGEventKeyboardSetUnicodeString` for apps where clipboard paste fails.

This should mirror Windows `Type text` mode and be available as:

- `Auto`
- `Cmd+V`
- `Type text`

For terminal apps, `Auto` should probably prefer `Type text` at first:

- Termius
- Terminal
- iTerm2
- Warp
- VS Code terminal

## Transcript Polishing

Port the current offline post-processor into `Wisp2Core` first, then reuse it from both apps.

Keep it optional:

- on by default for dictation text;
- easy to disable for shell commands.

Future improvement:

- optional LLM-based polishing provider;
- terminal-safe polishing mode that avoids adding final punctuation.

## Packaging

First build:

- `.app` bundle
- zipped artifact from GitHub Actions
- `whisper-cli` must be bundled under `Contents/Resources/whisper/whisper-cli`; users must not install Homebrew, Python, or a separate Whisper binary.

Production build:

- signed `.app`
- notarized `.dmg`

GitHub Actions:

- add a macOS job beside the Windows job;
- upload `Wisp2Mac.app.zip`;
- attach it to the same GitHub release tag.

Release naming:

```text
Wisp2Win-Setup-vX.Y.Z-win-x64.exe
Wisp2Mac-vX.Y.Z-macos-universal.zip
```

## Implementation Phases

### Phase 1: Shared Core

- Create `src/Wisp2Core`.
- Move model profiles and transcript polishing into shared code.
- Keep Windows behavior unchanged.

### Phase 2: macOS Prototype

- Menu bar Swift app.
- Model download.
- Microphone recording to WAV.
- Local transcription with whisper.cpp.
- Manual start/stop button.

### Phase 3: Native Workflow

- Global hotkey.
- Frontmost app capture.
- Clipboard paste and direct typing.
- Logs and settings window.

### Phase 4: Packaging

- GitHub Actions macOS build.
- Release artifacts in the existing repo.
- Signing and notarization once Apple Developer account credentials are available.

## Open Questions

- Apple Developer account availability for signing and notarization.
- Whether the first macOS build should be Intel, Apple Silicon, or universal.
- Whether model download URLs should remain direct Hugging Face links or go through a project-owned manifest.
- Whether to add `large-v3-turbo` before or after macOS support.

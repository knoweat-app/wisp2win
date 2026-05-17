#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PACKAGE_DIR="$ROOT_DIR/src/Wisp2Mac"
APP_DIR="$ROOT_DIR/macos/output/Wisp2Mac.app"
CONTENTS_DIR="$APP_DIR/Contents"
MACOS_DIR="$CONTENTS_DIR/MacOS"
RESOURCES_DIR="$CONTENTS_DIR/Resources"
APP_BIN="$MACOS_DIR/Wisp2Mac"
VERSION="${WISP_VERSION:-0.3.1}"

find_swift_binary() {
  local arch="$1"
  local preferred="$PACKAGE_DIR/.build/${arch}-apple-macosx/release/Wisp2Mac"

  if [ -f "$preferred" ]; then
    printf '%s\n' "$preferred"
    return 0
  fi

  find "$PACKAGE_DIR/.build" -path "*/release/Wisp2Mac" -type f | grep "$arch" | head -n 1 || true
}

rm -rf "$APP_DIR"
mkdir -p "$MACOS_DIR" "$RESOURCES_DIR/whisper"

if command -v lipo >/dev/null 2>&1; then
  swift build --package-path "$PACKAGE_DIR" -c release --arch arm64
  swift build --package-path "$PACKAGE_DIR" -c release --arch x86_64
  ARM64_BIN="$(find_swift_binary arm64)"
  X64_BIN="$(find_swift_binary x86_64)"

  if [ -z "$ARM64_BIN" ] || [ -z "$X64_BIN" ]; then
    echo "SwiftPM did not produce both arm64 and x86_64 binaries" >&2
    find "$PACKAGE_DIR/.build" -path "*/release/Wisp2Mac" -type f >&2
    exit 1
  fi

  lipo -create \
    "$ARM64_BIN" \
    "$X64_BIN" \
    -output "$APP_BIN"
else
  swift build --package-path "$PACKAGE_DIR" -c release
  cp "$PACKAGE_DIR/.build/release/Wisp2Mac" "$APP_BIN"
fi

chmod +x "$APP_BIN"

if [ -f "$ROOT_DIR/macos/whisper/whisper-cli" ]; then
  # Copy whisper-cli and any bundled dylibs (ggml backends) into the app bundle.
  cp -R "$ROOT_DIR/macos/whisper/." "$RESOURCES_DIR/whisper/"
  chmod +x "$RESOURCES_DIR/whisper/whisper-cli"
else
  echo "Missing macos/whisper/whisper-cli. Run ./macos/build_whisper.sh before packaging." >&2
  exit 1
fi

cat > "$CONTENTS_DIR/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleExecutable</key>
  <string>Wisp2Mac</string>
  <key>CFBundleIdentifier</key>
  <string>app.knoweat.wisp2mac</string>
  <key>CFBundleName</key>
  <string>Wisp2Mac</string>
  <key>CFBundleDisplayName</key>
  <string>Wisp2Mac</string>
  <key>CFBundlePackageType</key>
  <string>APPL</string>
  <key>CFBundleShortVersionString</key>
  <string>$VERSION</string>
  <key>CFBundleVersion</key>
  <string>$VERSION</string>
  <key>LSMinimumSystemVersion</key>
  <string>13.0</string>
  <key>NSMicrophoneUsageDescription</key>
  <string>Wisp2Mac записывает голос для локальной диктовки.</string>
  <key>NSAppleEventsUsageDescription</key>
  <string>Wisp2Mac возвращает фокус в активное приложение после диктовки.</string>
  <key>NSInputMonitoringUsageDescription</key>
  <string>Wisp2Mac отслеживает нажатие горячей клавиши для запуска диктовки.</string>
</dict>
</plist>
PLIST

# Ad-hoc signature so Gatekeeper shows "Open Anyway" instead of "cannot verify".
# Full notarization requires a paid Apple Developer ID certificate.
codesign --force --deep --sign - "$APP_DIR"

ditto -c -k --sequesterRsrc --keepParent "$APP_DIR" "$ROOT_DIR/macos/output/Wisp2Mac-v$VERSION-macos-universal.zip"

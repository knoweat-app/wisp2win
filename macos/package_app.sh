#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PACKAGE_DIR="$ROOT_DIR/src/Wisp2Mac"
BUILD_DIR="$PACKAGE_DIR/.build/release"
APP_DIR="$ROOT_DIR/macos/output/Wisp2Mac.app"
CONTENTS_DIR="$APP_DIR/Contents"
MACOS_DIR="$CONTENTS_DIR/MacOS"
RESOURCES_DIR="$CONTENTS_DIR/Resources"

rm -rf "$APP_DIR"
mkdir -p "$MACOS_DIR" "$RESOURCES_DIR/whisper"

swift build --package-path "$PACKAGE_DIR" -c release
cp "$BUILD_DIR/Wisp2Mac" "$MACOS_DIR/Wisp2Mac"
chmod +x "$MACOS_DIR/Wisp2Mac"

if [ -f "$ROOT_DIR/macos/whisper/whisper-cli" ]; then
  cp "$ROOT_DIR/macos/whisper/whisper-cli" "$RESOURCES_DIR/whisper/whisper-cli"
  chmod +x "$RESOURCES_DIR/whisper/whisper-cli"
fi

cat > "$CONTENTS_DIR/Info.plist" <<'PLIST'
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
  <string>0.3.0</string>
  <key>CFBundleVersion</key>
  <string>0.3.0</string>
  <key>LSMinimumSystemVersion</key>
  <string>13.0</string>
  <key>NSMicrophoneUsageDescription</key>
  <string>Wisp2Mac записывает голос для локальной диктовки.</string>
  <key>NSAppleEventsUsageDescription</key>
  <string>Wisp2Mac возвращает фокус в активное приложение после диктовки.</string>
</dict>
</plist>
PLIST

ditto -c -k --sequesterRsrc --keepParent "$APP_DIR" "$ROOT_DIR/macos/output/Wisp2Mac-v0.3.0-macos.zip"

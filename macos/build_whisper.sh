#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
WHISPER_REF="${WHISPER_CPP_REF:-v1.7.6}"
WORK_DIR="${WHISPER_BUILD_WORK_DIR:-$ROOT_DIR/macos/.whisper-build}"
SOURCE_DIR="$WORK_DIR/whisper.cpp"
BUILD_DIR="$WORK_DIR/build"
OUTPUT_DIR="$ROOT_DIR/macos/whisper"
OUTPUT_BIN="$OUTPUT_DIR/whisper-cli"

rm -rf "$WORK_DIR"
mkdir -p "$WORK_DIR" "$OUTPUT_DIR"

git clone --depth 1 --branch "$WHISPER_REF" https://github.com/ggml-org/whisper.cpp.git "$SOURCE_DIR"

cmake -S "$SOURCE_DIR" -B "$BUILD_DIR" \
  -DCMAKE_BUILD_TYPE=Release \
  -DCMAKE_OSX_ARCHITECTURES="arm64;x86_64" \
  -DWHISPER_BUILD_TESTS=OFF \
  -DWHISPER_BUILD_EXAMPLES=ON

cmake --build "$BUILD_DIR" --config Release --target whisper-cli --parallel

if [ -f "$BUILD_DIR/bin/whisper-cli" ]; then
  cp "$BUILD_DIR/bin/whisper-cli" "$OUTPUT_BIN"
elif [ -f "$BUILD_DIR/examples/cli/whisper-cli" ]; then
  cp "$BUILD_DIR/examples/cli/whisper-cli" "$OUTPUT_BIN"
else
  echo "whisper-cli was not produced by the whisper.cpp build" >&2
  find "$BUILD_DIR" -name 'whisper-cli' -type f >&2
  exit 1
fi

chmod +x "$OUTPUT_BIN"
file "$OUTPUT_BIN"

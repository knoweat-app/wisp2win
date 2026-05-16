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
  -DBUILD_SHARED_LIBS=OFF \
  -DGGML_METAL_EMBED_LIBRARY=ON \
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

# ggml backends are often still produced as dylibs even with BUILD_SHARED_LIBS=OFF.
# Bundle every non-system .dylib from the build tree alongside whisper-cli so the
# app bundle is self-contained, then rewrite the load paths to @loader_path so the
# OS finds them relative to whisper-cli (in Resources/whisper/).

echo "Scanning for dylib dependencies..."

# Copy all dylibs produced by the build into the output dir.
find "$BUILD_DIR" -name "*.dylib" -type f | while read -r dylib; do
  libname="$(basename "$dylib")"
  dest="$OUTPUT_DIR/$libname"
  echo "  bundling $libname"
  cp "$dylib" "$dest"
  chmod 755 "$dest"
  # Change the dylib's own ID so it knows it lives at @loader_path.
  install_name_tool -id "@loader_path/$libname" "$dest"
done

# Rewrite @rpath references in whisper-cli to @loader_path.
rewrite_rpath_refs() {
  local binary="$1"
  otool -L "$binary" | awk '{print $1}' | grep '^@rpath/' | while read -r dep; do
    local libname="${dep#@rpath/}"
    if [ -f "$OUTPUT_DIR/$libname" ]; then
      install_name_tool -change "$dep" "@loader_path/$libname" "$binary"
      echo "  rewrote $dep → @loader_path/$libname in $(basename "$binary")"
    fi
  done
}

rewrite_rpath_refs "$OUTPUT_BIN"

# Also rewrite cross-references inside the bundled dylibs themselves.
for dylib in "$OUTPUT_DIR"/*.dylib; do
  [ -f "$dylib" ] || continue
  rewrite_rpath_refs "$dylib"
done

file "$OUTPUT_BIN"
echo ""
echo "whisper output dir:"
ls -lh "$OUTPUT_DIR"

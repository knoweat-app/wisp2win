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

# ggml v1.7.6 may still produce backend dylibs even with BUILD_SHARED_LIBS=OFF.
# Bundle every .dylib from the build tree alongside whisper-cli and rewrite load
# paths to @loader_path so dyld finds them relative to the binary at runtime.

echo "Scanning for dylib dependencies..."

# Copy every dylib produced by cmake into the output dir and fix its own ID.
find "$BUILD_DIR" -name "*.dylib" -type f | while read -r dylib; do
  libname="$(basename "$dylib")"
  dest="$OUTPUT_DIR/$libname"
  echo "  bundling $libname"
  cp "$dylib" "$dest"
  chmod 755 "$dest"
  install_name_tool -id "@loader_path/$libname" "$dest"
done

# Rewrite @rpath/libX references in a binary to @loader_path/libX.
# Uses a subshell variable to avoid the pipefail-from-grep-no-match trap.
rewrite_rpath_refs() {
  local binary="$1"
  local rpath_deps
  rpath_deps="$(otool -L "$binary" | awk '{print $1}' | grep '^@rpath/' || true)"
  [ -z "$rpath_deps" ] && return 0
  while IFS= read -r dep; do
    local libname="${dep#@rpath/}"
    if [ -f "$OUTPUT_DIR/$libname" ]; then
      install_name_tool -change "$dep" "@loader_path/$libname" "$binary"
      echo "  rewrote $dep → @loader_path/$libname in $(basename "$binary")"
    else
      echo "  WARNING: no bundled dylib for $dep" >&2
    fi
  done <<< "$rpath_deps"
}

rewrite_rpath_refs "$OUTPUT_BIN"

for dylib in "$OUTPUT_DIR"/*.dylib; do
  [ -f "$dylib" ] || continue
  rewrite_rpath_refs "$dylib"
done

file "$OUTPUT_BIN"
echo ""
echo "whisper output dir:"
ls -lh "$OUTPUT_DIR"

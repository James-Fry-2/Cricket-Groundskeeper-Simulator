#!/usr/bin/env bash
# Builds the playtest for macOS (Apple silicon and Intel) and Windows, each self-contained in
# one zip with the content, a README for its platform and the quick-start guide.
set -euo pipefail

root="$(cd "$(dirname "$0")/.." && pwd)"
version="$(git -C "$root" describe --tags --always --dirty)"
if [[ "$version" == *-dirty ]]; then
    echo "Warning: building from uncommitted changes ($version). Commit first for a build testers can be traced to." >&2
fi
dist="$root/dist"
rm -rf "$dist"
mkdir -p "$dist"

publish() {
    local rid="$1" label="$2" readme="$3"
    local name="CricketGroundsman-$version-$label"
    local out="$dist/$name"

    dotnet publish "$root/src/Groundsman.Cli" -c Release -r "$rid" --self-contained \
        -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \
        -p:DebugType=none -p:InformationalVersion="$version" \
        -o "$out" > "$dist/$name.log"

    cp "$root/docs/playtest/$readme" "$out/README.txt"
    cp "$root/docs/playtest/guide.md" "$out/Guide.md"
    # The harness's stand-in scoring isn't part of the game.
    rm -f "$out/content/scoring.json"
    (cd "$dist" && zip -qr "$name.zip" "$name")
    echo "$dist/$name.zip"
}

publish osx-arm64 macos-apple-silicon readme-macos.txt
publish osx-x64 macos-intel readme-macos.txt
publish win-x64 windows readme-windows.txt

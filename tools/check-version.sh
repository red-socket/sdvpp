#!/usr/bin/env bash
# Verify the three version fields agree and are plain N.N.N (no pre-release suffix).
set -euo pipefail
cd "$(dirname "$0")/.."
csproj=$(sed -n 's|.*<Version>\(.*\)</Version>.*|\1|p' src/SDVPP/SDVPP.csproj)
cs=$(python3 -c 'import json;print(json.load(open("src/SDVPP/manifest.json"))["Version"])')
cp=$(python3 -c 'import json;print(json.load(open("src/SDVPP.Content/manifest.json"))["Version"])')
echo "csproj=$csproj  mod manifest=$cs  content manifest=$cp"
[[ "$csproj" == "$cs" && "$cs" == "$cp" ]] || { echo "Versions differ" >&2; exit 1; }
[[ "$cs" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]] || { echo "Version must be N.N.N" >&2; exit 1; }
echo OK

#!/usr/bin/env bash
# Symlink the Content Patcher pack into the game's Mods folder so edits to
# content.json / assets apply on the next `patch reload` (no rebuild needed).
set -euo pipefail
GAME="${STARDEW_GAME:-$HOME/.local/share/Steam/steamapps/common/Stardew Valley}"
SRC="$(cd "$(dirname "$0")/.." && pwd)/src/SDVPP.Content"
ln -sfn "$SRC" "$GAME/Mods/SDVPP.Content"
echo "Linked $GAME/Mods/SDVPP.Content -> $SRC"

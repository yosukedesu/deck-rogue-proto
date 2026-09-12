#!/usr/bin/env bash
# scripts/sync-web-art.sh — Unity 用のドット絵 (unity/Assets/Resources/Art) をブラウザ版 (public/art) へ複製する (2026-09-12)。
# PixelLab で絵を作り直したら実行 → git に入れる。メタ (.pixellab.json) は複製しない
set -euo pipefail
cd "$(dirname "$0")/.."
mkdir -p public/art/cards
rsync -a --delete --include='*.png' --exclude='*' unity/Assets/Resources/Art/cards/ public/art/cards/
echo "public/art/cards: $(ls public/art/cards | wc -l) files"

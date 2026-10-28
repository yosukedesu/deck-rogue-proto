#!/bin/bash
# 一周の動画 (品質スプリント #2「画面の切り替えと動き」の門。2026-10-29 docs/design/quality-sprint-2026-10-28.md §9-5):
# 自動操縦 loop (タイトル→出立→地図→戦闘→報酬→地図→店/焚き火/?→…) を幕・入り・飛びを動かしたまま 30fps でコマ撮りし、ffmpeg で mp4 にする。
#   scripts/loop-video.sh [出力フォルダ] [seed]
#   SHOT_W/SHOT_H (既定 1280×720)・SHOT_TIMEOUT (既定 900 秒)・PLAYER_ARGS="-every 3" でコマの間隔 (既定 2 フレーム=30fps)
set -u
REPO=$(cd "$(dirname "$0")/.." && pwd)
OUT="${1:-$REPO/unity/Shots/loop}"; SEED="${2:-4242}"
mkdir -p "$OUT"; rm -f "$OUT"/loop-*.png "$OUT"/loop.mp4
SHOTS_OUT="$OUT" SHOT_W="${SHOT_W:-1280}" SHOT_H="${SHOT_H:-720}" SHOT_TIMEOUT="${SHOT_TIMEOUT:-900}" "$REPO/scripts/unity-win.sh" shots loop "$SEED" </dev/null | tail -5
N=$(ls "$OUT"/loop-*.png 2>/dev/null | wc -l)
echo "frames: $N"
if [ "$N" -gt 0 ]; then
  ffmpeg -y -loglevel error -framerate 30 -pattern_type glob -i "$OUT/loop-*.png" -c:v libx264 -pix_fmt yuv420p -crf 20 "$OUT/loop.mp4" \
    && echo "mp4: $OUT/loop.mp4 ($(du -h "$OUT/loop.mp4" | cut -f1)・$(echo "scale=1; $N/30" | bc) 秒)"
fi

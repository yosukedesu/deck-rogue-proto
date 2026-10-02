#!/usr/bin/env bash
# scripts/hd2d-motion/frames2mp4.sh — うちの撮影の連写 (pshots の <名前>-<n>.png) を本当の速さの 60fps の mp4 にする (HD-2D 段2 レーン F。2026-10-03)。
#   scripts/hd2d-motion/frames2mp4.sh <撮影のフォルダ> <名前> [出力.mp4]
#   例: scripts/hd2d-motion/frames2mp4.sh ~/.cache/deck-rogue/hd2d-stage2/shots/t1/s2-dio23 PC-T2-attack
#   出力の既定は ~/.cache/deck-rogue/hd2d-stage2/motion/ours/<名前>.mp4 (git の管理外。本家と並べる前のうちの画だけ)。
# ★ 連写の 1 コマは 1/60 秒ではない (1 コマ = every + 4 フレーム = playevery=1 でも 1/12 秒。seq.py の頭)。何枚目が何秒かは
#   seq.py が layout.json の frame (dumplayout=1) か every から読み、1 枚ごとの長さつきの一覧を ffmpeg の concat に渡す
#   (= 1 コマを本当の秒だけ保つ 60fps の mp4。本家の clip と左右に並べても速さがそろう)。連写の後ろの 1 枚撮り (state-*) は入れない。
#   mp4 の 0 秒 = 連写の 1 枚目 = 命令 (札を出す・手番を終える…) の every+1 フレーム後。出す前の盤面は入らない (-pre の 1 枚撮りは別の画)。
#   環境変数: EVERY=N (layout.json も STATE も無い時の every)・FPS=F (pshots でない素の連番 = 1 コマ 1/F 秒)・BURST=play|gear|end|fire|enter|fx
set -euo pipefail
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DIR="${1:?撮影のフォルダ}"; NAME="${2:?名前 (PC-T2-attack など)}"
OUT="${3:-$HOME/.cache/deck-rogue/hd2d-stage2/motion/ours/$NAME.mp4}"
[ $# -le 3 ] || { echo "引数が多い (4 つ目の fps は廃止。素の連番は FPS=F、every は EVERY=N で書く)"; exit 2; }
mkdir -p "$(dirname "$OUT")"
LIST="$(mktemp --suffix=.ffconcat)"; trap 'rm -f "$LIST"' EXIT
python3 "$HERE/seq.py" "$DIR" "$NAME" ${EVERY:+--every "$EVERY"} ${FPS:+--fps "$FPS"} ${BURST:+--burst "$BURST"} --concat "$LIST"
TOTAL="$(python3 "$HERE/seq.py" "$DIR" "$NAME" ${EVERY:+--every "$EVERY"} ${FPS:+--fps "$FPS"} ${BURST:+--burst "$BURST"} --total)"
# 最後の 1 枚は一覧に 2 度書いてある (concat が最後の duration を捨てないように) = -t で合計の秒に切る
ffmpeg -v error -y -f concat -safe 0 -i "$LIST" -vf "fps=60,scale=trunc(iw/2)*2:trunc(ih/2)*2" -t "$TOTAL" \
  -c:v libx264 -preset veryfast -crf 14 -pix_fmt yuv420p -movflags +faststart "$OUT"
echo "$OUT ($(awk "BEGIN{printf \"%.2f\", $TOTAL}") 秒・60fps)"

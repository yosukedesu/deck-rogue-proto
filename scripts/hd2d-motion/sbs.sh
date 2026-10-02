#!/usr/bin/env bash
# scripts/hd2d-motion/sbs.sh — 本家の clip｜うちの mp4 を左右に並べる (ffmpeg hstack・同じ高さ。HD-2D 段2 レーン F。2026-10-03)。
# 判定の方法 (計画 §1・分析書 §8・motion.md §7): 静止画の「見劣り」では動きを測れないので、5 場面 × 2〜4 秒を左右に並べて 1 本 15〜20 秒で見る。
# ★ 本家の動画は著作物。出力は ~/.cache の中だけに置き、リポジトリにも外にも出さない (本家の動画の置き場も決め打ちしない = 引数)。
#
#   1 組:   scripts/hd2d-motion/sbs.sh <本家の動画> <本家の開始秒> <長さ秒> <うちの mp4> <出力.mp4> [高さ=540] [うちの開始秒=0]
#   並べた組をつなぐ (1 本 15〜20 秒):
#           scripts/hd2d-motion/sbs.sh -l <一覧.txt> <出力.mp4> [高さ=540]
#           一覧は 1行 = 見出し|本家の動画|本家の開始秒|長さ秒|うちの mp4|うちの開始秒 (# と空行は読み飛ばす)。例 (motion.md §7 の 5 場面):
#             A 通常の大きい当たり|<videos>/ot1_tomb_behemoth_Xmcszr-JVrc.mp4|107.4|2.4|<ours>/PC-T2-attack.mp4|0
#             B ブースト級 (X 札)|<videos>/ot1_gaston_i1CslIhswks.mp4|5.4|2.8|<ours>/PC-T2-x.mp4|0
#             C 呪文|<videos>/ot1_lordforest_Z6YxMVcHFJI.mp4|23.0|3.0|<ours>/PC-T2-spell.mp4|0
#             D 敵の大技|<videos>/ot1_guardian_i1CslIhswks.mp4|21.8|3.5|<ours>/PC-T2-enemy-big.mp4|0
#             E 待機|<videos>/ot1_lordforest_Z6YxMVcHFJI.mp4|80.0|4.0|<ours>/PC-T2-idle.mp4|1.0
#   短い側は最後のコマを止めて長さをそろえる。左上に「本家」「うち」(と組の見出し) を焼く (字が焼けない ffmpeg なら焼かずに続ける)。
#   環境変数: LABEL_L=本家 LABEL_R=うち FONT=<字の .otf> (既定はリポジトリの NotoSansJP-Bold)
set -euo pipefail
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"; REPO="$(dirname "$(dirname "$HERE")")"
FONT="${FONT:-$REPO/unity/Assets/Resources/Fonts/NotoSansJP-Bold.otf}"
LABEL_L="${LABEL_L:-本家}"; LABEL_R="${LABEL_R:-うち}"
esc() { printf '%s' "$1" | sed -e "s/\\\\/\\\\\\\\/g" -e "s/:/\\\\:/g" -e "s/'/\\\\'/g"; }

# 1 組を作る: pair <本家> <本家の開始> <長さ> <うち> <うちの開始> <高さ> <出力> [見出し]
pair() {
  local hv="$1" hs="$2" ln="$3" ov="$4" os="$5" h="$6" out="$7" title="${8:-}"
  [ -f "$hv" ] || { echo "本家の動画が無い: $hv"; return 2; }
  [ -f "$ov" ] || { echo "うちの mp4 が無い: $ov"; return 2; }
  local base="fps=60,scale=-2:${h},setsar=1,format=yuv420p,tpad=stop_mode=clone:stop_duration=${ln}"
  local fs=$((h / 18)) lab="" ft
  if [ -f "$FONT" ]; then
    ft="fontfile='$(esc "$FONT")':fontsize=${fs}:fontcolor=white:box=1:boxcolor=black@0.55:boxborderw=6"
    lab=1
  fi
  local fa="$base" fb="$base"
  if [ -n "$lab" ]; then
    fa="$fa,drawtext=${ft}:x=12:y=10:text='$(esc "$LABEL_L${title:+ $title}")'"
    fb="$fb,drawtext=${ft}:x=12:y=10:text='$(esc "$LABEL_R")'"
  fi
  if ! ffmpeg -nostdin -v error -y -ss "$hs" -t "$ln" -i "$hv" -ss "$os" -t "$ln" -i "$ov" \
      -filter_complex "[0:v]${fa}[a];[1:v]${fb}[b];[a][b]hstack=inputs=2[v]" -map "[v]" -t "$ln" -an \
      -c:v libx264 -preset veryfast -crf 18 -pix_fmt yuv420p "$out" 2>"$out.err"; then
    if [ -n "$lab" ]; then   # 字が焼けない (drawtext が無い) なら字なしで作り直す
      echo "字を焼けなかった (字なしで続ける): $(tail -1 "$out.err")"
      ffmpeg -nostdin -v error -y -ss "$hs" -t "$ln" -i "$hv" -ss "$os" -t "$ln" -i "$ov" \
        -filter_complex "[0:v]${base}[a];[1:v]${base}[b];[a][b]hstack=inputs=2[v]" -map "[v]" -t "$ln" -an \
        -c:v libx264 -preset veryfast -crf 18 -pix_fmt yuv420p "$out"
    else
      cat "$out.err"; rm -f "$out.err"; return 1
    fi
  fi
  rm -f "$out.err"
}

if [ "${1:-}" = "-l" ]; then
  LIST="${2:?一覧}"; OUT="${3:?出力.mp4}"; H="${4:-540}"
  TMP="$(mktemp -d)"; trap 'rm -rf "$TMP"' EXIT
  i=0; inputs=(); widths=()
  while IFS='|' read -r title hv hs ln ov os; do
    case "$title" in ''|\#*) continue ;; esac
    i=$((i + 1)); f="$TMP/p$i.mp4"
    pair "$hv" "$hs" "$ln" "$ov" "${os:-0}" "$H" "$f" "$title"
    inputs+=("$f"); widths+=("$(ffprobe -v error -select_streams v:0 -show_entries stream=width -of csv=p=0 "$f")")
  done < <(tr -d '\r' < "$LIST")
  [ "$i" -ge 1 ] || { echo "一覧に組が無い: $LIST"; exit 2; }
  W=0; for w in "${widths[@]}"; do [ "$w" -gt "$W" ] && W=$w; done
  args=(); fc=""; cat=""
  for k in $(seq 0 $((i - 1))); do
    args+=(-i "${inputs[$k]}")
    fc+="[$k:v]pad=${W}:${H}:(ow-iw)/2:0:color=black,setsar=1[v$k];"; cat+="[v$k]"
  done
  ffmpeg -nostdin -v error -y "${args[@]}" -filter_complex "${fc}${cat}concat=n=${i}:v=1:a=0[v]" -map "[v]" -an \
    -c:v libx264 -preset veryfast -crf 18 -pix_fmt yuv420p -movflags +faststart "$OUT"
  echo "$OUT ($i 組・幅 $W・高さ $H)"
else
  [ $# -ge 5 ] || { sed -n '2,20p' "$0"; exit 2; }
  pair "$1" "$2" "$3" "$4" "${7:-0}" "${6:-540}" "$5"
  echo "$5"
fi

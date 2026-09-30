#!/usr/bin/env bash
# scripts/hd2d-shots.sh — HD-2D 見本の撮影の一覧をまとめて撮って、名前を付けて1つのフォルダに集める (2026-09-30 P08)。
# 計画 docs/design/hd2d-slice-plan-2026-09-30.md §4 P08・§6。撮るのは scripts/unity-win.sh (shots か states)。ビルド済みの exe が要る。
#
#   scripts/hd2d-shots.sh <一覧> <出力名> [--phone | --phone-tier] [--states] [--no-det] [--add "k=v;…"] [--only 正規表現] [--seed N] [--retry N]
#     一覧   = scripts/hd2d-states/*.txt (1行 = 名前|STATE。# と空行は読み飛ばす)
#     出力名 = unity/Shots/hd2d/<出力名>/ に集める (git の管理外)。絶対パスならそこへ
#     --phone      スマホ相当の寸法 (SHOT_W=1920 SHOT_H=886 UISCALE=1.6)。STATE は変えない = W0 の基準と比べられる
#     --phone-tier スマホ相当の寸法＋各行に tier=phone (スマホの品質の段。見本と比べる時はこちら。計画 §6)
#     --states     1回の起動で順に撮る (unity-win.sh states)。速いが「合否の門には使わない」(計画 §8)。既定は1行1起動 (門の撮り方)
#     --no-det     決定的な撮影 (-det) を切る (既定は DET=1。perf の計測は det なしで)
#     --add        全部の行の STATE の後ろに足す (例 --add "dumplayout=1")
#     --only       名前がこの正規表現に合う行だけ
#     --seed       シード (既定 4242)
#     --retry      1行が撮れなかった時にやり直す回数 (既定 2)
#
# できるもの (W0 の基準 scratchpad/hd2d/w0/baseline と同じ名前 = pixcmp.py・hd2d-measure.py がそのまま読む)
#   <PC|PH>-<名前>-<k>.png          k = その行の何枚目 (連番の撮影は 1,2,3…)
#   <PC|PH>-<名前>-<k>.layout.json  dumplayout=1 の時 (同じ k)
#   <PC|PH>-<名前>-<元の名前>.csv/.json  perf の記録 (PerfProbe)
#   <PC|PH>-<名前>.log              その行の撮影のログ (unity-win.sh の出力)
#   _status.txt                     行ごとの成否。states の時は _states.log
#
# 環境: UNITY_WIN=<unity-win.sh の代わり> (試験用)。SHOT_TIMEOUT・PLAYER_EXE・PLAYER_ARGS・HD2D はそのまま unity-win.sh へ渡る
set -u
REPO="$(cd "$(dirname "$0")/.." && pwd)"
UW="${UNITY_WIN:-$REPO/scripts/unity-win.sh}"
LIST="${1:-}"; NAME="${2:-}"
if [ -z "$LIST" ] || [ -z "$NAME" ] || [ ! -f "$LIST" ]; then sed -n '2,24p' "$0"; exit 2; fi
shift 2
PHONE=0; TIER=0; STATES=0; DETV=1; ADD=""; ONLY=""; SEED=4242; RETRY=2
while [ $# -gt 0 ]; do
  case "$1" in
    --phone) PHONE=1; shift ;;
    --phone-tier) PHONE=1; TIER=1; shift ;;
    --states) STATES=1; shift ;;
    --no-det) DETV=0; shift ;;
    --add) ADD="${2:-}"; shift 2 ;;
    --only) ONLY="${2:-}"; shift 2 ;;
    --seed) SEED="${2:-4242}"; shift 2 ;;
    --retry) RETRY="${2:-2}"; shift 2 ;;
    *) echo "不明な引数: $1"; exit 2 ;;
  esac
done
case "$NAME" in /*) OUT="$NAME" ;; *) OUT="$REPO/unity/Shots/hd2d/$NAME" ;; esac
mkdir -p "$OUT"
DEV=PC; [ "$PHONE" = 1 ] && DEV=PH
DIM=()
[ "$PHONE" = 1 ] && DIM=(SHOT_W=1920 SHOT_H=886 UISCALE=1.6)
DETENV=()
[ "$DETV" = 1 ] && DETENV=(DET=1)

# 行の STATE を組む (tier と --add を足す)
make_state() {
  local st="$1"
  [ "$TIER" = 1 ] && case ";$st;" in *";tier="*) ;; *) st="$st;tier=phone" ;; esac
  [ -n "$ADD" ] && st="$st;$ADD"
  echo "$st"
}

# 取り出した撮影 (dir) を <DEV>-<行>-<k>.* に付け直して OUT へ。1行1起動の名前は「NN-name.png」(NN は2桁以上の通し番号)
collect_line() {   # $1 = 取り出した置き場  $2 = 行の名前
  local src="$1" row="$2" k=0 f base stem
  # 番号の数で並べる (100 枚を超える連番でも 1,2,…,100 の順)
  while IFS= read -r f; do
    [ -n "$f" ] || continue
    k=$((k + 1)); base="$(basename "$f")"; stem="${base%.png}"
    cp "$f" "$OUT/$DEV-$row-$k.png"
    [ -f "$src/$stem.layout.json" ] && cp "$src/$stem.layout.json" "$OUT/$DEV-$row-$k.layout.json"
  done < <(ls "$src"/*.png 2>/dev/null | awk -F/ '{n=$NF; split(n,a,"-"); printf "%09d\t%s\n", a[1]+0, $0}' | sort -k1,1n -k2 | cut -f2)
  for f in "$src"/*.csv "$src"/*.json; do
    [ -f "$f" ] || continue
    case "$f" in *.layout.json) continue ;; esac
    cp "$f" "$OUT/$DEV-$row-$(basename "$f")"
  done
  echo "$k"
}

if [ "$STATES" = 1 ]; then
  # 1回の起動で撮る: 一覧を組み直して unity-win.sh states へ
  TMPL="$(mktemp "${TMPDIR:-/tmp}/hd2d-states.XXXXXX")"
  TMPD="$(mktemp -d "${TMPDIR:-/tmp}/hd2d-shots.XXXXXX")"
  trap 'rm -rf "$TMPL" "$TMPD"' EXIT
  NL=0
  while IFS='|' read -r -u 3 N ST; do
    N="$(echo "$N" | tr -d '\r')"; ST="$(echo "${ST:-}" | tr -d '\r')"
    case "$N" in ''|\#*) continue ;; esac
    [ -n "$ONLY" ] && ! echo "$N" | grep -Eq -- "$ONLY" && continue
    echo "$N|$(make_state "$ST")" >> "$TMPL"; NL=$((NL + 1))
  done 3< "$LIST"
  echo "states: $NL 行 → $OUT ($DEV)"
  env "${DIM[@]}" "${DETENV[@]}" SHOTS_OUT="$TMPD" bash "$UW" states "$TMPL" "$SEED" < /dev/null > "$OUT/_states.log" 2>&1
  RC=$?
  # states の名前は「<行の名前>-<k>.png」(Autopilot の SafeFileName) → DEV を前に付ける
  n=0
  for f in "$TMPD"/*; do
    [ -f "$f" ] || continue
    cp "$f" "$OUT/$DEV-$(basename "$f")"; n=$((n + 1))
  done
  echo "states rc=$RC files=$n" | tee -a "$OUT/_status.txt"
  exit $RC
fi

# 1行1起動 (合否の門の撮り方)
TMPD="$(mktemp -d "${TMPDIR:-/tmp}/hd2d-shots.XXXXXX")"
trap 'rm -rf "$TMPD"' EXIT
NOK=0; NNG=0
while IFS='|' read -r -u 3 N ST; do
  N="$(echo "$N" | tr -d '\r')"; ST="$(echo "${ST:-}" | tr -d '\r')"
  case "$N" in ''|\#*) continue ;; esac
  [ -n "$ONLY" ] && ! echo "$N" | grep -Eq -- "$ONLY" && continue
  S2="$(make_state "$ST")"
  got=0
  for attempt in $(seq 1 $((RETRY + 1))); do
    rm -rf "$TMPD/line"; mkdir -p "$TMPD/line"
    env "${DIM[@]}" "${DETENV[@]}" SHOTS_OUT="$TMPD/line" STATE="$S2" bash "$UW" shots state "$SEED" < /dev/null > "$OUT/$DEV-$N.log" 2>&1
    RC=$?
    got=$(collect_line "$TMPD/line" "$N")
    if [ "$got" -ge 1 ]; then
      echo "$DEV-$N ok rc=$RC n=$got attempt=$attempt" >> "$OUT/_status.txt"; break
    fi
    echo "$DEV-$N fail rc=$RC attempt=$attempt" >> "$OUT/_status.txt"
    sleep "${HD2D_RETRY_SLEEP:-12}"
  done
  if [ "$got" -ge 1 ]; then NOK=$((NOK + 1)); else NNG=$((NNG + 1)); fi
  echo "$DEV-$N: $got 枚"
done 3< "$LIST"
echo "DONE ok=$NOK ng=$NNG" | tee -a "$OUT/_status.txt"
[ "$NNG" = 0 ]

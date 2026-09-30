#!/usr/bin/env bash
# scripts/pshots.sh — ビルド済みプレイヤーで撮影を並列に回す (2026-09-30 HD-2D 見本の時短)。
# unity-win.sh shots は1枚ごとに作業コピーを同期し、動いているプレイヤーを全部落としてから起動する＝並列にできない。
# ここでは同期も kill もせず、場面ごとに別の Shots フォルダと別の log で同時に起動する。
#   scripts/pshots.sh <一覧> <出力フォルダ> [並列数=3] [seed=4242]
#   一覧は1行=名前|PC または PH|STATE か 名前|STATE（後者は DEV=PH で全部スマホ相当・既定 PC。# と空行は読み飛ばす）
#   環境変数: DET=1（-det）・HD2D=…（-hd2d）・PLAYER_ARGS=…・SHOT_TIMEOUT=秒（既定 240）・RETRY=回（既定 2）・WIN_DIR
#   PH の行は 1920×886・UI 1.6倍（S25 横持ち相当）。出力は <出力>/<名前>-<n>.png（と dumplayout=1 なら <名前>-<n>.layout.json）・<名前>.log・perf の <名前>-*.json/csv、最後に _status.txt に DONE。
# 前提: 先に scripts/unity-win.sh build（このスクリプトはビルドも同期もしない）。
set -u
LIST="${1:?一覧}"; OUT="${2:?出力フォルダ}"; JOBS="${3:-3}"; SEED="${4:-4242}"
WIN_DIR="${WIN_DIR:-/mnt/d/deck-rogue/unity-batch}"
EXE="$WIN_DIR/Build/DeckRogue.exe"
[ -f "$EXE" ] || { echo "ビルドが無い: $EXE"; exit 2; }
mkdir -p "$OUT"; : > "$OUT/_status.txt"
export WIN_DIR EXE OUT SEED

one() {
  local line="$1" name dev st
  name="${line%%|*}"; line="${line#*|}"
  case "$line" in PC\|*|PH\|*) dev="${line%%|*}"; st="${line#*|}" ;; *) dev="${DEV:-PC}"; st="$line" ;; esac
  local work="$WIN_DIR/PShots/$name"
  local h=1080 ui=""
  if [ "$dev" = PH ]; then h=886; ui="1.6"; fi
  local attempt rc n
  for attempt in $(seq 1 $(( ${RETRY:-2} + 1 ))); do
    rm -rf "$work"; mkdir -p "$work"
    timeout -k 5 "${SHOT_TIMEOUT:-240}" "$EXE" -autopilot state -seed "$SEED" -shots "$(wslpath -w "$work")" \
      ${DET:+-det} ${ui:+-uiscale "$ui"} -state "$st" ${HD2D:+-hd2d "$HD2D"} ${PLAYER_ARGS:-} \
      -screen-width 1920 -screen-height "$h" -screen-fullscreen 0 -logFile "$(wslpath -w "$work/player.log")" </dev/null >/dev/null 2>&1
    rc=$?
    # timeout で WSL 側の中継だけが死に、Windows の DeckRogue.exe は止まったまま残る (2026-09-30 W3 の統合: 残った 17 本が GPU を握り、
    # 後の撮影ほど止まる・作業フォルダを消せず rc=5 で落ちる、が起きた)。止まった時はこの作業フォルダで起動したプレイヤーを Windows 側で落とす
    if [ "$rc" -ge 124 ]; then
      powershell.exe -NoProfile -Command "Get-CimInstance Win32_Process -Filter \"Name='DeckRogue.exe'\" | Where-Object { \$_.CommandLine -like '*PShots\\$name *' } | ForEach-Object { Stop-Process -Id \$_.ProcessId -Force }" >/dev/null 2>&1
      sleep 2
    fi
    n=$(ls "$work"/*.png 2>/dev/null | wc -l)
    if [ "$n" -ge 1 ]; then
      local i=0 f stem
      # 連番の撮影は「NN-…png」(NN は2桁以上の通し番号)。数で並べて 1,2,3… を振り、dumplayout=1 の同じ名前の .layout.json も同じ番号へ
      # (hd2d-measure.py・hd2d-layout-check.py は <名前>-<k>.png と <名前>-<k>.layout.json を対で読む。2026-09-30 W2)
      while IFS= read -r f; do
        i=$((i + 1)); stem="$(basename "$f" .png)"
        cp "$f" "$OUT/$name-$i.png"
        [ -f "$work/$stem.layout.json" ] && cp "$work/$stem.layout.json" "$OUT/$name-$i.layout.json"
      done < <(ls "$work"/*.png | awk -F/ '{n=$NF; split(n,a,"-"); printf "%09d\t%s\n", a[1]+0, $0}' | sort -k1,1n -k2 | cut -f2)
      for f in "$work"/*.json "$work"/*.csv; do
        case "$f" in *.layout.json) continue ;; esac
        [ -f "$f" ] && cp "$f" "$OUT/$name-$(basename "$f")"
      done
      cp "$work/player.log" "$OUT/$name.log" 2>/dev/null
      echo "$name ok rc=$rc n=$n attempt=$attempt" >> "$OUT/_status.txt"
      return 0
    fi
    cp "$work/player.log" "$OUT/$name.fail$attempt.log" 2>/dev/null
    echo "$name fail rc=$rc attempt=$attempt" >> "$OUT/_status.txt"
    sleep 5
  done
  return 1
}
export -f one
tr -d '\r' < "$LIST" | grep -vE '^[[:space:]]*(#|$)' | xargs -P "$JOBS" -I{} bash -c 'one "$@"' _ {}
echo DONE >> "$OUT/_status.txt"
grep -c " ok " "$OUT/_status.txt" | sed 's/^/ok: /'
grep " fail " "$OUT/_status.txt" || true

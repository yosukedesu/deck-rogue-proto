#!/usr/bin/env bash
# scripts/unity-win.sh — WSL から Windows 側の Unity Editor をバッチ実行する (2026-09-07)。
# 正本は WSL の unity/ (リポジトリ)。Windows 側 (C:\Users\yosuke\deck-rogue-unity-batch) は rsync で作る使い捨ての作業コピー
# (\\wsl$ の UNC パスを Unity が扱えないため)。Library/ は作業コピー側に残るので2回目以降は速い。
#   scripts/unity-win.sh compile   # 同期 → バッチ起動 → コンパイル結果 (error CS...) を要約
#   scripts/unity-win.sh verify    # 同期 → Assets/Editor/BatchTools.VerifyGoldens (エンジンの実機ゴールデン照合)
#   scripts/unity-win.sh play      # 同期 → BatchTools.PlaySmoke (プレイモードに入り、セットアップ→ラン開始→進路→戦闘を UI 経由で回す)
#   scripts/unity-win.sh setup-urp # URP アセットを作って割り当てる (一度だけ)
#   scripts/unity-win.sh sync      # 同期だけ
# ログ: C:\Users\yosuke\deck-rogue-unity\unity-batch.log (WSL からは $WIN_DIR/unity-batch.log)
set -u
MODE="${1:-compile}"
REPO="$(cd "$(dirname "$0")/.." && pwd)"
# バッチ用と GUI 用で作業コピーを分ける (同じプロジェクトを2つの Unity は開けない。GUI 用は WIN_DIR=/mnt/c/Users/yosuke/deck-rogue-unity で sync)
WIN_DIR="${WIN_DIR:-/mnt/c/Users/yosuke/deck-rogue-unity-batch}"
UNITY="${UNITY_EXE:-$(ls -d "/mnt/c/Program Files/Unity/Hub/Editor/"*/Editor/Unity.exe 2>/dev/null | sort | tail -1)}"
if [ -z "$UNITY" ]; then echo "Unity.exe が見つからない (Hub の Editor フォルダ)"; exit 2; fi

mkdir -p "$WIN_DIR"
# 作業コピーへ同期 (Library/Temp/Logs/obj/bin は作業コピー側の生成物なので触らない)
rsync -a --delete \
  --exclude 'Library/' --exclude 'Temp/' --exclude 'Logs/' --exclude 'UserSettings/' --exclude 'obj/' --exclude 'bin/' \
  --exclude 'EngineTests/' --exclude '*.csproj' --exclude '*.sln' --exclude 'unity-batch.log' --exclude 'goldens/' \
  "$REPO/unity/" "$WIN_DIR/"
mkdir -p "$WIN_DIR/goldens"
rsync -a --delete "$REPO/goldens/" "$WIN_DIR/goldens/"
echo "synced → $WIN_DIR (Unity: $UNITY)"
[ "$MODE" = "sync" ] && exit 0

WIN_PROJ="$(wslpath -w "$WIN_DIR")"
LOG="$WIN_DIR/unity-batch.log"
rm -f "$LOG"
ARGS=(-batchmode -nographics -projectPath "$WIN_PROJ" -logFile "$(wslpath -w "$LOG")")
case "$MODE" in
  compile) ARGS+=(-quit) ;;
  verify) ARGS+=(-quit -executeMethod DeckRogue.EditorTools.BatchTools.VerifyGoldens) ;;
  # play は -quit を付けない (プレイモードに入るため)。スモーク側が EditorApplication.Exit で必ず終わる。保険で timeout
  play) ARGS+=(-executeMethod DeckRogue.EditorTools.PlaySmoke.Run) ;;
  setup-urp) ARGS+=(-quit -executeMethod DeckRogue.EditorTools.UrpSetup.Run) ;;   # URP アセット生成→Graphics/Quality へ割当 (2026-09-07)
  *) echo "unknown mode: $MODE"; exit 2 ;;
esac
echo "run: Unity ${ARGS[*]}"
timeout -k 10 540 "$UNITY" "${ARGS[@]}"
CODE=$?
if [ "$CODE" = "124" ]; then echo "timeout: Unity を強制終了する"; taskkill.exe /IM Unity.exe /F >/dev/null 2>&1; fi
echo "exit code: $CODE"
# 作業コピー側で Unity が書き換えた ProjectSettings (ProjectVersion のリビジョン・Graphics/Quality の URP 割当 等) と
# Assets/Settings (URP アセット。.meta の GUID ごと) を正本へ戻す。作業コピーは次の同期で上書きされるため
rsync -a "$WIN_DIR/ProjectSettings/" "$REPO/unity/ProjectSettings/"
if [ -d "$WIN_DIR/Assets/Settings" ]; then rsync -a "$WIN_DIR/Assets/Settings/" "$REPO/unity/Assets/Settings/"; [ -f "$WIN_DIR/Assets/Settings.meta" ] && cp "$WIN_DIR/Assets/Settings.meta" "$REPO/unity/Assets/Settings.meta"; fi
if [ -f "$LOG" ]; then
  echo "---- errors ----"
  grep -E "error CS|Scripts have compiler errors|Assembly .* will not be loaded|License|license|Aborting batchmode|Exception|\[DeckRogue\]" "$LOG" | grep -v "^  at " | sort -u | head -60
  echo "---- tail ----"
  tail -5 "$LOG"
fi
exit $CODE

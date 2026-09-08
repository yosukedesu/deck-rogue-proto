#!/usr/bin/env bash
# scripts/unity-win.sh — WSL から Windows 側の Unity Editor をバッチ実行する (2026-09-07)。
# 正本は WSL の unity/ (リポジトリ)。Windows 側 (C:\Users\yosuke\deck-rogue-unity-batch) は rsync で作る使い捨ての作業コピー
# (\\wsl$ の UNC パスを Unity が扱えないため)。Library/ は作業コピー側に残るので2回目以降は速い。
#   scripts/unity-win.sh compile   # 同期 → バッチ起動 → コンパイル結果 (error CS...) を要約
#   scripts/unity-win.sh verify    # 同期 → Assets/Editor/BatchTools.VerifyGoldens (エンジンの実機ゴールデン照合)
#   scripts/unity-win.sh play      # 同期 → BatchTools.PlaySmoke (プレイモードに入り、セットアップ→ラン開始→進路→戦闘を UI 経由で回す)
#   scripts/unity-win.sh setup-urp # URP アセットを作って割り当てる (一度だけ)
#   scripts/unity-win.sh setup-tmp # TextMeshPro の必須リソースを取り込む (一度だけ)
#   scripts/unity-win.sh build     # Windows プレイヤー (Build/DeckRogue.exe) をビルド
#   scripts/unity-win.sh shots [tour] [seed]  # プレイヤーを自動操縦で起動して各画面の PNG を unity/Shots/ に回収
#   scripts/unity-win.sh sync      # 同期だけ
# ログ: C:\Users\yosuke\deck-rogue-unity\unity-batch.log (WSL からは $WIN_DIR/unity-batch.log)
set -u
MODE="${1:-compile}"
REPO="$(cd "$(dirname "$0")/.." && pwd)"
# バッチ用と GUI 用で作業コピーを分ける (同じプロジェクトを2つの Unity は開けない。GUI 用は WIN_DIR=/mnt/c/Users/yosuke/deck-rogue-unity で sync)
WIN_DIR="${WIN_DIR:-/mnt/c/Users/yosuke/deck-rogue-unity-batch}"
UNITY="${UNITY_EXE:-$(ls -d "/mnt/c/Program Files/Unity/Hub/Editor/"*/Editor/Unity.exe 2>/dev/null | sort | tail -1)}"
if [ -z "$UNITY" ]; then echo "Unity.exe が見つからない (Hub の Editor フォルダ)"; exit 2; fi

if [ "$MODE" = "pull" ]; then
  SRC="${2:?回収するパス (作業コピー相対。例: Assets/SomePack)}"
  GUI="${GUI_DIR:-/mnt/c/Users/yosuke/deck-rogue-unity}"
  if [ ! -e "$GUI/$SRC" ]; then echo "無い: $GUI/$SRC"; exit 2; fi
  mkdir -p "$REPO/unity/$(dirname "$SRC")"
  rsync -a "$GUI/$SRC" "$REPO/unity/$(dirname "$SRC")/"
  [ -f "$GUI/$SRC.meta" ] && cp "$GUI/$SRC.meta" "$REPO/unity/$SRC.meta"
  echo "pulled → unity/$SRC ($(find "$REPO/unity/$SRC" -type f | wc -l) files)"
  exit 0
fi
mkdir -p "$WIN_DIR"
# 作業コピーへ同期 (Library/Temp/Logs/obj/bin は作業コピー側の生成物なので触らない)
rsync -a --delete \
  --exclude 'Library/' --exclude 'Temp/' --exclude 'Logs/' --exclude 'UserSettings/' --exclude 'obj/' --exclude 'bin/' \
  --exclude 'EngineTests/' --exclude '*.csproj' --exclude '*.sln' --exclude 'unity-batch.log' --exclude 'goldens/' --exclude 'Build/' --exclude 'Shots/' --exclude 'player.log' \
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
  setup-tmp) ARGS+=(-quit -executeMethod DeckRogue.EditorTools.BuildTools.SetupTmp) ;;   # TMP Essential Resources の取り込み (一度だけ)
  build) ARGS+=(-quit -buildTarget Win64 -executeMethod DeckRogue.EditorTools.BuildTools.BuildWindows) ;;   # Build/DeckRogue.exe
  android) ARGS+=(-quit -buildTarget Android -executeMethod DeckRogue.EditorTools.BuildTools.BuildAndroid) ;;   # Build/DeckRogue.apk (Hub の Android Build Support が必要。2026-09-09)
  install)
    # 直前の android ビルドを USB 接続のスマホへ入れる (Windows 側の adb)
    APK="$WIN_DIR/Build/DeckRogue.apk"
    ADB="/mnt/c/Users/$(ls /mnt/c/Users | grep -v -i 'public\|default\|all users' | head -1)/AppData/Local/Android/Sdk/platform-tools/adb.exe"
    [ -f "$APK" ] || { echo "APK が無い: $APK (先に scripts/unity-win.sh android)"; exit 2; }
    [ -f "$ADB" ] || ADB=adb
    "$ADB" devices
    "$ADB" install -r "$(wslpath -w "$APK")"
    exit $?
    ;;
  shots)
    # 自動操縦スクショ: ビルド済みプレイヤーを起動し、PNG を unity/Shots/ (git 管理外) へ回収する
    EXE="$WIN_DIR/Build/DeckRogue.exe"
    if [ ! -f "$EXE" ]; then echo "ビルドが無い: $EXE (先に scripts/unity-win.sh build)"; exit 2; fi
    SCENARIO="${2:-tour}"; SEED="${3:-4242}"; STAGEACT="${4:-}"   # 4つ目: 舞台の幕だけ差し替え (-stageact)
    rm -rf "$WIN_DIR/Shots"; mkdir -p "$WIN_DIR/Shots"
    timeout -k 5 180 "$EXE" -autopilot "$SCENARIO" -seed "$SEED" -shots "$(wslpath -w "$WIN_DIR/Shots")" ${STAGEACT:+-stageact "$STAGEACT"} \
      -screen-width 1920 -screen-height 1080 -screen-fullscreen 0 -logFile "$(wslpath -w "$WIN_DIR/player.log")"
    PCODE=$?
    mkdir -p "$REPO/unity/Shots"; rm -f "$REPO/unity/Shots"/*.png
    cp "$WIN_DIR/Shots"/*.png "$REPO/unity/Shots/" 2>/dev/null
    echo "player exit=$PCODE shots: $(ls "$REPO/unity/Shots" 2>/dev/null | tr '\n' ' ')"
    grep -E "\[Autopilot\]|Exception|error" "$WIN_DIR/player.log" 2>/dev/null | cut -c1-200 | head -20
    exit $PCODE
    ;;
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
for d in Settings Scenes "TextMesh Pro"; do if [ -d "$WIN_DIR/Assets/$d" ]; then rsync -a "$WIN_DIR/Assets/$d/" "$REPO/unity/Assets/$d/"; [ -f "$WIN_DIR/Assets/$d.meta" ] && cp "$WIN_DIR/Assets/$d.meta" "$REPO/unity/Assets/$d.meta"; fi; done
if [ -f "$LOG" ]; then
  echo "---- errors ----"
  grep -E "error CS|Scripts have compiler errors|Assembly .* will not be loaded|License|license|Aborting batchmode|Exception|\[DeckRogue\]" "$LOG" | grep -v "^  at " | sort -u | head -60
  echo "---- tail ----"
  tail -5 "$LOG"
fi
exit $CODE

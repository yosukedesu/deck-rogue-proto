#!/usr/bin/env bash
# scripts/unity-win.sh — WSL から Windows 側の Unity Editor をバッチ実行する (2026-09-07)。
# 正本は WSL の unity/ (リポジトリ)。Windows 側 (D:\deck-rogue\unity-batch) は rsync で作る使い捨ての作業コピー
# (2026-09-16 C: の空き不足で D: へ移した。Library の IL2CPP キャッシュ 6GB と Gradle のキャッシュ (GRADLE_USER_HOME=D:\deck-rogue\gradle) が C: を食わない。D: は HDD)
# (\\wsl$ の UNC パスを Unity が扱えないため)。Library/ は作業コピー側に残るので2回目以降は速い。
#   scripts/unity-win.sh compile   # 同期 → バッチ起動 → コンパイル結果 (error CS...) を要約
#   scripts/unity-win.sh verify    # 同期 → Assets/Editor/BatchTools.VerifyGoldens (エンジンの実機ゴールデン照合)
#   scripts/unity-win.sh play      # 同期 → BatchTools.PlaySmoke (プレイモードに入り、セットアップ→ラン開始→進路→戦闘を UI 経由で回す)
#   scripts/unity-win.sh setup-urp # URP アセットを作って割り当てる (一度だけ)
#   scripts/unity-win.sh setup-tmp # TextMeshPro の必須リソースを取り込む (一度だけ)
#   scripts/unity-win.sh build     # Windows プレイヤー (Build/DeckRogue.exe) をビルド
#   scripts/unity-win.sh shots [tour] [seed]  # プレイヤーを自動操縦で起動して各画面の PNG を unity/Shots/ に回収 (DET=1 で決定的な撮影 = -det)
#   scripts/unity-win.sh sync      # 同期だけ
#   scripts/unity-win.sh live      # 同期 → 常駐のヘッドレス Editor (-quit 無し) を起動して Pipeline サーバを立てる (2026-09-15)。
#                                  #   以後 `unity command <名前> --project-path 'D:\deck-rogue\unity-batch'` で 0.5 秒で再コンパイル・テスト・eval
#   scripts/unity-win.sh stop      # live の Editor を終わらせる
# HD-2D 見本の道具 (2026-09-30 P06。docs/design/hd2d-slice-plan-2026-09-30.md):
#   scripts/unity-win.sh prep         # HD2DSetup.Apply = Linear・レイヤー名・Rendering Layers・PC/スマホの URP・品質レベル2・霧の版を残す。
#                                     #   設定のファイルを正本へ戻し、差分の一覧を出す。控え = unity/ProjectSettings/HD2DSetupBackup.json
#                                     #   PREP_SKIP="fog,cascade" = 旧舞台の見た目に効く2項目を飛ばす (Linear だけの差で比べたい時)
#   scripts/unity-win.sh revert-prep  # HD2DSetup.Revert = 控えの値へ丸ごと戻す (作ったアセットと控えは正本からも消す)
#   scripts/unity-win.sh checks       # HD2DSetup.Checks = 箱庭の Check (P04) とシェーダの実変換 (P03 HD2DShaderCheck) を1回の起動で。
#                                     #   CHECKS="型.関数;型.関数" で差し替え
#   scripts/unity-win.sh states <一覧> [seed]  # 1回の起動で一覧 (1行=名前|STATE) を順に撮る (-statesfile。合否の門には使わない)
#   scripts/unity-win.sh build-perf [android|win]  # 計測用 = Build/DeckRogue-perf.apk (com.deckrogue.proto.perf) / Build/perf-win/DeckRogue.exe
#   scripts/unity-win.sh install [perf]            # 直前の APK をスマホへ (perf = 計測用。ADB_SERIAL=<serial> で -s)
#   shots・states の回収は png・layout.json・csv・json (SHOTS_OUT で置き場を変えられる。既定 unity/Shots)。
#   shots・states に渡せる環境: DET=1・SHOT_W/SHOT_H・UISCALE・HD2D="k=v,..." (-hd2d)・PLAYER_ARGS="-perfprobe ..."・
#     PLAYER_EXE=perf-win/DeckRogue.exe (作業コピーの Build/ からの相対か絶対)・SHOT_TIMEOUT=<秒> (既定 shots 300・states 行数×40+120)
# ログ: C:\Users\yosuke\deck-rogue-unity\unity-batch.log (WSL からは $WIN_DIR/unity-batch.log)
set -u
MODE="${1:-compile}"
REPO="$(cd "$(dirname "$0")/.." && pwd)"
# バッチ用と GUI 用で作業コピーを分ける (同じプロジェクトを2つの Unity は開けない。GUI 用は WIN_DIR=/mnt/c/Users/yosuke/deck-rogue-unity で sync)
WIN_DIR="${WIN_DIR:-/mnt/d/deck-rogue/unity-batch}"
# Android ビルドの Gradle キャッシュも D: へ (Unity が起動する gradle は環境変数 GRADLE_USER_HOME を読む。WSLENV の /p で Windows のパスに写して渡す)
export GRADLE_USER_HOME="${GRADLE_USER_HOME:-/mnt/d/deck-rogue/gradle}"
export WSLENV="${WSLENV:+$WSLENV:}GRADLE_USER_HOME/p"
UNITY="${UNITY_EXE:-$(ls -d "/mnt/c/Program Files/Unity/Hub/Editor/"*/Editor/Unity.exe 2>/dev/null | sort | tail -1)}"
if [ -z "$UNITY" ]; then echo "Unity.exe が見つからない (Hub の Editor フォルダ)"; exit 2; fi

# ---- プレイヤー (shots・states) の共通の部品 (2026-09-30 HD-2D P06) ----
# 撮ったものを作業コピーの Shots/ から正本へ回収する。png に加えて layout.json (dumplayout)・csv と json (perf の記録)。
# 置き場は SHOTS_OUT (既定 unity/Shots)。前回の同じ種類のファイルは先に消す (下のフォルダ unity/Shots/hd2d/ などは触らない)。回収した数を出力
collect_shots() {
  local out="${SHOTS_OUT:-$REPO/unity/Shots}" f n=0
  mkdir -p "$out"
  rm -f "$out"/*.png "$out"/*.json "$out"/*.csv
  for f in "$WIN_DIR/Shots"/*.png "$WIN_DIR/Shots"/*.json "$WIN_DIR/Shots"/*.csv; do
    [ -f "$f" ] || continue
    cp "$f" "$out/"; n=$((n + 1))
  done
  echo "$n"
}
# 直前のプレイヤーが残っていると次の起動が D3D の初期化で固まる (2026-09-12 連続撮影で2回目が 300 秒タイムアウト) → 先に落として少し待つ。
# 落とすのは作業コピー (unity-batch\Build) から起動したものだけ: ユーザーが遊んでいる D:\deck-rogue\DeckRogue-win の exe は殺さない (2026-09-20 威圧の報告の最中に殺してしまった)
kill_batch_players() {
  powershell.exe -NoProfile -Command "Get-Process DeckRogue -ErrorAction SilentlyContinue | Where-Object { \$_.Path -like '$(wslpath -w "$WIN_DIR")\Build\*' } | Stop-Process -Force" >/dev/null 2>&1; sleep 2
}
# プレイヤーの exe。PLAYER_EXE で差し替え (作業コピーの Build/ からの相対か絶対。例 PLAYER_EXE=perf-win/DeckRogue.exe)
player_exe() {
  case "${PLAYER_EXE:-}" in
    "") echo "$WIN_DIR/Build/DeckRogue.exe" ;;
    /*) echo "$PLAYER_EXE" ;;
    *) echo "$WIN_DIR/Build/$PLAYER_EXE" ;;
  esac
}

if [ "$MODE" = "stop" ]; then
  # live で起動した常駐 Editor を終わらせる (先に eval で EditorApplication.Exit、駄目なら記述子の PID を kill)
  DESC="$WIN_DIR/Library/Pipeline/.unity-pipeline-port"
  if [ ! -f "$DESC" ]; then echo "常駐の Editor は無い ($DESC が無い)"; exit 0; fi
  PID=$(python3 -c "import json;print(json.load(open('$DESC'))['pid'])" 2>/dev/null)
  unity command eval 'UnityEditor.EditorApplication.Exit(0); return "bye";' --project-path "$(wslpath -w "$WIN_DIR")" --format json --no-banner --timeout 10 >/dev/null 2>&1
  # 記述子はすぐ消えるがプロセス (と AssetImportWorker の子 Unity.exe) の終了は数秒かかる。次の compile/build がロックで待たされないよう終了まで待つ
  for i in $(seq 1 20); do [ -n "$PID" ] && tasklist.exe /FI "PID eq $PID" 2>/dev/null | grep -q "^Unity.exe" || break; sleep 2; done
  if [ -n "$PID" ] && tasklist.exe /FI "PID eq $PID" 2>/dev/null | grep -q "^Unity.exe"; then echo "終わらないので kill (PID $PID)"; taskkill.exe /PID "$PID" /F >/dev/null 2>&1; fi
  rm -f "$DESC"
  echo "stopped"
  exit 0
fi
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
# Packages/manifest.json と packages-lock.json は作業コピー側 (GUI の Package Manager) が書き換えることがあるので --delete の対象から外し、
# manifest はリポジトリ側が新しい時だけ上書きする (2026-09-12: 同期が GUI で入れた Unity AI のパッケージを消し、コンパイル DAG が古い参照を抱えて CS2001 の嵐になった)。
# GUI で足したパッケージをリポジトリへ戻すのは scripts/unity-win.sh pull Packages/manifest.json
rsync -a --delete \
  --exclude 'Library/' --exclude 'Temp/' --exclude 'Logs/' --exclude 'UserSettings/' --exclude 'obj/' --exclude 'bin/' \
  --exclude 'EngineTests/' --exclude '*.csproj' --exclude '*.sln' --exclude 'unity-batch.log' --exclude 'goldens/' --exclude 'Build/' --exclude 'Shots/' --exclude 'PShots/' --exclude 'player.log' \
  --exclude 'Packages/manifest.json' --exclude 'Packages/packages-lock.json' \
  "$REPO/unity/" "$WIN_DIR/"
mkdir -p "$WIN_DIR/Packages"
rsync -a --update "$REPO/unity/Packages/manifest.json" "$WIN_DIR/Packages/manifest.json"
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
  # ---- HD-2D 見本の道具 (2026-09-30 P06) ----
  prep)   # Linear・レイヤー・URP (PC/スマホ)・品質レベル2・霧の版。PREP_SKIP="fog,cascade" で旧舞台の見た目に効く2項目を飛ばせる
    ARGS+=(-quit -executeMethod DeckRogue.EditorTools.HD2DSetup.Apply)
    if [ -n "${PREP_SKIP:-}" ]; then ARGS+=(-hd2dskip "$PREP_SKIP"); fi
    ;;
  revert-prep) ARGS+=(-quit -executeMethod DeckRogue.EditorTools.HD2DSetup.Revert) ;;  # 控えの値へ丸ごと戻す
  checks)
    ARGS+=(-quit -executeMethod DeckRogue.EditorTools.HD2DSetup.Checks)
    if [ -n "${CHECKS:-}" ]; then ARGS+=(-hd2dchecks "$CHECKS"); fi
    ;;
  build-perf)
    # 計測用のビルド (ユーザーのセーブのある本体と別のアプリ/別の persistentDataPath。Frame Timing Stats あり)
    case "${2:-android}" in
      android) ARGS+=(-quit -buildTarget Android -executeMethod DeckRogue.EditorTools.HD2DSetup.BuildPerfAndroid) ;;   # Build/DeckRogue-perf.apk
      win) ARGS+=(-quit -buildTarget Win64 -executeMethod DeckRogue.EditorTools.HD2DSetup.BuildPerfWindows) ;;          # Build/perf-win/DeckRogue.exe
      *) echo "build-perf の対象は android か win"; exit 2 ;;
    esac
    ;;
  install)
    # 直前の android ビルドを USB 接続のスマホへ入れる (Windows 側の adb)。install perf = 計測用 (Build/DeckRogue-perf.apk)。ADB_SERIAL=<serial> で -s
    APK="$WIN_DIR/Build/DeckRogue.apk"
    [ "${2:-}" = "perf" ] && APK="$WIN_DIR/Build/DeckRogue-perf.apk"
    ADB="/mnt/c/Users/$(ls /mnt/c/Users | grep -v -i 'public\|default\|all users' | head -1)/AppData/Local/Android/Sdk/platform-tools/adb.exe"
    [ -f "$APK" ] || { echo "APK が無い: $APK (先に scripts/unity-win.sh android / build-perf)"; exit 2; }
    [ -f "$ADB" ] || ADB=adb
    "$ADB" devices
    "$ADB" ${ADB_SERIAL:+-s "$ADB_SERIAL"} install -r "$(wslpath -w "$APK")"
    exit $?
    ;;
  shots)
    # 自動操縦スクショ: ビルド済みプレイヤーを起動し、PNG (と layout.json・csv・json) を unity/Shots/ (git 管理外) へ回収する
    EXE="$(player_exe)"
    if [ ! -f "$EXE" ]; then echo "ビルドが無い: $EXE (先に scripts/unity-win.sh build)"; exit 2; fi
    SCENARIO="${2:-tour}"; SEED="${3:-4242}"; STAGEACT="${4:-}"   # 4つ目: 舞台の幕だけ差し替え (-stageact)
    rm -rf "$WIN_DIR/Shots"; mkdir -p "$WIN_DIR/Shots"
    kill_batch_players
    # SHOT_W/SHOT_H で窓の寸法、UISCALE でスマホの倍率を PC で再現 (例: SHOT_W=1920 SHOT_H=886 UISCALE=1.3 = S25 の 1800×831 キャンバス)
    # STATE="phase=workshop;pick=0;viewmap=1" scripts/unity-win.sh shots state 4242  = 任意の状態へ跳んで1枚撮る (キーは Autopilot.StateJump の説明)
    # FAKECUTOUT="0,416,78,53" = 画面の切り欠き (実px・左下原点。「;」で複数) を差し込む = S25 のパンチホールを SHOT_W=1920 SHOT_H=886 で再現 (2026-09-29 p10)
    # DET=1 = 決定的な撮影 (-det: 1フレーム=1/60秒・乱数の種・舞台の粒を固定。同じ STATE を2回撮って画素一致。2026-09-30 HD-2D 見本 P00)
    # HD2D="stage=diorama,cam=28" = HD-2D の旗 (-hd2d。P01)・PLAYER_ARGS="-perfprobe" = そのまま渡す引数 (空白で区切る)・SHOT_TIMEOUT=<秒> (既定 300。PC の perf は長く)
    timeout -k 5 "${SHOT_TIMEOUT:-300}" "$EXE" -autopilot "$SCENARIO" -seed "$SEED" -shots "$(wslpath -w "$WIN_DIR/Shots")" ${DET:+-det} ${STAGEACT:+-stageact "$STAGEACT"} ${UISCALE:+-uiscale "$UISCALE"} ${FAKECUTOUT:+-fakecutout "$FAKECUTOUT"} ${STATE:+-state "$STATE"} \
      ${HD2D:+-hd2d "$HD2D"} ${PLAYER_ARGS:-} \
      -screen-width "${SHOT_W:-1920}" -screen-height "${SHOT_H:-1080}" -screen-fullscreen 0 -logFile "$(wslpath -w "$WIN_DIR/player.log")"
    PCODE=$?
    NGOT=$(collect_shots)
    echo "player exit=$PCODE shots: $(ls "${SHOTS_OUT:-$REPO/unity/Shots}" 2>/dev/null | grep -E '\.(png|json|csv)$' | tr '\n' ' ')($NGOT files)"
    grep -E "\[Autopilot\]|Exception|error" "$WIN_DIR/player.log" 2>/dev/null | cut -c1-200 | head -20
    exit $PCODE
    ;;
  states)
    # 1回の起動で一覧の行を順に撮る (Autopilot の -statesfile・各行の頭で旗を Reset。P01)。**合否の門には使わない、まとめて撮る専用** (計画 §8 審査1。門は shots を1行1起動)
    #   scripts/unity-win.sh states scripts/hd2d-states/baseline.txt [seed]
    # 一覧は1行=名前|STATE (# と空行は読み飛ばす)。起動は -autopilot state -statesfile <一覧> (P01 の口が別の scenario 名なら SCENARIO=<名前>)。
    # DET・SHOT_W/SHOT_H・UISCALE・FAKECUTOUT・HD2D・PLAYER_ARGS・PLAYER_EXE は shots と同じ。時間切れは 行数×STATE_SECS(既定40)+120 秒 (SHOT_TIMEOUT で上書き)
    EXE="$(player_exe)"
    if [ ! -f "$EXE" ]; then echo "ビルドが無い: $EXE (先に scripts/unity-win.sh build)"; exit 2; fi
    LIST="${2:-}"; SEED="${3:-4242}"
    if [ -z "$LIST" ] || [ ! -f "$LIST" ]; then echo "一覧が無い: '$LIST' (例 scripts/unity-win.sh states scripts/hd2d-states/baseline.txt)"; exit 2; fi
    NLINES=$(tr -d '\r' < "$LIST" | grep -cvE '^[[:space:]]*(#|$)')
    rm -rf "$WIN_DIR/Shots"; mkdir -p "$WIN_DIR/Shots"
    tr -d '\r' < "$LIST" > "$WIN_DIR/states-run.txt"
    kill_batch_players
    STIMEOUT="${SHOT_TIMEOUT:-$((120 + NLINES * ${STATE_SECS:-40}))}"
    echo "states: $NLINES 行 (時間切れ ${STIMEOUT} 秒)"
    timeout -k 5 "$STIMEOUT" "$EXE" -autopilot "${SCENARIO:-state}" -statesfile "$(wslpath -w "$WIN_DIR/states-run.txt")" -seed "$SEED" -shots "$(wslpath -w "$WIN_DIR/Shots")" \
      ${DET:+-det} ${UISCALE:+-uiscale "$UISCALE"} ${FAKECUTOUT:+-fakecutout "$FAKECUTOUT"} ${HD2D:+-hd2d "$HD2D"} ${PLAYER_ARGS:-} \
      -screen-width "${SHOT_W:-1920}" -screen-height "${SHOT_H:-1080}" -screen-fullscreen 0 -logFile "$(wslpath -w "$WIN_DIR/player.log")"
    PCODE=$?
    NGOT=$(collect_shots)
    echo "player exit=$PCODE states: $NLINES 行 → $NGOT files (${SHOTS_OUT:-$REPO/unity/Shots})"
    grep -E "\[Autopilot\]|Exception|error" "$WIN_DIR/player.log" 2>/dev/null | cut -c1-200 | tail -30
    exit $PCODE
    ;;
  live)
    # 常駐のヘッドレス Editor: -quit を付けずに起動すると Pipeline パッケージ (com.unity.pipeline) のサーバ (127.0.0.1:7800〜) が立ち、
    # Windows 側の Unity CLI (WSL の ~/.local/bin/unity が橋渡し) で `unity command recompile|run_tests|eval|console` が 0.2〜2 秒で回る。
    # -nographics なので screenshot/capture_game_view は撮れない (絵は shots か GUI の Editor で)。プロジェクトのロックを持つので compile/build と同時には使えない
    DESC="$WIN_DIR/Library/Pipeline/.unity-pipeline-port"
    if [ -f "$DESC" ]; then echo "既に常駐している: $DESC (止めるなら scripts/unity-win.sh stop)"; exit 0; fi
    LIVELOG="$WIN_DIR/unity-live.log"; rm -f "$LIVELOG"
    cmd.exe /c start "" /B "$(wslpath -w "$UNITY")" -batchmode -nographics -projectPath "$WIN_PROJ" -logFile "$(wslpath -w "$LIVELOG")" >/dev/null 2>&1 &
    for i in $(seq 1 60); do [ -f "$DESC" ] && break; sleep 3; done
    if [ ! -f "$DESC" ]; then echo "Pipeline サーバが立たない (3分)。ログ: $LIVELOG"; grep -E "error CS|Safe Mode|Aborting" "$LIVELOG" | head; exit 6; fi
    # 記述子が出ても起動処理 (初期インポート・インデックス) の間はメインスレッドが塞がっている。editor_status が ready を返すまで待つ
    for i in $(seq 1 60); do
      ST=$(unity command editor_status --project-path "$WIN_PROJ" --format json --no-banner --timeout 5 2>/dev/null | tr -d '\r' | python3 -c "import json,sys;d=json.load(sys.stdin);r=d.get('data',{}).get('result') or {};print(r.get('status',''),r.get('compiling',''))" 2>/dev/null)
      case "$ST" in "ready False") break;; esac
      sleep 3
    done
    unity command set_autotick --enable true --project-path "$WIN_PROJ" --format json --no-banner >/dev/null 2>&1   # 非フォーカスでも tick を止めない
    echo "live: $(python3 -c "import json;d=json.load(open('$DESC'));print('port', d['port'], 'pid', d['pid'])") → unity command <名前> --project-path '$WIN_PROJ'"
    exit 0
    ;;
  *) echo "unknown mode: $MODE"; exit 2 ;;
esac
echo "run: Unity ${ARGS[*]}"
LIMIT=540; [ "$MODE" = "android" ] && LIMIT=3600   # Android は IL2CPP のコンパイルで初回 15 分以上かかる (2026-09-09)
[ "$MODE" = "build-perf" ] && [ "${2:-android}" = "android" ] && LIMIT=3600
case "$MODE" in prep|revert-prep) LIMIT=1800 ;; esac   # 色空間を変えるとテクスチャを取り込み直す (次の起動にずれることもある)
timeout -k 10 "$LIMIT" "$UNITY" "${ARGS[@]}"
CODE=$?
if [ "$CODE" = "124" ]; then echo "timeout: Unity を強制終了する"; taskkill.exe /IM Unity.exe /F >/dev/null 2>&1; fi
echo "exit code: $CODE"
# 作業コピー側で Unity が書き換えた ProjectSettings (ProjectVersion のリビジョン・Graphics/Quality の URP 割当 等) と
# Assets/Settings (URP アセット。.meta の GUID ごと) を正本へ戻す。作業コピーは次の同期で上書きされるため
rsync -a "$WIN_DIR/ProjectSettings/" "$REPO/unity/ProjectSettings/"
for d in Settings Scenes "TextMesh Pro"; do if [ -d "$WIN_DIR/Assets/$d" ]; then rsync -a "$WIN_DIR/Assets/$d/" "$REPO/unity/Assets/$d/"; [ -f "$WIN_DIR/Assets/$d.meta" ] && cp "$WIN_DIR/Assets/$d.meta" "$REPO/unity/Assets/$d.meta"; fi; done
# HD-2D の prep / revert-prep / checks / build-perf (2026-09-30 P06): 上の書き戻しは消したファイルを正本から消さないので、
# Revert が消したもの (「[HD2DSetup] deleted: <パス>」の行) を正本からも消す。prep と revert-prep は正本の差分の一覧も出す
if [ -f "$LOG" ]; then
  case "$MODE" in
    revert-prep)
      grep -aF "[DeckRogue][HD2DSetup] deleted: " "$LOG" | sed 's/.*\[HD2DSetup\] deleted: //' | tr -d '\r' | sort -u | while read -r P; do
        case "$P" in
          Assets/*|ProjectSettings/*)
            if [ ! -e "$WIN_DIR/$P" ]; then rm -f "$REPO/unity/$P" "$REPO/unity/$P.meta"; echo "正本からも消した: unity/$P"; fi ;;
        esac
      done
      ;;
  esac
  case "$MODE" in
    prep|revert-prep|checks|build-perf)
      echo "---- HD2DSetup ----"
      grep -aF "[DeckRogue][HD2DSetup]" "$LOG" | tr -d '\r' | awk '!seen[$0]++' | head -100
      ;;
  esac
  case "$MODE" in
    prep|revert-prep)
      if git -C "$REPO" rev-parse --is-inside-work-tree >/dev/null 2>&1; then
        echo "---- 正本の差分 (unity/ProjectSettings・unity/Assets/Settings) ----"
        git -C "$REPO" status --short -- unity/ProjectSettings unity/Assets/Settings
        git -C "$REPO" diff --stat -- unity/ProjectSettings unity/Assets/Settings | tail -15
      fi
      ;;
  esac
fi
if [ -f "$LOG" ]; then
  echo "---- errors ----"
  grep -E "error CS|Scripts have compiler errors|Assembly .* will not be loaded|License|license|Aborting batchmode|Exception|\[DeckRogue\]" "$LOG" | grep -v "^  at " | sort -u | head -60
  echo "---- tail ----"
  tail -5 "$LOG"
fi
exit $CODE

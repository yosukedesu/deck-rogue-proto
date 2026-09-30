# HD-2D 見本の性能の測り方 (手順書)

2026-10-01 (W4 P31)。計画 `docs/design/hd2d-slice-plan-2026-09-30.md` の §1-2「5. 性能」・§2-7「スマホ」・P31 の手順書。

## 0. 何を測るか

| 端末 | 何を | 門 | 見本の判定に使うか |
|---|---|---|---|
| PC (1920×1080) | 見本の戦闘を 120 秒回した時の **GPU 時間の p95** | 記録するだけ (門なし) | 使わない (記録) |
| Galaxy S25 (実機) | 見本の戦闘を **15 分** 回した時の fps と GPU 時間 | **15 分で 30fps・GPU の p95 25ms 未満** | **使わない (保留の門)**。実機がつながった時に測る |

S25 は続けて負荷を掛けると GPU の性能が約 46% 落ちる (調査 `hd2d-result.json`)。だから 15 分続けて測り、終わりの 5 分 (端末が温まった後) も別に見る。

## 1. 用意してあるもの

| 物 | 置き場 | 中身 |
|---|---|---|
| 計測用の APK | `scripts/unity-win.sh build-perf` → `D:\deck-rogue\unity-batch\Build\DeckRogue-perf.apk` | applicationId **`com.deckrogue.proto.perf`** (本体 `com.deckrogue.proto` と別のアプリ = **ユーザーのセーブを消さない**・並べて入れられる)。表示名「DeckRogue Perf」。Frame Timing Stats あり (GPU 時間が取れる)。IL2CPP・ARM64・release (本体と同じ) |
| 計測用の exe | `scripts/unity-win.sh build-perf win` → `D:\deck-rogue\unity-batch\Build\perf-win\DeckRogue.exe` | 表示名「DeckRogue Perf」(persistentDataPath がユーザーの exe と別)。Frame Timing Stats あり |
| 記録の置き物 | `unity/Assets/Game/PerfProbe.cs` の `PerfProbe` | 1 秒ごとに CSV の1行・区間の終わりに要約の JSON。`-perfprobe`・`perf=<秒>`・計測用のビルドの時だけ動く。測る間は画面を消さない (`Screen.sleepTimeout = NeverSleep`) |
| 30fps | `PerfProbe.cs` の `PhoneFramePacing` | 実機では最初のシーンの前に `Application.targetFrameRate = 30` (Android の既定も 30 = 明示するだけ)。起動引数 `-targetfps <n>` で上書き。PC は触らない |
| 自動で戦う | `Autopilot` の STATE の `perf=<秒>` (P01) | 3 秒ごとに札を1枚。毎ターンの頭に自分のブロック 999 (負けない)。決着したら同じ盤面へ跳び直す (終わらない戦闘)。`-perfprobe` の時はセーブを消さない |
| 引数ファイル | 実機の `/sdcard/Android/data/com.deckrogue.proto.perf/files/perf-args.txt` | 計測用の APK だけが読む (起動引数の後ろに足す)。Android の `-e unity` で引数が届かない時の逃げ道 |
| スマホの段 | `unity/Assets/Resources/Stage/look_act1.phone.json`・`look_act1_phone1〜3.json` | 下の §2 |
| スマホの URP | `unity/Assets/Settings/URP-Phone.asset` (+ Renderer) | `scripts/unity-win.sh prep` (HD2DSetup.Apply) が作る。品質レベル2 = Android の既定。PC でも `tier=phone` で切り替わる |

## 2. スマホの段 (段0〜3)

計画 §2-7「重い時に落とす順: 光の筋と霧の面 → 影つきの逆光 → SSAO → ぼかしの段数。解像度は下げない」を4つの段にした。**段は前の段を全部含む**。

| 段 | 選び方 | 中身 |
|---|---|---|
| 段0 | `tier=phone` (実機は既定で phone) | 影つきの光は月と舞台の灯の2つ (影 512・カスケード1)・逆光は影なし・MSAA なし・SSAO なし・ぼかしは半解像度 (上限 10px = 1080 基準)・技の光は影なし・追加ライトの影の段 128/256/512・クッキーの地図 1024・30fps (実機だけ。PC の tier=phone は品質レベル2の vSync のまま)。計画 §2-7 のまま |
| 段1 | + `look=look_act1_phone1` | 光の筋と霧の面 (箱庭の部品 shaft・fog = 材質 glow) を消す。`diorama.dropKinds` を `Diorama.Build` が読んでその部品を組まない (W4 の統合で足した。部品 313 → 306)＋材質の強さ 0 |
| 段2 | + `look=look_act1_phone2` | 段1 + 逆光 (坑口の奥の脈の青緑の点光源) を消す (段0で影は既に無いので、光そのもの) |
| 段3 | + `look=look_act1_phone3` | 段2 + ぼかしの点の数を半分 (スマホの taps 16→8・nearOwnTaps 12→6)。ぼけの上限・半解像度・ピントの帯は同じ |

- SSAO は段0で既に無い (URP-Phone-Renderer) ので段に数えない。
- 段3 でも届かない時の次の手 (まだ用意していない・**ユーザーに確かめてから**): 影を硬くする (lamp・moon の `shadows: "hard"`)・ブルームを軽くする (Volume の downscale と高品質フィルタ。StageLook に口が要る)・ぼかしを切る (`dof=0`)。**解像度は下げない** (キャラのドットを崩す)。
- PC で段の見た目を撮る時: `STATE="…;hd2d=slice;tier=phone;look=look_act1_phone2"` (スマホ相当は `SHOT_W=1920 SHOT_H=886 UISCALE=1.6`)。

## 3. 前準備 (一度だけ。統合担当)

```bash
cd /home/yosuke/projects/deck-rogue-proto
scripts/unity-win.sh prep              # P31 のスマホの詰め (影の段・クッキーの地図) を URP-Phone に入れる。
                                       # 他の項目は W2 で入れたままなので「変更」は URP-Phone.asset の4行 (影の段 128/256/512・クッキー 1024) のはず。
                                       # 出力の「---- 正本の差分 ----」で確かめる (Unity が同じファイルの m_Prefilter* の行を書き直すことはある)
scripts/unity-win.sh build             # いつもの exe (撮影・見本の確かめ)
scripts/unity-win.sh build-perf win    # 計測用の exe (PC の GPU 時間)
scripts/unity-win.sh build-perf        # 計測用の APK (Android。初回は IL2CPP で 15 分以上。時間切れは 3600 秒)
```

- `build-perf` (android) の後、作業コピーは Android 向きのまま残る。次の `scripts/unity-win.sh build` が Windows 向きに戻す (その分だけ時間がかかる)。
- `build-perf` のログに `perf build の設定 (Android): graphicsAPIs=… optimizedFramePacing=True frameTimingStats=True colorSpace=Linear` が出る。測った数字と一緒に残す。
- 計測用のビルドは release なので、CSV の `drawcalls`・`setpass`・`triangles` は空欄になる (Development ビルドでしか取れない)。箱庭の部品・三角形の数は要約の `diorama` に出る。

## 4. PC: perf=120 (GPU の p95 を記録する)

**他の撮影 (pshots.sh・shots) と同時に回さない** (GPU を取り合って数字が狂う)。`DET=1` は付けない (det は描画を実時間から外すので計測にならない。付けると警告が出る)。
`SHOT_TIMEOUT` は shots の既定 (300) より長く取る (起動 約50秒 + 120秒 + 終わり)。

```bash
cd /home/yosuke/projects/deck-rogue-proto
OUT=/home/yosuke/projects/deck-rogue-proto/unity/Shots/hd2d/perf-pc   # git の管理外
run() {  # $1 = 名前  $2 = STATE  (残りの環境はそのまま渡る)
  mkdir -p "$OUT/$1"
  SHOTS_OUT="$OUT/$1" PLAYER_EXE=perf-win/DeckRogue.exe SHOT_TIMEOUT=420 PLAYER_ARGS="-perfprobe" \
    STATE="$2" scripts/unity-win.sh shots state 4242 > "$OUT/$1.out" 2>&1
}
run pc-wolf  "phase=combat;enemy=enemy_wolf;hd2d=slice;perf=120"
run pc-quad  "phase=combat;enemy=enc_biting_scrolls_quad;hd2d=slice;perf=120"
run pc-dolls "phase=combat;leader=leader_white;enemy=enemy_wolf;perms=white_perm_squire,white_perm_shieldmaiden,white_perm_choir,white_perm_banneret,white_perm_band,white_perm_hound,white_perm_squire,white_perm_shieldmaiden,white_perm_choir;hd2d=slice;perf=120"
run pc-wolf-old "phase=combat;enemy=enemy_wolf;perf=120"          # 今の舞台 (stage=old) = 比べる元
# スマホの段を PC で (参考。PC の GPU の数字で、スマホの数字ではない)
SHOT_W=1920 SHOT_H=886 UISCALE=1.6 run ph-wolf-t0 "phase=combat;enemy=enemy_wolf;hd2d=slice;tier=phone;perf=120"
```

- 1本あたり約3分。長い時は Bash の `run_in_background` で回して、`$OUT/<名前>.out` の `player exit=` を待つ。
- 出る物: `$OUT/<名前>/perf-<日時>.csv`・`perf-<日時>.summary.json`・最初の1枚の PNG。
- ログの `[PerfProbe] end secs=… gpu_p95=… cpu_p95=… frame_p95=…` が要約の1行。`frame_timing_stats` が false なら GPU は空欄 (計測用の exe でなかった)。
- PC の品質レベル (Ultra) は vSync 1 なので fps はモニタのリフレッシュで頭打ちになる。GPU 時間はそれと別に取れる (`refresh_hz` を一緒に書く)。

## 5. S25 (実機): 15 分

### 5-0. 端末の用意と記録すること
- 実機: Galaxy S25 (SM-S931Z)・serial **`RFCY11047JW`**。`adb devices` にオフラインの `emulator-5562` も出るので **`-s` 必須**。
- 画面のロックを外しておく (ロック中は起動しても前に出ない)。明るさを固定する (例 50%。自動の明るさは切る)。
- 記録する: 部屋の温度の目安・始めの電池の残量と温度 (`adb shell dumpsys battery | grep -E "level|temperature"`。温度は 1/10 度)・充電中かどうか・Game Booster の設定 (本体と同じく「ゲーム」と見なされる = AndroidIsGame)。
- **充電しながら測らない** (充電で端末が温まり、数字が悪く出る)。起動を確かめたらケーブルを抜いてよい (記録は端末に残る。CSV は1秒ごとに書き足す)。抜けない時は `battery_status` の列に Charging と残るので、それを書き添える。
- 前の計測から続けて測る時は 10 分以上冷ます (電池の温度が始めと同じくらいに戻るまで)。

### 5-1. 入れる
```bash
cd /home/yosuke/projects/deck-rogue-proto
ADB=/mnt/c/Users/yosuke/AppData/Local/Android/Sdk/platform-tools/adb.exe
S=RFCY11047JW
PKG=com.deckrogue.proto.perf
"$ADB" devices
ADB_SERIAL=$S scripts/unity-win.sh install perf              # Build/DeckRogue-perf.apk を入れる (本体は消えない)
"$ADB" -s $S shell cmd package resolve-activity --brief $PKG   # 起動の画面の名前を確かめる
# → com.deckrogue.proto.perf/com.unity3d.player.UnityPlayerGameActivity (androidApplicationEntry 2 = GameActivity)
```

### 5-2. 引数を渡す (どちらか1つ)
渡す中身 (段0・狼1体・15分):
`-autopilot state -seed 4242 -state phase=combat;enemy=enemy_wolf;hd2d=slice;perf=900 -perfprobe`
- 段1〜3 は STATE に `;look=look_act1_phone1` (〜3) を足す。`tier=phone` は実機の既定なので書かなくてよい。
- 伸びしろを見る時だけ `-targetfps 60` を足す (門の計測は 30 のまま)。
- `-perfprobe` は最後に置く (後ろに秒を書くとその秒で区間を切る。書かなければ perf の 900 秒)。

**(a) intent の `-e unity` で渡す** (まずこちらを試す):
```bash
"$ADB" -s $S logcat -c
"$ADB" -s $S logcat -G 16M                                   # 15 分ぶんのログが残るように広げる
"$ADB" -s $S shell "am start -S -n $PKG/com.unity3d.player.UnityPlayerGameActivity -e unity '-autopilot state -seed 4242 -state phase=combat;enemy=enemy_wolf;hd2d=slice;perf=900 -perfprobe'"
sleep 40
"$ADB" -s $S logcat -d -s Unity | grep -E "\[PerfProbe\]|\[Autopilot\]|\[HD2DFlags\]|\[StageLook\]" | head -20
```
届いた印: `[PerfProbe] 準備 (-perfprobe=True … perf=900 … targetFrameRate=30 …)` と `[Autopilot] perf start secs=900`。
届いていない印: タイトル画面が出て `[Autopilot]` の行が無い。→ `"$ADB" -s $S shell am force-stop $PKG` して (b) へ。
(STATE の `;` は端末の shell が区切りに読むので、`am start …` 全体を `"…"` で、`-e unity` の中身を `'…'` で囲む。)

**(b) 引数ファイル `perf-args.txt` で渡す** (a が届かない時):
```bash
F=/mnt/d/deck-rogue/perf-s25/perf-args.txt
mkdir -p "$(dirname "$F")"
cat > "$F" <<'EOF'
# 計測用 APK の引数 (空白で区切った起動引数。"…" で囲めば空白を含められる。# の行は読み飛ばす)
-autopilot state -seed 4242 -state "phase=combat;enemy=enemy_wolf;hd2d=slice;perf=900" -perfprobe
EOF
"$ADB" -s $S shell monkey -p $PKG -c android.intent.category.LAUNCHER 1   # 一度起動して置き場のフォルダを作らせる
sleep 10; "$ADB" -s $S shell am force-stop $PKG
"$ADB" -s $S push "$(wslpath -w "$F")" /sdcard/Android/data/$PKG/files/perf-args.txt
"$ADB" -s $S logcat -c; "$ADB" -s $S logcat -G 16M
"$ADB" -s $S shell monkey -p $PKG -c android.intent.category.LAUNCHER 1
sleep 40
"$ADB" -s $S logcat -d -s Unity | grep -E "\[Autopilot\] 引数ファイル|\[PerfProbe\]|\[Autopilot\] perf start"
```
届いた印: `[Autopilot] 引数ファイル … から 7 個の引数を足した` と `[Autopilot] perf start secs=900`。
**測り終えたら引数ファイルを消す** (`"$ADB" -s $S shell rm /sdcard/Android/data/$PKG/files/perf-args.txt`。残すと計測用の APK を開くたびに自動で走る。本体の APK は読まない)。

### 5-3. 待つ
- 起動 + 900 秒 (約16分)。終わると自動でアプリが閉じる (`Application.Quit`)。画面は消えない (PerfProbe が NeverSleep にする)。
- 途中で画面に触らない・通知を開かない (裏へ回ると区間がそこで切れて要約が書かれる)。
- ケーブルをつないだままなら、途中の様子は `"$ADB" -s $S shell "tail -n 3 /sdcard/Android/data/$PKG/files/perf/perf-*.csv"` で見られる。

### 5-4. 回収
```bash
OUT=/mnt/d/deck-rogue/perf-s25/$(date +%m%d-%H%M)-t0-wolf        # 段と場面を名前に
mkdir -p "$OUT"
"$ADB" -s $S pull /sdcard/Android/data/$PKG/files/perf  "$(wslpath -w "$OUT")"
"$ADB" -s $S pull /sdcard/Android/data/$PKG/files/shots "$(wslpath -w "$OUT")"
"$ADB" -s $S logcat -d > "$OUT/logcat.txt"
grep " E Unity" "$OUT/logcat.txt" | head -40          # 例外 (IL2CPP の壊れはここに出る。画面にも ErrorOverlay が貼る)
grep -E "\[PerfProbe\]|\[Autopilot\] perf|\[StageLook\] Apply|\[Diorama\]|\[TiltShift\]" "$OUT/logcat.txt"
"$ADB" -s $S shell dumpsys battery | grep -E "level|temperature"   # 終わりの電池
```
- `adb pull` の行き先は Windows の場所 (D:) にする (adb.exe は Windows のプログラム)。リポジトリへは写すだけ (`unity/Shots/hd2d/perf-s25/` = git の管理外)。
- `[StageLook] Apply look_act1 [look_act1+look_act1.char+look_act1.dof+look_act1.phone]` の [] の中に `.phone` (と段の変種) があれば、スマホの段で描いている。

### 5-5. 読む
```bash
python3 - "$OUT"/perf/perf-*.summary.json <<'EOF'
import json, sys
for p in sys.argv[1:]:
    d = json.load(open(p))
    print(p)
    print("  秒", d["secs"], "fps", d["fps_mean"], "GPU", d["gpu_ms"], "CPU", d["cpu_ms"])
    print("  終わり", d["tail"])
    print("  1秒ごとの fps", d["fps_1s"])
    print("  温度", d["thermal"])
    print("  門", {k: v for k, v in d["gate"].items() if not k.startswith("_")})
    print("  段", d["tier"], d["look"], "品質", d["quality"], "target", d["target_fps"], "Hz", d["refresh_hz"], "API", d["graphics_api"])
EOF
```
- **門 (保留)**: `gate.secs_ok`・`fps_ok`・`gpu_p95_ok` が true、あわせて `tail_fps_ok`・`tail_gpu_p95_ok` (終わりの 5 分) も true なら「15分で30fps・GPU の p95 25ms 未満」。`fps_ok` の目安は平均 29.0 以上 (30 を目指して揃えると 29.9〜30.0 になる)。
- `fps_1s.secs_below_28` = 28fps を割った秒の数。平均が 30 でも、ここが多いとカクつく。
- `thermal.status_max` (0 なし・1 軽い・2 中 = 抑え始める・3 重い・4 危険)・`moderate_from_s` (中に入った秒)・`headroom_last` (1.0 で抑え始める目安)。GPU が終わりの 5 分で遅くなっていて、同じ頃に status が 2 以上なら、熱で抑えられた。
- `gpu_ms` が null なら GPU 時間が取れていない (`frame_timing_stats` が false = 計測用の APK でない、か端末が対応していない)。

### 5-6. 段を上げる
段0で門に届かなければ、冷ましてから段1 (`;look=look_act1_phone1`) → 段2 → 段3 の順に同じ 15 分を回す。どの段で届いたかを記録する。**段3 でも届かない時は、次の手 (§2) をユーザーに確かめる。**
比べる元として、今の舞台 (STATE から `hd2d=slice` を外す = stage=old) も1回 15 分回しておくと、見本でどれだけ重くなったかが分かる。

### 5-7. 片付け
- 引数ファイルを消す (5-2 b)。計測用の APK は残してよい (本体とは別のアプリ)。消すなら `"$ADB" -s $S uninstall $PKG`。
- 回収した物は `unity/Shots/hd2d/perf-s25/` へ写し、下の §7 の表に書く。

## 6. CSV と要約の中身

**CSV** (`perf-<日時>.csv`・1秒1行。列は後ろに足すだけ):
`t_s`(区間の頭からの秒)・`frames`・`fps`・`cpu_p50/95/99_ms`・`gpu_p50/95/99_ms`・`frame_p50/95/99_ms`(その1秒の中の割合)・`drawcalls`・`setpass`・`triangles`(release では空欄)・`plays`・`turns`・`rejumps`(自動で戦った数)・`thermal_status`・`thermal_headroom`(2秒に1回)・`battery_level`(0〜1)・`battery_status`。Android 以外と読めない値は空欄。

**要約** (`perf-<日時>.summary.json`・`schema: hd2d-perf/1`):
`secs`・`frames`・`fps_mean`・`cpu_ms`/`gpu_ms`/`frame_ms` ({p50, p95, p99, n})・`frame_timing_stats`・`screen`・`quality`・`target_fps`・`vsync`・`device`・`gpu`・`graphics_api`・`identifier`・`flags`・`det`・`plays`/`turns`/`rejumps`・`tier`・`look`・`look_sources`・`refresh_hz`・`sleep_timeout`・`diorama` (部品・三角形・Renderer・材質・動く物)・`fps_1s` ({p5, p50, min, secs_below_28, n})・`tail` (終わりの 300 秒。区間が 600 秒未満なら後ろ半分: {from_s, secs, frames, fps_mean, cpu_ms, gpu_ms, frame_ms})・`thermal` ({status_first/last/max, moderate_from_s, headroom_first/last/max, battery_first/last})・`gate` ({secs_min 900, fps_mean_min 29, gpu_p95_max_ms 25, secs_ok, fps_ok, tail_fps_ok, gpu_p95_ok, tail_gpu_p95_ok})・`csv`。

## 7. 結果の記録 (ここに書き足す)

| 日付 | 端末 | 場面 | 段 | 秒 | fps 平均 | fps 終わり5分 | 28fps割れの秒 | GPU p50 / p95 | GPU p95 終わり5分 | CPU p95 | 温度 max・中に入った秒 | 電池 始め→終わり | 門 | メモ |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 2026-10-01 | PC (perf-win・RTX 2070・D3D12・1920×1080・Ultra・vSync 59.95Hz) | S-wolf (hd2d=slice) | — | 120 | 59.58 | 59.53 (後半60秒) | 0 (最低 48.2) | 3.54 / 9.61 | 9.65 | 16.81 | — | — | 記録のみ | 箱庭 部品313・三角形 96,723・描く物 39。fps は vSync の頭打ち |
| 2026-10-01 | PC (同上) | S-quad (4体) | — | 120 | 59.75 | — | 0 (最低 51) | 4.00 / 6.69 | 9.61 | 16.81 | — | — | 記録のみ | |
| 2026-10-01 | PC (同上) | S-dolls (ひなた・人形9) | — | 120 | 59.55 | — | 0 (最低 55.2) | 3.63 / 6.20 | 9.68 | 16.82 | — | — | 記録のみ | |
| 2026-10-01 | PC (同上) | S-wolf 今の舞台 (stage=old) | — | 120 | 59.74 | — | 0 (最低 53) | 2.94 / 6.01 | 6.11 | 16.81 | — | — | 記録のみ | 比べる元 (Linear 版の今) |
| 2026-10-01 | PC (同上・1920×886・UI 1.6倍・tier=phone = 品質 Medium) | S-wolf | 段0 | 120 | 59.77 | — | 0 (最低 51.9) | 4.68 / 4.83 | 4.82 | 16.82 | — | — | 記録のみ | PC の GPU での参考 (スマホの数字ではない) |
| 2026-10-01 | PC (同上・tier=phone) | S-wolf | 段1 | 120 | 59.71 | — | 0 (最低 51) | 4.62 / 4.80 | 4.82 | 16.81 | — | — | 記録のみ | 描く物 39 → 32・三角形 −38 (霧と筋の面 7 枚を組まない = dropKinds が効いた)。PC の GPU では差 0.03ms |
| (未測) | S25 | S-wolf | 段0 | 900 | | | | | | | | | 保留 | |

- PC の記録の置き場: 作業場 `scratchpad/hd2d/w4/perf-pc/<名前>/perf-*.csv・*.summary.json` (W4 の統合。git の管理外)。「fps 終わり5分」の列は 120 秒の区間では要約の `tail` = 後半 60 秒。
- PC の GPU p95 は p50 の 2〜3 倍に跳ねる (wolf 3.5 → 9.6)。PC の門は無い (記録だけ) ので追っていない。S25 で同じ形が出たら、どの描く段で跳ねるかを Development ビルドで見る。

## 8. 分かっていない所・残っている口

- **`-e unity` の引数が `Environment.GetCommandLineArgs()` に届くか** は実機で確かめていない (届かなければ 5-2 b)。
- **段1 の「描くのを止める」口**: W4 の統合で `Diorama.Build` に足した (設計図の `diorama.dropKinds` の種類の部品を組まない。ログに `[Diorama] 組まない部品の種類` の1行)。段1 から霧と筋の面の描く重さが消える。
- **共有シートの FileProvider（直した・2026-10-01）**: `unity/Assets/Plugins.meta`・`Plugins/Android.meta`・`Plugins/Android/DeckRogueShare.androidlib.meta`（PluginImporter・Android を有効）をリポジトリに入れた。同期 (rsync --delete) が .meta を消さなくなり、作業コピーが androidlib を取り込み続ける。直した後の本体の APK と計測用 APK の manifest に `com.deckrogue.proto.fileprovider`・`com.deckrogue.proto.perf.fileprovider` があることを確かめた。APK を出す前の確かめ方は下の元の記録のコマンドのまま。以下は元の記録: **計測用 APK に共有シートの FileProvider が入っていない** (W4 の統合で見つけた。`DeckRogue-perf.apk` の manifest に `…perf.fileprovider` が無い)。作業コピーの Bee が `DeckRogueShare.androidlib` を「使われていない」として落とした (`unity/Assets/Plugins/Android` の .meta がリポジトリに無く、同期のたびに作り直される)。計測は `adb pull` で回収するので要らないが、**本体の APK を次に出す前に** manifest の `com.deckrogue.proto.fileprovider` を確かめる (`unzip -p Build/DeckRogue.apk AndroidManifest.xml | strings -el | grep fileprovider`)。9/30 の本体の APK には入っている。
- **IL2CPP での PowerManager (温度) の読み**は実機で確かめていない。読めなければ列が空欄になるだけ (ログに `[PerfProbe] 端末の温度を読めない`)。
- Galaxy の Game Booster (ゲームの最適化) が計測用の APK にも掛かるかは端末で見る (本体と同じく「ゲーム」扱い)。
- APK を出す前の実機の確認 (CLAUDE.md): `adb -s <serial> logcat -d --pid=$(adb shell pidof com.deckrogue.proto.perf)` の `E Unity`。計測用の APK は自動で閉じるので、終わった後は 5-4 のように `logcat -d` 全体から探す。

# HD-2D 段2（幕2・幕3 の箱庭）の計画 — 分析と裁定を受けた版

2026-10-03（まとめ役 Fable 5.1）。元にした物＝分析書 `docs/design/hd2d-stage2-analysis-2026-10-02.md`（第1段 7 レーン・提案 A/B × 2 幕・反証 3 役 × 2 幕・動きの分析）と、ユーザーの裁定（同 §10 の Q1〜Q9）。
**この計画の時点でコード・データ・絵・設計図は三周目の最終（`9fa71df` 以降の docs だけ）のまま。段1 はこの計画から始める。担当は Opus 5.5（引き継ぎ `docs/design/hd2d-stage2-impl-prompt-2026-10-02.md`）。**

## 0. 裁定（2026-10-03。変えない。変えたい時は理由を添えて聞く）

1. **進める順＝幕2 から。幕3 は幕2 の試しのビルドが通ったら同じ型で半歩遅れで並走**（C・B・D は別ファイル）。
2. **人工の大物の完全な 3D を段2 に入れる**（幕2＝支保工・歩廊・レール・坑口・階段／幕3＝大門 arch・柱 pillar・階段・胸壁。像・碑・彫刻はキャラ級＝半立体のまま）。
3. **動き（寄り・技の光・暗転・敵の大技のフラッシュ・火花・ピントの連動）を「レーン M」として段2 に入れ、判定は動画の左右並べ（本家 clip｜うちの mp4・5 場面×2〜4 秒）で下す。**
4. **幕2/3 の敵 37 体の描き直し（320 生成）は入れない＝見本を撮ってから別裁定**。計画と判定に「敵の描き直しが無い限り 1〜2 体の場面は 3.5 が上限」と明記する。
5. 幕2＝帯型（壁の照明）・暖色は洞窟型 2〜5%・暗部は紺緑・見下ろし 5° のまま・座席の床に砂の粒（art-bible §3-3 の幕2 の例外）。
6. 幕3＝物差しを「ot7 の戦闘の作り＋Tomb の材質」に読み替え・光は「主人公の真後ろに斜めの柱・足元が頂点」・幕1 の裁定の文言を「地平線のある幕」と「閉じた幕（床の光溜まり＋縦の柱）」に分ける（art-bible §5-6 に追記）。
7. 既定（`ApplyActDefault`）へ入れるのはユーザーの判定（5 場面のシート＋動画の左右並べ）で幕1 と同等以上（3.5）の時。それまでは旗 `dioramaacts=` だけ。
8. 読み替え 6 件: ①縁 1px・r3 の UI・LitUnits・MenuShade・desk-shade は「箱庭なら全幕で効く。今の舞台には効かない」②幕3 の白い筋＝「上の裂け目の露頭から降りる脈の光」③`gen_art.py`・keyflip は触らない（幕2/3 の生成器が import して色表を差し替える）④回帰の基準を HEAD で撮り直してから移行⑤MenuShade の α は幕ごとの口（look・無ければ今の定数）⑥絵の出力は `Art/stage/actN/` へ別名（今の舞台の `Art/tiles`・`Art/props` は上書きしない）。
9. 本家の戦闘画（Steam 版・坑道と Tomb/Everhold）はユーザーが撮って送る。来るまで ot2／Tomb＋ot7 で進め、来たら帯・床・暖色の数字だけ差し替える。
10. 幕1 の裁定はそのまま: 主人公 62・1 ドット 4px・カメラ 22°/5°/7°・足元の線 PC 0.36／スマホ 0.525・札は夜色・外注なし・縁 1px・接地影は薄く広く・このはの色は 0 周目・合否は目・幕1 の見本は 1 画素も変えない・PixelLab は 10/7 まで呼ばない（幕1 の四周目 約 590 が先。幕2/3 の舞台は約 32〜40）。

## 1. 目標と合否

- **目標の画**: 幕2＝`~/.cache/deck-rogue/hd2d-ref/steam/ot_921570_2.jpg`（小物の見本 `hd2d-stage2/targets/web/ot1web_derelict_mine.jpg`）／幕3＝`hd2d-stage2/targets/web/ot1web_tomb_imperator.jpg`（戦闘の作りと数字は `ot_921570_7.jpg`・水面は `ot1web_tomb_imperator_dungeon.jpg`）。
- **合否は目で**（5 場面の静止画のシート＋動画の左右並べ）。数字は「どの層が足りないか」の補助。見込み（反証）: 幕2 3.1〜3.3・幕3 3.25（動きなし）。目標は幕1 と同等の 3.5（動きを入れて）。4 に要る物（敵の描き直し・本家の戦闘画・10/7 の壁の絵）は段2 の外と明記。
- **幕2 の光の合否（3 条件・目で）**: ①主人公の列（±125px・10 行の最大）の最大が壁にあり床の 1.15 倍以上 ②壁の上段（行 130〜290）≤60 ③灯の真下の床は無彩色（彩度 ≤0.12）。歯止め: p95 ≤174・主人公の体 ≥ 床・紙の UI がいちばん明るくならない。
- **幕3 の光の合否（3 条件）**: ①主人公の列で足元（0〜40px 上）が最大・上へ 1 本の勾配 ②その真後ろに柱（幅 ≈400px 半値・斜め 15〜20°）・柱の中÷外 ≥3（帯の幅を書いて測る） ③柱の外の壁は中央値 ≤25・床の横 7 分割の中央÷端 ≥3。
- **動きの合否（§7 の動画）**: 寄りの間の壁 ×2 以上・暖色の割合 0.6 以上（幕2）・技の光 0.9 秒（保つ 0.3）・暗転 ×0.5・待機のカメラ 0。軸は ①光が舞台に当たる ②寄りと間 ③粒と火花 ④暗転→溜め→当たり→余韻（①②を重く）。
- **壊していないか**: 幕1 の見本（`s2-slice`・`s2-k0v`・`s2-ui`・`s2-regress(-ph)`）と今の舞台（`s2-old`・`s2-old23`・`s2-regress-old(-ph)`）と旗なし起動（`s2-apk`・`s2-screens`）の画素一致（差 >2 の画素が 0.1% 超＝0 件）・`s2-mixed`（`act=2/3 × stage=diorama` が今の舞台に落ちる）・`Diorama.Check` result=OK・配置の検査 0 件・PC の GPU p95（old と箱庭と phone tier）。S25 は端末があれば幕1 を先に 1 回。
- **物差しは 1 組に**: 段2 の最初に幕2/3 の `hideui`／`unitsonly` の基準を撮り、暗い画素・中央値は hideui の全画素（targets の `measure.py` と同じ式）、帯は「主人公の列 ±125・10 行の最大」と「全幅 3% 窓の最大」を併記。本家の数字は `targets/metrics.json`（ot2・Tomb・ot7）。

## 2. レーン（ファイルは重ならない。三周目の S/D/C/B/E/A/F に M と「口」を足す）

全レーン共通: `git status` がきれい（先頭はこの計画の commit 以降）／ルールエンジン（`src/engine`・`unity/Packages/com.deckrogue.engine`）を触らない／幕1 の見本と今の舞台を 1 画素も変えない（新しい振る舞いは `HD2DFlags.DioramaHere` か look の口の枝）／生の UI 色を足さない／Unity・PixelLab を使わない（統合だけ）／`scripts/typecheck-unity/run.sh --quick` で確かめる／commit はしない（統合がレーンごとに 1 つ）／値の出発点は分析書と提案の下書き（`~/.cache/deck-rogue/hd2d-stage2/B/act2/propA/look_act2.draft.json`・`act2_layout.draft.json`・`B/act3/propA/propA-look_act3-draft.json`・`propA-layout-draft.json`・`B/act3/propB/geom3.txt`・`B/act2/propB/geom_b2.md`）。

| レーン | 持ち場 | 段1（幕2）でやること | 段1b（幕3） |
|---|---|---|---|
| **口（移行）** | `Game/HD2DFlags.cs`・`Stage.cs`（`Paint`・`WantDiorama`・`PaintDiorama` の失敗時・`SetFxForAct`）・`BattleScreen.cs:24・1958`・`StageCamera.cs:68`・`StageFx.cs:73`・`StageUnits.cs` 5 か所・`StageLook.cs:316/713`・`DioramaLayoutTool.cs` | migrate §3 の 12 件: 旗 `dioramaacts=1|1,2|1,3|all|none`（既定 `1`・束に入れない・`Reset` で `1`）・`StageAct`（Paint の頭。Autopilot が `act=` を読んだ所でも先に書く）・`DioramaHere = StageMode==Diorama && DioramaOn(StageAct) && !DioramaFallback[act]`・11 か所を差し替え・`WantDiorama(act)`・`PaintDiorama` の門は「設計図 **と** `look_act<N>.json` の両方」（無ければ fallback で今の舞台にそろえる）・`SetFxForAct` は箱庭の時だけ look の `fx` 表が勝ち、無い名は今の規則（`!dio` の枝は 1 文字も触らない。`look_act1.json` の fx は `leaves:false` だけ）・メニュー「幕2/3 を組む」。keyflip・gen_art.py は触らない。**基準の撮り直し（§3 手順 0）の後に入れる** | — |
| **S シェーダとメッシュ** | `Game/Diorama.cs`（`BuildPart` の switch）・`DioramaMesh.cs`（新しい形を足すだけ。既存の Slab/Block/Rock/Marker/Rig は触らない）・`Resources/Shaders/StageModule.shader`（技の光の受光の係数の口）・新シェーダは別ファイル | 新 kind: **`rail`**（レール 2 本＋不等間隔の枕木）・**`arch`**（大門・半アーチ・池の橋）・**`pillar`**（角柱＋柱頭）・**`lights[]`**（look の影なし点光源 ≤4・`_rig` の子・`RigShadowCount` に数えない＝`EnforceShadowCap` と `hitLight` の貸し借りに触れない）・暈は `fog` kind（丸）か小さな新 kind（`card`＋glow は四角になる）・block の側面 AO を切る口（`ao: false`）。**天井は作らない**（block/slab に底の面が無い。底を持つ形は 10/7 以降 0.5 日）。**`water` は試し撮りの後**（`StageWater.shader`・`Stage.Reflection` は触らない＝今の舞台が読む。箱庭の water は `EnsureReflection(y)` を呼ぶだけ・材質は別インスタンス）。`Diorama.Check` の門は設計図の `gates`（partsMin 160〜180・materialsMax 8）。半立体の「技の光にだけ強く受ける」係数（M と対） | 同じ kind を幕3 でも使う |
| **D 絵** | `docs/pixellab/hd2d-act2/{gen_items.py,palette-act2.json}`・`hd2d-act3/`（`hd2d-act1/gen_art.py` を import して `PAL`/`PALRGB` と出力の根を差し替える。幕1 のスクリプトは触らない）・`Art/stage/act2/{tiles,relief,litter}/`・`index.json`・`keyflip.json`（新しい名前だけ） | **色表を先に**: `stage-palette.py build palette-act2.json` を既存の `Art/props/act2_*`＋本家 ot2 の切り抜き（暗部 (21,44,50)・灯の真下の無彩色の床）から／座席の土 4 種（コード生成・砂の粒 細かさ 40〜90・等間隔にしない）・小札（砂利・小石・歯車・木くず）／既存 42 枚: 26 枚そのまま・16 枚は `relief-clean.py` で足元と皿を切る・等角投影の 13 枚は足元を切るか 10/7 まで置かない／炉・結晶・提灯の明るい部分を `_e.png` に切り出す／`sprite-normals.py normals-stage`／タイル `act2_cliff/stone` は `tile-calm` で 1/3〜1/5（`Art/stage/act2/tiles/` へ別名） | palette-act3.json（本家 Tomb の壁の暗部 (8,42,53)・柱の白 (156,160,147) を先に）・大石板 calm・29 枚の半立体化・発光（結晶・紋・裂け目・脈・灯） |
| **C 設計図** | `Resources/Stage/act2_layout.json`・`docs/design/hd2d-stage2/layout-gen/{gen_act2.py,place2.py}`（`hd2d-slice/r2-layout-gen/place.py`・`layoutio.py` を import。`place.py:41` の SP の直しは別 commit） | 分析書 §4-1 の層: slab の段（棚 0・二段目 +1.8・一段目 +0.9・手前 −1.4 の板張り）／壁＝3D 岩棚 block×3（棚 s 10.9 y 1.8 h 1.1／本体 s 12.4 y 1.8〜4.4／上段 s 12.0 y 4.4〜6.3・張り出し 2〜4 unit・上段にも岩の模様・`ao:false` の口）／壁の前の `mist` 1 枚（y 1.5〜4.5）／提灯 2＝`lights`（y 3.3 前後・t をばらす）／支保工の柱 3（t −5.6・0・≥17）＋梁 2（y 5.75）＋持ち送り／歩廊（block/plank＋fence）／レール（rail）／坑口（marker×2＋block の梁＋黒い card・t 19.5 s 11.3）／小物は B の置き場 70（`StageActs.cs` の t・s・高さ。炉は t −6.2）／鍾乳石 2〜4（t ≥16・x 1200〜1550 の真上は空ける）／棚の縁 s ≤−5.0・frame 3（支保工 1・岩 2）／`sources`・`gates`（partsMin 160・materialsMax 8）・`phone`（hide: 天井・壁の上段・紐・左の歩廊・下の坑道・床の霧の板・右の岩／t: 炉・左の提灯）。**規則**: 座席の帯（t −8.5〜13・s −2.6〜2.8）は高さ 0・部品なし／全座席の足元の通り（敵 ±140px・主人公 ±60px・人形 x 552〜911）の真後ろに背の高い物を置かない（x は Cam で出す。t の順で読まない）／意図の札の箱と幕ボス 160 の頭（x 1200〜1550・行 0〜140）を空ける／水路・桟橋は組まない。`place2.py` が全部を検査 | `act3_layout.json`・`gen_act3.py`: 立面 4 段（T1 +1.0 s 9／T2 +2.0 s 13／T3 +3.0 s 17.5／T4 +4.0 s 22・水路は埋める）・奥の壁 s 27 y 4〜11（back 28）・縞を断つ部品（斜めの大階段を右 1/3・控え壁 2・崩落 1 セル）＋変種（T2〜T4 の高さを t で変える）・大門 arch（門の軸）・柱 pillar 2（画面の中に）・館 block・灯柱 marker（t ≥6.5）・篝火 2（画面の中・fire2 t 12.5）・像 1・折れ柱 1・結晶 1・瓦礫 2・壺 1（**小物 8 以下**）・鎖 1〜2・池（pond-bed の暗い底と縁の立面だけ・水面は後）・phone（hide: 壁2・櫓・裂け目・池・機械の庭・mist-far） |
| **B 光と空気** | `Resources/Stage/look_act2{,.char,.dof,.phone}.json`・変種 `look_act2_<名前>.json`・`StageLook.cs`（`lights[]`・`fx` 表・MenuShade の α の口・`charKeyDir` の既定は幕1）・`Stage.cs` の `Moondust()`／`SetFxForAct()` の箱庭の枝だけ | 分析書 §4-2 の台本: 平行光＝風穴 0.15〜0.22 [52,34,0]（影）／舞台の灯 ほぼ白 (0.92,0.90,0.86) pool level 1.35 mask outside 0.08／炉＝backlight 影なし／提灯 2＝lights／霧 14→40・色 [0.30,0.37,0.44]・far2 20〜36・lobe は補助／nightGrade [0.11,0.21,0.24]・**hi 0.08〜0.12**／envGrade tint [0.98,1.12,1.28]／stageVignette top 0.22／露出 0.45（変種 0.4／0.5）・bloom 1.15／塵＝StageDust 暖白 20（座席〜壁）・火の粉の位置を fx 表に。`.char`＝`look_act1.char.json` の写し（このはの 0 周目の値は変えない）・`.dof`＝写し（lensFar 3.0／2.4 の変種）・`.phone`＝`look_act1.phone.json` の 7 つの鍵（fog.lobe.powerV・stageVignette・backlight.shadow・shadow×4・camera.msaaMax・tiltShift.halfRes・envGrade.tint）を写してから差分（帯を壁 y 1.1〜2.6 へ・霧の r 1.393）。MenuShade の α の口 | `look_act3*`: 平行光 上寄り [64,18,0] 0.5／舞台の灯 青白／柱＝backlight（point・t −1〜−1.6・s 12.4・y 6・range ≤16・影つき）＋shaft 2（dir に横成分＝斜め 15〜20°・gain 1.6）／篝火 2・灯柱・割れ目＝lights／霧 start 12・end 58（壁で 0.4）・lobe off・far2 は壁の手前 26 で止める／nightGrade (8,42,53)／envGrade 0.55／露出 0.25（変種 0.35）／stageVignette top 0／光溜まり＝主人公側（t −5〜−1）＋2 つ目／粒は柱の中だけ 20〜40 |
| **E キャラ** | `Resources/Stage/look_act2.char.json`・`look_act3.char.json` の `art.enemy_*` だけ | 写しに古機 2 体（金切り顎・砥石の巨像）の上書き（whiteCap・emission）を撮ってから | 古機 12 体 whiteCap 0.85・rim 0.35・blackLift 0.45（撮って決める）・亡者 4 体 fog 0.4 |
| **A カメラ・UI・座席** | `Game/StageSeats.cs`（苔の産み手の席・合成獣の残機＝UI 層だけ・engine の `spawnEnemies` は触らない）・`BattleScreen.cs`（MenuShade の α を look から読む 1 行） | 幕2 を撮ってから: 苔の産み手（席 6）が重なれば箱庭の時だけ 5 体目から詰める／番人の絡繰 48 の表 | 合成獣の三の相（残機）は親の席に |
| **M 動き** | `Game/StageFx.cs`（`HitLight` の値と「寄りの間だけ」の口）・`StageCamera.cs`（第 2 のカメラ＝寄り）・`Tween.cs`／`Presenter.cs`（暗転・敵の大技のフラッシュ・火花の量）・`BattleView.cs`（紙の UI の退避）・`Resources/Shaders/StageModule.shader` と `ReliefMesh` の受光の係数（S と相談）・look の `hitLight`／`zoom` のキー。**箱庭（`DioramaHere`）の時だけ**。今の舞台の演出 R1〜R16 は 1 画素も変えない | 分析書 §8 の 1〜7: ①**寄り**＝大きい当たり（15 以上・急所）0.45 秒・X 札/全体の最後の 1 発/とどめ 0.9 秒・幕ボスのとどめ 1.8 秒。切り替えは 1 フレーム（トゥイーンしない）、第 2 のカメラは対象の座席の 1.2〜1.35 倍・足元の少し下から見上げ・対象の背後の壁が画面の右半分に入る向き。戻しは切り替え。**紙の UI（帳面・手札・自分の札）は寄りの間だけ退避**（夜色の札は残す）②**技の光**＝`hitLight` 強さ 2.6→10〜15・range 5→10・dur 0.26→1.0・hold 0.35（寄りの間だけ。寄らない当たりは今のまま）・色は技の色（物理＝暖色・呪文＝青緑）・半立体と壁は「技の光にだけ強く受ける」係数（舞台の灯の二重の陰影は避ける）③**暗転**＝大技の札を出した瞬間に舞台の灯と平行光を ×0.3〜0.5 に落とし技の後まで保つ（キャラの固定キーは落とさない）④**敵の大技**（予告つき大技だけ）＝赤い合図 0.17 秒→暖のフラッシュ 0.15 秒→白 0.3 秒→技の色の帯 1.2 秒（舞台の板だけ・紙は染めない）⑤**火花** 50〜100 粒・放射・0.5 秒 ⑥**ピント**＝寄りの間だけピントの帯を対象の深度へ。撮影キー: `play=attack;playshots=120;playevery=1` 等（1 コマ＝1/60 秒・DET）。OT2 の回転は見送り | 同じ器。幕3 は柱と壁で |
| **F 物差しと撮影** | `scripts/hd2d-states/s2-*.txt`（`~/.cache/deck-rogue/hd2d-stage2/migrate/states/` の下書きから。`s2-mixed` に幕3 の 3 行・`s2-old23` に 5 場面と PH の hideui/unitsonly・`s2-dio23` に enemy= 名指しと変種と saveexit・`perf=120` を 4 本）・`scripts/hd2d-a2-targets.py`／`hd2d-a3-targets.py`（新。幕1 の `hd2d-r3-targets.py`・`gates.json` の既定は触らない）・`scripts/hd2d-pixdiff.py`（migrate の下書きを写す。scipy が無いので `--specks` は効かない）・`hd2d-r2-sheet.py`（s2 の列: 本家｜今｜箱庭 × PC/PH）・動画の道具（`motion/tools/{fine.py,sway.py}` を写す・`ffmpeg hstack`） | 基準の撮り直し（§3 手順 0）・幕2 の 5 場面（従士と射手 `enc_squire_archer`／3 体＝走竜の巣か道化と妖術師と太鼓／大亀 `enemy_turtle`／人形 9／スマホ）・ot2 の層の矩形・暖色%・床の彩度・動画の 5 場面（§7） | 幕3 の 5 場面（巻物 4 体 `enc_biting_scrolls_quad`〔錨〕／彫師と用心深い影 `enc_sculptor_shadow`／石殻 `enemy_shell_guard` か斧鬼／門番 `enemy_warden`／人形 9＋彫師／スマホ）・Tomb/ot7 の矩形・「床が頂点・柱の中÷外・壁の p10/p90・床の横 7 分割」 |
| **統合** | Unity を動かすのは統合だけ（作業コピー 1 つ） | §3 | §3 |

## 3. 統合の手順

0. **基準を撮り直す（移行の commit の前・HEAD のビルドで）**: `scripts/unity-win.sh build` → `DET=1 scripts/pshots.sh scripts/hd2d-states/s2-<組>.txt ~/.cache/deck-rogue/hd2d-stage2/shots/s2-base/<組> 2` を 11 本（`s2-mixed`・`s2-dio23` 以外。約 275 行・40〜50 分・推測）。
1. 口（移行）の差分 → `scripts/typecheck-unity/run.sh` → `npm run check:colors` → `compile` → `build` → 同じ 11 本＋`s2-mixed` を `s2-after/` に → `hd2d-pixdiff.py`（差 >2 の画素が 0.1% 超＝0 件。**まず幕1 の組、次に今の舞台の組、最後に `s2-mixed`**）。落ちたら migrate §3 の # を当てる（幕1 で落ちるなら fx・StageAct の順番が疑わしい）。
2. **幕2 の最小の試し撮り 1 枚（半日）**: 設計図は「岩棚つきの壁 1 枚＋提灯 2（lights）・座席の帯・slab の段だけ」・look は台本の光だけ・lobe なし。`STATE="phase=combat;act=2;deck=deck_growth;enemy=enc_squire_archer;stage=diorama;hd2d=slice;dioramaacts=all;hideui=1;dumplayout=1"`。**帯の 3 条件（§1）を目と数字で**。届かなければ提灯の range・mist の α・壁の tint・露出の変種 2〜3 本をここで決める（設計図を全部書く前に）。
3. 段1（幕2）: S・D・C・B・E・F・M を並列（各レーンは §2 の自分の行を読み、やることを 1 ページで復唱してから着手）→ typecheck → compile → build → 試しの撮影（§4）→ 計測（`hd2d-a2-targets.py`・`hd2d-layout-check`・`Diorama.Check`・perf=120）→ 試しのシート（本家 ot2｜今｜箱庭 × PC/PH）＋動画の左右並べ 2 本（A 通常の大きい当たり・B ブースト級）。
4. 決める: 壁の岩棚の張り出し（2／3／4）・提灯の range と暈・露出（0.4／0.45／0.5）・nightGrade.hi・スマホの帯の高さ（壁の根元か PC と同じか）・炉の phone.t・寄りの倍率と秒・技の光の強さ。**S25 は端末があれば幕1 を先に 1 回**。
5. 段1b（幕3）: C・B・D・E を幕3 で並走（幕2 の型を写す）→ 試しの撮影（§4。縞を断つ変種 2 枚・柱 t −1／−1.6・露出 2 本）→ 決める（段の高さ・階段・柱の位置・小物 8）。
6. 本番（両幕）: 値の既定化 → compile → build → 本番の撮影（5 場面 × PC/PH × 通常/hideui/uionly/unitsonly＝約 80 枚＋動画の 5 場面 × 2 幕）→ 計測 → 比較シート（本家｜今の舞台｜箱庭・5 場面＋384px の「ひと目」）＋動画の左右並べ（1 本 15〜20 秒／幕）→ **反証 2 役**（「本家と並べる」役＝厳しく 3.5 に届くか／「壊していないか」役＝幕1・今の舞台の画素一致・演出 R1〜R16・配置・性能）→ 直しの輪は最大 2 回（値だけ・直したら必ず画を見る）。
7. commit はレーンごとに 1 つ（口・S・D・C・B・E・A・M・F の順）→ 文書（`hd2d-stage2-plan` に §6 結果・CLAUDE.md の HD-2D の行〔自分の行だけ〕・art-bible〔§5-6 に「閉じた幕」の帯の読み方・§3-3 の幕2 の例外・§2 の arch/pillar/rail〕）。
8. ユーザーに 5 場面のシート＋動画の左右並べ＋exe で「見劣り 1〜5」を聞く（幕2・幕3 を別々に）。幕1 と同等以上（3.5）なら既定へ（裁定 7・`ApplyActDefault` の 1 行＋既定 `"1,2"`〔幕3 も通れば `"1,2,3"`〕。その時 `s2-apk` の幕2/3 の行は意図的に変わる＝基準を取り直す）。APK（`scripts/unity-win.sh android` → aapt で fileprovider → `C:\Users\yosuke\copy-apk.ps1` で Drive。古い hd2d の APK は消す）と exe（`D:\deck-rogue\DeckRogue-win`。**ユーザーの exe が動いていないことを先に確かめる**）。
9. 敵の描き直し（320）・幕ボス 160 の 5px・苔の産み手の席は、判定の後に幕1 の四周目の裁定と一緒に。

## 4. 試しの撮影（`s2-dio23.txt`。共通 `phase=combat;stage=diorama;hd2d=slice;dioramaacts=all;dumplayout=1`）

| 名前 | 端末 | 追加の STATE | 見る物 |
|---|---|---|---|
| T2-min | PC | `act=2;deck=deck_growth;enemy=enc_squire_archer;hideui=1`（最小の設計図） | 帯の 3 条件（手順 2） |
| T2-squire／T2-trio／T2-turtle／T2-dolls | PC | `act=2;enemy=enc_squire_archer`／3 体の編成／`enemy=enemy_turtle;boss=1`／人形 9（ひなた・幕1 の S-dolls と同じ）各 hideui と通常 | 層・壁・帯・床・小物・通りの規則・幕ボス 128 |
| T2-squire-ph／T2-turtle-ph | PH | `tier=phone` | スマホの帯の高さ・phone.hide・炉 |
| T2-exp040／T2-exp050／T2-wall3／T2-wall4 | PC | `look=look_act2_exp040` 等・設計図の変種 `act2_layout_wall3` 等 | 露出・張り出し |
| T2-attack／T2-x／T2-enemy | PC | `enemy=enc_squire_archer;hand=green_strike;eexposed=1;play=0;playshots=120;playevery=1`／X 札 180 コマ／`endplay=1;endshots=210;endevery=1` | 寄り・技の光・暗転・敵の大技（動画） |
| T2-map／T2-reward | PC/PH | `phase=map`／`phase=reward` | MenuShade の α |
| T3-quad／T3-sculptor／T3-shell／T3-warden／T3-dolls | PC | `act=3;deck=deck_big_mana;enemy=enc_biting_scrolls_quad`／`enc_sculptor_shadow`／`enemy_shell_guard`／`enemy_warden;boss=1`／人形 9＋彫師 | 幕3 の 5 場面 |
| T3-quad-ph／T3-warden-ph | PH | `tier=phone` | 壁 1 枚＋柱＋立面 |
| T3-tier／T3-stairs／T3-pillar1／T3-exp035 | PC | 変種（段の高さを t で変える／斜めの大階段／柱 t −1／露出 0.35） | 縞を断つ・柱の位置 |
| T3-attack／T3-spell | PC | 動画の A・C | 柱と壁で光が読めるか |
| M2-old／M3-old／M2-dio／M3-dio／M1-ph | PC/PH | `perf=120`（old と箱庭・幕1 の phone tier） | 重さ |

## 5. 危険と戻し方

- 旗 `dioramaacts=`（既定 1）と `DioramaFallback` で、幕1 の STATE は 1 文字も変わらず、今の舞台は `stage=old` で先に落ちる。レーンごとの commit なので revert もレーン単位。
- 帯が届かない（模型は塗った物）→ 手順 2 の最小の試し撮りで先に決める。露出で合わせない（p95 と主人公の体の歯止め）。
- 段を block にすると `HeightAtPath` が −1.4 を返して部品が沈む → 段は slab。天井は作らない（底の面が無い）。
- 技の光を強くすると半立体が二重の陰影 → 「技の光にだけ強く受ける」係数（S と M）。寄りは壁が無いと空振り → 幕2/3 で先に型を作る（幕1 は近い幹の列で四周目に）。
- 暗転中の 1 枚で「暗い画素 ≤0.66」を測ると割れる → 計測の時刻を決める（寄りの前の静止画）。
- 幕3 は 5° で段丘が出ない（構造）→ 縞を断つ部品の変種を試し撮りで見て、届かなければカメラの裁定をユーザーに開く（計画の外）。
- スマホの重さ: S25 未計測。入らなければ `phone.hide`・段 0〜3 で落とす順（寄りだけ残し光と影は落とす）。
- 10/7 まで壁の絵が cliff のまま＝試し撮りの合否は「形と光の置き場」で読み、「粒」は 10/7 の後。
- MenuShade・からくりの匣の色（`PalOf`）は箱庭の幕2/3 で見てから（幕ごとの口・直すと幕1 も変わる物は触らない）。
- 本家の動画・画・それを含むシートはリポジトリと外に出さない。

## 6. 10/7 以降（PixelLab・幕2/3 の分）

`~/.cache/deck-rogue/hd2d-stage2/pixellab/order-draft-act23.md`／`.json` を `docs/pixellab/hd2d-act23-order-draft.md` に写してから送る（幕1 の四周目 約 590 が先）。幕2＝タイル `act2_side_timber`＋石筍 2・苔の塊 2・樽・提灯の頭・屋台の布（任意: 吊り提灯 neutral・top_plank・ceiling_rock）＝16〜20／幕3＝タイル `act3_side_wall_crumbled`・**壁の大ブロック（発注文に「deep black recesses between blocks・blocks one third of a figure tall・no moss, no vines in the tile」）**・段鼻・崩れた縁 2・紋の帯・柱頭・灯の頭・蔦 2＝14〜20／合計 約 32〜40（任意込み 52）。敵 320 は別裁定。順番＝両幕の最小セット（タイル＋共通の半立体 約 20）→ 3D の範囲の残り → 任意と仮枠は見本を撮ってから。

## 7. 結果（段1 以降が記録する）

（空）

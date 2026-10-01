# HD-2D 見本 三周目の計画（幕1・段1 のもう一周）— 分析と裁定を受けた版

2026-10-01（16:45・まとめ役 Fable）。元にした物＝三周目の分析 `docs/design/hd2d-r3-analysis-2026-10-01.md`（6レーンの分析・提案 A/B・反証3役・§7 の順位表 R1〜R15・§8 の進め方）と、ユーザーの8つの回答（同文書の冒頭）。
**この計画の時点でコード・データ・絵・設計図は二周目の最終（`1498a88`）のまま。段1 はこの計画から始める。**

## 0. 裁定（2026-10-01・分析書 Q1〜Q8。変えない）
1. UI の作り直し（R3）＝入れる。**まず試し撮り1枚**（旗 `groundline=0.36;ledger=feet`＋手札を沈める数字）を段1 の試しのビルドに相乗りし、画を見てから本番（7〜9 日）を裁定。本文は「触れて読む」。
2. 戦闘のキャラの輪郭に **1px の中間色を許す**（規約の例外。ドットの中は 4px・輪郭の1画素だけ・幕2/3 には効かせない）。
3. 接地影＝**薄く広い影＋足元の光溜まり**へ（N15 の向きも本家側 0.93〜0.98 に）。
4. 10/7 の描き直し＝**描き方は変える・形と色の設計は変えない**。
5. 1〜2体で出る獣の枠＝**80 のまま段1 を撮ってから判断**。
6. 主人公の画風＝**据え置き**（見込みの上限 4 として記録）。
7. 針葉樹の幹・枝・房＝**コード生成で先に作る**。箱庭の数の門＝三周目の値に書き直す（小札 250・材質 7・部品の下限は設計図で決め直す）。
8. 判定＝**5場面の総合**（オーガ・狼・4体・人形9体・スマホ）。**S25 の15分の計測を試しのビルドの条件に**（端末が無ければ「未計測」と明記して進め、つながった時に測る）。

## 1. 目標と合否
- 比較シートは **本家｜W5｜二周目｜三周目**（主役オーガ・狼・4体・人形9体・スマホ オーガ）＋384px の「ひと目」。**合否は目で決める**（数字は補助）。見込みは 3.5（4 は R3 と 10/7 の絵がそろった時の縁）。
- 数字の補助（レーン F が `scripts/hd2d-r3-targets.py` に作る。分析書 §3-2）: M1 層の数（本家 6〜7）・M2 上の覆い 35〜60%・M3/M4 幹の太さと暗さの分散・M5 樹皮の読める率・M6 地面の縁の硬さ <3・M7 立った株 8〜12/100px・M9 体÷後ろの窓 ≤1.5・M10 縁の幅 p50 1〜2px・層ごとの細かさの比（床つき）・手前の縁 ≥30/万・帯の中の縁 ≥100/万・敵の鋭い縁の割合 ≤0.4。**歯止め**: 帯の頂点 ≥145・p95 ≥125・暗い画素 ≤66%（森を足しても霧は光る）。N13 は本家 97 基準に下げ、N15 は向きを直し、N2 は σ で、N5/N6a は芯基準の窓で。
- 門①〜⑩は従来どおり測るが、**三周目の判定には使わない**（二周目で通しても見劣りした）。`Diorama.Check` は result=OK を必ず（門の表を直した後）。
- 壊していないか: 幕2/3（`stage=old`）32枚の画素比べ・演出 R1〜R16・配置の検査 0件・S25 の 15 分（30fps・GPU p95 25ms）。

## 2. レーン（ファイルは重ならない。二周目の A〜F に S を足す）
全レーン共通: `git status` がきれい（先頭は三周目の分析の commit 以降）／ルールエンジン・幕2/3 を触らない／新しい振る舞いは箱庭の時だけの枝／生の UI 色を足さない／Unity・PixelLab を使わない（統合だけ）／`scripts/typecheck-unity/run.sh --quick` で確かめる／**二周目の写しを先に作る**（B: `look_act1_r2.json`・`look_act1_r2.dof.json`、E: `look_act1_r2char.json`、C: `act1_layout_r2.json`＝今のファイルのコピー。以後、新しいキーを足したら写しに「off／二周目の値」を書く）／commit はしない（統合がレーンごとに1つ）。

| レーン | 持ち場 | 段1 でやること（値は分析書 §7） |
|---|---|---|
| **S シェーダとメッシュ** | `Resources/Shaders/StageModule.shader`・`Include/HD2DPixel.hlsl`・`Game/DioramaMesh.cs`・`ReliefMesh.cs`・`DioramaTextures.cs`・`Diorama.cs` | R7 半立体の法線アトラス（`_UV_MESH` 枝に `_NormalAtlas`。接空間は板の向き＝`StageUnitLit` の R/U/F の式を写す。`<絵>_n.png` が無い絵は平らな法線で埋める）／R8 `_HD2DFog2`（深さ 45〜90 で芯の色へ。キャラと筋には足さない）とα合成のノイズ入り霧の板（材質 +1）／R14 揺れ（頂点色 a で房と草だけ sin 2種・影と深さの3パスにも同じ変位）／`Diorama.Check` の門の表（小札 250・材質 7・部品の下限は設計図の数から）／設計図の新しいキー（relief の `sway`・`normal`、slab の既存 `mottle`）を読む口。**試しのビルドの前に入れる**（シェーダの文法は統合のビルドで初めて分かる＝足した行を報告に書く） |
| **D 絵のコード生成** | `docs/pixellab/hd2d-act1/gen_art.py`（`r3` 副命令）・`Art/stage/act1/relief/*`（新しい名前だけ）・`Art/stage/act1/tiles/*`（新しい名前だけ）・`scripts/sprite-normals.py`（舞台の出力の口） | R1 針葉樹＝**幹＋枝＋房が1枚の絵**（幅 20／32／48 ドットの3段×各2・高さ 280・cos の円筒陰影をやめ影絵＋**左の縁の明るい筋 2〜3 ドット幅・明暗差 ≥40**・枝 4〜8 本・先に房。房は不透明 0.7 以上の影絵＝半透明なし）／R2 垂れる枝 `bough_hang_1〜4`（160×80・付け根太く先細・房 12〜30 ドット・不透明 0.7 以上）／R4 茂みの塊 `bush_clump_{l,m,s}`×2（96×64／64×48／48×32・内部の明暗は低く・縁ぎざぎざ・底は暗い）・座席の土タイル `top_path_seat_r3_*`（1〜2 テクセルのノイズ ±6%）／R6 手前の草の帯（既存 `act1_tallgrass1`・`fern` から穂の間隔 ≥40px の影絵を 4〜6 枚）／R7 舞台の半立体の `_n.png`（`sprite-normals.py normals` を relief に回す）。色は色表 48 色だけ。既存の絵は上書きしない。4倍の確認シートを `scratchpad` に |
| **C 設計図** | `Resources/Stage/act1_layout.json`・`docs/design/hd2d-slice/r2-layout-gen/{gen_layout.py,place.py}`（r3 の節を足す） | **各レーンの「置く物の表」を受けて焼く**。R1: 帯の木 9 本（s 7〜14・tint 1.0・霧で暗さ）・奥の木 8〜10（s 14〜25）・最奥の列 3・敵の側（x1040〜1760）に細い幹 3〜4（K11 は細い幹で守る）・`needle_bough` 11 枚と `crown-*` の垂れ葉を廃止・近い幹 2 本（幅 34〜44 ドット・画面の上を突き抜ける）。R2: 垂れる枝 6＋2 を近い木の上端から（覆い 35〜45%・霧 0.3 以上・x1200〜1550 の真上は 160 のボスの頭と意図の札のために空ける）・`moon-shaft-2/3` 廃止。R4: 段1 の垂直面と面取りを茂み 2〜3 段（s 4.5〜5.5／6〜7.5／8.5〜10）で隠す・羊歯の一列を廃止・chamfer 0.6→0.1〜0.3・段1 の前の縁に株 40・座席の帯の株 40〜60（中央 ±200px は土を見せる・**全座席の足元の通り〔敵 ±140px・主人公 ±60px〕を空ける**）。R6: 額縁の草 3→10（深さ 10〜14・scale 0.5〜0.7・tint 0.5・vy ≤0.2・名前の帯と手札の矩形を避ける・スマホは左右だけ）。`place.py` の表＝窓・意図の札・座席の通り・N24・部品の数・M2 の覆い |
| **B 光と空気** | `Resources/Stage/look_act1*.json`（＋写し `look_act1_r2*`・変種 `look_act1_r3*`）・`Game/StageLook.cs`・`StageShaft.shader`・`Stage.cs` の `Moondust()`／`SetFxForAct()` だけ・`Game/Render/TiltShiftLook.cs`／`TiltShiftPass.cs`（nearScale の頭打ち 2→3 の2か所だけ） | R1 の霧の変種 `look_act1_r3fog40`／`r3fog45`（fog.end 28→40／45。`lensFar` 3.0 はそのまま。**幹の筋が入った後に `lensFar` 1.6〜2.0 の変種も1本**）／R2 月の塵 center y 1.5→5.5・暖かい白 [2.6,2.4,2.0]・24 個・`moon-shaft-0` gain 1.1→1.3・`stageVignette.side` 0.6→0.5／R4 `lamp.pool.rs` 9→5・level 1.3・`lamp.color` の目標は layers の実測で決め直す／R6 `nearScale` 実効 2→1.0・`lensNear` 2.2→1.2〜1.4・`pathBand` 奥 4.5→3.0／R7 `materials.relief.receive` 0.35→0.6／R8 `_HD2DFog2` の値・高さの霧 0.3→0.2・霧の面 4 枚の廃止（S のシェーダが入ってから）／R9 2段の色寄せ（暗部 色相 210°・C* 13／明部は彩度を残す。`envGrade` を明るさで2段に＝S に `_HD2DEnvGrade2` の口を頼む）／スマホの `look_act1.phone.json` は霧の値を PC と同じに・部品の `phone.hide` の候補を書く |
| **E キャラ** | `Resources/Shaders/StageUnitLit.shader`・`Include/HD2DCharLight.hlsl`・`Game/StageUnits.cs`・`Resources/Stage/look_act1.char.json`（＋写し `look_act1_r2char.json`） | R5 (a) 照明 8段→3段: `heroLift` 1.7→1.3・`shadeLift` [2.4,0.2]→[1.3,0.2]・`saturation` 1.2→1.05・`ambientScale` 1.1→1.0（変種 `look_act1_r3char_soft`＝もう一段下げた物も）／(b) `_BodyShade`（足元ほど暗い勾配）は **3〜4体の明るい敵（N14 107〜125）と人形だけ**（オーガには掛けない）／(c) **縁 1px の中間色**（inline sampler の双線形を輪郭の1画素だけ。幕2/3 の `StageUnit` は触らない・`LitUnits` の時だけ）／(d) 接地影 `contactCore` 0.55→0.25・`contactWidth` 0.85→1.15・`contactAlpha` 0.75→0.45・`heroGem.range` 2.75→3.5（スマホは lamp.pool 側）／R11 `ambientScale` を座席の番号で 1.0／0.92／0.85／0.8・人形の後列 0.85・待機の位相の幅を広げる。(e) 足元を株で隠す＝見送り |
| **A カメラ・UI の配置** | `Game/StageCamera.cs`・`HD2DFlags.cs`・`StageSeats.cs`・`BattleScreen.cs`・`BattleView.cs`・`scripts/hd2d-seatfit.py` | R3 の**試し撮り1枚だけ**: 旗 `uitrial=1`（箱庭の時だけ）で `GroundLineRatio` 0.407→0.36・`ledger=feet`（名前＋HP の 60px）・手札を沈める（`HandSink` 19→183・触れた札だけ +70）・暗幕 `desk-shade` を手札の矩形だけ（R6）。既定は変えない。seatfit の表（0.36・PC/PH）。足元の x の表（1〜4体・PC/PH）を C へ |
| **F 物差しと撮影** | `scripts/hd2d-r3-targets.py`（新）・`hd2d-r2-sheet.py`（r3 の列）・`hd2d-layout-check.py`・`Autopilot.cs`（`hideui` で `desk-shade` も消す1行だけ）・`scripts/hd2d-states/r3-*.txt`・`docs/design/hd2d-slice/perf-runbook.md` | R12 の物差し（§1）と、**二周目の最終の撮影を新しい道具で撮り直す**（暗幕なしの hideui）。一覧: `r3-trial.txt`（§4）・`r3-slice.txt`（PC/PH × 5場面 × 通常/hideui/uionly/unitsonly＝40）・`r3-look.txt`（変種）・`r3-regress*.txt`（R1〜R16）・`r3-old*.txt`（`stage=old`）・`r3-apk.txt`・`r3-screens.txt`・`r3-clips.txt`。シートは 本家｜W5｜二周目｜三周目＋384px の「ひと目」。R13 S25 の計測の手順（`build-perf android`→15分）を段1 の条件に |

## 3. 統合の手順（統合担当だけが Unity を動かす）
1. 段1 の報告がそろう → `scripts/typecheck-unity/run.sh`（全部）→ `npm run check:colors` → `scripts/unity-win.sh compile` → `build`（シェーダのエラーは統合が直して報告に書く）。
2. **S25**: `adb devices` に端末があれば `scripts/unity-win.sh build-perf android` → 15 分（perf-runbook）。無ければ「未計測」と明記。
3. **試しの撮影** `DET=1 scripts/pshots.sh scripts/hd2d-states/r3-trial.txt <出力> 2`（§4・約 16 枚）→ `hd2d-r3-targets.py`・`hd2d-layout-check.py`・`Diorama.Check`（dumplayout の `extra.diorama.ok`）→ 試しのシート（本家｜二周目｜試し）。
4. **決めること**: 霧の end（40／45）・`lensFar`（3.0／1.6〜2.0）・上の覆いの枚数と tint（暗い画素 ≤66%・帯の頂点 ≥145 を守る）・株の本数・R3 の試し撮りを見てユーザーに「本番に入れるか」を聞く（Q1 の2段目）。
5. 段2（値の既定化・B の霧の面の廃止・E の変種の採否・残り）→ `compile`→`build`→ 本番の撮影（約 200 枚・並列 2）→ 計測 → シート → **反証 2 役**（「本家と並べる」「壊していないか」）→ 直しの輪は最大 2 回（値だけ・外れた場面だけ撮り直す）。
6. commit はレーンごとに1つ（S・D・C・B・E・A・F の順）→ 文書（計画 `hd2d-slice-plan` §12・CLAUDE.md の HD-2D の行・art-bible〔縁 1px の例外・接地影・霧・木の置き方・門の表〕）。
7. ユーザーに 5 場面のシート＋exe で見劣り 1〜5 を聞く。
8. APK（`scripts/unity-win.sh android` → aapt で fileprovider → `copy-apk.ps1` で Drive。古い hd2d の APK は消す）と exe（`D:\deck-rogue\DeckRogue-win`。ユーザーの exe が動いていないことを先に確かめる）。

## 4. 試しの撮影（`r3-trial.txt`。共通 `phase=combat;stage=diorama;hd2d=slice;dumplayout=1`）
| 名前 | 端末 | 追加の STATE | 見る物 |
|---|---|---|---|
| T3-ogre／T3-wolf／T3-quad／T3-dolls | PC | `enemy=enemy_brute;boss=1`／`enemy=enemy_wolf`／`enemy=enc_biting_scrolls_quad`／人形9体（slice の S-dolls と同じ）各 hideui と通常 | 層・幹・土手・手前・キャラ（5場面のうち4） |
| T3-ogre-ph／T3-quad-ph | PH | `tier=phone` | スマホの帯・覆い・部品 |
| T3-fog40／T3-fog45 | PC | `enemy=enemy_wolf;hideui=1;look=look_act1_r3fog40`／`r3fog45` | 霧の end（N22・N8・N3） |
| T3-lensfar | PC | `enemy=enemy_wolf;hideui=1;look=look_act1_r3lens` | 幹の筋が読めるか（M5） |
| T3-char-soft | PC | `enemy=enemy_brute;boss=1;look=look_act1_r3char_soft` | 主人公の照明の段 |
| T3-ui | PC | `enemy=enemy_brute;boss=1;uitrial=1` | **R3 の試し撮り（Q1 の2段目に見せる）** |
| T3-r2 | PC | `enemy=enemy_brute;boss=1;look=look_act1_r2+look_act1_r2char`（設計図 `act1_layout_r2`） | 二周目の写しが同じ画になること（戻し方の確認） |

## 5. 危険と戻し方
- 二周目の写し（`look_act1_r2*`・`act1_layout_r2`）で撮影の中だけ戻せる。レーンごとの commit なので revert もレーン単位。
- 森を足して暗くなる（模型 A 暗い画素 77%）→ 覆い 35〜45%・霧の歯止め（頂点 ≥145）。帯が暗ければ `fog.lobe.strength` を上げる前に覆いの枚数を減らす。
- 半立体は α 0.4 で切る＝房・茂み・草は不透明 0.7 以上の影絵（D）。
- 座席は戦闘ごと＝株と茂みは全座席の通りを空ける（C）。
- `Diorama.Check` の門の表を直すまで result は false のまま＝S が最初に直す。
- スマホの重さ: 部品 +50・霧の板 +2・法線 +1 サンプル → 入らなければ `phone.hide`・段1〜3 で落とす順（二周目の口）。
- 幕2/3 への漏れ: `r3-old*`・`r3-apk` の幕2/3 を二周目の最終と画素で比べる。

## 6. 10/7 以降（PixelLab）
`docs/pixellab/hd2d-r3-order-draft.md`（A 幕ボス 160×8・B 幕1 の敵 80×17・C 舞台 25 枚・D 第2姿勢 6・E 獣 96 は予約・合計 510〜590 生成）。Q4 の文（上辺だけ光・体は影・縁線は暗い紺・毛の揺らぎ）を足す。段1 の針葉樹のコード生成は「仕上げの差し替え」の形を先に決める物。

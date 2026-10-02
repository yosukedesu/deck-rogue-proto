# HD-2D 段2 — レーン間の約束（2026-10-03・統合担当 Opus 5.5）

計画 `docs/design/hd2d-stage2-plan-2026-10-02.md` §2 のレーンを**並列に**書くための約束。レーンどうしは互いの完成を待たない。
ここに書いた名前・キー・既定値・置き場は、統合の前に勝手に変えない（変えたら報告に書く。統合が直す）。

## C0 共通の規則（全レーン）

- 幕1 の見本（`stage=diorama`・幕1）と今の舞台（`stage=old`・全幕）は**1画素も変えない**。新しい振る舞いは次のどれかの門の内側に入れる:
  1. `HD2DFlags.DioramaHere`（有効な箱庭。口＝移行で入った）かつ「新しいキーが設計図・光の設計図に書いてある時だけ」
  2. 新しい kind・新しいキー（書いていなければ今のコードの道を1文字も変えずに通る）
  3. シェーダの新しいプロパティの既定値が「今と同じ式」になる（例 `_HitReceive` 既定 1）
- **幕1 の光の設計図 `look_act1*.json`・設計図 `act1_layout*.json`・絵 `Art/stage/act1/` には新しいキーを書かない**（四周目の仕事）。
- 新しい生の UI 色を足さない（`npm run check:colors`）。Unity のフォントに「⚙」は無い。
- ルールエンジン（`src/engine`・`unity/Packages/com.deckrogue.engine`）は触らない。`StageActs.cs`・`StageWater.shader`・`Stage.Reflection`・`Art/tiles/act2|3_*`・`Art/props/act2|3_*`・`docs/pixellab/hd2d-act1/gen_art.py`・`Art/stage/act1/keyflip.json` は触らない（読むのはよい）。
- Unity（`scripts/unity-win.sh`）と PixelLab は使わない（統合だけが Unity を動かす。PixelLab は 10/7 まで呼ばない）。git の commit・stash・checkout・reset・restore はしない。
- C# の確かめは `scripts/typecheck-unity/run.sh --quick --grep <自分のファイル名>`（他のレーンの書きかけのエラーは無視してよい。自分のファイルのエラーは 0 にする）。
- 本家の画（`~/.cache/deck-rogue/hd2d-ref/`・`hd2d-stage2/` の中のシートと模型）はリポジトリに入れない。
- 文書・コメント・ログは日本語（コードの名前は英語）。既存のコメントの密度と書き方に合わせる。

## C1 設計図の新しい部品（実装 = レーン S `Diorama.cs`・`DioramaMesh.cs`／使う = レーン C）

座標は既存の部品と同じ（道の座標 t・s、y は `abs:true` で絶対・無ければ地面 `HeightAtPath(t,s)` からの高さ）。`yaw` は道の向き `PathYaw` に足す（block と同じ「道に沿う部品」）。`phone`・`tint`・`shadow`・`surface`・`seed`・`name`・`onlyWith` は既存どおり全部の新 kind で効くこと。

1. **`block` の `"ao": false`**（任意）: 側面の頂点色 AO を付けない（帯の壁が 45% 暗くなるのを防ぐ）。`"aoAmount": 0〜1` で強さ（既定 = 今の AO）。書かなければ今と同じメッシュ。
2. **`"kind": "rail"`**: レール 2 本＋不等間隔の枕木。キー: `length`（道に沿う長さ・既定 8）・`gauge`（レールの中心間・1.2）・`railW` 0.08・`railH` 0.1・`sleeperLen`（横・既定 gauge+0.6）・`sleeperW`（道の向きの幅・0.22）・`sleeperH` 0.07・`sleepers`（枕木の間隔 [min,max]・既定 [0.8,1.3]・seed で決定的・等間隔にしない）・`railSurface`（レールの材質・既定 `"iron"` が surfaces にあればそれ・無ければ `"rock"`）。枕木は `surface`（既定 `"wood"`）。影は既定あり。草案の `"gauge"`/`"length"`/`"sleepers"` の書き方はそのまま読める。
3. **`"kind": "arch"`**（大門・半アーチ・池の橋）: キー: `w`（外の幅・4）・`h`（全高・4.5）・`d`（奥行き・1.0）・`pier`（脚の幅・0.8）・`rise`（弧の高さ・既定 (w−2·pier)/2）・`segs`（弧の分割・10）・`half`（true = 左の脚と 1/4 の弧だけ）・`bridge`（true = 上を平らな天面にして橋に）・`keystone`（true = 要石を少し出す）。既定の材質 `"rock"`。法線と UV は array シェーダの側面／天面の振り分けが効くように（天面 = 上向きの面）。
4. **`"kind": "pillar"`**: キー: `w` 0.9・`d` 0.9・`h` 5・`capH` 0.35・`capOver` 0.15・`baseH` 0.3・`baseOver` 0.12・`chamfer` 0.06・`broken`（0 = 完全・>0 = 上端をその高さだけギザギザに欠く）・`fluted`（true = 面に縦の溝を 1 ドット幅以上で刻む・任意）。既定の材質 `"rock"`。
5. **`"kind": "halo"`**（灯の暈）: 丸くやわらかい光の円盤（四角に見えないこと）。キー: `r`（半径・unit・0.8）・`color` [r,g,b]（0〜1 か 0〜255）・`gain`（1.0）・`core`（芯の明るさの割合・0.35）・`facing`（`"camera"` = 組んだ時のレイアウトのカメラへ向ける〔既定〕・`"path"` = 道の向きに立てる）・`flicker`（0〜1・既定 0。det の撮影では揺らさない）。材質は既定 `"glow"`（設計図の surfaces の glow）。色は頂点色で持つ（暈ごとに材質を増やさない）。座席の帯の点検（侵入）に数えない。
6. **技の光にだけ強く受ける口**: シェーダ `StageModule.shader`（と半立体・札が使う同じシェーダ）に `_HitReceive`（既定 **1** = 今と同じ）。印の付いた光（`StageHitReceive.Mark(light, true)`・`Game/StageHitReceive.cs`）の寄与だけを `_HitReceive` 倍で受ける。印の仕組み（URP の光の rendering layer の 1 bit・シェーダのグローバル配列など）は S が決めて `StageHitReceive.cs` の頭に書く。**印の無い光・`_HitReceive` が 1 の材質は今と1画素も変わらない**こと。光の設計図の `materials.<名前>.hitReceive` → `_HitReceive`（読むのはレーン B）。
7. **`water` は今回作らない**（試し撮りの後）。天井（底の面を持つ形）も作らない。
8. 新 kind は `DefaultSurface`・点検（`LastStats.ByKind`・部品数）・`dumplayout` の記録・エディタの書き戻し（`PartObjectName`）に今の kind と同じように乗ること。設計図の `"gates"` の `materialsMax`（既定なし）を点検に足す（材質の種類がこれを超えたら Ok=false＋理由）。

## C2 光の設計図の新しいキー（実装 = レーン B `StageLook.cs`・`Stage.cs` の `SetFxForAct`／`Moondust` の箱庭の枝／使う = B・M・A）

1. **`"lights"`**（配列・4 個まで・影なし点光源）: 1 つ = `{"name", "at"?, "t", "s", "y", "dy"?, "color": [r,g,b], "illum", "illumDist" (既定 2), "range", "flicker"? (0〜1・det では揺らさない), "phone"?: {"on": false} | {"illumMul": N} | {"t": N}}`。
   - `at`: 設計図（look の `"layout"` で名指しの物があればそれ・無ければ `Diorama.LoadLayout(act)`）の同じ `name` の部品の t・s を使う（tier=phone なら部品の `phone.t`/`phone.s` を優先）。`y` は絶対の高さ（無ければ部品の y＋`dy`）。設計図の部品を動かせば光も付いてくる。
   - `Light.intensity = illum × illumDist²`（backlight と同じ）。影なし・リグ (`HD2D-LookRig`) の子・`RigShadowCount` に数えない（`EnforceShadowCap` と技の光の影の貸し借りに触れない）。rendering layer は backlight と同じ（地形・大物・半立体・キャラを照らす）。
   - `"lightsMul": {"illum": 1, "range": 1}`（任意）= 全部の lights に掛ける倍率（試し撮りの変種用）。
   - dumplayout の記録（`StageLook` の記録の口）に lights の位置・強さ・range を足す。
2. **`materials.<名前>.hitReceive`** → 材質の `_HitReceive`（C1-6）。書かなければ材質に触らない。
3. **`"menuShade": {"top": a, "bottom": b}`** → `StageLook.MenuShadeTop`・`MenuShadeBottom`（今は空の口が null を返す）。無ければ null（`BattleScreen` は今の定数 0.68／0.4）。読むのはレーン A（`BattleScreen.MenuShade` の 1 行と、色の焼き込みのキャッシュの名前に値を入れる）。
4. **`"fx"` の物の形**: 今の `fx.<名前>: true|false`（口で入った）に加えて `fx.<名前>: {"on": bool, "pos": [t, s, y]}`（y は絶対）。`embers`（火の粉）など位置のある粒は箱庭では `pos` を書いた時だけ点く。`Stage.SetFxForAct` の箱庭の枝だけを触る（`!dio` の枝・幕1 の値は1文字も変えない）。
5. **`StageLook.SetDim(float k)`**（今は空の口）: 月・舞台の灯（スポット）・逆光・lights の `intensity` を「組んだ時の値 × k」にする（k=1 で正確に元へ。0≦k≦1）。キャラの固定のキー（`ApplyCharGlobals` のグローバル）と技の光は触らない。光の設計図の `"motion"` が無い時も呼ばれうる（呼ばれたら素直に効く。呼ぶ側の M が門を持つ）。
6. **`"motion"`**（オブジェクト）: レーン M が読む（B は解釈しない。`StageLook.Current.Raw["motion"]`）。**`motion.on` が true の光の設計図の時だけ M の新しい演出が動く**（幕1 の look には書かない = 幕1 の見本は1画素も変わらない）。B は `look_act2.json`／`look_act3.json` に `"motion": {"on": true}` を書く（値は M の既定・統合で詰める）。
7. 幕2 の `look_act2.json` は計画 §2 B の行と分析書 §4-2 の台本の値。`look_act2.char.json` = `look_act1.char.json` の写し（このはの0周目の値は変えない）・`look_act2.dof.json` = 写し・`look_act2.phone.json` = `look_act1.phone.json` の 7 つの鍵を写してから差分。**試し撮りの最小の光** `look_act2_min.json` = `{"layout": "act2_layout_min", …台本の光だけ・lobe なし…}` も書く（下の C3 の最小の設計図と対）。変種 `look_act2_exp040.json`・`look_act2_exp050.json`（露出）・`look_act2_lamp*.json`（lights の range／強さ）・`look_act2_wall3.json`（`"layout": "act2_layout_wall3"`）・`look_act2_wall4.json` を書く。

## C3 幕2 の設計図（レーン C `Resources/Stage/act2_layout*.json`・`docs/design/hd2d-stage2/layout-gen/`）

- 生成器 `docs/design/hd2d-stage2/layout-gen/gen_act2.py`（`docs/design/hd2d-slice/r2-layout-gen/place.py`・`layoutio.py` を import。`place.py` 自体は直さない）が書く。出力: `act2_layout.json`（本番）・`act2_layout_min.json`（**最小の試し**: 3D の岩棚つきの壁 1 式＋提灯 2＋座席の帯の slab と段だけ・lobe も小物も無し）・`act2_layout_wall3.json`・`act2_layout_wall4.json`（岩棚の張り出し 3／4 unit の変種。本番は 2）。
- **名前の約束**（レーン B の lights の `at` が読む。動かしたら位置ごと動く＝B は値を書き直さなくてよい）: 提灯の部品 `lantern-a`・`lantern-b`（半立体の絵）、暈 `lantern-a-halo`・`lantern-b-halo`、炉 `hearth`（半立体）・`hearth-halo`、坑口の奥の灯 `mouth-lamp`（暈）。光の高さ y は B が書く（提灯は y 3.3 前後）。
- 材質（`surfaces`）の名前は 8 つまで: `terrain`（座席の帯と段の天面）・`rock`（壁・岩棚・崩落岩）・`wood`（支保工・梁・枕木・持ち送り）・`plank`（板張り・歩廊）・`iron`（レール）・`relief`・`card`・`glow`。光の設計図の `materials` と同じ名前（レーン B）。
- 絵のパスはレーン D の置き場（C4）。D の絵がまだ無い名前も約束どおりに書いてよい（無い絵は Diorama が「見つからない物」に数えて色で代わる＝統合で揃える）。
- 規則（計画 §2 C の行・分析書 §4-1）: 座席の帯（t −8.5〜13・s −2.6〜2.8）は高さ 0・部品なし／**全座席の足元の通り（敵 ±140px・主人公 ±60px・人形 x 552〜911）の真後ろに背の高い物を置かない**（x は `place.py` の Cam で出す。t の順で読まない）／意図の札の箱と幕ボス 160 の頭（x 1200〜1550・行 0〜140）を空ける／水路・桟橋は組まない／段は slab（block にすると `HeightAtPath` が −1.4 を返す）／天井は作らない。`place2.py`（新）がこれを全部検査して数を出す。
- `"gates"`: `{"partsMin": 120〜160, "partsMax": 260, "litterMax": 120, "materialsMax": 8}`（計画は partsMin 160。最小の設計図は gates を緩める）。

## C4 幕2 の絵の置き場（レーン D `docs/pixellab/hd2d-act2/`・`Art/stage/act2/`）

- 色表 `docs/pixellab/hd2d-act2/palette-act2.json`（`scripts/stage-palette.py build` の形）。
- タイル `Art/stage/act2/tiles/<名前>_{a,b,c,d}.png`（64×64・`tile-calm` 済み）。名前（＝設計図の `tiles` の材質名に使う）:
  `side_cliff`（壁・岩棚の側面。`Art/tiles/act2_cliff_a〜d` を calm 1/3〜1/5）・`top_cliff`（岩棚の天面）・`top_earth_seat`（座席の帯の床・コード生成・砂の粒 細かさ 40〜90・等間隔にしない・灯の真下が無彩色に読める明るさ）・`top_earth`（段の天面）・`side_earth`（段と slab の前の面）・`top_stone`（一段目の敷石・`act2_stone_a〜d` calm 0.25）・`side_timber`／`top_timber`（支保工・梁・枕木）・`side_plank`／`top_plank`（板張り・歩廊・`act2_plank`）・`side_iron`／`top_iron`（レール・コード生成の暗い鉄）。
- 半立体 `Art/stage/act2/relief/<名前>.png`（＋法線 `<名前>_n.png`・発光 `<名前>_e.png`）。名前 = 元の `Art/props/act2_<名前>.png` から `act2_` を取った物（例 `hearth`・`lantern_post`・`lantern_hang`・`crystal_big`・`barrel`・`crate_stack`・`minecart`・`winch`・`stall_a`・`stalactite_a`・`post_brace`・`ore_pile`…）。足元と皿を切った物（`relief-clean.py`）。捨てた物・10/7 まで置かない物は `index.json` に理由つきで。
- 小札 `Art/stage/act2/litter/<名前>_<n>.png`（コード生成・背丈 12 ドット以下）: `gravel`・`pebble`・`gear`・`chips`（木くず）。
- `Art/stage/act2/index.json`（幕1 の `Art/stage/act1/index.json` と同じ形）・`Art/stage/act2/keyflip.json`（左右反転した絵だけ。無ければ空の配列）。

## C5 動き（レーン M）の門と口

- **門**: `StageFx.Live`（= 有効な箱庭かつ光が当たっている）かつ `StageLook.Current.Raw["motion"]["on"] == true`。どちらかが偽なら今の演出と1画素も変わらない（幕1 の look には motion が無い＝幕1 の見本・演出 R1〜R16 は不変）。
- 値は全部 `motion` の下のキー（既定はコード）: 寄り（大きい当たり 0.45 秒・X 札／全体の最後の 1 発／とどめ 0.9 秒・幕ボスのとどめ 1.8 秒・倍率 1.2〜1.35・1 フレームで切り替え・戻しも切り替え）・技の光（強さ 10〜15・range 10・dur 1.0・hold 0.35・寄りの間だけ・色は技の色）・暗転（`StageLook.SetDim(0.3〜0.5)`）・敵の大技（予告つき大技だけ・赤の合図 0.17 → 暖のフラッシュ 0.15 → 白 0.3 → 技の色の帯 1.2・舞台の板だけ）・火花 50〜100 粒・ピント（寄りの間だけ対象の深度へ）。計画 §2 M の行・分析書 §8。
- 使ってよい他のレーンの口: `StageLook.SetDim`（B）・`StageHitReceive.Mark`（S）。どちらも今は空の口（呼んでも何も起きない）なので M は単独で型検査が通る。
- 紙の UI（帳面・手札・自分の札）は寄りの間だけ退避（夜色の札は残す）。カメラは待機中に動かさない（本家は 0.00px）。OT2 の回転は入れない。

## C6 撮影と物差し（レーン F）

- 統合担当が既に書いた一覧（**触らない**）: `scripts/hd2d-states/s2-{slice,k0v,ui,old,old23,apk,screens,regress,regress-ph,regress-old,regress-old-ph,mixed}.txt`。
- F が書く: `s2-dio23.txt`（下書きを広げる＝計画 §4 の T2-*・M2-*・T3-*・M3-*）・`scripts/hd2d-a2-targets.py`・`hd2d-a3-targets.py`・`scripts/hd2d-pixdiff.py`（下書きの写し）・`hd2d-r2-sheet.py` の s2 の列（本家｜今｜箱庭 × PC/PH・本家の画素を含むシートは `~/.cache` にだけ書く）・動画の道具 `scripts/hd2d-motion/{fine.py,sway.py,sbs.sh}`（`~/.cache/deck-rogue/hd2d-stage2/motion/tools` の写し＋ffmpeg の左右並べ）。
- 場面名は `<PC|PH>-T2-<場面>[-変種]`（試し）・`<PC|PH>-S2-<場面>`（本番）・幕3 は `T3`/`S3`。

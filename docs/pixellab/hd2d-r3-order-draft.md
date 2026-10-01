# HD-2D 三周目 — PixelLab 発注書の下書き（2026-10-01）

**まだ送らない。10/7 まで月の枠が無い。**
> **ユーザーの回答（2026-10-01）**: Q4＝**描き方は変える・形と色の設計は変えない**（A・B の文に「上辺だけ光・体は影・縁線は暗い紺・毛や皮の 1〜2px の揺らぎ」を**足す**）。Q5＝獣の枠は **80 のまま段1 を撮ってから判断**（E 枠は予約だけ・B から外さない）。Q7＝針葉樹はコード生成で先に作る（C の針葉樹は「仕上げの差し替え」＝段1 の形で発注の文を直す）。 10/7 に枠が戻ったら `node scripts/pixellab-v2.mjs balance` で確かめてから `order --dry` → `--budget N --reserve 50`。
元はレーン char の下書き（`~/.cache/deck-rogue/hd2d-r3/char/order-draft.json`・同 `.md`。機械可読の JSON はそちら）。この文書は、提案 A・B と反証 3 役で変わった分を反映した**人が読む版**。実際の JSON は裁定（分析書 §10 の Q4・Q5）の後に `docs/pixellab/hd2d-act1/template.json` の形で起こす。

## 0. 守ること（CLAUDE.md の勘所・`docs/art-bible.md` §8）
- 禁止語（forest・night・moonlight・scene・moss green・dirt）は描写に入れない（否定の文には書いてよい）。苔は `mossy`／`lichen`、土は `packed earth`／`soil`。夜は描かせず `neutral mid-tone colors`／`evenly lit neutral daylight`＝夜はエンジンの光で作る。
- 透明背景は**面積 30,000px 以下**（幹 100×280＝28,000・針葉樹の列 240×120＝28,800・近い大木 120×240＝28,800・ボス 160×160＝25,600）。タイル 2 種だけ背景あり（160 を作って中央 128 を 64×4 に切る）。
- 等間隔の粒・星形の葉・泡は否定の文に（`evenly spaced`・`symmetrical`・`pile of pebbles`・`evenly spaced dots`）。出た物は `gen_art.py check`（bright_specks・集合）に通す。
- 舞台の小物は `view: side`・**正面の立面図**（天面も地面も台座も無し）。敵は元の絵の向き（プレイヤーと向かい合う斜め左手前）を edit で保つ（`facing the same direction`）。
- **半立体は α 0.4 で切る（`surfaces.relief.cutoff 0.4`）ので、房・茂み・草の帯は「不透明の影絵（縁以外に半透明の画素なし）」で描く。薄さは霧と tint で付ける。** 全部の舞台の絵の文に `solid opaque silhouette, no semi-transparent pixels except the outline` を足す（反証 feasible 3-2）。
- 色と意匠は元の絵から写す＝「手元で EPX×2 に拡大 → 目標の背丈へ縮め → 枠に置く → edit-images-v2 `--size N` → 消えた差し色を元から戻す」（memory `pixellab-enlarge-existing-sprite`）。
- 出来た舞台の絵は `docs/pixellab/hd2d-act1/relief-clean.py`（根元の土を消す）→ `scripts/stage-palette.py map`（色表 48 色）→ `scripts/art-lint.py`。
- **舞台の絵は雛形どおり「中立の明るさ」で頼む**（root・提案 A の「夜の色表で描く」は、昼色の既存の絵と混ぜると色が割れる＝層ごと一括で差し替える時に別に判断。分析書 §11-3）。

## A. 1 体で出る幕ボスを 160 の枠（裁定 2026-10-01）— 8 体 × 2 シード × 20 ＝ **320**

| 順 | id | 元 | 体（ドット）128→160 | 文で名指しして残す部品 |
|---|---|---|---|---|
| 1 | enemy_brute（脳筋オーガ） | 128・体 100 | 138 | 猪の鼻と牙・淡い灰白の苔の毛・鬣の金の筋・腕の房・棍棒 |
| 2 | enemy_haze_stag（朧の大鹿） | 128・体 104 | 140 | 霜の枝の角・脚を隠す青灰の霧・霧に溶ける縁（細い角は点サンプルで拡大） |
| 3 | enemy_chimera_1（合成獣 一の相） | 128・体 103 | 135 | 淡い苔色の毛・クリームの頭・金の鋲の首輪・爪 |
| 4 | enemy_chimera_2（二の相） | 128・体 109 | 138 | 同上（色が苔に寄ったら Lab の a,b を戻す） |
| 5 | enemy_chimera_3（三の相） | 128・体 111 | 140 | 耳の内の赤・帯と脚の金具・黄の目 |
| 6 | enemy_warden（門番・古機） | 128・体 104 | 140 | 錆びた金属・真鍮の歯車・橙の差し色・硬いハイライト |
| 7 | enemy_ember_furnace（熾を喰う古炉・古機） | 128・体 104 | 140 | 炉の体・真鍮・火床の熾 |
| 8 | enemy_turtle（眠たがりの大亀） | 128・体 100 | 138 | 甲羅の板・淡い苔の肌・眠たい姿勢（豆の目は手で直す） |

- 道: edit-images-v2 `--size 160`・view はそのまま・`n 1`・seeds [1, 2]。**160 は未実績（128 は 1 体 20 生成の実績）＝最初の 1 体（オーガ）を 1 シード送り、受かるか・差し色の戻しが要るかを見てから残りを回す。** 受からなければ: 128 の絵を 160 の枠に置いて手元で拡大し `--size 128` で上下 2 分割→継ぎ目を手で直す（迂回）／pixflux で参考画なしに描き直す（意匠が変わる＝ユーザーに聞く）。
- 文（獣）: `same creature, same design, same pose, same colors and same size, facing the same direction, redraw as clean crisp pixel art at this resolution with sharper details, <残す部品>, soft shading like watercolor washes, thin dark ink outline in dark brown or deep indigo instead of pure black, two small round black bead eyes, no mouth, transparent background, no ground, no shadow on the ground, no text`。古機は `watercolor washes` を `hard metallic highlights` に。
- **Q4 が「描き方は変える」なら足す文**: `the upper edges of the body catch the light while the lower body falls into shadow, fur and hide drawn with one or two pixel irregular strands, no large flat areas of a single color`（反証 score の抜け M2＝上辺だけ光・体は影・毛の揺らぎ。オーガの明るさの分布は既に本家並みなので、変えるのは縁と中の模様だけ）。
- 足元は最下端から 2 ドット・左右中央。PC の箱は `BattleScreen.FillEnemyPanel` が絵のドット数×4 で読むので 160 の絵を置けば 640px（コードは段1 以降）。スマホは今の `<id>_96.png` のまま（同じ粒では体 103 ドットが上限。`char.md` §2）。
- 優先順: オーガ・大鹿（見本の幕）→ 合成獣 一・二・三（膨らむ残機の並び 135→138→140）→ 門番・古炉・大亀。

## B. 1〜2 体で出る幕1 の敵を 80 の枠（裁定 2026-10-01）— 17 体 ＝ 2 回 × 2 シード × 20 ＝ **80**

出所: `src/engine/map.ts` の `ACT_POOLS[0]`・`WEAK_POOLS[0]`・`ELITE_POOLS[0]` を `src/data/encounters.json` で展開。

| 枠 | 敵（id） |
|---|---|
| 1 回目（9 枚） | 探り屋 enemy_probe・うねる獣 enemy_wide_power・針毛の栗鼠 enemy_thorn_squirrel・見習い巨像 enemy_apprentice_colossus・囁きの狂信者 enemy_cultist・酸吐きの蜥蜴 enemy_slug・泥まとうもの enemy_mud_lump・裂け口の獣 enemy_gaping_maw・歯車の箱兵 enemy_cog_construct（古機＝文を hard metallic に） |
| 2 回目（8 枚） | 蔦纏いの歩き木 enemy_vine_walker・巻きつく大蛇 enemy_strangler_serpent・鉄殻の溝貝 enemy_iron_clam・汚泥紡ぎの蜘蛛 enemy_sludge_spider・物真似の子鬼 enemy_mimic_imp・こそ泥ゴブリン enemy_thief・噛みつき果実 enemy_snap_fruit・胞子吹きの茸 enemy_spore_cap |
| 64 のまま | 小泥 enemy_mudling（3〜4 体の群れだけ） |
| 既に 80 | 鬼軍曹・歩哨・金羽の大鴉・大喰らいの蟲（強個体） |
| 既に 96 | 血族の司祭・踊り手（幕ボス・3 体） |

- 道: edit-images-v2 `--size 80`（80px は 1 回に 9 枚まで同梱）・入力は今の 64 の絵を EPX×2 → 体 68〜72 ドットへ縮めて 80 の枠に置く。
- 文: `same creature, same design, same pose, same colors and same size, facing the same direction, redraw as clean crisp pixel art at this resolution, keep the small round black bead eyes and the quiet expression, soft shading like watercolor washes, thin dark ink outline in dark brown or deep indigo instead of pure black, transparent background, no ground, no shadow on the ground, no text`。Q4 が「変える」なら A と同じ描き方の文を足す。
- 注意: 3 体の編成にも出る敵（探り屋・栗鼠・大蛇・泥まとうもの・胞子茸・噛みつき果実）は 3 体の座席（間隔 267px）で 80 の箱 320px が重なる（体の幅 ≈280px なら 13px）＝座席の表の調整が要るかもしれない（段1 以降・`StageSeats.cs`）。
- 出来た絵は `_n`（法線）を `scripts/sprite-normals.py normals` で作り直す。`docs/pixellab-assets.md` の寸法の段に「幕1 の 1〜2 体の敵 80」を足す。

## C. 舞台の絵 — 25 枚 × 2 シード ＝ **50**（pixflux・view side・正面の立面図・中立の明るさ・不透明の影絵）

共通の頭（`_reliefText`）: `strictly front-facing orthographic elevation of a single object, drawn like a 2D side-scrolling platformer background sprite, no perspective, no depth, the top surface is not visible, the ground plane is not visible, no base platform, centered, nothing else in the frame, completely empty transparent background, no soil, no shadow on the ground, no sky, <物>, solid opaque silhouette, no semi-transparent pixels except the outline, lit softly from the upper left, neutral mid-tone colors, HD-2D Octopath Traveler style pixel art, detailed shading, no text`
共通の否定（`_reliefNegative`）: `isometric, dimetric, oblique, 3/4 view, perspective, vanishing point, diagonal base, diamond platform, tilted top surface, visible floor, ground tile, grass field, island, scene, background, landscape, forest, trees behind, sky, moon, frame, border, text, watermark, blurry, bright saturated colors, night, dark blue tint, people, character, face, animal, semi-transparent, translucent, gradient fade`（＋各項目の追加）

| id | 大きさ（ドット） | <物> の要点 | 追加の否定 | 使い所（設計図の kind・位置） |
|---|---|---|---|---|
| conifer_row_far | 240×120 | 間隔と高さが不ぞろいの細い針葉樹の列。尖った樹冠・幹は根元だけ・平らな影絵 | round crown, deciduous, broad leaves, evenly spaced, identical trees | 霧に溶ける列・最奥（深さ 26〜31 unit）。左右反転で 2〜3 枚。R1 の最奥の列 |
| conifer_row_mid | 240×120 | 5〜7 本の針葉樹・垂れる房・幹が房の間に見える・中くらいの細かさ | 同上 | 列の中の段（深さ 18〜24）。R1 の奥の木の層 |
| conifer_near_big | 120×240 | 近くで見た 1 本の大きな針葉樹。太い樹皮の幹・上から下まで層になった長い房・樹冠は上端で切れる | round crown, deciduous, whole small tree, pot | 左右の端（t −6〜−4／10〜12）・幹は画面の上を突き抜ける。R2 の近い木 |
| trunk_branched_w48／w32／w20 | 100×280／80×280／64×280 | 幹の全高・粗い灰茶の樹皮・**幹に直接付く短い枝 2〜4 本と針葉の房 1〜2**・根元が少し広い。**左の縁に明るい縦の筋 1〜2 本**（本家の幹の形） | round crown, whole tree with crown, roots spreading on ground | `sources.trunkLong1..3`／`trunkThin1..3` の候補の先頭。R1 の帯の木・両端の幹 |
| **bough_hang_1〜4（新規・A2）** | 160×80 | 上端から垂れ下がる針葉の枝。付け根は太く先は細い・枝分かれ 3〜5・房は 12〜30 ドット・下に向かって垂れる | round leaves, evenly spaced, symmetrical, whole tree | R2 の上の覆い（近い木の上端の外 y 7.5〜9・s 2〜5）。今の `needle_bough` の置き換え |
| **bush_clump_l／m／s（新規・A3。各 2 種＝6 枚）** | 96×64／64×48／48×32 | 丸い茂みの塊。内部のコントラストが低い・縁はぎざぎざ・暗い底 | bright yellow-green, flowers, evenly spaced dots, pot | R4 の土手の茂み 2〜3 段（s 4.5〜10）。夜の色は色表と霧で |
| rock_moss_mid_a／b／c | 64×48／56×40／48×48 | 中くらいの苔岩（丸い頂／低く広い／立った石）。苔は上だけ・底は暗い | bright yellow-green, pile of pebbles, evenly spaced dots, gravestone | 土手の縁・座席の帯の外。今は大（112×72）と小石しか無い |
| grass_stand_tall／mid／small | 48×40／40×32／32×32 | 立った草の株（背丈 36〜40／24〜28／12〜16）。長さと向きが不ぞろいの葉 | flowers, seed heads, evenly spaced blades, symmetrical, pot | R4 の土手の束・座席の帯の縁・小札（12 に切り詰め）。PixelLab の下限は 32×32 |
| grass_band_foreground | 200×100 | 手前の背の高い草の帯。不ぞろいの穂が重なる・数本だけ高い・下端は水平の直線 | reeds, cattails, flowers, evenly spaced blades | R6 の額縁（s −9〜−12）。穂の間隔 ≥40px（ぼけの幅より大きい形） |
| bank_edge_grass_over_soil_tile | 160→64×4（背景あり） | 低い土手の縦の面の横に繋がるタイル。上 1/3 は草と根が縁に被さる・下 2/3 は小石と根の出た土 | trees, path, gradient, stripes, rows, white specks, evenly spaced | R4 の段1 の前の面（`side_rock` の代わり）。横だけ繋がればよい |
| bank_root_cliff_tile | 160→64×4（背景あり） | 根の出た崖の面。節くれだった根が土と割れた灰色の石の間を走る | 同上 | 高い段の縦の面・左右の端 |

## D. 同じ敵の第 2 姿勢（新規・反証 score の抜け M3）— 6 体 × 2 シード × 20 ＝ **40**（または 64px 16 枚同梱なら 1 回 × 2 シード ＝ 40 のまま）

3〜4 体で出る敵は「同じ絵・同じ向き・等間隔」が最も舞台装置に見える（eyes #6）。明暗差（R11）では消えないので、**待機の姿勢だけ違う絵を 1 枚ずつ**作り、座席で交互に使う（偶数の座席は第 2 姿勢）。

| id | 元 | 変える所 |
|---|---|---|
| enemy_biting_scroll（噛みつく巻物） | 64 | 巻きの開き具合・首の傾き |
| enemy_mudling（小泥） | 64 | 塊の潰れ方・滴 |
| enemy_probe（探り屋） | 64（B で 80 へ。80 で作る） | 首の向き・翼のたたみ |
| enemy_thorn_squirrel（針毛の栗鼠） | 64（B で 80 へ。80 で作る） | 針の立ち方・尾 |
| enemy_chomper（金切り顎） | 64 | 顎の開き |
| enemy_scald_gnat（灼き虻） | 64 | 翅の角度・脚 |

- 文: `same creature, same design, same colors and same size, facing the same direction, a slightly different idle pose: <変える所>, redraw as clean crisp pixel art, two small round black bead eyes, no mouth, transparent background, no ground, no shadow on the ground, no text`。否定に `mirrored, flipped, different creature`。
- 置き場は `Art/enemies/<id>_alt.png`（案。コードは段1 以降＝`StageUnits` が座席の番号で絵を選ぶ 10 行）。向きの規約（斜め左手前）は保つ。

## E. 条件つき（Q5 の裁定「獣だけ 96」の時だけ）— 5 体 × 2 回 × 2 シード × 20 ＝ **0〜80**

1〜2 体で出る獣（牙嵐の狼 enemy_wolf・双牙の狼 enemy_bond_wolf・うねる獣 enemy_wide_power・裂け口の獣 enemy_gaping_maw・巻きつく大蛇 enemy_strangler_serpent）を 96 の枠（体 84 ドット＝主人公の 1.35 倍）。96px は 1 回に 4 枚同梱。B の 80 と二重になる 3 体は B から外す。推奨は「80 のまま段1 を撮ってから」なので、**この枠は予約だけ**。

## F. 仮枠 — **20**
レーン eyes／root／統合の要望（例: 霧の中の小さな灯・倒木・水たまりの縁・幹の根元の茂み・光る側のある針葉樹の列）。要らなければ A の 160 の再試行に回す。

## 合計と月の枠
| 区分 | 生成数 |
|---|---|
| A 幕ボス 160 | 320 |
| B 幕1 の敵 80 | 80 |
| C 舞台 25 枚 | 50 |
| D 第 2 姿勢 | 40 |
| E 獣 96（条件つき） | 0〜80 |
| F 仮枠 | 20 |
| **合計** | **510（条件つき込み 590）。予備 15% で ≈590〜680 ＝ 月の枠（Tier 2）5,000 の 12〜14%** |

順番: A のオーガ 1 体（160 が受かるか）→ C の針葉樹（R1・R2 の絵の差し替え）→ B → D → A の残り → C の残り。実行の記録は `<発注書>.log.jsonl`、出来た物の台帳は `docs/pixellab-assets.md` の「HD-2D 見本 幕1」の節に足す。

# HD-2D 段2 幕2「先代の坑道」の箱庭の絵（レーン D・2026-10-03）

計画 `docs/design/hd2d-stage2-plan-2026-10-02.md` §2 D・約束 `docs/design/hd2d-stage2/contracts.md` C4・分析書 `docs/design/hd2d-stage2-analysis-2026-10-02.md` §9。
**PixelLab は 0 回**（10/7 まで呼ばない）。今ある絵（`Art/props/act2_*`・`Art/tiles/act2_*`）と幕1 の道具だけで作った。
今の舞台（`stage=old`）が読む `Art/props`・`Art/tiles`・幕1 の `Art/stage/act1`・`docs/pixellab/hd2d-act1/` は **1 バイトも変えていない**（読むだけ・import するだけ）。

## 作り直し方（同じ入力なら同じ絵）

```
python3 -B docs/pixellab/hd2d-act2/gen_items.py all [--work <作業場>] [--sheet <確認シート.png>]
python3 -B docs/pixellab/hd2d-act2/gen_items.py lint          # 点検だけ → work/lint.md・lint.json
python3 -B docs/pixellab/hd2d-act2/gen_items.py sheet --out <png>
python3 -B docs/pixellab/hd2d-act2/gen_items.py honke <ot_921570_2.jpg>   # 本家の色の錨を測り直す (結果は gen_items.py の HONKE_* に手で写す)
```

`all` は `unity/Assets/Resources/Art/stage/act2/` を消してから全部を書き直す（名前を変えた絵が残らないように）。
下書き（calm 済みのタイル・足元を切った小物＝色表へ写す前）は作業場 `--work`（既定は一時フォルダの `hd2d-act2-work`）に置き、リポジトリには入れない。
**作業場は毎回消して作り直す**ので、`all` は次の時は止まって何も消さない: 作業場がリポジトリの中かリポジトリを含む（`--work docs/pixellab/hd2d-act2/work`・`--work .`・`--work ~`）／
中に `all` の作っていない物がある（目印 `.hd2d-act2-work` が無く、中身が `calm/`・`relief/` だけでもない）。
`docs/pixellab/hd2d-act2/work/`（作業場とは別）には点検の表だけ（`lint.md`・`lint.json`・`tiles.json`）。

流れ: ① タイルを `scripts/tile-calm.py` の `calm()` で静かな面に → ② 小物の足元と皿を切る（`docs/pixellab/hd2d-act1/relief-clean.py` の
`drop_specks`・`trim`）→ ③ 色表 → ④ 色表へ写す（`scripts/stage-palette.py` の `map_image`）→ ⑤ コード生成（土・鉄・小札。
`docs/pixellab/hd2d-act1/gen_art.py` を import して `PAL`・`PALRGB`・`EARTH`・`STONE`・`GREEN` を幕2 の物へ差し替えて呼ぶ）→
⑥ 法線（`scripts/sprite-normals.py` の `stage_normal_map`＝`normals-stage` と同じ）・発光 → ⑦ 一覧 → ⑧ 点検。

## 置いた物

| 物 | 置き場 | 数 |
|---|---|---|
| 色表 | `docs/pixellab/hd2d-act2/palette-act2.{json,png}` | 64 色 |
| タイル 64×64 | `Art/stage/act2/tiles/<材質>_{a,b,c,d}.png` | 12 材質 × 4 = 48 |
| 半立体 | `Art/stage/act2/relief/<名前>.png`（＋法線 `_n`・発光 `_e`） | 34（法線 34・発光 9） |
| 地面の小札 | `Art/stage/act2/litter/{gravel,pebble,gear,chips}_<n>.png` | 15 |
| 一覧 | `Art/stage/act2/index.json`（幕1 と同じ形＋ `litter`・`held`・`discarded`） | — |
| _KeyFlip の表 | `Art/stage/act2/keyflip.json`（空の配列。左右反転して置いた舞台の絵は無い） | — |

### 色表 `palette-act2.json`（64 色）
- 塊 36 色: calm 済みのタイルと足元を切った小物の色（画素数の重み・Oklab・明るさ 1.5 倍の k-means。`stage-palette.py build` と同じ式）
- 差し色 8 色: 発光の帯に入る画素（炉の火・提灯のガラス・結晶）。画素数の平方根の重み＝少ない画素でも消えない
- 本家 ot2 の錨 11 色: 暗部 (3,21,23)・(11,43,53)・**(21,44,50)**・(34,61,69)・(63,81,90)／灯の真下の床 (25,48,48)・(55,72,73)・(92,96,93)・**(105,105,110)**・(115,112,108)・(139,129,122)。本家の画素は数えただけで、絵には入れていない
- 床の灰白の段 9 色: 本家の床の暖かい灰の色相のまま明るさを伸ばした (46,45,44)〜(146,140,132)。既存の絵には無彩色の灰の段がほとんど無く、近い色を選ぶと青か紫に寄った（座席の床が灯の真下で無彩色に読めない）ので足した

### タイル（`index.json` の `materials`。どれも calm 済み＝読み込み側で `calm=1・sat=1`）

| 材質 | 面 | 回し方（推奨） | 元 | 備考 |
|---|---|---|---|---|
| `side_cliff` | 縦 | flipX | `act2_cliff_a`・`b`・`a`(上下反転)・`d` | 明暗 1/3（ざらつき 640 を超えれば 1/5 まで自動）。**`act2_cliff_c` は丸い小石が詰まった「泡状の石」なので使わない** |
| `top_cliff` | 天面 | rot4 | 同じ岩 | 1/4・砂ぼこりで少し明るく（Oklab L +0.05）・彩度 0.6 |
| `top_stone` | 天面 | flipX | `act2_stone_a`・`b`・`c`(上下反転)・`d` | 1/4。stone_c は stone_a と同じ絵なので上下反転。石が横の列に並ぶので回さない |
| `side_plank`・`top_plank` | 縦・天面 | **none** | `act2_plank`（1 枚） | 板の列（幅 16 の板 4 枚）を並べ替え・上下反転して 4 種。天面の板は縦＝奥行き s の向き（歩廊を横切る板）。**継ぎ目の暗い列がいつも 0・16・32・48 列（左端）に来るので回さない**（flipX だと継ぎ目が 63 列へ移り、隣との間で継ぎ目が 2 テクセル幅の所と、消えて幅 32 の板になる所がでたらめに出る） |
| `side_timber`・`top_timber` | 縦・天面 | flipX | `act2_wood`（1 枚） | 板の列（幅 28・19・17）を並べ替え・上下反転して 4 種。継ぎ目は tile-calm（半分ずらしのキルティング）の後に中の列へ散り左右の端に来ないので、flipX でも継ぎ目は 2 重にも消えもしない（幕1 の `side_wood` と同じ。none でもよい）。10/7 の `act2_side_timber` で差し替える予定 |
| `top_earth_seat` | 天面 | rot4 | コード生成 | 座席の帯の床。灰白の踏み固めた砂の床・砂の粒（横 4〜6×縦 3〜4 テクセル・白色の乱数で置く）・小石 1〜3。下の「座席の床」 |
| `top_earth` | 天面 | rot4 | コード生成 | 段の天面。`gen_art.earth_tile_r2` を床の灰白の段の暗い側で |
| `side_earth` | 縦 | flipX | コード生成 | 段と slab の前の面。横に流れる地層の筋・途切れた継ぎ目・埋まった小石 |
| `side_iron`・`top_iron` | 縦・天面 | flipX | コード生成 | レール（幅 2 テクセル）。側面は暗い鉄＋圧延の筋＋錆の点、天面は磨かれた鋼の長い筋 |

### 座席の床 `top_earth_seat`（art-bible §3-3 の幕2 の例外）
- タイルのざらつき（ラプラシアン分散）712〜768（門 ≤800＝art-bible §3-3 の奥の段と同じ値。幕1 の座席の土 `top_path_seat_r3` は 715〜864）。彩度の中央値 0.043（≤0.12）。明るさの平均 約 103〜110
- 画面での細かさの模型 `screen_fineness`（横 1 テクセル＝4px・縦は見下ろし 5° で 1/2 テクセル＝1px に平均・σ0.7 のぼかしの後のラプラシアン分散＝本家の物差しと同じ式）: 48〜54。
  光溜まり（pool level 1.35）で明るさが上がった時 88〜98。分析書の目標は撮影で 40〜90（本家 ot2 の床 92）。模型は幕1 の座席の土（`top_path_seat_r3`）で 15〜19（実際の撮影の幕1 の座席は 11.6＝撮影÷模型 約 0.6〜0.7）。
  この比なら撮影は灯なしで 約 30〜38・光溜まりで 約 55〜69。幕2 の粒は幕1 の 1 テクセルの斑より大きい（横 4〜6）ので、双線形とミップで潰れる分は幕1 より小さく、比は 1 に近い見込み（その時は灯なし 48〜54・光溜まり 88〜98）
- 縦 1 テクセルの粒は見下ろしで画面に残らない（縦に 2 行が 1px に潰れる）ので、粒は縦 3〜4 テクセルにした。粒の量はタイルごとに「ざらつきが門の 0.96 倍以下になる最初の量」を選ぶ（`SEAT_GRAIN_STEPS` を ×2.6 から下へ。大きな斑の出方で同じ量でも 650〜830 にばらつく。今は ×1.8〜×2.4）
- 2026-10-03 の反証の直し: 最初の版は門を 500 に置いていた（レーンが自分で決めた値で、約束にも art-bible にも無い）。そのため模型が灯なしで 36〜41＝目標の下の端か下だった。
  量を増やしても粒が重なって細かさは頭打ちになる（門 1,000・量 ×3.0 でも模型 53〜59）。**撮影で 40 に届かなければ、次に効くのは量でなく粒の明暗の幅**（今は床の段 ±1 段）

### 半立体（`index.json` の `relief`・`held`・`discarded`）
元の 42 枚の仕分け（`~/.cache/deck-rogue/hd2d-stage2/pixellab/props-judgement.md` に沿う）:

| 区分 | 枚 | 名前 |
|---|---|---|
| 足元と皿を切った | 16 | barrel_stack・crate_stack・crystal_big・gear_big・gear_pile・hearth・lumber・minecart・minecart_tipped・sacks・shelter・stall_a・stall_b・toolrack・winch・wreck |
| そのまま（薄い皿・影・草だけ切った物を含む） | 12 | bell_post・crate・crate_open・crystal・lantern_hang・lantern_hang_b・ore_pile・pulley・roots_hang・stalactite_a・trough・tunnel_side |
| 等角で頼んだ絵のうち使う物 | 6 | ladder・lantern・lantern_post・roots・stalactite・tunnel_mouth（吊り物と正面に近い物。台座・影・砂は切った） |
| 10/7 まで置かない | 2 | barrel（単体の樽は等角しか無い）・post_brace（支保工は 3D で組む・縄の巻きつき） |
| 捨てる | 6 | rope_coil（縄の輪は却下の柄）・gear・rail_a・rail_b・rubble_rock・stall |

- 切り方は `gen_items.py` の `RELIEF`（行で切る `below`・皿の横の広がりを切る `band`・地面の色 `ground`＝砂／草／草の暗い縁／茶色の土／岩／影）。最後に欠片を消して外接の四角＋余白 2 に詰める
- 法線は全部（`stage_normal_map`）。坑口と脇坑の中の暗がりは平らな法線（丸みで膨らませると穴が枕のように光る）
- 発光 `_e.png`（9 枚）: 炉の火（火床の範囲だけ）・提灯 4 種のガラス・結晶 2・鉱の結晶（鉱の山・トロッコ）。光は絵に描き込まれたまま（10/7 に光の無い絵で頼むかは見本を撮ってから）
- **発光 `_e` は箱庭ではまだ効かない**（2026-10-03）: `_e` を読む口はキャラの板（`StageUnits`）にしか無く、箱庭の道（`Diorama`・`DioramaTextures`）は半立体の `_n` だけ読む。
  今は炉・提灯のガラス・結晶は描き込まれた色のまま。光らせるなら統合（か S）が半立体の材料に `_e` の口を作る（`index.json` の各項目の `emissionUse` にも書いた）
- 色表へ写す前の距離 p95: 小物 2.6〜10.4（中央 約 4.8。吊り提灯 b が 10.4＝紫のガラス）。写した後は 0

### 小札（`index.json` の `litter`・背丈 12 ドット以下）
砂利 4（壁の青灰の岩の欠片 3〜5 個の不規則な塊）・小石 4・落ちた歯車 3（真鍮 2・鉄 1）・木くず 4。どれも粒を等間隔に並べない。

## 点検（`work/lint.md`）
全 140 枚: 半透明 0・面積 ≤30,000（最大 tunnel_mouth 96×97）・色表からの距離 p95 = 0（法線と発光は除く）・小札の背丈 ≤12・タイル 64×64。

tile-calm の検査で「端の差」「継ぎ目の比」が外れるタイルがある（`index.json` の `pass`）。中身は:
- 板（plank）: 板の継ぎ目の暗い線がタイルの左端の列に来る（回さずに敷くと継ぎ目の線が 16 列おきに並ぶ＝板として正しい）ので、縁の列の差が大きく出る。回し方は none（上の表）
- 坑木（timber）: 継ぎ目は中の列に散る。外れるのは継ぎ目の比が 1.6 を少し超える 2 枚（1.75）だけ
- 鉄・石畳: 模様の少ない面に筋や目地が縁をまたぐだけ（コード生成のタイルはどれも巻き戻しで作ってあり、つながりは崩れない）
- 岩（cliff）: 端の差 2.0〜3.5 レベル（幕1 でも `top_grass_alt_d` 3.0 を据え置いた）。撮影で継ぎ目が見えたら直す

## 決めていない・撮って確かめる所
- 回し方（`rot`）は推奨。設計図の `tiles` が決める（レーン C）
- 提灯の部品 `lantern-a`・`lantern-b` に使える絵: `lantern_post`（立つ灯柱）・`lantern_hang`／`lantern_hang_b`（吊り）・`lantern`（壁掛け・等角）
- 座席の床の「細かさ 40〜90」は撮影の値。模型は灯なし 48〜54・光溜まり 88〜98。撮影の hideui の床で測って、40 に届かなければ粒の明暗の幅、90 を大きく超えれば粒の量（`SEAT_GRAIN_STEPS`）を下げる
- 座席の床は rot4 で回すので、回ったタイルどうしの縁で大きな斑が途切れる（敷いた絵で縦横 64 テクセルおきにかすかに見える。直しの前から同じ）。撮影で格子に見えたら flipX にする

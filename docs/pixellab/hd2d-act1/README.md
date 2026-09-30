# HD-2D 見本 幕1の舞台とキャラの絵 (P05・2026-09-30)

計画 `docs/design/hd2d-slice-plan-2026-09-30.md` の包み P05「絵とスクリプト」の置き場。W1 (見た目を1画素も変えない波) なので、
ここで作った絵はどれも**まだどこからも読まれていない** (旗 `HD2DFlags` の後ろで P04・P11・P12・P22・P23 がつなぐ)。

## 置いた物

| 物 | 置き場 | 数 | 作り方 |
|---|---|---|---|
| 舞台のタイル 64×64 | `unity/Assets/Resources/Art/stage/act1/tiles/<材質>_{a,b,c,d}.png` | 7材質×4 | PixelLab pixflux 160×160 → `scripts/tile-calm.py` → `scripts/stage-palette.py map` |
| 半立体の元の絵 | `unity/Assets/Resources/Art/stage/act1/relief/<名前>.png` | 新規8 | PixelLab pixflux (view side) → `relief-clean.py` → 色表へ写す |
| 既存の小物の写し | `unity/Assets/Resources/Art/stage/act1/relief/act1_<名前>.png` | 16 | `Art/props/act1_*` を色表へ写しただけ (元の絵は変えていない) |
| 一覧 | `unity/Assets/Resources/Art/stage/act1/index.json` | — | 材質ごとの4枚の Resources のパス・回し方・半立体のドット数と 25/unit での大きさ |
| _KeyFlip の表 | `unity/Assets/Resources/Art/stage/act1/keyflip.json` | 16 (+候補9) | `scripts/art-lint.py` |
| 色表 | `docs/pixellab/hd2d-act1/palette-act1.{json,png}` | 48色 | タイル28＋新規の半立体8から Oklab の k-means |
| 48 の縮めた見本 | `Art/leaders/leader_green_48.png`・`Art/leaders/anim/leader_green_48_{idle0-7,attack0-3,block0-3}.png` | 17 | `scripts/hero-downscale.py` (清書ではない) |
| 法線 | 全キャラの隣に `<名前>_n.png` (リーダー・敵・人形・全コマ・48 も) | 163 | `scripts/sprite-normals.py normals --all` |
| 発光 | このは (一枚絵・全コマ・48 の全コマ)・狼・オーガ (128 と 96) の隣に `<名前>_e.png` | 37 | `scripts/sprite-normals.py emission --preset konoha|wolf|ogre` |
| 検査表 | `art-lint.md`・`art-lint.json`・`calm/check-mapped.json` | — | 下の「確かめ方」 |
| 確認シート | `sheets/*.png` | 6 | 62 と 48・法線と発光・タイル (2×2 に敷いた)・半立体・試し2つ |

### タイルの材質 (index.json)

| 材質 | 面 | 回し方 | 元 | 備考 |
|---|---|---|---|---|
| `top_grass` | 天面 | flipX | raw/tiles/top_grass_s2 | 芝と平たい苔の株 |
| `top_grass_alt` | 天面 | flipX | raw/tiles/top_grass_s1 | 大きな葉の株 (差し替え候補)。d は端の差 3.0 で検査を外れた |
| `top_path` | 天面 | rot4 | raw/tiles/top_path_s1 | 踏み固めた土。色表に写すと3色の静かな面 |
| `top_rock` | 天面 | rot4 | raw/tiles/top_rock_s1 | 平らな風化した岩 |
| `side_rock` | 縦 | flipX | raw/tiles/side_rock_s1 | 崖・段の縦の面 |
| `side_bark` | 縦 | flipX | raw/tiles/side_bark_s1 | 幹 (明暗をほかより絞った `--max-lap 640`) |
| `side_wood` | 縦 | flipX | raw/tiles/side_wood_s1 | 縦の板と釘。梁は 90° 回して貼る |

タイルは tile-calm 済み＝読み込み側 (`DioramaTileCandidate`) では `calm=1・sat=1` で使う (絞り直すと二重になる)。

### 半立体 (新規8)

| 名前 | 元 | 手直し | 大きさ (ドット) |
|---|---|---|---|
| canopy_1 | canopy_1_s1 | 幹と根を落とす (樹冠の下端で切る) | 107×62 |
| canopy_2 | canopy_2_s1 | 同上 | 124×65 |
| canopy_3 | canopy_3_s2 | 同上 | 77×72 |
| frame_1 | frame_1_s2 | そのまま (横に伸びる葉の枝) | 150×34 |
| frame_2 | frame_2_s2 | そのまま (上から垂れる葉と蔓の帯) | 159×58 |
| fern_1 | fern_1_s1 | 根元の土の皿を消す | 84×59 |
| fern_2 | fern_2_s1 | 同上 | 56×49 |
| bush_1 | bush_1_s1 | 同上 | 76×54 |

選んだ理由は `relief-clean.py` の `PICK`。

## PixelLab の使い方 (発注書と禁止語)

- 発注書: `template.json` (雛形)・`trial.json`・`tiles.json`・`relief.json`。実行は `node scripts/pixellab-v2.mjs order <発注書> [--dry] [--budget N] [--reserve 50]`。
  1つ作るごとに枠の残りを見て、予備を切るなら止まる。HTTP 4xx (402 枠切れ・422 引数) は止まる。使った数は `<発注書>.log.jsonl`
  (tiles.log.jsonl の 2〜6 行目の `used: 0` は記録の不具合を直す前の行＝正しくは `usage` の 1。直してある)。
- **禁止語** (描写にあると送る前に止まる。否定の文には書いてよい): `forest`・`night`・`moonlight`・`scene`・`moss green`・`dirt`。
  木・月・風景・段丘・緑の土を描き足すため (memory pixellab-stage-lessons)。土は `packed earth`／`soil`、夜は描かせず「evenly lit neutral daylight」で頼んで、夜はエンジンの光で作る。
- 新しい副命令: `balance` (v2 の枠の残り＝`subscription.generations`。v1 の balance は USD しか出さない)・`tilespro`・`mapobj`・`order`・`forbidden <文>`。
  `order` の kind は `tilespro`・`mapobj`・`pixflux` (v1。否定の文が効く)・`edit` (edit-images-v2)。

### 使った回数 (2026-09-30)

| 何 | 回数 |
|---|---|
| 試し map-objects 1本 | 1 |
| 試し create-tiles-pro 1本 | 25 |
| タイル pixflux 160×160 (6材質＋草の2シード目) | 7 |
| 半立体 pixflux (8種×2シード) | 16 |
| **合計** | **49** |

枠は 76.45 → 27.45 (Tier 2・10/7 に 5,000 へ戻る)。計画の見込み 150〜500 より少ないのは、始める時点で枠が 76 しか残っていなかったため
(9/29 の敵の向きと 128 化で使った)。48 の清書 (P23・edit-images-v2 は1回 20) は 10/7 の後でないと作れない。

### 試しの結果 (手順2)

- **map-objects** (1回 1生成・160×112・背景なし): 背景はきれいに抜ける・色数 50。ただし否定の文が無いので「幹なし」を守らず、木を丸ごと描いた (`sheets/trial-mapobj.png`)。安いので小物の下絵には使える。
- **create-tiles-pro** (1回 **25生成**・64px で **16枚**・`square_topdown`・`top-down`・`segmentation`): 番号つきの12項目に対して16枚返った。どれも色数3〜24の平らな絵で、右と下の縁に 1〜3px の案内線が残り、敷くとつながらない (`sheets/trial-tilespro.png`)。`tile-calm.py --trim 3` で線は消せるが、平らすぎて舞台の天面には使っていない。
  → **既知の道 (pixflux で大きく作って切る＋highpass) に落とした**。ただし 96 で作って中央 64 ではなく、**160 で作って中央 128 を 64×4** にした (1生成で4種そろう)。
- どちらの道でも、半立体を頼むと「幹なし」「地面なし」は守られない (pixflux も map-objects も)。樹冠は丸ごとの木から切り出し、羊歯と茂みは根元の土を消す手直しが要る (`relief-clean.py`)。2シード目で言い回しを変えた (tree の語を外し tree/trunk/soil を否定に) ら、泡の粒の集まり・星形の葉・台つきの門が出た＝**等間隔の粒は集合体恐怖症の地雷なので捨てた**。

## スクリプト

| スクリプト | 役目 |
|---|---|
| `scripts/art-lint.py` | キャラの規格の表 (輪郭の最暗色・白235超の割合・描き込まれた光の向き・台座の疑い・左右反転)・_KeyFlip の一覧・舞台の絵の色表からの距離 (p95・写す前と後) |
| `scripts/sprite-normals.py` | 法線 (輪郭からの距離で膨らませる＋明るさの細部)・発光 (色相の帯。`PRESETS`)・確認シート |
| `scripts/tile-calm.py` | 切り出し (継ぎ目が最も目立たない位置へずらす)・highpass・白い粒・明暗 1/3 (ラプラシアン分散 800 を超えれば自動でさらに絞る)・彩度 −20%・画像のキルティングで継ぎ目をなくす・検査 |
| `scripts/stage-palette.py` | 色表を作る (Oklab の k-means・種固定)・写す (最も近い色。`--dither` で秩序ディザ)・距離 |
| `scripts/hero-downscale.py` | このは v2 を多数決で 48 の背丈へ (一枚絵と全コマ・同じ倍率・足元の中央を基準) |
| `docs/pixellab/hd2d-act1/relief-clean.py` | 半立体の手直し (樹冠の下を切る・根元の土を消す・欠片を消す・切り詰め) |

作り直す順 (同じ入力なら同じ出力):

```
python3 scripts/tile-calm.py --outdir docs/pixellab/hd2d-act1/calm --json docs/pixellab/hd2d-act1/calm/calm.json --name top_grass docs/pixellab/hd2d-act1/raw/tiles/top_grass_s2.png
  (top_grass_alt=top_grass_s1・top_path・top_rock・side_rock・side_wood は <材質>_s1・side_bark は --max-lap 640)
python3 docs/pixellab/hd2d-act1/relief-clean.py docs/pixellab/hd2d-act1/raw/relief-clean
python3 scripts/stage-palette.py build --k 48 --out docs/pixellab/hd2d-act1/palette-act1 docs/pixellab/hd2d-act1/calm/*.png docs/pixellab/hd2d-act1/raw/relief-clean/*.png
python3 scripts/stage-palette.py map --palette docs/pixellab/hd2d-act1/palette-act1.json --outdir unity/Assets/Resources/Art/stage/act1/tiles docs/pixellab/hd2d-act1/calm/*.png
python3 scripts/stage-palette.py map --palette … --outdir unity/Assets/Resources/Art/stage/act1/relief docs/pixellab/hd2d-act1/raw/relief-clean/*.png unity/Assets/Resources/Art/props/act1_{leafclump1..5,bush2,bush3,fern,fern2,reed,root,rock_big1,rock_big2,tree_giant,tree_oak,tree_oak2}.png
python3 scripts/hero-downscale.py
python3 scripts/sprite-normals.py normals --all
python3 scripts/sprite-normals.py emission --preset konoha <このはの一枚絵・全コマ・48>   (wolf: enemy_wolf / ogre: enemy_brute・enemy_brute_96)
python3 scripts/art-lint.py
```

## 確かめ方の結果

- **タイル** (写した後・`calm/check-mapped.json`): ラプラシアン分散は 28枚すべて 800 以下 (257〜788)。端の差 (右端と左端の列・下端と上端の行の平均輝度の差) は 27枚が 2 レベル以下、`top_grass_alt_d` だけ 3.0 (中の隣どうしの列の差の 90 パーセンタイルも 2.95 あり、差し替え候補なので据え置き)。継ぎ目の段差の比 (縁をまたぐ差 ÷ 中の隣どうしの差) はどれも 1.6 以下。元のラプラシアン分散は 3,000〜33,000。
- **art-lint** (`art-lint.md`): キャラ 163枚 (うちアニメのコマ 48)。輪郭の最暗色の中央値は 5 (範囲 0〜46)・20〜30 に入るのは 12枚＝ほとんどの絵は輪郭がほぼ黒 (P23 の目安 20〜30 に遠い)。白 (235超) を含む絵 110/115。描き込まれた光は 上 42・左上 30・左 13・右上 11 ほか。台座の疑い 23枚 (人形は小さいので足が横幅の半分を超えやすく、疑いに入りやすい)。
- **_KeyFlip** (`keyflip.json`): 画像ファイルを左右反転した絵 16枚 (`.pixellab.json` の `mirrored`・`rotated` の「左右反転」)。光が右から描き込まれている絵 9枚は候補 (狼と血族の司祭は両方に入る)。
- **48 の見本**: 一枚絵の背丈 48 (元 62・倍率 0.7742)。コマは 45〜52 (元のポーズの背丈 58〜67 に比例。構え 58→45・振り上げ 67→52)。
- **法線と発光**: `sheets/normals-emission.png` (元・法線・左上の手前から照らした見本・発光)。発光はこのは＝斧の宝石と刃の結晶・真鍮 (攻撃の振り抜きのコマの橙の弧も光る)、狼＝胸の襟毛と尾の先のクリーム色 (11%)、オーガ＝鬣の金と手首の金具 (0.5%)。
- 舞台の絵の色表からの距離 (写す前の p95): 中央 2.6・最大 16.2 (`act1_leafclump3`・`act1_rock_big2` の月の青)。

# 幕2 の箱庭の設計図の生成器（HD-2D 段2・レーン C）

計画 `docs/design/hd2d-stage2-plan-2026-10-02.md` §2 C・約束 `docs/design/hd2d-stage2/contracts.md` C1・C3・C4・分析 `docs/design/hd2d-stage2-analysis-2026-10-02.md` §4-1。
設計図は手で直さず、ここで焼き直す（`Resources/Stage/act2_layout*.json` の `_doc` にも同じことを書いた）。

| ファイル | 役 |
|---|---|
| `gen_act2.py` | 4 枚の設計図を焼く（種 20261003・決定的）。焼いた後に `place2.py` で全部を数える |
| `place2.py` | 配置の規則の検査（R1〜R7・L8）と光なしの構図の画。gen_act2 も同じ関数（R6・R7 の `stand_info` など）で置き場を選ぶ。幕1 の `docs/design/hd2d-slice/r2-layout-gen/place.py` を import（place.py は直さない） |

## 使い方

```bash
cd docs/design/hd2d-stage2/layout-gen
python3 -B gen_act2.py                       # 4 枚を unity/Assets/Resources/Stage/ へ焼いて検査（約 5 分）
python3 -B gen_act2.py --only act2_layout    # 1 枚だけ
python3 -B gen_act2.py --out-dir /tmp/x --no-check
python3 -B place2.py ../../../../unity/Assets/Resources/Stage/act2_layout.json --img --md /tmp/place2.md   # 検査だけ
```

基準の撮影の UI の矩形（`~/.cache/deck-rogue/hd2d-stage2/shots/s2-base/s2-slice/{PC,PH}-S-{wolf,ogre,trio,quad,dolls}-1.layout.json`）が 1 つでも無ければ、
`gen_act2.py` も `place2.py` も名前を標準エラーに出して終了コード 1 で止まる（無いまま数えると R3 と UI の帯の判定が空の矩形で通って「違反 0」と出る）。
キャッシュを消した後・別の端末では、統合の基準の撮影（`s2-slice.txt`）を撮り直してから焼く。`--allow-missing-ui` を付けた時だけ警告して続ける。

出力の表と画は `~/.cache/deck-rogue/hd2d-stage2/lanes/C/`（`place2-report.md`・`place2-<設計図>.json`・`place2-<設計図>-<PC|PH|PC21>.png`）。
画は光なしの構図（地面は段ごとの色・半立体は絵・3D の形は箱の影絵・柵は箱で塗る）に、座席の帯（主人公 黄・人形 水色・敵 赤）・意図の札（橙）・幕ボス 160 の頭（赤）・UI の矩形を重ねた物。

| 設計図 | 中身 | 光の設計図 |
|---|---|---|
| `act2_layout.json` | 本番（岩棚の張り出し 2 unit） | `look_act2.json` |
| `act2_layout_wall3.json`／`_wall4.json` | 張り出し 3／4 の変種（棚の前 s 9.4 は同じで本体から奥を 1／2 unit 下げる。提灯と坑口は画面の x を本番にそろえた。小物は本番と同じ集合を本番と同じ置き場から先に試す＝張り出しを比べる撮影に小物の差を混ぜない） | `look_act2_wall3.json`／`_wall4.json` |
| `act2_layout_min.json` | 最小の試し（岩棚つきの壁 1 式＋提灯 2 と暈＋座席の帯の slab と段だけ・gates は緩い） | `look_act2_min.json` |

## 規則（place2.py の頭が正。違反 0 で焼く）

- **R1** 座席の帯（t −8.5〜13・s −2.6〜2.8）の高さ 0・部品の足跡が入らない（小札・段・霧・光・額縁・暈は除く）。`Diorama.Check` と同じ「届き」（`S2S_Reach` の写し・abs の部品は数えない・スマホは phone の t/s）でも 0
- **R2** 全座席（敵 1〜4 体〔枠 128／96／96・64・64／64〕・主人公・人形 9・PC/PH/PC21）の足元の通り（敵 ±140・主人公 ±60・人形 ±50〔スマホは 0.79 倍〕・行 = 頭か意図の札の上端〜足元 +20）に、背の高い物（地面に立つ高さ 1.2 以上か、浮いた物）の見える画素が 20px² を超えて入らない。壁（`wall-`）・棚の縁（`ledge-`）・土留め（`edge-`・`stake-`）・段をつなぐ階段（`steps-`）・レール・小札・霧・暈・額縁は数えない。生成器は主人公の列だけ低い物も入れない（帯の合否①を測る列）
- **R3** 意図の札の箱（基準の撮影の UI の矩形＋2 体と強個体の見積もり）と幕ボス 160 の頭（PC x 1200〜1550・行 0〜140）に部品を掛けない（壁・段・階段・レール・霧は除く・暈は「光」として別に数える）
- **R4** 小札は足元の通り（足元の 8 上〜20 下）に入らない
- **R5** 部品・小札・材質の数を設計図の `gates` と比べる
- **R6** 板（relief・card）の立ち方（PC とスマホの置き場）: 下端と「支え」（板の下端の線の下の段 slab と地面の block〔`wall-`・`steps-`〕の天面のいちばん高い所）の差が ±0.04 を超えない。沈み・浮き・片浮き（岩棚の区切りの段差を跨いで片側が浮く）を数える。吊る物（提灯・鍾乳石・垂れる根）は浮きと片浮きを数えない
- **R7** 板の貫き: 板の高さの範囲を block が通らない（柱・脚・歩廊の床・上段の張り出し）。自分の鉤（`<名前>-hook`）と坑口の枠（`mouth-`）は除く
- **L8** 額縁（kind frame と、名前 `frame-` の世界に置いた手前の 3D）と UI の重なり 30% 以下

## 決めごと（なぜそうしたか）

- 段は slab（block にすると `HeightAtPath` が下の坑道の −1.4 を返して部品が沈む）。壁と棚は abs の block（`"ao": false`）
- 頂（`wall-crown`・6.3〜11.5）を足した: 22°・5°・足元 0.36 では本体の上（y 6.3）が PC 行 107〜151 で、上部バー（72）の下に背景が出る
- 坑口は棚の上の台（y 3.6）に開く上の坑口: 二段目の高さに開くと敵 3〜4 体の座席の通り（行 328〜）と 4 体目の意図の札に掛かる。スマホでは組まない（上部バーの裏）＝光 `mouth-lamp` もスマホでは消す（レーン B が lights に `"phone": {"on": false}`。`StageLook.S2B_PartPlace` は部品の phone.hide を見ない）
- 坑口の札・柱・梁と提灯は、その幅と高さに掛かる壁の面（上段の張り出し 〜0.3 を含む `wall_face`）の手前に立てる。本体の前（`main_face`）を基準にすると上段の張り出しが坑口の上半分と提灯の上を覆う
- 提灯の下端は下の支え（岩棚の区切りの天面 2.75〜3.05・坑口の台 3.6）の 0.03 上、棚の上の小物は 0.02 上（`rest_on`。固定の y だと浮く物と沈む物が出る）
- 提灯 b は坑口の脇（絵の下端 y 3.62〜3.63 = PC 行 316）: 敵の側の壁は x 986〜1912 が全部どれかの座席の通り（幕ボス 128 は行 133〜）
- スマホの提灯は棚の前（s 9.28）の二段目の床（y 1.8）の 0.03 上に置く（y 1.83）: 棚の高さ 1.1 は提灯の背丈 1.4 より低く、棚の縁から吊ると下 0.35 が床に沈む。棚の天面に載せると上部バーの裏に 37% 入る
- 支保工の左の柱は t −8.2（計画の t −5.6 は炉 t −6.2 の手前）。梁 2 は y 6.2（計画 5.75）で、真ん中の柱をその高さまで伸ばして載せる
- 鍾乳石（天井から吊る物）は t ≥16（幕ボス 160 の頭 x 1200〜1550 の真上を空ける）
- 材質は 7 つ（card は書かない）＋霧の板 = 8。card を書くと 9 になり `Diorama.Check` が NG
- 額縁 2 は世界に置いた 3D（下の坑道の岩 2。左の支保工の柱は統合 2026-10-03 に外した＝画面の左端の黒い縦の帯に見えた）: 幕2 に支保工と岩の額縁に使える絵が無い（D: post_brace は置かない・rubble_rock は捨てた）
- D のタイル `top_earth`・`top_stone` は使わない: 段の天面に別の材質を使うと材質が 9 になり門（8）を超える（座席の帯も段も terrain＝`top_earth_seat`）
- 画（`place2-*.png`）は壁（`wall-`）を背景として先に描く＝上段の張り出しに埋まった物は画では見えない。埋まりは R6・R7 が数える
- 暈は `toward`・`lobe` を書かない（既定 = glow の softDepth だけ寄せる・lobe 1。`Diorama.cs` 頭の段2 の注記）
- 岩棚の天面の見かけは PC で 3〜6px（張り出し 2／3／4 で 3.1／4.5／5.8）。棚の天面 y 2.9 は目の高さ 3.93 の 1 unit 下なので、見下ろし 5° でもほぼ真横から見る（分析の「天面 10〜25px」は 5° の傾きで計算した値）

# 幕3 の箱庭の設計図の生成器（HD-2D 段2 段1b・レーン C3）

計画 `docs/design/hd2d-stage2-plan-2026-10-02.md` §0 裁定 6・§1 幕3 の合否・§2 C の段1b・約束 `docs/design/hd2d-stage2/contracts.md` C3（幕2 の名前の約束を幕3 でも同じ形で）・分析 `docs/design/hd2d-stage2-analysis-2026-10-02.md` §5-1・§5-2。
物差しは「ot7 の戦闘の作り＋Tomb の材質」・光は主人公の真後ろに斜めの柱・足元が頂点（光そのものはレーン B の `look_act3*.json`）。幕2 の節と同じく設計図は手で直さず焼き直す。

| ファイル | 役 |
|---|---|
| `gen_act3.py` | 4 枚の設計図を焼く（種 20261004・決定的）。`gen_act2.py` の `Judge`・`overlaps`・`Seeds`・`segments` を import する（gen_act2.py は直さない）。焼いた後に `place3.py` で数える |
| `place3.py` | `place2.py` の R1〜R7・L8 に幕3 の R8〜R13 を足した検査と、幕3 の材質の色の構図の画（place2 は直さない。`place2.HUNG_SRC`・`SUPPORT_NAMES`・`stand_blocks` を `place3.act3_rules()` の with の中だけ広げて、出る時に元へ戻す＝鎖・灯の頭は吊る物、灯柱 pillar も板の支えと貫きに数える。入るのは `check3` と `gen_act3.build` だけ＝place3 を import したプロセスで幕2 を place2 で検査しても幕2 の R6・R7 は甘くならない） |

```bash
cd docs/design/hd2d-stage2/layout-gen
python3 -B gen_act3.py                              # 4 枚を unity/Assets/Resources/Stage/ へ焼いて検査（約 15 分）
python3 -B gen_act3.py --only act3_layout_tier      # 1 枚だけ
python3 -B place3.py ../../../../unity/Assets/Resources/Stage/act3_layout.json --img   # 検査だけ
```

出力の表と画は `~/.cache/deck-rogue/hd2d-stage2/lanes/C3/`（`place3-report.md`・`place3-<設計図>.json`・`place3-<設計図>-<PC|PH|PC21>.png`＝光なしの構図に座席の帯・意図の札・UI・光の柱の四角形・名前・主人公の後ろの窓を重ねた物・`place3-<設計図>-scene-<場面>.png`＝5 場面〔巻物 4 体・彫師と影・石殻 1 体・門番 128・人形 9＋彫師・スマホの巻物 4 体〕の座席だけを箱で重ねた物）。

| 設計図 | 中身 | 光の設計図（レーン B が書く） |
|---|---|---|
| `act3_layout.json` | 本番 | `look_act3.json` |
| `act3_layout_tier.json` | 段の高さを t で変える（座席の外で左 t ≤−3.4 は T2〜T4 を 2 段 1→3→4、右 t ≥17.2 は 1→2.5→3.6→4。嵩上げは block `wall-tier-`） | `look_act3_tier.json`（`"layout": "act3_layout_tier"`） |
| `act3_layout_stairs.json` | 斜めの大階段を強める（幅 3.0→4.2・1 段 0.25→0.2・段ごとの側壁 `steps-cheek-`） | `look_act3_stairs.json` |
| `act3_layout_pillar1.json` | 光の柱の足を t −3.0→−2.4（B の backlight t −1.6→−1.0 と対）。裂け目 crack は柱の中心の線を上へ延ばして PC 行 90 に当たる点に付いていくので、足の差 0.6 より大きく t 4.8→5.8（+1.0）・y 7.9→7.95 動く（`_doc` にも生成器が本番からの動きを書く） | `look_act3_pillar1.json` |

## 幕3 の規則（place3.py の頭が正。幕2 の R1〜R7・L8 と合わせて違反 0 で焼く）

- **R8** 判定の画の小物（像・篝火・結晶・瓦礫・壺・碑の半立体と折れ柱 `pillar-broken`）が PC の画面に 8 以下（分析 §5-1。鎖・小札・灯の頭は数えない）
- **R9** 主人公の後ろの窓（PC x 300〜560・行 380〜560。art-bible §2-2）に暗い物（光・段・壁・階段・崩れ石・小札の外）が 20px² を超えて入らない
- **R10** 光の柱 `pillar-shaft-a` が主人公の真後ろ（PC の主人公の頭の行で、柱の幅の中の u 0.05〜0.6・柱の中心が主人公より右）
- **R11** スマホで組まない物の表（計画: 壁2・櫓・裂け目・池・機械の庭・mist-far）のうち設計図にある物（`crack`・`pond-bed`・`mist-far`・`wall-backstop`）がスマホで組まれていない
- **R12** 光の名前の約束（`fire1`・`fire2`・`fire1-halo`・`fire2-halo`・`lampC`・`crack`）が設計図にある
- **R13** 小札が block・柱・岩の中に埋まらない（PC とスマホの置き場。小札の中心から足跡の縁まで 0.15 の余白）。`HeightAtPath` は slab しか見ないので、block の足跡の中の小札は下の段の天面の高さに置かれて block の中に隠れる（控え壁・tier の嵩上げ `wall-tier-`・大階段 `steps-` と側壁 `steps-cheek-`・崩れ石）。place2 の R4 は小札の足元の通りしか見ない。生成器の `place_litter` も同じ判定（`place3.litter_buried`）で候補を外す

## 決めごと（なぜそうしたか）

- 段は slab（T1 +1.0 s 9／T2 +2.0 s 13／T3 +3.0 s 17.5／T4 +4.0 s 22・前の縁は折れ ±0.35〜0.6・角は 0.32 の斜め）。T4 の天面は目の高さ（PC 3.93）なので見えない＝立面 4 本の縞。水路は埋めた（5° では水面が近岸の縁の裏で 0px）
- 奥の壁は block の区切り（`wall-back-`・前 s 27・y 4〜11.6）。壁柱（`wall-pilaster-`）で縦の拍・門と裂け目と館の t は空ける。頂（`wall-crown-`）と奥の保険（`wall-backstop`）で PC21 の右の端まで上部バーの下に背景を出さない
- 縞を断つ: 大階段は t 14 の帯（道に沿って奥へ上る＝画面では右下 x ≈1600 から左上 x ≈1310 へ）。`steps-` は R2・R3 の背景（段と同じ）。控え壁 `buttress-a`（t −8.0）・`buttress-b`（t 4.4）は天面を y 3.9（目の高さの下）に止める: 上に出すと T4 の上の物（館・瓦礫・灯柱の足元 = PC 行 ≈293）を天面の縁で隠す（試しの y 4.65 で瓦礫 b の 7 割が隠れた）。崩落は T2 の縁 t 1.2〜3.6 を 1.45 奥へ（敵の通り x ≥986 の外）＋崩れ石 3（低い rock）＋瓦礫 a
- 光の柱 `pillar-shaft-a/b`（shaft・lobe 0）の足は主人公の右後ろの床（t −3.0・s 7.3 = PC x 501・行 625）、画面で縦から右へ 17.5° 傾けて上の口は画面の上の外（y 11）。T1〜T4 の立面の手前の空中を降りる（足を T1 の天面 s 12.4 に置くと立面 0〜1 が柱の下で暗く切れて「足元から上へ 1 本の勾配」にならない）。`gain` は頂点色で 1 が上限（`GlowVertexColor` の Clamp01）＝明るさは B の `materials.glow.shaftGain`。幅 3.0→5.2（主）／4.8→8.3（柔らかい縁・スマホでは組まない）＝本家 Tomb の柱の輝度 >80／>70 の幅 414／627px（1080p 換算）に合わせた
- 裂け目 `crack`（暈）は壁の面 s 26.85 の、柱の中心の線を上へ延ばして PC 行 90（上部バーのすぐ下）に来る点（t 4.8・y 7.9）。分析の s 33 y 9.9 は壁が 2 段（壁2 s 34）の案の値＝壁 1 枚（s 27）では壁の裏に隠れる
- 大門 `gate`（arch・T4 の上 t −1.0 s 26.25）は主人公の列の上（PC x 227〜518・行 −46〜301＝主人公の頭 458 より上）。暗い奥は block `wall-gate-recess`（tint 0.18）。左右の柱 `pillar-gate`（t −4.65）・`pillar-gate-R`（t 2.65）＝計画 §2 C 段1b の「柱 pillar 2（画面の中に）」。どちらも門の脚の外 1.15 の間合い。右の柱は裂け目の暈 crack（t 4.8）の左で、壁柱は門の左右 1.6 まで空ける。灯柱 `lamp-post` と折れ柱 `pillar-broken` は柱の数に入れない。右の柱の種は固定（`GATE_R_SEED`・後ろの部品の種をずらさない）
- 館（`house-`・brick）は T4 の上 t 8.2（裂け目の右・PC x 765〜987）。前に瓦礫 b・右の端の手前に灯柱（`lamp-post`＝細い pillar＋D3 の灯の頭 `lamp-head`＋暈 `lampC`・t 9.3・T4）。T3 に置くと灯柱の足元（行 351）が敵 4 体の 1 体目の通り（頭 350）に掛かる
- 篝火 `fire2` は T4 の右（t 25.6・s 22.9 = PC x 1659〜1711・行 201〜293）。計画の t 12.5（s 5.3 = x 1675・行 590）は全座席の足元の通り（x 1382〜1912・行 328〜）に入る（篝火の絵は 2.08 unit＝背の高い物）。t 24 は 2 体 96 の意図の札の見積もりに掛かる。スマホでは組まない（上部バーの裏）
- 胸壁 `parapet-`（T3 の縁の上の笠石＋小さな凸・t −0.6〜5.8）: 裁定 2 の幕3 の 3D「胸壁」。段の縞の上の縁を歯形に切る
- 瓦礫（幅 3.9 unit）は板を道の向きに回す（yaw −22）: 世界の yaw 0 だと下の線が s を 1.5 跨いで上の段に沈む（R6）
- スマホ: 上部バーの裏か画面の上の外に 4 割以上・からくりの帯／自分の札の裏に 3 割以上隠れる組は組まない（`phone_trim`。灯柱の組・館の組・像・瓦礫 b・控え壁 a）。篝火 fire1 はスマホでは t −6.8・s 8.2。スマホで fire1 に重なる小物はスマホだけ動かす（`phone_unstack`）。**レーン B の lights は部品の phone.hide を見ない**（`StageLook.S2B_PartPlace`）＝スマホで組まない灯（`lampC`・`fire2`・`crack`）は lights に `"phone": {"on": false}` が要る
- 材質は 7 つ（terrain・step・wall・pillar・brick・relief・glow）＋霧の板 = 8。card は書かない。タイルの名前は D3 の `Art/stage/act3/tiles/`（`side_wall`／`top_wall`・`side_step`／`top_step`・`top_floor_seat`・`side_pillar`／`top_pillar`・`side_brick`）で、無ければ `Art/tiles/act3_*` を calm（石積みは flipX だけ・D3 の index の決まり）
- 部品の門 partsMin は 130（幕2 は 160・計画の分析 R3-2 も 160。約束 C3 の幅 120〜160 の中）: 判定の画の小物を 8 以下にする（本家 Tomb の中景に小物はほぼ無い）ので部品が少ない（本番 146）。160 に戻すなら部品を 15 前後足す（小札は門の部品に数えるが、増やすと「磨いた石の床 = 少なく」に反する）＝統合の判断
- 暈の高さは D3 の `Art/stage/act3/index.json` の半立体の `glow.y`（光る所の中心・見えている絵の下端から。D3 の約束「暈と lights の高さはこれを使う」）から読む（`gen_act3.glow_y`）: 篝火 `fire1-halo`・`fire2-halo` は地面から 1.339（前は絵の高さ×0.78＝1.498）、灯の頭の暈 `lampC` は灯の頭の板の y 6.91 から 0.59 の 7.50（前は 7.619）。lights は at で暈に付いていくので光も同じだけ下がる。index に無ければ前の式に戻る
- 上の「スマホ」の続き: 2026-10-03 の時点で `look_act3.json` の lights の `lampC` に `"phone": {"on": false}` が無い（`fire2`・`crack` にはある）＝スマホでは灯柱の無い所（上部バーの裏か画面の上の外）に青白い点光源（range 4.5）が組まれ、影なしの光の枠 4 の 1 つを使う。B の持ち場なので統合で揃える

# 画風の規格（art bible）— HD-2D の舞台とキャラ

2026-10-01（HD-2D 見本 段1・W4 の P32）。同日 二周目（見劣り 3/5 の判定を受けたもう一周）でカメラ・霧の帯・木の置き方・コード生成の絵を足した（§2-1・§2-2・§3-4・§5-6。計画 `docs/design/hd2d-round2-plan-2026-10-01.md`・結果は計画 `hd2d-slice-plan-2026-09-30.md` §11）。
幕1・坑口の森の見本（枝 `hd2d/slice`・旗 `hd2d=slice`）で決めた「絵をどう作り、どう光を当てるか」の決まりを1か所にまとめた文書。
**新しい舞台の絵・キャラの絵を作る前、PixelLab に頼む前、光や色の値を動かす前に読む。** 幕2・幕3を作り直す時（段2）も同じ決まりを使う。

- 一次資料: 計画 `docs/design/hd2d-slice-plan-2026-09-30.md`・提案書 `docs/design/hd2d-quality-proposal-2026-09-30.md`・本家の色の設計書 `docs/design/hd2d-slice/honke-color.md`・門 `docs/design/hd2d-slice/gates.json`。
- **値の正本はコードと設計図の JSON**（この文書は理由と入口）。値を変えたら、この文書の数字も直す。
  - 光・霧・後処理・舞台の材質: `unity/Assets/Resources/Stage/look_act1.json`
  - キャラの光: `look_act1.char.json`／ぼかし: `look_act1.dof.json`／スマホの段: `look_act1.phone.json`
  - 箱庭の置き場と材質: `unity/Assets/Resources/Stage/act1_layout.json`
  - 舞台の絵の一覧: `unity/Assets/Resources/Art/stage/act1/index.json`・`keyflip.json`
- 「本家」＝オクトパストラベラー1。主に夜の森の戦闘（Steam の公式スクショ `ot_921570_16`）と比べる。オクトラ2は頭身が違うので混ぜない。

---

## 0. 大きな決まり（7つ）

1. **ドットの粒を1つにそろえる**: 世界は 25 テクセル/unit、座席のキャラは 1ドット＝画面 4px（PC）。場所によって粒が変わる絵は置かない。
2. **作り分けは本家どおり**: 人工の大物と地形は3D、木や草むらなどの自然物は表側だけ立体の「半立体」、草・小石・花だけ2Dの札。カメラの方を向く書き割りの板は舞台に置かない。
3. **光の向きは左上の手前から**: 絵に描き込む光も、舞台の月も、キャラのキーも、この向きにそろえる。
4. **キャラは舞台の光を受けない**: 全員に同じ向き・同じ色の固定の光（キー）を当て、影は1方向に1本。ドットに描き込んだ陰影を主役にする。
5. **舞台は夜の青にまとめ、キャラは昼の色を保つ**: 暗い所は黒でなく紺、明るい所ほど色が抜ける。キャラの暖色と白は冷やさない。
6. **地面は主役より静かに**: 地面の模様をキャラより目立たせない（ざらつき 1,500 以下・キャラ以下）。光溜まりと影が先に目に入るようにする。
7. **光と空気は舞台だけ**: 画面の紙（UI）は光もぼかしも受けない（今までの規約のまま）。

---

## 1. 粒（ドットの大きさ）

| 物 | 決まり | 正本 |
|---|---|---|
| 世界のテクスチャ | **25 テクセル/unit**（キャラの1ドット＝0.04 unit と同じ粒） | `DioramaMesh.TexelsPerUnit`・`index.json` の `texelsPerUnit` |
| 地面・崖のタイル | **64×64（2.56 unit）**。予備は 32×32。既存の act1 のタイル（32 と 64）は縮めずに 25/unit で使う | `DioramaMesh.TileTexels`・`index.json` の `tileSize` |
| 座席の1ドット | PC で **4.0±0.1px**（門⑩。必ず満たす）。スマホ相当（1920×886・UI 1.6倍）は 3.15px | `scripts/hd2d-measure.py` の⑩ |
| 主人公（このは） | **62 ドットが既定**（2026-10-01 W5 の判定。`hd2d=slice` に `herodots=62`＝幕1 と幕2/3 で背丈が同じ）。48 は `herodots=48` で見られる（v2 を縮めた見本のまま。W3〜W5 は 48 が既定だった） | `HD2DFlags` の `SliceBundle`・`Art/leaders/leader_green_48*` |
| 敵・人形 | 今の段のまま（通常 64・強個体 80・複数体の幕ボス 96・1体で戦う幕ボス 128・人形 32／大きい人形 48）。どれも1ドット 4px で置く | `docs/pixellab-assets.md` の寸法の表 |
| 地面の小札 | 背丈 **12 ドット以下**（0.48 unit） | `Diorama.LitterMaxTexels` |

- **絵の読み方**: キャラは Point（ドットの角を立てる）。舞台の絵（`/Art/stage/`）はバイリニア＋ミップで、ドットの縁だけ1画素なじませる（`fwidth` を使う `HD2D_FatTexel`。`Include/HD2DPixel.hlsl`）。光と影はドットの中心で計算し、1ドットの中で明るさを割らない（`StageModule` の `_TexelLighting`・キャラは `StageUnitLit`）。
- **ぎざぎざ取り（AA）**: MSAA だけ（PC 4x・スマホは切る）。キャラの板はアルファで切っているので中のドットはぼけない。FXAA・SMAA は使わない。
  - **例外（2026-10-01 三周目の裁定 Q2）: 戦闘のキャラの輪郭の1画素だけ中間色を許す**（ドットの中は 4px のまま・箱庭の `LitUnits` の時だけ・幕2/3 の `StageUnit` には効かせない）。本家はキャラの縁も 1〜2px の中間色で夜に沈み、うちは 4px の角の立った縁が「貼った感」の主因（鋭い縁の割合 オーガ 0.88 対 本家の魔物 0.35）。実装は三周目 レーン E（`StageUnitLit` の inline sampler）。同時に接地影は「芯のある影」から「薄く広い影＋足元の光溜まり」へ（裁定 Q3。§5-4 は三周目で書き直す）。
- **解像度は下げない**: 重い時も描画の解像度の倍率は使わない（4px のドットが崩れる）。落とす順は計画 §2-7。

---

## 2. 作り分け（本家どおり）

本家は、人工の大物（建物・櫓）を3Dで作り、木や岩などの自然物は表側だけ立体の絵（2.5D）にしている。うちも同じにする。
**裁定②「全部3D」は「カメラの方を向く書き割りの板を舞台から無くす」と読む**（2026-09-30）。

| 物 | 作り方 | 設計図の kind（見本の数） | 正本 |
|---|---|---|---|
| 人工の大物（坑口の櫓・柵・標） | 完全な3D（コードで作る形） | `rig` 1・`fence` 5・`marker` 2 | `DioramaMesh` |
| 地形（地面・崖・段・岩棚） | 3Dのメッシュ | `block` 9・`slab` 5 | `DioramaMesh` |
| 岩 | 低ポリの面取り多面体（8〜20面・面ごとに平面の UV） | `rock` 37 | `DioramaMesh` |
| 自然物（樹冠・幹・根・茂み・羊歯・葉の塊） | **半立体**＝スプライトの輪郭から高さを作り、表側だけ膨らませて裏面を持たない形。深さは既定で幅の 0.12 倍（設計図の `sources` の `depth` で絵ごとに 0.06〜0.2。奥の幹は 0.1）。カメラの方へは回さず、決まった向きで立てる | `relief` 75・`tree` 7（幹は `trunk=relief` が既定） | `ReliefMesh` |
| 草・小石・花・茸 | 2Dの札 | `card` 123 | `Diorama`（`CardFor`） |
| 地面の小札（小石・草の株・落ち葉・小枝） | 2Dの札・背丈12ドット以下・座席の帯にも置ける | `litter` 39 | `Diorama` |
| 霧の面・月光の筋 | 光の面（加算・深度で縁を消す・霧に沈む） | `fog` 3・`shaft` 4 | `StageShaft` |
| 手前の額縁（枝葉） | 半立体。カメラから深さ6〜10に置き、大きくぼかす。UI の札に隠れる所には置かない | `frame` 3 | `Diorama.OnCameraLayout` |

- **3Dの形の作法**（古い3Dに見せないため）: 崖と段の凹凸は**輪郭（平面の中の折れ線の位置）にだけ**入れ、法線方向のノイズは入れない。折れ線は 0.32〜1.28 unit の格子に合わせる。陰は頂点色で付ける（凹みほど暗い AO）。
- **FBX で差し替える口**: `Resources/Stage/Act<N>/Models/<名前>` に形（Read/Write 有効）を置けば、同じ名前のコードの形と入れ替わる（今は置いていない）。
- **数の上限**（`Diorama` の門）: 部品 250〜350（見本 313）・Renderer 120 以下・材質 6 以下・三角形 25万以下・地面の小札 80 以下。座席の帯（s −3〜+4）の高さは 0 で、背丈のある部品を立てない。
  - 二周目の設計図は部品 242（litter 110・relief 54・card 47・rock 12・slab 5・fog 4・shaft 4・frame 4〔背景の板を含む〕・rig 1・block 1）・三角形 73,043・Renderer 27・材質 6。**`Diorama.Check` の門のうち「部品 250 以上」「地面の小札 80 以下」の2つを外れている**（丸い木と柵を外し、座席の縁を小札の株で切ったため）。重さは W5 と同じなので、門の数を直すか設計図を直すかは次の周で決める。

### 2-1 構図とカメラ（二周目 2026-10-01）
- **画角 22°・見下ろし PC 5°／スマホ 7°**（旗の束 `hd2d=slice` の `cam=22;pitch=5;pitchphone=7`）。足元の線は PC 0.407・スマホ 0.525（`StageCamera.GroundLineRatio`）。W5 は 28°・12°。
  - 地平線は PC の行 297・スマホ 163（1920×886）。画面の端の縦の傾き PC 1.73°（本家 0.3° 以下・W5 5.05°）・スマホ 2.96°。
  - スマホは足元の線が高いので、5° だと地平線が顔にかかる → 7°。
- **目の高さは主人公の約 1.4 倍**（本家は 2.4〜2.7 倍）。低いので奥の地面は潰れて写る＝段の天面は 20〜25px にしかならない。手間は段より縦の層（幹・霧・枝）にかける。
- 今の舞台（幕2/3）は 36°・12° のまま（`OldBundle` が数で戻す）。旗 `pitch=` を変えて撮る時は `pitchphone=` も書く（書かないと束の 7 がスマホで勝つ）。W5 のカメラは `cam=28;pitch=12;pitchphone=off;groundline=0.41`。

### 2-2 木と部品の置き方（二周目。配置の計算は `docs/design/hd2d-slice/r2-layout-gen/place.py` の写し）
- **主人公の後ろの窓**（PC x300〜560・行380〜560／スマホ x230〜430・行230〜380）に暗い物（幹・岩・木・根・土の皿）を入れない。
- **太い幹**（見かけの幅 40px 以上）は、1〜2体の敵の足元の x の ±130px に置かない（PC の足元の x: 1体 1374／2体 1267・1522）。霧の 0.7 倍より明るく沈めた細い幹は、3〜4体の座席の後ろなら置ける。意図の札の箱に幹・月光の筋を掛けない。
- **霧の帯と画面の外側には幹だけ**（画面の上を突き抜ける長い幹と細い幹）と、霧に沈めた針葉の枝の影絵。**丸い樹冠の木（oak・giant・pine の全身）は霧の帯と座席の後ろに置かない**（小さな丸い木は公園の木に見える）。濃い樹冠で上の真ん中を塞がない（本家の上は霧に透けた高い枝で、天井ではない）。
- **左右の端の近い木立**は暗い壁にしない: 幹の右の縁を画面の x≈180 までにして、その内側に霧と月光の筋を見せる（二周目の直しの輪2: 平均 7.6→17.6）。
- **座席の帯**（t −8.5〜13・s −2.6〜2.8）には部品を置かない（背丈 12 ドット以下の小札だけ）。帯の前の縁は石の縁石の線にしない＝段差を低く（前の段 top −0.06）・面取り 0.3・縁を株の塊で不規則に切る。
- **手前の草**は背の高い物を世界に置かない（低いカメラでは手前の物が大きく写り、敵の足にかかる）。背の高い草は額縁（カメラに付く `frame`）で左右の下の角に。世界に置く株は「株の先の行 ＞ その x でいちばん低い足の行 ＋20」。
- **額縁の下端は vy 0.83 より上**（幕ボスの登場と撃破の寄り・待機の漂いの余裕）。上の縁から部品の先だけがのぞく置き方をしない（直しの輪2 で蔦の額縁を画面の外へ出した）。
- **奥の端**は地面を s70 まで伸ばし、カメラに付く背景の板（深さ 140・`backdrop_plain`）を霧で満たして消す。地面の端が見える所は、その深さで距離の霧が 0.9 以上。

**三周目（2026-10-02。生成器 `docs/design/hd2d-slice/r2-layout-gen/gen_r3.py`・`gen_layout.py --r3 --repo`・検査 `place.py --r3`）で上の規則に足した物**
- **森は「1本の木＝幹＋枝＋房が1枚の絵」を3段の距離に**: 帯（s 7〜14）は細い針葉樹 `conifer_w20`（太い w48 は奥だけ＝霧の中でも柱に見える）・奥の木（s 15〜22）・最奥の列（s 35〜38）。手前寄りの帯と敵の側の霞んだ木は**密な針葉樹 `conifer_dense`**。暗く見える幹は帯の中に 6〜8 本（本家 7）。
- **上の覆いは近い木の段で**: 密な針葉樹を手前の近い幹のすぐ奥の同じ x に置き、いちばん下の段が画面の上の方に来る高さへ持ち上げる（`abs`。根元は手前の幹の後ろに隠す）。大きな垂れる枝を画面で幅 400px 以上にしない（椰子の葉に見えた）。覆いは 35〜45%。x 1200〜1550・行 0〜140 は幕ボス 160 の頭と意図の札のために空ける。
- **土手は低い茂みを途切れ途切れに**（l は置かず m と s・間に株と草）。羊歯を同じ高さで一列に並べない（鉢植えの列）。茂みを暗い影絵にしない（霧の帯の下に黒い溝ができた）。
- **座席の帯の前の縁**は段差 0.01 以下・縁の形を不規則に・低い株を縁に沿って。手前の地面を暗くしない（草の影絵が溶ける）。
- **霧に沈めた木は意図の札の後ろを通ってよい**（細い針葉樹は霧 R 0.45 以上・密な針葉樹と 160 のボスの箱は 0.8 以上。`place.r3_hazy_tree_ok`）＝「意図の札の箱に幹を掛けない」の例外。
- **額縁の草**は穂の間隔が画面で 40px 以上の影絵・tint 0.75（0.5 は墨の爪に見えた）。手札の真ん中の後ろの3枚は `onlyWith: uitrial`（UI の作り直しで手札を沈めた時だけ見える）。

---

## 3. 舞台のテクスチャ（地面・崖・幹）

### 3-1 貼り方
- 道の座標（道の向き −22° で逆回転したローカルの座標）で、**いちばん近い軸を1つだけ選んで投影**する（3軸の混ぜはしない）。UV はテクセルの格子に合わせる。
- 天面の苔（草）と側面（土・岩）の切り替えは、ドット単位のディザで段々に切る（`_TopThreshold`・`_TopBlend`）。
- 材質ごとに4種のタイルを `Texture2DArray` に積み、CPU の `SetPixels32` で埋める（`DioramaTextures`）。

### 3-2 タイルの回し方（材質ごと）
向きのない材質だけ回してよい。草は左右の反転だけ（回すと葉の向きで遠近が狂う）。

| 材質 | 面 | 回し方 |
|---|---|---|
| `top_grass`・`top_grass_alt`・`top_grass_seat` | 天面 | `flipX` |
| `top_path`・`top_path_seat`・`top_rock` | 天面 | `rot4` |
| `side_rock`・`side_bark`・`side_wood` | 縦の面 | `flipX`（梁は板を 90° 回して貼る） |

### 3-3 地面は「光を受ける静かな面」
- 本家の地面のざらつき（ラプラシアン分散）は 178〜214。今までのうちの土は約9,000 で、主役より騒がしかった。**門⑤は 1,500 以下かつキャラ以下**。
- **奥の段**: `scripts/tile-calm.py` で明暗を約 1/3・彩度 −20%・白い粒を消す（タイルのラプラシアン分散 800 以下・端の差 2 レベル以下）。
- **座席の帯**（2026-09-30 ユーザー「本家っぽく」⑧＝地面の模様は読める程度に戻す）: 草は明暗 0.6（`tile-calm.py --contrast 0.6 --no-auto`）・土はコード生成の踏み固めた土（斑・細かい粒・小石・ひび・草の芽。色は色表の色だけ）・地面の小札を不規則に控えめに（群れの中心 15・1〜4枚ずつ・足元の通りと匣は避ける）。見本の⑤は 495（キャラ 4,737）。
- **土はコード生成にする**（PixelLab の土は泡・段丘・丸い塊になる）。
- 白い小石の粒・写実的なひびの札は置かない（寝かせた札は見下ろし約14°で縦が 1/4 に潰れてドットが壊れる。ひびはタイルの中に描く）。
- 二周目（見下ろし 5°）では地面の縦がさらに潰れる（座席の縁のタイルは縦 0.09 倍で段々の欠片に見えた）。座席の前の縁は手前のぼけ（`pathBand` の手前 −2.4）に入れてなめらかにする。地面の配列は PC で異方性 8（`DioramaTextures`・設計図の `anisoLevel`）。

### 3-4 コード生成の絵（二周目。PixelLab を使わずに作った物）
作り方は `docs/pixellab/hd2d-act1/gen_art.py`（`w3b <out>`＝W3b の18枚を画素まで同じに作り直せる・`r2 <out>`＝二周目の25枚）。**色は幕1の色表の48色だけ**（色表からの距離 p95 = 0）。今ある絵は上書きせず、新しい名前で置く。

| 絵 | 中身 | 決まり |
|---|---|---|
| 長い幹 `trunk_long_1〜3` | 幅 40〜56・高さ 280 ドット＝画面の上を突き抜ける。光の側（左 22%）に途切れる明るい縦の筋・斜め上へ伸びて先が垂れる細い枝 3〜6 本 | 霧の帯の幹と左右の端の木立。丸い樹冠は付けない |
| 細い幹 `trunk_thin_1〜3` | 幅 16〜24・枝 2〜4 本 | 霧に沈めて奥へ。3〜4体の座席の後ろにも置ける |
| 針葉の枝 `needle_bough_1〜4` | 主枝の下に垂れる房の塊（不透明 35〜44%・色相 208〜213°） | 霧の帯の上にまばらに。細い線の網は霧に沈めると網の目にしか見えなかったので使わない |
| 立った草の株 `tuft_stand_1〜4` | 背丈 12・12・16・20 ドット。根元は台形と茎 2〜3 本（不透明の四角を作らない＝鉢植えに見えた） | 座席の縁と段の根元に塊で。等間隔にしない |
| 座席の土 `top_path_seat_r2_a〜d` | 明るい点 176→49／1万画素（乾いた灰色の斑の閾値 0.91） | W5 の写しの `top_path_seat_*` は残す |
| 背景の板 `backdrop_plain` | 32×32・不透明の無地 #204043（色表の暗い紺緑） | カメラの奥（深さ 140）で霧に満たされる |
| そのほか | 垂れ葉の紺緑・大樹の皿を切った写し・小物（切り株・道標・松・背の高い草）の色表への写し | — |
- 敵と人形の待機の4コマ（`Art/enemies/anim/<名前>_r2idle_<0..3>`）も `scripts/gen-enemy-idle.py` のコード生成（足元は動かさず頭だけ 1ドット上下）。PixelLab の breathing-idle が使えるようになったら差し替える。

**三周目（2026-10-02・`gen_art.py r3 <out>`・種固定で再現）**
| 絵 | 中身 | 決まり |
|---|---|---|
| 霧の帯の針葉樹 `conifer_w20/32/48_{1,2}` | 高さ 280・幹の幅は名前の数字。弓なりの枝 10〜13 本（上ほど短い）・梢は細かい段の尖り。**幹は暗い影絵＋左の縁の明るい筋 2〜3 ドット（明暗差 40 以上）**・樹皮の筋も 2〜3 ドット（1 ドット幅は奥のぼかしで消える）。cos の円筒陰影は使わない | 帯は w20、奥は w32・w48。flip しない（光が左上に描いてある） |
| 密な針葉樹 `conifer_dense_1〜4` | 150×300／200×340・幹 11〜16。段 16〜23 を下から描き上の段が上書き・**段の下の縁はのこぎり（2.5〜4.5 ドットの歯・不規則）**・段の上の縁だけ明るい | 近い木（上の覆い）と帯の手前・敵の側の霞んだ木 |
| 近い幹 `conifer_near_1/2` | 幅 36/42・高さ 320・枝 1〜2 本と付け根の残り | 左右の端で画面の上を突き抜ける |
| 垂れる枝 `bough_hang_1〜4` | 160×80・付け根は左の辺。段 3〜5 枚を楔形に積む | 霧 0.3 以上で薄く。大きく置かない |
| 低木 `bush_clump_{l,m,s}_{1,2}` | 低く横に長い不規則な山 3〜8 個・上の縁は小枝と針の先がぎざぎざ・草の葉 3〜6 本 | ドーム（生け垣）にしない |
| 手前の草 `fore_grass_1〜6` | 太い葉の影絵・葉先の間隔 11 ドット以上 | 額縁で |
| 座席の土 `top_path_seat_r3_a〜d` | 二周目の土に 1〜2 テクセルの粒（±6%） | 縦に 1/8 に潰れても土に読める |
| 舞台の法線 `<名前>_n.png` | `sprite-normals.py normals-stage`（局所の太さの円柱＋明るさの細部） | 半立体の法線アトラスが読む（強さは設計図 `surfaces.relief.normal`）。見た目の効きは小さい |
- 房・茂み・草は**不透明の影絵**（α は 0 か 255）。半立体は α 0.4 で切るので、半透明の房は網・旗になる。
- 10/7 の PixelLab の描き直しは、同じ名前・同じ足元の約束のまま差し替える（発注書 `docs/pixellab/hd2d-r3-order-draft.md`）。コード生成の針葉には点描の粒が無い＝最後の差は絵で埋める。

---

## 4. 色

### 4-1 色表（舞台の絵）
- 幕1の舞台の絵（キャラ以外）は、**幕1の色表 48 色**（`docs/pixellab/hd2d-act1/palette-act1.json`）へ Oklab の距離で写してそろえる（`scripts/stage-palette.py map`）。出所の違う絵（PixelLab・既存の小物・コード生成）の色がそろう。
- コードで作る舞台の絵も、色表の色だけを使う。
- **キャラは色表へ写さない。**
- はみ出しは `scripts/art-lint.py` の「色表からの距離の 95 パーセンタイル」で見る。
- 幕2・3は幕ごとに別の色表を作る（同じ手順 `stage-palette.py build`）。

### 4-2 本家の夜の色（見本の舞台で寄せた形）
本家の夜の森を測った結果（`honke-color.md` §0）を、舞台の色の決まりにする。

1. **舞台の色は青ひと色にまとめる**（色相 195〜235° の帯に 85% 以上）。
2. **暗い所は黒でなく紺**（暗部の平均 b* −8 以下・彩度 C* 10 以上・赤はほぼ 0）。
3. **明るくなるほど色が抜けて白に近づく**（L* 60〜75 の彩度 C* 8 以下）。
4. **画面でいちばん明るいのは奥の霧**（主人公たちの後ろの帯。本家は輝度 152・淡い青灰）。樹冠は霧の 0.5〜0.7 倍、座席の地面はもっと暗く、手前の額縁がいちばん暗い。
5. **キャラは舞台の色に溶かさない**（暖色の彩度は元の絵の 0.8 倍以上・白は b* +5〜+9 の暖かい白）。

寄せる手段（どれも**舞台だけ**に掛かり、キャラの板には掛からない）:
- `envGrade`（`look_act1.json`）: 舞台の絵（`StageModule`）の彩度を落とし（0.64）、青へ寄せる倍率（[0.74, 0.90, 1.08]）。
- 光の色（月・舞台の灯・逆光・環境光・霧・高さの霧・霧の光の芯）を青へ。
- 舞台だけの周辺減光 `stageVignette`（画面の左右の外 18% と上の 14% を沈める。キャラと UI には掛からない）。

キャラにも掛かる後処理は控えめにする: 色の膜（`colorFilter`）は使わない（1,1,1）・暗部だけ紺（`lift`）・明部をわずかに暖かく（`gain` は本家の目安の半分）・中間は触らない（`gamma` 1 = 肌を青くしない）・コントラスト 14・トーンマップ ACES。キャラは `cancelColorFilter` で舞台の色の膜を打ち消す。

測り方: `scripts/hd2d-honke-targets.py`（本家の色の目安 21項目。見本 W3b は 16/21）と `scripts/hd2d-color.py`（Oklab の目安 8つ。W3b は 6/8）。

### 4-3 彩度の上限は入れない（**ユーザーに確かめる項目**）
- キャラの絵に彩度の上限は入れない。このは v2 の高彩度（ディスガイア系のポップな画風・ユーザーの裁定）を守り、そろえるのは輪郭の色・白・光の向きだけ。
- トライアングルストラテジーが彩度を抑えたのは大人っぽい世界観のためで、本家の規則ではない。
- 舞台の `envGrade` の彩度落としは「本家の夜の色へ寄せる」ためで、キャラの上限とは別の話。
- **見本の判定（W5・P30）でユーザーに確かめる**（計画 §7 リスク17・提案書の問5）。

---

## 5. 光

### 5-1 光の向き（art-lint で測った）
- キャラの絵 115 枚（リーダーの一枚絵・敵・人形。アニメのコマは除く）の「描き込まれた光の向き」（`scripts/art-lint.py`。明るい画素の重心 − 形の重心）は **上 42・左上 30・左 13・右上 11**・ほか少し。→ **光は左上から**にそろえる。
- 舞台の月（平行光）は**左上の手前から**（`look_act1.json` の `moon.euler` [48, 38, 0]）。
- **新しい絵は「lit softly from the upper left」で頼む**（発注書の雛形 `docs/pixellab/hd2d-act1/template.json` の `_reliefText`）。
- 光が**右から**描き込まれている絵は `_KeyFlip`（キーの向きの X を反転）で合わせる。対象は art-lint が測った表（`keyflip.json` の `lightFromRight` 9枚＋`look_act1.char.json` の `keyFlipAdd` の `enemy_scald_gnat`）。`keyFlipFrom: measured`。
  - 「画像ファイルを左右反転した絵」（`keyflip.json` の `keyflip`・`.pixellab.json` の `mirrored`）で選ぶと、16枚中9枚が左からの光で逆に当たる（W3 で分かった）。反転の有無ではなく、描き込みの向きで選ぶ。
- **塗り直しは `_KeyFlip` で直らない絵だけ**（edit-images-v2 で「光は左上から」）。全部の絵を塗り直さない（PixelLab の往復が増え、元に戻せなくなる）。

### 5-2 舞台の光（`look_act1.json`・持ち主 P22）
| 光 | 役目 | 影を落とす物 |
|---|---|---|
| 月（平行光・左上の手前から） | 舞台の形を影で読ませる | **地形と大物だけ**（`shadowLayers: environment`） |
| 舞台の灯（スポット・カメラ側の上から座席の帯へ） | 座席の帯と奥の真ん中だけを灯す（外は 0.05 倍）。木漏れ日のクッキー（大きく不定形な明暗 4つ・座席の帯のコントラスト 0.2 以下） | **キャラの板だけ**（`shadowLayers: characters`） |
| 逆光（坑口の奥の脈の青緑の点光源） | 奥の霧を光らせる | 地形（PC だけ） |
| 技の光（`StageFx.HitLight`） | 当たりの近くの地面を一瞬明るく。同時に3つまで | 大きい当たりだけ |
- 影を落とす光は画面に最大3つ（スマホは2つ）。
- 環境光は Trilight（空・地平・地面の3色）。遮蔽は頂点色の AO と SSAO（PC だけ）。ライトマップは焼かない。
- 材質ごとの受光（`materials`）: 地形・岩・木・樹皮 0.8／半立体 0.35／札 0.25。影の強さは別の値（`shadowStrength`）。二重の陰影で泥やプラスチックのように見えるのを防ぐ。
- **二周目の値**: 舞台の灯 `lamp.illum` 2.6・色 [0.70, 0.82, 1.0]（座席の地面を「月に照らされた青い道」に。地面の中央 81＝本家 101 の帯の下端）。灯を上げると暗めの敵の足元の輪も上がる（オーガの門⑧ と両立しない所＝計画 §11-4）。
- 舞台だけの周辺減光 `stageVignette`: **上は両端だけ**（`top` 0.12・`topBand` 0.14）・横 `side` 0.6（スマホ 0.75＝門③の中央÷端を守る）・`sideStart` 0.30。座席の帯（`_HD2DSeatBand`）の中は横の減光を受けない。
- 色の寄せ（二周目）: `envGrade.tint` [0.78, 0.90, 1.08]・`lift` [0.955, 1.01, 1.04, −0.011]（変種 `look_act1_r2liftref` へ5割。そのものにすると本家の色の目安の暗部が外れる）。
- 月の塵（二周目）: 箱庭だけの粒 `moondust-slice`。箱はピントの帯の中（s −1.5〜4.5）・大きさ 0.07〜0.14。粒の芯だけ深さを書く材質（`Shaders/StageDust`）で、ぼかしに芯を座席の帯の中と読ませる（深さを書かない粒は後ろの霧の深さでぼけて丸くにじむ）。HDR の明るさは材質の強さで掛ける（頂点の色は 0〜1 に丸められる）。

### 5-3 キャラの光（`look_act1.char.json`・持ち主 P23・シェーダ `StageUnitLit`）
- **キャラの板は場の光を受けない。** 全員に同じ向き・同じ色の固定のキー（向き＝舞台の灯の向き `StageLook.KeyDir`）と、上下の環境光だけを受ける（本家は写実的な光を当てると薄っぺらく見えたので固定の光にしている）。
- **キーの色は [1.0, 0.95, 0.88]**（本家の夜でもキャラの白は暖かい白。`honke-color.md` §5）。計画の初めの既定は中立の [0.92, 0.96, 1.0] だったが、白が冷える（狼の白 b* −2）ので W3b で少し暖かくした。暖色の変種は `keycolor=warm`（`keyWarm` [1.0, 0.92, 0.8]）。
- 受光 0.6（このはは 0.9）・環境光の倍率 1.1・主役の持ち上げ 1.2（このはは 1.5）・このはだけ暗い色の持ち上げ（`shadeLift` [2.0, 0.2]。黒鉄の衣と大斧が ACES で潰れるのを戻す）。旗 `receive=`・`herolift=` はファイルの値より勝つ。
- **主人公は舞台の中央値より明るく、足元の地面より明るく**（門⑧: 差 15 以上か比 1.4 以上。本家は地面の 1.5〜2.2 倍）。見本 W3b のこのはは体 82・足元の輪 48。
  - **例外＝このは（三周目 直しの輪4・2026-10-02・ユーザー「このはの色彩は0周目の時っぽくできる？」）**: このはの色は0周目（今の舞台 `stage=old`）に忠実にする。平らな光（受光 0.34・環境光 1.57）・舞台の灯を受けない・暗い色の持ち上げなし・輪郭の持ち上げは赤紫で 0.8 倍・冷たい色の掛け算（`tint`）・白の上限なし・霧 1・宝石の光の板なし。体の中央値は 35 前後で舞台より暗い（この規則の「中央値より明るく」は外れる。足元の輪との差は満たす）。明るくしたい時は変種 `look_act1_r3k0c`（体 62）。絵ごとの上書きのキーは `tint`・`whiteCap`・`fog`・`rim`・`gemHalo`・`blackLiftColor`（`look_act1.char.json` の `art`）。
- **受光を 0 にすれば光だけ元に戻る**（戻し方の1段目。2段目は `HD2DSetup.Revert` で設定を丸ごと戻す）。
- **二周目の値**（`look_act1.char.json`）:
  - このは: `heroLift` 1.7・`shadeLift` [2.4, 0.2]・彩度 1.2（W5 の 2.0 と [3.0, 0.2] は白く光って浮いた＝上位5% 250・白飛び 7.7%）。体の中央値 136（本家ハンイット 157・目標 110 以上）。
  - **暗めの敵は本家どおり暗い体**（本家の魔物の体 55）: 既定 `art."enemy_"` の `ambientScale` 0.65・受光 0.7、オーガ 0.6、噛みつく巻物 0.85、白い狼は 0.8（発光 0.9 で体は明るいまま）。楽隊の人形だけ 1.6（足元の輪との差を保つ）。
  - キャラの彩度 `saturation` 1.25（`_CharSat`。光の後・色の膜の前）。リムは敵 0・人形 0.1・主人公 0.25（`rim`・`dollRim`・`heroRim`）。キャラの板が霧を受ける割合 `fog` 0.25。
  - 斧の宝石の灯 `heroGem`（青緑・強さ 0.6）。待機以外のコマでは光の板を隠す（振りの間に宙に残らない）。旧・頭の上の暖色の円（`HaloUvFor`）は見本では宝石の位置へ（§9 の未決は閉じた）。

### 5-4 影は1方向
- キャラの影は、舞台の灯（スポット）からだけ落ちる（URP の Rendering Layers で分ける。キャラの板は Characters・舞台は Environment）。**影は1体に1本、右奥へ**。
- 月はキャラの影を落とさない（2本の影にしない）。
- 接地影の楕円は濃さ 0.35・幅 0.6 倍（影の本体はスポットの影）。**二周目（`charshadow=1` の時だけ）は芯のある影**（半径の 55% まで平らで、そこから滑らかに 0。`R2E_BlobCoreTex`）を幅 ×0.85・濃さ ×0.75 で置き、同じ所に広く薄い暈（幅 1.8 倍・濃さ 0.3）を重ねる（本家の魔物の足の真下は左右の地面の 0.6 倍）。
- 旗 `charshadow=1`（`hd2d=slice` に入っている）。
- **三周目（裁定 Q3・2026-10-01）: 芯のある影をやめ、薄く広い影＋足元の光溜まり**（`contactCore` 0.25・`contactWidth` 1.15・`contactAlpha` 0.1・暈 0.1・宝石の灯 `heroGem.range` 3.5）。本家は足元の方が明るく影はほぼ見えない（足の真下÷左右 本家 0.93〜0.98）。三周目は主人公 0.86。足が浮いて見えたら 0.15／0.12 へ。

### 5-5 法線と発光（キャラ）
- **法線 `<名前>_n.png`**: `scripts/sprite-normals.py normals`。輪郭からの距離で膨らませる丸み＋明るさの細部。向きは赤＝+x 右・緑＝+y 上・青＝+z 手前（OpenGL 形。逆の絵は `_NormalYSign` −1）。透明の所は (128,128,255)。sRGB なし・Point で取り込む。シェーダの中で `tex×2−1` でほどく（`UnpackNormal` は使わない）。光はドットの中心で計算する。
- **発光 `<名前>_e.png`**: 目・結晶・灯だけ。`sprite-normals.py emission --preset konoha|wolf|ogre`（色相の帯で自動に抜く。`--add/--remove` で手直し）。見本はこのは（斧の宝石・刃の結晶・真鍮）・狼（胸の襟毛と尾の先）・オーガ（鬣の金と手首の金具）だけ。
  - **白い毛・白い衣を発光マスクに入れない**（入れると白飛びし、ブルームで白が冷える。狼は強さを 0.9 に落として逃げている＝`art.enemy_wolf.emission`）。
  - 暖色の点は灯の点だけ。意味の無い光の玉を描かない（このはの大斧の頭の上の暖色の円は、二周目で宝石の青緑の灯に替えた）。

### 5-6 霧の帯の位置（二周目 2026-10-01・ユーザーの裁定「いちばん明るい霧の帯は本家と同じくキャラのすぐ上へ」）
- **画面でいちばん明るいのは、キャラの頭のすぐ上の霧の帯**（本家 夜の森 ot16 は主人公の足元から 328px 上・明るさ 152.5・半値幅 160px）。帯より上も下も暗くなる。二周目は足元から 329px・154（PC）、スマホは 268px・153。
- **帯は距離の霧で作る**（線形・奥ほど明るい。`fog.start` 14・`end` 28 にカメラの距離の比を掛ける＝PC とスマホで同じ所に来る）。霧の始まりを座席の奥に置くので、座席とキャラには霧がかからない。
- **霧の光の芯 `fog.lobe` は地平線の真ん中の奥**（t 32.5・s 80・y 2.2）。芯は縦と横で別の絞り（`powerV` 900・`powerH` 100。スマホ 1600）＝帯は縦に細く、横には広い。強さ 1.2。縦の絞りを強くすると上の真ん中とキャラの後ろが一緒に暗くなる（N6a と N8 が両立しない所）。
- **高さの霧（低い所ほど濃い）は地面のもやだけ**（上 1.5・濃さ 0.3・座席より奥）。高さの霧で帯を作ると、低いカメラでは頂点が頭の後ろ（行 ≈446）に来る（`honke/fog_newcam.py` の計算）。
- **上の真ん中は暗くしない**（本家は頂点の 0.5〜0.65 倍）。暗いのは両端だけ。上の真ん中は霧に透けた幹と枝の影絵で埋める。
- 測り方: `scripts/hd2d-r2-targets.py`（N1 帯の頂点の位置は主人公の足元からの相対・N2 太さ・N3 明るさ・N6a/N6b 上・N8 キャラの後ろ・N22 奥の端）。目標は `docs/design/hd2d-slice/r2-targets.json`。
- **三周目（2026-10-02）: 霧は3段**＝`fog.end` 40・距離の霧の2段目 `fog.far2`（24〜40・0.5。`_HD2DFog2`）・芯の強さ 1.8・芯の色を藤色 [0.56,0.59,0.68]。帯の木（霧 0.25〜0.6）・奥の木（0.6〜0.86）・最奥（0.86〜0.98）が3段に読める。**歯止め＝帯の頂点 145 以上・暗い画素 0.66 以下**（森を足すと帯が沈む。end 40／45 だけでは頂点 130／122 に落ちた）。加算の霧の面はやめ、α合成のノイズ入りの霧の板（`kind: mist`）2枚にした（霧の面の縁は真っ直ぐな横線に見えた）。
- **夜の色（三周目）**: 暗部は本家の中間の紺へ寄せる（`nightGrade` [0.20,0.29,0.41]・強さ 0.7・明るさ 0.05〜0.32 で効きが消える。明るさを保って色相と彩度だけ寄せる）。逆光と灯も青へ。灰青・ティール（HSV 197°）でなく紺（本家 210°）。測り方は `scripts/hd2d-r3-targets.py`。

---

## 6. 輪郭と白

### 6-1 輪郭
- **画面の上での輪郭の最暗色は 20〜30**（同じ HD-2D のトライアングルストラテジーで、キャラのドットを担当した森本さんが決めた RGB 24 を参考。提案書 ④）。
- 絵の輪郭は黒のままでよい（art-lint: 輪郭の最暗色の中央値 5・20〜30 に入る絵は 115 枚中 12 枚）。**絵は描き直さず、シェーダで持ち上げる**: `look_act1.char.json` の `outlineTarget`（27）から、後処理と霧を逆にたどって `_BlackLift` を絵ごとに解く（`StageUnits.BlackLiftForTarget`）。持ち上げの色味は紺（`blackLift`）。
- 輪郭がもともと明るい絵（ひなた・白の人形）は持ち上げない（自動）。
- 見本 W3b の画面での最暗色: このは 24.0・狼 21.1・オーガ 21.0・巻物 20.3〜21.9（合格）／太鼓 19.5・ひなた 37.4（外れ。絵ごとの目標を足すかは §9）。
- 後処理に新しい段（Shadows Midtones Highlights など）を足した時は、逆にたどる近似（`StageUnits.PostGrayDisplay`）にも同じ段を入れるか、`outlineTarget` を撮って合わせ直す。
- **縁 1px の中間色（三周目・裁定 Q2）**: 戦闘のキャラ（箱庭の `StageUnitLit`・PC の MSAA の時だけ）は、α が隣と違う輪郭の1画素だけ双線形の値を混ぜる（`look_act1.char.json` の `edgeSoft` 1.0・alpha-to-coverage）。ドットの中は 4px のまま。幕2/3 の `StageUnit` とスマホ（MSAA なし）は今のまま。外形だけで、絵の中の 4px の段は残る（敵の鋭い縁の割合 オーガ 0.44・本家 0.07＝10/7 の描き直し Q4）。

### 6-2 白
- ~~白の上限は入れない~~ → **二周目で入れた: `char.whiteCap` 0.7**（発光以外の出力の明るさ Y を、上限の 0.6 倍から上限へ漸近する柔らかい肩で縮める。色相は保つ・発光〔狼の白い毛〕には掛からない）。主人公の肌と顔がキーの光で飛んだため（上位5% 253・白飛び 9.4% → 上位5% 200・白飛び 0%。チャンネルごとの頭打ち 0.5 は体の中央値 99 まで沈んだので明るさで測る形に）。純白を灰色に寄せる描き直しはしない。
- 白を冷やさない（キーを少し暖かく・環境光の青を弱く・色の膜を打ち消す）。目安はキャラの白の b* +5〜+9（本家 +6.9）。
- 白を発光で飛ばさない（§5-5）。

### 6-3 そのほかのキャラの決まり（今までの規約のまま）
- **地面・台座・背景を描かない**（art-lint が「台座の疑い」を出す）。
- 敵は斜め左手前（3/4）。右向きに出たら左右反転して置き、`.pixellab.json` に `mirrored` を記録する。
- 規格の自動検査は `scripts/art-lint.py`（輪郭の最暗色・白 235 超の割合・光の向き・台座の疑い・左右反転・色表からの距離）。出力 `docs/pixellab/hd2d-act1/art-lint.{md,json}` は手で直さない。

---

## 7. ぼかしと後処理（絵の見え方に効く所）

- **ぼかしは座席の数字が読めることを先に守る**（奥の敵の意図と HP が読めること）。そのうえで本家並み（門⑥ ピントの外の鋭さ 0.1 以下）。
- ピントの帯は道に沿った帯（`look_act1.dof.json` の `pathBand` s −3〜5.5。二周目は −2.4〜4.5・奥の `lensFar` 3.0）。帯の中はぼかし 0。奥は上限 10px・手前は 20px（1080 の画面）。キャラの板は帯の中にいるのでぼけない。手前の舞う葉は、ぼかした絵として置く。
- 後処理: トーンマップ ACES・露出 0.3・ブルームの閾値 1.0（二周目は 1.3＝主人公の明るい所でにじみを出さない）・フィルムグレインといつも掛かる色収差はやめる・URP の周辺減光は弱く（0.15。キャラにも掛かるので、舞台の端は `stageVignette` で沈める）。
- 画面の紙（UI）は Overlay のままで、光もぼかしも受けない。舞台の上に常に出る札（敵の帳面・自分の札・上部バー・頭上の意図の札）は夜色の札、紙のまま残すのは手札・確認の窓・メニューだけ（裁定③）。

---

## 8. PixelLab の作法（HD-2D の舞台）

### 8-1 禁止語
**描写（text）にあると、送る前に止まる**（`scripts/pixellab-v2.mjs` の `FORBIDDEN_DEFAULT`。否定の文 negative には書いてよい）。

| 語 | 呼んでしまう物 |
|---|---|
| `forest`・`night`・`moonlight`・`scene` | 木・月・風景を描き足す |
| `moss green` | 土まで緑にする |
| `dirt` | 段丘や丸い塊を呼ぶ |

- 土は `packed earth`・`soil` と書く。**夜は描かせず「evenly lit neutral daylight」（中立の昼の明るさ）で頼み、夜はエンジンの光で作る。**
- 確かめる: `node scripts/pixellab-v2.mjs forbidden "<文>"`（禁止語があれば終了コード 3）。

### 8-2 手順
1. 雛形 `docs/pixellab/hd2d-act1/template.json` を写して発注書を作る（id・out・text。タイルと半立体の決まり文句 `_tileText`・`_reliefText` と否定の文 `_tileNegative`・`_reliefNegative` がある）。
2. `node scripts/pixellab-v2.mjs balance` で月の枠の残りを見る（v2 の `/balance`＝`subscription.generations`。v1 の balance は USD しか出さない＝枠ではない）。
3. `node scripts/pixellab-v2.mjs order <発注書> --dry` で禁止語と見込みの数を確かめる。
4. `node scripts/pixellab-v2.mjs order <発注書> --budget N --reserve 50` で作る。1つ作るごとに枠の残りを見て、予備を切るなら止まる。**HTTP 4xx（402 枠切れ・422 引数）で止まる**＝止まったら記録して、それ以上送らない。使った数は `<発注書>.log.jsonl` に残る。
5. タイルは `scripts/tile-calm.py` → `scripts/stage-palette.py map`、半立体は手直し（`docs/pixellab/hd2d-act1/relief-clean.py`）→ `stage-palette.py map`。
6. `scripts/art-lint.py` と確認シート（`docs/pixellab/hd2d-act1/sheets/`）で目で見る。
7. 使った数を `docs/pixellab-assets.md` の台帳に書く。

### 8-3 物ごとの頼み方
| 物 | 頼み方 | 使わない道（試した結果） |
|---|---|---|
| タイル（64×64・各4種） | pixflux で **160×160** を作り、中央 128 を 64×4 に切る（1生成で4種そろう）。天面（草・土・岩）は `low top-down`、縦の面（岩・樹皮・板）は `view: side`。中立の明るさで頼む | create-tiles-pro（1回 25 生成で 64px が16枚。右と下の縁に 1〜3px の案内線が残り、敷くとつながらない・色数3〜24の平らな絵） |
| 半立体（樹冠・額縁・羊歯・茂み） | pixflux・`view: side`・「正面の立面図（横スクロールの背景のように・天面も地面も台座も無し）」・背景なし・**面積 3万px 以下**（透明背景の上限）・各2シード | map-objects（1回 1 生成・背景は抜けるが否定の文が無く「幹なし」を守らず木を丸ごと描く）。どちらの道でも「幹なし」「地面なし」は守られない＝樹冠は丸ごとの木から切り出し、根元の土は消す |
| キャラの清書・塗り直し | edit-images-v2（1回 **20 生成**。同じ大きさなら 64px 16枚／80px 9枚／96px 4枚まで） | — |
| 向きを変える | generate-8-rotations-v2（1体 20〜25 生成。返りの8枚は画面で 正面→斜め右手前→右→斜め右奥→背中→斜め左奥→左→斜め左手前 の順） | edit の文で向きを変える（ごく軽くしか効かない） |

- **等間隔の粒・泡状の塊・星形の葉は捨てる**（ユーザー「集合体恐怖症を煽る」）。縄の輪も捨てる。
- 土・ひび・地面の小札・奥の高い幹はコードで作る（色表の色だけ）。

---

## 9. 未決（見本の判定でユーザーに確かめる）

| 項目 | 今の形 | 推奨 |
|---|---|---|
| 彩度の上限 | 入れていない | 入れない（§4-3） |
| 主人公の大きさ | **62 に決定**（2026-10-01 W5 の判定・48 は旗で残す）。48 は v2 を縮めた見本のまま | 48 で決まれば清書（edit-images-v2 で待機8・攻撃4・防御4＝約40 生成。PixelLab の月の枠が 10/7 に戻ってから） |
| キーの色 | 少し暖かい [1.0, 0.95, 0.88] | 本家の白に合わせて今のまま（中立に戻すなら `look_act1.char.json` の `key` を [0.92, 0.96, 1.0] へ。変種 `look=look_act1_char_w3` でキャラの光ごと W3 に戻して比べられる） |
| ~~このはの大斧の頭の上の暖色の円~~ | **二周目で閉じた**: 見本では斧の宝石の青緑の灯（`heroGem`）に替えた（待機以外のコマでは光の板を隠す） | — |
| 輪郭が目標から外れる絵 | 太鼓 19.5・ひなた 37.4 | 絵ごとの `outlineTarget` を足す（`look_act1.char.json` の `art`） |
| 奥の霧の明るさと①の読み方 | 霧の帯 137（本家 152）・① 61.7（ユーザーの目安 35〜55） | 霧を本家の明るさのまま（①は物差しの違い）か、霧の帯 110 で ① 55 か |
| ~~いちばん明るい帯の位置~~ | **決まった**（W5 の判定でユーザー「本家と同じくキャラのすぐ上へ」）。二周目で足元から 329px（本家 328）へ下げた（§5-6） | — |
| カメラ（二周目） | 22°・5°（スマホ 7°） | 変種 22°・6°／28°・5.5° と並べてユーザーに聞く（計画 §11-5） |
| スマホの粒（二周目） | 世界のドットがキャラより 1.3 倍粗い（寄せ 1.2） | 寄せ 1.0（座席の間隔が 17% 縮む）と撮り比べて聞く |

---

## 10. 検査の道具

| 道具 | 見る物 |
|---|---|
| `scripts/art-lint.py` | キャラの規格の表（輪郭・白・光の向き・台座・反転）・`_KeyFlip` の表・舞台の絵の色表からの距離 |
| `scripts/sprite-normals.py` | 法線・発光・確認シート |
| `scripts/tile-calm.py` | タイルの明暗と継ぎ目（ラプラシアン分散 800 以下・端の差 2 以下・継ぎ目の段差の比 1.6 以下） |
| `scripts/stage-palette.py` | 色表を作る・写す・距離 |
| `scripts/hero-downscale.py` | このは v2 を 48 の背丈へ縮める（見本用・清書ではない） |
| `scripts/hd2d-measure.py` | 門の①〜⑩（`docs/design/hd2d-slice/gates.json`） |
| `scripts/hd2d-honke-targets.py`・`scripts/hd2d-color.py` | 本家の色の目安（21項目・Oklab の8つ） |
| `scripts/hd2d-layout-check.py` | 札・額縁・キャラの重なり（三周目の L12 匣・L13 窓・L14 草の帯） |
| `scripts/hd2d-r3-targets.py` | 三周目の物差し（★目安・◆歯止め＝帯の頂点・p95・暗い画素・主人公の体。UI なしの画で舞台を測る） |
| `scripts/hd2d-r2-sheet.py r3-final` | 本家｜W5｜二周目｜三周目 × 5場面＋384px のひと目 |
| `docs/design/hd2d-slice/r2-layout-gen/place.py --r3` | 設計図の置き場の検査（窓・太い幹・意図の札・座席の通り・名前の帯・上の覆いの模型） |

撮影は `scripts/pshots.sh`（並列）か `scripts/unity-win.sh shots state`（1枚ずつ）。見本の場面は STATE に `hd2d=slice` を付ける。

---

## 11. 決めた経緯

- 2026-09-30 HD-2D 刷新の裁定（外注しない・舞台は全部3Dの箱庭・舞台の上の札は夜色・主人公は 62 と 48 を並べて選ぶ）→ 計画と提案書。
- 同日 W1: 25 テクセル/unit・タイル 64・色表 48色・art-lint・法線と発光・48 の縮めた見本（PixelLab 49 生成）。
- 同日 W2: 箱庭が立つ・光を受ける板・キャラの影1本（`charshadow`）・Linear。
- 同日20時 ユーザー「本家っぽく」（夜の森・座席だけ明るく端が沈む・ぼかしは本家並み・主人公 48 を既定・幹と根は半立体・札は夜色）・「本家の色彩も参考にしてほしい」（`honke-color.md`）・地面の模様を読める程度に戻す。
- 2026-10-01 W3b: キーの色を少し暖かく・輪郭の目標 27・舞台だけの周辺減光・座席の帯の土と草と小札・奥の高い幹。門は全場面 8/9（残りは座席の帯のむら）・本家の色の目安 16/21。
- 同日 W5 の判定（見劣り 3/5・主人公 62・霧の帯はキャラのすぐ上へ）→ 二周目: カメラ 22°・5°（スマホ 7°）・霧の帯は距離の霧＋縦に絞った芯・幹だけの霧の帯と左右の木立・コード生成の幹と枝と株・暗めの敵は暗い体・白の上限 0.7・芯のある接地影・門③⑥の読み方（ユーザーの回答）。門は全場面 8/9 以上・新しい物差し PC 22〜25/37（W5 5/37）・本家の色の目安 15/21（計画 §11）。
- 2026-10-01〜02 三周目（段0 の分析と裁定8件 → 段1〜直しの輪2。計画 §12）: 森を「1本の木＝幹＋枝＋房」の3段に・密な針葉樹の近い木で上を覆う・低い茂みの土手・α合成の霧の板・霧3段と紺の夜の色寄せ・照明3段と縁 1px と薄く広い接地影・UI の作り直し（足元の線 0.36・手札を沈める・足元の帳と左下の匣）。反証の見込み 3.1（直しの輪の前）・PC の GPU p95 6ms。S25 は未計測。


## Codex 改善版 2026-10-05（専用ブランチ）

今回のユーザー指定により、戦闘エフェクトは ChatGPT 画像生成を使用する。
キャラクター・背景の既存アセットを流用せず描き直す指示ではない。
`Art/fx/codex-vfx-atlas.png` は独自の斬撃・着弾・魔法・防御の透過4分割素材。
VFXは滑らかに縮小し、キャラのドットは保持する。画面全体の白い膜でなく、対象の近くで短く展開する。

幕1の月光は `materials.glow.shaftTexture` の強さ0.9・周波数18・流速0.015・密度下限0.18。
`shaftGain` は2.15（密度変調を掛ける前の値）。霧・灯の暈には適用しない。
木漏れ月光は `cookie.canopy=0.9` で起動時に既存の256pxクッキーへ焼き込む。
中央のコントラスト上限0.18は保つ。新しいライト・シャドウマップ・全画面パスは追加しない。
二周目・W5の完成版は強さ0を明記して比較用の見た目を保つ。

物理攻撃は0.13秒で踏み込みの頂点、敵の近接は0.11秒で着弾。
大きな単発／多段の最終打のみ55msの短いヒットストップ、敵は小さく後退して復帰。
カメラ移動禁止設定は旧ZoomPunchにも適用する。通常攻撃に毎回のカメラ揺れを足さない。
VFXは最大24クワッド（低画質12）、アトラスは最大1024px、CPU読み取りコピー・ミップなし。


## 演出重視の比較版（2026-10-05・クラウド編集、Unity視覚確認前）

`combatpresentation=baseline`（既定）はこの時点の現行演出を保持する。
`combatpresentation=showcase` は箱庭の5枚だけに溜め・踏み込み・着弾の局所光・粒子余韻を足す。
比較は同じリーダー、敵、手札、解像度で行い、戦闘の途中では旗を切り替えない。

| 札 | 最初の命中 | 次の波 | 粒子の余韻 |
|---|---:|---:|---:|
| 打撃 | 0.13秒 | — | 0.36秒 |
| 二連の蔦打ち | 0.16秒 | +0.12秒 | 0.44秒 |
| 水撃 | 0.24秒 | — | 0.48秒 |
| 大地の唸り | 0.30秒 | +0.15秒（実際に二波出る時） | 0.58秒 |
| 巨獣の踏みつけ | 0.34秒 | — | 0.65秒 |

値の正本は `CardVfxProfile`。表は通常の自ターン・副次誘発なしの目安で、実際の解決イベントを省略しない。
全体攻撃の同じ波は対象ごとに同時着弾する。キャラの構えは波数で保持し、敵の数で長くしない。
HP表示は命中まで保留するが、ルール状態・対象の合法性・セーブは即座に確定する。
新しい札の入力で前の札の命中を取り消さない。決着だけは残りの命中と0.65秒の余韻を見せて画面を移す。

新規画像・色表変更・world lightの弱体化・全画面の白い膜・追加のグローバルヒットストップはない。
既存の属性色・強い技の光・みぞれの待機コマを保持する。通常版のカメラ・光・テンポは変更しない。
クラウドではUnity import・コンパイル・実描画は未確認。`presentation-review` でA/Bの安全性とタイミングをPC上で検証してから見た目を判断する。

### 比較版の命中ウェイト（2026-10-06・「より重く」）

接触までの時刻は上表を維持し、接触したキャラのコマと戦闘移動だけを一瞬保持する。
打撃65ms、水撃75ms、巨獣の踏みつけ120ms。多段途中は25ms、二連の蔦打ちの最後は95ms、大地の唸りの最後は110ms。
大地が単発で終わる場合は110ms、比較対象の決着打は140ms。ガード・無効・回復は止めない。
命中フレームでのけぞり量の35%を入れ、保持後に通常8.5%／重い技15%の身長比まで動かして戻す。
一続きの停止は最大140ms、次の停止まで50msの復帰時間を置く。AOEの対象数で加算しない。
HP・属性光・粒子・カメラ・UIとカード入力は元の時計で進める。新しい札は防御札を含めて前の攻撃の動作所有権を取り直す。
比較版の決着も全体のtimeScaleを変更しない。reset／画面離脱で局所停止を捨て、消えた対象や同じキーの別キャラへ持ち越さない。
`presentation-review` は21ケース×2モード。停止中の次入力・防御入力・reset・画面離脱・対象消失と、実コマ／移動の静止を追加確認する。

Android候補は `BuildTools.BuildAndroidShowcase` で作る。ビルドローカルな `DECKROGUE_IMPACT_SHOWCASE` だけで候補APKの初期値をONにし、通常ビルドのbaseline既定と明示切替は保持する。恒久的なScripting Define Symbols設定は変更しない。

### 承認済みV2攻撃演出（2026-10-09・3技、Unity視覚確認前）

対象は comparison の `green_strike` / `blue_strike` / `green_finisher_stomp` のみ。未適用V1を置き換え、PCで受理済みの2f31050から直接導入する。
既存の属性共通画像や通常baselineは維持し、`attack-{slash,water,stomp}-{body,particles,emission}-v2-8f` と踏みつけ専用 `attack-stomp-dust-v2-8f` を使う。
全10画像は1024×512、4×2、256pxセル、straight alpha、中心pivot (128,128)。承認された二形状の原画とネイティブ変形・粒子から8時点を焼いたもので、8枚の新規描き起こしではない。

- 斬撃は細く熱い切り込み→開いた弧、水撃は淡い青白の扇→水滴、踏みつけは足元の低い放射圧→遅れた薄い土煙。本体と粒子/土煙は後処理後の通常alpha合成。body scalar gainは1.05/.94/.94、粒子/土煙は1。RGBの色相は変えず、V1の追加opacityカーブを掛けない。
- emissionは独立の正規化された余剰発光画像。sRGBをlinearへ復号し、alphaを1回だけ掛け、gain3.55/4.8/4.4で後処理/bloom前へ加算する。全面をぼかした白い板や、本体alphaから作ったhaloは足さない。
- HP命中時刻 .13/.24/.34秒と、キャラの局所停止65/75/120msは既存どおり。frame2は実際の命中callbackで同期する。画像のframe2保持は66.667/75/120msで、キャラ停止をもう一度足さない。全8コマは等間隔でなくproduction manifestの境界を使用する。
- 命中後の画像終端は333.333/390/490ms。疎な末尾へ急に切れないよう、frame6→7だけpremultiplied補間し、その後最後のセルをlinear fadeで0へ落とす。予備/命中/保持は補間しない。元の残留待ち .36/.48/.65秒は独立した寿命下限として維持し、追加の待ちや戦闘ロジック変更はしない。
- 技の光は画像/粒子の停止と独立して更新する。秒数は通常速度時の値で、既存のscaled deltaを維持し、決着FinishSlow中は他の場面演出と同様に伸長する。既存の強いcontact18/bounce28とheavy1.65倍を維持し、斬撃は暖白、水撃は淡青、踏みつけは暖灰白。hold後の指数減衰tauは斬撃46/46ms、水撃43/72ms、踏みつけ52/85ms（target/floor）。斬撃だけ承認previewに含まれるrelease phase16.667msを引き継ぐ。光は終端で必ず消し、pool再利用で他の技へ減衰設定を持ち越さない。
- 各技の全層はpool1枠を共有。reset・離脱・枠の再利用はversionで保護し、古いticketは新しいeffectを命中扱いにしない。missing texture/shaderやpost pass停止は既存fallbackへ戻す。全攻撃・回復・ガード等のresolved receipt経路を維持する。
- 全層は固定camera-facing billboard。quad/敵身長比は2.0/2.173913/2.565217。踏みつけは捕捉した足元＋world-up身長比.034783に置き、遠近は画像に焼き込み済みなので床面へ再回転しない。V1の50px持ち上げは廃止。既存の手前方向.28unitオフセットを保持する。床とのdepth、敵の隠れ、サイズ感はUnity実画面で確認する。
- 使用済みカードは3技だけ敵中央を経由せず捨て札へ短く退場し、命中前にoverlayを消す。ゲームの札移動/ルールは変更しない。
- 初回検証では新しい10atlasを全platformで非圧縮（約20MiB RGBA、mipmapなし）に保つ。既存elementのAndroid ASTC6×6は維持する。新画像の圧縮/端末性能はUnity/実機の別検証事項。
- カメラ、グローバルpost、他カード、敵turn完了gate、決着slow(.22)→消滅→残光→報酬、skip/reset、通常saveは変更しない。

クラウドのsource/hash/golden/タイミングモデル確認とUnity import/compile/実ゲーム描画は別。`presentation-review` 42ケースに全resource・合成材質・命中frame・全8コマ・独立光減衰・カード退場・足元位置の検査を含める。実行前の検査コード追加は合格報告ではなく、Android実機も未検証。

### 蔦斬撃 R1 の連続描画候補（2026-10-09・Stage13）

ユーザー承認の深緑の短い開弧を、`green_double_lash` の正確な card presentation metadata がある実HP命中だけに使用する。旧蔦8コマ・不採用の鞭・追加キャラpose/socketは使わない。登録は45/242カード（うち1枚は既存リーダー条件付き）、敵は1/167のまま。これは候補経路の登録数であり、Unityで全件見た目が合格した数ではない。

- 元契約は vine_slash_runtime_pack_r1 の contract SHA256 `117c2fce159ef2b5bda9233b1c161d091a04309d114cce895f8d19d0c8043e38`。静止RGBA素材2172×724を連続meshで動かす。1 receipt につき主面・追い線・芯・粒子の4batch、330頂点/402三角形。第二receiptはcamera-rightだけ反転し、UVは反転しない。120msは既存カードの2receipt間隔であり、rendererから第二hitを生成する時刻ではない。
- 実contactでage0を同期する。native予兆66.667msは、利用時間が短ければ負時間だけC1補正。先行receipt未解決等で予兆が間に合わなければcontact-onlyを明記する。予測contactを越えて未着弾のとき本体を止めて待たず隠す。実Land後の主面105ms/追い線135ms/芯80ms/最終粒子182msで終了。診断用333msや旧蔦440msを追加の不可視待ちにしない。
- actorの局所停止25/95msと既存contact160/280msはそのまま。body/粒子は局所停止中も進み、既存FinishSlowのscaled clockには従う。cancel/reset/再利用は世代を検査し、古いLandは無効。Stage12の入力queue/終了gateを維持する。
- Hは同じRecordBoxで記録した基準canvasのworld高に、基準spriteのalpha>40の可視行割合を掛けた値。後からattack画像が変わっても伸縮しない。読取不可・空画像は理由つき既存fallback。開始時の実camera姿勢をcaptureし、共通depth biasは既存impact位置のまま、Blenderの0.54worldをコピーしない。
- Texture RGBはsRGB samplerで一度linearへ、alphaはstraight。linear輝度をshadow/primary/edgeのrampへ変換、芯と粒子はcore色。palette JSONのHEXは一度だけlinearへ変換してVectorで渡す。HDR gainは主2.25/追い1.80/芯3.8/粒子2.2に既存のbounded intensity scalarを一度合成。premultiplied outputと `Blend One OneMinusSrcAlpha` の一回合成で、bodyの加算複製はない。
- 新shaderはURP forwardの透明物として通常postの前に描く。旧ElementAfterPostには登録せず、その停止で新meshを消さない。BlenderのAgX/FOG_GLOWとUnityのpost、Principled芯/粒子と今回のunlit近似は別物で、画質一致は未確認。
- Scene lightは色#8BA18Aと実contact同期波形を使い、同じ対象への新蔦だけmax合成。最初のpoint-light較正はtarget6.3/floor3.36を基準に既存の最終強度上限/全体capへ通す暫定値。Blender34/11Wからの換算値ではない。旧familyの加算則は変えない。
- 専用poolは低画質4/通常8 receipt、最大16/32 drawの計画上限。飽和時はserialで最古の残りを置換し、回数を記録する。全receiptが別々のframeで見える保証ではない。既存quad poolの無描画true問題は次の独立修正対象。
- PNGは元byteを保持し、exact-path importerでDefault/Bilinear/sRGB/非圧縮/no mip/no CPU read/max4096/NPOT無変換を固定する。GPUのmip0 RGBA8見積り6,290,112bytes（約6MiB）。meshやdraw上限は数値上の計画で、Android GC/CPU/GPU/overdrawは未計測。既存画像とGUIDは不変。

クラウドの数式・source・stub検証と、Unityのimport/compile/GPU/見た目は区別する。nativeで色・線幅・depth・接触と実HP・連打・消滅/報酬・skip/reset・低画質pool・Android負荷を確認するまで、全体完成とは扱わない。

### Pool の無描画成功・取消安全の修正候補（2026-10-09・Stage14）

新しい技形の追加ではなく、既存の描画受付と寿命の修正。新素材登録は45/242カード・1/167敵のまま。PC/Unity/Androidでは未検証。

- 旧UI atlasの12/24枠が満杯でも、新しい受付を黙って捨ててtrueにしない。単調serialで最古の枠を置換する。同一frameのworld poolも時刻の同値ではなくserialで順番を決める。飽和時には古い演出が実描画前に置換され得るので、全receiptが別々に見えた保証とはしない。
- Slash/Spellは本体・芯の両Emitを必ず呼び、実際の受付結果をORする。無効なlayer/Canvas/camera、取消済みのscopeにはfalseを返す。受付counterとGPUで見えた画素は別。
- UI poolは取消時に即座に無効化し、正確に所有する子だけをreset/rebuild時のDestroyから保護する。非poolの浮き文字等は従来どおり消す。古い完了/取消callbackはversionで新しい画像を隠せない。終了済み/取消済みscopeへ新しい描画を予約せず、新しい明示scopeは利用できる。
- 共通Tweenの省略可能alive条件は、新しいpool利用箇所だけで世代切替を検出し、古い無操作仕事を次tickで解放する。既存呼出しは既定のまま。scopeを先に取消してからpoolを破棄し、取消が通常完了callbackを呼ぶことを避ける。同frameに大量受付した場合の一時的なcoroutine/closure数は硬い上限ではなく、CPU/GC実測が必要。
- 最後のresolved UI ringだけを、atlasが無くても使える別の12/24枠へ変更する。元の0.22秒・OutQuad・scale0.3→1.3・alpha1−k²を保持する。旧atlas poolと別枠なので、最大は低画質12+12/通常24+24画像。一般のTween.Sparks/RingBurst/浮き文字まで全経路pool化したものではない。
- World/vineは停止中のcameraや取消済みscopeで新しい受付/着弾を成功扱いにせず、既存fallbackを試す。fallback側も利用不能なら理由を記録し、無条件に表示成功を数えない。HP receipt/guard/nullify/costの分類とcommit、ゲーム結果/RNGは変更しない。

クラウドのactual-source+明示stubは12/24枠、複数Emit、満杯連打、取消、deferred Destroy、再利用、最古順、guard/HP receipt保持を確認する。Unity実体のDestroy境界・Canvas/Shader/GPU・画面ROI・低画質切替・連打負荷・終了/報酬/skipは別途実機で検証する。

画質を戦闘中にHigh→Lowへ変える場合、UIは次の利用可能な受付で上位の使用枠を止める。World/Vineは新しい取得範囲だけを絞り、既に動いているHigh側の余韻は通常終了まで残る。したがって切替直後の全体に12/4枠を即時強制したとは扱わず、全体上限はWorld24/Vine8のまま記録する。


### Stage15 continuous physical contact R1 candidate (2026-10-09)

- Two explicit opt-ins only: enemy_brute/club (blunt) and enemy_brood_raptor/claw (three scratch ribbons, one actual receipt). All other enemy IDs retain declared routes. Conditional unknown branches stay fallback. Registration is not Unity visual acceptance.
- Existing immediate melee receipt owns contact0. Initial claw integration is contact-only; its native66.666667ms anticipation is not appended to gameplay. Positive-time body/core/particles retain contract timing: claw115/40/172ms; blunt49ms accent (first12ms held), continuous pressure165ms/debris222ms. No synthetic hits or HP split.
- Ordinary pooled dynamic meshes, at most3 draws per active receipt; shared low4/high8 pool (at most24 active draws). Lazy blunt/claw sets yield at most48 resident meshes after both-family warmup. Prior high-quality tails may finish after downgrade. Buffers are reused; native CPU/GC and GPU cost are unmeasured.
- Stable captured base-visible targetH and camera basis; common contact depth bias. Source+Z becomes camera-toward. Body keeps advancing during actor pause. Target identity gates attached light, not captured-position fallback.
- Separate physical palette: shadow#535E69 / primary#C6C6BE / edge#DEDCD1 / core#F2EEE4. Scene light#C6C6BE separately. sRGB source decode once, linear palette once, HDR gain once and premultiplied coverage once. No additive duplicate. Point-light target6.3/floor3.36 pre-scalar provisional maxima reuse bounded max-combine policy; actual exposure is uncalibrated.
- Shared2172×724 slash texture retains its existing GUID. New unchanged1774×887 blunt material uses only the first contact image; no second-image swap. Uncompressed RGBA8/mip0 newraw6,294,152bytes (~6.003MiB), no CPU-readable copy. Android compression/downsampling awaits visual QA, so this is a candid provisional memory cost.
- Freeze requires reference numerical, source lifecycle and full regressions. Unity import/HLSL/GPU/sort/motion/HP/finish/cancel/device exposure and frame-time QA remain mandatory. Old rejected vine/whip/water designs are not restored.


### Stage16 continuous fang and neutral beam R1 candidate (2026-10-09)

- Explicit IDs only: enemy_elite_devourer/bite (physical fang, no absorption) and enemy_elite_sentry/beam (shape known, element unspecified). Beam retains declared neutral display fallback, not a new game damage attribute. Each has one real receipt.
- Fang begins at actual melee contact0, omitting native90ms anticipation rather than delaying HP. Beam uses native80ms anticipation backwards from existing220ms contact; shader/body clocks keep moving independently of actor pause. Actual Land owns0, expired preview hides instead of freezing at negative epsilon; late contact uses contact-only/catch-up and no fabricated HP. Fang ends140ms, beam160ms.
- Beam source and target are captured world points via existing board-center fallback, never guessed eye/weapon sockets. Stable target-visibleH sets width; source-target span is not multiplied by damage size. Body center follows actual span, transverse width follows projected-ray perpendicular; endpoint/particles rotate from native direction into real ray direction. This changes the native camera-up width by approximately3.72degrees in the fixed reference view. Fixed golden parity and general world adapter QA are separate.
- Procedural beam field is the specified three-sin function, no image/noise dependency. Target UVu=1 stays fixed; longitudinal density is per projected world-H distance, never rescaled to shrinking head/tail. Invalid/missing/coincident/projected-zero source omits ray and retains legitimate local endpoint, with explicit audit reason.
- The same sharedlow4/high8 receipt pool now has four lazy3-draw sets per slot, at most24 active/96 resident meshes after all-family warmup. Existing high tails may survive low downgrade. Dynamic beam bounds/UV updates, alpha sorting and CPU/GPU cost require Unity QA.
- Fang reuses the physical palette and straight-alpha shader path. One unchanged1942×809 RGBA8 row is added (~5.993MiB mip0, no CPU copy), used by two rigid jaws, not switched images. Beam uses separate neutral-grey roles and no texture. Single premultiplied coverage and bounded contact light contribution remain; no additive duplicate or guard meaning is implied. Existing metadata/GUIDs remain unchanged.
- No engine/result/RNG or actual damage-receipt count change. Native Unity/Android/contact/finish/cancel/preview and exposure validation remain pending.

### Stage17: receipt-proven guard / nullify

- Exact zero-HP shield-resource loss selects the small upright continuous shield. Exact unchanged HP/Block/IceBlock selects incoming-side dissipation. Blocked payload alone cannot prove resource consumption; missing/invalid metadata or resource increases retain an audited legacy fallback. Positive HP loss remains the existing hit path.
- P0, guard180ms/nullify212ms, no inserted wait or actor pause. Each receipt starts once at its existing contact. Nullify creates no light, hurt flash, guard sound or block pose. Guard's target-only light is provisional bounded max-combined (target multiplier.10 × native maximum envelope.33); no floor light and no second old Guard light after successful new rendering.
- Frozen source board center and stable receiver-visible H determine incoming direction. Shield stays upright and mirrors for source-right; nullify rotates its local source-to-target axis. Invalid/projected-zero direction explicitly uses camera-left incoming. No weapon socket is inferred. Reset/generation ownership and shared low4/high8 recycling mirror the existing receipt-pool contract; at most24 active draws and40 lazy resident meshes for this separate system pool.
- No textures. Linear palette roles, per-part gain and alpha are applied once, premultiplied blend. CPU formula parity and lifecycle stubs are not Unity shader/render proof. Depth, half-size visibility, exposure and Android mesh/pool cost require native QA.
- Legacy Canvas has meaning-correct explicit fallback only: existing guard atlas if available, neutral bounded ring for nullify (or last guard fallback). It is not the R1 continuous shape. The remaining Canvas quality gap is tracked. Nullify is intentionally silent until a distinct neutral sound is authored; attack swing cues stay. Suppressing misleading hit/block sounds changes cosmetic Unity random draws from Audio, not engine RNG or combat results.

### Stage18: positive reflection / BurnTick receipt responses

- Exact positive ThornsReflected HP loss selects a small physical thorn contact at player, with a short residue toward the captured reflecting enemy. Exact positive BurnTick selects small target-local fire tongues/embers at EnemyIndex. Valid metadata and matching HP delta are mandatory; missing/inconsistent metadata, resource increase, or Burn shield change retain an explicit old fallback. No originating card/element is inferred beyond the named Burn event. ScaldTick, HP cost and card reactions are not registered by this increment.
- One aggregate receipt remains one response. Reflect140ms / Burn tongues200ms + embers238ms from actual contact0; P0, no new pause or explicit wait. Raw overkill and later DeathSaved stay distinct. Fixed restrained system strength uses authored part count/gain, not attack heavy escalation. Actual positive injury cues already owned by Presenter remain; old extra world hit lighting is suppressed after new system success.
- The existing system pool is shared across guard/nullify/reflect/Burn: low4/high8 allocation,8globalcontacts,24maximum active draws and80 resident lazy meshes when all four families have warmed. No new textures. Direction failure omits only reflection residue; local legitimate contact stays. Burn stays upright in captured camera space. Captured base-visible H and target/generation-safe light ownership are retained.
- Physical/fire palette roles decode once, per-part gain/coverage premultiply once. Scene light color is separately verified as each palette's primary (scene_light_color_r1 supplement), no surface-core tint inference or double multiplication. One target-only provisional light uses multiplier.10 × authored max envelope.12/.14; no floor and no nullify light. Native exposure/depth/mobile visibility remain unverified.
- Frozen reflection/Burn pack's optional +50ms debug PNG/log are an older width trial; use final +75ms/continuous sequence/primary MP4 as art reference. Contract, geometry, shader and goldens are final. Legacy Canvas remains explicit fallback quality, not this continuous shape.


### Stage19: local HP loss and hand-affliction responses (2026-10-09)
- HpLost stays own-resource depletion; ScaldTick stays a neutral mixed hand-affliction. Exact positive receipt HP delta and unchanged Block/Ice/MaxHP required; no inferred attacker/element or attack-heavy scale. Unknown metadata retains explicit local neutral fallback.
- P0, selfloss182ms / affliction200ms, no new light/hitstop/launch wait. Existing numerical receipt and DeathSaved remain separate. Zero/negative synthetic losses acknowledge without shape, pain cue or HP−0 label.
- Texture-free: selfloss40 vertices/20triangles/1draw; affliction146/124/2draws. Per-thread phase goes UV1, coverage stays per-fragment at receipt age. Cull Off preserves opposite face winding. Six-family system pool low4/high8, max24 active draws/104 lazy resident meshes; actual Android costs unmeasured.
- Positive Scald's existing audio/flash and source labels remain; new shape adds none. Native Unity color/readability/GPU/contact/reset validation pending.


### Stage20: physical pierce / meteor candidate (2026-10-09)
- Exact card metadata only: green_horn_thrust→pierce_contact, red_meteor→physical_meteor. Meteor remains physical, its9fragments are one receipt; no fire or extra damage metadata. Old literal slash fallback style corrected to blunt for the meteor.
- Native anticipation130ms fits existing first contact. True Land owns contact0/HP; C1 negative compression only if start is late. Predicted expiry hides the unlanded preview rather than holding body atcontact; stale callbacks use captured-position contact fallback. New residues178/270ms join existing finish/reset ownership with no added actor pause.
- Physical palette/linear texture decoding and premultiplied coverage once. Rock front/depth remain one rigid object beforecontact, then separate. Shared concentrated blunt uses identical old texture and mathematical shape, x/y1.32,z+.040.
- Separate bounded pool low4/high8, max32active draws/56resident lazy meshes; existing intensity Size/Emission/Particles/Light applied once to this standard family, authored1.32meteor contact is intrinsic rather than a tier variant. Target/floor light .35/.12 provisional max-combined physical group, no prelight. Native Unity/Android exposure/render/memory not measured.

### Stage21 cloud candidate: shared physical slash authored tiers

The existing `slash` physical route (15 explicit profiles plus green_strike's showcase) now has three continuous authored shapes selected from the receipt-frozen DamageVfxIntensity tier. No registry expansion or damage-rule change. Small is one short arc/3 sparks, medium the reviewed main arc/trailing stroke/6 sparks, large a wider arc/secondary stroke/9 sparks. The secondary stroke belongs to one receipt. Native preparation45/66.666667/130ms is scheduled backward from the existing true contact; tails140/252/320ms are live presentation work, not an extra delay per target. Actor hitstop and HP timing remain unchanged. Existing `small` semantic presentation caps selection at small; size/count/emission/light are authored and do not receive the old tier scalars again.

Each effect uses captured base-visible H and camera basis. It shares the byte-identical2172×724 slash texture and physical palette already imported, with no additional PNG memory. Body material uses source RGB decoded once; linear physical core accents use a premultiplied URP unlit approximation. This omits Blender Principled/specular, AgX and compositor behavior. Maximum4 draws/receipt, low4/high8 pool,44/88 lazily retained meshes across3 tiers. Warm pure geometry allocation is0 over20,000 samples/tier; native renderer/GPU allocations remain unmeasured.

Lights begin only on actual Land. Provisional medium target/floor multipliers.35/.12 use small source ratios28/48 and8/20, large60/48 and24/20, with same-target maximum combination. No negative prelight slot ownership and no old slash light when the new renderer succeeds. Begin or Land failure falls back to the existing standard slash and its own light, then generic/Canvas receipt routes. No Blender-watt equivalence or device exposure approval is claimed.

Review must cover all tier boundaries/overkill/ratio/semantic cap, shader premultiply and linear inputs, property-upload failure, pool recycling, cancel, numeric dependency deadline, target replacement, terminal longest-tail handling, and transparent overlap. Native Unity import/GPU/Android frame time, exposure, motion readability, audio and final contact alignment are still pending. Other families retain their current scalar policy and standard-art fallback until their own authored variants are reviewed.

### Stage22 cloud candidate: continuous waterA shared route

The existing5 water routes (blue_strike showcase, blue_tide_drop, blue_rapid_strike, blue_catalyst_echo, fusion_tide_stance) now use the retained waterA continuous stream. Registry/meaning stays unchanged; no rejected water ring/vortex is reintroduced. Exact card-metadata restrictions and the two semantic-small profiles remain. Fusion nonattack mode, reactive/necro sources, guard/nullify and zeroHP do not create an attack receipt.

Native160ms preparation fits before blue_strike's unchanged240ms contact. Other normal130ms contacts use the frozen negative-time C1 shortening, with no HP wait added. Body150ms/core90ms/droplets354ms have independent visibility; diagnostic620ms is not an action wait. Captured base-visible H/camera and caster side determine the initial local stream/mirror. Missing side remains audited camera-right flow. Actor hitstop does not freeze the body.

One continuous standard shape receives the old bounded Size/Emission/Particles/Light scalars once, unlike the already-authored three slash variants. UV flow is written once by geometry. Body texture and256×128 droplet lookup decode sRGB once, filter in linear space, premultiply alpha once; lookup alpha is ignored, body outsideUV is clipped. Core and scene colors are separate. Blender Principled/specular/AgX/glare remain an explicit unlit approximation. Scene lights begin only at actual Land, provisional target/floor.35/.12 with same-target maximum. No preview light slot and no old duplicate water light on success; failed Begin/Land falls back to standard water then generic/Canvas.

Two new original textures total6,421,184 theoretical RGBA8 bytes without mips (~6.124MiB), not measured native memory. Exact import exceptions preserve original dimensions, bilinear sRGB/straight alpha/no CPU read/no mip/uncompressed baseline. Low4/high8 pool owns16/32 lazy meshes, maximum32draws,382vertices/458triangles per receipt. Pure geometry warm sampling allocates0bytes/20,000calls. GPU uploads, mobile exposure/overdraw/frame time and final material readability remain native QA tasks.

The depth formula in the frozen contract prose was corrected by a separate supplement to match actual motion/golden/video: center bulge .014*sin(13u−6t)*(1−side²). No source art or frozenpack was changed. Native Unity import/full compile/HLSL/device appearance and all-attack completion remain unverified.

### Stage23 cloud candidate: continuous fire projectile and local discharge

Three existing fire routes (red_strike, red_spreading_fire, red_double_tap) share the continuous projectile. One new exact-card opt-in, red_burst_release, uses target-local discharge only. BurnDischarged consumes status and is not an additional hit; only its subsequent actual positive DamageDealt receipt starts this effect. Burn0, noncard/legacy provenance, full guard/nullify, later thorns and death-triggered damage keep their own meanings and receipts. Registered damaging cards are48/242 (one leader-conditional), enemies5/167; registration is not Unity visual acceptance.

Projectile native130ms preparation fits the existing130ms contact; local P0 suppresses anticipation but still begins at that existing real contact. True Land owns HP and light. Projectile head continues23ms past contact; body/core/embers independently finish within276ms, local within284ms, core57ms and light115ms. These tails join existing scoped work rather than adding target-count waits or actor pauses. Reset and stale generation callbacks remain guarded.

Actual captured source/target centers are projected into the captured camera basis, divided by composed visibleH×Size so the single root scale does not alter endpoints. The near-source flight cutoff is .05 of composedH×Size, an explicit difference from the asset's base-H prose. Missing/invalid/coincident source suppresses flight and preborn delivery embers only, retaining local impact with a reason. No eye/weapon socket is inferred. Authored front-face depth remains separate from centerline endpoint placement.

One original1774×887 straight-alpha texture adds6,294,152 theoretical RGBA8 bytes (~6.003MiB, no mip or CPU read). Bilinear sRGB input decodes once; luminance maps once to linear fire roles, authored gain and captured intensity apply once, and coverage premultiplies once. Local/flight surfaces share material without switching peak/trail images. Blender AgX/glare/lighting are not reproduced by this unlit shader. Low4/high8 pool has52/104 lazy resident meshes across both modes and at most56 active draws. Geometry535/568 vertices and680 triangles/mode,16visual embers. Android memory/CPU/GPU/overdraw remain unmeasured.

Scene light uses independent #DFAA78 and actual-Land-only provisional target/floor.35/.12, local source ratios62/55 and22/20, with max combination and captured Light scalar once. No negative prelight slot and no duplicate old fire light after success. Failed projectile Begin/Land falls back to standard fire then generic/Canvas; failed local discharge goes directly to local element/shared fallback and never invents a projectile. Exposure, transparent overlap, half-size visibility, audio and native contact/finish/cancel still require Unity/device QA.

### Stage24 cloud candidate: continuous dark-drain shared family

Eight existing onPlay dark_drain routes share two short inward surfaces, a local core,8intake motes and12decorative return motes. Source registrations are unchanged. black_corpse_collect retains its small semantic scale, and all existing exact-card metadata gates remain. General dark slashes and reactive/necro/self-exhaust paths do not inherit this art. Real HpHealed stays independent: the engine can heal despite full guard or zero actual damage, cap can emit0, and thorns/death triggers may occur between damage and recovery. Return motion neither infers the amount nor owns recovery timing.

Native130ms anticipation fits unchanged130ms contact; only unavailable negative time is C1 compressed. Actual Land owns contact-relative age zero; pre-contact inward/intake motion is decorative, while core and return begin at contact. Two main paths keep advancing through actor pause and lose shader coverage by91ms, core62ms/intake79ms. Valid source return ends301ms; invalid/missing/coincident capture omits return and ends local work104ms. Capture uses true source/target centers and camera basis, divided by composed base-visibleH×Size so root scaling preserves world endpoints. The .05composedH×Size near-source cutoff is a declared adapter difference from base-H prose. No socket or heal target is guessed. Numeric readiness, captured target identity, generation-safe cancellation and terminal work ownership remain.

Four fixed dynamic batches total419vertices/560triangles. Per-vertexCoverage is passed in UV1 and multiplied once in the fragment shader; UVflow already exists in geometry. Source texture sRGB decodes once, linear luminance maps once to dark palette, bounded receipt Size/Emission/Particles/Light apply once, and final alpha premultiplies once. Independent scene color#9383A8 is not surface-core tint. Provisional contact-only lights use target.35×38/55 andfloor.12×13/20 relative to fire's initial bounded baseline, same-target max; no prelight/return light slots or old duplicate light. Blender watts, AgX/glow and native exposure are not matched measurements.

One unchanged1774×887 original adds6,294,152 theoretical RGBA8/mip0 bytes (~6.003MiB), with bilinear/sRGB/straightalpha/clamp/no mip/no CPU read/uncompressed import exception. Low4/high8 separate pool owns16/32lazy meshes and at most32active draws. Warm pure ConfigureSource+Sample allocates0bytes/20,000pairs; native allocation/Android memory/overdraw/frame time remain unmeasured. Begin/Land failure tries standard dark_drain once, then generic/world/Canvas with explicit reason. Native Unity rendering/contact/audio/finish/cancel/visibility quality and all-attack completion remain unverified.

### Stage25 cloud candidate: continuous positive recovery

Existing positive HpHealed, EnemyHealed and RegenTicked receipts now share continuous recovery motes, thin tails and a local aura. Amount<=0, HP costs, mixed affliction and attack light remain excluded. Recovery belongs to the actual recipient rather than source/healer. No card/attack registration, engine data, event count, RNG, HP time or actor stop changes. Damage tiers are not applied. Existing small recovery semantic (Amount1–4) keeps .72 size/.75 emission/.65 light once; authored16 motes and8 tails remain visual parts of one receipt.

P0 starts at real recovery commit, including the existing enemy recovery delivery time. Camera basis and base-visible height are captured. The new origin is captured feet+camera-up×.4652173913H, independently of root semantic scale; this deliberately differs from the old LayoutRotation/transparent-board-height approximation. Old fallback position remains unchanged. Aura205ms/light240ms/last mote541ms define real work; old590ms is not retained as a wait. Reset, target identity, generation, longest-tail and numeric readiness use existing ownership.

Three dynamic batches total193vertices/168triangles. A fixed1774×887 texture adds6,294,152 theoretical RGBA8 bytes without mip/CPU read; bilinear sRGB decoding, linear palette mapping, per-vertex coverage and final alpha premultiply each occur once. This unlit material omits Blender Principled, AgX and glare. Low4/high8 pool owns12/24 lazy meshes, max24 draws. Pure warm Sample/surface allocate0bytes over20,000 iterations; native GPU/Android allocation and overdraw remain unmeasured.

The soft effect is fully transparent at age0. Actual Land still validates all three invisible mesh/property uploads before reporting success, so an initial upload failure can reach old authored-heal fallback at the same contact. This does not promise recovery from arbitrary later GPU failure. Begin/Land failure tries old authored heal then element/Canvas; successful new or old authored heal suppresses the duplicate Cast light. Independent scene color#ACC5B1 receives provisional actual-Land-only max-combined target.35×28/55 and floor.12×10/20; visible light is zero at0, rises, and reaches zero again at240ms; contribution ownership remains generation-guarded until final541ms retirement. Unity exposure, origin alignment, transparency sorting, small-target readability, audio/contact/finish and device performance remain unverified.


### Stage26 light continuous rebuild R2 (cloud candidate)
Seven already registered light attacks now use a camera-local continuous passage with four mesh batches, not a caster beam. Contact and actual HP receipts remain unchanged; native anticipation100ms fits the normal130ms contact. Negative late-start time alone is C1-compressed according to the R2 clock. Main85ms/core72ms/airy183ms/glints200ms; actor stop is independent. Guard/nullify keep separate responses.

Straight sRGB source is decoded once, remapped in linear color, HDR gain once, and coverage premultiplied once. Scene light #DED5BA is a distinct input. Provisional bounded target .35×60/55 and floor .12×20/20, actual-Land-only max combination; Unity exposure remains uncalibrated. All four Land uploads are tested even when a batch is invisible. Missing/unsupported resources fall back once to the prior light sheet, then existing element/Canvas paths.

The unsaved original Stage26 was lost during cloud reversion. This R2 is rebuilt from the identical recovered motion source and original PNG, plus a newly reconstructed contract; it is not a byte-identical restoration of the lost archive. Stable base-visible target H/captured camera basis are used. Original1774×887 RGBA8 costs6,294,152 nominal GPU bytes without mips; Android compression/residency and actual GPU appearance/performance are unverified. No change to engine/RNG or7existingroute membership.


### Stage27 Ice crystal rebuild R2 (cloud candidate)
The existing exact blue_ice_lance route now forms one unchanged left-panel crystal surface, holds its rigid shape to55ms, then continuously separates nine irregular radial regions. No peak/trail picture switch. P130 stays inside existing130ms actual contact. Shader reveal is per fragment from UV1.front; vertex alpha remains independent. Body ends305ms, core79ms, fourteen chips261ms. Zero HP loss/IceBlock0 use existing guard/nullify. No shield resources, hits, actor stops, source membership or engine rules change.

Three mesh batches109vertices62triangles; base-visible targetH/captured camera basis, local target contact. Static sRGB PNG1774×887 is6,294,152 nominal RGBA8 bytes without mips. Linear luminance^1.35 clamps before palette ramp, one final premultiply; no refraction or thick-glass claim. Scene#A2C5D2 is separate. Provisional actual-Land-only max light .35×42/55 and .12×16/20 comes from new R2 reference, not restored R1 watts. Native Unity depth/transparency/exposure/Android memory and quality are unverified. Failure falls back once to the existing ice sheet, then element/Canvas. All3 Land uploads are validated even if a batch is initially invisible.

### Stage28 actual-ground Earth/Stomp R2 cloud candidate (2026-10-09)

- Existing `green_earth_roar`, `green_finisher_stomp`, and earth-typed `green_sig_trample` registrations use a certified actual-terrain capture for the new floor mode. This is conditional display coverage, not proof of new-mode success at every battlefield location. Unknown competing meshes, gaps, invalid normals/heights, unsupported births or ownership changes suppress floor, particles and new light together; the existing local impact remains once. No flat surrogate or old ground sheet is substituted as certified terrain.
- Original `FullStageGround.Height` behavior is preserved separately. Known Batch02 resource identity plus geometry SHA, immutable generation-owned capture, full-domain seam checks and 4,096v/2,048tri cap guard the new mode. Size applies before capture; final world floor vertices never rescale. Future in-place stage geometry writers must invalidate the provider first. No per-frame terrain sampling is added.
- Earth P140 fits its existing contact; ground420ms/chips347ms. Stomp P0 at current contact; irregular pressure180ms/dust267ms/pebbles232ms. R2 reconstructs new fields/trajectories after original R1 source loss. The exact current HP contacts and65/75/120ms heavy actor pauses stay unchanged. Multi-target actual receipts retain their own clocks; geometric waves are not new hits.
- Earth2/Stomp3 draws maximum,8 slots (Low4), fixed8 earth chips /4 stomp stones+8 dust. One1774×887 grain PNG adds6,294,152 nominal uncompressed RGBA8 no-mip bytes; native residency/performance is unmeasured. Capture allocations and terrain checks are bounded but not zero allocation.
- Linear role vectors and hardware sRGB texture decoding occur once. Grain RGB repeats with no alpha dilation. Ground/dust are premultiplied unlit; tetrahedral chips are roughness.86 URP-lit facets with separate edge emission. Earth scene#B3A08B and Stomp scene#C6C5BC are separate from surface core. Initial point-light calibration is bounded, actual-contact-only, max-composed and uncalibrated on device.
- Both shaders live in Resources/Shaders. Cloud math/source/stub/golden checks are distinct from Unity shader compilation/stripping, actual forest depth, small-line visibility, transparent sorting, HDR exposure, Android frame time and GPU memory. Those remain required QA. Canvas stays an explicit legacy semantic fallback.


### Stage29 continuous Lightning R2 and finite dust shader correction (cloud candidate)
The existing exact blue_aether_burst+leader_izzet and enemy_thunder_globe/thunder_strike routes now use a local continuous discharge. The decorative leader is target-local and does not claim a weapon/eye socket or world-spanning ray. Source registrations, existing ranged transport/contact scheduling and actual HP receipts are unchanged. Ordinary enemy hits3 still produces one aggregate DamageDealt and one playback. Guard/nullify remain separate. Native P100 fits existing contact; body110ms/core60ms/twelve sparks200ms, actual-Land-only light75ms. No added actor hitstop or periodic reflashes; flash-safety certification remains unverified.

Three dynamic batches total65 vertices38 triangles, Low4/high8 slots with12/24 lazy meshes and at most24 active draws. Captured base-visible H/camera basis, one bounded Size/Emission/Particles/Light composition, per-fragment reveal on a single fixed left-panel image, and independent core/body clocks preserve the R2 contract. All three Land uploads are validated even when invisible; failure tries the prior lightning sheet once before generic/Canvas fallback. Cancel/reset and oldest-slot retirement invalidate stale contacts. New-mode success suppresses duplicate legacy light.

The original1774×887 sRGB/straight-alpha texture adds6,294,152 nominal RGBA8 no-mip bytes. Palette maps in linear RGB, HDR gain occurs once and output is premultiplied once. Scene#B6B0D0 is separate from surface core. Provisional target .35×42/55 and floor .12×14/20 use actual-contact-only max composition, not matched Blender watts. R2 rebuilt pivot/scale/shader coefficients after original pack loss; original PNG matches. Unity shader compile/stripping, exposure, transparency, continuous motion/readability, flash accessibility and Android residency/frame time are pending.

A separate Stage28 dust safety correction replaces pow(sin(x),2) with an explicit sn*sn square. HLSL pow may produce NaN for a negative base, unlike the C++ pow used by prior arithmetic fixtures. The normative appearance and coefficients are unchanged; negative-sin finite-input fixtures and a narrow audit accompany this increment. Frozen Stage28 assets/archive remain historical and unchanged; use this correction before native QA. CPU/source checks do not establish GPU execution.


### Stage30 small scatter profiles R2 (cloud candidate)
Four new exact-card opt-ins cover red_spark, white_spark_scatter, blue_ice_hail and green_catalyst_spore with fire fragments, warm sparks, hard ice chips and drifting botanical spores. Existing element/style labels remain display classifications. Token addition, IceBlock gain, noncard provenance, old metadata-free payloads, guard/nullify and fusion-catalyst declarations do not create new hits. Each actual positive HP-loss DamageDealt owns one P0 response at its existing contact. White's two damage effects and real AoE target counts remain engine-owned.

No texture/importer addition. Two analytic mesh batches,54vertices/28triangles (ice46/24),12particles maximum (ice10), eight global slots/Low admissionfour. Per-vertex phase and gain survive batching. Fixed-prefix thinning applies bounded Particles only when below1; standard/large do not exceed the authored count. Size/Emission/Light compose once, no extra Alpha field or particle brightness multiplier. Cached profiles may retain64lazy meshes across eight slots but only16draws active; native memory/performance remains unmeasured.

P0 skips preview allocation; actual Land validates both mesh/property uploads, even when invisible. Core50ms, light70ms, profile tails205/190/219/260ms. Bodies keep moving during core/actor hold. Existing scope/cancel/reset/oldest-generation ownership remains. New-mode failure uses existing element/Canvas fallback once; no nonexistent legacy scatter atlas is claimed. Successful new rendering suppresses duplicate old hit light.

Palette sRGB decodes once; analytic coverage and authored gain produce one premultiplied HDR output. Scene colors are separate. Provisional target .35×32/55 and floor .12×8/20 are multiplied once by the native envelope already containing profile maxima .32/.26/.24/.15, plus bounded receipt Light. No prelight. R2 reconstructs lost original coefficients; Unity/HLSL/GPU exposure, half-size readability, sorting and Android repeated/AoE cost remain unverified. Eligible card count52/242 is not all-attack or guaranteed-visibility acceptance; Canvas remains semantic fallback.


### Stage31 wide water/fire R2 (cloud candidate)
Four exact modern-card opt-ins cover blue_tsunami/blue_rolling_wave and red_flame_wave/red_blaze_sweep. The two families are broad target-local lateral passages, not caster-to-target transport. Each positive actual damage receipt retains its target, amount and contact. Existing AoE stagger and the conditional blaze second damage are engine-owned; neither decorative width nor particle count creates targets or hits. Missing/wrong/noncard metadata keeps the recorded old fallback.

Native anticipation100ms fits the existing receipt window using the frozen negative-only C1 mapping. Postcontact bodies205/190ms and final tails300/262ms remain independent of core23ms hold/65ms end and actor pause. Real Land refreshes every mesh/property batch including invisible layers; unsupported resources or failed uploads retain one fallback. Scope/reset/recycling ownership remains versioned. No extra HP delay or actor pause is introduced.

Existing water, fire and droplet-lookup textures are shared byte-for-byte with unchanged imports and GUIDs. Water is510vertices/706triangles/3batches; fire703/962/4; fixed16particle birth cap. Bounded standard scalar applies once. Camera basis and alpha-visible target height are captured, right mirror fixed1; no inferred caster socket. Linear texture sampling, native per-vertex alpha/gain and one premultiplied output preserve the reference. Waterdrop lookup/core mixture is an explicit R2 unlit approximation of earlier Principled shading.

Eight global slots/Low admissionfour. Per-profile texture admission avoids disabling fire for missing water resources. Scene colors are independent and active only at actual Land; provisional bounded point-light ratios38/14 water and45/16 fire against55/20 reference are not physical area-watt equivalence. Source/stub/CPU numerical tests do not establish native Unity HDR/overdraw/exposure/sorting, camera-edge readability or Android performance. Continuous-card candidate count56/242 and enemy5/167 are routing coverage only; Canvas remains fallback.


### Stage32 local material profiles R1 (cloud candidate)
Five exact definitions share local dark, light, botanical, physical and short-water responses. Four support card-kind damage; two of those also support exact necro damage. Powder pod supports reaction-kind counter and set-destruction damage. The source-kind allowlist requires modern metadata, rejects malformed/unknown tokens and never extends to legacy marker-only events. Existing element/style labels, gameplay, events, RNG, actual receipt timing and old card-only memberships remain unchanged.

Each positive actual DamageDealt owns one P0 effect; guard/nullify, costs, DoT, reflection, recovery and non-damage resource events retain their separate paths. Water has no ring/vortex/return, dark is outward release without drain/heal implication. Native post tails218/198/273/238/263ms; dark body212ms follows the frozen formula and corrects only the old208ms prose (overall218ms unchanged). Native motion does not stop with core14ms hold or actor pause.

Three batches with per-vertex role and phase. Fixed12births, prefix thinning only, scalar once, no new hits. Five existing PNGs are shared; botanical has no texture dependency. Palette and independent scene colors decode once, one premultiplied output. Eight global slots/Low4, maximum24active draws and120lazy meshes (Low-only60). These are code budgets, not native memory or frame-time measurements. Scene lighting is bounded provisional Unity mapping, actual-contact only and max-combined. Failed new drawing retires once and leaves one existing fallback.

All11 minimal-profile shapes are integrated as cloud candidates after this stage. Membership is61/242 damaging definitions:57 literal profiles (including one reaction-only definition), one leader-conditional and three direct Showcase candidates. Ordinary card-kind eligible count60, with two additional necro contexts. This is not all-source or native visual completion: semantic expansion, real enemy multi-hit contribution metadata, non-slash tier geometry and Unity/Android exposure/motion/performance remain open.


## Stage33: explicit shared semantic routes (cloud candidate)

98 finite, individually reviewed memberships reuse existing continuous components:40 modern card-kind definitions and58 ordinary enemy move keys. Existing element/style, target count, random damage, actor pause, transport delay and contact ownership remain unchanged. No new textures, renderer code or engine logic. Card memberships require exact modern source metadata and an explicit card-only allowlist; unrelated reaction/necro/permanent/gear contexts remain blocked.

Shared point/horn/fang/claw/fire contacts describe the local impact, not bespoke full actor or weapon motion. `green_sweeping_horn` keeps its game targeting and uses horn-tip contact; no swept hitboxes or additional targets are created. `red_bomb` retains the existing fire presentation interpretation without declaring a new gameplay attribute. All multi-hit enemies still produce one aggregate receipt and one contact until the separate real-contribution metadata/projection stage.

The rock-beetle forced-bite path can retain an old intent ID; both bite and horn_jab remain outside this expansion pending exact executed-move provenance. The normally reachable burrow-worm bite remains allowed only through the existing matching snapshot key. Conditional-alt and missing-snapshot fallbacks remain. Newborn or replaced actors must pass the existing lineage checks. Renderer/resource/pool failure continues into the existing single fallback. Native Unity/device visibility, audio/contact feel, CPU/GPU performance and all-path acceptance remain unverified.

### Stage34: actual enemy presentation provenance and hit contributions

Existing declaration/executing/damage events may carry versioned display-only move provenance. The declared primary/alternate IDs are captured from the chosen definition; the branch is captured at the existing effective-intent call before locking/pre-reaction mutation. Forced burrow bite captures its actual definition ID even when the old gameplay intentMoveId remains stale. Self-indexed event links survive full native saves; missing, malformed, trimmed or crossed-action histories stay unresolved. These references are not cryptographic proof of arbitrarily hand-edited state.

Ordinary enemy DamageDealt additionally captures ordered amount/HP/block/ice/nullified/mitigated contributions from the existing loop. Engine HP still mutates once; hooks, counter, DeathSaved, event count/order and RNG remain unchanged. No intermediate engine HP snapshots are claimed. At most256 rows are retained; larger loops record actual executed count and an explicit omission reason, never a partial array or equal division. ConfusedAttack has no such loop and remains aggregate-only.

This increment only supplies metadata. The subsequent numeric-ledger/contact consumer must validate all sums and links, own one outer receipt, commit each display contribution in order, and keep turn/finish gates pending until the final contact. Until that consumer lands, existing aggregate presentation and the Stage33 routing remain unchanged. Native Unity/Android verification is still pending.


## Stage35: real enemy contact projection (2026-10-09 cloud candidate)

Ordinary enemy DamageDealt now consumes the optional Stage34 calculation-derived contribution rows as presentation-only intermediate vitals. One real engine event keeps one numeric receipt and one outer action/VFX acknowledgement. For 2–12 validated rows, each real HP, Block and IceBlock contribution receives a visible contact; nullified/mitigated/zero rows remain real contact outcomes. Nominal spacing is120ms anchored to the first actual landing, with a maximum planned additional span1.32s. A long missed-frame interval shifts remaining visual time and never emits two children in one frame. Actual pending work, not that estimate, owns completion. Normal authored enemy attack definitions currently declare at most6 hits; dynamic mirrors are not universally capped.

The first landing retains the existing sequence slot and existing snapshot-based transport delay (0 or220ms), while actual executed-move metadata selects the impact route. Conditional alternatives and forced bite no longer have to borrow a primary/stale snapshot move when a validated explicit key exists. Legacy missing metadata retains the old fallback. Source proof is an accepted-command/event-reference lease and captured source-record identity, with same-ID record changes conservatively unproved. Post-attack source death preserves its earlier valid contributions; hatch/prior retirement do not.

Projected children are plain independent FX tickets, never invented GameEvents or extra numeric receipts. Raw negative projected HP is retained; actual later DeathSaved/heal/counter determines the gameplay outcome. No child infers a finishing blow or adds actor hitstop. One attack preparation occurs, followed by contact-only child effects. Missing original art anchors permit numeric-only contacts; replacing a captured source/target visual unit cancels and reconciles a started train. Before child0 a safe aggregate fallback remains possible. Completed source leases are released.

More than12 actual hits use one explicitly labelled aggregate showing the true calculated count and HP total (including0, and missing-player-anchor fallback). Captured rows are not divided, truncated into invented hits or consumed twice. More than256 rows keeps Stage34's explicit capture-limit reason and real total count. ConfusedAttack has no such per-hit calculation record and remains one aggregate receipt.

Later numeric values, reaction/status dispatch, next actor cues, preview/trace preparation, and display-sequence finish wait for earlier train contact debt. Existing scoped Tween/effect tails and actor/death work remain in PresentationIdle; GameRoot's existing enemy-turn/terminal completion gate waits that predicate before outcome/reward rebuild. No new engine state, RNG calls, damage rules or additional hit events are introduced. Native Unity/Android frame-time, art readability, source/target replacement and actual reward-screen timing still require execution QA.

The old27 subtle S/M/L proposal remains rejected and unintegrated. Distinct-spell-like fire/ice tier redesign is separate: future anticipation changes must share the actual landing clock with HP/guard UI and actor cue, and completion must include visible lifetime.

Stage35 global response cap: local impacts, real-row numbers, outcomes and audio stay per contact. The existing aggregate fullscreen hurt flash and any already-enabled legacy camera shake are submitted once at the first positive-HP contribution. All guard/nullify rows submit neither. Existing fixed-camera/world-mode gates remain untouched; no shake/zoom feature is enabled. This prevents overlapping full-screen red overlays; it is not native flash-safety certification.


### Stage36: twenty-nine exact enemy definition routes

Adds 25 regular definition keys and four execution-proof-only keys to existing continuous families: pierce, slash, fire projectile/local release, blunt and fang. Existing style/material values remain. The four set_breaker/smash, joker/cautious_jab, rock_beetle/horn_jab and rock_beetle/bite require validated declaration/execution metadata. Without proof they produce an explicit neutral legacy fallback with ExactSource=false; snapshot candidate IDs are not described as executed moves. Invalid annotated metadata still fails closed without a snapshot retry. Other legacy snapshot routes retain their compatibility behavior.

The joker cautious_jab definition is currently dormant in the normal graph. It is counted as a registered definition, not proof of reachable combat population. First-contact transport timing, actual contributions, HP/guard, actor pauses and RNG are unchanged. New authored residue may differ from previous fallback art and remains owned by the existing completion scopes. No added hit, camera feature, texture or gameplay change. Unity/GPU/Android appearance remains unverified. The rejected subtle 27-tier candidate is not used.


### Stage37: ten exact card-kind shared routes

Reuses wide water/fire, fire/warm-white scatter and local light on ten explicitly reviewed definitions. Their source metadata must identify card kind and the definition ID; the supplied UID is carried for attribution. This existing route gate does not independently prove UID lineage or reject an absent UID. Both the source-kind allowlist and card-cast metadata gate are required; this suppresses duplicate legacy generic caster effects when a matching positive card receipt and family resources are available. No non-card context is automatically added. Permanent embers remain a separate policy task.

Actual effect receipts own all targets, damage counts and timing. Empty-hand Tide Surge may still have positive damage from modifiers; zero Sparks skips only Spark Storm's second, scaled damage effect; its first 8-damage effect remains. Blaze Spark's conditional second damage and first-hit lethal suppression come from the engine, not particle counts. Token/light/energy/status changes do not create attack effects. Material/style, first contact, actor pause, gameplay and RNG are unchanged; authored residue remains scoped. No new texture, renderer, camera feature or rejected tier candidate. Cloud source tests do not imply Unity/Android visual acceptance.


## Consolidated source coverage candidate 38 (2026-10-10)

The static inventory now assigns all 419 damage-capable definitions (242 cards/fusions, 167 enemy moves, two leaders, six relics and two gears), covering 428 definition/source-kind contexts and the non-Izzet default of aether burst. This is routing eligibility, not exhaustive combat reachability, pixel visibility or final art acceptance. 151 contexts intentionally use explicit neutral contact; their semantic reasons remain auditable. Two move definitions, break_trap and call_bluff, have definition/renderer checks but no verified reachable graph node.

Exact optional source metadata selects `_presentationSourceRoutes[definitionId][kind]`. UID is carried unchanged, including null; it is not independent lineage proof. Non-card routes require `noncard_fixed_small_contact_v1`: final size .88, particles .65, emission .72, scene light .65 and small presentation topology; factual damage intensity remains intact. This policy is contact-only and does not add actor anticipation, pause, hits or gameplay events. Authored slash already encodes its small topology, so scalar attenuation is not applied twice. Ordinary card overlays retain their existing contact clocks.

The bounded contact renderer adds 24 compact profiles for conservative minimal shapes/materials and neutral positive-damage receipts. No new texture is imported. Each instance has one transparent draw, at most 160 vertices/240 triangles and four motes, with a 180 ms tail. The pool caps at 24 high/12 low and recycles with versioned handles. Linear palette input is converted once from sRGB and shader output is premultiplied once. These are implementation budgets, not measured GPU or mobile frame-time results.

Unknown/missing provenance remains inexact. A positive damage receipt with unproved provenance, or a missing bounded renderer resource, gets one achromatic world contact when capture/resources are valid, or an achromatic UI contact where an active canvas exists. It does not inherit a previous card's weapon or element. No available world/UI surface is recorded as `no-render-surface`, not a successful draw. Guard, nullify, burn, affliction, self-cost, reflection and healing keep their distinct outcome routing. Legacy enemy transport styles and the four existing executed-move proof gates are unchanged.

The existing actual-contribution enemy timeline remains: two to twelve captured real contributions, typically 120 ms apart; thirteen or more retain the true count and aggregate result. Confused attacks remain aggregate-only. Receipt completion, numeric projection, actor pause ownership, target generation, cancellation and enemy/finish barriers continue to own presentation. New tails may differ from legacy art, but no added per-target gameplay delay is introduced.

Autopilot fire coverage now accepts an observed continuous-fire resolved contact instead of incorrectly requiring eight legacy atlas frames after that renderer has been replaced. Legacy playback still requires the full frame mask. This source correction has not been executed in native Unity.

Remaining gates: independent cloud regressions and exact incremental application checks; all Unity import/GPU/transparent sorting/real target visibility, Android memory/frame-time and accessibility validation; PC application blocked by the existing transfer403 issue. Compact neutral/minimal shapes are conservative readable candidates, not proof of professional visual quality. The rejected subtle27-tier set remains unintegrated. New distinct-spell fire/ice/water tier designs remain separate unregistered candidates and cannot be counted as completed runtime variants. No rejected water vortex or old vine art is restored.

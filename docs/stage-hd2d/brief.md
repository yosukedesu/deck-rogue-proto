# 幕2/3 の舞台を HD-2D 品質へ — 設計者向けブリーフ (2026-09-21)

## 依頼
ユーザー「マップ背景の3Dが結構荒いのでハイクオリティなHD-2Dゲーム（オクトパストラベラー）くらいのディテールにしてほしい。全体的にHD-2Dに」。
幕1（坑口の森）は作法を固めて済み（下の「幕1でやったこと」）。同じ作法で幕2（先代の坑道の宿場跡）と幕3（埋もれた古代都市）を作る。

## 舞台の座標系（Stage.cs）
- 地面 y=0。道は PathYaw=-22° で手前左→奥右。OnPath(t, s): t=道に沿う位置（リーダー t=-5、敵 t=1.6〜11.2）、s=道からの横（+奥/-手前）。戦闘の場は t∈[-13,17]・s∈[-2.6,4.6] の楕円で、ここには大きい物を置かない。
- カメラ: 透視・画角36°・見下ろし12°。基準深度で 1 unit = 100px（1080p）。画面の高さ = 10.8 units。世界原点は画面の下から45%。
- 粒: キャラは 1ドット=4px（0.04 unit/ドット）。地面のタイルは 64ドット=1.28 unit（2px/ドット）。敷物も 2px/ドット。
- 幕2/3 は今 `_flat=true`（平らな床・起伏なし）。幕2は PaintMarket、幕3は PaintCorridor（同梱の paint-act23.cs）。
- 既存の幕2/3の PixelLab 小物: act2_{barrel,crate,crystal,gear,lantern,minecart,roots,stalactite,stall}、act3_{aqueduct,brazier,chain,gate,pillar,pillar_fallen,spire,statue}（32〜128ドット）。背景の板 bg/act2.png, bg/act3.png (384×216)。タイル act2/3_{grass,dirt,stone,cliff} 32×32（旧・4px/ドット）。

## 幕1でやったこと（同じ作法で幕2/3も）
1. 地面: 材質ごとに 64×64 のタイル4種（a〜d）を 2×2 のアトラスに束ね、タイル単位で種と向きを変えて繰り返しを消す。明度→ノーマルマップで月光の陰影。`Ground(act, kind)` が `Art/tiles/act<N>_<kind>_{a,b,c,d}.png` を拾う。
2. 敷物: 材質タイル `Art/tiles/act<N>_m_<name>.png` をコードで不定形にくり抜く（`PatchSet`）。落ち葉・苔・砂利・ひび・泥…。
3. 大物は一枚絵の板（ビルボード）: `Art/props/act<N>_<name>.png`。`SpriteH(name, tex, pos, 世界の高さ, 接地影の幅の係数)`。影を落とす。
4. 光: SSAO・月光を強く・光の筋（Px.Beam の板）＋足元の光溜まり・DoF は Bokeh のティルトシフト（座席は鮮明）。幕2は提灯の暖色、幕3は脈の白緑の光。
5. 幕3の床の一部を浅い水面にして反射を出す（ユーザー裁定）。幕2/3 に段差・台座（本家の段丘）を入れる。無地の箱の柱・梁（今の幕2）は一枚絵に置き換える。

## PixelLab（pixflux）の勘所 — 幕1の実測
- 上限 400×400 だが **透明背景が効くのは面積 ≈30,000px まで**（160×192 は可、192×224・256×320・320×160・400×112 は灰色の背景が付く）。大物は 160×200 以内で作り、必要なら 1.25〜1.6 倍に拡大して置く。
- 一枚絵の決まり文句（効いた）: "isolated pixel art game sprite of a single object, centered, nothing else in the frame, completely empty transparent background, no scenery, no ground, no soil, no shadow on the ground, no sky, no moon, no platform, <物の説明>, HD-2D Octopath Traveler style pixel art, detailed shading, muted cool palette (...), no text" ／ negative: "scene, background, landscape, forest, trees behind, sky, moon, ground plane, grass field, island, platform, isometric block, soil disc, shadow disc, frame, border, text, watermark, blurry, bright saturated colors, daylight, people, character, face, animal"
- 描写に「forest」「night」「moonlight」「scene」を入れると木・月・風景を描く。小さな物を大きなキャンバスに頼むと周りに風景を描く（キャンバスは物の大きさに合わせる）。
- **「地面に寝かせた敷物」は頼めない**（浮島・台座・皿になる）→ 材質タイルをコードでくり抜く。水たまりも皿になった→コード生成。
- タイル: "flat seamless repeating texture of <材質>, extreme close-up, uniform all over, fills the whole frame edge to edge, no vignette, no objects, no border, lineless" で 96×96 を作り中央 64 を切る（縁の額を外す）。「forest」「dirt」は木や段丘を呼ぶので材質名は具体的に（"bare brown earth, packed", "old flagstones"）。土は "green" を negative に。pixflux は丸い塊（lumps）を描きがち。
- **等間隔に粒が並ぶ絵（花・粒・穴）はユーザーが「集合体恐怖症を煽る」で却下**。花の茂み・クローバーの絨毯は避ける。
- 生成は 1枚 ≈10秒。数は気にしなくてよい（月の枠は十分）。

## 幕2「先代の坑道」の世界観
何代も前の坑匠が掘った坑道。宿場の跡に提灯と炉と行商の店。木の枠組み（支保工）・トロッコ・樽・歯車。獣と古機が混在。光は提灯の暖色だけ（他は青灰の夜）。露頭の脈（青緑）が壁に走る。

## 幕3「埋もれた古代都市」の世界観
坑の底。石造の街路と、止まらない採掘機械（古機）。脈が最も濃く、空気が光って見える（白緑）。古代都市がなぜ滅んだかは決めない。回り続ける櫓・岩天井・街の輪郭・脈の光の帯が今ある。床の一部を浅い水面に（反射）。

## 求める成果物（JSON）
各幕について:
- composition: 遠景/中景/近景/額縁（画面の上下左右の縁）に何を置くか、地面の材質の分布（道・床・段差・水）、光（光源・色・筋・光溜まり）、粒子。オクトラの同種の場面（坑道＝Quarrycrest/ Clearbrook の洞窟、古代都市＝Hornburg/ 神殿）を参考に「何があると HD-2D に見えるか」を具体的に。
- tiles: 地面の材質タイル（a〜d の4種＝同じ材質の揺らぎ）と敷物の材質（m_*）の一覧。id・description（上の決まり文句に沿う）・negative。
- props: 一枚絵の一覧。id・size [w,h]（≤160×200・物の大きさに合わせる）・description・negative・想定の世界の高さ(units)・置き場（遠景/中景/近景/額縁）・数。
- terrain: 段差・台座・水面・壁の作り（コードで組む幾何。何 units の段を何段など）。
- lighting: 光源の位置と色・SSAO・光の筋・光溜まり・粒子。
- risks: PixelLab が失敗しそうな絵と回避策。

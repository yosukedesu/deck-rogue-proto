# 作成が必要な画像一覧（2026-09-11 時点）

`docs/pixellab-assets.md` の発注書と `unity/Assets/Resources/Art/` の実物を突き合わせた**未作成リスト**。
無い絵はすべてコード生成のプレースホルダーで動いている（ゲームは止まらない）。寸法は実寸、画面には整数倍で置く。
優先度は「プレイ中に目に入る面積 × 枚数」で付けた。

## 済んでいるもの

| 種別 | 状態 |
|---|---|
| 敵 | **84/84**（幕1〜3・エリート・ボス。2026-09-11 に幕2/3の59体を追加、向きを検算済み） |
| カード挿絵（緑） | **91/91**（打撃・打ち据えは魔導の大斧版に更新済み） |
| このは | ちび1・アニメ4種（待機3/被弾3/防御3/攻撃4） |
| 舞台の小物・タイル | 幕1: タイル6（bark/cliff/dirt/grass/grass2/stone）・小物15／幕2: 小物5（barrel/crate/gear/lantern/stall）／幕3: 小物4（brazier/chain/pillar/statue） |

## 未作成（優先度順）

### A. 面積が大きく毎戦闘で見える

| # | 種別 | 置き場・名前 | 実寸 | 枚数 | 備考 |
|---|---|---|---|---|---|
| 1 | カード挿絵（青） | `Art/cards/<id>.png` | 80×48 | **76** | 発注書 `docs/pixellab/cards-green.json` の style を流用。青は凍結中だがプレイ可 |
| 2 | カード挿絵（赤） | 同上 | 80×48 | **84** | |
| 3 | カード挿絵（白） | 同上 | 80×48 | **77** | |
| 4 | カード挿絵（黒） | 同上 | 80×48 | **84** | |
| 5 | リーダーのちび（残り14人） | `Art/leaders/<id>.png` | 64×64 | **14** | 絵柄は「オクトラ正統」（HD-2D 3頭身アニメ顔）で確定。向きの検算（右向き）必須。武器は各リーダーの設定に合わせて要裁定 |
| 6 | リーダーのアニメ（残り14人） | `Art/leaders/anim/<id>_{idle,hurt,block}_{0..2}.png`・`<id>_attack_{0..3}.png` | 64／攻撃128×96 | 14人×13コマ | `scripts/konoha-weapon.py` の手順（武器は別レイヤー・骨格は本人の絵から推定） |

### B. ラン画面・マップで常に見える

| # | 種別 | 置き場・名前 | 実寸 | 枚数 | 備考 |
|---|---|---|---|---|---|
| 7 | レリック | `Art/relics/<id>.png` | 32×32 | **39** | 今はコード描画の紋（`ThemeFx.RelicGlyph`）。世界観: 坑で出るもの＝古代の遺物／店のもの＝行商の品 |
| 8 | リーダーのアイコン | `Art/leaders/<id>_icon.png` | 32×32 | **15** | セットアップ・ラン画面用（ちびの顔の切り出しでも可） |
| 9 | マップのノード | `Art/map/node_<type>.png`（battle/elite/boss/shop/campfire/workshop/unknown/treasure） | 32×32 | **8** | 今はアイコンの絵文字＋枠 |
| 10 | 意図アイコン | `Art/icons/intent_<kind>.png`（attack/buff/defend/destroy-set/flee/hatch/heal/hex/mill/rally/rest/steal-gold） | 24×24 | **12** | 今は状態アイコンで代用 |
| 11 | 状態アイコン | `Art/icons/<name>.png`（sword/shield/heart/energy/draw/growth/momentum/burn/exposed/gold/exhaust/pierce/skull/crown/hammer/question/chest/flag/map/star + set/crest_permanent） | 16×16 | **22** | 今はコード描画（`Theme.IconArt` のビットマップ）で成立しているので後回し可 |

### C. 舞台（幕2/3を潜った時だけ）

| # | 種別 | 置き場・名前 | 実寸 | 枚数 | 備考 |
|---|---|---|---|---|---|
| 12 | 幕2/3のタイル | `Art/tiles/act{2,3}_{grass,dirt,stone,cliff}.png` | 32×32 Repeat | **8** | 今はコード生成の石床。2026-09-11 のハイディテール舞台はコード生成で成立しているので急がない |
| 13 | 幕2/3の小物の追加 | `Art/props/act2_*`・`act3_*` | 任意 | 任意 | 結晶・トロッコ・鍾乳石・大門・水道橋はコード生成（`Px.*`）。差し替えるなら同名で置く |
| 14 | 背景（空の板） | `Art/bg/act{1,2,3}.png` | 480×270 | 3 | 今はグラデーション。幕2/3は岩天井なので不要になった可能性が高い |

### D. UI の部品（コード生成で完成しているもの＝任意）

| # | 種別 | 置き場・名前 | 実寸 | 枚数 | 備考 |
|---|---|---|---|---|---|
| 15 | 紙の9スライス | `Art/ui/paper_{card,panel,tag,button}.png` | 52/48/32/40 | 4 | 今は `PaperFx` が紙と鉛筆の二重線を描く。差し替え不要の判断でよい |
| 16 | コスト玉・レア度の宝石 | `Art/ui/cost_orb.png`・`gem_{common,uncommon,rare}.png` | 20／12 | 4 | 同上 |
| 17 | 攻撃のエフェクト | `Art/fx/slash.png` | 任意 | 1 | 今はコード描画 |
| 18 | 焚き火・工房・ショップ・イベントの情景 | `Art/scenes/{campfire,workshop,shop,event}.png` | 240×135 | 4 | 現状の画面は紙のパネルだけで成立。世界観（宿場の炉・行商）を見せるなら |

### 外注（AI 不採用）

| 種別 | 枚数 | 備考 |
|---|---|---|
| リーダー立ち絵（頭身高め・萌え） | 15 | 人間の絵師へ（1〜5万円/体）。面白さ検証後に投資判断 |
| キービジュアル | 1 | 同上（3〜10万円） |

## 合計（PixelLab で作る分）

- 必須級（A+B）: カード 321・ちび 14・アニメ 14人分・レリック 39・アイコン 15・マップ 8・意図 12 ＝ **約 410 枚＋アニメ**
- 任意（C+D）: タイル 8・背景 3・UI 部品 8・エフェクト 1・情景 4

## 手順の参照

- 敵: `docs/pixellab-assets.md`「幕2/3の敵59体」（種の記述 → 2シード → 判定 → 適用 → 作り直し → 向きの検算）
- リーダー: 同「リーダーの絵柄＝日本のかわいいデフォルメ」＋ `scripts/konoha-weapon.py`
- カード: `docs/pixellab/cards-green.json` / `cards-green-magiaxe.json` の style（Octopath HD-2D、人物なし、単一の主題）

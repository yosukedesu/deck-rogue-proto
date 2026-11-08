# A2b 潮の岐路 / Tide Crossroad

**完成・取り込み用 PNG 同梱。ネイティブ 80×48、RGB、39 色、完全不透明。**

- カード ID: `blue_mode_tide_crossroad`
- 仕様: 1E 呪文。「7 ダメージ」または「氷壁 4」を選ぶ。
- 束: `codex/assets-a2b-20261108`
- 取り込むファイル: **`Art/cards/blue_mode_tide_crossroad.png`**（この README からの相対パス）
- ゲーム側の取り込み先: `Art/cards/blue_mode_tide_crossroad.png`
- Filter: Point / Nearest、Mipmaps: off。静止画 1 枚。

## 制作方法と決定事項

ユーザーの「本物のドットをあなたが打てない？」という指定を受け、80×48 のキャンバス上へ整数座標で輪郭・色面・1px 単位の光を直接配置した。大きな生成画像の縮小ではない。

- `source/draw_tide_crossroad.py` に全配色と座標を収録。画像を入力せず、原寸で描画する。
- アンチエイリアス、ぼかし、ランダムなノイズ、画像の縮小は使わない。
- 39 色の固定パレット。既存の比較用青カードは各 36 色。色数のみでなく原寸の視認性と構図を比較した。
- 既存 567 枚と同じ、一人称で手元から術を出す構図。紺の袖と手袋を手前に置いた。
- 左の曲がる水流と右の角張った氷壁で、攻撃／防御の二択を一枚の空間にまとめた。
- 青系カードの既存原画に合わせ、青緑の術と赤茶の石造背景を対比させた。
- 既存カードの柔らかい陰影に対し、本作の輪郭は硬く明瞭なネイティブドットで、質感と陰影もやや簡素。手の甲・指の側面・袖・背景アーチの陰影は再審査で改善した。完全に同一の描法と称していない。
- パネル分割、文字、数字、カード枠、キャラクター全身は描かない。
- 夜硝子・真鍮は UI 側の規約。カード絵に UI 枠を焼き込まず、紙地・クリーム色の UI・太い墨線も追加しない。

初めは imagegen で 80×48 の出力を 2 回試したが、1619×971 の画像になったため不採用。納品 PNG はそれらの画像を入力せず、座標から新規に制作したもの。試作原画は取り込み物に混ぜていない。

## 検証

- 実ファイル 80×48、RGB、39 色、透過なし。
- 原寸と 10 倍 Nearest 表示を目視確認。両手、水流、氷壁を識別できる。
- 既存の水流・氷壁・防御カードと 4 倍の比較シートで対照。
- 制作スクリプトから再生成した PNG の SHA-256 が一致。
- `qa/validation.json` に検査結果、`manifest.json` に実寸・チェックサムを収録。
- `qa/` の拡大画像はレビュー専用。ゲームへ取り込むのは `Art/cards/` の原寸 PNG のみ。

## ファイル

- `Art/cards/blue_mode_tide_crossroad.png`: 取り込み用の完成画像。
- `source/draw_tide_crossroad.py`: 再制作できる原寸座標ソース。Pillow が必要。
- `qa/blue_mode_tide_crossroad_10x.png`: ドット確認用 10 倍 Nearest。
- `qa/style_comparison_4x.png`: 本作と既存 3 枚の 4 倍比較。
- `references/`: 読み取り参照のコピー。取り込み不要。

再生成:

```sh
python source/draw_tide_crossroad.py
```

## 参照と変更範囲

基準は main `114ee430310c5a2df02ea048f23d1f8375c22c66` の `docs/dot-asset-backlog-2026-11-02.md` と、その後のユーザー指示。公開 main の一覧は最新の取り込み状況を反映していないため、本束で一覧やゲーム実装を上書きしない。

比較用 PNG は基準コミットの `unity/Assets/Resources/Art/cards/` からコピーした、既存 80×48 RGB 原画。

- `blue_tide_drop.png`: 仮置き元・一人称の袖と手元。
- `blue_current_lash.png`: 水流の弧と色調。
- `blue_ice_wall.png`: 氷壁の稜線と面。
- `blue_guard.png`: 防御の手元構図。
- `blue_mode_tide_crossroad.png`: 置き換え前の仮置き。`blue_tide_drop.png` と同一。

**変更はこの handoff フォルダのみ。** 立ち絵、BGM、キービジュアル、`src/engine`、`src/data`、Unity のコード、既存素材本体は触っていない。ゲームへの取り込みは未実施。

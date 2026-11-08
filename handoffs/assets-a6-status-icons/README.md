# A6 状態異常アイコン14種

2026-11-08。32×32 RGBA の透明PNGを14枚。夜硝子の暗板と真鍮の細線に合わせ、実寸格子上へ直接描いたピクセル素材です。紙・クリーム色・太い墨線、数字や共通背景板は焼き込んでいません。

## 取り込み

`png/` に各アイコン、`manifest.json` に意味・エンジン側フィールド・旧UI別名・SHA-256を記載。`status_atlas.png` は左上から行優先の7列×2行、各32×32です。

各画像の32×32枠を維持し、透明余白を自動トリミングせず使用してください。補間はNearestを想定。アトラスはパディングなしなので、フィルタやmipmapを使うUIには個別PNGを推奨します。

| 素材 | 日本語 | 実装への対応 |
|---|---|---|
| status_weak.png | 弱体 | PlayerState.weak / PlayerStatus.weak |
| status_vulnerable.png | 脆弱 | PlayerState.vulnerable / PlayerStatus.vulnerable |
| status_frail.png | 虚弱 | PlayerState.frail / PlayerStatus.frail |
| status_restrain.png | 拘束 | PlayerState.restrain / PlayerStatus.restrain |
| status_mist.png | 霞み | PlayerState.mist / PlayerStatus.mist |
| status_slow.png | 重り | PlayerState.slow / PlayerStatus.slow |
| status_wound.png | 負傷 | PlayerStatus.wound |
| status_scald.png | 火傷 | PlayerStatus.scald |
| status_confusion.png | 混乱 | EnemyState.confusion |
| status_exposed.png | 急所 | EnemyState.exposed |
| status_enemy_weak.png | 威圧 | EnemyState.weak |
| status_burn_resist.png | 延焼耐性 | EnemyDef.burnResist |
| status_armor.png | 装甲 | EnemyState.armor / EnemyDef.armor |
| status_thorns.png | とげ | EnemyState.thorns / EnemyDef.thorns |

## 決めたこと

- 弱体は二重下向き、威圧は俯く角頭＋小さい二重下向き。両者は同じ `weak` でも実装の主体が違うため、素材名だけを分けました。エンジンIDは変更していません。
- 脆弱＝割れた心臓、虚弱＝割れ盾、急所＝的、装甲＝胴鎧として、旧UIで同じ的を借用していた状態も形で区別しました。
- 火傷 `scald` は焦げた掌と熱い水疱・湯気、延焼耐性 `burnResist` は炎を遮る盾です。延焼 `burn` 単体の流用ではありません。
- 霞みは紙色の札ではなく、青い板状の札を霧が横切る形です。
- 色は共通の青灰・真鍮を主体とし、負傷/脆弱の赤、混乱の紫、とげの緑を少量使います。色だけで状態を区別しない構成です。
- 生成した大画像を縮めた素材ではありません。`source/draw_icons.py` は32×32の整数座標へ直接描画し、入力画像を読み込まず制作します（参照画像は描画に入力しません）。拡大は確認画像だけです。

## 検証と制限

`qa/verification.json` に14枚の寸法・RGBA・透明度・余白・パレット・SHA・重複なし・アトラス一致・再生成一致を記録。`qa/visual_review.md` に独立した原寸/暗背景の見た目検査を記録します。

実ゲームへの取り込み、現行Godotの状態名マッピング、実機での32px未満の表示は未検証です。旧Unityは表示寸法によりコード描画の16pxアイコンを返すため、PNGを置くだけで自動的に全画面へ反映されるとは扱わないでください。

`wound` と `scald` は状態札の概念であり、持続カウンターと決めつけていません。`confusion` の旧UI別名は `confuse`、`thorns` は `counter` です。`junk`、`turnArmor`、延焼そのもの、B1の付与アニメはこの14枚の範囲外です。

## 原典と変更範囲

- 対象: `docs/dot-asset-backlog-2026-11-02.md` のA6。
- 参照した公開main: `114ee430310c5a2df02ea048f23d1f8375c22c66`（2026-11-04）。最新ユーザー指示の夜硝子・真鍮を優先。11/05追補の正確な色トークンはこのmainにはないため、完全一致とは記載しません。
- 詳細な各状態の出典: `source/targets.json` と `manifest.json`。
- 既存6枚を `references/` に無改変保存。意味や輪郭の参考のみで、旧クリーム色/濃い縁は新素材へ継承しません。
- この納品は `handoffs/assets-a6-status-icons/` の追加のみ。立ち絵・BGM・キービジュアル・src/engine・src/data・Unity・本体コードは変更していません。

## 再現

Python 3 と Pillow が必要です。

```
python source/draw_icons.py
python source/verify.py
```

後者は一時コピーで再生成のバイト一致も確認します。画像のパレットは `palette.json`、座標は制作ソースに含みます。

# 残件の記録（2026-11-04 夜・ユーザー就寝前）

ユーザーの指示「残件は記録しつつ、見た目・UX・演出の課題をまとめて単独で解決できる箇所を全て実装・改善。大きく見た目や仕組みを変えてもいい。大手並みの UI/UX・面白さ・ドーパミンが出る演出」。
この夜の作業の記録は `docs/design/juice-pass-2026-11-04.md`（作業中に作る）。

## A. 今すぐ手を付けられるもの（dot を待たない）

| # | 残件 | 状態 | 置き場・手がかり |
|---|---|---|---|
| 1 | **品質スプリント #5 音の点検**。札ごとの当たりの音は 3 枚だけ・`block` の音の鍵がコードにあるのに `audio.json` に無い・幕1 の一周を聴いて採点していない | 未着手 | `docs/design/quality-sprint-2026-10-28.md` §2 の順・`unity/Assets/Game/Audio.cs`・`Resources/Data/audio.json` |
| 2 | **ひばなの法線**。dot の束に法線が無く、旧い一枚絵の法線のまま 13 コマが動く | 未着手 | `scripts/sprite-normals.py normals`・`Art/leaders/anim/leader_red_*` |
| 3 | **テストの既知の赤 1 件**。`src/sim/unity-colors.test.ts` が dot の `LightningR9CandidateFx.cs:44` の色の直書きで落ちる（main でも） | 未着手 | 色を `PaperFx` の名前かデータ JSON へ |
| 4 | **魔導書の発光の位置**が待機 1 コマ基準で固定（`StageUnits.cs` の `{"leader_red", (0.48,0.42)}`）。攻撃の当たりで本の位置とずれる | 未着手・目立たない | コマごとの位置を持たせる |

## B. 次の節目・実機待ち

| # | 残件 | 待ち |
|---|---|---|
| 5 | 攻撃以外の演出の経路で PC の撮影で未確認のもの | `docs/dot-godot-port-2026-11-02.md` §5-2 |
| 6 | Android で Float32 の頂点色（`SetVertexBufferParams`）が IL2CPP で通るか | 次の APK |
| 7 | Xperia 5 II の fps の数字（友人） | 報告 |
| 8 | 立ち絵（ChatGPT・`docs/design/portraits-chatgpt-2026-10-29.md`）→ 出立の支度 3.5 の再採点 | ユーザー |
| 9 | ひばなの採点（1〜5）→ `docs/dot-asset-backlog-2026-11-02.md` A1 に書く | ユーザー |
| 10 | dot の次の納品（とばり＝`leader_black`）→ §3 の受け入れ手順（取り出し→検算→取り込み→ビルド→撮影→シート→一覧→commit→push） | dot |

## C. 凍結中・裁定待ち

- 幕2/3 をジオラマにするか（段2。`HD2DFlags.DefaultDioramaActs="1"`）
- 人間ラン・ルール調整・新色の解凍（品質スプリント中は凍結）

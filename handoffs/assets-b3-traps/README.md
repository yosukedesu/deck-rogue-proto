# B3 からくり・仕込み札の演出 4 種

2026-11-08 / `codex/assets-b3-20261108`。**素材の納品のみ。実ゲームへの取り込み・実機再生は未検証。**

| 演出 | `Art/fx/` | 合計時間 | 対応イベント |
|---|---|---:|---|
| 仕込む | `trap_place_8f.png` | 580 ms | `CardSet` |
| 鳴る | `trap_trigger_8f.png` | 560 ms | `ReactionTriggered` |
| 期限切れ | `trap_expire_8f.png` | 650 ms | `SetCardExpired` |
| 壊される | `trap_destroy_8f.png` | 575 ms | `SetCardDestroyed` |

全て **1 コマ 64×64・横 8 コマ 512×64・RGBA・左→右・index 0〜7**。中心アンカー **(32,32)**。index 7 は透明なクリアコマ。ループせず、終了時に VFX だけを消す。各コマ時間・矩形・SHA256 は `manifest.json`。

## 今回決めた規格と表現

B3 の一覧は寸法未指定だったため **64×64** とした。参照できた既存仕込みトークンは 68×74、挿絵は 64×38。演出はトークン中心で見せるオーバーレイであり、**64×38 の挿絵枠へ押しつぶして入れない**。状態帯にも一時的に重なる。ツールチップより下に置き、共通の論理ピクセル縮尺で等方拡大する。

- 最終サイズの整数画素で直接描いたネイティブドット。画像生成モデル・大画像の縮小・既存画像の切り貼りなし。再生成できる制作ソースを同梱。
- B1/B2 と同じパレット。夜硝子の暗色、青緑の歯車、真鍮の細い軸と接点。紙・クリーム色・太い墨線なし。
- **仕込む**: 離れた二つの歯車が寄って噛む。**鳴る**: 噛んだまま逆方向に回り光る。**期限切れ**: 噛み合いを解き、壊さず細い線へほどける。**壊される**: 亀裂と三方向の破片。失効と破壊を区別した。
- 透明余白は全方向 4 px 以上。alpha 0/255、透明 RGB=0。NEAREST・ミップマップなし・通常 alpha 合成。ぼかし・加算合成・トリミングなし。

## 実装する側へのメモ

1. VFX の位置は**仕込み枠中心で固定**。飛んでいくカードの子にしない。カード移動・配置・削除・行き先は既存 UI とイベントを正とする。PNG 単独がゲームの状態を変更することはない。
2. `CardSet` は既存の手札→仕込み枠 300 ms 飛翔が到着した時に再生。`CardSet` SE はすでに `BattleView` が鳴らすため追加で鳴らさない。終了後も仕込みトークン本体は残す。
3. `ReactionTriggered` は**発動前の枠**で再生。既存の 140 ms 跳ね→300 ms の対象への飛翔と着弾 `trap_fire_big` は別役割。歯車を対象への追加攻撃として扱わない。既存の枠の輪は置換候補とし、全部を重ねて過密にしない。カードが残るか離れるかは既存実装で判断。
4. **`SetCardExpired.to` は `discard / exhaust / hand` の三通り。期限切れ=消滅ではない。** 同時に `CardExhausted` が通知される場合、同じ出発点で B2 消滅と B3 ほどけるを同時に重ねない。B3 を枠で見せ、行き先側の移動処理と分担する。
5. `SetCardDestroyed` は生きていた仕込み札が壊されて捨て札へ行く通知。`onSetDestroyed` 内部処理が呼ばれたというだけで破壊演出を選ばない。期限切れで同処理が動くケースがある。
6. `ReactionWhiffed`（空振り）・`ReactionHeld`（温存）・`TrapLifeExtended`（寿命延長）には今回の失効／破壊を再生しない。毎ターン末だけで期限切れを推測せず、正式通知に従う。
7. `cardId` はカード定義 ID で、実体固有 ID ではない。**表示中の旧盤面とイベント順から対象枠を特定し、中心座標を保存してから UI を更新**する。同名複数枚と削除後の添字ずれに注意。
8. 既存の期限切れ移動は 550 ms、破壊は 300 ms。今回は VFX の尾を含め 650 / 575 ms にした。カードが消えた後も固定位置の VFX を最後まで再生できる独立ノードにする。既存タイミングを変更する場合は実装側で確認する。

SE は同梱しておらず、新たな SE 呼び出しも要求していない。

## プレビューと検証

- `previews/traps_contactsheet.png`: 全 32 コマ、2 倍 NEAREST。時間と index 付き。
- `previews/traps_native.png`: 1 倍で 2 種の暗色背景上に表示。**このファイルを等倍で確認する**。
- `previews/traps_playback.png`: APNG。4 種を順番に再生。manifest と同じコマ時間に、各演出の前 350 ms・後 700 ms の比較用停止だけを追加。APNG 非対応ビューアでは contactsheet を使う。
- 再生プレビューの枠は新規に描いた**説明用ダミー**であり、production PNG に含まれない。発動後に枠が残る見せ方は比較の便宜であり、ゲーム状態の指示ではない。
- `qa/verification.json`: 画素・寸法・色・alpha・余白・時間・フレーム区別・APNG デコード全コマ・再生成一致。
- `qa/visual_review.md`: 別担当による等倍／拡大と APNG フレーム確認。ゲーム画面での再生を見たものではない。

## 参照と変更範囲

`docs/dot-asset-backlog-2026-11-02.md` B3、`src/engine/types.ts`、`src/engine/combat.ts`、`unity/Assets/Game/{Presenter,BattleScreen,BattleView}.cs` を読み取り参照。確認可能だった main は `114ee430310c5a2df02ea048f23d1f8375c22c66`。参照箇所は `reference/implementation_notes.json`。最新のユーザー指定「夜硝子と真鍮」を旧紙テーマより優先。

変更は `handoffs/assets-b3-traps/` のみ。立ち絵・BGM・キービジュアル・`src/engine`・`src/data`・Unity コードは変更していない。

再生成（Python 3 + Pillow）:
```sh
python source/draw_traps.py
python source/build_manifest.py
python source/verify.py
```
プレビューのラベルは DejaVuSans。制作 PNG 自体に文字・フォントはない。

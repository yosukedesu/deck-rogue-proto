# ひなた v2 — PixelLab 発注書（2026-09-18。ユーザーが PixelLab を回す。手順はこのは v2 `docs/pixellab/konoha-v2/` と同じ）

## 0. 手順（このは v2 と同じ）

1. Gemini の決定稿 `reference-gemini.png`（1024×1024）から、**顎から下を縦 0.6〜0.7 に詰めた参考画** `reference-chibi.png` を作る（決定稿が既に4頭身なので 0.7 から試す。目標は 2.5〜3 頭身）。Claude に頼めばスクリプトで作る。
2. PixelLab **Create from Reference**（v3）に参考画を入れ、下の設定で8方向を作る。採用は **south-east**（右向き＝敵の方）。左向きに出たら PNG を左右反転（`.pixellab.json` の `postprocess` に記す）。
3. 同じキャラで Animate: idle（テンプレ breathing-idle）／attack（下の文）／block（下の文）。書き出しは **92×92**。
4. 書き出しフォルダ（`Idle/rotations/*.png`・`Idle/animations/**/frame_NNN.png`・`pixellab-metadata.json`）を `docs/pixellab/hinata-v2/` に置く。コマの選抜（攻撃4コマ・防御4コマ）・92×78 への切り詰め・`Art/leaders/` への配置・アイコン 32×32 の切り出しは Claude がやる。

## 1. キャラクター（Create from Reference の設定）

| 項目 | 値 |
|---|---|
| Template | mannequin |
| Size | **88×64** |
| Directions | 8 |
| View | **low top-down** |
| Reference | `reference-chibi.png`（顔と服の色を写す。形は参考画どおり） |
| 採用方向 | south-east |

プロンプト（このは v2 の文の型。コピーして貼る）:

```
Chibi 3 heads tall pixel art sprite in Nippon Ichi Disgaea style, bold black outline, flat colors with two-step cel shading. A young lamp-keeper apprentice girl with a timid but gentle look, a slightly troubled smile with raised inner eyebrows, faint pink blush marks. Large round sky-blue eyes with two white highlights, small closed smile.
Long blonde hair in one thick braid hanging over her left shoulder, soft bangs. She wears the white hood of her robe up over her head, edged with a thin brass trim; the bangs and the braid come out from under the hood and her whole face is visible.
Design language: black iron, polished brass, brown leather, white cloth.
Outfit: an ankle-length white hooded robe with wide sleeves and a short white shoulder cape with a thin brass trim, a large round polished brass clasp at the center of the chest. A wide brown leather belt with a square brass buckle, a small brown leather tool pouch on her left hip holding a brass oil can and a few brass screws, a brass wind-up key and a small key hanging on her right hip. Black leather bands on both wrists. Black iron boots showing under the hem.
Prop: a long thin dark iron pole much taller than her, held diagonally in both hands and raised forward, with a large brass and black iron lantern with glass panes hanging from a chain at the tip, glowing with warm orange-amber light. The lantern is on her right side, forward, lighting the path ahead.
```

ネガティブ（欄があれば）: `sword, axe, staff with crystal, blue light, green light, dolls, companions, cape covering face, nun, halo`

## 2. アニメ（Animate。south-east だけでよい）

| 動き | 方法 | 文 | コマ | Unity で使う数 |
|---|---|---|---|---|
| idle | テンプレ **breathing-idle** | — | 8 | 8（4fps。全部使う） |
| attack | v3 custom text | `She takes a big wind-up, raising the long lantern pole high above her head with both hands, leaning back, then swings it down in a huge wide arc across her whole body and thrusts the glowing brass lantern far forward at the enemy, the pole reaching out well past her body. The lantern glows with warm orange-amber light through the whole motion and, at the moment of impact, releases a soft gentle flash: a diffuse hazy bloom of warm golden light spreading around the lantern with soft blurred edges, no sharp rays or hard-edged star shapes, then the glow gently fades back as she pulls the pole back`（2026-09-19 ユーザー指示3件: ランタン発光→閃光を柔らかく→大きく振る） | 13〜17 | 4（振りかぶり／溜め（光る）／着弾／戻し） |
| block | v3 custom text | `She braces for impact, pulling the lantern pole horizontally in front of her body with both hands like a bar, hood and cape fluttering, the lantern glowing steadily` | 9 | 4（2,4,6,8 の要領） |

hurt は作らない（ユーザー裁定「ダメージは揺れるだけでいい」＝Presenter ののけぞり・点滅・揺れ）。

## 3. 置き場と名前（Claude が最後にやる）

- 一枚絵: `unity/Assets/Resources/Art/leaders/leader_white.png`（88×64・south-east）。
- コマ: `unity/Assets/Resources/Art/leaders/anim/leader_white_{idle,attack,block}_{n}.png`（92×78＝書き出し 92×92 の足元 y=78 より下を切る。足元中央は一枚絵と同じ）。
- アイコン: `unity/Assets/Resources/Art/leaders/leader_white_icon.png`（32×32・ちびの顔の切り出し）。
- 攻撃のコマ送りは `Stage.cs` の FrameDur（0.05/0.08/0.04/0.18）を共用。着弾は CardPlayed の 0.13 秒後＝3コマ目が着弾になるように選ぶ。
- 記録: `docs/pixellab/hinata-v2/`（rotations・animations・metadata・`leader_white_anim.pixellab.json`）と `docs/pixellab-assets.md` のリーダーの節。

## 4. 確認

`STATE="phase=combat;enemy=enemy_probe;leader=leader_white;play=attack;playshots=8;playevery=1" scripts/unity-win.sh shots state` で振りの途中を撮る（`leader=` はチェックポイントのリーダー指定）。

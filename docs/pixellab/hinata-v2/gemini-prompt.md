# ひなた v2 — Gemini 設定画のプロンプト（2026-09-18。`docs/world.md`「ひなたの核」の記号化を英語に写したもの。同日ユーザー「フードかぶらせるようにしてほしい」でフード着用に）

使い方: Gemini（画像生成）にそのまま貼る。候補は2〜4枚出して1枚のシートに並べ、決定稿を `docs/pixellab/hinata-v2/reference-gemini.png` に保存。
次の工程（顎から下を縦0.6に詰めた参考画 → PixelLab Create from Reference 88×64・8方向・south-east）はこのは v2（`docs/pixellab/konoha-v2/`）と同じ。

## メイン（設定画・等身・待機ポーズ）

```
Character design sheet of a young girl, "Hinata", an apprentice lamp-keeper of an underground mana mine, for a Japanese fantasy roguelike game.

STYLE: Japanese anime game illustration in the style of Disgaea / Nippon Ichi character art — bold thick black ink outlines, flat high-saturation cel shading with 2-3 tones, clean and pop, slender proportions. Full body, standing, three-quarter front view facing the viewer's right. Plain cream (#f4ecd6) background, no text, no logo, no watermark.

BODY: petite and slim, short stature, delicate arms. Age about 15-17.

FACE & HAIR: sky-blue eyes, large round anime eyes with two highlights, small mouth. Timid "troubled smile" expression (slightly raised inner eyebrows, gentle smile). Long blonde hair in a single thick braid hanging over one shoulder, soft bangs. She wears the white hood of her robe UP over her head (the hood is part of the robe, edged with a thin brass trim); the braid and bangs come out from under the hood, and her face is fully visible.

CLOTHES (design language: "black iron and brass", a steam-mechanic artisan): an ankle-length white hooded robe with wide sleeves, hood worn up; a large brass clasp on the chest; a brown leather tool belt at the waist with brass fittings (small oil can, a few brass screws, a wound key); black leather bands at both wrists; black iron boots peeking out under the hem. Simple, clean silhouette. NO scarf, NO apron.

PROP: she holds a long pole (taller than herself) with a large brass lantern hanging from its tip. The lantern is brass and black iron with glass panes, and glows with warm orange-amber mana light (NOT blue-green). She holds the pole diagonally in both hands, raising the lantern forward to light the path ahead — a lamp-keeper's working pose, slightly leaning back, a little timid.

Do NOT draw any dolls, companions, or other characters. Single character only.
```

## ネガティブ（使えるなら）

```
scarf, apron, cape, armor, sword, axe, blue lantern light, green lantern light, chibi, super-deformed, watercolor, realistic, painterly, text, extra characters, doll, puppet, wings, halo, nun outfit, cross
```

## 差分（必要な時だけ）

- 表情の別案: `expression: confident smile, chest out` を足すと「人形がいる時の顔」。
- 三面図が欲しい時: `three views: front, side, back, same character` を足す（PixelLab は1枚から8方向を作れるので必須ではない）。
- 背景に世界を出したい時（キービジュアル用・立ち絵には使わない）: `background: a dark mine tunnel with wooden props and faint blue-green mana veins in the rock` を足す。

## 判定のポイント（シートを見る時）

1. 髪＝金の三つ編み一本・目＝空色（緑や琥珀になっていないか）。
2. ローブ＝足首まで・白・**フードをかぶっている**（顔は全部見える・三つ編みはフードの下から出る。修道女のように見えたら真鍮の縁取り・留め具・道具ベルトを強く）。
3. ランタンの光＝暖色（青緑は坑の脈の色なので不可）。竿は自分より長い。
4. 顔＝困り笑い（怯えすぎ・無表情は不可）。
5. 人形が描かれていないこと。

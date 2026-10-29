# リーダーの立ち絵 — ChatGPT への発注書（2026-10-29 品質スプリント #4）

ユーザー裁定（2026-10-29 ask_user）: 立ち絵は **ChatGPT で作る**（絵師への外注はその後。来たら差し替え）。
対象は絵のある 4 人＝**このは・みぞれ・ひばな・ひなた**。残り 11 人は画面で「封」の札（絵は要らない）。

## 1. 置き場と形（コードが読む約束）

| 項目 | 値 |
|---|---|
| 置き場 | `unity/Assets/Resources/Art/portraits/<leaderId>.png`（`leader_green` `leader_blue` `leader_red` `leader_white`） |
| 形式 | **PNG・背景は透明**（白や単色の背景は不可。切り抜きが要る） |
| 大きさ | **縦長 2:3（1024×1536 以上。1536×2304 なら理想）**。画面では高さ 800〜840 px に縮めて出す |
| 入れ方 | 全身・**足の先まで**入れる。頭の上と足の下に 3〜5% の余白。体は枠の高さの **85〜90%** |
| 向き | **斜め右向き（3/4）**＝画面の右にある名前の板・難易度の方を見る。正面でもよいが真横は不可 |
| 立ち方 | 待機（戦闘のちびと同じ気分）。このは＝大斧を右肩に担ぐ／みぞれ＝杖を立てて持つ／ひばな＝魔導書を抱える／ひなた＝ランタンの竿を斜めに掲げる |
| 読み込み | 絵が無ければ今のドット（戦闘のちび）を拡大して出す。Point フィルタではなく **Bilinear**（なめらかな絵なので）。`.meta` は Unity が作る |

## 2. 絵柄（4 人共通。プロンプトの頭に付ける）

- **日本のかわいいデフォルメ**（オクトラ正統ではなく、`docs/world.md` このは v2 の「ディスガイア系のポップ」）: 太い墨線・高彩度・セル塗り＋少しの影。**頭身は立ち絵なので 5〜6**（戦闘のちびは 2.5〜3。立ち絵は高め＝確定済みルール「絵柄の頭身」）。
- 意匠は「**黒鉄と真鍮**」: 金具・留め具・歯車・ランタンは真鍮（#c99a3a 系）、道具の鉄は黒鉄。マナの光は**青緑**（#3aa79b）、ひなたのランタンだけ**暖色**。
- 背景なし・影なし・文字なし・装飾枠なし。**萌え絵・写実・AI 臭いツヤ（光沢の塗り・過剰なグロー）は避ける**（ユーザーは AI 臭さに強い拒否反応）。
- 参考として、戦闘のちび（`unity/Assets/Resources/Art/leaders/leader_<色>.png`）を添付して「このキャラの立ち絵に」と頼むと配色がそろう。

英語の共通の頭:
> Full-body standing character illustration, Japanese anime style with bold clean ink outlines and vivid cel shading, cute stylized proportions about 5.5 heads tall, three-quarter view facing slightly right, relaxed idle pose, feet fully visible, **transparent background, no shadow on the ground, no text, no frame**. Design language: black iron and brass fittings, mana light is teal (except warm lantern light). Not photorealistic, no glossy airbrush rendering.

## 3. 一人ずつ（`docs/world.md` の核をそのまま）

### このは（leader_green）— 大樹の坑匠
栗色の跳ねたショート＋大きなアホ毛、緑の目（丸く大きい・ハイライト 2 つ）、小さな笑み・薄い頬の赤み。小柄で細身、手と腕はしっかり。
深緑のチューブトップ（黒の縁・胸に真鍮の留め具。へそ出し・肩と腕は素肌）、黒革のチョーカー、左上腕に真鍮の腕輪、茶革のグローブ（右は短い手袋・左は長い手甲）、黒のショートパンツに太い茶革のベルトと大きな真鍮のバックル（右腰に圧力計とレンチ・左腰に歯車の飾りの道具袋）、左脚だけ太ももまでの黒ソックス（真鍮の歯車のガーター）、茶革の編み上げブーツ（真鍮の歯車の留め具）。
**自分より大きな魔導の大斧**を右肩に担ぐ: 黒鉄の片側一枚刃（鋲打ちの真鍮の縁・背に短い棘）、付け根に大きな真鍮の歯車と中心の緑の宝石、緑の紐と真鍮の帯を巻いた黒鉄の柄、末端に真鍮の錘。
却下＝フード・首の布・アームカバー・前掛け・黄緑・寡黙な顔。

> A small slender girl, chestnut spiky short hair with a big ahoge, large round green eyes with two highlights, small confident smile. Dark green tube top with black trim and a brass clasp, bare shoulders, black leather choker, brass armband on the left upper arm, brown leather gloves (short on the right, long gauntlet on the left), black shorts with a wide brown belt and a big brass buckle, pressure gauge and wrench on the right hip, gear-decorated pouch on the left hip, one thigh-high black sock on the left leg with a brass gear garter, brown laced boots with brass gear clasps. She shoulders an oversized magitech great axe on her right shoulder: single black-iron blade with riveted brass edge and short spikes on the back, a large brass gear with a green gem at the base, black iron haft wrapped with green cord and brass bands, brass counterweight.

### みぞれ（leader_blue）— 潮読みの導師
（核の文書は無し。**Codex の絵が正**＝`Art/leaders/leader_blue.png` を添付して合わせる）
長い銀白の髪（毛先が淡い青）、青い目、大きなつば広のとんがり帽子（生成りに青の帯）、青のケープと白の服、茶革のブーツ、**先に青い水晶の玉を留めた木の杖**。落ち着いた顔。

> A calm girl with long silver-white hair tinted pale blue at the tips, blue eyes, a wide-brimmed pointed witch hat in cream with a blue band, a blue cape over a white dress with brass fittings, brown leather boots, holding a tall wooden staff topped with a blue crystal orb in a brass claw. Cool blue and white palette with brass accents.

### ひばな（leader_red）— 燼の工房の赤魔術師
黒髪ボブ・控えめな赤い目・赤い短マント（フードは下ろす）・生成りの高襟の胴衣・炭色の腰当てと革帯に小さな袋・赤い割れスカート・炭色のタイツ・赤い縁の革靴。小型の赤茶の魔導書（赤金の菱形の留め金 1 つ）を体の手前側に抱え、空いた手は下げる。帽子・杖・武器は無し。

> A girl with a black bob haircut and subtle red eyes, a short red cape with the hood down, an off-white high-collared tunic, charcoal waist guard and leather belt with a small pouch, red slit skirt over charcoal tights, leather shoes with red trim. She holds a small reddish-brown grimoire with a single red-gold diamond clasp against her front, the other hand relaxed at her side. No hat, no staff, no weapon.

### ひなた（leader_white）— 光耀の灯匠
金髪の三つ編み（長め・一本）、空色の目、**足首までの長い白ローブ、フードをかぶる**（フードは真鍮の細い縁取り・顔は全部見え三つ編みはフードの下から出る）。胸に真鍮の大きな留め具・腰に真鍮の道具ベルト・手首に黒革のバンド・黒鉄の小物。**大きな真鍮のランタンを提げた長い竿**を斜めに掲げて前を照らす（ランタンの中は**暖色**の光）。困り笑い。小柄で細身。人形は描かない。

> A small timid-looking girl with a long single blonde braid and sky-blue eyes, wearing an ankle-length white robe with the hood up (thin brass trim on the hood, face fully visible, braid coming out from under the hood), a large brass clasp on the chest, a brass tool belt, black leather wristbands and small black-iron tools. She holds a long pole diagonally with a big brass lantern hanging from its tip, the lantern glowing warm amber, lighting the way ahead. Nervous apologetic smile. No dolls or companions.

## 4. 受け取りの確認（受け取ったら Claude が見る）

- 透明か（角の画素の α）・足まで入っているか・向き・縦横比。
- 4 人の線の太さと頭身がそろっているか（ばらつけば同じ会話で「この 4 枚の線と頭身をそろえて」と頼む）。
- 画面の仮当て: `STATE="phase=setup;leader=leader_green" scripts/unity-win.sh shots state`（実装後に用意する撮影の口）。

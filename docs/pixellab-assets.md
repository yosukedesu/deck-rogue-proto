# PixelLab 素材一覧（発注書）

`npx tsx scripts/pixellab-sheet.ts` で `src/data` から生成。**置き場と名前は `Assets/Resources/Art/<種別>/<id>.png`**。
寸法はドット絵の実寸（画面には整数倍で置く。基準解像度 1920×1080 では 3〜4 倍）。背景は透過 PNG。パレットは種別ごとに揃える。
プレースホルダー（コード生成）が同じ名前で出ているので、1枚ずつ差し替えて見比べられる。

## 寸法と枚数の要約

| 種別 | フォルダ | 実寸 (px) | 枚数 | 備考 |
|---|---|---|---|---|
| 敵（通常） | enemies | **64×64**（4倍で256px） | 63 | 2026-09-07 ユーザー裁定「オクトラのドット数がいい・128は微妙」: 密度をオクトラ相当（キャラ高 40〜48ドット・1ドット＝画面4px）に固定。API のアニメは 64 が本命サイズ。待機は `<id>_idle.png` のシート（後で対応） |
| 敵（エリート） | enemies | 80×80（4倍で320px） | 13 | 密度は同じで一回り大きい |
| 敵（幕ボス） | enemies | 96×96（4倍で384px） | 8 | 密度は同じで大きく。第2形態は色替えで代用 |
| リーダー（戦闘内ちび） | leaders | **64×64**（4倍で256px） | 15 | 頭身低め（確定済みルール表「絵柄の頭身」）。立ち絵は人間の絵師へ外注 |
| リーダー（アイコン） | leaders | 32×32 | 15 | `<id>_icon.png`。上部バーの左端に2倍（このは済＝ちびの顔の切り出し。2026-09-11） |
| レリック | relics | 32×32 | 39 | **済（2026-09-11 一括）** |
| 紙の9スライス | ui | 52×52 / 48×48 / 32×32 / 40×40 | 4 | `paper_card.png`（カード。角丸14・border16）／`paper_panel.png`（頁・吹き出し。角丸12・border14）／`paper_tag.png`（札。角丸8・border10）／`paper_button.png`（ボタン。下に厚み。角丸10・border12）。クリーム色の紙＋鉛筆の二重線。**2026-09-07 の絵本調決定でタイプ×色の枠は廃止**——タイプは左上の「しおり」、色は台紙の縁で出す |
| カードのしおり（タイプ） | ui | 16×24 | 4 | `bookmark_<type>.png`。物理=砂色／呪文=藤色／リアクション=青緑／置物=蜂蜜色。コストの数字は文字で乗せる |
| 役割のにじみ（剣・盾・返し） | ui | 32×20 | 3 | `blob_<dmg|block|counter>.png`。水彩のにじみ形。カード左下＝与ダメ、右下＝ブロック。数字は文字 |
| カード挿絵 | cards | **80×48** | 412（最後） | `<id>.png`（例 `green_strike.png`）。**2倍で 160×96、紙の台紙（168×104）に貼る**。絵が無い札はタイプの紋章 16×16 を3倍 |
| レア度の宝石 | ui | 12×12 | 3 | `gem_common/uncommon/rare.png`（済。32 で生成し 12 に詰める） |
| コスト玉 | ui | 26×26 | 1 | `cost_orb.png`（済。2倍で 52 ＝ カードの玉。数字は文字で乗せる）。2026-09-16 カラーテーマ: エナジー＝真鍮（ユーザー「エナジー表記は黄色系がいい」）。**いまの png は PixelLab の原本（`docs/pixellab/ui/cost_orb_pixellab.png`）の彩度を2割落として色相を真鍮側へ半歩回した派生**（陰影は原本のまま）＝玉・輪・ボタン・目盛りが一つの金属に見えるように。描き直す時は真鍮 #c99a3a を基準色に |
| 状態アイコン | icons | **32×32**（API の下限） | 下表 | 32px 以上の置き場（焚き火の札・マップ・浮き絵）で絵。16px のインライン（カード文面・バフ欄）は `Theme.IconArt` のビットマップのまま。済 22＋紋章4（2026-09-11） |
| 意図アイコン | icons | **32×32** | 13 | `intent_<kind>.png`（吹き出しに2倍＝64）。済 13/13（2026-09-11） |
| マップのノード | map | 32×32 | 8 | `node_<type>.png`（通常1倍・ボス2倍）。済 8/8 |
| 背景（幕） | bg | **384×216**（API 上限 400） | 3 | `act1/act2/act3.png`。舞台の一番奥の板（幕2/3も）。済 3/3 |
| 焚き火・工房・ショップ・イベントの情景 | scenes | **240×132**（4の倍数） | 4 | `campfire/workshop/shop/event.png`。見出しの左の窓に2倍／イベントは挿絵つきの頁。済 4/4 |
| UI 部品 | ui | 各種 | 約10 | パネル9スライス（`panel.png` 24×24 角6）・ボタン3態（`btn_normal/hover/pressed.png` 24×24 角6）・HPバー枠 |

## 状態アイコン（icons/16px）

| 名前 | 用途 |
|---|---|
| sword | ダメージ／攻撃 |
| shield | ブロック |
| heart | HP・回復 |
| energy | エナジー |
| draw | ドロー |
| growth | 成長 |
| momentum | 勢い |
| pierce | 貫通 |
| burn | 延焼 |
| ice | 氷壁 |
| aether | 霊気 |
| exposed | 急所 |
| weak | 弱体 |
| frail | 虚弱 |
| vulnerable | 脆弱 |
| restrain | 拘束 |
| haze | 霞み |
| weight | 重り |
| strength | 筋力 |
| armor | 装甲 |
| thorns | とげ |
| regen | 再生 |
| artifact | アーティファクト |
| exhaust | 消滅 |
| retain | 保持 |
| set | 伏せ |
| gold | ゴールド |
| cast | 詠唱数 |
| permanent | 置物 |
| reaction | リアクション |

## 意図アイコン（icons/24px・`intent_<kind>.png`）

attack / defend / buff / rally / heal / hex / destroy-set / destroy-token / steal-gold / flee / mill / rest / hatch

## 敵（enemies/）

| id | 名前 | 区分 | 寸法 |
|---|---|---|---|
| enemy_wide_power | うねる獣 | 通常 | 64×64 |
| enemy_probe | 探り屋 | 通常 | 64×64 |
| enemy_set_wary | 用心深い影 | 通常 | 64×64 |
| enemy_set_breaker | 罠壊し | 通常 | 64×64 |
| enemy_brute | 脳筋オーガ | 幕ボス | 96×96 |
| enemy_turtle | 眠たがりの大亀 | 幕ボス | 96×96 |
| enemy_hexer | 泥投げの妖術師 | 通常 | 64×64 |
| enemy_wolf | 牙嵐の狼 | 通常 | 64×64 |
| enemy_moss | 苔まといの主 | 通常 | 64×64 |
| enemy_joker | 嘲る道化 | 通常 | 64×64 |
| enemy_drummer | 鼓吹きコボルト | 通常 | 64×64 |
| enemy_warden | 刻限の門番 | 幕ボス | 96×96 |
| enemy_thorn_squirrel | 針毛の栗鼠 | 通常 | 64×64 |
| enemy_thief | こそ泥ゴブリン | 通常 | 64×64 |
| enemy_bomber | 火薬樽かつぎ | 通常 | 64×64 |
| enemy_moss_healer | 苔の癒し手 | 通常 | 64×64 |
| enemy_axe_ogre | 大振りの斧鬼 | 通常 | 64×64 |
| enemy_shell_guard | 石殻の番人 | 通常 | 64×64 |
| enemy_apprentice_colossus | 見習い巨像 | 通常 | 64×64 |
| enemy_mimic_imp | 物真似の子鬼 | 通常 | 64×64 |
| enemy_cultist | 囁きの狂信者 | 通常 | 64×64 |
| enemy_slug | 酸吐きの蛞蝓 | 通常 | 64×64 |
| enemy_whetstone_colossus | 砥石の巨像 | 通常 | 64×64 |
| enemy_mimic_jester | 物真似の道化 | 通常 | 64×64 |
| enemy_cinder_imp | 焚きつけのインプ | 通常 | 64×64 |
| enemy_rock_beetle | 岩皮の甲虫 | 通常 | 64×64 |
| enemy_big_slime | 大苔スライム | 通常 | 64×64 |
| enemy_moss_slime | 苔スライム | 通常 | 64×64 |
| enemy_moss_spawner | 苔の産み手 | 通常 | 64×64 |
| enemy_elite_sergeant | 鞭打ちの鬼軍曹 | エリート | 80×80 |
| enemy_elite_sentry | 歩哨 | エリート | 80×80 |
| enemy_elite_iron_egg | 眠れる鉄卵 | エリート | 80×80 |
| enemy_elite_slaver | 鎖持ちの奴隷商 | エリート | 80×80 |
| enemy_elite_stab_book | 刺突の書 | エリート | 80×80 |
| enemy_elite_giant_face | 巨面 | エリート | 80×80 |
| enemy_elite_gold_raven | 金羽の大鴉 | エリート | 80×80 |
| enemy_elite_mirror_djinn | 写し身の魔人 | エリート | 80×80 |
| enemy_elite_doom_chanter | 終焉の唱い手 | エリート | 80×80 |
| enemy_elite_devourer | 大喰らいの蟲 | エリート | 80×80 |
| enemy_elite_owl | 読み手の梟 | エリート | 80×80 |
| enemy_elite_deathless | 不滅の騎士 | エリート | 80×80 |
| enemy_shield_squire | 盾持ちの従士 | 通常 | 64×64 |
| enemy_crossbow_archer | 弩弓の射手 | 通常 | 64×64 |
| enemy_bond_wolf | 双牙の狼 | 通常 | 64×64 |
| enemy_mud_lump | 泥まとうもの | 通常 | 64×64 |
| enemy_elite_husk_3 | 不滅の骸兵・参 | エリート | 80×80 |
| enemy_elite_husk_2 | 不滅の骸兵・弐 | 通常 | 64×64 |
| enemy_elite_husk_1 | 不滅の骸兵・壱 | 通常 | 64×64 |
| enemy_brood_toad | 蟲抱えの蛙鬼 | 通常 | 64×64 |
| enemy_broodling | 蠢く幼体 | 通常 | 64×64 |
| enemy_brood_raptor | 抱卵の走竜 | 通常 | 64×64 |
| enemy_raptor_egg | 走竜の卵 | 通常 | 64×64 |
| enemy_raptor_chick | 走竜の仔 | 通常 | 64×64 |
| enemy_mourn_beast | 弔いの獣 | 通常 | 64×64 |
| enemy_maw_hunter | 大顎の狩人 | 通常 | 64×64 |
| enemy_sludge_berserker | 汚泥の大暴れ | 通常 | 64×64 |
| enemy_mudling | 小泥 | 通常 | 64×64 |
| enemy_gaping_maw | 裂け口の獣 | 通常 | 64×64 |
| enemy_cog_construct | 歯車の箱兵 | 通常 | 64×64 |
| enemy_spore_cap | 胞子吹きの茸 | 通常 | 64×64 |
| enemy_strangler_serpent | 巻きつく大蛇 | 通常 | 64×64 |
| enemy_snap_fruit | 噛みつき果実 | 通常 | 64×64 |
| enemy_vine_walker | 蔦纏いの歩き木 | 通常 | 64×64 |
| enemy_iron_clam | 鉄殻の溝貝 | 通常 | 64×64 |
| enemy_sludge_spider | 汚泥紡ぎの蜘蛛 | 通常 | 64×64 |
| enemy_axe_automaton | 絡繰の斧兵 | 通常 | 64×64 |
| enemy_axe_automaton_reboot | 絡繰の斧兵・再起 | 通常 | 64×64 |
| enemy_thunder_globe | 雷球の巨頭 | 通常 | 64×64 |
| enemy_frog_knight | 蛙の騎士 | 通常 | 64×64 |
| enemy_biting_scroll | 噛みつく巻物 | 通常 | 64×64 |
| enemy_lost_soul | 失せ人の霊 | 通常 | 64×64 |
| enemy_forgotten_soul | 忘れ人の霊 | 通常 | 64×64 |
| enemy_devoted_sculptor | 献身の彫師 | 通常 | 64×64 |
| enemy_chomper | 金切り顎 | 通常 | 64×64 |
| enemy_scald_gnat | 灼き虻 | 通常 | 64×64 |
| enemy_kin_priest | 血族の司祭 | 幕ボス | 96×96 |
| enemy_kin_follower | 血族の踊り手 | 幕ボス | 96×96 |
| enemy_crab_crusher | 巨蟹の砕き腕 | 幕ボス | 96×96 |
| enemy_crab_cannon | 巨蟹の撃ち腕 | 幕ボス | 96×96 |
| enemy_chimera_1 | 蘇る合成獣・一の相 | 幕ボス | 96×96 |
| enemy_chimera_2 | 蘇る合成獣・二の相 | 通常 | 64×64 |
| enemy_chimera_3 | 蘇る合成獣・三の相 | 通常 | 64×64 |
| enemy_burrow_worm | 潜行する大地虫 | 通常 | 64×64 |
| enemy_bowl_bug | 転がる岩虫 | 通常 | 64×64 |
| enemy_nemesis_wraith | 因縁の亡霊 | 通常 | 64×64 |

## リーダー（leaders/）

| id | 名前 | 色 |
|---|---|---|
| leader_green | このは | green |
| leader_blue | みぞれ | blue |
| leader_red | ひばな | red |
| leader_izzet | らいこ | blue+red |
| leader_white | ひなた | white |
| leader_black | とばり | black |
| leader_azorius | なぎ | white+blue |
| leader_rakdos | あかね | black+red |
| leader_gruul | いぶき | red+green |
| leader_selesnya | わかば | green+white |
| leader_orzhov | あかり | white+black |
| leader_golgari | くろは | black+green |
| leader_boros | あさひ | red+white |
| leader_simic | しずく | green+blue |
| leader_dimir | かすみ | blue+black |

## レリック（relics/ 32×32）

| id | 名前 | 現在の絵文字 |
|---|---|---|
| relic_thorn_crown | 茨の冠 | 👑 |
| relic_sage_scroll | 賢者の巻物 | 📜 |
| relic_swift_boots | 早駆けの靴 | 👢 |
| relic_shield_shard | 大盾の欠片 | 🛡️ |
| relic_vanguard_shield | 先手の盾 | ⛨ |
| relic_iron_heart | 鉄の心臓 | 🫀 |
| relic_hunters_boon | 狩人の恵み | 🏹 |
| relic_collectors_bag | 収集家の鞄 | 🎒 |
| relic_deep_breath | 深呼吸の香 | 🕯️ |
| relic_growth_seed | 成長の種 | 🌱 |
| relic_swift_sash | 韋駄天の帯 | 🎽 |
| relic_oldroot_cup | 古根の杯 | 🏆 |
| relic_raptor_eye | 猛禽の眼 | 👁️ |
| relic_merchant_scale | 商人の秤 | ⚖️ |
| relic_smith_whetstone | 鍛冶の砥石 | 🪨 |
| relic_talisman_pouch | 符師の懐 | 👝 |
| relic_quiet_bell | 静かな鈴 | 🔔 |
| relic_crown_shard | 王冠の欠片 | 👑 |
| relic_cursed_key | 呪いの鍵 | 🗝️ |
| relic_philosopher_stone | 賢者の石 | 💎 |
| relic_slaver_collar | 鎖の首輪 | ⛓️ |
| relic_whetstone_chip | 砥石の欠片 | 🪨 |
| relic_herb_pouch | 薬草袋 | 🌿 |
| relic_old_purse | 古い財布 | 👛 |
| relic_momentum_whip | 勢いの鞭 | 🪢 |
| relic_reading_glasses | 読みの眼鏡 | 👓 |
| relic_retrieve_cord | 回収の紐 | 🧵 |
| relic_mortar | 薬研 | ⚗️ |
| relic_loot_bag | 戦利品袋 | 🎒 |
| relic_great_tree_heart | 大樹の心 | 🌳 |
| relic_steadfast_root | 不動の根 | 🌱 |
| relic_harvest_sickle | 収穫の鎌 | 🌾 |
| relic_membership_card | 会員証 | 🪪 |
| relic_golden_boots | 金の靴 | 👢 |
| relic_removal_chisel | 除去の鑿 | 🔩 |
| relic_witch_scale | 魔女の秤 | ⚖️ |
| relic_black_star | 黒星の欠片 | 🌟 |
| relic_tiny_house | 小さな家 | 🏠 |

## 作成の優先順（2026-09-07 ユーザー「イメージを固めるために PixelLab で作ってみたい」）

原則: **毎回の戦闘で目に入るもの → 毎ターン目に入る小物 → カード面の統一 → ラン画面 → 残りの敵 → カード挿絵 → 背景**。
最初の1〜2バッチで「絵本の紙の UI × 本物のドット絵」の相性を確かめ、合わなければ紙側（貼り絵の縁の太さ・にじみの色）を直す。
置き場は `unity/Assets/Resources/Art/<種別>/<id>.png`（同名のプレースホルダーが差し替わる）。確認は `scripts/unity-win.sh shots battle 6` か Hub で Play。

| 順 | 何を | 枚数 | 寸法 | 理由 |
|---|---|---|---|---|
| 1 | **このは（戦闘内ちび）** `leaders/leader_green.png` | 1 | 64×64 | 主役。全戦闘に出る。**大樹の狩人（2026-09-08 改称）**＝元気な見習い狩人・焦げ茶のおさげ・緑の目・深緑のフード付き外套・橙の首巻き・自分より大きな両手斧（木の長柄＋鋼の刃）を肩に担ぐ。敵と同じ紙の肌で、暖色は首巻きだけ。発注書 `docs/pixellab/leader-green-hunter-v2.json`（髪型4×服2×2シードの比較から D_おさげ×1_外套 seed11。初版 `leader-green-hunter.json` は赤茶ポニーテール＝差し替え済み） |
| 2 | **幕1・弱プールの敵** 探り屋 `enemy_probe`／酸吐きの蛞蝓 `enemy_slug`／小泥 `enemy_mudling`／汚泥紡ぎの蜘蛛 `enemy_sludge_spider`／鉄殻の溝貝 `enemy_iron_clam`／噛みつき果実 `enemy_snap_fruit` | 6 | 96×96 | 最初の3戦で出る顔。ここまでで「初戦の絵」が全部本物になる |
| 3 | **意図アイコン** `icons/intent_<kind>.png`（attack / defend / buff / rally / heal / hex / destroy-set / destroy-token / steal-gold / flee / mill / rest / hatch） | 13 | 24×24 | 敵の頭上の吹き出しに毎ターン出る。線は墨1色＋役割の色1つ |
| 4 | **状態アイコン・上位8** sword（攻撃/筋力）・shield（ブロック）・growth（成長）・momentum（勢い）・exposed（急所）・burn（延焼）・set（伏せ）・heart（HP） | 8 | 16×16 | カード本文・バフ欄・剣と盾の札に毎ターン出る（残りは表の後で） |
| 5 | **カード面の部品** 紋章4 `icons/crest_<type>.png`（16×16→3倍）・しおり4 `ui/bookmark_<type>.png`（16×24）・にじみ3 `ui/blob_<dmg|block|counter>.png`（32×20）・星 `icons/star.png`（16×16） | 12 | 各 | 全カードに出る。ここで手札の統一感が決まる |
| 6 | **マップのノード** `map/node_<type>.png` | 8 | 32×32 | ランで最も長く見る画面 |
| 7 | **幕1で出やすいレリック**（成長の種・古根の杯・商人の秤・読みの眼鏡・早駆けの靴・鉄の心臓・大樹の心・薬草袋・砥石の欠片・古い財布） | 10 | 32×32 | 上部の札の丸に入る。残り28はその後 |
| 8 | **リーダーのアイコン** `leaders/<id>_icon.png` | 15 | 32×32 | タイトルの15人。ちびの顔を切り出す形でよい |
| 9 | **幕1・本帯の敵** うねる獣・針毛の栗鼠・見習い巨像・囁きの狂信者・泥まとうもの・物真似の子鬼・こそ泥ゴブリン・裂け口の獣・歯車の箱兵・蔦纏いの歩き木・巻きつく大蛇・胞子吹きの茸 | 12 | 96×96 | 幕1を一周できる |
| 10 | **幕1のエリートとボス** 鞭打ちの鬼軍曹・歩哨・金羽の大鴉・大喰らいの蟲（128×128）／脳筋オーガ・血族の司祭と踊り手（160×160） | 4＋2組 | 128 / 160 | 山場の顔 |
| 11 | **残りのリーダーちび** | 14 | 96×96 | 色ごとに1人ずつ（みぞれ・ひばな・ひなた・とばり）→ ギルド10人 |
| 12 | **カード挿絵・緑91種** `cards/<id>.png`（2026-09-08 ユーザー「緑のカード画像も作成して」＝発注書 `docs/pixellab/cards-green.json`。挿絵は背景つきの一枚絵で紙の台紙に貼る。夜の森・緑と苔の差し色・文字と枠は禁止語） | 91 | 80×48 | 「紙の台紙に貼った絵」が成立するかをここで判定。良ければ緑のコモン25 → 全412 |
| 13 | **幕2・幕3の敵**（残り約45）・エリート8・ボス6 | 約60 | 96/128/160 | 幕1で絵の文法が固まってから |
| 14 | **背景と情景** `bg/act1..3.png`（480×270→4倍）・`scenes/campfire|workshop|shop|event.png` | 7 | 480×270 / 240×135 | 今の水彩の夜で成立しているので最後 |

### PixelLab で作るときのメモ（2026-09-07 改定: 向きは斜め前 3/4・絵柄はオクトパストラベラー風の美麗ドット）

- **絵柄の要件（ユーザー指定）**: オクトパストラベラー（HD-2D）風の美麗ドット。陰影を細かく（detailed shading）、輪郭は黒1色でなく暗色の選択的輪郭（selective outline）、面は多階調。ゆるかわの造形（丸い頭・大きい目）は保ちつつ、描き込みで格を出す。
- **向きと視点**: **斜め前 3/4**。PixelLab では direction を **south-east（リーダー・右向き）／south-west（敵・左向き）**。east/west は横顔になるので使わない。**view は `low top-down`（2026-09-08 ユーザー「マップの角度と敵の向きが合っていない」）**＝舞台は見下ろしのカメラなので、`side`（水平に見た絵）だと地面の角度とずれる。オクトラのスプライトも少し上から見た絵。出来上がった絵の向きを変えるだけなら **Rotate**（from east → to south-east）。
- **向きの検算（2026-09-08 ユーザー「向きが逆じゃない？」）**: pixflux は `direction: south-east` と「facing right」を書いても左向きに出ることがある（このは seed61 は左向き）。採用前に肌色の重心が体の重心より右か（右向き）を測り、逆なら PNG を左右反転する（`.pixellab.json` に `postprocess` を記す）。リーダーは右向き（敵へ）・敵は左向き。
- **寸法はこの表のとおり**（通常とリーダー 64・エリート 80・ボス 96。どれも**4倍表示＝1ドットが画面4px**でオクトラと同じ密度。アイコンは 16/24/32）。キャラの実体は 64 キャンバスの中で高さ 40〜48 ドットが目安（オクトラは 18×32 前後）。画面には整数倍で置くので、キャンバスいっぱいに使わず、敵は下端を地面に接し左右に 6〜10px の余白。
- **頭身**: 戦闘内ちびは 2.5〜3 頭身（オクトラの街の人物に近い）。塔の深層（幕3）ほど不穏に。
- **敵の絵柄＝ホロウナイトの寂しさ×ゴースト（2026-09-08 ユーザー「もっとホロウナイトに似せて。カラフルじゃなく寂しげな良さ」→「虫よりゴースト的なお化け」。「可愛さ」の解釈〔丸くてカラフルなマスコット〕と虫の解釈は却下）**: 墨黒・骨白・くすんだ青灰だけの低彩度（アクセントは薄い青緑1色まで）、顔は滑らかな白い仮面に空洞の黒い目、口は無いか小さい、体は下に向かって靄に溶ける幽霊の形（脚なし・浮遊・細い腕）、静かで寂しげ。笑顔・頬の赤み・鮮やかな色・虫の脚や触角は negative へ。**顔の装置を変更（2026-09-08 ユーザー「ホロウナイトすぎる、パクリになる」→3択で「灯りの目」）**: 白い仮面と空洞の眼窩をやめ、顔も仮面も無い暗い体に灯りの目（炭火色）が2つだけ。素材は森のもの（苔・樹皮・藁・粘土＝塔に住み着いた森の付喪神）、色は墨＋苔＋黄土＋生成りで目だけが暖色。寂しさは静かな姿勢と灯りの小ささで出す。土台は `docs/pixellab/enemies-lantern.json`。シェーダは暖色で明るいドットを環境光で暗くせずブルームに乗せる（`ember`）。**体の形は名前の生き物そのもの（同日ユーザー「敵のデザイン形状を名前に合ったものにして」）**＝蜘蛛は8本脚、果実は葉つきの丸い実、蛞蝓は長い体。「靄に溶ける体」の指定は形を全部同じにしたので撤回。人型（フード・外套）は狂信者・探り屋・こそ泥以外で禁止語。プロンプトの土台は `docs/pixellab/enemies-named.json` の `style`（前段の `enemies-ghost.json`＝幽霊版・`enemies-hollow.json`＝虫版・`enemies-cute.json`＝可愛い版は記録として残置）。
- **敵の絵柄・確定＝2系統（2026-09-10 世界観改稿で書き直し。旧「②水彩絵本×③月明かり」＝2026-09-08 ユーザー裁定「2と3」の見た目はそのまま引き継ぎ、月光の語だけ脈のマナに差し替えた。上の灯りの目・仮面の記述は経緯として残置＝現行ではない）**。
  系統の割り当ては `docs/pixellab/enemy-kinds.json`（84体。beast 53／machine 20／hybrid 6／revenant 5）。
  - **獣（beast）＝露頭でマナを浴びて育った生き物。** `style` は「gentle watercolor picture book illustration of a pale creature that grew up beside a glowing ore vein deep in the ground, body of soft pale grey-white moss-like fur faintly glowing from within, tiny face with two small round black bead eyes looking slightly down and no mouth, plain cheeks with no blush marks, serene quiet and a little lonely, palette of cream paper, pale silver, faded sage, dusty blue-grey and ink, thin dark ink outline, soft shading like watercolor washes, low contrast」＋ `medium shading`。**光る理由が変わっただけで絵は同じ**なので、生成済みの絵は一枚も無駄にならない。発注書 `docs/pixellab/enemies-act1-beasts.json`（22体）
  - **古機（machine）＝野生化した古代の採掘機械。金属質にして獣と質感を分ける（2026-09-10 ユーザー裁定）。** `style` は「ancient mining automaton that was never switched off, still working in a buried city, body of tarnished pale metal plates and worn stone with visible rivets, brass cogs and a taut cable, hard straight edges and crisp metallic highlights, thin dark ink outline, one small pale glowing lens where a face would be, no mouth, no fur, no animal features, cold patient and a little sad, palette of cream paper, tarnished silver, weathered brass, dusty blue-grey slate and ink, low contrast」＋ `detailed shading`（硬いハイライトを出す）＋ negative に `fur, moss, soft fuzzy texture, animal, beast, watercolor wash, pastel, cute mascot` を追加（獣の画材へ流れるのを防ぐ）。発注書 `docs/pixellab/enemies-act1-machines.json`（3体）
  - **hybrid＝生き物に古代の部品を継いだもの**（蘇る合成獣・蛙の騎士・従士と射手）は獣の画材で描き、継ぎ手だけ金属のハイライトを乗せる。**revenant＝坑で消えた者の名残**（失せ人／忘れ人／因縁の亡霊／用心深い影／写し身の魔人）は獣の画材で輪郭を薄く。どちらも幕2/3の発注時に style を確定する
  - 共通: `low top-down`・`south-west`・`selective outline`・`highly detailed`・guidance 9（API の shading enum は flat/basic/medium/detailed/highly detailed。`soft shading` は 422）。種は文頭で言い切る（`a fat SLUG: …`）。**人型は描かない**（探り屋＝梟・狂信者＝蟇蛙・こそ泥＝袋に潜む小獣・歩哨＝石灯籠・鬼軍曹とオーガ＝猪面の獣・血族の司祭＝鷺・踊り手＝蛾）
  - **語の教訓**: 「plush」は熊・兎、「spirit / goblin / sneaking / hugging」は人型の子ども、「mask」は人型の体を呼ぶ。頬の赤みは negative では消えず positive の「plain cheeks with no blush marks」でだけ減る。bitforge の style_image は形まで写す（55 で全員が果実、28 でも半分）ので参考画像は使わず言葉で揃える
  - **手順**: 各2シード（23/41）を scratchpad に出して良い方を `Art/enemies` へコピー、人型・熊兎に流れた札は種を差し替えて3シード（7/57/83）で作り直す。幕2/3は同じ2つの style で種の記述だけ書く
- **このは v2（2026-09-16）**: 絵柄をディスガイア系のポップに変更（太い墨線・高彩度・スレンダーな 2.5〜3 頭身・「オクトラ正統」は廃止）。設定（`docs/world.md` このはの核）から記号化し、Gemini で等身の設定画 → 顎から下だけ縦 0.6 倍に詰めて 2.5 頭身の参考画（`docs/pixellab/konoha-v2/reference-chibi.png`。Gemini は参照の比率を守るので、文で「3頭身に」と頼んでも下がらない＝比率は画像側で作ってから渡す）→ PixelLab Web の **Create from Reference**（Humanoid・v3・Low Top-Down。Description は `Art/leaders/leader_green.pixellab.json` の prompt）→ 88×64・8方向（`docs/pixellab/konoha-v2/rotations/`）→ south-east を `leader_green.png`、south の顔を `leader_green_icon.png`（32×32）。向きの検算は「肌の重心」だと斧の重みで狂う（斧が右に張り出す絵）ので目視。アニメ（2026-09-16）: Characters ページの Add Animation で idle＝テンプレ `breathing-idle`、attack／block＝v3 の文（コマ数 4 と頼んでも 17／9 コマで出るので、こちらで4コマに間引く）。書き出しは 92×92・足元 y=78。hurt は作らず Presenter の揺れで代える。
- **リーダーの絵柄＝日本のかわいいデフォルメ（2026-09-11 ユーザー「日本のキャラデザのようなかわいいデフォルメ。今のは安い中国かアメリカのデザインっぽい」）**: 「日本らしく」は小道具（鉞・勾玉・和装）ではなく**絵柄**の話だった。絵柄8案（ディスガイア型／元気SD／FEHちび／日本一SD／オクトラ正統／ライブアライブ／絵本SDアニメ顔／HD-2Dセル顔）から **5「オクトラ正統」** を採用: `Japanese JRPG pixel art character sprite in the HD-2D style of Octopath Traveler and Live A Live, kawaii anime chara-design, 3 heads tall super-deformed proportions with a large round head and a slim small body, soft round anime face, big sparkling green anime eyes with two white highlights each, tiny dot nose, small closed smiling mouth, ... clean cel shading in three flat tone steps, thin dark selective outline, subdued Octopath palette` ＋ negative に `western cartoon, disney, pixar, cheap mobile game, chinese hanfu, wuxia, realistic proportions, small narrow eyes, muddy colors, watercolor bleeding, painterly`。**旧「水彩絵本」の style 文はリーダーには使わない**（敵は据え置き）。武器は「魔法機械感」8案 → キャラ＝結晶芯・武器＝結晶の欠片、を画素で合成（`unity/Assets/Resources/Art/leaders/leader_green.pixellab.json`）。発注書: `docs/pixellab/konoha-chibi-japanese-style.json`（絵柄8案）・`konoha-style5-magitech.json`・`konoha-style5-crystal-mix.json`。init_image でキャラを固定する手（強さ220）は武器が変わらず失敗。
- **幕2/3の敵59体（2026-09-11 ユーザー「2,3幕の敵画像もpixellab作成して適用して」）**: 手順＝①`docs/pixellab/enemy-kinds.json` の系統（獣31/古機17/継ぎ物6/名残5）ごとに workflow で「種の記述」（何の生き物/機械として描くか＝`form_ja`。人型は動物・物・機械に置換）を書かせ ②`scripts/enemy-orders-act23.py` で系統別の発注書 `enemies-act23-{beast,machine,hybrid,revenant}.json`（各2シード 23/41）③`scripts/enemy-pairs-sheet.py` で敵ごとの A/B 比較画像 ④判定 workflow（画像を Read して「人型／形違い／画材違い／64pxで読めない」を不合格に）⑤`scripts/enemy-apply-act23.py` で合格を `Art/enemies/<id>.png` へ ⑥不合格は判定の fix_hint を種の記述にして3シード（7/57/83）で作り直し（`enemies-act23-redo-*.json`）。初回の合格 47/59。不合格の型＝**小さな哺乳類はネズミ・猫に流れる**（イモリ・モグラ・カメレオン・アルマジロが全部ネズミ/猫になった＝種名を大文字で先頭に置き、mouse/cat/fennec を negative へ）、**「壁から出た腕だけ」は蟹の全身になる**、**名残（顔なし・手足なし）はお化けの顔と手が生える**、走竜の雛は鶏の雛になる。継ぎ物の style は獣の style ＋「grafted tarnished metal parts」、名残は「faceless limbless wisp」の専用 style。**第2回（fix_hint で3シード）の答え合わせ**: 種が通ったのは従士=アルマジロ・合成獣二の相・砕き腕=壁穴の蟹・魔人=石板の中の影・幼体=尾つき・射手=カメレオン・走竜の仔。通らなかったのはイモリ・モグラ（3シードとも齧歯類）・走竜の母（兎）・名残2体（「顔なし・手足なし」でも頭巾のお化け）＝**この style で小さな両生類/食虫類/爬虫類の母/顔なしの霊は出ない**。名前が動物を指定しない敵（妖術師・コボルト）は小道具（泥玉・太鼓）を持つ小獣として採用し、名残は「頭巾の影」を許容した（次に描き直すなら名残の style を「lantern glow in fog, no shape」まで抽象化する）。**向きの検算（2026-09-11 ユーザー「右を向いてる敵が多くない？」）**: 敵は右側に立つのでプレイヤー側＝**左**を向く。pixflux は `south-west` でも右向きに出ることがある（84体中20体）。採用時にシートで向きを見て、右向きは PNG を左右反転し `.pixellab.json` に `postprocess: flip_left_right` を記す。対称・正面向きはそのまま。
- **舞台の小物と葉の塊（2026-09-08）**: `Art/props/act<N>_<name>.png`（fern/shroom/rock/log/root/stone/reed/leafclump1〜3）を置くと `Stage.PropTex` が仮絵の代わりに使う。透明が 45% 未満の絵（風景ごと描かれた絵）は自動で捨てる。プロンプトは「isolated pixel art game sprite of a single object, centered, nothing else, completely empty transparent background, no scenery」＋ negative に scene/background/landscape/forest/sky/ground。葉の塊は「foliage only (no trunk)」でも幹が出るので、下の幹の行を切り落とす（幅が最大の45%未満の下端）。地面タイルは 64 で生成して継ぎ目の良い 32 窓を切る。発注書 `docs/pixellab/stage-act1-forest.json`。
- **色**: 紙の UI（#f4ecd6）の上に貼り絵の縁が付くので、輪郭の外側に発光やぼかしを描かない。パレットは 16〜32 色に収める。
- **アイコン**: 墨1色の線＋役割の色1つ（攻撃=薔薇 #d97b7b・ブロック=空 #7fa7c9・成長=苔 #8fae7b・勢い=蜂蜜 #e0b25a・呪文=藤 #a98cc4・リアクション=青緑 #7ab8b0）。背景透過。
- **待機の2コマ目**（`<id>_idle.png` のシート）は後回し。まず1コマで並べて相性を見る。
- **書き出し**は PNG・透過・実寸（拡大しない）。ファイル名は表の id に揃える（`Assets/Resources/Art/<種別>/` に置くだけで差し替わる）。
- **Create 画面の使い分け**: S–XL (Pro) は1回20生成で16案（最終の1枚を選ぶ用）、M–XL は安価で試行錯誤用。Pro の画面には Outline/Shading の欄が無いので、絵柄の指定は Description に書く。

## B7–D18 の一括生産（2026-09-11 ユーザー「b7-d18まですべて作成して」）

`docs/art-todo.md` の B7〜D18（レリック39・リーダーのアイコン・マップの駒8・意図13・状態26・幕2/3のタイル8と小物8・背景3・情景4・コスト玉と宝石・斬撃）を一括で作った。裁定（ask_user 4件）: 14人のリーダーのアイコンは外見が未定なので保留（このはだけ顔の切り出し）／紙の9スライスは作らない（規約「紙はなめらか」）・コスト玉と宝石だけドット／情景4枚は画面に組み込む／幕2/3の小物は差し替え口を作って描く。

- **手順**: ①workflow で発注文を起草→査読（`docs/pixellab/b7d18-descriptions.json`。世界観・単一主題・人物と文字の禁止・シルエットの重複を検査） ②`python3 scripts/art-b7d18.py orders <descriptions> <scratch>` が発注書 `docs/pixellab/b7d18-{relics,icons,env,ui}.json`（各2シード 23/41）を書く ③`node scripts/pixellab.mjs gen …` ④`art-b7d18.py sheet <scratch> <out> [cats]` で A/B 比較シート（市松の下地・整数倍） ⑤判定 JSON（`{id, pick: A|B|none, passed, problem, fix_hint}`。作り直し分は `seed`/`dir` を行に書く） ⑥`art-b7d18.py apply <judge> <scratch>` が `Art/<種別>/` へ写す（コスト玉は内容を 26×26 に・宝石は 12×12 に詰める後処理つき）。
- **API の制約**: pixflux は **面積 32×32 以上**（16×16・24×24・48×12 は 422）、**幅と高さは 4 の倍数**（240×135 → **240×132**）、上限は 400。よって状態アイコンと意図アイコンは **32 ドット**で作り、UI は整数倍で置く（`UiKit.Icon` は 32px 未満の置き場では従来の 16px ビットマップ、32 以上で絵。TMP のインライン `<sprite>` も 16px ビットマップのまま）。背景の板は 480×270 でなく **384×216**（5倍で 1920×1080）。
- **Unity の差し替え口（同日）**: マップの駒＝32 を 1倍・ボス 2倍（`MapScreen`）／意図＝2倍 64・吹き出しを 68 高に（`BattleScreen`）／レリック＝20→32・84→64（`RunUi.RelicArt`）／コスト玉 `Art/ui/cost_orb.png` 26 ドット×2・宝石 `gem_<rarity>.png` 12×2（`CardView`）／紋章 `crest_<type>` は 32×2／幕2/3の小物 `Art/props/act2_{crystal,minecart,stalactite,roots}`・`act3_{spire,gate,aqueduct,pillar_fallen}`（`Stage.PropTexRaw`＝透明率の検査なし。幕3の小結晶は幕2の絵を共用）／背景の板 `Art/bg/act<N>`（幕2/3も `Stage.BgTex`）／**情景の窓** `RunUi.SceneWindow`（見出しの左に 500×290 の紙の枠、絵は 2倍。焚き火は札を 40px 下げ、ショップの棚と工房のデッキは `RunUi.SceneBottom` から）・イベントは挿絵つきの頁（左に絵・右に名前と本文）／上部バーの左端にリーダーの顔 `Art/leaders/<id>_icon.png` を 2倍。
- **判定の教訓**: タイルは「穴」「土の塊」「縁つき」が出やすい（act2_dirt は2回とも失敗→幕1の土を暗く灰寄せして派生）。垂れ根は「苔の島」に、鍾乳石は「切り株」に流れる（「上端に付く」「upright/stump を negative」で直る）。倒れた柱は地面を描きたがる（「floating, nothing beneath」）。背景は「月は舞台側で描く」ので描かせない。情景は seed によって上下に黒帯が入る（不採用）。

## 合成札の絵（2026-09-11 ユーザー裁定）

- **計算合成（`fused_<A>__<B>`）＝素材2枚の絵をその場で溶かし合わせる**（`ThemeFx.FusedArt`）: 左半分に素材A・右半分に素材Bを、下36→上44ドットの斜めの継ぎ目で繋ぎ、継ぎ目に1ドットの青緑（マナ）と両脇のほのかな光。決定的・キャッシュ・コストゼロで全ペア（緑だけで約3,700）を賄う。素材の絵が片方でも無い色は紋章のまま。id の照合は engine（`fusion.ts resolveFusedDef`）と同じ貪欲一致＝工房産を素材にした入れ子も辿る。
- **同名2枚の「真・」化**＝元の絵に青緑の内枠（1ドット）と内側へ薄れる光。
- **レシピ産（`fusion_*`）＝PixelLab で専用の挿絵（24/24 済・2026-09-12）**。レシピの作り直し（`docs/fusion-recipes-proposal.md`）の後、workflow で「素材2枚が溶け合った一つの物」の発注文を起草→査読（`docs/pixellab/cards-fusion-recipes.json`。2シード 11/37）。教訓: 「armor」「hedge」の主題は人物・人型の木を呼ぶ（絡みつく重鎧A・木漏れ日の生垣Aを不採用）。レシピ産の札は紙の外線が蜂蜜色（レア扱い）。
- **打撃・打ち据えの作り直し（同日 ユーザー「ダサいので作り直して」）**: 道具を置いただけの構図が他の札の情景と釣り合っていなかった。4構図（一閃・断ち割り・構え・接写／叩きつけ・急所のひび・横薙ぎ・歯車の火花）×2シードのシートから裁定＝打撃 B2「幹に食い込んだ刃と青緑の割れ目」・打ち据え B1「石畳に広がる青緑の割れ」（`docs/pixellab/cards-green-axe-v3.json`）。

## 画面の肌（2026-09-07 決定・デザインカンバス「戦闘画面 作り直し」第5版）

日本一ソフトウェアの絵本調の空気を自作の意匠で: クリーム色の紙（#f4ecd6）に鉛筆の二重線、水彩のにじみ、手書き風の文字（本文 Klee One／名前 Kaisei Decol）、夜の背景は水彩の群青。
**規約「絵はドット、紙と文字はなめらか」**: 挿絵的なもの（カードの絵・紋章・状態と意図のアイコン・剣と盾のしるし・敵とリーダー）はドット絵を**整数倍**で置き、紙・鉛筆線・水彩の面・文字はなめらかなUI層。
同じ存在を2つの画材で描かない（従者のカードの絵＝場のトークンと同じドット）。ドット絵に滑らかなにじみを重ねない——舞台の絵は**紙の切り抜き（貼り絵）**の縁と影で乗せる。
ここに載る絵の枡目はそのまま: 状態アイコン 16×16（インライン1倍）、意図アイコン 24×24（吹き出しに1倍）、紋章 16×16（絵が無い窓に3倍）、カード挿絵 80×48（2倍）。

**舞台＝HD-2D ジオラマ（2026-09-07 「オクトラ風なら」→ 外部レビュー5点で作り直し）**: 舞台だけ本物の3D、キャラは2Dドット。見下ろし28°の俯瞰カメラで、道と崖は手前左から奥右へ斜めに抜け、隊列も道に沿う対角線（リーダーが最も手前・敵ほど奥。座席は舞台が決め、UI の札が追従。名前札と HP バーは全員同じ線）。3Dの箱庭（地面と道・段々の台地・崖・遺跡の柱・十字の板の木と茂み・草株と小石・ランタン）に、近景シャープ→遠景ほど段階的なボケと空気遠近（霧）、月光の影と足元の接地影、夜の色補正（ランタン周りだけ暖色）、ブルーム、粒子。キャラの板はその座席の深度の面に立つので「1ドット＝画面4px」のまま（奥行きで縮めない）。紙の UI（Overlay キャンバス）には何も掛からない。
**粒の規約**: このは・敵・地面のタイル・木は全部「1ドット＝画面4px」。仮の敵絵も 64/80/96 ドットで生成する。
いまはコード生成の仮ドット。PixelLab で差し替える素材:
| 素材 | 置き場 | 大きさ | 備考 |
|---|---|---|---|
| 地面のタイル | `Art/tiles/act<N>_grass.png` | 32×32 | 敷き詰め（Repeat）。幕1=草地・幕2=苔むした石・幕3=灰の地 |
| 道のタイル | `Art/tiles/act<N>_dirt.png` | 32×32 | キャラの立つ線に沿う帯 |
| 石積みのタイル | `Art/tiles/act<N>_stone.png` | 32×32 | 遺跡の柱・ランタンの柱 |
| 崖の面のタイル | `Art/tiles/act<N>_cliff.png` | 32×32 | 台地の側面 |
| 空の板 | `Art/bg/act<N>.png` | 480×270 | 遠景の一枚絵（PixelLab の背景。4倍で敷く） |
| 木・茂み・岩（十字の板） | 未接続（次の段） | 40×60 / 28×18 / 24×14 | 3D の十字板に貼る。光を受け影を落とす。遠景はぼける |
| 草株・花・小石 | 未接続（次の段） | 12×10 / 8×10 / 12×8 | 地面に散らすデカール（十字板・寝かせた板） |
PixelLab のタイル生成（Pro の tileset）で「top-down tile, seamless, 32×32」を指定。プロンプトの土台は「Octopath Traveler style HD-2D pixel art tile, seamless, detailed shading」。

## カード（絵は後回し）

カードは 412 種。1枚ずつのイラストは最後（ゲートを越えてから）。それまでは枠＋タイプアイコン＋名前で成立させる。
タイプ別の枠色: 物理=茶／呪文=紫／リアクション=青緑／置物=金。色（緑青赤白黒）は枠の縁取り。


## レリック本家形の第1波（2026-09-12）

在庫39→102 に伴う新レリック63個の 32×32。発注書は `docs/pixellab/relics-2026-09-12.json`（説明の原本は `relics-2026-09-12-descriptions.json`）で、
B7–D18 のレリックと同じ定型（`ancient mine relic, single object centered, muted cream and ink palette` ＋ `TAIL_OBJ`／`NEG_OBJ`。view=side・selective outline・detailed shading）。
`scripts/art-b7d18.py orders` は `b7d18-*.json` を上書きするので発注書は別名で自前生成し、`node scripts/pixellab.mjs gen` → `art-b7d18.py sheet <scratch> <out> relics` → 判定 → `art-b7d18.py apply` の順。
判定は63/63採用（B=seed41 を選んだのは15: 増幅の薬・連節棍・鍛冶の火種・融合の鎚・角の留め具・大きな果実・大口の貯金箱・行商の食券・苔むした卵・安らぎの煙管・祈りの車輪・蛇の頭骨・石の暦・頑丈な留め具・旅の蝋燭）。
教訓: 「hand」「mask」は人型を呼ぶので negative に `person, arm, body`（干からびた手は seed41 が腕、赤面の面は両シードとも面だけで可）。「bank」は豚でなく蛙の口で指定した（B が口を開けた蛙で本家 Maw の意図に近い）。

## ギア（gears/ 32×32・2026-09-17）

**済 35/35（2026-09-18）**: 抽選外の3種を除く34＋魔素。発注文 `docs/pixellab/gears-2026-09-18-descriptions.json`（`_base` の定型＝"a small clockwork part from a gearwitch's karakuri toolbox, single object centered, brass and black iron, muted cream and ink palette"）→ `python3 scripts/art-b7d18.py orders <descriptions> <scratch>`（`gears` 群を追加。発注書は `gears-<_date>.json`）→ `node scripts/pixellab.mjs gen` → `art-b7d18.py sheet <scratch> <out> gears` → 判定 `gears-2026-09-18-judge.json` → `art-b7d18.py apply`（`gear_` と `mana` は `Art/gears/` へ）。発条（渦が小さすぎ）と締め紐（結び目が振り子に見えた）は seed 7/99 で作り直し（`gears-2026-09-18-redo.json`。判定行に `seed`/`dir`）。教訓: 32 ドットの物は「fills the frame」を書かないと小さく出る／flame は negative に書いても焼き鏝・火薬樽には出る（見た目は許容）。

消耗品「ギア」33種の挿絵。置き場は `unity/Assets/Resources/Art/gears/<id>.png`（32×32・Point・PPU 100・非圧縮＝レリックと同じインポート規約）。
無い間はコード生成の歯車 `ThemeFx.GearGlyph(id, family)`（id から決まる歯数 6〜9・軸穴。干渉系だけ青緑、他は真鍮）が出る。
使われる場所: 戦闘の自分の札のトークン（PC 62×62・スマホ 64×66 の中に 40〜48px）／報酬・店の札（64px）／上部バーの魔素の札のアイコン（18px＝`gears/mana.png` があればそれ）／組んだ演出の幽霊。
絵柄の指針: からくりの匣の中身＝真鍮と黒鉄の小さな部品（歯車・発条・楔・小瓶）。世界観では「実物にするのは からくりの匣・置物・レリック・素材・ゴールドだけ」＝ギアは実物なので**物として描く**（札の抽象ではない）。
干渉系（楔・錆びた楔・鎮めの錘）は脈の青緑の光を帯びる。両刃（過負荷の歯車・火薬樽・血の発条）は朱の差し色。大物（蘇りの発条・時の歯車・無銘の部品・煙玉）はレア＝蜂蜜の縁が付くので絵は控えめでよい。

| id | 名前 | レア度 | 系統 | 効果（文面） |
|---|---|---|---|---|
| `gear_spring` | 発条 | common | 汎用 | ブロック10 |
| `gear_cog` | 歯車 | common | 汎用 | 一時マナ+2 |
| `gear_oilcan` | 油差し | common | 汎用 | 3ドロー |
| `gear_powder` | 火薬 | common | 汎用 | 敵全体に10ダメージ |
| `gear_repair_oil` | 修理油 | common | 汎用 | 最大HPの20%回復 |
| `gear_brand_iron` | 焼き鏝 | uncommon | 汎用 | 対象に急所3 |
| `gear_rust_powder` | 錆粉 | uncommon | 汎用 | 対象に威圧3 |
| `gear_wedge` | 楔 | uncommon | 干渉（青緑） | 対象のいま宣言されている行動を打ち消す |
| `gear_rusty_wedge` | 錆びた楔 | uncommon | 干渉（青緑） | 対象の召喚・分裂・孵化をこの戦闘で1回止める |
| `gear_stilling_weight` | 鎮めの錘 | rare | 干渉（青緑） | 対象の割り込み（HP半分の豹変・目覚め）をこの戦闘中起こさない |
| `gear_cleansing_water` | 清めの水 | uncommon | 清め | 自分の弱体・脆弱・虚弱・拘束・霞み・重りを全て消す |
| `gear_ash_remover` | 灰落とし | common | 清め | 手札の負傷・火傷・がらくた・烙印を全て消滅させる |
| `gear_ward_charm` | 厄除けの符 | uncommon | 清め | 次に受ける状態異常を1回弾く（**抽選から外した 2026-09-18**＝2本続けて0回。絵は要らない） |
| `gear_dig_out` | 掘り出し | common | 場の操作 | 捨て札から1枚を手札へ |
| `gear_wanted_item` | 目当ての品 | uncommon | 場の操作 | 山札から1枚を選んで手札へ |
| `gear_redraw` | 引き直し | uncommon | 場の操作 | 手札を全て捨て、同じ枚数を引く（**抽選から外した 2026-09-18**） |
| `gear_next_prep` | 次の備え | common | 場の操作 | 次のターンのドロー+3 |
| `gear_whetstone_oil` | 砥ぎ油 | common | 札の一時変化 | 手札1枚をこの戦闘中鍛える |
| `gear_copy` | 写し | uncommon | 札の一時変化 | 手札1枚のコピーを手札に加える（この戦闘限り） |
| `gear_shift_powder` | 化けの粉 | rare | 札の一時変化 | 手札1枚を同じレア度の別の札に変える（この戦闘限り） |
| `gear_bewilder` | 惑わし | common | 敵の状態操作 | 対象に混乱3 |
| `gear_rust_stop` | 錆止め | uncommon | 敵の状態操作 | 対象の筋力を0に戻す（マイナスの筋力は戻さない） |
| `gear_hammer` | 金槌 | common | 敵の状態操作 | 対象のブロックと殻を砕く |
| `gear_tie_cord` | 締め紐 | common | 敵の状態操作 | 対象の次のターンの行動を隙にする（いま宣言されている行動は止まらない） |
| `gear_overload_cog` | 過負荷の歯車 | uncommon | 両刃 | 一時マナ+3・次のターンのドロー−2 |
| `gear_powder_keg` | 火薬樽 | uncommon | 両刃 | 敵全体に16ダメージ・自分もHP−4 |
| `gear_blood_spring` | 血の発条 | uncommon | 両刃 | ブロック20・HP−4 |
| `gear_paper_slip` | 挟み紙 | common | 保持・持ち越し | このターンは手札を捨てない（**抽選から外した 2026-09-18**） |
| `gear_stockpile` | 貯め置き | uncommon | 保持・持ち越し | 余ったエナジーを次のターンへ持ち越す |
| `gear_revive_spring` | 蘇りの発条 | rare | 大物 | この戦闘中、致死ダメージを一度耐えてHP1で立つ |
| `gear_time_cog` | 時の歯車 | rare | 大物 | 敵全員のいま宣言している行動を打ち消す（楔の全体版。2026-09-18 #14 のメモで「次のターン」→「このターン」へ） |
| `gear_wax_plug` | 蝋の栓 | common | 敵の状態 | 対象のいま宣言している行動の付随物（状態異常・同時強化・同時防御・からくり壊し）を消す。攻撃そのものは通る（**2026-09-18 追加**＝腐った3種の別案） |
| `gear_rusty_chain` | 錆びた鎖 | uncommon | 敵の状態 | 対象のいま宣言している攻撃の連撃を1回にする（手数の鏡も1回）（**2026-09-18 追加**） |
| `gear_decoy_charm` | 身代わりの符 | uncommon | 汎用 | このターン、最初に受ける敵の攻撃1回のHP損失を0にする（完全に防いだ扱い）（**2026-09-18 追加**） |
| `gear_spring_cog` | 湧き水の歯車 | rare | 大物 | この戦闘中、エナジー上限+1（次のターンから。芽吹きのギア版）（**2026-09-18 追加**） |
| `gear_nameless` | 無銘の部品 | rare | 大物 | このランで拾ったことのあるギアのどれかになる |
| `gear_smoke` | 煙玉 | rare | 大物 | この戦闘から逃げる（幕ボス以外。報酬は得られない） |
| `mana` | 魔素（アイコン） | — | 干渉（青緑） | 上部バーと店の魔素の札に使う青緑の歯車。無ければ `GearGlyph("mana","interfere")` |

## 白のカード（cards/ 80×48・2026-09-19 作成。88/88）

- **+1（2026-09-20）**: 灯の岐路（`white_mode_crossroad`・初期デッキのモード札）。同じ工程を1札だけ手で回した（発注文は `cards-white-descriptions.py` に追記済み・4シード 11/37/101/457 から **37**＝二股の坑道の分かれ道に真鍮のランタン、左は眩む閃光・右は琥珀の光の壁。記録は `Art/cards/white_mode_crossroad.pixellab.json`）。白 89/89。

白82種（トークン見習い1含む）＋白のレシピ産6（灯火の構え・灯すか守るか・小人形の伏兵・弩人形の号砲・報復の刃・点灯の台座）＝**88枚を 2026-09-19 に作成・適用**。
絵柄は緑の札と同じ規約（80×48・ドット・紙の面に2倍）。**白の意匠＝灯火の工房**: 真鍮のランタン・暖色の光（差し色はこれ1つ。青緑は使わない）・光の壁（半透明の琥珀色の板）・灯印（光る丸い印）・眩み（白金の閃光）・白い盾と真鍮の縁・白鉄の小さな騎士（人形）。

- **工程（緑と同じ・道具は `scripts/card-art.py`）**: 発注文の生成器 `docs/pixellab/cards-white-descriptions.py` → `cards-white-descriptions.json`（id/name/ja/description/negative_extra/doll/seeds）→ `card-art.py orders` が2シードの発注書 `cards-white-orders.json` → `node scripts/pixellab.mjs gen` → `card-art.py sheet [--with-current]`（8札/1シートの old|A|B）→ 判定 `cards-white-judge.json`（`{id, pick: A|B|old|none, passed, problem, seed?, dir?}`）→ `card-art.py apply` が `Art/cards/<id>.png` と `.pixellab.json`（judge を追記）へ。
- **第1稿の失敗（同日朝）**: 全札を「群青の無地＋琥珀の光」で揃えたら、盾12・据え置きのランタン20超・光の円盤5・同じ白騎士の立ち姿28 に集中し、ユーザー「同じような見た目のカードが多すぎる」。→ ask_user 3件の裁定: **主題はランタン中心のまま構図と場を札ごとに変える／人形が主題でない札は人形を出さない／札ごとに場を変える＋盾が主題の札を減らし、効果と名前を絵に出す**。
- **第2稿の規約（生成器の冒頭に同文）**: ①場を札ごとに変える（工房の作業台・坑道の支保工・石段・鉄の扉・炉・庭・宿場・軌道・洞窟・見張り台） ②効果と名前が絵に出る（一撃＝衝撃、壁＝壁、点灯＝芯に火が入る瞬間、ドロー＝白紙の手帳、召喚＝灯が点く、捧げ＝炉にくべる） ③盾が主題の札は5枚まで（白盾・灯盾の一撃・大盾の反撃・大盾の灯・誓いの盾）。他の「ブロック」は光の壁・板・天幕・籠手・鉄床・反射鏡で ④人形（白鉄の小さな騎士）は従者11札＋人形が主題の4札（小人形の列・人形の総突撃・小人形の伏兵・弩人形の号砲）だけ。点灯・合図・行列・捧げ・分列・身代わり・白光の壁は道具と光だけ ⑤seed は札ごとに変える。
- **人形（従者）の裁定（同日 ask_user 2ラウンド。第1稿「真鍮のからくり人形（丸い頭・レンズの目・胸の灯）」は金ピカのロボットに寄った）**: **白鉄と真鍮のからくり／顔なし（フードの奥に暖色の光が2つ）／ランタンは背中に背負う／犬は同じ意匠の四足**。体型は4案（ずんぐり・2.5頭身の布フード・小さな騎士の板金・卵型）のシートから **V3「小さな騎士」**（重なる白い板金＋真鍮の鋲と縁・フード型の白鉄の兜）。関節や歯車は足さない。
  - **文の型の教訓**: 体の記述（長い）を先頭に置くと**全札が同じ絵に潰れて役割の道具（剣・盾・旗）が消える**（同じ seed×似た文）。**道具と動きを文頭に、体の記述は後ろの括弧に、seed を札ごとに変える**と道具が出る。「ランタンを背負う」は手に提げた絵に流れやすい＝「in both hands」で両手を道具で塞ぐ。
- **人物が出る型**: 「光線・矢・鎖・鏡が跳ね返す・振る・注ぐ」は negative に person/hands があっても**持ち手の人物**（カウボーイ・外套の男）が出る。処方＝主語を物にして「by itself / nobody holding it / no bow, no archer / no hands」を positive に、negative に figure, character, cloak, hat, wielder。「灯台」は洞窟と書いても空と月が出る（「rough stone cave ceiling with stalactites above it」で1/3が洞窟に）。「月」は banner・chain・sanctuary・小人形の列にも紛れ込むので採用時に見る。
- **第2稿の結果**: 88札を2シードで生成し old|A|B で判定＝新版 71・旧版据え置き 17 → 旧版のうち15札を3シード（401/457/483・`--force-seeds`）で作り直して13札が通った（灯火の一撃＝ランタンが岩を割る／光壁の反撃／萎縮の光／小人形の点灯／灯列の突き／点灯の合図／鐘の人形／修繕の灯／光の器／恵光の灯籠／灯の鐘／灯りの庭／灯火の構え）。最後まで旧版が残ったのは4札（光壁の一突き＝新版が光のアーチで他と同じ絵・大盾の灯＝3回とも盾が出ず祠になる・灯火の号砲＝3回とも月・白盾の第1稿系は据え置き）。判定の記録は `cards-white-judge.json`（picks・作り直しの seed/dir・不合格の理由）。
- **道具の追補（同日）**: `card-art.py sheet --with-current`（左に現行版を並べる）／`orders --force-seeds`（札ごとの seeds を無視して3シード）／`pixellab.mjs` は 5xx を3回まで待って再送（1回の 502 で45枚の作り直しが止まった）。
- 青・赤・黒（計244枚）も同じ道具で作る（発注文を色ごとに書き、**最初から場と構図を散らし、効果と名前を絵に出す**）。

## 人形（舞台）（dolls/ 32×32・2026-09-19 作成 11/11）

人形の盤面表示（デザインカンバス「人形の盤面表示」案A「灯りの列」→ ユーザー裁定「A だが B との中間の位置」）で、白の従者11体がひなたの前の道に立つ。
**絵は新規に PixelLab で作る（2026-09-19 ユーザー指示「盤面の人形のイラストは新規で pixellab で作成すること」）**＝カードの挿絵の切り出しは使わない。
発注書 `docs/pixellab/dolls-stage.json`（11体・pixflux・**32×32**〔4px/ドット＝128px・ひなた 224px の半分強〕・**south-east**〔右＝敵の方〕・**low top-down**〔舞台の見下ろし〕・背景なし・selective outline・detailed shading・medium detail）。
人形の定義は白のカード挿絵と同じ（白鉄と真鍮のからくり／顔なし＝フードの奥の暗い空洞に琥珀の目2つ／ランタンは背中／犬は四足）。道具と動きを文頭に、体の記述は後ろの括弧に（白のカードの教訓）。
`node scripts/pixellab.mjs gen docs/pixellab/dolls-stage.json` で `unity/Assets/Resources/Art/dolls/<id>.png` に入り、`Creature.Get("dolls", id)` が拾う（無い間はコード生成のフードの小さな騎士 `Creature.Doll`）。
**`pixellab.mjs balance` の 0 USD は買い足しのクレジットで、月の生成枠（Tier 2・5,000枚）はそれとは別に残っている（2026-09-19 ユーザー「いやまだのこってるよ」）＝枠があれば balance 0 でも作れる**。11体を seed 1901〜1911 で一発生成し、小さな人形だけ盾を持ってしまったので 1921（手ぶら・ずんぐり）に差し替え（手当て・鐘の別シードは人の顔や桃色の鎧が出て不採用）。舞台で確認済み（`shots state` の `perms=`）。悪い札は `--only <id> --seed N --force` で作り直す。**左を向いて出た6体（剣・旗・鐘・犬・弩・小さな）は PNG を左右反転して右向き＝敵の方に揃えた（同日ユーザー指示。盾は一度反転したら左向きに見えたので生成時のまま＝ユーザー「盾だけ再度反転」。`<id>.pixellab.json` に flipped を記録。作り直したらもう一度向きを見る）**。

## ひなた v2（2026-09-18〜19。白の解凍・`docs/world.md`「ひなたの核」）

- 工程はこのは v2 と同じ: Gemini の設定画（`docs/pixellab/hinata-v2/reference-gemini.png`・プロンプトは `gemini-prompt.md`）→ 顎から下を**縦 0.55** に詰めた参考画（`reference-chibi.png`。設定画が既に4頭身だったので 0.6 でなく 0.55 でこのはの 2.7 頭身に揃えた）→ PixelLab Create from Reference（mannequin・**100×64**〔PixelLab が幅100で出した。画面は絵の幅×4px なので可〕・8方向・low top-down。発注書 `order.md`）→ ~~south-east を採用（右向きの検算済み）~~ **→ south を採用（2026-09-19 ユーザー「ひなたの向きは south を使うべきじゃない？」）**: アニメ3本（待機・攻撃・防御）は書き出しどおり **south**（正面寄りの3/4＝顔が全部見える）のコマで、一枚絵とアイコンだけ south-east（横顔でフードに顔が隠れる）だった。戦闘では待機コマが一枚絵を即座に置き換えるので、タイトル・マップの駒・上部バーのアイコンだけが横顔になっていた＝一枚絵とアイコンも south に揃えた。「リーダーは右向き（敵へ）」の規約はランタンの竿が右へ伸びることで満たす（攻撃の振りも右へ）。**south の各コマと一枚絵は足元の x 範囲が同じ（28〜93）なので配置は不変**。
- 配置: `Art/leaders/leader_white.png`（112×64・south）・`leader_white_icon.png`（32×32＝顔の切り出し (40,6)-(72,38)。旧 (44,6) はランタン込みの横顔）。記録 `leader_white.pixellab.json`。8方向と metadata は `docs/pixellab/hinata-v2/rotations/`。舞台ではランタンの暖色がブルームに乗る（`shots state` で確認済み）。
- **アニメ済み（2026-09-19）**: PixelLab の書き出しはキャラが **112×64** で作り直されていた（一枚絵とアイコンも同じ書き出しに差し替え）。~~方向の欄は「south」だが絵は south-east（右向き）~~ **訂正（2026-09-19）: コマは本当に south**（正面寄りの3/4）。一枚絵も同日 south に揃えた（上の行）。攻撃9コマ（大振り・柔らかい閃き＝ユーザー指示3件で文を練った）から **[2,4,7,8]**（振り上げ／振り下ろし／着弾の閃き／戻し）、防御9コマから **[2,4,6,8]**（竿を横に→金色の輪）、待機はテンプレ8コマ全部。コマの高さは 104（攻撃・待機）と 64（防御）で、**足元より下の透明行を一枚絵と同じにして下端を揃えた**（攻撃・待機は 112×84。`StageUnit.Apply` が枠を texture 比で拡大するのでドットの粒は変わらない）。`Art/leaders/anim/leader_white_{attack,block,idle}_N.png`・記録 `leader_white_anim.pixellab.json`・原本 `docs/pixellab/hinata-v2/animations/`。`shots state ... play=attack` で振りの途中を確認済み。hurt は作らない。

## 白の再設計「灯」の挿絵（cards/ 80×48・2026-09-20 **作成・適用済み 10/10**。発注文は `cards-white-descriptions.py` 末尾・発注書 `cards-white-light-orders.json`・2シードの A/B から判定）

白の再設計（`docs/white-redesign-proposal-2026-09-20.md`）で新規10枚。意匠は「白のカード」節と同じ（灯火の工房・白鉄と真鍮の人形・暖色のランタン）。灯＝ランタンの中のマナの光（暖色）を主役に。

| id | 名前 | 絵の要点 |
|---|---|---|
| white_light_bolt | 灯の矢 | ランタンから暖色の光が矢になって飛ぶ |
| white_light_strike | 灯集めの一撃 | 竿で打ちながら散った光の粒がランタンへ戻る |
| white_light_hoard | 灯り溜め | 光の壁の内側でランタンに光を溜める |
| white_perm_wick | 灯芯の人形 | 小さな人形が芯を掲げて灯を分ける |
| white_light_torrent | 光の奔流 | ランタンから溢れた光が奔流になって敵へ |
| white_light_verdict | 光の裁き | 頭上から一条の強い光が敵を射る（眩む） |
| white_reaction_light_wall | 灯守りの壁 | 光の壁が攻撃を受け止め、こぼれた光がランタンへ |
| white_perm_light_ballista | 灯の弩 | 真鍮の弩が灯の粒を全方位へ放つ |
| white_light_double | 灯の倍化 | 鏡でランタンの光が二つに |
| white_light_burst | 眩光の大放出 | ランタンが割れるほどの光が画面全体へ |

作り直し7枚（点灯の合図・灯火の大行列・大灯台・灼く光・灯の輪・大いなる癒し・光壁砕き）は挿絵を流用。撤去13枚の挿絵は `Art/cards/` に残置（無害）。

## 灯と人形の結び（2026-09-20 夜・`card-power.md` §74）: 新規の人形2体の挿絵と舞台の絵

- **挿絵（cards/ 80×48）**: 発注文は `cards-white-descriptions.py` 末尾（seeds 677/691・703/727）→ `card-art.py orders --only … --out cards-white-dolls2-orders.json` → 2シードの A/B → 判定 `cards-white-dolls2-judge.json` → apply。
  - 灯篭の人形（`white_perm_lantern`）: 677＝赤い目の騎士・691＝人形が小さく灯篭を掲げていない → 733/757（`--force-seeds`・`cards-white-dolls2-orders-redo.json`）で作り直し、**733**＝白鉄の人形が琥珀の目で灯篭を頭上に掲げる。
  - 篝火の人形（`white_perm_bonfire`）: 703＝フードの奥に人の顔（肌色）が出た → **727**＝顔なしの空洞・背中の篝火の籠と炎・埋み火。
- **舞台の人形（dolls/ 32×32・south-east・low top-down）**: `dolls-stage.json` に2体を追加（13体）。灯篭＝1931/1941 は人の顔が出て **1951**（顔なし・琥珀の目・竿の灯篭を右へ）。篝火＝**1932** 一発（背中の篝火・赤い目・右向き）。どちらも右を向いて出たので反転なし。
- 教訓の再確認: 「holding up a lantern」系は2回に1回フードの奥に人の顔（肌色）が出る＝必ず拡大して顔を見る。

## 火種と放出の軸の挿絵（cards/ 80×48・2026-09-20 夜 **作成・適用済み 19/19＋トークン**。`card-power.md` §75）

発注文は `cards-white-descriptions.py` 末尾（seeds 743〜1337）→ `card-art.py orders --only … --out cards-white-spark-orders.json`（38件）→ 2シードの A/B → 判定 `cards-white-spark-judge.json` → apply。意匠＝「燠の種（ember seed）」＝豆粒ほどの暖色の光。
- 一発で通ったもの18: 火種（作業台の小さな火）・火種撒き（廊下に散る小さな火）・火守りの盾・火花散らし（金床の火花）・大焚き付け（火柱）・灯火の炉（扉の開いたストーブ）・火起こし（傾いたランタン→藁）・灯の火床・火の粉・灯の継ぎ手（灯の連なり→人形の松明）・灯の壁・眩む閃光・灯の鍛冶・灯の手帳・残り火・灯の大炉・灯の大槌・灯の火皿。
- **火種の嵐は4回外れた**（1003/1021/1401/1423＝「洞窟に燠の嵐」は必ず外套の人物の後ろ姿を呼ぶ。negative に man/woman/cloak/cape/hood/traveler/back view を足しても出る）→ 舞台を捨てて**接写**（作業台のランタンから噴き上がる燠の嵐）にして 1441 で通った。教訓: 「洞窟＋嵐／光」は人物の署名。人物を消したい時は舞台を消して接写にする。
- 舞台の人形は増えない（新規の人形は無し。灯の継ぎ手・火の粉などは道具の置物）。

## 灯の器＝真鍮のランタン（ui/ 32×48・2026-09-20・デザインカンバス `docs/design/light-gauge/`。**未発注＝コード生成の絵で稼働中**）

ひなたの戦闘画面の灯（白の蓄積）をエナジーの輪と釣り合う器にした（案B。`unity/Assets/Game/LightUi.cs`）。いまの絵は `ThemeFx.Lantern`（コードで描いた 32×48 の正面図＝吊り輪・笠・黒鉄の枠・暗い硝子・台座）と `ThemeFx.Flame`（12×16 の炎）で、差し替え口は次の3つ:

| 置き場 | 寸法 | 条件 |
|---|---|---|
| `Art/ui/lantern.png`（点灯）／`Art/ui/lantern_dark.png`（消灯・省略可） | 32×48・正面・原点は台座の下端の中央 | **硝子の窓 x 7〜24・y 14〜35（上が 0）は暗いまま空けておく**（炎と数字はコードがその上に描く）。黒鉄の枠と真鍮の飾り＝ひなたの竿の灯籠と同じ意匠。PixelLab に頼むなら「brass and black-iron lantern, front view, empty dark glass window, pedestal base, no flame」 |
| `Art/fx/flame.png` | 12×16・原点は根元の中央 | 白い芯 → 真鍮の紙 → 真鍮（延焼の橙は使わない） |
| `Art/fx/light_streak.png` | 96×24 | 放出の光の筋（斬撃の筋の暖色版） |

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
| リーダー（アイコン） | leaders | 32×32 | 15 | `<id>_icon.png`。セットアップ・ラン画面用 |
| レリック | relics | 32×32 | 38 | |
| 紙の9スライス | ui | 52×52 / 48×48 / 32×32 / 40×40 | 4 | `paper_card.png`（カード。角丸14・border16）／`paper_panel.png`（頁・吹き出し。角丸12・border14）／`paper_tag.png`（札。角丸8・border10）／`paper_button.png`（ボタン。下に厚み。角丸10・border12）。クリーム色の紙＋鉛筆の二重線。**2026-09-07 の絵本調決定でタイプ×色の枠は廃止**——タイプは左上の「しおり」、色は台紙の縁で出す |
| カードのしおり（タイプ） | ui | 16×24 | 4 | `bookmark_<type>.png`。物理=砂色／呪文=藤色／リアクション=青緑／置物=蜂蜜色。コストの数字は文字で乗せる |
| 役割のにじみ（剣・盾・返し） | ui | 32×20 | 3 | `blob_<dmg|block|counter>.png`。水彩のにじみ形。カード左下＝与ダメ、右下＝ブロック。数字は文字 |
| カード挿絵 | cards | **80×48** | 412（最後） | `<id>.png`（例 `green_strike.png`）。**2倍で 160×96、紙の台紙（168×104）に貼る**。絵が無い札はタイプの紋章 16×16 を3倍 |
| レア度の宝石 | ui | 12×12 | 3 | `gem_common/uncommon/rare.png` |
| コスト玉 | ui | 20×20 | 1 | `cost_orb.png`（数字は文字で乗せる） |
| 状態アイコン（16px） | icons | 16×16 | 下表 | インライン用（カード文面・意図・バフ欄） |
| 意図アイコン | icons | 24×24 | 13 | `intent_<kind>.png`（吹き出しでは2倍）。無い間は状態アイコンで代用 |
| マップのノード | map | 32×32 | 8 | `node_<type>.png`（battle/elite/boss/shop/campfire/workshop/unknown/treasure） |
| 背景（幕） | bg | 480×270 | 3 | `act1/act2/act3.png`。4倍で 1920×1080 |
| 焚き火・工房・ショップの情景 | scenes | 240×135 | 4 | `campfire/workshop/shop/event.png` |
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
| enemy_wide_power | うねる獣 | 通常 | 96×96 |
| enemy_probe | 探り屋 | 通常 | 96×96 |
| enemy_set_wary | 用心深い影 | 通常 | 96×96 |
| enemy_set_breaker | 罠壊し | 通常 | 96×96 |
| enemy_brute | 脳筋オーガ | 幕ボス | 160×160 |
| enemy_turtle | 眠たがりの大亀 | 幕ボス | 160×160 |
| enemy_hexer | 泥投げの妖術師 | 通常 | 96×96 |
| enemy_wolf | 牙嵐の狼 | 通常 | 96×96 |
| enemy_moss | 苔まといの主 | 通常 | 96×96 |
| enemy_joker | 嘲る道化 | 通常 | 96×96 |
| enemy_drummer | 鼓吹きコボルト | 通常 | 96×96 |
| enemy_warden | 刻限の門番 | 幕ボス | 160×160 |
| enemy_thorn_squirrel | 針毛の栗鼠 | 通常 | 96×96 |
| enemy_thief | こそ泥ゴブリン | 通常 | 96×96 |
| enemy_bomber | 火薬樽かつぎ | 通常 | 96×96 |
| enemy_moss_healer | 苔の癒し手 | 通常 | 96×96 |
| enemy_axe_ogre | 大振りの斧鬼 | 通常 | 96×96 |
| enemy_shell_guard | 石殻の番人 | 通常 | 96×96 |
| enemy_apprentice_colossus | 見習い巨像 | 通常 | 96×96 |
| enemy_mimic_imp | 物真似の子鬼 | 通常 | 96×96 |
| enemy_cultist | 囁きの狂信者 | 通常 | 96×96 |
| enemy_slug | 酸吐きの蛞蝓 | 通常 | 96×96 |
| enemy_whetstone_colossus | 砥石の巨像 | 通常 | 96×96 |
| enemy_mimic_jester | 物真似の道化 | 通常 | 96×96 |
| enemy_cinder_imp | 焚きつけのインプ | 通常 | 96×96 |
| enemy_rock_beetle | 岩皮の甲虫 | 通常 | 96×96 |
| enemy_big_slime | 大苔スライム | 通常 | 96×96 |
| enemy_moss_slime | 苔スライム | 通常 | 96×96 |
| enemy_elite_sergeant | 鞭打ちの鬼軍曹 | エリート | 128×128 |
| enemy_elite_sentry | 歩哨 | エリート | 128×128 |
| enemy_elite_iron_egg | 眠れる鉄卵 | エリート | 128×128 |
| enemy_elite_slaver | 鎖持ちの奴隷商 | エリート | 128×128 |
| enemy_elite_stab_book | 刺突の書 | エリート | 128×128 |
| enemy_elite_giant_face | 巨面 | エリート | 128×128 |
| enemy_elite_gold_raven | 金羽の大鴉 | エリート | 128×128 |
| enemy_elite_mirror_djinn | 写し身の魔人 | エリート | 128×128 |
| enemy_elite_doom_chanter | 終焉の唱い手 | エリート | 128×128 |
| enemy_elite_devourer | 大喰らいの蟲 | エリート | 128×128 |
| enemy_elite_owl | 読み手の梟 | エリート | 128×128 |
| enemy_elite_deathless | 不滅の騎士 | エリート | 128×128 |
| enemy_shield_squire | 盾持ちの従士 | 通常 | 96×96 |
| enemy_crossbow_archer | 弩弓の射手 | 通常 | 96×96 |
| enemy_bond_wolf | 双牙の狼 | 通常 | 96×96 |
| enemy_mud_lump | 泥まとうもの | 通常 | 96×96 |
| enemy_elite_husk_3 | 不滅の骸兵・参 | エリート | 128×128 |
| enemy_elite_husk_2 | 不滅の骸兵・弐 | 通常 | 96×96 |
| enemy_elite_husk_1 | 不滅の骸兵・壱 | 通常 | 96×96 |
| enemy_brood_toad | 蟲抱えの蛙鬼 | 通常 | 96×96 |
| enemy_broodling | 蠢く幼体 | 通常 | 96×96 |
| enemy_brood_raptor | 抱卵の走竜 | 通常 | 96×96 |
| enemy_raptor_egg | 走竜の卵 | 通常 | 96×96 |
| enemy_raptor_chick | 走竜の仔 | 通常 | 96×96 |
| enemy_mourn_beast | 弔いの獣 | 通常 | 96×96 |
| enemy_maw_hunter | 大顎の狩人 | 通常 | 96×96 |
| enemy_sludge_berserker | 汚泥の大暴れ | 通常 | 96×96 |
| enemy_mudling | 小泥 | 通常 | 96×96 |
| enemy_gaping_maw | 裂け口の獣 | 通常 | 96×96 |
| enemy_cog_construct | 歯車の箱兵 | 通常 | 96×96 |
| enemy_spore_cap | 胞子吹きの茸 | 通常 | 96×96 |
| enemy_strangler_serpent | 巻きつく大蛇 | 通常 | 96×96 |
| enemy_snap_fruit | 噛みつき果実 | 通常 | 96×96 |
| enemy_vine_walker | 蔦纏いの歩き木 | 通常 | 96×96 |
| enemy_iron_clam | 鉄殻の溝貝 | 通常 | 96×96 |
| enemy_sludge_spider | 汚泥紡ぎの蜘蛛 | 通常 | 96×96 |
| enemy_axe_automaton | 絡繰の斧兵 | 通常 | 96×96 |
| enemy_axe_automaton_reboot | 絡繰の斧兵・再起 | 通常 | 96×96 |
| enemy_thunder_globe | 雷球の巨頭 | 通常 | 96×96 |
| enemy_frog_knight | 蛙の騎士 | 通常 | 96×96 |
| enemy_biting_scroll | 噛みつく巻物 | 通常 | 96×96 |
| enemy_lost_soul | 失せ人の霊 | 通常 | 96×96 |
| enemy_forgotten_soul | 忘れ人の霊 | 通常 | 96×96 |
| enemy_devoted_sculptor | 献身の彫師 | 通常 | 96×96 |
| enemy_chomper | 金切り顎 | 通常 | 96×96 |
| enemy_scald_gnat | 灼き虻 | 通常 | 96×96 |
| enemy_kin_priest | 血族の司祭 | 幕ボス | 160×160 |
| enemy_kin_follower | 血族の踊り手 | 幕ボス | 160×160 |
| enemy_crab_crusher | 巨蟹の砕き腕 | 幕ボス | 160×160 |
| enemy_crab_cannon | 巨蟹の撃ち腕 | 幕ボス | 160×160 |
| enemy_chimera_1 | 蘇る合成獣・一の相 | 幕ボス | 160×160 |
| enemy_chimera_2 | 蘇る合成獣・二の相 | 通常 | 96×96 |
| enemy_chimera_3 | 蘇る合成獣・三の相 | 通常 | 96×96 |
| enemy_burrow_worm | 潜行する大地虫 | 通常 | 96×96 |
| enemy_bowl_bug | 転がる岩虫 | 通常 | 96×96 |
| enemy_nemesis_wraith | 因縁の亡霊 | 通常 | 96×96 |

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
| 1 | **このは（戦闘内ちび）** `leaders/leader_green.png` | 1 | 96×96 | 主役。全戦闘に出る。貼り絵の縁と最初に合わせる1枚 |
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
| 12 | **カード挿絵・初期デッキ** 打撃・打ち据え・防御・絡み蔦・蔦の楔・茨の返し `cards/card_<id>.png` | 6 | 80×48 | 「紙の台紙に貼った絵」が成立するかをここで判定。良ければ緑のコモン25 → 全412 |
| 13 | **幕2・幕3の敵**（残り約45）・エリート8・ボス6 | 約60 | 96/128/160 | 幕1で絵の文法が固まってから |
| 14 | **背景と情景** `bg/act1..3.png`（480×270→4倍）・`scenes/campfire|workshop|shop|event.png` | 7 | 480×270 / 240×135 | 今の水彩の夜で成立しているので最後 |

### PixelLab で作るときのメモ（2026-09-07 改定: 向きは斜め前 3/4・絵柄はオクトパストラベラー風の美麗ドット）

- **絵柄の要件（ユーザー指定）**: オクトパストラベラー（HD-2D）風の美麗ドット。陰影を細かく（detailed shading）、輪郭は黒1色でなく暗色の選択的輪郭（selective outline）、面は多階調。ゆるかわの造形（丸い頭・大きい目）は保ちつつ、描き込みで格を出す。
- **向き**: **斜め前 3/4**。PixelLab では view = side のまま direction を **south-east（リーダー・右向き）／south-west（敵・左向き）**。east/west は横顔になるので使わない。出来上がった絵の向きを変えるだけなら **Rotate**（from east → to south-east）。
- **寸法はこの表のとおり**（通常とリーダー 64・エリート 80・ボス 96。どれも**4倍表示＝1ドットが画面4px**でオクトラと同じ密度。アイコンは 16/24/32）。キャラの実体は 64 キャンバスの中で高さ 40〜48 ドットが目安（オクトラは 18×32 前後）。画面には整数倍で置くので、キャンバスいっぱいに使わず、敵は下端を地面に接し左右に 6〜10px の余白。
- **頭身**: 戦闘内ちびは 2.5〜3 頭身（オクトラの街の人物に近い）。塔の深層（幕3）ほど不穏に。
- **色**: 紙の UI（#f4ecd6）の上に貼り絵の縁が付くので、輪郭の外側に発光やぼかしを描かない。パレットは 16〜32 色に収める。
- **アイコン**: 墨1色の線＋役割の色1つ（攻撃=薔薇 #d97b7b・ブロック=空 #7fa7c9・成長=苔 #8fae7b・勢い=蜂蜜 #e0b25a・呪文=藤 #a98cc4・リアクション=青緑 #7ab8b0）。背景透過。
- **待機の2コマ目**（`<id>_idle.png` のシート）は後回し。まず1コマで並べて相性を見る。
- **書き出し**は PNG・透過・実寸（拡大しない）。ファイル名は表の id に揃える（`Assets/Resources/Art/<種別>/` に置くだけで差し替わる）。
- **Create 画面の使い分け**: S–XL (Pro) は1回20生成で16案（最終の1枚を選ぶ用）、M–XL は安価で試行錯誤用。Pro の画面には Outline/Shading の欄が無いので、絵柄の指定は Description に書く。

## 画面の肌（2026-09-07 決定・デザインカンバス「戦闘画面 作り直し」第5版）

日本一ソフトウェアの絵本調の空気を自作の意匠で: クリーム色の紙（#f4ecd6）に鉛筆の二重線、水彩のにじみ、手書き風の文字（本文 Klee One／名前 Kaisei Decol）、夜の背景は水彩の群青。
**規約「絵はドット、紙と文字はなめらか」**: 挿絵的なもの（カードの絵・紋章・状態と意図のアイコン・剣と盾のしるし・敵とリーダー）はドット絵を**整数倍**で置き、紙・鉛筆線・水彩の面・文字はなめらかなUI層。
同じ存在を2つの画材で描かない（従者のカードの絵＝場のトークンと同じドット）。ドット絵に滑らかなにじみを重ねない——舞台の絵は**紙の切り抜き（貼り絵）**の縁と影で乗せる。
ここに載る絵の枡目はそのまま: 状態アイコン 16×16（インライン1倍）、意図アイコン 24×24（吹き出しに1倍）、紋章 16×16（絵が無い窓に3倍）、カード挿絵 80×48（2倍）。

**舞台＝HD-2D ジオラマ（2026-09-07 「オクトラ風なら」→ 外部レビュー5点で作り直し）**: 舞台だけ本物の3D、キャラは2Dドット。見下ろし20°の透視カメラで、敵は手前左から奥右へ斜めの列（座席は舞台が決め、UI の札が追従）。3Dの箱庭（地面と道・段々の台地・崖・遺跡の柱・十字の板の木と茂み・草株と小石・ランタン）に、近景シャープ→遠景ほど段階的なボケと空気遠近（霧）、月光の影と足元の接地影、夜の色補正（ランタン周りだけ暖色）、ブルーム、粒子。キャラの板はその座席の深度の面に立つので「1ドット＝画面4px」のまま（奥行きで縮めない）。紙の UI（Overlay キャンバス）には何も掛からない。
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

#!/usr/bin/env python3
"""白のカード挿絵 88 種の発注文を書く (2026-09-19)。→ docs/pixellab/cards-white-descriptions.json
   python3 docs/pixellab/cards-white-descriptions.py

第1稿 (同日朝) は「群青の無地＋琥珀の光」で全札を揃え、盾12・据え置きのランタン20超・光の円盤5・白騎士28 に集中して
「同じような見た目のカードが多すぎる」(ユーザー)。第2稿 (同日) の規約:
  1. 主題はランタン中心のまま (灯火の工房の枠を出ない)。**構図と場を札ごとに変える** (工房の作業台・坑道・石段・鉄の扉・炉・庭・宿場・軌道…)
  2. **効果と名前が絵に出る** (一撃=衝撃、壁=壁、点灯=芯に火が入る瞬間、ドロー=白紙の手帳、召喚=灯が点く…)
  3. 盾を主題にする札は5枚まで (白盾・灯盾の一撃・大盾の反撃・大盾の灯・誓いの盾)。他の「ブロック」は光の壁・板・天幕・籠手・鉄床で
  4. 人形 (白鉄の小さな騎士) を出すのは従者11札＋人形が主題の札だけ (小人形の列・人形の総突撃・小人形の伏兵・弩人形の号砲)。
     点灯・合図・行列・捧げ・分列・身代わり・白光の壁は道具と光だけで描く
  5. seed は札ごとに変える (同じ seed×似た文は同じ絵に潰れる)。人形の文は「道具と動き」が先・体の記述は後ろの括弧
"""
import json, os
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

STYLE = ("Octopath Traveler HD-2D style pixel art illustration for a small card, the single subject fills most of the frame in close-up, "
         "painterly detailed shading, muted palette where warm lamplight is the only bright accent, polished brass and white as the materials, "
         "no text, no letters, no border, no frame, no people, no moon, no sky")
KD = "a small white iron knight doll"
BODY = (" (two heads tall, matte white enameled iron armor plates with brass rivets, hood-shaped white iron helm, "
        "no face: only an empty dark hollow with two small warm amber glowing eyes, a small brass lantern strapped on its back)")
KDS = "small white iron knight dolls"
BODYS = (" (two heads tall each, matte white enameled iron armor plates with brass rivets, hood-shaped white iron helms, "
         "no faces: only empty dark hollows with two small warm amber glowing eyes, small brass lanterns strapped on their backs)")
DOG = ("a small white iron armored hound doll, a four-legged clockwork dog (matte white enameled iron armor plates with brass rivets, "
       "hood-shaped white iron helm over its head with an empty dark hollow and two small warm amber glowing eyes, no fur, no snout, a small brass lantern strapped on its back)")
NEG = ("person, human, man, woman, child, adventurer, hero, warrior, figure, silhouette, hands, face, portrait, skin, hair, moon, sun, sky, "
       "text, letters, watermark, border, frame, card frame, ui, blurry, bright saturated colors, teal glow, green glow")
NEG_DOLL = ("human, person, child, man, woman, girl, boy, skin, flesh, hair, face, eyes with pupils, mouth, nose, skull, ghost, cloth doll, rag doll, plush, wooden puppet, "
            "marionette strings, gold robot, holding a lantern, handheld lantern, moon, sun, sky, text, letters, watermark, border, frame, card frame, ui, blurry, bright saturated colors, teal glow, green glow")
NOBODY = "figure, character, cloak, hat, silhouette of a person, hand, arm, wielder, someone holding it"
NOTEXT = "letters, runes, glyphs, writing"

items = []
def add(id, name, ja, desc, doll=False, neg=None):
    it = {'id': id, 'name': name, 'ja': ja, 'description': '{style}, ' + desc}
    if doll: it['doll'] = True
    if neg: it['negative_extra'] = neg
    items.append(it)

# ---- 攻撃と壁 ----
add('white_strike', '灯火の一撃', '竿のランタンを岩に振り下ろす（接写・作り直し）', 'a heavy square brass lantern on the end of a long dark iron pole smashing down onto a cracked boulder, close-up on the impact, the boulder splitting with a burst of amber light, sparks and stone chips flying, rough mine tunnel wall behind, the pole enters the frame from the top left, no one holding it', neg=NOBODY + ', moon, circle in the sky')
add('white_shield_strike', '灯盾の一撃', '盾の灯から光の一撃（石壁）', "a round white shield with a brass rim seen at a slight angle against a dark stone wall of a mine tunnel, a short thick beam of warm amber light bursting straight out of its central brass lantern boss toward the right, bright impact sparks at the end of the beam, close-up on the shield, nobody holding it", neg=NOBODY)
add('white_wall_jab', '光壁の一突き', '光の壁から棘（坑道）', "a translucent pane of warm amber light standing upright like a wall in a mine tunnel with timber supports, a single sharp spike of solid light thrusting out of it toward the right, cracks of light around the base of the spike, motion")
add('white_guard', '白盾', '工房の壁の白い盾', "a round white-lacquered shield with a brass rim, brass rivets and a small unlit brass lantern boss at its center, hanging on a wooden workshop wall beside brass tools on hooks, straight on, close-up")
add('white_bodyslam', '光壁砕き', '光の壁が突進して砕ける', "a thick slab of solid warm amber light wall ramming forward to the right and shattering at its leading edge into flying shards of light against a brick wall, impact, motion")
add('white_mode_crossroad', '灯の岐路', '二股の坑道と分かれ道のランタン（初期デッキのモード札 2026-09-20）', "a forked mine tunnel splitting into two passages at a brass lantern on a short post standing at the fork, the left passage flooded by a harsh dazzling white-amber flash with jagged rays, the right passage sealed by a calm translucent wall of warm amber light, timber supports and rough rock, a choice at a crossroads", neg=NOBODY)
add('white_fortress', '大光壁', '真鍮の柱の間の巨大な光の壁', "a towering wall of translucent warm amber light panes held between tall brass pillars in a great stone hall, seen from below, imposing, warm glow")
add('white_rampart_riposte', '光壁の反撃', '光の胸壁から光の矢が飛ぶ（作り直し）', 'a battlement of glowing amber light panes built along the top of a low stone wall, seen from the side at an angle, dozens of small darts of light flicking out from its top edge toward the right like arrows, chipped stone, mine tunnel behind, nobody present', neg=NOBODY)
add('white_shield_bash', '大盾の反撃', '大盾の突進と衝撃波（石畳）', "a huge white tower shield with a brass rim slamming forward across a flagstone floor, a shockwave of warm light bursting from its face, impact, motion", neg=NOBODY)
add('white_gate_close', '光門閉鎖', '真鍮の大扉が閉まり光が隙間を塞ぐ', "massive brass double doors slamming shut, a sheet of warm amber light sealing the gap between them, dust and sparks, straight on")
add('white_rank_shield', '灯列の盾', '灯の列が光の柵になる', "a row of small square brass lanterns on short brass posts standing in a line on a flagstone floor, each throwing a translucent pane of warm light that overlaps with the next into a low glowing fence, seen straight on")
add('white_shield_prayer', '大盾の灯', '石段の上の灯つき大盾（作り直し）', 'a large white tower shield with a brass rim standing upright at the top of a short flight of stone steps, a square brass lantern set into its center glowing warm amber, vines on the steps, close-up')
add('white_shield_verse', '盾持ちの灯', '籠手に掛けた灯と白紙の手帳（ドロー）', "a small square brass lantern hooked onto the cuff of a white iron gauntlet resting on a wooden workbench, the lantern's warm light falling across a small open notebook with blank pages, close-up", neg=NOTEXT + ', ' + NOBODY)
add('white_catalyst_retain', '不動の誓い', '床に留めた鉄床と灯（炉）', "a heavy black iron anvil bolted to a stone floor with thick brass bolts and short chains, a small square brass lantern standing on it glowing steadily, forge glow in the background, close-up")
add('white_oath_shield', '誓いの盾', 'リボンを結んだ盾（作業台の白い布）', "a round white shield with a white ribbon tied around it and a glowing warm amber mark at its center, resting on a white cloth on a wooden workbench, steady light, close-up")

# ---- 光の術（眩み・灼き・矢・印） ----
add('white_menace', '眩ます灯印', '灯印を石に押す（眩む）', "a brass seal stamp pressing a glowing circular mark of warm light onto a dark cobblestone floor, the mark flaring with harsh glare rays, close-up", neg=NOTEXT)
add('white_seal_light', '灯印の光', '巻物に押した光る蝋の封', "a rolled scroll tied with cord lying on a wooden desk, sealed with a blob of glowing amber wax stamped with a circular mark, the seal's light spilling over the desk, a brass stamp beside it, close-up", neg=NOTEXT)
add('white_reaction_ward', '護りの灯印', '鉄の扉に焼き付いた護りの印', "a heavy dark iron door set in a stone wall with a circular ward-mark of glowing amber light burned into its center, light seeping from the mark's lines, calm, seen straight on", neg=NOTEXT)
add('white_decree', '眩む光', '全体を眩ませる白金の閃光', "a small square brass lantern on a stone floor exploding into a huge blinding burst of white-gold light, long radial glare rays filling the entire frame, the surrounding stone washed out white, close-up")
add('white_cowering_light', '萎縮の光', '坑道の隅で影が縮む（接写・作り直し）', 'extreme close-up of a dark corner of a mine tunnel where a harsh cone of warm lantern light from the left hits the wall, the black shadows on the wall recoiling and shrinking into the corner as if alive, dust in the beam, a timber support at the edge, no figures', neg=NOBODY)
add('white_reaction_holy_wall', '眩みの障壁', '石のアーチに嵌まる眩しい光の板', "an upright pane of dazzling white-gold light set in a stone archway with a thin brass frame, glaring rays shooting outward from its surface, close-up")
add('white_judgment', '灼く光', 'レンズを通した光線が石を灼く', "a large brass magnifying lens mounted on a brass stand, a narrow white-gold beam passing through the lens and burning a glowing molten line into the dark stone floor, smoke curling up, close-up on the lens and the burn, nobody present", neg=NOBODY)
add('white_light_arrow', '白光の矢', '光の矢が坑道を飛ぶ', "extreme close-up of a single arrow made entirely of white-gold light flying by itself down a dark mine tunnel from left to right, a long bright trail behind its fletching, sparks, no bow, no archer", neg=NOBODY)
add('white_glory_chain', '眩光の鎖', '石壁の環から光の鎖', "a heavy chain made of links of blinding white-gold light coiling across the frame in the dark, one end anchored to a brass ring set into a stone wall, the links glaring, nobody holding it", neg=NOBODY)
add('white_verdict_hammer', '眩みの槌', 'ランタン頭の槌の閃光（鍛冶場）', "a heavy brass war hammer with a blazing lantern set into its head slamming down onto the stone floor of a forge, a blinding flash of glare rays and cracks of light at the impact, motion", neg=NOBODY)
add('white_holy_maul', '灯火の大槌', '大槌が石を割り光のひび', "a huge two-handed brass maul with a glowing lantern in its head crashing down onto dark stone, cracks of warm light spreading from the impact, motion", neg=NOBODY)
add('white_reaction_retribution', '報復の光', '反射鏡が光を跳ね返す（坑道）', "a polished brass parabolic reflector dish bolted to the stone wall of a mine tunnel, a harsh beam of light coming in from the right and bouncing straight back toward the right in a sharp flare, nobody present, close-up", neg=NOBODY)
add('white_reaction_chant', '光盾の点灯', '本の上のランプから光のドーム（呪文で鳴る）', "a small brass oil lamp standing on a closed old leather book on a workbench, its flame flaring up and a translucent dome of warm light rising over the lamp and hardening into a shell, close-up", neg=NOTEXT)
add('white_reaction_bright_wall', '白光の壁', '床のランプから立つ白光の壁', "a blinding wall of white-gold light rising from a row of small lamps set into a flagstone floor, the wall filling the frame, a stone corridor washed out white behind it")
add('white_hymn_wall', '灯りの壁', '石の回廊に吊るしたランタンの壁', "a wall made of many hanging square brass lanterns on chains filling a stone corridor, their combined warm light forming a barrier, straight on, close-up")
add('white_perm_pavilion', '光の天幕', '洞窟の床の光の布の天幕', "a small tent canopy made of glowing warm amber light cloth held up by brass poles on a rocky cavern floor, sheltering the ground beneath, close-up")
add('white_perm_ballista', '光壁の弩', '光の壁の上の弩', "a brass ballista mounted on top of a wall of glowing warm amber light, firing a bolt of solid light to the right, close-up")
add('white_perm_warcry', '灯り増し', '作業台の油ランプの芯を上げる', "a brass oil lamp on a wooden workbench among brass tools with its wick wheel being turned up, the flame growing tall and the warm glow doubling in size, close-up", neg='hands')
add('white_perm_anthem', '輝き増し', '大広間のシャンデリアの全灯', "a great brass chandelier of many small lanterns hanging in a great stone hall, all blazing brighter at once, rays of warm light filling the frame, close-up")

# ---- 回復（傷を塞ぐ光）と灯りの道具 ----
add('white_heal', '癒しの光', '光の粒が白い花に降る', "a soft rain of warm amber light motes falling from a small hanging brass lantern onto a cluster of pale white flowers that glow as the light touches them, close-up")
add('white_mass_heal', '大いなる癒し', '天井のランタンから光の洪水', "a huge brass ceiling lantern opened wide, a flood of warm amber light pouring straight down and filling the whole frame, drifting motes, rays")
add('white_service', '灯り継ぎ', '宿場の棚で蝋燭から蝋燭へ火を継ぐ', "two brass candlesticks on a wooden shelf of an inn, a small flame being passed from the lit one to the unlit one, the new wick just catching light, close-up", neg='hands')
add('white_healing_verse', '癒しの灯', '作業台の白紙の手帳とランプ（ドロー）', "an open lamp-keeper's notebook with blank pages lying on a wooden workbench, a small brass lamp set on the page corner, warm light and rising motes, a pressed white flower on the page, close-up", neg=NOTEXT)
add('white_holy_oil', '灯の油', '光る油をランタンに注ぐ（作業台）', "a small glass vial of glowing golden lamp oil tipped in midair by itself above the open door of a square brass lantern on a workbench, a thin stream of glowing oil pouring into the lantern, drops of light, no hands", neg=NOBODY)
add('white_prayer_verse', '灯りの一節', '石の棚の短い蝋燭ひとつ', "a single short white candle stub with a small steady warm flame on a brass dish, standing on a stone ledge in a dark tunnel, close-up, quiet")
add('white_miracle_light', '輝きの光', '洞窟に吊るしたランタンの中で星が弾ける', "a blinding star of white-gold light bursting inside a square brass lantern hanging in a dark cave, rays escaping through its glass panes, close-up")
add('white_mercy_staff', '灯りの杖', '鉤に小さなランタンの杖', "a white staff with a brass hook at its top and a small square lantern hanging from the hook glowing warm amber, leaning against dark stone, close-up", neg='hands')
add('white_blade_prayer', '灯りの刃', '光の刃の短剣', "a short sword whose blade is made of solid glowing amber light, brass hilt, laid on white cloth, gentle motes rising, close-up")
add('white_perm_spring', '灯りの泉', '光の水の泉', "a small round stone basin fountain in a wall niche filled with liquid warm amber light instead of water, glowing light trickling from a brass spout, ferns around it, close-up")
add('white_mending', '修繕の灯', '作業台の胸当てが繕われる（作り直し）', 'a dented white iron breastplate (chest armor) lying on a wooden workbench, a small brass repair lamp beside it shining on the dent, the dent sealing itself with a glowing seam of warm light, brass tools around, close-up', neg='mannequin, torso, cloth, paper, sheet')
add('white_perm_chalice', '光の器', '祭壇の光が溢れる器（作り直し）', 'a wide shallow brass bowl on a stone altar in a wall niche, filled to the brim with glowing liquid amber light that spills over its rim and runs down the altar in glowing streams, no fire, no flames, close-up', neg='fire, flames, brazier, burning')
add('white_perm_apostle', '恵光の灯籠', '地下の苔の庭の灯籠と光の蛾（作り直し）', 'a tall standing brass lantern with paper panes glowing warm amber in a mossy underground garden, small moths of light circling it, rock ceiling above, no sky, no moon', neg='moon, sky, circle, orb in the sky')
add('white_perm_bell', '灯の鐘', '鐘楼の部屋の中の鐘（作り直し）', 'a large brass bell hanging from a wooden beam inside a dark bell tower room with stone walls all around, a small warm lantern flame glowing inside the bell where the clapper would be, soft light spilling from its mouth, no window, no sky', neg='sky, moon, window, clouds')
add('white_perm_cathedral', '大灯台', '洞窟の底の灯台', "a great brass lighthouse tower standing on a rock inside a huge dark underground cavern, rough stone cave ceiling with hanging stalactites above it, its lamp room blazing and a wide beam sweeping through the darkness, no sky, no clouds, no moon")
add('white_reaction_martyr', '灯の身代わり', 'ランタンが身代わりに砕けて癒しの光', "a square brass lantern on a stone ledge shattering as it takes an unseen blow, its glass panes bursting outward and a soft flood of warm healing light pouring out of the broken lantern, mine tunnel behind, close-up", neg=NOBODY)
add('white_catalyst_echo', '重ねる灯', '宿場の台に重なる二つの灯', "two identical square brass lanterns overlapping each other on the wooden counter of an inn, their warm lights merging with faint echo rings rippling out, close-up")
add('white_reaction_sanctuary', '灯りの庭', '岩天井の下の囲われた庭（作り直し）', 'a small walled stone garden enclosed on all sides by high stone walls under a rock ceiling, lit by many hanging brass lanterns, white flowers glowing in pools of warm light, peaceful, no sky', neg='sky, clouds, house, village, moon')

# ---- 点灯（召喚）と灯の列。人形は出さず「灯が入る瞬間」と「灯の並び」で ----
add('white_page_signal', '小人形の点灯', '火花が小さなランプの芯に飛ぶ（作り直し）', "close-up of a tiny candle-sized brass lamp on a dark workbench, a single spark jumping from a small brass flint-and-steel striker device lying beside it into the lamp's wick, the first tiny flame catching, no hands, no window", neg=NOBODY + ', window, moon')
add('white_calling', '点灯', '鍵を巻いてランタンに灯が入る', "a brass wind-up key turning in the base of a square brass lantern on a workbench, the lantern's flame just catching and its light spreading over the tools around it, close-up, no hands", neg='hands')
add('white_muster', '一斉点灯', '二つのランタンが同時に点く', "two square brass lanterns hanging side by side on a brass rail against a stone wall, both flaring on at the same instant from one spark, rays of warm light")
add('white_grand_rally', '大点灯', '坑道の大ランプ三つが一斉に点く', "three big brass lamps in a row along a mine tunnel all blazing on at once in a huge burst of warm light, the whole tunnel lit, rays")
add('white_shield_call', '盾人形の点灯', '光の板の奥でランタンが点く', "a square brass lantern igniting behind a translucent pane of warm light standing upright on stone steps, the pane brightening as the lantern lights, close-up")
add('white_maiden_prayer', '盾人形と灯り', '人形大の盾がランタンに寄りかかる', "a doll-sized round white shield with a brass rim propped against a glowing square brass lantern set on flagstones, both lit warm, quiet, close-up")
add('white_hound_whistle', '犬人形の点灯', '犬笛と首輪のランプが点く', "a brass dog collar with a small lantern on it lying on a wooden floor beside a brass whistle on a cord, the collar lantern flaring on with a puff of warm light, close-up")
add('white_march_order', '点灯の合図', '見張り台の合図のランタン（作り直し）', 'a brass signal lantern raised high on a wooden pole on a lookout post built of timber deep inside a dark cavern, flashing a bright burst of warm light with rays fanning out into the dark, rock ceiling above, no sky, no moon', neg='moon, sky, clouds')
add('white_holy_procession', '灯りの行列', '石段を下るランタンの列', "a line of square brass lanterns hanging from hooks along a descending stone staircase, lit one after another down into the dark, warm light on the steps")
add('white_rank_thrust', '灯列の突き', 'ランタンを付けた槍が突く（作り直し）', 'three brass spearheads with small square lanterns tied under them thrusting into the frame from the left edge toward the right in unison, motion lines, the lanterns blazing, dark mine tunnel behind, nobody holding them', neg=NOBODY + ', lamp post, street lamp, row of lamps')
add('white_miracle_division', '人形の分列', '一つの炎が二つに分かれる', "one lantern flame splitting into two identical flames inside a pair of matching square brass lanterns standing side by side on a workbench, a seam of bright light between them, close-up")
add('white_perm_martyr_vow', '灯の捧げ', 'ランタンを炉にくべて大ランプが輝く', "a small square brass lantern being fed into the open mouth of a brass furnace, its flame flowing out as a stream of warm light into a big lamp above the furnace, the big lamp blazing brighter, forge glow", neg='hands')
add('white_rally', '灯の集い', 'ランタンの輪の光が集まる', "a ring of many small square brass lanterns set on dark stone, all lit at once, their warm light beams converging into one bright point in the middle, close-up")
add('white_praise_chorus', '灯の輪', '床の光の輪とランタン', "a glowing ring of warm light drawn on the dark stone floor with small brass lanterns set at its points, light rising from the ring, seen from above at an angle")
add('white_grand_charge', '灯火の大行列', 'ランタンの列が坑道に続く', "a long line of lit square brass lanterns on tall brass poles receding into a dark mine tunnel, a river of warm amber light, no people visible")
add('white_perm_banner', '白銀の旗', '洞窟の岩に立つ白銀の旗', "a tall silver-white banner on a brass pole planted on a rocky outcrop in a cavern, its edges glowing warm amber, fluttering, close-up")
add('white_perm_war_banner', '灯火の旗印', '坑口の旗とランタン', "a war banner on a brass pole with a square brass lantern hung from its crossbar, planted at the mouth of a mine tunnel, glowing warm, fluttering, close-up")
add('white_war_horn', '灯火の号砲', '見張り塔の胸壁の角笛（作り直し）', 'a curved brass war horn with a small lantern set in its bell resting on a stone parapet of a mine watchtower, blasting out rings of warm light, dark rock behind, no sky, no moon', neg='moon, sky, hands')

# ---- 従者（人形）。白鉄と真鍮の小さな騎士。道具と動きが先・体は後ろの括弧・場を変える ----
add('white_perm_squire', '剣の人形', '剣を掲げて坑道を駆ける', KD + " charging forward to the right down a mine tunnel with timber supports, gripping a brass short sword raised in both hands" + BODY + ", motion, close-up", doll=True)
add('white_perm_shieldmaiden', '盾の人形', '石段で片膝をつき盾を構える', KD + " kneeling on one knee on stone steps, bracing a round white shield with a brass rim in both hands in front of its body" + BODY + ", close-up", doll=True)
add('white_perm_choir', '癒しの人形', '庭で光の器を掲げる', KD + " kneeling in a small garden of white flowers, holding up a brass bowl brimming with warm healing light in both hands, motes rising" + BODY + ", close-up", doll=True)
add('white_perm_banneret', '旗の人形', '岩の上で大きな旗を掲げる', KD + " standing on a rock, holding a huge white banner on a brass pole taller than the frame in both hands, the banner streaming in the wind" + BODY + ", close-up", doll=True)
add('white_perm_band', '鐘の人形', '鐘を鳴らす人形（正面・作り直し）', KD + " ringing a brass hand bell held up high in both hands, rings of warm light spreading through a stone corridor, three-quarter front view" + BODY + ", close-up", doll=True)
add('white_perm_bandleader', '大鐘の人形', '自分より大きな鐘を橇で引く', KD + " pulling a brass bell twice its own size on a small wooden sled with a rope in both hands, the bell glowing warm amber, minecart rails on the ground" + BODY + ", close-up", doll=True)
add('white_perm_hound', '犬の人形', 'からくり犬が軌道を走る', DOG + " running along minecart rails in a tunnel, motion streaks, close-up", doll=True, neg='cat, fox, wolf, fur, real dog')
add('white_perm_archer', '弩の人形', '木箱の上で弩を構える', KD + " crouching on top of a wooden crate, aiming a small brass crossbow loaded with a bolt of light held in both hands" + BODY + ", close-up", doll=True)
add('white_perm_monk', '手当ての人形', '割れたランタンに包帯を巻く', KD + " kneeling beside a cracked square lantern on a workshop floor, wrapping it in white bandage held in both hands, a small brass medicine bag open beside it" + BODY + ", close-up", doll=True)
add('white_perm_candle', '燭の人形', '蝋燭ひとつで暗い坑道を歩く', KD + " walking through a dark mine tunnel with a tall white candle in a brass holder carried in both hands, the candle flame the only light" + BODY + ", close-up", doll=True)
add('white_perm_page', '小さな人形', '作業台の上の手のひらサイズの人形（俯瞰）', "a tiny white iron knight doll the size of a cup seen from above, sitting on a wooden workbench among brass screws, a cog and a screwdriver" + BODY + ", its back lantern glowing softly, close-up", doll=True)
add('white_page_rank', '小人形の列', '梁の上を一列に歩く小さな人形二体', "two tiny " + KDS + " marching in single file along a wooden beam above a dark drop" + BODYS + ", close-up", doll=True)
add('white_knight_charge', '人形の総突撃', '人形の群れの突撃（低いアングル）', "a crowd of " + KDS + " seen from a low angle charging forward to the right down a tunnel with tiny brass swords raised" + BODYS + ", back lanterns blazing, dust and motion streaks, close-up", doll=True)

# ---- 工房レシピ産（白） ----
add('fusion_holy_stance', '灯火の構え', '立てた竿の灯が光の半球を張る（作り直し）', "a long dark iron lantern pole planted upright on flagstones with a square brass lantern at its top, the lantern's light spreading forward into a translucent half-dome barrier of warm light in front of the pole, a stone corridor behind, close-up, no hands", neg=NOBODY + ', street lamp, lamp post')
add('fusion_pray_or_guard', '灯すか守るか', '坑道の分かれ道: 光の道か門の道か', "a fork in a mine tunnel with a single square brass lantern on the ground between the two passages, the left passage bathed in soft warm light and the right passage barred by a heavy brass gate, close-up")
add('fusion_page_ambush', '小人形の伏兵', '木箱の陰から覗く小さな人形', "a tiny white iron knight doll peeking out from behind a wooden crate stacked in a dark tunnel, ready to leap" + BODY + ", its back lantern dimmed, close-up", doll=True)
add('fusion_archer_horn', '弩人形の号砲', '石壁の上の弩の人形と角笛', KD + " standing on top of a low stone wall, holding a small brass crossbow in both hands while a brass horn mounted beside it blasts rings of warm light, a spray of tiny bolts of light flying to the right" + BODY + ", close-up", doll=True)
add('fusion_retribution_blade', '報復の刃', '床に立つ光の刃が光を跳ね返す', "a short sword with a blade of solid amber light standing upright point-down in a stone floor by itself, a harsh beam of light striking the blade and reflecting back as a sharp flash, sparks, close-up, nobody holding it", neg=NOBODY)
add('fusion_chant_altar', '点灯の台座', '台座の上に光の盾', "a brass pedestal altar with a square lantern set on top, a round shield of warm light hovering above it, motes rising, close-up")

# 札ごとの seed (第2稿は 101/137 から。第1稿の 11/37 系と混ざらない)
for n, it in enumerate(items):
    it['seeds'] = [101 + 2 * n, 137 + 2 * n]

# ---- 白の再設計「灯」(2026-09-20 新規10。docs/white-redesign-proposal-2026-09-20.md) ----
def addl(id, name, ja, desc, seeds, doll=False, neg=None):
    add(id, name, ja, desc, doll=doll, neg=neg)
    items[-1]['seeds'] = seeds
addl('white_light_bolt', '灯の矢', 'ランタンの光が矢になって飛ぶ（灯の放出・初期デッキ）', "a square brass lantern on a short post in a dark mine tunnel, a single long arrow made of solid warm amber light shooting out of its open door toward the right, trailing sparks, by itself, nobody holding it, no bow, no archer, motion", [211, 233], neg=NOBODY)
addl('white_light_strike', '灯集めの一撃', '竿で打った衝撃で散った光の粒がランタンへ吸い戻される', "the square brass lantern at the tip of a long dark iron pole striking a mossy rock, and dozens of small motes of warm amber light streaming back into the lantern's open door like sparks drawn to it, close-up, motion, nobody holding the pole", [307, 331], neg=NOBODY)
addl('white_light_hoard', '灯り溜め', '光の壁の内側でランタンが光を溜める', "a small square brass lantern sitting on a flagstone floor inside a low curved wall of translucent warm amber light panes, the lantern's flame swelling and overflowing with layered warm glow, seen straight on, close-up, steady", [353, 379])
addl('white_perm_wick', '灯芯の人形', '芯を掲げて灯を分ける小さな人形（従者）', "{doll} holding up a long brass wick-trimmer with a tiny burning wick in both hands, lighting a small square brass lantern on a stone shelf in a mine tunnel, warm light spreading from the wick", [401, 427], doll=True)
addl('white_light_torrent', '光の奔流', 'ランタンから溢れた光が奔流になって右へ', "a square brass lantern on a stone ledge with its door burst open, a wide roaring torrent of warm amber light pouring out of it and rushing to the right like a flood through a dark mine tunnel, motion, by itself", [449, 467], neg=NOBODY)
addl('white_light_verdict', '光の裁き', '頭上から一条の強い光が石床を射る（眩む）', "a single thick vertical shaft of harsh white-gold light striking down from above onto a dark cobblestone floor in a stone hall, the floor glowing and cracking under the beam, dust lit in the beam, seen from the side, no figure", [491, 509], neg=NOBODY)
addl('white_reaction_light_wall', '灯守りの壁', '光の壁が受け止め、こぼれた光がランタンへ戻る', "a translucent pane of warm amber light standing upright as a wall in a mine tunnel, a dark spiked iron club striking it and stopping with a burst of sparks, while small motes of light run down the wall into a square brass lantern at its foot, motion, no figure", [523, 547], neg=NOBODY)
addl('white_perm_light_ballista', '灯の弩', '真鍮の弩が灯の粒を全方位へ放つ（置物）', "a squat brass ballista mounted on a stone pedestal with a small square lantern set at its center, spraying dozens of tiny darts of warm amber light in every direction, seen from the side at an angle, by itself, no crew", [563, 587], neg=NOBODY)
addl('white_light_double', '灯の倍化', '鏡でランタンの光が二つに', "a square brass lantern standing before a tall polished brass mirror on a wooden workshop table, its warm flame and its reflection both blazing so the light doubles and fills the frame, close-up, steady", [601, 617])
addl('white_light_burst', '眩光の大放出', 'ランタンが割れるほどの光が画面全体へ', "a square brass lantern at the center of a dark stone hall bursting apart, its glass panes shattering outward, an enormous flood of warm amber light exploding to fill the whole frame, shards and sparks flying, motion, no figure", [641, 659], neg=NOBODY)

# ---- 灯と人形の結び (2026-09-20 夜。card-power.md §74) 新規の人形2体。道具と動きが先・体は後ろの括弧 ----
addl('white_perm_lantern', '灯篭の人形', '大きな吊り灯篭を竿で掲げ、光が四方へ広がる（灯参照の人形）', KD + " standing at the center of a dark stone hall, hoisting a big square brass hanging lantern twice its own size on a short pole held up in both hands, the lantern blazing so bright that rays of warm amber light fan out to every side of the frame" + BODY + ", close-up", [677, 691], doll=True)
addl('white_perm_bonfire', '篝火の人形', '背中に篝火の籠を背負い炎が噴き上がる（固定の大出力の人形）', KD + " marching forward to the right through a dark mine tunnel, carrying a big iron fire basket full of roaring warm amber flames strapped on its back instead of a lantern, both hands gripping the basket's brass chains, embers and sparks streaming behind it" + BODY + ", motion, close-up", [703, 727], doll=True)

# ---- 火種と放出の軸 (2026-09-20 夜。本家 Soul の白版。card-power.md §75) 新規19＋トークン ----
addl('white_spark_token', '火種', '作業台の上に浮かぶ小さな燠の種（トークン）', "a single tiny glowing ember seed the size of a bean floating above a dark wooden workbench, warm amber light with a soft halo, a few drifting sparks, close-up, macro, nothing else", [743, 757], neg=NOBODY)
addl('white_spark_scatter', '火種撒き', '竿で床を打ち、燠の種が散る', "the brass-shod butt of a long dark iron pole striking a stone tunnel floor, a spray of dozens of tiny glowing ember seeds scattering across the floor to the right, motion, nobody holding it", [769, 787], neg=NOBODY)
addl('white_spark_shield', '火守りの盾', '盾の中心で燠の種を守る', "a round white shield with a brass rim leaning against a mine timber, a small glowing ember seed cradled at its center boss, a few sparks drifting, dark tunnel behind, close-up, no hands", [803, 821], neg=NOBODY)
addl('white_spark_burst', '火花散らし', '金床の火打ちから火花が散る', "a brass flint striker on a small workshop anvil throwing a burst of bright sparks to the right, several ember seeds tumbling through the air, dark workshop, close-up, no hands", [839, 853], neg=NOBODY)
addl('white_spark_kindling', '大焚き付け', '焚き付けの山から燠の種が立ちのぼる', "a heap of dry kindling twigs under a brass brazier catching fire, a rushing column of glowing ember seeds rising up and filling the upper frame, dark cellar, close-up", [871, 887], neg=NOBODY)
addl('white_perm_spark_furnace', '灯火の炉', '小さな真鍮の炉から燠の種がひとつ出てくる', "a small squat brass furnace on a stone floor with its little door open, a single glowing ember seed floating out of the door, warm glow inside, dark workshop, close-up", [901, 919])
addl('white_spark_kindle', '火起こし', 'ランタンの炎を藁に移し、種がふたつ立つ', "a square brass lantern tilted so its flame touches a nest of dry straw on a stone floor, two glowing ember seeds rising from the straw, dark tunnel, close-up, nobody holding it", [937, 953], neg=NOBODY)
addl('white_perm_spark_hearth', '灯の火床', '炭の火床から燠が暗い穴へ落ちる', "a stone hearth bed full of glowing coals under a brass grate, glowing ember seeds dripping from it down into a dark pit below, mine tunnel, close-up", [971, 989])
addl('white_spark_storm', '火種の嵐', '燠の嵐（作り直し2: 洞窟の絵は人物を呼ぶので接写に）', "extreme close-up of a square brass lantern on a dark wooden workbench, its flame erupting upward into a spiraling storm of thousands of glowing ember seeds that fills the whole frame, embers streaking in curves, dark background, macro, nothing else", [1441, 1463], neg=NOBODY + ', man, woman, cloak, cape, hood, traveler, wanderer, back view, standing figure, cave, cavern, tunnel')
addl('white_perm_embers', '火の粉', '灯から散った火の粉が岩に着く', "a square brass lantern on a rock ledge with its door open, small embers drifting down and landing on dark rocks where tiny flames catch, mine tunnel, close-up", [1039, 1051])
addl('white_perm_spark_relay', '灯の継ぎ手', '小さな灯の連なりが人形へ火を渡す', "a wire strung with a row of tiny brass lanterns lighting one after another, the last one passing its flame to " + KD + " standing at the end with a small brass torch raised in both hands" + BODY + ", close-up", [1069, 1087], doll=True)
addl('white_light_wall', '灯の壁', '灯の列から光の壁が立ち上がる', "a row of small square brass lanterns on a stone floor, a tall translucent wall of warm amber light rising from them, dark tunnel behind, close-up, no figure", [1103, 1117], neg=NOBODY)
addl('white_light_flash', '眩む閃光', '閃光が石の広間を白く染める', "a square brass lantern at the center of a dark stone hall releasing a blinding white-gold flash, rays whiting out the pillars, dust lit up, no figure", [1129, 1147], neg=NOBODY)
addl('white_light_forge', '灯の鍛冶', '灯の下で真鍮の槌が板を打つ', "a brass hammer resting on a small anvil beside a card-shaped white metal plate glowing at the edges, a square lantern above casting warm light, sparks, dark workshop, close-up, no hands", [1163, 1181], neg=NOBODY)
addl('white_light_ledger', '灯の手帳', '灯に照らされた真鍮綴じの手帳', "an open small notebook bound in brass on a wooden table, its pages fanning up as if turning by themselves, a square lantern beside it casting warm light, dark workshop, close-up, no hands", [1193, 1211], neg=NOBODY)
addl('white_perm_embers_last', '残り火', '炎の消えたランタンの底に残る燠', "a square brass lantern with its flame gone out, only a few embers glowing red-amber at its bottom, thin smoke rising, dark workshop, close-up", [1229, 1247])
addl('white_perm_great_furnace', '灯の大炉', '大炉から多くのランタンに火が入る', "a huge brass furnace with its door blazing, a dozen small square lanterns arranged before it each catching a warm flame, dark foundry, close-up, no figure", [1259, 1277], neg=NOBODY)
addl('white_light_maul', '灯の大槌', '灯を仕込んだ大槌が振り下ろされる', "a huge brass-headed maul with a small square lantern set into its head crashing down onto cracked stone, a burst of warm light and stone chips, motion, nobody holding it", [1291, 1303], neg=NOBODY)
addl('white_perm_fire_dish', '灯の火皿', '油の火皿と、めくれ上がる紙', "a shallow brass oil dish with a single flame on a stone shelf, a loose sheet of paper beside it lifting into the air on the heat, dark workshop, close-up, no hands", [1319, 1337], neg=NOBODY)


spec = {
 '_note': '白のカード挿絵 88 種 (2026-09-19 第2稿)。灯火の工房＝真鍮のランタン・暖色の光。札ごとに場と構図を変え、効果と名前を絵に出す。盾が主題は5枚まで。人形は従者11＋主題の4札だけ。生成器 cards-white-descriptions.py',
 'color': 'white', 'style': STYLE, 'doll': KD + BODY,
 'defaults': {'engine': 'pixflux', 'size': [80, 48], 'view': 'side', 'outline': 'selective outline', 'shading': 'detailed shading', 'detail': 'highly detailed', 'no_background': False, 'guidance': 8},
 'negative': NEG, 'negative_doll': NEG_DOLL, 'items': items}
out = os.path.join(ROOT, 'docs/pixellab/cards-white-descriptions.json')
json.dump(spec, open(out, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
cards = {c['id'] for c in json.load(open(os.path.join(ROOT, 'src/data/cards.white.json'), encoding='utf-8'))}
fus = {r['result']['id'] for r in json.load(open(os.path.join(ROOT, 'src/data/fusions.json'), encoding='utf-8')) if r['result']['id'].startswith('fusion_') and r['a'].startswith('white_')}
ids = [it['id'] for it in items]
print('items', len(items), 'dup', len(ids) - len(set(ids)), 'dolls', sum(1 for i in items if i.get('doll')))
print('missing cards', sorted(cards - set(ids)), 'missing fusions', sorted(fus - set(ids)), 'unknown', sorted(set(ids) - cards - fus))

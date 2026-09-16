# build.py — 全体のカラーテーマ (2026-09-16 ユーザー「全体のカラーテーマを一度決めたい」)。
# 現状の棚卸し (UI 12ファイルに 163 色の生の値・蜂蜜が6役・淡い6色が同じ明度) ＋ 4案 (A 墨と蜂蜜／B 黒鉄と真鍮／C 脈の光／D 朱と藍) ＋ 比較。
# 4案は同じ見本 (上部バー・敵と自分の帳面・手札2枚・ボタン・からくり・置物・状態の札) を同じ配置で描き、色だけを差し替える。
# 肌 (クリーム色の紙＋鉛筆の二重線＋水彩＋手書き風の文字) は 2026-09-07 の裁定で決まっているので触らない＝色の割り当てだけを決める。
# 使い方: python3 build.py → *.dc.html を書く → seed-canvas.mjs で束ねる (docs/design/deck-list と同じ手順)。
import base64, os
OUT = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(OUT, '..', '..', '..'))
ART_DIR = os.path.join(REPO, 'unity', 'Assets', 'Resources', 'Art')

def uri(path):
    with open(path, 'rb') as f: return 'data:image/png;base64,' + base64.b64encode(f.read()).decode('ascii')
def card_art(cid): return uri(os.path.join(ART_DIR, 'cards', cid + '.png'))
def icon(name): return uri(os.path.join(ART_DIR, 'icons', name + '.png'))
def ui_art(name): return uri(os.path.join(ART_DIR, 'ui', name + '.png'))

ICON = {k: icon(k) for k in ['heart', 'gold', 'energy', 'sword', 'shield', 'growth', 'set', 'exposed']}
ORB = ui_art('cost_orb'); GEM = {r: ui_art('gem_' + r) for r in ['common', 'uncommon', 'rare']}
KONOHA = uri(os.path.join(ART_DIR, 'leaders', 'leader_green_icon.png'))
ART = {k: card_art(k) for k in ['green_strike', 'green_reaction_vine', 'green_perm_thorn_vine']}

W = 1120

HEAD = '''<!doctype html>
<html>
<head>
  <meta charset="utf-8">
  <script src="./support.js"></script>
</head>
<body>
<x-dc>
<helmet>
  <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Kaisei+Decol:wght@400;700&amp;family=Klee+One:wght@400;600&amp;display=swap">
  <style>
    body { margin: 0; background: #0f1120; font-family: "Klee One", "Hiragino Sans", "Noto Sans JP", sans-serif; -webkit-font-smoothing: antialiased; }
    a { color: #7a4e12; } a:hover { color: #3b2f2f; }
    .abs { position: absolute; }
    .deco { font-family: "Kaisei Decol", "Hiragino Mincho ProN", serif; font-weight: 700; }
    .px { image-rendering: pixelated; }
    .note { background: #fff3ea; color: #9c3a2a; border: 1.5px solid #9c3a2a; border-radius: 6px; padding: 6px 10px; font-size: 14px; line-height: 19px; box-shadow: 0 4px 10px rgba(0,0,0,0.35); box-sizing: border-box; }
    .note b { color: #7a2418; }
    .cap { background: rgba(8,8,20,0.8); color: #f4ecd6; font-size: 14px; line-height: 20px; padding: 1px 10px; border-radius: 4px; letter-spacing: 0.04em; white-space: nowrap; }
  </style>
</helmet>
'''
TAIL = '''</x-dc>
</body>
</html>
'''

def write(name, html):
    with open(os.path.join(OUT, name + '.dc.html'), 'w', encoding='utf-8') as f: f.write(HEAD + html + TAIL)

def img(src, x, y, w, h, extra=''):
    return '<img class="px" src="%s" style="position: absolute; left: %spx; top: %spx; width: %spx; height: %spx; display: block; %s">' % (src, x, y, w, h, extra)
def ic(name, size=16, extra=''):
    return '<img class="px" src="%s" style="width: %dpx; height: %dpx; display: block; flex: none; %s">' % (ICON[name], size, size, extra)
def note(x, y, w, html, arrow=None):
    s = '<div class="abs note" style="left: %dpx; top: %dpx; width: %dpx; z-index: 60">%s</div>' % (x, y, w, html)
    if arrow:
        ax, ay, bx, by = arrow
        s += ('<svg class="abs" style="left: 0; top: 0; overflow: visible; pointer-events: none; z-index: 60" width="1" height="1"><line x1="%d" y1="%d" x2="%d" y2="%d" stroke="#9c3a2a" stroke-width="2.5" stroke-dasharray="6 4"></line><circle cx="%d" cy="%d" r="5" fill="#9c3a2a"></circle></svg>' % (ax, ay, bx, by, bx, by))
    return s
def caption(x, y, text):
    return '<div class="abs cap" style="left: %dpx; top: %dpx; z-index: 60">%s</div>' % (x, y, text)

# ================================================================ 4案のパレット (役割 → 色)
# 役割: paper/paper2/paper3 (紙3段)・ink/ink_soft (墨)・night/window (夜と札の窓)・accent/accent_light/accent_ink (主役=選択・決定・狙い)
#       accent2/accent2_ink (2つ目の主役。無い案は None)・energy/gold (資源)・hp/block/good/status (意味の色)・bad_ink (危険の墨)
#       types (タイプの帯 4)・colors (5色の縁)・rarity (レア度の外線 3)・seam (舞台の露頭の光。UI ではなく絵なので全案同じ)
SEAM = '#3aa79b'
P_A = dict(
    key='A', name='墨と蜂蜜', sub='いまの色を整える',
    principle='蜂蜜＝あなたの選択と資源（選択の縁・決定のボタン・G・エナジー）。意味の色（薔薇・空・苔・藤）は水彩のまま一段はっきり分ける。紙と墨は今のまま',
    tradeoff='蜂蜜が5役（選択・決定・G・エナジー・置物とレア）のまま。安全だが「いまと同じ」に見える',
    paper='#f4ecd6', paper2='#eadfc4', paper3='#fbf6e8', ink='#3b2f2f', ink_soft='#574b48', night='#1a1c33', window='#20233a',
    accent='#e0b25a', accent_light='#f6dd98', accent_ink='#7a4e12', accent2=None, accent2_ink=None,
    energy='#e0b25a', energy_ink='#7a4e12', gold='#e0b25a', gold_ink='#7a4e12',
    hp='#d46b6b', block='#6f9fd0', good='#7fae6b', good_ink='#276a34', bad_ink='#9c3a2a', status='#a98cc4', status_bg='#eddbf7', status_ink='#5a3d78',
    types=dict(physical='#8a6a3c', spell='#6c4f9c', reaction='#3f8c86', permanent='#b08a2e'),
    colors=dict(green='#5fb85a', blue='#4f8fd6', red='#d65a4f', white='#e8e2c8', black='#6b4f8a'),
    rarity=dict(common='#3b2f2f', uncommon='#5f86a8', rare='#c9963a'), orb_filter='',
)
P_B = dict(
    key='B', name='黒鉄と真鍮', sub='このは v2 と脈の光に UI を揃える',
    principle='真鍮＝価値（選択の縁・決定・G・レア）、脈の青緑＝マナ（エナジー・仕込み札・光る物）。墨は鉛筆の黒鉄色（青みの黒）、紙はクリームのまま少し白く',
    tradeoff='主役が2色になるので使い分けの規律が要る。墨と紙が少し冷えるぶん「絵本」の温かさが半歩引く',
    paper='#f3ecdc', paper2='#e6ddc9', paper3='#faf5ea', ink='#2f2e35', ink_soft='#57555e', night='#151926', window='#1d2231',
    accent='#c99a3a', accent_light='#ead08a', accent_ink='#6b4a10', accent2='#3aa79b', accent2_ink='#1c675f',
    energy='#3aa79b', energy_ink='#1c675f', gold='#c99a3a', gold_ink='#6b4a10',
    hp='#c9635a', block='#6f95b8', good='#7fa86c', good_ink='#2e6a3a', bad_ink='#9c3a2a', status='#9d86bf', status_bg='#e9def3', status_ink='#5a3d78',
    types=dict(physical='#7d6146', spell='#6c4f9c', reaction='#2f8a80', permanent='#a8842a'),
    colors=dict(green='#5fb85a', blue='#4f8fd6', red='#d65a4f', white='#e8e2c8', black='#6b4f8a'),
    rarity=dict(common='#2f2e35', uncommon='#5f86a8', rare='#c99a3a'), orb_filter='',
)
P_C = dict(
    key='C', name='脈の光', sub='青緑を全部の主役に',
    principle='脈のマナの青緑を一本の主役に: 選択の縁・決定・エナジー・仕込み札・斬撃の縁・舞台の露頭が同じ光。金は G だけに退く。紙はやや冷たいクリーム、墨は藍がかった黒',
    tradeoff='全体が冷える（絵本の温かさは薄い）。紙の上の青緑は文字にすると読めないので、文字は必ず濃い版を使う',
    paper='#f1eee4', paper2='#e4e0d2', paper3='#f9f7f0', ink='#2c3138', ink_soft='#535960', night='#0f1c24', window='#16303a',
    accent='#3fb7aa', accent_light='#bfe8e1', accent_ink='#14625a', accent2=None, accent2_ink=None,
    energy='#3fb7aa', energy_ink='#14625a', gold='#c4a04e', gold_ink='#7a5a12',
    hp='#e2735f', block='#5a86b8', good='#8bb85f', good_ink='#2e6a3a', bad_ink='#a3402c', status='#a98cc4', status_bg='#eddbf7', status_ink='#5a3d78',
    types=dict(physical='#a08659', spell='#6c4f9c', reaction='#2f8a80', permanent='#b08a2e'),
    colors=dict(green='#5fb85a', blue='#4f8fd6', red='#d65a4f', white='#e8e2c8', black='#6b4f8a'),
    rarity=dict(common='#2c3138', uncommon='#5f86a8', rare='#c4a04e'), orb_filter='hue-rotate(125deg) saturate(0.9)',
)
P_D = dict(
    key='D', name='朱と藍', sub='和紙に朱・藍・山吹',
    principle='朱＝決める（選択の縁・決定・狙い）、藍＝守りと情報（ブロック・仕込み札）、山吹＝資源（G・エナジー）。紙は白めの和紙、墨は無彩色の黒',
    tradeoff='朱が危険（HP・敗北）と同じ系統に見える。王道ファンタジーの世界に「和」の色が乗る＝手書き文字とは合うが敵の水彩とは別の国',
    paper='#f6efe0', paper2='#ebe2cf', paper3='#fcf8ee', ink='#2a2a2a', ink_soft='#4f4b48', night='#141b2f', window='#1c2540',
    accent='#d2553c', accent_light='#f3c2ae', accent_ink='#9c3a2a', accent2='#2f5a8a', accent2_ink='#244a74',
    energy='#e6b422', energy_ink='#7a5a12', gold='#e6b422', gold_ink='#7a5a12',
    hp='#c0504e', block='#2f5a8a', good='#6f9a5c', good_ink='#2e6a3a', bad_ink='#8f2f3b', status='#8a6aa8', status_bg='#e6dcf0', status_ink='#4a3068',
    types=dict(physical='#8a6a3c', spell='#6c4f9c', reaction='#2f5a8a', permanent='#b08a2e'),
    colors=dict(green='#5fb85a', blue='#4f8fd6', red='#d65a4f', white='#e8e2c8', black='#6b4f8a'),
    rarity=dict(common='#2a2a2a', uncommon='#5f86a8', rare='#e6b422'), orb_filter='',
)

# ================================================================ 決定版 (2026-09-16 ユーザー裁定: B／意味の色は一段濃く／リーダー色は出さない／墨だけ黒鉄・紙と夜は今のまま／玉と輪は青緑に描き直す)
# 中墨・各色の「墨」は紙の上で 7:1 前後 (2026-09-09 の読みやすさの規約)。帯の文字 (紙) は現状値を下回らない
P_FINAL = dict(P_B)
P_FINAL.update(
    key='決定', name='黒鉄と真鍮', sub='2026-09-16 決定',
    principle='真鍮＝価値（選択の縁・狙いの縁・決定のボタン・G・レアの外線・行動が変わる線）、脈の青緑＝マナ（エナジーの輪と数字・コスト玉・からくりの帯・光る物）。墨だけ鉛筆の黒鉄色、紙と夜は今のまま。文字は墨か各色の「墨」版だけ',
    tradeoff='',
    paper='#f4ecd6', paper2='#eadfc4', paper3='#fbf6e8', ink='#2f2e35', ink_soft='#4e4c55', night='#1a1c33', window='#20233a',
    accent='#c99a3a', accent_light='#ead08a', accent_ink='#634410', accent2='#3aa79b', accent2_ink='#155650',
    energy='#3aa79b', energy_ink='#155650', gold='#c99a3a', gold_ink='#634410',
    hp='#c9635a', block='#6f95b8', good='#7fa86c', good_ink='#276a34', bad_ink='#9c3a2a', status='#9d86bf', status_bg='#e9def3', status_ink='#5a3d78',
    types=dict(physical='#c9a982', spell='#a98cc4', reaction='#7ab8b0', permanent='#a8a66b'), band_ink=True,   # 実装どおり淡い帯＋墨の文字 (CardView のリボン)
    rarity=dict(common='#2f2e35', uncommon='#6f95b8', rare='#c99a3a'), orb_filter='hue-rotate(125deg) saturate(0.9)',
)
GROUND = '#0f1120'

# ================================================================ 見本の部品 (色は P から)
def paper_box(P, x, y, w, h, bg=None, r='10px 13px 9px 12px / 12px 9px 13px 10px', extra='', z=None):
    return '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; background: %s; color: %s; border-radius: %s; box-shadow: 0 0 0 1.5px %s, 0 4px 10px rgba(0,0,0,0.35); box-sizing: border-box; %s%s">' % (
        x, y, w, h, bg or P['paper'], P['ink'], r, P['ink'], extra, ('z-index: %d;' % z) if z is not None else '')
def tag(P, x, y, html, h=32, w=None, bg=None, size=15, gap=6):
    return '<div class="abs" style="left: %dpx; top: %dpx; height: %dpx; %s background: %s; color: %s; border-radius: 9px 12px 9px 11px / 11px 9px 12px 9px; box-shadow: 0 0 0 1.5px %s, 0 3px 8px rgba(0,0,0,0.35); display: flex; align-items: center; gap: %dpx; padding: 0 10px; font-size: %dpx; white-space: nowrap; box-sizing: border-box">%s</div>' % (
        x, y, h, ('width: %dpx; justify-content: center;' % w) if w else '', bg or P['paper'], P['ink'], P['ink'], gap, size, html)
def pill(P, text, ink, bg, size=13, h=22, icon_name=None):
    return '<span style="display: inline-flex; align-items: center; gap: 3px; height: %dpx; padding: 0 8px 0 6px; border-radius: 7px; background: %s; color: %s; box-shadow: 0 0 0 1px %s; font-size: %dpx; white-space: nowrap; flex: none">%s%s</span>' % (
        h, bg, ink, ink, size, ic(icon_name, 14) if icon_name else '', text)
def button(P, x, y, w, h, text, primary=False, size=17, danger=False):
    bg = P['accent_light'] if primary else ('#e8b8b0' if danger else P['paper'])
    fg = P['ink']
    return '<div class="abs deco" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; background: %s; color: %s; border-radius: 10px 12px 9px 11px / 11px 9px 12px 10px; box-shadow: 0 0 0 1.5px %s, 0 3px 8px rgba(0,0,0,0.35); display: flex; align-items: center; justify-content: center; font-size: %dpx; letter-spacing: 0.12em; box-sizing: border-box">%s</div>' % (
        x, y, w, h, bg, fg, P['ink'], size, text)
def hpbar(P, x, y, w, h, frac, label, tick=None, size=13):
    s = '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; background: %s; box-shadow: 0 0 0 2px %s; border-radius: 2px">' % (x, y, w, h, P['paper'], P['ink'])
    s += '<div class="abs" style="left: 2px; top: 2px; width: %dpx; height: %dpx; background: linear-gradient(%s, %s)"></div>' % (int((w - 4) * frac), h - 4, P['hp'], P['hp'])
    if tick is not None:
        tx = int(w * tick)
        s += '<div class="abs" style="left: %dpx; top: -3px; width: 6px; height: %dpx; background: %s"><div class="abs" style="left: 1.5px; top: 1px; width: 3px; height: %dpx; background: %s"></div></div>' % (tx - 3, h + 6, P['ink'], h + 4, P['accent'])
    s += '<div class="abs" style="inset: 0; display: flex; align-items: center; justify-content: center; font-size: %dpx; color: %s; text-shadow: 0 0 2px %s, 0 0 2px %s">%s</div>' % (size, P['ink'], P['paper'], P['paper'], label)
    return s + '</div>'

def card(P, x, y, cid, name, cost, ctype, tja, body, rar, scale=0.74, sel=False, z=None):
    edge = P['rarity'][rar]
    ring = '' if not sel else ', 0 0 0 7px %s, 0 0 18px %s' % (P['accent'], P['accent'])
    s = '<div class="abs" style="left: %dpx; top: %dpx; width: 200px; height: 290px; transform: scale(%s); transform-origin: 0 0; %s">' % (x, y, scale, ('z-index: %d;' % z) if z is not None else '')
    s += '<div class="abs" style="inset: 0; background: %s; border-radius: 8px 10px 8px 9px / 9px 8px 10px 8px; box-shadow: 0 0 0 1.5px %s, 0 0 0 4px %s, 0 0 0 5.5px %s, 0 6px 14px rgba(0,0,0,0.45)%s"></div>' % (P['paper'], P['ink'], P['paper'], edge, ring)
    s += '<div class="abs" style="left: 12px; top: 44px; right: 12px; height: 100px; background: %s; box-shadow: 0 0 0 1.5px %s">%s</div>' % (P['window'], P['ink'], img(ART[cid], 8, 2, 160, 96))
    s += '<div class="abs" style="left: -6px; top: -6px; width: 52px; height: 52px">%s<div class="abs deco" style="inset: 0; display: flex; align-items: center; justify-content: center; font-size: 22px; padding-bottom: 2px; color: %s">%s</div></div>' % (img(ORB, 0, 0, 52, 52, ('filter: %s;' % P['orb_filter']) if P['orb_filter'] else ''), P['ink'], cost)
    s += '<div class="abs deco" style="left: 44px; top: 8px; right: 12px; height: 32px; display: flex; align-items: center; justify-content: center; font-size: %dpx; white-space: nowrap; color: %s">%s</div>' % (20 if len(name) <= 4 else 17, P['ink'], name)
    s += '<div class="abs" style="left: 25px; top: 136px; width: 150px; height: 26px; background: %s; border-radius: 4px; box-shadow: 0 0 0 1.5px %s; display: flex; align-items: center; justify-content: center; gap: 5px; color: %s; font-size: 14px; letter-spacing: 0.15em">%s%s</div>' % (P['types'][ctype], P['ink'], P['ink'] if P.get('band_ink') else P['paper'], img(GEM[rar], 0, 0, 24, 24, 'position: static'), tja)
    s += '<div class="abs" style="left: 14px; top: 172px; right: 14px; bottom: 14px; text-align: center; font-size: 16px; line-height: 24px; color: %s">%s</div>' % (P['ink'], body)
    return s + '</div>'

def swatch(x, y, hexv, label, ink_on, w=62, h=40, light_edge='#3b2f2f'):
    """色見本1枚: 色の板 (淡い色は墨の縁) と役割の名と値"""
    return ('<div class="abs" style="left: %dpx; top: %dpx; width: %dpx">' % (x, y, w) +
            '<div style="width: %dpx; height: %dpx; background: %s; border-radius: 6px; box-shadow: 0 0 0 1px %s"></div>' % (w, h, hexv, light_edge) +
            '<div style="font-size: 13px; line-height: 16px; color: %s; margin-top: 4px; white-space: nowrap">%s</div>' % (ink_on, label) +
            '<div style="font-size: 13px; line-height: 15px; color: %s; opacity: 0.85; white-space: nowrap">%s</div></div>' % (ink_on, hexv))

def group(x, y, title, items, ink_on, edge):
    """役割の群: 見出し＋見本の列。items = [(hex, label), ...]。幅を返す"""
    s = '<div class="abs" style="left: %dpx; top: %dpx; font-size: 13px; letter-spacing: 0.12em; color: %s; opacity: 0.8">%s</div>' % (x, y, ink_on, title)
    for i, (hx, lb) in enumerate(items):
        s += swatch(x + i * 72, y + 20, hx, lb, ink_on, light_edge=edge)
    return s, len(items) * 72 + 30

# ================================================================ 見本 (戦闘の一場面) — 1120×350 の夜の帯
def hud(P):
    s = '<div class="abs" style="left: 0; top: 0; width: %dpx; height: 350px; background: linear-gradient(180deg, %s 0%%, %s 70%%, %s 100%%); overflow: hidden">' % (W, P['night'], P['night'], P['window'])
    # 舞台の露頭の光 (絵の側の色。全案同じ青緑) と地面の帯
    s += '<div class="abs" style="left: 0; top: 296px; width: %dpx; height: 54px; background: linear-gradient(180deg, rgba(0,0,0,0) 0%%, rgba(58,167,155,0.18) 45%%, rgba(0,0,0,0) 100%%)"></div>' % W
    s += '<div class="abs" style="left: 420px; top: 318px; width: 280px; height: 3px; border-radius: 2px; background: %s; box-shadow: 0 0 14px %s, 0 0 30px %s; opacity: 0.85"></div>' % (SEAM, SEAM, SEAM)
    s += '<div class="abs cap" style="left: 700px; top: 322px; font-size: 12px; opacity: 0.8">舞台の露頭の光 (絵の色・全案同じ)</div>'
    # 上部バー
    s += '<div class="abs" style="left: 0; top: 0; width: %dpx; height: 56px; background: linear-gradient(rgba(0,0,0,0.5), rgba(0,0,0,0))"></div>' % W
    s += '<div class="abs" style="left: 22px; top: 8px; width: 40px; height: 40px; border-radius: 50%%; background: %s; box-shadow: 0 0 0 1.5px %s; overflow: hidden">%s</div>' % (P['window'], P['ink'], img(KONOHA, 4, 4, 32, 32))
    s += tag(P, 70, 11, '<span style="font-size: 13px; letter-spacing: 0.1em; color: %s">幕 1 · 行 16</span><span class="deco" style="font-size: 18px">脳筋オーガ</span>' % P['ink_soft'], h=34)
    s += tag(P, 300, 11, '<span style="font-size: 13px; letter-spacing: 0.1em; color: %s">ターン</span><span class="deco" style="font-size: 17px; font-weight: 400">3</span>' % P['ink_soft'], h=34)
    s += tag(P, 470, 11, '<span class="deco" style="font-size: 15px; font-weight: 400; letter-spacing: 0.2em">あなたの番</span>', h=32, w=150, bg=P['paper3'])
    s += tag(P, 900, 11, '%s<span class="deco" style="font-size: 17px; font-weight: 400">50</span><span style="font-size: 13px; color: %s">G</span>' % (ic('gold', 16), P['ink_soft']), h=34)
    s += tag(P, 1010, 11, '<span style="font-size: 20px">≡</span>', h=34, w=44)
    # 自分の帳面 (左)
    s += paper_box(P, 30, 130, 300, 96) + hpbar(P, 12, 10, 276, 18, 80 / 90, '80 / 90')
    s += '<div class="abs" style="left: 12px; top: 36px; font-size: 14px; line-height: 20px; color: %s">被ダメ <b style="color: %s">14</b> − <span style="color: %s">盾 5</span> ＝ HP <b style="color: %s">−9</b> → 71</div>' % (P['ink_soft'], P['bad_ink'], P['block'] if P['key'] != 'A' else '#2f5a7a', P['bad_ink'])
    s += '<div class="abs" style="left: 12px; top: 62px; display: flex; gap: 6px">%s%s%s</div>' % (
        pill(P, '3 / 4 エナジー', P['energy_ink'], mix(P['energy'], P['paper'], 0.35), icon_name='energy'),
        pill(P, '成長 +2', P['good_ink'], mix(P['good'], P['paper'], 0.35), icon_name='growth'),
        pill(P, '弱体 3', P['status_ink'], P['status_bg'], icon_name='exposed'))
    s += '</div>'
    # からくり (仕込み札) のトークンと置物の付箋
    s += paper_box(P, 30, 240, 68, 74, r='6px') + '<div class="abs" style="left: 4px; top: 4px; width: 60px; height: 40px; background: %s; box-shadow: 0 0 0 1px %s; overflow: hidden">%s</div>' % (P['window'], P['ink'], img(ART['green_reaction_vine'], -10, -2, 80, 48))
    s += '<div class="abs" style="left: 0; bottom: 0; width: 68px; height: 24px; background: %s; color: %s; font-size: 12px; display: flex; align-items: center; justify-content: center; border-radius: 0 0 6px 6px; letter-spacing: 0.08em">準備中</div></div>' % (P['accent2'] or P['types']['reaction'], P['paper'])
    s += paper_box(P, 110, 246, 176, 40, bg=P['paper2'], r='6px') + '<div class="abs" style="left: 4px; top: 4px; width: 48px; height: 32px; background: %s; overflow: hidden">%s</div><div class="abs" style="left: 60px; top: 0; height: 40px; display: flex; align-items: center; font-size: 15px">棘の蔓</div></div>' % (P['window'], img(ART['green_perm_thorn_vine'], -16, -8, 80, 48))
    s += '<div class="abs cap" style="left: 30px; top: 320px; font-size: 12px; opacity: 0.8">からくり＝仕込み札の帯 / 置物の付箋</div>'
    # 手札 2枚 (打撃=選択中、守りの蔓)
    s += card(P, 400, 118, 'green_strike', '打撃', 1, 'physical', '物理', 'ダメージ<b style="font-size: 130%">6</b>', 'common', sel=True, z=5)
    s += card(P, 560, 132, 'green_reaction_vine', '守りの蔓', 1, 'reaction', '仕込み札', '<span style="font-size: 13px; line-height: 17px; color: %s">被攻撃前:</span><br>ブロック<b style="font-size: 130%%">12</b>' % P['ink_soft'], 'common')
    # 敵の帳面 (右・ボス幅)
    s += paper_box(P, 760, 128, 330, 96)
    s += '<div class="abs deco" style="left: 10px; top: 6px; font-size: 16px">脳筋オーガ</div>'
    s += '<div class="abs" style="right: 8px; top: 6px; display: flex; gap: 4px">%s%s</div>' % (pill(P, '筋力+1', P['ink'], P['paper2'], icon_name='sword'), pill(P, '装甲25', P['ink_soft'], P['paper2'], icon_name='shield'))
    s += hpbar(P, 10, 36, 310, 16, 1.0, '181 / 181', tick=0.5)
    s += '<div class="abs" style="left: 10px; top: 60px; display: flex; align-items: center; gap: 8px">%s<span class="deco" style="font-size: 24px">10</span>%s</div>' % (ic('sword', 26), pill(P, 'HP90以下で攻撃8〜10×2', P['accent_ink'], P['accent_light'], icon_name='sword'))
    s += '</div>'
    # 浮き文字 (与ダメ・被ダメ)
    s += '<div class="abs deco" style="left: 880px; top: 84px; font-size: 30px; color: %s; text-shadow: 0 0 3px %s, 0 0 3px %s, 2px 2px 0 %s">18</div>' % (P['paper'], P['ink'], P['ink'], P['ink'])
    s += '<div class="abs deco" style="left: 150px; top: 92px; font-size: 26px; color: %s; text-shadow: 0 0 3px %s, 0 0 3px %s">−9</div>' % ('#f0a090' if P['key'] != 'D' else '#f3a89c', P['ink'], P['ink'])
    # ボタン
    s += button(P, 900, 244, 190, 50, 'ターン終了', primary=True)
    s += button(P, 760, 250, 120, 40, '閉じる', size=15)
    s += '<div class="abs cap" style="left: 760px; top: 300px; font-size: 12px; opacity: 0.8">決定のボタン＝主役の色の薄い紙 / 選択中の札＝主役の縁</div>'
    return s + '</div>'

def mix(a, b, t):
    """a と b を t で混ぜる (t=1 で a)。色見本の淡い地に使う"""
    def h2(c): return [int(c[i:i+2], 16) for i in (1, 3, 5)]
    A, B = h2(a), h2(b)
    return '#%02x%02x%02x' % tuple(int(A[i] * t + B[i] * (1 - t)) for i in range(3))

# ================================================================ パレット表 — 夜の帯の下に紙を敷いて色を並べる
def palette(P, y0=350):
    H = 410
    s = '<div class="abs" style="left: 0; top: %dpx; width: %dpx; height: %dpx; background: %s"></div>' % (y0, W, H, P['paper'])
    ink = P['ink']
    s += '<div class="abs deco" style="left: 30px; top: %dpx; font-size: 24px; color: %s">案%s　%s <span style="font-weight: 400; font-size: 15px; color: %s">— %s</span></div>' % (y0 + 14, ink, P['key'], P['name'], P['ink_soft'], P['sub'])
    s += '<div class="abs" style="left: 30px; top: %dpx; width: 1060px; font-size: 14px; line-height: 20px; color: %s">%s</div>' % (y0 + 46, ink, P['principle'])
    s += '<div class="abs" style="left: 30px; top: %dpx; width: 1060px; font-size: 13px; line-height: 18px; color: %s">落とし穴: %s</div>' % (y0 + 70, P['bad_ink'], P['tradeoff'])
    x = 30; y = y0 + 100
    g, w = group(x, y, '紙と墨', [(P['paper'], '紙'), (P['paper2'], '紙 (濃)'), (P['paper3'], '紙 (明)'), (P['ink'], '墨'), (P['ink_soft'], '中墨')], ink, ink); s += g; x += w
    g, w = group(x, y, '夜', [(P['night'], '舞台の夜'), (P['window'], '札の窓')], ink, ink); s += g; x += w
    main = [(P['accent'], '主役'), (P['accent_light'], '主役 (紙)'), (P['accent_ink'], '主役の墨')]
    if P['accent2']: main += [(P['accent2'], '2つ目'), (P['accent2_ink'], '2つ目の墨')]
    g, w = group(x, y, '主役の色 (選択・決定・狙い)', main, ink, ink); s += g; x += w
    g, w = group(x, y, '資源', [(P['energy'], 'エナジー'), (P['gold'], 'G')], ink, ink); s += g; x += w
    x = 30; y = y0 + 200
    g, w = group(x, y, '意味の色', [(P['hp'], 'HP・与ダメ'), (P['block'], 'ブロック'), (P['good'], '成長・良い'), (P['good_ink'], '良いの墨'), (P['status'], '状態異常'), (P['status_bg'], '状態の紙'), (P['bad_ink'], '危険の墨')], ink, ink); s += g; x += w
    g, w = group(x, y, 'レア度の外線', [(P['rarity']['common'], 'C 墨'), (P['rarity']['uncommon'], 'U 空'), (P['rarity']['rare'], 'R')], ink, ink); s += g; x += w
    x = 30; y = y0 + 300
    t = P['types']
    g, w = group(x, y, 'タイプの帯', [(t['physical'], '物理'), (t['spell'], '呪文'), (t['reaction'], '仕込み札'), (t['permanent'], '置物')], ink, ink); s += g; x += w
    c = P['colors']
    g, w = group(x, y, '5色の縁 (リーダー・札の色)', [(c['green'], '緑'), (c['blue'], '青'), (c['red'], '赤'), (c['white'], '白'), (c['black'], '黒')], ink, ink); s += g; x += w
    g, w = group(x, y, '絵の光', [(SEAM, '露頭・斬撃の縁')], ink, ink); s += g
    return s

def board_dir(P):
    s = '<div style="position: relative; width: %dpx; height: 760px; overflow: hidden; background: %s">' % (W, P['night'])
    s += hud(P) + palette(P)
    return s + '</div>'

# ================================================================ 決定版の板 (Main)
TOKENS = [
    ('紙と墨', [
        ('紙', 'paper', P_FINAL['paper'], 'パネル・札・ボタンの地'),
        ('紙 (濃)', 'paper2', P_FINAL['paper2'], '置物の付箋・状態の札・しおりの帯'),
        ('紙 (明)', 'paper3', P_FINAL['paper3'], '手番の札・見出しの札・決定後の紙'),
        ('墨', 'ink', P_FINAL['ink'], '文字・線・C の外線（11.4:1）'),
        ('中墨', 'inkSoft', P_FINAL['ink_soft'], '二次の文字＝注記・単位（7.2:1）'),
    ]),
    ('夜', [
        ('舞台の夜', 'night', P_FINAL['night'], '舞台の背景・メニューの幕'),
        ('札の窓', 'window', P_FINAL['window'], '札の挿絵の窓・からくりの窓'),
        ('地', 'ground', GROUND, '画面の外・最奥'),
    ]),
    ('真鍮＝価値', [
        ('真鍮', 'brass', P_FINAL['accent'], '選択と狙いの縁・G・R の外線・行動が変わる線'),
        ('真鍮の紙', 'brassLight', P_FINAL['accent_light'], '決定のボタン・予告の札の地'),
        ('真鍮の墨', 'brassInk', P_FINAL['accent_ink'], '予告の文字・G の数字・注意書き（7.5:1）'),
    ]),
    ('青緑＝マナ', [
        ('脈の青緑', 'mana', P_FINAL['accent2'], 'エナジーの輪・コスト玉・からくりの帯・斬撃・露頭'),
        ('青緑の紙', 'manaLight', '#b5ddd6', 'エナジーのピルの地'),
        ('青緑の墨', 'manaInk', P_FINAL['accent2_ink'], 'エナジーの数字・からくりの文字（7.2:1）'),
    ]),
    ('意味の色', [
        ('薔薇', 'rose', P_FINAL['hp'], 'HP バー・与ダメの札・敗北'),
        ('鋼青', 'sky', P_FINAL['block'], 'ブロック・盾のピル（U の外線は据え置き）'),
        ('苔', 'moss', P_FINAL['good'], '成長・割引の玉'),
        ('良いの墨', 'goodInk', P_FINAL['good_ink'], '鍛えた数字・割引のコスト・上がった数字'),
        ('危険の墨', 'badInk', P_FINAL['bad_ink'], '被ダメの数字・エラー・敗因'),
        ('藤', 'plum', P_FINAL['status'], '状態異常の印'),
        ('藤の紙', 'plumLight', P_FINAL['status_bg'], '状態異常のピルの地'),
        ('藤の墨', 'plumInk', P_FINAL['status_ink'], '状態異常の文字（6.8:1）'),
        ('危険のボタン', 'dangerBtn', '#e8b8b0', '「ランを放棄」だけ'),
    ]),
    ('タイプの帯 (淡い色＋墨の文字)', [
        ('物理 砂', 'Sand', P_FINAL['types']['physical'], '据え置き（墨 6.1:1）'),
        ('呪文 藤', 'PlumBand', P_FINAL['types']['spell'], '据え置き（墨 4.6:1）'),
        ('仕込み札 青緑', 'Teal', P_FINAL['types']['reaction'], '据え置き＝マナの淡い版（墨 6.0:1）'),
        ('置物 鈍い黄', 'Olive', P_FINAL['types']['permanent'], '蜂蜜→オリーブ。真鍮の縁と混ざらない（墨 5.3:1）'),
    ]),
]
CHANGES = [
    ('墨', '#3b2f2f', P_FINAL['ink'], '焦げ茶 → 鉛筆の黒鉄'), ('中墨', '#574b48', P_FINAL['ink_soft'], '7:1 を保って黒鉄側へ'),
    ('主役 (蜂蜜→真鍮)', '#e0b25a', P_FINAL['accent'], '選択・狙い・G・R・目盛り'), ('決定のボタン', '#f6dd98', P_FINAL['accent_light'], '真鍮の紙'), ('金の文字', '#7a4e12', P_FINAL['accent_ink'], '真鍮の墨 7.5:1'),
    ('エナジー', '#e0b25a', P_FINAL['energy'], '青緑へ（玉と輪は描き直し）'), ('HP', '#d97b7b', P_FINAL['hp'], '一段濃く'), ('ブロック', '#7fa7c9', P_FINAL['block'], '一段濃く・青へ'),
    ('成長', '#8fae7b', P_FINAL['good'], '一段濃く・緑へ'), ('状態異常', '#a98cc4', P_FINAL['status'], '一段濃く'), ('置物の帯', '#e0b25a', P_FINAL['types']['permanent'], '蜂蜜 → 鈍い黄（真鍮と分ける）'),
]

def board_main():
    P = P_FINAL
    H = 1380
    s = '<div style="position: relative; width: %dpx; height: %dpx; overflow: hidden; background: %s">' % (W, H, P['paper'])
    s += hud(P)
    ink = P['ink']
    y = 366
    s += '<div class="abs deco" style="left: 30px; top: %dpx; font-size: 26px; color: %s">カラーテーマ　黒鉄と真鍮 <span style="font-weight: 400; font-size: 15px; color: %s">— 2026-09-16 決定。肌（クリーム色の紙・鉛筆の二重線・水彩・手書き文字）は 2026-09-07 のまま</span></div>' % (y, ink, P['ink_soft'])
    s += '<div class="abs" style="left: 30px; top: %dpx; width: 1060px; font-size: 14px; line-height: 20px; color: %s">%s</div>' % (y + 38, ink, P['principle'])
    # 役割→色の表 (2列)
    y0 = y + 76
    col_x = [30, 580]
    rows_per_col = [TOKENS[:4], TOKENS[4:]]
    for ci, groups in enumerate(rows_per_col):
        x = col_x[ci]; yy = y0
        for title, items in groups:
            s += '<div class="abs" style="left: %dpx; top: %dpx; font-size: 13px; letter-spacing: 0.12em; color: %s">%s</div>' % (x, yy, P['ink_soft'], title)
            yy += 20
            for label, key, hx, use in items:
                s += '<div class="abs" style="left: %dpx; top: %dpx; width: 44px; height: 22px; background: %s; border-radius: 4px; box-shadow: 0 0 0 1px %s"></div>' % (x, yy + 1, hx, ink)
                s += '<div class="abs" style="left: %dpx; top: %dpx; width: 92px; font-size: 14px; line-height: 24px; color: %s; white-space: nowrap">%s</div>' % (x + 52, yy, ink, label)
                s += '<div class="abs" style="left: %dpx; top: %dpx; width: 70px; font-size: 13px; line-height: 24px; color: %s; white-space: nowrap">%s</div>' % (x + 146, yy, P['ink_soft'], hx)
                s += '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; font-size: 13px; line-height: 24px; color: %s; white-space: nowrap; overflow: hidden; text-overflow: ellipsis">%s</div>' % (x + 218, yy, 322 if ci == 0 else 292, ink, use)
                yy += 26
            yy += 10
    # 規律
    ry = y0 + 500
    rules = [
        '① 真鍮は「価値」に限る: 選択中の札と狙っている敵の縁・決定のボタン（真鍮の紙）・G・R の外線・HP バーの「行動が変わる線」・勢いと急所の印。置物の帯は鈍い黄（オリーブ）で真鍮から離す',
        '② 青緑は「マナ」に限る: エナジーの輪と数字・コスト玉・からくり（仕込み札）の帯とトークン・斬撃の縁・舞台の露頭。割引の玉は苔のまま',
        '③ 文字は墨か各色の「墨」版だけ（中墨 7:1・真鍮の墨 7.5:1・青緑の墨 7.2:1）。淡い色は塗りにだけ使う（2026-09-09 の規約を継承）',
        '④ 意味の色は4つだけ: 薔薇＝HP／鋼青＝ブロック／苔＝成長・良い／藤＝状態異常。危険は「危険の墨」の文字で言い、朱の塗りは作らない',
        '⑤ リーダーの色（5色）は UI に出さない。札の縁・リーダー札・敵の絵柄など「世界の側」だけ',
        '⑥ 夜は3つ（舞台・札の窓・地）で固定。紙＝暖・夜＝寒の対比がこの UI の芯',
        '⑦ 新しい色は足さない。足したくなったら上の表の役割に当てる（当たらないなら役割を1つ増やして表に書く）',
    ]
    s += '<div class="abs deco" style="left: 30px; top: %dpx; font-size: 18px; color: %s">使い分けの規律</div>' % (ry, ink)
    for i, r in enumerate(rules):
        s += '<div class="abs" style="left: 30px; top: %dpx; width: 1060px; font-size: 13px; line-height: 18px; color: %s">%s</div>' % (ry + 28 + i * 22, ink, r)
    # 現状からの変更
    cy = ry + 28 + len(rules) * 22 + 16
    s += '<div class="abs deco" style="left: 30px; top: %dpx; font-size: 18px; color: %s">現状からの変更 (11)　<span style="font-weight: 400; font-size: 13px; color: %s">据え置き: 紙3・夜3・5色の縁・呪文の帯・良い/危険の墨・U の外線・状態の墨</span></div>' % (cy, ink, P['ink_soft'])
    for i, (label, a, b, why) in enumerate(CHANGES):
        col, row = divmod(i, 7)
        x = 30 + col * 570; yy = cy + 30 + row * 26
        s += '<div class="abs" style="left: %dpx; top: %dpx; width: 120px; font-size: 13px; line-height: 24px; color: %s; white-space: nowrap">%s</div>' % (x, yy, ink, label)
        s += '<div class="abs" style="left: %dpx; top: %dpx; width: 30px; height: 20px; background: %s; border-radius: 3px; box-shadow: 0 0 0 1px %s"></div>' % (x + 124, yy + 2, a, ink)
        s += '<div class="abs" style="left: %dpx; top: %dpx; font-size: 13px; line-height: 24px; color: %s">%s → </div>' % (x + 160, yy, P['ink_soft'], a)
        s += '<div class="abs" style="left: %dpx; top: %dpx; width: 30px; height: 20px; background: %s; border-radius: 3px; box-shadow: 0 0 0 1px %s"></div>' % (x + 246, yy + 2, b, ink)
        s += '<div class="abs" style="left: %dpx; top: %dpx; font-size: 13px; line-height: 24px; color: %s; white-space: nowrap">%s　<span style="color: %s">%s</span></div>' % (x + 282, yy, ink, b, P['ink_soft'], why)
    return s + '</div>'

# ================================================================ 現状の棚卸し
def board_current():
    s = '<div style="position: relative; width: %dpx; height: 720px; overflow: hidden; background: #0f1120">' % W
    s += '<img src="current-boss.jpg" style="position: absolute; left: 60px; top: 56px; width: 1000px; height: 461px; display: block; border-radius: 4px; box-shadow: 0 0 0 1.5px #3b2f2f">'
    s += caption(60, 18, '現状 (2026-09-16 35fc04a・スマホ相当・幕1ボス)。肌 (紙・二重線・水彩・手書き文字) は決定済み＝決めるのは「色の割り当て」')
    s += note(14, 330, 250, '<b>①</b> 蜂蜜 (#e0b25a) が6役: 選択の縁・決定のボタン・G・エナジーの玉・置物の帯・レアの外線。何が「あなたの選択」で何が「資源」か色では読めない', (264, 380, 350, 320))
    s += note(1076 - 250 + 40, 250, 250, '<b>②</b> 意味の色6つ (薔薇・空・苔・藤・青緑・砂) がほぼ同じ明度と彩度。札の小さな帯やピルでは 空 (ブロック) と青緑 (仕込み札) と藤 (呪文) が見分けにくい', (1076 - 250 + 40, 300, 780, 330))
    s += note(300, 540, 380, '<b>③</b> UI 12ファイルに 163 色の生の値 (`Hex("#…")`)。PaperFx / UiKit / Theme の3か所に同じ紙・墨・蜂蜜が別名で重複し、蜂蜜だけで7つの近い値 (下の帯)。一度「役割→色」の表を決めれば、ここを1か所に畳める')
    s += note(700, 540, 380, '<b>④</b> 夜は3つの紺 (#1a1c33 舞台・#20233a 札の窓・#0f1120 地) がなんとなく使い分けられている。紙＝暖・夜＝寒 の対比がこの UI の芯なので、夜の色も表に載せて決める')
    s += '<div class="abs" style="left: 60px; top: 540px; width: 220px; font-size: 13px; line-height: 17px; color: #f4ecd6; opacity: 0.9">「蜂蜜のつもり」の7色 (実装値):</div>'
    for i, hx in enumerate(['#e0b25a', '#f6dd98', '#f0d58a', '#e0b84a', '#f0c33c', '#c9963a', '#e0a04a']):
        s += '<div class="abs" style="left: %dpx; top: 566px; width: 28px; height: 28px; background: %s; border-radius: 4px; box-shadow: 0 0 0 1px #f4ecd6"></div>' % (60 + i * 32, hx)
    s += '<div class="abs" style="left: 60px; top: 600px; width: 220px; font-size: 13px; line-height: 17px; color: #f4ecd6; opacity: 0.9">「淡い6色」(同じ明度帯):</div>'
    for i, hx in enumerate(['#d97b7b', '#7fa7c9', '#8fae7b', '#a98cc4', '#7ab8b0', '#c9a982']):
        s += '<div class="abs" style="left: %dpx; top: 626px; width: 28px; height: 28px; background: %s; border-radius: 4px; box-shadow: 0 0 0 1px #f4ecd6"></div>' % (60 + i * 32, hx)
    s += caption(60, 680, '4案は下。見本は同じ場面 (上部バー・自分と敵の帳面・手札2枚・からくり・置物・ボタン) を同じ配置で描き、色だけを差し替えている')
    return s + '</div>'

# ================================================================ 比較
def board_compare():
    rows = [
        ('紙の温度', 'クリーム (今のまま)', 'クリーム・少し白く', 'やや冷たいクリーム', '白めの和紙'),
        ('墨', '焦げ茶 (今のまま)', '鉛筆の黒鉄 (青みの黒)', '藍がかった黒', '無彩色の黒'),
        ('主役 (選択・決定・狙い)', '蜂蜜', '真鍮', '脈の青緑', '朱'),
        ('2つ目の主役', '無し', '脈の青緑 (マナ)', '無し (金は G だけ)', '藍 (守りと情報)'),
        ('エナジーの色', '蜂蜜 (G と同じ)', '青緑 (G と分かれる)', '青緑 (G と分かれる)', '山吹 (G と同じ)'),
        ('世界観との整合', '○ 絵本', '◎ 黒鉄と真鍮・脈の光・絵本', '○ 脈の光 (冷たい)', '△ 和 (敵の水彩と別の国)'),
        ('蜂蜜6役の解消', '× 5役のまま', '○ 価値／マナに2分', '○ 光／金に2分', '○ 決める／守る／資源に3分'),
        ('移行の手間', '小 (値の整理だけ)', '中 (墨と2色目の規律)', '中〜大 (玉・玉の絵・演出の色)', '大 (朱の絵・和の統一)'),
        ('落とし穴', '「いまと同じ」に見える', '2色の使い分けの規律', '全体が冷える・紙の上の青緑の文字', '朱が危険と同じ系統'),
    ]
    s = '<div style="position: relative; width: %dpx; height: 640px; overflow: hidden; background: #f4ecd6; color: #3b2f2f">' % W
    s += '<div class="abs deco" style="left: 30px; top: 18px; font-size: 24px">4案の比較</div>'
    s += '<div class="abs" style="left: 30px; top: 56px; width: 1060px; font-size: 14px; line-height: 20px; color: #574b48">どの案でも: 肌 (紙・二重線・水彩・手書き文字) は不変／文字は「墨」か各色の「墨」版だけ (紙の上の淡い色は塗りにしか使わない＝2026-09-09 の読みやすさの規約)／5色の縁とレア度の外線は据え置き／絵 (ドット絵・舞台の光) は塗り替えない</div>'
    cols = [(30, 190), (220, 200), (430, 230), (670, 220), (900, 200)]
    heads = ['', 'A 墨と蜂蜜', 'B 黒鉄と真鍮', 'C 脈の光', 'D 朱と藍']
    y = 110
    for j, h in enumerate(heads):
        s += '<div class="abs deco" style="left: %dpx; top: %dpx; width: %dpx; font-size: 16px; border-bottom: 1.5px solid #3b2f2f; padding-bottom: 6px">%s</div>' % (cols[j][0], y, cols[j][1] - 10, h)
    y += 40
    for r in rows:
        for j, cell in enumerate(r):
            s += '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; font-size: 14px; line-height: 19px; %s">%s</div>' % (cols[j][0], y, cols[j][1] - 10, 'color: #574b48' if j == 0 else '', cell)
        y += 42
    s += '<div class="abs" style="left: 30px; top: %dpx; width: 1060px; background: #fbf6e8; box-shadow: 0 0 0 1.5px #3b2f2f; border-radius: 8px; padding: 10px 14px; font-size: 14px; line-height: 20px; box-sizing: border-box"><b>推奨は B</b>。決定済みの世界 (このは v2＝黒鉄と真鍮・脈のマナの青緑・舞台の露頭と斬撃の縁が既に青緑) に UI の色を揃え、いちばんの問題「蜂蜜6役」をエナジーとGの分離で解く。紙と肌はそのまま＝移行は色の表の差し替えで済む。A は B の「墨と2色目を変えない版」として残す</div>' % (y + 6)
    return s + '</div>'

if __name__ == '__main__':
    write('Main', board_main())
    write('Current', board_current())
    for P in (P_A, P_B, P_C, P_D): write('Dir' + P['key'], board_dir(P))
    write('Compare', board_compare())
    import json
    canvas = dict(
        pages=[dict(id='decided', name='決定'), dict(id='options', name='検討 (4案と比較)')],
        artboards=[
            dict(file='Main.dc.html', x=0, y=0, w=W, h=1380, title='カラーテーマ 決定版', page='decided'),
            dict(file='Current.dc.html', x=0, y=0, w=W, h=720, title='現状の棚卸し', page='options'),
            dict(file='DirA.dc.html', x=0, y=860, w=W, h=760, title='案A 墨と蜂蜜', page='options'),
            dict(file='DirB.dc.html', x=1220, y=860, w=W, h=760, title='案B 黒鉄と真鍮 (採択)', page='options'),
            dict(file='DirC.dc.html', x=0, y=1760, w=W, h=760, title='案C 脈の光', page='options'),
            dict(file='DirD.dc.html', x=1220, y=1760, w=W, h=760, title='案D 朱と藍', page='options'),
            dict(file='Compare.dc.html', x=0, y=2660, w=W, h=640, title='比較と推奨', page='options'),
        ],
        annotations=[
            dict(id='decided-note', x=1220, y=0, w=520, page='decided', text='2026-09-16 裁定 (ask_user 5件): 案B／意味の色は一段濃く／リーダーの色は UI に出さない／墨だけ黒鉄・紙と夜は今のまま／コスト玉とエナジーの輪は青緑に描き直す。\n一次資料は docs/color-theme.md。実装は PaperFx を唯一の出典にして 163 の生の値を畳む。'),
            dict(id='brief', x=1220, y=0, w=520, page='options', text='全体のカラーテーマ (2026-09-16)\n決めるのは「役割→色」の割り当て。肌 (紙・二重線・水彩・手書き文字) は 2026-09-07 の裁定のまま。\n4案は同じ場面を同じ配置で描いて色だけ差し替え。'),
        ],
        launch=dict(view='canvas', page='decided'),
    )
    with open(os.path.join(OUT, 'canvas.json'), 'w', encoding='utf-8') as f: json.dump(canvas, f, ensure_ascii=False, indent=1)
    print('ok')

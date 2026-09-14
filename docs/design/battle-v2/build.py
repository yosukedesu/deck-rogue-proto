# build.py — 戦闘画面の見直し (2026-09-15 ユーザー「現在の戦闘画面についてより良くする案を出して。パーツ配置などを大幅に見直しても問題ない」)。
# 現状の指摘 (スマホ・PC) ＋ 配置の3案 (A 額縁の柱／B 看板の列と作業台／C 帳面の一行) をスマホ (S25 相当 1462×675) と PC (1920×1080) の両方で、
# ＋ 各案の確認ウィンドウ (発動/温存) ＋ 比較と推奨。
# 下地は UI を全部消した Unity のスクショ (Autopilot の hideui=1)。舞台と絵はそのまま、上に重ねた紙の物だけが提案。
# 座標はキャンバス単位 (1 unit = 1 CSS px)。UI の寸法 (札 200×290・トークン 68×74・付箋 168×40・文字 13px 以上) は Unity の実装値と同じ。
import base64, os
OUT = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(OUT, '..', '..', '..'))
ART_DIR = os.path.join(REPO, 'unity', 'Assets', 'Resources', 'Art')

INK = '#3b2f2f'; INK_SOFT = '#574b48'; PAPER = '#f4ecd6'; PAPER2 = '#eadfc4'; HONEY = '#e0b25a'; GOLD_INK = '#7a4e12'
ROSE = '#d97b7b'; SKY = '#7fa7c9'; MOSS = '#8fae7b'; MOSS_DEEP = '#3f8a4a'; GREY = '#8a8a94'; NIGHT = '#1a1c33'; BAD_INK = '#9c3a2a'; PLUM_INK = '#5a3d78'
TYPE_COL = dict(physical='#8a6a3c', spell='#6c4f9c', reaction='#3f8c86', permanent='#b08a2e')

def uri(path):
    with open(path, 'rb') as f: return 'data:image/png;base64,' + base64.b64encode(f.read()).decode('ascii')
def card_art(cid): return uri(os.path.join(ART_DIR, 'cards', cid + '.png'))
def icon(name): return uri(os.path.join(ART_DIR, 'icons', name + '.png'))
def ui_art(name): return uri(os.path.join(ART_DIR, 'ui', name + '.png'))
def enemy_art(eid): return uri(os.path.join(ART_DIR, 'enemies', eid + '.png'))
def leader_art(lid): return uri(os.path.join(ART_DIR, 'leaders', lid + '.png'))
def relic_art(rid): return uri(os.path.join(ART_DIR, 'relics', rid + '.png'))

ART = {k: card_art(k) for k in ['green_strike', 'green_entangle', 'green_basic_bash', 'green_reaction_vine', 'green_vine_wedge', 'green_reaction_thorns', 'green_perm_growth_tree', 'green_perm_thorn_vine']}
ICON = {k: icon(k) for k in ['intent_attack', 'intent_defend', 'sword', 'shield', 'set', 'growth', 'skull', 'energy', 'draw', 'exhaust', 'gold', 'heart', 'question', 'chest', 'crest_permanent', 'momentum']}
ORB = ui_art('cost_orb'); GEM = ui_art('gem_common')
PROBE = enemy_art('enemy_probe'); BRUTE = enemy_art('enemy_brute'); KONOHA = leader_art('leader_green')
RELICS = [relic_art('relic_abacus'), relic_art('relic_amplifier_draught')]

# ---- 見本の盤面 (全案で同じ) ----
# 幕1・行3 探り屋の三人組・ターン2。このは HP 71/80・ブロック5・成長2・エナジー 3/4・山札5・捨て札4・消滅0。
# からくり: 守りの蔓 (前のターンに仕込んだ=今ターン鳴る・あと2回)／茨の返し (今ターン仕込んだ=準備中)。置物: 年輪の大樹・棘の蔓。
# 敵: ①探り屋 14/18 ⚔4 (狙っている)／②探り屋 16/16 🛡8＋筋力+1／③探り屋 19/19 ⚔13。全員 筋力-3 (群れ補正)。被ダメ 4+13=17 − 盾5 = HP−12 → 59
ENEMIES = [
    dict(name='探り屋', hp=14, mx=18, kind='attack', val=4, rider=None, aimed=True),
    dict(name='探り屋', hp=16, mx=16, kind='defend', val=8, rider=('sword', '筋力+1', '#7a5a1a', '#faebc7'), aimed=False),
    dict(name='探り屋', hp=19, mx=19, kind='attack', val=13, rider=None, aimed=False),
]
HAND = [
    dict(id='green_strike', name='打撃', cost=1, type='physical', body='ダメージ<b>8</b>', mod=True),
    dict(id='green_entangle', name='絡み蔦', cost=1, type='physical', body='<span class="small">どちらか一つ</span><br>◆ブロック<b>7</b><br>◆ダメージ<b>9</b>', mod=True),
    dict(id='green_basic_bash', name='打ち据え', cost=1, type='physical', body='ダメージ<b>6</b><br>急所+<b>2</b>', mod=True),
    dict(id='green_reaction_vine', name='守りの蔓', cost=1, type='reaction', body='被攻撃前: ブロック<b>12</b><br>完全に凌げば次のターン<b>1</b>ドロー'),
    dict(id='green_vine_wedge', name='蔦の楔', cost=1, type='physical', body='敵のブロックを全て壊す<br>ダメージ<b>7</b>', mod=True),
]
PLAYER = dict(hp=71, mx=80, block=5, growth=2, energy=3, emax=4, draw=5, discard=4, exhaust=0, incoming=17)
PERMS = [('green_perm_growth_tree', '年輪の大樹', '毎ターン開始時: 成長+1'), ('green_perm_thorn_vine', '棘の蔓', '攻撃をプレイするたび: ブロック2')]

# ---- スマホ (1462×675) の下地の座標 (絵の位置は舞台が決める。UI 無しのスクショから読んだ値) ----
PH = dict(
    leader=dict(x0=251, x1=396, top=209, feet=324, cx=324), box=dict(x0=430, x1=484, top=297, feet=339),
    enemies=[dict(x0=851, x1=952, top=177, feet=299, cx=902), dict(x0=1043, x1=1127, top=156, feet=282, cx=1085), dict(x0=1241, x1=1333, top=168, feet=270, cx=1287)],
    boss=dict(x0=956, x1=1142, top=110, feet=293, cx=1049),
)
# ---- PC (1920×1080) ----
PC = dict(
    leader=dict(x0=320, x1=540, top=420, feet=600, cx=430), box=dict(x0=565, x1=635, top=580, feet=640),
    enemies=[dict(x0=1120, x1=1250, top=400, feet=530, cx=1185), dict(x0=1355, x1=1470, top=350, feet=520, cx=1412), dict(x0=1660, x1=1775, top=360, feet=520, cx=1717)],
)

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
    body { margin: 0; background: #0f1120; font-family: "Klee One", "Hiragino Sans", "Noto Sans JP", sans-serif; color: #3b2f2f; -webkit-font-smoothing: antialiased; }
    a { color: #7a4e12; } a:hover { color: #3b2f2f; }
    .abs { position: absolute; }
    .deco { font-family: "Kaisei Decol", "Hiragino Mincho ProN", serif; font-weight: 700; }
    .px { image-rendering: pixelated; }
    .paper { background: #f4ecd6; color: #3b2f2f; border-radius: 10px 13px 9px 12px / 12px 9px 13px 10px; box-shadow: 0 0 0 1.5px #3b2f2f, 0 4px 10px rgba(0,0,0,0.35); box-sizing: border-box; }
    .tag { background: #f4ecd6; color: #3b2f2f; border-radius: 9px 12px 9px 11px / 11px 9px 12px 9px; box-shadow: 0 0 0 1.5px #3b2f2f, 0 3px 8px rgba(0,0,0,0.35); display: flex; align-items: center; gap: 6px; height: 30px; padding: 0 10px; font-size: 15px; white-space: nowrap; box-sizing: border-box; }
    .night { background: rgba(26,28,51,0.88); color: #f4ecd6; border-radius: 6px; box-shadow: 0 0 0 1px rgba(244,236,214,0.35); box-sizing: border-box; font-size: 13px; line-height: 17px; padding: 3px 8px; white-space: nowrap; }
    .note { background: #fff3ea; color: #9c3a2a; border: 1.5px solid #9c3a2a; border-radius: 6px; padding: 6px 10px; font-size: 14px; line-height: 19px; box-shadow: 0 4px 10px rgba(0,0,0,0.35); box-sizing: border-box; }
    .note b { color: #7a2418; }
    .small { font-size: 13px; line-height: 17px; color: #574b48; }
    .num { font-size: 130%; font-weight: 700; }
    .card b { font-size: 130%; font-weight: 700; }
    .dim { filter: brightness(0.55) saturate(0.8); }
  </style>
</helmet>
'''
TAIL = '''</x-dc>
</body>
</html>
'''

def board(bg, w, h, scale=1.0, dx=0, dy=0, dim=False):
    s = '<div style="position: relative; width: %dpx; height: %dpx; overflow: hidden; background: #0f1120">' % (w, h)
    if bg:
        s += '<img src="%s" style="position: absolute; left: %dpx; top: %dpx; width: %dpx; height: %dpx; display: block%s">' % (bg, dx, dy, round(w * scale), round(h * scale), '; filter: brightness(0.6)' if dim else '')
    return s
def close(): return '</div>'

def img(src, x, y, w, h, extra=''):
    return '<img class="px" src="%s" style="position: absolute; left: %spx; top: %spx; width: %spx; height: %spx; display: block; %s">' % (src, x, y, w, h, extra)

def note(x, y, w, html, arrow=None, big=False):
    s = '<div class="abs note" style="left: %dpx; top: %dpx; width: %dpx; z-index: 60%s">%s</div>' % (x, y, w, '; font-size: 16px; line-height: 22px' if big else '', html)
    if arrow:
        ax, ay, bx, by = arrow
        s += ('<svg class="abs" style="left: 0; top: 0; overflow: visible; pointer-events: none; z-index: 60" width="1" height="1"><line x1="%d" y1="%d" x2="%d" y2="%d" stroke="#9c3a2a" stroke-width="2.5" stroke-dasharray="6 4"></line><circle cx="%d" cy="%d" r="5" fill="#9c3a2a"></circle></svg>' % (ax, ay, bx, by, bx, by))
    return s
def label(x, y, text, size=14, color='#f4ecd6'):
    return '<div class="abs" style="left: %dpx; top: %dpx; color: %s; font-size: %dpx; letter-spacing: 0.06em; text-shadow: 0 0 3px #000, 0 0 6px #000, 0 1px 0 #000; white-space: nowrap">%s</div>' % (x, y, color, size, text)
def caption(x, y, text):
    return '<div class="abs" style="left: %dpx; top: %dpx; z-index: 60; background: rgba(8,8,20,0.8); color: #f4ecd6; font-size: 14px; line-height: 20px; padding: 1px 10px; border-radius: 4px; letter-spacing: 0.04em; white-space: nowrap">%s</div>' % (x, y, text)

# ---- 共通の部品 ----

def tag(x, y, html, h=30, w=None, bg=PAPER, rot=0, size=15):
    return '<div class="abs tag" style="left: %dpx; top: %dpx; height: %dpx; %s background: %s; transform: rotate(%sdeg); font-size: %dpx">%s</div>' % (x, y, h, ('width: %dpx;' % w) if w else '', bg, rot, size, html)
def ic(name, size=16, extra=''):
    return '<img class="px" src="%s" style="width: %dpx; height: %dpx; display: block; flex: none; %s">' % (ICON[name], size, size, extra)

def topbar(w, phone, enc='探り屋の三人組', turn=2, act=1, row=3, gold=50, relics=True, extra_center=None):
    s = tag(26, 13, '<span class="small" style="letter-spacing: 0.1em">幕 %d · 行 %d</span><span class="deco" style="font-size: 19px">%s</span>' % (act, row, enc), h=36, rot=-0.6)
    s += tag(26 + (250 if phone else 250), 15, '%s<span>ターン %d</span>' % (ic('set', 16), turn), h=32, rot=1)
    if extra_center: s += extra_center
    gx = w - (262 if phone else 640)
    s += tag(gx, 15, '%s<span class="deco" style="font-size: 18px">%d</span><span class="small">G</span>' % (ic('gold', 16), gold), h=34)
    if relics:
        for i, r in enumerate(RELICS):
            s += '<div class="abs" style="left: %dpx; top: 15px; width: 34px; height: 34px; border-radius: 50%%; background: #f4ecd6; box-shadow: 0 0 0 1.5px #3b2f2f">%s</div>' % (gx + 96 + i * 40, img(r, 7, 7, 20, 20))
    if not phone:
        for i, t in enumerate(['マップ', 'ログ', 'メモ', 'レポート']):
            s += tag(gx + 190 + i * 92, 15, t, h=34, w=84, size=13)
            s = s.replace('>%s</div>' % t, '><span style="width: 100%%; text-align: center">%s</span></div>' % t, 1)
    s += tag(w - 52, 15, '≡', h=34, w=40, size=20)
    return s

def hpbar(x, y, w, hp, mx, h=18, block=None, size=13):
    frac = hp / mx
    s = '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; background: #2a2020; border-radius: 4px; box-shadow: 0 0 0 1.5px #3b2f2f, 0 2px 4px rgba(0,0,0,0.4)">' % (x, y, w, h)
    s += '<div class="abs" style="left: 0; top: 0; bottom: 0; width: %d%%; background: %s; border-radius: 4px"></div>' % (round(frac * 100), ROSE)
    s += '<div class="abs deco" style="inset: 0; display: flex; align-items: center; justify-content: center; color: #fff; font-size: %dpx; font-weight: 400; text-shadow: 0 0 2px #000, 0 1px 0 #000">%d / %d</div></div>' % (size, hp, mx)
    if block:
        s += '<div class="abs deco" style="left: %dpx; top: %dpx; height: %dpx; padding: 0 8px 0 4px; display: flex; align-items: center; gap: 3px; background: %s; color: #fff; border-radius: 10px; box-shadow: 0 0 0 1.5px #3b2f2f; font-size: %dpx; text-shadow: 0 1px 0 #000">%s%d</div>' % (x - 14, y - 6, h + 12, SKY, size + 2, ic('shield', 16), block)
    return s

def chip(x, y, iconname, text, h=24, size=13, bg=PAPER, color=INK, w=None):
    return '<div class="abs" style="left: %dpx; top: %dpx; height: %dpx; %s padding: 0 8px 0 5px; display: flex; align-items: center; gap: 4px; background: %s; color: %s; border-radius: 8px 10px 8px 9px; box-shadow: 0 0 0 1.5px #3b2f2f, 0 2px 5px rgba(0,0,0,0.35); font-size: %dpx; white-space: nowrap; box-sizing: border-box">%s%s</div>' % (x, y, h, ('width: %dpx;' % w) if w else '', bg, color, size, ic(iconname, 16), text)

def pill(iconname, text, ink, paper, size=13):
    return '<span style="display: inline-flex; align-items: center; gap: 3px; height: 22px; padding: 0 7px 0 4px; border-radius: 7px; background: %s; color: %s; box-shadow: 0 0 0 1px %s; font-size: %dpx; white-space: nowrap">%s%s</span>' % (paper, ink, ink, size, ic(iconname, 14), text)

def intent_html(kind, val, rider=None, icon_size=32, num_size=24, compact=True):
    s = '<span style="display: inline-flex; align-items: center; gap: 8px">'
    s += ic('intent_attack' if kind == 'attack' else 'intent_defend', icon_size)
    s += '<span class="deco" style="font-size: %dpx; line-height: 1">%s</span>' % (num_size, val)
    if rider: s += pill(rider[0], rider[1], rider[2], rider[3])
    return s + '</span>'

def ring(cx, feet, w=120, h=30, color=HONEY):
    return '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; border-radius: 50%%; border: 3px solid %s; box-sizing: border-box; box-shadow: 0 0 8px %s, inset 0 0 6px %s"></div>' % (cx - w // 2, feet - h // 2 - 4, w, h, color, color, color)

def energy_orb(x, y, size=128, energy=3, emax=4):
    deg = round(360 * energy / emax)
    s = '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; transform: rotate(-3deg)">' % (x, y, size, size)
    s += '<div class="abs" style="inset: 0; border-radius: 50%%; background: conic-gradient(%s 0deg %ddeg, rgba(224,178,90,0.18) %ddeg 360deg)"></div>' % (HONEY, deg, deg)
    s += '<div class="abs" style="inset: 7px; border-radius: 50%; background: #f4ecd6; box-shadow: 0 0 0 1.5px #3b2f2f, 0 4px 10px rgba(0,0,0,0.4)"></div>'
    s += '<div class="abs deco" style="inset: 0; display: flex; align-items: center; justify-content: center; font-size: %dpx; color: #3b2f2f; padding-right: 22px; padding-bottom: 6px">%d</div>' % (round(size * 0.31), energy)
    s += '<div class="abs" style="left: 55%%; top: 46%%; font-size: 15px; color: #574b48">/ %d</div>' % emax
    s += '<div class="abs" style="left: 0; right: 0; bottom: %dpx; text-align: center; font-size: 13px; color: #574b48; letter-spacing: 0.15em">エナジー</div></div>' % round(size * 0.17)
    return s

def pile(x, y, iconname, count, lab, w=130, count2=None, lab2=None, rot=-1):
    s = '<div class="abs tag" style="left: %dpx; top: %dpx; width: %dpx; height: 40px; transform: rotate(%sdeg); gap: 6px; padding: 0 12px">%s<span class="deco" style="font-size: 17px">%d</span><span class="small">%s</span>' % (x, y, w, rot, ic(iconname, 16), count, lab)
    if count2 is not None:
        s += '<span style="width: 1.5px; height: 14px; background: #574b48"></span><span class="deco" style="font-size: 17px">%d</span><span class="small">%s</span>' % (count2, lab2)
    return s + '</div>'

def end_turn(x, y, w=230, h=64, size=21, rot=-1):
    return '<div class="abs deco" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; background: #f6dd98; border-radius: 12px 14px 11px 13px / 13px 11px 14px 12px; box-shadow: 0 0 0 1.5px #3b2f2f, 0 4px 10px rgba(0,0,0,0.4); display: flex; align-items: center; justify-content: center; font-size: %dpx; letter-spacing: 0.2em; transform: rotate(%sdeg); box-sizing: border-box">ターン終了</div>' % (x, y, w, h, size, rot)

def button(x, y, w, h, text, size=17, primary=False):
    return '<div class="abs deco" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; background: %s; border-radius: 10px 12px 9px 11px / 11px 9px 12px 10px; box-shadow: 0 0 0 1.5px #3b2f2f, 0 3px 8px rgba(0,0,0,0.35); display: flex; align-items: center; justify-content: center; font-size: %dpx; letter-spacing: 0.12em; box-sizing: border-box">%s</div>' % (x, y, w, h, '#f6dd98' if primary else PAPER, size, text)

# ---- カードの面 (200×290・Unity の CardView と同じ骨格) ----
def card(x, y, d, scale=1.0, rot=0, raised=False, playable=True, z=None):
    tc = TYPE_COL[d['type']]
    typ = dict(physical='物理', spell='呪文', reaction='リアクション', permanent='置物')[d['type']]
    body = d['body']
    if d.get('mod'):
        body = body.replace('<b>', '<b style="color: #276a34">', 1) if False else body   # 実値は緑にしない (成長込みの数字は本文の1か所)
    s = '<div class="abs card" style="left: %dpx; top: %dpx; width: 200px; height: 290px; transform: rotate(%sdeg) scale(%s); transform-origin: 50%% 100%%; %s">' % (x, y, rot, scale, ('z-index: %d;' % z) if z is not None else '')
    s += '<div class="abs paper" style="inset: 0; border-radius: 8px 10px 8px 9px / 9px 8px 10px 8px; box-shadow: 0 0 0 1.5px #3b2f2f, 0 0 0 4px #f4ecd6, 0 0 0 5px rgba(59,47,47,0.55), 0 6px 14px rgba(0,0,0,0.45)%s"></div>' % ('' if playable else '; filter: brightness(0.85)')
    s += '<div class="abs" style="left: 12px; top: 44px; right: 12px; height: 100px; background: #20233a; box-shadow: 0 0 0 1.5px #3b2f2f">%s</div>' % img(ART[d['id']], 8, 2, 160, 96)
    s += '<div class="abs" style="left: -6px; top: -6px; width: 52px; height: 52px">%s<div class="abs deco" style="inset: 0; display: flex; align-items: center; justify-content: center; font-size: 22px; padding-bottom: 2px">%s</div></div>' % (img(ORB, 0, 0, 52, 52), d['cost'])
    ns = 20 if len(d['name']) <= 4 else 18 if len(d['name']) == 5 else 17
    s += '<div class="abs deco" style="left: 44px; top: 8px; right: 12px; height: 32px; display: flex; align-items: center; justify-content: center; font-size: %dpx; white-space: nowrap">%s</div>' % (ns, d['name'])
    s += '<div class="abs" style="left: 25px; top: 136px; width: 150px; height: 26px; background: %s; border-radius: 4px; box-shadow: 0 0 0 1.5px #3b2f2f; display: flex; align-items: center; justify-content: center; gap: 5px; color: #f4ecd6; font-size: 14px; letter-spacing: 0.15em">%s%s</div>' % (tc, img(GEM, 0, 0, 24, 24, 'position: static'), typ)
    s += '<div class="abs" style="left: 14px; top: 172px; right: 14px; bottom: 14px; text-align: center; font-size: 16px; line-height: 24px">%s</div>' % body
    return s + '</div>'

def hand(x0, y, spacing=180, scale=1.0, raised=None, rot_spread=0.0, dim=False):
    s = ''
    n = len(HAND)
    for i, d in enumerate(HAND):
        rot = (i - (n - 1) / 2) * rot_spread
        r = raised == i
        s += card(x0 + i * spacing, y - (44 if r else 0), d, scale=scale * (1.06 if r else 1), rot=rot, playable=not dim, z=10 if r else i)
    if dim: s = '<div class="abs" style="left: 0; top: 0; width: 0; height: 0; filter: brightness(0.55) saturate(0.8); overflow: visible">' + s + '</div>'
    return s

# ---- からくりのトークン (68×74) と置物 ----
STATE = {
    'prep': dict(band=GREY, text='準備中', edge='#6a6a74', glow=None, badge=None),
    'live': dict(band='#7a6a3a', text='あと2回', edge=HONEY, glow='rgba(224,178,90,0.45)', badge='2'),
    'now': dict(band=MOSS_DEEP, text='今ターン', edge='#f0d58a', glow='rgba(240,213,138,0.75)', badge='2'),
    'keep': dict(band='#7a6a3a', text='期限なし', edge=HONEY, glow='rgba(224,178,90,0.45)', badge='∞'),
}
def token(x, y, cid, state, scale=1.0):
    st = STATE[state]
    w, h = 68 * scale, 74 * scale
    s = '<div class="abs" style="left: %spx; top: %spx; width: %spx; height: %spx">' % (x, y, w, h)
    if st['glow']: s += '<div class="abs" style="inset: -4px; border-radius: 10px; background: %s; filter: blur(4px)"></div>' % st['glow']
    s += '<div class="abs paper" style="inset: 0; border-radius: 6px; box-shadow: 0 0 0 2px %s, 0 3px 8px rgba(0,0,0,0.4)"></div>' % st['edge']
    s += img(ART[cid], 2 * scale, 2 * scale, round(64 * scale), round(38 * scale), 'border-radius: 3px; box-shadow: 0 0 0 1px %s' % INK)
    s += '<div class="abs deco" style="left: 0; right: 0; bottom: 0; height: %spx; border-radius: 0 0 6px 6px; background: %s; color: #f4ecd6; font-size: %spx; display: flex; align-items: center; justify-content: center; letter-spacing: 0.02em; font-weight: 400">%s</div>' % (28 * scale, st['band'], round(15 * scale), st['text'])
    if st['badge']:
        s += '<div class="abs deco" style="right: %spx; top: %spx; width: %spx; height: %spx; border-radius: 50%%; background: #f4ecd6; color: #3b2f2f; box-shadow: 0 0 0 1.5px #3b2f2f; font-size: %spx; display: flex; align-items: center; justify-content: center">%s</div>' % (-7 * scale, -7 * scale, 20 * scale, 20 * scale, round(13 * scale), st['badge'])
    return s + '</div>'
def empty_token(x, y, scale=1.0):
    return '<div class="abs" style="left: %spx; top: %spx; width: %spx; height: %spx; border: 2px dashed rgba(244,236,214,0.55); border-radius: 6px; background: rgba(244,236,214,0.08); box-sizing: border-box"></div>' % (x, y, 68 * scale, 74 * scale)

def perm_chip(x, y, cid, name, w=168, h=40):
    return ('<div class="abs tag" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; padding: 0 8px 0 5px; gap: 8px">%s<span class="deco" style="font-size: 15px; font-weight: 400; overflow: hidden; text-overflow: ellipsis">%s</span></div>'
            % (x, y, w, h, img(ART[cid], 0, 0, 48, 29, 'position: static; border-radius: 3px; box-shadow: 0 0 0 1px %s' % INK), name))
def perm_note(x, y, cid, name, body, w=150, h=56, rot=-1.5):
    return ('<div class="abs paper" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; transform: rotate(%sdeg); padding: 5px 8px 4px 6px; display: flex; gap: 7px; align-items: center">'
            '<div class="abs" style="left: 50%%; top: -5px; width: 12px; height: 12px; margin-left: -6px; border-radius: 50%%; background: %s; box-shadow: 0 0 0 1px #3b2f2f"></div>'
            '%s<div style="min-width: 0"><div class="deco" style="font-size: 14px; font-weight: 400; white-space: nowrap">%s</div><div class="small" style="font-size: 12px; line-height: 15px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis">%s</div></div></div>'
            % (x, y, w, h, rot, ROSE, img(ART[cid], 0, 0, 48, 29, 'position: static; flex: none; border-radius: 3px; box-shadow: 0 0 0 1px %s' % INK), name, body))
def perm_icon(x, y, cid, size=40):
    return '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; border-radius: 6px; background: #f4ecd6; box-shadow: 0 0 0 1.5px #3b2f2f, 0 2px 5px rgba(0,0,0,0.4); overflow: hidden">%s</div>' % (x, y, size, size, img(ART[cid], -round(size * 0.35), 0, round(size * 1.67), size))

# ---- 敵の情報の3つの形 ----

def bubble(cx, y, name, kind, val, rider=None, w=170, tail_to=None, glow=False, name_size=15):
    """案B: 看板 (名前＋意図の1枚)。tail_to = 頭の上端の y (尾の線を垂らす)"""
    h = 62
    s = ''
    if tail_to is not None and tail_to > y + h:
        s += '<svg class="abs" style="left: 0; top: 0; overflow: visible; pointer-events: none" width="1" height="1"><line x1="%d" y1="%d" x2="%d" y2="%d" stroke="#f4ecd6" stroke-width="2" stroke-dasharray="3 4" opacity="0.8"></line></svg>' % (cx, y + h, cx, tail_to - 4)
    s += '<div class="abs paper" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; padding: 3px 6px 0; display: flex; flex-direction: column; align-items: center%s">' % (cx - w // 2, y, w, h, '; box-shadow: 0 0 0 1.5px #3b2f2f, 0 0 0 5px rgba(240,213,138,0.75), 0 4px 10px rgba(0,0,0,0.35)' if glow else '')
    s += '<div class="small" style="font-size: %dpx; line-height: 18px; white-space: nowrap">%s</div>' % (name_size, name)
    s += '<div style="height: 36px; display: flex; align-items: center">%s</div>' % intent_html(kind, val, rider)
    s += '<div class="abs" style="left: 50%; bottom: -9px; width: 14px; height: 14px; margin-left: -7px; background: #f4ecd6; transform: rotate(45deg); box-shadow: 1.5px 1.5px 0 0 #3b2f2f"></div>'
    return s + '</div>'

def strip(x, y, w, h, name, hp, mx, kind, val, rider=None, status=None, aimed=False, num=None, glow=False, trait=None, big=False):
    """案C: 帳面の一行 (足元の線の札)。名前＋HP／意図＋ライダー／状態"""
    s = '<div class="abs paper" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; padding: %s 8px; border-radius: 8px 10px 8px 9px / 9px 8px 10px 8px; box-sizing: border-box%s">' % (x, y, w, h, ('4px' if big else '3px'),
        '; box-shadow: 0 0 0 2.5px %s, 0 0 10px rgba(224,178,90,0.6), 0 4px 10px rgba(0,0,0,0.35)' % HONEY if aimed else ('; box-shadow: 0 0 0 1.5px #3b2f2f, 0 0 0 5px rgba(240,213,138,0.75), 0 4px 10px rgba(0,0,0,0.35)' if glow else ''))
    nm = ('<span style="color: #7a4e12">%s</span> ' % num) if num else ''
    if big:
        barw = w - 16 - 110
        s += '<div style="display: flex; align-items: center; gap: 6px; height: 24px"><span class="deco" style="font-size: 19px; font-weight: 400; white-space: nowrap; flex: none">%s%s</span><span style="position: relative; flex: 1; height: 24px">%s</span></div>' % (nm, name, hpbar(0, 3, barw, hp, mx, h=18, size=13))
        rowh = h - 24 - 8 - (20 if trait else 0)
        s += '<div style="display: flex; align-items: center; gap: 8px; height: %dpx">%s' % (rowh, intent_html(kind, val, rider, icon_size=40, num_size=30))
        if status: s += '<span style="margin-left: auto">%s</span>' % pill(status[0], status[1], INK, PAPER2)
        s += '</div>'
        if trait: s += '<div class="small" style="font-size: 13px; line-height: 16px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis">%s</div>' % trait
    else:
        # スマホ: 名前＋状態／HP バー／意図 の3段 (幅 176 に意図・ライダー・状態を1行で並べると溢れる)
        s += '<div style="display: flex; align-items: center; gap: 6px; height: 18px"><span class="deco" style="font-size: 15px; font-weight: 400; white-space: nowrap; flex: none">%s%s</span>' % (nm, name)
        if status: s += '<span style="margin-left: auto; transform: scale(0.92); transform-origin: right center">%s</span>' % pill(status[0], status[1], INK, PAPER2)
        s += '</div>'
        s += '<div style="position: relative; height: 16px; margin-top: 2px">%s</div>' % hpbar(0, 0, w - 16, hp, mx, h=16, size=13)
        s += '<div style="display: flex; align-items: center; gap: 8px; height: %dpx; margin-top: 2px">%s</div>' % (h - 8 - 18 - 16 - 4, intent_html(kind, val, rider, icon_size=26, num_size=22))
    return s + '</div>'

def roster_row(x, y, w, h, num, name, hp, mx, kind, val, rider=None, status=None, aimed=False, portrait=PROBE, glow=False):
    """案A: 右の柱の一行 (顔＋番号／名前＋HP／意図／状態)"""
    s = '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; border-radius: 8px; background: %s; box-shadow: 0 0 0 1.5px %s%s; box-sizing: border-box; padding: 6px 8px 6px 6px; display: flex; gap: 8px">' % (
        x, y, w, h, '#fbf6e8' if aimed else PAPER, HONEY if aimed else INK, ', 0 0 0 4px rgba(224,178,90,0.55)' if aimed else (', 0 0 0 5px rgba(240,213,138,0.75)' if glow else ''))
    s += '<div style="position: relative; width: 46px; height: 46px; flex: none; border-radius: 6px; background: #20233a; box-shadow: 0 0 0 1.5px #3b2f2f; overflow: hidden">%s<div class="abs deco" style="left: -6px; top: -6px; width: 22px; height: 22px; border-radius: 50%%; background: #f4ecd6; box-shadow: 0 0 0 1.5px #3b2f2f; font-size: 13px; display: flex; align-items: center; justify-content: center">%s</div></div>' % (img(portrait, -10, -4, 64, 64), num)
    s += '<div style="flex: 1; min-width: 0; display: flex; flex-direction: column; gap: 4px">'
    s += '<div style="display: flex; align-items: center; gap: 6px"><span class="deco" style="font-size: 15px; font-weight: 400; white-space: nowrap">%s</span><span style="position: relative; flex: 1; height: 18px">%s</span></div>' % (name, hpbar(0, 0, w - 46 - 8 - 14 - 8 - 4 * len(name) - 30, hp, mx, h=18, size=13))
    s += '<div style="display: flex; align-items: center; gap: 8px; height: 30px">%s' % intent_html(kind, val, rider, icon_size=28, num_size=22)
    if status: s += '<span style="margin-left: auto">%s</span>' % pill(status[0], status[1], INK, PAPER2)
    s += '</div></div></div>'
    return s

def marker(cx, top, num):
    return '<div class="abs deco" style="left: %dpx; top: %dpx; width: 26px; height: 26px; border-radius: 50%%; background: #f4ecd6; box-shadow: 0 0 0 1.5px #3b2f2f, 0 2px 5px rgba(0,0,0,0.4); font-size: 15px; display: flex; align-items: center; justify-content: center">%s</div>' % (cx - 13, top - 34, num)

def incoming_note(x, y, incoming, block, hp, lethal=False, w=None):
    left = incoming - block
    txt = '被ダメ <b style="color: #f6dd98">%d</b> − 盾 %d ＝ <b style="color: %s">HP −%d → %d</b>' % (incoming, block, '#f5a3a3' if left > 0 else '#bfe6ff', max(0, left), hp - max(0, left))
    if lethal: txt = ic('skull', 16, 'display: inline-block; vertical-align: -3px') + ' ' + txt
    return '<div class="abs night" style="left: %dpx; top: %dpx%s">%s</div>' % (x, y, ('; width: %dpx' % w) if w else '', txt)

def phase_tag(x, y, text='あなたの番', w=None):
    return tag(x, y, '<span class="deco" style="font-size: 15px; font-weight: 400; letter-spacing: 0.15em; width: 100%%; text-align: center">%s</span>' % text, h=30, w=w, bg='#fbf6e8')

def write(name, html):
    with open(os.path.join(OUT, name + '.dc.html'), 'w', encoding='utf-8') as f: f.write(HEAD + html + TAIL)

# ================================================================ 現状 (スマホ)
def board_current_phone():
    s = board('current-phone.jpg', 1462, 675)
    s += note(500, 70, 330, '<b>①</b> 手札は5枚で 180px 間隔＝重なる。本文が読めるのは持ち上がった1枚だけで、隣の札の数字が隠れる', (660, 150, 700, 470))
    s += note(396, 190, 250, '<b>②</b> 置物の付箋の列がリーダーの背中に触れる。5枚目からは「+N」', (396, 220, 372, 246))
    s += note(1180, 168, 270, '<b>③</b> 吹き出しの高さが敵の奥行きでばらつく (76／65／76)。ボスは頭に重なる', (1180, 180, 1160, 100))
    s += note(24, 420, 236, '<b>④</b> 被ダメ予測 (敵の攻撃の合計−ブロック) が無い。本家とブラウザ版にはある', (150, 420, 300, 350))
    s += note(1010, 540, 300, '<b>⑤</b> 山札は左下・捨て札は右下・エナジーは左＝自分の資源が三隅に散る', (1010, 560, 1290, 615))
    s += caption(24, 648, '現状 スマホ (2026-09-15 348a23b)・キャンバス 1462×675')
    return s + close()

# ================================================================ 現状 (PC)
def board_current_pc():
    s = board('current-pc.jpg', 1920, 1080)
    s += note(690, 300, 320, '<b>①</b> からくりの札と置物の付箋が舞台の真ん中 (敵1の足元) に浮く。箱庭の絵を分断し、敵と重なる', (850, 300, 820, 510), big=True)
    s += note(1330, 300, 340, '<b>②</b> 吹き出しは奥の敵ほど高く (y120〜210)、名前札は 400px 下＝1体の情報が縦に散る。敵3体で3つの高さを読む', (1330, 320, 1290, 250), big=True)
    s += note(1230, 830, 300, '<b>③</b> ホバーで持ち上がった札が隣の本文を隠す (5枚目の守りの蔓)', (1230, 860, 1160, 880), big=True)
    s += note(60, 630, 280, '<b>④</b> 被ダメ予測が無い (ブラウザ版は HP の直下にある)', (340, 660, 440, 700), big=True)
    s += caption(24, 1050, '現状 PC (2026-09-15 348a23b)・1920×1080')
    return s + close()

# ================================================================ 案A 額縁の柱 (左=自分の手帳・右=敵の一覧)
def column(x, y, w, h, side):
    r = '10px 0 0 10px' if side == 'right' else '0 10px 10px 0'
    return ('<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; background: #f4ecd6; box-shadow: 0 0 0 1.5px #3b2f2f, 0 0 18px rgba(0,0,0,0.5); box-sizing: border-box; border-radius: %s"></div>'
            '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; border: 1px solid rgba(59,47,47,0.35); border-radius: %s; box-sizing: border-box; pointer-events: none"></div>') % (x, y, w, h, r, x + 6, y + 6, w - 12, h - 12, r)

def ink_line(x, y, html, size=13, w=None):
    return '<div class="abs" style="left: %dpx; top: %dpx; %s font-size: %dpx; line-height: %dpx; color: #574b48; white-space: nowrap">%s</div>' % (x, y, ('width: %dpx;' % w) if w else '', size, size + 5, html)

def incoming_ink(x, y, incoming, block, hp, size=14, wrap=False):
    left = max(0, incoming - block)
    if wrap:
        return ink_line(x, y, '被ダメ <b style="color: #9c3a2a">%d</b> − 盾 %d' % (incoming, block), size) + ink_line(x, y + size + 6, '＝ <b style="color: #3b2f2f">HP −%d → %d</b>' % (left, hp - left), size)
    return ink_line(x, y, '被ダメ <b style="color: #9c3a2a">%d</b> − 盾 %d ＝ <b style="color: #3b2f2f">HP −%d → %d</b>' % (incoming, block, left, hp - left), size)

def player_column_phone(x0=0, y0=56, w=232, h=619):
    s = column(x0, y0, w, h, 'left')
    p = PLAYER
    s += '<div class="abs" style="left: %dpx; top: %dpx; width: 40px; height: 40px; border-radius: 6px; background: #20233a; box-shadow: 0 0 0 1.5px #3b2f2f; overflow: hidden">%s</div>' % (x0 + 16, y0 + 12, img(KONOHA, -6, -2, 52, 52))
    s += '<div class="abs deco" style="left: %dpx; top: %dpx; font-size: 17px">このは</div>' % (x0 + 64, y0 + 14)
    s += ink_line(x0 + 64, y0 + 36, '大樹の坑匠')
    s += hpbar(x0 + 30, y0 + 64, w - 46, p['hp'], p['mx'], h=20, block=p['block'], size=14)
    s += incoming_ink(x0 + 16, y0 + 90, p['incoming'], p['block'], p['hp'], wrap=True)
    s += chip(x0 + 16, y0 + 132, 'growth', '成長 %d' % p['growth'])
    s += ink_line(x0 + 16, y0 + 166, 'からくり 2 / 2')
    s += token(x0 + 16, y0 + 186, 'green_reaction_vine', 'now'); s += token(x0 + 92, y0 + 186, 'green_reaction_thorns', 'prep')
    s += ink_line(x0 + 16, y0 + 270, '置物 2')
    s += perm_chip(x0 + 16, y0 + 290, PERMS[0][0], PERMS[0][1], w=w - 32); s += perm_chip(x0 + 16, y0 + 336, PERMS[1][0], PERMS[1][1], w=w - 32)
    s += energy_orb(x0 + 12, y0 + 392, 104, p['energy'], p['emax'])
    s += pile(x0 + 122, y0 + 402, 'draw', p['draw'], '山札', w=96); s += pile(x0 + 122, y0 + 446, 'exhaust', p['discard'], '捨て札', w=96, rot=1); s += pile(x0 + 122, y0 + 490, 'exhaust', p['exhaust'], '消滅', w=96)
    return s

def enemy_column_phone(x0=1170, y0=56, w=292, h=619, expanded=None, phase='あなたの番'):
    s = column(x0, y0, w, h, 'right')
    s += '<div class="abs deco" style="left: %dpx; top: %dpx; font-size: 15px; font-weight: 400; letter-spacing: 0.1em">敵 3</div>' % (x0 + 14, y0 + 12)
    s += phase_tag(x0 + w - 146, y0 + 8, phase, w=134)
    y = y0 + 44
    for i, e in enumerate(ENEMIES):
        s += roster_row(x0 + 12, y, w - 24, 92, '①②③'[i], e['name'], e['hp'], e['mx'], e['kind'], e['val'], e['rider'], ('sword', '筋力-3'), aimed=e['aimed'])
        y += 100
    s += ink_line(x0 + 14, y + 6, '探り屋: 探り→構え→探り→本気の4拍', 13)
    s += ink_line(x0 + 14, y + 26, '三度目が本気', 13)
    s += end_turn(x0 + 12, y0 + h - 76, w - 24, 60, size=19, rot=0)
    return s

def board_a_phone():
    s = board('base-phone.jpg', 1462, 675, scale=0.82, dx=52)
    f = lambda v: round(v * 0.82 + 52); g = lambda v: round(v * 0.82)
    s += topbar(1462, True)
    s += player_column_phone(); s += enemy_column_phone()
    for i, e in enumerate(PH['enemies']):
        s += marker(f(e['cx']), g(e['top']), '①②③'[i])
    s += ring(f(PH['enemies'][0]['cx']), g(PH['enemies'][0]['feet']), 100, 26)
    s += hand(240, 371, spacing=180)
    s += caption(246, 652, '案A — 額縁の柱: 左に自分の手帳・右に敵の一覧。舞台の上には番号の札だけ (座席は柱の内側に寄せる＝絵は 0.82 倍)')
    return s + close()

def player_column_pc(x0=0, y0=72, w=300, h=1008):
    s = column(x0, y0, w, h, 'left')
    p = PLAYER
    s += '<div class="abs" style="left: %dpx; top: %dpx; width: 48px; height: 48px; border-radius: 6px; background: #20233a; box-shadow: 0 0 0 1.5px #3b2f2f; overflow: hidden">%s</div>' % (x0 + 20, y0 + 16, img(KONOHA, -8, -2, 64, 64))
    s += '<div class="abs deco" style="left: %dpx; top: %dpx; font-size: 19px">このは</div>' % (x0 + 76, y0 + 18)
    s += ink_line(x0 + 76, y0 + 44, '大樹の坑匠')
    s += hpbar(x0 + 34, y0 + 80, w - 54, p['hp'], p['mx'], h=22, block=p['block'], size=15)
    s += incoming_ink(x0 + 20, y0 + 112, p['incoming'], p['block'], p['hp'], size=15)
    s += chip(x0 + 20, y0 + 142, 'growth', '成長 %d' % p['growth'], h=26, size=14)
    s += ink_line(x0 + 20, y0 + 186, 'からくり 2 / 2', 14)
    s += token(x0 + 20, y0 + 208, 'green_reaction_vine', 'now'); s += token(x0 + 98, y0 + 208, 'green_reaction_thorns', 'prep')
    s += ink_line(x0 + 20, y0 + 300, '置物 2', 14)
    for i, pm in enumerate(PERMS): s += perm_note(x0 + 20, y0 + 324 + i * 64, pm[0], pm[1], pm[2], w=w - 40, h=56, rot=(-1 if i == 0 else 1))
    s += energy_orb(x0 + 16, y0 + 470, 128, p['energy'], p['emax'])
    s += pile(x0 + 156, y0 + 484, 'draw', p['draw'], '山札', w=120); s += pile(x0 + 156, y0 + 530, 'exhaust', p['discard'], '捨て札', w=120, rot=1); s += pile(x0 + 156, y0 + 576, 'exhaust', p['exhaust'], '消滅', w=120)
    return s

def enemy_column_pc(x0=1560, y0=72, w=360, h=1008):
    s = column(x0, y0, w, h, 'right')
    s += '<div class="abs deco" style="left: %dpx; top: %dpx; font-size: 17px; font-weight: 400; letter-spacing: 0.1em">敵 3</div>' % (x0 + 18, y0 + 14)
    s += phase_tag(x0 + w - 150, y0 + 10, 'あなたの番', w=134)
    y = y0 + 52
    for i, e in enumerate(ENEMIES):
        s += roster_row(x0 + 16, y, w - 32, 104, '①②③'[i], e['name'], e['hp'], e['mx'], e['kind'], e['val'], e['rider'], ('sword', '筋力-3'), aimed=e['aimed'])
        y += 114
    s += ink_line(x0 + 18, y + 8, '探り屋: 探り→構え→探り→本気の4拍。三度目が本気', 13, w=w - 36)
    s += end_turn(x0 + 16, y0 + h - 96, w - 32, 72, size=21, rot=0)
    return s

def board_a_pc():
    s = board('base-pc.jpg', 1920, 1080, scale=0.82, dx=60, dy=194)
    f = lambda v: round(v * 0.82 + 60); g = lambda v: round(v * 0.82 + 194)
    s += topbar(1920, False)
    s += player_column_pc(); s += enemy_column_pc()
    for i, e in enumerate(PC['enemies']):
        s += marker(f(e['cx']), g(e['top']), '①②③'[i])
    s += ring(f(PC['enemies'][0]['cx']), g(PC['enemies'][0]['feet']), 130, 30)
    s += hand(468, 760, spacing=196, scale=0.92)
    s += caption(320, 1052, '案A — 額縁の柱 (PC)。柱は 300 / 360。舞台は柱の内側 (絵は 0.82 倍)')
    return s + close()

# ================================================================ 案B 看板の列と作業台
def desk(x, y, w, h):
    return ('<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; background: linear-gradient(180deg, #221f34 0%%, #1a1828 100%%); border-top: 2px solid #3b2f2f; box-shadow: inset 0 2px 0 #f4ecd6, inset 0 4px 0 rgba(59,47,47,0.6), 0 -6px 16px rgba(0,0,0,0.45); box-sizing: border-box"></div>' % (x, y, w, h))
def tray(x, y, w, h, lab):
    return ('<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; border: 1.5px dashed rgba(244,236,214,0.4); border-radius: 8px; box-sizing: border-box"></div>' % (x, y, w, h)) + label(x + 8, y - 8, lab, 13)

def board_b_phone(drawer=False):
    s = board('base-phone.jpg', 1462, 675)
    if drawer:
        e3 = PH['enemies'][2]
        s += '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; border-radius: 50%%; box-shadow: 0 0 0 9999px rgba(8,8,20,0.5)"></div>' % (e3['x0'] - 60, e3['top'] - 110, e3['x1'] - e3['x0'] + 120, e3['feet'] - e3['top'] + 170)
    s += topbar(1462, True, extra_center=phase_tag(668, 13, '敵の番 ③ / 3' if drawer else 'あなたの番', w=130))
    for i, (e, pe) in enumerate(zip(ENEMIES, PH['enemies'])):
        s += bubble(pe['cx'], 62, e['name'], e['kind'], e['val'], e['rider'], w=170, tail_to=pe['top'], glow=(drawer and i == 2))
        s += hpbar(pe['cx'] - 50, pe['feet'] + 4, 100, e['hp'], e['mx'], h=18)
        s += chip(pe['cx'] - 36, pe['feet'] + 27, 'sword', '筋力-3', h=22)
    if not drawer: s += ring(PH['enemies'][0]['cx'], PH['enemies'][0]['feet'], 110, 28)
    else: s += ring(PH['enemies'][2]['cx'], PH['enemies'][2]['feet'], 110, 28, '#f0d58a')
    pl = PH['leader']; p = PLAYER
    s += chip(pl['cx'] - 34, pl['top'] - 30, 'growth', '成長 %d' % p['growth'])
    s += hpbar(pl['cx'] - 70, 326, 150, p['hp'], p['mx'], h=18, block=p['block'])
    s += incoming_note(pl['cx'] - 84, 348, p['incoming'], p['block'], p['hp'])
    s += desk(0, 371, 1462, 304)
    if not drawer:
        s += energy_orb(28, 392, 100, p['energy'], p['emax'])
        s += pile(22, 508, 'draw', p['draw'], '山札', w=116)
        s += tray(142, 386, 158, 104, 'からくり 2 / 2')
        s += token(150, 402, 'green_reaction_vine', 'now'); s += token(224, 402, 'green_reaction_thorns', 'prep')
        s += hand(300, 380, spacing=165)
        s += label(1176, 378, '置物 2', 13)
        s += perm_chip(1176, 394, PERMS[0][0], PERMS[0][1], w=132); s += perm_chip(1176, 440, PERMS[1][0], PERMS[1][1], w=132)
        s += end_turn(1318, 386, 130, 56, size=18, rot=0)
        s += pile(1176, 500, 'exhaust', p['discard'], '捨て札', w=190, count2=p['exhaust'], lab2='消滅', rot=1)
        s += caption(320, 654, '案B — 看板の列と作業台: 敵の名前と意図は頭上の一列の看板、自分の物は全部下の作業台。舞台の中には HP と状態だけ')
    else:
        s += '<div class="abs paper" style="left: 28px; top: 386px; width: 400px; height: 126px; padding: 10px 14px"><div class="deco" style="font-size: 16px; font-weight: 400">③ 探り屋 の行動の前（実行前）</div><div style="margin-top: 6px; display: flex; align-items: center; gap: 10px">%s<span class="small">×1（実値）</span></div><div class="small" style="margin-top: 4px; font-size: 14px">盾 %d を差し引いて <b style="color: #9c3a2a; font-size: 16px">HP −8</b>（71 → 63）</div></div>' % (intent_html('attack', 13, None, 32, 26), p['block'])
        s += button(28, 528, 400, 56, '温存する（発動しない）', 18)
        s += label(28, 600, '温存すると窓は閉じ、罠は次の窓まで残る（あと1回）', 13)
        s += card(470, 314, HAND[3], scale=0.8)
        s += '<div class="abs night" style="left: 464px; top: 610px; width: 176px; text-align: center; font-size: 12px">ブロック12 → 完全に凌ぐ・次T 1ドロー</div>'
        s += button(478, 636, 152, 30, '発動', 15, primary=True)
        s += card(690, 314, dict(HAND[3], id='green_reaction_thorns', name='茨の返し', body='被攻撃後: 返し<b>10</b><br>受けた攻撃が10以上なら急所+<b>2</b>'), scale=0.8, playable=False)
        s += '<div class="abs night" style="left: 684px; top: 610px; width: 176px; text-align: center; color: #c4beb2">準備中（次のターンから）</div>'
        s += label(900, 400, '手札は敵の番のあいだ引き出しの奥（見えない）。', 13); s += label(900, 424, '舞台はそのまま＝③の看板が光り、他は暗く沈む。', 13); s += label(900, 448, '発動候補は札の実物（数字は成長・弱体込みの実値）', 13)
        s += caption(300, 62, '案B の確認ウィンドウ — 作業台の引き出し (モーダル無し。敵は隠れない)')
    return s + close()

def board_b_pc():
    s = board('base-pc.jpg', 1920, 1080)
    s += topbar(1920, False, extra_center=phase_tag(900, 15, 'あなたの番', w=130))
    for i, (e, pe) in enumerate(zip(ENEMIES, PC['enemies'])):
        s += bubble(pe['cx'], 262, e['name'], e['kind'], e['val'], e['rider'], w=190, tail_to=pe['top'], name_size=15)
        s += hpbar(pe['cx'] - 60, pe['feet'] + 6, 120, e['hp'], e['mx'], h=20, size=14)
        s += chip(pe['cx'] - 36, pe['feet'] + 32, 'sword', '筋力-3', h=24)
    s += ring(PC['enemies'][0]['cx'], PC['enemies'][0]['feet'], 140, 34)
    pl = PC['leader']; p = PLAYER
    s += chip(pl['cx'] - 34, pl['top'] - 34, 'growth', '成長 %d' % p['growth'], h=26, size=14)
    s += hpbar(pl['cx'] - 80, 610, 180, p['hp'], p['mx'], h=20, block=p['block'], size=14)
    s += incoming_note(pl['cx'] - 94, 636, p['incoming'], p['block'], p['hp'])
    s += desk(0, 770, 1920, 310)
    s += energy_orb(60, 800, 128, p['energy'], p['emax'])
    s += pile(48, 946, 'draw', p['draw'], '山札', w=130)
    s += tray(222, 790, 174, 112, 'からくり 2 / 2')
    s += token(232, 808, 'green_reaction_vine', 'now'); s += token(310, 808, 'green_reaction_thorns', 'prep')
    s += hand(468, 760, spacing=196, scale=0.92)
    s += label(1500, 782, '置物 2', 13)
    for i, pm in enumerate(PERMS): s += perm_note(1500, 800 + i * 64, pm[0], pm[1], pm[2], w=160, h=56, rot=(-1.5 if i == 0 else 1.2))
    s += end_turn(1680, 806, 220, 64)
    s += pile(1680, 900, 'exhaust', p['discard'], '捨て札', w=190, count2=p['exhaust'], lab2='消滅', rot=1)
    s += caption(400, 1050, '案B — 看板の列と作業台 (PC)。看板の列は最も高い頭のすぐ上・尾は点線')
    return s + close()

# ================================================================ 案C 帳面の一行 (足元の線に敵の札・自分の札)
def player_strip_phone(x=24, y=298, w=212, h=72):
    p = PLAYER
    s = '<div class="abs paper" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; padding: 6px 8px 4px; border-radius: 8px 10px 8px 9px / 9px 8px 10px 8px"></div>' % (x, y, w, h)
    s += hpbar(x + 22, y + 8, w - 32, p['hp'], p['mx'], h=18, block=p['block'])
    s += ink_line(x + 8, y + 40, '被ダメ <b style="color: #9c3a2a">%d</b> − 盾 %d ＝ <b>HP −%d → %d</b>' % (p['incoming'], p['block'], p['incoming'] - p['block'], p['hp'] - p['incoming'] + p['block']), 13)
    return s

def board_c_phone(react=False, boss=False):
    s = board('base-phone-boss.jpg' if boss else 'base-phone.jpg', 1462, 675)
    if react:
        e3 = PH['enemies'][2]
        s += '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; border-radius: 50%%; box-shadow: 0 0 0 9999px rgba(8,8,20,0.5)"></div>' % (e3['x0'] - 60, e3['top'] - 80, e3['x1'] - e3['x0'] + 120, e3['feet'] - e3['top'] + 130)
    s += topbar(1462, True, enc=('脳筋オーガ' if boss else '探り屋の三人組'), row=(16 if boss else 3), extra_center=phase_tag(668, 13, '敵の番 ③ / 3' if react else 'あなたの番', w=130))
    p = PLAYER; pl = PH['leader']
    # 左の一角 (現行の3段のうち、資源の札は頭上・HP は帳面へ)
    s += label(26, 66, 'からくり 2 / 2', 13)
    s += token(26, 88, 'green_reaction_vine', 'now'); s += token(102, 88, 'green_reaction_thorns', 'prep')
    s += label(26, 178, '置物 2', 13)
    s += perm_chip(26, 200, PERMS[0][0], PERMS[0][1]); s += perm_chip(26, 246, PERMS[1][0], PERMS[1][1])
    s += chip(pl['cx'] - 34, pl['top'] - 30, 'growth', '成長 %d' % p['growth'])
    s += player_strip_phone()
    if boss:
        b = PH['boss']
        s += strip(b['cx'] - 150, 294, 300, 72, '脳筋オーガ', 135, 175, 'attack', 12, None, ('shield', '装甲25'), aimed=True)
        s += '<div class="abs paper" style="left: %dpx; top: 298px; width: 214px; height: 72px; padding: 6px 10px; background: #eadfc4"><div class="small" style="font-size: 13px; line-height: 17px">予告: HPが87以下になると</div><div style="display: flex; align-items: center; gap: 6px; margin-top: 4px">%s<span class="deco" style="font-size: 18px">7〜9</span><span class="small">×2 → 雄叫び</span></div></div>' % (b['cx'] + 160, ic('intent_attack', 28))
        s += '<div class="abs" style="left: %dpx; top: %dpx; width: 0; height: 0; border-left: 9px solid transparent; border-right: 9px solid transparent; border-top: 12px solid %s"></div>' % (b['cx'] - 9, b['top'] - 22, HONEY)
    else:
        for i, (e, pe) in enumerate(zip(ENEMIES, PH['enemies'])):
            s += strip(pe['cx'] - 88, 294, 176, 72, e['name'], e['hp'], e['mx'], e['kind'], e['val'], e['rider'], ('sword', '筋力-3'), aimed=(e['aimed'] and not react), num='①②③'[i], glow=(react and i == 2))
        aim = PH['enemies'][2 if react else 0]
        s += '<div class="abs" style="left: %dpx; top: %dpx; width: 0; height: 0; border-left: 9px solid transparent; border-right: 9px solid transparent; border-top: 12px solid %s"></div>' % (aim['cx'] - 9, aim['top'] - 22, HONEY if not react else '#f0d58a')
    s += hand(280, 371, spacing=180, dim=react)
    s += energy_orb(96, 431, 128, p['energy'], p['emax'])
    s += pile(24, 611, 'draw', p['draw'], '山札', w=130)
    s += pile(1248, 611, 'exhaust', p['discard'], '捨て札', w=190, count2=p['exhaust'], lab2='消滅', rot=1)
    if not react:
        s += end_turn(1192, 465, 230, 64)
        s += '<div class="abs night" style="left: 1230px; top: 440px">手札 5 · からくり 2/2</div>'
    if react:
        # 帳面の一行から立ち上がる窓 (③の左に。③は見える)
        s += '<div class="abs paper" style="left: 780px; top: 112px; width: 410px; height: 262px; padding: 12px 14px; border-radius: 10px 12px 2px 2px"></div>'
        s += '<div class="abs deco" style="left: 796px; top: 122px; font-size: 16px; font-weight: 400">③ 探り屋 の行動の前（実行前）</div>'
        s += '<div class="abs" style="left: 796px; top: 148px; display: flex; align-items: center; gap: 10px">%s<span class="small">×1（実値）</span><span class="small" style="margin-left: 8px">盾 5 を差し引いて</span><b style="color: #9c3a2a; font-size: 17px">HP −8</b></div>' % intent_html('attack', 13, None, 32, 26)
        s += token(796, 196, 'green_reaction_vine', 'now')
        s += '<div class="abs" style="left: 874px; top: 194px; width: 190px; font-size: 14px; line-height: 19px"><b>守りの蔓</b><br><span class="small">ブロック12 → 完全に凌ぐ (HP −0)・次のターン 1ドロー</span></div>'
        s += button(1074, 206, 100, 44, '発動', 16, primary=True)
        s += token(796, 280, 'green_reaction_thorns', 'prep')
        s += '<div class="abs" style="left: 874px; top: 282px; width: 190px; font-size: 14px; line-height: 19px; color: #574b48"><b>茨の返し</b><br><span class="small">準備中（次のターンから）</span></div>'
        s += button(796, 336, 378, 30, '温存する（発動しない）', 14)
        s += label(300, 64, '舞台はそのまま。③の札が光り、③以外は暗く沈む。', 13); s += label(300, 88, '窓は帳面の一行から立ち上がる＝敵の札と同じ場所で決める。', 13); s += label(300, 112, '手札は敵の番のあいだ薄くなる（触れない）。', 13)
        s += caption(320, 654, '案C の確認ウィンドウ — 帳面から立ち上がる窓 (モーダル無し。③は見える)')
    elif boss:
        s += caption(320, 654, '案C ボス戦 — 帳面の一行は絵の大きさに寄らない (ボスの札は幅を広く・隣に予告の札)')
    else:
        s += caption(320, 654, '案C — 帳面の一行: 敵の名前・HP・意図・状態を足元の線の札に。頭上には何も置かない。自分の札も同じ線')
    return s + close()

def board_c_pc():
    s = board('base-pc.jpg', 1920, 1080)
    s += topbar(1920, False, extra_center=phase_tag(900, 15, 'あなたの番', w=130))
    p = PLAYER; pl = PC['leader']
    s += chip(pl['cx'] - 34, pl['top'] - 34, 'growth', '成長 %d' % p['growth'], h=26, size=14)
    # 自分の札 (帳面の左端): HP・被ダメ・からくり・置物
    s += '<div class="abs paper" style="left: 40px; top: 640px; width: 700px; height: 140px; padding: 10px 14px; border-radius: 8px 10px 8px 9px / 9px 8px 10px 8px"></div>'
    s += '<div class="abs" style="left: 56px; top: 650px; width: 40px; height: 40px; border-radius: 6px; background: #20233a; box-shadow: 0 0 0 1.5px #3b2f2f; overflow: hidden">%s</div>' % img(KONOHA, -6, -2, 52, 52)
    s += '<div class="abs deco" style="left: 104px; top: 652px; font-size: 17px">このは</div>'
    s += hpbar(118, 700, 200, p['hp'], p['mx'], h=22, block=p['block'], size=15)
    s += incoming_ink(56, 736, p['incoming'], p['block'], p['hp'], size=15)
    s += '<div class="abs" style="left: 338px; top: 652px; width: 1px; height: 116px; background: rgba(59,47,47,0.35)"></div>'
    s += ink_line(352, 650, 'からくり 2 / 2', 13)
    s += token(352, 672, 'green_reaction_vine', 'now'); s += token(428, 672, 'green_reaction_thorns', 'prep')
    s += '<div class="abs" style="left: 512px; top: 652px; width: 1px; height: 116px; background: rgba(59,47,47,0.35)"></div>'
    s += ink_line(526, 650, '置物 2', 13)
    s += perm_chip(526, 672, PERMS[0][0], PERMS[0][1], w=200); s += perm_chip(526, 718, PERMS[1][0], PERMS[1][1], w=200)
    for i, (e, pe) in enumerate(zip(ENEMIES, PC['enemies'])):
        s += strip(pe['cx'] - 105, 640, 210, 140, e['name'], e['hp'], e['mx'], e['kind'], e['val'], e['rider'], ('sword', '筋力-3'), aimed=e['aimed'], num='①②③'[i], big=True, trait='探り→構え→探り→本気の4拍')
    aim = PC['enemies'][0]
    s += '<div class="abs" style="left: %dpx; top: %dpx; width: 0; height: 0; border-left: 10px solid transparent; border-right: 10px solid transparent; border-top: 14px solid %s"></div>' % (aim['cx'] - 10, aim['top'] - 26, HONEY)
    s += hand(468, 760, spacing=196, scale=0.92)
    s += energy_orb(96, 836, 128, p['energy'], p['emax'])
    s += pile(24, 1016, 'draw', p['draw'], '山札', w=130)
    s += pile(1706, 1016, 'exhaust', p['discard'], '捨て札', w=190, count2=p['exhaust'], lab2='消滅', rot=1)
    s += end_turn(1650, 870, 230, 64)
    s += '<div class="abs night" style="left: 1690px; top: 844px">手札 5 · からくり 2/2</div>'
    s += caption(400, 1050, '案C — 帳面の一行 (PC)。PC は札に特性の一文も入る')
    return s + close()

# ================================================================ 案A の確認ウィンドウ (スマホ)
def board_a_react():
    s = board('base-phone.jpg', 1462, 675, scale=0.82, dx=52)
    f = lambda v: round(v * 0.82 + 52); g = lambda v: round(v * 0.82)
    e3 = PH['enemies'][2]
    s += '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; border-radius: 50%%; box-shadow: 0 0 0 9999px rgba(8,8,20,0.5)"></div>' % (f(e3['x0']) - 50, g(e3['top']) - 70, f(e3['x1']) - f(e3['x0']) + 100, g(e3['feet']) - g(e3['top']) + 110)
    s += topbar(1462, True)
    s += player_column_phone()
    # 右の柱: ①②は1行に畳み、③が窓になる
    x0, y0, w, h = 1170, 56, 292, 619
    s += column(x0, y0, w, h, 'right')
    s += '<div class="abs deco" style="left: %dpx; top: %dpx; font-size: 15px; font-weight: 400; letter-spacing: 0.1em">敵 3</div>' % (x0 + 14, y0 + 12)
    s += phase_tag(x0 + w - 146, y0 + 8, '敵の番 ③ / 3', w=134)
    for i in range(2):
        e = ENEMIES[i]; yy = y0 + 44 + i * 40
        s += '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: 34px; border-radius: 6px; background: #eadfc4; box-shadow: 0 0 0 1px #574b48; padding: 0 8px; display: flex; align-items: center; gap: 8px; box-sizing: border-box"><span class="deco" style="font-size: 14px; font-weight: 400">%s %s</span><span style="position: relative; flex: 1; height: 16px">%s</span><span class="small">済</span></div>' % (x0 + 12, yy, w - 24, '①②'[i], e['name'], hpbar(0, 0, 100, e['hp'], e['mx'], h=16, size=12))
    wy = y0 + 128
    s += '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: 430px; border-radius: 8px; background: #fbf6e8; box-shadow: 0 0 0 1.5px #3b2f2f, 0 0 0 5px rgba(240,213,138,0.75); box-sizing: border-box"></div>' % (x0 + 12, wy, w - 24)
    s += '<div class="abs deco" style="left: %dpx; top: %dpx; font-size: 15px; font-weight: 400">③ 探り屋 の行動の前</div>' % (x0 + 24, wy + 10)
    s += '<div class="abs" style="left: %dpx; top: %dpx; display: flex; align-items: center; gap: 8px">%s<span class="small">×1（実値）</span></div>' % (x0 + 24, wy + 36, intent_html('attack', 13, None, 32, 26))
    s += ink_line(x0 + 24, wy + 78, '盾 5 を差し引いて <b style="color: #9c3a2a; font-size: 15px">HP −8</b>（71 → 63）', 13)
    s += '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: 1px; background: rgba(59,47,47,0.35)"></div>' % (x0 + 24, wy + 104, w - 48)
    s += token(x0 + 24, wy + 118, 'green_reaction_vine', 'now')
    s += '<div class="abs" style="left: %dpx; top: %dpx; width: 168px; font-size: 14px; line-height: 19px"><b>守りの蔓</b><br><span class="small">ブロック12 → 完全に凌ぐ (HP −0)。次のターン 1ドロー</span></div>' % (x0 + 100, wy + 116)
    s += button(x0 + 100, wy + 194, 168, 40, '発動', 16, primary=True)
    s += token(x0 + 24, wy + 250, 'green_reaction_thorns', 'prep')
    s += '<div class="abs" style="left: %dpx; top: %dpx; width: 168px; font-size: 14px; line-height: 19px; color: #574b48"><b>茨の返し</b><br><span class="small">準備中（次のターンから）</span></div>' % (x0 + 100, wy + 250)
    s += button(x0 + 24, wy + 340, w - 48, 44, '温存する', 16)
    s += ink_line(x0 + 24, wy + 394, '温存すると罠は次の窓まで残る（あと1回）', 12)
    s += marker(f(e3['cx']), g(e3['top']), '③')
    s += ring(f(e3['cx']), g(e3['feet']), 100, 26, '#f0d58a')
    s += hand(240, 371, spacing=180, dim=True)
    s += caption(246, 652, '案A の確認ウィンドウ — 右の柱の③の行が窓になる (モーダル無し。舞台は③だけ明るい)')
    return s + close()

# ================================================================ 比較と推奨
def board_compare():
    W, H = 1180, 700
    s = '<div style="position: relative; width: %dpx; height: %dpx; overflow: hidden; background: #f4ecd6; padding: 26px 30px; box-sizing: border-box; color: #3b2f2f">' % (W, H)
    s += '<div class="deco" style="font-size: 22px">比較と推奨</div>'
    s += '<div class="small" style="margin-top: 4px; font-size: 14px; line-height: 20px">3案とも「被ダメ予測」「からくり・置物を舞台の真ん中から外す」「持ち上げた札が隣を隠さない」「確認ウィンドウをモーダルにしない」は共通。違うのは<b>敵の数字をどこで読むか</b>（横の柱／頭上の一列／足元の一行）</div>'
    rows = [
        ('敵の数字を読む動線', '右の柱を上から下へ。舞台の番号と往復する', '頭上の一列を左から右へ。本家と同じ向き', '足元の一行を左から右へ。手札のすぐ上＝ブロックの計算と同じ高さ'),
        ('敵4体・ボス', '行が縮む (4体は 72px)。ボスは1行で余る', '看板が 130px まで縮む。ボスの頭には重なる (今と同じ)', '札が 130px まで縮む。ボスは絵の大きさに寄らない (幅を広くできる)'),
        ('スマホの縦の余白', '柱の高さ 315px に敵3行と手帳。4体は詰まる', '看板 62px + 尾で足りる。頭上の余白の争いは消える', '帳面 72px を足元の線に。絵の足元と重ならないよう座席を 20px 上げる'),
        ('舞台の清潔さ', '◎ 番号の札だけ (ただし絵が 0.82 倍・座席を寄せる)', '○ 看板と尾が舞台の上に残る', '◎ 頭上に何も無い。足元の線から下が UI'),
        ('確認ウィンドウ', '柱の行が窓になる。敵は見える', '作業台が引き出しになる。敵は見える', '帳面から窓が立ち上がる。敵は見える'),
        ('実装の重さ', '重い (柱の器・座席の再配置・カメラのズーム)', '中 (看板の一列＝今の吹き出しの y を揃える・作業台の器)', '中 (足元の札の器＝今の名前札と HP バーの拡張・吹き出しの撤去)'),
        ('本家との距離', '遠い (JRPG の戦況板)', '近い (StS も頭上)', '中 (オクトパストラベラーの「絵の下に情報」に近い)'),
    ]
    s += '<table style="margin-top: 14px; border-collapse: collapse; width: 100%; font-size: 14px; line-height: 19px"><tr><th style="text-align: left; padding: 6px 8px; border-bottom: 1.5px solid #3b2f2f; width: 150px"></th><th style="text-align: left; padding: 6px 8px; border-bottom: 1.5px solid #3b2f2f">案A 額縁の柱</th><th style="text-align: left; padding: 6px 8px; border-bottom: 1.5px solid #3b2f2f">案B 看板の列と作業台</th><th style="text-align: left; padding: 6px 8px; border-bottom: 1.5px solid #3b2f2f; background: #fbf6e8">案C 帳面の一行 ★推奨</th></tr>'
    for r in rows:
        s += '<tr><td style="padding: 7px 8px; border-bottom: 1px solid rgba(59,47,47,0.25); color: #574b48; vertical-align: top">%s</td>' % r[0]
        for j, c in enumerate(r[1:]): s += '<td style="padding: 7px 8px; border-bottom: 1px solid rgba(59,47,47,0.25); vertical-align: top%s">%s</td>' % ('; background: #fbf6e8' if j == 2 else '', c)
        s += '</tr>'
    s += '</table>'
    s += '<div style="margin-top: 16px; display: flex; gap: 20px">'
    s += '<div style="flex: 1; font-size: 14px; line-height: 20px"><div class="deco" style="font-size: 16px">推奨: 案C 帳面の一行</div>スマホで一番足りないのは<b>頭上の縦の余白</b>で、敵の絵が大きいほど・奥にいるほど吹き出しが天井とぶつかる。数字を足元の線に下ろせば絵の大きさに寄らず、敵が何体でも同じ高さで読める。手札の真上に「攻撃13」と「盾5」が並ぶので、防御を何枚出すかの算数が1か所で済む。舞台は絵だけになり、箱庭の見せ場が戻る。<br><span class="small">次点は案B。本家と同じ「頭上」を保ちたいならこちら。看板を一列に揃えるだけでも今の高さのばらつきは消える。</span></div>'
    s += '<div style="flex: 1; font-size: 14px; line-height: 20px"><div class="deco" style="font-size: 16px">全案共通で入れるもの</div>① 被ダメ予測「被ダメ 17 − 盾 5 ＝ HP −12 → 59」(エンジンの incomingTotal をそのまま。致死級は骸骨の印)<br>② からくり・置物は舞台の真ん中に置かない<br>③ 押した (ホバーした) 札は持ち上がるだけで隣を隠さない。6枚以上は詰めて重ねる<br>④ 手番の札「あなたの番／敵の番 ③/3」を上部バーの中央に<br>⑤ 確認ウィンドウは敵を隠さない (舞台に窓を重ねない)<br>⑥ 狙っている敵は足元の輪＋札の蜂蜜の縁の二重で</div>'
    s += '</div></div>'
    return s

if __name__ == '__main__':
    write('Main', board_current_phone())
    write('CurrentPC', board_current_pc())
    write('APhone', board_a_phone()); write('APC', board_a_pc())
    write('BPhone', board_b_phone()); write('BPC', board_b_pc())
    write('CPhone', board_c_phone()); write('CPC', board_c_pc())
    write('CBoss', board_c_phone(boss=True))
    write('ReactA', board_a_react()); write('ReactB', board_b_phone(drawer=True)); write('ReactC', board_c_phone(react=True))
    write('Compare', board_compare())
    print('ok')

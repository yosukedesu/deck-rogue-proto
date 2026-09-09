# build.py — カードの面・第2版のデザインカンバス (2026-09-09 ユーザー「カードのデザインについてちゃんと詰めていきたい」)。
# 同じ6枚 (打撃・牙の一撃・絡み蔦・茨の返し・年輪の大樹・芽吹き) を4つの案 (A 整理／B 本家型の帯／C 大きな数字／D 絵はがき) で並べ、
# 推奨案の状態一覧と、文字と墨の規約 (コントラスト比つき) を添える。挿絵は本物 (Art/cards/<id>.png 80×48 を2倍)。
# 使い方: python3 build.py → Main/States/Type.dc.html と canvas.json → seed-canvas.mjs で1枚の html にして公開。
import json, os
OUT = os.path.dirname(os.path.abspath(__file__))

INK = '#3b2f2f'; INK_MID = '#574b48'; PAPER = '#f4ecd6'; PAPER2 = '#eadfc4'; NIGHT = '#1a1c33'
C = dict(rose='#d97b7b', sky='#7fa7c9', moss='#8fae7b', honey='#e0b25a', plum='#a98cc4', teal='#7ab8b0', sand='#c9a982',
         physical='#c9a982', spell='#a98cc4', reaction='#7ab8b0', permanent='#e0b25a')
ROLE = dict(dmg=C['rose'], block=C['sky'], counter=C['teal'], growth=C['moss'], momentum=C['honey'], expose='#e0a04a', mana='#e0b25a')
ROLE_ICON = dict(dmg='sword', block='shield', counter='undo', growth='leaf', momentum='wind', expose='target', mana='bolt')
ROLE_JA = dict(dmg='ダメージ', block='ブロック', counter='返し', growth='成長', momentum='勢い', expose='急所', mana='エナジー上限')
TYPE_JA = dict(physical='物理', spell='呪文', reaction='リアクション', permanent='置物')
RAR_JA = dict(common='コモン', uncommon='アンコモン', rare='レア')
UP = '#276a34'; DOWN = '#a33a30'; GOLD_INK = '#7a4e12'; PLUM_INK = '#5a3d78'

def lerp(a, b, t):
    a = a.lstrip('#'); b = b.lstrip('#')
    ra, ga, ba = int(a[0:2], 16), int(a[2:4], 16), int(a[4:6], 16)
    rb, gb, bb = int(b[0:2], 16), int(b[2:4], 16), int(b[4:6], 16)
    return '#%02x%02x%02x' % (round(ra + (rb - ra) * t), round(ga + (gb - ga) * t), round(ba + (bb - ba) * t))
def rgba(hexcol, a):
    h = hexcol.lstrip('#'); return 'rgba(%d,%d,%d,%s)' % (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)
def blend(fg, bg, a):
    return lerp(bg, fg, a)
def lum(hexcol):
    h = hexcol.lstrip('#'); out = []
    for i in (0, 2, 4):
        c = int(h[i:i + 2], 16) / 255
        out.append(c / 12.92 if c <= 0.03928 else ((c + 0.055) / 1.055) ** 2.4)
    return 0.2126 * out[0] + 0.7152 * out[1] + 0.0722 * out[2]
def contrast(fg, bg):
    l1, l2 = lum(fg), lum(bg)
    if l1 < l2: l1, l2 = l2, l1
    return (l1 + 0.05) / (l2 + 0.05)

BITMAPS = {
 'sword': ["..............-.", ".............-#-", "............-#+-", "...........-#+-.", "..........-#+-..", ".........-#+-...", "..-.....-#+-....", ".-#-...-#+-.....",
           "..-#-.-#+-......", "...-#-#+-.......", "....-##-........", "...-#-#-........", "..-#-..-........", ".-#-............", ".--.............", "................"],
 'shield': ["................", "...--------.....", "..-########-....", ".-##++####+#-...", ".-#+#######+#-..", ".-#+########-...", ".-##########-...", ".-##########-...",
            "..-########-....", "..-########-....", "...-######-.....", "....-####-......", ".....-##-.......", "......--........", "................", "................"],
 'undo': ["................", "................", "....--..........", "...-#-..........", "..-#-----.......", ".-######-.......", "..-#-----#-.....", "...-#-...-#-....",
          "....--....-#-...", "...........#-...", "..........-#-...", ".....---.-#-....", ".....-###-......", "......---.......", "................", "................"],
 'leaf': ["................", "..........---...", "........--###-..", "......--####+-..", ".....-####+#-...", "....-###+##-....", "...-##+###-.....", "...-#+###-......",
          "..-#+##-........", "..-##-#-........", "..-#-.-.........", ".-#-............", ".--.............", "................", "................", "................"],
 'wind': ["................", "................", "....--------....", "...-########-...", "....------#-....", ".........#-.....", "..----------....", ".-##########-...",
          "..-----------...", ".............-..", "...--------#-...", "..-#########-...", "...---------....", "................", "................", "................"],
 'target': ["................", ".....------.....", "...--######--...", "..-##------##-..", ".-##-......-##-.", ".-#-..----..-#-.", "-#-..-####-..-#-", "-#-..-#++#-..-#-",
            "-#-..-####-..-#-", ".-#-..----..-#-.", ".-##-......-##-.", "..-##------##-..", "...--######--...", ".....------.....", "................", "................"],
 'bolt': ["................", "........--......", ".......-#-......", "......-#-.......", ".....-#-........", "....-#----......", "...-######-.....", "....----#-......",
          ".......-#-......", "......-#-.......", ".....-#-........", "....-#-.........", "....--..........", "................", "................", "................"],
 'star': ["................", ".......--.......", ".......##.......", "......-##-......", "..------##------", "..-############-", "...-##########-.", "....-########-..",
          ".....-######-...", "....-###--###-..", "...-##-....-##-.", "..-#-........-#-", "..--..........--", "................", "................", "................"],
 'gem': ["................", "................", "................", "................", ".......--.......", "......-##-......", ".....-#++#-.....", "....-#+###-.....",
         ".....-####-.....", "......-##-......", ".......--.......", "................", "................", "................", "................", "................"],
}
def pixel_icon(name, scale=1, main=None, dark='#3b2f2f', light=None):
    rows = BITMAPS[name]
    main = main or '#3b2f2f'; light = light or lerp(main, '#ffffff', 0.45)
    rects = []
    for y, row in enumerate(rows):
        x = 0
        while x < 16:
            ch = row[x]
            if ch == '.': x += 1; continue
            x2 = x
            while x2 < 16 and row[x2] == ch: x2 += 1
            col = {'#': main, '-': dark, '+': light}[ch]
            rects.append('<rect x="%d" y="%d" width="%d" height="1" fill="%s"></rect>' % (x, y, x2 - x, col))
            x = x2
    size = 16 * scale
    return '<svg width="%d" height="%d" viewBox="0 0 16 16" shape-rendering="crispEdges" style="display:block;flex:none">%s</svg>' % (size, size, ''.join(rects))

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
    body { margin: 0; background: #1a1c33; font-family: "Klee One", "Hiragino Sans", sans-serif; color: #3b2f2f; -webkit-font-smoothing: antialiased; }
    .abs { position: absolute; }
    .deco { font-family: "Kaisei Decol", "Hiragino Mincho ProN", serif; font-weight: 700; }
    .hand { font-family: "Klee One", sans-serif; font-weight: 600; }
    .num { font-variant-numeric: tabular-nums; }
    .light { color: #f4ecd6; }
    .px { image-rendering: pixelated; image-rendering: crisp-edges; display: block; }
    .paper { background: #f4ecd6; color: #3b2f2f; border-radius: 18px 22px 16px 20px / 20px 16px 22px 18px; box-shadow: 0 0 0 1.5px #3b2f2f, 0 0 0 4px #f4ecd6, 0 0 0 5.5px rgba(59,47,47,0.5), 0 10px 22px rgba(0,0,0,0.35); }
    .tag { background: #f4ecd6; color: #3b2f2f; border-radius: 10px 14px 10px 12px / 12px 10px 14px 10px; box-shadow: 0 0 0 1.5px #3b2f2f, 0 3px 8px rgba(0,0,0,0.3); display: inline-flex; align-items: center; gap: 6px; height: 28px; padding: 0 10px; font-size: 13px; white-space: nowrap; }
    .kw { display: inline-flex; align-items: center; height: 18px; padding: 0 6px; border: 1.5px solid #3b2f2f; border-radius: 6px; font-size: 12px; line-height: 1; background: rgba(255,255,255,0.35); margin-left: 4px; vertical-align: 2px; }
    .note { font-size: 13px; line-height: 20px; color: #f4ecd6; text-shadow: 0 1px 0 rgba(0,0,0,0.6); }
  </style>
</helmet>
'''
TAIL = '''</x-dc>
</body>
</html>
'''
NOISE = "url(\"data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='120' height='120'%3E%3Cfilter id='n'%3E%3CfeTurbulence type='fractalNoise' baseFrequency='0.9' numOctaves='2' stitchTiles='stitch'/%3E%3CfeColorMatrix values='0 0 0 0 0.2 0 0 0 0 0.15 0 0 0 0 0.1 0 0 0 0.3 0'/%3E%3C/filter%3E%3Crect width='100%25' height='100%25' filter='url(%23n)'/%3E%3C/svg%3E\")"
def grain():
    return '<div class="abs" style="inset: 0; border-radius: inherit; background-image: %s; opacity: 0.35; mix-blend-mode: multiply; pointer-events: none"></div>' % NOISE

def art_img(cid, w=160, h=96, dim=False):
    return '<img class="px" src="%s.png" width="%d" height="%d" style="%s">' % (cid, w, h, 'filter: saturate(0.4) brightness(0.8)' if dim else '')

def blob(color, w, h, alpha=0.9, rot=0):
    return ('<div class="abs" style="left: 0; top: 0; width: %dpx; height: %dpx; border-radius: 48%% 52%% 45%% 55%% / 55%% 45%% 55%% 45%%; background: radial-gradient(circle at 40%% 35%%, %s, %s 70%%, %s 100%%); transform: rotate(%ddeg)"></div>'
            % (w, h, rgba(lerp(color, '#ffffff', 0.25), alpha), rgba(color, alpha), rgba(lerp(color, '#000000', 0.15), alpha * 0.8), rot))
def tape(x, y, w, text, rot=-4):
    return ('<div class="abs hand" style="left: %dpx; top: %dpx; width: %dpx; height: 22px; transform: rotate(%ddeg); background: rgba(255,226,140,0.74); border-left: 1px dashed rgba(59,47,47,0.35); border-right: 1px dashed rgba(59,47,47,0.35); box-shadow: 0 1px 2px rgba(0,0,0,0.15); display: flex; align-items: center; justify-content: center; font-size: 13px; color: %s">%s</div>'
            % (x, y, w, rot, INK, text))
def gem(rarity, scale=1):
    col = {'common': '#9aa39c', 'uncommon': C['sky'], 'rare': C['honey']}[rarity]
    return pixel_icon('gem', scale, main=col, dark=lerp(col, '#000000', 0.5), light=lerp(col, '#ffffff', 0.4))
def stars(rarity):
    n = {'common': 1, 'uncommon': 2, 'rare': 3}[rarity]
    fill = {'common': PAPER, 'uncommon': C['sky'], 'rare': C['honey']}[rarity]
    return '<div style="display: flex">' + ''.join(pixel_icon('star', 1, main=fill, dark=INK) for _ in range(n)) + '</div>'

# ---- 6枚の見本 (本物の定義から) ----
CARDS = [
 dict(id='green_strike', name='打撃', typ='physical', cost=1, rarity='common', lines=[('dmg', '6')]),
 dict(id='green_fang', name='牙の一撃', typ='physical', cost=2, rarity='uncommon', lines=[('dmg', '17')], kw=['貫通']),
 dict(id='green_entangle', name='絡み蔦', typ='physical', cost=1, rarity='common', modes=[[('block', '7')], [('dmg', '7')]]),
 dict(id='green_reaction_thorns', name='茨の返し', typ='reaction', cost=1, rarity='common', lines=[('counter', '10')], pre='被攻撃後'),
 dict(id='green_perm_growth_tree', name='年輪の大樹', typ='permanent', cost=2, rarity='rare', lines=[('block', '5'), ('growth', '+1', '毎T開始時')]),
 dict(id='green_ramp_sprout', name='芽吹き', typ='spell', cost=1, rarity='common', lines=[('mana', '+1'), ('block', '2')], notes='消滅'),
]

def line_text(l, numsize=None, color=None, pattern='A'):
    """1行の効果文。l = (role, value[, prefix])"""
    role, val = l[0], l[1]; prefix = l[2] if len(l) > 2 else None
    label = ROLE_JA[role]
    col = color or INK
    if numsize:
        v = '<span class="num" style="font-size: %dpx; color: %s; line-height: 1">%s</span>' % (numsize, col, val)
    else:
        v = '<span class="num" style="color: %s">%s</span>' % (col, val)
    pre = (prefix + ': ') if prefix else ''
    return pre + label + v

def role_glyph(role, size=16):
    return pixel_icon(ROLE_ICON[role], 1, main=INK, dark=INK, light=lerp(INK, '#ffffff', 0.5))

# ---- 案A: 現行の骨格を整理 (今日のコントラスト是正後の姿) ----
def card_A(c, dim=False, hover=False, numcol=None):
    tcol = C[c['typ']]
    body = ''
    lines = c.get('lines') or []
    for l in lines: body += '<div style="line-height: 22px">%s</div>' % line_text(l, color=numcol)
    if c.get('modes'):
        for m in c['modes']: body += '<div style="line-height: 22px">◆%s</div>' % line_text(m[0], color=numcol)
    if c.get('kw'): body += '<div style="line-height: 22px">(%s)</div>' % '・'.join(c['kw'])
    if c.get('pre'): body = body.replace(ROLE_JA['counter'], c['pre'] + ': ' + ROLE_JA['counter'])
    blobs = ''
    dmg = next((l[1] for l in lines if l[0] == 'dmg'), None); blk = next((l[1] for l in lines if l[0] == 'block'), None); ctr = next((l[1] for l in lines if l[0] == 'counter'), None)
    if c.get('modes'):
        for m in c['modes']:
            if m[0][0] == 'dmg': dmg = m[0][1]
            if m[0][0] == 'block': blk = m[0][1]
    def vb(role, v, right):
        side = 'right: 10px' if right else 'left: 10px'
        return ('<div class="abs" style="%s; bottom: 8px; width: 60px; height: 40px">%s<div class="abs hand num" style="inset: 0; display: flex; align-items: center; justify-content: center; gap: 3px; font-size: 18px; color: %s">%s<span>%s</span></div></div>'
                % (side, blob(ROLE[role], 60, 40, 0.9, 5 if right else -4), numcol or INK, role_glyph(role), v))
    if dmg: blobs += vb('dmg', dmg, False)
    elif ctr: blobs += vb('counter', ctr, False)
    if blk: blobs += vb('block', blk, True)
    center = ''
    if c.get('modes'): center = '<div class="abs" style="left: 0; right: 0; bottom: 18px; text-align: center; font-size: 13px; color: %s">どちらか</div>' % INK_MID
    elif c.get('notes'): center = tape(58, 254, 84, c['notes'])
    ink = INK if not dim else rgba(INK, 0.7)
    return ('<div style="position: relative; width: 200px; height: 290px; %s">'
            '<div class="abs paper" style="inset: 0; %s">%s</div>'
            '<div class="abs" style="left: 12px; top: -6px; width: 34px; height: 50px; background: %s; clip-path: polygon(0 0, 100%% 0, 100%% 100%%, 50%% 84%%, 0 100%%); box-shadow: inset 0 0 0 1.5px %s"></div>'
            '<div class="abs hand num" style="left: 12px; top: 2px; width: 34px; text-align: center; font-size: 19px; color: %s">%s</div>'
            '<div class="abs" style="right: 12px; top: 12px">%s</div>'
            '<div class="abs deco" style="left: 50px; right: 30px; top: 10px; height: 34px; display: flex; align-items: center; justify-content: center; font-size: %dpx; color: %s; white-space: nowrap">%s</div>'
            '<svg class="abs" style="left: 56px; top: 44px" width="110" height="6" viewBox="0 0 110 6" fill="none" stroke="%s" stroke-width="1.4" stroke-linecap="round"><path d="M1 4c20-3 40 1 55-1s35-2 53 1"></path></svg>'
            '<div class="abs" style="left: 16px; top: 54px; width: 168px; height: 104px; transform: rotate(-0.6deg); background: %s; box-shadow: 0 0 0 1.5px %s, 0 3px 6px rgba(0,0,0,0.25); display: flex; align-items: center; justify-content: center">%s</div>'
            '<div class="abs" style="left: 0; right: 0; top: 160px; text-align: center; font-size: 13px; letter-spacing: 0.1em; color: %s">%s ・ %s</div>'
            '<div class="abs hand" style="left: 14px; right: 14px; top: 180px; bottom: 52px; text-align: center; font-size: 15px; color: %s">%s</div>'
            '%s%s</div>') % (
        'filter: saturate(0.35) brightness(0.85);' if dim else '', '' if not hover else 'box-shadow: 0 0 0 1.5px #3b2f2f, 0 0 0 4px #f4ecd6, 0 0 0 5.5px rgba(59,47,47,0.5), 0 0 0 9px rgba(224,178,90,0.55), 0 14px 26px rgba(0,0,0,0.4);',
        grain(), tcol, rgba(INK, 0.8), ink, c['cost'], stars(c['rarity']), 16 if len(c['name']) > 5 else (18 if len(c['name']) > 4 else 20), ink, c['name'],
        rgba(INK, 0.55), PAPER2, rgba(INK, 0.75), art_img(c['id'], dim=dim), INK_MID, TYPE_JA[c['typ']], RAR_JA[c['rarity']][0] if False else RAR_JA[c['rarity']], ink, body, blobs, center)

# ---- 案B: 本家型の帯 (コスト玉・全幅の窓・タイプの帯・本文の大きな数字・にじみの札は無し) ----
def ribbon(typ, text, w=150, y=136, gemr=None):
    tcol = C[typ]
    return ('<div class="abs" style="left: 50%%; top: %dpx; width: %dpx; height: 26px; margin-left: -%dpx; background: %s; clip-path: polygon(8px 0, calc(100%% - 8px) 0, 100%% 50%%, calc(100%% - 8px) 100%%, 8px 100%%, 0 50%%); box-shadow: inset 0 0 0 1.5px %s"></div>'
            '<div class="abs hand" style="left: 0; right: 0; top: %dpx; height: 26px; display: flex; align-items: center; justify-content: center; gap: 6px; font-size: 14px; color: %s; letter-spacing: 0.08em">%s%s</div>'
            ) % (y, w, w // 2, tcol, rgba(INK, 0.55), y, INK, gem(gemr) if gemr else '', text)

def orb(cost, x=-8, y=-8, size=44, col=None, textcol=None):
    return ('<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; border-radius: 50%%; background: radial-gradient(circle at 35%% 30%%, %s, %s 60%%, %s 100%%); box-shadow: 0 0 0 2px %s, 0 0 0 4px %s, 0 3px 6px rgba(0,0,0,0.35)"></div>'
            '<div class="abs deco num" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; display: flex; align-items: center; justify-content: center; font-size: 22px; color: %s">%s</div>') % (
        x, y, size, size, lerp(col or C['honey'], '#ffffff', 0.35), col or C['honey'], lerp(col or C['honey'], '#000000', 0.2), INK, PAPER, x, y, size, size, textcol or INK, cost)

def frame_B(rarity, dim=False, hover=False, extra=''):
    outer = {'common': rgba(INK, 0.5), 'uncommon': C['sky'], 'rare': C['honey']}[rarity]
    outer_w = '5.5px' if rarity == 'common' else '6.5px'
    glow = ', 0 0 0 10px rgba(224,178,90,0.55), 0 14px 26px rgba(0,0,0,0.4)' if hover else ', 0 10px 22px rgba(0,0,0,0.35)'
    return '<div class="abs" style="inset: 0; background: %s; border-radius: 18px 22px 16px 20px / 20px 16px 22px 18px; box-shadow: 0 0 0 1.5px %s, 0 0 0 4px %s, 0 0 0 %s %s%s; %s">%s</div>' % (PAPER, INK, PAPER, outer_w, outer, glow, extra, grain())

def window_B(cid, dim=False, top=44, h=100):
    return ('<div class="abs" style="left: 12px; right: 12px; top: %dpx; height: %dpx; background: #20233a; box-shadow: 0 0 0 1.5px %s, inset 0 0 0 3px %s; display: flex; align-items: center; justify-content: center; overflow: hidden">%s</div>'
            % (top, h, INK, rgba(PAPER, 0.35), art_img(cid, dim=dim)))

def body_B(c, numcol=None, top=172, size=16, numsize=21):
    body = ''
    lines = c.get('lines') or []
    def kwtags():
        return ''.join('<span class="kw">%s</span>' % k for k in c.get('kw', []))
    for i, l in enumerate(lines):
        body += '<div style="line-height: 26px">%s%s</div>' % (line_text(l, numsize=numsize, color=numcol if l[0] == 'dmg' else None), kwtags() if i == 0 else '')
    if c.get('modes'):
        body += '<div style="font-size: 13px; color: %s; line-height: 18px">どちらか一つ</div>' % INK_MID
        for m in c['modes']: body += '<div style="line-height: 26px">◆ %s</div>' % line_text(m[0], numsize=numsize, color=numcol if m[0][0] == 'dmg' else None)
    if c.get('pre'): body = body.replace(ROLE_JA['counter'], c['pre'] + ': ' + ROLE_JA['counter'])
    return '<div class="abs hand" style="left: 14px; right: 14px; top: %dpx; bottom: 40px; text-align: center; font-size: %dpx; color: %s">%s</div>' % (top, size, INK, body)

def card_B(c, dim=False, hover=False, numcol=None, name=None, cost=None, costcol=None, preview=None):
    ink = INK if not dim else rgba(INK, 0.7)
    nm = name or c['name']
    s = '<div style="position: relative; width: 200px; height: 290px; %s">' % ('filter: saturate(0.35) brightness(0.85);' if dim else '')
    s += frame_B(c['rarity'], dim, hover)
    s += orb(c['cost'] if cost is None else cost, col=costcol)
    s += '<div class="abs deco" style="left: 40px; right: 12px; top: 8px; height: 32px; display: flex; align-items: center; justify-content: center; font-size: %dpx; color: %s; white-space: nowrap">%s</div>' % (17 if len(nm) > 5 else (18 if len(nm) > 4 else 20), ink, nm)
    s += window_B(c['id'], dim)
    s += ribbon(c['typ'], TYPE_JA[c['typ']], gemr=c['rarity'])
    s += body_B(c, numcol=numcol)
    if preview: s += '<div class="abs hand num" style="left: 0; right: 0; bottom: 42px; text-align: center; font-size: 13px; color: %s">%s</div>' % (GOLD_INK, preview)
    if c.get('notes'): s += tape(58, 258, 84, c['notes'])
    s += '</div>'
    return s

# ---- 案C: 大きな数字 (本文の代わりに主効果を 40px の数字で) ----
def card_C(c, dim=False, hover=False, numcol=None):
    ink = INK if not dim else rgba(INK, 0.7)
    s = '<div style="position: relative; width: 200px; height: 290px; %s">' % ('filter: saturate(0.35) brightness(0.85);' if dim else '')
    s += frame_B('common', dim, hover)
    s += orb(c['cost'])
    s += '<div class="abs" style="left: 40px; top: 6px">%s</div>' % gem(c['rarity'], 1)
    s += '<div class="abs deco" style="left: 40px; right: 12px; top: 8px; height: 32px; display: flex; align-items: center; justify-content: center; font-size: %dpx; color: %s; white-space: nowrap">%s</div>' % (17 if len(c['name']) > 5 else (18 if len(c['name']) > 4 else 20), ink, c['name'])
    s += window_B(c['id'], dim)
    lines = list(c.get('lines') or [])
    modes = c.get('modes')
    prim = None; rest = []
    if modes:
        prim = [m[0] for m in modes]
    elif lines:
        prim = [lines[0]]; rest = lines[1:]
    def big(l, w=84):
        role, val = l[0], l[1]
        return ('<div style="display: flex; flex-direction: column; align-items: center; width: %dpx">'
                '<div style="display: flex; align-items: center; gap: 4px">%s<span class="deco num" style="font-size: 40px; line-height: 44px; color: %s">%s</span></div>'
                '<div style="font-size: 13px; color: %s; margin-top: -2px">%s%s</div></div>') % (w, pixel_icon(ROLE_ICON[role], 1, main=INK, dark=INK), numcol if role == 'dmg' and numcol else INK, val, INK_MID, (l[2] + ' ') if len(l) > 2 else '', ROLE_JA[role])
    if prim:
        mid = '<div class="abs" style="left: 12px; right: 12px; top: 150px; height: 68px; display: flex; align-items: center; justify-content: center; gap: 4px">%s%s</div>' % (
            big(prim[0]), ('<div style="font-size: 13px; color: %s; padding: 0 2px">or</div>%s' % (INK_MID, big(prim[1]))) if len(prim) > 1 else '')
        s += mid
    sub = ''
    if c.get('pre'): sub += '<div>%s</div>' % c['pre']
    if c.get('kw'): sub += ''.join('<span class="kw" style="margin: 0 2px">%s</span>' % k for k in c['kw'])
    for l in rest: sub += '<div style="line-height: 22px">%s</div>' % line_text(l)
    s += '<div class="abs hand" style="left: 14px; right: 14px; top: 220px; bottom: 30px; text-align: center; font-size: 14px; color: %s">%s</div>' % (INK, sub)
    s += '<div class="abs hand" style="left: 10px; bottom: 8px; height: 22px; padding: 0 8px 0 6px; display: inline-flex; align-items: center; gap: 5px; border-radius: 8px; background: %s; box-shadow: 0 0 0 1.5px %s; font-size: 13px; color: %s">%s</div>' % (
        rgba(C[c['typ']], 0.55), rgba(INK, 0.7), INK, TYPE_JA[c['typ']])
    if c.get('notes'): s += tape(104, 258, 76, c['notes'], 3)
    s += '</div>'
    return s

# ---- 案D: 絵はがき (絵を額縁いっぱいに・下の紙帯に名前と本文・切手のコスト・封蝋のレア度) ----
def card_D(c, dim=False, hover=False, numcol=None):
    ink = INK if not dim else rgba(INK, 0.7)
    tcol = C[c['typ']]
    s = '<div style="position: relative; width: 200px; height: 290px; %s">' % ('filter: saturate(0.35) brightness(0.85);' if dim else '')
    s += frame_B('common', dim, hover)
    # 絵 (額縁いっぱい): 上の角は紙と同じ丸み
    s += ('<div class="abs" style="left: 3px; right: 3px; top: 3px; height: 134px; border-radius: 16px 20px 0 0 / 18px 14px 0 0; background: radial-gradient(ellipse at 50%% 40%%, #2c3050, #1a1c33 75%%); overflow: hidden; display: flex; align-items: center; justify-content: center; box-shadow: inset 0 -1.5px 0 %s">%s</div>' % (INK, art_img(c['id'], dim=dim)))
    # タイプのしおり (左上)
    s += '<div class="abs" style="left: 14px; top: -4px; width: 22px; height: 44px; background: %s; clip-path: polygon(0 0, 100%% 0, 100%% 100%%, 50%% 80%%, 0 100%%); box-shadow: inset 0 0 0 1.5px %s"></div>' % (tcol, rgba(INK, 0.8))
    # 切手のコスト (右上)
    s += ('<div class="abs deco num" style="right: 10px; top: 8px; width: 34px; height: 36px; background: %s; border: 2px dashed %s; outline: 2px solid %s; display: flex; align-items: center; justify-content: center; font-size: 20px; color: %s">%s</div>'
          % (PAPER, rgba(INK, 0.7), PAPER, ink, c['cost']))
    # 名前の帯 (絵の下端にかかる)
    s += ('<div class="abs" style="left: 12px; right: 12px; top: 124px; height: 30px; background: %s; box-shadow: 0 0 0 1.5px %s, 0 3px 6px rgba(0,0,0,0.25); display: flex; align-items: center; padding: 0 10px; gap: 8px">'
          '<span class="deco" style="font-size: %dpx; color: %s; white-space: nowrap">%s</span><span style="flex: 1"></span><span style="font-size: 13px; color: %s; letter-spacing: 0.06em">%s</span></div>'
          % (PAPER, INK, 16 if len(c['name']) > 5 else 18, ink, c['name'], INK_MID, TYPE_JA[c['typ']]))
    s += body_B(c, numcol=numcol, top=164, size=15, numsize=19)
    # 封蝋 (レア度)
    if c['rarity'] != 'common':
        col = C['sky'] if c['rarity'] == 'uncommon' else C['honey']
        s += ('<div class="abs" style="left: 12px; bottom: 10px; width: 30px; height: 30px; border-radius: 50%%; background: radial-gradient(circle at 40%% 35%%, %s, %s 70%%); box-shadow: 0 0 0 1.5px %s, 0 2px 3px rgba(0,0,0,0.3); display: flex; align-items: center; justify-content: center">%s</div>'
              % (lerp(col, '#ffffff', 0.3), col, rgba(INK, 0.6), pixel_icon('star', 1, main=lerp(col, '#ffffff', 0.5), dark=lerp(col, '#000000', 0.4))))
    if c.get('notes'): s += tape(104, 258, 76, c['notes'], 3)
    s += '</div>'
    return s

PATTERNS = [
 ('A', '整理', card_A, '今日のコントラスト是正後の姿。骨格は現行のまま (しおりにコスト・星のレア度・台紙の挿絵・左下の剣と右下の盾のにじみ)。<br>タイプ行は 13px の中墨、本文 15px。数字は本文とにじみの札に2度出る。', None),
 ('B', '本家型の帯', card_B, 'コスト玉・全幅の窓・<b>タイプの帯</b> (色と文字で「物理／呪文／リアクション／置物」)・本文の数字を大きく。にじみの札は廃止 (数字は1か所)。<br>レア度は外側の線の色 (C 墨・U 空・R 蜂蜜) と帯の宝石。貫通・消滅などは小さな角札。', '推奨'),
 ('C', '大きな数字', card_C, '主効果を 40px の数字にして「6 ダメージ」を一目で。副次効果と条件だけ本文に。スマホで最も読める。<br>タイプは左下の札、レア度は玉の横の宝石。文が長い札 (条件つき・置物) はこの型に収まりにくいのが弱点。', None),
 ('D', '絵はがき', card_D, '絵を額縁いっぱいに (窓 194×134)。名前は絵の下端にかかる紙の帯、コストは右上の切手、タイプは左上のしおり、レア度は左下の封蝋。<br>本文は下の紙。絵が主役になる分、文の場所が狭い。', None),
]

def main_board():
    W = 1760; row_h = 430; top = 150
    H = top + row_h * len(PATTERNS) + 30
    s = '<div style="position: relative; width: %dpx; height: %dpx; overflow: hidden; background: #1a1c33">' % (W, H)
    s += '<div class="abs" style="inset: 0; background: radial-gradient(ellipse at 20% 10%, rgba(120,90,160,0.25), rgba(0,0,0,0) 50%), radial-gradient(ellipse at 80% 30%, rgba(70,120,140,0.2), rgba(0,0,0,0) 50%), linear-gradient(180deg, #26294a 0%, #12142a 100%)"></div>'
    s += '<div class="abs deco light" style="left: 40px; top: 26px; font-size: 30px; letter-spacing: 0.06em; text-shadow: 0 2px 0 rgba(0,0,0,0.5)">カードの面・第2版 — 同じ6枚を4つの案で</div>'
    s += ('<div class="abs note" style="left: 40px; top: 74px; width: 1500px">左から 打撃 (C 物理)・牙の一撃 (U 物理・貫通)・絡み蔦 (C 選択式)・茨の返し (C リアクション)・年輪の大樹 (R 置物)・芽吹き (C 呪文・消滅)。挿絵は本物 (80×48 を2倍)、文字は Kaisei Decol (名前・数字) と Klee One (本文)。'
          '4案とも紙と鉛筆線の「絵本」の肌は同じで、変えるのは <b>情報の置き方</b> (コスト・タイプ・レア度・数字の見せ方)。</div>')
    for r, (key, title, fn, desc, badge) in enumerate(PATTERNS):
        y = top + r * row_h
        s += '<div class="abs" style="left: 40px; top: %dpx; width: 1680px; height: 1px; background: rgba(244,236,214,0.18)"></div>' % (y - 14)
        s += '<div class="abs deco light" style="left: 40px; top: %dpx; font-size: 34px; text-shadow: 0 2px 0 rgba(0,0,0,0.5)">%s</div>' % (y + 6, key)
        s += '<div class="abs deco light" style="left: 84px; top: %dpx; font-size: 22px; text-shadow: 0 2px 0 rgba(0,0,0,0.5)">%s</div>' % (y + 14, title)
        if badge: s += '<div class="abs hand" style="left: 210px; top: %dpx; height: 26px; padding: 0 10px; display: inline-flex; align-items: center; border-radius: 8px; background: %s; color: %s; font-size: 14px; box-shadow: 0 0 0 1.5px %s">%s</div>' % (y + 16, C['honey'], INK, INK, badge)
        s += '<div class="abs note" style="left: 40px; top: %dpx; width: 250px; font-size: 13px; line-height: 19px; opacity: 0.92">%s</div>' % (y + 56, desc)
        for i, c in enumerate(CARDS):
            x = 320 + i * 240
            s += '<div class="abs" style="left: %dpx; top: %dpx; width: 200px; height: 290px; transform: scale(1.15) rotate(%sdeg); transform-origin: 0 0">%s</div>' % (x, y + 40, (-1.0, 0.7, -0.4, 1.0, -0.8, 0.5)[i], fn(c))
    s += '</div>'
    return s

# ---- 推奨案 (B) の状態一覧 ----
def states_board():
    W = 1760; H = 900
    s = '<div style="position: relative; width: %dpx; height: %dpx; overflow: hidden; background: #1a1c33">' % (W, H)
    s += '<div class="abs" style="inset: 0; background: linear-gradient(180deg, #26294a 0%, #12142a 100%)"></div>'
    s += '<div class="abs deco light" style="left: 40px; top: 26px; font-size: 30px; letter-spacing: 0.06em; text-shadow: 0 2px 0 rgba(0,0,0,0.5)">案B の状態一覧 — 1枚の札が戦闘中に取る姿</div>'
    s += '<div class="abs note" style="left: 40px; top: 72px; width: 1600px">本家のカードの読み方に合わせる: 数字が上がれば緑、下がれば朱、鍛えた札は名前に「+」と緑の数字。使えない札は彩度を落とす。ホバーで 1.2倍と蜂蜜色の光。予測行は対象を決めた時だけ (金の墨)。</div>'
    strike = CARDS[0]; fang = CARDS[1]; entangle = CARDS[2]; thorns = CARDS[3]; tree = CARDS[4]; sprout = CARDS[5]
    x_flurry = dict(id='green_x_vine_flurry', name='蔦の連撃', typ='physical', cost='X', rarity='uncommon', lines=[('dmg', '5')], kw=['×X回'])
    bloom = dict(id='green_sig_rite_of_bloom', name='開花の儀', typ='spell', cost=2, rarity='rare', lines=[('growth', '2倍')], notes='消滅')
    stomp = dict(id='green_finisher_stomp', name='巨獣の踏みつけ', typ='physical', cost=5, rarity='rare', lines=[('dmg', '50')], notes='保持')
    items = [
        ('通常', card_B(strike)),
        ('使えない (エナジー不足)', card_B(fang, dim=True)),
        ('ホバー (1.2倍・光)', '<div style="transform: scale(1.2); transform-origin: 50%% 100%%">%s</div>' % card_B(strike, hover=True)),
        ('鍛えた (+)', card_B(dict(strike, name='打撃+', lines=[('dmg', '9')]), numcol=UP)),
        ('割引 (次のカード -1)', card_B(strike, cost=0, costcol=C['moss'])),
        ('X コスト', card_B(x_flurry)),
        ('消滅', card_B(bloom)),
        ('保持 (5E の大型)', card_B(stomp)),
        ('選択式', card_B(entangle)),
        ('リアクション (伏せる)', card_B(thorns)),
        ('置物 (レア)', card_B(tree)),
        ('弱体で下がる', card_B(dict(strike, lines=[('dmg', '4')]), numcol=DOWN)),
        ('成長 +3 で上がる', card_B(dict(strike, lines=[('dmg', '9')]), numcol=UP)),
        ('対象を決めた時の予測行', card_B(strike, preview='→ 実ダメ 3 (装甲5)')),
    ]
    for i, (label, html) in enumerate(items):
        col = i % 7; row = i // 7
        x = 40 + col * 245; y = 130 + row * 380
        s += '<div class="abs" style="left: %dpx; top: %dpx; width: 200px; height: 290px">%s</div>' % (x, y + 30, html)
        s += '<div class="abs note" style="left: %dpx; top: %dpx; width: 210px; font-size: 13px">%s</div>' % (x, y, label)
    s += '</div>'
    return s

# ---- 文字と墨の規約 (コントラスト比つき) ----
def type_board():
    W = 1760; H = 760
    s = '<div style="position: relative; width: %dpx; height: %dpx; overflow: hidden; background: #1a1c33">' % (W, H)
    s += '<div class="abs" style="inset: 0; background: linear-gradient(180deg, #26294a 0%, #12142a 100%)"></div>'
    s += '<div class="abs deco light" style="left: 40px; top: 26px; font-size: 30px; letter-spacing: 0.06em; text-shadow: 0 2px 0 rgba(0,0,0,0.5)">文字と墨の規約 — 今日の是正 (WCAG のコントラスト比)</div>'
    s += '<div class="abs note" style="left: 40px; top: 72px; width: 1600px">二次の文字は透明度で薄めず実色で持つ。紙の上の金と朱は「塗りの色」と「文字の墨」を分ける。舞台 (夜) の上の文字は縁取りだけでなく夜色の札に乗せる。文字の最小サイズは 13px (1920×1080 基準)。</div>'
    lilac = '#eddbf7'
    rows = [
        ('注記 (タイプ行・G・幕/行)', blend(INK, PAPER, 0.62), INK_MID, PAPER, '11px 墨62%', '13px 中墨 #574b48'),
        ('金の文字 (レア枠・価格・クリックで選ぶ)', C['honey'], GOLD_INK, PAPER, '13px 蜂蜜 #e0b25a', '13px 金の墨 #7a4e12'),
        ('朱の文字 (エラー・買えない価格)', '#c8583f', '#9c3a2a', PAPER, '15px 朱 #c8583f', '15px 朱の墨 #9c3a2a'),
        ('状態異常の札 (弱体 1T・脆弱)', C['plum'], PLUM_INK, lilac, '13px 藤 #a98cc4', '14px 藤の墨 #5a3d78'),
        ('上がった数字 (成長・鍛えた)', '#3f8f4a', UP, PAPER, '#3f8f4a', '#276a34'),
        ('下がった数字 (弱体)', '#c0453a', DOWN, PAPER, '#c0453a', '#a33a30'),
        ('夜の上の淡い文字 (地図の注記)', blend(PAPER, NIGHT, 0.72), '#c4beb2', NIGHT, '紙色72%', '実色 #c4beb2'),
    ]
    y0 = 130
    s += '<div class="abs note" style="left: 40px; top: %dpx; width: 360px; font-size: 13px; opacity: 0.8">用途</div><div class="abs note" style="left: 420px; top: %dpx; font-size: 13px; opacity: 0.8">是正前</div><div class="abs note" style="left: 900px; top: %dpx; font-size: 13px; opacity: 0.8">是正後</div>' % (y0 - 26, y0 - 26, y0 - 26)
    for i, (use, before, after, bg, b_label, a_label) in enumerate(rows):
        y = y0 + i * 62
        s += '<div class="abs note" style="left: 40px; top: %dpx; width: 360px; font-size: 14px; line-height: 20px">%s</div>' % (y + 12, use)
        for x, col, label in ((420, before, b_label), (900, after, a_label)):
            cr = contrast(col, bg)
            ok = cr >= 4.5
            s += ('<div class="abs" style="left: %dpx; top: %dpx; width: 300px; height: 44px; background: %s; border-radius: 10px; box-shadow: 0 0 0 1.5px %s; display: flex; align-items: center; padding: 0 14px; gap: 12px">'
                  '<span class="hand" style="font-size: 15px; color: %s">%s</span></div>'
                  '<div class="abs hand" style="left: %dpx; top: %dpx; font-size: 15px; color: %s">%.1f : 1 %s</div>') % (
                x, y, bg, INK if bg != NIGHT else rgba(PAPER, 0.4), col, label, x + 312, y + 12, '#9fd8a0' if ok else '#f0a08a', cr, '✓' if ok else '✗ 読めない')
    # 夜の札
    y = y0 + len(rows) * 62 + 6
    s += '<div class="abs note" style="left: 40px; top: %dpx; width: 360px; font-size: 14px; line-height: 20px">舞台の上の注記 (伏せ分岐・敵の特性・手札の数)</div>' % (y + 12)
    s += ('<div class="abs" style="left: 420px; top: %dpx; width: 300px; height: 44px; border-radius: 10px; background: linear-gradient(90deg, #3d6a3a, #5a8a4a 50%%, #2e4a4a); display: flex; align-items: center; padding: 0 14px">'
          '<span class="hand" style="font-size: 13px; color: %s; text-shadow: 0 0 2px #000, 0 0 2px #000">伏せ札あり→用心の2連 / なし→裂き</span></div>'
          '<div class="abs hand" style="left: 732px; top: %dpx; font-size: 15px; color: #f0a08a">縁取りだけ ✗</div>') % (y, PAPER, y + 12)
    s += ('<div class="abs" style="left: 900px; top: %dpx; width: 300px; height: 44px; border-radius: 10px; background: linear-gradient(90deg, #3d6a3a, #5a8a4a 50%%, #2e4a4a); display: flex; align-items: center; padding: 0 8px">'
          '<span class="hand" style="font-size: 13px; color: %s; background: rgba(26,28,51,0.84); box-shadow: 0 0 0 1.5px #0c0d1a; border-radius: 8px; padding: 4px 9px">伏せ札あり→用心の2連 / なし→裂き</span></div>'
          '<div class="abs hand" style="left: 1212px; top: %dpx; font-size: 15px; color: #9fd8a0">夜の札 9.2 : 1 ✓</div>') % (y, PAPER, y + 12)
    # 文字の階段
    y2 = y + 70
    s += '<div class="abs note" style="left: 40px; top: %dpx; width: 1600px; font-size: 13px; opacity: 0.8">文字の階段 (1920×1080 基準)</div>' % y2
    scale = [('注記 13', 13, 'hand', INK_MID), ('本文 15', 15, 'hand', INK), ('本文の数字 21', 21, 'deco', INK), ('名前 19', 19, 'deco', INK), ('意図 26', 26, 'deco', INK), ('大きな数字 40', 40, 'deco', INK)]
    x = 40
    for label, size, cls, col in scale:
        s += ('<div class="abs" style="left: %dpx; top: %dpx; height: 70px; padding: 0 16px; background: %s; border-radius: 12px; box-shadow: 0 0 0 1.5px %s; display: flex; flex-direction: column; justify-content: center; align-items: center; gap: 2px">'
              '<span class="%s" style="font-size: %dpx; line-height: 1.1; color: %s">打撃 17</span><span style="font-size: 12px; color: %s">%s</span></div>') % (x, y2 + 24, PAPER, INK, cls, size, col, INK_MID, label)
        x += 190
    s += ('<div class="abs note" style="left: 40px; top: %dpx; width: 1600px; font-size: 13px; opacity: 0.85">スマホの注意: 1920 基準の 13px は Galaxy S25 の画面で約 0.9mm (Android の最小目安の半分以下)。スマホで読ませるなら UI 全体を 1.5〜1.7倍にする別の決定が要る (CLAUDE.md「やらないこと: モバイル対応」の見直し)。</div>') % (y2 + 110)
    s += '</div>'
    return s

def write(name, body):
    with open(os.path.join(OUT, name), 'w', encoding='utf-8') as f:
        f.write(HEAD + body + TAIL)

write('Main.dc.html', main_board())
write('States.dc.html', states_board())
write('Type.dc.html', type_board())
canvas = {
    'artboards': [
        {'file': 'Main.dc.html', 'title': '4つの案 × 同じ6枚', 'x': 0, 'y': 0, 'w': 1760, 'h': 150 + 430 * 4 + 30},
        {'file': 'States.dc.html', 'title': '案B の状態一覧', 'x': 1880, 'y': 0, 'w': 1760, 'h': 900},
        {'file': 'Type.dc.html', 'title': '文字と墨の規約 (コントラスト比)', 'x': 1880, 'y': 1040, 'w': 1760, 'h': 760},
    ],
    'annotations': [
        {'id': 'brief', 'x': 0, 'y': -220, 'w': 700, 'text': 'カードの面・第2版 (2026-09-09)。「絵本」の肌 (紙・鉛筆線・水彩) は固定し、情報の置き方だけを4案で比べる。A は今日のコントラスト是正後の現行、B は本家 (StS) の帯の文法、C は数字を主役に、D は絵を主役に。推奨は B: タイプ行の読めなさと数字の二重表示を一度に解き、状態 (鍛えた・弱体・割引) の表現も本家の読み方に乗る。'},
        {'id': 'note-states', 'x': 1880, 'y': -120, 'w': 560, 'text': '状態は案が決まってから他案にも同じ規則で入れる。数字の色 (上がる=緑 #276a34・下がる=朱 #a33a30) と「+」は4案共通。'},
    ],
    'launch': {'view': 'canvas'},
}
with open(os.path.join(OUT, 'canvas.json'), 'w', encoding='utf-8') as f:
    json.dump(canvas, f, ensure_ascii=False, indent=2)
print('written card-design v1')

# build.py — 戦闘UIデザインカンバス v3「静かな夜」: 暗いガラスの面・細い金の線・明朝の名前・数字は Cinzel。
# 骨格 (配置) は v1 のまま。ごつい斜面・木目・羊皮紙・太縁の札は撤去。値は unity/Assets/Game/Theme.cs の色を土台に再構成。
import json, random, os
OUT = os.path.dirname(os.path.abspath(__file__))

C = dict(bg='#0f1412', text='#eef2ea', dim='rgba(238,242,234,0.55)', faint='rgba(238,242,234,0.14)',
         gold='#e0b84a', goldSoft='#f0d58a', hp='#c94f4f', block='#6f9fd8', bad='#e06c6c', accent='#6abf69',
         physical='#b08a5a', spell='#9a7fd0', reaction='#5fb0a8', permanent='#d1a33a', green='#5fb85a', blue='#4f8fd6', red='#d65a4f', white='#e8e2c8', black='#8a6fb0',
         skyTop='#162919', skyBot='#0a0f0b', groundTop='#1f3322', groundBot='#0d170e', horizon='#364c39')
ROLE = dict(dmg='#e06c6c', block='#6f9fd8', counter='#5fb0a8', growth='#6abf69', momentum='#e0b84a', expose='#e0a04a', shatter='#c9a27a', draw='#9fb0d8', mana='#f0d58a')
ROLE_ICON = dict(dmg='sword', block='shield', counter='undo', growth='leaf', momentum='wind', expose='target', shatter='hammer', draw='deck', mana='bolt')

def lerp(a, b, t):
    a = a.lstrip('#'); b = b.lstrip('#')
    ra, ga, ba = int(a[0:2], 16), int(a[2:4], 16), int(a[4:6], 16)
    rb, gb, bb = int(b[0:2], 16), int(b[2:4], 16), int(b[4:6], 16)
    return '#%02x%02x%02x' % (round(ra + (rb - ra) * t), round(ga + (gb - ga) * t), round(ba + (bb - ba) * t))

def rgba(hexcol, a):
    h = hexcol.lstrip('#')
    return 'rgba(%d,%d,%d,%s)' % (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)

# ---- placeholder creature (アプリの Creature と同じ「idのハッシュから左右対称」のドット絵) ----
def creature_svg(seed, dark, mid, light, size, eyes=True):
    rng = random.Random(seed)
    n = 16
    mask = [[False] * n for _ in range(n)]
    for y in range(2, n - 1):
        for x in range(n // 2):
            cx = (x + 0.5) / (n / 2); cy = 1 - abs((y - n / 2) / (n / 2))
            mask[y][x] = rng.random() < 0.12 + 0.62 * cx * cy
    def at(x, y):
        if x < 0 or y < 0 or x >= n or y >= n: return False
        return mask[y][x if x < n // 2 else n - 1 - x]
    rects = []
    for y in range(n):
        for x in range(n):
            if at(x, y):
                edge = not at(x - 1, y) or not at(x + 1, y) or not at(x, y - 1) or not at(x, y + 1)
                col = dark if edge else (light if y < n * 0.45 else mid)
                rects.append('<rect x="%d" y="%d" width="1" height="1" fill="%s"></rect>' % (x, y, col))
            elif at(x - 1, y) or at(x + 1, y) or at(x, y - 1) or at(x, y + 1):
                rects.append('<rect x="%d" y="%d" width="1" height="1" fill="#0b0f0d"></rect>' % (x, y))
    if eyes:
        rects.append('<rect x="5" y="6" width="1" height="1" fill="#ffffff"></rect><rect x="10" y="6" width="1" height="1" fill="#ffffff"></rect>')
    return '<svg viewBox="0 0 16 16" width="%d" height="%d" shape-rendering="crispEdges" style="display:block">%s</svg>' % (size, size, ''.join(rects))

# ---- icons (stroke SVG, 24 grid) ----
PATHS = {
    'sword': '<path d="M14 4l6 6-9 9-3 1-3-3 1-3 8-10z"></path><path d="M5 19l-2 2"></path><path d="M13 7l4 4"></path>',
    'shield': '<path d="M12 3l7 3v5c0 5-3.5 8.5-7 10-3.5-1.5-7-5-7-10V6l7-3z"></path>',
    'heart': '<path d="M12 20s-7-4.5-7-10a4 4 0 0 1 7-2.5A4 4 0 0 1 19 10c0 5.5-7 10-7 10z"></path>',
    'coin': '<circle cx="12" cy="12" r="8"></circle><path d="M9 12h6M12 9v6"></path>',
    'bolt': '<path d="M13 3L5 14h6l-1 7 8-11h-6l1-7z"></path>',
    'deck': '<rect x="5" y="4" width="11" height="15" rx="1"></rect><path d="M9 8h11v13H9"></path>',
    'leaf': '<path d="M5 19C7 9 13 5 20 4c-1 8-5 14-15 15z"></path><path d="M5 19l8-8"></path>',
    'wind': '<path d="M3 8h11a3 3 0 1 0-3-3"></path><path d="M3 13h15a3 3 0 1 1-3 3"></path><path d="M3 18h7"></path>',
    'down': '<path d="M12 4v14"></path><path d="M6 12l6 6 6-6"></path>',
    'target': '<circle cx="12" cy="12" r="8"></circle><circle cx="12" cy="12" r="3"></circle><path d="M12 2v3M12 19v3M2 12h3M19 12h3"></path>',
    'eyeoff': '<path d="M3 3l18 18"></path><path d="M10 6.5A9 9 0 0 1 21 12a9 9 0 0 1-2.5 3.5"></path><path d="M6 8a9 9 0 0 0-3 4 9 9 0 0 0 11 5"></path>',
    'gear': '<circle cx="12" cy="12" r="3"></circle><path d="M12 2v3M12 19v3M2 12h3M19 12h3M4.9 4.9l2.1 2.1M17 17l2.1 2.1M4.9 19.1L7 17M17 7l2.1-2.1"></path>',
    'log': '<path d="M5 6h14M5 12h14M5 18h9"></path>',
    'undo': '<path d="M9 14L4 9l5-5"></path><path d="M4 9h10a6 6 0 0 1 0 12h-3"></path>',
    'clock': '<circle cx="12" cy="12" r="8"></circle><path d="M12 8v4l3 2"></path>',
    'arrow': '<path d="M4 12h14"></path><path d="M13 6l6 6-6 6"></path>',
    'warn': '<path d="M12 3l10 18H2L12 3z"></path><path d="M12 10v5M12 18v.5"></path>',
    'hammer': '<path d="M14 4l6 6-3 3-6-6 3-3z"></path><path d="M12 10L4 18l2 2 8-8"></path><path d="M9 5l3-3M19 15l3-3"></path>',
    'flag': '<path d="M5 21V4"></path><path d="M5 4h11l-2 4 2 4H5"></path>',
    'gem': '<path d="M6 3h12l4 6-10 12L2 9l4-6z"></path><path d="M2 9h20M10 3l2 18M14 3l-2 18"></path>',
}
def icon(name, size=20, color='currentColor', sw=1.8):
    return ('<svg width="%d" height="%d" viewBox="0 0 24 24" fill="none" stroke="%s" stroke-width="%s" stroke-linecap="round" stroke-linejoin="round" style="display:block;flex:none">%s</svg>'
            % (size, size, color, sw, PATHS[name]))

def crest_svg(typ, size=52, color=None):
    col = color or {'physical': '#d9c39a', 'spell': '#cdbdf0', 'reaction': '#9fd8d0', 'permanent': '#f0d58a'}[typ]
    if typ == 'physical':
        inner = '<path d="M8 40L40 8M8 8l32 32"></path><path d="M10 6h5v5M38 6h-5v5M6 38v-5h5M42 38v-5h-5"></path>'
    elif typ == 'spell':
        inner = '<path d="M24 5v38M5 24h38M10.6 10.6l26.8 26.8M37.4 10.6L10.6 37.4"></path><circle cx="24" cy="24" r="8"></circle>'
    elif typ == 'reaction':
        inner = '<path d="M4 24c6-9 14-13 20-13s14 4 20 13c-6 9-14 13-20 13S10 33 4 24z"></path><circle cx="24" cy="24" r="6"></circle>'
    else:
        inner = '<path d="M10 42h28M14 42V18M34 42V18M8 18h32M12 14l12-8 12 8"></path><path d="M21 42V28h6v14"></path>'
    return ('<svg width="%d" height="%d" viewBox="0 0 48 48" fill="none" stroke="%s" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" style="display:block">%s</svg>' % (size, size, col, inner))

HEAD = '''<!doctype html>
<html>
<head>
  <meta charset="utf-8">
  <script src="./support.js"></script>
</head>
<body>
<x-dc>
<helmet>
  <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Noto+Sans+JP:wght@400;500;700&amp;family=Shippori+Mincho+B1:wght@600;700&amp;family=Cinzel:wght@600;700&amp;family=Zen+Kurenaido&amp;display=swap">
  <style>
    body { margin: 0; background: #0f1412; font-family: "Noto Sans JP", "Hiragino Sans", "Yu Gothic", system-ui, sans-serif; color: #eef2ea; -webkit-font-smoothing: antialiased; }
    a { color: #e0b84a; } a:hover { color: #f0d58a; }
    .abs { position: absolute; }
    .serif { font-family: "Shippori Mincho B1", "Hiragino Mincho ProN", "Yu Mincho", serif; }
    .numeral { font-family: Cinzel, "Times New Roman", serif; font-weight: 700; font-variant-numeric: tabular-nums; }
    .glass { background: rgba(12,17,15,0.72); border: 1px solid rgba(238,242,234,0.14); border-radius: 8px; box-shadow: inset 0 1px 0 rgba(255,255,255,0.06), 0 8px 24px rgba(0,0,0,0.35); }
    .label { font-size: 11px; letter-spacing: 0.14em; color: rgba(238,242,234,0.55); font-weight: 500; }
    .pill { display: flex; align-items: center; gap: 6px; height: 26px; padding: 0 10px; border-radius: 13px; background: rgba(0,0,0,0.45); border: 1px solid rgba(238,242,234,0.14); font-size: 12px; font-weight: 500; white-space: nowrap; }
    .btn { display: flex; align-items: center; justify-content: center; gap: 10px; border-radius: 6px; font-weight: 700; }
    .btn-primary { background: #e0b84a; color: #15120a; box-shadow: 0 0 0 1px rgba(0,0,0,0.5), 0 6px 18px rgba(224,184,74,0.25); }
    .btn-ghost { background: rgba(0,0,0,0.35); border: 1px solid rgba(238,242,234,0.22); color: #eef2ea; }
    .keycap { display: inline-flex; align-items: center; justify-content: center; min-width: 18px; height: 18px; padding: 0 5px; border: 1px solid rgba(238,242,234,0.3); border-radius: 4px; color: rgba(238,242,234,0.6); font-size: 11px; font-weight: 500; font-family: "Noto Sans JP", sans-serif; }
  </style>
</helmet>
'''
TAIL = '''</x-dc>
</body>
</html>
'''

# ---- card (200×290) 「静かな夜」 ----
def role_glyph(kind, size=14):
    return '<span style="display:inline-flex; align-items:center; justify-content:center; width:%dpx; height:%dpx; flex:none">%s</span>' % (size + 4, size + 4, icon(ROLE_ICON[kind], size, ROLE[kind], 2))

def line(kind, text, color='#dfe5da'):
    if kind is None:
        return '<div style="line-height: 20px; color: %s">%s</div>' % (color, text)
    return ('<div style="display: flex; align-items: center; justify-content: center; gap: 4px; line-height: 20px; color: %s">%s<span>%s</span></div>' % (color, role_glyph(kind), text))

def value_badge(kind, value, right=False):
    col = ROLE[kind]
    side = 'right: 14px' if right else 'left: 14px'
    return ('<div class="abs numeral" style="%s; bottom: 12px; height: 30px; padding: 0 10px 0 8px; display: flex; align-items: center; gap: 6px; border-radius: 6px; background: %s; border: 1px solid %s; color: %s; font-size: 17px">%s<span>%s</span></div>'
            % (side, rgba(col, 0.16), rgba(col, 0.55), lerp(col, '#ffffff', 0.35), icon(ROLE_ICON[kind], 14, lerp(col, '#ffffff', 0.35), 2.2), value))

def card(name, typ, cost, rarity, body_lines, notes=None, preview=None, playable=True, key=None, mode_lines=None, dmg=None, blk=None, counter=None, color='green', scale=1.0):
    tcol = C[typ]
    type_ja = {'physical': '物理', 'spell': '呪文', 'reaction': 'リアクション', 'permanent': '置物'}[typ]
    rar = {'common': 'コモン', 'uncommon': 'アンコモン', 'rare': 'レア'}[rarity]
    gem = {'common': 'rgba(238,242,234,0.5)', 'uncommon': '#6f9fd8', 'rare': '#e0b84a'}[rarity]
    name_size = 16 if len(name) > 5 else (18 if len(name) > 4 else 20)
    body = ''.join(line(k, t) for k, t in body_lines)
    if mode_lines:
        body += '<div style="margin-top: 2px; display: flex; flex-direction: column; gap: 0px">' + ''.join(
            '<div style="display: flex; align-items: center; justify-content: center; gap: 6px; line-height: 20px"><span style="width: 5px; height: 5px; border-radius: 50%%; background: %s; flex: none"></span>%s</div>' % (ROLE[k], line(k, t).replace('justify-content: center; ', '')) for k, t in mode_lines) + '</div>'
    if preview:
        body += '<div class="numeral" style="line-height: 22px; font-size: 13px; color: %s; letter-spacing: 0.02em">%s</div>' % (C['goldSoft'], preview)
    badges = ''
    if dmg is not None: badges += value_badge('dmg', dmg)
    if counter is not None: badges += value_badge('counter', counter)
    if blk is not None: badges += value_badge('block', blk, right=True)
    center_note = ''
    if mode_lines and dmg is not None and blk is not None:
        center_note = '<div class="abs label" style="left: 0; right: 0; bottom: 20px; text-align: center; font-size: 10px">どちらか</div>'
    elif notes:
        center_note = '<div class="abs label" style="left: 0; right: 0; bottom: 20px; text-align: center; font-size: 10px; color: %s; letter-spacing: 0.1em">%s</div>' % (C['goldSoft'], notes)
    keycap = ('<div class="abs" style="right: 10px; top: -22px"><span class="keycap">%s</span></div>' % key) if key else ''
    dim_css = '' if playable else 'filter: saturate(0.4) brightness(0.7);'
    return ('<div class="card" style="position: relative; width: 200px; height: 290px; transform: scale(%s); transform-origin: 50%% 100%%; %s">'
            '<div class="abs" style="inset: 0; border-radius: 10px; background: linear-gradient(180deg, #1d2624 0%%, #11171500 60%%), #121816; border: 1px solid %s; box-shadow: 0 0 0 1px rgba(0,0,0,0.7), 0 12px 24px rgba(0,0,0,0.45), inset 0 1px 0 rgba(255,255,255,0.07)"></div>'
            '<div class="abs" style="left: 1px; right: 1px; top: 1px; height: 110px; border-radius: 9px 9px 0 0; background: linear-gradient(180deg, %s, rgba(0,0,0,0))"></div>'
            # コスト
            '<div class="abs numeral" style="left: 12px; top: 12px; width: 34px; height: 34px; border-radius: 50%%; border: 1px solid %s; background: rgba(0,0,0,0.55); display: flex; align-items: center; justify-content: center; font-size: 17px; color: %s">%s</div>'
            # レア度
            '<div class="abs" style="right: 14px; top: 20px">%s</div>'
            # 名前
            '<div class="abs serif" style="left: 50px; right: 36px; top: 12px; height: 34px; display: flex; align-items: center; justify-content: center; font-size: %dpx; font-weight: 700; color: #f4f6f0; white-space: nowrap; letter-spacing: 0.04em">%s</div>'
            '<div class="abs" style="left: 60px; right: 46px; top: 50px; height: 1px; background: linear-gradient(90deg, rgba(0,0,0,0), %s, rgba(0,0,0,0))"></div>'
            # 紋章の窓
            '<div class="abs" style="left: 14px; right: 14px; top: 60px; height: 92px; border-radius: 6px; background: radial-gradient(ellipse at 50%% 35%%, %s 0%%, rgba(0,0,0,0.25) 75%%); border: 1px solid rgba(255,255,255,0.05); display: flex; align-items: center; justify-content: center">%s</div>'
            # タイプ
            '<div class="abs label" style="left: 0; right: 0; top: 160px; text-align: center; font-size: 10px">%s · %s</div>'
            # 本文
            '<div class="abs" style="left: 16px; right: 16px; top: 180px; bottom: 50px; text-align: center; font-size: 13px; font-weight: 500">%s</div>'
            '%s%s%s</div>') % (
        scale, dim_css, rgba(tcol, 0.55), rgba(tcol, 0.28), rgba(C['gold'], 0.8), C['goldSoft'], cost,
        icon('gem', 14, gem, 1.6), name_size, name, C[color], rgba(tcol, 0.35), crest_svg(typ, 50), type_ja, rar, body, badges, center_note, keycap)

def card_back(w, h):
    return ('<div style="position: relative; width: %dpx; height: %dpx; border-radius: 8px; background: #0e1b1a; border: 1px solid %s; box-shadow: 0 8px 18px rgba(0,0,0,0.45); overflow: hidden">'
            '<div class="abs" style="inset: 8px; border-radius: 4px; border: 1px solid rgba(159,216,208,0.25); background-image: repeating-linear-gradient(135deg, rgba(159,216,208,0.08) 0 1px, transparent 1px 9px)"></div>'
            '<div class="abs" style="inset: 0; display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 10px; color: #9fd8d0">%s<div class="serif" style="font-size: 14px; font-weight: 700; letter-spacing: 0.1em">伏せ札</div></div></div>') % (w, h, rgba(C['reaction'], 0.6), icon('eyeoff', 26, '#9fd8d0', 1.6))

def hpbar(w, cur, mx, block=None, color=C['hp'], numeral=15):
    pct = max(0, min(100, cur / mx * 100))
    badge = ''
    if block is not None:
        badge = ('<div class="numeral abs" style="left: -54px; top: -9px; height: 28px; padding: 0 8px 0 6px; display: flex; align-items: center; gap: 4px; border-radius: 14px; background: rgba(0,0,0,0.5); border: 1px solid %s; color: %s; font-size: 14px">%s%d</div>'
                 % (rgba(C['block'], 0.6), lerp(C['block'], '#ffffff', 0.35), icon('shield', 14, lerp(C['block'], '#ffffff', 0.35), 2.2), block))
    return ('<div style="position: relative; width: %dpx; height: 10px; border-radius: 5px; background: rgba(255,255,255,0.08); box-shadow: inset 0 1px 0 rgba(0,0,0,0.6)">'
            '<div class="abs" style="left: 0; top: 0; bottom: 0; width: %.1f%%; border-radius: 5px; background: linear-gradient(90deg, %s, %s); box-shadow: 0 0 10px %s"></div>%s'
            '<div class="abs numeral" style="left: 0; right: 0; top: 14px; text-align: center; font-size: %dpx; color: #f4f6f0; letter-spacing: 0.04em">%d <span style="color: rgba(238,242,234,0.45); font-size: %dpx">/ %d</span></div></div>') % (
        w, pct, lerp(color, '#000000', 0.15), lerp(color, '#ffffff', 0.15), rgba(color, 0.35), badge, numeral, cur, max(11, numeral - 3), mx)

def pill(icon_name, label, color):
    return '<div class="pill" style="color: %s; border-color: %s">%s<span>%s</span></div>' % (lerp(color, '#ffffff', 0.25), rgba(color, 0.45), icon(icon_name, 14, lerp(color, '#ffffff', 0.25), 2), label)

def intent_capsule(kind, text, sub, color, cx, y):
    w = 220
    return ('<div class="abs glass" style="left: %dpx; top: %dpx; width: %dpx; height: 52px; display: flex; align-items: center; justify-content: center; gap: 10px; border-color: %s">'
            '%s<span class="numeral" style="font-size: 26px; color: %s; letter-spacing: 0.04em">%s</span></div>'
            '<div class="abs label" style="left: %dpx; top: %dpx; width: %dpx; text-align: center; font-size: 11px; letter-spacing: 0.06em; color: rgba(238,242,234,0.7); line-height: 16px">%s</div>'
            '<div class="abs" style="left: %dpx; top: %dpx; width: 1px; height: 22px; background: linear-gradient(180deg, %s, rgba(0,0,0,0))"></div>') % (
        cx - w // 2, y, w, rgba(color, 0.45), icon(kind, 24, color, 1.8), color, text, cx - w // 2 - 40, y + 58, w + 80, sub, cx, y + 52, rgba(color, 0.6))

def background(w=1920, h=1080):
    hy = round(h * 0.66)
    return ('<div class="abs" style="left: 0; top: 0; width: %dpx; height: %dpx; background: linear-gradient(180deg, %s 0%%, %s 100%%)"></div>'
            '<div class="abs" style="left: 0; top: %dpx; width: %dpx; height: %dpx; background: linear-gradient(180deg, %s 0%%, %s 100%%)"></div>'
            '<div class="abs" style="left: 0; top: %dpx; width: %dpx; height: 1px; background: %s"></div>'
            '<div class="abs" style="left: 0; top: %dpx; width: %dpx; height: 120px; background: linear-gradient(180deg, rgba(255,255,255,0.03), rgba(0,0,0,0))"></div>'
            '<div class="abs" style="left: 0; top: 0; width: %dpx; height: %dpx; background: radial-gradient(ellipse at 50%% 40%%, rgba(0,0,0,0) 40%%, rgba(0,0,0,0.6) 100%%)"></div>') % (
        w, hy, C['skyTop'], C['skyBot'], hy, w, h - hy, C['groundTop'], C['groundBot'], hy, w, rgba('#7fa88a', 0.35), hy, w, w, h)

def sprite(svg, x, y, size, ring=None):
    s = ''
    if ring:
        s += '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: 22px; border-radius: 50%%; border: 1px solid %s; box-shadow: 0 0 24px %s, inset 0 0 12px %s"></div>' % (x - 10, y + size - 12, size + 20, ring, rgba(ring, 0.5), rgba(ring, 0.25))
    s += '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: 26px; border-radius: 50%%; background: radial-gradient(ellipse, rgba(0,0,0,0.65), rgba(0,0,0,0) 70%%)"></div>' % (x, y + size - 14, size)
    s += '<div class="abs" style="left: %dpx; top: %dpx">%s</div>' % (x, y, svg)
    return s

LEADER = creature_svg('leader_green', '#1c3d2a', '#2e7a4a', '#7fe0a0', 220)
PROBE_A = creature_svg('enemy_probe', '#2a2438', '#5a4a7a', '#a48ad0', 200)
PROBE_B = creature_svg('enemy_probe_2', '#2a2438', '#5a4a7a', '#a48ad0', 200)

def topbar():
    relics = ['成長の種', '古根の杯', '商人の秤', '読みの眼鏡']
    rel = ''.join('<div style="width: 36px; height: 36px; border-radius: 50%%; border: 1px solid rgba(238,242,234,0.18); background: rgba(0,0,0,0.4); display: flex; align-items: center; justify-content: center" title="%s">%s</div>' % (r, creature_svg('relic:' + r, '#7a5a18', '#e0b84a', '#fff0a8', 20, eyes=False)) for r in relics)
    return ('<div class="abs" style="left: 0; top: 0; width: 1920px; height: 64px; background: linear-gradient(180deg, rgba(0,0,0,0.55), rgba(0,0,0,0)); display: flex; align-items: center; padding: 0 32px; gap: 28px; box-sizing: border-box">'
            '<div style="display: flex; align-items: baseline; gap: 14px"><span class="label">幕 1 · 行 4 / 16</span><span class="serif" style="font-size: 20px; font-weight: 700; letter-spacing: 0.06em">探り屋の二人組</span></div>'
            '<div class="pill" style="height: 28px">%s<span class="label" style="color: rgba(238,242,234,0.75); letter-spacing: 0.08em">ターン 3</span></div>'
            '<div style="flex: 1"></div>'
            '<div style="display: flex; align-items: center; gap: 8px">%s<span class="numeral" style="font-size: 20px; color: %s">67</span><span class="label">G</span></div>'
            '<div class="pill btn-ghost" style="height: 34px; padding: 0 14px; font-size: 13px; border-radius: 17px">%s<span>デッキ 14</span></div>'
            '<div style="display: flex; gap: 8px; align-items: center">%s</div>'
            '<div class="pill btn-ghost" style="height: 34px; padding: 0 14px; font-size: 13px; border-radius: 17px">%s<span>ログ</span></div>'
            '<div class="pill btn-ghost" style="width: 34px; height: 34px; padding: 0; justify-content: center; border-radius: 17px">%s</div>'
            '</div>') % (icon('clock', 14, 'rgba(238,242,234,0.6)'), icon('coin', 18, C['gold']), C['goldSoft'], icon('deck', 16, C['text']), rel, icon('log', 16, C['text']), icon('gear', 16, C['text']))

def player_zone(hp=62, block=8, worst=14):
    s = sprite(LEADER, 150, 380, 220)
    s += '<div class="abs serif" style="left: 100px; top: 600px; width: 320px; text-align: center; font-size: 20px; font-weight: 700; letter-spacing: 0.08em">大樹の巫女 このは</div>'
    s += '<div class="abs" style="left: 120px; top: 636px">%s</div>' % hpbar(280, hp, 80, block=block, numeral=16)
    s += ('<div class="abs" style="left: 110px; top: 676px; width: 460px; display: flex; align-items: center; gap: 8px; white-space: nowrap">'
          '<span class="pill" style="color: %s; border-color: %s; background: %s">%s<span>最悪被ダメ</span><span class="numeral" style="font-size: 14px">−%d</span></span>'
          '<span class="label" style="letter-spacing: 0.04em">→ HP <span class="numeral" style="font-size: 14px; color: #f4f6f0">%d</span>　ブロック %d を差し引き済</span></div>') % (
        lerp(C['bad'], '#ffffff', 0.2), rgba(C['bad'], 0.5), rgba(C['bad'], 0.12), icon('warn', 13, lerp(C['bad'], '#ffffff', 0.2), 2), worst, hp - worst, block)
    s += '<div class="abs" style="left: 110px; top: 710px; display: flex; gap: 8px">%s%s%s</div>' % (pill('leaf', '成長 +3', C['accent']), pill('wind', '勢い +2', C['gold']), pill('down', '弱体 1', '#a48ad0'))
    # 伏せ場
    s += '<div class="abs label" style="left: 424px; top: 374px; display: flex; align-items: center; gap: 6px">%s伏せ場 <span class="numeral" style="font-size: 12px; color: #f4f6f0">1 / 1</span></div>' % icon('eyeoff', 13, 'rgba(238,242,234,0.55)')
    s += '<div class="abs" style="left: 420px; top: 398px; padding: 8px; border-radius: 12px; border: 1px dashed rgba(159,216,208,0.4)">%s</div>' % card_back(120, 172)
    s += '<div class="abs label" style="left: 416px; top: 598px; width: 144px; text-align: center; font-size: 10px; line-height: 16px">被攻撃後に発動候補<br>回収 1E</div>'
    # 置物
    s += '<div class="abs label" style="left: 604px; top: 374px">置物</div>'
    perms = [('年輪の大樹', '毎T 成長+1', 'permanent', False), ('大樹の根', '2ターン目から上限4', 'permanent', True)]
    tiles = ''
    for nm, eff, typ, innate in perms:
        tiles += ('<div class="glass" style="width: 148px; height: 120px; padding: 10px 12px; box-sizing: border-box; display: flex; flex-direction: column; gap: 6px; opacity: %s; border-radius: 10px">'
                  '<div style="display: flex; align-items: center; gap: 8px">%s<div class="serif" style="font-size: 14px; font-weight: 700; white-space: nowrap">%s</div></div>'
                  '<div class="label" style="font-size: 11px; letter-spacing: 0.04em; line-height: 16px">%s</div></div>') % ('0.7' if innate else '1', crest_svg('permanent', 26, C['green'] if innate else None), nm, eff)
    s += '<div class="abs" style="left: 600px; top: 398px; display: flex; gap: 10px">%s</div>' % tiles
    return s

def enemy(cx, svg, name, hp, mx, intent, chips, ring=None, block=None):
    kind, text, sub, color = intent
    s = intent_capsule(kind, text, sub, color, cx, 292)
    s += sprite(svg, cx - 100, 400, 200, ring)
    s += '<div class="abs serif" style="left: %dpx; top: 604px; width: 300px; text-align: center; font-size: 20px; font-weight: 700; letter-spacing: 0.1em">%s</div>' % (cx - 150, name)
    s += '<div class="abs" style="left: %dpx; top: 640px">%s</div>' % (cx - 110, hpbar(220, hp, mx, block=block))
    s += '<div class="abs" style="left: %dpx; top: 680px; width: 300px; display: flex; justify-content: center; gap: 8px">%s</div>' % (cx - 150, ''.join(chips))
    return s

HAND_CARDS = [
    dict(name='打撃', typ='physical', cost=1, rarity='common', body_lines=[('dmg', 'ダメージ 6')], preview='→ 探り屋 に 6', dmg='6'),
    dict(name='蔦の楔', typ='physical', cost=1, rarity='common', body_lines=[('shatter', '粉砕: 敵ブロック全壊'), ('dmg', 'ダメージ 5')], preview='→ 探り屋 に 5', dmg='5'),
    dict(name='絡み蔦', typ='physical', cost=1, rarity='common', body_lines=[(None, 'どちらか1つ')], mode_lines=[('block', 'ブロック 7'), ('dmg', 'ダメージ 7')], dmg='7', blk='7'),
    dict(name='防御', typ='physical', cost=1, rarity='common', body_lines=[('block', 'ブロック 5')], blk='5'),
    dict(name='茨の返し', typ='reaction', cost=1, rarity='common', body_lines=[('counter', '被攻撃後: 返し 10')], notes='伏せる 1E', counter='10'),
]

def hand(hover_index=2):
    offs = [(-392, 34, -9), (-196, 10, -4.5), (0, 0, 0), (196, 10, 4.5), (392, 34, 9)]
    s = ''
    for i, (spec, (dx, dy, rot)) in enumerate(zip(HAND_CARDS, offs)):
        hovered = i == hover_index
        c = card(key=str(i + 1), **spec)
        sc = 0.92 if not hovered else 1.0
        lift = 0 if not hovered else -70
        glow = 'filter: drop-shadow(0 0 18px rgba(224,184,74,0.45));' if hovered else ''
        s += ('<div class="abs" style="left: %dpx; bottom: %dpx; width: 200px; height: 290px; transform: rotate(%sdeg) scale(%s); transform-origin: 50%% 100%%; z-index: %d; %s">%s</div>' % (
            960 - 100 + dx, 30 - dy - lift, 0 if hovered else rot, sc, 10 if hovered else i, glow, c))
    return s

def energy_ring(cur, mx, x, y, size=124):
    r = 54; circ = 2 * 3.14159 * r
    frac = cur / mx
    return ('<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx">'
            '<svg width="%d" height="%d" viewBox="0 0 124 124" style="display:block; transform: rotate(-90deg)"><circle cx="62" cy="62" r="%d" fill="rgba(0,0,0,0.45)" stroke="rgba(238,242,234,0.1)" stroke-width="6"></circle>'
            '<circle cx="62" cy="62" r="%d" fill="none" stroke="%s" stroke-width="6" stroke-linecap="round" stroke-dasharray="%.1f %.1f"></circle></svg>'
            '<div class="abs numeral" style="inset: 0; display: flex; align-items: baseline; justify-content: center; padding-top: 34px; color: #f4f6f0; font-size: 40px; text-shadow: 0 0 18px %s">%d<span style="font-size: 18px; color: rgba(238,242,234,0.5); margin-left: 4px">/ %d</span></div>'
            '<div class="abs label" style="left: 0; right: 0; bottom: 20px; text-align: center; font-size: 10px">エナジー</div></div>') % (
        x, y, size, size, size, size, r, r, C['gold'], circ * frac, circ * (1 - frac), rgba(C['gold'], 0.6), cur, mx)

def bottom_controls(energy=3, emax=4):
    s = energy_ring(energy, emax, 96, 838)
    s += '<div class="abs label" style="left: 96px; top: 966px; width: 124px; text-align: center; font-size: 10px; letter-spacing: 0.08em">上限 %d · 2ターン目から</div>' % emax
    s += ('<div class="abs pill" style="left: 40px; top: 1012px; height: 34px; padding: 0 14px; border-radius: 17px">%s<span class="numeral" style="font-size: 16px; color: #f4f6f0">12</span><span class="label">山札</span></div>') % icon('deck', 16, 'rgba(238,242,234,0.7)')
    s += ('<div class="abs pill" style="left: 1660px; top: 1012px; height: 34px; padding: 0 14px; border-radius: 17px; gap: 10px">%s<span class="numeral" style="font-size: 16px; color: #f4f6f0">3</span><span class="label">捨て札</span><span style="width: 1px; height: 14px; background: rgba(238,242,234,0.2)"></span><span class="numeral" style="font-size: 16px; color: #f4f6f0">1</span><span class="label">消滅</span></div>') % icon('undo', 16, 'rgba(238,242,234,0.7)')
    s += ('<div class="abs btn btn-ghost" style="left: 1650px; top: 872px; width: 230px; height: 64px; font-size: 20px; border-color: %s; letter-spacing: 0.1em; border-radius: 32px" ><span class="serif">ターン終了</span><span class="keycap">E</span></div>'
          '<div class="abs label" style="left: 1650px; top: 846px; width: 230px; text-align: right; font-size: 10px">手札 5 · 伏せ 1/1</div>') % rgba(C['gold'], 0.5)
    return s

def battle_scene(with_modal=False):
    s = '<div style="position: relative; width: 1920px; height: 1080px; overflow: hidden; background: #0f1412">'
    s += background()
    s += topbar()
    s += player_zone()
    if not with_modal:
        s += enemy(1180, PROBE_A, '探り屋', 31, 38, ('sword', '5〜7', '伏せ札あり → 12〜16 か 5〜7（順番を守らない）', C['bad']),
                   [pill('target', '急所 2', C['expose'] if 'expose' in C else '#e0a04a'), pill('down', '筋力 −2', '#8a9a90')], ring=C['gold'])
        s += enemy(1580, PROBE_B, '探り屋', 38, 38, ('sword', '12〜16', '本気の突き（三度目）', C['bad']), [pill('down', '筋力 −2', '#8a9a90')], block=6)
        s += hand(2)
        s += bottom_controls()
    else:
        s += enemy(1180, PROBE_A, '探り屋', 31, 38, ('sword', '5〜7', '伏せ札あり → 12〜16 か 5〜7', C['bad']),
                   [pill('target', '急所 2', '#e0a04a'), pill('down', '筋力 −2', '#8a9a90')])
        s += enemy(1580, PROBE_B, '探り屋', 38, 38, ('sword', '14', '実値 · 表示は 12〜16', C['bad']), [pill('down', '筋力 −2', '#8a9a90')], block=6, ring=C['bad'])
        s += hand(None)
        s += bottom_controls(energy=0)
        s += '<div class="abs" style="left: 0; top: 0; width: 1920px; height: 1080px; background: rgba(6,9,8,0.7); z-index: 50"></div>'
        s += reaction_modal()
    s += '</div>'
    return s

def reaction_modal():
    m = '<div class="abs glass" style="left: 480px; top: 270px; width: 960px; height: 520px; z-index: 51; border-radius: 14px; border-color: rgba(224,184,74,0.35); box-shadow: 0 40px 80px rgba(0,0,0,0.6), inset 0 1px 0 rgba(255,255,255,0.08)">'
    m += ('<div class="abs" style="left: 36px; top: 28px; right: 36px; display: flex; align-items: baseline; gap: 18px">'
          '<div class="serif" style="font-size: 26px; font-weight: 700; letter-spacing: 0.1em">リアクション</div><div class="label" style="font-size: 13px; letter-spacing: 0.1em">発動する？ 温存する？</div>'
          '<div class="label" style="margin-left: auto">被攻撃後（解決後）の窓</div></div>')
    m += ('<div class="abs" style="left: 36px; top: 84px; width: 420px; display: flex; flex-direction: column; gap: 16px">'
          '<div style="display: flex; flex-direction: column; gap: 12px; padding: 16px 18px; border-radius: 10px; background: rgba(0,0,0,0.35); border: 1px solid rgba(238,242,234,0.1)">'
          '<div class="label" style="display: flex; align-items: center; gap: 8px">%s探り屋（2体目）の行動</div>'
          '<div style="display: flex; align-items: baseline; gap: 12px"><span class="numeral" style="font-size: 44px; color: %s; line-height: 1">14</span><span class="label">実値 · 表示は 12〜16</span></div>'
          '<div style="height: 1px; background: rgba(238,242,234,0.1)"></div>'
          '<div style="display: flex; align-items: center; gap: 10px; font-size: 14px">%s<span>ブロック 8 で受け</span><span class="label">→</span><span class="numeral" style="font-size: 18px; color: %s">−6</span><span class="label">HP 62 → 56</span></div>'
          '</div>'
          '<div style="display: flex; gap: 10px; align-items: flex-start; padding: 12px 14px; border-radius: 10px; border: 1px solid %s; background: %s; font-size: 13px; line-height: 19px; color: %s">%s<span>発動すると後続の探り屋は「伏せなし」の分岐で確定する（5〜7 → 12〜16 の可能性）</span></div>'
          '<div class="label" style="font-size: 12px; line-height: 18px; letter-spacing: 0.04em">温存すれば札は伏せたまま残り、次のターンも同じ窓が開く。回収は自ターンに 1E。</div>'
          '</div>') % (icon('sword', 14, C['bad'], 2), C['bad'], icon('shield', 18, C['block']), C['bad'], rgba(C['gold'], 0.4), rgba(C['gold'], 0.08), C['goldSoft'], icon('warn', 18, C['gold']))
    m += '<div class="abs label" style="left: 520px; top: 84px">発動できる伏せ札</div>'
    m += '<div class="abs" style="left: 520px; top: 112px; width: 200px; height: 290px">%s</div>' % card('茨の返し', 'reaction', 1, 'common', [('counter', '被攻撃後: 返し 10')], preview='→ 探り屋 38 → 28', counter='10')
    m += ('<div class="abs" style="left: 748px; top: 120px; width: 176px; display: flex; flex-direction: column; gap: 12px">'
          '<div class="btn btn-primary" style="height: 56px; font-size: 18px"><span class="serif" style="letter-spacing: 0.12em">発動</span><span class="keycap" style="color: #15120a; border-color: rgba(0,0,0,0.4)">F</span></div>'
          '<div class="btn btn-ghost" style="height: 48px; font-size: 15px">温存する <span class="keycap">H</span></div>'
          '<div class="label" style="font-size: 10px; line-height: 15px; text-align: center; letter-spacing: 0.04em">発動後は捨て札へ。<br>消滅持ちは消滅置き場へ</div></div>')
    m += ('<div class="abs label" style="left: 36px; right: 36px; top: 462px; display: flex; align-items: center; gap: 10px; font-size: 12px">%s<span>この後 探り屋（1体目）の行動が続く：攻撃 5〜7</span></div>') % icon('arrow', 14, 'rgba(238,242,234,0.5)')
    m += '</div>'
    return m

# ---- カードの面 (役割の読み方) ----
def card_anatomy():
    W, H = 1760, 800
    s = '<div style="position: relative; width: %dpx; height: %dpx; overflow: hidden; background: #0f1412">' % (W, H)
    s += background(W, H)
    s += '<div class="abs serif" style="left: 40px; top: 26px; font-size: 28px; font-weight: 700; letter-spacing: 0.1em">カードの面</div>'
    s += ('<div class="abs" style="left: 40px; top: 70px; font-size: 13px; color: rgba(238,242,234,0.7); line-height: 20px; width: 1100px">暗い面に、タイプの色は縁と紋章の光だけ。名前は明朝、数字は Cinzel。<b style="color: #f4f6f0">左下の剣＝与えるダメージ、右下の盾＝得るブロック</b>。数字だけ見れば攻めか守りか分かる。1枚ずつの絵は最後なので、窓にはタイプの紋章。</div>')
    cards = [
        (card('打撃', 'physical', 1, 'common', [('dmg', 'ダメージ 6')], preview='→ 探り屋 に 6', dmg='6'), '攻撃'),
        (card('防御', 'physical', 1, 'common', [('block', 'ブロック 5')], blk='5'), '防御'),
        (card('絡み蔦', 'physical', 1, 'common', [(None, 'どちらか1つ')], mode_lines=[('block', 'ブロック 7'), ('dmg', 'ダメージ 7')], dmg='7', blk='7'), '択'),
        (card('二連の蔦打ち', 'physical', 1, 'common', [('dmg', 'ダメージ 5 ×2')], preview='→ 探り屋 に 5+5', dmg='5×2'), '多段'),
        (card('疾風の号砲', 'spell', 1, 'uncommon', [('momentum', '勢い +3'), ('momentum', '勢い 2倍')], notes='消滅'), '補助'),
        (card('茨の返し', 'reaction', 1, 'common', [('counter', '被攻撃後: 返し 10')], notes='伏せる 1E', counter='10'), '返し'),
        (card('年輪の大樹', 'permanent', 2, 'rare', [('block', 'ブロック 5'), ('growth', '毎T開始時: 成長 +1')], blk='5'), '置物'),
    ]
    sc = 1.15; step = 236
    for i, (c, role) in enumerate(cards):
        x = 60 + i * step
        s += '<div class="abs" style="left: %dpx; top: 140px; width: 200px; height: 290px; transform: scale(%s); transform-origin: 0 0">%s</div>' % (x, sc, c)
        s += '<div class="abs serif" style="left: %dpx; top: 490px; width: %dpx; text-align: center; font-size: 18px; font-weight: 700; color: %s; letter-spacing: 0.2em">%s</div>' % (x, 200 * sc, C['goldSoft'], role)
    s += '<div class="abs label" style="left: 60px; top: 560px">本文のしるし</div>'
    leg = [('dmg', 'ダメージ'), ('block', 'ブロック'), ('counter', '返し'), ('growth', '成長'), ('momentum', '勢い'), ('expose', '急所'), ('shatter', '粉砕'), ('draw', 'ドロー'), ('mana', 'エナジー')]
    s += '<div class="abs" style="left: 60px; top: 586px; display: flex; gap: 10px; flex-wrap: wrap; width: 1640px">'
    for k, t in leg:
        s += '<div class="pill" style="height: 30px; color: %s; border-color: %s">%s<span>%s</span></div>' % (lerp(ROLE[k], '#ffffff', 0.25), rgba(ROLE[k], 0.45), icon(ROLE_ICON[k], 14, lerp(ROLE[k], '#ffffff', 0.25), 2), t)
    s += '</div>'
    s += ('<div class="abs" style="left: 60px; top: 650px; width: 1640px; font-size: 12px; color: rgba(238,242,234,0.55); line-height: 20px; letter-spacing: 0.02em">'
          '実装メモ: 面は 1px の縁＋上部のタイプ色の光（9スライスは色を掛けるだけの1種で足りる）。剣と盾の札は効果から導く（dealDamage の合計→剣、gainBlock/gainIceBlock→盾、counter→戻り矢印。データにカテゴリは増やさない）。'
          '手札では 0.92 倍・ホバーで 1.0 と金の淡い光。名前 Shippori Mincho B1 20px／本文 Noto Sans JP 13px 500／数字 Cinzel。使えない札は彩度と明度を落とす。</div>')
    s += '</div>'
    return s

# ---- 見た目の別案 (同じ部品を3つの肌で) ----
def styles_board():
    W, H = 1760, 620
    s = '<div style="position: relative; width: %dpx; height: %dpx; overflow: hidden; background: #0f1412">' % (W, H)
    s += '<div class="abs serif" style="left: 40px; top: 26px; font-size: 28px; font-weight: 700; letter-spacing: 0.1em">見た目の方向 — 同じ部品を3つの肌で</div>'
    s += '<div class="abs label" style="left: 40px; top: 68px; font-size: 12px">A が本命（左の2枚に適用済み）。B・C は同じ骨格に別の肌を着せた場合。</div>'
    cols = [
        ('A', '静かな夜', '暗いガラスの面・細い金の線・明朝。夜の塔の空気を壊さず、数字と名前だけを浮かせる', '派手さは無い。ドット絵の敵が主役になる分、UIは引く',
         dict(bg='linear-gradient(180deg, #162919, #0a0f0b)', panel='rgba(12,17,15,0.72)', border='rgba(238,242,234,0.16)', text='#eef2ea', dim='rgba(238,242,234,0.55)', radius='8px', shadow='0 8px 24px rgba(0,0,0,0.35)', accent='#e0b84a', hp='#c94f4f', bad='#e06c6c', block='#6f9fd8', numeral='#f4f6f0', btnbg='#e0b84a', btntext='#15120a', cardbg='linear-gradient(180deg, #1d2624, #121816)', cardborder='rgba(176,138,90,0.55)')),
        ('B', '白の版画', '生成りの紙に黒い線と赤。版画の刷りのような硬い輪郭で、情報が紙に刷られている感じ', '夜の舞台と対比が強く、目が疲れる人もいる。暗い敵の絵と喧嘩しやすい',
         dict(bg='linear-gradient(180deg, #2a2622, #15130f)', panel='#efe9dc', border='#15130f', text='#15130f', dim='rgba(21,19,15,0.6)', radius='2px', shadow='4px 4px 0 #15130f', accent='#c8321e', hp='#c8321e', bad='#c8321e', block='#1f4e8c', numeral='#15130f', btnbg='#15130f', btntext='#efe9dc', cardbg='#f3eee2', cardborder='#15130f')),
        ('C', '霧の水彩', '青緑の霧のような柔らかい面と丸み。光がにじむ。ゆるかわの住人と相性がよい', '締まりが出にくく、緊張の場面（確認ウィンドウ）で圧が弱い',
         dict(bg='linear-gradient(180deg, #1b3a3d, #0d1c20)', panel='linear-gradient(180deg, rgba(120,170,175,0.22), rgba(40,80,90,0.28))', border='rgba(190,230,230,0.28)', text='#f2fbfa', dim='rgba(242,251,250,0.6)', radius='16px', shadow='0 12px 30px rgba(0,0,0,0.35), 0 0 30px rgba(120,200,200,0.12)', accent='#ffd27a', hp='#ff7a6e', bad='#ff9a8e', block='#8ec5ff', numeral='#ffffff', btnbg='#ffd27a', btntext='#1b2a2c', cardbg='linear-gradient(180deg, rgba(120,170,175,0.25), rgba(30,60,70,0.6))', cardborder='rgba(190,230,230,0.35)')),
    ]
    for i, (tag, title, why, cost, t) in enumerate(cols):
        x = 40 + i * 570
        s += '<div class="abs" style="left: %dpx; top: 100px; width: 540px; height: 490px; border-radius: 12px; background: %s; border: 1px solid rgba(238,242,234,0.1); overflow: hidden">' % (x, t['bg'])
        s += ('<div class="abs" style="left: 22px; top: 18px; display: flex; align-items: baseline; gap: 12px"><span class="numeral" style="font-size: 22px; color: %s">%s</span><span class="serif" style="font-size: 22px; font-weight: 700; color: %s; letter-spacing: 0.1em">%s</span></div>') % (t['accent'], tag, t['text'] if tag != 'B' else '#efe9dc', title)
        # 部品1: 意図カプセル＋名前＋HP
        s += ('<div class="abs" style="left: 22px; top: 70px; width: 236px; height: 52px; border-radius: %s; background: %s; border: 1px solid %s; box-shadow: %s; display: flex; align-items: center; justify-content: center; gap: 10px">%s<span class="numeral" style="font-size: 24px; color: %s">12〜16</span></div>'
              '<div class="abs serif" style="left: 22px; top: 134px; width: 236px; text-align: center; font-size: 18px; font-weight: 700; color: %s; letter-spacing: 0.1em">探り屋</div>'
              '<div class="abs" style="left: 40px; top: 166px; width: 200px; height: 10px; border-radius: %s; background: rgba(0,0,0,0.25); border: 1px solid %s"><div style="width: 80%%; height: 100%%; border-radius: inherit; background: %s"></div></div>'
              '<div class="abs numeral" style="left: 40px; top: 182px; width: 200px; text-align: center; font-size: 14px; color: %s">31 <span style="opacity: 0.5">/ 38</span></div>') % (
            t['radius'], t['panel'], t['border'], t['shadow'], icon('sword', 22, t['bad'], 1.8), t['bad'] if tag != 'B' else '#c8321e',
            '#efe9dc' if tag == 'B' else t['text'], '5px' if tag != 'B' else '0', t['border'] if tag == 'B' else 'rgba(0,0,0,0)', t['hp'], '#efe9dc' if tag == 'B' else t['numeral'])
        # 部品2: ボタン
        s += ('<div class="abs" style="left: 22px; top: 224px; width: 236px; display: flex; flex-direction: column; gap: 10px">'
              '<div class="btn" style="height: 48px; font-size: 16px; border-radius: %s; background: %s; color: %s; box-shadow: %s"><span class="serif" style="letter-spacing: 0.12em">発動</span></div>'
              '<div class="btn" style="height: 44px; font-size: 14px; border-radius: %s; background: %s; color: %s; border: 1px solid %s">温存する</div></div>') % (
            t['radius'], t['btnbg'], t['btntext'], t['shadow'], t['radius'], t['panel'] if tag != 'A' else 'rgba(0,0,0,0.35)', t['text'] if tag == 'B' else t['text'], t['border'])
        # 部品3: カード (スキンを当てる簡易版)
        s += '<div class="abs" style="left: 300px; top: 70px">%s</div>' % skin_card(t, tag)
        s += ('<div class="abs" style="left: 22px; bottom: 18px; width: 496px; font-size: 12px; line-height: 18px; color: %s"><b style="color: %s">ねらい:</b> %s<br><b style="color: %s">代償:</b> %s</div>') % (
            'rgba(238,242,234,0.75)', t['accent'], why, t['accent'], cost)
        s += '</div>'
    s += '</div>'
    return s

def skin_card(t, tag):
    hard = tag == 'B'
    radius = t['radius']
    return ('<div style="position: relative; width: 200px; height: 290px">'
            '<div class="abs" style="inset: 0; border-radius: %s; background: %s; border: %s solid %s; box-shadow: %s"></div>'
            '<div class="abs numeral" style="left: 12px; top: 12px; width: 34px; height: 34px; border-radius: 50%%; border: 1px solid %s; background: %s; display: flex; align-items: center; justify-content: center; font-size: 17px; color: %s">1</div>'
            '<div class="abs serif" style="left: 50px; right: 36px; top: 12px; height: 34px; display: flex; align-items: center; justify-content: center; font-size: 20px; font-weight: 700; color: %s; letter-spacing: 0.04em">打撃</div>'
            '<div class="abs" style="left: 14px; right: 14px; top: 60px; height: 92px; border-radius: %s; background: %s; border: 1px solid %s; display: flex; align-items: center; justify-content: center">%s</div>'
            '<div class="abs label" style="left: 0; right: 0; top: 160px; text-align: center; font-size: 10px; color: %s">物理 · コモン</div>'
            '<div class="abs" style="left: 16px; right: 16px; top: 182px; text-align: center; font-size: 13px; font-weight: 500; color: %s; display: flex; align-items: center; justify-content: center; gap: 4px">%s ダメージ 6</div>'
            '<div class="abs numeral" style="left: 14px; bottom: 12px; height: 30px; padding: 0 10px 0 8px; display: flex; align-items: center; gap: 6px; border-radius: %s; background: %s; border: 1px solid %s; color: %s; font-size: 17px">%s<span>6</span></div>'
            '</div>') % (
        radius, t['cardbg'], '2px' if hard else '1px', t['cardborder'], t['shadow'],
        t['accent'] if not hard else t['border'], 'rgba(0,0,0,0.55)' if not hard else '#efe9dc', t['accent'] if not hard else t['text'],
        t['text'] if not hard else '#15130f', '4px' if hard else ('6px' if tag == 'A' else '12px'),
        'rgba(0,0,0,0.3)' if not hard else '#e4dccb', t['cardborder'] if hard else 'rgba(255,255,255,0.06)', crest_svg('physical', 50, '#15130f' if hard else None),
        t['dim'] if not hard else 'rgba(21,19,15,0.6)', t['text'] if not hard else '#15130f', icon('sword', 14, t['bad'], 2),
        '4px' if hard else ('6px' if tag == 'A' else '10px'), rgba(t['bad'], 0.16) if not hard else '#c8321e', rgba(t['bad'], 0.55) if not hard else '#15130f', lerp(t['bad'], '#ffffff', 0.35) if not hard else '#efe9dc', icon('sword', 14, lerp(t['bad'], '#ffffff', 0.35) if not hard else '#efe9dc', 2.2))

# ---- 低精細の構造案 (据え置き) ----
def lowfi(title, motivation, tradeoff, boxes):
    s = ('<div style="position: relative; width: 960px; height: 540px; overflow: hidden; background: #f4f1ea; color: #2a2a2a; font-family: \'Zen Kurenaido\', \'Noto Sans JP\', sans-serif">')
    s += '<div class="abs" style="left: 20px; top: 12px; font-size: 24px; font-weight: 700">%s</div>' % title
    for x, y, w, h, label, kind in boxes:
        border = '2px dashed #6b6b6b' if kind == 'ui' else '2px solid #2a2a2a'
        bg = '#e8e3d8' if kind == 'field' else ('#fff7d6' if kind == 'accent' else ('#ffffff' if kind == 'card' else 'transparent'))
        s += ('<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; border: %s; background: %s; display: flex; align-items: center; justify-content: center; text-align: center; font-size: 14px; line-height: 18px; padding: 4px; box-sizing: border-box">%s</div>') % (x, y, w, h, border, bg, label)
    s += ('<div class="abs" style="left: 20px; top: 468px; width: 920px; font-size: 14px; line-height: 20px"><span style="font-weight: 700">ねらい:</span> %s<br><span style="font-weight: 700">代償:</span> %s</div>') % (motivation, tradeoff)
    return s + '</div>'

def direction_b():
    boxes = [(20, 50, 920, 400, '', 'field'), (330, 60, 120, 90, '敵A\n意図カード', 'card'), (470, 60, 120, 90, '敵B\n意図カード', 'card'), (610, 60, 120, 90, '敵C\n意図カード', 'card'),
             (330, 160, 400, 60, '敵の列（顔・HP・状態）', 'ui'), (400, 240, 260, 90, '卓の中央 = 伏せ場（大きく）\n敵の意図カードと向き合う', 'accent'),
             (40, 300, 200, 140, 'リーダーの卓席\n顔・HP・ブロック・最悪被ダメ', 'ui'), (700, 300, 220, 60, '置物の列', 'ui'), (260, 350, 420, 90, '手札は扇でなく一列（重ならない）', 'ui'), (700, 380, 220, 60, 'エナジー · ターン終了', 'ui')]
    return lowfi('案B — 卓上型（テーブル）', '伏せ札を「卓の中央に置く」物理感で set-confirm を主役にする。敵の意図もカードとして卓に出るので読み合いが一枚の卓で完結する',
                 '敵が3体以上だと意図カードが窮屈。人物の絵が小さくなり、PixelLab の立ち絵が活きにくい', boxes)

def direction_c():
    boxes = [(20, 50, 220, 400, 'サイドレール\n\nリーダー顔 / HP\nブロック / 状態\n最悪被ダメ\n\n山札 / 捨て札 / 消滅\n\n直近ログ 5行\n\nキー凡例', 'ui'),
             (260, 50, 680, 40, '予測バー: 最悪 −14 ／ 打ち消し可 2 ／ 伏せ 1/1', 'accent'), (260, 100, 680, 220, '戦場（敵は大きく・意図は頭上）', 'field'),
             (260, 330, 680, 120, '手札は一列・文字を大きく（ホバーで拡大）', 'ui'), (860, 100, 70, 40, 'ログ', 'ui')]
    return lowfi('案C — HUDレール型（情報優先）', 'テスターがいま一番使っている数字（最悪被ダメ・伏せ・打ち消し）を左のレールと予測バーに常設し、暗算を消す',
                 '画面が「道具」に寄り、ゲームらしい没入感は薄れる。1280 幅では手札が窮屈', boxes)

def write(name, body):
    with open(os.path.join(OUT, name), 'w', encoding='utf-8') as f:
        f.write(HEAD + body + TAIL)

write('Main.dc.html', battle_scene(False))
write('ReactionWindow.dc.html', battle_scene(True))
write('Styles.dc.html', styles_board())
write('CardAnatomy.dc.html', card_anatomy())
write('DirectionB.dc.html', direction_b())
write('DirectionC.dc.html', direction_c())
canvas = {
    'artboards': [
        {'file': 'Main.dc.html', 'title': '戦闘画面（自分のターン・手札ホバー）', 'x': 0, 'y': 0, 'w': 1920, 'h': 1080},
        {'file': 'ReactionWindow.dc.html', 'title': '確認ウィンドウ（発動／温存）', 'x': 2040, 'y': 0, 'w': 1920, 'h': 1080},
        {'file': 'Styles.dc.html', 'title': '見た目の方向 A / B / C', 'x': 0, 'y': 1240, 'w': 1760, 'h': 620},
        {'file': 'CardAnatomy.dc.html', 'title': 'カードの面', 'x': 1880, 'y': 1240, 'w': 1760, 'h': 800},
        {'file': 'DirectionB.dc.html', 'title': '別案B（配置の低精細）', 'x': 0, 'y': 2160, 'w': 960, 'h': 540},
        {'file': 'DirectionC.dc.html', 'title': '別案C（配置の低精細）', 'x': 1080, 'y': 2160, 'w': 960, 'h': 540},
    ],
    'annotations': [
        {'id': 'brief', 'x': 0, 'y': -200, 'w': 640, 'text': '戦闘UIの作り直し・第3版「静かな夜」。配置は第1版のまま、見た目を一新: ごつい斜面のパネル・木目と羊皮紙・太縁の札をやめ、暗いガラスの面・1pxの金の線・明朝の名前・Cinzel の数字・余白で見せる。剣＝攻撃／盾＝防御の札はそのまま（細い縁の小札に）。'},
        {'id': 'note-modal', 'x': 2040, 'y': -120, 'w': 520, 'text': 'set-confirm の山場。左に「実値・受け・HP差分」、中央に候補札、右に発動（金）／温存（線）。後続の敵の分岐が反転する警告は金の細枠。'},
        {'id': 'note-styles', 'x': 0, 'y': 1150, 'w': 640, 'text': '見た目の別案。A（本命・適用済み）／B 白の版画＝生成りの紙と黒い線／C 霧の水彩＝青緑の柔らかい面。B か C に寄せるなら、本命2枚を同じ肌で描き直す。'},
    ],
    'launch': {'view': 'canvas'},
}
with open(os.path.join(OUT, 'canvas.json'), 'w', encoding='utf-8') as f:
    json.dump(canvas, f, ensure_ascii=False, indent=2)
print('written', sorted(f for f in os.listdir(OUT) if f.endswith('.html') or f.endswith('.json')))

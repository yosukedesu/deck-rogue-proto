# build.py — 戦闘UIデザインカンバス v4「絵本」: クリーム色の紙・鉛筆の二重線・水彩のにじみ・手書き風の文字。
# 骨格 (配置) は第1版のまま。日本一ソフトウェアの絵本調の空気を、自作の意匠で。値は Theme.cs の色を土台に紙と水彩へ再構成。
import json, random, os
OUT = os.path.dirname(os.path.abspath(__file__))

INK = '#3b2f2f'; INK_SOFT = 'rgba(59,47,47,0.6)'; PAPER = '#f4ecd6'; PAPER2 = '#eadfc4'
C = dict(rose='#d97b7b', sky='#7fa7c9', moss='#8fae7b', honey='#e0b25a', plum='#a98cc4', teal='#7ab8b0', sand='#c9a982',
         physical='#c9a982', spell='#a98cc4', reaction='#7ab8b0', permanent='#e0b25a', green='#8fae7b',
         nightTop='#26294a', nightBot='#12142a', groundTop='#3b3a2c', groundBot='#1e1f18')
ROLE = dict(dmg=C['rose'], block=C['sky'], counter=C['teal'], growth=C['moss'], momentum=C['honey'], expose='#e0a04a', shatter='#b78a6a', draw='#9fa8d0', mana='#e0b25a')
ROLE_ICON = dict(dmg='sword', block='shield', counter='undo', growth='leaf', momentum='wind', expose='target', shatter='hammer', draw='deck', mana='bolt')
import urllib.parse
_NOISE_SVG = ("<svg xmlns='http://www.w3.org/2000/svg' width='240' height='240'><filter id='n'><feTurbulence type='fractalNoise' baseFrequency='0.85' numOctaves='2' stitchTiles='stitch'/>"
              "<feColorMatrix values='0 0 0 0 0.2 0 0 0 0 0.15 0 0 0 0 0.1 0 0 0 0.35 0'/></filter><rect width='100%' height='100%' filter='url(#n)'/></svg>")
NOISE = "url('data:image/svg+xml," + urllib.parse.quote(_NOISE_SVG, safe='') + "')"   # 属性の二重引用符と衝突しないよう全て百分率符号化

def lerp(a, b, t):
    a = a.lstrip('#'); b = b.lstrip('#')
    ra, ga, ba = int(a[0:2], 16), int(a[2:4], 16), int(a[4:6], 16)
    rb, gb, bb = int(b[0:2], 16), int(b[2:4], 16), int(b[4:6], 16)
    return '#%02x%02x%02x' % (round(ra + (rb - ra) * t), round(ga + (gb - ga) * t), round(ba + (bb - ba) * t))
def rgba(hexcol, a):
    h = hexcol.lstrip('#'); return 'rgba(%d,%d,%d,%s)' % (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)

def creature_svg(seed, dark, mid, light, size, eyes=True):
    rng = random.Random(seed); n = 16
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
                rects.append('<rect x="%d" y="%d" width="1" height="1" fill="%s"></rect>' % (x, y, dark if edge else (light if y < n * 0.45 else mid)))
            elif at(x - 1, y) or at(x + 1, y) or at(x, y - 1) or at(x, y + 1):
                rects.append('<rect x="%d" y="%d" width="1" height="1" fill="#2a2222"></rect>' % (x, y))
    if eyes:
        rects.append('<rect x="5" y="6" width="1" height="1" fill="#ffffff"></rect><rect x="10" y="6" width="1" height="1" fill="#ffffff"></rect>')
    return '<svg viewBox="0 0 16 16" width="%d" height="%d" shape-rendering="crispEdges" style="display:block">%s</svg>' % (size, size, ''.join(rects))

PATHS = {
    'sword': '<path d="M14 4l6 6-9 9-3 1-3-3 1-3 8-10z"></path><path d="M5 19l-2 2"></path><path d="M13 7l4 4"></path>',
    'shield': '<path d="M12 3l7 3v5c0 5-3.5 8.5-7 10-3.5-1.5-7-5-7-10V6l7-3z"></path>',
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
    'star': '<path d="M12 3l2.7 5.6 6.1.9-4.4 4.3 1 6.1L12 17l-5.4 2.9 1-6.1L3.2 9.5l6.1-.9z"></path>',
    'question': '<path d="M9 9a3 3 0 1 1 4.5 2.6c-1 .6-1.5 1.2-1.5 2.4"></path><path d="M12 17v.5"></path>',
}
def icon(name, size=20, color=INK, sw=1.8):
    return ('<svg width="%d" height="%d" viewBox="0 0 24 24" fill="none" stroke="%s" stroke-width="%s" stroke-linecap="round" stroke-linejoin="round" style="display:block;flex:none">%s</svg>' % (size, size, color, sw, PATHS[name]))

def crest_svg(typ, size=52, color=INK):
    if typ == 'physical': inner = '<path d="M8 40L40 8M8 8l32 32"></path><path d="M10 6h5v5M38 6h-5v5M6 38v-5h5M42 38v-5h-5"></path>'
    elif typ == 'spell': inner = '<path d="M24 5v38M5 24h38M10.6 10.6l26.8 26.8M37.4 10.6L10.6 37.4"></path><circle cx="24" cy="24" r="8"></circle>'
    elif typ == 'reaction': inner = '<path d="M4 24c6-9 14-13 20-13s14 4 20 13c-6 9-14 13-20 13S10 33 4 24z"></path><circle cx="24" cy="24" r="6"></circle>'
    else: inner = '<path d="M10 42h28M14 42V18M34 42V18M8 18h32M12 14l12-8 12 8"></path><path d="M21 42V28h6v14"></path>'
    return '<svg width="%d" height="%d" viewBox="0 0 48 48" fill="none" stroke="%s" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" style="display:block">%s</svg>' % (size, size, color, inner)

# 紙・水彩の部品
PAPER_SHADOW = '0 0 0 1.5px %s, 0 0 0 4px %s, 0 0 0 5.5px %s, 0 10px 22px rgba(0,0,0,0.35)' % (INK, PAPER, rgba(INK, 0.5))
def paper_style(radius='18px 22px 16px 20px / 20px 16px 22px 18px', bg=PAPER, shadow=None):
    return 'background: %s; border-radius: %s; box-shadow: %s; color: %s;' % (bg, radius, shadow or PAPER_SHADOW, INK)
def grain(extra=''):
    return '<div class="abs" style="inset: 0; border-radius: inherit; background-image: %s; opacity: 0.35; mix-blend-mode: multiply; pointer-events: none; %s"></div>' % (NOISE, extra)
def blob(color, w, h, alpha=0.85, rot=0):
    return ('<div class="abs" style="left: 0; top: 0; width: %dpx; height: %dpx; border-radius: 48%% 52%% 45%% 55%% / 55%% 45%% 55%% 45%%; background: radial-gradient(circle at 40%% 35%%, %s, %s 70%%, %s 100%%); transform: rotate(%ddeg)"></div>'
            % (w, h, rgba(lerp(color, '#ffffff', 0.25), alpha), rgba(color, alpha), rgba(lerp(color, '#000000', 0.15), alpha * 0.8), rot))
def tape(x, y, w=86, rot=-6, text=None):
    return ('<div class="abs hand" style="left: %dpx; top: %dpx; width: %dpx; height: 24px; transform: rotate(%ddeg); background: rgba(255,226,140,0.72); border-left: 1px dashed rgba(59,47,47,0.35); border-right: 1px dashed rgba(59,47,47,0.35); box-shadow: 0 1px 2px rgba(0,0,0,0.15); display: flex; align-items: center; justify-content: center; font-size: 11px; color: %s">%s</div>'
            % (x, y, w, rot, INK, text or ''))

HEAD = '''<!doctype html>
<html>
<head>
  <meta charset="utf-8">
  <script src="./support.js"></script>
</head>
<body>
<x-dc>
<helmet>
  <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Kaisei+Decol:wght@400;700&amp;family=Klee+One:wght@400;600&amp;family=Zen+Kurenaido&amp;display=swap">
  <style>
    body { margin: 0; background: #1a1c33; font-family: "Klee One", "Zen Kurenaido", "Hiragino Sans", sans-serif; color: #3b2f2f; -webkit-font-smoothing: antialiased; }
    a { color: #b8862a; } a:hover { color: #8a6218; }
    .abs { position: absolute; }
    .deco { font-family: "Kaisei Decol", "Hiragino Mincho ProN", serif; font-weight: 700; }
    .hand { font-family: "Klee One", "Zen Kurenaido", sans-serif; font-weight: 600; }
    .num { font-variant-numeric: tabular-nums; }
    .paper { background: #f4ecd6; color: #3b2f2f; border-radius: 18px 22px 16px 20px / 20px 16px 22px 18px; box-shadow: 0 0 0 1.5px #3b2f2f, 0 0 0 4px #f4ecd6, 0 0 0 5.5px rgba(59,47,47,0.5), 0 10px 22px rgba(0,0,0,0.35); }
    .tag { background: #f4ecd6; color: #3b2f2f; border-radius: 10px 14px 10px 12px / 12px 10px 14px 10px; box-shadow: 0 0 0 1.5px #3b2f2f, 0 3px 8px rgba(0,0,0,0.3); display: flex; align-items: center; gap: 6px; height: 28px; padding: 0 10px; font-size: 12px; white-space: nowrap; }
    .pbtn { display: flex; align-items: center; justify-content: center; gap: 10px; background: #f4ecd6; color: #3b2f2f; border-radius: 14px 18px 12px 16px / 16px 12px 18px 14px; box-shadow: 0 0 0 1.5px #3b2f2f, 0 4px 0 rgba(59,47,47,0.55), 0 8px 16px rgba(0,0,0,0.3); }
    .label { font-size: 11px; letter-spacing: 0.08em; color: rgba(59,47,47,0.65); }
    .light { color: #f4ecd6; }
    .keycap { display: inline-flex; align-items: center; justify-content: center; min-width: 18px; height: 18px; padding: 0 5px; border: 1.5px solid rgba(59,47,47,0.6); border-radius: 5px; color: rgba(59,47,47,0.75); font-size: 11px; background: rgba(255,255,255,0.4); }
  </style>
</helmet>
'''
TAIL = '''</x-dc>
</body>
</html>
'''

def role_glyph(kind, size=14):
    col = ROLE[kind]
    return ('<span style="position: relative; display: inline-flex; width: %dpx; height: %dpx; align-items: center; justify-content: center; flex: none">'
            '<span class="abs" style="inset: 1px; border-radius: 48%% 52%% 45%% 55%% / 55%% 45%% 55%% 45%%; background: %s"></span><span style="position: relative">%s</span></span>') % (size + 6, size + 6, rgba(col, 0.55), icon(ROLE_ICON[kind], size, INK, 2))

def line(kind, text):
    if kind is None: return '<div style="line-height: 21px">%s</div>' % text
    return '<div style="display: flex; align-items: center; justify-content: center; gap: 5px; line-height: 21px">%s<span>%s</span></div>' % (role_glyph(kind), text)

def stars(rarity):
    n = {'common': 1, 'uncommon': 2, 'rare': 3}[rarity]
    fill = {'common': 'none', 'uncommon': C['sky'], 'rare': C['honey']}[rarity]
    return '<div style="display: flex; gap: 1px">' + ''.join(icon('star', 11, INK, 1.6).replace('fill="none"', 'fill="%s"' % fill) for _ in range(n)) + '</div>'

def value_blob(kind, value, right=False):
    col = ROLE[kind]
    side = 'right: 10px' if right else 'left: 10px'
    return ('<div class="abs" style="%s; bottom: 8px; width: 60px; height: 40px">%s'
            '<div class="abs hand num" style="inset: 0; display: flex; align-items: center; justify-content: center; gap: 3px; font-size: 18px; color: %s">%s<span>%s</span></div></div>'
            % (side, blob(col, 60, 40, 0.9, -4 if not right else 5), INK, icon(ROLE_ICON[kind], 15, INK, 2.2), value))

def card(name, typ, cost, rarity, body_lines, notes=None, preview=None, playable=True, key=None, mode_lines=None, dmg=None, blk=None, counter=None, color='green', scale=1.0):
    tcol = C[typ]
    type_ja = {'physical': '物理', 'spell': '呪文', 'reaction': 'リアクション', 'permanent': '置物'}[typ]
    rar = {'common': 'コモン', 'uncommon': 'アンコモン', 'rare': 'レア'}[rarity]
    name_size = 16 if len(name) > 5 else (18 if len(name) > 4 else 20)
    body = ''.join(line(k, t) for k, t in body_lines)
    if mode_lines:
        body += '<div style="margin-top: 2px; display: flex; flex-direction: column">' + ''.join(
            '<div style="display: flex; align-items: center; justify-content: center; gap: 6px; line-height: 21px"><span style="width: 6px; height: 6px; border-radius: 50%%; background: %s; flex: none"></span>%s</div>' % (ROLE[k], line(k, t).replace('justify-content: center; ', '')) for k, t in mode_lines) + '</div>'
    if preview: body += '<div class="num" style="line-height: 22px; font-size: 13px; color: %s">%s</div>' % ('#8a5a1a', preview)
    blobs = ''
    if dmg is not None: blobs += value_blob('dmg', dmg)
    if counter is not None: blobs += value_blob('counter', counter)
    if blk is not None: blobs += value_blob('block', blk, right=True)
    center = ''
    if mode_lines and dmg is not None and blk is not None:
        center = '<div class="abs label" style="left: 0; right: 0; bottom: 18px; text-align: center; font-size: 10px">どちらか</div>'
    elif notes:
        center = tape(100 - 40, 252, 80, -4, notes)
    keycap = ('<div class="abs" style="right: 8px; top: -24px"><span class="keycap">%s</span></div>' % key) if key else ''
    dim_css = '' if playable else 'filter: saturate(0.35) brightness(0.85);'
    return ('<div class="card" style="position: relative; width: 200px; height: 290px; transform: scale(%s); transform-origin: 50%% 100%%; %s">'
            '<div class="abs paper" style="inset: 0">%s</div>'
            # タイプのしおり (左上に垂れる。コストを乗せる)
            '<div class="abs" style="left: 12px; top: -6px; width: 34px; height: 50px; background: %s; clip-path: polygon(0 0, 100%% 0, 100%% 100%%, 50%% 84%%, 0 100%%); box-shadow: inset 0 0 0 1.5px %s"></div>'
            '<div class="abs hand num" style="left: 12px; top: 2px; width: 34px; text-align: center; font-size: 19px; color: %s">%s</div>'
            # 星 (レア度)
            '<div class="abs" style="right: 12px; top: 12px">%s</div>'
            # 名前
            '<div class="abs deco" style="left: 50px; right: 30px; top: 10px; height: 34px; display: flex; align-items: center; justify-content: center; font-size: %dpx; color: %s; white-space: nowrap; letter-spacing: 0.02em">%s</div>'
            '<svg class="abs" style="left: 56px; top: 44px" width="110" height="6" viewBox="0 0 110 6" fill="none" stroke="%s" stroke-width="1.4" stroke-linecap="round"><path d="M1 4c20-3 40 1 55-1s35-2 53 1"></path></svg>'
            # 挿絵の窓 (水彩のにじみ + 墨の紋章)
            '<div class="abs" style="left: 16px; right: 16px; top: 58px; height: 92px; border-radius: 40%% 60%% 55%% 45%% / 50%% 40%% 60%% 50%%; background: radial-gradient(circle at 45%% 40%%, %s, %s 65%%, %s 100%%); display: flex; align-items: center; justify-content: center">%s</div>'
            # タイプ
            '<div class="abs label" style="left: 0; right: 0; top: 158px; text-align: center; font-size: 10px">%s ・ %s</div>'
            # 本文
            '<div class="abs hand" style="left: 16px; right: 16px; top: 178px; bottom: 52px; text-align: center; font-size: 14px; color: %s">%s</div>'
            '%s%s%s</div>') % (
        scale, dim_css, grain(), tcol, rgba(INK, 0.8), INK, cost, stars(rarity), name_size, INK, name, rgba(INK, 0.55),
        rgba(lerp(tcol, '#ffffff', 0.35), 0.9), rgba(tcol, 0.55), rgba(tcol, 0.0), crest_svg(typ, 50), type_ja, rar, INK, body, blobs, center, keycap)

def card_back(w, h):
    return ('<div style="position: relative; width: %dpx; height: %dpx; border-radius: 14px 18px 12px 16px / 16px 12px 18px 14px; background: #2b2d4d; box-shadow: 0 0 0 1.5px %s, 0 0 0 4px %s, 0 0 0 5.5px %s, 0 8px 18px rgba(0,0,0,0.4); overflow: hidden">'
            '<div class="abs" style="inset: 8px; border-radius: 10px; border: 1.5px dashed rgba(244,236,214,0.5); background-image: radial-gradient(rgba(244,236,214,0.18) 1.5px, transparent 1.6px); background-size: 14px 14px"></div>'
            '<div class="abs" style="inset: 0; display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 8px; color: #f4ecd6">%s<div class="deco" style="font-size: 14px">伏せ札</div></div></div>') % (w, h, INK, PAPER, rgba(INK, 0.5), icon('question', 32, '#f4ecd6', 1.8))

def hpbar(w, cur, mx, block=None, numeral=15):
    pct = max(0, min(100, cur / mx * 100))
    badge = ''
    if block is not None:
        badge = ('<div class="abs" style="left: -50px; top: -14px; width: 42px; height: 36px">%s<div class="abs hand num" style="inset: 0; display: flex; align-items: center; justify-content: center; gap: 2px; font-size: 15px; color: %s">%s%d</div></div>'
                 % (blob(C['sky'], 42, 36, 0.9, -8), INK, icon('shield', 13, INK, 2.2), block))
    return ('<div style="position: relative; width: %dpx; height: 14px; border-radius: 8px 10px 8px 10px; background: rgba(244,236,214,0.9); box-shadow: 0 0 0 1.5px %s">'
            '<div class="abs" style="left: 2px; top: 2px; bottom: 2px; width: calc(%.1f%% - 4px); border-radius: 6px 8px 6px 8px; background: linear-gradient(90deg, %s, %s)"></div>%s'
            '<div class="abs hand num" style="left: 0; right: 0; top: 18px; text-align: center; font-size: %dpx; color: #f4ecd6; text-shadow: 0 1px 0 rgba(0,0,0,0.6)">%d <span style="opacity: 0.6; font-size: %dpx">/ %d</span></div></div>') % (
        w, INK, pct, C['rose'], lerp(C['rose'], '#ffffff', 0.2), badge, numeral, cur, max(11, numeral - 3), mx)

def pill(icon_name, label, color):
    return '<div class="tag" style="height: 26px; padding: 0 8px 0 6px"><span style="width: 10px; height: 10px; border-radius: 50%%; background: %s; flex: none"></span>%s<span class="hand">%s</span></div>' % (color, icon(icon_name, 13, INK, 2), label)

def bubble(kind, text, sub, color, cx, y):
    w = 220
    return ('<div class="abs paper" style="left: %dpx; top: %dpx; width: %dpx; height: 54px; border-radius: 26px 30px 24px 28px / 28px 24px 30px 26px; display: flex; align-items: center; justify-content: center; gap: 10px">%s'
            '%s<span class="deco num" style="font-size: 27px; color: %s">%s</span></div>'
            '<svg class="abs" style="left: %dpx; top: %dpx" width="30" height="22" viewBox="0 0 30 22" fill="%s" stroke="%s" stroke-width="1.5"><path d="M4 0c4 6 6 12 10 20 2-8 6-14 12-20z"></path></svg>'
            '<div class="abs hand" style="left: %dpx; top: %dpx; width: %dpx; text-align: center; font-size: 12px; color: #f4ecd6; text-shadow: 0 1px 0 rgba(0,0,0,0.6); line-height: 16px">%s</div>') % (
        cx - w // 2, y, w, grain(), icon(kind, 24, color, 2), INK, text, cx - 12, y + 56, PAPER, INK, cx - w // 2 - 40, y + 82, w + 80, sub)

def background(w=1920, h=1080):
    hy = round(h * 0.66)
    return ('<div class="abs" style="left: 0; top: 0; width: %dpx; height: %dpx; background: radial-gradient(ellipse at 20%% 20%%, rgba(120,90,160,0.35), rgba(0,0,0,0) 55%%), radial-gradient(ellipse at 80%% 30%%, rgba(70,120,140,0.3), rgba(0,0,0,0) 50%%), linear-gradient(180deg, %s 0%%, %s 100%%)"></div>'
            '<div class="abs" style="left: 0; top: %dpx; width: %dpx; height: %dpx; background: radial-gradient(ellipse at 50%% 0%%, rgba(143,174,123,0.25), rgba(0,0,0,0) 60%%), linear-gradient(180deg, %s 0%%, %s 100%%)"></div>'
            '<div class="abs" style="left: 0; top: %dpx; width: %dpx; height: 3px; background: rgba(244,236,214,0.25); filter: blur(1px)"></div>'
            '<div class="abs" style="left: 0; top: 0; width: %dpx; height: %dpx; background-image: %s; opacity: 0.5; mix-blend-mode: overlay"></div>'
            '<div class="abs" style="left: 0; top: 0; width: %dpx; height: %dpx; background: radial-gradient(ellipse at 50%% 40%%, rgba(0,0,0,0) 45%%, rgba(0,0,0,0.45) 100%%)"></div>') % (
        w, hy, C['nightTop'], C['nightBot'], hy, w, h - hy, C['groundTop'], C['groundBot'], hy - 1, w, w, h, NOISE, w, h)

def sprite(svg, x, y, size, ring=None, wash=C['plum']):
    s = '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; filter: blur(6px); opacity: 0.55">%s</div>' % (x - 20, y - 6, size + 40, size + 30, blob(wash, size + 40, size + 30, 0.7, 6))
    if ring:
        s += '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: 24px; border-radius: 50%%; border: 2px dashed %s"></div>' % (x - 10, y + size - 12, size + 20, ring)
    s += '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: 24px; border-radius: 50%%; background: radial-gradient(ellipse, rgba(0,0,0,0.55), rgba(0,0,0,0) 70%%)"></div>' % (x, y + size - 12, size)
    s += '<div class="abs" style="left: %dpx; top: %dpx">%s</div>' % (x, y, svg)
    return s

LEADER = creature_svg('leader_green', '#1c3d2a', '#2e7a4a', '#7fe0a0', 220)
PROBE_A = creature_svg('enemy_probe', '#2a2438', '#5a4a7a', '#a48ad0', 200)
PROBE_B = creature_svg('enemy_probe_2', '#2a2438', '#5a4a7a', '#a48ad0', 200)

def topbar():
    relics = ['成長の種', '古根の杯', '商人の秤', '読みの眼鏡']
    rel = ''.join('<div class="tag" style="width: 34px; height: 34px; padding: 0; justify-content: center; border-radius: 50%%" title="%s">%s</div>' % (r, creature_svg('relic:' + r, '#7a5a18', '#e0b25a', '#fff0a8', 18, eyes=False)) for r in relics)
    return ('<div class="abs" style="left: 0; top: 0; width: 1920px; height: 72px; display: flex; align-items: center; padding: 0 28px; gap: 16px; box-sizing: border-box">'
            '<div class="tag" style="height: 40px; padding: 0 16px; gap: 12px; transform: rotate(-0.6deg)"><span class="label">幕 1 · 行 4 / 16</span><span class="deco" style="font-size: 19px">探り屋の二人組</span></div>'
            '<div class="tag" style="height: 32px; transform: rotate(1deg)">%s<span class="hand">ターン 3</span></div>'
            '<div style="flex: 1"></div>'
            '<div class="tag" style="height: 34px; gap: 6px">%s<span class="deco num" style="font-size: 18px">67</span><span class="label">G</span></div>'
            '<div class="tag" style="height: 34px">%s<span class="hand">デッキ 14</span></div>'
            '<div style="display: flex; gap: 6px; align-items: center">%s</div>'
            '<div class="tag pbtn" style="height: 34px; border-radius: 12px">%s<span class="hand">ログ</span></div>'
            '<div class="tag pbtn" style="width: 34px; height: 34px; padding: 0; justify-content: center; border-radius: 12px">%s</div>'
            '</div>') % (icon('clock', 14, INK), icon('coin', 18, '#b8862a', 2), icon('deck', 15, INK), rel, icon('log', 15, INK), icon('gear', 15, INK))

def nameplate(text, x, y, w, rot=0):
    return ('<div class="abs tag deco" style="left: %dpx; top: %dpx; width: %dpx; height: 34px; justify-content: center; font-size: 19px; letter-spacing: 0.06em; transform: rotate(%sdeg); box-sizing: border-box">%s</div>' % (x, y, w, rot, text))

def player_zone(hp=62, block=8, worst=14):
    s = sprite(LEADER, 150, 380, 220, wash=C['moss'])
    s += nameplate('大樹の巫女 このは', 140, 596, 240, -1)
    s += '<div class="abs" style="left: 120px; top: 646px">%s</div>' % hpbar(280, hp, 80, block=block, numeral=16)
    s += ('<div class="abs" style="left: 110px; top: 692px; display: flex; align-items: center; gap: 8px; white-space: nowrap">'
          '<div class="tag" style="height: 28px; background: %s">%s<span class="hand">最悪被ダメ</span><span class="deco num" style="font-size: 15px">−%d</span></div>'
          '<span class="hand light" style="font-size: 13px; text-shadow: 0 1px 0 rgba(0,0,0,0.6)">→ HP %d　ブロック %d を差し引き済</span></div>') % (rgba(C['rose'], 0.6), icon('warn', 13, INK, 2), worst, hp - worst, block)
    s += '<div class="abs" style="left: 110px; top: 728px; display: flex; gap: 8px">%s%s%s</div>' % (pill('leaf', '成長 +3', C['moss']), pill('wind', '勢い +2', C['honey']), pill('down', '弱体 1', C['plum']))
    s += '<div class="abs tag" style="left: 420px; top: 366px; height: 26px; transform: rotate(-2deg)">%s<span class="hand">伏せ場 1 / 1</span></div>' % icon('eyeoff', 13, INK)
    s += '<div class="abs" style="left: 420px; top: 400px; padding: 10px; border-radius: 16px; border: 2px dashed rgba(244,236,214,0.55); background: rgba(244,236,214,0.08)">%s</div>' % card_back(120, 172)
    s += '<div class="abs hand light" style="left: 410px; top: 604px; width: 160px; text-align: center; font-size: 11px; line-height: 15px; text-shadow: 0 1px 0 rgba(0,0,0,0.6)">被攻撃後に発動候補 · 回収 1E</div>'
    s += '<div class="abs tag" style="left: 604px; top: 366px; height: 26px; transform: rotate(1.5deg)"><span class="hand">置物</span></div>'
    perms = [('年輪の大樹', '毎ターン 成長+1', -1.5, False), ('大樹の根', '2ターン目から上限4', 1.2, True)]
    tiles = ''
    for nm, eff, rot, innate in perms:
        tiles += ('<div class="paper" style="position: relative; width: 148px; height: 112px; padding: 12px 12px; box-sizing: border-box; transform: rotate(%sdeg); opacity: %s; border-radius: 10px 14px 10px 12px / 12px 10px 14px 10px">%s'
                  '<div style="position: relative; display: flex; align-items: center; gap: 8px">%s<div class="deco" style="font-size: 15px; white-space: nowrap">%s</div></div>'
                  '<div class="hand" style="position: relative; margin-top: 8px; font-size: 12px; line-height: 17px; color: %s">%s</div>'
                  '<div class="abs" style="left: 50%%; top: -7px; width: 12px; height: 12px; border-radius: 50%%; background: %s; box-shadow: 0 0 0 1.5px %s; transform: translateX(-50%%)"></div></div>') % (rot, '0.8' if innate else '1', grain(), crest_svg('permanent', 24, INK), nm, INK_SOFT, eff, C['rose'] if not innate else C['moss'], INK)
    s += '<div class="abs" style="left: 600px; top: 404px; display: flex; gap: 14px">%s</div>' % tiles
    return s

def enemy(cx, svg, name, hp, mx, intent, chips, ring=None, block=None):
    kind, text, sub, color = intent
    s = bubble(kind, text, sub, color, cx, 280)
    s += sprite(svg, cx - 100, 400, 200, ring)
    s += nameplate(name, cx - 70, 600, 140, 1 if cx % 2 else -1)
    s += '<div class="abs" style="left: %dpx; top: 648px">%s</div>' % (cx - 110, hpbar(220, hp, mx, block=block))
    s += '<div class="abs" style="left: %dpx; top: 694px; width: 300px; display: flex; justify-content: center; gap: 8px">%s</div>' % (cx - 150, ''.join(chips))
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
        glow = 'filter: drop-shadow(0 0 16px rgba(255,226,140,0.55));' if hovered else ''
        s += ('<div class="abs" style="left: %dpx; bottom: %dpx; width: 200px; height: 290px; transform: rotate(%sdeg) scale(%s); transform-origin: 50%% 100%%; z-index: %d; %s">%s</div>' % (
            960 - 100 + dx, 30 - dy - lift, 0 if hovered else rot, sc, 10 if hovered else i, glow, c))
    return s

def energy_sun(cur, mx, x, y):
    size = 128; r = 50; circ = 2 * 3.14159 * r; frac = cur / mx
    return ('<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; transform: rotate(-3deg)">'
            '<div class="abs paper" style="inset: 6px; border-radius: 50%%">%s</div>'
            '<svg class="abs" style="left: 0; top: 0; transform: rotate(-90deg)" width="%d" height="%d" viewBox="0 0 128 128"><circle cx="64" cy="64" r="%d" fill="none" stroke="%s" stroke-width="7" stroke-linecap="round" stroke-dasharray="%.1f %.1f"></circle></svg>'
            '<div class="abs deco num" style="inset: 0; display: flex; align-items: baseline; justify-content: center; padding-top: 36px; font-size: 42px; color: %s">%d<span class="hand" style="font-size: 17px; opacity: 0.6; margin-left: 3px">/ %d</span></div>'
            '<div class="abs label" style="left: 0; right: 0; bottom: 22px; text-align: center; font-size: 10px">エナジー</div></div>') % (
        x, y, size, size, grain(), size, size, r, C['honey'], circ * frac, circ * (1 - frac), INK, cur, mx)

def bottom_controls(energy=3, emax=4):
    s = energy_sun(energy, emax, 92, 834)
    s += '<div class="abs hand light" style="left: 84px; top: 966px; width: 144px; text-align: center; font-size: 11px; text-shadow: 0 1px 0 rgba(0,0,0,0.6)">上限 %d · 2ターン目から</div>' % emax
    s += '<div class="abs tag" style="left: 40px; top: 1012px; height: 34px; transform: rotate(-1deg)">%s<span class="deco num" style="font-size: 17px">12</span><span class="label">山札</span></div>' % icon('deck', 15, INK)
    s += '<div class="abs tag" style="left: 1668px; top: 1012px; height: 34px; gap: 8px; transform: rotate(1deg)">%s<span class="deco num" style="font-size: 17px">3</span><span class="label">捨て札</span><span style="width: 1.5px; height: 14px; background: rgba(59,47,47,0.35)"></span><span class="deco num" style="font-size: 17px">1</span><span class="label">消滅</span></div>' % icon('undo', 15, INK)
    s += ('<div class="abs pbtn" style="left: 1650px; top: 870px; width: 230px; height: 64px; background: linear-gradient(180deg, %s, %s); transform: rotate(-1deg)"><span class="deco" style="font-size: 21px; letter-spacing: 0.1em">ターン終了</span><span class="keycap">E</span></div>'
          '<div class="abs hand light" style="left: 1650px; top: 842px; width: 230px; text-align: right; font-size: 11px; text-shadow: 0 1px 0 rgba(0,0,0,0.6)">手札 5 · 伏せ 1/1</div>') % (lerp(C['honey'], '#ffffff', 0.35), C['honey'])
    return s

def battle_scene(with_modal=False):
    s = '<div style="position: relative; width: 1920px; height: 1080px; overflow: hidden; background: #1a1c33">'
    s += background()
    s += topbar()
    s += player_zone()
    if not with_modal:
        s += enemy(1180, PROBE_A, '探り屋', 31, 38, ('sword', '5〜7', '伏せ札あり → 12〜16 か 5〜7（順番を守らない）', C['rose']), [pill('target', '急所 2', '#e0a04a'), pill('down', '筋力 −2', '#b7a89a')], ring=C['honey'])
        s += enemy(1580, PROBE_B, '探り屋', 38, 38, ('sword', '12〜16', '本気の突き（三度目）', C['rose']), [pill('down', '筋力 −2', '#b7a89a')], block=6)
        s += hand(2)
        s += bottom_controls()
    else:
        s += enemy(1180, PROBE_A, '探り屋', 31, 38, ('sword', '5〜7', '伏せ札あり → 12〜16 か 5〜7', C['rose']), [pill('target', '急所 2', '#e0a04a'), pill('down', '筋力 −2', '#b7a89a')])
        s += enemy(1580, PROBE_B, '探り屋', 38, 38, ('sword', '14', '実値 · 表示は 12〜16', C['rose']), [pill('down', '筋力 −2', '#b7a89a')], block=6, ring=C['rose'])
        s += hand(None)
        s += bottom_controls(energy=0)
        s += '<div class="abs" style="left: 0; top: 0; width: 1920px; height: 1080px; background: rgba(20,18,40,0.68); z-index: 50"></div>'
        s += reaction_modal()
    s += '</div>'
    return s

def reaction_modal():
    m = '<div class="abs paper" style="left: 470px; top: 258px; width: 980px; height: 548px; z-index: 51; transform: rotate(0.4deg); border-radius: 22px 28px 20px 26px / 26px 20px 28px 22px">%s' % grain()
    m += tape(430, -10, 120, -3)
    m += ('<div class="abs" style="left: 40px; top: 30px; right: 40px; display: flex; align-items: baseline; gap: 16px">'
          '<div class="deco" style="font-size: 28px; letter-spacing: 0.06em">リアクション</div><div class="hand" style="font-size: 15px; color: %s">発動する？ 温存する？</div>'
          '<div class="label" style="margin-left: auto">被攻撃後（解決後）の窓</div></div>'
          '<svg class="abs" style="left: 40px; top: 70px" width="220" height="8" viewBox="0 0 220 8" fill="none" stroke="%s" stroke-width="1.6" stroke-linecap="round"><path d="M1 5c40-4 80 2 110-1s70-3 108 1"></path></svg>') % (INK_SOFT, INK)
    m += ('<div class="abs" style="left: 40px; top: 96px; width: 430px; display: flex; flex-direction: column; gap: 16px">'
          '<div style="position: relative; padding: 16px 18px; border-radius: 14px 18px 12px 16px / 16px 12px 18px 14px; background: %s; box-shadow: 0 0 0 1.5px %s">'
          '<div class="label" style="display: flex; align-items: center; gap: 8px">%s探り屋（2体目）の行動</div>'
          '<div style="display: flex; align-items: baseline; gap: 12px; margin-top: 6px"><span class="deco num" style="font-size: 46px; color: %s; line-height: 1">14</span><span class="hand" style="font-size: 13px; color: %s">実値 · 表示は 12〜16</span></div>'
          '<svg width="380" height="6" viewBox="0 0 380 6" fill="none" stroke="%s" stroke-width="1.2"><path d="M1 3c60-2 120 2 190 0s130-2 188 1"></path></svg>'
          '<div class="hand" style="display: flex; align-items: center; gap: 10px; font-size: 15px; margin-top: 6px">%s<span>ブロック 8 で受け</span><span style="color: %s">→</span><span class="deco num" style="font-size: 20px; color: %s">−6</span><span style="color: %s">HP 62 → 56</span></div>'
          '</div>'
          '<div style="position: relative; padding: 14px 16px 12px; background: %s; border-radius: 8px 12px 8px 10px; box-shadow: 0 0 0 1.5px %s; transform: rotate(-0.8deg)">%s'
          '<div class="hand" style="display: flex; gap: 10px; align-items: flex-start; font-size: 13px; line-height: 19px">%s<span>発動すると後続の探り屋は「伏せなし」の分岐で確定する（5〜7 → 12〜16 の可能性）</span></div></div>'
          '<div class="hand" style="font-size: 12px; line-height: 18px; color: %s">温存すれば札は伏せたまま残り、次のターンも同じ窓が開く。回収は自ターンに 1E。</div>'
          '</div>') % (PAPER2, rgba(INK, 0.5), icon('sword', 14, C['rose'], 2.2), lerp(C['rose'], '#000000', 0.25), INK_SOFT, rgba(INK, 0.5), icon('shield', 18, INK, 2), INK_SOFT, lerp(C['rose'], '#000000', 0.25), INK_SOFT,
                     rgba(C['honey'], 0.35), rgba(INK, 0.4), tape(150, -14, 70, 4), icon('warn', 18, INK, 2), INK_SOFT)
    m += '<div class="abs label" style="left: 530px; top: 96px">発動できる伏せ札</div>'
    m += '<div class="abs" style="left: 530px; top: 126px; width: 200px; height: 290px">%s</div>' % card('茨の返し', 'reaction', 1, 'common', [('counter', '被攻撃後: 返し 10')], preview='→ 探り屋 38 → 28', counter='10')
    m += ('<div class="abs" style="left: 760px; top: 132px; width: 176px; display: flex; flex-direction: column; gap: 14px">'
          '<div class="pbtn" style="height: 58px; font-size: 19px; background: linear-gradient(180deg, %s, %s)"><span class="deco" style="letter-spacing: 0.14em">発動</span><span class="keycap">F</span></div>'
          '<div class="pbtn" style="height: 48px; font-size: 15px"><span class="hand">温存する</span><span class="keycap">H</span></div>'
          '<div class="label" style="font-size: 10px; line-height: 15px; text-align: center">発動後は捨て札へ。<br>消滅持ちは消滅置き場へ</div></div>') % (lerp(C['honey'], '#ffffff', 0.35), C['honey'])
    m += '<div class="abs hand" style="left: 40px; right: 40px; top: 488px; display: flex; align-items: center; gap: 10px; font-size: 12px; color: %s">%s<span>この後 探り屋（1体目）の行動が続く：攻撃 5〜7</span></div>' % (INK_SOFT, icon('arrow', 14, INK_SOFT))
    m += '</div>'
    return m

def card_anatomy():
    W, H = 1760, 800
    s = '<div style="position: relative; width: %dpx; height: %dpx; overflow: hidden; background: #1a1c33">' % (W, H) + background(W, H)
    s += '<div class="abs deco light" style="left: 40px; top: 26px; font-size: 28px; letter-spacing: 0.06em; text-shadow: 0 2px 0 rgba(0,0,0,0.5)">カードの面 — 絵本の一枚として</div>'
    s += ('<div class="abs hand light" style="left: 40px; top: 70px; font-size: 13px; line-height: 20px; width: 1200px; text-shadow: 0 1px 0 rgba(0,0,0,0.6)">クリーム色の紙に鉛筆の二重線。タイプは左上の「しおり」の色（コストを乗せる）、レア度は星の数。挿絵の窓は水彩のにじみに墨の紋章（1枚ずつの絵は最後）。'
          '<b>左下の剣のにじみ＝与えるダメージ、右下の盾のにじみ＝得るブロック</b>。数字だけ見れば攻めか守りか分かる。</div>')
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
        s += '<div class="abs" style="left: %dpx; top: 150px; width: 200px; height: 290px; transform: scale(%s) rotate(%sdeg); transform-origin: 0 0">%s</div>' % (x, sc, (-1.2, 0.8, -0.5, 1.4, -0.9, 0.6, -1.0)[i], c)
        s += '<div class="abs deco light" style="left: %dpx; top: 500px; width: %dpx; text-align: center; font-size: 19px; letter-spacing: 0.2em; text-shadow: 0 2px 0 rgba(0,0,0,0.5)">%s</div>' % (x, 200 * sc, role)
    s += '<div class="abs hand light" style="left: 60px; top: 566px; font-size: 12px; text-shadow: 0 1px 0 rgba(0,0,0,0.6)">本文のしるし</div>'
    leg = [('dmg', 'ダメージ'), ('block', 'ブロック'), ('counter', '返し'), ('growth', '成長'), ('momentum', '勢い'), ('expose', '急所'), ('shatter', '粉砕'), ('draw', 'ドロー'), ('mana', 'エナジー')]
    s += '<div class="abs" style="left: 60px; top: 590px; display: flex; gap: 10px; flex-wrap: wrap; width: 1640px">'
    for k, t in leg:
        s += '<div class="tag" style="height: 32px; padding: 0 12px 0 6px">%s<span class="hand">%s</span></div>' % (role_glyph(k, 14), t)
    s += '</div>'
    s += ('<div class="abs hand light" style="left: 60px; top: 650px; width: 1640px; font-size: 12px; line-height: 20px; text-shadow: 0 1px 0 rgba(0,0,0,0.6); opacity: 0.85">'
          '実装メモ: 紙の面は 9スライス1種（紙色）＋鉛筆の二重線、しおりはタイプ色の1パーツ、にじみは役割色の1パーツ（剣/盾）。剣と盾は効果から導く（dealDamage の合計→剣、gainBlock/gainIceBlock→盾、counter→戻り矢印。データにカテゴリは増やさない）。'
          '手札は 0.92 倍・ホバーで 1.0 と蜂蜜色の淡い光。名前 Kaisei Decol／本文 Klee One 14px。使えない札は彩度を落とす。紙の粒はノイズ1枚の乗算。</div>')
    s += '</div>'
    return s

def styles_board():
    W, H = 1760, 620
    s = '<div style="position: relative; width: %dpx; height: %dpx; overflow: hidden; background: #1a1c33">' % (W, H)
    s += '<div class="abs deco light" style="left: 40px; top: 26px; font-size: 28px; letter-spacing: 0.06em">日本一の手触り — 3つの肌</div>'
    s += '<div class="abs hand light" style="left: 40px; top: 68px; font-size: 12px; opacity: 0.8">A 絵本（選んだ方向・左の2枚に適用済み）／B 魔女の書／C ゴシック・コミック。同じ部品で見比べる。</div>'
    cols = [
        ('A', '絵本', 'クリーム色の紙・鉛筆の二重線・水彩のにじみ。かわいい住人と相性がよく、塔の不穏さは夜の背景に任せる', '緊張の場面で圧が弱くなりがち。紙が多いと画面が明るくなる',
         dict(bg='linear-gradient(180deg, #26294a, #12142a)', panel=PAPER, border=INK, text=INK, dim=INK_SOFT, radius='14px 18px 12px 16px / 16px 12px 18px 14px', shadow=PAPER_SHADOW, accent=C['honey'], hp=C['rose'], bad=C['rose'], block=C['sky'], numeral=INK, btnbg='linear-gradient(180deg, #f3d98f, #e0b25a)', btntext=INK, cardbg=PAPER, cardborder=INK, font='deco')),
        ('B', '魔女の書', '暗い群青と葡萄色、金の唐草の細い縁飾り、羊皮紙の窓に墨の挿絵。ゴシックなおとぎ話', '装飾が増えるので、数字の読み取りに一手間かかる。作り込みの工数も大きい',
         dict(bg='linear-gradient(180deg, #1c1630, #0d0a18)', panel='rgba(28,22,48,0.92)', border='#c9a24a', text='#efe6d2', dim='rgba(239,230,210,0.6)', radius='4px', shadow='0 0 0 1px #c9a24a, 0 0 0 4px rgba(28,22,48,1), 0 0 0 5px rgba(201,162,74,0.5), 0 12px 24px rgba(0,0,0,0.5)', accent='#c9a24a', hp='#b8324a', bad='#e06c7c', block='#6f9fd8', numeral='#efe6d2', btnbg='linear-gradient(180deg, #d9b65a, #a8802a)', btntext='#1c1630', cardbg='linear-gradient(180deg, #2a2145, #16112a)', cardborder='#c9a24a', font='deco')),
        ('C', 'ゴシック・コミック', '黒・白・赤の強いコントラスト、太い輪郭、吹き出し。ポップで悪ノリ、数字が一番読みやすい', 'ドット絵の敵と輪郭が競合しやすく、王道ファンタジーの空気からは離れる',
         dict(bg='linear-gradient(180deg, #2a2a2a, #111)', panel='#ffffff', border='#111111', text='#111111', dim='rgba(17,17,17,0.6)', radius='10px', shadow='0 0 0 3px #111111, 6px 6px 0 #111111', accent='#e02a2a', hp='#e02a2a', bad='#e02a2a', block='#2a6ae0', numeral='#111111', btnbg='#e02a2a', btntext='#ffffff', cardbg='#ffffff', cardborder='#111111', font='hand')),
    ]
    for i, (tag_, title, why, cost, t) in enumerate(cols):
        x = 40 + i * 570
        s += '<div class="abs" style="left: %dpx; top: 100px; width: 540px; height: 490px; border-radius: 12px; background: %s; border: 1px solid rgba(244,236,214,0.12); overflow: hidden">' % (x, t['bg'])
        s += '<div class="abs" style="left: 22px; top: 18px; display: flex; align-items: baseline; gap: 12px"><span class="deco num" style="font-size: 22px; color: %s">%s</span><span class="deco light" style="font-size: 22px; letter-spacing: 0.1em">%s</span></div>' % (t['accent'], tag_, title)
        s += ('<div class="abs" style="left: 22px; top: 70px; width: 236px; height: 54px; border-radius: %s; background: %s; box-shadow: %s; display: flex; align-items: center; justify-content: center; gap: 10px">%s<span class="deco num" style="font-size: 26px; color: %s">12〜16</span></div>'
              '<div class="abs deco" style="left: 22px; top: 140px; width: 236px; text-align: center; font-size: 18px; color: #f4ecd6; letter-spacing: 0.1em">探り屋</div>'
              '<div class="abs" style="left: 40px; top: 174px; width: 200px; height: 12px; border-radius: 6px; background: %s; box-shadow: 0 0 0 1.5px %s"><div style="width: 80%%; height: 100%%; border-radius: inherit; background: %s"></div></div>'
              '<div class="abs deco num" style="left: 40px; top: 192px; width: 200px; text-align: center; font-size: 14px; color: #f4ecd6">31 <span style="opacity: 0.6">/ 38</span></div>') % (
            t['radius'], t['panel'], t['shadow'], icon('sword', 24, t['bad'], 2), t['bad'] if tag_ != 'A' else INK, t['panel'] if tag_ != 'B' else 'rgba(0,0,0,0.4)', t['border'], t['hp'])
        s += ('<div class="abs" style="left: 22px; top: 232px; width: 236px; display: flex; flex-direction: column; gap: 12px">'
              '<div style="height: 50px; display: flex; align-items: center; justify-content: center; border-radius: %s; background: %s; color: %s; box-shadow: %s" class="deco">発動</div>'
              '<div style="height: 44px; display: flex; align-items: center; justify-content: center; border-radius: %s; background: %s; color: %s; box-shadow: %s" class="hand">温存する</div></div>') % (
            t['radius'], t['btnbg'], t['btntext'], t['shadow'], t['radius'], t['panel'], t['text'], t['shadow'])
        s += '<div class="abs" style="left: 300px; top: 70px">%s</div>' % skin_card(t, tag_)
        s += '<div class="abs hand light" style="left: 22px; bottom: 18px; width: 496px; font-size: 12px; line-height: 18px; opacity: 0.9"><b style="color: %s">ねらい:</b> %s<br><b style="color: %s">代償:</b> %s</div>' % (t['accent'], why, t['accent'], cost)
        s += '</div>'
    s += '</div>'
    return s

def skin_card(t, tag_):
    if tag_ == 'A':
        return card('打撃', 'physical', 1, 'common', [('dmg', 'ダメージ 6')], dmg='6')
    dark = tag_ == 'B'
    return ('<div style="position: relative; width: 200px; height: 290px">'
            '<div class="abs" style="inset: 0; border-radius: %s; background: %s; box-shadow: %s"></div>'
            '<div class="abs deco num" style="left: 12px; top: 12px; width: 34px; height: 34px; border-radius: 50%%; border: 2px solid %s; background: %s; display: flex; align-items: center; justify-content: center; font-size: 17px; color: %s">1</div>'
            '<div class="abs deco" style="left: 50px; right: 30px; top: 12px; height: 34px; display: flex; align-items: center; justify-content: center; font-size: 20px; color: %s">打撃</div>'
            '<div class="abs" style="left: 14px; right: 14px; top: 60px; height: 92px; border-radius: %s; background: %s; border: %s; display: flex; align-items: center; justify-content: center">%s</div>'
            '<div class="abs" style="left: 0; right: 0; top: 160px; text-align: center; font-size: 10px; letter-spacing: 0.08em; color: %s">物理 ・ コモン</div>'
            '<div class="abs hand" style="left: 16px; right: 16px; top: 182px; text-align: center; font-size: 14px; color: %s; display: flex; align-items: center; justify-content: center; gap: 6px">%s ダメージ 6</div>'
            '<div class="abs deco num" style="left: 12px; bottom: 12px; height: 32px; padding: 0 10px; display: flex; align-items: center; gap: 6px; border-radius: %s; background: %s; color: %s; box-shadow: %s; font-size: 17px">%s<span>6</span></div>'
            '</div>') % (
        t['radius'], t['cardbg'], t['shadow'], t['border'], 'rgba(0,0,0,0.5)' if dark else '#ffffff', t['accent'] if dark else t['text'], t['text'],
        '2px' if dark else '8px', 'rgba(239,230,210,0.9)' if dark else '#f0f0f0', ('1px solid %s' % t['border']) if dark else '3px solid #111', crest_svg('physical', 50, '#1c1630' if dark else '#111'),
        t['dim'], t['text'], icon('sword', 15, t['bad'], 2.2),
        '2px' if dark else '8px', t['bad'], '#ffffff', ('0 0 0 1px %s' % t['border']) if dark else '0 0 0 3px #111', icon('sword', 14, '#ffffff', 2.4))

def lowfi(title, motivation, tradeoff, boxes):
    s = '<div style="position: relative; width: 960px; height: 540px; overflow: hidden; background: #f4f1ea; color: #2a2a2a; font-family: \'Zen Kurenaido\', sans-serif">'
    s += '<div class="abs" style="left: 20px; top: 12px; font-size: 24px; font-weight: 700">%s</div>' % title
    for x, y, w, h, label, kind in boxes:
        border = '2px dashed #6b6b6b' if kind == 'ui' else '2px solid #2a2a2a'
        bg = '#e8e3d8' if kind == 'field' else ('#fff7d6' if kind == 'accent' else ('#ffffff' if kind == 'card' else 'transparent'))
        s += '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; border: %s; background: %s; display: flex; align-items: center; justify-content: center; text-align: center; font-size: 14px; line-height: 18px; padding: 4px; box-sizing: border-box">%s</div>' % (x, y, w, h, border, bg, label)
    s += '<div class="abs" style="left: 20px; top: 468px; width: 920px; font-size: 14px; line-height: 20px"><span style="font-weight: 700">ねらい:</span> %s<br><span style="font-weight: 700">代償:</span> %s</div>' % (motivation, tradeoff)
    return s + '</div>'

def direction_b():
    boxes = [(20, 50, 920, 400, '', 'field'), (330, 60, 120, 90, '敵A\n意図カード', 'card'), (470, 60, 120, 90, '敵B\n意図カード', 'card'), (610, 60, 120, 90, '敵C\n意図カード', 'card'),
             (330, 160, 400, 60, '敵の列（顔・HP・状態）', 'ui'), (400, 240, 260, 90, '卓の中央 = 伏せ場（大きく）\n敵の意図カードと向き合う', 'accent'),
             (40, 300, 200, 140, 'リーダーの卓席\n顔・HP・ブロック・最悪被ダメ', 'ui'), (700, 300, 220, 60, '置物の列', 'ui'), (260, 350, 420, 90, '手札は扇でなく一列（重ならない）', 'ui'), (700, 380, 220, 60, 'エナジー · ターン終了', 'ui')]
    return lowfi('案B — 卓上型（テーブル）', '伏せ札を「卓の中央に置く」物理感で set-confirm を主役にする。敵の意図もカードとして卓に出るので読み合いが一枚の卓で完結する', '敵が3体以上だと意図カードが窮屈。人物の絵が小さくなり、PixelLab の立ち絵が活きにくい', boxes)

def direction_c():
    boxes = [(20, 50, 220, 400, 'サイドレール\n\nリーダー顔 / HP\nブロック / 状態\n最悪被ダメ\n\n山札 / 捨て札 / 消滅\n\n直近ログ 5行\n\nキー凡例', 'ui'),
             (260, 50, 680, 40, '予測バー: 最悪 −14 ／ 打ち消し可 2 ／ 伏せ 1/1', 'accent'), (260, 100, 680, 220, '戦場（敵は大きく・意図は頭上）', 'field'),
             (260, 330, 680, 120, '手札は一列・文字を大きく（ホバーで拡大）', 'ui'), (860, 100, 70, 40, 'ログ', 'ui')]
    return lowfi('案C — HUDレール型（情報優先）', 'テスターがいま一番使っている数字（最悪被ダメ・伏せ・打ち消し）を左のレールと予測バーに常設し、暗算を消す', '画面が「道具」に寄り、ゲームらしい没入感は薄れる。1280 幅では手札が窮屈', boxes)

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
        {'file': 'Styles.dc.html', 'title': '日本一の手触り A / B / C', 'x': 0, 'y': 1240, 'w': 1760, 'h': 620},
        {'file': 'CardAnatomy.dc.html', 'title': 'カードの面', 'x': 1880, 'y': 1240, 'w': 1760, 'h': 800},
        {'file': 'DirectionB.dc.html', 'title': '別案B（配置の低精細）', 'x': 0, 'y': 2160, 'w': 960, 'h': 540},
        {'file': 'DirectionC.dc.html', 'title': '別案C（配置の低精細）', 'x': 1080, 'y': 2160, 'w': 960, 'h': 540},
    ],
    'annotations': [
        {'id': 'brief', 'x': 0, 'y': -200, 'w': 640, 'text': '戦闘UIの作り直し・第4版「絵本」（日本一ソフトウェアの絵本調の空気を自作の意匠で）。配置は第1版のまま、見た目を紙と水彩に: クリーム色の紙・鉛筆の二重線・水彩のにじみ・手書き風の文字（Klee One）と装飾明朝（Kaisei Decol）。剣＝攻撃／盾＝防御はにじみの札に。'},
        {'id': 'note-modal', 'x': 2040, 'y': -120, 'w': 520, 'text': 'set-confirm の山場は「開いた本の一頁」。左に実値・受け・HP差分、中央に候補札、右に発動（蜂蜜色）／温存（紙）。分岐が反転する警告はマスキングテープで貼った付箋。'},
        {'id': 'note-styles', 'x': 0, 'y': 1150, 'w': 640, 'text': '日本一の手触り3種。A 絵本（選択・適用済み）／B 魔女の書＝群青と金の唐草／C ゴシック・コミック＝黒白赤。B か C に変えるなら本命2枚を描き直す。'},
    ],
    'launch': {'view': 'canvas'},
}
with open(os.path.join(OUT, 'canvas.json'), 'w', encoding='utf-8') as f:
    json.dump(canvas, f, ensure_ascii=False, indent=2)
print('written v4')

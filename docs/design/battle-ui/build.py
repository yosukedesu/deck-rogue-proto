# build.py — 戦闘UIデザインカンバスの作業ファイル (.dc.html / canvas.json) を生成する。
# 値は unity/Assets/Game/{Theme,UiKit,CardView,BattleScreen}.cs の実値 (色・寸法) を写している。
import json, random, os
OUT = os.path.dirname(os.path.abspath(__file__))

# ---- tokens (UiKit / Theme の実値) ----
C = dict(bg='#131917', panel='#1e2622', panel2='#243230', edge='#0b0f0d', light='#3a4a44', btn='#2c3a35', btnLight='#5a7a6c',
         gold='#e0b84a', energy='#f0c33c', text='#e6ecdf', dim='#8a9a90', accent='#6abf69', hp='#c94f4f', block='#6f9fd8', bad='#e06c6c',
         cardFrame='#e9e2cf', cardInner='#1b2420', physical='#8a6a3c', spell='#6c4f9c', reaction='#3f8c86', permanent='#b08a2e', green='#5fb85a',
         skyTop='#162919', skyBot='#0a0f0b', groundTop='#1f3322', groundBot='#0d170e', horizon='#364c39')

def lerp(a, b, t):
    a = a.lstrip('#'); b = b.lstrip('#')
    ra, ga, ba = int(a[0:2], 16), int(a[2:4], 16), int(a[4:6], 16)
    rb, gb, bb = int(b[0:2], 16), int(b[2:4], 16), int(b[4:6], 16)
    return '#%02x%02x%02x' % (round(ra + (rb - ra) * t), round(ga + (gb - ga) * t), round(ba + (bb - ba) * t))

# ---- placeholder art (アプリの Creature / CardArt と同じ「idのハッシュから左右対称」の作法) ----
def creature_svg(seed, dark, mid, light, size, eyes=True, friendly=False):
    rng = random.Random(seed)
    n = 16
    mask = [[False] * n for _ in range(n)]
    for y in range(2, n - 1):
        for x in range(n // 2):
            cx = (x + 0.5) / (n / 2)
            cy = 1 - abs((y - n / 2) / (n / 2))
            mask[y][x] = rng.random() < 0.12 + 0.62 * cx * cy
    def at(x, y):
        if x < 0 or y < 0 or x >= n or y >= n: return False
        mx = x if x < n // 2 else n - 1 - x
        return mask[y][mx]
    rects = []
    for y in range(n):
        for x in range(n):
            if at(x, y):
                edge = not at(x - 1, y) or not at(x + 1, y) or not at(x, y - 1) or not at(x, y + 1)
                col = dark if edge else (light if y < n * 0.45 else mid)
                rects.append('<rect x="%d" y="%d" width="1" height="1" fill="%s"></rect>' % (x, y, col))
            elif at(x - 1, y) or at(x + 1, y) or at(x, y - 1) or at(x, y + 1):
                rects.append('<rect x="%d" y="%d" width="1" height="1" fill="%s"></rect>' % (x, y, C['edge']))
    if eyes:
        ey = 6
        rects.append('<rect x="5" y="%d" width="1" height="1" fill="#ffffff"></rect><rect x="10" y="%d" width="1" height="1" fill="#ffffff"></rect>' % (ey, ey))
    return ('<svg viewBox="0 0 16 16" width="%d" height="%d" shape-rendering="crispEdges" style="display:block">%s</svg>' % (size, size, ''.join(rects)))

def cardart_svg(seed, tint, w, h):
    rng = random.Random(seed)
    W, H = 24, 16
    dark = lerp(tint, '#000000', 0.55); light = lerp(tint, '#ffffff', 0.35); bg = lerp(dark, '#000000', 0.6)
    mask = [[False] * (W // 2) for _ in range(H)]
    for y in range(1, H - 1):
        for x in range(W // 2):
            cx = (x + 0.5) / (W / 2); cy = 1 - abs((y - H / 2) / (H / 2))
            mask[y][x] = rng.random() < 0.08 + 0.5 * cx * cy
    def at(x, y):
        if x < 0 or y < 0 or y >= H or x >= W: return False
        mx = x if x < W // 2 else W - 1 - x
        return mask[y][mx]
    rects = ['<rect x="0" y="0" width="24" height="16" fill="%s"></rect>' % bg]
    for y in range(H):
        for x in range(W):
            if at(x, y):
                edge = not at(x - 1, y) or not at(x + 1, y) or not at(x, y - 1) or not at(x, y + 1)
                col = dark if edge else (light if y > H / 2 else tint)
                rects.append('<rect x="%d" y="%d" width="1" height="1" fill="%s"></rect>' % (x, y, col))
    return '<svg viewBox="0 0 24 16" width="%d" height="%d" preserveAspectRatio="none" shape-rendering="crispEdges" style="display:block">%s</svg>' % (w, h, ''.join(rects))

# ---- icons (stroke SVG, 24 grid) ----
def icon(name, size=20, color='currentColor', sw=2):
    paths = {
        'sword': '<path d="M14 4l6 6-9 9-3 1-3-3 1-3 8-10z"></path><path d="M5 19l-2 2"></path><path d="M13 7l4 4"></path>',
        'shield': '<path d="M12 3l7 3v5c0 5-3.5 8.5-7 10-3.5-1.5-7-5-7-10V6l7-3z"></path>',
        'heart': '<path d="M12 20s-7-4.5-7-10a4 4 0 0 1 7-2.5A4 4 0 0 1 19 10c0 5.5-7 10-7 10z"></path>',
        'coin': '<circle cx="12" cy="12" r="8"></circle><path d="M9 12h6M12 9v6"></path>',
        'bolt': '<path d="M13 3L5 14h6l-1 7 8-11h-6l1-7z"></path>',
        'deck': '<rect x="5" y="4" width="11" height="15" rx="1"></rect><path d="M9 8h11v13H9"></path>',
        'skull': '<path d="M12 3a7 7 0 0 1 7 7v3l-2 2v3H7v-3l-2-2v-3a7 7 0 0 1 7-7z"></path><circle cx="9.5" cy="11" r="1.2"></circle><circle cx="14.5" cy="11" r="1.2"></circle>',
        'leaf': '<path d="M5 19C7 9 13 5 20 4c-1 8-5 14-15 15z"></path><path d="M5 19l8-8"></path>',
        'wind': '<path d="M3 8h11a3 3 0 1 0-3-3"></path><path d="M3 13h15a3 3 0 1 1-3 3"></path><path d="M3 18h7"></path>',
        'down': '<path d="M12 4v14"></path><path d="M6 12l6 6 6-6"></path>',
        'target': '<circle cx="12" cy="12" r="8"></circle><circle cx="12" cy="12" r="3"></circle><path d="M12 2v3M12 19v3M2 12h3M19 12h3"></path>',
        'eyeoff': '<path d="M3 3l18 18"></path><path d="M10 6.5A9 9 0 0 1 21 12a9 9 0 0 1-2.5 3.5"></path><path d="M6 8a9 9 0 0 0-3 4 9 9 0 0 0 11 5"></path>',
        'gear': '<circle cx="12" cy="12" r="3"></circle><path d="M12 2v3M12 19v3M2 12h3M19 12h3M4.9 4.9l2.1 2.1M17 17l2.1 2.1M4.9 19.1L7 17M17 7l2.1-2.1"></path>',
        'log': '<path d="M5 6h14M5 12h14M5 18h9"></path>',
        'key': '<circle cx="8" cy="12" r="4"></circle><path d="M12 12h9M17 12v3M20 12v2"></path>',
        'undo': '<path d="M9 14L4 9l5-5"></path><path d="M4 9h10a6 6 0 0 1 0 12h-3"></path>',
        'fire': '<path d="M12 3c1 4 5 5 5 10a5 5 0 0 1-10 0c0-3 2-4 2-4s0 3 2 3c1-3-1-5 1-9z"></path>',
        'clock': '<circle cx="12" cy="12" r="8"></circle><path d="M12 8v4l3 2"></path>',
        'arrow': '<path d="M4 12h14"></path><path d="M13 6l6 6-6 6"></path>',
        'warn': '<path d="M12 3l10 18H2L12 3z"></path><path d="M12 10v5M12 18v.5"></path>',
        'hourglass': '<path d="M6 3h12M6 21h12M8 3c0 5 8 6 8 9s-8 4-8 9M16 3c0 5-8 6-8 9s8 4 8 9"></path>',
    }
    return ('<svg width="%d" height="%d" viewBox="0 0 24 24" fill="none" stroke="%s" stroke-width="%s" stroke-linecap="round" stroke-linejoin="round" style="display:block;flex:none">%s</svg>'
            % (size, size, color, sw, paths[name]))

HEAD = '''<!doctype html>
<html>
<head>
  <meta charset="utf-8">
  <script src="./support.js"></script>
</head>
<body>
<x-dc>
<helmet>
  <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Noto+Sans+JP:wght@400;700;900&amp;family=Zen+Kurenaido&amp;display=swap">
  <style>
    body { margin: 0; background: #131917; font-family: "Noto Sans JP", "Hiragino Sans", "Yu Gothic", system-ui, sans-serif; color: #e6ecdf; }
    a { color: #e0b84a; } a:hover { color: #f0d58a; }
    .abs { position: absolute; }
    .panel { background: #1e2622; border: 3px solid #0b0f0d; box-shadow: inset 0 3px 0 #3a4a44, inset 3px 0 0 #3a4a44; }
    .btn { background: #2c3a35; border: 3px solid #0b0f0d; box-shadow: inset 0 3px 0 #5a7a6c; color: #e6ecdf; font-weight: 700; display: flex; align-items: center; justify-content: center; gap: 10px; }
    .btn-gold { background: #e0b84a; color: #1a1208; box-shadow: inset 0 3px 0 #fff0a8; }
    .chip { display: flex; align-items: center; gap: 6px; height: 30px; padding: 0 10px 0 8px; background: rgba(0,0,0,0.55); border: 2px solid #0b0f0d; font-size: 14px; font-weight: 700; }
    .keycap { display: inline-flex; align-items: center; justify-content: center; min-width: 20px; height: 20px; padding: 0 5px; border: 2px solid #0b0f0d; background: #243230; box-shadow: inset 0 2px 0 #3a4a44; color: #8a9a90; font-size: 12px; font-weight: 700; }
    .num { font-variant-numeric: tabular-nums; }
  </style>
</helmet>
'''
TAIL = '''</x-dc>
</body>
</html>
'''

# ---- card (200×290; CardView.cs の配置を写す) ----
def card(name, typ, cost, rarity, body_lines, notes=None, preview=None, playable=True, key=None, mode_lines=None, scale=1.0, faceup=True):
    tint = C[typ]
    frame = tint if playable else lerp(tint, '#000000', 0.5)
    inner = C['cardInner'] if playable else '#141917'
    txt = C['text'] if playable else C['dim']
    type_ja = {'physical': '物理', 'spell': '呪文', 'reaction': 'リアクション', 'permanent': '置物'}[typ]
    rar = {'common': 'C', 'uncommon': 'U', 'rare': 'R'}[rarity]
    gem = {'common': '#9fb0a6', 'uncommon': '#6f9fd8', 'rare': '#f0c33c'}[rarity]
    name_size = 18 if len(name) > 6 else (20 if len(name) > 4 else 21)
    body = ''.join('<div style="line-height: 22px">%s</div>' % l for l in body_lines)
    if mode_lines:
        body += '<div style="margin-top: 6px; display: flex; flex-direction: column; gap: 4px">' + ''.join(
            '<div style="display: flex; align-items: center; gap: 6px; justify-content: center"><span style="width: 8px; height: 8px; background: %s; transform: rotate(45deg); flex: none"></span><span>%s</span></div>' % (C['gold'], m) for m in mode_lines) + '</div>'
    pv = ('<div class="abs num" style="left: 18px; right: 18px; bottom: 36px; text-align: left; font-size: 14px; font-weight: 700; color: %s">%s</div>' % (C['energy'], preview)) if preview else ''
    nt = ('<div class="abs" style="left: 16px; right: 16px; bottom: 14px; text-align: center; font-size: 12px; color: %s">%s</div>' % (C['energy'], notes)) if notes else ''
    keycap = ('<div class="abs" style="right: -6px; top: -10px"><span class="keycap">%s</span></div>' % key) if key else ''
    return ('<div class="card" style="position: relative; width: 200px; height: 290px; transform: scale(%s); transform-origin: 50%% 100%%">'
            '<div class="abs" style="inset: 0; background: %s; border: 3px solid %s; box-shadow: inset 0 0 0 3px %s"></div>'
            '<div class="abs" style="inset: 12px; background: %s"></div>'
            '<div class="abs" style="left: 16px; right: 16px; top: 52px; height: 104px; overflow: hidden; border-bottom: 3px solid %s">%s</div>'
            '<div class="abs" style="left: 44px; right: 40px; top: 12px; height: 36px; display: flex; align-items: center; justify-content: center; font-size: %dpx; font-weight: 700; color: %s; white-space: nowrap; overflow: hidden; text-overflow: ellipsis">%s</div>'
            '<div class="abs" style="left: -14px; top: -6px; width: 64px; height: 64px; border-radius: 50%%; background: radial-gradient(circle at 40%% 35%%, #fff0a8 0%%, %s 45%%, #9a7a1c 100%%); border: 3px solid #1a1208; display: flex; align-items: center; justify-content: center; font-size: 28px; font-weight: 900; color: #fff; text-shadow: 0 0 3px #000, 0 2px 0 #000">%s</div>'
            '<div class="abs" style="right: 16px; top: 20px; width: 24px; height: 24px; background: %s; transform: rotate(45deg) scale(0.7); border: 3px solid #0b0f0d"></div>'
            '<div class="abs" style="left: 14px; right: 14px; top: 160px; height: 18px; text-align: center; font-size: 12px; color: %s">%s  ·  %s</div>'
            '<div class="abs" style="left: 18px; right: 18px; top: 182px; bottom: 36px; text-align: center; font-size: 15px; color: %s">%s</div>'
            '%s%s%s</div>') % (
        scale, lerp(frame, C['cardFrame'], 0.35), frame, frame, inner, C['edge'], cardart_svg(name, tint, 168, 104), name_size, txt, name,
        C['energy'], cost, gem, lerp(tint, '#ffffff', 0.55), type_ja, rar, txt, body, pv, nt, keycap)

def card_back(w, h, label='伏せ札'):
    return ('<div style="position: relative; width: %dpx; height: %dpx; background: #17332f; border: 3px solid %s; box-shadow: inset 0 0 0 3px #0b0f0d; overflow: hidden">'
            '<div class="abs" style="inset: 10px; background-image: repeating-linear-gradient(45deg, rgba(255,255,255,0.06) 0 6px, transparent 6px 14px), repeating-linear-gradient(-45deg, rgba(255,255,255,0.06) 0 6px, transparent 6px 14px); border: 2px solid rgba(255,255,255,0.12)"></div>'
            '<div class="abs" style="inset: 0; display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 8px; color: #9fd8d0">%s<div style="font-size: 13px; font-weight: 700">%s</div></div></div>') % (w, h, C['reaction'], icon('eyeoff', 28, '#9fd8d0'), label)

def hpbar(w, h, cur, mx, color=C['hp'], block=None, font=15):
    pct = max(0, min(100, cur / mx * 100))
    badge = ''
    if block is not None:
        badge = ('<div class="abs" style="left: -22px; top: -12px; width: 44px; height: 44px; display: flex; align-items: center; justify-content: center">%s'
                 '<div class="abs num" style="inset: 0; display: flex; align-items: center; justify-content: center; padding-top: 1px; font-size: 17px; font-weight: 900; color: #fff; text-shadow: 0 0 3px #000">%d</div></div>'
                 % (icon('shield', 44, C['block'], 2.2).replace('fill="none"', 'fill="#2c4a6e"'), block))
    return ('<div style="position: relative; width: %dpx; height: %dpx; background: #0b0f0d; border: 3px solid #0b0f0d; box-shadow: inset 0 0 0 2px #2a1414">'
            '<div class="abs" style="left: 0; top: 0; bottom: 0; width: %.1f%%; background: linear-gradient(180deg, %s, %s)"></div>'
            '<div class="abs num" style="inset: 0; display: flex; align-items: center; justify-content: center; font-size: %dpx; font-weight: 700; color: #fff; text-shadow: 0 1px 0 #000, 0 0 3px #000">%d / %d</div>%s</div>') % (
        w, h, pct, lerp(color, '#ffffff', 0.18), lerp(color, '#000000', 0.25), font, cur, mx, badge)

def chip(icon_name, label, color):
    return '<div class="chip" style="color: %s">%s<span class="num">%s</span></div>' % (color, icon(icon_name, 18, color), label)

def intent_bubble(kind, text, sub, color, x, y, w=232):
    return ('<div class="abs panel" style="left: %dpx; top: %dpx; width: %dpx; height: 62px; display: flex; align-items: center; justify-content: center; gap: 10px; background: rgba(20,26,24,0.92)">'
            '%s<span class="num" style="font-size: 28px; font-weight: 900; color: %s; text-shadow: 0 0 6px rgba(0,0,0,0.8)">%s</span></div>'
            '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; text-align: center; font-size: 13px; color: %s; line-height: 18px">%s</div>'
            '<div class="abs" style="left: %dpx; top: %dpx; width: 0; height: 0; border-left: 10px solid transparent; border-right: 10px solid transparent; border-top: 12px solid #0b0f0d"></div>') % (
        x, y, w, icon(kind, 40, color, 2.2), color, text, x - 30, y + 70, w + 60, C['energy'], sub, x + w // 2 - 10, y + 62)

def background(w=1920, h=1080, act_sky=(C['skyTop'], C['skyBot']), ground=(C['groundTop'], C['groundBot']), horizon=C['horizon']):
    hy = round(h * 0.66)
    return ('<div class="abs" style="left: 0; top: 0; width: %dpx; height: %dpx; background: linear-gradient(180deg, %s 0%%, %s 100%%)"></div>'
            '<div class="abs" style="left: 0; top: %dpx; width: %dpx; height: %dpx; background: linear-gradient(180deg, %s 0%%, %s 100%%)"></div>'
            '<div class="abs" style="left: 0; top: %dpx; width: %dpx; height: 6px; background: %s"></div>'
            '<div class="abs" style="left: 0; top: 0; width: %dpx; height: %dpx; background: radial-gradient(ellipse at 50%% 45%%, rgba(0,0,0,0) 45%%, rgba(0,0,0,0.55) 100%%)"></div>') % (
        w, hy, act_sky[0], act_sky[1], hy, w, h - hy, ground[0], ground[1], hy - 3, w, horizon, w, h)

def sprite_with_shadow(svg, x, y, size, ring=None):
    s = ''
    if ring:
        s += '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: 26px; border-radius: 50%%; border: 4px solid %s; box-shadow: 0 0 18px %s"></div>' % (x - 8, y + size - 14, size + 16, ring, ring)
    s += '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: 30px; border-radius: 50%%; background: radial-gradient(ellipse, rgba(0,0,0,0.6), rgba(0,0,0,0) 70%%)"></div>' % (x, y + size - 16, size)
    s += '<div class="abs" style="left: %dpx; top: %dpx">%s</div>' % (x, y, svg)
    return s

LEADER = creature_svg('leader_green', '#1c3d2a', '#2e7a4a', '#7fe0a0', 220)
PROBE_A = creature_svg('enemy_probe', '#2a2438', '#5a4a7a', '#a48ad0', 200)
PROBE_B = creature_svg('enemy_probe_2', '#2a2438', '#5a4a7a', '#a48ad0', 200)

def topbar(dimmed=False):
    relics = ['成長の種', '古根の杯', '商人の秤', '読みの眼鏡']
    rel = ''.join('<div class="panel" style="width: 44px; height: 44px; display: flex; align-items: center; justify-content: center" title="%s">%s</div>' % (r, creature_svg('relic:' + r, '#7a5a18', '#e0b84a', '#fff0a8', 26, eyes=False)) for r in relics)
    return ('<div class="abs" style="left: 0; top: 0; width: 1920px; height: 64px; background: rgba(0,0,0,0.58); border-bottom: 3px solid #0b0f0d; display: flex; align-items: center; padding: 0 24px; gap: 28px; box-sizing: border-box">'
            '<div style="display: flex; align-items: center; gap: 14px; font-size: 19px; font-weight: 700"><span>幕1</span><span style="color: #8a9a90">行 4 / 16</span><span style="color: #8a9a90">·</span><span>探り屋の二人組</span></div>'
            '<div class="chip" style="color: #e6ecdf; height: 34px; font-size: 16px">%s<span>ターン 3</span></div>'
            '<div style="flex: 1"></div>'
            '<div class="chip" style="color: %s; height: 36px; font-size: 20px">%s<span class="num">67 G</span></div>'
            '<div class="btn" style="height: 40px; padding: 0 16px; font-size: 16px">%s<span>デッキ 14</span></div>'
            '<div style="display: flex; gap: 6px; align-items: center">%s</div>'
            '<div class="btn" style="height: 40px; padding: 0 14px; font-size: 15px">%s<span>ログ</span></div>'
            '<div class="btn" style="width: 40px; height: 40px">%s</div>'
            '</div>') % (icon('clock', 18, C['dim']), C['gold'], icon('coin', 22, C['gold']), icon('deck', 20, C['text']), rel, icon('log', 18, C['text']), icon('gear', 20, C['text']))

def player_zone(hp=62, block=8, worst=14):
    s = sprite_with_shadow(LEADER, 150, 380, 220)
    s += '<div class="abs" style="left: 100px; top: 598px; width: 320px; text-align: center; font-size: 20px; font-weight: 700">大樹の巫女 このは</div>'
    s += '<div class="abs" style="left: 110px; top: 634px">%s</div>' % hpbar(300, 24, hp, 80, block=block)
    s += ('<div class="abs" style="left: 110px; top: 668px; width: 460px; display: flex; align-items: center; gap: 8px; font-size: 15px; font-weight: 700; color: %s; white-space: nowrap">%s<span>最悪被ダメ −%d</span><span style="color: %s; font-weight: 400">→ HP %d</span>'
          '<span style="font-size: 12px; font-weight: 400; color: %s; margin-left: 6px">（ブロック %d を差し引き済）</span></div>') % (C['bad'], icon('warn', 16, C['bad']), worst, C['text'], hp - worst, C['dim'], block)
    s += ('<div class="abs" style="left: 110px; top: 700px; display: flex; gap: 8px">%s%s%s</div>' % (
        chip('leaf', '成長 +3', C['accent']), chip('wind', '勢い +2', C['gold']), chip('down', '弱体 1', '#a48ad0')))
    # 伏せ場
    s += '<div class="abs" style="left: 420px; top: 372px; font-size: 14px; font-weight: 700; color: %s; display: flex; align-items: center; gap: 6px">%s伏せ場 <span class="num">1 / 1</span></div>' % (C['dim'], icon('eyeoff', 16, C['dim']))
    s += '<div class="abs" style="left: 420px; top: 398px; padding: 6px; border: 3px dashed rgba(159,216,208,0.45); background: rgba(0,0,0,0.25)">%s</div>' % card_back(120, 172)
    s += '<div class="abs" style="left: 420px; top: 596px; width: 136px; text-align: center; font-size: 12px; color: %s; line-height: 16px">被攻撃後に発動候補<br>回収 1E</div>' % C['dim']
    # 置物
    s += '<div class="abs" style="left: 600px; top: 372px; font-size: 14px; font-weight: 700; color: %s">置物</div>' % C['dim']
    perms = [('年輪の大樹', '毎T 成長+1', C['permanent'], False), ('大樹の根', '2ターン目から上限4', C['green'], True)]
    tiles = ''
    for nm, eff, col, innate in perms:
        tiles += ('<div class="panel" style="width: 150px; height: 128px; padding: 8px; box-sizing: border-box; display: flex; flex-direction: column; gap: 6px; opacity: %s">'
                  '<div style="height: 52px; border: 2px solid %s; overflow: hidden">%s</div>'
                  '<div style="font-size: 14px; font-weight: 700; white-space: nowrap">%s</div><div style="font-size: 12px; color: %s; line-height: 15px">%s</div></div>') % (
            '0.75' if innate else '1', C['edge'], cardart_svg(nm, col, 128, 52), nm, C['dim'], eff)
    s += '<div class="abs" style="left: 600px; top: 398px; display: flex; gap: 10px">%s</div>' % tiles
    return s

def enemy(x, svg, name, hp, mx, intent, chips, ring=None, block=None, dim=False):
    kind, text, sub, color = intent
    s = intent_bubble(kind, text, sub, color, x - 16, 286)
    s += sprite_with_shadow(svg, x, 400, 200, ring)
    s += '<div class="abs" style="left: %dpx; top: 608px; width: 300px; text-align: center; font-size: 20px; font-weight: 700">%s</div>' % (x - 50, name)
    s += '<div class="abs" style="left: %dpx; top: 644px">%s</div>' % (x - 20, hpbar(240, 22, hp, mx, block=block))
    s += '<div class="abs" style="left: %dpx; top: 680px; width: 300px; display: flex; justify-content: center; gap: 8px">%s</div>' % (x - 50, ''.join(chips))
    return s

def hand(hover_index=2):
    cards = [
        card('打撃', 'physical', 1, 'common', ['ダメージ 6'], preview='→ 探り屋 に 6', key='1'),
        card('蔦の楔', 'physical', 1, 'common', ['粉砕（敵ブロック全壊）', 'ダメージ 5'], preview='→ 探り屋 に 5', key='2'),
        card('絡み蔦', 'physical', 1, 'common', ['どちらか1つ'], mode_lines=['ブロック 7', 'ダメージ 7'], key='3'),
        card('防御', 'physical', 1, 'common', ['ブロック 5'], key='4'),
        card('茨の返し', 'reaction', 1, 'common', ['被攻撃後: 返し 10'], notes='伏せる: 1E · 右クリック', key='5'),
    ]
    offs = [(-392, 34, -9), (-196, 10, -4.5), (0, 0, 0), (196, 10, 4.5), (392, 34, 9)]
    s = ''
    for i, (c, (dx, dy, rot)) in enumerate(zip(cards, offs)):
        hovered = i == hover_index
        sc = 0.92 if not hovered else 1.0
        lift = 0 if not hovered else -70
        glow = 'filter: drop-shadow(0 0 14px rgba(240,195,60,0.55));' if hovered else ''
        z = 10 if hovered else i
        s += ('<div class="abs" style="left: %dpx; bottom: %dpx; width: 200px; height: 290px; transform: rotate(%sdeg) scale(%s); transform-origin: 50%% 100%%; z-index: %d; %s">%s</div>' % (
            960 - 100 + dx, 30 - dy - lift, 0 if hovered else rot, sc, z, glow, c))
    return s

def bottom_controls(energy=3, emax=4):
    s = ('<div class="abs" style="left: 96px; top: 830px; width: 136px; height: 136px; border-radius: 50%%; background: radial-gradient(circle at 40%% 35%%, #fff0a8 0%%, %s 40%%, #9a7a1c 100%%); border: 4px solid #1a1208; box-shadow: 0 0 28px rgba(240,195,60,0.35); display: flex; align-items: center; justify-content: center; font-size: 44px; font-weight: 900; color: #fff; text-shadow: 0 0 4px #000, 0 3px 0 #000" class="num">%d/%d</div>'
         '<div class="abs" style="left: 96px; top: 972px; width: 136px; text-align: center; font-size: 13px; color: %s">エナジー · 上限 %d</div>') % (C['energy'], energy, emax, C['dim'], emax)
    s += ('<div class="abs panel" style="left: 40px; top: 1000px; width: 132px; height: 62px; display: flex; align-items: center; gap: 10px; padding: 0 12px; box-sizing: border-box">%s<span class="num" style="font-size: 26px; font-weight: 900">12</span><span style="font-size: 12px; color: %s; margin-left: auto">山札</span></div>') % (icon('deck', 24, C['text']), C['dim'])
    s += ('<div class="abs panel" style="left: 1690px; top: 1000px; width: 190px; height: 62px; display: flex; align-items: center; gap: 10px; padding: 0 12px; box-sizing: border-box">%s<span class="num" style="font-size: 26px; font-weight: 900">3</span><span style="font-size: 12px; color: %s">捨て札</span><span class="num" style="font-size: 20px; font-weight: 700; color: %s; margin-left: auto">1</span><span style="font-size: 12px; color: %s">消滅</span></div>') % (icon('undo', 24, C['text']), C['dim'], C['dim'], C['dim'])
    s += ('<div class="abs btn" style="left: 1650px; top: 870px; width: 230px; height: 72px; font-size: 22px">ターン終了 <span class="keycap" style="font-size: 13px">E</span></div>'
          '<div class="abs" style="left: 1650px; top: 838px; width: 230px; text-align: right; font-size: 13px; color: %s">手札 5 · 伏せ 1/1</div>') % C['dim']
    return s

def battle_scene(with_modal=False):
    s = '<div style="position: relative; width: 1920px; height: 1080px; overflow: hidden; background: #131917">'
    s += background()
    s += topbar()
    if not with_modal:
        s += player_zone()
        s += enemy(1080, PROBE_A, '探り屋', 31, 38, ('sword', '5〜7', '伏せ札あり → 12〜16 か 5〜7（順番を守らない）', C['bad']),
                   [chip('target', '急所 2', C['energy']), chip('down', '筋力 −2', C['dim'])], ring=C['gold'])
        s += enemy(1480, PROBE_B, '探り屋', 38, 38, ('sword', '12〜16', '本気の突き（三度目）', C['bad']),
                   [chip('down', '筋力 −2', C['dim'])], block=6)
        s += hand(2)
        s += bottom_controls()
    else:
        s += player_zone(hp=62, block=8, worst=14)
        s += enemy(1080, PROBE_A, '探り屋', 31, 38, ('sword', '5〜7', '伏せ札あり → 12〜16 か 5〜7', C['bad']),
                   [chip('target', '急所 2', C['energy']), chip('down', '筋力 −2', C['dim'])])
        s += enemy(1480, PROBE_B, '探り屋', 38, 38, ('sword', '14', '実値 · 表示は 12〜16', C['bad']),
                   [chip('down', '筋力 −2', C['dim'])], block=6, ring=C['bad'])
        s += hand(None)
        s += bottom_controls(energy=0)
        s += '<div class="abs" style="left: 0; top: 0; width: 1920px; height: 1080px; background: rgba(0,0,0,0.62); z-index: 50"></div>'
        s += reaction_modal()
    s += '</div>'
    return s

def reaction_modal():
    m = '<div class="abs panel" style="left: 460px; top: 260px; width: 1000px; height: 540px; z-index: 51; background: #1e2622; box-shadow: inset 0 3px 0 #3a4a44, inset 3px 0 0 #3a4a44, 0 30px 60px rgba(0,0,0,0.6)">'
    m += ('<div class="abs" style="left: 32px; top: 24px; right: 32px; display: flex; align-items: baseline; gap: 16px"><div style="font-size: 28px; font-weight: 900; color: %s">リアクション — 発動する？ 温存する？</div>'
          '<div style="font-size: 14px; color: %s; margin-left: auto">被攻撃後（解決後）の窓</div></div>') % (C['accent'], C['dim'])
    # 行動の要約
    m += ('<div class="abs" style="left: 32px; top: 84px; width: 440px; display: flex; flex-direction: column; gap: 14px">'
          '<div class="panel" style="padding: 14px 16px; background: rgba(0,0,0,0.35); display: flex; flex-direction: column; gap: 10px">'
          '<div style="display: flex; align-items: center; gap: 10px; font-size: 16px; color: %s">%s<span>探り屋（2体目）の行動</span></div>'
          '<div style="display: flex; align-items: center; gap: 12px">%s<span class="num" style="font-size: 34px; font-weight: 900; color: %s">14</span><span style="font-size: 14px; color: %s">実値（表示は 12〜16）</span></div>'
          '<div style="display: flex; align-items: center; gap: 10px; font-size: 16px">%s<span>ブロック 8 で受け</span><span style="color: %s">→</span><span class="num" style="font-weight: 900; color: %s">HP −6</span><span class="num" style="color: %s">62 → 56</span></div>'
          '</div>'
          '<div class="panel" style="padding: 12px 16px; background: rgba(224,184,74,0.10); border-color: #4a3a10; display: flex; gap: 10px; align-items: flex-start; font-size: 14px; line-height: 20px; color: %s">%s<span>発動すると後続の 探り屋 は「伏せなし」の分岐で確定する（5〜7 → 12〜16 になる可能性）</span></div>'
          '<div style="font-size: 13px; color: %s; line-height: 19px">温存すれば札は伏せたまま残る。次のターンも同じ窓が開く。<br>回収は自ターンに 1E。</div>'
          '</div>') % (C['dim'], icon('sword', 20, C['bad']), icon('sword', 36, C['bad'], 2.2), C['bad'], C['dim'], icon('shield', 22, C['block']), C['dim'], C['bad'], C['dim'], C['gold'], icon('warn', 20, C['gold']), C['dim'])
    # 候補札
    m += '<div class="abs" style="left: 520px; top: 84px; font-size: 14px; font-weight: 700; color: %s">発動できる伏せ札</div>' % C['dim']
    m += '<div class="abs" style="left: 520px; top: 110px; width: 200px; height: 290px">%s</div>' % card('茨の返し', 'reaction', 1, 'common', ['被攻撃後: 返し 10'], preview='→ 探り屋 38 → 28')
    m += ('<div class="abs" style="left: 748px; top: 118px; width: 220px; display: flex; flex-direction: column; gap: 12px">'
          '<div class="btn btn-gold" style="height: 64px; font-size: 22px">発動 <span class="keycap" style="color: #1a1208; background: #fff0a8">F</span></div>'
          '<div class="btn" style="height: 56px; font-size: 18px">温存する <span class="keycap">H</span></div>'
          '<div style="font-size: 12px; color: %s; line-height: 17px; text-align: center">発動後は捨て札へ。<br>消滅持ちは消滅置き場へ</div>'
          '</div>') % C['dim']
    m += ('<div class="abs" style="left: 32px; right: 32px; top: 476px; display: flex; align-items: center; gap: 10px; font-size: 13px; color: %s">%s<span>この後 探り屋（1体目）の行動が続く：攻撃 5〜7</span></div>') % (C['dim'], icon('arrow', 16, C['dim']))
    m += '</div>'
    return m

def card_anatomy():
    s = '<div style="position: relative; width: 1200px; height: 720px; overflow: hidden; background: #131917">'
    s += '<div class="abs" style="left: 40px; top: 28px; font-size: 26px; font-weight: 900">カードの面 — 200×290（手札は 0.92 倍・ホバーで 1.0）</div>'
    s += '<div class="abs" style="left: 40px; top: 66px; font-size: 14px; color: %s">4タイプの枠色は Theme.CardTypeColor の実値。紋章の窓は PixelLab の絵（Art/cards/&lt;id&gt;.png）で差し替わる</div>' % C['dim']
    cards = [
        card('打撃', 'physical', 1, 'common', ['ダメージ 6'], preview='→ 探り屋 に 6'),
        card('疾風の号砲', 'spell', 1, 'uncommon', ['勢い +3', '勢い 2倍'], notes='消滅'),
        card('茨の返し', 'reaction', 1, 'common', ['被攻撃後: 返し 10'], notes='伏せる: 1E'),
        card('年輪の大樹', 'permanent', 2, 'rare', ['ブロック 5', '毎T開始時: 成長 +1']),
    ]
    for i, c in enumerate(cards):
        s += '<div class="abs" style="left: %dpx; top: 130px; width: 200px; height: 290px; transform: scale(1.3); transform-origin: 0 0">%s</div>' % (60 + i * 290, c)
    labels = [(1, 'コスト玉（左上・28px 900）。割引中は緑の数字'), (2, '名前（21px／8文字以上は 18px）'), (3, 'レア度の宝石（C 灰・U 青・R 金）'),
              (4, '紋章の窓 168×104（PixelLab 差し替え枠）'), (5, 'タイプ帯「物理 · C」'), (6, '本文 15px。選択式は ◆ の箇条書き'),
              (7, '予測行（対象が決まると実値。成長・勢い・弱体・急所・装甲を通した数）'), (8, '注記（消滅・保持・追加コスト）')]
    s += '<div class="abs" style="left: 60px; top: 540px; display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 10px 40px; width: 1080px">'
    for n, t in labels:
        s += ('<div style="display: flex; align-items: center; gap: 10px; font-size: 14px"><span class="num" style="width: 24px; height: 24px; border-radius: 50%%; background: %s; color: #1a1208; font-weight: 900; display: inline-flex; align-items: center; justify-content: center; flex: none">%d</span><span>%s</span></div>') % (C['gold'], n, t)
    s += '</div>'
    # 番号の打ち込み (1枚目の上)
    pins = [(1, 46, 118), (2, 200, 124), (3, 300, 134), (4, 170, 250), (5, 170, 336), (6, 170, 372), (7, 110, 438), (8, 170, 484)]
    for n, x, y in pins:
        s += '<div class="abs num" style="left: %dpx; top: %dpx; width: 26px; height: 26px; border-radius: 50%%; background: %s; color: #1a1208; font-weight: 900; font-size: 14px; display: flex; align-items: center; justify-content: center; border: 2px solid #1a1208; box-shadow: 0 0 0 2px %s">%d</div>' % (x, y, C['gold'], C['gold'], n)
    s += '</div>'
    return s

def lowfi(title, motivation, tradeoff, boxes, notes):
    # boxes: (x, y, w, h, label, kind) kind: 'field'|'ui'|'card'|'accent'
    s = ('<div style="position: relative; width: 960px; height: 540px; overflow: hidden; background: #f4f1ea; color: #2a2a2a; font-family: \'Zen Kurenaido\', \'Noto Sans JP\', sans-serif">')
    s += '<div class="abs" style="left: 20px; top: 12px; font-size: 24px; font-weight: 700">%s</div>' % title
    for x, y, w, h, label, kind in boxes:
        border = '2px dashed #6b6b6b' if kind == 'ui' else '2px solid #2a2a2a'
        bg = '#e8e3d8' if kind == 'field' else ('#fff7d6' if kind == 'accent' else ('#ffffff' if kind == 'card' else 'transparent'))
        s += ('<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; border: %s; background: %s; display: flex; align-items: center; justify-content: center; text-align: center; font-size: 14px; line-height: 18px; padding: 4px; box-sizing: border-box">%s</div>') % (x, y, w, h, border, bg, label)
    s += ('<div class="abs" style="left: 20px; top: 468px; width: 920px; font-size: 14px; line-height: 20px"><span style="font-weight: 700">ねらい:</span> %s<br><span style="font-weight: 700">代償:</span> %s</div>') % (motivation, tradeoff)
    return s + '</div>'

def direction_b():
    boxes = [
        (20, 50, 920, 400, '', 'field'),
        (330, 60, 120, 90, '敵A\n意図カード', 'card'), (470, 60, 120, 90, '敵B\n意図カード', 'card'), (610, 60, 120, 90, '敵C\n意図カード', 'card'),
        (330, 160, 400, 60, '敵の列（顔・HP・状態）', 'ui'),
        (400, 240, 260, 90, '卓の中央 = 伏せ場（大きく）\n敵の意図カードと向き合う', 'accent'),
        (40, 300, 200, 140, 'リーダーの卓席\n顔・HP・ブロック・最悪被ダメ', 'ui'),
        (700, 300, 220, 60, '置物の列', 'ui'),
        (260, 350, 420, 90, '手札は扇でなく一列（重ならない）', 'ui'),
        (700, 380, 220, 60, 'エナジー · ターン終了', 'ui'),
    ]
    return lowfi('案B — 卓上型（テーブル）', '伏せ札を「卓の中央に置く」物理感で set-confirm を主役にする。敵の意図もカードとして卓に出るので読み合いが一枚の卓で完結する',
                 '敵が3体以上だと意図カードが窮屈。人物の絵が小さくなり、PixelLab の立ち絵が活きにくい', boxes, None)

def direction_c():
    boxes = [
        (20, 50, 220, 400, 'サイドレール\n\nリーダー顔 / HP\nブロック / 状態\n最悪被ダメ\n\n山札 / 捨て札 / 消滅\n\n直近ログ 5行\n\nキー凡例', 'ui'),
        (260, 50, 680, 40, '予測バー: 最悪 −14 ／ 打ち消し可 2 ／ 伏せ 1/1', 'accent'),
        (260, 100, 680, 220, '戦場（敵は大きく・意図は頭上）', 'field'),
        (260, 330, 680, 120, '手札は一列・文字を大きく（ホバーで拡大）', 'ui'),
        (860, 100, 70, 40, 'ログ', 'ui'),
    ]
    return lowfi('案C — HUDレール型（情報優先）', 'テスターがいま一番使っている数字（最悪被ダメ・伏せ・打ち消し）を左のレールと予測バーに常設し、暗算を消す',
                 '画面が「道具」に寄り、ゲームらしい没入感は薄れる。1280 幅では手札が窮屈', boxes, None)

def write(name, body):
    with open(os.path.join(OUT, name), 'w', encoding='utf-8') as f:
        f.write(HEAD + body + TAIL)

write('Main.dc.html', battle_scene(False))
write('ReactionWindow.dc.html', battle_scene(True))
write('CardAnatomy.dc.html', card_anatomy())
write('DirectionB.dc.html', direction_b())
write('DirectionC.dc.html', direction_c())
canvas = {
    'artboards': [
        {'file': 'Main.dc.html', 'title': '戦闘画面（自分のターン・手札ホバー）', 'x': 0, 'y': 0, 'w': 1920, 'h': 1080},
        {'file': 'ReactionWindow.dc.html', 'title': '確認ウィンドウ（発動／温存）', 'x': 2040, 'y': 0, 'w': 1920, 'h': 1080},
        {'file': 'CardAnatomy.dc.html', 'title': 'カードの面', 'x': 0, 'y': 1240, 'w': 1200, 'h': 720},
        {'file': 'DirectionB.dc.html', 'title': '別案B（低精細）', 'x': 1320, 'y': 1240, 'w': 960, 'h': 540},
        {'file': 'DirectionC.dc.html', 'title': '別案C（低精細）', 'x': 2400, 'y': 1240, 'w': 960, 'h': 540},
    ],
    'annotations': [
        {'id': 'brief', 'x': 0, 'y': -200, 'w': 640, 'text': '戦闘UIの作り直し（静的モック）。Theme/UiKit の実値（#131917 夜空・#e0b84a 金・Noto Sans JP・カード200×290）で描いた本命＝現行の「左にリーダー、右に敵、扇の手札」を保ちつつ、伏せ場を専用スロットに、最悪被ダメをHPの直下に、意図を吹き出しに、置物を面で並べる。'},
        {'id': 'note-modal', 'x': 2040, 'y': -120, 'w': 520, 'text': 'set-confirm の山場。左に「実値・受け・HP差分」、中央に候補札、右に発動／温存。後続の敵の分岐が反転する警告を金で。'},
        {'id': 'note-alts', 'x': 1320, 'y': 1140, 'w': 560, 'text': '別案は構造だけの下書き。B は伏せ場を卓の中央に置く物理感、C は数字を常設する情報優先。どちらかに寄せるなら本命に取り込む。'},
    ],
    'launch': {'view': 'canvas'},
}
with open(os.path.join(OUT, 'canvas.json'), 'w', encoding='utf-8') as f:
    json.dump(canvas, f, ensure_ascii=False, indent=2)
print('written', sorted(os.listdir(OUT)))

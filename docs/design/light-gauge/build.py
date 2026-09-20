# build.py — 灯の表示 (2026-09-20 ユーザー「ひなたの戦闘画面に灯表記をエナジーくらいリッチに表現してほしい」)。
# 灯 (白の蓄積資源) はいま「資源の札」の中の 13px のピル「灯 6」でしかなく、エナジーの輪 (紙の円盤＋真鍮の弧＋40px の数字) と釣り合わない。
# 現状 (PC 1920×1080・スマホ 1462×675) の指摘 ＋ 3案 (A 双子の輪／B 真鍮のランタン／C 灯の列) ＋ スマホの置き場 ＋ 演出と情報 ＋ 比較と原則。
# 下地は実機のスクショ (`STATE="phase=combat;leader=leader_white;deck=deck_horde_v2;enemy=enc_probe_pair;perms=white_perm_wick,white_perm_squire;plight=6;penergy=3;
#   hand=white_light_bolt,white_march_order,white_light_hoard,white_strike,white_guard;wait=2.5" scripts/unity-win.sh shots state`。hideui=1 の版を UI の差し替えに使う。
#   スマホは SHOT_W=1920 SHOT_H=886 UISCALE=1.6 で撮って 1462×675 に縮小 = キャンバス単位)。
# ランタンの絵は仮＝灯の矢の挿絵から切り出した (lantern-src.png 26×44)。本番は PixelLab で 32×48 (正面・黒鉄の枠と真鍮・暗い硝子) を作り、炎と光はコードで描く。
# 使い方: python3 build.py → *.dc.html → node ../../../scripts/design-canvas/seed-canvas.mjs で束ねる (README 参照)。
import base64, os, json
OUT = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(OUT, '..', '..', '..'))
ART_DIR = os.path.join(REPO, 'unity', 'Assets', 'Resources', 'Art')

# カラーテーマ「黒鉄と真鍮」(docs/color-theme.md)
INK = '#2f2e35'; INK_SOFT = '#4e4c55'; PAPER = '#f4ecd6'; PAPER2 = '#eadfc4'; PAPER3 = '#fbf6e8'; PAPER_DIM = '#c4beb2'
BRASS = '#c99a3a'; BRASS_LIGHT = '#ead08a'; BRASS_INK = '#634410'; MANA = '#3aa79b'; MANA_LIGHT = '#b5ddd6'; MANA_INK = '#155650'
ROSE = '#c9635a'; SKY = '#6f95b8'; SKY_LIGHT = '#d6e6fa'; SKY_INK = '#2f5a7a'; PLUM = '#9d86bf'; PLUM_LIGHT = '#e9def3'; PLUM_INK = '#5a3d78'; BAD_INK = '#9c3a2a'
MOSS = '#7fa86c'; MOSS_INK = '#276a34'; NIGHT = '#1a1c33'; WINDOW = '#20233a'; GROUND = '#0f1120'
FLAME_CORE = '#fff6d2'   # 炎の芯 = 紙 (明) より白い。灯の光は真鍮の系 (BrassLight → 白) で、延焼の橙 (Ember) は使わない

def uri(path):
    with open(path, 'rb') as f: return 'data:image/png;base64,' + base64.b64encode(f.read()).decode('ascii')
def icon(name): return uri(os.path.join(ART_DIR, 'icons', name + '.png'))
ICON = {k: icon(k) for k in ['energy', 'heart', 'sword', 'shield', 'draw', 'crest_permanent', 'star', 'exposed', 'set']}
LANTERN = uri(os.path.join(OUT, 'lantern-src.png')); LW, LH = 26, 44
CARD_ART = {k: uri(os.path.join(ART_DIR, 'cards', k + '.png')) for k in ['white_march_order', 'white_light_bolt', 'white_perm_wick']}

W, H = 1920, 1080          # PC
PW, PH = 1462, 675         # スマホ (キャンバス単位)

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
    body { margin: 0; background: #0f1120; font-family: "Klee One", "Hiragino Sans", "Noto Sans JP", sans-serif; color: #2f2e35; -webkit-font-smoothing: antialiased; }
    a { color: #634410; } a:hover { color: #2f2e35; }
    .abs { position: absolute; }
    .deco { font-family: "Kaisei Decol", "Hiragino Mincho ProN", serif; font-weight: 700; }
    .px { image-rendering: pixelated; }
    .note { background: #fff3ea; color: #9c3a2a; border: 1.5px solid #9c3a2a; border-radius: 6px; padding: 6px 10px; font-size: 14px; line-height: 19px; box-shadow: 0 4px 10px rgba(0,0,0,0.35); box-sizing: border-box; }
    .note b { color: #7a2418; }
    .cap { background: rgba(8,8,20,0.8); color: #f4ecd6; font-size: 14px; line-height: 20px; padding: 1px 10px; border-radius: 4px; letter-spacing: 0.04em; white-space: nowrap; }
    .outl { color: #f4ecd6; text-shadow: 0 0 2px #2f2e35, 0 0 2px #2f2e35, 1px 1px 0 #2f2e35, -1px -1px 0 #2f2e35, 1px -1px 0 #2f2e35, -1px 1px 0 #2f2e35, 0 2px 4px rgba(0,0,0,0.6); }
    .nn { background: rgba(26,28,51,0.88); color: #f4ecd6; border-radius: 5px; padding: 1px 8px; font-size: 13px; letter-spacing: 0.06em; white-space: nowrap; box-shadow: 0 0 0 1px rgba(244,236,214,0.25); }
  </style>
</helmet>
'''
TAIL = '''</x-dc>
</body>
</html>
'''
def write(name, html):
    with open(os.path.join(OUT, name + '.dc.html'), 'w', encoding='utf-8') as f: f.write(HEAD + html + TAIL)

def board(bg, w=W, h=H, dim=False):
    s = '<div style="position: relative; width: %dpx; height: %dpx; overflow: hidden; background: #0f1120">' % (w, h)
    if bg: s += '<img src="%s" style="position: absolute; left: 0; top: 0; width: %dpx; height: %dpx; display: block%s">' % (bg, w, h, '; filter: brightness(0.7)' if dim else '')
    return s
def close(): return '</div>'
def ic(name, size=16, extra=''):
    return '<img class="px" src="%s" style="width: %dpx; height: %dpx; display: block; flex: none; %s">' % (ICON[name], size, size, extra)
def note(x, y, w, html, arrow=None, z=60):
    s = '<div class="abs note" style="left: %dpx; top: %dpx; width: %dpx; z-index: %d">%s</div>' % (x, y, w, z, html)
    if arrow:
        ax, ay, bx, by = arrow
        s += ('<svg class="abs" style="left: 0; top: 0; overflow: visible; pointer-events: none; z-index: %d" width="1" height="1"><line x1="%d" y1="%d" x2="%d" y2="%d" stroke="#9c3a2a" stroke-width="2.5" stroke-dasharray="6 4"></line><circle cx="%d" cy="%d" r="5" fill="#9c3a2a"></circle></svg>' % (z, ax, ay, bx, by, bx, by))
    return s
def caption(x, y, text): return '<div class="abs cap" style="left: %dpx; top: %dpx; z-index: 60">%s</div>' % (x, y, text)
def nightnote(x, y, text, z=30): return '<div class="abs nn" style="left: %dpx; top: %dpx; z-index: %d">%s</div>' % (x, y, z, text)
def patch(bg, x, y, w, h, bw=W, bh=H, z=1):
    """下地 (UIなし) の同じ場所を貼って、その上の UI を消す"""
    return '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; background: url(%s) -%dpx -%dpx / %dpx %dpx no-repeat; z-index: %d"></div>' % (x, y, w, h, bg, x, y, bw, bh, z)
def paste(bg, x, y, w, h, bw=W, bh=H, z=5, dx=0, dy=0):
    """下地 (UIあり) の同じ場所を (dx,dy) ずらして貼る = 部品の移動"""
    return '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; background: url(%s) -%dpx -%dpx / %dpx %dpx no-repeat; z-index: %d"></div>' % (x + dx, y + dy, w, h, bg, x, y, bw, bh, z)

# ---- 紙の部品 ----
def sheet(x, y, w, h, bg=PAPER2, edge=None, r='8px 10px 8px 9px / 9px 8px 10px 8px', z=None, extra=''):
    ring = '' if not edge else ', 0 0 0 4px %s' % edge
    return '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; background: %s; border-radius: %s; box-shadow: 0 0 0 1.5px %s%s, 0 3px 8px rgba(0,0,0,0.4); box-sizing: border-box; %s%s">' % (x, y, w, h, bg, r, INK, ring, extra, ('z-index: %d;' % z) if z is not None else '')
def pill(text, ink=INK, bg=PAPER2, size=13, h=22, icon_name=None, gap=3):
    return '<span style="display: inline-flex; align-items: center; gap: %dpx; height: %dpx; padding: 0 7px 0 5px; border-radius: 6px; background: %s; color: %s; box-shadow: 0 0 0 1px %s; font-size: %dpx; white-space: nowrap; flex: none">%s%s</span>' % (gap, h, bg, ink, ink, size, ic(icon_name, 13) if icon_name else '', text)
def stamp(cx, cy, text, bg=BRASS_LIGHT, ink=BRASS_INK, edge=BRASS, size=18, rot=-6, z=40):
    """判 (Tween.Stamp): 紙に太い縁・少し傾く"""
    return '<div class="abs deco" style="left: %dpx; top: %dpx; transform: translate(-50%%, -50%%) rotate(%ddeg); background: %s; color: %s; border: 2.5px solid %s; border-radius: 6px; padding: 2px 10px; font-size: %dpx; letter-spacing: 0.1em; white-space: nowrap; box-shadow: 0 3px 8px rgba(0,0,0,0.4); z-index: %d">%s</div>' % (cx, cy, rot, bg, ink, edge, size, z, text)
def floatnum(x, y, text, color=BRASS, size=24, z=50):
    """浮き文字 (Tween.Float): 太い縁取り＋影"""
    return '<div class="abs deco" style="left: %dpx; top: %dpx; transform: translate(-50%%, -50%%); font-size: %dpx; color: %s; text-shadow: 0 0 3px #f4ecd6, 0 0 3px #f4ecd6, 1px 1px 0 #f4ecd6, -1px -1px 0 #f4ecd6, 1px -1px 0 #f4ecd6, -1px 1px 0 #f4ecd6, 0 3px 6px rgba(0,0,0,0.5); white-space: nowrap; z-index: %d">%s</div>' % (x, y, size, color, z, text)

# ---- 資源の絵 ----
def orb(x, y, size, num, mx, label, fill, rot=-3, z=20, dim=False, arc=BRASS, segments=None, inner_glow=0.0, arc_w=7, sub=None):
    """エナジーの輪の写し: 紙の円盤＋真鍮の弧 (fill 0..1)。segments=N なら弧を N 分割の目盛りにする (灯の輪)。inner_glow=中心の光 (0..1)"""
    s = '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; transform: rotate(%ddeg); z-index: %d">' % (x, y, size, size, rot, z)
    # 円盤 (紙) と縁
    s += '<div class="abs" style="left: 6px; top: 6px; right: 6px; bottom: 6px; border-radius: 50%%; background: %s; box-shadow: 0 0 0 1.5px %s, 0 4px 10px rgba(0,0,0,0.45)%s"></div>' % (PAPER if not dim else '#d9d9d9', INK, '' if inner_glow <= 0 else ', inset 0 0 %dpx %dpx rgba(234,208,138,%.2f)' % (size * 0.25, size * 0.06, min(1, inner_glow)))
    # 弧: conic-gradient をリングにマスク
    if segments:
        stops = []
        for i in range(segments):
            a0 = i / segments * 100.0; a1 = (i + 1) / segments * 100.0
            lit = i < round(fill * segments)
            col = arc if lit else 'rgba(47,46,53,0.18)'
            stops.append('%s %.1f%% %.1f%%' % (col, a0 + 1.2, a1 - 1.2))
            stops.append('transparent %.1f%% %.1f%%' % (a1 - 1.2, a1 + 1.2))
        grad = 'conic-gradient(from 0deg, ' + ', '.join(stops) + ')'
    else:
        grad = 'conic-gradient(from 0deg, %s 0%% %.1f%%, rgba(47,46,53,0.12) %.1f%% 100%%)' % (arc, fill * 100, fill * 100)
    s += '<div class="abs" style="left: 0; top: 0; right: 0; bottom: 0; border-radius: 50%%; background: %s; -webkit-mask: radial-gradient(circle closest-side, transparent 0 calc(100%% - %dpx), #000 calc(100%% - %dpx)); mask: radial-gradient(circle closest-side, transparent 0 calc(100%% - %dpx), #000 calc(100%% - %dpx))"></div>' % (grad, arc_w, arc_w, arc_w, arc_w)
    # 数字・上限・名札
    s += '<div class="abs deco" style="left: 0; top: 0; right: 8px; bottom: 6px; display: flex; align-items: center; justify-content: center; font-size: %dpx; color: %s">%s</div>' % (int(size * 0.31), INK, num)
    if mx is not None: s += '<div class="abs" style="left: %dpx; top: %dpx; font-size: %dpx; color: %s">/ %s</div>' % (size * 0.62, size * 0.43, int(size * 0.12), INK_SOFT, mx)
    if sub: s += '<div class="abs" style="left: 0; right: 0; top: %dpx; text-align: center; font-size: %dpx; color: %s">%s</div>' % (size * 0.60, int(size * 0.09), BRASS_INK, sub)
    s += '<div class="abs" style="left: 0; right: 0; top: %dpx; text-align: center; font-size: %dpx; letter-spacing: 0.15em; color: %s">%s</div>' % (size * 0.70, int(size * 0.10), INK_SOFT, label)
    return s + '</div>'

def flame(cx, base, h, alpha=1.0, z=None):
    """炎 1本 (真鍮の系: 白い芯 → 真鍮の紙 → 真鍮)。cx=中心・base=根元の y・h=高さ"""
    w = h * 0.62
    zi = '' if z is None else 'z-index: %d;' % z
    return ('<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; %s opacity: %.2f; border-radius: 50%% 50%% 50%% 50%% / 70%% 70%% 30%% 30%%; '
            'background: radial-gradient(ellipse at 50%% 78%%, %s 0%%, %s 42%%, %s 78%%, rgba(201,154,58,0) 100%%); box-shadow: 0 0 %dpx %dpx rgba(234,208,138,0.35)"></div>'
            % (cx - w / 2, base - h, w, h, zi, alpha, FLAME_CORE, BRASS_LIGHT, BRASS, h * 0.5, h * 0.12))
def glowpool(cx, cy, rx, ry, alpha=0.3, z=10):
    return '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; border-radius: 50%%; background: radial-gradient(closest-side, rgba(255,240,196,%.2f), rgba(234,208,138,%.2f) 45%%, rgba(234,208,138,0)); z-index: %d; pointer-events: none"></div>' % (cx - rx, cy - ry, rx * 2, ry * 2, alpha, alpha * 0.55, z)
def spark(x, y, r=3, alpha=0.9, z=45, col=BRASS_LIGHT):
    return '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; border-radius: 50%%; background: %s; opacity: %.2f; box-shadow: 0 0 %dpx %s; z-index: %d"></div>' % (x - r, y - r, r * 2, r * 2, col, alpha, r * 3, col, z)

TIER = [(0, '消えている'), (1, 'ともる'), (3, '灯る'), (6, '盛る'), (10, '眩い')]
def tier_of(n):
    t = 0
    for i, (lo, _) in enumerate(TIER):
        if n >= lo: t = i
    return t

def lantern(x, base_y, scale, num, label='灯', z=20, tag=True, shake=0, bad=False, preview=None, sparks=True, name_z=None):
    """真鍮のランタン (32×48 ドットの正面図を SVG で描く。scale=1ドットの px): x=中心・base_y=台座の下端。num=灯。
    炎と硝子の光は灯の段階 (TIER) で育つ。bad=灯が足りない (朱)。preview=(文, 色, 地) の注記を頭上に"""
    w, h = 32 * scale, 48 * scale
    t = tier_of(num)
    s = ''
    if t >= 2: s += glowpool(x, base_y - h * 0.06, w * (0.9 + 0.3 * t), h * 0.16 * (0.7 + 0.2 * t), 0.2 + 0.08 * t, z - 3)
    # 硝子の窓の外へ漏れる光 (絵の後ろ)
    if t >= 1: s += '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; border-radius: 50%%; background: radial-gradient(closest-side, rgba(255,246,210,%.2f), rgba(234,208,138,%.2f) 50%%, rgba(234,208,138,0)); z-index: %d"></div>' % (x - w * 0.95, base_y - h * 0.78, w * 1.9, h * 0.72, 0.28 + 0.14 * t, 0.2 + 0.08 * t, z - 2)
    iron, iron_hi, brass, brass_hi, brass_lo = INK, INK_SOFT, BRASS, BRASS_LIGHT, BRASS_INK
    if t == 0 and not bad: iron, iron_hi, brass, brass_hi, brass_lo = '#26252c', '#3a393f', '#8a6d33', '#a58a52', '#4a3410'
    glass = WINDOW if t == 0 else ('url(#glassbad)' if bad else 'url(#glass)')
    r = []
    def R(x0, y0, w0, h0, fill): r.append('<rect x="%g" y="%g" width="%g" height="%g" fill="%s"/>' % (x0, y0, w0, h0, fill))
    # 吊り輪と飾り
    R(14, 0, 4, 1, brass_lo); R(13, 1, 1, 3, brass); R(18, 1, 1, 3, brass); R(14, 3, 4, 1, brass_hi)
    # 笠 (台形)
    for i, (x0, w0) in enumerate([(13, 6), (11, 10), (9, 14), (7, 18), (6, 20)]): R(x0, 4 + i, w0, 1, brass_hi if i < 2 else brass)
    R(6, 9, 20, 1, brass_lo)
    # 上の板
    R(4, 10, 24, 2, brass); R(4, 10, 24, 1, brass_hi)
    # 胴の枠 (黒鉄) と硝子
    R(4, 12, 24, 26, iron)
    R(5, 12, 1, 26, iron_hi); R(4, 12, 24, 1, iron_hi)
    r.append('<rect x="7" y="14" width="18" height="22" fill="%s"/>' % glass)
    # 硝子の桟 (細い黒鉄の縦線) と真鍮の鋲
    R(15.5, 14, 1, 22, 'rgba(47,46,53,0.35)')
    for (rx, ry) in [(5, 13), (26, 13), (5, 36), (26, 36)]: R(rx, ry, 1, 1, brass_hi)
    # 下の板
    R(4, 38, 24, 2, brass); R(4, 39, 24, 1, brass_lo)
    # 台座
    R(13, 40, 6, 3, iron); R(13, 40, 1, 3, iron_hi)
    R(10, 43, 12, 2, brass); R(8, 45, 16, 2, brass); R(8, 45, 16, 1, brass_hi); R(7, 47, 18, 1, brass_lo)
    flame_svg = ''
    if t >= 1:
        fh = [0, 7, 11, 14, 17][t]; fw = [0, 4, 6, 7, 8][t]
        cx, cy = 16, 35
        flame_svg = '<path d="M %g %g C %g %g, %g %g, %g %g C %g %g, %g %g, %g %g Z" fill="url(#flame)"/>' % (
            cx - fw / 2, cy, cx - fw / 2, cy - fh * 0.55, cx - fw * 0.15, cy - fh * 0.85, cx, cy - fh,
            cx + fw * 0.15, cy - fh * 0.85, cx + fw / 2, cy - fh * 0.55, cx + fw / 2, cy)
    defs = ('<defs><radialGradient id="glass" cx="50%%" cy="95%%" r="90%%"><stop offset="0" stop-color="%s"/><stop offset="0.45" stop-color="%s"/><stop offset="1" stop-color="%s"/></radialGradient>'
            '<radialGradient id="glassbad" cx="50%%" cy="95%%" r="90%%"><stop offset="0" stop-color="#ffd9d0"/><stop offset="0.5" stop-color="#e8a498"/><stop offset="1" stop-color="#9c3a2a"/></radialGradient>'
            '<radialGradient id="flame" cx="50%%" cy="80%%" r="70%%"><stop offset="0" stop-color="%s"/><stop offset="0.5" stop-color="%s"/><stop offset="1" stop-color="%s" stop-opacity="0.9"/></radialGradient></defs>') % (FLAME_CORE, BRASS_LIGHT, BRASS, FLAME_CORE, BRASS_LIGHT, BRASS)
    rot = 'transform: rotate(%ddeg); transform-origin: 50%% 90%%;' % shake if shake else ''
    s += '<svg class="abs" viewBox="0 0 32 48" width="%d" height="%d" shape-rendering="crispEdges" style="left: %dpx; top: %dpx; z-index: %d; %s filter: drop-shadow(0 3px 4px rgba(0,0,0,0.45))">%s%s%s</svg>' % (w, h, x - w / 2, base_y - h, z, rot, defs, ''.join(r), flame_svg)
    # 硝子の窓に数字 (x 7〜25・y 14〜36 ドット)
    gx, gy, gw, gh = x - w / 2 + 7 * scale, base_y - h + 14 * scale, 18 * scale, 22 * scale
    numcol = PAPER_DIM if t == 0 else (BAD_INK if bad else INK)
    s += '<div class="abs deco" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; display: flex; align-items: center; justify-content: center; font-size: %dpx; color: %s; z-index: %d; text-shadow: 0 0 5px rgba(255,246,210,0.95), 0 0 2px rgba(255,246,210,0.9)">%s</div>' % (gx, gy, gw, gh, int(gh * 0.62), numcol, z + 3, num)
    if sparks and t >= 3:
        for i, (dx, dy, rr) in enumerate([(-0.3, 1.02, 2.5), (0.28, 1.14, 2), (0.05, 1.26, 1.5), (0.5, 0.98, 2), (-0.45, 1.2, 1.5)][: 1 + t]):
            s += spark(x + w * dx, base_y - h * dy, rr * scale / 3, 0.85, z + 4)
    if tag:
        s += '<div class="abs" style="left: %dpx; top: %dpx; transform: translateX(-50%%); z-index: %d">%s</div>' % (x, base_y + 4, z + 5 if name_z is None else name_z, pill(label, INK_SOFT, PAPER2, 13, 20))
    if preview:
        txt, col, bg = preview
        s += '<div class="abs deco" style="left: %dpx; top: %dpx; transform: translateX(-50%%); background: %s; color: %s; border-radius: 6px; padding: 1px 8px; font-size: 15px; box-shadow: 0 0 0 1.5px %s, 0 3px 6px rgba(0,0,0,0.4); white-space: nowrap; z-index: %d">%s</div>' % (x, base_y - h - 30, bg, col, col, z + 6, txt)
    return s

def rail(x, y, w, count, slots=8, scale=1.0, z=20, label='灯', highlight=None, extra=None):
    """灯の列: 真鍮の横木に灯芯 (slots 本)。count 本が燃えている。highlight=(n, text) で右から n 本に印と注記"""
    post_gap = (w - 90 * scale) / slots
    s = '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; z-index: %d">' % (x, y, w, 46 * scale, z)
    # 数字の札
    s += '<div class="abs deco" style="left: 0; top: %dpx; height: %dpx; padding: 0 10px; display: flex; align-items: center; gap: 6px; background: %s; border-radius: 7px; box-shadow: 0 0 0 1.5px %s, 0 3px 6px rgba(0,0,0,0.4); font-size: %dpx; color: %s">%s <span style="font-size: %dpx; font-family: \'Klee One\', sans-serif; font-weight: 400; letter-spacing: 0.1em; color: %s">%s</span></div>' % (6 * scale, 34 * scale, PAPER, INK, int(26 * scale), INK, count, int(12 * scale), INK_SOFT, label)
    # 横木
    rx = 80 * scale
    s += '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; border-radius: 3px; background: linear-gradient(%s, %s 55%%, %s); box-shadow: 0 2px 4px rgba(0,0,0,0.5)"></div>' % (rx, 30 * scale, w - rx, 6 * scale, BRASS_LIGHT, BRASS, BRASS_INK)
    for i in range(slots):
        cx = rx + post_gap * (i + 0.5)
        # 灯芯
        s += '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; background: %s; border-radius: 1px"></div>' % (cx - 1.5 * scale, 22 * scale, 3 * scale, 9 * scale, INK)
        if i < count: s += flame(cx, 24 * scale, 27 * scale, 1.0)
        else: s += '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; border-radius: 50%%; background: rgba(234,208,138,0.22); box-shadow: 0 0 0 1px rgba(47,46,53,0.6)"></div>' % (cx - 3 * scale, 14 * scale, 6 * scale, 6 * scale)
    if count > slots:
        s += '<div class="abs deco" style="left: %dpx; top: %dpx; font-size: %dpx; color: %s; text-shadow: 0 0 3px #000">+%d</div>' % (w + 4, 6 * scale, int(18 * scale), BRASS_LIGHT, count - slots)
    if highlight:
        n, text = highlight
        x0 = rx + post_gap * (min(count, slots) - n); x1 = rx + post_gap * min(count, slots)
        s += '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; border: 2px dashed %s; border-radius: 6px"></div>' % (x0, -4, x1 - x0, 44 * scale, BAD_INK if text.startswith('-') else MOSS_INK)
        s += '<div class="abs deco" style="left: %dpx; top: %dpx; transform: translateX(-50%%); font-size: %dpx; color: %s; background: %s; border-radius: 6px; padding: 0 8px; box-shadow: 0 0 0 1.5px %s; white-space: nowrap">%s</div>' % ((x0 + x1) / 2, -30, int(14 * scale), BAD_INK if text.startswith('-') else MOSS_INK, PAPER, INK, text)
    if extra: s += extra
    return s + '</div>'

# ---- 位置 (PC 1920×1080) ----
ORB_PC = (96, 836, 128)              # エナジーの輪 (x, top, size)
STRIP_PC = (40, 640, 800, 138)       # 自分の札 (HP・からくり・ギア・置物)
CHIP_PC = (54, 712, 84, 24)          # 「灯 6」のピル
HAND_L_PC = 470                      # 手札の左端
DECK_PC = (22, 1018, 130, 36)        # 山札の札
LEADER_PC = dict(cx=460, feet=600)
DOLLS_PC = [(533, 600), (633, 605)]
OWL_PC = [(1270, 560), (1530, 520)]
# ---- 位置 (スマホ 1462×675) ----
ORB_PH = (96, 431, 128)
CARD_PH = (23, 265, 228, 101)        # 自分の札 (HP・被ダメ・資源の1行)
CHIP_PH = (40, 337, 116, 26)
HAND_L_PH = 251
DECK_PH = (23, 609, 130, 38)
LEADER_PH = dict(cx=350, feet=305)
OWL_PH = [(1255, 340), (1530 * 0.7616, 300)]

def energy_pc(x=None, size=None):
    x0, y0, sz = ORB_PC
    return orb(x if x is not None else x0, y0 + (sz - (size or sz)), size or sz, '3', '3', 'エナジー', 1.0)
def hide_chip_pc(): return patch('cur-pc.jpg', 44, 706, 110, 34, z=6, bw=W, bh=H) .replace('cur-pc.jpg', 'strip-paper.jpg') if False else '<div class="abs" style="left: 46px; top: 704px; width: 108px; height: 36px; background: %s; z-index: 6"></div>' % PAPER2
def hide_chip_ph(): return '<div class="abs" style="left: 32px; top: 333px; width: 132px; height: 32px; background: %s; z-index: 6"></div>' % PAPER2

# ================================================================ 現状
def board_current_pc():
    s = board('cur-pc.jpg')
    s += note(30, 250, 330, '<b>①</b> 灯はここ＝資源の札の 13px のピル「灯 6」。成長・勢い・弱体と同じ見た目で、<b>白の主資源が状態異常の列に紛れる</b>', (120, 330, 96, 716))
    s += note(30, 430, 330, '<b>②</b> エナジーは 128px の紙の円盤＋真鍮の弧＋40px の数字＋名札。灯にはこの器が無い。「灯 +1」の浮き文字も<b>エナジーの輪の上に出る</b> (灯の置き場が無いので)', (200, 560, 200, 890))
    s += note(700, 100, 380, '<b>③</b> 灯が 0 になるとピルごと消える＝「灯0 で放出は不発」「灯2 で号令が撃てる」が画面から読めない。しきい値 (灯5でさらに／灯コスト2) の手掛かりも無い', None)
    s += note(1180, 100, 380, '<b>④</b> 号令 (灯−2)・放出 (灯を全て失う)・回復や灯芯の人形の +1 は、数字が変わるだけで<b>炎の絵が無い</b>。エナジーには X の玉が飛び・不足で輪が朱に光る反応がある', None)
    s += caption(24, 1050, '現状 (PC 1920×1080・ひなた・灯6・エナジー3/3・灯芯の人形と剣の人形・探り屋の二人組)。灯＝資源の札のピル')
    return s + close()

def board_current_ph():
    s = board('cur-phone.jpg', PW, PH)
    s += note(270, 300, 300, '<b>①</b> スマホも同じ: 自分の札の3段目のピル「灯 6」(13px)。手札の下端より上・エナジーの輪より小さい', (270, 340, 160, 350))
    s += note(270, 420, 330, '<b>②</b> エナジーの輪は 128px。右は手札 (251px から) なので<b>灯の器を横に置く余地は 27px</b>。上 (自分の札との間) は 65px', (300, 470, 224, 500))
    s += caption(24, 648, '現状 (スマホ 1462×675・同じ盤面)')
    return s + close()

# ================================================================ 案A 双子の輪 (灯の輪をエナジーの右に)
def light_orb(x, top, size, num, z=20, preview=None):
    n = int(num)
    fill = min(1.0, n / 6.0)
    s = orb(x, top, size, str(n), None, '灯', fill, rot=3, z=z, segments=6, inner_glow=0.0 if n == 0 else min(0.3, 0.08 + n * 0.03), arc=BRASS, sub=('満ちる' if n >= 6 else None), dim=(n == 0))
    if n >= 3:
        for i, (dx, dy, r) in enumerate([(0.3, -0.06, 2.5), (0.72, -0.1, 2), (0.5, -0.16, 1.5)][: 1 + n // 3]):
            s += spark(x + size * dx, top + size * dy, r, 0.85, z + 2)
    if preview:
        txt, col, bg = preview
        s += '<div class="abs deco" style="left: %dpx; top: %dpx; transform: translateX(-50%%); background: %s; color: %s; border-radius: 6px; padding: 1px 8px; font-size: 15px; box-shadow: 0 0 0 1.5px %s, 0 3px 6px rgba(0,0,0,0.4); white-space: nowrap; z-index: %d">%s</div>' % (x + size / 2, top - 28, bg, col, col, z + 6, txt)
    return s

def board_a_pc():
    s = board('cur-pc.jpg')
    s += hide_chip_pc()
    s += light_orb(248, 836, 128, 6)
    s += note(30, 250, 330, '<b>双子の輪</b>: エナジーと同じ器 (紙の円盤・数字 40px・名札) をすぐ右に。傾きは逆 (+3°) で対にする。弧は<b>6分割の目盛り</b>＝灯1につき1つ点く (典型の灯6で「満ちる」)。7以上は目盛りが全部点いたまま数字だけ増え、中心が白く光る', (200, 400, 312, 850))
    s += note(30, 430, 330, '資源の札のピル「灯 6」は消す (同じ物を2か所に描かない)。「灯 +1／−2／放出」の浮き文字と、X の玉と同じ真鍮の粒の飛び先は<b>この輪</b> (anchor "light")', (200, 540, 140, 720))
    s += note(1180, 100, 380, '<b>良い</b>: 文法がエナジーと同じ＝一目で「あなたの資源が2つ」。実装も BuildEndTurn の輪をもう1つ組むだけ。<br><b>弱い</b>: 灯に上限は無いので「満ちる」は嘘になりかける (6以上も溜める価値はある)。ランタンの絵が無いので「灯＝ひなたの灯り」が絵で読めない', None)
    s += caption(24, 1050, '案A 双子の輪 (PC・灯6)。エナジーの右に同じ器・6分割の目盛り')
    return s + close()

# ================================================================ 案B 真鍮のランタン (推奨)
def board_b_pc(num=6):
    s = board('cur-pc.jpg')
    s += hide_chip_pc()
    s += lantern(300, 964, 3, num)
    s += note(30, 250, 330, '<b>真鍮のランタン</b>: ひなたが竿に提げているのと同じ形の灯籠 (32×48 ドット・正面・黒鉄の枠と真鍮・PC は 3px/ドット＝96×144) をエナジーの輪の右に立てる。<b>硝子の窓に数字 (40px)</b>、その後ろで炎が燃える', (200, 400, 300, 850))
    s += note(30, 430, 330, '<b>炎は灯の量で育つ</b>: 0＝消えて硝子は暗い (数字は薄墨の 0)／1〜2 小さな火／3〜5 灯る＋足元に光溜まり／6〜9 盛る＋火の粉が舞う／10〜 眩い (白い芯)。上限は無いので「満ちる」は言わない＝溜めるほど明るい', None)
    s += note(1180, 100, 380, '<b>良い</b>: 一目で「ひなたの灯り」。エナジー (輪) と灯 (灯籠) が<b>別の形</b>なので混ざらない。炎の大小が「いま吐くか溜めるか」の手触りになる。<br><b>弱い</b>: 絵を1枚作る (PixelLab。それまではコード生成の枠)。数字は硝子の中なので輪より少し小さい', None)
    s += note(700, 100, 380, '足元の名札「灯」はタップの的＝用語の説明 (灯の定義・この戦闘の入り: 灯匠 +1／灯芯 +1／回復 +1)。エナジーの輪と下端を揃える', None)
    s += caption(24, 1050, '案B 真鍮のランタン (PC・灯6)。エナジーの右に灯籠・硝子に数字・炎が育つ・足元に光溜まり (推奨)。絵は仮＝灯の矢の挿絵の切り出し')
    return s + close()

# ================================================================ 案C 灯の列 (真鍮の横木に炎)
def board_c_pc():
    s = board('cur-pc.jpg')
    s += hide_chip_pc()
    s += rail(244, 900, 226, 6, 8, 1.0)
    s += note(30, 250, 330, '<b>灯の列</b>: 真鍮の横木に灯芯を8本並べ、灯1につき1本が燃える。左の札に数字。8を超えたら右端に「+N」。号令で2払えば右の2本が消え、放出なら全部の炎が敵へ飛ぶ＝<b>払った本数がそのまま見える</b>', (200, 400, 330, 895))
    s += note(30, 430, 330, 'しきい値の印が自然: 灯コスト2の札をつかむと右の2本に朱の枠「−2」、「灯5でさらに」の札なら5本目に苔の目印', None)
    s += note(1180, 100, 380, '<b>良い</b>: 数えられる・払う本数が見える・薄いので置き場に困らない (スマホは自分の札と輪の間の 65px に入る)。<br><b>弱い</b>: 小さな炎の列は「リッチ」というより状態バー。10 を超えると列が伸びるか「+N」に逃げる。エナジーの輪と釣り合う大きな物が無い', None)
    s += caption(24, 1050, '案C 灯の列 (PC・灯6)。エナジーの右に横木と炎の列')
    return s + close()

# ================================================================ スマホの置き場 (3案)
def phone_a():
    s = board('cur-phone.jpg', PW, PH)
    s += hide_chip_ph()
    # エナジーの輪を左へ寄せて小さく (128→108)、灯の輪を右に
    s += patch('base-phone.jpg', 90, 425, 140, 140, PW, PH, z=2)
    s += orb(12, 451, 108, '3', '3', 'エナジー', 1.0, z=20)
    s += light_orb(132, 451, 108, 6)
    return s + close()
def phone_b():
    s = board('cur-phone.jpg', PW, PH)
    s += hide_chip_ph()
    s += patch('base-phone.jpg', 90, 425, 140, 140, PW, PH, z=2)
    s += orb(16, 431, 128, '3', '3', 'エナジー', 1.0, z=20)
    s += lantern(196, 559, 2.5, 6)
    return s + close()
def phone_c():
    s = board('cur-phone.jpg', PW, PH)
    s += hide_chip_ph()
    s += rail(26, 380, 222, 6, 8, 0.86)
    return s + close()
def phone_b_hp():
    """自分の札に状態異常の行が増えた時 (弱体・虚弱) でもランタンは動かない"""
    s = board('cur-phone.jpg', PW, PH)
    s += hide_chip_ph()
    s += '<div class="abs" style="left: 40px; top: 337px; display: flex; gap: 6px; z-index: 7">%s%s</div>' % (pill('弱体 2T', PLUM_INK, PLUM_LIGHT, 13, 24, 'exposed'), pill('成長 3', MOSS_INK, PAPER2, 13, 24, 'star'))
    s += patch('base-phone.jpg', 90, 425, 140, 140, PW, PH, z=2)
    s += orb(16, 431, 128, '3', '3', 'エナジー', 1.0, z=20)
    s += lantern(196, 559, 2.5, 6)
    return s + close()

def crop(html, x, y, w, h, scale, left, top, title, foot, fw=None):
    """スマホの盤面 (1462×675 の HTML) の (x,y,w,h) を scale 倍で切り出して置く"""
    cw, ch = w * scale, h * scale
    s = '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; overflow: hidden; border-radius: 6px; box-shadow: 0 0 0 1.5px #3b2f2f; background: #0f1120">' % (left, top, cw, ch)
    s += '<div style="position: absolute; left: %dpx; top: %dpx; transform: scale(%.3f); transform-origin: 0 0">%s</div>' % (-x * scale, -y * scale, scale, html)
    s += '</div>'
    s += '<div class="abs deco" style="left: %dpx; top: %dpx; font-size: 17px; color: %s">%s</div>' % (left, top - 28, PAPER, title)
    s += '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; font-size: 13px; line-height: 18px; color: %s">%s</div>' % (left, top + ch + 8, fw or cw, PAPER_DIM, foot)
    return s

def board_phone():
    Wb, Hb = 2140, 1400
    s = '<div style="position: relative; width: %dpx; height: %dpx; overflow: hidden; background: #0f1120">' % (Wb, Hb)
    s += '<div class="abs deco" style="left: 24px; top: 14px; font-size: 22px; color: %s">スマホの置き場 (1462×675 の左下を 1.5倍で切り出し)</div>' % PAPER
    s += '<div class="abs" style="left: 24px; top: 46px; width: 1600px; font-size: 14px; line-height: 20px; color: %s">スマホは手札が 251px から始まるので、エナジーの輪 (96〜224) の右には 27px しか無い。3案とも「輪を左へ寄せる」か「上の隙間 (自分の札の下 366〜431) を使う」かで置く。人形の足元の札・匣は動かさない</div>' % PAPER_DIM
    X, Y, CW, CH, SC = 0, 240, 520, 435, 1.3
    s += crop(phone_a(), X, Y, CW, CH, SC, 24, 120, 'A 双子の輪 (輪を 108px に縮めて2つ並べる)', 'エナジーを 12〜120、灯を 132〜240 に。数字は 40→34px。手札との間 11px。2つの輪が同じ大きさで並ぶので釣り合いは最も良いが、エナジーの輪も小さくなる')
    s += crop(phone_b(), X, Y, CW, CH, SC, 24 + 700, 120, 'B ランタン (輪はそのまま左へ寄せ、右に灯籠) ＝推奨', 'エナジーの輪は 128px のまま 16〜144 へ (山札の札の真上)。灯籠は 2.5px/ドット＝80×120 で 156〜236。手札との間 15px。硝子の数字は 26px (帳面の HP と同じ帯)。足元の名札「灯」は輪の名札と同じ高さ')
    s += crop(phone_c(), X, Y, CW, CH, SC, 24 + 1400, 120, 'C 灯の列 (自分の札と輪の間の隙間に横木)', '横木は 26〜248・高さ 40。輪は動かさない。自分の札に状態異常の行が増えると (2行目) 隙間が 35px に縮み、横木が輪に掛かる＝その時は輪の上に重ねて縮める')
    s += crop(phone_b_hp(), X, Y, CW, CH, SC, 24 + 1400, 120 + 660, 'B 状態異常が付いた時 (自分の札が伸びても灯籠は動かない)', '自分の札の資源の行 (弱体・成長) は今までどおり札の中。灯だけを札から出したので、行が増えても輪と灯籠の高さは変わらない (最悪 2行＝札の下端 396・灯籠の上端 451 で 55px 空く)')
    s += '<div class="abs" style="left: 24px; top: 800px; width: 1300px; font-size: 14px; line-height: 20px; color: %s"><b style="color: %s">共通</b>: ①灯の器は白の色を持つリーダー (ひなた・なぎ・あかり・あさひ) では<b>灯0 でも出す</b> (消えた灯籠)。他のリーダーは灯が 1 以上になった時だけ出る (黒のドレインの回復でも灯は溜まるが、吐く札は白だけ)。②「灯 +1」「灯 −2」「灯6 放出」の浮き文字は灯の器の上。③灯コストが払えない札は今までどおり扇の中で沈み、押すと灯籠が首を振って「灯が足りない (あと1)」。</div>' % (PAPER_DIM, PAPER)
    s += caption(24, 1372, '下地は同じ盤面 (灯6・エナジー3/3)。切り出しは x 0〜520・y 240〜675')
    return s + '</div>'

# ================================================================ 演出と情報 (案B の上で)
def cell(x, y, w, h, bg_css, title, inner, foot, z=1):
    s = '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; %s; border-radius: 6px; box-shadow: 0 0 0 1.5px #3b2f2f; overflow: hidden; z-index: %d">%s</div>' % (x, y, w, h, bg_css, z, inner)
    s += '<div class="abs deco" style="left: %dpx; top: %dpx; font-size: 16px; color: %s">%s</div>' % (x, y - 26, PAPER, title)
    s += '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; font-size: 13px; line-height: 18px; color: %s">%s</div>' % (x, y + h + 6, w, PAPER_DIM, foot)
    return s
def crop_bg(bg, x, y, w, h, scale=1.0, bw=W, bh=H):
    return 'background: url(%s) -%dpx -%dpx / %dpx %dpx no-repeat' % (bg, x * scale, y * scale, bw * scale, bh * scale)

def board_moments():
    Wm, Hm = 1540, 1330
    s = '<div style="position: relative; width: %dpx; height: %dpx; overflow: hidden; background: #0f1120">' % (Wm, Hm)
    s += '<div class="abs deco" style="left: 24px; top: 14px; font-size: 22px; color: %s">演出と情報 (案B の上で。PC の左下 x 60〜520・y 700〜1000 を切り出し)</div>' % PAPER
    cw, ch = 460, 300
    R1, R2, R3 = 84, 470, 856
    ox, oy = 60, 700
    def L(num, **kw): return lantern(300 - ox, 964 - oy, 3, num, **kw)
    def E(): return orb(96 - ox, 836 - oy, 128, '3', '3', 'エナジー', 1.0)
    base = crop_bg('cur-pc.jpg', ox, oy, cw, ch) + '; background-color: #0f1120'
    chip_hide = '<div class="abs" style="left: %dpx; top: %dpx; width: 108px; height: 36px; background: %s"></div>' % (46 - ox, 704 - oy, PAPER2)
    # ① 灯+1
    inner = chip_hide + E() + L(7)
    # 光の粒が出所 (灯芯の人形＝画面の右上・ここでは切り出しの外) から飛んで来る軌跡
    inner += '<svg class="abs" style="left: 0; top: 0; overflow: visible; z-index: 30" width="1" height="1"><path d="M 470 20 C 380 60, 330 120, 310 180" fill="none" stroke="%s" stroke-width="3" stroke-dasharray="2 7" stroke-linecap="round" opacity="0.9"></path></svg>' % BRASS_LIGHT
    inner += spark(318, 170, 5, 1.0, 40) + spark(360, 110, 3, 0.7, 40) + spark(420, 50, 2.5, 0.5, 40)
    inner += floatnum(300 - ox, 964 - oy - 175, '灯 +1', BRASS, 26)
    s += cell(24, R1, cw, ch, base, '① 灯を得る (回復・灯芯の人形・灯匠の +1)', inner, '真鍮の粒が<b>出どころ</b> (回復ならひなたの胸・人形ならその人形・灯匠ならひなたのランタン) から灯籠へ飛び込み、着いた瞬間に炎がひと膨らみ (Punch) して数字が増え、上に「灯 +1」。浮き文字の色は真鍮 (今と同じ)')
    # ② 号令 (灯−2)
    inner = chip_hide + E() + L(4)
    inner += '<svg class="abs" style="left: 0; top: 0; overflow: visible; z-index: 30" width="1" height="1"><path d="M 300 180 C 340 120, 400 100, 460 60" fill="none" stroke="%s" stroke-width="3" stroke-dasharray="2 7" stroke-linecap="round" opacity="0.9"></path></svg>' % BRASS_LIGHT
    inner += spark(340, 130, 4, 0.9, 40) + spark(410, 90, 4, 0.9, 40)
    inner += floatnum(300 - ox, 964 - oy - 175, '灯 −2', INK_SOFT, 26)
    inner += '<div class="abs" style="left: 392px; top: 8px; width: 60px; height: 80px; border-radius: 4px; background: %s; box-shadow: 0 0 0 1.5px %s; z-index: 20; overflow: hidden"><img class="px" src="%s" style="width: 60px; height: 36px; display: block"><div style="font-size: 9px; text-align: center; padding-top: 4px">点灯の合図</div></div>' % (PAPER, INK, CARD_ART['white_march_order'])
    s += cell(514, R1, cw, ch, base, '② 号令 (灯コストを払う: 点灯の合図・灯2)', inner, '払った数だけ真鍮の粒が灯籠から<b>その札</b>へ飛ぶ (X の玉が輪から飛ぶのと同じ Presenter.ShowEnergyPaid の器)。炎が一段小さくなり「灯 −2」(中墨)。ログは「灯-2 (点灯の合図)」のまま')
    # ③ 放出
    inner = chip_hide + E() + L(0, sparks=False)
    inner += '<svg class="abs" style="left: 0; top: 0; overflow: visible; z-index: 30" width="1" height="1"><path d="M 300 150 L 460 40" stroke="%s" stroke-width="10" stroke-linecap="round" opacity="0.55"></path><path d="M 300 150 L 460 40" stroke="%s" stroke-width="4" stroke-linecap="round"></path></svg>' % (BRASS_LIGHT, FLAME_CORE)
    for i in range(6): inner += spark(300 + i * 27, 150 - i * 18, 3 + (i % 2), 0.9, 41, FLAME_CORE)
    inner += stamp(300 - ox + 90, 964 - oy - 170, '放出 6', size=18)
    s += cell(1004, R1, cw, ch, base, '③ 放出 (灯の矢・光の奔流・眩光の大放出)', inner, '炎が硝子から抜けて<b>光の筋</b> (斬撃の筋の白〜真鍮版) になり、対象の敵へ飛んで着弾＝灯の数だけヒット (2026-09-20 裁定「灯1につき1ヒット」)。灯籠は暗くなり数字は 0。判「放出 6」。硝子の数字は 6→0 へ 0.4秒で減る (炎が吸われる)。全体放出は筋が全員へ扇に広がる')
    # ④ 灯が足りない
    inner = chip_hide + E() + L(1, shake=-4, bad=True)
    inner += '<div class="abs" style="left: %dpx; top: %dpx; transform: translateX(-50%%); z-index: 50">%s</div>' % (300 - ox, 964 - oy - 185, pill('灯が足りない (あと1)', BAD_INK, '#fff3ea', 14, 26))
    s += cell(24, R2, cw, ch, base, '④ 灯が足りない札を押した', inner, '灯コスト・灯N以上の札は今までどおり扇の中で 16px 沈む (出せない札)。押すと札が首を振り、<b>灯籠も首を振って硝子が朱に一瞬光り</b>「灯が足りない (あと1)」＝エナジー不足で輪が朱に光るのと対 (BattleScreen.CannotPlay)')
    # ⑤ 手札をつかんだ時の予告
    inner = chip_hide + E() + L(6, preview=('−2 → 4', BRASS_INK, BRASS_LIGHT))
    inner += '<div class="abs" style="left: 392px; top: 8px; width: 60px; height: 80px; border-radius: 4px; background: %s; box-shadow: 0 0 0 3px %s; z-index: 20; overflow: hidden"><img class="px" src="%s" style="width: 60px; height: 36px; display: block"><div style="font-size: 9px; text-align: center; padding-top: 4px">点灯の合図</div></div>' % (PAPER, BRASS, CARD_ART['white_march_order'])
    s += cell(514, R2, cw, ch, base, '⑤ 札をつかむと灯籠に予告', inner, '灯コストの札をつかむ (ホバー／ドラッグ) と灯籠の上に「−2 → 4」。「灯5でさらに」の札なら「5以上 ✓」(苔) か「5以上 ✗ いま4」(朱)。放出の札なら「6ヒット」。コスト玉が割引で緑になるのと同じ「実際に払う量を出す」規約')
    # ⑥ 段階
    inner = '<div class="abs" style="left: 0; top: 0; right: 0; bottom: 0; background: linear-gradient(#20233a, #0f1120)"></div>'
    for i, n in enumerate([0, 1, 3, 6, 12]):
        inner += lantern(50 + i * 90, 226, 2, n, tag=False)
        inner += '<div class="abs" style="left: %dpx; top: 232px; width: 90px; text-align: center; font-size: 12px; color: %s">%s<br><span style="color: %s">%s</span></div>' % (5 + i * 90, PAPER, TIER[tier_of(n)][1], PAPER_DIM, ['灯 0', '灯 1〜2', '灯 3〜5', '灯 6〜9', '灯 10〜'][i])
    s += cell(1004, R2, cw, ch, 'background: #20233a', '⑥ 炎の段階 (灯の量)', inner, '0＝硝子が暗く枠も沈む (数字は薄墨)。1〜2 ともる。3〜5 灯る＝足元に光溜まり。6〜9 盛る＝火の粉が舞う。10〜 眩い＝芯が白く光溜まりが手札の縁まで届く。段は「典型の灯6」と号令 2・火種 3・鍛冶 4・灯5でさらに、のしきい値に沿う')
    # ⑦ タップ = 説明
    inner = chip_hide + E() + L(6)
    inner += sheet(150, 12, 300, 128, PAPER3, z=40)
    inner += '<div class="abs deco" style="left: 12px; top: 8px; font-size: 16px">灯 <span style="font-size: 12px; font-weight: 400; color: %s">ともしび・白の資源</span></div>' % INK_SOFT
    inner += '<div class="abs" style="left: 12px; top: 34px; width: 276px; font-size: 12px; line-height: 17px">回復するたび +1・灯匠ひなたは毎ターン +1。号令 (灯コスト) と放出 (灯1につき1ヒット) で使う。戦闘の間ずっと残る<br><span style="color: %s">この戦闘の入り: 灯匠 +1／灯芯の人形 +1／回復 +2 ＝ 4/T</span></div>' % BRASS_INK
    inner += '</div>'
    s += cell(24, R3, cw, ch, base, '⑦ 灯籠をタップ＝説明と収支', inner, '名札「灯」か灯籠を押すと画面左上の固定パネル (Tooltip.ShowPinned) に用語の説明 (KeywordHelp「灯」) と、この戦闘の入りの内訳 (灯匠・灯芯・回復＝置物と灯芯を数えて予告)。号令の実値 (rallyPreview) は札の側に出る')
    # ⑧ 灯0 の姿 (白のリーダー)
    inner = chip_hide + E() + L(0, sparks=False)
    s += cell(514, R3, cw, ch, base, '⑧ 灯0 (戦闘開始・放出の直後)', inner, '戦闘開始 (灯0): 灯籠は消えているが場所は取る＝「ここに灯が溜まる」の予告。白のリーダーは灯0でも灯籠を出す (消灯)。T1 のターン開始で灯匠の +1 が飛び込んで初めて点く＝毎戦「点ける」瞬間がある。他色のリーダーは灯1以上で初めて現れる (小さく)')
    # ⑨ 音
    inner = '<div class="abs" style="left: 0; top: 0; right: 0; bottom: 0; background: linear-gradient(#20233a, #0f1120)"></div>'
    rows = [('灯を得る', '小さな鈴 (heal の系・高め)。既存 audio.json の LightGained は無い＝足す'), ('号令 (払う)', '真鍮の粒の飛び (EnergyPaid と同じ音)'), ('放出', '炎が抜ける吹き音 → 着弾は攻撃の当たり (既存)'), ('灯が足りない', '首振り (CannotPlay と同じ)'), ('点灯 (灯籠が点く)', 'ターン開始の +1 で 0→1 になった時だけ、小さな着火音')]
    for i, (a, b) in enumerate(rows):
        inner += '<div class="abs deco" style="left: 16px; top: %dpx; font-size: 14px; color: %s">%s</div><div class="abs" style="left: 150px; top: %dpx; width: 300px; font-size: 12px; line-height: 16px; color: %s">%s</div>' % (22 + i * 52, PAPER, a, 22 + i * 52, PAPER_DIM, b)
    s += cell(1004, R3, cw, ch, 'background: #20233a', '⑨ 音 (audio.json の表に足す)', inner, '演出の器は既存 (Tween.Projectile／Float／Stamp／Punch／SlashFx の筋)。新しい絵は灯籠のドット1枚 (32×48) と炎 (コード＝ThemeFx.Glow の重ね)。差し替え口 Art/ui/lantern.png')
    s += caption(24, 1306, '数値の出所は engine の同じ式 (Light・LightCost・MinLight・dischargeLight の実値)。表示だけを足し、ゴールデンは不変')
    return s + '</div>'

# ================================================================ 比較と原則
def board_compare():
    rows = [
        ('形', 'エナジーと同じ紙の円盤＋6分割の目盛り', '真鍮の灯籠 (32×48 ドット)。硝子に数字・炎が育つ', '真鍮の横木に炎の列 (8本＋「+N」)', '13px のピル「灯 6」'),
        ('一目の読み', '「資源が2つ」は読めるが、灯とエナジーが似て見える', '「ひなたの灯り」。輪と灯籠で形が違う＝混ざらない', '本数が数えられる。払う本数がそのまま消える', '状態異常の列に紛れる'),
        ('量の見え方', '目盛り 6 で満ちる (7以上は数字だけ)＝上限が無い資源に「満ちる」を言う', '炎の段階 (0/1〜2/3〜5/6〜9/10〜)＝溜めるほど明るい。上限を言わない', '本数そのもの。10 超で「+N」', '数字だけ'),
        ('演出の的', '粒の飛び先・浮き文字・不足の朱＝輪と同じ', '同じ＋炎の膨らみ・放出で炎が抜けて光の筋になる', '炎が1本ずつ点く／消える。放出は全部飛ぶ', '無い (エナジーの輪の上に出ていた)'),
        ('しきい値の予告', '目盛りに印 (5本目に苔)', '灯籠の上に札「−2 → 4」「5以上 ✓」', '本に朱の枠「−2」＝最も自然', '無い'),
        ('スマホ', '輪を 108 に縮めて2つ並べる', '輪はそのまま左へ寄せ、灯籠 80×120 を右に', '自分の札と輪の隙間 (65px) に横木', '札の中'),
        ('絵の投資', '無し (コードの輪)', '灯籠のドット1枚 (PixelLab)。それまでコード生成の枠', '炎の小さなドット (コードでも可)', '—'),
        ('実装', 'BuildEndTurn に輪をもう1つ・anchor "light"', '同＋灯籠の Image と炎の段階・Presenter の的を "light" へ', '横木の部品・本数の増減の演出', '—'),
    ]
    s = '<div style="position: relative; width: 1160px; height: 1010px; overflow: hidden; background: %s; color: %s">' % (PAPER, INK)
    s += '<div class="abs deco" style="left: 30px; top: 18px; font-size: 24px">灯の表示 — 3案の比較と原則</div>'
    cols = [(30, 140), (170, 250), (420, 270), (690, 250), (940, 200)]
    heads = ['', 'A 双子の輪', 'B 真鍮のランタン (推奨)', 'C 灯の列', '現状 ピル']
    y = 64
    for j, h in enumerate(heads):
        s += '<div class="abs deco" style="left: %dpx; top: %dpx; width: %dpx; font-size: 16px; border-bottom: 1.5px solid %s; padding-bottom: 6px">%s</div>' % (cols[j][0], y, cols[j][1] - 10, INK, h)
    y += 40
    for r in rows:
        for j, cellt in enumerate(r):
            s += '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; font-size: 13.5px; line-height: 18px; %s">%s</div>' % (cols[j][0], y, cols[j][1] - 10, 'color: %s' % INK_SOFT if j == 0 else '', cellt)
        y += 58
    y += 18
    s += '<div class="abs deco" style="left: 30px; top: %dpx; font-size: 18px">灯の表示の原則 (どの案でも)</div>' % y
    rules = [
        '① 灯は「あなたの資源」＝真鍮の系 (docs/color-theme.md 規律1)。炎と光は 真鍮の紙 → 白 (延焼の橙は使わない)。エナジーの輪と同じ段の器で、下端を揃えて左下に並べる',
        '② 数字は実処理と同じ値 (Player.Light)。しきい値・コストの予告も engine の同じ式 (LightCost・MinLight・dischargeLight の実値)。表示だけを足し、ゴールデンは不変',
        '③ 資源の札のピル「灯 N」は消す (同じ物を2か所に描かない)。他の資源 (成長・勢い・霊気・状態異常) は今までどおり札の中',
        '④ 白の色を持つリーダー (ひなた・なぎ・あかり・あさひ) は灯0 でも器を出す (消灯)。他のリーダーは灯1以上で現れる',
        '⑤ 灯の出来事 (LightGained／LightSpent／LightDischarged) の的は灯の器 (anchor "light")。粒は出どころから器へ・器から札や敵へ',
        '⑥ 器はタップの的＝用語の説明 (KeywordHelp「灯」) と、この戦闘の入りの内訳',
        '⑦ 「エナジーくらいリッチ」の物差し: 大きさ (128px 級)・数字 (40px 級)・反応 (得る／払う／放出／不足の4つに絵がある)・予告 (つかんだ札の実値)。この4つを満たして初めて釣り合う',
    ]
    for i, r in enumerate(rules):
        s += '<div class="abs" style="left: 30px; top: %dpx; width: 1100px; font-size: 13.5px; line-height: 19px">%s</div>' % (y + 30 + i * 24, r)
    y += 30 + len(rules) * 24 + 12
    s += '<div class="abs" style="left: 30px; top: %dpx; width: 1100px; background: %s; box-shadow: 0 0 0 1.5px %s; border-radius: 8px; padding: 10px 14px; font-size: 13.5px; line-height: 19px; box-sizing: border-box"><b>推奨は B</b>。灯は「ひなたの灯り」であり上限の無い蓄積なので、輪 (割合の器) より灯籠 (量で炎が育つ器) が意味に合う。エナジーの輪と形が違うことで「資源が2つ」が混ざらずに読め、放出＝炎が抜けて光の筋になる、という白の山場が絵になる。<br>A は最も安く釣り合うが「満ちる」が嘘になる。C は払う本数が見えて良いが薄く、「リッチ」の物差し (大きさ・数字) を満たさない。C の「本に印」は B の予告の札で代える。<br><b>絵</b>: 灯籠は PixelLab で 32×48 (正面・黒鉄の枠と真鍮の飾り・暗い硝子の窓・台座。ひなたの竿の灯籠と同じ意匠)。炎と光溜まり・火の粉はコード (ThemeFx.Glow の重ね＋Tween)。差し替え口 Art/ui/lantern.png。</div>' % (y, PAPER3, INK)
    return s + '</div>'

if __name__ == '__main__':
    write('Current', board_current_pc())
    write('CurrentPhone', board_current_ph())
    write('A', board_a_pc())
    write('Main', board_b_pc())
    write('C', board_c_pc())
    write('Phone', board_phone())
    write('Moments', board_moments())
    write('Compare', board_compare())
    canvas = {
        'artboards': [
            {'file': 'Current.dc.html', 'x': 0, 'y': 0, 'w': W, 'h': H, 'title': '現状 PC (灯＝資源の札のピル)'},
            {'file': 'CurrentPhone.dc.html', 'x': 0, 'y': 1180, 'w': PW, 'h': PH, 'title': '現状 スマホ'},
            {'file': 'A.dc.html', 'x': 2020, 'y': 0, 'w': W, 'h': H, 'title': '案A 双子の輪'},
            {'file': 'Main.dc.html', 'x': 2020, 'y': 1180, 'w': W, 'h': H, 'title': '案B 真鍮のランタン (推奨)'},
            {'file': 'C.dc.html', 'x': 2020, 'y': 2360, 'w': W, 'h': H, 'title': '案C 灯の列'},
            {'file': 'Phone.dc.html', 'x': 4040, 'y': 0, 'w': 2140, 'h': 1120, 'title': 'スマホの置き場 (3案)'},
            {'file': 'Moments.dc.html', 'x': 4040, 'y': 1220, 'w': 1540, 'h': 1330, 'title': '演出と情報 (案B の上で)'},
            {'file': 'Compare.dc.html', 'x': 5680, 'y': 1220, 'w': 1160, 'h': 1010, 'title': '比較と原則'},
        ],
        'annotations': [
            {'id': 'brief', 'x': 0, 'y': -170, 'w': 1300, 'text': '灯の表示 (2026-09-20 ユーザー「ひなたの戦闘画面に灯表記をエナジーくらいリッチに表現してほしい」)\n灯 (白の蓄積資源・2026-09-20 の再設計で白の芯になった) は、いま資源の札の 13px のピル「灯 6」でしかない。エナジーの輪 (紙の円盤＋真鍮の弧＋40px の数字＋粒の飛び・不足の朱) と釣り合う器を3案。\n下地は実機 (ひなた・灯6・エナジー3/3・灯芯の人形と剣の人形・探り屋の二人組)。ランタンの絵は仮 (灯の矢の挿絵の切り出し)。\n決めること: ①形 (A 双子の輪／B 真鍮のランタン／C 灯の列) ②スマホの置き場 (輪を左へ寄せて右に／輪を縮めて2つ／隙間に横木) ③白以外のリーダーでの扱い (灯1以上で出す／出さない) ④演出の範囲 (得る・払う・放出・不足の4つ＋予告＋タップの説明)\n裁定 (ask_user 4件・2026-09-20): ①B 真鍮のランタン ②輪を左へ寄せ右に灯籠 ③灯1以上で出す (白は常時) ④全部 → 同日 Unity に実装 (LightUi.cs・Presenter の LightGained/LightSpent/LightDischarged・ThemeFx.Lantern/Flame/LightStreak・LightGained.sourceUid)。絵はコード生成、差し替え口 Art/ui/lantern.png (docs/pixellab-assets.md)'},
        ],
        'launch': {'view': 'canvas'},
    }
    with open(os.path.join(OUT, 'canvas.json'), 'w', encoding='utf-8') as f: json.dump(canvas, f, ensure_ascii=False, indent=1)
    print('ok')

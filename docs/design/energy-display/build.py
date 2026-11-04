# build.py — エナジーの表記の見直し (2026-11-04 ユーザー「エナジーの表記を /design でリッチにかつわかりやすくしたい」)。
# 今 = 紙の円盤に真鍮の弧 (分母で目盛り)・数字 40・「/ 3」15・超えた分は輪の外の小玉・予告の札「次のターン 上限 N」(BattleScreen.cs 4140〜4275)。
# 案 4 つ (A 真鍮の歯車の器／B 硝子の燃料瓶の棚／C 大きな数字と玉の帯／D からくりの圧力計) を、同じ 6 つの状態で描く:
#   ①満タン 3/3 ②払った後 1/3 ③0 ④一時マナで上限超え 5/3 ⑤次のターンの上限 +1 の予告 ⑥札を持ち上げた時の払う見込み (2E で 3→1・3E で残り 1＝足りない)。
# 各案に PC (左下の定位置・実寸) とスマホ (1.28 倍＝S25 相当 1920×886 の実測) の配置を 1 枚ずつ、灯籠 (白のリーダー) が右に付く 1 枚、比較 (Main)。
# 下地は Unity のスクショ (E-base-1 など) の輪と予告の札を、UI を消した同じ場面 (H-hibana-hideui-1) で塗りつぶしたもの。
# 色は PaperFx.cs の値 (真鍮＝資源・夜の札)。字は Noto Sans JP (Font A)。
# 使い方: python3 build.py → *.dc.html と canvas.json → seed-canvas.mjs で束ねる (README.md)。
import base64, math, os, shutil
OUT = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(OUT, '..', '..', '..'))

# ---- PaperFx の色 ----
PAPER = '#f4ecd6'; PAPER2 = '#eadfc4'; PAPER3 = '#fbf6e8'; PAPER_DIM = '#c4beb2'
INK = '#2f2e35'; INK_SOFT = '#4e4c55'
NIGHT = '#1a1c33'; WINDOW = '#20233a'; GROUND = '#0f1120'; NIGHT_EDGE = '#8a879c'; NIGHT_PILL = '#2a2d48'; NIGHT_TRACK = '#31344f'
BRASS = '#c99a3a'; BRASS_LIGHT = '#ead08a'; BRASS_INK = '#634410'; NIGHT_BRASS = '#3d3214'
ROSE = '#c9635a'; BAD_INK = '#9c3a2a'; NIGHT_BAD_INK = '#f2a193'; NIGHT_ROSE = '#3d2028'
EMBER = '#e8742f'; GOOD_INK = '#276a34'; NIGHT_GOOD_INK = '#a6d99a'
CARD_SHELL = '#223442'; CARD_BODY = '#2c3d4b'; CARD_DIV = '#5c7280'; CARD_INK = '#f6f1d9'; CARD_COST_UP = '#ffb6aa'; FACTION_GREEN = '#326447'

# ---- 下地 (スクショ) の準備: 元が scratchpad にある時だけ作り直す。docs に置いた画像が正 ----
SRC = os.environ.get('ENERGY_SRC', '/tmp/claude-1000/-home-yosuke-projects-deck-rogue-proto/31d623d9-6b03-4c3d-b43c-8579b601c36c/scratchpad/fb')
PC_ERASE = (0, 822, 304, 1012)      # 輪と予告の札 (山札は残す)
PH_ERASE = (0, 554, 258, 784)
CARD_BOX = (862, 796, 1058, 1080)   # 真ん中の「防御」の札 (持ち上げの見本)
def prep():
    if not os.path.isdir(SRC): return
    from PIL import Image
    import numpy as np
    def load(p): return Image.open(os.path.join(SRC, p)).convert('RGB')
    def patch(base, hide, box, strip, const_rows=None):
        """UI を消した同じ場面で塗る。手札の下の帯の明るさ (行ごとの差) を右隣の細い帯から足し、縁は 12px でぼかす"""
        a = np.asarray(base).astype(float); b = np.asarray(hide).astype(float)
        x0, y0, x1, y1 = box; sx0, sx1 = strip
        if const_rows:   # 隣の帯に札の縁が掛かる時は、決まった行だけで測った一定の差
            r0, r1 = const_rows
            off = np.tile(np.median((a[r0:r1, sx0:sx1] - b[r0:r1, sx0:sx1]).reshape(-1, 3), axis=0), (y1 - y0, 1))
        else:
            off = (a[y0:y1, sx0:sx1] - b[y0:y1, sx0:sx1]).mean(axis=1)            # 行ごとの差 (3 色)
            k = np.ones(21) / 21.0
            off = np.stack([np.convolve(np.pad(off[:, c], 10, mode='edge'), k, 'valid') for c in range(3)], 1)
        fill = b[y0:y1, x0:x1] + off[:, None, :]
        h, w = y1 - y0, x1 - x0
        yy, xx = np.mgrid[0:h, 0:w]
        f = 12.0
        m = np.clip(np.minimum.reduce([(xx + 1) / f if x0 > 0 else np.ones_like(xx, float), (w - xx) / f, (yy + 1) / f, (h - yy) / f]), 0, 1)[..., None]
        a[y0:y1, x0:x1] = a[y0:y1, x0:x1] * (1 - m) + fill * m
        return Image.fromarray(np.clip(a, 0, 255).astype('uint8'))
    pc, pch = load('pc/E-base-1.png'), load('pc/H-hibana-hideui-1.png')
    ph, phh = load('ph/E-base-1.png'), load('ph/H-hibana-hideui-1.png')
    b = patch(pc, pch, PC_ERASE, (306, 336)); b.save(os.path.join(OUT, 'base-pc.jpg'), quality=60)
    card = b.crop(CARD_BOX); card.save(os.path.join(OUT, 'card-pc.png'))
    nc = patch(b, pch, CARD_BOX, (306, 336)); nc.save(os.path.join(OUT, 'base-pc-lift.jpg'), quality=60)
    c = patch(ph, phh, PH_ERASE, (256, 260), (700, 784)); c.save(os.path.join(OUT, 'base-ph.jpg'), quality=60)
    # 状態の枠の下地 (UI の無い舞台の左下)
    st = patch(pch, pch, (0, 0, 1, 1), (0, 1)).crop((0, 680, 560, 1080)); st.save(os.path.join(OUT, 'stage-pc.jpg'), quality=60)
    # 今の見た目 (左下の切り抜き 2 倍)
    for name, src in [('cur-full', 'pc/E-base-1.png'), ('cur-low', 'pc/E-base-low-1.png'), ('cur-over', 'pc/E-hibana-blaze-1.png'), ('cur-ph', 'ph/E-base-low-1.png')]:
        im = load(src)
        box = (0, 800, 470, 1080) if not name.endswith('ph') else (0, 540, 470, 886)
        im = im.crop(box); im = im.resize((im.width * 2, im.height * 2), Image.LANCZOS)
        im.save(os.path.join(OUT, name + '.jpg'), quality=70)
prep()

# ================================================================ SVG の部品
DEFS = '''<svg width="0" height="0" style="position:absolute"><defs>
 <radialGradient id="jw" cx=".38" cy=".34" r=".7"><stop offset="0" stop-color="#fffbe6"/><stop offset=".35" stop-color="#f6d27a"/><stop offset=".75" stop-color="#c99a3a"/><stop offset="1" stop-color="#7a5418"/></radialGradient>
 <radialGradient id="jwHot" cx=".4" cy=".36" r=".7"><stop offset="0" stop-color="#ffffff"/><stop offset=".45" stop-color="#fff0b8"/><stop offset="1" stop-color="#e0b04a"/></radialGradient>
 <radialGradient id="jwOver" cx=".38" cy=".34" r=".7"><stop offset="0" stop-color="#fff4e0"/><stop offset=".4" stop-color="#ffc46a"/><stop offset=".85" stop-color="#e8742f"/><stop offset="1" stop-color="#8a3a14"/></radialGradient>
 <radialGradient id="jwEmpty" cx=".5" cy=".6" r=".6"><stop offset="0" stop-color="#2a2533"/><stop offset="1" stop-color="#110f18"/></radialGradient>
 <linearGradient id="brass" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#ecc777"/><stop offset=".5" stop-color="#bf8f33"/><stop offset="1" stop-color="#6e4e16"/></linearGradient>
 <linearGradient id="brassDark" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#8a6420"/><stop offset="1" stop-color="#3d2a0a"/></linearGradient>
 <linearGradient id="liquid" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#ffeaa8"/><stop offset=".5" stop-color="#e9bf5c"/><stop offset="1" stop-color="#a67a26"/></linearGradient>
 <linearGradient id="liquidHot" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#ffffff"/><stop offset="1" stop-color="#f3d27c"/></linearGradient>
 <linearGradient id="liquidOver" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#ffe0b0"/><stop offset=".6" stop-color="#f09a4a"/><stop offset="1" stop-color="#b4521c"/></linearGradient>
 <linearGradient id="glass" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="#2b2f4a"/><stop offset=".3" stop-color="#3a3f5e"/><stop offset="1" stop-color="#1c1f33"/></linearGradient>
 <radialGradient id="face" cx=".5" cy=".42" r=".6"><stop offset="0" stop-color="#fbf6e8"/><stop offset=".8" stop-color="#f4ecd6"/><stop offset="1" stop-color="#d9cba6"/></radialGradient>
 <radialGradient id="flame" cx=".5" cy=".7" r=".6"><stop offset="0" stop-color="#fffbe6"/><stop offset=".45" stop-color="#ffd27a"/><stop offset="1" stop-color="#e8742f" stop-opacity="0"/></radialGradient>
 <filter id="glow" x="-50%" y="-50%" width="200%" height="200%"><feGaussianBlur stdDeviation="2.6" result="b"/><feMerge><feMergeNode in="b"/><feMergeNode in="SourceGraphic"/></feMerge></filter>
 <filter id="glowBig" x="-80%" y="-80%" width="260%" height="260%"><feGaussianBlur stdDeviation="5" result="b"/><feMerge><feMergeNode in="b"/><feMergeNode in="b"/><feMergeNode in="SourceGraphic"/></feMerge></filter>
 <filter id="drop" x="-20%" y="-20%" width="140%" height="150%"><feDropShadow dx="0" dy="3" stdDeviation="3" flood-color="#000" flood-opacity=".55"/></filter>
 <pattern id="hatch" width="6" height="6" patternUnits="userSpaceOnUse" patternTransform="rotate(45)"><rect width="6" height="6" fill="#ead08a" fill-opacity=".35"/><rect width="2.4" height="6" fill="#ead08a" fill-opacity=".9"/></pattern>
 <pattern id="hatchRose" width="6" height="6" patternUnits="userSpaceOnUse" patternTransform="rotate(45)"><rect width="6" height="6" fill="#c9635a" fill-opacity=".25"/><rect width="2.4" height="6" fill="#c9635a" fill-opacity=".9"/></pattern>
</defs></svg>'''

def num_text(x, y, s, size, fill, weight=900, anchor='middle', extra=''):
    return '<text x="%.1f" y="%.1f" font-size="%d" font-weight="%d" fill="%s" text-anchor="%s" font-family="Noto Sans JP, Noto Sans CJK JP, sans-serif" %s>%s</text>' % (x, y, size, weight, fill, anchor, extra, s)

def cog_path(cx, cy, ro, rr, teeth, rot=0.0):
    pts = []
    half = math.pi / teeth
    for i in range(teeth):
        b = 2 * math.pi * i / teeth + rot
        for r, a in [(rr, b - half * 0.92), (ro, b - half * 0.5), (ro, b + half * 0.5), (rr, b + half * 0.92)]:
            pts.append('%.1f,%.1f' % (cx + r * math.cos(a), cy + r * math.sin(a)))
    return 'M' + ' L'.join(pts) + ' Z'

def chip(x, y, text, kind='brass', size=14, anchor='left'):
    """夜の札 (舞台の上に出る小札)。kind: brass=見込み・予告 / bad=足りない"""
    col = {'brass': (NIGHT, BRASS_LIGHT, BRASS), 'bad': (NIGHT_ROSE, NIGHT_BAD_INK, ROSE), 'over': (NIGHT, '#ffc46a', EMBER)}[kind]
    tr = 'translateX(-50%)' if anchor == 'center' else ('translateX(-100%)' if anchor == 'right' else 'none')
    return ('<div style="position:absolute; left:%dpx; top:%dpx; transform:%s; background:%s; color:%s; box-shadow:0 0 0 1.5px %s, 0 3px 8px rgba(0,0,0,.5); '
            'border-radius:6px; padding:2px 9px; font-size:%dpx; line-height:%dpx; font-weight:700; white-space:nowrap; letter-spacing:.02em">%s</div>') % (x, y, tr, col[0], col[1], col[2], size, size + 8, text)

# ================================================================ 案A 真鍮の歯車の器
def comp_A(st, phone=False):
    e, m, nxt, pay = st['e'], st['m'], st.get('nxt'), st.get('pay')
    W, H = 260, 236
    cx, cy, ro, rr = 86, 112, 80, 71
    ghost = max(0, (nxt or 0) - m)
    n = m + ghost
    zero = e <= 0
    s = '<svg width="%d" height="%d" viewBox="0 0 %d %d" style="position:absolute; left:0; top:0; overflow:visible">' % (W, H, W, H)
    g = 'filter="url(#drop)"' + (' style="filter: saturate(.35) brightness(.8)"' if zero else '')
    s += '<g %s>' % g
    # 一時マナの小さな歯車 (噛み合う)
    over = max(0, e - m)
    if over:
        sx, sy = cx + 88 * math.cos(math.radians(-38)), cy + 88 * math.sin(math.radians(-38))
        s += '<path d="%s" fill="url(#brass)" stroke="%s" stroke-width="1.5"/>' % (cog_path(sx, sy, 32, 26, 9, 0.2), BRASS_INK)
        s += '<circle cx="%.1f" cy="%.1f" r="22" fill="url(#jwOver)" stroke="%s" stroke-width="2" filter="url(#glow)"/>' % (sx, sy, '#8a3a14')
        s += num_text(sx, sy + 8, '+%d' % over, 22, '#3a1606', 900)
    # 本体の歯車
    s += '<path d="%s" fill="url(#brass)" stroke="%s" stroke-width="2"/>' % (cog_path(cx, cy, ro, rr, 16, math.pi / 16), BRASS_INK)
    s += '<circle cx="%d" cy="%d" r="66" fill="url(#brassDark)" stroke="%s" stroke-width="1.5"/>' % (cx, cy, BRASS)
    s += '</g>'
    # 受け口 (エナジーの数だけ)
    fill_n = min(e, m)
    pay_ok = pay is not None and pay <= e
    short = pay is not None and pay > e
    for k in range(n):
        a = math.radians(-90 + 360.0 * k / n)
        x, y = cx + 54 * math.cos(a), cy + 54 * math.sin(a)
        if k >= m:   # 次のターンに増える受け口
            s += '<circle cx="%.1f" cy="%.1f" r="11.5" fill="#14131f" stroke="%s" stroke-width="2" stroke-dasharray="3.5 3"/>' % (x, y, BRASS_LIGHT)
            s += num_text(x, y + 5, '+1', 13, BRASS_LIGHT, 700)
            continue
        filled = k < fill_n
        hot = pay_ok and fill_n - pay <= k < fill_n
        if filled:
            grad = 'jwHot' if hot else 'jw'
            stroke = '#ffffff' if hot else (ROSE if short else '#7a5418')
            s += '<circle cx="%.1f" cy="%.1f" r="12" fill="url(#%s)" stroke="%s" stroke-width="%s" filter="url(#glow)"/>' % (x, y, grad, stroke, '3' if (hot or short) else '1.5')
            if hot: s += '<circle cx="%.1f" cy="%.1f" r="17" fill="none" stroke="#fff3c4" stroke-width="2" stroke-opacity=".8"/>' % (x, y)
        else:
            s += '<circle cx="%.1f" cy="%.1f" r="11.5" fill="url(#jwEmpty)" stroke="%s" stroke-width="1.5"/>' % (x, y, BRASS_INK)
            if short and k < pay:
                s += '<circle cx="%.1f" cy="%.1f" r="12" fill="url(#hatchRose)" stroke="%s" stroke-width="2.5" stroke-dasharray="4 3"/>' % (x, y, ROSE)
    # 軸 (数字)
    s += '<circle cx="%d" cy="%d" r="37" fill="%s" stroke="%s" stroke-width="2.5"/>' % (cx, cy, NIGHT, BRASS)
    numcol = PAPER_DIM if zero else (NIGHT_BAD_INK if short else ('#ffc46a' if over else PAPER))
    s += num_text(cx, cy + 13, str(e), 44, numcol, 900)
    s += num_text(cx, cy + 30, '/ %d' % m, 13, PAPER_DIM, 700)
    s += '</svg>'
    # 名札
    s += ('<div style="position:absolute; left:%dpx; top:%dpx; transform:translateX(-50%%); background:linear-gradient(#ecc777,#c99a3a); color:%s; box-shadow:0 0 0 1.5px %s, 0 3px 6px rgba(0,0,0,.45); '
          'border-radius:5px; padding:0 12px; font-size:14px; line-height:22px; font-weight:700; letter-spacing:.2em">エナジー</div>') % (cx, cy + ro - 6, BRASS_INK, BRASS_INK)
    s += extra_chips(st, (0, cy - ro - 30) if phone else (cx + ro + 8, cy - ro + 6), cy + ro + 24)
    LAST['yb'] = cy + ro + 24 + 28; LAST['body'] = cy + ro
    return s, W, H

LAST = {}
def extra_chips(st, pay_xy, ybot, left=0):
    """見込みの札 (上) と予告の札 (下)。どの案も同じ文言・同じ場所"""
    s = ''
    e, pay, nxt, m = st['e'], st.get('pay'), st.get('nxt'), st['m']
    if pay is not None:
        if pay <= e: s += chip(pay_xy[0], pay_xy[1], '使うと %d → <b style="font-size:17px">%d</b>' % (e, e - pay), 'brass')
        else: s += chip(pay_xy[0], pay_xy[1], 'エナジーが %d 足りない' % (pay - e), 'bad')
    if nxt and nxt > m:
        s += chip(left, ybot, '次のターン 上限 %d' % nxt, 'brass', 13)
    return s

# ================================================================ 案B 硝子の燃料瓶の棚
def vial(x, y, state, w=22, h=50, tilt=0):
    """state: full / empty / hot / over / ghost / short (足りない空き)"""
    neck = 8
    s = '<g transform="rotate(%d %.1f %.1f)">' % (tilt, x + w / 2, y + h)
    if state == 'ghost':
        s += '<rect x="%.1f" y="%.1f" width="%d" height="%d" rx="7" fill="none" stroke="%s" stroke-width="2" stroke-dasharray="4 3"/>' % (x, y + neck, w, h - neck, BRASS_LIGHT)
        s += num_text(x + w / 2, y + neck + (h - neck) / 2 + 5, '+1', 13, BRASS_LIGHT, 700)
        return s + '</g>'
    # 栓と首
    s += '<rect x="%.1f" y="%.1f" width="%.1f" height="%d" rx="2" fill="url(#brass)" stroke="%s" stroke-width="1"/>' % (x + w * 0.25, y, w * 0.5, neck, BRASS_INK)
    # 硝子
    s += '<rect x="%.1f" y="%.1f" width="%d" height="%d" rx="7" fill="url(#glass)" stroke="%s" stroke-width="1.5"/>' % (x, y + neck, w, h - neck, '#9aa0b8')
    liq = {'full': 'liquid', 'hot': 'liquidHot', 'over': 'liquidOver'}.get(state)
    if liq:
        s += '<rect x="%.1f" y="%.1f" width="%d" height="%d" rx="5" fill="url(#%s)" filter="url(#glow)"/>' % (x + 2.5, y + neck + 6, w - 5, h - neck - 8.5, liq)
        s += '<rect x="%.1f" y="%.1f" width="3" height="%d" rx="1.5" fill="#fffbe6" fill-opacity=".85"/>' % (x + 5, y + neck + 9, h - neck - 18)
    else:
        s += '<rect x="%.1f" y="%.1f" width="3" height="%d" rx="1.5" fill="#9aa0b8" fill-opacity=".35"/>' % (x + 5, y + neck + 6, h - neck - 14)
    if state == 'hot':
        s += '<rect x="%.1f" y="%.1f" width="%d" height="%d" rx="9" fill="none" stroke="#ffffff" stroke-width="2.5"/>' % (x - 2.5, y + neck - 2.5, w + 5, h - neck + 5)
        s += '<path d="M%.1f %.1f l6 -8 l-12 0 z" fill="%s"/>' % (x + w / 2, y - 3, BRASS_LIGHT)
    if state == 'short':
        s += '<rect x="%.1f" y="%.1f" width="%d" height="%d" rx="7" fill="url(#hatchRose)" stroke="%s" stroke-width="2.5" stroke-dasharray="4 3"/>' % (x, y + neck, w, h - neck, ROSE)
    if state == 'fullshort':
        pass
    return s + '</g>'

def comp_B(st, phone=False):
    e, m, nxt, pay = st['e'], st['m'], st.get('nxt'), st.get('pay')
    per_row = 4 if phone else 6
    sp = 23 if phone else 30
    vw = 18 if phone else 22
    ghost = max(0, (nxt or 0) - m)
    slots = m + ghost
    rows = (slots + per_row - 1) // per_row
    over = max(0, e - m)
    x0 = 80 if phone else 92
    W = x0 + 4 + min(slots, per_row) * sp + 18 + (over * 22 + 10 if over else 0)
    rowh = 66
    H = 40 + rows * rowh + 40
    shelf_y0 = 40 + 50
    zero = e <= 0
    s = '<svg width="%d" height="%d" viewBox="0 0 %d %d" style="position:absolute; left:0; top:0; overflow:visible">' % (W, H, W, H)
    # 数字の徽章
    mx, my = (36 if phone else 42), 40 + rows * rowh - 30
    mr = 32 if phone else 38
    s += '<g filter="url(#drop)"%s>' % (' style="filter: saturate(.35) brightness(.8)"' if zero else '')
    s += '<circle cx="%d" cy="%d" r="%d" fill="url(#brass)" stroke="%s" stroke-width="2"/>' % (mx, my, mr, BRASS_INK)
    s += '<circle cx="%d" cy="%d" r="%d" fill="%s" stroke="%s" stroke-width="1.5"/>' % (mx, my, mr - 7, NIGHT, BRASS_INK)
    s += '</g>'
    short = pay is not None and pay > e
    pay_ok = pay is not None and pay <= e
    numcol = PAPER_DIM if zero else (NIGHT_BAD_INK if short else ('#ffc46a' if over else PAPER))
    s += num_text(mx, my + 12, str(e), 38, numcol, 900)
    s += num_text(mx, my + 27, '/ %d' % m, 13, PAPER_DIM, 700)
    # 棚 (行ごと)
    fill_n = min(e, m)
    for r in range(rows):
        cnt = min(per_row, slots - r * per_row)
        yb = 40 + (rows - 1 - r) * rowh + 54   # 下の段が 1 行目
        s += '<g filter="url(#drop)"><rect x="%d" y="%d" width="%d" height="7" rx="2" fill="url(#brass)" stroke="%s" stroke-width="1"/>' % (x0 - 6, yb, cnt * sp + 8, BRASS_INK)
        s += '<rect x="%d" y="%d" width="4" height="12" fill="%s"/><rect x="%d" y="%d" width="4" height="12" fill="%s"/></g>' % (x0 - 2, yb + 7, BRASS_INK, x0 + cnt * sp - 6, yb + 7, BRASS_INK)
        for i in range(cnt):
            k = r * per_row + i
            vx = x0 + i * sp
            if k >= m: stt = 'ghost'
            elif k < fill_n: stt = 'hot' if (pay_ok and fill_n - pay <= k) else 'full'
            else: stt = 'short' if (short and k < pay) else 'empty'
            s += vial(vx, yb - 52, stt, vw, 52)
            if short and k < fill_n:
                s += '<rect x="%.1f" y="%.1f" width="%d" height="%d" rx="8" fill="none" stroke="%s" stroke-width="2.5"/>' % (vx - 2, yb - 46, vw + 4, 48, ROSE)
    # 一時マナ＝棚の外に立てかけた瓶
    if over:
        ox = x0 + min(slots, per_row) * sp + 10
        yb = 40 + (rows - 1) * rowh + 54
        for i in range(over):
            s += vial(ox + i * 22, yb - 50, 'over', 18, 48, -8 + 10 * i)
        s += num_text(ox + over * 11, yb - 58, '一時 +%d' % over, 13, '#ffc46a', 700)
    s += '</svg>'
    s += ('<div style="position:absolute; left:%dpx; top:%dpx; transform:translateX(-50%%); background:linear-gradient(#ecc777,#c99a3a); color:%s; box-shadow:0 0 0 1.5px %s, 0 3px 6px rgba(0,0,0,.45); '
          'border-radius:5px; padding:0 9px; font-size:13px; line-height:20px; font-weight:700; letter-spacing:.12em">エナジー</div>') % (mx, my + 30, BRASS_INK, BRASS_INK)
    s += extra_chips(st, (0, -2), my + 58)
    LAST['yb'] = my + 58 + 28; LAST['body'] = my + mr
    return s, W, H

# ================================================================ 案C 大きな数字と玉の帯
def comp_C(st, phone=False):
    e, m, nxt, pay = st['e'], st['m'], st.get('nxt'), st.get('pay')
    ghost = max(0, (nxt or 0) - m)
    over = max(0, e - m)
    W, H = 200, 210
    cx, cy, r = 68, 92, 60
    zero = e <= 0
    short = pay is not None and pay > e
    pay_ok = pay is not None and pay <= e
    s = '<svg width="%d" height="%d" viewBox="0 0 %d %d" style="position:absolute; left:0; top:0; overflow:visible">' % (W, H, W, H)
    s += '<g filter="url(#drop)"%s>' % (' style="filter: saturate(.35) brightness(.85)"' if zero else '')
    s += '<circle cx="%d" cy="%d" r="%d" fill="url(#brass)" stroke="%s" stroke-width="2"/>' % (cx, cy, r + 4, BRASS_INK)
    s += '<circle cx="%d" cy="%d" r="%d" fill="url(#face)" stroke="%s" stroke-width="1.5"/>' % (cx, cy, r - 5, BRASS_INK)
    s += '</g>'
    numcol = '#8d877a' if zero else (BAD_INK if short else INK)
    s += num_text(cx, cy + 20, str(e), 60, numcol, 900)
    if pay_ok: s += num_text(cx, cy + 40, '使うと %d' % (e - pay), 14, BRASS_INK, 900)
    elif short: s += num_text(cx, cy + 40, '%d 足りない' % (pay - e), 13, BAD_INK, 700)
    else: s += num_text(cx, cy + 40, 'エナジー', 13, INK_SOFT, 700, extra='letter-spacing="2"')
    # 玉の帯
    slots = m + ghost
    pr, sp = 9, 23
    bw = slots * sp + (over * sp + 14 if over else 0) + 14
    by = cy + r + 12
    s += '<rect x="4" y="%d" width="%d" height="30" rx="8" fill="%s" stroke="%s" stroke-width="1.5" filter="url(#drop)"/>' % (by, bw, NIGHT, NIGHT_EDGE)
    fill_n = min(e, m)
    for k in range(slots):
        x, y = 4 + 7 + pr + k * sp, by + 15
        if k >= m:
            s += '<circle cx="%.1f" cy="%.1f" r="%d" fill="none" stroke="%s" stroke-width="2" stroke-dasharray="3 2.5"/>' % (x, y, pr, BRASS_LIGHT)
            continue
        if k < fill_n:
            hot = pay_ok and fill_n - pay <= k
            s += '<circle cx="%.1f" cy="%.1f" r="%d" fill="url(#%s)" stroke="%s" stroke-width="%s" filter="url(#glow)"/>' % (x, y, pr, 'jwHot' if hot else 'jw', '#ffffff' if hot else (ROSE if short else '#7a5418'), '2.5' if hot or short else '1.2')
        else:
            s += '<circle cx="%.1f" cy="%.1f" r="%d" fill="url(#jwEmpty)" stroke="%s" stroke-width="1.2"/>' % (x, y, pr, BRASS_INK)
            if short and k < pay: s += '<circle cx="%.1f" cy="%.1f" r="%d" fill="url(#hatchRose)" stroke="%s" stroke-width="2" stroke-dasharray="3 2.5"/>' % (x, y, pr, ROSE)
    if over:
        xd = 4 + 7 + slots * sp + 2
        s += '<rect x="%.1f" y="%d" width="2" height="20" fill="%s"/>' % (xd, by + 5, NIGHT_EDGE)
        for i in range(over):
            x = xd + 14 + pr + i * sp - 4
            s += '<circle cx="%.1f" cy="%d" r="%d" fill="url(#jwOver)" stroke="#8a3a14" stroke-width="1.2" filter="url(#glow)"/>' % (x, by + 15, pr)
    s += '</svg>'
    s += extra_chips(st, (0, -6) if phone else (cx + r + 10, cy - r + 4), by + 38)
    LAST['yb'] = by + 38 + 28; LAST['body'] = by + 30
    return s, W, H

# ================================================================ 案D からくりの圧力計
def comp_D(st, phone=False):
    e, m, nxt, pay = st['e'], st['m'], st.get('nxt'), st.get('pay')
    W, H = 200, 230
    cx, cy, R = 92, 98, 84
    tot = 2 * m
    def th(v): return math.radians(150 + 240.0 * v / tot)
    def pt(v, r): a = th(v); return cx + r * math.cos(a), cy + r * math.sin(a)
    zero = e <= 0
    short = pay is not None and pay > e
    pay_ok = pay is not None and pay <= e
    over = max(0, e - m)
    s = '<svg width="%d" height="%d" viewBox="0 0 %d %d" style="position:absolute; left:0; top:0; overflow:visible">' % (W, H, W, H)
    s += '<g filter="url(#drop)"%s>' % (' style="filter: saturate(.4) brightness(.85)"' if zero else '')
    s += '<circle cx="%d" cy="%d" r="%d" fill="url(#brass)" stroke="%s" stroke-width="2"/>' % (cx, cy, R, BRASS_INK)
    # ねじ 4 つ
    for a in [45, 135, 225, 315]:
        x, y = cx + (R - 4) * math.cos(math.radians(a)), cy + (R - 4) * math.sin(math.radians(a))
        s += '<circle cx="%.1f" cy="%.1f" r="3" fill="%s"/>' % (x, y, BRASS_INK)
    s += '<circle cx="%d" cy="%d" r="%d" fill="url(#face)" stroke="%s" stroke-width="1.5"/>' % (cx, cy, R - 9, BRASS_INK)
    s += '</g>'
    def arc(v0, v1, r0, r1, fill, extra=''):
        a0, a1 = th(v0), th(v1)
        large = 1 if (a1 - a0) > math.pi else 0
        p = 'M%.1f %.1f A%d %d 0 %d 1 %.1f %.1f L%.1f %.1f A%d %d 0 %d 0 %.1f %.1f Z' % (
            cx + r1 * math.cos(a0), cy + r1 * math.sin(a0), r1, r1, large, cx + r1 * math.cos(a1), cy + r1 * math.sin(a1),
            cx + r0 * math.cos(a1), cy + r0 * math.sin(a1), r0, r0, large, cx + r0 * math.cos(a0), cy + r0 * math.sin(a0))
        return '<path d="%s" fill="%s" %s/>' % (p, fill, extra)
    # 目盛りの帯: 0〜上限＝真鍮の帯 (残りの分だけ明るい)・上限〜＝一時 (熾)
    s += arc(0, m, 62, 70, '#e6d7b0')
    if e > 0: s += arc(0, min(e, m), 62, 70, BRASS)
    s += arc(m, tot, 62, 70, '#f3c9a8')
    if over: s += arc(m, min(e, tot), 62, 70, EMBER)
    if pay_ok: s += arc(e - pay, e, 40, 70, 'url(#hatch)', 'stroke="#ffffff" stroke-width="1.5"')
    if short:
        s += arc(0, e, 40, 70, 'none', 'stroke="%s" stroke-width="2.5"' % ROSE)
        s += arc(-(pay - e), 0, 40, 70, 'url(#hatchRose)', 'stroke="%s" stroke-width="2" stroke-dasharray="4 3"' % ROSE)
    for v in range(tot + 1):
        x0, y0 = pt(v, 60); x1, y1 = pt(v, 72)
        s += '<line x1="%.1f" y1="%.1f" x2="%.1f" y2="%.1f" stroke="%s" stroke-width="%s"/>' % (x0, y0, x1, y1, INK, '2.5' if v <= m else '1.5')
        if v <= m:
            tx, ty = pt(v, 48)
            s += num_text(tx, ty + 5, str(v), 14, INK, 700)
    tx, ty = pt(m + m / 2.0, 48)
    s += num_text(tx, ty + 5, '一時', 13, '#8a3a14', 700)
    # 針
    def needle(v, ghost=False):
        a = th(v)
        tip = (cx + 64 * math.cos(a), cy + 64 * math.sin(a))
        l = (cx + 6 * math.cos(a + math.pi / 2), cy + 6 * math.sin(a + math.pi / 2))
        r_ = (cx + 6 * math.cos(a - math.pi / 2), cy + 6 * math.sin(a - math.pi / 2))
        tail = (cx - 14 * math.cos(a), cy - 14 * math.sin(a))
        if ghost:
            return '<path d="M%.1f %.1f L%.1f %.1f L%.1f %.1f L%.1f %.1f Z" fill="none" stroke="%s" stroke-width="1.8" stroke-dasharray="4 3"/>' % (tip + l + tail + r_ + (INK_SOFT,))
        return '<path d="M%.1f %.1f L%.1f %.1f L%.1f %.1f L%.1f %.1f Z" fill="%s"/>' % (tip + l + tail + r_ + (BAD_INK if short else INK,))
    if pay_ok: s += needle(e - pay, True)
    s += needle(min(e, tot))
    s += '<circle cx="%d" cy="%d" r="8" fill="url(#brass)" stroke="%s" stroke-width="1.5"/>' % (cx, cy, BRASS_INK)
    # 数字の窓
    wy = cy + 26
    s += '<rect x="%d" y="%d" width="76" height="42" rx="6" fill="%s" stroke="%s" stroke-width="2"/>' % (cx - 38, wy, NIGHT, BRASS_INK)
    numcol = PAPER_DIM if zero else (NIGHT_BAD_INK if short else ('#ffc46a' if over else PAPER))
    s += num_text(cx - 8, wy + 34, str(e), 34, numcol, 900)
    s += num_text(cx + 22, wy + 32, '/%d' % m, 14, PAPER_DIM, 700)
    s += '</svg>'
    s += ('<div style="position:absolute; left:%dpx; top:%dpx; transform:translateX(-50%%); background:linear-gradient(#ecc777,#c99a3a); color:%s; box-shadow:0 0 0 1.5px %s, 0 3px 6px rgba(0,0,0,.45); '
          'border-radius:5px; padding:0 10px; font-size:13px; line-height:20px; font-weight:700; letter-spacing:.16em">エナジー</div>') % (cx, cy + R - 6, BRASS_INK, BRASS_INK)
    s += extra_chips(st, (0, cy - R - 32) if phone else (cx + R + 8, cy - R + 6), cy + R + 22)
    LAST['yb'] = cy + R + 22 + 28; LAST['body'] = cy + R
    return s, W, H

COMPS = {'A': comp_A, 'B': comp_B, 'C': comp_C, 'D': comp_D}
NAMES = {'A': '真鍮の歯車の器', 'B': '硝子の燃料瓶の棚', 'C': '大きな数字と玉の帯', 'D': 'からくりの圧力計'}
# 部品の「本体の左下」(輪の下端に当たる点) の、部品の箱の中での位置。今の輪の下端 (PC y=964・スマホ 732) に揃える
BOTTOM = {'A': 192, 'B': None, 'C': 152, 'D': 182}

def place_top(key, st, x, top, scale=1.0, phone=False):
    """部品の箱の上端 (y=0) を top に置く (見込みの札は箱の上に少しはみ出す)"""
    html, W, H = COMPS[key](st, phone)
    return '<div style="position:absolute; left:%.1fpx; top:%.1fpx; width:%dpx; height:%dpx; transform:scale(%.3f); transform-origin:0 0">%s</div>' % (x, top, W, H, scale, html)

def place(key, st, x, ybottom, scale=1.0, phone=False):
    html, W, H = COMPS[key](st, phone)
    b = BOTTOM[key]
    if b is None:   # B は行数で高さが変わる
        per_row = 4 if phone else 6
        rows = (st['m'] + max(0, (st.get('nxt') or 0) - st['m']) + per_row - 1) // per_row
        b = 40 + rows * 66
    top = ybottom - b * scale
    return '<div style="position:absolute; left:%.1fpx; top:%.1fpx; width:%dpx; height:%dpx; transform:scale(%.3f); transform-origin:0 0">%s</div>' % (x, top, W, H, scale, html)

# ================================================================ 灯籠 (白のリーダー。LightUi の 32×48 ドットを 3px の近似)
def lantern(x, y, n=3, scale=1.0):
    s = '<div style="position:absolute; left:%.1fpx; top:%.1fpx; width:96px; height:144px; transform:scale(%.2f); transform-origin:0 0">' % (x, y, scale)
    s += '<svg width="96" height="144" viewBox="0 0 96 144" style="position:absolute; left:0; top:0; overflow:visible" filter="url(#drop)">'
    s += '<ellipse cx="48" cy="140" rx="46" ry="8" fill="#ffd27a" fill-opacity=".25"/>'
    s += '<path d="M36 18 Q48 0 60 18" fill="none" stroke="%s" stroke-width="4"/>' % BRASS_INK
    s += '<path d="M22 22 L74 22 L66 36 L30 36 Z" fill="url(#brass)" stroke="%s" stroke-width="2"/>' % BRASS_INK
    s += '<rect x="22" y="36" width="52" height="72" rx="4" fill="#3a2a10" stroke="%s" stroke-width="2"/>' % BRASS_INK
    s += '<rect x="28" y="42" width="40" height="60" rx="3" fill="#ffe6a0" fill-opacity=".9"/>'
    s += '<ellipse cx="48" cy="80" rx="18" ry="26" fill="url(#flame)"/>'
    s += '<rect x="18" y="108" width="60" height="12" rx="2" fill="url(#brass)" stroke="%s" stroke-width="2"/>' % BRASS_INK
    s += num_text(48, 82, str(n), 26, INK, 900)
    s += '</svg>'
    s += ('<div style="position:absolute; left:48px; top:122px; transform:translateX(-50%%); background:%s; color:%s; box-shadow:0 0 0 1.5px %s; border-radius:5px; padding:0 10px; font-size:13px; line-height:20px; font-weight:700">灯</div>') % (NIGHT, BRASS_LIGHT, BRASS)
    return s + '</div>'

# ================================================================ 小さな札の見本 (⑥)
def mini_card(x, y, cost, name, body, ok=True, lifted=True):
    shadow = ('0 0 0 3px %s, 0 0 20px rgba(234,208,138,.75), 0 10px 18px rgba(0,0,0,.6)' % BRASS) if ok else ('0 0 0 3px %s, 0 10px 18px rgba(0,0,0,.6)' % ROSE)
    cc = CARD_INK if ok else CARD_COST_UP
    cb = CARD_SHELL if ok else NIGHT_ROSE
    s = '<div style="position:absolute; left:%dpx; top:%dpx; width:104px; height:142px; background:%s; border-radius:7px; box-shadow:0 0 0 2px #7a878b, %s; transform:translateY(%dpx) rotate(2deg)">' % (x, y, CARD_SHELL, shadow, -16 if lifted else 0)
    s += '<div style="position:absolute; left:5px; top:5px; width:28px; height:28px; background:%s; box-shadow:inset 0 0 0 2px #7a878b; border-radius:3px; color:%s; font-size:21px; font-weight:900; line-height:28px; text-align:center">%d</div>' % (cb, cc, cost)
    s += '<div style="position:absolute; left:37px; top:5px; right:5px; height:28px; background:%s; border-radius:3px; color:%s; font-size:13px; font-weight:700; line-height:28px; text-align:center; white-space:nowrap">%s</div>' % (FACTION_GREEN, CARD_INK, name)
    s += '<div style="position:absolute; left:6px; top:38px; right:6px; height:52px; background:linear-gradient(135deg,#3e6f7a,#9b6a3c); border-radius:2px"></div>'
    s += '<div style="position:absolute; left:6px; top:96px; right:6px; bottom:6px; background:%s; border-radius:3px; color:%s; font-size:13px; line-height:40px; text-align:center">%s</div>' % (CARD_BODY, CARD_INK, body)
    return s + '</div>'

# ================================================================ 板の共通
HEAD = '''<!doctype html>
<html>
<head>
  <meta charset="utf-8">
  <script src="./support.js"></script>
</head>
<body>
<x-dc>
<helmet>
  <link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Noto+Sans+JP:wght@400;700;900&amp;display=swap">
  <style>
    body { margin: 0; background: #0f1120; font-family: "Noto Sans JP", "Noto Sans CJK JP", sans-serif; color: #f4ecd6; -webkit-font-smoothing: antialiased; }
    .abs { position: absolute; }
    .note { background: #fff3ea; color: #9c3a2a; border: 1.5px solid #9c3a2a; border-radius: 6px; padding: 6px 10px; font-size: 14px; line-height: 20px; box-shadow: 0 4px 10px rgba(0,0,0,0.35); box-sizing: border-box; }
    .note b { color: #7a2418; }
    .gnote { background: #f4ecd6; color: #2f2e35; border: 1.5px solid #634410; border-radius: 6px; padding: 8px 12px; font-size: 14px; line-height: 21px; box-shadow: 0 4px 10px rgba(0,0,0,0.35); box-sizing: border-box; }
    .gnote b { color: #634410; }
    .cap { background: rgba(8,8,20,0.82); color: #f4ecd6; font-size: 14px; line-height: 22px; padding: 2px 10px; border-radius: 4px; white-space: nowrap; }
    .h1 { font-size: 26px; font-weight: 900; letter-spacing: .04em; color: #f4ecd6; }
    .h2 { font-size: 17px; font-weight: 700; color: #ead08a; }
  </style>
</helmet>
'''
TAIL = '''</x-dc>
</body>
</html>
'''
def write(name, html):
    with open(os.path.join(OUT, name + '.dc.html'), 'w', encoding='utf-8') as f: f.write(HEAD + html + TAIL)
def board(w, h, bg=None):
    s = '<div style="position: relative; width: %dpx; height: %dpx; overflow: hidden; background: %s">' % (w, h, GROUND) + DEFS
    if bg: s += '<img src="%s" style="position: absolute; left: 0; top: 0; width: %dpx; height: %dpx; display: block">' % (bg, w, h)
    return s
def close(): return '</div>'
def note(x, y, w, html, cls='note', arrow=None):
    s = '<div class="abs %s" style="left: %dpx; top: %dpx; width: %dpx; z-index: 60">%s</div>' % (cls, x, y, w, html)
    if arrow:
        ax, ay, bx, by = arrow
        s += ('<svg class="abs" style="left: 0; top: 0; overflow: visible; pointer-events: none; z-index: 60" width="1" height="1"><line x1="%d" y1="%d" x2="%d" y2="%d" stroke="#9c3a2a" stroke-width="2.5" stroke-dasharray="6 4"></line><circle cx="%d" cy="%d" r="5" fill="#9c3a2a"></circle></svg>' % (ax, ay, bx, by, bx, by))
    return s
def cap(x, y, text): return '<div class="abs cap" style="left: %dpx; top: %dpx; z-index: 60">%s</div>' % (x, y, text)

STATES = [
    ('①', '満タン 3 / 3', dict(e=3, m=3)),
    ('②', '払った後 1 / 3', dict(e=1, m=3)),
    ('③', '0 (使い切り)', dict(e=0, m=3)),
    ('④', '一時マナで超え 5 / 3', dict(e=5, m=3)),
    ('⑤', '次のターン 上限 +1 (このは 1 ターン目)', dict(e=3, m=3, nxt=4)),
    ('⑥a', '2E の札を持ち上げた (3 → 1)', dict(e=3, m=3, pay=2)),
    ('⑥b', '3E の札・残り 1 (足りない)', dict(e=1, m=3, pay=3)),
]

# ---- 各案の 6 つの状態 (1.3 倍)
CW, CH = 420, 390
def states_board(key, blurb):
    W, H = CW * 4 + 50, 110 + CH * 2 + 30
    s = board(W, H)
    s += '<div class="abs h1" style="left: 24px; top: 18px">案%s %s — 6 つの状態 (PC の 1.2 倍)</div>' % (key, NAMES[key])
    s += '<div class="abs" style="left: 24px; top: 58px; width: %dpx; font-size: 15px; line-height: 22px; color: #c4beb2">%s</div>' % (W - 48, blurb)
    for i, (no, label, st) in enumerate(STATES):
        cx0, cy0 = 10 + (i % 4) * (CW + 10), 110 + (i // 4) * (CH + 10)
        s += '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; overflow: hidden; border-radius: 8px; background: url(stage-pc.jpg) -10px -40px / 560px 400px; box-shadow: 0 0 0 1px #31344f">' % (cx0, cy0, CW, CH)
        s += '<div class="abs" style="inset: 0; background: rgba(15,17,32,.35)"></div>'
        s += '<div class="abs h2" style="left: 14px; top: 10px; text-shadow: 0 1px 3px #000">%s %s</div>' % (no, label)
        s += place_top(key, st, 26, 74, 1.2)
        if 'pay' in st:
            ok = st['pay'] <= st['e']
            s += mini_card(CW - 124, CH - 200, st['pay'], '樫の一撃' if ok else '大樹の怒り', 'ダメージ %d' % (12 if ok else 30), ok)
        s += '</div>'
    # 8 番目の枠: 動き
    cx0, cy0 = 10 + 3 * (CW + 10), 110 + CH + 10
    s += '<div class="abs gnote" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx">%s</div>' % (cx0, cy0, CW, CH, MOTION[key])
    return s + close(), W, H

MOTION = {
 'A': '<b>動き (今の B03 の器をそのまま使う)</b><br>・払う: 札から玉が飛び (0.22 秒)、着いた受け口から順に宝玉が消えて溝に戻る。歯車が払った量だけ <b>小さく逆回り</b> (1 個＝22.5°)<br>・補充: ターン開始に歯車が 1 歯ぶん回り、受け口に宝玉がはまる (カチッ)<br>・見込み: 持ち上げた札のコスト分の宝玉が <b>1.2Hz で白く点滅</b>、足りない分は朱の点線<br>・一時マナ: 小さな歯車が噛み合って現れ、使うと外れて沈む',
 'B': '<b>動き</b><br>・払う: 札から玉が飛び、右端の瓶から <b>液が下がって空になる</b> (0.25 秒)<br>・補充: 栓が開いて液が満ちる<br>・見込み: 払う分の瓶が白く点滅＋上に ▼<br>・一時マナ: 棚の外に瓶が立てかけられ、使うと倒れて消える<br>・注意: 瓶 6 本を超えると 2 段目 (スマホは 4 本で 2 段目)',
 'C': '<b>動き</b><br>・払う: 帯の右の玉から暗くなる。数字が跳ねて数え下がる (今の B03 のまま)<br>・見込み: 払う分の玉が点滅し、円盤の下の名札が「使うと 1」に変わる<br>・足りない: 数字が朱の墨・「2 足りない」<br>・一時マナ: 帯の仕切りの右に熾の玉<br>・今の輪 (弧＋目盛り) を玉の帯に置き換えるだけ＝最小の変更',
 'D': '<b>動き</b><br>・払う: 針が下がる (ばねの戻りで少し揺れる)。窓の数字は数え下がる<br>・補充: 針が上限まで跳ね上がる (OutBack)<br>・見込み: 下がる先に点線の影の針と斜線の扇<br>・一時マナ: 針が上限を越えて熾の帯 (一時) に入る<br>・注意: 上限が変わると目盛りが描き直される (次のターンの予告は札でしか出せない)',
}

BLURB = {
 'A': '題の紋 (gear_emblem) と同じ「歯車」をエナジーの器に。受け口の数＝このターンの上限、はまった宝玉＝残り。数えなくても形で読める (本家の宝石の玉の「数」の明快さ) うえに、中央の大きな数字でも読める。超えた分は噛み合う小さな歯車。',
 'B': '燃料の瓶が棚に並び、使うと空になる (水晶が並ぶ型)。いちばん「数が見える」。数字は左の徽章。瓶の列は横に伸びるので、上限が大きい緑ランプとスマホの狭い左下が弱点。',
 'C': '今の輪の場所と形を守り、数字を 40 → 60 に大きく、分母の「/ 3」を下の玉の帯に置き換える。最小の変更で読みやすさを上げる案。リッチさは控えめ。',
 'D': '機械職人らしい圧力計。針が上限を越えると「一時」の熾の帯に入る＝上限超えが一目で特別に見える。数字は下の窓。小さい整数を針で読むのは数より遅い。',
}

def pc_board(key):
    """PC の実寸: 輪の定位置 (左 32・下端 964) に ⑥a (2E の札を持ち上げた) を置く"""
    W, H = 1920, 1080
    s = board(W, H, 'base-pc-lift.jpg')
    s += '<img src="card-pc.png" style="position:absolute; left:%dpx; top:%dpx; width:%dpx; height:%dpx; box-shadow: 0 0 0 3px %s, 0 0 26px rgba(234,208,138,.7)">' % (CARD_BOX[0], CARD_BOX[1] - 64, CARD_BOX[2] - CARD_BOX[0], CARD_BOX[3] - CARD_BOX[1], BRASS)
    s += '<div style="position:absolute; left:878px; top:%dpx; width:34px; height:34px; background:%s; color:%s; font-size:30px; font-weight:900; line-height:34px; text-align:center">2</div>' % (812 - 64, CARD_SHELL, CARD_INK)
    st = dict(e=3, m=3, nxt=4, pay=2)
    COMPS[key](st); yb = LAST['yb']
    s += place_top(key, st, 32, 1012 - yb, 1.0)
    s += note(560, 560, 330, '<b>⑥ 札を持ち上げた</b>: 2E の札 → 払う 2 つが白く点滅し、器の上に「使うと 3 → 1」。指を離せば消える。札のコスト枠も真鍮に光る', arrow=(700, 640, 880, 742))
    s += cap(24, 1048, '案%s %s ・ PC 実寸 (1920×1080・左下の定位置・山札の真上)。⑤ 次のターン 上限 4 ＋ ⑥a 2E の札を持ち上げた' % (key, NAMES[key]))
    return s + close(), W, H

def ph_board(key):
    """スマホ (S25 相当 1920×886・UI 1.28 倍): ② 1/3 ＋ ⑤ 次のターン 上限 4"""
    W, H = 1920, 886
    s = board(W, H, 'base-ph.jpg')
    st = dict(e=1, m=3, nxt=4)
    COMPS[key](st, True); yb = LAST['yb']
    s += place_top(key, st, 30, 782 - yb * 1.28, 1.28, phone=True)
    s += '<div class="abs" style="left: 262px; top: 520px; width: 0; height: 366px; border-left: 2px dashed #f2a193"></div>'
    s += note(300, 400, 300, '<b>スマホの余白</b>: 手札の扇の左端 x=262 (朱の点線) までに収める。下は山札 (y=787) の上まで', arrow=(300, 450, 262, 560))
    s += cap(24, 860, '案%s %s ・ スマホ (S25 相当 1920×886・1.28 倍)。② 1 / 3 ＋ ⑤ 次のターン 上限 4' % (key, NAMES[key]))
    return s + close(), W, H

def lantern_board():
    W, H = 1500, 760
    s = board(W, H)
    s += '<div class="abs h1" style="left: 24px; top: 18px">灯籠 (白のリーダー) が右に付く時 — PC 実寸・灯 3・エナジー 2 / 3</div>'
    s += '<div class="abs" style="left: 24px; top: 58px; font-size: 15px; color: #c4beb2">灯籠は器の右 12・台座の下端を器の下端に揃える (LightUi 今のまま)。器が横に広い案 (B) は灯籠を右へ押し出し、手札の扇 (PC x=463) に近づく。</div>'
    for i, key in enumerate('ABCD'):
        cx0, cy0 = 10 + (i % 2) * 745, 100 + (i // 2) * 325
        cw, ch = 735, 315
        s += '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; overflow: hidden; border-radius: 8px; background: url(base-pc.jpg) 0 -765px / 1920px 1080px; box-shadow: 0 0 0 1px #31344f">' % (cx0, cy0, cw, ch)
        s += '<div class="abs h2" style="left: 14px; top: 10px; text-shadow: 0 1px 3px #000">案%s %s</div>' % (key, NAMES[key])
        st = dict(e=2, m=3)
        html, w, h = COMPS[key](st)
        top = 247 - LAST['yb']
        s += place_top(key, st, 32, top)
        bottom = top + LAST['body']
        wid = {'A': 168, 'B': 96 + 3 * 30 + 4, 'C': 128, 'D': 176}[key]
        lx = 32 + wid + 12
        s += lantern(lx, bottom - 144, 3)
        s += '<div class="abs" style="left: %dpx; top: 30px; width: 0; height: 290px; border-left: 2px dashed #f2a193"></div>' % (463 - 0)
        s += '<div class="abs" style="left: 470px; top: 36px; font-size: 13px; color: #f2a193">手札の扇の左端 (x=463)</div>'
        s += '</div>'
    return s + close(), W, H

def current_board():
    W, H = 1900, 1060
    s = board(W, H)
    s += '<div class="abs h1" style="left: 24px; top: 18px">今のエナジーの輪 (2026-11-04 の shots・左下を 2 倍)</div>'
    for i, (img, t) in enumerate([('cur-full.jpg', '3 / 3 (1 ターン目・このは相当の予告つき)'), ('cur-low.jpg', '1 / 3 (払った後)'), ('cur-over.jpg', '5 / 3 (一時マナ・ひばな)')]):
        x = 20 + i * 625
        s += '<div class="abs" style="left: %dpx; top: 70px; width: 610px; height: 470px; overflow: hidden; border-radius: 8px; box-shadow: 0 0 0 1px #31344f"><img src="%s" style="position:absolute; left:-10px; top:-60px; width:940px; height:560px"></div>' % (x, img)
        s += cap(x + 10, 80, t)
    s += '<div class="abs" style="left: 20px; top: 556px; width: 900px; height: 488px; overflow: hidden; border-radius: 8px; box-shadow: 0 0 0 1px #31344f"><img src="cur-ph.jpg" style="position:absolute; left:0; top:0; width:940px; height:692px"></div>'
    s += cap(30, 566, 'スマホ (S25 相当) 1 / 3')
    s += note(950, 560, 920, '<b>良いところ (壊さない)</b>: 左下の定位置・山札の真上・丸い形・真鍮＝資源・払うと玉が札から輪へ飛ぶ (B03)・数字が数え下がる。<br><br>'
              '<b>読みにくいところ</b><br>'
              '1. <b>リッチさが足りない</b>: 夜の札の UI の中で、クリームの円盤だけが紙の質感で浮く。数字 40 は大きいが、器そのものに世界観 (からくり・歯車・灯) が無い<br>'
              '2. <b>残りが「弧の長さ」</b>: 3 つ中 1 つ残りは弧の 1/3。弧の目盛りは細い紙色の切れ目で、使った分の溝 (真鍮の墨) と残りの真鍮の明るさの差が小さい<br>'
              '3. <b>上限超え</b>: 5 / 3 は輪の右上の 14px の小玉 2 つ。火の粉と見分けにくく「何の 2 つか」読めない<br>'
              '4. <b>払う見込みが無い</b>: 札を持ち上げても輪は何も変わらない。足りない時は出そうとして初めて輪が朱に光る<br>'
              '5. <b>予告の札</b>「次のターン 上限 4」は輪から離れた別の札で、輪のどこが増えるのか形で見えない<br>'
              '6. 「/ 3」と「エナジー」は 13〜15px の中墨で、紙の上で小さい', cls='gnote')
    return s + close(), W, H

# ---- 比較 (Main): 4 案を ① と ⑥a と ④ で並べ、推奨と評価
VERDICT = {
 'A': ('推奨', BRASS, '★★★★★', [
     '<b>分かりやすさ</b>: 受け口の数＝上限・宝玉＝残りで、数を数えなくても形で読める。中央の数字 44 でも読める (二重の手がかり)',
     '<b>リッチさ</b>: 題の紋と同じ歯車＝ギアウィッチの器。宝玉の光・噛み合う小歯車 (一時マナ)・回る動きで「からくり」が出る',
     '<b>壊さない</b>: 左下・丸い形・山札の真上・B03 の飛ぶ玉と数え下がりをそのまま使える。予告は受け口の点線 (+1) と札',
     '<b>手間</b>: 歯車・宝玉・溝はコード生成 (PaperFx.Ring と同じ作り方) で出せる。磨くなら歯車 1 枚だけ PixelLab',
     '<b>スマホ</b>: 外形は約 205px (今の輪 164px より一回り大きい) で、手札の扇 (x=262) の手前に収まる。受け口は 12 個まで輪に並ぶ (緑のランプ 8〜10 も入る)']),
 'B': ('次点', NIGHT_EDGE, '★★★☆☆', [
     '<b>分かりやすさ</b>: 数がいちばん見える (水晶が並ぶ型)。ただ数字は左の徽章に分かれ、視線が 2 か所',
     '<b>リッチさ</b>: 硝子と液の光はきれい。灯籠 (灯) と「光る容れ物」が並ぶので白のリーダーでは紛らわしい',
     '<b>壊さない</b>: 丸い形を捨てて横長になる。灯籠を右へ押し出し、手札の扇に近づく',
     '<b>手間</b>: 瓶はコード生成で可。棚の段組み (6 本・スマホ 4 本で折り返し) の配置が要る',
     '<b>スマホ</b>: 左下の幅が 230px しかなく、4 本で 2 段目＝上限 5 以上で縦に伸びる']),
 'C': ('安全策', NIGHT_EDGE, '★★★☆☆', [
     '<b>分かりやすさ</b>: 数字 60 で最も速く読める。帯の玉で残りと上限も見える。見込みは名札の「使うと 1」',
     '<b>リッチさ</b>: 今とほぼ同じ見た目。「リッチに」の要望には弱い',
     '<b>壊さない</b>: いちばん壊さない (今の円盤と場所のまま)',
     '<b>手間</b>: 最小 (弧と目盛りを外し、帯を足すだけ)',
     '<b>スマホ</b>: 帯が 5 玉で円盤の幅。6 玉以上は玉を縮める']),
 'D': ('見送り', NIGHT_EDGE, '★★☆☆☆', [
     '<b>分かりやすさ</b>: 小さな整数を針の角度で読むのは数より遅い。結局は下の窓の数字を読む',
     '<b>リッチさ</b>: いちばん機械らしく、上限越えの「一時」の帯は気持ちいい',
     '<b>壊さない</b>: 丸い形は保つが、数字が下の小窓 (34px) に縮む',
     '<b>手間</b>: 目盛りが上限ごとに描き直し・針の揺れ。予告 (+1) を形で出せない',
     '<b>スマホ</b>: 目盛りの数字 14px が 1.28 倍でも細かい']),
}

def main_board():
    W, H = 2000, 1240
    s = board(W, H)
    s += '<div class="abs h1" style="left: 28px; top: 20px; font-size: 30px">エナジーの表記 — 4 案の比較と推奨</div>'
    s += '<div class="abs" style="left: 28px; top: 66px; font-size: 15px; color: #c4beb2">どの案も ① 満タン 3/3・⑥a 2E の札を持ち上げた・④ 一時マナ 5/3 を PC の実寸で (下地は UI を消した舞台)。物差しは StS2 (色の宝珠に数字)・Hearthstone (水晶が並び、使うと空になる)。構図は写さず、器を世界観 (歯車・燃料・からくり) から作った。</div>'
    for i, key in enumerate('ABCD'):
        x0, y0 = 20 + (i % 2) * 990, 110 + (i // 2) * 560
        tag, col, stars, lines = VERDICT[key]
        edge = '0 0 0 3px %s' % BRASS if key == 'A' else '0 0 0 1px #31344f'
        s += '<div class="abs" style="left: %dpx; top: %dpx; width: 970px; height: 545px; border-radius: 10px; background: %s; box-shadow: %s">' % (x0, y0, WINDOW, edge)
        s += '<div class="abs" style="left: 18px; top: 12px; font-size: 22px; font-weight: 900">案%s %s</div>' % (key, NAMES[key])
        s += '<div class="abs" style="right: 18px; top: 14px; background: %s; color: %s; border-radius: 6px; padding: 2px 12px; font-size: 15px; font-weight: 900">%s %s</div>' % (col if key == 'A' else NIGHT_PILL, BRASS_INK if key == 'A' else PAPER, tag, stars)
        s += '<div class="abs" style="left: 10px; top: 52px; width: 950px; height: 270px; overflow: hidden; border-radius: 8px; background: url(stage-pc.jpg) 0 -60px / 1120px 800px">'
        for j, st in enumerate([dict(e=3, m=3), dict(e=3, m=3, pay=2), dict(e=5, m=3)]):
            s += place_top(key, st, 20 + j * 315, 36, 1.0)
            s += '<div class="abs" style="left: %dpx; top: 8px; font-size: 14px; font-weight: 700; color: #ead08a; text-shadow: 0 1px 2px #000">%s</div>' % (20 + j * 315, ['① 3 / 3', '⑥a 2E の札を持ち上げた', '④ 一時マナ 5 / 3'][j])
        s += '</div>'
        s += '<div class="abs" style="left: 22px; top: 336px; width: 926px; font-size: 15px; line-height: 24px; color: #e8e2ce">%s</div>' % '<br>'.join('・' + l for l in lines)
        s += '</div>'
    return s + close(), W, H

# ================================================================ 書き出し
boards = []
def emit(name, title, fn, x, y):
    html, w, h = fn()
    write(name, html)
    boards.append(dict(file=name + '.dc.html', x=x, y=y, w=w, h=h, title=title))

emit('Main', '比較と推奨 (案A を推奨)', main_board, 0, 0)
emit('Current', '今のエナジーの輪', current_board, 0, 1340)
emit('Lantern', '灯籠が右に付く時 (4 案)', lantern_board, 0, 2500)
X = 2140
for i, key in enumerate('ABCD'):
    y = i * 1200
    emit('%sStates' % key, '案%s %s 6 つの状態' % (key, NAMES[key]), lambda k=key: states_board(k, BLURB[k]), X, y)
    emit('%sPC' % key, '案%s PC 実寸' % key, lambda k=key: pc_board(k), X + 1800, y)
    emit('%sPhone' % key, '案%s スマホ' % key, lambda k=key: ph_board(k), X + 3800, y)
import json
with open(os.path.join(OUT, 'canvas.json'), 'w', encoding='utf-8') as f:
    json.dump({'artboards': boards}, f, ensure_ascii=False, indent=1)
print('wrote', len(boards), 'artboards')

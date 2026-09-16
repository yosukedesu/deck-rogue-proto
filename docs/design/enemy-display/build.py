# build.py — 敵の表示の見直し (2026-09-16 ユーザー「敵の行動は敵の上に表示したほうがわかりやすい。敵の表示についてよく考えて」)。
# 2026-09-15 案C「帳面の一行」は名前・HP・意図を足元の1枚に畳んだ。意図 (いちばん読ませたい情報) が札の3段目に埋もれる、を直す。
# 現状 (スマホ 1462×675) の指摘 ＋ 3案 (A 頭上に意図・足元に名前とHP／B 頭上に看板 (名前＋意図)・足元にHP／C 紙なしで浮かせる) ＋ 比較と原則。
# 下地は UI を消した Unity のスクショ (Autopilot の hideui=1。カラーテーマ「黒鉄と真鍮」適用後)。座標はキャンバス単位。
# 敵の絵の位置は UI ありのスクショから読んだ値 (帳面の中心＝入れ物の中心、足元＝StatusLine 365 − 足の高さ)。
# 使い方: python3 build.py → *.dc.html → seed-canvas.mjs で束ねる (docs/design/color-theme と同じ手順)。
import base64, os
OUT = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(OUT, '..', '..', '..'))
ART_DIR = os.path.join(REPO, 'unity', 'Assets', 'Resources', 'Art')

# カラーテーマ「黒鉄と真鍮」(docs/color-theme.md)
INK = '#2f2e35'; INK_SOFT = '#4e4c55'; PAPER = '#f4ecd6'; PAPER2 = '#eadfc4'; PAPER3 = '#fbf6e8'
BRASS = '#c99a3a'; BRASS_LIGHT = '#ead08a'; BRASS_INK = '#634410'; MANA = '#3aa79b'; MANA_LIGHT = '#b5ddd6'; MANA_INK = '#155650'
ROSE = '#c9635a'; SKY = '#6f95b8'; SKY_LIGHT = '#d6e6fa'; SKY_INK = '#2f5a7a'; PLUM = '#9d86bf'; PLUM_LIGHT = '#e9def3'; PLUM_INK = '#5a3d78'; BAD_INK = '#9c3a2a'

def uri(path):
    with open(path, 'rb') as f: return 'data:image/png;base64,' + base64.b64encode(f.read()).decode('ascii')
def icon(name): return uri(os.path.join(ART_DIR, 'icons', name + '.png'))
ICON = {k: icon(k) for k in ['intent_attack', 'intent_defend', 'intent_hex', 'sword', 'shield', 'exposed', 'burn', 'growth', 'set']}

W, H = 1462, 675

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
def ic(name, size=16):
    return '<img class="px" src="%s" style="width: %dpx; height: %dpx; display: block; flex: none">' % (ICON[name], size, size)
def note(x, y, w, html, arrow=None):
    s = '<div class="abs note" style="left: %dpx; top: %dpx; width: %dpx; z-index: 60">%s</div>' % (x, y, w, html)
    if arrow:
        ax, ay, bx, by = arrow
        s += ('<svg class="abs" style="left: 0; top: 0; overflow: visible; pointer-events: none; z-index: 60" width="1" height="1"><line x1="%d" y1="%d" x2="%d" y2="%d" stroke="#9c3a2a" stroke-width="2.5" stroke-dasharray="6 4"></line><circle cx="%d" cy="%d" r="5" fill="#9c3a2a"></circle></svg>' % (ax, ay, bx, by, bx, by))
    return s
def caption(x, y, text): return '<div class="abs cap" style="left: %dpx; top: %dpx; z-index: 60">%s</div>' % (x, y, text)

# ---- 紙の部品 ----
def sheet(x, y, w, h, bg=PAPER2, edge=None, r='8px 10px 8px 9px / 9px 8px 10px 8px', z=None, extra=''):
    ring = '' if not edge else ', 0 0 0 4px %s' % edge
    return '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; background: %s; border-radius: %s; box-shadow: 0 0 0 1.5px %s%s, 0 3px 8px rgba(0,0,0,0.4); box-sizing: border-box; %s%s">' % (x, y, w, h, bg, r, INK, ring, extra, ('z-index: %d;' % z) if z is not None else '')
def pill(text, ink=INK, bg=PAPER2, size=13, h=20, icon_name=None, gap=3):
    return '<span style="display: inline-flex; align-items: center; gap: %dpx; height: %dpx; padding: 0 7px 0 5px; border-radius: 6px; background: %s; color: %s; box-shadow: 0 0 0 1px %s; font-size: %dpx; white-space: nowrap; flex: none">%s%s</span>' % (gap, h, bg, ink, ink, size, ic(icon_name, 13) if icon_name else '', text)
def hpbar(x, y, w, h, hp, mx, block=None, tick=None, size=13):
    s = '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; background: %s; box-shadow: 0 0 0 2px %s; border-radius: 2px">' % (x, y, w, h, PAPER, INK)
    s += '<div class="abs" style="left: 2px; top: 2px; width: %dpx; height: %dpx; background: %s"></div>' % (int((w - 4) * hp / mx), h - 4, ROSE)
    if tick is not None:
        tx = int(w * tick)
        s += '<div class="abs" style="left: %dpx; top: -3px; width: 6px; height: %dpx; background: %s"><div class="abs" style="left: 1.5px; top: 1px; width: 3px; height: %dpx; background: %s"></div></div>' % (tx - 3, h + 6, INK, h + 4, BRASS)
    s += '<div class="abs" style="inset: 0; display: flex; align-items: center; justify-content: center; font-size: %dpx; color: %s; text-shadow: 0 0 2px %s, 0 0 2px %s">%d / %d</div>' % (size, INK, PAPER, PAPER, hp, mx)
    s += '</div>'
    if block:
        # 本家形: HP バーの左端に盾 (円の盾に数字)
        s += '<div class="abs deco" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; border-radius: 50%%; background: %s; box-shadow: 0 0 0 2px %s; display: flex; align-items: center; justify-content: center; gap: 1px; font-size: %dpx; color: %s">%s%d</div>' % (x - 14, y - (26 - h) // 2, 40, 26, SKY_LIGHT, INK, 15, SKY_INK, ic('shield', 12), block)
    return s

def intent_row(kind, val, riders=(), icon_size=32, num_size=24, gap=6):
    icon_name = {'attack': 'intent_attack', 'defend': 'intent_defend', 'hex': 'intent_hex'}[kind]
    s = '<div style="display: flex; align-items: center; gap: %dpx">%s' % (gap, ic(icon_name, icon_size))
    if val is not None: s += '<span class="deco" style="font-size: %dpx; line-height: 1; color: %s">%s</span>' % (num_size, INK, val)
    for r in riders: s += pill(*r)
    return s + '</div>'

# 敵の見本 (探り屋の三人組・ターン2。①狙っている ⚔4／②🛡8＋筋力+1／③⚔13。全員 筋力-3)
TRIO = [dict(name='探り屋', num='①', hp=14, mx=18, kind='attack', val=4, riders=[], status=[('sword', '筋力-3')], aimed=True),
        dict(name='探り屋', num='②', hp=16, mx=16, kind='defend', val=8, riders=[('筋力+1', BRASS_INK, PAPER2, 13, 20, 'sword')], status=[('sword', '筋力-3')], aimed=False, block=None),
        dict(name='探り屋', num='③', hp=19, mx=19, kind='attack', val=13, riders=[], status=[('sword', '筋力-3')], aimed=False)]
QUAD = [dict(name='小泥', num='①', hp=9, mx=9, kind='attack', val=2, riders=[], status=[('sword', '筋力-1')], aimed=True),
        dict(name='小泥', num='②', hp=8, mx=8, kind='hex', val=None, riders=[('負傷1', PLUM_INK, PLUM_LIGHT, 13, 20, 'exposed')], status=[('sword', '筋力-1')], aimed=False),
        dict(name='小泥', num='③', hp=9, mx=9, kind='defend', val=4, riders=[('筋力+1', BRASS_INK, PAPER2, 13, 20, 'sword')], status=[], aimed=False),
        dict(name='小泥', num='④', hp=8, mx=8, kind='hex', val=None, riders=[('負傷1', PLUM_INK, PLUM_LIGHT, 13, 20, 'exposed')], status=[], aimed=False)]
BOSS = dict(name='脳筋オーガ', num='', hp=181, mx=181, kind='attack', val=10, riders=[], status=[('sword', '筋力+1')], traits=['装甲25'], aimed=True, forecast='HP90以下で 攻撃8〜10×2')

# 絵の位置 (キャンバス単位)。cx=入れ物の中心・top=頭の上端・feet=足元。帳面の下端 (StatusLine) はスマホ 365・PC 780
PH_TRIO = [dict(cx=894, top=156, feet=293), dict(cx=1077, top=149, feet=270), dict(cx=1280, top=152, feet=263)]
PH_QUAD = [dict(cx=856, top=152, feet=295), dict(cx=1024, top=150, feet=276), dict(cx=1215, top=152, feet=268), dict(cx=1327, top=155, feet=262)]
PH_BOSS = dict(cx=1037, top=114, feet=282)
PC_TRIO = [dict(cx=1178, top=372, feet=585), dict(cx=1399, top=322, feet=536), dict(cx=1714, top=305, feet=518)]
LINE_PH = 365; LINE_PC = 780

# ---- 共通: 上部バー・自分の札・手札 (省略形。色の主題ではないので簡略) ----
def topbar(w, phone=True):
    s = sheet(70, 11, 200, 34, PAPER2) + '<div class="abs" style="left: 10px; top: 0; height: 34px; display: flex; align-items: center; gap: 8px"><span style="font-size: 13px; color: %s; letter-spacing: 0.1em">幕 1 · 行 3</span><span class="deco" style="font-size: 18px">探り屋の三人組</span></div></div>' % INK_SOFT
    s += sheet(300, 11, 92, 34, PAPER2) + '<div class="abs" style="left: 10px; top: 0; height: 34px; display: flex; align-items: center; gap: 6px"><span style="font-size: 13px; color: %s">ターン</span><span class="deco" style="font-size: 17px; font-weight: 400">2</span></div></div>' % INK_SOFT
    s += sheet(w // 2 - 75, 12, 150, 32, PAPER3) + '<div class="abs deco" style="inset: 0; display: flex; align-items: center; justify-content: center; font-size: 15px; font-weight: 400; letter-spacing: 0.2em">あなたの番</div></div>'
    s += sheet(w - 150, 11, 84, 34, PAPER2) + '<div class="abs deco" style="inset: 0; display: flex; align-items: center; justify-content: center; gap: 4px; font-size: 17px; font-weight: 400">50<span style="font-size: 13px; color: %s">G</span></div></div>' % INK_SOFT
    s += sheet(w - 56, 11, 44, 34, PAPER2) + '<div class="abs" style="inset: 0; display: flex; align-items: center; justify-content: center; font-size: 20px">≡</div></div>'
    return s
def self_ledger(x, y):
    s = sheet(x, y, 300, 76, PAPER2)
    s += hpbar(12, 10, 276, 16, 71, 80)
    s += '<div class="abs" style="left: 12px; top: 34px; font-size: 14px; line-height: 20px; color: %s">被ダメ <b style="color: %s">17</b> − <span style="color: %s">盾 5</span> ＝ HP <b style="color: %s">−12</b> → 59</div>' % (INK_SOFT, BAD_INK, SKY_INK, BAD_INK)
    s += '<div class="abs" style="left: 12px; top: 54px; display: flex; gap: 6px">%s%s</div>' % (pill('成長 +2', '#276a34', '#cfeacc', icon_name='growth'), pill('3 / 4 エナジー', BRASS_INK, BRASS_LIGHT))
    return s + '</div>'
def hand_hint(w, y):
    # 手札は色の主題ではないので、紙の札の上端だけを帯で示す
    s = ''
    for i in range(5):
        x = 400 + i * 150
        s += '<div class="abs" style="left: %dpx; top: %dpx; width: 150px; height: 190px; background: %s; border-radius: 8px 10px 8px 9px; box-shadow: 0 0 0 1.5px %s, 0 6px 14px rgba(0,0,0,0.45); transform: rotate(%sdeg)"><div class="abs" style="left: 10px; top: 32px; right: 10px; height: 70px; background: #20233a; box-shadow: 0 0 0 1.5px %s"></div><div class="abs deco" style="left: 40px; top: 6px; font-size: 17px">%s</div><div class="abs" style="left: 0; right: 0; top: 118px; text-align: center; font-size: 14px">%s</div></div>' % (
            x, y, PAPER, INK, INK, (i - 2) * 1.5, ['打撃', '絡み蔦', '打ち据え', '守りの蔓', '蔦の楔'][i], ['ダメージ<b>8</b>', 'ブロック7／ダメージ9', 'ダメージ6・急所+2', '被攻撃前: ブロック12', '盾喰い・ダメージ7'][i])
    return s
def end_turn(x, y, w=190, h=50):
    return '<div class="abs deco" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; background: %s; border-radius: 10px 12px 9px 11px / 11px 9px 12px 10px; box-shadow: 0 0 0 1.5px %s, 0 3px 8px rgba(0,0,0,0.35); display: flex; align-items: center; justify-content: center; font-size: 19px; letter-spacing: 0.12em">ターン終了</div>' % (x, y, w, h, BRASS_LIGHT, INK)

# ================================================================ 案A: 頭上に意図・足元に名前とHP
def intent_tag(cx, bottom, e, phone=True, edge=None, small=False, stack=False):
    """頭のすぐ上の小さな札: [意図の絵][数字][rider]。狙っている／行動中は真鍮の縁。stack=rider を下の段に (隣の頭が近い時)"""
    icon_size, num, h = (28, 22, 38) if small else ((32, 24, 42) if phone else (44, 32, 56))
    riders = e['riders']
    if stack and riders:
        est_top = icon_size + 8 + (len(str(e['val'])) * num * 0.62 if e['val'] is not None else 0) + 26
        est_r = max(30 + len(r[0]) * 9.5 + 6 for r in riders) + 12
        w = max(72, int(max(est_top, est_r))); h += 24
        s = sheet(cx - w // 2, bottom - h, w, h, PAPER, edge=edge, z=5)
        s += '<div class="abs" style="left: 0; right: 0; top: 0; height: %dpx; display: flex; align-items: center; justify-content: center">%s</div>' % (h - 24, intent_row(e['kind'], e['val'], (), icon_size, num))
        s += '<div class="abs" style="left: 0; right: 0; top: %dpx; height: 22px; display: flex; align-items: center; justify-content: center; gap: 4px">%s</div>' % (h - 26, ''.join(pill(*r) for r in riders))
    else:
        est = icon_size + 8 + (len(str(e['val'])) * num * 0.62 if e['val'] is not None else 0) + sum(30 + len(r[0]) * 9.5 + 6 for r in riders) + 26
        w = max(72, int(est))
        s = sheet(cx - w // 2, bottom - h, w, h, PAPER, edge=edge, z=5)
        s += '<div class="abs" style="inset: 0; display: flex; align-items: center; justify-content: center">%s</div>' % intent_row(e['kind'], e['val'], riders, icon_size, num)
    # 尾 (頭へ)
    s += '<div class="abs" style="left: 50%%; bottom: -7px; width: 12px; height: 12px; margin-left: -6px; background: %s; transform: rotate(45deg); box-shadow: 1.5px 1.5px 0 0 %s"></div>' % (PAPER, INK)
    return s + '</div>'
def feet_ledger_a(cx, line, e, w, phone=True, forecast=None, traits=None, edge=None):
    """足元の帳面 (2段): 名前＋状態／HP (左端に盾)。予告があれば3段目"""
    rowh = 24 if phone else 30; barh = 16 if phone else 20
    h = 6 + rowh + 4 + barh + 8 + ((20 if phone else 24) if forecast else 0)
    x = cx - w // 2; y = line - h
    s = sheet(x, y, w, h, PAPER2, edge=edge)
    s += '<div class="abs deco" style="left: 8px; top: 4px; height: %dpx; display: flex; align-items: center; font-size: %dpx">%s%s</div>' % (rowh, 15 if phone else 19, e['num'], e['name'])
    pills = ''.join(pill(t, INK, PAPER, icon_name=i) for i, t in e.get('status', [])) + ''.join(pill(t, INK_SOFT, PAPER, icon_name='shield') for t in (traits or []))
    s += '<div class="abs" style="right: 8px; top: 4px; height: %dpx; display: flex; align-items: center; gap: 4px">%s</div>' % (rowh, pills)
    bx = 8 + (18 if e.get('block') else 0)
    s += hpbar(bx, 6 + rowh + 4, w - bx - 8, barh, e['hp'], e['mx'], block=e.get('block'), tick=0.5 if forecast else None)
    if forecast:
        s += '<div class="abs" style="left: 8px; right: 8px; top: %dpx; font-size: 13px; line-height: 20px; color: %s; white-space: nowrap; overflow: hidden; text-overflow: ellipsis">%s%s</div>' % (6 + rowh + 4 + barh + 4, BRASS_INK, ic('sword', 13).replace('display: block', 'display: inline-block; vertical-align: -2px; margin-right: 4px'), forecast)
    return s + '</div>'

def board_a_trio():
    s = board('base-phone-trio.jpg') + topbar(W)
    for e, p in zip(TRIO, PH_TRIO):
        edge = BRASS if e['aimed'] else None
        s += intent_tag(p['cx'], p['top'] - 8, e, edge=edge)
        s += feet_ledger_a(p['cx'], LINE_PH, e, 176, edge=edge)
    s += self_ledger(30, LINE_PH - 76) + hand_hint(W, 390) + end_turn(W - 220, 470)
    s += note(880, 40, 300, '<b>意図は頭のすぐ上</b>: 絵→数字の順に読む。狙っている敵は札も帳面も真鍮の縁 (▼は廃止)', (1000, 100, 1077, 108))
    s += note(1190, 400, 260, '<b>足元は2段</b>: 名前＋状態／HP。盾は HP バーの左端 (本家形)。3段目は予告がある時だけ', (1190, 430, 1160, 330))
    s += caption(24, 648, '案A 頭上に意図・足元に名前とHP (スマホ 1462×675・探り屋の三人組)。意図の札＝紙 (明るい)、帳面＝紙(濃)')
    return s + close()
def board_a_boss():
    s = board('base-phone-boss.jpg') + topbar(W)
    e = dict(BOSS); p = PH_BOSS
    s += intent_tag(p['cx'], p['top'] - 6, e, edge=BRASS)
    s += feet_ledger_a(p['cx'], LINE_PH, e, 300, forecast=e['forecast'], traits=e['traits'], edge=BRASS)
    s += self_ledger(30, LINE_PH - 76) + hand_hint(W, 390) + end_turn(W - 220, 470)
    s += note(1180, 60, 260, '<b>ボス</b>: 頭 (y≈114) の上に札が収まる (上部バー 56 の下)。収まらない敵は頭に重ねる (本家も重なる)', (1180, 90, 1090, 90))
    s += note(1200, 400, 240, '<b>予告は HP バーの下</b>: 目盛り (半分の線) と同じ札に「HP90以下で 攻撃8〜10×2」。装甲などの特性は名前の行の右', (1200, 430, 1180, 345))
    s += caption(24, 648, '案A ボス (幅300の帳面: 名前＋筋力＋特性／HP と半分の目盛り／予告)')
    return s + close()
def board_a_quad():
    s = board('base-phone-quad.jpg') + topbar(W)
    for e, p in zip(QUAD, PH_QUAD):
        edge = BRASS if e['aimed'] else None
        s += intent_tag(p['cx'], p['top'] - 8, e, edge=edge, small=True, stack=True)   # 4体: rider は下の段 (頭が 110 しか離れていない)
        s += feet_ledger_a(p['cx'], LINE_PH, e, 104 if p is PH_QUAD[2] or p is PH_QUAD[3] else 150, edge=edge)
    s += self_ledger(30, LINE_PH - 76) + hand_hint(W, 390) + end_turn(W - 220, 470)
    s += note(740, 30, 320, '<b>4体</b>: 頭上の札は 72〜100 幅・絵 28・数字 22。rider は下の段に1つまで (2つ目以降は帳面のタップで)。頭が 110 しか離れていなくても札は重ならない', (1000, 90, 1215, 96))
    s += note(1180, 400, 270, '4体の帳面は 104 幅: 名前と HP だけ。状態の札は HP バーの上に重ねず、タップの説明に', (1180, 430, 1215, 330))
    s += caption(24, 648, '案A 4体 (小泥の大群)。頭上の札が横に並ぶ＝本家と同じ読み方向 (左から右)')
    return s + close()
def board_a_pc():
    Wp, Hp = 1920, 1080
    s = board('base-pc-trio.jpg', Wp, Hp)
    for e, p in zip(TRIO, PC_TRIO):
        edge = BRASS if e['aimed'] else None
        s += intent_tag(p['cx'], p['top'] - 10, e, phone=False, edge=edge)
        s += feet_ledger_a(p['cx'], LINE_PC, e, 210, phone=False, edge=edge)
    s += note(1200, 120, 320, '<b>PC</b>: 絵 44・数字 32 の札。帳面 210 幅は 名前＋状態／HP の2段 (いまの 140 高から 74 高へ)。特性と分岐の一文はホバーの説明へ', (1300, 200, 1399, 260))
    s += caption(24, 1050, '案A PC (1920×1080)。頭上の札と足元の帳面 (2段)')
    return s + close()

# ================================================================ 案B: 頭上に看板 (名前＋意図)・足元に HP だけ
def sign(cx, bottom, e, edge=None):
    w = 170; h = 62
    s = sheet(cx - w // 2, bottom - h, w, h, PAPER, edge=edge, z=5)
    s += '<div class="abs" style="left: 0; right: 0; top: 4px; text-align: center; font-size: 13px; color: %s">%s%s</div>' % (INK_SOFT, e['num'], e['name'])
    s += '<div class="abs" style="left: 0; right: 0; top: 22px; height: 36px; display: flex; align-items: center; justify-content: center">%s</div>' % intent_row(e['kind'], e['val'], e['riders'], 30, 22)
    s += '<div class="abs" style="left: 50%%; bottom: -7px; width: 12px; height: 12px; margin-left: -6px; background: %s; transform: rotate(45deg); box-shadow: 1.5px 1.5px 0 0 %s"></div>' % (PAPER, INK)
    return s + '</div>'
def feet_hp_b(cx, line, e, w, edge=None):
    h = 46; x = cx - w // 2; y = line - h
    s = sheet(x, y, w, h, PAPER2, edge=edge)
    s += hpbar(8, 6, w - 16, 16, e['hp'], e['mx'])
    s += '<div class="abs" style="left: 8px; top: 26px; display: flex; gap: 4px">%s</div>' % ''.join(pill(t, INK, PAPER, icon_name=i, h=16, size=12) for i, t in e.get('status', []))
    return s + '</div>'
def board_b_trio():
    s = board('base-phone-trio.jpg') + topbar(W)
    for e, p in zip(TRIO, PH_TRIO):
        edge = BRASS if e['aimed'] else None
        s += sign(p['cx'], p['top'] - 8, e, edge=edge)
        s += feet_hp_b(p['cx'], LINE_PH, e, 176, edge=edge)
    s += self_ledger(30, LINE_PH - 76) + hand_hint(W, 390) + end_turn(W - 220, 470)
    s += note(880, 40, 300, '<b>看板</b>: 名前＋意図を1枚に (2026-09-14 の吹き出しの再来)。足元は HP と状態だけの薄い札', (1000, 100, 1077, 90))
    s += note(1190, 400, 260, '看板 62 高×170 幅。3体で頭上に 3 枚並ぶ＝紙の面積は案A より大きい。ボスは頭に重なる', (1190, 430, 1280, 100))
    s += caption(24, 648, '案B 頭上に看板 (名前＋意図)・足元に HP')
    return s + close()

# ================================================================ 案C: 紙なしで浮かせる (本家そのもの)
def float_intent(cx, bottom, e, edge=None):
    icon_size, num = 40, 30
    s = '<div class="abs" style="left: %dpx; top: %dpx; width: 200px; height: 46px; margin-left: -100px; display: flex; align-items: center; justify-content: center; gap: 6px; z-index: 5">' % (cx, bottom - 46)
    s += ic({'attack': 'intent_attack', 'defend': 'intent_defend', 'hex': 'intent_hex'}[e['kind']], icon_size)
    if e['val'] is not None: s += '<span class="deco outl" style="font-size: %dpx; line-height: 1">%s</span>' % (num, e['val'])
    for r in e['riders']: s += pill(r[0], r[1], r[2], 13, 20, r[5] if len(r) > 5 else None)
    if edge: s += '<div class="abs" style="left: 50%%; top: -6px; width: 14px; height: 14px; margin-left: -7px; background: %s; clip-path: polygon(0 0, 100%% 0, 50%% 100%%)"></div>' % edge
    return s + '</div>'
def feet_hp_c(cx, line, e, w, edge=None):
    x = cx - w // 2; y = line - 30
    s = ''
    if edge: s += '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: 30px; border-radius: 4px; box-shadow: 0 0 0 3px %s"></div>' % (x - 3, y - 3, w + 6, edge)
    s += hpbar(x, y, w, 20, e['hp'], e['mx'])
    s += '<div class="abs deco outl" style="left: %dpx; top: %dpx; font-size: 15px">%s</div>' % (x, y - 22, e['num'] + e['name'])
    st = ''.join('<span style="display:inline-flex;align-items:center;gap:2px;font-size:12px;color:%s;text-shadow:0 0 2px #000">%s%s</span>' % (PAPER, ic(i, 12), t) for i, t in e.get('status', []))
    if st: s += '<div class="abs" style="left: %dpx; top: %dpx; display: flex; gap: 6px">%s</div>' % (x, y + 24, st)
    return s
def board_c_trio():
    s = board('base-phone-trio.jpg') + topbar(W)
    for e, p in zip(TRIO, PH_TRIO):
        edge = BRASS if e['aimed'] else None
        s += float_intent(p['cx'], p['top'] - 6, e, edge=edge)
        s += feet_hp_c(p['cx'], LINE_PH - 8, e, 150, edge=edge)
    s += self_ledger(30, LINE_PH - 76) + hand_hint(W, 390) + end_turn(W - 220, 470)
    s += note(880, 40, 300, '<b>紙なし</b>: 本家そのもの。意図の絵 40＋縁取りの数字。舞台がいちばん見える', (1000, 100, 1077, 100))
    s += note(1190, 400, 260, '足元は HP バーだけ (名前は縁取りの小文字・状態は小さな絵)。rider と特性・予告はタップの説明にしか置けない', (1190, 430, 1280, 340))
    s += caption(24, 648, '案C 紙なしで浮かせる (意図の絵と縁取りの数字・足元は HP バーだけ)')
    return s + close()

# ================================================================ 現状
def board_current():
    s = board('current-phone.jpg', 1100, 508)
    s = s.replace('overflow: hidden; background: #0f1120', 'overflow: visible; background: #0f1120')
    s = '<div style="position: relative; width: 1100px; height: 760px; overflow: hidden; background: #0f1120">' + s
    s += note(600, 30, 300, '<b>①</b> 意図 (いちばん読ませたい数字) が足元の札の3段目・22px。名前・筋力・HP と同じ箱の中で埋もれる', (700, 105, 640, 335))
    s += note(24, 100, 250, '<b>②</b> 目線: 手札 → 敵の絵 → (下へ) 札 → 意図、の往復。本家は 絵 → すぐ上の意図 の一往復', (150, 190, 330, 260))
    s += note(24, 330, 250, '<b>③</b> 頭上には狙っている敵の▼だけ。頭上の余白が空いている', (150, 330, 640, 150))
    s += '<img src="current-quad.jpg" style="position: absolute; left: 600px; top: 528px; width: 480px; height: 222px; display: block; border-radius: 4px; box-shadow: 0 0 0 1.5px #3b2f2f">'
    s += note(24, 560, 540, '<b>④</b> 4体では札が 104 幅に縮み、rider (負傷1・筋力+1) が切れる。数字は 22px のまま絵 (32) と並ぶので、4体のうち誰が殴ってくるかを読むのに全部の札を見る')
    s += caption(24, 4, '現状 案C「帳面の一行」(2026-09-15)。スマホ・探り屋の三人組 (右下は小泥の大群)')
    s += close()
    return s + close()

# ================================================================ 比較と原則
def board_compare():
    rows = [
        ('意図の位置', '頭のすぐ上 (紙の札)', '頭のすぐ上 (看板・名前つき)', '頭のすぐ上 (紙なし・縁取り)', '足元の札の3段目'),
        ('意図の絵と数字', '32＋24 (ボス・PC は 44＋32)', '30＋22', '40＋30', '32＋22'),
        ('名前', '足元の帳面の1段目', '看板の上の段', '足元に縁取りの小文字', '足元の帳面の1段目'),
        ('HP・盾', '足元。盾は HP バー左端 (本家形)', '足元 (薄い札)', '足元の HP バーだけ', '足元。盾は名前の行のピル'),
        ('状態・特性・予告', '帳面 (名前の行／HP の下)', '足元の札の2段目 (特性・予告はタップ)', 'タップの説明にしか置けない', '帳面 (PC は4段目)'),
        ('4体', '札 72〜110・rider 1つまで', '看板が 130 に縮む', '絵と数字だけなので余裕', '札 104・rider が切れる'),
        ('ボス', '頭 114 の上に収まる', '頭に重なる (62 高)', '収まる', '—'),
        ('紙の面積', '中 (札 小＋帳面 2段)', '大 (看板＋札)', '小 (HP バーだけ)', '中 (帳面 3段)'),
        ('本家との距離', '近い (意図↑ HP↓)', '近い', '同じ', '遠い (絵の下に全部)'),
        ('実装', '中: 意図の描画を帳面から頭上へ・帳面 2段・盾の位置', '中: 2026-09-14 の吹き出しを戻す＋帳面を薄く', '小〜中: 紙を消す。rider の説明を全部タップへ', '—'),
    ]
    s = '<div style="position: relative; width: 1120px; height: 900px; overflow: hidden; background: %s; color: %s">' % (PAPER, INK)
    s += '<div class="abs deco" style="left: 30px; top: 18px; font-size: 24px">敵の表示 — 3案の比較と原則</div>'
    cols = [(30, 170), (200, 240), (450, 230), (690, 220), (920, 190)]
    heads = ['', 'A 頭上に意図', 'B 頭上に看板', 'C 紙なしで浮かせる', '現状 帳面の一行']
    y = 64
    for j, h in enumerate(heads):
        s += '<div class="abs deco" style="left: %dpx; top: %dpx; width: %dpx; font-size: 16px; border-bottom: 1.5px solid %s; padding-bottom: 6px">%s</div>' % (cols[j][0], y, cols[j][1] - 10, INK, h)
    y += 40
    for r in rows:
        for j, cell in enumerate(r):
            s += '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; font-size: 14px; line-height: 19px; %s">%s</div>' % (cols[j][0], y, cols[j][1] - 10, 'color: %s' % INK_SOFT if j == 0 else '', cell)
        y += 46
    y += 10
    s += '<div class="abs deco" style="left: 30px; top: %dpx; font-size: 18px">敵の表示の原則 (どの案でも)</div>' % y
    rules = [
        '① 意図は敵の顔の隣に。目線が敵の絵で止まる場所＝頭のすぐ上。絵→数字の順に、手札と一往復で読めること',
        '② HP は足元 (本家)。盾は HP バーに寄せる (「盾を差し引いた被ダメ」の計算が同じ場所で済む)',
        '③ 予告 (HP半分で行動が変わる) は HP バーの目盛りと同じ札に。意図の隣には置かない (いまの行動と混ざる)',
        '④ 状態 (筋力・延焼・急所…) は名前の隣、静的な特性 (装甲・とげ…) も名前の隣。どちらも「今の行動」より一段小さく',
        '⑤ 狙っている／行動中の敵は、絵の縁ではなく札の縁 (真鍮) で示す。頭上と足元の両方が光れば絵を挟んで読める',
        '⑥ 1体でも4体でも同じ形。4体で溢れる情報は「タップの説明」へ落とし、意図の絵と数字だけは絶対に切らない',
        '⑦ 確認の窓 (発動/温存) は行動する敵の意図の札から立ち上がる (いまは帳面から)',
    ]
    for i, r in enumerate(rules):
        s += '<div class="abs" style="left: 30px; top: %dpx; width: 1060px; font-size: 14px; line-height: 20px">%s</div>' % (y + 30 + i * 24, r)
    y += 30 + len(rules) * 24 + 12
    s += '<div class="abs" style="left: 30px; top: %dpx; width: 1060px; background: %s; box-shadow: 0 0 0 1.5px %s; border-radius: 8px; padding: 10px 14px; font-size: 14px; line-height: 20px; box-sizing: border-box"><b>推奨は A</b>。意図を頭上に戻して読み方向を本家と揃え、足元の帳面は 名前＋状態／HP の2段に痩せる (予告はボスのように必要な時だけ3段目)。C は舞台がいちばん見えるが、rider・特性・予告の置き場がタップの説明しか無くなる＝実値公開の情報量を捨てる。B は看板が重い (2026-09-14 に一度やめた形)</div>' % (y, PAPER3, INK)
    return s + '</div>'

if __name__ == '__main__':
    write('Main', board_a_trio())
    write('ABoss', board_a_boss()); write('AQuad', board_a_quad()); write('APC', board_a_pc())
    write('B', board_b_trio()); write('C', board_c_trio())
    write('Current', board_current()); write('Compare', board_compare())
    import json
    canvas = dict(
        artboards=[
            dict(file='Current.dc.html', x=0, y=0, w=1100, h=760, title='現状 帳面の一行'),
            dict(file='Main.dc.html', x=1560, y=0, w=W, h=H, title='案A 頭上に意図・足元に名前とHP (採択)'),
            dict(file='ABoss.dc.html', x=1560, y=800, w=W, h=H, title='案A ボス'),
            dict(file='AQuad.dc.html', x=1560, y=1600, w=W, h=H, title='案A 4体'),
            dict(file='APC.dc.html', x=3120, y=0, w=1920, h=1080, title='案A PC'),
            dict(file='B.dc.html', x=0, y=900, w=W, h=H, title='案B 頭上に看板'),
            dict(file='C.dc.html', x=0, y=1700, w=W, h=H, title='案C 紙なしで浮かせる'),
            dict(file='Compare.dc.html', x=3120, y=1200, w=1120, h=900, title='比較と原則'),
        ],
        annotations=[dict(id='brief', x=0, y=-140, w=1000, text='敵の表示 (2026-09-16 ユーザー「敵の行動は敵の上に表示したほうがわかりやすい。敵の表示についてよく考えて」)\n意図を頭上へ戻す3つの形。下地は UI を消した実機のスクショ、絵の位置は実装値。色はカラーテーマ「黒鉄と真鍮」。\n裁定 (ask_user 3件): 案A／盾は HP バーの左端／予告は HP バーの下 → 同日 Unity に実装 (BattleScreen.IntentTag・EnemyStripH・BlockShield・ForecastLine)。')],
        launch=dict(view='canvas'),
    )
    with open(os.path.join(OUT, 'canvas.json'), 'w', encoding='utf-8') as f: json.dump(canvas, f, ensure_ascii=False, indent=1)
    print('ok')

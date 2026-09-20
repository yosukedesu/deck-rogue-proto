# build.py — 人形の盤面表示 (2026-09-19 ユーザー「人形は戦場の盤面にも表示するようにしたい」)。
# ひなたの人形 (白の従者＝置物トークン) はいま「置物の付箋」(紙の帯) にしかいない。舞台 (HD-2D ジオラマ) に立たせる置き場を3案で描く。
# 現状 (スマホ 1462×675) の指摘 ＋ 3案 (A 灯りの列＝ひなたの前・道に沿って／B 足元の従者＝ひなたの後ろと足元／C 役割で前後＝攻める人形は前・守る人形は後ろ)
# ＋ 案A の 敵2体・PC ＋ 演出と情報 (点灯・行動・灯が消える・タップ・束ね方) ＋ 比較と原則。
# 下地は実機のスクショ (`STATE="phase=combat;leader=leader_white;deck=deck_horde_v2;enemy=enc_probe_trio;perms=…;wait=2.5" scripts/unity-win.sh shots state`。
# hideui=1 の版を紙の帯の差し替えに使う)。座標はキャンバス単位 (スマホ: shot 1920×886 → 1462×675 = ×0.7616)。
# 人形の絵は仮＝白のカードの挿絵 (80×48) から GrabCut で切り出した図 (doll-*.png)。本番は PixelLab で 32×32 (ひなたの半分の背丈) を11体作る。
# 使い方: python3 build.py → *.dc.html → node ../../../scripts/design-canvas/seed-canvas.mjs で束ねる (README 参照)。
import base64, os
OUT = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(OUT, '..', '..', '..'))
ART_DIR = os.path.join(REPO, 'unity', 'Assets', 'Resources', 'Art')

# カラーテーマ「黒鉄と真鍮」(docs/color-theme.md)
INK = '#2f2e35'; INK_SOFT = '#4e4c55'; PAPER = '#f4ecd6'; PAPER2 = '#eadfc4'; PAPER3 = '#fbf6e8'
BRASS = '#c99a3a'; BRASS_LIGHT = '#ead08a'; BRASS_INK = '#634410'; MANA = '#3aa79b'; MANA_LIGHT = '#b5ddd6'; MANA_INK = '#155650'
ROSE = '#c9635a'; SKY = '#6f95b8'; SKY_LIGHT = '#d6e6fa'; SKY_INK = '#2f5a7a'; PLUM = '#9d86bf'; PLUM_LIGHT = '#e9def3'; PLUM_INK = '#5a3d78'; BAD_INK = '#9c3a2a'
MOSS = '#7fa86c'; MOSS_INK = '#276a34'; MOSS_LIGHT = '#cfeacc'

def uri(path):
    with open(path, 'rb') as f: return 'data:image/png;base64,' + base64.b64encode(f.read()).decode('ascii')
def icon(name): return uri(os.path.join(ART_DIR, 'icons', name + '.png'))
ICON = {k: icon(k) for k in ['sword', 'shield', 'heart', 'intent_attack', 'intent_defend', 'intent_heal', 'intent_destroy-token', 'draw', 'set', 'crest_permanent', 'star']}
CARD_ART = {k: uri(os.path.join(ART_DIR, 'cards', k + '.png')) for k in ['white_perm_warcry', 'white_perm_squire']}
# 人形の絵 (仮): 挿絵の切り出し。剣＝小さな人形の図で代用 (剣の人形の挿絵は坑道の奥に小さく描かれていて切り出せない)
from PIL import Image
DOLL_FILES = {'sword': 'doll-page.png', 'hound': 'doll-hound.png', 'archer': 'doll-archer.png', 'shield': 'doll-shieldmaiden.png', 'heal': 'doll-choir.png', 'candle': 'doll-candle.png', 'bell': 'doll-bandleader.png'}
DOLL = {}
for k, f in DOLL_FILES.items():
    im = Image.open(os.path.join(OUT, f)); DOLL[k] = dict(uri=uri(os.path.join(OUT, f)), w=im.width, h=im.height)
DOLL_NAME = {'sword': '剣の人形', 'hound': '犬の人形', 'archer': '弩の人形', 'shield': '盾の人形', 'heal': '癒しの人形', 'candle': '燭の人形', 'bell': '大鐘の人形'}
# 足元の札 (絵＋数字)。剣＝毎T 2ダメ／犬＝攻撃ごと 2ダメ／弩＝毎T 全体1／盾＝毎T ブロック2／癒し＝毎T 回復2
DOLL_TAG = {'sword': ('sword', '2'), 'hound': ('sword', '2'), 'archer': ('sword', '1<span style="font-size: 10px; margin-left: 1px">全</span>'), 'shield': ('shield', '2'), 'heal': ('heart', '2'), 'candle': ('heart', '1'), 'bell': ('shield', '2')}
TAG_INK = {'sword': INK, 'shield': SKY_INK, 'heart': MOSS_INK}

W, H = 1462, 675
UNIT_PH = 4 * 0.6 / 1.3132     # 1ドットの画面幅 (スマホのキャンバス px)。ArtScale 0.6・shot→canvas ×0.7616
UNIT_PC = 4.0
DOT_H = 32                     # 人形の背丈 (ドット)。ひなた (見えている部分 ≈56ドット) の半分強
FIG_H = 38.0                   # 切り出した図のおおよその高さ (px) → これを 32 ドットとみなす

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
def ic(name, size=16, extra=''):
    return '<img class="px" src="%s" style="width: %dpx; height: %dpx; display: block; flex: none; %s">' % (ICON[name], size, size, extra)
def note(x, y, w, html, arrow=None, z=60):
    s = '<div class="abs note" style="left: %dpx; top: %dpx; width: %dpx; z-index: %d">%s</div>' % (x, y, w, z, html)
    if arrow:
        ax, ay, bx, by = arrow
        s += ('<svg class="abs" style="left: 0; top: 0; overflow: visible; pointer-events: none; z-index: %d" width="1" height="1"><line x1="%d" y1="%d" x2="%d" y2="%d" stroke="#9c3a2a" stroke-width="2.5" stroke-dasharray="6 4"></line><circle cx="%d" cy="%d" r="5" fill="#9c3a2a"></circle></svg>' % (z, ax, ay, bx, by, bx, by))
    return s
def caption(x, y, text): return '<div class="abs cap" style="left: %dpx; top: %dpx; z-index: 60">%s</div>' % (x, y, text)
def patch(bg, x, y, w, h, bw=W, bh=H, z=1):
    """下地 (UIなし) の同じ場所を貼って、その上の UI を消す"""
    return '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; background: url(%s) -%dpx -%dpx / %dpx %dpx no-repeat; z-index: %d"></div>' % (x, y, w, h, bg, x, y, bw, bh, z)

# ---- 紙の部品 ----
def sheet(x, y, w, h, bg=PAPER2, edge=None, r='8px 10px 8px 9px / 9px 8px 10px 8px', z=None, extra=''):
    ring = '' if not edge else ', 0 0 0 4px %s' % edge
    return '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; background: %s; border-radius: %s; box-shadow: 0 0 0 1.5px %s%s, 0 3px 8px rgba(0,0,0,0.4); box-sizing: border-box; %s%s">' % (x, y, w, h, bg, r, INK, ring, extra, ('z-index: %d;' % z) if z is not None else '')
def pill(text, ink=INK, bg=PAPER2, size=13, h=20, icon_name=None, gap=3):
    return '<span style="display: inline-flex; align-items: center; gap: %dpx; height: %dpx; padding: 0 7px 0 5px; border-radius: 6px; background: %s; color: %s; box-shadow: 0 0 0 1px %s; font-size: %dpx; white-space: nowrap; flex: none">%s%s</span>' % (gap, h, bg, ink, ink, size, ic(icon_name, 13) if icon_name else '', text)
def stamp(cx, cy, text, bg=BRASS_LIGHT, ink=BRASS_INK, edge=BRASS, size=18, rot=-6, z=40):
    """判 (Tween.Stamp): 紙に太い縁・少し傾く"""
    return '<div class="abs deco" style="left: %dpx; top: %dpx; transform: translate(-50%%, -50%%) rotate(%ddeg); background: %s; color: %s; border: 2.5px solid %s; border-radius: 6px; padding: 2px 10px; font-size: %dpx; letter-spacing: 0.1em; white-space: nowrap; box-shadow: 0 3px 8px rgba(0,0,0,0.4); z-index: %d">%s</div>' % (cx, cy, rot, bg, ink, edge, size, z, text)

# ---- 人形 ----
def doll(kind, feet_x, feet_y, unit=UNIT_PH, z=None, extra='', tag=True, tag_ink=None, tag_num=None, dim=False, glow=None, edge=None, dx=0):
    """kind の人形を足元 (feet_x, feet_y) に立てる。unit=1ドットの px。tag=足元の小さな札 (絵＋数字)。dim=灯が消えた。glow=足元の光 (色)。edge=選択の縁"""
    d = DOLL[kind]
    sc = DOT_H / FIG_H * unit
    w, h = d['w'] * sc, d['h'] * sc
    zi = z if z is not None else int(feet_y)
    s = ''
    # 接地影 (楕円。横幅 0.8 倍)
    s += '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; margin-left: %dpx; border-radius: 50%%; background: rgba(10,14,40,0.45); z-index: %d"></div>' % (feet_x + dx, feet_y - h * 0.06, w * 0.8, h * 0.16, -w * 0.4, zi - 1)
    if glow:
        s += '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; margin-left: %dpx; border-radius: 50%%; background: radial-gradient(closest-side, %s, rgba(0,0,0,0)); z-index: %d"></div>' % (feet_x + dx, feet_y - h * 0.9, w * 2.2, h * 1.4, -w * 1.1, glow, zi - 2)
    style = 'left: %dpx; top: %dpx; width: %dpx; height: %dpx; margin-left: %dpx; z-index: %d; %s' % (feet_x + dx, feet_y - h, w, h, -w / 2, zi, extra)
    if dim: style += 'filter: grayscale(1) brightness(0.55); opacity: 0.8;'
    s += '<img class="abs px" src="%s" style="%s">' % (d['uri'], style)
    if edge:
        s += '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; margin-left: %dpx; border-radius: 50%%; box-shadow: 0 0 0 2.5px %s; z-index: %d"></div>' % (feet_x + dx, feet_y - h * 0.10, w * 1.1, h * 0.24, -w * 0.55, edge, zi - 1)
    if tag:
        icn, num = DOLL_TAG[kind]
        if tag_num is not None: num = tag_num
        ink = tag_ink or TAG_INK[icn]
        tw, th, isz, fsz = (38, 18, 12, 12) if unit < 3 else (48, 24, 16, 16)
        s += '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; margin-left: %dpx; background: %s; border-radius: 5px; box-shadow: 0 0 0 1px %s, 0 2px 4px rgba(0,0,0,0.4); display: flex; align-items: center; justify-content: center; gap: 2px; font-size: %dpx; font-weight: 700; color: %s; z-index: %d">%s%s</div>' % (feet_x + dx, feet_y + 2, tw, th, -tw // 2, PAPER2, INK, fsz, ink, zi + 20, ic(icn, isz), num)
    return s

# ---- 位置 (スマホ・キャンバス単位) ----
LEADER_PH = dict(cx=327, feet=306, top=180)            # ひなた
BOX_PH = (426, 283, 476, 327)                          # からくりの匣
OWL_PH = [dict(cx=899, feet=293, top=156), dict(cx=1077, feet=267, top=149), dict(cx=1287, feet=255, top=152)]
LINE_PH = 365                                          # 帳面の下端 (StatusLine)
def road_ph(x): return 306 - (x - 327) * 13.0 / 572.0
# 座席: 道に沿って t=-2.9 … 0.5 (0.85 刻み)。ひなた t=-5 (x=327)・敵① t=2.2 (x=899) → 1unit ≈ 79.4px
def seat_ph(t, s=0.0):
    x = 327 + (t + 5.0) * 79.4 + s * 28
    return x, road_ph(x) - s * 24
FRONT_T = [-2.9, -2.0, -1.1, -0.2, 0.7]
def front_seats_ph(n):
    """前列 n 体: 手前 (s=-0.35) と奥 (s=+0.5) を交互に＝一直線を崩す"""
    return [seat_ph(FRONT_T[i], (-0.35 if i % 2 else 0.5)) for i in range(n)]
def back_seats_ph(n):
    """後列 (前列より奥・右に少し)"""
    return [seat_ph(FRONT_T[i] + 0.05, 1.3) for i in range(n)]
# PC
LEADER_PC = dict(cx=425, feet=618, top=395)
BOX_PC = (565, 595, 635, 650)
OWL_PC = [dict(cx=1178, feet=585, top=372), dict(cx=1399, feet=536, top=322), dict(cx=1714, feet=518, top=305)]
def road_pc(x): return 618 - (x - 425) * 33.0 / 753.0
def seat_pc(t, s=0.0):
    x = 425 + (t + 5.0) * 104.6 + s * 40
    return x, road_pc(x) - s * 36
FRONT_T_PC = [-2.9, -1.9, -0.9, 0.1]
def front_seats_pc(n): return [seat_pc(FRONT_T_PC[i], (-0.35 if i % 2 else 0.5)) for i in range(n)]
def back_seats_pc(n): return [seat_pc(FRONT_T_PC[i] + 0.1, 1.3) for i in range(n)]

SIX = ['sword', 'sword', 'hound', 'archer', 'shield', 'heal']   # 見本の6体 (点灯した順)
ATTACKERS = ['sword', 'sword', 'hound', 'archer']; SUPPORTERS = ['shield', 'heal']

# ---- UI の部品 (スマホ) ----
def band_ph(perm_count=1, pockets=2, x=30, y=62):
    """上の帯: からくり (ポケット) ＋ 置物 (人形以外の付箋)。人形は舞台に立つので帯からは消える"""
    s = '<div class="abs" style="left: %dpx; top: %dpx; font-size: 13px; color: %s; text-shadow: 0 0 3px #000, 0 0 3px #000; letter-spacing: 0.08em; z-index: 5">からくり 0 / %d</div>' % (x, y + 4, PAPER, pockets)
    for i in range(pockets):
        s += '<div class="abs" style="left: %dpx; top: %dpx; width: 68px; height: 74px; border-radius: 8px; background: rgba(120,110,100,0.45); box-shadow: 0 0 0 1.5px rgba(244,236,214,0.35) inset; z-index: 5"></div>' % (x + i * 80, y + 26)
    px = x + pockets * 80 + 14
    s += '<div class="abs" style="left: %dpx; top: %dpx; font-size: 13px; color: %s; text-shadow: 0 0 3px #000, 0 0 3px #000; letter-spacing: 0.08em; z-index: 5">置物 %d</div>' % (px, y + 4, PAPER, perm_count)
    s += perm_chip(px, y + 26, '灯り増し', 'white_perm_warcry', z=5)
    return s
def perm_chip(x, y, name, art, w=168, h=40, z=None):
    s = sheet(x, y, w, h, PAPER2, z=z)
    s += '<img class="px" src="%s" style="position: absolute; left: 6px; top: 5px; width: 48px; height: 29px; display: block; border-radius: 3px; box-shadow: 0 0 0 1px %s">' % (CARD_ART[art], INK)
    s += '<div class="abs deco" style="left: 62px; top: 0; height: %dpx; display: flex; align-items: center; font-size: 15px; font-weight: 400">%s</div>' % (h, name)
    return s + '</div>'
def doll_count_pill(x, y, n, unit_note=False):
    return '<div class="abs" style="left: %dpx; top: %dpx; z-index: 30">%s</div>' % (x, y, pill('人形 %d' % n, BRASS_INK, BRASS_LIGHT, icon_name='crest_permanent'))

# ================================================================ 現状
def board_current():
    s = board('cur-phone.jpg')
    s += note(560, 50, 330, '<b>①</b> 点灯した人形6体はここ＝紙の帯の付箋。3枚見えて残りは「+3 …」。<b>盤面のどこにも人形はいない</b>', (560, 100, 440, 150))
    s += note(560, 215, 300, '<b>②</b> ひなたと敵の間の道は空いている。「灯列」「行列」と名の付く札を出しても列は現れない', (700, 260, 620, 300))
    s += note(1060, 560, 380, '<b>③</b> 犬の人形・剣の人形の攻撃は、敵の上に数字だけが出る＝<b>誰が殴ったか</b>が絵に無い。盾・回復も同じ (ひなたに数字が出るだけ)', (1200, 560, 1000, 330))
    s += note(30, 500, 280, '<b>④</b> 敵の「人形壊し」(destroy-token) は付箋が1枚消えるだけ。ホードの「数」の気持ちよさも、壊される痛みも見えない', None)
    s += caption(24, 648, '現状 (スマホ 1462×675・ひなた・人形6体＋灯り増し・探り屋の三人組)。人形＝置物の付箋')
    return s + close()

# ================================================================ 案A 灯りの列 (ひなたの前・道に沿って)
def draw_dolls_on(seats, kinds, unit=UNIT_PH, **kw):
    """奥 (feet が小さい) から先に描く"""
    items = sorted(zip(seats, kinds), key=lambda p: p[0][1])
    return ''.join(doll(k, x, y, unit=unit, **kw) for (x, y), k in items)

def board_a():
    s = board('cur-phone.jpg')
    s += patch('base-phone.jpg', 30, 60, 700, 120)          # 付箋の帯を消す
    s += band_ph(perm_count=1)
    seats = front_seats_ph(5) + back_seats_ph(1)
    s += draw_dolls_on(seats, SIX)
    s += note(900, 40, 320, '<b>前列</b>: ひなたと敵①の間の道に、点灯した順に並ぶ (t=-2.9…0.7・0.9 刻み＝71px)。手前と奥を交互にずらして一直線を崩す＝敵の座席と同じ作法', (900, 90, 712, 262))
    s += note(540, 40, 330, '<b>6体目から後列</b> (奥・右に半歩)。前5＋後4＝上限9、それを超えたら最後の人形の札が「+N」', (640, 105, 540, 246))
    s += note(1060, 500, 380, '<b>足元の札</b>: 絵＋数字＝「何が出るか」だけ (剣 2・盾 2・弩 1全)。輝き増しで+1 なら真鍮の数字。いつ出るか (毎ターン／攻撃ごと) はタップの説明', (1060, 540, 700, 322))
    s += note(30, 500, 300, '<b>帯からは消える</b>: 上の帯は からくり＋人形以外の置物 (灯り増し) だけ。数は舞台で数える', (150, 500, 250, 120))
    s += caption(24, 648, '案A 灯りの列 (スマホ・探り屋の三人組・人形6体)。人形＝32ドット (ひなたの半分の背丈)・足元に絵＋数字の札。絵は仮 (挿絵の切り出し)')
    return s + close()

def board_a_pair():
    s = board('base-phone-pair.jpg')
    s += band_ph(perm_count=1)
    seats = front_seats_ph(5) + back_seats_ph(1)
    s += draw_dolls_on(seats, SIX)
    # 敵の帳面と意図 (簡略)
    s += note(880, 60, 320, '<b>敵が少ない時</b>も座席は同じ (ひなたからの距離で決まる)。敵①が t=3.2 に下がるので列との間が空く', (880, 100, 830, 285))
    s += note(1000, 500, 400, '敵の「人形壊し」の意図は、狙われた人形が<b>その場で分かる</b>ようにする: 意図の札の下に「人形壊し」＋ 狙いの人形の足元に朱の輪 (実処理は実行時のランダム＝表示は「どれかが壊れる」で全員に薄い輪)', None)
    s += caption(24, 648, '案A 敵2体 (蜘蛛と噛みつき果実)。UI は帯と人形だけ描いた下地')
    return s + close()

def board_a_pc():
    Wp, Hp = 1920, 1080
    s = board('cur-pc.jpg', Wp, Hp)
    s += '<div class="abs" style="left: 612px; top: 652px; width: 222px; height: 122px; background: #eadec4; z-index: 1"></div>'    # 帳面の置物の段を消す (紙の色で塗る)
    s += '<div class="abs" style="left: 620px; top: 660px; font-size: 13px; color: %s; z-index: 5">置物 1</div>' % INK_SOFT
    s += perm_chip(620, 680, '灯り増し', 'white_perm_warcry', 200, 34, z=5)
    s += '<div class="abs" style="left: 620px; top: 728px; z-index: 5">%s</div>' % pill('人形 6 → 舞台', BRASS_INK, BRASS_LIGHT, icon_name='crest_permanent')
    seats = front_seats_pc(4) + back_seats_pc(2)
    s += draw_dolls_on(seats, SIX, unit=UNIT_PC)
    s += note(1230, 130, 340, '<b>PC</b>: 人形は 32×32 ドット＝128px (ひなた 224px の半分強)。前列は4体 (t=-2.9…0.1・1.0 刻み＝105px)、5体目から後列 (奥・右に半歩)。手前と奥の交互で少し重なる', (1300, 210, 1010, 560))
    s += note(60, 400, 320, '匣 (からくり) は今の場所 (t=-3.7・手前) のまま。前列は t=-2.9 から始まるので重ならない', (200, 470, 600, 622))
    s += note(1200, 830, 420, '帳面の置物の段は「人形以外」だけ。人形の数は舞台で数え、札「人形 6」を押すと名前の一覧 (＝いまの「+N …」と同じ窓)', (1200, 850, 830, 730))
    s += caption(24, 1050, '案A PC (1920×1080)。前列4＋後列2・足元の札 48×24')
    return s + close()

# ================================================================ 案B 足元の従者 (ひなたの後ろ・足元)
B_SEATS_PH = [(215, 284), (255, 280), (350, 282), (300, 340), (385, 338), (240, 336)]
def board_b():
    s = board('cur-phone.jpg')
    s += patch('base-phone.jpg', 30, 60, 700, 120)
    s += band_ph(perm_count=1)
    # ひなたは人形より手前に描かれる＝後ろの人形は隠れる。下地の絵に重なる分は描画順で下 (z<300)
    s += draw_dolls_on(B_SEATS_PH, SIX, z=None)
    # ひなたの絵を上に重ね直す (下地から切り抜いて貼る)
    s += '<div class="abs" style="left: 280px; top: 176px; width: 100px; height: 134px; background: url(base-phone.jpg) -280px -176px / 1462px 675px no-repeat; z-index: 300"></div>'
    # 自分の札 (下地の UI) を上に (人形が札の下に潜る)
    s += '<div class="abs" style="left: 24px; top: 290px; width: 228px; height: 80px; background: url(cur-phone.jpg) -24px -290px / 1462px 675px no-repeat; z-index: 350"></div>'
    s += note(560, 190, 320, '<b>後ろと足元</b>: ひなたを囲む (t=-5.6…-4.4・左右 ±1.4)。小さく寄り添う＝「灯りに集まる」読み', (560, 230, 380, 300))
    s += note(30, 490, 320, '<b>置き場が無い</b>: スマホは左に自分の札 (HP・被ダメ)、右に匣。6体目は自分の札の下に潜り、ひなたの真後ろの1体は体に隠れる', (150, 490, 240, 340))
    s += note(1060, 500, 380, '攻撃の筋は<b>ひなたの後ろから</b>敵へ飛ぶ。盾・回復はすぐ隣なので短い。「数」は塊で見えるが誰が何かは読めない (札は重なる)', None)
    s += caption(24, 648, '案B 足元の従者 (スマホ・人形6体)。ひなたの後ろ・足元に寄せる')
    return s + close()

# ================================================================ 案C 役割で前後 (攻める人形は前・守る人形は後ろ)
def board_c():
    s = board('cur-phone.jpg')
    s += patch('base-phone.jpg', 30, 60, 700, 120)
    s += band_ph(perm_count=1)
    front = front_seats_ph(4)
    back = [(255, 280), (215, 284)]
    s += draw_dolls_on(front, ATTACKERS)
    s += draw_dolls_on(back, SUPPORTERS)
    s += '<div class="abs" style="left: 280px; top: 176px; width: 100px; height: 134px; background: url(base-phone.jpg) -280px -176px / 1462px 675px no-repeat; z-index: 300"></div>'
    s += note(560, 40, 330, '<b>前＝攻める人形</b> (剣・犬・弩・小さな)。案A と同じ列', (700, 96, 660, 262))
    s += note(30, 500, 320, '<b>後ろ＝守る人形</b> (盾・癒し・手当て・燭・旗・鐘)。ひなたのすぐ後ろに寄るので盾と回復の絵は短い', (150, 500, 235, 286))
    s += note(1060, 500, 380, '役割はデータに無い＝<b>効果で分ける規則</b>が要る (ダメージを与える置物＝前・それ以外＝後ろ)。敵の「人形壊し」は前後を見ない (ランダム) ので、前後に意味は生まれない', None)
    s += caption(24, 648, '案C 役割で前後 (スマホ・人形6体)。攻める4体が前、守る2体がひなたの後ろ')
    return s + close()

# ================================================================ 演出と情報 (案A の上で)
def crop_bg(bg, x, y, w, h, scale=1.5, bw=W, bh=H):
    return 'background: url(%s) -%dpx -%dpx / %dpx %dpx no-repeat' % (bg, x * scale, y * scale, bw * scale, bh * scale)
def cell(x, y, w, h, bg_css, title, inner, foot):
    s = '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; %s; border-radius: 6px; box-shadow: 0 0 0 1.5px #3b2f2f; overflow: hidden">%s</div>' % (x, y, w, h, bg_css, inner)
    s += '<div class="abs deco" style="left: %dpx; top: %dpx; font-size: 16px; color: %s">%s</div>' % (x, y - 26, PAPER, title)
    s += '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; font-size: 13px; line-height: 18px; color: %s">%s</div>' % (x, y + h + 6, w, '#c4beb2', foot)
    return s
def board_moments():
    Wm, Hm = 1462, 1110
    s = '<div style="position: relative; width: %dpx; height: %dpx; overflow: hidden; background: #0f1120">' % (Wm, Hm)
    s += '<div class="abs deco" style="left: 24px; top: 14px; font-size: 22px; color: %s">演出と情報 (案A の上で)</div>' % PAPER
    SC = 1.5
    def d(kind, fx, fy, ox, oy, **kw):   # クロップ内の座標へ (キャンバス座標 fx,fy を ox,oy 起点で 1.5 倍)
        return doll(kind, (fx - ox) * SC, (fy - oy) * SC, unit=UNIT_PH * SC, **kw)
    cw, ch = 440, 250
    R1, R2, R3 = 84, 424, 764
    # (a) 点灯 = 登場
    ox, oy = 420, 150
    inner = d('sword', 502, 290, ox, oy, glow='rgba(255,214,120,0.55)', tag=False)
    inner += stamp((502 - ox) * SC, (290 - oy) * SC - 110, '点灯', size=17)
    inner += '<div class="abs" style="left: %dpx; top: %dpx; width: 8px; height: 8px; border-radius: 50%%; background: %s; box-shadow: 0 0 8px %s"></div>' % ((520 - ox) * SC, (215 - oy) * SC, BRASS_LIGHT, BRASS_LIGHT)
    inner += '<div class="abs" style="left: %dpx; top: %dpx; width: 6px; height: 6px; border-radius: 50%%; background: %s; box-shadow: 0 0 8px %s"></div>' % ((470 - ox) * SC, (232 - oy) * SC, BRASS_LIGHT, BRASS_LIGHT)
    s += cell(24, R1, cw, ch, crop_bg('base-phone.jpg', ox, oy, cw / SC, ch / SC, SC), '① 点灯 (登場)', inner, '暗い (灰の) 人形が座席に置かれ、ひなたのランタンの色が灯って白鉄が光る＝小さく現れて弾む (召喚の子と同じ)＋足元に暖色の光の輪＋判「点灯」。駆けつけはこの直後に「駆けつけ 剣の人形」の判が重なる')
    # (b) 行動 = 犬の人形が敵へ
    ox, oy = 640, 150
    inner = d('hound', 708, 297, ox, oy, dx=16)
    x0, y0 = (708 - ox) * SC + 34, (272 - oy) * SC; x1, y1 = (880 - ox) * SC, (250 - oy) * SC
    inner += '<svg class="abs" style="left: 0; top: 0; overflow: visible; z-index: 30" width="1" height="1"><line x1="%d" y1="%d" x2="%d" y2="%d" stroke="%s" stroke-width="7" stroke-linecap="round" opacity="0.85"></line><line x1="%d" y1="%d" x2="%d" y2="%d" stroke="#ffffff" stroke-width="3" stroke-linecap="round"></line></svg>' % (x0, y0, x1, y1, MANA, x0, y0, x1, y1)
    inner += '<div class="abs deco outl" style="left: %dpx; top: %dpx; font-size: 30px; z-index: 31">2</div>' % (x1 - 6, y1 - 54)
    s += cell(494, R1, cw, ch, crop_bg('base-phone.jpg', ox, oy, cw / SC, ch / SC, SC), '② 行動 (犬の人形 → 敵)', inner, '人形が半歩踏み込み (Tween.Lunge)、斬撃の筋が<b>人形から</b>敵へ伸びて数字が出る。弩は光の玉 (Projectile)、盾は人形からひなたへ盾の面 (GuardDisc の小さい版)、回復は苔の玉。誰の仕事かが絵で読める')
    # (c) 灯が消える
    ox, oy = 420, 150
    inner = d('sword', 502, 290, ox, oy, dim=True, tag=False, extra='-webkit-mask-image: linear-gradient(to bottom, rgba(0,0,0,0) 0%, rgba(0,0,0,0) 22%, #000 55%); mask-image: linear-gradient(to bottom, rgba(0,0,0,0) 0%, rgba(0,0,0,0) 22%, #000 55%);')
    inner += stamp((502 - ox) * SC, (290 - oy) * SC - 110, '灯が消えた', '#d8d4cc', '#4e4c55', '#8a8680', size=16)
    for i, (px, py) in enumerate([(492, 228), (512, 222), (503, 238), (520, 236)]):
        inner += '<div class="abs" style="left: %dpx; top: %dpx; width: 5px; height: 5px; border-radius: 50%%; background: %s; opacity: 0.8"></div>' % ((px - ox) * SC, (py - oy) * SC, PAPER if i % 2 else BRASS_LIGHT)
    s += cell(964, R1, cw, ch, crop_bg('base-phone.jpg', ox, oy, cw / SC, ch / SC, SC), '③ 灯が消える (人形壊し・灯の捧げ)', inner, '光が抜けて灰になり、頭から崩れる (StageUnit の _Dissolve＝倒れた敵と同じ) ＋ 紙色の粒。敵に壊されたら判は灰の「灯が消えた」、灯の捧げ (自分で消す) なら真鍮の「捧げた」')
    # (d) タップの説明 (固定パネル)
    ox, oy = 380, 150
    inner = sheet(12, 12, 300, 122, PAPER3, z=40)
    inner += '<div class="abs deco" style="left: 12px; top: 8px; font-size: 16px">剣の人形 <span style="font-size: 12px; font-weight: 400; color: %s">人形 (置物)</span></div>' % INK_SOFT
    inner += '<div class="abs" style="left: 12px; top: 36px; width: 276px; font-size: 13px; line-height: 19px">毎ターン開始時に <b>2</b> ダメージ<br><span style="color: %s">輝き増しで +1 → いまは <b>3</b></span><br><span style="color: %s">敵の「人形壊し」で壊れる。灯の捧げの対価に選べる</span></div>' % (BRASS_INK, INK_SOFT)
    inner += '</div>'
    inner += d('sword', 560, 288, ox, oy, edge=BRASS, tag_num='<span style="color: %s">3</span>' % BRASS_INK)
    inner += d('shield', 630, 300, ox, oy)
    s += cell(24, R2, cw, ch, crop_bg('base-phone.jpg', ox, oy, cw / SC, ch / SC, SC), '④ タップ＝説明・対象', inner, '人形を押すと画面左上の固定パネル (敵と同じ Tooltip.ShowPinned) に名前・効果・いまの値 (輝き増し込み)。灯の捧げなど「人形を1体選ぶ」札は、人形を押して選ぶ (縁が真鍮に)')
    # (e) 束ね方: 一体ずつ vs ×N
    ox, oy = 440, 150
    inner = d('sword', 480, 300, ox, oy) + d('sword', 520, 292, ox, oy) + d('sword', 560, 300, ox, oy)
    inner += '<div class="abs" style="left: 220px; top: 0; width: 2px; height: 250px; background: rgba(244,236,214,0.35)"></div>'
    inner += d('sword', 660, 296, ox, oy, tag_num='2 <span style="font-size: 10px; margin-left: 3px; color: %s">×3</span>' % INK_SOFT)
    inner += '<div class="abs deco" style="left: 14px; top: 10px; font-size: 15px; color: %s">一体ずつ</div><div class="abs deco" style="left: 236px; top: 10px; font-size: 15px; color: %s">束ねて ×N</div>' % (PAPER, PAPER)
    s += cell(494, R2, cw, ch, crop_bg('base-phone.jpg', ox, oy, cw / SC, ch / SC, SC), '⑤ 同じ人形の束ね方', inner, '左: 点灯した順に一体ずつ (上限9・超えたら最後の札が「+N」)＝ホードの「数」がそのまま見える。右: 同じ種類は1体にまとめて札に「×N」＝整うが、分列の奇跡で倍になっても絵は増えない')
    # (f) 輝き増し (アンセム) の見え方
    ox, oy = 440, 150
    inner = d('sword', 490, 298, ox, oy, tag_num='<span style="color: %s">3</span>' % BRASS_INK, glow='rgba(255,214,120,0.35)') + d('shield', 560, 292, ox, oy, tag_num='<span style="color: %s">3</span>' % BRASS_INK, glow='rgba(255,214,120,0.35)') + d('archer', 630, 298, ox, oy, tag_num='<span style="color: %s">2</span><span style="font-size: 10px; margin-left: 1px">全</span>' % BRASS_INK, glow='rgba(255,214,120,0.35)')
    s += cell(964, R2, cw, ch, crop_bg('base-phone.jpg', ox, oy, cw / SC, ch / SC, SC), '⑥ 輝き増し (人形の効果 +N)', inner, '輝き増し・灯り増しがある間は、人形の足元の光が濃くなり、札の数字が真鍮 (2 → 3)。「置く前と後で別ゲーム」(白 Opus) が絵で分かる。値は表示と実処理が同じ式 (blessRetainers を読む)')
    # (g) 大きさ: 32 ドット vs 40 ドット (ひなたと並べて)
    ox, oy = 250, 150
    inner = ''
    global DOT_H
    keep = DOT_H
    DOT_H = 32; inner += d('sword', 440, 300, ox, oy) + d('shield', 500, 296, ox, oy)
    DOT_H = 40; inner += d('sword', 600, 300, ox, oy) + d('shield', 665, 296, ox, oy)
    DOT_H = keep
    inner += '<div class="abs deco" style="left: 250px; top: 10px; font-size: 15px; color: %s">32 ドット (推奨)</div><div class="abs deco" style="left: 500px; top: 10px; font-size: 15px; color: %s">40 ドット</div>' % (PAPER, PAPER)
    s += cell(24, R3, cw + 220, ch, crop_bg('base-phone.jpg', ox, oy, (cw + 220) / SC, ch / SC, SC), '⑦ 大きさ (ひなたと並べて)', inner, '32 ドット＝ひなた (見えている部分 ≈56 ドット) の半分強＝「小さな人形」。40 ドットは敵 (64) の 2/3 で人形というより小柄な兵。PixelLab の下限は 32×32 なのでどちらも作れる。スマホは ×0.6 なので 32 ドットは 58px (指の的としては札で補う)')
    # (h) 人形壊しの狙い
    ox, oy = 640, 150
    inner = d('sword', 708, 297, ox, oy, edge='rgba(201,99,90,0.9)') + d('archer', 780, 292, ox, oy, edge='rgba(201,99,90,0.9)')
    inner += sheet((880 - ox) * SC - 70, 40, 140, 44, PAPER, edge=None, z=40) + '<div class="abs" style="inset: 0; display: flex; align-items: center; justify-content: center; gap: 6px">%s<span class="deco" style="font-size: 15px; font-weight: 400">人形壊し</span></div></div>' % ic('intent_destroy-token', 26)
    s += cell(714, R3, cw + 220, ch, crop_bg('base-phone.jpg', ox, oy, (cw + 220) / SC, ch / SC, SC), '⑧ 敵の「人形壊し」の予告', inner, '意図の札が「人形壊し」の時、生きている人形の足元に薄い朱の輪 (どれかが壊れる＝実処理は実行時のランダム)。実行の瞬間に選ばれた1体だけ輪が濃くなって③へ')
    s += caption(24, 1082, '演出は既存の器 (Tween.Lunge／SlashFx／Projectile／GuardDisc／Stamp／StageUnit._Dissolve) を人形の座席に向ける。新しい絵は人形のドット (11体・32×32) だけ')
    return s + '</div>'

# ================================================================ 比較と原則
def board_compare():
    rows = [
        ('置き場', 'ひなたと敵①の間の道 (t=-2.9…0.7)', 'ひなたの後ろと足元 (t=-5.6…-4.4)', '攻める人形は道・守る人形は後ろ', '紙の帯の付箋 (舞台にいない)'),
        ('読み (誰が何をするか)', '一列に並ぶ＋足元の札＝1体ずつ読める', '塊。重なって札は読めない', '前後で役割は読めるが規則を知る必要', '名前だけ (3枚まで)'),
        ('演出の飛び先', '人形→敵 (攻撃)・人形→ひなた (盾・回復) が長さのある筋になる', '攻撃はひなたの後ろから飛ぶ (誰の筋か曖昧)', '攻撃は前から・守りは後ろから＝最も自然', '無い (数字だけ)'),
        ('数が増えた時', '前5＋後4＝9体まで。超えたら「+N」', '6体で置き場が尽きる (札と匣に挟まれる)', '前4＋後2〜3', '「+N …」'),
        ('スマホ', '道は空いている。敵①の帳面には掛からない', '自分の札 (左下) と匣 (右) に挟まれる', '前は A と同じ・後ろは B と同じ狭さ', '帯が敵の意図に近い'),
        ('世界の読み', '灯列・行列＝札の名前どおり。灯りが前を照らす', '灯りに集まる従者', '前衛と後衛', '—'),
        ('敵の人形壊し', '前列のどれかが灰になって崩れる＝見える', '塊の中の1体', '前後どちらでも', '付箋が1枚消える'),
        ('実装', '座席 Stage.DollSlots＋BindUnit (敵と同じ)・足元の札・タップ・演出の向き', '座席だけ違う', 'A＋効果から役割を引く規則', '—'),
    ]
    s = '<div style="position: relative; width: 1120px; height: 960px; overflow: hidden; background: %s; color: %s">' % (PAPER, INK)
    s += '<div class="abs deco" style="left: 30px; top: 18px; font-size: 24px">人形の盤面表示 — 3案の比較と原則</div>'
    cols = [(30, 150), (180, 250), (430, 230), (660, 230), (890, 220)]
    heads = ['', 'A 灯りの列 (前)', 'B 足元の従者 (後ろ)', 'C 役割で前後', '現状 付箋']
    y = 64
    for j, h in enumerate(heads):
        s += '<div class="abs deco" style="left: %dpx; top: %dpx; width: %dpx; font-size: 16px; border-bottom: 1.5px solid %s; padding-bottom: 6px">%s</div>' % (cols[j][0], y, cols[j][1] - 10, INK, h)
    y += 40
    for r in rows:
        for j, cellt in enumerate(r):
            s += '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; font-size: 14px; line-height: 19px; %s">%s</div>' % (cols[j][0], y, cols[j][1] - 10, 'color: %s' % INK_SOFT if j == 0 else '', cellt)
        y += 50
    y += 30
    s += '<div class="abs deco" style="left: 30px; top: %dpx; font-size: 18px">人形の表示の原則 (どの案でも)</div>' % y
    rules = [
        '① 人形は舞台の住人＝敵と同じ器 (座席→ProjectFeet→BindUnit・接地影・呼吸・崩れ)。紙の UI (付箋) では二重に描かない',
        '② 大きさは 32×32 ドット (4px/ドット・スマホは 0.6)＝ひなたの半分の背丈。「小さな真鍮の人形」が絵で分かる。奥行きで縮めない (本家と同じ)',
        '③ 点灯した順に座席を埋め、消えたら詰めない (倒れた敵と同じ)。上限を超えた分は最後の札に「+N」、押せば一覧',
        '④ 足元の札は「何が出るか」の絵＋数字だけ。いつ出るか・条件・輝き増しの内訳はタップの固定パネル',
        '⑤ 演出は人形から出る (攻撃の筋・光の玉・盾の面・回復の玉)。点灯＝登場、灯が消える＝破壊。判は人形の頭上',
        '⑥ 人形はタップの的 (説明・「人形を1体選ぶ」札の対象)。敵と同じ Button＋Tooltip',
        '⑦ 人形以外の置物 (灯り増し・輝き増し・大灯台…) は今までどおり付箋。人形だけが舞台へ出る (生き物の置物＝retainer:true)',
    ]
    for i, r in enumerate(rules):
        s += '<div class="abs" style="left: 30px; top: %dpx; width: 1060px; font-size: 14px; line-height: 20px">%s</div>' % (y + 30 + i * 24, r)
    y += 30 + len(rules) * 24 + 12
    s += '<div class="abs" style="left: 30px; top: %dpx; width: 1060px; background: %s; box-shadow: 0 0 0 1.5px %s; border-radius: 8px; padding: 10px 14px; font-size: 14px; line-height: 20px; box-sizing: border-box"><b>推奨は A</b>。空いている道に一列で並び、1体ずつ読めて、攻撃の筋が「人形から敵へ」伸びる。札の名前 (灯列の突き・灯りの行列・小人形の列) がそのまま絵になる。B は数の塊としては良いがスマホに置き場が無く、誰が何かが読めない。C は演出の向きは最も自然だが、データに無い「役割」の規則を足すことになり、敵の人形壊しは前後を見ないので前後に意味が生まれない。<br><b>絵</b>: PixelLab で11体 (32×32・south-east・low top-down・白鉄と真鍮・顔は暗い空洞に琥珀の目2つ＝挿絵と同じ定義)。残高が 0 USD なので発注の前に補充が要る。それまではコード生成のシルエット (Creature.Get の 32 ドット版) で配置を先に作る</div>' % (y, PAPER3, INK)
    return s + '</div>'

if __name__ == '__main__':
    write('Current', board_current())
    write('Main', board_a())
    write('APair', board_a_pair())
    write('APC', board_a_pc())
    write('B', board_b())
    write('C', board_c())
    write('Moments', board_moments())
    write('Compare', board_compare())
    import json
    canvas = {
        'artboards': [
            {'file': 'Current.dc.html', 'x': 0, 'y': 0, 'w': W, 'h': H, 'title': '現状 付箋 (舞台に人形がいない)'},
            {'file': 'Main.dc.html', 'x': 1560, 'y': 0, 'w': W, 'h': H, 'title': '案A 灯りの列 (推奨)'},
            {'file': 'APair.dc.html', 'x': 1560, 'y': 800, 'w': W, 'h': H, 'title': '案A 敵2体'},
            {'file': 'APC.dc.html', 'x': 3120, 'y': 0, 'w': 1920, 'h': 1080, 'title': '案A PC'},
            {'file': 'B.dc.html', 'x': 0, 'y': 800, 'w': W, 'h': H, 'title': '案B 足元の従者'},
            {'file': 'C.dc.html', 'x': 0, 'y': 1600, 'w': W, 'h': H, 'title': '案C 役割で前後'},
            {'file': 'Moments.dc.html', 'x': 1560, 'y': 1600, 'w': 1462, 'h': 1110, 'title': '演出と情報 (案A の上で)'},
            {'file': 'Compare.dc.html', 'x': 3120, 'y': 1200, 'w': 1120, 'h': 960, 'title': '比較と原則'},
        ],
        'annotations': [
            {'id': 'brief', 'x': 0, 'y': -150, 'w': 1100, 'text': '人形の盤面表示 (2026-09-19 ユーザー「人形は戦場の盤面にも表示するようにしたい」)\nひなたの人形 (白の従者＝置物トークン) を舞台 (HD-2D ジオラマ) に立たせる置き場を3案。下地は実機のスクショ (ひなた・人形6体＋灯り増し・探り屋の三人組)、人形の絵は仮 (挿絵の切り出し)。\n決めること: ①置き場 (A 前の列／B 後ろと足元／C 役割で前後) ②同じ人形は一体ずつか ×N か ③足元の札 (絵＋数字) を出すか ④付箋からは消すか。\n裁定 (ask_user 4件・2026-09-19): ①A だが「B との中間」(ひなたのすぐ前 t=-4.1 から) ②一体ずつ (上限9・超えたら「+N」) ③足元の札を出す ④付箋にも残す → 同日 Unity に実装 (Stage.DollSlots・BattleView.SyncDolls/KillDoll・BattleScreen.FillDollPanel/DollTip・engine の sourceUid)。絵は新規に PixelLab (docs/pixellab/dolls-stage.json・残高待ち)。'},
        ],
        'launch': {'view': 'canvas'},
    }
    with open(os.path.join(OUT, 'canvas.json'), 'w', encoding='utf-8') as f: json.dump(canvas, f, ensure_ascii=False, indent=1)
    print('ok')

# build.py — スマホ (S25 相当・キャンバス 1462×675) の戦闘画面レイアウト案。
# 2026-09-14 ユーザー「スマホだと置物が敵と重なる／伏せの表現も場所取りすぎ。戦闘画面レイアウトを考え直したい」。
# 実機と同じ画角の Unity のスクショ (伏せ場と置物を消した下地 = Autopilot の hidezone=1) の上に、置物と仕込み札の新しい表現を HTML で重ねる。
# 座標はキャンバス単位 (1 unit = 1 CSS px)。Unity 側は BattleScreen.cs の Phone 分岐がこの座標をそのまま使える。
import base64, json, os
OUT = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(OUT, '..', '..', '..'))

INK = '#3b2f2f'; PAPER = '#f4ecd6'; HONEY = '#e0b25a'; MOSS = '#3f8a4a'; GREY = '#8a8a94'

def art_uri(card_id):
    p = os.path.join(REPO, 'public', 'art', 'cards', card_id + '.png')
    with open(p, 'rb') as f: return 'data:image/png;base64,' + base64.b64encode(f.read()).decode('ascii')

ART = {k: art_uri(k) for k in ['green_reaction_vine', 'green_reaction_thorns', 'green_perm_growth_tree', 'green_perm_thorn_vine', 'green_perm_wild_call', 'green_perm_sprout_keeper']}
NAME = {'green_reaction_vine': '守りの蔓', 'green_reaction_thorns': '茨の返し', 'green_perm_growth_tree': '年輪の大樹', 'green_perm_thorn_vine': '棘の蔓', 'green_perm_wild_call': '荒野の呼び声', 'green_perm_sprout_keeper': '芽守り'}
BODY = {'green_perm_growth_tree': '毎ターン開始時: 成長+1', 'green_perm_thorn_vine': '攻撃をプレイするたび: ブロック2', 'green_perm_wild_call': '毎ターン開始時: 勢い+3', 'green_perm_sprout_keeper': '成長3以上ならターン開始時: 1ドロー',
        'green_reaction_vine': '被攻撃前: ブロック12。完全に凌げば次のターン1ドロー', 'green_reaction_thorns': '被攻撃後: 返し10。敵の行動値が10以上なら急所+2'}

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
    body { margin: 0; background: #1a1c33; font-family: "Klee One", "Hiragino Sans", "Noto Sans JP", sans-serif; color: #3b2f2f; -webkit-font-smoothing: antialiased; }
    .abs { position: absolute; }
    .deco { font-family: "Kaisei Decol", "Hiragino Mincho ProN", serif; font-weight: 700; }
    .px { image-rendering: pixelated; }
    .tag { background: #f4ecd6; color: #3b2f2f; border-radius: 9px 12px 9px 11px / 11px 9px 12px 9px; box-shadow: 0 0 0 1.5px #3b2f2f, 0 3px 8px rgba(0,0,0,0.35); display: flex; align-items: center; gap: 6px; height: 30px; padding: 0 10px; font-size: 15px; white-space: nowrap; box-sizing: border-box; }
    .paper { background: #f4ecd6; color: #3b2f2f; border-radius: 10px 13px 9px 12px / 12px 9px 13px 10px; box-shadow: 0 0 0 1.5px #3b2f2f, 0 4px 10px rgba(0,0,0,0.35); box-sizing: border-box; }
    .note { background: #fff3ea; color: #9c3a2a; border: 1.5px solid #9c3a2a; border-radius: 6px; padding: 6px 10px; font-size: 14px; line-height: 19px; box-shadow: 0 4px 10px rgba(0,0,0,0.35); box-sizing: border-box; }
    .note b { color: #7a2418; }
    .small { font-size: 13px; line-height: 17px; color: #574b48; }
  </style>
</helmet>
'''
TAIL = '''</x-dc>
</body>
</html>
'''

def board(bg, w=1462, h=675):
    return '<div style="position: relative; width: %dpx; height: %dpx; overflow: hidden; background: #1a1c33">%s' % (w, h, ('<img src="%s" style="position: absolute; left: 0; top: 0; width: %dpx; height: %dpx; display: block">' % (bg, w, h)) if bg else '')
def close(): return '</div>'

def art_img(card_id, w, h, extra_style=''):
    return '<img class="px" src="%s" style="display: block; width: %dpx; height: %dpx; %s">' % (ART[card_id], w, h, extra_style)

def note(x, y, w, html, arrow=None):
    s = '<div class="abs note" style="left: %dpx; top: %dpx; width: %dpx">%s</div>' % (x, y, w, html)
    if arrow:
        ax, ay, bx, by = arrow
        s += ('<svg class="abs" style="left: 0; top: 0; overflow: visible; pointer-events: none" width="1" height="1"><line x1="%d" y1="%d" x2="%d" y2="%d" stroke="#9c3a2a" stroke-width="2.5" stroke-dasharray="6 4"></line><circle cx="%d" cy="%d" r="5" fill="#9c3a2a"></circle></svg>' % (ax, ay, bx, by, bx, by))
    return s

# ==== 仕込み札のトークン (68×74): 上に挿絵 64×38、下に状態の一言。縁の色が状態 ====
STATE = {
    'prep': dict(band=GREY, text='準備中', edge='#6a6a74', glow=None, badge=None),
    'live': dict(band='#7a6a3a', text='あと2回', edge=HONEY, glow='rgba(224,178,90,0.45)', badge='2'),
    'now': dict(band=MOSS, text='今ターン', edge='#f0d58a', glow='rgba(240,213,138,0.75)', badge='2'),
    'last': dict(band=MOSS, text='今ターン', edge='#f0d58a', glow='rgba(240,213,138,0.75)', badge='1'),
    'keep': dict(band='#7a6a3a', text='期限なし', edge=HONEY, glow='rgba(224,178,90,0.45)', badge='∞'),
}
def token(x, y, card_id, state, scale=1.0):
    st = STATE[state]
    w, h = 68 * scale, 74 * scale
    s = '<div class="abs" style="left: %spx; top: %spx; width: %spx; height: %spx">' % (x, y, w, h)
    if st['glow']:
        s += '<div class="abs" style="left: -4px; top: -4px; right: -4px; bottom: -4px; border-radius: 10px; background: %s; filter: blur(4px)"></div>' % st['glow']
    s += '<div class="abs paper" style="inset: 0; border-radius: 6px; box-shadow: 0 0 0 2px %s, 0 3px 8px rgba(0,0,0,0.4)"></div>' % st['edge']
    s += '<div class="abs" style="left: %spx; top: %spx">%s</div>' % (2 * scale, 2 * scale, art_img(card_id, int(64 * scale), int(38 * scale), 'border-radius: 3px; box-shadow: 0 0 0 1px %s' % INK))
    s += '<div class="abs deco" style="left: 0; right: 0; bottom: 0; height: %spx; border-radius: 0 0 6px 6px; background: %s; color: #f4ecd6; font-size: %spx; display: flex; align-items: center; justify-content: center; letter-spacing: 0.02em">%s</div>' % (28 * scale, st['band'], 15 * scale, st['text'])
    if st['badge']:
        s += '<div class="abs deco" style="right: %spx; top: %spx; width: %spx; height: %spx; border-radius: 50%%; background: #f4ecd6; color: #3b2f2f; box-shadow: 0 0 0 1.5px #3b2f2f; font-size: %spx; display: flex; align-items: center; justify-content: center">%s</div>' % (-7 * scale, -7 * scale, 20 * scale, 20 * scale, 13 * scale, st['badge'])
    return s + '</div>'
def empty_token(x, y, scale=1.0):
    return '<div class="abs" style="left: %spx; top: %spx; width: %spx; height: %spx; border: 2px dashed rgba(244,236,214,0.55); border-radius: 6px; background: rgba(244,236,214,0.08); box-sizing: border-box"></div>' % (x, y, 68 * scale, 74 * scale)

# ==== 置物のチップ (168×40): 挿絵 48×29 + 名前。紙の付箋 ====
def perm_chip(x, y, card_id, w=168, name=None):
    return ('<div class="abs tag" style="left: %dpx; top: %dpx; width: %dpx; height: 40px; padding: 0 8px 0 5px; gap: 8px">%s<span class="deco" style="font-size: 15px; font-weight: 400; overflow: hidden; text-overflow: ellipsis">%s</span></div>'
            % (x, y, w, art_img(card_id, 48, 29, 'border-radius: 3px; box-shadow: 0 0 0 1px %s; flex: none' % INK), name or NAME[card_id]))
def more_chip(x, y, n):
    return '<div class="abs tag" style="left: %dpx; top: %dpx; height: 40px; padding: 0 12px; font-size: 15px">+%d …</div>' % (x, y, n)
def res_chip(x, y, text, color='#dfe8c8'):
    return '<div class="abs tag" style="left: %dpx; top: %dpx; height: 28px; font-size: 15px; padding: 0 9px; background: %s"><span style="display: inline-block; width: 10px; height: 10px; border-radius: 50%%; background: #3b2f2f"></span>%s</div>' % (x, y, color, text)
def small_label(x, y, text, w=None):
    return '<div class="abs" style="left: %dpx; top: %dpx; %s color: #f4ecd6; font-size: 14px; letter-spacing: 0.06em; text-shadow: 0 0 3px #000, 0 0 6px #000, 0 1px 0 #000">%s</div>' % (x, y, ('width: %dpx; ' % w) if w else '', text)

# ==== レールの升 (48×48): 挿絵を切り抜いて置く ====
def rail_square(x, y, card_id, state=None, badge=None, size=48):
    edge = INK
    glow = ''
    if state:
        st = STATE[state]; edge = st['edge']
        if st['glow']: glow = '<div class="abs" style="inset: -4px; border-radius: 9px; background: %s; filter: blur(4px)"></div>' % st['glow']
    s = '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx">%s' % (x, y, size, size, glow)
    s += '<div class="abs" style="inset: 0; border-radius: 6px; background: #f4ecd6; box-shadow: 0 0 0 2px %s, 0 3px 8px rgba(0,0,0,0.4); overflow: hidden">' % edge
    s += '<img class="px" src="%s" style="position: absolute; left: -16px; top: 0; width: 80px; height: 48px">' % ART[card_id]
    if state:
        s += '<div class="abs" style="left: 0; right: 0; bottom: 0; height: 8px; background: %s"></div>' % STATE[state]['band']
    s += '</div>'
    b = badge if badge is not None else (STATE[state]['badge'] if state else None)
    if b:
        s += '<div class="abs deco" style="right: -7px; top: -7px; width: 20px; height: 20px; border-radius: 50%%; background: #f4ecd6; color: #3b2f2f; box-shadow: 0 0 0 1.5px #3b2f2f; font-size: 13px; display: flex; align-items: center; justify-content: center">%s</div>' % b
    return s + '</div>'
def rail_empty(x, y, size=48):
    return '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; border: 2px dashed rgba(244,236,214,0.55); border-radius: 6px; box-sizing: border-box"></div>' % (x, y, size, size)

# ================================================================ 現状
def board_current():
    s = board('current-trio.jpg')
    s += note(560, 62, 250, '<b>①</b> 置物の札は上部バーの下 = <b>敵の吹き出しと同じ帯</b>。名前が長い／敵が左寄り (3〜4体・召喚) だと重なる', (700, 100, 553, 98))
    s += note(258, 160, 210, '<b>②</b> 伏せ場 232×200 が舞台の左を占める。札の裏 (匣/？) は何を仕込んだか読めない', (300, 200, 252, 240))
    s += note(1010, 372, 300, '<b>③</b> 3体以上は吹き出し同士も重なる (今回の範囲外・別件で直す)', (1080, 372, 985, 160))
    s += small_label(24, 645, '現状 (2026-09-14 f605573)・キャンバス 1462×675')
    return s + close()

# ================================================================ 案A 足元の匣 + 左の付箋列
def board_a():
    s = board('base-trio.jpg')
    s += res_chip(200, 181, '成長 3'); s += res_chip(288, 181, '勢い 4')
    s += small_label(26, 66, '置物')
    for i, cid in enumerate(['green_perm_growth_tree', 'green_perm_thorn_vine', 'green_perm_wild_call', 'green_perm_sprout_keeper']):
        s += perm_chip(24, 88 + i * 46, cid)
    s += more_chip(24, 88 + 4 * 46, 1)
    s += small_label(470, 164, 'からくり 2 / 2')
    s += token(470, 186, 'green_reaction_vine', 'now')
    s += token(548, 186, 'green_reaction_thorns', 'prep')
    s += '<svg class="abs" style="left: 0; top: 0; overflow: visible; pointer-events: none" width="1" height="1"><path d="M505 272 C 505 268, 540 266, 540 262" stroke="rgba(244,236,214,0.6)" stroke-width="1.5" fill="none" stroke-dasharray="3 3"></path></svg>'
    s += small_label(24, 645, '案A — 足元の匣 + 左の付箋列 (推奨)')
    return s + close()

# ================================================================ 案B 左にまとめる (自分の欄を3段)
def board_b():
    s = board('base-trio.jpg')
    s += res_chip(24, 60, '成長 3'); s += res_chip(112, 60, '勢い 4')
    s += small_label(26, 96, 'からくり 2 / 2')
    s += token(24, 118, 'green_reaction_vine', 'now')
    s += token(102, 118, 'green_reaction_thorns', 'prep')
    s += small_label(26, 200, '置物')
    for i, cid in enumerate(['green_perm_growth_tree', 'green_perm_thorn_vine', 'green_perm_wild_call', 'green_perm_sprout_keeper']):
        s += perm_chip(24 + (i % 2) * 176, 222 + (i // 2) * 46, cid)
    s += note(360, 60, 300, '<b>案B 左にまとめる</b>: 自分に関する物 (資源・からくり・置物) を左の一角に3段で。匣とポケットは空ける。場所は今と同程度だが上部バーの帯からは外れる')
    s += small_label(24, 645, '案B — 左に3段でまとめる')
    return s + close()

# ================================================================ 案C 左のレール (閉じた時・開いた時)
def rail(s):
    s += '<div class="abs" style="left: 6px; top: 52px; width: 60px; height: 322px; border-radius: 10px; background: rgba(28,26,40,0.55); box-shadow: 0 0 0 1px rgba(244,236,214,0.25)"></div>'
    s += rail_square(12, 60, 'green_reaction_vine', 'now')
    s += rail_square(12, 114, 'green_reaction_thorns', 'prep')
    s += '<div class="abs" style="left: 14px; top: 170px; width: 44px; height: 1px; background: rgba(244,236,214,0.45)"></div>'
    for i, cid in enumerate(['green_perm_growth_tree', 'green_perm_thorn_vine', 'green_perm_wild_call']):
        s += rail_square(12, 178 + i * 54, cid)
    s += '<div class="abs deco" style="left: 12px; top: 340px; width: 48px; height: 28px; border-radius: 6px; background: #f4ecd6; box-shadow: 0 0 0 1.5px #3b2f2f; font-size: 14px; display: flex; align-items: center; justify-content: center">+1 …</div>'
    return s
def board_c():
    s = board('base-trio.jpg')
    s = rail(s)
    s += note(90, 60, 300, '<b>案C 左のレール</b>: 画面の左端に 48px の升だけ。上2つが仕込み枠 (縁の色と下の帯が状態・角の数字が残り回数)、下が置物。名前と本文はタップで引き出しに出る')
    s += small_label(24, 645, '案C — 左のレール (閉じた時)')
    return s + close()
def board_c_open():
    s = board('base-trio.jpg')
    s += '<div class="abs" style="inset: 0; background: rgba(0,0,0,0.35)"></div>'
    s = rail(s)
    s += '<div class="abs paper" style="left: 74px; top: 52px; width: 430px; height: 322px; padding: 12px 14px"></div>'
    s += '<div class="abs deco" style="left: 90px; top: 62px; font-size: 16px">からくり 2 / 2</div>'
    for i, (cid, state, life) in enumerate([('green_reaction_vine', 'now', '今ターン鳴る！ あと2回'), ('green_reaction_thorns', 'prep', '準備中: 次のターンから')]):
        y = 88 + i * 62
        s += token(90, y, cid, state, 0.72)
        s += '<div class="abs deco" style="left: 150px; top: %dpx; font-size: 16px">%s</div>' % (y + 2, NAME[cid])
        s += '<div class="abs small" style="left: 150px; top: %dpx; width: 350px; white-space: nowrap">%s</div>' % (y + 22, BODY[cid])
        s += '<div class="abs small" style="left: 150px; top: %dpx; color: #7a4e12">%s</div>' % (y + 38, life)
    s += '<div class="abs" style="left: 90px; top: 214px; width: 400px; height: 1px; background: rgba(59,47,47,0.35)"></div>'
    s += '<div class="abs deco" style="left: 90px; top: 220px; font-size: 16px">置物 4</div>'
    for i, cid in enumerate(['green_perm_growth_tree', 'green_perm_thorn_vine', 'green_perm_wild_call', 'green_perm_sprout_keeper']):
        y = 246 + i * 30
        s += '<div class="abs" style="left: 90px; top: %dpx">%s</div>' % (y, art_img(cid, 40, 24, 'border-radius: 3px; box-shadow: 0 0 0 1px %s' % INK))
        s += '<div class="abs deco" style="left: 140px; top: %dpx; font-size: 15px; font-weight: 400">%s</div>' % (y + 2, NAME[cid])
        s += '<div class="abs small" style="left: 250px; top: %dpx; width: 250px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis">%s</div>' % (y + 4, BODY[cid])
    s += small_label(24, 645, '案C — 左のレール (升をタップして引き出しを開いた時。外を触ると閉じる)')
    return s + close()

# ================================================================ 案A をボス戦 (1体) でも
def board_a_boss():
    s = board('base-boss.jpg')
    s += res_chip(200, 181, '成長 6')
    s += small_label(26, 66, '置物')
    for i, cid in enumerate(['green_perm_growth_tree', 'green_perm_thorn_vine']):
        s += perm_chip(24, 88 + i * 46, cid)
    s += small_label(470, 164, 'からくり 1 / 2')
    s += token(470, 186, 'green_reaction_vine', 'live')
    s += empty_token(548, 186)
    s += small_label(24, 645, '案A — 幕3ボス (1体)。空いた枠は点線・「あと2回」= 生きているが今ターンは鳴らない')
    return s + close()

# ================================================================ 札の状態一覧 (2倍)
def board_tokens():
    s = board(None, 1180, 470)
    s += '<div class="abs" style="inset: 0; background: #2a2c48"></div>'
    s += '<div class="abs deco" style="left: 24px; top: 16px; font-size: 22px; color: #f4ecd6">仕込み札のトークン (68×74・2倍で表示) と置物のチップ</div>'
    items = [('prep', '準備中 (仕込んだターン)', '灰の縁。次のターンから'), ('live', '生きている・今の構えでは鳴らない', '蜂蜜の縁。角に残り回数'), ('now', '今ターン鳴る！', '緑の帯・縁が脈打つ'), ('last', '最後の窓', '角の数字が 1'), ('keep', '期限なし (大樹の守り手)', '角は ∞')]
    for i, (st, title, sub) in enumerate(items):
        x = 24 + i * 226
        s += token(x + 30, 70, ['green_reaction_vine', 'green_reaction_thorns', 'green_reaction_vine', 'green_reaction_thorns', 'green_reaction_vine'][i], st, 2.0)
        s += '<div class="abs" style="left: %dpx; top: 232px; width: 210px; color: #f4ecd6; font-size: 16px; line-height: 22px">%s<br><span style="color: #c4beb2; font-size: 14px">%s</span></div>' % (x, title, sub)
    s += '<div class="abs deco" style="left: 24px; top: 300px; font-size: 18px; color: #f4ecd6">置物のチップ (168×40)・レールの升 (48×48)・空き枠</div>'
    s += '<div class="abs" style="left: 24px; top: 336px; transform: scale(2); transform-origin: 0 0">' + perm_chip(0, 0, 'green_perm_thorn_vine') + more_chip(178, 0, 2) + '</div>'
    s += '<div class="abs" style="left: 520px; top: 336px; transform: scale(2); transform-origin: 0 0">' + rail_square(0, 0, 'green_reaction_vine', 'now') + rail_square(60, 0, 'green_perm_thorn_vine') + rail_empty(120, 0) + '</div>'
    s += '<div class="abs" style="left: 900px; top: 336px; transform: scale(2); transform-origin: 0 0">' + empty_token(0, 0) + '</div>'
    s += '<div class="abs" style="left: 24px; top: 440px; color: #c4beb2; font-size: 14px">文字の最小は 15px (スマホの規約)。挿絵は 80×48 のドット絵を 64×38 / 48×29 に縮小 (スマホは整数倍の規約を外している)。タップで画面左上の固定パネルに名前・本文・状態</div>'
    return s + close()

# ================================================================ 比較
def board_compare():
    s = board(None, 1180, 560)
    s += '<div class="abs" style="inset: 0; background: #f4ecd6"></div>'
    s += '<div class="abs deco" style="left: 28px; top: 20px; font-size: 24px">比較と推奨</div>'
    rows = [
        ('', '案A 足元の匣', '案B 左に3段', '案C 左のレール'),
        ('占有 (現状 232×200 + 上の帯)', '置物 168×230 + 札 146×74', '340×220 (今と同程度)', '60×322 (最小)'),
        ('敵の吹き出しとの衝突', 'なし', 'なし', 'なし'),
        ('一目で分かるもの', '置物の名前・仕込み札の絵と状態', '同左', '絵と状態の色だけ (名前はタップ)'),
        ('世界観 (からくりの匣)', '匣の上に札 = 物と一致', '匣は飾り', '匣は飾り'),
        ('舞台の見え方', 'ポケットに札が2枚、左に付箋', '左の一角が埋まる', '最も広い'),
        ('実装', 'FillPlayerPanel の Phone 分岐のみ (座標は匣の投影)', '同左 (固定座標)', '升 + 引き出し (新しい部品1つ)'),
    ]
    y = 66
    for r in rows:
        for j, cell in enumerate(r):
            x = 28 + [0, 300, 590, 880][j]
            w = [260, 280, 280, 280][j]
            s += '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; font-size: 15px; line-height: 21px; %s">%s</div>' % (x, y, w, 'font-weight: 700; font-family: "Kaisei Decol", serif' if (y == 66 or j == 0) else '', cell)
        y += 52
        s += '<div class="abs" style="left: 28px; top: %dpx; width: 1120px; height: 1px; background: rgba(59,47,47,0.3)"></div>' % (y - 8)
    s += ('<div class="abs" style="left: 28px; top: %dpx; width: 1120px; font-size: 15px; line-height: 22px">'
          '<b>推奨: 案A</b>。理由 = ①上部バーの下 (敵の吹き出しの帯) から自分の物を全部どける ②仕込み札を「匣の中身」として匣の真上に置く = 世界観の実物と表示が一致し、仕込むと匣の蓋が開く演出がそのまま説明になる '
          '③置物は名前が読める付箋のまま小さく (48×29 の挿絵で見分けがつく) ④面積は現状の約半分。'
          '<br>共通: 札の裏 (匣/？) をやめて挿絵を出す (何を仕込んだかは自分は知っている。敵は伏せを見ない)。状態は縁の色＋一言＋角の残り回数の3点で今の3状態 (準備中／鳴る／今ターン鳴る) を保つ。PC の配置は変えない。'
          '<br>別件: 3体以上で吹き出し同士が重なる (現状の③) は、この案とは独立に直す (吹き出しの幅を体数で縮める／2段に分ける)。</div>') % (y + 6)
    return s + close()

def write(name, body):
    with open(os.path.join(OUT, name), 'w', encoding='utf-8') as f: f.write(HEAD + body + TAIL)

write('Main.dc.html', board_current())
write('DirectionA.dc.html', board_a())
write('DirectionABoss.dc.html', board_a_boss())
write('DirectionB.dc.html', board_b())
write('DirectionC.dc.html', board_c())
write('DirectionCOpen.dc.html', board_c_open())
write('Tokens.dc.html', board_tokens())
write('Compare.dc.html', board_compare())
canvas = {
    'artboards': [
        {'file': 'Main.dc.html', 'title': '現状 (スマホ・問題の指摘)', 'x': 0, 'y': 0, 'w': 1462, 'h': 675},
        {'file': 'DirectionA.dc.html', 'title': '案A 足元の匣 + 左の付箋列 (推奨)', 'x': 1560, 'y': 0, 'w': 1462, 'h': 675},
        {'file': 'DirectionABoss.dc.html', 'title': '案A ボス戦 (1体・空き枠)', 'x': 1560, 'y': 800, 'w': 1462, 'h': 675},
        {'file': 'DirectionB.dc.html', 'title': '案B 左に3段でまとめる', 'x': 0, 'y': 800, 'w': 1462, 'h': 675},
        {'file': 'DirectionC.dc.html', 'title': '案C 左のレール (閉)', 'x': 0, 'y': 1600, 'w': 1462, 'h': 675},
        {'file': 'DirectionCOpen.dc.html', 'title': '案C 左のレール (開)', 'x': 1560, 'y': 1600, 'w': 1462, 'h': 675},
        {'file': 'Tokens.dc.html', 'title': '札の状態一覧 (2倍)', 'x': 0, 'y': 2400, 'w': 1180, 'h': 470},
        {'file': 'Compare.dc.html', 'title': '比較と推奨', 'x': 1560, 'y': 2400, 'w': 1180, 'h': 560},
    ],
    'annotations': [
        {'id': 'brief', 'x': 0, 'y': -170, 'w': 720, 'text': '【採択: 案B 左に3段 (2026-09-14 ユーザー裁定。挿絵を出す・吹き出しの重なりも一緒に直す)】スマホ (S25・キャンバス 1462×675) の戦闘画面: 置物と仕込み札 (からくり) の置き場の作り直し。下地は実機と同じ画角の Unity のスクショ (伏せ場と置物を消したもの)。上に重ねた紙の物だけが新しい提案。座標はそのまま Unity の Phone 分岐に写せる。'},
        {'id': 'note-a', 'x': 1560, 'y': -110, 'w': 560, 'text': '案A: 仕込み札 = 匣の真上のトークン (挿絵・状態の一言・角に残り回数)。置物 = 左端の付箋の列 (挿絵 + 名前、5枚目以降は +N)。資源チップは頭上のまま (列と重ならないよう右へ)。'},
    ],
    'launch': {'view': 'canvas'},
}
with open(os.path.join(OUT, 'canvas.json'), 'w', encoding='utf-8') as f: json.dump(canvas, f, ensure_ascii=False, indent=2)
print('written')

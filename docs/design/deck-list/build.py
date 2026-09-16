# build.py — デッキから札を選ぶ画面の見直し (2026-09-16 ユーザー「ゲーム内にデッキ一覧を選択させる場面がある。モバイル版だとスクロールなど操作しにくい。
# スクロールバーの導入や現状のレイアウトを構築し直してほしい」)。
# 対象: 焚き火/ショップの鍛える・取り除く、工房 (素材2枚)、?イベントの「デッキから1枚選ぶ」、星読みの盤/空の鳥籠、デッキ一覧 (閲覧)。
# 現状 (スマホ S25 相当・キャンバス 1462×675) の指摘 ＋ 3案 (A 一面の棚としおり／B ページ送り／C 帳面の目録) ＋ 比較と推奨。
# 下地は現状のスクショ (Autopilot の shots state)。札 200×290・文字 13px 以上・ボタン 48 以上は Unity の実装値と同じ。
# 使い方: python3 build.py → *.dc.html を書く → seed-canvas.mjs で束ねる (docs/design/battle-v2 と同じ手順)。
import base64, os
OUT = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(OUT, '..', '..', '..'))
ART_DIR = os.path.join(REPO, 'unity', 'Assets', 'Resources', 'Art')

INK = '#3b2f2f'; INK_SOFT = '#574b48'; PAPER = '#f4ecd6'; PAPER2 = '#eadfc4'; PAPER3 = '#fbf6e8'; HONEY = '#e0b25a'; GOLD_INK = '#7a4e12'
ROSE = '#d97b7b'; SKY = '#7fa7c9'; MOSS = '#8fae7b'; MOSS_DEEP = '#3f8a4a'; GREY = '#8a8a94'; NIGHT = '#1a1c33'; BAD_INK = '#9c3a2a'; PLUM_INK = '#5a3d78'; GOOD_INK = '#276a34'
TYPE_COL = dict(physical='#8a6a3c', spell='#6c4f9c', reaction='#3f8c86', permanent='#b08a2e')
TYPE_JA = dict(physical='物理', spell='呪文', reaction='リアクション', permanent='置物')

def uri(path):
    with open(path, 'rb') as f: return 'data:image/png;base64,' + base64.b64encode(f.read()).decode('ascii')
def card_art(cid): return uri(os.path.join(ART_DIR, 'cards', cid + '.png'))
def icon(name): return uri(os.path.join(ART_DIR, 'icons', name + '.png'))
def ui_art(name): return uri(os.path.join(ART_DIR, 'ui', name + '.png'))
def leader_art(lid): return uri(os.path.join(ART_DIR, 'leaders', lid + '.png'))

# ---- 見本のデッキ (deck_big_mana ＝ ビッグマナ理想形 26枚。現状のスクショと同じ) ----
CARDS = {
    'green_ramp_sprout': dict(name='芽吹き', cost=1, type='spell', body='エナジー上限+<b>1</b><br>ブロック<b>2</b><br>消滅', one='エナジー上限+1・ブロック2・消滅', rar='common'),
    'green_ramp_deep_roots': dict(name='深根', cost=2, type='spell', body='エナジー上限+<b>2</b><br>ブロック<b>4</b><br>消滅', one='エナジー上限+2・ブロック4・消滅', rar='common'),
    'green_ramp_sunlight': dict(name='陽光の恵み', cost=2, type='spell', body='<span class="small">どちらか一つ</span><br>◆エナジー上限+<b>1</b>／<b>1</b>ドロー<br>◆<b>1</b>ドロー／成長+<b>2</b>', one='どちらか: 上限+1／1ドロー or 1ドロー／成長+2', rar='uncommon'),
    'green_sig_canopy': dict(name='天蓋の実り', cost=3, type='spell', body='エナジー上限+<b>2</b><br><b>2</b>ドロー<br>ブロック<b>4</b><br>消滅', one='エナジー上限+2・2ドロー・ブロック4・消滅', rar='rare'),
    'green_earth_roar': dict(name='大地の唸り', cost=2, type='spell', body='敵全体にダメージ<b>6</b><br><span class="small">[エナジー上限5以上]</span><br>敵全体にダメージ<b>6</b>', one='敵全体に6・[上限5以上] さらに全体に6', rar='uncommon'),
    'green_x_sylvan_tempest': dict(name='森羅の大嵐', cost='X', type='spell', body='<span class="small">X: エナジーを全て払う</span><br>敵全体にダメージ<b>4</b>×X回', one='X: 敵全体に4×X回', rar='rare'),
    'green_finisher_stomp': dict(name='巨獣の踏みつけ', cost=5, type='physical', body='ダメージ<b>50</b><br>保持', one='ダメージ50・保持', rar='rare'),
    'green_finisher_wrath': dict(name='大樹の怒り', cost=4, type='physical', body='ダメージ<b>36</b><br>成長+<b>1</b><br>保持', one='ダメージ36・成長+1・保持', rar='uncommon'),
    'green_bark_armor': dict(name='樹皮の鎧', cost=2, type='physical', body='ブロック<b>14</b><br>成長+<b>1</b>', one='ブロック14・成長+1', rar='common'),
    'green_guard': dict(name='防御', cost=1, type='physical', body='ブロック<b>5</b>', one='ブロック5', rar='common'),
    'green_reaction_root_weave': dict(name='根の紡ぎ', cost=2, type='reaction', body='敵行動時: 打ち消し<br>成長+<b>2</b>', one='敵行動時: 打ち消し・成長+2', rar='uncommon'),
    'green_ritual_surge': dict(name='樹液', cost=1, type='spell', body='一時マナ+<b>2</b><br>消滅', one='一時マナ+2・消滅', rar='common'),
    'green_flash_insight': dict(name='緑の閃き', cost=1, type='spell', body='<span class="small">追加コスト: 手札1枚を捨てる</span><br><b>4</b>ドロー', one='捨て1: 4ドロー', rar='common'),
    'green_x_bark_armor': dict(name='樹皮の重鎧', cost='X', type='spell', body='<span class="small">X: エナジーを全て払う</span><br>ブロック<b>6</b>×X回', one='X: ブロック6×X回', rar='uncommon'),
    'green_sapling_strike': dict(name='若幹の一撃', cost=1, type='physical', body='ダメージ<b>6</b><br><span class="small">[エナジー上限5以上]</span><br>ダメージ<b>6</b>', one='ダメージ6・[上限5以上] さらに6', rar='common'),
    'green_reaction_vine': dict(name='守りの蔓', cost=1, type='reaction', body='被攻撃前: ブロック<b>12</b><br>完全に凌げば次のターン<b>1</b>ドロー', one='被攻撃前: ブロック12・凌げば1ドロー', rar='common'),
}
DECK = (['green_ramp_sprout'] * 2 + ['green_ramp_deep_roots'] * 3 + ['green_ramp_sunlight'] * 2 + ['green_sig_canopy'] * 2 + ['green_earth_roar'] + ['green_x_sylvan_tempest']
        + ['green_finisher_stomp'] * 2 + ['green_finisher_wrath'] + ['green_bark_armor'] * 2 + ['green_guard'] + ['green_reaction_root_weave'] * 2 + ['green_ritual_surge'] * 2
        + ['green_flash_insight'] * 2 + ['green_x_bark_armor'] + ['green_sapling_strike'] + ['green_reaction_vine'])
assert len(DECK) == 26

# ---- 鍛えた後 (緑の本家形: 上限ランプはコスト−1、しきい値札はしきい値−1＋量+50%、単位持ちは単位+1＋量+50%、素の量は+50%) ----
UP = {
    'green_ramp_sprout': dict(cost=0, body='エナジー上限+<b>1</b><br>ブロック<b>2</b><br>消滅'),
    'green_ramp_deep_roots': dict(cost=1, body='エナジー上限+<b>2</b><br>ブロック<b>4</b><br>消滅'),
    'green_ramp_sunlight': dict(cost=1, body='<span class="small">どちらか一つ</span><br>◆エナジー上限+<b>1</b>／<b>1</b>ドロー<br>◆<b>1</b>ドロー／成長+<b>2</b>'),
    'green_sig_canopy': dict(cost=2, body='エナジー上限+<b>2</b><br><b>2</b>ドロー<br>ブロック<b>4</b><br>消滅'),
    'green_earth_roar': dict(cost=None, body='敵全体にダメージ<b class="good">9</b><br><span class="small good">[エナジー上限4以上]</span><br>敵全体にダメージ<b class="good">9</b>'),
    'green_x_sylvan_tempest': dict(cost=None, body='<span class="small">X: エナジーを全て払う</span><br>敵全体にダメージ<b class="good">6</b>×X回'),
    'green_finisher_stomp': dict(cost=None, body='ダメージ<b class="good">75</b><br>保持'),
    'green_finisher_wrath': dict(cost=None, body='ダメージ<b class="good">54</b><br>成長+<b class="good">2</b><br>保持'),
    'green_bark_armor': dict(cost=None, body='ブロック<b class="good">21</b><br>成長+<b class="good">2</b>'),
    'green_guard': dict(cost=None, body='ブロック<b class="good">8</b>'),
    'green_reaction_root_weave': dict(cost=None, body='敵行動時: 打ち消し<br>成長+<b class="good">3</b>'),
    'green_ritual_surge': dict(cost=None, body='一時マナ+<b class="good">3</b><br>消滅'),
    'green_flash_insight': dict(cost=None, body='<span class="small">追加コスト: 手札1枚を捨てる</span><br><b class="good">5</b>ドロー'),
    'green_x_bark_armor': dict(cost=None, body='<span class="small">X: エナジーを全て払う</span><br>ブロック<b class="good">9</b>×X回'),
    'green_sapling_strike': dict(cost=None, body='ダメージ<b class="good">9</b><br><span class="small good">[エナジー上限4以上]</span><br>ダメージ<b class="good">9</b>'),
    'green_reaction_vine': dict(cost=None, body='被攻撃前: ブロック<b class="good">18</b><br>完全に凌げば次のターン<b class="good">2</b>ドロー'),
}
ART = {k: card_art(k) for k in CARDS}
ICON = {k: icon(k) for k in ['heart', 'gold', 'hammer', 'star', 'energy', 'growth', 'draw', 'exhaust', 'sword', 'shield']}
ORB = ui_art('cost_orb'); GEM = {r: ui_art('gem_' + r) for r in ['common', 'uncommon', 'rare']}
KONOHA = uri(os.path.join(ART_DIR, 'leaders', 'leader_green_icon.png'))

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
    .note { background: #fff3ea; color: #9c3a2a; border: 1.5px solid #9c3a2a; border-radius: 6px; padding: 6px 10px; font-size: 14px; line-height: 19px; box-shadow: 0 4px 10px rgba(0,0,0,0.35); box-sizing: border-box; }
    .note b { color: #7a2418; }
    .small { font-size: 13px; line-height: 17px; color: #574b48; }
    .card b { font-size: 130%; font-weight: 700; }
    .good { color: #276a34; }
  </style>
</helmet>
'''
TAIL = '''</x-dc>
</body>
</html>
'''

def write(name, html):
    with open(os.path.join(OUT, name + '.dc.html'), 'w', encoding='utf-8') as f: f.write(HEAD + html + TAIL)

def board(bg, w=1462, h=675, dim=False, blur=False):
    s = '<div style="position: relative; width: %dpx; height: %dpx; overflow: hidden; background: #0f1120">' % (w, h)
    if bg:
        fl = []
        if dim: fl.append('brightness(0.45)')
        if blur: fl.append('blur(6px)')
        s += '<img src="%s" style="position: absolute; left: 0; top: 0; width: %dpx; height: %dpx; display: block%s">' % (bg, w, h, ('; filter: ' + ' '.join(fl)) if fl else '')
    return s
def close(): return '</div>'
def img(src, x, y, w, h, extra=''):
    return '<img class="px" src="%s" style="position: absolute; left: %spx; top: %spx; width: %spx; height: %spx; display: block; %s">' % (src, x, y, w, h, extra)
def ic(name, size=16, extra=''):
    return '<img class="px" src="%s" style="width: %dpx; height: %dpx; display: block; flex: none; %s">' % (ICON[name], size, size, extra)
def note(x, y, w, html, arrow=None, big=False):
    s = '<div class="abs note" style="left: %dpx; top: %dpx; width: %dpx; z-index: 60%s">%s</div>' % (x, y, w, '; font-size: 16px; line-height: 22px' if big else '', html)
    if arrow:
        ax, ay, bx, by = arrow
        s += ('<svg class="abs" style="left: 0; top: 0; overflow: visible; pointer-events: none; z-index: 60" width="1" height="1"><line x1="%d" y1="%d" x2="%d" y2="%d" stroke="#9c3a2a" stroke-width="2.5" stroke-dasharray="6 4"></line><circle cx="%d" cy="%d" r="5" fill="#9c3a2a"></circle></svg>' % (ax, ay, bx, by, bx, by))
    return s
def caption(x, y, text):
    return '<div class="abs" style="left: %dpx; top: %dpx; z-index: 60; background: rgba(8,8,20,0.8); color: #f4ecd6; font-size: 14px; line-height: 20px; padding: 1px 10px; border-radius: 4px; letter-spacing: 0.04em; white-space: nowrap">%s</div>' % (x, y, text)
def tag(x, y, html, h=30, w=None, bg=PAPER, rot=0, size=15, z=None):
    return '<div class="abs tag" style="left: %dpx; top: %dpx; height: %dpx; %s background: %s; transform: rotate(%sdeg); font-size: %dpx%s">%s</div>' % (x, y, h, ('width: %dpx;' % w) if w else '', bg, rot, size, ('; z-index: %d' % z) if z is not None else '', html)
def button(x, y, w, h, text, size=17, primary=False, disabled=False, z=None):
    bg = '#f6dd98' if primary else PAPER
    return '<div class="abs deco" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; background: %s; border-radius: 10px 12px 9px 11px / 11px 9px 12px 10px; box-shadow: 0 0 0 1.5px #3b2f2f, 0 3px 8px rgba(0,0,0,0.35); display: flex; align-items: center; justify-content: center; font-size: %dpx; letter-spacing: 0.12em; box-sizing: border-box%s%s">%s</div>' % (
        x, y, w, h, bg, size, '; opacity: 0.55' if disabled else '', ('; z-index: %d' % z) if z is not None else '', text)
def pill(text, ink=INK, paper=PAPER2, size=13, h=22):
    return '<span style="display: inline-flex; align-items: center; height: %dpx; padding: 0 8px; border-radius: 7px; background: %s; color: %s; box-shadow: 0 0 0 1px %s; font-size: %dpx; white-space: nowrap; flex: none">%s</span>' % (h, paper, ink, ink, size, text)

# ---- 上部バー (ラン画面。スマホ 56px) ----
def runbar(w=1462, act=2, row=0, place='焚き火', hp=80, mx=80, gold=150, deck=26, center=None):
    s = '<div class="abs" style="left: 0; top: 0; width: %dpx; height: 56px; background: linear-gradient(rgba(10,10,24,0.55), rgba(10,10,24,0))"></div>' % w
    s += '<div class="abs" style="left: 22px; top: 8px; width: 40px; height: 40px; border-radius: 50%%; background: #20233a; box-shadow: 0 0 0 1.5px #3b2f2f; overflow: hidden">%s</div>' % img(KONOHA, 4, 4, 32, 32)
    s += tag(70, 13, '<span class="small" style="letter-spacing: 0.1em">幕 %d · 行 %d / 15</span><span class="deco" style="font-size: 18px">%s</span>' % (act, row, place), h=34, rot=-0.6)
    if center: s += center
    gx = w - 470
    s += tag(gx, 13, '%s<span class="deco" style="font-size: 17px; font-weight: 400">%d</span><span class="small">/ %d</span>' % (ic('heart', 16), hp, mx), h=34)
    s += tag(gx + 110, 13, '%s<span class="deco" style="font-size: 17px; font-weight: 400">%d</span><span class="small">G</span>' % (ic('gold', 16), gold), h=34)
    s += tag(gx + 214, 13, '<span class="small" style="letter-spacing: 0.06em">デッキ</span><span class="deco" style="font-size: 17px; font-weight: 400">%d</span>' % deck, h=34)
    s += tag(w - 66, 13, '<span style="width: 100%; text-align: center; font-size: 20px">≡</span>', h=34, w=44)
    return s
def bar_title(text, sub=None, x=395, w=560):
    inner = '<span class="deco" style="font-size: 17px; font-weight: 400; letter-spacing: 0.08em; white-space: nowrap">%s</span>' % text
    if sub: inner += '<span class="small" style="margin-left: 10px; white-space: nowrap">%s</span>' % sub
    return '<div class="abs tag" style="left: %dpx; top: 13px; width: %dpx; height: 34px; justify-content: center; background: #fbf6e8">%s</div>' % (x, w, inner)

# ---- カードの面 (200×290・Unity の CardView と同じ骨格。scale は左上を原点に) ----
def card(x, y, cid, scale=1.0, sel=False, dim=False, z=None, plus=False, body=None, cost=None, star=False, mark=None):
    d = CARDS[cid]
    tc = TYPE_COL[d['type']]
    cst = cost if cost is not None else d['cost']
    body = body or d['body']
    name = d['name'] + ('+' if plus else '')
    s = '<div class="abs card" style="left: %dpx; top: %dpx; width: 200px; height: 290px; transform: scale(%s); transform-origin: 0 0; %s">' % (x, y, scale, ('z-index: %d;' % z) if z is not None else '')
    edge = {'common': '#3b2f2f', 'uncommon': '#5f86a8', 'rare': '#c9963a'}[d['rar']]
    glow = '' if not sel else ', 0 0 0 7px rgba(224,178,90,0.85), 0 0 18px rgba(224,178,90,0.8)'
    s += '<div class="abs paper" style="inset: 0; border-radius: 8px 10px 8px 9px / 9px 8px 10px 8px; box-shadow: 0 0 0 1.5px #3b2f2f, 0 0 0 4px #f4ecd6, 0 0 0 5.5px %s, 0 6px 14px rgba(0,0,0,0.45)%s%s"></div>' % (edge, glow, '; filter: brightness(0.7) saturate(0.8)' if dim else '')
    s += '<div class="abs" style="left: 12px; top: 44px; right: 12px; height: 100px; background: #20233a; box-shadow: 0 0 0 1.5px #3b2f2f">%s</div>' % img(ART[cid], 8, 2, 160, 96)
    s += '<div class="abs" style="left: -6px; top: -6px; width: 52px; height: 52px">%s<div class="abs deco" style="inset: 0; display: flex; align-items: center; justify-content: center; font-size: 22px; padding-bottom: 2px%s">%s</div></div>' % (img(ORB, 0, 0, 52, 52), '; color: #276a34' if (cost is not None and cost != d['cost']) else '', cst)
    ns = 20 if len(name) <= 4 else 18 if len(name) <= 5 else 16 if len(name) <= 6 else 15
    s += '<div class="abs deco" style="left: 44px; top: 8px; right: 12px; height: 32px; display: flex; align-items: center; justify-content: center; font-size: %dpx; white-space: nowrap">%s</div>' % (ns, name)
    s += '<div class="abs" style="left: 25px; top: 136px; width: 150px; height: 26px; background: %s; border-radius: 4px; box-shadow: 0 0 0 1.5px #3b2f2f; display: flex; align-items: center; justify-content: center; gap: 5px; color: #f4ecd6; font-size: 14px; letter-spacing: 0.15em">%s%s</div>' % (tc, img(GEM[d['rar']], 0, 0, 24, 24, 'position: static'), TYPE_JA[d['type']])
    s += '<div class="abs" style="left: 14px; top: 172px; right: 14px; bottom: 14px; text-align: center; font-size: 16px; line-height: 24px">%s</div>' % body
    if star: s += '<div class="abs" style="left: -14px; top: -14px; width: 40px; height: 40px">%s</div>' % img(ICON['star'], 0, 0, 40, 40)
    if mark: s += '<div class="abs deco" style="right: -12px; top: -12px; width: 40px; height: 40px; border-radius: 50%%; background: #f6dd98; box-shadow: 0 0 0 2px #3b2f2f; font-size: 22px; display: flex; align-items: center; justify-content: center">%s</div>' % mark
    return s + '</div>'

# ---- スクロールバー「しおり」(紙の帯に墨の栞。掴んで引ける・帯を押すとその位置へ) ----
def bookmark(x, y, h, frac_top, frac_len, w=22):
    s = '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; border-radius: 11px; background: #eadfc4; box-shadow: 0 0 0 1.5px #3b2f2f, inset 0 2px 4px rgba(0,0,0,0.25)"></div>' % (x, y, w, h)
    ty = y + 3 + (h - 6) * frac_top; th = max(48, (h - 6) * frac_len)
    s += '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; border-radius: 9px; background: #3b2f2f; box-shadow: 0 2px 5px rgba(0,0,0,0.4)"></div>' % (x + 3, ty, w - 6, th)
    cy = ty + th / 2
    for i in range(3): s += '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: 2px; background: #f4ecd6; opacity: 0.8; border-radius: 1px"></div>' % (x + 7, cy - 6 + i * 6, w - 14)
    return s

# ---- 一覧の共通部品 ----
def grid(x0, y0, cols, scale, ids, gap=14, sel=None, dim=False, start=0, stars=None, marks=None, y_offset=0, clip=None, upgraded=False):
    cw, ch = 200 * scale, 290 * scale
    s = ''
    for i, cid in enumerate(ids):
        r, c = divmod(i, cols)
        x = x0 + c * (cw + gap); y = y0 + r * (ch + gap) + y_offset
        if clip is not None and y > clip: continue
        u = UP[cid] if upgraded else None
        s += card(x, y, cid, scale, sel=(sel == i), dim=dim and sel != i, z=(20 if sel == i else None), star=bool(stars and i in stars), mark=(marks or {}).get(i), plus=upgraded, cost=(u['cost'] if u and u['cost'] is not None else None), body=(u['body'] if u else None))
    return s
def sort_chips(x, y, active=0, labels=('元の順', 'コスト', 'タイプ', '名前')):
    s = '<div class="abs" style="left: %dpx; top: %dpx; display: flex; gap: 6px; align-items: center"><span class="small" style="color: #f4ecd6; text-shadow: 0 0 3px #000, 0 1px 0 #000; margin-right: 2px">並び</span>' % (x, y)
    for i, l in enumerate(labels):
        s += '<span style="display: inline-flex; align-items: center; height: 30px; padding: 0 12px; border-radius: 8px 10px 8px 9px; background: %s; color: %s; box-shadow: 0 0 0 1.5px #3b2f2f; font-size: 14px; white-space: nowrap">%s</span>' % ('#3b2f2f' if i == active else PAPER, PAPER if i == active else INK, l)
    return s + '</div>'
def checkbox(x, y, text, on=False, h=44):
    box = '<span style="width: 24px; height: 24px; border-radius: 5px; background: %s; box-shadow: 0 0 0 1.5px #3b2f2f; display: inline-flex; align-items: center; justify-content: center; color: #f4ecd6; font-size: 18px; flex: none">%s</span>' % ('#3b2f2f' if on else '#fbf6e8', '✓' if on else '')
    return '<div class="abs tag" style="left: %dpx; top: %dpx; height: %dpx; gap: 10px; padding: 0 14px 0 10px; background: %s">%s<span style="font-size: 15px">%s</span></div>' % (x, y, h, '#fbf6e8' if on else PAPER, box, text)
def count_tag(x, y, text): return tag(x, y, '<span class="small" style="letter-spacing: 0.06em">%s</span>' % text, h=30, bg=PAPER3)

# ================================================================ 現状 (スマホ)
def board_current_campfire():
    s = board('current-campfire.jpg')
    s += note(1090, 62, 340, '<b>①</b> 一度に見えるのは 6.5 枚 (26枚中)。見出し2行と札ごとのボタンが縦を食い、2行目が半分だけ', (1090, 110, 620, 330))
    s += note(1090, 250, 330, '<b>②</b> 右半分は「札に触れたら並ぶ」ための空き地。選ぶ間はずっと空', (1090, 260, 1020, 232))
    s += note(24, 300, 300, '<b>③</b> 慣性なし＝指を離した所で止まる。26枚 (6行) を見るのに 4〜5 回引く。どこまで来たかの目印も無い', (170, 300, 290, 250))
    s += note(24, 420, 300, '<b>④</b> 札の下の「鍛える」は 140×42 (押せる最小 48 を下回る)。スクロール中に指が札に乗ると長押しの拡大が開く', (170, 420, 240, 240))
    s += note(760, 430, 330, '<b>⑤</b> 「戻る」だけが右下に孤立。並び替えも枚数も無い', (900, 430, 900, 405))
    s += caption(24, 648, '現状 焚き火の鍛える スマホ (2026-09-16 96b8908)・キャンバス 1462×675・デッキ26枚')
    return s + close()

def board_current_workshop():
    s = board('current-workshop.jpg')
    s += note(660, 470, 330, '<b>①</b> 素材の板が右の 45% を占め、一覧は 5列×1.4行＝7枚。素材を選ぶたびに一覧を引き戻す', (660, 480, 600, 420))
    s += note(24, 100, 300, '<b>②</b> ⭐レシピの相手札は光るが、光る札が画面の外にあると気づけない', (60, 100, 40, 66))
    s += note(1000, 70, 300, '<b>③</b> 焚き火・ショップ・イベント・一覧の5画面が同じ部品 (CardGrid) なので、一覧の直しは一度で全部に効く')
    s += caption(24, 648, '現状 工房 スマホ・デッキ26枚 (素材は未選択)')
    return s + close()

# ================================================================ 案A 一面の棚としおり
GRID_A = dict(x0=20, y0=66, cols=7, scale=0.9, gap=14)   # 7×180 + 6×14 = 1344 (x 20〜1364)、2行 = 261×2+14 = 536 (y 66〜602)
def board_a():
    s = board('current-campfire.jpg', dim=True, blur=True)
    s += runbar(place='ショップ', center=bar_title('取り除く (50G)', '1枚選ぶ。取り除いた札は戻らない'))
    s += grid(GRID_A['x0'], GRID_A['y0'], GRID_A['cols'], GRID_A['scale'], DECK[:14], sel=8)
    s += bookmark(1404, 66, 536, 0.0, 0.5)
    s += '<div class="abs small" style="left: 1378px; top: 606px; width: 80px; text-align: center; color: #f4ecd6; text-shadow: 0 0 3px #000, 0 1px 0 #000; font-size: 13px">1〜14 / 26</div>'
    s += sort_chips(22, 626)
    s += button(700, 615, 380, 52, '<span style="display:flex;align-items:center;gap:10px">大地の唸り を取り除く</span>', size=18, primary=True)
    s += button(1264, 617, 176, 48, '戻る', size=16)
    s += note(430, 330, 300, '取り除く・?イベントの「デッキから1枚」・星読みの盤・一覧 (閲覧) は全部この形。押した札に蜂蜜の縁、下の帯で確定', (560, 330, 500, 300))
    s += caption(24, 4, '案A 取り除く／イベント／一覧 (共通の棚): 7列×2行=14枚・しおり・押す＝選ぶ・下の帯で確定')
    return s + close()

def board_a_forge(upgraded=False, sel=None):
    """鍛える (焚き火・ショップ): 本家の Smith と同じく「鍛えた後を見る」のチェックで全部の札が鍛えた後の姿になる。札を押す＝選ぶ → 下の帯に「この札を鍛える」"""
    s = board('current-campfire.jpg', dim=True, blur=True)
    s += runbar(center=bar_title('鍛える', '1枚選ぶ。チェックで鍛えた後の姿を見比べる'))
    s += grid(GRID_A['x0'], GRID_A['y0'], GRID_A['cols'], GRID_A['scale'], DECK[:14], sel=sel, upgraded=upgraded)
    s += bookmark(1404, 66, 536, 0.0, 0.5)
    s += '<div class="abs small" style="left: 1378px; top: 606px; width: 80px; text-align: center; color: #f4ecd6; text-shadow: 0 0 3px #000, 0 1px 0 #000; font-size: 13px">1〜14 / 26</div>'
    s += checkbox(22, 619, '鍛えた後を見る', on=upgraded)
    s += sort_chips(232, 626)
    if sel is not None:
        s += button(700, 615, 380, 52, '<span style="display:flex;align-items:center;gap:10px">%s %s を鍛える</span>' % (ic('hammer', 22), CARDS[DECK[sel]]['name']), size=18, primary=True)
    else:
        s += '<div class="abs small" style="left: 700px; top: 632px; width: 380px; text-align: center; color: #f4ecd6; text-shadow: 0 0 3px #000, 0 1px 0 #000; font-size: 14px">札を押すとここに「〜を鍛える」</div>'
    s += button(1264, 617, 176, 48, '戻る', size=16)
    if upgraded:
        s += note(430, 330, 330, '<b>鍛えた後を見る = ON</b> 全部の札が「+」の姿。変わる所は緑の字 (コスト玉・数字・しきい値)。本家 Smith の「Show Upgrade」と同じ', (560, 330, 560, 300))
        s += caption(24, 4, '案A 鍛える (チェック ON): 一覧そのものが鍛えた後の姿になる。押した札 (深根) は蜂蜜の縁、下の帯に「深根 を鍛える」')
    else:
        s += note(1100, 470, 280, '<b>しおり</b> 紙の帯に墨の栞。掴んで引く／帯を押すとその位置へ。長さ＝見えている割合 (2行/4行)', (1380, 480, 1412, 300))
        s += note(430, 330, 300, '札を押す＝選ぶ。札の下のボタンは無し。フリックで慣性が付き、行の高さで止まる (吸着)', (560, 330, 500, 300))
        s += caption(24, 4, '案A 鍛える (チェック OFF): 7列×2行=14枚。左下のチェックで鍛えた後の姿に切り替え、押した札を下の帯で確定')
    return s + close()

def board_a_workshop():
    """工房: 合成結果と素材の板は今どおり右に。左の棚だけ案A (札の下のボタンを外し 2行＋しおり)"""
    s = board('current-workshop.jpg', dim=True, blur=True)
    s += runbar(place='工房', gold=400, center=bar_title('工房', '同じ色の2枚を1枚に (100G)。★はレシピの相手', x=395, w=520))
    sc, cols, gap = 0.8, 5, 14   # 5×160 + 4×14 = 856 (x 20〜876)、2行 = 232×2+14 = 478 (y 66〜544)
    s += grid(20, 66, cols, sc, DECK[:10], gap=gap, sel=0, stars=[1], marks={0: 'A'})
    s += bookmark(896, 66, 478, 0.0, 0.38)
    s += '<div class="abs small" style="left: 870px; top: 548px; width: 80px; text-align: center; color: #f4ecd6; text-shadow: 0 0 3px #000, 0 1px 0 #000; font-size: 13px">1〜10 / 26</div>'
    s += sort_chips(22, 626)
    s += '<div class="abs small" style="left: 400px; top: 632px; color: #f4ecd6; text-shadow: 0 0 3px #000, 0 1px 0 #000; font-size: 14px">押した札が素材 A → B。もう一度押すと外す</div>'
    # 右の板 (今の配置のまま)
    px, py, pw, ph = 950, 66, 492, 597
    s += '<div class="abs paper" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; border-radius: 12px"></div>' % (px, py, pw, ph)
    s += '<div class="abs deco" style="left: %dpx; top: %dpx; font-size: 15px; font-weight: 400">素材</div>' % (px + 16, py + 12)
    def slot(x, y, lab, cid=None, mark=None):
        if cid:
            r = '<div class="abs" style="left: %dpx; top: %dpx; width: 160px; height: 232px">%s' % (x, y, card(0, 0, cid, 0.8).replace('class="abs card"', 'class="abs card"', 1))
            r += '<div class="abs deco" style="left: -12px; top: -12px; width: 36px; height: 36px; border-radius: 50%%; background: #f6dd98; box-shadow: 0 0 0 2px #3b2f2f; font-size: 19px; display: flex; align-items: center; justify-content: center; z-index: 3">%s</div>' % mark
            r += '<div class="abs" style="right: -6px; top: -6px; width: 28px; height: 28px; border-radius: 50%; background: #f4ecd6; box-shadow: 0 0 0 1.5px #3b2f2f; font-size: 15px; display: flex; align-items: center; justify-content: center; z-index: 3">✕</div></div>'
            return r
        return '<div class="abs" style="left: %dpx; top: %dpx; width: 160px; height: 232px; display: flex; align-items: center; justify-content: center; text-align: center; border: 2px dashed #3b2f2f; border-radius: 8px; box-sizing: border-box; color: #574b48; font-size: 15px; line-height: 22px">%s</div>' % (x, y, lab)
    s += slot(px + 44, py + 40, '', 'green_ramp_sprout', 'A')
    s += '<div class="abs deco" style="left: %dpx; top: %dpx; font-size: 28px; color: #7a4e12">+</div>' % (px + 228, py + 136)
    s += slot(px + 288, py + 40, 'B<br><span class="small">もう1枚を押す<br>(★は光る)</span>')
    s += '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: 1.5px; background: rgba(59,47,47,0.35)"></div>' % (px + 16, py + 292, pw - 32)
    s += '<div class="abs deco" style="left: %dpx; top: %dpx; font-size: 15px; font-weight: 400">▼ 合成結果</div>' % (px + 16, py + 304)
    s += '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: 150px; border-radius: 8px; background: #eadfc4; box-shadow: inset 0 0 0 1.5px rgba(59,47,47,0.3); display: flex; align-items: center; justify-content: center; text-align: center; color: #574b48; font-size: 14px; line-height: 21px">2枚選ぶとここに札の実物<br>(名前・コスト・効果)</div>' % (px + 16, py + 332, pw - 32)
    s += button(px + 16, py + 498, pw - 32, 52, '合成する 100G', size=17, primary=True, disabled=True)
    s += button(px + 16, py + 556, pw - 32, 36, '見送る', size=15)
    s += note(600, 300, 320, '素材の板・合成結果は今どおり右。左の棚は 5列×2行=10枚 (今は 7枚)。札の下の「選ぶ」を外し、押した札に A/B の印', (700, 300, 660, 240))
    s += caption(24, 4, '案A 工房: 右の板はそのまま、左の棚だけ 案A (押す＝素材・2行・しおり)')
    return s + close()

# ================================================================ 案B ページ送り
def board_b():
    s = board('current-campfire.jpg', dim=True, blur=True)
    s += runbar(center=bar_title('鍛える', '札を押すと 元 → 鍛えた後 が出る'))
    s += grid(78, 66, 7, 0.9, DECK[:14])
    # 両端の矢印 (44 幅)
    s += '<div class="abs" style="left: 14px; top: 250px; width: 52px; height: 160px; border-radius: 10px; background: rgba(244,236,214,0.12); box-shadow: 0 0 0 1.5px rgba(244,236,214,0.35); display: flex; align-items: center; justify-content: center; color: rgba(244,236,214,0.5); font-size: 30px">‹</div>'
    s += '<div class="abs" style="left: 1396px; top: 250px; width: 52px; height: 160px; border-radius: 10px; background: rgba(244,236,214,0.35); box-shadow: 0 0 0 1.5px #f4ecd6; display: flex; align-items: center; justify-content: center; color: #f4ecd6; font-size: 30px; text-shadow: 0 0 4px #000">›</div>'
    # ページの点
    s += '<div class="abs" style="left: 640px; top: 626px; display: flex; gap: 10px; align-items: center; height: 30px"><span style="width: 14px; height: 14px; border-radius: 50%%; background: #f4ecd6; box-shadow: 0 0 0 1.5px #3b2f2f"></span><span style="width: 14px; height: 14px; border-radius: 50%%; background: rgba(244,236,214,0.25); box-shadow: 0 0 0 1.5px #3b2f2f"></span><span class="deco" style="margin-left: 8px; color: #f4ecd6; font-size: 16px; font-weight: 400; text-shadow: 0 0 3px #000, 0 1px 0 #000">1 / 2</span><span class="small" style="margin-left: 8px; color: #f4ecd6; text-shadow: 0 0 3px #000">← 横に引くと次の14枚</span></div>'
    s += sort_chips(22, 623)
    s += button(1264, 617, 176, 48, '戻る', size=16)
    s += note(1100, 430, 300, '縦にはスクロールしない。1ページ＝7列×2行の14枚を丸ごと差し替える (横フリックか両端の矢印)', (1200, 430, 1420, 400))
    s += caption(24, 4, '案B ページ送り: 半端に切れる行が無く、位置は「1 / 2」。26枚は2ページ・40枚でも3ページ')
    return s + close()

# ================================================================ 案C 帳面の目録 (一覧＋見本)
def board_c():
    s = board('current-campfire.jpg', dim=True, blur=True)
    s += runbar(center=bar_title('鍛える', '行を押すと右に 元 → 鍛えた後'))
    lx, ly, lw = 20, 66, 880
    s += '<div class="abs paper" style="left: %dpx; top: %dpx; width: %dpx; height: 540px; border-radius: 10px"></div>' % (lx, ly, lw)
    s += '<div class="abs" style="left: %dpx; top: %dpx; z-index: 2">%s</div>' % (lx + 12, ly + 8, sort_chips(0, 0).replace('color: #f4ecd6; text-shadow: 0 0 3px #000, 0 1px 0 #000;', ''))
    s += '<div class="abs small" style="left: %dpx; top: %dpx; z-index: 2">26枚・鍛え済み 0</div>' % (lx + 760, ly + 14)
    # 行 (44px × 10 行が見える)
    order = list(range(26))
    ry = ly + 48
    for k, i in enumerate(order[:10]):
        cid = DECK[i]; d = CARDS[cid]
        sel = (i == 0)
        s += '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: 44px; display: flex; align-items: center; gap: 10px; padding: 0 12px 0 10px; box-sizing: border-box; border-bottom: 1px solid rgba(59,47,47,0.25); background: %s%s">' % (lx + 4, ry + k * 44, lw - 8 - 30, '#fbf6e8' if sel else 'transparent', '; box-shadow: inset 4px 0 0 0 #e0b25a' if sel else '')
        s += '<div style="position: relative; width: 34px; height: 34px; flex: none">%s<div class="abs deco" style="inset: 0; display: flex; align-items: center; justify-content: center; font-size: 16px; padding-bottom: 1px">%s</div></div>' % (img(ORB, 0, 0, 34, 34), d['cost'])
        s += '<div style="position: relative; width: 60px; height: 36px; flex: none; background: #20233a; box-shadow: 0 0 0 1px #3b2f2f; overflow: hidden">%s</div>' % img(ART[cid], 0, 0, 60, 36)
        s += '<span class="deco" style="font-size: 16px; font-weight: 400; width: 130px; flex: none; white-space: nowrap; overflow: hidden; text-overflow: ellipsis">%s</span>' % d['name']
        s += pill(TYPE_JA[d['type']], '#f4ecd6', TYPE_COL[d['type']])
        s += '<span class="small" style="flex: 1; min-width: 0; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; font-size: 14px">%s</span>' % d['one']
        s += '</div>'
    s += bookmark(lx + lw - 30, ly + 48, 484, 0.0, 0.38)
    # 右の見本
    s += card(946, 74, 'green_ramp_sprout', 0.9)
    s += '<div class="abs deco" style="left: 1136px; top: 180px; font-size: 30px; color: #f4ecd6; text-shadow: 0 0 4px #000">→</div>'
    s += card(1178, 74, 'green_ramp_sprout', 0.9, plus=True, cost=0, body='<span class="good">コスト 1 → <b>0</b></span><br>エナジー上限+<b>1</b><br>ブロック<b>2</b><br>消滅')
    s += button(946, 360, 412, 60, '<span style="display:flex;align-items:center;gap:10px">%s この札を鍛える</span>' % ic('hammer', 24), size=19, primary=True)
    s += '<div class="abs small" style="left: 946px; top: 430px; width: 412px; color: #f4ecd6; text-shadow: 0 0 3px #000, 0 1px 0 #000; font-size: 14px; line-height: 20px">札の絵は行にも小さく出る。長押しで実物の大きさ。<br>並び替えで「鍛えられる札だけ」「コスト順」に切り替え</div>'
    s += button(1264, 617, 176, 48, '戻る', size=16)
    s += note(400, 560, 420, '1行 44px で10行が見える。26枚でも1画面と少し。40枚のデッキ (幕3) では札の絵より名前で探す方が速い', (500, 560, 460, 500))
    s += caption(24, 4, '案C 帳面の目録: 左に札の目録 (コスト・絵・名前・タイプ・効果の1行)、右に選んだ札の見本と 元→鍛えた後')
    return s + close()

# ================================================================ 比較と推奨
def board_compare():
    W, H = 1180, 780
    s = '<div style="position: relative; width: %dpx; height: %dpx; overflow: hidden; background: #f4ecd6; padding: 26px 30px; box-sizing: border-box; color: #3b2f2f">' % (W, H)
    s += '<div class="deco" style="font-size: 22px">比較と推奨</div>'
    s += '<div class="small" style="margin-top: 4px; font-size: 14px; line-height: 20px">3案とも共通: <b>札を押して選ぶ→下の帯で確定</b> (札の下のボタンを無くす)・見出しを上部バーに畳む・慣性 (フリック)・並び替え・枚数の表示・PC も同じしおり。違うのは<b>26枚をどう繰るか</b> (縦に流す／ページで差し替える／目録で探す)</div>'
    rows = [
        ('一度に見える札', '14枚 (7列×2行)。26枚は2画面', '14枚。26枚は2ページ', '10行 (絵は小さく)。26枚は1画面と少し'),
        ('26枚を見る操作', 'フリック1〜2回。しおりで位置が分かる', '横に1回引く。ページの点で位置が分かる', '縦に軽く1回。しおり'),
        ('探し方', '絵で探す (今と同じ)', '絵で探す', '名前・コストで探す (絵は 60×36)'),
        ('誤タップ', '押す＝選ぶだけ。下の帯の「〜を鍛える」で確定するので安全', '同じ', '同じ。行は 44px 高で押しやすい'),
        ('工房・一覧への展開', '◎ 同じ棚。工房は右の板をそのままに左の棚だけ (ユーザー裁定)', '○ 工房は作業台を下に。一覧はページ', '△ 一覧 (閲覧) が目録になると札の絵が見えない'),
        ('40枚 (幕3) のとき', '3画面。しおりが短くなる', '3ページ', '4画面。ただし名前で探せる'),
        ('本家との距離', '近い (StS も格子を縦に流す。バー付き)', '中', '遠い (帳面)'),
        ('実装の重さ', '軽い (CardGrid 1か所: 列数・倍率・ボタン撤去・Scrollbar・慣性) ＋ 鍛えた後を見るチェック', '中 (ページの器と横フリック)', '中 (行の部品・並び替え・見本の板)'),
    ]
    s += '<table style="margin-top: 14px; border-collapse: collapse; width: 100%; font-size: 14px; line-height: 19px"><tr><th style="text-align: left; padding: 6px 8px; border-bottom: 1.5px solid #3b2f2f; width: 150px"></th><th style="text-align: left; padding: 6px 8px; border-bottom: 1.5px solid #3b2f2f; background: #fbf6e8">案A 一面の棚としおり ★推奨</th><th style="text-align: left; padding: 6px 8px; border-bottom: 1.5px solid #3b2f2f">案B ページ送り</th><th style="text-align: left; padding: 6px 8px; border-bottom: 1.5px solid #3b2f2f">案C 帳面の目録</th></tr>'
    for r in rows:
        s += '<tr><td style="padding: 7px 8px; border-bottom: 1px solid rgba(59,47,47,0.25); color: #574b48; vertical-align: top">%s</td>' % r[0]
        for j, c in enumerate(r[1:]): s += '<td style="padding: 7px 8px; border-bottom: 1px solid rgba(59,47,47,0.25); vertical-align: top%s">%s</td>' % ('; background: #fbf6e8' if j == 0 else '', c)
        s += '</tr>'
    s += '</table>'
    s += '<div style="margin-top: 16px; display: flex; gap: 20px">'
    s += '<div style="flex: 1; font-size: 14px; line-height: 20px"><div class="deco" style="font-size: 16px">推奨: 案A 一面の棚としおり</div>操作しにくさの正体は「見える札が 6.5 枚・慣性なし・位置の目印なし・札の下の小さなボタン」の4つで、どれも棚の作り方の問題。棚を一面 (7列×2行) に広げて札の下のボタンを外すだけで見える枚数が2倍になり、しおりと慣性で「どこまで来たか」「あと何回引くか」が消える。5画面が同じ部品なので直しは1か所。<br><span class="small">案B は「切れる行が無い」のが利点だが、ページの境で隣の札と見比べられない。案C は幕3の40枚デッキで効くが、絵で選ぶ楽しさが薄れるので閲覧の一覧には向かない。案A の並び替えに「鍛えられる札だけ」を足せば案C の利点は拾える。</span></div>'
    s += '<div style="flex: 1; font-size: 14px; line-height: 20px"><div class="deco" style="font-size: 16px">案A の細目</div>① 札 0.9 倍 (180×261)・7列・2行＝14枚。工房は右の板をそのままに 0.8 倍・5列・10枚<br>② しおり: 幅 22 の紙の帯＋墨の栞 (指の的は 44)。掴んで引く／帯を押すと跳ぶ。長さ＝見えている割合<br>③ フリックの慣性 (減速 0.135)・行の高さに吸着<br>④ 札を押す＝選ぶ。下の帯に「〜を鍛える」(380×52)。鍛えるは「鍛えた後を見る」のチェックで一覧全部が＋の姿 (本家 Smith の Show Upgrade)。変わる所は緑の字<br>⑤ 見出しは上部バーの中央の札「鍛える ─ 札を押すと…」<br>⑥ 左下に並び (元の順／コスト／タイプ／名前)、右下に戻る。しおりの下に「1〜14 / 26」<br>⑦ PC は今の配置のまま、しおりと慣性だけ足す</div>'
    s += '</div></div>'
    return s

if __name__ == '__main__':
    write('Main', board_a_forge(upgraded=True, sel=2))
    write('AForgeOff', board_a_forge(upgraded=False, sel=None))
    write('AWorkshop', board_a_workshop())
    write('AGrid', board_a())
    write('CurrentForge', board_current_campfire())
    write('CurrentWorkshop', board_current_workshop())
    write('BPage', board_b())
    write('CList', board_c())
    write('Compare', board_compare())
    print('ok')

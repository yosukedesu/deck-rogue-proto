# build.py — ギア（消耗品）の置き場 (2026-09-17 ユーザー「/designで相談」)。
# 提案書 docs/parts-proposal-2026-09-17.md §6「置き場」の設計カンバス。裁定済みの骨格＝拾ったギアを自ターンに1個・魔素1で組む／持ち歩き10／魔素は上限10。
# 問い＝戦闘画面のどこにギア (最大10個) と魔素の数を置くか。3案 (A 匣の帯＝からくりの隣／B 足元の匣＝引き出し／C 上部バー＝本家形) ＋ 使う時の窓 ＋ 報酬画面 ＋ 比較。
# 下地は docs/design/enemy-display と同じ (UI を消した実機のスクショ)。敵の表示は 2026-09-16 採択の案A (頭上に意図・足元に名前とHP) の描き方を enemy-display/build.py から借りる。
# 使い方: python3 build.py            → この場所に *.dc.html (画像は data URI。ローカル確認用)
#         python3 build.py --blob DIR → DIR/project/*.dc.html (画像は assets.json の /_blob/ URL。Design キャンバスへ publish する束)
import base64, importlib.util, json, os, sys
OUT = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(OUT, '..', '..', '..'))
ART_DIR = os.path.join(REPO, 'unity', 'Assets', 'Resources', 'Art')

spec = importlib.util.spec_from_file_location('ed', os.path.join(OUT, '..', 'enemy-display', 'build.py'))
ed = importlib.util.module_from_spec(spec); spec.loader.exec_module(ed)
from_ed = ['INK', 'INK_SOFT', 'PAPER', 'PAPER2', 'PAPER3', 'BRASS', 'BRASS_LIGHT', 'BRASS_INK', 'MANA', 'MANA_LIGHT', 'MANA_INK', 'ROSE', 'SKY', 'SKY_LIGHT', 'SKY_INK', 'PLUM', 'PLUM_LIGHT', 'PLUM_INK', 'BAD_INK']
for k in from_ed: globals()[k] = getattr(ed, k)
W, H = ed.W, ed.H; LINE_PH = ed.LINE_PH; LINE_PC = ed.LINE_PC
NIGHT = '#20233a'; NIGHT_DEEP = '#1a1c33'; MOSS_INK = '#276a34'; MOSS_LIGHT = '#cfeacc'

BLOB = None
out_dir = OUT
if len(sys.argv) > 2 and sys.argv[1] == '--blob':
    with open(os.path.join(OUT, 'assets.json'), encoding='utf-8') as f: BLOB = json.load(f)
    out_dir = os.path.join(sys.argv[2], 'project'); os.makedirs(out_dir, exist_ok=True)

def uri(path):
    with open(path, 'rb') as f: return 'data:image/%s;base64,' % ('jpeg' if path.endswith('.jpg') else 'png') + base64.b64encode(f.read()).decode('ascii')
def src(name):
    """画像の参照: --blob なら /_blob/ URL、素なら data URI"""
    if BLOB: return BLOB[name]
    return uri(os.path.join(OUT, name) if name.endswith('.jpg') else os.path.join(ART_DIR, 'icons', name + '.png'))
ICONS = ['intent_attack', 'intent_defend', 'sword', 'shield', 'growth', 'gold', 'energy', 'set']
ed.ICON = {k: src(k) for k in ICONS}     # enemy-display の描き手が読む絵を差し替える
ed.ICON['intent_hex'] = ed.ICON['sword']; ed.ICON['exposed'] = ed.ICON['sword']; ed.ICON['burn'] = ed.ICON['sword']

# enemy-display の部品を借りる
sheet, pill, hpbar, note, caption, close = ed.sheet, ed.pill, ed.hpbar, ed.note, ed.caption, ed.close
intent_tag, feet_ledger_a, hand_hint, end_turn, ic = ed.intent_tag, ed.feet_ledger_a, ed.hand_hint, ed.end_turn, ed.ic
TRIO, PH_TRIO, PC_TRIO = ed.TRIO, ed.PH_TRIO, ed.PC_TRIO

HEAD = ed.HEAD.replace('<html>', '<html lang="ja">').replace('</style>', '''
    .cog { display: block; flex: none; }
    .tok { position: absolute; box-sizing: border-box; }
    .dim { filter: brightness(0.5); }
  </style>''')
TAIL = '''</x-dc>
<script data-dc-script data-props='{"$preview":{"width":%d,"height":%d}}'>
class Component extends DCLogic {
  renderVals() { return {}; }
}
</script>
</body>
</html>
'''
def write(name, html, w=W, h=H):
    with open(os.path.join(out_dir, name + '.dc.html'), 'w', encoding='utf-8') as f: f.write(HEAD + html + TAIL % (w, h))
def board(bg, w=W, h=H, dim=False):
    return ed.board(src(bg) if bg else None, w, h, dim)

# ---- ギアの部品 ----
def cog(size=24, color=BRASS, ring=INK):
    """歯車の記号 (点線の輪＝歯・中の円・軸穴)。emoji は使わない"""
    r = size / 2
    return ('<svg class="cog" viewBox="0 0 24 24" width="%d" height="%d">'
            '<circle cx="12" cy="12" r="7.5" fill="none" stroke="%s" stroke-width="5" stroke-dasharray="3.3 2.6"></circle>'
            '<circle cx="12" cy="12" r="6.6" fill="%s" stroke="%s" stroke-width="1.2"></circle>'
            '<circle cx="12" cy="12" r="2.2" fill="%s"></circle></svg>' % (size, size, color, color, ring, ring))
RAR_EDGE = {'C': (INK, 1.5), 'U': (SKY, 3), 'R': (BRASS, 3)}
RAR_NAME = {'C': 'コモン', 'U': 'アンコモン', 'R': 'レア'}
# 見本の持ち物 (台帳 §3 の仮名)
GEARS = [dict(name='発条', rar='C', charges=2, effect='ブロック6', fam='汎用'),
         dict(name='油差し', rar='C', charges=1, effect='2ドロー', fam='汎用'),
         dict(name='火薬', rar='C', charges=1, effect='敵全体に6ダメージ', fam='汎用'),
         dict(name='楔', rar='U', charges=1, effect='対象の次の行動を打ち消す', fam='干渉'),
         dict(name='清めの水', rar='U', charges=1, effect='自分の状態異常を全て消す', fam='清め'),
         dict(name='蘇りの発条', rar='R', charges=1, effect='この戦闘中、致死ダメージを一度耐えて HP1 で立つ', fam='大物')]
def gear_token(x, y, g, w=64, h=66, selected=False, dimmed=False, z=None):
    edge, ew = RAR_EDGE[g['rar']]
    ring = '0 0 0 %spx %s' % (ew, edge)
    if selected: ring = '0 0 0 3px %s, 0 0 12px rgba(201,154,58,0.7)' % BRASS
    s = '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; border-radius: 6px; background: %s; box-shadow: %s, 0 3px 8px rgba(0,0,0,0.4)%s%s">' % (
        x, y, w, h, PAPER2, ring, '; opacity: 0.45' if dimmed else '', ('; z-index: %d' % z) if z is not None else '')
    s += '<div class="abs" style="left: 3px; top: 3px; right: 3px; height: %dpx; background: %s; border-radius: 3px; display: flex; align-items: center; justify-content: center">%s</div>' % (h - 30, NIGHT, cog(26, BRASS if g['fam'] != '干渉' else MANA, PAPER))
    s += '<div class="abs" style="left: 0; right: 0; bottom: 3px; height: 22px; display: flex; align-items: center; justify-content: center; font-size: 13px; color: %s; white-space: nowrap; overflow: hidden">%s</div>' % (INK, g['name'])
    if g['charges'] > 1:
        s += '<div class="abs deco" style="right: -7px; top: -7px; width: 20px; height: 20px; border-radius: 50%%; background: %s; color: %s; box-shadow: 0 0 0 1.5px %s; font-size: 13px; display: flex; align-items: center; justify-content: center">%d</div>' % (PAPER, INK, INK, g['charges'])
    return s + '</div>'
def empty_slot(x, y, w=64, h=66):
    return '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; border: 2px dashed rgba(244,236,214,0.5); border-radius: 6px; background: rgba(244,236,214,0.07); box-sizing: border-box"></div>' % (x, y, w, h)
def karakuri_token(x, y, state='prep', w=68, h=74):
    band = {'prep': ('#6a6a74', '準備中'), 'live': ('#7a6a3a', 'あと2回')}[state]
    s = '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; border-radius: 6px; background: %s; box-shadow: 0 0 0 2px %s, 0 3px 8px rgba(0,0,0,0.4)">' % (x, y, w, h, PAPER2, MANA if state == 'live' else '#6a6a74')
    s += '<div class="abs" style="left: 2px; top: 2px; right: 2px; height: 38px; background: %s; border-radius: 3px; display: flex; align-items: center; justify-content: center">%s</div>' % (NIGHT, ic('set', 22))
    s += '<div class="abs deco" style="left: 0; right: 0; bottom: 0; height: 28px; border-radius: 0 0 6px 6px; background: %s; color: %s; font-size: 15px; font-weight: 400; display: flex; align-items: center; justify-content: center">%s</div>' % (band[0], PAPER, band[1])
    return s + '</div>'
def empty_karakuri(x, y): return empty_slot(x, y, 68, 74)
def outl(x, y, text, size=13):
    return '<div class="abs outl" style="left: %dpx; top: %dpx; font-size: %dpx; letter-spacing: 0.06em; white-space: nowrap">%s</div>' % (x, y, size, text)
def mana_pill(n, mx=10, h=20, size=13):
    """魔素の札: 歯車の記号＋数"""
    return '<span style="display: inline-flex; align-items: center; gap: 4px; height: %dpx; padding: 0 7px 0 4px; border-radius: 6px; background: %s; color: %s; box-shadow: 0 0 0 1px %s; font-size: %dpx; white-space: nowrap; flex: none">%s魔素 %d<span style="color: %s">/%d</span></span>' % (h, MANA_LIGHT, MANA_INK, MANA_INK, size, cog(14, MANA_INK, MANA_LIGHT), n, INK_SOFT, mx)

# ---- 上部バー (enemy-display の topbar に 魔素 の札と、案C のギアの枠を足せる形) ----
def topbar(w, mana_in_bar=True, gear_slots=None):
    s = sheet(70, 11, 230, 34, PAPER2) + '<div class="abs" style="left: 10px; top: 0; height: 34px; display: flex; align-items: center; gap: 8px; white-space: nowrap"><span style="font-size: 13px; color: %s; letter-spacing: 0.1em">幕 1 · 行 3</span><span class="deco" style="font-size: 18px">探り屋の三人組</span></div></div>' % INK_SOFT
    s += sheet(310, 11, 92, 34, PAPER2) + '<div class="abs" style="left: 10px; top: 0; height: 34px; display: flex; align-items: center; gap: 6px"><span style="font-size: 13px; color: %s">ターン</span><span class="deco" style="font-size: 17px; font-weight: 400">2</span></div></div>' % INK_SOFT
    s += sheet(w // 2 - 75, 12, 150, 32, PAPER3) + '<div class="abs deco" style="inset: 0; display: flex; align-items: center; justify-content: center; font-size: 15px; font-weight: 400; letter-spacing: 0.2em">あなたの番</div></div>'
    if mana_in_bar:
        s += sheet(w - 270, 11, 112, 34, PAPER2) + '<div class="abs" style="inset: 0; display: flex; align-items: center; justify-content: center">%s</div></div>' % mana_pill(3, h=22, size=14)
    s += sheet(w - 150, 11, 84, 34, PAPER2) + '<div class="abs deco" style="inset: 0; display: flex; align-items: center; justify-content: center; gap: 4px; font-size: 17px; font-weight: 400">50<span style="font-size: 13px; color: %s">G</span></div></div>' % INK_SOFT
    s += sheet(w - 56, 11, 44, 34, PAPER2) + '<div class="abs" style="inset: 0; display: flex; align-items: center; justify-content: center; font-size: 20px">≡</div></div>'
    if gear_slots is not None:
        # 案C: ターンの札の右に 30×30 の枠を並べる (中央の手番の札まで 244px＝7枠)
        x = 412
        for i in range(7):
            if i < len(gear_slots):
                g = gear_slots[i]; edge, ew = RAR_EDGE[g['rar']]
                s += '<div class="abs" style="left: %dpx; top: 13px; width: 30px; height: 30px; border-radius: 5px; background: %s; box-shadow: 0 0 0 %spx %s; display: flex; align-items: center; justify-content: center">%s</div>' % (x, NIGHT, ew, edge, cog(20, BRASS, PAPER))
                if g['charges'] > 1: s += '<div class="abs deco" style="left: %dpx; top: 6px; width: 15px; height: 15px; border-radius: 50%%; background: %s; box-shadow: 0 0 0 1px %s; font-size: 11px; display: flex; align-items: center; justify-content: center">%d</div>' % (x + 22, PAPER, INK, g['charges'])
            else:
                s += '<div class="abs" style="left: %dpx; top: 13px; width: 30px; height: 30px; border-radius: 5px; border: 1.5px dashed rgba(244,236,214,0.5); box-sizing: border-box"></div>' % x
            x += 32
    return s
def self_ledger(x, y, mana=None):
    """自分の札 (HP／被ダメ予測／資源)。mana を渡すと資源の行に魔素の札"""
    s = sheet(x, y, 300, 76, PAPER2)
    s += hpbar(12, 10, 276, 16, 71, 80)
    s += '<div class="abs" style="left: 12px; top: 34px; font-size: 14px; line-height: 20px; color: %s">被ダメ <b style="color: %s">17</b> − <span style="color: %s">盾 5</span> ＝ HP <b style="color: %s">−12</b> → 59</div>' % (INK_SOFT, BAD_INK, SKY_INK, BAD_INK)
    s += '<div class="abs" style="left: 12px; top: 54px; display: flex; gap: 6px">%s%s%s</div>' % (pill('成長 +2', MOSS_INK, MOSS_LIGHT, icon_name='growth'), pill('3 / 4 エナジー', BRASS_INK, BRASS_LIGHT), mana_pill(mana) if mana is not None else '')
    return s + '</div>'
def enemies_phone():
    s = ''
    for e, p in zip(TRIO, PH_TRIO):
        edge = BRASS if e['aimed'] else None
        s += intent_tag(p['cx'], p['top'] - 8, e, edge=edge)
        s += feet_ledger_a(p['cx'], LINE_PH, e, 176, edge=edge)
    return s
def base_phone(mana_in_bar=True, gear_slots=None, mana_in_ledger=None):
    return board('base-phone-trio.jpg') + topbar(W, mana_in_bar, gear_slots) + enemies_phone() + self_ledger(30, LINE_PH - 76, mana_in_ledger) + hand_hint(W, 390) + end_turn(W - 220, 470)

# 舞台の匣 (リーダーの足元の実物)。スマホの下地では x≈458 y≈303 (幅53 高37)。案B の押す的
BOX = dict(x=458, y=303, w=53, h=37)

# ================================================================ 現状
def board_current():
    s = board('current-phone.jpg', 1100, 508)
    s = s.replace('overflow: hidden; background: #0f1120', 'overflow: visible; background: #0f1120')
    s = '<div style="position: relative; width: 1100px; height: 700px; overflow: hidden; background: #0f1120">' + s
    s += note(150, 130, 250, '<b>①</b> 左上の帯＝からくり (仕込み札) の枠 2つ。匣の中身はいまここにしか出ていない。右へ空いている', (150, 160, 60, 90))
    s += note(560, 60, 260, '<b>②</b> 上部バーは 5枚の札で満杯。G の左に魔素を足す余地は 110px ほど', (700, 60, 960, 24))
    s += note(90, 300, 240, '<b>③</b> 足元の匣 (実物) は仕込むと蓋が開くだけで、押せない', (200, 300, 345, 240))
    s += note(620, 380, 300, '<b>④</b> エナジーの輪と手札の間・ターン終了の上は空いている (ただし手札の扇が伸びると重なる)', (620, 420, 190, 330))
    s += close()
    s += '<div class="abs" style="left: 24px; top: 528px; width: 1052px; font-size: 14px; line-height: 21px; color: %s">問い＝<b>ギア (最大10個・自ターンに1個を魔素1で組む)</b> と <b>魔素 (0〜10)</b> を戦闘画面のどこに置くか。<br>条件: ①いつでも見えている (抱えて死ぬのは学習に任せる＝見えていないと学習が起きない) ②1タップで使える (死線の総動員は手数が命) ③匣の中身＝からくりと同じ物として読める ④4体戦・ボス戦・PC でも同じ形。</div>' % PAPER
    s += caption(24, 672, '現状 (スマホ 1462×675・探り屋の三人組)。ギアの置き場の候補 ①〜④')
    return s + close()

# ================================================================ 案A: 匣の帯 (からくりの隣)
def gear_band(x, y, gears, cap=10, show=6):
    """からくりの枠の右に、細い仕切りを挟んでギアのトークンを並べる。溢れは「+N」"""
    s = outl(x, y - 20, 'からくり 0 / 2') + karakuri_token(x, y, 'prep') + empty_karakuri(x + 76, y)
    gx = x + 76 + 68 + 14
    s += '<div class="abs" style="left: %dpx; top: %dpx; width: 0; height: 74px; border-left: 2px dotted rgba(244,236,214,0.5)"></div>' % (gx - 8, y)
    s += outl(gx, y - 20, 'ギア %d / %d' % (len(gears), cap))
    for i, g in enumerate(gears[:show]): s += gear_token(gx + i * 70, y + 4, g)
    rest = len(gears) - show
    if rest > 0: s += '<div class="abs outl deco" style="left: %dpx; top: %dpx; font-size: 15px">+%d</div>' % (gx + show * 70 + 4, y + 26, rest)
    elif len(gears) < cap: s += empty_slot(gx + len(gears) * 70, y + 4)
    return s
def board_a():
    s = base_phone()
    s += gear_band(24, 82, GEARS)
    s += note(660, 175, 300, '<b>ギアはからくりの右</b>: 同じ帯に「仕込み札 2枠｜ギア 6/10」。匣の中身が一列に並ぶ＝からくりとギアは同じ物 (匣に入れる実物)', (700, 200, 600, 125))
    s += note(1180, 400, 260, '<b>魔素は G の隣</b> (ラン資源は上部バー)。使う時の窓にも「魔素 1 (3→2)」を出すので、ここは残高だけ', (1260, 400, 1230, 46))
    s += note(24, 420, 300, 'トークン 64×66 (からくりの 68×74 より一回り小さい)。回数つき (発条) は右上に残り回数。レア度は紙の外線 (C 墨／U 空／R 蜂蜜)＝札と同じ。干渉系 (楔) だけ歯車が青緑', (170, 420, 300, 150))
    s += caption(24, 648, '案A 匣の帯 — からくりの隣にギアを並べる (スマホ)。魔素は上部バーの G の隣')
    return s + close()
def board_a_window():
    s = base_phone()
    s += gear_band(24, 82, GEARS[:5] + [dict(GEARS[5])])
    # 選んだトークン (蘇りの発条＝6つ目) を光らせ、その下に窓
    gx = 24 + 76 + 68 + 14 + 5 * 70
    s += gear_token(gx, 86, GEARS[5], selected=True, z=8)
    wx, wy, ww, wh = gx - 120, 170, 330, 168
    s += sheet(wx, wy, ww, wh, PAPER, z=7)
    s += '<div class="abs" style="left: 50%%; top: -7px; width: 12px; height: 12px; margin-left: 30px; background: %s; transform: rotate(45deg); box-shadow: -1.5px -1.5px 0 0 %s"></div>' % (PAPER, INK)
    s += '<div class="abs" style="left: 12px; top: 10px; right: 12px; display: flex; align-items: center; gap: 8px"><span class="deco" style="font-size: 19px">蘇りの発条</span>%s<span style="margin-left: auto; font-size: 13px; color: %s">残り 1回</span></div>' % (pill('レア', BRASS_INK, BRASS_LIGHT, h=18), INK_SOFT)
    s += '<div class="abs" style="left: 12px; top: 44px; right: 12px; font-size: 14px; line-height: 20px">この戦闘中、致死ダメージを一度耐えて <b>HP1</b> で立つ。</div>'
    s += '<div class="abs" style="left: 12px; top: 92px; display: flex; align-items: center; gap: 8px; font-size: 14px; color: %s">%s<span>→ 2</span><span style="color: %s">・このターンはあと 0 個</span></div>' % (INK_SOFT, mana_pill(3), INK_SOFT)
    s += '<div class="abs deco" style="left: 12px; top: 122px; width: 150px; height: 36px; border-radius: 8px; background: %s; box-shadow: 0 0 0 1.5px %s; display: flex; align-items: center; justify-content: center; font-size: 16px; letter-spacing: 0.1em">魔素 1 で組む</div>' % (BRASS_LIGHT, INK)
    s += '<div class="abs deco" style="left: 176px; top: 122px; width: 110px; height: 36px; border-radius: 8px; background: %s; box-shadow: 0 0 0 1.5px %s; display: flex; align-items: center; justify-content: center; font-size: 16px; font-weight: 400; letter-spacing: 0.1em">やめる</div>' % (PAPER2, INK)
    s += close()
    s += note(1000, 60, 340, '<b>使う時の窓</b>: トークンを押すと下に紙の窓 (確認ウィンドウと同じ形)。名前・レア度・残り回数／効果／魔素の収支 (3→2)／組む・やめる。対象を取るギア (楔・焼き鏝) は「組む」の代わりに敵の札を押す＝札のプレイと同じ操作', (1000, 130, 748, 172))
    s += note(24, 420, 300, '「このターンはあと 0 個」＝1ターン1個の残りをここで読む。魔素 0 なら「組む」は灰色＋理由「魔素がない」 (CannotPlay と同じ首振り)', (170, 420, 300, 300))
    s += caption(24, 648, '案A 使う時の窓 (蘇りの発条を押した所)')
    return s + close()
def board_a_pc():
    Wp, Hp = 1920, 1080
    s = board('base-pc-trio.jpg', Wp, Hp)
    for e, p in zip(TRIO, PC_TRIO):
        edge = BRASS if e['aimed'] else None
        s += intent_tag(p['cx'], p['top'] - 10, e, phone=False, edge=edge)
        s += feet_ledger_a(p['cx'], LINE_PC, e, 210, phone=False, edge=edge)
    # PC の自分の札: HP／予測／資源／からくり／ギア (2026-09-15 の「自分の札にからくりと置物も収める」の続き)
    x, y, w, h = 30, LINE_PC - 262, 600, 262
    s += sheet(x, y, w, h, PAPER2)
    s += hpbar(12, 10, 300, 20, 71, 80)
    s += '<div class="abs" style="left: 12px; top: 40px; font-size: 15px; line-height: 22px; color: %s">被ダメ <b style="color: %s">17</b> − <span style="color: %s">盾 5</span> ＝ HP <b style="color: %s">−12</b> → 59</div>' % (INK_SOFT, BAD_INK, SKY_INK, BAD_INK)
    s += '<div class="abs" style="left: 330px; top: 10px; display: flex; flex-direction: column; gap: 6px; align-items: flex-start">%s%s%s</div>' % (pill('成長 +2', MOSS_INK, MOSS_LIGHT, 14, 24, 'growth'), pill('3 / 4 エナジー', BRASS_INK, BRASS_LIGHT, 14, 24), mana_pill(3, h=24, size=14))
    s += '<div class="abs" style="left: 12px; top: 72px; font-size: 13px; color: %s; letter-spacing: 0.08em">からくり 0 / 2</div>' % INK_SOFT
    s += karakuri_token(12, 92, 'prep') + empty_karakuri(88, 92)
    s += '<div class="abs" style="left: 176px; top: 72px; font-size: 13px; color: %s; letter-spacing: 0.08em">ギア 6 / 10</div>' % INK_SOFT
    for i, g in enumerate(GEARS): s += gear_token(176 + i * 68, 92, g, w=62, h=62)
    s += '<div class="abs" style="left: 12px; top: 176px; font-size: 13px; color: %s; letter-spacing: 0.08em">置物</div>' % INK_SOFT
    s += '<div class="abs" style="left: 12px; top: 196px; display: flex; gap: 8px">%s%s</div>' % (pill('棘の蔓 — 攻撃ごとブロック2', INK, PAPER, 14, 40), pill('風渡り — 勢いを得るたび1ドロー', INK, PAPER, 14, 40))
    s += close()
    s += note(660, 560, 340, '<b>PC</b>: 自分の札 (600×262) の中に「からくり／ギア／置物」を段で置く。ギアは 62×62 で 10個まで横に (幅 860 まで伸ばせる。舞台の左 1/3 は空)。魔素は資源の列 (成長・エナジーの下)。上部バーの G の隣にも同じ数を出すかは案A と同じ裁定', (640, 600, 632, 600))
    s += caption(24, 1050, '案A PC (1920×1080)。自分の札の中にからくり・ギア・置物を段で。魔素は資源の列')
    return s + close(), Wp, Hp

# ================================================================ 案B: 足元の匣 (引き出し)
def box_badge(open_=False):
    b = BOX; cx = b['x'] + b['w'] // 2
    s = '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; border-radius: 6px; box-shadow: 0 0 0 2px %s, 0 0 14px rgba(201,154,58,0.6)"></div>' % (b['x'] - 6, b['y'] - 6, b['w'] + 12, b['h'] + 12, BRASS)
    # 匣の上の札: ⚙6 · 魔素3
    w = 128
    s += sheet(cx - w // 2, b['y'] - 44, w, 30, PAPER2, z=5)
    s += '<div class="abs" style="inset: 0; display: flex; align-items: center; justify-content: center; gap: 6px; font-size: 14px">%s<span class="deco">6</span><span style="color: %s">·</span>%s<span class="deco" style="color: %s">3</span></div>' % (cog(16, BRASS, PAPER2), INK_SOFT, cog(16, MANA_INK, PAPER2), MANA_INK)
    s += '<div class="abs" style="left: 50%%; bottom: -6px; width: 10px; height: 10px; margin-left: -5px; background: %s; transform: rotate(45deg); box-shadow: 1.5px 1.5px 0 0 %s"></div>' % (PAPER2, INK)
    s += close()
    return s
def board_b_closed():
    s = base_phone()
    s += outl(24, 62, 'からくり 0 / 2') + karakuri_token(24, 82, 'prep') + empty_karakuri(100, 82)
    s += box_badge()
    s += note(560, 180, 300, '<b>足元の匣が押す的</b>: 実物の匣に「ギア 6・魔素 3」の小さな札。押すと引き出しが開く (次の枚)。48px の的を匣の周りに取る', (560, 210, 500, 290))
    s += note(24, 420, 300, 'からくりの帯はいまのまま。ギアは匣の中に隠れる＝画面はいちばん静か。ただし<b>閉じている間は何を持っているか見えない</b> (数だけ)', (170, 420, 300, 120))
    s += caption(24, 648, '案B 足元の匣 — 閉じている時 (数と魔素だけ)')
    return s + close()
def board_b_open():
    s = base_phone()
    s += '<div class="abs" style="left: 0; top: 0; width: %dpx; height: %dpx; background: rgba(15,17,32,0.55)"></div>' % (W, H)
    s += outl(24, 62, 'からくり 0 / 2') + karakuri_token(24, 82, 'prep') + empty_karakuri(100, 82)
    b = BOX; cx = b['x'] + b['w'] // 2
    s += '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; border-radius: 6px; box-shadow: 0 0 0 2px %s, 0 0 14px rgba(201,154,58,0.6)"></div>' % (b['x'] - 6, b['y'] - 6, b['w'] + 12, b['h'] + 12, BRASS)
    tw, th = 640, 130; tx, ty = 180, 150
    s += sheet(tx, ty, tw, th, PAPER2, z=6)
    s += '<div class="abs" style="left: 12px; top: 8px; display: flex; align-items: center; gap: 10px"><span class="deco" style="font-size: 17px">匣の中</span><span style="font-size: 13px; color: %s">ギア 6 / 10</span>%s<span style="margin-left: auto"></span></div>' % (INK_SOFT, mana_pill(3))
    for i, g in enumerate(GEARS): s += gear_token(12 + i * 72, 40, g, w=64, h=66)
    s += empty_slot(12 + 6 * 72, 40) + empty_slot(12 + 7 * 72, 40)
    s += '<div class="abs deco" style="right: 12px; top: 8px; width: 80px; height: 26px; border-radius: 6px; background: %s; box-shadow: 0 0 0 1.5px %s; display: flex; align-items: center; justify-content: center; font-size: 14px; font-weight: 400">閉じる</div>' % (PAPER, INK)
    s += '<div class="abs" style="left: %dpx; bottom: -8px; width: 14px; height: 14px; background: %s; transform: rotate(45deg); box-shadow: 1.5px 1.5px 0 0 %s"></div>' % (cx - tx - 7, PAPER2, INK)
    s += close()
    s += note(1170, 400, 270, '<b>引き出し</b>: 匣から紙の盆が立ち上がり、ギアが一列に並ぶ。押すと案A と同じ窓 (組む／やめる)。開いている間は他が暗く沈む (確認の窓と同じ)＝<b>使うのに2タップ</b>', (1170, 440, 822, 215))
    s += caption(24, 648, '案B 足元の匣 — 開いた時 (引き出し)')
    return s + close()

# ================================================================ 案C: 上部バー (本家形)
def board_c():
    s = base_phone(gear_slots=GEARS)
    s += outl(24, 62, 'からくり 0 / 2') + karakuri_token(24, 82, 'prep') + empty_karakuri(100, 82)
    s += note(380, 60, 340, '<b>上部バー</b>: ターンの札の右に 30×30 の枠を 7つ (本家のポーション枠)。持ち歩き10 は入り切らない (7枠＋…)。歯車の絵だけで名前は無い＝押して読む', (500, 60, 500, 40))
    s += note(1040, 400, 300, '魔素は G の隣。ギアとの距離が遠い (左端と右端) ので「魔素 1 で組む」の収支は窓で読む', (1180, 400, 1230, 46))
    s += note(24, 420, 300, '本家と同じ場所なので迷わない。反面、スマホの上部バーは 34px 高＝歯車 20px・的 30px は指には小さい (規約 48)。的を 48 にすると幕/敵名の札を削る', (170, 420, 480, 40))
    s += caption(24, 648, '案C 上部バー (本家形)。歯車の絵だけの小さな枠')
    return s + close()

# ================================================================ 報酬画面 (札3枚の横にギア1つ)
def reward_card(x, y, name, cost, body, w=176, h=250):
    s = '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; height: %dpx; background: %s; border-radius: 8px 10px 8px 9px; box-shadow: 0 0 0 1.5px %s, 0 6px 14px rgba(0,0,0,0.45)">' % (x, y, w, h, PAPER, INK)
    s += '<div class="abs deco" style="left: 8px; top: 8px; width: 30px; height: 30px; border-radius: 50%%; background: %s; box-shadow: 0 0 0 1.5px %s; display: flex; align-items: center; justify-content: center; font-size: 17px">%d</div>' % (BRASS_LIGHT, INK, cost)
    s += '<div class="abs deco" style="left: 44px; top: 12px; font-size: 17px">%s</div>' % name
    s += '<div class="abs" style="left: 10px; top: 46px; right: 10px; height: 96px; background: %s; box-shadow: 0 0 0 1.5px %s"></div>' % (NIGHT, INK)
    s += '<div class="abs" style="left: 0; right: 0; top: 150px; text-align: center; font-size: 14px; line-height: 20px; padding: 0 10px">%s</div>' % body
    return s + '</div>'
def board_reward():
    s = '<div style="position: relative; width: %dpx; height: %dpx; overflow: hidden; background: %s">' % (W, H, NIGHT_DEEP)
    s += topbar(W)
    s += '<div class="abs" style="left: 0; right: 0; top: 66px; text-align: center; color: %s"><div class="deco" style="font-size: 24px; letter-spacing: 0.12em">戦利品</div><div style="font-size: 14px; color: #c4beb2; margin-top: 4px">探り屋の三人組 · +15G · 魔素 +1 (2 → 3)</div></div>' % PAPER
    cards = [('若葉の一撃', 1, '5貫通ダメージ<br>1ドロー'), ('木漏れ日', 1, 'ブロック7<br>1ドロー'), ('棘の蔓', 1, '置物<br>攻撃ごとブロック2')]
    for i, (n, c, b) in enumerate(cards): s += reward_card(240 + i * 196, 150, n, c, b)
    s += '<div class="abs" style="left: 240px; top: 410px; width: 568px; text-align: center; font-size: 14px; color: #c4beb2">札を 1枚 (見送りも可)</div>'
    s += '<div class="abs" style="left: 850px; top: 150px; width: 0; height: 250px; border-left: 2px dotted rgba(244,236,214,0.4)"></div>'
    # ギアの札
    gx, gy = 900, 150
    s += '<div class="abs" style="left: %dpx; top: %dpx; width: 200px; height: 272px; background: %s; border-radius: 8px 10px 8px 9px; box-shadow: 0 0 0 3px %s, 0 6px 14px rgba(0,0,0,0.45)">' % (gx, gy, PAPER2, SKY)
    s += '<div class="abs" style="left: 10px; top: 10px; right: 10px; height: 96px; background: %s; border-radius: 4px; box-shadow: 0 0 0 1.5px %s; display: flex; align-items: center; justify-content: center">%s</div>' % (NIGHT, INK, cog(56, MANA, PAPER))
    s += '<div class="abs" style="left: 10px; top: 114px; right: 10px; display: flex; align-items: center; gap: 6px"><span class="deco" style="font-size: 18px">楔</span>%s<span style="margin-left: auto; font-size: 13px; color: %s">1回</span></div>' % (pill('アンコモン', SKY_INK, SKY_LIGHT, h=18), INK_SOFT)
    s += '<div class="abs" style="left: 10px; top: 146px; right: 10px; font-size: 14px; line-height: 20px">対象の次の行動を打ち消す。<br><span style="color: %s">自ターンに魔素 1 で組む</span></div>' % INK_SOFT
    s += '<div class="abs deco" style="left: 10px; bottom: 10px; right: 10px; height: 36px; border-radius: 8px; background: %s; box-shadow: 0 0 0 1.5px %s; display: flex; align-items: center; justify-content: center; font-size: 15px; letter-spacing: 0.1em">取る (7 / 10)</div>' % (BRASS_LIGHT, INK)
    s += '</div>'
    s += '<div class="abs" style="left: 900px; top: 430px; width: 200px; text-align: center; font-size: 14px; color: #c4beb2">ギアを 1つ (札とは別枠)</div>'
    s += end_turn(W - 220, 560).replace('ターン終了', '次へ')
    s += note(1120, 150, 300, '<b>札3枚の横にギア1つ</b>: 札のピックとは別枠で両方取れる (本家のポーション報酬と同じ位置づけ)。ギアの札は 200×272・レア度の外線・歯車の大きな絵 (PixelLab の挿絵に差し替え)。魔素の増分は見出しの下に「魔素 +1 (2→3)」', (1120, 200, 1100, 250))
    s += note(1120, 420, 300, '<b>満杯 (10/10) なら「入れ替え」</b>: 「取る」を押すと持ち物の一覧が開き、1つ捨ててから入る (捨てるのをやめれば見送り)。ドロップは 60%＋pity なので出ない戦闘ではこの枠ごと無い', (1120, 470, 1100, 390))
    s += caption(24, 648, '報酬画面 (スマホ)。札3枚＋ギア1つ (別枠)。魔素は勝利で +1 (エリート・ボスは +2)')
    return s + close()

# ================================================================ 比較と原則
def board_compare():
    rows = [
        ('いつでも見える', '○ 帯に並ぶ (名前つき)', '△ 数だけ (中身は開いて見る)', '○ 上部バー (絵だけ・名前なし)'),
        ('使うまでのタップ', '2 (押す→組む)', '3 (匣→押す→組む)', '2 (押す→組む)'),
        ('指の的', '64×66 (規約 48 を満たす)', '匣 48 の輪／引き出しの中は 64×66', '30×30 (小さい。48 にすると上部バーの札を削る)'),
        ('10個の収まり', '6個＋「+N」(帯の幅 480)。4体戦でも敵の頭上 (x≥800) に掛からない', '引き出しは 8個＋2列目', '7個で切れる'),
        ('匣＝からくりとの関係', '同じ帯＝「匣の中身が一列」', '匣そのものが的＝世界観に最も忠実', '別の場所＝匣との縁が薄い'),
        ('魔素の置き場', '上部バー G の隣 (＋窓に収支)', '匣の札に数・引き出しに札', '上部バー G の隣'),
        ('ボス戦・4体戦', '帯は左上で不変', '匣の位置は舞台が決める (ズームで動く)', '不変'),
        ('PC', '自分の札の中に段で (からくり／ギア／置物)', '同じ引き出しを匣から', '上部バーに 10枠 (幅は足りる)'),
        ('実装', '中: 帯にトークン列＋窓。PC は自分の札に段を足す', '中〜大: 舞台の匣に当たり判定＋引き出しの開閉', '小: 上部バーに枠列'),
        ('弱点', '帯が横に長くなる (からくり 2枠＋ギア 6)', '閉じている間は忘れる＝「抱えて死ぬ」を助長', '的が小さい・名前が無い・10個入らない'),
    ]
    s = '<div style="position: relative; width: 1120px; height: 900px; overflow: hidden; background: %s; color: %s">' % (PAPER, INK)
    s += '<div class="abs deco" style="left: 30px; top: 18px; font-size: 24px">ギアの置き場 — 3案の比較と原則</div>'
    cols = [(30, 190), (220, 300), (520, 300), (820, 280)]
    heads = ['', 'A 匣の帯 (からくりの隣)', 'B 足元の匣 (引き出し)', 'C 上部バー (本家形)']
    y = 64
    for j, h in enumerate(heads):
        s += '<div class="abs deco" style="left: %dpx; top: %dpx; width: %dpx; font-size: 16px; border-bottom: 1.5px solid %s; padding-bottom: 6px">%s</div>' % (cols[j][0], y, cols[j][1] - 10, INK, h)
    y += 40
    for r in rows:
        for j, cell in enumerate(r):
            s += '<div class="abs" style="left: %dpx; top: %dpx; width: %dpx; font-size: 14px; line-height: 19px; %s">%s</div>' % (cols[j][0], y, cols[j][1] - 10, 'color: %s' % INK_SOFT if j == 0 else '', cell)
        y += 50
    y += 6
    s += '<div class="abs deco" style="left: 30px; top: %dpx; font-size: 18px">原則 (どの案でも)</div>' % y
    rules = [
        '① ギアは見えている物にする。死蔵は学習に任せる、と裁定した以上「持っているのに忘れる」画面は作らない',
        '② 使う操作は札のプレイと同じ文法 (押す→窓→組む／対象を取るなら敵の札を押す)。敵ターンには押せない＝灰色',
        '③ 魔素は収支を使う場所で読ませる (窓に「3→2」)。残高はラン資源なので上部バー (G の隣)',
        '④ 回数つき (発条・歯車) は右上の数字、レア度は紙の外線 (C 墨／U 空／R 蜂蜜)＝札の規約と同じ',
        '⑤ 実物は匣 (からくりの匣) だけ。ギアを組む時は舞台の匣の蓋が開いて閃く (からくりを動かす時と同じ演出)',
        '⑥ 報酬は札3枚とは別枠のギア1つ。満杯なら入れ替え',
    ]
    for i, r in enumerate(rules):
        s += '<div class="abs" style="left: 30px; top: %dpx; width: 1060px; font-size: 14px; line-height: 20px">%s</div>' % (y + 30 + i * 24, r)
    y += 30 + len(rules) * 24 + 12
    s += '<div class="abs" style="left: 30px; top: %dpx; width: 1060px; background: %s; box-shadow: 0 0 0 1.5px %s; border-radius: 8px; padding: 10px 14px; font-size: 14px; line-height: 20px; box-sizing: border-box"><b>推奨は A</b>。からくりの帯を右へ伸ばしてギアを名前つきで並べる＝いつでも見えて 2タップ、匣の中身が一列に読める。B は世界観に最も忠実だが「閉じている間は忘れる」が死線の総動員と正面から衝突する (匣の蓋を開ける演出は A でも使う)。C は本家の場所で迷わないが、スマホの上部バーでは的が 30px で規約 48 を割り、10個が入らない。<br>A の残る問い＝魔素を G の隣 (上部バー) に置くか、エナジーの隣 (自分の札) に置くか。組む時の収支は窓で読むので、残高はどちらでも成立する。</div>' % (y, PAPER3, INK)
    return s + '</div>'

if __name__ == '__main__':
    write('Main', board_a())
    write('AWindow', board_a_window())
    apc, wp, hp = board_a_pc(); write('APC', apc, wp, hp)
    write('BClosed', board_b_closed()); write('BOpen', board_b_open())
    write('C', board_c())
    write('Reward', board_reward())
    write('Current', board_current(), 1100, 700)
    write('Compare', board_compare(), 1120, 900)
    canvas = dict(
        artboards=[
            dict(file='Current.dc.html', x=0, y=0, w=1100, h=700, title='現状と問い'),
            dict(file='Main.dc.html', x=1560, y=0, w=W, h=H, title='案A 匣の帯 (からくりの隣)'),
            dict(file='AWindow.dc.html', x=1560, y=800, w=W, h=H, title='案A 使う時の窓'),
            dict(file='APC.dc.html', x=3120, y=0, w=1920, h=1080, title='案A PC'),
            dict(file='BClosed.dc.html', x=0, y=900, w=W, h=H, title='案B 足元の匣 (閉)'),
            dict(file='BOpen.dc.html', x=0, y=1700, w=W, h=H, title='案B 足元の匣 (開)'),
            dict(file='C.dc.html', x=1560, y=1600, w=W, h=H, title='案C 上部バー (本家形)'),
            dict(file='Reward.dc.html', x=3120, y=1200, w=W, h=H, title='報酬画面 (札3枚＋ギア1つ)'),
            dict(file='Compare.dc.html', x=3120, y=2000, w=1120, h=900, title='比較と原則'),
        ],
        annotations=[dict(id='brief', x=0, y=-160, w=1200, text='ギアの置き場 (2026-09-17 部品＝消耗品の具体化。提案書 docs/parts-proposal-2026-09-17.md §6)\n拾ったギアを自ターンに1個・魔素1で組む／持ち歩き10／魔素 0〜10。戦闘画面のどこに置くかの3案＋使う時の窓＋報酬画面。\n下地は UI を消した実機のスクショ (スマホ 1462×675・PC 1920×1080)。敵の表示は 2026-09-16 採択の案A。')],
        launch=dict(view='canvas'),
    )
    with open(os.path.join(OUT, 'canvas.json'), 'w', encoding='utf-8') as f: json.dump(canvas, f, ensure_ascii=False, indent=1)
    if BLOB:
        boards = {a['file']: dict(x=a['x'], y=a['y'], w=a['w'], h=a['h'], title=a['title']) for a in canvas['artboards']}
        idx = dict(v=3, createdOnFiles=dict(v=1, at='2026-09-17T12:00:00Z'), title='ギアの置き場', launch=dict(view='canvas'), pages=[],
                   boards=boards, order=[a['file'] for a in canvas['artboards']],
                   notes={'brief': dict(x=0, y=-320, w=1300, text=canvas['annotations'][0]['text'], size='m')}, designSystems=[])
        with open(os.path.join(out_dir, 'canvas.json'), 'w', encoding='utf-8') as f: json.dump(idx, f, ensure_ascii=False, indent=1)
    print('ok', out_dir)

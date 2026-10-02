#!/usr/bin/env python3
"""HD-2D 見本の配置の検査 (2026-09-30 P08。計画 docs/design/hd2d-slice-plan-2026-09-30.md §1-2 の3・§4 P08)。

dumplayout=1 で撮った <名前>.layout.json (P01 の hd2d-layout/1) を読み、札とキャラの重なりと距離を検査する。
矩形は全部 PNG の画素 [x, y, w, h] (左上が原点・y は下向き)。違反が1つでもあれば終了コード 1。

使い方
  scripts/hd2d-layout-check.py <フォルダか .layout.json …> [--md 表.md] [--json 結果.json] [--draw <絵の置き場>] [--moving] [--design <設計図.json>]
  --draw を付けると、PNG に矩形と違反を描いた確認の絵を書く (PNG が隣にある時)。
  --moving = 演出の途中のコマとして読む (板と矩形のずれの許容を 2px に)。STATE に play=・endplay=・fire=・entershots= があれば自動で 2px。

規則 (違反 = 数える。注意 = 表に出すだけ)
  L1 帳面と手札: 敵の帳面・自分の札の下端と、横に重なる手札の札の上端 (傾きの分を除いた札の中央の上端) の間が 6 キャンバス単位以上
     (PC 6px・スマホ 7.9px。スマホは設計の間隔が 6 = BattleView.StatusLineY のため。計画の「8px」はこの読み)。--hand-gap で変えられる。
  L2 足元: キャラ (敵・リーダー・人形) の足元の点を、ほかの札 (帳面・自分の札・意図の札・上部バー・手札) が 16px 以上隠さない
     (足元より上に札の上端が 16px 以上入り込んでいる = 違反)。人形の足元の札は自分の人形の足元に付く物なので数えない。
  L3 意図の札: 上部バーに掛からない。ほかの敵の意図の札・帳面と重ならない。
  L4 画面の外: 帳面・意図の札・自分の札・上部バー・人形の札・本文の数字が画面からはみ出さない (1px の許容)。
     敵の帳面と意図の札は右端から 16px 以上内側 (seatfit の右端の条件と同じ)。キャラの絵が画面からはみ出すのは「注意」。
  L5 本文の数字: 手札の本文の数字の下端が、画面の下端から 36px 以上上にある (digitsBottomGap ≧ 36)。
  L6 人形の札: 人形の札どうしが重ならない。人形の札が敵の帳面・自分の札と重ならない。
  L7 帳面どうし: 敵の帳面どうしが重ならない。いちばん左の敵の帳面が自分の札 (hpwrap・からくり・ギア・置物の欄) に入らない。
  L8 額縁: extra.frames (額縁の画面の矩形。P10・P12 が HD2DFlags.LayoutDumpers["frames"] で足す = [{name, px}]) が
     UI の矩形と重なる割合が 30% 以下。記録が無ければ検査しない (注意に1行)。
  L9 板のずれ: stage.unitBoxes (P11 の Stage.DebugUnitBoxes) の各キャラの板と矩形のずれが、静止 1px・演出中 2px 以下。
     ずれは記録の dev (px) を使い、無ければ rectPx と boardPx の4辺の差の最大。記録が無ければ検査しない。
  L10 からくりの匣 (2026-10-01 二周目の直しの輪1): extra.karakuriBox (Stage.R2A_DumpKarakuriBox = 匣の板の画面の矩形) が、
     主人公の足元 (足元の x ±0.15×絵の幅・足元の 24px 上〜8px 下) と自分の札 (hpwrap・からくり・ギア・置物の欄) に掛からない。記録が無ければ検査しない。
     演出中のコマ (--moving・STATE に play= など) の足元は注意 (のけぞった主人公が奥の匣の手前を横切るだけ)。
  L8 の額縁から、深さ 50 以上の板 (カメラに付く背景の板 backdrop = 深さ 140・全幅) は外す (手前の額縁ではない。直しの輪1)。
  L11 三周目の部品 (2026-10-01 三周目 レーン F・分析 R2・R6・計画 §2 C の「⑨」): 設計図 (extra.diorama.layout の Resources/Stage/<名前>.json) の部品を
     この撮影のカメラ (stage.camera) で画面へ写し、三周目の部品を数える (霧の板 mist・針葉樹 conifer_w*・近い幹 conifer_near*・垂れる枝 bough_hang*・
     手前の草の額縁 fore_grass*・茂みの塊 bush_clump*。名前は取り決め 1 の絵の名前で見分ける。絵がまだ無ければ取り決めの大きさで箱を作る)。違反:
     ⑨ 垂れる枝・近い幹の箱が「幕ボスの頭と意図の札の真上」= x1200〜1550 (画面の幅で縮める)・行 0〜max(140, その列の意図の札の下端) の 1% 超を覆う、
        か意図の札に掛かる (分析 R2・place.py の R3_BOSS160 と同じ窓)。
     ⑩ 手前の草の額縁 (extra.frames の箱) が手札の札の矩形・名前の帯 (nodes の nameband。無ければ Presenter.NameBandPlace と同じ置き場の予想 =
        いちばん下の敵の足元の 6〜14 下から高さ 62 (スマホ 54)・幅いっぱい・足元の線の 4 上で止める) に掛かる (分析 R6「名前の帯と手札の矩形を避ける」)。
     箱は絵の外接矩形 (垂れる枝は斜めの絵なので箱は大きめ = 違反の時は確認の絵 --draw で見る)。設計図に三周目の部品が無ければ数えるだけ (0)。

三周目 r3 の割り付け (2026-10-02 本番・仕様 docs/design/hd2d-slice/r3-ui-spec.md §13。記録の flags が stage=diorama かつ uilayout=r3 (旧い記録は uitrial=1) の時。
  スマホの記録 (canvas.phone) は flags の uilayoutphone=r3 の時 (2026-10-02 からスマホの既定は r2。uilayoutphone の無い旧い記録は uilayout を読む)):
  手札は沈め、本文は触れて読む。そのため L4 の「本文の数字が画面の外」と L5 は、触れて上がった札 (札の上端が画面の高さの 75% より上) だけで測る。
  L5 は代わりに、休んでいる手札の「要の数字の札」(nodes の …/handN/keynum) の下端が画面の下端から 36px 以上・あとから描く札 (右の札) に 30% 以上隠れない。
  記録に要の数字の札が無ければ注意 (撮影の側で手札の子が記録に入っていない)。L2 で触れて上がった札が足元を隠すのは注意 (仕様 §9 の例外 = 触れている間だけ)。
  飛んでいる途中・つかんでいる札 (ドローで山札から飛ぶ・捨て札/からくり/敵へ飛ぶ・PC でつかんだ札) は L4・L5 で数えない (隠す側にも数えない)。
  見分けは記録の flying (2026-10-02 から LayoutDump が書く)、無い旧い記録は ①同じ名前の2つ目以降 ②裏 (…/handN/back) ③休んでいる札より小さい外接の箱 (hand_flying)。
  要の数字の札と触れた札は記録ごとに持つ (飛んでいく古い札も handN の名前を持つので、名前で引くと休んでいる札と取り違える。直しの輪1)。
  自分の欄に匣 (…/player/box と、その子 setzone・gearzone・perms) とスマホの上の帯の状態の札 (…/player/chips) を足す (L4・L6・L7・L10 が読む)。
  L12 匣 (PC): …/player/box が x ≤ 余白+152 (キャンバス 184)・上端が画面の上から 480 (キャンバス) より下・エナジーの輪 (energy)・灯籠 (light)・足元の帳 (hpwrap) と重ならない。
  L13 窓 (確認の窓 reaction・ギアの窓 gear-window・持ち物の一覧 gear-more-window): 主人公・人形の絵の箱を縁で切らない (重なるなら丸ごと覆う。体の箱の 8〜92% が窓の中 = 違反)・
      敵の意図の札に掛からない。PC は敵の帳面にも掛からない (スマホは幅の下限で他の敵の帳面に掛かってよい＝注意)。
  L14 草の帯 (PC): 画面の行 800〜950・x 512〜1460 (画面の大きさで縮める) に、窓・触れた札・選択式の窓・名前の帯 以外の UI (敵の帳面・意図の札・自分の欄・人形の札・上部バー) が無い。
"""
import argparse
import glob
import json
import math
import os
import re
import sys

LAY_EXT = '.layout.json'


def load(p):
    with open(p, encoding='utf-8') as f:
        return json.load(f)


def area(r):
    return max(0.0, r[2]) * max(0.0, r[3])


def inter(a, b):
    x0, y0 = max(a[0], b[0]), max(a[1], b[1])
    x1, y1 = min(a[0] + a[2], b[0] + b[2]), min(a[1] + a[3], b[1] + b[3])
    if x1 <= x0 or y1 <= y0:
        return None
    return [x0, y0, x1 - x0, y1 - y0]


def xover(a, b):
    return max(0.0, min(a[0] + a[2], b[0] + b[2]) - max(a[0], b[0]))


def units(lay):
    return [u for u in lay.get('units', []) if isinstance(u, dict)]


def nodes(lay, pattern):
    rx = re.compile(pattern)
    return [n for n in lay.get('nodes', []) if isinstance(n, dict) and rx.search(n.get('path', '')) and n.get('px')]


def feet_of(lay, u):
    sp = u.get('sprite')
    if not sp:
        return None
    x = sp[0] + sp[2] / 2.0
    y = sp[1] + sp[3]
    sl = lay.get('statusLineY') or {}
    fo = u.get('feetOffset')
    sc = (lay.get('canvas') or {}).get('scale') or 1.0
    if isinstance(fo, (int, float)) and not (isinstance(fo, float) and math.isnan(fo)) and isinstance(sl.get('px'), (int, float)):
        y = sl['px'] - fo * sc
    return (x, y)


def self_rects(lay):
    """自分の札の欄 (PC は帳面の左端・スマホは左下と上の帯。r3 の PC は足元の帳と匣 box の区画・r3 のスマホは上の帯の状態の札 chips も)"""
    return [(n['path'].split('/')[-1], n['px']) for n in nodes(lay, r'/player/(?:box/)?(hpwrap|setzone|gearzone|perms|box|chips)$')]


def is_r3(lay):
    """三周目 r3 の割り付けの撮影か (箱庭・旗 uilayout=r3。旗の無い旧い記録は uitrial=1)。
    スマホ (canvas.phone) は旗 uilayoutphone (2026-10-02 から。既定 r2 = 二周目のスマホ)。uilayoutphone の無い旧い記録のスマホは今までどおり uilayout を読む"""
    f = lay.get('flags') or {}
    if f.get('stage') != 'diorama':
        return False
    if (lay.get('canvas') or {}).get('phone') and f.get('uilayoutphone') is not None:
        return f.get('uilayoutphone') == 'r3'
    ul = f.get('uilayout')
    if ul is None:
        return bool(f.get('uitrial'))
    return ul == 'r3'


def card_poly(h, lay):
    """手札の札の外接の箱 (px) から、回った札の4隅を解く (札の大きさ 200×290×倍率は分かっているので、箱の幅と高さから傾きを出す)。解けなければ None"""
    sc = (lay.get('canvas') or {}).get('scale') or 1.0
    phone = (lay.get('canvas') or {}).get('phone')
    k = sc * (1.0 if phone else 0.92)
    w0, h0 = 200.0 * k, 290.0 * k
    x, y, W, H = h['px']
    if W < w0 * 0.75 or H < h0 * 0.75:
        return None
    key = (round(x, 1), round(y, 1), round(W, 1), round(H, 1), round(w0, 2))
    if key in _POLY:
        return _POLY[key]
    # 等倍以外 (触れて 1.18 倍) は箱の大きさで倍率を推す
    best = None
    for scale in (1.0, 1.18, 0.8):
        ww, hh = w0 * scale, h0 * scale
        # W = ww c + hh s'・H = ww s' + hh c (0 ≤ θ ≤ 30°)
        for t10 in range(0, 301):
            th = math.radians(t10 / 10.0)
            c_, s_ = math.cos(th), math.sin(th)
            e = abs(ww * c_ + hh * s_ - W) + abs(ww * s_ + hh * c_ - H)
            if best is None or e < best[0]:
                best = (e, scale, th)
    if best is None or best[0] > 6.0:
        _POLY[key] = None
        return None
    _, scale, th = best
    ww, hh = w0 * scale, h0 * scale
    cx, cy = x + W / 2.0, y + H / 2.0
    # 傾きの向き: 札の番号が扇の中央より左なら左回り (画面の y が下向きなので反時計) …向きは2通りとも返す
    out = []
    for sgn in (1.0, -1.0):
        a = sgn * th
        ca, sa = math.cos(a), math.sin(a)
        pts = []
        for dx, dy in ((-ww / 2, -hh / 2), (ww / 2, -hh / 2), (ww / 2, hh / 2), (-ww / 2, hh / 2)):
            pts.append((cx + dx * ca - dy * sa, cy + dx * sa + dy * ca))
        out.append(pts)
    _POLY[key] = out
    return out


_POLY = {}


def in_poly(pt, pts):
    x, y = pt
    inside = False
    n = len(pts)
    for i in range(n):
        x1, y1 = pts[i]
        x2, y2 = pts[(i + 1) % n]
        if (y1 > y) != (y2 > y) and x < (x2 - x1) * (y - y1) / ((y2 - y1) or 1e-9) + x1:
            inside = not inside
    return inside


def doll_tags(lay):
    out = []
    for n in nodes(lay, r'/(doll:[^/]+)/tag$'):
        m = re.search(r'/(doll:[^/]+)/tag$', n['path'])
        out.append((m.group(1), n['px']))
    return out


def hand_children(lay, hand):
    """手札の各記録 (lay['hand'] の1つ1つ) の子: 要の数字の札 (keynum) の矩形と、裏 (back) が見えているか。{id(記録): {'name','keynum','back'}}。
    nodes は画面の子の順 (深さ優先) で、手札の記録も同じ HandLayer の子の順に並ぶので、…/handN の出た順で突き合わせる
    (飛んでいく古い札は今の札と同じ名前 handN を持つ＝名前だけで引くと後ろの札で上書きされる。2026-10-02 三周目 直しの輪1)。
    数か名前の並びが合わなければ (nodes の上限で切れた等) 名前で引く (最初に出た物)"""
    occ = []
    for n in lay.get('nodes', []):
        p = n.get('path') or ''
        m = re.match(r'^screen/handlayer/(hand\d+)$', p)
        if m:
            occ.append({'name': m.group(1), 'keynum': None, 'back': False})
            continue
        m = re.match(r'^screen/handlayer/(hand\d+)/(keynum|back)$', p)
        if m and occ and occ[-1]['name'] == m.group(1):
            if m.group(2) == 'keynum':
                occ[-1]['keynum'] = n.get('px')
            else:
                occ[-1]['back'] = True
    named = [h for h in hand if re.match(r'^hand\d+$', h.get('name') or '')]
    out = {}
    if len(named) == len(occ) and all(h.get('name') == o['name'] for h, o in zip(named, occ)):
        for h, o in zip(named, occ):
            out[id(h)] = o
    else:
        first = {}
        for o in occ:
            first.setdefault(o['name'], o)
        for h in hand:
            o = first.get(h.get('name'))
            if o:
                out[id(h)] = o
    return out


def hand_flying(lay, hand, kids):
    """手札の各記録が扇に休んでいないか (ドローで山札から飛ぶ・捨て札/からくり/敵へ飛ぶ・PC でつかんだ札)。{id(記録): bool}。
    記録に flying があればそれ (Autopilot の LayoutDump が書く。2026-10-02 から)。無い旧い記録は推す:
    ① 同じ名前の2つ目以降 (手札から出た札は SetAsLastSibling で今の札の後ろに並ぶ) ② 裏 (…/handN/back) が見えている
    ③ 外接の箱が休んでいる札 (倍率 CardScale・傾き ±6° の箱は札の幅・高さ以上) の 95% より小さい (ドローの途中・めくりの途中・捨て札へ縮む途中)"""
    sc = (lay.get('canvas') or {}).get('scale') or 1.0
    phone = (lay.get('canvas') or {}).get('phone')
    k = sc * (1.0 if phone else 0.92)
    w0, h0 = 200.0 * k, 290.0 * k
    out, seen = {}, set()
    for h in hand:
        nm = h.get('name')
        dup = nm in seen
        seen.add(nm)
        if 'flying' in h:
            out[id(h)] = bool(h.get('flying'))
            continue
        _, _, W, H = h['px']
        small = W < 0.95 * w0 or H < 0.95 * h0
        out[id(h)] = bool(dup or (kids.get(id(h)) or {}).get('back') or small)
    return out


def moving_state(state):
    return bool(re.search(r'(^|;)(play|endplay|fire|entershots|usegear|fx)=', state or ''))


def check(lay, hand_gap_units=6.0, feet_hide=16.0, digits_min=36.0, right_margin=16.0, frame_max=0.30, box_tol=None):
    V, W_ = [], []   # 違反・注意 (dict: rule, msg, rects)
    sw = (lay.get('screen') or {}).get('w') or 1920
    sh = (lay.get('screen') or {}).get('h') or 1080
    sc = (lay.get('canvas') or {}).get('scale') or 1.0
    us = [u for u in units(lay) if u.get('alive', True) and u.get('active', True) is not False]
    enemies = [u for u in us if u.get('kind') == 'enemy']
    strips = [(u['key'], u['strip']) for u in enemies if u.get('strip')]
    intents = [(u['key'], u['intent']) for u in enemies if u.get('intent')]
    selfr = self_rects(lay)
    dtags = doll_tags(lay)
    hand = [h for h in lay.get('hand', []) if isinstance(h, dict) and h.get('px')]
    topbar = (lay.get('anchors') or {}).get('topbar')

    def v(rule, msg, *rects):
        V.append({'rule': rule, 'msg': msg, 'rects': [list(map(float, r)) for r in rects if r]})

    def w(rule, msg, *rects):
        W_.append({'rule': rule, 'msg': msg, 'rects': [list(map(float, r)) for r in rects if r]})

    # L1 帳面と手札
    gap_px = hand_gap_units * sc
    card_h = 290.0 * sc * (0.92 if not (lay.get('canvas') or {}).get('phone') else 1.0)
    for key, s in strips + [('player/' + n, r) for n, r in selfr if n == 'hpwrap']:
        bottom = s[1] + s[3]
        for h in hand:
            r = h['px']
            if xover(s, r) < 12:
                continue
            eff_top = r[1] + max(0.0, r[3] - card_h) / 2.0   # 傾いた札の外接の箱の膨らみの半分を除く = 札の中央の上端
            gap = eff_top - bottom
            if gap < gap_px - 0.5:
                v('L1', '%s の下端と手札 %s の上端の間 %.1fpx (< %.1fpx)' % (key, h.get('name'), gap, gap_px), s, r)
    r3 = is_r3(lay)
    # r3: 飛んでいる途中・つかんでいる札 (ドロー・捨て・プレイの演出の途中) は休んでいる手札として数えない (L4・L5)。
    # 触れて上がった札は記録ごと (id) で持つ (2026-10-02 三周目 直しの輪1: 旧は名前で持ったので、敵へ飛ぶ札 hand0 と同じ名前の休んでいる hand0 も「触れた札」として測っていた)
    kids = hand_children(lay, hand) if r3 else {}
    flying = hand_flying(lay, hand, kids) if r3 else {}
    raised = set(id(h) for h in hand if r3 and not flying.get(id(h)) and (h.get('raised') or h['px'][1] < 0.75 * sh))   # 触れて上がった札 (r3)
    # L2 足元
    covers = [('strip:' + k, r, None) for k, r in strips] + [('intent:' + k, r, None) for k, r in intents] + \
             [('self:' + n, r, None) for n, r in selfr] + [('hand:' + (h.get('name') or ''), h['px'], id(h)) for h in hand]
    if topbar:
        covers.append(('topbar', topbar, None))
    for u in us:
        f = feet_of(lay, u)
        if not f:
            continue
        fx, fy = f
        half = max(8.0, 0.25 * u['sprite'][2])
        probe = [fx - half, fy - 1, 2 * half, 2]
        for name, r, hid in covers:
            if name.endswith(':' + u.get('key', '')) and name.startswith('intent:'):
                continue
            if xover(probe, r) < 4:
                continue
            if r[1] < fy <= r[1] + r[3] + 0.5:
                hidden = fy - r[1]
                if hidden >= feet_hide:
                    if hid is not None and hid in raised:   # r3: 触れている間だけ (仕様 §9 の例外)
                        w('L2', '%s の足元 (%.0f,%.0f) を触れて上がった %s が %.0fpx 隠す (触れている間だけ・例外)' % (u.get('key'), fx, fy, name, hidden), r)
                        continue
                    if hid is not None and flying.get(hid):   # r3: 敵へ飛ぶ・山札から飛ぶ途中の札 (演出の途中。旧は名前で「触れた札」に数えて注意にしていた)
                        w('L2', '%s の足元 (%.0f,%.0f) を飛んでいる途中の %s が %.0fpx 隠す (演出の途中)' % (u.get('key'), fx, fy, name, hidden), r)
                        continue
                    v('L2', '%s の足元 (%.0f,%.0f) を %s が %.0fpx 隠す' % (u.get('key'), fx, fy, name, hidden), r, [fx - 3, fy - 3, 6, 6])
    # L3 意図の札
    for k, r in intents:
        if topbar and inter(r, topbar) and area(inter(r, topbar)) > 4:
            v('L3', '%s の意図の札が上部バーに掛かる' % k, r, topbar)
        for k2, r2 in intents:
            if k2 <= k:
                continue
            i = inter(r, r2)
            if i and area(i) > 4:
                v('L3', '%s と %s の意図の札が重なる (%.0fpx²)' % (k, k2, area(i)), r, r2)
        for k2, s2 in strips:
            if k2 == k:
                continue
            i = inter(r, s2)
            if i and area(i) > 4:
                v('L3', '%s の意図の札が %s の帳面と重なる' % (k, k2), r, s2)
    # L4 画面の外
    tol = 1.0
    boxes = [('strip:' + k, r, right_margin) for k, r in strips] + [('intent:' + k, r, right_margin) for k, r in intents] + \
            [('self:' + n, r, 0.0) for n, r in selfr] + [('dolltag:' + k, r, 0.0) for k, r in dtags]
    if topbar:
        boxes.append(('topbar', topbar, 0.0))
    for name, r, rm in boxes:
        if r[0] < -tol or r[1] < -tol or r[0] + r[2] > sw - rm + tol or r[1] + r[3] > sh + tol:
            v('L4', '%s が画面の外へ出る (%s・右の余白 %dpx)' % (name, [round(x) for x in r], rm), r)
    for u in us:
        sp = u.get('sprite')
        if sp and (sp[0] < -tol or sp[0] + sp[2] > sw + tol or sp[1] < -tol):
            w('L4', '%s の絵の矩形が画面からはみ出す (%s)' % (u.get('key'), [round(x) for x in sp]), sp)
    for h in hand:
        if r3 and id(h) not in raised:
            continue   # r3: 休んでいる手札の本文は画面の外 (本文は触れて読む)。飛んでいる途中の札も測らない
        for d in h.get('digits') or []:
            if d[0] < -tol or d[0] + d[2] > sw + tol or d[1] + d[3] > sh + tol:
                v('L4', '手札 %s の本文の数字が画面の外' % h.get('name'), d)
    # L5 本文の数字 (r3 は触れた札の本文と、休んでいる札の要の数字の札)
    for h in hand:
        if r3 and id(h) not in raised:
            continue
        g = h.get('digitsBottomGap')
        if isinstance(g, (int, float)) and g < digits_min:
            v('L5', '手札 %s の本文の数字が下端から %.0fpx (< %.0f)%s' % (h.get('name'), g, digits_min, '・触れた札' if r3 else ''), *(h.get('digits') or [h['px']]))
    if r3:
        # 休んでいる札だけ (触れて上がった札・飛んでいる途中の札は除く)。要の数字の札は記録ごと (同じ名前の飛んでいく札の物で上書きしない)
        rest = [h for h in hand if id(h) not in raised and not flying.get(id(h))]
        if rest and not any((kids.get(id(h)) or {}).get('keynum') for h in rest):
            w('L5', '要の数字の札 (…/handN/keynum) の記録が無い (量の無い札だけの手札か、記録に手札の子が入っていない)')
        order = lambda h: int(re.sub(r'\D', '', h.get('name') or '0') or 0)
        for h in rest:
            kr = (kids.get(id(h)) or {}).get('keynum')
            if not kr:
                continue
            gap = sh - (kr[1] + kr[3])
            if gap < digits_min:
                v('L5', '手札 %s の要の数字の札が下端から %.0fpx (< %.0f)' % (h.get('name'), gap, digits_min), kr)
            # あとから描く札 (番号が大きい札。触れて上がった札は最前) に隠れる割合: 要の数字の札の 5×3 の点のうち札の中に入る数
            pts = [(kr[0] + kr[2] * (i + 0.5) / 5.0, kr[1] + kr[3] * (j + 0.5) / 3.0) for i in range(5) for j in range(3)]
            hid = 0
            for p_ in pts:
                for h2 in hand:
                    if h2 is h or flying.get(id(h2)) or (order(h2) <= order(h) and id(h2) not in raised):
                        continue
                    polys = card_poly(h2, lay)
                    if polys is None:
                        if h2['px'][0] <= p_[0] <= h2['px'][0] + h2['px'][2] and h2['px'][1] <= p_[1] <= h2['px'][1] + h2['px'][3]:
                            hid += 1
                            break
                        continue
                    # 傾きの向きは分からないので、両方の向きの交わり (どちらの向きでも札の中) を隠れたと見る
                    if all(in_poly(p_, pp) for pp in polys):
                        hid += 1
                        break
            if hid / 15.0 > 0.30 and id(h) not in raised:
                v('L5', '手札 %s の要の数字の札が右の札に %.0f%% 隠れる' % (h.get('name'), hid / 15.0 * 100), kr)
    # L6 人形の札
    for i, (k, r) in enumerate(dtags):
        for k2, r2 in dtags[i + 1:]:
            it = inter(r, r2)
            if it and area(it) > 4:
                v('L6', '人形の札 %s と %s が重なる' % (k, k2), r, r2)
        for name, r2 in [('strip:' + a, b) for a, b in strips] + [('self:' + a, b) for a, b in selfr]:
            it = inter(r, r2)
            if it and area(it) > 4:
                v('L6', '人形の札 %s が %s と重なる' % (k, name), r, r2)
    # L7 帳面どうし・自分の札
    for i, (k, r) in enumerate(strips):
        for k2, r2 in strips[i + 1:]:
            it = inter(r, r2)
            if it and area(it) > 4:
                v('L7', '%s と %s の帳面が重なる' % (k, k2), r, r2)
    if strips:
        k0, r0 = min(strips, key=lambda kr: kr[1][0])
        for n, r in selfr:
            it = inter(r0, r)
            if it and area(it) > 4:
                v('L7', 'いちばん左の敵 %s の帳面が自分の札 (%s) に入る' % (k0, n), r0, r)
    if r3:
        anchors = lay.get('anchors') or {}
        # L12 匣 (PC)
        box = [r for n_, r in selfr if n_ == 'box']
        if box and not (lay.get('canvas') or {}).get('phone'):
            b = box[0]
            if b[0] + b[2] > (32.0 + 152.0) * sc + 1.0:
                v('L12', '匣の右端 %.0f が余白+152 (%.0f) を越える' % (b[0] + b[2], 184.0 * sc), b)
            if b[1] < 480.0 * sc - 1.0:
                v('L12', '匣の上端 (行 %.0f) が 480 より上' % b[1], b)
            for nm in ('energy', 'light'):
                ar = anchors.get(nm)
                it = inter(b, ar) if ar else None
                if it and area(it) > 4:
                    v('L12', '匣が %s と重なる' % nm, b, ar)
            for n_, r in selfr:
                if n_ == 'hpwrap':
                    it = inter(b, r)
                    if it and area(it) > 4:
                        v('L12', '匣が足元の帳 (hpwrap) と重なる', b, r)
        # L13 窓
        wins = [(n['path'].split('/')[-1], n['px']) for n in nodes(lay, r'/(reaction|gear-window|gear-more-window)$')]
        bodies = [(u.get('key'), u['sprite']) for u in us if u.get('kind') in ('player', 'doll') and u.get('sprite')]
        for wn, wr in wins:
            for key, sp in bodies:
                it = inter(wr, sp)
                if not it:
                    continue
                fr = area(it) / max(1.0, area(sp))
                if 0.08 < fr < 0.92:
                    v('L13', '窓 %s が %s の絵の箱を縁で切る (%.0f%% が窓の中)' % (wn, key, fr * 100), wr, sp)
            for k, r in intents:
                it = inter(wr, r)
                if it and area(it) > 4:
                    v('L13', '窓 %s が %s の意図の札に掛かる' % (wn, k), wr, r)
            for k, r in strips:
                it = inter(wr, r)
                if it and area(it) > 4:
                    (w if (lay.get('canvas') or {}).get('phone') else v)('L13', '窓 %s が %s の帳面に掛かる' % (wn, k), wr, r)
        # L14 草の帯 (PC)
        if not (lay.get('canvas') or {}).get('phone'):
            gb = [512.0 * sw / 1920.0, 800.0 * sh / 1080.0, (1460.0 - 512.0) * sw / 1920.0, 150.0 * sh / 1080.0]
            items = [('strip:' + k, r) for k, r in strips] + [('intent:' + k, r) for k, r in intents] + \
                    [('self:' + n_, r) for n_, r in selfr] + [('dolltag:' + k, r) for k, r in dtags]
            if topbar:
                items.append(('topbar', topbar))
            for name, r in items:
                it = inter(gb, r)
                if it and area(it) > 4:
                    v('L14', '%s が草の帯 (行 800〜950・x 512〜1460) に入る (%.0fpx²)' % (name, area(it)), r, gb)
    # L8 額縁
    frames = (lay.get('extra') or {}).get('frames')
    if isinstance(frames, list) and frames:
        ui_rects = [r for _, r in strips] + [r for _, r in intents] + [r for _, r in selfr] + [r for _, r in dtags] + [h['px'] for h in hand]
        if topbar:
            ui_rects.append(topbar)
        for fr in frames:
            r = fr.get('px') if isinstance(fr, dict) else fr
            if not r or area(r) <= 0:
                continue
            if isinstance(fr, dict) and isinstance(fr.get('depth'), (int, float)) and fr['depth'] >= 50:
                continue   # 背景の板 (深さ 140) は額縁ではない (直しの輪1)
            cov = union_area_within(r, ui_rects) / area(r)
            if cov > frame_max:
                v('L8', '額縁 %s が UI と %.0f%% 重なる (> %.0f%%)' % (fr.get('name') if isinstance(fr, dict) else '?', cov * 100, frame_max * 100), r)
    else:
        w('L8', '額縁の記録 (extra.frames) が無いので検査しない')
    # L9 板のずれ
    ub = (lay.get('stage') or {}).get('unitBoxes')
    tolb = box_tol if box_tol is not None else (2.0 if moving_state(lay.get('state')) else 1.0)
    entries = []
    if isinstance(ub, list):
        entries = [e for e in ub if isinstance(e, dict)]
    elif isinstance(ub, dict):
        entries = [dict(e, key=k) for k, e in ub.items() if isinstance(e, dict)]
    if entries:
        for e in entries:
            dev = e.get('dev')
            if dev is None:
                a, b = e.get('rectPx') or e.get('rect'), e.get('boardPx') or e.get('board')
                if a and b and len(a) >= 4 and len(b) >= 4:
                    dev = max(abs(a[0] - b[0]), abs(a[1] - b[1]), abs(a[0] + a[2] - b[0] - b[2]), abs(a[1] + a[3] - b[1] - b[3]))
            if isinstance(dev, (int, float)) and dev > tolb:
                v('L9', '%s の板と矩形のずれ %.1fpx (> %.0fpx)' % (e.get('key'), dev, tolb), e.get('boardPx') or e.get('board') or [0, 0, 0, 0])
    elif ub is None:
        w('L9', '板の記録 (stage.unitBoxes) が無いので検査しない')
    # L10 からくりの匣 (直しの輪1)
    kb = (lay.get('extra') or {}).get('karakuriBox')
    if isinstance(kb, dict) and kb.get('px'):
        rb = kb['px']
        for u in us:
            if u.get('kind') != 'player' or not u.get('sprite'):
                continue
            f = feet_of(lay, u)
            if not f:
                continue
            fx, fy = f
            half = max(8.0, 0.15 * u['sprite'][2])   # 足の幅 (絵の矩形は斧・竿を含むので 0.25 では広すぎる)
            probe = [fx - half, fy - 24, 2 * half, 32]
            it = inter(rb, probe)
            if it and area(it) > 4:
                # 演出中 (被弾でのけぞる・盾で下がる) は主人公が匣の手前を横切る = 奥の匣を隠すだけなので注意に留める
                (w if moving_state(lay.get('state')) else v)('L10', 'からくりの匣 (%s) が主人公の足元 (%.0f,%.0f) に掛かる (%.0fpx²)' % (kb.get('place'), fx, fy, area(it)), rb, probe)
        for n, r2 in selfr:
            it = inter(rb, r2)
            if it and area(it) > 4:
                v('L10', 'からくりの匣 (%s) が自分の札 (%s) に掛かる (%.0fpx²)' % (kb.get('place'), n, area(it)), rb, r2)
    # L11 三周目の部品 (⑨ 幕ボスの頭と意図の札の真上・⑩ 手前の草と手札・名前の帯)
    r3 = r3_parts(lay)
    lay['_r3'] = r3   # main が表に数を書く
    if r3.get('error'):
        w('L11', '三周目の部品を写せない (%s)' % r3['error'])
    else:
        zx0, zx1 = 1200.0 * sw / 1920.0, 1550.0 * sw / 1920.0
        zb = 140.0 * sh / 1080.0
        for k, r in intents:
            cx = r[0] + r[2] / 2.0
            if zx0 <= cx <= zx1:
                zb = max(zb, r[1] + r[3])
        zone = [zx0, 0.0, zx1 - zx0, zb]
        for o in r3['parts']:
            if o['cat'] not in ('bough', 'near') or not o.get('box'):
                continue
            b = o['box']
            it = inter(b, zone)
            if it and area(it) > 0.01 * area(zone):
                v('L11', '⑨ %s (%s) の箱が幕ボスの頭と意図の札の真上 (x%.0f〜%.0f・行 0〜%.0f) の %.0f%% を覆う' % (o['name'], o['cat'], zx0, zx1, zb, area(it) / area(zone) * 100), b, zone)
            for k, r in intents:
                it2 = inter(b, r)
                if it2 and area(it2) > 4:
                    v('L11', '⑨ %s (%s) の箱が %s の意図の札に掛かる' % (o['name'], o['cat'], k), b, r)
        band = name_band(lay, enemies, sh, sw)
        for o in r3['parts']:
            if o['cat'] != 'fore_grass' or not o.get('box'):
                continue
            b = o['box']
            for h in hand:
                it = inter(b, h['px'])
                if it and area(it) > 4:
                    v('L11', '⑩ 手前の草 %s が手札 %s の矩形に掛かる (%.0fpx²)' % (o['name'], h.get('name'), area(it)), b, h['px'])
            if band:
                it = inter(b, band['px'])
                if it and area(it) > 4:
                    v('L11', '⑩ 手前の草 %s が名前の帯 (%s) に掛かる (%.0fpx²)' % (o['name'], band['src'], area(it)), b, band['px'])
    return V, W_


# ---- L11 三周目の部品を画面へ写す (place.py の式の写し。2026-10-01 三周目 レーン F)

R3_RES = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), 'unity', 'Assets', 'Resources')
R3_TPU = 25.0
R3_CATS = (('conifer_near', 'near'), ('conifer_w', 'conifer'), ('bough_hang', 'bough'), ('fore_grass', 'fore_grass'), ('bush_clump', 'bush'))
R3_CAT_JA = {'mist': '霧の板', 'conifer': '針葉樹', 'near': '近い幹', 'bough': '垂れる枝', 'fore_grass': '手前の草', 'bush': '茂みの塊'}
_R3_LAYOUTS = {}
_R3_ART = {}
R3_DESIGN = None   # --design で設計図を差し替える (レーン C の書きかけ・試しの設計図を、撮影の記録のカメラで先に見る)


def r3_contract_size(art):
    """取り決め 1 の絵の大きさ (ドット)。絵がまだ無い時の箱"""
    b = os.path.basename(art)
    m = re.match(r'conifer_w(\d+)_', b)
    if m:
        return int(round(int(m.group(1)) * 4.25)), 280
    m = re.match(r'conifer_near_(\d+)', b)
    if m:
        return int(round((36 if m.group(1) == '1' else 42) * 4.25)), 320
    if b.startswith('bough_hang'):
        return 160, 80
    m = re.match(r'bush_clump_([lms])_', b)
    if m:
        return {'l': (96, 64), 'm': (64, 48), 's': (48, 32)}[m.group(1)]
    return None


def r3_art_size(L, src):
    """sources の art のうち先に見つかった絵の alpha>16 の外接矩形の大きさ (ドット)。無ければ取り決めの大きさ。(w, h, art, 実物か)"""
    if src in _R3_ART:
        return _R3_ART[src]
    res = None
    sd = (L.get('sources') or {}).get(src) or {}
    arts = sd.get('art') or []
    arts = arts if isinstance(arts, list) else [arts]
    for a in arts:
        p = os.path.join(R3_RES, a + '.png')
        if os.path.exists(p):
            try:
                from PIL import Image
                import numpy as np
                al = np.asarray(Image.open(p).convert('RGBA'))[..., 3]
                ys, xs = np.nonzero(al > 16)
                if len(xs):
                    res = (int(xs.max() - xs.min() + 1), int(ys.max() - ys.min() + 1), a, True)
                    break
            except Exception:
                pass
    if res is None:
        for a in arts:
            cs = r3_contract_size(a)
            if cs:
                res = (cs[0], cs[1], a, False)
                break
    if res is None and arts:
        res = (None, None, arts[0], False)
    _R3_ART[src] = res
    return res


def r3_cat(kind, art):
    if kind == 'mist':
        return 'mist'
    b = os.path.basename(art or '')
    for key, cat in R3_CATS:
        if b.startswith(key):
            return cat
    return None


def r3_cam(c):
    ex, ey, ez = [math.radians(v) for v in (c.get('renderEuler') or c.get('layoutEuler') or [c.get('pitch', 5), 0, 0])]
    cx, sx, cy, sy, cz, sz = math.cos(ex), math.sin(ex), math.cos(ey), math.sin(ey), math.cos(ez), math.sin(ez)
    Rx = [[1, 0, 0], [0, cx, -sx], [0, sx, cx]]
    Ry = [[cy, 0, sy], [0, 1, 0], [-sy, 0, cy]]
    Rz = [[cz, -sz, 0], [sz, cz, 0], [0, 0, 1]]

    def mm(A, B):
        return [[sum(A[i][k] * B[k][j] for k in range(3)) for j in range(3)] for i in range(3)]
    R = mm(mm(Ry, Rx), Rz)
    col = lambda j: [R[0][j], R[1][j], R[2][j]]
    r_, u_, f_ = col(0), col(1), col(2)
    P = c.get('renderPos') or c.get('camBase')
    W, H = c.get('screen') or [1920, 1080]
    th = math.tan(math.radians(c['fov']) / 2)

    def proj(X):
        v = [X[0] - P[0], X[1] - P[1], X[2] - P[2]]
        d = sum(a * b for a, b in zip(v, f_))
        if d <= 0.05:
            return None
        x = (sum(a * b for a, b in zip(v, r_)) / (d * th * W / H) + 1) / 2 * W
        y = (1 - sum(a * b for a, b in zip(v, u_)) / (d * th)) / 2 * H
        return x, y, d
    return proj


def r3_on_path(yaw_deg, t, s, y):
    a = math.radians(yaw_deg)
    return [t * math.cos(a) + s * math.sin(a), y, -t * math.sin(a) + s * math.cos(a)]


def r3_ground(L):
    """Diorama.HeightAtPath (段 slab のうち (t,s) を含む物の天面の最大。無ければいちばん低い天面)"""
    slabs = []
    for p in L.get('parts', []):
        if p.get('kind') != 'slab' or not p.get('front'):
            continue
        fr = sorted(p['front'], key=lambda q: q[0])
        slabs.append((fr, p.get('back', 10.0), p.get('top', 0.0)))
    low = min([s[2] for s in slabs] or [0.0])

    def height(t, s):
        best = None
        for fr, back, top in slabs:
            if t < fr[0][0] or t > fr[-1][0]:
                continue
            fa = fr[-1][1]
            for (t0, s0), (t1, s1) in zip(fr, fr[1:]):
                if t0 <= t <= t1:
                    fa = s0 + (s1 - s0) * ((t - t0) / (t1 - t0) if t1 > t0 else 0.0)
                    break
            if fa - 1e-4 <= s <= back and (best is None or top > best):
                best = top
        return low if best is None else best
    return height


def r3_parts(lay):
    """三周目の部品の画面の箱 (dict: parts=[{name, cat, box, depth}], counts={cat: [設計図の数, 画面の数]})"""
    out = dict(parts=[], counts={})
    dio = ((lay.get('extra') or {}).get('diorama') or {})
    name = dio.get('layout')
    cam = (lay.get('stage') or {}).get('camera') or {}
    if not name or cam.get('mode') != 'diorama' or cam.get('fov') is None:
        return out   # 今の舞台・記録の無い撮影 = 数えない
    path = R3_DESIGN or os.path.join(R3_RES, name + '.json')
    if not os.path.exists(path):
        out['error'] = '設計図が無い: %s' % path
        return out
    if path not in _R3_LAYOUTS:
        try:
            _R3_LAYOUTS[path] = load(path)
        except Exception as ex:
            out['error'] = '設計図が読めない: %s' % ex
            return out
    L = _R3_LAYOUTS[path]
    phone = bool((lay.get('canvas') or {}).get('phone'))
    sw, sh = (cam.get('screen') or [1920, 1080])
    proj = r3_cam(cam)
    yaw = L.get('pathYaw', -22.0)
    height = r3_ground(L)
    frames_dump = (lay.get('extra') or {}).get('frames') or []
    built_frames = []
    for i, p in enumerate(L.get('parts', [])):
        k = p.get('kind')
        ph = p.get('phone') if (phone and isinstance(p.get('phone'), dict)) else None
        if k == 'frame':
            if ph and ph.get('hide'):
                continue
            built_frames.append(p)
            continue
        src = p.get('relief') if k == 'tree' else p.get('src')
        art = None
        if k != 'mist':
            sz = r3_art_size(L, src) if src else None
            art = sz[2] if sz else None
        cat = r3_cat(k, art)
        if cat is None:
            continue
        c = out['counts'].setdefault(cat, [0, 0])
        c[0] += 1
        if ph and ph.get('hide'):
            continue
        q = dict(p)
        if ph and k != 'slab':
            for kk in ('t', 's', 'y', 'scale'):
                if kk in ph:
                    q[kk] = ph[kk]
        t, s = q.get('t', 0.0), q.get('s', 0.0)
        y0 = q['y'] if q.get('abs') else height(t, s) + q.get('y', 0.0)
        pos = r3_on_path(yaw, t, s, y0)
        scale = q.get('scale', 1.0) or 1.0
        if k == 'tree':
            scale *= q.get('reliefScale', 1.0)
        if k == 'mist':
            w, h = q.get('w', 8.0), q.get('h', 2.0)
        else:
            if not sz or sz[0] is None:
                continue
            w, h = sz[0] / R3_TPU * scale, sz[1] / R3_TPU * scale
        a = math.radians(q.get('yaw', 0.0))
        pts = []
        for lx, ly in ((-w / 2, 0), (-w / 2, h), (w / 2, h), (w / 2, 0)):
            X = [pos[0] + lx * math.cos(a), pos[1] + ly, pos[2] - lx * math.sin(a)]
            pr = proj(X)
            if pr:
                pts.append(pr)
        if len(pts) < 2:
            continue
        xs = [pp[0] for pp in pts]; ys = [pp[1] for pp in pts]
        box = [min(xs), min(ys), max(xs) - min(xs), max(ys) - min(ys)]
        on = box[0] < sw and box[0] + box[2] > 0 and box[1] < sh and box[1] + box[3] > 0
        if on:
            c[1] += 1
            out['parts'].append(dict(name=q.get('name') or '%s#%d' % (k, i), cat=cat, box=box, depth=round(sum(pp[2] for pp in pts) / len(pts), 2)))
    # 額縁 (extra.frames の箱 = Unity が置いた画面の矩形)。frame-<n> は組んだ順 (スマホで隠す物は飛ばす)
    for j, fd in enumerate(frames_dump):
        p = built_frames[j] if j < len(built_frames) and len(built_frames) == len(frames_dump) else None
        if p is None:
            dd = fd.get('depth') if isinstance(fd, dict) else None
            cand = [bf for bf in built_frames if isinstance(dd, (int, float))]
            p = min(cand, key=lambda bf: abs(float(bf.get('depth', 8.0)) - dd)) if cand else None
        if p is None:
            continue
        sz = r3_art_size(L, p.get('src')) if p.get('src') else None
        cat = r3_cat('frame', sz[2] if sz else None)
        if cat is None:
            continue
        c = out['counts'].setdefault(cat, [0, 0])
        c[1] += 1
        out['parts'].append(dict(name=p.get('name') or fd.get('name'), cat=cat, box=list(fd['px']), depth=fd.get('depth')))
    for p in L.get('parts', []):
        if p.get('kind') == 'frame':
            sz = r3_art_size(L, p.get('src')) if p.get('src') else None
            cat = r3_cat('frame', sz[2] if sz else None)
            if cat:
                out['counts'].setdefault(cat, [0, 0])[0] += 1
    return out


def name_band(lay, enemies, sh, sw):
    """名前の帯の矩形: 記録 (nodes の …/nameband) があればそれ。無ければ Presenter.NameBandPlace の予想 (帯の上端 = いちばん下の敵の足元の
    6〜14 下・帳面との間の余りの半分・高さ PC 62・スマホ 54。キャンバスの単位 × scale。下端は足元の線の 4 上まで)"""
    for n in nodes(lay, r'/nameband$'):
        return dict(px=n['px'], src='記録')
    feet = [feet_of(lay, u) for u in enemies]
    feet = [f for f in feet if f]
    if not feet:
        return None
    sc = (lay.get('canvas') or {}).get('scale') or 1.0
    phone = bool((lay.get('canvas') or {}).get('phone'))
    h = (54.0 if phone else 62.0) * sc
    fy = max(f[1] for f in feet)   # いちばん下の (画面で低い) 足元 = NameBandPlace の feet (上向きの座標の最小)
    tops = [u['strip'][1] for u in enemies if u.get('strip')]
    space = (min(tops) - fy) if tops else 1000.0
    below = max((4.0 if phone else 6.0) * sc, min(14.0 * sc, (space - h) / 2.0))
    top = fy + below
    sl = (lay.get('statusLineY') or {}).get('px')
    if isinstance(sl, (int, float)) and top + h > sl - 4.0 * sc:   # 足元の線 (手札のすぐ上) より下へは出さない
        top = sl - 4.0 * sc - h
    return dict(px=[0.0, top, float(sw), h], src='予想')


def union_area_within(r, others, step=4):
    """r の中で others のどれかに覆われる面積 (格子で数える)"""
    x0, y0, w, h = r
    n = 0
    tot = 0
    y = y0 + step / 2
    while y < y0 + h:
        x = x0 + step / 2
        while x < x0 + w:
            tot += 1
            for o in others:
                if o[0] <= x < o[0] + o[2] and o[1] <= y < o[1] + o[3]:
                    n += 1
                    break
            x += step
        y += step
    return area(r) * (n / tot) if tot else 0.0


def draw(png, lay, V, out):
    from PIL import Image, ImageDraw
    im = Image.open(png).convert('RGB')
    d = ImageDraw.Draw(im)
    for u in units(lay):
        for k, col in (('sprite', (80, 200, 255)), ('strip', (255, 220, 80)), ('intent', (255, 140, 60))):
            r = u.get(k)
            if r:
                d.rectangle([r[0], r[1], r[0] + r[2], r[1] + r[3]], outline=col, width=1)
        f = feet_of(lay, u)
        if f:
            d.ellipse([f[0] - 4, f[1] - 4, f[0] + 4, f[1] + 4], outline=(0, 255, 120), width=2)
    for h in lay.get('hand', []):
        r = h.get('px')
        if r:
            d.rectangle([r[0], r[1], r[0] + r[2], r[1] + r[3]], outline=(180, 180, 180), width=1)
    for e in V:
        for r in e['rects']:
            d.rectangle([r[0], r[1], r[0] + r[2], r[1] + r[3]], outline=(255, 0, 0), width=3)
    im.save(out)


def gather(paths):
    out = []
    for p in paths:
        if os.path.isdir(p):
            out += sorted(glob.glob(os.path.join(p, '*' + LAY_EXT)))
        elif p.endswith(LAY_EXT):
            out.append(p)
    return out


def main():
    ap = argparse.ArgumentParser(description='HD-2D 見本の配置の検査 (layout.json)')
    ap.add_argument('paths', nargs='+')
    ap.add_argument('--md')
    ap.add_argument('--json')
    ap.add_argument('--draw', help='確認の絵の置き場')
    ap.add_argument('--moving', action='store_true', help='板のずれの許容を 2px に')
    ap.add_argument('--hand-gap', type=float, default=6.0, help='L1 の間隔 (キャンバス単位。既定 6)')
    ap.add_argument('--all-variants', action='store_true', help='uionly・unitsonly・hideui の撮影の記録も読む (既定は通常の画の記録だけ)')
    ap.add_argument('--design', help='L11 で写す設計図 (既定は記録の extra.diorama.layout = Resources/Stage/<名前>.json)')
    a = ap.parse_args()
    global R3_DESIGN
    R3_DESIGN = a.design
    files = gather(a.paths)
    if not a.all_variants:
        files = [f for f in files if not re.search(r'-(uionly|unitsonly|hideui)-\d+\.layout\.json$', f)]
    if not files:
        print('layout.json が無い (dumplayout=1 で撮る)', file=sys.stderr)
        return 2
    res = {}
    total = 0
    for f in files:
        try:
            lay = load(f)
        except Exception as ex:
            res[os.path.basename(f)] = {'error': str(ex)}
            total += 1
            continue
        V, Wn = check(lay, hand_gap_units=a.hand_gap, box_tol=2.0 if a.moving else None)
        res[os.path.basename(f)] = {'violations': V, 'warnings': Wn, 'errors': lay.get('errors') or [],
                                    'r3counts': (lay.get('_r3') or {}).get('counts') or {}}
        total += len(V)
        if a.draw:
            png = f[:-len(LAY_EXT)] + '.png'
            if os.path.exists(png):
                os.makedirs(a.draw, exist_ok=True)
                draw(png, lay, V, os.path.join(a.draw, os.path.basename(png)[:-4] + '-check.png'))
    lines = ['# 配置の検査 (hd2d-layout-check)', '', '規則は scripts/hd2d-layout-check.py の先頭。違反 %d 件 (%d 枚)。' % (total, len(files)), '',
             '三周目の部品 (L11) = 画面に写る数 / 設計図の数: ' + '・'.join(R3_CAT_JA.values()) + ' の順。', '',
             '| 記録 | 違反 | 注意 | 記録の失敗 | 三周目の部品 |', '|---|---|---|---|---|']
    for k, r in res.items():
        if 'error' in r:
            lines.append('| %s | 読めない | %s | | |' % (k, r['error']))
            continue
        cnt = r.get('r3counts') or {}
        r3s = '・'.join('%d/%d' % tuple(cnt.get(c, [0, 0])[::-1]) for c in R3_CAT_JA) if cnt else '—'
        lines.append('| %s | %d | %d | %d | %s |' % (k, len(r['violations']), len(r['warnings']), len(r['errors']), r3s))
    for k, r in res.items():
        if r.get('violations') or r.get('errors'):
            lines += ['', '## ' + k]
            for e in r.get('violations', []):
                lines.append('- **%s** %s' % (e['rule'], e['msg']))
            for e in r.get('errors', []):
                lines.append('- 記録の失敗: %s' % e)
    md = '\n'.join(lines) + '\n'
    if a.md:
        with open(a.md, 'w', encoding='utf-8') as f:
            f.write(md)
    if a.json:
        with open(a.json, 'w', encoding='utf-8') as f:
            json.dump(res, f, ensure_ascii=False, indent=1)
    print(md)
    return 1 if total else 0


if __name__ == '__main__':
    sys.exit(main())

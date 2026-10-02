#!/usr/bin/env python3
"""HD-2D 段2 レーン C: 幕2 の設計図 (act2_layout*.json) の配置の規則を数える (計画 docs/design/hd2d-stage2-plan-2026-10-02.md §2 C・
約束 docs/design/hd2d-stage2/contracts.md C3)。

カメラ・地面・部品の写し方は幕1 の docs/design/hd2d-slice/r2-layout-gen/place.py をそのまま import する (place.py は直さない)。
幕2 で足した kind (rail・halo・arch・pillar) と、幕2 の座席・UI の矩形・規則はこのファイルに書く。

カメラ (三周目の既定 = r3 の UI。StageCamera.LayoutCamera の写し)
  PC   1920×1080・画角 22°・見下ろし 5°・足元の線 0.36
  PH   1920×886 (S25 相当・UI 1.6 倍)・22°・7°・0.525
  PC21 2560×1080・22°・5°・0.36 (座席と座席の帯だけ。UI の矩形は持たない)
座席 (StageSeats.cs の見本の表・画角 22° の行。幕を見ない = 幕1 と同じ足元)
  主人公 (−5, 0.9)・敵 n=1〜4 (t の表・s は偶数 −0.5／奇数 PC 2体 0.7・それ以外 0.1)・人形 9 (前列 t −3.9 + j×刻み・後列 +0.45/+2.0〔スマホ +0.5/+2.8〕)
  絵の枠: 敵 64 (PC 256px)・強個体 80 (320)・2体の幕ボス巨蟹 96 (384)・1体の幕ボス 128 (512。スマホは _96 の絵 302px)・主人公 62 ドット (248px)・人形 32 (128px)

規則 (違反の数を出す。0 にする)
  R1 座席の帯 (t −8.5〜13・s −2.6〜2.8): 段の高さ 0 (|y| < 0.01)・部品の足跡が入らない (足跡の矩形で厳密に。小札・段・霧・光・額縁・暈は数えない)。
     あわせて Diorama.Check と同じ「届き」の数え方 (abs の部品は数えない・PlaceOf はスマホの上書き) でも 0
  R2 足元の通りの真後ろ: 座席ごとの帯 (x = 足元 ±幅〔敵 140・主人公 60・人形 50。スマホは絵の比 0.79 倍〕・行 = 頭〔意図の札の上端か絵の上端〕〜足元 +20)
     に「背の高い物」の見えている画素が 20px² を超えて入らない。背の高い物 = 地面に立つ高さ 1.2 unit 以上の部品と、地面から浮いた部品
     (歩廊・梁・吊り物・坑口・提灯)。段・壁 (名前 wall-)・棚の縁 (ledge-)・一段目の土留め (edge-・stake-)・段をつなぐ階段 (steps-)・レール・小札・霧・光・暈・額縁は数えない。
     高さ 1.2 未満の地面の小物は「低い物」として数だけ出す
  R3 意図の札の箱 (基準の撮影 s2-base/s2-slice の PC/PH-S-*-1.layout.json の enemy.intent＋2 体と強個体の見積もり) と
     幕ボス 160 の頭 (PC x 1200〜1550・行 0〜140) に部品を掛けない (壁・段・階段・レール・霧・背景の板は除く。暈と光の筋は「光」として別に数える)
  R4 小札 (litter・card) は足元の通り (足元 ±幅・足元の 8 上〜20 下) に入らない (幕1 の R3_CORR と同じ形・背丈を問わない)
  R5 数: 部品 (小札・段を含む Diorama の数え方)・kind ごと・小札・材質 (surfaces の使われる物＋霧の板の材質) を設計図の gates と比べる
  R6 立ち方 (板 = relief・card。PC とスマホの置き場の両方): 下端と「支え」の差が ±0.04 unit を超えない。支え = 板の下端の線 (板の幅を 9 点) の下にある
     段 slab の高さと、地面の block (名前 wall-・steps- = 岩棚・台・本体・上段・頂・奥・階段) のうち下端が板の下端以下の物の天面の、いちばん高い所。
     支えより低い = 沈み (床や棚に刺さる)・高い = 浮き・点ごとの支えのいちばん低い所より 0.04 を超えて高い = 片浮き (岩棚の区切りの段差を跨ぐ)。
     吊る物 HUNG_SRC (提灯・鍾乳石・垂れる根) は浮きと片浮きを数えない (沈みは数える)
  R7 貫き (板): 板の高さの範囲 (下端〜上端) を block が通る。支えにならない block (柱・脚・歩廊の床・梁) が板の下端より下から上へ抜ける物と、
     どの block でも下端が板の下端より上にあって板の高さに掛かる物 (上段の張り出し・歩廊の床) を数える。自分の鉤 (名前 <板の名前>-…) と坑口の枠 (mouth-) は除く

基準の撮影の UI の矩形 (SHOTS の PC/PH-S-<場面>-1.layout.json) が 1 つでも無ければ、名前を標準エラーに出して終了コード 1 で止まる
(無いまま数えると R3 と UI の帯の判定が空の矩形で通って「違反 0」と出る)。--allow-missing-ui を付けた時だけ、警告を出して続ける。

使い方
  place2.py <layout.json> [<layout.json> ...] [--out-dir DIR] [--img] [--md PATH] [--allow-missing-ui]
出力
  <out-dir>/place2-<名前>.json (表)・--img なら <out-dir>/place2-<名前>-<PC|PH|PC21>.png (光なしの構図の画に座席の帯・意図の札・規則の箱を重ねた物)
  --md を書けば全部の設計図の要約を 1 枚の md に
"""
import argparse
import json
import math
import os
import re
import sys

import numpy as np
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, '..', '..', '..', '..'))
sys.path.insert(0, os.path.join(REPO, 'docs', 'design', 'hd2d-slice', 'r2-layout-gen'))
import place  # noqa: E402
from place import Cam, Ground, TPU, yaw_rot  # noqa: E402

LANE_DIR = os.path.expanduser('~/.cache/deck-rogue/hd2d-stage2/lanes/C')
SHOTS = os.path.expanduser('~/.cache/deck-rogue/hd2d-stage2/shots/s2-base/s2-slice')

SEAT_T0, SEAT_T1, SEAT_S0, SEAT_S1 = -8.5, 13.0, -2.6, 2.8   # Diorama.SeatT0〜SeatS1
BOSS160 = (1200.0, 0.0, 350.0, 140.0)                         # PC の x・行・幅・高さ
LOW_H = 1.2                                                    # 地面に立つ物で、これ未満の高さは「低い物」
ZONE_PX = 20                                                   # 帯に入ってよい見えている画素 (px²)
INTENT_PX = 4

# StageSeats.cs の表 (画角 22° の行) と人形の刻み
SEAT_T_PC = {1: [4.459], 2: [3.2, 7.055], 3: [2.2, 5.708, 8.741], 4: [1.6, 4.417, 6.716, 10.01]}
SEAT_T_PH = {1: [4.431], 2: [3.2, 7.02], 3: [2.2, 5.651, 8.617], 4: [1.6, 4.387, 6.645, 9.994]}
DOLL_STEP = {False: 0.93, True: 0.945}
DOLL_BACK = {False: (0.45, 2.0), True: (0.5, 2.8)}
LEADER = (-5.0, 0.9)


def cams():
    return [Cam('PC', 1920, 1080, False, 22, 5, 0.36), Cam('PH', 1920, 886, True, 22, 7, 0.525),
            Cam('PC21', 2560, 1080, False, 22, 5, 0.36)]


# ------------------------------------------------------------------ 座席

def enemy_seats(cam, G, n):
    tbl = SEAT_T_PH if cam.phone else SEAT_T_PC
    sB = 0.1 if (cam.phone or n >= 3) else 0.7
    out = []
    for i, t in enumerate(tbl[n]):
        s = -0.5 if i % 2 == 0 else sB
        x, y, d = cam.project(G.on_path(t, s, 0.0))
        out.append(dict(t=t, s=s, x=float(x), y=float(y), d=float(d)))
    return out


def doll_seats(cam, G):
    out = []
    step = DOLL_STEP[cam.phone]
    bdt, bds = DOLL_BACK[cam.phone]
    for i in range(9):
        j = i % 5
        back = i >= 5
        t = -3.9 + j * step + (bdt if back else 0.0)
        near_first = i == 0 and not cam.phone
        s = (0.25 if near_first else (0.9 if j % 2 == 0 else 0.25)) + (bds if back else 0.0)
        x, y, d = cam.project(G.on_path(t, s, 0.0))
        out.append(dict(t=t, s=s, x=float(x), y=float(y), d=float(d)))
    return out


def frame_px(cam, dots):
    """敵の絵の枠 (ドット) → 画面の高さ px。PC は 1 ドット 4px。スマホは 0.6 倍×キャンバスの倍率 (128 は _96 の絵 = 96 ドット×2.4)"""
    if not cam.phone:
        return dots * 4.0
    sf = cam.canvas_sf
    if dots >= 128:
        return 96 * 2.4 * sf
    return dots * 2.4 * sf


UI_SCENES = ('wolf', 'ogre', 'trio', 'quad', 'dolls')
ALLOW_MISSING_UI = False   # --allow-missing-ui (gen_act2.py・place2.py) で True。普段は無ければ止まる
_ui_warned = set()


class MissingUIError(RuntimeError):
    """基準の撮影の UI の矩形のファイルが無い (呼び手の main が標準エラーに名前を出して終了コード 1 にする)"""

    def __init__(self, files):
        super().__init__('基準の撮影の UI の矩形が無い: ' + '・'.join(files))
        self.files = files


def load_ui(cam):
    """基準の撮影 (HEAD・幕1 の見本。UI の矩形は幕を見ない) の意図の札と UI の矩形。
    ファイルが 1 つでも無ければ MissingUIError (ALLOW_MISSING_UI の時だけ標準エラーに警告して、ある物だけで続ける)"""
    pre = 'PH' if cam.phone else 'PC'
    res = dict(intents=[], ui=[], topbar=None, hand=[], strips=[])
    if cam.name == 'PC21':
        return res
    missing = [os.path.join(SHOTS, '%s-S-%s-1.layout.json' % (pre, sc)) for sc in UI_SCENES
               if not os.path.exists(os.path.join(SHOTS, '%s-S-%s-1.layout.json' % (pre, sc)))]
    if missing:
        if not ALLOW_MISSING_UI:
            raise MissingUIError(missing)
        for fp in missing:
            if fp not in _ui_warned:
                _ui_warned.add(fp)
                print('警告: 基準の撮影の UI の矩形が無い (--allow-missing-ui で続ける。R3 と UI の帯の判定が甘くなる): %s' % fp, file=sys.stderr)
    for scene in UI_SCENES:
        fp = os.path.join(SHOTS, '%s-S-%s-1.layout.json' % (pre, scene))
        if not os.path.exists(fp):
            continue
        d = json.load(open(fp))
        for u in d['units']:
            if isinstance(u, dict) and u.get('kind') == 'enemy':
                if u.get('intent'):
                    res['intents'].append(('%s-%s' % (scene, u.get('id')), [float(v) for v in u['intent']]))
                if u.get('strip'):
                    res['strips'].append(('%s-strip' % scene, [float(v) for v in u['strip']]))
        tb = (d.get('anchors') or {}).get('topbar')
        if tb:
            res['topbar'] = tb
        for n in d.get('nodes', []):
            p = n.get('path', '') if isinstance(n, dict) else ''
            if n.get('px') and re.search(r'/player/(hpwrap|box|setzone|gearzone|perms|chips)$|/ui/energyOrb$|/ui/relics?$|relicrow$', p):
                res['ui'].append((p.split('/')[-1], [float(v) for v in n['px']]))
        res['hand'] += [('hand', [float(v) for v in h['px']]) for h in d.get('hand', []) if isinstance(h, dict) and h.get('px')]
    return res


def seat_zones(cam, G, ui):
    """規則 R2 の帯と R4 の足元の通り。[(名前, 足元 x, 足元の行, 深さ, 半幅, 頭の行)]"""
    k = 0.7875 if cam.phone else 1.0
    zones = []
    hx, hy, hd = cam.project(G.on_path(LEADER[0], LEADER[1], 0.0))
    zones.append(('hero', float(hx), float(hy), float(hd), 60.0 * k, float(hy) - 62 * (2.4 * cam.canvas_sf if cam.phone else 4.0) - 10))
    # 敵: 体数ごとの絵の枠 (幕2 の編成: 1体 = 64・80・128／2体 = 64・巨蟹 96／3体 = 64・番人 96+絡繰 48／4体以上 = 64)
    frames = {1: [128], 2: [96], 3: [96, 64, 64], 4: [64, 64, 64, 64]}
    for n in (1, 2, 3, 4):
        for i, e in enumerate(enemy_seats(cam, G, n)):
            fr = frames[n][i] if i < len(frames[n]) else 64
            top = e['y'] - frame_px(cam, fr) - 30
            zones.append(('enemy%d-%d' % (n, i), e['x'], e['y'], e['d'], 140.0 * k, top))
    for i, dl in enumerate(doll_seats(cam, G)):
        zones.append(('doll%d' % i, dl['x'], dl['y'], dl['d'], 50.0 * k, dl['y'] - (32 * (2.4 * cam.canvas_sf if cam.phone else 4.0)) - 10))
    # 意図の札の上端が絵の上端より上の座席は、帯の頭を意図の札の上端へ
    for nm, r in ui['intents']:
        cx = r[0] + r[2] / 2.0
        for j, z in enumerate(zones):
            if z[0].startswith('enemy') and abs(z[1] - cx) < 60 and r[1] < z[5]:
                zones[j] = z[:5] + (r[1],)
    return zones


def intent_rects(cam, G, ui):
    """基準の撮影の意図の札＋撮影の無い並びの見積もり (2体 64・巨蟹 96・強個体 80)"""
    rects = list(ui['intents'])
    est = []
    for n, dots in ((2, 64), (2, 96), (1, 80)):
        for i, e in enumerate(enemy_seats(cam, G, n)):
            hpx = frame_px(cam, dots)
            w = 200.0 * (0.7875 if cam.phone else 1.0)
            h = 56.0 * (0.98 if cam.phone else 1.0)
            # 絵の上端の少し下 (頭) に意図の札の下端が来る (撮影の 64: 上端 +20・128: +96 の間を枠で比例)
            bottom = e['y'] - hpx + (20.0 + (hpx - 256.0) * 0.3 if not cam.phone else 16.0 + (hpx - 201.6) * 0.3)
            est.append(('est-n%d-%d-%d' % (n, dots, i), [e['x'] - w / 2.0, bottom - h, w, h]))
    return rects + est


# ------------------------------------------------------------------ 部品

NO_SILHOUETTE_KINDS = ('slab', 'mist', 'fog', 'shaft', 'litter', 'frame', 'halo', 'rail')
NO_SILHOUETTE_NAMES = ('wall-', 'ledge-', 'edge-', 'stake-', 'steps-')   # steps- = 段をつなぐ石段・木の階段 (段の一部。高さ 0.3 の板を積むので「浮いた物」に見える)
NO_INTENT_KINDS = ('slab', 'mist', 'fog', 'rail')   # rail = 二段目の地面の上の線路 (奥の端が意図の札の行へ上がるが地面の一部)
NO_INTENT_NAMES = ('wall-', 'steps-')
LIGHT_KINDS = ('halo', 'shaft')


class P2:
    """幕2 の新 kind も含めた 1 つの部品の画面の姿 (place.Placed の欄に合わせる)"""
    pass


def num(p, k, d):
    v = p.get(k, d)
    return float(v) if isinstance(v, (int, float)) else d


def phone_place(p, cam):
    """Diorama.PlaceOf: スマホなら "phone" の t・s・y・scale で上書き (段と額縁は上書きしない)"""
    ph = p.get('phone') if (cam.phone and isinstance(p.get('phone'), dict) and p['kind'] not in ('slab', 'frame')) else None
    if ph and ph.get('hide'):
        return None
    if ph:
        q = dict(p)
        for kk in ('t', 's', 'y', 'scale', 'w', 'h', 'alpha', 'r'):
            if kk in ph:
                q[kk] = ph[kk]
        return q
    return p


def place_new(L, G, cam, i, p0, with_image):
    """rail・halo・arch・pillar (幕2 で足した kind)。箱 (rail・arch・pillar) と円 (halo)"""
    p = phone_place(p0, cam)
    o = place.Placed()
    o.i, o.p, o.kind = i, (p if p is not None else p0), p0['kind']
    o.src = None
    o.art, o.box, o.depth, o.visible, o.facing, o.mask, o.rgba, o.origin, o.wpx, o.note, o.worldH, o.foot = None, None, None, False, True, None, None, (0, 0), 0.0, '', 0.0, None
    if p is None:
        o.note = 'phone-hide'
        return o
    t, s = num(p, 't', 0.0), num(p, 's', 0.0)
    gy = G.gy(t, s)
    y0 = num(p, 'y', 0.0) if p.get('abs') else gy + num(p, 'y', 0.0)
    pos = G.on_path(t, s, y0)
    sc = num(p, 'scale', 1.0) or 1.0
    fx, fy, fd = cam.project(pos)
    o.foot = (float(fx), float(fy))
    k = p['kind']
    if k == 'halo':
        r = num(p, 'r', 0.8) * sc
        cx, cy, cd = cam.project(pos)
        rp = r * cam.f / max(cd, 0.1)
        o.box = (float(cx - rp), float(cy - rp), float(cx + rp), float(cy + rp))
        o.depth = float(cd)
        o.worldH = 2 * r
        o.wpx = 2 * rp
        if with_image and rp > 0.5 and o.box[2] > -2000 and o.box[0] < cam.W + 2000:
            x0, y0_ = int(math.floor(o.box[0])), int(math.floor(o.box[1]))
            sz = int(math.ceil(2 * rp)) + 2
            yy, xx = np.mgrid[0:sz, 0:sz]
            dd = np.sqrt((xx + x0 - cx) ** 2 + (yy + y0_ - cy) ** 2) / max(rp, 1e-3)
            a = np.clip(1.0 - dd, 0, 1) ** 1.6
            col = p.get('color', [1.0, 0.7, 0.4])
            col = [c / 255.0 if any(v > 1.001 for v in col) else c for c in col]
            rg = np.zeros((sz, sz, 4), np.uint8)
            rg[..., 0] = int(255 * col[0])
            rg[..., 1] = int(255 * col[1])
            rg[..., 2] = int(255 * col[2])
            rg[..., 3] = (a * 255 * min(1.0, num(p, 'gain', 1.0))).astype(np.uint8)
            o.rgba, o.mask, o.origin = rg, a > 0.15, (x0, y0_)
        o.visible = o.box[2] > 0 and o.box[0] < cam.W and o.box[3] > 0 and o.box[1] < cam.H
        return o
    yaw = G.yaw + num(p, 'yaw', 0.0)
    if k == 'rail':
        gauge = num(p, 'gauge', 1.2)
        ex, ez = num(p, 'length', 8.0) / 2, num(p, 'sleeperLen', gauge + 0.6) / 2
        y1, yb = num(p, 'railH', 0.1) + num(p, 'sleeperH', 0.07), 0.0
    elif k == 'pillar':
        ex, ez = num(p, 'w', 0.9) / 2 + num(p, 'baseOver', 0.12), num(p, 'd', 0.9) / 2 + num(p, 'baseOver', 0.12)
        y1, yb = num(p, 'h', 5.0), 0.0
    else:  # arch
        ex, ez = num(p, 'w', 4.0) / 2, num(p, 'd', 1.0) / 2
        y1, yb = num(p, 'h', 4.5), -num(p, 'sink', 0.15)
    ex, ez, y1, yb = ex * sc, ez * sc, y1 * sc, yb * sc
    o.worldH = y1 - yb
    pts = [cam.project(pos + yaw_rot(yaw, lx, ly, lz)) for lx in (-ex, ex) for lz in (-ez, ez) for ly in (yb, y1)]
    xs = [q[0] for q in pts]
    ys = [q[1] for q in pts]
    o.box = (min(xs), min(ys), max(xs), max(ys))
    o.wpx = float(o.box[2] - o.box[0])
    o.depth = float(cam.project(pos + np.array([0, (y1 + yb) / 2, 0]))[2])
    if with_image:
        hl = place.hull([(q[0], q[1]) for q in pts])
        x0, y0_, x1, y1_ = [int(math.floor(v)) for v in o.box]
        x1 += 2
        y1_ += 2
        if 0 < x1 - x0 < 8000 and 0 < y1_ - y0_ < 8000:
            m = Image.new('L', (x1 - x0, y1_ - y0_), 0)
            ImageDraw.Draw(m).polygon([(a - x0, b - y0_) for a, b in hl], fill=255)
            o.mask = np.array(m) > 0
            o.origin = (x0, y0_)
            col = {'rail': (70, 64, 60), 'pillar': (150, 150, 140), 'arch': (150, 150, 140)}[k]
            rg = np.zeros(o.mask.shape + (4,), np.uint8)
            rg[o.mask] = col + (255,)
            o.rgba = rg
    o.visible = o.box[2] > 0 and o.box[0] < cam.W and o.box[3] > 0 and o.box[1] < cam.H and o.depth > 0.3
    return o


def place_any(L, G, cam, i, p, with_image):
    if p['kind'] in ('rail', 'halo', 'arch', 'pillar'):
        return place_new(L, G, cam, i, p, with_image)
    # place.place_part は block の scale を掛ける・marker は 0.3×0.2 の箱。"ao" などの新しいキーは読まない (形の大きさには効かない)
    return place.place_part(L, G, cam, i, p, with_image=with_image)


# ------------------------------------------------------------------ 座席の帯の足跡

def footprint(L, p, phone=False):
    """部品の足跡 (道の座標の多角形)。None = 数えない"""
    k = p['kind']
    if k in ('slab', 'litter', 'mist', 'fog', 'shaft', 'frame', 'halo'):
        return None
    if phone:
        ph = p.get('phone')
        if isinstance(ph, dict):
            if ph.get('hide'):
                return None
            p = dict(p)
            for kk in ('t', 's', 'scale'):
                if kk in ph:
                    p[kk] = ph[kk]
    t, s = num(p, 't', 0.0), num(p, 's', 0.0)
    sc = num(p, 'scale', 1.0) or 1.0
    if k in ('block', 'rig', 'fence', 'marker', 'rail', 'arch', 'pillar'):
        if k == 'block':
            hw, hd = num(p, 'w', 1.6) / 2, num(p, 'd', 1.2) / 2
        elif k == 'rig':
            hw, hd = num(p, 'w', 2.0) / 2, num(p, 'd', 1.6) / 2
        elif k == 'fence':
            hw, hd = num(p, 'len', 3.0) / 2, 0.1
        elif k == 'marker':
            hw, hd = 0.5, 0.15
        elif k == 'rail':
            hw, hd = num(p, 'length', 8.0) / 2, num(p, 'sleeperLen', num(p, 'gauge', 1.2) + 0.6) / 2
        elif k == 'pillar':
            hw, hd = num(p, 'w', 0.9) / 2 + 0.15, num(p, 'd', 0.9) / 2 + 0.15
        else:
            hw, hd = num(p, 'w', 4.0) / 2, num(p, 'd', 1.0) / 2
        hw, hd = hw * sc, hd * sc
        a = math.radians(num(p, 'yaw', 0.0))
        pts = []
        for lx, lz in ((-hw, -hd), (hw, -hd), (hw, hd), (-hw, hd)):
            pts.append((t + lx * math.cos(a) + lz * math.sin(a), s - lx * math.sin(a) + lz * math.cos(a)))
        return pts
    if k == 'rock':
        r = num(p, 'r', 0.6) * sc
    elif k == 'tree':
        r = num(p, 'r', 0.55) + num(p, 'rootLen', 1.6)
    else:   # relief・card: 板の幅の半分 (絵が無ければ 0.3)
        im, _ = place.art_for(L, p.get('src') or p.get('relief')) if (p.get('src') or p.get('relief')) else (None, None)
        r = (im.size[0] / TPU * sc / 2) if im is not None else 0.3
    return [(t - r, s - r), (t + r, s - r), (t + r, s + r), (t - r, s + r)]


def poly_hits_band(pts):
    ts = [q[0] for q in pts]
    ss = [q[1] for q in pts]
    return max(ts) > SEAT_T0 and min(ts) < SEAT_T1 and max(ss) > SEAT_S0 and min(ss) < SEAT_S1


def diorama_reach_intrusions(L, phone=False):
    """Diorama.Check の数え方そのまま (届き・abs は数えない・PlaceOf)"""
    out = []
    for i, p in enumerate(L['parts']):
        k = p['kind']
        if k in ('litter', 'slab', 'fog', 'shaft', 'frame', 'mist', 'halo'):
            continue
        q = phone_place(p, Cam('x', 1920, 886, phone, 22, 7, 0.525)) if phone else p
        if q is None:
            continue
        g = num(q, 'gauge', 1.2)
        reach = {'rock': num(q, 'r', 0.6), 'block': max(num(q, 'w', 1.6), num(q, 'd', 1.2)) * 0.5,
                 'tree': num(q, 'r', 0.55) + num(q, 'rootLen', 1.6), 'fence': num(q, 'len', 3.0) * 0.5, 'rig': 1.5,
                 # 段2 (Diorama.S2S_Reach の写し): 線路・門・柱
                 'rail': max(num(q, 'length', 8.0), num(q, 'sleeperLen', g + 0.6)) * 0.5,
                 'arch': max(num(q, 'w', 4.0), num(q, 'd', 1.0)) * 0.5,
                 'pillar': max(num(q, 'w', 0.9), num(q, 'd', 0.9)) * 0.5 + max(0.0, max(num(q, 'capOver', 0.15), num(q, 'baseOver', 0.12)))}.get(k, 0.3)
        t, s = num(q, 't', 0.0), num(q, 's', 0.0)
        if t + reach > SEAT_T0 and t - reach < SEAT_T1 and s + reach > SEAT_S0 and s - reach < SEAT_S1 and not q.get('abs'):
            out.append(dict(i=i, kind=k, name=p.get('name') or p.get('src'), t=t, s=s, reach=round(reach, 2)))
    return out


def seat_band_height(G):
    tt, ss = np.meshgrid(np.arange(SEAT_T0, SEAT_T1 + 1e-3, 0.25), np.arange(SEAT_S0, SEAT_S1 + 1e-3, 0.2))
    h, _ = G.height(tt.ravel(), ss.ravel())
    return float(np.max(np.abs(h)))


# ------------------------------------------------------------------ 板の立ち方 (規則 R6・R7。生成器 gen_act2.py も同じ関数で置き場を決める)

SUPPORT_NAMES = ('wall-', 'steps-')   # 上に物を載せる地面の block (岩棚・坑口の台・本体・上段・頂・奥・段をつなぐ階段)
HUNG_SRC = ('lanternHang', 'lanternHangB', 'stalactite', 'stalactiteCluster', 'rootsHang', 'pulley')   # 上から吊る物 (浮いてよい・沈んではいけない)
STAND_TOL = 0.04                      # 沈み・浮きの許し (unit)
BOARD_KINDS = ('relief', 'card')
_PHONE_CAM = Cam('x', 1920, 886, True, 22, 7, 0.525)   # phone_place は cam.phone だけを見る


def board_line(L, G, p, n=9):
    """板 (relief・card) の下端の高さ・背丈・下端の線の点 [(t, s)]。絵が無ければ None。
    板の yaw は世界の向き (Diorama: relief・card は道に沿わない = place.place_part と同じ)。p はスマホの置き場を当てた後の物を渡す"""
    src = p.get('src') or p.get('relief')
    im, _ = place.art_for(L, src) if src else (None, None)
    if im is None:
        return None
    sc = num(p, 'scale', 1.0) or 1.0
    w, h = im.size[0] / TPU * sc, im.size[1] / TPU * sc
    t, s = num(p, 't', 0.0), num(p, 's', 0.0)
    base = num(p, 'y', 0.0) if p.get('abs') else G.gy(t, s) + num(p, 'y', 0.0)
    pos = G.on_path(t, s, base)
    pts = []
    for lx in np.linspace(-w / 2, w / 2, n):
        wp = pos + yaw_rot(num(p, 'yaw', 0.0), lx, 0.0, 0.0)
        tt, ss = G.to_path(wp[0], wp[2])
        pts.append((float(tt), float(ss)))
    return base, h, pts


def block_span(G, b):
    """block の下端 (sink 込み) と上端 (place.place_part と同じ: 下 −sink×scale・上 h×scale)"""
    t, s = num(b, 't', 0.0), num(b, 's', 0.0)
    sc = num(b, 'scale', 1.0) or 1.0
    y = num(b, 'y', 0.0) if b.get('abs') else G.gy(t, s) + num(b, 'y', 0.0)
    return y - num(b, 'sink', 0.2) * sc, y + num(b, 'h', 1.0) * sc


def block_has(b, t, s):
    """道の座標 (t, s) が block の足跡 (yaw は道に足す・footprint と同じ回し方) の中か"""
    sc = num(b, 'scale', 1.0) or 1.0
    a = math.radians(num(b, 'yaw', 0.0))
    dt, ds = t - num(b, 't', 0.0), s - num(b, 's', 0.0)
    lx = dt * math.cos(a) - ds * math.sin(a)
    lz = dt * math.sin(a) + ds * math.cos(a)
    return abs(lx) <= num(b, 'w', 1.6) * sc / 2 and abs(lz) <= num(b, 'd', 1.2) * sc / 2


def stand_blocks(L, G, phone=False):
    """立ち方を見る block の一覧 [(置き場を当てた block, (下端, 上端))]。スマホなら phone の置き場・hide は外す"""
    out = []
    for b in L['parts']:
        if b['kind'] != 'block':
            continue
        q = phone_place(b, _PHONE_CAM) if phone else b
        if q is None:
            continue
        out.append((q, block_span(G, q)))
    return out


def same_group(nm, bn):
    """板と block が同じ物の一部 (提灯とその鉤・坑口の札と柱と梁)"""
    return bool(nm) and (bn.startswith(nm + '-') or (nm.startswith('mouth-') and bn.startswith('mouth-')))


def stand_info(L, G, p, blocks):
    """板の立ち方 (R6・R7)。p は置き場を当てた後の物。dict(base, top, support, who, d, pierce=[block の名前]) か None (絵が無い)"""
    bl = board_line(L, G, p)
    if bl is None:
        return None
    base, h, pts = bl
    nm = p.get('name') or ''
    sup, who = -1e9, None
    lo = 1e9                                # 下端の線の点ごとの支えのいちばん低い所 (段差を跨ぐと片側が浮く)
    pierce = set()
    for t, s in pts:
        g = G.gy(t, s)
        here = g
        if g > sup:
            sup, who = g, 'slab'
        for b, (b0, b1) in blocks:
            bn = b.get('name') or ''
            if same_group(nm, bn) or not block_has(b, t, s):
                continue
            if b0 <= base + 0.05:
                if bn.startswith(SUPPORT_NAMES):
                    here = max(here, b1)
                    if b1 > sup:
                        sup, who = b1, bn
                elif b1 > base + 0.02:
                    pierce.add(bn)          # 柱・脚が板の下から上へ抜ける
            elif b0 < base + h - 0.02:
                pierce.add(bn)              # 上から張り出す block (上段・歩廊の床・梁) が板の高さに掛かる
        lo = min(lo, here)
    return dict(base=round(base, 3), top=round(base + h, 3), support=round(sup, 3), supportMin=round(lo, 3), who=who, d=round(base - sup, 3),
                gap=round(base - lo, 3), pierce=sorted(pierce))


def stand_problems(L, G, p, blocks, phone=False):
    """1 つの板の R6・R7 の掛かり [(規則, 中身, 量)]。p は設計図のままの部品 (スマホなら phone の置き場をここで当てる)"""
    if p['kind'] not in BOARD_KINDS:
        return []
    q = phone_place(p, _PHONE_CAM) if phone else p
    if q is None:
        return []
    si = stand_info(L, G, q, blocks)
    if si is None:
        return []
    out = []
    hung = (p.get('src') or '') in HUNG_SRC
    if si['d'] < -STAND_TOL:
        out.append(('R6', '沈み', si['d'], si['who']))
    elif si['d'] > STAND_TOL and not hung:
        out.append(('R6', '浮き', si['d'], si['who']))
    elif si['gap'] > STAND_TOL and not hung:
        out.append(('R6', '片浮き', si['gap'], '%s と低い側 %.2f' % (si['who'], si['supportMin'])))   # 段差 (岩棚の区切り) を跨いで片側が浮く
    if si['pierce']:
        out.append(('R7', '貫き', len(si['pierce']), '・'.join(si['pierce'])))
    return out


# ------------------------------------------------------------------ 分類

def is_silhouette(p, G):
    """規則 R2 の「背の高い物」か (と、その理由)"""
    k = p['kind']
    nm = p.get('name') or ''
    if k in NO_SILHOUETTE_KINDS or nm.startswith(NO_SILHOUETTE_NAMES):
        return False, 'skip'
    t, s = num(p, 't', 0.0), num(p, 's', 0.0)
    gy = G.gy(t, s)
    base = num(p, 'y', 0.0) if p.get('abs') else gy + num(p, 'y', 0.0)
    if base - gy > 0.5:
        return True, 'elevated'
    return None, 'ground'   # 地面に立つ物 = 高さで決める (呼び手が worldH を見る)


def surfaces_used(L):
    used = set()
    for p in L['parts']:
        k = p['kind']
        if k == 'mist':
            used.add('(mist)')
            continue
        if p.get('surface'):
            used.add(p['surface'])
            continue
        d = {'slab': 'terrain', 'card': 'card' if 'card' in L.get('surfaces', {}) else 'relief', 'litter': 'card' if 'card' in L.get('surfaces', {}) else 'relief',
             'block': 'rock', 'rock': 'rock', 'tree': 'bark', 'rig': 'wood', 'fence': 'wood', 'marker': 'wood', 'fog': 'glow', 'shaft': 'glow',
             'halo': 'glow', 'rail': 'wood', 'arch': 'rock', 'pillar': 'rock'}.get(k, 'relief')
        used.add(d)
        if k == 'rail':
            used.add(p.get('railSurface') or ('iron' if 'iron' in L.get('surfaces', {}) else 'rock'))
        if k == 'rig':
            used.add('bark')
    return sorted(used)


# ------------------------------------------------------------------ 検査

def rect_count(vm, r):
    x0, y0, w, h = r
    H, W = vm.shape
    a0, b0 = max(0, int(round(x0))), max(0, int(round(y0)))
    a1, b1 = min(W, int(round(x0 + w))), min(H, int(round(y0 + h)))
    if a1 <= a0 or b1 <= b0:
        return 0
    return int(vm[b0:b1, a0:a1].sum())


def check(L, want_img=False, out_dir=LANE_DIR, tag=None):
    from collections import Counter
    G = Ground(L)
    tag = tag or L.get('_name', 'layout')
    res = dict(name=tag, parts=len(L['parts']), byKind=dict(Counter(p['kind'] for p in L['parts'])))
    res['litter'] = sum(1 for p in L['parts'] if p['kind'] == 'litter')
    res['gates'] = L.get('gates')
    su = surfaces_used(L)
    res['surfacesUsed'] = su
    res['materials'] = len(su)
    res['surfacesDeclared'] = sorted((L.get('surfaces') or {}).keys())
    res['missingSurfaces'] = [s for s in su if s != '(mist)' and s not in (L.get('surfaces') or {})]
    res['seatBandMaxAbsY'] = round(seat_band_height(G), 4)
    band = []
    for ph in (False, True):
        for i, p in enumerate(L['parts']):
            fp = footprint(L, p, phone=ph)
            if fp and poly_hits_band(fp):
                band.append(dict(i=i, phone=ph, kind=p['kind'], name=p.get('name') or p.get('src'), t=p.get('t'), s=p.get('s')))
    res['seatBandParts'] = band
    res['dioramaReach'] = {'PC': diorama_reach_intrusions(L, False), 'PH': diorama_reach_intrusions(L, True)}
    # 規則 R6・R7: 板 (relief・card) の立ち方。カメラに依らない (置き場だけ PC とスマホ)。吊る物の浮きは数だけ hung に出す
    stand, hung = [], []
    for ph in (False, True):
        blocks = stand_blocks(L, G, phone=ph)
        for i, p in enumerate(L['parts']):
            if p['kind'] not in BOARD_KINDS:
                continue
            q = phone_place(p, _PHONE_CAM) if ph else p
            for rule, what, v, who in stand_problems(L, G, p, blocks, phone=ph):
                stand.append(dict(i=i, phone=ph, rule=rule, what=what, v=v, who=who, name=p.get('name') or p.get('src'), t=q.get('t'), s=q.get('s'), y=q.get('y')))
            if q is not None and (p.get('src') or '') in HUNG_SRC:
                si = stand_info(L, G, q, blocks)
                if si is not None and si['d'] > STAND_TOL:
                    hung.append(dict(name=p.get('name') or p.get('src'), phone=ph, d=si['d'], who=si['who']))
    res['stand'] = stand
    res['hungFloat'] = hung
    g = L.get('gates') or {}
    viol = []
    for b in stand:
        if b['rule'] == 'R6':
            viol.append('R6 %s %s #%d %s (t %s・s %s・y %s) %+.2f (支え %s)' % ('スマホ' if b['phone'] else 'PC', b['what'], b['i'], b['name'], b['t'], b['s'], b['y'], b['v'], b['who']))
        else:
            viol.append('R7 %s %s #%d %s (t %s・s %s・y %s) を %s が通る' % ('スマホ' if b['phone'] else 'PC', b['what'], b['i'], b['name'], b['t'], b['s'], b['y'], b['who']))
    if res['seatBandMaxAbsY'] >= 0.01:
        viol.append('R1 座席の帯の高さ %.3f' % res['seatBandMaxAbsY'])
    for b in band:
        viol.append('R1 座席の帯の部品 #%d %s (%s) %s' % (b['i'], b['name'], b['kind'], 'スマホ' if b['phone'] else 'PC'))
    for cn, lst in res['dioramaReach'].items():
        for b in lst:
            viol.append('R1 Diorama.Check の届きで座席の帯 #%d %s (%s)' % (b['i'], b['name'], cn))
    if g:
        if not (g.get('partsMin', 0) <= res['parts'] <= g.get('partsMax', 10 ** 6)):
            viol.append('R5 部品 %d (門 %s〜%s)' % (res['parts'], g.get('partsMin'), g.get('partsMax')))
        if res['litter'] > g.get('litterMax', 10 ** 6):
            viol.append('R5 小札 %d (門 %s)' % (res['litter'], g.get('litterMax')))
        if res['materials'] > g.get('materialsMax', 10 ** 6):
            viol.append('R5 材質 %d (門 %s)' % (res['materials'], g.get('materialsMax')))
    for m in res['missingSurfaces']:
        viol.append('R5 材質 %s が surfaces に無い' % m)
    per = {}
    for cam in cams():
        ui = load_ui(cam)
        zones = seat_zones(cam, G, ui)
        intents = intent_rects(cam, G, ui) if cam.name != 'PC21' else []
        rc = G.raycast(cam, 4)
        tdf = place.upsample(rc, cam.W, cam.H)
        placed = []
        for i, p in enumerate(L['parts']):
            o = place_any(L, G, cam, i, p, want_img or True)
            if o is not None:
                placed.append(o)
        info = dict(zones=[dict(name=z[0], x=round(z[1]), row=round(z[2]), depth=round(z[3], 1), half=round(z[4]), headRow=round(z[5])) for z in zones])
        cv = []   # 規則 R2
        low = []
        for o in placed:
            if o.note in ('phone-hide', 'no-art', 'flag-hide') or o.box is None:
                continue
            sil, why = is_silhouette(o.p, G)
            if sil is False:
                continue
            if sil is None:
                if (o.worldH or 0.0) < LOW_H:
                    sil, why = 'low', 'ground-low'
                else:
                    sil, why = True, 'ground-tall'
            vm, npx = place.visible_mask(o, cam, tdf, cam.W, cam.H)
            if vm is None or npx == 0:
                continue
            for nm, fx, fy, fd, hw, head in zones:
                r = (fx - hw, head, 2 * hw, fy + 20 - head)
                c = rect_count(vm, r)
                if c > ZONE_PX:
                    rec = dict(i=o.i, kind=o.kind, name=o.p.get('name') or o.src, seat=nm, px=c, why=why, behind=bool(o.depth > fd),
                               box=[round(v) for v in o.box], worldH=round(o.worldH or 0, 2))
                    (low if sil == 'low' else cv).append(rec)
        info['tallBehind'] = cv
        info['lowBehind'] = low
        # 規則 R3: 意図の札・幕ボス 160
        it, lights = [], []
        rects = list(intents)
        if cam.name == 'PC':
            rects.append(('boss160', list(BOSS160)))
        for o in placed:
            if o.note in ('phone-hide', 'no-art', 'flag-hide') or o.box is None:
                continue
            nm = o.p.get('name') or ''
            if o.kind in NO_INTENT_KINDS or nm.startswith(NO_INTENT_NAMES) or (o.kind == 'frame' and o.p.get('src') == 'backdropPlain'):
                continue
            if o.kind == 'litter':
                continue
            vm, npx = place.visible_mask(o, cam, tdf if o.kind != 'frame' else None, cam.W, cam.H)
            if vm is None or npx == 0:
                continue
            for rn, r in rects:
                c = rect_count(vm, r)
                lim = 0.01 * r[2] * r[3] if rn == 'boss160' else INTENT_PX
                if c > lim:
                    rec = dict(i=o.i, kind=o.kind, name=nm or o.src, rect=rn, px=c)
                    (lights if o.kind in LIGHT_KINDS else it).append(rec)
        # 光の筋 (shaft) の四角形も意図の札に掛けない
        for p in L['parts']:
            if p['kind'] != 'shaft':
                continue
            q = phone_place(p, cam)
            if q is None:
                continue
            sh = place.place_shaft(G, cam, q)
            qx = [a[0] for a in sh['quad']]
            qy = [a[1] for a in sh['quad']]
            for rn, r in rects:
                if max(qx) >= r[0] and min(qx) <= r[0] + r[2] and max(qy) >= r[1] and min(qy) <= r[1] + r[3]:
                    lights.append(dict(name=p.get('name'), kind='shaft', rect=rn, px=-1))
        info['intent'] = it
        info['lightsOnIntent'] = lights
        # 規則 R4: 小札・札が足元の通りに入らない
        corr = []
        for o in placed:
            if o.kind not in ('litter', 'card') or o.box is None or not o.visible:
                continue
            x0, y0, x1, y1 = o.box
            for nm, fx, fy, fd, hw, head in zones:
                if x1 >= fx - hw and x0 <= fx + hw and y1 >= fy - 8 and y0 <= fy + 20:
                    corr.append(dict(i=o.i, kind=o.kind, src=o.src, seat=nm, t=o.p.get('t'), s=o.p.get('s')))
                    break
        info['feetCorridor'] = corr
        # 名前つきの部品 (光の置き場・スマホの UI との当たり)
        named = {}
        for o in placed:
            nm = o.p.get('name')
            if nm and (nm.startswith(('lantern-', 'hearth', 'mouth-', 'post-', 'beam-', 'gallery-', 'frame-')) or nm in ('hearth',)):
                if o.box is not None and o.note != 'phone-hide':
                    named[nm] = dict(foot=[round(v) for v in (o.foot or (0, 0))], box=[round(v) for v in o.box], depth=round(o.depth or 0, 1),
                                     t=o.p.get('t'), s=o.p.get('s'), y=o.p.get('y'), abs=bool(o.p.get('abs')))
                elif o.note == 'phone-hide':
                    named[nm] = 'phone-hide'
        info['named'] = named
        if cam.phone:
            strip = (0.0, 81.0, 640.0, 129.0)       # スマホの上の帯 (からくり・ギア) x 0〜640・行 81〜210
            selfc = (36.0, 378.0, 286.0, 98.0)      # スマホの自分の札
            hits = []
            for o in placed:
                nm = o.p.get('name') or ''
                if not nm.startswith(('lantern-', 'hearth')) or o.box is None or o.note == 'phone-hide':
                    continue
                vm, npx = place.visible_mask(o, cam, tdf, cam.W, cam.H)
                if vm is None or npx == 0:
                    continue
                hits.append(dict(name=nm, strip=rect_count(vm, strip), self=rect_count(vm, selfc), visible=npx))
            info['phoneUi'] = hits
        # 額縁と UI (L8 の形: 額縁の箱の 30% 以下)
        fr = []
        if cam.name != 'PC21':
            uirects = [r for _, r in ui['ui']] + [r for _, r in ui['hand']] + [r for _, r in ui['strips']] + [r for _, r in intents]
            if ui['topbar']:
                uirects.append(ui['topbar'])
            for o in placed:   # 額縁 (kind frame) と、世界に置いた手前の額縁 (名前 frame-・幕2 の block・rock)
                is_frame = o.kind == 'frame' or (o.p.get('name') or '').startswith('frame-')
                if not is_frame or o.box is None or o.p.get('src') == 'backdropPlain' or o.note == 'phone-hide':
                    continue
                x0, y0, x1, y1 = o.box
                cx0, cy0, cx1, cy1 = max(0, x0), max(0, y0), min(cam.W, x1), min(cam.H, y1)
                if cx1 <= cx0 or cy1 <= cy0:
                    fr.append(dict(name=o.p.get('name'), onscreen=0))
                    continue
                cov = place._rect_union_frac((cx0, cy0, cx1 - cx0, cy1 - cy0), uirects)
                fr.append(dict(name=o.p.get('name'), box=[round(cx0), round(cy0), round(cx1), round(cy1)], ui=round(cov, 3)))
        info['frames'] = fr
        # 壁の帯 (主人公の列 ±125 で、壁の部品が占める行) と棚の天面の見かけの高さ
        info['wall'] = wall_info(L, G, cam, placed)
        per[cam.name] = info
        for v in cv:
            viol.append('R2 %s %s (%s) が %s の帯に %dpx² (%s・%s)' % (cam.name, v['name'], v['kind'], v['seat'], v['px'], v['why'], '後ろ' if v['behind'] else '手前'))
        for v in it:
            viol.append('R3 %s %s (%s) が %s に %dpx²' % (cam.name, v['name'], v['kind'], v['rect'], v['px']))
        for v in corr:
            viol.append('R4 %s 小札 %s (t %s・s %s) が %s の足元の通り' % (cam.name, v['src'], v['t'], v['s'], v['seat']))
        for f in fr:
            if f.get('ui', 0) > 0.30:
                viol.append('L8 %s 額縁 %s が UI と %.0f%% 重なる' % (cam.name, f['name'], f['ui'] * 100))
        if want_img:
            draw(cam, G, L, placed, rc, zones, intents, ui, os.path.join(out_dir, 'place2-%s-%s.png' % (tag, cam.name)))
    res['cams'] = per
    res['violations'] = viol
    return res


def wall_info(L, G, cam, placed):
    hero_x = cam.project(G.on_path(LEADER[0], LEADER[1], 0.0))[0]
    out = {}
    rows = []
    for o in placed:
        nm = o.p.get('name') or ''
        if nm.startswith('wall-') and o.box is not None and o.box[0] <= hero_x <= o.box[2]:
            rows.append((nm, round(o.box[1]), round(o.box[3])))
    out['heroColumn'] = sorted(rows, key=lambda r: r[1])
    # 棚の天面: 棚の前の縁と奥 (本体の前の面 = wall-main の前のいちばん手前。張り出しの変種で奥へ下がる) の、棚の天面の高さの行の差 (主人公の列と敵の列)
    shelf = [p for p in L['parts'] if (p.get('name') or '').startswith('wall-shelf')]
    mains = [p for p in L['parts'] if (p.get('name') or '').startswith('wall-main')]
    if shelf:
        front = min(num(p, 's', 10.9) - num(p, 'd', 1.0) / 2 for p in shelf)
        back = min(num(p, 's', 12.4) - num(p, 'd', 2.0) / 2 for p in mains) if mains else 11.4
        top = max(num(p, 'y', 1.8) + num(p, 'h', 1.1) for p in shelf)
        res = {}
        for t in (-2.6, 4.6, 10.0):
            a = cam.project(G.on_path(t, front, top))[1]
            b = cam.project(G.on_path(t, back, top))[1]
            res['t%.1f' % t] = round(float(a - b), 1)
        out['shelfTopPx'] = res
        out['shelfFront'] = round(front, 2)
        out['mainFace'] = round(back, 2)
    return out


# ------------------------------------------------------------------ 画 (光なしの構図。確かめ用)

SLAB_COL = [(58, 54, 50), (92, 86, 76), (108, 104, 96), (86, 92, 96), (70, 74, 80), (60, 60, 64)]


def draw(cam, G, L, placed, rc, zones, intents, ui, path):
    W, H = cam.W, cam.H
    img = np.zeros((H, W, 3), np.float32)
    img[:] = (22, 30, 36)
    step = rc['step']
    hit = rc['hit']
    for i in range(hit.shape[0]):
        for j in range(hit.shape[1]):
            if not hit[i, j]:
                continue
            wi = rc['slab'][i, j]
            c = np.array(SLAB_COL[wi % len(SLAB_COL)], np.float32)
            d = rc['depth'][i, j]
            k = float(np.clip(1.0 - (d - 20) / 60.0, 0.45, 1.0))
            y0, x0 = i * step, j * step
            img[y0:y0 + step, x0:x0 + step] = c * k
    # 壁 (wall-) は背景として先に描く (塊の中心の深さで並べると、壁の前に吊った提灯より「手前」になって提灯を塗りつぶす = 画だけの話・数え方は変わらない)
    order = sorted([o for o in placed if o.rgba is not None and o.mask is not None and o.note != 'phone-hide'],
                   key=lambda o: -((o.depth or 0) + (1000.0 if (o.p.get('name') or '').startswith('wall-') else 0.0)))
    tdf = place.upsample(rc, W, H)
    for o in order:
        X0, Y0 = o.origin
        h, w = o.mask.shape
        xa, ya = max(0, X0), max(0, Y0)
        xb, yb = min(W, X0 + w), min(H, Y0 + h)
        if xb <= xa or yb <= ya:
            continue
        sub = o.rgba[ya - Y0:yb - Y0, xa - X0:xb - X0].astype(np.float32)
        m = o.mask[ya - Y0:yb - Y0, xa - X0:xb - X0]
        if o.kind != 'frame' and o.kind != 'halo':
            m = m & (o.depth <= tdf[ya:yb, xa:xb] + 0.6)
        if o.kind == 'halo':
            a = (sub[..., 3:4] / 255.0) * 0.8
            img[ya:yb, xa:xb] = img[ya:yb, xa:xb] + sub[..., :3] * a
            continue
        nm = o.p.get('name') or ''
        col = sub[..., :3]
        if o.kind in ('block', 'rock', 'fence', 'marker', 'rig', 'rail'):
            base = {'rock': (96, 100, 104), 'wood': (104, 82, 60), 'plank': (120, 96, 70), 'iron': (54, 52, 58)}
            surf = o.p.get('surface') or {'block': 'rock', 'rock': 'rock', 'rail': 'wood'}.get(o.kind, 'wood')
            col = np.zeros_like(col) + np.array(base.get(surf, (100, 100, 100)), np.float32)
            if nm.startswith('wall-upper'):
                col *= 0.7
            if o.p.get('tint') is not None:
                tv = o.p['tint']
                tv = [tv] * 3 if isinstance(tv, (int, float)) else tv
                col = col * np.array(tv[:3], np.float32)
        if o.kind == 'frame':
            col = col * 0.6
        img[ya:yb, xa:xb][m] = col[m]
    im = Image.fromarray(np.clip(img, 0, 255).astype(np.uint8))
    d = ImageDraw.Draw(im, 'RGBA')
    for nm, fx, fy, fd, hw, head in zones:
        colr = (255, 210, 80, 255) if nm == 'hero' else ((120, 220, 255, 255) if nm.startswith('doll') else (255, 120, 120, 255))
        if nm.startswith('enemy4') or nm.startswith('enemy1') or nm.startswith('enemy2') or nm.startswith('enemy3') or nm in ('hero',) or nm.startswith('doll'):
            d.rectangle([fx - hw, head, fx + hw, fy + 20], outline=colr[:3] + (90,), width=1)
            d.ellipse([fx - 4, fy - 4, fx + 4, fy + 4], fill=colr)
    for nm, r in intents:
        d.rectangle([r[0], r[1], r[0] + r[2], r[1] + r[3]], outline=(255, 160, 40, 220), width=2)
    if cam.name == 'PC':
        b = BOSS160
        d.rectangle([b[0], b[1], b[0] + b[2], b[1] + b[3]], outline=(255, 60, 60, 255), width=2)
    for _, r in ui['ui'] + ui['hand']:
        d.rectangle([r[0], r[1], r[0] + r[2], r[1] + r[3]], fill=(10, 14, 30, 110), outline=(200, 200, 220, 120))
    if ui['topbar']:
        r = ui['topbar']
        d.rectangle([r[0], r[1], r[0] + r[2], r[1] + r[3]], fill=(10, 14, 30, 150))
    if cam.phone:
        d.rectangle([0, 81, 640, 210], outline=(160, 255, 160, 200), width=2)
    for o in placed:
        nm = o.p.get('name') or ''
        if o.box is not None and nm and o.note != 'phone-hide' and (nm.startswith(('lantern-', 'hearth', 'mouth-', 'post-'))):
            d.text((o.box[0], max(0, o.box[1] - 12)), nm, fill=(255, 255, 255, 230))
    im.save(path)


# ------------------------------------------------------------------ 表

def summary_md(results):
    L = []
    L.append('# place2 — 幕2 の設計図の規則の検査（レーン C・自動生成）\n')
    L.append('規則と数え方は `docs/design/hd2d-stage2/layout-gen/place2.py` の頭。カメラ PC 22°/5°/0.36・PH 22°/7°/0.525・PC21 2560×1080。'
             'UI の矩形は基準の撮影 `s2-base/s2-slice`（幕1 の見本・HEAD。UI と座席は幕を見ない）。\n')
    L.append('| 設計図 | 部品 | 小札 | 材質 | 門 | 座席の帯 |y| | 座席の帯の部品 | 違反 |')
    L.append('|---|---|---|---|---|---|---|---|')
    for r in results:
        g = r.get('gates') or {}
        L.append('| %s | %d | %d | %d (%s) | %s〜%s・小札 %s・材質 %s | %.3f | %d | **%d** |' % (
            r['name'], r['parts'], r['litter'], r['materials'], '・'.join(r['surfacesUsed']), g.get('partsMin'), g.get('partsMax'),
            g.get('litterMax'), g.get('materialsMax'), r['seatBandMaxAbsY'], len(r['seatBandParts']), len(r['violations'])))
    for r in results:
        L.append('\n## %s\n' % r['name'])
        L.append('kind: ' + '・'.join('%s %d' % (k, v) for k, v in sorted(r['byKind'].items(), key=lambda kv: -kv[1])))
        L.append('')
        if r['violations']:
            L.append('**違反 %d**' % len(r['violations']))
            for v in r['violations'][:80]:
                L.append('- ' + v)
        else:
            L.append('**違反 0**（R1 座席の帯・R2 足元の通りの真後ろ・R3 意図の札と幕ボス 160 の頭・R4 小札の足元・R5 数・R6 板の立ち方・R7 板の貫き・L8 額縁）')
        if r.get('hungFloat'):
            L.append('\n吊る物の浮き（違反ではない・R6 の例外）: ' + '・'.join('%s%s %+.2f（下は %s）' % (
                h['name'], '（スマホ）' if h['phone'] else '', h['d'], h['who']) for h in r['hungFloat']))
        for cn, info in r['cams'].items():
            L.append('\n### %s\n' % cn)
            L.append('- 低い物（高さ %.1f 未満の地面の小物）が座席の帯に入る: %d 件（違反ではない・数だけ）%s' % (
                LOW_H, len(info['lowBehind']), ('　' + '・'.join(sorted({'%s→%s' % (d['name'], d['seat'].split('-')[0]) for d in info['lowBehind']}))[:400]) if info['lowBehind'] else ''))
            if info['lightsOnIntent']:
                L.append('- 光（暈・筋）が意図の札／160 の頭に掛かる（違反ではない）: ' + '・'.join('%s→%s' % (d['name'], d['rect']) for d in info['lightsOnIntent']))
            w = info.get('wall') or {}
            if w:
                L.append('- 壁: 主人公の列の壁の部品の行 %s・棚の天面の見かけ %s px（棚の前の縁 s %s・本体の前の面 s %s）' % (
                    '・'.join('%s %d〜%d' % x for x in w.get('heroColumn', [])), w.get('shelfTopPx'), w.get('shelfFront'), w.get('mainFace')))
            nm = info.get('named') or {}
            if nm:
                L.append('- 名前つきの部品（足元 x・行／箱）:')
                for k, v in sorted(nm.items()):
                    if v == 'phone-hide':
                        L.append('  - %s: スマホでは組まない' % k)
                    else:
                        L.append('  - %s: t %s・s %s・y %s%s → 足元 %s・箱 %s・深さ %s' % (k, v['t'], v['s'], v['y'], '（abs）' if v['abs'] else '', v['foot'], v['box'], v['depth']))
            if info.get('phoneUi'):
                L.append('- スマホの上の帯（x 0〜640・行 81〜210）と自分の札に掛かる画素: ' + '・'.join('%s 帯 %d・札 %d／見える %d' % (d['name'], d['strip'], d['self'], d['visible']) for d in info['phoneUi']))
            if info.get('frames'):
                L.append('- 額縁と UI の重なり: ' + '・'.join('%s %s' % (f['name'], ('%.0f%%' % (f['ui'] * 100)) if 'ui' in f else '画面の外') for f in info['frames']))
    return '\n'.join(L) + '\n'


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('layouts', nargs='+')
    ap.add_argument('--out-dir', default=LANE_DIR)
    ap.add_argument('--img', action='store_true')
    ap.add_argument('--md', default=None)
    ap.add_argument('--allow-missing-ui', action='store_true', help='基準の撮影の UI の矩形が無くても続ける (警告だけ)')
    a = ap.parse_args()
    global ALLOW_MISSING_UI
    ALLOW_MISSING_UI = a.allow_missing_ui
    os.makedirs(a.out_dir, exist_ok=True)
    results = []
    for fp in a.layouts:
        L = json.load(open(fp))
        tag = os.path.splitext(os.path.basename(fp))[0]
        try:
            r = check(L, want_img=a.img, out_dir=a.out_dir, tag=tag)
        except MissingUIError as e:
            print('止めた: %s (--allow-missing-ui で続ける)' % e, file=sys.stderr)
            for f in e.files:
                print('  無い: ' + f, file=sys.stderr)
            return 1
        results.append(r)
        with open(os.path.join(a.out_dir, 'place2-%s.json' % tag), 'w') as f:
            json.dump(r, f, ensure_ascii=False, indent=1)
        print('%s: 部品 %d・小札 %d・材質 %d・違反 %d' % (tag, r['parts'], r['litter'], r['materials'], len(r['violations'])))
        for v in r['violations'][:40]:
            print('   ' + v)
    if a.md:
        with open(a.md, 'w') as f:
            f.write(summary_md(results))
    return 0 if all(not r['violations'] for r in results) else 1


if __name__ == '__main__':
    sys.exit(main())

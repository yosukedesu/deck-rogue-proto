#!/usr/bin/env python3
"""HD-2D 段2 幕2「先代の坑道」の物差し (2026-10-03 レーン F。計画 docs/design/hd2d-stage2-plan-2026-10-02.md §1・説明 docs/design/hd2d-stage2/measure.md)。

合否は目で決める (裁定)。ここの数字は「どの層が足りないか」を言葉にする補助。幕1 の物差し (scripts/hd2d-r3-targets.py・gates.json) は触らない。
目標の画は本家 ot_921570_2 (松明の洞窟の戦闘 = 帯型。~/.cache/deck-rogue/hd2d-ref/steam)。本家の値は ~/.cache/deck-rogue/hd2d-stage2/targets/metrics.json
(A2-1) を横に並べ、同じ式で本家の画も測り直す (列「本家 ot2 (測り直し)」と「本家 ot2 (metrics.json)」が食い違わないことが物差しの確かめ)。

輝度 L = 0.2126R + 0.7152G + 0.0722B (sRGB の値 0〜255。targets の measure.py・hd2d-measure.py と同じ)。
舞台の画素 = UI なしの画 (<場面>-hideui-1.png) の全画素 (本家は metrics.json の UI の矩形を除く = measure.py と同じ)。
キャラの型 = <場面>-unitsonly-1.png (マゼンタ以外) を 4px 太らせた型。無ければ同じフォルダの unitsonly から借りる (同じキャラ・同じ板 ±0.6px・
  同じ画面・同じ時刻 ±0.05 秒 = DET=1 の撮影は場面をまたいで同じ画素。hd2d-r3-targets.py と同じ借り方)。借りられなければ「近似」=
  キャラを除かずに測り、主人公の体の物差しは合否なし (≈)。本家は scripts/hd2d-r2-targets.py の REFS の矩形。
長さは PC の px (1920×1080・座席で 1 ドット = 4px)。スマホ相当 (1920×886) は 主人公の 1 ドットの px ÷ 4 = sc で縮める (本家は H/1080)。

物差し (★ 合否 = 計画 §1 の光の 3 条件・◆ 歯止め・— 参考 = 数字だけ)
  ★ L1 主人公の列の最大が壁にあり床の 1.15 倍以上: 主人公の足元の x ±125px の列で、行 10px ごとの平均 (UI だけ除く・キャラは除かない) の
       最大が「壁 = 足元の 60px より上」にあり、その値 ÷「床 = 足元の 20px 上〜140px 下の最大」≥ 1.15。本家 ot2 は 139 ÷ 118 = 1.18
       (反証 facts の verify_measure.py の「hero col」と同じ列・同じ 10 行 = 計画の 1.15 はこの測り方から出た。キャラを除いた列は excl に出す。
        本家でキャラを除くと 1.12 に下がる = 除いた列で 1.15 を当てると本家も落ちる)
  ★ L2 壁の上段 ≤ 60: 足元の 576〜416px 上 (PC の主人公の足元 行 706 で 行 130〜290 = 計画 §1 の字) × 主人公の x+150px〜右端の 60px 手前、
       キャラを除いた中央値。字のままの窓 (行 130〜290 × x 600〜1400) の中央値も rows130 に出す。スマホは上部バーの裏になるので参考
  ★ L3 灯の真下の床は無彩色 (彩度 ≤ 0.12): 舞台の灯 (layout.json の extra.look.lights の Spot) の向きが床 (y 0) に当たる点を画面に写し、
       半径 45px の窓 (キャラを除く) の HSV の彩度の中央値 (反証 score の「座席の床の彩度」と同じ = 窓の全画素の中央値)。灯の記録が無ければ座席の帯で
       いちばん明るい 40px の升の窓。本家は主人公の足元の窓 (x ±75・足元の 5px 上〜75px 下 = 反証 score の floor-hero-foot) で 0.09
       (明るい側 = 60 百分位より上だけの中央値は brightSat に出す。本家 0.13 = 松明の近くは少し暖かい)
       点光源 (lights[] の提灯など) の床 (y 0 と仮定) の窓も lamps に並べる (参考)
  ☆ L1w 壁の最大 ≥ 120 (目安。分析書 §11-1「実機で提灯 2＋壁で 120 以上に届くか」・本家 139。L1 は比だけなので暗い画でも通る = 今の舞台の幕2 は 56 ÷ 30 で通る)
  ◆ G1 p95 ≤ 174 (舞台の全画素。本家 ot2 173.9)
  ◆ G2 主人公の体 ≥ 床: 主人公の型 (2px 内側) の輝度の中央値 ≥ 足元の床 (足元の 10px 上〜60px 下 × x ±120・キャラを除く) の中央値
  ◆ G3 紙の UI がいちばん明るくならない: UI ありの画 (<場面>-1.png) と UI なしの画の差 (>12) を UI とし、手札の外の UI の面の
       中央値 ≤ 帯の頂点 (L1 の壁の最大)。手札 (紙) は除く (舞台の上の札は夜色 = 2026-09-30 の裁定)。p95 と「帯より明るい UI の画素/万」は参考
       (三周目の U3・U4 と同じ。字の画素が入るので p95 は帯を超えるのが普通)
  ★ W1 暖色の割合 2〜6%: 色相 10〜60°・彩度 ≥0.3 の画素の割合 (反証 score の vmeasure.py・模型 A の mock_act2.py と同じ。本家 0.3%・計画は 2〜5%・上限 6%)
  — 中央値・暗い画素 (<60)・明るい画素 (>150)・光の色相・暗部の色相 (measure.py と同じ。本家 ot2 は 67.7・0.448・0.080。露出で合わせない)
  — 帯 (全幅): 行ごとの平均 (UI を除く・幅の 30% 以上が舞台の行だけ) を画面の高さの 3% の窓でならした最大 (足元の 3% より上 = measure.py の peak_up)。
       帯の高さは「足元から何 px 上」(PC の px)。本家 ot2 は 行 585・105.5・足元から 180px
  — 箱庭の門 (layout.json の extra.diorama: ok・部品・材質・failures)

使い方
  python3 scripts/hd2d-a2-targets.py <撮影のフォルダ> <場面> [<場面> …] [--ref ot2] [--md 表.md] [--json 結果.json]
      場面は pshots の名前 (PC-T2-squire など。-hideui・-unitsonly・-uionly を付けても同じ場面)。
  python3 scripts/hd2d-a2-targets.py --all <撮影のフォルダ> [--ref ot2]       # フォルダの場面を全部
  python3 scripts/hd2d-a2-targets.py --image <画.png> [--layout <.layout.json>] [--units <unitsonly.png>] [--ui <UI ありの画.png>]
  python3 scripts/hd2d-a2-targets.py --ref ot2 [--ref mine]                    # 本家だけ (ot2・mine = derelict_mine・metrics.json の A2-* の名前も可)
  共通: [--metrics ~/.cache/deck-rogue/hd2d-stage2/targets/metrics.json] [--ref-dir …] [--quiet]
"""
import argparse
import colorsys
import importlib.util
import json
import math
import os
import sys
import warnings

import numpy as np
from PIL import Image

sys.dont_write_bytecode = True
HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(HERE)
CACHE = os.path.expanduser('~/.cache/deck-rogue')
DEFAULT_METRICS = os.path.join(CACHE, 'hd2d-stage2', 'targets', 'metrics.json')
REF_DIRS = [os.path.join(CACHE, 'hd2d-ref', 'steam'), os.path.join(CACHE, 'hd2d-ref'), os.path.join(CACHE, 'hd2d-stage2', 'targets', 'web')]

_spec = importlib.util.spec_from_file_location('hd2d_r2_targets', os.path.join(HERE, 'hd2d-r2-targets.py'))
T = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(T)   # lum・hsv・cam_project・REFS (本家の主人公とキャラの矩形) を借りる

# 本家の呼び名 → metrics.json の鍵・r2-targets の REFS の鍵 (キャラの矩形)
REF_ALIAS = {'ot2': ('A2-1', 'ot2'), 'mine': ('A2-2', None), 'study': ('A2-3', None), 'quarry': ('A2-4', None), 'tunnel': ('A2-5', None),
             'tomb': ('A3-1', None), 'tombdungeon': ('A3-2', None), 'everhold': ('A3-3', None), 'ot7': ('A3-4', 'ot7'),
             'sinking': ('A3-5', None), 'ot16': ('A1-ref', 'ot16')}
SUFFIXES = ('-hideui', '-unitsonly', '-uionly')

# 長さ (PC の px)。スマホ・本家は sc を掛ける
COL_HALF = 125        # 主人公の列の半幅
COL_STEP = 10         # 行の刻み
WALL_ABOVE = 60       # 壁 = 足元のこれより上
FLOOR_UP, FLOOR_DOWN = 20, 140   # 床 = 足元の 20px 上〜140px 下
UPPER = (576, 416)    # 壁の上段 = 足元の 576〜416px 上 (足元 706 で 行 130〜290)
POOL_R = 45           # 灯の真下の窓の半径
BAND_RATIO = 1.15

# 物差しの表 (id, 鍵, 見出し, 目標 [下, 上], 分類, PH の目標 or None = PH は参考)
CHECKS = [
    dict(id='L1', key='light.L1.ok', label='主人公の列の最大が壁で床の 1.15 倍以上', cls='合否', fmt='bool'),
    dict(id='L1r', key='light.L1.ratio', label='  壁の最大 ÷ 床の最大 (≥1.15)', rng=[BAND_RATIO, None], cls='合否'),
    dict(id='L1w', key='light.L1.wallPeak', label='  壁の最大 (主人公の列・10 行の平均)', rng=[120, None], cls='目安'),
    dict(id='L1h', key='light.L1.wallAboveFeet', label='  その行は足元から何 px 上 (PC の px)', cls='参考'),
    dict(id='L1f', key='light.L1.floorPeak', label='  床の最大 (足元の 20px 上〜140px 下)', cls='参考'),
    dict(id='L1x', key='light.L1.excl.ratio', label='  キャラを除いた列の 壁 ÷ 床 (参考)', cls='参考'),
    dict(id='L2', key='light.L2.median', label='壁の上段の中央値 (足元の 576〜416px 上・主人公の右)', rng=[None, 60], cls='合否', ph=False),
    dict(id='L2b', key='light.L2.rows130', label='  字のままの窓 (行 130〜290 × x 600〜1400) の中央値', cls='参考'),
    dict(id='L3', key='light.L3.sat', label='灯の真下の床の彩度 (HSV・窓の中央値)', rng=[None, 0.12], cls='合否'),
    dict(id='L3l', key='light.L3.lum', label='  その窓の明るさ (中央値)', cls='参考'),
    dict(id='L3b', key='light.L3.brightSat', label='  その窓の明るい側 (60 百分位より上) の彩度', cls='参考'),
    dict(id='G1', key='guard.p95', label='舞台の p95', rng=[None, 174], cls='歯止め'),
    dict(id='G2', key='guard.bodyMinusFloor', label='主人公の体 − 足元の床 (≥0)', rng=[0, None], cls='歯止め'),
    dict(id='G3', key='guard.uiFaceOverBand', label='手札の外の UI の面の中央値 ÷ 帯の頂点 (≤1)', rng=[None, 1.0], cls='歯止め'),
    dict(id='W1', key='color.warm', label='暖色の割合 (色相 10〜60°・彩度 ≥0.3)', rng=[0.02, 0.06], cls='合否'),
    dict(id='M1', key='global.median', label='中央値 (舞台の全画素)', cls='参考'),
    dict(id='M2', key='global.dark60', label='暗い画素 (<60) の割合', cls='参考'),
    dict(id='M3', key='global.bright150', label='明るい画素 (>150) の割合', cls='参考'),
    dict(id='B1', key='band.peakUpLum', label='帯 (全幅・3% 窓) の頂点の明るさ', cls='参考'),
    dict(id='B2', key='band.aboveFeet', label='帯 (全幅) の頂点は足元から何 px 上 (PC の px)', cls='参考'),
    dict(id='C1', key='color.shadowHue', label='暗部の色 (20<L<60 の平均の色相・彩度・RGB)', cls='参考', fmt='hue'),
    dict(id='C2', key='color.lightHue', label='光の色 (L>120 の平均の色相・彩度・RGB)', cls='参考', fmt='hue'),
    dict(id='G2b', key='guard.heroBody', label='  主人公の体の中央値', cls='参考'),
    dict(id='G3b', key='guard.uiP95', label='  手札の外の UI の p95 (字を含む・参考)', cls='参考'),
    dict(id='D1', key='diorama.ok', label='箱庭の門 (Diorama.Check の result)', cls='参考', fmt='bool'),
]
CLS_MARK = {'合否': '★', '目安': '☆', '歯止め': '◆', '参考': '—'}
APPROX_KEYS = {'guard.bodyMinusFloor', 'guard.heroBody'}   # 主人公の型が近似の時は合否なし


# ---- 小道具

def rnd(v, n=3):
    return T.rnd(v, n)


def box_any(m, r):
    """m を一辺 2r+1 の正方形で太らせる (積分画像)"""
    if r <= 0:
        return m.copy()
    H, W = m.shape
    ii = np.zeros((H + 1, W + 1), np.int32)
    ii[1:, 1:] = np.cumsum(np.cumsum(m.astype(np.int32), 0), 1)
    y0 = np.clip(np.arange(H) - r, 0, H); y1 = np.clip(np.arange(H) + r + 1, 0, H)
    x0 = np.clip(np.arange(W) - r, 0, W); x1 = np.clip(np.arange(W) + r + 1, 0, W)
    s = ii[y1][:, x1] - ii[y0][:, x1] - ii[y1][:, x0] + ii[y0][:, x0]
    return s > 0


def box_count(m, r):
    H, W = m.shape
    ii = np.zeros((H + 1, W + 1), np.int32)
    ii[1:, 1:] = np.cumsum(np.cumsum(m.astype(np.int32), 0), 1)
    y0 = np.clip(np.arange(H) - r, 0, H); y1 = np.clip(np.arange(H) + r + 1, 0, H)
    x0 = np.clip(np.arange(W) - r, 0, W); x1 = np.clip(np.arange(W) + r + 1, 0, W)
    return ii[y1][:, x1] - ii[y0][:, x1] - ii[y1][:, x0] + ii[y0][:, x0]


def erode(m, r):
    return ~box_any(~m, r)


def rect_mask(H, W, rects, pad=0):
    m = np.zeros((H, W), bool)
    for x, y, w, h in rects:
        x0, y0 = int(round(x)) - pad, int(round(y)) - pad
        x1, y1 = int(round(x + w)) + pad, int(round(y + h)) + pad
        m[max(0, y0):max(0, y1), max(0, x0):max(0, x1)] = True
    return m


def load_json(p):
    try:
        return json.load(open(p, encoding='utf-8')) if p and os.path.exists(p) else None
    except Exception:  # noqa
        return None


def hue_of(px):
    """平均の色: [色相°, 彩度, [R,G,B]] (measure.py と同じ)"""
    if len(px) < 50:
        return None
    m = px.mean(0) / 255
    h, s, _ = colorsys.rgb_to_hsv(*m)
    return [int(round(h * 360)), round(float(s), 2), [int(x * 255) for x in m]]


def sat_of(a):
    mx = a.max(-1); mn = a.min(-1)
    return np.where(mx > 0, (mx - mn) / np.maximum(mx, 1e-6), 0)


def warm_frac(a, valid):
    """色相 10〜60°・彩度 ≥0.3 の画素の割合 (vmeasure.py・mock_act2.py と同じ式。色相は atan2 で)"""
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    hue = np.degrees(np.arctan2(math.sqrt(3) * (g - b), 2 * r - g - b)) % 360
    s = sat_of(a)
    w = (hue >= 10) & (hue <= 60) & (s >= 0.3) & valid
    return float(w.sum() / max(1, valid.sum()))


# ---- 1枚の場面

class Scene:
    """測る場面 1 つ。a = 舞台の画 (RGB float)・ui = UI の型 (舞台から除く)・chars = キャラの型 (太らせた)・hero = 主人公の型"""

    def __init__(self):
        self.notes = []
        self.lay = None
        self.chars = None
        self.hero = None
        self.heroExact = False
        self.uiImg = None
        self.uiLay = None
        self.src = {}


def unit_boxes(lay):
    return {u['key']: u for u in (((lay or {}).get('stage') or {}).get('unitBoxes') or [])}


def unit_sig(lay, key):
    """キャラ1体の目印 (同じキャラ・同じ板・同じ1ドットの px・同じ画面・同じ時刻なら同じ絵 = DET=1)"""
    u = unit_boxes(lay).get(key)
    if not u:
        return None
    uid = next((x.get('id') for x in (lay.get('units') or []) if x.get('key') == key), None) or key
    return dict(id=uid, raw=u.get('boardRawPx') or u.get('boardPx'), board=u.get('boardPx'), ppd=u.get('pxPerDot'),
                screen=((lay.get('screen') or {}).get('w'), (lay.get('screen') or {}).get('h')), time=lay.get('time'))


def same_sig(a, b):
    if not a or not b or a['id'] != b['id'] or a['screen'] != b['screen'] or a['ppd'] != b['ppd']:
        return False
    for k in ('raw', 'board'):
        if not a[k] or not b[k] or max(abs(p - q) for p, q in zip(a[k], b[k])) > 0.6:
            return False
    ta, tb = a.get('time'), b.get('time')
    return ta is None or tb is None or abs(ta - tb) <= 0.05


def split_units(png, lay):
    """unitsonly の画をキャラごとの型に分ける {key: 型}。画素は板 (boardRawPx) に入るキャラのうち、絵の矩形の中心がいちばん近い物へ"""
    u = np.asarray(Image.open(png).convert('RGB'))
    H, W = u.shape[:2]
    ch = ~((u[..., 0] > 240) & (u[..., 1] < 20) & (u[..., 2] > 240))
    ub = unit_boxes(lay)
    keys = [k for k in ub if ub[k].get('boardRawPx') or ub[k].get('boardPx')]
    ys, xs = np.nonzero(ch)
    best = np.full(len(ys), -1); bestd = np.full(len(ys), np.inf)
    for i, k in enumerate(keys):
        x, y, w, h = [int(round(v)) for v in (ub[k].get('boardRawPx') or ub[k].get('boardPx'))]
        inside = (xs >= x) & (xs < x + w) & (ys >= y) & (ys < y + h)
        bx, by, bw, bh = ub[k].get('boardPx') or (x, y, w, h)
        d = np.hypot(xs - (bx + bw / 2), ys - (by + bh / 2))
        sel = inside & (d < bestd)
        best[sel] = i; bestd[sel] = d[sel]
    out = {}
    for i, k in enumerate(keys):
        m = np.zeros((H, W), bool); sel = best == i
        m[ys[sel], xs[sel]] = True
        out[k] = m
    return ch, out


def scene_base(folder, name):
    for suf in SUFFIXES:
        if name.endswith(suf):
            b = name[:-len(suf)]
            if any(os.path.exists(os.path.join(folder, b + q)) for q in ('-1.png', '-hideui-1.png', '-unitsonly-1.png')):
                return b
    return name


def is_hideui(lay):
    return bool(lay) and 'hideui=1' in (lay.get('state') or '')


def resolve(folder, name):
    """場面の画: stage = UI なしの画・ui = UI ありの画・units = unitsonly。uiOnly = UI ありの画しか無い (舞台の物差しは合否なし)"""
    base = scene_base(folder, name)
    p = lambda suf: os.path.join(folder, base + suf)   # noqa: E731
    ex = lambda q: q if os.path.exists(q) else None    # noqa: E731
    stage = stage_lay = ui = ui_lay = None
    h = ex(p('-hideui-1.png'))
    if h:
        stage, stage_lay = h, ex(p('-hideui-1.layout.json'))
    one = ex(p('-1.png')); one_lay = ex(p('-1.layout.json'))
    if one:
        if is_hideui(load_json(one_lay)):
            if not stage:
                stage, stage_lay = one, one_lay
        else:
            ui, ui_lay = one, one_lay
    if not stage and not ui:
        raise SystemExit('画が無い: %s/%s-hideui-1.png か -1.png' % (folder, base))
    return dict(base=base, stage=stage or ui, stageLay=stage_lay if stage else ui_lay, ui=ui, uiLay=ui_lay,
                units=ex(p('-unitsonly-1.png')), unitsLay=ex(p('-unitsonly-1.layout.json')), uiOnly=stage is None)


def layout_ui_mask(lay, H, W):
    """layout.json の UI の矩形 (葉の node。キャラの絵・背景・全画面の入れ物・当たり判定は除く。hd2d-r3-targets と同じ)"""
    m = np.zeros((H, W), bool)
    nodes = (lay or {}).get('nodes') or []
    paths = [n.get('path') or '' for n in nodes]
    for n, pth in zip(nodes, paths):
        if not pth.startswith('screen/') or '/sprite' in pth or pth.endswith('/bg') or 'catch' in pth:
            continue
        pre = pth + '/'
        if any(q.startswith(pre) for q in paths):
            continue
        x, y, w, h = n.get('px') or (0, 0, 0, 0)
        if w * h <= 0 or w * h > 0.25 * W * H:
            continue
        m |= rect_mask(H, W, [[x, y, w, h]])
    return m


def hero_feet(lay):
    """主人公の足元 (x, y) の画面の px。座席の記録 (stage.camera.seats) が無ければ板の矩形の下端"""
    cam = ((lay or {}).get('stage') or {}).get('camera') or {}
    for s in cam.get('seats') or []:
        if s.get('key') == 'player' and s.get('px'):
            return float(s['px'][0]), float(s['px'][1])
    u = unit_boxes(lay).get('player')
    if u:
        x, y, w, h = u.get('boardPx') or u.get('boardRawPx')
        return x + w / 2, y + h - (u.get('feetPadPx') or 0)
    for u in (lay or {}).get('units') or []:
        if u.get('kind') == 'player':
            px = u['px']
            return px[0] + px[2] / 2, px[1] + px[3] - (u.get('feetOffset') or 0)
    return None


def all_feet_rows(lay):
    cam = ((lay or {}).get('stage') or {}).get('camera') or {}
    return [float(s['px'][1]) for s in cam.get('seats') or [] if s.get('px')]


def borrow_units(folder, lay, keys):
    """同じフォルダの unitsonly からキャラの型を借りる {key: (型, 名前)}"""
    out = {}
    if not folder or not os.path.isdir(folder):
        return out
    for p in sorted(os.listdir(folder)):
        if not p.endswith('-unitsonly-1.png'):
            continue
        q = os.path.join(folder, p); dl = load_json(q[:-4] + '.layout.json')
        if not dl:
            continue
        need = [k for k in keys if k not in out and same_sig(unit_sig(lay, k), unit_sig(dl, k))]
        if not need:
            continue
        _, parts = split_units(q, dl)
        for k in need:
            if k in parts and parts[k].sum() > 50:
                out[k] = (parts[k], p[:-len('-unitsonly-1.png')])
    return out


def make_scene(stage_png, stage_lay=None, units_png=None, ui_png=None, ui_lay=None, folder=None, name=None, ui_only=False):
    s = Scene()
    s.kind = 'ours'
    s.name = name or os.path.basename(stage_png)[:-4]
    s.path = stage_png
    s.a = T.load(stage_png); s.H, s.W = s.a.shape[:2]
    s.lay = load_json(stage_lay) or load_json(stage_png[:-4] + '.layout.json')
    s.dev = 'PH' if s.H < 1000 else 'PC'
    ub = unit_boxes(s.lay)
    ppd = (ub.get('player') or {}).get('pxPerDot')
    s.sc = (ppd / 4.0) if ppd else (s.H / 1080.0)
    f = hero_feet(s.lay)
    if f is None:
        f = (s.W * 0.23, s.H * 0.654); s.notes.append('主人公の足元の記録が無い = 画面の 23%・65% と仮定')
    s.feet = f
    s.feetRows = all_feet_rows(s.lay) or [f[1]]
    s.uiOnly = ui_only
    s.ui = layout_ui_mask(s.lay, s.H, s.W) if ui_only else np.zeros((s.H, s.W), bool)
    s.src = dict(stage=os.path.basename(stage_png), units=None, donor=None, ui=os.path.basename(ui_png) if ui_png else None)
    # キャラの型
    keys = [k for k in ub if ub[k].get('boardRawPx') or ub[k].get('boardPx')]
    parts = {}
    if units_png and os.path.exists(units_png):
        ul = load_json(units_png[:-4] + '.layout.json') or s.lay
        ch, parts = split_units(units_png, ul)
        s.src['units'] = os.path.basename(units_png)
    if keys and any(k not in parts for k in keys):
        got = borrow_units(folder or os.path.dirname(os.path.abspath(stage_png)), s.lay, [k for k in keys if k not in parts])
        if got:
            for k, (m, donor) in got.items():
                parts[k] = m
            s.src['donor'] = sorted(set(d for _, d in got.values()))
    if parts:
        allm = np.zeros((s.H, s.W), bool)
        for m in parts.values():
            if m.shape == allm.shape:
                allm |= m
        s.chars = box_any(allm, max(1, int(round(4 * s.sc))))
        s.charsExact = all(k in parts for k in keys)
        if 'player' in parts and parts['player'].sum() > 50:
            s.hero = parts['player']; s.heroExact = True
    if s.hero is None and 'player' in ub:
        x, y, w, h = [int(round(v)) for v in (ub['player'].get('boardPx') or ub['player'].get('boardRawPx'))]
        hm = np.zeros((s.H, s.W), bool); hm[y + int(0.25 * h):y + int(0.75 * h), x + int(0.3 * w):x + int(0.7 * w)] = True
        s.hero = hm; s.heroExact = False
        s.notes.append('主人公の型が無い = 体は絵の矩形の真ん中 (近似・合否なし)')
    if s.chars is None:
        s.notes.append('キャラの型が無い = キャラを除かずに測った (近似)')
    s.uiImg = ui_png; s.uiLay = ui_lay
    return s


def ref_scene(key, metrics, ref_dirs):
    """本家 (metrics.json の UI の矩形と主人公の足元・r2-targets の REFS のキャラの矩形)"""
    mk, rk = REF_ALIAS.get(key, (key, None))
    m = (metrics or {}).get(mk)
    if not m:
        raise SystemExit('metrics.json に %s (%s) が無い' % (key, mk))
    path = None
    for d in ref_dirs:
        for nm in (m['name'] + '.jpg', 'ot1web_' + m['name'] + '.jpg', m['name'] + '.png'):
            q = os.path.join(d, nm)
            if os.path.exists(q):
                path = q; break
        if path:
            break
    if not path:
        raise SystemExit('本家の画が無い: %s (%s)' % (m['name'], ', '.join(ref_dirs)))
    s = Scene()
    s.kind = 'ref'; s.name = key; s.path = path; s.metricsKey = mk; s.metrics = m
    s.a = T.load(path); s.H, s.W = s.a.shape[:2]
    s.dev = 'PC'; s.sc = s.H / 1080.0
    s.feet = tuple(float(v) for v in m['hero_feet'])
    s.feetRows = [s.feet[1]]
    s.ui = rect_mask(s.H, s.W, m.get('ui_rects') or [])
    s.uiOnly = False
    s.src = dict(stage=os.path.basename(path))
    R = T.REFS.get(rk) if rk else None
    if R:
        s.chars = rect_mask(s.H, s.W, R['chars'])
        s.hero = rect_mask(s.H, s.W, [R['hero']]); s.heroExact = True
        s.charsExact = True
    else:
        s.notes.append('本家のキャラの矩形が無い = キャラを除かずに測った')
    return s


# ---- 測る

def measure(s):
    warnings.simplefilter('ignore', RuntimeWarning)
    H, W, sc = s.H, s.W, s.sc
    a = s.a; L = T.lum(a)
    valid = ~s.ui                                      # 全画素 (UI を除く) = measure.py
    stage = valid & (~s.chars if s.chars is not None else True)   # 舞台 (キャラも除く)
    fx, fy = s.feet
    r = dict(name=s.name, kind=s.kind, dev=s.dev, path=s.path, size=[W, H], sc=rnd(sc), feet=[rnd(fx, 1), rnd(fy, 1)],
             notes=list(s.notes), uiOnly=bool(s.uiOnly), src=dict(s.src), heroExact=bool(s.heroExact),
             charsExact=bool(getattr(s, 'charsExact', False)))
    if s.lay:
        r['state'] = s.lay.get('state')
        dio = (s.lay.get('extra') or {}).get('diorama') or {}
        if dio:
            r['diorama'] = dict(ok=dio.get('ok'), layout=dio.get('layout'), parts=dio.get('parts'), materials=dio.get('materials'),
                                failures=dio.get('failures'), byKind=dio.get('byKind'), gates=dio.get('gates'))
        look = (s.lay.get('extra') or {}).get('look') or {}
        if look:
            r['look'] = dict(name=look.get('name'), sources=look.get('sources'))
    # ---- 全画素 (measure.py と同じ)
    v = L[valid]
    r['global'] = dict(median=rnd(float(np.median(v)), 1), dark60=rnd(float((v < 60).mean())), bright150=rnd(float((v > 150).mean())),
                       p95=rnd(float(np.percentile(v, 95)), 1))
    av = a[valid]; lv = v
    r['color'] = dict(warm=rnd(warm_frac(a, valid), 4), lightHue=hue_of(av[lv > 120]), shadowHue=hue_of(av[(lv > 20) & (lv < 60)]))
    # ---- 帯 (全幅・3% 窓 = measure.py の peak_up)
    rowsum = np.where(valid, L, 0).sum(1); cnt = valid.sum(1)
    rowmean = np.where(cnt > W * 0.3, rowsum / np.maximum(cnt, 1), np.nan)
    k = max(15, int(H * 0.03)); rm = np.convolve(np.nan_to_num(rowmean, nan=0.0), np.ones(k) / k, 'same')
    y0 = int(H * 0.02); ya = max(y0 + 1, int(fy - H * 0.03))
    pu = int(np.argmax(rm[y0:ya])) + y0
    pk = int(np.argmax(rm[y0:int(H * 0.9)])) + y0
    r['band'] = dict(peakUpRow=pu, peakUpLum=rnd(float(rm[pu]), 1), aboveFeet=rnd((fy - pu) / sc, 0),
                     peakRow=pk, peakLum=rnd(float(rm[pk]), 1), window=k)
    # ---- 光の 3 条件
    light = {}
    light['L1'] = l1_column(s, L, valid, stage)
    light['L2'] = l2_upper(s, L, stage)
    light['L3'] = l3_pool(s, a, L, stage)
    r['light'] = light
    # ---- 歯止め
    g = dict(p95=r['global']['p95'])
    body = floor = None
    if s.hero is not None:
        hb = erode(s.hero, 2) if s.heroExact else s.hero
        if hb.sum() > 30:
            body = float(np.median(L[hb]))
    fw = rect_mask(H, W, [[fx - 120 * sc, fy - 10 * sc, 240 * sc, 70 * sc]]) & stage
    if fw.sum() > 100:
        floor = float(np.median(L[fw]))
    g.update(heroBody=rnd(body, 1), heroFloor=rnd(floor, 1), bodyMinusFloor=rnd(body - floor, 1) if (body is not None and floor is not None) else None)
    band = light['L1'].get('wallPeak')
    if s.kind != 'ref':
        u = ui_numbers(s, L, band)
        if u:
            g.update(u)
    r['guard'] = g
    return r


def column_profile(s, L, ok):
    """主人公の列 (足元 x ±125·sc) の 10 行ごとの平均 (ok の画素だけ・3 割以上ある行だけ)。[(行の頭, 平均 or nan)]"""
    H, W, sc = s.H, s.W, s.sc
    fx, fy = s.feet
    x0 = int(max(0, round(fx - COL_HALF * sc))); x1 = int(min(W, round(fx + COL_HALF * sc)))
    step = max(4, int(round(COL_STEP * sc)))
    yend = int(min(H, fy + FLOOR_DOWN * sc))
    out = []
    for y in range(0, yend, step):
        blk = L[y:y + step, x0:x1]; m = ok[y:y + step, x0:x1]
        out.append((y, float(blk[m].mean()) if m.sum() >= 0.3 * m.size else float('nan')))
    return out, step, (x0, x1)


def l1_column(s, L, valid, stage):
    sc = s.sc; fx, fy = s.feet
    prof, step, xs = column_profile(s, L, valid)    # 判定 = キャラを除かない列 (verify_measure.py と同じ)
    excl, _, _ = column_profile(s, L, stage)        # 参考 = キャラを除いた列
    wall_end = fy - WALL_ABOVE * sc
    fl0, fl1 = fy - FLOOR_UP * sc, fy + FLOOR_DOWN * sc

    def peak(p, lo, hi):
        c = [(y, v) for y, v in p if np.isfinite(v) and y + step / 2 >= lo and y + step / 2 < hi]
        return max(c, key=lambda q: q[1]) if c else (None, None)
    wy, wv = peak(prof, 0, wall_end)
    fy_, fv = peak(prof, fl0, fl1)
    ay, av = peak(prof, 0, fl1)
    out = dict(cols=list(xs), step=step, wallRows=[0, rnd(wall_end, 0)], floorRows=[rnd(fl0, 0), rnd(fl1, 0)],
               wallPeak=rnd(wv, 1), wallPeakRow=wy, wallAboveFeet=rnd((fy - (wy + step / 2)) / sc, 0) if wy is not None else None,
               floorPeak=rnd(fv, 1), floorPeakRow=fy_, maxRow=ay,
               ratio=rnd(wv / fv, 3) if (wv and fv) else None,
               profile=[[y, rnd(v, 1)] for y, v in prof])
    out['maxOnWall'] = (ay is not None and ay + step / 2 < wall_end)
    out['ok'] = bool(out['maxOnWall'] and out['ratio'] is not None and out['ratio'] >= BAND_RATIO)
    # 半値幅 (壁の最大から床の最大までの間を地にして、地より上の分が半分以上の行の幅。参考)
    if wv and fv:
        half = fv + (wv - fv) / 2
        rows = [y for y, v in prof if np.isfinite(v) and v >= half and y + step / 2 < wall_end]
        out['halfWidth'] = rnd((max(rows) - min(rows) + step) / sc, 0) if rows else None
    # キャラを除いた列 (参考。本家はキャラの矩形・うちは unitsonly の型)
    if s.chars is not None:
        ewy, ewv = peak(excl, 0, wall_end); efy, efv = peak(excl, fl0, fl1)
        out['excl'] = dict(wallPeak=rnd(ewv, 1), wallPeakRow=ewy, floorPeak=rnd(efv, 1), ratio=rnd(ewv / efv, 3) if (ewv and efv) else None,
                           profile=[[y, rnd(v, 1)] for y, v in excl])
    return out


def l2_upper(s, L, stage):
    H, W, sc = s.H, s.W, s.sc
    fx, fy = s.feet
    y0 = int(max(0, fy - UPPER[0] * sc)); y1 = int(max(0, fy - UPPER[1] * sc))
    x0 = int(max(0, fx + 150 * sc)); x1 = int(min(W, W - 60 * sc))
    out = dict(rows=[y0, y1], cols=[x0, x1])
    m = np.zeros_like(stage); m[y0:y1, x0:x1] = True; m &= stage
    out['median'] = rnd(float(np.median(L[m])), 1) if m.sum() > 500 else None
    out['frac'] = rnd(float(m.sum()) / max(1, (y1 - y0) * max(1, x1 - x0)), 2)
    # 字のままの窓 (行 130〜290 × x 600〜1400。PC の画面の割合で写す)
    hs = H / 1080.0; ws = W / 1920.0
    m2 = np.zeros_like(stage); m2[int(130 * hs):int(290 * hs), int(600 * ws):int(1400 * ws)] = True; m2 &= stage
    out['rows130'] = rnd(float(np.median(L[m2])), 1) if m2.sum() > 500 else None
    if s.dev == 'PH':
        out['note'] = 'スマホは上部バーの裏 (参考)'
    return out


def project_floor(cam, P):
    try:
        x, y, d = T.cam_project(cam, P)
        return (float(x), float(y), float(d)) if d > 0 else None
    except Exception:  # noqa
        return None


def pool_window(s, a, L, stage, cx, cy, r):
    H, W = s.H, s.W
    m = np.zeros_like(stage)
    y0, y1 = int(max(0, cy - r)), int(min(H, cy + r)); x0, x1 = int(max(0, cx - r)), int(min(W, cx + r))
    if y1 - y0 < 4 or x1 - x0 < 4:
        return None
    m[y0:y1, x0:x1] = True; m &= stage
    if m.sum() < 60:
        return None
    lv = L[m]; thr = np.percentile(lv, 60)
    sel = m & (L >= thr)
    return dict(center=[rnd(cx, 0), rnd(cy, 0)], r=rnd(r, 0), sat=rnd(float(np.median(sat_of(a[m]))), 3), lum=rnd(float(np.median(lv)), 1),
                brightSat=rnd(float(np.median(sat_of(a[sel]))), 3), color=hue_of(a[m]))


def l3_pool(s, a, L, stage):
    sc = s.sc; fx, fy = s.feet
    r = POOL_R * sc
    out = dict(lamps=[])
    if s.kind == 'ref':
        w = rect_mask(s.H, s.W, [[fx - 75 * sc, fy - 5 * sc, 150 * sc, 80 * sc]]) & stage
        if w.sum() > 60:
            lv = L[w]; sel = w & (L >= np.percentile(lv, 60))
            out.update(source='本家の主人公の足元 (x ±75・足元の 5px 上〜75px 下)', sat=rnd(float(np.median(sat_of(a[w]))), 3),
                       lum=rnd(float(np.median(lv)), 1), brightSat=rnd(float(np.median(sat_of(a[sel]))), 3), color=hue_of(a[w]))
        return out
    cam = ((s.lay or {}).get('stage') or {}).get('camera') or {}
    lights = (((s.lay or {}).get('extra') or {}).get('look') or {}).get('lights') or []
    main = None
    for li in lights:
        typ = li.get('type'); pos = li.get('position'); fwd = li.get('forward')
        if not pos or not cam.get('fov') or li.get('enabled') is False or (li.get('intensity') or 0) <= 0 or 'HitLight' in (li.get('name') or ''):
            continue   # 消えている光・技の光 (待機中は 0) は数えない
        if typ == 'Spot' and fwd and fwd[1] < -0.05:
            t = -pos[1] / fwd[1]
            P = [pos[0] + fwd[0] * t, 0.0, pos[2] + fwd[2] * t]
            pr = project_floor(cam, P)
            if pr and 0 <= pr[0] < s.W and 0 <= pr[1] < s.H:
                w = pool_window(s, a, L, stage, pr[0], pr[1], r)
                if w:
                    w.update(name=li.get('name'), type=typ, floor=[rnd(q, 2) for q in P])
                    out['lamps'].append(w)
                    if main is None or 'StageLamp' in (li.get('name') or ''):
                        main = w
        elif typ == 'Point':
            P = [pos[0], 0.0, pos[2]]
            pr = project_floor(cam, P)
            if pr and 0 <= pr[0] < s.W and 0 <= pr[1] < s.H:
                w = pool_window(s, a, L, stage, pr[0], pr[1], r)
                if w:
                    w.update(name=li.get('name'), type=typ, floor=[rnd(q, 2) for q in P], note='床 y 0 と仮定 (参考)')
                    out['lamps'].append(w)
    if main is None:
        # 座席の帯でいちばん明るい 40px の升
        f0, f1 = min(s.feetRows), max(s.feetRows)
        y0 = int(max(0, f0 - 40 * sc)); y1 = int(min(s.H, f1 + 120 * sc)); x0 = int(150 * s.W / 1920); x1 = int(s.W - 150 * s.W / 1920)
        bs = max(8, int(round(40 * sc))); best = None
        for yy in range(y0, max(y0 + 1, y1 - bs), bs // 2):
            for xx in range(x0, max(x0 + 1, x1 - bs), bs // 2):
                m = stage[yy:yy + bs, xx:xx + bs]
                if m.sum() < 0.5 * m.size:
                    continue
                v = float(L[yy:yy + bs, xx:xx + bs][m].mean())
                if best is None or v > best[0]:
                    best = (v, xx + bs / 2, yy + bs / 2)
        if best:
            main = pool_window(s, a, L, stage, best[1], best[2], r)
            if main:
                main['name'] = '座席の帯でいちばん明るい升 (灯の記録が無い)'
    if main:
        out.update(source=main.get('name'), sat=main['sat'], lum=main['lum'], brightSat=main.get('brightSat'), center=main['center'], color=main.get('color'))
    return out


def ui_numbers(s, L, band):
    """UI ありの画があれば: UI = 双子の UI なしの画との差 (>12)・無ければ layout.json の UI の矩形。手札の外の UI の面の中央値・p95・帯より明るい画素"""
    if not s.uiImg or not os.path.exists(s.uiImg):
        return None
    u = T.load(s.uiImg)
    if u.shape != s.a.shape:
        return dict(uiNote='UI ありの画の大きさが違う')
    lay = load_json(s.uiLay) or load_json(s.uiImg[:-4] + '.layout.json') or {}
    st_u = (lay.get('state') or '').replace(';hideui=1', ''); st_s = ((s.lay or {}).get('state') or '').replace(';hideui=1', '')
    if s.lay and st_u == st_s and not s.uiOnly:
        m = np.abs(u - s.a).max(-1) > 12
        m &= box_count(m, 4) >= 12    # 1 コマ違いの塵の粒 (孤立した数画素) は UI でない
        src = 'UI なしの画との差'
    else:
        m = layout_ui_mask(lay, s.H, s.W); src = 'layout.json の UI の矩形'
    hand = np.zeros_like(m)
    for hd in lay.get('hand') or []:
        hand |= rect_mask(s.H, s.W, [hd['px']])
    Lu = T.lum(u)
    mo = m & ~hand
    out = dict(uiMaskSrc=src, uiCover=rnd(float(m.mean())))
    if mo.sum() > 100:
        face = float(np.median(Lu[mo])); p95 = float(np.percentile(Lu[mo], 95))
        out.update(uiFace=rnd(face, 1), uiP95=rnd(p95, 1))
        if band:
            out.update(uiFaceOverBand=rnd(face / band, 3), uiBrightOverBand=rnd(float((mo & (Lu > band)).sum()) / (s.H * s.W) * 1e4, 1))
    return out


# ---- 合否と表

def get_key(r, key):
    cur = r
    for k in key.split('.'):
        if not isinstance(cur, dict) or k not in cur:
            return None
        cur = cur[k]
    return cur


def judge_one(r, c):
    """(値, 合否 True/False/None, 合否なしの理由)"""
    v = get_key(r, c['key'])
    if c['cls'] == '参考' or v is None:
        return v, None, None
    if r.get('kind') in ('ref', 'metrics'):   # 本家 (測り直した列も metrics.json の列も) には合否を付けない
        return v, None, '本家'
    if r.get('dev') == 'PH' and c.get('ph') is False:
        return v, None, 'スマホは参考'
    if c['key'] in APPROX_KEYS and not r.get('heroExact'):
        return v, None, '近似'
    if r.get('uiOnly') and not c['key'].startswith('guard.ui'):
        return v, None, 'UI あり'
    if c.get('fmt') == 'bool':
        return v, bool(v), None
    rng = c.get('rng')
    if not rng:
        return v, None, None
    ok = (rng[0] is None or v >= rng[0]) and (rng[1] is None or v <= rng[1])
    return v, ok, None


def fmt_v(v, c=None):
    if v is None:
        return '—'
    if c and c.get('fmt') == 'hue':
        return '%d°・%.2f・%s' % (v[0], v[1], tuple(v[2])) if v else '—'
    if isinstance(v, bool):
        return 'はい' if v else 'いいえ'
    if isinstance(v, float):
        return ('%.3g' % v) if abs(v) < 10 else ('%.1f' % v if abs(v) < 1000 else '%.0f' % v)
    return str(v)


def fmt_rng(rng):
    if not rng:
        return '—'
    a, b = rng
    if a is not None and b is not None:
        return '%s〜%s' % (fmt_v(a), fmt_v(b))
    return ('≥%s' % fmt_v(a)) if a is not None else ('≤%s' % fmt_v(b))


def metrics_column(m, label='本家 ot2 (metrics.json)'):
    """metrics.json の本家の値を同じ鍵の形に (並べる用)"""
    return dict(name=label, kind='metrics', dev='PC', label=label,
                **{'global': dict(median=m.get('median'), dark60=m.get('dark_lt60'), bright150=m.get('bright_gt150'), p95=m.get('p95')),
                   'band': dict(peakUpRow=m.get('peak_up_row'), peakUpLum=m.get('peak_up_lum'), aboveFeet=m.get('band_up_above_feet_px1080')),
                   'color': dict(lightHue=m.get('light_hue'), shadowHue=m.get('shadow_hue')),
                   'guard': dict(p95=m.get('p95'))})


LEAD = ('★ 合否 = 計画 §1 の幕2 の光の 3 条件と暖色・☆ 目安・◆ 歯止め・— 参考。合否は目で決める (裁定)。数字は「どの層が足りないか」の補助。'
        '○ = 入った・× = 外れた・≈ = 合否を付けない (本家・スマホの参考・主人公の型が近似・UI ありの画だけ)。説明は docs/design/hd2d-stage2/measure.md。')


def md_table(results, title='幕2 の物差し (scripts/hd2d-a2-targets.py)', lead=None):
    head = '| 物差し | 分類 | 目標 | ' + ' | '.join(r.get('label', r['name']) for r in results) + ' |'
    rows = ['# ' + title, '', lead or LEAD, '', head, '|' + '---|' * (3 + len(results))]
    rows.append('| 舞台を測った画 | | | %s |' % ' | '.join((r.get('src') or {}).get('stage') or '—' for r in results))
    rows.append('| キャラの型 | | | %s |' % ' | '.join(units_text(r) for r in results))
    for c in CHECKS:
        cells = []
        for r in results:
            v, ok, why = judge_one(r, c)
            t = fmt_v(v, c)
            if ok is not None:
                t += ' ○' if ok else ' ×'
            elif why and why != '本家' and v is not None:
                t += ' ≈'
            cells.append(t)
        rows.append('| %s %s %s | %s | %s | %s |' % (CLS_MARK[c['cls']], c['id'], c['label'], c['cls'], fmt_rng(c.get('rng')) if c.get('fmt') != 'bool' else 'はい',
                                                     ' | '.join(cells)))
    tally = []
    for r in results:
        js = [judge_one(r, c)[1] for c in CHECKS]
        tally.append('%d/%d' % (sum(1 for q in js if q), sum(1 for q in js if q is not None)) if r.get('kind') == 'ours' else '—')
    rows.append('| 合否・目安・歯止めの ○ の数 | | | %s |' % ' | '.join(tally))
    notes = [n for r in results for n in ('%s: %s' % (r.get('label', r['name']), q) for q in r.get('notes', []))]
    if notes:
        rows += [''] + ['- ' + n for n in notes]
    return '\n'.join(rows) + '\n'


def units_text(r):
    if r.get('kind') == 'metrics':
        return '—'
    if r.get('kind') == 'ref':
        return '本家の矩形' if r.get('heroExact') else 'なし'
    src = r.get('src') or {}
    if src.get('units'):
        return src['units']
    if src.get('donor'):
        return '借りた (%s)' % ', '.join(src['donor'])
    return '近似 (無い)'


def text_report(r):
    lines = ['== %s (%s・%s) 足元 %s・sc %s・キャラの型 %s' % (r.get('label', r['name']), r['kind'], r['dev'], r.get('feet'), r.get('sc'), units_text(r))]
    for n in r.get('notes', []):
        lines.append('  注: ' + n)
    for c in CHECKS:
        v, ok, why = judge_one(r, c)
        mark = '' if ok is None else (' ○' if ok else ' ×')
        if ok is None and why and v is not None and c['cls'] != '参考':
            mark = ' ≈ (%s)' % why
        lines.append('  %s %-4s %-44s %-24s 目標 %s%s' % (CLS_MARK[c['cls']], c['id'], c['label'], fmt_v(v, c), fmt_rng(c.get('rng')), mark))
    l3 = get_key(r, 'light.L3') or {}
    for lp in l3.get('lamps') or []:
        lines.append('    灯の窓: %s (%s) 中心 %s 彩度 %s 明るさ %s %s' % (lp.get('name'), lp.get('type'), lp.get('center'), lp.get('sat'), lp.get('lum'), lp.get('note') or ''))
    return '\n'.join(lines)


# ---- 場面の一覧

def measure_scene(folder, name, label=None):
    q = resolve(folder, name)
    s = make_scene(q['stage'], q['stageLay'], q['units'], q['ui'], q['uiLay'], folder=folder, name=q['base'], ui_only=q['uiOnly'])
    r = measure(s)
    r['label'] = label or q['base']
    return r


def measure_image(img, layout=None, units=None, ui=None, label=None):
    lay = layout or (img[:-4] + '.layout.json')
    lj = load_json(lay)
    ui_only = bool(lj) and not is_hideui(lj) and ui is None   # UI ありの画を舞台として渡された = UI を layout の矩形で除く (舞台の物差しは合否なし)
    s = make_scene(img, lay if os.path.exists(lay) else None, units, ui, None, folder=os.path.dirname(os.path.abspath(img)), ui_only=ui_only)
    r = measure(s)
    if lj is None:   # layout が無い = UI の有無も主人公の足元も分からない = 舞台の物差しは合否なし
        r['uiOnly'] = True
        r['notes'].append('layout.json が無い = UI の有無が分からない (舞台の物差しは合否なし)')
    r['label'] = label or os.path.basename(img)[:-4]
    return r


def measure_ref(key, metrics=None, ref_dirs=None):
    metrics = metrics if metrics is not None else load_json(DEFAULT_METRICS)
    s = ref_scene(key, metrics, ref_dirs or REF_DIRS)
    r = measure(s)
    r['label'] = '本家 %s (測り直し)' % key
    r['metricsKey'] = s.metricsKey
    return r


def all_scenes(folder):
    out = []
    for p in sorted(os.listdir(folder)):
        if not p.endswith('-1.png'):
            continue
        b = p[:-6]
        if b.endswith('-unitsonly') or b.endswith('-uionly'):
            continue
        b = scene_base(folder, b)
        if b not in out:
            out.append(b)
    return out


def main():
    ap = argparse.ArgumentParser(description='HD-2D 段2 幕2 の物差し (合否は目で。数字は補助)')
    ap.add_argument('folder', nargs='?')
    ap.add_argument('scenes', nargs='*')
    ap.add_argument('--all', action='store_true', help='フォルダの場面を全部')
    ap.add_argument('--image'); ap.add_argument('--layout'); ap.add_argument('--units'); ap.add_argument('--ui')
    ap.add_argument('--ref', action='append', default=[], help='本家 (ot2・mine・study・quarry・tunnel か metrics.json の A2-*)')
    ap.add_argument('--no-metrics-col', action='store_true', help='metrics.json の本家の値の列を足さない')
    ap.add_argument('--metrics', default=DEFAULT_METRICS)
    ap.add_argument('--ref-dir', action='append', default=[])
    ap.add_argument('--md'); ap.add_argument('--json'); ap.add_argument('--quiet', action='store_true')
    a = ap.parse_args()
    metrics = load_json(a.metrics) or {}
    ref_dirs = list(a.ref_dir) + REF_DIRS
    results = []
    for k in a.ref:
        try:
            results.append(measure_ref(k, metrics, ref_dirs))
            mk = REF_ALIAS.get(k, (k, None))[0]
            if not a.no_metrics_col and mk in metrics:
                results.append(metrics_column(metrics[mk], '本家 %s (metrics.json)' % k))
        except SystemExit as e:
            print('本家を測れない:', e)
    if a.image:
        results.append(measure_image(a.image, a.layout, a.units, a.ui))
    if a.folder:
        names = all_scenes(a.folder) if a.all or not a.scenes else a.scenes
        seen = set()
        for n in names:
            b = scene_base(a.folder, n)
            if b in seen:
                continue
            seen.add(b)
            try:
                results.append(measure_scene(a.folder, b))
            except SystemExit as e:
                print('測れない:', e)
    if not results:
        ap.error('測る物が無い (フォルダと場面・--image・--ref のどれか)')
    if not a.quiet:
        for r in results:
            if r.get('kind') != 'metrics':
                print(text_report(r))
    md = md_table(results)
    if a.md:
        open(a.md, 'w', encoding='utf-8').write(md)
    else:
        print(md)
    if a.json:
        json.dump(results, open(a.json, 'w', encoding='utf-8'), ensure_ascii=False, indent=1,
                  default=lambda o: o.item() if hasattr(o, 'item') else str(o))


if __name__ == '__main__':
    main()

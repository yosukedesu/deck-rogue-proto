#!/usr/bin/env python3
"""HD-2D 見本の数字の物差し (2026-09-30 P08。計画 docs/design/hd2d-slice-plan-2026-09-30.md §1-2 の①〜⑩)。

うちの撮影 (scripts/hd2d-shots.sh の置き場) と本家の画 (scripts/hd2d-fetch-ref.py の置き場) に同じ物差しを当てる。

使い方
  scripts/hd2d-measure.py <撮影のフォルダ> [--out metrics.json] [--md metrics.md] [--gates gates.json] [--frame 1] [--only 正規表現]
  scripts/hd2d-measure.py --ref <本家の画のフォルダ> [--out …] [--md …]          # 本家 (ref-patches の矩形で UI とキャラを除く)
  scripts/hd2d-measure.py --ref <本家の画のフォルダ> --write-gates <gates.json>    # 本家3枚から門を作る (W1 の統合で1回。以後は動かさない)
  --gates を付けると各数字に合否 (✓/✗) を付け、①〜⑨の合格数と⑩を数える。
  --ref-patches <json> で本家の矩形を差し替えられる (既定はこのファイルの REF_PATCHES)。

撮影のフォルダの中の名前 (hd2d-shots.sh・W0 の baseline と同じ)
  <PC|PH>-<場面>-<k>.png            通常 (UI あり)
  <PC|PH>-<場面>-hideui-<k>.png     UI なし (舞台と絵だけ)
  <PC|PH>-<場面>-uionly-<k>.png     UI だけ (舞台はマゼンタ。P01)
  <PC|PH>-<場面>-unitsonly-<k>.png  キャラの板だけ (ほかはマゼンタ。P01)
  <PC|PH>-<場面>-<k>.layout.json    dumplayout=1 の記録 (P01 の hd2d-layout/1)。通常の画の隣にあれば使う
  <PC|PH>-<場面>.log                撮影のログ (layout.json が無い時の⑩の手がかり。W0)
  場面ごとに通常の画を主にし、ある物だけ使う。無い物は近似に落とし、notes に書く (数字の横に「≈」)。

定義 (輝度 L = 0.2126R + 0.7152G + 0.0722B。sRGB の値のまま。画面の割合は画の高さ・幅に対して)
  ① m1  舞台の輝度の中央値: 通常の画の全体から UI を除いた画素の中央値。
         UI = uionly の画でマゼンタでない画素 (外周がマゼンタでそろっていなければ使わない) → 無ければ 通常と hideui の差 >12 の画素
         → 無ければ layout.json の札の矩形 → 無ければ W0 の近似 (紙の色の画素・①②は PC 縦 5.6〜59%・スマホ 7〜46% だけ)
         → 本家は矩形 (REF_PATCHES の ui)。
         本家は参考に全体の値 m1_full・m2_full も出す (調査の ot_921570_16 = 39.5・62%)。
  ② m2  暗い画素 (L<60) の割合: ①と同じ画素で。
  ③ m3  中央と左右端の明るさの比: 舞台の画 (hideui。無ければ通常) の縦 20〜80% で、中央 (横 40〜60%) の平均 ÷ 左右端 (0〜10% と 90〜100%) の平均。
         キャラ (unitsonly のマスク → 無ければ layout の絵の矩形 → 本家は矩形) と UI は除く。
         m3_uneven = 座席の帯のむら: 足元の線の少し上の帯 (左端の足元−5%〜右端の足元+5%。キャラと UI を除く) を横8つに分け、
         各区画の平均の (最大−最小)÷最大 (= いちばん暗い区画が明るい区画の何割か。0.2 以下 = 0.8 倍以上)。layout の足元が要る。
         m3_right_left = いちばん右の敵の体の明るさ ÷ いちばん左の敵の体の明るさ (⑧の体の値。敵2体以上の時)。
  ④ m4  上の1/3の輝度の中央値: 舞台の画 (hideui) の縦 0〜33.3%。本家は UI の矩形を除く。
  ⑤ m5  地面のざらつき: 舞台の画 (hideui) のグレーのラプラシアン分散を、地面の矩形 (リーダーの足元と左端の敵の足元の間・足元の線の近く。
         本家は ref-patches の ground) で。キャラは除く。m5_char = キャラの画素 (unitsonly のマスクを1画素削った内側) のラプラシアン分散。
  ⑥ m6  ピントの外の鋭さ: 舞台の画 (hideui) の帯ごとの「ラプラシアン分散 ÷ 分散」。m6 = 奥の帯 (上端〜25%) と手前の帯 (73〜98%) の大きい方。
         m6_seat = 座席の帯 (足元の線の上 15%) の同じ値 (P24 の「座席の帯の鋭さが dof=0 の0.95倍以上」は同じ場面の dof=0 の m6_seat と比べる)。
         m6_ratio = m6 ÷ m6_seat (参考)。
  ⑦ m7  明るい画素 (L>150) のうち、舞台の上の札 (敵の帳面・意図の札・自分の札・上部バー・人形の札) が占める割合。
         手札・確認の窓・ポップアップの矩形は分母からも除く (layout.json が要る)。m7_all = UI 全部 (手札込み) の割合 (参考。StS1 55%)。
         layout.json が無い時は W0 と同じ近似 (UI = 通常と hideui の差・縦 66% より下を捨てる)。
  ⑧ m8  キャラの体の明るさと足元の後ろの地面の輪の明るさ: 体 = 通常の画のキャラの画素 (unitsonly のマスク ∩ 絵の矩形・UI を除く) の中央値。
         輪 = 足元の周り (横 ±0.7×絵の幅・足元の 0.25×絵の高さ上〜5px 下) からキャラと UI を除いた画素の中央値。
         合格 = 差 15 以上か比 1.4 以上 (どちら向きでも)。あわせて主人公の体 ≧ ① (舞台の中央値)。
  ⑨ m9  意図の数字: layout の意図の札 (intent) の中の数字の文字の大きさ (px) と、文字の色 (記録の color) と札の地 (数字の箱の周りの画素の中央値) のコントラスト (WCAG)。
  ⑩ m10 座席での1ドットの大きさ (px): 絵の矩形の幅 ÷ 絵のドットの幅 (Resources/Art)。layout の stage.unitBoxes に板の箱 (boardPx) があればそちら。
         無ければ撮影のログの layout 行 (W0)。
"""
import argparse
import datetime
import glob
import json
import math
import os
import re
import sys

import numpy as np
from PIL import Image

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ART = os.path.join(REPO, 'unity', 'Assets', 'Resources', 'Art')

# 本家の画の矩形 [x, y, w, h] (1920×1080 の画素)。ground と bg は調査 (hd2d-result の patches.png の赤と水色) と同じ。
# ui と chars は 2026-09-30 に目で取った (UI = 行動順の帯・HP の箱・弱点の札・コマンドの窓・名前の札。chars = キャラと敵の体の外接の箱)。
REF_PATCHES = {
    'ot_921570_16': {
        'title': 'オクトラ1 夜の森の戦闘 (ハンイット)',
        'ground': [900, 780, 350, 100], 'bg': [900, 250, 400, 200],
        'ui': [[37, 22, 968, 143], [1642, 30, 248, 142], [428, 777, 217, 63]],
        'chars': [[165, 210, 720, 570], [1312, 638, 120, 150]],
        'hero': [1312, 638, 120, 150],
    },
    'ot_921570_7': {
        'title': 'オクトラ1 洞窟の戦闘 (トレサ)',
        'ground': [700, 800, 300, 100], 'bg': [700, 250, 400, 200],
        'ui': [[37, 22, 968, 143], [1560, 30, 278, 142], [1087, 588, 605, 230], [450, 787, 180, 61]],
        'chars': [[397, 382, 286, 398], [960, 622, 98, 143]],
        'hero': [960, 622, 98, 143],
    },
    'ot_921570_11': {
        'title': 'オクトラ1 昼の村の戦闘 (光の筋)',
        'ground': [1100, 860, 300, 70], 'bg': [300, 250, 500, 170],
        'ui': [[37, 22, 968, 143], [897, 172, 126, 60], [1545, 22, 293, 155], [487, 795, 211, 57]],
        'chars': [[558, 637, 97, 135], [904, 637, 108, 131], [1095, 510, 435, 322]],
        'hero': [904, 637, 108, 131],
    },
}
GATE_REFS = ['ot_921570_16', 'ot_921570_7', 'ot_921570_11']   # 門を決める本家の画 (計画 §1-2)
GATE_MAIN = 'ot_921570_16'                                       # 門の基準の1枚 (夜の森の戦闘)

VARIANTS = ('hideui', 'uionly', 'unitsonly')
MAGENTA_TOL = 10          # マゼンタ (255,0,255) と見なす差 (各チャンネル)
BRIGHT = 150              # ⑦ の明るい画素
DARK = 60                 # ② の暗い画素

# ------------------------------------------------------------------------------------------ 画素の道具


def load_rgb(p):
    return np.asarray(Image.open(p).convert('RGB')).astype(np.float32)


def lum(a):
    return 0.2126 * a[..., 0] + 0.7152 * a[..., 1] + 0.0722 * a[..., 2]


def gray_of(a):
    # PIL の convert('L') と同じ係数 (ITU-R 601-2)。調査の hf.py・otlum の lapVar と同じ物差し
    return (a[..., 0] * 299 + a[..., 1] * 587 + a[..., 2] * 114) / 1000.0


def lap(g):
    return g[1:-1, 1:-1] * 4 - g[:-2, 1:-1] - g[2:, 1:-1] - g[1:-1, :-2] - g[1:-1, 2:]


def erode(m, n=1):
    m = m.copy()
    for _ in range(n):
        e = m.copy()
        e[1:, :] &= m[:-1, :]
        e[:-1, :] &= m[1:, :]
        e[:, 1:] &= m[:, :-1]
        e[:, :-1] &= m[:, 1:]
        e[0, :] = e[-1, :] = False
        e[:, 0] = e[:, -1] = False
        m = e
    return m


def dilate(m, n=1):
    m = m.copy()
    for _ in range(n):
        e = m.copy()
        e[1:, :] |= m[:-1, :]
        e[:-1, :] |= m[1:, :]
        e[:, 1:] |= m[:, :-1]
        e[:, :-1] |= m[:, 1:]
        m = e
    return m


def lapvar_region(g, mask):
    """mask の内側 (1画素削った所) のラプラシアン分散。画素が少なすぎれば None"""
    L = lap(g)
    m = erode(mask, 1)[1:-1, 1:-1]
    if m.sum() < 64:
        return None
    return float(L[m].var())


def sharp_band(g, y0, y1, mask=None):
    """帯の「ラプラシアン分散 ÷ 分散」(明暗の幅に依らない鋭さ)。mask は除く画素"""
    y0 = max(1, y0)
    y1 = min(g.shape[0] - 1, y1)
    if y1 - y0 < 8:
        return None
    band = g[y0 - 1:y1 + 1]
    L = lap(band)
    core = band[1:-1, 1:-1]
    keep = np.ones(core.shape, bool)
    if mask is not None:
        keep &= ~dilate(mask, 1)[y0:y1, 1:-1]
    if keep.sum() < 256:
        return None
    v = float(core[keep].var())
    if v < 1e-6:
        return None
    return float(L[keep].var()) / v


def rect_mask(shape, rects, pad=0):
    H, W = shape[:2]
    m = np.zeros((H, W), bool)
    for r in rects or []:
        if r is None or len(r) < 4:
            continue
        x, y, w, h = r[:4]
        x0 = int(max(0, math.floor(x - pad)))
        y0 = int(max(0, math.floor(y - pad)))
        x1 = int(min(W, math.ceil(x + w + pad)))
        y1 = int(min(H, math.ceil(y + h + pad)))
        if x1 > x0 and y1 > y0:
            m[y0:y1, x0:x1] = True
    return m


def is_magenta(a, tol=MAGENTA_TOL):
    return (np.abs(a[..., 0] - 255) <= tol) & (a[..., 1] <= tol) & (np.abs(a[..., 2] - 255) <= tol)


def srgb_to_linear(c):
    c = c / 255.0
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def rel_lum(rgb):
    lin = srgb_to_linear(np.asarray(rgb, dtype=np.float64))
    return float(0.2126 * lin[0] + 0.7152 * lin[1] + 0.0722 * lin[2])


def contrast(rgb1, rgb2):
    a, b = rel_lum(rgb1), rel_lum(rgb2)
    hi, lo = max(a, b), min(a, b)
    return (hi + 0.05) / (lo + 0.05)


def hex_rgb(s):
    s = (s or '').lstrip('#')
    if len(s) < 6:
        return None
    try:
        return [int(s[0:2], 16), int(s[2:4], 16), int(s[4:6], 16)]
    except ValueError:
        return None


def rnd(v, n=3):
    if v is None:
        return None
    if isinstance(v, float):
        if math.isnan(v) or math.isinf(v):
            return None
        return round(v, n)
    return v

# ------------------------------------------------------------------------------------------ 撮影のフォルダ

NAME_RE = re.compile(r'^(?:(PC|PH)-)?(.+?)(?:-(hideui|uionly|unitsonly))?-(\d+)\.png$')


def scan_dir(d, frame=1, only=None):
    """場面 → {dev, scene, normal, hideui, uionly, unitsonly, layout, log}。通常の画がある場面だけ"""
    scenes = {}
    for p in sorted(glob.glob(os.path.join(d, '*.png'))):
        m = NAME_RE.match(os.path.basename(p))
        if not m:
            continue
        dev, scene, var, k = m.group(1) or '', m.group(2), m.group(3) or 'normal', int(m.group(4))
        if k != frame:
            continue
        key = (dev + '-' if dev else '') + scene
        if only and not re.search(only, key):
            continue
        e = scenes.setdefault(key, {'dev': dev or 'PC', 'scene': scene, 'key': key})
        e[var] = p
    out = {}
    for key, e in scenes.items():
        if 'normal' not in e:
            continue
        stem = e['normal'][:-4]
        lj = stem + '.layout.json'
        if os.path.exists(lj):
            e['layout'] = lj
        lg = os.path.join(d, key + '.log')
        if os.path.exists(lg):
            e['log'] = lg
        out[key] = e
    return out


def load_layout(p):
    try:
        with open(p, encoding='utf-8') as f:
            return json.load(f)
    except Exception:
        return None

# ------------------------------------------------------------------------------------------ layout.json から


def lay_units(lay):
    return [u for u in (lay or {}).get('units', []) if isinstance(u, dict)]


def unit_feet(lay, u):
    """足元の画面の点 (x, y) = 絵の矩形の下端の中央。y は statusLineY と feetOffset があればそちら"""
    sp = u.get('sprite')
    if not sp:
        return None
    x = sp[0] + sp[2] / 2.0
    y = sp[1] + sp[3]
    sl = (lay or {}).get('statusLineY') or {}
    fo = u.get('feetOffset')
    sc = ((lay or {}).get('canvas') or {}).get('scale') or 1.0
    if isinstance(fo, (int, float)) and not (isinstance(fo, float) and math.isnan(fo)) and isinstance(sl.get('px'), (int, float)):
        y = sl['px'] - fo * sc
    return (x, y)


def nodes_matching(lay, pattern):
    rx = re.compile(pattern)
    return [n for n in (lay or {}).get('nodes', []) if isinstance(n, dict) and rx.search(n.get('path', ''))]


def overlay_ui_rects(lay):
    """舞台の上に常に出る札の矩形 (⑦ の分子): 敵の帳面・意図の札・自分の札・上部バー・人形の札"""
    rects = []
    for u in lay_units(lay):
        if u.get('kind') == 'enemy':
            for k in ('strip', 'intent'):
                if u.get(k):
                    rects.append(u[k])
    for n in nodes_matching(lay, r'/player/(hpwrap|setzone|gearzone|perms)$'):
        rects.append(n['px'])
    for n in nodes_matching(lay, r'/doll:[^/]+/tag$'):
        rects.append(n['px'])
    tb = ((lay or {}).get('anchors') or {}).get('topbar')
    if tb:
        rects.append(tb)
    return rects


def excluded_ui_rects(lay):
    """⑦ の分母から除く矩形: 手札・確認の窓・ポップアップ"""
    rects = [h['px'] for h in (lay or {}).get('hand', []) if isinstance(h, dict) and h.get('px')]
    for n in nodes_matching(lay, r'(^popup/[^/]+$)|(/reaction$)'):
        rects.append(n['px'])
    return rects


def sprite_rects(lay, kinds=('enemy', 'player', 'doll')):
    return [u['sprite'] for u in lay_units(lay) if u.get('kind') in kinds and u.get('sprite') and u.get('alive', True)]


def art_dots(kind, uid, flags=None, phone=False):
    """絵のドットの幅 (Resources/Art)。見つからなければ None"""
    if not uid:
        return None
    folder = {'enemy': 'enemies', 'player': 'leaders', 'doll': 'dolls'}.get(kind)
    if folder is None:
        return None
    cands = []
    if kind == 'player' and flags and str(flags.get('herodots', '')) == '48':
        cands.append(uid + '_48')
    if kind == 'enemy' and phone:
        cands.append(uid + '_96')
    cands.append(uid)
    for c in cands:
        p = os.path.join(ART, folder, c + '.png')
        if os.path.exists(p):
            try:
                return Image.open(p).size[0]
            except Exception:
                pass
    return None

# ------------------------------------------------------------------------------------------ 1場面を測る


class Scene:
    def __init__(self, e):
        self.e = e
        self.notes = []
        self.normal = load_rgb(e['normal'])
        self.H, self.W = self.normal.shape[:2]
        self.hideui = self._opt('hideui')
        self.uionly = self._opt('uionly')
        self.unitsonly = self._opt('unitsonly')
        self.lay = load_layout(e['layout']) if e.get('layout') else None
        self.phone = e.get('dev') == 'PH' or bool(((self.lay or {}).get('canvas') or {}).get('phone'))

    def _opt(self, v):
        p = self.e.get(v)
        if not p:
            return None
        a = load_rgb(p)
        if a.shape != self.normal.shape:
            self.notes.append('%s の寸法が違うので使わない' % v)
            return None
        return a

    # ---- マスク
    def topbar_bottom(self):
        tb = ((self.lay or {}).get('anchors') or {}).get('topbar')
        if tb:
            return int(tb[1] + tb[3])
        return int(self.H * (0.07 if self.phone else 0.067))

    def ui_mask(self):
        if hasattr(self, '_ui'):
            return self._ui
        m = None
        if self.uionly is not None:
            mag = is_magenta(self.uionly)
            y0 = self.topbar_bottom() + 4
            y1 = int(self.H * 0.55)
            # 手札の下の暗幕 (screen/field/desk-shade・画面の全幅の半透明の帯) は UI なので外周の検査から外す。
            # スマホ相当では暗幕が画の 48% から始まり、55% までの窓に掛かって「後処理」と取り違えていた (W1 統合 2026-09-30)
            for n in nodes_matching(self.lay, r'/desk-shade$'):
                y1 = min(y1, int(n['px'][1]))
            border = np.concatenate([mag[y0:y1, :3].ravel(), mag[y0:y1, -3:].ravel()])
            if border.size and border.mean() >= 0.98:
                m = ~mag
                self.ui_source = 'uionly'
            else:
                self.notes.append('uionly の外周がマゼンタでそろわない (%.0f%%) = 後処理が掛かっている → 使わない' % (100 * (border.mean() if border.size else 0)))
        if m is None and self.hideui is not None:
            m = np.abs(self.normal - self.hideui).max(axis=2) > 12
            self.ui_source = 'diff'
            self.notes.append('UI = 通常と hideui の差 (近似)')
        if m is None and self.lay is not None and self.lay.get('units') is not None:
            # layout.json の札の矩形 (夜色の札でも使える。紙の色の近似より確か)
            rects = overlay_ui_rects(self.lay) + excluded_ui_rects(self.lay)
            rects += [n['px'] for n in nodes_matching(self.lay, r'/(desk-shade|handlayer)$')]
            rects += [r for k, r in ((self.lay.get('anchors') or {}).items()) if isinstance(r, list)]
            m = rect_mask(self.normal.shape, rects)
            self.ui_source = 'rects'
            self.notes.append('UI = layout.json の札の矩形 (近似。uionly も hideui も無い)')
        if m is None:
            # W0 と同じ近似: クリーム色の紙の画素を UI とみなし、①②は舞台の帯 (PC 縦 5.6〜59%・スマホ 7〜46%) だけで測る
            a = self.normal
            r_, g_, b_ = a[..., 0], a[..., 1], a[..., 2]
            m = (r_ > 185) & (g_ > 170) & (b_ > 130) & ((r_ - b_) < 90) & ((r_ - b_) > 5)
            self.ui_source = 'paper'
            self.notes.append('UI = 紙の色の画素・①②は舞台の帯だけ (W0 の近似。uionly も hideui も無い)')
        self._ui = m
        return m

    def stage_rows(self):
        """W0 の舞台の帯 (紙の近似の時だけ①②に使う)"""
        return (int(self.H * 0.07), int(self.H * 0.46)) if self.phone else (int(self.H * 0.056), int(self.H * 0.59))

    def char_mask(self):
        if hasattr(self, '_ch'):
            return self._ch
        m = None
        if self.unitsonly is not None:
            mag = is_magenta(self.unitsonly)
            y0 = self.topbar_bottom() + 4
            border = np.concatenate([mag[y0:, :2].ravel(), mag[y0:, -2:].ravel(), mag[-2:, :].ravel()])
            if border.size and border.mean() >= 0.98 and (~mag).sum() > 200:
                m = ~mag
                self.char_source = 'unitsonly'
            else:
                self.notes.append('unitsonly が使えない (外周がそろわないか板が写っていない)')
        if m is None and self.lay is not None:
            m = rect_mask(self.normal.shape, sprite_rects(self.lay))
            self.char_source = 'rects'
            self.notes.append('キャラ = 絵の矩形 (近似)')
        if m is None:
            m = np.zeros((self.H, self.W), bool)
            self.char_source = 'none'
            self.notes.append('キャラのマスクなし')
        self._ch = m
        return m

    def stage_img(self):
        if self.hideui is not None:
            return self.hideui, False
        self.notes.append('舞台の画 = 通常の画 (hideui なし)')
        return self.normal, True

    # ---- 足元
    def feet(self):
        out = []
        for u in lay_units(self.lay):
            if not u.get('alive', True) or not u.get('sprite'):
                continue
            f = unit_feet(self.lay, u)
            if f:
                out.append((u, f))
        return out

    def feet_line(self):
        fs = [f[1] for u, f in self.feet() if u.get('kind') in ('enemy', 'player')]
        if fs:
            return float(np.median(fs))
        return self.H * (0.47 if self.phone else 0.55)

    # ---- ①〜⑩
    def measure(self):
        r = {'file': os.path.basename(self.e['normal']), 'size': [self.W, self.H], 'dev': self.e.get('dev'), 'scene': self.e.get('scene')}
        Ln = lum(self.normal)
        ui = self.ui_mask()
        # ① ②
        keep = ~ui
        if getattr(self, 'ui_source', '') == 'paper':
            y0r, y1r = self.stage_rows()
            rows = np.zeros_like(keep)
            rows[y0r:y1r] = True
            keep &= rows
        if keep.sum() < 1000:
            keep = np.ones_like(ui)
            self.notes.append('UI を除くと画素が残らない → 全体で')
        r['m1'] = float(np.median(Ln[keep]))
        r['m2'] = float((Ln[keep] < DARK).mean())
        r['stage_px_share'] = float(keep.mean())
        # ③
        img, usesNormal = self.stage_img()
        Ls = lum(img)
        ch = self.char_mask()
        ex = ch | (ui if usesNormal else np.zeros_like(ch))
        y0, y1 = int(self.H * 0.2), int(self.H * 0.8)
        band = np.zeros_like(ch)
        band[y0:y1] = True
        cmask = band.copy()
        cmask[:, :int(self.W * 0.4)] = False
        cmask[:, int(self.W * 0.6):] = False
        emask = band.copy()
        emask[:, int(self.W * 0.1):int(self.W * 0.9)] = False
        cm, em = cmask & ~ex, emask & ~ex
        if cm.sum() > 500 and em.sum() > 500:
            r['m3'] = float(Ls[cm].mean() / max(1e-6, Ls[em].mean()))
            r['m3_center'] = float(Ls[cm].mean())
        # ③ むら (座席の帯)
        feet = self.feet()
        fl = self.feet_line()
        xs = [f[0] for u, f in feet if u.get('kind') in ('enemy', 'player')]
        bins = []
        if xs:
            bx0, bx1 = int(max(0, min(xs) - 0.05 * self.W)), int(min(self.W, max(xs) + 0.05 * self.W))
            by0, by1 = int(fl - 0.08 * self.H), int(fl + 0.02 * self.H)
        else:
            bx0 = bx1 = by0 = by1 = 0
            if not isinstance(self, RefScene):
                self.notes.append('③のむらは測らない (layout の足元なし)')
        if bx1 - bx0 > 80 and by1 > by0:
            edges = np.linspace(bx0, bx1, 9).astype(int)
            for i in range(8):
                mm = np.zeros_like(ch)
                mm[max(0, by0):min(self.H, by1), edges[i]:edges[i + 1]] = True
                mm &= ~ex
                if mm.sum() > 50:
                    bins.append(float(Ls[mm].mean()))
        if len(bins) >= 4:
            # 「いちばん暗い区画がいちばん明るい区画の 0.8 倍以上」= (最大−最小)÷最大 ≦ 0.2 (右の敵の 0.8 倍と同じ読み)
            r['m3_uneven'] = float((max(bins) - min(bins)) / max(1e-6, max(bins)))
            r['m3_bins'] = [round(b, 1) for b in bins]
        # ④
        top = np.zeros_like(ch)
        top[:int(self.H / 3)] = True
        if usesNormal:
            top &= ~ui
        r['m4'] = float(np.median(Ls[top]))
        # ⑤
        g = gray_of(img)
        gr = self.ground_rect()
        gm = rect_mask(img.shape, [gr]) & ~ch
        if usesNormal:
            gm &= ~ui
        r['m5_rect'] = [int(v) for v in gr]
        r['m5'] = lapvar_region(g, gm)
        if getattr(self, 'char_source', '') == 'unitsonly':
            r['m5_char'] = lapvar_region(g, ch)
        elif self.lay is not None:
            inner = []
            for s in sprite_rects(self.lay):
                inner.append([s[0] + s[2] * 0.3, s[1] + s[3] * 0.25, s[2] * 0.4, s[3] * 0.5])
            r['m5_char'] = lapvar_region(g, rect_mask(img.shape, inner))
            if r['m5_char'] is not None:
                self.notes.append('⑤のキャラ = 絵の矩形の中央 (近似)')
        # ⑥
        far0 = self.topbar_bottom() if not self.phone else int(self.H * 0.07)
        far = sharp_band(g, far0, int(self.H * 0.25), ui if usesNormal else None)
        near = sharp_band(g, int(self.H * 0.73), int(self.H * 0.98), ui if usesNormal else None)
        seat = sharp_band(g, int(fl - 0.15 * self.H), int(fl), ui if usesNormal else None)
        r['m6_far'], r['m6_near'], r['m6_seat'] = far, near, seat
        outs = [v for v in (far, near) if v is not None]
        if outs:
            r['m6'] = max(outs)
            if seat:
                r['m6_ratio'] = r['m6'] / seat
        # ⑦
        bright = Ln > BRIGHT
        if self.lay is not None and self.lay.get('units') is not None:
            over = rect_mask(self.normal.shape, overlay_ui_rects(self.lay)) & ui
            excl = rect_mask(self.normal.shape, excluded_ui_rects(self.lay))
            den = bright & ~excl
            if den.sum() > 0:
                r['m7'] = float((den & over).sum() / den.sum())
            r['m7_all'] = float((bright & ui).sum() / max(1, bright.sum()))
            r['m7_bright_px'] = int(den.sum())
        else:
            b = bright.copy()
            b[int(self.H * 0.66):] = False
            if b.sum() > 0:
                r['m7'] = float((b & ui).sum() / b.sum())
            r['m7_all'] = float((bright & ui).sum() / max(1, bright.sum()))
            self.notes.append('⑦ = W0 の近似 (UI 全部・縦 66% より下を捨てる)')
        # ⑧
        units8 = []
        for u, f in feet:
            sp = u['sprite']
            sm = rect_mask(self.normal.shape, [sp])
            body_m = erode(sm & ch, 1) & ~ui
            if getattr(self, 'char_source', '') != 'unitsonly':
                body_m = rect_mask(self.normal.shape, [[sp[0] + sp[2] * 0.3, sp[1] + sp[3] * 0.25, sp[2] * 0.4, sp[3] * 0.5]]) & ~ui
            body = float(np.median(Ln[body_m])) if body_m.sum() > 30 else None
            w, h = sp[2], sp[3]
            ring_r = [f[0] - 0.7 * w, f[1] - 0.25 * h, 1.4 * w, 0.25 * h + 5]
            ring_m = rect_mask(self.normal.shape, [ring_r]) & ~dilate(ch, 2) & ~ui
            src = 'normal'
            if ring_m.sum() < 200 and self.hideui is not None:
                ring_m = rect_mask(self.normal.shape, [ring_r]) & ~dilate(ch, 2)
                src = 'hideui'
            ringv = None
            if ring_m.sum() > 30:
                ringv = float(np.median((Ln if src == 'normal' else lum(self.hideui))[ring_m]))
            ok = None
            if body is not None and ringv is not None:
                d = body - ringv
                ratio = max(body, ringv) / max(1.0, min(body, ringv))
                ok = abs(d) >= 15 or ratio >= 1.4
            units8.append({'key': u.get('key'), 'kind': u.get('kind'), 'body': rnd(body, 1), 'ring': rnd(ringv, 1), 'ringFrom': src, 'ok': ok, 'x': rnd(f[0], 0)})
        if units8:
            r['m8_units'] = units8
            oks = [u['ok'] for u in units8 if u['ok'] is not None]
            hero = [u for u in units8 if u['kind'] == 'player' and u['body'] is not None]
            r['m8_hero_body'] = hero[0]['body'] if hero else None
            r['m8_hero_ok'] = (hero[0]['body'] >= r['m1']) if hero else None
            r['m8_ok'] = (all(oks) if oks else None)
            if r['m8_ok'] is not None and r['m8_hero_ok'] is not None:
                r['m8_ok'] = r['m8_ok'] and r['m8_hero_ok']
            en = sorted([u for u in units8 if u['kind'] == 'enemy' and u['body'] is not None], key=lambda u: u['x'])
            if len(en) >= 2:
                r['m3_right_left'] = float(en[-1]['body'] / max(1.0, en[0]['body']))
        # ⑨
        m9 = self.intent_digits()
        if m9:
            r['m9_units'] = m9
            fs = [u['fs'] for u in m9 if u['fs'] is not None]
            cs = [u['contrast'] for u in m9 if u['contrast'] is not None]
            r['m9_fs'] = min(fs) if fs else None
            r['m9_contrast'] = min(cs) if cs else None
        # ⑩
        m10 = self.dots()
        if m10:
            r['m10_units'] = m10
            vals = [u['pxPerDot'] for u in m10 if u['pxPerDot'] is not None]
            if vals:
                r['m10_min'] = min(vals)
                r['m10_max'] = max(vals)
        r['sources'] = {'ui': getattr(self, 'ui_source', None), 'chars': getattr(self, 'char_source', None), 'layout': bool(self.lay), 'hideui': self.hideui is not None}
        r['notes'] = self.notes
        return {k: rnd(v) if isinstance(v, float) else v for k, v in r.items()}

    def ground_rect(self):
        feet = self.feet()
        fl = self.feet_line()
        pl = [f for u, f in feet if u.get('kind') == 'player']
        en = sorted([f for u, f in feet if u.get('kind') == 'enemy'], key=lambda f: f[0])
        if pl and en:
            x0 = pl[0][0] + 0.06 * self.W
            x1 = en[0][0] - 0.06 * self.W
            if x1 - x0 >= 0.05 * self.W:
                return [x0, fl - 0.02 * self.H, x1 - x0, 0.09 * self.H]
        self.notes.append('⑤の地面 = 固定の矩形 (調査と同じ 700,560,200,100 を画の大きさに合わせた)')
        return [self.W * 700 / 1920, self.H * 560 / 1080, self.W * 200 / 1920, self.H * 100 / 1080]

    def intent_digits(self):
        if self.lay is None:
            return None
        out = []
        nodes = [n for n in self.lay.get('nodes', []) if isinstance(n, dict) and n.get('digits')]
        for u in lay_units(self.lay):
            it = u.get('intent')
            if u.get('kind') != 'enemy' or not it or not u.get('alive', True):
                continue
            x, y, w, h = it
            best = None
            for n in nodes:
                px = n.get('px') or [0, 0, 0, 0]
                cx, cy = px[0] + px[2] / 2, px[1] + px[3] / 2
                if not (x <= cx <= x + w and y <= cy <= y + h):
                    continue
                fs = n.get('fs')
                if best is None or (fs or 0) > (best.get('fs') or 0):
                    best = n
            if best is None:
                out.append({'key': u.get('key'), 'fs': None, 'contrast': None})
                continue
            col = hex_rgb(best.get('color'))
            dm = rect_mask(self.normal.shape, best['digits'])
            bg_m = rect_mask(self.normal.shape, [it]) & ~dilate(dm, 3)
            con = None
            if col is not None and bg_m.sum() > 20:
                bg = [float(np.median(self.normal[..., c][bg_m])) for c in range(3)]
                con = contrast(col, bg)
            out.append({'key': u.get('key'), 'fs': rnd(float(best.get('fs') or 0), 1), 'contrast': rnd(con, 2), 'text': best.get('text')})
        return out

    def dots(self):
        out = []
        flags = (self.lay or {}).get('flags') or {}
        boxes = {}
        ub = ((self.lay or {}).get('stage') or {}).get('unitBoxes')
        if isinstance(ub, list):
            for b in ub:
                if isinstance(b, dict) and b.get('key'):
                    boxes[b['key']] = b
        elif isinstance(ub, dict):
            for k, b in ub.items():
                if isinstance(b, dict):
                    boxes[k] = b
        for u in lay_units(self.lay):
            if not u.get('alive', True) or not u.get('sprite'):
                continue
            dots = art_dots(u.get('kind'), u.get('id'), flags, self.phone)
            if not dots:
                continue
            w = u['sprite'][2]
            src = 'rect'
            b = boxes.get(u.get('key'))
            if b:
                bp = b.get('boardPx') or b.get('board')
                if isinstance(bp, list) and len(bp) >= 4:
                    w = bp[2]
                    src = 'board'
            out.append({'key': u.get('key'), 'pxPerDot': rnd(w / dots, 3), 'from': src})
        if not out and self.e.get('log'):
            out = self.dots_from_log()
        return out

    def dots_from_log(self):
        try:
            s = open(self.e['log'], encoding='utf-8', errors='ignore').read()
        except Exception:
            return []
        m = re.search(r'\[Autopilot\] layout .*', s)
        if not m:
            return []
        line = m.group(0)
        cw = re.search(r'canvas=\(([0-9.]+),', line)
        if not cw:
            return []
        k = self.W / float(cw.group(1))
        out = []
        for key, wv in re.findall(r'(enemy\d+|player)=\(([0-9.]+),', line):
            out.append({'key': key, 'pxPerDot': None, 'canvasW': float(wv), 'k': k, 'from': 'log'})
        self.notes.append('⑩ = 撮影のログ (絵の id が分からないので px の幅だけ。W0)')
        return out

# ------------------------------------------------------------------------------------------ 本家


class RefScene(Scene):
    def __init__(self, path, patch):
        self.e = {'normal': path, 'dev': 'REF', 'scene': os.path.basename(path)[:-4]}
        self.notes = []
        self.normal = load_rgb(path)
        self.H, self.W = self.normal.shape[:2]
        self.hideui = self.uionly = self.unitsonly = None
        self.lay = None
        self.phone = False
        self.patch = patch or {}
        self._ui = rect_mask(self.normal.shape, self.patch.get('ui'))
        self.ui_source = 'ref-patches'
        self._ch = rect_mask(self.normal.shape, self.patch.get('chars'))
        self.char_source = 'ref-patches'

    def topbar_bottom(self):
        return int(self.H * 0.056)

    def stage_img(self):
        return self.normal, True

    def feet(self):
        return []

    def feet_line(self):
        h = self.patch.get('hero')
        if h:
            return float(h[1] + h[3])
        return self.H * 0.75

    def ground_rect(self):
        g = self.patch.get('ground')
        return g if g else [self.W * 0.45, self.H * 0.75, self.W * 0.15, self.H * 0.08]

    def measure(self):
        r = Scene.measure(self)
        # 本家は UI の矩形を除いて② ① を測る。参考に、調査と同じ「全体」の値も残す (ot_16 = 39.5・62%)
        Ln = lum(self.normal)
        r['m1_full'] = rnd(float(np.median(Ln)))
        r['m2_full'] = rnd(float((Ln < DARK).mean()))
        # 本家の⑦ = 明るい画素に占める UI の矩形 (参考)
        bright = Ln > BRIGHT
        r['m7'] = rnd(float((bright & self._ui).sum() / max(1, bright.sum())))
        r['m7_all'] = r['m7']
        # ⑧ 主人公の体 (矩形の中央) と足元の輪
        h = self.patch.get('hero')
        if h:
            body_m = rect_mask(self.normal.shape, [[h[0] + h[2] * 0.3, h[1] + h[3] * 0.25, h[2] * 0.4, h[3] * 0.5]])
            ring_m = rect_mask(self.normal.shape, [[h[0] + h[2] / 2 - 0.7 * h[2], h[1] + h[3] - 0.25 * h[3], 1.4 * h[2], 0.25 * h[3] + 5]]) & ~dilate(self._ch, 2) & ~self._ui
            body = float(np.median(Ln[body_m]))
            ringv = float(np.median(Ln[ring_m])) if ring_m.sum() > 30 else None
            ok = None
            if ringv is not None:
                ok = abs(body - ringv) >= 15 or max(body, ringv) / max(1.0, min(body, ringv)) >= 1.4
            r['m8_hero_body'] = rnd(body, 1)
            r['m8_units'] = [{'key': 'hero', 'kind': 'player', 'body': rnd(body, 1), 'ring': rnd(ringv, 1), 'ok': ok}]
            r['m8_ok'] = ok
        r['m5_bg'] = lapvar_region(gray_of(self.normal), rect_mask(self.normal.shape, [self.patch.get('bg')])) if self.patch.get('bg') else None
        r['title'] = self.patch.get('title')
        r['notes'] = [n for n in self.notes if not n.startswith('⑦')]
        return r

# ------------------------------------------------------------------------------------------ 門


def make_gates(refs):
    """本家3枚の数字から門 (計画 §1-2 の読み)"""
    main = refs.get(GATE_MAIN)
    if not main:
        raise SystemExit('門の基準の画 %s が無い' % GATE_MAIN)
    m1s = [refs[k]['m1'] for k in GATE_REFS if k in refs and refs[k].get('m1') is not None]
    g = {
        'm1': {'min': rnd(main['m1'] * 0.85, 1), 'max': rnd(max(m1s) * 1.15, 1),
               'why': '%s の ① %.1f の −15%% 以上、かつ本家3枚の最大 %.1f の 1.15 倍以下' % (GATE_MAIN, main['m1'], max(m1s))},
        'm2': {'max': rnd(main['m2'], 3), 'why': '%s の ② %.1f%% 以下' % (GATE_MAIN, main['m2'] * 100)},
        'm3': {'min': 4.0, 'unevenMax': 0.2, 'rightLeftMin': 0.8,
               'why': '4 以上 (本家3枚: %s)。あわせて座席の帯のむら 20%% 以下・いちばん右の敵の体がいちばん左の 0.8 倍以上' % ', '.join('%s %.2f' % (k, refs[k]['m3']) for k in GATE_REFS if k in refs and refs[k].get('m3'))},
        'm4': {'min': rnd(main['m4'] * 0.75, 1), 'why': '%s の ④ %.1f の 0.75 倍以上' % (GATE_MAIN, main['m4'])},
        'm5': {'max': 1500, 'charMax': True, 'why': 'キャラ以下 (m5 ≦ m5_char)。目安 1,500 (本家3枚: %s)' % ', '.join('%s %.0f' % (k, refs[k]['m5']) for k in GATE_REFS if k in refs and refs[k].get('m5') is not None)},
        'm6': {'max': 0.1, 'why': '目安 0.1 以下。奥の座席の敵の意図と HP が読めることが先 (本家3枚: %s)' % ', '.join('%s %.2f' % (k, refs[k]['m6']) for k in GATE_REFS if k in refs and refs[k].get('m6') is not None)},
        'm7': {'max': 0.5, 'why': '50% 未満 (手札と確認の窓は除いて測る)'},
        'm8': {'minDiff': 15, 'minRatio': 1.4, 'heroAtLeastM1': True, 'why': '各キャラの体と足元の後ろの地面の輪の差 15 以上か比 1.4 以上。主人公の体は ① 以上'},
        'm9': {'minPx': 32, 'minContrast': 7.0, 'why': '意図の数字 32px 以上・地とのコントラスト 7:1 以上'},
        'm10': {'target': 4.0, 'tol': 0.1, 'pcOnly': True, 'why': '座席での1ドット 4.0±0.1px (PC)'},
    }
    return {
        'schema': 'hd2d-gates/1',
        'made': datetime.datetime.now().isoformat(timespec='seconds'),
        'tool': 'scripts/hd2d-measure.py --write-gates',
        'plan': 'docs/design/hd2d-slice-plan-2026-09-30.md §1-2',
        'refs': {k: {kk: refs[k].get(kk) for kk in ('title', 'm1', 'm2', 'm1_full', 'm2_full', 'm3', 'm4', 'm5', 'm6', 'm7')} for k in GATE_REFS if k in refs},
        'gates': g,
        'rule': {'passM1toM9': 7, 'm10Required': True, 'why': '①〜⑨のうち7つ以上を満たし、⑩は必ず満たす'},
    }


def judge(r, gates):
    """場面の数字に合否を付ける。戻り値 {m1: True/False/None, …}・合格数"""
    G = (gates or {}).get('gates', {})
    j = {}

    def between(v, g):
        if v is None:
            return None
        if 'min' in g and v < g['min']:
            return False
        if 'max' in g and v > g['max']:
            return False
        return True
    j['m1'] = between(r.get('m1'), G.get('m1', {}))
    j['m2'] = between(r.get('m2'), G.get('m2', {}))
    g3 = G.get('m3', {})
    if r.get('m3') is None:
        j['m3'] = None
    else:
        ok = r['m3'] >= g3.get('min', 4.0)
        if r.get('m3_uneven') is not None:
            ok = ok and r['m3_uneven'] <= g3.get('unevenMax', 0.2)
        if r.get('m3_right_left') is not None:
            ok = ok and r['m3_right_left'] >= g3.get('rightLeftMin', 0.8)
        j['m3'] = ok
    j['m4'] = between(r.get('m4'), G.get('m4', {}))
    g5 = G.get('m5', {})
    if r.get('m5') is None:
        j['m5'] = None
    elif r.get('m5_char') is not None and g5.get('charMax', True):
        j['m5'] = r['m5'] <= r['m5_char']
    else:
        j['m5'] = r['m5'] <= g5.get('max', 1500)
    j['m6'] = between(r.get('m6'), G.get('m6', {}))
    j['m7'] = None if r.get('m7') is None else r['m7'] < G.get('m7', {}).get('max', 0.5)
    j['m8'] = r.get('m8_ok')
    g9 = G.get('m9', {})
    if r.get('m9_fs') is None and r.get('m9_contrast') is None:
        j['m9'] = None
    else:
        j['m9'] = (r.get('m9_fs') or 0) >= g9.get('minPx', 32) and (r.get('m9_contrast') or 0) >= g9.get('minContrast', 7.0)
    g10 = G.get('m10', {})
    if r.get('m10_min') is None or (g10.get('pcOnly', True) and r.get('dev') == 'PH'):
        j['m10'] = None
    else:
        t, tol = g10.get('target', 4.0), g10.get('tol', 0.1)
        j['m10'] = (r['m10_min'] >= t - tol) and (r['m10_max'] <= t + tol)
    passed = sum(1 for k in ('m1', 'm2', 'm3', 'm4', 'm5', 'm6', 'm7', 'm8', 'm9') if j.get(k) is True)
    known = sum(1 for k in ('m1', 'm2', 'm3', 'm4', 'm5', 'm6', 'm7', 'm8', 'm9') if j.get(k) is not None)
    need = ((gates or {}).get('rule') or {}).get('passM1toM9', 7)
    j['_passed'] = passed
    j['_known'] = known
    j['_overall'] = (passed >= need) and (j.get('m10') is True) if j.get('m10') is not None else None
    return j

# ------------------------------------------------------------------------------------------ 表


def fmt(v, kind):
    if v is None:
        return '-'
    if kind == 'pct':
        return '%.0f%%' % (v * 100)
    if kind == 'f1':
        return '%.1f' % v
    if kind == 'f2':
        return '%.2f' % v
    if kind == 'int':
        return '%.0f' % v
    return str(v)


COLS = [('m1', '① 中央値', 'f1'), ('m2', '② 暗部', 'pct'), ('m3', '③ 中央÷端', 'f2'), ('m4', '④ 上1/3', 'f1'),
        ('m5', '⑤ 地面', 'int'), ('m6', '⑥ ピント外', 'f2'), ('m7', '⑦ 札の割合', 'pct'), ('m8', '⑧ 体と輪', None),
        ('m9', '⑨ 意図の数字', None), ('m10', '⑩ 1ドット', None)]


def cell(r, key, kind, j):
    if key == 'm8':
        if not r.get('m8_units'):
            v = '-'
        else:
            v = ' '.join('%s %s/%s' % ((u['key'] or '')[:7], fmt(u['body'], 'int'), fmt(u['ring'], 'int')) for u in r['m8_units'][:5])
    elif key == 'm9':
        v = '-' if r.get('m9_fs') is None else '%spx %s:1' % (fmt(r['m9_fs'], 'int'), fmt(r.get('m9_contrast'), 'f1'))
    elif key == 'm10':
        v = '-' if r.get('m10_min') is None else ('%.2f' % r['m10_min'] if r['m10_min'] == r['m10_max'] else '%.2f〜%.2f' % (r['m10_min'], r['m10_max']))
    elif key == 'm3':
        v = fmt(r.get('m3'), kind)
        extra = []
        if r.get('m3_uneven') is not None:
            extra.append('むら%s' % fmt(r['m3_uneven'], 'pct'))
        if r.get('m3_right_left') is not None:
            extra.append('右/左%s' % fmt(r['m3_right_left'], 'f2'))
        if extra:
            v += ' (' + ' '.join(extra) + ')'
    elif key == 'm5':
        v = fmt(r.get('m5'), kind)
        if r.get('m5_char') is not None:
            v += ' (キャラ %s)' % fmt(r['m5_char'], 'int')
    else:
        v = fmt(r.get(key), kind)
    if j is not None and key in j and j[key] is not None:
        v += ' ✓' if j[key] else ' ✗'
    return v


def to_md(res, gates=None, title='HD-2D 見本の数字'):
    lines = ['# ' + title, '', '定義は scripts/hd2d-measure.py の先頭。✓/✗ は門 (gates.json) との比べ。≈ = 近似 (notes 参照)。', '']
    lines.append('| 場面 | ' + ' | '.join(c[1] for c in COLS) + ' | 合格 |')
    lines.append('|' + '---|' * (len(COLS) + 2))
    for k, r in res.items():
        j = judge(r, gates) if gates else None
        approx = ' ≈' if r.get('notes') else ''
        tot = '-' if not j else '%d/%d%s' % (j['_passed'], j['_known'], '' if j['_overall'] is None else (' 合格' if j['_overall'] else ' 不合格'))
        lines.append('| %s%s | ' % (k, approx) + ' | '.join(cell(r, c[0], c[2], j) for c in COLS) + ' | %s |' % tot)
    notes = [(k, r['notes']) for k, r in res.items() if r.get('notes')]
    if notes:
        lines += ['', '## 近似と注記', '']
        for k, n in notes:
            lines.append('- %s: %s' % (k, ' / '.join(n)))
    return '\n'.join(lines) + '\n'

# ------------------------------------------------------------------------------------------ 入口


def measure_dir(d, frame=1, only=None):
    res = {}
    for key, e in sorted(scan_dir(d, frame, only).items()):
        try:
            res[key] = Scene(e).measure()
        except Exception as ex:   # 1枚が壊れていても他は測る
            res[key] = {'error': '%s: %s' % (type(ex).__name__, ex)}
    return res


def measure_refs(d, patches):
    res = {}
    for name, patch in patches.items():
        cands = [os.path.join(d, name + ext) for ext in ('.jpg', '.png', '.jpeg')]
        p = next((c for c in cands if os.path.exists(c)), None)
        if not p:
            continue
        res[name] = RefScene(p, patch).measure()
    return res


def main():
    ap = argparse.ArgumentParser(description='HD-2D 見本の数字 (①〜⑩)')
    ap.add_argument('dir', nargs='?', help='撮影のフォルダ (hd2d-shots.sh の置き場)')
    ap.add_argument('--ref', help='本家の画のフォルダ (hd2d-fetch-ref.py の置き場。既定 ~/.cache/deck-rogue/hd2d-ref)', nargs='?', const=os.path.expanduser('~/.cache/deck-rogue/hd2d-ref'))
    ap.add_argument('--ref-patches', help='本家の矩形の JSON (既定はこのファイルの REF_PATCHES)')
    ap.add_argument('--out', help='数字の JSON')
    ap.add_argument('--md', help='表の md')
    ap.add_argument('--gates', help='門の JSON (docs/design/hd2d-slice/gates.json)')
    ap.add_argument('--write-gates', help='本家3枚から門を作って書く (--ref が要る)')
    ap.add_argument('--frame', type=int, default=1, help='連番の何枚目を測るか (既定 1)')
    ap.add_argument('--only', help='場面の名前の正規表現')
    a = ap.parse_args()
    if not a.dir and not a.ref:
        ap.error('撮影のフォルダか --ref のどちらかが要る')
    patches = REF_PATCHES
    if a.ref_patches:
        with open(a.ref_patches, encoding='utf-8') as f:
            patches = json.load(f)
    res = {}
    refs = {}
    if a.ref:
        refs = measure_refs(a.ref, patches)
        if not refs:
            print('本家の画が見つからない: %s (scripts/hd2d-fetch-ref.py で取る)' % a.ref, file=sys.stderr)
        for k, v in refs.items():
            res['REF-' + k] = v
    if a.dir:
        res.update(measure_dir(a.dir, a.frame, a.only))
    gates = None
    if a.write_gates:
        if not refs:
            ap.error('--write-gates には --ref (本家の画) が要る')
        gates = make_gates(refs)
        os.makedirs(os.path.dirname(os.path.abspath(a.write_gates)), exist_ok=True)
        with open(a.write_gates, 'w', encoding='utf-8') as f:
            json.dump(gates, f, ensure_ascii=False, indent=1)
        print('門を書いた: %s' % a.write_gates)
    elif a.gates:
        with open(a.gates, encoding='utf-8') as f:
            gates = json.load(f)
    if gates:
        for k, r in res.items():
            if 'error' not in r and not k.startswith('REF-'):
                r['judge'] = judge(r, gates)
    if a.out:
        with open(a.out, 'w', encoding='utf-8') as f:
            json.dump(res, f, ensure_ascii=False, indent=1)
    md = to_md({k: v for k, v in res.items() if 'error' not in v}, gates)
    if a.md:
        with open(a.md, 'w', encoding='utf-8') as f:
            f.write(md)
    print(md)
    errs = {k: v['error'] for k, v in res.items() if 'error' in v}
    for k, v in errs.items():
        print('測れなかった: %s %s' % (k, v), file=sys.stderr)
    return 1 if errs else 0


if __name__ == '__main__':
    sys.exit(main())

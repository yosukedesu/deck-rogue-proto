#!/usr/bin/env python3
"""HD-2D 見本 二周目の新しい物差し N1〜N12・N18〜N20・N22・N23 (2026-10-01 レーン F 段1)。

計画 docs/design/hd2d-round2-plan-2026-10-01.md §1-2 の表を1場面ずつ測る。元は作業場の r2/ref/measure_targets.py (本家の分解)。
目標は docs/design/hd2d-slice/r2-targets.json (gates.json は変えない)。N13〜N17 は hd2d-char-metrics.py (段2)・N24 はレーン C の place.py・N25 は apkfix の bgmax.py。

  輝度 = 0.2126R + 0.7152G + 0.0722B (sRGB の値 0〜255 のまま。hd2d-measure.py と同じ)。色は CIELAB (D65)・色相は HSV の色相を彩度で重みを付けた円の平均。
  舞台の画素 = UI なし (hideui) の画から、キャラを除いた物。キャラの除き方は unitsonly の画 (マゼンタ以外) を 4px 太らせたマスク。
    unitsonly が無い時 (試しの撮影は hideui だけ) は layout.json の unitBoxes の板の矩形 (4px 足す) で除く (結果の charMask に書く)。
  帯の位置は主人公の足元の行からの相対 (layout.json の stage.camera.seats の player の px)。スマホ相当の画は主人公の1ドットの px (pxPerDot)÷4 = sc で
    足元からの距離と幅を縮める (W5 の PH は sc 0.787)。画面の上からの行で決める物 (N5・N6・N20・N23) は画の高さ H÷1080 で縮める。
  本家 (--ref ot16 など) は UI とキャラを矩形で除く (矩形は下の REFS)。ot16 は層の矩形 (REF_RECTS) で N8・N8b・N9・N10・N12・N18 を測る＝本家の分解 r2/ref/targets.md と同じ所。

物差し (PC の値。スマホは上の sc か H/1080 で縮める)
  N1  霧の帯のいちばん明るい行: 中央の列 (x700〜1220。本家は 900〜1300) の 10 行ごとの平均 (キャラを除く) を5つの窓でならした折れ線の最大の行。
      feetMinusPeak = 足元の行 − 頂点の行 (目標は足元からの上の距離)
  N2  帯の太さ: 頂点と「頂点の150px上より上の中央値」の間の半分の明るさまで上へ、「頂点の200〜400px下の中央値」との半分まで下へ (measure_targets と同じ)
  N3  帯の頂点の明るさ
  N4  地平線の行・画面の端 (中心から ±900px) の縦の傾き: layout.json の stage.camera の horizonRow・edgeTiltDeg (レーン A)。無ければ
      renderEuler の x (見下ろし) と画角から計算 (H/2 − f·tan(見下ろし)・atan(900·tan(見下ろし)/f)、f = (H/2)/tan(画角/2))
  N5  上の40% (行 0〜430) の暗い塊 (<60)・明るい霞 (>130) の割合・細かさ (σ1 でぼかした輝度の勾配の平均)
  N6a 上の真ん中 (中央の列・行 0〜300) の中央値 ÷ N3。N6b 上の両端 (x0〜300 と x1620〜1920・行 0〜300) の中央値 ÷ N3 (左・右・大きい方・小さい方)
  N6c (参考・合否なし。段2 で足した) 両端の内側 x300〜600・行 0〜300 の中央値 ÷ N3 (右の対 x1320〜1620 も)。W5 は両端 x0〜300 が額縁と減光で既に暗く
      N6b では 0.11〜0.14 と出るので、計画の表の「W5 0.7前後」に当たるこの列を並べて読む
  N7  霧の帯 (N1 の頂点 ±80px) を縦に横切る暗い筋: 帯の中の列ごとの中央値が、左右 241px の中央値 (ならした地) の 0.85 倍より暗く、幅 12px 以上続く所。
      両端の太い幹 = x<300 か x>1620 で、列の中央値が帯の行の中央値の 0.4 倍以下で 40px 以上続く所 (輝度を書く。目標 15〜35)
  N8  キャラの後ろ (足元の 250〜60px 上・中央の列) の中央値 ÷ N3 と、p90−p10
  N8b 主人公の後ろの窓 (主人公の足元の x ±130px・足元の 274〜94px 上) の中央値。unitsonly があれば主人公の体 (中央値) − 窓
  N9  座席の地面の中央 (x600〜1200・足元の 65px 上〜95px 下) の中央値
  N10 暗い所の色: 両端 (x0〜160 と x1760〜1920・行 0〜700) の色相・C*・a*／手前 (足元の 130px 下より下) の色相・C*・輝度の p90
  N11 明るい灰 (L*>45 かつ C*<10) の割合 (舞台の画素の中)
  N12 ぼけ (鋭い縁の割合・縁の幅 p10。measure_targets の sharp_frac。キャラと UI に掛かる縁は数えない): キャラのすぐ後ろ (x560〜1260・足元の 280〜120px 上)／
      手前 (足元の 130px 下より下)／奥の霧の層 (x400〜1500・N1 の頂点の 95px 上〜105px 下)
  N18 地面の細かい明るい点: 7×7 の中央値より 18 以上明るい画素の数 (1万画素あたり)。土 = x600〜1200・足元の 25px 上〜35px 下、草の縁 = x200〜1400・足元の 60〜120px 下
  N19 月光の筋: 筋の中 (軸から ±75px) の中央値 ÷ すぐ横 (軸から 112〜188px の両側を合わせた) の中央値。行 0〜500。
      筋の軸は設計図の shaft の部品 (layout.json の extra.diorama.layout の JSON) を layout.json のカメラで画面へ写した線。
      斜め (傾き |dx/dy|>0.2) で上端が画面の左半分の物 = 左上、|dx/dy|≤0.2 = 縦の筋。設計図が読めない時は線を探す (結果の shafts.mode)
  N20 上の小さな光の粒 (行 180〜360): σ4 でぼかした画との差 30 以上・周りの輪 (半径 5・7) の p90 より 15 以上明るい・輝度 80 以上で輪の中央値の 1.2 倍以上・
      大きさ 60 画素以下で 9×9 に収まる塊の数。明るい細部を持つ部品 (羊歯・草・花・きのこ・落ち葉ほか。幹と針葉の枝は除く) の画面の箱の中の点は数えない
      (docs/design/hd2d-slice/n20-part-boxes.json・直しの輪2。葉の明るい所を粒に数えていた)
  N22 奥の端の段差: 中央の列の 4px ごとの中央値で、行 地平線−27〜地平線+73 (スマホ −23〜+37。本家は計画の表どおり行 270〜370) の隣どうしの差の最大
  N23 上端の暗さ: 行 0〜60 の両端 (x0〜300・x1620〜1920) の中央値
  K13 人形の後列 (人形が6体以上の時): 後列 (5体目より後) の頭が、画面の x がいちばん近い前列の頭より何 px 上か (頭 = 板の上端 + 絵の上の透明な行 × 1ドットの px)

使い方
  python3 scripts/hd2d-r2-targets.py <撮影のフォルダ> <場面>        # <場面>-hideui-1.png (無ければ <場面>-1.png) と -unitsonly-1.png と .layout.json
  python3 scripts/hd2d-r2-targets.py --image <画.png> [--layout <.layout.json>] [--units <unitsonly.png>]
  python3 scripts/hd2d-r2-targets.py --ref ot16                          # 本家 (ot16|ot7|ot11|ot2|ot13|ot8。--ref-dir で置き場を足す)
  python3 scripts/hd2d-r2-targets.py --all <撮影のフォルダ>               # UI なしの画を全部 (unitsonly・uionly の画は除く)
  共通: [--targets docs/design/hd2d-slice/r2-targets.json] [--json 出力.json] [--md 出力.md] [--layout-json 設計図.json (N19 の筋を別の設計図で)]
  W5 の画を測る時は設計図が今と違うかもしれないので --layout-json unity/Assets/Resources/Stage/act1_layout_w5.json (レーン C の写し) を付ける。
"""
import argparse
import glob
import json
import math
import os
import re
import sys
import warnings

import numpy as np
from PIL import Image
from numpy.lib.stride_tricks import sliding_window_view as _sw

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(HERE)
RES = os.path.join(REPO, 'unity', 'Assets', 'Resources')
DEFAULT_TARGETS = os.path.join(REPO, 'docs', 'design', 'hd2d-slice', 'r2-targets.json')
# N20 の数え違いを消す箱 (直しの輪2・2026-10-01): 明るい細部を持つ部品 (羊歯・草・花・きのこ・落ち葉ほか。幹・針葉の枝は除く) の画面の箱。
# キー = 設計図|画角|見下ろし|足元の線|端末 (hd2d-measure.py の⑥の近い木と同じ)。--part-boxes none で使わない (直しの輪1 までと同じ数え方)
DEFAULT_PART_BOXES = os.path.join(REPO, 'docs', 'design', 'hd2d-slice', 'n20-part-boxes.json')
PART_BOXES = None   # 初めて要る時に DEFAULT_PART_BOXES を読む ({} = 使わない)
DEFAULT_REF_DIRS = [os.path.expanduser('~/.cache/deck-rogue/hd2d-ref')]

# ---- 本家の矩形 [x, y, w, h] (r2/ref/work/common.py の REFS の写し。1920×1080)
REFS = {
    'ot16': dict(file='ot_921570_16.jpg', title='OT1 夜の森 (ハンイット)',
                 ui=[[37, 22, 968, 143], [1642, 30, 248, 142], [428, 777, 217, 63], [860, 130, 110, 30]],
                 chars=[[165, 205, 730, 580], [1312, 632, 125, 158]], hero=[1312, 638, 120, 148], heroHead=640, heroFeet=783),
    'ot7': dict(file='ot_921570_7.jpg', title='OT1 夜の洞窟 (トレサ)',
                ui=[[37, 22, 968, 143], [1560, 30, 278, 142], [1087, 588, 605, 230], [450, 787, 180, 61], [860, 130, 110, 30]],
                chars=[[397, 382, 286, 398], [960, 618, 100, 150]], hero=[960, 622, 98, 143], heroHead=622, heroFeet=762),
    'ot11': dict(file='ot_921570_11.jpg', title='OT1 昼の村 (ハンイット)',
                 ui=[[37, 22, 968, 143], [897, 172, 126, 60], [1545, 22, 293, 155], [487, 795, 211, 57], [860, 130, 110, 30]],
                 chars=[[558, 637, 97, 135], [904, 637, 108, 131], [1095, 510, 435, 322]], hero=[904, 637, 108, 131], heroHead=640, heroFeet=765),
    'ot13': dict(file='ot_921570_13.jpg', title='OT1 屋内 館 (テリオン)',
                 ui=[[37, 22, 1340, 150], [1640, 30, 250, 142], [190, 740, 520, 150]],
                 chars=[[170, 555, 230, 265], [440, 285, 300, 455], [1375, 605, 100, 190]], hero=[1380, 615, 90, 175], heroHead=642, heroFeet=790),
    'ot2': dict(file='ot_921570_2.jpg', title='OT1 洞窟 たいまつ (オルベリク)',
                ui=[[37, 22, 1340, 150], [1540, 30, 300, 142], [80, 725, 650, 160]],
                chars=[[0, 150, 950, 700], [960, 610, 110, 160]], hero=[970, 620, 100, 150], heroHead=622, heroFeet=762),
    'ot8': dict(file='ot_921570_8.jpg', title='OT1 昼の砂漠 (プリムローズ)',
                ui=[[37, 22, 1340, 150], [1520, 30, 300, 142], [880, 170, 150, 65], [350, 730, 230, 70]],
                chars=[[0, 130, 730, 620], [1100, 580, 150, 150]], hero=[1110, 585, 130, 140], heroHead=590, heroFeet=722),
}
# 本家で層の矩形を使う物差し (r2/ref/targets.md §1 の層。ot16 だけ)。地平線・筋は目で読んだ値と、見つけた線
REF_RECTS = {
    'ot16': dict(
        N8=[880, 520, 420, 120],                   # ③ 奥の土手 (キャラのすぐ後ろ)
        N8b=[880, 520, 420, 120],                  # 主人公は小さいので、主人公の後ろの窓 = 同じ土手
        N9=[880, 640, 420, 150],                   # ④ 座席
        N10side=[[0, 0, 160, 700]],                # ① 両端の暗い木 (右端は UI とボス)
        N10front=[0, 880, 1920, 200],              # ⑤ 手前の草
        N12behind=[880, 520, 420, 120], N12front=[0, 880, 1920, 200], N12fog=[850, 380, 600, 140],
        N18dirt=[880, 640, 420, 150], N18grass=[0, 800, 1920, 40],   # 土 = ④ 座席・草の縁 = 道の手前の縁
        N19=[dict(name='左上の斜めの筋', x0=180, k=0.9), dict(name='主人公の側の縦の筋', x0=1240, k=0.2)],
        horizon=455, horizonNote='地平線は霧の芯の行 (目で読んだ値。館 ot13 の床の線は 403)', edgeTiltDeg=0.3),
}

# ---- 基本の道具


def lum(a):
    return 0.2126 * a[..., 0] + 0.7152 * a[..., 1] + 0.0722 * a[..., 2]


def load(p):
    return np.asarray(Image.open(p).convert('RGB')).astype(np.float32)


def rectmask(shape, rects, pad=0):
    m = np.zeros(shape[:2], bool)
    for x, y, w, h in rects:
        x, y, w, h = int(round(x)), int(round(y)), int(round(w)), int(round(h))
        m[max(0, y - pad):max(0, y + h + pad), max(0, x - pad):max(0, x + w + pad)] = True
    return m


def dilate(m, n):
    o = m.copy()
    for _ in range(n):
        p = o.copy()
        p[1:] |= o[:-1]; p[:-1] |= o[1:]; p[:, 1:] |= o[:, :-1]; p[:, :-1] |= o[:, 1:]
        o = p
    return o


def gblur(g, s):
    if s <= 0:
        return g
    r = int(3 * s) + 1
    x = np.arange(-r, r + 1); k = np.exp(-x * x / (2 * s * s)); k /= k.sum()
    p = np.pad(g, ((r, r), (r, r)), mode='reflect')
    t = np.zeros((p.shape[0], g.shape[1]), np.float32)
    for i, kv in enumerate(k):
        t += kv * p[:, i:i + g.shape[1]]
    o = np.zeros_like(g)
    for i, kv in enumerate(k):
        o += kv * t[i:i + g.shape[0], :]
    return o


def srgb2lin(c):
    c = c / 255.0
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def rgb2lab(a):
    l = srgb2lin(a)
    M = np.array([[0.4124, 0.3576, 0.1805], [0.2126, 0.7152, 0.0722], [0.0193, 0.1192, 0.9505]])
    xyz = l @ M.T; xyz /= np.array([0.95047, 1.0, 1.08883])
    f = np.where(xyz > 0.008856, np.cbrt(xyz), 7.787 * xyz + 16 / 116)
    return np.stack([116 * f[..., 1] - 16, 500 * (f[..., 0] - f[..., 1]), 200 * (f[..., 1] - f[..., 2])], -1)


def hsv(a):
    r, g, b = a[..., 0] / 255, a[..., 1] / 255, a[..., 2] / 255
    mx = np.maximum(np.maximum(r, g), b); mn = np.minimum(np.minimum(r, g), b); d = mx - mn
    h = np.zeros_like(mx); m = d > 1e-6
    rc = (mx == r) & m; gc = (mx == g) & m & ~rc; bc = m & ~rc & ~gc
    h[rc] = ((g - b)[rc] / d[rc]) % 6; h[gc] = ((b - r)[gc] / d[gc]) + 2; h[bc] = ((r - g)[bc] / d[bc]) + 4
    h *= 60; s = np.where(mx > 0, d / np.maximum(mx, 1e-6), 0)
    return h, s, mx


def rnd(v, n=3):
    if v is None:
        return None
    if isinstance(v, (float, np.floating)):
        if not np.isfinite(v):
            return None
        return round(float(v), n)
    if isinstance(v, (np.integer,)):
        return int(v)
    return v


def clip_rect(rect, W, H):
    x, y, w, h = [int(round(v)) for v in rect]
    x0, y0, x1, y1 = max(0, x), max(0, y), min(W, x + w), min(H, y + h)
    return x0, y0, max(x0, x1), max(y0, y1)


def region_vals(L, valid, rect):
    H, W = L.shape
    x0, y0, x1, y1 = clip_rect(rect, W, H)
    blk = L[y0:y1, x0:x1][valid[y0:y1, x0:x1]]
    return blk


def med(v):
    return float(np.median(v)) if v.size else None


def sharp_frac(g, rect, minC=14, valid=None):
    """鋭い縁の割合と縁の幅 p10 (measure_targets の sharp_frac と同じ。valid を渡すと、キャラの画素に掛かる縁は数えない)"""
    H, W = g.shape
    x0, y0, x1, y1 = clip_rect(rect, W, H)
    reg = g[y0:y1, x0:x1].astype(np.float32); h, w = reg.shape; ws = []
    vr = valid[y0:y1, x0:x1] if valid is not None else np.ones_like(reg, bool)
    profs = [(reg[j, :], vr[j, :]) for j in range(0, h, 2)] + [(reg[:, i], vr[:, i]) for i in range(0, w, 2)]
    for prof, pv in profs:
        n = len(prof)
        if n < 40:
            continue
        d = np.zeros(n); d[1:-1] = prof[2:] - prof[:-2]; ad = np.abs(d); i = 16
        while i < n - 16:
            if ad[i] >= 8 and ad[i] == ad[i - 2:i + 3].max() and pv[i - 8:i + 9].all():
                seg = prof[i - 8:i + 9]; a0 = seg[:3].mean(); a1 = seg[-3:].mean(); C = a1 - a0
                if abs(C) >= minC:
                    s = (seg - a0) / C; l = 8
                    while l > 0 and s[l] > 0.1:
                        l -= 1
                    r = 8
                    while r < 16 and s[r] < 0.9:
                        r += 1
                    ws.append(r - l if (0 < l and r < 16) else 17)
                i += 3
                continue
            i += 1
    if not ws:
        return None
    ws = np.array(ws)
    return dict(sharp=float((ws <= 3).mean()), p10=float(np.percentile(ws, 10)), n=int(len(ws)))


def color_of(a, lab_all, hue_s, mask):
    if mask.sum() < 100:
        return None
    lab = lab_all[mask]; h, s = hue_s; ang = np.deg2rad(h[mask]); ss = s[mask]
    hue = float(np.rad2deg(np.arctan2((ss * np.sin(ang)).sum(), (ss * np.cos(ang)).sum())) % 360)
    return dict(hue=hue, Lstar=float(lab[:, 0].mean()), a=float(lab[:, 1].mean()), b=float(lab[:, 2].mean()),
                C=float(np.hypot(lab[:, 1], lab[:, 2]).mean()), rgb=[int(round(q)) for q in a[mask].mean(0)])


def runs(b):
    out = []; i = 0; n = len(b)
    while i < n:
        if b[i]:
            j = i
            while j < n and b[j]:
                j += 1
            out.append((i, j)); i = j
        else:
            i += 1
    return out


def running_median(v, win):
    r = win // 2
    return np.median(_sw(np.pad(v, r, mode='edge'), 2 * r + 1), axis=-1)


def label8(mask):
    """8 近傍の塊 (union-find)。戻り値 (ys, xs, 根)"""
    H, W = mask.shape
    ys, xs = np.nonzero(mask); n = len(ys)
    if n == 0:
        return ys, xs, np.zeros(0, int)
    idx = -np.ones((H, W), np.int64); idx[ys, xs] = np.arange(n)
    parent = np.arange(n)

    def find(i):
        while parent[i] != i:
            parent[i] = parent[parent[i]]; i = parent[i]
        return i
    for dy, dx in ((0, 1), (1, -1), (1, 0), (1, 1)):
        y2 = ys + dy; x2 = xs + dx; ok = (y2 < H) & (x2 >= 0) & (x2 < W)
        a = np.nonzero(ok)[0]; b = idx[y2[ok], x2[ok]]; sel = b >= 0
        for i, j in zip(a[sel], b[sel]):
            ri, rj = find(i), find(j)
            if ri != rj:
                parent[ri] = rj
    return ys, xs, np.array([find(i) for i in range(n)])

# ---- カメラ (layout.json の stage.camera。Unity の Euler は Y・X・Z の順)


def cam_rot(e):
    ex, ey, ez = [math.radians(v) for v in e]
    Rx = np.array([[1, 0, 0], [0, math.cos(ex), -math.sin(ex)], [0, math.sin(ex), math.cos(ex)]])
    Ry = np.array([[math.cos(ey), 0, math.sin(ey)], [0, 1, 0], [-math.sin(ey), 0, math.cos(ey)]])
    Rz = np.array([[math.cos(ez), -math.sin(ez), 0], [math.sin(ez), math.cos(ez), 0], [0, 0, 1]])
    return Ry @ Rx @ Rz


def cam_project(c, X):
    """世界の点 → 画面の px (x, 上からの行, 深さ)。レンダーの位置と向きを使う (座席の px と一致を確かめてある)"""
    R = cam_rot(c.get('renderEuler') or c.get('layoutEuler') or [c.get('pitch', 12), 0, 0])
    P = np.array(c.get('renderPos') or c.get('camBase'))
    W, H = c.get('screen') or [1920, 1080]
    f = R @ [0, 0, 1]; u = R @ [0, 1, 0]; r = R @ [1, 0, 0]
    v = np.asarray(X, float) - P; d = v @ f
    th = math.tan(math.radians(c['fov']) / 2)
    return ((v @ r) / (d * th * W / H) + 1) / 2 * W, (1 - (v @ u) / (d * th)) / 2 * H, d


def cam_numbers(c, H):
    """地平線の行と画面の端の縦の傾き。レーン A の値 (horizonRow・edgeTiltDeg) があればそれ"""
    pitch = (c.get('renderEuler') or [c.get('pitch', 0)])[0]
    fpx = (H / 2) / math.tan(math.radians(c['fov']) / 2)
    hz = H / 2 - fpx * math.tan(math.radians(pitch))
    tilt = math.degrees(math.atan(900 * math.tan(math.radians(pitch)) / fpx))
    out = dict(pitch=rnd(pitch, 2), fov=c['fov'], horizonRowCalc=rnd(hz, 1), edgeTiltDegCalc=rnd(tilt, 2))
    out['horizonRow'] = rnd(c['horizonRow'], 1) if c.get('horizonRow') is not None else out['horizonRowCalc']
    out['edgeTiltDeg'] = rnd(c['edgeTiltDeg'], 2) if c.get('edgeTiltDeg') is not None else out['edgeTiltDegCalc']
    out['source'] = 'layout (レーン A)' if c.get('horizonRow') is not None else '見下ろしと画角から計算'
    return out


def on_path(yaw_deg, t, s, y):
    yw = math.radians(yaw_deg)
    return np.array([t * math.cos(yw) + s * math.sin(yw), y, -t * math.sin(yw) + s * math.cos(yw)])

# ---- 1枚を読む


class Shot:
    """測る画1枚 (撮影か本家)。L = 輝度 (キャラと UI は NaN)・valid = 舞台の画素"""

    def __init__(self):
        self.notes = []


def shot_from_files(img, layout=None, units=None):
    s = Shot(); s.kind = 'sample'; s.path = img
    s.a = load(img); H, W = s.a.shape[:2]; s.H, s.W = H, W
    s.lay = json.load(open(layout)) if layout and os.path.exists(layout) else None
    s.name = os.path.basename(img).replace('.png', '')
    st = (s.lay or {}).get('state', '')
    if s.lay and 'hideui=1' not in st:
        s.notes.append('UI がある画 (hideui=1 でない) = 数字は参考')
    cam = (s.lay or {}).get('stage', {}).get('camera', {}) if s.lay else {}
    s.cam = cam
    s.phone = bool((s.lay or {}).get('canvas', {}).get('phone')) if s.lay else (H < 1000)
    s.dev = 'PH' if s.phone else 'PC'
    ub = {u['key']: u for u in ((s.lay or {}).get('stage', {}).get('unitBoxes') or [])}
    seats = {x['key']: x for x in (cam.get('seats') or [])}
    s.unitBoxes = ub; s.seats = seats
    pl = seats.get('player')
    if pl:
        s.heroX, s.feet = float(pl['px'][0]), float(pl['px'][1])
    elif 'player' in ub:
        x, y, w, h = ub['player']['boardPx']; s.heroX, s.feet = x + w / 2, y + h - ub['player'].get('feetPadPx', 0)
        s.notes.append('足元は板の下端から (seats が無い)')
    else:
        s.heroX, s.feet = 438.0, 664.6 * H / 1080; s.notes.append('主人公が見つからない = 足元は W5 の値')
    pxd = (ub.get('player') or {}).get('pxPerDot')
    s.sc = (pxd / 4.0) if pxd else H / 1080.0
    s.center = (700, 1220)
    # キャラのマスク
    if units and os.path.exists(units):
        u = load(units)
        mag = (u[..., 0] > 240) & (u[..., 1] < 20) & (u[..., 2] > 240)
        s.charm = ~mag; s.excl = dilate(s.charm, 4); s.charMask = 'unitsonly を 4px 太らせた物'
    else:
        rects = [x['boardPx'] for x in ub.values() if x.get('boardPx')]
        s.charm = rectmask(s.a.shape, rects, 0); s.excl = rectmask(s.a.shape, rects, 4)
        s.charMask = 'unitBoxes の板の矩形 +4px (unitsonly が無い)' if rects else 'なし (layout に unitBoxes が無い)'
    # 主人公の頭
    s.head = None
    if 'player' in ub and s.charMask.startswith('unitsonly'):
        x, y, w, h = [int(round(v)) for v in ub['player']['boardPx']]
        mm = s.charm[max(0, y - 40):y + h + 10, max(0, x - 10):x + w + 10]
        ys, _ = np.nonzero(mm)
        if len(ys):
            s.head = int(ys.min() + max(0, y - 40))
    if s.head is None and 'player' in ub:
        s.head = float(ub['player']['boardPx'][1]); s.headNote = '板の上端 (見込み)'
    s.refRects = {}
    return s


def shot_from_ref(key, ref_dirs):
    d = REFS[key]; path = None
    for rd in ref_dirs:
        p = os.path.join(rd, d['file'])
        if os.path.exists(p):
            path = p; break
    if not path:
        raise SystemExit('本家の画が無い: %s (--ref-dir で置き場を足す)' % d['file'])
    s = Shot(); s.kind = 'ref'; s.path = path; s.name = key; s.title = d['title']
    s.a = load(path); s.H, s.W = s.a.shape[:2]
    s.excl = rectmask(s.a.shape, d['ui'], 4) | rectmask(s.a.shape, d['chars'], 6)
    s.charm = rectmask(s.a.shape, d['chars'], 0); s.charMask = '本家の矩形 (UI +4・キャラ +6)'
    s.lay = None; s.cam = {}; s.phone = False; s.dev = 'PC'; s.sc = 1.0
    s.feet = float(d['heroFeet']); s.head = float(d['heroHead']); s.heroX = d['hero'][0] + d['hero'][2] / 2
    s.center = (900, 1300); s.unitBoxes = {}; s.seats = {}
    s.refRects = REF_RECTS.get(key, {})
    return s

# ---- 測る


def profile(s, L):
    x0, x1 = s.center; H = s.H
    prof = []
    for y in range(0, H, 10):
        blk = L[y:y + 10, x0:x1]
        prof.append(float(np.nanmean(blk)) if np.isfinite(blk).sum() > 50 else np.nan)
    prof = np.array(prof)
    sm = np.convolve(np.nan_to_num(prof, nan=np.nanmean(prof)), np.ones(5) / 5, mode='same')
    return prof, sm


def measure(s, layout_json=None):
    H, W = s.H, s.W; a = s.a; sc = s.sc; hs = H / 1080.0
    L = lum(a); Ln = L.copy(); Ln[s.excl] = np.nan; valid = ~s.excl
    r = dict(name=s.name, kind=s.kind, dev=s.dev, path=s.path, size=[W, H], sc=rnd(sc), charMask=s.charMask,
             feet=rnd(s.feet, 1), head=rnd(s.head, 1) if s.head is not None else None, heroX=rnd(s.heroX, 1), notes=s.notes)
    if s.lay:
        r['state'] = s.lay.get('state'); r['flags'] = {k: s.lay.get('flags', {}).get(k) for k in ('cam', 'pitch', 'pitchphone', 'groundline', 'look', 'herodots', 'tier')}
    x0, x1 = s.center
    # N1〜N3 (measure_targets と同じ)
    prof, sm = profile(s, Ln)
    ip = int(np.nanargmax(sm[3:-3])) + 3; peak = float(sm[ip]); ypk = ip * 10 + 5
    base_hi = float(np.nanmedian(prof[:max(1, ip - 15)])) if ip > 15 else float(prof[0])
    half = (peak + base_hi) / 2
    lo = ip
    while lo > 0 and sm[lo] > half:
        lo -= 1
    hi = ip
    base_lo = np.nanmedian(prof[min(len(prof) - 1, ip + 20):ip + 40]) if ip + 20 < len(prof) else prof[-1]
    while hi < len(sm) - 1 and sm[hi] > (peak + base_lo) / 2:
        hi += 1
    r['profile'] = dict(rows=[y for y in range(0, H, 10)], mean=[rnd(v, 1) for v in prof], smooth=[rnd(v, 1) for v in sm], cols=[x0, x1])
    r['N1'] = dict(peakRow=ypk, feetMinusPeak=rnd(s.feet - ypk, 1), headMinusPeak=rnd(s.head - ypk, 1) if s.head is not None else None)
    r['N2'] = dict(fwhm=(hi - lo) * 10, band=[lo * 10, hi * 10])
    r['N3'] = dict(peak=rnd(peak, 1))
    # N4
    if s.kind == 'ref':
        rr = s.refRects
        r['N4'] = dict(horizonRow=rr.get('horizon'), edgeTiltDeg=rr.get('edgeTiltDeg'), source=rr.get('horizonNote', '本家は測れない'))
    elif s.cam.get('fov'):
        r['N4'] = cam_numbers(s.cam, H)
    else:
        r['N4'] = dict(horizonRow=None, edgeTiltDeg=None, source='カメラの値が無い')
    hz = r['N4'].get('horizonRow')
    # N5
    y40 = int(round(430 * hs)); m = valid
    up = L[:y40][m[:y40]]
    g = gblur(L, 1.0); gx = np.zeros_like(g); gy = np.zeros_like(g)
    gx[:, 1:-1] = g[:, 2:] - g[:, :-2]; gy[1:-1] = g[2:] - g[:-2]
    r['N5'] = dict(rows=[0, y40], darkLt60=rnd(float((up < 60).mean())), hazeGt130=rnd(float((up > 130).mean())),
                   meanGrad=rnd(float(np.hypot(gx, gy)[:y40][m[:y40]].mean()), 2))
    # N6
    y30 = int(round(300 * hs))
    mid = med(region_vals(L, valid, [x0, 0, x1 - x0, y30]))
    lft = med(region_vals(L, valid, [0, 0, 300, y30])); rgt = med(region_vals(L, valid, [1620, 0, 300, y30]))
    r['N6a'] = dict(median=rnd(mid, 1), ratio=rnd(mid / peak if mid is not None else None))
    lr = [v / peak for v in (lft, rgt) if v is not None]
    r['N6b'] = dict(left=rnd(lft, 1), right=rnd(rgt, 1), leftRatio=rnd(lft / peak if lft is not None else None), rightRatio=rnd(rgt / peak if rgt is not None else None),
                    max=rnd(max(lr)) if lr else None, min=rnd(min(lr)) if lr else None)
    # N6c (参考・合否なし。2026-10-01 段2 の統合の裁定): 両端 (x0〜300) は額縁と減光で W5 でも暗い。その内側の列 x300〜600 (と右の対 x1320〜1620)
    lc = med(region_vals(L, valid, [300, 0, 300, y30])); rc = med(region_vals(L, valid, [1320, 0, 300, y30]))
    r['N6c'] = dict(left=rnd(lc, 1), right=rnd(rc, 1), ratio=rnd(lc / peak if lc is not None else None), rightRatio=rnd(rc / peak if rc is not None else None))
    # N7
    half_b = int(round(80 * sc)); b0, b1 = max(0, ypk - half_b), min(H, ypk + half_b)
    B = Ln[b0:b1]
    with warnings.catch_warnings():
        warnings.simplefilter('ignore', RuntimeWarning)   # キャラで全部隠れた列 = NaN のまま
        rowmed = float(np.nanmedian(B))
        colmed = np.nanmedian(B, axis=0)
    fin = np.isfinite(colmed)
    cm = np.where(fin, colmed, np.nanmedian(colmed) if fin.any() else 0)
    base = running_median(cm, 241)
    wmin = max(6, int(round(12 * sc)))
    streaks = []
    for x_a, x_b in runs((cm < 0.85 * base) & fin):
        if x_b - x_a >= wmin:
            streaks.append(dict(x=[int(x_a), int(x_b)], width=int(x_b - x_a), ratio=rnd(float(np.median(cm[x_a:x_b] / base[x_a:x_b])), 2),
                                lum=rnd(float(np.median(cm[x_a:x_b])), 1)))
    weak = [dict(x=[int(a_), int(b_)], ratio=rnd(float(np.median(cm[a_:b_] / base[a_:b_])), 2)) for a_, b_ in runs((cm < 0.95 * base) & (cm >= 0.85 * base) & fin) if b_ - a_ >= wmin]
    thick = []
    tw = max(20, int(round(40 * sc)))
    for x_a, x_b in runs((cm <= 0.4 * rowmed) & fin):
        if x_b - x_a >= tw and (x_a < 300 or x_b > 1620):
            thick.append(dict(x=[int(x_a), int(x_b)], width=int(x_b - x_a), lum=rnd(float(np.median(cm[x_a:x_b])), 1)))
    inrange = [q for q in streaks if 0.6 <= q['ratio'] <= 0.85]
    best_thick = None
    if thick:
        best_thick = min(thick, key=lambda q: abs(q['lum'] - 25))['lum']
    r['N7'] = dict(band=[b0, b1], rowMedian=rnd(rowmed, 1), count=len(streaks), countInRange=len(inrange), streaks=streaks, weakDips=weak,
                   thick=thick, thickBestLum=best_thick)
    # N8・N8b
    rr = s.refRects
    if 'N8' in rr:
        v = region_vals(L, valid, rr['N8'])
        r['N8'] = dict(rect=rr['N8'])
    else:
        ry0, ry1 = int(round(s.feet - 250 * sc)), int(round(s.feet - 60 * sc))
        v = region_vals(L, valid, [x0, ry0, x1 - x0, ry1 - ry0]); r['N8'] = dict(rect=[x0, ry0, x1 - x0, ry1 - ry0])
    if v.size:
        r['N8'].update(median=rnd(float(np.median(v)), 1), ratio=rnd(float(np.median(v)) / peak), spread=rnd(float(np.percentile(v, 90) - np.percentile(v, 10)), 1))
    if 'N8b' in rr:
        rect = rr['N8b']
    else:
        rect = [s.heroX - 130 * sc, s.feet - 274 * sc, 260 * sc, 180 * sc]
    v = region_vals(L, valid, rect)
    r['N8b'] = dict(rect=[rnd(q, 0) for q in rect], median=rnd(med(v), 1),
                    p10=rnd(float(np.percentile(v, 10)), 1) if v.size else None, p90=rnd(float(np.percentile(v, 90)), 1) if v.size else None,
                    validFrac=rnd(v.size / max(1.0, rect[2] * rect[3])))
    if s.kind == 'sample' and s.charMask.startswith('unitsonly') and 'player' in s.unitBoxes:
        x, y, w, h = [int(round(q)) for q in s.unitBoxes['player']['boardPx']]
        body = np.zeros((H, W), bool); body[max(0, y):y + h, max(0, x):x + w] = True; body &= s.charm
        if body.sum() > 200 and r['N8b']['median'] is not None:
            hb = float(np.median(L[body])); r['N8b']['heroBody'] = rnd(hb, 1); r['N8b']['heroMinusWindow'] = rnd(hb - r['N8b']['median'], 1)
    # N9
    rect9 = rr.get('N9') or [600, s.feet - 65 * sc, 600, 160 * sc]
    v = region_vals(L, valid, rect9)
    r['N9'] = dict(rect=[rnd(q, 0) for q in rect9], median=rnd(med(v), 1))
    # N10
    lab_all = rgb2lab(a); h_, s_, _ = hsv(a)
    sides = rr.get('N10side') or [[0, 0, 160, 700 * hs], [W - 160, 0, 160, 700 * hs]]
    ms = rectmask(a.shape, sides) & valid
    frect = rr.get('N10front') or [0, s.feet + 130 * sc, W, H]
    mf = rectmask(a.shape, [frect]) & valid
    cs = color_of(a, lab_all, (h_, s_), ms); cf = color_of(a, lab_all, (h_, s_), mf)
    if cf is not None:
        cf['p90'] = rnd(float(np.percentile(L[mf], 90)), 1)
    r['N10'] = dict(side=cs, front=cf, sideRects=[[rnd(q, 0) for q in x] for x in sides], frontRect=[rnd(q, 0) for q in frect])
    # N11
    Ls = lab_all[..., 0][valid]; Cs = np.hypot(lab_all[..., 1], lab_all[..., 2])[valid]
    r['N11'] = dict(greyBright=rnd(float(((Ls > 45) & (Cs < 10)).mean()), 4))
    # N12
    beh = rr.get('N12behind') or [560, s.feet - 280 * sc, 700, 160 * sc]
    frn = rr.get('N12front') or [0, s.feet + 130 * sc, W, H]
    fog = rr.get('N12fog') or [400, ypk - 95 * sc, 1100, 200 * sc]
    r['N12'] = {k: dict(rect=[rnd(q, 0) for q in rc], **(sharp_frac(L, rc, valid=valid) or {})) for k, rc in (('behind', beh), ('front', frn), ('fog', fog))}
    # N18
    def specks(rect):
        x0_, y0_, x1_, y1_ = clip_rect(rect, W, H)
        if x1_ - x0_ < 8 or y1_ - y0_ < 8:
            return None
        sub = np.pad(L[y0_:y1_, x0_:x1_], 3, mode='reflect')
        M7 = np.median(_sw(sub, (7, 7)), axis=(-2, -1))
        sp = (L[y0_:y1_, x0_:x1_] - M7) >= 18
        vv = valid[y0_:y1_, x0_:x1_]
        return rnd(float(sp[vv].mean() * 1e4), 1) if vv.sum() > 500 else None
    dirt = rr.get('N18dirt') or [600, s.feet - 25 * sc, 600, 60 * sc]
    grass = rr.get('N18grass') or [200, s.feet + 60 * sc, 1200, 60 * sc]
    r['N18'] = dict(dirt=specks(dirt), grass=specks(grass), dirtRect=[rnd(q, 0) for q in dirt], grassRect=[rnd(q, 0) for q in grass])
    # N19
    r['N19'] = shafts(s, Ln, layout_json)
    # N20
    p0, p1 = int(round(180 * hs)), int(round(360 * hs))
    pb = part_boxes(s)
    ex20 = s.excl if not pb else (s.excl | rectmask(a.shape, pb, 0))
    r['N20'] = particles(L, ex20, p0, p1)
    r['N20']['partBoxes'] = len(pb) if pb is not None else None
    # N22
    if s.kind == 'ref':
        g0, g1 = 270, 370   # 本家は計画の表の行のまま (本家の地平線 455 の下は土手の縁が入る)
    elif hz is not None:
        lo_, hi_ = (-23, 37) if s.phone else (-27, 73)
        g0, g1 = max(0, int(round(hz + lo_))), min(H, int(round(hz + hi_)))
    if s.kind == 'ref' or hz is not None:
        meds = []
        for y in range(g0, g1, 4):
            blk = Ln[y:y + 4, x0:x1]
            meds.append(float(np.nanmedian(blk)) if np.isfinite(blk).sum() > 20 else np.nan)
        md = np.array(meds); dd = np.abs(np.diff(md))
        r['N22'] = dict(rows=[g0, g1], maxStep=rnd(float(np.nanmax(dd)), 1) if np.isfinite(dd).any() else None,
                        atRow=int(g0 + 4 * (int(np.nanargmax(dd)) + 1)) if np.isfinite(dd).any() else None)
    else:
        r['N22'] = dict(maxStep=None, note='地平線が分からない')
    # N23
    y6 = max(1, int(round(60 * hs)))
    lt = med(region_vals(L, valid, [0, 0, 300, y6])); rt = med(region_vals(L, valid, [1620, 0, 300, y6]))
    both = [v for v in (lt, rt) if v is not None]
    r['N23'] = dict(left=rnd(lt, 1), right=rnd(rt, 1), max=rnd(max(both), 1) if both else None, min=rnd(min(both), 1) if both else None)
    # K13
    if s.kind == 'sample':
        k13 = dolls_back_row(s)
        if k13:
            r['K13'] = k13
    return r


def shafts(s, Ln, layout_json=None):
    """N19: 月光の筋。本家は REF_RECTS の線、撮影は設計図の shaft を写した線"""
    H, W = s.H, s.W; sc = s.sc; w = 150 * sc; y1 = int(round(500 * H / 1080))
    XX = np.arange(W)[None, :]

    def ratio_line(xs_by_row):
        ins = []; sid = []
        for y, xc, k in xs_by_row:
            if y < 0 or y >= y1:
                continue
            row = Ln[y]; f = np.isfinite(row)
            dist = (np.arange(W) - xc) / math.sqrt(1 + k * k)
            ins.append(row[(np.abs(dist) <= w / 2) & f]); sid.append(row[(np.abs(dist) >= 0.75 * w) & (np.abs(dist) <= 1.25 * w) & f])
        if not ins:
            return None, 0
        ins = np.concatenate(ins); sid = np.concatenate(sid)
        if ins.size < 500 or sid.size < 500:
            return None, int(ins.size)
        return float(np.median(ins) / np.median(sid)), int(ins.size)

    out = []; mode = None
    if s.kind == 'ref':
        mode = '本家の線 (見つけた線を固定)' if s.refRects.get('N19') else '本家の筋の線が無い (線を固定したのは ot16 だけ)'
        for ln in s.refRects.get('N19', []):
            rows = [(y, ln['x0'] + ln['k'] * y, ln['k']) for y in range(0, y1)]
            rt, n = ratio_line(rows)
            out.append(dict(name=ln['name'], x0=ln['x0'], k=ln['k'], ratio=rnd(rt, 2), cls='vertical' if abs(ln['k']) <= 0.2 else ('diagLeft' if ln['x0'] < W / 2 else 'diagRight')))
    else:
        lay_path = layout_json
        if not lay_path and s.lay:
            dl = (s.lay.get('extra', {}).get('diorama') or {}).get('layout')
            if dl:
                lay_path = os.path.join(RES, dl + '.json')
        parts = []
        if lay_path and os.path.exists(lay_path) and s.cam.get('fov'):
            try:
                ld = json.load(open(lay_path)); yaw = ld.get('pathYaw', -22)
                parts = [p for p in ld.get('parts', []) if p.get('kind') == 'shaft']
                mode = '設計図の筋を写した線 (%s)' % os.path.relpath(lay_path, REPO)
            except Exception as e:  # noqa
                parts = []; mode = '設計図が読めない (%s)' % e
        for p in parts:
            if not p.get('abs', False):
                s.notes.append('筋 %s は abs でない = 地面の高さを 0 と見た' % p.get('name'))
            p0 = on_path(yaw, p['t'], p['s'], p.get('y', 0)); dv = np.array(p.get('dir') or [0.35, -1, 0.3], float); dv /= np.linalg.norm(dv)
            pts = [cam_project(s.cam, p0 + dv * p.get('len', 12) * q) for q in np.linspace(0, 1, 400)]
            pts = [(x, y) for x, y, d in pts if d > 0.1]
            if len(pts) < 2:
                continue
            ys = np.array([q[1] for q in pts]); xs = np.array([q[0] for q in pts])
            order = np.argsort(ys); ys, xs = ys[order], xs[order]
            k = float((xs[-1] - xs[0]) / max(1e-6, ys[-1] - ys[0]))
            rows = []
            for y in range(max(0, int(math.ceil(ys[0]))), min(y1, int(ys[-1]) + 1)):
                rows.append((y, float(np.interp(y, ys, xs)), k))
            rt, n = ratio_line(rows)
            top = next(((y, x) for y, x, _ in rows), None)
            xtop = top[1] if top else float(xs[0])
            cls = 'vertical' if abs(k) <= 0.2 else ('diagLeft' if xtop < W / 2 else 'diagRight')
            out.append(dict(name=p.get('name'), xTop=rnd(xtop, 0), rowTop=top[0] if top else None, k=rnd(k, 3), ratio=rnd(rt, 2), cls=cls, pixels=n))
        if not parts:
            # 設計図が無い時: 線を探す (両端の暗い減光を拾わないよう、斜めは x0 −300〜700・縦は x 300〜1620 に絞る)
            mode = (mode + ' → ' if mode else '') + '線を探した (設計図の筋なし)'
            best = {}
            for cls, ks, xr in (('diagLeft', np.linspace(0.3, 1.2, 10), range(-300, 700, 20)), ('vertical', np.linspace(-0.2, 0.2, 9), range(300, 1620, 20))):
                bb = None
                for k in ks:
                    for x0 in xr:
                        rt, n = ratio_line([(y, x0 + k * y, k) for y in range(0, y1, 2)])
                        if rt is not None and (bb is None or rt > bb[0]):
                            bb = (rt, float(k), x0)
                if bb:
                    out.append(dict(name='探した線', xTop=bb[2], k=rnd(bb[1], 3), ratio=rnd(bb[0], 2), cls=cls))
    dl = [q['ratio'] for q in out if q['cls'] == 'diagLeft' and q['ratio'] is not None]
    vt = [q['ratio'] for q in out if q['cls'] == 'vertical' and q['ratio'] is not None]
    return dict(mode=mode, width=rnd(w, 0), rows=[0, y1], shafts=out, diagLeft=max(dl) if dl else None, vertical=max(vt) if vt else None)


def part_boxes(s):
    """N20 で数えない部品の箱 (n20-part-boxes.json)。本家・箱庭でない画・箱の無いカメラは None (= 今までどおり数える)"""
    global PART_BOXES
    if PART_BOXES is None:
        PART_BOXES = {}
        if os.path.exists(DEFAULT_PART_BOXES):
            with open(DEFAULT_PART_BOXES, encoding='utf-8') as f:
                PART_BOXES.update({k: v for k, v in json.load(f).items() if not k.startswith('_')})
    if not PART_BOXES or not s.lay:
        return None
    cam = s.cam or {}
    name = ((s.lay.get('extra') or {}).get('diorama') or {}).get('layout')
    if not name or cam.get('mode') != 'diorama' or cam.get('fov') is None:
        return None
    pitch = (cam.get('layoutEuler') or [cam.get('pitch')])[0]
    k = '%s|%g|%g|%.3f|%s' % (name, round(float(cam['fov']), 2), round(float(pitch), 2), float(cam.get('groundLine') or 0), s.dev)
    v = PART_BOXES.get(k)
    if v is None:
        s.notes.append('N20 の部品の箱が無い (%s) = 部品の上の点も数える' % k)
        return None
    return v.get('boxes') or []


def particles(L, excl, y0, y1, thr=30, iso_min=15, lmin=80, rel=1.2, amax=60, bmax=9):
    """N20: 小さな光の粒の数"""
    H, W = L.shape
    a0 = max(0, y0 - 20); sub = L[a0:y1 + 20]
    D = (sub - gblur(sub, 4))[y0 - a0:y0 - a0 + (y1 - y0)]
    m = (D >= thr) & ~excl[y0:y1]
    ys, xs, roots = label8(m)
    out = []
    ang = np.linspace(0, 2 * np.pi, 16, endpoint=False)
    for rt in np.unique(roots):
        sel = roots == rt; yy = ys[sel]; xx = xs[sel]
        if len(yy) > amax or np.ptp(yy) + 1 > bmax or np.ptp(xx) + 1 > bmax:
            continue
        j = int(np.argmax(D[yy, xx])); cy, cx = int(yy[j]) + y0, int(xx[j])
        ring = []
        for rad in (5, 7):
            ry = np.clip(np.round(cy + rad * np.sin(ang)).astype(int), 0, H - 1); rx = np.clip(np.round(cx + rad * np.cos(ang)).astype(int), 0, W - 1)
            ring.append(L[ry, rx])
        ring = np.concatenate(ring)
        if L[cy, cx] - np.percentile(ring, 90) >= iso_min and L[cy, cx] >= lmin and L[cy, cx] >= rel * np.median(ring):
            out.append([cx, cy])
    return dict(rows=[y0, y1], count=len(out), points=out[:80])


def doll_top_margin(art):
    for sub in ('dolls',):
        p = os.path.join(RES, 'Art', sub, art + '.png')
        if os.path.exists(p):
            im = np.asarray(Image.open(p).convert('RGBA'))
            rows = np.nonzero(im[..., 3].max(1) > 16)[0]
            return (int(rows[0]) if len(rows) else 0), im.shape[0]
    return None, None


def dolls_back_row(s):
    dolls = [(k, u) for k, u in s.unitBoxes.items() if k.startswith('doll:')]
    if len(dolls) < 6:
        return None
    def idx(k):
        m_ = re.search(r'#\D*(\d+)$', k)
        return int(m_.group(1)) if m_ else 0
    dolls.sort(key=lambda q: idx(q[0]))
    items = []
    for k, u in dolls:
        x, y, w, h = u['boardPx']; top, th = doll_top_margin(u.get('art', ''))
        ppd = u.get('pxPerDot', 4)
        head = y + (top if top is not None else 0) * ppd
        items.append(dict(key=k, idx=idx(k), cx=x + w / 2, head=head, exact=top is not None))
    front = [q for q in items if q['idx'] < 5]; back = [q for q in items if q['idx'] >= 5]
    rows = []
    for b in back:
        f = min(front, key=lambda q: abs(q['cx'] - b['cx']))
        rows.append(dict(back=b['key'], front=f['key'], up=rnd(f['head'] - b['head'], 1), side=rnd(abs(f['cx'] - b['cx']), 1)))
    ups = [q['up'] for q in rows]
    return dict(rows=rows, minUp=min(ups) if ups else None, headFromArt=all(q['exact'] for q in items),
                note='頭 = 板の上端 + 人形の絵 (Art/dolls) の上の透明な行 × pxPerDot。待機の上下 ±2px は見ていない')

# ---- 目標と合否


def get_key(r, key):
    cur = r
    for part in key.split('.'):
        if not isinstance(cur, dict) or part not in cur:
            return None
        cur = cur[part]
    return cur


def judge(r, targets):
    """r2-targets.json の checks で合否。戻り値 [(id, key, 値, 範囲, 合否 True/False/None)]"""
    out = []
    dev = r.get('dev', 'PC')
    for c in targets.get('checks', []):
        rng = c.get(dev) if dev in c else c.get('all')
        if rng is None:
            continue
        v = get_key(r, c['key'])
        lo, hi = rng
        ok = None if v is None else ((lo is None or v >= lo) and (hi is None or v <= hi))
        out.append((c['id'], c['key'], v, rng, ok, c.get('label', '')))
    return out


def fmt_rng(rng):
    lo, hi = rng
    if lo is None:
        return '≤%s' % hi
    if hi is None:
        return '≥%s' % lo
    return '%s〜%s' % (lo, hi)


def text_report(r, targets=None):
    lines = ['== %s (%s・%s)  足元 %s・頭 %s・sc %s・キャラ: %s' % (r['name'], r['kind'], r['dev'], r['feet'], r['head'], r['sc'], r['charMask'])]
    for n in r.get('notes', []):
        lines.append('  注: ' + n)
    J = {}
    if targets:
        for cid, key, v, rng, ok, lab in judge(r, targets):
            J.setdefault(key, (rng, ok))

    def tok(key, v, unit=''):
        if v is None:
            return '%s —' % key.split('.')[-1]
        mark = ''
        if key in J:
            rng, ok = J[key]
            mark = ' [%s %s]' % ('○' if ok else ('×' if ok is False else '?'), fmt_rng(rng))
        return '%s %s%s%s' % (key.split('.')[-1], v, unit, mark)
    g = lambda k: get_key(r, k)
    lines.append('  N1  頂点の行 %s・%s・頭の上 %s' % (g('N1.peakRow'), tok('N1.feetMinusPeak', g('N1.feetMinusPeak'), 'px'), g('N1.headMinusPeak')))
    lines.append('  N2  %s (帯 %s)  N3 %s' % (tok('N2.fwhm', g('N2.fwhm'), 'px'), g('N2.band'), tok('N3.peak', g('N3.peak'))))
    lines.append('  N4  %s・%s (%s)' % (tok('N4.horizonRow', g('N4.horizonRow')), tok('N4.edgeTiltDeg', g('N4.edgeTiltDeg'), '°'), g('N4.source')))
    lines.append('  N5  %s・%s・%s' % (tok('N5.darkLt60', g('N5.darkLt60')), tok('N5.hazeGt130', g('N5.hazeGt130')), tok('N5.meanGrad', g('N5.meanGrad'))))
    lines.append('  N6a %s (中央値 %s)  N6b 左 %s・右 %s・%s・%s' % (tok('N6a.ratio', g('N6a.ratio')), g('N6a.median'), g('N6b.leftRatio'), g('N6b.rightRatio'),
                                                              tok('N6b.max', g('N6b.max')), tok('N6b.min', g('N6b.min'))))
    lines.append('  N6c (参考) x300〜600 ÷ 頂点 %s (中央値 %s)・右の対 x1320〜1620 %s' % (g('N6c.ratio'), g('N6c.left'), g('N6c.rightRatio')))
    st = g('N7.streaks') or []
    lines.append('  N7  %s・%s (帯 %s)  筋 %s  太い幹 %s・%s' % (tok('N7.count', g('N7.count'), '本'), tok('N7.countInRange', g('N7.countInRange'), '本'), g('N7.band'),
                                                      ' '.join('%d-%d(%.2f)' % (q['x'][0], q['x'][1], q['ratio']) for q in st) or 'なし',
                                                      ' '.join('%d-%d(輝度%s)' % (q['x'][0], q['x'][1], q['lum']) for q in (g('N7.thick') or [])) or 'なし',
                                                      tok('N7.thickBestLum', g('N7.thickBestLum'))))
    lines.append('  N8  %s・%s (中央値 %s)  N8b %s (p10 %s・p90 %s・主人公の体−窓 %s)' % (tok('N8.ratio', g('N8.ratio')), tok('N8.spread', g('N8.spread')), g('N8.median'),
                                                                        tok('N8b.median', g('N8b.median')), g('N8b.p10'), g('N8b.p90'), g('N8b.heroMinusWindow')))
    lines.append('  N9  %s' % tok('N9.median', g('N9.median')))
    sd = g('N10.side') or {}; fr = g('N10.front') or {}
    lines.append('  N10 両端 %s・%s・%s  手前 %s・%s・%s' % (tok('N10.side.hue', rnd(sd.get('hue'), 0), '°'), tok('N10.side.C', rnd(sd.get('C'), 1)), tok('N10.side.a', rnd(sd.get('a'), 1)),
                                                     tok('N10.front.hue', rnd(fr.get('hue'), 0), '°'), tok('N10.front.C', rnd(fr.get('C'), 1)), tok('N10.front.p90', fr.get('p90'))))
    lines.append('  N11 %s' % tok('N11.greyBright', g('N11.greyBright')))
    for k in ('behind', 'front', 'fog'):
        e = g('N12.' + k) or {}
        lines.append('  N12 %-6s %s・%s (縁 %s)' % (k, tok('N12.%s.sharp' % k, rnd(e.get('sharp'), 3)), tok('N12.%s.p10' % k, e.get('p10')), e.get('n')))
    lines.append('  N18 %s・%s' % (tok('N18.dirt', g('N18.dirt')), tok('N18.grass', g('N18.grass'))))
    lines.append('  N19 %s・%s  (%s)  %s' % (tok('N19.diagLeft', g('N19.diagLeft')), tok('N19.vertical', g('N19.vertical')), g('N19.mode'),
                                         ' '.join('%s[%s %s]' % (q.get('name'), q['cls'], q['ratio']) for q in (g('N19.shafts') or []))))
    lines.append('  N20 %s (行 %s)' % (tok('N20.count', g('N20.count'), '個'), g('N20.rows')))
    lines.append('  N22 %s (行 %s・段差の行 %s)' % (tok('N22.maxStep', g('N22.maxStep')), g('N22.rows'), g('N22.atRow')))
    lines.append('  N23 左 %s・右 %s・%s・%s' % (g('N23.left'), g('N23.right'), tok('N23.max', g('N23.max')), tok('N23.min', g('N23.min'))))
    if r.get('K13'):
        lines.append('  K13 後列の頭が前列より上 最小 %s px (%s)' % (tok('K13.minUp', g('K13.minUp')), ' '.join('%s' % q['up'] for q in r['K13']['rows'])))
    if targets:
        res = judge(r, targets)
        ok = sum(1 for q in res if q[4]); ng = sum(1 for q in res if q[4] is False); na = sum(1 for q in res if q[4] is None)
        lines.append('  合否: ○ %d・× %d・測れない %d (目標 %s)' % (ok, ng, na, os.path.relpath(targets.get('_path', ''), REPO) if targets.get('_path') else ''))
    return '\n'.join(lines)


def md_table(results, targets):
    ids = []
    for c in targets.get('checks', []):
        if c['key'] not in ids:
            ids.append(c['key'])
    head = '| 物差し | 目標 PC / PH | ' + ' | '.join(r['name'] for r in results) + ' |'
    sep = '|' + '---|' * (2 + len(results))
    rows = [head, sep]
    cmap = {c['key']: c for c in targets.get('checks', [])}
    for key in ids:
        c = cmap[key]
        tg = '%s / %s' % (fmt_rng(c['PC']) if c.get('PC') else (fmt_rng(c['all']) if c.get('all') else '—'), fmt_rng(c['PH']) if c.get('PH') else (fmt_rng(c['all']) if c.get('all') else '—'))
        cells = []
        for r in results:
            v = get_key(r, key); j = {q[1]: q for q in judge(r, targets)}.get(key)
            mark = '' if not j or j[4] is None else (' ○' if j[4] else ' ×')
            cells.append('—' if v is None else '%s%s' % (v, mark))
        rows.append('| %s %s | %s | %s |' % (c['id'], c.get('label', key), tg, ' | '.join(cells)))
    return '\n'.join(rows)


def load_targets(p):
    if p and os.path.exists(p):
        t = json.load(open(p)); t['_path'] = p
        return t
    return None


def find_pair(folder, scene):
    """(画, layout, unitsonly)。<場面>-hideui-1.png が無ければ <場面>-1.png (試しの撮影は名前に hideui が付かない)"""
    for img in (os.path.join(folder, scene + '-hideui-1.png'), os.path.join(folder, scene + '-1.png')):
        if os.path.exists(img):
            lay = img[:-4] + '.layout.json'
            if not os.path.exists(lay):
                alt = os.path.join(folder, scene + '-1.layout.json')
                lay = alt if os.path.exists(alt) else None
            units = os.path.join(folder, scene + '-unitsonly-1.png')
            return img, lay, units if os.path.exists(units) else None
    raise SystemExit('画が無い: %s/%s-hideui-1.png か -1.png' % (folder, scene))


def all_scenes(folder):
    out = []
    for p in sorted(glob.glob(os.path.join(folder, '*-1.png'))):
        b = os.path.basename(p)[:-6]
        if b.endswith('-unitsonly') or b.endswith('-uionly'):
            continue
        lay = p[:-4] + '.layout.json'
        st = json.load(open(lay)).get('state', '') if os.path.exists(lay) else ''
        if 'hideui=1' not in st and not b.endswith('-hideui'):
            continue
        out.append(b[:-7] if b.endswith('-hideui') else b)
    return out


def measure_scene(folder, scene, layout_json=None):
    img, lay, units = find_pair(folder, scene)
    s = shot_from_files(img, lay, units)
    s.name = scene
    return measure(s, layout_json)


def main():
    ap = argparse.ArgumentParser(description='HD-2D 見本 二周目の新しい物差し (N1〜N12・N18〜N20・N22・N23)')
    ap.add_argument('folder', nargs='?'); ap.add_argument('scene', nargs='?')
    ap.add_argument('--image'); ap.add_argument('--layout'); ap.add_argument('--units')
    ap.add_argument('--ref', action='append', help='本家 (ot16 など。何回でも)')
    ap.add_argument('--ref-dir', action='append', default=[], help='本家の画の置き場を足す (既定 ~/.cache/deck-rogue/hd2d-ref)')
    ap.add_argument('--all', help='フォルダの UI なしの画を全部')
    ap.add_argument('--targets', default=DEFAULT_TARGETS)
    ap.add_argument('--layout-json', help='N19 の筋を読む設計図 (既定は layout.json の extra.diorama.layout)')
    ap.add_argument('--json', help='結果の JSON'); ap.add_argument('--md', help='表の md')
    ap.add_argument('--quiet', action='store_true')
    ap.add_argument('--part-boxes', help='N20 で数えない部品の箱の JSON (既定 docs/design/hd2d-slice/n20-part-boxes.json。none で使わない)')
    a = ap.parse_args()
    global PART_BOXES, DEFAULT_PART_BOXES
    if a.part_boxes == 'none':
        PART_BOXES = {}
    elif a.part_boxes:
        DEFAULT_PART_BOXES = a.part_boxes
    targets = load_targets(a.targets)
    results = []
    for k in a.ref or []:
        results.append(measure(shot_from_ref(k, a.ref_dir + DEFAULT_REF_DIRS)))
    if a.image:
        s = shot_from_files(a.image, a.layout or (a.image[:-4] + '.layout.json'), a.units)
        results.append(measure(s, a.layout_json))
    if a.all:
        for sc_ in all_scenes(a.all):
            results.append(measure_scene(a.all, sc_, a.layout_json))
    if a.folder and a.scene:
        results.append(measure_scene(a.folder, a.scene, a.layout_json))
    if not results:
        ap.error('測る画が無い')
    if not a.quiet:
        for r in results:
            print(text_report(r, targets))
    if a.json:
        slim = [{k: v for k, v in r.items()} for r in results]
        json.dump(slim, open(a.json, 'w'), ensure_ascii=False, indent=1)
    if a.md and targets:
        open(a.md, 'w').write(md_table(results, targets) + '\n')


if __name__ == '__main__':
    main()

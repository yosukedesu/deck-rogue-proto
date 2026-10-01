#!/usr/bin/env python3
"""docs/pixellab/hd2d-act1/gen_art.py — 幕1の箱庭の絵をコードで作る (PixelLab は使わない＝生成 0 回)。

元は W3b P22 (2026-09-30「本家っぽく」) が作業場 (scratchpad の p22b/gen_art.py) に置いた物。docs/pixellab-assets.md の約束どおり
ここへ写し、二周目 (2026-10-01 計画 docs/design/hd2d-round2-plan-2026-10-01.md §2 レーン D) の絵を足した。
色はどれも幕1の色表 (docs/pixellab/hd2d-act1/palette-act1.json・48色) の色だけで塗る。光は左上から。1ドット = 座席で 4px (倍率で縮めない)。

使い方 (出力の根 <out> の下に tiles/・relief/・litter/ を作る。Art へ直に書く時は <out> = unity/Assets/Resources/Art/stage/act1):
  python3 -B docs/pixellab/hd2d-act1/gen_art.py w3b <out> [--sheet <png>]
      W3b の絵 (座席の土 top_path_seat_{a..d}・奥の幹 trunk_tall_{1..3}・小札 litter/*)。今の Art と画素まで同じ物が出る
      (2026-10-01 に作業場の元のスクリプトの出力と Art の18枚を突き合わせて一致)。
  python3 -B docs/pixellab/hd2d-act1/gen_art.py r2 <out> [--only <名前,...>] [--sheet <png>]
      二周目の絵 (下の R2_ITEMS)。--only で一部だけ (例 --only backdrop,trunk_long)。
      **今ある絵は上書きしない**: trunk_tall_* は書かない (W5 の写しが使う)。座席の土は新しい名前 top_path_seat_r2_{a..d} で出す
      (W5 の写し act1_layout_w5 が読む top_path_seat_{a..d} を画素まで守る。設計図の tiles.pathSeat の候補の先頭に足して使う)。
      草 (seat_grass) は段2 の試しで作り直さないと決まったので既定では作らない (--only seat_grass で top_grass_seat_r2_{a..d})。
  python3 -B docs/pixellab/hd2d-act1/gen_art.py check <png...>
      色表からの距離 (p95)・大きさ・不透明の割合・CIELab の色相の中央値・(タイルは) 明るい点の数 (N18 の数え方＝7×7 の中央値より 18 以上明るい画素を1万画素あたり)。

二周目の絵 (R2_ITEMS):
  backdrop       relief/backdrop_plain.png          32×32 不透明の無地 #204043 (色表の暗い紺緑・CIELab 色相 207°)。カメラの奥の背景の板 (段1)
  trunk_long     relief/trunk_long_{1..3}.png       幹の幅 40〜56・高さ 280。樹皮のタイル side_bark を縦に敷いて切る。上へ少し細る・根元の張り出し・枝の付け根 2〜3
  trunk_thin     relief/trunk_thin_{1..3}.png       幹の幅 16〜24・高さ 280。同じ作り方の細い幹 (霧の帯の幹)
  needle_bough   relief/needle_bough_{1..4}.png     96×48・120×56・84×42・108×60 の針葉の枝の影絵 (段2 で作り直し: 垂れる房の塊・不透明 35〜45%・
                 平均の色相 210〜212°)。上の真ん中の「霧に透けた高い枝」。元の端は細い (画面の外の幹へつなぐ側)
  tuft_stand     relief/tuft_stand_{1..4}.png       立った草の株 (背丈 12・12・16・20・葉 5〜9枚・間隔は不規則)。1・2 は背丈 12＝手前の株 (K12)。
                 3・4 は背丈 16・20 = 地面の小札 (litter の上限 12) にはできない＝card か relief で
  canopy_navy    relief/act1_canopy_navy.png        Art/props/act1_canopy (垂れた葉・色相 191°) を色表の紺緑の4段へ (中の段は青と緑を半々)
  giant_cut      relief/act1_tree_giant_cut.png     relief/act1_tree_giant の足元の土の皿と石を透明にして下を切った写し
  props_map      relief/act1_{tree_pine,tallgrass1,stump,waystone}.png   Art/props の同名を色表へ写す (既存の16枚と同じ stage-palette の map)
  seat_soil      tiles/top_path_seat_r2_{a..d}.png  座席の土の作り直し: 小石 6〜8→2〜3 個で暗い石・明るい粒 1.2%→0.4%・細かい斜めの筋 10〜16 本
                 (本家 ot11 の土)。タイルの明るい点 176→47/1万 (−73%)
  seat_grass     tiles/top_grass_seat_r2_{a..d}.png (頼んだ時だけ) scripts/tile-calm.py --contrast 0.45 --no-auto → map
"""
import json
import math
import os
import sys

import numpy as np
from PIL import Image

REPO = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', '..'))
PAL = json.load(open(REPO + '/docs/pixellab/hd2d-act1/palette-act1.json'))['colors']


def hx(h):
    return (int(h[1:3], 16), int(h[3:5], 16), int(h[5:7], 16))


def lum(c):
    return 0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2]


PALRGB = [hx(c) for c in PAL]


def near(c):
    """色表でいちばん近い色 (Oklab の代わりに RGB の距離。色表の中の色を選ぶだけなので十分)"""
    best, bd = None, 1e9
    for p in PALRGB:
        d = sum((a - b) ** 2 for a, b in zip(c, p))
        if d < bd:
            best, bd = p, d
    return best


# 土の色 (暗い → 明るい)
EARTH = [hx('#3d332f'), hx('#5a4334'), hx('#624c3b'), hx('#6e5038'), hx('#706259'), hx('#776c63'), hx('#867e72')]
STONE = [hx('#3f3f38'), hx('#535457'), hx('#6a6f73'), hx('#858789'), hx('#928f90')]
GREEN = [hx('#193d20'), hx('#2d482f'), hx('#3c5a30'), hx('#4c6c32'), hx('#577e38')]
BARK_DARK = hx('#16151e')


# ---------------------------------------------------------------- 巻き戻しの値ノイズ
def periodic_noise(n, cells, rng):
    g = rng.random((cells, cells))
    ys, xs = np.mgrid[0:n, 0:n]
    fx = xs / n * cells
    fy = ys / n * cells
    x0 = np.floor(fx).astype(int) % cells
    y0 = np.floor(fy).astype(int) % cells
    x1 = (x0 + 1) % cells
    y1 = (y0 + 1) % cells
    tx = fx - np.floor(fx)
    ty = fy - np.floor(fy)
    tx = tx * tx * (3 - 2 * tx)
    ty = ty * ty * (3 - 2 * ty)
    a = g[y0, x0] * (1 - tx) + g[y0, x1] * tx
    b = g[y1, x0] * (1 - tx) + g[y1, x1] * tx
    return a * (1 - ty) + b * ty


def poisson(n, count, mind, rng, tries=4000, avoid=None):
    pts = []
    for _ in range(tries):
        if len(pts) >= count:
            break
        p = (int(rng.integers(0, n)), int(rng.integers(0, n)))
        ok = True
        for q in pts + (avoid or []):
            dx = min(abs(p[0] - q[0]), n - abs(p[0] - q[0]))
            dy = min(abs(p[1] - q[1]), n - abs(p[1] - q[1]))
            if dx * dx + dy * dy < mind * mind:
                ok = False
                break
        if ok:
            pts.append(p)
    return pts


# ---------------------------------------------------------------- 1. 座席の帯の土
def earth_tile(seed, n=64):
    rng = np.random.default_rng(seed)
    f = 0.62 * periodic_noise(n, 4, rng) + 0.38 * periodic_noise(n, 8, rng)
    f = (f - f.min()) / max(1e-6, f.max() - f.min())
    jitter = rng.random((n, n))
    img = np.zeros((n, n, 3), np.uint8)
    # 段: 暗い踏み跡 (d1) / 地 (base) / 乾いた明るい斑 (l1)。境目はランダムなディザ (規則的な網目を作らない)
    for y in range(n):
        for x in range(n):
            v = f[y, x] + (jitter[y, x] - 0.5) * 0.09
            if v < 0.20:
                c = EARTH[1]          # 踏み跡 (暗い)
            elif v < 0.44:
                c = EARTH[2]          # 地の斑
            elif v < 0.85:
                c = EARTH[3]          # 地
            else:
                c = EARTH[4]          # 乾いた斑 (少し)
            img[y, x] = c
    # 細かい粒 (まばら・不規則): 暗い粒 2.5%・明るい粒 1.2%
    g = rng.random((n, n))
    for y in range(n):
        for x in range(n):
            if g[y, x] < 0.025:
                img[y, x] = EARTH[1] if f[y, x] > 0.3 else EARTH[0]
            elif g[y, x] > 0.988:
                img[y, x] = EARTH[5] if f[y, x] < 0.7 else EARTH[6]
    # ひび (横に長い細い線・1本 6〜11 ドット・枝を1つ)
    for _ in range(2):
        x, y = int(rng.integers(0, n)), int(rng.integers(0, n))
        length = int(rng.integers(6, 12))
        dirx = 1 if rng.random() < 0.5 else -1
        for i in range(length):
            img[y % n, x % n] = EARTH[0]
            x += dirx
            r = rng.random()
            if r < 0.22:
                y += 1
            elif r < 0.44:
                y -= 1
            if i == length // 2 and rng.random() < 0.7:
                bx, by = x, y
                for _ in range(int(rng.integers(2, 4))):
                    by += 1 if rng.random() < 0.5 else -1
                    bx += dirx
                    img[by % n, bx % n] = EARTH[1]
    # 小石 (6〜8 個・間を空ける): 左上が明るく、右下に影
    stones = poisson(n, int(rng.integers(6, 9)), 13, rng)
    for (sx, sy) in stones:
        w = int(rng.integers(2, 4))
        h = 1 if w == 2 and rng.random() < 0.5 else 2
        for yy in range(h):
            for xx in range(w):
                c = STONE[3] if (xx == 0 and yy == 0) else STONE[2] if yy == 0 else STONE[1]
                img[(sy + yy) % n, (sx + xx) % n] = c
        # 影 (下と右下)
        for xx in range(1, w + 1):
            img[(sy + h) % n, (sx + xx) % n] = EARTH[0] if xx < w else EARTH[1]
    # 小さな草の芽 (3〜4 か所・2〜3 ドット)
    for (gx, gy) in poisson(n, int(rng.integers(3, 5)), 18, rng, avoid=stones):
        img[gy % n, gx % n] = GREEN[3]
        img[(gy + 1) % n, gx % n] = GREEN[2]
        if rng.random() < 0.6:
            img[gy % n, (gx + 1) % n] = GREEN[2]
            img[(gy - 1) % n, (gx + 1) % n] = GREEN[3]
    return img


# ---------------------------------------------------------------- 2. 奥の高い幹
def trunk(bark_tile, w, h, seed, flare=1.7):
    rng = np.random.default_rng(seed)
    bt = np.array(Image.open(bark_tile).convert('RGB'))
    th, tw = bt.shape[:2]
    W = int(math.ceil(w * flare)) + 6
    out = np.zeros((h, W, 4), np.uint8)
    cx = W / 2.0
    ox = int(rng.integers(0, tw))
    wobble = periodic_noise(64, 4, rng)[:, 0]
    root_lobes = sorted(rng.uniform(-1, 1, 2))
    lean_px = float(rng.uniform(-4, 4))
    for y in range(h):
        yb = h - 1 - y   # 下からの行
        # 幅: 上に向かってわずかに細く・根元で広がる
        base = w * (1.0 - 0.12 * (y / h) * 0) * (0.86 + 0.14 * (1 - y / h))
        if yb < 16:
            k = (16 - yb) / 16.0
            base = base * (1 + (flare - 1) * k * k)
        half = base / 2.0 + (wobble[(y * 3) % 64] - 0.5) * 1.6
        lean = lean_px * (1.0 - yb / float(h)) ** 1.5   # 上ほど傾く (まっすぐな柱に見せない)
        left = cx - half + lean
        right = cx + half + lean
        for x in range(W):
            if x + 0.5 < left or x + 0.5 > right:
                # 根の張り出し (根元の 6 行だけ・2本)
                if yb < 6:
                    for lob in root_lobes:
                        rx = cx + lean + (1 if lob >= 0 else -1) * (half + 1.5)   # 根元のすぐ外 (離れた点にしない)
                        if abs(x + 0.5 - rx) < (6 - yb) * 0.7 + 0.5:
                            out[y, x] = (*EARTH[1], 255)
                continue
            u = (x + 0.5 - left) / max(1e-3, right - left)   # 0..1 幹の左から右
            c = bt[(y + 7 * seed) % th, (ox + int(u * w)) % tw].astype(float)
            # 丸い陰 (光は左上から): 明るさ 0.35〜1.0
            shade = 0.35 + 0.65 * max(0.0, math.cos((u - 0.34) * math.pi * 0.95))
            shade *= 0.72   # 奥の幹は影の中
            c = c * shade
            if u < 0.06 or u > 0.94:
                c = np.array(BARK_DARK, float)
            out[y, x] = (*near(tuple(int(v) for v in c)), 255)
    # 枝の付け根 (上 1/3 に 1〜2 本・短く斜めに・幹と同じ暗い樹皮)
    for bi in range(int(rng.integers(1, 3))):
        by = int(rng.integers(8, h // 3))
        side = 1 if rng.random() < 0.5 else -1
        row = out[by]
        xs = [x for x in range(W) if row[x, 3] > 0]
        if not xs:
            continue
        x0 = xs[-1] - 2 if side > 0 else xs[0] + 2   # 幹の内側から生やす (離れた線にしない)
        L = int(rng.integers(6, 11))
        for i in range(L):
            for t in range(2 if i < L - 2 else 1):
                yy = by - i // 2 - t
                xx = x0 + side * i
                if 0 <= yy < h and 0 <= xx < W:
                    c = bt[(yy + 3) % th, (ox + i) % tw].astype(float) * 0.62
                    out[yy, xx] = (*near(tuple(int(v) for v in c)), 255)
    return out


# ---------------------------------------------------------------- 3. 地面の小札
def sprite(rows, colors):
    h = len(rows)
    w = max(len(r) for r in rows)
    a = np.zeros((h, w, 4), np.uint8)
    for y, r in enumerate(rows):
        for x, ch in enumerate(r):
            if ch in colors:
                a[y, x] = (*colors[ch], 255)
    return a


LITTER = {
    # 小石 (h = 明るい面・m = 地の石・s = 影の側・k = 接地の影)
    'pebble_1': (['.hm.', 'hmms', '.kks'], {'h': STONE[4], 'm': STONE[2], 's': STONE[1], 'k': EARTH[0]}),
    'pebble_2': (['.hmm.', 'hmmms', 'mmsss', '.kkk.'], {'h': STONE[3], 'm': STONE[2], 's': STONE[1], 'k': EARTH[0]}),
    'pebble_3': (['hm', 'ms'], {'h': STONE[3], 'm': STONE[2], 's': STONE[0]}),
    'pebble_4': (['..hm..', '.hmmms', 'hmmsss', '.kkkk.'], {'h': STONE[4], 'm': STONE[2], 's': STONE[1], 'k': EARTH[0]}),
    # 草の株 (t = 先・l = 明るい葉・m = 葉・d = 根元)
    'tuft_1': (['t...t', '.l.l.', '.lml.', 'dmmmd'], {'t': GREEN[4], 'l': GREEN[3], 'm': GREEN[2], 'd': GREEN[1]}),
    'tuft_2': (['..t..t.', 't..l.l.', '.l.lml.', '.lmmm.l', 'dmmdmmd'], {'t': GREEN[4], 'l': GREEN[3], 'm': GREEN[2], 'd': GREEN[1]}),
    'tuft_3': (['.t.', 'tl.', '.lt', 'dmd'], {'t': GREEN[4], 'l': GREEN[3], 'm': GREEN[2], 'd': GREEN[1]}),
    'tuft_4': (['t....t..', '.l..t.l.', '.l.lm.l.', '..lmmlm.', '.dmmmmmd'], {'t': GREEN[4], 'l': GREEN[3], 'm': GREEN[2], 'd': GREEN[0]}),
    # 落ち葉 (o = 明るい・b = 葉・k = 影)
    'leaves_1': (['.ob..', 'obbo.', '.kk.b'], {'o': hx('#6e5038'), 'b': hx('#5a4334'), 'k': EARTH[0]}),
    'leaves_2': (['ob.....', 'bbo.ob.', '.k..bbk'], {'o': hx('#776c63'), 'b': hx('#624c3b'), 'k': EARTH[0]}),
    # 小枝
    'twig_1': (['......bb', '..bbbb..', 'bb..k...'], {'b': hx('#5a4334'), 'k': EARTH[0]}),
}




# ================================================================ 二周目 (2026-10-01 レーン D)

import importlib.util
import subprocess
import tempfile

ART = os.path.join(REPO, 'unity', 'Assets', 'Resources', 'Art')
ACT1 = os.path.join(ART, 'stage', 'act1')
PAL_JSON = os.path.join(REPO, 'docs', 'pixellab', 'hd2d-act1', 'palette-act1.json')

sys.dont_write_bytecode = True   # scripts/__pycache__ を作らない
_spec = importlib.util.spec_from_file_location('stage_palette', os.path.join(REPO, 'scripts', 'stage-palette.py'))
SP = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(SP)

# 暗い紺緑 (CIELab の色相 207°・C* 12)。背景の板・針葉の枝・垂れ葉の明るい側
NAVY = hx('#204043')
# 紺緑の影の中間 (緑の #112f23 を 2・青い #172d44 を 1 の割合で混ぜて、色相の平均を 205〜215° に保つ)
NAVY_MID = (hx('#112f23'), hx('#112f23'), hx('#172d44'))
# 芯と最暗 (色みの無い黒に近い色＝色相の平均を青へ引っぱらない)
NAVY_DEEP = hx('#1c1d1c')
NAVY_BLACK = hx('#16151e')


def _put(img, x, y, c):
    h, w = img.shape[:2]
    if 0 <= x < w and 0 <= y < h:
        img[y, x] = (*c, 255)


def _line(img, x0, y0, x1, y1, c):
    """ブレゼンハムの線 (端を含む)"""
    x0, y0, x1, y1 = int(round(x0)), int(round(y0)), int(round(x1)), int(round(y1))
    dx, dy = abs(x1 - x0), -abs(y1 - y0)
    sx, sy = (1 if x0 < x1 else -1), (1 if y0 < y1 else -1)
    err = dx + dy
    while True:
        _put(img, x0, y0, c)
        if x0 == x1 and y0 == y1:
            break
        e2 = 2 * err
        if e2 >= dy:
            err += dy
            x0 += sx
        if e2 <= dx:
            err += dx
            y0 += sy


def _crop_alpha(img, keep_height=True):
    """透明な列を左右から切る (keep_height なら行は切らない＝足元の行を保つ)"""
    a = img[..., 3] > 0
    cols = np.where(a.any(0))[0]
    if len(cols) == 0:
        return img
    img = img[:, cols[0]:cols[-1] + 1]
    if not keep_height:
        rows = np.where(a.any(1))[0]
        img = img[rows[0]:rows[-1] + 1]
    return img


# ---------------------------------------------------------------- 背景の板
def backdrop_plain(n=32):
    return np.full((n, n, 4), (*NAVY, 255), np.uint8)


# ---------------------------------------------------------------- 長い幹・細い幹
def trunk_r2(bark_tile, w, h, seed, flare=1.75, taper=0.2, nbranch=(2, 4), shade_k=0.72, moss=0, twig=0.6, hl=0.22):
    """W3b の trunk() を 280 ドットへ伸ばした物。違い: 樹皮の列を少しずつ横へずらす (64 行ごとの繰り返しを目立たせない・筋が少しねじれる)・
    太さの揺れを 1D の値ノイズで (周期を作らない)・枝を上の 60% に nbranch 本・光の側 (左) に苔の縦の筋 moss 本。
    段2 (本家 ot16 の幹と並べて): 光の側 (幹の左 22% の所) に途切れ途切れの明るい縦の筋 (hl = 明るさの足し分)・
    枝は付け根の太い短い物だけでなく、斜め上へ伸びて先が少し垂れる細い枝 (長さは幹の幅の 1/4〜twig 倍・傾きは乱数)"""
    rng = np.random.default_rng(seed)
    bt = np.array(Image.open(bark_tile).convert('RGB'))
    th, tw = bt.shape[:2]
    W = int(math.ceil(max(w * flare * 1.2, w * (1.0 + 2.0 * twig)))) + 6
    out = np.zeros((h, W, 4), np.uint8)
    cx = W / 2.0
    ox0 = int(rng.integers(0, tw))
    drift = float(rng.uniform(-0.06, 0.06))          # 1 行あたりの樹皮の横のずれ (280 行で ±17 テクセル)
    knots = rng.random(h // 20 + 3)
    wob = np.interp(np.arange(h), np.arange(len(knots)) * 20.0, knots)
    hk = rng.random(h // 9 + 3)                        # 光の筋の途切れ (9 行ごとの値を線形に)
    hband = np.interp(np.arange(h), np.arange(len(hk)) * 9.0, hk)
    lean_px = float(rng.uniform(-5, 5))
    buttress = float(rng.uniform(0.6, 1.4))            # 根元の張り出しの左右の差 (左右対称の柱に見せない)
    for y in range(h):
        yb = h - 1 - y
        base = w * (1.0 - taper * (yb / float(h)))
        if yb < 24:
            k = (24 - yb) / 24.0
            base = base * (1 + (flare - 1) * k * k * k)
        half = base / 2.0 + (wob[y] - 0.5) * 2.0
        if yb < 24:
            cx_shift = (buttress - 1.0) * half * ((24 - yb) / 24.0) ** 3 * 0.5
        else:
            cx_shift = 0.0
        lean = lean_px * (yb / float(h)) ** 1.5
        left = cx - half + lean + cx_shift
        right = cx + half + lean + cx_shift
        ox = ox0 + int(round(drift * y))
        for x in range(W):
            if x + 0.5 < left or x + 0.5 > right:
                continue
            u = (x + 0.5 - left) / max(1e-3, right - left)
            c = bt[(y + 7 * seed) % th, (ox + int(u * w)) % tw].astype(float)
            shade = 0.35 + 0.65 * max(0.0, math.cos((u - 0.34) * math.pi * 0.95))
            lit = 1.0 + hl * math.exp(-((u - 0.22) / 0.07) ** 2) * (1.0 if hband[y] > 0.32 else 0.35)
            c = c * shade * shade_k * lit
            if u < 0.05 or u > 0.95:
                c = np.array(BARK_DARK, float)
            out[y, x] = (*near(tuple(int(v) for v in c)), 255)
    # 苔の縦の筋 (光の側・短く・不規則)
    for _ in range(moss):
        y0 = int(rng.integers(int(h * 0.25), h - 12))
        L = int(rng.integers(6, 22))
        uc = float(rng.uniform(0.14, 0.38))
        for i in range(L):
            yy = y0 + i
            xs = [x for x in range(W) if out[yy, x, 3] > 0]
            if len(xs) < 4:
                continue
            xx = xs[0] + int(uc * (xs[-1] - xs[0]))
            if rng.random() < 0.8:
                out[yy, xx] = (*GREEN[1], 255)
            if rng.random() < 0.35:
                out[yy, xx + 1] = (*GREEN[0], 255)
    # 枝 (上の 60% に・幹の内側から斜め上へ・付け根が太い・先は細く少し垂れる)
    nb = int(rng.integers(nbranch[0], nbranch[1] + 1))
    used = []
    for _ in range(nb):
        for _try in range(30):
            by = int(rng.integers(6, int(h * 0.6)))
            if all(abs(by - u) > 16 for u in used):
                break
        used.append(by)
        side = 1 if rng.random() < 0.5 else -1
        xs = [x for x in range(W) if out[by, x, 3] > 0]
        if not xs:
            continue
        x0 = xs[-1] - 2 if side > 0 else xs[0] + 2
        L = int(rng.integers(max(5, w // 4), max(8, int(w * twig)) + 1))
        slope = float(rng.uniform(0.35, 0.9))           # 1 ドット外へ行くあいだに上がる量
        thick = 3 if w >= 36 else 2
        y = float(by)
        for i in range(L):
            k = i / max(1, L - 1)
            tt = thick if i < max(2, L // 5) else (2 if i < L // 2 else 1)
            y -= slope * (1.0 - 1.6 * max(0.0, k - 0.7))  # 先の 3 割で垂れる
            for t in range(tt):
                yy = int(round(y)) - t
                xx = x0 + side * i
                if 0 <= yy < h and 0 <= xx < W:
                    c = bt[(yy + 3) % th, (ox0 + i) % tw].astype(float) * (0.62 if t == 0 else 0.5)
                    out[yy, xx] = (*near(tuple(int(v) for v in c)), 255)
    return out


# ---------------------------------------------------------------- 針葉の枝の影絵
NAVY_GREEN = hx('#112f23')   # 影の中間の緑 (CIELab 色相 164°)
NAVY_BLUE = hx('#172d44')    # 影の中間の青 (269°)。緑と混ぜる割合 p_blue で枝の平均の色相を 205〜215° に合わせる


def needle_bough(seed, W, H, p_blue=0.45):
    """画面の上の真ん中の「霧に透けた高い枝」(本家の夜の森 ot16 の上の真ん中＝霧に沈んだトウヒの枝の段)。
    段2 (2026-10-01) で作り直し: 段1 の下書き (細い小枝と針の線・不透明 18〜26%) は霧に沈めると網の目にしか見えず、
    上の真ん中の明るい面 (N6a) を埋められなかった → 主枝の下に「先の方へ少し流れて垂れる房」(下向きの細い三角) を
    2.5〜7.5 ドットの乱数の間隔で並べ、つけ根を1本の帯でつないだ塊にする (不透明 35〜45%)。上には寝かせた短い房を少し。
    元の端 (画面の外の幹へつながる側) は細く、先は細い小枝 2〜3 本。形・長さ・間隔・流れはどれも乱数 (等間隔の粒を作らない)。
    光は左上から: 上の縁と左の縁は紺緑 #204043、下の縁は影の芯 #1c1d1c、中は影の中間 (緑と青を p_blue で混ぜる)・針の照り 16%。
    主枝は元の方の 45% だけ最暗の線で見せる。半分の確率で左右反転"""
    rng = np.random.default_rng(seed)
    occ = np.zeros((H, W), bool)
    tipx = float(rng.uniform(0.86, 0.95)) * (W - 1)
    y0 = float(rng.uniform(0.10, 0.20)) * H
    sag = float(rng.uniform(0.18, 0.34)) * H
    ph = float(rng.uniform(0, math.tau))

    def ys(x):
        return y0 + sag * (min(max(x, 0.0), tipx) / tipx) ** 1.45 + 1.2 * math.sin(x / tipx * 4.3 + ph)

    def room(x):   # その x で主枝から下の余白 (房はここまで垂れてよい)
        return max(4.0, H - 1.5 - ys(x))

    Dmax = room(tipx * 0.5)

    def env(u):    # 房の深さの包み: 元は細く・中ほどで深く・先で細る
        a = min(1.0, u / 0.24)
        b = min(1.0, (1.0 - u) / 0.35)
        return max(0.0, a) ** 1.2 * max(0.0, b) ** 0.8

    def put(x, y):
        xi, yi = int(round(x)), int(round(y))
        if 0 <= xi < W and 0 <= yi < H:
            occ[yi, xi] = True

    # 1. 芯の帯 (房のつけ根をつなぐ)
    for xi in range(int(tipx) + 1):
        th = 1.0 + 0.34 * room(xi) * env(xi / tipx)
        yc = ys(xi)
        for yy in range(int(math.floor(yc - 1)), int(math.ceil(yc + th)) + 1):
            put(xi, yy)
    # 2. 垂れる房 (下向きの細い三角・先の方へ少し流れる)
    x = float(rng.uniform(0.05, 0.09)) * tipx
    while x < tipx - 1:
        u = x / tipx
        L = min(room(x), room(x) * env(u) ** 0.8 * float(rng.uniform(0.6, 1.0)) + float(rng.uniform(1, 3)))
        w = float(rng.uniform(2.8, 5.6)) * (0.75 + 0.5 * env(u))
        lean = float(rng.uniform(0.08, 0.40)) * L * (1 if rng.random() < 0.85 else -0.5)
        yb = ys(x)
        for r in range(int(L) + 1):
            k = r / max(1.0, L)
            hw = w * (1.0 - k) ** 0.85 + 0.35
            cx = x + lean * k ** 1.3
            for xx in range(int(math.floor(cx - hw)), int(math.ceil(cx + hw)) + 1):
                if abs(xx - cx) <= hw:
                    put(xx, yb + r)
        for _ in range(int(rng.integers(1, 4))):   # 房から横へ出る細い針 (縁のぎざぎざ)
            r = float(rng.uniform(0.2, 0.8)) * L
            side = 1 if rng.random() < 0.5 else -1
            cx = x + lean * (r / max(1.0, L)) ** 1.3
            for i in range(int(rng.integers(2, 5))):
                put(cx + side * (w * (1 - r / max(1.0, L)) + i), yb + r + i * 0.7)
        x += float(rng.uniform(2.5, 7.5)) * (0.8 + 0.4 * (1 - env(u)))
    # 3. 上の短い房 (まばら・先の方へ寝かせる＝とげに見せない)
    x = float(rng.uniform(4, 10))
    while x < tipx - 3:
        u = x / tipx
        L = (1.5 + 0.12 * Dmax * env(u)) * float(rng.uniform(0.4, 0.9))
        w = float(rng.uniform(1.5, 2.6))
        lean = float(rng.uniform(0.9, 1.7)) * L
        yb = ys(x)
        for r in range(int(L) + 1):
            k = r / max(1.0, L)
            hw = w * (1.0 - k) + 0.3
            cx = x + lean * k
            for xx in range(int(math.floor(cx - hw)), int(math.ceil(cx + hw)) + 1):
                if abs(xx - cx) <= hw:
                    put(xx, yb - r)
        x += float(rng.uniform(6.0, 14.0))
    # 4. 先の細い小枝
    for _ in range(int(rng.integers(2, 4))):
        a = math.radians(float(rng.uniform(-10, 40)))
        L = float(rng.uniform(3, 8))
        for i in range(int(L) + 1):
            put(tipx + i * math.cos(a), ys(tipx) + i * math.sin(a))
    # 5. 色 (光は左上から)
    img = np.zeros((H, W, 4), np.uint8)
    up = np.zeros_like(occ); up[1:] = occ[:-1]
    dn = np.zeros_like(occ); dn[:-1] = occ[1:]
    lf = np.zeros_like(occ); lf[:, 1:] = occ[:, :-1]
    r = rng.random((H, W))
    pb = rng.random((H, W))
    for yy in range(H):
        for xx in range(W):
            if not occ[yy, xx]:
                continue
            if not up[yy, xx] or (not lf[yy, xx] and r[yy, xx] < 0.6):
                c = NAVY
            elif not dn[yy, xx]:
                c = NAVY_DEEP
            elif r[yy, xx] < 0.16:
                c = NAVY
            elif r[yy, xx] < 0.22:
                c = NAVY_DEEP
            else:
                c = NAVY_BLUE if pb[yy, xx] < p_blue else NAVY_GREEN
            img[yy, xx] = (*c, 255)
    # 6. 主枝 (元の方だけ見える最暗の線・元が太い)
    for xi in range(int(tipx * 0.45)):
        yc = int(round(ys(xi)))
        _put(img, xi, yc, NAVY_BLACK)
        if xi < tipx * 0.15:
            _put(img, xi, yc + 1, NAVY_DEEP)
    if rng.random() < 0.5:
        img = img[:, ::-1].copy()
    return img


# ---------------------------------------------------------------- 立った草の株
def tuft_stand(seed, h, nbl):
    """背丈 h・葉 nbl 枚。葉の根元は正規分布で固まり (等間隔にしない)、長さ・傾き・曲がりはどれも乱数。光は左上から (右の葉は一段暗い)"""
    rng = np.random.default_rng(seed)
    W = 2 * h + 8
    img = np.zeros((h, W, 4), np.uint8)
    cx = W / 2.0
    blades = []
    sig = max(1.4, 0.14 * h)                                 # 根元の固まり (背の高い株ほど広い)
    for i in range(nbl):
        bx = cx + float(rng.normal(0, sig))
        hb = h if i == 0 else float(rng.uniform(0.45, 0.95)) * h
        dx = float(rng.uniform(-0.6, 0.6)) * hb * (1.0 if abs(bx - cx) < sig else 1.3) * (1 if (bx - cx) * float(rng.uniform(-0.3, 1.0)) >= 0 else -1)
        blades.append((bx, hb, dx))
    blades.sort(key=lambda b: -b[0])                         # 右 (影の側) から先に描き、左 (光の側) を手前に
    for (bx, hb, dx) in blades:
        right = bx > cx + 0.5
        for r_i in range(int(round(hb))):
            r = r_i / max(1.0, hb - 1)
            x = int(round(bx + dx * r ** 1.7))
            y = h - 1 - r_i
            k = 1 if r < 0.25 else 2 if r < 0.55 else 3 if r < 0.85 else 4
            if right:
                k = max(0, k - 1)
            _put(img, x, y, GREEN[k])
            if r < 0.35:
                _put(img, x + (1 if right else -1), y, GREEN[max(0, k - 1)] if right else GREEN[k])   # 根元の太さ: 光の側は同じ段・影の側は一段暗い
    xs = np.where(img[h - 1, :, 3] > 0)[0]
    if len(xs) and not TUFT_ROOT_TRIM:
        for x in range(xs.min(), xs.max() + 1):
            _put(img, x, h - 1, GREEN[0])
    cols = np.where((img[..., 3] > 0).any(0))[0]             # 切る列は根元を削る前の形で決める (幅と足元の中心を変えない)
    if TUFT_ROOT_TRIM and len(cols):
        # 直しの輪2 (2026-10-01・反証 medium「株の根元が 3〜4 行の黒い四角＝鉢植えや箱の上の草」): 下 2 行を台形に。
        # 下から2行目は左右の端のドットを1つずつ抜き、最下段は真ん中に近い茎 2〜3 本の根元だけ (横いっぱいの暗い帯を作らない)
        row = img[h - 2, :, 3] > 0
        on = np.where(row)[0]
        if len(on) >= 3:
            img[h - 2, on[0]] = 0
            img[h - 2, on[-1]] = 0
        keepx = set()
        for b in sorted(blades, key=lambda b: abs(b[0] - cx)):   # 真ん中に近い茎から、違う列の根元を 2〜3 本
            if len(keepx) < (2 if h <= 12 else 3) and img[h - 1, int(round(b[0])), 3] > 0:
                keepx.add(int(round(b[0])))
        for x in range(W):
            if x not in keepx:
                img[h - 1, x] = 0
    if TUFT_LIFT > 0:
        # 直しの輪1 (2026-10-01・反証 medium「ほぼ黒の株が床のシールに見える」): 色表の中で一段明るく (GREEN[k] → GREEN[k+1]・最上段はそのまま)。
        # 札の材質は受光 0.25 で地面 (0.8) より暗く写るので、地面との差を縮める。色表の外の色は作らない (art-bible)
        out = img.copy()
        for k in range(len(GREEN) - 1, -1, -1):
            src = np.all(img[..., :3] == np.array(GREEN[k], np.uint8), axis=-1) & (img[..., 3] > 0)
            out[src, :3] = GREEN[min(len(GREEN) - 1, k + TUFT_LIFT)]
        img = out
    if TUFT_ROOT_TRIM and len(cols):
        return img[:, cols[0]:cols[-1] + 1]
    return _crop_alpha(img)


# ---------------------------------------------------------------- 絵の写し (垂れ葉・大樹・Art/props の4枚)
def canopy_navy(src):
    """明るさの順位で紺緑の4段へ (最暗 15%・暗 25%・中 22%＝青と緑を半々・紺緑 38%)。アルファはそのまま"""
    a = np.array(Image.open(src).convert('RGBA'))
    m = a[..., 3] > 0
    l = 0.2126 * a[..., 0] + 0.7152 * a[..., 1] + 0.0722 * a[..., 2]
    q = np.quantile(l[m], [0.15, 0.40, 0.62])
    rng = np.random.default_rng(191)
    out = a.copy()
    ys, xs = np.nonzero(m)
    for y, x in zip(ys, xs):
        v = l[y, x]
        c = NAVY_BLACK if v < q[0] else NAVY_DEEP if v < q[1] else (NAVY_BLUE if rng.random() < 0.5 else NAVY_GREEN) if v < q[2] else NAVY
        out[y, x, :3] = c
    return out


GIANT_CUT_ROW = 172          # この行から下 (土の皿) を捨てる
GIANT_BAND_TOP = 150         # この行から下で、幹の円錐の外と石・紺の色を消す
STONEISH = {hx(c) for c in ('#6c767b', '#818182', '#4c4d4e', '#64696d', '#858789', '#5b5a5b', '#606062', '#928f90', '#a5b274', '#7e7974')}
NAVYISH = {hx(c) for c in ('#172d44', '#204043', '#112f23')}


def giant_cut(src):
    a = np.array(Image.open(src).convert('RGBA'))
    H, W = a.shape[:2]
    out = a.copy()
    for y in range(GIANT_BAND_TOP, H):
        for x in range(W):
            if out[y, x, 3] == 0:
                continue
            k = y - GIANT_BAND_TOP
            inside = 55 - k * 1.3 <= x <= 100 + k * 1.3
            c = tuple(int(v) for v in out[y, x, :3])
            if y >= GIANT_CUT_ROW or not inside or c in NAVYISH or c in STONEISH:
                out[y, x] = (0, 0, 0, 0)
    out = out[:GIANT_CUT_ROW]
    # いちばん大きなつながり (8近傍) だけ残す (切った後に残る石のかけら)
    op = out[..., 3] > 16
    lab = np.zeros(op.shape, np.int32)
    n = 0
    sizes = {}
    for y0 in range(op.shape[0]):
        for x0 in range(op.shape[1]):
            if not op[y0, x0] or lab[y0, x0]:
                continue
            n += 1
            st = [(y0, x0)]
            lab[y0, x0] = n
            cnt = 0
            while st:
                y, x = st.pop()
                cnt += 1
                for dy in (-1, 0, 1):
                    for dx in (-1, 0, 1):
                        yy, xx = y + dy, x + dx
                        if 0 <= yy < op.shape[0] and 0 <= xx < op.shape[1] and op[yy, xx] and not lab[yy, xx]:
                            lab[yy, xx] = n
                            st.append((yy, xx))
            sizes[n] = cnt
    if sizes:
        keep = max(sizes, key=sizes.get)
        out[(lab != keep) & op] = (0, 0, 0, 0)
    return out


PROPS_MAP = ('act1_tree_pine', 'act1_tallgrass1', 'act1_stump', 'act1_waystone')


def props_map(name):
    pal = SP.load_palette(PAL_JSON)
    a = SP.load_rgba(os.path.join(ART, 'props', name + '.png'))
    return SP.map_image(a, pal)


# ---------------------------------------------------------------- 座席の土 (二周目)
DRY_R2 = 0.91   # 乾いた明るい斑 (灰色がかった #706259) になる値。W3b は 0.85 = 大きな灰色のしみが目立った (段2 のシート)


def earth_tile_r2(seed, n=64):
    """W3b の earth_tile の作り直し (計画 §2 D 段2 の 7・N18)。違い: 明るい粒 1.2%→0.4% (明るい方の色だけ)・
    小石 6〜8→2〜3 個で暗い石・細かい斜めの筋 (本家 ot11 の土) を 10〜16 本・長さ 3〜7・向きは右下がりが主で乱数・
    乾いた灰色の斑を小さく (DRY_R2)"""
    rng = np.random.default_rng(seed)
    f = 0.62 * periodic_noise(n, 4, rng) + 0.38 * periodic_noise(n, 8, rng)
    f = (f - f.min()) / max(1e-6, f.max() - f.min())
    jitter = rng.random((n, n))
    img = np.zeros((n, n, 3), np.uint8)
    for y in range(n):
        for x in range(n):
            v = f[y, x] + (jitter[y, x] - 0.5) * 0.09
            img[y, x] = EARTH[1] if v < 0.20 else EARTH[2] if v < 0.44 else EARTH[3] if v < DRY_R2 else EARTH[4]
    g = rng.random((n, n))
    for y in range(n):
        for x in range(n):
            if g[y, x] < 0.025:
                img[y, x] = EARTH[1] if f[y, x] > 0.3 else EARTH[0]
            elif g[y, x] > 0.996:
                img[y, x] = EARTH[5]
    # 細かい斜めの筋 (掻いた跡)。1 本ごとに向き・長さ・位置が乱数。暗い線の上に1段明るい縁を半分の確率で
    for _ in range(int(rng.integers(10, 17))):
        x, y = int(rng.integers(0, n)), int(rng.integers(0, n))
        L = int(rng.integers(3, 8))
        down = rng.random() < 0.8
        step = int(rng.integers(1, 3))                      # 1 = 45°・2 = なだらか
        ridge = rng.random() < 0.5
        for i in range(L):
            base = f[y % n, x % n]
            img[y % n, x % n] = EARTH[1] if base > 0.44 else EARTH[0]
            if ridge:
                img[(y - 1) % n, x % n] = EARTH[4] if base > 0.44 else EARTH[3]
            x += 1
            if i % step == 0:
                y += 1 if down else -1
    for _ in range(2):
        x, y = int(rng.integers(0, n)), int(rng.integers(0, n))
        length = int(rng.integers(6, 12))
        dirx = 1 if rng.random() < 0.5 else -1
        for i in range(length):
            img[y % n, x % n] = EARTH[0]
            x += dirx
            r = rng.random()
            if r < 0.22:
                y += 1
            elif r < 0.44:
                y -= 1
    stones = poisson(n, int(rng.integers(2, 4)), 20, rng)
    for (sx, sy) in stones:
        w = int(rng.integers(2, 4))
        h = 1 if w == 2 and rng.random() < 0.5 else 2
        for yy in range(h):
            for xx in range(w):
                c = STONE[2] if (xx == 0 and yy == 0) else STONE[1] if yy == 0 else STONE[0]
                img[(sy + yy) % n, (sx + xx) % n] = c
        for xx in range(1, w + 1):
            img[(sy + h) % n, (sx + xx) % n] = EARTH[0]
    for (gx, gy) in poisson(n, int(rng.integers(3, 5)), 18, rng, avoid=stones):
        img[gy % n, gx % n] = GREEN[3]
        img[(gy + 1) % n, gx % n] = GREEN[2]
        if rng.random() < 0.6:
            img[gy % n, (gx + 1) % n] = GREEN[2]
            img[(gy - 1) % n, (gx + 1) % n] = GREEN[3]
    return img


def grass_seat_r2(workdir, contrast=0.45):
    """草 = scripts/tile-calm.py --contrast 0.45 --no-auto (W3b は 0.6) → stage-palette の map。4 枚の配列を返す"""
    src = os.path.join(REPO, 'docs', 'pixellab', 'hd2d-act1', 'raw', 'tiles', 'top_grass_s2.png')
    subprocess.run([sys.executable, '-B', os.path.join(REPO, 'scripts', 'tile-calm.py'), '--outdir', workdir, '--name', 'top_grass_seat',
                    '--contrast', str(contrast), '--no-auto', src], check=True, stdout=subprocess.DEVNULL)
    pal = SP.load_palette(PAL_JSON)
    return {v: SP.map_image(SP.load_rgba(os.path.join(workdir, 'top_grass_seat_%s.png' % v)), pal) for v in 'abcd'}


# ---------------------------------------------------------------- 並べ方
# (名前, 作る関数) — 幅と高さ・種は固定 (同じ入力なら同じ絵)
BARK = os.path.join(ACT1, 'tiles', 'side_bark_%s.png')
TRUNK_LONG = [(48, 'a', 101, 3), (40, 'b', 102, 2), (56, 'c', 103, 4)]     # 幹の幅・樹皮・種・苔の筋
TRUNK_THIN = [(20, 'c', 111), (16, 'a', 112), (24, 'b', 113)]
BOUGHS = [(96, 48, 121), (120, 56, 122), (84, 42, 123), (108, 60, 124)]
TUFTS = [(12, 5, 131), (12, 6, 132), (16, 7, 133), (20, 9, 134)]           # 背丈・葉の数・種 (1・2 は背丈 12＝手前の株)
TUFT_LIFT = 1   # 直しの輪1: 立った草の株の色を色表の中で何段明るくするか (0 = 二周目の段2 の絵)
TUFT_ROOT_TRIM = True   # 直しの輪2: 株の下 2 行を台形にし、最下段は茎 2〜3 本の根元だけ (False = 直しの輪1 の絵)

R2_ITEMS = ('backdrop', 'trunk_long', 'trunk_thin', 'needle_bough', 'tuft_stand', 'canopy_navy', 'giant_cut', 'props_map', 'seat_soil')
# 頼んだ時だけ作る物 (--only で名前を書く)。草は段2 の試しで「作り直さない」と決まった (画面の草の縁 PC 78〜92・スマホ 172〜180 で目標 250 以下)
R2_OPTIONAL = ('seat_grass',)


def _save(img, path, mode='RGBA'):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    Image.fromarray(img, mode).save(path, optimize=True)
    return path


def make_r2(root, only=None):
    items = [i for i in R2_ITEMS + R2_OPTIONAL if (only is None and i in R2_ITEMS) or (only is not None and i in only)]
    written = []
    R = lambda n: os.path.join(root, 'relief', n + '.png')
    for it in items:
        if it == 'backdrop':
            written.append(_save(backdrop_plain(), R('backdrop_plain')))
        elif it == 'trunk_long':
            for i, (w, b, seed, moss) in enumerate(TRUNK_LONG):
                written.append(_save(trunk_r2(BARK % b, w, 280, seed, flare=1.75, taper=0.2, nbranch=(3, 6), moss=moss, twig=0.6), R('trunk_long_%d' % (i + 1))))
        elif it == 'trunk_thin':
            for i, (w, b, seed) in enumerate(TRUNK_THIN):
                written.append(_save(trunk_r2(BARK % b, w, 280, seed, flare=1.5, taper=0.25, nbranch=(2, 4), shade_k=0.66, twig=0.8), R('trunk_thin_%d' % (i + 1))))
        elif it == 'needle_bough':
            for i, (w, h, seed) in enumerate(BOUGHS):
                written.append(_save(needle_bough(seed, w, h), R('needle_bough_%d' % (i + 1))))
        elif it == 'tuft_stand':
            for i, (h, nb, seed) in enumerate(TUFTS):
                written.append(_save(tuft_stand(seed, h, nb), R('tuft_stand_%d' % (i + 1))))
        elif it == 'canopy_navy':
            written.append(_save(canopy_navy(os.path.join(ART, 'props', 'act1_canopy.png')), R('act1_canopy_navy')))
        elif it == 'giant_cut':
            written.append(_save(giant_cut(os.path.join(ACT1, 'relief', 'act1_tree_giant.png')), R('act1_tree_giant_cut')))
        elif it == 'props_map':
            for name in PROPS_MAP:
                written.append(_save(props_map(name), R(name)))
        elif it == 'seat_soil':
            # 新しい名前で出す (W5 の写し act1_layout_w5 が読む top_path_seat_{a..d} を画素まで守る)。設計図の tiles.pathSeat の候補の先頭に足して使う
            for i, v in enumerate('abcd'):
                written.append(_save(earth_tile_r2(950 + i), os.path.join(root, 'tiles', 'top_path_seat_r2_%s.png' % v), 'RGB'))
        elif it == 'seat_grass':
            with tempfile.TemporaryDirectory() as td:
                for v, a in grass_seat_r2(td).items():
                    written.append(_save(a, os.path.join(root, 'tiles', 'top_grass_seat_r2_%s.png' % v)))
    return written


# ---------------------------------------------------------------- 点検
def _cielab(rgb):
    c = np.asarray(rgb, float) / 255.0
    c = np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)
    M = np.array([[0.4124, 0.3576, 0.1805], [0.2126, 0.7152, 0.0722], [0.0193, 0.1192, 0.9505]])
    xyz = c @ M.T / np.array([0.95047, 1.0, 1.08883])
    f = np.where(xyz > 0.008856, np.cbrt(xyz), 7.787 * xyz + 16 / 116)
    L = 116 * f[..., 1] - 16
    a = 500 * (f[..., 0] - f[..., 1])
    b = 200 * (f[..., 1] - f[..., 2])
    return L, a, b


def bright_specks(img):
    """N18 の数え方をタイルに当てる: 3×3 に敷いた真ん中で、7×7 の中央値より輝度が 18 以上明るい画素を1万画素あたり"""
    rgb = img[..., :3].astype(float)
    l = 0.2126 * rgb[..., 0] + 0.7152 * rgb[..., 1] + 0.0722 * rgb[..., 2]
    n = l.shape[0]
    big = np.tile(l, (3, 3))
    from numpy.lib.stride_tricks import sliding_window_view
    win = sliding_window_view(np.pad(big, 3, mode='wrap'), (7, 7))
    med = np.median(win, axis=(-1, -2))
    mid = (big - med)[n:2 * n, n:2 * n]
    return float((mid >= 18).sum()) / (n * n) * 10000.0


def check(paths):
    pal = SP.load_palette(PAL_JSON)
    for p in paths:
        a = SP.load_rgba(p)
        m = a[..., 3] >= 128
        d = SP.palette_distance(a, pal)
        L, aa, bb = _cielab(a[..., :3][m]) if m.any() else (np.zeros(1),) * 3
        hue = math.degrees(math.atan2(float(np.mean(bb)), float(np.mean(aa)))) % 360 if m.any() else 0.0
        chroma = float(np.hypot(np.mean(aa), np.mean(bb))) if m.any() else 0.0
        line = '%-34s %4d×%-4d 不透明 %5.1f%%  色表 p95 %.1f  平均の色相 %3.0f°  C* %4.1f  L* %4.1f' % (
            os.path.basename(p), a.shape[1], a.shape[0], 100.0 * m.mean(), float(np.percentile(d, 95)) if len(d) else 0.0, hue, chroma, float(np.mean(L)))
        if a.shape[0] == a.shape[1] == 64 and m.all():
            line += '  明るい点 %.0f/1万' % bright_specks(a)
        print(line)


def sheet(paths, out, scale=4):
    """確認のシート: 夜の地 (#262a3a) に 4 倍 (タイルは 2×2 に敷いて 2 倍) で並べる"""
    from PIL import ImageDraw
    items = []
    for p in paths:
        im = Image.open(p).convert('RGBA')
        if im.width == im.height == 64 and '/tiles/' in p.replace('\\', '/'):
            t = Image.new('RGBA', (128, 128))
            for x0 in (0, 64):
                for y0 in (0, 64):
                    t.paste(im, (x0, y0))
            im = t.resize((256, 256), Image.NEAREST)
        else:
            im = im.resize((im.width * scale, im.height * scale), Image.NEAREST)
        items.append((os.path.basename(p), im))
    Wmax = 2400
    x = y = 10
    rowh = 0
    pos = []
    for name, im in items:
        if x + im.width > Wmax:
            x = 10
            y += rowh + 24
            rowh = 0
        pos.append((x, y))
        x += im.width + 16
        rowh = max(rowh, im.height)
    sh = Image.new('RGBA', (Wmax, y + rowh + 30), (38, 42, 58, 255))
    dr = ImageDraw.Draw(sh)
    for (name, im), (px, py) in zip(items, pos):
        sh.alpha_composite(im, (px, py + 14))
        dr.text((px, py), name, fill=(220, 220, 210, 255))
    sh.convert('RGB').save(out)


# ================================================================ 入口
W3B_STATS_KEYS = ('mean', 'std', 'p10', 'p90')


def make_w3b(root, sheet_out=None):
    """W3b の絵 (作業場 p22b/gen_art.py の main と同じ手順・同じ種)"""
    os.makedirs(root + '/tiles', exist_ok=True)
    os.makedirs(root + '/relief', exist_ok=True)
    os.makedirs(root + '/litter', exist_ok=True)
    stats = {}
    for i, v in enumerate('abcd'):
        img = earth_tile(930 + i)
        Image.fromarray(img).save(root + '/tiles/top_path_seat_%s.png' % v)
        l = 0.2126 * img[..., 0] + 0.7152 * img[..., 1] + 0.0722 * img[..., 2]
        stats['top_path_seat_' + v] = dict(mean=round(float(l.mean()), 1), std=round(float(l.std()), 1),
                                            p10=round(float(np.percentile(l, 10)), 1), p90=round(float(np.percentile(l, 90)), 1))
    bark = REPO + '/unity/Assets/Resources/Art/stage/act1/tiles/side_bark_%s.png'
    for i, (w, b) in enumerate([(26, 'a'), (20, 'c'), (32, 'b')]):
        t = trunk(bark % b, w, 200, 11 + i)
        Image.fromarray(t, 'RGBA').save(root + '/relief/trunk_tall_%d.png' % (i + 1))
    for name, (rows, cols) in LITTER.items():
        Image.fromarray(sprite(rows, cols), 'RGBA').save(root + '/litter/%s.png' % name)
    json.dump(stats, sys.stdout, ensure_ascii=False, indent=1)
    print()
    if sheet_out:
        paths = [root + '/tiles/top_path_seat_%s.png' % v for v in 'abcd'] + [root + '/relief/trunk_tall_%d.png' % i for i in (1, 2, 3)]
        paths += [root + '/litter/%s.png' % n for n in LITTER]
        sheet(paths, sheet_out)


def main():
    import argparse
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest='cmd', required=True)
    w = sub.add_parser('w3b')
    w.add_argument('root')
    w.add_argument('--sheet')
    r = sub.add_parser('r2')
    r.add_argument('root')
    r.add_argument('--only', help='カンマ区切り: ' + ','.join(R2_ITEMS + R2_OPTIONAL))
    r.add_argument('--sheet')
    c = sub.add_parser('check')
    c.add_argument('files', nargs='+')
    args = ap.parse_args()
    if args.cmd == 'w3b':
        make_w3b(args.root, args.sheet)
    elif args.cmd == 'r2':
        only = None
        if args.only:
            only = [x.strip() for x in args.only.split(',') if x.strip()]
            bad = [x for x in only if x not in R2_ITEMS + R2_OPTIONAL]
            if bad:
                sys.exit('知らない名前: %s (選べるのは %s)' % (','.join(bad), ','.join(R2_ITEMS + R2_OPTIONAL)))
        written = make_r2(args.root, only)
        check(written)
        if args.sheet:
            sheet(written, args.sheet)
    else:
        check(args.files)


if __name__ == '__main__':
    main()

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
  python3 -B docs/pixellab/hd2d-act1/gen_art.py r3 <out> [--only <名前,...>] [--sheet <png>] [--info <json>]
      三周目の絵 (下の R3_ITEMS。2026-10-01 レーン D・計画 docs/design/hd2d-round3-plan-2026-10-01.md §2 D)。種は固定＝同じ絵が出る。
      今ある絵は上書きしない (どれも新しい名前)。最後に色表 p95 = 0・半透明なし・大きさを点検 (NG なら終了コード 1)。
  python3 -B docs/pixellab/hd2d-act1/gen_art.py fogmodel <png...> --out <png> [--fog 0.55 --sigma 3 --tint 0.5 --px 3 --ref <本家の切り抜き>]
      霧の帯での見え方の模型 (1 ドット = px 画素・左上の手前からの光〔隣に <名前>_n.png があれば法線あり／なしの2枚〕・tint・霧・σ のぼかし)。
  python3 -B docs/pixellab/hd2d-act1/gen_art.py check <png...>
      色表からの距離 (p95)・大きさ・不透明の割合・CIELab の色相の中央値・(タイルは) 明るい点の数 (N18 の数え方＝7×7 の中央値より 18 以上明るい画素を1万画素あたり)。

三周目の絵 (R3_ITEMS。名前はレーン間の取り決め 1。α は 0 か 255・色は色表の 48 色だけ・光は左上から):
  段1b (D2・2026-10-02) で conifer・bough_hang・bush_clump を描き直し、conifer_dense を足した (試しの撮影 trial1 の所見:
  垂れる枝が椰子の葉・帯の木が傘と灰色の柱・茂みが青い丸い生け垣に見えた)。共通の部品は「段」(_r3_tier＝外へ張り出して少し垂れる
  針葉の層・下の縁はのこぎり 2.5〜4.5 ドットの歯・上の縁だけ明るい・中に外と下へ向かう斜めの針の筋) で、下の段から描いて上の段が上書きする。
  conifer        relief/conifer_w{20,32,48}_{1,2}.png   帯の木・奥の木 (R1)。高さ 280・幹の幅 20/32/48 (根元の張り出しの上)・絵の幅は幹の 3.6 倍前後。
                 幹は暗い影絵 (光の側を二周目の段1 より約 25% 暗く＝筋 #776c63/#706259・山 #706259/#5b5a5b・溝 #1c1d1c)＋左の縁の明るい筋 2〜3 ドット
                 (隣の溝と 81 差)。弓なりの枝 9〜14 本 (下から 28〜80%・上ほど詰む・長さは幹の幅の 1.3〜0.6 倍)＝付け根から上がって先が垂れる弓に
                 細い針葉の層 (先で垂れて尖る)。梢 (上の 20%) は細かい段の密な尖り。足元 (外接の四角の下辺の中央) = 幹の中心 (_r3_balance)
  conifer_dense  relief/conifer_dense_{1..4}.png        近い木・帯の手前の木 (本家 ref16_upperright)。1・2 = 150×300・3・4 = 200×340。細い幹 (根元 11〜16)・
                 段 16〜24 (左右 1 枚ずつ半段ずらす・2 割は幹の手前を横切る)・三角の影絵・葉は高さの 15〜25% から上 (下は幹と短い枯れ枝)
  conifer_near   relief/conifer_near_{1,2}.png          近い幹 (R2)。幅 36/42・高さ 320 (画面の上を突き抜ける)・枝の付け根の残り 2〜4・房つきの枝 1〜2
  bough_hang     relief/bough_hang_{1..4}.png           垂れる枝 160×80 (R2)。付け根は絵の左の辺 (太さ 6・上から 8〜14 行)。段 3〜6 枚を棚のように縦に積み
                 (下の段ほど右から始まる＝付け根で細く右へ行くほど厚い楔形・先は少し垂れる)。芯の線は付け根の樹皮だけ
  bush_clump     relief/bush_clump_{l,m,s}_{1,2}.png    低木の塊 96×64・64×48・48×32 (R4)。高さは絵の 66〜74% (＋小枝と葉で 70〜80%・上は透明)・
                 低く横に長い不規則な山 3〜8 個の重なり (左右非対称)・上の輪郭は小枝と針の先が 2〜8 ドット突き出してぎざぎざ・V 字の切れ込み・
                 中は暗い緑 (二周目の段1 より約 23% 暗い)・底は暗い・草の葉 3〜6 本
  fore_grass     relief/fore_grass_{1..6}.png           手前の額縁の草 (R6)。act1_tallgrass1・act1_fern の形を太い葉で描き直した暗い影絵・葉先の間隔 11 ドット以上
  seat_soil      tiles/top_path_seat_r3_{a..d}.png      座席の土 = top_path_seat_r2 に 1〜2 テクセルのノイズ (明るさ ±6%・土の段の隣へ確率で移る形)

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


# ================================================================ 三周目 (2026-10-01 レーン D)
# 計画 docs/design/hd2d-round3-plan-2026-10-01.md §2 D・分析 docs/design/hd2d-r3-analysis-2026-10-01.md の R1・R2・R4・R6・§11-11。
# 共通の決まり:
#  - 色は幕1の色表 48 色だけ (下の C3 から選んで塗る。最後に stage-palette の map を通して確かめる)。光は左上から。
#  - α は 0 か 255 だけ (半立体は α 0.4 で切る＝半透明の房は網になる。房・茂み・草は不透明の影絵で塗り、薄さは霧と tint で付ける)。
#  - 足元 = 不透明の外接の四角の下辺の中央 (DioramaTextures.BuildAtlas が透明の余白を切る)。木は幹の中心と外接の四角の中心をそろえる
#    (_r3_balance が短い側の端に数ドットの小枝を足す)。
#  - 筋は 2〜3 ドット幅・明暗差 (輝度) 40 以上 (霧の帯の幹は画面で 1 ドット ≈3px なのに奥のぼかしが 2.4〜3.9px＝1 ドット幅の筋は消える。§11-11)。
#  - 房は塊で描く (細い針を等間隔に並べると「くし」、薄い板は「旗」、穴のある房は「網」に見える)。下の縁のぎざぎざは乱数で長さも位置も散らす。
#  - 段1b (D2): 1 本の弓＋房の縁取りは「椰子の葉」、疎らな弓は「傘」、丸い房の並びは「生け垣」に見えた → 針葉は「段」(_r3_tier) を重ねて描く。

C3 = {name: hx(h) for name, h in (
    ('k0', '#0b081c'), ('k1', '#16151e'), ('k2', '#1c1d1c'),
    ('g0', '#112f23'), ('b0', '#172d44'), ('g1', '#193d20'), ('n0', '#204043'),
    ('g2', '#2d482f'), ('g3', '#364e33'), ('g4', '#3b5434'), ('g5', '#4a5a40'), ('g6', '#4f6443'),
    ('gy1', '#4c4d4e'), ('gy2', '#535457'), ('gy3', '#64696d'), ('gy4', '#6a6f73'), ('gy6', '#858789'), ('gy7', '#928f90'),
    # 樹皮の光の側 (暖かい灰。舞台の色寄せ envGrade で青灰へ寄る＝本家の霧の幹の淡い藤色)
    ('w0', '#3d332f'), ('w1', '#474641'), ('w2', '#776c63'), ('w3', '#867e72'), ('w4', '#5b5a5b'),
    # 段1b (D2): 暗い幹の光の側 (今の w3/w2 より約 25% 暗い暖かい灰)・暗い幹の中間
    ('w5', '#706259'), ('w6', '#3f3f38'),
)}

LAY_EMPTY, LAY_TRUNK, LAY_BARK, LAY_NEEDLE = 0, 1, 2, 3


def _rnd(v):
    """四捨五入 (Python の round は .5 を偶数へ丸める＝斜めに流す房で 1 列おきの穴ができた)"""
    return int(math.floor(v + 0.5))


def _knots(n, step, rng):
    """1 次元の値ノイズ (step ドットごとの乱数を線形に)。0〜1"""
    k = rng.random(int(n // step) + 3)
    return np.interp(np.arange(n), np.arange(len(k)) * float(step), k)


def _noise2(h, w, cell, rng):
    """2 次元の値ノイズ (巻き戻さない・cell ドットの格子・なめらかな補間)。0〜1"""
    gh, gw = int(h // cell) + 3, int(w // cell) + 3
    g = rng.random((gh, gw))
    ys, xs = np.mgrid[0:h, 0:w]
    fy, fx = ys / float(cell), xs / float(cell)
    y0, x0 = np.floor(fy).astype(int), np.floor(fx).astype(int)
    ty, tx = fy - y0, fx - x0
    ty = ty * ty * (3 - 2 * ty)
    tx = tx * tx * (3 - 2 * tx)
    a = g[y0, x0] * (1 - tx) + g[y0, x0 + 1] * tx
    b = g[y0 + 1, x0] * (1 - tx) + g[y0 + 1, x0 + 1] * tx
    return a * (1 - ty) + b * ty


def _set(img, lay, y, x, col, kind):
    h, w = lay.shape
    if 0 <= y < h and 0 <= x < w:
        img[y, x] = (*C3[col], 255)
        lay[y, x] = kind


# ---------------------------------------------------------------- 幹 (影絵＋左の縁の明るい筋＋樹皮の縦の筋)
def _r3_trunk(img, lay, cx0, w, wtop, ybot, ytop, rng, flare=1.35, flare_rows=14, sway=1.2, near=False, crown=None, dark=False):
    """幹を行 ytop〜ybot に描く。cos の円筒陰影は使わない (霧に沈めると勾配しか残らなかった＝二周目の柱)。
    左から: 明るい筋 (2〜3 ドット・外 #867e72・芯 #928f90、途切れる所は #776c63) | 暗い溝 1 | 樹皮の板 (山 2〜3 と溝 2〜4 の縦の筋・左ほど明るい) | 右の縁は最暗。
    光の側 (左 3 割・近い幹は 2 割) は暖かい灰の樹皮 (山 #867e72 127 / #776c63 110 対 溝 #3d332f 53 = 57〜74)、影の側は紺 (山 #5b5a5b 90・#474641 70・
    #204043 57・#172d44 42 対 溝 #1c1d1c 29 / #16151e 22 = 20〜61)。灰色の石の柱に見えないよう、光は暖かく影は紺 (舞台の色寄せで青灰へ寄る)。
    霧 0.55 では暗い色ほど差が潰れる (線形で混ぜるので) → 光の側を明るい樹皮にして、霧の中で読める筋と「左が明るく右が暗い」を左に集める
    (山 #867e72 と溝 #3d332f は線形で 0.19 の差＝霧 0.55 の後も約 25 段)。
    筋と隣の溝は 81 以上。筋の幅は山ごと溝ごとに 2〜3 の乱数 (縞模様にしない)・山は 16〜45 行ごとに 1〜2 行の割れ目で切れる (長い樹皮の板。短いと煉瓦の壁に見えた)。
    横の位置は 23 行ごとの値ノイズでゆっくり最大 2 ドットずれる。返り値: 行 → (左の縁, 右の縁)
    dark = 段1b (D2) の暗い幹 (帯の木・密な針葉樹): 光の側を約 25% 暗く (筋 外 #706259 100・芯 #776c63 110、山 #706259 / #5b5a5b、
    溝 #1c1d1c 29＝山と溝の線形の差は今の #776c63 対 #3d332f と同じ)・影の側も 1 段暗く (#474641・#3f3f38・#204043・#172d44)。
    筋と隣の溝は 71 以上 (明暗差 40 以上を守る)。灰色の柱に見えないように (試しの撮影 T3-ogre-hideui の帯の幹 x 620〜720)"""
    H, W = lay.shape
    n = ybot - ytop + 1
    swv = (_knots(H, 37, rng) - 0.5) * 2.0 * sway
    phase = _knots(H, 23, rng) * 2.0
    hb = _knots(H, 7, rng)
    buttress = float(rng.uniform(-0.5, 0.5))
    # 樹皮の筋の並び (幹の左から右へ: 溝・山・溝・山…。幅は 2〜3 の乱数)。山ごとの割れ目の行と、割れ目でのずれ
    bounds, kinds, x = [], [], 0.0
    ridge = False
    while x < w * 1.6 + 8:
        wd = float(rng.choice([2.0, 2.0, 3.0])) if ridge else float(rng.choice([2.0, 3.0, 3.0, 4.0]))
        bounds.append((x, x + wd))
        kinds.append(ridge)
        x += wd
        ridge = not ridge
    cracks = []
    for _ in bounds:
        rows, yy = set(), ytop + int(rng.integers(0, 30))
        while yy < ybot:
            rows.add(yy)
            if rng.random() < 0.3:
                rows.add(yy + 1)
            yy += int(rng.integers(16, 46))
        cracks.append(rows)
    edges = {}
    for y in range(ytop, ybot + 1):
        yb = ybot - y
        k = (y - ytop) / max(1.0, n - 1.0)               # 0 = 上・1 = 根元
        if crown is None:
            wt = wtop + (w - wtop) * k ** 0.9
        else:
            # 針葉樹: 梢の下 (crown = 下からの高さの割合) までは柱に近く (根元の 6 割まで細る)、梢の中で先へ急に細る (梢の房に隠れる)
            h = 1.0 - k
            if h <= crown:
                wt = w * (1.0 - 0.4 * (h / crown) ** 1.4)
            else:
                wt = wtop + (w * 0.6 - wtop) * (1.0 - (h - crown) / (1.0 - crown)) ** 1.2
        cs = 0.0
        if yb < flare_rows:
            q = (flare_rows - yb) / float(flare_rows)
            wt *= 1 + (flare - 1) * q ** 3
            cs = buttress * wt * q ** 3 * 0.25
        c = cx0 + swv[y] * (1.0 - k) + cs                # 揺れは上ほど (根元は動かさない)
        left, right = c - wt / 2.0, c + wt / 2.0
        edges[y] = (left, right)
        sw = 3 if wt >= 13 else 2 if wt >= 6 else 1
        re = 2 if wt >= 10 else 1
        row = []                                              # (x, 種類 s=筋 e=縁 r=山 g=溝, 山の色, 溝の色)
        for x in range(int(math.floor(left)), int(math.ceil(right)) + 1):
            xc = x + 0.5
            if xc < left or xc > right or x < 0 or x >= W:
                continue
            du, dr = xc - left, right - xc
            if du < sw:
                br = hb[y] < 0.22
                if du < 1 and sw >= 2:
                    col = ('w4' if br else 'w5') if dark else ('w2' if br else 'w3')
                else:
                    col = ('w5' if br else 'w2') if dark else ('w2' if br else 'gy7')
                row.append((x, 's', col, col))
            elif dr < re:
                row.append((x, 'e', 'k1', 'k1'))
            else:
                bu = du - sw
                bw = max(1.0, wt - sw - re)
                f = bu / bw
                v = bu + phase[y]
                j = 0
                while j < len(bounds) - 1 and v >= bounds[j][1]:
                    j += 1
                ridge = kinds[j] and bu >= 1.0 and y not in cracks[j]
                lit = f < (0.22 if near else 0.3)                  # 光の側 (左 2〜3 割)
                if dark:
                    rcol = ('w5' if j % 4 == 1 else 'w4') if lit else 'w1' if f < 0.45 else 'w6' if f < 0.62 else 'n0' if f < 0.82 else 'b0'
                    gcol = 'k2' if (lit or f < 0.45) else 'k1'
                else:
                    rcol = ('w3' if j % 4 == 1 else 'w2') if lit else 'w4' if f < 0.45 else 'w1' if f < 0.62 else 'n0' if f < 0.82 else 'b0'
                    gcol = 'w0' if (lit and bu >= 1.0) else 'k2' if f < 0.45 else 'k1'
                row.append((x, 'r' if ridge else 'g', rcol, gcol))
        # 1 ドット幅の山と溝は消す (霧の帯では奥のぼかしで消える＝2〜3 ドットの筋だけを残す。溝の始まりと右の縁で切れた山がここに来る)
        kinds_row = [k for (_, k, _, _) in row]
        for i in range(len(row)):
            k = kinds_row[i]
            if k not in 'rg':
                continue
            lk = kinds_row[i - 1] if i > 0 else 'e'
            rk = kinds_row[i + 1] if i + 1 < len(row) else 'e'
            if k == 'r' and lk != 'r' and rk != 'r':
                kinds_row[i] = 'g'
            elif k == 'g' and lk == 'r' and rk == 'r':
                kinds_row[i] = 'r'
        for (x, _, rcol, gcol), k in zip(row, kinds_row):
            _set(img, lay, y, x, rcol if k in 'sre' else gcol, LAY_TRUNK)
    return edges


def _r3_path(x0, y0, side, L, s0, droop):
    """枝の芯の点列 (1 ドットごと)。s0 = 付け根の上がり (1 ドットあたり)・droop = 先の垂れ (長さに対する割合)"""
    pts = []
    for i in range(_rnd((L)) + 1):
        u = i / max(1.0, L)
        pts.append((x0 + side * i, y0 - s0 * i + droop * L * u ** 2.2))
    return pts


def _r3_bough(img, lay, pts, t0):
    """枝の樹皮。付け根が太く (t0)、先は 1。上の縁の元の 4 割だけ少し明るい (#4c4d4e)。縦の隙間は前の点から埋める"""
    n = len(pts)
    prev = None
    for i, (x, y) in enumerate(pts):
        tt = max(1, _rnd((t0 * (1.0 - i / max(1.0, n - 1.0)) ** 0.8 + 0.4)))
        xi = _rnd((x))
        yc = _rnd((y))
        ylo, yhi = yc - tt // 2, yc - tt // 2 + tt - 1
        if prev is not None:
            ylo, yhi = min(ylo, prev[0]), max(yhi, prev[1])
        for yy in range(ylo, yhi + 1):
            if yy == ylo and tt >= 2:
                col = 'gy1' if i < n * 0.4 else 'k2'
            elif yy == yhi:
                col = 'k1'
            else:
                col = 'k2'
            _set(img, lay, yy, xi, col, LAY_BARK)
        prev = (yc - tt // 2, yc - tt // 2 + tt - 1)


def _r3_spikes(lay, rng, p=0.16, maxlen=2):
    """房の下の縁から針の先を 1〜2 ドット垂らす (確率 p・長さも横のずれも乱数＝等間隔のくしにしない)"""
    H, W = lay.shape
    nm = lay == LAY_NEEDLE
    occ_below = np.zeros_like(nm)
    occ_below[:-1] = lay[1:] > 0
    ys, xs = np.nonzero(nm & ~occ_below)
    for y, x in zip(ys, xs):
        if rng.random() >= p:
            continue
        dx = 0
        for i in range(1, int(rng.integers(1, maxlen + 1)) + 1):
            if rng.random() < 0.3:
                dx += 1 if rng.random() < 0.5 else -1
            yy, xx = y + i, x + dx
            if 0 <= yy < H and 0 <= xx < W and lay[yy, xx] == LAY_EMPTY:
                lay[yy, xx] = LAY_NEEDLE


def _r3_color_needles(img, lay, rng, p_blue=0.55, cell=3.0):
    """房の色 (影絵・光は左上): 上の縁 = #204043 (7 割)・#535457 (光の粒 14%)・#364e33。下の縁 = #1c1d1c / #16151e。
    中 = 影の中間 (青 #172d44 と緑 #112f23 を p_blue で混ぜる＝平均の色相 205〜215°) に、3 ドットの値ノイズで明るい塊 (#204043) と
    暗い塊 (#1c1d1c)。上の縁から深い所ほど暗い塊が増える (房の下半分は影)"""
    H, W = lay.shape
    nm = lay == LAY_NEEDLE
    occ = lay > 0
    up_empty = np.ones_like(nm)
    up_empty[1:] = ~occ[:-1]
    dn_empty = np.ones_like(nm)
    dn_empty[:-1] = ~occ[1:]
    dtop = np.zeros((H, W), np.int16)
    for y in range(1, H):
        dtop[y] = np.where(nm[y] & nm[y - 1], dtop[y - 1] + 1, 0)
    nz = 0.65 * _noise2(H, W, cell, rng) + 0.35 * rng.random((H, W))
    r = rng.random((H, W))
    for y, x in zip(*np.nonzero(nm)):
        rv = r[y, x]
        if up_empty[y, x]:
            c = 'n0' if rv < 0.72 else 'gy2' if rv < 0.86 else 'g3'
        elif dn_empty[y, x]:
            c = 'k2' if rv < 0.7 else 'k1'
        elif dtop[y, x] <= 1 and rv < 0.5:
            c = 'n0'
        elif nz[y, x] > 0.72:
            c = 'n0'
        elif nz[y, x] < 0.26 + 0.04 * min(int(dtop[y, x]), 6):
            c = 'k2'
        else:
            c = 'b0' if rv < p_blue else 'g0'
        img[y, x] = (*C3[c], 255)


def _r3_balance(lay, cx):
    """外接の四角の中心を cx (幹の中心) にそろえる: 短い側のいちばん外の列から、細い小枝 (針) を足りない分だけ横へ伸ばす。足したドット数を返す"""
    H, W = lay.shape
    occ = lay > 0
    cols = np.where(occ.any(0))[0]
    le, re_ = cx - cols[0], cols[-1] + 1 - cx
    d = _rnd((abs(le - re_)))
    if d == 0:
        return 0
    side = 1 if le > re_ else -1
    col = cols[-1] if side > 0 else cols[0]
    ys = np.where(occ[:, col])[0]
    y = int(ys[len(ys) // 2])
    for i in range(1, d + 1):
        x = col + side * i
        if 0 <= x < W:
            lay[y, x] = LAY_NEEDLE
            if i >= d - 1 and y + 1 < H:
                lay[y + 1, x] = LAY_NEEDLE
    return d * side


def _r3_swag(img, lay, x0, y0, side, L, D, rng, t0=3, s0=(0.15, 0.4), droop=(0.55, 0.85), bare=(0.15, 0.28),
             lobe=(4.0, 9.0), up=(1.0, 2.5), lean=0.25, cxc=None, maxx=None, tip=0.12, bark=True):
    """弓なりの枝 1 本: 付け根から少し上がって先が垂れる芯 (樹皮・付け根 t0 → 先 1) と、その下に垂れる房の幕。
    房の幕は元の bare 割を裸にして、その先から先端の少し先 (tip) まで。深さは先ほど深く (先で D 行)、大小 2 つの値ノイズで
    不規則な塊に膨らみ、ときどき 1〜2 列切れる (下の縁が不規則な弧＝細い針や同じ歯を等間隔に並べた「くし」にしない)。下ほど外へ lean 流れる。
    上へ up 行の短い針 (上の縁＝光の側)。cxc と maxx = 幹の中心とそこからの横の最大 (房の外の端まで)。返り値: 芯の点列"""
    a = float(rng.uniform(*s0))
    d = float(rng.uniform(*droop))
    Li = _rnd((L))
    Le = _rnd((L * (1.0 + tip)))
    pts = [(x0 + side * i, y0 - a * L * (i / L) + d * L * (i / L) ** 2) for i in range(Le + 1)]
    if bark:
        _r3_bough(img, lay, pts[:Li + 1], t0)
    H, W = lay.shape
    # 房の深さ = 先ほど深い基準 × ゆっくりの値ノイズ (塊の大きさ lobe ドット) と速いノイズ (lobe の 1/3) の和。
    # 等間隔の歯 (くし) にしないため、塊の幅も深さも不規則・ときどき 1〜2 列の切れ目 (霧が透ける)
    lo, hi = lobe
    slow = _knots(Le + 8, float(rng.uniform(lo, hi)), rng)
    fast = _knots(Le + 8, max(2.0, lo / 2.5), rng)
    i0 = _rnd((float(rng.uniform(*bare)) * L))
    gap_until = -1
    for ii in range(i0, Le + 1):
        if ii > gap_until and rng.random() < 0.035:
            gap_until = ii + int(rng.integers(1, 3))
        u = ii / L
        k = 0.62 * slow[ii] + 0.38 * fast[ii]
        k = min(1.0, max(0.12, 0.2 + 0.95 * k))
        if ii <= gap_until:
            k = 0.1
        ramp = min(1.0, (ii - i0 + 1) / 4.0)              # 房の始まりは浅く
        depth = D * (0.3 + 0.7 * min(1.0, u) ** 0.9) * k * ramp
        if ii > Li:                                       # 先の先 (芯なし) は細って終わる
            depth *= max(0.3, 1.0 - (ii - Li) / max(1.0, Le - Li) * 0.6)
        x, y = pts[ii]
        if cxc is not None and maxx is not None:
            room = maxx - abs(x - cxc)
            if room < 0.5:
                break
            depth = min(depth, room / max(0.05, lean))
        top = -float(rng.uniform(*up)) * (0.4 + 0.6 * k)
        for r in range(int(math.floor(top)), int(math.ceil(depth)) + 1):
            yy = _rnd((y + r))
            xx = _rnd((x + side * lean * max(0, r)))
            if 0 <= yy < H and 0 <= xx < W:
                lay[yy, xx] = LAY_NEEDLE
    return pts


def _r3_foot_meta(lay, cxb, bal):
    """足元の確かめ: 幹の中心 − 外接の四角の中心 (ドット。0 なら足元＝幹の中心)・外接の四角の左の端からの幹の中心"""
    cols = np.where((lay > 0).any(0))[0]
    x0, x1 = int(cols[0]), int(cols[-1]) + 1
    return {'footOffset': round(cxb - 0.5 * (x0 + x1), 2), 'trunkX': round(cxb - x0, 1), 'balance': bal}


# ---------------------------------------------------------------- 段1b (D2): 針葉の段 (tier) と房の塗り
# 試しの撮影 (trial1) の所見: 1 本の弓＋下の房の縁取りは「椰子の葉」、疎らな弓 5〜8 本は「傘」に見えた。本家の近い木
# (ref16_upperright) は細い幹から出た「段」の重なり＝各段は幹から外へ張り出して少し垂れる針葉の層で、下の縁がのこぎり (2〜4 ドットの歯)、
# 上の縁だけ少し明るい。ここではその段を 1 つの部品 (_r3_tier) にして、密な針葉樹・帯の木の梢・垂れる枝・茂みで使い回す。
# 塗りは段ごとにその場で (_r3_paint_cols)。下の段から描いて上の段が上書きする＝上の段の暗い歯が下の段の明るい上の縁に掛かる (本家の段の重なり)。

def _r3_teeth(n, rng, amp=(2.5, 4.5), period=(3, 6), rev=0.12):
    """のこぎりの歯の深さの列 (長さ n)。歯は外へ向かって深くなり、外の端で切れる (針が外と下へ向く)。
    幅 period・深さ amp は歯ごとに乱数・ときどき向きが逆 (rev)＝等間隔の「くし」にしない"""
    t = np.zeros(n + 8)
    d = int(rng.integers(0, 3))
    while d < len(t):
        P = int(rng.integers(period[0], period[1] + 1))
        A = float(rng.uniform(*amp))
        back = rng.random() < rev
        for k in range(P):
            if d + k < len(t):
                f = (k + 1) / float(P)
                t[d + k] = A * ((1.0 - f + 1.0 / P) if back else f)
        d += P
    return t


def _r3_tier(x0, y0, side, L, T, rng, lift=(0.0, 0.04), drop=(0.15, 0.32), slope=(0.08, 0.2), amp=(2.5, 4.5), period=(3, 6), start=-1,
             bump=0.12, tip=True, grow=0.0):
    """針葉の段 1 枚の列の表 [(x, 上の行, 体の下の行, 歯の先の行, u)]。(x0, y0) = 付け根 (幹の縁)・side の向きへ長さ L・付け根の厚み T。
    上の縁 = y0 + slope·d + L(−a·u + b·u²) (少し上がってから先が垂れる)・厚みは先へ細る (先で 1)・下の縁はのこぎり (_r3_teeth)。
    start < 0 = 幹の上を横切って始まる (手前の段＝幹を隠す)。bump = 上の縁から 1 ドット突き出す針の確率。tip = 先に 1〜2 ドットの垂れた針。
    grow > 0 = 厚みが元から grow 割までに 1 → T へ太り、そこから先へ細る (枝の塊の中の段＝元を他の段に溶かす)。0 = 元がいちばん厚い (幹から出る段)"""
    a = float(rng.uniform(*lift))
    b = float(rng.uniform(*drop))
    sl = float(rng.uniform(*slope)) if isinstance(slope, tuple) else float(slope)
    Le = max(1, _rnd(L))
    teeth = _r3_teeth(Le + 4, rng, amp, period)
    cols = []
    for i in range(start, Le + 1):
        d = max(0, i)
        u = min(1.0, d / max(1.0, L))
        top = y0 + sl * d + L * (-a * u + b * u * u)
        if grow > 0:
            th = 1.0 + (T - 1.0) * (min(1.0, u / grow) ** 0.7 if u < grow else (1.0 - u) / (1.0 - grow))
        else:
            th = 1.0 + (T - 1.0) * (1.0 - u) ** 1.0
        tz = teeth[d] * min(1.0, (1.0 - u) * 2.2 + 0.25) * min(1.0, (i - start + 2) / 5.0)
        yt = _rnd(top)
        if 0.1 < u < 0.92 and rng.random() < bump:
            yt -= 1
        yb = _rnd(top + th)
        cols.append((_rnd(x0 + side * i), yt, yb, max(yb, _rnd(top + th + tz)), u))
    if tip:
        x, yt, yb, yz, u = cols[-1]
        for k in range(1, int(rng.integers(1, 3)) + 1):
            yy = yb + k - 1 + int(rng.integers(0, 2))
            cols.append((x + side * k, yy, yy, yy, 1.0))
    return cols


def _r3_paint_cols(img, lay, cols, lit, rng, nz, p_blue=0.55, kind=None, pal=None, hatch=None):
    """段の列の表を塗る (光は左上)。lit = 0〜1 (左の段・上の段ほど明るい)。
    上の縁 1 行 = 明るい (#204043 が主・lit に応じて #535457 #4a5a40 #364e33 の光の粒)・2 行目 = 半分が #204043・
    中 = 影の中間 (#172d44 と #112f23 を p_blue で混ぜる) に 3 ドットの値ノイズの明るい塊 (#204043) と暗い塊 (#1c1d1c。深い所と lit の低い段ほど多い)・
    歯と下の縁 = #1c1d1c / #16151e (歯の中はときどき #172d44)。pal で色の名前を差し替える (茂みは緑)。
    hatch = (side, 間隔, 位相): 中に外と下へ向かう斜めの針の筋 (明るい筋と暗い筋の対。ノイズの明るい所だけ＝等間隔の縞にしない)"""
    P = {'hi': ('gy2', 'g5', 'g3'), 'top': 'n0', 'dim': 'b0', 'mid': ('b0', 'g0'), 'lt': 'n0', 'dk': 'k2', 'blk': 'k1'}
    if pal:
        P.update(pal)
    kind = LAY_NEEDLE if kind is None else kind
    H, W = lay.shape
    for (x, yt, yb, yz, u) in cols:
        if not 0 <= x < W:
            continue
        for y in range(max(0, yt), min(H - 1, yz) + 1):
            r = y - yt
            rv = rng.random()
            if r == 0:
                pb = 0.06 + 0.38 * lit * (0.55 + 0.45 * u)
                if rv < pb:
                    hi = P['hi']
                    c = hi[0] if rng.random() < 0.5 else hi[1] if (lit > 0.55 and rng.random() < 0.45) else hi[2]
                elif rv < pb + 0.5 + 0.35 * lit:
                    c = P['top']
                else:
                    c = P['dim']
            elif y >= yz and yz > yt + 1:
                c = P['blk'] if rv < 0.45 else P['dk']
            elif y > yb:
                c = P['dk'] if rv < 0.6 else P['mid'][0] if rv < 0.82 else P['blk']
            elif r == 1 and rv < 0.35 + 0.45 * lit:
                c = P['top']
            else:
                n = nz[y, x]
                deep = min(r, 8)
                hk = ((hatch[0] * x - y + hatch[2]) % hatch[1]) if hatch else -1
                if hk == 0 and n > 0.42 and rv < 0.8:
                    c = P['lt']
                elif hk == 1 and n > 0.42 and rv < 0.6:
                    c = P['dk']
                elif n > 0.8 - 0.1 * lit:
                    c = P['lt']
                elif n < 0.18 + 0.035 * deep + 0.12 * (1.0 - lit):
                    c = P['dk']
                else:
                    c = P['mid'][0] if rv < p_blue else P['mid'][1]
            img[y, x] = (*C3[c], 255)
            lay[y, x] = kind


def _r3_leader(img, lay, x, y0, y1, rng):
    """梢の先の芯 (1 ドット・ときどき左右に 1 ドットの針)"""
    for y in range(y0, y1 + 1):
        _set(img, lay, y, x, 'n0' if (y - y0) % 3 == 0 else 'b0', LAY_NEEDLE)
        if y > y0 + 1 and rng.random() < 0.35:
            _set(img, lay, y, x + (1 if rng.random() < 0.5 else -1), 'k2', LAY_NEEDLE)


def _r3_tier_stack(img, lay, cxf, edgef, wf, ybase, ytop, Lbase, rng, nz, levels, Tk=(1.05, 1.5), drop=(0.15, 0.4), lift=(0.0, 0.1),
                   amp=(2.0, 4.0), period=(3, 6), front=0.2, short=0.12, env_pow=0.92, lmin=1.5, lit_l=(0.62, 0.38), lit_r=(0.18, 0.3),
                   p_blue=0.55, full_bottom=True, hatch_p=(4, 6)):
    """段を下 (ybase) から上 (ytop) へ levels 段積む (左右 1 枚ずつ・半段ずらす)。段の長さ = 三角の包み Lbase·(1−h)^env_pow (h = 0 下〜1 上) ×乱数
    (ときどき短い段 short)。いちばん下の段は左右とも Lbase (外接の四角の中心＝幹の中心)。厚み = 段の間隔 × Tk。front = 幹の上を横切る段の確率。
    描く順は下の段から (上の段が上書き)。cxf(y) = 幹の中心・edgef(y, side) = 幹の縁・wf(y) = 幹の幅"""
    span = float(ybase - ytop)
    sp = span / max(1, levels)
    tiers = []
    sside = 1 if rng.random() < 0.5 else -1
    for k in range(levels):
        yk = ybase - k * sp + float(rng.uniform(-0.25, 0.25)) * sp
        h = min(1.0, max(0.0, (ybase - yk) / span))
        env = Lbase * (1.0 - h) ** env_pow + lmin
        for side in (-1, 1):
            y0 = yk + (float(rng.uniform(0.35, 0.6)) * sp if side == sside else 0.0)
            if k == 0 and full_bottom:
                L = Lbase
            elif rng.random() < short:
                L = env * float(rng.uniform(0.45, 0.65))
            else:
                L = env * float(rng.uniform(0.74, 1.0))
            T = min(sp * float(rng.uniform(*Tk)), 0.5 * L + 2.0)
            T = max(T, 2.0)
            st = -_rnd(wf(y0)) if (k > 0 and rng.random() < front) else -1
            lit = (lit_l[0] + lit_l[1] * h) if side < 0 else (lit_r[0] + lit_r[1] * h)
            tiers.append((y0, side, L, T, lit, st))
        sside = -sside
    for (y0, side, L, T, lit, st) in sorted(tiers, key=lambda t: -t[0]):
        cols = _r3_tier(edgef(y0, side), y0, side, L, T, rng, lift=lift, drop=drop, amp=amp, period=period, start=st)
        hatch = (side, int(rng.integers(hatch_p[0], hatch_p[1] + 1)), int(rng.integers(0, 8))) if hatch_p else None
        _r3_paint_cols(img, lay, cols, lit, rng, nz, p_blue=p_blue, hatch=hatch)
    return sp


# ---------------------------------------------------------------- 密な針葉樹 (近い木・帯の手前の木)
def conifer_dense_r3(W, H, seed, tw=12, levels=(16, 20), bare=(0.15, 0.25), twigs=(2, 4), env_pow=0.92):
    """本家 ref16_upperright の近い木 (段1b・D2)。細い幹 (根元 tw ドット・上へ細る) から段 (tier) が levels 段 (左右 1 枚ずつ・半段ずらす)。
    各段は幹から外へ張り出して少し垂れる針葉の層で、下の縁がのこぎり (2〜4 ドットの歯・不規則)・上の縁 1〜2 ドットだけ明るい (左の段ほど)。
    段どうしは上下に重なり (厚み = 段の間隔の 1.05〜1.5 倍)、2 割の段は幹の手前を横切る (幹は段の間から見える)。
    全体は下が広く上が尖る三角 (幅 W ≈ 高さの 0.45〜0.6)。葉は高さの bare (15〜25%) から上・下は幹と短い枯れ枝 twigs 本。
    足元 = 下辺の中央 = 幹の中心 (いちばん下の段は左右とも同じ長さ＋_r3_balance)"""
    rng = np.random.default_rng(seed)
    Wc = W + 24
    img = np.zeros((H, Wc, 4), np.uint8)
    lay = np.zeros((H, Wc), np.int8)
    cx0 = Wc / 2.0
    ybot = H - 1
    ytip = 1
    fr = 8 + tw // 2
    edges = _r3_trunk(img, lay, cx0, tw, 2.0, ybot, ytip + 3, rng, flare=1.35, flare_rows=fr, sway=0.8, dark=True)

    def clampy(y):
        return min(max(_rnd(y), ytip + 3), ybot)

    def cxf(y):
        lft, rgt = edges[clampy(y)]
        return 0.5 * (lft + rgt)

    def edgef(y, side):
        lft, rgt = edges[clampy(y)]
        return (rgt - 1.0) if side > 0 else (lft + 1.0)

    def wf(y):
        lft, rgt = edges[clampy(y)]
        return rgt - lft

    nz = 0.8 * _noise2(H, Wc, 3.0, rng) + 0.2 * rng.random((H, Wc))
    yf = ybot - float(rng.uniform(*bare)) * H
    # 下の裸の幹の枯れ枝 (短く細い・暗い)
    for _ in range(int(rng.integers(twigs[0], twigs[1] + 1))):
        y0 = ybot - float(rng.uniform(0.06, max(0.08, (ybot - yf) / H - 0.02))) * H
        side = 1 if rng.random() < 0.5 else -1
        L = float(rng.uniform(3.0, 9.0))
        _r3_bough(img, lay, _r3_path(edgef(y0, side), y0, side, L, float(rng.uniform(-0.6, 0.15)), float(rng.uniform(0.1, 0.4))), 2)
    Lbase = (W - tw) / 2.0 - 1.0
    ytop_f = ytip + 7
    nlev = int(rng.integers(levels[0], levels[1] + 1))
    sp = _r3_tier_stack(img, lay, cxf, edgef, wf, yf, ytop_f, Lbase, rng, nz, nlev, env_pow=env_pow)
    _r3_leader(img, lay, int(math.floor(cxf(ytip + 3))), ytip, ytop_f + 2, rng)
    _r3_spikes(lay, rng, p=0.05, maxlen=1)
    for y, x in zip(*np.nonzero((lay == LAY_NEEDLE) & (img[..., 3] == 0))):
        img[y, x] = (*C3['k2'], 255)
    cxb = cxf(ybot - fr - 2)
    bal = _r3_balance(lay, cxb)
    for y, x in zip(*np.nonzero((lay > 0) & (img[..., 3] == 0))):
        img[y, x] = (*C3['k2'], 255)
    img[lay == LAY_EMPTY] = 0
    meta = _r3_foot_meta(lay, cxb, bal)
    meta.update(levels=nlev, spacing=round(sp, 1), foliageFrom=round((ybot - yf) / H, 3))
    return _crop_alpha(img), meta


# ---------------------------------------------------------------- 帯の木・奥の木の弓なりの枝 (先に垂れた房)
def _r3_arm(img, lay, x0, y0, side, L, rng, nz, t0=2, lit=0.5, rise=(0.25, 0.5), droop=(0.75, 1.2), bare=(0.08, 0.2), T=(1.5, 2.5)):
    """弓なりの枝 1 本 (本家 ref16_fogtrunk_x2・段1b D2): 付け根から外へ上がって先が下へ曲がる弓の上に、細い針葉の層が乗って先で垂れる
    (帯の木の「弓なりの枝が多く細く重なる」)。付け根の bare 割は樹皮だけ (t0 → 細る)。層の厚みは T + 0.12L で、中ほどが厚く先へ細り、
    下の縁はのこぎり (1.5〜3.5 ドットの歯)・上の縁は少し明るい。先は 1〜3 列だけ下へ垂れて尖る (房)"""
    a = float(rng.uniform(*rise))
    b = float(rng.uniform(*droop))
    Li = max(2, _rnd(L))
    yc = lambda i: y0 + L * (-a * (i / L) + b * (i / L) ** 2)
    i0 = _rnd(float(rng.uniform(*bare)) * L)
    _r3_bough(img, lay, [(x0 + side * i, yc(i)) for i in range(0, i0 + 3)], t0)
    th0 = float(rng.uniform(*T)) + 0.09 * L
    ext = int(rng.integers(1, 4))
    teeth = _r3_teeth(Li + ext + 2, rng, amp=(2.0, 4.0), period=(2, 5))
    cols = []
    for i in range(i0, Li + ext + 1):
        g = (i - i0) / max(1.0, Li - i0)
        if i <= Li:
            x, y = x0 + side * i, yc(i)
        else:
            x, y = x0 + side * i, yc(Li) + 1.4 * (i - Li)
        gg = min(1.0, g)
        prof = math.sin(math.pi * min(0.999, 0.12 + 0.88 * gg) * 0.9) ** 0.6     # 中ほどが厚い・先へ細る (根元側は少し薄い)
        th = 1.0 + (th0 - 1.0) * prof * (1.0 if i <= Li else max(0.3, 1.0 - 0.35 * (i - Li)))
        top = y - (1.0 if 0.1 < gg < 0.85 and rng.random() < 0.5 else 0.0)
        tz = teeth[i - i0] * min(1.0, (i - i0 + 1) / 3.0) * (0.5 + 0.5 * prof)
        yt, yb = _rnd(top), _rnd(y + th)
        cols.append((_rnd(x), yt, yb, max(yb, _rnd(y + th + tz)), 0.3 + 0.7 * gg))
    hatch = (side, int(rng.integers(4, 7)), int(rng.integers(0, 8)))
    _r3_paint_cols(img, lay, cols, lit, rng, nz, hatch=hatch)


# ---------------------------------------------------------------- 針葉樹 (帯の木・奥の木。幹＋枝＋房が 1 枚)
def conifer_r3(w, H, seed, nb=(9, 14), low=0.28, high=0.8):
    """霧の帯と奥の針葉樹 (R1。段1b・D2 で描き直し)。幹の幅 w (根元の張り出しの上)・高さ H。
    幹は暗い (dark＝光の側を約 25% 暗く・左の縁の明るい筋 2〜3 ドットと明暗差 40 以上は守る＝霧の中で灰色の柱にしない)。
    弓なりの枝 nb 本 (9〜14・下から low〜high・左右おおむね交互): 長さは幹の幅の 1.3 倍 (下)〜0.6 倍 (上)・付け根から上がって先が垂れ、
    外の 5〜6 割に細い房 (先ほど深い・下の縁はぎざぎざ)。梢 (上の 1−high) は細かい段 (_r3_tier_stack) の密な尖り。
    下の裸の幹に短い枝の残り 1〜3。足元 = 幹の中心 (_r3_balance)"""
    rng = np.random.default_rng(seed)
    W = int(math.ceil(w * 4.6)) + 16
    img = np.zeros((H, W, 4), np.uint8)
    lay = np.zeros((H, W), np.int8)
    cx0 = W / 2.0
    ybot = H - 1
    ytip = 2
    ycrown = _rnd(((1.0 - high) * H))                  # 梢の下の端 (上からの行)
    fr = int(10 + w * 0.12)
    edges = _r3_trunk(img, lay, cx0, w, 2.0, ybot, ytip + 4, rng, flare=1.38, flare_rows=fr, crown=high, dark=True)

    def clampy(y):
        return min(max(_rnd(y), ytip + 4), ybot)

    def cxf(y):
        lft, rgt = edges[clampy(y)]
        return 0.5 * (lft + rgt)

    def edgef(y, side):
        lft, rgt = edges[clampy(y)]
        return (rgt - 1.0) if side > 0 else (lft + 1.0)

    def wf(y):
        lft, rgt = edges[clampy(y)]
        return rgt - lft

    nz = 0.65 * _noise2(H, W, 3.0, rng) + 0.35 * rng.random((H, W))
    t0 = 2 if w < 30 else 3
    # 下の裸の幹の枝の残り
    for _ in range(int(rng.integers(1, 4))):
        y0 = ybot - float(rng.uniform(0.08, max(0.1, low - 0.05))) * H
        side = 1 if rng.random() < 0.5 else -1
        L = float(rng.uniform(3.0, 6.0)) * (w / 20.0) ** 0.5
        _r3_bough(img, lay, _r3_path(edgef(y0, side), y0, side, L, float(rng.uniform(-0.35, 0.05)), 0.15), 2)
    # 弓なりの枝 (下から上へ。上の枝が下の枝の房に重なる)
    n = int(rng.integers(nb[0], nb[1] + 1))
    ts = np.linspace(0.0, 1.0, n) + rng.uniform(-0.3, 0.3, n) / max(1, n - 1)
    ts = np.clip(np.sort(ts), 0.0, 1.0) ** 0.8              # 上ほど詰む (梢の下で枝が重なる)
    side = 1 if rng.random() < 0.5 else -1
    arms = []
    for i, t in enumerate(ts):
        if i > 0:
            side = side if rng.random() < 0.2 else -side
        y0 = ybot - (low + (high - low) * t) * H
        L = w * min(1.3, max(0.6, (1.3 - 0.7 * t) * float(rng.uniform(0.85, 1.05))))
        lit = (0.55 + 0.35 * t) if side < 0 else (0.2 + 0.25 * t)
        arms.append((y0, side, L, lit))
    # いちばん下の左右は同じ長さ (外接の四角の中心＝幹の中心)
    for s in (-1, 1):
        idx = [k for k, a in enumerate(arms) if a[1] == s]
        if idx:
            y0, sd, L, lit = arms[idx[0]]
            arms[idx[0]] = (y0, sd, 1.3 * w, lit)
    for (y0, sd, L, lit) in arms:
        _r3_arm(img, lay, edgef(y0, sd), y0, sd, L, rng, nz, t0=t0, lit=lit)
    # 梢: 細かい段の密な尖り (段の間隔 4〜6・幅は梢の下で幹の幅の 0.62 倍〜先で 1)
    span = ycrown - (ytip + 4)
    nlev = max(5, int(round(span / float(rng.uniform(4.0, 6.0)))))
    _r3_tier_stack(img, lay, cxf, edgef, wf, ycrown + 3, ytip + 4, 0.62 * w, rng, nz, nlev, Tk=(1.15, 1.6), drop=(0.2, 0.45),
                   amp=(1.5, 3.0), period=(2, 4), front=0.35, short=0.15, env_pow=1.0, lmin=1.0, lit_l=(0.6, 0.3), lit_r=(0.2, 0.25),
                   full_bottom=False)
    _r3_leader(img, lay, int(math.floor(cxf(ytip + 4))), 0, ytip + 6, rng)
    _r3_spikes(lay, rng, p=0.06, maxlen=1)
    cxb = cxf(ybot - fr - 2)
    bal = _r3_balance(lay, cxb)
    for y, x in zip(*np.nonzero((lay > 0) & (img[..., 3] == 0))):
        img[y, x] = (*C3['k2'], 255)
    img[lay == LAY_EMPTY] = 0
    meta = _r3_foot_meta(lay, cxb, bal)
    meta.update(arms=n, crownLevels=nlev)
    return _crop_alpha(img), meta


def conifer_near_r3(w, H, seed, nstub=(2, 4)):
    """近い幹 (R2・画面の上を突き抜ける)。幅 w・高さ H。上へはほとんど細らない (画面に入るのは下の 4 割ほど)。
    樹皮の筋 (山と溝 2〜3 ドット・明暗差 35〜55)・左の縁の明るい筋・枝の付け根の残り nstub・房つきの枝 2 本
    (左右に 1 本ずつ・根元から 22〜42% の高さ＝画面の上の方に入る所。1 本だと足元＝外接の四角の中心が幹から大きくずれる)"""
    rng = np.random.default_rng(seed)
    W = int(math.ceil(w * 3.8)) + 16
    img = np.zeros((H, W, 4), np.uint8)
    lay = np.zeros((H, W), np.int8)
    cx0 = W / 2.0
    ybot = H - 1
    fr = int(12 + w * 0.15)
    edges = _r3_trunk(img, lay, cx0, w, w * 0.8, ybot, 0, rng, flare=1.3, flare_rows=fr, sway=1.0, near=True)

    def cxf(y):
        y = min(max(_rnd((y)), 0), ybot)
        return 0.5 * (edges[y][0] + edges[y][1])

    def edge(y, side):
        lft, rgt = edges[min(max(_rnd((y)), 0), ybot)]
        return (rgt - 1.0) if side > 0 else (lft + 1.0)

    maxreach = 1.9 * w
    for _ in range(int(rng.integers(nstub[0], nstub[1] + 1))):
        y0 = ybot - float(rng.uniform(0.06, 0.5)) * H
        side = 1 if rng.random() < 0.5 else -1
        L = float(rng.uniform(3.0, 7.0))
        _r3_bough(img, lay, _r3_path(edge(y0, side), y0, side, L, float(rng.uniform(-0.45, 0.0)), 0.1), 3)
    side = 1 if rng.random() < 0.5 else -1
    ya = ybot - float(rng.uniform(0.22, 0.3)) * H
    yb_ = ybot - float(rng.uniform(0.34, 0.42)) * H
    for y0 in (ya, yb_):
        L = w * float(rng.uniform(1.05, 1.3))
        _r3_swag(img, lay, edge(y0, side), y0, side, L, 0.3 * w + 4.0, rng, t0=4, s0=(0.05, 0.25), droop=(0.45, 0.7),
                 bare=(0.35, 0.5), lobe=(5.0, 9.0), cxc=cxf(y0), maxx=maxreach)
        side = -side
    _r3_spikes(lay, rng)
    cxb = cxf(ybot - fr - 2)
    bal = _r3_balance(lay, cxb)
    _r3_color_needles(img, lay, rng)
    img[lay == LAY_EMPTY] = 0
    return _crop_alpha(img), _r3_foot_meta(lay, cxb, bal)


# ---------------------------------------------------------------- 垂れる枝 (上の覆い)
def bough_hang_r3(seed, W=160, H=80, nspray=(3, 6)):
    """近い木の上端から画面の上を横切る垂れた枝 (R2・段1b D2 で描き直し)。付け根は絵の左の辺 (太さ 6・上から 8〜14 行)。
    1 本の弓に房の縁取りを付けると椰子の葉に見えた (試しの撮影 T3-ogre-hideui の左上) ので、本家の近い木の枝 (ref16_upperright) のように
    「段が 3〜6 枚重なった針葉の枝の塊」にする: 段 (_r3_tier・元は細く 35〜55% で最も厚い・厚み 8〜14) を棚のように縦に積む
    (段 k の元は付け根から右へ 6〜12・下へ 8.5〜11.5 ずつ。どの段も右の端の近くまで伸びて先が少し垂れる)。
    塊は楔形 (付け根で細く・右へ行くほど厚い)。段どうしの隙間に下の段の明るい上の縁がのぞく。各段の下の縁はのこぎり・
    上の縁は少し明るい (上の段ほど)。芯の線は付け根の樹皮 (左 24 ドット・すぐ段に隠れる) だけ (中央の芯・左右対称の葉の筋を作らない)。
    反対側 (右の木から左へ) は設計図の flip で使う"""
    rng = np.random.default_rng(seed)
    y0 = float(rng.uniform(8, 14))
    n = int(rng.integers(nspray[0], nspray[1] + 1))
    # 段を棚のように縦に積む (本家の近い木の段＝ref16_upperright の左上): 段 k の元は付け根から右下へ (dx, dy) ずつ。
    # 下の段ほど右から始まり、どの段も右の端の近くまで伸びて先が少し垂れる＝塊は付け根で細く右へ行くほど厚い (楔形)。
    # 段どうしの縦の間隔 ≈ 段の厚み＋歯 (隙間に下の段の明るい上の縁がのぞく)
    dy = min(float(rng.uniform(8.5, 11.5)), (H - 30.0 - y0) / max(1, n - 1)) if n > 1 else 0.0
    dx = float(rng.uniform(6.0, 12.0))
    sprays = []
    ys = y0
    xs = 1.0
    for k in range(n):
        f = k / max(1.0, n - 1.0)
        if k > 0:
            xs += dx * float(rng.uniform(0.6, 1.4))
            ys += dy * float(rng.uniform(0.75, 1.2))
        L = (W - 3.0 - xs) * (float(rng.uniform(0.5, 0.8)) if k == 0 else float(rng.uniform(0.6, 0.97)))
        T = float(rng.uniform(7.5, 10.5)) + 0.025 * L
        sl = float(rng.uniform(0.0, 0.05)) + 0.025 * k
        L = min(L, (H - 4.0 - ys - T - 2.0) / max(0.05, sl + 0.17))   # 下へはみ出さない (傾き＋先の垂れ)
        lit = 0.85 - 0.55 * f
        sprays.append((xs, ys, sl, L, T, lit))
    img = np.zeros((H, W, 4), np.uint8)
    lay = np.zeros((H, W), np.int8)
    nz = 0.8 * _noise2(H, W, 3.0, rng) + 0.2 * rng.random((H, W))
    _r3_bough(img, lay, [(float(x), y0 + 0.08 * x) for x in range(0, 24)], 6)
    for (xs, ys, sl, L, T, lit) in reversed(sprays):           # 下の段から (上の段が上書き)
        cols = _r3_tier(xs, ys, 1, L, T, rng, lift=(0.02, 0.08), drop=(0.1, 0.2), slope=(sl * 0.9, sl * 1.1),
                        amp=(2.5, 4.5), period=(3, 6), start=-2, bump=0.16, grow=float(rng.uniform(0.35, 0.55)))
        hatch = (1, int(rng.integers(4, 7)), int(rng.integers(0, 8)))
        _r3_paint_cols(img, lay, cols, lit, rng, nz, p_blue=0.5, hatch=hatch)
    _r3_spikes(lay, rng, p=0.08, maxlen=2)
    for y, x in zip(*np.nonzero((lay > 0) & (img[..., 3] == 0))):
        img[y, x] = (*C3['k2'], 255)
    img[lay == LAY_EMPTY] = 0
    return img, {'attach': (0, _rnd((y0))), 'sprays': n}


# ---------------------------------------------------------------- 茂みの塊 (土手の段を隠す)
def bush_clump_r3(seed, W, H, nmound=(4, 7), ngrass=(3, 6), hfrac=(0.66, 0.74)):
    """土手の縦の面と面取りを隠す低木の塊 (R4・段1b D2 で描き直し)。丸い房のドームの並びは「青く丸い生け垣・植木鉢の列」に見えた
    (試しの撮影の座席の後ろ) ので、本家の土手 (ref16_leftbank・ref16_fogtrunk の足元の低木) のように低く横に長い不規則な低木の塊にする:
    高さは絵の hfrac (66〜74%＋上へ突き出す小枝と葉で今の 70〜80%。上は透明＝足元は絵の下辺)・左右非対称の低い山 nmound 個の重なり＋低い裾 (横に長い)・
    上の輪郭は小枝と針葉の先が 2〜8 ドット突き出してぎざぎざ・ところどころ 1〜3 ドットの隙間で切れる・
    中の明暗は低く暗い (緑の影 #193d20・#112f23・#172d44 が主＝今より約 20% 暗い)・上の縁だけ少し明るい (左ほど)・
    山の重なりの境に 1 段明るい縁 (塊が読める)・底は暗い。草の葉 ngrass 本 (細く・少し明るい緑) が混じって上へ抜ける"""
    rng = np.random.default_rng(seed)
    Hs = float(rng.uniform(*hfrac)) * H
    base = H - 1
    xl = float(rng.uniform(1.0, 3.5))
    xr = W - 1.0 - float(rng.uniform(1.0, 3.5))
    span = xr - xl
    xs = np.arange(W) + 0.5
    prof = np.zeros(W)
    owner = np.full(W, -1)
    n = int(rng.integers(nmound[0], nmound[1] + 1))
    hk_all = np.sort(rng.uniform(0.45, 0.9, n))[::-1]
    hk_all[0] = 1.0
    rng.shuffle(hk_all)
    mounds = []
    peak = float(rng.uniform(0.3, 0.7))                   # いちばん高い所 (左右非対称)
    for k in range(n):
        c = xl + span * (0.06 + 0.88 * (k + float(rng.uniform(0.15, 0.85))) / n)
        wl = span * float(rng.uniform(0.09, 0.2))          # 左右の幅を別に (非対称)
        wr = span * float(rng.uniform(0.09, 0.2))
        e = (c - xl) / span
        env = 0.5 + 0.5 * max(0.0, 1.0 - abs(e - peak) / max(peak, 1.0 - peak)) ** 0.8
        hk = Hs * float(hk_all[k]) * env
        dxm = xs - c
        q = np.where(dxm < 0, dxm / wl, dxm / wr)
        h = hk * np.clip(1.0 - np.abs(q) ** 2.0, 0.0, 1.0) ** 0.7
        better = h > prof
        owner[better] = k
        prof = np.maximum(prof, h)
        mounds.append((c, wl, wr, hk))
    prof *= Hs / max(1e-3, prof.max())                  # いちばん高い山 = Hs
    # 低い裾 (横に長く・端は 2〜4 ドットで地面へ)
    skirt = Hs * float(rng.uniform(0.22, 0.32))
    ramp = float(rng.uniform(2.0, 4.0))
    sk = skirt * np.clip(np.minimum(xs - xl, xr - xs) / ramp, 0.0, 1.0) ** 0.7
    sk *= (xs > xl) & (xs < xr)
    owner[sk > prof] = -1
    prof = np.maximum(prof, sk)
    # ぎざぎざ: ゆっくりの揺れ＋列ごとの ±1
    prof += (_knots(W, 5.0, rng) - 0.5) * 2.4 + (_knots(W, 2.0, rng) - 0.5) * 2.0 + rng.integers(-1, 2, W) * 0.8
    prof = np.where((xs > xl) & (xs < xr), np.maximum(prof, 1.0), 0.0)
    # 隙間で切れる (1〜3 か所の V 字の切れ込み・幅 3〜6・深さはその所の高さの 25〜45%)
    for _ in range(int(rng.integers(1, 4))):
        x0 = float(rng.uniform(xl + 0.15 * span, xr - 0.15 * span))
        hw = float(rng.uniform(1.5, 3.0))
        dep = float(rng.uniform(0.25, 0.45))
        v = np.clip(1.0 - np.abs(xs - x0) / hw, 0.0, 1.0)
        prof *= (1.0 - dep * v)
    lay = np.zeros((H, W), np.int8)
    for x in range(W):
        if prof[x] >= 1.0:
            lay[max(0, _rnd(base - prof[x] + 1)):base + 1, x] = LAY_NEEDLE
    occ0 = lay > 0
    topy = np.full(W, H)
    for x in range(W):
        ys_ = np.nonzero(occ0[:, x])[0]
        if len(ys_):
            topy[x] = ys_[0]
    img = np.zeros((H, W, 4), np.uint8)
    # 小枝 (上の縁から外と上へ 2〜8 ドット・1 ドット幅・先に 2〜3 ドットの針の塊)
    twig_px = []
    for _ in range(int(rng.integers(3, 4)) + W // 24):
        x = int(rng.uniform(xl + 2, xr - 2))
        if topy[x] >= H:
            continue
        y = topy[x] + 1
        Lt = float(rng.uniform(2.0, 8.0)) * (0.6 if H < 40 else 1.0)
        ang = float(rng.uniform(-0.9, 0.9)) + (0.35 if x > W / 2 else -0.35)
        pts = []
        for i in range(1, _rnd(Lt) + 1):
            pts.append((_rnd(x + ang * i), y - i))
        for (px, py) in pts:
            if 0 <= px < W and 0 <= py < H:
                twig_px.append((py, px, 'k2' if rng.random() < 0.6 else 'w0'))
        if pts and rng.random() < 0.6:
            px, py = pts[-1]
            for (ddx, ddy) in ((0, -1), (1 if ang > 0 else -1, 0), (1 if ang > 0 else -1, -1)):
                if rng.random() < 0.7 and 0 <= px + ddx < W and 0 <= py + ddy < H:
                    twig_px.append((py + ddy, px + ddx, 'g1'))
    # 針の先 (上の縁から 1〜2 ドット・確率と向きは乱数)
    for x in range(W):
        if topy[x] < H and rng.random() < 0.3:
            dx_ = int(rng.integers(-1, 2))
            for i in range(1, int(rng.integers(1, 3)) + 1):
                yy, xx = topy[x] - i, x + (dx_ if i > 1 else 0)
                if 0 <= yy < H and 0 <= xx < W:
                    lay[yy, xx] = LAY_NEEDLE
    occ = lay > 0
    upE = np.ones_like(occ)
    upE[1:] = ~occ[:-1]
    up2 = np.ones_like(occ)
    up2[2:] = ~occ[:-2]
    nz = 0.75 * _noise2(H, W, 2.5, rng) + 0.25 * rng.random((H, W))
    nb = _noise2(H, W, 4.0, rng)
    r = rng.random((H, W))
    for y, x in zip(*np.nonzero(occ)):
        b = (base - y) / max(1.0, Hs)                       # 地面からの高さの割合 (茂みの高さに対して)
        rv = r[y, x]
        litx = 1.0 - x / float(W)                          # 左ほど明るい (光は左上)
        dark = min(1.0, max(0.0, (0.35 - b) / 0.35)) ** 1.2
        o = owner[x]
        if upE[y, x]:
            c = 'g4' if rv < 0.12 + 0.2 * litx else 'g3' if rv < 0.5 + 0.2 * litx else 'g2' if rv < 0.85 else 'n0'
        elif up2[y, x]:
            c = 'g2' if rv < 0.45 + 0.2 * litx else 'g1'
        elif y >= base - 1:
            c = 'k2' if rv < 0.5 else 'g0' if rv < 0.85 else 'k1'
        elif nb[y, x] < dark * 1.1:
            c = 'g0' if (rv < 0.45 and dark > 0.5) else 'k2' if dark > 0.75 and rv < 0.7 else 'g1'
        elif o >= 0 and x > 0 and owner[x - 1] >= 0 and owner[x - 1] != o and rv < 0.6:
            c = 'g2'                                          # 山の重なりの境
        elif nz[y, x] > 0.76:
            c = 'g2' if rv < 0.7 else 'n0'
        elif nz[y, x] < 0.3:
            c = 'g0' if (rv < 0.75 or nz[y, x] > 0.2) else 'b0'
        else:
            c = 'g1' if rv < 0.7 else 'g0'
        img[y, x] = (*C3[c], 255)
    # 山の上の縁 (手前の山の上の輪郭が、奥の山の中に 1 段明るい縁として見える)
    for k, (c, wl, wr, hk) in enumerate(mounds):
        for x in range(max(0, int(c - wl)), min(W, int(c + wr) + 1)):
            dxm = x + 0.5 - c
            q = dxm / wl if dxm < 0 else dxm / wr
            h = hk * max(0.0, 1.0 - q * q) ** 0.55
            y = _rnd(base - h + 1)
            if 0 <= y < H and occ[y, x] and y > topy[x] + 1 and rng.random() < 0.55:
                img[y, x] = (*C3['g3' if rng.random() < 0.5 else 'g2'], 255)
    for (py, px, c) in twig_px:
        img[py, px] = (*C3[c], 255)
        lay[py, px] = LAY_BARK
    # 草の葉 (細く・少し明るい緑・根元 2 ドット・上へ抜けて先が曲がる)
    for _ in range(int(rng.integers(ngrass[0], ngrass[1] + 1))):
        bx = float(rng.uniform(xl + 0.08 * span, xr - 0.08 * span))
        by = base - float(rng.uniform(0.0, 0.3)) * Hs
        Lg = min(float(rng.uniform(0.5, 0.9)) * Hs + 2.0, (by - (base - 1.08 * Hs)) / 0.75)   # 先は茂みの高さの 1.08 倍まで
        lean = float(rng.uniform(-0.45, 0.45))
        bend = float(rng.uniform(-0.15, 0.15))
        steps = _rnd(Lg)
        prev_py = H
        for i in range(steps + 1):
            t = i / max(1.0, steps)
            px = _rnd(bx + lean * Lg * t + bend * Lg * t * t)
            py = _rnd(by - Lg * t * (1.0 - 0.25 * t * t))
            if i > 0 and py >= prev_py and t > 0.5:
                break
            prev_py = py
            for ww in ((0, 1) if t < 0.35 else (0,)):
                xx = px + ww
                if 0 <= py < H and 0 <= xx < W:
                    lit_side = (ww == 0 and lean < 0) or (ww == 1 and lean >= 0)
                    img[py, xx] = (*C3['g3' if (t > 0.5 and rng.random() < 0.5) or lit_side else 'g2'], 255)
                    lay[py, xx] = LAY_NEEDLE
    img[lay == LAY_EMPTY] = 0
    return img, {'height': int(H - np.nonzero((lay > 0).any(1))[0][0])}


# ---------------------------------------------------------------- 手前の草の帯 (額縁の影絵)
def _r3_stamp(lay, x, y, rad):
    H, W = lay.shape
    r = max(0.5, rad)
    for yy in range(int(math.floor(y - r)), int(math.ceil(y + r)) + 1):
        for xx in range(int(math.floor(x - r)), int(math.ceil(x + r)) + 1):
            if (xx + 0.5 - x) ** 2 + (yy + 0.5 - y) ** 2 <= r * r and 0 <= yy < H and 0 <= xx < W:
                lay[yy, xx] = LAY_NEEDLE


def _r3_curve(lay, p0, p1, p2, t0, t1=1.2, tipdroop=0.0):
    """2 次ベジェの葉。太さは根元 t0 → 先 t1 (先の方まで太さを保つ＝ぼけても形が読める)。先 2 割は tipdroop だけ下へ垂れる"""
    L = math.hypot(p2[0] - p0[0], p2[1] - p0[1]) + math.hypot(p1[0] - p0[0], p1[1] - p0[1])
    n = max(8, int(L * 2.5))
    pts = []
    for i in range(n + 1):
        t = i / float(n)
        x = (1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t * t * p2[0]
        y = (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t * t * p2[1]
        if t > 0.8:
            y += tipdroop * ((t - 0.8) / 0.2) ** 2
        _r3_stamp(lay, x, y, 0.5 * (t1 + (t0 - t1) * (1.0 - t) ** 0.6))
        pts.append((x, y))
    return pts


def _r3_frond(lay, p0, p1, p2, wmax, period, rng):
    """羊歯の葉 (影絵): 弓なりの軸に沿って、両側に前へ流れる小葉の塊 (間隔 period ドット・幅は軸の真ん中で wmax)。
    小葉は軸に沿った三角波で太さが膨らむ形＝等間隔の細い針 (くし) でなく、太い葉の縁の切れ込み"""
    L = math.hypot(p2[0] - p0[0], p2[1] - p0[1]) + math.hypot(p1[0] - p0[0], p1[1] - p0[1])
    n = max(16, int(L * 3))
    pts = []
    for i in range(n + 1):
        t = i / float(n)
        pts.append(((1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t * t * p2[0],
                    (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t * t * p2[1]))
    s = 0.0
    ph = float(rng.uniform(0, period))
    for i in range(len(pts)):
        if i > 0:
            s += math.hypot(pts[i][0] - pts[i - 1][0], pts[i][1] - pts[i - 1][1])
        q = pts[min(len(pts) - 1, i + 1)]
        pp = pts[max(0, i - 1)]
        tx, ty = q[0] - pp[0], q[1] - pp[1]
        tl = math.hypot(tx, ty) or 1.0
        tx, ty = tx / tl, ty / tl
        nx, ny = -ty, tx
        u = s / max(1.0, L * 0.9)
        prof = math.sin(math.pi * min(1.0, u * 0.95 + 0.05)) ** 0.7
        saw = 1.0 - abs(((s + ph) % period) / period * 2.0 - 1.0)       # 0 (切れ込み) 〜 1 (小葉の先)
        hw = wmax * prof * (0.15 + 0.85 * saw)
        _r3_stamp(lay, pts[i][0], pts[i][1], 1.3)
        o = -hw
        while o <= hw:
            fx = pts[i][0] + nx * o + tx * abs(o) * 0.35
            fy = pts[i][1] + ny * o + ty * abs(o) * 0.35
            xi, yi = int(math.floor(fx)), int(math.floor(fy))
            if 0 <= yi < lay.shape[0] and 0 <= xi < lay.shape[1]:
                lay[yi, xi] = LAY_NEEDLE
            o += 0.5
    return pts


def _r3_tips(n, lo, hi, mind, rng, tries=400):
    """葉先の x を n 個 (間隔 mind 以上・範囲 lo〜hi)。足りなければ取れた分だけ"""
    out = []
    for _ in range(tries):
        if len(out) >= n:
            break
        x = float(rng.uniform(lo, hi))
        if all(abs(x - o) >= mind for o in out):
            out.append(x)
    return sorted(out)


def fore_grass_r3(seed, W, H, nblade=0, nfrond=0, stalk=False, mind=11.0):
    """手前の額縁の草 (R6)。元の形: act1_tallgrass1 (根元から扇に開く細長い葉・穂の茎) と act1_fern (弓なりの複葉)。
    ぼけても形が読めるように葉を太く (根元 6・中ほど 4・先 1.2 ドット)・葉先の間隔を mind ドット以上 (額縁の倍率で画面 40px 以上)・
    羊歯は小葉の間隔 10.5〜12 ドットの太い切れ込みの葉 (額縁のいちばん小さい倍率 深さ 14・scale 0.5 で 1 ドット ≈4px → 42px 以上)。不透明で暗い影絵 (#112f23・#193d20・#1c1d1c、光の側の縁だけ #2d482f)"""
    rng = np.random.default_rng(seed)
    lay = np.zeros((H, W), np.int8)
    cx = W / 2.0
    base = H - 1
    tips = _r3_tips(nblade + nfrond + (1 if stalk else 0), W * 0.04, W * 0.96, mind, rng)
    order = list(range(len(tips)))
    rng.shuffle(order)
    tips = [tips[i] for i in order]
    k = 0
    for i in range(nfrond):                      # 羊歯は奥 (先に描く)
        if k >= len(tips):
            break
        xt = tips[k]
        k += 1
        bx = cx + float(rng.normal(0, W * 0.04))
        ht = float(rng.uniform(0.7, 0.98)) * H
        p1 = (bx + (xt - bx) * 0.35, base - ht)
        tip = (xt, base - ht * float(rng.uniform(0.45, 0.7)))
        _r3_frond(lay, (bx, base + 1), p1, tip, float(rng.uniform(4.5, 5.5)), float(rng.uniform(10.5, 12.0)), rng)
    for i in range(nblade):
        if k >= len(tips):
            break
        xt = tips[k]
        k += 1
        bx = cx + float(rng.normal(0, W * 0.05))
        ht = float(rng.uniform(0.55, 0.98)) * H
        yt = base - ht
        lean = (xt - bx)
        p1 = (bx + lean * float(rng.uniform(0.15, 0.4)), base - ht * float(rng.uniform(0.6, 0.85)))
        droop = float(rng.uniform(0.0, 0.18)) * ht if abs(lean) > W * 0.2 else 0.0
        _r3_curve(lay, (bx, base + 1), p1, (xt, yt), float(rng.uniform(5.5, 6.5)), 1.2, droop)
    if stalk and k < len(tips):
        xt = tips[k]
        k += 1
        bx = cx + float(rng.normal(0, W * 0.04))
        yt = base - float(rng.uniform(0.85, 0.98)) * H + 8
        pts = _r3_curve(lay, (bx, base + 1), (bx + (xt - bx) * 0.3, base - (base - yt) * 0.7), (xt, yt), 3.0, 2.2)
        hx_, hy_ = pts[-1]
        for j in range(12):   # 穂 (縦長の塊 5×12)
            _r3_stamp(lay, hx_, hy_ - j * 0.9, 2.5 if 2 <= j <= 8 else 1.6)
    occ = lay > 0
    img = np.zeros((H, W, 4), np.uint8)
    upE = np.ones_like(occ)
    upE[1:] = ~occ[:-1]
    lfE = np.ones_like(occ)
    lfE[:, 1:] = ~occ[:, :-1]
    rtE = np.ones_like(occ)
    rtE[:, :-1] = ~occ[:, 1:]
    nz = _noise2(H, W, 4.0, rng)
    r = rng.random((H, W))
    for y, x in zip(*np.nonzero(occ)):
        rv = r[y, x]
        if (upE[y, x] or lfE[y, x]) and rv < 0.7:
            c = 'g2'
        elif rtE[y, x]:
            c = 'k2'
        elif nz[y, x] < 0.35:
            c = 'k2' if rv < 0.6 else 'g0'
        else:
            c = 'g1' if rv < 0.45 else 'g0'
        img[y, x] = (*C3[c], 255)
    return _crop_alpha(img), {}


# ---------------------------------------------------------------- 座席の土 (三周目)
R3_SEAT_AMP = 0.06     # 明るさのノイズの振れ (±6%)


def earth_tile_r3(seed, nseed, n=64, amp=R3_SEAT_AMP):
    """二周目の座席の土 earth_tile_r2(seed) に 1〜2 テクセルの細かいノイズ (明るさ ±amp) を足す (R4)。
    見下ろし 5° で縦が 1/8 に潰れても「土の粒」に読ませる (本家の地面は細かいノイズで受けている)。向きのないノイズ (rot4 で回るので)。
    色表の外の色を作らないため、ノイズは「土の明暗の段 EARTH の隣の段へ確率で移る」形 (確率 = 目標の明るさまでの距離÷段の差＝平均は目標どおり)。
    明るい点 (N18) を増やさないよう、上へ移るのは EARTH[4] (#706259) まで・移った先が 7×7 の中央値より 16 以上明るくなる所では上へ移さない。
    ノイズは 1 テクセルの白色と 2×2 の塊の和＝64 で巻き戻る (継ぎ目なし)"""
    img = earth_tile_r2(seed, n)
    rng = np.random.default_rng(nseed)
    w1 = rng.uniform(-1.0, 1.0, (n, n))
    w2 = rng.uniform(-1.0, 1.0, (n // 2, n // 2)).repeat(2, 0).repeat(2, 1)
    w2 = np.roll(w2, (int(rng.integers(0, 2)), int(rng.integers(0, 2))), (0, 1))
    nz = 0.55 * w1 + 0.45 * w2
    nz = nz / np.abs(nz).max() * amp
    lums = [lum(c) for c in EARTH]
    idx = {c: i for i, c in enumerate(EARTH)}
    r = rng.random((n, n))
    out = img.copy()
    # 7×7 の中央値 (巻き戻し)。上の段へ移った画素がまわりより 16 以上明るくなる所では上へ移さない (明るい点 N18 を増やさない)
    from numpy.lib.stride_tricks import sliding_window_view
    lm = 0.2126 * img[..., 0] + 0.7152 * img[..., 1] + 0.0722 * img[..., 2]
    med = np.median(sliding_window_view(np.pad(lm, 3, mode='wrap'), (7, 7)), axis=(-1, -2))
    for y in range(n):
        for x in range(n):
            i = idx.get(tuple(int(v) for v in img[y, x]))
            if i is None:
                continue
            L = lums[i]
            tgt = L * (1.0 + nz[y, x])
            if tgt > L and i <= 3 and lums[i + 1] - med[y, x] < 16:
                j = i + 1
                p = (tgt - L) / (lums[j] - L)
            elif tgt < L and i >= 1:
                j = i - 1
                p = (L - tgt) / (L - lums[j])
            else:
                continue
            if r[y, x] < p:
                out[y, x] = EARTH[j]
    return out


# ---------------------------------------------------------------- 並べ方 (三周目)
# (名前, 作る関数の引数) — 種は固定 (同じ入力なら同じ絵)。名前は取り決め (計画の付録「レーン間の取り決め」1) のとおり
R3_CONIFERS = [(20, 1, 301, (9, 12)), (20, 2, 302, (10, 13)), (32, 1, 311, (10, 13)), (32, 2, 312, (11, 14)), (48, 1, 321, (11, 14)), (48, 2, 322, (12, 14))]
# 密な針葉樹 (段1b D2): (番号, 幅, 高さ, 種, 幹の幅, 段の数, 三角の包みの指数＝小さいほど上まで太い)
R3_DENSE = [(1, 150, 300, 381, 12, (16, 20), 0.92), (2, 150, 300, 382, 11, (18, 22), 0.8),
            (3, 200, 340, 383, 15, (14, 18), 1.05), (4, 200, 340, 384, 16, (20, 24), 0.9)]
R3_NEAR = [(1, 36, 331), (2, 42, 332)]
R3_HANG = [(1, 341), (2, 342), (3, 343), (4, 344)]
R3_BUSH = [('l', 1, 96, 64, 351, (5, 7)), ('l', 2, 96, 64, 352, (6, 8)), ('m', 1, 64, 48, 353, (4, 6)), ('m', 2, 64, 48, 354, (5, 6)),
           ('s', 1, 48, 32, 355, (3, 5)), ('s', 2, 48, 32, 356, (4, 5))]
# 手前の草: (番号, 幅, 高さ, 種, 葉, 羊歯の葉, 穂の茎)
R3_FORE = [(1, 96, 88, 361, 6, 0, True), (2, 120, 72, 362, 8, 0, False), (3, 72, 96, 363, 5, 0, False),
           (4, 112, 64, 364, 0, 4, False), (5, 88, 60, 365, 0, 3, False), (6, 104, 80, 366, 4, 2, False)]
R3_ITEMS = ('conifer', 'conifer_dense', 'conifer_near', 'bough_hang', 'bush_clump', 'fore_grass', 'seat_soil')
R3_SIZES = {}   # 名前 → 期待の大きさ (W, H) か None (高さだけ)


def make_r3(root, only=None):
    items = [i for i in R3_ITEMS if only is None or i in only]
    written, info = [], {}
    R = lambda n: os.path.join(root, 'relief', n + '.png')
    for it in items:
        if it == 'conifer':
            for (w, k, seed, nb) in R3_CONIFERS:
                name = 'conifer_w%d_%d' % (w, k)
                img, meta = conifer_r3(w, 280, seed, nb=nb)
                written.append(_save(img, R(name)))
                info[name] = dict(meta, trunkW=w, size=(img.shape[1], img.shape[0]))
                R3_SIZES[name] = (None, 280)
        elif it == 'conifer_dense':
            for (k, w, h, seed, tw, lv, ep) in R3_DENSE:
                name = 'conifer_dense_%d' % k
                img, meta = conifer_dense_r3(w, h, seed, tw=tw, levels=lv, env_pow=ep)
                written.append(_save(img, R(name)))
                info[name] = dict(meta, trunkW=tw, size=(img.shape[1], img.shape[0]))
                R3_SIZES[name] = (None, h)
        elif it == 'conifer_near':
            for (k, w, seed) in R3_NEAR:
                name = 'conifer_near_%d' % k
                img, meta = conifer_near_r3(w, 320, seed)
                written.append(_save(img, R(name)))
                info[name] = dict(meta, trunkW=w, size=(img.shape[1], img.shape[0]))
                R3_SIZES[name] = (None, 320)
        elif it == 'bough_hang':
            for (k, seed) in R3_HANG:
                name = 'bough_hang_%d' % k
                img, meta = bough_hang_r3(seed)
                written.append(_save(img, R(name)))
                info[name] = dict(meta, size=(img.shape[1], img.shape[0]))
                R3_SIZES[name] = (160, 80)
        elif it == 'bush_clump':
            for (sz, k, w, h, seed, nl) in R3_BUSH:
                name = 'bush_clump_%s_%d' % (sz, k)
                img, meta = bush_clump_r3(seed, w, h, nl, ngrass=(3, 4) if sz == 's' else (4, 5) if sz == 'm' else (5, 6))
                written.append(_save(img, R(name)))
                info[name] = dict(meta, size=(img.shape[1], img.shape[0]))
                R3_SIZES[name] = (w, h)
        elif it == 'fore_grass':
            for (k, w, h, seed, nbl, nfr, stalk) in R3_FORE:
                name = 'fore_grass_%d' % k
                img, meta = fore_grass_r3(seed, w, h, nbl, nfr, stalk)
                written.append(_save(img, R(name)))
                info[name] = dict(meta, size=(img.shape[1], img.shape[0]))
                R3_SIZES[name] = (None, h)
        elif it == 'seat_soil':
            # 二周目の座席の土 (top_path_seat_r2_*) と同じ種 950〜953 に、ノイズの種 970〜973 を足す。新しい名前 top_path_seat_r3_{a..d}
            for i, v in enumerate('abcd'):
                name = 'top_path_seat_r3_%s' % v
                written.append(_save(earth_tile_r3(950 + i, 970 + i), os.path.join(root, 'tiles', name + '.png'), 'RGB'))
                R3_SIZES[name] = (64, 64)
    # 色表へ写す (塗った色はどれも色表の色＝変わらないはず。変わったら塗り間違い)
    pal = SP.load_palette(PAL_JSON)
    for p in written:
        a = SP.load_rgba(p)
        b = SP.map_image(a, pal)
        if not np.array_equal(a, b):
            print('注意: 色表の外の色があったので写した', os.path.basename(p))
            tile = '/tiles/' in p.replace('\\', '/')
            Image.fromarray(b[..., :3] if tile else b, 'RGB' if tile else 'RGBA').save(p, optimize=True)
    return written, info


def check_r3(paths):
    """三周目の点検: 色表 p95 = 0・半透明 (α 1〜178) なし・大きさ (R3_SIZES)。NG があれば False"""
    pal = SP.load_palette(PAL_JSON)
    ok_all = True
    for p in paths:
        a = SP.load_rgba(p)
        name = os.path.splitext(os.path.basename(p))[0]
        d = SP.palette_distance(a, pal)
        p95 = float(np.percentile(d, 95)) if len(d) else 0.0
        semi = int(((a[..., 3] > 0) & (a[..., 3] < 179)).sum())
        exp = R3_SIZES.get(name)
        size_ok = True
        if exp is not None:
            ew, eh = exp
            size_ok = (ew is None or a.shape[1] == ew) and (eh is None or a.shape[0] == eh)
        ok = p95 == 0.0 and semi == 0 and size_ok
        ok_all &= ok
        print('%-26s %s  色表 p95 %.1f  半透明 %d  大きさ %d×%d%s' % (name, 'OK' if ok else 'NG', p95, semi, a.shape[1], a.shape[0],
                                                               '' if size_ok else ' (期待 %s)' % (exp,)))
    return ok_all


# ---------------------------------------------------------------- 霧の模型 (絵が霧の帯でどう見えるか)
def _s2l(c):
    c = np.asarray(c, float) / 255.0
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def _l2s(c):
    c = np.clip(np.asarray(c, float), 0.0, 1.0)
    return np.clip(np.round(np.where(c <= 0.0031308, c * 12.92, 1.055 * c ** (1 / 2.4) - 0.055) * 255.0), 0, 255).astype(np.uint8)


def _gauss_blur(a, sigma):
    """分離した 2 回の 1 次元ガウス (端は端の値を伸ばす)。a は (H, W, C) の float"""
    if sigma <= 0:
        return a
    r = int(math.ceil(sigma * 3))
    k = np.exp(-0.5 * (np.arange(-r, r + 1) / sigma) ** 2)
    k /= k.sum()
    p = np.pad(a, ((r, r), (0, 0), (0, 0)), mode='edge')
    a = sum(k[i] * p[i:i + a.shape[0]] for i in range(2 * r + 1))
    p = np.pad(a, ((0, 0), (r, r), (0, 0)), mode='edge')
    return sum(k[i] * p[:, i:i + a.shape[1]] for i in range(2 * r + 1))


def fog_model(rgba, fog=0.55, sigma=3.0, tint=0.5, px=3.0, fogc=(150, 156, 176), normal=None, receive=0.6,
              light=(-0.55, 0.55, 0.63)):
    """霧の帯の見え方の模型: 1 ドット = px 画素に拡げ、(法線があれば左上の手前からの光で照らし)・tint (sRGB の読み＝線形にして掛ける)・
    霧 fog で霧の色へ寄せ・霧の色の地に重ね・σ sigma のガウスでぼかす (奥のぼかし)。線形の色で計算して sRGB で返す"""
    s = _rnd((px))
    a = np.repeat(np.repeat(rgba, s, 0), s, 1)
    alpha = (a[..., 3:4] >= 102).astype(float)
    alb = _s2l(a[..., :3])
    shade = 1.0
    if normal is not None:
        nm = np.repeat(np.repeat(normal, s, 0), s, 1)[..., :3].astype(float) / 255.0 * 2 - 1
        l = np.array(light, float)
        l /= np.linalg.norm(l)
        ndl = np.clip((nm * l).sum(-1, keepdims=True), 0, 1)
        shade = (1.0 - receive) + receive * (0.35 + 1.1 * ndl)
    col = alb * shade * float(_s2l([tint * 255.0])[0])
    fl = _s2l(fogc)
    out = alpha * (col * (1 - fog) + fl * fog) + (1 - alpha) * fl
    return _l2s(_gauss_blur(out, sigma))


def fogmodel_sheet(paths, out, refs=(), fog=0.55, sigma=3.0, tint=0.5, px=3.0, show=2, crop_h=None):
    """霧の模型のシート: 絵ごとに [模型 (法線なし)] [模型 (法線あり＝_n.png)] を show 倍 (本家の 2 倍の切り抜きと同じ縮尺) で並べ、右に本家の切り抜き"""
    from PIL import ImageDraw
    tiles = []
    for p in paths:
        a = np.array(Image.open(p).convert('RGBA'))
        n_path = os.path.splitext(p)[0] + '_n.png'
        nrm = np.array(Image.open(n_path).convert('RGBA')) if os.path.exists(n_path) else None
        for lab, nn in (('flat', None), ('normal', nrm)):
            if lab == 'normal' and nn is None:
                continue
            m = fog_model(a, fog, sigma, tint, px, normal=nn)
            if crop_h:
                m = m[-crop_h:]
            im = Image.fromarray(m, 'RGB')
            im = im.resize((im.width * show, im.height * show), Image.NEAREST)
            tiles.append(('%s %s fog%.2f σ%.0f tint%.1f' % (os.path.basename(p)[:-4], lab, fog, sigma, tint), im))
    for rp in refs:
        if os.path.exists(rp):
            tiles.append(('本家 ' + os.path.basename(rp), Image.open(rp).convert('RGB')))
    Wmax = max(2400, max(t[1].width for t in tiles) + 20)
    x = y = 10
    rowh = 0
    pos = []
    for name, im in tiles:
        if x + im.width > Wmax:
            x = 10
            y += rowh + 24
            rowh = 0
        pos.append((x, y))
        x += im.width + 12
        rowh = max(rowh, im.height)
    sh = Image.new('RGB', (Wmax, y + rowh + 30), (38, 42, 58))
    dr = ImageDraw.Draw(sh)
    for (name, im), (px_, py) in zip(tiles, pos):
        sh.paste(im, (px_, py + 14))
        dr.text((px_, py), name, fill=(230, 230, 210))
    sh.save(out)


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
    r3 = sub.add_parser('r3')
    r3.add_argument('root')
    r3.add_argument('--only', help='カンマ区切り: ' + ','.join(R3_ITEMS))
    r3.add_argument('--sheet', help='4 倍の確認シート (png)')
    r3.add_argument('--info', help='足元・大きさの表 (json) の書き出し先')
    fm = sub.add_parser('fogmodel')
    fm.add_argument('files', nargs='+')
    fm.add_argument('--out', required=True)
    fm.add_argument('--fog', type=float, default=0.55)
    fm.add_argument('--sigma', type=float, default=3.0)
    fm.add_argument('--tint', type=float, default=0.5)
    fm.add_argument('--px', type=float, default=3.0, help='1 ドットの画面の画素 (霧の帯の幹 s 7〜9 で約 3)')
    fm.add_argument('--ref', action='append', default=[], help='並べる本家の切り抜き (2 倍の物)')
    fm.add_argument('--crop-h', type=int, default=0, help='模型の下から何画素を見せるか (0 = 全部)')
    c = sub.add_parser('check')
    c.add_argument('files', nargs='+')
    args = ap.parse_args()
    if args.cmd == 'r3':
        only = None
        if args.only:
            only = [x.strip() for x in args.only.split(',') if x.strip()]
            bad = [x for x in only if x not in R3_ITEMS]
            if bad:
                sys.exit('知らない名前: %s (選べるのは %s)' % (','.join(bad), ','.join(R3_ITEMS)))
        written, info = make_r3(args.root, only)
        ok = check_r3(written)
        check(written)
        for k, v in info.items():
            print(k, json.dumps(v, ensure_ascii=False))
        if args.info:
            json.dump(info, open(args.info, 'w'), ensure_ascii=False, indent=1)
        if args.sheet:
            sheet(written, args.sheet)
        if not ok:
            sys.exit('点検 NG')
        return
    if args.cmd == 'fogmodel':
        fogmodel_sheet(args.files, args.out, args.ref, args.fog, args.sigma, args.tint, args.px, crop_h=args.crop_h or None)
        print(args.out)
        return
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

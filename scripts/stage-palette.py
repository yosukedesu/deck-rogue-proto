#!/usr/bin/env python3
"""scripts/stage-palette.py — 幕の舞台用の色表を作り、舞台の絵を Oklab の距離で写す (2026-09-30 HD-2D 見本 P05)。

計画 docs/design/hd2d-slice-plan-2026-09-30.md §2-2「舞台の絵（キャラ以外）は、幕1の舞台用の色表（32〜48色）へ
Oklab の距離で写し、色をそろえる。キャラは写さない」。出所が3つ (PixelLab の絵・tile-calm のタイル・コード生成) の
材質の色をそろえるための道具。

使い方:
  # 色表を作る (選んだタイルと半立体から。k-means を Oklab で・重みは画素数・種は固定＝同じ入力なら同じ表)
  python3 scripts/stage-palette.py build --k 40 --out docs/pixellab/hd2d-act1/palette-act1 <png...>
      → <out>.png (8列の見本・1色＝8×8)・<out>.json ({"colors": ["#rrggbb", ...], "oklab": [[L,a,b], ...]})
  # 絵を色表へ写す (不透明な画素だけ。透明はそのまま。--dither で 4×4 の秩序ディザ＝2番目に近い色と混ぜる)
  python3 scripts/stage-palette.py map --palette docs/pixellab/hd2d-act1/palette-act1.json --outdir <dir> <png...>
  # 色表からの距離 (Oklab・不透明な画素) の 95 パーセンタイル。art-lint.py も同じ関数を使う
  python3 scripts/stage-palette.py dist --palette <json> <png...>

Oklab は Björn Ottosson の式 (sRGB→線形→LMS→立方根→Lab)。距離は ΔE_ok×100 (1.0 ≈ 見分けられる最小の差の目安)。
"""
import argparse
import json
import os
import sys

import numpy as np
from PIL import Image


# ---------- Oklab ----------
def srgb_to_linear(c):
    c = np.asarray(c, dtype=np.float64)
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def linear_to_srgb(c):
    c = np.clip(np.asarray(c, dtype=np.float64), 0.0, 1.0)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * np.power(c, 1 / 2.4) - 0.055)


_M1 = np.array([[0.4122214708, 0.5363325363, 0.0514459929],
                [0.2119034982, 0.6806995451, 0.1073969566],
                [0.0883024619, 0.2817188376, 0.6299787005]])
_M2 = np.array([[0.2104542553, 0.7936177850, -0.0040720468],
                [1.9779984951, -2.4285922050, 0.4505937099],
                [0.0259040371, 0.7827717662, -0.8086757660]])
_M2I = np.linalg.inv(_M2)
_M1I = np.linalg.inv(_M1)


def rgb8_to_oklab(rgb):
    """(..., 3) の 0〜255 → (..., 3) の Oklab (L は 0〜1)"""
    lin = srgb_to_linear(np.asarray(rgb, dtype=np.float64) / 255.0)
    lms = lin @ _M1.T
    return np.cbrt(lms) @ _M2.T


def oklab_to_rgb8(lab):
    lms_ = np.asarray(lab, dtype=np.float64) @ _M2I.T
    lin = (lms_ ** 3) @ _M1I.T
    return np.clip(np.round(linear_to_srgb(lin) * 255.0), 0, 255).astype(np.uint8)


def load_rgba(path):
    return np.array(Image.open(path).convert('RGBA'))


def opaque_pixels(a, alpha_min=128):
    m = a[..., 3] >= alpha_min
    return a[..., :3][m], m


# ---------- k-means (Oklab・重みつき・種固定) ----------
def weighted_kmeans(x, w, k, iters=40, seed=20260930):
    rng = np.random.default_rng(seed)
    n = len(x)
    k = min(k, n)
    # k-means++ の初期値 (重みつき)
    idx = [int(rng.choice(n, p=w / w.sum()))]
    d2 = ((x - x[idx[0]]) ** 2).sum(1)
    for _ in range(1, k):
        p = w * d2
        if p.sum() <= 0:
            break
        j = int(rng.choice(n, p=p / p.sum()))
        idx.append(j)
        d2 = np.minimum(d2, ((x - x[j]) ** 2).sum(1))
    c = x[idx].copy()
    for _ in range(iters):
        dist = ((x[:, None, :] - c[None, :, :]) ** 2).sum(2)
        lab = dist.argmin(1)
        newc = c.copy()
        for j in range(len(c)):
            m = lab == j
            if m.any():
                newc[j] = (x[m] * w[m, None]).sum(0) / w[m].sum()
        if np.allclose(newc, c, atol=1e-6):
            c = newc
            break
        c = newc
    return c


def build_palette(files, k, max_unique=40000):
    cols = []
    for f in files:
        px, _ = opaque_pixels(load_rgba(f))
        cols.append(px)
    allpx = np.concatenate(cols, 0)
    uniq, cnt = np.unique(allpx.reshape(-1, 3), axis=0, return_counts=True)
    if len(uniq) > max_unique:  # 多すぎる時は頻度の高い順に切る (舞台の絵は数千色程度)
        order = np.argsort(-cnt)[:max_unique]
        uniq, cnt = uniq[order], cnt[order]
    lab = rgb8_to_oklab(uniq)
    # 明るさの軸を少し重く (段の見分けを色みより優先する)。距離の単位は写す時と同じ素の Oklab に戻す
    scale = np.array([1.5, 1.0, 1.0])
    c = weighted_kmeans(lab * scale, cnt.astype(np.float64), k) / scale
    rgb = oklab_to_rgb8(c)
    # 同じ色に丸まったものを落とし、明るさ順に並べる
    rgb, ui = np.unique(rgb, axis=0, return_index=True)
    lab2 = rgb8_to_oklab(rgb)
    order = np.lexsort((np.arctan2(lab2[:, 2], lab2[:, 1]), lab2[:, 0]))
    return rgb[order]


def save_palette(rgb, out):
    os.makedirs(os.path.dirname(out) or '.', exist_ok=True)
    lab = rgb8_to_oklab(rgb)
    json.dump({'colors': ['#%02x%02x%02x' % tuple(int(v) for v in c) for c in rgb],
               'oklab': [[round(float(v), 5) for v in l] for l in lab],
               'k': int(len(rgb))}, open(out + '.json', 'w'), ensure_ascii=False, indent=1)
    cols = 8
    rows = (len(rgb) + cols - 1) // cols
    im = Image.new('RGB', (cols * 8, rows * 8), (0, 0, 0))
    px = im.load()
    for i, c in enumerate(rgb):
        x0, y0 = (i % cols) * 8, (i // cols) * 8
        for y in range(8):
            for x in range(8):
                px[x0 + x, y0 + y] = tuple(int(v) for v in c)
    im.save(out + '.png')


def load_palette(path):
    if path.endswith('.png'):
        a = np.array(Image.open(path).convert('RGB')).reshape(-1, 3)
        return np.unique(a, axis=0)
    j = json.load(open(path))
    return np.array([[int(h[1:3], 16), int(h[3:5], 16), int(h[5:7], 16)] for h in j['colors']], dtype=np.uint8)


_BAYER4 = np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]) / 16.0 + 1 / 32.0


def map_image(a, pal_rgb, dither=False, alpha_min=128):
    """不透明な画素を色表の最も近い色へ。dither なら 2番目に近い色と秩序ディザで混ぜる (近い2色の間にある時だけ)"""
    out = a.copy()
    m = a[..., 3] >= alpha_min
    if not m.any():
        return out
    pal_lab = rgb8_to_oklab(pal_rgb)
    lab = rgb8_to_oklab(a[..., :3][m])
    d = ((lab[:, None, :] - pal_lab[None, :, :]) ** 2).sum(2)
    order = np.argsort(d, 1)
    i1 = order[:, 0]
    if dither:
        i2 = order[:, 1]
        d1 = np.sqrt(d[np.arange(len(d)), i1])
        d2 = np.sqrt(d[np.arange(len(d)), i2])
        t = d1 / np.maximum(d1 + d2, 1e-9)  # 0=1番に一致・0.5=2色の真ん中
        ys, xs = np.nonzero(m)
        thr = _BAYER4[ys % 4, xs % 4]
        pick2 = t > thr * 1.0
        pick2 &= t > 0.25
        idx = np.where(pick2, i2, i1)
    else:
        idx = i1
    out[..., :3][m] = pal_rgb[idx]
    return out


def palette_distance(a, pal_rgb, alpha_min=128):
    """不透明な画素ごとの色表までの最短距離 (ΔE_ok×100)。無ければ空の配列"""
    px, _ = opaque_pixels(a, alpha_min)
    if len(px) == 0:
        return np.zeros(0)
    uniq, inv = np.unique(px, axis=0, return_inverse=True)
    lab = rgb8_to_oklab(uniq)
    pal_lab = rgb8_to_oklab(pal_rgb)
    d = np.sqrt(((lab[:, None, :] - pal_lab[None, :, :]) ** 2).sum(2)).min(1) * 100.0
    return d[inv.reshape(-1)]


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest='cmd', required=True)
    b = sub.add_parser('build')
    b.add_argument('--k', type=int, default=40)
    b.add_argument('--out', required=True)
    b.add_argument('files', nargs='+')
    mp = sub.add_parser('map')
    mp.add_argument('--palette', required=True)
    mp.add_argument('--outdir', required=True)
    mp.add_argument('--suffix', default='')
    mp.add_argument('--dither', action='store_true')
    mp.add_argument('files', nargs='+')
    ds = sub.add_parser('dist')
    ds.add_argument('--palette', required=True)
    ds.add_argument('files', nargs='+')
    args = ap.parse_args()

    if args.cmd == 'build':
        if not (8 <= args.k <= 64):
            sys.exit('k は 8〜64 (計画は 32〜48)')
        rgb = build_palette(args.files, args.k)
        save_palette(rgb, args.out)
        print(f'{len(rgb)} 色 → {args.out}.png / .json (入力 {len(args.files)} 枚)')
    elif args.cmd == 'map':
        pal = load_palette(args.palette)
        os.makedirs(args.outdir, exist_ok=True)
        for f in args.files:
            a = load_rgba(f)
            before = palette_distance(a, pal)
            o = map_image(a, pal, dither=args.dither)
            name = os.path.splitext(os.path.basename(f))[0] + args.suffix + '.png'
            Image.fromarray(o, 'RGBA').save(os.path.join(args.outdir, name))
            p95 = float(np.percentile(before, 95)) if len(before) else 0.0
            print(f'{name}: 写す前の距離 p95={p95:.1f}')
    elif args.cmd == 'dist':
        pal = load_palette(args.palette)
        for f in args.files:
            d = palette_distance(load_rgba(f), pal)
            if len(d):
                print(f'{os.path.basename(f)}: p50={np.percentile(d, 50):.1f} p95={np.percentile(d, 95):.1f} max={d.max():.1f}')
            else:
                print(f'{os.path.basename(f)}: 不透明な画素なし')


if __name__ == '__main__':
    main()

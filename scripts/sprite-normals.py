#!/usr/bin/env python3
"""scripts/sprite-normals.py — キャラの絵 (リーダー・敵・人形の全コマ) の法線マップ <名前>_n.png と発光マスク <名前>_e.png を作る
(2026-09-30 HD-2D 見本 P05。計画 docs/design/hd2d-slice-plan-2026-09-30.md P05 手順7・8)。

法線 (normals): アルファの輪郭からの距離で膨らませる (Laigter と同じ考え)。高さ h = 滑らかな丘 (輪郭で0・内側 R ドットで1)
  ＋ 絵の明るさの細部を少し (--detail)。傾き (ソーベル) から法線を作り、RGB = n×0.5+0.5 で書く。
  向き: 赤＝+x (右)・緑＝+y (上＝画像の上。Unity/OpenGL の流儀)・青＝+z (手前)。透明の画素は (128,128,255)。
  アルファは元の絵のまま (取り込みの alphaIsTransparency の縁のにじみで透明部の色が変わっても、シェーダは元の絵のアルファで切る)。
  StageUnitLit は tex×2−1 で自分でほどく (UnpackNormal は使わない)。取り込みは sRGB なし・Point (P06 ArtImporter)。

発光 (emission): 色相と明るさで自動に抜き出す (--preset konoha|wolf|ogre|auto)。発光する画素は元の色、ほかは黒 (アルファは元の絵)。
  __名前の設定は PRESETS__ に。抜き出した後は確認シート (--sheet) で目で見て、要れば --add/--remove の矩形で直す。

使い方:
  python3 scripts/sprite-normals.py normals <png...>                 # 隣に <名前>_n.png
  python3 scripts/sprite-normals.py normals --all                    # リーダー・敵・人形の全部 (既にあれば作り直す)
  python3 scripts/sprite-normals.py emission --preset konoha <png...> # 隣に <名前>_e.png
  python3 scripts/sprite-normals.py sheet --out <png> <元の png...>   # 元・法線・法線で照らした見本・発光 を並べる
オプション: --radius 0 (丸みが立ち上がる幅・ドット。0=自動) --strength 2.5 --detail 0.25 --suffix _n
"""
import argparse
import colorsys
import glob
import json
import os
import sys

import numpy as np
from PIL import Image, ImageDraw

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
ART = os.path.join(ROOT, 'unity', 'Assets', 'Resources', 'Art')


def is_generated(path):
    b = os.path.splitext(os.path.basename(path))[0]
    return b.endswith('_n') or b.endswith('_e')


def all_character_pngs():
    fs = []
    fs += glob.glob(os.path.join(ART, 'leaders', 'leader_*.png'))
    fs += glob.glob(os.path.join(ART, 'leaders', 'anim', '*.png'))
    fs += glob.glob(os.path.join(ART, 'enemies', '*.png'))
    fs += glob.glob(os.path.join(ART, 'dolls', '*.png'))
    fs = [f for f in fs if not is_generated(f) and not f.endswith('_icon.png')]
    return sorted(fs)


def load(path):
    return np.array(Image.open(path).convert('RGBA'))


def distance_inside(mask):
    """不透明の画素ごとに、いちばん近い透明の画素 (または絵の外) までのチェス盤距離の近似 (2パスのチャンファ)。
    scipy を使わない (numpy だけ)。絵は 128 ドット以下なので速さは問題ない"""
    h, w = mask.shape
    INF = 10 ** 6
    d = np.where(mask, INF, 0).astype(np.float64)
    a, b = 1.0, 1.4142
    # 前向き
    for y in range(h):
        for x in range(w):
            if d[y, x] == 0:
                continue
            v = d[y, x]
            v = min(v, (d[y, x - 1] + a) if x > 0 else a)
            v = min(v, (d[y - 1, x] + a) if y > 0 else a)
            v = min(v, (d[y - 1, x - 1] + b) if (x > 0 and y > 0) else b)
            v = min(v, (d[y - 1, x + 1] + b) if (x < w - 1 and y > 0) else b)
            d[y, x] = v
    # 後ろ向き
    for y in range(h - 1, -1, -1):
        for x in range(w - 1, -1, -1):
            if d[y, x] == 0:
                continue
            v = d[y, x]
            v = min(v, (d[y, x + 1] + a) if x < w - 1 else a)
            v = min(v, (d[y + 1, x] + a) if y < h - 1 else a)
            v = min(v, (d[y + 1, x + 1] + b) if (x < w - 1 and y < h - 1) else b)
            v = min(v, (d[y + 1, x - 1] + b) if (x > 0 and y < h - 1) else b)
            d[y, x] = v
    return d


def blur3(x, n=1):
    out = x.astype(np.float64)
    for _ in range(n):
        p = np.pad(out, 1, mode='edge')
        out = (p[:-2, :-2] + 2 * p[:-2, 1:-1] + p[:-2, 2:] + 2 * p[1:-1, :-2] + 4 * p[1:-1, 1:-1] + 2 * p[1:-1, 2:]
               + p[2:, :-2] + 2 * p[2:, 1:-1] + p[2:, 2:]) / 16.0
    return out


def auto_radius(alpha):
    ys, xs = np.nonzero(alpha)
    bw, bh = xs.max() - xs.min() + 1, ys.max() - ys.min() + 1
    return float(np.clip(0.1 * min(bw, bh), 3.0, 10.0))


def normal_map(a, radius=None, strength=2.5, detail=0.25):
    alpha = a[..., 3] >= 128
    if not alpha.any():
        out = np.zeros_like(a)
        out[..., 0] = 128; out[..., 1] = 128; out[..., 2] = 255
        return out
    if not radius:
        radius = auto_radius(alpha)   # 絵の大きさに合わせる (人形 32・敵 64・ボス 128 で丸みの幅をそろえる)
    dist = distance_inside(alpha)
    t = np.clip(dist / radius, 0.0, 1.0)
    hill = np.sqrt(1.0 - (1.0 - t) ** 2)   # 輪郭で急に立ち上がり、内側で平らになる丸み (円の断面)
    rgb = a[..., :3].astype(np.float64)
    lum = (0.299 * rgb[..., 0] + 0.587 * rgb[..., 1] + 0.114 * rgb[..., 2]) / 255.0
    # 明るさの細部: 局所平均からの差だけ (焼き込まれた陰影の大きな勾配は丸みと二重にしない)
    lum_hp = lum - blur3(np.where(alpha, lum, lum[alpha].mean()), 3)
    hgt = hill + detail * lum_hp
    hgt = np.where(alpha, hgt, 0.0)
    hgt = blur3(hgt, 1) * alpha + hgt * (~alpha)
    p = np.pad(hgt, 1, mode='edge')
    # ソーベル (画像の座標: x 右・y 下)
    gx = (p[:-2, 2:] + 2 * p[1:-1, 2:] + p[2:, 2:] - p[:-2, :-2] - 2 * p[1:-1, :-2] - p[2:, :-2]) / 8.0
    gy = (p[2:, :-2] + 2 * p[2:, 1:-1] + p[2:, 2:] - p[:-2, :-2] - 2 * p[:-2, 1:-1] - p[:-2, 2:]) / 8.0
    nx = -gx * strength
    ny = gy * strength           # 上 (画像の y の逆) を + にする: n_up = -dh/dy_up = +dh/dy_img
    nz = np.ones_like(nx)
    ln = np.sqrt(nx * nx + ny * ny + nz * nz)
    n = np.dstack([nx / ln, ny / ln, nz / ln])
    enc = np.clip(np.round((n * 0.5 + 0.5) * 255.0), 0, 255).astype(np.uint8)
    out = np.zeros_like(a)
    out[..., :3] = enc
    out[~alpha, 0] = 128; out[~alpha, 1] = 128; out[~alpha, 2] = 255
    out[..., 3] = a[..., 3]
    return out


# ---------- 発光 ----------
# 色相は度 (0〜360)・彩度と明るさは 0〜1。どれか1つの帯に入れば光る
PRESETS = {
    # このは: 斧の刃のマナの結晶 (青緑〜空色) と真鍮の灯・握り手の淡い光 (黄〜橙の明るい所)
    # (2026-09-30 実測: 一枚絵の斧の宝石は緑 h144〜148・攻撃のコマの刃は青緑 h154〜178・真鍮は h30〜41 の明るい所。肌は h17〜24 なので除く)
    'konoha': [
        {'name': '結晶', 'hue': (135, 205), 'sat': (0.30, 1.0), 'val': (0.45, 1.0)},
        {'name': '真鍮の灯', 'hue': (30, 58), 'sat': (0.35, 1.0), 'val': (0.72, 1.0)},
    ],
    # 狼: 脈のマナを浴びて内側から淡く光る苔色の毛の、いちばん明るい所 (淡い緑〜青白)。目は黒い豆なので光らない
    # (実測: いちばん明るい毛はクリーム色 h37〜54・彩度0.15以下・明るさ0.9以上＝胸の襟毛と尾の先)
    'wolf': [
        {'name': '光る毛の芯', 'hue': (30, 70), 'sat': (0.0, 0.20), 'val': (0.90, 1.0)},
        {'name': '白', 'hue': (0, 360), 'sat': (0.0, 0.05), 'val': (0.97, 1.0)},
    ],
    # オーガ (猪面の獣): 鬣の金と、脈の光を浴びた明るい差し色
    'ogre': [
        {'name': '鬣の金', 'hue': (38, 60), 'sat': (0.45, 1.0), 'val': (0.78, 1.0)},
        {'name': '脈の光', 'hue': (150, 210), 'sat': (0.35, 1.0), 'val': (0.60, 1.0)},
    ],
}


def emission_mask(a, bands):
    alpha = a[..., 3] >= 128
    rgb = a[..., :3].astype(np.float64) / 255.0
    mx = rgb.max(-1)
    mn = rgb.min(-1)
    v = mx
    s = np.where(mx > 0, (mx - mn) / np.maximum(mx, 1e-9), 0.0)
    # 色相 (度)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    d = np.maximum(mx - mn, 1e-9)
    hh = np.where(mx == r, ((g - b) / d) % 6, np.where(mx == g, (b - r) / d + 2, (r - g) / d + 4)) * 60.0
    hit = np.zeros(alpha.shape, bool)
    for bd in bands:
        h0, h1 = bd['hue']
        inh = (hh >= h0) & (hh <= h1) if h0 <= h1 else ((hh >= h0) | (hh <= h1))
        m = inh & (s >= bd['sat'][0]) & (s <= bd['sat'][1]) & (v >= bd['val'][0]) & (v <= bd['val'][1])
        hit |= m
    hit &= alpha
    # 1画素だけ離れて光る点は落とす (8近傍に仲間が無い)
    p = np.pad(hit, 1)
    nb = sum(np.roll(np.roll(p, dy, 0), dx, 1) for dy in (-1, 0, 1) for dx in (-1, 0, 1) if (dy, dx) != (0, 0))[1:-1, 1:-1]
    hit &= nb > 0
    out = np.zeros_like(a)
    out[..., :3][hit] = a[..., :3][hit]
    out[..., 3] = a[..., 3]
    return out, int(hit.sum())


def rect_edit(mask_img, src, rects, add):
    for (x0, y0, x1, y1) in rects:
        sl = (slice(y0, y1), slice(x0, x1))
        if add:
            mask_img[sl][..., :3] = src[sl][..., :3]
        else:
            mask_img[sl][..., :3] = 0
    return mask_img


def lit_preview(a, n, light=(-0.55, 0.55, 0.63)):
    """法線で照らした見本 (左上の手前から)。明るさ = 0.35 + 0.65×max(0, n·l)"""
    l = np.array(light, dtype=np.float64)
    l = l / np.linalg.norm(l)
    nv = n[..., :3].astype(np.float64) / 255.0 * 2 - 1
    ndl = np.clip((nv * l).sum(-1), 0, 1)
    k = 0.35 + 0.65 * ndl
    rgb = np.clip(a[..., :3].astype(np.float64) * k[..., None] * 1.1, 0, 255).astype(np.uint8)
    out = a.copy()
    out[..., :3] = rgb
    return out


def sheet(files, out, scale=3):
    rows = []
    for f in files:
        a = load(f)
        stem = os.path.splitext(f)[0]
        n = load(stem + '_n.png') if os.path.exists(stem + '_n.png') else normal_map(a)
        e = load(stem + '_e.png') if os.path.exists(stem + '_e.png') else None
        rows.append((os.path.basename(f), a, n, lit_preview(a, n), e))
    w = max(r[1].shape[1] for r in rows) * scale
    h = max(r[1].shape[0] for r in rows) * scale
    cols = 4
    im = Image.new('RGB', (cols * (w + 6), len(rows) * (h + 16)), (70, 72, 80))
    d = ImageDraw.Draw(im)
    for i, (name, a, n, lit, e) in enumerate(rows):
        y = i * (h + 16)
        d.text((3, y + 1), name, fill=(255, 230, 90))
        for j, img in enumerate([a, n, lit, e]):
            if img is None:
                continue
            p = Image.fromarray(img, 'RGBA').resize((img.shape[1] * scale, img.shape[0] * scale), Image.NEAREST)
            bg = Image.new('RGBA', p.size, (70, 72, 80, 255) if j != 3 else (0, 0, 0, 255))
            bg.alpha_composite(p)
            im.paste(bg.convert('RGB'), (j * (w + 6), y + 14))
    im.save(out)


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest='cmd', required=True)
    nm = sub.add_parser('normals')
    nm.add_argument('files', nargs='*')
    nm.add_argument('--all', action='store_true')
    nm.add_argument('--radius', type=float, default=0.0, help='0=自動 (絵の短い辺の 0.1・3〜10 ドット)')
    nm.add_argument('--strength', type=float, default=2.5)
    nm.add_argument('--detail', type=float, default=0.25)
    nm.add_argument('--suffix', default='_n')
    nm.add_argument('--skip-existing', action='store_true')
    em = sub.add_parser('emission')
    em.add_argument('files', nargs='+')
    em.add_argument('--preset', required=True, choices=sorted(PRESETS.keys()))
    em.add_argument('--add', action='append', default=[], help='x0,y0,x1,y1 の矩形を光らせる (目で見て直す用)')
    em.add_argument('--remove', action='append', default=[], help='x0,y0,x1,y1 の矩形を光らせない')
    em.add_argument('--suffix', default='_e')
    sh = sub.add_parser('sheet')
    sh.add_argument('--out', required=True)
    sh.add_argument('files', nargs='+')
    args = ap.parse_args()

    if args.cmd == 'normals':
        files = all_character_pngs() if args.all else args.files
        files = [f for f in files if not is_generated(f)]
        n = 0
        for f in files:
            out = os.path.splitext(f)[0] + args.suffix + '.png'
            if args.skip_existing and os.path.exists(out):
                continue
            Image.fromarray(normal_map(load(f), args.radius, args.strength, args.detail), 'RGBA').save(out)
            n += 1
        print(f'法線 {n} 枚')
    elif args.cmd == 'emission':
        bands = PRESETS[args.preset]
        parse = lambda s: tuple(int(v) for v in s.split(','))
        for f in args.files:
            a = load(f)
            m, cnt = emission_mask(a, bands)
            if args.add:
                m = rect_edit(m, a, [parse(s) for s in args.add], True)
            if args.remove:
                m = rect_edit(m, a, [parse(s) for s in args.remove], False)
            out = os.path.splitext(f)[0] + args.suffix + '.png'
            Image.fromarray(m, 'RGBA').save(out)
            lit = int((m[..., :3].max(-1) > 0).sum())
            print(f'{os.path.basename(out)}: 光る画素 {lit} ({lit / max(1, int((a[..., 3] >= 128).sum())):.1%})')
    elif args.cmd == 'sheet':
        sheet(args.files, args.out)
        print(args.out)


if __name__ == '__main__':
    main()

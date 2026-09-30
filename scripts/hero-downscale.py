#!/usr/bin/env python3
"""scripts/hero-downscale.py — 主人公 (このは v2) を多数決で 48 ドットの背丈へ縮めた見本を作る (2026-09-30 HD-2D 見本 P05 手順6)。

計画 docs/design/hd2d-slice-plan-2026-09-30.md §1-1「主人公: 62ドットと48ドットを並べる。まず v2 を縮めた安い見本で
『大きさだけ』を W2 の途中で選んでもらう。48 が選ばれた時だけ清書を作る (P23)」。これは清書ではない。

縮め方 (1枚ごと・全コマで同じ倍率と同じ基準点):
  - 倍率 s = 目標の背丈 ÷ 一枚絵の不透明の背丈 (このはの一枚絵は 62 ドット → s = 48/62)
  - 絵の枠も s 倍 (四捨五入)。基準点は「下端の中央」＝足元の位置と左右の中心をそろえる (Stage の FeetPad と板の中心がそのまま効く)
  - 縮めた1ドットに入る元のドットを面積で数え、いちばん多い色を選ぶ (多数決。色を混ぜない＝ドット絵のまま)。
    ただし ①不透明の面積が --keep (既定 0.45) 以上なら透明より不透明を選ぶ (細い斧の柄・髪の房が消えにくい)
            ②輪郭の暗い色 (透明に接するか、明るさがまわりより大きく低い) は重み ×--outline (既定 1.35) (輪郭が途切れにくい)
  - 名前は <元の名前 の leader_green を leader_green_48 に> (一枚絵は Art/leaders/、コマは Art/leaders/anim/)

使い方:
  python3 scripts/hero-downscale.py                       # 既定: このは v2 の一枚絵と全コマ → leader_green_48*.png
  python3 scripts/hero-downscale.py --height 48 --sheet <png>   # 62 と 48 を 4px/ドットで並べた確認シートも
  python3 scripts/hero-downscale.py --check               # 書かずに背丈だけ出す
"""
import argparse
import glob
import os
import re

import numpy as np
from PIL import Image, ImageDraw

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
ART = os.path.join(ROOT, 'unity', 'Assets', 'Resources', 'Art')


def bbox_h(a):
    ys = np.nonzero(a[..., 3] >= 128)[0]
    return int(ys.max() - ys.min() + 1) if len(ys) else 0


def outline_mask(a):
    """輪郭の暗い画素: 不透明で、4近傍に透明があるか、明るさが 4近傍の平均より 40 以上低い"""
    al = a[..., 3] >= 128
    y = a[..., :3].astype(np.float64) @ np.array([0.299, 0.587, 0.114])
    p_al = np.pad(al, 1)
    edge = al & ~(p_al[:-2, 1:-1] & p_al[2:, 1:-1] & p_al[1:-1, :-2] & p_al[1:-1, 2:])
    py = np.pad(y, 1, mode='edge')
    avg = (py[:-2, 1:-1] + py[2:, 1:-1] + py[1:-1, :-2] + py[1:-1, 2:]) / 4.0
    dark = al & (avg - y > 40)
    return (edge & (y < 110)) | dark


def downscale(a, s, keep=0.45, outline=1.35):
    H, W = a.shape[:2]
    Wt, Ht = int(round(W * s)), int(round(H * s))
    out = np.zeros((Ht, Wt, 4), np.uint8)
    om = outline_mask(a)
    al = a[..., 3] >= 128
    cx_s, cx_t = W / 2.0, Wt / 2.0
    for ty in range(Ht):
        y0 = H - (Ht - ty) / s
        y1 = y0 + 1.0 / s
        for tx in range(Wt):
            x0 = cx_s + (tx - cx_t) / s
            x1 = x0 + 1.0 / s
            votes = {}
            opaque_area = 0.0
            total = 0.0
            for sy in range(max(0, int(np.floor(y0))), min(H, int(np.ceil(y1)))):
                oy = min(y1, sy + 1) - max(y0, sy)
                if oy <= 0:
                    continue
                for sx in range(max(0, int(np.floor(x0))), min(W, int(np.ceil(x1)))):
                    ox = min(x1, sx + 1) - max(x0, sx)
                    if ox <= 0:
                        continue
                    area = ox * oy
                    total += area
                    if not al[sy, sx]:
                        continue
                    opaque_area += area
                    key = tuple(int(v) for v in a[sy, sx, :3])
                    votes[key] = votes.get(key, 0.0) + area * (outline if om[sy, sx] else 1.0)
            if total <= 0 or opaque_area / total < keep or not votes:
                continue
            col = max(votes.items(), key=lambda kv: kv[1])[0]
            out[ty, tx, :3] = col
            out[ty, tx, 3] = 255
    return out


def sources():
    main = os.path.join(ART, 'leaders', 'leader_green.png')
    frames = sorted(f for f in glob.glob(os.path.join(ART, 'leaders', 'anim', 'leader_green_*.png'))
                    if re.search(r'leader_green_(idle|attack|block|hurt)_\d+\.png$', f))
    return main, frames


def out_name(path):
    d, b = os.path.split(path)
    return os.path.join(d, b.replace('leader_green', 'leader_green_48', 1))


def sheet(pairs, out, px=4):
    """(名前, 元, 縮めた) を 1ドット=px で並べる。足元をそろえる"""
    rows = []
    for name, a, b in pairs:
        rows.append((name, a, b))
    Wc = max(r[1].shape[1] for r in rows) * px
    Hc = max(r[1].shape[0] for r in rows) * px
    cols = 2
    per_row = 4
    n = len(rows)
    grid_r = (n + per_row - 1) // per_row
    im = Image.new('RGB', (per_row * (Wc * cols + 20), grid_r * (Hc + 22)), (64, 66, 74))
    d = ImageDraw.Draw(im)
    for i, (name, a, b) in enumerate(rows):
        gx, gy = (i % per_row) * (Wc * cols + 20), (i // per_row) * (Hc + 22)
        d.text((gx + 3, gy + 2), f'{name}  62:{bbox_h(a)} → 48:{bbox_h(b)}', fill=(255, 230, 90))
        for j, img in enumerate((a, b)):
            p = Image.fromarray(img, 'RGBA').resize((img.shape[1] * px, img.shape[0] * px), Image.NEAREST)
            x = gx + j * Wc + (Wc - p.width) // 2
            y = gy + 18 + Hc - p.height
            im.paste(p, (x, y), p)
    im.save(out)


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('--height', type=int, default=48)
    ap.add_argument('--keep', type=float, default=0.45)
    ap.add_argument('--outline', type=float, default=1.35)
    ap.add_argument('--sheet')
    ap.add_argument('--check', action='store_true')
    args = ap.parse_args()
    main_png, frames = sources()
    a_main = np.array(Image.open(main_png).convert('RGBA'))
    s = args.height / bbox_h(a_main)
    print(f'倍率 {s:.4f} (一枚絵の背丈 {bbox_h(a_main)} → {args.height})')
    pairs = []
    for f in [main_png] + frames:
        a = np.array(Image.open(f).convert('RGBA'))
        b = downscale(a, s, args.keep, args.outline)
        pairs.append((os.path.basename(f), a, b))
        tag = '' if 46 <= bbox_h(b) <= 50 or f != main_png else '  ← 46〜50 を外れた'
        print(f'{os.path.basename(out_name(f))}: {b.shape[1]}×{b.shape[0]} 背丈 {bbox_h(b)} (元 {bbox_h(a)}){tag}')
        if not args.check:
            Image.fromarray(b, 'RGBA').save(out_name(f))
    if args.sheet:
        sheet(pairs, args.sheet)
        print(args.sheet)


if __name__ == '__main__':
    main()

#!/usr/bin/env python3
"""docs/pixellab/hd2d-act1/relief-clean.py — 半立体 (ReliefMesh) の元の絵の手直し (2026-09-30 HD-2D 見本 P05 手順4)。

PixelLab (pixflux) は「幹なし」「地面なし」を守らず、樹冠を頼むと木を丸ごと、羊歯と茂みには根元に土の皿を描いた
(raw/relief/*_s1.png・*_s2.png。map-objects も同じ)。ここで:
  - crown (樹冠): 緑の行が途切れる所 (樹冠の下端) より下を捨てる＝幹と根を落とす。樹冠の中に見える枝は残す
  - soil (羊歯・茂み): 下の 45% にある「葉の緑でない」画素で、絵の下端とつながった塊を透明にする
  - keep: そのまま
  - 最後に、ほかから離れた欠片 (6 画素未満か、いちばん大きい塊の 3% 未満) を消し、不透明の外接矩形 + 余白 2 に切り詰める
どの元を使うかは下の PICK (2シードを見比べて選んだ。見た目の理由は同じ行に)。

使い方: python3 docs/pixellab/hd2d-act1/relief-clean.py <出力フォルダ>   (既定 scratchpad。Resources へは stage-palette.py map で写す)
"""
import colorsys
import os
import sys

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
RAW = os.path.join(HERE, 'raw', 'relief')

# 名前: (元の絵, 手直し, 理由)
PICK = {
    'canopy_1': ('canopy_1_s1.png', 'crown', 's1 = 塊の樹冠 (s2 は泡の粒の集まり＝集合体に見える)'),
    'canopy_2': ('canopy_2_s1.png', 'crown', 's1 = 横に広い樹冠 (s2 は星形の葉の繰り返し)'),
    'canopy_3': ('canopy_3_s2.png', 'crown', 's2 = 縦に丸い樹冠 (s1 は幹が樹冠の下半分まで見える)'),
    'frame_1': ('frame_1_s2.png', 'keep', 's2 = 横に伸びる葉の枝 (s1 は柳の木を丸ごと)'),
    'frame_2': ('frame_2_s2.png', 'keep', 's2 = 上から垂れる葉と蔓の帯 (s1 は門の枠に宝石の看板)'),
    'fern_1': ('fern_1_s1.png', 'soil', 's1 = 弓なりの葉 (s2 は根元に石と根)'),
    'fern_2': ('fern_2_s1.png', 'soil', 's1 = 若い羊歯 (s2 は石の上)'),
    'bush_1': ('bush_1_s1.png', 'soil', 's1 = 丸い茂み (s2 は赤い実と土)'),
}


def hsv(a):
    rgb = a[..., :3].astype(np.float64) / 255.0
    mx, mn = rgb.max(-1), rgb.min(-1)
    d = np.maximum(mx - mn, 1e-9)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    h = np.where(mx == r, ((g - b) / d) % 6, np.where(mx == g, (b - r) / d + 2, (r - g) / d + 4)) * 60.0
    s = np.where(mx > 0, (mx - mn) / np.maximum(mx, 1e-9), 0)
    return h, s, mx


def is_green(a):
    h, s, v = hsv(a)
    return (h >= 55) & (h <= 175) & (s >= 0.18)


def crown(a):
    al = a[..., 3] >= 128
    g = is_green(a) & al
    rows = g.sum(1)
    if rows.max() == 0:
        return a
    ys = np.nonzero(al.any(1))[0]
    top = ys.min()
    # 樹冠の下端: 上から見て、緑の数が最大の 25% を下回った最初の行 (樹冠の中ほどより下で)
    peak = int(np.argmax(rows))
    cut = None
    for y in range(peak, a.shape[0]):
        if rows[y] < 0.25 * rows.max():
            cut = y
            break
    out = a.copy()
    if cut is not None:
        out[cut:, :, 3] = 0
        # 下端のすぐ上の行で、緑でない (幹の) 画素が横に続く所も落とす (幹の頭が樹冠の下から覗く)
        for y in range(max(top, cut - 3), cut):
            row_trunk = al[y] & ~g[y]
            if row_trunk.sum() > 0.5 * max(1, al[y].sum()):
                out[y, :, 3] = 0
    return out


def soil(a):
    al = a[..., 3] >= 128
    h, s, v = hsv(a)
    # 土の皿は茶 (h0〜30)・赤紫の影 (h330〜360)・暗い紫 (h250〜310) と色がばらばら (2026-09-30 実測) なので、
    # 「葉の緑でない物」を土とみなす。葉の暗い輪郭 (青緑で暗い) は残す
    dark_leaf_outline = (h >= 150) & (h <= 235) & (v < 0.3) & (s >= 0.4)
    brown = al & ~is_green(a) & ~dark_leaf_outline
    ys = np.nonzero(al.any(1))[0]
    y0, y1 = ys.min(), ys.max()
    band = np.zeros_like(al)
    band[int(y0 + 0.55 * (y1 - y0)):, :] = True
    cand = brown & band
    # 下端の行から塗りつぶしで、土の色の塊だけをたどる
    seed = np.zeros_like(al)
    seed[y1 - 1:y1 + 1, :] = cand[y1 - 1:y1 + 1, :]
    reach = seed.copy()
    while True:
        p = np.pad(reach, 1)
        grow = (p[:-2, 1:-1] | p[2:, 1:-1] | p[1:-1, :-2] | p[1:-1, 2:]) & cand
        nxt = reach | grow
        if (nxt == reach).all():
            break
        reach = nxt
    out = a.copy()
    out[reach, 3] = 0
    return out


def drop_specks(a, min_px=6, rel=0.03):
    """ほかから離れた欠片を消す: min_px 未満か、いちばん大きい塊の rel 未満 (土の皿の消し残し・離れた葉の粒)"""
    al = a[..., 3] >= 128
    lab = np.zeros(al.shape, np.int32)
    n = 0
    comps = []
    for y in range(al.shape[0]):
        for x in range(al.shape[1]):
            if al[y, x] and lab[y, x] == 0:
                n += 1
                stack = [(y, x)]
                lab[y, x] = n
                cnt = 0
                while stack:
                    cy, cx = stack.pop()
                    cnt += 1
                    for dy in (-1, 0, 1):
                        for dx in (-1, 0, 1):
                            ny, nx = cy + dy, cx + dx
                            if 0 <= ny < al.shape[0] and 0 <= nx < al.shape[1] and al[ny, nx] and lab[ny, nx] == 0:
                                lab[ny, nx] = n
                                stack.append((ny, nx))
                comps.append(cnt)
    if comps:
        limit = max(min_px, rel * max(comps))
        small = [i + 1 for i, c in enumerate(comps) if c < limit]
        out = a.copy()
        out[np.isin(lab, small), 3] = 0
        return out
    return a


def trim(a, pad=2):
    al = a[..., 3] >= 128
    ys, xs = np.nonzero(al)
    y0, y1 = max(0, ys.min() - pad), min(a.shape[0], ys.max() + 1 + pad)
    x0, x1 = max(0, xs.min() - pad), min(a.shape[1], xs.max() + 1 + pad)
    out = a[y0:y1, x0:x1].copy()
    out[out[..., 3] < 128] = 0   # 半透明は作らない (切り抜きの縁をはっきり)
    return out


def main():
    outdir = sys.argv[1] if len(sys.argv) > 1 else os.path.join(HERE, 'raw', 'relief-clean')
    os.makedirs(outdir, exist_ok=True)
    for name, (src, mode, why) in PICK.items():
        a = np.array(Image.open(os.path.join(RAW, src)).convert('RGBA'))
        if mode == 'crown':
            a = crown(a)
        elif mode == 'soil':
            a = soil(a)
        a = trim(drop_specks(a))
        Image.fromarray(a, 'RGBA').save(os.path.join(outdir, name + '.png'))
        print(f'{name}: {src} ({mode}) → {a.shape[1]}×{a.shape[0]}  {why}')


if __name__ == '__main__':
    main()

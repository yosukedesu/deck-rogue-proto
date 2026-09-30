#!/usr/bin/env python3
"""本家の色の設計書の目安 (docs/design/hd2d-slice/honke-color.json の design.targets) を、撮った画で同じ物差しで測る (2026-10-01 W3b の統合)。

honke-color.md §0〜§6 を書いた作業場の道具 (scratchpad の color/measure.py・final.py・chars.py・final_d.py) の定義を写した。
Oklab で測る scripts/hd2d-color.py とは別の物差し (CIELAB D65・HSV の色相・輝度 = Rec.709 の係数を sRGB の値 0〜255 に掛けた物)。

  舞台 = hideui の画から unitsonly のマスク (マゼンタ以外) を 2px 太らせた物を除いた画素
  帯 = L* 暗部 <15・中間 15〜45・明部 45〜75・最明部 ≥75
  領域 (PC 1080 の縦。スマホ相当は H/1080 で縮める) = 樹冠 0〜150・奥の霧 180〜340・奥の段 380〜500・座席 520〜740・手前 770〜1080
  hueShareBlue195to235 = 彩度 (C*>4) の重みのうち HSV の色相 195〜235° にある割合
  charWhite_b = キャラの絵の矩形 (layout の unitBoxes) の中の L*≥68 の画素の b* の平均

使い方: python3 scripts/hd2d-honke-targets.py 名前:接頭辞=フォルダ ... [--scenes wolf,ogre] [--md 表.md] [--json 数.json]
  例: python3 scripts/hd2d-honke-targets.py "W3b:PC-S-=shots/slice" "W3b-PH:PH-S-=shots/slice" --scenes wolf
  フォルダには <接頭辞><場面>-hideui-1.png と -unitsonly-1.png (と hideui の .layout.json) が要る。
"""
import argparse
import json
import os

import numpy as np
from PIL import Image, ImageFilter

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DESIGN = os.path.join(REPO, 'docs', 'design', 'hd2d-slice', 'honke-color.json')
REG = dict(canopy=(0, 150), mist=(180, 340), wall=(380, 500), seat=(520, 740), front=(770, 1080))
BANDS = [('dark', 0, 15), ('mid', 15, 45), ('bright', 45, 75), ('peak', 75, 101)]
KEYS = ['hueShareBlue195to235', 'dark_b', 'dark_C', 'dark_rgbR', 'mid_b', 'mid_C', 'bright_C', 'chromaAtLstar60to75', 'chromaP99',
        'darkShareLstar15', 'lumaP25', 'lumaP50', 'lumaP95', 'lumaP99_9', 'mistBandLumaPeak', 'canopyOverMist', 'seatLumaP10', 'seatLumaP90',
        'frontLumaMedian', 'shaftOverRegionMedian', 'vignetteCornerOverCenter']

# ---- 色の変換 (sRGB → 線形 → XYZ → CIELAB D65・HSV)
M_RGB2XYZ = np.array([[0.4124564, 0.3575761, 0.1804375], [0.2126729, 0.7151522, 0.0721750], [0.0193339, 0.1191920, 0.9503041]])
WHITE = np.array([0.95047, 1.0, 1.08883])


def load(p):
    return np.asarray(Image.open(p).convert('RGB')).astype(np.float64) / 255.0


def s2l(c):
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def l2s(c):
    c = np.clip(c, 0, None)
    return np.where(c <= 0.0031308, 12.92 * c, 1.055 * np.power(c, 1 / 2.4) - 0.055)


def srgb2lab(c):
    t = (s2l(c) @ M_RGB2XYZ.T) / WHITE
    f = np.where(t > (6 / 29) ** 3, np.cbrt(t), t / (3 * (6 / 29) ** 2) + 4 / 29)
    return np.stack([116 * f[..., 1] - 16, 500 * (f[..., 0] - f[..., 1]), 200 * (f[..., 1] - f[..., 2])], -1)


def luma709(c):
    return (0.2126 * c[..., 0] + 0.7152 * c[..., 1] + 0.0722 * c[..., 2]) * 255


def hsv_hue(c):
    mx = c.max(-1); mn = c.min(-1); d = mx - mn
    r, g, b = c[..., 0], c[..., 1], c[..., 2]
    dd = np.maximum(d, 1e-9)
    h = np.zeros_like(mx)
    h = np.where(mx == r, ((g - b) / dd) % 6, h)
    h = np.where(mx == g, (b - r) / dd + 2, h)
    h = np.where(mx == b, (r - g) / dd + 4, h)
    return np.where(d == 0, 0, h) * 60


def dilate(m, k):
    im = Image.fromarray((m * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(2 * k + 1))
    return np.asarray(im) > 127


def erode(m, k):
    return ~dilate(~m, k)

# ---- 測る


def measure(d, prefix, sc):
    p = os.path.join(d, f'{prefix}{sc}-hideui-1.png')
    pu = os.path.join(d, f'{prefix}{sc}-unitsonly-1.png')
    if not (os.path.exists(p) and os.path.exists(pu)):
        return None
    c = load(p); u = load(pu)
    H, W = c.shape[:2]
    mag = (u[..., 0] > 0.94) & (u[..., 1] < 0.08) & (u[..., 2] > 0.94)
    st = ~dilate(~mag, 2)
    lab = srgb2lab(c)
    y = luma709(c)
    L = lab[..., 0][st]
    C = np.hypot(lab[..., 1], lab[..., 2])[st]
    out = {}
    bands = {}
    for name, lo, hi in BANDS:
        b = st & (lab[..., 0] >= lo) & (lab[..., 0] < hi)
        if b.sum() < 50:
            bands[name] = None
            continue
        lb = lab[b]
        rgb = l2s(s2l(c[b]).mean(0)) * 255
        bands[name] = dict(share=round(float(b.sum() / st.sum() * 100), 1), rgb=[round(float(v)) for v in rgb],
                           L=round(float(lb[:, 0].mean()), 1), a=round(float(lb[:, 1].mean()), 1), b=round(float(lb[:, 2].mean()), 1),
                           C=round(float(np.hypot(lb[:, 1], lb[:, 2]).mean()), 1))
    h = hsv_hue(c[st]); w = C * (C > 4)
    out['hueShareBlue195to235'] = round(float(w[(h >= 195) & (h < 235)].sum() / max(w.sum(), 1e-6)), 3)
    if bands['dark']:
        out['dark_b'] = bands['dark']['b']; out['dark_C'] = bands['dark']['C']; out['dark_rgbR'] = bands['dark']['rgb'][0]
    if bands['mid']:
        out['mid_b'] = bands['mid']['b']; out['mid_C'] = bands['mid']['C']
    if bands['bright']:
        out['bright_C'] = bands['bright']['C']
    sel = (L >= 60) & (L < 75)
    out['chromaAtLstar60to75'] = round(float(C[sel].mean()), 1) if sel.sum() > 100 else None
    out['chromaP99'] = round(float(np.percentile(C, 99)), 1)
    out['darkShareLstar15'] = round(float((L < 15).mean() * 100), 1)
    ys = y[st]
    for k, q in (('lumaP25', 25), ('lumaP50', 50), ('lumaP95', 95), ('lumaP99_9', 99.9)):
        out[k] = round(float(np.percentile(ys, q)), 1)
    sy = H / 1080.0
    rows = np.repeat(np.arange(H)[:, None], W, 1)
    reg = {}
    for k, (a, b) in REG.items():
        m = (rows >= a * sy) & (rows < b * sy) & st
        reg[k] = dict(median=round(float(np.median(y[m])), 1), p10=round(float(np.percentile(y[m], 10)), 1),
                      p90=round(float(np.percentile(y[m], 90)), 1), rgb=[int(v * 255) for v in l2s(s2l(c[m]).mean(0))])
    out['mistBandLumaPeak'] = reg['mist']['median']
    out['canopyOverMist'] = round(reg['canopy']['median'] / max(reg['mist']['median'], 1e-6), 2)
    out['seatLumaP10'] = reg['seat']['p10']; out['seatLumaP90'] = reg['seat']['p90']
    out['frontLumaMedian'] = reg['front']['median']
    sky = (rows < int(230 * sy)) & st
    if sky.sum() >= 500:
        thr = np.percentile(y[sky], 95)
        out['shaftOverRegionMedian'] = round(float(y[sky & (y >= thr)].mean() / max(float(np.median(y[sky])), 1e-6)), 2)
    center = np.zeros((H, W), bool); center[int(H * .3):int(H * .7), int(W * .3):int(W * .7)] = True
    corner = np.zeros((H, W), bool)
    for ys_ in (slice(0, int(H * .15)), slice(int(H * .85), H)):
        for xs_ in (slice(0, int(W * .12)), slice(int(W * .88), W)):
            corner[ys_, xs_] = True
    out['vignetteCornerOverCenter'] = round(float(y[corner].mean() / max(y[center].mean(), 1e-6)), 3)
    out['regions'] = reg
    out['bands'] = bands
    chw = {}
    lay = os.path.join(d, f'{prefix}{sc}-hideui-1.layout.json')
    if os.path.exists(lay):
        with open(lay, encoding='utf-8') as f:
            J = json.load(f)
        for ub in J.get('stage', {}).get('unitBoxes', []):
            x, yy, w_, h_ = [int(v) for v in ub['rectPx']]
            m = np.zeros((H, W), bool); m[max(0, yy):yy + h_, max(0, x):x + w_] = True
            m &= erode(~mag, 2)
            lb = lab[m]
            lb = lb[lb[:, 0] >= 68] if len(lb) else lb
            chw[ub['key']] = round(float(lb[:, 2].mean()), 1) if len(lb) >= 5 else None
    out['charWhite_b'] = chw
    return out


def judge(v, t):
    if v is None or not t:
        return ''
    if 'min' in t and v < t['min']:
        return '✗'
    if 'max' in t and v > t['max']:
        return '✗'
    if 'range' in t and not (t['range'][0] <= v <= t['range'][1]):
        return '✗'
    return '✓' if any(k in t for k in ('min', 'max', 'range')) else ''


def main():
    ap = argparse.ArgumentParser(description='本家の色の設計書の目安を同じ物差しで測る')
    ap.add_argument('sets', nargs='+', help='名前:接頭辞=フォルダ (接頭辞の既定 PC-S-)')
    ap.add_argument('--scenes', default='wolf')
    ap.add_argument('--md'); ap.add_argument('--json')
    a = ap.parse_args()
    with open(DESIGN, encoding='utf-8') as f:
        T = json.load(f)['design']['targets']
    res = {}
    for spec in a.sets:
        name, d = spec.split('=', 1)
        pref = 'PC-S-'
        if ':' in name:
            name, pref = name.split(':', 1)
        for sc in a.scenes.split(','):
            r = measure(d, pref, sc)
            if r:
                res[f'{name}/{sc}'] = r
    cols = list(res)
    lines = ['| 目安 | 本家 | 門 | ' + ' | '.join(cols) + ' |', '|---|---|---|' + '---|' * len(cols)]
    ok = {c: [0, 0] for c in cols}
    for k in KEYS:
        t = T.get(k, {})
        gate = ('≥%s' % t['min']) if 'min' in t else ('≤%s' % t['max']) if 'max' in t else ('%s〜%s' % tuple(t['range'])) if 'range' in t else ''
        row = [k, str(t.get('honke', '')), gate]
        for c in cols:
            v = res[c].get(k); j = judge(v, t)
            if j:
                ok[c][1] += 1
                ok[c][0] += j == '✓'
            row.append(f'{v} {j}')
        lines.append('| ' + ' | '.join(row) + ' |')
    lines.append('| キャラの白の b* (狼・enemy0) | %s | 5〜9 | ' % T.get('charWhite_b', {}).get('honke', '') + ' | '.join(str(res[c]['charWhite_b'].get('enemy0')) for c in cols) + ' |')
    lines.append('| **合格** | | | ' + ' | '.join('**%d/%d**' % tuple(ok[c]) for c in cols) + ' |')
    md = '\n'.join(lines)
    print(md)
    if a.md:
        with open(a.md, 'w', encoding='utf-8') as f:
            f.write(md + '\n')
    if a.json:
        with open(a.json, 'w', encoding='utf-8') as f:
            json.dump(res, f, ensure_ascii=False, indent=1)
    return 0


if __name__ == '__main__':
    raise SystemExit(main())

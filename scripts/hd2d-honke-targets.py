#!/usr/bin/env python3
"""本家の色の設計書の目安 (docs/design/hd2d-slice/honke-color.json の design.targets) を、撮った画で同じ物差しで測る (2026-10-01 W3b の統合)。

honke-color.md §0〜§6 を書いた作業場の道具 (scratchpad の color/measure.py・final.py・chars.py・final_d.py) の定義を写した。
Oklab で測る scripts/hd2d-color.py とは別の物差し (CIELAB D65・HSV の色相・輝度 = Rec.709 の係数を sRGB の値 0〜255 に掛けた物)。

  舞台 = hideui の画から unitsonly のマスク (マゼンタ以外) を 2px 太らせた物を除いた画素
  帯 = L* 暗部 <15・中間 15〜45・明部 45〜75・最明部 ≥75
  領域 (PC 1080 の縦。スマホ相当は H/1080 で縮める) = 樹冠 0〜150・奥の霧 180〜340・奥の段 380〜500・座席 520〜740・手前 770〜1080
  hueShareBlue195to235 = 彩度 (C*>4) の重みのうち HSV の色相 195〜235° にある割合
  charWhite_b = キャラの絵の矩形 (layout の unitBoxes) の中の L*≥68 の画素の b* の平均

  --relative (2026-10-01 二周目 レーン F): 奥の霧の帯と樹冠を主人公の足元の行 (layout.json の stage.camera.seats の player) からの距離で測る
      奥の霧 = 足元の 360〜160px 上・樹冠 = 行 0〜足元の 450px 上 (スマホ相当は1ドットの比 sc = 主人公の pxPerDot ÷ 4 で縮める)。
      新しいカメラ (二周目 22°・5°) は足元が行 654 で、既定の行 180〜340 は霧の帯 (目標 行 290〜380) より上の樹冠に当たる = 帯を上へ引っぱる物差しになる。
      変わるのは mistBandLumaPeak と canopyOverMist だけ (他の領域・キーは既定のまま)。**既定は今の定義のまま** (W3b・W5 の 16/21 と比べられるように)。
      足元が分からない画 (layout.json が無い) は既定の行で測って注に書く。
  --ref ot16 (本家。ot7・ot11 も): 本家の画を同じ物差しで (UI とキャラは hd2d-measure.py の REF_PATCHES の矩形で除く。足元は ot16 783・ot7 762・ot11 765)。
      目安 (honke-color.json) の「本家」の値は別の測り方 (敵の右の列を目で読んだ値) なので、同じ定義の本家の値を並べて読むために使う。

使い方: python3 scripts/hd2d-honke-targets.py 名前:接頭辞=フォルダ ... [--scenes wolf,ogre] [--relative] [--ref ot16] [--md 表.md] [--json 数.json]
  例: python3 scripts/hd2d-honke-targets.py "W3b:PC-S-=shots/slice" "W3b-PH:PH-S-=shots/slice" --scenes wolf
      python3 scripts/hd2d-honke-targets.py "W5:PC-S-=final/shots/hero62" "二周目:PC-S-=r2/shots/r2-slice" --scenes wolf,ogre --relative --ref ot16
  フォルダには <接頭辞><場面>-hideui-1.png と -unitsonly-1.png (と hideui の .layout.json) が要る。
"""
import argparse
import importlib.util
import json
import os
import sys

import numpy as np
from PIL import Image, ImageFilter

sys.dont_write_bytecode = True   # 読み込む hd2d-measure.py の .pyc を scripts/ に残さない (--ref の時だけ読む)
REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DESIGN = os.path.join(REPO, 'docs', 'design', 'hd2d-slice', 'honke-color.json')
REG = dict(canopy=(0, 150), mist=(180, 340), wall=(380, 500), seat=(520, 740), front=(770, 1080))
REL = dict(mist=(360, 160), canopyBelowFeet=450)   # --relative: 奥の霧 = 足元の 360〜160px 上・樹冠 = 行0〜足元の 450px 上 (PC の px)
REF_DIRS = [os.path.expanduser('~/.cache/deck-rogue/hd2d-ref')]
REF_FILES = {'ot16': ('ot_921570_16', 783), 'ot7': ('ot_921570_7', 762), 'ot11': ('ot_921570_11', 765)}   # (画の名前, 主人公の足元の行)
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


def feet_of(lay):
    """(主人公の足元の行, sc = 主人公の pxPerDot ÷ 4)。分からなければ (None, None)"""
    if not lay:
        return None, None
    stg = lay.get('stage') or {}
    seat = next((q for q in ((stg.get('camera') or {}).get('seats') or []) if q.get('key') == 'player'), None)
    ub = next((q for q in (stg.get('unitBoxes') or []) if q.get('key') == 'player'), None)
    sc = (ub['pxPerDot'] / 4.0) if ub and ub.get('pxPerDot') else None
    return (float(seat['px'][1]) if seat else None), sc


def measure(d, prefix, sc, relative=False):
    p = os.path.join(d, f'{prefix}{sc}-hideui-1.png')
    pu = os.path.join(d, f'{prefix}{sc}-unitsonly-1.png')
    if not (os.path.exists(p) and os.path.exists(pu)):
        return None
    c = load(p); u = load(pu)
    mag = (u[..., 0] > 0.94) & (u[..., 1] < 0.08) & (u[..., 2] > 0.94)
    st = ~dilate(~mag, 2)
    lay = os.path.join(d, f'{prefix}{sc}-hideui-1.layout.json')
    J = None
    if os.path.exists(lay):
        with open(lay, encoding='utf-8') as f:
            J = json.load(f)
    feet, fsc = feet_of(J)
    return core(c, st, mag, J, feet, fsc, relative)


def measure_ref(key, relative=False, ref_dirs=None):
    """本家の画 (UI とキャラは hd2d-measure.py の REF_PATCHES の矩形で除く)"""
    name, feet = REF_FILES[key]
    path = None
    for rd in (ref_dirs or []) + REF_DIRS:
        for ext in ('.jpg', '.png'):
            q = os.path.join(rd, name + ext)
            if os.path.exists(q):
                path = q; break
        if path:
            break
    if not path:
        return None
    spec = importlib.util.spec_from_file_location('hd2d_measure', os.path.join(REPO, 'scripts', 'hd2d-measure.py'))
    M = importlib.util.module_from_spec(spec); spec.loader.exec_module(M)
    pt = M.REF_PATCHES.get(name, {})
    c = load(path); H, W = c.shape[:2]
    ex = np.zeros((H, W), bool)
    for (x, y, w, h), pad in [(r_, 4) for r_ in pt.get('ui', [])] + [(r_, 6) for r_ in pt.get('chars', [])]:
        ex[max(0, int(y) - pad):int(y + h) + pad, max(0, int(x) - pad):int(x + w) + pad] = True
    return core(c, ~ex, ex, None, float(feet), 1.0, relative)


def core(c, st, mag, J, feet, fsc, relative):
    H, W = c.shape[:2]
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
    bounds = {k: (a * sy, b * sy) for k, (a, b) in REG.items()}
    out['definition'] = '既定 (画面の縦の固定の行)'
    if relative:
        if feet is None:
            out['definition'] = '足元が分からない = 既定の行で測った'
        else:
            k_ = fsc or sy
            bounds['mist'] = (feet - REL['mist'][0] * k_, feet - REL['mist'][1] * k_)
            bounds['canopy'] = (0.0, max(1.0, feet - REL['canopyBelowFeet'] * k_))
            out['definition'] = '足元から (奥の霧 = 足元の 360〜160px 上・樹冠 = 行0〜足元の 450px 上。sc %.3f)' % k_
    out['feet'] = round(feet, 1) if feet is not None else None
    out['regionRows'] = {k: [round(a, 1), round(b, 1)] for k, (a, b) in bounds.items()}
    for k, (a, b) in bounds.items():
        m = (rows >= a) & (rows < b) & st
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
    if J:
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
    ap.add_argument('--relative', action='store_true', help='奥の霧の帯と樹冠を主人公の足元からの距離で測る (二周目の新しいカメラ用。既定は画面の縦の固定の行)')
    ap.add_argument('--ref', action='append', default=[], help='本家の画も同じ物差しで (ot16・ot7・ot11)')
    ap.add_argument('--ref-dir', action='append', default=[])
    ap.add_argument('--md'); ap.add_argument('--json')
    a = ap.parse_args()
    with open(DESIGN, encoding='utf-8') as f:
        T = json.load(f)['design']['targets']
    res = {}
    for k in a.ref:
        r = measure_ref(k, a.relative, a.ref_dir)
        if r:
            res['本家 %s' % k] = r
    for spec in a.sets:
        name, d = spec.split('=', 1)
        pref = 'PC-S-'
        if ':' in name:
            name, pref = name.split(':', 1)
        for sc in a.scenes.split(','):
            r = measure(d, pref, sc, a.relative)
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
    lines.append('| キャラの白の b* (狼・enemy0) | %s | 5〜9 | ' % T.get('charWhite_b', {}).get('honke', '') + ' | '.join(str(res[c].get('charWhite_b', {}).get('enemy0')) for c in cols) + ' |')
    lines.append('| **合格** | | | ' + ' | '.join('**%d/%d**' % tuple(ok[c]) for c in cols) + ' |')
    lines.append('| 奥の霧と樹冠の測り方 | | | ' + ' | '.join('%s (霧 %s・樹冠 %s)' % (res[c].get('definition'), res[c]['regionRows'].get('mist'), res[c]['regionRows'].get('canopy')) for c in cols) + ' |')
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

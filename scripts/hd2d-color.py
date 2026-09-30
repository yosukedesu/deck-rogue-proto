#!/usr/bin/env python3
"""HD-2D 見本の色彩の物差し (2026-09-30 W3 の統合。ユーザー「本家の色彩も参考にしてほしい」)。

本家 (オクトラ1 の Steam の公式スクショ) と うちの撮影に同じ色の物差しを当てる。hd2d-measure.py の UI とキャラのマスクをそのまま使い、
「舞台の色」(UI とキャラを除いた画素) と「キャラの色」を Oklab (知覚に沿った色空間。L = 明るさ 0〜1・a = 緑↔赤・b = 青↔黄・C = 彩度) で測る。

使い方
  scripts/hd2d-color.py --ref [<本家の画のフォルダ>] --write-design docs/design/hd2d-slice/honke-color.json [--md docs/design/hd2d-slice/honke-color.md]
      本家3枚を測って色の設計書 (数字と寄せる目安) を書く。本家の画そのものはリポジトリに入れない (数字と色の値だけ)。
  scripts/hd2d-color.py <撮影のフォルダ> [--design docs/design/hd2d-slice/honke-color.json] [--out color.json] [--md color.md] [--only 正規表現]
      うちの撮影を測り、設計書の目安 (本家の夜の森 ot_921570_16) との差を出す。
  scripts/hd2d-color.py --sheet <出力.png> --set 名前=フォルダ … [--scene PC-S-wolf] [--design …]
      色の比較シート (本家と同じ大きさの画・色の帯・明るさ別の色味・色相の割合) を書く。

物差し (舞台 = UI とキャラを除いた画素。本家は ref-patches の矩形で除く。うちは hideui の画 ＋ unitsonly のマスク)
  c1 舞台の彩度の中央値 C (Oklab)         c2 彩度の上位 10% (p90)          c3 色の豊かさ (Hasler-Süsstrunk M3)
  c4 舞台の色味の平均 (a, b) と色相 h       c5 明るさ別の色味: 暗部 (L<0.36 ≈ 輝度60未満)・中間・明部 (L≥0.67 ≈ 輝度150以上) の割合と平均の色
  c6 縦3帯 (上・中・下) の平均の色        c7 色相の割合 (彩度 C>0.03 の画素の数を色相の6区分で): 赤橙・黄・緑・青緑・青・紫
  c8 色表 (k-means 8色。Oklab。割合つき)  c9 キャラの彩度 ÷ 舞台の彩度 (キャラが舞台から浮くか)・キャラの明るさ ÷ 舞台の明るさ
比べ方 (うち − 本家の目安)
  dE_*  Oklab の距離 ×100 (ΔE。2 前後で見分けにくい・5 を超えると別の色)  chroma_ratio = c1 ÷ 本家の c1   hue_dist = 色相の割合の差の合計 ÷ 2 (0〜1)
  palette_dist = 色表どうしの近い色の距離の平均 (×100)
"""
import argparse
import importlib.util
import json
import math
import os
import re
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont

sys.dont_write_bytecode = True
HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(HERE)
DEFAULT_REF = os.path.expanduser('~/.cache/deck-rogue/hd2d-ref')
DEFAULT_DESIGN = os.path.join(REPO, 'docs', 'design', 'hd2d-slice', 'honke-color.json')
FONT = os.path.join(REPO, 'unity', 'Assets', 'Resources', 'Fonts', 'NotoSansJP-Regular.otf')
FONT_B = os.path.join(REPO, 'unity', 'Assets', 'Resources', 'Fonts', 'NotoSansJP-Bold.otf')
MAIN_REF = 'ot_921570_16'   # 幕1 (坑口の森・夜) の目安にする本家の1枚 = 夜の森の戦闘

L_DARK, L_BRIGHT = 0.36, 0.67   # Oklab の L。輝度 60 ≈ 0.356・輝度 150 ≈ 0.673 (hd2d-measure の②・⑦と同じ境)
C_MIN = 0.03                    # 色相を数える彩度の下限
HUE_BINS = [('赤橙', 345, 75), ('黄', 75, 115), ('緑', 115, 170), ('青緑', 170, 225), ('青', 225, 285), ('紫', 285, 345)]


def load_mod(name, file):
    spec = importlib.util.spec_from_file_location(name, os.path.join(HERE, file))
    m = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    return m


M = load_mod('hd2d_measure', 'hd2d-measure.py')

# ------------------------------------------------------------------------------------------ Oklab


def srgb_to_lin(c):
    c = c / 255.0
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def lin_to_srgb(c):
    c = np.clip(c, 0, 1)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * np.power(c, 1 / 2.4) - 0.055) * 255.0


def to_oklab(rgb):
    """rgb (…, 3) 0〜255 → (…, 3) L a b"""
    lin = srgb_to_lin(rgb.astype(np.float64))
    r, g, b = lin[..., 0], lin[..., 1], lin[..., 2]
    l = 0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b
    m = 0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b
    s = 0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b
    l_, m_, s_ = np.cbrt(l), np.cbrt(m), np.cbrt(s)
    return np.stack([0.2104542553 * l_ + 0.7936177850 * m_ - 0.0040720468 * s_,
                     1.9779984951 * l_ - 2.4285922050 * m_ + 0.4505937099 * s_,
                     0.0259040371 * l_ + 0.7827717662 * m_ - 0.8086757660 * s_], axis=-1)


def from_oklab(lab):
    L, a, b = lab[..., 0], lab[..., 1], lab[..., 2]
    l_ = L + 0.3963377774 * a + 0.2158037573 * b
    m_ = L - 0.1055613458 * a - 0.0638541728 * b
    s_ = L - 0.0894841775 * a - 1.2914855480 * b
    l, m, s = l_ ** 3, m_ ** 3, s_ ** 3
    rgb = np.stack([4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s,
                    -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s,
                    -0.0041960863 * l - 0.7034186147 * m + 1.7076147010 * s], axis=-1)
    return lin_to_srgb(rgb)


def hexof(lab):
    c = from_oklab(np.asarray(lab, dtype=np.float64))
    return '#%02x%02x%02x' % tuple(int(round(float(v))) for v in c)


def hue_deg(a, b):
    return (np.degrees(np.arctan2(b, a)) + 360.0) % 360.0


def rnd(v, n=4):
    if v is None:
        return None
    if isinstance(v, (list, tuple)):
        return [rnd(x, n) for x in v]
    return round(float(v), n)

# ------------------------------------------------------------------------------------------ 測る


def kmeans_lab(X, k=8, iters=25, seed=20260930):
    """X (N,3) Oklab。初期値は L の分位点 (決定的)。戻り値 (中心, 割合) を L の順に"""
    if len(X) < k:
        return X.copy(), np.ones(len(X)) / max(1, len(X))
    rng = np.random.default_rng(seed)
    order = np.argsort(X[:, 0])
    C = X[order[np.linspace(0, len(X) - 1, k).astype(int)]].copy()
    C += rng.normal(0, 1e-4, C.shape)
    for _ in range(iters):
        d = ((X[:, None, :] - C[None, :, :]) ** 2).sum(-1)
        lab = d.argmin(1)
        newC = np.array([X[lab == i].mean(0) if (lab == i).any() else C[i] for i in range(k)])
        if np.allclose(newC, C, atol=1e-5):
            C = newC
            break
        C = newC
    d = ((X[:, None, :] - C[None, :, :]) ** 2).sum(-1)
    lab = d.argmin(1)
    frac = np.bincount(lab, minlength=k) / len(X)
    o = np.argsort(C[:, 0])
    return C[o], frac[o]


def colorfulness(rgb):
    R, G, B = rgb[:, 0], rgb[:, 1], rgb[:, 2]
    rg = R - G
    yb = 0.5 * (R + G) - B
    return float(math.sqrt(rg.std() ** 2 + yb.std() ** 2) + 0.3 * math.sqrt(rg.mean() ** 2 + yb.mean() ** 2))


def region_stats(lab, rgb=None):
    """lab (N,3) の要約"""
    if len(lab) < 50:
        return None
    L, a, b = lab[:, 0], lab[:, 1], lab[:, 2]
    C = np.hypot(a, b)
    ma, mb = float(a.mean()), float(b.mean())
    out = {'n': int(len(lab)), 'L_med': rnd(np.median(L)), 'L_mean': rnd(L.mean()), 'a': rnd(ma), 'b': rnd(mb),
           'C_med': rnd(np.median(C)), 'C_p90': rnd(np.percentile(C, 90)), 'tint_C': rnd(math.hypot(ma, mb)),
           'tint_h': rnd(hue_deg(ma, mb), 1), 'mean_hex': hexof([float(L.mean()), ma, mb])}
    if rgb is not None:
        out['colorfulness'] = rnd(colorfulness(rgb), 2)
    return out


def hue_shares(lab):
    C = np.hypot(lab[:, 1], lab[:, 2])
    m = C > C_MIN
    out = {'chromatic': rnd(m.mean())}
    if m.sum() == 0:
        for name, _, _ in HUE_BINS:
            out[name] = 0.0
        return out
    h = hue_deg(lab[m, 1], lab[m, 2])
    for name, h0, h1 in HUE_BINS:
        sel = ((h >= h0) | (h < h1)) if h0 > h1 else ((h >= h0) & (h < h1))
        out[name] = rnd(sel.mean())
    return out


def measure_pixels(img, stage_mask, char_mask):
    """img (H,W,3) 0〜255。stage_mask / char_mask (H,W) bool"""
    H, W = img.shape[:2]
    lab = to_oklab(img)
    r = {}
    st = stage_mask
    Ls = lab[st]
    r['stage'] = region_stats(Ls, img[st].reshape(-1, 3))
    # 明るさ別
    bands = {}
    for name, lo, hi in (('dark', -1, L_DARK), ('mid', L_DARK, L_BRIGHT), ('bright', L_BRIGHT, 2)):
        sel = (Ls[:, 0] >= lo) & (Ls[:, 0] < hi)
        s = region_stats(Ls[sel])
        if s:
            s['share'] = rnd(sel.mean())
        else:
            s = {'share': rnd(sel.mean())}
        bands[name] = s
    r['lbands'] = bands
    # 縦3帯
    vb = {}
    for name, y0, y1 in (('top', 0, 1 / 3), ('mid', 1 / 3, 2 / 3), ('bottom', 2 / 3, 1)):
        m = np.zeros((H, W), bool)
        m[int(H * y0):int(H * y1)] = True
        vb[name] = region_stats(lab[m & st])
    r['vbands'] = vb
    r['hues'] = hue_shares(Ls)
    # 色表 (2万画素を決まった間隔で間引く)
    step = max(1, len(Ls) // 20000)
    Cc, fr = kmeans_lab(Ls[::step], 8)
    r['palette'] = [{'hex': hexof(c), 'lab': rnd(c.tolist()), 'share': rnd(f, 3)} for c, f in zip(Cc, fr)]
    # キャラ
    if char_mask is not None and char_mask.sum() > 200:
        ch = region_stats(lab[char_mask], img[char_mask].reshape(-1, 3))
        r['chars'] = ch
        if ch and r['stage']:
            r['char_pop_C'] = rnd(ch['C_med'] / max(1e-4, r['stage']['C_med']), 3)
            r['char_pop_L'] = rnd(ch['L_med'] / max(1e-4, r['stage']['L_med']), 3)
    return r


def measure_ref(path, patch):
    rs = M.RefScene(path, patch)
    img = rs.normal
    ui = rs._ui
    ch = rs._ch
    r = measure_pixels(img, ~ui & ~ch, ch & ~ui)
    r['file'] = os.path.basename(path)
    r['title'] = patch.get('title')
    return r


def measure_scene(e):
    sc = M.Scene(e)
    img, uses_normal = sc.stage_img()
    ui = sc.ui_mask() if uses_normal else np.zeros(img.shape[:2], bool)
    ch = sc.char_mask()
    stage = ~ui & ~M.dilate(ch, 1)
    # キャラの色は通常の画 (UI を除く) で (hideui でも同じ画素)
    r = measure_pixels(img, stage, ch & ~ui)
    r['file'] = os.path.basename(e['normal'])
    r['stage_source'] = 'hideui' if not uses_normal else 'normal-ui'
    r['char_source'] = getattr(sc, 'char_source', None)
    return r

# ------------------------------------------------------------------------------------------ 比べる


def dE(p, q):
    if not p or not q:
        return None
    return rnd(100 * math.sqrt((p['L_mean'] - q['L_mean']) ** 2 + (p['a'] - q['a']) ** 2 + (p['b'] - q['b']) ** 2), 2)


def dE_chroma(p, q):
    """明るさを除いた色味だけの差 (a, b)"""
    if not p or not q:
        return None
    return rnd(100 * math.hypot(p['a'] - q['a'], p['b'] - q['b']), 2)


def palette_dist(P, Q):
    A = np.array([x['lab'] for x in P])
    B = np.array([x['lab'] for x in Q])
    wa = np.array([x['share'] for x in P])
    wb = np.array([x['share'] for x in Q])
    d = np.sqrt(((A[:, None, :] - B[None, :, :]) ** 2).sum(-1))
    return rnd(100 * 0.5 * ((d.min(1) * wa).sum() / wa.sum() + (d.min(0) * wb).sum() / wb.sum()), 2)


def compare(r, ref):
    out = {}
    out['dE_stage'] = dE(r['stage'], ref['stage'])
    out['dEab_stage'] = dE_chroma(r['stage'], ref['stage'])
    out['chroma_ratio'] = rnd(r['stage']['C_med'] / max(1e-4, ref['stage']['C_med']), 3)
    for k in ('dark', 'mid', 'bright'):
        out['dEab_' + k] = dE_chroma(r['lbands'].get(k), ref['lbands'].get(k))
        out['share_' + k] = rnd((r['lbands'][k].get('share') or 0) - (ref['lbands'][k].get('share') or 0), 3)
    for k in ('top', 'mid', 'bottom'):
        out['dE_v' + k] = dE(r['vbands'].get(k), ref['vbands'].get(k))
    hs = sum(abs((r['hues'].get(n) or 0) - (ref['hues'].get(n) or 0)) for n, _, _ in HUE_BINS)
    out['hue_dist'] = rnd(hs / 2, 3)
    out['palette_dist'] = palette_dist(r['palette'], ref['palette'])
    if r.get('char_pop_C') is not None and ref.get('char_pop_C') is not None:
        out['char_pop_C_ratio'] = rnd(r['char_pop_C'] / ref['char_pop_C'], 3)
    return out


def judge(cmp_, targets):
    """目安 (targets) に入るか。目安は門ではない (gates.json は動かさない)。数を数えるだけ"""
    j = {}
    for k, t in (targets or {}).items():
        v = cmp_.get(k)
        if v is None:
            j[k] = None
            continue
        ok = True
        if 'max' in t:
            ok &= v <= t['max']
        if 'min' in t:
            ok &= v >= t['min']
        j[k] = bool(ok)
    return j

# ------------------------------------------------------------------------------------------ 設計書


TARGETS = {
    # 本家の夜の森 (ot_921570_16) に対する差の目安。本家3枚の間の差 (夜の森と洞窟) を物差しの幅にした。
    'dEab_stage': {'max': 3.0, 'why': '舞台の平均の色味 (a, b) の差。本家の夜の森と洞窟の差がこの程度'},
    'chroma_ratio': {'min': 0.7, 'max': 1.3, 'why': '舞台の彩度の中央値が本家の 0.7〜1.3 倍'},
    'dEab_dark': {'max': 3.0, 'why': '暗部の色味 (本家は紺〜群青)'},
    'dEab_mid': {'max': 3.5, 'why': '中間の色味 (本家は青灰〜藤)'},
    'dEab_bright': {'max': 5.0, 'why': '明部の色味 (本家は霧と月光の淡い青白)'},
    'hue_dist': {'max': 0.35, 'why': '色相の割合の差 (0 = 同じ・1 = 全部違う)'},
    'palette_dist': {'max': 6.0, 'why': '色表どうしの距離 (ΔE)'},
    'dE_vtop': {'max': 8.0, 'why': '上の1/3 (空と樹冠と奥の霧) の平均の色'},
}


def design_md(refs, targets):
    lines = ['# 本家の色彩 (HD-2D 見本の色の設計書)', '',
             '2026-09-30 W3 の統合で作った (ユーザー「本家の色彩も参考にしてほしい」)。道具 `scripts/hd2d-color.py`。',
             '本家の画 (オクトラ1 の Steam の公式スクショ・`~/.cache/deck-rogue/hd2d-ref/`) は著作物なのでリポジトリに入れない。ここには測った数字と色の値だけを置く。',
             '色は Oklab (L = 明るさ 0〜1・a = 緑↔赤・b = 青↔黄・C = 彩度・h = 色相の角度)。舞台 = UI とキャラを除いた画素。', '']
    main = refs.get(MAIN_REF)
    if main:
        s = main['stage']
        lb = main['lbands']
        vb = main['vbands']
        hs = main['hues']
        lines += ['## 幕1 (坑口の森・夜) の目安 = 本家の夜の森の戦闘 (%s)' % MAIN_REF, '',
                  '- **舞台は青紫の夜**: 平均の色 %s (L %.2f・色味 C %.3f・h %.0f°)。彩度の中央値 C %.3f・上位10%% %.3f・色の豊かさ %.1f。' % (
                      s['mean_hex'], s['L_mean'], s['tint_C'], s['tint_h'], s['C_med'], s['C_p90'], s.get('colorfulness') or 0),
                  '- **明るさ別の色味** (割合・平均の色): 暗部 %.0f%% %s (h %.0f°) ／ 中間 %.0f%% %s (h %.0f°) ／ 明部 %.0f%% %s (h %.0f°)。' % (
                      100 * lb['dark']['share'], lb['dark'].get('mean_hex'), lb['dark'].get('tint_h') or 0,
                      100 * lb['mid']['share'], lb['mid'].get('mean_hex'), lb['mid'].get('tint_h') or 0,
                      100 * lb['bright']['share'], (lb['bright'] or {}).get('mean_hex', '—'), (lb['bright'] or {}).get('tint_h') or 0),
                  '- **縦3帯**: 上 %s (L %.2f) ／ 中 %s (L %.2f) ／ 下 %s (L %.2f)。' % (
                      vb['top']['mean_hex'], vb['top']['L_mean'], vb['mid']['mean_hex'], vb['mid']['L_mean'], vb['bottom']['mean_hex'], vb['bottom']['L_mean']),
                  '- **色相の割合** (彩度 %.2f 以上の画素 %.0f%% の中で): %s。' % (C_MIN, 100 * hs['chromatic'], '・'.join('%s %.0f%%' % (n, 100 * (hs.get(n) or 0)) for n, _, _ in HUE_BINS)),
                  '- **キャラは舞台より鮮やか**: キャラの彩度 ÷ 舞台の彩度 = %.2f 倍・明るさ %.2f 倍 (矩形なので背景を含む。下限の目安)。' % (main.get('char_pop_C') or 0, main.get('char_pop_L') or 0),
                  '- **色表** (Oklab の k-means 8色・暗い順・割合): ' + '・'.join('%s %.0f%%' % (p['hex'], 100 * p['share']) for p in main['palette']) + '。', '']
    lines += ['## 本家3枚の数字', '', '| 画 | 舞台の平均の色 | 彩度の中央値 | 暗部 | 中間 | 明部 | 上の1/3 | 緑 | 青緑 | 青 | 紫 | 赤橙 | キャラ÷舞台 (彩度) |', '|---|---|---|---|---|---|---|---|---|---|---|---|---|']
    for name, r in refs.items():
        s, lb, vb, hs = r['stage'], r['lbands'], r['vbands'], r['hues']
        lines.append('| %s %s | %s | %.3f | %s %.0f%% | %s %.0f%% | %s %.0f%% | %s | %.0f%% | %.0f%% | %.0f%% | %.0f%% | %.0f%% | %.2f |' % (
            name, r.get('title') or '', s['mean_hex'], s['C_med'],
            lb['dark'].get('mean_hex', '—'), 100 * lb['dark']['share'], lb['mid'].get('mean_hex', '—'), 100 * lb['mid']['share'],
            (lb['bright'] or {}).get('mean_hex', '—'), 100 * lb['bright']['share'], vb['top']['mean_hex'],
            100 * hs['緑'], 100 * hs['青緑'], 100 * hs['青'], 100 * hs['紫'], 100 * hs['赤橙'], r.get('char_pop_C') or 0))
    lines += ['', '## 寄せる目安 (本家の夜の森との差)', '',
              '門 (`gates.json`) ではない。W3 以降の色の詰め (P22 の舞台・P23 のキャラ) の物差し。`scripts/hd2d-color.py <撮影のフォルダ>` が数字と ○× を出す。', '',
              '| 数字 | 目安 | 理由 |', '|---|---|---|']
    for k, t in targets.items():
        rng_ = ('≦ %g' % t['max']) if 'max' in t and 'min' not in t else ('%g〜%g' % (t['min'], t['max']) if 'max' in t else '≧ %g' % t['min'])
        lines.append('| %s | %s | %s |' % (k, rng_, t['why']))
    lines += ['', '## 読み (色の作法)', '',
              '- 本家の夜は**舞台の色を1つの色相 (青〜青紫) にまとめ、彩度を落とす**。緑の草も茶の土も、夜の光で青灰に沈む (緑・赤橙の割合が小さい)。',
              '- **暗部は黒でなく紺** (暗部の平均の色味が青へ寄る)。明部は霧と月光の淡い青白で、暖色は小さな灯り (蛍・火の粉) だけ。',
              '- **キャラは舞台の色の外にいる**: 舞台の彩度が低いぶん、キャラの暖色と肌の色が浮く (キャラ÷舞台の彩度が 1 より大きい)。',
              '  → 舞台を寄せる時は舞台 (地面・崖・霧・環境光) の色を落とし、キャラは落とさない。後処理の彩度はキャラにも掛かるので控えめに。',
              '- 上の1/3は「暗い空」ではなく**光る霧**の青白 (上の1/3の明るさ ④ の門と同じ向き)。']
    return '\n'.join(lines) + '\n'


def cmp_md(res, design):
    targets = (design or {}).get('targets', TARGETS)
    lines = ['| 場面 | 舞台の平均の色 | 彩度 C (比) | ΔE 色味 舞台 | 暗部 | 中間 | 明部 | 上の1/3 ΔE | 色相の差 | 色表の差 | 緑 | 青緑 | 青 | キャラ÷舞台 | 目安の合格 |',
             '|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|']
    for k, r in res.items():
        if 'error' in r:
            continue
        c = r.get('cmp') or {}
        j = r.get('judge') or {}

        def m(key, fmt='%.1f'):
            v = c.get(key)
            if v is None:
                return '—'
            s = fmt % v
            if j.get(key) is True:
                return s + ' ✓'
            if j.get(key) is False:
                return s + ' ✗'
            return s
        passed = sum(1 for v in j.values() if v)
        lines.append('| %s | %s | %.3f (%s) | %s | %s | %s | %s | %s | %s | %s | %.0f%% | %.0f%% | %.0f%% | %s | %d/%d |' % (
            k, r['stage']['mean_hex'], r['stage']['C_med'], m('chroma_ratio', '%.2f'), m('dEab_stage'), m('dEab_dark'), m('dEab_mid'), m('dEab_bright'),
            m('dE_vtop'), m('hue_dist', '%.2f'), m('palette_dist'), 100 * r['hues']['緑'], 100 * r['hues']['青緑'], 100 * r['hues']['青'],
            ('%.2f' % r['char_pop_C']) if r.get('char_pop_C') is not None else '—', passed, len([v for v in j.values() if v is not None])))
    return '\n'.join(lines) + '\n'

# ------------------------------------------------------------------------------------------ シート


def font(sz, bold=False):
    try:
        return ImageFont.truetype(FONT_B if bold else FONT, sz)
    except Exception:
        return ImageFont.load_default()


def swatch_row(d, x, y, w, h, pal):
    cx = x
    for p in pal:
        pw = max(1, int(round(w * p['share'])))
        d.rectangle([cx, y, cx + pw, y + h], fill=p['hex'])
        cx += pw


def draw_column(canvas, x, y, W, name, img, r, cmp_=None, j=None):
    d = ImageDraw.Draw(canvas)
    f = font(26, True)
    f2 = font(20)
    d.text((x, y), name, fill=(232, 226, 210), font=f)
    y += 40
    if img is not None:
        im = img.resize((W, int(W * img.height / img.width)), Image.LANCZOS)
        canvas.paste(im, (x, y))
        y += im.height + 12
    # 色表
    d.text((x, y), '色表 (舞台・k-means 8色・割合の幅)', fill=(170, 166, 156), font=f2)
    y += 28
    swatch_row(d, x, y, W, 56, r['palette'])
    y += 66
    # 明るさ別の色味
    d.text((x, y), '明るさ別の平均の色 (暗部｜中間｜明部・幅 = 割合)', fill=(170, 166, 156), font=f2)
    y += 28
    cx = x
    for k in ('dark', 'mid', 'bright'):
        b = r['lbands'][k]
        pw = max(1, int(round(W * (b.get('share') or 0))))
        if b.get('mean_hex'):
            d.rectangle([cx, y, cx + pw, y + 56], fill=b['mean_hex'])
        cx += pw
    y += 66
    # 彩度を上げて色味だけ見る帯 (L を 0.6 にそろえ、色味を 3 倍)
    d.text((x, y), '色味だけ (明るさをそろえ色味を3倍: 暗部｜中間｜明部｜上｜中｜下)', fill=(170, 166, 156), font=f2)
    y += 28
    cells = [r['lbands'][k] for k in ('dark', 'mid', 'bright')] + [r['vbands'][k] for k in ('top', 'mid', 'bottom')]
    cw = W // 6
    for i, b in enumerate(cells):
        if b and b.get('a') is not None:
            hx = hexof([0.62, 3 * b['a'], 3 * b['b']])
            d.rectangle([x + i * cw, y, x + (i + 1) * cw - 4, y + 56], fill=hx)
    y += 66
    # 色相の割合
    d.text((x, y), '色相の割合 (彩度 %.2f 以上 %.0f%%)' % (C_MIN, 100 * r['hues']['chromatic']), fill=(170, 166, 156), font=f2)
    y += 28
    hue_col = {'赤橙': '#c8604a', '黄': '#c8b04a', '緑': '#5aa050', '青緑': '#3aa79b', '青': '#4a70c8', '紫': '#8a60c0'}
    cx = x
    for n, _, _ in HUE_BINS:
        pw = int(round(W * (r['hues'].get(n) or 0)))
        if pw > 0:
            d.rectangle([cx, y, cx + pw, y + 40], fill=hue_col[n])
            if pw > 50:
                d.text((cx + 6, y + 8), '%s %.0f%%' % (n, 100 * r['hues'][n]), fill=(250, 250, 250), font=font(18, True))
        cx += pw
    y += 52
    s = r['stage']
    txt = '舞台 %s・彩度 C %.3f (p90 %.3f)・豊かさ %.1f' % (s['mean_hex'], s['C_med'], s['C_p90'], s.get('colorfulness') or 0)
    d.text((x, y), txt, fill=(232, 226, 210), font=f2)
    y += 28
    if r.get('char_pop_C') is not None:
        d.text((x, y), 'キャラ÷舞台: 彩度 %.2f・明るさ %.2f' % (r['char_pop_C'], r.get('char_pop_L') or 0), fill=(232, 226, 210), font=f2)
        y += 28
    if cmp_:
        ok = (110, 200, 120)
        ng = (235, 110, 80)
        items = [('色味ΔE', 'dEab_stage', '%.1f'), ('彩度比', 'chroma_ratio', '%.2f'), ('暗部', 'dEab_dark', '%.1f'), ('中間', 'dEab_mid', '%.1f'),
                 ('明部', 'dEab_bright', '%.1f'), ('上1/3', 'dE_vtop', '%.1f'), ('色相', 'hue_dist', '%.2f'), ('色表', 'palette_dist', '%.1f')]
        cx = x
        for lab_, key, fm in items:
            v = cmp_.get(key)
            t = '%s %s' % (lab_, (fm % v) if v is not None else '—')
            col = ok if (j or {}).get(key) else (ng if (j or {}).get(key) is False else (170, 166, 156))
            tw = d.textlength(t, font=f2) + 18
            if cx + tw > x + W:
                cx = x
                y += 28
            d.text((cx, y), t, fill=col, font=f2)
            cx += tw
        y += 30
    return y


def make_sheet(out, sets, scene, design, refdir, patches):
    cols = []
    main = design['refs'].get(MAIN_REF) if design else None
    p = os.path.join(refdir, MAIN_REF + '.jpg')
    if main and os.path.exists(p):
        cols.append(('本家 夜の森 (%s)' % MAIN_REF, Image.open(p).convert('RGB'), main, None, None))
    for name, d in sets:
        dev, sc = scene.split('-', 1)
        found = M.scan_dir(d, 1, '^' + re.escape(scene) + '$')
        e = found.get(scene)
        if not e:
            continue
        r = measure_scene(e)
        c = compare(r, main) if main else None
        j = judge(c, design.get('targets')) if c else None
        cols.append((name, Image.open(e['normal']).convert('RGB'), r, c, j))
    W = 960
    gap = 40
    H = 60 + 40 + int(W * 1080 / 1920) + 12 + 7 * 94 + 140
    canvas = Image.new('RGB', (gap + len(cols) * (W + gap), H), (22, 24, 34))
    dd = ImageDraw.Draw(canvas)
    dd.text((gap, 14), '09 色彩: 本家と並べる (%s・Oklab。数字は scripts/hd2d-color.py・目安は docs/design/hd2d-slice/honke-color.json)' % scene, fill=(232, 226, 210), font=font(28, True))
    ymax = 0
    for i, (name, img, r, c, j) in enumerate(cols):
        ymax = max(ymax, draw_column(canvas, gap + i * (W + gap), 60, W, name, img, r, c, j))
    canvas = canvas.crop((0, 0, canvas.width, ymax + 20))
    os.makedirs(os.path.dirname(os.path.abspath(out)), exist_ok=True)
    canvas.save(out)
    return out

# ------------------------------------------------------------------------------------------


def main():
    ap = argparse.ArgumentParser(description='HD-2D 見本の色彩の物差し')
    ap.add_argument('dir', nargs='?', help='撮影のフォルダ')
    ap.add_argument('--ref', nargs='?', const=DEFAULT_REF, help='本家の画のフォルダ')
    ap.add_argument('--write-design', help='本家3枚から色の設計書 (json) を書く')
    ap.add_argument('--design', default=DEFAULT_DESIGN, help='色の設計書 (比べる目安)')
    ap.add_argument('--md', help='表の md')
    ap.add_argument('--out', help='数字の json')
    ap.add_argument('--only', help='場面の名前の正規表現')
    ap.add_argument('--sheet', help='色の比較シート (png) を書く')
    ap.add_argument('--set', action='append', default=[], help='シートの組 名前=フォルダ')
    ap.add_argument('--scene', default='PC-S-wolf')
    a = ap.parse_args()
    patches = M.REF_PATCHES
    if a.ref:
        refs = {}
        for name, patch in patches.items():
            p = next((os.path.join(a.ref, name + e) for e in ('.jpg', '.png') if os.path.exists(os.path.join(a.ref, name + e))), None)
            if p:
                refs[name] = measure_ref(p, patch)
        design = {'schema': 'hd2d-color/1', 'made': '2026-09-30', 'tool': 'scripts/hd2d-color.py --ref --write-design',
                  'mainRef': MAIN_REF, 'space': 'Oklab (L 0〜1・a・b・C = hypot(a,b)・h 度)',
                  'bands': {'dark': 'L<%.2f' % L_DARK, 'bright': 'L≥%.2f' % L_BRIGHT, 'chromaMin': C_MIN,
                            'hues': {n: [h0, h1] for n, h0, h1 in HUE_BINS}},
                  'refs': refs, 'targets': TARGETS}
        if a.write_design:
            os.makedirs(os.path.dirname(os.path.abspath(a.write_design)), exist_ok=True)
            # 手で書いた設計 (json の design キー・md の §0〜§6。2026-09-30 同日) は消さない: design は引き継ぎ、手書きの md は別名 (.auto.md) に出す
            if os.path.exists(a.write_design):
                try:
                    with open(a.write_design, encoding='utf-8') as f:
                        old = json.load(f)
                    for k in old:
                        if k not in design:
                            design[k] = old[k]
                except Exception:
                    pass
            with open(a.write_design, 'w', encoding='utf-8') as f:
                json.dump(design, f, ensure_ascii=False, indent=1)
            print('色の設計書を書いた: %s' % a.write_design)
            if a.md:
                out_md = a.md
                if os.path.exists(out_md):
                    with open(out_md, encoding='utf-8') as f:
                        if '## 0. 結論' in f.read():
                            out_md = out_md[:-3] + '.auto.md' if out_md.endswith('.md') else out_md + '.auto'
                            print('手で書いた md があるので別名に出す: %s' % out_md)
                with open(out_md, 'w', encoding='utf-8') as f:
                    f.write(design_md(refs, TARGETS))
                print('md: %s' % out_md)
        else:
            print(json.dumps(refs, ensure_ascii=False, indent=1))
        return 0
    design = None
    if a.design and os.path.exists(a.design):
        with open(a.design, encoding='utf-8') as f:
            design = json.load(f)
    if a.sheet:
        sets = [tuple(s.split('=', 1)) for s in a.set]
        print(make_sheet(a.sheet, sets, a.scene, design, DEFAULT_REF, patches))
        return 0
    if not a.dir:
        ap.error('撮影のフォルダか --ref か --sheet が要る')
    res = {}
    main_ref = (design or {}).get('refs', {}).get(MAIN_REF)
    for key, e in sorted(M.scan_dir(a.dir, 1, a.only).items()):
        try:
            r = measure_scene(e)
            if main_ref:
                r['cmp'] = compare(r, main_ref)
                r['judge'] = judge(r['cmp'], design.get('targets'))
            res[key] = r
        except Exception as ex:
            res[key] = {'error': '%s: %s' % (type(ex).__name__, ex)}
    if a.out:
        with open(a.out, 'w', encoding='utf-8') as f:
            json.dump(res, f, ensure_ascii=False, indent=1)
    md = cmp_md(res, design)
    if a.md:
        with open(a.md, 'w', encoding='utf-8') as f:
            f.write(md)
    print(md)
    for k, r in res.items():
        if 'error' in r:
            print('測れなかった: %s %s' % (k, r['error']), file=sys.stderr)
    return 0


if __name__ == '__main__':
    sys.exit(main())

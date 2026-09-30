#!/usr/bin/env python3
"""HD-2D 見本の比較シート (2026-09-30 P08。計画 docs/design/hd2d-slice-plan-2026-09-30.md §6)。

撮影のフォルダ (scripts/hd2d-shots.sh の置き場) と本家の画 (scripts/hd2d-fetch-ref.py の置き場) を並べて PNG のシートを作る。
数字は scripts/hd2d-measure.py を読み込んで同じ物差しで測り、門 (--gates) があれば合格を緑・不合格を朱で書く。
出力は既定で unity/Shots/hd2d/compare/ (git の管理外。本家の画を含むのでリポジトリには入れない。SendUserFile で送る)。

使い方 (組 = 名前=フォルダ。フォルダの中の名前は <PC|PH>-<場面>-<k>.png)
  scripts/hd2d-compare.py all --set 今=unity/Shots/hd2d/w0 --set Linear版の今=unity/Shots/hd2d/w2-old --set 見本62=unity/Shots/hd2d/w2-slice \
        [--set 見本48=…] [--variants <dir>] [--regress-old <dir> --regress-new <dir>] [--ref ~/.cache/deck-rogue/hd2d-ref] [--gates gates.json] [--dev PC|PH]
  scripts/hd2d-compare.py overview|crops|hero|camera|light|paper|regress|bands (同じ引数。1枚だけ)
  --crops <json> = 1:1 の切り抜きの場所を差し替える ({"主人公": "player" か [x,y,w,h] の割合, …}。組ごとに {"組名": {…}} も可)

シート (§6)
  01 総覧: 本家｜今｜Linear版の今｜見本 × UI あり／UI なし。各画の下に①〜⑩ (合格 = 緑・不合格 = 朱)
  02 1:1 の切り抜き: 主人公・崖・幹・奥の段・手前の額縁 (本家は主人公・地面・奥)
  03 主人公: 62 と 48 を S-ogre・S-wolf で。ボスとの背丈の比・通常の敵との比・座席での1ドット
  04 カメラ: cam 36／28／22 (22 は見下ろし 10° と 12°) × 1〜4体 (variants の V-cam*)
  05 光: receive・keycolor・herolift・charshadow (variants)
  06 紙: ui=night／paper × 敵1〜4体と人形9体 (variants。layout-check の違反の数を添える)
  07 演出: R1〜R15 を old (上) と見本 (下) で並べた連続写真。板と矩形のずれの最大 (layout.json の stage.unitBoxes)
  08 帯の明るさ: 横5帯の明るさの折れ線 (本家・今・見本)
"""
import argparse
import glob
import importlib.util
import json
import math
import os
import re
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont

sys.dont_write_bytecode = True   # 読み込む hd2d-measure.py などの .pyc を scripts/ に残さない

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(HERE)
FONT = os.path.join(REPO, 'unity', 'Assets', 'Resources', 'Fonts', 'NotoSansJP-Regular.otf')
FONT_B = os.path.join(REPO, 'unity', 'Assets', 'Resources', 'Fonts', 'NotoSansJP-Bold.otf')
DEFAULT_OUT = os.path.join(REPO, 'unity', 'Shots', 'hd2d', 'compare')
DEFAULT_REF = os.path.expanduser('~/.cache/deck-rogue/hd2d-ref')

BG = (22, 24, 34)
INK = (232, 226, 210)
INK2 = (170, 166, 156)
OKC = (110, 200, 120)
NGC = (235, 110, 80)
LINE = (70, 72, 88)


def load_mod(name, file):
    spec = importlib.util.spec_from_file_location(name, os.path.join(HERE, file))
    m = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    return m


M = load_mod('hd2d_measure', 'hd2d-measure.py')
LC = load_mod('hd2d_layout_check', 'hd2d-layout-check.py')


def font(sz, bold=False):
    try:
        return ImageFont.truetype(FONT_B if bold else FONT, sz)
    except Exception:
        return ImageFont.load_default()


def fit(im, w):
    if im.width == w:
        return im
    h = max(1, round(im.height * w / im.width))
    return im.resize((w, h), Image.LANCZOS)


def find(d, dev, scene, var='', k=1):
    if not d:
        return None
    name = '%s-%s%s-%d.png' % (dev, scene, ('-' + var) if var else '', k)
    p = os.path.join(d, name)
    return p if os.path.exists(p) else None


def ref_path(refdir, name='ot_921570_16'):
    for ext in ('.jpg', '.png'):
        p = os.path.join(refdir or '', name + ext)
        if os.path.exists(p):
            return p
    return None


def measure_one(d, dev, scene):
    """場面1つを hd2d-measure と同じ物差しで"""
    p = find(d, dev, scene)
    if not p:
        return None
    e = {'normal': p, 'dev': dev, 'scene': scene, 'key': dev + '-' + scene}
    for v in M.VARIANTS:
        q = find(d, dev, scene, v)
        if q:
            e[v] = q
    lj = p[:-4] + '.layout.json'
    if os.path.exists(lj):
        e['layout'] = lj
    lg = os.path.join(d, dev + '-' + scene + '.log')
    if os.path.exists(lg):
        e['log'] = lg
    try:
        return M.Scene(e).measure()
    except Exception as ex:
        return {'error': str(ex)}


def measure_ref(refdir, name):
    p = ref_path(refdir, name)
    if not p:
        return None
    return M.RefScene(p, M.REF_PATCHES.get(name, {})).measure()


METRIC_LINES = [('m1', '①', 'f1'), ('m2', '②', 'pct'), ('m3', '③', 'f2'), ('m4', '④', 'f1'), ('m5', '⑤', 'int'),
                ('m6', '⑥', 'f2'), ('m7', '⑦', 'pct'), ('m8', '⑧', None), ('m9', '⑨', None), ('m10', '⑩', None)]


def metric_tokens(r, gates):
    """[(文字, 色)] = 数字を1つずつ。門があれば合否で色"""
    if not r or 'error' in r:
        return [('測れない', NGC)]
    j = M.judge(r, gates) if gates else {}
    out = []
    for key, lab, kind in METRIC_LINES:
        if key == 'm8':
            u = r.get('m8_units') or []
            v = ('%d/%d' % (sum(1 for x in u if x.get('ok')), len(u))) if u else '-'
        elif key == 'm9':
            v = '-' if r.get('m9_fs') is None else '%.0fpx %.1f:1' % (r['m9_fs'], r.get('m9_contrast') or 0)
        elif key == 'm10':
            v = '-' if r.get('m10_min') is None else '%.2f' % r['m10_min']
        else:
            v = M.fmt(r.get(key), kind)
        col = INK
        if key in j and j[key] is not None:
            col = OKC if j[key] else NGC
        out.append(('%s %s' % (lab, v), col))
    return out


def draw_tokens(d, x, y, toks, w, fnt, gap=14):
    cx, cy = x, y
    lh = fnt.size + 6
    for t, c in toks:
        tw = d.textlength(t, font=fnt)
        if cx + tw > x + w and cx > x:
            cx = x
            cy += lh
        d.text((cx, cy), t, fill=c, font=fnt)
        cx += tw + gap
    return cy + lh


def sheet(cells, cols, cell_w, title, out, caption_h=110, img_h=None):
    """cells = [(画像 or None, 見出し, [(文字, 色)])]。cols 列に並べる"""
    rows = math.ceil(len(cells) / cols) if cells else 1
    if img_h is None:
        img_h = round(cell_w * 1080 / 1920)
    pad = 16
    head_h = 56
    W = pad + cols * (cell_w + pad)
    H = head_h + rows * (img_h + caption_h + pad) + pad
    sh = Image.new('RGB', (W, H), BG)
    d = ImageDraw.Draw(sh)
    d.text((pad, 14), title, fill=INK, font=font(26, True))
    f1, f2 = font(17, True), font(15)
    for i, (im, lab, toks) in enumerate(cells):
        cx = pad + (i % cols) * (cell_w + pad)
        cy = head_h + (i // cols) * (img_h + caption_h + pad)
        if im is not None:
            t = fit(im.convert('RGB'), cell_w)
            if t.height > img_h:
                t = t.crop((0, 0, cell_w, img_h))
            sh.paste(t, (cx, cy))
        else:
            d.rectangle([cx, cy, cx + cell_w, cy + img_h], outline=LINE)
            d.text((cx + 12, cy + 12), '(無い)', fill=INK2, font=f2)
        d.text((cx, cy + img_h + 6), lab, fill=INK, font=f1)
        draw_tokens(d, cx, cy + img_h + 32, toks or [], cell_w, f2)
    os.makedirs(os.path.dirname(out), exist_ok=True)
    sh.save(out)
    print('シート: %s' % out)
    return out


def open_img(p):
    return Image.open(p) if p else None

# ------------------------------------------------------------------------------------------ 01 総覧


def s_overview(a, gates, out):
    cells = []
    sets = a.sets
    scene = a.scene
    for var, vlab in (('', 'UI あり'), ('hideui', 'UI なし')):
        rp = ref_path(a.ref, 'ot_921570_16')
        rr = measure_ref(a.ref, 'ot_921570_16') if rp else None
        im = open_img(rp)
        if im is not None and var == 'hideui':
            im = Image.blend(im.convert('RGB'), Image.new('RGB', im.size, (0, 0, 0)), 0.35)   # 本家に UI なしの画は無い = 薄くして同じ画
        cells.append((im, '本家 ot_921570_16 (%s)' % ('UI あり' if not var else '同じ画・UI なしは無い'), metric_tokens(rr, None) if not var else []))
        for name, d in sets:
            p = find(d, a.dev, scene, var)
            r = measure_one(d, a.dev, scene) if not var else None
            cells.append((open_img(p), '%s %s-%s (%s)' % (name, a.dev, scene, vlab), metric_tokens(r, gates) if r else []))
    return sheet(cells, 1 + len(sets), 560, '01 総覧 (%s・%s)  数字は hd2d-measure の①〜⑩%s' % (a.dev, scene, '・緑=合格 朱=不合格' if gates else ''), out, caption_h=96)

# ------------------------------------------------------------------------------------------ 02 1:1 の切り抜き

DEFAULT_CROPS = {   # 画の大きさに対する割合 [x, y, w, h]。"player" = 主人公の絵の矩形の周り (layout.json)
    '主人公': 'player',
    '崖': [0.03, 0.30, 0.16, 0.20],
    '幹': [0.30, 0.06, 0.12, 0.26],
    '奥の段': [0.52, 0.14, 0.20, 0.16],
    '手前の額縁': [0.00, 0.62, 0.16, 0.22],
}


def crop_box(d, dev, scene, spec, size):
    W, H = size
    if spec == 'player':
        p = find(d, dev, scene)
        lj = p[:-4] + '.layout.json' if p else None
        if lj and os.path.exists(lj):
            lay = M.load_layout(lj)
            for u in M.lay_units(lay):
                if u.get('kind') == 'player' and u.get('sprite'):
                    x, y, w, h = u['sprite']
                    cx, cy = x + w / 2, y + h / 2
                    s = max(w, h) * 1.25
                    return [int(cx - s / 2), int(cy - s / 2), int(s), int(s)]
        return [int(W * 0.12), int(H * 0.30), int(H * 0.36), int(H * 0.36)]
    x, y, w, h = spec
    return [int(x * W), int(y * H), int(w * W), int(h * H)]


def s_crops(a, gates, out):
    crops = DEFAULT_CROPS
    per = {}
    if a.crops:
        with open(a.crops, encoding='utf-8') as f:
            c = json.load(f)
        if all(isinstance(v, dict) for v in c.values()):
            per = c
        else:
            crops = c
    cells = []
    cols = 0
    rp = ref_path(a.ref, 'ot_921570_16')
    for place, spec in crops.items():
        row = []
        if rp:
            im = Image.open(rp).convert('RGB')
            patch = M.REF_PATCHES['ot_921570_16']
            which = {'主人公': 'hero', '手前の額縁': 'ground'}.get(place, 'bg')   # 本家は主人公・地面・奥 (ref-patches の矩形) の3か所
            x, y, w, h = patch[which]
            s = max(w, h, 300)
            cx, cy = x + w / 2, y + h / 2
            row.append((im.crop((int(cx - s / 2), int(cy - s / 2), int(cx + s / 2), int(cy + s / 2))),
                        '本家 %s' % {'hero': '主人公', 'ground': '地面', 'bg': '奥'}[which], []))
        for name, d in a.sets:
            p = find(d, a.dev, a.scene, 'hideui') or find(d, a.dev, a.scene)
            if not p:
                row.append((None, '%s %s' % (name, place), []))
                continue
            im = Image.open(p).convert('RGB')
            sp = per.get(name, {}).get(place, spec)
            x, y, w, h = crop_box(d, a.dev, a.scene, sp, im.size)
            row.append((im.crop((x, y, x + w, y + h)), '%s %s (%d×%d・1:1)' % (name, place, w, h), []))
        cols = max(cols, len(row))
        cells += row
    # 1:1 で見せる = 列の幅を切り抜きの最大に合わせる
    cw = max((c[0].width for c in cells if c[0] is not None), default=300)
    ch = max((c[0].height for c in cells if c[0] is not None), default=300)
    cw = min(cw, 520)
    fixed = []
    for im, lab, toks in cells:
        if im is not None and (im.width > cw or im.height > ch):
            im = im.crop((0, 0, min(im.width, cw), min(im.height, ch)))
        if im is not None:
            canvas = Image.new('RGB', (cw, ch), BG)
            canvas.paste(im, (0, 0))
            im = canvas
        fixed.append((im, lab, toks))
    return sheet(fixed, cols, cw, '02 1:1 の切り抜き (%s・%s・UI なしの画。拡大も縮小もしない)' % (a.dev, a.scene), out, caption_h=40, img_h=ch)

# ------------------------------------------------------------------------------------------ 03 主人公


def opaque_dots_h(kind, uid, flags=None):
    folder = {'enemy': 'enemies', 'player': 'leaders', 'doll': 'dolls'}.get(kind)
    cands = []
    if kind == 'player' and flags and str(flags.get('herodots', '')) == '48':
        cands.append(uid + '_48')
    cands.append(uid)
    for c in cands:
        p = os.path.join(M.ART, folder, c + '.png')
        if os.path.exists(p):
            im = Image.open(p).convert('RGBA')
            bb = im.getchannel('A').point(lambda v: 255 if v > 0 else 0).getbbox()
            return (bb[3] - bb[1]) if bb else im.height
    return None


def hero_stats(d, dev, scene):
    p = find(d, dev, scene)
    if not p:
        return None
    lj = p[:-4] + '.layout.json'
    if not os.path.exists(lj):
        return None
    lay = M.load_layout(lj)
    flags = lay.get('flags') or {}
    out = {}
    for u in M.lay_units(lay):
        if not u.get('sprite') or not u.get('alive', True):
            continue
        dots = M.art_dots(u.get('kind'), u.get('id'), flags, dev == 'PH')
        hd = opaque_dots_h(u.get('kind'), u.get('id'), flags)
        if not dots or not hd:
            continue
        ppd = u['sprite'][2] / dots
        out[u['key']] = {'kind': u['kind'], 'id': u.get('id'), 'pxPerDot': ppd, 'heightPx': hd * ppd, 'heightDots': hd}
    return out


def s_hero(a, gates, out):
    cells = []
    for scene in ('S-ogre', 'S-wolf'):
        for name, d in a.sets:
            p = find(d, a.dev, scene)
            st = hero_stats(d, a.dev, scene) if p else None
            toks = []
            if st and 'player' in st:
                hp = st['player']
                toks.append(('主人公 %d ドット・%.0fpx・1ドット %.2fpx' % (hp['heightDots'], hp['heightPx'], hp['pxPerDot']), INK))
                for k, v in st.items():
                    if v['kind'] == 'enemy':
                        toks.append(('%s÷主人公 %.2f 倍' % (v['id'], v['heightPx'] / max(1.0, hp['heightPx'])), INK2))
            elif p:
                toks.append(('layout.json が無い (dumplayout=1 で撮る)', INK2))
            cells.append((open_img(p), '%s %s-%s' % (name, a.dev, scene), toks))
            crop = None
            if p and st and 'player' in st:
                im = Image.open(p).convert('RGB')
                lay = M.load_layout(p[:-4] + '.layout.json')
                sp = [u for u in M.lay_units(lay) if u.get('kind') == 'player'][0]['sprite']
                cx, cy = sp[0] + sp[2] / 2, sp[1] + sp[3] / 2
                crop = im.crop((int(cx - 200), int(cy - 200), int(cx + 200), int(cy + 200)))   # 400×400 を 1:1 (拡大しない)
            cells.append((crop, '%s 主人公の周り 400×400 (1:1)' % name, []))
    return sheet(cells, 2 * max(1, len(a.sets)), 400, '03 主人公の大きさ (62 と 48。背丈 = 絵の不透明な部分のドット × 座席での1ドット)', out, caption_h=110, img_h=400)

# ------------------------------------------------------------------------------------------ 04〜06 変種の格子


def variant_cells(d, dev, names, gates, with_lc=False):
    cells = []
    for nm in names:
        p = find(d, dev, nm)
        r = measure_one(d, dev, nm) if p else None
        toks = metric_tokens(r, gates) if r else []
        if with_lc and p:
            lj = p[:-4] + '.layout.json'
            if os.path.exists(lj):
                V, _ = LC.check(M.load_layout(lj))
                toks = [('配置の違反 %d' % len(V), OKC if not V else NGC)] + toks
        cells.append((open_img(p), '%s-%s' % (dev, nm), toks))
    return cells


def names_in(d, dev, rx):
    out = []
    for p in sorted(glob.glob(os.path.join(d or '', '%s-*-1.png' % dev))):
        n = os.path.basename(p)[len(dev) + 1:-6]
        if re.search(r'-(hideui|uionly|unitsonly)$', n):
            continue
        if re.search(rx, n):
            out.append(n)
    return out


def s_grid(a, gates, out, rx, title, cols=4, with_lc=False):
    d = a.variants
    if not d:
        print('変種のフォルダ (--variants) が無いので %s は作らない' % title)
        return None
    names = names_in(d, a.dev, rx)
    if not names:
        print('%s: 名前が %s の撮影が無い' % (title, rx))
        return None
    return sheet(variant_cells(d, a.dev, names, gates, with_lc), cols, 460, title, out, caption_h=110)

# ------------------------------------------------------------------------------------------ 07 演出


def unitbox_dev(lj):
    if not os.path.exists(lj):
        return None
    lay = M.load_layout(lj)
    ub = (lay.get('stage') or {}).get('unitBoxes')
    if not ub:
        return None
    ent = ub if isinstance(ub, list) else [dict(v, key=k) for k, v in ub.items()]
    best = 0.0
    for e in ent:
        if not isinstance(e, dict):
            continue
        dv = e.get('dev')
        if dv is None:
            r, b = e.get('rectPx'), e.get('boardPx')
            if r and b:
                dv = max(abs(r[i] - b[i]) for i in range(4))
        if isinstance(dv, (int, float)):
            best = max(best, dv)
    return best


def s_regress(a, gates, out_dir):
    if not (a.regress_old and a.regress_new):
        print('演出のシートには --regress-old と --regress-new が要る')
        return []
    outs = []
    rows = sorted({re.sub(r'-\d+\.png$', '', os.path.basename(p))[len(a.dev) + 1:] for p in glob.glob(os.path.join(a.regress_new, '%s-R*-1.png' % a.dev))})
    for row in rows:
        cells = []
        n = 1
        while find(a.regress_new, a.dev, row, '', n + 1) or find(a.regress_old, a.dev, row, '', n + 1):
            n += 1
        worst = 0.0
        for lab, d in (('old', a.regress_old), ('見本', a.regress_new)):
            for k in range(1, n + 1):
                p = find(d, a.dev, row, '', k)
                dv = unitbox_dev(p[:-4] + '.layout.json') if p else None
                if dv is not None and lab == '見本':
                    worst = max(worst, dv)
                cells.append((open_img(p), '%s %d' % (lab, k), [('ずれ %.1fpx' % dv, OKC if dv <= 2 else NGC)] if dv is not None else []))
        out = os.path.join(out_dir, '07-regress-%s-%s.png' % (a.dev, row))
        outs.append(sheet(cells, n, 300, '07 演出 %s (上 = old・下 = 見本。板と矩形のずれの最大 %.1fpx)' % (row, worst), out, caption_h=48))
    return outs

# ------------------------------------------------------------------------------------------ 08 帯の明るさ


def band_means(p, n=5):
    a = M.load_rgb(p)
    L = M.lum(a)
    H = L.shape[0]
    return [float(L[int(H * i / n):int(H * (i + 1) / n)].mean()) for i in range(n)]


def s_bands(a, gates, out):
    series = []
    rp = ref_path(a.ref, 'ot_921570_16')
    if rp:
        series.append(('本家 ot_921570_16', band_means(rp), (240, 240, 240)))
    pal = [(235, 110, 80), (110, 200, 120), (120, 170, 250), (240, 200, 90)]
    for i, (name, d) in enumerate(a.sets):
        p = find(d, a.dev, a.scene, 'hideui') or find(d, a.dev, a.scene)
        if p:
            series.append(('%s (%s)' % (name, 'UI なし' if 'hideui' in p else 'UI あり'), band_means(p), pal[i % len(pal)]))
    W, H = 1100, 620
    im = Image.new('RGB', (W, H), BG)
    d = ImageDraw.Draw(im)
    d.text((20, 14), '08 横5帯の明るさ (上→下。%s・%s)' % (a.dev, a.scene), fill=INK, font=font(24, True))
    x0, y0, x1, y1 = 90, 80, W - 300, H - 70
    vmax = max([max(s[1]) for s in series] + [60.0]) * 1.1
    for v in range(0, int(vmax) + 1, 20):
        y = y1 - (y1 - y0) * v / vmax
        d.line([(x0, y), (x1, y)], fill=LINE)
        d.text((x0 - 50, y - 10), '%d' % v, fill=INK2, font=font(15))
    for i in range(5):
        x = x0 + (x1 - x0) * i / 4
        d.text((x - 24, y1 + 12), '帯%d' % (i + 1), fill=INK2, font=font(15))
    for k, (lab, vals, col) in enumerate(series):
        pts = [(x0 + (x1 - x0) * i / 4, y1 - (y1 - y0) * v / vmax) for i, v in enumerate(vals)]
        d.line(pts, fill=col, width=3)
        for x, y in pts:
            d.ellipse([x - 5, y - 5, x + 5, y + 5], fill=col)
        d.text((x1 + 20, y0 + k * 30), lab, fill=col, font=font(16))
    os.makedirs(os.path.dirname(out), exist_ok=True)
    im.save(out)
    print('シート: %s' % out)
    return out

# ------------------------------------------------------------------------------------------ 入口


def main():
    ap = argparse.ArgumentParser(description='HD-2D 見本の比較シート')
    ap.add_argument('what', choices=['all', 'overview', 'crops', 'hero', 'camera', 'light', 'paper', 'regress', 'bands'])
    ap.add_argument('--set', action='append', default=[], help='組名=フォルダ (並べる順)')
    ap.add_argument('--ref', default=DEFAULT_REF)
    ap.add_argument('--gates')
    ap.add_argument('--dev', default='PC', choices=['PC', 'PH'])
    ap.add_argument('--scene', default='S-wolf')
    ap.add_argument('--variants', help='variants.txt で撮ったフォルダ')
    ap.add_argument('--regress-old')
    ap.add_argument('--regress-new')
    ap.add_argument('--crops')
    ap.add_argument('--out', default=DEFAULT_OUT)
    a = ap.parse_args()
    a.sets = []
    for s in a.set:
        if '=' not in s:
            ap.error('--set は 組名=フォルダ')
        k, v = s.split('=', 1)
        a.sets.append((k, v))
    gates = None
    if a.gates:
        with open(a.gates, encoding='utf-8') as f:
            gates = json.load(f)
    o = a.out
    tag = '%s-%s' % (a.dev, a.scene)
    todo = [a.what] if a.what != 'all' else ['overview', 'crops', 'hero', 'camera', 'light', 'paper', 'regress', 'bands']
    made = []
    for w in todo:
        if w == 'overview':
            made.append(s_overview(a, gates, os.path.join(o, '01-overview-%s.png' % tag)))
        elif w == 'crops':
            made.append(s_crops(a, gates, os.path.join(o, '02-crops-%s.png' % tag)))
        elif w == 'hero':
            made.append(s_hero(a, gates, os.path.join(o, '03-hero-%s.png' % a.dev)))
        elif w == 'camera':
            made.append(s_grid(a, gates, os.path.join(o, '04-camera-%s.png' % a.dev), r'^V-cam', '04 カメラ (cam 36／28／22・22 は見下ろし 10° と 12°)'))
        elif w == 'light':
            made.append(s_grid(a, gates, os.path.join(o, '05-light-%s.png' % a.dev), r'^V-(recv|key|lift|shadow)', '05 光 (receive・keycolor・herolift・charshadow)'))
        elif w == 'paper':
            made.append(s_grid(a, gates, os.path.join(o, '06-paper-%s.png' % a.dev), r'^V-(ui|ledger)', '06 紙 (夜色／紙・帳面の置き方)。配置の違反 = hd2d-layout-check', 5, True))
        elif w == 'regress':
            made += s_regress(a, gates, o)
        elif w == 'bands':
            made.append(s_bands(a, gates, os.path.join(o, '08-bands-%s.png' % tag)))
    made = [m for m in made if m]
    print('作った: %d 枚 (%s)' % (len(made), o))
    return 0


if __name__ == '__main__':
    sys.exit(main())

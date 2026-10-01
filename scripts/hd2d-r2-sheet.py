#!/usr/bin/env python3
"""HD-2D 見本 二周目の比較シート (2026-10-01 レーン F)。

段1 = trial: 試しのビルド (scripts/hd2d-states/r2-trial.txt の12枚) を、本家 ot16 と W5 の見本と同じ大きさで並べる。
  各画に 霧の帯の頂点の行 (金の線)・N1 の目標の帯 (緑の帯。主人公の足元から上の距離)・足元の行 (水色の短い線)・地平線 (点線) を重ね、
  N1 (帯の位置)・N2 (太さ)・N3 (頂点の明るさ)・N8 (キャラの後ろ÷頂点)・N22 (奥の端の段差)・N6a (上の真ん中) を合否つきで書く。
  下に中央の列の縦の明るさの折れ線 (横軸 = 主人公の足元から上の距離。スマホ相当は1ドットの比 sc で PC の px にそろえる)。
  数字は scripts/hd2d-r2-targets.py (同じ物差し)・目標は docs/design/hd2d-slice/r2-targets.json。
段2 = final: 本番の撮影 (scripts/hd2d-states/r2-*.txt を pshots.sh で撮った <shots>/r2-slice・r2-cam・r2-look・r2-regress*) を
  本家 (ot16・ot7・ot2)｜W5 の見本｜二周目 で同じ大きさに並べる (主役はオーガ)。final/final-sheets.py (W5) と同じ書き方 (日本語の見出し・同じ幅)。
  00-gates     門①〜⑩ (hd2d-measure.py・gates.json) の表: 本家・W5・二周目の PC 5場面・スマホ相当 5場面
  00-targets   新しい物差し N1〜N23 (hd2d-r2-targets.py・r2-targets.json) の合否の表 (UI なしの画)
  00-char      キャラと敵 N13〜N17 (hd2d-char-metrics.py・r2-targets.json の charChecks) の合否の表
  01・02       総覧 オーガ (主役)・狼: 本家 ot16｜W5 (主人公62・28°・12°)｜二周目 × UI あり／UI なし／明るさの地図 ＋ ほかの本家 (ot7・ot2)
  03           敵が多い場面 (3体・4体・人形9体): 本家 (ot7・ot11・ot2)｜W5 (主人公48)｜二周目
  04           スマホ相当 (オーガ・4体・狼): 本家｜W5｜二周目
  05           縦の明るさの折れ線 (本家・W5・二周目の3本＋N1 の目標の帯・各線の N2 の太さ): PC オーガ・PC 狼・スマホ相当 オーガ
  06           カメラの比べ (r2-cam: 22°・5°／22°・6°／28°・5.5°・0.41／W5 のカメラと設計図 × PC 狼・PC 4体・PH 狼)
  07           光の変種の並び (r2-look: 既定と6変種 × 狼・オーガ)
  08           演出の回帰 (PC・スマホ相当): 今 (stage=old) の2コマ｜二周目の2コマ・板と矩形のずれ・配置の検査 (= regress と同じ)
  final-numbers.md / .json  上の表の数字 (md の表と生の値)
段2 = regress: 08 だけ (--shots の r2-regress*・r2-regress-old*)。

使い方
  python3 scripts/hd2d-r2-sheet.py trial --shots <試しの撮影のフォルダ> [--w5 <W5 の shots (hero62 と slice の親)>] [--out シート.png]
          [--md 数字.md] [--json 数字.json] [--ref-dir 本家の画の置き場]
  python3 scripts/hd2d-r2-sheet.py final --shots <本番の撮影の親 (r2-slice などの親)> [--w5 <W5 の shots>] [--out-dir <出力のフォルダ>]
          [--only 00-gates,01,…] [--gates docs/design/hd2d-slice/gates.json] [--ref-dir …]
  python3 scripts/hd2d-r2-sheet.py regress --shots <同じ親> [--out-dir …] [--dev PC|PH]
  --w5 の既定は作業場の scratchpad/hd2d/final/shots (無ければ W5 の列は「無い」)。本家の画の既定の置き場は ~/.cache/deck-rogue/hd2d-ref と
  作業場の scratchpad/hd2d/steam (ot2 はこちらにだけある)。無い画・未撮影の場面は「未撮影」の枠で出す (止まらない)。
  出力は既定で unity/Shots/hd2d/r2/ (git の管理外。本家の画を含むのでリポジトリには入れない)。計画 §3-5 の置き場は scratchpad/hd2d/r2/sheets/ (--out-dir で)。
"""
import argparse
import importlib.util
import json
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont

sys.dont_write_bytecode = True
HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(HERE)
FONT = os.path.join(REPO, 'unity', 'Assets', 'Resources', 'Fonts', 'NotoSansJP-Regular.otf')
FONT_B = os.path.join(REPO, 'unity', 'Assets', 'Resources', 'Fonts', 'NotoSansJP-Bold.otf')
DEFAULT_OUT = os.path.join(REPO, 'unity', 'Shots', 'hd2d', 'r2')
DEFAULT_W5 = '/tmp/claude-1000/-home-yosuke-projects-deck-rogue-proto/2379737a-3c0d-4f8d-8002-16334720cb9f/scratchpad/hd2d/final/shots'

BG = (22, 24, 34)
INK = (232, 226, 210)
INK2 = (170, 166, 156)
OKC = (110, 200, 120)
NGC = (235, 110, 80)
LINE = (70, 72, 88)
GOLD = (236, 196, 96)
CYAN = (110, 200, 230)

_spec = importlib.util.spec_from_file_location('hd2d_r2_targets', os.path.join(HERE, 'hd2d-r2-targets.py'))
T = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(T)


def font(sz, bold=False):
    try:
        return ImageFont.truetype(FONT_B if bold else FONT, sz)
    except Exception:
        return ImageFont.load_default()


def text_wrap(d, x, y, s, w, fnt, fill):
    line = ''; lh = fnt.size + 6
    for ch in s:
        if d.textlength(line + ch, font=fnt) > w and line:
            d.text((x, y), line, fill=fill, font=fnt); y += lh; line = ch
        else:
            line += ch
    if line:
        d.text((x, y), line, fill=fill, font=fnt); y += lh
    return y


# ---- 1枚の画と数字

class Cell:
    def __init__(self, label, kind, src, sub=''):
        self.label = label; self.kind = kind; self.src = src; self.sub = sub
        self.img = None; self.r = None; self.err = None


def load_cell(c, layout_json=None, ref_dirs=None):
    try:
        if c.kind == 'ref':
            s = T.shot_from_ref(c.src, ref_dirs or T.DEFAULT_REF_DIRS)
            c.r = T.measure(s); c.img = Image.open(s.path).convert('RGB')
        else:
            folder, scene = c.src
            img, lay, units = T.find_pair(folder, scene)
            s = T.shot_from_files(img, lay, units); s.name = scene
            c.r = T.measure(s, layout_json); c.img = Image.open(img).convert('RGB')
            c.r['label'] = c.label
    except SystemExit as e:
        c.err = str(e)
    except Exception as e:  # noqa
        c.err = '%s: %s' % (type(e).__name__, e)
    return c


def target_rows(r, targets):
    """N1 の目標の帯を画の行へ (足元から上の距離 → 行)"""
    for ch in targets.get('checks', []):
        if ch['key'] == 'N1.feetMinusPeak':
            rng = ch.get(r['dev']) or ch.get('all')
            if rng:
                return r['feet'] - rng[1], r['feet'] - rng[0]
    return None


def thumb(c, w, targets):
    im = c.img
    H = im.height; W = im.width; s = w / W
    t = im.resize((w, int(round(H * s))), Image.LANCZOS).convert('RGBA')
    ov = Image.new('RGBA', t.size, (0, 0, 0, 0)); d = ImageDraw.Draw(ov)
    r = c.r
    tr = target_rows(r, targets)
    if tr:
        d.rectangle([0, tr[0] * s, 14, tr[1] * s], fill=(110, 200, 120, 200))
        d.rectangle([w - 14, tr[0] * s, w, tr[1] * s], fill=(110, 200, 120, 200))
    pk = T.get_key(r, 'N1.peakRow')
    if pk is not None:
        d.line([(0, pk * s), (w, pk * s)], fill=GOLD + (230,), width=2)
        b = T.get_key(r, 'N2.band')
        if b:
            for yy in b:
                d.line([(20, yy * s), (60, yy * s)], fill=GOLD + (200,), width=1)
                d.line([(w - 60, yy * s), (w - 20, yy * s)], fill=GOLD + (200,), width=1)
    hz = T.get_key(r, 'N4.horizonRow')
    if hz is not None:
        for x in range(0, w, 12):
            d.line([(x, hz * s), (x + 6, hz * s)], fill=(230, 230, 230, 170), width=1)
    fx = r.get('heroX'); fy = r.get('feet')
    if fy is not None:
        x0 = (fx or 440) * s
        d.line([(x0 - 30, fy * s), (x0 + 30, fy * s)], fill=CYAN + (255,), width=3)
    x0c, x1c = r['profile']['cols']
    d.rectangle([x0c * s, 0, x1c * s, 3], fill=(236, 196, 96, 160))
    return Image.alpha_composite(t, ov).convert('RGB')


def tokens(r, targets):
    """キャプションの数字 (合否つき)"""
    J = {q[1]: q for q in T.judge(r, targets)} if targets else {}
    out = []

    def add(label, key, fmt='%s', unit=''):
        v = T.get_key(r, key)
        if v is None:
            out.append(('%s —' % label, INK2)); return
        j = J.get(key)
        col = INK if not j or j[4] is None else (OKC if j[4] else NGC)
        out.append(((label + ' ' + fmt + unit) % v, col))
    add('N1 足元から', 'N1.feetMinusPeak', '%.0f', 'px')
    add('頂点の行', 'N1.peakRow', '%d')
    add('N2 太さ', 'N2.fwhm', '%d', 'px')
    add('N3 頂点', 'N3.peak', '%.0f')
    add('N8 後ろ÷頂点', 'N8.ratio', '%.2f')
    add('N22 段差', 'N22.maxStep', '%.1f')
    add('N6a 上÷頂点', 'N6a.ratio', '%.2f')
    add('N4 地平線', 'N4.horizonRow', '%.0f')
    if r.get('K13'):
        add('K13 後列の頭', 'K13.minUp', '%.0f', 'px上')
    return out


def draw_tokens(d, x, y, toks, w, fnt, gap=12):
    cx, cy = x, y; lh = fnt.size + 6
    for t, c in toks:
        tw = d.textlength(t, font=fnt)
        if cx + tw > x + w and cx > x:
            cx = x; cy += lh
        d.text((cx, cy), t, fill=c, font=fnt); cx += tw + gap
    return cy + lh


PALETTE = [(255, 255, 255), (236, 196, 96), (110, 200, 230), (235, 110, 80), (150, 220, 120), (200, 150, 240), (240, 160, 200), (160, 200, 200), (230, 230, 120), (120, 160, 255)]


def chart(cells, targets, dev, w, h, title):
    im = Image.new('RGB', (w, h), BG); d = ImageDraw.Draw(im)
    f = font(17); fb = font(21, True)
    d.text((20, 10), title, fill=INK, font=fb)
    L, R, TOP, BOT = 80, w - 360, 50, h - 50
    d.rectangle([L, TOP, R, BOT], outline=LINE)
    xmax = 800.0; ymax = 200.0
    X = lambda v: L + (R - L) * v / xmax
    Y = lambda v: BOT - (BOT - TOP) * v / ymax
    # 目標の帯 (PC px にそろえた足元からの距離)
    for ch in targets.get('checks', []):
        if ch['key'] == 'N1.feetMinusPeak':
            rng = ch.get(dev) or ch.get('all')
            if rng:
                sc = 1.0 if dev == 'PC' else 0.787
                a_, b_ = rng[0] / sc, rng[1] / sc
                d.rectangle([X(a_), TOP, X(b_), BOT], fill=(40, 70, 48))
                d.text((X(a_) + 4, TOP + 4), 'N1 の目標', fill=OKC, font=f)
    # 目盛り
    for v in range(0, int(xmax) + 1, 100):
        d.line([(X(v), BOT), (X(v), BOT + 6)], fill=INK2); d.text((X(v) - 14, BOT + 8), '%d' % v, fill=INK2, font=f)
    for v in range(0, int(ymax) + 1, 50):
        d.line([(L - 6, Y(v)), (L, Y(v))], fill=INK2); d.text((L - 50, Y(v) - 10), '%d' % v, fill=INK2, font=f)
        d.line([(L, Y(v)), (R, Y(v))], fill=(34, 36, 48))
    d.text((L, BOT + 28), '主人公の足元から上の距離 (px。スマホ相当は1ドットの比でPCにそろえた) →  右ほど画面の上', fill=INK2, font=f)
    # 線
    ly = TOP
    k = 0
    for c in cells:
        if c.r is None or (c.r['dev'] != dev and c.kind != 'ref') or (c.kind == 'ref' and dev != 'PC'):
            continue
        col = PALETTE[k % len(PALETTE)]; k += 1
        r = c.r; sc = r['sc'] or 1.0
        rows = r['profile']['rows']; sm = r['profile']['smooth']
        pts = []
        for y_, v in zip(rows, sm):
            if v is None:
                continue
            dist = (r['feet'] - (y_ + 5)) / sc
            if 0 <= dist <= xmax:
                pts.append((X(dist), Y(min(ymax, v))))
        if len(pts) > 1:
            d.line(pts, fill=col, width=3 if c.kind == 'ref' else 2)
        pk = T.get_key(r, 'N1.feetMinusPeak')
        if pk is not None:
            xx = X(pk / sc); d.ellipse([xx - 5, Y(r['N3']['peak']) - 5, xx + 5, Y(r['N3']['peak']) + 5], outline=col, width=2)
        d.line([(R + 20, ly + 12), (R + 50, ly + 12)], fill=col, width=3)
        d.text((R + 58, ly), c.label, fill=col, font=f); ly += 26
    return im


def sheet_trial(a):
    targets = T.load_targets(a.targets) or {'checks': []}
    sh = a.shots; w5 = a.w5
    PC, PH = 'PC', 'PH'

    def S(name):
        return ('sample', (sh, name))

    def W5(sub, scene):
        return ('sample', (os.path.join(w5, sub), scene))
    rows_spec = [
        ('PC 狼 (霧の変種とカメラ)', [
            ('本家 ot16 (夜の森)', 'ref', 'ot16'),
            ('W5 の見本 (28°・12°)', *W5('hero62', 'PC-S-wolf')),
            ('T-w5-wolf (W5 の光と配置・新カメラ)', *S('T-w5-wolf')),
            ('T-A-wolf (霧 A)', *S('T-A-wolf')),
            ('T-B-wolf (霧 B)', *S('T-B-wolf')),
            ('T-C-wolf (霧 C)', *S('T-C-wolf')),
            ('T-A-wolf-p6 (霧 A・見下ろし6°)', *S('T-A-wolf-p6')),
        ]),
        ('PC 4体・人形9体', [
            ('W5 の見本 4体', *W5('slice', 'PC-S-quad')),
            ('T-w5-quad', *S('T-w5-quad')),
            ('T-A-quad', *S('T-A-quad')),
            ('W5 の見本 人形9体', *W5('slice', 'PC-S-dolls')),
            ('T-A-dolls (K13)', *S('T-A-dolls')),
        ]),
        ('スマホ相当 (1920×886)', [
            ('W5 の見本 スマホ 狼', *W5('hero62', 'PH-S-wolf')),
            ('T-w5-wolf-ph', *S('T-w5-wolf-ph')),
            ('T-A-wolf-ph', *S('T-A-wolf-ph')),
            ('W5 の見本 スマホ 4体', *W5('slice', 'PH-S-quad')),
            ('T-A-quad-ph', *S('T-A-quad-ph')),
        ]),
        ('UI あり (重なりは hd2d-layout-check.py で)', [
            ('W5 の見本 オーガ (UI あり)', 'uiimg', os.path.join(w5, 'hero62', 'PC-S-ogre-1.png')),
            ('T-A-ogre (UI あり)', 'uiimg', os.path.join(sh, 'T-A-ogre-1.png')),
        ]),
    ]
    rows = []
    allcells = []
    for lab, specs in rows_spec:
        cells = []
        for label, kind, src in specs:
            c = Cell(label, kind, src)
            if kind == 'uiimg':
                if os.path.exists(src):
                    c.img = Image.open(src).convert('RGB')
                else:
                    c.err = '未撮影 (%s)' % os.path.basename(src)
            else:
                load_cell(c, a.layout_json if (kind == 'sample' and src[0] == sh) else (a.w5_layout_json if kind == 'sample' else None), a.ref_dir + T.DEFAULT_REF_DIRS)
                if c.err and 'が無い' in c.err:
                    c.err = '未撮影'
            cells.append(c); allcells.append(c)
        rows.append((lab, cells))

    cw = a.cell_w; pad = 20
    ncol = max(len(c) for _, c in rows)
    Wd = pad + ncol * (cw + pad)
    notes = [
        '二周目の試しのビルド (計画 §3-2): カメラ 22°・見下ろし 5° (スマホ 7°) と霧の方式 (距離の霧で帯・高さの霧は地面のもや) だけを先に撮った画。'
        'キャラの光は全部 W5 の写し。霧と配置は 22°・5° 用 (W5 の見本の列だけ 28°・12° の W5 のカメラ)。',
        '重ねた線: 金の横線 = 霧の帯のいちばん明るい行 (N1)・金の短い線 = 帯の上下の端 (N2)・左右の緑の帯 = N1 の目標 (主人公の足元から上 274〜364px。スマホ相当 213〜283px)・'
        '水色の短い線 = 主人公の足元・白い点線 = 地平線 (N4)・上端の金の帯 = 縦の明るさを測る中央の列。',
        '数字: 緑 = 目標に入った・朱 = 外れた・白 = 目標の無い値。N8 = キャラの後ろ (足元の 250〜60px 上) ÷ 帯の頂点 (目標 0.55〜0.8)・N22 = 奥の端の段差 (目標 ≤6)・'
        'N6a = 上の真ん中 ÷ 頂点 (目標 0.45〜0.7)。本家は目安 (本家 ot16 の N9 101 は目標 80〜100 の上の端)。',
    ]
    probe = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    f_title, f_note, f_head, f_cap, f_tok = font(34, True), font(19), font(24, True), font(19, True), font(17)
    y = pad + 50
    for n in notes:
        y = text_wrap(probe, pad, y, n, Wd - 2 * pad, f_note, INK2)
    head_h = y + 10
    # 高さを見積もる
    row_hs = []
    for lab, cells in rows:
        ih = 0; ch = 0
        for c in cells:
            if c.img is not None:
                ih = max(ih, int(round(c.img.height * cw / c.img.width)))
            else:
                ih = max(ih, int(cw * 9 / 16))
            if c.r:
                yy = draw_tokens(probe, 0, 0, tokens(c.r, targets), cw, f_tok)
                ch = max(ch, yy)
        row_hs.append((ih, 40 + 30 + ch + 10))
    chart_h = 460
    Ht = head_h + sum(40 + ih + chh for ih, chh in row_hs) + 2 * (chart_h + pad) + pad
    im = Image.new('RGB', (Wd, Ht), BG); d = ImageDraw.Draw(im)
    d.text((pad, pad), '二周目 試しのビルド — 霧の帯の位置とカメラ (本家 ot16｜W5｜試しの変種・同じ大きさ)', fill=INK, font=f_title)
    y = pad + 50
    for n in notes:
        y = text_wrap(d, pad, y, n, Wd - 2 * pad, f_note, INK2)
    y = head_h
    for (lab, cells), (ih, chh) in zip(rows, row_hs):
        d.text((pad, y), lab, fill=GOLD, font=f_head); y += 40
        for i, c in enumerate(cells):
            x = pad + i * (cw + pad)
            if c.img is not None and c.r is not None:
                im.paste(thumb(c, cw, targets), (x, y))
            elif c.img is not None:
                t = c.img.resize((cw, int(round(c.img.height * cw / c.img.width))), Image.LANCZOS); im.paste(t, (x, y))
            else:
                d.rectangle([x, y, x + cw, y + int(cw * 9 / 16)], outline=LINE, fill=(30, 32, 44))
                d.text((x + 14, y + 14), c.err or '無い', fill=INK2, font=f_cap)
            d.text((x, y + ih + 6), c.label, fill=INK, font=f_cap)
            if c.r:
                draw_tokens(d, x, y + ih + 36, tokens(c.r, targets), cw, f_tok)
        y += ih + chh
    y += pad
    im.paste(chart(allcells, targets, 'PC', Wd - 2 * pad, chart_h, '中央の列の縦の明るさ (PC。x700〜1220・本家は 900〜1300。10行ごとの平均を5つでならした線・丸 = 頂点)'), (pad, y))
    y += chart_h + pad
    im.paste(chart(allcells, targets, 'PH', Wd - 2 * pad, chart_h, '中央の列の縦の明るさ (スマホ相当 1920×886)'), (pad, y))
    os.makedirs(os.path.dirname(os.path.abspath(a.out)), exist_ok=True)
    im.save(a.out)
    results = [c.r for c in allcells if c.r]
    for r_, c in zip(results, [c for c in allcells if c.r]):
        r_['name'] = c.label
    if a.md:
        open(a.md, 'w').write('# 二周目 試しのビルドの数字 (hd2d-r2-targets.py)\n\n' + T.md_table(results, targets) + '\n')
    if a.json:
        json.dump(results, open(a.json, 'w'), ensure_ascii=False, indent=1)
    miss = [c.label for c in allcells if c.err]
    print('シート: %s (%d×%d)・測れた画 %d・無い画 %d%s' % (a.out, im.width, im.height, len(results), len(miss), (' = ' + '／'.join(miss)) if miss else ''))


# ==================================================================================== 段2 = final・regress

STEAM_DIR = '/tmp/claude-1000/-home-yosuke-projects-deck-rogue-proto/2379737a-3c0d-4f8d-8002-16334720cb9f/scratchpad/hd2d/steam'   # ot_921570_2 (ot2) はここにだけある
DEFAULT_GATES = os.path.join(REPO, 'docs', 'design', 'hd2d-slice', 'gates.json')
W5_LAYOUT = os.path.join(T.RES, 'Stage', 'act1_layout_w5.json')
REF_FILE = {'ot16': 'ot_921570_16', 'ot7': 'ot_921570_7', 'ot11': 'ot_921570_11', 'ot2': 'ot_921570_2'}
REF_TITLE = {'ot16': '本家 夜の森 (ot16)', 'ot7': '本家 夜の洞窟 (ot7)', 'ot11': '本家 昼の村 (ot11)', 'ot2': '本家 洞窟 たいまつ (ot2)'}
SERIES = [(236, 236, 236), (120, 170, 255), (236, 196, 96), (150, 220, 120), (235, 110, 80), (200, 150, 240)]


def load_mod(name, file):
    spec = importlib.util.spec_from_file_location(name, os.path.join(HERE, file))
    m = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    return m


class Final:
    """本番の撮影の読み込みと数字 (同じ画は1回だけ測る)"""

    def __init__(self, a):
        self.C = load_mod('hd2d_compare', 'hd2d-compare.py')          # 門①〜⑩ (hd2d-measure.py)・配置の検査
        self.CM = load_mod('hd2d_char_metrics', 'hd2d-char-metrics.py')  # N13〜N17
        self.M = self.C.M
        self.r2 = a.shots; self.w5 = a.w5; self.out = a.out_dir
        self.refdirs = list(a.ref_dir) + T.DEFAULT_REF_DIRS + [STEAM_DIR]
        self.targets = T.load_targets(a.targets) or {'checks': [], 'charChecks': []}
        self.gates = json.load(open(a.gates, encoding='utf-8')) if a.gates and os.path.exists(a.gates) else None
        self.w5lay = W5_LAYOUT if os.path.exists(W5_LAYOUT) else None
        self._g, self._t, self._c, self._ref = {}, {}, {}, {}
        os.makedirs(self.out, exist_ok=True)

    # 置き場: r2 = <shots>/r2-slice、W5 = <w5>/hero62 (狼・オーガ・主人公62) か slice (3体・4体・人形9体・主人公48)
    def r2d(self, sub='r2-slice'):
        return os.path.join(self.r2, sub)

    def w5d(self, scene):
        return os.path.join(self.w5, 'hero62' if scene in ('S-wolf', 'S-ogre') else 'slice')

    def path(self, folder, dev, scene, var='', k=1):
        p = os.path.join(folder, '%s-%s%s-%d.png' % (dev, scene, ('-' + var) if var else '', k))
        return p if os.path.exists(p) else None

    def has(self, folder, name):
        """<名前>-hideui-1.png か <名前>-1.png がある"""
        return any(os.path.exists(os.path.join(folder, name + s)) for s in ('-hideui-1.png', '-1.png'))

    def img(self, folder, dev, scene, var=''):
        p = self.path(folder, dev, scene, var)
        return Image.open(p).convert('RGB') if p else None

    def gates_of(self, folder, dev, scene):
        k = (folder, dev, scene)
        if k not in self._g:
            self._g[k] = self.C.measure_one(folder, dev, scene) if self.path(folder, dev, scene) else None
        return self._g[k]

    def tgt(self, folder, name, w5=False):
        k = (folder, name)
        if k not in self._t:
            try:
                img, lay, units = T.find_pair(folder, name)
                s = T.shot_from_files(img, lay, units); s.name = name
                self._t[k] = T.measure(s, self.w5lay if w5 else None)
            except SystemExit:
                self._t[k] = None
            except Exception as e:  # noqa
                print('測れない: %s/%s (%s)' % (folder, name, e))
                self._t[k] = None
        return self._t[k]

    def ref(self, key):
        if key not in self._ref:
            try:
                self._ref[key] = T.measure(T.shot_from_ref(key, self.refdirs))
            except SystemExit:
                self._ref[key] = None
        return self._ref[key]

    def ref_img(self, key):
        for d in self.refdirs:
            for ext in ('.jpg', '.png'):
                p = os.path.join(d, REF_FILE[key] + ext)
                if os.path.exists(p):
                    return Image.open(p).convert('RGB')
        return None

    def ref_gates(self, key):
        k = ('ref', key)
        if k not in self._g:
            p = None
            for d in self.refdirs:
                q = os.path.join(d, REF_FILE[key] + '.jpg')
                if os.path.exists(q):
                    p = q; break
            self._g[k] = self.M.RefScene(p, self.M.REF_PATCHES.get(REF_FILE[key], {})).measure() if (p and REF_FILE[key] in self.M.REF_PATCHES) else None
        return self._g[k]

    def char(self, folder, name):
        k = (folder, name)
        if k not in self._c:
            try:
                self._c[k] = self.CM.measure_scene(folder, name, self.targets) if self.has(folder, name) else None
            except SystemExit:
                self._c[k] = None
            except Exception as e:  # noqa
                print('キャラを測れない: %s/%s (%s)' % (folder, name, e))
                self._c[k] = None
        return self._c[k]

    # ---- キャプションの数字
    def gate_toks(self, r):
        return self.C.metric_tokens(r, self.gates) if r else []

    def n_toks(self, r, keys=None):
        if not r:
            return []
        J = {q[1]: q for q in T.judge(r, self.targets)}
        out = []
        spec = [('N1 足元から', 'N1.feetMinusPeak', '%.0f', 'px'), ('N2 太さ', 'N2.fwhm', '%d', 'px'), ('N3 頂点', 'N3.peak', '%.0f', ''),
                ('N6a 上', 'N6a.ratio', '%.2f', ''), ('N8 後ろ', 'N8.ratio', '%.2f', ''), ('N8b 窓', 'N8b.median', '%.0f', ''), ('N9 座席', 'N9.median', '%.0f', ''),
                ('N11 灰', 'N11.greyBright', '%.3f', ''), ('N22 段差', 'N22.maxStep', '%.1f', ''), ('N5 霞', 'N5.hazeGt130', '%.2f', ''),
                ('N4 地平線', 'N4.horizonRow', '%.0f', ''), ('N4 傾き', 'N4.edgeTiltDeg', '%.2f', '°'), ('N6c 参考', 'N6c.ratio', '%.2f', ''),
                ('K13 後列', 'K13.minUp', '%.0f', 'px')]
        for lab, key, fmt, unit in spec:
            if keys and key not in keys:
                continue
            v = T.get_key(r, key)
            if v is None:
                continue
            j = J.get(key)
            col = INK if not j or j[4] is None else (OKC if j[4] else NGC)
            out.append(((lab + ' ' + fmt + unit) % v, col))
        return out

    def c_toks(self, cr):
        if not cr:
            return []
        out = []
        for cid, lab, who, v, rng, ok in self.CM.judge(cr, self.targets):
            short = {'body.p50': '体', 'gate8': '⑧', 'contact': '接地', 'satRatio': '彩度比', 'feetDiff12': '足の差', 'feetDiff34': '足の差',
                     'absSepBack': '背景差', 'bodyMinusRing': '輪差', 'absBodyMinusRing': '輪差', 'body.p5': 'p5', 'body.p25': 'p25', 'contactSpread': '接地の開き'}
            key = next((c['key'] for c in self.targets.get('charChecks', []) if c['label'] == lab), '')
            nm = who_short(who) if who != '場面' else ''
            vv = ('○' if v else '×') if key == 'gate8' else v
            out.append(('%s%s %s' % (nm + ' ' if nm else '', short.get(key, key), vv), OKC if ok else NGC))
        return out


def who_short(who):
    """'enemy0 enemy_brute' → '敵1'・'player leader_green' → '主人公'"""
    k = who.split(' ')[0]
    if k == 'player':
        return '主人公'
    if k.startswith('enemy') and k[5:].isdigit():
        return '敵%d' % (int(k[5:]) + 1)
    return k


def grid(title, notes, rows, cell_w, out, cap_h=110, row_label_w=0, col_heads=None):
    """rows = [(行の見出し, [(画像 or None, 見出し, [(文字, 色)]) …])]。画像は cell_w に合わせて縮める (同じ幅 = 同じ大きさ)"""
    pad = 20
    ncol = max(len(r[1]) for r in rows)
    W = pad + row_label_w + ncol * (cell_w + pad)
    probe = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    f_title, f_note, f_head, f_cap, f_tok = font(34, True), font(19), font(24, True), font(19, True), font(16)
    y = pad + 50
    for n in notes:
        y = text_wrap(probe, pad, y, n, W - 2 * pad, f_note, INK2)
    head_h = y + 10 + (40 if col_heads else 0)
    row_h = []
    for lab, cells in rows:
        ih = 0; th = 0
        for im, cap, toks in cells:
            if im is not None:
                ih = max(ih, round(im.height * cell_w / im.width))
            th = max(th, draw_tokens(probe, 0, 0, toks or [], cell_w, f_tok) if toks else 0)
        row_h.append(((ih or round(cell_w * 9 / 16)), max(cap_h, 36 + th + 8)))
    H = head_h + sum(a + b + pad for a, b in row_h) + pad
    sh = Image.new('RGB', (W, H), BG); d = ImageDraw.Draw(sh)
    d.text((pad, pad), title, fill=INK, font=f_title)
    y = pad + 50
    for n in notes:
        y = text_wrap(d, pad, y, n, W - 2 * pad, f_note, INK2)
    if col_heads:
        for i, ch in enumerate(col_heads):
            d.text((pad + row_label_w + i * (cell_w + pad), head_h - 38), ch, fill=GOLD, font=f_head)
    y = head_h
    for (lab, cells), (ih, ch_) in zip(rows, row_h):
        if row_label_w and lab:
            text_wrap(d, pad, y + 8, lab, row_label_w - 16, f_head, GOLD)
        for i, (im, cap, toks) in enumerate(cells):
            x = pad + row_label_w + i * (cell_w + pad)
            if im is not None:
                t = im.resize((cell_w, round(im.height * cell_w / im.width)), Image.LANCZOS)
                sh.paste(t, (x, y))
            else:
                d.rectangle([x, y, x + cell_w, y + ih], outline=LINE, fill=(30, 32, 44))
                d.text((x + 14, y + 14), '未撮影 (この画は無い)', fill=INK2, font=f_tok)
            d.text((x, y + ih + 6), cap, fill=INK, font=f_cap)
            draw_tokens(d, x, y + ih + 34, toks or [], cell_w, f_tok, gap=12)
        y += ih + ch_ + pad
    sh.save(out)
    print('シート:', out, sh.size)
    return out


def table_image(title, notes, head, rows, colw, out):
    """head = [見出し…]・rows = [[(文字, 色) …] …]。文字が幅に入らなければ折り返す"""
    pad = 24
    W = pad * 2 + sum(colw)
    probe = ImageDraw.Draw(Image.new('RGB', (10, 10)))
    f_t, f_n, f_h, f_c = font(34, True), font(18), font(19, True), font(17)
    y = pad + 52
    for n in notes:
        y = text_wrap(probe, pad, y, n, W - 2 * pad, f_n, INK2)
    top = y + 12
    rh = []
    for row in [[(h, GOLD) for h in head]] + rows:
        h = 0
        for (t, c), w in zip(row, colw):
            h = max(h, text_wrap(probe, 0, 0, t, w - 10, f_c, c))
        rh.append(h + 8)
    H = top + sum(rh) + pad
    im = Image.new('RGB', (W, H), BG); d = ImageDraw.Draw(im)
    d.text((pad, pad), title, fill=INK, font=f_t)
    y = pad + 52
    for n in notes:
        y = text_wrap(d, pad, y, n, W - 2 * pad, f_n, INK2)
    y = top
    for i, (row, h) in enumerate(zip([[(h_, GOLD) for h_ in head]] + rows, rh)):
        x = pad
        if i % 2 == 0 and i > 0:
            d.rectangle([pad, y - 2, W - pad, y + h - 4], fill=(28, 30, 42))
        for (t, c), w in zip(row, colw):
            text_wrap(d, x, y, t, w - 10, f_h if i == 0 else f_c, c)
            x += w
        if i == 0:
            d.line([(pad, y + h - 4), (W - pad, y + h - 4)], fill=LINE, width=2)
        y += h
    im.save(out)
    print('シート:', out, im.size)
    return out


def heat(im):
    """明るさの地図 (24px の平均を 0〜160 で灰に。final-sheets.py と同じ)"""
    a = np.asarray(im.convert('RGB')).astype(float)
    y = 0.2126 * a[..., 0] + 0.7152 * a[..., 1] + 0.0722 * a[..., 2]
    H, W = y.shape; bs = 24
    yy = y[:H // bs * bs, :W // bs * bs].reshape(H // bs, bs, W // bs, bs).mean((1, 3))
    g = np.clip(yy / 160 * 255, 0, 255).astype(np.uint8)
    return Image.fromarray(g).resize((W, H), Image.NEAREST).convert('RGB')


def dimmed(im):
    return Image.blend(im, Image.new('RGB', im.size, (0, 0, 0)), 0.45) if im is not None else None


# ---- 00 表

PC_SCENES = [('S-wolf', '狼'), ('S-ogre', 'オーガ'), ('S-trio', '3体'), ('S-quad', '4体'), ('S-dolls', '人形9体')]


def s_gates(F, out):
    rows = []
    fmt = F.M.fmt
    cols = [('m1', 'f1'), ('m2', 'pct'), ('m3', 'f2'), ('m4', 'f1'), ('m5', 'int'), ('m6', 'f2'), ('m7', 'pct')]
    specs = [('本家 夜の森 ot16', None, 'PC', 'ot16'), ('本家 夜の洞窟 ot7', None, 'PC', 'ot7')]
    for sc, lab in PC_SCENES:
        specs.append(('W5 PC %s%s' % (lab, '' if sc in ('S-wolf', 'S-ogre') else ' (主人公48)'), F.w5d(sc), 'PC', sc))
    for sc, lab in (('S-wolf', '狼'), ('S-ogre', 'オーガ'), ('S-quad', '4体')):
        specs.append(('W5 スマホ相当 %s' % lab, F.w5d(sc), 'PH', sc))
    for sc, lab in PC_SCENES:
        specs.append(('二周目 PC %s' % lab, F.r2d(), 'PC', sc))
    for sc, lab in PC_SCENES:
        specs.append(('二周目 スマホ相当 %s' % lab, F.r2d(), 'PH', sc))
    for lab, d, dev, sc in specs:
        if d is None:
            r = F.ref_gates(sc); j = {}
        else:
            r = F.gates_of(d, dev, sc)
            j = F.M.judge(r, F.gates) if (r and 'error' not in r and F.gates) else {}
        if not r or 'error' in r:
            rows.append([(lab, INK)] + [('未撮影' if not r else '測れない', INK2)] + [('', INK)] * 12)
            continue

        def c(key, s):
            if key in j and j[key] is not None:
                return (s, OKC if j[key] else NGC)
            return (s, INK)
        row = [(lab, INK)]
        for key, kind in cols:
            row.append(c(key, fmt(r.get(key), kind) if r.get(key) is not None else '-'))
        un = r.get('m3_uneven')
        rg = r.get('m3_ring_min')   # 2026-10-01 ユーザーの回答: ③ の副条件 = どの足元の輪もいちばん明るい輪の 0.6 倍以上 (むらは参考)
        g3 = ((F.gates or {}).get('gates') or {}).get('m3') or {}
        rgc = INK if rg is None or 'ringMinRatio' not in g3 else (OKC if rg >= g3['ringMinRatio'] else NGC)
        row.insert(4, ((fmt(rg, 'f2') if rg is not None else '-'), rgc))
        row.insert(5, ((fmt(un, 'pct') if un is not None else '-'), INK2))
        m8u = r.get('m8_units') or []
        row.append(c('m8', ('%d/%d' % (sum(1 for x in m8u if x.get('ok')), len(m8u))) if m8u else '-'))
        row.append(c('m9', '-' if r.get('m9_fs') is None else '%.0fpx %.0f:1' % (r['m9_fs'], r.get('m9_contrast') or 0)))
        row.append((('-' if r.get('m10_min') is None else '%.2f' % r['m10_min']), (OKC if j.get('m10') else NGC) if (dev == 'PC' and 'm10' in j and j['m10'] is not None) else INK))
        row.append((('%d/%d' % (j.get('_passed', 0), j.get('_known', 9))) + ('' if dev == 'PC' else ' (⑩ は PC)') if j else '(門の元)', INK))
        rows.append(row)
    head = ['場面', '①', '②', '③', '輪', 'むら(参考)', '④', '⑤', '⑥', '⑦', '⑧', '⑨', '⑩', '合格 ①〜⑨']
    colw = [420, 110, 110, 110, 90, 140, 110, 100, 100, 110, 110, 170, 110, 220]
    notes = ['門 (docs/design/hd2d-slice/gates.json) を hd2d-measure.py で測った表。緑 = 合格・朱 = 不合格・白 = 本家 (門の元) か判定の無い値。',
             '③⑥ はユーザーの回答 (2026-10-01) の読み方: ③ の副条件 =「輪」(どのキャラの足元の輪もいちばん明るい輪の 0.6 倍以上。旧「むら 20% 以下」は参考)・⑥ = キャラの板と近い木を除いた値。',
             'W5 の 3体・4体・人形9体は主人公48 の画 (W5 の門の撮影)。']
    return table_image('00 門①〜⑩: 本家｜W5 の見本｜二周目', notes, head, rows, colw, out)


def s_targets(F, out):
    cols = [('本家 ot16', 'ref', 'ot16', None)]
    for sc, lab in (('S-wolf', '狼'), ('S-ogre', 'オーガ')):
        cols.append(('W5 %s' % lab, 'w5', F.w5d(sc), 'PC-' + sc))
    for sc, lab in PC_SCENES:
        cols.append(('二周目 %s' % lab, 'r2', F.r2d(), 'PC-' + sc))
    for sc, lab in (('S-wolf', '狼'), ('S-ogre', 'オーガ'), ('S-quad', '4体')):
        cols.append(('二周目 スマホ %s' % lab, 'r2', F.r2d(), 'PH-' + sc))
    res = []
    for lab, kind, d, name in cols:
        r = F.ref(d) if kind == 'ref' else F.tgt(d, name, w5=(kind == 'w5'))
        res.append((lab, r))
    rows = []
    passed = [[0, 0] for _ in res]
    for c in F.targets.get('checks', []):
        rng_pc = c.get('PC') or c.get('all'); rng_ph = c.get('PH') or c.get('all')
        tg = '%s / %s' % (T.fmt_rng(rng_pc) if rng_pc else '—', T.fmt_rng(rng_ph) if rng_ph else '—') if (rng_pc or rng_ph) else '参考'
        row = [('%s %s' % (c['id'], c.get('label', c['key'])), INK), (tg, INK2)]
        for i, (lab, r) in enumerate(res):
            if not r:
                row.append(('未撮影', INK2)); continue
            v = T.get_key(r, c['key'])
            if v is None:
                row.append(('—', INK2)); continue
            rng = c.get(r['dev']) if r['dev'] in c else c.get('all')
            ok = None if rng is None else ((rng[0] is None or v >= rng[0]) and (rng[1] is None or v <= rng[1]))
            if ok is not None and r['kind'] != 'ref':
                passed[i][1] += 1; passed[i][0] += bool(ok)
            row.append((('%.3g' % v) if isinstance(v, float) else str(v), INK if (ok is None or r['kind'] == 'ref') else (OKC if ok else NGC)))
        rows.append(row)
    rows.append([('合格の数 (本家は目安なので数えない)', GOLD), ('', INK)] + [(('%d/%d' % tuple(p)) if res[i][1] and res[i][1]['kind'] != 'ref' else '', GOLD) for i, p in enumerate(passed)])
    head = ['物差し', '目標 PC / PH'] + [c[0] for c in cols]
    colw = [470, 190] + [150] * len(cols)
    notes = ['新しい物差し (計画 §1-2・docs/design/hd2d-slice/r2-targets.json) を scripts/hd2d-r2-targets.py で UI なしの画から測った表。帯の位置 (N1) は主人公の足元から上の距離。',
             'W5 は主人公62・28°・12° の画 (W5 の設計図の筋で N19 を写す)。N6c は参考 (合否なし)。スマホ相当の距離と幅は1ドットの比で縮めた目標で判定。',
             '二周目の N24 (木の面積) はレーン C の place.py、N25 (戦闘以外の画面の字) は r2-screens の画に apkfix の bgmax.py。']
    return table_image('00 新しい物差し N1〜N23 の合否: 本家｜W5｜二周目', notes, head, rows, colw, out), res


def s_char(F, out):
    cols = []
    for sc, lab in (('S-wolf', '狼'), ('S-ogre', 'オーガ'), ('S-quad', '4体 (主人公48)')):
        cols.append(('W5 %s' % lab, F.w5d(sc), 'PC-' + sc))
    for sc, lab in (('S-wolf', '狼'), ('S-ogre', 'オーガ'), ('S-trio', '3体'), ('S-quad', '4体')):
        cols.append(('二周目 %s' % lab, F.r2d(), 'PC-' + sc))
    for sc, lab in (('S-wolf', '狼'), ('S-ogre', 'オーガ')):
        cols.append(('二周目 スマホ %s' % lab, F.r2d(), 'PH-' + sc))
    res = [(lab, F.char(d, name)) for lab, d, name in cols]
    rows = []
    for c in F.targets.get('charChecks', []):
        rng = c.get('all') or c.get('PC')
        row = [('%s %s' % (c['id'], c['label']), INK), (T.fmt_rng(rng) if rng else '—', INK2), (str(c.get('honke', '')), INK2)]
        for lab, r in res:
            if not r:
                row.append(('未撮影', INK2)); continue
            js = F.CM.judge(r, {'charChecks': [c]})
            if not js:
                row.append(('—', INK2)); continue
            txt = '・'.join('%s%s' % ((who_short(q[2]) + ' ') if q[2] != '場面' else '', q[3]) for q in js)
            row.append((txt, OKC if all(q[5] for q in js) else NGC))
        rows.append(row)
    head = ['物差し', '目標', '本家'] + [c[0] for c in cols]
    colw = [520, 110, 130] + [190] * len(cols)
    notes = ['キャラと敵 (計画 §1-2 の N13〜N17) を scripts/hd2d-char-metrics.py で測った表 (UI なしの画・unitsonly のマスク)。緑 = その行の全員が合格・朱 = 誰かが不合格。',
             '暗めの敵 = 白い敵 (r2-targets.json の whiteEnemies = 狼) 以外の敵。敵1〜4 = 左 (手前) からの座席の順。足の高さの差はスマホ相当も PC の px にそろえた。本家は計画の表の値 (N16 だけ hd2d-char-metrics.py --ref ot16 で 1.47)。']
    return table_image('00 キャラと敵 N13〜N17 の合否: W5｜二周目', notes, head, rows, colw, out), res


# ---- 01〜04 画の並び


def s_overview(F, scene, label, out):
    rows = []
    ot = F.ref_img('ot16')
    w5d, r2d = F.w5d(scene), F.r2d()
    w5_ui, r2_ui = F.img(w5d, 'PC', scene), F.img(r2d, 'PC', scene)
    w5_h, r2_h = F.img(w5d, 'PC', scene, 'hideui'), F.img(r2d, 'PC', scene, 'hideui')
    rows.append(('UI あり', [
        (ot, REF_TITLE['ot16'], [(t, INK) for t, _ in F.gate_toks(F.ref_gates('ot16'))]),
        (w5_ui, 'W5 の見本 (主人公62・28°・12°)', F.gate_toks(F.gates_of(w5d, 'PC', scene))),
        (r2_ui, '二周目 (22°・5°)', F.gate_toks(F.gates_of(r2d, 'PC', scene))),
    ]))
    rows.append(('UI なし', [
        (dimmed(ot), '本家 (UI なしの画は無い。同じ画を暗くした)', [(t, INK) for t, _ in F.n_toks(F.ref('ot16'))]),
        (w5_h, 'W5 (UI なし)', F.n_toks(F.tgt(w5d, 'PC-' + scene, w5=True))),
        (r2_h, '二周目 (UI なし)', F.n_toks(F.tgt(r2d, 'PC-' + scene))),
    ]))
    rows.append(('明るさの地図・キャラ', [
        (None if ot is None else heat(ot), '本家 (明るさの地図。白いほど明るい)', []),
        (None if w5_h is None else heat(w5_h), 'W5 (明るさの地図)', F.c_toks(F.char(w5d, 'PC-' + scene)) if w5_h is not None else []),
        (None if r2_h is None else heat(r2_h), '二周目 (明るさの地図)', F.c_toks(F.char(r2d, 'PC-' + scene)) if r2_h is not None else []),
    ]))
    rows.append(('ほかの本家', [
        (F.ref_img('ot7'), REF_TITLE['ot7'] + ' (参考)', [(t, INK) for t, _ in F.n_toks(F.ref('ot7'), ('N1.feetMinusPeak', 'N2.fwhm', 'N3.peak', 'N9.median'))]),
        (F.ref_img('ot2'), REF_TITLE['ot2'] + ' (参考)', [(t, INK) for t, _ in F.n_toks(F.ref('ot2'), ('N1.feetMinusPeak', 'N2.fwhm', 'N3.peak', 'N9.median'))]),
    ]))
    notes = ['本家・W5 の見本・二周目を同じ幅 (960px = 1920×1080 の半分) で並べた。UI ありの行 = 門①〜⑩ (緑 合格・朱 不合格・本家は白)、UI なしの行 = 新しい物差し N1〜N23、'
             '3行目 = 明るさの地図とキャラの物差し N13〜N17。本家 = オクトラ1 公式の Steam スクリーンショット (送るだけ・リポジトリには入れない)。']
    return grid('%s 総覧 %s: 本家｜W5 の見本｜二周目' % (os.path.basename(out)[:2], label), notes, rows, 960, out, cap_h=100, row_label_w=150)


def s_many(F, out):
    rows = []
    for scene, lab, refk in (('S-trio', '狼と妖術師と太鼓 (3体)', 'ot7'), ('S-quad', '噛みつく巻物・四巻 (4体)', 'ot11'), ('S-dolls', 'ひなた＋人形9体', 'ot2')):
        w5d = F.w5d(scene); r2d = F.r2d()
        k13 = F.n_toks(F.tgt(r2d, 'PC-' + scene), ('K13.minUp', 'N17')) if scene == 'S-dolls' else []
        rows.append((lab, [
            (F.ref_img(refk), REF_TITLE[refk] + ' (参考)', []),
            (F.img(w5d, 'PC', scene), 'W5 の見本 (主人公48・28°・12°)', F.gate_toks(F.gates_of(w5d, 'PC', scene))),
            (F.img(r2d, 'PC', scene), '二周目', F.gate_toks(F.gates_of(r2d, 'PC', scene)) + k13 + F.c_toks(F.char(r2d, 'PC-' + scene)) if F.img(r2d, 'PC', scene) is not None else []),
        ]))
    notes = ['壊していないかの確認の場面 (UI あり)。本家に同じ編成の画は無いので参考の3枚を置いた。W5 の3体・4体・人形9体は W5 の門の撮影 (主人公48)。',
             '人形9体の K13 = 後列の頭がいちばん近い前列の頭より何 px 上か (20 以上が目標。試しのビルドでは最小 16 = 絵の背丈の差)。']
    return grid('03 敵が多い場面と人形9体 (PC): 本家｜W5｜二周目', notes, rows, 960, out, cap_h=110, row_label_w=170)


def s_phone(F, out):
    ot = F.ref_img('ot16')
    rows = []
    for scene, lab in (('S-ogre', '脳筋オーガ (幕ボス)'), ('S-quad', '巻物4体'), ('S-wolf', 'このは＋狼')):
        w5d = F.w5d(scene); r2d = F.r2d()
        rows.append((lab, [
            (ot, '本家 (1920×1080)', []),
            (F.img(w5d, 'PH', scene), 'W5 (スマホ相当 1920×886・tier=phone)', F.gate_toks(F.gates_of(w5d, 'PH', scene))),
            (F.img(r2d, 'PH', scene), '二周目 (22°・7°・足元の線 0.525)', F.gate_toks(F.gates_of(r2d, 'PH', scene)) + F.n_toks(F.tgt(r2d, 'PH-' + scene), ('N1.feetMinusPeak', 'N2.fwhm', 'N3.peak', 'N8.ratio', 'N22.maxStep'))),
        ]))
    notes = ['スマホ相当 = PC で 1920×886・UI 1.6倍・スマホの品質の段 (tier=phone) で撮った画。同じ幅で並べた。⑩ は PC の門 (スマホ相当は 3.15px 前後)。',
             '二周目の数字の後ろの N は UI なしの画の新しい物差し (スマホ相当の目標: 帯の頂点は足元から 213〜283px 上・太さ 90〜130px)。']
    return grid('04 スマホ相当: 本家｜W5｜二周目', notes, rows, 960, out, cap_h=100, row_label_w=170)


# ---- 05 縦の明るさの折れ線


def profile_chart(F, series, dev, w, h, title):
    """series = [(見出し, r (hd2d-r2-targets の結果), 色, 太さ)]。横軸 = 足元から上の距離 (PC の px)"""
    im = Image.new('RGB', (w, h), BG); d = ImageDraw.Draw(im)
    f = font(17); fb = font(21, True)
    d.text((20, 10), title, fill=INK, font=fb)
    L, R, TOP, BOT = 80, w - 520, 50, h - 56
    d.rectangle([L, TOP, R, BOT], outline=LINE)
    xmax, ymax = 800.0, 200.0
    X = lambda v: L + (R - L) * v / xmax
    Y = lambda v: BOT - (BOT - TOP) * v / ymax
    for c in F.targets.get('checks', []):
        if c['key'] == 'N1.feetMinusPeak':
            rng = c.get(dev) or c.get('all')
            if rng:
                sc = 1.0 if dev == 'PC' else 0.787
                d.rectangle([X(rng[0] / sc), TOP, X(rng[1] / sc), BOT], fill=(40, 70, 48))
                d.text((X(rng[0] / sc) + 4, TOP + 4), 'N1 の目標 (帯の頂点)', fill=OKC, font=f)
    for v in range(0, int(xmax) + 1, 100):
        d.line([(X(v), BOT), (X(v), BOT + 6)], fill=INK2); d.text((X(v) - 14, BOT + 8), '%d' % v, fill=INK2, font=f)
    for v in range(0, int(ymax) + 1, 50):
        d.line([(L - 6, Y(v)), (L, Y(v))], fill=INK2); d.text((L - 50, Y(v) - 10), '%d' % v, fill=INK2, font=f)
        d.line([(L, Y(v)), (R, Y(v))], fill=(34, 36, 48))
    d.text((L, BOT + 30), '主人公の足元から上の距離 (px。スマホ相当は1ドットの比で PC にそろえた) → 右ほど画面の上。短い横線 = 各線の帯の太さ (N2・半値幅)', fill=INK2, font=f)
    n2 = next((c for c in F.targets.get('checks', []) if c['key'] == 'N2.fwhm'), None)
    ly = TOP
    for lab, r, col, wd in series:
        if not r:
            d.text((R + 20, ly), '%s: 未撮影' % lab, fill=INK2, font=f); ly += 52; continue
        sc = r.get('sc') or 1.0
        pts = []
        for y_, v in zip(r['profile']['rows'], r['profile']['smooth']):
            if v is None:
                continue
            dist = (r['feet'] - (y_ + 5)) / sc
            if 0 <= dist <= xmax:
                pts.append((X(dist), Y(min(ymax, v))))
        if len(pts) > 1:
            d.line(pts, fill=col, width=wd)
        pk = T.get_key(r, 'N1.feetMinusPeak'); pv = T.get_key(r, 'N3.peak')
        band = T.get_key(r, 'N2.band')
        if pk is not None and pv is not None:
            xx = X(pk / sc); d.ellipse([xx - 6, Y(pv) - 6, xx + 6, Y(pv) + 6], outline=col, width=3)
            if band:
                x_a, x_b = X((r['feet'] - band[1]) / sc), X((r['feet'] - band[0]) / sc)
                yy = Y(pv * 0.5 + 25)
                d.line([(x_a, yy), (x_b, yy)], fill=col, width=3)
                d.line([(x_a, yy - 6), (x_a, yy + 6)], fill=col, width=2); d.line([(x_b, yy - 6), (x_b, yy + 6)], fill=col, width=2)
        J = {q[1]: q for q in T.judge(r, F.targets)} if r.get('kind') != 'ref' else {}

        def cc(key):
            j = J.get(key)
            return INK if not j or j[4] is None else (OKC if j[4] else NGC)
        d.line([(R + 20, ly + 12), (R + 54, ly + 12)], fill=col, width=wd)
        d.text((R + 62, ly), lab, fill=col, font=f)
        tx = R + 62
        for t, key in (('頂点 足元から %s' % (('%.0f' % pk) if pk is not None else '—'), 'N1.feetMinusPeak'), ('太さ %s' % (T.get_key(r, 'N2.fwhm')), 'N2.fwhm'), ('明るさ %s' % pv, 'N3.peak')):
            d.text((tx, ly + 24), t, fill=cc(key), font=f); tx += d.textlength(t, font=f) + 12
        ly += 56
    if n2:
        d.text((R + 20, BOT - 24), 'N2 の目標 %s (PC)・%s (スマホ相当)' % (T.fmt_rng(n2['PC']), T.fmt_rng(n2['PH'])), fill=INK2, font=f)
    return im


def s_profile(F, out):
    w = 2200; h = 520; charts = []
    for dev, scene, lab in (('PC', 'S-ogre', 'PC オーガ (主役)'), ('PC', 'S-wolf', 'PC 狼'), ('PH', 'S-ogre', 'スマホ相当 オーガ')):
        series = [(REF_TITLE['ot16'], F.ref('ot16'), SERIES[0], 4),
                  ('W5 の見本', F.tgt(F.w5d(scene), '%s-%s' % (dev, scene), w5=True), SERIES[1], 3),
                  ('二周目', F.tgt(F.r2d(), '%s-%s' % (dev, scene)), SERIES[2], 4)]
        charts.append(profile_chart(F, series, dev, w, h, '中央の列の縦の明るさ: %s (x700〜1220・本家は x900〜1300。10行ごとの平均を5つでならした線・丸 = 頂点)' % lab))
    pad = 20
    head = Image.new('RGB', (w, 120), BG); d = ImageDraw.Draw(head)
    d.text((pad, pad), '05 霧の帯の位置 (縦の明るさ): 本家｜W5｜二周目', fill=INK, font=font(34, True))
    text_wrap(d, pad, pad + 50, '裁定「いちばん明るい霧の帯は本家と同じくキャラのすぐ上へ」の答え合わせ。緑の帯 = N1 の目標 (帯の頂点が主人公の足元から 274〜364px 上。スマホ相当は 213〜283px を PC の px にそろえた)。', w - 2 * pad, font(19), INK2)
    sh = Image.new('RGB', (w, head.height + sum(c.height + pad for c in charts)), BG)
    sh.paste(head, (0, 0)); y = head.height
    for c in charts:
        sh.paste(c, (0, y)); y += c.height + pad
    sh.save(out); print('シート:', out, sh.size)
    return out


# ---- 06 カメラ・07 光の変種

CAMS = [('cam22p5', '22°・5° (既定)'), ('cam22p6', '22°・6°'), ('cam28p55', '28°・5.5°・足元 0.41'), ('w5cam', 'W5 のカメラと設計図 (28°・12°)')]
LOOKS = [('r2lamp14', '座席の灯 1.4'), ('r2lamp23', '座席の灯 2.3'), ('r2liftref', '暗い所の色 (本家の分解の lift)'), ('r2dofstrong', 'ぼかしを強く'),
         ('r2lampside', '灯を横へ'), ('r2moonck', '月の木漏れ日'),
         ('r2edge03', '試しの決定の霧 (芯の edge 0.3・横の減光 0.9/0.28)')]   # 統合: 本番の前に既定を edge 1.0・0.8/0.30 へ変えた。その前の姿


def s_camera(F, out):
    d = os.path.join(F.r2, 'r2-cam')
    rows = []
    keys = ('N1.feetMinusPeak', 'N2.fwhm', 'N3.peak', 'N4.horizonRow', 'N4.edgeTiltDeg', 'N8.ratio', 'N22.maxStep', 'N6a.ratio')
    for dev, scene, lab in (('PC', 'S-wolf', 'PC 狼'), ('PC', 'S-quad', 'PC 4体'), ('PH', 'S-wolf', 'スマホ相当 狼')):
        cells = []
        for tag, cl in CAMS:
            name = '%s-V-%s-%s' % (dev, tag, scene)
            cells.append((F.img(d, dev, 'V-%s-%s' % (tag, scene), 'hideui'), cl, F.n_toks(F.tgt(d, name, w5=(tag == 'w5cam')), keys)))
        rows.append((lab, cells))
    notes = ['★ 霧と配置は 22°・5° 用 (段2 の既定の設計図は新しいカメラに合わせて置いた)。他のカメラの列は「カメラだけ替えた時の見え方」で、帯や木の位置が合わないのは当たり前。'
             'W5 の列だけは W5 の光と配置 (look_act1_w5 + look_act1_w5char) を W5 のカメラで撮った物 = 二周目の既定との全体の比べ。',
             'スマホ相当の見下ろしは PC +2° (22°・5° → 7°・22°・6° → 8°・28°・5.5° → 7.5°・W5 は 12°)。数字は UI なしの画の新しい物差し (緑 目標に入った・朱 外れた)。']
    return grid('06 カメラの比べ (UI なし): 22°・5°｜22°・6°｜28°・5.5°｜W5', notes, rows, 640, out, cap_h=90, row_label_w=150, col_heads=[c[1] for c in CAMS])


def s_look(F, out):
    d = os.path.join(F.r2, 'r2-look')
    rows = []
    gk = ('①', '③', '④', '⑧')

    def toks(folder, dev, scene, name):
        g = [t for t in F.gate_toks(F.gates_of(folder, dev, scene)) if t[0][:1] in gk]
        n = F.n_toks(F.tgt(folder, name), ('N3.peak', 'N9.median', 'N8b.median'))
        return g + n + F.c_toks(F.char(folder, name))[:4]
    rows.append(('既定 (二周目)', [(F.img(F.r2d(), 'PC', sc), '既定・%s' % lab, toks(F.r2d(), 'PC', sc, 'PC-' + sc)) for sc, lab in (('S-wolf', '狼'), ('S-ogre', 'オーガ'))]))
    for tag, lab in LOOKS:
        rows.append((lab, [(F.img(d, 'PC', 'V-%s-%s' % (tag, sc)), 'look_act1_%s・%s' % (tag, sl), toks(d, 'PC', 'V-%s-%s' % (tag, sc), 'PC-V-%s-%s' % (tag, sc))) for sc, sl in (('S-wolf', '狼'), ('S-ogre', 'オーガ'))]))
    notes = ['レーン B の光の変種 (look=<変種> を既定の設計図に重ねた) を UI ありの画で並べた。数字: 門の①③④⑧ (hd2d-measure)・N3 帯の頂点・N9 座席の地面・N8b 主人公の後ろ (UI なしの画)・'
             'キャラの物差し (N13〜N17・unitsonly のマスク)。既定の行は r2-slice の画。変種の UI なし・キャラの板だけの画が無い時は数字が近似 (UI の明るさが入る) になる。']
    return grid('07 光の変種 (PC・UI あり): 既定｜灯 1.4｜灯 2.3｜lift｜ぼかし｜灯を横へ｜木漏れ日｜試しの決定の霧', notes, rows, 800, out, cap_h=110, row_label_w=200, col_heads=['狼', 'オーガ (幕ボス)'])


# ---- 08 演出の回帰

REG_ROWS = [('R01-swing', '振り (打撃)'), ('R02-multi', '多段'), ('R03-aoe', '全体攻撃'), ('R04-exposed', '急所'), ('R05-enrage', '豹変 (HP 半分)'),
            ('R06-guard', '盾で受ける'), ('R07-hurt', '被弾'), ('R08-kill', 'とどめと崩れ'), ('R09-enter', '登場と名前の帯'), ('R10-trap', 'からくりの発動'),
            ('R11-doll', '人形の踏み込み'), ('R12-gearwin', 'ギアの窓'), ('R14-popup', '札の拡大'), ('R15-tip', '説明パネル'), ('R16-bosskill', '幕ボスの撃破の寄り (二周目で足した)')]


def s_regress(F, dev, out):
    dn = os.path.join(F.r2, 'r2-regress' if dev == 'PC' else 'r2-regress-ph')
    do = os.path.join(F.r2, 'r2-regress-old' if dev == 'PC' else 'r2-regress-old-ph')
    C = F.C
    rows = []
    for row, lab in REG_ROWS:
        n = 0
        while F.path(dn, dev, row, '', n + 1):
            n += 1
        n_old = 0
        while F.path(do, dev, row, '', n_old + 1):
            n_old += 1
        if n == 0 and n_old == 0:
            continue
        nn = max(n, n_old, 1)
        ks = sorted({max(1, round(nn * 0.4)), max(1, round(nn * 0.7))}) if nn > 1 else [1]

        def worst_of(dd, cnt):
            w = None; lc = 0
            for k in range(1, cnt + 1):
                p = F.path(dd, dev, row, '', k)
                if not p:
                    continue
                lj = p[:-4] + '.layout.json'
                dv = C.unitbox_dev(lj)
                if dv is not None:
                    w = dv if w is None else max(w, dv)
                if os.path.exists(lj):
                    V, _ = C.LC.check(F.M.load_layout(lj))
                    lc += len(V)
            return w, lc
        wn, lcn = worst_of(dn, n)
        wo, lco = worst_of(do, n_old)
        moving = nn > 1
        ok = None if wn is None else ((wn - (wo or 0) <= 1.0) if moving else (wn <= 1.0))
        cells = []
        for k in ks:
            p = F.path(do, dev, row, '', k)
            cells.append((Image.open(p).convert('RGB') if p else None, '今 %d/%d 枚目' % (k, n_old), []))
        for k in ks:
            p = F.path(dn, dev, row, '', k)
            tk = []
            if k == ks[0] and wn is not None:
                tk.append(('板と矩形のずれ 最大 %.1fpx (今 %s・差 %+.1f)' % (wn, ('%.1f' % wo) if wo is not None else '—', wn - (wo or 0)) if moving else '板と矩形のずれ 最大 %.1fpx (静止)' % wn, OKC if ok else NGC))
                tk.append(('配置の検査 %d 件 (全コマ・今 %d 件)' % (lcn, lco), OKC if lcn <= lco else NGC))
            cells.append((Image.open(p).convert('RGB') if p else None, '二周目 %d/%d 枚目' % (k, n), tk))
        rows.append(('%s %s' % (row[:3], lab), cells))
    notes = ['演出の回帰 R1〜R16 (R13 ギアの一覧は省いた) から、連番の 4割と 7割の所の2枚ずつ。左2枚 = 今 (stage=old)・右2枚 = 二周目。W5 と画素一致は求めない (カメラが変わった)。',
             '「板と矩形のずれ」= キャラの板が UI の矩形からずれた最大 (静止は 1px 以内・演出中は今の舞台との差 1px 以内が合格)。「配置の検査」= hd2d-layout-check.py の違反の数 (全コマの合計)。',
             '目で見る所: 斬撃・盾・崩れ・登場の帯・罠の幽霊・人形の点灯と踏み込みが座席と札に合っているか。撃破 (R08・R16) と幕ボスの登場 (R09) の寄りで額縁の下端がボスの頭と名前の帯に掛からないか。振り (R01) で宝石の光が宙に残らないか。']
    if not rows:
        print('演出の回帰の画が無い: %s・%s' % (dn, do))
        return None
    return grid('08 演出の回帰 (%s): 今｜二周目' % ('PC' if dev == 'PC' else 'スマホ相当'), notes, rows, 480, out, cap_h=70, row_label_w=200)


def sheet_final(a):
    F = Final(a)
    only = set((a.only or '').split(',')) - {''}

    def want(k):
        return not only or any(k.startswith(o) for o in only)
    made = []
    numbers = {}
    if want('00-gates'):
        made.append(s_gates(F, os.path.join(F.out, '00-gates.png')))
    if want('00-targets'):
        p, res = s_targets(F, os.path.join(F.out, '00-targets.png')); made.append(p)
        numbers['targets'] = [dict(r, name=lab) for lab, r in res if r]
    if want('00-char'):
        p, res = s_char(F, os.path.join(F.out, '00-char.png')); made.append(p)
        numbers['char'] = [dict(r, name=lab) for lab, r in res if r]
    if want('01'):
        made.append(s_overview(F, 'S-ogre', '(PC・このは＋脳筋オーガ＝幕ボス。主役)', os.path.join(F.out, '01-overview-PC-ogre.png')))
    if want('02'):
        made.append(s_overview(F, 'S-wolf', '(PC・このは＋牙嵐の狼)', os.path.join(F.out, '02-overview-PC-wolf.png')))
    if want('03'):
        made.append(s_many(F, os.path.join(F.out, '03-many-enemies-dolls.png')))
    if want('04'):
        made.append(s_phone(F, os.path.join(F.out, '04-phone.png')))
    if want('05'):
        made.append(s_profile(F, os.path.join(F.out, '05-fog-profile.png')))
    if want('06'):
        made.append(s_camera(F, os.path.join(F.out, '06-camera.png')))
    if want('07'):
        made.append(s_look(F, os.path.join(F.out, '07-look-variants.png')))
    if want('08'):
        made.append(s_regress(F, 'PC', os.path.join(F.out, '08-regress-PC.png')))
        made.append(s_regress(F, 'PH', os.path.join(F.out, '08-regress-PH.png')))
    if numbers:
        md = ['# 二周目の本番の数字 (hd2d-r2-sheet.py final)', '']
        if numbers.get('targets'):
            md += ['## 新しい物差し N1〜N23 (hd2d-r2-targets.py)', '', T.md_table(numbers['targets'], F.targets), '']
        if numbers.get('char'):
            md += ['## キャラと敵 N13〜N17 (hd2d-char-metrics.py)', '', F.CM.md_table(numbers['char'], F.targets), '']
        open(os.path.join(F.out, 'final-numbers.md'), 'w').write('\n'.join(md) + '\n')
        json.dump(numbers, open(os.path.join(F.out, 'final-numbers.json'), 'w'), ensure_ascii=False, indent=1)
    print(json.dumps([m for m in made if m], ensure_ascii=False))


def sheet_regress(a):
    F = Final(a)
    for dev in (a.dev.split(',') if a.dev else ['PC', 'PH']):
        s_regress(F, dev, os.path.join(F.out, '08-regress-%s.png' % dev))


def main():
    ap = argparse.ArgumentParser(description='HD-2D 見本 二周目の比較シート')
    sp = ap.add_subparsers(dest='cmd', required=True)
    t = sp.add_parser('trial', help='試しのビルドのシート (段1)')
    t.add_argument('--shots', required=True, help='試しの撮影のフォルダ (pshots.sh の出力)')
    t.add_argument('--w5', default=DEFAULT_W5, help='W5 の shots (hero62 と slice の親)')
    t.add_argument('--out', default=os.path.join(DEFAULT_OUT, 'trial-sheet.png'))
    t.add_argument('--md'); t.add_argument('--json')
    t.add_argument('--targets', default=T.DEFAULT_TARGETS)
    t.add_argument('--ref-dir', action='append', default=[])
    t.add_argument('--layout-json', help='試しの画の N19 の筋を読む設計図 (既定は layout.json の extra.diorama.layout)')
    t.add_argument('--w5-layout-json', help='W5 の画の N19 の筋を読む設計図 (既定は同じく layout.json から。act1_layout_w5.json ができたらそれを)')
    t.add_argument('--cell-w', type=int, default=480)
    for nm, hp in (('final', '本番の比較シート (段2)'), ('regress', '演出の回帰のシートだけ (段2)')):
        f = sp.add_parser(nm, help=hp)
        f.add_argument('--shots', required=True, help='本番の撮影の親のフォルダ (r2-slice・r2-cam・r2-look・r2-regress* が並ぶ所)')
        f.add_argument('--w5', default=DEFAULT_W5, help='W5 の shots (hero62・slice の親)')
        f.add_argument('--out-dir', default=os.path.join(DEFAULT_OUT, 'final'))
        f.add_argument('--targets', default=T.DEFAULT_TARGETS)
        f.add_argument('--gates', default=DEFAULT_GATES)
        f.add_argument('--ref-dir', action='append', default=[])
        if nm == 'final':
            f.add_argument('--only', help='作るシートの頭の名前 (カンマ区切り: 00-gates,00-targets,00-char,01,02,…,08)')
        else:
            f.add_argument('--dev', help='PC・PH (カンマ区切り。既定は両方)')
    a = ap.parse_args()
    if a.cmd == 'final':
        return sheet_final(a)
    if a.cmd == 'regress':
        return sheet_regress(a)
    if a.cmd == 'trial' and not a.w5_layout_json:
        w5lay = os.path.join(T.RES, 'Stage', 'act1_layout_w5.json')   # レーン C の W5 の写し (あれば W5 の画の筋はこれで写す)
        if os.path.exists(w5lay):
            a.w5_layout_json = w5lay
    if a.cmd == 'trial':
        sheet_trial(a)


if __name__ == '__main__':
    main()

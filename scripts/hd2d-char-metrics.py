#!/usr/bin/env python3
"""HD-2D 見本 二周目のキャラと敵の物差し N13〜N17 (2026-10-01 レーン F 段2)。

計画 docs/design/hd2d-round2-plan-2026-10-01.md §1-2 の N13〜N17 を1場面ずつ測る。元は作業場の r2/char/char_metrics.py (キャラの分析)。
目標は docs/design/hd2d-slice/r2-targets.json の charChecks (whiteEnemies = 白い敵の絵の名前の頭)。

  輝度 = 0.2126R + 0.7152G + 0.0722B (sRGB の値 0〜255。hd2d-measure.py と同じ)。
  測る画 = UI なしの画 (<場面>-hideui-1.png。無ければ <場面>-1.png。UI がある画なら layout.json の札の矩形か uionly の画で UI を除く)。
  キャラの画素 = unitsonly の画 (マゼンタ以外)。1体ぶん = 板の矩形 (stage.unitBoxes の boardRawPx) ∩ キャラの画素。
    unitsonly が無い時は近似 (体 = 絵の矩形の真ん中 [x+0.3w, y+0.25h, 0.4w, 0.5h]・キャラを除く = 板の矩形。hd2d-measure.py の⑧と同じ近似)。結果の charMask に書く。
    重なったキャラ (人形の列・主人公の前の人形) は板の矩形で分けるだけ = 前の人形の画素が主人公に混ざる。人形は数字を出すが合否には数えない。

物差し (1体ごと)
  body     体の輝度の p5・p25・p50・p95 (キャラの画素を 2px 削った内側)                      N13 (主人公の p50)・N14 (敵)
  ring     足元の後ろの地面の輪 = 門⑧ の定義 (hd2d-measure.py の⑧と同じ矩形: 足元の横 ±0.7×絵の幅・
           足元の 0.25×絵の高さ上〜5px 下。キャラ (2px 太らせた物) と UI を除いた中央値)
  gate8    体と輪の差 15 以上か比 1.4 以上なら 1 (門⑧ と同じ。主人公の「①以上」は hd2d-measure.py で見る)
  back     体の上 55% のすぐ外 (4〜28px) の背景の中央値。sepBack = body.p50 − back (N14「体と背景の差」)
  contact  足の真下 (足元の −2〜+10px・足の幅) ÷ 左右の地面 (足の幅の 0.4〜1.2 倍離れた所)                     N15
  sat      体の彩度 (Oklab の C の中央値) ÷ 舞台の彩度 (キャラを 1px 太らせて除いた画素の C の中央値)
  feetY    足元の行 (layout.json の stage.camera.seats の px。無ければキャラの画素の下端)
物差し (場面で1つ)
  satRatio      キャラ全員の彩度 ÷ 舞台の彩度 = hd2d-color.py の char_pop_C と同じ定義                               N16
  contactSpread 主人公と敵の contact の最大 − 最小                                                                  N15
  feetDiff12 / feetDiff34  敵の足元と主人公の足元の行の差の最大 (1〜2体 / 3体以上。スマホ相当は1ドットの比 sc で PC の px にそろえる) N17
  heroHeightFrac・bossHeightFrac  背丈 ÷ 画面の高さ (N21 は記録だけ)

使い方
  python3 scripts/hd2d-char-metrics.py <撮影のフォルダ> <場面> [<場面> ...]      # 例: … shots/r2-slice PC-S-ogre PC-S-wolf
  python3 scripts/hd2d-char-metrics.py --all <撮影のフォルダ>                      # 場面を全部 (unitsonly・uionly・hideui の画は同じ場面の部品として読む)
  python3 scripts/hd2d-char-metrics.py --ref ot16                                  # 本家は N16 だけ (hd2d-color.py の本家の矩形。ot16 は 1.47 前後)
  共通: [--targets docs/design/hd2d-slice/r2-targets.json] [--json 出力.json] [--md 出力.md] [--quiet]
"""
import argparse
import glob
import importlib.util
import json
import os
import sys

import numpy as np
from PIL import Image

sys.dont_write_bytecode = True   # 読み込む hd2d-measure.py・hd2d-color.py の .pyc を scripts/ に残さない
HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.dirname(HERE)
DEFAULT_TARGETS = os.path.join(REPO, 'docs', 'design', 'hd2d-slice', 'r2-targets.json')
DEFAULT_REF_DIRS = [os.path.expanduser('~/.cache/deck-rogue/hd2d-ref')]
REF_FILES = {'ot16': 'ot_921570_16', 'ot7': 'ot_921570_7', 'ot11': 'ot_921570_11'}


def load_mod(name, file):
    spec = importlib.util.spec_from_file_location(name, os.path.join(HERE, file))
    m = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    return m


M = load_mod('hd2d_measure', 'hd2d-measure.py')   # unit_feet・札の矩形 (門⑧ と同じ足元と UI)
COL = load_mod('hd2d_color', 'hd2d-color.py')     # to_oklab (N16 = char_pop_C と同じ色空間)

# ---- 道具


def load(p):
    return np.asarray(Image.open(p).convert('RGB')).astype(np.float32)


def lum(a):
    return 0.2126 * a[..., 0] + 0.7152 * a[..., 1] + 0.0722 * a[..., 2]


def grow(m, r):
    a = m.copy()
    for ax in (0, 1):
        b = a.copy()
        for k in range(1, r + 1):
            if ax == 0:
                b[k:] |= a[:-k]; b[:-k] |= a[k:]
            else:
                b[:, k:] |= a[:, :-k]; b[:, :-k] |= a[:, k:]
        a = b
    return a


def shrink(m, r):
    return ~grow(~m, r)


def rect_mask(shape, rects):
    H, W = shape[:2]
    m = np.zeros((H, W), bool)
    for x, y, w, h in rects:
        x0, y0 = max(0, int(round(x))), max(0, int(round(y)))
        x1, y1 = min(W, int(round(x + w))), min(H, int(round(y + h)))
        if x1 > x0 and y1 > y0:
            m[y0:y1, x0:x1] = True
    return m


def rnd(v, n=1):
    if v is None:
        return None
    try:
        f = float(v)
    except Exception:
        return v
    return round(f, n) if np.isfinite(f) else None


def pc(a, q):
    return rnd(np.percentile(a, q), 1) if a.size else None


def is_magenta(a):
    return (a[..., 0] > 240) & (a[..., 1] < 20) & (a[..., 2] > 240)

# ---- 場面を読む


def scene_files(folder, scene):
    """(測る画, UI あり?, layout, unitsonly, uionly)"""
    p = lambda s: os.path.join(folder, scene + s)
    img = None; has_ui = False
    for cand in ('-hideui-1.png', '-1.png'):
        if os.path.exists(p(cand)):
            img = p(cand); break
    if img is None:
        raise SystemExit('画が無い: %s/%s-hideui-1.png か -1.png' % (folder, scene))
    lay = None
    for cand in (img[:-4] + '.layout.json', p('-1.layout.json'), p('-hideui-1.layout.json')):
        if os.path.exists(cand):
            lay = cand; break
    L = json.load(open(lay)) if lay else None
    if img.endswith('-1.png') and not img.endswith('-hideui-1.png'):
        has_ui = not (L and 'hideui=1' in (L.get('state') or ''))
    units = p('-unitsonly-1.png') if os.path.exists(p('-unitsonly-1.png')) else None
    uio = p('-uionly-1.png') if os.path.exists(p('-uionly-1.png')) else None
    return img, has_ui, L, units, uio


def ui_mask(a, lay, uio):
    if uio:
        u = load(uio)
        return ~is_magenta(u), 'uionly'
    if lay and lay.get('units') is not None:
        rects = M.overlay_ui_rects(lay) + M.excluded_ui_rects(lay)
        rects += [n['px'] for n in M.nodes_matching(lay, r'/(desk-shade|handlayer)$')]
        return rect_mask(a.shape, rects), 'layout の札の矩形 (近似)'
    return np.zeros(a.shape[:2], bool), 'なし'


def white_of(targets):
    return tuple((targets or {}).get('whiteEnemies') or ['enemy_wolf'])


def measure_scene(folder, scene, targets=None):
    img, has_ui, lay, units_p, uio = scene_files(folder, scene)
    a = load(img); H, W = a.shape[:2]
    Lh = lum(a)
    r = dict(name=scene, folder=folder, image=os.path.basename(img), size=[W, H], notes=[])
    if lay is None:
        r['notes'].append('layout.json が無い = キャラの場所が分からない')
        r['units'] = []
        return r
    r['state'] = lay.get('state')
    r['dev'] = 'PH' if (lay.get('canvas') or {}).get('phone') else 'PC'
    ui = np.zeros((H, W), bool)
    if has_ui:
        ui, src = ui_mask(a, lay, uio)
        r['notes'].append('UI がある画 (hideui の画が無い)。UI = %s' % src)
    ub = {u['key']: u for u in ((lay.get('stage') or {}).get('unitBoxes') or [])}
    seats = {s['key']: s for s in (((lay.get('stage') or {}).get('camera') or {}).get('seats') or [])}
    us = {u.get('key'): u for u in M.lay_units(lay)}
    approx = True
    if units_p:
        uo = load(units_p)
        ch = ~is_magenta(uo)
        if ch.sum() > 200:
            approx = False
    if approx:
        ch = rect_mask(a.shape, [u['boardRawPx'] for u in ub.values() if u.get('boardRawPx')])
        r['charMask'] = '板の矩形 (近似・unitsonly が無い)'
    else:
        r['charMask'] = 'unitsonly'
    r['approx'] = approx
    ph = ub.get('player', {}).get('pxPerDot')
    sc = (ph / 4.0) if ph else H / 1080.0
    r['sc'] = rnd(sc, 3)
    # 舞台の彩度 (hd2d-color.py の measure_scene と同じ: キャラを 1px 太らせて除く・UI を除く)
    lab = COL.to_oklab(a)
    Cimg = np.hypot(lab[..., 1], lab[..., 2])
    stage = ~ui & ~grow(ch, 1)
    stageC = float(np.median(Cimg[stage])) if stage.sum() > 500 else None
    r['stageC'] = rnd(stageC, 4)
    if not approx:
        chars = ch & ~ui
        r['charsC'] = rnd(float(np.median(Cimg[chars])), 4) if chars.sum() > 200 else None
        r['satRatio'] = rnd(r['charsC'] / stageC, 3) if (r['charsC'] and stageC) else None
    white = white_of(targets)
    out = []
    for key, box in ub.items():
        u = us.get(key) or {}
        if u and not u.get('alive', True):
            continue
        kind = u.get('kind') or ('doll' if key.startswith('doll:') else ('player' if key == 'player' else 'enemy'))
        art = box.get('art') or u.get('id') or ''
        bx = box.get('boardRawPx') or box.get('boardPx')
        if not bx:
            continue
        board = rect_mask(a.shape, [bx])
        e = dict(key=key, kind=kind, art=art, pxPerDot=box.get('pxPerDot'))
        if kind == 'enemy':
            e['cls'] = 'white' if art.startswith(white) else 'dark'
        sp = u.get('sprite') or box.get('rectPx') or bx
        f = M.unit_feet(lay, u) if u.get('sprite') else (sp[0] + sp[2] / 2.0, sp[1] + sp[3])
        if approx:
            m = rect_mask(a.shape, [[sp[0] + sp[2] * 0.3, sp[1] + sp[3] * 0.25, sp[2] * 0.4, sp[3] * 0.5]]) & ~ui
            body = m
            top, bot = sp[1], sp[1] + sp[3]
            e['note'] = '近似 (絵の矩形の真ん中)'
        else:
            m = board & ch
            if m.sum() < 50:
                continue
            ys, xs = np.nonzero(m)
            top, bot = int(ys.min()), int(ys.max())
            body = shrink(m, 2) & ~ui
        hh = bot - top + 1
        if not approx:   # 近似の時は絵の矩形 (透明な余白を含む) なので背丈は出さない
            e['heightPx'] = int(hh); e['heightFrac'] = rnd(hh / H, 3)
        bv = Lh[body]
        e['body'] = dict(p5=pc(bv, 5), p25=pc(bv, 25), p50=pc(bv, 50), p95=pc(bv, 95))
        # 門⑧ の輪
        w_, h_ = sp[2], sp[3]
        ring_m = rect_mask(a.shape, [[f[0] - 0.7 * w_, f[1] - 0.25 * h_, 1.4 * w_, 0.25 * h_ + 5]]) & ~grow(ch, 2) & ~ui
        ringv = float(np.median(Lh[ring_m])) if ring_m.sum() > 30 else None
        e['ring'] = rnd(ringv, 1)
        b50 = e['body']['p50']
        if b50 is not None and ringv is not None:
            e['bodyMinusRing'] = rnd(b50 - ringv, 1)
            e['absBodyMinusRing'] = rnd(abs(b50 - ringv), 1)
            e['ringRatio'] = rnd(max(b50, ringv) / max(1.0, min(b50, ringv)), 2)
            e['gate8'] = 1 if (abs(b50 - ringv) >= 15 or e['ringRatio'] >= 1.4) else 0
        if not approx:
            # 体の上 55% のすぐ外の背景
            ringb = grow(m, 28) & ~grow(m, 4) & ~ch & ~ui
            up = ringb.copy(); up[int(top + hh * 0.55):] = False
            if up.sum() > 50 and b50 is not None:
                e['back'] = rnd(float(np.median(Lh[up])), 1)
                e['sepBack'] = rnd(b50 - e['back'], 1); e['absSepBack'] = rnd(abs(b50 - e['back']), 1)
            # 接地の暗さ
            low = m[max(0, bot - int(0.12 * hh)):bot + 1]
            fx = np.nonzero(low)[1]
            if fx.size:
                fx0, fx1 = int(fx.min()), int(fx.max()); fw = max(1, fx1 - fx0)
                rows = slice(max(0, bot - 2), min(H, bot + 10))
                under = np.zeros((H, W), bool); under[rows, fx0:fx1] = True; under &= ~ch & ~ui
                side = np.zeros((H, W), bool)
                side[rows, max(0, fx0 - int(1.2 * fw)):max(0, fx0 - int(0.4 * fw))] = True
                side[rows, min(W, fx1 + int(0.4 * fw)):min(W, fx1 + int(1.2 * fw))] = True
                side &= ~ch & ~ui
                if under.sum() > 10 and side.sum() > 10:
                    e['contact'] = rnd(float(np.median(Lh[under])) / max(1.0, float(np.median(Lh[side]))), 2)
            # 彩度
            bc = Cimg[body]
            if bc.size > 50 and stageC:
                e['sat'] = rnd(float(np.median(bc)) / stageC, 3)
        seat = seats.get(key)
        e['feetY'] = rnd(seat['px'][1], 1) if seat else rnd(bot, 1)
        e['feetFrom'] = 'seats' if seat else 'キャラの画素の下端'
        out.append(e)
    r['units'] = out
    hero = next((e for e in out if e['kind'] == 'player'), None)
    enemies = [e for e in out if e['kind'] == 'enemy']
    cs = [e['contact'] for e in out if e['kind'] in ('player', 'enemy') and e.get('contact') is not None]
    if len(cs) >= 2:
        r['contactSpread'] = rnd(max(cs) - min(cs), 2)
    if hero and enemies:
        diffs = [abs(e['feetY'] - hero['feetY']) / sc for e in enemies]
        k = 'feetDiff12' if len(enemies) <= 2 else 'feetDiff34'
        r[k] = rnd(max(diffs), 1)
        r['feetDiffAll'] = [rnd(v, 1) for v in diffs]
        ef = [e['feetY'] for e in enemies]
        r['enemyFeetSpread'] = rnd((max(ef) - min(ef)) / sc, 1)
    if hero and hero.get('heightFrac'):
        r['heroHeightFrac'] = hero['heightFrac']
    big = [e for e in enemies if e.get('heightFrac')]
    if big:
        r['bossHeightFrac'] = max(e['heightFrac'] for e in big)
    return r


def measure_ref(key, ref_dirs):
    """本家は N16 だけ (hd2d-color.py の本家の矩形。キャラの画素は矩形なので背景を含む = 下限の目安)"""
    name = REF_FILES.get(key, key)
    path = None
    for d in ref_dirs:
        for ext in ('.jpg', '.png'):
            q = os.path.join(d, name + ext)
            if os.path.exists(q):
                path = q; break
        if path:
            break
    if not path:
        raise SystemExit('本家の画が無い: %s (--ref-dir で置き場を足す)' % name)
    c = COL.measure_ref(path, M.REF_PATCHES.get(name, {}))
    return dict(name=key, folder=os.path.dirname(path), image=os.path.basename(path), dev='PC', charMask='本家の矩形 (hd2d-color.py)',
                approx=True, units=[], satRatio=c.get('char_pop_C'), notes=['本家は N16 (satRatio) だけ。体・輪・接地は計画の表の値 (charChecks の honke) を見る'])

# ---- 合否


def get_key(d, key):
    cur = d
    for part in key.split('.'):
        if not isinstance(cur, dict) or part not in cur:
            return None
        cur = cur[part]
    return cur


def judge(r, targets):
    """[(id, label, 誰, 値, 範囲, 合否)]。当てはまらない物 (1〜2体の場面の feetDiff34 など) は出さない"""
    out = []
    dev = r.get('dev', 'PC')
    for c in (targets or {}).get('charChecks', []):
        rng = c.get(dev) if dev in c else c.get('all')
        if rng is None:
            continue
        who = c['who']
        if who == 'scene':
            subj = [('場面', r)]
        elif who == 'hero':
            subj = [(e['key'], e) for e in r.get('units', []) if e['kind'] == 'player']
        elif who == 'enemy':
            subj = [(e['key'] + ' ' + e['art'], e) for e in r.get('units', []) if e['kind'] == 'enemy']
        elif who == 'enemyDark':
            subj = [(e['key'] + ' ' + e['art'], e) for e in r.get('units', []) if e['kind'] == 'enemy' and e.get('cls') == 'dark']
        elif who == 'enemyWhite':
            subj = [(e['key'] + ' ' + e['art'], e) for e in r.get('units', []) if e['kind'] == 'enemy' and e.get('cls') == 'white']
        else:
            subj = []
        for label, d in subj:
            v = get_key(d, c['key'])
            if v is None:
                continue
            if r.get('approx') and c['key'] in ('body.p5', 'body.p25', 'contact', 'absSepBack', 'contactSpread'):
                continue   # 近似では出さない (体の中の小さな画素と足の形が分からない)
            lo, hi = rng
            ok = (lo is None or v >= lo) and (hi is None or v <= hi)
            out.append((c['id'], c['label'], label, v, rng, ok))
    return out


def fmt_rng(rng):
    lo, hi = rng
    if lo is None:
        return '≤%s' % hi
    if hi is None:
        return '≥%s' % lo
    return '%s〜%s' % (lo, hi)


def text_report(r, targets=None):
    L = ['== %s (%s・キャラ: %s・sc %s)' % (r['name'], r.get('dev'), r.get('charMask'), r.get('sc'))]
    for n in r.get('notes', []):
        L.append('  注: ' + n)
    for e in r.get('units', []):
        b = e.get('body') or {}
        L.append('  %-34s %-6s 背丈 %4spx (%s)  体 p5/25/50/95 %s/%s/%s/%s  輪 %s (差 %s・比 %s・門⑧ %s)  背景 %s (差 %s)  接地 %s  彩度比 %s  足 %s' % (
            (e['key'] + ' ' + e['art'])[:34], e.get('cls') or e['kind'], e.get('heightPx'), e.get('heightFrac'),
            b.get('p5'), b.get('p25'), b.get('p50'), b.get('p95'), e.get('ring'), e.get('bodyMinusRing'), e.get('ringRatio'),
            {1: '○', 0: '×'}.get(e.get('gate8'), '—'), e.get('back'), e.get('sepBack'), e.get('contact'), e.get('sat'), e.get('feetY')))
    L.append('  場面: 彩度比 (N16) %s・接地の開き %s・足の差 1〜2体 %s / 3体以上 %s (全員 %s)・敵の足の上下の開き %s・主人公の背丈 %s・いちばん大きい敵 %s' % (
        r.get('satRatio'), r.get('contactSpread'), r.get('feetDiff12'), r.get('feetDiff34'), r.get('feetDiffAll'), r.get('enemyFeetSpread'),
        r.get('heroHeightFrac'), r.get('bossHeightFrac')))
    if targets:
        res = judge(r, targets)
        for cid, lab, who, v, rng, ok in res:
            L.append('  %s %s  %s: %s [%s %s]' % ('○' if ok else '×', cid, who, v, fmt_rng(rng), lab))
        L.append('  合否: ○ %d・× %d' % (sum(1 for q in res if q[5]), sum(1 for q in res if not q[5])))
    return '\n'.join(L)


def short_who(who):
    """'enemy0 enemy_brute' → '敵1 (brute)'・'player leader_green' → '主人公'・'場面' → ''"""
    if who == '場面':
        return ''
    k, _, art = who.partition(' ')
    if k == 'player':
        return '主人公'
    if k.startswith('enemy') and k[5:].isdigit():
        return '敵%d (%s)' % (int(k[5:]) + 1, art.replace('enemy_', ''))
    return who


def md_table(results, targets):
    """行 = charChecks・列 = 場面。セルは「誰: 値 ○×」を並べる"""
    head = '| 物差し | 目標 | 本家 | W5 | ' + ' | '.join(r['name'] for r in results) + ' |'
    rows = [head, '|' + '---|' * (4 + len(results))]
    for c in (targets or {}).get('charChecks', []):
        rng = c.get('all') or c.get('PC')
        cells = []
        for r in results:
            js = [q for q in judge(r, {'charChecks': [c]})]
            cells.append('<br>'.join('%s %s%s' % (short_who(q[2]), q[3], ' ○' if q[5] else ' ×') for q in js) or '—')
        rows.append('| %s %s | %s | %s | %s | %s |' % (c['id'], c['label'], fmt_rng(rng) if rng else '—', c.get('honke', ''), c.get('w5', ''), ' | '.join(cells)))
    return '\n'.join(rows)


def all_scenes(folder):
    names = set()
    for p in glob.glob(os.path.join(folder, '*-1.png')):
        b = os.path.basename(p)[:-6]
        for suf in ('-hideui', '-uionly', '-unitsonly'):
            if b.endswith(suf):
                b = b[:-len(suf)]
        names.add(b)
    return sorted(names)


def load_targets(p):
    if p and os.path.exists(p):
        return json.load(open(p))
    return None


def main():
    ap = argparse.ArgumentParser(description='HD-2D 見本 二周目のキャラと敵の物差し (N13〜N17)')
    ap.add_argument('folder', nargs='?'); ap.add_argument('scenes', nargs='*')
    ap.add_argument('--all', help='フォルダの場面を全部')
    ap.add_argument('--ref', action='append', help='本家 (ot16 など。N16 だけ)')
    ap.add_argument('--ref-dir', action='append', default=[])
    ap.add_argument('--targets', default=DEFAULT_TARGETS)
    ap.add_argument('--json'); ap.add_argument('--md'); ap.add_argument('--quiet', action='store_true')
    a = ap.parse_args()
    targets = load_targets(a.targets)
    res = []
    for k in a.ref or []:
        res.append(measure_ref(k, a.ref_dir + DEFAULT_REF_DIRS))
    if a.all:
        for sc in all_scenes(a.all):
            try:
                res.append(measure_scene(a.all, sc, targets))
            except SystemExit as e:
                print('飛ばした: %s (%s)' % (sc, e), file=sys.stderr)
    if a.folder:
        for sc in a.scenes:
            res.append(measure_scene(a.folder, sc, targets))
    if not res:
        ap.error('測る場面が無い')
    if not a.quiet:
        for r in res:
            print(text_report(r, targets))
    if a.json:
        json.dump(res, open(a.json, 'w'), ensure_ascii=False, indent=1)
    if a.md and targets:
        open(a.md, 'w').write('# キャラと敵の物差し (hd2d-char-metrics.py・N13〜N17)\n\n' + md_table(res, targets) + '\n')


if __name__ == '__main__':
    main()

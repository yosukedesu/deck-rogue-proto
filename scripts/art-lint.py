#!/usr/bin/env python3
"""scripts/art-lint.py — 絵の規格の検査表 (2026-09-30 HD-2D 見本 P05 手順1。計画 docs/design/hd2d-slice-plan-2026-09-30.md)。

キャラ (リーダー・敵・人形) について、1枚ごとに:
  - 輪郭の最暗色 (outlineMin・outlineMedian): 透明に接する不透明の画素の輝度 (0〜255)。P23 の目安は最暗色 20〜30
  - 白の割合 (white235): 不透明の画素のうち輝度が 235 を超える割合
  - 描き込まれた光の向き (light): 明るい画素 (輝度の上位 20%) の重心 − 形の重心 を、絵の半分の大きさで割った向き。
    右上・左上…の8方位と大きさ (0.06 未満は「なし」)。舞台の月は左上の手前から (§2-4)
  - 台座の疑い (pedestal): 絵の下端から2ドット以内まで届く列が、横幅の 55% 以上 (足だけなら 20〜40%)
  - 左右反転 (mirrored): <id>.pixellab.json の mirrored か、rotated に「左右反転」
  - _KeyFlip の対象 (keyFlip): 画像ファイルを左右反転した絵 (焼き込まれた光の左右が逆)。光が右から描き込まれている絵は候補 (lightFromRight)
舞台の絵 (--palette を渡した時): 色表からの距離 (Oklab・ΔE×100) の 95 パーセンタイル (palP95)。キャラは写さない (対象外)。

使い方:
  python3 scripts/art-lint.py                                   # 既定: リーダー・敵・人形の全部と、Art/stage/act1 の舞台の絵
  python3 scripts/art-lint.py --palette docs/pixellab/hd2d-act1/palette-act1.json
  出力: docs/pixellab/hd2d-act1/art-lint.md (表)・art-lint.json (全部の値)・
        unity/Assets/Resources/Art/stage/act1/keyflip.json ({"keyflip": [...], "lightFromRight": [...]}。P11 が読む用)
"""
import argparse
import glob
import importlib.util
import json
import math
import os
import sys

import numpy as np
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..'))
ART = os.path.join(ROOT, 'unity', 'Assets', 'Resources', 'Art')
OUTDIR = os.path.join(ROOT, 'docs', 'pixellab', 'hd2d-act1')

sys.dont_write_bytecode = True   # scripts/__pycache__ を作らない (リポジトリを汚さない)
_spec = importlib.util.spec_from_file_location('stage_palette', os.path.join(ROOT, 'scripts', 'stage-palette.py'))
sp = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(sp)


def is_generated(path):
    b = os.path.splitext(os.path.basename(path))[0]
    return b.endswith('_n') or b.endswith('_e')


def characters():
    rows = []
    for f in sorted(glob.glob(os.path.join(ART, 'leaders', 'leader_*.png'))):
        if not is_generated(f) and not f.endswith('_icon.png'):
            rows.append(('leader', f))
    for f in sorted(glob.glob(os.path.join(ART, 'leaders', 'anim', '*.png'))):
        if not is_generated(f):
            rows.append(('leader-anim', f))
    for f in sorted(glob.glob(os.path.join(ART, 'enemies', '*.png'))):
        if not is_generated(f):
            rows.append(('enemy', f))
    for f in sorted(glob.glob(os.path.join(ART, 'dolls', '*.png'))):
        if not is_generated(f):
            rows.append(('doll', f))
    return rows


def stage_art():
    return sorted(f for f in glob.glob(os.path.join(ART, 'stage', 'act1', '**', '*.png'), recursive=True) if not is_generated(f))


def before_source(path):
    """置いた舞台の絵の、色表へ写す前の元。タイルは docs/pixellab/hd2d-act1/calm/、半立体は raw/relief-clean/ か Art/props/"""
    b = os.path.basename(path)
    if '/tiles/' in path.replace('\\', '/'):
        return os.path.join(OUTDIR, 'calm', b)
    cand = os.path.join(OUTDIR, 'raw', 'relief-clean', b)
    if os.path.exists(cand):
        return cand
    return os.path.join(ART, 'props', b)


def luma(rgb):
    return rgb[..., :3].astype(np.float64) @ np.array([0.299, 0.587, 0.114])


DIRS = ['右', '右上', '上', '左上', '左', '左下', '下', '右下']


def lint_character(path):
    a = np.array(Image.open(path).convert('RGBA'))
    al = a[..., 3] >= 128
    if not al.any():
        return {'empty': True}
    y = luma(a)
    ys, xs = np.nonzero(al)
    x0, x1, y0, y1 = xs.min(), xs.max(), ys.min(), ys.max()
    bw, bh = x1 - x0 + 1, y1 - y0 + 1
    # 輪郭 (4近傍に透明か絵の外)
    p = np.pad(al, 1)
    edge = al & ~(p[:-2, 1:-1] & p[2:, 1:-1] & p[1:-1, :-2] & p[1:-1, 2:])
    ol = y[edge]
    # 白
    yo = y[al]
    white = float((yo > 235).mean())
    # 光の向き
    thr = np.percentile(yo, 80)
    br = al & (y >= thr)
    cx, cy = xs.mean(), ys.mean()
    bys, bxs = np.nonzero(br)
    w = (y[br] - thr + 1.0)
    bx, by = (bxs * w).sum() / w.sum(), (bys * w).sum() / w.sum()
    dx = (bx - cx) / (bw / 2.0)
    dy = (cy - by) / (bh / 2.0)   # 上を +
    mag = math.hypot(dx, dy)
    ang = math.degrees(math.atan2(dy, dx)) % 360
    label = 'なし' if mag < 0.06 else DIRS[int(((ang + 22.5) % 360) // 45)]
    # 台座: 下端から2ドット以内まで届く列の割合
    lowest = np.full(a.shape[1], -1)
    for xx in range(x0, x1 + 1):
        col = np.nonzero(al[:, xx])[0]
        if len(col):
            lowest[xx] = col.max()
    reach = float(((lowest[x0:x1 + 1] >= y1 - 2)).mean())
    # 左右反転の記録
    meta_path = os.path.splitext(path)[0] + '.pixellab.json'
    mirrored = False
    if os.path.exists(meta_path):
        try:
            m = json.load(open(meta_path))
            mirrored = bool(m.get('mirrored')) or ('左右反転' in str(m.get('rotated', '')))
        except Exception:
            pass
    return {
        'size': [int(a.shape[1]), int(a.shape[0])], 'height': int(bh), 'width': int(bw),
        'outlineMin': round(float(ol.min()), 1), 'outlineMedian': round(float(np.median(ol)), 1),
        'white235': round(white, 4),
        'lightDx': round(dx, 3), 'lightDy': round(dy, 3), 'lightMag': round(mag, 3), 'lightDeg': round(ang, 1), 'light': label,
        'bottomReach': round(reach, 3), 'pedestal': reach >= 0.55,
        'mirrored': mirrored,
    }


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('--palette', default=os.path.join(OUTDIR, 'palette-act1.json'))
    ap.add_argument('--stage', nargs='*', default=None, help='色表の距離を測る舞台の絵 (省略時は Art/stage/act1 の全部)')
    ap.add_argument('--out', default=OUTDIR)
    ap.add_argument('--keyflip-out', default=os.path.join(ART, 'stage', 'act1', 'keyflip.json'))
    args = ap.parse_args()

    res = {'characters': {}, 'stage': {}}
    for cat, f in characters():
        rel = os.path.relpath(f, ART).replace('\\', '/')
        r = lint_character(f)
        r['category'] = cat
        res['characters'][rel] = r
    # アニメのコマは一枚絵の反転を引き継ぐ (コマの記録は1つの json にまとめてある)
    for rel, r in res['characters'].items():
        if r.get('category') == 'leader-anim':
            base = os.path.basename(rel).split('_')[:2]
            key = 'leaders/' + '_'.join(base) + '.png'
            if key in res['characters']:
                r['mirrored'] = res['characters'][key].get('mirrored', False)

    pal = None
    if args.palette and os.path.exists(args.palette):
        pal = sp.load_palette(args.palette)
        for f in (args.stage if args.stage is not None else stage_art()):
            a = np.array(Image.open(f).convert('RGBA'))
            d = sp.palette_distance(a, pal)
            rel = os.path.relpath(f, ART).replace('\\', '/') if f.startswith(ART) else os.path.relpath(f, ROOT)
            res['stage'][rel] = {
                'palP50': round(float(np.percentile(d, 50)), 2) if len(d) else None,
                'palP95': round(float(np.percentile(d, 95)), 2) if len(d) else None,
                'colors': int(len(np.unique(a[a[..., 3] >= 128][:, :3], axis=0))),
            }
            # 写す前の元 (index.json の from／tile-calm の出力) があれば、その距離も (はみ出しの一覧＝写した時にどれだけ色が動いたか)
            src = before_source(f)
            if src and os.path.exists(src):
                d0 = sp.palette_distance(np.array(Image.open(src).convert('RGBA')), pal)
                res['stage'][rel]['before'] = os.path.relpath(src, ROOT)
                res['stage'][rel]['beforeP95'] = round(float(np.percentile(d0, 95)), 2) if len(d0) else None

    def stem(rel):
        return os.path.splitext(os.path.basename(rel))[0]

    keyflip = sorted(stem(k) for k, v in res['characters'].items() if v.get('mirrored'))
    from_right = sorted(stem(k) for k, v in res['characters'].items()
                        if not v.get('empty') and v.get('lightMag', 0) >= 0.12 and v.get('lightDx', 0) >= 0.10 and v.get('category') != 'leader-anim')
    res['keyflip'] = keyflip
    res['lightFromRight'] = from_right

    os.makedirs(args.out, exist_ok=True)
    json.dump(res, open(os.path.join(args.out, 'art-lint.json'), 'w'), ensure_ascii=False, indent=1)
    os.makedirs(os.path.dirname(args.keyflip_out), exist_ok=True)
    json.dump({'_': 'scripts/art-lint.py が書く。keyflip = 画像ファイルを左右反転した絵 (StageUnitLit の _KeyFlip=1 の対象)。'
                    'lightFromRight = 光が右から描き込まれている絵 (反転はしていない。_KeyFlip の候補)。名前は Resources/Art の絵の名前 (拡張子なし)',
               'keyflip': keyflip, 'lightFromRight': from_right}, open(args.keyflip_out, 'w'), ensure_ascii=False, indent=1)

    # 表 (md)
    L = ['# art-lint (scripts/art-lint.py が書く。手で直さない)', '',
         '計画 docs/design/hd2d-slice-plan-2026-09-30.md P05 手順1。輝度は 0〜255 (0.299R+0.587G+0.114B)。光の向きは明るい画素の重心 − 形の重心 (上が +)。', '',
         f'- _KeyFlip の対象 (画像ファイルを左右反転した絵) {len(keyflip)} 枚: ' + ', '.join(keyflip),
         f'- 光が右から描き込まれている絵 (候補) {len(from_right)} 枚: ' + ', '.join(from_right), '']
    chars = [(k, v) for k, v in res['characters'].items() if not v.get('empty')]
    main_rows = [(k, v) for k, v in chars if v['category'] != 'leader-anim']
    agg = lambda key: np.array([v[key] for _, v in main_rows], dtype=float)
    L += ['## まとめ (リーダーの一枚絵・敵・人形。アニメのコマは除く)', '',
          f'- 枚数 {len(main_rows)}・輪郭の最暗色の中央値 {np.median(agg("outlineMin")):.1f} (範囲 {agg("outlineMin").min():.0f}〜{agg("outlineMin").max():.0f})・'
          f'20〜30 に入る絵 {int(((agg("outlineMin") >= 20) & (agg("outlineMin") <= 30)).sum())} 枚',
          f'- 白 (235超) を含む絵 {int((agg("white235") > 0).sum())} 枚・白の割合の中央値 {np.median(agg("white235")):.3%}',
          '- 光の向き: ' + '・'.join(f'{d} {sum(1 for _, v in main_rows if v["light"] == d)}' for d in DIRS + ['なし']),
          f'- 台座の疑い {sum(1 for _, v in main_rows if v["pedestal"])} 枚: ' + ', '.join(stem(k) for k, v in main_rows if v['pedestal']),
          '']
    L += ['## キャラ', '', '| 絵 | 種 | 寸法 | 背丈 | 輪郭の最暗 | 輪郭の中央 | 白235超 | 光の向き | 大きさ | 下端に届く列 | 台座 | 反転 |',
          '|---|---|---|---|---|---|---|---|---|---|---|---|']
    for k, v in chars:
        L.append(f"| {stem(k)} | {v['category']} | {v['size'][0]}×{v['size'][1]} | {v['height']} | {v['outlineMin']:.0f} | {v['outlineMedian']:.0f} | "
                 f"{v['white235']:.2%} | {v['light']} | {v['lightMag']:.2f} | {v['bottomReach']:.0%} | {'疑い' if v['pedestal'] else ''} | {'反転' if v['mirrored'] else ''} |")
    if res['stage']:
        L += ['', '## 舞台の絵 (色表からの距離。ΔE_ok×100)', '', f'色表: {os.path.relpath(args.palette, ROOT)} ({len(pal)} 色)。'
              '置いた絵は写してあるので距離 0 が正しい。「写す前 p95」が元の絵のはみ出し (大きいほど写した時に色が動いた)', '',
              '| 絵 | 色数 | 距離 p50 | 距離 p95 | 写す前 p95 | 元 |', '|---|---|---|---|---|---|']
        for k, v in res['stage'].items():
            L.append(f"| {k} | {v['colors']} | {v['palP50']} | {v['palP95']} | {v.get('beforeP95', '')} | {v.get('before', '')} |")
    open(os.path.join(args.out, 'art-lint.md'), 'w').write('\n'.join(L) + '\n')
    print(f'キャラ {len(chars)} 枚・舞台 {len(res["stage"])} 枚 → {os.path.relpath(args.out, ROOT)}/art-lint.md・.json')
    print(f'keyflip {len(keyflip)}: {", ".join(keyflip)}')
    print(f'lightFromRight {len(from_right)}')


if __name__ == '__main__':
    main()

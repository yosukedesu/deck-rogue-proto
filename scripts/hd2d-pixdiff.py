#!/usr/bin/env python3
"""hd2d-pixdiff.py — 2つの撮影フォルダの画を画素比べする (HD-2D 段2 の回帰。2026-10-03 レーン F。migrate の下書きの写し＋別名の対)。

規約 (scripts/hd2d-states/r3-old.txt の頭・hd2d-r2-sheet.py の r3_diff と同じ):
  画素の値の差 (RGB の最大) が 2 を超える画素の割合が 0.1% 以下なら「同じ画」。
  --specks を付けると、差の塊 (8近傍) のうち 40×40 に収まり 800 画素以下の物 (月の塵などの粒) を除いた割合でも判定する (r3_diff(specks=True) と同じ)。
  塊の数え方は scipy があればそれ、無ければこのファイルの中の素朴な数え方 (結果は同じ。差の画素が 20 万を超える時は粒を数えない = 下書きと同じ)。
合格の規約: 一覧の全枚が「同じ画」= 「差 >2 の画素が 0.1% 超」が 0 件 (計画 docs/design/hd2d-stage2-plan-2026-10-02.md §1・§3 手順 1)。

使い方:
  python3 scripts/hd2d-pixdiff.py <基準のフォルダ> <比べるフォルダ> [--specks] [--md 表.md] [--json 結果.json] [--diff-dir 差の絵の置き場] [--only 正規表現] [--strict]
      同じ名前の PNG どうしを比べる (pshots.sh の出力 = <名前>-<n>.png)。基準のフォルダの名前が基準。
  python3 scripts/hd2d-pixdiff.py <基準のフォルダ> <比べるフォルダ> --pairs 'PC-O2-squire:PC-M2-squire,PC-O3-quad:PC-M3-quad'
      別名の対 (場面の名前 = 末尾の -<n>.png を外した名前。<n> は 1,2,… を順に対にする。.png まで書けばその1枚どうし)。
      例: s2-mixed (旗で今の舞台に落ちる画) と s2-old23 (今の舞台) を比べる = 基準 s2-old23・比べる s2-mixed。
  --pairs @<組の名前> で下の PRESETS (s2-mixed の頭の「対」の写し)。--pairs-file <ファイル> は1行1対 (A:B。# と空行は読み飛ばす)。
  --thresh 2・--limit 0.001 で規約を変えられる (既定は上の規約)。
  終了コード 0 = 全枚合格・1 = 1枚でも不合格 (--strict なら対なしも不合格)・2 = 対になる画が1枚も無い
基準のフォルダにしか無い名前は「対なし」として表に出す (既定は不合格に数えない = 下書きと同じ。撮り損ねを落としたい時は --strict)。
比べるフォルダにしか無い画は表の下に名前だけ並べる。
"""
import argparse
import glob
import json
import os
import re
import sys

import numpy as np
from PIL import Image

THRESH = 2        # 値の差がこれを超える画素を数える
LIMIT = 0.001     # 0.1%
SPECK_BOX = 40    # 粒 = 40×40 に収まり
SPECK_PIX = 800   #      800 画素以下の差の塊
SPECK_CAP = 200000  # 差の画素がこれを超えたら粒を数えない (下書きと同じ)

# 別名の対の組 (s2-mixed.txt の頭の「対」の写し。基準 → 比べる)
PRESETS = {
    # 基準 = s2-old23 (今の舞台)・比べる = s2-mixed (旗で今の舞台に落ちる画)
    'mixed-old23': ['PC-O2-squire:PC-M2-squire', 'PC-O3-quad:PC-M3-quad', 'PC-O3-warden:PC-M3-warden',
                    'PH-O2-squire:PH-M2-squire', 'PH-O2-squire-tier:PH-M2-squire-tier', 'PH-O3-quad:PH-M3-quad',
                    'PH-O3-quad-tier:PH-M3-quad-tier', 'PC-O2-squire:PC-M2-squire-off', 'PC-O3-quad:PC-M3-quad-off'],
    # 同じ基準で「設計図か look_act<N> が無いのに旗で入れた」2 行 (幕2/3 の設計図を置く前だけ今の舞台と同じ画が合格。置いた後は箱庭になって違う画)
    'mixed-nolayout': ['PC-O2-squire:PC-M2-squire-nolayout', 'PC-O3-quad:PC-M3-quad-nolayout'],
    # 基準 = s2-apk (旗なし起動)・比べる = s2-mixed
    'mixed-apk': ['PC-A2-squire:PC-M2-squire', 'PC-A3-quad:PC-M3-quad', 'PH-A2-squire:PH-M2-squire', 'PH-A3-quad:PH-M3-quad'],
    # 基準 = s2-slice (幕1 の見本)・比べる = s2-mixed
    'mixed-slice': ['PC-S-wolf:PC-M1-wolf'],
}


# ---- 差の塊 (8近傍)

def _label_numpy(mask):
    """scipy が無い時の素朴な塊の数え方: 差の画素を行ごとの連続の区間に分け、上下の行の重なる区間 (8近傍 = 斜めも) を併合する"""
    H, W = mask.shape
    runs = []          # (y, x0, x1) 区間
    row_start = []     # 行ごとの区間の始まりの番号
    for y in range(H):
        row_start.append(len(runs))
        r = mask[y]
        if not r.any():
            continue
        d = np.diff(np.concatenate([[0], r.astype(np.int8), [0]]))
        s = np.nonzero(d == 1)[0]; e = np.nonzero(d == -1)[0]
        for a, b in zip(s, e):
            runs.append((y, int(a), int(b)))   # [a, b)
    row_start.append(len(runs))
    parent = list(range(len(runs)))

    def find(i):
        while parent[i] != i:
            parent[i] = parent[parent[i]]
            i = parent[i]
        return i
    for y in range(1, H):
        i0, i1 = row_start[y], row_start[y + 1]
        j0, j1 = row_start[y - 1], row_start[y]
        j = j0
        for i in range(i0, i1):
            _, a, b = runs[i]
            while j < j1 and runs[j][2] < a:     # 前の行の区間の終わり (b) が a-1 より左 = 重ならない (斜めは b == a で接する)
                j += 1
            k = j
            while k < j1 and runs[k][1] <= b:    # 前の行の区間の始まりが b 以下 (斜め込み) = 重なる
                ra, rb = find(i), find(k)
                if ra != rb:
                    parent[rb] = ra
                k += 1
    boxes = {}
    for i, (y, a, b) in enumerate(runs):
        r = find(i)
        if r not in boxes:
            boxes[r] = [y, y + 1, a, b, 0]
        bx = boxes[r]
        bx[0] = min(bx[0], y); bx[1] = max(bx[1], y + 1); bx[2] = min(bx[2], a); bx[3] = max(bx[3], b); bx[4] += b - a
    return [tuple(v) for v in boxes.values()]


def comps(mask):
    """差の塊ごとに (y0, y1, x0, x1, 画素数) (y1・x1 は末尾の次)"""
    try:
        from scipy import ndimage
        lab, n = ndimage.label(mask, structure=np.ones((3, 3), int))
        out = []
        for i, sl in enumerate(ndimage.find_objects(lab), 1):
            c = int((lab[sl] == i).sum())
            out.append((sl[0].start, sl[0].stop, sl[1].start, sl[1].stop, c))
        return out
    except ImportError:
        return _label_numpy(mask)


# ---- 比べる

def diff(a, b, specks, thresh=THRESH):
    """(差 >thresh の割合, 粒を除いた割合, 粒の数, 最大の差, 差の絵の元) か 大きさ違いなら None"""
    x = np.asarray(Image.open(a).convert('RGB')).astype(np.int16)
    y = np.asarray(Image.open(b).convert('RGB')).astype(np.int16)
    if x.shape != y.shape:
        return None
    dd = np.abs(x - y).max(-1)
    bad = dd > thresh
    frac = float(bad.mean())
    rest, n = int(bad.sum()), 0
    if specks and rest and rest < SPECK_CAP:
        for y0, y1, x0, x1, c in comps(bad):
            if y1 - y0 < SPECK_BOX and x1 - x0 < SPECK_BOX and c <= SPECK_PIX:
                n += 1; rest -= c
    return frac, rest / dd.size, n, int(dd.max()), dd


def scene_images(folder, scene):
    """場面の名前 → [<場面>-1.png, <場面>-2.png, …] (番号順)。.png まで書いてあればその1枚"""
    if scene.endswith('.png'):
        p = os.path.join(folder, scene)
        return [p] if os.path.exists(p) else []
    out = []
    for p in glob.glob(os.path.join(folder, glob.escape(scene) + '-*.png')):
        m = re.match(re.escape(scene) + r'-(\d+)\.png$', os.path.basename(p))
        if m:
            out.append((int(m.group(1)), p))
    return [p for _, p in sorted(out)]


def read_pairs(a):
    """--pairs / --pairs-file → [(基準の場面, 比べる場面)]"""
    items = []
    for spec in a.pairs or []:
        if spec.startswith('@'):
            key = spec[1:]
            if key not in PRESETS:
                raise SystemExit('組の名前が無い: %s (ある組: %s)' % (key, ', '.join(sorted(PRESETS))))
            items += PRESETS[key]
        else:
            items += [s for s in spec.split(',') if s.strip()]
    for f in a.pairs_file or []:
        for line in open(f, encoding='utf-8'):
            line = line.strip()
            if line and not line.startswith('#'):
                items.append(line)
    out = []
    for it in items:
        if ':' not in it:
            raise SystemExit('対の書き方は 基準:比べる (%s)' % it)
        p, q = it.split(':', 1)
        out.append((p.strip(), q.strip()))
    return out


def pct(v):
    return '' if v is None else '%.3f%%' % (v * 100)


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('base', help='基準のフォルダ (移行の前の撮影 s2-base/<組>)')
    ap.add_argument('new', help='比べるフォルダ (移行の後の撮影)')
    ap.add_argument('--specks', action='store_true', help='粒 (40×40 に収まり 800 画素以下の差の塊) を除いた割合でも判定する')
    ap.add_argument('--md', help='表を md に書く')
    ap.add_argument('--json', help='結果を JSON に書く')
    ap.add_argument('--diff-dir', help='不合格の画の差の絵 (差 ×8) を置く所')
    ap.add_argument('--only', help='基準の名前をこの正規表現で絞る')
    ap.add_argument('--pairs', action='append', help="別名の対 'A:B,C:D' か @組の名前 (%s)" % ', '.join(sorted(PRESETS)))
    ap.add_argument('--pairs-file', action='append', help='別名の対のファイル (1行1対 A:B)')
    ap.add_argument('--strict', action='store_true', help='対なし (比べる側に画が無い) も不合格に数える')
    ap.add_argument('--thresh', type=int, default=THRESH, help='数える値の差 (既定 2 = 差 >2)')
    ap.add_argument('--limit', type=float, default=LIMIT, help='合格の上限の割合 (既定 0.001 = 0.1%%)')
    a = ap.parse_args()
    for d in (a.base, a.new):
        if not os.path.isdir(d):
            raise SystemExit('フォルダが無い: %s' % d)

    # 比べる対の一覧 [(基準の画, 比べる画 or None, 見出し)]
    jobs = []
    pairs = read_pairs(a)
    if pairs:
        for p, q in pairs:
            if a.only and not re.search(a.only, p):
                continue
            ps, qs = scene_images(a.base, p), scene_images(a.new, q)
            if not ps:
                jobs.append((None, None, '%s ↔ %s' % (p, q), '基準に無い')); continue
            for i, pp in enumerate(ps):
                qq = qs[i] if i < len(qs) else None
                jobs.append((pp, qq, '%s ↔ %s' % (os.path.basename(pp), os.path.basename(qq) if qq else q + '-%d.png' % (i + 1)), None))
        used_new = set()
    else:
        names = sorted(os.path.basename(p) for p in glob.glob(os.path.join(a.base, '*.png')))
        if a.only:
            names = [n for n in names if re.search(a.only, n)]
        for n in names:
            q = os.path.join(a.new, n)
            jobs.append((os.path.join(a.base, n), q if os.path.exists(q) else None, n, None))
        used_new = set(names)

    rows, res, ng, npair, nmiss = [], [], 0, 0, 0
    for p, q, label, note in jobs:
        if note:
            rows.append((label, note, '', '', '', '')); nmiss += 1
            res.append(dict(label=label, verdict=note)); ng += 1 if a.strict else 0
            continue
        if q is None:
            rows.append((label, '対なし', '', '', '', '')); nmiss += 1
            res.append(dict(label=label, base=p, verdict='対なし')); ng += 1 if a.strict else 0
            continue
        r = diff(p, q, a.specks, a.thresh)
        if r is None:
            rows.append((label, '大きさ違い', '', '', '', '')); ng += 1
            res.append(dict(label=label, base=p, new=q, verdict='大きさ違い'))
            continue
        npair += 1
        frac, rest, nsp, mx, dd = r
        ok = (rest if a.specks else frac) <= a.limit
        if not ok:
            ng += 1
            if a.diff_dir:
                os.makedirs(a.diff_dir, exist_ok=True)
                Image.fromarray(np.clip(dd * 8, 0, 255).astype(np.uint8)).save(os.path.join(a.diff_dir, os.path.basename(q)))
        rows.append((label, 'OK' if ok else 'NG', pct(frac), pct(rest) if a.specks else '', str(nsp) if a.specks else '', str(mx)))
        res.append(dict(label=label, base=p, new=q, verdict='OK' if ok else 'NG', frac=frac, rest=rest if a.specks else None,
                        specks=nsp if a.specks else None, maxDiff=mx))
    extra = []
    if not pairs:
        extra = sorted(set(os.path.basename(p) for p in glob.glob(os.path.join(a.new, '*.png'))) - used_new)
    lines = ['| 画 | 判定 | 差>%d の割合 | 粒を除いた割合 | 粒の数 | 最大の差 |' % a.thresh, '|---|---|---|---|---|---|'] + \
            ['| %s | %s | %s | %s | %s | %s |' % r for r in rows]
    lines.append('')
    lines.append('対 %d 枚・不合格 %d 枚・対なし %d 枚%s・比べる側だけの画 %d 枚%s' % (
        npair, ng, nmiss, ' (--strict で不合格に数えた)' if a.strict and nmiss else '', len(extra),
        (' (%s%s)' % (', '.join(extra[:8]), ' …' if len(extra) > 8 else '')) if extra else ''))
    lines.append('規約: 値の差 >%d の画素の割合が %.3f%% 以下 = 同じ画%s' % (a.thresh, a.limit * 100, '・粒 (40×40・800 画素以下の差の塊) を除いた割合で判定' if a.specks else ''))
    text = '\n'.join(lines)
    print(text)
    if a.md:
        open(a.md, 'w', encoding='utf-8').write('# 画素比べ: %s → %s\n\n%s\n' % (a.base, a.new, text))
    if a.json:
        json.dump(dict(base=a.base, new=a.new, thresh=a.thresh, limit=a.limit, specks=a.specks, pairs=npair, ng=ng, missing=nmiss,
                       extra=extra, results=res), open(a.json, 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    sys.exit(0 if (npair and ng == 0) else (2 if not npair else 1))


if __name__ == '__main__':
    main()

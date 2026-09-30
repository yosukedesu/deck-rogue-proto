#!/usr/bin/env python3
"""scripts/tile-calm.py — PixelLab の地面・壁の絵を「光を受ける静かな面」の 64×64 タイルにする (2026-09-30 HD-2D 見本 P05)。

計画 docs/design/hd2d-slice-plan-2026-09-30.md §2-2「地面は光を受ける静かな面にする。明暗の幅を今の約1/3にし、
白い小石の粒と写実的なひびは捨てる」・P05 手順3「tile-calm（明暗1/3・彩度−20%・白い粒を消す・継ぎ目の検査）」。

流れ (1枚ごと):
  1. 切り出し: 160×160 なら中央 128 を 64×4 (a b / c d)、96 なら中央 64、64 ならそのまま
     (PixelLab は縁に額・暗い縁取りを描きがち＝外周を捨てる。memory pixellab-stage-lessons)
  2. 焼き込まれた大きな明暗の勾配を消す (Oklab の L から、巻き戻しの箱ぼかし半径 R を引いて平均を足す＝highpass)
  3. 白い粒を消す: まわり 5×5 の中央値より L が --speck 以上明るい画素を、その中央値の色へ
  4. 明暗の幅を --contrast 倍 (既定 1/3)・彩度 (Oklab の a,b) を --sat 倍 (既定 0.8)
  5. つなぎ目: 半分ずらした絵 (縁が必ずつながる) を土台にし、十字の継ぎ目の帯を縦だけ・横だけずらした絵と元の絵で
     埋める。帯の境目は両側の差が最も小さい道 (画像のキルティング)。色を混ぜない＝ドットは元の絵の色のまま
  6. 検査: ラプラシアン分散 (輝度 0〜255) ≤ --max-lap (既定 800)・向かい合う縁の平均輝度の差 ≤ 2 レベル・
     継ぎ目の段差の比 (縁をまたぐ差 ÷ 中の隣どうしの差) を表に出す

使い方:
  python3 scripts/tile-calm.py --outdir unity/Assets/Resources/Art/stage/act1/tiles --name top_grass docs/pixellab/hd2d-act1/raw/tiles/top_grass_s1.png
      → top_grass_a.png … top_grass_d.png (160 の入力なら4枚) と、--sheet で 2×2 に敷いた確認シート
  python3 scripts/tile-calm.py --check <64×64 の png...>    # 手を加えずに検査だけ
  python3 scripts/tile-calm.py --trim 3 --name top_grass_flat --outdir … raw/trial/trial_tilespro_top_0.png   # tiles-pro の1枚 (縁の案内線を消す)
  オプション: --contrast 0.333 --sat 0.8 --radius 12 --speck 0.10 --band 14 --max-lap 800 (超えたら明暗の幅を自動で絞る。--no-auto で切る・--min-contrast 0.15) --sheet <png> --json <path>
"""
import argparse
import importlib.util
import json
import os
import sys

import numpy as np
from PIL import Image, ImageDraw

_here = os.path.dirname(os.path.abspath(__file__))
sys.dont_write_bytecode = True   # scripts/__pycache__ を作らない (リポジトリを汚さない)
_spec = importlib.util.spec_from_file_location('stage_palette', os.path.join(_here, 'stage-palette.py'))
sp = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(sp)


def luma(rgb):
    rgb = np.asarray(rgb, dtype=np.float64)
    return 0.299 * rgb[..., 0] + 0.587 * rgb[..., 1] + 0.114 * rgb[..., 2]


def box_blur_wrap(x, r):
    """巻き戻し (トーラス) の箱ぼかし。2回掛けて三角に近づける"""
    out = x.astype(np.float64)
    for _ in range(2):
        acc = np.zeros_like(out)
        for d in range(-r, r + 1):
            acc += np.roll(out, d, axis=1)
        out = acc / (2 * r + 1)
        acc = np.zeros_like(out)
        for d in range(-r, r + 1):
            acc += np.roll(out, d, axis=0)
        out = acc / (2 * r + 1)
    return out


def median5_wrap(x):
    stack = [np.roll(np.roll(x, dy, 0), dx, 1) for dy in range(-2, 3) for dx in range(-2, 3)]
    return np.median(np.stack(stack, 0), 0)


def trim_border(img, n):
    """外周 n ドットを、内側の絵の鏡写しで埋める (tiles-pro の右と下の縁に残る 1〜3px の案内線を消す。2026-09-30 試し)"""
    if n <= 0:
        return img
    inner = img[n:-n, n:-n]
    return np.pad(inner, ((n, n), (n, n), (0, 0)), mode='reflect')


def crop_origins(img):
    """(名前, 左上 x, 左上 y, ずらしてよい幅) の一覧"""
    h, w = img.shape[:2]
    if (w, h) == (160, 160):
        return [('a', 16, 16, 6), ('b', 80, 16, 6), ('c', 16, 80, 6), ('d', 80, 80, 6)]
    if (w, h) == (128, 128):
        return [('a', 0, 0, 0), ('b', 64, 0, 0), ('c', 0, 64, 0), ('d', 64, 64, 0)]
    if (w, h) == (96, 96):
        return [('a', 16, 16, 12)]
    if (w, h) == (64, 64):
        return [('a', 0, 0, 0)]
    sys.exit(f'大きさ {w}×{h} は扱わない (160・128・96・64)')


def crops(img):
    """切り出し。つなぎ目 (make_seamless) で縁になるのは切り出しの真ん中の列 31|32 と行 31|32 なので、
    そこの段が最も小さくなるように切り出しの位置を ±slack だけずらす (明るさの段がそのまま敷いた時の継ぎ目になるため)"""
    L = sp.rgb8_to_oklab(img[..., :3].astype(np.float64))[..., 0]
    out = []
    for name, x0, y0, slack in crop_origins(img):
        best = None
        for dy in range(-slack, slack + 1):
            for dx in range(-slack, slack + 1):
                x, y = x0 + dx, y0 + dy
                if x < 0 or y < 0 or x + 64 > img.shape[1] or y + 64 > img.shape[0]:
                    continue
                c = L[y:y + 64, x:x + 64]
                e = np.abs(c[:, 31] - c[:, 32]).mean() + np.abs(c[31, :] - c[32, :]).mean() \
                    + abs(c[:, 31].mean() - c[:, 32].mean()) * 2 + abs(c[31, :].mean() - c[32, :].mean()) * 2
                if best is None or e < best[0]:
                    best = (e, x, y)
        _, x, y = best
        out.append((name, img[y:y + 64, x:x + 64]))
    return out


def min_cut_path(cost, cyclic=True):
    """cost (N×W) の上から下へ、1段ごとに横へ高々1しか動かない最小の道 (画像のキルティングの境目)。
    cyclic なら最後の段と最初の段の位置の差も1以内 (巻き戻しでつながる)。戻り値は各段の列 (0..W-1)"""
    n, w = cost.shape
    best, best_path = None, None
    starts = range(w) if cyclic else [None]
    for s in starts:
        acc = np.full((n, w), np.inf)
        back = np.zeros((n, w), np.int64)
        if s is None:
            acc[0] = cost[0]
        else:
            acc[0, s] = cost[0, s]
        for y in range(1, n):
            for x in range(w):
                lo, hi = max(0, x - 1), min(w, x + 2)
                j = lo + int(np.argmin(acc[y - 1, lo:hi]))
                acc[y, x] = acc[y - 1, j] + cost[y, x]
                back[y, x] = j
        last = acc[-1].copy()
        if s is not None:
            ok = np.abs(np.arange(w) - s) <= 1
            last[~ok] = np.inf
        x = int(np.argmin(last))
        total = last[x]
        if best is None or total < best:
            path = [x]
            for y in range(n - 1, 0, -1):
                x = back[y, x]
                path.append(x)
            best, best_path = total, path[::-1]
    return np.array(best_path)


def make_seamless(tile, band, seed=0):
    """tile (64×64×C) → 縁がつながる 64×64 (画像のキルティング。色を混ぜない＝ドットは元の絵の色のまま)。
    4枚の写しを使う: base=縦横とも半分ずらす (縁はつながる・十字に継ぎ目) / hs=縦だけずらす (縦の帯に・上下の縁がつながる) /
    vs=横だけずらす (横の帯に・左右の縁がつながる) / tile=元の絵 (帯の交わる真ん中に)。
    帯の境目は、両側の差が最も小さい道を選ぶ (1段に横へ高々1・巻き戻しでつながる)。band は境目を探す幅 (中心から)"""
    n = tile.shape[0]
    c = n // 2
    h = n // 2
    base = np.roll(np.roll(tile, h, 0), h, 1)
    hs = np.roll(tile, h, 0)
    vs = np.roll(tile, h, 1)
    f = lambda a: sp.rgb8_to_oklab(a[..., :3].astype(np.float64))
    lb, lhs, lvs, lt = f(base), f(hs), f(vs), f(tile)
    b = int(max(3, min(c - 2, band)))
    L0, L1 = c - b, c - 1          # 左の境目の窓 (この列から帯)
    R0, R1 = c + 1, c + b          # 右の境目の窓 (この列から外)
    diff = lambda p, q: ((p - q) ** 2).sum(-1)
    # 縦の帯の境目: 外 base と中 hs。ただし帯の交わる行では中は tile なので、その行は base と tile の差も見る
    dv = diff(lb, lhs)
    left = min_cut_path(dv[:, L0:L1 + 1]) + L0
    right = min_cut_path(dv[:, R0:R1 + 1]) + R0
    # 横の帯の境目: 外 base と中 vs (縦の帯の内側の列では外 hs・中 tile)
    dh = diff(lb, lvs).T.copy()               # 行と列を入れ替えて「上から下」の道にする
    inner = diff(lhs, lt).T
    colmask = np.zeros(n, bool)
    colmask[int(np.min(left)):int(np.max(right))] = True
    dh[colmask] = inner[colmask]
    top = min_cut_path(dh[:, L0:L1 + 1]) + L0
    bot = min_cut_path(dh[:, R0:R1 + 1]) + R0
    yy, xx = np.mgrid[0:n, 0:n]
    in_v = (xx >= left[yy]) & (xx < right[yy])
    in_h = (yy >= top[xx]) & (yy < bot[xx])
    out = base.copy()
    out[in_v & ~in_h] = hs[in_v & ~in_h]
    out[in_h & ~in_v] = vs[in_h & ~in_v]
    out[in_v & in_h] = tile[in_v & in_h]
    return out


def calm(tile_rgba, contrast, sat, radius, speck, band, seed, target_lap=None, min_contrast=0.15):
    rgb = tile_rgba[..., :3].astype(np.float64)
    lab = sp.rgb8_to_oklab(rgb)
    L = lab[..., 0]
    mean_L = L.mean()
    # 2. 大きな勾配を消す
    L = L - box_blur_wrap(L, radius) + mean_L
    # 3. 白い粒: 中央値より speck 以上明るい画素は中央値へ (a,b も中央値へ)
    med_L = median5_wrap(L)
    spk = (L - med_L) > speck
    if spk.any():
        L = np.where(spk, med_L, L)
        for ch in (1, 2):
            mc = median5_wrap(lab[..., ch])
            lab[..., ch] = np.where(spk, mc, lab[..., ch])
    # 4. 明暗の幅・彩度 (target_lap があれば、ラプラシアン分散がそれ以下になるまで明暗の幅をさらに絞る。下限 min_contrast)
    lab[..., 1] *= sat
    lab[..., 2] *= sat
    Lc = L.copy()
    used = contrast
    for _ in range(4):
        lab[..., 0] = mean_L + (Lc - mean_L) * used
        out = np.dstack([sp.oklab_to_rgb8(lab), np.full(L.shape, 255, np.uint8)])
        if not target_lap:
            break
        lv = check(out)['lapvar']
        if lv <= target_lap or used <= min_contrast + 1e-6:
            break
        used = max(min_contrast, used * np.sqrt(target_lap / lv) * 0.97)
    # 5. つなぎ目
    out = make_seamless(out, band, seed)
    return out.astype(np.uint8), int(spk.sum()), float(used)


def check(tile_rgba):
    y = luma(tile_rgba[..., :3])
    lap = 4 * y[1:-1, 1:-1] - y[:-2, 1:-1] - y[2:, 1:-1] - y[1:-1, :-2] - y[1:-1, 2:]
    inner_dx = np.abs(np.diff(y, axis=1)).mean()
    inner_dy = np.abs(np.diff(y, axis=0)).mean()
    seam_x = np.abs(y[:, 0] - y[:, -1]).mean()
    seam_y = np.abs(y[0, :] - y[-1, :]).mean()
    # 端の差: 敷いた時の継ぎ目の段＝右端の列と左端の列 (下端と上端の行) の平均輝度の差。中の隣どうしの列の差の 90 パーセンタイルも
    # 出し、段が中の普通の段より大きくなければ合格にする (縦の筋の多い樹皮・板は隣の列でも 2 レベル以上ずれるため)
    colm, rowm = y.mean(0), y.mean(1)
    return {
        'lapvar': round(float(lap.var()), 1),
        'edgeMeanDiffLR': round(float(abs(colm[0] - colm[-1])), 2),
        'edgeMeanDiffTB': round(float(abs(rowm[0] - rowm[-1])), 2),
        'innerStepP90X': round(float(np.percentile(np.abs(np.diff(colm)), 90)), 2),
        'innerStepP90Y': round(float(np.percentile(np.abs(np.diff(rowm)), 90)), 2),
        'bandDiffLR': round(float(abs(y[:, :8].mean() - y[:, -8:].mean())), 2),
        'bandDiffTB': round(float(abs(y[:8, :].mean() - y[-8:, :].mean())), 2),
        'seamRatioX': round(float(seam_x / max(inner_dx, 1e-6)), 2),
        'seamRatioY': round(float(seam_y / max(inner_dy, 1e-6)), 2),
        'lumaMean': round(float(y.mean()), 1),
        'lumaStd': round(float(y.std()), 1),
        'colors': int(len(np.unique(tile_rgba[..., :3].reshape(-1, 3), axis=0))),
    }


def verdict(c, max_lap):
    ok_lap = c['lapvar'] <= max_lap
    ok_edge = (c['edgeMeanDiffLR'] <= max(2.0, c['innerStepP90X'])) and (c['edgeMeanDiffTB'] <= max(2.0, c['innerStepP90Y']))
    ok_seam = c['seamRatioX'] <= 1.6 and c['seamRatioY'] <= 1.6
    c['pass'] = bool(ok_lap and ok_edge and ok_seam)
    c['fail'] = [k for k, v in (('lapvar', ok_lap), ('edge', ok_edge), ('seam', ok_seam)) if not v]
    return c


def sheet(rows, out, scale=3):
    """rows: [(name, tile)] を 2×2 に敷いて並べる (継ぎ目の目視用)"""
    cell = 128 * scale
    cols = 4
    r = (len(rows) + cols - 1) // cols
    im = Image.new('RGB', (cols * (cell + 8), r * (cell + 22)), (36, 36, 36))
    d = ImageDraw.Draw(im)
    for i, (name, t) in enumerate(rows):
        rep = np.tile(t[..., :3], (2, 2, 1))
        p = Image.fromarray(rep.astype(np.uint8), 'RGB').resize((cell, cell), Image.NEAREST)
        x, y = (i % cols) * (cell + 8), (i // cols) * (cell + 22)
        im.paste(p, (x, y + 18))
        d.text((x + 3, y + 3), name, fill=(255, 230, 90))
    os.makedirs(os.path.dirname(out) or '.', exist_ok=True)
    im.save(out)


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('files', nargs='+')
    ap.add_argument('--outdir')
    ap.add_argument('--name', help='出力の名前の頭 (<name>_a.png …)。省略時は入力の名前から _s1 などを落とした物')
    ap.add_argument('--check', action='store_true', help='手を加えずに検査だけ')
    ap.add_argument('--contrast', type=float, default=1 / 3)
    ap.add_argument('--sat', type=float, default=0.8)
    ap.add_argument('--radius', type=int, default=12)
    ap.add_argument('--speck', type=float, default=0.10, help='白い粒とみなす明るさの差 (Oklab L・0〜1)')
    ap.add_argument('--band', type=float, default=14.0, help='継ぎ目を隠す帯の境目を探す幅 (中心から・px)')
    ap.add_argument('--max-lap', type=float, default=800.0)
    ap.add_argument('--trim', type=int, default=0, help='外周 N ドットを内側の鏡写しで埋めてから処理する (tiles-pro の案内線)')
    ap.add_argument('--min-contrast', type=float, default=0.15, help='明暗の幅を自動で絞る時の下限')
    ap.add_argument('--no-auto', action='store_true', help='ラプラシアン分散に合わせて明暗の幅を絞らない (--contrast のまま)')
    ap.add_argument('--sheet')
    ap.add_argument('--json')
    args = ap.parse_args()

    results = {}
    rows = []
    if args.check:
        for f in args.files:
            a = np.array(Image.open(f).convert('RGBA'))
            c = verdict(check(a), args.max_lap)
            results[os.path.basename(f)] = c
            rows.append((os.path.basename(f), a))
            print(os.path.basename(f), json.dumps(c, ensure_ascii=False))
    else:
        if not args.outdir:
            sys.exit('--outdir が要る')
        os.makedirs(args.outdir, exist_ok=True)
        used_letters = {}
        for f in args.files:
            a = trim_border(np.array(Image.open(f).convert('RGBA')), args.trim)
            base = args.name or os.path.splitext(os.path.basename(f))[0].rsplit('_s', 1)[0]
            for k, (suffix, t) in enumerate(crops(a)):
                # 同じ名前に 64 の入力を何枚も渡した時は a, b, c… と順に振る (上書きしない)
                n_used = used_letters.get(base, 0)
                suffix = 'abcdefghijklmnop'[n_used]
                used_letters[base] = n_used + 1
                seed = (sum(map(ord, base + suffix)) * 7919) % (2 ** 31)   # 名前から決まる種 (同じ入力なら同じ出力)
                o, nspk, used = calm(t, args.contrast, args.sat, args.radius, args.speck, args.band, seed,
                                     target_lap=None if args.no_auto else args.max_lap * 0.8, min_contrast=args.min_contrast)
                name = f'{base}_{suffix}.png'
                Image.fromarray(o, 'RGBA').save(os.path.join(args.outdir, name))
                c = verdict(check(o), args.max_lap)
                c['src'] = os.path.relpath(f)
                c['specksRemoved'] = nspk
                c['contrastUsed'] = round(used, 3)
                c['before'] = check(t)
                results[name] = c
                rows.append((name, o))
                print(name, 'OK' if c['pass'] else 'NG ' + ','.join(c['fail']), json.dumps({k2: c[k2] for k2 in ('lapvar', 'edgeMeanDiffLR', 'edgeMeanDiffTB', 'seamRatioX', 'seamRatioY', 'colors', 'contrastUsed')}), '(元 lapvar', c['before']['lapvar'], ')')
    if args.sheet:
        sheet(rows, args.sheet)
    if args.json:
        prev = json.load(open(args.json)) if os.path.exists(args.json) else {}
        prev.update(results)
        json.dump(prev, open(args.json, 'w'), ensure_ascii=False, indent=1)


if __name__ == '__main__':
    main()

#!/usr/bin/env python3
"""敵と白の人形の待機のコマ (二周目 レーン E の 7。計画 docs/design/hd2d-round2-plan-2026-10-01.md §2 レーン E・char C8)。

一枚絵 (と法線 _n・発光 _e) から「境目の行より上を1ドット上へずらした」コマを作り、
元・上・上・元 の4コマを Resources/Art/enemies/anim/<名前>_r2idle_<0..3>(.png / _n.png / _e.png) へ書く。
StageUnits.BindUnit は 箱庭 (stage=diorama) かつ look の char.r2idle がある時だけ r2idle を読む (今の舞台 = stage=old はコードの上下のまま)。
人形 (Art/dolls) のコマも enemies/anim へ置く (BindUnit の AnimFolder は player 以外 enemies/anim)。

ずらし方 (1列ずつ):
  列 x の境目の行 b(x) より上 (y < b(x)) を1ドット上へ動かし、行 b(x)−1 は元の行を残す (= その列が1ドット伸びる。隙間は出ない)。
  足元 (y ≥ b(x)) は4コマで1画素も変わらない。どの列も同じ1ドットだけ動くので、縦の段 (列ごとのずれ) は出ない。
  目に見える傷は「境目の行に横向きの線があると2ドットに太る」だけ = 境目は脚・腕・胴の脇のような縦の線を横切る行に置く。
  境目の表 SEAMS は 敵ごとに必ず持つ (四つ足の狼は胴と頭が上がり脚が伸びる。オーガは棍棒の上)。表に無い絵は作らない。
  b(x) は 1つの数 (全部の列が同じ行) か [[x, 行], …] の折れ線 (x の間は線形・端はそのまま)。行は PNG の上が 0。
  列を絞る時は "cols": [x0, x1] (この範囲の外は動かない。範囲の端の列と外の列の間に中身がつながっていると段になるので、スクリプトが数えて止める)。

確かめ (スクリプトが止める):
  ① いちばん上の動く行 (y=0) に中身があれば作らない (上へ押し出して欠ける)
  ② 足元 (いちばん下の不透明の行から上 4 行) が境目より上に掛かっていれば作らない
  ③ cols の端で中身がつながっている行があれば作らない (段)
  ④ 横線の太り (境目の行が上下の行とどれだけ違うか) を数えて表に出す。閾値を超える絵は作らない (表から外す目安)
使い方:
  python3 scripts/gen-enemy-idle.py               … 表の全部を書く (書いた数と各絵の太りの数を出す)
  python3 scripts/gen-enemy-idle.py --dry          … 書かずに確かめだけ
  python3 scripts/gen-enemy-idle.py --sheet OUT.png [名前 …]   … 元・上 を3倍で並べた確かめのシート (境目を赤、動いた画素を差分で)
  python3 scripts/gen-enemy-idle.py --suggest 名前 … 境目の候補の行 (太りが少ない順) を出す
"""
import argparse, json, os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
ART = os.path.join(ROOT, 'unity/Assets/Resources/Art')
OUT = os.path.join(ART, 'enemies/anim')
STEM = 'r2idle'

# 境目の表 (PNG の行・上が 0)。note = どこで切ったか (3倍のシートで確かめた)
SEAMS = {
    # ---- 撮影に出る敵 (狼・オーガとスマホの 96・探り屋・妖術師・太鼓・噛みつく巻物)
    'enemy_wolf':          {'row': [[8, 43], [30, 43], [31, 42], [35, 42], [36, 43], [55, 43]], 'note': '四つ足: 前脚と後ろ脚の途中 = 胴・頭・尾が上がり、脚が1ドット伸びる'},
    'enemy_brute':         {'row': [[10, 74], [26, 74], [27, 73], [117, 73]], 'note': '棍棒と手の上 (肩・頭・胸が上がり、二の腕が伸びる。棍棒と手と脚は動かない)'},
    'enemy_brute_96':      {'row': [[11, 55], [23, 55], [24, 54], [25, 53], [26, 52], [55, 52], [56, 53], [68, 53], [69, 54], [70, 55], [91, 55]], 'note': 'スマホの 96。128 と同じ割合 (棍棒の上)'},
    'enemy_hexer':         {'row': [[16, 40], [28, 40], [29, 41], [53, 41]], 'note': '木の実を抱えた腕の下・腹'},
    'enemy_drummer':       {'row': [[3, 49], [33, 49], [34, 50], [50, 50]], 'note': '太鼓の下の方・足の上 (肩から下げた太鼓も一緒に上がる)'},
    'enemy_biting_scroll': {'row': 42, 'note': '巻物の下・二本脚の途中'},
    'enemy_probe':         {'row': [[6, 44], [18, 44], [19, 43], [43, 43], [44, 42], [51, 42]], 'note': '腹の下・足の上 (杖も上がる)'},
    # ---- 幕1 の本帯・弱枠・幕ボス・強個体
    'enemy_mudling':       {'row': 39, 'note': '泥の裾の上'},
    'enemy_mud_lump':      {'row': [[7, 37], [25, 37], [26, 38], [27, 37], [31, 37], [32, 36], [54, 36]], 'note': '胴の中ほど'},
    'enemy_slug':          {'row': [[8, 42], [23, 42], [24, 43], [25, 44], [38, 44], [39, 43], [51, 43], [52, 44], [55, 44]], 'note': '蜥蜴の脚の付け根'},
    'enemy_thorn_squirrel': {'row': 50, 'note': '足の上'},
    'enemy_cultist':       {'row': [[4, 37], [19, 37], [20, 38], [23, 38], [24, 39], [29, 39], [30, 38], [59, 38]], 'note': '外套の中ほど'},
    'enemy_sludge_spider': {'row': 46, 'note': '脚の途中 (胴と脚の付け根が上がる)'},
    'enemy_snap_fruit':    {'row': 35, 'note': '洋梨の胴の中ほど (縦の輪郭を横切る)'},
    'enemy_spore_cap':     {'row': 45, 'note': '足の上'},
    'enemy_thief':         {'row': 47, 'note': '袋の下の方'},
    'enemy_wide_power':    {'row': [[6, 47], [12, 47], [13, 48], [27, 48], [28, 49], [56, 49]], 'note': '足の上'},
    'enemy_apprentice_colossus': {'row': [[18, 51], [26, 51], [27, 50], [55, 50]], 'note': '膝'},
    'enemy_cog_construct': {'row': 34, 'note': '箱の頭の下 (首と胴)'},
    'enemy_gaping_maw':    {'row': [[9, 34], [41, 34], [42, 35], [43, 34], [60, 34]], 'note': '口の下・腹'},
    'enemy_iron_clam':     {'row': [[9, 37], [17, 37], [18, 38], [45, 38], [46, 39], [60, 39]], 'note': '殻の合わせ目の上'},
    'enemy_haze_stag':     {'row': 89, 'note': '脚の途中 (128)'},
    'enemy_haze_stag_96':  {'row': [[6, 57], [66, 57], [67, 56], [88, 56]], 'note': '脚の途中 (スマホの 96)'},
    'enemy_strangler_serpent': {'row': [[6, 35], [33, 35], [34, 36], [35, 35], [57, 35]], 'note': 'とぐろの上'},
    'enemy_vine_walker':   {'row': 40, 'note': '幹の中ほど'},
    'enemy_mimic_imp':     {'row': [[8, 46], [44, 46], [45, 47], [55, 47]], 'note': '足の上'},
    'enemy_kin_priest':    {'row': 60, 'note': '衣の中ほど'},
    'enemy_kin_follower':  {'row': 67, 'note': '衣の下の方'},
    'enemy_elite_sergeant': {'row': [[5, 50], [69, 50], [70, 51], [72, 51]], 'note': '腰'},
    'enemy_elite_sentry':  {'row': 49, 'note': '胴の下'},
    'enemy_elite_gold_raven': {'row': 58, 'note': '脚の途中'},
    'enemy_elite_devourer': {'row': 50, 'note': '毛玉の下の方'},
    # ---- 白の人形 (32 ドット。横線が太る絵 = 楽隊・旗手・弓兵・楽長・修道士は作らない)
    'white_perm_squire':   {'row': [[5, 24], [10, 24], [11, 23], [27, 23]], 'note': '脚の途中'},
    'white_perm_shieldmaiden': {'row': [[5, 20], [15, 20], [16, 19], [20, 19], [21, 20], [24, 20]], 'note': '盾の中ほど・腰'},
    'white_perm_choir':    {'row': [[5, 19], [19, 19], [20, 20], [27, 20]], 'note': '衣の中ほど'},
    'white_perm_hound':    {'row': [[6, 24], [14, 24], [15, 23], [26, 23]], 'note': '脚の途中'},
    'white_perm_bonfire':  {'row': [[5, 16], [17, 16], [18, 17], [26, 17]], 'note': '炎の下'},
    'white_perm_candle':   {'row': [[5, 24], [21, 24], [22, 25], [26, 25]], 'note': '脚の途中'},
    'white_perm_dragon':   {'row': [[3, 31], [25, 31], [26, 32], [27, 31], [36, 31], [37, 30], [39, 30]], 'note': '脚の付け根 (48)'},
    'white_perm_lantern':  {'row': [[3, 23], [12, 23], [13, 22], [27, 22]], 'note': '腰'},
    'white_perm_lion':     {'row': [[6, 30], [21, 30], [22, 31], [23, 32], [26, 32], [27, 31], [44, 31]], 'note': '脚の付け根 (48)'},
    'white_perm_page':     {'row': [[6, 24], [17, 24], [18, 23], [21, 23]], 'note': '脚の途中'},
}

MAX_THICKEN = 0.15   # 境目の行が上下どちらの行とも違う (1ドットの横線が2ドットに太る) 列の割合の上限


def load(path):
    if not os.path.exists(path):
        return None
    return np.asarray(Image.open(path).convert('RGBA')).copy()


def folder_of(name):
    return 'dolls' if name.startswith('white_perm') else 'enemies'


def seam_rows(entry, w):
    """列ごとの境目の行 (長さ w の整数配列)"""
    r = entry['row']
    if isinstance(r, (int, float)):
        return np.full(w, int(r), dtype=int)
    pts = sorted((int(x), int(y)) for x, y in r)
    xs = [p[0] for p in pts]; ys = [p[1] for p in pts]
    return np.round(np.interp(np.arange(w), xs, ys)).astype(int)


def cols_mask(entry, w):
    c = entry.get('cols')
    m = np.zeros(w, dtype=bool)
    if c is None:
        m[:] = True
    else:
        m[max(0, c[0]):min(w, c[1] + 1)] = True
    return m


def shift_up(img, b, cm):
    """列 x の y < b[x] を1ドット上へ。行 b[x]−1 は元のまま (伸びる)。cm = 動かす列"""
    out = img.copy()
    h, w = img.shape[:2]
    for x in range(w):
        if not cm[x]:
            continue
        bx = int(b[x])
        if bx <= 1:
            continue
        bx = min(bx, h)
        out[0:bx - 1, x] = img[1:bx, x]
        out[bx - 1, x] = img[bx - 1, x]
    return out


def colordist(a, b):
    """RGBA の差 (0〜1)。透明どうしは 0"""
    aa = a[..., 3:4] / 255.0; ba = b[..., 3:4] / 255.0
    ca = a[..., :3] / 255.0 * aa; cb = b[..., :3] / 255.0 * ba
    return np.maximum(np.abs(ca - cb).max(-1), np.abs(aa - ba)[..., 0])


def check(name, entry, base):
    """確かめ ①〜④。返す: (ok, 理由の並び, 太りの割合)"""
    h, w = base.shape[:2]
    a = base[..., 3] > 40
    b = seam_rows(entry, w); cm = cols_mask(entry, w)
    reasons = []
    # ① 上の行 (動く列の y=0) に中身
    if (a[0] & cm).any():
        reasons.append('上の行 (y=0) に中身がある')
    # ② 足元 (いちばん下の不透明の行から上 4 行) に境目が掛かる
    ys = np.nonzero(a.any(1))[0]
    if len(ys) == 0:
        return False, ['中身が無い'], 0.0
    bottom = ys.max()
    if (b[cm] > bottom - 4).any() and a[:, cm].any():
        # 中身のある列だけ見る
        bad = [x for x in range(w) if cm[x] and a[:, x].any() and b[x] > bottom - 4]
        if bad:
            reasons.append(f'境目が足元 (行 {bottom - 4} より下) に掛かる列 {len(bad)}')
    # ③ cols の端で中身がつながる (段)
    c = entry.get('cols')
    if c is not None:
        for edge in (c[0], c[1] + 1):
            if 0 < edge < w:
                xl, xr = edge - 1, edge
                bmax = max(b[xl] if cm[xl] else 0, b[xr] if cm[xr] else 0)
                touch = int((a[:bmax, xl] & a[:bmax, xr]).sum())
                if touch > 0:
                    reasons.append(f'列 {xl}|{xr} で中身がつながる行 {touch} (段になる)')
    # ④ 太り: 境目の行 b−1 が「上の行 b−2 とも下の行 b とも違う」= 1ドットの横線の列の割合 (中身のある列だけ)。
    #    その行を2回描くと横線が2ドットに太る。上か下のどちらかと同じ色なら、境目は面の中 (伸びても見えない) か面の縁 (縁が1ドット動くだけ)
    thick = 0; n = 0
    for x in range(w):
        if not cm[x]:
            continue
        bx = int(b[x])
        if bx < 2 or bx > h - 1:
            continue
        r1 = base[bx - 1, x]; r0 = base[bx - 2, x]; r2 = base[bx, x]
        if r1[3] <= 40:
            continue
        n += 1
        if min(colordist(r1[None], r0[None])[0], colordist(r1[None], r2[None])[0]) > 0.2:
            thick += 1
    frac = thick / n if n else 0.0
    if frac > MAX_THICKEN:
        reasons.append(f'横線の太り {frac:.2f} > {MAX_THICKEN}')
    return len(reasons) == 0, reasons, frac


def frames_for(img, entry):
    h, w = img.shape[:2]
    up = shift_up(img, seam_rows(entry, w), cols_mask(entry, w))
    return [img, up, up, img]


def write_all(names, dry):
    os.makedirs(OUT, exist_ok=True)
    report = []
    for name in names:
        entry = SEAMS[name]
        fdir = os.path.join(ART, folder_of(name))
        base = load(os.path.join(fdir, name + '.png'))
        if base is None:
            report.append((name, False, ['絵が無い'], 0.0)); continue
        ok, reasons, frac = check(name, entry, base)
        report.append((name, ok, reasons, frac))
        if not ok or dry:
            continue
        for suffix in ('', '_n', '_e'):
            src = base if suffix == '' else load(os.path.join(fdir, name + suffix + '.png'))
            if src is None:
                continue
            if src.shape[:2] != base.shape[:2]:
                print(f'  ! {name}{suffix}: 大きさが一枚絵と違う {src.shape[:2]} → 作らない'); continue
            fr = frames_for(src, entry)
            for i, f in enumerate(fr):
                Image.fromarray(f, 'RGBA').save(os.path.join(OUT, f'{name}_{STEM}_{i}{suffix}.png'))
        # 足元の行が4コマで同じか (書いた物を読み直して)
        fr = [load(os.path.join(OUT, f'{name}_{STEM}_{i}.png')) for i in range(4)]
        w = base.shape[1]; b = seam_rows(entry, w); cm = cols_mask(entry, w)
        low = int(b[cm].max()) if cm.any() else base.shape[0]
        same = all(np.array_equal(fr[0][low:], f[low:]) for f in fr[1:]) and all(
            np.array_equal(fr[0][:, ~cm], f[:, ~cm]) for f in fr[1:])
        if not same:
            print(f'  ! {name}: 足元の行が4コマで変わった (直すこと)')
    for name, ok, reasons, frac in report:
        print(f'{"OK " if ok else "NG "} {name:28s} 太り {frac:.2f}  {"; ".join(reasons)}')
    return report


def suggest(name, top=6):
    base = load(os.path.join(ART, folder_of(name), name + '.png'))
    h, w = base.shape[:2]
    a = base[..., 3] > 40
    ys = np.nonzero(a.any(1))[0]; t, bot = ys.min(), ys.max()
    rows = []
    for b in range(t + int((bot - t) * 0.4), bot - 3):
        e = {'row': b}
        ok, reasons, frac = check(name, e, base)
        rows.append((frac, b, ok))
    rows.sort()
    print(name, f'上 {t}・下 {bot}')
    for frac, b, ok in rows[:top]:
        print(f'   行 {b}: 太り {frac:.2f}  上の {100 * (b - t) / (bot - t + 1):.0f}% が動く {"" if ok else "(NG)"}')


def thin_cost(base, x, bx):
    """列 x の境目 bx で「1ドットの横線が太る」なら 1 (check の ④ と同じ判定)"""
    h = base.shape[0]
    if bx < 2 or bx > h - 1:
        return 0.0
    r1 = base[bx - 1, x]; r0 = base[bx - 2, x]; r2 = base[bx, x]
    if r1[3] <= 40:
        return 0.0
    return 1.0 if min(colordist(r1[None], r0[None])[0], colordist(r1[None], r2[None])[0]) > 0.2 else 0.0


def auto_seam(name, lo=0.55, hi=0.85, jump=0.35):
    """折れ線の境目を動的計画法で探す (中身のある列ごとに行を選び、横線が太る列の数 + 段差の数 × jump を最小に。隣の列との差は 1 行まで)。
    返す: ([[x, 行], …] の折れ線, 太りの割合)。行は「上の lo〜hi が動く」範囲"""
    base = load(os.path.join(ART, folder_of(name), name + '.png'))
    h, w = base.shape[:2]
    a = base[..., 3] > 40
    ys = np.nonzero(a.any(1))[0]; t, bot = ys.min(), ys.max()
    xs = np.nonzero(a.any(0))[0]; x0, x1 = xs.min(), xs.max()
    rlo = t + int((bot - t) * lo); rhi = min(bot - 4, t + int((bot - t) * hi))
    R = list(range(rlo, rhi + 1))
    INF = 1e9
    cost = [[thin_cost(base, x, r) for r in R] for x in range(x0, x1 + 1)]
    dp = [cost[0][:]]; bk = [[-1] * len(R)]
    for i in range(1, len(cost)):
        row = []; brow = []
        for j in range(len(R)):
            best, bj = INF, -1
            for d in (-1, 0, 1):
                k = j + d
                if 0 <= k < len(R):
                    v = dp[-1][k] + (jump if d else 0.0)
                    if v < best:
                        best, bj = v, k
            row.append(best + cost[i][j]); brow.append(bj)
        dp.append(row); bk.append(brow)
    j = int(np.argmin(dp[-1])); path = [0] * len(cost)
    for i in range(len(cost) - 1, -1, -1):
        path[i] = R[j]; j = bk[i][j] if i > 0 else j
    knots = []
    for i, r in enumerate(path):
        x = x0 + i
        if i == 0 or i == len(path) - 1 or r != path[i - 1] or r != path[i + 1]:
            knots.append([int(x), int(r)])
    # 端の外 (中身の無い列) は端の行のまま = seam_rows の np.interp がそうする
    ok, reasons, frac = check(name, {'row': knots}, base)
    return knots, frac


def sheet(out, names, S=3):
    font = None
    for fp in (os.path.expanduser('~/.local/share/fonts/NotoSansCJKjp-Regular.otf'), '/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc'):
        try:
            font = ImageFont.truetype(fp, 12); break
        except OSError:
            pass
    font = font or ImageFont.load_default()
    tiles = []
    for name in names:
        entry = SEAMS[name]
        base = load(os.path.join(ART, folder_of(name), name + '.png'))
        h, w = base.shape[:2]
        up = frames_for(base, entry)[1]
        diff = colordist(base, up) > 0.01
        pan = []
        for img, lab in ((base, '元'), (up, '上'), (None, '差')):
            bg = Image.new('RGBA', (w * S, h * S), (64, 72, 92, 255))
            if img is not None:
                bg.alpha_composite(Image.fromarray(img, 'RGBA').resize((w * S, h * S), Image.NEAREST))
            else:
                d = np.zeros((h, w, 4), np.uint8); d[diff] = (255, 80, 80, 255)
                ghost = base.copy(); ghost[..., 3] = (ghost[..., 3] * 0.35).astype(np.uint8)
                bg.alpha_composite(Image.fromarray(ghost, 'RGBA').resize((w * S, h * S), Image.NEAREST))
                bg.alpha_composite(Image.fromarray(d, 'RGBA').resize((w * S, h * S), Image.NEAREST))
            dr = ImageDraw.Draw(bg)
            b = seam_rows(entry, w)
            for x in range(w):
                dr.line([(x * S, b[x] * S), (x * S + S - 1, b[x] * S)], fill=(255, 0, 0, 255))
            dr.text((2, 2), lab, fill=(255, 255, 255, 255), font=font)
            pan.append(bg)
        # 境目のまわり (上 8 行・下 4 行) を 6倍で 元|上 に並べる (段・隙間・横線の太りを見る)
        Z = 6; b = seam_rows(entry, w); y0 = max(0, int(b.min()) - 8); y1 = min(h, int(b.max()) + 4)
        xs = np.nonzero((base[..., 3] > 40).any(0))[0]; x0, x1 = (xs.min(), xs.max() + 1) if len(xs) else (0, w)
        for img in (base, up):
            crop = Image.fromarray(img[y0:y1, x0:x1], 'RGBA').resize(((x1 - x0) * Z, (y1 - y0) * Z), Image.NEAREST)
            bgz = Image.new('RGBA', crop.size, (64, 72, 92, 255)); bgz.alpha_composite(crop)
            dz = ImageDraw.Draw(bgz)
            for x in range(x0, x1):
                yy = (b[x] - y0) * Z
                dz.line([((x - x0) * Z, yy), ((x - x0) * Z + Z - 1, yy)], fill=(255, 0, 0, 255))
            pan.append(bgz)
        tw = sum(p.width for p in pan) + 8 * len(pan)
        tile = Image.new('RGBA', (tw, max(p.height for p in pan) + 18), (30, 30, 30, 255))
        ImageDraw.Draw(tile).text((2, 0), f'{name} 境目 {entry["row"]}', fill=(255, 255, 255, 255), font=font)
        x = 0
        for p in pan:
            tile.alpha_composite(p, (x, 18)); x += p.width + 8
        tiles.append(tile)
    W = max(t.width for t in tiles); H = sum(t.height + 6 for t in tiles)
    out_im = Image.new('RGBA', (W, H), (20, 20, 20, 255)); y = 0
    for t in tiles:
        out_im.alpha_composite(t, (0, y)); y += t.height + 6
    out_im.convert('RGB').save(out)
    print('シート', out, out_im.size)


if __name__ == '__main__':
    ap = argparse.ArgumentParser()
    ap.add_argument('--dry', action='store_true')
    ap.add_argument('--sheet')
    ap.add_argument('--suggest', action='store_true')
    ap.add_argument('--json', help='表を JSON で書き出す (StageUnits の確かめ用)')
    ap.add_argument('names', nargs='*')
    a = ap.parse_args()
    if a.suggest:
        for n in a.names:
            suggest(n)
            knots, frac = auto_seam(n)
            print(f'   折れ線の候補 (太り {frac:.2f}): {json.dumps(knots)}')
        sys.exit(0)
    # 確かめ用: 名前:行 で表の境目を仮に置き換える (--sheet・--dry だけ。書く時は表のまま)
    names = []
    for n in (a.names or list(SEAMS)):
        if ':' in n:
            n, r = n.split(':', 1)
            SEAMS[n] = dict(SEAMS.get(n, {}), row=int(r), note='(仮)')
            if not (a.sheet or a.dry):
                sys.exit('名前:行 は --sheet か --dry の時だけ (書く時は表を直す)')
        names.append(n)
    if a.sheet:
        sheet(a.sheet, names); sys.exit(0)
    rep = write_all(names, a.dry)
    if a.json:
        json.dump({n: SEAMS[n] for n in names}, open(a.json, 'w'), ensure_ascii=False, indent=1)
    sys.exit(0 if all(r[1] for r in rep) else 1)

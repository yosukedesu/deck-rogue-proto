#!/usr/bin/env python3
"""docs/pixellab/hd2d-act3/gen_items.py — 幕3「埋もれた古代都市」の箱庭の絵をそろえる (2026-10-03 HD-2D 段2 段1b レーン D3。PixelLab は使わない＝生成 0 回)。

計画 docs/design/hd2d-stage2-plan-2026-10-02.md §2 D の「段1b」・約束 docs/design/hd2d-stage2/contracts.md (幕2 の C4 と同じ形)・分析書 §5・§9。
今ある絵 (unity/Assets/Resources/Art/props/act3_*・Art/tiles/act3_*) は読むだけ。出力は新しい置き場 Art/stage/act3/ だけ。
幕1 の道具 (scripts/stage-palette.py・tile-calm.py・sprite-normals.py・art-lint.py・docs/pixellab/hd2d-act1/gen_art.py・relief-clean.py) は
import して使う (1 文字も変えない)。幕2 の docs/pixellab/hd2d-act2/gen_items.py は手本にしただけで import しない (幕2 を直しても幕3 が変わらない)。

流れ (`all` が全部を順に回す。種は固定＝同じ入力なら同じ絵):
  1. tiles   : 石積み (壁・段の立面・柱・館) は「石積みの calm」＝石の面は明暗 1/3・**目地は暗いまま奥へ沈める**・ブロックの上の縁の光は半分残す。
               天面 (壁・段・柱) は tile-calm。座席の大石板はコード生成 (面の粒は act3_stone_a の面から・静か・無彩に近い灰)
  2. relief  : 既存の小物の足元と皿を切る・等角投影の物を仕分ける・約束の名前の 3 枚 (statue・brazier・lamp_head) を作る (下書き → 作業場)
  3. palette : 本家 Tomb の錨 (壁の暗部 (8,42,53)・柱の白 (156,160,147) ほか) を先に ＋ 1・2 の色 ＋ 発光の差し色 → palette-act3.json
  4. map     : 1・2 を色表へ写して Art/stage/act3/{tiles,relief}/ へ
  5. litter  : 小札 (瓦礫・欠片・砂利・小石。コード生成・背丈 12 ドット以下)
  6. normals : 半立体の法線 <名前>_n.png (sprite-normals の normals-stage と同じ関数)・発光 <名前>_e.png (結晶・裂け目・脈・灯・篝火の火・紋)
  7. index   : index.json・keyflip.json
  8. lint    : 全部の PNG の大きさ・半透明・面積・色表からの距離・タイルのざらつき・光の向き (work/lint.md・lint.json)

使い方:
  python3 -B docs/pixellab/hd2d-act3/gen_items.py all [--work <dir>] [--sheet <png>]
  python3 -B docs/pixellab/hd2d-act3/gen_items.py lint                      # 点検だけ (Art/stage/act3 の今の絵)
  python3 -B docs/pixellab/hd2d-act3/gen_items.py sheet --out <png>         # 全部の絵を並べたシート
  python3 -B docs/pixellab/hd2d-act3/gen_items.py honke <tomb.jpg>          # 本家 Tomb の錨の色を測り直す (結果は TOMB_* に手で写す)
本家の画はリポジトリに入れない (~/.cache の中だけ)。色の錨は下の TOMB_* に数字だけ書いてある (測り方は honke)。
"""
import argparse
import importlib.util
import json
import os
import shutil
import sys
import tempfile

import numpy as np
from PIL import Image, ImageDraw

sys.dont_write_bytecode = True   # docs/ と scripts/ に __pycache__ を作らない

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, '..', '..', '..'))
ART = os.path.join(REPO, 'unity', 'Assets', 'Resources', 'Art')
PROPS = os.path.join(ART, 'props')
SRC_TILES = os.path.join(ART, 'tiles')
OUT = os.path.join(ART, 'stage', 'act3')
PAL_BASE = os.path.join(HERE, 'palette-act3')          # → .json と見本の .png
PAL_JSON = PAL_BASE + '.json'
WORK_DOCS = os.path.join(HERE, 'work')                  # 点検の表だけ (PNG の下書きは作業場 = --work)
ARTP = 'Art/stage/act3/'                                # index.json に書く Resources からのパス


def _load(modname, path):
    spec = importlib.util.spec_from_file_location(modname, path)
    m = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    return m


SP = _load('stage_palette', os.path.join(REPO, 'scripts', 'stage-palette.py'))
TC = _load('tile_calm', os.path.join(REPO, 'scripts', 'tile-calm.py'))
SN = _load('sprite_normals', os.path.join(REPO, 'scripts', 'sprite-normals.py'))
AL = _load('art_lint', os.path.join(REPO, 'scripts', 'art-lint.py'))
RC = _load('relief_clean_act1', os.path.join(REPO, 'docs', 'pixellab', 'hd2d-act1', 'relief-clean.py'))
GA = _load('gen_art_act1', os.path.join(REPO, 'docs', 'pixellab', 'hd2d-act1', 'gen_art.py'))


# ================================================================ 本家 Tomb の色の錨 (2026-10-03 に honke で測った・絵には入れない)
# 測った所 (ot1web_tomb_imperator.jpg 1280×720 の画素): 壁の暗部 = 左上の壁 (80,20)-(420,200)・右の壁 (950,200)-(1250,330)・上端 →
# k-means 5 の暗い 4 つ ＋ 分析書の (8,42,53) ＋ 下の壁の平均 (20,49,57)。柱の白 = 分析書の (156,160,147) の色相 (80°・彩度 0.08) のまま明るさを 7 段に
# (柱と門の石の地の色。既存の絵には骨白の灰の段がほとんど無い)。柱の光の中の冷たい灰 = 光の柱 (590,150)-(690,400) の k-means。
# 床 = 光の当たる床 (560,430)-(700,470) の (92,93,95)・(113,123,133)。篝火の炎 = 左右の篝火の明るい画素の k-means 5
TOMB_DARK = [(2, 22, 33), (3, 34, 46), (8, 42, 53), (8, 48, 61), (20, 49, 57), (38, 80, 85)]
PILLAR_RAMP = [(70, 72, 66), (88, 90, 83), (106, 109, 101), (124, 127, 117), (140, 144, 133), (156, 160, 147), (176, 180, 166), (198, 201, 187), (222, 223, 209)]
TOMB_COLUMN = [(93, 111, 126), (107, 121, 135)]
# 座席の床の灰の段: 本家の光の当たる床 (92,93,95)・(113,123,133) の間の冷たい灰 (色相 205°・彩度 0.05〜0.08) を明るさ 6 段に
FLOOR_RAMP = [(62, 66, 69), (76, 80, 84), (89, 94, 98), (101, 106, 110), (113, 118, 123), (127, 132, 137)]
FIRE = [(142, 96, 57), (177, 122, 75), (207, 154, 94), (239, 200, 131), (253, 239, 189)]
# 地衣と苔 (折れ柱・館の壁の苔。くすんだ緑＝暖色にしない。錨が無いと鮮やかな黄緑の苔が篝火の黄土へ写った＝2026-10-03 の試し)
MOSS = [(66, 78, 54), (94, 108, 70), (126, 138, 90), (160, 172, 110)]
# 冷たい灯 (灯柱・吊り灯のガラス・冷たい炎の火鉢の青白い光。既存の絵 act3_brazier_cold の炎の色そのもの)。
# 結晶の青緑の差し色だけだと、青白い灯が青緑に写って「結晶」に見えた (2026-10-03 の試し)
COLD = [(65, 140, 176), (113, 189, 227), (164, 224, 244), (220, 246, 253)]


def honke_measure(path):
    """本家 Tomb の暗部・柱の光・床・篝火の色を k-means で測る (TOMB_* を作った手順。結果を表示するだけ)"""
    a = np.array(Image.open(path).convert('RGB'))

    def px(rects, lmin=None):
        P = np.concatenate([a[y0:y1, x0:x1].reshape(-1, 3) for x0, y0, x1, y1 in rects])
        if lmin is not None:
            P = P[(P @ np.array([0.299, 0.587, 0.114])) > lmin]
        return P
    regions = {
        'dark': px([(80, 20, 420, 200), (950, 200, 1250, 330), (0, 0, 1280, 40)]),
        'column': px([(590, 150, 690, 400)]),
        'floor': px([(560, 430, 700, 470), (20, 420, 420, 480)]),
        'fire': px([(85, 335, 125, 385), (1065, 495, 1110, 545)], lmin=90),
    }
    for name, P in regions.items():
        q = (P // 4) * 4 + 2
        u, c = np.unique(q, axis=0, return_counts=True)
        lab = SP.rgb8_to_oklab(u)
        sc = np.array([1.5, 1.0, 1.0])
        cen = SP.weighted_kmeans(lab * sc, c.astype(float), 5) / sc
        rgb = SP.oklab_to_rgb8(cen)
        o = np.argsort(cen[:, 0])
        print(name, [tuple(int(v) for v in rgb[i]) for i in o])


# ================================================================ 小さな道具
def hsv(a):
    rgb = a[..., :3].astype(np.float64) / 255.0
    mx, mn = rgb.max(-1), rgb.min(-1)
    d = np.maximum(mx - mn, 1e-9)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    h = np.where(mx == r, ((g - b) / d) % 6, np.where(mx == g, (b - r) / d + 2, (r - g) / d + 4)) * 60.0
    s = np.where(mx > 0, (mx - mn) / np.maximum(mx, 1e-9), 0.0)
    return h, s, mx


def luma(rgb):
    rgb = np.asarray(rgb, dtype=np.float64)
    return 0.299 * rgb[..., 0] + 0.587 * rgb[..., 1] + 0.114 * rgb[..., 2]


def load_rgba(path):
    return np.array(Image.open(path).convert('RGBA'))


def save(img, path, mode=None):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    if mode is None:
        mode = 'RGBA' if img.shape[-1] == 4 else 'RGB'
    Image.fromarray(np.ascontiguousarray(img).astype(np.uint8), mode).save(path, optimize=True)
    return path


def lab_of(c):
    return SP.rgb8_to_oklab(np.array(c, dtype=np.float64))


def opaque(rgb_or_lab_img):
    return np.dstack([rgb_or_lab_img, np.full(rgb_or_lab_img.shape[:2], 255, np.uint8)]).astype(np.uint8)


def lapvar_masked(tile, mask):
    """ラプラシアン分散 (tile-calm の check と同じ式) を mask の画素だけで"""
    y = luma(tile[..., :3])
    lap = 4 * y[1:-1, 1:-1] - y[:-2, 1:-1] - y[2:, 1:-1] - y[1:-1, :-2] - y[1:-1, 2:]
    m = mask[1:-1, 1:-1]
    return round(float(lap[m].var()), 1) if m.sum() > 50 else None


# ================================================================ 1. タイル
# ---- 1a. 石積みの calm (目地を沈める)
def runs(mask, axis):
    """mask の連なりの長さ (横 axis=1・縦 axis=0。巻き戻し)。目地 (長い直線) とひび (短い斜めの線) を分ける"""
    m = mask if axis == 1 else mask.T
    out = np.zeros(m.shape, int)
    n = m.shape[1]
    for y in range(m.shape[0]):
        row = m[y]
        if row.all():
            out[y] = n
            continue
        if not row.any():
            continue
        s = int(np.argmin(row))           # 目地でない所から数え始める (巻き戻しの連なりを切らない)
        r = np.roll(row, -s)
        ln = np.zeros(n, int)
        i = 0
        while i < n:
            if r[i]:
                j = i
                while j < n and r[j]:
                    j += 1
                ln[i:j] = j - i
                i = j
            else:
                i += 1
        out[y] = np.roll(ln, s)
    return out if axis == 1 else out.T


def otsu(v):
    """明るさの 2 群の境 (目地と石の面)"""
    hist, edges = np.histogram(v, 64)
    c = (edges[:-1] + edges[1:]) / 2
    best, thr = -1, c[len(c) // 2]
    for i in range(1, 63):
        w0, w1 = hist[:i].sum(), hist[i:].sum()
        if w0 == 0 or w1 == 0:
            continue
        m0 = (hist[:i] * c[:i]).sum() / w0
        m1 = (hist[i:] * c[i:]).sum() / w1
        bv = w0 * w1 * (m0 - m1) ** 2
        if bv > best:
            best, thr = bv, c[i]
    return float(thr)


def find_joints(lab, jrel=None, run_h=5, run_v=4):
    """目地 = 暗い画素のうち、横に run_h 以上か縦に run_v 以上つながる物 (石の面のひび＝短い斜めの線は目地にしない)。
    暗さの境は jrel (中央値の倍率) か、無ければ Otsu"""
    Lc = lab[..., 0]
    thr = np.median(Lc) * jrel if jrel else min(otsu(Lc), np.median(Lc) * 0.8)
    D = Lc < thr
    return D & ((runs(D, 1) >= run_h) | (runs(D, 0) >= run_v))


def joint_bands(joint, frac=0.4):
    """横の目地の帯 (行の目地の割合 frac 以上) の中心の行の並び (巻き戻し)"""
    f = joint.mean(1)
    rows = [y for y in range(joint.shape[0]) if f[y] > frac]
    bands, cur = [], []
    for y in rows:
        if cur and y != cur[-1] + 1:
            bands.append(cur)
            cur = []
        cur.append(y)
    if cur:
        bands.append(cur)
    n = joint.shape[0]
    if len(bands) > 1 and bands[0][0] == 0 and bands[-1][-1] == n - 1:
        bands[0] = [v - n for v in bands[-1]] + bands[0]
        bands = bands[:-1]
    return [(float(np.mean(b)) % n, min(b) % n, max(b) % n) for b in bands]


def roll_joint_to_bottom(t, joint):
    """横の目地の帯の 1 本がタイルの下端 (行 63) で終わるように縦に回す＝上下の継ぎ目が目地の中に隠れる
    (下の端の行が目地・上の端の行は次の段の石の面＝本物の石積みと同じ並び)"""
    bands = joint_bands(joint)
    if not bands:
        return t, joint, 0
    n = t.shape[0]
    # 帯の下の端 (max) が 63 に来る量 = 63 - max。動かす量の小さい帯を選ぶ
    shifts = [((n - 1) - b[2]) % n for b in bands]
    k = min(shifts, key=lambda s: min(s, n - s))
    return np.roll(t, k, 0), np.roll(joint, k, 0), int(k)


def seam_h(tile, band=14):
    """左右の継ぎ目だけを直す (横に半分ずらして、真ん中の縦の帯を元の絵で埋める・境目は差の最も小さい道)。
    上下は roll_joint_to_bottom で目地の中に隠してあるので縦にはずらさない (tile-calm の make_seamless は縦にも 32 ずらすので、
    段の高さが 32 の約数でない石積みでは帯の中の段が半段ずれる＝cliff_c・stone で目地が階段になった)"""
    n = tile.shape[1]
    c = n // 2
    base = np.roll(tile, c, 1)
    lb = SP.rgb8_to_oklab(base[..., :3].astype(np.float64))
    lt = SP.rgb8_to_oklab(tile[..., :3].astype(np.float64))
    dv = ((lb - lt) ** 2).sum(-1)
    b = int(max(3, min(c - 2, band)))
    left = TC.min_cut_path(dv[:, c - b:c]) + (c - b)
    right = TC.min_cut_path(dv[:, c + 1:c + b + 1]) + (c + 1)
    yy, xx = np.mgrid[0:tile.shape[0], 0:n]
    inv = (xx >= left[yy]) & (xx < right[yy])
    out = base.copy()
    out[inv] = tile[inv]
    return out


def mason(src, face_L, contrast=1 / 3, sat=0.8, ab=None, hl_keep=0.55, joint_L=None, joint_ab=None, jblend=0.6,
          jrel=None, run_h=5, run_v=4, radius=12, vroll=None):
    """石積みの calm。src = 64×64 RGBA。返り値: (タイル, 目地のマスク, 記録)
      - 石の面: 大きな明暗の勾配を消し (半径 radius の巻き戻しの箱ぼかしを引く)・白い粒を消し・明暗の幅を contrast・
        明るさの平均を face_L (Oklab L)・色み (a,b) を ab (無ければ元の色みの sat 倍)。ブロックの上の縁の明るい線 (面の平均より 0.10 以上明るい) は hl_keep
      - 目地: 明るさを joint_L へ沈める (元の明暗の差は 0.25 だけ残す)・色みを joint_ab へ jblend だけ寄せる
      - 継ぎ目: 横の目地を下端へ回し (vroll で量を指定できる)・左右だけキルティング"""
    lab0 = SP.rgb8_to_oklab(src[..., :3].astype(np.float64))
    joint0 = find_joints(lab0, jrel, run_h, run_v)
    if vroll is None:
        src, joint, k = roll_joint_to_bottom(src, joint0)
    else:
        k = int(vroll)
        src, joint = np.roll(src, k, 0), np.roll(joint0, k, 0)
    # 下端の行 63 を段の帯の最後の行にそろえる (帯の最後が行 62 で行 63 が半分だけ目地だと、元のちがう種を上下に並べた時に
    # 段の高さが 1 行ずれて境が目立った＝side_step の stone_b。2026-10-03 直しの番)
    for _ in range(3):
        f = joint.mean(1)
        if f[-1] < 0.6 and f[-2] >= 0.6:
            src, joint, k = np.roll(src, 1, 0), np.roll(joint, 1, 0), k + 1
        elif f[0] >= 0.6:
            src, joint, k = np.roll(src, -1, 0), np.roll(joint, -1, 0), k - 1
        else:
            break
    lab = SP.rgb8_to_oklab(src[..., :3].astype(np.float64))
    Lc = lab[..., 0]
    face = ~joint
    fm = float(Lc[face].mean())
    Lf = np.where(face, Lc, fm)
    hp = Lf - TC.box_blur_wrap(Lf, radius) + fm
    medL = TC.median5_wrap(hp)
    hl = face & ((hp - fm) > 0.10)
    spk = face & ~hl & ((hp - medL) > 0.08)
    hp = np.where(spk, medL, hp)
    newL = np.where(hl, face_L + (hp - fm) * hl_keep, face_L + (hp - fm) * contrast)
    if joint.any():
        jm = float(Lc[joint].mean())
        newL = np.where(joint, joint_L + (Lc - jm) * 0.25, newL)
    a, b = lab[..., 1].copy(), lab[..., 2].copy()
    am, bm = float(a[face].mean()), float(b[face].mean())
    if ab is not None:
        a = np.where(face, ab[0] + (a - am) * sat, a)
        b = np.where(face, ab[1] + (b - bm) * sat, b)
    else:
        a = np.where(face, a * sat, a)
        b = np.where(face, b * sat, b)
    if joint_ab is not None:
        a = np.where(joint, (1 - jblend) * a + jblend * joint_ab[0], a)
        b = np.where(joint, (1 - jblend) * b + jblend * joint_ab[1], b)
    out = opaque(SP.oklab_to_rgb8(np.dstack([newL, a, b])))
    out = seam_h(out)
    jout = seam_h(np.dstack([joint.astype(np.uint8) * 255] * 3 + [np.full(joint.shape, 255, np.uint8)]))[..., 0] > 127
    rec = dict(vroll=k, jointFrac=round(float(joint.mean()), 3), bands=[round(v[0], 1) for v in joint_bands(joint)],
               faceMeanBefore=round(fm, 3))
    return out, jout, rec


def unify_edges(tiles, joints, band=12, ref=0, min_stone=6, pen_sliver=1.0, pen_joint=0.03):
    """石積みの 4 種の左右の端の帯を、種 ref (a) の端の帯にそろえる (2026-10-03 直しの番・反証の指摘 1)。
    seam_h は 1 枚ごとにしか継がないので、横に回した種 (c・d) や別の元 (cliff_b と d) どうしを並べると端の列が合わなかった
    (side_wall の a|c で端の差 56・中の差 10)。ここでは各種の左 band 列と右 band 列を a の帯で置き換える。境目は差の最も小さい
    縦の道 (tile-calm の min_cut_path・上下も巻き戻しでつながる)。道の重み: 色の差＋「道の左の最後の縦の目地と右の最初の縦の目地の
    間が min_stone テクセル未満になる (細い石ができる)」所に pen_sliver＋縦の目地の中を切る (目地の幅が変わる) 所に pen_joint。
    列 0 と列 63 は必ず a の列＝どの種をどの向きで並べても、境は a の右端の列と a の左端の列 (a 自身の継ぎ目と同じ) か、
    同じ列どうし (反転した時) になる。戻り値: [(タイル, 目地), ...]"""
    R, RJ = tiles[ref], joints[ref]
    n = R.shape[1]
    lR = SP.rgb8_to_oklab(R[..., :3].astype(np.float64))

    def vjoint(J):
        hb = J.mean(1) > 0.4                      # 横の目地の帯の行 (ここは道が自由に動いてよい)
        hb = hb | np.roll(hb, 1) | np.roll(hb, -1)
        return J & ~hb[:, None]

    def sliver_cost(VL, VR, xs, side):
        """xs の各列で切った時の重み。side='right': 列 x までが VL の絵・x+1 からが VR の絵。side='left': 列 x−1 までが VR・x からが VL"""
        c = np.zeros((n, len(xs)))
        for y in range(n):
            jl = np.where(VL[y])[0]
            jr = np.where(VR[y])[0]
            for k, x in enumerate(xs):
                if side == 'right':
                    a_ = jl[jl <= x]
                    b_ = jr[jr > x]
                    lo = a_.max() if len(a_) else None
                    hi = b_.min() if len(b_) else (jr.min() + n if len(jr) else None)
                    cut_in = VL[y, x] != VR[y, (x + 1) % n]
                else:
                    a_ = jr[jr < x]
                    b_ = jl[jl >= x]
                    lo = a_.max() if len(a_) else (jr.max() - n if len(jr) else None)
                    hi = b_.min() if len(b_) else None
                    cut_in = VR[y, x - 1] != VL[y, x]
                if lo is not None and hi is not None and 0 < hi - lo - 1 < min_stone:
                    c[y, k] += pen_sliver
                if cut_in:
                    c[y, k] += pen_joint
        return c
    VR_ = vjoint(RJ)
    res = []
    for i, (T, J) in enumerate(zip(tiles, joints)):
        if i == ref:
            res.append((T, J))
            continue
        lT = SP.rgb8_to_oklab(T[..., :3].astype(np.float64))
        cost = ((lR - lT) ** 2).sum(-1)
        VT = vjoint(J)
        lx = np.arange(1, band)
        rx = np.arange(n - band, n - 1)
        cl = cost[:, 1:band] + sliver_cost(VT, VR_, lx, 'left')
        cr = cost[:, n - band:n - 1] + sliver_cost(VT, VR_, rx, 'right')
        left = TC.min_cut_path(cl) + 1                 # x < left → a
        right = TC.min_cut_path(cr) + (n - band)       # x > right → a
        yy, xx = np.mgrid[0:T.shape[0], 0:n]
        take = (xx < left[yy]) | (xx > right[yy])
        o, oj = T.copy(), J.copy()
        o[take] = R[take]
        oj[take] = RJ[take]
        res.append((o, oj))
    return res


def cross_seams(tiles, rot):
    """別の種どうし (と回し方 flipX の反転どうし) を並べた時の継ぎ目 (2026-10-03 直しの番・反証の指摘 1・2)。tiles = RGB(A) の 4 枚。
      lrRatio : 左のタイルの右端の列と右のタイルの左端の列の明るさの差の平均 ÷ 中の隣どうしの列の差の平均 (4 種の平均) の、全部の組の最大
      tbRatio : 上のタイルの下端の行と下のタイルの上端の行の差の平均の、全部の組の最大 ÷ 同じ種を上下に並べた時の差の平均
                (上下の境は石積みも石板も目地の行 (行 63)＝境の段差は「目地 → 次の段の石の面」。別の種どうしでも同じ種どうしと同じ強さか)
      tbJoint : 上下の境の差の平均 (全部の組) ÷ 中の「横に続く目地の画素 → その下の石の面」の段差 (目地 = 中央値より 10 暗い画素)。
                境の線が中の目地より目立たないか (天面だけ門にする。石積みは段の帯の端の明暗が画素で測ると割れるので記録だけ)
      fullRows: 行 63 以外で、目地 (中央値より 10 暗い画素) が横幅の 6 割を超える行 (横幅いっぱいの目地の線)"""
    Ls = [luma(t[..., :3]) for t in tiles]
    flips = (False, True) if rot == 'flipX' else (False,)
    inner_x = float(np.mean([np.abs(np.diff(l, axis=1)).mean() for l in Ls]))
    px_s = []
    for l in Ls:
        dk = l < np.median(l) - 10
        hz = dk & np.roll(dk, 1, 1) & np.roll(dk, -1, 1)
        tr = hz[:-1] & ~dk[1:]
        px_s.append(np.abs(l[1:] - l[:-1])[tr])
    allp = np.concatenate(px_s)
    inner_j = float(allp.mean()) if len(allp) else 1.0
    tb_self = float(np.mean([np.abs(l[-1, :] - l[0, :]).mean() for l in Ls]))
    worst_lr, worst_tb, klr, ktb, tb_all = 0.0, 0.0, '', '', []
    for i, p in enumerate(Ls):
        for fp in flips:
            P = p[:, ::-1] if fp else p
            for j, q in enumerate(Ls):
                for fq in flips:
                    Q = q[:, ::-1] if fq else q
                    key = 'abcd'[i] + ("'" if fp else '') + 'abcd'[j] + ("'" if fq else '')
                    s = float(np.abs(P[:, -1] - Q[:, 0]).mean())
                    t = float(np.abs(P[-1, :] - Q[0, :]).mean())
                    tb_all.append(t)
                    if s > worst_lr:
                        worst_lr, klr = s, key
                    if t > worst_tb:
                        worst_tb, ktb = t, key
    rows = []
    for l in Ls:
        dk = l < np.median(l) - 10
        rows.append([y for y in range(l.shape[0] - 1) if dk[y].mean() > 0.6])
    return dict(lrRatio=round(worst_lr / max(inner_x, 1e-6), 2), lrWorst=klr, lrMean=round(worst_lr, 1), innerX=round(inner_x, 1),
                tbRatio=round(worst_tb / max(tb_self, 1e-6), 2), tbWorst=ktb, tbMean=round(worst_tb, 1), tbSelf=round(tb_self, 1),
                tbJoint=round(float(np.mean(tb_all)) / max(inner_j, 1e-6), 2), innerJoint=round(inner_j, 1), fullRows=rows)


SEAM_GATE = dict(lr=1.6, tb=1.25, tbJoint=1.25)   # 別の種どうしの継ぎ目の門 (左右は tile-calm の seamRatioX と同じ 1.6・上下は同じ種どうしの 1.25 倍まで・天面の境は中の目地の 1.25 倍まで)


def material_seams():
    """Art/stage/act3/tiles の今の絵で、材質ごとに cross_seams を測る (lint からも呼ぶ)。天面 (GEN_TILES) は横幅いっぱいの目地の行も門"""
    out = {}
    for name, spec in list(MASON.items()) + list(GEN_TILES.items()):
        ps = [os.path.join(OUT, 'tiles', f'{name}_{v}.png') for v in 'abcd']
        if not all(os.path.exists(p) for p in ps):
            continue
        cs = cross_seams([load_rgba(p) for p in ps], spec['rot'])
        if name in GEN_TILES:
            # 天面: 横の目地を石板ごとに埋めてあるので、行 63 の暗い所の割合は種ごとに違う (上下の比は種の違いを測ってしまう)。
            # 境が中の目地より目立たないか (tbJoint) と、横幅いっぱいの目地の行が無いかで見る
            ok = cs['lrRatio'] <= SEAM_GATE['lr'] and cs['tbJoint'] <= SEAM_GATE['tbJoint'] and not any(cs['fullRows'])
        else:
            ok = cs['lrRatio'] <= SEAM_GATE['lr'] and cs['tbRatio'] <= SEAM_GATE['tb']
        cs['pass'] = bool(ok)
        out[name] = cs
        print(f"{name}: {'OK' if ok else 'NG'}  左右 {cs['lrRatio']} (最悪 {cs['lrWorst']})  上下 {cs['tbRatio']} (最悪 {cs['tbWorst']})  境÷中の目地 {cs['tbJoint']}  "
              f"横幅いっぱいの目地の行 {cs['fullRows'] if name in GEN_TILES else '(石積みは段が横に通るので数えない)'}")
    return out


# 材質: 石積み (面の手直し・目地の沈め方)。src = [(元の絵, 変種の作り方)]。変種の作り方 'x<N>' = 石積みの calm の後に横へ N 回す
# (縦には回さない＝段の高さの並びが 4 種で同じ＝隣のタイルと横の目地がそろう)。'flipX' = 左右反転 (光は上から描いてあるので上下は返さない)
TDARK = lab_of(TOMB_DARK[0])       # (2,22,33) = いちばん深い目地
TDARK2 = lab_of(TOMB_DARK[1])      # (3,34,46)
BONE = lab_of((156, 160, 147))
MASON = {
    'side_wall': dict(src=[('act3_cliff_b', ''), ('act3_cliff_d', ''), ('act3_cliff_b', 'x24'), ('act3_cliff_d', 'x40')],
                      face_L=0.46, contrast=0.22, sat=0.8, ab=None, hl_keep=0.35, joint_L=float(TDARK[0]), joint_ab=tuple(TDARK[1:]), jblend=0.6,
                      rot='flipX', face='side', gate=1500,
                      note='奥の壁・胸壁の立面。Art/tiles/act3_cliff_b・d (段の高さ 16 テクセル＝2 枚の横の目地がそろう) を石積みの calm: 石の面は明暗 1/3・彩度 0.8、'
                           '目地は本家 Tomb の壁の暗部 (2,22,33) まで沈める (目地の奥が黒い大ブロック)、ブロックの上の縁の光は 0.35 残す (本家の壁の「上の縁が光る」)。'
                           'cliff_a・c は段の高さが 11〜13 で b・d とそろわないので壁には混ぜない (a は館 side_brick、c は柱 side_pillar)。'
                           '泡状の石 (act3_m_rubble・bed) は使わない。上下は返さない (光が上から描いてある)。回し方は flipX (rot4 は段が縦になる)'),
    'side_step': dict(src=[('act3_stone_d', 'x3'), ('act3_stone_d', 'x19'), ('act3_stone_d', 'x35'), ('act3_stone_d', 'x51')],
                      face_L=0.50, contrast=0.25, sat=0.75, ab=None, hl_keep=0.4, joint_L=float(TDARK2[0]), joint_ab=tuple(TDARK2[1:]), jblend=0.5,
                      rot='flipX', face='side', gate=1500,
                      note='段 (T1〜T4) の立面・階段の蹴上げ。Art/tiles/act3_stone_d の大きな切石 (段の高さ 24 テクセル＝背丈の約 1/3) を石積みの calm。'
                           '壁より少し明るく・目地は (3,34,46) まで (壁より一段浅い)。元が 1 枚なので横に 3・19・35・51 回して 4 種。'
                           'stone_b は 2026-10-03 の直しの番で外した: 下の目地の帯が行 61〜2 まで厚くぎざぎざに続き (行 0〜2 の目地の割合 0.55〜0.72)、'
                           'stone_d の種を上下に並べると帯の厚さが 3 行ちがって境が目立った (上下の比 1.47。蹴上げは世界の高さ 2.56 ごとにタイルの境をまたぐ)。'
                           'stone_a・c は段の並びが d とそろわないので混ぜない'),
    'side_pillar': dict(src=[('act3_cliff_c', ''), ('act3_cliff_c', 'x16'), ('act3_cliff_c', 'x32'), ('act3_cliff_c', 'x48')],
                        face_L=0.67, contrast=0.3, sat=0.5, ab=(float(BONE[1]), float(BONE[2])), hl_keep=0.4, joint_L=0.44,
                        joint_ab=(float(BONE[1]) * 0.5, float(BONE[2]) * 0.5), jblend=0.8, jrel=0.8,
                        rot='flipX', face='side', gate=1500,
                        note='柱・門 (arch・pillar) の側面。Art/tiles/act3_cliff_c (細い目地の静かな切石) を石積みの calm で骨白の石へ (色相を本家 Tomb の柱の白 (156,160,147) へ・'
                             '明るさの平均 約 135)。目地は石より暗いが黒くはしない (白い石の目地)。柱の光が当たると (156,160,147) に読める地の色。'
                             '元が 1 枚なので横に 16・32・48 回して 4 種'),
    'side_brick': dict(src=[('act3_cliff_a', ''), ('act3_cliff_a', 'x16'), ('act3_cliff_a', 'x32'), ('act3_cliff_a', 'x48')],
                       face_L=0.38, contrast=0.2, sat=0.75, ab=None, hl_keep=0.32, joint_L=float(TDARK[0]), joint_ab=tuple(TDARK[1:]), jblend=0.6,
                       rot='flipX', face='side', gate=1500,
                       note='館の壁 (block)。Art/tiles/act3_cliff_a (段の高さ 11〜13 の小さめの切石・ひびあり) を石積みの calm。壁より暗く (館は柱の光の外)。'
                            'ひびは目地にしない (横 5・縦 4 以上つながる暗い線だけを目地に)。元が 1 枚なので横に回して 4 種。'
                            '計画 (plan.md) では act3_dirt_a・d だったが使わない: dirt は床に頼んだ「角の丸い不揃いな敷石」で目地が太く (目地の割合 0.35・cliff_a は 0.22)、'
                            '目地を暗部の錨へ沈めると黒の上に丸い石の粒が並ぶ「泡状の石」になった (art-bible §8-3 の「等間隔の粒・泡状の塊は捨てる」)。'
                            'dirt_d は dirt_b と段の並びが同じ (ほぼ同じ絵)'),
}

# ---- 1b. 天面 (tile-calm。明るさの平均を材質ごとにそろえる)
# 天面 (壁・段・柱) も 2026-10-03 の試しで石板のコード生成へ移した (下の GEN_TILES)。tile-calm だと元の 4 枚の模様 (煉瓦・ひびの十字) が
# 種ごとに違い、混ぜて敷くとつぎはぎに見えた (段の天面の大きなひびが鏡写しで並ぶ)。tile-calm の道は残す (ここに書けば使える)
CALM = {}


def _variant_pre(a, how):
    """calm の前の手直し (flipX = 左右反転)"""
    for step in [s for s in how.split('|') if s]:
        if step == 'flipX':
            a = a[:, ::-1]
        elif step.startswith('x'):
            pass                        # 石積みの横回しは calm の後
        else:
            sys.exit('知らない手直し: ' + step)
    return np.ascontiguousarray(a)


def _xroll(how):
    for step in [s for s in how.split('|') if s]:
        if step.startswith('x'):
            return int(step[1:])
    return 0


def _retone(tile, L=None, ab=None, sat=1.0):
    """Oklab で明るさの平均を L・色みの平均を ab (無ければそのまま) に。sat = 色みの散らばりの倍率"""
    lab = SP.rgb8_to_oklab(tile[..., :3].astype(np.float64))
    if L is not None:
        lab[..., 0] += L - lab[..., 0].mean()
    for ch, i in ((0, 1), (1, 2)):
        m = lab[..., i].mean()
        tgt = ab[ch] if ab is not None else m
        lab[..., i] = tgt + (lab[..., i] - m) * sat
    out = tile.copy()
    out[..., :3] = SP.oklab_to_rgb8(lab)
    return out


def make_tiles(work):
    """石積みと天面 (色表へ写す前の下書き。作業場 work/calm/<名前>_<a..d>.png)。戻り値: 点検の表"""
    res = {}
    d = os.path.join(work, 'calm')
    os.makedirs(d, exist_ok=True)
    for name, spec in MASON.items():
        cache = {}
        drafts = []
        ab = spec['ab']
        if ab is None:
            # 色み: 最初の元の石の面の平均 × sat に全部の元をそろえる (最初の元は今までと同じ絵。2 つ目の元 (cliff_d・stone_d) は
            # 石の面の色みが少し違い、端の帯を a にそろえた時に 1 つの石が 2 色に割れて見えた＝2026-10-03 直しの番)
            a0 = load_rgba(os.path.join(SRC_TILES, spec['src'][0][0] + '.png'))
            l0 = SP.rgb8_to_oklab(a0[..., :3].astype(np.float64))
            f0 = ~find_joints(l0, spec.get('jrel'))
            ab = (float(l0[..., 1][f0].mean()) * spec['sat'], float(l0[..., 2][f0].mean()) * spec['sat'])
        for i, (src, how) in enumerate(spec['src']):
            if src not in cache:
                a = load_rgba(os.path.join(SRC_TILES, src + '.png'))
                if a.shape[:2] != (64, 64):
                    sys.exit(f'{src} は 64×64 でない')
                cache[src] = mason(a, spec['face_L'], spec['contrast'], spec['sat'], ab, spec['hl_keep'], spec['joint_L'],
                                   spec['joint_ab'], spec['jblend'], spec.get('jrel'))
            o, jm, rec = cache[src]
            k = _xroll(how)
            drafts.append((np.roll(o, k, 1), np.roll(jm, k, 1), rec))
        # 4 種の左右の端の帯を a の帯にそろえる (別の種どうしを並べても継ぎ目が出ない・反証の指摘 1)
        unified = unify_edges([t for t, _, _ in drafts], [j for _, j, _ in drafts], band=spec.get('edgeBand', 12))
        for i, (src, how) in enumerate(spec['src']):
            v = 'abcd'[i]
            o2, jm2 = unified[i]
            rec = drafts[i][2]
            save(o2, os.path.join(d, f'{name}_{v}.png'))
            MASKS[f'{name}_{v}'] = jm2
            c = mason_verdict(o2, jm2, spec['gate'])
            fl = c['faceLapvar']
            c['src'] = f'Art/tiles/{src}' + (f' ({how})' if how else '')
            c.update(rec)
            res[f'{name}_{v}'] = c
            print(f'{name}_{v}: {"OK" if c["pass"] else "NG " + ",".join(c["fail"])}  石の面のざらつき {fl}  全体 {c["lapvar"]}  '
                  f'目地 {rec["jointFrac"]}  段 {rec["bands"]}  ← {c["src"]}')
    for name, spec in CALM.items():
        outs = []
        for i, (src, how) in enumerate(spec['src']):
            v = 'abcd'[i]
            a = load_rgba(os.path.join(SRC_TILES, src + '.png'))
            t = _variant_pre(a, how)
            seed = (sum(map(ord, name + v)) * 7919) % (2 ** 31)
            o, nspk, used = TC.calm(t, spec['contrast'], spec['sat'], 12, 0.10, 14.0, seed,
                                    target_lap=spec['maxlap'] * 0.8, min_contrast=spec['min'])
            o = _retone(o, spec['L'], spec['ab'], 1.0)
            save(o, os.path.join(d, f'{name}_{v}.png'))
            c = TC.verdict(TC.check(o), spec['maxlap'])
            c['src'] = f'Art/tiles/{src}' + (f' ({how})' if how else '')
            c['contrastUsed'] = round(used, 3)
            c['before'] = TC.check(t)['lapvar']
            res[f'{name}_{v}'] = c
            print(f'{name}_{v}: {"OK" if c["pass"] else "NG " + ",".join(c["fail"])}  lapvar {c["lapvar"]} (元 {c["before"]})  contrast {used:.3f}  ← {c["src"]}')
    return res


MASKS = {}      # 石積みのタイル名 → 目地のマスク (色表へ写した後に石の面のざらつきを測り直す)


def mason_verdict(tile, joint, gate):
    """石積みの門: 石の面 (目地から 1 テクセル離れた所) のざらつき ≤ gate・左右の継ぎ目 (端の差・継ぎ目の比)。
    上下は下端の行が目地・上端の行が次の段の石の面なので、上下の端の差は門にしない (目地の帯の中に継ぎ目を隠してある)"""
    c = TC.check(tile)
    fl = lapvar_masked(tile, ~_dilate(joint, 1))
    ok_edge = c['edgeMeanDiffLR'] <= max(2.0, c['innerStepP90X'])
    ok_seam = c['seamRatioX'] <= 1.6
    c['faceLapvar'] = fl
    c['fail'] = [k for k, v in (('faceLapvar', fl is not None and fl <= gate), ('edgeLR', ok_edge), ('seamLR', ok_seam)) if not v]
    c['pass'] = not c['fail']
    return c


def _dilate(m, r):
    out = m.copy()
    for dy in range(-r, r + 1):
        for dx in range(-r, r + 1):
            out |= np.roll(np.roll(m, dy, 0), dx, 1)
    return out


# ---- 1c. 座席の大石板 (コード生成)
def stone_grain(src, contrast, sat, L, ab, radius=3, seed=7):
    """石の面の粒 (64×64・継ぎ目なし)。元のタイルの目地と縁の光を周りの面で埋めて (8 近傍の平均を 12 回) から、半径 3 の箱ぼかしを引く＝
    元の石の形 (大きな斑・ブロックの影) は消え、PixelLab の石の細かい粒だけが残る。明暗 contrast・彩度 sat・明るさの平均 L・色みの平均 ab"""
    t = load_rgba(os.path.join(SRC_TILES, src + '.png'))
    lab = SP.rgb8_to_oklab(t[..., :3].astype(np.float64))
    Lc = lab[..., 0]
    med = np.median(Lc)
    hole = (Lc < med * 0.78) | (Lc > med * 1.22)
    hole = _dilate(hole, 1)
    F = np.where(hole, np.nan, Lc)
    for _ in range(12):
        nb = np.stack([np.roll(np.roll(F, dy, 0), dx, 1) for dy in (-1, 0, 1) for dx in (-1, 0, 1)])
        cnt = np.isfinite(nb).sum(0)
        sm = np.where(np.isfinite(nb), nb, 0).sum(0)
        fill = np.where(cnt > 0, sm / np.maximum(cnt, 1), np.nan)
        F = np.where(np.isnan(F), fill, F)
    F = np.where(np.isnan(F), np.nanmean(F), F)
    hp = F - TC.box_blur_wrap(F, radius) + F.mean()
    medL = TC.median5_wrap(hp)
    hp = np.where(np.abs(hp - medL) > 0.08, medL, hp)
    Lf = L + (hp - hp.mean()) * contrast
    if ab is None:
        ab = (float(lab[..., 1].mean()) * 0.8, float(lab[..., 2].mean()) * 0.8)
    a = ab[0] + (lab[..., 1] - lab[..., 1].mean()) * sat
    b = ab[1] + (lab[..., 2] - lab[..., 2].mean()) * sat
    o = opaque(SP.oklab_to_rgb8(np.dstack([Lf, a, b])))
    o = TC.make_seamless(o, 14, seed)     # 粒だけなので縦横にずらしてよい
    return SP.rgb8_to_oklab(o[..., :3].astype(np.float64))


def slab_floor(grain, seed, courses=(32, 32), wmin=28, wmax=52, edge=(10, 22), joint_dL=-0.085, hl=0.012, sh=0.018, cracks=1,
               fade=0.22, dark_frac=(0.3, 0.58)):
    """天面の石板 (座席の大石板・笠石・敷石・柱頭)。戻り値: (タイル RGBA, 目地のマスク)。
    courses = 段の高さ (合計 64。4 種で同じ＝境をまたぐ石板の上下の目地が隣のタイルとそろう)。各段の石板の幅は wmin〜wmax の乱数・
    最初の縦の目地は x∈edge・最後の縦の目地は 64−edge[0] まで (隣のタイルとの境で石板が細くならない。石板が境をまたいでも面の粒は 4 種で同じ＝つながる)。
    目地は 1 テクセル・石より joint_dL だけ暗い (黒くしない＝静かな床)。石板の上の縁 +hl・下の縁 −sh・左の縁 +0.6hl・右の縁 −0.6sh。細いひび cracks 本。
    2026-10-03 直しの番 (反証の指摘 2): 段の高さが 4 種で同じなので、横の目地 (行 31・63) がどの種でも横幅いっぱいに通り、床に 32 テクセルおきの
    まっすぐな横線が切れ目なく並んだ。段の高さを種ごとに変えると、境をまたぐ石板の上下の目地が隣の種と合わなくなる (または境に縦の目地を通す＝
    64 ごとに縦の線が並ぶ。試して、見下ろしの画面の模型で縦の線の柵になったので捨てた)。そこで横の目地を石板 1 枚ごとの区切りで
    「砂で埋まった目地」(暗さ fade 倍・縁の明暗も同じ倍) にし、1 本の横の目地の暗い所が横幅の dark_frac (3〜6 割未満) に収まるようにした
    ＝線が石板 1〜2 枚ごとに切れる。縦の目地 (石の形が読める所) は全部暗いまま。行 0 の上の縁の光は付けない (上のタイルは別の種で、
    その行 63 の目地が埋まっているかが分からない)"""
    rng = np.random.default_rng(seed)
    n = 64
    L = grain[..., 0].copy()
    a = grain[..., 1].copy()
    b = grain[..., 2].copy()
    J = np.zeros((n, n), bool)
    D = np.zeros((n, n))          # 目地の暗さの倍率 (1 = joint_dL・fade = 埋まった目地)
    E = np.zeros((n, n))
    y = 0
    for h in courses:
        ys = np.arange(y, y + h)
        yb = ys[-1]
        x = int(rng.integers(edge[0], edge[1] + 1))
        xs = []
        while x <= n - edge[0] - 1:
            xs.append(x)
            x += int(rng.integers(wmin, wmax + 1))
        # この段の石板の下の目地を、石板ごとの区切り (縦の目地の間) に分ける。境をまたぐ石板は列 xs[-1]+1〜63 と 0〜xs[0]−1
        segs = [np.arange(xs[k] + 1, xs[k + 1]) for k in range(len(xs) - 1)]
        segs.append(np.concatenate([np.arange(xs[-1] + 1, n), np.arange(0, xs[0])]))
        widths = np.array([len(sg) for sg in segs], float)
        rowm = np.ones(n)                                      # この段の下の目地の行の、列ごとの暗さの倍率
        for _ in range(64):
            pick = rng.random(len(segs)) < 0.5
            dark = (widths[~pick].sum() + len(xs)) / n         # 縦の目地の列は暗いまま
            if dark_frac[0] <= dark <= dark_frac[1]:
                for sg, pk in zip(segs, pick):
                    if pk:
                        rowm[sg] = fade
                break
        else:
            # 石板の区切りでは割合に収まらない (縦の目地が 1 本だけの段＝境をまたぐ石板が横幅の大半) → いちばん長い区切りの中の一続きだけを埋める
            k = int(np.argmax(widths))
            sg = segs[k]
            want = int(round(0.45 * n))
            other = int(widths.sum() - widths[k]) + len(xs)
            m = int(np.clip(len(sg) - max(0, want - other), 0, len(sg) - 4))
            st = int(rng.integers(2, max(3, len(sg) - m - 1)))
            rowm[sg[st:st + m]] = fade
        J[yb, :] = True
        D[yb, :] = rowm
        D[yb, xs] = 1.0
        E[ys[-2], :] -= sh * rowm                              # 下の縁の影 (埋まった目地は弱く)
        if yb + 1 < n:
            E[yb + 1, :] += hl * rowm                          # 次の段の上の縁の光 (この段の下の目地と同じ区切り)
        for x in xs:
            J[ys, x] = True
            D[ys, x] = 1.0
            E[ys[:-1], (x + 1) % n] += hl * 0.6
            E[ys[:-1], (x - 1) % n] -= sh * 0.6
        y += h
    # 細いひび (石板の中だけ・斜めに 6〜12 テクセル・目地より浅い)
    for _ in range(cracks):
        cy, cx = int(rng.integers(4, n - 4)), int(rng.integers(0, n))
        dx = 1 if rng.random() < 0.5 else -1
        for i in range(int(rng.integers(6, 13))):
            yy, xx = (cy + i // 2) % n, (cx + i * dx) % n
            if not J[yy, xx] and rng.random() < 0.85:
                E[yy, xx] += joint_dL * 0.45
    L = L + E
    L = np.where(J, L + joint_dL * D, L)
    return opaque(SP.oklab_to_rgb8(np.dstack([L, a, b]))), J & (D > 0.5)


def floor_verdict(tile, gate, J=None):
    """天面の石板の門: ざらつき ≤ gate・左右の継ぎ目・横幅いっぱいの目地の行が無い (行 63 以外で目地が横幅の 6 割を超える行。
    J が無ければ「中央値より 10 暗い画素」で数える)。上下の境の段差は別の種どうしの門 (cross_seams の tb) で見る
    (行 63 は全部の種で目地＝境は目地の中)"""
    c = TC.check(tile)
    ok_edge = c['edgeMeanDiffLR'] <= max(2.0, c['innerStepP90X'])
    ok_seam = c['seamRatioX'] <= 1.6
    if J is None:
        l = luma(tile[..., :3])
        J = l < np.median(l) - 10
    rf = J[:-1].mean(1)
    c['jointRowMax'] = round(float(rf.max()), 2)
    c['jointRowMaxAt'] = int(rf.argmax())
    ok_rows = rf.max() <= 0.6
    c['fail'] = [k for k, v in (('lapvar', c['lapvar'] <= gate), ('edgeLR', ok_edge), ('seamLR', ok_seam), ('fullRow', ok_rows)) if not v]
    c['pass'] = not c['fail']
    return c


GEN_TILES = {
    # 名前: grain = (面の粒の元・明暗・彩度・明るさの平均 L・色みの平均 ab〔None = 元の色みの 0.8 倍〕・箱ぼかしの半径)、slab = slab_floor の引数、seed = 種の頭
    'top_floor_seat': dict(face='top', rot='flipX', gate=800, seed=3100,
                           grain=('act3_stone_a', 0.76, 0.25, 0.535, (-0.004, -0.006), 2),
                           slab=dict(courses=(32, 32), wmin=28, wmax=52, joint_dL=-0.085, hl=0.012, sh=0.018, cracks=1),
                           note='座席の帯の大石板 (コード生成)。石板 32×28〜52 テクセル (段の高さ 32 は 4 種で同じ＝横の目地がそろう)・目地は 1 テクセルで石より少し暗いだけ (黒くしない)・'
                                '石の面は act3_stone_a の面の細かい粒だけ (目地と縁を埋めてから半径 2 の箱ぼかしを引く＝64 ごとに大きな斑が並ばない)。明るさ 約 105・彩度の中央値 ≤0.12 (無彩に近い冷たい灰＝'
                                '本家 Tomb の光の当たる床 (92,93,95)〜(113,123,133))。回し方は flipX (rot4 だと横の目地が縦になる)'),
    'top_wall': dict(face='top', rot='flipX', gate=800, seed=3200,
                     grain=('act3_cliff_b', 0.26, 0.5, 0.49, None, 2),
                     slab=dict(courses=(32, 32), wmin=24, wmax=44, joint_dL=-0.16, hl=0.02, sh=0.03, cracks=1),
                     note='壁・胸壁の天面 (笠石。コード生成)。面の粒は side_wall と同じ act3_cliff_b から・笠石の目地は石より暗い (side_wall ほど黒くしない)。'
                          '天面は 5° で 5〜20px なので静かに'),
    'top_step': dict(face='top', rot='flipX', gate=800, seed=3300,
                     grain=('act3_stone_b', 0.28, 0.5, 0.53, None, 2),
                     slab=dict(courses=(32, 32), wmin=26, wmax=48, joint_dL=-0.14, hl=0.02, sh=0.025, cracks=1),
                     note='段の天面 (敷石。コード生成)。面の粒は side_step と同じ act3_stone_b から。本家の段は天面が立面より明るい (明るさ 約 107)'),
    'top_pillar': dict(face='top', rot='flipX', gate=800, seed=3400,
                       grain=('act3_cliff_c', 0.35, 0.4, 0.66, (float(BONE[1]), float(BONE[2])), 2),
                       slab=dict(courses=(21, 21, 22), wmin=20, wmax=40, joint_dL=-0.12, hl=0.02, sh=0.02, cracks=0),
                       note='柱頭・門の天面 (コード生成)。side_pillar と同じ act3_cliff_c の粒の骨白の石'),
}


def make_gen_tiles(pal):
    res = {}
    dst = os.path.join(OUT, 'tiles')
    os.makedirs(dst, exist_ok=True)
    for name, spec in GEN_TILES.items():
        src, con, sat, L, ab, rad = spec['grain']
        grain = stone_grain(src, con, sat, L, ab, radius=rad, seed=spec['seed'])
        for i, v in enumerate('abcd'):
            seed = spec['seed'] + i
            img, J = slab_floor(grain, seed, **spec['slab'])
            img = SP.map_image(img, palette_for(name, pal))[..., :3]
            save(img, os.path.join(dst, f'{name}_{v}.png'), 'RGB')
            MASKS[f'{name}_{v}'] = J
            c = floor_verdict(opaque(img), spec['gate'], J)
            c['src'] = f'コード生成 (gen_items.py slab_floor 種 {seed}・面の粒は Art/tiles/{src})'
            res[f'{name}_{v}'] = c
    return res


# ================================================================ 2. 半立体 (既存の小物の足元と皿を切る)
# 区分: keep = そのまま (色表へ写すだけ。薄い皿だけ切る物あり) / cut = 足元と皿を切る / derived = 約束の名前のために別の絵から作る /
#       held = 3D で組むので 10/7 まで置かない (段2 の裁定 2「人工の大物は 3D」) / discard = 捨てる
# 手直し (どれも省略可。上から順に):
#   band=(y0, x0, x1)      : 行 y0 から下で、列 x0〜x1 の外を透明に
#   footprint=(y0, k)      : 行 y0 から下で、その上の k 行の不透明の列の範囲の外を透明に (地面の皿は本体より横に広い)
#   ground=(y0, 種類) / [..]: 行 y0 から下で、地面の色の画素を透明に。種類は _ground_mask
#   colors=(y0, [rgb..], 許容差) : 行 y0 から下で、その色 (RGB の各成分の差が許容差以内) を透明に (色の決まった影の皿)
#   below=y                : 行 y から下を全部透明に
#   rects=[(x0,y0,x1,y1)]  : 矩形を透明に
# 最後に relief-clean の drop_specks (6 画素未満・最大の塊の 3% 未満の欠片) と trim (外接の四角＋余白 2) を通す。
FAR = '遠景の板の予備。判定の画には置かない (分析書 §5-1: 5° では壁の裏・街の輪郭・水道橋・館の遠景・機械の庭は外す)'
RELIEF = [
    # ---- cut: 足元と皿を切る
    ('block', 'cut', dict(ground=(28, 'warm'), below=38), '切石 (傾いたブロック)。下の桃色と茶の土の皿を切る'),
    ('brazier_cold', 'cut', dict(colors=(47, [(0x61, 0x73, 0x7f), (0x68, 0x79, 0x87), (0x5d, 0x6d, 0x7c)], 10), footprint=(47, 4)),
     '冷たい炎の火鉢 (青白い炎)。台の下の青灰の影の皿を切る。炎は _e.png'),
    ('urn', 'cut', dict(ground=(30, 'grass+moss+red+weed'), below=35), '壺。下の草と赤い花を切る (壺の足の台は残す)'),
    ('stele', 'cut', dict(footprint=(101, 5), ground=(100, 'warm'), below=106), '石碑。下の茶の土を切る (台座は石なので残す)。緑の紋は _e.png'),
    ('lamp_post', 'cut', dict(footprint=(95, 3), ground=(94, 'warm'), below=99),
     '灯柱 (等角で頼んだ絵)。台座は正面に近いので残し、下の菱形の土だけ切る。ガラスの灯は _e.png。灯柱を 3D の marker で組む時は頭だけの lamp_head を使う'),
    ('statue_headless', 'cut', dict(ground=(100, 'moss'), footprint=(105, 4)), '首の無い像。台座の下の苔と左の小石を切る'),
    ('pillar_broken', 'cut', dict(ground=(98, 'moss')), '折れ柱。根元の苔を切る (柱の体の苔は絵のまま)'),
    ('wall_ruin', 'cut', dict(band=(93, 17, 146), ground=(92, 'warm+moss'), below=101), '崩れた館の壁 (半立体の予備。館は 3D の block が本命)。下の砂と草と影の帯を切る'),
    ('rubble_pile', 'cut', dict(ground=(40, 'moss'), colors=(46, [(0xa1, 0x7b, 0x7d), (0x99, 0x76, 0x77), (0xa6, 0x80, 0x83), (0x76, 0x8f, 0x77)], 6), below=48),
     '瓦礫の山。下の草と灰桃の影の帯を切る'),
    ('pillar_fallen', 'cut', dict(ground=(30, 'moss'), colors=(38, [(0x6f, 0x77, 0x9b), (0x74, 0x7d, 0xa0), (0x69, 0x73, 0x92), (0x62, 0x67, 0x85)], 8), below=42),
     '倒れた柱。下の草と青灰の影の帯を切る'),
    ('fountain_dry', 'cut', dict(footprint=(67, 3), ground=(63, 'moss+grass+warm'),
                                 colors=(67, [(0xbf, 0xc0, 0xae), (0xc2, 0xc5, 0xb4), (0xb8, 0xb8, 0xa7), (0xa2, 0xa2, 0x95), (0x96, 0x91, 0x87), (0x90, 0x8b, 0x82),
                                              (0x9e, 0x9a, 0x90), (0x86, 0x80, 0x7b), (0x8c, 0x86, 0x7f), (0x7f, 0x7b, 0x7a), (0x60, 0x5c, 0x5f), (0x54, 0x51, 0x56)], 6),
                                 below=86), '枯れた噴水 (予備)。水盤のまわりの砂の皿と草を切る'),
    ('ore_cart', 'cut', dict(ground=(44, 'sand+moss+grass'), below=58), '鉱車 (予備・機械の庭)。下の砂と草を切る (地面に落ちた鉱は残す)。鉱は _e.png'),
    ('vein_wall', 'cut', dict(footprint=(47, 4), ground=(44, 'warm+moss'), below=52), '脈の結晶の岩 (壁の脈)。下の土と草と白い小石を切る。結晶は _e.png'),
    ('crystal_spire_big', 'cut', dict(ground=(134, 'notcrystal')), '大結晶の尖塔。根元の白い縁と黒い岩を切る (岩は 3D の rock に任せる)。結晶は _e.png'),
    ('skyline_a', 'cut', dict(below=69), '街の輪郭 (遠景の予備)。下半分に描き込まれた水の映りを切る (映りは箱庭の水に任せる)。' + FAR),
    ('drill_rig', 'cut', dict(below=145), '掘削の腕 (遠景の予備・等角)。下の白い地面の線を切る。' + FAR),
    # ---- keep: そのまま (色表へ写すだけ)
    ('crystal_cluster', 'keep', dict(ground=(60, 'moss')), '結晶の群れ (根元の岩ごと＝足元を切ると結晶の下端が切り口になる)。右下の苔だけ切る。結晶は _e.png'),
    ('statue_kneel', 'keep', dict(), '跪く像 (台座は石なので残す)。手の玉は _e.png。約束の名前 statue でも同じ絵を置く'),
    ('monument_disc', 'keep', dict(), '歯車形の碑。芯の青緑は _e.png'),
    ('ceiling_fissure', 'keep', dict(), '天井の裂け目 (壁の上端・柱の出所)。青緑の光は _e.png'),
    ('ceiling_fringe', 'keep', dict(), '天井の房 (上の額縁の吊り物)'),
    ('chain_hang', 'keep', dict(), '吊り鎖 (吊り物)'),
    ('lamp_hang', 'keep', dict(), '吊り灯 (吊り物・青白い結晶の灯)。ガラスは _e.png'),
    ('stalactite_cluster', 'keep', dict(), '結晶の鍾乳石 (吊り物)'),
    ('frieze', 'keep', dict(), '紋の帯 (段鼻の帯飾り＝壁に貼る札)'),
    ('aqueduct_far', 'keep', dict(), '水道橋 (遠景の予備)。' + FAR),
    ('facade_far', 'keep', dict(), '館の正面 (遠景の予備)。' + FAR),
    ('skyline_b', 'keep', dict(), '街の輪郭 (遠景の予備)。' + FAR),
    ('boiler_pipes', 'keep', dict(), 'ボイラー (遠景の予備・機械の庭)。窓の青緑は _e.png。' + FAR),
    ('gear_wall', 'keep', dict(), '歯車の壁 (遠景の予備・機械の庭)。' + FAR),
    # ---- derived: 約束の名前 (C3 の設計図が読む) のために別の絵から作る
    ('statue', 'derived', dict(base='statue_kneel'), '像 1 (約束の名前)。元の act3_statue は等角 (丸い台座の天面が見える) なので、正面の跪く像 statue_kneel の絵をこの名前で置く'),
    ('brazier', 'derived', dict(base='brazier_cold', fire=True),
     '篝火 (約束の名前・暖色の火)。元の act3_brazier は等角で炎が青白い。正面の石の火鉢 brazier_cold の器のまま、青白い炎の画素を明るさの順に本家 Tomb の篝火の色 (FIRE 5 段) へ描き直した。炎は _e.png'),
    ('lamp_head', 'derived', dict(base='lamp_post', head=31), '灯柱の頭だけ (行 0〜30 のガラスの籠と首)。灯柱を 3D の marker で組む時に頭へ載せる。ガラスは _e.png'),
    # ---- held: 3D で組むので 10/7 まで置かない (裁定 2)
    ('arch_ruin', 'held', dict(), '崩れたアーチは 3D の arch で組む (裁定 2)。絵は予備に残す (区分を cut に変えて足元の草を切れば使える)'),
    ('balustrade', 'held', dict(), '欄干は 3D の fence で組む。絵は等角 (天面が見える)'),
    ('gate_great', 'held', dict(), '大門は 3D の arch で組む。絵は等角 (天面が見える)・28,160px。紋の帯は 10/7 の act3_gate_carving_band'),
    ('pillar_tall', 'held', dict(), '高い柱は 3D の pillar で組む (柱の面のタイル side_pillar／top_pillar)。柱頭の絵は 10/7'),
    ('rig_far', 'held', dict(), '櫓は 3D の rig (幕1 と同じ形・絵が要らない)'),
    # ---- discard: 捨てる
    ('aqueduct', 'discard', dict(), '等角の水道橋。aqueduct_far がある'),
    ('brazier', 'discard', dict(), '等角の火鉢 (台座の天面が見える・炎は青白い)。名前 brazier は brazier_cold の器から作った篝火に使う'),
    ('chain', 'discard', dict(), '等角の鎖。chain_hang がある'),
    ('gate', 'discard', dict(), '等角の小門。大門は 3D の arch'),
    ('pillar', 'discard', dict(), '等角の柱。柱は 3D の pillar'),
    ('spire', 'discard', dict(), '等角の結晶の尖塔。crystal_spire_big がある'),
    ('statue', 'discard', dict(), '等角の立像 (丸い台座の天面が見える)。名前 statue は statue_kneel の絵に使う'),
]


def _ground_mask(a, kinds):
    h, s, v = hsv(a)
    al = a[..., 3] >= 128
    m = np.zeros(al.shape, bool)
    for k in kinds.split('+'):
        if k == 'sand':        # 砂と黄土 (明るい黄土〜淡い橙)
            m |= (h >= 12) & (h <= 52) & (s >= 0.18) & (v >= 0.42)
        elif k == 'grass':     # 草の緑 (結晶の青緑は 160° より上なので外す)
            m |= (h >= 50) & (h <= 160) & (s >= 0.18)
        elif k == 'moss':      # 鮮やかな草と苔 (彩度 0.45 以上。像の衣の淡い緑灰は残る)
            m |= (h >= 45) & (h <= 160) & (s >= 0.45)
        elif k == 'weed':      # 草の暗い縁取り (暗い青緑)
            m |= (h >= 140) & (h <= 200) & (s >= 0.2) & (v < 0.42)
        elif k == 'red':       # 赤い花
            m |= ((h <= 15) | (h >= 335)) & (s >= 0.4)
        elif k == 'warm':      # 暖色と紫の土 (色相 255〜52°・彩度 0.08 以上・明るさ 0.2 以上)。青灰の石の物にだけ使う
            m |= ((h >= 255) | (h <= 52)) & (s >= 0.08) & (v >= 0.2)
        elif k == 'dirt':      # 茶色の土
            m |= (h >= 8) & (h <= 40) & (s >= 0.3) & (v >= 0.22) & (v <= 0.62)
        elif k == 'shadow':    # 色みの無い灰の影の帯
            m |= s < 0.15
        elif k == 'notcrystal':   # 結晶 (青緑・明るい) でない物 = 根元の岩と縁
            m |= ~((h >= 150) & (h <= 200) & (s >= 0.12) & (v >= 0.4))
        else:
            sys.exit('知らない地面の種類: ' + k)
    return m & al


def clean_relief(a, ops):
    out = a.copy()
    H, W = out.shape[:2]
    xs = np.arange(W)
    if 'band' in ops:
        y0, x0, x1 = ops['band']
        out[y0:, (xs < x0) | (xs > x1), 3] = 0
    if 'footprint' in ops:
        y0, k = ops['footprint']
        ref = out[max(0, y0 - k):y0, :, 3] >= 128
        cols = np.nonzero(ref.any(0))[0]
        if len(cols):
            x0, x1 = int(cols.min()), int(cols.max())
            out[y0:, (xs < x0) | (xs > x1), 3] = 0
    if 'ground' in ops:
        for y0, kinds in (ops['ground'] if isinstance(ops['ground'], list) else [ops['ground']]):
            m = _ground_mask(out, kinds)
            m[:y0] = False
            out[m, 3] = 0
    if 'colors' in ops:
        y0, cols, tol = ops['colors']
        rgb = out[..., :3].astype(int)
        m = np.zeros((H, W), bool)
        for c in cols:
            m |= (np.abs(rgb - np.array(c)) <= tol).all(-1)
        m[:y0] = False
        out[m, 3] = 0
    if 'below' in ops:
        out[ops['below']:, :, 3] = 0
    for (x0, y0, x1, y1) in ops.get('rects', []):
        out[y0:y1, x0:x1, 3] = 0
    out[out[..., 3] < 128] = 0          # 半透明は作らない (α は 0 か 255)
    out[out[..., 3] >= 128, 3] = 255
    out = RC.drop_specks(out)
    al = out[..., 3] >= 128
    ys, xs2 = np.nonzero(al)
    off = (max(0, int(xs2.min()) - 2), max(0, int(ys.min()) - 2))   # RC.trim の余白 2 と同じ = 元の絵の座標からの引き算
    return RC.trim(out), off


# 篝火: 青白い炎の画素 (器より上の行・色相 180〜230° か白い芯) を明るさの順に FIRE の 5 段へ
FIRE_ROWS = 26          # brazier_cold の器の縁の行 (これより上が炎。器の中の照り返しの 2 行を含む)


def fire_mask(a, rows=FIRE_ROWS, warm=False):
    """炎の画素 (rows より上の行)。warm=False = 青白い炎 (色相 170〜235° か白い芯)・warm=True = 描き直した暖色の炎 (色相 15〜65°)"""
    h, s, v = hsv(a)
    al = a[..., 3] >= 128
    if warm:
        m = al & (h >= 15) & (h <= 65) & (s >= 0.2) & (v >= 0.5)
    else:
        m = al & ((((h >= 170) & (h <= 235)) & (s >= 0.15) & (v >= 0.38)) | ((s < 0.15) & (v >= 0.85)))
    m[rows:] = False
    return m


def warm_fire(a):
    out = a.copy()
    m = fire_mask(a)
    l = luma(a[..., :3])
    edges = [130, 160, 190, 225]
    idx = np.digitize(l, edges)
    for i, c in enumerate(FIRE):
        out[m & (idx == i), :3] = c
    return out


def make_relief(work):
    """作業場 work/relief/<名前>.png (色表へ写す前)。戻り値: 名前 → 記録 (held・discard は 'held:<名>'・'discard:<名>' の鍵)"""
    d = os.path.join(work, 'relief')
    os.makedirs(d, exist_ok=True)
    rec = {}
    for name, group, ops, why in RELIEF:
        if group in ('held', 'discard'):
            a = load_rgba(os.path.join(PROPS, 'act3_' + name + '.png'))
            rec[group + ':' + name] = dict(name=name, group=group, src='Art/props/act3_' + name, why=why, srcDots=[int(a.shape[1]), int(a.shape[0])])
            continue
        if group == 'derived':
            base = ops['base']
            bops = next(o for n, g, o, w in RELIEF if n == base and g in ('cut', 'keep'))
            a = load_rgba(os.path.join(PROPS, 'act3_' + base + '.png'))
            src = 'Art/props/act3_' + base
            if ops.get('fire'):
                a = warm_fire(a)
            o, off = clean_relief(a, bops)
            if ops.get('head'):
                hrow = ops['head'] - off[1]
                o = o[:hrow].copy()
                o, off2 = clean_relief(o, {})
                off = (off[0] + off2[0], off[1] + off2[1])
            r = dict(name=name, group=group, src=src, why=why, srcDots=[int(a.shape[1]), int(a.shape[0])], ops=dict(bops, **{k: v for k, v in ops.items() if k != 'base'}),
                     base=base)
        else:
            a = load_rgba(os.path.join(PROPS, 'act3_' + name + '.png'))
            o, off = clean_relief(a, ops)
            r = dict(name=name, group=group, src='Art/props/act3_' + name, why=why, srcDots=[int(a.shape[1]), int(a.shape[0])], ops=dict(ops))
        save(o, os.path.join(d, name + '.png'))
        r['dots'] = [int(o.shape[1]), int(o.shape[0])]
        r['offset'] = list(off)   # 切り詰めで動いた量 (元の絵の座標 − この値 = 切った後の座標。発光の範囲 rect を写す)
        rec[name] = r
        print(f'{name:18s} {group:8s} {r["srcDots"][0]}×{r["srcDots"][1]} → {o.shape[1]}×{o.shape[0]}  {why[:60]}')
    return rec


# ================================================================ 6b. 発光 (結晶・裂け目・脈・灯・篝火の火・紋)
# 帯は sprite-normals の PRESETS と同じ形 (色相 度・彩度・明るさ)。rect = 光らせてよい範囲 (元の絵の座標・無ければ全体)
TEAL = {'name': '結晶', 'hue': (150, 200), 'sat': (0.12, 1.0), 'val': (0.62, 1.0)}
EMISSION = {
    'crystal_cluster': dict(bands=[TEAL]),
    'crystal_spire_big': dict(bands=[TEAL]),
    'vein_wall': dict(bands=[{'name': '脈の結晶', 'hue': (130, 200), 'sat': (0.12, 1.0), 'val': (0.6, 1.0)}]),
    'ceiling_fissure': dict(bands=[{'name': '裂け目の光', 'hue': (165, 205), 'sat': (0.3, 1.0), 'val': (0.5, 1.0)}]),
    'lamp_hang': dict(bands=[{'name': 'ガラスの灯', 'hue': (130, 210), 'sat': (0.06, 1.0), 'val': (0.6, 1.0)},
                             {'name': 'ガラスの照り', 'hue': (0, 360), 'sat': (0.0, 0.12), 'val': (0.85, 1.0)}]),
    'lamp_post': dict(bands=[{'name': 'ガラスの灯', 'hue': (170, 240), 'sat': (0.1, 1.0), 'val': (0.5, 1.0)}], rect=(0, 0, 48, 27)),
    'lamp_head': dict(bands=[{'name': 'ガラスの灯', 'hue': (170, 240), 'sat': (0.1, 1.0), 'val': (0.5, 1.0)}], rect=(0, 0, 48, 27)),
    'brazier_cold': dict(fire='cold'),
    'brazier': dict(fire='warm'),
    'statue_kneel': dict(bands=[{'name': '手の玉', 'hue': (150, 195), 'sat': (0.25, 1.0), 'val': (0.5, 1.0)}], rect=(20, 36, 44, 60)),
    'statue': dict(bands=[{'name': '手の玉', 'hue': (150, 195), 'sat': (0.25, 1.0), 'val': (0.5, 1.0)}], rect=(20, 36, 44, 60)),
    'monument_disc': dict(bands=[{'name': '芯の光', 'hue': (150, 195), 'sat': (0.2, 1.0), 'val': (0.5, 1.0)}], rect=(40, 30, 90, 80)),
    'stele': dict(bands=[{'name': '紋の光', 'hue': (140, 190), 'sat': (0.3, 1.0), 'val': (0.55, 1.0)}]),
    'ore_cart': dict(bands=[{'name': '鉱の結晶', 'hue': (160, 195), 'sat': (0.25, 1.0), 'val': (0.55, 1.0)}]),
    'boiler_pipes': dict(bands=[{'name': '窓の光', 'hue': (150, 195), 'sat': (0.3, 1.0), 'val': (0.45, 1.0)}], rect=(28, 80, 72, 112)),
    'drill_rig': dict(bands=[{'name': '芯の光', 'hue': (150, 195), 'sat': (0.15, 1.0), 'val': (0.6, 1.0)}], rect=(58, 118, 112, 150)),
    'aqueduct_far': dict(bands=[{'name': '紋の光', 'hue': (160, 195), 'sat': (0.5, 1.0), 'val': (0.6, 1.0)}]),
}


def emission_of(name, a, off=(0, 0)):
    """発光のマスク。rect は元の絵 (Art/props/act3_*) の座標で書き、off (切り詰めで動いた量) を引いて当てる。fire = 炎の画素 (器より上)"""
    spec = EMISSION[name]
    if spec.get('fire'):
        rows = max(0, FIRE_ROWS - off[1])
        m = fire_mask(a, rows, warm=spec['fire'] == 'warm')
        out = np.zeros_like(a)
        out[..., :3][m] = a[..., :3][m]
        out[..., 3] = a[..., 3]
        return out
    m, cnt = SN.emission_mask(a, spec['bands'])
    if 'rect' in spec:
        x0, y0, x1, y1 = spec['rect']
        x0, x1, y0, y1 = x0 - off[0], x1 - off[0], y0 - off[1], y1 - off[1]
        keep = np.zeros(a.shape[:2], bool)
        keep[max(0, y0):max(0, y1), max(0, x0):max(0, x1)] = True
        m[~keep, :3] = 0
    return m


# ================================================================ 3. 色表
def build_palette(work, rec, k_bulk=23, k_accent=5):
    """色表 = (1) 本家 Tomb の錨 (壁の暗部 6・柱の白の段 9・柱の光の冷たい灰 2・座席の床の灰の段 6・篝火の炎 5) と冷たい灯 4・地衣 4 を先に
    ＋ (2) 石積みと天面の下書きと切った後の小物の色 (画素数の重み・Oklab・明るさ 1.5 倍の k-means k_bulk)
    ＋ (3) 発光の差し色 (結晶・灯・紋。画素数の平方根の重みの k-means k_accent＝少ない画素でも残る)。
    同じ色に丸まった物を落として明るさ順に並べる (stage-palette の形で保存。合計 64 以下)。
    錨を先に置くのは、既存の絵の「夜の青緑の焼き込み」だけから作ると色が 195〜205° の単色に寄るため (反証 verify-score §1-9)"""
    files = sorted([os.path.join(work, 'calm', f) for f in os.listdir(os.path.join(work, 'calm'))]) + \
        sorted([os.path.join(work, 'relief', f) for f in os.listdir(os.path.join(work, 'relief'))])
    px = np.concatenate([SP.opaque_pixels(load_rgba(f))[0] for f in files], 0)
    uniq, cnt = np.unique(px.reshape(-1, 3), axis=0, return_counts=True)
    sc = np.array([1.5, 1.0, 1.0])
    lab = SP.rgb8_to_oklab(uniq)
    bulk = SP.oklab_to_rgb8(SP.weighted_kmeans(lab * sc, cnt.astype(np.float64), k_bulk) / sc)
    acc = []
    for name in EMISSION:
        if name not in rec or EMISSION[name].get('fire'):
            continue
        a = load_rgba(os.path.join(work, 'relief', name + '.png'))
        m = emission_of(name, a, rec[name].get('offset', (0, 0)))
        hit = m[..., :3].max(-1) > 0
        acc.append(a[..., :3][hit])
    acc = np.concatenate(acc, 0)
    ua, ca = np.unique(acc, axis=0, return_counts=True)
    accent = SP.oklab_to_rgb8(SP.weighted_kmeans(SP.rgb8_to_oklab(ua) * sc, np.sqrt(ca.astype(np.float64)), k_accent) / sc)
    anchors = np.array(TOMB_DARK + PILLAR_RAMP + TOMB_COLUMN + FLOOR_RAMP + FIRE + COLD + MOSS, dtype=np.uint8)
    rgb = np.unique(np.concatenate([anchors, bulk, accent], 0), axis=0)
    lab2 = SP.rgb8_to_oklab(rgb)
    order = np.lexsort((np.arctan2(lab2[:, 2], lab2[:, 1]), lab2[:, 0]))
    rgb = rgb[order]
    SP.save_palette(rgb, PAL_BASE)
    print(f'色表 {len(rgb)} 色 (本家の錨 {len(anchors)}・塊 {len(bulk)}・差し色 {len(accent)}) → {os.path.relpath(PAL_JSON, REPO)}')
    if len(rgb) > 64:
        sys.exit('色表が 64 色を超えた')
    return rgb


def ramp(pal, targets):
    """目標の RGB に Oklab でいちばん近い色表の色を、並べた順に返す (同じ色を2度使わない＝段が潰れない)"""
    pl = SP.rgb8_to_oklab(pal)
    out, used = [], set()
    for t in targets:
        d = ((pl - SP.rgb8_to_oklab(np.array(t))) ** 2).sum(1)
        for i in np.argsort(d):
            if int(i) not in used:
                used.add(int(i))
                out.append(tuple(int(v) for v in pal[int(i)]))
                break
    return out


# ================================================================ 4. 色表へ写す
FIRE_ONLY = {'brazier'}     # 篝火の明るい炎の色 (FIRE の明るい 3 段) へ写してよい絵。ほかの絵は色表からこの 3 色を除いて写す
                            # (光のルール「暖色は灯の範囲だけ」。除かないと像の白い縁・倒れた柱のクリーム・苔の黄緑が炎の黄土へ写った)


def palette_for(name, pal):
    if name in FIRE_ONLY:
        return pal
    hot = {tuple(c) for c in FIRE[2:]}
    return np.array([c for c in pal if tuple(int(v) for v in c) not in hot], dtype=pal.dtype)


def map_all(work, pal):
    """戻り値: 絵の名前 → 写す前の色表からの距離 p95 (ΔE_ok×100。写した後は 0)"""
    before = {}
    for sub in ('calm', 'relief'):
        dst = os.path.join(OUT, 'tiles' if sub == 'calm' else 'relief')
        os.makedirs(dst, exist_ok=True)
        for f in sorted(os.listdir(os.path.join(work, sub))):
            a = load_rgba(os.path.join(work, sub, f))
            pl = palette_for(os.path.splitext(f)[0], pal)
            d = SP.palette_distance(a, pl)
            before[os.path.splitext(f)[0]] = round(float(np.percentile(d, 95)), 2) if len(d) else 0.0
            o = SP.map_image(a, pl)
            if sub == 'calm':
                save(o[..., :3], os.path.join(dst, f), 'RGB')
            else:
                save(o, os.path.join(dst, f))
    return before


# ================================================================ 5. 小札 (瓦礫・欠片・砂利・小石)
def litter_items(C):
    """小札 (背丈 12 ドット以下・座席の帯にも置ける)。どれも不規則 (粒を等間隔に並べない)。左上が明るく右下に影 (光は左上から)"""
    S, k = C['rock'], C['k']
    P = C['pale']
    col = {'h': S[3], 'm': S[2], 's': S[1], 'd': S[0], 'k': k, 'p': P[0], 'q': P[1]}
    sp = GA.sprite
    out = {}
    # 瓦礫: 角ばった石の欠片 3〜5 個の不規則な塊 (大きさと間がばらばら・角が欠けた切石)
    out['rubble_1'] = sp(['..hhm.......', '.hmmms..hm..', 'hmmmss.hmms.', '.dsssk.kdss.', '..kkk....kk.'], col)
    out['rubble_2'] = sp(['......ph.....', '.hm..hmmm....', 'hmms.hmmss.hm', '.kks.dmsssmss', '.....kkkkk.kk'], col)
    out['rubble_3'] = sp(['.hm......', 'hmms.hhm.', 'dsss.mmss', '.kk..dssk', '......kk.'], col)
    out['rubble_4'] = sp(['...hhm..........', '..hmmms....hm...', '.hmmmsss..hmms..', 'hmmssssk..dssk.h', '.dddsskk...kk.hm', '..kkkk.......kk.'], col)
    # 欠片: 薄く平たい石の欠片 (骨白の縁が光る・長さと向きがばらばら)
    out['chip_1'] = sp(['..pqm..', 'pqmmsk.', '.kkk...'], col)
    out['chip_2'] = sp(['pmm.....', '.ksm.pq.', '....kmsk'], col)
    out['chip_3'] = sp(['.....pm', '.pqm.ks', 'qmssk..', '.kk....'], col)
    out['chip_4'] = sp(['pqmm....', '.kssmpm.', '....kkss'], col)
    # 砂利: 小さな石の粒 3〜6 個のまばらな塊 (C3 の設計図 gen_act3.py が gravel_<n> の名前で読む＝幕2 の小札と同じ名前。粒の間はばらばら)
    out['gravel_1'] = sp(['.hm....', 'hms..hm', '.k..hms', '.....k.'], col)
    out['gravel_2'] = sp(['....hm...', 'hm.hmms..', 'mk..kk.hm', '.......mk'], col)
    out['gravel_3'] = sp(['hm..h.', 'mk.hms', '....k.'], col)
    out['gravel_4'] = sp(['..hm.....h', 'hmmk..hm.m', '.k...hmsk.', '......k...'], col)
    # 小石: 1 個ずつの丸みのある石
    out['pebble_1'] = sp(['.hm.', 'hmms', '.kks'], col)
    out['pebble_2'] = sp(['..hmm.', '.hmmms', 'hmmsss', '.kkkk.'], col)
    out['pebble_3'] = sp(['hm.', 'mss', 'kk.'], col)
    out['pebble_4'] = sp(['.hmm..', 'hmmmms', '.mssss', '..kkk.'], col)
    return out


def gen_colors(pal):
    """コード生成の色の並び (どれも色表の色だけ)"""
    return {
        # 石の欠片 (壁の青緑の灰・暗→明 4 段)
        'rock': ramp(pal, [(26, 44, 52), (44, 66, 74), (66, 88, 96), (92, 112, 118)]),
        # 欠片の骨白の縁 (柱の白の段)
        'pale': ramp(pal, [(124, 127, 117), (156, 160, 147)]),
        # 接地の影 (壁の暗部)
        'k': ramp(pal, [(8, 30, 40)])[0],
    }


def make_litter(pal):
    C = gen_colors(pal)
    ldst = os.path.join(OUT, 'litter')
    os.makedirs(ldst, exist_ok=True)
    lit = {}
    for name, img in litter_items(C).items():
        img = SP.map_image(img, palette_for(name, pal))
        save(img, os.path.join(ldst, name + '.png'))
        lit[name] = [int(img.shape[1]), int(img.shape[0])]
    return lit


# ================================================================ 6. 法線と発光
# 暗い穴 (門・館の戸口・窓) は法線を平らにする: 輪郭からの丸みで膨らませると、穴が枕のように盛り上がって光る
NORMAL_FLAT_DARK = {'wall_ruin': 24, 'facade_far': 22}   # 名前: この明るさ (輝度) より暗い画素は平らな法線


def make_normals_emission(rec):
    d = os.path.join(OUT, 'relief')
    for name, r in rec.items():
        if r['group'] in ('held', 'discard'):
            continue
        p = os.path.join(d, name + '.png')
        a = load_rgba(p)
        nm = SN.stage_normal_map(a)
        if name in NORMAL_FLAT_DARK:
            dark = (luma(a[..., :3]) < NORMAL_FLAT_DARK[name]) & (a[..., 3] >= 128)
            nm[dark, 0], nm[dark, 1], nm[dark, 2] = 128, 128, 255
        save(nm, os.path.join(d, name + '_n.png'))
        r['normal'] = ARTP + 'relief/' + name + '_n'
        if name in EMISSION:
            m = emission_of(name, a, r.get('offset', (0, 0)))
            n = int((m[..., :3].max(-1) > 0).sum())
            save(m, os.path.join(d, name + '_e.png'))
            r['emission'] = ARTP + 'relief/' + name + '_e'
            r['emissionPx'] = n
            r['emissionFrac'] = round(n / max(1, int((a[..., 3] >= 128).sum())), 3)
            r['glow'] = glow_of(a, m)


def glow_of(a, m, tpu=25.0, cut=16):
    """光る所の中心 (2026-10-03 直しの番・反証の指摘 3)。設計図 (C3) が暈 halo と lights の at の高さを「絵の高さ × 係数」で決めると、
    絵の光る所と合わなかった (灯の頭は約 0.25 unit 上・篝火は透明な余白を高さに入れて約 0.16 unit 上)。ここで測って index.json に書く。
    Diorama (DioramaTextures.BuildAtlas) は α > 16 の外接の四角で切ってから足元の中心 (下端の真ん中) を原点にするので、同じ切り方で測る:
      y      = 見えている絵の下端 (いちばん下の不透明の行の下の縁) から光の重心 (発光の明るさの重み) までの高さ (unit)
      x      = 見えている絵の横の真ん中から光の重心まで (unit・右が正。左右反転して置いた時は符号を返す)
      yRange = 光る行 (発光の重みの 10〜90%) の下の縁と上の縁の高さ (unit) / yMid = yRange の真ん中 /
      visible = 見えている絵の幅と高さ (unit) / yFrac = y ÷ visible の高さ。
    設計図は暈 halo の中心と lights の at の高さに「置いた高さ ＋ y」を使う (絵の高さ × 係数は使わない)"""
    al = a[..., 3] > cut
    if not al.any():
        return None
    ys, xs = np.where(al)
    y0, y1, x0, x1 = ys.min(), ys.max(), xs.min(), xs.max()
    w = luma(m[..., :3]) * (m[..., :3].max(-1) > 0)
    if w.sum() <= 0:
        return None
    gy, gx = np.mgrid[0:a.shape[0], 0:a.shape[1]]
    cy = float((gy * w).sum() / w.sum()) + 0.5
    cx = float((gx * w).sum() / w.sum()) + 0.5
    # 光る行の範囲は発光の重みの 10〜90% (灯の頭の笠の縁の 1〜2 画素のような飛び地で範囲が伸びないように)
    cw = np.cumsum(w.sum(1)) / w.sum()
    e_top = int(np.searchsorted(cw, 0.10))
    e_bot = int(np.searchsorted(cw, 0.90))
    bottom = y1 + 1
    vis_h = (y1 - y0 + 1) / tpu
    lo, hi = (bottom - (e_bot + 1)) / tpu, (bottom - e_top) / tpu
    return dict(y=round((bottom - cy) / tpu, 3), x=round((cx - (x0 + x1 + 1) / 2.0) / tpu, 3),
                yRange=[round(lo, 3), round(hi, 3)], yMid=round((lo + hi) / 2, 3),
                visible=[round((x1 - x0 + 1) / tpu, 3), round(vis_h, 3)], yFrac=round((bottom - cy) / tpu / vis_h, 3))


# ================================================================ 5b. 画面での細かさの模型 (座席の床・幕2 の gen_items.py と同じ式)
def screen_fineness(tile, mean_to=None, sx=4, sy=0.5, rep=6):
    """座席の床が画面でどう見えるかの模型: タイルを rep×rep に敷き、横は 1 テクセル = sx px (PC の座席の 1 ドット 4px)、
    縦は見下ろし 5° で約 1/8 に潰れる＝1 テクセル = sy px (面積の平均で縮める)。明るさを mean_to にそろえ (無ければそのまま)、
    本家の物差しと同じ「σ0.7 のぼかしの後のラプラシアン分散」を返す (本家 Tomb の床は灯なしで 12〜16・幕2 の撮影÷模型は約 0.6〜0.7)"""
    l = luma(tile[..., :3])
    big = np.tile(l, (rep, rep))
    H, W = big.shape
    big = np.repeat(big, sx, axis=1)
    k = int(round(1 / sy))
    big = big[:(H // k) * k].reshape(H // k, k, -1).mean(1)
    if mean_to:
        big = big * (mean_to / max(1e-6, big.mean()))
    s = 0.7
    r = int(3 * s) + 1
    x = np.arange(-r, r + 1)
    kern = np.exp(-x * x / (2 * s * s))
    kern /= kern.sum()
    Hb, Wb = big.shape
    p = np.pad(big, r, mode='wrap')
    t = sum(kv * p[:, i:i + Wb] for i, kv in enumerate(kern))
    t = sum(kv * t[i:i + Hb, :] for i, kv in enumerate(kern))
    lp = t[:-2, 1:-1] + t[2:, 1:-1] + t[1:-1, :-2] + t[1:-1, 2:] - 4 * t[1:-1, 1:-1]
    return float(lp[4:-4, 4:-4].var())


# ================================================================ 7. 一覧
# 発光 _e.png を読む口は、2026-10-03 の時点でキャラの板 (StageUnits) にしか無い。箱庭の道 (Diorama・DioramaTextures) は
# 半立体の _n だけ読み、_e には触れない＝今は結晶・灯・篝火の火は描き込まれた色のままで、発光としては効かない (幕2 と同じ申し送り)
EMISSION_UNUSED = '箱庭は未使用 (2026-10-03: Diorama・DioramaTextures は半立体の _n だけ読み _e を読まない。光らせるには半立体の材料に _e の口が要る＝統合か S への申し送り)'


LIGHT_SIDED = ('左', '左上', '左下', '右', '右上', '右下')


def flip_rule(light):
    """左右反転して置いてよいか (2026-10-03 直しの番・反証の指摘 5)。光の向き (art-lint の lint_character) に左か右が入る絵は、反転すると
    描き込まれた光が反対の側からになり、art-bible の「光は左上から」に逆らう＝反転しない。上・下・なしの絵は反転してよい"""
    if light in LIGHT_SIDED:
        return dict(flipOk=False, flipNote=f'光が{light}から描き込まれている。左右反転すると光が{light.replace("左", "右") if "左" in light else light.replace("右", "左")}からになる＝反転して置かない')
    return dict(flipOk=True)


def write_index(tile_res, gen_res, rec, lit, seams=None):
    mats = {}
    for name, spec in list(MASON.items()) + list(CALM.items()):
        keys = [f'{name}_{v}' for v in 'abcd']
        m = dict(face=spec['face'], rot=spec['rot'], art=[ARTP + f'tiles/{k}' for k in keys], calm=1, sat=1,
                 note=spec['note'], src=[tile_res[k]['src'] for k in keys],
                 lapvar=[tile_res[k]['lapvar'] for k in keys], pass_=[tile_res[k]['pass'] for k in keys],
                 palP95Before=[tile_res[k].get('palP95Before') for k in keys],
                 lumaMean=[tile_res[k].get('lumaMean') for k in keys])
        if name in MASON:
            m['kind'] = '石積みの calm (目地を沈める)'
            m['faceLapvar'] = [tile_res[k].get('faceLapvar') for k in keys]
            m['gateFaceLapvar'] = spec['gate']
            m['jointFrac'] = [tile_res[k].get('jointFrac') for k in keys]
            m['courseRows'] = [tile_res[k].get('bands') for k in keys]
        else:
            m['kind'] = 'tile-calm'
            m['contrastUsed'] = [tile_res[k].get('contrastUsed') for k in keys]
        mats[name] = m
    for name, spec in GEN_TILES.items():
        keys = [f'{name}_{v}' for v in 'abcd']
        m = dict(face=spec['face'], rot=spec['rot'], art=[ARTP + f'tiles/{k}' for k in keys], calm=1, sat=1, note=spec['note'], kind='コード生成',
                 src=[gen_res[k]['src'] for k in keys], lapvar=[gen_res[k]['lapvar'] for k in keys], pass_=[gen_res[k]['pass'] for k in keys],
                 gateLapvar=spec['gate'], lumaMean=[gen_res[k].get('lumaMean') for k in keys],
                 screenFineness=[gen_res[k].get('screenFineness') for k in keys],
                 screenFinenessLit=[gen_res[k].get('screenFinenessLit') for k in keys],
                 satMedian=[gen_res[k].get('satMedian') for k in keys])
        mats[name] = m
    for name, spec in GEN_TILES.items():
        keys = [f'{name}_{v}' for v in 'abcd']
        mats[name]['jointRowMax'] = [gen_res[k].get('jointRowMax') for k in keys]
        mats[name]['layout'] = ('段 %s (4 種で同じ＝境をまたぐ石板の上下の目地がそろう)・横の目地は石板 1 枚ごとの区切りで「砂で埋まった目地」(暗さ %s 倍) を混ぜ、'
                                '1 本の横の目地の暗い所は横幅の 3〜6 割未満 (横幅いっぱいの線にしない)・縦の目地は全部暗いまま' % (list(spec['slab']['courses']), 0.22))
    for name, m in mats.items():
        m['pass'] = m.pop('pass_')
        if seams and name in seams:
            m['seams'] = seams[name]
            m['seamGate'] = dict(SEAM_GATE, note='別の種どうしを (回し方 flipX なら反転も) 並べた時の継ぎ目。lrRatio ≤ lr・tbRatio ≤ tb (gen_items.py cross_seams)')
            m['seamPass'] = seams[name]['pass']
    relief, held, discarded = {}, {}, {}
    for key, r in rec.items():
        if r['group'] == 'held':
            held[r['name']] = dict(from_=r['src'], srcDots=r['srcDots'], reason=r['why'])
            continue
        if r['group'] == 'discard':
            discarded[r['name']] = dict(from_=r['src'], srcDots=r['srcDots'], reason=r['why'])
            continue
        name = key
        w, h = r['dots']
        kind = {'cut': '足元と皿を切った', 'keep': 'そのまま' if not r.get('ops') else 'そのまま (苔・草だけ切った)',
                'derived': '約束の名前のために別の絵から作った'}[r['group']]
        e = dict(art=ARTP + 'relief/' + name, kind=kind, group=r['group'], dots=[w, h], unitsAt25=[round(w / 25.0, 2), round(h / 25.0, 2)],
                 from_=r['src'], srcDots=r['srcDots'], note=r['why'], normal=r.get('normal'), palP95Before=r.get('palP95Before'),
                 light=r.get('light'))
        e.update(flip_rule(r.get('light')))
        if r.get('base'):
            e['base'] = r['base']
        if 'emission' in r:
            e['emission'] = r['emission']
            e['emissionFrac'] = r['emissionFrac']
            e['emissionUse'] = EMISSION_UNUSED
            if r.get('glow'):
                e['glow'] = r['glow']
        if r.get('ops'):
            e['ops'] = r['ops']
        relief[name] = e
    litter = {name: dict(art=ARTP + 'litter/' + name, dots=d, flipOk=False,
                         flipNote='光が左上から描いてある (左上が明るく右下に影)。左右反転すると光が右上からになる＝反転して置かない (向きの違いは 4 種の形と yaw で出す)',
                         note={
        'rubble': '瓦礫 (角ばった石の欠片 3〜5 個の不規則な塊)', 'chip': '欠片 (薄く平たい石・骨白の縁)',
        'gravel': '砂利 (小さな石の粒 3〜6 個のまばらな塊。C3 の gen_act3.py が読む名前)',
        'pebble': '小石 1 個 (左上が明るく右下に影)'}[name.split('_')[0]]) for name, d in lit.items()}

    def fix(o):
        if isinstance(o, dict):
            return {('from' if k == 'from_' else k): fix(v) for k, v in o.items()}
        if isinstance(o, (list, tuple)):
            return [fix(v) for v in o]
        if isinstance(o, (np.integer,)):
            return int(o)
        if isinstance(o, (np.floating,)):
            return float(o)
        return o
    idx = {
        '_': 'HD-2D 段2 の幕3「埋もれた古代都市」の箱庭の絵の一覧 (2026-10-03 レーン D3 が書く。作り方は docs/pixellab/hd2d-act3/README.md・'
             '作り直しは python3 -B docs/pixellab/hd2d-act3/gen_items.py all)。art は Resources からのパス (拡張子なし)。'
             'タイルは calm 済み＝読み込み側で明暗を絞り直さない (DioramaTileCandidate の calm=1・sat=1)。'
             '色はどれも docs/pixellab/hd2d-act3/palette-act3.json へ写してある (半立体と小札も)。'
             '石積み (side_wall・side_step・side_pillar・side_brick) は段の高さの並びが 4 種で同じ (横の目地が隣のタイルとそろう)＝回し方は flipX か none (rot4 にしない)。'
             '4 種の左右の端の 12 列は種 a と同じ＝どの種をどの向きで隣に並べても継ぎ目が出ない (materials.*.seams)。'
             '半立体の glow = 光る所の中心 (見えている絵の下端から・unit。暈と lights の高さはこれを使う)・flipOk = 左右反転して置いてよいか (光の向きに左右が入る絵は false)。'
             '半立体の発光 emission (_e) は箱庭の道がまだ読まない (各項目の emissionUse)。'
             '幕1・幕2 の Art/stage/act1・act2・今の舞台の Art/tiles・Art/props は読むだけで変えていない',
        'tileSize': 64,
        'texelsPerUnit': 25,
        'palette': 'docs/pixellab/hd2d-act3/palette-act3.json',
        'materials': mats,
        'relief': relief,
        'litter': litter,
        'held': held,
        'discarded': discarded,
    }
    os.makedirs(OUT, exist_ok=True)
    json.dump(fix(idx), open(os.path.join(OUT, 'index.json'), 'w'), ensure_ascii=False, indent=1)
    flips = [n for n, r in rec.items() if r.get('flipped')]
    lfr = [n for n, r in rec.items() if r['group'] not in ('held', 'discard') and r.get('light') in ('右上', '右', '右下')]
    json.dump({'_': 'HD-2D 段2 レーン D3 (2026-10-03)。keyflip = 画像ファイルを左右反転した絵 (StageUnitLit の _KeyFlip=1)・lightFromRight = 光が右から描き込まれた絵 '
                    '(art-lint の lint_character で測った向き。反転はしていない)。幕3 の舞台の絵は左右反転して置いた物が無いので keyflip は空。'
                    'キャラの絵の表は幕1 の Art/stage/act1/keyflip がどの幕でも読まれる',
               'keyflip': flips, 'lightFromRight': lfr}, open(os.path.join(OUT, 'keyflip.json'), 'w'), ensure_ascii=False, indent=1)


# ================================================================ 8. 点検
def lint(pal=None):
    if pal is None:
        pal = SP.load_palette(PAL_JSON)
    rows = []
    import glob
    for p in sorted(glob.glob(os.path.join(OUT, '**', '*.png'), recursive=True)):
        rel = os.path.relpath(p, OUT)
        a = load_rgba(p)
        base = os.path.splitext(os.path.basename(p))[0]
        gen = base.endswith('_n') or base.endswith('_e')
        semi = int(((a[..., 3] > 0) & (a[..., 3] < 255)).sum())
        area = int(a.shape[0] * a.shape[1])
        opq = float((a[..., 3] >= 128).mean())
        d = SP.palette_distance(a, pal) if not gen else np.zeros(0)
        p95 = round(float(np.percentile(d, 95)), 2) if len(d) else None
        r = dict(file=rel.replace(os.sep, '/'), w=int(a.shape[1]), h=int(a.shape[0]), area=area, opaque=round(opq, 3), semi=semi, palP95=p95)
        ok = semi == 0 and area <= 30000 and (gen or p95 == 0.0)
        if rel.startswith('litter'):
            ok &= a.shape[0] <= 12
            r['litterH'] = int(a.shape[0])
        if rel.startswith('tiles'):
            ok &= a.shape[:2] == (64, 64)
            c = TC.check(a)
            r['lapvar'] = c['lapvar']
            h, s, v = hsv(a)
            r['satMedian'] = round(float(np.median(s)), 3)
            r['lumaMean'] = round(float(luma(a[..., :3]).mean()), 1)
        if rel.startswith('relief') and not gen:
            try:
                r['light'] = AL.lint_character(p).get('light')
            except Exception:
                r['light'] = None
        r['ok'] = bool(ok)
        rows.append(r)
    os.makedirs(WORK_DOCS, exist_ok=True)
    json.dump(rows, open(os.path.join(WORK_DOCS, 'lint.json'), 'w'), ensure_ascii=False, indent=1)
    L = ['# 幕3 の絵の点検 (gen_items.py lint が書く)', '',
         '門: 半透明 0 (α は 0 か 255)・面積 30,000 以下・色表からの距離 p95 = 0 (法線 _n と発光 _e は色でないので測らない)・'
         '小札は背丈 12 以下・タイルは 64×64。ざらつき = ラプラシアン分散 (tile-calm の check と同じ・石積みは目地の段差を含む全体の値。'
         '石の面だけの値は index.json の faceLapvar)。光 = art-lint の lint_character の描き込まれた光の向き。', '',
         '| 絵 | 大きさ | 面積 | 不透明 | 半透明 | 色表 p95 | ざらつき | 彩度の中央値 | 明るさ | 光 | 判定 |', '|---|---|---|---|---|---|---|---|---|---|---|']
    for r in rows:
        L.append(f"| {r['file']} | {r['w']}×{r['h']} | {r['area']} | {r['opaque']} | {r['semi']} | {r['palP95'] if r['palP95'] is not None else '—'} | "
                 f"{r.get('lapvar', '—')} | {r.get('satMedian', '—')} | {r.get('lumaMean', '—')} | {r.get('light', '—') or '—'} | {'OK' if r['ok'] else 'NG'} |")
    ng = [r['file'] for r in rows if not r['ok']]
    L += ['', f'合計 {len(rows)} 枚・NG {len(ng)}' + (': ' + ', '.join(ng) if ng else '')]
    # 材質ごと: 別の種どうし (回し方 flipX なら反転も) を並べた時の継ぎ目 (2026-10-03 直しの番・反証の指摘 1・2)
    seams = material_seams()
    L += ['', '## 別の種どうしの継ぎ目 (材質ごと・gen_items.py cross_seams)', '',
          f"門: 左右 = 隣の種の端の列の差 ÷ 中の隣の列の差 ≤ {SEAM_GATE['lr']}（4 種 × 反転の全部の組の最大）・"
          f"上下 = 上の種の行 63 と下の種の行 0 の差の最大 ÷ 同じ種どうしの差 ≤ {SEAM_GATE['tb']}・"
          f"天面は 上下の境の差 ÷ 中の「横に続く目地 → 下の石の面」の段差 ≤ {SEAM_GATE['tbJoint']} と、行 63 以外で目地 (中央値より 10 暗い画素) が横幅の 6 割を超える行が無いこと"
          '（石積みは段が横に通るのが正しいので数えず、境÷中の目地は記録だけ）', '',
          '| 材質 | 左右の比 (最悪の組) | 左右の差 / 中 | 上下の比 (最悪の組) | 上下の差 / 同じ種 | 境 ÷ 中の目地 | 横幅いっぱいの行 (a b c d) | 判定 |',
          '|---|---|---|---|---|---|---|---|']
    for name, c in seams.items():
        L.append(f"| {name} | {c['lrRatio']} ({c['lrWorst']}) | {c['lrMean']} / {c['innerX']} | {c['tbRatio']} ({c['tbWorst']}) | {c['tbMean']} / {c['tbSelf']} | {c['tbJoint']} | "
                 f"{' '.join(str(r) for r in c['fullRows']) if name in GEN_TILES else '—'} | {'OK' if c['pass'] else 'NG'} |")
    sng = [n for n, c in seams.items() if not c['pass']]
    L += ['', '組の書き方: 左の種・右の種 (上の種・下の種) の順。\' は左右反転', '', f'材質 {len(seams)}・NG {len(sng)}' + (': ' + ', '.join(sng) if sng else '')]
    open(os.path.join(WORK_DOCS, 'lint.md'), 'w').write('\n'.join(L) + '\n')
    json.dump(rows + [dict(material=n, **c) for n, c in seams.items()], open(os.path.join(WORK_DOCS, 'lint.json'), 'w'), ensure_ascii=False, indent=1)
    print(f'点検 {len(rows)} 枚・NG {len(ng)}' + (': ' + ', '.join(ng) if ng else '') + f'  材質の継ぎ目 NG {len(sng)}' + (': ' + ', '.join(sng) if sng else ''))
    return rows


# ================================================================ シート
def sheet(out, work=None):
    """全部の絵を夜の地 (#262a3a) に並べる: タイルは 2×2 に敷いて 2 倍、半立体は 3 倍 (元・法線・発光を並べる)、小札は 8 倍"""
    import glob
    items = []
    for name in list(MASON) + list(CALM) + list(GEN_TILES):
        row = []
        for v in 'abcd':
            p = os.path.join(OUT, 'tiles', f'{name}_{v}.png')
            if os.path.exists(p):
                t = Image.open(p).convert('RGBA')
                big = Image.new('RGBA', (128, 128))
                for x0 in (0, 64):
                    for y0 in (0, 64):
                        big.paste(t, (x0, y0))
                row.append(big.resize((256, 256), Image.NEAREST))
        # 4 種を混ぜて敷いた 4×2 (隣のタイルと目地がそろうか)
        mix = Image.new('RGBA', (256, 128))
        rng = np.random.default_rng(5)
        for gx in range(4):
            for gy in range(2):
                v = 'abcd'[int(rng.integers(0, 4))]
                p = os.path.join(OUT, 'tiles', f'{name}_{v}.png')
                if os.path.exists(p):
                    t = Image.open(p).convert('RGBA')
                    if rng.random() < 0.5:
                        t = t.transpose(Image.FLIP_LEFT_RIGHT)
                    mix.paste(t, (gx * 64, gy * 64))
        row.append(mix.resize((512, 256), Image.NEAREST))
        items.append(('tiles/' + name + '  (a b c d / mixed 4x2)', row))
    for p in sorted(glob.glob(os.path.join(OUT, 'relief', '*.png'))):
        b = os.path.splitext(os.path.basename(p))[0]
        if b.endswith('_n') or b.endswith('_e'):
            continue
        row = []
        src = None
        for n, g, o, w in RELIEF:
            if n == b and g in ('cut', 'keep'):
                src = os.path.join(PROPS, 'act3_' + b + '.png')
            elif n == b and g == 'derived':
                src = os.path.join(PROPS, 'act3_' + o['base'] + '.png')
        if src and os.path.exists(src):
            im = Image.open(src).convert('RGBA')
            row.append(im.resize((im.width * 3, im.height * 3), Image.NEAREST))
        for suf in ('', '_n', '_e'):
            q = os.path.join(OUT, 'relief', b + suf + '.png')
            if os.path.exists(q):
                im = Image.open(q).convert('RGBA')
                if suf == '_e':
                    bg = Image.new('RGBA', im.size, (0, 0, 0, 255))
                    bg.alpha_composite(im)
                    im = bg
                row.append(im.resize((im.width * 3, im.height * 3), Image.NEAREST))
        items.append(('relief/' + b + '  (src / cut / _n / _e)', row))
    lrow = []
    for p in sorted(glob.glob(os.path.join(OUT, 'litter', '*.png'))):
        im = Image.open(p).convert('RGBA')
        lrow.append(im.resize((im.width * 8, im.height * 8), Image.NEAREST))
    items.append(('litter (x8)', lrow))
    pal = SP.load_palette(PAL_JSON)
    prow = Image.new('RGBA', (len(pal) * 24, 24))
    dr0 = ImageDraw.Draw(prow)
    for i, c in enumerate(pal):
        dr0.rectangle([i * 24, 0, i * 24 + 23, 23], fill=tuple(int(v) for v in c) + (255,))
    items.append(('palette-act3 (%d)' % len(pal), [prow]))
    Wmax = 2600
    x = y = 10
    rowh = 0
    pos = []
    for label, ims in items:
        w = sum(i.width for i in ims) + 12 * max(0, len(ims) - 1)
        if x + w > Wmax and x > 10:
            x = 10
            y += rowh + 30
            rowh = 0
        pos.append((x, y))
        x += w + 28
        rowh = max(rowh, max([i.height for i in ims] + [10]))
    sh = Image.new('RGBA', (Wmax, y + rowh + 40), (38, 42, 58, 255))
    dr = ImageDraw.Draw(sh)
    for (label, ims), (px, py) in zip(items, pos):
        dr.text((px, py), label, fill=(230, 226, 210, 255))
        cx = px
        for im in ims:
            sh.alpha_composite(im, (cx, py + 16))
            cx += im.width + 12
    os.makedirs(os.path.dirname(out) or '.', exist_ok=True)
    sh.convert('RGB').save(out)
    print('シート →', out)


# ================================================================ まとめて
WORK_MARK = '.hd2d-act3-work'          # 作業場の目印 (all が作った作業場だけを消す)
WORK_SUBDIRS = {'calm', 'relief'}       # all が作業場に作る物


def prepare_work(work):
    """作業場を空にして作る。消してよいのは「all が作った作業場」だけ: リポジトリの中・リポジトリを含むフォルダは止める。
    中身があるなら、目印 WORK_MARK があるか、中身が calm/・relief/ だけの時だけ消す"""
    work = os.path.abspath(work)
    if os.path.commonpath([work, REPO]) in (work, REPO):
        sys.exit(f'作業場 {work} はリポジトリの中かリポジトリを含むので使えない (リポジトリの外を --work に渡す。既定は一時フォルダ)')
    if os.path.exists(work):
        if not os.path.isdir(work):
            sys.exit(f'作業場 {work} はフォルダでない')
        names = set(os.listdir(work))
        if names and WORK_MARK not in names and not names <= WORK_SUBDIRS:
            sys.exit(f'作業場 {work} に all が作っていない物がある ({", ".join(sorted(names)[:5])}…)。消さずに止める (空のフォルダか新しい名前を --work に渡す)')
        shutil.rmtree(work)
    os.makedirs(work)
    open(os.path.join(work, WORK_MARK), 'w').write('gen_items.py all の作業場 (毎回消して作り直す)\n')
    return work


def run_all(work, sheet_out=None):
    work = prepare_work(work)
    print('== 1. タイル (石積みの calm・天面の calm)')
    tile_res = make_tiles(work)
    print('== 2. 半立体 (足元と皿を切る・約束の名前の 3 枚)')
    rec = make_relief(work)
    print('== 3. 色表')
    pal = build_palette(work, rec)
    print('== 4. 色表へ写す')
    if os.path.exists(OUT):     # 前の出力を消してから書く (名前を変えた絵が残らないように)
        shutil.rmtree(OUT)
    before = map_all(work, pal)
    for k in tile_res:
        tile_res[k]['palP95Before'] = before.get(k)
    for k, r in rec.items():
        if k in before:
            r['palP95Before'] = before[k]
    for k in tile_res:          # 写した後の値で点検し直す
        a = load_rgba(os.path.join(OUT, 'tiles', k + '.png'))
        mat = k.rsplit('_', 1)[0]
        if mat in MASON:
            c = mason_verdict(a, MASKS[k], MASON[mat]['gate'])
        else:
            c = TC.verdict(TC.check(a), CALM[mat]['maxlap'])
        for kk in ('lapvar', 'edgeMeanDiffLR', 'edgeMeanDiffTB', 'seamRatioX', 'seamRatioY', 'lumaMean', 'pass', 'fail', 'faceLapvar'):
            if kk in c:
                tile_res[k][kk] = c[kk]
        print(f'{k}: {"OK" if c["pass"] else "NG " + ",".join(c["fail"])}  (色表へ写した後) ざらつき {c["lapvar"]}' +
              (f'  石の面 {c["faceLapvar"]}' if 'faceLapvar' in c else '') + f'  明るさ {c["lumaMean"]}')
    print('== 1c. 座席の大石板 (コード生成)')
    gen_res = make_gen_tiles(pal)
    for k, c in gen_res.items():
        a = load_rgba(os.path.join(OUT, 'tiles', k + '.png'))
        c['screenFineness'] = round(screen_fineness(a), 1)
        c['screenFinenessLit'] = round(screen_fineness(a, mean_to=luma(a[..., :3]).mean() * 1.35), 1)
        c['satMedian'] = round(float(np.median(hsv(a)[1])), 3)
        print(f"{k}: {'OK' if c['pass'] else 'NG ' + ','.join(c['fail'])}  lapvar {c['lapvar']}  明るさ {c['lumaMean']}  "
              f"画面の細かさ {c['screenFineness']} (光溜まりで {c['screenFinenessLit']})  彩度の中央値 {c['satMedian']}")
    print('== 1d. 別の種どうしの継ぎ目 (左右・上下・横幅いっぱいの目地の行)')
    seams = material_seams()
    print('== 5. 小札')
    lit = make_litter(pal)
    print('== 6. 法線と発光')
    make_normals_emission(rec)
    for name, r in rec.items():
        if r['group'] in ('held', 'discard'):
            continue
        try:
            r['light'] = AL.lint_character(os.path.join(OUT, 'relief', name + '.png')).get('light')
        except Exception:
            r['light'] = None
    print('== 7. 一覧')
    write_index(tile_res, gen_res, rec, lit, seams)
    print('== 8. 点検')
    lint(pal)
    os.makedirs(WORK_DOCS, exist_ok=True)
    json.dump({k: v for k, v in list(tile_res.items()) + list(gen_res.items())}, open(os.path.join(WORK_DOCS, 'tiles.json'), 'w'),
              ensure_ascii=False, indent=1, default=lambda o: o.item() if hasattr(o, 'item') else str(o))
    if sheet_out:
        sheet(sheet_out, work)


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest='cmd', required=True)
    a = sub.add_parser('all')
    a.add_argument('--work', default=os.path.join(tempfile.gettempdir(), 'hd2d-act3-work'), help='下書きの作業場 (リポジトリの外。毎回消して作り直す)')
    a.add_argument('--sheet')
    sub.add_parser('lint')
    s = sub.add_parser('sheet')
    s.add_argument('--out', required=True)
    h = sub.add_parser('honke')
    h.add_argument('image')
    args = ap.parse_args()
    if args.cmd == 'all':
        run_all(os.path.abspath(args.work), args.sheet)
    elif args.cmd == 'lint':
        lint()
    elif args.cmd == 'sheet':
        sheet(args.out)
    elif args.cmd == 'honke':
        honke_measure(args.image)


if __name__ == '__main__':
    main()

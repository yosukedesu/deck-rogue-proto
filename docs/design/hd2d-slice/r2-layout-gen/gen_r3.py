#!/usr/bin/env python3
"""レーン C 三周目 段1: 三周目の設計図 act1_layout.json を、二周目の最終の写し act1_layout_r2.json から焼く
(計画 docs/design/hd2d-round3-plan-2026-10-01.md §0・§2 C・分析 docs/design/hd2d-r3-analysis-2026-10-01.md §7 R1・R2・R4・R6・R8・§11-11)。

gen_layout.py --r3 がこれを呼ぶ (直接 python3 gen_r3.py でも同じ)。乱数は固定の種 SEED_R3 (決定的)。
配置の判断 (窓・意図の札・太い幹・額縁の L8) は、取り決め 1 の新しい絵 (レーン D) があればその不透明の画素で、無ければ取り決めの大きさの
「代わりの影絵」(place.r3_proxy) で数える = 同じ絵なら同じ物を焼く。焼いた時の絵の指紋 (md5) をログの1行目に出す。D が絵を描き直したら焼き直す
(place.R3_FORCE_PROXY = True にすれば絵の有無に依らず代わりの影絵で焼く)。絵の名前の候補列の後ろには今ある絵を「無ければこれ」として必ず置く (D が遅れても組める)。

やること (値の出どころ):
  R1 森: 帯の木 11 (細い w20 を主に・w32 は手前の1本と 21:9 の余白・w48 は帯に置かない。暗く見える幹は霧 R 0.46〜0.55 を x 600〜800・
     ≈1075・1690〜1765 に。帯の頂点の列 x 880〜1060 は霞んだ1本だけ)・敵の側の霞んだ密な針葉樹 3 (霧 R 0.83〜0.88・幹 11〜12 ドット)・
     奥の木 9 (s 14〜25)・最奥の列 3・敵の側の細い幹 4 (霧 F45 ≥0.72)・近い幹 2 (画面の上を突き抜ける・左右の端)。針葉の枝 11 と角の垂れ葉 4 は廃止。
     主人公の後ろの窓・1〜2体の足元 ±130px の太い幹・意図の札の箱 (霧 F45 <0.7 の暗い物) を避ける。段1b: 霞んだ帯の木は意図の札
     (夜色の不透明の札) の後ろを通ってよい = place.r3_hazy_tree_ok (統合の裁定待ち)
  R2 上の覆い (段1b): 近い木の段 = 密な針葉樹 conifer_dense を近い幹 (左右) と帯の手前の木 band-tree-2 の幹のすぐ奥に置き、いちばん下の段が
     行 214〜272 に来る高さへ持ち上げる (abs。幹は手前の幹の後ろに隠れる)。垂れる枝は霧 R 0.77〜0.85 の高い枝 2 (画面で幅 305〜320px)。
     x1200〜1550 の真上 (行 0〜140) は空ける。moon-shaft-2/3 を廃止 (moon-shaft-1 は gain 0.385 で二周目の明るさのまま)
  段2 C3 (2026-10-02): 帯の真ん中の幹を細く (band-tree-2 w20・far-tree-1〜4 w20 で帯の木の幹の後ろ)・座席の帯の段の前の縁 s −5.0 (土の縁 = 空き地のノイズの縁)・
     座席の帯の前の縁の株 52・茂みの l は段 a・b で m 2 つ・前の段の天面 −0.01 (継ぎ目の段を消す)・株の足元の通りに 21:9 も (下の各節の「段2 C3」の注)
  R4 地面と土手: 茂みの塊 3 段 (s 5.1〜6.0／7.8〜8.9／9.3〜10.6・低く途切れ途切れ・l は 3 個まで・2 段目は 1 段目の真後ろに置かない・
     tint BUSH_TINT = 直しの輪1 で [0.95,0.97,1.0])・土手の株 (茂みの間)・羊歯の一列 (bank・bank-top) を廃止・段1 の chamfer 0.6→0.2・
     段1 の前の縁の株 40・座席の帯の株 40 (真ん中 ±200px は土・全座席の足元の通りを空ける)・座席の帯の前の縁の株 26 (M6)・
     座席の土のタイル top_path_seat_r3_* を候補の先頭へ
  R6 手前: 額縁の草 3→10 (fore_grass_*・深さ 10〜14・scale 0.5〜0.7・tint FORE_TINT = 直しの輪1 で 0.75・vy ≤0.2・名前の帯を避ける・真ん中 3 は onlyWith uitrial)
  直しの輪1 (2026-10-02・反証のまとめ (b) 3・4・5・8): 頂の定数 BUSH_TINT・FORE_TINT・CANOPY_SIDE_TINT・CANOPY_R_PHONE_T・FRONT_STEP_TINT と
     二周目の写しから来る部品の書き換え R2_RETOUCH (茸 2 の tint 0.55・左の羊歯 2 の tint 0.45/0.75・中央下の岩を t −9 へ)
  R8 霧: 加算の霧の面 4 枚を廃止・α合成の霧の板 (kind mist) 2 枚
  揺れ: 房・草・茂み・垂れる枝の "sway"・surfaces.relief.sway・法線 surfaces.relief.normal 0.6
  門: 頂の "gates" (焼いた後の部品の数から)

使い方: gen_r3.py [--out <path>] [--repo]   (既定は一時フォルダの hd2d-r3/layout-cand.json。--repo で unity/Assets/Resources/Stage/act1_layout.json へ)
"""
import argparse
import copy
import json
import math
import os
import random
import sys
import tempfile
from collections import Counter

import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import place  # noqa: E402
from place import Ground, RES  # noqa: E402
from layoutio import dump_layout  # noqa: E402

STAGE = RES + 'Stage/'
SEED_R3 = 20261003
R2_DOC_PREFIX_END = '中身は書き換えない】'   # act1_layout_r2.json の _doc の先頭に足した「写し」の段落の終わり
RA = 'Art/stage/act1/relief/'
NEW = place.R3_NEW_PREFIX   # 三周目で足した部品の名前の頭 'r3-'

PC, PH, PC21, PCU = place.r3_cams()
HERO_WIN = {'PC': (300, 380, 560, 560), 'PH': (230, 230, 430, 380)}
HERO_WIN_PAD = 18          # 窓の左右の余白 (px)。幹・茂み・株は窓の x の外に置く
BOSS160 = place.R3_BOSS160


# ------------------------------------------------------------------ 道具

def solve_t(G, cam, s, x_target, y_rel=0.0, lo=-60.0, hi=60.0):
    """画面の x (その s の地面 + y_rel の高さ) が x_target になる道の t"""
    for _ in range(70):
        m = (lo + hi) / 2
        px = cam.project(G.on_path(m, s, G.gy(m, s) + y_rel))[0]
        if px < x_target:
            lo = m
        else:
            hi = m
    return round((lo + hi) / 2, 3)


def solve_t_abs(G, cam, s, y_abs, x_target, lo=-60.0, hi=60.0):
    for _ in range(70):
        m = (lo + hi) / 2
        px = cam.project(G.on_path(m, s, y_abs))[0]
        if px < x_target:
            lo = m
        else:
            hi = m
    return round((lo + hi) / 2, 3)


def solve_y_abs(G, cam, t, s, row_target, lo=-5.0, hi=40.0):
    """その (t, s) の絶対の高さ y で、画面の行が row_target になる y"""
    for _ in range(70):
        m = (lo + hi) / 2
        if cam.project(G.on_path(t, s, m))[1] > row_target:
            lo = m
        else:
            hi = m
    return round((lo + hi) / 2, 3)


class Seeds:
    def __init__(self, base):
        self.n = base

    def __call__(self):
        self.n += 7
        return self.n


def boxes_intersect(a, b):
    """a = (x0,y0,x1,y1)・b = (x,y,w,h)"""
    return a[2] >= b[0] and a[0] <= b[0] + b[2] and a[3] >= b[1] and a[1] <= b[1] + b[3]


def interp_front(slab):
    T = [q[0] for q in slab['front']]
    S = [q[1] for q in slab['front']]
    return lambda t: float(np.interp(t, T, S))


# ------------------------------------------------------------------ 段2 C3 (2026-10-02): 座席の帯の前の縁
# 二周目の座席の帯 (slab seat-band) の前の縁は s ≈−3.52 の直線 (ゆるい山 4 つ)。空き地 (mask clearing) の楕円 + ノイズの前の縁は
# 敵の側 (t ≥0 = 画面 x 1100〜1850) で s −3.6〜−4.6 まで出るので、土が段の直線の縁で切られ「まっすぐな硬い線」になっていた (統合の所見・M6 12)。
# 段の前の縁を s −5.0 (ゆるい波 ±0.18) へ出し、土と草の境目を空き地の値ノイズの縁 (Diorama.ClearingMask) にする。段の継ぎ目 (前の段 front-step との
# 0.06 の段) は草の中へ。継ぎ目で地面の汚し (mottle) の模様が切れないよう、座席の帯にも前の段と同じ mottle (種 536 = front-step の既定の種) を持たせる
SEAT_FRONT_R3 = -5.0
FRONT_STEP_TOP_R3 = -0.01   # 前の段の天面 (二周目 −0.06)。下の build_r3 の注

# ------------------------------------------------------------------ 直しの輪1 (2026-10-02・三周目 反証のまとめ (b) 3・4・5・8)
# 色の倍率 "tint" は Diorama.PartTint が頂点色 (AO) に掛ける (半立体・札・額縁・段 slab の全部。slab は terrain の _VColorAO 1 で効く)。
# 置き場の判断 (窓・意図の札・足元の通り) は不透明の画素と箱で数えるので、tint を変えても配置は変わらない
BUSH_TINT = [0.95, 0.97, 1.0]   # (b)3 茂みの塊 3 段 (旧 [0.7,0.78,0.9] = 霧の帯の下の黒い溝・中央値 72 → 土手 約 95〜105)。(b)2 の紺と同じ輪で入れる
FORE_TINT = 0.75                # (b)4 額縁の草 r3-fore-1〜10 (旧 0.5 = 明るい地面の上の墨の爪・輝度 1〜10 → 暗い草の影絵 18〜25)
CANOPY_SIDE_TINT = 0.92         # (b)5 上の覆い canopy-l・canopy-r (旧 0.75 = 4〜6px の段の縁の切り絵。canopy-c は 1.0 のまま)
CANOPY_R_PHONE_T = 15.5         # (b)8 canopy-r をスマホでも組む (旧 phone.hide。近い幹 (右) はスマホで外したまま = 幹は band-tree-6・7 の幹の前)
FRONT_STEP_TINT = None   # 直しの輪2 (統合 2026-10-02): 手前の地面を暗くすると草の影絵が溶け (手前の縁 141→11)・暗い画素も 0.70 に。外す           # (b)4 前の段 front-step (手前の地面 p90 58 → 約 38)。暗い画素 G3 > 0.66 になったらこれを最初に外す (None で書かない)
# 二周目の写しから来る部品 (gen_layout の二周目の焼き = act1_layout_r2.json の中身) の書き換え。(種類, 名前, 絵, t, s) で1つに決まる物だけ。
# 'tint' = 色の倍率を書く・'t' = 道の t を動かす。焼き直しで消えないよう、写しでなくここで掛ける (写し act1_layout_r2.json は二周目の画のまま)
R2_RETOUCH = (
    (('card', None, 'shroom2', 4.12, 8.941), {'tint': 0.55}),       # (b)3 霧の足元の青白い塊 (輝度 150〜170)
    (('card', None, 'shroom', 5.508, 11.092), {'tint': 0.55}),      # (b)3 同上
    (('relief', 'wide-edge', 'fern2', -10.141, -4.6), {'tint': 0.45}),   # (b)4 左端の白く光る羊歯 (p95 167・最大 208 → 約 70〜80)
    (('relief', 'grove-foot', 'fern', -7.478, 3.35), {'tint': 0.75}),    # (b)4 同じ左の羊歯の奥の 1 本
    (('rock', None, None, -3.298, -7.6), {'t': -9.0}),              # (b)4 中央下の黒い岩の染み → 左へ (t −8.471 s −6.4 の岩と 1.3 離れる)
)
FRONT_STEP_MOTTLE_SEED = 0 * 7919 + 13 + 523   # Diorama: 部品の Seed の既定 = 部品の番号×7919+13 (front-step は 0 番)・mottle の種の既定 = Seed+523


def _hash(x, y, seed):
    """DioramaTextures.Hash の写し (32 bit の掛け算の桁あふれ)"""
    h = (x * 374761393 + y * 668265263 + seed * 144269504) & 0xFFFFFFFF
    h = ((h ^ (h >> 13)) * 1274126177) & 0xFFFFFFFF
    h ^= h >> 16
    return (h & 0xFFFFFF) / float(0x1000000)


def _noise(x, y, seed):
    x0, y0 = math.floor(x), math.floor(y)
    fx, fy = x - x0, y - y0
    sx, sy = fx * fx * (3 - 2 * fx), fy * fy * (3 - 2 * fy)
    a, b = _hash(x0, y0, seed), _hash(x0 + 1, y0, seed)
    c, d = _hash(x0, y0 + 1, seed), _hash(x0 + 1, y0 + 1, seed)
    return (a + (b - a) * sx) + ((c + (d - c) * sx) - (a + (b - a) * sx)) * sy


def _fbm(x, y, seed):
    return _noise(x, y, seed) * 0.65 + _noise(x * 2.03 + 17.1, y * 2.03 - 5.3, seed + 101) * 0.35


def clearing_v(sb, t, s):
    """Diorama.ClearingMask の楕円の値 v (踏み跡は除く)。v < maskEdge の真ん中 (0.98) なら土"""
    c, rr = sb['maskCenter'], sb['maskRadius']
    e = ((t - c[0]) / rr[0]) ** 2 + ((s - c[1]) / rr[1]) ** 2
    return e + (_fbm(t * 0.35, s * 0.35, sb.get('seed', 0)) - 0.5) * 0.9


def dirt_front_s(sb, t, s_lo=SEAT_FRONT_R3, s_hi=-0.3, step=0.02):
    """その t の土の前の縁 (手前から奥へ進んで最初に v ≤0.98 になる s)。土が無ければ None"""
    mid = 0.5 * (sb['maskEdge'][0] + sb['maskEdge'][1])
    s = s_lo
    while s <= s_hi:
        if clearing_v(sb, t, s) <= mid:
            return round(s, 3)
        s += step
    return None


# ------------------------------------------------------------------ sources (取り決め 1 の名前・候補列の後ろに今ある絵)

def add_sources(L):
    S = L['sources']
    doc_tree = ('三周目 R1 (レーン D のコード生成・取り決め 1): 幹＋枝＋房が1枚の針葉樹。数字は根元の幹の幅 (ドット)・高さ 280 ドット (11.2 unit)・'
                '足元＝絵の下辺の中央。幹は影絵＋左の縁の明るい筋 2〜3 ドット幅。房は不透明 0.7 以上。無ければ二周目の幹 (trunk_thin・trunk_long)')
    pairs = (('conifer20a', 'conifer_w20_1', 'trunk_thin_1'), ('conifer20b', 'conifer_w20_2', 'trunk_thin_2'),
             ('conifer32a', 'conifer_w32_1', 'trunk_long_2'), ('conifer32b', 'conifer_w32_2', 'trunk_long_1'),
             ('conifer48a', 'conifer_w48_1', 'trunk_long_3'), ('conifer48b', 'conifer_w48_2', 'trunk_long_3'))
    for k, (name, art, fb) in enumerate(pairs):
        S[name] = {'art': [RA + art, RA + fb], 'depth': 0.08, 'cells': 48}
        if k == 0:
            S[name] = {'_doc': doc_tree, **S[name]}
    doc_dense = ('段1b (レーン D2 2026-10-02): 密な針葉樹 (段 16〜23 枚の重なり・三角の影絵・下の縁はのこぎり・上の縁だけ明るい・葉は高さの約 10% から上)。'
                 '1・2 = 150×300 (幹 12・11 ドット)、3・4 = 200×340 (幹 15・16)。足元 = 幹の中心。光は左上に描き込み = 反転しない。無ければ二周目の長い幹')
    for i, fb in ((1, 'trunk_long_2'), (2, 'trunk_long_1'), (3, 'trunk_long_3'), (4, 'trunk_long_3')):
        d = {'art': [RA + 'conifer_dense_%d' % i, RA + fb], 'depth': 0.08, 'cells': 48}
        if i == 1:
            d = {'_doc': doc_dense, **d}
        S['dense%d' % i] = d
    S['nearTrunk1'] = {'_doc': '三周目 R1・R2: 近い幹 (画面の上を突き抜ける・左右の端。幹の幅 36・42・高さ 320 ドット・枝の付け根の短い残り)。無ければ二周目の長い幹',
                       'art': [RA + 'conifer_near_1', RA + 'trunk_long_1'], 'depth': 0.1, 'cells': 48}
    S['nearTrunk2'] = {'art': [RA + 'conifer_near_2', RA + 'trunk_long_3'], 'depth': 0.1, 'cells': 48}
    for i in range(1, 5):
        d = {'art': [RA + 'bough_hang_%d' % i, RA + 'needle_bough_%d' % i], 'depth': 0.05, 'cells': 40}
        if i == 1:
            d = {'_doc': '三周目 R2: 垂れる枝 (160×80・付け根は絵の左の辺 = swayFrom "left"・右と下へ垂れて先細り・房 12〜30 ドット・不透明 0.7 以上)。'
                         '反対の側は flip。無ければ二周目の針葉の枝', **d}
        S['boughHang%d' % i] = d
    for sz, fbs in (('l', ('act1_bush2', 'bush_1')), ('m', ('bush_1', 'act1_leafclump1')), ('s', ('act1_leafclump4', 'act1_leafclump1'))):
        for i in (1, 2):
            d = {'art': [RA + 'bush_clump_%s_%d' % (sz, i), RA + fbs[i - 1]], 'depth': 0.14, 'cells': 40}
            if sz == 'l' and i == 1:
                d = {'_doc': '三周目 R4: 茂みの塊 (l 96×64・m 64×48・s 48×32 ドット・夜の色・内部の明暗は低く・縁はぎざぎざ・底は暗い)。'
                             '段1 の垂直面と面取りを 3 段で隠す。無ければ今の茂み・葉の塊 (昼の色 = 部品の tint [0.8,0.9,1.0] が保険)', **d}
            S['bush%s%d' % (sz.upper(), i)] = d
    for i in range(1, 7):
        fb = 'act1_tallgrass1' if i % 2 == 1 else 'act1_fern2'
        d = {'art': [RA + 'fore_grass_%d' % i, RA + fb], 'depth': 0.06, 'cells': 32}
        if i == 1:
            d = {'_doc': '三周目 R6: 手前の草の帯 (額縁の影絵・穂の間隔 ≥40px。大きさはレーン D)。無ければ背の高い草・羊歯', **d}
        S['foreGrass%d' % i] = d
    # 座席の土のタイル (取り決め 1): 候補の先頭に r3、後ろに r2 (今の先頭)
    for v, lst in zip('abcd', L['tiles']['pathSeat']['variants']):
        name = 'Art/stage/act1/tiles/top_path_seat_r3_%s' % v
        if name not in lst:
            lst.insert(0, name)


# ------------------------------------------------------------------ 本体

def art_fingerprint():
    """取り決め 1 の新しい絵の有無と中身の指紋 (焼き直しが要るかを見分ける)"""
    import hashlib
    h = hashlib.md5()
    have = 0
    for a in sorted(place.R3_ART):
        fp = RES + a + '.png'
        if os.path.exists(fp):
            have += 1
            h.update(a.encode())
            h.update(open(fp, 'rb').read())
    return '%d/%d 枚あり・md5 %s' % (have, len(place.R3_ART), h.hexdigest()[:12])


def load_r2():
    L = json.load(open(STAGE + 'act1_layout_r2.json', encoding='utf-8'))
    doc = L['_doc']
    i = doc.find(R2_DOC_PREFIX_END)
    if i >= 0:
        doc = doc[i + len(R2_DOC_PREFIX_END):]
    L['_doc'] = doc
    return L


def build_r3(L):
    place.R3_FORCE_PROXY = False   # レーン D の絵があればその形で判断する (無ければ代わりの影絵)。D が描き直したら焼き直す
    place._ART.clear()
    log = []
    log.append('判断に使った新しい絵: ' + art_fingerprint())
    seed = Seeds(SEED_R3)
    parts = L['parts']
    add_sources(L)
    S = L['sources']

    # ---- 面: 法線と揺れ (取り決め 2)
    rel = L['surfaces']['relief']
    rel['normal'] = 0.6
    rel['sway'] = {'amp': 0.04, 'freq': [0.35, 0.9]}

    # ---- 段1 の面取り (R4)
    for p in parts:
        if p['kind'] == 'slab' and p['name'] == 'terrace-1':
            assert p['chamfer'] == 0.6
            p['chamfer'] = 0.2
    # ---- 段2 C3: 座席の帯の前の縁を s −5.0 へ (土の前の縁 = 空き地のノイズの縁。上の SEAT_FRONT_R3 の注)
    fs = [p for p in parts if p['kind'] == 'slab' and p['name'] == 'front-step'][0]
    assert parts.index(fs) == 0 and 'seed' not in fs and fs['back'] == -3.2 and fs['top'] == -0.06
    fs['mottle'] = dict(fs['mottle'], seed=FRONT_STEP_MOTTLE_SEED)   # 既定の種と同じ値を書くだけ (模様は変わらない)
    # 継ぎ目の段 0.06 → 0.01: 段の高さの差がそのまま縁石 (座席の帯の面取りの 45° の帯の上 0.06 = 焦点で約 6px の明るい横線) になる
    # (試しの撮影 2 回目の土の前の縁の明るい筋はこれ)。前の段の天面を −0.01 へ上げ、座席の帯の前の縁 (s −5.0) の段を 1px 未満にする。
    # 前の段は座席の帯の下 (s −5.18〜−3.2) と重なるが 0.01 の差 (深度の分解能 ≈2e-4 の 50 倍)。座席 (s −2.6〜2.8) の高さは変わらない
    fs['top'] = FRONT_STEP_TOP_R3
    if FRONT_STEP_TINT is not None:
        fs['tint'] = FRONT_STEP_TINT   # 直しの輪1 (b)4: 手前の地面を沈める (Diorama: slab も部品の tint を頂点色に掛ける)
    for p in parts:
        if p['kind'] == 'slab' and p['name'] == 'seat-band':
            assert min(q[1] for q in p['front']) >= -3.85 and 'mottle' not in p
            p['front'] = [[t, round(SEAT_FRONT_R3 + 0.12 * math.sin(t * 0.83 + 0.4) + 0.06 * math.sin(t * 2.1 + 1.3), 3)] for t, _ in p['front']]
            p['mottle'] = {'scale': [2, 5], 'amount': 0.1, 'seed': FRONT_STEP_MOTTLE_SEED}
            fr_ = interp_front(p)
            vmin = min(clearing_v(p, t, fr_(t) + ds) for t in np.arange(-34.0, 37.7, 0.16) for ds in (0.0, 0.16, 0.32))
            assert vmin >= p['maskEdge'][1], '土が段の前の縁に届く (v %.3f)' % vmin   # 前の縁から 1 格子の奥まで苔 (継ぎ目は草の中)
    G = Ground(L)
    t1 = [p for p in parts if p['kind'] == 'slab' and p['name'] == 'terrace-1'][0]
    f1 = interp_front(t1)

    # ---- 外す
    def drop(pred, why):
        nonlocal parts
        keep, gone = [], []
        for p in parts:
            (gone if pred(p) else keep).append(p)
        parts = keep
        log.append('外す %-34s %3d  %s' % (why, len(gone), ', '.join(sorted({(q.get('name') or q.get('src') or q['kind']) for q in gone}))[:150]))
        return gone
    nm = lambda p: p.get('name') or ''
    drop(lambda p: nm(p) == 'bough', 'R1 針葉の枝 (枝は木に付く)')
    drop(lambda p: nm(p) in ('crown-left', 'crown-right', 'crown-left-wide', 'crown-right-wide'), 'R1 角の丸い垂れ葉')
    drop(lambda p: nm(p) in ('band-trunk', 'far-trunk'), 'R1 二周目の霧の帯の幹・奥の幹 (帯の木・奥の木へ)')
    old_near = drop(lambda p: nm(p) in ('grove-trunk-1', 'edge-trunk-r'), 'R1 左右の長い幹 (近い幹へ)')
    drop(lambda p: nm(p) in ('bank', 'bank-top'), 'R4 羊歯の一列 (土手の草・小さな羊歯)')
    drop(lambda p: p['kind'] == 'fog', 'R8 加算の霧の面 4 枚')
    drop(lambda p: p['kind'] == 'shaft' and nm(p) in ('moon-shaft-2', 'moon-shaft-3'), 'R2 右上の月光の筋 2 本')
    drop(lambda p: p['kind'] == 'frame' and nm(p) in ('frame-low-left', 'frame-low-right'), 'R6 手前の額縁の草 2 (額縁の草 10 へ)')

    # 二周目の「段1 の土手の上の縁の株」(scatter 26 = s が段1 の前の縁 +0.35〜1.3) を段1 の前の縁の株 40 に置き換える
    def old_t1_edge_tuft(p):
        if p['kind'] not in ('card', 'litter') or not (p.get('src') or '').startswith('tuftStand'):
            return False
        t, s = p['t'], p['s']
        if t < -2.5:
            return False
        e = f1(t)
        return e + 0.3 <= s <= e + 1.35
    drop(old_t1_edge_tuft, 'R4 二周目の段1 の土手の上の縁の株')

    # ---- 残す部品の書き換え
    for p in parts:
        if nm(p) == 'grove-trunk-3':
            p['src'] = 'nearTrunk2'           # 21:9 の左端だけに映る長い幹も新しい近い幹の絵へ (二周目の cos の円筒陰影を混ぜない)
            p['flip'] = True
        if p['kind'] == 'shaft' and nm(p) == 'moon-shaft-1':
            # R2 の「moon-shaft-0 の gain 1.1→1.3」はレーン B が look の materials.glow.shaftGain 1.3 で入れた (部品の gain は頂点色 = 0〜1 に丸められ、
            # 1.1 も 1.3 も 1.0 として効く)。shaftGain は筋の全部に掛かるので、主人公の頭上を横切る moon-shaft-1 は二周目の明るさ (0.5) に据え置く = 0.5÷1.3
            assert p['gain'] == 0.5
            p['gain'] = 0.385
        if nm(p) == 'waystone':
            p['phone'] = {'hide': True}       # スマホで 3〜4体の意図の札に 789px 掛かる (r3 の節の検査)
        # 揺れ: 二周目から残る羊歯・茂み・背の高い草・株 (房・草・茂み)
        src = p.get('src') or ''
        if p['kind'] == 'relief' and src in ('fern', 'fern2', 'bush', 'tallgrass', 'fernSmall'):
            p['sway'] = 0.5
        if p['kind'] == 'card' and src.startswith('tuftStand'):
            p['sway'] = 0.4
        if p['kind'] == 'frame' and nm(p) == 'frame-left-mid':
            assert p['vy'] == 0.55 and p['phone']['vy'] == 0.66
            p['vy'] = 0.6                     # 名前の帯 (PC 行 595〜707) に 13% 掛かっていた (二周目から) → 帯の上へ
            p['phone']['vy'] = 0.71           # スマホの帯 (行 380〜496) に 17%
            p['sway'] = 0.6
            p['swayFrom'] = 'left'
    # 直しの輪1: 二周目の写しから来る部品の色と置き場 (上の R2_RETOUCH。どれも 1 つだけ当たること)
    for (kind_, name_, src_, t_, s_), what in R2_RETOUCH:
        hit = [p for p in parts if p['kind'] == kind_ and (name_ is None or p.get('name') == name_) and (src_ is None or p.get('src') == src_)
               and abs(p.get('t', 1e9) - t_) < 1e-3 and abs(p.get('s', 1e9) - s_) < 1e-3]
        assert len(hit) == 1, ('直しの輪1 の書き換えが 1 つに決まらない', kind_, name_, src_, t_, s_, len(hit))
        hit[0].update(what)
        log.append('直しの輪1 %s %s (t %.3f s %.3f) ← %s' % (kind_, name_ or src_ or '', t_, s_, what))

    new = []

    def add(p, why):
        if p['kind'] not in ('frame', 'mist'):
            p.setdefault('seed', seed())
        elif p['kind'] == 'mist':
            p.setdefault('seed', seed())
        new.append((p, why))
        return p

    # ---- 置いた部品を、PC (と R3 の試し撮り PCU) の画で規則の外へ寄せる (絵の形 = 実際の不透明の画素で数える)
    L_tmp = L   # place_part は L の sources だけを読む
    ui = {c: place.r3_shot_ui(c) for c in ('PC', 'PH')}
    intents = {'PC': [r for rec in ui['PC'].values() for r in rec['intents']], 'PH': [r for rec in ui['PH'].values() for r in rec['intents']]}   # 160 のボスの箱は violations が別に数える
    feet12 = sorted({e['x'] for n in (1, 2) for e in place.seats(PC, G)['enemies'][n]})
    tdf = {}
    for cam in (PC, PCU):
        rc_ = G.raycast(cam, 4)
        tdf[cam.name] = place.upsample(rc_, cam.W, cam.H)
    hero_pcu = place.seats(PCU, G)['hero']
    WIN = {'PC': HERO_WIN['PC'], 'PCU': (300, int(hero_pcu[1] - 274), 560, int(hero_pcu[1] - 94))}

    def violations(p, tree):
        """PC と PCU で: 窓 (霧 F45 が 木 0.7・ほか 0.4 未満なら)・意図の札と 160 のボスの箱 (霧 F45 <0.7。段1b: 霞んだ帯の木は
        place.r3_hazy_tree_ok で許す)・太い幹 (1〜2体の足元 ±130px)。画素の数の合計"""
        bad = 0
        for cam in (PC, PCU):
            o = place.place_part(L_tmp, G, cam, 0, p, with_image=True)
            if o is None or o.box is None:
                continue
            vm, npx = place.visible_mask(o, cam, tdf[cam.name], cam.W, cam.H)
            if vm is None:
                continue
            f45 = place.fog_frac(o.depth, 14.0, 45.0, cam.r)
            w0, wy0, w1, wy1 = WIN[cam.name]
            if f45 < (0.7 if tree else 0.4):
                bad += int(vm[max(0, wy0):wy1, max(0, w0 - HERO_WIN_PAD):w1 + HERO_WIN_PAD].sum())
            if cam is PC:
                if f45 < 0.7:
                    boxes_ = ([] if place.r3_hazy_tree_ok(o, cam, False) else list(intents['PC'])) + \
                             ([] if place.r3_hazy_tree_ok(o, cam, True) else [BOSS160])
                    for r in boxes_:
                        x0, y0, ww, hh = [int(round(v)) for v in r]
                        bad += int(vm[max(0, y0):max(0, y0 + hh), max(0, x0):max(0, x0 + ww)].sum())
                if tree:
                    tw = place.r3_trunk_px(o, cam)
                    cx = o.foot[0]
                    if tw >= 40 and any(cx + tw / 2 >= fx - 130 and cx - tw / 2 <= fx + 130 for fx in feet12):
                        bad += 100000
        return bad

    def settle(p, tree=True, max_dx=320, step=16, allow_flip=False):
        """規則に掛かるなら x を ±step px ずつ (近い方から) 動かして、掛からない置き場を探す (段1b: 絵の光は左上に描き込んであるので、
        木と枝は反転しない = allow_flip False)。見つからなければ None (置かない)"""
        if violations(p, tree) == 0:
            return p
        a = p.get('abs')
        x0 = PC.project(G.on_path(p['t'], p['s'], p['y'] if a else G.gy(p['t'], p['s']) + p.get('y', 0.0)))[0]
        for k in range(0, max_dx // step + 1):
            for sign in ((0,) if k == 0 else (-1, 1)):
                for fl in ((p.get('flip', False), not p.get('flip', False)) if allow_flip else (p.get('flip', False),)):
                    q = dict(p)
                    q['flip'] = fl
                    x = x0 + sign * k * step
                    q['t'] = solve_t_abs(G, PC, q['s'], q['y'], x) if a else solve_t(G, PC, q['s'], x)
                    if violations(q, tree) == 0:
                        if k:
                            log.append('寄せる %s x %.0f → %.0f%s' % (p['name'], x0, x, '・反転' if fl != p.get('flip', False) else ''))
                        return q
        log.append('置かない %s (x %.0f の ±%dpx に規則の外の置き場が無い)' % (p['name'], x0, max_dx))
        return None

    def add_settled(p, why, tree=True, max_dx=320, allow_flip=False):
        q = settle(p, tree, max_dx, allow_flip=allow_flip)
        if q is not None:
            add(q, why)
        return q

    def fog_r(p, cam=PC):
        o = place.place_part(L_tmp, G, cam, 0, p, with_image=False)
        return place.fog_frac(o.depth, 14.0, 28.0, cam.r) if o is not None and o.depth else 0.0

    def s_for_fog(x, src, target, lo=-2.0, hi=40.0):
        """幹の中心が画面の x に来る s のうち、板の中心の霧 R が target になる s (霧は奥へ単調)"""
        for _ in range(40):
            m = (lo + hi) / 2
            q = {'kind': 'relief', 'src': src, 't': solve_t(G, PC, m, x), 's': m}
            if fog_r(q) < target:
                lo = m
            else:
                hi = m
        return round((lo + hi) / 2, 2)

    # ================================================================ R1 森 (段1b 2026-10-02: レーン D2 の描き直しの絵で焼き直し・統合の所見 1〜4)
    # 近い幹 2 (左右の端・画面の上を突き抜ける)。左は二周目の grove-trunk-1 の置き場 (幹の右の縁 x≈150)、右は edge-trunk-r の外側。
    # 段1b: 右も反転しない (絵の左の縁の明るい筋 = 光は左上。反転すると明るい側が右へ移る = D2 の申し送り 4)
    gl = old_near[0] if nm(old_near[0]) == 'grove-trunk-1' else old_near[1]
    t = solve_t(G, PC, 3.55, 70)
    pl = {'kind': 'relief', 'name': NEW + 'near-trunk-l', 'src': 'nearTrunk1', 't': t, 's': 3.55, 'yaw': 3, 'flip': False, 'shadow': 1, 'tint': 0.6}
    if gl.get('phone'):
        pl['phone'] = dict(gl['phone'])
    near_l = add_settled(pl, 'R1 近い幹 (左)', max_dx=160, allow_flip=True)   # 近い幹の絵は描き直していない (枝の付け根の残りが右へ長い = 反転で窓を避ける・試しの撮影 T3 と同じ)
    # 段1b: 右の近い幹は s 4.6→3.0 (右は道が奥へ向かうので同じ s でも左より遠い = 霧 R 0.6 で霞んでいた。右上の段 canopy-r をこの幹の奥に置くので手前へ。
    # t ≈16 = 座席の帯 (t ≤13) の外)
    t = solve_t(G, PC, 3.0, 1935)
    near_r = add_settled({'kind': 'relief', 'name': NEW + 'near-trunk-r', 'src': 'nearTrunk2', 't': t, 's': 3.0, 'yaw': -3, 'flip': False, 'shadow': 1, 'tint': 0.6,
                          'phone': {'hide': True}}, 'R1 近い幹 (右・スマホは外す = 二周目の edge-trunk-r と同じ)', max_dx=160, allow_flip=True)

    # ---- 上の覆い = 近い木の段の重なり (所見 1。本家 ref16_upperright の近い密な針葉樹)。密な針葉樹 conifer_dense を近い幹のすぐ奥 (s +0.8〜1.0) の
    #      同じ画面の x に置き、いちばん下の段が行 CANOPY_ROWS に来る高さへ持ち上げる (abs)。密な針葉樹の幹 (15〜16 ドット) は近い幹 (33〜37 ドット) の
    #      後ろに隠れる = 近い木の幹から段が張り出して画面の上を横切る形。立てたままだと葉が高さの 10% から始まり、霧の帯 (芯 ±80) と主人公の後ろの窓・
    #      右の意図の札 (行 283〜403) を三角の段で塞ぐ (帯の暗い幹の筋 T1 が左右で消える)。スマホは近い幹とずれれば外す (下のスマホの節)
    canopies = []

    def place_canopy(name, src, base, ds, rows_try, tint, why):
        if base is None:
            log.append('置かない %s (元の幹が無い)' % name)
            return None
        xb = PC.project(G.on_path(base['t'], base['s'], G.gy(base['t'], base['s'])))[0]
        s_ = round(base['s'] + ds, 2)
        im_, _ = place.art_for(L, src)
        h_units = im_.size[1] / place.TPU
        fol = 0.105   # 葉の下端 = 絵の高さの約 10.5% (D2 の絵の実測 10〜14%)
        for rb in rows_try:
            t = solve_t(G, PC, s_, xb)
            y = 0.0
            for _ in range(5):
                y = solve_y_abs(G, PC, t, s_, rb) - fol * h_units
                t = solve_t_abs(G, PC, s_, y, xb)
            q = {'kind': 'relief', 'name': NEW + name, 'src': src, 't': t, 's': s_, 'y': round(y, 3), 'abs': True, 'yaw': 0, 'flip': False,
                 'shadow': 0, 'tint': tint, 'sway': 0.2}
            if violations(q, True) == 0:
                log.append('上の覆い %s: %s の幹の x %.0f の奥 (s %.2f)・いちばん下の段 行 %d・y %.2f (地面 %.2f)・霧 R %.2f' % (
                    name, base['name'], xb, s_, rb, y, G.gy(t, s_), fog_r(q)))
                add(q, why)
                canopies.append((q, base))
                return q
        log.append('置かない %s (行 %s のどれでも規則に掛かる)' % (name, rows_try))
        return None
    place_canopy('canopy-l', 'dense3', near_l, 1.0, (272, 258, 244, 230, 216, 202), CANOPY_SIDE_TINT, 'R2 上の覆い (近い木の段・持ち上げ)')
    place_canopy('canopy-r', 'dense4', near_r, 0.7, (232, 218, 204, 190, 176, 162), CANOPY_SIDE_TINT, 'R2 上の覆い (近い木の段・持ち上げ)')

    # ---- 帯の木 (s 7〜14・tint なし = 暗さは霧で)。x は幹の中心 (PC)。主人公の後ろの窓 (x300〜560) の外。所見 2:
    #      細い w20 を主に・w32 は手前の1本だけ・w48 は帯に置かない (奥 s 14 より奥と 21:9 の余白だけ)。
    #      暗く見える幹 (霧 R 0.46〜0.6) を 主人公と敵の間 (x 600〜800)・敵の側の左の端 (x ≈1075)・右の端 (x 1690〜1765) に。帯の頂点の列 (x 880〜1060) は
    #      霞んだ 2 本だけ (G1 の歯止め)。帯の木は座席の帯 (s ≤2.8 + 0.3) より奥 (seat_intrusions)。1〜2体の足元 ±130px (x 1137〜1652) は太い幹 (見かけ 40px 以上) を置かない (K11) = 霞んだ密な針葉樹 2 本だけ。
    #      敵の側の帯の木は意図の札 (夜色の不透明の札) の後ろを通る = place.r3_hazy_tree_ok (霧 R ≥0.45。統合の裁定待ち)
    BAND = (  # (名前, x, ('s', s) | ('fog', 霧 R の目標), 絵)
        ('band-tree-1', 228, ('s', 10.2), 'conifer20b'),
        # 段2 C3 (2026-10-02): band-tree-2 は w32 → w20 (霧 R 0.46 → 0.50)。試しの撮影 2 回目で x 620〜700 の太い灰色の柱 (画面で約 90px) だった。
        # w20 の幹 = 根元 20・高さ 30〜40% で 17〜18 ドット → 画面で約 55〜63px (本家の帯の幹 30〜60px)。canopy-c (dense4 の幹 16 ドット) はこの幹の奥 s +1.6 に
        # 隠れる (足の高さ 23〜35% で w20 の幹 18.5〜17.6 ドット・dense4 の幹 16〜14 ドット × 深さの比 0.96 = 片側 3〜5px の余り)
        ('band-tree-2', 612, ('fog', 0.50), 'conifer20b'),
        ('band-tree-3', 772, ('fog', 0.55), 'conifer20a'),
        ('band-tree-4', 866, ('fog', 0.72), 'conifer20b'),
        # 敵の側 (3〜4体の座席の後ろ) は art-bible §2-2「霧に沈めた細い幹」= 霧 R 0.56〜0.6 (統合の所見 2 の 0.55〜0.85 の手前の端)
        # 段2 C3: band-tree-5 (x 1072 = 試しの撮影 2 回目の x 1050 の柱) は霧 R 0.56 → 0.62 (幹 61 → 約 56px・画面の中ほどで約 48px)。すぐ左の band-tree-8 と
        # 奥の far-tree-4 (どちらも x 1000・w32 83/78px) が隙間 15px で並び、霧の変種 (end 40) では 1 本の太い柱 (x 958〜1103) に見えたので、下でそれも離して細くする
        ('band-tree-5', 1072, ('fog', 0.62), 'conifer20a'),
        ('band-tree-6', 1688, ('fog', 0.58), 'conifer20b'),
        ('band-tree-7', 1764, ('fog', 0.60), 'conifer20a'),
        ('band-tree-8', 965, ('fog', 0.84), 'conifer20b'),     # 段2 C3: x 1000 → 965・w32 → w20 (band-tree-5 との隙間 15 → 約 50px)
        ('band-tree-9', -300, ('s', 13.0), 'conifer32a'),       # 21:9 の左の余白
        ('band-tree-10', -130, ('s', 9.4), 'conifer32b'),       # 21:9 の左の余白
        ('band-tree-11', 2092, ('s', 10.6), 'conifer20a'))      # 21:9 の右の余白
    rng = random.Random(SEED_R3 + 1)
    for name, x, (how, val), src in BAND:
        s_ = val if how == 's' else s_for_fog(x, src, val)
        t = solve_t(G, PC, s_, x)
        add_settled({'kind': 'relief', 'name': NEW + name, 'src': src, 't': t, 's': s_, 'yaw': round(rng.uniform(-6, 6), 1), 'flip': False,
                     'shadow': 0, 'sway': 0.3}, 'R1 帯の木', max_dx=200)
    # 段1b: 主人公と敵の間の上 (x 360〜960・行 0〜215) = 帯の手前の木 band-tree-2 (w32・霧 R ≈0.46) の幹の奥に密な針葉樹の段 (canopy-c)。
    # 霧に透けた段の重なり (本家 ot16 の上の真ん中の霞んだ段)。いちばん下の段は霧の帯の頂点の行 (285〜345) より上 = 帯の頂点 G1 と暗い幹の筋 T1 に掛けない
    bt2 = [p for p, _ in new if p.get('name') == NEW + 'band-tree-2']
    place_canopy('canopy-c', 'dense4', bt2[0] if bt2 else None, 1.6, (214, 200, 186, 172), 1.0, 'R2 上の覆い (帯の手前の木の段・持ち上げ)')
    # 敵の側の霞んだ密な針葉樹 3 (霧 R 0.83〜0.88 = 霧の中の影絵。右は霧の地が暗い = 薄くても暗い画素 G3 を足すので 0.83 より手前にしない。幹 11〜12 ドット = 見かけ 30px 未満 = K11 の外。本家 ref16_fogtrunk の霧の中の木)
    for name, x, fr, src in (('enemy-haze-1', 1296, 0.85, 'dense2'), ('enemy-haze-2', 1566, 0.88, 'dense1'), ('enemy-haze-3', 1442, 0.83, 'dense1')):
        s_ = s_for_fog(x, src, fr)
        t = solve_t(G, PC, s_, x)
        add_settled({'kind': 'relief', 'name': NEW + name, 'src': src, 't': t, 's': s_, 'yaw': round(rng.uniform(-4, 4), 1), 'flip': False,
                     'shadow': 0, 'sway': 0.2}, 'R1 敵の側の霞んだ密な針葉樹', max_dx=120)
    # 奥の木 9 (s 14〜25・w32/48)。今の霧 (end 28) ではほぼ消え、変種 (end 40・45) で層になる。意図の札・1〜2体の足元・主人公の後ろの窓の外
    # 段2 C3 (2026-10-02): 帯の木の真後ろの奥の木 (far-tree-1〜4) は、帯の木より太い w32・w48 (78〜118px) だと霧の変種 (end 40) で帯の木の両側に
    # 淡い幹がはみ出し、二重の太い柱になる (x 620〜700・x 1000〜1100)。→ w20 にして x を手前の帯の木の幹の x にそろえる (幹は帯の木の後ろに隠れ、枝だけが奥の層になる)
    def bx(nm_, x_):
        q_ = [p for p, _ in new if p.get('name') == NEW + nm_]
        return PC.project(G.on_path(q_[0]['t'], q_[0]['s'], G.gy(q_[0]['t'], q_[0]['s'])))[0] if q_ else x_
    # 段2 C3 (続き): w20 にすると枝の層も細くなり、上の真ん中の覆い (place.py の topMid = 二周目の N6a) が PC 85→79%・スマホ 64→48% (目安 50% 超) に落ちる。
    # 帯の木の真後ろを密な針葉樹 (幹 11〜16 ドット = 見かけ 27〜42px < 手前の帯の木の幹 52〜63px = 隠れる) にする道も模型で比べた:
    #   4 本とも dense = topMid 86/60%・模型 (霧 R/F40) M2 0.31/0.47・E2 64/105・G1 117/92
    #   1・2 本目だけ   = topMid 82/57%・M2 0.32/0.49・E2 65/112・G1 117/93
    #   全部 w20 (採用) = topMid 79/48%・M2 0.32/0.51・E2 70/122・G1 118/97 (試しの撮影 2 回目の設計図は M2 0.34/0.50・E2 75/127・G1 113/82)
    # 密な針葉樹の塊は帯の中の縁 E2 と模様 M2 を減らす (統合が良いとした霧の変種 end 40 の強み) ので w20 にする。dense を試すなら下の表の 4 つ目に名前を書く
    # (規則に掛かるか幹が手前より太ければ w20 に戻る・どちらも寄せない = 寄せると幹が帯の木の横に出る)
    FAR = ((bx('band-tree-2', 660), 18.5, 'conifer20a', None, 'band-tree-2'), (bx('band-tree-3', 772), 15.0, 'conifer20b', None, 'band-tree-3'),
           (bx('band-tree-4', 874), 22.5, 'conifer20a', None, 'band-tree-4'), (bx('band-tree-8', 965), 17.0, 'conifer20b', None, 'band-tree-8'),
           (232, 15.5, 'conifer32b', None, None), (66, 21.0, 'conifer48a', None, None),
           (2010, 19.5, 'conifer48b', None, None), (-210, 16.0, 'conifer32a', None, None), (2150, 15.0, 'conifer32a', None, None))
    for k, (x, s_, src, dense, front_nm) in enumerate(FAR, start=1):
        t = solve_t(G, PC, s_, x)
        q = {'kind': 'relief', 'name': NEW + 'far-tree-%d' % k, 'src': src, 't': t, 's': s_, 'yaw': round(rng.uniform(-5, 5), 1), 'flip': False,
             'shadow': 0, 'sway': 0.2}
        if dense is not None:
            qd = dict(q, src=dense)
            fr_ = [p for p, _ in new if p.get('name') == NEW + front_nm]
            od = place.place_part(L_tmp, G, PC, 0, qd, with_image=False)
            tw_f = place.r3_trunk_px(place.place_part(L_tmp, G, PC, 0, fr_[0], with_image=False), PC) if fr_ else 0.0
            tw_d = place.r3_trunk_px(od, PC) if od is not None else 1e9
            if violations(qd, True) == 0 and tw_d < tw_f:
                log.append('奥の木 %s: 密な針葉樹 %s (幹 %.0fpx < 手前の %s %.0fpx)' % (q['name'], dense, tw_d, front_nm, tw_f))
                add(qd, 'R1 奥の木')
                continue
            log.append('奥の木 %s: 密な針葉樹は規則に掛かる・幹 %.0f/%.0fpx → %s' % (q['name'], tw_d, tw_f, src))
            if violations(q, True) == 0:
                add(q, 'R1 奥の木')
            else:
                log.append('置かない %s (帯の木の真後ろから寄せない)' % q['name'])
            continue
        add_settled(q, 'R1 奥の木')
    # 敵の側の細い幹 4 (conifer_w20・霧 F45 ≥0.72 の深さ = 幹の見かけ <40px。今の霧では消える・霧の変種 end 40・45 で層になる)
    for k, x in enumerate((1112, 1290, 1468, 1662), start=1):
        s_ = 26.0
        for _ in range(40):   # 霧 F45 が 0.72 になる s まで奥へ
            t = solve_t(G, PC, s_, x)
            d = PC.project(G.on_path(t, s_, G.gy(t, s_)))[2]
            if place.fog_frac(d, 14.0, 45.0, PC.r) >= 0.72:
                break
            s_ = round(s_ + 0.5, 2)
        add({'kind': 'relief', 'name': NEW + 'enemy-thin-%d' % k, 'src': 'conifer20a' if k % 2 else 'conifer20b', 't': t, 's': s_,
             'yaw': round(rng.uniform(-4, 4), 1), 'flip': False, 'shadow': 0, 'sway': 0.2}, 'R1 敵の側の細い幹 (霧に沈む)')
    # 最奥の列 3 (conifer_w48・s 33〜40・霧に溶けて輪郭だけ)
    for k, (x, s_) in enumerate(((420, 36.0), (1000, 38.0), (1790, 35.0)), start=1):
        t = solve_t(G, PC, s_, x)
        add({'kind': 'relief', 'name': NEW + 'farthest-%d' % k, 'src': 'conifer48a' if k % 2 else 'conifer48b', 't': t, 's': s_, 'yaw': 0,
             'flip': False, 'shadow': 0}, 'R1 最奥の列')

    # ================================================================ R2 垂れる枝 (段1b: 画面で幅 120〜300px。付け根は木の幹の近く)
    # 新しい bough_hang (段 3〜5 枚の棚・付け根 = 絵の左の辺 12 行目・右へ厚くなる楔)。絵の幅 119〜151 ドットは、霧 R 0.3〜0.7 の深さだと
    # 画面で 400〜500px になる (所見 1 の「大きな椰子の葉」)。→ ①右: 近い幹 (右) の奥から左へ (反転・付け根は幹の後ろに隠れる = 見えるのは幹の左の縁から先だけ)
    # ②真ん中: 奥の木 (s 14〜15) の幹から霧の上を横切る高い枝 (霧 R 0.8 前後 = 本家の「霧に透けた高い枝」)。左は近い木の段 (canopy-l) が覆う
    # 右上は近い木の段 (canopy-r) が覆う。その下 (行 232〜283) に枝を足すと右の意図の札 (行 283〜403・x1628〜1819) に掛かる (試した = 置き場が無い)
    # 主人公と敵の間の上は canopy-c が覆う (霞んだ枝を重ねると、帯の木の枝の縁が霧の上でなく枝の上に乗って模様 M2 が減る = 模型で確かめた)
    BOUGH = [('c1', 3, 14.6, 905, 24, False),        # 帯の木 band-tree-4 (x866) の奥の幹から右へ = 霧の芯の上を横切る高い枝 (160 のボスの箱 x1200 より左)
             ('c2', 3, 13.6, 700, 124, False)]       # 帯の木 band-tree-2 (x660) の奥の幹から右へ (霧の帯の頂点の行 285〜345 より上)
    nbw = 0
    for name, v, s_, xa, row_top, fl in BOUGH:
        im_, _ = place.art_for(L, 'boughHang%d' % v)                        # アトラスと同じくアルファで切った大きさ
        art_w, art_h = im_.size[0] / place.TPU, im_.size[1] / place.TPU
        # 板の足元 (下辺の中央) を、付け根の辺が xa に来る t に。flip なら付け根 (絵の左の辺) は画面の右側
        y = 8.0
        t = 0.0
        for _ in range(6):
            cx = xa + (art_w / 2) * PC.f / max(1.0, PC.project(G.on_path(t, s_, y))[2]) * (-1 if fl else 1)
            t = solve_t_abs(G, PC, s_, y, cx)
            y = solve_y_abs(G, PC, t, s_, row_top) - art_h      # 板の上の縁の行 = row_top
        q = add_settled({'kind': 'relief', 'name': NEW + 'bough-' + name, 'src': 'boughHang%d' % v, 't': t, 's': s_, 'y': round(y, 3), 'abs': True,
                         'yaw': 0, 'flip': fl, 'shadow': 0, 'sway': 1.0, 'swayFrom': 'left'}, 'R2 垂れる枝', tree=False, max_dx=96)
        if q is not None:
            nbw += 1
            o = place.place_part(L_tmp, G, PC, 0, q, with_image=True)
            vm, npx = place.visible_mask(o, PC, tdf['PC'], PC.W, PC.H)
            cols = np.nonzero(vm.any(axis=0))[0] if vm is not None else []
            if near_r is not None and name.startswith('r'):   # 近い幹 (右) の後ろの所は見えない
                on = place.place_part(L_tmp, G, PC, 0, near_r, with_image=True)
                vn, _ = place.visible_mask(on, PC, tdf['PC'], PC.W, PC.H)
                if vn is not None and len(cols):
                    vm = vm & ~vn
                    cols = np.nonzero(vm.any(axis=0))[0]
            log.append('垂れる枝 %s: 画面の幅 %dpx (x %d〜%d)・霧 R %.2f' % (q['name'], len(cols), cols.min() if len(cols) else -1,
                                                                   cols.max() if len(cols) else -1, fog_r(q)))
    log.append('垂れる枝 %d' % nbw)

    # ================================================================ R4 茂みの塊 3 段 (段1b 所見 3: 低く途切れ途切れの土手)
    # 新しい bush_clump (低い山の重なり・見える高さ l 46／m 33〜37／s 23〜24 ドット)。座席の帯に近い段は s 5 より奥へ。l は全部で 3 個まで、m と s を主に。
    # 塊の間は空ける (塊の幅 + 40〜220px) = 間に土手の株 (下の株の節)。tint は頂の BUSH_TINT (直しの輪1 で明るい土手 [0.95,0.97,1.0])。全座席の足元の通りと主人公の後ろの窓・意図の札を空ける
    def ok_box(p, need_fog=0.7):
        """部品の箱 (代わりの影絵の板の矩形) が PC・スマホの窓と、暗いなら意図の札に掛からない"""
        for cam, cn in ((PC, 'PC'), (PH, 'PH')):
            o = place.place_part(L_tmp, G, cam, 0, p, with_image=False)
            if o is None or o.box is None or o.note == 'phone-hide':
                continue
            x0, y0, x1, y1 = o.box
            w0, wy0, w1, wy1 = HERO_WIN[cn]
            if x1 >= w0 - HERO_WIN_PAD and x0 <= w1 + HERO_WIN_PAD and y1 >= wy0 and y0 <= wy1:
                return False
            if place.fog_frac(o.depth, 14.0, 45.0, cam.r) < need_fog:
                for r in intents[cn]:
                    if boxes_intersect(o.box, r):
                        return False
        return True
    seats_pts = {}
    for cam in (PC, PH, PCU, PC21):   # 段2 C3: 21:9 も (座席の帯の前の縁 s −4.8 の株が 21:9 の 4体目の通りに入った)
        pts, st = place.r3_seat_points(cam, G)
        seats_pts[cam.name] = pts

    def corr_ok(p):
        """どのカメラでも全座席の足元の通り (x ±幅・根元が足元の 8px 上より手前・先が足元の 20px 下より上) に掛からない"""
        for cam in (PC, PH, PCU, PC21):
            o = place.place_part(L_tmp, G, cam, 0, p, with_image=False)
            if o is None or o.box is None or o.note == 'phone-hide':
                continue
            x0, y0, x1, y1 = o.box
            for nm_, fx, fy, hw in seats_pts[cam.name]:
                if x1 >= fx - hw and x0 <= fx + hw and y1 >= fy - place.R3_CORR_BACK and y0 <= fy + place.R3_CORR_FRONT:
                    return False
        return True
    rng = random.Random(SEED_R3 + 4)
    ROWS = (  # (段, s の範囲, 塊の間の空き (px), 大きさの候補 = 重み付きの抽選)
        ('a', (5.1, 6.0), (60, 220), ('S', 'M', 'S', 'S', 'M', 'L')),
        ('b', (7.8, 8.9), (40, 180), ('M', 'S', 'M', 'S', 'L')),       # 段1 の縦の面 (s 9) のすぐ手前 = 面を隠す・段 a の塊と画面で重ねない
        ('c', (9.3, 10.6), (120, 300), ('S', 'S', 'M')))
    # BUSH_TINT は頂の定数 (直しの輪1 (b)3 で [0.7,0.78,0.9] → [0.95,0.97,1.0])
    nb = 0
    nL = 0
    bank_gaps = []   # 塊の間 (段, s の範囲, x0, x1) = 土手の株の置き場
    row_a_spans = []
    for row, (slo, shi), gap, sizes in ROWS:
        x = rng.uniform(-60, 40)
        k = 0
        prev_x1 = None
        while x < 2010:
            s_ = round(rng.uniform(slo, shi), 2)
            t = solve_t(G, PC, s_, x)
            first = rng.choice(sizes)
            if first == 'L' and nL >= 3:
                first = 'M'
            order = [z for z in 'LMS' if 'LMS'.index(z) >= 'LMS'.index(first)]   # 選んだ大きさ → 小さい方へ
            choice = None
            twin = False
            for sz in order:
                src = 'bush%s%d' % (sz, rng.choice((1, 2)))
                if sz == 'L' and row in 'ab':
                    # 段2 C3 (2026-10-02): l (見える高さ 46 ドット・片側に尖った峰) は段 a・b に置かない。試しの撮影 2 回目で段 a の l が x 960 に背の高い円錐の
                    # 茂みとして 1 つ目立った (峰 46 → m は 33〜37 ドット = 画面で約 150 → 120px)。l の幅 (96 ドット) は m を 2 つ並べて埋める (段1 の縦の面を隠す幅を保つ)
                    src = 'bushM' + src[-1]
                    twin = True
                cand = {'kind': 'relief', 'name': NEW + 'bush-%s%d' % (row, k + 1), 'src': src, 't': t, 's': s_, 'yaw': round(rng.uniform(-10, 10), 1),
                        'flip': rng.random() < 0.5, 'shadow': 1 if row == 'a' else 0, 'tint': list(BUSH_TINT), 'sway': 0.4}
                if row == 'b':   # 段 a の塊の真後ろには置かない (画面で横に 35% 以上重なれば次へ = 2 段が重なって生け垣の列にならない)
                    ob_ = place.place_part(L_tmp, G, PC, 0, cand, with_image=False)
                    bw_ = max(1.0, ob_.box[2] - ob_.box[0])
                    if any(min(ob_.box[2], b1_) - max(ob_.box[0], b0_) > 0.35 * bw_ for b0_, b1_ in row_a_spans):
                        continue
                if ok_box(cand) and corr_ok(cand):
                    choice = cand
                    break
            wpx = 40.0 if row == 'b' else 120.0
            if choice is not None:
                k += 1
                nb += 1
                if choice['src'].startswith('bushL'):
                    nL += 1
                add(choice, 'R4 茂みの塊 段%s' % row)
                o = place.place_part(L_tmp, G, PC, 0, choice, with_image=False)
                wpx = o.wpx
                if twin:   # l の代わりの 2 つ目の m (1 つ目の右に 15% 重ねて・反転を逆に・s を少しずらす)
                    s2 = round(min(shi, max(slo, s_ + rng.uniform(-0.25, 0.25))), 2)
                    c2 = {k_: v_ for k_, v_ in choice.items() if k_ != 'seed'}
                    c2 = dict(c2, name=NEW + 'bush-%s%d' % (row, k + 1), src='bushM%d' % rng.choice((1, 2)), s=s2,
                              t=solve_t(G, PC, s2, o.box[2] + 0.35 * o.wpx), yaw=round(rng.uniform(-10, 10), 1), flip=not choice['flip'])
                    if ok_box(c2) and corr_ok(c2):
                        k += 1
                        nb += 1
                        add(c2, 'R4 茂みの塊 段%s' % row)
                        o2 = place.place_part(L_tmp, G, PC, 0, c2, with_image=False)
                        o = place.place_part(L_tmp, G, PC, 0, choice, with_image=False)
                        o.box = (min(o.box[0], o2.box[0]), o.box[1], max(o.box[2], o2.box[2]), o.box[3])
                        wpx = o.box[2] - o.box[0]
                if row == 'a':
                    row_a_spans.append((o.box[0], o.box[2]))
                if prev_x1 is not None and o.box[0] - prev_x1 > 30:
                    bank_gaps.append((row, (slo, shi), prev_x1, o.box[0]))
                prev_x1 = o.box[2]
            x += wpx + rng.uniform(*gap)
    log.append('茂みの塊 %d (l %d・段 a・b の l は m 2 つ)' % (nb, nL))

    # ================================================================ R4 株: 段1 の前の縁 40・座席の帯 40〜60
    existing = [(p['t'], p['s']) for p in parts if p['kind'] in ('litter', 'card') and 't' in p] + [(p['t'], p['s']) for p, _ in new if p['kind'] in ('litter', 'card')]
    BOXES_KARAKURI = ((-6.3, 1.7), (-5.75, 2.4), (-5.3, -1.9))

    def tuft_ok(p, check_window=True, center_free=False, tips_always=False):
        """株の板の矩形が、どのカメラでも全座席の足元の通りに掛からない (PC・スマホ・R3 の試し撮り)・主人公の後ろの窓の x の外 (段の縁の株)・
        画面の真ん中 ±200px (座席の帯の株)"""
        for cam in (PC, PH, PCU, PC21):
            o = place.place_part(L_tmp, G, cam, 0, p, with_image=False)
            if o is None or o.box is None:
                continue
            x0, y0, x1, y1 = o.box
            for nm_, fx, fy, hw in seats_pts[cam.name]:
                if x1 >= fx - hw and x0 <= fx + hw and y1 >= fy - place.R3_CORR_BACK and y0 <= fy + place.R3_CORR_FRONT:
                    return False
            if (p['s'] < -3.0 or tips_always) and cam is not PCU and cam is not PC21:   # art-bible §2-2「世界に置く株は 株の先の行 ＞ その x でいちばん低い足の行 ＋20」(主人公 ±180・敵 ±120。二周目の frontTips)
                for nm_, fx, fy, hw in seats_pts[cam.name]:
                    if nm_.startswith('doll'):
                        continue
                    hw2 = 180 if nm_ == 'hero' else 120
                    if x1 >= fx - hw2 and x0 <= fx + hw2 and y0 <= fy + 20:
                        return False
            if cam in (PC, PH) and check_window:   # 段2 C3: スマホの窓も (茂みの並びが変わり、土手の株 1 本がスマホの窓に 349px 入った)
                w0, wy0, w1, wy1 = HERO_WIN[cam.name]
                if x1 >= w0 - HERO_WIN_PAD and x0 <= w1 + HERO_WIN_PAD and y1 >= wy0 and y0 <= wy1:
                    return False
            if cam is PC and center_free and abs((x0 + x1) / 2 - 960) <= 200 + (x1 - x0) / 2:
                return False
            if cam is PC and p['kind'] == 'card':
                for r in intents['PC']:
                    if boxes_intersect(o.box, r):
                        return False
        for bt, bs in BOXES_KARAKURI:
            if abs(p['t'] - bt) < 0.8 and abs(p['s'] - bs) < 0.8:
                return False
        return True

    def far_enough(t, s_, md=0.55):
        return not any(abs(t - a) < md and abs(s_ - b) < 0.45 for a, b in existing)

    # 二周目から残る立った株 (背丈 10 ドット以上) のうち、どれかの座席の足元の通りに掛かる物は外す (人形 9体の通りが新しい = 空き地の手前の縁の塊)
    def in_corridor(p):
        for cam in (PC, PH, PCU):
            o = place.place_part(L_tmp, G, cam, 0, p, with_image=False)
            if o is None or o.box is None or not o.visible or o.worldH < place.R3_CORR_MIN_H:
                continue
            x0, y0, x1, y1 = o.box
            for nm_, fx, fy, hw in seats_pts[cam.name]:
                if x1 >= fx - hw and x0 <= fx + hw and y1 >= fy - place.R3_CORR_BACK and y0 <= fy + place.R3_CORR_FRONT:
                    return True
        return False
    drop(lambda p: p['kind'] in ('litter', 'card') and in_corridor(p), 'R4 足元の通りに掛かる二周目の株')

    # 段1 の前の縁 40: 2〜4 本の塊を不規則に。6 割は縁のすぐ奥 (天面の縁を切る)、4 割は縁の足元 (前の面の下を切る)
    rng = random.Random(SEED_R3 + 5)
    made = 0
    x = rng.uniform(560, 620)
    tries = 0
    while made < 40 and tries < 400:
        tries += 1
        nn = min(40 - made, rng.choice((2, 3, 3, 4)))
        tc = solve_t(G, PC, 7.0, x)
        for _ in range(3):
            tc = solve_t(G, PC, f1(tc), x)
        for m_ in range(nn):
            t = round(tc + rng.uniform(-0.6, 0.6), 3)
            e = f1(t)
            s_ = round(e + rng.uniform(0.05, 0.5) if rng.random() < 0.6 else e - rng.uniform(0.1, 0.5), 3)
            sr = rng.choice(('tuftStand3', 'tuftStand4', 'tuftStand3', 'tuftStand1', 'tuftStand2'))
            kind = 'litter' if sr in ('tuftStand1', 'tuftStand2') else 'card'
            p = {'kind': kind, 'name': NEW + 'edge-tuft', 'src': sr, 't': t, 's': s_, 'yaw': round(rng.uniform(-12, 12), 1), 'flip': rng.random() < 0.5}
            if kind == 'card':
                p['sway'] = 0.5
            if not far_enough(t, s_, 0.45) or not tuft_ok(p):
                continue
            add(p, 'R4 段1 の前の縁の株')
            existing.append((t, s_))
            made += 1
        x += rng.uniform(40, 120)
        if x > 1960:
            x = rng.uniform(560, 640)   # 一周して足りなければもう一周 (塊の間を埋める)
    log.append('段1 の前の縁の株 %d (試行 %d)' % (made, tries))

    # 座席の帯の株 48 (t −9〜13.5・s −4.2〜4.3 = 座席の帯と空き地の手前・奥の縁・背丈 12 ドット = litter)。空き地の縁に寄せ、真ん中 ±200px は土・
    # 全座席の足元の通りを空ける (足元より手前は隠すので置かない = 敵の側の座席の帯の手前は空く。奥は体の後ろ)
    rng = random.Random(SEED_R3 + 6)
    sb = [p for p in parts if p['kind'] == 'slab' and p['name'] == 'seat-band'][0]
    mc, mr = sb['maskCenter'], sb['maskRadius']
    made = 0
    tries = 0
    seat_xy = []
    while made < 48 and tries < 24000:
        tries += 1
        t = round(rng.uniform(-9.0, 13.5), 3)
        s_ = round(rng.uniform(-4.2, 4.3), 3)   # 座席の帯 (s −2.6〜2.8) と空き地の手前・奥の縁 (茂みの段 a の手前まで)
        rr = math.hypot((t - mc[0]) / mr[0], (s_ - mc[1]) / mr[1])
        if rr < 0.62 and rng.random() < 0.75:
            continue
        p = {'kind': 'litter', 'name': NEW + 'seat-tuft', 'src': rng.choice(('tuftStand1', 'tuftStand2')), 't': t, 's': s_,
             'yaw': round(rng.uniform(-12, 12), 1), 'flip': rng.random() < 0.5}
        sx, sy, _ = PC.project(G.on_path(t, s_, G.gy(t, s_)))
        if not (40 <= sx <= 1900):                          # 16:9 の画面の中だけ (21:9 の余白には置かない)
            continue
        if any(abs(sx - a) < 50 and abs(sy - b) < 20 for a, b in seat_xy):   # 画面で間を空ける (塊が絨毯にならない)
            continue
        if not far_enough(t, s_, 0.6) or not tuft_ok(p, check_window=False, center_free=True):
            continue
        add(p, 'R4 座席の帯の株')
        existing.append((t, s_))
        seat_xy.append((sx, sy))
        made += 1
    log.append('座席の帯の株 %d (試行 %d)' % (made, tries))

    # 段1b 所見 3: 土手の株 (茂みの塊の間・背丈 16〜20 ドットの立った株 = card)。茂みの段 a・b の空きに 1〜2 本ずつ
    rng = random.Random(SEED_R3 + 8)
    made = 0
    for row, (slo, shi), gx0, gx1 in bank_gaps:
        if row == 'c':
            continue
        for _ in range(rng.choice((1, 1, 2))):
            for _try in range(6):
                x = rng.uniform(gx0 + 10, gx1 - 10)
                s_ = round(rng.uniform(slo, shi), 3)
                t = solve_t(G, PC, s_, x)
                p = {'kind': 'card', 'name': NEW + 'bank-tuft', 'src': rng.choice(('tuftStand3', 'tuftStand4', 'tuftStand3')), 't': t, 's': s_,
                     'yaw': round(rng.uniform(-12, 12), 1), 'flip': rng.random() < 0.5, 'sway': 0.5}
                if far_enough(t, s_, 0.4) and tuft_ok(p):
                    add(p, 'R4 土手の株 (茂みの間)')
                    existing.append((t, s_))
                    made += 1
                    break
    log.append('土手の株 %d (茂みの間 %d か所)' % (made, sum(1 for g in bank_gaps if g[0] != 'c')))

    # 段1b 所見 4: 座席の帯の前の縁 (空き地の手前の縁 = 土の前の縁。M6 地面の縁の硬さ) に沿った低い株を不規則な塊で。
    # 株の先は足の行 +20 より下 (art-bible §2-2・主人公 ±180・敵 ±120) なので、縁が足元のすぐ下の所は
    # ①縁のすぐ手前に背丈 12 ドットの株 (tuftStand1・2) ②縁の上に小さな株 (litterTuft 4〜5 ドット) ③背丈 12 ドットの株を縁の少し手前の草の側、の順に置ける方を選ぶ
    # 段2 C3 (2026-10-02): 縁は座席の帯の段の直線でなく空き地の値ノイズの縁 (dirt_front_s = Diorama.ClearingMask の写し) になったので、株もその縁に沿わせる。
    # 26 → 52 本・塊 1〜4 本・塊の間 30〜90px (縁の長さ x150〜1850 の 6〜7 割を株の塊が切る = 一本の横線にしない)
    def s_edge(t):
        e = dirt_front_s(sb, t)
        return e if e is not None else mcy - mry * math.sqrt(max(0.0, 0.98 - ((t - mcx) / mrx) ** 2))
    mcx, mcy = sb['maskCenter']
    mrx, mry = sb['maskRadius']
    N_FRONT = 52
    rng = random.Random(SEED_R3 + 7)
    made = 0
    tries = 0
    kinds = Counter()
    x = rng.uniform(150, 210)
    while made < N_FRONT and tries < 2400:
        tries += 1
        nn = min(N_FRONT - made, rng.choice((1, 2, 2, 3, 3, 4)))
        tc = solve_t(G, PC, -3.5, x)
        for _ in range(4):
            tc = solve_t(G, PC, s_edge(tc), x)
        for m_ in range(nn):
            t = round(tc + rng.uniform(-0.4, 0.4), 3)
            yaw_, flip_ = round(rng.uniform(-12, 12), 1), rng.random() < 0.5
            big = rng.choice(('tuftStand1', 'tuftStand2'))
            small = rng.choice(('litterTuft1', 'litterTuft2', 'litterTuft4', 'litterTuft2'))
            cands = [(big, rng.uniform(0.05, 0.5)), (small, rng.uniform(-0.1, 0.35)), (big, rng.uniform(0.7, 1.6))]
            for src, off in cands:
                s_ = round(s_edge(t) - off, 3)
                p = {'kind': 'litter', 'name': NEW + 'front-tuft', 'src': src, 't': t, 's': s_, 'yaw': yaw_, 'flip': flip_}
                if far_enough(t, s_, 0.3) and tuft_ok(p, check_window=False, tips_always=True):
                    add(p, 'R4 座席の帯の前の縁の株')
                    existing.append((t, s_))
                    made += 1
                    kinds['大きい株 縁' if (src == big and off < 0.6) else ('小さい株 縁' if src == small else '大きい株 縁の手前')] += 1
                    break
        x += rng.uniform(30, 90)
        if x > 1850:
            x = rng.uniform(150, 260)
    log.append('座席の帯の前の縁の株 %d (試行 %d・%s)' % (made, tries, dict(kinds)))

    # ================================================================ R6 額縁の草 10 (カメラに付く。PC の vx・vy・深さ・scale。スマホは phone で左右だけ)
    # 左 4・右 3 = 手札 (PC x463〜1458) の外 (L8 = 額縁の矩形と UI の重なり ≤28%)。真ん中 3 = R3 の試し撮り (手札を 164px 沈める) で手札の上端の上
    # (普段の UI では手札の後ろ = L8 は外れる。報告)。どれも矩形の上端を名前の帯の下 (PC 707・R3 の試し撮り 757) より下 = 行 770 以下に置く (vy で解く)
    def ui_rects(cn, sunk=False):
        out = []
        for rec in ui[cn].values():
            if sunk:
                rr = [[h_[0], h_[1] + 164, h_[2], h_[3]] for h_ in rec['hand']] + list(rec['intents']) + list(rec['self'])
            else:
                rr = list(rec['hand']) + list(rec['intents']) + list(rec['strips']) + list(rec['self']) + list(rec['doll_tags'])
            if rec.get('topbar'):
                rr.append(rec['topbar'])
            out.append(rr)
        return out

    def frame_box(p, cam):
        o = place.place_part(L_tmp, G, cam, 0, p, with_image=False)
        x0, y0, x1, y1 = o.box
        return o.box, (max(0, x0), max(0, y0), min(cam.W, x1) - max(0, x0), min(cam.H, y1) - max(0, y0))

    def fit_frame(p, cam, top, rects_by_scene, max_l8=0.28):
        """矩形の上端が top の行に来る vy を解き、L8 (場面ごとの最大) が max_l8 を超えれば外側 (近い画面の端) へ vx を 0.01 ずつ、それでも駄目なら scale を 0.92 倍"""
        q = dict(p)
        for it in range(30):
            lo, hi = 0.0, 0.5
            for _ in range(40):
                q['vy'] = round((lo + hi) / 2, 4)
                if frame_box(q, cam)[0][1] < top:
                    hi = q['vy']
                else:
                    lo = q['vy']
            q['vy'] = round(lo, 4)
            box, r = frame_box(q, cam)
            l8 = max(place._rect_union_frac(r, rr) for rr in rects_by_scene) if r[2] > 0 and r[3] > 0 else 0.0
            if l8 <= max_l8:
                break
            if it < 12:
                q['vx'] = round(q['vx'] - 0.01 if q['vx'] < 0.5 else q['vx'] + 0.01, 4)
            else:
                q['scale'] = round(q['scale'] * 0.92, 3)
        return q, round(l8, 3), box

    FORE = (  # (vx, depth, scale, roll, flip, 絵, 上端の行, 群)
        (0.035, 11.5, 0.56, 3.0, False, 1, 790, 'side'), (0.115, 13.5, 0.55, -2.0, True, 2, 850, 'side'),
        (0.170, 12.5, 0.50, 4.0, False, 3, 780, 'side'), (0.215, 13.0, 0.50, -3.0, True, 5, 900, 'side'),
        (0.800, 12.0, 0.56, 2.0, False, 6, 820, 'side'), (0.880, 13.0, 0.58, -4.0, True, 3, 790, 'side'), (0.965, 11.0, 0.62, 3.0, False, 1, 800, 'side'),
        (0.360, 13.5, 0.55, -2.0, False, 4, 770, 'mid'), (0.515, 13.0, 0.50, 2.5, True, 5, 775, 'mid'), (0.665, 14.0, 0.55, -3.0, False, 2, 770, 'mid'))
    PHONE_FORE = {1: (0.04, 11.5, 0.5, 520), 2: (0.11, 13.5, 0.5, 600), 6: (0.90, 13.0, 0.52, 560), 7: (0.965, 11.0, 0.55, 520)}   # (vx, depth, scale, 上端の行)
    rect_pc = ui_rects('PC')
    rect_pcu = ui_rects('PC', sunk=True)
    rect_ph = ui_rects('PH')
    for k, (vx, dp, sc, roll, fl, v, top, grp) in enumerate(FORE, start=1):
        p = {'kind': 'frame', 'name': NEW + 'fore-%d' % k, 'src': 'foreGrass%d' % v, 'vx': vx, 'vy': 0.1, 'depth': dp, 'scale': sc, 'flip': fl,
             'roll': roll, 'tint': FORE_TINT, 'sway': 0.8}
        p, l8, box = fit_frame(p, PC, top, rect_pc if grp == 'side' else rect_pcu)
        l8n = max(place._rect_union_frac(frame_box(p, PC)[1], rr) for rr in rect_pc)
        if k in PHONE_FORE:
            pvx, pdp, psc, ptop = PHONE_FORE[k]
            ph = {'vx': pvx, 'vy': 0.1, 'depth': pdp, 'scale': psc}
            # スマホ: frame_box は phone の上書きを読む (place_frame)
            for it in range(30):
                lo, hi = 0.0, 0.5
                for _ in range(40):
                    m = (lo + hi) / 2
                    ph['vy'] = round(m, 4)
                    bxy = place.place_part(L_tmp, G, PH, 0, dict(p, phone=ph), with_image=False).box
                    if bxy[1] < ptop:
                        hi = m
                    else:
                        lo = m
                ph['vy'] = round(lo, 4)
                bx = place.place_part(L_tmp, G, PH, 0, dict(p, phone=ph), with_image=False).box
                r = (max(0, bx[0]), max(0, bx[1]), min(PH.W, bx[2]) - max(0, bx[0]), min(PH.H, bx[3]) - max(0, bx[1]))
                l8p = max(place._rect_union_frac(r, rr) for rr in rect_ph) if r[2] > 0 and r[3] > 0 else 0.0
                if l8p <= 0.28:
                    break
                if it < 12:
                    ph['vx'] = round(ph['vx'] - 0.01 if ph['vx'] < 0.5 else ph['vx'] + 0.01, 4)
                else:
                    ph['scale'] = round(ph['scale'] * 0.92, 3)
            p['phone'] = ph
            log.append('額縁 %s PC vx %.3f vy %.3f scale %.2f 上端 %d L8 %.2f (普段 %.2f)・スマホ vx %.3f vy %.3f scale %.2f L8 %.2f' % (
                p['name'], p['vx'], p['vy'], p['scale'], box[1], l8, l8n, ph['vx'], ph['vy'], ph['scale'], l8p))
        else:
            p['phone'] = {'hide': True}
            if grp == 'mid':
                p['onlyWith'] = 'uitrial'   # 段1b 所見 5: 旗 uitrial=1 (手札を沈めた R3 の試し撮り) の時だけ組む = 普段は手札の後ろに組まない (Diorama.R3I_FlagHidden)
            log.append('額縁 %s PC vx %.3f vy %.3f scale %.2f 上端 %d L8 %.2f%s (普段の UI %.2f)・スマホは外す' % (
                p['name'], p['vx'], p['vy'], p['scale'], box[1], l8, ' (R3 の試し撮り)' if grp == 'mid' else '', l8n))
        add(p, 'R6 額縁の草')

    # ================================================================ R8 α合成の霧の板 2 (kind mist・取り決め 2)
    add({'kind': 'mist', 'name': NEW + 'mist-near', 't': 4.5, 's': 9.0, 'y': 0.0, 'w': 70.0, 'h': 2.0, 'alpha': 0.32, 'noise': [0.18, 0.9], 'flow': 0.02},
        'R8 霧の板 (s 8〜10)')
    add({'kind': 'mist', 'name': NEW + 'mist-far', 't': 8.0, 's': 14.5, 'y': 0.0, 'w': 90.0, 'h': 2.4, 'alpha': 0.28, 'noise': [0.1, 0.6], 'flow': 0.015,
         'phone': {'hide': True}}, 'R8 霧の板 (s 13〜16・スマホは外す = 1 枚)')

    # ---- スマホ: 新しい半立体が主人公の後ろの窓・意図の札 (暗い物) に入るなら、スマホだけ t をずらす (phone {t})・ずらしきれなければ外す
    #      霧に沈める物 (敵の側の細い幹・最奥の列) はスマホでは外す (スマホのカメラは近い = 同じ深さで霧が薄く、太く写る。重さも減らす)
    for p, why in new:
        if p['kind'] == 'relief' and (p['name'].startswith(NEW + 'enemy-thin') or p['name'].startswith(NEW + 'farthest')):
            p['phone'] = {'hide': True}
    rcp = G.raycast(PH, 4)
    tdfp = place.upsample(rcp, PH.W, PH.H)
    pintents = intents['PH']

    def ph_bad(p):
        o = place.place_part(L_tmp, G, PH, 0, p, with_image=True)
        if o is None or o.note == 'phone-hide':
            return 0, 0
        vm, npx = place.visible_mask(o, PH, tdfp, PH.W, PH.H)
        if vm is None:
            return 0, 0
        w0, wy0, w1, wy1 = HERO_WIN['PH']
        cw = int(vm[wy0:wy1, max(0, w0 - HERO_WIN_PAD):w1 + HERO_WIN_PAD].sum())
        ci = 0
        if place.fog_frac(o.depth, 14.0, 45.0, PH.r) < 0.7 and not place.r3_hazy_tree_ok(o, PH, False):
            for r in pintents:
                x0, y0, ww, hh = [int(round(v)) for v in r]
                ci += int(vm[max(0, y0):max(0, y0 + hh), max(0, x0):max(0, x0 + ww)].sum())
        return cw, ci
    # 持ち上げた近い木の段 (canopy): スマホのカメラでも密な針葉樹の幹が近い幹の後ろに隠れ、窓と意図の札に掛からなければ残す。そうでなければ外す
    #  (近い幹 (右) はスマホで外すので canopy-r も外す)
    canopy_names = set()
    for cp, base in canopies:
        canopy_names.add(cp['name'])
        ok_ = not (isinstance(base.get('phone'), dict) and base['phone'].get('hide'))
        why_ = '近い幹をスマホで外す'
        if ok_:
            pb = dict(base)
            if isinstance(base.get('phone'), dict):
                pb.update({k: v for k, v in base['phone'].items() if k in ('t', 's', 'y')})
            pb.pop('phone', None)
            xb_ = PH.project(G.on_path(pb['t'], pb['s'], G.gy(pb['t'], pb['s'])))[0]
            xc_ = PH.project(G.on_path(cp['t'], cp['s'], cp['y']))[0]
            ob = place.place_part(L_tmp, G, PH, 0, pb, with_image=False)
            oc = place.place_part(L_tmp, G, PH, 0, dict(cp, phone=None), with_image=False)
            hb = place.r3_trunk_px(ob, PH) * 0.8 / 2      # 近い幹の見かけの半幅 (枝の付け根を除いた幹 ≈ 取り決めの幅の 0.8)
            hc = place.r3_trunk_px(oc, PH) / 2
            cw_, ci_ = ph_bad(dict(cp, phone=None))
            ok_ = abs(xc_ - xb_) + hc <= hb and cw_ == 0 and ci_ == 0
            why_ = '幹のずれ %.0fpx (許す %.0f)・窓 %dpx・意図の札 %dpx' % (abs(xc_ - xb_), hb - hc, cw_, ci_)
        if not ok_ and cp['name'] == NEW + 'canopy-r' and CANOPY_R_PHONE_T is not None:
            # 直しの輪1 (b)8: スマホの上の覆い (M2 0.24 → 約 0.30) に右上の段を戻す。近い幹 (右) はスマホで外したままなので、段の下の短い幹は
            # 帯の木 band-tree-6・7 (スマホで x 1892・1912) の幹の前に重ねる = t 15.5 (足元の x ≈1886)。窓と意図の札 (暗い物) に掛からない時だけ
            q_ = dict(cp, t=CANOPY_R_PHONE_T)
            q_.pop('phone', None)
            cw_, ci_ = ph_bad(q_)
            if cw_ == 0 and ci_ == 0:
                ok_ = True
                cp['phone'] = {'t': CANOPY_R_PHONE_T}
                xr_ = PH.project(G.on_path(CANOPY_R_PHONE_T, cp['s'], cp['y']))[0]
                why_ += ' → 直しの輪1: t %.1f (足元の x %.0f)・窓 0・意図の札 0' % (CANOPY_R_PHONE_T, xr_)
            else:
                why_ += ' → 直しの輪1: t %.1f は窓 %dpx・意図の札 %dpx = 外す' % (CANOPY_R_PHONE_T, cw_, ci_)
        if not ok_:
            cp['phone'] = {'hide': True}
        log.append('スマホ: %s %s (%s)' % (cp['name'], '残す' if ok_ else '外す', why_))
    nph = 0
    for p, why in new:
        if p['kind'] != 'relief' or (isinstance(p.get('phone'), dict) and p['phone'].get('hide')) or p['name'] in canopy_names:
            continue
        cw, ci = ph_bad(p)
        if cw == 0 and ci == 0:
            continue
        t0 = p['t']
        cx = PH.project(G.on_path(t0, p['s'], G.gy(t0, p['s'])))[0]
        done = False
        # 窓に入る物は外 (左) へ・意図の札に入る物は近い方の外へ。PH の x で 30px ずつ 16 回まで
        dirs = (-1,) if cw > 0 and cx < 640 else ((-1, 1) if cx < 1100 else (1, -1))
        for dsign in dirs:
            for k in range(1, 17):
                q = dict(p)
                q['phone'] = {'t': solve_t(G, PH, p['s'], cx + dsign * 30 * k)}
                pq = dict(q)
                pq['t'] = q['phone']['t']
                pq.pop('phone')
                a, b = ph_bad(pq)
                if a == 0 and b == 0:
                    p['phone'] = q['phone']
                    done = True
                    break
            if done:
                break
        if not done:
            p['phone'] = {'hide': True}
        nph += 1
        log.append('スマホ: %s (%s) 窓 %dpx・意図の札 %dpx → %s' % (p['name'], p['src'], cw, ci, p['phone']))

    # ---- スマホの重さ (レーン B の look_act1.phone.json の phone.hide の候補 ②③④⑥⑦): スマホの画に 1000px 未満しか映らない新しい半立体
    #      (垂れる枝の外寄り・21:9 の余白の木)・s 20 より奥の奥の木・茂みの 3 段目はスマホでは組まない (最奥の列と真ん中の額縁は上で外した)
    for p, why in new:
        if p['kind'] != 'relief' or (isinstance(p.get('phone'), dict) and p['phone'].get('hide')):
            continue
        n_ = p['name']
        reason = None
        if n_.startswith(NEW + 'far-tree') and p['s'] > 20:
            reason = 's 20 より奥の奥の木'
        elif n_.startswith(NEW + 'bush-c'):
            reason = '茂みの 3 段目'
        else:
            o = place.place_part(L_tmp, G, PH, 0, p, with_image=True)
            if o is not None and o.note != 'phone-hide':
                vm, npx = place.visible_mask(o, PH, tdfp, PH.W, PH.H)
                if npx < 1000:
                    reason = 'スマホの画に %dpx' % npx
        if reason:
            p['phone'] = {'hide': True}
            log.append('スマホで外す %s (%s)' % (n_, reason))

    # ---- 並べ直し (種類ごとの塊・同じ種類の中は元の順の後ろに新しい物)
    order = ['slab', 'rig', 'block', 'relief', 'rock', 'card', 'litter', 'fog', 'mist', 'shaft', 'frame']
    all_parts = parts + [p for p, _ in new]
    all_parts.sort(key=lambda p: order.index(p['kind']) if p['kind'] in order else 99)
    L['parts'] = all_parts
    cnt = Counter(why for _, why in new)
    for why, n in cnt.items():
        log.append('足す %-34s %3d' % (why, n))
    for p in all_parts:
        if p['kind'] in ('rock', 'block', 'fence', 'marker', 'tree', 'rig', 'card', 'relief', 'litter'):
            assert 'seed' in p, p
    # ---- 門 (取り決め 2): 焼いた後の数から
    n = len(all_parts)
    lit = sum(1 for p in all_parts if p['kind'] == 'litter')
    L['gates'] = {'partsMin': int(math.floor(n * 0.9)), 'partsMax': n + 80, 'litterMax': 250}
    log.append('部品 %d (%s)・小札 %d・門 %s' % (n, dict(Counter(p['kind'] for p in all_parts)), lit, L['gates']))
    si = place.seat_intrusions(L)
    log.append('座席の帯の部品 (seat_intrusions) %d%s' % (len(si), (' ' + str(si)) if si else ''))
    # 頂の並び: gates は tiles・surfaces・sources の前 (読みやすさ。読み手はキーで引く)
    head = {}
    for k in list(L.keys()):
        if k == 'tiles':
            head['gates'] = L['gates']
        if k != 'gates':
            head[k] = L[k]
    L.clear()
    L.update(head)
    place.R3_FORCE_PROXY = False
    place._ART.clear()
    return L, log


DOC_R3 = ('【三周目 段1・段1b (計画 docs/design/hd2d-round3-plan-2026-10-01.md §2 レーン C・分析 hd2d-r3-analysis-2026-10-01.md §7・'
          '2026-10-01／段1b 2026-10-02 = レーン D2 の描き直しの絵と統合の試しの撮影 trial1 の所見 1〜5 で焼き直し)。'
          '二周目の最終の写しは act1_layout_r2.json (look の "layout": "act1_layout_r2" で戻せる)。作り方は docs/design/hd2d-slice/r2-layout-gen/gen_r3.py '
          '(gen_layout.py --r3 = 種 20261003。新しい絵の不透明の画素で規則を判断する = 絵を描き直したら焼き直す)・検査は place.py <設計図> --r3 [--img]'
          ' (模型の M2・T1・G1・E2 の見当と段1 の縦の面の隠れ具合も出す)。'
          'R1 森: 1 本の木 = 幹＋枝＋房が1枚の針葉樹。帯の木 11 (細い conifer_w20 を主に・w32 は手前の1本 band-tree-2 と 21:9 の余白・w48 は奥と余白だけ。'
          '暗く見える幹 = 霧 R 0.46〜0.55 を 主人公と敵の間 x 600〜800・敵の側の左の端 x≈1075・右の端 x 1690〜1765 に。帯の頂点の列 x 880〜1060 は霞んだ1本)・'
          '敵の側の霞んだ密な針葉樹 3 (conifer_dense・霧 R 0.83〜0.88・幹は見かけ 30px 未満 = K11 の外)・奥の木 9 (s 15〜22.5)・最奥の列 3・'
          '敵の側の細い幹 4 (霧 F45 ≥0.72)・近い幹 2 (左 s 3.55 x70 反転・右 s 3.0 x1903。右は道が奥へ向かうので手前へ)。'
          '帯の霞んだ木 (conifer_w20・w32 は霧 R ≥0.45、密な針葉樹は ≥0.8) は意図の札 (夜色の不透明の札) の後ろを通る = art-bible §2-2「意図の札の箱に幹を掛けない」の例外 (統合の裁定待ち)。'
          '160 のボスの箱 (x1200〜1550・行 0〜140) は霧 R ≥0.8 の木だけ。主人公の後ろの窓 (x300〜560 ±18px) と 1〜2体の足元 ±130px の太い幹 (見かけ ≥40px) は今までどおり 0。'
          'R2 上の覆い: 近い木の段 3 = 密な針葉樹を 近い幹 (左・右) と帯の手前の木 band-tree-2 の幹のすぐ奥 (s +0.7〜1.6) の同じ画面の x に置き、'
          'いちばん下の段が 行 272 (左)・232 (右)・214 (真ん中) に来る高さへ持ち上げる (abs。密な針葉樹の幹 47〜59px は手前の幹 99〜146px の後ろに隠れる・'
          'PC・PCU・21:9・スマホで幹のずれ 2〜12px を確かめた)。立てたままだと葉が高さの 10% から始まり、霧の帯と主人公の後ろの窓・右の意図の札を三角の段で塞ぐため。'
          '垂れる枝 2 (霧 R 0.77〜0.85 の高い枝・画面で幅 305〜320px・帯の木の奥の幹から右へ)。二周目からの大きな垂れる枝 8 (画面で幅 400〜500px = 椰子の葉に見えた) は廃止。'
          '右上の月光の筋 2 本を外す (moon-shaft-1 は gain 0.385 = 二周目の明るさのまま)。'
          'R4 地面と土手: 茂みの塊 3 段 (s 5.1〜6.0／7.8〜8.9／9.3〜10.6・低く途切れ途切れ = 塊の間に 40〜300px の空き・l は 3 個まで・2 段目は 1 段目の真後ろに置かない・'
          'tint [0.95,0.97,1.0] (直しの輪1。旧 [0.7,0.78,0.9]))・塊の間に土手の株 (立った株 16〜20 ドット)・羊歯の一列 (bank・bank-top) と二周目の段1 の上の縁の株 26 を外す・段1 の chamfer 0.2・'
          '段1 の前の縁の株 40・座席の帯と空き地の縁の株 40 (s −4.2〜4.3。真ん中 ±200px は土)・座席の帯の前の縁 (空き地の手前の縁) の株 26 '
          '(縁の上に小さな株 litterTuft・背丈 12 ドットの株は縁のすぐ手前の草の側 = 株の先は足の行 +20 より下)。1〜4体・主人公・人形 9 体の全部の座席の足元の通り '
          '(足元の x ±140/60/50px で根元が足元の 8px 上より手前・先が足元の 20px 下より上) に株・茂みを置かない。座席の土のタイル top_path_seat_r3_* を候補の先頭へ。'
          'R6 手前: 額縁の草 10 (fore_grass_*・深さ 10.5〜14・scale 0.5〜0.62・tint 0.75 (直しの輪1。旧 0.5)・矩形の上端は行 770 以下 = 名前の帯の下。'
          '左 4・右 3 は手札の外 (L8 ≤0.25)、真ん中 3 (r3-fore-8〜10) は "onlyWith": "uitrial" = 旗 uitrial=1 (手札を沈めた R3 の試し撮り) の時だけ組む。スマホは左右の 4 だけ)。'
          'frame-left-mid は名前の帯の上へ (vy 0.6・スマホ 0.71)。R8 霧: 加算の霧の面 4 枚を外し、α合成の霧の板 (kind mist) 2 枚 (s 9・s 14.5。スマホは手前の 1 枚)。'
          '揺れ: 房・草・茂み・垂れる枝 (swayFrom left) の部品に sway・surfaces.relief.sway・法線 surfaces.relief.normal 0.6。'
          'スマホ: 新しい半立体でスマホの画に 1000px 未満しか映らない物・s 20 より奥の奥の木・茂みの 3 段目・敵の側の細い幹・最奥の列は phone.hide (右上の近い木の段 canopy-r は直しの輪1 で phone.t 15.5)。'
          '門は頂の gates (部品の数の 0.9 倍〜+80・小札 250)。'
          '段2 C3 (2026-10-02・試しの撮影 2 回目の所見): ①帯の真ん中の太い幹を細く = band-tree-2 w32→w20 (霧 R 0.50・幹 103→63px・画面の中ほどで約 55px)・'
          'band-tree-5 霧 R 0.56→0.62 (61→59px)・band-tree-8 x1000→965 w32→w20 (band-tree-5 との隙間 15→約 50px)・帯の木の真後ろの奥の木 far-tree-1〜4 を w20 にして'
          '手前の帯の木の幹の x へ (霧 end 40 で両側に淡い幹がはみ出して二重の太い柱になっていた)。canopy-c は band-tree-2 (w20) の幹の奥 s +1.6 のまま'
          ' ②座席の帯の段の前の縁 s −3.52 (直線) → −5.0 (ゆるい波 ±0.18)・mottle (front-step と同じ種 536)。土と草の境目は段の直線でなく空き地の値ノイズの縁'
          ' (敵の側で s −3.6〜−4.2 まで土が出る) になる。継ぎ目 (0.06 の段) は草の中。座席の帯の前の縁の株 26→52 (縁は dirt_front_s = Diorama.ClearingMask の写し)'
          ' ③茂みの段 a・b の l (46 ドットの尖った峰) は m 2 つに'
          ' ④前の段 (front-step) の天面 −0.06→−0.01 = 座席の帯の前の縁の段 (面取りの 45° の帯 = 焦点で約 6px の明るい横線・試しの撮影 2 回目の土の縁の筋) を 1px 未満に'
          ' (前の段は座席の帯の下 s −5.18〜−3.2 と 0.01 の差で重なる)・株の足元の通りの検査に 21:9 も'
          ' ⑤奥の木 far-tree-1〜4 は w20 のまま (密な針葉樹に替えると上の真ん中の覆いは戻るが模型の E2・M2 が減る = gen_r3.py の注)。'
          '直しの輪1 (2026-10-02・反証のまとめ (b) 3・4・5・8。値は gen_r3.py の頂の定数): ①茂みの塊 r3-bush-* の tint [0.7,0.78,0.9]→[0.95,0.97,1.0] '
          '(霧の帯の下の黒い溝を明るい土手に。(b)2 の紺と同じ輪)・茸 shroom2 (t 4.12 s 8.941)・shroom (t 5.508 s 11.092) に tint 0.55 '
          '②額縁の草 r3-fore-1〜10 の tint 0.5→0.75 (墨の爪を暗い草の影絵に)・左端の羊歯 wide-edge fern2 (t −10.141 s −4.6) に tint 0.45・'
          'grove-foot fern (t −7.478 s 3.35) に tint 0.75・中央下の岩 (s −7.6) を t −3.298→−9・前の段 front-step に tint 0.7 '
          '(Diorama: slab も部品の tint を頂点色に掛ける。暗い画素 G3 > 0.66 になったら front-step の tint を最初に外す = FRONT_STEP_TINT None) '
          '③上の覆い canopy-l・canopy-r の tint 0.75→0.92 (段の縁の切り絵をやわらげる) ④canopy-r をスマホでも組む = phone.hide → phone.t 15.5 '
          '(近い幹 (右) はスマホで外したまま・段の下の短い幹は帯の木 band-tree-6・7 の幹の前。窓と意図の札に掛からない時だけ)。'
          '二周目から来る部品 (茸・羊歯・岩・前の段) の書き換えは写しでなく gen_r3.py の R2_RETOUCH と build_r3 で掛ける (写し act1_layout_r2.json は二周目の画のまま)。】')


DEFAULT_OUT = os.path.join(tempfile.gettempdir(), 'hd2d-r3', 'layout-cand.json')   # 試しの書き出し先 (リポジトリの外)。--repo で act1_layout.json へ


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--out', default=DEFAULT_OUT)
    ap.add_argument('--repo', action='store_true')
    args = ap.parse_args()
    L, log = build_r3(load_r2())
    L['_doc'] = L['_doc'] + DOC_R3
    out = STAGE + 'act1_layout.json' if args.repo else args.out
    os.makedirs(os.path.dirname(out), exist_ok=True)
    open(out, 'w', encoding='utf-8').write(dump_layout(L))
    print('\n'.join(log))
    print(out)


if __name__ == '__main__':
    main()

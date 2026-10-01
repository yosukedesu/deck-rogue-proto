#!/usr/bin/env python3
"""レーン C 段2: 既定の設計図 act1_layout.json を作る (計画 docs/design/hd2d-round2-plan-2026-10-01.md §2 レーン C 段2 の 3〜14 と、
統合担当の試しのビルドの決定 forLanes.C の (1)〜(9))。

元 = 2回目の試しの設計図 act1_layout_r2t2.json (段2 の段と霧の面が撮影で確かめ済み)。そこから:
  段 (K3)・見えない/規則に外れる部品を外す・新しい部品 (幹の3つの帯・針葉の枝・上の角と手前の額縁・土手の草・道標・株・岩)・
  月光の筋・新しい source・座席の土のタイルの名前・rock の面。
配置の計算は place.py (同じ作業場)。乱数は固定の種 (決定的)。

使い方: gen_layout.py [--out <path>] [--prune]   (既定は scratch の s2/cand.json。--repo で act1_layout.json へ)
       gen_layout.py --r3 [--out <path>|--repo]  三周目 (2026-10-01): 二周目の最終の写し act1_layout_r2.json から三周目の設計図を焼く
         (作り方は gen_r3.py・検査は place.py <layout> --r3 [--img])。二周目の build() はこの写しを作った物 (act1_layout_r2t2.json から)
"""
import argparse
import copy
import json
import math
import os
import random
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import place  # noqa: E402
from place import Ground, RES, REPO  # noqa: E402
from layoutio import dump_layout  # noqa: E402

STAGE = RES + 'Stage/'
SEED0 = 20261001
FIX1_FRONT_STEP_TOP = -0.06   # 直しの輪1 (2026-10-01・統合の直し): 座席の帯の前の段の高さ
FIX1_BOUGH_TINT_ADD = 0.15    # 直しの輪1: 針葉の枝の tint に足す量 (0.42〜0.58 → 0.57〜0.73。1回目の直しで PC の門④ 38〜40・N6a 0.27)
FIX2_GROVE = {3: (-9.6, False), 1: (-8.15, False)}  # 直しの輪2: 左の近い木立の長い幹 (幹の番号: (t, flip))。幹1 は左右反転を外すと幹の本体が約 47px 右へ寄るので、右の縁 ≈180px は t −8.15 (1回目のビルドの −7.4 は右の縁 258px)
FIX2_MIST_LIP = {'gain': 0.35, 'y': 0.0, 'v0': 0.45}  # 直しの輪2: 霧の面 mist-lip (下の縁を段1 の前の面の後ろへ隠す。v0 は mist-t2・t3 と同じ)
FIX2_VINES_Y = (6.65, 4.5)    # 直しの輪2: 蔦の額縁 crown-vines の y (PC・スマホ) = 画面の外へ (先だけが上の縁から「?」のようにのぞいていた)


# ------------------------------------------------------------------ 共通の道具

PC, PH, PC21 = place.cams(extra_wide=True)


def smooth(a, b, x):
    if x <= a:
        return 0.0
    if x >= b:
        return 1.0
    u = (x - a) / (b - a)
    return u * u * (3 - 2 * u)


def solve_t(G, cam, s, x_target, y_rel=0.0, lo=-40.0, hi=50.0):
    """画面の x (その s の地面 + y_rel の高さ) が x_target になる道の t"""
    for _ in range(70):
        m = (lo + hi) / 2
        px = cam.project(G.on_path(m, s, G.gy(m, s) + y_rel))[0]
        if px < x_target:
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


def find(parts, pred):
    return [p for p in parts if pred(p)]


# ------------------------------------------------------------------ 本体

def build():
    L = json.load(open(STAGE + 'act1_layout_r2t2.json'))
    w5 = json.load(open(STAGE + 'act1_layout_w5.json'))
    seed = Seeds(SEED0)
    parts = L['parts']
    log = []

    # ---------------- 段 (K3。forLanes.C (1)・(7))
    for p in parts:
        if p['kind'] != 'slab':
            continue
        n = p['name']
        if n == 'front-step':
            p['top'] = FIX1_FRONT_STEP_TOP          # 計画 10: −0.6 → −0.3。直しの輪1: → −0.06 (座席の帯の前の縁の段 34px → 約7px)
            p['mottle'] = {'scale': [2, 5], 'amount': 0.1}
        elif n == 'seat-band':
            p['maskEdge'] = [0.94, 1.02]            # 計画 13 (空き地の縁のゴマ塩を細く)
            assert p['back'] == 9.2
            # 直しの輪1 (反証 high「座席の手前の石の縁石の一本線」・N18): 面取り 0.12→0.3・前の縁の s の段 (−3.52/−3.84/−3.2 の飛び。x1270 の折れ) をならす・
            # 土の明るい点 (空き地の縁の混ざる帯) を細く: grid・gridT 0.64→0.32 (レーン D の見込み −62%)・空き地の前の縁を s ≈−3.6 へ (箱 N18 の外。後ろの縁は s 3.6→3.3)
            p['chamfer'] = 0.3
            p['grid'] = 0.32
            p['gridT'] = 0.32
            p['maskCenter'] = [3.0, -0.3]
            p['maskRadius'] = [14.5, 3.6]
            fr = p['front']
            ss = [q[1] for q in fr]
            sm = []
            for i in range(len(ss)):
                acc = wsum = 0.0
                for k in range(-4, 5):
                    j = min(len(ss) - 1, max(0, i + k))
                    w_ = math.exp(-0.5 * (k / 2.0) ** 2)
                    acc += w_ * ss[j]; wsum += w_
                sm.append(round(acc / wsum, 3))
            p['front'] = [[q[0], v] for q, v in zip(fr, sm)]
        elif n == 'terrace-1':
            p['surface'] = 'terrain'                # 石の縁の横一文字 → 草の土手
            p['chamfer'] = 0.36
            assert (p['top'], p['bottom'], p['back']) == (0.8, -0.25, 15.0)
            # 前の縁: t<−2 は s 9 (座席の地面を奥へ続ける)・それ以外は 6.0+1.3·sin(t/2.2) (r2t2 と同じ)。
            # r2t2 は t=−2 で s 9 → 5 に段で落ちていた (道と平行な土の壁) ので、t −3.6〜−1.6 だけ滑らかにつなぐ
            fr = []
            for t, s in p['front']:
                wave = 6.0 + 1.3 * math.sin(t / 2.2)
                k = smooth(-3.6, -1.6, t)
                fr.append([t, round(9.0 * (1 - k) + wave * k, 3)])
            p['front'] = fr
            p['mottle'] = {'scale': [2, 5], 'amount': 0.1}
            # 直しの輪1 (反証 medium「座席のすぐ奥の暗い石の板」・N8): 天面 0.8→0.5・面取り 0.36→0.6 (前の面を低く丸く。霧の面 mist-lip で覆う)
            p['top'] = 0.5
            p['chamfer'] = 0.6
        elif n == 'terrace-2':
            p['surface'] = 'terrain'
            p['chamfer'] = 0.6
            assert (p['top'], p['bottom']) == (1.4, 0.3)
            p['mottle'] = {'scale': [2, 5], 'amount': 0.1}
        elif n == 'terrace-3':
            p['surface'] = 'terrain'
            assert (p['top'], p['bottom'], p['back']) == (2.0, 0.6, 70.0)
    G = Ground(L)

    # ---------------- 外す部品
    def drop(pred, why):
        nonlocal parts
        keep, gone = [], []
        for p in parts:
            (gone if pred(p) else keep).append(p)
        parts = keep
        log.append('外す %-28s %3d  %s' % (why, len(gone), ', '.join(sorted({(q.get('src') or q.get('name') or q['kind']) for q in gone}))[:160]))
        return gone

    src = lambda p: p.get('src') or ''
    near = lambda p, t, s, e=0.02: abs(p.get('t', 1e9) - t) < e and abs(p.get('s', 1e9) - s) < e
    drop(lambda p: p['kind'] == 'tree', '丸い樹冠の木 (kind tree 7本)')
    drop(lambda p: p['kind'] == 'relief' and src(p) in ('canopy1', 'canopy2', 'canopy3', 'leaf4', 'leaf5'), '黄緑の樹冠と小さな木 (canopy・leaf)')
    drop(lambda p: p['kind'] == 'relief' and src(p) in ('pine', 'pine2', 'treeOak2', 'treeOak', 'treeGiant'), '奥の丸い木と松の全身')
    drop(lambda p: p['kind'] == 'relief' and src(p).startswith('trunkTall'), 'W3b の高い幹 (新しい幹に置き換え)')
    drop(lambda p: p['kind'] == 'relief' and src(p) == 'treeline', '並木2 (決めた霧 end 28 で深さ 56〜64 = 霧 1.0 で見えない・右は意図の札と足元)')
    drop(lambda p: p['kind'] == 'relief' and src(p) in ('rockBig1', 'rockBig2'), '苔の大岩 (手前・座席の奥。左の手前へ置き直す)')
    drop(lambda p: p['kind'] in ('block', 'marker', 'fence'), '石の塊9・道標2 (置き直す)・柵3')
    drop(lambda p: p['kind'] == 'card' and src(p) == 'sprout', '芽 (計画 11)')
    drop(lambda p: p['kind'] == 'card' and src(p) == 'shroom2' and p['s'] < -3.0, '手前のきのこ shroom2')
    # 手前の羊歯 2つ (足の行 +20 より上に先が出る)・座席の奥の羊歯と茂み (主人公の後ろの窓・意図の札・中央の列)
    drop(lambda p: p['kind'] == 'relief' and src(p) in ('fern', 'fern2', 'bush') and (near(p, 2.031, -8.765) or near(p, -2.091, -8.831) or near(p, -0.408, 9.881)
                                                                                     or near(p, 15.131, 6.732) or near(p, 11.566, 8.219) or near(p, 0.149, 16.491)
                                                                                     or near(p, -8.593, 27.122)), '手前と座席の奥の羊歯・茂み 7')
    # コードの岩: 残すのは画面に映る低い物だけ (座席の奥 s3〜10 の岩は外す = 見える岩は大岩2 + 石の塊1 の3つ)
    ROCK_KEEP = [(-11.881, -0.978), (10.07, -6.68), (7.023, -6.667), (19.158, 3.419), (-12.4, 16.198)]
    drop(lambda p: p['kind'] == 'rock' and not any(near(p, t, s) for t, s in ROCK_KEEP), 'コードの岩 (残すのは 5つ)')
    for p in parts:
        if p['kind'] == 'rock':
            p['h'] = round(min(p['h'], 0.6 * p['r']), 3)   # 計画 9: 低く (h/r ≤0.6)
    # 座席の奥の土手 (s 3〜10) で、主人公と敵の後ろ (PC x 560〜1700) の小石の札と、1〜2体の敵の後ろ (x 1137〜1652) のきのこを外す
    # (計画 8「キャラの後ろを草の土手に」・白い狼の後ろを静かに)
    xpc = lambda p: PC.project(G.on_path(p['t'], p['s'], G.gy(p['t'], p['s'])))[0]
    drop(lambda p: p['kind'] == 'card' and src(p) in ('pebble', 'rockSmall') and 3.0 <= p['s'] <= 10.0 and 560 <= xpc(p) <= 1700, '土手の小石の札 (キャラの後ろ)')
    drop(lambda p: p['kind'] == 'card' and src(p) in ('shroom', 'shroom2', 'flower') and p['s'] > 3.0 and 1137 <= xpc(p) <= 1652, '1〜2体の敵の後ろのきのこと花')
    # 直しの輪1 (反証 medium「床に貼ったシール」): 座席の帯の中 (s −3〜3) の W5 の草の小札も外す (株は縁に塊で置き直す)
    drop(lambda p: p['kind'] == 'litter' and src(p).startswith('litterTuft') and -3.0 <= p['s'] <= 3.0, '座席の帯の中の草の小札 (直しの輪1)')
    # 直しの輪1 (反証 medium・N8): 段1 の前の暗い面の手前の明るい青いきのこと花 (s 3〜10) を s +2 奥へ = 霧に沈める
    for p in parts:
        if p['kind'] == 'card' and src(p) in ('shroom', 'shroom2', 'flower') and 3.0 < p['s'] < 10.0:
            p['s'] = round(p['s'] + 2.0, 3)

    # ---------------- sources (計画 14。ファイルが無くても落ちない並べ方 = 先に見つかった絵)
    S = L['sources']
    R = 'Art/stage/act1/relief/'
    for i, (w, fb) in enumerate(((1, 1), (2, 2), (3, 3)), start=1):
        S['trunkLong%d' % i] = {
            '_doc': '二周目 (計画 §2 レーン C 段2 の5・K4・K11): 画面の上を突き抜ける長い幹 (レーン D のコード生成・幹の幅 40〜56・高さ 280 ドット = 11.2 unit)。'
                    '左の近い木立と右端と霧の帯。無ければ W3b の高い幹 (200 ドット)',
            'art': [R + 'trunk_long_%d' % i, R + 'trunk_tall_%d' % fb], 'depth': 0.1, 'cells': 48}
    for i, fb in enumerate((2, 1, 2), start=1):
        S['trunkThin%d' % i] = {
            '_doc': '二周目: 霧の帯の細い幹 (レーン D・幹の幅 16〜24・高さ 280 ドット)。無ければ W3b の高い幹',
            'art': [R + 'trunk_thin_%d' % i, R + 'trunk_tall_%d' % fb], 'depth': 0.08, 'cells': 48}
    for i in range(1, 5):
        S['needleBough%d' % i] = {
            '_doc': '二周目 (K4): 上の真ん中の「霧に透けた高い枝」= 針葉の枝の影絵 (レーン D・80〜120×40〜60・暗い紺緑)。無ければ組まない',
            'art': [R + 'needle_bough_%d' % i], 'depth': 0.05, 'cells': 40}
    for i, lt in enumerate((1, 2, 4, 3), start=1):
        S['tuftStand%d' % i] = {
            '_doc': '二周目 (計画 10・11・K12): 立った草の株 (レーン D・背丈 12・12・16・20 ドット。1・2 は背丈 12 = 手前と座席の帯の縁の litter)。無ければ W3b の小札の草',
            'art': [R + 'tuft_stand_%d' % i, 'Art/stage/act1/litter/tuft_%d' % lt], 'depth': 0.0, 'cells': 8, 'flat': True}
    S['canopyHang'] = {
        '_doc': '二周目 (K4・計画 5): 上の角の垂れた葉 (Art/props/act1_canopy を色表の紺緑へ写した物 = レーン D の act1_canopy_navy)',
        'art': [R + 'act1_canopy_navy', 'Art/props/act1_canopy'], 'depth': 0.1, 'cells': 40}
    S['waystone'] = {
        '_doc': '二周目 (計画 9): 道標 (箱の marker → 半立体。レーン D が色表へ写した act1_waystone)',
        'art': [R + 'act1_waystone', 'Art/props/act1_waystone'], 'depth': 0.18, 'cells': 36}
    S['fernSmall'] = {
        '_doc': '二周目 (計画 8): 段1 の土手の上の小さな羊歯 (act1_fern 34×29 ドット)。fern (fern_2 52×45) より小さい',
        'art': [R + 'act1_fern', 'Art/props/act1_fern'], 'depth': 0.1, 'cells': 32}
    S['giantCut'] = {
        '_doc': '二周目 (計画 5・14): 大樹の足元の土の皿と石を切った写し (レーン D)。使う時は画面の外に樹冠が出る左端だけ (今は置いていない)',
        'art': [R + 'act1_tree_giant_cut'], 'depth': 0.1, 'cells': 48}
    for k, a in (('pine', 'act1_tree_pine'), ('tallgrass', 'act1_tallgrass1'), ('stump', 'act1_stump')):
        arts = S[k]['art'] if isinstance(S[k]['art'], list) else [S[k]['art']]
        if R + a not in arts:
            S[k]['art'] = [R + a] + arts   # レーン D が色表へ写した物を先に拾う
    # 座席の土のタイル (forLanes.C (9)。レーン D の top_path_seat_r2_*。W5 の写しは今の名前のまま)
    ps = L['tiles']['pathSeat']
    for v, lst in zip('abcd', ps['variants']):
        name = 'Art/stage/act1/tiles/top_path_seat_r2_%s' % v
        if name not in lst:
            lst.insert(0, name)
    # rock の面 (計画 9)
    L['surfaces']['rock']['topThreshold'] = 0.55
    L['surfaces']['rock']['tint'] = [0.55, 0.6, 0.62]

    new = []

    def add(p, why):
        p.setdefault('seed', seed())
        new.append((p, why))
        return p

    # ---------------- 左の近い木立 (s 3.4〜5.0。計画 5・K11): 長い幹2本 (x 0〜250・影あり)・松 (右の枝 x ≤290)
    # 直しの輪1 (反証 medium「左の 1/6 がほぼ真っ黒の壁」・N7 端の幹): tint 0.35→0.5
    # 直しの輪2 (反証 high「左の木立が真っ黒の壁・右の縁が段々の線・左右反転の小枝がひっかき傷」): 幹3 を t −8.513→−9.6 (ほぼ画面の外)・
    # 幹1 を左右反転しない (小枝が画面の外の左へ)・t −6.914→−7.4 (右の縁を約 180px へ。そこから x 300 まで霧の隙間)
    for x, s_, v, fl, yaw, tint in ((36, 4.3, 3, False, -4, 0.5), (205, 3.55, 1, True, 3, 0.5)):
        t = solve_t(G, PC, s_, x)
        if v in FIX2_GROVE:
            t, fl = FIX2_GROVE[v]
        add({'kind': 'relief', 'name': 'grove-trunk-%d' % v, 'src': 'trunkLong%d' % v, 't': t, 's': s_, 'yaw': yaw, 'flip': fl, 'shadow': 1, 'tint': tint},
            '左の近い木立の長い幹')
    t = solve_t(G, PC, 4.9, -95)
    add({'kind': 'relief', 'name': 'grove-pine', 'src': 'pine', 't': t, 's': 4.9, 'yaw': -6, 'flip': False, 'shadow': 1, 'tint': 0.3}, '左の近い木立の松')

    # ---------------- 苔の大岩 (計画 9): 左の手前 2つ・半分埋め・左右反転
    for x, s_, sr, y, fl in ((-60, 3.7, 'rockBig1', -0.7, True), (118, 4.45, 'rockBig2', -0.55, True)):
        t = solve_t(G, PC, s_, x)
        add({'kind': 'relief', 'name': 'grove-rock', 'src': sr, 't': t, 's': s_, 'y': y, 'yaw': 6, 'flip': fl, 'shadow': 1}, '左の手前の大岩')

    # 左の木立と右端の根元: 羊歯と倒木 (暗い縁取り。主人公の後ろの窓の外)
    for x, s_, sr, fl in ((150, 3.35, 'fern', False), (-40, 4.9, 'fern2', True), (1905, 3.6, 'fern', True), (2010, 4.8, 'fern2', False)):
        t = solve_t(G, PC, s_, x)
        add({'kind': 'relief', 'name': 'grove-foot', 'src': sr, 't': t, 's': s_, 'yaw': round(random.Random(int(x) + 5).uniform(-10, 10), 1), 'flip': fl, 'shadow': 1},
            '木立の根元の羊歯')
    # 21:9 の左右の余白 (PC 16:9 とスマホの外 = x −320〜0・1920〜2240): 羊歯・茂み・背の高い草・低い石 (21:9 の画面の端を裸にしない)
    for x, s_, sr in ((-250, 3.9, 'bush'), (-120, -4.6, 'fern2'), (-300, 6.3, 'tallgrass'), (2010, -4.1, 'fern'), (2150, 3.6, 'bush'), (2090, 6.4, 'tallgrass')):
        t = solve_t(G, PC, s_, x)
        add({'kind': 'relief', 'name': 'wide-edge', 'src': sr, 't': t, 's': s_, 'yaw': round(random.Random(int(x) + 9).uniform(-12, 12), 1), 'flip': x > 960, 'shadow': 1},
            '21:9 の端の草')
    for x, s_, r_ in ((-280, -7.2, 0.5), (2200, -6.8, 0.44)):
        t = solve_t(G, PC, s_, x)
        add({'kind': 'rock', 't': t, 's': s_, 'yaw': float((x * 13) % 360), 'r': r_, 'h': round(0.5 * r_, 3), 'sides': 7, 'squash': 0.85}, '21:9 の端の低い石')
    t = solve_t(G, PC, 3.3, 60)
    add({'kind': 'relief', 'name': 'grove-log', 'src': 'log', 't': t, 's': 3.3, 'y': -0.15, 'yaw': 8, 'flip': True, 'shadow': 1, 'tint': 0.6}, '木立の倒木')

    # ---------------- 霧の帯の幹 (s 5.5〜9.6。影なし・tint 0.5〜0.6)。x は主人公と敵の間 (意図の札の箱 x≥1081 には掛けない)
    BAND = ((612, 7.3, 'trunkThin3', True, 0.52), (724, 8.9, 'trunkThin2', False, 0.58), (846, 7.8, 'trunkThin1', True, 0.55),
            (952, 9.35, 'trunkLong2', False, 0.6), (1040, 8.4, 'trunkThin2', True, 0.5))
    for x, s_, sr, fl, tint in BAND:
        t = solve_t(G, PC, s_, x)
        add({'kind': 'relief', 'name': 'band-trunk', 'src': sr, 't': t, 's': s_, 'yaw': round(random.Random(int(x)).uniform(-6, 6), 1), 'flip': fl, 'shadow': 0, 'tint': tint},
            '霧の帯の幹')
    # 霧に沈んだ奥の幹 (K4: s12〜25)
    for x, s_, sr, fl, tint in ((790, 13.4, 'trunkThin1', False, 0.6), (905, 15.2, 'trunkThin3', False, 0.62)):
        t = solve_t(G, PC, s_, x)
        add({'kind': 'relief', 'name': 'far-trunk', 'src': sr, 't': t, 's': s_, 'yaw': 0, 'flip': fl, 'shadow': 0, 'tint': tint}, '霧に沈んだ奥の幹')
    # 右端 (座席の帯の外・画面の x ≥1860。スマホは外す)
    t = solve_t(G, PC, 4.6, 1925)
    add({'kind': 'relief', 'name': 'edge-trunk-r', 'src': 'trunkLong1', 't': t, 's': 4.6, 'yaw': -3, 'flip': True, 'shadow': 1, 'tint': 0.35,
         'phone': {'hide': True}}, '右端の長い幹 (スマホは外す)')

    # ---------------- 上の真ん中の針葉の枝 (s 12〜16・高い所・霧に沈める)。abs = 絶対の高さ (枝は空中)
    BOUGH = ((770, 13.0, 6.4, 1, False, 0.5), (1000, 14.5, 7.3, 2, True, 0.55), (1150, 12.6, 5.9, 3, False, 0.48),
             (880, 16.0, 5.3, 4, True, 0.58), (1520, 13.8, 7.6, 1, True, 0.55), (1330, 12.8, 7.8, 3, True, 0.5), (1760, 12.2, 6.9, 2, False, 0.5),
             (560, 12.4, 7.4, 4, False, 0.52), (1290, 10.2, 7.2, 2, False, 0.45), (1560, 9.6, 6.6, 4, True, 0.45), (1820, 10.6, 7.4, 3, False, 0.42))
    for x, s_, yabs, v, fl, tint in BOUGH:
        tint = round(min(1.0, tint + FIX1_BOUGH_TINT_ADD), 3)   # 直しの輪1: 上の真ん中 (N6a・門④) を明るく = 霧に透けた枝
        t = solve_t(G, PC, s_, x, y_rel=yabs - 1.0)
        add({'kind': 'relief', 'name': 'bough', 'src': 'needleBough%d' % v, 't': t, 's': s_, 'y': yabs, 'abs': True, 'yaw': 0, 'flip': fl, 'shadow': 0, 'tint': tint},
            '針葉の枝の影絵')

    # ---------------- 道標と石の塊 (右・3〜4体の座席の後ろ・段1 の土手の足元)
    t = solve_t(G, PC, 4.5, 1792)
    add({'kind': 'relief', 'name': 'waystone', 'src': 'waystone', 't': t, 's': 4.5, 'yaw': -8, 'flip': False, 'shadow': 1}, '道標 (半立体)')
    t = solve_t(G, PC, 5.1, 1712)
    add({'kind': 'block', 't': t, 's': 5.1, 'y': 0.0, 'yaw': 10.0, 'w': 1.3, 'd': 1.0, 'h': 0.72, 'cut': 0.32, 'chamfer': 0.14, 'sink': 0.25}, '道標の横の石の塊')

    # ---------------- 土手の草 (段1 の足元 s 4.2〜5.4。主人公の後ろの窓・意図の札・中央の列を避けて少なめに)
    GRASS = ((700, 4.6, 'fernSmall', False), (822, 4.3, 'fernSmall', True), (1128, 4.9, 'tallgrass', False),
             (1592, 4.2, 'fern', False), (1660, 4.3, 'tallgrass', True))
    for x, s_, sr, fl in GRASS:
        t = solve_t(G, PC, s_, x)
        add({'kind': 'relief', 'name': 'bank', 'src': sr, 't': t, 's': s_, 'yaw': round(random.Random(int(x) * 3).uniform(-10, 10), 1), 'flip': fl, 'shadow': 1},
            '土手の草')

    # ---------------- 手前の低い石 (霧の手前・足の行より下)
    for x, s_, r_, h_ in ((150, -6.4, 0.46, 0.24), (930, -7.6, 0.38, 0.2), (1880, -6.9, 0.52, 0.28), (-220, -4.6, 0.55, 0.3), (2130, -4.2, 0.48, 0.26)):
        t = solve_t(G, PC, s_, x)
        add({'kind': 'rock', 't': t, 's': s_, 'yaw': float((x * 37) % 360), 'r': r_, 'h': h_, 'sides': 7, 'squash': 0.85}, '手前の低い石')

    # ---------------- 立った草の株 (計画 10・11。等間隔にしない = 間隔を1本ずつ乱数で・位置も揺らす)
    rng = random.Random(SEED0 + 11)
    placed_pts = []
    t1 = [p for p in parts if p['kind'] == 'slab' and p['name'] == 'terrace-1'][0]
    T1T = [q[0] for q in t1['front']]
    T1S = [q[1] for q in t1['front']]

    def t1_front(t):
        import numpy as np
        return float(np.interp(t, T1T, T1S))

    def xs_ok(t, s_, bans_pc, bans_ph):
        xp = PC.project(G.on_path(t, s_, G.gy(t, s_)))[0]
        xh = PH.project(G.on_path(t, s_, G.gy(t, s_)))[0]
        return not any(a_ <= xp <= b_ for a_, b_ in bans_pc) and not any(a_ <= xh <= b_ for a_, b_ in bans_ph)

    def scatter(n, s_fn, x_lo, x_hi, kinds, bans_pc=(), bans_ph=(), mind=(0.9, 2.6), label='株'):
        pts = []
        tries = 0
        while len(pts) < n and tries < 6000:
            tries += 1
            x = rng.uniform(x_lo, x_hi)
            s_guess = s_fn(None)
            t = solve_t(G, PC, s_guess, x)
            s_ = s_fn(t)
            if not xs_ok(t, s_, bans_pc, bans_ph):
                continue
            md = rng.uniform(*mind)
            if any(abs(t - q[0]) < md and abs(s_ - q[1]) < 0.7 for q in pts + placed_pts):
                continue
            pts.append((t, s_))
        placed_pts.extend(pts)
        for t, s_ in pts:
            sr = rng.choice(kinds)
            k = 'litter' if (sr in ('tuftStand1', 'tuftStand2') or sr.startswith('litter')) else 'card'
            add({'kind': k, 'src': sr, 't': round(t, 3), 's': round(s_, 3), 'yaw': round(rng.uniform(-12, 12), 1), 'flip': rng.random() < 0.5}, label)
        return pts
    U = lambda lo, hi: (lambda t: rng.uniform(lo, hi))
    HERO_PC, HERO_PH = (300, 575), (225, 440)                       # 主人公の後ろの窓 (+15px)
    FEET_PC, FEET_PH = ((263, 623), (1006, 1892)), ((255, 640), (1000, 1870))   # 手前の縁の株の先が足にかぶる列 (主人公 ±180・敵 ±120)
    # 手前の株 (K12・背丈 12 ドット): 1本ずつ撒かず 2〜4本の塊を不規則に (同じ粒が等間隔に並ぶのを避ける)
    for cx, cs, nn in ((-20, -10.2, 3), (330, -9.6, 2), (610, -7.4, 4), (905, -10.6, 2), (1180, -6.2, 3), (1395, -9.1, 4), (1640, -7.0, 2),
                       (1880, -10.0, 3), (140, -6.1, 2), (770, -8.8, 3), (1530, -10.4, 2)):
        tc = solve_t(G, PC, cs, cx)
        for m_ in range(nn):
            t_ = tc + rng.uniform(-0.55, 0.55)
            s_ = cs + rng.uniform(-0.45, 0.45)
            sr = rng.choice(('tuftStand1', 'tuftStand2'))
            add({'kind': 'litter', 'src': sr, 't': round(t_, 3), 's': round(s_, 3), 'yaw': round(rng.uniform(-12, 12), 1), 'flip': rng.random() < 0.5},
                '手前の株の塊 (K12)')
            placed_pts.append((t_, s_))
    # 直しの輪1 (反証 medium「床に貼ったシール」・high「縁石の一本線」): 座席の帯の株を約半分に減らし、縁に塊で寄せる (等間隔にしない)。
    # 手前の縁 (s −3.3〜−3.7 = ならした前の縁の上) に 2〜3本の塊を不規則に計約10本 (線を切る)・奥の縁は 18→9本を塊で・帯の中の10本は外す
    def clumps(n_total, s_lo, s_hi, x_lo, x_hi, kinds, bans_pc, bans_ph, label, gap=(140, 330)):
        x = x_lo + rng.uniform(0, gap[0])
        made = 0
        while made < n_total and x < x_hi:
            cs = rng.uniform(s_lo, s_hi)
            tc = solve_t(G, PC, cs, x)
            nn = min(n_total - made, rng.choice((2, 2, 3)))
            if xs_ok(tc, cs, bans_pc, bans_ph):
                for m_ in range(nn):
                    t_ = tc + rng.uniform(-0.45, 0.45)
                    s_ = min(s_hi, max(s_lo, cs + rng.uniform(-0.15, 0.15)))
                    if not xs_ok(t_, s_, bans_pc, bans_ph):
                        continue
                    sr = rng.choice(kinds)
                    k = 'litter' if (sr in ('tuftStand1', 'tuftStand2') or sr.startswith('litter')) else 'card'
                    add({'kind': k, 'src': sr, 't': round(t_, 3), 's': round(s_, 3), 'yaw': round(rng.uniform(-12, 12), 1), 'flip': rng.random() < 0.5}, label)
                    placed_pts.append((t_, s_))
                    made += 1
            x += rng.uniform(*gap)
    clumps(10, -3.7, -3.3, 20, 1900, ('tuftStand1', 'tuftStand2'), FEET_PC, FEET_PH, '空き地の手前の縁の株 (塊)', gap=(150, 360))
    clumps(9, 3.1, 3.8, -40, 1900, ('tuftStand1', 'tuftStand2', 'tuftStand3', 'tuftStand4'), (HERO_PC,), (HERO_PH,), '空き地の奥の縁の株 (塊)', gap=(220, 520))
    # 段1 の土手の上の縁 (草の土手に株を立てる)
    scatter(26, lambda t: (t1_front(t) + rng.uniform(0.35, 1.3)) if t is not None else 7.5, 560, 1900,
            ('tuftStand3', 'tuftStand4', 'tuftStand3', 'tuftStand1'), bans_pc=(HERO_PC,), bans_ph=(HERO_PH,), mind=(0.8, 2.4), label='土手の上の縁の株')
    # 段2 の前の縁の株 (霧に沈む・意図の札の列 x1081〜1819 は避ける)
    t2 = [p for p in parts if p['kind'] == 'slab' and p['name'] == 'terrace-2'][0]
    T2T = [q[0] for q in t2['front']]
    T2S = [q[1] for q in t2['front']]

    def t2_front(t):
        import numpy as np
        return float(np.interp(t, T2T, T2S))
    scatter(10, lambda t: (t2_front(t) + rng.uniform(0.3, 1.2)) if t is not None else 14.8, 560, 1990,
            ('tuftStand3', 'tuftStand4', 'tuftStand1'), bans_pc=(HERO_PC, (1060, 1840)), bans_ph=(HERO_PH, (1040, 1830)), mind=(0.9, 2.6), label='段2 の縁の株')
    # 土手の上の小さな羊歯 (段1 の天面の縁のすぐ奥)
    # 直しの輪1 (反証 medium): 段1 の上の縁の直線を切る小さな羊歯を3つ足す (x 665・720・905。主人公の後ろの窓 x ≤575 の外)
    for x, fl in ((640, True), (768, False), (1003, True), (1872, False), (1935, True), (665, False), (720, True), (905, False)):
        t = solve_t(G, PC, 7.0, x)
        s_ = round(t1_front(t) + 0.55, 3)
        t = solve_t(G, PC, s_, x)
        add({'kind': 'relief', 'name': 'bank-top', 'src': 'fernSmall', 't': t, 's': s_, 'yaw': round(rng.uniform(-10, 10), 1), 'flip': fl, 'shadow': 0}, '土手の上の小さな羊歯')
    # 座席の帯の奥の落ち葉と小枝 (暗い小札。明るい小石は足さない = N18)
    scatter(8, U(3.2, 5.6), 560, 1880, ('litterLeaves1', 'litterLeaves2', 'litterTwig1', 'tuftStand2'), bans_pc=(HERO_PC,), bans_ph=(HERO_PH,), mind=(1.0, 3.0),
            label='土手の足元の落ち葉')
    scatter(6, U(3.2, 4.4), -120, 280, ('tuftStand3', 'tuftStand4', 'tuftStand1'), bans_pc=(HERO_PC,), bans_ph=(HERO_PH,), mind=(0.6, 1.6), label='木立の根元の株')
    scatter(12, U(1.8, 2.6), 60, 1880, ('litterLeaves1', 'litterLeaves2', 'litterTwig1'), bans_pc=(HERO_PC,), bans_ph=(HERO_PH,), mind=(1.0, 3.0), label='座席の帯の奥の落ち葉')
    # 土手の足元のきのこ (霧の手前・主人公と敵の間)
    # 直しの輪1 (反証 medium・N8): 段1 の暗い面の前の明るい青いきのこ2つ (x 870・1015) は置かない

    # ---------------- 額縁 (計画 5・10): 上の角 (紺緑の垂れた葉・蔓)・手前の背の高い草
    fr = {p['name']: p for p in parts if p['kind'] == 'frame'}
    fl_, fr_ = fr['frame-low-left'], fr['frame-low-right']
    fl_.update({'src': 'tallgrass', 'vx': 0.085, 'vy': 0.12, 'depth': 6.5, 'scale': 0.4, 'flip': False, 'roll': 3.0, 'tint': 0.3})
    fr_.update({'src': 'tallgrass', 'vx': 0.925, 'vy': 0.13, 'depth': 6.8, 'scale': 0.42, 'flip': True, 'roll': -3.0, 'tint': 0.3, 'phone': {'hide': True}})
    # 上の角の垂れた葉と蔓 (計画 5・K4)。額縁 (kind frame) にすると上部バー (行0〜72) と重なる割合が L8 の門 30% を必ず超える
    # (下端を vy 0.83 = 行 184 より上に置くと、画面に写る高さ ≤184 のうち 72 が上部バー = 39%) ので、世界に掛けた半立体 (abs) にする。
    # 手前 (s −9) に置けば霧は 0・1ドット ≈5.8px (近いほど粗い = 自然)・幕ボスの寄りでも画面の角のまま。スマホは phone の t・y で角へ
    def solve_y(cam, t, s_, row_target):
        lo, hi = -5.0, 30.0
        for _ in range(60):
            m = (lo + hi) / 2
            if cam.project(G.on_path(t, s_, m))[1] > row_target:
                lo = m
            else:
                hi = m
        return round((lo + hi) / 2, 3)
    CROWNS = (('crown-left', 'canopyHang', -9.0, 20, 175, 25, 150, False, 0.4),
              ('crown-right', 'canopyHang', -9.0, 1900, 175, 1895, 150, True, 0.4),
              # 直しの輪1 (反証 medium「上の真ん中が暗すぎる」): 蔓は下の縁を行 95→35 (スマホ 70→25) へ上げる・真ん中に浮いた垂れ葉 crown-mid は外す
              ('crown-vines', 'frameVines', 3.0, 820, 35, 820, 25, False, 0.45),
              ('crown-left-wide', 'canopyHang', -9.0, -230, 175, None, None, True, 0.4),
              ('crown-right-wide', 'canopyHang', -9.0, 2150, 175, None, None, False, 0.4))
    for name, sr, s_, xpc_, rowpc, xph, rowph, fl, tint in CROWNS:
        t = solve_t(G, PC, s_, xpc_, y_rel=6.0)
        y = solve_y(PC, t, s_, rowpc)
        p = {'kind': 'relief', 'name': name, 'src': sr, 't': t, 's': s_, 'y': y, 'abs': True, 'yaw': 0, 'flip': fl, 'shadow': 0, 'tint': tint}
        if xph is not None:
            tph = solve_t(G, PH, s_, xph, y_rel=5.0)
            p['phone'] = {'t': tph, 'y': solve_y(PH, tph, s_, rowph)}
        else:
            p['phone'] = {'hide': True}
        if name == 'crown-vines':   # 直しの輪2: PC 6.226→6.65・スマホ 4.117→4.5 (画面の外へ。上の真ん中は針葉の枝に任せる)
            p['y'] = FIX2_VINES_Y[0]
            p['phone']['y'] = FIX2_VINES_Y[1]
        add(p, '上の角の垂れた葉 (世界に掛ける)')

    # ---------------- 霧の面 (直しの輪1・反証 high「霧の帯に頂点が無い」・medium「段1 の前の暗い面」)
    fogp = {p['name']: p for p in parts if p['kind'] == 'fog'}
    fogp['mist-t2']['h'] = 1.4                       # 2.2 → 1.4 (帯の下の台地を細く)
    fogp['mist-lip'].update({'gain': 0.5, 'y': 0.4})  # 0.3・0.6 → 0.5・0.4 (段1 の前の面を霧で覆う)
    # 直しの輪2 (反証 high「mist-lip の下の縁が画面の端から端まで真っ直ぐな横線 (行 537・段差 8.4)」): y 0.4→0.0・v0 0.15→0.45・gain 0.5→0.35
    fogp['mist-lip'].update(FIX2_MIST_LIP)

    # ---------------- 月光の筋 (計画 7)
    sh = {p['name']: p for p in parts if p['kind'] == 'shaft'}
    sh['moon-shaft-0'].update({'top': 1.4, 'bottom': 3.6, 'gain': 1.1})
    sh['moon-shaft-1'].update({'gain': 0.5})
    # 右の2本は縦に近い向き。足は霧の帯の上 (意図の札の行より上で終える)
    for name, x, row_foot, s_, gain, bottom in (('moon-shaft-2', 1176, 292, 6.4, 1.1, 2.8), ('moon-shaft-3', 1585, 250, 7.0, 0.9, 2.4)):
        p = sh[name]
        d = [-0.08, -1.0, 0.30]
        dn = math.sqrt(sum(v * v for v in d))
        ytop = 9.2
        t = solve_t(G, PC, s_, x)

        def foot(t_, ln_):
            top = G.on_path(t_, s_, ytop)
            return PC.project(top + [v / dn * ln_ for v in d])
        ln = 4.0
        for _ in range(30):
            lo, hi = 0.5, 14.0            # 長さ: 足の行を row_foot に
            for _ in range(50):
                m = (lo + hi) / 2
                if foot(t, m)[1] < row_foot:
                    lo = m
                else:
                    hi = m
            ln = (lo + hi) / 2
            bx = foot(t, ln)[0]          # 出口の t: 足の x を x に
            t += (x - bx) / 110.0
        p.update({'t': round(t, 3), 's': s_, 'y': ytop, 'abs': True, 'top': 1.4, 'bottom': bottom, 'len': round(ln, 2), 'dir': d, 'gain': gain, 'lobe': 0.0})

    # ---------------- 並べ直し: 新しい部品は種類ごとの塊の後ろへ (書き戻しの行が読みやすいように)
    order = ['slab', 'rig', 'block', 'relief', 'rock', 'card', 'litter', 'fog', 'shaft', 'frame']
    all_parts = parts + [p for p, _ in new]
    all_parts.sort(key=lambda p: order.index(p['kind']) if p['kind'] in order else 99)   # sorted は安定 = 同じ種類の中の順は元のまま
    L['parts'] = all_parts
    for p, why in new:
        log.append('足す %-28s %s t%.2f s%.2f' % (why, p.get('src') or p['kind'], p.get('t', float('nan')) if 't' in p else float('nan'), p.get('s', float('nan')) if 's' in p else float('nan')))
    for p in all_parts:
        if p['kind'] in ('rock', 'block', 'fence', 'marker', 'tree', 'rig', 'card', 'relief', 'litter'):
            assert 'seed' in p, p
    return L, log


def phone_fix(L, log):
    """スマホの主人公の後ろの窓 (x230〜430・行230〜380) に左の近い木立が入るなら、スマホだけ t を左へ (レーン D の口 phone {t})"""
    G = Ground(L)
    rc = G.raycast(PH, 4)
    tdf = place.upsample(rc, PH.W, PH.H)
    win = (230, 230, 430, 380)
    for i, p in enumerate(L['parts']):
        if not (p.get('name') or '').startswith('grove-'):
            continue
        for _ in range(6):
            o = place.place_part(L, G, PH, i, p, with_image=True)
            vm, npx = place.visible_mask(o, PH, tdf, PH.W, PH.H)
            c = int(vm[win[1]:win[3], win[0]:win[2]].sum()) if vm is not None else 0
            if c == 0:
                break
            cols = vm[win[1]:win[3], win[0]:win[2]].any(axis=0)
            right = win[0] + int(max(i_ for i_, v in enumerate(cols) if v))
            dx = right - win[0] + 12
            ph = dict(p.get('phone') or {})
            t0 = ph.get('t', p['t'])
            cx = PH.project(G.on_path(t0, p['s'], G.gy(t0, p['s'])))[0]
            ph['t'] = solve_t(G, PH, p['s'], cx - dx)
            p['phone'] = ph
            log.append('スマホ: %s を t %.2f → %.2f (窓に %d px)' % (p['name'], p['t'], ph['t'], c))


def prune(L, log):
    """見えない・規則に外れる札 (card・litter) を外す。半立体と岩は数えて報告するだけ (手で直す)。
    外す: どの画面 (PC 16:9・PC 21:9・スマホ) にも映らない (画面の外か、決めた霧で 0.97 以上)／主人公の後ろの窓 (PC・スマホ)／
    意図の札の箱 (W5 の札を新しい足元へ・PC)／手前の株の先が足の行 +20 より上"""
    G = Ground(L)
    ctx = {}
    for cam in (PC, PH, PC21):
        rc = G.raycast(cam, 4)
        ctx[cam.name] = (rc, place.upsample(rc, cam.W, cam.H), place.seats(cam, G))
    iw = place.w5_intents()
    boxes = []
    st = ctx['PC'][2]
    for key, lst in iw.items():
        n = {'wolf': 1, 'ogre': 1, 'quad': 4, 'trio': 3}[key]
        for j, u in enumerate(lst):
            e = st['enemies'][n][j]
            dx, dy = e['x'] - u['feet'][0], e['y'] - u['feet'][1]
            ix, iy, iw_, ih = u['intent']
            boxes.append((int(ix + dx), int(iy + dy), int(ix + dx + iw_), int(iy + dy + ih)))
    fr, _ = place.FOG_VARIANTS['R']
    out, why = [], {}
    report = []
    for i, p in enumerate(L['parts']):
        k = p['kind']
        if k in ('slab', 'fog', 'shaft', 'frame', 'rig'):
            out.append(p)
            continue
        vis = False
        bad = None
        fogmin = 1.0
        for cam in (PC, PH, PC21):
            rc, tdf, sts = ctx[cam.name]
            o = place.place_part(L, G, cam, i, p, with_image=True)
            if o is None or not o.visible or not o.facing:
                continue
            vm, npx = place.visible_mask(o, cam, tdf, cam.W, cam.H)
            f = place.fog_frac(o.depth, *place.FOG_VARIANTS['R'], cam.r)
            if npx > 0 and f < 0.97:
                vis = True
            if npx > 0:
                fogmin = min(fogmin, f)
            if cam is PC21 or vm is None:
                continue
            win = (300, 380, 560, 560) if cam is PC else (230, 230, 430, 380)
            if int(vm[win[1]:win[3], win[0]:win[2]].sum()) > 0:
                bad = bad or '主人公の後ろの窓 %s' % cam.name
            if cam is PC:
                for b in boxes:
                    if int(vm[max(0, b[1]):b[3], max(0, b[0]):b[2]].sum()) > 0:
                        bad = bad or '意図の札'
            if p.get('s', 0) < -3.0 and o.box is not None and not p.get('abs'):   # 空中に掛けた物は下端が上 = 足を隠さない
                feet = [(sts['hero'][0], sts['hero'][1], 180)] + [(e['x'], e['y'], 120) for n in (1, 2, 3, 4) for e in sts['enemies'][n]]
                x0, y0, x1, y1 = o.box
                for fx, fy, hw in feet:
                    if x1 >= fx - hw and x0 <= fx + hw and y0 <= fy + 20:
                        bad = bad or '手前の先が足にかぶる %s' % cam.name
        if not vis:
            bad = bad or '映らない'
        if k == 'card' and p.get('src') in ('flower', 'shroom', 'shroom2', 'pebble', 'rockSmall') and fogmin > 0.6 and not (p.get('name') or ''):
            bad = bad or '霧の奥の小札 (灰色の粒に見える)'
        if bad and (k in ('card', 'litter') or bad == '映らない'):   # 半立体・岩は「どこにも映らない」だけ自動で外す
            why[bad.split(' ')[0]] = why.get(bad.split(' ')[0], 0) + 1
            continue
        if bad:
            report.append('残した (手で直す): #%d %s %s t%.2f s%.2f — %s' % (i, k, p.get('src') or p.get('name') or '', p.get('t', 0), p.get('s', 0), bad))
        out.append(p)
    L['parts'] = out
    log.append('札を外す (見えない・規則): ' + '・'.join('%s %d' % kv for kv in why.items()))
    log.extend(report)


def w5_doc():
    txt = subprocess.check_output(['git', '-C', REPO, 'show', 'e30c510:unity/Assets/Resources/Stage/act1_layout.json']).decode()
    return json.loads(txt)['_doc']


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--out', default=None)
    ap.add_argument('--repo', action='store_true')
    ap.add_argument('--r3', action='store_true', help='三周目: act1_layout_r2.json (二周目の最終の写し) から三周目の設計図を焼く (gen_r3.py・種 20261003)')
    args = ap.parse_args()
    if args.r3:
        import gen_r3
        L, log = gen_r3.build_r3(gen_r3.load_r2())
        L['_doc'] = L['_doc'] + gen_r3.DOC_R3
        out = STAGE + 'act1_layout.json' if args.repo else (args.out or gen_r3.DEFAULT_OUT)
        os.makedirs(os.path.dirname(out), exist_ok=True)
        open(out, 'w', encoding='utf-8').write(dump_layout(L))
        print('\n'.join(log))
        print(out)
        return
    if args.out is None:
        args.out = os.path.join(HERE, 's2', 'layout-cand.json')
    L, log = build()
    phone_fix(L, log)
    prune(L, log)
    L['_doc'] = w5_doc() + DOC_R2
    out = STAGE + 'act1_layout.json' if args.repo else args.out
    os.makedirs(os.path.dirname(out), exist_ok=True)
    open(out, 'w', encoding='utf-8').write(dump_layout(L))
    print('\n'.join(log))
    from collections import Counter
    print('parts', len(L['parts']), dict(Counter(p['kind'] for p in L['parts'])))
    print(out)


DOC_R2 = ('【二周目 (計画 docs/design/hd2d-round2-plan-2026-10-01.md §2 レーン C 段2・2026-10-01)。W5 の設計図は act1_layout_w5.json (look_act1_w5 が指す)。'
          'カメラ 22°・見下ろし 5° (スマホ 7°) 用。段: 段1〜3 の天面 0.8/1.4/2.0 (目の高さ 3.42 より下)・草の土手 (surface terrain)・段1 の前の縁は t<−2 で s9・それ以外 6.0+1.3·sin(t/2.2)・'
          '段3 の奥 s70・座席の帯の奥 9.2。奥の端 = カメラに付く背景の板 (額縁 backdrop・深さ 140・flat)。霧の面: mist-t3 gain 0.25・vein-mist h5 gain 0.12・mist-t2 v0 0.45 gain 0.5・mist-lip。'
          '木 (K4・K11): 丸い樹冠の木と黄緑の樹冠は置かない。左の近い木立 (長い幹2・松・苔の大岩2)・霧の帯の幹 (主人公と敵の間 x 610〜1040・影なし・霧に沈める)・'
          '霧に沈んだ奥の幹2・右端の長い幹 (スマホは外す)・上の真ん中の針葉の枝・上の角の紺緑の垂れた葉と蔓 (額縁)。手前 (K12): 背の高い草は額縁 (左右の下の角・スマホは左だけ)、'
          '世界には背丈 12 ドット以下の株だけ。座席の奥: 道標 (半立体) と石の塊1・土手の草。月光の筋: 左上の2本は光の向きのまま、右の2本は縦 (足は意図の札の行より上)。'
          '配置の規則と計算は scratchpad/hd2d/r2/lane-C/place.py・作り方は gen_layout.py】'
          '【直しの輪1 (2026-10-01・統合): 座席の帯の前の段 −0.3→−0.06・帯の面取り 0.3・前の縁の s をならす・帯の格子 0.32・空き地の前の縁 s≈−3.6・'
          '段1 の天面 0.5・面取り 0.6・mist-t2 の h 1.4・mist-lip gain 0.5 y 0.4・左の木立の幹 tint 0.5・上の真ん中の垂れ葉を外し蔓を上へ・'
          '座席の帯の株を半分に減らして縁に塊で・段1 の前のきのこを外すか奥へ・段1 の上の縁に小さな羊歯3・針葉の枝の tint +0.15】'
          '【直しの輪2 (2026-10-01・統合): 左の近い木立の幹3 を t −9.6 (ほぼ画面の外)・幹1 を t −8.15 で左右反転しない (右の縁を x≈180 へ・小枝は画面の外の左へ)・'
          'mist-lip を y 0.0・v0 0.45・gain 0.35 (下の縁を段1 の前の面の後ろへ)・蔦の額縁 crown-vines を画面の外 (y PC 6.65・スマホ 4.5)】')


if __name__ == '__main__':
    main()

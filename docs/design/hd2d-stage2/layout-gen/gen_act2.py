#!/usr/bin/env python3
"""HD-2D 段2 レーン C: 幕2「先代の坑道」の箱庭の設計図 act2_layout*.json を焼く
(計画 docs/design/hd2d-stage2-plan-2026-10-02.md §2 C・約束 docs/design/hd2d-stage2/contracts.md C1・C3・C4・
分析 docs/design/hd2d-stage2-analysis-2026-10-02.md §4-1)。

カメラ・地面・部品の写し方は幕1 の docs/design/hd2d-slice/r2-layout-gen/place.py を import する (place.py は直さない)。
配置の規則の検査は同じフォルダの place2.py (この生成器も同じ規則で置き場を選ぶ = 焼いた後に place2.py で数えて違反 0 を確かめる)。
乱数は固定の種 SEED (決定的)。D の絵 (Art/stage/act2/) があればその大きさで、無ければ元の Art/props/act2_* の大きさで判断する。

出力 (unity/Assets/Resources/Stage/。--out-dir で変えられる)
  act2_layout.json        本番 (岩棚の張り出し 2 unit)
  act2_layout_wall3.json  張り出し 3 unit の変種 (look_act2_wall3 が指す)
  act2_layout_wall4.json  張り出し 4 unit の変種 (look_act2_wall4 が指す)
  act2_layout_min.json    最小の試し (岩棚つきの壁 1 式＋提灯 2 と暈＋座席の帯の slab と段だけ。小物・霧の板・額縁なし。gates は緩める)

層 (道の座標 t・s・y。分析 §4-1 の表。PC 22°・5°・足元 0.36 で行を出した)
  下の坑道  slab y −1.4 (s −30〜−5)。手前の板張り (ledge-) と横木 (暗く) で座席の帯の縁 s −5.0 を隠す
  座席の帯  slab y 0 (s −5.0〜5.0)。t −8.5〜13・s −2.6〜2.8 は高さ 0・部品なし (小札だけ)
  一段目    slab y +0.9 (s 5.0〜7.9)。前の縁に土留めの横木 (edge-) と杭 (stake-)。宿場 (炉・樽・屋台)
  二段目    slab y +1.8 (s 7.9〜)。レール (kind rail・abs = Diorama.Check の届きは線路の長さの半分を半径に読むので) が通る。岩棚の前 s 9.4 まで
  岩棚 (棚)  block abs y 1.8〜2.9 前 s 9.4 (区切りごとに 9.35〜9.6・高さ 2.75〜3.05)。張り出し = 本体の前の面 − 棚の前 (2/3/4)
  本体      block abs y 1.8〜4.4 前 s = 9.4 + 張り出し (本番 11.4)・奥行き 2
  上段      block abs y 4.4〜6.3 中心 = 本体の中心 − 0.4 (本番 12.0)。少しだけ張り出す (0.3 以下 = 底の面が無いので穴が見えない量)
  頂 (crown) block abs y 6.3〜11.5 (段2 で足した: 22°・5°・足元 0.36 では本体の上 6.3 が PC 行 107〜151 = 上部バー 72 の下に背景が出る)
  奥 (back)  block abs y 1.8〜13.8 前 s = 本体の前 + 8.6 (底の面が無い block の隙間から背景が見えない保険)
  坑口      棚の上の台 (wall-landing y 2.9〜3.6) に開く上の坑口 (柱 block×2・梁 block・黒い card・奥の灯の暈)。t 19.5・s = 坑口の幅と高さに掛かる
            壁の面 (上段の張り出しを含む wall_face) − 0.08 (二段目の高さに開くと、敵 3〜4 体の座席の通り (行 328〜) と 4 体目の意図の札 (行 328〜408)
            に掛かる = 規則 R2・R3)。スマホでは組まない (上部バーの裏) = 光 mouth-lamp もスマホでは消す (B が lights に phone.on false)
  灯        提灯 lantern-a (主人公の列 t −2.9)・lantern-b (坑口の脇 t 17.2・座席の通りの上)・lantern-c (真ん中 t 4.3・光の 4 つ目の枠の候補) と暈・
            炉 hearth (t −6.2) と暈・坑口の奥の灯 mouth-lamp (暈)。光そのものはレーン B が look_act2*.json の lights に at で書く。
            提灯の下端は下の支え (岩棚の区切りの天面・坑口の台) の 0.03 上 (rest_on)・壁の面の 0.12 手前。スマホは棚の前の二段目の床 (y 1.83) に置く
  支保工    柱 3 (左 t −8.2・真ん中 t 0 = 棚の前に寄せる・右 t ≥17 = 座席の通りの外)・梁 2 (左の 2 本の柱の頭を道に沿って結ぶ y 5.75・
            真ん中の柱から手前へ張り出す y 6.2 = 真ん中の柱はこの高さまで伸ばして載せる。どちらも座席の帯の上は通らない)・柱の頭の持ち送り (cap)。
            左の柱は計画の t −5.6 だと炉 (t −6.2) の手前に立つので t −8.2
  歩廊      左 (t −13〜−4.0) の棚の上 y 3.6 (床 plank・脚 wood・手すり fence)。スマホでは組まない
  手前      額縁 2 = 世界に置いた 3D (下の坑道の岩 2。左の支保工の柱は統合 2026-10-03 に外した = 画面の左端の黒い縦の帯に見えた)。
            幕2 に支保工と岩の額縁に使える絵が無い (D: post_brace は置かない・rubble_rock は捨てた) ので kind frame でなく rock で組む
  霧        壁の前の霧の板 1 (wall-mist・y 1.2〜3.9・s 9.0。統合の格子 k1 の値)・床の霧の板 1 (floor-mist・スマホでは組まない)
  小物      B の置き場 70 (StageActs.PaintMarket2 の t・s) を候補に、規則に掛かる物は t を動かすか外す (炉は t −6.2)。鍾乳石は t ≥16。
            棚の上の物の下端はその区切りの天面の 0.02 上・壁の前の物 (横坑の口) は壁の面の 0.06 手前で岩棚の天面の上。
            張り出しの変種 (wall3・wall4) は本番が置いた小物と同じ集合を、本番と同じ置き場から先に試す (張り出しを比べる撮影に小物の差を混ぜない)
  小札      砂利・小石・歯車・木くず (D の Art/stage/act2/litter)。足元の通りは空ける

材質 (surfaces) は 7 つ = terrain・rock・wood・plank・iron・relief・glow ＋ 霧の板の材質 (mist) = 8 (門 materialsMax 8)。
約束 C3 の 8 つの名前のうち card は書かない (札と小札は Diorama の既定どおり relief に落ちる = 幕1 と同じ)。書くと 9 になり Diorama.Check が NG になる。

使い方: gen_act2.py [--out-dir DIR] [--only NAME[,NAME]] [--no-check] [--allow-missing-ui]
  焼いた後に place2.py で全部を数え、表を ~/.cache/deck-rogue/hd2d-stage2/lanes/C/place2-report.md に書く (--no-check で省く)。
  置き場は place2 の規則 (R1〜R4・R6 板の立ち方・R7 板の貫き) で選ぶ。基準の撮影の UI の矩形が無ければ止まる (place2.load_ui。--allow-missing-ui で続ける)
"""
import argparse
import copy
import json
import math
import os
import random
import sys

import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, '..', '..', '..', '..'))
sys.path.insert(0, os.path.join(REPO, 'docs', 'design', 'hd2d-slice', 'r2-layout-gen'))
sys.path.insert(0, HERE)
import place  # noqa: E402
from layoutio import dump_layout  # noqa: E402
import place2  # noqa: E402

RES = os.path.join(REPO, 'unity', 'Assets', 'Resources') + '/'
STAGE = RES + 'Stage/'
A2 = 'Art/stage/act2/'
SEED = 20261003

# ------------------------------------------------------------------ 層の数 (道の座標)

PATH_YAW = -22.0
T0, T1 = -34.0, 40.0          # 段の t の範囲 (21:9 の左右の端まで)
WT0, WT1 = -22.0, 38.0        # 壁の t の範囲
LOWER_Y = -1.4                # 下の坑道
SEAT_FRONT = -5.0             # 座席の帯の前の縁 (棚の縁 s ≤ −5.0)
TIER1_FRONT, TIER1_Y = 5.0, 0.9
TIER2_FRONT, TIER2_Y = 7.9, 1.8
SHELF_FRONT = 9.4             # 岩棚の前 (区切りごとに +−)
SHELF_TOP = 2.9
MAIN_TOP = 4.4
UPPER_TOP = 6.3
CROWN_TOP = 11.5
OVERHANG = {'act2_layout': 2.0, 'act2_layout_wall3': 3.0, 'act2_layout_wall4': 4.0, 'act2_layout_min': 2.0}

# 灯の置き場 (t)。lantern-b は坑口の脇 (x ≈1690) = 座席の通りの上 (行 328 より上) に掛ける高さ
LANTERN_T = {'lantern-a': -2.9, 'lantern-b': 17.2, 'lantern-c': 4.3}
# 絵の下端 (abs) の目安。実際は下の支え (岩棚の区切りごとの天面 2.75〜3.05・坑口の台 3.6) の LANTERN_LIFT 上に置く (rest_on)。
# lantern-b だけは目安を床にする (座席の通り = PC 行 328 より上に吊る。台が無い最小の設計図では鉤から吊って棚から浮く)
LANTERN_BOTTOM = {'lantern-a': 2.95, 'lantern-b': 3.62, 'lantern-c': 2.95}
LANTERN_LIFT = 0.03           # 提灯の下端は支えの 0.03 上 (place2 の R6 の許し 0.04 の内)
PROP_LIFT = 0.02              # 棚の上・壁の前の小物の下端は支えの 0.02 上
# スマホの提灯は二段目の床 (y 1.8) に置く (棚の高さ 1.1 は提灯の背丈 1.4 より低いので、棚の縁から吊ると下 0.35 が床に沈む)
PHONE_LANTERN_Y = round(TIER2_Y + LANTERN_LIFT, 3)
HANG_T_MIN = 16.0             # 天井から吊る物の t の下限 (計画 §2 C = 幕ボス 160 の頭 x 1200〜1550 の真上を空ける)
HEARTH_T, HEARTH_S = -6.2, 6.8
MOUTH_T = 19.5
WARM = [1.0, 0.64, 0.34]
WARM_FIRE = [1.0, 0.56, 0.26]
VEIN = [0.42, 0.95, 0.86]


def main_face(ov):
    return SHELF_FRONT + ov


_PC = None


def _pc_x(t, s, y):
    global _PC
    if _PC is None:
        _PC = place2.cams()[0]
    return float(_PC.project(place.yaw_rot(PATH_YAW, t, y, s))[0])


def wall_face(P, t0, t1, y0, y1):
    """t0〜t1・高さ y0〜y1 に掛かる壁の面 (wall-main・wall-upper・wall-crown の前の面) のいちばん手前の s。
    上段は本体の前から 0.3 まで張り出すので、本体の前 (main_face) を基準に置くと上段の張り出しに板が埋まる (提灯の上・坑口の上半分)"""
    f = None
    for p in P:
        nm = p.get('name') or ''
        if p['kind'] != 'block' or not nm.startswith(('wall-main', 'wall-upper', 'wall-crown')):
            continue
        if p['t'] + p['w'] / 2 < t0 or p['t'] - p['w'] / 2 > t1:
            continue
        b0, b1 = p['y'] - p.get('sink', 0.2), p['y'] + p['h']
        if b1 < y0 or b0 > y1:
            continue
        fr = p['s'] - p['d'] / 2
        f = fr if f is None else min(f, fr)
    return f


def match_t(t2, ov, s_of_t, y_abs, lo=-40.0, hi=60.0):
    """張り出しの変種で、本番 (張り出し 2) の t2 の画面の x (PC) を保つ t。s_of_t(t, o) = 張り出し o の設計図での s
    (壁の block は張り出しの差だけ奥へずらした同じ乱数なので、本番の s = 変種の s − (ov − 2))。本番ならそのまま t2"""
    if ov == 2.0:
        return t2
    x = _pc_x(t2, s_of_t(t2, 2.0), y_abs)
    for _ in range(60):
        m = (lo + hi) / 2
        if _pc_x(m, s_of_t(m, ov), y_abs) < x:
            lo = m
        else:
            hi = m
    return r3((lo + hi) / 2)


def face_s(P, ov, half, y0, y1, off):
    """match_t に渡す s_of_t: t±half・高さ y0〜y1 の壁の面 − off (張り出し o の設計図へは ov − o だけ手前へ戻す)"""
    def f(t, o):
        fc = wall_face(P, t - half, t + half, y0, y1)
        return (fc if fc is not None else main_face(ov)) - (ov - o) - off
    return f


def stand_ctx(P):
    """立ち方 (place2.stand_info) を当てるための仮の設計図と地面 (段 slab と block だけ見る)"""
    L = dict(pathYaw=PATH_YAW, sources=sources(), parts=P)
    return L, place.Ground(L)


def rest_on(L, G, p, lift, floor=None):
    """板 p の下端を、その下の支え (段・岩棚・台の天面のいちばん高い所 = place2.stand_info) の lift 上に置く (abs)。
    floor を渡すと、それより下には置かない (提灯 b = 座席の通りより上に吊る)。スマホの置き場は触らない"""
    q = dict(p)
    q['abs'] = True
    for _ in range(2):   # 支えになる block は下端で決まるので、置き直した高さでもう一度
        si = place2.stand_info(L, G, q, place2.stand_blocks(L, G))
        if si is None:
            return p
        y = si['support'] + lift
        if floor is not None:
            y = max(y, floor)
        q['y'] = r3(y)
    return q


# ------------------------------------------------------------------ 頂 (tiles・surfaces・sources)

def tile(names, rot, fallback, raw=None):
    """D のタイル (calm 済み) を先に、無ければ元の Art/tiles を calm して使う候補列"""
    vs = []
    for i, n in enumerate(names):
        cand = [A2 + 'tiles/' + n]
        if raw:
            r = raw[i % len(raw)]
            cand.append(dict(art=r[0], calm=r[1], sat=r[2], clampWhite=True))
        vs.append(cand)
    return dict(rot=rot, fallback=fallback, variants=vs)


def head(name, ov):
    abcd = 'abcd'
    tiles = {
        'side_earth': tile(['side_earth_%s' % c for c in abcd], 'flipX', [67, 66, 63], [('Art/tiles/act2_dirt', 0.3, 0.8), ('Art/tiles/act2_dirt_a', 0.3, 0.8)]),
        'top_earth_seat': tile(['top_earth_seat_%s' % c for c in abcd], 'rot4', [107, 105, 101], [('Art/tiles/act2_dirt_a', 0.25, 0.6), ('Art/tiles/act2_dirt', 0.25, 0.6)]),
        'side_cliff': tile(['side_cliff_%s' % c for c in abcd], 'flipX', [56, 66, 85],
                           [('Art/tiles/act2_cliff_a', 0.33, 0.8), ('Art/tiles/act2_cliff_b', 0.33, 0.8), ('Art/tiles/act2_cliff_a', 0.33, 0.8), ('Art/tiles/act2_cliff_d', 0.33, 0.8)]),
        'top_cliff': tile(['top_cliff_%s' % c for c in abcd], 'rot4', [67, 81, 97],
                          [('Art/tiles/act2_cliff_b', 0.25, 0.6), ('Art/tiles/act2_cliff_a', 0.25, 0.6), ('Art/tiles/act2_cliff_d', 0.25, 0.6), ('Art/tiles/act2_cliff_b', 0.25, 0.6)]),
        'side_timber': tile(['side_timber_%s' % c for c in abcd], 'flipX', [78, 75, 80], [('Art/tiles/act2_wood', 0.42, 1.0)]),
        'top_timber': tile(['top_timber_%s' % c for c in abcd], 'flipX', [83, 80, 85], [('Art/tiles/act2_wood', 0.35, 1.0)]),
        'side_plank': tile(['side_plank_%s' % c for c in abcd], 'flipX', [71, 67, 73], [('Art/tiles/act2_plank', 0.4, 1.0)]),
        'top_plank': tile(['top_plank_%s' % c for c in abcd], 'flipX', [79, 76, 81], [('Art/tiles/act2_plank', 0.3, 1.0)]),
        'side_iron': tile(['side_iron_%s' % c for c in abcd], 'flipX', [47, 45, 45]),
        'top_iron': tile(['top_iron_%s' % c for c in abcd], 'flipX', [96, 111, 121]),
    }
    surfaces = {
        'terrain': dict(shader='array', side='side_earth', top='top_earth_seat', receive=0.8, shadowStrength=1.0, topThreshold=0.65, topBlend=0.15, vcolorAO=1.0,
                        tint=[0.82, 0.82, 0.82]),   # 統合 (2026-10-03・格子 k1): 座席の床を 0.82 倍 (床 164 → 105。本家 ot2 の床 110〜125)
        'rock': dict(shader='array', side='side_cliff', top='top_cliff', receive=0.8, shadowStrength=1.0, topThreshold=0.55, topBlend=0.12, vcolorAO=1.0),
        'wood': dict(shader='array', side='side_timber', top='top_timber', receive=0.8, shadowStrength=1.0, vcolorAO=1.0),
        'plank': dict(shader='array', side='side_plank', top='top_plank', receive=0.8, shadowStrength=1.0, vcolorAO=1.0),
        'iron': dict(shader='array', side='side_iron', top='top_iron', receive=0.6, shadowStrength=1.0, vcolorAO=1.0),
        'relief': dict(shader='atlas', receive=0.6, shadowStrength=1.0, cutoff=0.4, vcolorAO=1.0, normal=0.6, sway=dict(amp=0.03, freq=[0.3, 0.8])),
        'glow': dict(shader='shaft', tint=[1.0, 1.0, 1.0], intensity=1.2, softDepth=1.5, nearFade=2.0, edgeFade=0.0, fog=0.25, lobeFloor=1.0),
    }
    doc = ('幕2「先代の坑道」の箱庭の設計図 (HD-2D 段2・計画 docs/design/hd2d-stage2-plan-2026-10-02.md §2 C・約束 docs/design/hd2d-stage2/contracts.md C3)。'
           '作り方は docs/design/hd2d-stage2/layout-gen/gen_act2.py (種 %d・手で直さず焼き直す)・規則の検査は place2.py。座標は幕1 と同じ道の座標 '
           '(t = 道に沿った距離・s = 道と直角 (+ が奥)・y = 地面からの高さ (abs なら絶対))。層: 下の坑道 −1.4・座席の帯 0 (s −5.0〜5.0)・一段目 +0.9 (s 5.0〜7.9)・'
           '二段目 +1.8 (s 7.9〜・レール)・岩棚 block abs 1.8〜2.9 (前 s 9.4)・壁の本体 1.8〜4.4 (前 s %.1f = 張り出し %.0f unit)・上段 4.4〜6.3・頂 6.3〜11.5・奥の保険。'
           '段は slab (block にすると HeightAtPath が下の坑道の −1.4 を返して上の部品が沈む)。天井は作らない (block に底の面が無い)・水路と桟橋は組まない。'
           '光の部品の名前 (lantern-a・lantern-b・lantern-a-halo・lantern-b-halo・hearth・hearth-halo・mouth-lamp) はレーン B の look_act2*.json の lights の at が読む '
           '(動かすと光も付いてくる)。lantern-c は光の 4 つ目の枠の候補 (点けなければ暈だけ)。材質は 7 つ + 霧の板 = 8 (card は書かない = 札と小札は relief)。'
           '絵は Art/stage/act2/ (レーン D) を先に、無ければ元の Art/props/act2_*・Art/tiles/act2_*' % (SEED, main_face(ov), ov))
    if name == 'act2_layout_min':
        doc = ('幕2 の最小の試しの設計図 (計画 §3 手順 2・約束 C3)。岩棚つきの壁 1 式＋提灯 2 と暈＋座席の帯の slab と段だけ (小物・霧の板・額縁なし)。'
               'look_act2_min.json の "layout" が指す。gates は緩めた。作り方は gen_act2.py。') + doc
    elif name != 'act2_layout':
        doc = ('岩棚の張り出し %.0f unit の変種 (本番は 2。計画 §3 手順 4 で決める)。look_act2_wall%.0f.json の "layout" が指す。棚の前 s 9.4 は同じで、'
               '本体・上段・頂・歩廊・坑口・提灯を奥へ %.0f unit ずらした。作り方は gen_act2.py。' % (ov, ov, ov - 2)) + doc
    gates = dict(partsMin=160, partsMax=260, litterMax=120, materialsMax=8)
    if name == 'act2_layout_min':
        gates = dict(partsMin=5, partsMax=260, litterMax=120, materialsMax=8)
    return dict(_doc=doc, version=1, act=2, pathYaw=PATH_YAW, tile=64, albedoLinear=True, normalStrength=0.6, staticBatch=True,
                gates=gates, tiles=tiles, surfaces=surfaces, sources={})


# 半立体・札の元の絵 (名前 → D の relief の名前)。art は D の絵を先に、無ければ元の Art/props/act2_*
RELIEF_SRC = {
    'hearth': 'hearth', 'lanternHang': 'lantern_hang', 'lanternHangB': 'lantern_hang_b', 'lanternPost': 'lantern_post',
    'barrelStack': 'barrel_stack', 'crate': 'crate', 'crateOpen': 'crate_open', 'crateStack': 'crate_stack', 'sacks': 'sacks',
    'trough': 'trough', 'lumber': 'lumber', 'toolrack': 'toolrack', 'bellPost': 'bell_post', 'shelter': 'shelter',
    'stallA': 'stall_a', 'stallB': 'stall_b', 'wreck': 'wreck', 'winch': 'winch', 'gearBig': 'gear_big', 'gearPile': 'gear_pile',
    'minecart': 'minecart', 'minecartTipped': 'minecart_tipped', 'orePile': 'ore_pile', 'crystal': 'crystal', 'crystalBig': 'crystal_big',
    'stalactite': 'stalactite_a', 'stalactiteCluster': 'stalactite', 'rootsHang': 'roots_hang', 'roots': 'roots', 'pulley': 'pulley',
    'tunnelSide': 'tunnel_side',
}
LITTER_SRC = {'litterGravel%d' % i: 'gravel_%d' % i for i in range(1, 5)}
LITTER_SRC.update({'litterPebble%d' % i: 'pebble_%d' % i for i in range(1, 5)})
LITTER_SRC.update({'litterGear%d' % i: 'gear_%d' % i for i in range(1, 4)})
LITTER_SRC.update({'litterChips%d' % i: 'chips_%d' % i for i in range(1, 5)})


def sources():
    s = {}
    for k, n in RELIEF_SRC.items():
        s[k] = dict(art=[A2 + 'relief/' + n, 'Art/props/act2_' + n], depth=0.12, cells=40)
    s['mouthDark'] = dict(_doc='坑口の中の黒い札 (平らな札・部品の tint で暗く)。D の mouth_dark が無ければ幕1 の無地の暗い紺緑 (32×32) を借りる',
                          art=[A2 + 'relief/mouth_dark', 'Art/stage/act1/relief/backdrop_plain'], depth=0.0, cells=8, flat=True)
    for k, n in LITTER_SRC.items():
        s[k] = dict(art=[A2 + 'litter/' + n], depth=0.0, cells=8, flat=True)
    return s


# ------------------------------------------------------------------ 道具

class Seeds:
    def __init__(self, base):
        self.n = base

    def __call__(self):
        self.n += 7
        return self.n


def r3(v):
    return round(float(v), 3)


def wobble(rng, t0, t1, base, amp, step=1.28):
    """縁の折れ線 (格子 step・振れ amp。輪郭にだけ凹凸 = art-bible §2)"""
    pts = []
    t = t0
    while t <= t1 + 1e-6:
        pts.append([r3(t), r3(base + (rng.random() - 0.5) * 2 * amp)])
        t += step
    return pts


def segments(rng, t0, t1, wmin, wmax):
    """t0〜t1 を幅 wmin〜wmax の区切りに割る [(中心, 幅)]"""
    out = []
    t = t0
    while t < t1 - 0.5:
        w = min(t1 - t, wmin + rng.random() * (wmax - wmin))
        if t1 - (t + w) < wmin * 0.6:
            w = t1 - t
        out.append((t + w / 2, w))
        t += w
    return out


# ------------------------------------------------------------------ 層

def slabs(rng, sd):
    P = []
    P.append(dict(kind='slab', name='lower-floor', surface='terrain', top=LOWER_Y, bottom=-3.0, back=SEAT_FRONT + 0.05, grid=1.28, chamfer=0.0,
                  front=[[T0, -30.0], [T1, -30.0]], mottle=dict(scale=[2, 5], amount=0.12, seed=sd())))
    P.append(dict(kind='slab', name='seat-band', surface='terrain', top=0.0, bottom=LOWER_Y - 0.05, back=TIER1_FRONT, grid=0.32, chamfer=0.0,
                  front=[[T0, SEAT_FRONT], [T1, SEAT_FRONT]], mottle=dict(scale=[2, 5], amount=0.08, seed=sd()), seed=sd()))
    P.append(dict(kind='slab', name='tier-1', surface='terrain', top=TIER1_Y, bottom=-0.1, back=TIER2_FRONT, grid=0.64, chamfer=0.12,
                  front=wobble(rng, T0, T1, TIER1_FRONT + 0.08, 0.08), mottle=dict(scale=[2, 5], amount=0.1, seed=sd())))
    P.append(dict(kind='slab', name='tier-2', surface='terrain', top=TIER2_Y, bottom=0.8, back=40.0, grid=0.64, chamfer=0.1,
                  front=wobble(rng, T0, T1, TIER2_FRONT + 0.06, 0.06), mottle=dict(scale=[2, 5], amount=0.1, seed=sd())))
    return P


def wall(rng, sd, ov):
    """3D の岩棚 (棚・本体・上段・頂・奥)。名前は wall- (place2 の R2・R3 は背景として数えない)"""
    mf = main_face(ov)
    P = []
    # 棚: 区切りごとに前と高さを少しずつ変える (鑿目は輪郭の折れ線だけ)。奥は本体の中へ 0.6 埋める
    for i, (tc, w) in enumerate(segments(rng, WT0, WT1, 4.5, 8.5)):
        front = SHELF_FRONT + rng.random() * 0.2 - 0.05
        top = SHELF_TOP + (rng.random() - 0.5) * 0.3
        back = mf + 0.6
        P.append(dict(kind='block', name='wall-shelf-%d' % i, surface='rock', t=r3(tc), s=r3((front + back) / 2), y=TIER2_Y, abs=True, yaw=0,
                      w=r3(w + 0.05), d=r3(back - front), h=r3(top - TIER2_Y), cut=0.3, chamfer=0.1, sink=0.2, ao=False, seed=sd()))
    # 本体 (帯が乗る面): 前を 0〜0.2 奥へ揺らす・上端を ±0.15
    for i, (tc, w) in enumerate(segments(rng, WT0, WT1, 6.0, 11.0)):
        front = mf + rng.random() * 0.2
        top = MAIN_TOP + (rng.random() - 0.5) * 0.3
        P.append(dict(kind='block', name='wall-main-%d' % i, surface='rock', t=r3(tc), s=r3(front + 1.0), y=TIER2_Y, abs=True, yaw=0,
                      w=r3(w + 0.05), d=2.0, h=r3(top - TIER2_Y), cut=0.32, chamfer=0.12, sink=0.2, ao=False, seed=sd()))
    # 上段 (暗い・岩の模様を残す): 本体の前から −0.3〜0 (底の面が無いので張り出しは 0.3 まで)
    for i, (tc, w) in enumerate(segments(rng, WT0, WT1, 5.0, 10.0)):
        front = mf - rng.random() * 0.3
        top = UPPER_TOP + (rng.random() - 0.5) * 0.4
        P.append(dict(kind='block', name='wall-upper-%d' % i, surface='rock', t=r3(tc), s=r3(front + 0.8), y=MAIN_TOP - 0.15, abs=True, yaw=0,
                      w=r3(w + 0.05), d=1.6, h=r3(top - MAIN_TOP + 0.15), cut=0.3, chamfer=0.1, sink=0.0, ao=False, tint=0.85,
                      phone=dict(hide=True), seed=sd()))
    # 頂: 上部バーの下まで岩で埋める (22°・5°・0.36 では上段の上が行 107〜151)
    for i, (tc, w) in enumerate(segments(rng, WT0, WT1, 8.0, 14.0)):
        front = mf + 0.25 + rng.random() * 0.3
        P.append(dict(kind='block', name='wall-crown-%d' % i, surface='rock', t=r3(tc), s=r3(front + 1.2), y=UPPER_TOP - 0.25, abs=True, yaw=0,
                      w=r3(w + 0.05), d=2.4, h=r3(CROWN_TOP - UPPER_TOP + 0.25), cut=0.3, chamfer=0.1, sink=0.0, ao=False, tint=0.7,
                      phone=dict(hide=True), seed=sd()))
    # 奥の保険 (普段は全部隠れる)
    P.append(dict(kind='block', name='wall-back', surface='rock', t=8.0, s=r3(mf + 10.6), y=TIER2_Y, abs=True, yaw=0, w=64.0, d=4.0, h=12.0,
                  cut=0.0, chamfer=0.0, sink=0.2, ao=False, tint=0.6, shadow=False, seed=sd()))
    return P


def mouth(sd, ov, W):
    """上の坑口 (棚の上の台 wall-landing の上に開く)。柱は block (marker は道標の腕つきの形なので使わない)。
    札・柱・梁・奥の灯は、坑口の幅 (梁 3.4) と高さ (3.6〜6.97) に掛かる壁の面 (上段の張り出しを含む wall_face) の手前に立てる
    (本体の前 main_face を基準にすると、上段の張り出し 〜0.3 が札の上半分と柱の頭を覆う)。W = ここまでに組んだ壁"""
    mf = main_face(ov)
    y0 = 3.6
    ytop = y0 + 2.95 + 0.42
    mt = match_t(MOUTH_T, ov, face_s(W, ov, 1.7, y0, ytop, 0.08), y0)   # 変種でも札の画面の x を本番と同じに
    face = wall_face(W, mt - 1.7, mt + 1.7, y0, ytop)
    face = mf if face is None else face
    P = []
    P.append(dict(kind='block', name='wall-landing', surface='rock', t=mt, s=r3((SHELF_FRONT + 0.2 + mf + 0.3) / 2), y=SHELF_TOP - 0.1, abs=True, yaw=0,
                  w=4.6, d=r3(mf + 0.3 - SHELF_FRONT - 0.2), h=r3(y0 - SHELF_TOP + 0.1), cut=0.3, chamfer=0.1, sink=0.0, ao=False, seed=sd()))
    # スマホでは組まない: 上部バーの裏 (行 −60〜106) で、下の 30px だけ敵の座席の通りに掛かる。坑口の奥の灯 (光 = レーン B の lights の mouth-lamp) も
    # スマホでは消す = B が lights の mouth-lamp に "phone": {"on": false} を書く (StageLook.S2B_PartPlace は部品の phone.hide を見ない)
    ph = dict(hide=True)
    P.append(dict(kind='card', name='mouth-dark', src='mouthDark', t=mt, s=r3(face - 0.08), y=y0, abs=True, yaw=PATH_YAW, scale=2.3, tint=0.12,
                  shadow=False, phone=ph, seed=sd()))
    for side, dt in (('L', -1.28), ('R', 1.28)):
        P.append(dict(kind='block', name='mouth-post-%s' % side, surface='wood', t=r3(mt + dt), s=r3(face - 0.22), y=y0, abs=True, yaw=0,
                      w=0.36, d=0.36, h=2.95, cut=0.06, chamfer=0.04, sink=0.05, tint=0.85, phone=ph, seed=sd()))
    P.append(dict(kind='block', name='mouth-lintel', surface='wood', t=mt, s=r3(face - 0.22), y=r3(y0 + 2.95), abs=True, yaw=0,
                  w=3.4, d=0.46, h=0.42, cut=0.08, chamfer=0.05, sink=0.0, tint=0.85, phone=ph, seed=sd()))
    P.append(dict(kind='halo', name='mouth-lamp', t=r3(mt + 0.3), s=r3(face - 0.35), y=y0 + 0.95, abs=True, r=0.42, color=[1.0, 0.68, 0.40],
                  gain=0.55, core=0.3, facing='camera', phone=ph, seed=sd()))
    return P


# 暈 (kind halo) は toward と lobe を書かない: toward の既定 = glow の softDepth (後ろの壁で薄くならない分だけカメラへ寄せる)・lobe の既定 1
# (0 にすると StageShaft が月光の筋の扱いにする)。glow の lobeFloor 1 = 霧の芯で暗くしない (Diorama.cs 頭の段2 の注記・レーン S)


def lanterns(sd, ov, W, with_c=True):
    """壁の提灯 (吊り提灯の半立体＋鉤＋暈)。W = ここまでに組んだ段と壁 (と坑口の台)。
    PC: 提灯の幅と高さに掛かる壁の面 (上段の張り出しを含む wall_face) の 0.12 手前に、下端を下の支え (岩棚の区切りの天面・坑口の台) の 0.03 上に置く。
        鉤は同じ面から出す。lantern-b は座席の通りより上 (下端 ≥3.62) に吊る (台の上なら台に載る)。
    スマホ: 上部バーとからくりの帯を避けて、棚の前 (s 9.28) の二段目の床 (y 1.8) に置く (y 1.83。置き場の t は fit_phone の PHONE_FIT)"""
    P = []
    names = ['lantern-a', 'lantern-b'] + (['lantern-c'] if with_c else [])
    phone = {
        'lantern-a': dict(t=0.8, s=9.28, y=PHONE_LANTERN_Y),     # からくり・ギアの帯 (x 0〜640・行 81〜210) の右
        'lantern-b': dict(t=21.0, s=9.28, y=PHONE_LANTERN_Y),    # 敵 3〜4 体の座席の通り (行 157〜) の外
        'lantern-c': dict(t=3.9, s=9.28, y=PHONE_LANTERN_Y),
    }
    src = 'lanternHang'
    im, _ = place.art_for(dict(sources=sources()), src)
    hh = (im.size[1] / place.TPU) if im is not None else 1.48
    hw = ((im.size[0] / place.TPU) if im is not None else 0.6) / 2 + 0.06
    for nm in names:
        yb = LANTERN_BOTTOM[nm]
        t = match_t(LANTERN_T[nm], ov, face_s(W, ov, hw, yb, yb + hh, 0.12), yb)   # 変種でも画面の x を本番と同じに
        face = wall_face(W, t - hw, t + hw, yb, yb + hh)
        face = main_face(ov) if face is None else face
        L, G = stand_ctx(W)
        p = dict(kind='relief', name=nm, src=src, t=t, s=r3(face - 0.12), y=yb, abs=True, yaw=PATH_YAW, flip=(nm == 'lantern-b'), shadow=False)
        p = rest_on(L, G, p, LANTERN_LIFT, floor=yb if nm == 'lantern-b' else None)
        y = p['y']
        ph = phone[nm]
        p['phone'] = dict(ph)
        p['seed'] = sd()
        P.append(p)
        P.append(dict(kind='block', name=nm + '-hook', surface='iron', t=t, s=r3(face - 0.3), y=r3(y + hh - 0.04), abs=True, yaw=0,
                      w=0.07, d=0.55, h=0.07, cut=0.0, chamfer=0.0, sink=0.0, phone=dict(hide=True), seed=sd()))
        P.append(dict(kind='halo', name=nm + '-halo', t=t, s=r3(face - 0.25), y=r3(y + hh * 0.42), abs=True, r=0.55, color=WARM,
                      gain=0.7 if nm != 'lantern-c' else 0.5, core=0.35, facing='camera',
                      phone=dict(t=ph['t'], s=r3(ph['s'] - 0.13), y=r3(ph['y'] + hh * 0.42)), seed=sd()))
    return P


def hearth(sd):
    """炉 (半立体) と暈。スマホの置き場は fit_phone (PHONE_FIT) が決める"""
    P = []
    P.append(dict(kind='relief', name='hearth', src='hearth', t=HEARTH_T, s=HEARTH_S, y=0.0, yaw=0, flip=False, seed=sd()))
    P.append(dict(kind='halo', name='hearth-halo', t=HEARTH_T, s=r3(HEARTH_S - 0.35), y=0.85, r=0.95, color=WARM_FIRE, gain=0.6, core=0.3,
                  facing='camera', seed=sd()))
    return P


def timber(sd, ov):
    """支保工: 柱 3・梁 2・持ち送り。座席の帯の上は通らない"""
    P = []
    post_s = r3(SHELF_FRONT + 0.05)
    top = 5.75
    beam2_y = 6.2   # 梁 2 の下端 (計画は 5.75。手前の端を上部バーの方へ上げる)。真ん中の柱 b はこの高さまで伸ばして梁 2 を載せる (5.75 のままだと梁が柱から 0.45 浮く)
    posts = [('a', -8.2, top), ('b', 0.0, beam2_y), ('c', 24.2, top)]
    for nm, t, ptop in posts:
        P.append(dict(kind='block', name='post-%s' % nm, surface='wood', t=t, s=post_s, y=TIER2_Y, abs=True, yaw=0, w=0.5, d=0.5, h=r3(ptop - TIER2_Y),
                      cut=0.06, chamfer=0.05, sink=0.2, seed=sd()))
        P.append(dict(kind='block', name='post-%s-cap' % nm, surface='wood', t=t, s=post_s, y=r3(ptop - 0.28), abs=True, yaw=0, w=0.95, d=0.75, h=0.28,
                      cut=0.06, chamfer=0.04, sink=0.0, seed=sd()))
    # 梁 1: 左の 2 本の柱の頭を道に沿って結ぶ
    P.append(dict(kind='block', name='beam-1', surface='wood', t=-4.1, s=post_s, y=top, abs=True, yaw=0, w=8.9, d=0.44, h=0.42, cut=0.06, chamfer=0.05,
                  sink=0.0, phone=dict(hide=True), seed=sd()))
    # 梁 2: 真ん中の柱から手前へ張り出す (天井の梁が上へ抜ける形。前の端 s 3.05 = 座席の帯の外)
    s0, s1 = 3.05, r3(main_face(ov) - 0.1)
    P.append(dict(kind='block', name='beam-2', surface='wood', t=0.0, s=r3((s0 + s1) / 2), y=beam2_y, abs=True, yaw=0, w=0.44, d=r3(s1 - s0), h=0.42, cut=0.06,
                  chamfer=0.05, sink=0.0, phone=dict(hide=True), seed=sd()))
    return P


def gallery(rng, sd, ov):
    """左の歩廊 (棚の上・床 y 3.6)。スマホでは組まない"""
    mf = main_face(ov)
    t0, t1 = -13.0, -4.0
    f0 = r3(mf - 1.3)
    P = []
    ph = dict(hide=True)
    P.append(dict(kind='block', name='gallery-floor', surface='plank', t=r3((t0 + t1) / 2), s=r3(f0 + 0.6), y=3.6, abs=True, yaw=0, w=t1 - t0, d=1.2, h=0.14,
                  cut=0.0, chamfer=0.02, sink=0.0, phone=ph, seed=sd()))
    t = t0 + 0.4
    k = 0
    while t < t1 - 0.2:
        P.append(dict(kind='block', name='gallery-leg-%d' % k, surface='wood', t=r3(t), s=r3(f0 + 0.15), y=SHELF_TOP - 0.05, abs=True, yaw=0, w=0.22, d=0.22,
                      h=r3(3.6 - SHELF_TOP + 0.05), cut=0.0, chamfer=0.02, sink=0.0, phone=ph, seed=sd()))
        k += 1
        t += 2.2 + rng.random() * 0.8
    P.append(dict(kind='fence', name='gallery-rail', t=r3((t0 + t1) / 2), s=r3(f0 + 0.08), y=3.74, abs=True, yaw=0, len=t1 - t0, h=0.85, posts=5,
                  phone=ph, seed=sd()))
    return P


def rails(sd):
    P = []
    # abs: Diorama.Check の座席の帯の届き (S2S_Reach) は線路の長さの半分を半径に読む = 長い線路は帯に掛かると数えられる。abs の部品は数えない
    P.append(dict(kind='rail', name='track-2', t=8.0, s=8.65, y=TIER2_Y, abs=True, yaw=0, length=56.0, gauge=0.9, sleeperLen=1.35, sleepers=[0.8, 1.3], seed=sd()))
    P.append(dict(kind='rail', name='track-lower', t=6.0, s=-6.4, y=LOWER_Y, abs=True, yaw=0, length=60.0, gauge=1.0, sleeperLen=1.6, sleepers=[0.8, 1.3],
                  phone=dict(hide=True), seed=sd()))
    return P


def edges(rng, sd):
    """棚の縁 (座席の帯の前・下の坑道へ垂れる板張りと暗い横木) と一段目の土留め (横木と杭)。石段と木の階段"""
    P = []
    P.append(dict(kind='block', name='ledge-planks', surface='plank', t=3.0, s=r3(SEAT_FRONT - 0.13), y=LOWER_Y, abs=True, yaw=0, w=74.0, d=0.1,
                  h=r3(-LOWER_Y - 0.03), cut=0.0, chamfer=0.0, sink=0.0, seed=sd()))
    P.append(dict(kind='block', name='ledge-cap', surface='wood', t=3.0, s=r3(SEAT_FRONT - 0.02), y=-0.15, abs=True, yaw=0, w=74.0, d=0.42, h=0.16,
                  cut=0.0, chamfer=0.03, sink=0.0, tint=0.55, seed=sd()))
    t = T0 + 1.0
    k = 0
    while t < T1 - 1.0:
        P.append(dict(kind='block', name='ledge-post-%d' % k, surface='wood', t=r3(t), s=r3(SEAT_FRONT - 0.28), y=LOWER_Y, abs=True, yaw=0, w=0.24, d=0.24,
                      h=r3(-LOWER_Y - 0.05), cut=0.0, chamfer=0.03, sink=0.0, tint=0.6, seed=sd()))
        k += 1
        t += 2.4 + rng.random() * 0.8
    P.append(dict(kind='block', name='edge-timber', surface='wood', t=3.0, s=r3(TIER1_FRONT + 0.02), y=0.58, abs=True, yaw=0, w=74.0, d=0.3, h=0.34,
                  cut=0.0, chamfer=0.04, sink=0.0, tint=0.8, seed=sd()))
    t = -16.0
    k = 0
    while t < 30.0:
        P.append(dict(kind='block', name='stake-%d' % k, surface='wood', t=r3(t), s=r3(TIER1_FRONT - 0.12), y=0.0, abs=True, yaw=0, w=0.16, d=0.16, h=1.05,
                      cut=0.0, chamfer=0.02, sink=0.1, tint=0.8, seed=sd()))
        k += 1
        t += 2.2 + rng.random() * 0.9
    for k in range(3):   # 石段 (座席の帯 → 一段目)
        P.append(dict(kind='block', name='steps-stone-%d' % k, surface='rock', t=-1.4, s=r3(TIER1_FRONT - 0.62 + k * 0.28), y=r3(k * 0.3), abs=True, yaw=0,
                      w=2.0, d=0.5, h=0.3, cut=0.1, chamfer=0.04, sink=0.1, seed=sd()))
    for k in range(4):   # 木の階段 (一段目 → 二段目)
        P.append(dict(kind='block', name='steps-wood-%d' % k, surface='wood', t=12.4, s=r3(TIER2_FRONT - 0.78 + k * 0.26), y=r3(TIER1_Y + k * 0.225), abs=True,
                      yaw=0, w=1.6, d=0.52, h=0.225, cut=0.0, chamfer=0.03, sink=0.05, seed=sd()))
    return P


def mists():
    return [
        # 統合 (2026-10-03・試し撮りの格子 k1): y 1.5→1.2・h 3.0→2.7 (帯の頂点を下げる)・alpha 0.2→0.34・tint [0.44,0.43,0.42]→[0.62,0.66,0.7]
        # (本家 ot2 の壁の前の青灰のもや (90,111,133)。最小の試しでは壁 100÷床 164 = 0.61・格子で 1.59)
        dict(kind='mist', name='wall-mist', t=4.0, s=9.0, y=1.2, abs=True, w=64.0, h=2.7, alpha=0.34, noise=[0.16, 0.8], flow=0.015,
             tint=[0.62, 0.66, 0.7], seed=20262601, phone=dict(alpha=0.22)),
        dict(kind='mist', name='floor-mist', t=4.0, s=4.3, y=0.0, w=64.0, h=1.0, alpha=0.1, noise=[0.2, 1.0], flow=0.01, seed=20262602,
             phone=dict(hide=True)),
    ]


def frames(sd):
    """手前の額縁 (世界の 3D)。岩は下の坑道の床の左右の隅
    (PC の左下 = エナジーの輪と手札の間 x ≈300・右下 = 手札とターン終了の間 x ≈1560)。スマホは組まない (左下は UI・柱と右の岩は計画 §2 C の phone.hide)"""
    return [
        # 左の支保工の柱 frame-post-L は外した (統合 2026-10-03: 近くて暗くぼけ、画面の左端の模様の無い黒い縦の帯 x 0〜70 に見えた)
        dict(kind='rock', name='frame-rock-L', t=-7.7, s=-7.4, y=0.0, yaw=24.0, r=1.25, h=1.15, sides=9, squash=0.75, tint=0.55,
             phone=dict(hide=True), seed=sd()),   # スマホの左下は UI (自分の札・エナジーの輪・手札) が 65% を覆う
        dict(kind='rock', name='frame-rock-R', t=0.4, s=-9.9, y=0.0, yaw=300.0, r=1.1, h=1.0, sides=8, squash=0.7, tint=0.55,
             phone=dict(hide=True), seed=sd()),
    ]


# 小物の候補 (B の置き場 = StageActs.PaintMarket2 の t・s。tier = 'tier1'(0.9)・'tier2'(1.8)・'shelf'(棚の上 abs)・'lower'(−1.4)・'hang'(上から吊る abs))
# alts = 規則に掛かった時に試す t のずらし。順に試し、全部だめなら外す
PROPS = [
    # 一段目 (宿場)
    ('barrelStack', -9.0, 5.8, 'tier1', False), ('barrelStack', 17.0, 6.8, 'tier1', True),
    ('shelter', -3.2, 7.6, 'tier1', False), ('stallA', 2.5, 7.0, 'tier1', False), ('stallB', 15.2, 7.3, 'tier1', True),
    ('toolrack', -0.8, 7.6, 'tier1', False), ('toolrack', 19.2, 7.6, 'tier1', True),
    ('lumber', 8.2, 6.1, 'tier1', False), ('lumber', 19.0, 7.4, 'tier1', False),
    ('bellPost', -0.8, 5.5, 'tier1', False),
    ('crate', -1.6, 7.6, 'tier1', False), ('crate', 4.4, 6.2, 'tier1', True), ('crate', 9.8, 7.5, 'tier1', False), ('crate', 14.0, 5.8, 'tier1', True),
    ('crate', 21.0, 7.4, 'tier1', False),
    ('crateStack', 12.6, 7.5, 'tier1', False),
    ('crateOpen', 3.4, 7.5, 'tier1', False), ('crateOpen', 10.2, 6.3, 'tier1', True),
    ('sacks', 1.0, 7.5, 'tier1', False), ('sacks', -4.6, 7.5, 'tier1', True), ('sacks', 18.2, 6.0, 'tier1', False),
    ('trough', -5.6, 7.5, 'tier1', False),
    ('lanternPost', -8.4, 5.4, 'tier1', False), ('lanternPost', 10.4, 5.4, 'tier1', True), ('lanternPost', 23.0, 5.4, 'tier1', False),
    # 二段目 (軌道)
    ('minecart', 5.0, 8.6, 'tier2', False), ('minecartTipped', -6.0, 8.5, 'tier2', True),
    ('orePile', -4.5, 8.4, 'tier2', False), ('orePile', 7.0, 8.4, 'tier2', False), ('orePile', 16.5, 8.4, 'tier2', True),
    # 棚の上 (壁の物)
    ('crystalBig', 1.9, 10.0, 'shelf', False), ('crystal', -8.5, 10.2, 'shelf', False), ('crystal', -5.0, 10.4, 'shelf', True),
    ('crystal', 4.5, 10.3, 'shelf', False), ('crystal', 25.0, 10.3, 'shelf', True),
    ('gearBig', -5.5, 10.4, 'shelf', False), ('gearPile', -5.3, 9.9, 'shelf', False), ('gearPile', 12.5, 10.0, 'shelf', True),
    ('wreck', -8.0, 10.2, 'shelf', False), ('winch', 25.5, 8.6, 'tier2', False), ('tunnelSide', 27.0, 0.0, 'wall', True),
    # 天井から (t ≥16・x 1200〜1550 の真上は空ける)
    ('stalactite', 15.6, 10.6, 'hang', False), ('stalactiteCluster', 21.6, 8.8, 'hang', True), ('stalactite', 23.4, 8.2, 'hang', True),
    # 下の坑道 (棚の手前 −1.4)
    ('minecart', -3.0, -6.4, 'lower', True), ('orePile', 8.0, -6.0, 'lower', False),
]
ALTS = [0.0, 0.6, -0.6, 1.2, -1.2, 1.8, -1.8, 2.4, -2.4, 3.0, -3.0, 4.0, -4.0]
HANG_TOP = 7.6     # 天井から吊る物の上端 (abs・PC 行 < 60 = 上部バーの裏から垂れる)


def prop_part(src, t, s, tier, flip, ov, sd, L, G):
    im, _ = place.art_for(L, src)
    hh = (im.size[1] / place.TPU) if im is not None else 1.5
    mf = main_face(ov)
    p = dict(kind='relief', src=src, t=r3(t), s=r3(s), yaw=0, flip=bool(flip), seed=sd())
    if tier == 'tier1':
        p['y'] = 0.0
        p['s'] = r3(min(max(s, TIER1_FRONT + 0.25), TIER2_FRONT - 0.15))
    elif tier == 'tier2':
        p['y'] = 0.0
        p['s'] = r3(min(max(s, TIER2_FRONT + 0.2), SHELF_FRONT - 0.15))
    elif tier == 'shelf':
        # 下端は岩棚の区切りごとの天面 (2.75〜3.05) の 0.02 上 (固定の SHELF_TOP だと浮く物と沈む物が出る)
        p['s'] = r3(min(max(s, SHELF_FRONT + 0.35), mf - 0.3))
        p['y'] = SHELF_TOP + PROP_LIFT
        p['abs'] = True
        p = rest_on(L, G, p, PROP_LIFT)
    elif tier == 'wall':
        # 壁の前に立つ物 (横坑の口): 幅と高さに掛かる壁の面 (上段の張り出しを含む) の 0.06 手前・下端は岩棚の天面の 0.02 上
        # (二段目の床 1.8 に立てると、壁の前に重なる岩棚 1.8〜2.9 に 1 unit 埋まる)
        w2 = ((im.size[0] / place.TPU) if im is not None else 1.0) / 2 + 0.06
        face = wall_face(L['parts'], t - w2, t + w2, SHELF_TOP, SHELF_TOP + hh)
        p['s'] = r3((mf if face is None else face) - 0.06)
        p['y'] = SHELF_TOP + PROP_LIFT
        p['abs'] = True
        p['yaw'] = PATH_YAW
        p = rest_on(L, G, p, PROP_LIFT)
    elif tier == 'hang':
        p['y'] = r3(HANG_TOP - hh)
        p['abs'] = True
        p['shadow'] = False
        p['phone'] = dict(hide=True)
        if src in ('rootsHang',):
            p['sway'] = 0.4
            p['swayFrom'] = 'top'
    elif tier == 'lower':
        p['y'] = 0.0
        p['phone'] = dict(hide=True)
    if src in ('crystalBig', 'crystal', 'orePile'):
        p['shadow'] = False
    if src == 'crystalBig':
        p['tint'] = 0.72   # 統合 (2026-10-03): 主人公の右の壁の上段 (合否②の窓) でいちばん明るい物 = 淡い結晶を沈める
    return p


# ------------------------------------------------------------------ 規則で置き場を選ぶ (place2 と同じ式)

class Judge:
    """place2 の R1〜R4 を 1 つの部品に当てる (PC・PH・PC21)"""

    def __init__(self, L):
        self.L = L
        self.G = place.Ground(L)
        self.cams = place2.cams()
        self.info = {}
        for cam in self.cams:
            ui = place2.load_ui(cam)
            zones = place2.seat_zones(cam, self.G, ui)
            intents = place2.intent_rects(cam, self.G, ui) if cam.name != 'PC21' else []
            rects = list(intents)
            if cam.name == 'PC':
                rects.append(('boss160', list(place2.BOSS160)))
            rc = self.G.raycast(cam, 4)
            tdf = place.upsample(rc, cam.W, cam.H)
            self.info[cam.name] = dict(cam=cam, zones=zones, rects=rects, tdf=tdf, ui=ui)

    def problems(self, p, litter=False):
        out = []
        for cn, inf in self.info.items():
            cam = inf['cam']
            if litter:
                q = place2.phone_place(p, cam)
                if q is None:
                    continue
                o = place2.place_any(self.L, self.G, cam, 0, q, False)
                if o is None or o.box is None:
                    continue
                x0, y0, x1, y1 = o.box
                for nm, fx, fy, fd, hw, head in inf['zones']:
                    if x1 >= fx - hw - 2 and x0 <= fx + hw + 2 and y1 >= fy - 10 and y0 <= fy + 22:
                        out.append((cn, 'R4', nm))
                        break
                continue
            o = place2.place_any(self.L, self.G, cam, 0, p, True)
            if o is None or o.box is None or o.note in ('phone-hide', 'no-art'):
                continue
            sil, why = place2.is_silhouette(o.p, self.G)
            vm, npx = place.visible_mask(o, cam, inf['tdf'], cam.W, cam.H)
            if vm is None or npx == 0:
                continue
            tall = sil is True or (sil is None and (o.worldH or 0.0) >= place2.LOW_H)
            if sil is not False:
                # 主人公の列は低い物も入れない (帯の合否①を測る列・頭の真後ろ = 生成器だけの決まり。place2 は低い物を数えるだけ)
                for nm, fx, fy, fd, hw, head in inf['zones']:
                    if not tall and nm != 'hero':
                        continue
                    c = place2.rect_count(vm, (fx - hw - 4, head - 4, 2 * hw + 8, fy + 24 - head))
                    if c > 0:
                        out.append((cn, 'R2', nm, c))
            nm = o.p.get('name') or ''
            if o.kind not in place2.NO_INTENT_KINDS and not nm.startswith(place2.NO_INTENT_NAMES) and o.kind != 'litter':
                for rn, r in inf['rects']:
                    c = place2.rect_count(vm, (r[0] - 3, r[1] - 3, r[2] + 6, r[3] + 6))
                    lim = 0   # 生成器は place2 (幕ボス 160 の頭は 1%) より厳しく 0
                    if c > lim and o.kind not in place2.LIGHT_KINDS:
                        out.append((cn, 'R3', rn, c))
        # R6・R7 (板の立ち方・貫き): カメラに依らない。置き場は PC とスマホ
        if not litter:
            for ph in (False, True):
                blocks = place2.stand_blocks(self.L, self.G, phone=ph)
                for rule, what, v, who in place2.stand_problems(self.L, self.G, p, blocks, phone=ph):
                    out.append(('PH' if ph else 'PC', rule, '%s (%s)' % (what, who), v))
        return out

    def box(self, p, cam_name='PC'):
        inf = self.info[cam_name]
        q = place2.phone_place(p, inf['cam']) if cam_name == 'PH' else p
        if q is None:
            return None
        o = place2.place_any(self.L, self.G, inf['cam'], 0, q, False)
        return None if (o is None or o.box is None or o.note in ('phone-hide', 'no-art')) else o.box

    def phone_ui_hits(self, p):
        """スマホのからくり・ギアの帯 (x 0〜640・行 81〜210) と自分の札 (x 36〜322・行 378〜476) に掛かる画素 (place2 の phoneUi と同じ矩形)"""
        b = self.box(p, 'PH')
        if b is None:
            return 0
        n = 0
        for r in ((0.0, 81.0, 640.0, 129.0), (36.0, 378.0, 286.0, 98.0)):
            w = min(b[2], r[0] + r[2]) - max(b[0], r[0])
            h = min(b[3], r[1] + r[3]) - max(b[1], r[1])
            if w > 0 and h > 0:
                n += w * h
        return n

    def band_ok(self, p):
        fp = place2.footprint(self.L, p, phone=False)
        fq = place2.footprint(self.L, p, phone=True)
        return not ((fp and place2.poly_hits_band(fp)) or (fq and place2.poly_hits_band(fq)))


def overlaps(b, boxes, frac=0.25):
    """画面の箱 b が boxes のどれかと、小さい方の面積の frac を超えて重なる"""
    if b is None:
        return False
    for q in boxes:
        w = min(b[2], q[2]) - max(b[0], q[0])
        h = min(b[3], q[3]) - max(b[1], q[1])
        if w > 0 and h > 0:
            a = min((b[2] - b[0]) * (b[3] - b[1]), (q[2] - q[0]) * (q[3] - q[1]))
            if a > 0 and w * h > frac * a:
                return True
    return False


def place_props(L, judge, ov, sd, log, fixed, only=None):
    """PROPS を規則に当てて置く。戻り値 (置いた部品, {PROPS の番号: (s の候補, t のずらし)})。
    only (本番が選んだ番号と置き場) を渡すと、その番号の物だけを、本番と同じ置き場から先に試す (張り出しの変種で小物の集合を本番にそろえる)"""
    placed = []
    picked = {}
    taken = [judge.box(p) for p in fixed]   # 画面の箱 (PC) で重なりを避ける (炉・提灯・柱・坑口など先に置いた物)
    taken = [b for b in taken if b is not None]
    for idx, (src, t, s, tier, flip) in enumerate(PROPS):
        if only is not None and idx not in only:
            continue
        best = None
        s_alts = [s, TIER2_FRONT - 0.15] if tier == 'tier1' else [s]   # 一段目の奥 (s 7.75) = 足元を少しでも上へ
        alts = ALTS
        if tier == 'hang':
            alts = [a for a in ALTS if t + a >= HANG_T_MIN - 1e-6]          # 天井から吊る物は t ≥16 (計画 §2 C = 幕ボス 160 の頭 x 1200〜1550 の真上を空ける)
        if src == 'crystalBig':
            alts = [0.0, -0.4, 0.4, -0.8]                                   # 大結晶は真ん中 (主人公の列の外・提灯 c と柱 b の間) だけ
        cands = [(sa, dt) for sa in s_alts for dt in alts]
        if only is not None:
            cands = [only[idx]] + [c for c in cands if c != only[idx]]
        hit = None
        for sa, dt in cands:
            p = prop_part(src, t + dt, sa, tier, flip, ov, sd, L, judge.G)
            if overlaps(judge.box(p), taken):
                continue
            if not judge.band_ok(p):
                continue
            pr = judge.problems(p)
            if not pr:
                best = p
                hit = (sa, dt)
                # スマホでからくりの帯・自分の札に 3 割以上隠れる物、上部バー (行 0〜73) の裏に 4 割以上入る物は、スマホでは組まない (切れた絵を見せない)
                b = judge.box(p, 'PH')
                if b is not None and 'phone' not in p:
                    area = max(1.0, (b[2] - b[0]) * (b[3] - b[1]))
                    top_hidden = max(0.0, min(b[3], 73.5) - b[1]) * (b[2] - b[0])
                    if judge.phone_ui_hits(p) > 0.3 * area or top_hidden > 0.4 * area:
                        best = dict(p)
                        best['phone'] = dict(hide=True)
                        best['_why'] = 'ui'
                break
            # スマホだけ掛かるなら、スマホでは組まない
            if all(q[0] == 'PH' for q in pr) and 'phone' not in p:
                p2 = dict(p)
                p2['phone'] = dict(hide=True)
                if not judge.problems(p2):
                    best = p2
                    hit = (sa, dt)
                    break
        if best is None:
            log.append('外した: %s (t %.1f・s %.1f・%s) = どのずらしでも規則に掛かる%s' % (src, t, s, tier, '（本番には置いた）' if only is not None else ''))
            continue
        picked[idx] = hit
        if only is not None:
            if hit != only[idx]:
                log.append('本番と違う置き場: %s (t %.2f・s %.2f)' % (src, best['t'], best['s']))
        elif abs(best['t'] - t) > 1e-6 or (tier == 'tier1' and abs(best['s'] - min(max(s, TIER1_FRONT + 0.25), TIER2_FRONT - 0.15)) > 1e-6):
            log.append('動かした: %s (t %.1f・s %.1f) → (t %.2f・s %.2f) (%s)' % (src, t, s, best['t'], best['s'], tier))
        if best.get('phone', {}).get('hide') and tier not in ('hang', 'lower'):
            why = 'スマホの UI (からくりの帯・自分の札・上部バー) の裏で切れる' if best.pop('_why', None) else 'スマホの座席の通りに掛かる'
            log.append('スマホでは組まない: %s (t %.2f) = %s' % (src, best['t'], why))
        placed.append(best)
        b = judge.box(best)
        if b is not None:
            taken.append(b)
    return placed, picked


# 名前つきの灯と炉のスマホの置き場の候補 (先頭から試す。どれもだめならスマホでは組まない)
# 提灯は棚の前 (s 9.28) の二段目の床 (y 1.8) の 0.03 上に置く (PHONE_LANTERN_Y。棚の縁から吊る y 1.45 は下 0.35 が床に沈んでいた)
PHONE_FIT = {
    'lantern-a': [dict(t=tt, s=9.28, y=PHONE_LANTERN_Y) for tt in (0.8, 1.1, 0.5, 1.4)],
    'lantern-b': [dict(t=tt, s=9.28, y=PHONE_LANTERN_Y) for tt in (21.0, 21.4, 20.6, 21.8)],
    'lantern-c': [dict(t=tt, s=9.28, y=PHONE_LANTERN_Y) for tt in (4.7, 4.5, 4.3, 3.9)],
    # 炉は今の t のまま (候補 {}) を先に: 火 (暈の中心) が行 ≈232 = からくりの帯 (行 81〜210) の下に出て、煙突だけ帯の裏。右へ寄せると主人公の通り・
    # 二段目へ移すと提灯 a・c と大結晶に重なる (統合の撮影で帯の裏の煙突が気になれば次の候補)
    'hearth': [dict(), dict(t=1.2, s=8.05), dict(t=1.6, s=8.05)],
}
STRIP_OK = {'hearth'}   # からくりの帯の裏を許す (火 = 暈がその下に出ること)


def fit_phone(P, judge, log):
    """灯と炉のスマホの置き場を、スマホの座席の通り・意図の札・からくりの帯・自分の札・ほかの物 (柱・坑口) に掛からない所へ。暈は親に付いていく"""
    by = {p.get('name'): p for p in P}
    others = [p for p in P if (p.get('name') or '') in ('post-a', 'post-b', 'post-c')]
    for nm, cands in PHONE_FIT.items():
        p = by.get(nm)
        if p is None:
            continue
        chosen = None
        halo = by.get(nm + '-halo')
        for c in cands:
            q = dict(p)
            if c:
                q['phone'] = dict(c)
            else:
                q.pop('phone', None)
            pr = [x for x in judge.problems(q) if x[0] == 'PH']
            ui_ok = judge.phone_ui_hits(q) == 0
            if not ui_ok and nm in STRIP_OK and halo is not None:
                hq = dict(halo)
                if c:
                    hq['phone'] = dict(t=c.get('t', halo['t']), s=r3(c.get('s', p['s']) - (p['s'] - halo['s'])))
                else:
                    hq.pop('phone', None)
                hb = judge.box(hq, 'PH')
                ui_ok = hb is not None and (hb[1] + hb[3]) / 2 > 215   # 火 (暈の中心) が帯の下
            if ui_ok and overlaps(judge.box(q, 'PH'), [b for b in (judge.box(o, 'PH') for o in others) if b is not None], 0.1):
                ui_ok = False
            if not pr and ui_ok:
                chosen = c
                break
        if chosen is None:
            p['phone'] = dict(hide=True)
            if halo is not None:
                halo['phone'] = dict(hide=True)
            log.append('スマホでは組まない: %s (どの候補もスマホの座席の通りか UI の帯に掛かる)' % nm)
            continue
        if not chosen:
            p.pop('phone', None)
            if halo is not None:
                halo.pop('phone', None)
            log.append('スマホの置き場: %s → PC と同じ (t %s)' % (nm, p['t']))
            continue
        p['phone'] = dict(chosen)
        if halo is not None:
            dy = halo['y'] - (p.get('y') or 0.0)
            hp = dict(t=chosen['t'], s=r3(chosen['s'] - (p['s'] - halo['s'])))
            if 'y' in chosen:
                hp['y'] = r3(chosen['y'] + dy)
            halo['phone'] = hp
        log.append('スマホの置き場: %s → %s' % (nm, json.dumps(chosen)))


def audit_fixed(P, judge, log):
    """先に置いた物 (小物と小札の外) の規則の掛かり (焼いた後の place2 でも数える。ここではログに出すだけ)"""
    for p in P:
        if p['kind'] in ('slab', 'mist', 'litter') or (p.get('name') or '').startswith(place2.NO_SILHOUETTE_NAMES):
            continue
        pr = judge.problems(p)
        if pr:
            log.append('規則に掛かる (先に置いた物): %s %s' % (p.get('name') or p.get('src'), pr[:3]))


LITTER_ZONES = [   # (t の範囲, s の範囲, 枚数, 段) 座席の帯の奥と前の縁・一段目・二段目・左の空き
    ((-14.0, 24.0), (2.6, 4.75), 22, 'seat'),
    ((-14.0, 24.0), (SEAT_FRONT + 0.25, SEAT_FRONT + 0.9), 12, 'seat'),
    ((-16.0, -8.6), (-2.6, 2.6), 6, 'seat'),
    ((-14.0, 26.0), (TIER1_FRONT + 0.3, TIER2_FRONT - 0.2), 18, 'tier1'),
    ((-14.0, 26.0), (TIER2_FRONT + 0.15, SHELF_FRONT - 0.1), 12, 'tier2'),
    ((-18.0, 26.0), (SEAT_FRONT - 4.0, SEAT_FRONT - 0.6), 6, 'lower'),
]


def place_litter(L, judge, rng, sd, log):
    names = list(LITTER_SRC.keys())
    weights = []
    for n in names:
        weights.append(3.0 if 'Gravel' in n else 3.0 if 'Pebble' in n else 1.0 if 'Gear' in n else 1.6)
    out = []
    rejected = 0
    for (tr, sr, n, tier) in LITTER_ZONES:
        k = 0
        tries = 0
        while k < n and tries < n * 40:
            tries += 1
            t = tr[0] + rng.random() * (tr[1] - tr[0])
            s = sr[0] + rng.random() * (sr[1] - sr[0])
            src = rng.choices(names, weights)[0]
            p = dict(kind='litter', src=src, t=r3(t), s=r3(s), yaw=round((rng.random() - 0.5) * 30, 1), flip=rng.random() < 0.5, seed=sd())
            if tier == 'lower':
                p['phone'] = dict(hide=True)
            if any(abs(p['t'] - q['t']) < 0.55 and abs(p['s'] - q['s']) < 0.35 for q in out):
                continue
            if judge.problems(p, litter=True):
                rejected += 1
                continue
            out.append(p)
            k += 1
        if k < n:
            log.append('小札: %s の帯に %d/%d 枚 (足元の通りで置けない所が多い)' % (tier, k, n))
    log.append('小札: %d 枚 (足元の通りで外した候補 %d)' % (len(out), rejected))
    return out


# ------------------------------------------------------------------ 組み立て

def build(name, judge_cache):
    ov = OVERHANG[name]
    only = None
    if name in ('act2_layout_wall3', 'act2_layout_wall4'):
        # 張り出しの変種の小物は本番と同じ集合・同じ置き場を先に (張り出しを比べる撮影に小物の差を混ぜない)。本番をまだ焼いていなければ先に選ぶ
        if 'picks:act2_layout' not in judge_cache:
            build('act2_layout', judge_cache)
        only = judge_cache['picks:act2_layout']
    rng = random.Random(SEED)
    sd = Seeds(SEED * 10)
    L = head(name, ov)
    L['sources'] = sources()
    log = []
    P = []
    P += slabs(rng, sd)
    P += wall(rng, sd, ov)
    if name != 'act2_layout_min':
        P += mouth(sd, ov, P)
    P += lanterns(sd, ov, P, with_c=(name != 'act2_layout_min'))
    key = 'ov%.1f' % ov
    if name == 'act2_layout_min':
        L['parts'] = P
        if key not in judge_cache:
            judge_cache[key] = Judge(L)
        judge_cache[key].L = L
        fit_phone(P, judge_cache[key], log)
        audit_fixed(P, judge_cache[key], log)
        _prune_sources(L)
        return L, log
    P += hearth(sd)
    P += timber(sd, ov)
    P += gallery(rng, sd, ov)
    P += rails(sd)
    P += edges(rng, sd)
    P += mists()
    P += frames(sd)
    # 小物と小札は、段と壁を入れた設計図で規則を当てて選ぶ (地面の高さ・隠れ具合は段で決まる)
    L['parts'] = P
    if key not in judge_cache:
        judge_cache[key] = Judge(L)
    judge = judge_cache[key]
    judge.L = L
    fit_phone(P, judge, log)
    audit_fixed(P, judge, log)
    # 重なりを避ける相手 = 小さくまとまった物だけ (歩廊の床・手すり・梁・鉤のような細長い物の外接の箱は大きすぎて、奥の物まで弾く)
    fixed = [p for p in P if p['kind'] in ('relief', 'card') or (p.get('name') or '') in ('post-a', 'post-b', 'post-c', 'mouth-post-L', 'mouth-post-R')]
    props, picks = place_props(L, judge, ov, sd, log, fixed, only=only)
    if name == 'act2_layout':
        judge_cache['picks:act2_layout'] = picks
    P += props
    P += place_litter(L, judge, random.Random(SEED + 77), sd, log)
    L['parts'] = P
    _prune_sources(L)
    return L, log


def _prune_sources(L):
    used = {p.get('src') for p in L['parts'] if p.get('src')}
    L['sources'] = {k: v for k, v in L['sources'].items() if k in used}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--out-dir', default=STAGE)
    ap.add_argument('--only', default=None)
    ap.add_argument('--no-check', action='store_true')
    ap.add_argument('--allow-missing-ui', action='store_true', help='基準の撮影の UI の矩形が無くても続ける (警告だけ。R3 と UI の帯の判定が甘くなる)')
    a = ap.parse_args()
    place2.ALLOW_MISSING_UI = a.allow_missing_ui
    try:
        return run(a)
    except place2.MissingUIError as e:
        print('止めた: %s (--allow-missing-ui で続ける)' % e, file=sys.stderr)
        for f in e.files:
            print('  無い: ' + f, file=sys.stderr)
        return 1


def run(a):
    names = ['act2_layout', 'act2_layout_wall3', 'act2_layout_wall4', 'act2_layout_min']
    if a.only:
        names = [n for n in names if n in a.only.split(',')]
    cache = {}
    paths = []
    for nm in names:
        L, log = build(nm, cache)
        fp = os.path.join(a.out_dir, nm + '.json')
        with open(fp, 'w') as f:
            f.write(dump_layout(L))
        paths.append(fp)
        from collections import Counter
        kinds = Counter(p['kind'] for p in L['parts'])
        print('%s: 部品 %d (%s)' % (nm, len(L['parts']), '・'.join('%s %d' % kv for kv in sorted(kinds.items(), key=lambda kv: -kv[1]))))
        for ln in log:
            print('   ' + ln)
    if not a.no_check:
        os.makedirs(place2.LANE_DIR, exist_ok=True)
        rc = 0
        res = []
        for fp in paths:
            L = json.load(open(fp))
            tag = os.path.splitext(os.path.basename(fp))[0]
            r = place2.check(L, want_img=True, out_dir=place2.LANE_DIR, tag=tag)
            res.append(r)
            with open(os.path.join(place2.LANE_DIR, 'place2-%s.json' % tag), 'w') as f:
                json.dump(r, f, ensure_ascii=False, indent=1)
            print('%s: 違反 %d' % (tag, len(r['violations'])))
            for v in r['violations'][:30]:
                print('   ' + v)
            rc |= 1 if r['violations'] else 0
        with open(os.path.join(place2.LANE_DIR, 'place2-report.md'), 'w') as f:
            f.write(place2.summary_md(res))
        return rc
    return 0


if __name__ == '__main__':
    sys.exit(main())

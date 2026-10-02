#!/usr/bin/env python3
"""HD-2D 段2 レーン C3: 幕3「埋もれた古代都市」の箱庭の設計図 act3_layout*.json を焼く
(計画 docs/design/hd2d-stage2-plan-2026-10-02.md §0 裁定 6・§1 幕3 の合否・§2 C の段1b・約束 docs/design/hd2d-stage2/contracts.md C0〜C3・
分析 docs/design/hd2d-stage2-analysis-2026-10-02.md §5-1・§5-2)。

幕2 の gen_act2.py と同じ作り: カメラ・地面・部品の写し方は幕1 の docs/design/hd2d-slice/r2-layout-gen/place.py、規則の当て方は gen_act2.Judge
(place2 の R1〜R4・R6・R7) を import する (place.py・gen_act2.py・place2.py は直さない)。焼いた後に place3.py (place2 の検査＋幕3 の規則) で数える。
乱数は固定の種 SEED (決定的)。D3 の絵 (Art/stage/act3/) があればその大きさで、無ければ元の Art/props/act3_* の大きさで判断する。

物差しは「ot7 の戦闘の作り＋Tomb の材質」(裁定 6)。光は主人公の真後ろに斜めの柱・足元が頂点 (光そのものはレーン B の look_act3*.json)。
この設計図は光の置き場 (名前つきの部品・柱の光の面 shaft・暈) と、光を受ける面 (段の立面・奥の壁・門・柱) を置く。

出力 (unity/Assets/Resources/Stage/。--out-dir で変えられる)
  act3_layout.json          本番
  act3_layout_tier.json     段の高さを t で変える変種 (座席の外で左は T2〜T4 を 2 段・右は 3 段の高さを変える。block の嵩上げ wall-tier-)
  act3_layout_stairs.json   斜めの大階段を強める変種 (幅 3.0→4.2・1 段 0.25→0.2・段ごとの側壁)
  act3_layout_pillar1.json  光の柱の足を t −3.0 → −2.4 へ (レーン B の backlight t −1.6 → −1.0 の変種 look_act3_pillar1 と対)

層 (道の座標 t・s・y。PC 22°・5°・足元 0.36 の行。分析 §5-1 の表)
  池        座席の帯の前 (s < −4.6・t −15〜6.5) の暗い底 pond-bed (top −1.1)。縁の立面 = 座席の帯の前の面 (y −1.25〜0)。左右は床 floor-front-L/R
            (端の面が池の左右の壁)。水面は作らない (water は試し撮りの後)。PC 行 ≈780〜950 (手札の上)。スマホは底を組まない (手札の裏)
  座席の帯  slab y 0 (s −4.6〜10.6・大石板 top_floor_seat)。t −8.5〜13・s −2.6〜2.8 は高さ 0・部品なし (小札だけ)。旧 H3 の水路は埋めた
  T1〜T4   slab y +1.0/+2.0/+3.0/+4.0・前の縁 s 9/13/17.5/22 (折れ ±0.3〜0.6)。立面 PC ≈75/69/63/57px。T4 の天面 = 目の高さ (3.93) なので見えない
  奥の壁    block の区切り (wall-back-) y 4〜11.6・前 s 27・奥行き 2.2。壁柱 (wall-pilaster-) で縦の拍。奥の保険 (wall-backstop) と頂 (wall-crown-)
  縞を断つ  斜めの大階段 (steps-・t 14 の帯・右 1/3)・控え壁 2 (buttress-)・崩落 1 セル (T2 の縁を 1.45 奥へ＋崩れ石 collapse-rock-＋瓦礫)
  門の軸    大門 arch (gate・T4 の上 s 26.25)＋暗い奥 (wall-gate-recess)＋左右の柱 (pillar-gate・pillar-gate-R = 計画の「柱 pillar 2」)
  光の柱    pillar-shaft-a/b (shaft・主人公の右後ろの床 t −3.0・s 7.3 に足・画面で右上から 17.5° 傾く・上は画面の外)。
            根元の裂け目の暈 crack は壁の面 (s 26.85) の、柱の上の延長が上部バーのすぐ下 (PC 行 ≈90) に来る所
  灯        篝火 fire1 (左の床)・fire2 (T4 の右)＝brazier の半立体＋暈 fire1-halo・fire2-halo。灯柱 lamp-post (pillar・T4・t ≥6.5)＋灯の頭 lamp-head＋暈 lampC。
            光そのものはレーン B が look_act3*.json の lights に at で書く (部品を動かすと光も付いてくる)
  小物      判定の画の小物 8 = 像 1・篝火 2・折れ柱 1・結晶 1・瓦礫 2・壺 1 (分析 §5-1)。鎖 2 (細く・上から垂れる)
  霧        壁の前の霧の板 mist-far 1 枚 (スマホでは組まない)。霧は柱の中と上端だけ (分析 §5-1)
  小札      座席の帯の奥と前の縁・段の天面に少しだけ (磨いた石の床 = 少なく)

材質 (surfaces) は 7 つ = terrain・step・wall・pillar・brick・relief・glow ＋ 霧の板の材質 (mist) = 8 (門 materialsMax 8)。card は書かない (札と小札は relief)。

使い方: gen_act3.py [--out-dir DIR] [--only NAME[,NAME]] [--no-check] [--allow-missing-ui]
  焼いた後に place3.py で全部を数え、表を ~/.cache/deck-rogue/hd2d-stage2/lanes/C3/place3-report.md に書く (--no-check で省く)
"""
import argparse
import json
import math
import os
import random
import sys
from collections import Counter

import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, '..', '..', '..', '..'))
sys.path.insert(0, os.path.join(REPO, 'docs', 'design', 'hd2d-slice', 'r2-layout-gen'))
sys.path.insert(0, HERE)
import place  # noqa: E402
from layoutio import dump_layout  # noqa: E402
import place2  # noqa: E402
import place3  # noqa: E402
from gen_act2 import Judge, Seeds, overlaps, r3, segments  # noqa: E402  (幕2 の道具をそのまま使う。gen_act2.py は直さない)

RES = os.path.join(REPO, 'unity', 'Assets', 'Resources') + '/'
STAGE = RES + 'Stage/'
A3 = 'Art/stage/act3/'
A2 = 'Art/stage/act2/'
SEED = 20261004
NAMES = ['act3_layout', 'act3_layout_tier', 'act3_layout_stairs', 'act3_layout_pillar1']

# ------------------------------------------------------------------ 層の数 (道の座標)

PATH_YAW = -22.0
T0, T1 = -40.0, 56.0          # 段の t の範囲 (21:9 の左右の端まで)
WT0, WT1 = -18.0, 50.0        # 奥の壁の t の範囲 (PC21 の左の端 t ≈ −14・右の端 t ≈ 43 @ s 27)
SEAT_FRONT = -4.6             # 座席の帯の前の縁 (池の縁)
POND_T0, POND_T1 = -15.0, 6.5
POND_Y = -1.1
SEAT_BOTTOM = -1.25
# 段: (名前, 天面 y, 前の縁 s, 奥 s)。奥は次の段の前の縁 (崩落の切り込みを含む) より奥まで重ねる (重なりは HeightAtPath が高い方を取る)
TIERS = [('T1', 1.0, 9.0, 15.2), ('T2', 2.0, 13.0, 18.9), ('T3', 3.0, 17.5, 23.3), ('T4', 4.0, 22.0, 29.6)]
# 段の前の縁の折れ (t0, t1, ds)。角は 0.32 の斜め (輪郭の折れ線 = art-bible §2)。大階段の帯 (t 11.4〜16.6) と控え壁の t には置かない
JOGS = {
    'T1': [(-19.0, -9.0, 0.45), (2.2, 7.4, -0.5), (22.0, 34.0, 0.35)],
    'T2': [(-15.0, -6.2, -0.45), (-2.4, 0.6, 0.4), (19.5, 30.0, -0.4)],
    'T3': [(-11.0, -2.4, 0.5), (6.6, 10.2, -0.4), (18.0, 26.0, 0.4)],
    'T4': [(-12.0, -4.6, -0.4), (6.4, 10.4, 0.4), (18.0, 24.0, -0.35)],
}
COLLAPSE = ('T2', 1.2, 3.6, 1.45)   # 崩落 1 セル (2.4 = タイル約 1 枚): T2 の縁を t 1.2〜3.6 だけ 1.45 奥へ (下の T1 の天面が切り込みに入る。敵の通り x ≥986 の外)
WALL_FRONT = 27.0
WALL_Y0, WALL_TOP = 4.0, 11.6
STAIR = dict(t=14.0, w=3.0, steps=4, tread=0.42, cheek=False)       # 大階段 (1 段 = 立面 1.0 / steps)
STAIR_STRONG = dict(t=14.0, w=4.2, steps=5, tread=0.36, cheek=True)
BUTTRESS = [('buttress-a', -8.0), ('buttress-b', 4.4)]                 # 控え壁 (T1 の天面から T4 の上まで)
GATE = dict(t=-1.0, s=26.25, w=5.0, h=6.0, d=1.1, pier=0.85)   # 主人公の列の上 (PC x ≈227〜518・行 −46〜301 = 主人公の頭 458 より上)
GATE_R_SEED = 202610049     # 門の右の柱の種 (固定。直しの輪 2026-10-03 で足した柱が後ろの部品の種をずらさないように)
PARAPET = (-0.6, 5.8)       # 胸壁の t (T3 の縁の上)
LAMP = (9.3, 22.7, 4.0)     # 灯柱の t・s・足元の高さ (T4 の上・館の右の端の前。PC x ≈980・行 ≈78〜293 = 敵の通りの頭 (行 ≥350) より上)
HOUSE = (8.2, 24.6, 3.6)    # 館の t・s・幅 (T4 の上・裂け目の右。PC x ≈765〜987 = 3 体の 1 体目の通り x 1040 の外)
SHAFT_FOOT = {'act3_layout_pillar1': (-2.4, 7.3)}                     # 光の柱の足 (t, s)。既定は SHAFT_FOOT_DEF
SHAFT_FOOT_DEF = (-3.0, 7.3)
SHAFT_LEAN = 17.5             # 画面で縦から右へ傾ける角 (度・右上から左下へ降りる。分析 §5-1・verify-facts §3-2)
SHAFT_TOP_Y = 11.0            # 光の柱の上の口 (abs。画面の上の外)
CRACK_ROW = 90.0              # 裂け目の暈の中心の PC の行 (上部バー 0〜72 のすぐ下)
WARM_FIRE = [1.0, 0.58, 0.28]
LAMP_C = [0.62, 0.8, 1.0]
VEIN_C = [0.62, 0.92, 0.88]


_A3_INDEX = None


def glow_y(relief, default):
    """D3 の Art/stage/act3/index.json の半立体の glow.y (光る所の中心・見えている絵の下端から・unit。D3 の約束「暈と lights の高さはこれを使う」)。
    無ければ default。art_for は絵を alpha>16 で切る (見えている絵の下端 = 置いた y) ので、暈の y = 板の y + glow.y"""
    global _A3_INDEX
    if _A3_INDEX is None:
        fp = RES + A3 + 'index.json'
        try:
            _A3_INDEX = json.load(open(fp)).get('relief', {})
        except (OSError, ValueError):
            _A3_INDEX = {}
    g = (_A3_INDEX.get(relief) or {}).get('glow') or {}
    v = g.get('y')
    return float(v) if isinstance(v, (int, float)) else default


def cam_pc():
    return place2.cams()[0]


def proj(cam, G, t, s, y):
    x, yy, d = cam.project(G.on_path(t, s, y))
    return float(x), float(yy), float(d)


# ------------------------------------------------------------------ 頂 (tiles・surfaces・sources)

def tile(names, rot, fallback, raw=None):
    """D3 のタイル (calm 済み) を先に、無ければ元の Art/tiles/act3_* を calm して使う候補列 (gen_act2.tile の幕3 版)"""
    vs = []
    for i, n in enumerate(names):
        cand = [A3 + 'tiles/' + n]
        if raw:
            r = raw[i % len(raw)]
            cand.append(dict(art=r[0], calm=r[1], sat=r[2], clampWhite=True))
        vs.append(cand)
    return dict(rot=rot, fallback=fallback, variants=vs)


def head(name):
    abcd = 'abcd'
    T = 'Art/tiles/act3_'
    cliff = [(T + 'cliff_a', 0.33, 0.8), (T + 'cliff_b', 0.33, 0.8), (T + 'cliff_d', 0.33, 0.8), (T + 'cliff_c', 0.33, 0.8)]
    cliff_step = [(T + 'cliff_c', 0.36, 0.8), (T + 'cliff_a', 0.36, 0.8), (T + 'cliff_b', 0.36, 0.8), (T + 'cliff_d', 0.36, 0.8)]
    stone = [(T + 'stone_a', 0.3, 0.6), (T + 'stone_b', 0.3, 0.6), (T + 'stone_c', 0.3, 0.6), (T + 'stone_d', 0.3, 0.6)]
    tiles = {
        'side_wall': tile(['side_wall_%s' % c for c in abcd], 'flipX', [44, 60, 70], cliff),
        'top_wall': tile(['top_wall_%s' % c for c in abcd], 'flipX', [54, 66, 74], [(r[0], 0.25, 0.6) for r in stone]),
        'side_step': tile(['side_step_%s' % c for c in abcd], 'flipX', [50, 64, 74], cliff_step),
        'top_step': tile(['top_step_%s' % c for c in abcd], 'flipX', [66, 78, 86], [(r[0], 0.25, 0.6) for r in stone]),
        'top_floor_seat': tile(['top_floor_seat_%s' % c for c in abcd], 'flipX', [74, 84, 92], [(r[0], 0.32, 0.6) for r in stone]),
        'side_pillar': tile(['side_pillar_%s' % c for c in abcd], 'flipX', [88, 94, 92], [(T + 'cliff_d', 0.4, 0.7), (T + 'cliff_b', 0.4, 0.7)]),
        'top_pillar': tile(['top_pillar_%s' % c for c in abcd], 'flipX', [92, 96, 94], [(T + 'stone_a', 0.3, 0.6)]),
        'side_brick': tile(['side_brick_%s' % c for c in abcd], 'flipX', [56, 64, 74], [(T + 'dirt_a', 0.35, 0.8), (T + 'dirt_b', 0.35, 0.8)]),
    }
    surfaces = {
        'terrain': dict(shader='array', side='side_step', top='top_floor_seat', receive=0.8, shadowStrength=1.0, topThreshold=0.65, topBlend=0.15, vcolorAO=1.0),
        'step': dict(shader='array', side='side_step', top='top_step', receive=0.8, shadowStrength=1.0, topThreshold=0.6, topBlend=0.12, vcolorAO=1.0),
        'wall': dict(shader='array', side='side_wall', top='top_wall', receive=0.75, shadowStrength=1.0, topThreshold=0.6, topBlend=0.12, vcolorAO=1.0),
        'pillar': dict(shader='array', side='side_pillar', top='top_pillar', receive=0.8, shadowStrength=1.0, topThreshold=0.6, topBlend=0.1, vcolorAO=1.0),
        'brick': dict(shader='array', side='side_brick', top='top_wall', receive=0.75, shadowStrength=1.0, topThreshold=0.6, topBlend=0.12, vcolorAO=1.0),
        'relief': dict(shader='atlas', receive=0.6, shadowStrength=1.0, cutoff=0.4, vcolorAO=1.0, normal=0.6, sway=dict(amp=0.03, freq=[0.25, 0.6])),
        # 暈の色は頂点色 × glow の tint × intensity (幕2 の統合で tint を白にした。暈ごとの色は部品の color)。光の柱の明るさは B の materials.glow.shaftGain
        'glow': dict(shader='shaft', tint=[1.0, 1.0, 1.0], intensity=1.2, softDepth=1.5, nearFade=2.0, edgeFade=0.0, fog=0.25, lobeFloor=1.0),
    }
    doc = ('幕3「埋もれた古代都市」の箱庭の設計図 (HD-2D 段2・段1b。計画 docs/design/hd2d-stage2-plan-2026-10-02.md §2 C・約束 docs/design/hd2d-stage2/contracts.md C3 の幕3 版)。'
           '作り方は docs/design/hd2d-stage2/layout-gen/gen_act3.py (種 %d・手で直さず焼き直す)・規則の検査は place3.py。物差しは「ot7 の戦闘の作り＋Tomb の材質」(裁定 6)。'
           '座標は幕1 と同じ道の座標 (t = 道に沿った距離・s = 道と直角 (+ が奥)・y = 地面からの高さ (abs なら絶対))。'
           '層: 池の底 −1.1 (水面なし)・座席の帯 0 (s −4.6〜10.6・大石板)・T1 +1.0 (s 9)・T2 +2.0 (s 13)・T3 +3.0 (s 17.5)・T4 +4.0 (s 22)・奥の壁 block y 4〜11.6 (前 s 27)。'
           '縞を断つ部品: 斜めの大階段 steps- (t %g・右 1/3)・控え壁 2 buttress-・崩落 1 セル (%s の縁 t %g〜%g を %g 奥へ)。段は slab・天面は作らない。'
           '光の部品の名前 (fire1・fire2・fire1-halo・fire2-halo・lampC・crack・pillar-shaft-a・pillar-shaft-b) はレーン B の look_act3*.json の lights の at が読む '
           '(動かすと光も付いてくる)。材質は 7 つ + 霧の板 = 8 (card は書かない = 札と小札は relief)。'
           '絵は Art/stage/act3/ (レーン D3) を先に、無ければ元の Art/props/act3_*・Art/tiles/act3_* (calm)'
           % ((SEED, STAIR['t']) + tuple(COLLAPSE)))
    pre = {
        'act3_layout_tier': '段の高さを t で変える変種 (計画 §3 手順 5・分析 §5-1「縞を断つ」)。座席の外で、左 (t ≤ −3.4) は T2〜T4 を 2 段 (1→3→4)、'
                            '右 (t ≥ 17.2) は 3 段の高さを変える (1→2.5→3.6→4)。嵩上げは block (wall-tier-) で、端の面が段の縞を縦に切る。look_act3_tier.json の "layout" が指す。',
        'act3_layout_stairs': '斜めの大階段を強める変種 (計画 §3 手順 5)。幅 3.0→4.2・1 段 0.25→0.2・段ごとの側壁 (steps-cheek-)。look_act3_stairs.json の "layout" が指す。',
        'act3_layout_pillar1': '光の柱を t −1 にする変種 (計画 §3 手順 5「柱 t −1／−1.6」)。光の柱の面 pillar-shaft-a/b の足を t {foot0} → {foot1}・'
                               '裂け目 crack は柱の中心の線を上へ延ばした点に付いていく (t {crack0} → {crack1}・y {cracky0} → {cracky1}。足の差より大きく動く) '
                               '(レーン B の look_act3_pillar1.json が backlight を t −1.6 → −1.0 にして "layout" でこれを指す)。',
    }
    if name in pre:
        doc = pre[name] + doc
    # 部品の門: 幕3 は判定の画の小物を 8 以下にする (分析 §5-1・本家 Tomb の中景に小物はほぼ無い) ので幕2 の 160 より下げる (本番 ≈145)
    gates = dict(partsMin=130, partsMax=300, litterMax=120, materialsMax=8)
    return dict(_doc=doc, version=1, act=3, pathYaw=PATH_YAW, tile=64, albedoLinear=True, normalStrength=0.6, staticBatch=True,
                gates=gates, tiles=tiles, surfaces=surfaces, sources={})


# 半立体の元の絵 (source の名前 → D3 の relief の名前)。art は D3 の絵を先に、無ければ元の Art/props/act3_*
RELIEF_SRC = {
    'statue': 'statue', 'brazier': 'brazier', 'brazierCold': 'brazier_cold', 'crystalCluster': 'crystal_cluster',
    'pillarBroken': 'pillar_broken', 'pillarFallen': 'pillar_fallen', 'rubblePile': 'rubble_pile', 'urn': 'urn',
    'chainHang': 'chain_hang', 'stele': 'stele', 'lampPost': 'lamp_post', 'lampHang': 'lamp_hang', 'lampHead': 'lamp_head',
}
# 小札: D3 の Art/stage/act3/litter/<名前>_<n> を先に、無ければ幕2 の同じ名前 (色が幕2 の色表なので仮)
LITTER_SRC = {'litterPebble%d' % i: 'pebble_%d' % i for i in range(1, 5)}
LITTER_SRC.update({'litterGravel%d' % i: 'gravel_%d' % i for i in range(1, 5)})
LITTER_A3_ONLY = {'litterChip%d' % i: 'chip_%d' % i for i in range(1, 5)}   # D3 だけにある名前 (幕2 に同じ名前が無い)
LITTER_A3_ONLY.update({'litterRubble%d' % i: 'rubble_%d' % i for i in range(1, 5)})


def sources():
    s = {}
    for k, n in RELIEF_SRC.items():
        s[k] = dict(art=[A3 + 'relief/' + n, 'Art/props/act3_' + n], depth=0.12, cells=40)
    for k, n in LITTER_SRC.items():
        s[k] = dict(art=[A3 + 'litter/' + n, A2 + 'litter/' + n], depth=0.0, cells=8, flat=True)
    for k, n in LITTER_A3_ONLY.items():
        s[k] = dict(art=[A3 + 'litter/' + n], depth=0.0, cells=8, flat=True)
    return s


# ------------------------------------------------------------------ 道具

def front_line(rng, base, jogs, t0=T0, t1=T1, amp=0.05, step=1.28, corner=0.32):
    """段の前の縁の折れ線: base に jogs (t0, t1, ds) の折れ (角は corner の斜め) と、小さな揺れ (±amp・格子 step)"""
    def ds_at(t):
        d = 0.0
        for a, b, v in jogs:
            if a - corner / 2 <= t <= b + corner / 2:
                if t < a + corner / 2:
                    k = (t - (a - corner / 2)) / corner
                elif t > b - corner / 2:
                    k = ((b + corner / 2) - t) / corner
                else:
                    k = 1.0
                d += v * max(0.0, min(1.0, k))
        return d
    ts = set()
    t = t0
    while t <= t1 + 1e-6:
        ts.add(round(t, 3))
        t += step
    for a, b, v in jogs:
        for tt in (a - corner / 2, a + corner / 2, b - corner / 2, b + corner / 2):
            ts.add(round(tt, 3))
    ts = sorted(ts)
    out = []
    for t in ts:
        if out and t - out[-1][0] < 0.05:
            continue
        near_corner = any(abs(t - c) < 0.2 for a, b, v in jogs for c in (a - corner / 2, a + corner / 2, b - corner / 2, b + corner / 2))
        w = 0.0 if near_corner else (rng.random() - 0.5) * 2 * amp
        out.append([r3(t), r3(base + ds_at(t) + w)])
    return out


def front_at(front, t):
    T = np.array([q[0] for q in front])
    S = np.array([q[1] for q in front])
    return float(np.interp(t, T, S))


# ------------------------------------------------------------------ 層

def terraces(rng, sd, name):
    """池・座席の帯・左右の床・T1〜T4。戻り値 (部品, {段の名前: 前の縁の折れ線})"""
    P = []
    fronts = {}
    # 座席の帯: 前の縁 s −4.6 = 池の縁の立面 (y −1.25〜0)。奥は T1 の前の縁より奥まで
    # gridT 0.64: 斑の格子を t の向きに 0.64 (既定は grid 0.32 = 幅 96 の帯で 300 行×48 列。幕2 の帯は 230×31) = 頂点を半分に
    P.append(dict(kind='slab', name='seat-band', surface='terrain', top=0.0, bottom=SEAT_BOTTOM, back=10.6, grid=0.32, gridT=0.64, chamfer=0.0,
                  front=[[T0, SEAT_FRONT], [T1, SEAT_FRONT]], mottle=dict(scale=[2, 5], amount=0.06, seed=sd()), seed=sd()))
    # 池の左右の床 (座席の帯と同じ高さ)。前の縁の折れ線の端の 1 区間が池の左右の壁 (左は +t・右は −t を向く = カメラ t ≈ −10 から見える側)
    P.append(dict(kind='slab', name='floor-front-L', surface='terrain', top=0.0, bottom=SEAT_BOTTOM, back=SEAT_FRONT + 0.05, grid=1.28, chamfer=0.0,
                  front=[[T0, -30.0], [POND_T0 - 0.3, -30.0], [POND_T0, SEAT_FRONT]]))
    P.append(dict(kind='slab', name='floor-front-R', surface='terrain', top=0.0, bottom=SEAT_BOTTOM, back=SEAT_FRONT + 0.05, grid=1.28, chamfer=0.0,
                  front=[[POND_T1, SEAT_FRONT], [POND_T1 + 0.3, -30.0], [T1, -30.0]]))
    # 池の暗い底 (水面は作らない)。スマホは手札の裏なので組まない
    P.append(dict(kind='slab', name='pond-bed', surface='step', top=POND_Y, bottom=POND_Y - 0.5, back=SEAT_FRONT + 0.1, grid=1.28, chamfer=0.0,
                  front=[[POND_T0 - 0.2, -30.0], [POND_T1 + 0.2, -30.0]], tint=0.32, phone=dict(hide=True)))
    prev_top = 0.0
    for nm, top, fs, back in TIERS:
        jogs = list(JOGS[nm])
        if COLLAPSE[0] == nm:
            jogs.append(COLLAPSE[1:])
        fr = front_line(rng, fs, jogs)
        fronts[nm] = fr
        P.append(dict(kind='slab', name='tier-%s' % nm, surface='step', top=top, bottom=r3(prev_top - 0.1), back=back, grid=0.64, chamfer=0.08,
                      front=fr, mottle=dict(scale=[2, 5], amount=0.08, seed=sd())))
        prev_top = top
    return P, fronts


def wall(rng, sd, keep_out):
    """奥の壁 (block の区切り・前 s 27)・壁柱・頂・奥の保険。名前は wall- (place2 の R2・R3 は背景として数えない)"""
    P = []
    for i, (tc, w) in enumerate(segments(rng, WT0, WT1, 4.5, 9.0)):
        front = WALL_FRONT + rng.random() * 0.15
        top = WALL_TOP + (rng.random() - 0.5) * 0.5
        P.append(dict(kind='block', name='wall-back-%d' % i, surface='wall', t=r3(tc), s=r3(front + 1.1), y=WALL_Y0, abs=True, yaw=0,
                      w=r3(w + 0.05), d=2.2, h=r3(top - WALL_Y0), cut=0.2, chamfer=0.08, sink=0.2, aoAmount=0.5, seed=sd()))
    # 壁柱 (縦の拍。本家 Tomb の壁の区画)。門・裂け目・館の t (keep_out) は空ける
    t = WT0 + 2.0
    k = 0
    while t < WT1 - 1.0:
        if not any(a0 < t < a1 for a0, a1 in keep_out):
            P.append(dict(kind='block', name='wall-pilaster-%d' % k, surface='wall', t=r3(t), s=r3(WALL_FRONT - 0.18), y=WALL_Y0, abs=True, yaw=0,
                          w=0.95, d=0.5, h=r3(WALL_TOP + 0.2 - WALL_Y0), cut=0.08, chamfer=0.05, sink=0.2, aoAmount=0.5, seed=sd()))
            k += 1
        t += 6.5 + rng.random() * 2.5
    # 頂 (上の端の保険・PC21 の右の端は y 11.6 が行 ≈0)。スマホは画面の外 (s 27 の上端 y ≈6)
    for i, (tc, w) in enumerate(segments(rng, WT0, WT1, 10.0, 16.0)):
        P.append(dict(kind='block', name='wall-crown-%d' % i, surface='wall', t=r3(tc), s=r3(WALL_FRONT + 1.6), y=r3(WALL_TOP - 0.4), abs=True, yaw=0,
                      w=r3(w + 0.05), d=2.0, h=4.4, cut=0.2, chamfer=0.08, sink=0.0, aoAmount=0.0, tint=0.7, phone=dict(hide=True), seed=sd()))
    P.append(dict(kind='block', name='wall-backstop', surface='wall', t=16.0, s=34.0, y=WALL_Y0, abs=True, yaw=0, w=90.0, d=3.0, h=14.0,
                  cut=0.0, chamfer=0.0, sink=0.2, aoAmount=0.0, tint=0.5, shadow=False, phone=dict(hide=True), seed=sd()))
    return P


def stairs(sd, fronts, cfg):
    """斜めの大階段 (右 1/3)。道の向きに沿って奥へ上る (t 一定) = 画面では右下から左上へ斜めに段の縞を切る。
    立面ごとに cfg['steps'] − 1 段の block (1 段の高さ = 1 / steps・踏み面 tread)。block は前の縁から立面の 0.1 奥まで (隙間を作らない)。名前 steps-"""
    P = []
    n = cfg['steps']
    h1 = 1.0 / n
    tc, w, tr = cfg['t'], cfg['w'], cfg['tread']
    for ri, (nm, top, fs, back) in enumerate(TIERS):
        sf = front_at(fronts[nm], tc)
        base = top - 1.0
        for k in range(1, n):
            f0 = sf - (n - k) * tr
            s1 = sf + 0.1
            P.append(dict(kind='block', name='steps-%s-%d' % (nm, k), surface='step', t=r3(tc), s=r3((f0 + s1) / 2), y=r3(base), abs=True, yaw=0,
                          w=r3(w), d=r3(s1 - f0), h=r3(h1 * k), cut=0.0, chamfer=0.03, sink=0.05, aoAmount=0.6, seed=sd()))
            if cfg['cheek']:
                for side, dt in (('L', -1), ('R', 1)):
                    P.append(dict(kind='block', name='steps-cheek-%s-%s-%d' % (side, nm, k), surface='step', t=r3(tc + dt * (w / 2 + 0.18)),
                                  s=r3(f0 + tr / 2), y=r3(base), abs=True, yaw=0, w=0.36, d=r3(tr + 0.02), h=r3(h1 * k + 0.32), cut=0.0, chamfer=0.03,
                                  sink=0.05, aoAmount=0.6, seed=sd()))
        if cfg['cheek']:   # 立面の上の端の側壁 (踏み面の上 0.32)
            for side, dt in (('L', -1), ('R', 1)):
                P.append(dict(kind='block', name='steps-cheek-%s-%s-top' % (side, nm), surface='step', t=r3(tc + dt * (w / 2 + 0.18)), s=r3(sf + 0.25),
                              y=r3(top - 0.3), abs=True, yaw=0, w=0.36, d=0.6, h=0.62, cut=0.0, chamfer=0.03, sink=0.0, aoAmount=0.6, seed=sd()))
    return P


def buttresses(sd, fronts):
    """控え壁 2: T1 の天面 (y 1) から T4 の天面の少し下 (y 3.9) まで、T2〜T4 の立面を縦に切る柱形の block。前は T2 の前の縁の 0.3 手前・奥は T4 の前の縁の 0.6 奥。
    天面を目の高さ (PC 3.93) より下にする: 上に出すと T4 の上の物 (館・瓦礫・灯柱の足元 = 行 ≈293) を天面の縁で隠す (試し: y 4.65 で瓦礫 b の 7 割が隠れた)"""
    P = []
    for nm, t in BUTTRESS:
        f2 = front_at(fronts['T2'], t)
        s0, s1 = f2 - 0.3, front_at(fronts['T4'], t) + 0.6
        P.append(dict(kind='block', name=nm, surface='wall', t=r3(t), s=r3((s0 + s1) / 2), y=1.0, abs=True, yaw=0, w=1.15, d=r3(s1 - s0), h=2.7,
                      cut=0.14, chamfer=0.06, sink=0.1, aoAmount=0.6, seed=sd()))
        P.append(dict(kind='block', name=nm + '-cap', surface='wall', t=r3(t), s=r3(s0 + 0.35), y=3.7, abs=True, yaw=0, w=1.4, d=0.8, h=0.2,
                      cut=0.06, chamfer=0.04, sink=0.0, aoAmount=0.0, seed=sd()))
    return P


def collapse(sd, fronts):
    """崩落 1 セル: T2 の縁の切り込みの足元 (T1 の天面) に崩れ石 (低い rock・背丈 < 1.2)。瓦礫の半立体は小物 (PROPS) で置く"""
    nm, t0, t1, ds = COLLAPSE
    tc = (t0 + t1) / 2
    sf = front_at(fronts[nm], tc)
    P = []
    for k, (dt, dsr, r, h, yaw) in enumerate([(-0.8, -0.35, 0.5, 0.42, 20.0), (0.35, -0.2, 0.62, 0.55, 140.0), (1.05, -0.5, 0.38, 0.3, 260.0)]):
        P.append(dict(kind='rock', name='collapse-rock-%d' % k, surface='step', t=r3(tc + dt), s=r3(sf + dsr), y=0.0, yaw=yaw, r=r, h=h, sides=7,
                      squash=0.8, tint=0.85, seed=sd()))
    return P


def tier_blocks(sd, fronts):
    """変種 tier: 座席の外で段の高さを t で変える嵩上げの block (名前 wall-tier- = 段と同じ背景)。
    左 (t ≤ −3.4): T2 の上に 1 unit 積んで T2・T3 を 1 段 (1→3) に = T2〜T4 が 2 段。右 (t ≥ 17.2): T2 を +0.5・T3 を +0.6 (1→2.5→3.6→4)"""
    P = []
    L0, L1 = -26.0, -3.4
    f2 = min(front_at(fronts['T2'], t) for t in np.linspace(L0, L1, 40))
    P.append(dict(kind='block', name='wall-tier-L', surface='step', t=r3((L0 + L1) / 2), s=r3((f2 + 22.3) / 2), y=1.0, abs=True, yaw=0, w=r3(L1 - L0),
                  d=r3(22.3 - f2), h=2.0, cut=0.0, chamfer=0.06, sink=0.05, aoAmount=0.6, seed=sd()))
    R0, R1 = 17.2, 48.0
    f2r = min(front_at(fronts['T2'], t) for t in np.linspace(R0, R1, 40))
    f3r = min(front_at(fronts['T3'], t) for t in np.linspace(R0, R1, 40))
    P.append(dict(kind='block', name='wall-tier-R2', surface='step', t=r3((R0 + R1) / 2), s=r3((f2r + 17.8) / 2), y=1.0, abs=True, yaw=0, w=r3(R1 - R0),
                  d=r3(17.8 - f2r), h=1.5, cut=0.0, chamfer=0.06, sink=0.05, aoAmount=0.6, seed=sd()))
    P.append(dict(kind='block', name='wall-tier-R3', surface='step', t=r3((R0 + 0.6 + R1) / 2), s=r3((f3r + 22.3) / 2), y=2.0, abs=True, yaw=0,
                  w=r3(R1 - R0 - 0.6), d=r3(22.3 - f3r), h=1.6, cut=0.0, chamfer=0.06, sink=0.05, aoAmount=0.6, seed=sd()))
    return P


def parapet(sd, fronts):
    """胸壁 (計画 §0 裁定 2 の幕3 の 3D「胸壁」): T3 の縁の上の凸凹 (笠石の帯＋小さな凸 merlon)。座席の通りの外 (PC x ≈450〜880・行 ≈290〜350)。
    T3 の立面の上の縁を歯形に切る = 段の縞の上端を縦に刻む。名前 parapet-"""
    P = []
    t0, t1 = PARAPET
    fr = fronts['T3']
    tm = (t0 + t1) / 2
    s0 = front_at(fr, tm) + 0.22
    P.append(dict(kind='block', name='parapet-base', surface='step', t=r3(tm), s=r3(s0), y=3.0, abs=True, yaw=0, w=r3(t1 - t0), d=0.36, h=0.26, cut=0.0,
                  chamfer=0.03, sink=0.04, aoAmount=0.5, seed=sd()))
    t = t0 + 0.3
    k = 0
    while t < t1 - 0.3:
        P.append(dict(kind='block', name='parapet-merlon-%d' % k, surface='step', t=r3(t), s=r3(s0), y=3.26, abs=True, yaw=0, w=0.5, d=0.34,
                      h=0.42 if k % 3 else 0.3, cut=0.0, chamfer=0.03, sink=0.02, aoAmount=0.4, seed=sd()))
        k += 1
        t += 0.95
    return P


def gate(sd):
    """大門 (arch・門の軸)＋暗い奥＋左右の柱 (計画 §2 C 段1b「柱 pillar 2 (画面の中に)」= pillar-gate・pillar-gate-R)。T4 の上 (y 4)。
    スマホは上部バーの裏 (行 ≈74〜95 の土台だけ) なので組まない。右の柱は門の右の脚の外 (左と同じ 1.15 の間合い・PC x ≈535〜620・行 ≤293 = 主人公の頭
    458 より上・裂け目の暈 t 4.8 の左)。種は固定 (sd() を引かない = 後ろの部品の種をずらさない)"""
    g = GATE
    ph = dict(hide=True)
    P = []
    P.append(dict(kind='arch', name='gate', surface='pillar', t=g['t'], s=g['s'], y=WALL_Y0, abs=True, yaw=0, w=g['w'], h=g['h'], d=g['d'], pier=g['pier'],
                  keystone=True, sink=0.1, aoAmount=0.5, phone=ph, seed=sd()))
    ow = g['w'] - 2 * g['pier']
    P.append(dict(kind='block', name='wall-gate-recess', surface='wall', t=g['t'], s=r3(g['s'] + g['d'] / 2 + 0.07 + 0.15), y=WALL_Y0, abs=True, yaw=0,
                  w=r3(ow + 0.1), d=0.3, h=r3(g['h'] - 0.55), cut=0.0, chamfer=0.0, sink=0.1, aoAmount=0.0, tint=0.18, shadow=False, phone=ph, seed=sd()))
    P.append(dict(kind='pillar', name='pillar-gate', surface='pillar', t=r3(g['t'] - g['w'] / 2 - 1.15), s=r3(g['s'] - 0.2), y=0.0, yaw=0, w=0.85, d=0.85,
                  h=7.6, capH=0.4, capOver=0.16, baseH=0.36, baseOver=0.14, chamfer=0.06, sink=0.12, aoAmount=0.5, phone=ph, seed=sd()))
    P.append(dict(kind='pillar', name='pillar-gate-R', surface='pillar', t=r3(g['t'] + g['w'] / 2 + 1.15), s=r3(g['s'] - 0.2), y=0.0, yaw=0, w=0.85, d=0.85,
                  h=7.6, capH=0.4, capOver=0.16, baseH=0.36, baseOver=0.14, chamfer=0.06, sink=0.12, aoAmount=0.5, phone=dict(ph), seed=GATE_R_SEED))
    return P


def house(sd):
    """館 (brick の block・T4 の上・裂け目の右)。崩れた上の端は高さの違う 2 つの block・暗い入口。名前 house-。前に瓦礫 rubble-b (PROPS3)・灯柱が右の端の前 (T4)"""
    t0, s0, w = HOUSE
    P = []
    P.append(dict(kind='block', name='house-body', surface='brick', t=t0, s=s0, y=0.0, yaw=0, w=w, d=2.4, h=4.3, cut=0.12, chamfer=0.06, sink=0.12,
                  aoAmount=0.6, seed=sd()))
    P.append(dict(kind='block', name='house-top', surface='brick', t=r3(t0 - 0.7), s=r3(s0 + 0.1), y=4.3, yaw=0, w=r3(w * 0.5), d=2.2, h=1.0, cut=0.3,
                  chamfer=0.06, sink=0.05, aoAmount=0.4, seed=sd()))
    P.append(dict(kind='block', name='house-door', surface='wall', t=r3(t0 + 0.5), s=r3(s0 - 1.2 - 0.02), y=0.0, yaw=0, w=1.1, d=0.12, h=2.1, cut=0.0,
                  chamfer=0.0, sink=0.05, aoAmount=0.0, tint=0.16, shadow=False, seed=sd()))
    return P


def lamp(sd):
    """灯柱 = 細い pillar (3D) の上に灯の頭の半立体 (D3 の lamp_head＝lamp_post の頭だけの絵)＋暈 lampC。T4 の上・t ≥6.5 (人形の列と意図の札の帯の外)"""
    t, s, gy = LAMP
    h = 2.9
    L = dict(sources=sources())
    im, _ = place.art_for(L, 'lampHead')
    hh = (im.size[1] / place.TPU) if im is not None else 1.24
    P = []
    P.append(dict(kind='pillar', name='lamp-post', surface='pillar', t=t, s=s, y=0.0, yaw=0, w=0.24, d=0.24, h=h, capH=0.18, capOver=0.08, baseH=0.26,
                  baseOver=0.1, chamfer=0.03, sink=0.08, aoAmount=0.3, seed=sd()))
    P.append(dict(kind='relief', name='lamp-head', src='lampHead', t=t, s=r3(s - 0.02), y=r3(gy + h + 0.01), abs=True, yaw=0, flip=False, shadow=False,
                  seed=sd()))
    # 暈の高さ = 灯の頭の板の y (柱の上 0.01) + D3 の glow.y (ガラスの籠の中心 0.59。前は絵の高さ×0.62 = 0.71 で 0.12 上だった)
    P.append(dict(kind='halo', name='lampC', t=t, s=r3(s - 0.4), y=r3(gy + h + 0.01 + glow_y('lamp_head', hh * 0.62)), abs=True, r=0.55, color=LAMP_C,
                  gain=0.75, core=0.4, facing='camera', seed=sd()))
    return P


def fire(sd, nm, t, s, phone=None):
    """篝火 (brazier の半立体) と暈。暈の高さ = D3 の glow.y (火の中心 1.339・見えている絵の下端から。前は絵の高さ×0.78 = 1.498 で 0.16 上だった)"""
    L = dict(sources=sources())
    im, _ = place.art_for(L, 'brazier')
    hh = (im.size[1] / place.TPU) if im is not None else 1.48
    p = dict(kind='relief', name=nm, src='brazier', t=t, s=s, y=0.0, yaw=0, flip=False, shadow=False, seed=sd())
    hp = dict(kind='halo', name=nm + '-halo', t=t, s=r3(s - 0.3), y=r3(glow_y('brazier', hh * 0.78)), r=0.62, color=WARM_FIRE, gain=0.75, core=0.35, facing='camera', seed=sd())
    if phone is not None:
        p['phone'] = dict(phone)
        hp['phone'] = dict(phone) if phone.get('hide') else dict(t=phone.get('t', t), s=r3(phone.get('s', s) - 0.3))
    return [p, hp]


def shaft_geom(G, foot_ts):
    """光の柱の形 (乱数なし)。足 (t, s, y 0) から上の口 (y SHAFT_TOP_Y) へ、画面で縦から右へ SHAFT_LEAN° 傾く向き。
    裂け目 crack は、柱の中心の線を上へ延ばして PC の行 CRACK_ROW に来る壁の面 (s WALL_FRONT − 0.15) の点。
    足を動かすと裂け目は柱の線の延長に付いていく (足の t の差より大きく動く: 足 −3.0→−2.4 の 0.6 で裂け目 t 4.8→5.8)"""
    cam = cam_pc()
    ft, fs = foot_ts
    foot = G.on_path(ft, fs, 0.0)
    fx, fy, _ = cam.project(foot)
    dz = 0.06   # 上の口を少し奥へ (下へ降りるほど手前)

    def top_for(ax):
        return foot + np.array([ax, 1.0, dz]) * (SHAFT_TOP_Y / 1.0)

    lo, hi = 0.0, 2.0
    for _ in range(50):
        m = (lo + hi) / 2
        tx, ty, _ = cam.project(top_for(m))
        ang = math.degrees(math.atan2(tx - fx, fy - ty))
        if ang < SHAFT_LEAN:
            lo = m
        else:
            hi = m
    ax = (lo + hi) / 2
    top = top_for(ax)
    d = foot - top
    ln = float(np.linalg.norm(d))
    dn = d / ln
    tt, ts = G.to_path(top[0], top[2])
    tx, ty, _ = cam.project(top)
    k = (fy - CRACK_ROW) / max(1e-3, (fy - ty))
    cx = fx + (tx - fx) * k
    s_c = WALL_FRONT - 0.15
    best = None
    for tq in np.linspace(-6.0, 16.0, 441):
        for yq in np.linspace(5.0, 11.0, 121):
            x, y, _ = cam.project(G.on_path(tq, s_c, yq))
            e = (x - cx) ** 2 + (y - CRACK_ROW) ** 2
            if best is None or e < best[0]:
                best = (e, tq, yq)
    ct, cy = best[1], best[2]
    return dict(top=(r3(tt), r3(ts), r3(top[1])), len=r3(ln + 0.4), dir=[r3(dn[0]), r3(dn[1]), r3(dn[2])],
                foot=(ft, fs), footPx=(round(fx), round(fy)), topPx=(round(tx), round(ty)), crack=(r3(ct), r3(s_c), r3(cy)), crackPx=(round(cx), CRACK_ROW))


def shafts(sd, G, name):
    """光の柱 (shaft 2)。部品の t・s・y は上の口 (DioramaMesh.Shaft の原点 = 出口)・dir は世界の向き (上から下)。形は shaft_geom"""
    g = shaft_geom(G, SHAFT_FOOT.get(name, SHAFT_FOOT_DEF))
    tt, ts, ty = g['top']
    P = []
    P.append(dict(kind='shaft', name='pillar-shaft-a', t=tt, s=ts, y=ty, abs=True, top=3.0, bottom=5.2, len=g['len'],
                  dir=list(g['dir']), gain=1.0, lobe=0.0))
    P.append(dict(kind='shaft', name='pillar-shaft-b', t=tt, s=ts, y=ty, abs=True, top=4.8, bottom=8.3, len=g['len'],
                  dir=list(g['dir']), gain=0.5, lobe=0.0, phone=dict(hide=True)))
    ct, s_c, cy = g['crack']
    P.append(dict(kind='halo', name='crack', t=ct, s=s_c, y=cy, abs=True, r=1.15, color=VEIN_C, gain=0.85, core=0.45, facing='camera',
                  phone=dict(hide=True), seed=sd()))
    return P, g


def mists():
    return [
        # 壁の前の霧の板 1 枚 (y 4.2〜7.8 = 奥の壁の根元〜中ほど。T4 と壁の継ぎ目をなじませる)。霧は柱の中と上端だけ (分析 §5-1)・far2 は B が壁の手前 26 で止める。
        # 薄く・暗めに (幕2 の統合の直しの輪1: 横いっぱいの霧の板 alpha 0.34 が乳白の横の縞の最大の出どころだった → 0.2)
        dict(kind='mist', name='mist-far', t=12.0, s=25.2, y=4.2, abs=True, w=72.0, h=3.6, alpha=0.12, noise=[0.12, 0.6], flow=0.012,
             tint=[0.36, 0.48, 0.54], seed=20263601, phone=dict(hide=True)),
    ]


def chains(sd):
    """鎖 2 (細く・上部バーの裏から垂れる)。下端の行が座席の通りの頭より上・幕ボス 160 の頭 (x 1200〜1550) は空ける。スマホは画面の外"""
    out = []
    L = dict(sources=sources())
    im, _ = place.art_for(L, 'chainHang')
    hh = (im.size[1] / place.TPU) if im is not None else 6.12
    for nm, t, s, top in (('chain-a', 11.2, 23.0, 11.4), ('chain-b', 27.6, 24.6, 10.9)):
        out.append(dict(kind='relief', name=nm, src='chainHang', t=t, s=s, y=r3(top - hh), abs=True, yaw=0, flip=(nm == 'chain-b'), shadow=False,
                        sway=0.3, swayFrom='top', phone=dict(hide=True), seed=sd()))
    return out


# 判定の画の小物 8 (分析 §5-1)。(名前, src・kind, t, s, 段の上の置き方, alts)。alts = 規則に掛かった時に試す (dt, ds)。順に試し、全部だめなら外す
PROPS3 = [
    ('statue', 'statue', -6.5, 10.7, [(0, 0), (-0.4, 0), (-0.8, 0.3), (0.3, 0.5), (-1.2, 0.6)]),
    ('urn', 'urn', -6.9, 4.2, [(0, 0), (-0.4, 0), (-0.8, 0.4), (-0.4, -0.4), (0.3, 0.3)]),
    ('rubble-a', 'rubblePile', 2.4, 12.55, [(0, 0), (-0.3, 0), (0.3, 0), (-0.6, 0), (-0.3, 0.3), (0.3, 0.3)]),
    ('rubble-b', 'rubblePile', 7.4, 23.0, [(0, 0), (-0.4, 0), (0.3, 0), (-0.4, 0.3), (-0.8, 0.2)]),   # 館の前 (T4)
    ('crystal', 'crystalCluster', 30.2, 23.4, [(0, 0), (-0.6, 0), (0.6, 0), (-1.2, 0.4), (1.2, 0.4)]),
]
BROKEN = ('pillar-broken', 27.4, 23.6)   # 折れ柱 (pillar kind・broken)。T4 の右
FIRE1 = (-7.6, 6.4)                      # 左の床
FIRE2 = (25.6, 22.9)                     # T4 の右 (t 12.5 は全座席の通りに入る・t 24 は 2 体 96 の意図の札の見積もりに掛かる = 報告)


def place_props3(L, judge, sd, log, fixed, fronts):
    placed = []
    taken = [b for b in (judge.box(p) for p in fixed) if b is not None]
    for nm, src, t, s, alts in PROPS3:
        best = None
        for dt, ds in alts:
            # 瓦礫は幅が 3.9 unit あるので、板の下の線を道に沿わせる (yaw = 道の向き。世界の yaw 0 だと下の線が s を 1.5 跨いで上の段に沈む)
            p = dict(kind='relief', name=nm, src=src, t=r3(t + dt), s=r3(s + ds), y=0.0, yaw=PATH_YAW if src == 'rubblePile' else 0, flip=nm in ('rubble-b', 'urn'), seed=sd())
            if src in ('crystalCluster',):
                p['shadow'] = False
            if overlaps(judge.box(p), taken, 0.35):
                continue
            if not judge.band_ok(p):
                continue
            pr = judge.problems(p)
            if not pr:
                best = p
                break
            if all(q[0] == 'PH' for q in pr) and 'phone' not in p:
                p2 = dict(p)
                p2['phone'] = dict(hide=True)
                if not judge.problems(p2):
                    best = p2
                    log.append('スマホでは組まない: %s (スマホの座席の通りに掛かる)' % nm)
                    break
        if best is None:
            log.append('外した: %s (t %.1f・s %.1f) = どのずらしでも規則に掛かる' % (nm, t, s))
            continue
        if abs(best['t'] - t) > 1e-6 or abs(best['s'] - s) > 1e-6:
            log.append('動かした: %s (t %.1f・s %.1f) → (t %.2f・s %.2f)' % (nm, t, s, best['t'], best['s']))
        placed.append(best)
        b = judge.box(best)
        if b is not None:
            taken.append(b)
    return placed


# スマホで UI の裏に大きく隠れる物は組まない (gen_act2.place_props の決まりの幕3 版)。(頭の部品, 一緒に消す部品)。箱は組の和
PHONE_TRIM = [('lamp-post', ('lamp-head', 'lampC')), ('house-body', ('house-top', 'house-door')), ('statue', ()), ('urn', ()), ('rubble-a', ()),
              ('rubble-b', ()), ('fire1', ('fire1-halo',)), ('buttress-a', ('buttress-a-cap',)), ('buttress-b', ('buttress-b-cap',)), ('crystal', ())]
PHONE_TOPBAR = 73.5


def phone_trim(P, judge, log):
    """スマホで上部バー (行 0〜73) の裏か画面の上の外に 4 割以上、またはからくりの帯 (x 0〜640・行 81〜210)・自分の札に 3 割以上隠れる組は、
    スマホでは組まない (切れた絵を見せない)。光の暈 (lampC など) も一緒に消す = レーン B の lights は phone.on false が要る (報告)"""
    by = {p.get('name'): p for p in P}
    for head, rest in PHONE_TRIM:
        grp = [by[n] for n in (head,) + tuple(rest) if n in by]
        if not grp or (grp[0].get('phone') or {}).get('hide'):
            continue
        boxes = [b for b in (judge.box(q, 'PH') for q in grp) if b is not None]
        if not boxes:
            continue
        b = (min(q[0] for q in boxes), min(q[1] for q in boxes), max(q[2] for q in boxes), max(q[3] for q in boxes))
        area = max(1.0, (b[2] - b[0]) * (b[3] - b[1]))
        top_hidden = max(0.0, min(b[3], PHONE_TOPBAR) - b[1]) * (b[2] - b[0])
        ui = 0.0
        for r in ((0.0, 81.0, 640.0, 129.0), (36.0, 378.0, 286.0, 98.0)):
            w = min(b[2], r[0] + r[2]) - max(b[0], r[0])
            h = min(b[3], r[1] + r[3]) - max(b[1], r[1])
            if w > 0 and h > 0:
                ui += w * h
        if top_hidden > 0.4 * area or ui > 0.3 * area:
            for q in grp:
                q['phone'] = dict(hide=True)
            log.append('スマホでは組まない: %s (%s)' % ('・'.join(q.get('name') for q in grp),
                                                      '上部バーの裏か画面の上の外 %.0f%%' % (100 * top_hidden / area) if top_hidden > 0.4 * area else 'からくりの帯・自分の札の裏 %.0f%%' % (100 * ui / area)))


PHONE_ALT = [(-1.5, 0.4), (-1.0, 0.0), (-1.8, 0.8), (0.8, -0.4), (1.2, 0.0)]   # スマホだけ動かす小物の (dt, ds) の候補


def phone_unstack(P, judge, log):
    """スマホの置き場で小物が灯 (篝火 fire1) の絵に 25% を超えて重なる時 (灯のスマホの置き場は fit_phone が別に決める)、小物のスマホの置き場を
    PHONE_ALT から規則に掛からない所へ。どれもだめならスマホでは組まない"""
    by = {p.get('name'): p for p in P}
    lights = [b for b in (judge.box(by[n], 'PH') for n in ('fire1',) if n in by) if b is not None]
    for nm, src, t, s, alts in PROPS3:
        p = by.get(nm)
        if p is None or (p.get('phone') or {}).get('hide'):
            continue
        if not overlaps(judge.box(p, 'PH'), lights, 0.25):
            continue
        done = False
        for dt, ds in PHONE_ALT:
            q = dict(p)
            q['phone'] = dict(t=r3(p['t'] + dt), s=r3(p['s'] + ds))
            if overlaps(judge.box(q, 'PH'), lights, 0.25) or not judge.band_ok(q):
                continue
            if [x for x in judge.problems(q) if x[0] == 'PH']:
                continue
            p['phone'] = q['phone']
            log.append('スマホの置き場: %s → %s (篝火 fire1 の絵と重ならない所)' % (nm, json.dumps(q['phone'])))
            done = True
            break
        if not done:
            p['phone'] = dict(hide=True)
            log.append('スマホでは組まない: %s (篝火 fire1 の絵と重なる)' % nm)


LITTER_ZONES = [   # (t の範囲, s の範囲, 枚数) 座席の帯の奥・前の縁・左の空き・段の天面の手前
    ((-14.0, 26.0), (3.2, 8.2), 26),
    ((-14.0, 20.0), (SEAT_FRONT + 0.25, -3.0), 10),
    ((-16.0, -9.2), (-2.4, 2.6), 5),
    ((-14.0, 30.0), (9.6, 12.4), 12),
    ((-14.0, 30.0), (13.6, 16.6), 8),
]


def place_litter(L, judge, rng, sd, log):
    names = list(LITTER_SRC.keys()) + list(LITTER_A3_ONLY.keys())
    weights = [3.0 if 'Pebble' in n else 2.4 if 'Chip' in n else 1.2 if 'Rubble' in n else 2.0 for n in names]
    out = []
    rejected = 0
    buried = 0
    # block (控え壁・段の嵩上げ wall-tier-・大階段 steps- と側壁)・柱・崩れ石の足跡の中は外す: HeightAtPath は slab しか見ないので、
    # そこに置いた小札は下の段の天面の高さに置かれて block の中に隠れる (place3 の R13。直しの輪 2026-10-03・反証の指摘)
    lists = {False: place3.bury_list(L, judge.G, phone=False), True: place3.bury_list(L, judge.G, phone=True)}
    for (tr, sr, n) in LITTER_ZONES:
        k = 0
        tries = 0
        while k < n and tries < n * 40:
            tries += 1
            t = tr[0] + rng.random() * (tr[1] - tr[0])
            s = sr[0] + rng.random() * (sr[1] - sr[0])
            src = rng.choices(names, weights)[0]
            p = dict(kind='litter', src=src, t=r3(t), s=r3(s), yaw=round((rng.random() - 0.5) * 30, 1), flip=rng.random() < 0.5, seed=sd())
            if any(abs(p['t'] - q['t']) < 0.6 and abs(p['s'] - q['s']) < 0.4 for q in out):
                continue
            # 段の縁の立面の真上は避ける (天面の手前 0.3 以内 = 縁から垂れて見える)
            if any(abs(s - front_at(fronts_cache[nm], t)) < 0.3 for nm in fronts_cache):
                continue
            if place3.litter_buried(L, judge.G, p, lists):
                buried += 1
                continue
            if judge.problems(p, litter=True):
                rejected += 1
                continue
            out.append(p)
            k += 1
        if k < n:
            log.append('小札: s %.1f〜%.1f の帯に %d/%d 枚 (足元の通りで置けない所が多い)' % (sr[0], sr[1], k, n))
    log.append('小札: %d 枚 (足元の通りで外した候補 %d・block・柱・岩の中で外した候補 %d)' % (len(out), rejected, buried))
    return out


fronts_cache = {}


# ------------------------------------------------------------------ 組み立て

def build(name, cache):
    """1 枚を組む。place2 の吊る物・支え・柱の扱いは幕3 の値 (place3.act3_rules の with の中だけ。出たら place2 の値に戻る)"""
    with place3.act3_rules():
        return _build(name, cache)


def _build(name, cache):
    rng = random.Random(SEED)
    sd = Seeds(SEED * 10)
    L = head(name)
    L['sources'] = sources()
    log = []
    P, fronts = terraces(rng, sd, name)
    fronts_cache.clear()
    fronts_cache.update(fronts)
    L['parts'] = list(P)
    sh, shinfo = shafts(sd, place.Ground(L), name)   # 裂け目の t を先に (壁柱を空ける)。段だけの地面で足りる (光の柱の足は床・裂け目は壁の面)
    if name in SHAFT_FOOT:   # 変種の _doc に本番からの動きを書く (数を二重に持たない)
        g0 = shaft_geom(place.Ground(L), SHAFT_FOOT_DEF)
        for k, v in (('foot0', g0['foot'][0]), ('foot1', shinfo['foot'][0]), ('crack0', g0['crack'][0]), ('crack1', shinfo['crack'][0]),
                     ('cracky0', g0['crack'][2]), ('cracky1', shinfo['crack'][2])):
            L['_doc'] = L['_doc'].replace('{%s}' % k, '%g' % v)
    ct = shinfo['crack'][0]
    keep_out = [(GATE['t'] - GATE['w'] / 2 - 1.6, GATE['t'] + GATE['w'] / 2 + 1.6), (ct - 1.3, ct + 1.3), (HOUSE[0] - HOUSE[2] / 2 - 0.8, HOUSE[0] + HOUSE[2] / 2 + 0.8)]
    P += wall(rng, sd, keep_out)
    if name == 'act3_layout_tier':
        P += tier_blocks(sd, fronts)
    P += stairs(sd, fronts, STAIR_STRONG if name == 'act3_layout_stairs' else STAIR)
    P += buttresses(sd, fronts)
    P += collapse(sd, fronts)
    P += parapet(sd, fronts)
    P += gate(sd)
    P += house(sd)
    P += lamp(sd)
    P += sh
    log.append('光の柱: 足 t %.1f・s %.1f (PC x %d・行 %d) → 上の口 PC x %d・行 %d (傾き %.1f°)。裂け目 crack t %.2f・s %.2f・y %.2f (PC x %d・行 %d)' % (
        shinfo['foot'][0], shinfo['foot'][1], shinfo['footPx'][0], shinfo['footPx'][1], shinfo['topPx'][0], shinfo['topPx'][1], SHAFT_LEAN,
        shinfo['crack'][0], shinfo['crack'][1], shinfo['crack'][2], shinfo['crackPx'][0], shinfo['crackPx'][1]))
    P += fire(sd, 'fire1', FIRE1[0], FIRE1[1])
    P += fire(sd, 'fire2', FIRE2[0], FIRE2[1], phone=dict(hide=True))
    P.append(dict(kind='pillar', name=BROKEN[0], surface='pillar', t=BROKEN[1], s=BROKEN[2], y=0.0, yaw=0, w=0.9, d=0.9, h=3.6, capH=0.0, capOver=0.0,
                  baseH=0.34, baseOver=0.13, chamfer=0.06, broken=1.0, sink=0.12, aoAmount=0.5, phone=dict(hide=True), seed=sd()))
    P += chains(sd)
    P += mists()
    L['parts'] = P
    key = 'judge'
    if key not in cache:
        cache[key] = Judge(L)
    judge = cache[key]
    judge.L = L
    judge.G = place.Ground(L)
    fit_phone(P, judge, log)
    place3.audit_fixed(P, judge, log)
    # 重なりを避ける相手 = 小さくまとまった物 (半立体・柱・館)。控え壁は奥へ長い block で外接の箱が大きすぎる (手前の瓦礫まで弾く) ので入れない
    fixed = [p for p in P if p['kind'] in ('relief', 'pillar') and p.get('name') != 'lamp-post']
    P += place_props3(L, judge, sd, log, fixed, fronts)
    L['parts'] = P
    phone_trim(P, judge, log)
    phone_unstack(P, judge, log)
    P += place_litter(L, judge, random.Random(SEED + 77), sd, log)
    L['parts'] = P
    _prune_sources(L)
    return L, log, shinfo


# 名前つきの灯のスマホの置き場の候補 (先頭から試す。どれもだめならスマホでは組まない)
PHONE_FIT = {
    'fire1': [dict(), dict(t=-7.2, s=6.8), dict(t=-8.2, s=7.4), dict(t=-6.8, s=8.2)],
    'lamp-post': [dict(), dict(t=8.4), dict(t=6.8)],
}


def fit_phone(P, judge, log):
    """灯のスマホの置き場を、スマホの座席の通り・意図の札・からくりの帯・自分の札に掛からない所へ。暈は親に付いていく (gen_act2.fit_phone の幕3 版)"""
    by = {p.get('name'): p for p in P}
    halo_of = {'fire1': 'fire1-halo', 'lamp-post': 'lampC'}
    for nm, cands in PHONE_FIT.items():
        p = by.get(nm)
        if p is None:
            continue
        halo = by.get(halo_of[nm])
        chosen = None
        for c in cands:
            q = dict(p)
            if c:
                q['phone'] = dict(c)
            else:
                q.pop('phone', None)
            pr = [x for x in judge.problems(q) if x[0] == 'PH']
            ui_ok = judge.phone_ui_hits(q) == 0 if q['kind'] in ('relief', 'card') else True
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
            log.append('スマホの置き場: %s → PC と同じ (t %s・s %s)' % (nm, p['t'], p['s']))
            continue
        p['phone'] = dict(chosen)
        if halo is not None:
            halo['phone'] = dict(t=chosen.get('t', halo['t']), s=r3(chosen.get('s', p['s']) - (p['s'] - halo['s'])))
        log.append('スマホの置き場: %s → %s' % (nm, json.dumps(chosen)))


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
    names = list(NAMES)
    if a.only:
        names = [n for n in names if n in a.only.split(',')]
    cache = {}
    paths = []
    for nm in names:
        L, log, _ = build(nm, cache)
        fp = os.path.join(a.out_dir, nm + '.json')
        with open(fp, 'w') as f:
            f.write(dump_layout(L))
        paths.append(fp)
        kinds = Counter(p['kind'] for p in L['parts'])
        print('%s: 部品 %d (%s)' % (nm, len(L['parts']), '・'.join('%s %d' % kv for kv in sorted(kinds.items(), key=lambda kv: -kv[1]))))
        for ln in log:
            print('   ' + ln)
    if not a.no_check:
        return place3.check_files(paths, want_img=True)
    return 0


if __name__ == '__main__':
    sys.exit(main())

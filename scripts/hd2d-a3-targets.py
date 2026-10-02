#!/usr/bin/env python3
"""HD-2D 段2 幕3「埋もれた古代都市」の物差し (2026-10-03 レーン F3・段1b。計画 docs/design/hd2d-stage2-plan-2026-10-02.md §1・
説明 docs/design/hd2d-stage2/measure.md §3)。合否は目で決める (裁定)。数字は「どの層が足りないか」を言葉にする補助。

幕3 は「閉じた幕」= 床型 (分析書 §5・裁定 6): 主人公の足元が頂点・その真後ろに斜めの光の柱・柱の外の壁は沈む。
目標の画は本家 Tomb of the Imperator (探索画・~/.cache/deck-rogue/hd2d-stage2/targets/web/ot1web_tomb_imperator.jpg = metrics.json の A3-1) と、
戦闘の作りと数字の物差しの ot_921570_7 (A3-4)。本家の値は metrics.json を横に並べ、同じ式で本家の画も測り直す。
測り方の道具 (輝度・UI の型・キャラの型・主人公の足元・本家の読み込み・UI の歯止め) は scripts/hd2d-a2-targets.py を借りる (同じ式)。
本家 Tomb のキャラの矩形は r2-targets の REFS に無いので、このファイルの REF_CHARS3 に持つ (目で読んだ値・720p の px)。

物差し (★ 合否 = 計画 §1 の幕3 の光の 3 条件・☆ 目安・◆ 歯止め・— 参考 = 数字だけ)
  ★ T1 主人公の列で足元 (0〜40px 上) が最大・上へ 1 本の勾配: 主人公の足元の x ±125px の列の 10 行ごとの平均 (UI とキャラを除く) の最大が
       足元の 40px 上〜10px 下にあり、主人公の頭の上から上へ 3 行ずつならした値が 8 以上上がる所が 0 (rises)。
       足元から頭までの行 (体の高さ) は勾配を数えない: キャラを除かない列では体の色 (このはの 0 周目は中央値 35) が、除いた列では
       体のすぐ後ろの段の立面が谷を作る (本家 Tomb も除くと 行 406〜427 が 71〜89 に沈む)。キャラを除くのは列の中の敵・人形を外して
       舞台の光の形 (レーン B・C が動かせる物) を測るため。キャラを除かない列 (計画の数字の出どころの verify-facts §3-1 と同じ) は T1i に並べる。
       頭のすぐ上 ÷ 足元の頂点 (T1h) は柱の体の明るさの参考 (本家 Tomb 0.66・verify-facts「柱の体は床の 0.8 倍」は列の別の読み)
  ★ T2 柱の中÷外 ≥ 3: 柱 = 主人公の足元の真上を通り、上へ行くほど右へ傾く帯 (既定 17.5°・幅 400px・PC の px)。
       中 = 帯の中・外 = 帯の縁から 100px より外 (どちらも 行 0〜足元の 40px 上・キャラと UI を除く) の中央値の比。帯の角度・幅・ずらしは
       --shaft-angle・--shaft-width・--shaft-dx で書く (計画 §1「帯の幅を書いて測る」)。角度とずらしを振っていちばん比の大きい帯も best に出す (参考)
  ☆ T2w・T2a 柱の形 (計画「幅 ≈400px 半値・斜め 15〜20°」): 足元の 640px 上〜60px 上を 60px の横帯に切り、帯ごとに列の平均 (41px でならす) の
       いちばん明るい所 (主人公の x −400〜+600px の中) と地 (その帯の 20 百分位) の半値の幅と中心を出す。幅 120px 以上の帯が測った帯の
       半分以上 (3 つ以上) あれば中心の並びに直線を当て、直線からのずれ (rms) が 80px 以下の時だけ (= 1 本の柱) 傾き (上へ行くほど右 = +)・
       足元の行での中心 (主人公から何 px = T2f)・幅 (帯の中央値) を出す。灯や結晶の細い光が帯ごとに別々に拾われた時は出さない。
       本家 Tomb はこの測り方で 幅 560px・傾き 13.5°・足元で主人公の 49px 左・ずれ 25px (計画の字は目で読んだ値) → 目安は 幅 300〜600・傾き 12〜22°
  ★ T3a 柱の外の壁の中央値 ≤ 25 (T2 の外)。— T3p 壁のむら (T2 の外 かつ 地平線より上〔本家は上 40%〕の p10・中央値・p90。
       計画 §2 F の「壁の p10/p90」・反証 verify-score の「目地の奥が黒い = p10 が低い」)
  ★ T3b 床の横 7 分割 (主人公が真ん中) の中央 ÷ 端 ≥ 3: 床の帯 (足元の 20px 上〜100px 下) を、主人公の足元を真ん中の 1 区にして画面の幅の
       1/7 ずつ 7 区に分け、真ん中の区の中央値 ÷ 端の区の中央値。端 = 真ん中から 3 区はなれた区 (本家 Tomb・ot7 の両端と同じ距離)。
       画面に半分以上入らない側は 2 区はなれた区で代え、それも無ければその側は使わない (うちの戦闘は主人公が左 x 443 なので右だけ)。
       画面の 7 等分の比 (screen7)・7 区の中央値・床の光溜まりの頂点は主人公から何 px (T3e) は参考。
       本家 Tomb は主人公の立つ通路が細く (下は橋の立面) 既定の帯で 2.82・細い帯 (--floor-band 10,30) で 4.5。ot7 は 5.84
  ◆ G1 p95 ≤ 174 (舞台の全画素。露出で合わせない。本家 Tomb 105・ot7 127)
  — G2 主人公の体 − 足元の床 (主人公の型 2px 内側の中央値 − 足元の 10px 上〜60px 下 × x ±120 の床の中央値。hd2d-a2-targets の歯止めと同じ式)。
       幕3 は床が頂点なので本家の戦闘 ot7 でも体が床より暗い (−8・比 0.94) = 合否を付けず数字と比 (G2r) を並べる。
       主人公が光の中で影絵になっていないか (反証 verify-safety #123) は目で見る
  ◆ G3 紙の UI がいちばん明るくならない: 手札の外の UI の面の中央値 ÷ 足元の頂点 (T1 の最大) ≤ 1 (hd2d-a2-targets の G3 の「帯」を「足元の頂点」に)
  ☆ W1 暖色の割合 ≤ 2% (色相 10〜60°・彩度 ≥0.3。提案 A の A3-6「暖色 ≤2%」= 篝火 2 点の半径 30〜50px だけ)
  — 全画素の中央値・暗い画素 (<60)・明るい画素 (>150)・帯 (全幅 3% 窓)・暗部と光の色 (scripts/hd2d-a2-targets.py と同じ。
    提案 B の目安 暗い画素 70〜76%・中央値 35〜42・p95 105〜125 は反証で「門にしない」= 数字だけ並べる)

使い方
  python3 scripts/hd2d-a3-targets.py <撮影のフォルダ> <場面> [<場面> …] [--honke] [--ref tomb] [--ref ot7] [--md 表.md] [--json 結果.json]
      場面は pshots の名前 (PC-T3-quad など。-hideui・-unitsonly・-uionly を付けても同じ場面)。
  python3 scripts/hd2d-a3-targets.py --all <撮影のフォルダ> [--honke]     # フォルダの幕3 の戦闘の場面を全部 (--match で名前を絞る)
  python3 scripts/hd2d-a3-targets.py --honke                               # 本家だけ (Tomb と ot7)
  python3 scripts/hd2d-a3-targets.py --image <画.png> [--layout …] [--units …] [--ui …]
  柱の帯: [--shaft-angle 17.5] [--shaft-width 400] [--shaft-dx 0]   床の帯: [--floor-band 20,100]
"""
import argparse
import importlib.util
import json
import math
import os
import re
import sys
import warnings

import numpy as np

sys.dont_write_bytecode = True
HERE = os.path.dirname(os.path.abspath(__file__))
_spec = importlib.util.spec_from_file_location('hd2d_a2_targets', os.path.join(HERE, 'hd2d-a2-targets.py'))
A2 = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(A2)
T = A2.T
rnd = A2.rnd

FOOT_PEAK = (40, 10)     # 足元の 40px 上〜10px 下に列の最大 (計画の 0〜40px 上を 10px 下まで広げる = 本家 Tomb の最大は足元の 7px 下。measure.md §3)
RISE = 8                 # 上へ行って 8 以上上がる所 = 2 本目の山
SHAFT = dict(angle=17.5, width=400, dx=0, margin=100)
# dx 0 = 計画 §1 の「真後ろ」の字のまま。うちの柱の中心は足元の行で主人公の 33px 右 (gen_act3 の足 PC x 501・行 625。pillar1 は 81px 右)・
#   本家 Tomb は 49px 左 (T2f) = 0 はその間。±50px で本家 Tomb の比は ±0.1 しか動かない (measure.md §3 の「決めたこと」)
SHAPE = dict(top=640, bottom=60, strip=60, smooth=41, left=400, right=600, minWidth=120, minRise=10, minShare=0.5, maxRms=80)   # 柱の形の測り方 (PC の px)
FLOOR7 = (20, 100)       # 床の帯 = 足元の 20px 上〜100px 下
REF_WALL_TOP = 0.40      # 本家の「壁の上段」= 上 40% (verify-facts §3-1「上 40% は石の壁」)
HONKE = ('tomb', 'ot7')

# 本家 Tomb のキャラの矩形 (720p の px・目で読んだ値。一行に並ぶ 5 人と足元の小さな獣。左の白い帽子の物は台座 = キャラでない。
#   主人公 = いちばん右の橙 = metrics.json の hero_feet 640,447)
REF_CHARS3 = {
    'tomb': dict(chars=[[415, 372, 46, 80], [462, 370, 44, 82], [514, 370, 44, 82], [550, 412, 22, 22], [568, 372, 42, 80], [618, 372, 50, 80]],
                 hero=[618, 372, 50, 80]),
}

CHECKS = [
    dict(id='T1', key='light.T1.ok', label='主人公の列で足元 (40px 上〜10px 下) が最大・上へ 1 本の勾配 (キャラを除く)', cls='合否', fmt='bool'),
    dict(id='T1p', key='light.T1.peakAboveFeet', label='  列の最大は足元から何 px 上 (PC の px)', cls='参考'),
    dict(id='T1r', key='light.T1.rises', label='  上へ行って 8 以上上がる所の数 (0 = 1 本の勾配)', rng=[None, 0], cls='合否'),
    dict(id='T1l', key='light.T1.peakLum', label='  足元の頂点の明るさ (列の最大)', cls='参考'),
    dict(id='T1h', key='light.T1.headOverPeak', label='  頭のすぐ上 ÷ 足元の頂点 (柱の体の明るさ)', cls='参考'),
    dict(id='T1i', key='light.T1incl.ok', label='  キャラを除かない列でも足元が最大・1 本 (参考)', cls='参考', fmt='bool'),
    dict(id='T1j', key='light.T1incl.peakAboveFeet', label='  その最大は足元から何 px 上', cls='参考'),
    dict(id='T2', key='light.T2.ratio', label='柱の中 ÷ 外 (帯は下の行)', rng=[3, None], cls='合否'),
    dict(id='T2b', key='light.T2.band', label='  帯 (角度°・幅 px・ずらし px)', cls='参考'),
    dict(id='T2v', key='light.T2.inOut', label='  帯の中・外の中央値', cls='参考'),
    dict(id='T2x', key='light.T2.best', label='  いちばん比の大きい帯 (角度・ずらし・比)', cls='参考'),
    dict(id='T2w', key='light.shape.width', label='柱の半値の幅 (PC の px・横帯の中央値)', rng=[300, 600], cls='目安'),
    dict(id='T2a', key='light.shape.angle', label='柱の傾き (上へ行くほど右 = +・度)', rng=[12, 22], cls='目安'),
    dict(id='T2f', key='light.shape.footOffset', label='  柱の中心は足元の行で主人公から何 px (右 = +)', cls='参考'),
    dict(id='T2n', key='light.shape.strips', label='  柱らしい横帯の数 / 測った横帯', cls='参考'),
    dict(id='T2r', key='light.shape.rms', label='  中心の直線からのずれ (px・80 以下で 1 本の柱)', cls='参考'),
    dict(id='T3a', key='light.T3.wallOutside', label='柱の外の壁の中央値', rng=[None, 25], cls='合否'),
    dict(id='T3p', key='light.T3.wallSpread', label='  壁のむら (柱の外・地平線より上の p10・中央値・p90)', cls='参考'),
    dict(id='T3b', key='light.T3.floorCenterOverEnds', label='床の横 7 分割 (主人公が真ん中) の中央 ÷ 端', rng=[3, None], cls='合否'),
    dict(id='T3u', key='light.T3.endsUsed', label='  端に使った区 (左から 0〜6・真ん中は 3)', cls='参考'),
    dict(id='T3c', key='light.T3.screen7', label='  画面の 7 等分の 真ん中 ÷ 両端 (参考)', cls='参考'),
    dict(id='T3d', key='light.T3.segments', label='  7 区の中央値 (左から。画面の外は —)', cls='参考'),
    dict(id='T3e', key='light.T3.poolOffset', label='  床の光溜まりの頂点は主人公から何 px (右 = +)', cls='参考'),
    dict(id='G1', key='guard.p95', label='舞台の p95', rng=[None, 174], cls='歯止め'),
    dict(id='G2', key='guard.bodyMinusFloor', label='主人公の体 − 足元の床 (幕2 の歯止め。幕3 は参考)', cls='参考'),
    dict(id='G2r', key='guard.bodyOverFloor', label='  主人公の体 ÷ 足元の床', cls='参考'),
    dict(id='G3', key='guard.uiFaceOverApex', label='手札の外の UI の面の中央値 ÷ 足元の頂点 (≤1)', rng=[None, 1.0], cls='歯止め'),
    dict(id='W1', key='color.warm', label='暖色の割合 (色相 10〜60°・彩度 ≥0.3)', rng=[None, 0.02], cls='目安'),
    dict(id='M1', key='global.median', label='中央値 (舞台の全画素)', cls='参考'),
    dict(id='M2', key='global.dark60', label='暗い画素 (<60) の割合', cls='参考'),
    dict(id='M3', key='global.bright150', label='明るい画素 (>150) の割合', cls='参考'),
    dict(id='B1', key='band.peakUpLum', label='帯 (全幅・3% 窓) の頂点の明るさ', cls='参考'),
    dict(id='B2', key='band.aboveFeet', label='帯 (全幅) の頂点は足元から何 px 上 (PC の px)', cls='参考'),
    dict(id='C1', key='color.shadowHue', label='暗部の色 (20<L<60 の平均)', cls='参考', fmt='hue'),
    dict(id='C2', key='color.lightHue', label='光の色 (L>120 の平均)', cls='参考', fmt='hue'),
    dict(id='G2b', key='guard.heroBody', label='  主人公の体の中央値', cls='参考'),
    dict(id='G3b', key='guard.uiP95', label='  手札の外の UI の p95 (字を含む・参考)', cls='参考'),
    dict(id='D1', key='diorama.ok', label='箱庭の門 (Diorama.Check の result)', cls='参考', fmt='bool'),
]


# ---- 光の 3 条件

def head_row(s):
    """主人公の頭の行 (主人公の型の上端。型が無ければ絵の矩形の上端・それも無ければ足元の 250px 上)"""
    if s.hero is not None and s.heroExact:
        rows = np.nonzero(s.hero.any(1))[0]
        if len(rows):
            return float(rows.min()), '主人公の型の上端'
    u = A2.unit_boxes(s.lay).get('player') if s.lay else None
    if u and (u.get('boardPx') or u.get('boardRawPx')):
        return float((u.get('boardPx') or u.get('boardRawPx'))[1]), '主人公の絵の矩形の上端'
    return s.feet[1] - 250 * s.sc, '足元の 250px 上 (主人公の型が無い)'


def t1_floor_peak(s, L, ok, head):
    """主人公の列 (ok の画素だけ) の最大が足元にあるか・頭の上から上へ 1 本の勾配か。
    足元から頭までの行 (主人公の体の高さ) は勾配を数えない: キャラを除かない列では体の色が・除いた列では体の後ろの段の立面が
    谷を作る (本家 Tomb も除くと 行 406〜427 が 71〜89 に沈む = 主人公のすぐ後ろの段の立面)。頂点は足元・勾配は頭の上で見る"""
    sc = s.sc; fx, fy = s.feet
    prof, step, xs = A2.column_profile(s, L, ok)
    pts = [(y, v) for y, v in prof if np.isfinite(v) and y + step / 2 < fy + FOOT_PEAK[1] * sc]
    if not pts:
        return dict(ok=None)
    by, bv = max(pts, key=lambda q: q[1])
    above = (fy - (by + step / 2)) / sc
    in_foot = -FOOT_PEAK[1] <= above <= FOOT_PEAK[0]
    # 頭の上から上へ: 3 行ずつならし、上がる所を数える
    vs = np.array([v for y, v in prof if np.isfinite(v) and y + step <= head], float)[::-1]   # 頭の上 → 上
    rises = 0; maxrise = 0.0
    if len(vs) >= 4:
        sm = np.convolve(np.pad(vs, 1, mode='edge'), np.ones(3) / 3, mode='valid')
        low = sm[0]
        inrise = False
        for v in sm[1:]:
            if v < low:
                low = v; inrise = False
            elif v - low >= RISE and not inrise:
                rises += 1; inrise = True; maxrise = max(maxrise, v - low)
            elif inrise:
                maxrise = max(maxrise, v - low)
    top = float(vs[0]) if len(vs) else None
    return dict(ok=bool(in_foot and rises == 0), peakRow=by, peakLum=rnd(bv, 1), peakAboveFeet=rnd(above, 0), inFoot=in_foot,
                rises=rises, maxRise=rnd(maxrise, 1), headRow=rnd(head, 0), headLum=rnd(top, 1) if top is not None else None,
                headOverPeak=rnd(top / max(bv, 1.0), 2) if top is not None else None,
                cols=list(xs), step=step, profile=[[y, rnd(v, 1)] for y, v in prof])


def shaft_masks(s, angle, width, dx, margin, stage):
    H, W = s.H, s.W; sc = s.sc; fx, fy = s.feet
    yy, xx = np.mgrid[0:H, 0:W]
    xc = fx + dx * sc + (fy - yy) * math.tan(math.radians(angle))
    d = np.abs(xx - xc)
    rows = yy < fy - 40 * sc
    inside = (d <= width * sc / 2) & rows & stage
    outside = (d >= width * sc / 2 + margin * sc) & rows & stage
    return inside, outside


def t2_shaft(s, L, stage, sh):
    inside, outside = shaft_masks(s, sh['angle'], sh['width'], sh['dx'], sh['margin'], stage)
    out = dict(band=[sh['angle'], sh['width'], sh['dx']], margin=sh['margin'])
    if inside.sum() < 500 or outside.sum() < 500:
        out['note'] = '帯の中か外の画素が足りない'
        return out, outside
    mi, mo = float(np.median(L[inside])), float(np.median(L[outside]))
    out.update(inside=rnd(mi, 1), outside=rnd(mo, 1), inOut=[rnd(mi, 1), rnd(mo, 1)], ratio=rnd(mi / max(mo, 1.0), 2))
    # 角度とずらしを振る (参考)
    best = None
    Ls = L[::4, ::4]; st = stage[::4, ::4]

    class _S:
        pass
    q = _S(); q.H, q.W = Ls.shape; q.sc = s.sc / 4; q.feet = (s.feet[0] / 4, s.feet[1] / 4)
    for ang in range(0, 35, 5):
        for dxx in range(-400, 401, 100):
            i_, o_ = shaft_masks(q, ang, sh['width'], dxx, sh['margin'], st)
            if i_.sum() < 40 or o_.sum() < 40:
                continue
            rr = float(np.median(Ls[i_])) / max(float(np.median(Ls[o_])), 1.0)
            if best is None or rr > best[2]:
                best = [ang, dxx, rnd(rr, 2)]
    out['best'] = best
    return out, outside


def smooth_nan(p, k):
    """nan を除いて幅 k でならす (有る画素が半分に満たない所は nan)"""
    ok = np.isfinite(p).astype(float)
    pv = np.nan_to_num(p)
    ker = np.ones(k)
    num = np.convolve(pv, ker, 'same'); den = np.convolve(ok, ker, 'same')
    return np.where(den >= k * 0.5, num / np.maximum(den, 1e-6), np.nan)


def shaft_shape(s, L, stage):
    """柱の形: 横帯ごとの半値の幅と中心 → 幅 (中央値)・傾き・足元の行での中心"""
    sc = s.sc; fx, fy = s.feet; H, W = s.H, s.W
    y0 = int(max(0, fy - SHAPE['top'] * sc)); y1 = int(max(0, fy - SHAPE['bottom'] * sc))
    hs = max(8, int(round(SHAPE['strip'] * sc)))
    k = max(5, int(round(SHAPE['smooth'] * sc))) | 1
    xa = int(max(0, fx - SHAPE['left'] * sc)); xb = int(min(W, fx + SHAPE['right'] * sc))
    rows = []
    for y in range(y0, y1 - hs + 1, hs):
        m = stage[y:y + hs]; blk = L[y:y + hs]
        cnt = m.sum(0); sm = np.where(m, blk, 0).sum(0)
        p = smooth_nan(np.where(cnt >= 0.3 * hs, sm / np.maximum(cnt, 1), np.nan), k)
        seg = p[xa:xb]
        if not np.isfinite(seg).any():
            continue
        xi = int(np.nanargmax(seg)) + xa; pk = float(p[xi])
        base = float(np.nanpercentile(p, 20))
        row = dict(y=rnd(y + hs / 2, 0), peak=rnd(pk, 1), base=rnd(base, 1))
        if pk - base >= SHAPE['minRise']:
            half = base + (pk - base) / 2
            xl = xi
            while xl > 0 and np.isfinite(p[xl - 1]) and p[xl - 1] >= half:
                xl -= 1
            xr = xi
            while xr < W - 1 and np.isfinite(p[xr + 1]) and p[xr + 1] >= half:
                xr += 1
            row.update(center=rnd((xl + xr) / 2, 1), width=rnd((xr - xl + 1) / sc, 0), offset=rnd(((xl + xr) / 2 - fx) / sc, 0))
        rows.append(row)
    good = [r for r in rows if r.get('width') is not None and r['width'] >= SHAPE['minWidth']]
    out = dict(rows=rows, strips='%d / %d' % (len(good), len(rows)))
    if len(good) >= max(3, int(math.ceil(SHAPE['minShare'] * len(rows)))):
        ys = np.array([r['y'] for r in good], float); cx = np.array([r['center'] for r in good], float)
        a, b = np.polyfit(ys, cx, 1)
        rms = float(np.sqrt(np.mean((cx - (a * ys + b)) ** 2))) / sc
        out['rms'] = rnd(rms, 0)
        if rms <= SHAPE['maxRms']:
            out.update(width=rnd(float(np.median([r['width'] for r in good])), 0), angle=rnd(math.degrees(math.atan(-a)), 1),
                       footOffset=rnd((a * fy + b - fx) / sc, 0))
        else:
            out['note'] = '柱らしい横帯の中心が直線から %dpx ずれる (1 本の柱でなく別々の光)' % round(rms)
    else:
        out['note'] = '幅 %dpx 以上の柱らしい横帯が測った横帯の %d%%・3 つに満たない (柱が無い・細い光だけ)' % (SHAPE['minWidth'], round(SHAPE['minShare'] * 100))
    return out


def wall_rows_end(s):
    """壁の上段の下端の行: うちは地平線 (layout の camera.horizonRow)・本家は上 40%"""
    if s.kind == 'ref':
        return s.H * REF_WALL_TOP, '本家の上 40%'
    cam = ((s.lay or {}).get('stage') or {}).get('camera') or {}
    if cam.get('fov'):
        n = T.cam_numbers(cam, s.H)
        return float(n['horizonRow']), '地平線 (%s)' % n['source']
    return s.feet[1] - 413 * s.sc, '足元の 413px 上 (カメラの記録が無い)'


def t3_floor7(s, L, stage, outside):
    H, W = s.H, s.W; sc = s.sc; fx, fy = s.feet
    out = {}
    # 柱の外の壁
    if outside is not None and outside.sum() > 500:
        out['wallOutside'] = rnd(float(np.median(L[outside])), 1)
        ye, src = wall_rows_end(s)
        wm = outside.copy(); wm[int(max(0, min(H, ye))):] = False
        if wm.sum() > 500:
            v = L[wm]
            out['wallSpread'] = [rnd(float(np.percentile(v, 10)), 1), rnd(float(np.median(v)), 1), rnd(float(np.percentile(v, 90)), 1)]
            out['wallRows'] = [0, rnd(ye, 0)]; out['wallRowsSrc'] = src
    # 床の 7 分割 (主人公が真ん中)
    y0 = int(max(0, fy - FLOOR7[0] * sc)); y1 = int(min(H, fy + FLOOR7[1] * sc))
    seg = W / 7.0
    meds = []
    for k in range(7):
        a = fx + (k - 3.5) * seg; b = a + seg
        ca, cb = int(max(0, round(a))), int(min(W, round(b)))
        if cb - ca < seg * 0.5:
            meds.append(None); continue
        m = stage[y0:y1, ca:cb]
        meds.append(rnd(float(np.median(L[y0:y1, ca:cb][m])), 1) if m.sum() > 200 else None)
    center = meds[3]
    used = []
    for side in ((0, 1), (6, 5)):     # 3 区はなれた区 → 無ければ 2 区はなれた区
        k = next((q for q in side if meds[q] is not None), None)
        if k is not None:
            used.append(k)
    ends = [meds[k] for k in used]
    ratio = rnd(center / max(float(np.mean(ends)), 1.0), 2) if (center is not None and ends) else None
    # 画面の 7 等分 (参考)
    sm = []
    for k in range(7):
        ca, cb = int(k * seg), int((k + 1) * seg)
        m = stage[y0:y1, ca:cb]
        sm.append(float(np.median(L[y0:y1, ca:cb][m])) if m.sum() > 200 else None)
    scr = rnd(sm[3] / max((sm[0] + sm[6]) / 2, 1.0), 2) if all(v is not None for v in (sm[0], sm[3], sm[6])) else None
    # 床の光溜まりの頂点 (床の帯の列の平均を 61px でならした最大)
    m = stage[y0:y1]; blk = L[y0:y1]
    cnt = m.sum(0); su = np.where(m, blk, 0).sum(0)
    p = smooth_nan(np.where(cnt >= 0.3 * (y1 - y0), su / np.maximum(cnt, 1), np.nan), max(5, int(round(61 * sc))) | 1)
    pool = None
    if np.isfinite(p).any():
        xi = int(np.nanargmax(p))
        pool = rnd((xi - fx) / sc, 0)
        out['poolLum'] = rnd(float(p[xi]), 1)
    out.update(rows=[y0, y1], segments=meds, floorCenterOverEnds=ratio, endsUsed=used or None, screen7=scr, poolOffset=pool)
    return out


def ref_scene3(key, metrics, ref_dirs):
    """本家 (hd2d-a2-targets の ref_scene に Tomb のキャラの矩形を足す)"""
    s = A2.ref_scene(key, metrics, ref_dirs)
    R = REF_CHARS3.get(key)
    if R and s.chars is None:
        s.chars = A2.rect_mask(s.H, s.W, R['chars'])
        s.hero = A2.rect_mask(s.H, s.W, [R['hero']]); s.heroExact = True; s.charsExact = True
        s.notes = [n for n in s.notes if 'キャラの矩形が無い' not in n]
        s.notes.append('キャラの矩形は hd2d-a3-targets.py の REF_CHARS3 (目で読んだ値)')
    return s


def measure(s, sh=None):
    warnings.simplefilter('ignore', RuntimeWarning)
    sh = dict(SHAFT, **(sh or {}))
    r = A2.measure(s)        # 全画素・帯・色・箱庭の門・主人公の体と床・p95 は幕2 と同じ式
    L = T.lum(s.a); valid = ~s.ui
    stage = valid & (~s.chars if s.chars is not None else True)
    t2, outside = t2_shaft(s, L, stage, sh)
    head, head_src = head_row(s)
    t1 = t1_floor_peak(s, L, stage, head); t1['headSrc'] = head_src
    r['light'] = dict(T1=t1, T1incl=t1_floor_peak(s, L, valid, head), T2=t2, shape=shaft_shape(s, L, stage), T3=t3_floor7(s, L, stage, outside))
    if s.chars is None:
        r['notes'].append('T1 はキャラを除けなかった (除かない列と同じ)')
    if s.kind != 'ref' and r['light']['shape'].get('note'):
        r['notes'].append('柱の形: ' + r['light']['shape']['note'])
    # UI の歯止めの「帯」を足元の頂点に (幕2 の L1 の壁の最大は幕3 では頂点でない)
    g = r.get('guard') or {}
    for k in ('uiFaceOverBand', 'uiBrightOverBand'):
        g.pop(k, None)
    apex = t1.get('peakLum')
    if s.kind != 'ref' and apex:
        u = A2.ui_numbers(s, L, apex)
        if u:
            if 'uiFaceOverBand' in u:
                u['uiFaceOverApex'] = u.pop('uiFaceOverBand')
            if 'uiBrightOverBand' in u:
                u['uiBrightOverApex'] = u.pop('uiBrightOverBand')
            g.update(u)
    hb, hf = g.get('heroBody'), g.get('heroFloor')
    g['bodyOverFloor'] = rnd(hb / max(hf, 1.0), 2) if (hb is not None and hf is not None) else None
    r['guard'] = g
    return r


# ---- 表 (hd2d-a2-targets の表の作りを CHECKS だけ差し替えて使う)

class _Checks:
    def __enter__(self):
        self.saved = A2.CHECKS; A2.CHECKS = CHECKS

    def __exit__(self, *a):
        A2.CHECKS = self.saved


def judge_one(r, c):
    return A2.judge_one(r, c)


LEAD = ('★ 合否 = 計画 §1 の幕3 の光の 3 条件 (床が頂点・柱の中÷外・柱の外の壁と床の 7 分割)・☆ 目安・◆ 歯止め・— 参考。'
        '合否は目で決める (裁定)。数字は「どの層が足りないか」の補助。○ = 入った・× = 外れた・≈ = 合否を付けない (本家・主人公の型が近似・UI ありの画だけ)。'
        '本家 Tomb は探索画 (主人公が画面の真ん中) = 物差しの目安の出どころ・ot7 は戦闘の作りと数字。説明は docs/design/hd2d-stage2/measure.md §3。')


def md_table(results, title='幕3 の物差し (scripts/hd2d-a3-targets.py)'):
    with _Checks():
        return A2.md_table(results, title, LEAD)


def text_report(r):
    with _Checks():
        t = A2.text_report(r)
    sh = (r.get('light') or {}).get('shape') or {}
    for row in sh.get('rows') or []:
        if row.get('width') is not None:
            t += '\n    柱の横帯: 行 %s 最大 %s 地 %s 中心 %+dpx 幅 %spx' % (row['y'], row['peak'], row['base'], row['offset'], row['width'])
    return t


def measure_scene(folder, name, sh=None):
    q = A2.resolve(folder, name)
    s = A2.make_scene(q['stage'], q['stageLay'], q['units'], q['ui'], q['uiLay'], folder=folder, name=q['base'], ui_only=q['uiOnly'])
    r = measure(s, sh); r['label'] = q['base']
    return r


def measure_image(img, layout=None, units=None, ui=None, sh=None):
    lay = layout or (img[:-4] + '.layout.json')
    lj = A2.load_json(lay)
    ui_only = bool(lj) and not A2.is_hideui(lj) and ui is None
    s = A2.make_scene(img, lay if os.path.exists(lay) else None, units, ui, None, folder=os.path.dirname(os.path.abspath(img)), ui_only=ui_only)
    r = measure(s, sh)
    if lj is None:
        r['uiOnly'] = True
        r['notes'].append('layout.json が無い = UI の有無が分からない (舞台の物差しは合否なし)')
    r['label'] = os.path.basename(img)[:-4]
    return r


def measure_ref(key, metrics, ref_dirs, sh=None):
    s = ref_scene3(key, metrics, ref_dirs)
    r = measure(s, sh); r['label'] = '本家 %s (測り直し)' % key
    r['metricsKey'] = s.metricsKey
    return r


def combat_scene(folder, base):
    """--all で拾う物: 戦闘の静止画 (連写・-pre・地図などの戦闘以外・セーブの行は外す)"""
    if base.endswith('-pre'):
        return False
    for suf in ('-hideui-1.layout.json', '-1.layout.json', '-unitsonly-1.layout.json'):
        lj = A2.load_json(os.path.join(folder, base + suf))
        if lj:
            st = lj.get('state') or ''
            return 'phase=combat' in st and not any(k in st for k in ('play=', 'endplay=', 'entershots=', 'saveexit=', 'usegear=', 'fire='))
    return os.path.exists(os.path.join(folder, base + '-hideui-1.png'))


def main():
    global FLOOR7
    ap = argparse.ArgumentParser(description='HD-2D 段2 幕3 の物差し (合否は目で。数字は補助)')
    ap.add_argument('folder', nargs='?'); ap.add_argument('scenes', nargs='*')
    ap.add_argument('--all', action='store_true', help='フォルダの幕3 の戦闘の場面を全部 (--match の名前だけ)')
    ap.add_argument('--match', default=r'-[A-Z]3-', help='--all で拾う名前 (正規表現・既定は幕3 = PC-T3-・PC-O3-・PC-S3-…)')
    ap.add_argument('--image'); ap.add_argument('--layout'); ap.add_argument('--units'); ap.add_argument('--ui')
    ap.add_argument('--honke', action='store_true', help='本家 Tomb と ot7 を両方足す (= --ref tomb --ref ot7)')
    ap.add_argument('--ref', action='append', default=[], help='本家 (tomb・ot7・tombdungeon・everhold・sinking か metrics.json の A3-*)')
    ap.add_argument('--no-metrics-col', action='store_true', help='metrics.json の本家の値の列を足さない')
    ap.add_argument('--metrics', default=A2.DEFAULT_METRICS); ap.add_argument('--ref-dir', action='append', default=[])
    ap.add_argument('--shaft-angle', type=float, default=SHAFT['angle']); ap.add_argument('--shaft-width', type=float, default=SHAFT['width'])
    ap.add_argument('--shaft-dx', type=float, default=SHAFT['dx'])
    ap.add_argument('--floor-band', default=None, help='床の 7 分割の帯 = 足元の 上px,下px (既定 %d,%d。本家 Tomb は細い通路なので 10,30 で 4.5)' % FLOOR7)
    ap.add_argument('--md'); ap.add_argument('--json'); ap.add_argument('--quiet', action='store_true')
    a = ap.parse_args()
    sh = dict(angle=a.shaft_angle, width=a.shaft_width, dx=a.shaft_dx)
    if a.floor_band:
        FLOOR7 = tuple(float(v) for v in a.floor_band.split(','))
    metrics = A2.load_json(a.metrics) or {}
    ref_dirs = list(a.ref_dir) + A2.REF_DIRS
    refs = list(a.ref)
    if a.honke:
        refs = list(HONKE) + [k for k in refs if k not in HONKE]
    results = []
    for k in refs:
        try:
            results.append(measure_ref(k, metrics, ref_dirs, sh))
            mk = A2.REF_ALIAS.get(k, (k, None))[0]
            if not a.no_metrics_col and mk in metrics:
                results.append(A2.metrics_column(metrics[mk], '本家 %s (metrics.json)' % k))
        except SystemExit as e:
            print('本家を測れない:', e)
    if a.image:
        results.append(measure_image(a.image, a.layout, a.units, a.ui, sh))
    if a.folder:
        if a.all or not a.scenes:
            names = [n for n in A2.all_scenes(a.folder) if re.search(a.match, n) and combat_scene(a.folder, n)]
        else:
            names = a.scenes
        seen = set()
        for n in names:
            b = A2.scene_base(a.folder, n)
            if b in seen:
                continue
            seen.add(b)
            try:
                results.append(measure_scene(a.folder, b, sh))
            except SystemExit as e:
                print('測れない:', e)
    if not results:
        ap.error('測る物が無い (フォルダと場面・--image・--honke・--ref のどれか)')
    if not a.quiet:
        for r in results:
            if r.get('kind') != 'metrics':
                print(text_report(r))
    md = md_table(results)
    if a.md:
        open(a.md, 'w', encoding='utf-8').write(md)
    else:
        print(md)
    if a.json:
        json.dump(results, open(a.json, 'w', encoding='utf-8'), ensure_ascii=False, indent=1,
                  default=lambda o: o.item() if hasattr(o, 'item') else str(o))


if __name__ == '__main__':
    main()

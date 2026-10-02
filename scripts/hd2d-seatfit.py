#!/usr/bin/env python3
"""座席の表 (2026-09-30 P08。計画 docs/design/hd2d-slice-plan-2026-09-30.md §2-3・§4 P08 手順4・P10 手順3と7)。

Stage.ProjectFeet と LayoutCamera の式を写し、画角 (FOV 36/28/22。22 は見下ろし 10° と 12°) ごとに
  1. 敵の座席の t (道に沿った距離) の表: 2〜4体で画面の足元の間隔がそろい (差 ±5%)、いちばん右の敵の絵と帳面が画面の右端 −16 の内側
  2. 人形の座席の t: 画面の間隔が今 (36°) 以上
  3. 足元の線 (GroundLine = 世界の原点を画面の下から何割に置くか。Stage.GroundLineRatio と HD2DFlags.GroundLine と同じ読み) の候補ごとの合否
を出す。W2 の既定 (P10) と最終の線 (P20) の根拠。ゲームは起動しない (式だけ)。

使い方
  scripts/hd2d-seatfit.py [--md 表.md] [--json 表.json] [--layout <PH の .layout.json>] [--digit-bottom 40] [--check-w0] [--r2|--r3]
  --r3 = 三周目 r3 UI (旗 uilayout=r3＝箱庭の PC の既定・PC 22°・5°・0.36／スマホは旗 uilayoutphone=r3 の時だけ 22°・7°・0.45。スマホの既定は 2026-10-02 から二周目の 0.525) の隠す量と、足元の x の表 (1〜4体・人形・PC 0.407/0.36・スマホ 0.525/0.45) だけ
  --check-w0 = 今の式 (36°・12°・PC 0.45・スマホ 0.56・今の t) で W0 の撮影のログの足元と一致するかを確かめる (式の写し間違いの検査)
  --layout = スマホ相当で撮った layout.json から手札の本文の数字の下端 (digitsBottomGap の最小) を読み、手札を沈められる量に使う

写した式 (unity/Assets/Game/StageCamera.cs・StageSeats.cs・Stage.cs・BattleView.cs・BattleScreen.cs。2026-09-30 の値。UI は 2026-10-01 に P20 の値へ)
  PlaneUnitsPerScreen (基準深度の画面の高さ) = PC 10.8・スマホ 10.8 / clamp(アスペクト/1.8, 1, 1.2)
  _k = PUPS / 画面の高さ(px)・_dist = PUPS/2 / tan(FOV/2) (基準深度で 1 unit = 画面の高さ/PUPS px = 1080p で 100px)
  カメラ: 回転 Euler(pitch,0,0)・原点が画面の下から GroundLine の高さに来る位置から _dist 引く
  座席: OnPath(t, s) = Euler(0,-22°,0)·(t, 0, s)・高さ 0 (幕1 の戦闘の場は平らに均してある。見本の座席の帯も高さ 0)
  リーダー t=-5 s=0.9。敵 s = 偶数番 -0.5・奇数番 0.1 (スマホか3体以上) / 0.7 (PC の2体)。人形 = StageSeats.DollSlots
  人形の後列 = 前列の t +0.45・s +2.0 (見本・二周目 K13)。今の舞台は +0.15・+1.15
  UI (キャンバス単位・下から。見本の箱庭 = BattleScreen.Hd2dLayout の P20 の値):
           PC 足元の線 StatusLineY 285・敵の帳面の上端 285+140・自分の札 x 32〜(32+300+からくり 2枠 180)・上端 285+140・手札の上端 11+290×0.92 (手札を 19 沈める)
           スマホ StatusLineY -3+290+6=293・帳面 下端 293 高さ 53 (予告つき 79)・自分の札 x 24〜248 高さ 110・手札の上端 -3+290 (手札を 17 沈める)
           (2026-09-30 W2 の表は PC 300/30・スマホ 310/14 = 手札を沈める前。二周目の 2026-10-01 に P20 の値へ直した = scratchpad/hd2d/p20/seatfit_p20.py と同じ)
  帳面の幅 = 隣との間隔から (BattleScreen.StripW)。1体は PC 440・スマホ 420

三周目 r3 UI (2026-10-02 本番・仕様 docs/design/hd2d-slice/r3-ui-spec.md。段1 の試し撮り uitrial=1 を置き換えた):
  旗 uilayout=r3 (箱庭の PC の既定) と uilayoutphone=r3 (スマホ。既定は r2 = 二周目・2026-10-02 ユーザー「カードの下半分隠すのやっぱ見にくい」) = 足元の線 PC 0.36・スマホ 0.45・手札を PC 168・スマホ 176 沈める・敵の帳面を足元の下に 60/88 (スマホ 53/79)・
  PC は足元の帳 (x 204〜500) と左下の匣・スマホは足元の帳 (x 24〜384)。隠す量・要の数字の札の高さ・UI の面積と、C への足元の x の表を
  「三周目 r3 UI」の節と JSON の round3 に書く (--r3 で節だけ)

二周目のカメラ (2026-10-01 レーン A・計画 docs/design/hd2d-round2-plan-2026-10-01.md K1・K13):
  PC 22°・5°・足元の線 0.407／スマホ 22°・7°・0.525 の隠す量・地平線の行・画面の端の縦の傾き・敵の t と StageSeats の 22° の行の差・人形の後列の表を
  「二周目のカメラ」の節と JSON の round2 に書く (--r2 で節だけ)
"""
import argparse
import json
import math
import os
import re
import sys

PATH_YAW = -22.0
LEADER = (-5.0, 0.9)
OLD_T = {1: [4.6], 2: [3.2, 7.6], 3: [2.2, 5.9, 9.4], 4: [1.6, 4.6, 7.15, 11.2]}
DOLL_T = [-3.9, -3.05, -2.2, -1.35, -0.5]
# (22, 7)・(22, 5) は二周目のカメラ (2026-10-01。スマホ 7°・PC 5°)。表は両方の端末で出す
FOVS = [(36.0, 12.0), (28.0, 12.0), (22.0, 12.0), (22.0, 10.0), (22.0, 7.0), (22.0, 5.0)]
# 人形の後列のずらし (t・s): 見本は二周目 K13 の +0.45・+2.0 (StageSeats.R2A_DollBack*)、今の舞台は +0.15・+1.15
DOLL_BACK = {'diorama': (0.45, 2.0), 'old': (0.15, 1.15)}
# PC の人形の1体目 (灯籠の下を避けて手前の道 = StageSeats.DollSlots の nearFirst)。自分の札がこの足元を隠さないか
DOLL0_PC = (-3.9, 0.25)

PLATFORMS = {
    # UI は P20 (2026-09-30・BattleScreen.Hd2dLayout = 見本の箱庭の時): PC 足元の線 285・手札を 19 沈めて 11。スマホ 足元の線 293・手札 -3 (17 沈める)
    'PC': dict(W=1920, H=1080, cw=1920.0, ch=1080.0, phone=False, gl_now=0.45, art_w=256.0, boss_w=512.0,
               status=285.0, strip_top_over=140.0, self_right_min=32.0 + 300.0 + 2 * (68.0 + 10.0) + 24.0, perm_sec=236.0,
               self_top_over=140.0, hand_y=11.0, card_scale=0.92, edge=32.0),
    'PH': dict(W=1920, H=886, cw=1462.75, ch=675.0, phone=True, gl_now=0.56, art_w=153.6, boss_w=184.0 * 64 / 58.0,
               status=293.0, strip_h=53.0, strip_h_fc=79.0, self_card=(24.0, 248.0), self_card_h=110.0,
               band_left=24.0, set_w=2 * (68.0 + 10.0), hand_y=-3.0, card_scale=1.0, edge=24.0, intent_half=112.0),
}
CARD_H = 290.0

# 二周目のカメラ (2026-10-01 レーン A・計画 K1)。(端末, 画角, 見下ろし, 足元の線, 名前)
R2_CAMS = [
    ('PC', 22.0, 5.0, 0.407, '二周目の既定 (PC)'),
    ('PC', 22.0, 6.0, 0.407, '試しの変種 T-A-wolf-p6 (PC・見下ろし 6°・同じ足元の線)'),
    ('PH', 22.0, 7.0, 0.525, '二周目の既定 (スマホ)'),
]
STAGE_SEATS_CS = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'unity', 'Assets', 'Game', 'StageSeats.cs')


def rotY(t, s, deg=PATH_YAW):
    a = math.radians(deg)
    # Unity の Quaternion.Euler(0, deg, 0) * (t, 0, s)
    return (t * math.cos(a) + s * math.sin(a), 0.0, -t * math.sin(a) + s * math.cos(a))


class Cam:
    def __init__(self, plat, fov, pitch, gl):
        self.p = PLATFORMS[plat]
        self.W, self.H = self.p['W'], self.p['H']
        pups = 10.8
        if self.p['phone']:
            zoom = min(1.2, max(1.0, (self.W / self.H) / 1.8))
            pups = 10.8 / zoom
        self.pups = pups
        self.k = pups / self.H
        self.dist = (pups * 0.5) / math.tan(math.radians(fov) * 0.5)
        pr = math.radians(pitch)
        self.fwd = (0.0, -math.sin(pr), math.cos(pr))
        self.up = (0.0, math.cos(pr), math.sin(pr))
        dy = (self.H * gl - self.H * 0.5) * self.k
        p0 = (-self.up[0] * dy, -self.up[1] * dy, -self.up[2] * dy)
        self.base = (p0[0] - self.fwd[0] * self.dist, p0[1] - self.fwd[1] * self.dist, p0[2] - self.fwd[2] * self.dist)
        self.sf = self.H / self.p['ch']   # キャンバスの倍率 (PC 1・スマホ 886/675)

    def project(self, w):
        """世界の点 → (キャンバスの x・キャンバスの y (下から)・深さ)"""
        rel = (w[0] - self.base[0], w[1] - self.base[1], w[2] - self.base[2])
        d = rel[0] * self.fwd[0] + rel[1] * self.fwd[1] + rel[2] * self.fwd[2]
        x = rel[0]
        y = rel[0] * self.up[0] + rel[1] * self.up[1] + rel[2] * self.up[2]
        k = self.k * d / self.dist
        sx = self.W * 0.5 + x / k
        sy = self.H * 0.5 + y / k
        return sx / self.sf, sy / self.sf, d

    def seat(self, t, s):
        return self.project(rotY(t, s))


def enemy_s(plat, n, i):
    sB = 0.1 if (PLATFORMS[plat]['phone'] or n >= 3) else 0.7
    return -0.5 if i % 2 == 0 else sB


def strip_w(plat, gap, solo):
    if PLATFORMS[plat]['phone']:
        return 420.0 if solo else min(176.0, max(96.0, gap - 4.0))
    return 440.0 if solo else min(220.0, max(116.0, gap - 12.0))


def solve_t(cam, s, x_target, lo, hi=30.0):
    """画面の足元の x が x_target になる t (二分法。x は t について単調に増える)"""
    f = lambda t: cam.seat(t, s)[0]
    if f(lo) >= x_target:
        return lo
    if f(hi) < x_target:
        return hi
    for _ in range(60):
        m = (lo + hi) / 2
        if f(m) < x_target:
            lo = m
        else:
            hi = m
    return (lo + hi) / 2


def enemy_layout(plat, fov, pitch, gl, n, x_last_target=None):
    """敵の t を解く: 先頭の t は今のまま (人形の列の約束)、いちばん右の足元は x_last_target (今の画面の右端の位置)、間は画面で等間隔"""
    cam = Cam(plat, fov, pitch, gl)
    p = PLATFORMS[plat]
    t0 = OLD_T[n][0]
    ss = [enemy_s(plat, n, i) for i in range(n)]
    if n == 1:
        t = [solve_t(cam, ss[0], x_last_target, -5.0) if x_last_target is not None else t0]
    else:
        x0 = cam.seat(t0, ss[0])[0]
        xl = x_last_target
        # 右端の条件: いちばん右の絵と帳面の右端 ≦ キャンバスの幅 − 16
        for _ in range(40):
            gap = (xl - x0) / (n - 1)
            half = max(p['art_w'] / 2.0, strip_w(plat, gap, False) / 2.0)
            if xl + half <= p['cw'] - 16.0:
                break
            xl -= 4.0
        t = [t0]
        for i in range(1, n):
            xi = x0 + i * (xl - x0) / (n - 1)
            t.append(solve_t(cam, ss[i], xi, t[-1] + 0.05))
    pts = [cam.seat(t[i], ss[i]) for i in range(n)]
    return cam, t, ss, pts


def old_x_last(plat, n):
    cam = Cam(plat, 36.0, 12.0, PLATFORMS[plat]['gl_now'])
    return cam.seat(OLD_T[n][-1], enemy_s(plat, n, n - 1))[0]


def evaluate(plat, fov, pitch, gl, digit_bottom_px=None):
    """1つの (画角・見下ろし・足元の線) の合否。2〜4体と1体 (通常の絵とボスの絵)、人形"""
    p = PLATFORMS[plat]
    rows = []
    for n in (1, 2, 3, 4):
        cam, t, ss, pts = enemy_layout(plat, fov, pitch, gl, n, old_x_last(plat, n))
        xs = [q[0] for q in pts]
        gaps = [xs[i + 1] - xs[i] for i in range(n - 1)]
        spread = (max(gaps) - min(gaps)) / (sum(gaps) / len(gaps)) if gaps else 0.0
        mingap = min(gaps) if gaps else float('inf')
        sw = strip_w(plat, mingap, n == 1)
        art_right = xs[-1] + (p['boss_w'] if n == 1 else p['art_w']) / 2.0
        right = max(art_right, xs[-1] + sw / 2.0)
        lead = cam.seat(*LEADER)
        feet_enemy = [q[1] for q in pts]
        r = {'n': n, 't': [round(v, 3) for v in t], 's': ss, 'x': [round(v, 1) for v in xs], 'feetY': [round(v, 1) for v in feet_enemy],
             'gapSpread': round(spread, 4), 'rightEdge': round(right, 1), 'rightOk': right <= p['cw'] - 16.0 + 0.5,
             'depthRatio': round(pts[-1][2] / lead[2], 3), 'leaderX': round(lead[0], 1), 'leaderFeetY': round(lead[1], 1)}
        if p['phone']:
            ledger_top = p['status'] + p['strip_h']
            ledger_top_fc = p['status'] + p['strip_h_fc']
            hand_top = p['hand_y'] + CARD_H * p['card_scale']
            need = []
            for fy in feet_enemy:
                need.append(ledger_top - 16.0 - fy)            # 帳面 (予告なし) が足元を 16 以上隠さない
            lx = lead[0]
            c0, c1 = p['self_card']
            if c0 - 10 <= lx <= c1 + 10:
                need.append(p['status'] + p['self_card_h'] - 16.0 - lead[1])   # 左下の自分の札が脚を隠さない
            need.append(hand_top - lead[1])                     # 主人公の足元が手札より上
            for fy in feet_enemy:
                need.append(hand_top - fy)
            sink = max(0.0, max(need))
            r['feetHidden'] = round(max(0.0, ledger_top - min(feet_enemy)), 1)
            r['feetHiddenForecast'] = round(max(0.0, ledger_top_fc - min(feet_enemy)), 1)
            r['sinkNeeded'] = round(sink, 1)
            if digit_bottom_px is not None:
                max_sink = (digit_bottom_px - 36.0) / cam.sf
                r['sinkMax'] = round(max_sink, 1)
                r['sinkOk'] = sink <= max_sink + 0.01
            # いちばん左の敵の帳面 (下) と意図の札 (上) が自分の欄に入らない
            r['selfMargin'] = round(min(xs[0] - sw / 2.0 - (c1 + 8.0), xs[0] - p['intent_half'] - (p['band_left'] + p['set_w'] + 8.0)), 1)
            r['ok'] = r['rightOk'] and spread <= 0.05 + 1e-9 and r['selfMargin'] >= 0 and r.get('sinkOk', sink <= 0.01)
        else:
            ledger_top = p['status'] + p['strip_top_over']
            self_top = p['status'] + p['self_top_over']
            doll0 = cam.seat(*DOLL0_PC)
            r['feetHidden'] = round(max(0.0, ledger_top - min(feet_enemy)), 1)
            r['leaderHidden'] = round(max(0.0, self_top - lead[1]), 1)
            r['doll0Hidden'] = round(max(0.0, self_top - doll0[1]), 1)   # P20 の L2: 自分の札が人形の1体目の足元を隠す量
            r['feetOk'] = r['feetHidden'] < 16.0
            r['leaderOk'] = r['leaderHidden'] < 16.0
            r['doll0Ok'] = r['doll0Hidden'] < 16.0
            zone_right = xs[0] - sw / 2.0 - 8.0
            r['selfMargin'] = round(zone_right - p['self_right_min'], 1)
            r['selfMarginPerm'] = round(zone_right - p['self_right_min'] - p['perm_sec'], 1)
            r['ok'] = r['rightOk'] and spread <= 0.05 + 1e-9 and r['feetOk'] and r['leaderOk'] and r['doll0Ok'] and r['selfMargin'] >= 0
        rows.append(r)
    return rows


def doll_slots(plat, step, cam, back=DOLL_BACK['diorama']):
    """StageSeats.DollSlots の写し (9体)。back = 後列のずらし (t, s)。[(t, s, (キャンバス x, y (下から), 深さ))…]"""
    phone = PLATFORMS[plat]['phone']
    out = []
    for i in range(9):
        j = i % 5
        is_back = i >= 5
        near_first = i == 0 and not phone
        s = (0.25 if near_first else (0.9 if j % 2 == 0 else 0.25)) + (back[1] if is_back else 0.0)
        t = DOLL_T[0] + j * step + (back[0] if is_back else 0.0)
        out.append((t, s, cam.seat(t, s)))
    return out


def doll_layout(plat, fov, pitch, gl):
    """人形の t: 1体目は今のまま、刻みを伸ばして画面の間隔を今 (36°) 以上に。敵4体の先頭との間も今の間隔以上 (前列の間隔だけを見る = 後列のずらしに依らない)"""

    def slots(step, cam, back=DOLL_BACK['diorama']):
        return doll_slots(plat, step, cam, back)
    old_cam = Cam(plat, 36.0, 12.0, PLATFORMS[plat]['gl_now'])
    old = slots(0.85, old_cam, DOLL_BACK['old'])
    old_gaps = [old[i + 1][2][0] - old[i][2][0] for i in range(4)]
    e1_old = old_cam.seat(OLD_T[4][0], enemy_s(plat, 4, 0))[0]
    old_to_enemy = e1_old - old[4][2][0]
    cam = Cam(plat, fov, pitch, gl)
    step = 0.85
    for _ in range(200):
        cur = slots(step, cam)
        gaps = [cur[i + 1][2][0] - cur[i][2][0] for i in range(4)]
        if min(g / og for g, og in zip(gaps, old_gaps)) >= 1.0 - 1e-6:
            break
        step += 0.005
    cur = slots(step, cam)
    e1 = cam.seat(OLD_T[4][0], enemy_s(plat, 4, 0))[0]
    to_enemy = e1 - cur[4][2][0]
    return {'step': round(step, 3), 't': [round(c[0], 3) for c in cur[:5]], 'x': [round(c[2][0], 1) for c in cur[:5]],
            'minGapRatio': round(min((cur[i + 1][2][0] - cur[i][2][0]) / old_gaps[i] for i in range(4)), 3),
            'toEnemy1': round(to_enemy, 1), 'toEnemy1Old': round(old_to_enemy, 1), 'enemyOk': to_enemy >= old_to_enemy * 0.9}


def read_stage_seats_22(path=STAGE_SEATS_CS):
    """StageSeats.cs の座席の表の 22° の行 (敵 SeatEnemyPc/Phone の先頭10個・人形の刻み SeatDollStepPc/Phone の先頭) を読む。読めなければ None"""
    try:
        src = open(path, encoding='utf-8').read()
    except OSError:
        return None
    num = re.compile(r'-?[0-9]+(?:\.[0-9]+)?')

    def arr(name):
        m = re.search(r'\b' + name + r'\s*=\s*\{([^}]*)\}', src)
        return [float(v) for v in num.findall(m.group(1))] if m else None
    out = {}
    for plat, en, dl in (('PC', 'SeatEnemyPc', 'SeatDollStepPc'), ('PH', 'SeatEnemyPhone', 'SeatDollStepPhone')):
        e, d = arr(en), arr(dl)
        if not e or len(e) < 10 or not d:
            return None
        row = e[:10]
        out[plat] = {'enemyT': {1: row[0:1], 2: row[1:3], 3: row[3:6], 4: row[6:10]}, 'dollStep': d[0]}
    return out


def horizon_and_tilt(plat, fov, pitch):
    """地平線の行 (PNG の上から)・画面の端の縦の傾き (度。x = W/2 と 900px)。StageCamera.DebugCameraInfo の horizonRow・edgeTiltDeg と同じ式"""
    p = PLATFORMS[plat]
    W, H = p['W'], p['H']
    f = (H / 2.0) / math.tan(math.radians(fov) / 2.0)
    tp = math.tan(math.radians(pitch))
    return {'horizonRow': round(H / 2.0 - f * tp, 1),
            'edgeTiltDeg': round(math.degrees(math.atan((W / 2.0) * tp / f)), 2),
            'edgeTiltDeg900': round(math.degrees(math.atan(900.0 * tp / f)), 2),
            'focalPx': round(f, 1)}


def r2_hidden(plat, fov, pitch, gl, enemy_t=None):
    """二周目の隠す量 (画面の px。16 未満で合格 = layout-check L2)。enemy_t = {n: [t…]} (None なら解き直した t)。
    PC: 主人公の足・人形の1体目・敵の帳面 (1〜4体の最悪)。スマホ: 幕ボス (1体・予告つき 79)・2〜4体 (予告なし 53)・4体 (予告つき)・主人公と手札の間"""
    p = PLATFORMS[plat]
    cam = Cam(plat, fov, pitch, gl)
    sc = cam.sf
    lead = cam.seat(*LEADER)
    feet = {}
    for n in (1, 2, 3, 4):
        ts = enemy_t[n] if enemy_t is not None else enemy_layout(plat, fov, pitch, gl, n, old_x_last(plat, n))[1]
        feet[n] = [cam.seat(t, enemy_s(plat, n, i))[1] for i, t in enumerate(ts)]
    o = {}
    if p['phone']:
        hand_top = p['hand_y'] + CARD_H * p['card_scale']
        o['soloHiddenPx'] = round(max(0.0, p['status'] + p['strip_h_fc'] - min(feet[1])) * sc, 1)
        o['multiHiddenPx'] = round(max(max(0.0, p['status'] + p['strip_h'] - min(feet[n])) for n in (2, 3, 4)) * sc, 1)
        o['quadHiddenForecastPx'] = round(max(0.0, p['status'] + p['strip_h_fc'] - min(feet[4])) * sc, 1)
        o['heroGapPx'] = round((lead[1] - hand_top) * sc, 1)
        o['ok'] = o['soloHiddenPx'] <= 12.0 + 1e-9 and o['multiHiddenPx'] < 16.0 and o['heroGapPx'] > 0.0
    else:
        top = p['status'] + p['self_top_over']
        ltop = p['status'] + p['strip_top_over']
        d0 = cam.seat(*DOLL0_PC)
        o['heroHiddenPx'] = round(max(0.0, top - lead[1]) * sc, 1)
        o['doll0HiddenPx'] = round(max(0.0, top - d0[1]) * sc, 1)
        o['ledgerHiddenPx'] = round(max(max(0.0, ltop - min(feet[n])) for n in feet) * sc, 1)
        o['ok'] = o['heroHiddenPx'] < 16.0 and o['doll0HiddenPx'] < 16.0 and o['ledgerHiddenPx'] < 16.0
    o['heroFeetRow'] = round((p['ch'] - lead[1]) * sc, 1)
    o['enemyFeetRows'] = {str(n): [round((p['ch'] - y) * sc, 1) for y in feet[n]] for n in feet}
    return o


def r2_lowest_gl(plat, fov, pitch):
    """0.001 刻みで、r2_hidden が通るいちばん低い足元の線 (見込み: evalcam の lowest_gl と同じ判定)"""
    rng = range(330, 470) if not PLATFORMS[plat]['phone'] else range(430, 600)
    for g in rng:
        gl = g / 1000.0
        if r2_hidden(plat, fov, pitch, gl)['ok']:
            return gl
    return None


def r2_table_check(plat, fov, pitch, gl, tbl):
    """StageSeats の 22° の行の t をこのカメラで置いた時の、画面の間隔の差・右端と、解き直した t との差"""
    p = PLATFORMS[plat]
    cam = Cam(plat, fov, pitch, gl)
    rows = []
    for n in (1, 2, 3, 4):
        _, solved, ss, _ = enemy_layout(plat, fov, pitch, gl, n, old_x_last(plat, n))
        tt = tbl['enemyT'][n] if tbl else solved
        pts = [cam.seat(t, enemy_s(plat, n, i)) for i, t in enumerate(tt)]
        xs = [q[0] for q in pts]
        gaps = [xs[i + 1] - xs[i] for i in range(n - 1)]
        spread = (max(gaps) - min(gaps)) / (sum(gaps) / len(gaps)) if gaps else 0.0
        sw = strip_w(plat, min(gaps) if gaps else float('inf'), n == 1)
        right = max(xs[-1] + (p['boss_w'] if n == 1 else p['art_w']) / 2.0, xs[-1] + sw / 2.0)
        rows.append({'n': n, 'tableT': [round(v, 3) for v in tt], 'solvedT': [round(v, 3) for v in solved],
                     'maxDiff': round(max(abs(a - b) for a, b in zip(tt, solved)), 3),
                     'x': [round(v * cam.sf, 1) for v in xs], 'gapSpread': round(spread, 4), 'rightEdge': round(right, 1),
                     'rightOk': right <= p['cw'] - 16.0 + 0.5})
    dl = doll_layout(plat, fov, pitch, gl)
    return rows, dl


def r2_doll_back(plat, fov, pitch, gl, step, back):
    """人形9体の後列 (5〜8体目) の足元 (= 頭。人形の絵は画面で同じ大きさ) が、画面の x がいちばん近い前列の人形より何 px 上・何 px 横か"""
    cam = Cam(plat, fov, pitch, gl)
    sl = doll_slots(plat, step, cam, back)
    sc = cam.sf
    front = sl[:5]
    out = []
    for b in range(5, 9):
        bx, by = sl[b][2][0], sl[b][2][1]
        f = min(range(5), key=lambda i: abs(front[i][2][0] - bx))
        out.append({'back': b + 1, 'nearestFront': f + 1, 'upPx': round((by - front[f][2][1]) * sc, 1),
                    'sidePx': round(abs(bx - front[f][2][0]) * sc, 1),
                    'backPx': [round(bx * sc, 1), round((PLATFORMS[plat]['ch'] - by) * sc, 1)]})
    fgaps = [round((front[i + 1][2][0] - front[i][2][0]) * sc, 1) for i in range(4)]
    return out, fgaps


def round2(md_lines):
    """「二周目のカメラ」の節と JSON (round2)"""
    tbl = read_stage_seats_22()
    md_lines += ['## 二周目のカメラ (2026-10-01・レーン A・計画 K1・K13)', '',
                 'カメラ 22°・見下ろし PC 5°／スマホ 7°・足元の線 PC 0.407／スマホ 0.525 (Stage.GroundLineRatio の箱庭の既定)。UI は P20 の値 (先頭の注記)。',
                 '隠す量は画面の px (1920×1080 か 1920×886)。敵の t は StageSeats.cs の 22° の行 (%s)。' % ('読めた' if tbl else '読めない = 解き直した t で計算'), '']
    out = []
    for plat, fov, pitch, gl, name in R2_CAMS:
        cam = Cam(plat, fov, pitch, gl)
        ht = horizon_and_tilt(plat, fov, pitch)
        t_tbl = tbl[plat] if tbl else None
        hid = r2_hidden(plat, fov, pitch, gl, t_tbl['enemyT'] if t_tbl else None)
        low = r2_lowest_gl(plat, fov, pitch)
        rows, dl = r2_table_check(plat, fov, pitch, gl, t_tbl)
        step_tbl = t_tbl['dollStep'] if t_tbl else dl['step']
        back_new, fgaps = r2_doll_back(plat, fov, pitch, gl, step_tbl, DOLL_BACK['diorama'])
        back_old, _ = r2_doll_back(plat, fov, pitch, gl, step_tbl, DOLL_BACK['old'])
        max_diff = max(r['maxDiff'] for r in rows)
        ent = {'platform': plat, 'fov': fov, 'pitch': pitch, 'groundLine': gl, 'name': name,
               'dist': round(cam.dist, 3), 'camY': round(cam.base[1], 3), **ht, 'hidden': hid, 'lowestOkGroundLine': low,
               'enemyTable': rows, 'enemyTableMaxDiff': max_diff, 'enemyTableKept': max_diff < 0.03,
               'dollStepTable': step_tbl, 'dollStepSolved': dl['step'], 'dollStepDiff': round(abs(step_tbl - dl['step']), 3),
               'dollFrontGapsPx': fgaps, 'dollBack': {'diorama': back_new, 'old': back_old}}
        out.append(ent)
        md_lines += ['### %s: %s・画角 %.0f°・見下ろし %.0f°・足元の線 %.3f' % (name, 'PC' if plat == 'PC' else 'スマホ相当', fov, pitch, gl), '',
                     '- 距離 %.2f unit・カメラの高さ %.2f unit・焦点 %.0fpx。**地平線の行 %.0f**・画面の端の縦の傾き %.2f° (中心から 900px で %.2f°)' % (
                         cam.dist, cam.base[1], ht['focalPx'], ht['horizonRow'], ht['edgeTiltDeg'], ht['edgeTiltDeg900'])]
        if plat == 'PC':
            md_lines.append('- 隠す量: 主人公の足 %.1f・人形の1体目 %.1f・敵の帳面 (1〜4体の最悪) %.1f px (16 未満で合格) → %s。通るいちばん低い線 %s' % (
                hid['heroHiddenPx'], hid['doll0HiddenPx'], hid['ledgerHiddenPx'], '○' if hid['ok'] else '×', '%.3f' % low if low else 'なし'))
        else:
            md_lines.append('- 隠す量: 幕ボスの帳面 (予告つき 79) がボスの足元を %.1f px (12 以下で合格)・2〜4体の帳面 (53) %.1f px・4体で予告つき %.1f px・主人公と手札の間 %.1f px → %s。通るいちばん低い線 %s' % (
                hid['soloHiddenPx'], hid['multiHiddenPx'], hid['quadHiddenForecastPx'], hid['heroGapPx'], '○' if hid['ok'] else '×', '%.3f' % low if low else 'なし'))
        md_lines.append('- 足元の行 (上から px): 主人公 %.0f・敵 %s' % (hid['heroFeetRow'], ' / '.join('%s体 %s' % (k, ','.join('%.0f' % v for v in vv)) for k, vv in hid['enemyFeetRows'].items())))
        md_lines += ['', '| 体数 | 表の t (22°) | 解き直した t | 差の最大 | 表の t の足元 x (px) | 間隔の差 | 右端 (キャンバス) |', '|---|---|---|---|---|---|---|']
        for r in rows:
            md_lines.append('| %d | %s | %s | %.3f | %s | %.1f%% | %.0f%s |' % (r['n'], ','.join('%.3f' % v for v in r['tableT']), ','.join('%.3f' % v for v in r['solvedT']),
                                                                         r['maxDiff'], ','.join('%.0f' % v for v in r['x']), r['gapSpread'] * 100, r['rightEdge'], '' if r['rightOk'] else ' ⚠'))
        md_lines += ['', '- 敵の t の表: 22° の行との差の最大 %.3f → %s' % (max_diff, '**表はそのまま** (0.03 未満)' if max_diff < 0.03 else '**差が大きい行を直す** (0.03 以上)'),
                     '- 人形の前列の刻み: 表 %.3f・解き直し %.3f (差 %.3f)。前列の足元の間隔 %s px' % (step_tbl, dl['step'], abs(step_tbl - dl['step']), ' / '.join('%.0f' % g for g in fgaps)), '',
                     '人形9体の後列 (人形の絵は画面で同じ大きさ = 頭の差は足元の差)。「上」= 後列の頭が、画面の x がいちばん近い前列の人形の頭より上へ出る px (試しのビルドの門 K13 = 20px 以上)。', '',
                     '| 後列 | いちばん近い前列 | 上 (二周目 +0.45・+2.0) | 横 | 上 (W5 の +0.15・+1.15) | 横 |', '|---|---|---|---|---|---|']
        for a, b in zip(back_new, back_old):
            md_lines.append('| %d体目 | %d体目 (W5: %d体目) | %.1f | %.1f | %.1f | %.1f |' % (a['back'], a['nearestFront'], b['nearestFront'], a['upPx'], a['sidePx'], b['upPx'], b['sidePx']))
        ups = [a['upPx'] for a in back_new]
        md_lines += ['', '- 二周目の後列: 上 %.1f〜%.1f px・横 %.1f〜%.1f px (K13 の門 20px 以上: %s)' % (
            min(ups), max(ups), min(a['sidePx'] for a in back_new), max(a['sidePx'] for a in back_new), '○' if min(ups) >= 20.0 else '×'), '']
    return out


# ------------------------------------------------------------------ 三周目 r3 UI (2026-10-02 本番・仕様 docs/design/hd2d-slice/r3-ui-spec.md)
# 旗 uilayout=r3 (箱庭の PC の既定) と uilayoutphone=r3 (スマホ。2026-10-02 から既定は r2) の UI。BattleScreen.R3U_* / R3A_*・BattleView.R3U_Fan*・StageCamera.R3U_* の写し。
# (段1 の試し撮り uitrial=1 = 手札 183・自分の札 300×62 の表はこの節に置き換えた。uitrial=1 は今は r3 の別名)
R3_UI = {
    'PC': dict(gl=0.36, fov=22.0, pitch=5.0, status=285.0, sink=168.0, hand_base=30.0, card_scale=0.92, lift=70.0,
               ledger_h=60.0, ledger_h_fc=88.0, ledger_gap=10.0,
               foot_x0=204.0, foot_x1=500.0, foot_h=74.0, foot_h_status=96.0, foot_gap=10.0,
               box_x0=32.0, box_w=152.0, box_bottom=268.0, box_top_row=480.0,
               orb=(32.0, 116.0, 160.0, 244.0), lantern=(172.0, 116.0, 268.0, 260.0)),
    'PH': dict(gl=0.45, fov=22.0, pitch=7.0, status=None, sink=176.0, hand_base=14.0, card_scale=1.0, lift=130.0,
               ledger_h=53.0, ledger_h_fc=79.0, ledger_gap=8.0,
               foot_x0=24.0, foot_w=360.0, foot_h=80.0, foot_gap=8.0,
               orb=(24.0, 68.0, 152.0, 196.0), lantern=(164.0, 68.0, 244.0, 188.0), piles_y=14.0, end_top_min=465.0, end_h=64.0,
               band=(24.0, 62.0, 170.0)),
}
R3_FAN = dict(drop=4.0, drop_max=12.0, tilt=3.0, tilt_max=6.0, drop_margin=111.0, hover_scale=1.18, unplay_sink=0.0, drop_below_lift_top=15.6,
              keynum=dict(x0=-88.0, x1=-42.0, y_top=95.0, y_bot=71.0))   # 要の数字の札 (札の中心からの単位。札 200×290・x 12〜58・上から 50〜74)
R3_HERO_ART = {'PC': (352.0, 256.0), 'PH': (352.0 * 0.6, 256.0 * 0.6)}   # 主人公の絵の箱 (このは 88×64 ドット×4・スマホ ×0.6)
R3_DOLL_HALF = {'PC': 64.0, 'PH': 38.4}
R3_FEET_ZONE = 60.0
# 三周目の足元の x の表 (C の申し送り): (端末, 画角, 見下ろし, 足元の線, 名前)
R3_FEET_CAMS = [
    ('PC', 22.0, 5.0, 0.407, '二周目の既定 (PC・uilayout=r2)'),
    ('PC', 22.0, 5.0, 0.36, '三周目 r3 (PC・uilayout=r3＝箱庭の既定)'),
    ('PH', 22.0, 7.0, 0.525, 'スマホの既定 (二周目の割り付け・uilayoutphone=r2。2026-10-02 から)'),
    ('PH', 22.0, 7.0, 0.45, '三周目 r3 (スマホ・uilayoutphone=r3。既定ではない)'),
]


def r3_feet_table(md_lines):
    """1〜4体・人形9体・主人公の足元 (画面の px・上から) と t/s。x は足元の線によらない (カメラは上下に平行移動するだけ)"""
    tbl = read_stage_seats_22()
    out = []
    md_lines += ['### 足元の x の表 (C の申し送り。画面の px・y は上から。PC 1920×1080・スマホ相当 1920×886)', '',
                 '敵の t は StageSeats.cs の 22° の行 (%s)。人形は前列 (1〜5体目) と後列 (6〜9体目・二周目の +0.45・+2.0)。' % ('読めた' if tbl else '読めない = 解き直した t'),
                 'カメラは上下に平行移動するだけなので、**x は足元の線によらず同じ** (足元の線で行だけが変わる)。', '']
    for plat, fov, pitch, gl, name in R3_FEET_CAMS:
        cam = Cam(plat, fov, pitch, gl)
        sc = cam.sf
        ch = PLATFORMS[plat]['ch']
        hero = cam.seat(*LEADER)
        ent = {'platform': plat, 'fov': fov, 'pitch': pitch, 'groundLine': gl, 'name': name,
               'hero': {'t': LEADER[0], 's': LEADER[1], 'x': round(hero[0] * sc, 1), 'row': round((ch - hero[1]) * sc, 1)},
               'enemies': {}, 'dolls': []}
        md_lines += ['#### %s: 画角 %.0f°・見下ろし %.0f°・足元の線 %.3f' % (name, fov, pitch, gl), '',
                     '| 座席 | t | s | 足元 x | 足元の行 (上から) |', '|---|---|---|---|---|',
                     '| 主人公 | %.2f | %.2f | %.0f | %.0f |' % (LEADER[0], LEADER[1], ent['hero']['x'], ent['hero']['row'])]
        for n in (1, 2, 3, 4):
            ts = tbl[plat]['enemyT'][n] if tbl else enemy_layout(plat, fov, pitch, gl, n, old_x_last(plat, n))[1]
            lst = []
            for i, t in enumerate(ts):
                s = enemy_s(plat, n, i)
                q = cam.seat(t, s)
                e = {'t': round(t, 3), 's': s, 'x': round(q[0] * sc, 1), 'row': round((ch - q[1]) * sc, 1)}
                lst.append(e)
                md_lines.append('| 敵 %d体の%d | %.3f | %.2f | %.0f | %.0f |' % (n, i + 1, t, s, e['x'], e['row']))
            ent['enemies'][str(n)] = lst
        step = tbl[plat]['dollStep'] if tbl else doll_layout(plat, fov, pitch, gl)['step']
        for i, (t, s, q) in enumerate(doll_slots(plat, step, cam)):
            d = {'t': round(t, 3), 's': round(s, 2), 'x': round(q[0] * sc, 1), 'row': round((ch - q[1]) * sc, 1)}
            ent['dolls'].append(d)
            md_lines.append('| 人形%d | %.3f | %.2f | %.0f | %.0f |' % (i + 1, t, s, d['x'], d['row']))
        md_lines.append('')
        out.append(ent)
    return out


def r3_box_h(slots, gears, perms, room):
    """PC の匣の高さ (BattleScreen.R3U_PcBox の写し)。room = 下端から上端の上限までの高さ。畳み: ①置物を1段 ②ギアを1段 ③名前つきのギアを 46 の1段"""
    head, gap, tok_h, small, pitch_y, chip_h, chip_pitch = 22.0, 8.0, 74.0, 46.0, 53.0, 32.0, 38.0
    set_rows = max(1, (slots + 1) // 2)
    h_set = head + set_rows * tok_h + (set_rows - 1) * 8.0 + 4.0
    gear_rows = 0 if gears <= 0 else (1 if gears <= 3 else 2)
    named = 0 < gears <= 2
    perm_rows = 2 if perms >= 2 else perms

    def gear_h():
        if gears <= 0:
            return 0.0
        return head + (tok_h if named else gear_rows * small + (gear_rows - 1) * (pitch_y - small)) + 4.0

    def perm_h():
        return 0.0 if perms <= 0 else head + (chip_h + (perm_rows - 1) * chip_pitch if perm_rows > 0 else 0.0) + 4.0

    def total():
        t = h_set
        if gears > 0:
            t += gap + gear_h()
        if perms > 0:
            t += gap + perm_h()
        return t
    if total() > room and perm_rows > 1:
        perm_rows = 1
    if total() > room and gear_rows > 1 and not named:
        gear_rows = 1
    if total() > room and named:
        named, gear_rows = False, 1
    return total()


def r3_keynum_gaps(plat):
    """手札 1〜10枚の、休んでいる札の要の数字の札の下端 (画面の下から px) の最小 (扇の下がりと傾きの頭打ち込み)。BattleView.SyncHand の写し"""
    U = R3_UI[plat]
    F = R3_FAN
    k = U['card_scale']
    hand_y = U['hand_base'] - U['sink']
    sc = PLATFORMS[plat]['H'] / PLATFORMS[plat]['ch']
    out = []
    for n in range(1, 11):
        c = (n - 1) / 2.0
        worst = 1e9
        for i in range(n):
            d = abs(i - c)
            cy = hand_y + 145.0 * k - min(d * F['drop'], F['drop_max']) - F['unplay_sink']   # 出せない札の沈み (BattleView.R3U_UnplayableSink。ターンの終わりは全部の札が出せない＝最悪で数える)
            ang = math.radians(max(-F['tilt_max'], min(F['tilt_max'], -(i - c) * F['tilt'])))
            for x in (F['keynum']['x0'], F['keynum']['x1']):
                for y in (F['keynum']['y_bot'],):
                    yy = x * k * math.sin(ang) + y * k * math.cos(ang)
                    worst = min(worst, (cy + yy) * sc)
        out.append(round(worst, 1))
    return out


def r3_ui(md_lines, plat, tbl):
    """三周目 r3 の割り付けで、帳面・自分の欄・手札が足元を何 px 隠すか (16 未満で合格)・要の数字の札の高さ・UI の面積の見込み"""
    U = R3_UI[plat]
    p = PLATFORMS[plat]
    cam = Cam(plat, U['fov'], U['pitch'], U['gl'])
    sc = cam.sf
    ch, cw = p['ch'], p['cw']
    hero = cam.seat(*LEADER)
    hand_h = CARD_H * U['card_scale']
    hand_y = U['hand_base'] - U['sink']
    hand_top = hand_y + hand_h
    status = U['status'] if U['status'] is not None else hand_top + 6.0   # スマホの足元の線は手札の上端＋6
    o = {'platform': plat, 'fov': U['fov'], 'pitch': U['pitch'], 'groundLine': U['gl'],
         'heroFeet': [round(hero[0], 1), round(hero[1], 1)], 'heroFeetRowPx': round((ch - hero[1]) * sc, 1),
         'heroFeetTopPct': round(100.0 * (ch - hero[1]) / ch, 1), 'statusLine': round(status, 1), 'handTop': round(hand_top, 1),
         'handTopRowPx': round((ch - hand_top) * sc, 1)}
    step = tbl[plat]['dollStep'] if tbl else doll_layout(plat, U['fov'], U['pitch'], U['gl'])['step']
    dolls = [q for (_, _, q) in doll_slots(plat, step, cam)]
    rects = {}   # 名前 → (x0, y0, x1, y1) キャンバス・下から
    # 足元の帳
    if plat == 'PC':
        foot_top = hero[1] - U['foot_gap']
        dollmin = min(q[0] for q in dolls)
        x1_white = min(U['foot_x1'], dollmin - 38.0)
        rects['foot'] = (U['foot_x0'], foot_top - U['foot_h'], U['foot_x1'], foot_top)
        rects['foot_status'] = (U['foot_x0'], foot_top - U['foot_h_status'], U['foot_x1'], foot_top)
        o['footRightWhite'] = round(x1_white, 1)
        # 匣 (BattleScreen.R3U_PcBox の写し。典型: からくり2・ギア2・置物2 / 最悪: 仕込み枠3 (かすみ・二重の符) ×{ギア2・ギア10}×置物多数
        # = ①置物を1段 → ②ギアを1段 → ③名前つきのギアを 46 の1段 の順に畳む。仕込み枠5 (かすみ＋二重の符＋罠師の茂み) は畳めない例外として別に出す)
        lim = ch - U['box_top_row']
        typ = r3_box_h(2, 2, 2, lim - U['box_bottom'])
        worst = max(r3_box_h(3, ng, 4, lim - U['box_bottom']) for ng in (1, 2, 3, 6, 10))
        slots5 = r3_box_h(5, 2, 2, lim - U['box_bottom'])
        rects['box_typ'] = (U['box_x0'], U['box_bottom'], U['box_x0'] + U['box_w'], U['box_bottom'] + typ)
        rects['box_worst'] = (U['box_x0'], U['box_bottom'], U['box_x0'] + U['box_w'], U['box_bottom'] + worst)
        o['boxTopRow'] = {'typ': round(ch - (U['box_bottom'] + typ), 1), 'worst': round(ch - (U['box_bottom'] + worst), 1),
                          'slots5': round(ch - (U['box_bottom'] + slots5), 1), 'limit': U['box_top_row']}
    else:
        foot_top = hero[1] - U['foot_gap']
        rects['foot'] = (U['foot_x0'], foot_top - U['foot_h'], U['foot_x0'] + U['foot_w'], foot_top)
        o['footRowsFromTop'] = [round(ch - foot_top, 1), round(ch - foot_top + U['foot_h'], 1)]
    rects['orb'] = U['orb']
    o['footVsOrbGap'] = round(rects['foot'][1] - U['orb'][3], 1)   # 足元の帳の下端と輪の上端の間 (正 = 空いている)
    # 足元の帳が主人公・人形の足元を隠す量
    o['heroHiddenByFoot'] = round(max(0.0, rects['foot'][3] - hero[1]), 1)
    dh = 0.0
    for q in dolls:
        if q[0] + R3_DOLL_HALF[plat] > rects['foot'][0] and q[0] - R3_DOLL_HALF[plat] < (o.get('footRightWhite') or rects['foot'][2]):
            dh = max(dh, rects['foot'][3] - q[1])
    o['dollHiddenByFoot'] = round(max(0.0, dh), 1)
    # 敵の帳面 (足元の gap 下に h)
    worst_own, worst_nb, nb_at, lowest = 0.0, 0.0, None, float('inf')
    ledgers = {}
    for n in (1, 2, 3, 4):
        ts = tbl[plat]['enemyT'][n] if tbl else enemy_layout(plat, U['fov'], U['pitch'], U['gl'], n, old_x_last(plat, n))[1]
        pts = [cam.seat(t, enemy_s(plat, n, i)) for i, t in enumerate(ts)]
        xs = [q[0] for q in pts]
        lst = []
        for i, q in enumerate(pts):
            ngap = min([abs(xs[j] - xs[i]) for j in range(n) if j != i] or [float('inf')])
            w = strip_w(plat, min(xs[j + 1] - xs[j] for j in range(n - 1)) if n > 1 else ngap, n == 1)
            top = max(status + U['ledger_h'], q[1] - U['ledger_gap'])
            own = max(0.0, top - q[1])
            worst_own = max(worst_own, own)
            lowest = min(lowest, top - U['ledger_h_fc'])
            for j, qj in enumerate(pts):
                if j == i:
                    continue
                if q[0] + w / 2.0 > qj[0] - R3_FEET_ZONE and q[0] - w / 2.0 < qj[0] + R3_FEET_ZONE:
                    over = top - qj[1]
                    if over > worst_nb:
                        worst_nb, nb_at = over, '%d体の%d番の帳面 → %d番の足元' % (n, i + 1, j + 1)
            lst.append({'x': round(q[0], 1), 'w': round(w, 1), 'rowsPx': [round((ch - top) * sc, 1), round((ch - top + U['ledger_h']) * sc, 1), round((ch - top + U['ledger_h_fc']) * sc, 1)]})
        ledgers[str(n)] = lst
    o['ledgers'] = ledgers
    o['ledgerHidesOwnFeet'] = round(worst_own, 1)
    o['ledgerHidesNeighborFeet'] = round(max(0.0, worst_nb), 1)
    o['ledgerHidesNeighborAt'] = nb_at
    o['ledgerLowestRowPx'] = round((ch - lowest) * sc, 1)
    # 手札
    o['handVisiblePct'] = round(100.0 * hand_top / ch, 1)
    o['heroAboveHand'] = round(hero[1] - hand_top, 1)
    raised_c = hand_y + hand_h / 2.0 + U['lift'] + U['sink']
    rh = CARD_H * R3_FAN['hover_scale'] / 2.0   # 触れた札の localScale は 1.18 そのもの (CardScale を掛けない。HookHandCard の PointerEnter)
    o['raisedCardRowsPx'] = [round((ch - raised_c - rh) * sc, 1), round((ch - raised_c + rh) * sc, 1)]
    # 「場に出す」線: PC は触れて上がった札の上端の 15.6 下 (二周目と同じ高さ)・スマホは沈めた手札の上端＋111 (BattleScreen.DropLineScreen)
    drop_line = (raised_c + rh - R3_FAN['drop_below_lift_top']) if plat == 'PC' else (hand_top + R3_FAN['drop_margin'])
    o['dropLineRowPx'] = round((ch - drop_line) * sc, 1)
    o['keynumBottomGapPx'] = r3_keynum_gaps(plat)
    o['keynumOk'] = min(o['keynumBottomGapPx']) >= 36.0
    # スマホ: ターン終了の上端 (敵の帳の最下端＋6・手札の上端より上)
    if plat == 'PH':
        end_top = max(U['end_top_min'], (ch - lowest) + 6.0)
        end_top = min(end_top, (ch - hand_top) - U['end_h'] - 4.0)
        o['endTurnTopFromTop'] = round(end_top, 1)
    # 主人公の周りの UI (主人公の絵の箱と重なる常時の UI。足元の帳は足元の下なので数えない)
    aw, ah = R3_HERO_ART[plat]
    hero_box = (hero[0] - aw / 2.0, hero[1], hero[0] + aw / 2.0, hero[1] + ah)
    around = []
    for name, r in rects.items():
        if name in ('foot', 'foot_status'):
            continue
        if r[0] < hero_box[2] and r[2] > hero_box[0] and r[1] < hero_box[3] and r[3] > hero_box[1]:
            around.append(name)
    o['uiAroundHero'] = around
    # UI の面積の見込み (常時の矩形の和。重なりは引かない＝上限の見込み)
    area = 0.0
    area += cw * ((p['phone'] and 56.0) or 72.0)                       # 上部バー
    area += (4 * 196.0 + 184.0 if not p['phone'] else (cw - 305.0) - (24.0 + 150.0 + 24.0)) * max(0.0, hand_top)   # 休んでいる手札 5枚 (PC 間隔 196・スマホは山札の右＋24〜幅−305)
    fr = rects['foot_status'] if 'foot_status' in rects else rects['foot']
    area += (fr[2] - fr[0]) * (fr[3] - fr[1])
    if 'box_typ' in rects:
        r = rects['box_typ']; area += (r[2] - r[0]) * (r[3] - r[1])
    r = U['orb']; area += (r[2] - r[0]) * (r[3] - r[1])
    for n in ('2',):
        for L in ledgers[n]:
            area += L['w'] * U['ledger_h']
    area += 2 * (130.0 if not p['phone'] else 150.0) * 52.0 + 230.0 * 64.0   # 山札・捨て札・ターン終了
    if p['phone']:
        b = U['band']; area += 300.0 * (b[2] - b[1])
    o['uiAreaPct2Enemies'] = round(100.0 * area / (cw * ch), 1)
    o['ok'] = o['heroHiddenByFoot'] < 16.0 and o['dollHiddenByFoot'] < 16.0 and o['ledgerHidesOwnFeet'] < 16.0 and \
        o['ledgerHidesNeighborFeet'] < 16.0 and o['heroAboveHand'] > 0.0 and o['keynumOk'] and \
        (plat != 'PC' or o['boxTopRow']['worst'] >= U['box_top_row'] - 0.5) and o['footVsOrbGap'] >= 0.0
    pn = 'PC' if plat == 'PC' else 'スマホ相当'
    md_lines += ['### %s: 画角 %.0f°・見下ろし %.0f°・足元の線 %.2f・手札の沈め %.0f' % (pn, U['fov'], U['pitch'], U['gl'], U['sink']), '',
                 '- 主人公の足元: 行 %.0f px (上から %.1f%%)。足元の線 (StatusLineY) %.1f・手札の上端 %.1f (行 %.0f px・画面の %.1f%%)。主人公の足元は手札の上端の %.1f 上' % (
                     o['heroFeetRowPx'], o['heroFeetTopPct'], status, hand_top, o['handTopRowPx'], o['handVisiblePct'], o['heroAboveHand']),
                 '- 足元の帳: x %.0f〜%.0f%s・行 %.0f〜%.0f (キャンバス・上から) → 主人公の足元を **%.1f**・人形の足元を **%.1f** 隠す (16 未満で合格)。輪の上端との間 %.1f' % (
                     rects['foot'][0], rects['foot'][2], ('（白は人形の座席の 38 手前 = %.0f）' % o['footRightWhite']) if plat == 'PC' else '',
                     ch - rects['foot'][3], ch - (rects['foot_status'] if 'foot_status' in rects else rects['foot'])[1], o['heroHiddenByFoot'], o['dollHiddenByFoot'], o['footVsOrbGap'])]
    if plat == 'PC':
        md_lines.append('- 匣: x %.0f〜%.0f・下端 行 %.0f。上端 典型 行 %.0f・最悪 (仕込み枠3・畳んだ後) 行 %.0f (上限 %.0f)。仕込み枠5 は畳めない例外で 行 %.0f' % (
            U['box_x0'], U['box_x0'] + U['box_w'], ch - U['box_bottom'], o['boxTopRow']['typ'], o['boxTopRow']['worst'], U['box_top_row'], o['boxTopRow']['slots5']))
    else:
        md_lines.append('- ターン終了の上端 (上から): %.0f (敵の帳の最下端＋6・下限 465・手札の上端より上)' % o['endTurnTopFromTop'])
    md_lines += ['- 敵の帳面: 自分の足元を **%.1f**・隣の敵の足元の通り (±60) を **%.1f** 隠す%s。いちばん下の帳面の下端 (予告つき) 行 %.0f px' % (
                     o['ledgerHidesOwnFeet'], o['ledgerHidesNeighborFeet'], '' if not nb_at else ' (%s)' % nb_at, o['ledgerLowestRowPx']),
                 '- 触れた札: 行 %.0f〜%.0f px・「場に出す」線 行 %.0f px' % (o['raisedCardRowsPx'][0], o['raisedCardRowsPx'][1], o['dropLineRowPx']),
                 '- 要の数字の札の下端 (休んでいる手札 1〜10枚の最小・画面の下から px。門 36): %s → %s' % (' / '.join('%d枚 %.0f' % (i + 1, g) for i, g in enumerate(o['keynumBottomGapPx'])), '○' if o['keynumOk'] else '×'),
                 '- 主人公の絵の箱に掛かる常時の UI: %s' % ('・'.join(around) if around else 'なし'),
                 '- UI の面積の見込み (2体・手札5枚・重なりは引かない): 画面の %.1f%%' % o['uiAreaPct2Enemies'],
                 '- 合否: %s' % ('○' if o['ok'] else '×'), '',
                 '| 体数 | 足元 x | 帳面の幅 | 帳面の行 (px・上から。予告なし下端／予告つき下端) |', '|---|---|---|---|']
    for n in ('1', '2', '3', '4'):
        for i, L in enumerate(ledgers[n]):
            md_lines.append('| %s体の%d | %.0f | %.0f | %.0f〜%.0f／%.0f |' % (n, i + 1, L['x'], L['w'], L['rowsPx'][0], L['rowsPx'][1], L['rowsPx'][2]))
    md_lines.append('')
    return o


def r3_trial(md_lines):
    """三周目 r3 UI の割り付け (PC とスマホ)。名前は段1 の試し撮りの名残 (round3 の 'trial')"""
    tbl = read_stage_seats_22()
    md_lines += ['## 三周目 r3 UI (2026-10-02 本番・旗 uilayout=r3＝箱庭の PC の既定・スマホは uilayoutphone=r3 の時だけ (スマホの既定は二周目の 0.525)・仕様 docs/design/hd2d-slice/r3-ui-spec.md)', '',
                 'UI は BattleScreen の r3 の割り付け: 足元の線 PC 0.36・スマホ 0.45／手札を PC 168・スマホ 176 沈め、扇の下がり 4 (12 で頭打ち)・傾き 3° (±6° で頭打ち)／'
                 '敵の帳面を足元の下 (PC 10・スマホ 8) に 60 (予告つき 88。スマホ 53/79)／PC は足元の帳 (x 204〜500) と左下の匣・スマホは足元の帳 (x 24〜384)。'
                 'キャンバスの y は下から・「行」は上から。隠す量はキャンバス単位 (スマホの px は ×1.31)。', '']
    o = {'PC': r3_ui(md_lines, 'PC', tbl), 'PH': r3_ui(md_lines, 'PH', tbl)}
    o['ok'] = o['PC']['ok'] and o['PH']['ok']
    return o


CardView_W = 200.0


def round3(md_lines):
    """「三周目の試し撮り」の節と足元の x の表 (JSON の round3)"""
    trial = r3_trial(md_lines)
    feet = r3_feet_table(md_lines)
    return {'trial': trial, 'feet': feet}


def gl_candidates(plat):
    if PLATFORMS[plat]['phone']:
        return [round(0.40 + 0.02 * i, 2) for i in range(11)]
    return [round(0.32 + 0.01 * i, 2) for i in range(14)]


def check_w0(log_dir):
    """W0 の撮影のログの足元 (StatusLineY からの高さ) と今の式の一致"""
    import glob
    want = {
        'PC-S-wolf': ('PC', 1), 'PC-S-trio': ('PC', 3), 'PC-S-quad': ('PC', 4), 'PH-S-wolf': ('PH', 1), 'PH-S-quad': ('PH', 4),
    }
    out = []
    for name, (plat, n) in want.items():
        p = os.path.join(log_dir, name + '.log')
        if not os.path.exists(p):
            continue
        s = open(p, encoding='utf-8', errors='ignore').read()
        m = re.search(r'\[Autopilot\] layout .*', s)
        if not m:
            continue
        line = m.group(0)
        status = float(re.search(r'statusLine=([0-9.]+)', line).group(1))
        got = {k: float(v) for k, v in re.findall(r'(enemy\d+|player)=\([^)]*\)@\([^)]*\) feet=([0-9.\-]+)', line)}
        cam = Cam(plat, 36.0, 12.0, PLATFORMS[plat]['gl_now'])
        for i in range(n):
            key = 'enemy%d' % i
            if key in got:
                y = cam.seat(OLD_T[n][i], enemy_s(plat, n, i))[1] - status
                out.append((name, key, got[key], round(y, 3), round(y - got[key], 3)))
        if 'player' in got:
            y = cam.seat(*LEADER)[1] - status
            out.append((name, 'player', got['player'], round(y, 3), round(y - got['player'], 3)))
    return out


def main():
    ap = argparse.ArgumentParser(description='座席の表 (FOV ごとの敵と人形の t・足元の線の合否)')
    ap.add_argument('--md')
    ap.add_argument('--json')
    ap.add_argument('--layout', help='スマホ相当の layout.json (手札の本文の数字の下端を読む)')
    # 既定 32 = W2 の撮影の実測 (手札の下端 14 で数字の下端 60.4px → 60.4 ÷ 1.3126 − 14 = 32.0 単位)。P20 の手札 -3 で 38.1px (P20 の見込みと同じ)
    ap.add_argument('--digit-bottom', type=float, default=32.0, help='手札の本文の数字の下端 (札の下から・札の単位。layout.json が無い時の見積り。既定 32 = W2 の実測)')
    ap.add_argument('--check-w0', nargs='?', const='', help='W0 の撮影のログのフォルダ (式の写しの検査)')
    ap.add_argument('--r2', action='store_true', help='「二周目のカメラ」の節だけを出す (md・json も節だけ)')
    ap.add_argument('--r3', action='store_true', help='「三周目 r3 UI」(uilayout=r3・足元の線 PC 0.36／スマホ 0.45) と足元の x の表だけを出す (md・json も節だけ)')
    a = ap.parse_args()

    if a.r3:
        lines = ['# 座席の表 (hd2d-seatfit・三周目 r3 UI と足元の x の表だけ)', '']
        r3 = round3(lines)
        md = '\n'.join(lines) + '\n'
        if a.md:
            with open(a.md, 'w', encoding='utf-8') as f:
                f.write(md)
        if a.json:
            with open(a.json, 'w', encoding='utf-8') as f:
                json.dump({'schema': 'hd2d-seatfit/1', 'round3': r3}, f, ensure_ascii=False, indent=1)
        print(md)
        return 0 if r3['trial']['ok'] else 1

    if a.r2:
        lines = ['# 座席の表 (hd2d-seatfit・二周目のカメラだけ)', '']
        r2 = round2(lines)
        md = '\n'.join(lines) + '\n'
        if a.md:
            with open(a.md, 'w', encoding='utf-8') as f:
                f.write(md)
        if a.json:
            with open(a.json, 'w', encoding='utf-8') as f:
                json.dump({'schema': 'hd2d-seatfit/1', 'round2': r2}, f, ensure_ascii=False, indent=1)
        print(md)
        return 0 if all(e['hidden']['ok'] for e in r2) else 1

    ph = PLATFORMS['PH']
    digit_px = (ph['hand_y'] + a.digit_bottom * ph['card_scale']) * (ph['H'] / ph['ch'])
    digit_src = '見積り (札の下から %.0f 単位 = 下端から %.1fpx)' % (a.digit_bottom, digit_px)
    if a.layout:
        with open(a.layout, encoding='utf-8') as f:
            lay = json.load(f)
        gaps = [h['digitsBottomGap'] for h in lay.get('hand', []) if isinstance(h.get('digitsBottomGap'), (int, float))]
        if gaps:
            digit_px = min(gaps)
            digit_src = '%s の digitsBottomGap の最小 %.1fpx' % (os.path.basename(a.layout), digit_px)

    lines = ['# 座席の表 (hd2d-seatfit)', '', '式と UI の値は scripts/hd2d-seatfit.py の先頭。キャンバス単位 (PC 1920×1080・スマホ相当 1462.75×675)。y は画面の下から。', '']
    result = {'schema': 'hd2d-seatfit/1', 'fovs': [], 'digitBottom': {'px': round(digit_px, 1), 'from': digit_src}}
    if a.check_w0 is not None:
        d = a.check_w0 or '.'
        rows = check_w0(d)
        lines += ['## 式の写しの検査 (W0 の撮影のログの足元と比べる)', '', '| 撮影 | 座席 | ログ | 式 | 差 |', '|---|---|---|---|---|']
        for r in rows:
            lines.append('| %s | %s | %.2f | %.2f | %+.3f |' % r)
        lines.append('')
        result['checkW0'] = [dict(zip(('shot', 'seat', 'log', 'model', 'diff'), r)) for r in rows]
    lines += ['## 今の座席 (36°・12°・今の足元の線・今の t) の検査', '',
              '| 端末 | 体数 | t | 足元の x | 間隔の差 | いちばん右の絵の右端 | 帳面の右端 | 右端 −16 |', '|---|---|---|---|---|---|---|---|']
    for plat in ('PC', 'PH'):
        pl = PLATFORMS[plat]
        cam = Cam(plat, 36.0, 12.0, pl['gl_now'])
        for n in (2, 3, 4):
            xs = [cam.seat(OLD_T[n][i], enemy_s(plat, n, i))[0] for i in range(n)]
            gaps = [xs[i + 1] - xs[i] for i in range(n - 1)]
            spread = (max(gaps) - min(gaps)) / (sum(gaps) / len(gaps))
            sw = strip_w(plat, min(gaps), False)
            lines.append('| %s | %d | %s | %s | %.1f%% | %.0f | %.0f | %.0f |' % (plat, n, ','.join('%.2f' % v for v in OLD_T[n]), ','.join('%.0f' % v for v in xs),
                                                                            spread * 100, xs[-1] + pl['art_w'] / 2, xs[-1] + sw / 2, pl['cw'] - 16))
    lines += ['', '(絵の矩形は透明な余白を含む。今の PC の4体は絵の右端が右端 −16 を 10 はみ出す。下の表は見本の読み = 絵の矩形も右端 −16 に収める)', '']
    for plat in ('PC', 'PH'):
        pl = PLATFORMS[plat]
        lines += ['## %s (%s)' % ('PC' if plat == 'PC' else 'スマホ相当', '1920×1080' if plat == 'PC' else '1920×886・UI 1.6倍'), '']
        for fov, pitch in FOVS:
            ent = {'platform': plat, 'fov': fov, 'pitch': pitch, 'groundLines': []}
            cam_ref = Cam(plat, fov, pitch, pl['gl_now'])
            lines += ['### 画角 %.0f°・見下ろし %.0f° (距離 %.2f unit)' % (fov, pitch, cam_ref.dist), '']
            head = '| 足元の線 | 敵2〜4体の t | 間隔の差 | 右端 | 主人公の足元 (上から) | 敵の足元 (上から) | '
            head += ('帳面が隠す | 札が脚を隠す (主人公／人形の1体目) | 自分の欄の余白 (置物込み) | 合否 |' if plat == 'PC' else '沈めない時に帳面が隠す (予告つき) | 沈める量 / 上限 | 自分の欄の余白 | 合否 |')
            lines += [head, '|' + '---|' * (head.count('|') - 1)]
            best = None
            best_nosink = None
            for gl in gl_candidates(plat):
                rows = evaluate(plat, fov, pitch, gl, digit_px)
                ok_all = all(r['ok'] for r in rows)
                if ok_all and all(r.get('sinkNeeded', 0.0) <= 0.01 for r in rows) and (best_nosink is None or gl < best_nosink):
                    best_nosink = gl
                multi = [r for r in rows if r['n'] >= 2]
                ts = ' / '.join(','.join('%.2f' % v for v in r['t']) for r in multi)
                spread = max(r['gapSpread'] for r in multi)
                right = max(r['rightEdge'] for r in rows)
                lead_y = rows[0]['leaderFeetY']
                enemy_ys = [y for r in rows for y in r['feetY']]
                top_pct = lambda y: 100.0 * (1.0 - y / pl['ch'])
                if plat == 'PC':
                    tail = '%.0f | %.0f／%.0f | %.0f (%.0f) |' % (max(r['feetHidden'] for r in rows), max(r['leaderHidden'] for r in rows), max(r['doll0Hidden'] for r in rows),
                                                                min(r['selfMargin'] for r in multi), min(r['selfMarginPerm'] for r in multi))
                else:
                    sink = max(r['sinkNeeded'] for r in rows)
                    smax = rows[0].get('sinkMax')
                    tail = '%.0f (%.0f) | %.0f / %s | %.0f |' % (max(r['feetHidden'] for r in rows), max(r['feetHiddenForecast'] for r in rows),
                                                                sink, '-' if smax is None else '%.0f' % smax, min(r['selfMargin'] for r in multi))
                lines.append('| %.2f | %s | %.1f%% | %.0f | %.1f%% | %.1f〜%.1f%% | %s %s |' % (
                    gl, ts, spread * 100, right, top_pct(lead_y), top_pct(max(enemy_ys)), top_pct(min(enemy_ys)), tail, '○' if ok_all else '×'))
                ent['groundLines'].append({'gl': gl, 'ok': ok_all, 'leaderFeetTopPct': round(top_pct(lead_y), 1), 'enemies': rows})
                if ok_all and (best is None or gl < best):
                    best = gl
            ent['lowestOk'] = best
            ent['lowestOkNoSink'] = best_nosink
            # 今の UI (P20) のままで通るいちばん低い線 (スマホは手札をこれ以上沈めない)。通らなければ今の線 (2026-09-30 W2 の表では「P20 の前の UI」の値だった)
            use = best_nosink if best_nosink is not None else pl['gl_now']
            dl = doll_layout(plat, fov, pitch, use)
            ent['dolls'] = dl
            ent['defaultGroundLine'] = use
            ent['enemyT'] = {str(r['n']): r['t'] for r in evaluate(plat, fov, pitch, use, digit_px)}
            fmt_gl = lambda v: 'なし (候補の中に通る線が無い)' if v is None else '%.2f' % v
            lines += ['', '- 今の UI (P20) のまま・手札をこれ以上沈めずに通るいちばん低い線 (0.01／0.02 刻みの候補): **%s**%s。その時の敵の t: %s' % (
                          fmt_gl(best_nosink), '' if not pl['phone'] else '。手札を上限まで沈めれば %s まで' % fmt_gl(best),
                          ' / '.join('%s体 %s' % (k, ','.join('%.2f' % v for v in t)) for k, t in ent['enemyT'].items())),
                      '- 人形の t: %s (刻み %.3f・画面の間隔は今の %.2f 倍以上・敵4体の先頭まで %.0f (今 %.0f)%s)' % (
                          ','.join('%.2f' % v for v in dl['t']), dl['step'], dl['minGapRatio'], dl['toEnemy1'], dl['toEnemy1Old'], '' if dl['enemyOk'] else ' ⚠ 今の 0.9 倍未満'),
                      '- 奥と手前の深さの比 (敵4体のいちばん奥 ÷ 主人公): %.3f' % evaluate(plat, fov, pitch, use, digit_px)[3]['depthRatio'], '']
            result['fovs'].append(ent)
        if plat == 'PH':
            lines.append('手札の本文の数字の下端: %s。沈める量の上限 = (下端 − 36px) ÷ キャンバスの倍率 (P20 で 17 沈めた後の残り)。' % digit_src)
            lines.append('')
    result['round2'] = round2(lines)
    result['round3'] = round3(lines)
    lines += ['## 読み方', '',
              '- 敵の t は、先頭 (人形の列の約束で今の値) と、いちばん右の足元の画面の x (今の36°の位置。右端 −16 に収まらなければ内側へ) を固定し、間を画面で等間隔に解いた値。',
              '- PC の「帳面が隠す」= 敵の帳面の上端 (285+140) が足元より上にある量。「札が脚を隠す」= 自分の札の上端が主人公 (と人形の1体目) の足元より上にある量。どれも 16 未満で合格 (layout-check の L2 と同じ)。',
              '- PC の自分の札は HandLayer (x≥260) の左に収まらない (最小でも 32+300+180=512) ので、「脚を隠さない高さ」で判定する。',
              '- スマホの「沈める量」= 帳面と手札の線を下げないと足元が隠れる量。上限を超えると本文の数字が下端から 36px を割る。',
              '- スマホの画角ごとの表の合否は予告なしの帳面 (高さ 53) で見る。幕ボスの帳面 (予告つき 79) がボスの足元を隠す量は「二周目のカメラ」の節で見る (P20 と二周目の足元の線はこちらで決めた)。',
              '- 自分の欄の余白 = いちばん左の敵の帳面 (と意図の札) の左端 −8 と、自分の欄の最小の右端の差 (負 = 入り込む)。',
              '- 合否 ○ = 1〜4体の全部で、間隔の差 5% 以下・右端・足元・自分の欄が通る (スマホは手札を上限まで沈めてよい読み)。',
              '- UI は P20 の値 (2026-10-01 に直した。2026-09-30 の W2 の表は P20 の前の UI = PC 300/手札30・スマホ 310/手札14 で、W2 の既定 PC 0.42・スマホ 0.52 はその表から)。',
              '- JSON (--json) の fovs[].defaultGroundLine・enemyT・dolls.t が StageSeats の表の元。groundLines[] に全候補の中身。',
              '- 「二周目のカメラ」の節 (JSON の round2) が二周目の既定 (PC 22°・5°・0.407／スマホ 22°・7°・0.525) の根拠: 隠す量 (px)・地平線の行・画面の端の縦の傾き・StageSeats の 22° の行との差・人形の後列の表。']
    md = '\n'.join(lines) + '\n'
    if a.md:
        with open(a.md, 'w', encoding='utf-8') as f:
            f.write(md)
    if a.json:
        with open(a.json, 'w', encoding='utf-8') as f:
            json.dump(result, f, ensure_ascii=False, indent=1)
    print(md)
    return 0


if __name__ == '__main__':
    sys.exit(main())

#!/usr/bin/env python3
"""座席の表 (2026-09-30 P08。計画 docs/design/hd2d-slice-plan-2026-09-30.md §2-3・§4 P08 手順4・P10 手順3と7)。

Stage.ProjectFeet と LayoutCamera の式を写し、画角 (FOV 36/28/22。22 は見下ろし 10° と 12°) ごとに
  1. 敵の座席の t (道に沿った距離) の表: 2〜4体で画面の足元の間隔がそろい (差 ±5%)、いちばん右の敵の絵と帳面が画面の右端 −16 の内側
  2. 人形の座席の t: 画面の間隔が今 (36°) 以上
  3. 足元の線 (GroundLine = 世界の原点を画面の下から何割に置くか。Stage.GroundLineRatio と HD2DFlags.GroundLine と同じ読み) の候補ごとの合否
を出す。W2 の既定 (P10) と最終の線 (P20) の根拠。ゲームは起動しない (式だけ)。

使い方
  scripts/hd2d-seatfit.py [--md 表.md] [--json 表.json] [--layout <PH の .layout.json>] [--digit-bottom 40] [--check-w0]
  --check-w0 = 今の式 (36°・12°・PC 0.45・スマホ 0.56・今の t) で W0 の撮影のログの足元と一致するかを確かめる (式の写し間違いの検査)
  --layout = スマホ相当で撮った layout.json から手札の本文の数字の下端 (digitsBottomGap の最小) を読み、手札を沈められる量に使う

写した式 (unity/Assets/Game/StageCamera.cs・StageSeats.cs・Stage.cs・BattleView.cs・BattleScreen.cs。2026-09-30 の値)
  PlaneUnitsPerScreen (基準深度の画面の高さ) = PC 10.8・スマホ 10.8 / clamp(アスペクト/1.8, 1, 1.2)
  _k = PUPS / 画面の高さ(px)・_dist = PUPS/2 / tan(FOV/2) (基準深度で 1 unit = 画面の高さ/PUPS px = 1080p で 100px)
  カメラ: 回転 Euler(pitch,0,0)・原点が画面の下から GroundLine の高さに来る位置から _dist 引く
  座席: OnPath(t, s) = Euler(0,-22°,0)·(t, 0, s)・高さ 0 (幕1 の戦闘の場は平らに均してある。見本の座席の帯も高さ 0)
  リーダー t=-5 s=0.9。敵 s = 偶数番 -0.5・奇数番 0.1 (スマホか3体以上) / 0.7 (PC の2体)。人形 = StageSeats.DollSlots
  UI (キャンバス単位・下から): PC 足元の線 StatusLineY 300・敵の帳面の上端 300+140・自分の札 x 32〜(32+300+からくり 2枠 180)・上端 300+140・手札の上端 30+290×0.92
           スマホ StatusLineY 14+290+6=310・帳面 下端 310 高さ 53 (予告つき 79)・自分の札 x 24〜248 高さ 110・手札の上端 14+290
  帳面の幅 = 隣との間隔から (BattleScreen.StripW)。1体は PC 440・スマホ 420
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
FOVS = [(36.0, 12.0), (28.0, 12.0), (22.0, 12.0), (22.0, 10.0)]

PLATFORMS = {
    'PC': dict(W=1920, H=1080, cw=1920.0, ch=1080.0, phone=False, gl_now=0.45, art_w=256.0, boss_w=512.0,
               status=300.0, strip_top_over=140.0, self_right_min=32.0 + 300.0 + 2 * (68.0 + 10.0) + 24.0, perm_sec=236.0,
               self_top_over=140.0, hand_y=30.0, card_scale=0.92, edge=32.0),
    'PH': dict(W=1920, H=886, cw=1462.75, ch=675.0, phone=True, gl_now=0.56, art_w=153.6, boss_w=184.0 * 64 / 58.0,
               status=310.0, strip_h=53.0, strip_h_fc=79.0, self_card=(24.0, 248.0), self_card_h=110.0,
               band_left=24.0, set_w=2 * (68.0 + 10.0), hand_y=14.0, card_scale=1.0, edge=24.0, intent_half=112.0),
}
CARD_H = 290.0


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
            r['feetHidden'] = round(max(0.0, ledger_top - min(feet_enemy)), 1)
            r['leaderHidden'] = round(max(0.0, self_top - lead[1]), 1)
            r['feetOk'] = r['feetHidden'] < 16.0
            r['leaderOk'] = r['leaderHidden'] < 16.0
            zone_right = xs[0] - sw / 2.0 - 8.0
            r['selfMargin'] = round(zone_right - p['self_right_min'], 1)
            r['selfMarginPerm'] = round(zone_right - p['self_right_min'] - p['perm_sec'], 1)
            r['ok'] = r['rightOk'] and spread <= 0.05 + 1e-9 and r['feetOk'] and r['leaderOk'] and r['selfMargin'] >= 0
        rows.append(r)
    return rows


def doll_layout(plat, fov, pitch, gl):
    """人形の t: 1体目は今のまま、刻みを伸ばして画面の間隔を今 (36°) 以上に。敵4体の先頭との間も今の間隔以上"""
    phone = PLATFORMS[plat]['phone']

    def slots(step, cam):
        out = []
        for i in range(9):
            j = i % 5
            back = i >= 5
            near_first = i == 0 and not phone
            s = (0.25 if near_first else (0.9 if j % 2 == 0 else 0.25)) + (1.15 if back else 0.0)
            t = DOLL_T[0] + j * step + (0.15 if back else 0.0)
            out.append((t, s, cam.seat(t, s)))
        return out
    old_cam = Cam(plat, 36.0, 12.0, PLATFORMS[plat]['gl_now'])
    old = slots(0.85, old_cam)
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
    ap.add_argument('--digit-bottom', type=float, default=40.0, help='手札の本文の数字の下端 (札の下から・札の単位。layout.json が無い時の見積り。既定 40)')
    ap.add_argument('--check-w0', nargs='?', const='', help='W0 の撮影のログのフォルダ (式の写しの検査)')
    a = ap.parse_args()

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
            head += ('帳面が隠す | 札が脚を隠す | 自分の欄の余白 (置物込み) | 合否 |' if plat == 'PC' else '沈めない時に帳面が隠す (予告つき) | 沈める量 / 上限 | 自分の欄の余白 | 合否 |')
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
                    tail = '%.0f | %.0f | %.0f (%.0f) |' % (max(r['feetHidden'] for r in rows), max(r['leaderHidden'] for r in rows),
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
            # W2 の既定 (P10): 今の UI のままで通るいちばん低い線 (スマホは手札を沈めない = P20 の前)。通らなければ今の線
            use = best_nosink if best_nosink is not None else pl['gl_now']
            dl = doll_layout(plat, fov, pitch, use)
            ent['dolls'] = dl
            ent['defaultGroundLine'] = use
            ent['enemyT'] = {str(r['n']): r['t'] for r in evaluate(plat, fov, pitch, use, digit_px)}
            fmt_gl = lambda v: 'なし (候補の中に通る線が無い)' if v is None else '%.2f' % v
            lines += ['', '- W2 の既定 (今の UI のまま・手札を沈めずに通るいちばん低い線): **%s**%s。その時の敵の t: %s' % (
                          fmt_gl(best_nosink), '' if not pl['phone'] else '。手札を沈めれば (P20) %s まで' % fmt_gl(best),
                          ' / '.join('%s体 %s' % (k, ','.join('%.2f' % v for v in t)) for k, t in ent['enemyT'].items())),
                      '- 人形の t: %s (刻み %.3f・画面の間隔は今の %.2f 倍以上・敵4体の先頭まで %.0f (今 %.0f)%s)' % (
                          ','.join('%.2f' % v for v in dl['t']), dl['step'], dl['minGapRatio'], dl['toEnemy1'], dl['toEnemy1Old'], '' if dl['enemyOk'] else ' ⚠ 今の 0.9 倍未満'),
                      '- 奥と手前の深さの比 (敵4体のいちばん奥 ÷ 主人公): %.3f' % evaluate(plat, fov, pitch, use, digit_px)[3]['depthRatio'], '']
            result['fovs'].append(ent)
        if plat == 'PH':
            lines.append('手札の本文の数字の下端: %s。沈める量の上限 = (下端 − 36px) ÷ キャンバスの倍率。' % digit_src)
            lines.append('')
    lines += ['## 読み方', '',
              '- 敵の t は、先頭 (人形の列の約束で今の値) と、いちばん右の足元の画面の x (今の36°の位置。右端 −16 に収まらなければ内側へ) を固定し、間を画面で等間隔に解いた値。',
              '- PC の「帳面が隠す」= 敵の帳面の上端 (300+140) が足元より上にある量。「札が脚を隠す」= 自分の札の上端が主人公の足元より上にある量。どちらも 16 未満で合格 (layout-check の L2 と同じ)。',
              '- PC の自分の札は HandLayer (x≥260) の左に収まらない (最小でも 32+300+180=512) ので、「脚を隠さない高さ」で判定する。',
              '- スマホの「沈める量」= 帳面と手札の線を下げないと足元が隠れる量。上限を超えると本文の数字が下端から 36px を割る。',
              '- 自分の欄の余白 = いちばん左の敵の帳面 (と意図の札) の左端 −8 と、自分の欄の最小の右端の差 (負 = 入り込む)。',
              '- 合否 ○ = 1〜4体の全部で、間隔の差 5% 以下・右端・足元・自分の欄が通る (スマホは手札を上限まで沈めてよい読み)。',
              '- W2 の既定 (P10) は「手札を沈めずに通る線」、最終の線 (P20) は手札と札の置き場を動かしてから決める (計画 §2-3)。',
              '- JSON (--json) の fovs[].defaultGroundLine・enemyT・dolls.t が P10 の読む値。groundLines[] に全候補の中身。']
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

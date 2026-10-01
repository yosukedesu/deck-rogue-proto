#!/usr/bin/env python3
"""HD-2D 見本の配置の検査 (2026-09-30 P08。計画 docs/design/hd2d-slice-plan-2026-09-30.md §1-2 の3・§4 P08)。

dumplayout=1 で撮った <名前>.layout.json (P01 の hd2d-layout/1) を読み、札とキャラの重なりと距離を検査する。
矩形は全部 PNG の画素 [x, y, w, h] (左上が原点・y は下向き)。違反が1つでもあれば終了コード 1。

使い方
  scripts/hd2d-layout-check.py <フォルダか .layout.json …> [--md 表.md] [--json 結果.json] [--draw <絵の置き場>] [--moving]
  --draw を付けると、PNG に矩形と違反を描いた確認の絵を書く (PNG が隣にある時)。
  --moving = 演出の途中のコマとして読む (板と矩形のずれの許容を 2px に)。STATE に play=・endplay=・fire=・entershots= があれば自動で 2px。

規則 (違反 = 数える。注意 = 表に出すだけ)
  L1 帳面と手札: 敵の帳面・自分の札の下端と、横に重なる手札の札の上端 (傾きの分を除いた札の中央の上端) の間が 6 キャンバス単位以上
     (PC 6px・スマホ 7.9px。スマホは設計の間隔が 6 = BattleView.StatusLineY のため。計画の「8px」はこの読み)。--hand-gap で変えられる。
  L2 足元: キャラ (敵・リーダー・人形) の足元の点を、ほかの札 (帳面・自分の札・意図の札・上部バー・手札) が 16px 以上隠さない
     (足元より上に札の上端が 16px 以上入り込んでいる = 違反)。人形の足元の札は自分の人形の足元に付く物なので数えない。
  L3 意図の札: 上部バーに掛からない。ほかの敵の意図の札・帳面と重ならない。
  L4 画面の外: 帳面・意図の札・自分の札・上部バー・人形の札・本文の数字が画面からはみ出さない (1px の許容)。
     敵の帳面と意図の札は右端から 16px 以上内側 (seatfit の右端の条件と同じ)。キャラの絵が画面からはみ出すのは「注意」。
  L5 本文の数字: 手札の本文の数字の下端が、画面の下端から 36px 以上上にある (digitsBottomGap ≧ 36)。
  L6 人形の札: 人形の札どうしが重ならない。人形の札が敵の帳面・自分の札と重ならない。
  L7 帳面どうし: 敵の帳面どうしが重ならない。いちばん左の敵の帳面が自分の札 (hpwrap・からくり・ギア・置物の欄) に入らない。
  L8 額縁: extra.frames (額縁の画面の矩形。P10・P12 が HD2DFlags.LayoutDumpers["frames"] で足す = [{name, px}]) が
     UI の矩形と重なる割合が 30% 以下。記録が無ければ検査しない (注意に1行)。
  L9 板のずれ: stage.unitBoxes (P11 の Stage.DebugUnitBoxes) の各キャラの板と矩形のずれが、静止 1px・演出中 2px 以下。
     ずれは記録の dev (px) を使い、無ければ rectPx と boardPx の4辺の差の最大。記録が無ければ検査しない。
  L10 からくりの匣 (2026-10-01 二周目の直しの輪1): extra.karakuriBox (Stage.R2A_DumpKarakuriBox = 匣の板の画面の矩形) が、
     主人公の足元 (足元の x ±0.15×絵の幅・足元の 24px 上〜8px 下) と自分の札 (hpwrap・からくり・ギア・置物の欄) に掛からない。記録が無ければ検査しない。
     演出中のコマ (--moving・STATE に play= など) の足元は注意 (のけぞった主人公が奥の匣の手前を横切るだけ)。
  L8 の額縁から、深さ 50 以上の板 (カメラに付く背景の板 backdrop = 深さ 140・全幅) は外す (手前の額縁ではない。直しの輪1)。
"""
import argparse
import glob
import json
import math
import os
import re
import sys

LAY_EXT = '.layout.json'


def load(p):
    with open(p, encoding='utf-8') as f:
        return json.load(f)


def area(r):
    return max(0.0, r[2]) * max(0.0, r[3])


def inter(a, b):
    x0, y0 = max(a[0], b[0]), max(a[1], b[1])
    x1, y1 = min(a[0] + a[2], b[0] + b[2]), min(a[1] + a[3], b[1] + b[3])
    if x1 <= x0 or y1 <= y0:
        return None
    return [x0, y0, x1 - x0, y1 - y0]


def xover(a, b):
    return max(0.0, min(a[0] + a[2], b[0] + b[2]) - max(a[0], b[0]))


def units(lay):
    return [u for u in lay.get('units', []) if isinstance(u, dict)]


def nodes(lay, pattern):
    rx = re.compile(pattern)
    return [n for n in lay.get('nodes', []) if isinstance(n, dict) and rx.search(n.get('path', '')) and n.get('px')]


def feet_of(lay, u):
    sp = u.get('sprite')
    if not sp:
        return None
    x = sp[0] + sp[2] / 2.0
    y = sp[1] + sp[3]
    sl = lay.get('statusLineY') or {}
    fo = u.get('feetOffset')
    sc = (lay.get('canvas') or {}).get('scale') or 1.0
    if isinstance(fo, (int, float)) and not (isinstance(fo, float) and math.isnan(fo)) and isinstance(sl.get('px'), (int, float)):
        y = sl['px'] - fo * sc
    return (x, y)


def self_rects(lay):
    """自分の札の欄 (PC は帳面の左端・スマホは左下と上の帯)"""
    return [(n['path'].split('/')[-1], n['px']) for n in nodes(lay, r'/player/(hpwrap|setzone|gearzone|perms)$')]


def doll_tags(lay):
    out = []
    for n in nodes(lay, r'/(doll:[^/]+)/tag$'):
        m = re.search(r'/(doll:[^/]+)/tag$', n['path'])
        out.append((m.group(1), n['px']))
    return out


def moving_state(state):
    return bool(re.search(r'(^|;)(play|endplay|fire|entershots|usegear|fx)=', state or ''))


def check(lay, hand_gap_units=6.0, feet_hide=16.0, digits_min=36.0, right_margin=16.0, frame_max=0.30, box_tol=None):
    V, W_ = [], []   # 違反・注意 (dict: rule, msg, rects)
    sw = (lay.get('screen') or {}).get('w') or 1920
    sh = (lay.get('screen') or {}).get('h') or 1080
    sc = (lay.get('canvas') or {}).get('scale') or 1.0
    us = [u for u in units(lay) if u.get('alive', True) and u.get('active', True) is not False]
    enemies = [u for u in us if u.get('kind') == 'enemy']
    strips = [(u['key'], u['strip']) for u in enemies if u.get('strip')]
    intents = [(u['key'], u['intent']) for u in enemies if u.get('intent')]
    selfr = self_rects(lay)
    dtags = doll_tags(lay)
    hand = [h for h in lay.get('hand', []) if isinstance(h, dict) and h.get('px')]
    topbar = (lay.get('anchors') or {}).get('topbar')

    def v(rule, msg, *rects):
        V.append({'rule': rule, 'msg': msg, 'rects': [list(map(float, r)) for r in rects if r]})

    def w(rule, msg, *rects):
        W_.append({'rule': rule, 'msg': msg, 'rects': [list(map(float, r)) for r in rects if r]})

    # L1 帳面と手札
    gap_px = hand_gap_units * sc
    card_h = 290.0 * sc * (0.92 if not (lay.get('canvas') or {}).get('phone') else 1.0)
    for key, s in strips + [('player/' + n, r) for n, r in selfr if n == 'hpwrap']:
        bottom = s[1] + s[3]
        for h in hand:
            r = h['px']
            if xover(s, r) < 12:
                continue
            eff_top = r[1] + max(0.0, r[3] - card_h) / 2.0   # 傾いた札の外接の箱の膨らみの半分を除く = 札の中央の上端
            gap = eff_top - bottom
            if gap < gap_px - 0.5:
                v('L1', '%s の下端と手札 %s の上端の間 %.1fpx (< %.1fpx)' % (key, h.get('name'), gap, gap_px), s, r)
    # L2 足元
    covers = [('strip:' + k, r) for k, r in strips] + [('intent:' + k, r) for k, r in intents] + \
             [('self:' + n, r) for n, r in selfr] + [('hand:' + (h.get('name') or ''), h['px']) for h in hand]
    if topbar:
        covers.append(('topbar', topbar))
    for u in us:
        f = feet_of(lay, u)
        if not f:
            continue
        fx, fy = f
        half = max(8.0, 0.25 * u['sprite'][2])
        probe = [fx - half, fy - 1, 2 * half, 2]
        for name, r in covers:
            if name.endswith(':' + u.get('key', '')) and name.startswith('intent:'):
                continue
            if xover(probe, r) < 4:
                continue
            if r[1] < fy <= r[1] + r[3] + 0.5:
                hidden = fy - r[1]
                if hidden >= feet_hide:
                    v('L2', '%s の足元 (%.0f,%.0f) を %s が %.0fpx 隠す' % (u.get('key'), fx, fy, name, hidden), r, [fx - 3, fy - 3, 6, 6])
    # L3 意図の札
    for k, r in intents:
        if topbar and inter(r, topbar) and area(inter(r, topbar)) > 4:
            v('L3', '%s の意図の札が上部バーに掛かる' % k, r, topbar)
        for k2, r2 in intents:
            if k2 <= k:
                continue
            i = inter(r, r2)
            if i and area(i) > 4:
                v('L3', '%s と %s の意図の札が重なる (%.0fpx²)' % (k, k2, area(i)), r, r2)
        for k2, s2 in strips:
            if k2 == k:
                continue
            i = inter(r, s2)
            if i and area(i) > 4:
                v('L3', '%s の意図の札が %s の帳面と重なる' % (k, k2), r, s2)
    # L4 画面の外
    tol = 1.0
    boxes = [('strip:' + k, r, right_margin) for k, r in strips] + [('intent:' + k, r, right_margin) for k, r in intents] + \
            [('self:' + n, r, 0.0) for n, r in selfr] + [('dolltag:' + k, r, 0.0) for k, r in dtags]
    if topbar:
        boxes.append(('topbar', topbar, 0.0))
    for name, r, rm in boxes:
        if r[0] < -tol or r[1] < -tol or r[0] + r[2] > sw - rm + tol or r[1] + r[3] > sh + tol:
            v('L4', '%s が画面の外へ出る (%s・右の余白 %dpx)' % (name, [round(x) for x in r], rm), r)
    for u in us:
        sp = u.get('sprite')
        if sp and (sp[0] < -tol or sp[0] + sp[2] > sw + tol or sp[1] < -tol):
            w('L4', '%s の絵の矩形が画面からはみ出す (%s)' % (u.get('key'), [round(x) for x in sp]), sp)
    for h in hand:
        for d in h.get('digits') or []:
            if d[0] < -tol or d[0] + d[2] > sw + tol or d[1] + d[3] > sh + tol:
                v('L4', '手札 %s の本文の数字が画面の外' % h.get('name'), d)
    # L5 本文の数字
    for h in hand:
        g = h.get('digitsBottomGap')
        if isinstance(g, (int, float)) and g < digits_min:
            v('L5', '手札 %s の本文の数字が下端から %.0fpx (< %.0f)' % (h.get('name'), g, digits_min), *(h.get('digits') or [h['px']]))
    # L6 人形の札
    for i, (k, r) in enumerate(dtags):
        for k2, r2 in dtags[i + 1:]:
            it = inter(r, r2)
            if it and area(it) > 4:
                v('L6', '人形の札 %s と %s が重なる' % (k, k2), r, r2)
        for name, r2 in [('strip:' + a, b) for a, b in strips] + [('self:' + a, b) for a, b in selfr]:
            it = inter(r, r2)
            if it and area(it) > 4:
                v('L6', '人形の札 %s が %s と重なる' % (k, name), r, r2)
    # L7 帳面どうし・自分の札
    for i, (k, r) in enumerate(strips):
        for k2, r2 in strips[i + 1:]:
            it = inter(r, r2)
            if it and area(it) > 4:
                v('L7', '%s と %s の帳面が重なる' % (k, k2), r, r2)
    if strips:
        k0, r0 = min(strips, key=lambda kr: kr[1][0])
        for n, r in selfr:
            it = inter(r0, r)
            if it and area(it) > 4:
                v('L7', 'いちばん左の敵 %s の帳面が自分の札 (%s) に入る' % (k0, n), r0, r)
    # L8 額縁
    frames = (lay.get('extra') or {}).get('frames')
    if isinstance(frames, list) and frames:
        ui_rects = [r for _, r in strips] + [r for _, r in intents] + [r for _, r in selfr] + [r for _, r in dtags] + [h['px'] for h in hand]
        if topbar:
            ui_rects.append(topbar)
        for fr in frames:
            r = fr.get('px') if isinstance(fr, dict) else fr
            if not r or area(r) <= 0:
                continue
            if isinstance(fr, dict) and isinstance(fr.get('depth'), (int, float)) and fr['depth'] >= 50:
                continue   # 背景の板 (深さ 140) は額縁ではない (直しの輪1)
            cov = union_area_within(r, ui_rects) / area(r)
            if cov > frame_max:
                v('L8', '額縁 %s が UI と %.0f%% 重なる (> %.0f%%)' % (fr.get('name') if isinstance(fr, dict) else '?', cov * 100, frame_max * 100), r)
    else:
        w('L8', '額縁の記録 (extra.frames) が無いので検査しない')
    # L9 板のずれ
    ub = (lay.get('stage') or {}).get('unitBoxes')
    tolb = box_tol if box_tol is not None else (2.0 if moving_state(lay.get('state')) else 1.0)
    entries = []
    if isinstance(ub, list):
        entries = [e for e in ub if isinstance(e, dict)]
    elif isinstance(ub, dict):
        entries = [dict(e, key=k) for k, e in ub.items() if isinstance(e, dict)]
    if entries:
        for e in entries:
            dev = e.get('dev')
            if dev is None:
                a, b = e.get('rectPx') or e.get('rect'), e.get('boardPx') or e.get('board')
                if a and b and len(a) >= 4 and len(b) >= 4:
                    dev = max(abs(a[0] - b[0]), abs(a[1] - b[1]), abs(a[0] + a[2] - b[0] - b[2]), abs(a[1] + a[3] - b[1] - b[3]))
            if isinstance(dev, (int, float)) and dev > tolb:
                v('L9', '%s の板と矩形のずれ %.1fpx (> %.0fpx)' % (e.get('key'), dev, tolb), e.get('boardPx') or e.get('board') or [0, 0, 0, 0])
    elif ub is None:
        w('L9', '板の記録 (stage.unitBoxes) が無いので検査しない')
    # L10 からくりの匣 (直しの輪1)
    kb = (lay.get('extra') or {}).get('karakuriBox')
    if isinstance(kb, dict) and kb.get('px'):
        rb = kb['px']
        for u in us:
            if u.get('kind') != 'player' or not u.get('sprite'):
                continue
            f = feet_of(lay, u)
            if not f:
                continue
            fx, fy = f
            half = max(8.0, 0.15 * u['sprite'][2])   # 足の幅 (絵の矩形は斧・竿を含むので 0.25 では広すぎる)
            probe = [fx - half, fy - 24, 2 * half, 32]
            it = inter(rb, probe)
            if it and area(it) > 4:
                # 演出中 (被弾でのけぞる・盾で下がる) は主人公が匣の手前を横切る = 奥の匣を隠すだけなので注意に留める
                (w if moving_state(lay.get('state')) else v)('L10', 'からくりの匣 (%s) が主人公の足元 (%.0f,%.0f) に掛かる (%.0fpx²)' % (kb.get('place'), fx, fy, area(it)), rb, probe)
        for n, r2 in selfr:
            it = inter(rb, r2)
            if it and area(it) > 4:
                v('L10', 'からくりの匣 (%s) が自分の札 (%s) に掛かる (%.0fpx²)' % (kb.get('place'), n, area(it)), rb, r2)
    return V, W_


def union_area_within(r, others, step=4):
    """r の中で others のどれかに覆われる面積 (格子で数える)"""
    x0, y0, w, h = r
    n = 0
    tot = 0
    y = y0 + step / 2
    while y < y0 + h:
        x = x0 + step / 2
        while x < x0 + w:
            tot += 1
            for o in others:
                if o[0] <= x < o[0] + o[2] and o[1] <= y < o[1] + o[3]:
                    n += 1
                    break
            x += step
        y += step
    return area(r) * (n / tot) if tot else 0.0


def draw(png, lay, V, out):
    from PIL import Image, ImageDraw
    im = Image.open(png).convert('RGB')
    d = ImageDraw.Draw(im)
    for u in units(lay):
        for k, col in (('sprite', (80, 200, 255)), ('strip', (255, 220, 80)), ('intent', (255, 140, 60))):
            r = u.get(k)
            if r:
                d.rectangle([r[0], r[1], r[0] + r[2], r[1] + r[3]], outline=col, width=1)
        f = feet_of(lay, u)
        if f:
            d.ellipse([f[0] - 4, f[1] - 4, f[0] + 4, f[1] + 4], outline=(0, 255, 120), width=2)
    for h in lay.get('hand', []):
        r = h.get('px')
        if r:
            d.rectangle([r[0], r[1], r[0] + r[2], r[1] + r[3]], outline=(180, 180, 180), width=1)
    for e in V:
        for r in e['rects']:
            d.rectangle([r[0], r[1], r[0] + r[2], r[1] + r[3]], outline=(255, 0, 0), width=3)
    im.save(out)


def gather(paths):
    out = []
    for p in paths:
        if os.path.isdir(p):
            out += sorted(glob.glob(os.path.join(p, '*' + LAY_EXT)))
        elif p.endswith(LAY_EXT):
            out.append(p)
    return out


def main():
    ap = argparse.ArgumentParser(description='HD-2D 見本の配置の検査 (layout.json)')
    ap.add_argument('paths', nargs='+')
    ap.add_argument('--md')
    ap.add_argument('--json')
    ap.add_argument('--draw', help='確認の絵の置き場')
    ap.add_argument('--moving', action='store_true', help='板のずれの許容を 2px に')
    ap.add_argument('--hand-gap', type=float, default=6.0, help='L1 の間隔 (キャンバス単位。既定 6)')
    ap.add_argument('--all-variants', action='store_true', help='uionly・unitsonly・hideui の撮影の記録も読む (既定は通常の画の記録だけ)')
    a = ap.parse_args()
    files = gather(a.paths)
    if not a.all_variants:
        files = [f for f in files if not re.search(r'-(uionly|unitsonly|hideui)-\d+\.layout\.json$', f)]
    if not files:
        print('layout.json が無い (dumplayout=1 で撮る)', file=sys.stderr)
        return 2
    res = {}
    total = 0
    for f in files:
        try:
            lay = load(f)
        except Exception as ex:
            res[os.path.basename(f)] = {'error': str(ex)}
            total += 1
            continue
        V, Wn = check(lay, hand_gap_units=a.hand_gap, box_tol=2.0 if a.moving else None)
        res[os.path.basename(f)] = {'violations': V, 'warnings': Wn, 'errors': lay.get('errors') or []}
        total += len(V)
        if a.draw:
            png = f[:-len(LAY_EXT)] + '.png'
            if os.path.exists(png):
                os.makedirs(a.draw, exist_ok=True)
                draw(png, lay, V, os.path.join(a.draw, os.path.basename(png)[:-4] + '-check.png'))
    lines = ['# 配置の検査 (hd2d-layout-check)', '', '規則は scripts/hd2d-layout-check.py の先頭。違反 %d 件 (%d 枚)。' % (total, len(files)), '',
             '| 記録 | 違反 | 注意 | 記録の失敗 |', '|---|---|---|---|']
    for k, r in res.items():
        if 'error' in r:
            lines.append('| %s | 読めない | %s | |' % (k, r['error']))
            continue
        lines.append('| %s | %d | %d | %d |' % (k, len(r['violations']), len(r['warnings']), len(r['errors'])))
    for k, r in res.items():
        if r.get('violations') or r.get('errors'):
            lines += ['', '## ' + k]
            for e in r.get('violations', []):
                lines.append('- **%s** %s' % (e['rule'], e['msg']))
            for e in r.get('errors', []):
                lines.append('- 記録の失敗: %s' % e)
    md = '\n'.join(lines) + '\n'
    if a.md:
        with open(a.md, 'w', encoding='utf-8') as f:
            f.write(md)
    if a.json:
        with open(a.json, 'w', encoding='utf-8') as f:
            json.dump(res, f, ensure_ascii=False, indent=1)
    print(md)
    return 1 if total else 0


if __name__ == '__main__':
    sys.exit(main())

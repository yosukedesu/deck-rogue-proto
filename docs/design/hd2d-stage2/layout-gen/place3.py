#!/usr/bin/env python3
"""HD-2D 段2 レーン C3: 幕3 の設計図 (act3_layout*.json) の配置の規則を数える (計画 docs/design/hd2d-stage2-plan-2026-10-02.md §1 幕3・§2 C の段1b・
約束 docs/design/hd2d-stage2/contracts.md C3 の幕3 版)。

幕2 の place2.py をそのまま import して R1〜R7・L8 を数え (place2.py は直さない)、幕3 の規則を足す:
  R8  判定の画の小物 (半立体の小物＝像・篝火・結晶・瓦礫・壺・碑と、折れ柱 pillar-broken) が PC の画面に 8 以下 (分析 §5-1)。鎖・小札は数えない
  R9  主人公の後ろの窓 (PC x 300〜560・行 380〜560。art-bible §2-2) に暗い物 (光・段・壁・階段・崩れ石・小札の外) の見えている画素が 20px² 以下
  R10 光の柱 (pillar-shaft-a) が主人公の真後ろ: PC の主人公の頭の行で、柱の幅の中に主人公の足元の x が入り (u 0.05〜0.6)、柱の中心が主人公より右 (分析 §5-1・propB の「芯を右へ」)
  R11 スマホで組まない物 (phone.hide) の表 (計画 §2 C 段1b: 壁2・櫓・裂け目・池・機械の庭・mist-far) の名前がスマホで組まれていない
  R12 名前の約束 (fire1・fire2・fire1-halo・fire2-halo・lampC・crack。レーン B の lights の at が読む) が設計図にある
  R13 小札が block・柱・岩の中に埋まらない (Diorama.HeightAtPath は slab しか見ない = block の足跡の中の小札は下の段の天面に置かれ、
      block の中に隠れる。控え壁・段の嵩上げ wall-tier-・大階段 steps- と側壁・崩れ石。place2 の R4 は小札の足元しか見ない)。PC とスマホの置き場
画は place2 の光なしの構図に、光の柱の四角形・名前を重ねた物 (place3-<設計図>-<PC|PH|PC21>.png) と、
5 場面 (巻物 4 体・彫師と影・石殻 1 体・門番 1 体 128・人形 9＋彫師・スマホの巻物 4 体) の座席だけを重ねた物 (place3-<設計図>-scene-<場面>.png)。

使い方
  place3.py <layout.json> [<layout.json> ...] [--out-dir DIR] [--img] [--md PATH] [--allow-missing-ui]
"""
import argparse
import contextlib
import json
import math
import os
import sys

import numpy as np
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, '..', '..', '..', '..'))
sys.path.insert(0, os.path.join(REPO, 'docs', 'design', 'hd2d-slice', 'r2-layout-gen'))
sys.path.insert(0, HERE)
import place  # noqa: E402
import place2  # noqa: E402
from place import Ground  # noqa: E402

# 幕3 の規則で place2 の 3 つの値を広げる (place2.py のファイルは直さない):
#   HUNG_SRC      幕3 の吊る物 (鎖・吊り灯・灯の頭) は浮いてよい (R6 の例外)。灯の頭は柱の首に掛けた灯 (絵の籠は首より広い = 片浮きに見える)
#   SUPPORT_NAMES 灯柱 lamp-post (kind pillar) も板の支えに数える (灯柱の上に灯の頭を載せる)
#   stand_blocks  幕3 の柱 (kind pillar) も板の立ち方 (R6・R7) の block に数える (板が柱を貫くのを数える)
# 広げるのは act3_rules() の with の中だけ (check3 と gen_act3.build が入る)。出たら元に戻す = place3 を import したプロセスで
# 幕2 の設計図を place2 で検査しても幕2 の R6・R7 は甘くならない (直しの輪 2026-10-03・反証の指摘。前は import した時に書き換えたままだった)
_HUNG_SRC2 = tuple(place2.HUNG_SRC)
_SUPPORT_NAMES2 = tuple(place2.SUPPORT_NAMES)
_stand_blocks2 = place2.stand_blocks
HUNG_SRC3 = _HUNG_SRC2 + ('chainHang', 'lampHang', 'lampHead')
SUPPORT_NAMES3 = _SUPPORT_NAMES2 + ('lamp-post',)


def _stand_blocks3(L, G, phone=False):
    out = _stand_blocks2(L, G, phone=phone)
    for b in L['parts']:
        if b['kind'] != 'pillar':
            continue
        q = place2.phone_place(b, place2._PHONE_CAM) if phone else b
        if q is None:
            continue
        over = max(place2.num(q, 'capOver', 0.15), place2.num(q, 'baseOver', 0.12))
        bb = dict(kind='block', name=q.get('name'), t=q.get('t', 0.0), s=q.get('s', 0.0), y=q.get('y', 0.0), abs=q.get('abs', False), yaw=q.get('yaw', 0.0),
                  w=place2.num(q, 'w', 0.9) + 2 * over, d=place2.num(q, 'd', 0.9) + 2 * over, h=place2.num(q, 'h', 5.0), sink=place2.num(q, 'sink', 0.15),
                  scale=q.get('scale', 1.0))
        out.append((bb, place2.block_span(G, bb)))
    return out


@contextlib.contextmanager
def act3_rules():
    """place2 の HUNG_SRC・SUPPORT_NAMES・stand_blocks を幕3 の値にして、出る時に元へ戻す (入れ子にしてよい = 外側の with が戻す)"""
    if place2.stand_blocks is _stand_blocks3:
        yield
        return
    saved = (place2.HUNG_SRC, place2.SUPPORT_NAMES, place2.stand_blocks)
    place2.HUNG_SRC, place2.SUPPORT_NAMES, place2.stand_blocks = HUNG_SRC3, SUPPORT_NAMES3, _stand_blocks3
    try:
        yield
    finally:
        place2.HUNG_SRC, place2.SUPPORT_NAMES, place2.stand_blocks = saved

LANE_DIR = os.path.expanduser('~/.cache/deck-rogue/hd2d-stage2/lanes/C3')
HERO_WINDOW = (300.0, 380.0, 260.0, 180.0)        # PC の主人公の後ろの窓 (x・行・幅・高さ。art-bible §2-2)
JUDGE_PROP_SRC = ('statue', 'brazier', 'brazierCold', 'crystalCluster', 'pillarBroken', 'pillarFallen', 'rubblePile', 'urn', 'stele', 'lampPost', 'lampHang')
JUDGE_PROP_NAMES = ('pillar-broken',)
NOT_DARK_KINDS = ('slab', 'mist', 'fog', 'shaft', 'halo', 'litter', 'frame')
NOT_DARK_NAMES = ('wall-', 'steps-', 'collapse-rock-')
PHONE_HIDE_NAMES = ('crack', 'pond-bed', 'mist-far', 'wall-backstop')   # 計画の phone.hide の表のうち、この設計図にある物 (櫓・機械の庭・壁2 の板は組んでいない)
LIGHT_NAMES = ('fire1', 'fire2', 'fire1-halo', 'fire2-halo', 'lampC', 'crack')
NAMED_PREFIX = ('fire', 'lamp', 'crack', 'pillar-', 'gate', 'buttress-', 'house-', 'statue', 'urn', 'rubble', 'crystal', 'chain', 'wall-gate', 'collapse')


BURY_MARGIN = 0.15   # 小札の中心から block の足跡の縁までの余白 (小札の絵の半分ほど = 縁に半分埋まる物も数える)


def bury_list(L, G, phone=False):
    """小札を埋めうる物 [(名前, 判定の関数 (t, s, 小札の地面の高さ) → bool)]。block と柱は place2.stand_blocks (act3_rules の中なら柱も入る)・岩は円柱"""
    out = []
    for b, (lo, hi) in place2.stand_blocks(L, G, phone=phone):
        def f(t, s, gy, b=b, lo=lo, hi=hi):
            if not (lo < gy + 0.03 < hi):
                return False
            sc = place2.num(b, 'scale', 1.0) or 1.0
            a = math.radians(place2.num(b, 'yaw', 0.0))
            dt, ds = t - place2.num(b, 't', 0.0), s - place2.num(b, 's', 0.0)
            lx = dt * math.cos(a) - ds * math.sin(a)
            lz = dt * math.sin(a) + ds * math.cos(a)
            return abs(lx) <= place2.num(b, 'w', 1.6) * sc / 2 + BURY_MARGIN and abs(lz) <= place2.num(b, 'd', 1.2) * sc / 2 + BURY_MARGIN
        out.append((b.get('name') or 'block', f))
    for r in L['parts']:
        if r['kind'] != 'rock':
            continue
        q = place2.phone_place(r, place2._PHONE_CAM) if phone else r
        if q is None:
            continue
        sc = place2.num(q, 'scale', 1.0) or 1.0
        rt, rs = place2.num(q, 't', 0.0), place2.num(q, 's', 0.0)
        base = place2.num(q, 'y', 0.0) if q.get('abs') else G.gy(rt, rs) + place2.num(q, 'y', 0.0)
        top = base + place2.num(q, 'h', 1.0) * sc
        rad = place2.num(q, 'r', 0.5) * sc

        def f(t, s, gy, rt=rt, rs=rs, base=base, top=top, rad=rad):
            return base - 0.2 < gy + 0.03 < top and math.hypot(t - rt, s - rs) <= rad + BURY_MARGIN
        out.append((q.get('name') or 'rock', f))
    return out


def litter_buried(L, G, p, lists=None):
    """小札 p (PC とスマホの置き場) を埋めている物の名前の一覧 (空なら埋まらない)。lists = {False: bury_list(PC), True: bury_list(スマホ)} を渡すと速い"""
    hit = []
    for ph in (False, True):
        q = place2.phone_place(p, place2._PHONE_CAM) if ph else p
        if q is None:
            continue
        t, s = place2.num(q, 't', 0.0), place2.num(q, 's', 0.0)
        gy = G.gy(t, s)
        bl = lists[ph] if lists is not None else bury_list(L, G, phone=ph)
        for nm, f in bl:
            if f(t, s, gy):
                hit.append(('PH ' if ph else 'PC ') + nm)
    return hit


def audit_fixed(P, judge, log):
    """先に置いた物 (小物・小札・段・霧・壁の外) の規則の掛かりをログに出す (焼いた後の check でも数える)"""
    for p in P:
        if p['kind'] in ('slab', 'mist', 'litter', 'shaft') or (p.get('name') or '').startswith(place2.NO_SILHOUETTE_NAMES):
            continue
        pr = judge.problems(p)
        if pr:
            log.append('規則に掛かる (先に置いた物): %s %s' % (p.get('name') or p.get('src'), pr[:3]))


def _placed(L, G, cam, with_image=True):
    out = []
    for i, p in enumerate(L['parts']):
        o = place2.place_any(L, G, cam, i, p, with_image)
        if o is not None:
            out.append(o)
    return out


def _vis(o, cam, tdf):
    if o.note in ('phone-hide', 'no-art', 'flag-hide') or o.box is None:
        return None, 0
    return place.visible_mask(o, cam, tdf, cam.W, cam.H)


def shaft_quad(G, cam, L, name):
    for p in L['parts']:
        if p['kind'] == 'shaft' and p.get('name') == name:
            q = place2.phone_place(p, cam)
            if q is None:
                return None
            return place.place_shaft(G, cam, q)
    return None


def check3(L, want_img=False, out_dir=LANE_DIR, tag=None):
    with act3_rules():
        return _check3(L, want_img=want_img, out_dir=out_dir, tag=tag)


def _check3(L, want_img=False, out_dir=LANE_DIR, tag=None):
    tag = tag or L.get('_name', 'layout')
    r = place2.check(L, want_img=False, out_dir=out_dir, tag=tag)   # 画は幕3 の色で draw3 が描く (place2.draw は幕2 の材質の色しか持たない)
    G = Ground(L)
    viol = r['violations']
    extra = {}
    cams = place2.cams()
    pc = cams[0]
    rc = G.raycast(pc, 4)
    tdf = place.upsample(rc, pc.W, pc.H)
    placed = _placed(L, G, pc)
    # R8 判定の画の小物
    props = []
    for o in placed:
        src = o.p.get('src') or ''
        nm = o.p.get('name') or ''
        if (o.kind == 'relief' and src in JUDGE_PROP_SRC) or nm in JUDGE_PROP_NAMES:
            vm, npx = _vis(o, pc, tdf)
            if vm is not None and npx > 40:
                props.append(dict(name=nm or src, src=src, px=int(npx)))
    extra['judgeProps'] = props
    if len(props) > 8:
        viol.append('R8 判定の画の小物 %d (8 以下)' % len(props))
    # R9 主人公の後ろの窓
    win = []
    for o in placed:
        nm = o.p.get('name') or ''
        if o.kind in NOT_DARK_KINDS or nm.startswith(NOT_DARK_NAMES):
            continue
        vm, npx = _vis(o, pc, tdf)
        if vm is None or npx == 0:
            continue
        c = place2.rect_count(vm, HERO_WINDOW)
        if c > place2.ZONE_PX:
            win.append(dict(name=nm or o.p.get('src'), kind=o.kind, px=c))
    extra['heroWindow'] = win
    for w in win:
        viol.append('R9 PC 主人公の後ろの窓に %s (%s) %dpx²' % (w['name'], w['kind'], w['px']))
    # R10 光の柱
    hero = pc.project(G.on_path(place2.LEADER[0], place2.LEADER[1], 0.0))
    head_row = float(hero[1]) - 62 * 4.0
    sh = shaft_quad(G, pc, L, 'pillar-shaft-a')
    col = {}
    if sh is not None:
        (x0, y0), (x1, _), (x2, y2), (x3, _) = sh['quad']   # 上の左・上の右・下の右・下の左
        k = (head_row - y0) / max(1e-3, (y2 - y0))
        left = x0 + (x3 - x0) * k
        right = x1 + (x2 - x1) * k
        u = (float(hero[0]) - left) / max(1e-3, right - left)
        ctr = (left + right) / 2
        col = dict(headRow=round(head_row), left=round(left), right=round(right), center=round(ctr), heroX=round(float(hero[0])), u=round(u, 2),
                   lean=round(math.degrees(math.atan2(sh['top'][0] - sh['bottom'][0], sh['bottom'][1] - sh['top'][1])), 1),
                   top=[round(v) for v in sh['top']], bottom=[round(v) for v in sh['bottom']])
        if not (0.05 <= u <= 0.6) or ctr <= float(hero[0]):
            viol.append('R10 光の柱が主人公の真後ろにない (u %.2f・中心 x %d・主人公 x %d)' % (u, ctr, hero[0]))
    else:
        viol.append('R10 光の柱 pillar-shaft-a が無い')
    extra['pillar'] = col
    # R11 スマホで組まない物
    ph_built = []
    for p in L['parts']:
        nm = p.get('name') or ''
        if nm in PHONE_HIDE_NAMES:
            ph = p.get('phone') if isinstance(p.get('phone'), dict) else {}
            if not ph.get('hide'):
                ph_built.append(nm)
    extra['phoneHideMissing'] = ph_built
    for nm in ph_built:
        viol.append('R11 スマホで組まない物 %s がスマホで組まれる' % nm)
    # R12 名前の約束
    names = {p.get('name') for p in L['parts']}
    miss = [n for n in LIGHT_NAMES if n not in names]
    extra['lightNamesMissing'] = miss
    for n in miss:
        viol.append('R12 光の名前 %s が無い' % n)
    # R13 小札が block・柱・岩の中に埋まらない
    lists = {False: bury_list(L, G, phone=False), True: bury_list(L, G, phone=True)}
    buried = []
    for p in L['parts']:
        if p['kind'] != 'litter':
            continue
        hit = litter_buried(L, G, p, lists)
        if hit:
            buried.append(dict(src=p.get('src'), t=p.get('t'), s=p.get('s'), by=hit))
    extra['litterBuried'] = buried
    for b in buried:
        viol.append('R13 小札 %s (t %s・s %s) が %s の中に埋まる' % (b['src'], b['t'], b['s'], '・'.join(b['by'])))
    # 名前つきの部品の画面の置き場 (PC・PH・PC21)
    named = {}
    for cam in cams:
        plc = _placed(L, G, cam, with_image=False)
        d = {}
        for o in plc:
            nm = o.p.get('name') or ''
            if not nm.startswith(NAMED_PREFIX):
                continue
            if o.note == 'phone-hide':
                d[nm] = 'phone-hide'
                continue
            if o.box is None:
                continue
            d[nm] = dict(t=o.p.get('t'), s=o.p.get('s'), y=o.p.get('y'), abs=bool(o.p.get('abs')), foot=[round(v) for v in (o.foot or (0, 0))],
                         box=[round(v) for v in o.box], depth=round(o.depth or 0, 1))
        for p in L['parts']:
            if p['kind'] == 'shaft':
                q = shaft_quad(G, cam, L, p.get('name'))
                d[p.get('name')] = 'phone-hide' if q is None else dict(quad=[[round(a), round(b)] for a, b in q['quad']], top=[round(v) for v in q['top']],
                                                                      bottom=[round(v) for v in q['bottom']])
        named[cam.name] = d
    extra['named'] = named
    r['act3'] = extra
    if want_img:
        for cam in cams:
            ui = place2.load_ui(cam)
            zones = place2.seat_zones(cam, G, ui)
            intents = place2.intent_rects(cam, G, ui) if cam.name != 'PC21' else []
            im = draw3(cam, G, L)
            overlay(L, G, cam, im, zones=zones, intents=intents, ui=ui)
            im.save(os.path.join(out_dir, 'place3-%s-%s.png' % (tag, cam.name)))
        scenes(L, G, out_dir, tag)
    return r


# ------------------------------------------------------------------ 画 (光なしの構図・幕3 の材質の色)

# 段の天面と立面・block の材質ごとの色 (光なしの見分け用。実機の色ではない)
TOP_COL = {'seat-band': (92, 100, 106), 'floor-front-L': (92, 100, 106), 'floor-front-R': (92, 100, 106), 'pond-bed': (18, 28, 34),
           'tier-T1': (112, 120, 124), 'tier-T2': (104, 112, 118), 'tier-T3': (96, 104, 110), 'tier-T4': (88, 96, 102)}
FACE_COL = {'seat-band': (52, 60, 66), 'floor-front-L': (52, 60, 66), 'floor-front-R': (52, 60, 66), 'pond-bed': (14, 22, 26),
            'tier-T1': (74, 86, 96), 'tier-T2': (70, 82, 92), 'tier-T3': (66, 78, 88), 'tier-T4': (62, 74, 84)}
SURF_COL = {'wall': (64, 76, 86), 'step': (124, 130, 132), 'pillar': (150, 148, 138), 'brick': (118, 98, 90), 'terrain': (92, 100, 106)}


def _dk(c, d):
    k = float(np.clip(1.0 - (d - 20.0) / 90.0, 0.45, 1.0))
    return tuple(int(v * k) for v in c)


def draw3(cam, G, L):
    W, H = cam.W, cam.H
    img = np.zeros((H, W, 3), np.float32)
    img[:] = (14, 20, 26)
    rc = G.raycast(cam, 4)
    step = rc['step']
    names = [sl['name'] for sl in G.slabs]
    tops = [sl['top'] for sl in G.slabs]
    for i in range(rc['hit'].shape[0]):
        for j in range(rc['hit'].shape[1]):
            if not rc['hit'][i, j]:
                continue
            wi = rc['slab'][i, j]
            nm = names[wi]
            face = rc['y'][i, j] < tops[wi] - 0.03
            c = (FACE_COL if face else TOP_COL).get(nm, (90, 90, 90))
            y0, x0 = i * step, j * step
            img[y0:y0 + step, x0:x0 + step] = _dk(c, rc['depth'][i, j])
    tdf = place.upsample(rc, W, H)
    placed = [o for o in _placed(L, G, cam, with_image=True) if o.rgba is not None and o.mask is not None and o.note != 'phone-hide']
    back = ('wall-back', 'wall-crown', 'wall-backstop', 'wall-pilaster', 'wall-gate')
    farthest = ('wall-backstop', 'wall-crown')   # 奥の保険と頂は壁より先に (塊の中心の深さで並べると、遠い壁の区切りの上に塗られる = 画だけの話)

    def key(o):
        nm = o.p.get('name') or ''
        return -((o.depth or 0) + (2000.0 if nm.startswith(farthest) else 1000.0 if nm.startswith(back) else 0.0))
    order = sorted(placed, key=key)
    for o in order:
        X0, Y0 = o.origin
        h, w = o.mask.shape
        xa, ya = max(0, X0), max(0, Y0)
        xb, yb = min(W, X0 + w), min(H, Y0 + h)
        if xb <= xa or yb <= ya:
            continue
        sub = o.rgba[ya - Y0:yb - Y0, xa - X0:xb - X0].astype(np.float32)
        m = o.mask[ya - Y0:yb - Y0, xa - X0:xb - X0]
        if o.kind == 'halo':
            a = (sub[..., 3:4] / 255.0) * 0.8
            img[ya:yb, xa:xb] = img[ya:yb, xa:xb] + sub[..., :3] * a
            continue
        m = m & (o.depth <= tdf[ya:yb, xa:xb] + 0.6)
        nm = o.p.get('name') or ''
        col = sub[..., :3]
        if o.kind in ('block', 'rock', 'pillar', 'arch', 'fence', 'marker'):
            surf = o.p.get('surface') or 'step'
            c = SURF_COL.get(surf, (100, 100, 100))
            if nm.startswith('wall-pilaster'):
                c = (78, 90, 98)
            if nm.startswith(('wall-crown', 'wall-backstop')):
                c = (44, 52, 58)
            if nm.startswith('buttress'):
                c = (96, 106, 112)
            tv = o.p.get('tint')
            if tv is not None:
                tv = [tv] * 3 if isinstance(tv, (int, float)) else tv
                c = tuple(int(a * b) for a, b in zip(c, tv[:3]))
            col = np.zeros_like(col) + np.array(_dk(c, o.depth or 30), np.float32)
        img[ya:yb, xa:xb][m] = col[m]
    return Image.fromarray(np.clip(img, 0, 255).astype(np.uint8)).convert('RGB')


def overlay(L, G, cam, im, zones=None, intents=None, ui=None):
    """光の柱の四角形・名前・(渡せば) 座席の帯と意図の札と UI の矩形を im (RGB) に重ねる"""
    d = ImageDraw.Draw(im, 'RGBA')
    for p in L['parts']:
        if p['kind'] != 'shaft':
            continue
        q = place2.phone_place(p, cam)
        if q is None:
            continue
        sh = place.place_shaft(G, cam, q)
        d.polygon(sh['quad'], fill=(220, 240, 235, 30), outline=(200, 255, 240, 200))
    if ui is not None:
        for _, r in ui.get('ui', []) + ui.get('hand', []):
            d.rectangle([r[0], r[1], r[0] + r[2], r[1] + r[3]], fill=(10, 14, 30, 110), outline=(200, 200, 220, 120))
        if ui.get('topbar'):
            r = ui['topbar']
            d.rectangle([r[0], r[1], r[0] + r[2], r[1] + r[3]], fill=(10, 14, 30, 150))
    for nm, fx, fy, fd, hw, head in (zones or []):
        colr = (255, 210, 80) if nm == 'hero' else ((120, 220, 255) if nm.startswith('doll') else (255, 120, 120))
        d.rectangle([fx - hw, head, fx + hw, fy + 20], outline=colr + (90,), width=1)
        d.ellipse([fx - 4, fy - 4, fx + 4, fy + 4], fill=colr + (255,))
    for nm, r in (intents or []):
        d.rectangle([r[0], r[1], r[0] + r[2], r[1] + r[3]], outline=(255, 160, 40, 220), width=2)
    if cam.name == 'PC' and zones is not None:
        b = place2.BOSS160
        d.rectangle([b[0], b[1], b[0] + b[2], b[1] + b[3]], outline=(255, 60, 60, 255), width=2)
    if cam.phone:
        d.rectangle([0, 81, 640, 210], outline=(160, 255, 160, 200), width=2)
    for o in _placed(L, G, cam, with_image=False):
        nm = o.p.get('name') or ''
        if o.box is None or o.note == 'phone-hide' or not nm.startswith(NAMED_PREFIX) or nm.startswith(('house-top', 'house-door', 'buttress-a-cap', 'buttress-b-cap', 'collapse-rock')):
            continue
        d.text((o.box[0], max(0, o.box[1] - 12)), nm, fill=(255, 255, 255, 235), font=place.font(14))
    if cam.name == 'PC':
        x, y, w, h = HERO_WINDOW
        d.rectangle([x, y, x + w, y + h], outline=(255, 230, 120, 200), width=2)
    return im


# ------------------------------------------------------------------ 5 場面の画 (座席だけを重ねる)

SCENES = [
    # (名前, カメラ, 敵の数, 敵の枠, 人形)。計画 §4 T3-* (巻物 4 体 = 錨・彫師と用心深い影・石殻・門番 128・人形 9＋彫師・スマホの巻物 4 体)
    ('quad', 'PC', 4, [64, 64, 64, 64], False),
    ('sculptor', 'PC', 2, [64, 64], False),
    ('shell', 'PC', 1, [64], False),
    ('warden', 'PC', 1, [128], False),
    ('dolls', 'PC', 1, [64], True),
    ('quad-ph', 'PH', 4, [64, 64, 64, 64], False),
]


def scenes(L, G, out_dir, tag):
    cams = {c.name: c for c in place2.cams()}
    base = {}
    for nm, cn, n, frames, dolls in SCENES:
        cam = cams[cn]
        if cn not in base:
            base[cn] = overlay(L, G, cam, draw3(cam, G, L))
        im = base[cn].copy()
        d = ImageDraw.Draw(im, 'RGBA')
        px = 4.0 if not cam.phone else 2.4 * cam.canvas_sf
        # 主人公 (62 ドット) と敵と人形の箱 (足元の中心・背丈 = 枠。幅は背丈の 0.8 倍の目安)
        hx, hy, _ = cam.project(G.on_path(place2.LEADER[0], place2.LEADER[1], 0.0))
        hh = 62 * px
        d.rectangle([hx - hh * 0.4, hy - hh, hx + hh * 0.4, hy], fill=(255, 210, 80, 70), outline=(255, 210, 80, 230), width=2)
        for i, e in enumerate(place2.enemy_seats(cam, G, n)):
            fr = frames[i]
            hpx = place2.frame_px(cam, fr)
            d.rectangle([e['x'] - hpx * 0.45, e['y'] - hpx, e['x'] + hpx * 0.45, e['y']], fill=(255, 110, 110, 70), outline=(255, 110, 110, 230), width=2)
            w = 200.0 * (0.7875 if cam.phone else 1.0)
            top = e['y'] - hpx - (56 if not cam.phone else 44)
            d.rectangle([e['x'] - w / 2, top, e['x'] + w / 2, top + (50 if not cam.phone else 40)], outline=(255, 160, 40, 230), width=2)
        if dolls:
            for dl in place2.doll_seats(cam, G):
                dh = 32 * px
                d.rectangle([dl['x'] - dh * 0.4, dl['y'] - dh, dl['x'] + dh * 0.4, dl['y']], fill=(120, 220, 255, 70), outline=(120, 220, 255, 220), width=1)
        if cam.name == 'PC' and frames[0] >= 128:
            b = place2.BOSS160
            d.rectangle([b[0], b[1], b[0] + b[2], b[1] + b[3]], outline=(255, 60, 60, 255), width=2)
        d.text((12, cam.H - 30), '%s  %s  (光なしの構図・キャラは箱)' % (tag, nm), fill=(255, 255, 255, 255), font=place.font(20))
        im.convert('RGB').save(os.path.join(out_dir, 'place3-%s-scene-%s.png' % (tag, nm)))


# ------------------------------------------------------------------ 表

def summary_md3(results):
    md = place2.summary_md(results)
    md = md.replace('# place2 — 幕2 の設計図の規則の検査（レーン C・自動生成）', '# place3 — 幕3 の設計図の規則の検査（レーン C3・自動生成）', 1)
    md = md.replace('規則と数え方は `docs/design/hd2d-stage2/layout-gen/place2.py` の頭。',
                    '規則と数え方は `docs/design/hd2d-stage2/layout-gen/place2.py` の頭（R1〜R7・L8）と `place3.py` の頭（R8〜R13）。', 1)
    L = [md, '\n# 幕3 の規則（R8〜R13）\n']
    for r in results:
        a = r.get('act3') or {}
        L.append('## %s\n' % r['name'])
        L.append('- R8 判定の画の小物 %d: %s' % (len(a.get('judgeProps', [])), '・'.join('%s (%dpx)' % (p['name'], p['px']) for p in a.get('judgeProps', []))))
        L.append('- R9 主人公の後ろの窓: %s' % ('なし' if not a.get('heroWindow') else '・'.join('%s %dpx²' % (w['name'], w['px']) for w in a['heroWindow'])))
        pc = a.get('pillar') or {}
        if pc:
            L.append('- R10 光の柱 (PC・主人公の頭の行 %d): 幅 x %d〜%d・中心 %d・主人公 x %d・u %.2f・傾き %.1f°・上の口 %s・足 %s' % (
                pc['headRow'], pc['left'], pc['right'], pc['center'], pc['heroX'], pc['u'], pc['lean'], pc['top'], pc['bottom']))
        L.append('- R11 スマホで組まない物の抜け: %s' % ('なし' if not a.get('phoneHideMissing') else '・'.join(a['phoneHideMissing'])))
        L.append('- R12 光の名前の抜け: %s' % ('なし' if not a.get('lightNamesMissing') else '・'.join(a['lightNamesMissing'])))
        L.append('- R13 埋まる小札: %s' % ('なし' if not a.get('litterBuried') else '・'.join('%s (t %s・s %s・%s)' % (b['src'], b['t'], b['s'], '・'.join(b['by'])) for b in a['litterBuried'])))
        for cn, d in (a.get('named') or {}).items():
            L.append('\n### 名前つきの部品 %s\n' % cn)
            for k in sorted(d):
                v = d[k]
                if v == 'phone-hide':
                    L.append('- %s: スマホでは組まない' % k)
                elif 'quad' in v:
                    L.append('- %s: 上の口 %s → 足 %s' % (k, v['top'], v['bottom']))
                else:
                    L.append('- %s: t %s・s %s・y %s%s → 足元 %s・箱 %s・深さ %s' % (k, v['t'], v['s'], v['y'], '（abs）' if v['abs'] else '', v['foot'], v['box'], v['depth']))
        L.append('')
    return '\n'.join(L) + '\n'


def check_files(paths, want_img=True, out_dir=LANE_DIR, md=None):
    os.makedirs(out_dir, exist_ok=True)
    res = []
    rc = 0
    for fp in paths:
        L = json.load(open(fp))
        tag = os.path.splitext(os.path.basename(fp))[0]
        r = check3(L, want_img=want_img, out_dir=out_dir, tag=tag)
        res.append(r)
        with open(os.path.join(out_dir, 'place3-%s.json' % tag), 'w') as f:
            json.dump(r, f, ensure_ascii=False, indent=1)
        print('%s: 部品 %d・小札 %d・材質 %d・違反 %d' % (tag, r['parts'], r['litter'], r['materials'], len(r['violations'])))
        for v in r['violations'][:40]:
            print('   ' + v)
        rc |= 1 if r['violations'] else 0
    with open(md or os.path.join(out_dir, 'place3-report.md'), 'w') as f:
        f.write(summary_md3(res))
    return rc


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('layouts', nargs='+')
    ap.add_argument('--out-dir', default=LANE_DIR)
    ap.add_argument('--img', action='store_true')
    ap.add_argument('--md', default=None)
    ap.add_argument('--allow-missing-ui', action='store_true', help='基準の撮影の UI の矩形が無くても続ける (警告だけ)')
    a = ap.parse_args()
    place2.ALLOW_MISSING_UI = a.allow_missing_ui
    try:
        return check_files(a.layouts, want_img=a.img, out_dir=a.out_dir, md=a.md)
    except place2.MissingUIError as e:
        print('止めた: %s (--allow-missing-ui で続ける)' % e, file=sys.stderr)
        for f in e.files:
            print('  無い: ' + f, file=sys.stderr)
        return 1


if __name__ == '__main__':
    sys.exit(main())

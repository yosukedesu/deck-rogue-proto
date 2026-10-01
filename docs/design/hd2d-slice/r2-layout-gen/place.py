#!/usr/bin/env python3
"""レーン C (箱庭の設計図) の配置の計算 — 計画 docs/design/hd2d-round2-plan-2026-10-01.md §2 レーン C。

設計図 (act1_layout*.json) の部品を、二周目のカメラ (PC 22°・5°・足元 0.407／スマホ 22°・7°・0.525) で画面へ写し、
配置の規則を機械的に確かめる。元にした物: honke/mock_plan.py・r2/stage/proj.py・scripts/hd2d-seatfit.py の Cam。
ほかのレーンの書きかけ (scripts/hd2d-seatfit.py) に依らないよう、カメラの式はここに写した。

写した式 (unity/Assets/Game/StageCamera.cs・Diorama.cs・DioramaMesh.cs・ReliefMesh.cs・DioramaTextures.cs。2026-10-01 の値)
  カメラ: PlaneUnitsPerScreen = PC 10.8・スマホ 10.8 / clamp(アスペクト/1.8, 1, 1.2)・dist = PUPS/2 / tan(FOV/2)・回転 Euler(pitch,0,0)
          原点が画面の下から GroundLine の高さに来る位置から dist 引く。FOV は縦。DistanceRatio r = tan(18°)/tan(FOV/2)
  道の座標: OnPath(t, s, y) = Euler(0, pathYaw, 0)·(t, y, s)。HeightAtPath = 段 (slab) のうち (t,s) を含む物の天面の最大 (無ければいちばん低い天面)
  部品: relief・card・litter・tree(trunk=relief) は立った板 (足元の中心が原点・表は −z・Euler(0, yaw, 0) で回す・裏は描かない = Cull Back)。
        絵はアトラスと同じく alpha>16 の外接矩形に切る (DioramaTextures.CropToAlpha)。1 unit = 25 テクセル。litter は大きさ 1 倍。
        block・rig・fence・marker は道の向き (pathYaw + yaw)・rock は世界の yaw。形は箱で近似。
  額縁 (frame): カメラから depth の所に、画面の割合 (vx, vy) で置く (Diorama.OnCameraLayout)。半立体は中心が原点、flat の source は足元の中心が原点 (DioramaMesh.Card)。
  地面: 段の高さ場を光線で進めて当てる (画素 4px ごと)。どの段にも入らない所は「地面なし」= 背景 (背景の板) が見える。

使い方
  place.py <layout.json> [--out <dir>] [--tag <名前>] [--img] [--fog A,B,C]
出力
  <out>/<tag>.json (表)・<out>/<tag>.md (人が読む要約)・--img なら <tag>-PC.png・<tag>-PH.png (光なしの構図の画)
"""
import argparse
import json
import math
import os
import sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont

REPO = '/home/yosuke/projects/deck-rogue-proto'
RES = REPO + '/unity/Assets/Resources/'
SP = '/tmp/claude-1000/-home-yosuke-projects-deck-rogue-proto/2379737a-3c0d-4f8d-8002-16334720cb9f/scratchpad/hd2d/'
TPU = 25.0
FONT = None
for fp in ('/home/yosuke/.local/share/fonts/NotoSansCJKjp-Regular.otf', '/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc'):
    if os.path.exists(fp):
        FONT = fp
        break


def font(sz):
    return ImageFont.truetype(FONT, sz) if FONT else ImageFont.load_default()


# ------------------------------------------------------------------ カメラ

class Cam:
    """StageCamera.LayoutCamera の写し。screen() は画面の px (左上が原点)"""

    def __init__(self, name, W, H, phone, fov, pitch, gl):
        self.name, self.W, self.H, self.phone = name, W, H, phone
        self.fov, self.pitch, self.gl = fov, pitch, gl
        pups = 10.8
        if phone:
            zoom = min(1.2, max(1.0, (W / H) / 1.8))
            pups = 10.8 / zoom
        self.pups = pups
        self.k = pups / H
        self.tanV = math.tan(math.radians(fov) * 0.5)
        self.dist = (pups * 0.5) / self.tanV
        self.f = (H * 0.5) / self.tanV
        pr = math.radians(pitch)
        self.fwd = np.array([0.0, -math.sin(pr), math.cos(pr)])
        self.up = np.array([0.0, math.cos(pr), math.sin(pr)])
        self.right = np.array([1.0, 0.0, 0.0])
        dy = (H * gl - H * 0.5) * self.k
        p0 = -self.up * dy
        self.base = p0 - self.fwd * self.dist
        self.r = math.tan(math.radians(18.0)) / self.tanV   # DistanceRatio (基準 36°)
        self.canvas_sf = H / 675.0 if phone else 1.0         # キャンバス→画面 (スマホは高さ 675 のキャンバス)

    def project(self, w):
        """世界の点 (…,3) → (x, y(上から), 深さ)"""
        w = np.asarray(w, float)
        rel = w - self.base
        d = rel @ self.fwd
        x = rel @ self.right
        y = rel @ self.up
        with np.errstate(divide='ignore', invalid='ignore'):
            sx = self.W * 0.5 + self.f * x / d
            sy = self.H * 0.5 - self.f * y / d
        return sx, sy, d

    def ray_dirs(self, xs, ys):
        X, Y = np.meshgrid(xs, ys)
        xn = (X - self.W * 0.5) / self.f
        yn = (self.H * 0.5 - Y) / self.f
        d = self.fwd[None, None, :] + xn[..., None] * self.right + yn[..., None] * self.up
        return d / np.linalg.norm(d, axis=-1, keepdims=True), X, Y


def cams(extra_wide=False):
    c = [Cam('PC', 1920, 1080, False, 22, 5, 0.407), Cam('PH', 1920, 886, True, 22, 7, 0.525)]
    if extra_wide:
        c.append(Cam('PC21', 2560, 1080, False, 22, 5, 0.407))
    return c


# ------------------------------------------------------------------ 道の座標と地面

def yaw_rot(deg, x, y, z):
    a = math.radians(deg)
    return np.array([x * math.cos(a) + z * math.sin(a), y, -x * math.sin(a) + z * math.cos(a)])


class Ground:
    def __init__(self, L):
        self.yaw = L.get('pathYaw', -22.0)
        self.slabs = []
        for p in L['parts']:
            if p['kind'] != 'slab':
                continue
            fr = sorted(p['front'], key=lambda q: q[0])
            self.slabs.append(dict(name=p.get('name'), T=np.array([q[0] for q in fr]), S=np.array([q[1] for q in fr]),
                                   back=p.get('back', 10.0), top=p.get('top', 0.0), bottom=p.get('bottom', -1.0)))
        self.lowest = min(s['top'] for s in self.slabs)

    def on_path(self, t, s, y):
        return yaw_rot(self.yaw, t, y, s)

    def to_path(self, x, z):
        a = math.radians(-self.yaw)
        t = x * np.cos(a) + z * np.sin(a)
        s = -x * np.sin(a) + z * np.cos(a)
        return t, s

    def height(self, t, s, none=None):
        """Diorama.HeightAtPath (none を渡すと「どの段にも入らない」所をその値に)"""
        t = np.asarray(t, float)
        s = np.asarray(s, float)
        best = np.full(t.shape, -np.inf)
        which = np.full(t.shape, -1)
        for i, sl in enumerate(self.slabs):
            fa = np.interp(t, sl['T'], sl['S'])
            inside = (t >= sl['T'][0]) & (t <= sl['T'][-1]) & (s >= fa - 1e-4) & (s <= sl['back'])
            upd = inside & (sl['top'] > best)
            best = np.where(upd, sl['top'], best)
            which = np.where(upd, i, which)
        miss = np.isneginf(best)
        if none is None:
            best = np.where(miss, self.lowest, best)
        else:
            best = np.where(miss, none, best)
        return best, which

    def gy(self, t, s):
        h, _ = self.height(np.array([t]), np.array([s]))
        return float(h[0])

    def raycast(self, cam, step_px=4):
        xs = np.arange(step_px // 2, cam.W, step_px)
        ys = np.arange(step_px // 2, cam.H, step_px)
        dirs, X, Y = cam.ray_dirs(xs, ys)
        C = cam.base
        hit = np.zeros(X.shape, bool)
        P = np.zeros(X.shape + (3,))
        W_ = np.full(X.shape, -1)
        tt = 0.0
        while tt < 260 and not hit.all():
            tt += 0.05 if tt < 40 else (0.12 if tt < 90 else 0.3)
            Q = C + dirs * tt
            t, s = self.to_path(Q[..., 0], Q[..., 2])
            hh, wi = self.height(t, s, none=-1e9)
            new = (~hit) & (Q[..., 1] <= hh)
            P[new] = Q[new]
            W_[new] = wi[new]
            hit |= new
        depth = (P - C) @ cam.fwd
        t, s = self.to_path(P[..., 0], P[..., 2])
        return dict(xs=xs, ys=ys, hit=hit, depth=np.where(hit, depth, np.inf), t=t, s=s, y=P[..., 1], slab=W_, step=step_px)


# ------------------------------------------------------------------ 絵

_ART = {}
DRAFT_PREFIX = 'Art/stage/act1/'
DRAFT_DIR = SP + 'r2/lane-D/r2-draft/'


def art_for(L, src):
    """sources の art のうち先に見つかった絵 (alpha>16 で切った RGBA) と、見つかったパス"""
    if src in _ART:
        return _ART[src]
    sd = L['sources'].get(src)
    res = (None, None)
    if sd:
        arts = sd['art'] if isinstance(sd['art'], list) else [sd['art']]
        for a in arts:
            fp = RES + a + '.png'
            if not os.path.exists(fp) and a.startswith(DRAFT_PREFIX):
                fp = DRAFT_DIR + a[len(DRAFT_PREFIX):] + '.png'   # レーン D の下書き (まだ Art に無い新しい絵)
            if os.path.exists(fp):
                im = Image.open(fp).convert('RGBA')
                al = np.array(im)[..., 3]
                ys, xs = np.nonzero(al > 16)
                if len(xs):
                    im = im.crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1))
                res = (im, a)
                break
    _ART[src] = res
    return res


def is_flat(L, src):
    sd = L['sources'].get(src) or {}
    return bool(sd.get('flat', False))


# ------------------------------------------------------------------ 部品 → 画面

BOARD_KINDS = ('relief', 'card', 'litter', 'tree')
BOX_KINDS = ('rock', 'block', 'rig', 'fence', 'marker')
PATH_ALIGNED = ('block', 'rig', 'fence', 'marker')
TRUNK_SRC = ('trunk', 'pine', 'tree', 'Giant', 'Oak')      # 縦の幹 (太い幹の規則の対象)
TREEISH_SRC = ('trunk', 'pine', 'tree', 'Giant', 'Oak', 'canopy', 'leaf', 'needle', 'Bough', 'treeline', 'frame', 'giant')


def perspective_coeffs(dst, src):
    """PIL の PERSPECTIVE: 出力 (dst) の点 → 入力 (src) の点 の係数"""
    A = []
    B = []
    for (x, y), (u, v) in zip(dst, src):
        A.append([x, y, 1, 0, 0, 0, -u * x, -u * y])
        A.append([0, 0, 0, x, y, 1, -v * x, -v * y])
        B += [u, v]
    return np.linalg.lstsq(np.array(A, float), np.array(B, float), rcond=None)[0]


def hull(pts):
    pts = sorted(set(map(tuple, pts)))
    if len(pts) <= 2:
        return pts

    def cross(o, a, b):
        return (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0])
    lo, hi = [], []
    for p in pts:
        while len(lo) >= 2 and cross(lo[-2], lo[-1], p) <= 0:
            lo.pop()
        lo.append(p)
    for p in reversed(pts):
        while len(hi) >= 2 and cross(hi[-2], hi[-1], p) <= 0:
            hi.pop()
        hi.append(p)
    return lo[:-1] + hi[:-1]


class Placed:
    """1つの部品の画面の姿 (1つのカメラ)"""
    __slots__ = ('i', 'p', 'kind', 'src', 'art', 'box', 'depth', 'visible', 'facing', 'mask', 'rgba', 'origin', 'wpx', 'note', 'worldH', 'foot')


def place_part(L, G, cam, i, p, with_image=False):
    k = p['kind']
    o = Placed()
    o.i, o.p, o.kind = i, p, k
    o.src = p.get('relief') if k == 'tree' else p.get('src')
    o.art, o.box, o.depth, o.visible, o.facing, o.mask, o.rgba, o.origin, o.wpx, o.note, o.worldH, o.foot = None, None, None, False, True, None, None, (0, 0), 0.0, '', 0.0, None
    if k in ('slab', 'fog', 'shaft'):
        return None
    if k == 'frame':
        return place_frame(L, cam, o, with_image)
    ph = p.get('phone') if (cam.phone and isinstance(p.get('phone'), dict) and k not in ('slab', 'frame')) else None   # Diorama.PlaceOf (段 slab・額縁は上書きしない)
    if ph and ph.get('hide'):
        o.note = 'phone-hide'
        return o
    if ph:
        p = dict(p)
        for kk in ('t', 's', 'y', 'scale'):
            if kk in ph:
                p[kk] = ph[kk]
        o.p = p
    t, s = p.get('t', 0.0), p.get('s', 0.0)
    gy = G.gy(t, s)
    y0 = p['y'] if p.get('abs') else gy + p.get('y', 0.0)
    pos = G.on_path(t, s, y0)
    yaw = (G.yaw + p.get('yaw', 0.0)) if k in PATH_ALIGNED else p.get('yaw', 0.0)
    scale = p.get('scale', 1.0) or 1.0
    fx, fy, fd = cam.project(pos)
    o.foot = (float(fx), float(fy))
    o.depth = float(fd)
    if k in BOARD_KINDS:
        im, a = art_for(L, o.src) if o.src else (None, None)
        if im is None:
            o.note = 'no-art'
            return o
        o.art = a
        if k == 'litter':
            scale = 1.0
        if k == 'tree':
            scale = scale * p.get('reliefScale', 1.0)
        w, h = im.size[0] / TPU * scale, im.size[1] / TPU * scale
        o.worldH = h
        corners = []
        for (lx, ly) in ((-w / 2, 0), (-w / 2, h), (w / 2, h), (w / 2, 0)):
            wp = pos + yaw_rot(yaw, lx, ly, 0.0)
            corners.append(cam.project(wp))
        n = yaw_rot(yaw, 0, 0, -1.0)
        ctr = pos + np.array([0, h / 2, 0])
        o.facing = bool(n @ (cam.base - ctr) > 0)
        xs = [c[0] for c in corners]
        ys = [c[1] for c in corners]
        o.box = (min(xs), min(ys), max(xs), max(ys))
        o.wpx = float(abs(corners[3][0] - corners[0][0]))
        o.depth = float(cam.project(ctr)[2])
        if with_image and o.facing:
            src_im = im.transpose(Image.FLIP_LEFT_RIGHT) if p.get('flip') else im
            if p.get('tint') is not None:
                tv = p['tint']
                tv = [tv, tv, tv] if isinstance(tv, (int, float)) else tv
                arr = np.array(src_im).astype(float)
                arr[..., :3] *= np.array(tv[:3])
                src_im = Image.fromarray(arr.clip(0, 255).astype(np.uint8), 'RGBA')
            render_quad(o, src_im, corners, cam)
    else:
        if k == 'rock':
            r = p.get('r', 0.6)
            ex, ez, y1, yb = r, r * p.get('squash', 0.8), p.get('h', 0.7), 0.0
        elif k == 'block':
            ex, ez, y1, yb = p.get('w', 1.6) / 2, p.get('d', 1.2) / 2, p.get('h', 1.0), -p.get('sink', 0.2)
        elif k == 'rig':
            ex, ez, y1, yb = p.get('w', 2.0) / 2, p.get('d', 1.6) / 2, p.get('h', 1.9) + 2 * p.get('wheel', 0.42), 0.0
        elif k == 'fence':
            ex, ez, y1, yb = p.get('len', 3.0) / 2, 0.1, p.get('h', 0.85), 0.0
        else:  # marker
            ex, ez, y1, yb = 0.3, 0.2, p.get('h', 1.3), 0.0
        ex, ez, y1, yb = ex * scale, ez * scale, y1 * scale, yb * scale
        o.worldH = y1 - yb
        pts = []
        for lx in (-ex, ex):
            for lz in (-ez, ez):
                for ly in (yb, y1):
                    pts.append(cam.project(pos + yaw_rot(yaw, lx, ly, lz)))
        xs = [q[0] for q in pts]
        ys = [q[1] for q in pts]
        o.box = (min(xs), min(ys), max(xs), max(ys))
        o.wpx = float(o.box[2] - o.box[0])
        o.depth = float(cam.project(pos + np.array([0, (y1 + yb) / 2, 0]))[2])
        if with_image:
            hl = hull([(q[0], q[1]) for q in pts])
            x0, y0_, x1, y1_ = [int(math.floor(v)) for v in o.box]
            x1 += 2
            y1_ += 2
            if x1 - x0 > 0 and y1_ - y0_ > 0 and x1 - x0 < 8000 and y1_ - y0_ < 8000:
                m = Image.new('L', (x1 - x0, y1_ - y0_), 0)
                ImageDraw.Draw(m).polygon([(a - x0, b - y0_) for a, b in hl], fill=255)
                o.mask = np.array(m) > 0
                o.origin = (x0, y0_)
                col = {'rock': (92, 98, 104), 'block': (110, 108, 100), 'rig': (120, 90, 60), 'fence': (120, 96, 70), 'marker': (130, 120, 100)}[k]
                tv = p.get('tint')
                if tv is not None:
                    tv = [tv, tv, tv] if isinstance(tv, (int, float)) else tv
                    col = tuple(int(c * v) for c, v in zip(col, tv))
                rg = np.zeros(o.mask.shape + (4,), np.uint8)
                rg[o.mask] = col + (255,)
                o.rgba = rg
    o.visible = o.facing and o.box is not None and o.box[2] > 0 and o.box[0] < cam.W and o.box[3] > 0 and o.box[1] < cam.H and o.depth > cam.base[2] * 0 + 0.3
    return o


def place_shaft(G, cam, p):
    """光の筋 (DioramaMesh.Shaft): 出口 (上) = 置き場・dir の向きへ len。画面の四角形 (上の左右・下の左右) と中心の線"""
    t, s = p.get('t', 0.0), p.get('s', 0.0)
    y0 = p['y'] if p.get('abs') else G.gy(t, s) + p.get('y', 0.0)
    top = G.on_path(t, s, y0)
    d = np.array(p.get('dir', [0.35, -1.0, 0.3]), float)
    d = d / np.linalg.norm(d)
    bot = top + d * p.get('len', 12.0)
    ht, hb = p.get('top', 0.8) / 2, p.get('bottom', 2.4) / 2
    r = np.array([1.0, 0.0, 0.0])
    q = [cam.project(top - r * ht), cam.project(top + r * ht), cam.project(bot + r * hb), cam.project(bot - r * hb)]
    ct, cb = cam.project(top), cam.project(bot)
    return dict(name=p.get('name'), quad=[(float(a[0]), float(a[1])) for a in q], top=(float(ct[0]), float(ct[1])), bottom=(float(cb[0]), float(cb[1])),
                depth=float(ct[2]), gain=p.get('gain', 1.0))


def place_fogplane(G, cam, p):
    t, s = p.get('t', 0.0), p.get('s', 0.0)
    y0 = G.gy(t, s) + p.get('y', 0.0)
    pos = G.on_path(t, s, y0)
    w, h = p.get('w', 80.0), p.get('h', 12.0)
    r = yaw_rot(p.get('yaw', 0.0), 1, 0, 0)
    a = cam.project(pos + np.array([0, 0, 0]))
    b = cam.project(pos + np.array([0, h, 0]))
    return dict(name=p.get('name'), rowBase=float(a[1]), rowTop=float(b[1]), depth=float(a[2]), gain=p.get('gain', 1.0), v0=p.get('v0', 0.0))


def render_quad(o, im, corners, cam):
    x0, y0, x1, y1 = o.box
    if not (x1 > -2000 and x0 < cam.W + 2000 and y1 > -3000 and y0 < cam.H + 3000):
        return
    X0, Y0 = int(math.floor(max(x0, -2000))), int(math.floor(max(y0, -3000)))
    X1, Y1 = int(math.ceil(min(x1, cam.W + 2000))) + 1, int(math.ceil(min(y1, cam.H + 3000))) + 1
    if X1 - X0 < 1 or Y1 - Y0 < 1:
        return
    wv, hv = im.size
    # 板の四隅 (左下・左上・右上・右下) ↔ 絵の (0,h)・(0,0)・(w,0)・(w,h)
    dst = [(c[0] - X0, c[1] - Y0) for c in corners]
    src = [(0, hv), (0, 0), (wv, 0), (wv, hv)]
    co = perspective_coeffs(dst, src)
    out = im.transform((X1 - X0, Y1 - Y0), Image.PERSPECTIVE, tuple(co), Image.NEAREST)
    a = np.array(out)
    o.rgba = a
    o.mask = a[..., 3] > 40
    o.origin = (X0, Y0)


def place_frame(L, cam, o, with_image):
    p = o.p
    ph = p.get('phone') if cam.phone else None
    ph = ph or {}
    if ph.get('hide'):   # Diorama.PhoneHidden: スマホでは組まない (額縁も)
        o.note = 'phone-hide'
        return o

    def num(key, d):
        v = ph.get(key, p.get(key, d))
        return float(v)
    depth = num('depth', 8.0)
    aspect = cam.W / cam.H
    x = (num('vx', 0.0) - 0.5) * 2 * cam.tanV * aspect * depth
    y = (num('vy', 0.5) - 0.5) * 2 * cam.tanV * depth
    sc = num('scale', p.get('scale', 1.0) or 1.0)
    roll = math.radians(num('roll', 0.0))
    im, a = art_for(L, o.src)
    if im is None:
        o.note = 'no-art'
        o.depth = depth
        return o
    o.art = a
    w, h = im.size[0] / TPU * sc, im.size[1] / TPU * sc
    flat = is_flat(L, o.src)
    py = 0.0 if flat else 0.5   # flat = 足元の中心が原点 (DioramaMesh.Card)・半立体 = 中心 (pivot 0.5,0.5)
    corners_local = [(-w / 2, -py * h), (-w / 2, (1 - py) * h), (w / 2, (1 - py) * h), (w / 2, -py * h)]
    if p.get('flip'):
        corners_local = [(-cx, cy) for cx, cy in corners_local]
    cs = []
    for cx, cy in corners_local:
        rx = cx * math.cos(roll) - cy * math.sin(roll)
        ry = cx * math.sin(roll) + cy * math.cos(roll)
        X = cam.W * 0.5 + cam.f * (x + rx) / depth
        Y = cam.H * 0.5 - cam.f * (y + ry) / depth
        cs.append((X, Y, depth))
    xs = [c[0] for c in cs]
    ys = [c[1] for c in cs]
    o.box = (min(xs), min(ys), max(xs), max(ys))
    o.depth = depth
    o.wpx = float(o.box[2] - o.box[0])
    o.worldH = h
    o.visible = o.box[2] > 0 and o.box[0] < cam.W and o.box[3] > 0 and o.box[1] < cam.H
    o.note = 'flat' if flat else 'relief'
    if with_image:
        src_im = im
        if p.get('tint') is not None:
            tv = p['tint']
            tv = [tv, tv, tv] if isinstance(tv, (int, float)) else tv
            arr = np.array(src_im).astype(float)
            arr[..., :3] *= np.array(tv[:3])
            src_im = Image.fromarray(arr.clip(0, 255).astype(np.uint8), 'RGBA')
        # 額縁は切り抜いた絵を大きく写すので、描くのは画面の中だけに切る
        big = max(o.box[2] - o.box[0], o.box[3] - o.box[1])
        if big > 6000 or o.src == 'backdropPlain':   # 背景の板 (霧に満ちる) は画の地 (霧の色 × 芯) がそれ = 絵は描かない
            o.mask = None
            return o
        render_quad(o, src_im, [(c[0], c[1], c[2]) for c in cs], cam)
    return o


# ------------------------------------------------------------------ キャラの座席

LEADER = (-5.0, 0.9)
FEET_TARGET = {  # 計画 §2 レーン C の足元の x (PC は画面の px・スマホは ×sf の画面の px)
    'PC': {1: [1374], 2: [1267, 1522], 3: [1180, 1447, 1713], 4: [1126, 1342, 1557, 1772]},
    'PH': {1: [1365], 2: [1261, 1533], 3: [1176, 1432, 1688], 4: [1124, 1332, 1541, 1742]},
}
W5_T = {1: [4.517], 2: [3.2, 7.276], 3: [2.2, 5.831, 9.005], 4: [1.6, 4.492, 6.863, 10.404]}


def enemy_s(phone, n, i):
    sB = 0.1 if (phone or n >= 3) else 0.7
    return -0.5 if i % 2 == 0 else sB


def solve_t(cam, G, s, x_target, lo=-5.0, hi=30.0):
    f = lambda t: cam.project(G.on_path(t, s, 0.0))[0]
    for _ in range(60):
        m = (lo + hi) / 2
        if f(m) < x_target:
            lo = m
        else:
            hi = m
    return (lo + hi) / 2


def seats(cam, G):
    plat = 'PH' if cam.phone else 'PC'
    if cam.name == 'PC21':
        plat = 'PC'
    hx, hy, hd = cam.project(G.on_path(LEADER[0], LEADER[1], 0.0))
    out = {'hero': (float(hx), float(hy), float(hd)), 'enemies': {}}
    for n in (1, 2, 3, 4):
        lst = []
        for i in range(n):
            s = enemy_s(cam.phone, n, i)
            if cam.name == 'PC21':
                t = W5_T[n][i]
            else:
                t = solve_t(cam, G, s, FEET_TARGET[plat][n][i])
            x, y, d = cam.project(G.on_path(t, s, 0.0))
            lst.append(dict(t=round(t, 3), s=s, x=float(x), y=float(y), d=float(d)))
        out['enemies'][n] = lst
    return out


# W5 の撮影 (主人公62) の UI の矩形 (意図の札・帳面) と足元。新しい足元へずらして使う
W5_SHOTS = SP + 'final/shots/'


def w5_intents():
    """{'wolf': [(feetX, feetYtop, [x,y,w,h]), ...], 'ogre': ..., 'quad': ...}"""
    res = {}
    for key, f in (('wolf', 'hero62/PC-S-wolf-1.layout.json'), ('ogre', 'hero62/PC-S-ogre-1.layout.json'), ('quad', 'slice/PC-S-quad-1.layout.json'),
                   ('trio', 'slice/PC-S-trio-1.layout.json')):
        fp = W5_SHOTS + f
        if not os.path.exists(fp):
            continue
        d = json.load(open(fp))
        lst = []
        csc = d['canvas'].get('scale', 1.0)
        for u in d['units']:
            if u['kind'] != 'enemy' or 'intent' not in u:
                continue
            # px・sprite・intent は画面の px (上から)。入れ物の下端 (px[1]+px[3]) = 足元の線、そこから feetOffset (キャンバスの単位) だけ上が足元
            px = u['px']
            feet = (px[0] + px[2] / 2, px[1] + px[3] - u.get('feetOffset', 0.0) * csc)
            lst.append(dict(id=u['id'], feet=feet, intent=u['intent'], sprite=u.get('sprite')))
        res[key] = lst
    return res


# ------------------------------------------------------------------ 規則の計算

def rect_px(box, W, H):
    x0, y0, x1, y1 = box
    return max(0, int(math.floor(x0))), max(0, int(math.floor(y0))), min(W, int(math.ceil(x1))), min(H, int(math.ceil(y1)))


def visible_mask(o, cam, terr_depth_full, W, H):
    """部品の見えている画素 (画面の大きさの bool) と画素の数。地面に隠れた所は数えない"""
    if o.mask is None:
        return None, 0
    X0, Y0 = o.origin
    h, w = o.mask.shape
    xa, ya = max(0, X0), max(0, Y0)
    xb, yb = min(W, X0 + w), min(H, Y0 + h)
    if xb <= xa or yb <= ya:
        return None, 0
    sub = o.mask[ya - Y0:yb - Y0, xa - X0:xb - X0]
    if terr_depth_full is not None:   # 額縁も地面より奥なら隠れる (背景の板)。手前の額縁 (深さ 6〜12) は隠れない
        td = terr_depth_full[ya:yb, xa:xb]
        sub = sub & (o.depth <= td + 0.6)
    full = np.zeros((H, W), bool)
    full[ya:yb, xa:xb] = sub
    return full, int(sub.sum())


def upsample(rc, W, H):
    step = rc['step']
    d = np.repeat(np.repeat(rc['depth'], step, axis=0), step, axis=1)[:H, :W]
    if d.shape != (H, W):   # 画面の高さが 4 の倍数でない (スマホ 886) 時は端の行・列を写して足す
        d = np.pad(d, ((0, H - d.shape[0]), (0, W - d.shape[1])), mode='edge')
    return d


def seat_intrusions(L):
    SeatT0, SeatT1, SeatS0, SeatS1 = -8.5, 13.0, -2.6, 2.8
    out = []
    for i, p in enumerate(L['parts']):
        k = p['kind']
        if k in ('litter', 'slab', 'fog', 'shaft', 'frame'):
            continue
        reach = {'rock': p.get('r', 0.6), 'block': max(p.get('w', 1.6), p.get('d', 1.2)) * 0.5,
                 'tree': p.get('r', 0.55) + p.get('rootLen', 1.6), 'fence': p.get('len', 3.0) * 0.5, 'rig': 1.5}.get(k, 0.3)
        t, s = p.get('t', 0.0), p.get('s', 0.0)
        if t + reach > SeatT0 and t - reach < SeatT1 and s + reach > SeatS0 and s - reach < SeatS1 and not p.get('abs'):
            out.append((i, k, p.get('name') or p.get('src'), t, s))
    return out


def fog_frac(depth, start, end, r):
    s, e = start * r, end * r
    return float(np.clip((depth - s) / (e - s), 0, 1))


# 段2: 試しのビルドで決まった霧 (統合 2026-10-01) = start 14・end 28 (×r)・芯 {t 32.5, s 80, y 1.0, power 200}。段1 の A〜C は --fogvariants で
FOG_VARIANTS = {'R': (14.0, 28.0)}
LOBE = dict(t=32.5, s=80.0, y=1.0, power=200.0, edge=0.3)
if os.environ.get('PLACE_FOGVARIANTS'):
    FOG_VARIANTS.update({'A': (12, 45), 'B': (8, 40), 'C': (12, 60)})


def ground_end(rc, cam, G):
    """地面の奥の端: 各列で、地面に当たる画素のうち真上が地面に当たらない (背景が見える) 物。
    返す: 列ごとの端 (行・深さ・t・s・y・段の名前) と、段ごとの集計"""
    hit = rc['hit']
    ys, xs = rc['ys'], rc['xs']
    edges = []
    for j in range(len(xs)):
        col = hit[:, j]
        for i in range(1, len(ys)):
            if col[i] and not col[i - 1]:
                edges.append(dict(x=int(xs[j]), row=int(ys[i]), depth=float(rc['depth'][i, j]), t=float(rc['t'][i, j]), s=float(rc['s'][i, j]),
                                  y=float(rc['y'][i, j]), slab=G.slabs[rc['slab'][i, j]]['name'] if rc['slab'][i, j] >= 0 else None))
    return edges


def summarize_edges(edges, cam):
    by = {}
    for e in edges:
        by.setdefault(e['slab'], []).append(e)
    out = {}
    for k, lst in by.items():
        d = np.array([e['depth'] for e in lst])
        rows = np.array([e['row'] for e in lst])
        xs = np.array([e['x'] for e in lst])
        s = np.array([e['s'] for e in lst])
        y = np.array([e['y'] for e in lst])
        o = dict(columns=len(lst), x=[int(xs.min()), int(xs.max())], row=[int(rows.min()), int(np.median(rows)), int(rows.max())],
                 depth=[round(float(d.min()), 1), round(float(np.median(d)), 1), round(float(d.max()), 1)],
                 s=[round(float(s.min()), 1), round(float(s.max()), 1)], y=[round(float(y.min()), 2), round(float(y.max()), 2)])
        for v, (st, en) in FOG_VARIANTS.items():
            f = np.clip((d - st * cam.r) / ((en - st) * cam.r), 0, 1)
            o['fog' + v] = [round(float(f.min()), 2), round(float(np.median(f)), 2)]
            o['fog' + v + '_lt0.9'] = int((f < 0.9).sum())
        out[k] = o
    return out


def l8_estimate(frame_px):
    """hd2d-layout-check.py の L8 (額縁が UI と重なる割合 ≤30%) の見込み: W5 の撮影 (主人公62・PC) の UI の矩形に、この額縁の矩形を足して検査を回す。
    敵の意図の札は W5 の足元の位置のまま (新しいカメラで 18〜33px 下がるが、背景の板の下の辺は画面の真ん中の行なので影響は小さい)"""
    import importlib.util
    spec = importlib.util.spec_from_file_location('lc', REPO + '/scripts/hd2d-layout-check.py')
    lc = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(lc)
    out = {}
    for key, f in (('wolf', 'hero62/PC-S-wolf-1.layout.json'), ('ogre', 'hero62/PC-S-ogre-1.layout.json'), ('quad', 'slice/PC-S-quad-1.layout.json'), ('dolls', 'slice/PC-S-dolls-1.layout.json')):
        fp = W5_SHOTS + f
        if not os.path.exists(fp):
            continue
        lay = json.load(open(fp))
        x0, y0, x1, y1 = frame_px
        lay.setdefault('extra', {})['frames'] = [{'name': 'frame-backdrop', 'px': [x0, y0, x1 - x0, y1 - y0], 'depth': 140}]
        V, Wn = lc.check(lay)
        l8 = [v_['msg'] for v_ in V if v_['rule'] == 'L8']
        # 重なりの割合そのもの (check の L8 と同じ矩形の集め方)
        us = [u for u in lc.units(lay) if u.get('alive', True) and u.get('active', True) is not False]
        ui = [u['strip'] for u in us if u.get('kind') == 'enemy' and u.get('strip')] + [u['intent'] for u in us if u.get('kind') == 'enemy' and u.get('intent')]
        ui += [r for _, r in lc.self_rects(lay)] + [r for _, r in lc.doll_tags(lay)] + [h['px'] for h in lay.get('hand', []) if isinstance(h, dict) and h.get('px')]
        tb = (lay.get('anchors') or {}).get('topbar')
        if tb:
            ui.append(tb)
        fr = [x0, y0, x1 - x0, y1 - y0]
        cov = lc.union_area_within(fr, ui) / (fr[2] * fr[3])
        out[key] = dict(overlap=round(cov, 3), violations=l8)
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('layout')
    ap.add_argument('--out', default=os.path.dirname(os.path.abspath(__file__)))
    ap.add_argument('--tag', default=None)
    ap.add_argument('--img', action='store_true')
    ap.add_argument('--fogshow', default='R')
    args = ap.parse_args()
    L = json.load(open(args.layout))
    tag = args.tag or os.path.splitext(os.path.basename(args.layout))[0]
    G = Ground(L)
    res = dict(layout=os.path.relpath(args.layout, REPO) if args.layout.startswith(REPO) else args.layout, parts=len(L['parts']))
    from collections import Counter
    res['byKind'] = dict(Counter(p['kind'] for p in L['parts']))
    res['seatParts'] = seat_intrusions(L)
    intents_w5 = w5_intents()
    cams_ = cams(extra_wide=True)
    per = {}
    placed_all = {}
    for cam in cams_:
        rc = G.raycast(cam, 4)
        W, H = cam.W, cam.H
        tdf = upsample(rc, W, H)
        st = seats(cam, G)
        placed = []
        for i, p in enumerate(L['parts']):
            o = place_part(L, G, cam, i, p, with_image=True)
            if o is not None:
                placed.append(o)
        placed_all[cam.name] = (placed, rc, st)
        info = dict(cam=dict(fov=cam.fov, pitch=cam.pitch, gl=cam.gl, W=W, H=H, eyeY=round(float(cam.base[1]), 3), camZ=round(float(cam.base[2]), 3), r=round(cam.r, 4),
                             horizonRow=round(H * 0.5 - cam.f * math.tan(math.radians(cam.pitch)), 1)))
        info['hero'] = [round(v, 1) for v in st['hero']]
        info['enemies'] = {n: [dict(t=e['t'], s=e['s'], x=round(e['x']), y=round(e['y']), d=round(e['d'], 2)) for e in lst] for n, lst in st['enemies'].items()}
        # 地面の奥の端
        edges = ground_end(rc, cam, G)
        info['groundEnd'] = summarize_edges(edges, cam)
        miss = ~rc['hit']
        # 背景が見える画素 (地面に当たらない) の範囲
        if miss.any():
            ys_m, xs_m = np.nonzero(miss)
            info['backgroundRows'] = [int(rc['ys'][ys_m.min()]), int(rc['ys'][ys_m.max()])]
            info['backgroundFrac'] = round(float(miss.mean()), 4)
        # 部品の表
        rows = []
        for o in placed:
            vm, npx = visible_mask(o, cam, tdf, W, H)
            o_rec = dict(i=o.i, kind=o.kind, name=o.p.get('name'), src=o.src, t=o.p.get('t'), s=o.p.get('s'),
                         box=[round(v) for v in o.box] if o.box else None, depth=round(o.depth, 1) if o.depth is not None else None,
                         wpx=round(o.wpx), visible=bool(o.visible), facing=bool(o.facing), px=npx, note=o.note,
                         fog=round(fog_frac(o.depth, FOG_VARIANTS['R'][0], FOG_VARIANTS['R'][1], cam.r), 2) if o.depth is not None else None)
            rows.append(o_rec)
        info['boxes'] = rows
        per[cam.name] = info
    res['cams'] = per

    # ---------------- 規則ごとの集計 (PC と PH)
    rules = {}
    for cname in ('PC', 'PH'):
        placed, rc, st = placed_all[cname]
        cam = [c for c in cams_ if c.name == cname][0]
        W, H = cam.W, cam.H
        tdf = upsample(rc, W, H)
        sf = cam.canvas_sf
        # 主人公の後ろの窓
        win = (300, 380, 560, 560) if cname == 'PC' else (230, 230, 430, 380)
        hw = []
        for o in placed:
            if o.kind in ('litter',) or o.kind == 'frame':
                continue
            vm, npx = visible_mask(o, cam, tdf, W, H)
            if vm is None:
                continue
            c = int(vm[win[1]:win[3], win[0]:win[2]].sum())
            if c > 0:
                hw.append(dict(i=o.i, kind=o.kind, name=o.p.get('name'), src=o.src, t=o.p.get('t'), s=o.p.get('s'), px=c))
        hw.sort(key=lambda d: -d['px'])
        rules.setdefault('heroWindow', {})[cname] = dict(window=win, total=sum(d['px'] for d in hw), parts=hw)
        # 太い幹 × 1〜2体の敵の足元 ±130px
        thick = []
        feet_x = sorted({round(e['x']) for n in (1, 2) for e in st['enemies'][n]})
        for o in placed:
            if not o.visible or o.box is None:
                continue
            src = o.src or ''
            trunkish = o.kind == 'tree' or any(k in src for k in TRUNK_SRC)
            if not trunkish:
                continue
            cx = (o.box[0] + o.box[2]) / 2
            if o.wpx >= 40:
                for fx in feet_x:
                    if abs(cx - fx) <= 130 or (o.box[0] - 130 <= fx <= o.box[2] + 130 and o.wpx < 400 and o.box[0] <= fx <= o.box[2]):
                        thick.append(dict(i=o.i, src=src, name=o.p.get('name'), cx=round(cx), wpx=round(o.wpx), footX=fx))
        rules.setdefault('thickTrunkNearFeet', {})[cname] = thick
        # 意図の札の箱 (W5 の矩形を新しい足元へずらす。PC だけ = W5 の撮影は PC の値)
        if cname == 'PC':
            inter = []
            for key, lst in intents_w5.items():
                n = {'wolf': 1, 'ogre': 1, 'quad': 4, 'trio': 3}[key]
                for j, u in enumerate(lst):
                    if j >= len(st['enemies'][n]):
                        continue
                    e = st['enemies'][n][j]
                    dx = e['x'] - u['feet'][0]
                    dy = e['y'] - u['feet'][1]
                    ix, iy, iw, ih = u['intent']
                    ib = (int(ix + dx), int(iy + dy), int(ix + dx + iw), int(iy + dy + ih))
                    for o in placed:
                        if o.kind in ('litter',):
                            continue
                        vm, npx = visible_mask(o, cam, tdf, W, H)
                        if vm is None:
                            continue
                        c = int(vm[max(0, ib[1]):max(0, ib[3]), max(0, ib[0]):max(0, ib[2])].sum())
                        if c > 0:
                            inter.append(dict(scene=key, enemy=j, intentBox=ib, i=o.i, kind=o.kind, src=o.src, name=o.p.get('name'), px=c))
            rules['intentBoxes'] = inter
        # 手前の株の先の行: s < −3 の部品の上端の行 ≤ (その x でいちばん低い足の行 + 20) なら違反
        feet_pts = [(st['hero'][0], st['hero'][1], 180)]
        for n in (1, 2, 3, 4):
            for e in st['enemies'][n]:
                feet_pts.append((e['x'], e['y'], 120))
        ft = []
        for o in placed:
            if o.kind == 'frame' or o.box is None or not o.visible:
                continue
            if o.p.get('s', 0.0) >= -3.0 or o.p.get('abs'):   # 空中に掛けた物 (上の角の垂れた葉) は先でなく下端が上にある = 足を隠さない
                continue
            x0, y0, x1, y1 = o.box
            worst = None
            for fx, fy, hwid in feet_pts:
                if x1 >= fx - hwid and x0 <= fx + hwid:
                    if y0 <= fy + 20:
                        worst = max(worst or 0, fy + 20 - y0)
            if worst is not None:
                ft.append(dict(i=o.i, kind=o.kind, src=o.src, name=o.p.get('name'), s=o.p.get('s'), top=round(y0), over=round(worst)))
        rules.setdefault('frontTips', {})[cname] = ft
        # 上の40% の木の面積 (N24)
        topH = int(H * 0.4)
        cov = np.zeros((topH, W), bool)
        for o in placed:
            src = o.src or ''
            treeish = o.kind in ('tree', 'frame') or any(k in src for k in TREEISH_SRC)
            if not treeish:
                continue
            vm, npx = visible_mask(o, cam, tdf, W, H)
            if vm is None:
                continue
            if o.kind == 'frame' and o.p.get('src') == 'backdropPlain':
                continue
            cov |= vm[:topH]
        rules.setdefault('N24', {})[cname] = round(float(cov.mean()), 3)
        # 額縁の下端 (上の角の額縁だけ = 中心の vy > 0.5)
        fr = []
        for o in placed:
            if o.kind != 'frame' or o.box is None:
                continue
            vyc = (o.p.get('phone', {}) if cam.phone else {}).get('vy', o.p.get('vy', 0.5))
            bottom_vy = 1.0 - o.box[3] / H
            fr.append(dict(i=o.i, name=o.p.get('name'), src=o.src, vy=vyc, bottomVy=round(bottom_vy, 3), box=[round(v) for v in o.box], upper=vyc > 0.5))
        rules.setdefault('frames', {})[cname] = fr
        # 段2 の天面の縁が主人公の頭の行 ±30 を主人公の x で横切らないか (計画 3: PC x300〜560・行376〜436／スマホ x230〜430・頭 243±30)
        hx0, hx1, hr0, hr1 = (300, 560, 376, 436) if cname == 'PC' else (230, 430, 213, 273)
        ti2 = [k_ for k_, sl in enumerate(G.slabs) if sl['name'] == 'terrace-2']
        edge_rows = []
        if ti2:
            k2 = ti2[0]
            topm = (rc['slab'] == k2) & (rc['y'] >= G.slabs[k2]['top'] - 0.02)
            cols = [j for j, x_ in enumerate(rc['xs']) if hx0 <= x_ <= hx1]
            for j in cols:
                c_ = topm[:, j]
                for i_ in range(1, len(c_)):
                    if c_[i_] != c_[i_ - 1]:
                        dd = float(rc['depth'][i_, j] if c_[i_] else rc['depth'][i_ - 1, j])
                        edge_rows.append((int(rc['ys'][i_]), round(fog_frac(dd, FOG_VARIANTS['R'][0], FOG_VARIANTS['R'][1], cam.r), 2)))
        inb = [e for e in edge_rows if hr0 <= e[0] <= hr1]
        rules.setdefault('terrace2EdgeInHead', {})[cname] = dict(rows=[min(e[0] for e in edge_rows), max(e[0] for e in edge_rows)] if edge_rows else None,
                                                               inBand=len(inb), inBandFogLt09=sum(1 for e in inb if e[1] < 0.9),
                                                               fogInBand=[min(e[1] for e in inb), max(e[1] for e in inb)] if inb else None)
        # 上の真ん中 (PC x700〜1220・行0〜300／スマホ 行0〜130) を暗い形 (背景の板を除く部品) が覆う割合 (N6a は中央値なので 0.5 を超えたい)
        tm = (700, 0, 1220, 300) if cname == 'PC' else (700, 0, 1220, 130)
        cov2 = np.zeros((tm[3] - tm[1], tm[2] - tm[0]), bool)
        who = []
        for o in placed:
            if o.kind == 'frame' and o.src == 'backdropPlain':
                continue
            vm, npx = visible_mask(o, cam, tdf, W, H)
            if vm is None:
                continue
            sub = vm[tm[1]:tm[3], tm[0]:tm[2]]
            if sub.any():
                who.append(dict(i=o.i, src=o.src, kind=o.kind, name=o.p.get('name'), px=int(sub.sum())))
            cov2 |= sub
        who.sort(key=lambda d: -d['px'])
        rules.setdefault('topMid', {})[cname] = dict(box=tm, cover=round(float(cov2.mean()), 3), parts=who[:14])
        # 中央の列の石の塊 (PC x700〜1220・行270〜370／スマホ 行140〜200 に置かない)
        cb = (700, 270, 1220, 370) if cname == 'PC' else (700, 140, 1220, 200)
        bl = []
        for o in placed:
            if o.kind not in ('block',):
                continue
            vm, npx = visible_mask(o, cam, tdf, W, H)
            if vm is None:
                continue
            c = int(vm[cb[1]:cb[3], cb[0]:cb[2]].sum())
            if c > 0:
                bl.append(dict(i=o.i, t=o.p.get('t'), s=o.p.get('s'), px=c))
        rules.setdefault('blocksCenter', {})[cname] = bl
        # 見える岩 (岩・大岩・石の塊) の数: 座席の奥 (s 3〜10) と全体
        rk = []
        for o in placed:
            src = o.src or ''
            isrock = o.kind in ('rock', 'block') or 'rockBig' in src
            if not isrock or not o.visible:
                continue
            vm, npx = visible_mask(o, cam, tdf, W, H)
            if npx < 150:
                continue
            rk.append(dict(i=o.i, kind=o.kind, src=o.src, t=o.p.get('t'), s=o.p.get('s'), px=npx))
        rules.setdefault('rocks', {})[cname] = dict(seatBack=[r_ for r_ in rk if 3.0 <= (r_['s'] or 0) <= 10.0], all=len(rk))
        # 霧の帯を縦に横切る幹 (N7 の見込み: 行 245〜405 を通る幹・枝の x と幅)
        band = (245, 405) if cname == 'PC' else (95, 215)
        tr = []
        for o in placed:
            src = o.src or ''
            if not (o.kind == 'tree' or any(k in src for k in ('trunk', 'Trunk', 'pine', 'Pine'))):
                continue
            vm, npx = visible_mask(o, cam, tdf, W, H)
            if vm is None:
                continue
            sub = vm[band[0]:band[1]]
            colcov = sub.mean(axis=0)
            xs_ = np.nonzero(colcov > 0.6)[0]
            if len(xs_) == 0:
                continue
            tr.append(dict(i=o.i, src=src, name=o.p.get('name'), x=[int(xs_.min()), int(xs_.max())], w=int(len(xs_)), depth=round(o.depth, 1),
                           fog=round(fog_frac(o.depth, FOG_VARIANTS['R'][0], FOG_VARIANTS['R'][1], cam.r), 2), tint=o.p.get('tint')))
        tr.sort(key=lambda d: d['x'][0])
        rules.setdefault('bandTrunks', {})[cname] = tr
        # 光の筋
        sh = []
        for p in L['parts']:
            if p['kind'] == 'shaft':
                sh.append(place_shaft(G, cam, p))
        rules.setdefault('shafts', {})[cname] = sh
        fp_ = []
        for p in L['parts']:
            if p['kind'] == 'fog':
                fp_.append(place_fogplane(G, cam, p))
        rules.setdefault('fogPlanes', {})[cname] = fp_
    # 映らない部品 (PC 16:9・PC 21:9・スマホ のどれにも)
    vis = {}
    for cname in ('PC', 'PC21', 'PH'):
        for rrow in per[cname]['boxes']:
            vis.setdefault(rrow['i'], []).append(rrow['visible'] and rrow['facing'])
    off = [i for i, v in vis.items() if not any(v)]
    rules['offscreenParts'] = dict(count=len(off), byKind=dict(Counter(L['parts'][i]['kind'] for i in off)), idx=off)
    # 背景の板 (額縁 src backdropPlain): 画面の覆いと、地面に当たらない画素 (背景が見える所) を全部覆うか・UI との重なり (L8 の見込み)
    bd = {}
    for cam in cams_:
        placed, rc, st = placed_all[cam.name]
        for o in placed:
            if o.kind == 'frame' and o.src == 'backdropPlain' and o.box is not None:
                x0, y0, x1, y1 = o.box
                miss = ~rc['hit']
                X, Y = np.meshgrid(rc['xs'], rc['ys'])
                inside = (X >= x0) & (X <= x1) & (Y >= y0) & (Y <= y1)
                unc = int((miss & ~inside).sum())
                cl = [max(0, x0), max(0, y0), min(cam.W, x1), min(cam.H, y1)]
                rec = dict(box=[round(v, 1) for v in o.box], clamped=[round(v) for v in cl], screenFrac=round((cl[2] - cl[0]) * (cl[3] - cl[1]) / (cam.W * cam.H), 3),
                           marginX=[round(-x0), round(x1 - cam.W)], topAbove=round(-y0), bottomRow=round(y1),
                           uncoveredBackgroundPx=unc * rc['step'] ** 2, depth=o.depth, widthUnits=round((x1 - x0) * o.depth / cam.f, 1))
                if cam.name == 'PC':
                    rec['L8'] = l8_estimate(cl)
                bd[cam.name] = rec
    rules['backdrop'] = bd
    res['rules'] = rules
    os.makedirs(args.out, exist_ok=True)
    jp = os.path.join(args.out, tag + '.json')
    json.dump(res, open(jp, 'w'), ensure_ascii=False, indent=1, default=float)
    if args.img:
        for cname, scene in (('PC', 'wolf'), ('PH', 'wolf'), ('PC', 'quad'), ('PC', 'dolls'), ('PH', 'quad')):
            placed, rc, st = placed_all[cname]
            cam = [c for c in cams_ if c.name == cname][0]
            nm = '%s-%s.png' % (tag, cname) if scene == 'wolf' else '%s-%s-%s.png' % (tag, cname, scene)
            draw(cam, G, L, placed, rc, st, per[cname], os.path.join(args.out, nm), args.fogshow, intents_w5, scene)
    open(os.path.join(args.out, tag + '.md'), 'w').write(summary_md(res, tag))
    print(jp)
    return res


def summary_md(res, tag):
    """人が読む要約 (1ページ)"""
    r = res['rules']
    out = ['# 配置の計算: %s' % tag, '', '設計図 `%s`・部品 %d (%s)' % (res['layout'], res['parts'], ', '.join('%s %d' % kv for kv in sorted(res['byKind'].items()))), '',
           '霧の変種 (start・end ×r): ' + '・'.join('%s %g/%g' % (k, a, b) for k, (a, b) in FOG_VARIANTS.items()), '']
    out.append('| | PC 16:9 | スマホ 1920×886 | PC 21:9 |')
    out.append('|---|---|---|---|')
    cs = res['cams']
    out.append('| 目の高さ・地平線の行 | %s | %s | %s |' % tuple('%.2f・%d' % (cs[c]['cam']['eyeY'], cs[c]['cam']['horizonRow']) for c in ('PC', 'PH', 'PC21')))
    out.append('| 背景が見える画素 (地面に当たらない)・行 | %s | %s | %s |' % tuple('%.1f%%・%s' % (100 * cs[c].get('backgroundFrac', 0), cs[c].get('backgroundRows')) for c in ('PC', 'PH', 'PC21')))
    for c in ('PC', 'PH', 'PC21'):
        for k, v in cs[c]['groundEnd'].items():
            fz = '・'.join('%s %s (0.9 未満 %d 列)' % (fv, v['fog' + fv], v['fog' + fv + '_lt0.9']) for fv in FOG_VARIANTS)
            out.append('| 地面の奥の端 %s (%s) | 行 %s・深さ %s・s %s・y %s | 霧 %s | |' % (c, k, v['row'], v['depth'], v['s'], v['y'], fz))
    bd = r.get('backdrop', {})
    if bd:
        out.append('')
        out.append('背景の板: ' + ' / '.join('%s 画面の %.0f%%・左右の余り %s px・覆えない背景 %d px' % (c, 100 * v['screenFrac'], v['marginX'], v['uncoveredBackgroundPx']) for c, v in bd.items()))
        if 'L8' in bd.get('PC', {}):
            out.append('L8 (額縁と UI の重なり ≤30%) の見込み: ' + '・'.join('%s %.1f%%' % (k, 100 * v['overlap']) for k, v in bd['PC']['L8'].items()))
    out.append('')
    out.append('規則 | PC | スマホ')
    out.append('---|---|---')
    out.append('主人公の後ろの窓の画素 (目標 0・200 以下なら可) | %d | %d' % (r['heroWindow']['PC']['total'], r['heroWindow']['PH']['total']))
    out.append('太い幹 × 1〜2体の足元 ±130px | %d | %d' % (len(r['thickTrunkNearFeet']['PC']), len(r['thickTrunkNearFeet']['PH'])))
    out.append('意図の札に掛かる部品 (PC・W5 の札を新しい足元へ) | %d | —' % len(r.get('intentBoxes', [])))
    out.append('座席の帯の部品 (seatParts) | %d | ' % len(res['seatParts']))
    out.append('手前の部品の先が足の行 +20 より上 | %d | %d' % (len(r['frontTips']['PC']), len(r['frontTips']['PH'])))
    out.append('N24 上の40%% の木と額縁の面積 (目標 PC 45〜65%%・スマホ 35〜60%%) | %.0f%% | %.0f%%' % (100 * r['N24']['PC'], 100 * r['N24']['PH']))
    out.append('どの画面にも映らない部品 | %d (%s) | ' % (r['offscreenParts']['count'], ', '.join('%s %d' % kv for kv in r['offscreenParts']['byKind'].items())))
    out.append('上の真ん中 (PC x700〜1220・行0〜300／スマホ 行0〜130) を部品が覆う割合 (目標 0.5 超) | %.0f%% | %.0f%%' % (100 * r['topMid']['PC']['cover'], 100 * r['topMid']['PH']['cover']))
    out.append('中央の列の石の塊 (PC 行270〜370／スマホ 行140〜200) | %d | %d' % (len(r['blocksCenter']['PC']), len(r['blocksCenter']['PH'])))
    out.append('見える岩 (座席の奥 s3〜10／全体) | %d／%d | %d／%d' % (len(r['rocks']['PC']['seatBack']), r['rocks']['PC']['all'], len(r['rocks']['PH']['seatBack']), r['rocks']['PH']['all']))
    out.append('段2 の天面の縁が主人公の頭の行 ±30 に入る列 (目標 0。うち霧 0.9 未満) | %d (%d・霧 %s) | %d (%d・霧 %s)' % (
        r['terrace2EdgeInHead']['PC']['inBand'], r['terrace2EdgeInHead']['PC']['inBandFogLt09'], r['terrace2EdgeInHead']['PC']['fogInBand'],
        r['terrace2EdgeInHead']['PH']['inBand'], r['terrace2EdgeInHead']['PH']['inBandFogLt09'], r['terrace2EdgeInHead']['PH']['fogInBand']))
    out.append('霧の帯を横切る幹 (N7 の見込み。幅>=12px) | %d | %d' % (sum(1 for d in r['bandTrunks']['PC'] if d['w'] >= 12), sum(1 for d in r['bandTrunks']['PH'] if d['w'] >= 12)))
    return '\n'.join(out) + '\n'


# ------------------------------------------------------------------ 光なしの構図の画

def draw(cam, G, L, placed, rc, st, info, path, fogv, intents_w5, scene='wolf'):
    W, H = cam.W, cam.H
    step = rc['step']
    st_, en_ = FOG_VARIANTS.get(fogv, FOG_VARIANTS['R'])
    fs, fe = st_ * cam.r, en_ * cam.r
    # 背景 (地面なし) = 背景の板が霧に満ちた色 (芯の向きで明るい)。大ざっぱ: 霧の色 × (0.35 + 0.65·lobe)
    dirs, X, Y = cam.ray_dirs(rc['xs'], rc['ys'])
    lobeP = G.on_path(LOBE['t'], LOBE['s'], LOBE['y'])
    l = (lobeP - cam.base) / np.linalg.norm(lobeP - cam.base)
    lobe = np.clip(dirs @ l, 0, 1) ** LOBE['power']
    fogcol = np.array([150, 158, 172])
    base_bg = fogcol * (0.35 + 0.65 * lobe)[..., None]
    dep = rc['depth']
    f = np.clip((dep - fs) / (fe - fs), 0, 1)
    # 地面の色: 段ごとに少し変える (座席の帯 = 土、段 = 岩)
    slab_col = {'front-step': (40, 52, 44), 'seat-band': (70, 66, 58), 'terrace-1': (62, 70, 74), 'terrace-2': (70, 78, 84), 'terrace-3': (80, 88, 96)}
    gcol = np.zeros(dep.shape + (3,))
    for i, sl in enumerate(G.slabs):
        m = rc['slab'] == i
        gcol[m] = slab_col.get(sl['name'], (70, 70, 70))
    # 段の前の面 (崖) は暗く: y が天面より 0.05 以上低い所
    tops = np.array([sl['top'] for sl in G.slabs])
    wall = (rc['slab'] >= 0) & (rc['y'] < tops[np.clip(rc['slab'], 0, None)] - 0.05)
    gcol[wall] *= 0.55
    fogc = fogcol * (0.35 + 0.65 * lobe)[..., None]
    col = np.where(rc['hit'][..., None], gcol * (1 - f[..., None]) + fogc * f[..., None], base_bg)
    colf = np.repeat(np.repeat(col, step, 0), step, 1)[:H, :W]
    if colf.shape[:2] != (H, W):
        colf = np.pad(colf, ((0, H - colf.shape[0]), (0, W - colf.shape[1]), (0, 0)), mode='edge')
    img = Image.fromarray(colf.clip(0, 255).astype(np.uint8)).convert('RGBA')
    tdf = upsample(rc, W, H)
    # 部品 (奥から手前へ)。霧は部品の深さで同じ式
    order = sorted([o for o in placed if o.rgba is not None and o.facing], key=lambda o: -(o.depth or 0))
    for o in order:
        a = o.rgba.copy()
        ff = float(np.clip(((o.depth or 0) - fs) / (fe - fs), 0, 1))
        a3 = a[..., :3].astype(float)
        a3 = a3 * (1 - ff) + np.array([150, 158, 172]) * 0.7 * ff
        a[..., :3] = a3.clip(0, 255).astype(np.uint8)
        # 地面に隠れる所を消す
        X0, Y0 = o.origin
        h, w = a.shape[:2]
        xa, ya, xb, yb = max(0, X0), max(0, Y0), min(W, X0 + w), min(H, Y0 + h)
        if xb <= xa or yb <= ya:
            continue
        sub = a[ya - Y0:yb - Y0, xa - X0:xb - X0].copy()
        occl = (o.depth > tdf[ya:yb, xa:xb] + 0.6)
        sub[occl, 3] = 0
        sub[sub[..., 3] <= 40, 3] = 0
        sub[sub[..., 3] > 40, 3] = 255
        img.alpha_composite(Image.fromarray(sub, 'RGBA'), (xa, ya))
    # 光の筋 (半透明の明るい四角形。出口 = 上)
    ov = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    od = ImageDraw.Draw(ov)
    for p in L['parts']:
        if p['kind'] == 'shaft':
            sh = place_shaft(G, cam, p)
            od.polygon(sh['quad'], fill=(200, 225, 235, int(55 * float(sh['gain']))))
    img.alpha_composite(ov)
    d = ImageDraw.Draw(img)
    # キャラ: W5 の unitsonly の絵 (マゼンタ抜き) を、試しのビルドの撮影 (新しいカメラ) の絵の矩形へ下端の中央をそろえて置く
    try:
        W5U = {('PC', 'wolf'): 'hero62/PC-S-wolf-unitsonly-1', ('PH', 'wolf'): 'hero62/PH-S-wolf-unitsonly-1',
               ('PC', 'quad'): 'slice/PC-S-quad-unitsonly-1', ('PC', 'dolls'): 'slice/PC-S-dolls-unitsonly-1', ('PH', 'quad'): 'slice/PH-S-quad-unitsonly-1'}
        NEWL = {('PC', 'wolf'): 'trial2/T-E-wolf-1', ('PH', 'wolf'): 'trial2/T-E-wolf-ph-1', ('PC', 'quad'): 'trial2/T-E-quad-1',
                ('PC', 'dolls'): 'trial/T-A-dolls-1', ('PH', 'quad'): 'trial/T-A-quad-ph-1'}
        key = ('PH' if cam.phone else 'PC', scene)
        u = np.array(Image.open(W5_SHOTS + W5U[key] + '.png').convert('RGB')).astype(int)
        keym = (np.abs(u - np.array([255, 0, 255])).sum(2) > 60)
        ua = Image.fromarray(np.dstack([u, keym * 255]).astype(np.uint8), 'RGBA')
        lay_old = json.load(open(W5_SHOTS + W5U[key] + '.layout.json'))
        lay_new = json.load(open(SP + 'r2/shots/' + NEWL[key] + '.layout.json'))
        for uo, un in zip(lay_old['units'], lay_new['units']):
            sx, sy, sw, sh = uo['sprite']
            nx, ny, nw, nh = un['sprite']
            crop = ua.crop((int(sx), int(sy), int(sx + sw), int(sy + sh)))
            img.alpha_composite(crop, (int(nx + nw / 2 - sw / 2), int(ny + nh - sh)))
    except Exception as ex:
        ImageDraw.Draw(img).text((20, 20), 'キャラの重ねに失敗: %s' % ex, fill=(255, 80, 80), font=font(20))
    d = ImageDraw.Draw(img)
    # 目印: 地平線・主人公の後ろの窓・足元・意図の札
    hr = info['cam']['horizonRow']
    d.line([(0, hr), (W, hr)], fill=(255, 230, 0, 200), width=1)
    win = (300, 380, 560, 560) if not cam.phone else (230, 230, 430, 380)
    d.rectangle(win, outline=(255, 90, 90), width=2)
    tm = (700, 0, 1220, 300) if not cam.phone else (700, 0, 1220, 130)
    d.rectangle(tm, outline=(90, 255, 255), width=1)
    cb = (700, 270, 1220, 370) if not cam.phone else (700, 140, 1220, 200)
    d.rectangle(cb, outline=(255, 255, 255), width=1)
    for n in (1, 2):
        for e in st['enemies'][n]:
            d.line([(e['x'] - 130, 8 + 6 * n), (e['x'] + 130, 8 + 6 * n)], fill=(255, 120, 60), width=3)
    for n in ((1, 2) if scene == 'wolf' else ((4,) if scene == 'quad' else (1,))):
        for e in st['enemies'][n]:
            d.ellipse([e['x'] - 5, e['y'] - 5, e['x'] + 5, e['y'] + 5], outline=(90, 200, 255), width=2)
    hx, hy = st['hero'][0], st['hero'][1]
    d.ellipse([hx - 6, hy - 6, hx + 6, hy + 6], outline=(255, 160, 60), width=2)
    skey = {'wolf': ('wolf', 1), 'quad': ('quad', 4), 'dolls': ('wolf', 1)}[scene]
    if not cam.phone and skey[0] in intents_w5:
        for j, u in enumerate(intents_w5[skey[0]]):
            e = st['enemies'][skey[1]][j]
            dx, dy = e['x'] - u['feet'][0], e['y'] - u['feet'][1]
            ix, iy, iw, ih = u['intent']
            d.rectangle([ix + dx, iy + dy, ix + dx + iw, iy + dy + ih], outline=(255, 210, 90), width=2)
    for o in placed:
        if o.kind == 'frame' and o.src == 'backdropPlain' and o.box is not None:
            yb = o.box[3]
            d.line([(0, yb), (W, yb)], fill=(120, 255, 160), width=2)
            d.text((W - 520, yb + 4), '背景の板の下の辺 (深さ %.0f・幅 %.0f unit)' % (o.depth, (o.box[2] - o.box[0]) * o.depth / cam.f), fill=(120, 255, 160), font=font(20))
    # 部品の名札 (櫓・柵・背景の板)
    for o in placed:
        if o.kind in ('rig', 'fence') and o.box is not None:
            x0, y0, x1, y1 = o.box
            d.rectangle([x0, y0, x1, y1], outline=(255, 120, 255), width=2)
            d.text((x0, max(0, y0 - 22)), '%s #%d s%.1f' % (o.kind, o.i, o.p.get('s', 0)), fill=(255, 160, 255), font=font(18))
    lbl = '%s  %s  %g°・%g°・足元 %.3f  目の高さ %.2f  地平線 行%d  霧 %s (start %d・end %d ×r%.3f)' % (
        os.path.basename(path), cam.name, cam.fov, cam.pitch, cam.gl, cam.base[1], hr, fogv, st_, en_, cam.r)
    d.rectangle([0, H - 34, W, H], fill=(0, 0, 0, 180))
    d.text((10, H - 30), lbl, fill=(255, 255, 255), font=font(20))
    img.convert('RGB').save(path)


if __name__ == '__main__':
    main()

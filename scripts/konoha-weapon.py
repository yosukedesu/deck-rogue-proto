#!/usr/bin/env python3
"""このはの武器レイヤー合成 (2026-09-11。2026-09-09 の斧の手順を再現可能にした)。

  mask    <chibi.png> <outdir>        武器の範囲 (頭の箱＋柄の帯＋手元) のマスクと武器レイヤー・握りの座標を出す
  strip   <chibi.png> <weapon.png> <out.png>   武器の画素を消して体の穴を近傍色で埋めた「体だけ」の参考画像を作る
  compose <bodyframes_prefix> <weapon.png> <anchor.json> <kp.json> <out_prefix>
          [--canvas 96|128x96] [--angles a,b,c] [--extra-idle body.png] [--palette ref.png] [--place identity|hand] [--align body_ref.png] [--grips "x,y;x,y;..."]
          place=identity (既定) は元絵の位置に貼る (待機・被弾・防御)。place=hand は RIGHT ARM (柄を持つ手) の関節へ握りを合わせる (攻撃)
          体のコマ (animate-with-skeleton の出力) に武器を握りの位置へ回して貼る。パレットは元絵に吸着・接地行を揃える
武器の範囲は元絵ごとに合わせる (ZONE)。"""
import sys, json, math
from PIL import Image

def load(p): return Image.open(p).convert('RGBA')

# 武器の範囲 (2026-09-11 konoha_final.png): 頭の箱・柄の帯 (握りの線)・手元の光
ZONE = {'head': (38, 63, 2, 34), 'haft': ((44, 22), (6, 46), 2.6), 'hand': (2, 12, 40, 49), 'grip': (17, 42)}

def in_zone(x, y):
    hx0, hx1, hy0, hy1 = ZONE['head']
    if hx0 <= x <= hx1 and hy0 <= y <= hy1: return True
    (ax, ay), (bx, by), w = ZONE['haft']
    vx, vy = bx - ax, by - ay; L = math.hypot(vx, vy)
    t = max(0.0, min(1.0, ((x - ax) * vx + (y - ay) * vy) / (L * L)))
    if math.hypot(x - (ax + vx * t), y - (ay + vy * t)) <= w: return True
    x0, x1, y0, y1 = ZONE['hand']
    return x0 <= x <= x1 and y0 <= y <= y1

def is_body_color(rgb):
    r, g, b = rgb
    if g > r and g > b: return True                        # 外套の緑
    if r > 200 and g > 170 and b > 140: return True        # 肌
    if r > 140 and 55 < g < 175 and b < 120 and r - g > 60 and g - b > 5: return True   # 首巻きの橙 (明るい橙 234,152,76 も。真鍮 200,160,70 は r-g=40 で武器)
    if r > g and b > g and r < 160 and r > 60: return True # 髪 (暗い赤紫)
    if r > 190 and g > 170 and b > 120 and r - b < 80: return True   # クリームのシャツ
    return False

def cmd_mask(src, outdir):
    im = load(src); w, h = im.size; ip = im.load()
    mask = Image.new('L', (w, h), 0); weapon = Image.new('RGBA', (w, h), (0, 0, 0, 0)); mp, wp = mask.load(), weapon.load()
    for y in range(h):
        for x in range(w):
            if not in_zone(x, y): continue
            mp[x, y] = 255
            r, g, b, a = ip[x, y]
            if a == 0: continue
            hx0, hx1, hy0, hy1 = ZONE['head']; x0, x1, y0, y1 = ZONE['hand']
            sure = (x >= hx0 + 7 and hy0 <= y <= hy1) or (x0 <= x <= x1 and y0 <= y <= y1)   # 頭の箱の右側と手元は体に掛からない = 色を見ずに武器
            if sure or not is_body_color((r, g, b)): wp[x, y] = (r, g, b, a)
    for _ in range(2):   # 穴埋め: 周囲4方向の3つ以上が武器なら武器 (輪郭の暗い線を拾う)
        add = []
        for y in range(1, h - 1):
            for x in range(1, w - 1):
                if wp[x, y][3] or ip[x, y][3] == 0 or mp[x, y] == 0: continue
                if sum(1 for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)) if wp[x + dx, y + dy][3]) >= 3: add.append((x, y))
        for x, y in add: wp[x, y] = ip[x, y]
    # 柄の補完: 線に沿って 1.1px 以内で透明なら柄の色 (レイヤー内の柄の最頻色) で描く
    from collections import Counter
    (ax, ay), (bx, by), _ = ZONE['haft']
    haft_cols = Counter()
    for y in range(h):
        for x in range(w):
            if not wp[x, y][3]: continue
            vx, vy = bx - ax, by - ay; L = math.hypot(vx, vy); t = max(0.0, min(1.0, ((x - ax) * vx + (y - ay) * vy) / (L * L)))
            if math.hypot(x - (ax + vx * t), y - (ay + vy * t)) <= 1.2 and 0.15 < t < 0.9: haft_cols[wp[x, y][:3]] += 1
    hc = haft_cols.most_common(1)[0][0] if haft_cols else (74, 52, 36)
    for y in range(h):
        for x in range(w):
            if wp[x, y][3] or ip[x, y][3] == 0: continue
            vx, vy = bx - ax, by - ay; L = math.hypot(vx, vy); t = max(0.0, min(1.0, ((x - ax) * vx + (y - ay) * vy) / (L * L)))
            if math.hypot(x - (ax + vx * t), y - (ay + vy * t)) <= 1.1 and 0.05 < t < 0.97: wp[x, y] = (hc[0], hc[1], hc[2], 255)
    mask.save(f'{outdir}/weapon_mask.png'); weapon.save(f'{outdir}/weapon_layer.png')
    json.dump({'grip': list(ZONE['grip'])}, open(f'{outdir}/weapon_anchor.json', 'w'))
    print('weapon px =', sum(1 for y in range(h) for x in range(w) if wp[x, y][3]))

def cmd_strip(src, weapon_png, out):
    """武器レイヤーの画素を元絵から消し、体の内側にできた穴を近傍の最頻色で埋める (inpaint は髪を生やすので使わない)"""
    from collections import Counter
    im = load(src); wl = load(weapon_png); w, h = im.size; ip, wp = im.load(), wl.load()
    for y in range(h):
        for x in range(w):
            if wp[x, y][3]: ip[x, y] = (0, 0, 0, 0)
    for _ in range(3):
        fills = []
        for y in range(1, h - 1):
            for x in range(1, w - 1):
                if ip[x, y][3] or not wp[x, y][3]: continue
                nb = [ip[x + dx, y + dy] for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)) if ip[x + dx, y + dy][3] and is_body_color(ip[x + dx, y + dy][:3])]
                if len(nb) >= 3: fills.append((x, y, Counter(c[:3] for c in nb).most_common(1)[0][0]))
        for x, y, c in fills: ip[x, y] = (c[0], c[1], c[2], 255)
    im.save(out); print('strip ->', out, 'filled', sum(1 for y in range(h) for x in range(w) if wp[x, y][3] and ip[x, y][3]))

def palette_of(im): return sorted({px[:3] for px in im.getdata() if px[3] > 0})

def snap(im, pal):
    out = im.copy(); p = out.load(); w, h = out.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = p[x, y]
            if a == 0: continue
            best = min(pal, key=lambda c: (c[0] - r) ** 2 + (c[1] - g) ** 2 + (c[2] - b) ** 2)
            p[x, y] = (best[0], best[1], best[2], 255 if a > 127 else 0)
    return out

def best_shift(ref, im, rng=6):
    """ref と im の不透明の重なりが最大になる (dx,dy)。骨格生成のコマは元絵から数px ずれるので合わせる"""
    rp, ip = ref.load(), im.load(); w, h = ref.size
    ref_set = {(x, y) for y in range(h) for x in range(w) if rp[x, y][3] > 127}
    im_pts = [(x, y) for y in range(h) for x in range(w) if ip[x, y][3] > 127]
    best = (0, 0); bestn = -1
    for dx in range(-rng, rng + 1):
        for dy in range(-rng, rng + 1):
            n = sum(1 for (x, y) in im_pts if (x + dx, y + dy) in ref_set)
            if n > bestn: bestn, best = n, (dx, dy)
    return best

def bottom_row(im):
    p = im.load(); w, h = im.size
    for y in range(h - 1, -1, -1):
        if any(p[x, y][3] > 127 for x in range(w)): return y
    return h - 1

def cmd_compose(prefix, weapon_png, anchor_json, kp_json, out_prefix, opts):
    cv = str(opts.get('canvas', '64')).lower().split('x'); cw, chh = int(cv[0]), int(cv[-1])   # "96" or "128x96" (StageUnit は幅・高さ別々に拡大率を持つ)
    angles = [float(a) for a in opts.get('angles', '0,0,0').split(',')]
    pal = palette_of(load(opts['palette'])) if opts.get('palette') else None
    weapon = load(weapon_png); grip0 = json.load(open(anchor_json))['grip']; frames = json.load(open(kp_json))
    # --grips "x,y;x,y;..." コマごとの握り (振り上げは柄の端を握る = 柄が顔を横切らない)
    grips = [tuple(int(v) for v in g.split(',')) for g in opts['grips'].split(';')] if opts.get('grips') else None
    bodies = []
    for i in range(len(frames)):
        try: bodies.append(load(f'{prefix}_{i}.png'))
        except FileNotFoundError: break
    if opts.get('extra-idle'):
        bodies.append(load(opts['extra-idle'])); frames = frames + [frames[-1]]; angles = angles + [0.0]
    ref = load(opts['align']) if opts.get('align') else None
    base_bottom = bottom_row(ref) if ref else bottom_row(bodies[0]); off = ((cw - 64) // 2, chh - 64)
    for i, body in enumerate(bodies):
        if ref:
            sx, sy = best_shift(ref, body); dy = sy
            b2 = Image.new('RGBA', body.size, (0, 0, 0, 0)); b2.paste(body, (sx, sy))
        else:
            dy = base_bottom - bottom_row(body)
            b2 = Image.new('RGBA', body.size, (0, 0, 0, 0)); b2.paste(body, (0, dy))
        out = Image.new('RGBA', (cw, chh), (0, 0, 0, 0)); out.paste(b2, off)
        pts = {p['label']: (p['x'] * 64, p['y'] * 64) for p in frames[i]}
        ra = pts.get('RIGHT ARM')
        if opts.get('place', 'identity') == 'hand' and ra: hx, hy = ra[0] + (sx if ref else 0), ra[1] + dy
        else: hx, hy = grip[0], grip[1] + dy
        grip = grips[i] if grips and i < len(grips) else grip0
        if not (opts.get('place', 'identity') == 'hand' and ra): hx, hy = grip[0], grip[1] + dy
        big = Image.new('RGBA', (128, 128), (0, 0, 0, 0)); big.paste(weapon, (32, 32))
        ang = angles[i] if i < len(angles) else 0.0
        rot = big.rotate(ang, resample=Image.NEAREST, center=(grip[0] + 32, grip[1] + 32))
        tx, ty = int(round(hx + off[0] - (grip[0] + 32))), int(round(hy + off[1] - (grip[1] + 32)))
        layer = Image.new('RGBA', (cw, chh), (0, 0, 0, 0)); layer.paste(rot, (tx, ty))
        out = Image.alpha_composite(out, layer)
        if pal: out = snap(out, pal)
        out.save(f'{out_prefix}_{i}.png'); print(f'{out_prefix}_{i}.png hand=({hx:.1f},{hy:.1f}) angle={ang}')

if __name__ == '__main__':
    a = sys.argv[1:]
    if not a: print(__doc__); sys.exit(1)
    cmd = a[0]; pos = [x for x in a[1:] if not x.startswith('--')]
    opts = {}
    for i, x in enumerate(a):
        if x.startswith('--'): opts[x[2:]] = a[i + 1] if i + 1 < len(a) and not a[i + 1].startswith('--') else '1'
    if cmd == 'mask': cmd_mask(pos[0], pos[1])
    elif cmd == 'strip': cmd_strip(pos[0], pos[1], pos[2])
    elif cmd == 'compose': cmd_compose(pos[0], pos[1], pos[2], pos[3], pos[4], opts)

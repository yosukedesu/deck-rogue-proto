#!/usr/bin/env python3
"""60fps で区間を細かく測る: 矩形の明るさ (full/top/mid/floor)・暖色の割合・背景の位相相関によるカメラのずれ (dx,dy)
(HD-2D 段2 レーン F。~/.cache/deck-rogue/hd2d-stage2/motion/tools/fine.py の写し。本家の動画の置き場は決め打ちしない = 動画は引数)。

使い方:
  python3 scripts/hd2d-motion/fine.py <動画 or 連番の画の型> <開始秒> <長さ秒> [見出し] [--region 名前=x0,y0,x1,y1 …] [--csv 出力.csv]
    動画は ffmpeg で 60fps・384×216 に落とす (元の fine.py と同じ)。うちの撮影の連写 (pshots の <名前>-<n>.png) は
    先に scripts/hd2d-motion/frames2mp4.sh で mp4 にするか、「<フォルダ>/<名前>-%d.png」の形で渡す。連写の 1 コマは 1/60 秒ではない
    (1 コマ = every + 4 フレーム = playevery=1 でも 1/12 秒) ので、seq.py が読んだ本当の秒で 60fps に並べ直してから測る
    (同じコマが続く = 1 行 = 1/60 秒のまま。連写の後ろの 1 枚撮り state-* は入れない)。[--every N] [--fps F] は seq.py と同じ
    矩形は 384×216 の座標 (元の fine.py の top・mid・floor・full が既定)。1080p の座標で書く時は --hd を付ける (×0.2 で写す)。
  出る物: コマごとの 基準 (最初の 6 コマの中央値) からの差・暖色 (R−B>20) と寒色 (B−R>20) の割合 (top の矩形)・上の帯の位相相関のずれ。
  motion.md §2〜§3 の本家の数字はこの道具の値 (YouTube の再圧縮込み ±10%)。
"""
import argparse
import os
import subprocess
import sys
import tempfile

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import seq as SEQ  # noqa: E402  (同じフォルダの seq.py = 連写の何枚目が何秒か)

W, H = 384, 216
REG = {'top': (slice(14, 72), slice(84, 300)), 'mid': (slice(72, 144), slice(24, 360)), 'floor': (slice(156, 206), slice(36, 348)),
       'full': (slice(0, H), slice(0, W))}


def seq_input(src, every=None, fps=None):
    """「<フォルダ>/<名前>-%d.png」→ ffmpeg の入力の引数 (本当の秒の concat の一覧)・合計秒・一覧のファイル。連番でなければ None"""
    sp = SEQ.split_pattern(src) if '%' in src else None
    if not sp:
        return None
    b = SEQ.read_burst(sp[0], sp[1], every, fps)
    print('# ' + SEQ.summary(b).replace('\n', '\n# '))
    fd, lst = tempfile.mkstemp(suffix='.ffconcat'); os.close(fd)
    tot = SEQ.write_concat(b, lst)
    return ['-f', 'concat', '-safe', '0', '-i', lst], tot, lst


def decode(src, start, ln, every=None, fps=None):
    args = ['ffmpeg', '-v', 'error']
    si = seq_input(src, every, fps)
    if si:   # 連番の画 (うちの撮影): 本当の秒で 60fps に並べ直し、開始秒で切る
        inp, tot, lst = si
        args += inp + ['-vf', 'fps=60,trim=start=%g:duration=%g,setpts=PTS-STARTPTS,scale=%d:%d' % (start, ln, W, H), '-t', '%g' % max(0.0, min(ln, tot - start))]
    else:
        lst = None
        args += ['-ss', str(start), '-t', str(ln), '-i', src, '-vf', 'fps=60,scale=%d:%d' % (W, H)]
    try:
        p = subprocess.run(args + ['-pix_fmt', 'rgb24', '-f', 'rawvideo', '-'], capture_output=True)
    finally:
        if lst:
            os.remove(lst)
    if p.returncode != 0 or not p.stdout:
        sys.exit('ffmpeg で読めない: %s\n%s' % (src, p.stderr.decode(errors='replace')[-800:]))
    return np.frombuffer(p.stdout, np.uint8).reshape(-1, H, W, 3).astype(np.float32)


def shift(ref, img):
    f = np.fft.fft2(ref); g = np.fft.fft2(img); r = f * np.conj(g); r /= (np.abs(r) + 1e-6); c = np.fft.ifft2(r).real
    iy, ix = np.unravel_index(np.argmax(c), c.shape)
    if iy > c.shape[0] // 2:
        iy -= c.shape[0]
    if ix > c.shape[1] // 2:
        ix -= c.shape[1]
    return ix, iy, float(c.max())


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('src'); ap.add_argument('start', type=float); ap.add_argument('len', type=float); ap.add_argument('label', nargs='?', default='')
    ap.add_argument('--region', action='append', default=[], help='名前=x0,y0,x1,y1 (384×216 の座標。--hd なら 1080p)')
    ap.add_argument('--hd', action='store_true', help='--region を 1080p の座標で読む')
    ap.add_argument('--csv')
    ap.add_argument('--every', type=int, help='連番: layout.json も STATE も無い時の every (seq.py と同じ)')
    ap.add_argument('--fps', type=float, help='連番: pshots でない素の連番 (1 コマ 1/F 秒)')
    a = ap.parse_args()
    reg = dict(REG)
    for spec in a.region:
        nm, v = spec.split('=', 1)
        x0, y0, x1, y1 = [float(q) for q in v.split(',')]
        if a.hd:
            x0, y0, x1, y1 = [q * W / 1920 for q in (x0, y0, x1, y1)]
        reg[nm] = (slice(int(y0), int(y1)), slice(int(x0), int(x1)))
    arr = decode(a.src, a.start, a.len, a.every, a.fps)
    L = 0.299 * arr[..., 0] + 0.587 * arr[..., 1] + 0.114 * arr[..., 2]
    m = {k: L[:, r, c].mean(axis=(1, 2)) for k, (r, c) in reg.items()}
    tr, tc = reg['top']
    warm = ((arr[..., 0] - arr[..., 2]) > 20)[:, tr, tc].mean(axis=(1, 2))
    cool = ((arr[..., 2] - arr[..., 0]) > 20)[:, tr, tc].mean(axis=(1, 2))
    ref = L[0, 10:150, 40:344] - L[0, 10:150, 40:344].mean()
    base = {k: np.median(m[k][:6]) for k in m}
    keys = list(reg)
    print('# %s start=%s len=%s base %s' % (a.label, a.start, a.len, ' '.join('%s=%.1f' % (k, base[k]) for k in keys)))
    print('frame  t(s)  ' + ' '.join('%6s' % k for k in keys) + '  warm  cool   dx  dy  corr')
    rows = []
    for i in range(len(L)):
        dx, dy, cc = shift(ref, L[i, 10:150, 40:344] - L[i, 10:150, 40:344].mean())
        vals = [m[k][i] - base[k] for k in keys]
        print('%4d %7.3f ' % (i, a.start + i / 60) + ' '.join('%+6.1f' % v for v in vals) + ' %5.2f %5.2f %4d %3d %5.2f' % (warm[i], cool[i], dx, dy, cc))
        rows.append([i, round(a.start + i / 60, 4)] + [round(float(v), 2) for v in vals] + [round(float(warm[i]), 3), round(float(cool[i]), 3), dx, dy, round(cc, 3)])
    if a.csv:
        with open(a.csv, 'w') as f:
            f.write(','.join(['frame', 't'] + keys + ['warm', 'cool', 'dx', 'dy', 'corr']) + '\n')
            for r in rows:
                f.write(','.join(str(v) for v in r) + '\n')


if __name__ == '__main__':
    main()

#!/usr/bin/env python3
"""待機中のカメラの揺れ: 全解像度 60fps で奥の帯 (1080p の座標) を基準コマと位相相関し、dx/dy (1080p の px・0.25px まで) を 1 コマごとに出す
(HD-2D 段2 レーン F。~/.cache/deck-rogue/hd2d-stage2/motion/tools/sway.py の写し。動画の置き場は決め打ちしない = 引数)。

使い方:
  python3 scripts/hd2d-motion/sway.py <動画 or 連番の画の型> <開始秒> <長さ秒> [x0,x1,y0,y1] [--every N] [--fps F]
    既定の矩形は x 600〜1320・y 60〜300 (元の sway.py と同じ。キャラと UI を避けた奥の帯)。うちの撮影の連写は「<フォルダ>/<名前>-%d.png」で渡せる。
    連写の 1 コマは 1/60 秒ではない (1 コマ = every + 4 フレーム = playevery=1 でも 1/12 秒) ので、seq.py が読んだ本当の秒で 60fps に
    並べ直してから測る (同じコマが続く。連写の後ろの 1 枚撮り state-* は入れない)。--every・--fps は seq.py と同じ。
    本家の待機は 3 場面 × 4 秒で 0.00px (motion.md §2-9)。目標 = 待機のカメラ 0 (計画 §1)。
  出る物: dx・dy の最小・最大・幅 (1080p の px)、10 コマおきの値。終了コード 0。
"""
import argparse
import os
import subprocess
import sys
import tempfile

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import seq as SEQ  # noqa: E402  (同じフォルダの seq.py = 連写の何枚目が何秒か)


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('src'); ap.add_argument('start', type=float); ap.add_argument('len', type=float)
    ap.add_argument('rect', nargs='?', default='600,1320,60,300', help='x0,x1,y0,y1 (1080p)')
    ap.add_argument('--every', type=int); ap.add_argument('--fps', type=float)
    a = ap.parse_args()
    v, start, ln = a.src, a.start, a.len
    x0, x1, y0, y1 = [int(x) for x in a.rect.split(',')]
    vf = 'scale=1920:1080,crop=%d:%d:%d:%d,format=gray' % (x1 - x0, y1 - y0, x0, y0)
    args = ['ffmpeg', '-v', 'error']
    lst = None
    sp = SEQ.split_pattern(v) if '%' in v else None
    if sp:
        b = SEQ.read_burst(sp[0], sp[1], a.every, a.fps)
        print('# ' + SEQ.summary(b).replace('\n', '\n# '))
        fd, lst = tempfile.mkstemp(suffix='.ffconcat'); os.close(fd)
        tot = SEQ.write_concat(b, lst)
        args += ['-f', 'concat', '-safe', '0', '-i', lst, '-vf', 'fps=60,trim=start=%g:duration=%g,setpts=PTS-STARTPTS,%s' % (start, ln, vf),
                 '-t', '%g' % max(0.0, min(ln, tot - start))]
    else:
        args += ['-ss', str(start), '-t', str(ln), '-i', v, '-vf', 'fps=60,' + vf]
    try:
        p = subprocess.run(args + ['-f', 'rawvideo', '-'], capture_output=True)
    finally:
        if lst:
            os.remove(lst)
    if p.returncode != 0 or not p.stdout:
        sys.exit('ffmpeg で読めない: %s\n%s' % (v, p.stderr.decode(errors='replace')[-800:]))
    a = np.frombuffer(p.stdout, np.uint8).reshape(-1, y1 - y0, x1 - x0).astype(np.float32)

    # 4 倍に拡大して 0.25px まで読む (フーリエの位相相関を 4 倍の格子で)
    def shift(ref, img):
        f = np.fft.fft2(ref); g = np.fft.fft2(img); r = f * np.conj(g); r /= (np.abs(r) + 1e-6)
        Hh, Ww = r.shape; R = np.zeros((Hh * 4, Ww * 4), complex)
        R[:Hh // 2, :Ww // 2] = r[:Hh // 2, :Ww // 2]; R[-Hh // 2:, :Ww // 2] = r[-Hh // 2:, :Ww // 2]
        R[:Hh // 2, -Ww // 2:] = r[:Hh // 2, -Ww // 2:]; R[-Hh // 2:, -Ww // 2:] = r[-Hh // 2:, -Ww // 2:]
        c = np.fft.ifft2(R).real; iy, ix = np.unravel_index(np.argmax(c), c.shape)
        if iy > c.shape[0] // 2:
            iy -= c.shape[0]
        if ix > c.shape[1] // 2:
            ix -= c.shape[1]
        return ix / 4, iy / 4
    ref = a[0] - a[0].mean(); xs = []; ys = []
    for i in range(len(a)):
        dx, dy = shift(ref, a[i] - a[i].mean()); xs.append(dx); ys.append(dy)
    xs = np.array(xs); ys = np.array(ys)
    print('%s %s+%ss crop x%d-%d y%d-%d: frames %d' % (v, start, ln, x0, x1, y0, y1, len(a)))
    print('dx: min %.2f max %.2f  range %.2f px | dy: min %.2f max %.2f range %.2f px (1080p)' % (xs.min(), xs.max(), xs.max() - xs.min(), ys.min(), ys.max(), ys.max() - ys.min()))
    print('every 10th frame dx,dy:', ' '.join('(%+.2f,%+.2f)' % (x, y) for x, y in zip(xs[::10], ys[::10])))


if __name__ == '__main__':
    main()

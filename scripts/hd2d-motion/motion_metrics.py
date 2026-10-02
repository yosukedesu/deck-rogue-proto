#!/usr/bin/env python3
"""動きの数字 (HD-2D 段2 レーン F。2026-10-03)。うちの連写 (pshots の <名前>-<n>.png) か動画 (本家の clip・うちの mp4) から、
計画 §1 の動きの目安の数字を出す: 寄りの間の壁 ×N・暖色の割合・技の光の長さと保つ秒・暗転 ×N・待機のカメラの揺れ。
★ 連写の 1 コマは 1/60 秒ではない (1 コマ = every + 4 フレーム = playevery=1 でも 1/12 秒)。何枚目が何秒かは seq.py が
  layout.json の frame (無ければ every) から読む。連写の後ろの 1 枚撮り (state-*) は外す。動画は ffmpeg で 60fps に落とす (1 コマ 1/60 秒)。
式は ~/.cache/deck-rogue/hd2d-stage2/motion/tools (metrics.py・fine.py・sway.py) と motion.md §2〜§3 と同じ:
  明るさ = 0.299R + 0.587G + 0.114B の矩形の平均 (動画の道具と同じ。静止画の物差しの 0.2126… とは別)
  暖色の割合 = R − B > 20 の画素の割合 (矩形の中)
  壁 ×N = 同じカメラの「光あり ÷ 光なし」(本家 松明の洞窟 6.90s ÷ 7.48s = ×2.27・右端 ×4.41)。ここでは寄りの区間 (カット〜カット) の
          最大 ÷ 最大の後のその区間の最小。寄りの前の基準に対する倍も peakOverBase に出す
  基準 = 動画は最初の 6 コマ (0.1 秒) の中央値。連写は「出す前の盤面」の 1 枚撮り <名前>-pre-1.png (s2-dio23.txt の -pre の行) があればそれ、
          無ければ連写の 1 枚目だけ (命令の every+1 フレーム後 = 当たりの光の前。2 枚目からは光が入りうる)。--base-image で書ける
  技の光の長さ = 壁の矩形の明るさが「区間の最小 + (最大 − 最小) × 0.1」を超えているコマの秒の合計・保つ = × 0.8 (本家 0.95 秒・保つ 0.35 秒)。
          連写は 1 コマ 1/12 秒以上なので、秒は 1 コマぶん (0.08 秒〜) の粗さを持つ
  暗転 = 最初の光の山より前 (0.1 秒より後) で、全体と床の明るさの最小 ÷ 基準 (本家 ブーストの溜め 全体 ×0.66・床 ×0.30)
  カット = 前のコマとの明るさの差の平均が 30 を超え、かつ形の相関 (位相相関の山) が 0.7 より低い所 (本家の寄りは 1 フレームで切り替わる。
          6.650s −0.5 → 6.667s +77.6。光の閃きは明るさだけ変わり形は残る = カットに数えない)。本家の YouTube の動画は暗い→暗いの切り戻しを
          拾えないことがある (圧縮のため) = --zoom 開始秒,終わり秒 で寄りの区間を書ける。うちの連写 (圧縮なし) は切り戻しも拾える
          (連写は 1 コマの間にキャラも動くので、差の平均は動画より大きく出る = 形の相関で見分ける)
  待機のカメラ = --idle の区間 (秒) で奥の帯 (既定 x 600〜1320・y 60〜300) を区間の最初のコマと位相相関したずれの幅 (px・1080p)。本家 0.00px
目標 (計画 §1・motion.md §7): 寄りの間の壁 ×2 以上・暖色 0.6 以上 (幕2)・光 0.9 秒 (保つ 0.3)・暗転 ×0.5・待機のカメラ 0。合否は目 (動画の左右並べ) で決める。
秒は全部「連写の 1 枚目 (動画は --start) = 0 秒」(frames2mp4.sh の mp4 の 0 秒と同じ)。

使い方:
  python3 scripts/hd2d-motion/motion_metrics.py <撮影のフォルダ> <名前> [--json 出力.json] [--csv コマごと.csv]
      [--every N (layout.json も STATE も無い時)] [--fps F (素の連番)] [--burst play|gear|end|fire|enter|fx] [--base-image <出す前の盤面.png>]
  python3 scripts/hd2d-motion/motion_metrics.py --video <動画> --start <秒> --len <秒> [--label 見出し]
  矩形は 1080p の座標: [--region wall=900,120,1900,520] [--region floor=500,760,1400,1000] (既定 = motion.md §2-1 の本家 松明の洞窟の「右の壁」と
    「床 中央」の矩形。うちの寄りの画では壁がどこに入るかが変わるので、撮った画を見て書き直す)・[--idle 開始秒,長さ秒]・[--cut 30]・[--corr 0.7]・[--zoom 開始秒,終わり秒]
  コマは 1 枚ずつ読んで捨てる (300 コマの連写でもメモリは 1 コマぶん)。
"""
import argparse
import json
import os
import subprocess
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import seq as SEQ  # noqa: E402  (同じフォルダの seq.py = 連写の何枚目が何秒か)

DS = 2   # 1/2 に縮めて測る (960×540)
DEF_REG = {'wall': (900, 120, 1900, 520), 'floor': (500, 760, 1400, 1000)}
TARGET = dict(wallX=2.0, warm=0.6, light=0.9, hold=0.3, dark=0.5, sway=0.0)
VIDEO_FPS = 60.0   # 動画は ffmpeg で 60fps に落として読む (1 コマ 1/60 秒)


class Clock:
    """コマの秒。動画は i/60 (元の式のまま)、連写は seq.py が読んだ秒 (1 枚目 = 0 秒・1 コマの長さ dur)"""

    def __init__(self, t=None, dur=None, rate=None):
        self.t = None if t is None else np.asarray(t, float)
        self.dur = None if dur is None else np.asarray(dur, float)
        self.rate = rate

    def at(self, i):
        return i / self.rate if self.rate else float(self.t[i])

    def idx(self, x):
        """x 秒のコマ (それ以前で最後のコマ)。動画は int(x × 60) = 元の式"""
        if self.rate:
            return int(x * self.rate)
        return max(0, int(np.searchsorted(self.t, x + 1e-9, 'right')) - 1)

    def span(self, s0, s1):
        """コマ s0〜s1 (s1 は含まない) の始まり・終わり・長さ"""
        if self.rate:
            return s0 / self.rate, s1 / self.rate, (s1 - s0) / self.rate
        a = float(self.t[s0]); b = float(self.t[s1 - 1] + self.dur[s1 - 1])
        return a, b, b - a

    def secs(self, mask, s0):
        """区間 (s0 から) の真のコマの秒の合計"""
        if self.rate:
            return float(mask.sum()) / self.rate
        return float(self.dur[s0:s0 + len(mask)][mask].sum())

    def total(self, n):
        return n / self.rate if self.rate else float(self.t[n - 1] + self.dur[n - 1])

    def after(self, i, sec):
        """コマ i が sec 秒より後か (動画は元の式 i > sec × 60)"""
        return i > sec * self.rate if self.rate else float(self.t[i]) > sec


def seq_frames(files):
    """連写を 1 枚ずつ縮めて返す (全部を一度に持たない)"""
    for p in files:
        with Image.open(p) as im:
            yield np.asarray(im.convert('RGB').reduce(DS), np.uint8)


def video_frames(path, start, ln):
    """動画を 60fps・1920 幅の 1/DS で 1 コマずつ返す。戻り値 (反復子, H0)"""
    probe = subprocess.run(['ffprobe', '-v', 'error', '-select_streams', 'v:0', '-show_entries', 'stream=width,height', '-of', 'csv=p=0', path],
                           capture_output=True, text=True)
    try:
        w, h = [int(v) for v in probe.stdout.strip().split(',')[:2]]
    except ValueError:
        sys.exit('ffprobe で読めない: %s' % path)
    H0 = round(1920 * h / w)          # 1920 幅にそろえた時の高さ
    W2, H2 = 1920 // DS, H0 // DS

    def gen():
        p = subprocess.Popen(['ffmpeg', '-v', 'error', '-ss', str(start), '-t', str(ln), '-i', path, '-vf', 'fps=60,scale=%d:%d' % (W2, H2),
                              '-pix_fmt', 'rgb24', '-f', 'rawvideo', '-'], stdout=subprocess.PIPE)
        size = W2 * H2 * 3; got = 0
        try:
            while True:
                buf = p.stdout.read(size)
                if len(buf) < size:
                    break
                got += 1
                yield np.frombuffer(buf, np.uint8).reshape(H2, W2, 3)
        finally:
            p.stdout.close(); p.wait()
        if got == 0:
            sys.exit('ffmpeg で読めない: %s' % path)
    return gen(), H0


def region(rect, H0):
    """1080p の矩形 → 縮めた画の切り口 (画の高さが 1080 でない時は縦を H0/1080 で写す)"""
    x0, y0, x1, y1 = rect
    sy = H0 / 1080.0
    return slice(int(y0 * sy / DS), int(y1 * sy / DS)), slice(int(x0 / DS), int(x1 / DS))


def lum(fr):
    a = fr.astype(np.float32)
    return a, 0.299 * a[..., 0] + 0.587 * a[..., 1] + 0.114 * a[..., 2]


def phase_shift(ref, img):
    f = np.fft.fft2(ref); g = np.fft.fft2(img); r = f * np.conj(g); r /= (np.abs(r) + 1e-6); c = np.fft.ifft2(r).real
    iy, ix = np.unravel_index(np.argmax(c), c.shape)
    if iy > c.shape[0] // 2:
        iy -= c.shape[0]
    if ix > c.shape[1] // 2:
        ix -= c.shape[1]
    return int(ix) * DS, int(iy) * DS


def pc_peak(x, y):
    """位相相関の山の高さ (形が同じ = 1 に近い・カメラが切り替わると下がる)"""
    f = np.fft.fft2(x); g = np.fft.fft2(y); r = f * np.conj(g); r /= (np.abs(r) + 1e-6)
    return float(np.fft.ifft2(r).real.max())


def seg_numbers(w, fl, wm, base, s0, s1, clk, manual=False):
    """1 区間 (同じカメラ) の 壁 ×N・暖色・光と保つ秒・床 ×N"""
    a, b, d = clk.span(s0, s1)
    sd = dict(start=round(a, 3), end=round(b, 3), dur=round(d, 3))
    if manual:
        sd['manual'] = True
    if w is not None and s1 > s0:
        seg = w[s0:s1]; ip = int(np.argmax(seg)); pk = float(seg[ip]); lo = float(seg[ip:].min())
        sd.update(wallPeak=round(pk, 1), wallPeakAt=round(clk.at(s0 + ip), 3), wallMinAfter=round(lo, 1),
                  wallX=round(pk / max(lo, 1.0), 2), peakOverBase=round(pk / max(base['wall'], 1.0), 2),
                  warmPeak=round(float(wm[s0:s1].max()), 3), warmBase=round(base['warmWall'], 3))
        span = pk - lo
        if span > 4:
            sd['light'] = round(clk.secs(seg > lo + 0.1 * span, s0), 3)
            sd['hold'] = round(clk.secs(seg > lo + 0.8 * span, s0), 3)
    if fl is not None and s1 > s0:
        fs = fl[s0:s1]
        sd['floorX'] = round(float(fs.max()) / max(float(fs[int(np.argmax(fs)):].min()), 1.0), 2)
    return sd


def frame_stats(fr, regs, H0):
    """1 コマの 全体・矩形の明るさ・矩形の暖色の割合 と、明るさの画 (カットと待機に使う)"""
    a, L = lum(fr)
    st = {'full': float(L.mean())}
    wm = {}
    for k, rect in regs.items():
        ry, rx = region(rect, H0)
        st[k] = float(L[ry, rx].mean())
        wm[k] = float(((a[ry, rx, 0] - a[ry, rx, 2]) > 20).mean())
    return st, wm, L


def analyze(frames, H0, regs, clk, cut=30.0, idle=None, zoom=None, corr=0.7, base_img=None):
    """frames = 縮めた RGB を 1 コマずつ返す反復子 (全部を一度に持たない)。clk = Clock。base_img = 出す前の盤面 (縮めた RGB) か None"""
    keys = ['full'] + list(regs)
    m = {k: [] for k in keys}
    warm = {k: [] for k in regs}
    cuts = []
    sway_x, sway_y = [], []
    i0 = i1 = None
    if idle:
        i0 = clk.idx(idle[0]); i1 = clk.idx(idle[0] + idle[1])
    ry_b, rx_b = region((600, 60, 1320, 300), H0)
    ref = None
    prevL = None
    n = 0
    for i, fr in enumerate(frames):
        st, wm, L = frame_stats(fr, regs, H0)
        for k in keys:
            m[k].append(st[k])
        for k in regs:
            warm[k].append(wm[k])
        if prevL is not None and float(np.abs(L - prevL).mean()) > cut:
            x = prevL[::2, ::2]; y = L[::2, ::2]
            if pc_peak(x - x.mean(), y - y.mean()) < corr:   # 形も変わった = カメラが切り替わった
                cuts.append(i)
        if idle and i0 <= i < i1:
            cur = L[ry_b, rx_b] - L[ry_b, rx_b].mean()
            if ref is None:
                ref = cur
            dx, dy = phase_shift(ref, cur); sway_x.append(dx); sway_y.append(dy)
        prevL = L
        n = i + 1
    if n < 2:
        sys.exit('コマが 2 枚より少ない')
    m = {k: np.asarray(v) for k, v in m.items()}
    warm = {k: np.asarray(v) for k, v in warm.items()}
    # カットの塊 (続くコマは 1 つにまとめる)
    cl = []
    for i in cuts:
        if not cl or i > cl[-1] + 1:
            cl.append(i)
    bounds = [0] + cl + [n]
    segs = [(bounds[i], bounds[i + 1]) for i in range(len(bounds) - 1) if bounds[i + 1] - bounds[i] >= 2]
    if base_img is not None:
        bst, bwm, _ = frame_stats(base_img, regs, H0)
        base = dict(bst); base['warmWall'] = bwm.get('wall', 0.0); base_src = '出す前の盤面 (1 枚撮り)'
    elif clk.rate:
        base = {k: float(np.median(v[:6])) for k, v in m.items()}
        base['warmWall'] = float(np.median(warm['wall'][:6])) if 'wall' in warm else 0.0
        base_src = '最初の 6 コマ (0.1 秒) の中央値'
    else:
        base = {k: float(v[0]) for k, v in m.items()}
        base['warmWall'] = float(warm['wall'][0]) if 'wall' in warm else 0.0
        base_src = '連写の 1 枚目 (出す前の盤面 <名前>-pre-1.png が無い)'
    out = dict(frames=n, seconds=round(clk.total(n), 3), base={k: round(v, 1) for k, v in base.items() if k != 'warmWall'},
               baseSource=base_src, cuts=[round(clk.at(i), 3) for i in cl], segments=[])
    w = m.get('wall')
    for s0, s1 in segs:
        out['segments'].append(seg_numbers(w, m.get('floor'), warm.get('wall'), base, s0, s1, clk))
    # 寄り = 最初の区間より後で 0.2 秒以上ある区間のうち、壁 ×N がいちばん大きい物 (--zoom で書けばその区間)
    if zoom:
        z0, z1 = clk.idx(zoom[0]), min(n, clk.idx(zoom[1]))
        if z1 <= z0:
            sys.exit('--zoom の区間にコマが無い (%s〜%s 秒)' % zoom)
        out['segments'].append(seg_numbers(w, m.get('floor'), warm.get('wall'), base, z0, z1, clk, manual=True))
        cand = [out['segments'][-1]]
    else:
        cand = [q for q in out['segments'][1:] if q['dur'] >= 0.2]
    if cand:
        z = max(cand, key=lambda q: q.get('wallX') or 0)
        out['zoom'] = dict(start=z['start'], dur=z['dur'], manual=z.get('manual', False), wallX=z.get('wallX'), peakOverBase=z.get('peakOverBase'),
                           warmPeak=z.get('warmPeak'), light=z.get('light'), hold=z.get('hold'), floorX=z.get('floorX'))
    # 暗転: 最初の光の山 (壁の最大) より前の 全体と床の最小 ÷ 基準
    if w is not None:
        ipk = int(np.argmax(w))
        if clk.after(ipk, 0.1):
            full = m['full']
            out['dark'] = dict(full=round(float(full[:ipk].min()) / max(base['full'], 1.0), 2),
                               floor=round(float(m['floor'][:ipk].min()) / max(base['floor'], 1.0), 2) if 'floor' in m else None,
                               at=round(clk.at(int(np.argmin(full[:ipk]))), 3))
    # 待機のカメラ
    if sway_x:
        out['sway'] = dict(dx=round(max(sway_x) - min(sway_x), 2), dy=round(max(sway_y) - min(sway_y), 2), frames=len(sway_x),
                           note='1/%d に縮めて測った (%dpx 刻み)。細かく見る時は sway.py' % (DS, DS))
    out['series'] = dict(t=[round(clk.at(i), 3) for i in range(n)], full=[round(float(v), 1) for v in m['full']],
                         **{k: [round(float(v), 1) for v in m[k]] for k in regs}, warmWall=[round(float(v), 3) for v in warm.get('wall', [])])
    return out


def report(o, label):
    tm = o.get('timing') or {}
    per = ('・1 コマ %s フレーム (1/%.3g 秒)' % (tm['step'], 60.0 / tm['step'])) if tm.get('step') else ('・1 コマ 1/60 秒' if tm.get('video') else '')
    lines = ['== %s (%d コマ・%.2f 秒%s・基準 %s = %s)' % (label, o['frames'], o['seconds'], per, o['base'], o.get('baseSource'))]
    if tm and not tm.get('video'):
        lines.append('  時刻: %s%s%s' % (tm.get('source'), ('・1 枚目は命令の %d フレーム後' % tm['firstAfterCommand']) if tm.get('firstAfterCommand') is not None else '',
                                         ('・外した: %s' % ', '.join('%d %s' % tuple(q) for q in tm['excluded'])) if tm.get('excluded') else ''))
        for q in tm.get('notes') or []:
            lines.append('  注: ' + q)
    lines.append('  カット: %s' % (', '.join('%.3fs' % c for c in o['cuts']) or 'なし'))
    for s in o['segments']:
        lines.append('  区間 %.3f〜%.3fs (%.2f 秒): 壁の最大 %s @%ss・壁 ×%s (同じカメラ)・基準の ×%s・暖色 %s→%s・光 %s 秒・保つ %s 秒・床 ×%s' % (
            s['start'], s['end'], s['dur'], s.get('wallPeak'), s.get('wallPeakAt'), s.get('wallX'), s.get('peakOverBase'),
            s.get('warmBase'), s.get('warmPeak'), s.get('light'), s.get('hold'), s.get('floorX')))
    z = o.get('zoom')
    if z:
        def mark(v, tgt, ge=True):
            if v is None:
                return '—'
            return '%s %s' % (v, '○' if (v >= tgt if ge else v <= tgt) else '×')
        lines.append('  寄り%s %.2f 秒: 壁 ×%s (目安 ≥2)・暖色 %s (目安 ≥0.6)・光 %s 秒 (目安 0.9)・保つ %s 秒 (目安 0.3)' % (
            ' (--zoom で書いた区間)' if z.get('manual') else '', z['dur'], mark(z.get('wallX'), TARGET['wallX']), mark(z.get('warmPeak'), TARGET['warm']), z.get('light'), z.get('hold')))
    else:
        lines.append('  寄り: カットが無い (切り替えの寄りが無い = 今の演出か、寄りの無い当たり)')
    if o.get('dark'):
        lines.append('  暗転: 全体 ×%s・床 ×%s (@%ss・目安 ×0.5)' % (o['dark']['full'], o['dark']['floor'], o['dark']['at']))
    if o.get('sway'):
        lines.append('  待機のカメラ: dx 幅 %spx・dy 幅 %spx (%d コマ・目安 0。%s)' % (o['sway']['dx'], o['sway']['dy'], o['sway']['frames'], o['sway']['note']))
    return '\n'.join(lines)


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('folder', nargs='?'); ap.add_argument('name', nargs='?')
    ap.add_argument('--video'); ap.add_argument('--start', type=float, default=0.0); ap.add_argument('--len', type=float, default=4.0)
    ap.add_argument('--label')
    ap.add_argument('--region', action='append', default=[], help='名前=x0,y0,x1,y1 (1080p の座標。wall・floor は既定を上書き)')
    ap.add_argument('--idle', help='待機のカメラを測る区間 開始秒,長さ秒')
    ap.add_argument('--cut', type=float, default=30.0, help='カットとみなす前のコマとの明るさの差 (既定 30)')
    ap.add_argument('--corr', type=float, default=0.7, help='カットとみなす形の相関の上限 (既定 0.7)')
    ap.add_argument('--zoom', help='寄りの区間を書く 開始秒,終わり秒 (本家の動画で切り戻しを拾えない時)')
    ap.add_argument('--every', type=int, help='連写: layout.json も STATE も無い時の every (playevery などの値。1 コマ = every+4 フレーム)')
    ap.add_argument('--fps', type=float, help='素の連番 (1 コマ 1/F 秒) の時だけ。pshots の連写には使わない')
    ap.add_argument('--burst', help='連写の種類 (play・gear・end・fire・enter・fx。既定 = いちばん多い物)')
    ap.add_argument('--base-image', help='基準にする「出す前の盤面」の画 (既定 = 同じフォルダの <名前>-pre-1.png があればそれ)')
    ap.add_argument('--json'); ap.add_argument('--csv')
    a = ap.parse_args()
    regs = dict(DEF_REG)
    for spec in a.region:
        k, v = spec.split('=', 1)
        regs[k] = tuple(float(q) for q in v.split(','))
    base_img = None
    if a.video:
        frames, H0 = video_frames(a.video, a.start, a.len)
        clk = Clock(rate=VIDEO_FPS); label = a.label or '%s %.2f+%.2fs' % (os.path.basename(a.video), a.start, a.len)
        timing = dict(video=True, source='動画を 60fps に落として読む')
    elif a.folder and a.name:
        b = SEQ.read_burst(a.folder, a.name, a.every, a.fps, a.burst)
        with Image.open(b['files'][0]) as im:
            H0, W0 = im.height, im.width
        frames = seq_frames(b['files'])
        clk = Clock(b['t'], b['dur']); label = a.label or a.name
        timing = dict(video=False, kind=b['kind'], every=b['every'], step=b['step'], firstAfterCommand=b['firstAfterCommand'], source=b['source'],
                      excluded=b['excluded'], notes=b['notes'])
        bp = a.base_image or os.path.join(a.folder, a.name + '-pre-1.png')
        if os.path.isfile(bp):
            with Image.open(bp) as im:
                if (im.width, im.height) != (W0, H0):
                    sys.exit('基準の画の大きさが連写と違う: %s (%dx%d・連写 %dx%d)' % (bp, im.width, im.height, W0, H0))
                base_img = np.asarray(im.convert('RGB').reduce(DS), np.uint8)
            timing['baseImage'] = bp
        elif a.base_image:
            sys.exit('基準の画が無い: %s' % a.base_image)
    else:
        ap.error('<撮影のフォルダ> <名前> か --video')
    idle = tuple(float(q) for q in a.idle.split(',')) if a.idle else None
    zoom = tuple(float(q) for q in a.zoom.split(',')) if a.zoom else None
    o = analyze(frames, H0, regs, clk, a.cut, idle, zoom=zoom, corr=a.corr, base_img=base_img)
    o['label'] = label; o['regions'] = regs; o['target'] = TARGET; o['timing'] = timing
    print(report(o, label))
    if a.json:
        json.dump(o, open(a.json, 'w', encoding='utf-8'), ensure_ascii=False, indent=1, default=lambda q: q.item() if hasattr(q, 'item') else str(q))
    if a.csv:
        s = o['series']; keys = [k for k in s if k != 't']
        with open(a.csv, 'w') as f:
            f.write(','.join(['t'] + keys) + '\n')
            for i in range(len(s['t'])):
                f.write(','.join([str(s['t'][i])] + [str(s[k][i]) if i < len(s[k]) else '' for k in keys]) + '\n')


if __name__ == '__main__':
    main()

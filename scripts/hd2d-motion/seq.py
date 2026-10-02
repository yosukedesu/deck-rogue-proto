#!/usr/bin/env python3
"""うちの撮影の連写 (pshots の <名前>-<n>.png) の「何枚目が何秒か」を読む (HD-2D 段2 レーン F。2026-10-03 反証の直し)。
motion_metrics.py・frames2mp4.sh・fine.py・sway.py が共有する。

★ 連写の 1 コマは 1/60 秒ではない。Autopilot の連写のループ (play=・usegear=・endplay=・fire=・entershots=) は
  `for f < every: yield null; yield return Shot(.., settle: 1)` で、Shot は撮る前に 1 フレーム・撮った後に 3 フレーム回す
  = 1 コマ = every + 4 フレーム (det の captureFramerate=60 で 1 フレーム = 1/60 秒。playevery=1 でも 5 フレーム = 1/12 秒)。
  1 枚目は命令 (札を出す・ギアを組む・手番を終える・発動する・登場を鳴らす) の every + 1 フレーム後 = 「札を出す前の盤面」ではない
  (出す前の盤面が要る時は、同じ STATE から play= などを抜いた 1 枚撮り <名前>-pre を撮る = s2-dio23.txt の -pre の行)。
  fx= の連写は 2 フレーム + Shot = 6 フレームごと (命令の 3 フレーム後が 1 枚目)。
  連写の後ろには 1 枚撮りの state-* (演出が落ち着いた後の盤面・間隔が飛ぶ) が同じ連番で続く = 連写から外す。

時刻の出どころ (上から順に使う):
  1. <名前>-<n>.layout.json (dumplayout=1) の frame (Time.frameCount) と name (play-0・state-combat …) = 正確
  2. <名前>.log の「[Autopilot] shot …\\NN-<撮った名前>.png」の並び (名前だけ) ＋ every で (every+4)/60 秒刻み。every は --every か、
     layout.json の state か、リポジトリの scripts/hd2d-states/*.txt の同じ名前の行の STATE から読む
     log の ut= は Time.unscaledTime = 実時間 (captureFramerate は deltaTime だけを固定する = Tween.UnscaledDt の注記)。撮影の止まりが入るので使わない
  3. どちらも無ければ --every N (pshots の連写) か --fps F (1 コマ 1/F 秒の素の連番) を必ず書く。書かなければ止まる (黙って 1/60 秒と読まない)
秒は「連写の 1 枚目 = 0 秒」(mp4 の 0 秒と同じ)。命令からのずれは firstAfterCommand (フレーム) に出す。

使い方:
  python3 scripts/hd2d-motion/seq.py <撮影のフォルダ> <名前> [--every N] [--fps F] [--burst play|gear|end|fire|enter|fx] [--concat 一覧.txt] [--json]
    --concat は ffmpeg の concat の一覧 (1 枚ごとの duration つき) を書く = frames2mp4.sh・fine.py・sway.py が「本当の秒」で読む
"""
import argparse
import glob
import json
import os
import re
import sys

import numpy as np

FRAME = 1.0 / 60.0   # det の captureFramerate=60 の 1 フレーム
BURST_RE = re.compile(r'^(play|gear|end|fire|enter|fx)-(\d+)$')
# 連写の種類 → STATE の every の鍵と Autopilot の既定値 (Autopilot.cs の連写のループの字のまま)
EVERY_KEY = {'play': ('playevery', 4), 'gear': ('playevery', 5), 'end': ('endevery', 10), 'fire': ('fireevery', 6), 'enter': ('enterevery', 8)}
FX_GAP = 2           # fx= は `yield null; yield null; Shot(.., 1)` = every 2 と同じ形
POST_SHOT = 4        # 1 コマ = every + POST_SHOT フレーム (撮る前 1・撮った後 3)


def list_pngs(folder, name):
    out = []
    for p in glob.glob(os.path.join(folder, glob.escape(name) + '-*.png')):
        m = re.match(re.escape(name) + r'-(\d+)\.png$', os.path.basename(p))
        if m:
            out.append((int(m.group(1)), p))
    return sorted(out)


def split_pattern(pat):
    """「<フォルダ>/<名前>-%d.png」→ (フォルダ, 名前)。形が違えば None"""
    m = re.match(r'^(.*)-%d\.png$', os.path.basename(pat))
    return (os.path.dirname(pat) or '.', m.group(1)) if m else None


def shot_names_from_log(folder, name):
    """<名前>.log の撮った名前の並び (pshots が付けた 1,2,3… と同じ順)。読めなければ None"""
    p = os.path.join(folder, name + '.log')
    if not os.path.isfile(p):
        return None
    rows = []
    for ln in open(p, encoding='utf-8', errors='replace'):
        m = re.search(r'\[Autopilot\] shot (.+?\.png)', ln)
        if not m:
            continue
        base = re.split(r'[\\/]', m.group(1))[-1]
        q = re.match(r'^(\d+)-(.+)\.png$', base)
        if not q:   # -statesfile (1 回の起動で順に撮る) の時は「行の名前-k.png」= 撮った名前が残らない
            return None
        rows.append((int(q.group(1)), q.group(2)))
    return [nm for _, nm in sorted(rows)] or None


def states_from_lists(name):
    """layout.json が無い時: リポジトリの scripts/hd2d-states/*.txt から「<名前>|…」の行の STATE を全部引く。戻り値 {STATE: [一覧の名前]}
    (同じ名前の行が違う STATE で複数の一覧にあっても、連写の every がそろっていれば使える = read_burst が見る)"""
    here = os.path.dirname(os.path.abspath(__file__))
    found = {}
    for lp in sorted(glob.glob(os.path.join(os.path.dirname(here), 'hd2d-states', '*.txt'))):
        for ln in open(lp, encoding='utf-8', errors='replace'):
            ln = ln.strip()
            if not ln.startswith(name + '|'):
                continue
            rest = ln[len(name) + 1:]
            st = rest.split('|', 1)[1] if rest[:3] in ('PC|', 'PH|') else rest
            found.setdefault(st, [])
            if os.path.basename(lp) not in found[st]:
                found[st].append(os.path.basename(lp))
    return found


def every_of(state, kind):
    if kind == 'fx':
        return FX_GAP
    key, dflt = EVERY_KEY.get(kind, (None, None))
    if key is None:
        return None
    m = re.search(r'(?:^|;)' + key + r'=(\d+)', state or '')
    v = int(m.group(1)) if m else dflt
    return v if v > 0 else dflt   # Autopilot と同じ (0 以下は既定)


def read_burst(folder, name, every=None, fps=None, burst=None):
    """連写のコマと秒。戻り値 dict: files・n (pshots の番号)・names・t (1 枚目 = 0 秒)・dur (1 枚ごとの秒)・kind・every・step (1 コマのフレーム)・
    firstAfterCommand (命令から 1 枚目までのフレーム)・source・excluded ([(番号, 名前)])・notes"""
    pngs = list_pngs(folder, name)
    if len(pngs) < 2:
        sys.exit('連写が無い: %s/%s-<n>.png (%d 枚)' % (folder, name, len(pngs)))
    notes = []
    lay = {}
    for n, p in pngs:
        lp = p[:-4] + '.layout.json'
        if os.path.isfile(lp):
            try:
                d = json.load(open(lp, encoding='utf-8'))
                lay[n] = (d.get('frame'), d.get('name'), d.get('state'))
            except (OSError, ValueError):
                pass
    names = None
    state = None
    if len(lay) == len(pngs) and all(v[1] for v in lay.values()):
        names = [lay[n][1] for n, _ in pngs]
        state = next((v[2] for v in lay.values() if v[2]), None)
    else:
        ln = shot_names_from_log(folder, name)
        if ln and len(ln) == len(pngs):
            names = ln
        elif ln:
            notes.append('log の撮った名前 %d 個と画 %d 枚が合わない = 名前を使わない' % (len(ln), len(pngs)))
        state = next((v[2] for v in lay.values() if v[2]), None)
    # 連写のコマだけ (state-* などの 1 枚撮りを外す)
    kind = None
    excluded = []
    if names:
        kinds = [(BURST_RE.match(nm).group(1) if BURST_RE.match(nm) else None) for nm in names]
        cnt = {}
        for k in kinds:
            if k:
                cnt[k] = cnt.get(k, 0) + 1
        if not cnt:
            sys.exit('%s: 連写のコマが無い (撮った名前 %s)' % (name, sorted(set(names))))
        kind = burst if burst else max(cnt, key=lambda k: cnt[k])
        if kind not in cnt:
            sys.exit('%s: 連写 %s が無い (ある物 %s)' % (name, kind, cnt))
        keep = [i for i, k in enumerate(kinds) if k == kind]
        excluded = [(pngs[i][0], names[i]) for i in range(len(pngs)) if kinds[i] != kind]
    else:
        keep = list(range(len(pngs)))
        notes.append('撮った名前が分からない (layout.json も log も無い) = 全部の画を連写として読む (最後の 1 枚が state-* でも外せない)')
    files = [pngs[i][1] for i in keep]
    nums = [pngs[i][0] for i in keep]
    knames = [names[i] for i in keep] if names else [None] * len(keep)
    ev_state = every_of(state, kind) if (kind and state) else None
    if ev_state is None and every is None and kind:
        lists = states_from_lists(name)
        evs = {every_of(st, kind) for st in lists}
        if len(evs) == 1:
            ev_state = evs.pop()
            notes.append('every %d は scripts/hd2d-states/%s の同じ名前の行の STATE から引いた (layout.json が無い)' % (ev_state, '・'.join(sorted({f for v in lists.values() for f in v}))))
        elif len(evs) > 1:
            notes.append('同じ名前の行の every が一覧ごとに違う (%s) = 使わない' % sorted(evs))
    if every is None and ev_state is not None:
        every = ev_state
    elif every is not None and ev_state is not None and every != ev_state:
        notes.append('--every %d と STATE の %d が違う (--every を使う)' % (every, ev_state))
    frames = [lay[n][0] for n in nums] if all(n in lay and isinstance(lay[n][0], int) for n in nums) else None
    step = None
    if frames:
        t = (np.asarray(frames, float) - frames[0]) * FRAME
        d = np.diff(frames)
        step = int(np.median(d)) if len(d) else None
        source = 'layout.json の frame'
        if every is not None and step != every + POST_SHOT:
            notes.append('1 コマ %s フレーム (every %d なら %d のはず)' % (step, every, every + POST_SHOT))
        if len(d) and (d.min() != d.max()):
            notes.append('コマの間隔がそろわない (%d〜%d フレーム)' % (d.min(), d.max()))
    elif every is not None:
        step = every + POST_SHOT
        t = np.arange(len(files)) * step * FRAME
        source = 'every %d から ((every+4)/60 秒刻み)' % every
    elif fps:
        t = np.arange(len(files)) / float(fps)
        source = '--fps %g (素の連番)' % fps
    else:
        sys.exit('%s: 何枚目が何秒か分からない (layout.json の frame も STATE の every も無い)。pshots の連写なら --every N、素の連番なら --fps F を書く' % name)
    dt = np.diff(t)
    last = float(np.median(dt)) if len(dt) else (step * FRAME if step else FRAME)
    dur = np.append(dt, last)
    fac = None
    if every is not None:
        fac = (FX_GAP + 1) if kind == 'fx' else every + 1
    return dict(files=files, n=nums, names=knames, t=t, dur=dur, kind=kind, every=every, step=step, firstAfterCommand=fac,
                source=source, excluded=excluded, notes=notes, folder=folder, name=name)


def summary(b):
    tot = float(b['t'][-1] + b['dur'][-1])
    s = '%s: 連写 %s %d コマ・%.2f 秒' % (b['name'], b['kind'] or '?', len(b['files']), tot)
    if b['step']:
        s += '・1 コマ %d フレーム (1/%.3g 秒)' % (b['step'], 60.0 / b['step'])
    s += '・時刻 = %s' % b['source']
    if b['firstAfterCommand'] is not None:
        s += '・1 枚目は命令の %d フレーム後' % b['firstAfterCommand']
    if b['excluded']:
        s += '・外した: %s' % ', '.join('%d %s' % q for q in b['excluded'])
    for q in b['notes']:
        s += '\n  注: ' + q
    return s


def write_concat(b, path):
    """ffmpeg の concat の一覧 (1 枚ごとの duration)。最後の 1 枚をもう一度書く (書かないと最後の duration が捨てられる)。
    読む側は出力に -t <合計秒> を付ける (付けないと最後の 1 枚が既定の 1/25 秒ぶん伸びる)。戻り値 = 合計秒"""
    def q(p):
        return "'" + os.path.abspath(p).replace("'", "'\\''") + "'"
    with open(path, 'w', encoding='utf-8') as f:
        f.write('ffconcat version 1.0\n')
        for p, d in zip(b['files'], b['dur']):
            f.write('file %s\nduration %.6f\n' % (q(p), d))
        f.write('file %s\n' % q(b['files'][-1]))
    return float(b['t'][-1] + b['dur'][-1])


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('folder'); ap.add_argument('name')
    ap.add_argument('--every', type=int, help='layout.json も STATE も無い時の every (pshots の連写の playevery などの値)')
    ap.add_argument('--fps', type=float, help='素の連番 (1 コマ 1/F 秒) の時だけ')
    ap.add_argument('--burst', help='連写の種類 (play・gear・end・fire・enter・fx。既定 = いちばん多い物)')
    ap.add_argument('--concat', help='ffmpeg の concat の一覧を書く')
    ap.add_argument('--total', action='store_true', help='合計秒だけを出す (シェル用)')
    ap.add_argument('--json', action='store_true')
    a = ap.parse_args()
    b = read_burst(a.folder, a.name, a.every, a.fps, a.burst)
    tot = write_concat(b, a.concat) if a.concat else float(b['t'][-1] + b['dur'][-1])
    if a.total:
        print('%.6f' % tot)
    elif a.json:
        print(json.dumps(dict(name=b['name'], kind=b['kind'], frames=len(b['files']), seconds=round(tot, 4), step=b['step'], every=b['every'],
                              firstAfterCommand=b['firstAfterCommand'], source=b['source'], excluded=b['excluded'], notes=b['notes'],
                              t=[round(float(v), 4) for v in b['t']]), ensure_ascii=False))
    else:
        print(summary(b))


if __name__ == '__main__':
    main()

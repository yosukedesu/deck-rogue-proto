#!/usr/bin/env python3
"""Rebuild from the included CC0 source excerpts. Requires Python 3, numpy, scipy, ffmpeg.
Usage: python3 tools/rebuild.py --out /tmp/b9-rebuilt
Writes only to --out; never modifies game code or included assets.
"""
from pathlib import Path
import numpy as np, scipy.signal as sig, scipy.io.wavfile as wav
import subprocess, json, hashlib, argparse
from fractions import Fraction
SR=48000
def dec(p):
    b = subprocess.check_output(['ffmpeg', '-v', 'error', '-i', str(p), '-ac', '1', '-ar', str(SR), '-f', 'f32le', '-'])
    return np.frombuffer(b, dtype='<f4').astype(np.float64)

def put(p, a, raw=False):
    if raw:
        wav.write(p, SR, a.astype(np.float32))
    else:
        rng = np.random.default_rng(20261108)
        d = (rng.random(len(a)) - rng.random(len(a))) / 65536
        q = np.round(np.clip(a + d, -1, 1 - 1 / 32768) * 32768).astype('<i2')
        q[0] = q[-1] = 0
        wav.write(p, SR, q)

def filt(a, hp=70, lp=12000):
    a = sig.sosfilt(sig.butter(2, hp, fs=SR, btype='highpass', output='sos'), a) if hp else a
    return sig.sosfilt(sig.butter(3, lp, fs=SR, btype='lowpass', output='sos'), a) if lp else a

def fades(a, attack=0.001, release=0.025):
    a = a.copy()
    n = min(int(attack * SR), len(a) // 2)
    m = min(int(release * SR), len(a) // 2)
    if n:
        a[:n] *= np.sin(np.linspace(0, np.pi / 2, n)) ** 2
    if m:
        a[-m:] *= np.cos(np.linspace(0, np.pi / 2, m)) ** 2
    a[0] = a[-1] = 0
    return a

def respeed(a, speed):
    frac = Fraction(1 / speed).limit_denominator(1000)
    return sig.resample_poly(a, frac.numerator, frac.denominator)

def unit(a):
    return a / max(np.max(abs(a)), 1e-09)

def mix(duration, layers):
    out = np.zeros(round(duration * SR))
    for d in layers:
        x = buf[d['source']]
        a = round(d['start'] * SR)
        b = round((d['start'] + d['length']) * SR) if d['length'] else len(x)
        x = x[a:b].copy()
        if d['reverse']:
            x = x[::-1]
        x = respeed(x, d['speed']) if d['speed'] != 1 else x
        x = unit(filt(x, d['hp'], d['lp']))
        if d['decay']:
            x *= np.exp(-np.arange(len(x)) / SR * d['decay'])
        x = fades(x, d['attack'], 0.025) * d['gain']
        delay = round(d['delay'] * SR)
        n = min(len(x), len(out) - delay)
        if n > 0:
            out[delay:delay + n] += x[:n]
    out = fades(filt(out, 45, 15000), 0.001, 0.035)
    return out

def kweight(a):
    b1 = [1.53512485958697, -2.69169618940638, 1.19839281085285]
    a1 = [1, -1.69065929318241, 0.73248077421585]
    b2 = [1, -2, 1]
    a2 = [1, -1.99004745483398, 0.99007225036621]
    return sig.lfilter(b2, a2, sig.lfilter(b1, a1, a))

def momentary(a):
    x = kweight(np.pad(a, (0, max(0, 19200 - len(a)))))
    sq = x * x
    cs = np.r_[0, np.cumsum(sq)]
    val = np.max((cs[19200:] - cs[:-19200]) / 19200)
    return -0.691 + 10 * np.log10(max(val, 1e-20))

def true_peak(a):
    return np.max(abs(sig.resample_poly(a, 4, 1)))

def main():
    global buf
    ap=argparse.ArgumentParser();ap.add_argument('--out',required=True);args=ap.parse_args()
    root=Path(__file__).resolve().parents[1];out=Path(args.out).resolve()
    out.mkdir(parents=True,exist_ok=True)
    manifest=json.loads((root/'manifest.json').read_text())
    provenance=json.loads((root/'provenance.json').read_text())
    buf={s['id']:dec(root/s['file']) for s in provenance['sources']}
    results=[]
    for a in manifest['assets']:
        un=unit(mix(a['measurements']['duration_s'],a['processing_recipe']))*.75
        goal=manifest['normalization']['onset_lufs_m' if a['phase']=='onset' else 'contact_lufs_m']
        gain=min(10**((goal-momentary(un))/20),10**(-6.1/20)/true_peak(un))
        p=out/(a['id']+'.wav');put(p,un*gain)
        digest=hashlib.sha256(p.read_bytes()).hexdigest()
        results.append({'id':a['id'],'sha256':digest,'matches_manifest':digest==a['sha256']})
    (out/'rebuild-report.json').write_text(json.dumps(results,indent=2))
    print(json.dumps({'matched':sum(r['matches_manifest'] for r in results),'total':len(results)}))
    if not all(r['matches_manifest'] for r in results):raise SystemExit(1)
if __name__=='__main__':main()

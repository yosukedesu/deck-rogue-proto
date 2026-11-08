#!/usr/bin/env python3
"""Numerical integrity checks only. Does not listen or establish sound quality.
Run: python3 tools/validate_audio.py [--out report.json]
Requires numpy, scipy and ffmpeg (the latter only for decoding raw source OGG).
"""
from pathlib import Path
import argparse, hashlib, json, wave
import numpy as np
from scipy import signal
from scipy.io import wavfile

def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
    ap=argparse.ArgumentParser();ap.add_argument('--out');args=ap.parse_args()
    root=Path(__file__).resolve().parents[1]
    m=json.loads((root/'manifest.json').read_text())
    provenance=json.loads((root/'provenance.json').read_text())
    rows=[]
    assert len(m['assets'])==26
    assert len({a['id'] for a in m['assets']})==26
    hashes=[]
    for a in m['assets']:
        p=root/a['file'];h=digest(p);hashes.append(h)
        assert h==a['sha256']
        with wave.open(str(p)) as w:
            assert (w.getframerate(),w.getnchannels(),w.getsampwidth())==(48000,1,2)
            n=w.getnframes()
            x=np.frombuffer(w.readframes(n),dtype='<i2').astype(float)/32768
        assert n/48000==a['measurements']['duration_s']
        peak=20*np.log10(np.max(abs(signal.resample_poly(x,16,1))))
        assert peak<=-6, (a['id'],peak)
        assert x[0]==0 and x[-1]==0
        assert np.max(abs(x))<1 and abs(np.mean(x))<.0001
        sr,raw=wavfile.read(root/a['raw_master'])
        assert sr==48000 and raw.ndim==1 and raw.dtype==np.float32
        assert digest(root/a['raw_master'])==a['raw_sha256']
        rows.append({'id':a['id'],'true_peak_16x_dbtp':round(float(peak),5),'sha256':h})
    assert len(set(hashes))==26
    for s in provenance['sources']:assert digest(root/s['file'])==s['sha256']
    f=json.loads((root/'audio.fragment.proposed.json').read_text())['sfx']
    r=json.loads((root/'routing.json').read_text())['routes']
    assert len(f)==26==len(r)
    for item in r:assert f[item['proposed_key']]['sfx']==item['asset']
    report={'passed':True,'auditory_evaluation':False,'game_integration':False,'count':26,'source_hash_checks':len(provenance['sources']),'measurements':rows}
    if args.out:Path(args.out).write_text(json.dumps(report,indent=2))
    print(json.dumps({'passed':True,'count':26,'auditory_evaluation':False,'max_true_peak_dbtp':max(x['true_peak_16x_dbtp'] for x in rows)}))
if __name__=='__main__':main()

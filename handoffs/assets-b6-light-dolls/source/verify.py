#!/usr/bin/env python3
from pathlib import Path
from PIL import Image,ImageColor
import json,hashlib,subprocess,sys
from draw_light_dolls import NAMES,TIMES,effect,composed
R=Path(__file__).resolve().parents[1]
m=json.loads((R/'manifest.json').read_text());pal={ImageColor.getrgb(x)+(255,) for x in json.loads((R/'palette.json').read_text()).values()}
reports=[]
for i,a in enumerate(m['animations']):
    p=R/a['path'];im=Image.open(p);assert im.mode=='RGBA' and im.size==(512,64)
    assert hashlib.sha256(p.read_bytes()).hexdigest()==a['sha256']
    assert set(im.getchannel('A').getdata())=={0,255}
    assert {c for c in im.getdata() if c[3]}<=pal
    cells=[]
    for f in range(8):
        cell=im.crop((64*f,0,64*f+64,64));assert cell.tobytes()==effect(a['key'],f).tobytes()
        bbox=cell.getbbox()
        assert (list(bbox) if bbox else None)==a['frames'][f]['alpha_bbox_px']
        if f<7:assert bbox and bbox[0]>=2 and bbox[1]>=2 and bbox[2]<=62 and bbox[3]<=62
        else:assert bbox is None
        cells.append({'frame':f,'bbox':bbox,'opaque_pixels':sum(1 for x in cell.getchannel('A').getdata() if x)})
    assert a['duration_ms']==sum(TIMES[i]);assert [f['duration_ms'] for f in a['frames']]==TIMES[i]
    apng=Image.open(R/f'previews/{a["key"]}_playback.png');assert apng.n_frames==9
    for f in range(9):
        apng.seek(f);assert round(apng.info['duration'])==(TIMES[i][f] if f<8 else 800)
        k=f if f<8 else 0
        assert apng.convert('RGBA').crop((152,66,216,130)).tobytes()==composed(a['key'],k).tobytes()
        assert apng.convert('RGBA').crop((8,34,136,162)).tobytes()==composed(a['key'],k).resize((128,128),Image.Resampling.NEAREST).tobytes()
        if a['key'] not in ('light_gain','light_release'):
            assert apng.convert('RGBA').crop((244,66,308,130)).tobytes()==composed(a['key'],k,'#17283f',48).tobytes()
    reports.append({'key':a['key'],'frames':cells,'duration_ms':a['duration_ms'],'apng_exact':True,'palette_pass':True,'binary_alpha':True,'terminal_transparent':True})
files=list((R/'Art').rglob('*.png'))+list((R/'previews').glob('*.png'))+[R/'manifest.json',R/'previews/playback_timing.json']
before={str(p.relative_to(R)):hashlib.sha256(p.read_bytes()).hexdigest() for p in files}
for name in ['draw_light_dolls.py','build_manifest.py']:subprocess.run([sys.executable,str(R/'source'/name)],check=True)
after={str(p.relative_to(R)):hashlib.sha256(p.read_bytes()).hexdigest() for p in files};assert before==after
out={'status':'pass','production_atlases':6,'production_frames':48,'dimensions':'512x64 atlas / 64x64 cell','checks':['RGBA8','binary alpha','shared palette','minimum 2px border','all final frames transparent','manifest hash/rect/bbox/times','6 APNGs exact native/2x/48px composites and timing','deterministic reproduction'],'animations':reports,'deterministic_regeneration':before==after,'runtime_integration_tested':False,'not_tested':['actual device rendering','real source/target travel','multi-doll simultaneous command','actual existing doll sprites']}
(R/'qa').mkdir(exist_ok=True);(R/'qa/verification.json').write_text(json.dumps(out,indent=2)+'\n');print('PASS: six atlases / 48 frames / six APNGs / deterministic regeneration')

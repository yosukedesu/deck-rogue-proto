from pathlib import Path
from PIL import Image
import numpy as np, json, hashlib, subprocess
ROOT=Path(__file__).resolve().parents[1]
rows=[]
def check(name, ok, detail=None):
    rows.append({'name':name,'passed':bool(ok),'detail':detail})
build=json.loads((ROOT/'qa/build_audit.json').read_text())
old=json.loads((ROOT/'source/source_manifest.json').read_text())
for o in old['assets']:
    p=ROOT/'source'/o['file'];h=hashlib.sha256(p.read_bytes()).hexdigest()
    check(f'{o["id"]}: original source byte preserved',h==o['sha256'],h)
for a in build['assets']:
    im=Image.open(ROOT/a['file']); x=np.array(im); key=a['id']
    check(f'{key}: size',im.size==(960,270),list(im.size))
    check(f'{key}: RGBA',im.mode=='RGBA')
    check(f'{key}: RGBA endpoint equality',np.array_equal(x[:,0],x[:,-1]))
    check(f'{key}: hash',hashlib.sha256((ROOT/a['file']).read_bytes()).hexdigest()==a['sha256'])
    if key.endswith('_0'):
        check(f'{key}: opaque base',np.all(x[:,:,3]==255))
        # Adjacent discontinuity in the 4px strip: inspect from both sides of join.
        b=x.astype(float); grads=np.concatenate([np.diff(b[:,:4,:3],axis=1),np.diff(b[:,-4:,:3],axis=1)],axis=1)
        check(f'{key}: seam-adjacent RGB delta max <= 8',np.abs(grads).max()<=8,{'max':float(np.abs(grads).max()),'mean':float(np.abs(grads).mean())})
    else:
        check(f'{key}: real transparency',np.any(x[:,:,3]==0) and np.any(x[:,:,3]>0))
        check(f'{key}: fully transparent LR columns',np.all(x[:,0,3]==0) and np.all(x[:,-1,3]==0))
        check(f'{key}: zero hidden RGB',np.all(x[x[:,:,3]==0,:3]==0))
for act in (2,3):
    m=json.loads((ROOT/f'qa/act{act}_motion_measurements.json').read_text())
    check(f'Act{act}: all rendered frames native size',m['all_frames_dimensions'])
    check(f'Act{act}: 240 simulated frames',m['frames']==240)
    for w in m['wrap_tests']:
        check(f'Act{act}/far{w["layer"]}: periodic samples',w['frame_at_0_equals_at_960'] and w['offset_959_to_0_is_one_pixel_roll'])
    r=json.loads(subprocess.check_output(['ffprobe','-v','error','-count_frames','-select_streams','v:0','-show_entries','stream=width,height,nb_read_frames,r_frame_rate,duration','-of','json',str(ROOT/f'qa/act{act}_parallax.mp4')]))['streams'][0]
    check(f'Act{act}: encoded preview frame count',int(r['nb_read_frames'])==240,r)
    check(f'Act{act}: encoded dimensions',r['width']==960 and r['height']==270)
    check(f'Act{act}: encoded fps',r['r_frame_rate']=='20/1')
# Recalculate fog's signed contribution in full composite before ground occlusion.
im=[Image.open(ROOT/f'assets/act2/far_{i}.png').convert('RGBA') for i in range(3)]
for i,o in enumerate([1,.65,.18]):im[i].putalpha(im[i].getchannel('A').point(lambda x:round(x*o)))
base=Image.alpha_composite(im[0],im[1]); full=Image.alpha_composite(base,im[2]);d=np.array(full).astype(int)-np.array(base).astype(int)
lum=d[:,:,:3]@np.array([.2126,.7152,.0722])
check('Act2: corrected fog lightens not blackens backdrop',float(lum.min())>=-1,{'min_luma_delta':float(lum.min()),'max_luma_delta':float(lum.max()),'changed_pixels':int(np.count_nonzero(lum))})
summary={'passed':sum(r['passed'] for r in rows),'total':len(rows),'all_pass':all(r['passed'] for r in rows),'scope':'PNG files + CPU software composition and encoded motion preview. Not game, GPU, browser or Android.','checks':rows}
(ROOT/'qa/validation.json').write_text(json.dumps(summary,indent=2)+'\n')
print(json.dumps(summary,indent=2))
assert summary['all_pass']

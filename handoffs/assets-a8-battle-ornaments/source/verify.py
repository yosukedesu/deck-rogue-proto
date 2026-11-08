#!/usr/bin/env python3
"""Read-only artwork checks, with a report written under qa/."""
from pathlib import Path
from PIL import Image, ImageOps
import hashlib, json, shutil, subprocess, sys, tempfile

ROOT=Path(__file__).resolve().parents[1]
M=json.loads((ROOT/'manifest.json').read_text())
P=json.loads((ROOT/'palette.json').read_text())
palette={tuple(bytes.fromhex(v[1:])) for v in P.values()}
checks=[]; details=[]
def test(name, value): checks.append({'name':name,'pass':bool(value)})
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def frame(im,i): return im.crop((32*i,0,32*(i+1),32))
test('core_asset_count_6',M['core_asset_count']==6 and len(M['assets'])==6)
test('asset_paths_exact', {a['file'] for a in M['assets']}=={p.relative_to(ROOT).as_posix() for p in (ROOT/'Art').rglob('*.png')})
test('all_unique_ids',len({a['id'] for a in M['assets']})==6)
test('only_assets_in_handoff','src/' not in ''.join(a['file'] for a in M['assets']))
test('binary_alpha_declared',M['alpha_values']==[0,255] and not M['premultiplied_alpha'])
test('nearest_no_mipmaps_no_trim',M['filter']=='nearest' and not M['mipmaps'] and not M['auto_trim'])
for a in M['assets']:
    im=Image.open(ROOT/a['file']); px=list(im.getdata()); alpha=im.getchannel('A'); b=alpha.getbbox()
    test(a['id']+'_size_and_mode',list(im.size)==a['size'] and im.mode=='RGBA')
    test(a['id']+'_sha',sha(ROOT/a['file'])==a['sha256'])
    test(a['id']+'_alpha_0_255',set(alpha.getdata())=={0,255})
    test(a['id']+'_transparent_black',all(p==(0,0,0,0) for p in px if p[3]==0))
    test(a['id']+'_palette_only',all(tuple(p[:3]) in palette for p in px if p[3]))
    test(a['id']+'_two_pixel_padding',b[0]>=2 and b[1]>=2 and b[2]<=im.width-2 and b[3]<=im.height-2)
    record={'id':a['id'],'size':list(im.size),'mode':im.mode,'alpha_values':sorted(set(alpha.getdata())),
            'visible_pixels':sum(p[3]>0 for p in px),'visible_colors':len({p[:3] for p in px if p[3]}),
            'bbox':list(b),'sha256':a['sha256']}
    if 'frame_count' in a:
        fs=[frame(im,i) for i in range(8)]
        test(a['id']+'_8_unique_frames',len({f.tobytes() for f in fs})==8)
        test(a['id']+'_8_rects_match',a['frame_rects']==[[i*32,0,32,32] for i in range(8)])
        test(a['id']+'_positive_durations',len(a['frame_durations_ms'])==8 and min(a['frame_durations_ms'])>0)
        test(a['id']+'_total_duration',sum(a['frame_durations_ms'])==a['total_duration_ms'])
        test(a['id']+'_fixed_pivot',a['pivot_top_left_px']==[16,16] and not a['translation_baked'])
        for i,f in enumerate(fs):
            box=f.getchannel('A').getbbox(); test(a['id']+f'_frame_{i}_padding',box[0]>=2 and box[1]>=2 and box[2]<=30 and box[3]<=30)
        record['frames']=[{'index':i,'bbox':list(f.getchannel('A').getbbox()),'visible_pixels':sum(p[3]>0 for p in f.getdata()),'duration_ms':a['frame_durations_ms'][i]} for i,f in enumerate(fs)]
    details.append(record)
left=Image.open(ROOT/'Art/ui/banner_end_l.png'); right=Image.open(ROOT/'Art/ui/banner_end_r.png')
test('caps_exact_horizontal_mirrors',ImageOps.mirror(left).tobytes()==right.tobytes())
test('caps_fit_existing_boss_bands',48+2*3==54 and 48+2*7==62)
test('emblems_same_96px_frame',all(a['size']==[96,96] for a in M['assets'] if a['id'] in ('win_emblem','lose_ink')))
petal=next(a for a in M['assets'] if a['id']=='petal_8f'); ember=next(a for a in M['assets'] if a['id']=='ember_8f')
test('petal_720ms_loop',petal['total_duration_ms']==720 and petal['loop'])
test('ember_680ms_one_shot',ember['total_duration_ms']==680 and not ember['loop'] and ember['hide_after_last_frame'])
test('ember_shrinks_each_frame',all(a['visible_pixels']>=b['visible_pixels'] for a,b in zip(details[-1]['frames'],details[-1]['frames'][1:])))

# APNG is an explanatory preview, but must still play at the manifest timings.
pf=Image.open(ROOT/petal['file']); ef=Image.open(ROOT/ember['file']); ap=Image.open(ROOT/'previews/particles.apng')
t=0; playback_ok=True; ends=[]; total=0
for duration in ember['frame_durations_ms']: total+=duration; ends.append(total)
for n in range(ap.n_frames):
    ap.seek(n); actual=ap.convert('RGBA')
    ei=next((i for i,end in enumerate(ends) if t<end),None)
    for x,src in [(55,frame(pf,int(t//90)%8)),(300,frame(ef,ei) if ei is not None else Image.new('RGBA',(32,32)))]:
        expected=Image.new('RGBA',(96,96),'#101b2b'); expected.alpha_composite(src.resize((96,96),Image.Resampling.NEAREST))
        playback_ok &= actual.crop((x,57,x+96,153)).tobytes()==expected.tobytes()
    t+=ap.info['duration']
test('preview_playback_matches_both_manifests',playback_ok)
test('preview_two_petal_cycles_1440ms',t==1440)

def luminance(hexstr):
    rgb=[v/255 for v in bytes.fromhex(hexstr[1:])]
    return sum(w*(c/12.92 if c<=.04045 else ((c+.055)/1.055)**2.4) for w,c in zip([.2126,.7152,.0722],rgb))
contrasts=[]
for bg in ['#0f1120','#20233a','#2c3d4b']:
    for key in ['brass2','steel2','rose2','ember2']:
        x,y=sorted([luminance(P[key]),luminance(bg)])
        r=(y+.05)/(x+.05); contrasts.append({'background':bg,'highlight':key,'ratio':round(r,3)})
        test('highlight_'+key+'_contrast3_'+bg,r>=3)

with tempfile.TemporaryDirectory(prefix='a8-regenerate-') as td:
    dst=Path(td)/'a8'; shutil.copytree(ROOT,dst)
    for p in (dst/'Art').rglob('*.png'): p.unlink()
    subprocess.run([sys.executable,str(dst/'source/draw_ornaments.py')],check=True)
    test('all_6_artworks_byte_exact_on_regenerate',all(sha(dst/a['file'])==a['sha256'] for a in M['assets']))
    test('manifest_byte_exact_on_regenerate',sha(dst/'manifest.json')==sha(ROOT/'manifest.json'))
    test('preview_apng_byte_exact_on_regenerate',sha(dst/'previews/particles.apng')==sha(ROOT/'previews/particles.apng'))

result={'result':'pass' if all(c['pass'] for c in checks) else 'fail',
        'checks_passed':sum(c['pass'] for c in checks),'checks_total':len(checks),
        'checks':checks,'assets':details,'highlight_contrasts':contrasts,
        'contrast_note':'Ratios measure selected structural highlights, not every decorative/shadow pixel; not a text accessibility certification.',
        'runtime_integration_tested':False,'visual_review':'qa/independent_visual_review.md'}
(ROOT/'qa/verification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({k:result[k] for k in ['result','checks_passed','checks_total']}))
if result['result']!='pass': print([c for c in checks if not c['pass']]); raise SystemExit(1)

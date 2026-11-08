#!/usr/bin/env python3
"""B3 deterministic asset QA. Does not run or modify game code."""
from pathlib import Path
from PIL import Image,ImageColor,__version__ as pillow_version
import json,hashlib,tempfile,subprocess,sys,shutil
from draw_traps import KEYS,TIMES,composite,slot,BG
R=Path(__file__).resolve().parents[1];m=json.loads((R/'manifest.json').read_text());p=json.loads((R/'palette.json').read_text())
checks=[];files=[];fs={}
def check(n,ok,detail=None):checks.append({'check':n,'pass':bool(ok),**({'detail':detail} if detail is not None else {})})
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
colors={ImageColor.getrgb(v) for v in p.values()};expected={f'Art/fx/trap_{k}_8f.png' for k in KEYS}
check('four_expected_sheets',len(m['assets'])==4 and {a['path'] for a in m['assets']}==expected=={str(p.relative_to(R)) for p in (R/'Art/fx').glob('*.png')})
check('native_grid_settings',m['production']['frame_size_px']==[64,64] and m['production']['filter']=='nearest' and not m['production']['mipmaps'] and not m['production']['trim'])
check('no_baked_proxy',m['compositing']['preview_proxy_in_production_assets'] is False)
check('runtime_not_tested',m['provenance']['runtime_integration_tested'] is False and m['qa']['runtime_integration_tested'] is False)
for a in m['assets']:
 k=a['key'];im=Image.open(R/a['path']);h=[];frames=[];cum=0
 check(k+':PNG_RGBA_size',im.format=='PNG' and im.mode=='RGBA' and im.size==(512,64))
 check(k+':hash',sha(R/a['path'])==a['sha256'])
 check(k+':palette',set(v[:3] for v in im.getdata() if v[3])<=colors)
 check(k+':binary_alpha',set(im.getchannel('A').getdata())=={0,255})
 check(k+':transparent_rgb_zero',all(v==(0,0,0,0) for v in im.getdata() if not v[3]))
 for f,dt in enumerate(TIMES[k]):
  cell=im.crop((f*64,0,(f+1)*64,64));frames.append(cell);h.append(hashlib.sha256(cell.tobytes()).hexdigest());bb=cell.getbbox();fr=a['frames'][f]
  check(f'{k}:{f}:rect_bbox_time',fr['index']==f and fr['rect_px']==[f*64,0,64,64] and fr['start_ms']==cum and fr['duration_ms']==dt and fr['bbox_local_px']==(list(bb) if bb else None))
  check(f'{k}:{f}:four_pixel_margin_or_clear',bb is None if f==7 else bb is not None and bb[0]>=4 and bb[1]>=4 and bb[2]<=60 and bb[3]<=60,bb)
  check(f'{k}:{f}:not_blank_until_clear',cell.getchannel('A').getextrema()==((0,0) if f==7 else (0,255)))
  cum+=dt
 check(k+':eight_distinct_frames',len(set(h))==8)
 check(k+':timing_single_shot_clear',cum==a['total_duration_ms'] and not a['loop'] and a['clear_frame_index']==7 and a['clear_on_finish'])
 check(k+':fixed_center',a['anchor_px']==[32,32]);fs[k]=frames
 files.append({'path':a['path'],'sha256':sha(R/a['path']),'size_px':list(im.size)})
check('four_correct_event_types',[a['recommended_event']['event'] for a in m['assets']]==['CardSet','ReactionTriggered','SetCardExpired','SetCardDestroyed'])
check('expiry_not_forced_exhaust','discard/exhaust/hand' in m['assets'][2]['recommended_event']['token_policy'])
check('exclude_whiff_hold_extend',m['precedence']['excluded_events']==['ReactionWhiffed','ReactionHeld','TrapLifeExtended'])
check('no_extra_sound',m['audio']=='none supplied; no new SE invocation implied')
t=json.loads((R/'previews/playback_timing.json').read_text());ap=Image.open(R/'previews/traps_playback.png');dur=[]
check('APNG:40_frames',ap.n_frames==40 and t['frame_count']==40 and ap.size==(650,236))
check('APNG:preview_loop',ap.info.get('loop')==0)
for i,e in enumerate(t['sequence']):
 ap.seek(i);dt=round(ap.info['duration']);dur.append(dt);key=e['effect'];st=e['stage'];active=KEYS.index(key)
 expected_dt=350 if st=='ready' else 700 if st=='hold' else TIMES[key][st]
 check(f'APNG:{i}:duration',dt==e['duration_ms']==expected_dt)
 for j,k in enumerate(KEYS):
  cell=slot() if j>active or (j==active and st=='ready') else composite(k,7 if j<active or st=='hold' else st,fs)
  expect=cell.convert('RGB').resize((128,128),Image.Resampling.NEAREST);got=ap.convert('RGB').crop((13+j*159,64,141+j*159,192))
  check(f'APNG:{i}:{k}:decoded_pixels',got.tobytes()==expect.tobytes())
check('APNG:durations_total',dur==t['durations_ms'] and sum(dur)==sum(sum(v) for v in TIMES.values())+4*(350+700),sum(dur))
check('APNG:no_short_frames',min(dur)>=60)
# deterministic source rebuild in isolated directory
reproduced=[]
with tempfile.TemporaryDirectory(prefix='b3-rebuild-') as tmp:
 tr=Path(tmp)/'bundle';(tr/'source').mkdir(parents=True);shutil.copy2(R/'palette.json',tr/'palette.json')
 for n in ['draw_traps.py','build_manifest.py']:shutil.copy2(R/'source'/n,tr/'source'/n)
 for n in ['draw_traps.py','build_manifest.py']:subprocess.run([sys.executable,str(tr/'source'/n)],check=True,capture_output=True)
 for rel in sorted(expected|{'manifest.json'}|{str(p.relative_to(R)) for p in (R/'previews').glob('*')}):
  check('reproduce:'+rel,sha(R/rel)==sha(tr/rel));reproduced.append(rel)
readme=(R/'README.md').read_text();check('README:limitations','実機再生は未検証' in readme and 'PNG 単独' in readme and '64×38' in readme)
report={'bundle':'B3-traps','verified_date':'2026-11-08','pillow_version':pillow_version,'all_pass':all(c['pass'] for c in checks),'checks_passed':sum(c['pass'] for c in checks),'checks_total':len(checks),'runtime_integration_tested':False,'files':files,'apng_frame_count':ap.n_frames,'apng_total_duration_ms':sum(dur),'recreated_byte_identical_files':reproduced,'checks':checks}
(R/'qa').mkdir(exist_ok=True);(R/'qa/verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
print(f"{report['checks_passed']}/{report['checks_total']} passed; game runtime NOT tested")
if not report['all_pass']:
 print(json.dumps([c for c in checks if not c['pass']],ensure_ascii=False,indent=2));sys.exit(1)

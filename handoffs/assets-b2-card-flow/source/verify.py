#!/usr/bin/env python3
"""B2 artifact QA. Runs no game and changes no game code."""
from pathlib import Path
from PIL import Image,ImageColor,__version__ as pillow_version
import json,hashlib,shutil,subprocess,sys,tempfile,math
from draw_card_flow import KEYS,composite,CARD
R=Path(__file__).resolve().parents[1]
m=json.loads((R/'manifest.json').read_text());p=json.loads((R/'palette.json').read_text());checks=[];files=[]
def check(name,ok,detail=None):checks.append({'check':name,'pass':bool(ok),**({'detail':detail} if detail is not None else {})})
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
colors={ImageColor.getrgb(v) for v in p.values()};overlays={}
expected={a['path'] for a in m['assets']}|{'Art/fx/card_exhaust_keep_mask_8f.png'}
check('exactly_5_effects_plus_1_mask',len(m['assets'])==5 and set(m['assets'][i]['key'] for i in range(5))==set(KEYS) and expected=={str(f.relative_to(R)) for f in (R/'Art/fx').glob('*.png')})
check('declared_native_grid',m['production']['frame_size_px']==[64,96] and m['production']['reference_card_rect_px']==[12,18,40,60])
check('pixel_import_flags',m['production']['filter']=='nearest' and m['production']['mipmaps'] is False and m['production']['trim'] is False)
check('runtime_not_tested',m['qa']['runtime_integration_tested'] is False and m['provenance']['runtime_integration_tested'] is False)
check('no_baked_proxy',m['compositing']['preview_proxy_in_production_assets'] is False)
for a in m['assets']:
 im=Image.open(R/a['path']);frames=[];hashes=[];cum=0
 check(a['key']+':PNG_RGBA_native',im.format=='PNG' and im.mode=='RGBA' and im.size==(512,96))
 check(a['key']+':sha256',sha(R/a['path'])==a['sha256'])
 check(a['key']+':palette',set(v[:3] for v in im.getdata() if v[3])<=colors)
 check(a['key']+':binary_alpha',set(im.getchannel('A').getdata())=={0,255})
 check(a['key']+':transparent_rgb_zero',all(v==(0,0,0,0) for v in im.getdata() if not v[3]))
 for f in range(8):
  cell=im.crop((f*64,0,(f+1)*64,96));frames.append(cell);hashes.append(hashlib.sha256(cell.tobytes()).hexdigest());bb=cell.getbbox();fr=a['frames'][f]
  check(f"{a['key']}:{f}:rect_bbox_time",fr['rect_px']==[f*64,0,64,96] and fr['bbox_local_px']==(list(bb) if bb else None) and fr['start_ms']==cum and 45<=fr['duration_ms']<=95)
  check(f"{a['key']}:{f}:margin_or_clear",bb is None if f==7 else bb and bb[0]>=1 and bb[1]>=1 and bb[2]<=63 and bb[3]<=95,bb)
  pose=fr['recommended_card_pose'];check(f"{a['key']}:{f}:pose_finite",all(math.isfinite(v) for v in pose['offset_px']+pose['scale_xy']+[pose['rotation_clockwise_deg'],pose['opacity']]) and 0<=pose['opacity']<=1)
  cum+=fr['duration_ms']
 check(a['key']+':total_one_shot',cum==a['total_duration_ms'] and a['loop'] is False and a['clear_frame_index']==7 and a['clear_on_finish'])
 check(a['key']+':unique_8_frames',len(set(hashes))==8)
 check(a['key']+':fixed_anchor',a['anchor_px']==[32,48])
 check(a['key']+':final_card_opacity',a['frames'][-1]['recommended_card_pose']['opacity']==(1 if a['key']=='draw' else 0))
 overlays[a['key']]=frames
 files.append({'path':a['path'],'sha256':sha(R/a['path']),'size':list(im.size),'nontransparent_pixels':sum(1 for v in im.getchannel('A').getdata() if v)})
maskp=R/'Art/fx/card_exhaust_keep_mask_8f.png';mask=Image.open(maskp);prev=None
check('mask:RGBA_atlas_size',mask.mode=='RGBA' and mask.size==(512,96))
check('mask:white_or_transparent',set(mask.getdata())=={(0,0,0,0),(255,255,255,255)})
check('mask:manifest_hash',sha(maskp)==m['assets'][0]['optional_keep_mask']['sha256'])
for f in range(8):
 pix=mask.crop((f*64,0,(f+1)*64,96));coords={(x,y) for y in range(96) for x in range(64) if pix.getpixel((x,y))[3]}
 check(f'mask:{f}:inside_reference',all(12<=x<52 and 18<=y<78 for x,y in coords))
 check(f'mask:{f}:monotonic_burn',prev is None or coords<=prev)
 check(f'mask:{f}:correct_empty_state',bool(coords)==(f<6))
 if f==0:check('mask:frame0_full_card',len(coords)==2400)
 prev=coords
files.append({'path':str(maskp.relative_to(R)),'sha256':sha(maskp),'size':list(mask.size)})
check('semantics:expiry_is_discard',m['assets'][3]['destination']=='discard' and m['assets'][3]['recommended_event']['event']=='ImpulseReturned')
check('semantics:kindle_targets_fuel',m['assets'][4]['recommended_event']['selector']=='cardIds[]' and m['assets'][4]['destination']=='exhaust')
check('semantics:event_dedup',set(m['precedence'])=={'kindle','expiry','draw'})
timing=json.loads((R/'previews/playback_timing.json').read_text());apng=Image.open(R/'previews/card_flow_playback.png');dur=[]
check('APNG:50_frames',apng.n_frames==50 and timing['frame_count']==50 and apng.size==(772,284))
check('APNG:preview_loop_only',apng.info.get('loop')==0)
for i,item in enumerate(timing['sequence']):
 apng.seek(i);dt=round(apng.info['duration']);dur.append(dt)
 key=item['effect'];stage=item['stage'];active=KEYS.index(key)
 expected_dt=350 if stage=='ready' else 700 if stage=='hold' else m['assets'][active]['frames'][stage]['duration_ms']
 check(f'APNG:{i}:exact_timing',dt==item['duration_ms']==expected_dt)
 # Decode actual APNG composition. Verify each visual panel versus the native
 # declared pose/effect, not just the preview's file metadata.
 for j,k in enumerate(KEYS):
  baseline=j>active or (j==active and stage=='ready')
  if baseline:
   cell=Image.new('RGBA',(64,96),'#0c1525')
   if k!='draw':cell.alpha_composite(CARD,(12,18))
  else:cell=composite(k,7 if j<active or stage=='hold' else stage,overlays)
  expected_cell=cell.convert('RGB').resize((128,192),Image.Resampling.NEAREST)
  got=apng.convert('RGB').crop((13+j*153,65,13+j*153+128,257))
  check(f'APNG:{i}:{k}:decoded_pixels',got.tobytes()==expected_cell.tobytes())
check('APNG:no_sub45ms_frames',min(dur)>=45)
check('APNG:duration_list_and_total',dur==timing['durations_ms'] and sum(dur)==sum(a['total_duration_ms'] for a in m['assets'])+5*(350+700),sum(dur))
# Deterministic reproduction in isolated temp tree, no overwriting delivery.
recreated=[]
with tempfile.TemporaryDirectory(prefix='b2-rebuild-') as t:
 tr=Path(t)/'bundle';(tr/'source').mkdir(parents=True);shutil.copy2(R/'palette.json',tr/'palette.json')
 for name in ['draw_card_flow.py','build_manifest.py']:shutil.copy2(R/'source'/name,tr/'source'/name)
 for name in ['draw_card_flow.py','build_manifest.py']:subprocess.run([sys.executable,str(tr/'source'/name)],check=True,capture_output=True)
 for rel in sorted(expected|{'manifest.json'}|{str(f.relative_to(R)) for f in (R/'previews').glob('*')}):
  check('reproduce:'+rel,sha(R/rel)==sha(tr/rel));recreated.append(rel)
readme=(R/'README.md').read_text();check('README:limitations_explicit','実機再生は未検証' in readme and 'PNG 単独' in readme)
report={'bundle':'B2-card-flow','verified_date':'2026-11-08','pillow_version':pillow_version,'all_pass':all(c['pass'] for c in checks),'checks_passed':sum(c['pass'] for c in checks),'checks_total':len(checks),'runtime_integration_tested':False,'files':files,'apng_frame_count':apng.n_frames,'apng_total_duration_ms':sum(dur),'recreated_byte_identical_files':recreated,'checks':checks}
(R/'qa').mkdir(exist_ok=True);(R/'qa/verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
print(f"{report['checks_passed']}/{report['checks_total']} passed; game runtime NOT tested")
if not report['all_pass']:
 print(json.dumps([c for c in checks if not c['pass']],ensure_ascii=False,indent=2));sys.exit(1)

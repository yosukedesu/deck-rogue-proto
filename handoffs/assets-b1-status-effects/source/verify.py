#!/usr/bin/env python3
"""Technical QA, no game runtime claims. Rebuild in temporary tree, never overwrite art."""
from pathlib import Path
from PIL import Image,ImageColor,__version__ as pillow_version
from collections import Counter
import json,hashlib,tempfile,shutil,subprocess,sys
R=Path(__file__).resolve().parents[1]
m=json.loads((R/'manifest.json').read_text());p=json.loads((R/'palette.json').read_text())
colors={ImageColor.getrgb(c) for c in p.values()};checks=[];files=[]
def check(name,ok,details=None):
 checks.append({'check':name,'pass':bool(ok),**({'details':details} if details is not None else {})})
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
expected={a[t]['path'] for a in m['assets'] for t in ('application','mark')}
check('exactly_18_native_assets',len(expected)==18 and {str(p.relative_to(R)) for p in (R/'Art/fx').glob('*.png')}==expected)
check('nine_target_keys',set(a['key'] for a in m['assets'])=={'enemy_weak','vulnerable','frail','restrain','mist','slow','wound','scald','confusion'})
check('eight_durations_sum_620',m['playback']['frame_durations_ms']==[65,65,70,80,85,85,100,70] and m['playback']['total_duration_ms']==620)
check('no_loop_or_stuck_last_frame',m['playback']['loop'] is False and m['playback']['clear_on_finish'] and m['playback']['clear_frame_index']==7)
check('mark_handoff_at_clear_frame',m['playback']['mark_start_ms']==550 and m['playback']['mark_start_frame_index']==7)
check('runtime_unverified_declared',m['runtime_integration_tested'] is False)
allhash=[]
for a in m['assets']:
 key=a['key'];card=key in ('wound','scald')
 check(f'{key}:target_and_anchor',a['target_kind']==('card' if card else ('enemy' if key in ('enemy_weak','confusion') else 'player')) and a['anchor_px']==([32,32] if card else [32,52]))
 for typ in ('application','mark'):
  path=R/a[typ]['path'];im=Image.open(path);alpha=im.getchannel('A');h=sha(path);allhash.append(h)
  check(f'{key}:{typ}:PNG_RGBA_size',im.format=='PNG' and im.mode=='RGBA' and list(im.size)==a[typ]['size'])
  check(f'{key}:{typ}:sha256',h==a[typ]['sha256'])
  check(f'{key}:{typ}:alpha_binary_with_transparency',set(alpha.getdata())=={0,255})
  used={v[:3] for v in im.getdata() if v[3]};check(f'{key}:{typ}:palette',used<=colors)
  check(f'{key}:{typ}:transparent_RGB_zero',all(v==(0,0,0,0) for v in im.getdata() if not v[3]))
  bboxes=[];fh=[]
  for i in range(8 if typ=='application' else 1):
   cell=im.crop((i*64,0,(i+1)*64,64));bb=cell.getbbox();bboxes.append(bb);fh.append(hashlib.sha256(cell.tobytes()).hexdigest())
   if typ=='application' and i==7:
    check(f'{key}:frame7_clear',bb is None)
   else:
    check(f'{key}:{typ}:{i}:nonempty_safe_margin',bb is not None and min(bb[:2])>=2 and max(bb[2:])<=62,bb)
   if typ=='application':check(f'{key}:frame{i}:manifest_rect_duration_bbox',a[typ]['frames'][i]['rect']==[i*64,0,64,64] and a[typ]['frames'][i]['duration_ms']==m['playback']['frame_durations_ms'][i] and a[typ]['frames'][i]['bbox_local']==(list(bb) if bb else None))
  check(f'{key}:{typ}:no_duplicate_cells',len(fh)==len(set(fh)))
  if typ=='mark':check(f'{key}:mark_bbox',a['mark']['bbox_local']==list(im.getbbox()))
  files.append({'path':a[typ]['path'],'sha256':h,'size':list(im.size),'colors':len(used),'nontransparent_pixels':sum(1 for v in alpha.getdata() if v),'cell_bboxes':bboxes})
check('all_18_assets_unique',len(allhash)==len(set(allhash)))
prev=Image.open(R/'previews/status_application_preview.png');dur=[];apnghash=[]
for i in range(prev.n_frames):
 prev.seek(i);dur.append(round(prev.info.get('duration',0)));apnghash.append(hashlib.sha256(prev.convert('RGBA').tobytes()).hexdigest())
check('APNG_10_full_frames',prev.n_frames==10 and prev.size==(624,696))
check('APNG_timing',dur==[65,65,70,80,85,85,100,70,900,220] and sum(dur)==1740,dur)
check('APNG_frames_distinct',len(apnghash)==len(set(apnghash)))
check('APNG_preview_loop_only',prev.info.get('loop')==0)
check('README_native_and_runtime_limitations','実機' in (R/'README.md').read_text() and '未検証' in (R/'README.md').read_text())
# Byte-perfect recreation includes PNG compression, pixel colors, previews and manifest.
recreated=[]
with tempfile.TemporaryDirectory(prefix='b1-rebuild-') as t:
 tr=Path(t)/'bundle';(tr/'source').mkdir(parents=True)
 for n in ('draw_status_fx.py','build_manifest.py'):shutil.copy2(R/'source'/n,tr/'source'/n)
 for n in ('draw_status_fx.py','build_manifest.py'):subprocess.run([sys.executable,str(tr/'source'/n)],check=True,capture_output=True)
 for rel in sorted(expected|{'palette.json','manifest.json'}|{str(p.relative_to(R)) for p in (R/'previews').glob('*.png')}):
  same=sha(R/rel)==sha(tr/rel);check(f'reproduce:{rel}',same);recreated.append(rel)
report={'bundle':'B1-status-application','verified_date':'2026-11-08','pillow_version':pillow_version,'checks_passed':sum(c['pass'] for c in checks),'checks_total':len(checks),'all_pass':all(c['pass'] for c in checks),'runtime_integration_tested':False,'visual_review_is_separate':'qa/visual_review.md','files':files,'apng_durations_ms':dur,'recreated_byte_identical_files':recreated,'checks':checks}
(R/'qa').mkdir(exist_ok=True);(R/'qa/verification.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
print(f"{report['checks_passed']}/{report['checks_total']} checks passed; runtime not tested")
if not report['all_pass']:
 print(json.dumps([c for c in checks if not c['pass']],indent=2));sys.exit(1)

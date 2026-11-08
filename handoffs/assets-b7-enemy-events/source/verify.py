#!/usr/bin/env python3
from pathlib import Path
from PIL import Image,ImageChops,ImageColor
import json,hashlib,subprocess,sys
R=Path(__file__).resolve().parents[1]
m=json.loads((R/'manifest.json').read_text());palette={ImageColor.getrgb(x)+(255,) for x in json.loads((R/'palette.json').read_text()).values()}
checks=[]
for a in m['assets']:
 p=R/a['file'];im=Image.open(p);assert im.mode=='RGBA' and im.size==(512,64),a['id']
 assert set(im.getchannel('A').getdata())=={0,255}
 assert all(px in palette for px in set(im.getdata()) if px[3])
 assert hashlib.sha256(p.read_bytes()).hexdigest()==a['sha256']
 assert a['total_duration_ms']==sum(a['duration_ms'])
 assert len(a['frames'])==8 and all(fr['rect']==[i*64,0,64,64] for i,fr in enumerate(a['frames']))
 distinct=[];bboxes=[]
 for f in range(8):
  fr=im.crop((f*64,0,(f+1)*64,64));box=fr.getbbox();bboxes.append(box);distinct.append(hashlib.sha256(fr.tobytes()).hexdigest())
  if f==7:assert box is None
  else:
   assert box is not None and box[0]>=2 and box[1]>=2 and box[2]<=62 and box[3]<=62,(a['id'],f,box)
 assert len(set(distinct))==8
 ap=Image.open(R/f"previews/{a['id']}_playback.png");assert ap.n_frames==9
 durations=[]
 for i in range(9):
  ap.seek(i);durations.append(round(ap.info['duration']))
  if i<8:
   bg=Image.new('RGBA',(64,64),'#17283f');bg.alpha_composite(im.crop((i*64,0,(i+1)*64,64)))
   actual=ap.convert('RGB').crop((151,40,215,104));assert ImageChops.difference(actual,bg.convert('RGB')).getbbox() is None,(a['id'],i)
 assert durations==a['duration_ms']+[800],(a['id'],durations)
 checks.append({'id':a['id'],'size':list(im.size),'frames_unique':8,'alpha':[0,255],'bboxes':bboxes,'duration_ms':a['total_duration_ms'],'apng_frames':9,'apng_timings_exact':True,'apng_pixels_match_production':True,'pass':True})
files=sorted([p for folder in ['Art','previews'] for p in (R/folder).rglob('*.png')]+[R/'manifest.json',R/'previews/playback_timing.json'])
before={str(p.relative_to(R)):hashlib.sha256(p.read_bytes()).hexdigest() for p in files}
subprocess.run([sys.executable,str(R/'source/draw_enemy_events.py')],check=True);subprocess.run([sys.executable,str(R/'source/build_manifest.py')],check=True)
after={str(p.relative_to(R)):hashlib.sha256(p.read_bytes()).hexdigest() for p in files};assert before==after
report={'result':'PASS','asset_count':10,'production_frames':80,'canvas':{'frame':[64,64],'strip':[512,64]},'reproduction_byte_exact':True,'production_and_preview_files_checked':len(files),'runtime_integration_tested':False,'assets':checks}
(R/'qa/verification.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({'result':'PASS','assets':10,'frames':80,'reproduction':'byte exact','runtime':'not tested'}))

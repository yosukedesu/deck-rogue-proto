#!/usr/bin/env python3
from pathlib import Path
from PIL import Image,ImageDraw
import json,hashlib,math,subprocess,sys
from draw_enemy_contacts import SPECS,P
R=Path(__file__).resolve().parents[1]
checks=0;failures=[];details={}
def check(ok,label):
 global checks
 checks+=1
 if not ok:failures.append(label)
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
manifest=json.loads((R/'manifest.json').read_text())
check(len(manifest['assets'])==8,'eight assets')
check({a['id'] for a in manifest['assets']}=={'enemy_'+n+'_contact' for n,l,t in SPECS},'exact eight identifiers')
for asset,(name,label,times) in zip(manifest['assets'],SPECS):
 im=Image.open(R/asset['file']);data=json.loads((R/asset['mesh_file']).read_text())
 check(im.mode=='RGBA' and im.size==(512,64),name+' PNG format')
 check(set(im.getchannel('A').getdata())=={0,255},name+' alpha binary')
 check(set(im.getdata())<=set(P.values())|{(0,0,0,0)},name+' palette')
 check(sum(times)==asset['total_duration_ms'],name+' total time')
 check(len(times)==8 and all(x>0 for x in times),name+' positive durations')
 check(sha(R/asset['file'])==asset['sha256'],name+' sha')
 check(sha(R/asset['mesh_file'])==asset['mesh_sha256'],name+' mesh sha')
 check(data['duration_ms']==times,name+' mesh timing')
 frames=[];num_pixels=[];max_vertices=0;max_expanded_vertices=0
 for f in range(8):
  fr=im.crop((f*64,0,(f+1)*64,64));frames.append(fr)
  bbox=fr.getbbox();num_pixels.append(sum(1 for p in fr.getdata() if p[3]))
  check((bbox is None) == (f==7),name+f' frame {f} endpoint')
  if bbox:check(bbox[0]>=2 and bbox[1]>=2 and bbox[2]<=62 and bbox[3]<=62,name+f' frame {f} margin')
  mesh=data['frames'][f];v=mesh['vertices'];uv=mesh['uv'];idx=mesh['indices'];max_vertices=max(max_vertices,len(v));max_expanded_vertices=max(max_expanded_vertices,len(idx))
  check(len(v)==len(uv),name+f' frame {f} paired vertices/uv')
  check(len(idx)%3==0 and all(isinstance(i,int) and 0<=i<len(v) for i in idx),name+f' frame {f} valid triangle indices')
  check(all(all(math.isfinite(q) for q in p) for p in v+uv),name+f' frame {f} finite geometry')
  check(all(len(p)==3 and p[2]==0 for p in v),name+f' frame {f} planar geometry')
  check(all(len(p)==2 and 0<=p[0]<=1 and 0<=p[1]<=1 for p in uv),name+f' frame {f} valid UV')
  check(all(abs((p[0]*64+32)-round(p[0]*64+32))<1e-5 and abs((32-p[1]*64)-round(32-p[1]*64))<1e-5 for p in v),name+f' frame {f} native grid')
  check(all(abs(u[0]*512-(p[0]*64+32+64*f))<1e-4 and abs(u[1]*64-(32-p[1]*64))<1e-4 for p,u in zip(v,uv)),name+f' frame {f} atlas mapping')
  # Reconstruct the exact covered pixel centers from row-run quads, independent of PNG alpha.
  cover=Image.new('L',(64,64));draw=ImageDraw.Draw(cover)
  area=0
  for n in range(0,len(v),4):
   x0=round(v[n][0]*64+32);y0=round(32-v[n][1]*64);x1=round(v[n+2][0]*64+32);y1=round(32-v[n+2][1]*64)
   check(x1>x0 and y1==y0+1,name+f' frame {f} rowrun {n//4}')
   draw.rectangle((x0,y0,x1-1,y1-1),fill=255);area+=(x1-x0)*(y1-y0)
  check(cover.tobytes()==fr.getchannel('A').tobytes(),name+f' frame {f} mesh coverage exact')
  check(area==num_pixels[-1],name+f' frame {f} no overdraw')
 check(len({fr.tobytes() for fr in frames})==8,name+' all frames differ')
 apng=Image.open(R/f'previews/enemy_{name}_contact_playback.png')
 check(apng.n_frames==9,name+' APNG frames')
 for f,ms in enumerate(times+[800]):
  apng.seek(f);check(abs(apng.info['duration']-ms)<.01,name+f' APNG time {f}')
  if f<8:
   # Original-size preview panel uses alternating backgrounds exactly as production preview.
   bg=Image.new('RGBA',(64,64),'#0c1525' if f%2==0 else '#17283f');bg.alpha_composite(frames[f])
   check(apng.convert('RGB').crop((151,40,215,104)).tobytes()==bg.convert('RGB').tobytes(),name+f' APNG image {f}')
 check(asset['mapping_status']==('unmapped_reserved_art' if name in ('poison','cold') else 'candidate_requiring_proven_execution'),name+' mapping status')
 details[name]={'opaque_pixels':num_pixels,'max_indexed_vertices_per_frame':max_vertices,'max_expanded_triangle_vertices_per_frame':max_expanded_vertices,'total_ms':sum(times),'max_422_triangle_soup_batches_512_capacity':math.ceil(max_expanded_vertices/510)}
check('TIME' not in (R/'godot/enemy_contact.gdshader').read_text(),'shader external clock')
before={str(p.relative_to(R)):sha(p) for d in ['Art','Geometry','previews'] for p in (R/d).rglob('*') if p.is_file()}
subprocess.run([sys.executable,str(R/'source/draw_enemy_contacts.py')],check=True)
after={str(p.relative_to(R)):sha(p) for d in ['Art','Geometry','previews'] for p in (R/d).rglob('*') if p.is_file()}
check(before==after,'deterministic complete regeneration')
g=json.loads((R/'qa/godot_verification.json').read_text());check(g['failures']==[] and g['checks']==176,'Godot sampler smoke')
result={'checks':checks,'failures':failures,'assets':details,'deterministic_regeneration':before==after,'game_runtime_tested':False,'gpu_render_tested':False,'geometry':'fixed-frame indexed row-run mesh; not continuous 422 geometry'}
(R/'qa/verification.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps({'checks':checks,'failures':failures}));sys.exit(bool(failures))

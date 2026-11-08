#!/usr/bin/env python3
"""Artifact checks only. This does not launch Unity or test map interactions."""
from pathlib import Path
from PIL import Image
import json,hashlib,subprocess,sys,tempfile
ROOT=Path(__file__).resolve().parents[1]
results=[]
def check(name,condition,details=None):
 results.append({'check':name,'pass':bool(condition),'details':details})
def sha(path):return hashlib.sha256(Path(path).read_bytes()).hexdigest()
def main():
 m=json.loads((ROOT/'manifest.json').read_text());assets=m['assets']
 check('new_count',sum(not x.get('reused',False) for x in assets)==24)
 check('reused_count',sum(x.get('reused',False) for x in assets)==8)
 check('unique_ids',len(set(x['id'] for x in assets))==32)
 check('png_inventory',set(p.name for p in (ROOT/'png').glob('*.png'))==set(Path(x['file']).name for x in assets))
 for a in assets:
  p=ROOT/a['file'];im=Image.open(p);key=a['id'];check(key+':size',list(im.size)==a['size'])
  check(key+':RGBA',im.mode=='RGBA');check(key+':sha256',sha(p)==a['sha256'])
  alpha=im.getchannel('A');av=set(alpha.getdata());bbox=alpha.getbbox()
  check(key+':binary_alpha',av.issubset({0,255}));check(key+':bbox',list(bbox)==a['visibleBoundsXYXY'])
  check(key+':filter',a['filterMode']=='Point')
  if a['kind']=='tile':
   check(key+':opaque',av=={255});check(key+':repeat',a['wrapMode']=='Repeat')
   check(key+':LR',list(im.crop((0,0,1,64)).getdata())==list(im.crop((63,0,64,64)).getdata()))
   check(key+':TB',list(im.crop((0,0,64,1)).getdata())==list(im.crop((0,63,64,64)).getdata()))
  else:
   check(key+':transparent_margin',bbox[0]>0 and bbox[1]>0 and bbox[2]<im.width and bbox[3]<im.height)
   check(key+':clamp',a['wrapMode']=='Clamp')
   if not a.get('reused',False):check(key+':ground_boundary',bbox[3]==a['anchorPixels'][1])
  if a.get('reused',False):check(key+':A3_byte_identity',a['reuseSource']['sha256']==sha(p))
 for act in (2,3):
  aa=Image.open(ROOT/'png'/f'act{act}_ground_a.png');bb=Image.open(ROOT/'png'/f'act{act}_ground_b.png')
  check(f'act{act}:mixedLR',list(aa.crop((63,0,64,64)).getdata())==list(bb.crop((0,0,1,64)).getdata()))
  check(f'act{act}:mixedTB',list(aa.crop((0,63,64,64)).getdata())==list(bb.crop((0,0,64,1)).getdata()))
  check(f'act{act}:variants_distinct',aa.tobytes()!=bb.tobytes())
  enc={a['encounterId'] for a in assets if a.get('act')==act and a.get('encounterId')}
  expected={2:{'enemy_turtle','enc_kaiser_crab','enemy_winch_warden'},3:{'enemy_warden','enemy_chimera_1','enemy_ember_furnace'}}[act]
  check(f'act{act}:boss_ids',enc==expected)
 with tempfile.TemporaryDirectory() as td:
  for script in ('draw_tiles.py','draw_act2.py','draw_act3.py'):
   r=subprocess.run([sys.executable,str(ROOT/'source'/script),td],capture_output=True,text=True)
   check(script+':runs',r.returncode==0,r.stderr[-400:] if r.returncode else None)
  for a in assets:
   if not a.get('reused',False):
    p=Path(td)/Path(a['file']).name
    check(a['id']+':source_reproduces_bytes',p.exists() and sha(p)==a['sha256'])
 check('imported_false',m['imported'] is False);check('runtimeVerified_false',m['runtimeVerified'] is False)
 out={'artifactOnly':True,'passed':sum(x['pass'] for x in results),'failed':sum(not x['pass'] for x in results),'checks':results,'notTested':['Unity import','actual map loader','click targets','route overlap with all generated seeds','Android','gameplay']}
 (ROOT/'qa').mkdir(exist_ok=True);(ROOT/'qa'/'validation.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n')
 print(json.dumps({k:v for k,v in out.items() if k!='checks'},ensure_ascii=False));return int(out['failed']>0)
if __name__=='__main__':raise SystemExit(main())

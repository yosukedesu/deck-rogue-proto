from pathlib import Path
import json,math
p=Path('evidence/unity-imp-final/burst-projection/continuous-clock.json');rows=json.loads(p.read_text(encoding='utf8'));out=[]
def box(v):return [min(x[0] for x in v),min(720-x[1] for x in v),max(x[0] for x in v),max(720-x[1] for x in v)]
for f in rows:
 if f['index'] not in [7,8,9,10,11,12,15]:continue
 for e in f['localEffects']:
  rp=e.get('receiverProjection')
  if not e['busy'] or not e['ownedBurst'] or not rp:continue
  for d in e['draws']:
   if d['kind']!=41:continue
   vs=[v for v in d['samples'] if v[7]>0]
   if not vs:continue
   main=box([[v[9],v[10]] for v in vs]);alpha=box(rp['alphaCornersBottomOrigin']);canvas=box(rp['canvasCornersBottomOrigin']);anchor=rp['contactBottomOrigin'];delta=[(main[0]+main[2])/2-anchor[0],(main[1]+main[3])/2-(720-anchor[1])];h=alpha[3]-alpha[1]
   out.append({'frame':f['index'],'target':e['target'],'age':e['age'],'rootScale':e['rootScale'],'capturedHeight':e['height'],'size':e['size'],'axis':e['axis'],'sprite':rp,'mainBoxTopOrigin':main,'spriteAlphaBoxTopOrigin':alpha,'spriteCanvasBoxTopOrigin':canvas,'mainSize':[main[2]-main[0],main[3]-main[1]],'mainBoxCenterMinusRootPixels':delta,'normalizedByAlphaHeight':{'width':(main[2]-main[0])/h,'height':(main[3]-main[1])/h,'dx':delta[0]/h,'dy':delta[1]/h,'distance':math.hypot(*delta)/h}})
Path('evidence/burst-actor-normalized.json').write_bytes(json.dumps(out,indent=2).encode());print(json.dumps(out[:1],indent=2))

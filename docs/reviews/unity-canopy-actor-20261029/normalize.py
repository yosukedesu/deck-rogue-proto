from pathlib import Path
import json,math
r=Path('evidence/unity-imp-cues');out={}
def box(v):
 xs=[x[0] for x in v];ys=[720-x[1] for x in v];return [min(xs),min(ys),max(xs),max(ys)]
for n in ['canopy-baseline','canopy-candidate']:
 p=r/n/'continuous-clock.json'
 if not p.exists():continue
 rows=json.loads(p.read_text());data=[]
 for f in rows:
  if f['index'] not in [7,8,9,10,11,12,15]:continue
  for b in f['canopyProjection']:
   rp=b.get('receiverProjection');vs=[v for v in b['vertices'] if v['role']==1 and v['alpha']>0]
   if not rp or not vs:continue
   main=box([v['screenBottomOrigin'] for v in vs]);canvas=box(rp['canvasCornersBottomOrigin']);alpha=box(rp['alphaCornersBottomOrigin']) if 'alphaCornersBottomOrigin' in rp else None
   anchor=rp['contactBottomOrigin'];center=[(main[0]+main[2])/2,(main[1]+main[3])/2];delta=[center[0]-anchor[0],center[1]-(720-anchor[1])]
   h=(alpha[3]-alpha[1]) if alpha else None
   data.append({'frame':f['index'],'target':b['target'],'age':b['age'],'sprite':rp,'mainBoxTopOrigin':main,'spriteCanvasBoxTopOrigin':canvas,'spriteAlphaBoxTopOrigin':alpha,'mainSize':[main[2]-main[0],main[3]-main[1]],'mainBoxCenterMinusContactPixels':delta,'normalizedByAlphaHeight':{'width':(main[2]-main[0])/h,'height':(main[3]-main[1])/h,'dx':delta[0]/h,'dy':delta[1]/h,'distance':math.hypot(*delta)/h} if h else None})
 out[n]=data
Path('evidence/canopy-actor-normalized.json').write_bytes(json.dumps(out,indent=2).encode());print(json.dumps({k:v[:1] for k,v in out.items()},indent=2))

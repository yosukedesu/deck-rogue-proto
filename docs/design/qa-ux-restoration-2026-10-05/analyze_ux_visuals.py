from pathlib import Path
import json,subprocess
import numpy as np
from PIL import Image
w=Path('/mnt/c/Users/yosuke/Documents/Codex/2026-10-05/task');r=Path('/home/yosuke/projects/deck-rogue-proto')
out={'worldLightSourceUnchanged':not subprocess.check_output(['git','diff','--','unity/Assets/Game/StageFx.cs'],cwd=r).strip(),'comparisons':[]}
for el,f in [('lightning',25),('light',21),('dark',25),('fire',21)]:
 a=np.asarray(Image.open(w/'ux-fixed-post-ab'/f'{el}-{f}.png').convert('RGB')).astype(float)
 b=np.asarray(Image.open(w/'ux-final-element-review'/f'{el}-{f}.png').convert('RGB')).astype(float)
 roi=(slice(230,420),slice(1200,1490));floor=(slice(380,420),slice(1010,1210));d=(b-a)[roi];l=d@np.array([.2126,.7152,.0722]);
 row={'element':el,'frame':f,'targetRoi':[1200,230,1490,420],'targetMeanRgbLumaDelta':float(l.mean()),'pixelsBrighterOver1':int((l>1).sum()),'pixelsDarkerBelowMinus1':int((l< -1).sum()),'targetMaxAbsRgbDelta':float(np.abs(d).max()),'floorRoi':[1010,380,1210,420],'floorOldMean':float(a[floor].mean()),'floorNewMean':float(b[floor].mean()),'allChangedPixelPercent':float(np.any(a!=b,axis=2).mean()*100)};out['comparisons'].append(row)
# Analytic coverage partition only, not a measured lux claim.
a=.447;q=np.clip((a-.15)/(.90-.15),0,1);body=a*q*q*(3-2*q);halo=a-body
out['exampleAlphaPartition']={'sourceAlpha':a,'bodyCoverage':float(body),'haloEnergyBeforeGain':float(halo),'sum':float(body+halo),'backdropTransmissionOld':1-a,'backdropTransmissionNew':float(1-body),'haloGain':4,'note':'Body/halo share atlas UV, pool slot, position, and lifetime. Halo is added before bloom; body after bloom. Two partitioned draws, not two complete image draws.'}
d=json.loads((w/'ux-fixed-idle-anchor-review/idle-runtime.json').read_text());obs=d['observations'];out['idleRuntime']={'observations':len(obs),'uniqueFeetPositions':len({tuple(o['feet']) for o in obs}),'uniqueHeights':len({o['height'] for o in obs}),'allImports128x84Pivot60x8':all(i['width']==128 and i['height']==84 and i['pivot']==[60,8] for i in d['imports']),'allEightFrames':sorted({int(o['anim'].split('#')[1]) for o in obs})}
(w/'ux-visual-comparison.json').write_text(json.dumps(out,indent=2));print(json.dumps(out,indent=2))

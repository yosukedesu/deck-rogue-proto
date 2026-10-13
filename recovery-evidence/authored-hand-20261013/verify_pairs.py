from pathlib import Path
import json,hashlib,subprocess,sys
b=Path(__file__).resolve().parent;root=b.parent.parent;rows=[]
for style in ['slash','water','stomp']:
 for scale in ['1.0','1.3']:
  dirs=[b/f'{r}-{style}-{scale}' for r in ['baseline','candidate']]
  if not all((p/'exit.txt').exists() for p in dirs):continue
  report=b/f'{style}-{scale}-comparison.json'
  subprocess.run([sys.executable,str(root/'ui-parity-restoration-20261013/hand-native-telemetry-qa/analyze.py'),str(dirs[0]/'hand-native-trace'),'--candidate',str(dirs[1]/'hand-native-trace'),'--expected',str(root/f'ui-parity-restoration-20261013/authored-hand-native-fixtures/{style}-expected-after.save.json'),'--out',str(report)],check=True,stdout=subprocess.DEVNULL)
  frames=[[json.loads(x) for x in (p/'frames.jsonl').read_text().splitlines()] for p in dirs]
  issues=[]
  stable=['actors','contacts','camera','energy','heroHp','enemyHp','Revision','InputLocked','IsPresenting','clock','ActiveMotionCount','ActiveEffectCount','SourceCastLightStrength','SourceCombatFloatCount','SourceCombatFloatsCreated','LastSourceCombatFloatText']
  for f,g in zip(*frames):
   for k in stable:
    if f[k]!=g[k]:issues.append({'frame':f['frame'],'field':k})
  summary=[]
  for p,fs in zip(dirs,frames):
   hashes=all(hashlib.sha256((p/f['png']).read_bytes()).hexdigest()==f['sha256'] for f in fs)
   consecutive=[f['frame'] for f in fs]==list(range(140)) and all(fs[i]['processFrame']==fs[i-1]['processFrame']+1 for i in range(1,len(fs)))
   nums=[c for f in fs for c in f['contacts'] if c['kind']=='numeric'];last=max(c['PresentationTime'] for c in nums)
   end=fs[-1];settled=not end['IsPresenting'] and end['ActiveMotionCount']==0 and end['ActiveEffectCount']==0 and end['ActiveHandDealCount']==0 and end['ActiveHandExitCount']==0 and end['SourceCombatFloatCount']==0
   summary.append({'run':p.name,'pngHashes':hashes,'consecutive140':consecutive,'lastNumericClock':last,'finalClock':end['clock'],'settled':settled,'postContactSeconds':end['clock']-last,'scope':'fixed60 native process/draw, not live performance'})
   assert hashes and consecutive and settled and end['clock']-last>=.9
  r={'style':style,'scale':scale,'unchangedActorContactStateFields':stable,'differences':issues,'runs':summary,'traceAnalysis':json.loads(report.read_text()),'visualAndPointerAcceptance':'separate owner review and actual-pointer window'}
  assert not issues,issues[:5]
  (b/f'{style}-{scale}-native-regression.json').write_text(json.dumps(r,indent=2));rows.append({'style':style,'scale':scale,'checksPass':True})
print(json.dumps(rows))

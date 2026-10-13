from pathlib import Path
import json,re,hashlib
W=Path(__file__).resolve().parents[3];R=W/'unity-parity-20261013/ui-rich-r2';F=W/'ui-parity-restoration-20261013/float-size-native-fixtures';OUT=Path(__file__).parent
geom=json.loads((F/'expected-geometry.json').read_text())['fixtures'];summaries=[]
def vec(s):return [float(x) for x in re.findall(r'-?\d+(?:\.\d+)?',s)]
for run,fixture,gi,role in [('float-phone100','phone-four',0,'100'),('float-phone130','phone-four',0,'130'),('float-desktop-correct-1.0','desktop-128',1,'100'),('float-desktop-correct-1.3','desktop-128',1,'130')]:
 p=R/run
 if not (p/'results.json').exists():continue
 result=json.loads((p/'results.json').read_text());assert result['passed'] and not result['remainingPresentation'] and result['consecutiveFrames']
 frames=[json.loads(x) for x in (p/'frames.jsonl').read_text().splitlines()];checks=[]
 def check(v,s):assert v,(run,s);checks.append(s)
 expected=json.loads((F/f'{fixture}-expected-after.save.json').read_text());actual=json.loads((p/'final-save.json').read_text());adapter=actual.pop('godotSession',None);check(actual==expected,'Complete original Report RunSave outcome matches; only Godot session adapter omitted')
 g=geom[gi];count=g['expectedCreatedFloatCount'];check(frames[-1]['SourceCombatFloatsCreated']==count,'Exactly one float per expected target');check(frames[-1]['SourceCombatFloatCount']==0,'All float lifetimes retired');check(frames[-1]['energy']==28 and frames[-1]['enemyHp']==[994]*count,'Original command outcome: energy28/enemies994')
 previous=0;contacts=[];maxerr=0
 for f in frames:
  check(f['EnemyHeadReadbacks']==count and f['EnemyHeadCacheEntries']==count and f['EnemyHeadCacheEvictions']==0,'Unique base textures cached without ordinary rebuild readback')
  for a in f['actors']:
   i=a['EnemyIndex']
   if i<0:continue
   e=g['seats'][i];check(a['DefinitionId']==e['enemyId'],'Correct actor seat identity');check(abs(a['SourceUiProxyHeight']-e['sourceUiProxyHeightLogical'])<.001,'Source base-art proxy height');check(abs(a['SourceUiProxyUnit']-e['sourceUiProxyUnit'])<.00001,'Source logical/render unit')
  for z in f['floats']:
   check(z['Text']=='6' and z['color']=='ead08aff' and z['fontSize']==g['expectedFontSizeByRole'][role],'Frozen number/sign/color/font role')
  n=f['SourceCombatFloatsCreated']
  if n>previous:
   for i in range(previous,n):
    cue=f['sourceFloatCues'][i];origin=vec(cue['origin']);a=next(a for a in f['actors'] if a['EnemyIndex']==i)
    expectedorigin=[a['sourceProxyFeet'][0],a['sourceProxyFeet'][1]-a['SourceUiProxyHeight']*(.5*a['executionScale'][1]+.1)-20*a['SourceUiProxyUnit']]
    error=max(abs(x-y) for x,y in zip(origin,expectedorigin));maxerr=max(maxerr,error);check(error<.001,'Immutable creation origin follows projected source proxy')
    ev=[json.loads(c['eventData']) for c in f['contacts'] if c['frame']==f['frame'] and c['kind']=='numeric' and c['type']=='DamageDealt'];check(any(e['enemyIndex']==i and e['amount']==6 and e['hpLoss']==6 and e['presentationSource']['uid']==f'float-size-aoe-{fixture}' for e in ev),'Float creation shares exact original UID numeric contact')
    contacts.append({'frame':f['frame'],'index':i,'definition':a['DefinitionId'],'origin':origin,'proxyHeight':a['SourceUiProxyHeight'],'proxyUnit':a['SourceUiProxyUnit'],'errorLogicalPixels':error,'executionScale':a['executionScale']})
   previous=n
 check(len(contacts)==count and not frames[-1]['IsPresenting'],'Every expected contact and settled presentation')
 report={'run':run,'passed':True,'assertions':len(checks),'frames':len(frames),'maxOriginErrorLogicalPixels':maxerr,'contacts':contacts,'fullOriginalSaveMatches':True,'adapterOnlyField':'godotSession','headReadbacksStartEnd':[frames[0]['EnemyHeadReadbacks'],frames[-1]['EnemyHeadReadbacks']],'headHitsStartEnd':[frames[0]['EnemyHeadCacheHits'],frames[-1]['EnemyHeadCacheHits']],'telemetrySha256':hashlib.sha256((p/'frames.jsonl').read_bytes()).hexdigest(),'limitations':['Natural size/contact matrix only; no arbitrary camera or execution squash variation','Native float picture reviewed separately; automation does not establish complete visual parity']}
 (OUT/(run+'.json')).write_text(json.dumps(report,indent=2)+'\n');summaries.append({k:report[k] for k in ['run','passed','assertions','frames','maxOriginErrorLogicalPixels']})
print(json.dumps(summaries,indent=2))

from pathlib import Path
import json,re
r=Path('evidence/unity-burst-resource');out={};allrows={}
for p in r.glob('*/continuous-clock.json'):
 n=p.parent.name;rows=json.loads(p.read_text());allrows[n]=rows;assert len(rows)==70
 ev=json.loads((p.parent/'actual-events.json').read_text());hits=[x for x in ev if x['type']=='DamageDealt'];weak=[x for x in ev if x['type']=='EnemyWeakened'];spent=[x['spent'] for x in ev if x['type']=='LightDischarged'];expected=12 if n=='three-light' else 4;count=1 if n=='single-target' else 3
 assert len(hits)==count and all(x['amount']==expected for x in hits);assert len(weak)==count and all(x['amount']==1 for x in weak)
 contacts={}
 for f in rows:
  for e in f['localEffects']:
   if e['busy'] and e['landed']:contacts.setdefault(e['target'],f['index'])
  if n=='cancel' and f['index']>=9:assert not any(e['busy'] for e in f['localEffects'])
 assert not any(e['busy'] for e in rows[-1]['localEffects'])
 out[n]={'frames':70,'contactFrames':contacts,'spent':spent,'damage':[h['amount'] for h in hits],'weak':[h['amount'] for h in weak],'finalClear':True}
for n in ['candidate','reuse']:
 if n not in allrows:continue
 assert json.loads((r/n/'actual-events.json').read_text())==json.loads((r/'baseline/actual-events.json').read_text())
 d={'effectAge':0,'sourceLightIntensity':0,'actorFlash':0,'geometryLocalAndUVAlpha':0}
 for a,b in zip(allrows['baseline'],allrows[n]):
  a=dict(a,localEffects=[e for e in a['localEffects'] if e['busy']]);b=dict(b,localEffects=[e for e in b['localEffects'] if e['busy']]);assert len(a['localEffects'])==len(b['localEffects'])
  for x,y in zip(a['localEffects'],b['localEffects']):
   assert x['target']==y['target'] and x['ownedBurst']==y['ownedBurst'];d['effectAge']=max(d['effectAge'],abs(x['age']-y['age']))
   assert len(x['draws'])==len(y['draws'])
   for dx,dy in zip(x['draws'],y['draws']):
    assert dx['kind']==dy['kind'] and dx['vertices']==dy['vertices']
    for sx,sy in zip(dx['samples'],dy['samples']):d['geometryLocalAndUVAlpha']=max([d['geometryLocalAndUVAlpha']]+[abs(v-w) for v,w in zip(sx[:9],sy[:9])])
  sa={(z['kind'],z['key']):z for z in a['lightAudit']['slots']};sb={(z['kind'],z['key']):z for z in b['lightAudit']['slots']};assert sa.keys()==sb.keys()
  for k in sa:d['sourceLightIntensity']=max(d['sourceLightIntensity'],abs(sa[k]['intensity']-sb[k]['intensity']))
  for x,y in zip(a['lightAudit']['actorCounts'],b['lightAudit']['actorCounts']):d['actorFlash']=max(d['actorFlash'],abs(x['flash']-y['flash']))
 out[n]['maxDifferencesFromBaseline']=d;assert d['actorFlash']==0
log=Path('evidence/unity-burst-resource-player.log').read_text(encoding='utf8',errors='replace');current=None;admissions={}
for line in log.splitlines():
 m=re.search(r'\[Autopilot\] states: .*?\d+ (\S+) \|',line)
 if m:current=m[1];admissions[current]=0
 if '[BurstResourceCue] exact' in line:admissions[current]+=1
for n,v in admissions.items():assert v==(1 if n in ['candidate','cancel','reuse'] else 0),(n,v)
Path('evidence/burst-resource-audit.json').write_bytes(json.dumps({'cases':out,'resourceCueAdmissions':admissions},indent=2).encode());print(json.dumps({'cases':out,'resourceCueAdmissions':admissions},indent=2))

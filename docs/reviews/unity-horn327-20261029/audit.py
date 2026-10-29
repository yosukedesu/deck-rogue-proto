from pathlib import Path
import json
r=Path('evidence/unity-horn327-v2');out={}
for p in r.glob('*/continuous-clock.json'):
 n=p.parent.name;rows=json.loads(p.read_text());active=[];targets=set();factor=set()
 for f in rows:
  h=f['lightAudit']['tripleHorn'];flags={a['key'] for a in h['actors'] if a['receiver']>0}
  if h['count']:
   active.append(f['index']);targets|=flags;factor|={x[3] for x in h['lights'][:h['count']]}
   assert h['count']==2 and flags=={'player','enemy1' if n=='trio-target1' else 'enemy0'},(n,f['index'],h)
  else:assert not flags,(n,f['index'],'stale flags')
 assert rows[-1]['lightAudit']['tripleHorn']['count']==0
 if n in ['identity','volley','unowned-energy4']:assert not active,(n,active)
 else:assert active,(n,'no admission')
 if n=='cancel':assert all(i<20 for i in active)
 events=json.loads((p.parent/'actual-events.json').read_text());hits=[e for e in events if e['type']=='DamageDealt'];assert len(hits)==3
 amount=2 if n=='volley' else 5;assert all(e['amount']==amount and e['hpLoss']==amount for e in hits)
 out[n]={'frames':len(rows),'activeFrames':active,'receivers':sorted(targets),'factors':sorted(factor),'damage':amount,'hits':len(hits),'pierced':[e.get('pierced') for e in hits],'finalReset':True}
if 'candidate' in out:
 base=json.loads((r/'baseline/continuous-clock.json').read_text());baseEvents=json.loads((r/'baseline/actual-events.json').read_text())
 for n in ['identity','candidate','candidate-mid','reuse']:
  if n not in out:continue
  assert json.loads((r/n/'actual-events.json').read_text())==baseEvents
  rows=json.loads((r/n/'continuous-clock.json').read_text());delta={'worldIntensity':0,'worldAge':0,'hurt':0,'effectAge':0}
  for a,b in zip(base,rows):
   sa={(x['kind'],x['key']):x for x in a['lightAudit']['slots']};sb={(x['kind'],x['key']):x for x in b['lightAudit']['slots']};assert sa.keys()==sb.keys()
   for k in sa:
    delta['worldIntensity']=max(delta['worldIntensity'],abs(sa[k]['intensity']-sb[k]['intensity']));delta['worldAge']=max(delta['worldAge'],abs(sa[k]['age']-sb[k]['age']))
   for x,y in zip(a['lightAudit']['actorCounts'],b['lightAudit']['actorCounts']):delta['hurt']=max(delta['hurt'],abs(x['flash']-y['flash']))
   assert len(a['effects'])==len(b['effects'])
   for x,y in zip(a['effects'],b['effects']):delta['effectAge']=max(delta['effectAge'],abs(x['age']-y['age']))
  out[n]['eventsEqualBaseline']=True;out[n]['maxDifferences']=delta
Path('evidence/horn327-v2-audit.json').write_bytes(json.dumps(out,indent=2).encode());print(json.dumps(out,indent=2))

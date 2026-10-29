from pathlib import Path
import json
r=Path('evidence/unity-horn-clock');out={}; allrows={}
for p in r.glob('*/continuous-clock.json'):
 n=p.parent.name;rows=json.loads(p.read_text());allrows[n]=rows; assert len(rows)==70
 contacts={}; max_native_error=0; positive=[]; identities=set()
 for f in rows:
  es=f['effects']; owned=[e for e in es if e['ownedTripleHorn']]
  assert len({e['effectIdentity'] for e in owned})==len(owned),(n,f['index'],'shared owned effect')
  for e in owned:
   identities.add(e['effectIdentity']);max_native_error=max(max_native_error,abs(e['age']-e['nativeAge']))
   assert abs(e['age']-e['nativeAge'])<.00001,(n,f['index'],e)
   if e['age']>=0:assert e['hornCommitted'],(n,f['index'],'uncommitted visible age')
  for e in es:
   if e['landed']:contacts.setdefault(str(e['hitIndex']),{'frame':f['index'],'time':e['actualContactTime'],'nativeAge':e['nativeAge'],'sampledLight':e['sampledLight']})
  if any(z['kind']=='contract-attack-target' and z['intensity']>0 for z in f['lightAudit']['slots']):positive.append(f['index'])
  if n in ['legacy','volley','unowned-energy4']:assert not owned
  if n.startswith('cancel') and f['index']>=int(n[6:]):assert not es,(n,f['index'],'stale effect')
 assert not rows[-1]['effects']
 assert rows[-1]['lightAudit']['tripleHorn']['count']==0
 events=json.loads((p.parent/'actual-events.json').read_text());hits=[x for x in events if x['type']=='DamageDealt'];amount=2 if n=='volley' else 5
 assert len(hits)==3 and all(x['amount']==amount and x['hpLoss']==amount for x in hits)
 out[n]={'frames':len(rows),'contacts':contacts,'positiveTargetLightFrames':positive,'ownedEffectIdentities':sorted(identities),'maxNativeAgeError':max_native_error,'damage':[x['amount'] for x in hits],'finalEffectClear':True}
 if n in ['fixed','fixed-receive','trio-target1','reuse']:
  assert [x['frame'] for x in contacts.values()]==[8,14,19],(n,contacts)
  assert len(identities)==3
  assert all(i in positive for i in [8,14,19])
if 'legacy' in allrows:
 for n in ['fixed','fixed-receive','reuse']:
  if n not in allrows:continue
  assert json.loads((r/n/'actual-events.json').read_text())==json.loads((r/'legacy/actual-events.json').read_text())
  delta={'hurt':0,'offset':0,'kick':0,'effectAge':0}
  for a,b in zip(allrows['legacy'],allrows[n]):
   assert len(a['effects'])==len(b['effects'])
   for x,y in zip(a['effects'],b['effects']):delta['effectAge']=max(delta['effectAge'],abs(x['age']-y['age']))
   for x,y in zip(a['lightAudit']['actorCounts'],b['lightAudit']['actorCounts']):delta['hurt']=max(delta['hurt'],abs(x['flash']-y['flash']))
   for fld in ['offset','kick']:delta[fld]=max([delta[fld]]+[abs(x-y) for x,y in zip(a['receiverPose'][fld],b['receiverPose'][fld])])
  assert delta['hurt']==delta['offset']==delta['kick']==0,delta
  out[n]['maxDifferenceFromLegacy']=delta
Path('evidence/horn-clock-audit.json').write_bytes(json.dumps(out,indent=2).encode());print(json.dumps(out,indent=2))

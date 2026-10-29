from pathlib import Path
import json
r=Path('evidence/unity-imp-final');out={}
for p in r.glob('horn-*/continuous-clock.json'):
 n=p.parent.name;rows=json.loads(p.read_text(encoding='utf8'));assert len(rows)==70;admitted=[];contacts={}
 for f in rows:
  h=f['lightAudit']['tripleHorn'];assert h['probe'] and abs(h['factor']-.12)<1e-6
  flags={x['key'] for x in h['actors'] if x['receiver']>0}
  if h['count']:
   admitted.append(f['index']);assert h['count']==2 and flags=={'player','enemy1' if n=='horn-target1' else 'enemy0'}
   assert all(abs(v[3]-.12)<1e-6 for v in h['lights'][:h['count']])
  else:assert not flags
  es=[e for e in f['effects'] if e['ownedTripleHorn']];assert len({e['effectIdentity'] for e in es})==len(es)
  for e in es:
   assert abs(e['nativeAge']-e['age'])<1e-5
   if e['landed']:contacts.setdefault(str(e['hitIndex']),f['index'])
  if n=='horn-cancel' and f['index']>=20:assert not f['effects'] and not h['count']
 if n in ['horn-volley','horn-energy4']:assert not admitted and not contacts
 else:assert [contacts[str(i)] for i in range(3)]==[8,14,19] and all(i in admitted for i in [8,14,19])
 assert not rows[-1]['effects'] and not rows[-1]['lightAudit']['tripleHorn']['count']
 ev=json.loads((p.parent/'actual-events.json').read_text(encoding='utf8'));hits=[x for x in ev if x['type']=='DamageDealt'];damage=2 if n=='horn-volley' else 5;assert len(hits)==3 and all(x['amount']==x['hpLoss']==damage for x in hits)
 out[n]={'frames':70,'ownedContactFrames':contacts,'admittedFrames':admitted,'actualDamage':[x['amount'] for x in hits],'selectedFactor':.12,'finalReset':True}
Path('evidence/horn-selected-final-audit.json').write_bytes(json.dumps(out,indent=2).encode());print(json.dumps(out,indent=2))

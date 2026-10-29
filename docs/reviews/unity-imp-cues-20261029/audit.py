from pathlib import Path
import json,re
root=Path('evidence/unity-imp-final');out={};rowsby={}
for p in root.glob('*/continuous-clock.json'):
 n=p.parent.name
 if n.startswith('horn-') or n=='burst-projection':continue
 rows=json.loads(p.read_text(encoding="utf8"));rowsby[n]=rows;assert len(rows)==80
 ev=json.loads((p.parent/'actual-events.json').read_text(encoding="utf8"));hits=[x for x in ev if x['type']=='DamageDealt'];scald=[x for x in ev if x['type']=='StatusInflicted' and x['status']=='scald']
 if n!='other-enemy':assert len(hits)==1 and hits[0]['amount']==hits[0]['hpLoss']==7 and len(scald)==1 and scald[0]['amount']==1
 first=next((f['index'] for f in rows if any(e['ownedImp'] and e['committed'] and e['landed'] for e in f['fire'])),None)
 flashes={tuple(z['color']) for f in rows for z in f['screenFlashes'] if not z['suppressed']};out[n]={'frames':80,'firstCommittedContact':first,'damage':[x['hpLoss'] for x in hits],'scald':[x['amount'] for x in scald],'flashColors':list(flashes)}
 if n!='other-enemy':assert first==36
 expected=.035 if n in ['overlay','combined','cancel','reuse'] else .205
 if n!='other-enemy':assert all(abs(c[3]-expected)<.000001 for c in flashes) and len(flashes)==1,(n,flashes)
 # Fire capture contains only active slots.
 assert not rows[-1]['fire'],(n,'final fire not clear')
 if n=='cancel':assert all(not f['fire'] for f in rows if f['index']>=37)
for n in ['overlay','scald','combined','reuse']:
 if n not in rowsby:continue
 assert json.loads((root/n/'actual-events.json').read_text(encoding="utf8"))==json.loads((root/'baseline/actual-events.json').read_text(encoding="utf8"))
 delta={'lightIntensity':0,'actorFlash':0,'fireAge':0,'batchAlpha':0}
 for a,b in zip(rowsby['baseline'],rowsby[n]):
  assert len(a['fire'])==len(b['fire'])
  for x,y in zip(a['fire'],b['fire']):
   delta['fireAge']=max(delta['fireAge'],abs(x['age']-y['age']));assert len(x['batches'])==len(y['batches'])
   for dx,dy in zip(x['batches'],y['batches']):assert dx['count']==dy['count'] and dx['visible']==dy['visible'];delta['batchAlpha']=max(delta['batchAlpha'],abs(dx['alpha']-dy['alpha']))
  sa={(z['kind'],z['key']):z for z in a['lightAudit']['slots']};sb={(z['kind'],z['key']):z for z in b['lightAudit']['slots']};assert sa.keys()==sb.keys()
  for k in sa:delta['lightIntensity']=max(delta['lightIntensity'],abs(sa[k]['intensity']-sb[k]['intensity']))
  for x,y in zip(a['lightAudit']['actorCounts'],b['lightAudit']['actorCounts']):delta['actorFlash']=max(delta['actorFlash'],abs(x['flash']-y['flash']))
 assert delta['actorFlash']==0;out[n]['maxDifferencesFromBaseline']=delta
Path('evidence/imp-final-audit.json').write_bytes(json.dumps(out,indent=2).encode());print(json.dumps(out,indent=2))

log=Path('evidence/unity-imp-final-player.log').read_text(encoding='utf8',errors='replace');current=None;admissions={}
for line in log.splitlines():
 m=re.search(r'\[Autopilot\] states: .*?\d+ (\S+) \|',line)
 if m:current=m[1];admissions[current]={'overlay':[],'scald':[]}
 if '[ImpCue] overlay' in line:admissions[current]['overlay'].append(line)
 if '[ImpCue] scald' in line:admissions[current]['scald'].append(line)
for n,a in admissions.items():
 assert len(a['overlay'])==(1 if n in ['overlay','combined','cancel','reuse'] else 0),(n,a)
 assert len(a['scald'])==(1 if n in ['scald','combined','reuse'] else 0),(n,a)
Path('evidence/imp-final-admission.json').write_bytes(json.dumps(admissions,indent=2).encode())

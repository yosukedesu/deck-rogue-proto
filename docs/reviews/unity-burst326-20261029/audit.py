from pathlib import Path
import json,math
root=Path('evidence/unity-burst326-v4');out={};total=0
for p in root.glob('*/continuous-clock.json'):
 n=p.parent.name; rows=json.loads(p.read_text());total+=len(rows); active={};private=[]
 for f in rows:
  for x in f['localEffects']:
   if x['ownedBurst']:
    private.append(x)
    for d in x['draws']:
     for v in d['samples']:assert all(math.isfinite(a) for a in v),(n,f['index'])
     if d['visible']:
      assert x['committed'] and x['age']>=0,(n,f['index'],x)
      if d['kind']==41:assert d['vertices']==64 and d['indexCount']==96
      if d['kind']==42:assert d['vertices']==4 and d['indexCount']==6
      active.setdefault(x['target'],[]).append(f['index'])
 for x in rows[-1]['localEffects']:
  if x['ownedBurst']:assert not x['busy'] and not x['committed'] and all(not d['visible'] and d['vertices']==0 for d in x['draws'])
 out[n]={'frames':len(rows),'privateObserved':bool(private),'active':{k:sorted(set(v)) for k,v in active.items()},'finalReset':True}
 events=json.loads((p.parent/'actual-events.json').read_text());base=Path('evidence/unity-burst326-baseline')/n/'actual-events.json'
 if base.exists():assert events==json.loads(base.read_text());out[n]['baselineEventsEqual']=True
 if n in ['radiance','zero-burst','single-target','three-light','owned-off']:assert not active,(n,active)
 elif n not in ['cancel-before','cancel-one']:assert active,(n,'private missing')
if len(out)==14:
 a=json.loads((root/'owned'/'continuous-clock.json').read_text());b=json.loads((root/'owned-off'/'continuous-clock.json').read_text());diff={'worldIntensity':0,'worldAge':0,'hurt':0}
 for x,y in zip(a,b):
  sx={(z['kind'],z['key']):z for z in x['lightAudit']['slots'] if z['kind'].startswith('contract-')};sy={(z['kind'],z['key']):z for z in y['lightAudit']['slots'] if z['kind'].startswith('contract-')};assert sx.keys()==sy.keys()
  for k in sx:
   diff['worldIntensity']=max(diff['worldIntensity'],abs(sx[k]['intensity']-sy[k]['intensity']));diff['worldAge']=max(diff['worldAge'],abs(sx[k]['age']-sy[k]['age']))
  for m,n in zip(x['lightAudit']['actorCounts'],y['lightAudit']['actorCounts']):diff['hurt']=max(diff['hurt'],abs(m['flash']-n['flash']))
 out['owned']['versusOff']=diff
 for name in ['cancel-before','cancel-one','cancel-two','cancel-three']:
  limit={'cancel-before':6,'cancel-one':7,'cancel-two':8,'cancel-three':10}[name];assert all(i<limit for v in out[name]['active'].values() for i in v),(name,out[name])
print(json.dumps(out,indent=2));Path('evidence/burst326-v4-audit.json').write_bytes(json.dumps({'cases':out,'frames':total},indent=2).encode())

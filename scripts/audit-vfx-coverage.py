"""Static definition/engine-emitter inventory; does NOT claim every card was executed."""
from pathlib import Path
import json,re,sys
r=Path(__file__).resolve().parents[1]
if not (r/'unity').exists():r=Path('/home/yosuke/projects/deck-rogue-proto')
profiles=json.loads((r/'unity/Assets/Resources/Data/card-vfx.json').read_text())
engine=r/'unity/Packages/com.deckrogue.engine/Runtime'
effects=(engine/'Effects.cs').read_text();cases=list(re.finditer(r'case "([^"]+)":',effects))
damage_effects=set()
for n,m in enumerate(cases):
 body=effects[m.end():cases[n+1].start() if n+1<len(cases) else len(effects)]
 if 'DealDamageToEnemy(' in body:damage_effects.add(m[1])
# Grouped case labels share the following implementation.
for n,m in enumerate(cases[:-1]):
 if not effects[m.end():cases[n+1].start()].strip() and cases[n+1][1] in damage_effects:damage_effects.add(m[1])
def walk(v):
 if isinstance(v,dict):
  if 'effect' in v:yield v
  for x in v.values():yield from walk(x)
 elif isinstance(v,list):
  for x in v:yield from walk(x)
cards=[];missing=[]
sources=[(f,json.loads(f.read_text())) for f in sorted((r/'src/data').glob('cards.*.json'))]
sources.append((r/'src/data/fusions.json',[x['result'] for x in json.loads((r/'src/data/fusions.json').read_text())]))
for f,definitions in sources:
 for c in definitions:
  es=list(walk(c));de=[e for e in es if e['effect'] in damage_effects]
  profile=profiles.get(c['id']);
  if profile is None:missing.append(c['id'])
  cards.append({'source':f.name,'id':c['id'],'name':c['name'],'type':c['type'],'vfx':profile,'directDamageEffects':sorted({e['effect'] for e in de}),'triggers':sorted({e.get('trigger','onPlay') for e in de}),'hasModes':bool(c.get('modes')),'route':'resolved DamageDealt -> ResolvedCombatFx; guard/nullified or dedicated/fallback impact','execution':'static only; see separate runtime report'})
events=['DamageDealt','BurnTick','ScaldTick','ThornsReflected','HpLost']
emitters=[]
for f in engine.glob('*.cs'):
 for n,line in enumerate(f.read_text().splitlines(),1):
  for ev in events:
   if 'new GameEvent_'+ev in line:emitters.append({'file':f.name,'line':n,'event':ev})
enemydata=r/'src/data/enemies.json'
enemies=json.loads(enemydata.read_text()) if enemydata.exists() else []
out={'kind':'static inventory, not exhaustive runtime verification','cards':len(cards),'cardsWithDirectDamage':sum(bool(c['directDamageEffects']) for c in cards),'missingProfiles':missing,'directDamageEffectKinds':sorted(damage_effects),'resolvedEmitterSites':emitters,'enemyDefinitions':len(enemies),'nonCardSources':[{'file':f.name,'definitions':[{'id':d.get('id'),'effects':sorted({e['effect'] for e in walk(d)}),'damageEffects':sorted({e['effect'] for e in walk(d) if e['effect'] in damage_effects}),'route':'resolved event or no attack; gameplay unchanged'} for d in json.loads(f.read_text())]} for f in [(r/'src/data/leaders.json'),(r/'src/data/relics.json'),(r/'src/data/gears.json')]],'statusCards':['status_wound','status_scald','status_brand','status_guilt','status_junk'],'fallback':'Every resolved DamageDealt independent of card/effect name. Other current HP-loss emitter types explicitly covered. No event means no invented hit.','definitions':cards,'enemyRoutes':[{'id':e.get('id'),'moves':[{'id':m.get('id'),'kind':m.get('kind'),'route':'DamageDealt(enemy) -> hit/guard/nullified' if m.get('kind')=='attack' else 'non-attack or effect-specific event'} for m in e.get('moves',[])],'route':'DamageDealt(enemy) -> resolved hit/guard/nullified'} for e in enemies]}
dest=r/'docs/design/vfx-coverage-static.json';dest.write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n')
assert not missing,missing
presenter=(r/'unity/Assets/Game/Presenter.cs').read_text();handler=(r/'unity/Assets/Game/ResolvedCombatFx.cs').read_text()
assert all('GameEvent_'+e in handler for e in events)
assert 'ResolvedCombatFx.IsResolved(ev)' in presenter and 'ResolvedCombatFx.Show(' in presenter
print(json.dumps({k:v for k,v in out.items() if k not in ['definitions','enemyRoutes','nonCardSources']},ensure_ascii=False,indent=2))

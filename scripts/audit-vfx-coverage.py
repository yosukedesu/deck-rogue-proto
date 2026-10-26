"""Current exact definition/kind routing inventory. Static eligibility is not pixel/runtime proof."""
from pathlib import Path
import argparse,collections,hashlib,json,re

def walk(value,path=''):
    if isinstance(value,dict):
        if isinstance(value.get('effect'),str):yield path,value
        for key,child in value.items():yield from walk(child,path+'/'+key)
    elif isinstance(value,list):
        for i,child in enumerate(value):yield from walk(child,path+'/'+str(i))

def inventory(root):
    registry=json.loads((root/'unity/Assets/Resources/Data/card-vfx.json').read_text())
    engine=root/'unity/Packages/com.deckrogue.engine/Runtime'
    text=(engine/'Effects.cs').read_text();cases=list(re.finditer(r'case "([^\"]+)":',text));damage=set()
    for i,m in enumerate(cases):
        if 'DealDamageToEnemy(' in text[m.end():cases[i+1].start() if i+1<len(cases) else len(text)]:damage.add(m[1])
    for i,m in reversed(list(enumerate(cases[:-1]))):
        if not text[m.end():cases[i+1].start()].strip() and cases[i+1][1] in damage:damage.add(m[1])
    assert 'counter' in damage and 'dealDamageDrain' in damage
    profile_code=(root/'unity/Assets/Game/CardVfxProfile.cs').read_text()
    showcase_names=dict(re.findall(r'static readonly Showcase (\w+)=new Showcase\([^;]*,\"([^\"]+)\"\);',profile_code))
    showcase={key:showcase_names[name] for key,name in re.findall(r'case \"([^\"]+)\":return (\w+);',profile_code) if name in showcase_names}
    assert showcase=={'green_strike':'slash','blue_strike':'water','green_finisher_stomp':'stomp'},showcase
    data=root/'src/data';sources=[]
    for f in sorted(data.glob('cards.*.json')):
        for i,d in enumerate(json.loads(f.read_text())):sources.append(('card',f.relative_to(root).as_posix(),'/'+str(i),d))
    for i,d in enumerate(json.loads((data/'fusions.json').read_text())):sources.append(('card','src/data/fusions.json','/'+str(i)+'/result',d['result']))
    for category,name in [('leader','leaders'),('relic','relics'),('gear','gears')]:
        for i,d in enumerate(json.loads((data/(name+'.json')).read_text())):sources.append((category,'src/data/'+name+'.json','/'+str(i),d))
    rows=[];errors=[];source_table=registry.get('_presentationSourceRoutes',{})
    if not isinstance(source_table,dict):errors.append('invalid_presentation_source_table');source_table={}
    for category,file,pointer,d in sources:
        effects=[(p,e) for p,e in walk(d) if e['effect'] in damage]
        if not effects:continue
        definition=d['id'] if category in ('card','gear') else d['id']+'_passive'
        base=registry.get('_sources',{}).get(definition,registry.get(definition))
        if not isinstance(base,dict):errors.append('missing_profile:'+definition);base={}
        scoped=source_table.get(definition,{})
        if not isinstance(scoped,dict):errors.append('invalid_scoped_profile:'+definition);scoped={}
        contexts={}
        for p,e in effects:
            trigger=e.get('trigger','onPlay')
            kind=category if category=='gear' else 'permanent' if category in ('leader','relic') else 'necro' if trigger=='onSelfExhausted' else 'card' if trigger=='onPlay' else 'reaction' if d.get('type')=='reaction' else 'permanent'
            contexts.setdefault(kind,[]).append({'pointer':file+'#'+pointer+p,'effect':e['effect'],'trigger':trigger})
        paths=[]
        for kind,es in sorted(contexts.items()):
            override=scoped.get(kind);profile=override if isinstance(override,dict) else base
            if override is not None:
                allowed={'authoredAttack','element','authoredPresentation','authoredSemanticReason'}
                valid=isinstance(override,dict) and not(set(override)-allowed) and all(isinstance(v,str) and bool(v) for v in override.values()) and bool(override.get('authoredAttack')) and override.get('element') in {'physical','arcane','neutral','fire','ice','water','lightning','dark','light','earth','vine'} and override.get('authoredPresentation') in ((None,'small','noncard_fixed_small_contact_v1') if kind=='card' else ('noncard_fixed_small_contact_v1',))
                if not valid:errors.append('invalid_override:'+definition+':'+kind)
            admitted=isinstance(override,dict) or kind in base.get('authoredSourceKinds',['card'])
            family=profile.get('authoredAttack') if admitted else None
            if not family and kind=='card':family=showcase.get(definition)
            byleader=base.get('authoredAttackByLeader',{}) if admitted else {}
            candidates=sorted(set(([family] if family else [])+list(byleader.values())))
            paths.append({'kind':kind,'effects':es,'metadataOverride':isinstance(override,dict),'comparisonShowcase':kind=='card' and definition in showcase,'family':family,'conditionalFamilies':byleader,'eligible':bool(candidates),'defaultEligible':bool(family),'families':candidates,'element':profile.get('element',base.get('element')),'presentation':profile.get('authoredPresentation'),'semanticReason':profile.get('authoredSemanticReason',profile.get('semanticReason',profile.get('authoredFallbackReason',profile.get('fallbackReason',profile.get('evidence'))))),'neutral':family in ('minimal_neutral_contact','neutral_contact'),'policy':'valid kind+definition metadata; UID carried, not independent lineage proof' if override is not None else 'existing root allowlist/legacy metadata policy'})
        rows.append({'key':category+':'+d['id'],'definitionId':d['id'],'presentationDefinitionId':definition,'category':category,'name':d.get('name'),'definitionPointer':file+'#'+pointer,'definitionSha256':hashlib.sha256(json.dumps(d,sort_keys=True,separators=(',',':'),ensure_ascii=False).encode()).hexdigest(),'contexts':paths,'eligible':any(p['eligible'] for p in paths),'allContextsEligible':all(p['eligible'] for p in paths)})
    enemies=json.loads((data/'enemies.json').read_text());expected=set()
    for i,e in enumerate(enemies):
        for j,m in enumerate(e.get('moves',[])):
            if m.get('kind')!='attack':continue
            key=e['id']+'/'+m['id'];expected.add(key);p=registry.get('_enemyMoves',{}).get(key,{})
            if not p:errors.append('missing_enemy_move:'+key)
            if p.get('requiresExecutedMoveMetadata') not in (None,True,False):errors.append('invalid_enemy_proof_policy:'+key)
            family=p.get('authoredAttack');rows.append({'key':'enemy:'+key,'definitionId':m['id'],'category':'enemy','name':e.get('name'),'definitionPointer':'src/data/enemies.json#/'+str(i)+'/moves/'+str(j),'definitionSha256':hashlib.sha256(json.dumps(m,sort_keys=True,separators=(',',':'),ensure_ascii=False).encode()).hexdigest(),'contexts':[{'kind':'enemy_actual_execution','family':family,'eligible':bool(family),'defaultEligible':bool(family),'families':[family] if family else [],'element':p.get('element'),'presentation':p.get('authoredPresentation'),'executionMetadataRequired':p.get('requiresExecutedMoveMetadata') is True,'semanticReason':p.get('authoredSemanticReason',p.get('semanticReason',p.get('fallbackReason'))),'neutral':family in ('minimal_neutral_contact','neutral_contact')}],'eligible':bool(family),'allContextsEligible':bool(family)})
    if set(registry.get('_enemyMoves',{}))!=expected:errors.append('enemy_registry_key_mismatch')
    keys=[r['key'] for r in rows]
    if len(keys)!=len(set(keys)):errors.append('duplicate_canonical_source')
    contexts=[c for r in rows for c in r['contexts']]
    counts=dict(collections.Counter(r['category'] for r in rows))
    return {'schema':'deckrogue.source-route-coverage/2','scope':'static definition and metadata-kind eligibility; not exhaustive execution, rendered pixels, semantic approval or Unity/device QA','sourceFiles':{p.relative_to(root).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(data.glob('*.json'))},'registrySha256':hashlib.sha256((root/'unity/Assets/Resources/Data/card-vfx.json').read_bytes()).hexdigest(),'damageEffectKinds':sorted(damage),'counts':{'definitions':len(rows),'byCategory':counts,'kindContexts':len(contexts),'anyEligibleDefinitions':sum(r['eligible'] for r in rows),'allKindEligibleDefinitions':sum(r['allContextsEligible'] for r in rows),'eligibleContexts':sum(c['eligible'] for c in contexts),'defaultEligibleContexts':sum(c['defaultEligible'] for c in contexts),'neutralContexts':sum(c['neutral'] for c in contexts),'enemyExecutionProofOnly':sum(c.get('executionMetadataRequired',False) for c in contexts)},'unassignedDefinitions':[r['key'] for r in rows if not r['eligible']],'unassignedContexts':[{'key':r['key'],'kind':c['kind']} for r in rows for c in r['contexts'] if not c['eligible']],'conditionalWithoutDefault':[{'key':r['key'],'kind':c['kind']} for r in rows for c in r['contexts'] if c['eligible'] and not c['defaultEligible']],'errors':errors,'rows':rows,'limits':['Exact definition and source kind do not independently prove UID lineage or visual-target binding. Existing presentation owners validate runtime state.','Unknown/missing/invalid source, resources, shader or pool still require actual fallback/runtime tests.','Ordinary enemy optional actual contributions: 2–12 ordered120ms visual contacts,13+ true-count aggregate; Confused aggregate-only. No synthetic engine events.','Neutral assignments record uncertain meaning; they are not proof of specific material/weapon.','Old rejected vine/water art and rejected subtle27-tier candidates are not approved.','All cloud incrementals remain unapplied on PC; transfer403 unresolved.']}

if __name__=='__main__':
    ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('output',type=Path);ap.add_argument('--repo',type=Path,default=Path(__file__).resolve().parents[1]);ap.add_argument('--require-complete',action='store_true');a=ap.parse_args();result=inventory(a.repo.resolve());a.output.parent.mkdir(parents=True,exist_ok=True);a.output.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n');print(json.dumps({'counts':result['counts'],'errors':result['errors'],'unassignedDefinitions':len(result['unassignedDefinitions']),'unassignedContexts':len(result['unassignedContexts']),'conditionalWithoutDefault':len(result['conditionalWithoutDefault'])},ensure_ascii=False,indent=2));assert not result['errors'],result['errors'];assert not a.require_complete or not(result['unassignedDefinitions'] or result['unassignedContexts'] or result['conditionalWithoutDefault']),'incomplete source/kind/default eligibility'

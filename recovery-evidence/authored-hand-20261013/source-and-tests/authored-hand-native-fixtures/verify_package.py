#!/usr/bin/env python3
"""Read-only package hashes and fixture schedule/state invariants; no native claims."""
import hashlib,json
from pathlib import Path
p=Path(__file__).resolve().parent
manifest=json.loads((p/'manifest.json').read_text())
for name,want in manifest['files'].items():
    f=p/name
    if not f.is_file() or hashlib.sha256(f.read_bytes()).hexdigest()!=want:
        raise SystemExit('FAIL package hash: '+name)
validation=json.loads((p/'engine-validation.json').read_text())
assert validation['roundtrips']==6 and validation['assertions']==93
assert len(validation['fixtures'])==3
for row in validation['fixtures']:
    save=json.loads((p/row['fixture']).read_text())
    after=json.loads((p/(row['name']+'-expected-after.save.json')).read_text())
    acts=json.loads((p/row['actions']).read_text())
    command=json.loads((p/(row['name']+'.command.json')).read_text())
    uid=row['cardUid']; sibling=row['unchangedSiblingUid']
    assert acts==[{'frame':30,'play':uid,'target':0}]
    assert command=={'command':{'cardUid':uid,'targetIndex':0,'type':'PlayCard'},'type':'Combat'}
    assert save['run']['leaderId']==row['leader'] and save['fingerprint']==row['fingerprint']
    before=save['run']['combat']['player']; final=after['run']['combat']['player']
    assert sum(c['uid']==uid for c in before['hand'])==1
    assert sum(c['uid']==sibling for c in before['hand'])==1
    assert all(c['uid']!=uid for c in final['hand'])
    assert sum(c['uid']==sibling for c in final['hand'])==1
    assert sum(c['uid']==uid for c in final['discardPile'])==1
    assert all(c['uid']!=uid for c in final['exhaustPile'])
    assert row['command']['loadRevision']==1 and row['command']['admittedRevision']==2
    assert after['run']['combat']['enemies'][0]['hp']>0
    assert save['logIndex']==len(save['run']['combat']['eventLog'])
    assert after['logIndex']==len(after['run']['combat']['eventLog'])
    assert after['run']['rng']==save['run']['rng']
    assert after['run']['combat']['rng']==save['run']['combat']['rng']
assert json.loads((p/'idle.actions.json').read_text())==[]
print(f"PASS: {len(manifest['files'])} package hashes; three exact input/action/UID/revision/destination/RNG contracts. Native acceptance NOT RUN.")

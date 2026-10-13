#!/usr/bin/env python3
"""Preserve actual pointer ownership pass and source drag-visual failure separately."""
from pathlib import Path
import json,hashlib
R=Path(__file__).resolve().parents[3]; O=Path(__file__).parent
expected=json.loads((R/'ui-parity-restoration-20261013/authored-hand-native-fixtures/slash-expected-after.save.json').read_text())['run']
results=[]
for scale in ['1.0','1.3']:
 p=R/f'unity-parity-20261013/authored-hand-native/return-desktop-{scale}/hand-native-trace'
 if not (p/'capture-status.json').exists():continue
 rows=[json.loads(x) for x in (p/'trace.jsonl').read_text().splitlines()];pick=lambda k:[x for x in rows if x['kind']==k]
 status=json.loads((p/'capture-status.json').read_text());assert status['observerClean'] and not status['faults']
 states=[x for x in pick('state-change') if not x['detail']['IsReset']];assert len(states)==1
 a=[x for x in pick('admission-before-rebuild') if not x['detail']['IsReset']];assert len(a)==1
 ad=a[0]['detail'];uid=ad['clonedUid'];tile=ad['tile']['instance'];assert ad['registeredChangeIdentity'] and ad['Revision']==2 and ad['epoch']==1
 assert all(x['detail']['uid']==x['detail']['pressedUid']==x['detail']['payloadUid']==uid and x['detail']['payloadEpoch']==1 and x['detail']['tile']['instance']==tile for x in pick('drag-payload'))
 assert json.loads((p/'final.save.json').read_text())['run']==expected
 cr=pick('authored-create');co=pick('authored-complete-committed');assert len(cr)==len(co)==1
 assert cr[0]['detail']['owner']['tile']['instance']==tile and cr[0]['detail']['owner']['plan']['clonedUid']==uid
 re=[x for x in pick('hover-return-create') if x['detail']['owner']['dragReturn']];assert len(re)==1
 ro=re[0]['detail']['owner'];rc=[x for x in pick('hover-return-complete') if x['detail']['owner']['ownerId']==ro['ownerId']];assert len(rc)==1
 assert rc[0]['detail']['owner']['clock']['elapsed']==.15 and ro['clock']['duration']==.15
 assert not any(x['processFrame']<=rc[0]['processFrame'] for x in states)
 zero=ro['from']==ro['destination'] and ro['fromScale']==ro['toScale'];assert zero
 draws=[x['detail']['snapshot'] for x in pick('draw')];assert all(not s['uiPhoneLayout'] and not s['stagePhoneLayout'] for s in draws)
 result={'textScale':scale,'source':str(p.relative_to(R)),'traceSha256':hashlib.sha256((p/'trace.jsonl').read_bytes()).hexdigest(),'pointerOwnershipAndOriginalOutcome':'PASS for named normal-time desktop case','returnClock':'PASS only clock/method ownership; source visible motion FAILS','returnOwner':ro['ownerId'],'returnCreateFrame':re[0]['processFrame'],'returnCompleteFrame':rc[0]['processFrame'],'returnElapsed':.15,'from':ro['from'],'to':ro['destination'],'fromScale':ro['fromScale'],'toScale':ro['toScale'],'returnWallSeconds':rc[0]['wallSeconds']-re[0]['wallSeconds'],'stateChangesAfterReset':1,'commandUid':uid,'tile':tile,'authoredCreateFrame':cr[0]['processFrame'],'authoredCompleteFrame':co[0]['processFrame'],'authoredNativeElapsedAtComplete':co[0]['detail']['owner']['tween']['totalElapsed'],'observerFaults':status['faults'],'qualifications':['Zero displacement/equal scale cannot establish visible Unity return','Original desktop preview disappears while live tile stays seated','Actual-pointer identity passes do not prove phone touch','Normal-time frame quantization recorded; no exact150ms wall claim']}
 results.append(result)
(O/'PRE_FIX_POINTER_AND_RETURN_GAP.json').write_text(json.dumps(results,indent=2)+'\n')
print(json.dumps(results,indent=2))

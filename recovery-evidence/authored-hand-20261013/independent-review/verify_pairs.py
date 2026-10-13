#!/usr/bin/env python3
"""Independent read-only review of completed fixed-60 authored retirement pairs."""
import json, hashlib, pathlib, sys
ROOT=pathlib.Path(__file__).resolve().parents[3]
NATIVE=ROOT/'unity-parity-20261013/authored-hand-native'
FIX=ROOT/'ui-parity-restoration-20261013/authored-hand-native-fixtures'
OUT=pathlib.Path(__file__).resolve().parent
KEYS='Revision PresentationTime InputLocked IsInputBlocked IsPresenting ActiveMotionCount ActiveCommandRevision PendingCommandCount PendingActorActions PendingActorContacts ActorActionLaunchCount LastActorLaunchAt LastActorLaunchFrame LastActorContactAt LastActorContactFrame LastActorContactRevision DamageAudioCount DamageContactCount PresentedSemanticCueCount RejectedSemanticCueCount PresentedBlockBoundaryCount CompletedEventCount RejectedReceiptCount LastEventReceiptRejected'.split()
def read(p):return json.loads(p.read_text())
def rows(p):return [json.loads(l) for l in (p/'trace.jsonl').read_text().splitlines()]
def pick(r,k):return [x for x in r if x['kind']==k]
def snap(x):return x['detail']['snapshot'] if x['kind']=='draw' else x['detail']
def near(a,b):return abs(a-b)<2e-5

def verify(style,scale):
 bdir=NATIVE/f'baseline-{style}-{scale}'/'hand-native-trace';cdir=NATIVE/f'candidate-{style}-{scale}'/'hand-native-trace'
 if not all((p/'capture-status.json').exists() for p in (bdir,cdir)):return None
 b,c=rows(bdir),rows(cdir);checks=0
 def ck(value,why):
  nonlocal checks;checks+=1
  if not value:raise AssertionError(f'{style}/{scale}: {why}')
 admits=[[x for x in pick(r,'admission-before-rebuild') if not x['detail']['IsReset']] for r in (b,c)]
 ck(all(len(x)==1 for x in admits),'exact single admission')
 b0,c0=[x[0]['processFrame'] for x in admits];ad=admits[1][0]['detail'];uid=ad['clonedUid'];tile=ad['tile']['instance']
 dispatch=pick(c,'original-dispatch-attempt');ck(len(dispatch)==1,'single original dispatch');dd=dispatch[0]['detail']
 ck(dd['command']==ad['command'] and dd['commandId']!=ad['commandId'] and dd['innerCommandId']!=ad['innerCommandId'],'cloned root and inner identity')
 ck(ad['registeredChangeIdentity'] and ad['epoch']==1 and ad['Revision']==2 and ad['SourceShowcaseEnabled'],'registered owner and frozen epoch/revision')
 plan=pick(c,'authored-plan-captured');create=pick(c,'authored-create');end=pick(c,'authored-complete-committed')
 ck(len(plan)==len(create)==len(end)==1,'one captured plan/create/complete');p=plan[0]['detail']['owner'];o=create[0]['detail']['owner']
 ck(p['tile']['instance']==tile and p['plan']['clonedUid']==uid and p['plan']['revision']==2 and p['plan']['epoch']==1,'plan exact tile UID and owner')
 ck(p['plan']['style']['AuthoredAttack']==style and not p['plan']['exhaust'],'proved source style/discard only')
 ck(all(near(a,z) for a,z in zip(p['destination'],ad['actualDestination'])),'frozen actual pile destination')
 be=pick(b,'baseline-complete-callback');bc=pick(b,'baseline-create');ck(len(be)==len(bc)==1,'baseline one create/complete')
 ck(bc[0]['detail']['duration']==o['plan']['duration'],'identical original native float duration')
 ck(create[0]['processFrame']-c0==bc[0]['processFrame']-b0,'same creation relative frame')
 ck(end[0]['processFrame']-c0==be[0]['processFrame']-b0,'same exact completion callback frame')
 bt=[x for x in pick(b,'tile-tree-exiting') if x['detail']['uid']==uid];ct=[x for x in pick(c,'tile-tree-exiting') if x['detail']['uid']==uid]
 ck(len(bt)==len(ct)==1 and bt[0]['processFrame']-b0==ct[0]['processFrame']-c0,'one same-frame native tree removal')
 # Match first positive native elapsed at the identical phase, never a baseline setter claim.
 first={}
 for kind,phase in [('process','process-before-node-update'),('draw','post-draw')]:
  br=[x for x in pick(b,'baseline-readback') if x['detail']['observationPhase']==phase and x['detail']['elapsed']>0]
  cr=[]
  for x in pick(c,kind):
   for a in snap(x)['activeAuthored']:
    tw=a['owner'].get('tween')
    if a['uid']==uid and tw and tw['totalElapsed']>0:cr.append((x,tw['totalElapsed']))
  ck(bool(br) and bool(cr),'positive elapsed phase '+phase)
  ck(br[0]['processFrame']-b0==cr[0][0]['processFrame']-c0 and near(br[0]['detail']['elapsed'],cr[0][1]),'first elapsed matches '+phase)
  first[phase]={'relativeFrame':br[0]['processFrame']-b0,'elapsed':br[0]['detail']['elapsed']}
  # Every same-phase native retirement elapsed sample must match.
  bm={x['processFrame']-b0:x['detail']['elapsed'] for x in br};cm={x['processFrame']-c0:e for x,e in cr}
  ck(bm.keys()==cm.keys(),'same elapsed observation frames '+phase)
  for f,e in bm.items():ck(near(e,cm[f]),'same elapsed '+phase+str(f))
 # Preserve all presentation owner/receipt/audio/fence clocks at every matched process/draw.
 for kind in ('process','draw'):
  bm={x['processFrame']-b0:snap(x) for x in pick(b,kind) if x['processFrame']>=b0}
  cm={x['processFrame']-c0:snap(x) for x in pick(c,kind) if x['processFrame']>=c0}
  ck(bm.keys()==cm.keys(),'same '+kind+' frame coverage')
  for f,s in bm.items():
   for key in KEYS:ck(s[key]==cm[f][key],f'{kind}{f} unchanged {key}')
  last=list(cm.values())[-1]
  ck(not last['IsPresenting'] and not last['InputLocked'] and last['ActiveMotionCount']==last['PendingCommandCount']==last['PendingActorActions']==last['PendingActorContacts']==0,'terminal presentation settled')
  ck(not last['activeAuthored'] and not last['pendingAuthored'] and last['authoredStarts']==last['authoredCompletes']==1,'terminal retirement clear')
  ck(last['PresentationTime']-last['LastActorContactAt']>=.9,'>=.9 presentation-second contact tail')
 for rr,start in [(b,b0),(c,c0)]:
  status=read((bdir if rr is b else cdir)/'capture-status.json');ck(status['observerClean'] and not status['faults'],'no observer faults')
  pre=next(snap(x) for x in pick(rr,'draw') if x['processFrame']==start-1);post=snap(pick(rr,'draw')[-1]);siblings={x['CardUid']:x['instance'] for x in pre['tiles'] if x['CardUid']!=uid}
  for su,si in siblings.items():ck(any(x['CardUid']==su and x['instance']==si for x in post['tiles']),'surviving sibling tile '+su)
  ck(not any(x['CardUid']==uid for x in post['tiles']),'played tile absent')
 expected=read(FIX/f'{style}-expected-after.save.json')['run']
 for directory in (bdir,cdir):ck(read(directory/'final.save.json')['run']==expected,'full original expected run/RNG')
 result={'style':style,'textScale':scale,'assertions':checks,'passed':True,'admissionProcessFrame':c0,'firstPositiveNativeElapsedByPhase':first,'createProcessFrame':create[0]['processFrame'],'completionProcessFrame':end[0]['processFrame'],'treeRemovalProcessFrame':ct[0]['processFrame'],'duration':o['plan']['duration'],'tailSeconds':last['PresentationTime']-last['LastActorContactAt'],'sourceHashes':{str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest() for d in (bdir,cdir) for p in (d/'trace.jsonl',d/'capture-status.json',d/'final.save.json')},'limits':['No baseline property-setter callback or allocator-destruction claim','No pointer, stale-race, exhaust or live-host cadence acceptance from scheduled fixed60 commands','Source trajectory is checked separately by the original analyzer and reviewed native pixels']}
 (OUT/f'{style}-{scale}-independent.json').write_text(json.dumps(result,indent=2)+'\n');return result
if __name__=='__main__':
 found=[]
 for style in ['slash','water','stomp']:
  for scale in ['1.0','1.3']:
   r=verify(style,scale)
   if r:found.append(r)
 print(json.dumps({'completedPairs':len(found),'assertions':sum(r['assertions'] for r in found),'results':[{k:v for k,v in r.items() if k!='sourceHashes'} for r in found]},indent=2))

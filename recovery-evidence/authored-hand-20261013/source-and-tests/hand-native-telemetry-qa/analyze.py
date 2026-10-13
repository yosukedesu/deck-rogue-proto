#!/usr/bin/env python3
"""Read-only native trace checks. Never turns partial observer coverage into acceptance."""
import argparse,collections,json,math,pathlib

def near(a,b,e=2e-3):return abs(a-b)<=e

def sample_expected(o,k):
    plan=o['plan'];exhaust=plan['exhaust'];move=0 if exhaust else 2*k*k if k<.5 else 1-(-2*k+2)**2/2
    alpha=o['startAlpha']*(1-k) if exhaust else 1 if k<.7 else max(0,min(1,1-(k-.7)/.3))
    return [x+(y-x)*move for x,y in zip(o['from'],o['destination'])],[x+((.25 if exhaust else .3)*o['unit']-x)*k*k for x in o['fromScale']],alpha

def evaluate(rows,status=None):
    issues=[];owners=collections.defaultdict(list);returns=collections.defaultdict(list);native=[]
    for r in rows:
        d=r['detail'];k=r['kind'];o=d.get('owner') or {};owner=o.get('ownerId')
        if k.startswith('authored-') and owner:owners[owner].append(r)
        if k.startswith('hover-return-') and owner:returns[owner].append(r)
        if k in ['process','draw']:native.append(r)
        if r.get('timeScale')!=1:issues.append(f"non-normal TimeScale at row {r.get('sequence')}")
        if k=='authored-sample-applied':
            p=o.get('plan') or {};g=o['guard'];t=o['tile']
            if not(p.get('valid') and p.get('epoch')==g['uiEpoch'] and p.get('clonedUid')==d['uid'] and g['sameCurrentOwner'] and not g['hasLiveSameUid']):issues.append('stale UID/epoch/revision/owner accepted sample')
            k=d['normalized'];position,scale,alpha=sample_expected(o,k)
            if not all(near(a,b) for a,b in zip(position,t['position'])):issues.append('authored position mismatch')
            if not all(near(a,b) for a,b in zip(scale,t['scale'])):issues.append('authored scale mismatch')
            if not near(alpha,t['alpha'],1e-5):issues.append('authored alpha mismatch')
        if k=='hover-return-sample':
            elapsed=o['clock']['elapsed'];duration=o['clock']['duration'];t=min(1,max(0,elapsed/duration));ret=o['dragReturn'];move=1-(1-t)**3 if ret else 1-(1-t)**2
            scale=0 if t<=0 else 1 if t>=1 else 1+2.70158*(t-1)**3+1.70158*(t-1)**2 if ret else 1-(1-t)**2
            if not near(duration,.15 if ret else .12,1e-12):issues.append('return/hover duration mismatch')
            for field,src,dst,f in [('position','from','destination',move),('scale','fromScale','toScale',scale)]:
                expected=[a+(b-a)*f for a,b in zip(o[src],o[dst])]
                if not all(near(a,b) for a,b in zip(expected,o['tile'][field])):issues.append('return/hover '+field+' mismatch')
    summaries=[]
    for oid,events in owners.items():
        created=[r for r in events if r['kind']=='authored-create'];complete=[r for r in events if r['kind']=='authored-complete-committed'];cancel=[r for r in events if r['kind']=='authored-cancel-committed']
        if len(created)>1 or len(complete)+len(cancel)>1:issues.append(f'duplicate authored retirement/terminal owner {oid}')
        if created and not complete and not cancel:issues.append(f'authored owner {oid} incomplete at capture boundary')
        if created:
            first=next((r for r in events if r['kind']=='authored-sample-applied'),None)
            summaries.append(dict(owner=oid,uid=created[0]['detail']['uid'],tile=created[0]['detail']['owner']['tile']['instance'],duration=created[0]['detail']['owner']['plan']['duration'],createFrame=created[0]['processFrame'],firstActualCallbackFrame=first['processFrame'] if first else None,completeFrame=complete[0]['processFrame'] if complete else None,cancelFrame=cancel[0]['processFrame'] if cancel else None))
    return_summaries=[]
    for oid,events in returns.items():
        created=[r for r in events if r['kind']=='hover-return-create'];terminal=[r for r in events if r['kind'] in ['hover-return-complete','hover-return-cancel']]
        if len(created)>1 or len(terminal)>1:issues.append(f'duplicate hover/return terminal owner {oid}')
        if created and not terminal:issues.append(f'hover/return owner {oid} incomplete at capture boundary')
        if created:
            o=created[0]['detail']['owner'];return_summaries.append(dict(owner=oid,uid=created[0]['detail']['uid'],tile=o['tile']['instance'],dragReturn=o['dragReturn'],duration=o['clock']['duration'],createFrame=created[0]['processFrame'],terminal=[dict(kind=r['kind'],frame=r['processFrame'],elapsed=r['detail']['owner']['clock']['elapsed']) for r in terminal]))
    if status and (not status['observerClean'] or status['faults']):issues.append('capture observer faults')
    # These events prove exercise only; absence is never treated as a rejection pass.
    stale_attempts=[]
    for r in rows:
        if r['kind'] in ['authored-sample-attempt','authored-complete-attempt']:
            o=r['detail']['owner'];p=o.get('plan') or {};g=o['guard']
            if p.get('epoch')!=g['uiEpoch'] or p.get('clonedUid')!=r['detail']['uid'] or not p.get('valid') or not g['sameCurrentOwner'] or g['hasLiveSameUid']:stale_attempts.append(r['sequence'])
    return dict(schema='hand-native-checks/1',observedChecksPass=not issues,issues=sorted(set(issues)),authored=summaries,hoverAndReturn=return_summaries,processRows=sum(r['kind']=='process' for r in rows),drawRows=sum(r['kind']=='draw' for r in rows),staleAttemptRows=stale_attempts,staleRaceCoverage='OBSERVED_ATTEMPTS_REVIEW_TRACE' if stale_attempts else 'NOT_EXERCISED',nativeAcceptance='NOT_AUTOMATICALLY_GRANTED',baselineFirstSetterCallback='UNOBSERVABLE_WITHOUT_CHANGING_PROPERTY_TWEENER; first positive native elapsed is a separate readback gate')

def load(path):
    p=pathlib.Path(path);return [json.loads(s) for s in (p/'trace.jsonl').read_text().splitlines()],json.loads((p/'capture-status.json').read_text())

def pair(base,candidate):
    b,bs=load(base);c,cs=load(candidate);issues=[]
    def rows(a,kind):return [r for r in a if r['kind']==kind]
    def admission(a):return next(r['processFrame'] for r in a if r['kind']=='admission-before-rebuild' and not r['detail']['IsReset'])
    b0=admission(b);c0=admission(c)
    bc=rows(b,'baseline-create');cc=rows(c,'authored-create');be=rows(b,'baseline-complete-callback');ce=rows(c,'authored-complete-committed')
    if not(len(bc)==len(cc)==len(be)==len(ce)==1):issues.append('requires one played retirement and one completion in each trace')
    else:
        if bc[0]['detail']['duration']!=cc[0]['detail']['owner']['plan']['duration']:issues.append('captured source duration differs')
        if bc[0]['processFrame']-b0!=cc[0]['processFrame']-c0:issues.append('creation relative frame differs')
        if be[0]['processFrame']-b0!=ce[0]['processFrame']-c0:issues.append('completion relative frame differs')
    for kind in ['state-change','numeric-contact','presented-event']:
        def signature(a,zero):
            keys=['Revision','IsReset','command','stateSha','events'] if kind=='state-change' else ['PresentationTime','Revision','RejectedReceiptCount','LastEventReceiptRejected','eventData']
            return [(r['processFrame']-zero,{k:r['detail'].get(k) for k in keys}) for r in rows(a,kind)]
        if signature(b,b0)!=signature(c,c0):issues.append(kind+' sequence/frame/content differs')
    bf=json.loads((pathlib.Path(base)/'final.save.json').read_text());cf=json.loads((pathlib.Path(candidate)/'final.save.json').read_text())
    if bf.get('run')!=cf.get('run'):issues.append('full final RunState differs (includes run/combat RNG)')
    # Full per-process authority/cue/fence readouts are retained for external review.
    return dict(baseline=evaluate(b,bs),candidate=evaluate(c,cs),pairIssues=issues,observedPairChecksPass=not issues,fullNativeAcceptance='PENDING_RUNTIME_REVIEW',firstNativeStep='Compare baseline-first-positive-native-elapsed with candidate native elapsed at the same observation phase. Never label it the baseline setter callback.',absoluteAdmissionFrames=dict(baseline=b0,candidate=c0))

def main():
    p=argparse.ArgumentParser();p.add_argument('trace',type=pathlib.Path);p.add_argument('--candidate',type=pathlib.Path);p.add_argument('--expected',type=pathlib.Path);p.add_argument('--out',type=pathlib.Path);a=p.parse_args()
    result=pair(a.trace,a.candidate) if a.candidate else evaluate(*load(a.trace))
    if a.expected:
        actual=json.loads((a.trace/'final.save.json').read_text());expected=json.loads(a.expected.read_text());result['fullFinalRunMatchesOriginalEngineExpected']=actual['run']==expected['run']
    content=json.dumps(result,indent=2)+'\n'
    if a.out:a.out.write_text(content)
    print(content,end='')
if __name__=='__main__':main()

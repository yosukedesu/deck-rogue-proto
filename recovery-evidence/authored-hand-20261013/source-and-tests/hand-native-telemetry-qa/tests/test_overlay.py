import importlib.util,json,pathlib,hashlib,tempfile,subprocess,copy
P=pathlib.Path(__file__).resolve().parents[1];W=P.parent.parent
spec=importlib.util.spec_from_file_location('overlay',P/'apply.py');m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
spec=importlib.util.spec_from_file_location('analyze',P/'analyze.py');a=importlib.util.module_from_spec(spec);spec.loader.exec_module(a)
checks=0
def c(ok,msg):
 global checks
 checks+=1
 if not ok:raise AssertionError(msg)
def sha(b):return hashlib.sha256(b).hexdigest()
inputs=json.loads((P/'verification/input-source-hashes.json').read_text())
for role in ['baseline','candidate']:
 root=P/(role+'-copy');manifest=json.loads((root/'hand-native-qa-manifest.json').read_text());modified={x['path'] for x in manifest['changes']}
 for ch in manifest['changes']:
  text=(root/ch['path']).read_text();c(sha(text.encode())==ch['afterSha256'],'installed hash')
  if not ch.get('newFile'):c(sha(m.strip(text).encode())==ch['beforeSha256']==ch['productionStrippedSha256'],'symbol-off exact original')
 for name,value in inputs.items():
  if name not in modified and not (role=='candidate' and name in ['DeckRogue/GodotPort/App/Main.cs','DeckRogue/GodotPort/UI/GameUI.RecoveredBattle.cs']):c(sha((root/name).read_bytes())==value,'untouched copied source '+name)
 # A second apply must fail before any writes, including existing payload conflicts.
 before={str(f):sha(f.read_bytes()) for f in root.rglob('*.cs')}
 r=subprocess.run(['python',str(P/'apply.py'),str(root),'--apply'],capture_output=True,text=True)
 c(r.returncode!=0,'reapply fails closed');c(before=={str(f):sha(f.read_bytes()) for f in root.rglob('*.cs')},'reapply no partial write')
for forbidden in [W/'runtime-recovery-20261012/project',P.parent/'authored-hand-retirement-patch/compile-snapshot']:
 r=subprocess.run(['python',str(P/'apply.py'),str(forbidden)],capture_output=True,text=True);c(r.returncode!=0,'forbidden destination rejected')
for name in ['authored-hand-retirement-patch','hand-exit-return-patch','authored-hand-native-fixtures']:
 frozen=json.loads((P/'verification'/f'{name}-frozen-hashes.json').read_text())
 now={str(f.relative_to(P.parent/name)):sha(f.read_bytes()) for f in (P.parent/name).rglob('*') if f.is_file()}
 c(frozen==now,'frozen package unchanged '+name)
# Observer source has no dispatch/load/timeScale setter or fabricated input. Original QA loaders retain theirs.
s=(P/'payload/HandNativeTrace.cs').read_text()
for prohibited in ['session.Dispatch(','.ForceDrag(','.WarpMouse(','.ParseInputEvent(','TimeScale=', '.Advance(','.QueueFree(','.Kill(','.SetProcess(','.SelectEnemy(']:c(prohibited not in s,'observer prohibited mutation '+prohibited)
c('ownerContainer=clock!=null&&F(owner,"DragReturn") is bool?"_uxHandHoverPoses"' in s,'hover/return observed from correct owner container')
c('admission!=null?"_uxAuthoredHandExits":plan!=null?"_uxAdmittedHandExits"' in s,'active/pending owner container distinction')
c('sameCurrentOwner=current!=null&&ReferenceEquals(owner,current)' in s,'null owner is not current')
c('double qaDuration=_uxRetirement?.Invoke' in (P/'baseline-copy/DeckRogue/GodotPort/UI/GameUI.HandExit.cs').read_text(),'one source duration local')
# Synthetic analyzer unit rows only. Never native execution or native proof.
base={'ownerId':5,'plan':{'epoch':1,'revision':2,'clonedUid':'played','definitionId':'green_strike','duration':.091,'valid':True,'exhaust':False},'tile':{'instance':42,'position':[0,0],'scale':[1,1],'alpha':1},'guard':{'uiEpoch':1,'sameCurrentOwner':True,'hasLiveSameUid':False},'from':[0,0],'destination':[100,100],'fromScale':[1,1],'startAlpha':1,'unit':1}
def row(kind,owner=base,norm=None,sequence=1):return {'sequence':sequence,'kind':kind,'processFrame':10,'timeScale':1,'detail':{'uid':'played','owner':copy.deepcopy(owner),'normalized':norm}}
create=row('authored-create');sample=row('authored-sample-applied',norm=.5);sample['detail']['owner']['tile'].update(position=[50,50],scale=[.825,.825],alpha=1);complete=row('authored-complete-committed')
valid=[create,sample,complete];c(a.evaluate(valid)['observedChecksPass'],'valid synthetic trajectory')
c(not a.evaluate(valid+[complete])['observedChecksPass'],'duplicate completion rejected')
for mismatch in ['epoch','uid','owner','tile','revision']:
 bad=copy.deepcopy(sample);o=bad['detail']['owner']
 if mismatch=='epoch':o['guard']['uiEpoch']=2
 if mismatch=='uid':o['plan']['clonedUid']='sibling'
 if mismatch=='owner':o['guard']['sameCurrentOwner']=False
 if mismatch=='tile':o['guard']['hasLiveSameUid']=True
 if mismatch=='revision':o['plan']['valid']=False
 c(not a.evaluate([create,bad,complete])['observedChecksPass'],'stale '+mismatch+' applied sample rejected')
c(a.evaluate(valid)['staleRaceCoverage']=='NOT_EXERCISED','absence not race proof')
for is_return,duration in [(True,.15),(False,.12)]:
 o={'ownerId':10,'dragReturn':is_return,'clock':{'duration':duration,'elapsed':0},'tile':{'instance':50,'position':[5,6],'scale':[1,1]},'from':[5,6],'destination':[100,200],'fromScale':[1,1],'toScale':[.7,.7]}
 cr=row('hover-return-create',o);o['clock']['elapsed']=duration/2;t=.5;move=1-(1-t)**(3 if is_return else 2);scale=1+2.70158*(t-1)**3+1.70158*(t-1)**2 if is_return else 1-(1-t)**2
 o['tile']['position']=[x+(y-x)*move for x,y in zip(o['from'],o['destination'])];o['tile']['scale']=[1+(.7-1)*scale]*2
 sm=row('hover-return-sample',o);o['clock']['elapsed']=duration;co=row('hover-return-complete',o)
 c(a.evaluate([cr,sm,co])['observedChecksPass'],'return/hover formula distinct')
 c(not a.evaluate([cr,sm,co,co])['observedChecksPass'],'return duplicate terminal rejected')
print(f'PASS {checks} copied-overlay integrity, frozen-package, mutation-scope, guarded-seam and synthetic analyzer assertions. No native execution.')

from pathlib import Path
import tempfile,subprocess,sys,json,hashlib,itertools
r=Path(__file__).resolve().parents[1];changes=json.loads((r/'changes.json').read_text());manifest=json.loads((r/'manifest.json').read_text());n=0
def C(x,label):
 global n;n+=1
 if not x:raise AssertionError(label)
def setup(p):
 for f in (r/'baseline').glob('*.cs'):
  target='DeckRogue/GodotPort/'+('App/' if f.name=='Main.cs' else 'UI/')+f.name;t=p/target;t.parent.mkdir(parents=True,exist_ok=True);t.write_bytes(f.read_bytes())
def snap(p):return {str(f.relative_to(p)):f.read_bytes() for f in p.rglob('*') if f.is_file()}
def run(p,script=None):return subprocess.run([sys.executable,str(script or r/'apply.py'),str(p),'--apply'],text=True,capture_output=True)
with tempfile.TemporaryDirectory(prefix='authored-hand-guards-') as d:
 p=Path(d);setup(p);before=snap(p);result=subprocess.run([sys.executable,str(r/'apply.py'),str(p)],text=True,capture_output=True);C(result.returncode==0,result.stderr);C(snap(p)==before,'dryrun no write');result=run(p);C(result.returncode==0,result.stderr)
 for f in (r/'composed').glob('*.cs'):
  target=p/'DeckRogue/GodotPort'/('App' if f.name=='Main.cs' else 'UI')/f.name;C(target.read_bytes()==f.read_bytes(),'exact compose '+f.name)
 for f in manifest['payload']:C(hashlib.sha256((p/f['path']).read_bytes()).hexdigest()==f['sha256'],'payload hash')
 applied=snap(p);C(run(p).returncode==0,'repeat apply');C(snap(p)==applied,'idempotence')
for c in changes:
 with tempfile.TemporaryDirectory(prefix='authored-hand-conflict-') as d:
  p=Path(d);setup(p);t=p/c['path'];s=t.read_text();C(s.count(c['before'])==1,'baseline hunk');t.write_text(s.replace(c['before'],'CONFLICT',1));before=snap(p);C(run(p).returncode!=0,'reject conflict '+c['name']);C(snap(p)==before,'no partial write '+c['name'])
for f in manifest['payload']:
 with tempfile.TemporaryDirectory(prefix='authored-hand-payload-') as d:
  p=Path(d);setup(p);t=p/f['path'];t.parent.mkdir(parents=True,exist_ok=True);t.write_text('conflicting implementation');before=snap(p);C(run(p).returncode!=0,'reject newfile conflict');C(snap(p)==before,'payload no partial write')
# Real installers: both cache and token v3 commute with this package after the
# ordinary/return prerequisite. Do not edit any independent lane's package.
scripts={'authored':r/'apply.py','cache':r.parent/'head-cache-lifetime-delta/apply.py','token':r.parent/'token-band-v3-patch/apply.py'};outputs=[]
for order in itertools.permutations(scripts):
 with tempfile.TemporaryDirectory(prefix='authored-hand-compose-') as d:
  p=Path(d);setup(p)
  for label in order:
   result=run(p,scripts[label]);C(result.returncode==0,str(order)+' '+result.stderr)
  C(run(p).returncode==0,'idempotent after composed '+str(order));outputs.append(snap(p))
C(all(o==outputs[0] for o in outputs),'all six authored/cache/token orders byte-identical')
helper=(r/'payload/DeckRogue/GodotPort/UI/GameUI.AuthoredHandRetirement.cs').read_text();stage=(r/'payload/DeckRogue/GodotPort/Presentation/BattleStage.AuthoredHandRetirement.cs').read_text();core=(r/'composed/GameUI.cs').read_text();retire=(r/'composed/GameUI.HandExit.cs').read_text()
for forbidden in ['_activeChange','_preferredTarget','CurrentRun','Dispatch(','Sound(','GameEvent_CardPlayed','CreateTimer','await ','SourceHandExitClock','SetPresentationBusy','ContactBeat(','LaunchOrdinary(']:C(forbidden not in helper,'no new owner '+forbidden)
C('ReferenceEquals(owner.Change,change)' in stage,'exact admitted owner descriptor')
C('AuthoredAttack:not null' in stage and 'SourceShowcaseEnabled' in stage,'explicit authored proof and showcase gate')
C('change.Previous?.Combat?.Player.Hand' in stage,'definition from admitted before hand')
C('ReferenceEquals(a.Tile,tile)' in helper and 'a.Plan.Epoch!=_uxEpoch' in helper,'UID epoch tile consume')
C('!ReferenceEquals(expected,motion)' in helper,'old callback cannot cancel newer owner')
C('UxCheckAuthoredHandExits()' in helper and 'delta' not in helper,'lifecycle callback never advances Tween clock')
C('        if(change.IsReset)UxResetHand();\n        UxCaptureAdmittedHandExit(change);' in core,'capture before rebuild and after reset')
C('tween.TweenMethod' in helper and 'a.Plan.Duration' in helper and 'tween.TweenCallback' in helper,'same one-duration then callback retirement schedule')
C('_uxRetirement?.Invoke(gone.GetMeta("definition_id","").AsString())??.25' in retire,'fallback retirement expression unchanged')
C('SourceHandExitMotion.Delay(ordinal)' in retire,'ordinary discard untouched')
C(all('UxPresentEvent' not in c['before'] and 'BuildUxPockets' not in c['before'] for c in changes),'token owner seams untouched')
C(not any(': Control' in p.read_text() or ': Node' in p.read_text() for p in (r/'payload').rglob('*.cs')),'no new Godot-derived class')
print(f'PASS {n} guard, idempotence, no-partial-write, six-order cache/token composition, and scope assertions.')

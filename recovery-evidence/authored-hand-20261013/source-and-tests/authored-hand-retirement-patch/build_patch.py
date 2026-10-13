from pathlib import Path
import hashlib,json,difflib
r=Path(__file__).resolve().parent;sha=lambda b:hashlib.sha256(b).hexdigest();texts={p.name:p.read_text() for p in (r/'baseline').glob('*.cs')};changes=[]
def edit(name,path,label,before,after):
 s=texts[name];assert s.count(before)==1,(name,label,s.count(before));texts[name]=s.replace(before,after,1);changes.append(dict(path=path,name=label,before=before,after=after,before_sha256=sha(before.encode()),after_sha256=sha(after.encode())))
ui='DeckRogue/GodotPort/UI/'
edit('GameUI.cs',ui+'GameUI.cs','capture admission before rebuild','        if(change.IsReset)UxResetHand();','        if(change.IsReset)UxResetHand();\n        UxCaptureAdmittedHandExit(change);')
edit('Main.cs','DeckRogue/GodotPort/App/Main.cs','bind owner-qualified source style','        _ui.SetCardRetirement(id => (double)_stage.GetCardRetirementSeconds(id));','        _ui.SetCardRetirement(id => (double)_stage.GetCardRetirementSeconds(id));\n        _ui.SetAdmittedAuthoredHandStyle(_stage.GetAdmittedAuthoredHandStyle);')
edit('GameUI.HandExit.cs',ui+'GameUI.HandExit.cs','consume frozen authored plan at old retirement start','        if(UxUseDiscardFlight(uid,state))','        if(UxTryStartAuthoredHandExit(uid,gone))return;\n        if(UxUseDiscardFlight(uid,state))')
f='GameUI.RecoveredBattle.cs'
edit(f,ui+f,'reset revokes admitted and running authored owners','UxCancelHandExits();UxCancelHandHovers(false);','UxCancelAuthoredHandExits();UxCancelHandExits();UxCancelHandHovers(false);')
edit(f,ui+f,'same UID new tile revokes authored owner','if(added){UxCancelHandExit(uid);tile=new CardTile','if(added){UxCancelAuthoredHandExit(uid);UxCancelHandExit(uid);tile=new CardTile')
edit(f,ui+f,'retain authored source hand order','        UxRestoreHandExitOrder();','        UxRestoreHandExitOrder();UxRestoreAuthoredHandExitOrder();')
edit(f,ui+f,'lifecycle checks never advance retirement clock','UxTickHandExits(delta);PositionUxEnemies();','UxTickHandExits(delta);UxCheckAuthoredHandExits();PositionUxEnemies();')
(r/'changes.json').write_text(json.dumps(changes,indent=2)+'\n');diff=''
for name,s in texts.items():
 (r/'composed'/name).write_text(s);path=next(c['path'] for c in changes if Path(c['path']).name==name);diff+=''.join(difflib.unified_diff((r/'baseline'/name).read_text().splitlines(True),s.splitlines(True),fromfile='a/'+path,tofile='b/'+path))
for p in sorted((r/'payload').rglob('*.cs')):diff+=''.join(difflib.unified_diff([],p.read_text().splitlines(True),fromfile='/dev/null',tofile='b/'+str(p.relative_to(r/'payload'))))
(r/'authored-hand-retirement.diff').write_text(diff)
(r/'manifest.json').write_text(json.dumps(dict(schema='authored-hand-retirement/1',baseline_commit='3df4d5a',requires='hand-exit-return-patch',runtime_applied=False,baselines=[dict(name=p.name,sha256=sha(p.read_bytes())) for p in sorted((r/'baseline').glob('*.cs'))],payload=[dict(path=str(p.relative_to(r/'payload')),sha256=sha(p.read_bytes())) for p in sorted((r/'payload').rglob('*.cs'))]),indent=2)+'\n')
print('Prepared',len(changes),'narrow hunks and three managed helper files. No live writes.')

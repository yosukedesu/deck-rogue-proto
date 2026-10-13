#!/usr/bin/env python3
"""TEST-ONLY copied-project overlay; --apply requires an explicit copy sentinel."""
import argparse, hashlib, json, pathlib, re, sys
HERE=pathlib.Path('/workspace/scratch/948944000d55/ui-parity-restoration-20261013/hand-native-telemetry-qa')
WORK=HERE.parent.parent
LIVE=WORK/'runtime-recovery-20261012/project'
UI='DeckRogue/GodotPort/UI/'
QA='recovery-validation/'
TRACE='DeckRogue.GodotPort.RecoveryQA.HandNativeTrace'
BEGIN='#if RECOVERY_QA && HAND_NATIVE_TRACE // HAND_TRACE_QA_BEGIN'
END='#endif // HAND_TRACE_QA_END'
def sha(b):return hashlib.sha256(b).hexdigest()
def guard(code,original=''):
    return '\n'+BEGIN+'\n'+code+'\n#else // HAND_TRACE_QA_ORIGINAL\n'+original+'\n'+END+'\n'
def strip(text):
    return re.sub(r'\n#if RECOVERY_QA && HAND_NATIVE_TRACE // HAND_TRACE_QA_BEGIN\n.*?\n#else // HAND_TRACE_QA_ORIGINAL\n(.*?)\n#endif // HAND_TRACE_QA_END\n',lambda m:m[1],text,flags=re.S)
def instrument(project):
    result={};changes=[]
    def edit(name,fn):
        path=project/name; before=path.read_text();after=fn(before)
        if strip(after)!=before:raise ValueError('non-QA restoration mismatch: '+name)
        result[name]=after;changes.append(dict(path=name,beforeSha256=sha(before.encode()),afterSha256=sha(after.encode()),productionStrippedSha256=sha(strip(after).encode())))
    def replace(s,old,new):
        if s.count(old)!=1:raise ValueError(f'Expected exact unique seam ({s.count(old)} matches): {old[:140]}')
        return s.replace(old,new,1)
    def after(s,old,code):return replace(s,old,old+guard(code))
    def hook(kind,owner='motion',tile='null',fraction='null'):return f'{TRACE}.Event(this,"{kind}",uid,{owner},{tile},{fraction});'
    edit(UI+'GameUI.cs',lambda s:after(s,'\t\t_activeChange = change;',f'{TRACE}.Admission(this,change);'))
    def exit(s):
        s=after(s,'var gone=_uxTiles[uid];',hook('retirement-removal-seam','null','gone'))
        old='var tween=CreateTween();tween.TweenProperty(gone,"modulate:a",0f,_uxRetirement?.Invoke(gone.GetMeta("definition_id","").AsString())??.25);\n        tween.TweenCallback(Callable.From(()=>{if(GodotObject.IsInstanceValid(gone))gone.QueueFree();}));'
        new=f'''var tween=CreateTween();
        double qaDuration=_uxRetirement?.Invoke(gone.GetMeta("definition_id","").AsString())??.25;
        tween.TweenProperty(gone,"modulate:a",0f,qaDuration);
        tween.TweenCallback(Callable.From(()=>{{{TRACE}.BaselineComplete(this,uid,gone,tween);if(GodotObject.IsInstanceValid(gone))gone.QueueFree();}}));
        {TRACE}.Baseline(this,uid,gone,tween,qaDuration);'''
        return replace(s,old,guard(new,old))
    edit(UI+'GameUI.HandExit.cs',exit)
    authored=(project/(UI+'GameUI.AuthoredHandRetirement.cs')).exists()
    if authored:
        def author(s):
            s=after(s,'if(plan.Valid)_uxAdmittedHandExits[uid]=new UxAdmittedHandExit(plan,tile,to,Size,BattleUnit);',f'{TRACE}.Event(this,"authored-plan-captured",uid,_uxAdmittedHandExits.TryGetValue(uid,out var qaAdmission)?qaAdmission:null,tile);')
            s=after(s,'if(!_uxAdmittedHandExits.Remove(uid,out var a))return false;',hook('authored-start-attempt','a','tile'))
            s=after(s,'tween.TweenCallback(Callable.From(()=>UxCompleteAuthoredHandExit(uid,motion)));',hook('authored-create'))
            s=after(s,'private void UxSampleAuthoredHandExit(string uid,UxAuthoredHandExit motion,float normalized)\n    {',hook('authored-sample-attempt',fraction='normalized'))
            s=after(s,'var color=a.Tile.Modulate;color.A=pose.Alpha;a.Tile.Modulate=color;',hook('authored-sample-applied',fraction='normalized'))
            s=after(s,'private void UxCompleteAuthoredHandExit(string uid,UxAuthoredHandExit motion)\n    {',hook('authored-complete-attempt'))
            s=after(s,'_uxAuthoredHandExits.Remove(uid);AuthoredHandExitCompleteCount++;',hook('authored-complete-committed'))
            s=after(s,'private void UxCancelAuthoredHandExit(string uid,UxAuthoredHandExit expected=null)\n    {',hook('authored-cancel-attempt','expected'))
            s=after(s,'_uxAuthoredHandExits.Remove(uid);\n        if(motion.Tween', 'UNUSED') if False else s
            s=replace(s,'_uxAuthoredHandExits.Remove(uid);\n        if(motion.Tween','_uxAuthoredHandExits.Remove(uid);'+guard(hook('authored-cancel-committed'))+'\n        if(motion.Tween')
            return s
        edit(UI+'GameUI.AuthoredHandRetirement.cs',author)
    def hover(s):
        line='_uxHandHoverPoses[uid]=new UxHandHoverPose{Tile=tile,Epoch=_uxEpoch,Clock=new SourceHandHoverClock(Godot.Engine.GetProcessFrames(),dragReturn?SourceHandReturnMotion.Duration:SourceHandHoverMotion.Duration),DragReturn=dragReturn,From=tile.Position,To=to,FromScale=tile.Scale,ToScale=scale,CanvasSize=Size,Unit=BattleUnit};'
        s=after(s,line,hook('hover-return-create','_uxHandHoverPoses[uid]','tile'))
        s=after(s,'if(!_uxHandHoverPoses.Remove(uid,out var p))return;',hook('hover-return-cancel','p'))
        s=after(s,'p.Tile.Position=p.From.Lerp(p.To,move);p.Tile.Scale=p.FromScale.Lerp(p.ToScale,scale);',hook('hover-return-sample','p'))
        old='if(p.Clock.Complete)_uxHandHoverPoses.Remove(uid);'
        new='if(p.Clock.Complete){'+hook('hover-return-complete','p')+'_uxHandHoverPoses.Remove(uid);}'
        return replace(s,old,guard(new,old))
    edit(UI+'GameUI.HandHover.cs',hover)
    def tile(s):
        s=after(s,'public override void _GuiInput(InputEvent evt)\n\t{',f'{TRACE}.Pointer(this,evt);')
        s=after(s,'if(what==NotificationDragEnd){',f'{TRACE}.DragEnd(this);')
        old='return DragPayload?.Invoke() ?? CardUid;'
        return replace(s,old,guard(f'return {TRACE}.Payload(this,DragPayload?.Invoke() ?? CardUid);',old))
    edit(UI+'CardTile.cs',tile)
    def sequence(s):
        s=after(s,'stage.Bind(session);ui.Bind(session);',f'{TRACE}.Start(main,stage,ui,session,args);')
        s=after(s,'static void Dispatch(RunCommand command){',f'{TRACE}.DispatchAttempt(command);')
        s=after(s,'if(frame+1>=count){',f'{TRACE}.Finish("sequence-completed");')
        return s
    edit(QA+'UnityParitySequenceQa.cs',sequence)
    def manual(s):
        s=after(s,'stage.PlaybackPaused=false;',f'{TRACE}.Start(root,stage,ui,session,args);')
        return after(s,'static void Poll(){',f'if({TRACE}.Passive)return; // Passive capture does not reload or alter TimeScale through QA hotkeys.')
    edit(QA+'UnityUiManualQa.cs',manual)
    for file in HERE.joinpath('payload').glob('*.cs'):
        name=QA+file.name
        if (project/name).exists():raise ValueError('Refusing existing payload '+name)
        result[name]=file.read_text();changes.append(dict(path=name,newFile=True,afterSha256=sha(result[name].encode())))
    return result,dict(schema='hand-native-qa-overlay/1',testOnly=True,authoredCandidate=authored,requiredDefines=['RECOVERY_QA','HAND_NATIVE_TRACE'],changes=changes)
def main():
    p=argparse.ArgumentParser();p.add_argument('project',type=pathlib.Path);p.add_argument('--apply',action='store_true');a=p.parse_args();project=a.project.resolve()
    if project==LIVE or LIVE in project.parents:raise ValueError('Live runtime is forbidden, including dry run')
    if HERE.parent in project.parents and not HERE in project.parents:raise ValueError('Other frozen overlay directories are forbidden')
    result,manifest=instrument(project)
    if a.apply:
        sentinel=project/'.hand-native-qa-copy'
        if not sentinel.is_file() or sentinel.read_text().strip()!='TEST-ONLY DISPOSABLE COPY':raise ValueError('Requires copy sentinel .hand-native-qa-copy containing TEST-ONLY DISPOSABLE COPY')
        for name,content in result.items():(project/name).write_text(content)
        (project/'hand-native-qa-manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
    print(json.dumps(manifest,indent=2))
if __name__=='__main__':
    try:main()
    except Exception as e:print('REFUSED:',e,file=sys.stderr);sys.exit(1)

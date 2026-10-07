using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using Newtonsoft.Json.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
namespace DeckRogue.Game {
 public partial class Autopilot {
  IEnumerator TimingReview(GameRoot g){
   var cases=Arg("-timing-sequence")=="1" ? new[]{"enemy","enemy-shield","enemy-fast","enemy-rebuild"} : new[]{"idle","single","multi","rapid","spell","enemy"};
   foreach(var name in cases){
    string hand=name=="multi"?"green_double_lash":name=="spell"?"blue_strike":"green_strike,green_strike,green_strike,green_strike";
    yield return StateJump(g,"phase=combat;leader="+(name=="idle"||name=="spell"?"leader_blue":"leader_green")+";enemy=enemy_probe;hand="+hand+";penergy=20"+(name=="enemy-shield"?";pblock=10":""));
    Presenter.MarkSeen(g.Rs.Combat);yield return new WaitForSecondsRealtime(1);
    CombatTimingProbe.Begin();int budget=180,timeouts=g.PresentationTimeoutCount;
    for(int f=0;f<budget&&(f<180||!TransitionIdle(g));f++){
     if(name!="idle"&&(f==30||(name=="rapid"&&(f==33||f==36||f==39)))){
      g.PreferredTarget=0;
      if(name.StartsWith("enemy")){CombatTimingProbe.Event("input","endTurn");g.DoCombat(new DeckRogue.Engine.Generated.Command_EndTurn());budget=Mathf.Max(budget,30+TransitionBudgetFrames());}
      else {var card=g.Battle.CardAt(0);CombatTimingProbe.Event("input",card.Card.Def.Id);ExecuteEvents.Execute(card.Rt.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);}
      CombatTimingProbe.Event("inputReturned");
     }
     if(name=="enemy-rebuild" && f==45){g.Rebuild();CombatTimingProbe.Event("rebuild","sameState");}
     if(name=="enemy-fast" && f==45){
      var before=g.Rs;var hc=g.Battle.CardAt(0);TempoRequire(g.CombatPresentationPending && hc!=null,"enemy presentation gate missing");
      CombatTimingProbe.Event("input","earlyCard:"+hc.Card.Def.Id);
      ExecuteEvents.Execute(hc.Rt.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
      TempoRequire(g.Error==null && ReferenceEquals(before,g.Rs),"early input bypassed enemy presentation gate");
      g.DoCombat(new Command_EndTurn());TempoRequire(ReferenceEquals(before,g.Rs),"rapid EndTurn bypassed enemy presentation gate");
      CombatTimingProbe.Event("inputReturned","earlyCard");
     }
     yield return new WaitForEndOfFrame();CombatTimingProbe.Sample();
     if(Arg("-timing-capture")=="1" && name.StartsWith("enemy")) {var frames=Path.Combine(_dir,name+"-frames");Directory.CreateDirectory(frames);var shot=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(frames,"frame_"+f.ToString("D4")+".png"),shot.EncodeToPNG());Destroy(shot);}
     yield return null;
    }
    if(name.StartsWith("enemy")){
     TempoRequire(TransitionIdle(g)&&g.PresentationTimeoutCount==timeouts,"enemy presentation failed to drain naturally "+name);
     TempoRequire(g.Battle.ShownPlayerBlock==g.Rs.Combat.Player.Block && g.Battle.ShownPlayerIce==g.Rs.Combat.Player.IceBlock,"stale defense display "+name);
     var bar=g.Battle.SelfArea.GetComponentInChildren<BattleScreen.HpBarInfo>();
     TempoRequire(bar!=null && bar.Value==g.Rs.Combat.Player.Hp,"stale HP display "+name);
     CombatTimingProbe.Event("displayVerified",name);
    }
    CombatTimingProbe.Save(Path.Combine(_dir,"timing-"+name+".json"));
   }
  }
  // PC-only contract runner. No captures by default; StateJump's incidental shots are suppressed.
  // DeckRogue.exe -autopilot transition-review -shots <isolated-directory> -det
  bool _transitionReviewFailed;
  static readonly string[] TransitionCases={"ordinaryRapid","enemy","multiEnemy","multiHit","burnLethal","thornsDeath","scaldDeath","rapidEndTurn","sameStateRebuild","reset","leaveCombat","victory","missingTarget","pendingCardThenEndTurn","resetDuringFinish","leaveDuringFinish","deathSaved","reactionContinuation","flee","multiHitVictory"};
  static bool TransitionIdle(GameRoot g)=>!g.CombatPresentationPending&&Presenter.PresentationIdle&&!Tween.FinishSlowActive;
  static int TransitionBudgetFrames()=>Mathf.Max(240,Mathf.CeilToInt((Mathf.Max(Presenter.LastSequenceDuration,Presenter.LastImpactDelay)+4f)*60f/.22f));
  static string[] TransitionHand(GameRoot g) {
   var result=new List<string>();if(g.Battle==null)return result.ToArray();
   for(int i=0;i<100;i++){var card=g.Battle.CardAt(i);if(card==null)break;result.Add(card.Card.Uid);}
   return result.ToArray();
  }
  static void TransitionClick(BattleView.HandCard card) {
   if(card!=null&&card.Rt!=null)ExecuteEvents.Execute(card.Rt.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
  }
  static void TransitionRejectedInputs(GameRoot g,JArray errors,string at) {
   var before=g.Rs;string error=g.Error;var pending=g.Pending;
   TransitionClick(g.Battle?.CardAt(0));
   PresentationCheck(errors,ReferenceEquals(before,g.Rs)&&ReferenceEquals(pending,g.Pending)&&g.Error==error,"UI card bypassed gate: "+at);
   if(g.Battle?.UiLayer!=null) {
    var end=g.Battle.UiLayer.GetComponentsInChildren<UnityEngine.UI.Button>().FirstOrDefault(b=>b.GetComponentInChildren<TMPro.TMP_Text>()?.text=="ターン終了");
    PresentationCheck(errors,end!=null,"old EndTurn control missing: "+at);
    if(end!=null)ExecuteEvents.Execute(end.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
    PresentationCheck(errors,ReferenceEquals(before,g.Rs)&&ReferenceEquals(pending,g.Pending)&&g.Error==error,"UI EndTurn bypassed gate: "+at);
   }
   var next=before?.Combat?.Player.Hand.FirstOrDefault();
   if(next!=null)g.DoCombat(new Command_PlayCard{CardUid=next.Uid,TargetIndex=0});
   PresentationCheck(errors,ReferenceEquals(before,g.Rs)&&ReferenceEquals(pending,g.Pending)&&g.Error==error,"direct card bypassed gate: "+at);
   for(int i=0;i<3;i++)g.DoCombat(new Command_EndTurn());
   PresentationCheck(errors,ReferenceEquals(before,g.Rs)&&ReferenceEquals(pending,g.Pending)&&g.Error==error,"rapid direct EndTurn bypassed gate: "+at);
  }
  static void TransitionDisplay(GameRoot g,GameState final,JArray errors,bool missing) {
   PresentationCheck(errors,g.Battle!=null,"battle disappeared before final display check");if(g.Battle==null)return;
   var hp=g.Battle.SelfArea?.GetComponentInChildren<BattleScreen.HpBarInfo>();
   PresentationCheck(errors,hp!=null&&hp.Value==Math.Max(0,final.Player.Hp),"final player HP display is stale");
   if(hp!=null)PresentationCheck(errors,Mathf.Abs(hp.Now-Math.Max(0,final.Player.Hp))<.1f,"player HP tween did not finish before unlock");
   PresentationCheck(errors,g.Battle.ShownPlayerBlock==final.Player.Block&&g.Battle.ShownPlayerIce==final.Player.IceBlock,"final block/ice display is stale");
   for(int i=0;i<final.Enemies.Count;i++) {
    if(missing&&i==0)continue;
    var bar=PresentationBar(g,i);
    if(final.Enemies[i].Hp<=0&&bar==null)continue;
    PresentationCheck(errors,bar!=null&&bar.Value==Math.Max(0,final.Enemies[i].Hp),"final enemy HP display is stale: "+i);
    if(bar!=null)PresentationCheck(errors,Mathf.Abs(bar.Now-Math.Max(0,final.Enemies[i].Hp))<.1f,"enemy HP tween did not finish before unlock: "+i);
    if(final.Enemies[i].Hp>0) {
     var tag=g.Anchor("enemy"+i)?.Find("intent-tag");
     PresentationCheck(errors,tag!=null,"next-turn intent missing: "+i);
     // Compare the rendered short intent against the UI's canonical formatter, including multi-hit values.
     var it=Effects.EffectiveIntent(final,i)??final.Enemies[i].Intent;
     var formatter=typeof(BattleScreen).GetMethod("IntentMetrics",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
     if(tag!=null&&it!=null&&formatter!=null) {
      var layout=g.Anchor("enemy"+i).GetComponent<BattleScreen.IntentLayout>();
      object[] args={final,i,it,layout!=null?layout.Gap:1000f,false,false,0f,0,0f,null,null,0f,0f,0f};
      formatter.Invoke(null,args);string expected=(string)args[9];
      PresentationCheck(errors,tag.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text==expected),"next-turn intent text is stale: "+i);
     }
    }
   }
   var incoming=g.Battle.SelfArea?.GetComponentInChildren<BattleScreen.IncomingInfo>();
   if(incoming!=null)PresentationCheck(errors,incoming.Incoming==Effects.IncomingTotal(final),"next-turn incoming damage display is stale");
  }

  IEnumerator TransitionReview(GameRoot g) {
   int originalFramerate=Time.captureFramerate;bool originalShowcase=HD2DFlags.CombatPresentationShowcase;
   EnableDet();_transitionReviewFailed=false;
   var rows=new JArray();var initialBaselines=new Dictionary<string,JToken>();var finalBaselines=new Dictionary<string,JToken>();
   var report=new JObject{{"status","running"},{"platform",Application.platform.ToString()},{"unity",Application.unityVersion},{"capture",false},{"fps",60},{"seed",_seed},{"savePath",SaveGame.FilePath},{"cases",rows}};
   string path=Path.Combine(_dir,"transition-review.json");
   foreach(var mode in new[]{"baseline","showcase"})foreach(var name in TransitionCases)rows.Add(new JObject{{"mode",mode},{"case",name},{"status","not_run"}});
   File.WriteAllText(path,report.ToString());
   try {
    foreach(JObject row in rows) {
     string name=(string)row["case"],mode=(string)row["mode"];var errors=new JArray();row["errors"]=errors;row["status"]="running";
     File.WriteAllText(path,report.ToString());
     yield return Guard(TransitionReviewCase(g,name,mode=="showcase",row,errors),e=>errors.Add(e.ToString()));
     if(CombatTimingProbe.Enabled)CombatTimingProbe.Save(Path.Combine(_dir,"transition-"+mode+"-"+name+"-timing.json"));
     if(row["initialEngine"]!=null&&row["finalEngine"]!=null) {
      if(mode=="baseline"){initialBaselines[name]=row["initialEngine"].DeepClone();finalBaselines[name]=row["finalEngine"].DeepClone();}
      else {
       bool initial=initialBaselines.TryGetValue(name,out var a)&&JToken.DeepEquals(a,row["initialEngine"]);
       bool final=finalBaselines.TryGetValue(name,out var b)&&JToken.DeepEquals(b,row["finalEngine"]);
       row["sameInitialEngineAsBaseline"]=initial;row["sameFinalEngineAsBaseline"]=final;
       PresentationCheck(errors,initial&&final,"baseline/showcase immutable engine states differ");
      }
     } else PresentationCheck(errors,false,"initial/final engine evidence missing");
     row["status"]=errors.Count==0?"completed":"failed";_transitionReviewFailed|=errors.Count>0;
     File.WriteAllText(path,report.ToString());
     Presenter.Reset(); // One failed scenario must not leak a gate or a slow clock into the next.
    }
    report["status"]=_transitionReviewFailed?"failed":"completed";report["completedCases"]=rows.Count(r=>(string)r["status"]=="completed");
    File.WriteAllText(path,report.ToString());Debug.Log("[TransitionReview] "+report["status"]+"; evidence "+path);
   } finally {Presenter.Reset();HD2DFlags.CombatPresentationShowcase=originalShowcase;Time.captureFramerate=originalFramerate;}
  }

  IEnumerator TransitionReviewCase(GameRoot g,string name,bool showcase,JObject row,JArray errors) {
   bool ordinary=name=="ordinaryRapid",duringFinish=name=="resetDuringFinish"||name=="leaveDuringFinish";
   bool leaving=name=="leaveCombat"||name=="leaveDuringFinish",reset=name=="reset"||name=="resetDuringFinish";
   bool lifecycle=leaving||reset,cardTerminal=name=="victory"||name=="multiHitVictory"||name=="thornsDeath"||duringFinish;
   string mode=showcase?"showcase":"baseline";
   string hand=name=="multiHitVictory"?"green_double_lash,green_strike,green_guard":cardTerminal?"green_finisher_stomp,green_strike,green_guard":name=="pendingCardThenEndTurn"?"green_double_lash,green_strike,green_guard":"green_strike,green_strike,green_guard";
   yield return ResetForLine(g,Screen.width,Screen.height);Time.captureFramerate=60;UnityEngine.Random.InitState(DetRandomSeed);
   string spec="phase=combat;hd2d=slice;tier=pc;difficulty=1;leader=leader_green;enemy=enemy_probe;hand="+hand+";penergy=40;combatpresentation="+mode;
   row["state"]=spec;yield return StateJump(g,spec);
   TempoRequire(g.Rs?.Combat!=null&&g.Battle!=null&&Stage.CombatWorld&&!UiKit.Phone,"transition-review requires a built PC diorama");
   TempoRequire(HD2DFlags.CombatPresentationShowcase==showcase,"combatpresentation flag was not applied");
   var st=g.Rs.Combat;int count=name=="multiEnemy"||name=="missingTarget"?4:1;
   var enemies=Enumerable.Range(0,count).Select(i=>st.Enemies[0] with {
    Hp=name=="burnLethal"||name=="victory"||duringFinish?1:name=="multiHitVictory"?9:500,MaxHp=500,Block=0,Burn=name=="burnLethal"?3:0,Thorns=name=="thornsDeath"?5:0,Exposed=0,Strength=0,
    Intent=new EnemyIntent{Kind=name=="flee"?"flee":"attack",Base=4,Actual=4,Hits=name=="multiHit"?3:1}
   }).ToList();
   var player=st.Player with {Hp=name=="thornsDeath"||name=="scaldDeath"||name=="deathSaved"?1:200,MaxHp=200,Block=name=="thornsDeath"||name=="deathSaved"?0:7,IceBlock=name=="thornsDeath"||name=="deathSaved"?0:3,Energy=40,EnergyMax=5,EnergyMaxAtTurnStart=5,DrawPile=Enumerable.Range(0,10).Select(i=>new CardInstance{Uid="transition-next-"+i,Def=Content.GetCardDef("green_strike")}).ToArray()};
   if(name=="scaldDeath")player=player with {Hand=new[]{new CardInstance{Uid="transition-scald",Def=Content.SCALD_DEF}}};
   if(name=="reactionContinuation")player=player with {SetCards=new[]{new CardInstance{Uid="transition-reaction",Def=Content.GetCardDef("green_reaction_preempt"),SetTurn=st.Turn-1}}};
   g.Rs=g.Rs with {Combat=st with {Player=player,Enemies=enemies,GearDeathSave=name=="deathSaved",ReactionMode=name=="reactionContinuation"?ReactionModes.SetConfirm:ReactionModes.SetAuto}};
   if(name=="reactionContinuation") {
    g.Rs=DeckRogue.Engine.Run.ApplyRunCommand(g.Rs,new RunCommand_Combat{Command=new Command_EndTurn()});
    TempoRequire(g.Rs.Combat.Phase==CombatPhases.AwaitingReaction,"reaction continuation fixture did not reach its confirmation window");
   }
   Presenter.Reset();Presenter.MarkSeen(g.Rs.Combat);g.Rebuild();for(int f=0;f<45;f++)yield return null;Canvas.ForceUpdateCanvases();
   TempoRequire(TransitionIdle(g),"fixture began with active presentation");
   var initial=g.Rs;row["initialEngine"]=JToken.FromObject(initial.Combat);
   int from=initial.Combat.EventLog.Count,received=ResolvedCombatFx.Received,presented=ResolvedCombatFx.Presented,duplicates=ResolvedCombatFx.DuplicateSuppressed;
   int world=ResolvedCombatFx.WorldPresented,fallback=ResolvedCombatFx.FallbackPresented,startFrame=Time.frameCount,timeouts=g.PresentationTimeoutCount;
   var actions=new JArray();var samples=new JArray();row["actions"]=actions;row["frames"]=samples;
   CombatTimingProbe.Begin();g.PreferredTarget=0;
   if(ordinary||name=="pendingCardThenEndTurn") {
    int plays=ordinary?2:1;
    for(int n=0;n<plays;n++) {
     var before=g.Rs;var card=before.Combat.Player.Hand[0];var expected=DeckRogue.Engine.Run.ApplyRunCommand(before,new RunCommand_Combat{Command=new Command_PlayCard{CardUid=card.Uid,TargetIndex=0}});
     int frame=Time.frameCount;var timer=System.Diagnostics.Stopwatch.StartNew();TransitionClick(g.Battle.CardAt(0));timer.Stop();
     PresentationCheck(errors,Time.frameCount==frame&&g.Error==null&&JToken.DeepEquals(JToken.FromObject(expected),JToken.FromObject(g.Rs)),"ordinary card did not synchronously match engine result");
     PresentationCheck(errors,!g.CombatPresentationPending&&Mathf.Approximately(Time.timeScale,1f),"ordinary card acquired transition gate or global slow");
     PresentationCheck(errors,GameObject.Find("inputblock")==null&&GameObject.Find("inputblock2")==null,"ordinary card added an input blocker");
     PresentationCheck(errors,TransitionHand(g).SequenceEqual(g.Rs.Combat.Player.Hand.Select(c=>c.Uid)),"ordinary card hand was not immediately refreshed");
     actions.Add(new JObject{{"command",card.Def.Id},{"sameFrame",frame==Time.frameCount},{"returnMs",timer.Elapsed.TotalMilliseconds},{"pendingImpacts",Presenter.PendingImpactCount}});
    }
    PresentationCheck(errors,Presenter.PendingImpactCount>0,"ordinary-card fixture lacks pending contacts");
   }
   string[] oldHand=TransitionHand(g);var board=g.Battle;int oldRevision=board.DisplayRevision;
   if(!ordinary) {
    var before=g.Rs;Command cmd=cardTerminal?(Command)new Command_PlayCard{CardUid=before.Combat.Player.Hand[0].Uid,TargetIndex=0}:name=="reactionContinuation"?(Command)new Command_ConfirmReaction{Fire=false}:new Command_EndTurn();
    var expected=DeckRogue.Engine.Run.ApplyRunCommand(before,new RunCommand_Combat{Command=cmd});int frame=Time.frameCount;
    var timer=System.Diagnostics.Stopwatch.StartNew();
    if(cardTerminal)TransitionClick(g.Battle.CardAt(0));else g.DoCombat(cmd);timer.Stop();
    PresentationCheck(errors,Time.frameCount==frame&&g.Error==null&&JToken.DeepEquals(JToken.FromObject(expected),JToken.FromObject(g.Rs)),"gated command did not synchronously match immutable engine result");
    PresentationCheck(errors,g.CombatPresentationPending,"transition input gate was not acquired synchronously");
    PresentationCheck(errors,ReferenceEquals(g.Battle,board),"old combat board was replaced before its presentation");
    PresentationCheck(errors,TransitionHand(g).SequenceEqual(oldHand),"hand changed before the transition gate drained");
    TransitionRejectedInputs(g,errors,"immediate");
    actions.Add(new JObject{{"command",cardTerminal?"terminalCard":name=="reactionContinuation"?"ConfirmReaction":"EndTurn"},{"sameFrame",frame==Time.frameCount},{"returnMs",timer.Elapsed.TotalMilliseconds},{"gated",g.CombatPresentationPending}});
   }
   var resolved=g.Rs.Combat.EventLog.Skip(from).Where(ResolvedCombatFx.IsResolved).ToList();var finalState=g.Rs.Combat;var finalRun=g.Rs;
   bool terminal=g.Rs.Phase!=RunPhases.Combat,won=terminal&&g.Rs.Phase!=RunPhases.Lost;
   row["finalEngine"]=JToken.FromObject(finalState);row["finalPhase"]=g.Rs.Phase;row["expectedEvents"]=JArray.FromObject(resolved);row["expectedReceipts"]=resolved.Count;
   PresentationCheck(errors,name=="flee"?resolved.Count==0:resolved.Count>0,"fixture has an unexpected resolved receipt count");
   if(name=="flee")PresentationCheck(errors,won&&finalState.EventLog.Skip(from).OfType<GameEvent_EnemyFled>().Count()==1,"flee did not cause a non-damage terminal transition");
   if(name=="deathSaved")PresentationCheck(errors,!terminal&&finalState.Player.Hp>0&&finalState.EventLog.Skip(from).OfType<GameEvent_DeathSaved>().Count()==1,"death-save fixture did not survive one lethal enemy hit");
   if(name=="reactionContinuation")PresentationCheck(errors,finalState.Phase==CombatPhases.PlayerTurn&&finalState.PendingWindow==null,"reaction continuation did not resolve the enemy phase");
   if(name=="multiEnemy")PresentationCheck(errors,resolved.OfType<GameEvent_DamageDealt>().Count(e=>e.Source!="player")==4,"multi-enemy fixture did not resolve four enemy hits");
   if(name=="multiHit") {
    // The engine consumes three hits individually, then emits one aggregate damage receipt.
    var hit=resolved.OfType<GameEvent_DamageDealt>().SingleOrDefault(e=>e.Source!="player");
    PresentationCheck(errors,initial.Combat.Enemies[0].Intent.Hits==3&&hit!=null&&hit.Amount==12&&hit.Blocked==10&&hit.HpLoss==2,"three-hit enemy fixture lost aggregate damage/defense accounting");
   }
   if(name=="multiHitVictory")PresentationCheck(errors,won&&resolved.OfType<GameEvent_DamageDealt>().Count(e=>e.Source=="player")==2,"multi-hit victory must retain both finishing-card receipts");
   if(name=="burnLethal")PresentationCheck(errors,won&&resolved.OfType<GameEvent_BurnTick>().Any(),"burn did not cause terminal victory");
   if(name=="scaldDeath")PresentationCheck(errors,g.Rs.Phase==RunPhases.Lost&&resolved.OfType<GameEvent_ScaldTick>().Any(),"scald did not cause terminal death");
   if(name=="thornsDeath")PresentationCheck(errors,g.Rs.Phase==RunPhases.Lost&&resolved.OfType<GameEvent_ThornsReflected>().Any(),"thorns did not cause terminal death");
   if(name=="victory"||duringFinish)PresentationCheck(errors,won,"finishing card did not resolve victory");
   if(name=="pendingCardThenEndTurn")PresentationCheck(errors,resolved.OfType<GameEvent_DamageDealt>().Count(e=>e.Source=="player")==2,"earlier card's two contacts are absent");
   if(name=="sameStateRebuild") {
    int impacts=Presenter.PendingImpactCount,visuals=Presenter.PendingVisualCount,tweens=Tween.PendingPresentationCount;
    g.Rebuild();
    PresentationCheck(errors,ReferenceEquals(g.Rs,finalRun)&&ReferenceEquals(g.Battle,board)&&board.DisplayRevision==oldRevision,"same-state rebuild was not deferred");
    PresentationCheck(errors,g.CombatPresentationPending&&Presenter.PendingImpactCount==impacts&&Presenter.PendingVisualCount==visuals&&Tween.PendingPresentationCount==tweens,"same-state rebuild cancelled presentation work");
    PresentationCheck(errors,TransitionHand(g).SequenceEqual(oldHand),"same-state rebuild exposed new hand");
   }
   bool missing=name=="missingTarget";
   if(missing) {var gone=g.Anchor("enemy0");TempoRequire(gone!=null,"missing-target fixture lacks enemy0");g.Battle.KillEnemy(g,0,false);g.RegisterAnchor("enemy0",null);PresentationCheck(errors,g.Anchor("enemy0")==null,"missing-target anchor was not removed");}
   bool cancelled=false;int cancelledReceipts=0,presentedAtCancel=0;
   Action cancel=()=>{
    cancelledReceipts=resolved.Count-(ResolvedCombatFx.Presented-presented);presentedAtCancel=ResolvedCombatFx.Presented;cancelled=true;
    if(leaving)g.BackToSetup();else {Presenter.Reset();Presenter.MarkSeen(g.Rs.Combat);g.Rebuild();}
    PresentationCheck(errors,!g.CombatPresentationPending&&Presenter.PresentationIdle&&Tween.PendingPresentationCount==0&&!Tween.FinishSlowActive&&Mathf.Approximately(Time.timeScale,1f),"leave/reset failed to synchronously release gate, work or slow clock");
   };
   if(lifecycle&&!duringFinish)cancel();
   int frames=0,budget=TransitionBudgetFrames(),gateFrames=0,slowFrames=0,holdFrames=0,rampFrames=0;
   bool wasGated=g.CombatPresentationPending,sawSlow=false,sawUnlock=false,rebuildMid=false,rejectedMid=false;
   bool terminalHpSeen=false;float minimumScale=1f;row["sequenceDuration"]=Presenter.LastSequenceDuration;row["drainBudgetFrames"]=budget;
   do {
    yield return new WaitForEndOfFrame();CombatTimingProbe.Sample();frames++;
    bool gate=g.CombatPresentationPending,slow=Tween.FinishSlowActive;
    if(gate) {
     gateFrames++;PresentationCheck(errors,ReferenceEquals(g.Battle,board),"combat screen advanced while its presentation gate was active");
     PresentationCheck(errors,TransitionHand(g).SequenceEqual(oldHand),"hand changed while transition presentation was pending");
     if(!rejectedMid&&ResolvedCombatFx.Presented>presented){TransitionRejectedInputs(g,errors,"mid-presentation");rejectedMid=true;}
    }
    if(!cancelled&&terminal&&gate&&ResolvedCombatFx.Presented-presented==resolved.Count) {
     var playerBar=g.Battle?.SelfArea?.GetComponentInChildren<BattleScreen.HpBarInfo>();
     bool targets=playerBar!=null&&playerBar.Value==Math.Max(0,finalState.Player.Hp);
     for(int i=0;i<finalState.Enemies.Count;i++) {
      var bar=PresentationBar(g,i);targets&=bar!=null?bar.Value==Math.Max(0,finalState.Enemies[i].Hp):finalState.Enemies[i].Hp<=0;
     }
     terminalHpSeen|=targets;
    }
    if(!cancelled&&wasGated&&!gate) {
     sawUnlock=true;PresentationCheck(errors,Presenter.PresentationIdle&&Tween.PendingPresentationCount==0&&!slow,"gate released before all presentation work drained");
     PresentationCheck(errors,ResolvedCombatFx.Presented-presented==resolved.Count,"gate released before final resolved receipt");
     if(!terminal)TransitionDisplay(g,finalState,errors,missing);
    }
    if(!cancelled&&terminal&&!sawUnlock)PresentationCheck(errors,g.Battle!=null,"reward/defeat screen replaced combat before unlock");
    if(slow) {
     sawSlow=true;slowFrames++;minimumScale=Mathf.Min(minimumScale,Time.timeScale);
     if(Mathf.Abs(Time.timeScale-.22f)<.005f)holdFrames++;
     else if(Time.timeScale>.22f&&Time.timeScale<1f)rampFrames++;
     PresentationCheck(errors,gate&&g.Battle!=null,"victory slow outlived its guarded battle screen");
     PresentationCheck(errors,won,"victory slow ran for a non-victory command");
     if(duringFinish&&!cancelled)cancel();
    } else if(!terminal&&!cancelled)PresentationCheck(errors,Mathf.Approximately(Time.timeScale,1f),"non-terminal command globally slowed the clock");
    if(name=="sameStateRebuild"&&!rebuildMid&&gate&&ResolvedCombatFx.Presented>presented) {
     var before=TransitionHand(g);int revision=board.DisplayRevision,impacts=Presenter.PendingImpactCount;
     g.Rebuild();rebuildMid=true;
     PresentationCheck(errors,revision==board.DisplayRevision&&impacts==Presenter.PendingImpactCount&&TransitionHand(g).SequenceEqual(before),"mid-sequence rebuild escaped the deferred gate");
    }
    samples.Add(new JObject{{"frame",Time.frameCount-startFrame},{"gate",gate},{"board",g.Battle!=null},{"timeScale",Time.timeScale},{"finishSlow",slow},{"received",ResolvedCombatFx.Received-received},{"presented",ResolvedCombatFx.Presented-presented},{"pendingImpacts",Presenter.PendingImpactCount},{"pendingVisuals",Presenter.PendingVisualCount},{"pendingTweens",Tween.PendingPresentationCount},{"activeFx",WorldBattleFx.ActiveCount},{"motion",Stage.HasCombatMotion},{"hand",new JArray(TransitionHand(g))}});
    wasGated=g.CombatPresentationPending;
    yield return null;
   }while(frames<budget&&(frames<(lifecycle?90:3)||!TransitionIdle(g)||(!ordinary&&!lifecycle&&!sawUnlock)));
   if(!ordinary&&!lifecycle)PresentationCheck(errors,sawUnlock&&gateFrames>0,"did not observe the gate remain active and then unlock");
   if(name=="sameStateRebuild")PresentationCheck(errors,rebuildMid,"rebuild was not exercised during the enemy sequence");
   if(duringFinish)PresentationCheck(errors,sawSlow&&cancelled,"lifecycle interruption did not exercise active victory slow");
   if(name=="flee")PresentationCheck(errors,!sawSlow,"non-damage flee transition incorrectly played a finishing hit slow");
   if(won&&!lifecycle&&name!="flee") {
    PresentationCheck(errors,sawSlow&&Mathf.Abs(minimumScale-.22f)<.005f,"victory did not reach the required .22x slow");
    // EndOfFrame then null resumes at the following frame; allow two boundary samples.
    PresentationCheck(errors,holdFrames/60f>=.45f-4/60f&&holdFrames/60f<=.45f+4/60f,"victory .22x hold differed from .45 seconds");
    PresentationCheck(errors,rampFrames/60f>=.2f-4/60f&&rampFrames/60f<=.2f+4/60f,"victory recovery ramp differed from .2 seconds");
   }
   PresentationCheck(errors,g.PresentationTimeoutCount==timeouts,"completion used the watchdog instead of naturally draining presentation");
   PresentationCheck(errors,TransitionIdle(g)&&frames<budget,"presentation did not drain within its timeline-derived budget");
   PresentationCheck(errors,Mathf.Approximately(Time.timeScale,1f)&&!Tween.FinishSlowActive,"finish/reset left a slow clock behind");
   PresentationCheck(errors,GameObject.Find("inputblock")==null&&GameObject.Find("inputblock2")==null,"input blocker survived completion");
   PresentationCheck(errors,g.Pending==null,"input rejection left a pending card selection");
   PresentationCheck(errors,ResolvedCombatFx.Received-received==resolved.Count,"resolved receipt capture count mismatch");
   PresentationCheck(errors,ResolvedCombatFx.Presented-presented+cancelledReceipts==resolved.Count,"presented + explicitly cancelled receipts do not equal resolved events");
   PresentationCheck(errors,ResolvedCombatFx.DuplicateSuppressed==duplicates,"duplicate receipt scheduling");
   PresentationCheck(errors,ResolvedCombatFx.WorldPresented-world+ResolvedCombatFx.FallbackPresented-fallback==ResolvedCombatFx.Presented-presented,"receipt surface counts disagree");
   if(cancelled)PresentationCheck(errors,ResolvedCombatFx.Presented==presentedAtCancel,"cancelled receipt escaped after lifecycle reset");
   if(!leaving&&!terminal) {
    TransitionDisplay(g,finalState,errors,missing);
    PresentationCheck(errors,TransitionHand(g).SequenceEqual(finalState.Player.Hand.Select(c=>c.Uid)),"hand did not become authoritative after unlock");
   }
   if(terminal&&!reset&&!leaving) {
    PresentationCheck(errors,g.Battle==null,"terminal screen was not shown after the presentation completed");
    PresentationCheck(errors,terminalHpSeen,"terminal HP targets were never shown on the guarded combat board before reward/defeat");
   }
   if(!lifecycle)PresentationCheck(errors,ReferenceEquals(g.Rs,finalRun),"presentation completion mutated immutable engine state");
   if(leaving)PresentationCheck(errors,g.Rs==null&&g.Battle==null,"leaving combat was undone by a stale callback");
   row["watchdogTimeouts"]=g.PresentationTimeoutCount-timeouts;row["drainFrames"]=frames;row["gateFrames"]=gateFrames;row["sawUnlock"]=sawUnlock;row["slowFrames"]=slowFrames;row["holdSamples"]=holdFrames;row["rampSamples"]=rampFrames;row["minimumScale"]=minimumScale;
   row["received"]=ResolvedCombatFx.Received-received;row["presented"]=ResolvedCombatFx.Presented-presented;row["cancelledByLifecycle"]=cancelledReceipts;row["finalTimeScale"]=Time.timeScale;
   row["terminalHpShownBeforeScreenChange"]=terminalHpSeen;row["finalGate"]=g.CombatPresentationPending;row["finalPendingTweens"]=Tween.PendingPresentationCount;row["finalPresentationIdle"]=Presenter.PresentationIdle;
   if(!lifecycle&&!terminal&&!ordinary) {
    // Deliberately distinct next-turn fixture cards prove that the hand changed only on unlock.
    PresentationCheck(errors,!oldHand.SequenceEqual(TransitionHand(g)),"fixture never replaced the old-turn hand");
    var card=g.Battle.CardAt(0);TempoRequire(card!=null&&card.Playable,"new-turn card stayed unavailable after unlock");
    var before=g.Rs;int target=missing?1:0;g.PreferredTarget=target;
    var expected=DeckRogue.Engine.Run.ApplyRunCommand(before,new RunCommand_Combat{Command=new Command_PlayCard{CardUid=card.Card.Uid,TargetIndex=target}});
    TransitionClick(card);
    PresentationCheck(errors,g.Error==null&&JToken.DeepEquals(JToken.FromObject(expected),JToken.FromObject(g.Rs))&&!g.CombatPresentationPending,"first UI card after unlock was rejected or re-gated");
    row["postUnlockInputAccepted"]=!ReferenceEquals(before,g.Rs);int afterFrames=0,afterBudget=TransitionBudgetFrames();
    do{yield return null;afterFrames++;}while(!TransitionIdle(g)&&afterFrames<afterBudget);
    PresentationCheck(errors,TransitionIdle(g)&&Mathf.Approximately(Time.timeScale,1f),"post-unlock ordinary card did not drain cleanly");
   }
  }
 }
}

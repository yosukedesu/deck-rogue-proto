using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
namespace DeckRogue.Game {
 public partial class Autopilot {
  IEnumerator VfxCoverage(GameRoot g) {
   var report=new JArray();
   var cases=new[]{"single","fire","multi","aoe","iceScale","drain","aether","conditional","guard","zero","enemy","enemyGuard","burn","thorns","cost","cancel","lethal","doll","scald","reaction","heal","missingTarget","unknownProfile","fusion","proceduralFusion"};
   foreach(var name in cases) {
    string card=name=="heal"?"white_mass_heal":name=="fire"?"red_strike":name=="multi"?"red_double_tap":name=="aoe"||name=="conditional"?"green_earth_roar":name=="iceScale"||name=="zero"?"blue_ice_lance":name=="drain"?"black_drain":name=="aether"?"blue_aether_burst":name=="cost"?"black_dark_pact":"green_strike";
    yield return StateJump(g,"phase=combat;leader=leader_green;enemy="+(name=="aoe"||name=="cancel"?"enc_biting_scrolls_quad":"enemy_probe")+";hand="+card+";penergy=20;dumplayout=1");
    var st=g.Rs.Combat;var foes=st.Enemies.ToList();
    foes[0]=foes[0] with {Block=name=="guard"?100:0,Hp=name=="lethal"?1:foes[0].Hp,Burn=name=="burn"?5:0,Thorns=name=="thorns"?3:0};
    var pl=st.Player with {Hp=30,IceBlock=name=="iceScale"?8:0,Aether=3,Block=name=="enemyGuard"?100:0,EnergyMax=name=="conditional"?5:st.Player.EnergyMax};
    if(name=="doll")pl=pl with {Permanents=new[]{new CardInstance{Uid="vfx-doll",Def=Content.GetCardDef("white_perm_squire")}}};
    if(name=="fusion")pl=pl with {Hand=new[]{new CardInstance{Uid="vfx-fusion",Def=Content.AllFusions.First(x=>x.Result.Id=="fusion_vine_wheel").Result}}};
    if(name=="proceduralFusion")pl=pl with {Hand=new[]{new CardInstance{Uid="vfx-procedural",Def=Fusion.FuseCards(new CardInstance{Uid="a",Def=Content.GetCardDef("blue_strike")},new CardInstance{Uid="b",Def=Content.GetCardDef("blue_ice_lance")})}}};
    if(name=="unknownProfile")pl=pl with {Hand=new[]{new CardInstance{Uid="vfx-unknown",Def=Content.GetCardDef("blue_strike") with {Id="future_unknown_attack"}}}};
    if(name=="scald")pl=pl with {Hand=new[]{new CardInstance{Uid="vfx-scald",Def=Content.SCALD_DEF}}};
    if(name=="reaction")pl=pl with {SetCards=new[]{new CardInstance{Uid="vfx-reaction",Def=Content.GetCardDef("green_reaction_preempt"),SetTurn=st.Turn-1}}};
    g.Rs=g.Rs with {Combat=st with {Player=pl,Enemies=foes,ReactionMode=ReactionModes.SetAuto}};g.Rebuild();yield return null;
    Presenter.MarkSeen(g.Rs.Combat);int from=g.Rs.Combat.EventLog.Count, received=ResolvedCombatFx.Received,shown=ResolvedCombatFx.Presented,duplicates=ResolvedCombatFx.DuplicateSuppressed;
    string error=null;int timeouts=g.PresentationTimeoutCount;
    try {
     if(name=="cancel"){g.PreferredTarget=-1;g.BeginPlay(g.Rs.Combat.Player.Hand[0],null);TempoRequire(g.Pending!=null,"cancel must open selection");g.CancelPending();}
     else if(name=="enemy"||name=="enemyGuard"||name=="burn"||name=="doll"||name=="scald"||name=="reaction")g.DoCombat(new Command_EndTurn());
     else {g.PreferredTarget=0;var hc=g.Battle.CardAt(0);ExecuteEvents.Execute(hc.Rt.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);}
     if(name=="missingTarget")g.Battle.KillEnemy(g,0,false);
     TempoRequire(g.Error==null,"command error "+g.Error);
     bool transition=name=="enemy"||name=="enemyGuard"||name=="burn"||name=="doll"||name=="scald"||name=="reaction"||g.Rs.Phase!=RunPhases.Combat;
     TempoRequire(g.CombatPresentationPending==transition,"unexpected transition gate state");
     if(!transition)TempoRequire(GameObject.Find("inputblock")==null&&GameObject.Find("inputblock2")==null,"ordinary card input lock");
    }catch(Exception e){error=e.ToString();}
    var events=g.Rs.Combat.EventLog.Skip(from).Where(ResolvedCombatFx.IsResolved).ToList();
    int frames=0,budget=TransitionBudgetFrames();do {yield return null;frames++;}while(!TransitionIdle(g)&&frames<budget);
    int actual=ResolvedCombatFx.Presented-shown;
    bool expectedEvents=name=="cancel"?events.Count==0:events.Count>0;
    bool pass=error==null&&expectedEvents&&actual==events.Count&&ResolvedCombatFx.Received-received==events.Count&&ResolvedCombatFx.DuplicateSuppressed==duplicates&&frames<budget&&g.PresentationTimeoutCount==timeouts&&Mathf.Approximately(Time.timeScale,1f);
    report.Add(new JObject{{"case",name},{"card",card},{"passed",pass},{"events",new JArray(events.Select(e=>e.Type))},{"expected",events.Count},{"presented",actual},{"received",ResolvedCombatFx.Received-received},{"drainFrames",frames},{"pendingVisuals",Presenter.PendingVisualCount},{"pendingImpacts",Presenter.PendingImpactCount},{"activeFx",WorldBattleFx.ActiveCount},{"drainBudgetFrames",budget},{"watchdogTimeouts",g.PresentationTimeoutCount-timeouts},{"error",error}});
    File.WriteAllText(Path.Combine(_dir,"vfx-coverage.json"),report.ToString());
    if(!pass){Debug.LogError("[VfxCoverage] failed "+name+" "+report.Last);Application.Quit(1);yield break;}
   }
   TempoRequire(CardVfxProfile.Style(new CardDef{Id="future_unknown",Type="spell"})=="spell","unknown spell fallback");
   TempoRequire(CardVfxProfile.Style(new CardDef{Id="future_unknown",Type="physical"})=="slash","unknown physical fallback");
   int fireMask=ElementBattleFx.FrameMasks.TryGetValue("element-fire-8f",out var mask)?mask:0;
   if(fireMask!=255){Debug.LogError("[VfxCoverage] fire frames missing mask="+fireMask);Application.Quit(1);yield break;}
   File.WriteAllText(Path.Combine(_dir,"vfx-frame-check.json"),new JObject{{"fireMask",fireMask},{"firstFrame",ElementBattleFx.Frame(0)},{"lastFrame",ElementBattleFx.Frame(.999f)}}.ToString());
   var known=Content.AllCards.Concat(Content.AllFusions.Select(x=>x.Result)).Concat(new[]{Content.WoundDef,Content.ScaldDef,Content.BrandDef,Content.GuiltDef,Content.JunkDef}).ToList();
   var missing=known.Where(c=>!CardVfxProfile.Contains(c.Id)).Select(c=>c.Id).ToArray();
   File.WriteAllText(Path.Combine(_dir,"vfx-profile-check.json"),new JObject{{"cards",known.Count},{"missing",new JArray(missing)}}.ToString());
   if(missing.Length>0){Debug.LogError("[VfxCoverage] missing profiles");Application.Quit(1);yield break;}
   Debug.Log("[VfxCoverage] PASS resolved engine events through actual UI commands; not exhaustive all-card execution");
  }
  // PC-only review runner. Build/import/play/render are not exercised by cloud source checks.
  // DeckRogue.exe -autopilot presentation-review -shots <isolated-dir> -det -presentation-capture 1
  // Omitting -presentation-capture leaves JSON probes only; baseline never inherits showcase HP assertions.
  bool _presentationReviewFailed;
  sealed class PresentationReceipt {
   public string Card,Target,Outcome; public int Amount; public float? Due; public bool Shown;
  }
  static readonly string[] PresentationProfiles={"green_strike","blue_strike","green_finisher_stomp","green_double_lash","green_earth_roar"};
  static readonly string[] PresentationCases={"green_strike","blue_strike","green_finisher_stomp","green_double_lash","green_earth_roar","rapidMixed","aoeMultiHit","missingTarget","guarded","zero","lethalOverkill","nextCardBeforeContact","sameStateRebuild","thornedMultiHit","leaveCombat","reset","inputDuringPause","blockDuringPause","resetDuringPause","leaveDuringPause","missingDuringPause"};
  static float? PresentationContact(string card) {
   switch(card){case "green_strike":return .13f;case "blue_strike":return .24f;case "green_finisher_stomp":return .34f;case "green_double_lash":return .16f;case "green_earth_roar":return .30f;default:return null;}
  }
  static float PresentationInterval(string card)=>card=="green_earth_roar" ? .15f : card=="green_double_lash" ? .12f : 0f;
  static BattleScreen.HpBarInfo PresentationBar(GameRoot g,int target) {
   var anchor=g.Anchor("enemy"+target);return anchor!=null?anchor.GetComponentInChildren<BattleScreen.HpBarInfo>():null;
  }
  static void PresentationCheck(JArray errors,bool condition,string text) {
   if(!condition&&!errors.Any(x=>(string)x==text))errors.Add(text);
  }
  static bool PresentationIdle()=>Presenter.PresentationIdle&&GameRoot.I?.CombatPresentationPending!=true;

  static JArray ReviewImpactPauseWindow() {
   var errors=new JArray();var p=new CombatImpactPause();
   p.Request(.045f,1f);for(int i=0;i<8;i++)p.Request(.045f,1f);
   PresentationCheck(errors,Mathf.Abs(p.Remaining(1f)-.045f)<.00001f,"AOE targets multiplied the pause");
   p.Clear();p.Request(.09f,1f);
   for(int i=1;i<=13;i++)p.Request(.09f,1f+i*.01f);
   PresentationCheck(errors,!p.IsPaused(1.141f),"rapid requests extended the 140ms cap");
   PresentationCheck(errors,!p.Request(.09f,1.15f)&&!p.IsPaused(1.15f),"burst did not leave a resume gap");
   PresentationCheck(errors,p.Request(.09f,1.20f),"pause did not re-arm after its resume gap");
   p.Clear();PresentationCheck(errors,!p.IsPaused(1.15f)&&p.Request(.045f,1.15f),"reset left a pause/cooldown behind");
   p.Clear();PresentationCheck(errors,!p.Request(float.NaN,2f)&&!p.Request(float.PositiveInfinity,2f)&&!p.Request(-1f,2f),"invalid pause accepted");
   return errors;
  }

  IEnumerator PresentationReview(GameRoot g) {
   bool capture=Arg("-presentation-capture")=="1";
   int originalFramerate=Time.captureFramerate;
   EnableDet(); // Fixed simulation steps and particle seeds for both halves of the comparison.
   bool originalShowcase=HD2DFlags.CombatPresentationShowcase;
   var originalHook=Presenter.EnemyImpactPresentedForReview;
   var rows=new JArray();var baselines=new Dictionary<string,JToken>();var initialBaselines=new Dictionary<string,JToken>();
   var report=new JObject{{"status","running"},{"platform",Application.platform.ToString()},{"unity",Application.unityVersion},{"capture",capture},{"fps",60},{"seed",_seed},{"savePath",SaveGame.FilePath},{"baselineHpTimingAsserted",false},{"cases",rows}};
   string reportPath=Path.Combine(_dir,"presentation-review.json");
   foreach(var mode in new[]{"baseline","showcase"})foreach(var name in PresentationCases)rows.Add(new JObject{{"mode",mode},{"case",name},{"status","not_run"}});
   File.WriteAllText(reportPath,report.ToString());
   var pauseContract=ReviewImpactPauseWindow();report["impactPauseWindowErrors"]=pauseContract;
   _presentationReviewFailed=pauseContract.Count>0;
   try {
    for(int n=0;n<rows.Count;n++) {
     var row=(JObject)rows[n];string name=(string)row["case"],mode=(string)row["mode"];
     row["status"]="running";File.WriteAllText(reportPath,report.ToString());
     var errors=new JArray();row["errors"]=errors;
     yield return Guard(PresentationReviewCase(g,name,mode=="showcase",capture,row,errors),e=>errors.Add(e.ToString()));
     // Guard flattens nested iterators; explicitly restore hooks even if a nested iterator threw.
     Presenter.EnemyImpactPresentedForReview=originalHook;
     if(CombatTimingProbe.Enabled)CombatTimingProbe.Save(Path.Combine(_dir,"presentation-"+mode+"-"+name+"-timing.json"));
     if(row["initialEngine"]!=null&&row["finalEngine"]!=null) {
      if(mode=="baseline") {initialBaselines[name]=row["initialEngine"].DeepClone();baselines[name]=row["finalEngine"].DeepClone();}
      else {
       bool sameInitial=initialBaselines.TryGetValue(name,out var initial)&&JToken.DeepEquals(initial,row["initialEngine"]);
       bool sameFinal=baselines.TryGetValue(name,out var final)&&JToken.DeepEquals(final,row["finalEngine"]);
       row["sameInitialEngineAsBaseline"]=sameInitial;row["sameFinalEngineAsBaseline"]=sameFinal;
       PresentationCheck(errors,sameInitial,"A/B fixture engine states differ");PresentationCheck(errors,sameFinal,"A/B final engine states differ");
      }
     } else PresentationCheck(errors,false,"fixture did not produce initial and final engine evidence");
     row["status"]=errors.Count==0?"completed":"failed";row["checksSatisfied"]=errors.Count==0;
     _presentationReviewFailed|=errors.Count>0;File.WriteAllText(reportPath,report.ToString());
    }
    report["status"]=_presentationReviewFailed?"failed":"completed";
    report["completedCases"]=rows.Count(x=>(string)x["status"]=="completed");
    File.WriteAllText(reportPath,report.ToString());
    Debug.Log("[PresentationReview] "+report["status"]+"; evidence "+reportPath);
   } finally {
    Presenter.EnemyImpactPresentedForReview=originalHook;
    Presenter.Reset();
    HD2DFlags.CombatPresentationShowcase=originalShowcase;
    Time.captureFramerate=originalFramerate;
   }
  }

  IEnumerator PresentationReviewCase(GameRoot g,string name,bool showcase,bool capture,JObject row,JArray errors) {
   string mode=showcase?"showcase":"baseline";
   bool duringLifecycle=name=="resetDuringPause"||name=="leaveDuringPause";
   bool lifecycle=name=="leaveCombat"||name=="reset"||duringLifecycle;
   bool terminal=name=="lethalOverkill"||name=="nextCardBeforeContact";
   string[] cards=PresentationProfiles.Contains(name)?new[]{name}:name=="rapidMixed"?PresentationProfiles:
    name=="aoeMultiHit"?new[]{"green_earth_roar","green_double_lash"}:
    name=="inputDuringPause"?new[]{"green_earth_roar","green_strike"}:
    name=="blockDuringPause"?new[]{"green_earth_roar","green_guard"}:
    duringLifecycle||name=="missingDuringPause"?new[]{"green_earth_roar"}:
    name=="nextCardBeforeContact"?new[]{"green_finisher_stomp","green_strike"}:
    name=="sameStateRebuild"||name=="thornedMultiHit"?new[]{"green_double_lash"}:
    name=="zero"?new[]{"blue_ice_lance"}:name=="lethalOverkill"?new[]{"green_finisher_stomp"}:
    lifecycle?new[]{"blue_strike"}:new[]{"green_strike"};
   bool crowd=name=="green_earth_roar"||name=="aoeMultiHit"||name=="rapidMixed"||name=="missingTarget"||name=="inputDuringPause"||name=="blockDuringPause";
   yield return ResetForLine(g,Screen.width,Screen.height);
   Time.captureFramerate=60;UnityEngine.Random.InitState(DetRandomSeed);
   string spec="phase=combat;hd2d=slice;tier=pc;difficulty=1;leader="+(cards[0].StartsWith("blue_")?"leader_blue":"leader_green")+
    ";enemy="+(crowd?"enc_biting_scrolls_quad":"enemy_probe")+";hand="+string.Join(",",cards)+";penergy=40;combatpresentation="+mode;
   row["state"]=spec;
   yield return StateJump(g,spec);
   TempoRequire(g.Rs!=null&&g.Rs.Combat!=null&&g.Battle!=null,"review fixture did not build");
   TempoRequire(Stage.CombatWorld&&!UiKit.Phone,"presentation-review requires the PC diorama");
   TempoRequire(HD2DFlags.CombatPresentationShowcase==showcase,"combatpresentation STATE flag was not applied");
   var st=g.Rs.Combat;
   var enemies=st.Enemies.Select((e,i)=>e with {Hp=name=="lethalOverkill"&&i==0?1:name=="nextCardBeforeContact"&&i==0?55:200,MaxHp=200,Block=name=="guarded"&&i==0?100:0,Exposed=0,Burn=0,Thorns=name=="thornedMultiHit"&&i==0?1:0}).ToList();
   g.Rs=g.Rs with {Combat=st with {Player=st.Player with {Energy=40,EnergyMax=5,EnergyMaxAtTurnStart=5,IceBlock=0},Enemies=enemies,ReactionMode=ReactionModes.SetAuto}};
   Presenter.Reset();Presenter.MarkSeen(g.Rs.Combat);g.Rebuild();
   // Let the baseline HP tween settle after fixture-only HP replacement before either input.
   for(int f=0;f<30;f++)yield return null;
   Canvas.ForceUpdateCanvases();
   int[] initialHp=g.Rs.Combat.Enemies.Select(e=>e.Hp).ToArray();
   int[] shownHp=(int[])initialHp.Clone();
   row["initialEngine"]=JToken.FromObject(g.Rs.Combat);
   int received=ResolvedCombatFx.Received,presented=ResolvedCombatFx.Presented,duplicates=ResolvedCombatFx.DuplicateSuppressed;
   int world=ResolvedCombatFx.WorldPresented,fallback=ResolvedCombatFx.FallbackPresented;
   var receipts=new List<PresentationReceipt>();var seenTickets=new HashSet<ResolvedCombatFx.Ticket>();
   var actions=new JArray();var contacts=new JArray();var samples=new JArray();var screenshots=new JArray();
   row["actions"]=actions;row["contacts"]=contacts;row["frames"]=samples;row["screenshots"]=screenshots;
   int from=g.Rs.Combat.EventLog.Count,startFrame=Time.frameCount;float started=Time.time;
   bool missing=false,cancelled=false,rebuiltAfterContact=false;
   int cancelledReceipts=0,presentedAtCancel=presented,poseHoldFrames=0;
   int playerHoldFrames=0,targetHoldFrames=0;
   bool duringAction=false;var pausedPoses=new Dictionary<string,JToken>();
   Action checkHp=()=>{
    if(!showcase||cancelled)return;
    for(int i=0;i<shownHp.Length;i++) {
     if(missing&&i==0)continue;
     var bar=PresentationBar(g,i);
     bool deadFinished=shownHp[i]<=0&&!receipts.Any(r=>r.Target=="enemy"+i&&!r.Shown);
     if(bar==null&&deadFinished)continue;
     PresentationCheck(errors,bar!=null,"missing HP bar for enemy"+i+" before its receipt");
     if(bar!=null)PresentationCheck(errors,bar.Value==Math.Max(0,shownHp[i]),"HP target differs from delivered receipts for enemy"+i+": expected "+Math.Max(0,shownHp[i])+", saw "+bar.Value);
    }
   };
   Action<ResolvedCombatFx.Ticket> onReceipt=t=>{
    PresentationCheck(errors,!cancelled,"receipt escaped leave/reset");
    PresentationCheck(errors,seenTickets.Add(t),"the same receipt callback ran twice");
    var match=receipts.Where(r=>!r.Shown&&r.Target==t.Target&&r.Amount==t.Amount&&r.Outcome==t.Outcome).OrderBy(r=>r.Due??float.MaxValue).FirstOrDefault();
    PresentationCheck(errors,match!=null,"unexpected receipt "+t.Target+" amount="+t.Amount+" outcome="+t.Outcome);
    if(match!=null) {
     match.Shown=true;
     if(int.TryParse(t.Target.Replace("enemy",""),out int index)&&index>=0&&index<shownHp.Length)shownHp[index]-=t.Amount;
     if(match.Due.HasValue&&!terminal) {
      float elapsed=Time.time-match.Due.Value;
      PresentationCheck(errors,elapsed>=-.002f&&elapsed<=2.1f/60f,"contact outside 2-frame tolerance: "+match.Card+" delta="+elapsed);
     }
    }
    float expectedPause=t.Outcome=="hit"&&t.Showcase!=null?CardVfxProfile.ImpactPauseFor(t.Showcase,t.Index,t.Total,t.Finishing):0f;
    PresentationCheck(errors,Mathf.Abs(t.RequestedPause-expectedPause)<.0001f,"wrong requested impact pause");
    if(showcase&&PresentationProfiles.Contains(name)&&t.Outcome=="hit")
     PresentationCheck(errors,Mathf.Abs(t.AppliedPause-expectedPause)<.0001f,"local target pause did not start at contact");
    PresentationCheck(errors,t.AppliedPause<=CombatImpactPause.MaxWindow+.0001f,"pause exceeded local cap");
    contacts.Add(new JObject{{"frame",Time.frameCount-startFrame},{"seconds",Time.time-started},{"card",match?.Card},{"target",t.Target},{"amount",t.Amount},{"outcome",t.Outcome},{"expectedSeconds",match?.Due.HasValue==true?(JToken)(match.Due.Value-started):JValue.CreateNull()},{"presented",ResolvedCombatFx.Presented-presented},{"requestedPauseMs",t.RequestedPause*1000f},{"appliedTargetPauseMs",t.AppliedPause*1000f},{"actorPauseMs",Stage.ImpactPauseRemaining("player")*1000f}});
    checkHp();
   };
   if(showcase)Presenter.EnemyImpactPresentedForReview+=onReceipt;
   CombatTimingProbe.Begin();
   try {
    for(int n=0;n<cards.Length;n++) {
     if(n>0&&(name=="inputDuringPause"||name=="blockDuringPause")) {
      int wait=0;while(ResolvedCombatFx.Presented==presented&&wait++<90)yield return null;
      PresentationCheck(errors,ResolvedCombatFx.Presented>presented,"input-during-pause fixture did not reach first contact");
      if(showcase)PresentationCheck(errors,Stage.ImpactPauseRemaining("player")>0f,"second input missed the actor's active pause");
     }
     var before=g.Rs;var combat=before.Combat;
     var card=combat.Player.Hand.First(c=>c.Def.Id==cards[n]);
     int handIndex=combat.Player.Hand.ToList().FindIndex(c=>c.Uid==card.Uid);
     var hand=g.Battle.CardAt(handIndex);
     TempoRequire(hand!=null&&hand.Card.Uid==card.Uid&&hand.Playable,"review card is not playable: "+cards[n]);
     var expected=DeckRogue.Engine.Run.ApplyRunCommand(before,new RunCommand_Combat{Command=new Command_PlayCard{CardUid=card.Uid,TargetIndex=0}});
     var added=expected.Combat.EventLog.Skip(combat.EventLog.Count).OfType<GameEvent_DamageDealt>().Where(e=>e.Source=="player").ToList();
     var occurrences=new Dictionary<int,int>();float inputTime=Time.time;
     foreach(var hit in added) {
      int target=hit.EnemyIndex??0;occurrences.TryGetValue(target,out int ordinal);occurrences[target]=ordinal+1;
      float? contact=PresentationContact(card.Def.Id);
      receipts.Add(new PresentationReceipt{Card=card.Def.Id,Target="enemy"+target,Amount=hit.HpLoss,Outcome=hit.HpLoss>0?"hit":(hit.Blocked??0)>0?"guard":"nullified",Due=contact.HasValue?inputTime+contact.Value+ordinal*PresentationInterval(card.Def.Id):(float?)null});
     }
     if(n>0&&name!="inputDuringPause"&&name!="blockDuringPause") {
      PresentationCheck(errors,Presenter.PendingImpactCount>0,"previous card contact was not pending at next input");
      PresentationCheck(errors,ResolvedCombatFx.Presented==presented,"next-card-before-contact fixture advanced before the next input");
     }
     PresentationCheck(errors,GameObject.Find("inputblock")==null&&GameObject.Find("inputblock2")==null,"presentation blocked card input");
     int inputFrame=Time.frameCount;g.PreferredTarget=0;
     CombatTimingProbe.Event("input",card.Def.Id);
     var timer=System.Diagnostics.Stopwatch.StartNew();
     ExecuteEvents.Execute(hand.Rt.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
     timer.Stop();CombatTimingProbe.Event("inputReturned",card.Def.Id);
     PresentationCheck(errors,g.Error==null,"command error: "+g.Error);
     PresentationCheck(errors,Time.frameCount==inputFrame,"input waited for a visual frame");
     PresentationCheck(errors,JToken.DeepEquals(JToken.FromObject(expected.Combat),JToken.FromObject(g.Rs.Combat)),"UI command differs from synchronous engine result: "+card.Def.Id);
     PresentationCheck(errors,!g.Rs.Combat.Player.Hand.Any(c=>c.Uid==card.Uid),"accepted card remained in hand");
     actions.Add(new JObject{{"card",card.Def.Id},{"frame",inputFrame-startFrame},{"inputToReturnMs",timer.Elapsed.TotalMilliseconds},{"pendingImpacts",Presenter.PendingImpactCount},{"authoritativeHp",new JArray(g.Rs.Combat.Enemies.Select(e=>e.Hp))},{"expectedReceipts",added.Count}});
     if(showcase&&n>0&&(name=="inputDuringPause"||name=="blockDuringPause"))PresentationCheck(errors,Stage.ImpactPauseRemaining("player")==0f,"new input inherited previous actor pause");
     checkHp();
    }
    var resolved=g.Rs.Combat.EventLog.Skip(from).Where(ResolvedCombatFx.IsResolved).ToList();
    row["expectedEvents"]=JArray.FromObject(resolved);row["expectedReceipts"]=resolved.Count;row["finalEngine"]=JToken.FromObject(g.Rs.Combat);
    PresentationCheck(errors,resolved.Count>0&&resolved.OfType<GameEvent_DamageDealt>().Count(e=>e.Source=="player")==receipts.Count,"fixture lacks expected player damage receipts");
    row["expectedEnemyReceipts"]=receipts.Count;
    if(name=="thornedMultiHit")PresentationCheck(errors,receipts.Count==2&&resolved.OfType<GameEvent_ThornsReflected>().Count()==2,"thorn fixture must interleave two reflected receipts with the two direct hits");
    if(name=="guarded")PresentationCheck(errors,receipts.All(r=>r.Outcome=="guard"&&r.Amount==0),"guard fixture did not resolve guarded zero HP damage");
    if(name=="zero")PresentationCheck(errors,receipts.All(r=>r.Outcome=="nullified"&&r.Amount==0),"zero fixture did not resolve nullified damage");
    if(name=="green_earth_roar"||name=="aoeMultiHit")PresentationCheck(errors,receipts.Count(r=>r.Card=="green_earth_roar")==8,"AOE fixture must resolve two waves across four targets");
    if(terminal)PresentationCheck(errors,g.Rs.Combat.Enemies[0].Hp<0&&g.Rs.Phase!=RunPhases.Combat,"overkill did not resolve a terminal negative raw HP");
    if(name=="missingTarget") {
     var gone=g.Anchor("enemy0");TempoRequire(gone!=null,"missing-target fixture had no target to remove");
     g.Battle.KillEnemy(g,0,false);g.RegisterAnchor("enemy0",null);Destroy(gone.gameObject);missing=true;
     CombatTimingProbe.Event("targetRemoved","enemy0");
    }
    if(name=="sameStateRebuild") {
     int pending=Presenter.PendingImpactCount;var same=g.Rs.Combat;g.Rebuild();
     PresentationCheck(errors,ReferenceEquals(same,g.Rs.Combat)&&Presenter.PendingImpactCount==pending,"same-state rebuild changed state or cancelled pending contacts");
     checkHp();CombatTimingProbe.Event("rebuild","beforeContact");
    }
    if(lifecycle&&!duringLifecycle) {
     PresentationCheck(errors,Presenter.PendingImpactCount>0,"leave/reset needs queued contacts");
     cancelledReceipts=resolved.Count-(ResolvedCombatFx.Presented-presented);presentedAtCancel=ResolvedCombatFx.Presented;cancelled=true;
     if(name=="leaveCombat")g.BackToSetup();
     else {Presenter.Reset();Presenter.MarkSeen(g.Rs.Combat);g.Rebuild();}
     row["pendingImmediatelyAfterLifecycle"]=Presenter.PendingImpactCount;
     row["visualsImmediatelyAfterLifecycle"]=Presenter.PendingVisualCount;
     row["fxImmediatelyAfterLifecycle"]=WorldBattleFx.ActiveCount;
     // Baseline preserves its preexisting leave behavior; stale board callbacks must still never present.
     if(showcase||name=="reset")PresentationCheck(errors,Presenter.PendingImpactCount==0&&Presenter.PendingVisualCount==0&&WorldBattleFx.ActiveCount==0,"leave/reset retained active presentation work");
     CombatTimingProbe.Event(name,"beforeContact");
    }
    int frames=0,budget=TransitionBudgetFrames();row["drainBudgetFrames"]=budget;
    do {
     yield return new WaitForEndOfFrame();CombatTimingProbe.Sample();
     if((duringLifecycle||name=="missingDuringPause")&&!duringAction&&ResolvedCombatFx.Presented>presented) {
      duringAction=true;
      PresentationCheck(errors,Presenter.PendingImpactCount>0,"during-pause lifecycle/removal needs a later queued hit");
      if(showcase)PresentationCheck(errors,Stage.ActiveImpactPauseCount>0,"lifecycle fixture missed active local pause");
      if(duringLifecycle) {
       cancelledReceipts=resolved.Count-(ResolvedCombatFx.Presented-presented);presentedAtCancel=ResolvedCombatFx.Presented;cancelled=true;
       if(name=="leaveDuringPause")g.BackToSetup();else {Presenter.Reset();Presenter.MarkSeen(g.Rs.Combat);g.Rebuild();}
       PresentationCheck(errors,Stage.ActiveImpactPauseCount==0,"lifecycle left a local pause behind");
      } else {
       var gone=g.Anchor("enemy0");g.Battle.KillEnemy(g,0,false);g.RegisterAnchor("enemy0",null);if(gone!=null)Destroy(gone.gameObject);missing=true;
      }
      CombatTimingProbe.Event(name,"duringContact");
     }
     foreach(string unit in new[]{"player","enemy0"}) {
      var pose=Stage.ImpactPose(unit);float remain=Stage.ImpactPauseRemaining(unit);
      if(pose!=null&&remain>0f) {
       var current=JToken.FromObject(pose);
       if(pausedPoses.TryGetValue(unit,out var prior)&&JToken.DeepEquals(prior["unit"],current["unit"])&&(int)prior["motion"]==(int)current["motion"]&&(int)prior["recoil"]==(int)current["recoil"]) {
        PresentationCheck(errors,JToken.DeepEquals(prior,current),"character pose advanced inside local hitstop: "+unit);poseHoldFrames++;
        if(unit=="player")playerHoldFrames++;else targetHoldFrames++;
       }
       pausedPoses[unit]=current;
      } else pausedPoses.Remove(unit);
     }
     if(name=="sameStateRebuild"&&!rebuiltAfterContact&&ResolvedCombatFx.Presented-presented==1&&Presenter.PendingImpactCount>0) {
      int pending=Presenter.PendingImpactCount;g.Rebuild();rebuiltAfterContact=true;
      PresentationCheck(errors,Presenter.PendingImpactCount==pending,"mid-chain rebuild cancelled remaining contacts");CombatTimingProbe.Event("rebuild","betweenContacts");
     }
     if(showcase&&name=="blockDuringPause")PresentationCheck(errors,Stage.ImpactPauseRemaining("player")==0f,"old AOE contact stopped the newer block animation");
     checkHp();
     // Terminal commands now retain the old board under the transition gate in both modes.
     if(!terminal) {
      PresentationCheck(errors,Mathf.Approximately(Time.timeScale,1f),"non-terminal global hitstop changed timeScale");
      PresentationCheck(errors,!g.CombatPresentationPending&&GameObject.Find("inputblock")==null&&GameObject.Find("inputblock2")==null,"ordinary presentation added an input blocker");
     } else if(!PresentationIdle())PresentationCheck(errors,g.CombatPresentationPending,"terminal presentation lost its input gate");
     var displayed=new JArray();foreach(int i in Enumerable.Range(0,shownHp.Length)){var bar=PresentationBar(g,i);displayed.Add(bar!=null?(JToken)bar.Value:JValue.CreateNull());}
     samples.Add(new JObject{{"frame",Time.frameCount-startFrame},{"seconds",Time.time-started},{"timeScale",Time.timeScale},{"received",ResolvedCombatFx.Received-received},{"presented",ResolvedCombatFx.Presented-presented},{"pendingImpacts",Presenter.PendingImpactCount},{"pendingVisuals",Presenter.PendingVisualCount},{"activeFx",WorldBattleFx.ActiveCount},{"hpTargets",displayed},{"activeLocalPauses",Stage.ActiveImpactPauseCount},{"actorPauseMs",Stage.ImpactPauseRemaining("player")*1000f},{"targetPauseMs",Stage.ImpactPauseRemaining("enemy0")*1000f}});
     if(capture&&frames<=60&&(frames%4==0||frames==10||frames==14||frames==18)) {
      string filename="presentation-"+mode+"-"+name+"-"+frames.ToString("D3")+".png";
      var shot=ScreenCapture.CaptureScreenshotAsTexture();
      try {File.WriteAllBytes(Path.Combine(_dir,filename),shot.EncodeToPNG());}finally {Destroy(shot);}
      screenshots.Add(filename);
     }
     yield return null;frames++;
    }while(frames<budget&&(frames<90||!PresentationIdle()));
    if(name=="sameStateRebuild")PresentationCheck(errors,rebuiltAfterContact,"did not exercise rebuild between two contacts");
    if(name=="missingTarget"||name=="missingDuringPause") {
     PresentationCheck(errors,g.Anchor("enemy0")==null,"removed target anchor reappeared");
     for(int i=1;i<initialHp.Length;i++)PresentationCheck(errors,g.Rs.Combat.Enemies[i].Hp==initialHp[i],"missing target redirected damage to enemy"+i);
    }
    if(lifecycle)PresentationCheck(errors,ResolvedCombatFx.Presented==presentedAtCancel,"cancelled receipt appeared after leave/reset");
    if(name=="reset"||name=="resetDuringPause")for(int i=0;i<initialHp.Length;i++) {
     var bar=PresentationBar(g,i);
     PresentationCheck(errors,bar!=null&&bar.Value==Math.Max(0,g.Rs.Combat.Enemies[i].Hp),"reset left HP debt on enemy"+i);
    }
    PresentationCheck(errors,PresentationIdle(),"presentation did not drain within timeline-derived budget");
    PresentationCheck(errors,Mathf.Approximately(Time.timeScale,1f)&&!Tween.FinishSlowActive,"completed presentation left global slow behind");
    PresentationCheck(errors,ResolvedCombatFx.Received-received==resolved.Count,"resolved receipt capture count mismatch");
    PresentationCheck(errors,ResolvedCombatFx.Presented-presented+cancelledReceipts==resolved.Count,"receipt conservation failed: presented + explicitly cancelled != resolved");
    PresentationCheck(errors,ResolvedCombatFx.DuplicateSuppressed==duplicates,"duplicate receipt was suppressed instead of being scheduled once");
    PresentationCheck(errors,ResolvedCombatFx.WorldPresented-world+ResolvedCombatFx.FallbackPresented-fallback==ResolvedCombatFx.Presented-presented,"visible receipt surface accounting mismatch");
    if(showcase&&!lifecycle)PresentationCheck(errors,receipts.All(r=>r.Shown),"showcase receipt callback missing");
    if(!lifecycle&&!terminal)for(int i=0;i<initialHp.Length;i++) {
     if(missing&&i==0)continue;var bar=PresentationBar(g,i);
     PresentationCheck(errors,bar!=null&&bar.Value==Math.Max(0,g.Rs.Combat.Enemies[i].Hp),"final HP target stale for enemy"+i);
    }
    if(duringLifecycle||name=="missingDuringPause")PresentationCheck(errors,duringAction,"during-pause action was not exercised");
    if(showcase&&PresentationProfiles.Contains(name)) {
     PresentationCheck(errors,playerHoldFrames>0,"no stable paused player pose was observed");
     PresentationCheck(errors,targetHoldFrames>0,"no stable paused target pose was observed");
    }
    row["poseHoldFrames"]=poseHoldFrames;row["playerHoldFrames"]=playerHoldFrames;row["targetHoldFrames"]=targetHoldFrames;row["duringPauseAction"]=duringAction;row["activeLocalPauses"]=Stage.ActiveImpactPauseCount;
    row["received"]=ResolvedCombatFx.Received-received;row["presented"]=ResolvedCombatFx.Presented-presented;row["cancelledByLifecycle"]=cancelledReceipts;
    row["duplicateSuppressed"]=ResolvedCombatFx.DuplicateSuppressed-duplicates;row["drainFrames"]=frames;row["pendingImpacts"]=Presenter.PendingImpactCount;row["pendingVisuals"]=Presenter.PendingVisualCount;row["activeFx"]=WorldBattleFx.ActiveCount;
    row["showcaseHpTimingAsserted"]=showcase&&!lifecycle;row["sameStateMidContactRebuild"]=rebuiltAfterContact;
   } finally {
    if(showcase)Presenter.EnemyImpactPresentedForReview-=onReceipt;
    if(CombatTimingProbe.Enabled)CombatTimingProbe.Save(Path.Combine(_dir,"presentation-"+mode+"-"+name+"-timing.json"));
   }
  }
 }
}

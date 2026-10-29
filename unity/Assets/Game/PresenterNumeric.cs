using System;
using System.Collections;
using System.Collections.Generic;
using DeckRogue.Engine.Generated;
using UnityEngine;

namespace DeckRogue.Game {
 public static partial class Presenter {
  static readonly CombatPresentationLedger Numeric=new CombatPresentationLedger();
  static readonly CombatActionQueue Actions=new CombatActionQueue();
  static readonly List<CombatPresentationLedger.Batch> NumericBatches=new List<CombatPresentationLedger.Batch>();
  static BattleView numericBoard; static int numericEpoch; static bool pumping;
  static readonly Dictionary<int,float> actionBudgets=new Dictionary<int,float>();
  public static int PendingNumericCount=>Numeric.PendingCount;
  public static int NumericHudFailures {get;private set;}
  public static int QueuedActionCount=>Actions.QueuedCount;
  public static double MaximumActionQueueWait=>Actions.MaxWait;
  // Conservative watchdog allowance only. It never schedules contact or sums target residue.
  public static float PresentationWatchdogBudget {get {float n=5f;foreach(var v in actionBudgets.Values)n+=v;return Mathf.Max(8f,n*2f);}}
  static Presenter() {
   Numeric.Audit=(reason,index)=>{if(CombatTimingProbe.Enabled)CombatTimingProbe.Event("numericAudit",index+":"+reason);};
   Actions.Audit=(token,cause)=>{if(CombatTimingProbe.Enabled)CombatTimingProbe.Event("actionQueue",cause+":"+(token?.Id??0)+":wait="+(token?.Wait??0));};
  }
  static void ClearNumericPresentation() {
   ++numericEpoch;pumping=false;EnemySources.Clear();EnemySourceVisuals.Clear();EnemySourceHadVisual.Clear();Actions.Cancel();Numeric.Clear();NumericBatches.Clear();actionBudgets.Clear();numericBoard=null;
  }
  static void PrepareNumeric(GameRoot g,GameState before,GameState after,RunCommand ownerCommand=null) {
   if(g?.Battle==null || before==null || after==null)return;
   if(numericBoard!=g.Battle || (Numeric.Active && !ReferenceEquals(Origin(Numeric.Latest),Origin(after)))) {
    ClearNumericPresentation();numericBoard=g.Battle;
   }
   foreach(var existing in NumericBatches)if(ReferenceEquals(existing.After,after))return;
   if(Numeric.Active && !ReferenceEquals(Numeric.Latest,before)) {
    // A replaced/trimmed command lineage cannot own old callbacks. Rebase only
    // the display reservation to the explicitly supplied current before snapshot.
    Debug.LogWarning("[Presentation] numeric lineage replaced; cancelling old owner");
    Reset();numericBoard=g.Battle;_seen=before.EventLog.Count;_seenCombat=before;
   }
   var batch=Numeric.Register(before,after,ownerCommand);
   if(batch!=null)NumericBatches.Add(batch);
  }
  static CombatPresentationLedger.Batch NumericBatch(GameState state) {
   foreach(var b in NumericBatches)if(ReferenceEquals(b.After,state))return b;
   return null;
  }
  public static bool OwnsNumeric(GameState state)=>Numeric.Active && ReferenceEquals(Numeric.Latest,state);
  public static bool KeepPresentationUnit(GameRoot g,int target)=>g?.Battle==numericBoard && Numeric.TryGet(g?.Rs?.Combat,target,out _);
  // UI-only copy. Rules, targeting, alive checks and saved state keep the authoritative object.
  public static GameState NumericDisplayState(GameState state) {
   if(!OwnsNumeric(state))return state;
   var player=state.Player;
   if(Numeric.TryGet(state,-1,out var p))player=player with {Hp=CombatPresentationLedger.Display(p.Hp),MaxHp=CombatPresentationLedger.Display(p.MaxHp),Block=CombatPresentationLedger.Display(p.Block),IceBlock=CombatPresentationLedger.Display(p.IceBlock)};
   var enemies=new List<EnemyState>(state.Enemies);
   for(int i=0;i<enemies.Count;i++)if(Numeric.TryGet(state,i,out var e))enemies[i]=enemies[i] with {Hp=CombatPresentationLedger.Display(e.Hp),MaxHp=CombatPresentationLedger.Display(e.MaxHp),Block=CombatPresentationLedger.Display(e.Block)};
   return state with {Player=player,Enemies=enemies};
  }
  static void DisplayNumeric(GameRoot g,int target) {
   if(g?.Battle!=numericBoard || !Numeric.TryGet(g.Rs?.Combat,target,out var value))return;
   try {numericBoard.DisplayNumericVitals(target,value);}
   catch(Exception e){NumericHudFailures++;Debug.LogWarning("[Presentation] numeric HUD unavailable target="+target+": "+e.Message);}
  }
  static void FinishNumeric(GameRoot g,CombatPresentationLedger.Batch batch,int epoch,int actionId) {
   if(epoch!=numericEpoch)return;
   actionBudgets.Remove(actionId);
   if(!Numeric.Finish(batch) || g?.Battle!=numericBoard || !ReferenceEquals(g.Rs?.Combat,Numeric.Latest))return;
   using(Tween.BeginPresentation()) {
    DisplayNumeric(g,-1);
    for(int i=0;i<Numeric.Latest.Enemies.Count;i++){DisplayNumeric(g,i);numericBoard.ReleasePresentedDeath(g,i);}
   }
   NumericBatches.RemoveAll(b=>b.Finished);
  }
  static void QueuePresentation(GameRoot g,GameState combat,bool preserveBoard) {
   if(g==null || combat==null)return;
   var previous=_seenCombat as GameState;
   if(ReferenceEquals(previous,combat))return; // No-op/rejected command: never replay a reserved batch.
   int from=Math.Min(_seen,combat.EventLog.Count);
   if(previous!=null && !ReferenceEquals(Origin(previous),Origin(combat)))from=0;
   var batch=NumericBatch(combat);if(batch!=null)from=batch.From;
   bool enemyPhase=HasEnemyPhase(combat);int epoch=numericEpoch;var board=g.Battle;
   var captured=new Dictionary<int,ResolvedCombatFx.Ticket>();
   Canvas.ForceUpdateCanvases();
   ReserveEnemyContacts(g,previous,combat,from,board);
   for(int i=from;i<combat.EventLog.Count;i++)
    if(ResolvedCombatFx.IsResolved(combat.EventLog[i])) {
     var ticket=ResolvedCombatFx.Capture(g,combat.EventLog[i]);captured[i]=ticket;
     ticket.LightBoltBinding=Hd2dLightBoltBinding.Capture(ticket,batch,Numeric.Find(i,combat.EventLog[i]),()=>Numeric.Generation);
     ticket.TripleHornBinding=TripleHornBinding.Capture(ticket,batch,Numeric.Find(i,combat.EventLog[i]),()=>Numeric.Generation,()=>g.LeaderId=="leader_green");
     ticket.LightBurstBinding=LightBurstBinding.Capture(ticket,batch,Numeric.Find(i,combat.EventLog[i]),()=>Numeric.Generation,()=>g.LeaderId=="leader_white");
     ticket.ColdSweepBinding=Hd2dColdSweepBinding.Capture(batch,Numeric.Find(i,combat.EventLog[i]),()=>Numeric.Generation);
     ticket.ImpSparkBinding=ImpSparkBinding.Capture(batch,Numeric.Find(i,combat.EventLog[i]),()=>Numeric.Generation);
     ticket.CanopyBinding=CanopyBinding.Capture(batch,Numeric.Find(i,combat.EventLog[i]),()=>Numeric.Generation);
     ticket.MonkSquireHealBinding=MonkSquireHealBinding.Capture(batch,Numeric.Find(i,combat.EventLog[i]),()=>Numeric.Generation);
     ticket.MonkSelfHealBinding=MonkSelfHealBinding.Capture(batch,Numeric.Find(i,combat.EventLog[i]),()=>Numeric.Generation);
     ticket.WraithCurseBinding=WraithCurseBinding.Capture(batch,Numeric.Find(i,combat.EventLog[i]),()=>Numeric.Generation);
     ticket.CrossbowAimedBinding=CrossbowAimedBinding.Capture(batch,Numeric.Find(i,combat.EventLog[i]),()=>Numeric.Generation);
    }
   // Nested play-from-exhaust also emits CardPlayed. Give every such segment
   // actor ownership through the same fence, without splitting gameplay or events.
   var starts=new List<int>{from};
   for(int i=from;i<combat.EventLog.Count;i++)
    if((combat.EventLog[i] is GameEvent_CardPlayed || combat.EventLog[i] is GameEvent_TurnEnded) && i>starts[starts.Count-1])starts.Add(i);
   var visualPlan=new CommandVisualPlan{From=from};int remaining=starts.Count;
   // Reserve now, not when a delayed actor acquires its pose. New input cannot replay old events.
   _seen=combat.EventLog.Count;_seenCombat=combat;
   for(int part=0;part<starts.Count;part++) {
    int start=starts[part],end=part+1<starts.Count?starts[part+1]:combat.EventLog.Count;
    var contacts=new List<int>();
    for(int i=start;i<end;i++)if(CombatPresentationLedger.IsNumeric(combat.EventLog[i]) || IsActorCue(combat.EventLog[i]))contacts.Add(i);
    var token=Actions.Enqueue(Time.time,contacts,action=>{
     if(epoch!=numericEpoch || board!=g.Battle)return;
     using(Tween.BeginPresentation(()=>{
      if(epoch!=numericEpoch)return;
      actionBudgets.Remove(action.Id);
      if(--remaining==0)FinishNumeric(g,batch,epoch,action.Id);
     })) {
      try {PlayTracked(g,combat,preserveBoard,start,previous,enemyPhase,action,captured,epoch,board,end,visualPlan);}
      catch(Exception e){AbortNumeric(g,"action_launch_failed:"+e.Message);}
     }
    });
    float extra=0f;for(int j=start;j<end;j++){var r=Numeric.Find(j,combat.EventLog[j]);if(r?.Projection?.Individual==true && r.ProjectionReason==null)extra+=r.Projection.Span;}
    actionBudgets[token.Id]=Mathf.Max(1f,(end-start)*.95f+2f+extra);
   }
   Actions.Pump(Time.time,Time.frameCount,Stage.ImpactPauseRemaining("player")>0f);
   if(!pumping && (Actions.QueuedCount>0 || Actions.PendingContacts>0)) {
    pumping=true;Tween.I.StartCoroutine(PumpActions(epoch));
   }
  }
  static IEnumerator PumpActions(int epoch) {
   try {
    while(epoch==numericEpoch && (Actions.QueuedCount>0 || Actions.PendingContacts>0)) {
     yield return null;
     if(epoch==numericEpoch)Actions.Pump(Time.time,Time.frameCount,Stage.ImpactPauseRemaining("player")>0f);
    }
   } finally {if(epoch==numericEpoch)pumping=false;}
  }
  static bool ContactCurrent(GameRoot g,ReactionCtx ctx) {
   if(ctx==null || ctx.Action==null)return true;
   var state=g?.Rs?.Combat;
   return ctx.NumericEpoch==numericEpoch && !ctx.Action.Cancelled && g.Battle==ctx.CapturedBoard && state!=null
    && ctx.EventIndex>=0 && ctx.EventIndex<state.EventLog.Count && ReferenceEquals(state.EventLog[ctx.EventIndex],ctx.ContactEvent);
  }
  static void AbortNumeric(GameRoot g,string reason) {
   Debug.LogWarning("[Presentation] "+reason+"; cancel and reconcile latest board");
   var latest=g?.Rs?.Combat;Reset();
   if(latest!=null && g?.Battle!=null){MarkSeen(latest);g.Battle.ReconcileComparisonCheckpoint(g,latest);g.Rebuild();}
  }
  static void RejectStaleContact(GameRoot g,ReactionCtx ctx) {
   // Cancelled generations are inert. Only a still-owned but broken lineage is
   // an error; it is explicitly reconciled rather than stranded until timeout.
   if(ctx?.Action!=null && ctx.NumericEpoch==numericEpoch && !ctx.Action.Cancelled)AbortNumeric(g,"contact_owner_invalid");
  }
  static void RunContact(GameRoot g,ReactionCtx ctx,Action present) {
   if(ctx?.Action==null){present?.Invoke();return;}
   if(ctx.ContactQueued || ctx.ContactDone)return;
   if(!ContactCurrent(g,ctx)){RejectStaleContact(g,ctx);return;}
   ctx.ContactQueued=true;
   Tween.RunPresentation(ContactCo(g,ctx,present));
  }
  static void BindWaterPbfCandidate(GameRoot g,ReactionCtx ctx,System.Collections.Generic.IReadOnlyList<GameEvent> log,int from,int to,float landAt) {
   if(!WaterPbfFx.Enabled)return;
   var ticket=ctx?.Feedback;var receipt=ctx?.NumericReceipt;
   if(!CardVfxProfile.ShowcaseOn||ticket==null||!WaterPbfFx.Ready||ticket.SourceKey!="blue_strike"||ticket.SourceKind!="card"||!ticket.ExactSource||ticket.SourceUnit!="player"||ticket.Outcome!="hit"||ticket.Amount<=0||ticket.Index!=0||ticket.Total!=1)return;
   if(from<0||from>=to||!(log[from] is GameEvent_CardPlayed played)||played.CardId!="blue_strike")return;
   int count=0;for(int j=from;j<to;j++)if(log[j] is GameEvent_DamageDealt d&&d.Source=="player"&&d.PresentationSource?.Kind=="card"&&d.PresentationSource.DefinitionId=="blue_strike")count++;
   if(count!=1||!(ctx.ContactEvent is GameEvent_DamageDealt hit)||hit.PresentationSource?.Kind!="card"||hit.PresentationSource.DefinitionId!="blue_strike"||!hit.EnemyIndex.HasValue||hit.HpLoss<=0||receipt==null||receipt.Values==null||receipt.Reason!=null||!Numeric.Current(receipt)||receipt.Projection!=null)return;
   int owner=numericEpoch;var board=g.Battle;var ev=ctx.ContactEvent;int index=ctx.EventIndex;
   ticket.PbfBinding=new WaterPbfBinding{Clock=new WaterPbfClock(Time.time,landAt),
    BeforeContact=()=>ContactCurrent(g,ctx)&&Numeric.Current(receipt)&&receipt.Reason==null,
    AfterCommit=()=>ContactCurrent(g,ctx)&&receipt.Committed&&receipt.Exact&&receipt.Reason==null,
    ResidueCurrent=()=>owner==numericEpoch&&board==g.Battle&&g.Rs?.Combat!=null&&index<g.Rs.Combat.EventLog.Count&&ReferenceEquals(g.Rs.Combat.EventLog[index],ev)};
  }
  static void BeginAuthoredPreview(GameRoot g,ReactionCtx ctx,float contactAt) {
   if(ctx.Feedback?.FixedSmallContact==true){ctx.Feedback.DeliveryPreparation="noncard_fixed_small_contact";return;}
   if(PriorEnemyProjection(ctx)){ctx.Feedback.RenderFallbackReason="contact_only_prior_enemy_projection";return;}
   if(ctx.Feedback?.AttackFamily!="vine_slash" && !PlayerContactFx.IsFamily(ctx.Feedback?.AttackFamily) && !PhysicalSlashFx.Eligible(ctx.Feedback) && !WaterContinuousFx.Eligible(ctx.Feedback) && !FireContinuousFx.Eligible(ctx.Feedback) && !DarkDrainFx.Eligible(ctx.Feedback) && !LightRayFx.Eligible(ctx.Feedback) && !IceCrystalFx.Eligible(ctx.Feedback) && !EarthStompFx.Eligible(ctx.Feedback) && !LightningDischargeFx.Eligible(ctx.Feedback)){WorldBattleFx.BeginAuthoredAttack(ctx.Feedback,contactAt);return;}
   if(PlayerContactFx.IsFamily(ctx.Feedback?.AttackFamily)||PhysicalSlashFx.Eligible(ctx.Feedback)||WaterContinuousFx.Eligible(ctx.Feedback)||FireContinuousFx.Eligible(ctx.Feedback)||DarkDrainFx.Eligible(ctx.Feedback)||LightRayFx.Eligible(ctx.Feedback)||IceCrystalFx.Eligible(ctx.Feedback)||EarthStompFx.Eligible(ctx.Feedback)||LightningDischargeFx.Eligible(ctx.Feedback))ctx.Feedback.DeliveryPreparation=ctx.NumericReceipt!=null&&!Numeric.Ready(ctx.NumericReceipt)?"waiting_numeric_dependency":"ready";
   Tween.RunPresentation(VinePreviewCo(g,ctx,contactAt));
  }
  static IEnumerator VinePreviewCo(GameRoot g,ReactionCtx ctx,float contactAt) {
   while(ContactCurrent(g,ctx) && Time.time<contactAt && ctx.NumericReceipt!=null && Numeric.Current(ctx.NumericReceipt) && !Numeric.Ready(ctx.NumericReceipt))yield return null;
   if(!ContactCurrent(g,ctx) || ctx.Feedback==null || ctx.Feedback.Shown)yield break;
   if(Time.time>=contactAt || (ctx.NumericReceipt!=null && !Numeric.Ready(ctx.NumericReceipt))){ctx.Feedback.RenderFallbackReason=ctx.Feedback.AttackFamily=="vine_slash"?"vine_contact_only_preparation_unready":PhysicalSlashFx.Eligible(ctx.Feedback)?"slash_tier_contact_only_preparation_unready":WaterContinuousFx.Eligible(ctx.Feedback)?"water_flow_contact_only_preparation_unready":FireContinuousFx.Eligible(ctx.Feedback)?"fire_flow_contact_only_preparation_unready":LightningDischargeFx.Eligible(ctx.Feedback)?"lightning_discharge_contact_only_preparation_unready":EarthStompFx.Eligible(ctx.Feedback)?"ground_contact_only_preparation_unready":IceCrystalFx.Eligible(ctx.Feedback)?"ice_crystal_contact_only_preparation_unready":LightRayFx.Eligible(ctx.Feedback)?"light_ray_contact_only_preparation_unready":DarkDrainFx.Eligible(ctx.Feedback)?"dark_drain_contact_only_preparation_unready":"delivery_contact_only_preparation_unready";if(PlayerContactFx.IsFamily(ctx.Feedback.AttackFamily)||PhysicalSlashFx.Eligible(ctx.Feedback)||WaterContinuousFx.Eligible(ctx.Feedback)||FireContinuousFx.Eligible(ctx.Feedback)||DarkDrainFx.Eligible(ctx.Feedback)||LightRayFx.Eligible(ctx.Feedback)||IceCrystalFx.Eligible(ctx.Feedback)||EarthStompFx.Eligible(ctx.Feedback)||LightningDischargeFx.Eligible(ctx.Feedback))ctx.Feedback.DeliveryPreparation="contact_only_preparation_unready";yield break;}
   WorldBattleFx.BeginAuthoredAttack(ctx.Feedback,contactAt);
  }
  static IEnumerator ContactCo(GameRoot g,ReactionCtx ctx,Action present) {
   while(ContactCurrent(g,ctx) && (PriorEnemyProjection(ctx) || (ctx.NumericReceipt!=null && Numeric.Current(ctx.NumericReceipt) && !Numeric.Ready(ctx.NumericReceipt))))yield return null;
   if(!ContactCurrent(g,ctx)){RejectStaleContact(g,ctx);yield break;}
   bool complete=false;
   try {
    ctx.ContactEntered=true;
    if(IndividualEnemyContact(ctx) && !EnemyProjectionVisualCurrent(ctx))ctx.NumericReceipt.ProjectionReason="enemy_contact_visual_lease_replaced";
    bool projected=IndividualEnemyContact(ctx) && Numeric.TryBeginProjection(ctx.NumericReceipt,EnemyProof(g,ctx));
    if(projected) {
     var receipt=ctx.NumericReceipt;var plan=receipt.Projection;float firstActual=Time.time;int lastContactFrame=-1;
     for(int ordinal=0;ordinal<plan.Count;ordinal++) {
      if(ordinal>0) {
       float next=firstActual+ordinal*EnemyAttackContactPlan.ContactSpacing;
       while(ContactCurrent(g,ctx) && (Time.time<next || Time.frameCount<=lastContactFrame))yield return null;
       // A long frame stall retimes only the remaining presentation. Do not burst
       // missed contacts together or accumulate normal frame rounding each hit.
       if(Time.time-next>EnemyAttackContactPlan.ContactSpacing) {
        firstActual+=Time.time-next;
        if(CombatTimingProbe.Enabled)CombatTimingProbe.Event("enemyContributionClockShift",ctx.EventIndex+":"+ordinal+":"+(Time.time-next));
       }
      }
      if(!ContactCurrent(g,ctx)){ResolvedCombatFx.CancelEnemyContributions(ctx.Feedback);RejectStaleContact(g,ctx);yield break;}
      if(!EnemyProjectionVisualCurrent(ctx) || !Numeric.TryGet(g.Rs?.Combat,-1,out var before) || !Numeric.CommitContribution(receipt,ordinal,EnemyProof(g,ctx))) {
       ResolvedCombatFx.CancelEnemyContributions(ctx.Feedback);AbortNumeric(g,"enemy_contribution_continuity_failed");yield break;
      }
      lastContactFrame=Time.frameCount;var row=plan[ordinal];LandIncoming(g,ctx,row.AmountContribution);DisplayNumeric(g,-1);
      if(CombatTimingProbe.Enabled)CombatTimingProbe.Event("enemyContributionCommit",ctx.EventIndex+":"+ordinal);
      try {
       var child=ResolvedCombatFx.CreateEnemyContribution(ctx.Feedback,row,ordinal,plan.Count,before.Hp,before.MaxHp);
       PresentEnemyContribution(g,ctx,row,child);
      } catch(Exception e) {Debug.LogWarning("[Presentation] enemy contribution surface unavailable: "+e.Message);}
      if(CombatTimingProbe.Enabled)CombatTimingProbe.Event("enemyContributionPresented",ctx.EventIndex+":"+ordinal);
     }
     if(!ResolvedCombatFx.CompleteEnemyContributions(ctx.Feedback)){AbortNumeric(g,"enemy_contribution_render_ack_failed");yield break;}
     if(CombatTimingProbe.Enabled)CombatTimingProbe.Event("contactCommit",ctx.EventIndex.ToString());
    } else {
     if(ctx.NumericReceipt!=null && ctx.ContactEvent is GameEvent_DamageDealt incoming && incoming.Source=="enemy")LandIncoming(g,ctx,incoming.Amount);
     if(ctx.NumericReceipt!=null && Numeric.Commit(ctx.NumericReceipt))DisplayNumeric(g,ctx.NumericReceipt.Target);
     if(CombatTimingProbe.Enabled)CombatTimingProbe.Event(CombatPresentationLedger.IsNumeric(ctx.ContactEvent)?"contactCommit":"actorCueCommit",ctx.EventIndex.ToString());
     if(ctx.NumericReceipt?.Reason=="numeric_target_identity_unproved" || ctx.NumericReceipt?.Reason=="numeric_population_boundary_unproved")ResolvedCombatFx.InvalidateSystemTarget(ctx.Feedback,ctx.NumericReceipt.Reason);
     bool detachedSystem=ctx.Feedback?.SystemResponseCandidate!=null && ctx.Feedback.HasWorld && !Stage.ImpactTargetCurrent(ctx.Feedback.Target,ctx.Feedback.Impact);
     if(ctx.Feedback?.SystemTargetUnproved==true || detachedSystem)ResolvedCombatFx.Show(g,g.FxLayer,ctx.Feedback);else present?.Invoke();
     ShowEnemyAggregateCount(g,ctx);
    }
    if(_enemyPresentation.ContainsKey(ctx.EventIndex))EnemyImpactPresentedForReview?.Invoke(ctx.Feedback);
    complete=true;
   } finally {
    if(complete && ContactCurrent(g,ctx)) {
     ctx.ContactDone=true;
     EnemySources.Remove(ctx.EventIndex);EnemySourceVisuals.Remove(ctx.EventIndex);EnemySourceHadVisual.Remove(ctx.EventIndex);
     Actions.ContactComplete(ctx.Action,ctx.EventIndex,Time.time,Time.frameCount);
     _enemyPresentation.TryGetValue(ctx.EventIndex,out var legacy);LandEnemyPresentation(g,ctx.EventIndex,legacy);
     if(ctx.NumericReceipt?.Target>=0)ctx.CapturedBoard.ReleasePresentedDeath(g,ctx.NumericReceipt.Target);
     else if(legacy!=null)ctx.CapturedBoard.ReleasePresentedDeath(g,legacy.Enemy);
     if(CombatTimingProbe.Enabled)CombatTimingProbe.Event(CombatPresentationLedger.IsNumeric(ctx.ContactEvent)?"contactPresented":"actorCuePresented",ctx.EventIndex.ToString());
    } else if(ContactCurrent(g,ctx)){ResolvedCombatFx.CancelEnemyContributions(ctx.Feedback);AbortNumeric(g,"contact_coroutine_incomplete");}
   }
  }
  static void ShowSafely(GameRoot g,RectTransform fx,GameEvent ev,ReactionCtx ctx) {
   if(PriorEnemyProjection(ctx)){if(!ctx.DispatchDeferred){ctx.DispatchDeferred=true;Tween.RunPresentation(ShowAfterEnemyProjection(g,fx,ev,ctx));}return;}
   if(!ContactCurrent(g,ctx)){RejectStaleContact(g,ctx);return;}
   try {
    bool enemyLeaseLost=ctx?.EnemySource!=null && (EnemyProof(g,ctx)?.ContinuityProven!=true || !EnemyProjectionVisualCurrent(ctx));
    if(enemyLeaseLost && ev is GameEvent_EnemyActionExecuting) {
     RunContact(g,ctx,()=>{if(CombatTimingProbe.Enabled)CombatTimingProbe.Event("enemyActorCueSuppressed",ctx.EventIndex+":captured_source_unproved");});return;
    }
    if(IndividualEnemyContact(ctx) && (enemyLeaseLost || !EnemyProjectionVisualCurrent(ctx))) {
     AfterVisual(EnemyInnerDelay(g,ev,ctx),()=>RunContact(g,ctx,()=>ResolvedCombatFx.Show(g,fx,ctx.Feedback)));return;
    }
    if(ctx?.NumericReceipt?.Reason=="numeric_target_identity_unproved" || ctx?.NumericReceipt?.Reason=="numeric_population_boundary_unproved")ResolvedCombatFx.InvalidateSystemTarget(ctx.Feedback,ctx.NumericReceipt.Reason);
    if(ctx?.NumericReceipt?.Reason=="numeric_target_identity_unproved" || ctx?.NumericReceipt?.Reason=="numeric_population_boundary_unproved"
        || (ctx?.Feedback?.HasWorld==true && !Stage.ImpactTargetCurrent(ctx.Feedback.Target,ctx.Feedback.Impact)))
     RunContact(g,ctx,()=>ResolvedCombatFx.Show(g,fx,ctx.Feedback));
    else Show(g,fx,ev,false,ctx);
   }
   catch(Exception e) {
    Debug.LogWarning("[Presenter] "+e.Message);
    // An unavailable UI surface cannot strand the accepted action/receipt queue.
    RunContact(g,ctx,()=>ResolvedCombatFx.Show(g,fx,ctx.Feedback));
   }
  }
  static bool IsActorCue(GameEvent ev)=>ev is GameEvent_TurnEnded || ev is GameEvent_TurnStarted || ev is GameEvent_EnemyActionExecuting;
  static bool InnerContact(GameEvent ev)=>ev is GameEvent_EnemyHealed || ev is GameEvent_DamageDealt d && d.Source!="player";
  static int ActualAbsorption(GameEvent ev,int legacy) {
   var r=CombatPresentationLedger.Read(ev);
   return CombatPresentationLedger.Valid(ev,r,out _)?CombatPresentationLedger.Display(Math.Max(0L,(long)r.Before.Block-r.After.Block)+Math.Max(0L,(long)(r.Before.IceBlock??0)-(r.After.IceBlock??0))):legacy;
  }
 }
}

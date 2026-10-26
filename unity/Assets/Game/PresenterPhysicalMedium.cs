using System;
using System.Collections.Generic;
using DeckRogue.Engine.Generated;
using UnityEngine;
namespace DeckRogue.Game {
 public static partial class Presenter {
  static readonly AttackScenarioScript PhysicalMediumScript=AttackScenarioScript.Create("physical-medium-r2-candidate",
   new[]{new AttackScenarioPhaseInterval(AttackScenarioPhase.Anticipation,-.20,-.14),new AttackScenarioPhaseInterval(AttackScenarioPhase.Charge,-.14,-.13),new AttackScenarioPhaseInterval(AttackScenarioPhase.Release,-.13,.115),new AttackScenarioPhaseInterval(AttackScenarioPhase.Impact,0,.136),new AttackScenarioPhaseInterval(AttackScenarioPhase.Afterglow,.115,PhysicalMediumCandidateFx.VisibleEnd)},
   new[]{new AttackScenarioTrackInterval(AttackScenarioTrackKind.Main,-.14,.33),new AttackScenarioTrackInterval(AttackScenarioTrackKind.Secondary,-.14,.4075290594653786),new AttackScenarioTrackInterval(AttackScenarioTrackKind.Particles,-.025,PhysicalMediumCandidateFx.VisibleEnd),new AttackScenarioTrackInterval(AttackScenarioTrackKind.SceneLight,0,.136)},new[]{0.0},1,.13);
  // Strict first integration: a single proven card receipt, not a replacement of
  // arbitrary logs, reactive damage, multi-target groups, guard/nullify or other tiers.
  static bool TryPhysicalMediumScenario(GameRoot g,GameState combat,int from,int to,GameState previous,CombatActionQueue.ActionToken action,Dictionary<int,ResolvedCombatFx.Ticket> captured,int epoch,BattleView board,CommandVisualPlan plan){
   if(!PhysicalMediumCandidateFx.Enabled||!CardVfxProfile.ShowcaseOn||!Stage.CombatWorld||action==null||from<0||from>=to||!(combat.EventLog[from] is GameEvent_CardPlayed card)||card.CardId!="green_strike")return false;
   var log=combat.EventLog;int at=-1;GameEvent_EnemyDied death=null;
   for(int i=from+1;i<to;i++){
    if(log[i] is GameEvent_DamageDealt d&&d.Source=="player"&&d.PresentationSource?.Kind=="card"&&d.PresentationSource.DefinitionId=="green_strike"&&d.HpLoss>0&&d.EnemyIndex.HasValue){if(at>=0)return false;at=i;continue;}
    if(log[i] is GameEvent_EnemyDied died&&death==null&&at>=0&&died.EnemyIndex==((GameEvent_DamageDealt)log[at]).EnemyIndex){death=died;continue;}
    // These are all other records dispatched by PlayTracked. A complex command
    // remains wholly on the established path, preserving its original event order.
    if(log[i] is GameEvent_CardPlayed||CombatPresentationLedger.IsNumeric(log[i])||ResolvedCombatFx.IsResolved(log[i])||IsActorCue(log[i])||IsStatusEvent(log[i])||IsTrapEvent(log[i])||IsEnemyActEvent(log[i])||TableSound(log[i])!=null)return false;
   }
   if(at<0||!captured.TryGetValue(at,out var ticket)||ticket==null||!plan.Hits.TryGetValue(at,out var hitPlan)||hitPlan.Total!=1||hitPlan.Index!=0||!plan.Sources.TryGetValue(at,out var route)||route==null||!plan.Intensity.TryGetValue(at,out var intensity)||intensity?.Tier!="medium")return false;
   var receipt=Numeric.Find(at,log[at]);
   if(receipt?.Values==null||receipt.Reason!=null||receipt.Projection!=null||!Numeric.Current(receipt))return false;
   ticket.Style=hitPlan.Style;ticket.Color=HitColor(hitPlan.Style,hitPlan.Def);ticket.Index=0;ticket.Total=1;ticket.Element=CardVfxProfile.Element(hitPlan.Def,g.LeaderId);ticket.Showcase=hitPlan.Showcase;ticket.Intensity=intensity;ResolvedCombatFx.ApplyRoute(ticket,route);
   if(ticket.SourceKey!="green_strike"||ticket.SourceKind!="card"||ticket.SourceUnit!="player"||!ticket.ExactSource||ticket.AttackFamily!="slash"||ticket.Element!="physical"||ticket.Outcome!="hit"||ticket.FixedSmallContact||ticket.AuthoredPresentation=="small")return false;
   ScenarioNumericBinding binding;try{binding=ScenarioNumericBinding.ForReceipts(Numeric,new[]{receipt},false);}catch(ArgumentException){ticket.RenderFallbackReason="physical_medium_numeric_binding_unavailable";return false;}
   var ctx=ReactionContextFor(g,log,at,previous);ctx.Display=plan.Display;ctx.EventIndex=at;ctx.ContactEvent=log[at];ctx.CapturedState=combat;ctx.Action=action;ctx.NumericEpoch=epoch;ctx.CapturedBoard=board;ctx.NumericReceipt=receipt;ctx.Feedback=ticket;ctx.Hit=hitPlan;ctx.FinishingBlow=at==plan.Finishing;
   if(plan.Motion!=null)plan.Motion.TryGetValue(at,out ctx.Motion);
   Stage.BindImpactActor(ticket.Impact);
   var lease=PhysicalMediumCandidateFx.Reserve(ticket,()=>ContactCurrent(g,ctx)&&receipt.Committed&&receipt.Exact&&receipt.Reason==null);
   if(lease==null){ticket.RenderFallbackReason=PhysicalMediumCandidateFx.LastFailure;return false;}
   ticket.AttackPlayback=lease.Playback;ctx.ContactQueued=true;
   var cardCtx=new ReactionCtx{HitCount=1,EnemyIndex=((GameEvent_DamageDealt)log[at]).EnemyIndex.Value,Prev=previous,Showcase=hitPlan.Showcase,AuthoredCastReady=true,MotionDim=plan.MotionDim!=null&&plan.MotionDim.Contains(from)};
   bool settled=false;
   var host=new ScenarioPresentationDriver.Callbacks{
    IsCurrent=()=>ContactCurrent(g,ctx),ReadyToBegin=()=>Stage.ImpactPauseRemaining("player")<=0,
    Prepare=f=>true, // All shader/mesh/anchor admission completed before claiming this action.
    ActorCue=f=>{_playHint=1;Show(g,g.FxLayer,card,false,cardCtx);Stage.BindImpactActor(ticket.Impact);if(CombatTimingProbe.Enabled)CombatTimingProbe.Event("physicalMediumActor",at.ToString());},
    Hud=(f,r,i)=>{DisplayNumeric(g,r.Target);if(CombatTimingProbe.Enabled)CombatTimingProbe.Event("contactCommit",at.ToString());},
    Present=(f,r,i)=>{
     ctx.ContactEntered=true;
     try{Show(g,g.FxLayer,ctx.ContactEvent,false,ctx);}catch(Exception e){Debug.LogWarning("[Presentation] physical medium surface "+e.Message);ResolvedCombatFx.Show(g,g.FxLayer,ticket);}
     if(death!=null)AfterVisual(.12f,()=>{if(ContactCurrent(g,ctx))Show(g,g.FxLayer,death,false,new ReactionCtx{Display=plan.Display,Prev=previous});});
     ctx.ContactDone=true;Actions.ContactComplete(action,at,Time.time,Time.frameCount);board.ReleasePresentedDeath(g,r.Target);
     if(CombatTimingProbe.Enabled)CombatTimingProbe.Event("contactPresented",at.ToString());
    },
    Draw=f=>lease.Draw(f.Age),VisualsIdle=()=>lease.Idle,
    Completed=()=>{settled=true;lease.Retire();if(plan.Display!=null&&to==log.Count)FinishDisplayAfterEnemyProjection(g,plan.Display,to,epoch,board);},
    Cancelled=reason=>{lease.Retire();if(!settled&&reason!="presentation_cancelled"&&ContactCurrent(g,ctx)){settled=true;AbortNumeric(g,"physical_medium_"+reason);}}
   };
   try{ScenarioPresentationDriver.Run(PhysicalMediumScript,Time.time+.13,binding,host);}
   catch(Exception e){lease.Retire();ticket.AttackPlayback=null;ctx.ContactQueued=false;ticket.RenderFallbackReason="physical_medium_admission_failed:"+e.GetType().Name;return false;}
   LastImpactDelay=.20f;LastSequenceDuration=(float)(.20+PhysicalMediumCandidateFx.VisibleEnd);return true;
  }
 }
}

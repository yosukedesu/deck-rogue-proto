using System;
using System.Collections.Generic;
using DeckRogue.Engine.Generated;
using UnityEngine;
namespace DeckRogue.Game {
 public static partial class Presenter {
  // Keep the whole original dispatcher. In particular AetherDischarged is still
  // dispatched by TableSound before the real damage receipt, never swallowed or
  // re-emitted by a scenario host. The flag adds a render-only lease to one ticket.
  static void BindLightningR9Candidate(GameRoot g,ReactionCtx ctx,IReadOnlyList<GameEvent> log,int from,int to,float landAt,int commandFrom){
   if(from!=commandFrom||to!=log.Count)return; // Reject nested/split commands, including play-from-exhaust.
   if(!LightningR9CandidateFx.Enabled||!CardVfxProfile.ShowcaseOn||!Stage.CombatWorld||g?.LeaderId!="leader_izzet")return;
   var ticket=ctx?.Feedback;var receipt=ctx?.NumericReceipt;
   if(ticket==null||ctx.Prev?.Player?.Aether!=1||ticket.SourceKey!="blue_aether_burst"||ticket.SourceKind!="card"||!ticket.ExactSource||ticket.SourceUnit!="player"||string.IsNullOrEmpty(ticket.SourceUid)||ticket.AttackFamily!="lightning_discharge"||ticket.Element!="lightning"||ticket.Outcome!="hit"||ticket.Amount<=0||ticket.Index!=0||ticket.Total!=1||ticket.Intensity?.Tier!="medium"||ticket.FixedSmallContact||ticket.AuthoredPresentation=="small")return;
   if(from<0||from>=to||to>log.Count||!(log[from] is GameEvent_CardPlayed played)||played.CardId!="blue_aether_burst"||!(ctx.ContactEvent is GameEvent_DamageDealt hit)||hit.Source!="player"||hit.PresentationSource?.Kind!="card"||hit.PresentationSource.DefinitionId!="blue_aether_burst"||string.IsNullOrEmpty(hit.PresentationSource.Uid)||!hit.EnemyIndex.HasValue||hit.HpLoss<=0||receipt?.Values==null||receipt.Reason!=null||receipt.Projection!=null||!Numeric.Current(receipt))return;
   // A ratio-promoted four-point hit is named medium by the common policy, but
   // this vertical slice requires five through fourteen actual effective HP loss.
   long effective=Math.Min((long)hit.HpLoss,Math.Max(0L,(long)receipt.Values.Before.Hp));
   if(effective<5||effective>14||ticket.Intensity.Effective!=effective||ticket.SourceUid!=hit.PresentationSource.Uid||ticket.Target!="enemy"+hit.EnemyIndex.Value||receipt.Target!=hit.EnemyIndex.Value||receipt.Values.Target!="enemy"||receipt.Values.EnemyIndex!=hit.EnemyIndex)return;
   int damage=-1,discharge=-1;bool death=false;
   for(int i=from+1;i<to;i++){
    if(log[i] is GameEvent_AetherDischarged a){if(discharge>=0||damage>=0||a.Spent!=1)return;discharge=i;continue;}
    if(log[i] is GameEvent_DamageDealt d){if(damage>=0||discharge<0||!ReferenceEquals(d,hit))return;damage=i;continue;}
    if(log[i] is GameEvent_EnemyDied dead&&damage>=0&&!death&&dead.EnemyIndex==hit.EnemyIndex){death=true;continue;}
    // Any additional dispatched work, nested play, real hit or target remains
    // wholly on the established route. Cosmetic pieces never create receipts.
    if(log[i] is GameEvent_CardPlayed||CombatPresentationLedger.IsNumeric(log[i])||ResolvedCombatFx.IsResolved(log[i])||IsActorCue(log[i])||IsStatusEvent(log[i])||IsTrapEvent(log[i])||IsEnemyActEvent(log[i])||TableSound(log[i])!=null)return;
   }
   if(damage!=ctx.EventIndex||discharge<0||!LightningR9Clock.Finite(landAt)||landAt-Time.time+1e-7<LightningR9Clock.NativeLead)return;
   LightningR9Clock clock;try{clock=new LightningR9Clock(Time.time,landAt);}catch(ArgumentException){return;}
   int owner=numericEpoch;var board=g.Battle;var ev=ctx.ContactEvent;int index=ctx.EventIndex;
   ticket.LightningR9Binding=new LightningR9Binding{Clock=clock,
    BeforeContact=()=>ContactCurrent(g,ctx)&&g.LeaderId=="leader_izzet"&&Numeric.Current(receipt)&&receipt.Reason==null,
    AfterCommit=()=>ContactCurrent(g,ctx)&&receipt.Committed&&receipt.Exact&&receipt.Reason==null,
    ResidueCurrent=()=>owner==numericEpoch&&board==g.Battle&&g.Rs?.Combat!=null&&index<g.Rs.Combat.EventLog.Count&&ReferenceEquals(g.Rs.Combat.EventLog[index],ev)};
  }
 }
}

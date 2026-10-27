using System;
using System.Collections;
using System.Collections.Generic;
using DeckRogue.Engine.Generated;
using UnityEngine;

namespace DeckRogue.Game {
 public static partial class Presenter {
  static readonly Dictionary<int,EnemySourceLease> EnemySources=new Dictionary<int,EnemySourceLease>();
  static readonly Dictionary<int,Stage.ImpactCapture> EnemySourceVisuals=new Dictionary<int,Stage.ImpactCapture>();
  static readonly HashSet<int> EnemySourceHadVisual=new HashSet<int>();
  static void ReserveEnemyContacts(GameRoot g,GameState before,GameState after,int from,BattleView board) {
   if(before==null || after==null)return;
   for(int i=from;i<after.EventLog.Count;i++) {
    var ev=after.EventLog[i];int sourceIndex=EnemySourceLease.SourceIndex(ev);
    if(sourceIndex<0 || !(ev is GameEvent_DamageDealt || ev is GameEvent_EnemyActionExecuting))continue;
    var receipt=Numeric.Find(i,ev);
    var lease=EnemySourceLease.Capture(before,after,i,board,out var reason);
    EnemySources[i]=lease;
    var visual=Stage.CaptureImpact("enemy"+sourceIndex);EnemySourceVisuals[i]=visual;if(visual?.Target!=null)EnemySourceHadVisual.Add(i);
    if(receipt!=null)Numeric.ConfigureEnemyProjection(receipt,lease?.Proof(g.Rs?.Combat,board,true,out _));
    if(reason!=null && CombatTimingProbe.Enabled)CombatTimingProbe.Event("enemyProjectionFallback",i+":"+reason);
   }
  }
  static EnemyAttackContactPlan.SourceProof EnemyProof(GameRoot g,ReactionCtx ctx) {
   return ctx?.EnemySource?.Proof(g?.Rs?.Combat,g?.Battle,ContactCurrent(g,ctx),out _);
  }
  static bool EnemyProjectionVisualCurrent(ReactionCtx ctx) {
   if(ctx?.Feedback?.HadCapturedTarget==true && !Stage.ImpactTargetCurrent("player",ctx.Feedback.Impact))return false;
   return !ctx.EnemySourceHadVisual || Stage.ImpactTargetCurrent("enemy"+EnemySourceLease.SourceIndex(ctx.ContactEvent),ctx.EnemySourceVisual);
  }
  static bool IndividualEnemyContact(ReactionCtx ctx)=>ctx?.NumericReceipt?.Projection?.Individual==true && ctx.NumericReceipt.ProjectionReason==null;
  static float EnemyContactSpan(ReactionCtx ctx)=>IndividualEnemyContact(ctx)?ctx.NumericReceipt.Projection.Span:0f;
  static bool PriorEnemyProjection(ReactionCtx ctx)=>ctx?.Action!=null && ctx.NumericEpoch==numericEpoch && Numeric.HasPendingEnemyProjectionBefore(ctx.EventIndex);
  static bool LegacyEnemyRanged(ReactionCtx ctx,int index) {var style=CardVfxProfile.EnemyFromSnapshot(ctx?.Prev,index).Style;return style=="beam" || style=="throw";}
  static float EnemyInnerDelay(GameRoot g,GameEvent ev,ReactionCtx ctx) {
   if(!(ev is GameEvent_DamageDealt d) || d.Source!="enemy" || g.Anchor("player")==null || g.Battle?.EnemySprite(d.EnemyIndex??-1)==null)return 0f;
   return LegacyEnemyRanged(ctx,d.EnemyIndex??-1)?.22f:0f;
  }
  static IEnumerator ShowAfterEnemyProjection(GameRoot g,RectTransform fx,GameEvent ev,ReactionCtx ctx) {
   while(ContactCurrent(g,ctx) && PriorEnemyProjection(ctx))yield return null;
   ctx.DispatchDeferred=false;
   if(!ContactCurrent(g,ctx)){RejectStaleContact(g,ctx);yield break;}
   ShowSafely(g,fx,ev,ctx);
  }
  static void FinishDisplayAfterEnemyProjection(GameRoot g,CombatDisplaySequence display,int eventEnd,int epoch,BattleView board) {
   Tween.RunPresentation(FinishDisplayCo(g,display,eventEnd,epoch,board));
  }
  static IEnumerator FinishDisplayCo(GameRoot g,CombatDisplaySequence display,int eventEnd,int epoch,BattleView board) {
   while(epoch==numericEpoch && g.Battle==board && Numeric.HasPendingEnemyProjectionBefore(eventEnd))yield return null;
   if(epoch==numericEpoch && g.Battle==board)display.Finish();
  }
  static void LandIncoming(GameRoot g,ReactionCtx ctx,int amount) {
   try {ctx.CapturedBoard.LandPlayerIncoming(amount);}
   catch(Exception e){NumericHudFailures++;Debug.LogWarning("[Presentation] incoming forecast unavailable: "+e.Message);}
  }
  // One real contribution, not another GameEvent, receipt, attack or gameplay hook.
  static void PresentEnemyContribution(GameRoot g,ReactionCtx ctx,EnemyAttackContribution row,ResolvedCombatFx.Ticket ticket) {
   var fx=g.FxLayer;ResolvedCombatFx.Show(g,fx,ticket);
   var rt=g.Anchor("player");var spr=g.Battle?.PlayerSprite();
   var hit=spr!=null?Tween.CenterIn(spr,fx):rt!=null?Tween.CenterIn(rt,fx):ticket.Ui;
   string style=ticket.Style;bool ranged=style=="beam" || style=="throw";
   int hp=row.HpLossContribution,blocked=row.BlockContribution+row.IceContribution;
   bool attached=!ticket.HasWorld || Stage.ImpactTargetCurrent("player",ticket.Impact);
   Stage.ReleaseEnemyMove(ticket.SourceUnit);   // 技のコマの当たりへ進める (2026-10-28)
   if(ticket.Outcome=="guard") {Audio.Key("DamageDealt.blocked");if(attached)Stage.PlayAnim("player","block");}
   else if(hp>0) {
    Audio.Key(hp>=12?"DamageDealt.enemy.big":"DamageDealt.enemy");
    if(attached) {
     if(!Stage.CombatWorld) {StageFx.FoeHit("player",ranged?ticket.Color:(Color?)null,hp>=12);}
     Stage.PlayAnim("player","hurt");if(spr!=null)Tween.Lunge(spr,new Vector2(-36f,0f));
     Stage.Flash("player");
     if(!ctx.EnemyGlobalHitCueShown) {
      ctx.EnemyGlobalHitCueShown=true;
      // Preserve the original aggregate envelope once, on first actual injury.
      // Stage.Shake keeps its existing fixed-camera/world-mode disable gate.
      int total=ctx.Feedback.Amount;
      Stage.Shake(Mathf.Min(18f,4f+total*.7f)*(style=="blunt"?1.3f:1f),.3f);
      float alpha=Mathf.Min(.35f,.1f+total*.015f);if(StageMotion.EnemyBigFlashing)alpha=Mathf.Min(alpha,StageMotion.EnemyBigHurtFlashAlpha);
      if(alpha>.001f)Tween.ScreenFlash(fx,new Color(.9f,.1f,.1f,alpha));
      if(CombatTimingProbe.Enabled)CombatTimingProbe.Event("enemyGlobalHitCue",ctx.EventIndex+":"+ticket.ContributionOrdinal);
     }
    }
   }
   Vector2 pos=(rt!=null?Tween.CenterIn(rt,fx):ticket.Ui)+new Vector2(UnityEngine.Random.Range(-40f,40f),10f);
   if(hp>0)Tween.Float(fx,pos,"-"+hp,UiKit.ColBad,hp>=15?46:36);
   else if(ticket.Outcome=="guard")Tween.Float(fx,pos,"防いだ",PaperFx.SkyLight,30);
   else if(row.AmountContribution==0)Tween.Float(fx,pos,"0",UiKit.ColDim,30);
   if(blocked>0) {
    if(hp>0)Tween.IconBurst(fx,hit+new Vector2(-16f,0),"shield",new Color(PaperFx.Sky.r,PaperFx.Sky.g,PaperFx.Sky.b,.7f),100f);
    AfterVisual(.08f,()=>Tween.Float(fx,pos+new Vector2(0,-36f),"ブロックで −"+blocked,PaperFx.SkyLight,22,34f,1f));
   }
   if(attached && !Stage.CombatWorld && rt!=null && hp>0)Tween.Punch(rt,Mathf.Min(.1f,.03f+row.AmountContribution*.004f),keepBottom:true);
  }
  static void ShowEnemyAggregateCount(GameRoot g,ReactionCtx ctx) {
   var p=ctx?.NumericReceipt?.Projection;
   if(p==null || p.ExecutedHits<=1 || (p.Reason!=null && p.Reason!="enemy_contact_presentation_limit" && p.Reason!="presentation_capture_limit"))return;
   // True calculated count, aggregate loss already shown by the original landing.
   Vector2 pos=ctx.Feedback?.Ui??new Vector2(-240,100);
   Tween.Float(g.FxLayer,pos+new Vector2(0,48),p.ExecutedHits+"回攻撃・合計"+(p.HpLoss==0?"HP損失0":!ctx.EnemyAggregatePopupShown?"HP −"+p.HpLoss:""),PaperFx.Paper,23,26f,1.05f);
   if(CombatTimingProbe.Enabled)CombatTimingProbe.Event("enemyContactAggregate",ctx.EventIndex+":"+p.ExecutedHits+":"+p.HpLoss+":"+p.Reason);
  }
 }
}

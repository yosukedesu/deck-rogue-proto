using System;
using DeckRogue.Engine.Generated;
namespace DeckRogue.Game {
 internal sealed class ImpSparkBinding {
  CombatPresentationLedger.Receipt receipt;GameEvent_DamageDealt hit;Func<int> generation;int index;
  public bool Committed=>generation()==receipt.Generation && receipt.Committed && receipt.Exact && receipt.Reason==null && ReferenceEquals(hit,receipt.Event);
  public bool Eligible(ResolvedCombatFx.Ticket t)=>HD2DFlags.Hd2dFx && t.ExactSource && t.SourceKey=="enemy:enemy_cinder_imp/spark_toss" && t.SourceUnit=="enemy"+index && t.Target=="player" && t.AttackFamily=="fire" && t.Element=="fire" && !t.FixedSmallContact && t.HasBeamOrigin && t.Impact!=null && Stage.ImpactTargetCurrent(t.Target,t.Impact);
  public bool ExactSevenScald=>receipt.Index+1<receipt.Batch.To&&receipt.Batch.After.EventLog[receipt.Index+1] is GameEvent_StatusInflicted ev&&AdmitsScald(ev);
  public bool ExactSeven=>hit.Amount==7&&hit.HpLoss==7;
  public bool AdmitsScald(GameEvent_StatusInflicted ev) {
   var b=receipt.Batch;int i=receipt.Index;
   return ExactSeven&&generation()==receipt.Generation&&receipt.Reason==null&&ev.Status=="scald"&&ev.Amount==1&&b!=null&&i+2<b.To&&ReferenceEquals(b.After.EventLog[i+1],ev)&&b.After.EventLog[i+2] is GameEvent_EnemyActionResolved a&&a.EnemyIndex==index&&a.Kind=="attack"&&a.Actual==7&&a.HpLoss==7;
  }
  public static ImpSparkBinding Capture(CombatPresentationLedger.Batch batch,CombatPresentationLedger.Receipt receipt,Func<int> generation) {
   if(batch?.Before==null || batch.After==null || receipt==null || receipt.Committed || receipt.Reason!=null || receipt.Generation!=generation() || !ReferenceEquals(receipt.Batch,batch))return null;
   if(!(batch.OwnerCommand is RunCommand_Combat cmd) || !(cmd.Command is Command_EndTurn))return null;
   if(!(receipt.Event is GameEvent_DamageDealt hit) || hit.Source!="enemy" || hit.HpLoss<=0 || !hit.EnemyIndex.HasValue || !CombatPresentationLedger.Valid(hit,receipt.Values,out _) || receipt.Values.Target!="player")return null;
   int index=hit.EnemyIndex.Value;
   if(index<0 || index>=batch.Before.Enemies.Count || index>=batch.After.Enemies.Count || batch.Before.Enemies[index].EnemyId!="enemy_cinder_imp" || batch.After.Enemies[index].EnemyId!="enemy_cinder_imp")return null;
   if(!EnemyExecutionProof.TryMove(batch.After.EventLog,receipt.Index,out var enemy,out var move,out _) || enemy!="enemy_cinder_imp" || move!="spark_toss")return null;
   var a=hit.PresentationEnemyAction;var m=hit.PresentationEnemyAttack;
   if(a.ExecutionEventIndex<batch.From || a.ExecutionEventIndex>=receipt.Index || !(batch.After.EventLog[a.ExecutionEventIndex] is GameEvent_EnemyActionExecuting x) || x.Kind!="attack")return null;
   if(m==null || m.Version!=1 || m.EnemyId!=enemy || m.Resolution!="aggregate-final-mutation" || m.LoopBound!=1 || m.ExecutedHits!=1 || m.Contributions?.Count!=1 || m.EffectiveActual!=hit.Amount)return null;
   if(!ReferenceEquals(batch.After.EventLog[receipt.Index],hit))return null;
   return new ImpSparkBinding{receipt=receipt,hit=hit,generation=generation,index=index};
  }
 }
}

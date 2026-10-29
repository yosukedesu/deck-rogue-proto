using System;
using DeckRogue.Engine.Generated;
namespace DeckRogue.Game {
 internal sealed class CrossbowAimedBinding {
  CombatPresentationLedger.Receipt receipt;GameEvent_DamageDealt hit;Func<int> generation;int index;
  public bool Committed=>generation()==receipt.Generation && receipt.Committed && receipt.Exact && receipt.Reason==null && ReferenceEquals(hit,receipt.Event);
  public bool Eligible(ResolvedCombatFx.Ticket t)=>HD2DFlags.Hd2dFx && t.ExactSource && t.SourceKey=="enemy:enemy_crossbow_archer/aimed_shot" && t.SourceUnit=="enemy"+index && t.Target=="player" && t.AttackFamily=="neutral_projectile_contact" && t.Element=="neutral" && !t.FixedSmallContact && t.HasBeamOrigin && t.Impact!=null && Stage.ImpactTargetCurrent(t.Target,t.Impact);
  public static CrossbowAimedBinding Capture(CombatPresentationLedger.Batch batch,CombatPresentationLedger.Receipt receipt,Func<int> generation){
   if(batch?.Before==null || batch.After==null || receipt==null || receipt.Committed || receipt.Reason!=null || receipt.Generation!=generation() || !ReferenceEquals(receipt.Batch,batch))return null;
   if(!(batch.OwnerCommand is RunCommand_Combat cmd) || !(cmd.Command is Command_EndTurn))return null;
   if(!(receipt.Event is GameEvent_DamageDealt hit) || hit.Source!="enemy" || hit.HpLoss<=0 || !hit.EnemyIndex.HasValue || !CombatPresentationLedger.Valid(hit,receipt.Values,out _) || receipt.Values.Target!="player")return null;
   int index=hit.EnemyIndex.Value;
   if(index<0 || index>=batch.Before.Enemies.Count || index>=batch.After.Enemies.Count || batch.Before.Enemies[index].EnemyId!="enemy_crossbow_archer" || batch.After.Enemies[index].EnemyId!="enemy_crossbow_archer")return null;
   if(!EnemyExecutionProof.TryMove(batch.After.EventLog,receipt.Index,out var enemy,out var move,out _) || enemy!="enemy_crossbow_archer" || move!="aimed_shot")return null;
   var a=hit.PresentationEnemyAction;var m=hit.PresentationEnemyAttack;
   if(a.ExecutionEventIndex<batch.From || a.ExecutionEventIndex>=receipt.Index || !(batch.After.EventLog[a.ExecutionEventIndex] is GameEvent_EnemyActionExecuting x) || x.Kind!="attack")return null;
   if(m==null || m.Version!=1 || m.EnemyId!=enemy || m.Resolution!="aggregate-final-mutation" || m.LoopBound!=1 || m.ExecutedHits!=1 || m.Contributions?.Count!=1 || m.EffectiveActual!=hit.Amount || !ReferenceEquals(batch.After.EventLog[receipt.Index],hit))return null;
   return new CrossbowAimedBinding{receipt=receipt,hit=hit,generation=generation,index=index};
  }
 }
}

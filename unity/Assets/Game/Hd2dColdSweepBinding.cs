using System;
using DeckRogue.Engine.Generated;
namespace DeckRogue.Game {
 internal sealed class Hd2dColdSweepBinding {
  CombatPresentationLedger.Receipt receipt;GameEvent hit;Func<int> generation;string uid;int target;
  public bool Committed=>generation()==receipt.Generation && receipt.Committed && receipt.Exact && receipt.Reason==null && ReferenceEquals(hit,receipt.Event);
  public bool Eligible(ResolvedCombatFx.Ticket t)=>HD2DFlags.Hd2dFx && t.ExactSource && t.SourceKind=="card" && t.SourceKey=="blue_cold_sweep" && t.SourceUid==uid && t.SourceUnit=="player" && t.Target=="enemy"+target && t.AttackFamily=="cold_sweep_contact" && t.Element=="ice" && !t.FixedSmallContact && t.HasBeamOrigin;
  public static Hd2dColdSweepBinding Capture(CombatPresentationLedger.Batch batch,CombatPresentationLedger.Receipt receipt,Func<int> generation) {
   if(batch?.Before==null || batch.After==null || receipt==null || receipt.Reason!=null || receipt.Committed || receipt.Generation!=generation() || !ReferenceEquals(receipt.Batch,batch))return null;
   if(!(batch.OwnerCommand is RunCommand_Combat cmd) || !(cmd.Command is Command_PlayCard play) || !play.TargetIndex.HasValue)return null;
   if(!(receipt.Event is GameEvent_DamageDealt hit) || hit.Source!="player" || hit.HpLoss<=0 || !CombatPresentationLedger.Valid(hit,receipt.Values,out _))return null;
   var source=hit.PresentationSource;
   if(source?.Kind!="card" || source.DefinitionId!="blue_cold_sweep" || string.IsNullOrEmpty(source.Uid) || source.Uid!=play.CardUid || play.TargetIndex!=hit.EnemyIndex)return null;
   int target=play.TargetIndex.Value;
   if(target<0 || target>=batch.Before.Enemies.Count || target>=batch.After.Enemies.Count || batch.Before.Enemies[target].EnemyId!=batch.After.Enemies[target].EnemyId)return null;
   bool inHand=false;foreach(var c in batch.Before.Player.Hand)if(c.Uid==source.Uid && c.Def.Id=="blue_cold_sweep")inHand=true;
   if(!inHand)return null;
   bool played=false;
   for(int i=batch.From;i<batch.To;i++) {
    var ev=batch.After.EventLog[i];
    if(ReferenceEquals(ev,hit))return played && i==receipt.Index?new Hd2dColdSweepBinding{receipt=receipt,hit=hit,generation=generation,uid=source.Uid,target=target}:null;
    if(ev is GameEvent_CardPlayed cp && cp.CardId=="blue_cold_sweep")played=true;
   }
   return null;
  }
 }
}

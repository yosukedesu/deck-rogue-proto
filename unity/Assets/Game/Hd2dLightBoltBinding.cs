using System;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Game {
 // A command-owned candidate captured with the numeric ledger; never synthesizes hits.
 internal sealed class Hd2dLightBoltBinding {
  CombatPresentationLedger.Receipt receipt;
  GameEvent hit;
  Func<int> generation;
  string uid;
  int target;
  static Hd2dLightBoltBinding Reject(CombatPresentationLedger.Receipt r,string reason) {
   if(r?.Event is GameEvent_DamageDealt d && d.PresentationSource?.DefinitionId=="white_light_bolt")UnityEngine.Debug.Log("[LightBolt305] candidate rejected: "+reason);
   return null;
  }
  public bool Committed => generation()!=receipt.Generation ? false : receipt.Committed && receipt.Exact && receipt.Reason==null && ReferenceEquals(receipt.Event,hit);
  public bool Eligible(ResolvedCombatFx.Ticket t) => t.ExactSource && t.SourceKind=="card" && t.SourceKey=="white_light_bolt" && t.SourceUid==uid && t.SourceUnit=="player" && t.Target=="enemy"+target && t.Element=="light" && t.AttackFamily=="light" && !t.FixedSmallContact;
  public static Hd2dLightBoltBinding Capture(ResolvedCombatFx.Ticket ticket,CombatPresentationLedger.Batch batch,CombatPresentationLedger.Receipt receipt,Func<int> generation) {
   if(batch?.Before==null || batch.After==null || receipt==null || receipt.Reason!=null || receipt.Committed || receipt.Generation!=generation() || !ReferenceEquals(receipt.Batch,batch))return Reject(receipt,"ledger");
   if(!(batch.OwnerCommand is RunCommand_Combat command) || !(command.Command is Command_PlayCard play) || !play.TargetIndex.HasValue)return Reject(receipt,"command_or_explicit_target");
   if(!(receipt.Event is GameEvent_DamageDealt hit) || hit.Source!="player" || hit.HpLoss<=0 || !CombatPresentationLedger.Valid(hit,receipt.Values,out _))return Reject(receipt,"hit_receipt");
   var source=hit.PresentationSource;
   if(source?.Kind!="card" || source.DefinitionId!="white_light_bolt" || string.IsNullOrEmpty(source.Uid) || source.Uid!=play.CardUid)return Reject(receipt,"source_uid");
   int target=play.TargetIndex.Value;
   if(hit.EnemyIndex!=target || target<0 || target>=batch.Before.Enemies.Count || target>=batch.After.Enemies.Count || batch.Before.Enemies[target].Hp<=0 || batch.Before.Enemies[target].EnemyId!=batch.After.Enemies[target].EnemyId || (batch.Before.Player.Light??0)<1)return Reject(receipt,"target_or_light");
   bool inHand=false;
   foreach(var card in batch.Before.Player.Hand)if(card.Uid==source.Uid && card.Def.Id=="white_light_bolt")inHand=true;
   if(!inHand)return Reject(receipt,"hand");
   int count=0;bool played=false,spent=false,found=false;
   for(int i=batch.From;i<batch.To;i++) {
    var ev=batch.After.EventLog[i];
    if(i<receipt.Index && ev is GameEvent_CardPlayed cp && cp.CardId=="white_light_bolt")played=true;
    if(i<receipt.Index && ev is GameEvent_LightSpent ls && ls.CardId=="white_light_bolt" && ls.Amount==1)spent=true;
    if(ev is GameEvent_DamageDealt d && d.Source=="player" && d.PresentationSource?.Kind=="card" && d.PresentationSource.DefinitionId=="white_light_bolt" && d.PresentationSource.Uid==source.Uid) {
     if(d.EnemyIndex!=target)return null;
     count++;if(ReferenceEquals(ev,hit) && i==receipt.Index)found=true;
    }
   }
   if(count!=3 || !played || !spent || !found)return Reject(receipt,$"events count={count} played={played} spent={spent} found={found}");
   return new Hd2dLightBoltBinding{receipt=receipt,hit=hit,generation=generation,uid=source.Uid,target=target};
  }
 }
}

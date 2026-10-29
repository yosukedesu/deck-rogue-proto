using System;
using System.Linq;
using DeckRogue.Engine.Generated;
namespace DeckRogue.Game {
 internal sealed class CanopyBinding {
  CombatPresentationLedger.Receipt receipt;GameEvent_DamageDealt hit;Func<int> generation;string uid;int target;
  public bool Committed=>generation()==receipt.Generation && receipt.Committed && receipt.Exact && receipt.Reason==null && ReferenceEquals(hit,receipt.Event);
  public bool Eligible(ResolvedCombatFx.Ticket t)=>HD2DFlags.Hd2dFx && t.ExactSource && t.SourceKey=="green_perm_thorn_canopy" && t.SourceKind=="permanent" && t.SourceUid==uid && t.SourceUnit=="player" && t.Target=="enemy"+target && t.AttackFamily=="plant_point_contact" && t.Element=="vine" && t.FixedSmallContact;
  public static CanopyBinding Capture(CombatPresentationLedger.Batch batch,CombatPresentationLedger.Receipt receipt,Func<int> generation){
   if(batch?.Before==null || batch.After==null || receipt==null || receipt.Committed || receipt.Reason!=null || receipt.Generation!=generation() || !ReferenceEquals(receipt.Batch,batch))return null;
   if(!(receipt.Event is GameEvent_DamageDealt hit) || hit.Source!="player" || hit.HpLoss<=0 || !hit.EnemyIndex.HasValue || !CombatPresentationLedger.Valid(hit,receipt.Values,out _))return null;
   var source=hit.PresentationSource;
   if(source?.Kind!="permanent" || source.DefinitionId!="green_perm_thorn_canopy" || string.IsNullOrEmpty(source.Uid) || hit.SourceUid!=source.Uid)return null;
   bool Installed(CardInstance p)=>p.Uid==source.Uid && p.Def.Id==source.DefinitionId && p.Def.Type=="permanent" && p.Def.Retainer!=true && p.Def.Echo!=true && p.Def.Life==null;
   if(!batch.Before.Player.Permanents.Any(Installed) && !batch.After.Player.Permanents.Any(Installed))return null;
   int target=hit.EnemyIndex.Value;
   if(target<0 || target>=batch.Before.Enemies.Count || target>=batch.After.Enemies.Count || batch.Before.Enemies[target].EnemyId!=batch.After.Enemies[target].EnemyId || !ReferenceEquals(batch.After.EventLog[receipt.Index],hit))return null;
   return new CanopyBinding{receipt=receipt,hit=hit,generation=generation,uid=source.Uid,target=target};
  }
 }
}

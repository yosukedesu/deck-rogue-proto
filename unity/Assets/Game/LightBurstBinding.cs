using System;
using System.Linq;
using DeckRogue.Engine.Generated;
namespace DeckRogue.Game {
 internal sealed class LightBurstBinding {
  CombatPresentationLedger.Receipt receipt;GameEvent_DamageDealt hit;ResolvedCombatFx.Ticket ticket;Func<int> generation;Func<bool> white;
  public bool Committed=>generation()==receipt.Generation&&white()&&receipt.Committed&&receipt.Exact&&receipt.Reason==null&&ReferenceEquals(receipt.Event,hit)&&Stage.ImpactTargetCurrent(ticket.Target,ticket.Impact);
  // Admission for the resource cue, deliberately before damage commit.
  public bool AdmitsDischarge(GameEvent_LightDischarged ev)=>generation()==receipt.Generation&&white()&&receipt.Reason==null&&receipt.Batch!=null&&receipt.Batch.After.EventLog.Skip(receipt.Batch.From).Take(receipt.Batch.To-receipt.Batch.From).Any(x=>ReferenceEquals(x,ev))&&ev.Spent==1&&Stage.ImpactTargetCurrent(ticket.Target,ticket.Impact);
  public bool Eligible(ResolvedCombatFx.Ticket t)=>t.ExactSource&&t.SourceKind=="card"&&t.SourceKey=="white_light_burst"&&t.SourceUid==hit.PresentationSource.Uid&&t.SourceUnit=="player"&&t.Target=="enemy"+hit.EnemyIndex&&t.Element=="light"&&t.AttackFamily=="local_light"&&!t.FixedSmallContact;
  static LightBurstBinding Reject(CombatPresentationLedger.Receipt r,string reason){if(r?.Event is GameEvent_DamageDealt d&&d.PresentationSource?.DefinitionId=="white_light_burst")UnityEngine.Debug.Log("[LightBurst326] rejected "+reason);return null;}
  public static LightBurstBinding Capture(ResolvedCombatFx.Ticket ticket,CombatPresentationLedger.Batch batch,CombatPresentationLedger.Receipt receipt,Func<int> generation,Func<bool> white){
   const string id="white_light_burst";
   if(!HD2DFlags.Hd2dFx||!white()||batch?.Before==null||batch.After==null||receipt==null||receipt.Committed||receipt.Reason!=null||receipt.Generation!=generation()||!ReferenceEquals(receipt.Batch,batch)||!(receipt.Event is GameEvent_DamageDealt hit)||!CombatPresentationLedger.Valid(hit,receipt.Values,out _))return Reject(receipt,"gate1");
   if(!(batch.OwnerCommand is RunCommand_Combat command)||!(command.Command is Command_PlayCard play)||string.IsNullOrEmpty(play.CardUid)||play.ModeIndex.HasValue||batch.Before.Player.Light!=1||batch.After.Player.Light!=0||!batch.Before.Player.Hand.Any(c=>c.Uid==play.CardUid&&c.Def.Id==id&&c.Def.Type=="spell"))return Reject(receipt,"gate2");
   var live=batch.Before.Enemies.Select((e,i)=>new{e,i}).Where(x=>x.e.Hp>0).ToArray();
   if(live.Length!=3||live.Any(x=>x.e.Hp<=4||x.i>=batch.After.Enemies.Count||x.e.EnemyId!=batch.After.Enemies[x.i].EnemyId))return Reject(receipt,"gate3");
   var events=batch.After.EventLog.Skip(batch.From).Take(batch.To-batch.From).ToArray();var played=events.OfType<GameEvent_CardPlayed>().ToArray();var discharged=events.OfType<GameEvent_LightDischarged>().ToArray();var hits=events.OfType<GameEvent_DamageDealt>().ToArray();var weak=events.OfType<GameEvent_EnemyWeakened>().ToArray();
   if(played.Length!=1||played[0].CardId!=id||discharged.Length!=1||discharged[0].Spent!=1||hits.Length!=3||weak.Length!=3||!hits.Any(h=>ReferenceEquals(h,hit))||receipt.Index<batch.From||receipt.Index>=batch.To||!ReferenceEquals(batch.After.EventLog[receipt.Index],hit)||Array.IndexOf(events,played[0])>=Array.IndexOf(events,discharged[0])||Array.IndexOf(events,discharged[0])>=Array.IndexOf(events,hits[0])||Array.IndexOf(events,hits[2])>=Array.IndexOf(events,weak[0]))return Reject(receipt,"gate4");
   for(int i=0;i<3;i++){var h=hits[i];if(h.EnemyIndex!=live[i].i||h.Source!="player"||h.Amount!=4||h.HpLoss<=0||h.SourceUid!=null||h.PresentationSource?.Kind!="card"||h.PresentationSource.DefinitionId!=id||h.PresentationSource.Uid!=play.CardUid||weak[i].EnemyIndex!=live[i].i||weak[i].Amount!=1)return Reject(receipt,"gate5");}
   var result=new LightBurstBinding{receipt=receipt,hit=hit,ticket=ticket,generation=generation,white=white};return result;
  }
 }
}

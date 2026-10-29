using System;
using System.Linq;
using DeckRogue.Engine.Generated;
namespace DeckRogue.Game {
 internal sealed class TripleHornBinding {
  CombatPresentationLedger.Receipt receipt; GameEvent_DamageDealt hit; ResolvedCombatFx.Ticket ticket; Func<int> generation; Func<bool> green; object hero;
  public object Target=>ticket.Impact?.Target;
  public object Hero=>hero;
  public bool Current=>generation()==receipt.Generation&&green()&&receipt.Committed&&receipt.Exact&&receipt.Reason==null&&ReferenceEquals(receipt.Event,hit)&&Stage.ImpactTargetCurrent(ticket.Target,ticket.Impact)&&ReferenceEquals(Stage.CaptureImpact("player")?.Target,hero);
  public bool Eligible(ResolvedCombatFx.Ticket t)=>t.ExactSource&&t.SourceKey=="green_triple_horn"&&t.SourceKind=="card"&&t.SourceUid==hit.PresentationSource.Uid&&t.SourceUnit=="player"&&t.Target==ticket.Target&&t.Element=="physical"&&!t.FixedSmallContact;
  public static TripleHornBinding Capture(ResolvedCombatFx.Ticket t,CombatPresentationLedger.Batch b,CombatPresentationLedger.Receipt r,Func<int> generation,Func<bool> green){
   const string id="green_triple_horn";
   if(!green()||b?.Before==null||b.After==null||r==null||r.Committed||r.Reason!=null||r.Generation!=generation()||!ReferenceEquals(r.Batch,b)||!(r.Event is GameEvent_DamageDealt hit)||!CombatPresentationLedger.Valid(hit,r.Values,out _))return null;
   if(!(b.OwnerCommand is RunCommand_Combat c)||!(c.Command is Command_PlayCard p)||!p.TargetIndex.HasValue||p.ModeIndex.HasValue||string.IsNullOrEmpty(p.CardUid))return null;
   int i=p.TargetIndex.Value;var before=b.Before;var after=b.After;
   if(before.Player.Energy!=3||after.Player.Energy!=1||before.Player.Growth!=0||before.Player.Momentum!=0||before.Player.Weak!=0||!before.Player.Hand.Any(x=>x.Uid==p.CardUid&&x.Def.Id==id&&x.Def.Type=="physical")||i<0||i>=before.Enemies.Count||i>=after.Enemies.Count||before.Enemies[i].Hp<=15||before.Enemies[i].Block!=0||before.Enemies[i].EnemyId!=after.Enemies[i].EnemyId)return null;
   var events=after.EventLog.Skip(b.From).Take(b.To-b.From).ToArray();var played=events.OfType<GameEvent_CardPlayed>().ToArray();var hits=events.OfType<GameEvent_DamageDealt>().ToArray();
   if(played.Length!=1||played[0].CardId!=id||hits.Length!=3||!hits.Any(x=>ReferenceEquals(x,hit))||Array.IndexOf(events,played[0])>=Array.IndexOf(events,hits[0])||!ReferenceEquals(after.EventLog[r.Index],hit))return null;
   if(hits.Any(x=>x.EnemyIndex!=i||x.Amount!=5||x.HpLoss!=5||x.Source!="player"||x.SourceUid!=null||x.PresentationSource?.Kind!="card"||x.PresentationSource.DefinitionId!=id||x.PresentationSource.Uid!=p.CardUid))return null;
   return new TripleHornBinding{receipt=r,hit=hit,ticket=t,generation=generation,green=green,hero=Stage.CaptureImpact("player")?.Target};
  }
 }
}

using System;
using System.Linq;
using DeckRogue.Engine.Generated;
using UnityEngine;
namespace DeckRogue.Game {
 internal sealed class MonkSquireHealBinding {
  CombatPresentationLedger.Receipt receipt; GameEvent_HpHealed heal; Func<int> generation; string monk,squire;
  public bool Current=>generation()==receipt.Generation&&receipt.Reason==null&&ReferenceEquals(receipt.Event,heal);
  public Material Receiver(ResolvedCombatFx.Ticket t){
   if(!Current||t.Target!="player"||t.Event!="HpHealed"||t.Amount!=1||t.Outcome!="heal"||t.AttackFamily!="heal_uplift"||!Stage.ImpactTargetCurrent("player",t.Impact)||t.Impact.Actor!=t.Impact.Target||!Stage.TryGetUnitBox("doll:"+monk,out _,out _)||!Stage.TryGetUnitBox("doll:"+squire,out _,out _))return null;
   var m=(t.Impact.Target as Component)?.GetComponent<MeshRenderer>()?.sharedMaterial;
   return m!=null&&m.shader.name=="DeckRogue/StageUnitLit"?m:null;
  }
  public static MonkSquireHealBinding Capture(CombatPresentationLedger.Batch batch,CombatPresentationLedger.Receipt receipt,Func<int> generation){
   const string m="white_perm_monk",s="white_perm_squire";
   if(batch?.Before==null||batch.After==null||receipt==null||receipt.Reason!=null||receipt.Generation!=generation()||!ReferenceEquals(receipt.Batch,batch)||!(receipt.Event is GameEvent_HpHealed heal)||heal.Amount!=1||string.IsNullOrEmpty(heal.SourceUid)||!CombatPresentationLedger.Valid(heal,receipt.Values,out _)||receipt.Values.Target!="player"||receipt.Values.After.Hp-receipt.Values.Before.Hp!=1)return null;
   if(!(batch.OwnerCommand is RunCommand_Combat cmd)||!(cmd.Command is Command_PlayCard play)||string.IsNullOrEmpty(play.CardUid)||play.CardUid==heal.SourceUid)return null;
   bool Prior(CardInstance p)=>p.Uid==heal.SourceUid&&p.Def.Id==m&&p.Def.Retainer==true&&p.Def.Life==2;
   if(!batch.Before.Player.Permanents.Any(Prior)||!batch.After.Player.Permanents.Any(Prior)||!batch.Before.Player.Hand.Any(p=>p.Uid==play.CardUid&&p.Def.Id==s)||batch.Before.Player.Permanents.Any(p=>p.Uid==play.CardUid)||!batch.After.Player.Permanents.Any(p=>p.Uid==play.CardUid&&p.Def.Id==s&&p.Def.Retainer==true&&p.Def.Life==4&&p.EnteredTurn==batch.After.Turn))return null;
   var events=batch.After.EventLog.Skip(batch.From).Take(batch.To-batch.From).Where(e=>e is GameEvent_PermanentPlayed||e is GameEvent_HpHealed||e is GameEvent_LightGained||e is GameEvent_DamageDealt||e is GameEvent_RetainerRushed).ToArray();
   if(events.Length!=5||!(events[0] is GameEvent_PermanentPlayed pp)||pp.CardId!=s||!ReferenceEquals(events[1],heal)||!ReferenceEquals(batch.After.EventLog[receipt.Index],heal)||!(events[2] is GameEvent_LightGained light)||light.Amount!=1||light.SourceUid!=heal.SourceUid||!(events[3] is GameEvent_DamageDealt hit)||hit.Source!="player"||hit.Amount!=3||hit.HpLoss!=3||hit.SourceUid!=play.CardUid||hit.PresentationSource?.Kind!="permanent"||hit.PresentationSource.DefinitionId!=s||hit.PresentationSource.Uid!=play.CardUid||!(events[4] is GameEvent_RetainerRushed rush)||rush.CardId!=s)return null;
   return new MonkSquireHealBinding{receipt=receipt,heal=heal,generation=generation,monk=heal.SourceUid,squire=play.CardUid};
  }
 }
}

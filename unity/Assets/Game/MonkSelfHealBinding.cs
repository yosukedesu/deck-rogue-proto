using System;
using System.Linq;
using DeckRogue.Engine.Generated;
using UnityEngine;
namespace DeckRogue.Game {
 internal sealed class MonkSelfHealBinding {
  CombatPresentationLedger.Receipt receipt;GameEvent_HpHealed heal;Func<int> generation;string uid;
  public bool Current=>generation()==receipt.Generation&&receipt.Reason==null&&ReferenceEquals(receipt.Event,heal);
  public Material Receiver(ResolvedCombatFx.Ticket t){
   if(!Current||t.Target!="player"||t.Event!="HpHealed"||t.Amount!=1||t.Outcome!="heal"||t.AttackFamily!="heal_uplift"||!Stage.ImpactTargetCurrent("player",t.Impact)||t.Impact.Actor!=t.Impact.Target||!Stage.TryGetUnitBox("doll:"+uid,out _,out _))return null;
   var c=t.Impact.Target as Component;var m=c?.GetComponent<MeshRenderer>()?.sharedMaterial;
   return m!=null&&m.shader.name=="DeckRogue/StageUnitLit"?m:null;
  }
  public static MonkSelfHealBinding Capture(CombatPresentationLedger.Batch batch,CombatPresentationLedger.Receipt receipt,Func<int> generation){
   const string id="white_perm_monk";
   if(batch?.Before==null||batch.After==null||receipt==null||receipt.Reason!=null||receipt.Generation!=generation()||!ReferenceEquals(receipt.Batch,batch)||!(receipt.Event is GameEvent_HpHealed heal)||heal.Amount!=1||string.IsNullOrEmpty(heal.SourceUid)||!CombatPresentationLedger.Valid(heal,receipt.Values,out _)||receipt.Values.Target!="player"||receipt.Values.After.Hp-receipt.Values.Before.Hp!=1)return null;
   if(!(batch.OwnerCommand is RunCommand_Combat cmd)||!(cmd.Command is Command_PlayCard play)||play.CardUid!=heal.SourceUid)return null;
   string uid=play.CardUid;
   if(!batch.Before.Player.Hand.Any(c=>c.Uid==uid&&c.Def.Id==id)||batch.Before.Player.Permanents.Any(c=>c.Uid==uid)||!batch.After.Player.Permanents.Any(c=>c.Uid==uid&&c.Def.Id==id&&c.Def.Retainer==true&&c.Def.Life==2&&c.EnteredTurn==batch.After.Turn))return null;
   bool installed=false;
   for(int i=batch.From;i<batch.To;i++){
    var ev=batch.After.EventLog[i];
    if(ev is GameEvent_PermanentPlayed p){if(installed||p.CardId!=id)return null;installed=true;}
    if(ev is GameEvent_HpHealed){if(!installed||!ReferenceEquals(ev,heal)||i!=receipt.Index)return null;return new MonkSelfHealBinding{receipt=receipt,heal=heal,generation=generation,uid=uid};}
   }
   return null;
  }
 }
}

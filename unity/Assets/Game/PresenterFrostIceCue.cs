using System;
using System.Linq;
using DeckRogue.Engine.Generated;
using TMPro;
using UnityEngine;
namespace DeckRogue.Game {
 public static partial class Presenter {
  sealed class FrostIceCue {internal CombatPresentationLedger.Batch Batch;internal int Generation;internal RectTransform Root;internal float Height;}
  static FrostIceCue frostIceCue;
  static Func<RectTransform,Vector2,Vector2> FrostIcePlacement(GameRoot g,ReactionCtx ctx,GameEvent_IceBlockGained ice){
   var receipt=ctx?.NumericReceipt;var batch=receipt?.Batch;
   bool Current()=>g?.LeaderId=="leader_blue"&&ContactCurrent(g,ctx)&&receipt!=null&&receipt.Generation==Numeric.Generation&&receipt.Committed&&receipt.Reason==null&&ReferenceEquals(receipt.Event,ice);
   if(!Current()||batch?.Before==null||batch.After==null||!CombatPresentationLedger.Valid(ice,receipt.Values,out _))return null;
   if(!(batch.OwnerCommand is RunCommand_Combat command)||!(command.Command is Command_PlayCard play)||string.IsNullOrEmpty(play.CardUid)||!play.TargetIndex.HasValue||play.ModeIndex.HasValue)return null;
   int target=play.TargetIndex.Value;
   if(target<0||target>=batch.Before.Enemies.Count||target>=batch.After.Enemies.Count||batch.Before.Enemies[target].EnemyId!=batch.After.Enemies[target].EnemyId||!batch.Before.Player.Hand.Any(c=>c.Uid==play.CardUid&&c.Def.Id=="blue_frost_shackle"))return null;
   var events=batch.After.EventLog.Skip(batch.From).Take(batch.To-batch.From).ToArray();
   var selected=events.Where(e=>e is GameEvent_CardPlayed||e is GameEvent_EnemyWeakened||e is GameEvent_IceBlockGained).ToArray();
   if(selected.Length!=4||!(selected[0] is GameEvent_CardPlayed card)||card.CardId!="blue_frost_shackle"||!(selected[1] is GameEvent_EnemyWeakened weak)||weak.EnemyIndex!=target||weak.Amount!=1||!(selected[2] is GameEvent_IceBlockGained nine)||nine.Amount!=9||!(selected[3] is GameEvent_IceBlockGained one)||one.Amount!=1||events.Any(e=>e is GameEvent_DamageDealt)||(!ReferenceEquals(ice,nine)&&!ReferenceEquals(ice,one))||receipt.Index<batch.From||receipt.Index>=batch.To||!ReferenceEquals(batch.After.EventLog[receipt.Index],ice))return null;
   return (root,pos)=>{
    if(!Current())return pos;
    float lo=float.PositiveInfinity,hi=float.NegativeInfinity;
    foreach(var text in root.GetComponentsInChildren<TMP_Text>()){
     text.ForceMeshUpdate();var bounds=text.mesh.bounds;
     float a=root.InverseTransformPoint(text.transform.TransformPoint(bounds.min)).y,b=root.InverseTransformPoint(text.transform.TransformPoint(bounds.max)).y;
     lo=Mathf.Min(lo,a);hi=Mathf.Max(hi,b);
    }
    float height=hi-lo;if(float.IsNaN(height)||float.IsInfinity(height)||height<=0)return pos;
    if(ReferenceEquals(ice,nine)){frostIceCue=new FrostIceCue{Batch=batch,Generation=receipt.Generation,Root=root,Height=height};Debug.Log("[Frost323] primary9 index="+receipt.Index+" height="+height+" y="+pos.y);return pos;}
    var previous=frostIceCue;frostIceCue=null;
    if(previous==null||previous.Root==null||previous.Generation!=Numeric.Generation||!ReferenceEquals(previous.Batch,batch))return pos;
    // TMP mesh bounds include outline padding; shadow bounds are included above. Existing OutBack scale peaks below1.041.
    float clearance=(previous.Height+height)*.5f*1.041f+8f;
    float y=Mathf.Max(pos.y,previous.Root.anchoredPosition.y+clearance);
    Debug.Log("[Frost323] passive1 index="+receipt.Index+" height="+height+" previousY="+previous.Root.anchoredPosition.y+" offset="+(y-pos.y)+" clearance="+clearance);
    return new Vector2(pos.x,y);
   };
  }
 }
}

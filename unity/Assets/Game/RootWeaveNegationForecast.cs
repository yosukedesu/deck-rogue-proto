using System;
using System.Linq;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
namespace DeckRogue.Game {
 // UI projection only. The authoritative engine state and enemy intents are never changed.
 internal static class RootWeaveNegationForecast {
  public static int? Remaining(CombatPresentationLedger.Batch batch,GameEvent_ActionNegated negated,int generation){
   const string id="green_reaction_root_weave";
   if(batch==null||batch.Generation!=generation||batch.Finished||negated==null||!(batch.OwnerCommand is RunCommand_Combat cmd)||!(cmd.Command is Command_ConfirmReaction confirm)||!confirm.Fire||string.IsNullOrEmpty(confirm.CardUid))return null;
   var before=batch.Before;var after=batch.After;int index=negated.EnemyIndex;
   if(before==null||after==null||before.ReactionMode!=ReactionModes.SetConfirm||before.Phase!=CombatPhases.AwaitingReaction||before.PendingWindow?.Stage!="pre"||before.PendingWindow.EnemyIndex!=index||index<0||index>=before.Enemies.Count||index>=after.Enemies.Count||before.Enemies[index].EnemyId!=after.Enemies[index].EnemyId)return null;
   var card=before.Player.SetCards.FirstOrDefault(x=>x.Uid==confirm.CardUid&&x.Def.Id==id);var window=Effects.WindowFromPending(before);
   if(card==null||card.Def.Type!=CardTypes.Reaction||window==null||!Effects.IsTrapLive(before,card)||!Effects.UsableSetCards(before,window).Any(x=>x.Uid==confirm.CardUid)||after.Player.SetCards.Any(x=>x.Uid==confirm.CardUid))return null;
   bool triggered=false,grown=false;
   for(int i=batch.From;i<batch.To;i++){
    var ev=after.EventLog[i];
    if(ReferenceEquals(ev,negated)){
     if(!triggered||!grown)return null;
     var enemies=before.Enemies.ToArray();enemies[index]=enemies[index] with {ActionNegated=true};
     return Effects.IncomingTotal(before with {Enemies=enemies});
    }
    if(ev is GameEvent_ReactionTriggered tr){if(triggered||tr.CardId!=id||tr.Mode!=ReactionModes.SetConfirm)return null;triggered=true;}
    if(ev is GameEvent_GrowthAdded growth){if(!triggered||grown||growth.Amount!=2)return null;grown=true;}
    if(ev is GameEvent_ActionNegated)return null;
   }
   return null;
  }
 }
}

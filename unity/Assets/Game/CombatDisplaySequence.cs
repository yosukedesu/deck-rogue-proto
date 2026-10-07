using System;
using DeckRogue.Engine.Generated;
namespace DeckRogue.Game {
 // Immutable engine state is already resolved. The old board stays visible under the transition input gate.
 sealed class CombatDisplaySequence {
  readonly GameRoot g; readonly BattleView board; readonly GameState previous,current; readonly int revision;
  public bool Current => g != null && g.Rs != null && ReferenceEquals(g.Rs.Combat,current) && g.Battle==board && board.DisplayRevision==revision;
  public CombatDisplaySequence(GameRoot root,GameState before,GameState after){
   g=root;board=g.Battle;previous=before;current=after;revision=board.DisplayRevision;
   board.SetPlayerBlock(before.Player.Block);board.SetPlayerIce(before.Player.IceBlock);
   board.DisplayPlayerHp(before.Player.Hp,true);
   BattleScreen.SetIncomingState(board.SelfArea,before);
   board.NudgePlayerHp(0);
   for(int i=0;i<before.Enemies.Count;i++){board.SetEnemyBlock(i,before.Enemies[i].Block);BattleScreen.RefreshIntent(g,before,i);}
   Mark("begin");
  }
  public void Before(GameEvent ev,int enemy){
   if(!Current)return;
   switch(ev){
    case GameEvent_DeathSaved h: board.DisplayPlayerHp(h.Hp,false);break;
    case GameEvent_HpHealed h: board.NudgePlayerHp(h.Amount);break;
    case GameEvent_HpLost h: board.NudgePlayerHp(-h.Amount);break;
    case GameEvent_ScaldTick h: board.NudgePlayerHp(-h.Amount);break;
    case GameEvent_ThornsReflected h: board.AbsorbPlayerBlock(Math.Max(0,h.Amount-h.HpLoss));board.NudgePlayerHp(-h.HpLoss);break;
    case GameEvent_TurnEnded _: board.ResetEnemyBlocks(previous);break;
    case GameEvent_TurnStarted _: board.SetPlayerBlock(current.BlockKeep.HasValue?Math.Min(board.ShownPlayerBlock,current.BlockKeep.Value):0);break;
    case GameEvent_BlockGained b: if(b.Target=="player")board.NudgePlayerBlock(b.Amount);else board.NudgeEnemyBlock(Math.Max(0,enemy),b.Amount);break;
    case GameEvent_IceBlockGained b: board.NudgePlayerIce(b.Amount);break;
    case GameEvent_BlockShattered b: board.SetEnemyBlock(b.EnemyIndex,0);break;
    case GameEvent_DamageDealt d: if(d.Source=="player" && d.Blocked>0)board.NudgeEnemyBlock(d.EnemyIndex??0,-d.Blocked.Value);break;
    case GameEvent_EnemyIntentDeclared d: BattleScreen.RefreshIntent(g,current,d.EnemyIndex);break;
    case GameEvent_EnemyInterrupted d: if(d.Replaced)BattleScreen.RefreshIntent(g,current,d.EnemyIndex);break;
   }
   Mark(ev.GetType().Name);
  }
  public void Land(GameEvent_DamageDealt d){if(!Current)return;board.LandPlayerIncoming(d.Amount);board.AbsorbPlayerBlock(d.Blocked??0);board.NudgePlayerHp(-d.HpLoss);Mark("land");}
  public void Finish(){
   if(!Current)return;
   board.SetPlayerBlock(current.Player.Block);board.SetPlayerIce(current.Player.IceBlock);
   BattleScreen.SetIncomingState(board.SelfArea,current);board.DisplayPlayerHp(current.Player.Hp,false);
   for(int i=0;i<current.Enemies.Count;i++){board.SetEnemyBlock(i,current.Enemies[i].Block);BattleScreen.RefreshIntent(g,current,i);}
   Mark("finish");
  }
  void Mark(string cause){if(CombatTimingProbe.Enabled)CombatTimingProbe.Event("defenseDisplay",cause+":"+board.ShownPlayerBlock+":"+board.ShownPlayerIce);}
 }
}

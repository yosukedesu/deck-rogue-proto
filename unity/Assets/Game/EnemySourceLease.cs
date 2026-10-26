using System;
using System.Collections.Generic;
using DeckRogue.Engine.Generated;
namespace DeckRogue.Game {
 // Presentation-only lease. Capture only the actual output of the accepted engine
 // command, at reservation (not later launch). The engine has no EnemyState UID.
 // A later record copy is an unproved continuation, not proof of replacement.
 public sealed class EnemySourceLease {
  public readonly int EnemyIndex,EventIndex;
  public readonly string EnemyId;
  public readonly EnemyActionPresentation Execution;
  readonly GameState commandAfter;
  readonly EnemyState source;
  readonly object board;
  EnemySourceLease(GameState after,int index,int eventIndex,object capturedBoard,EnemyActionPresentation execution) {
   commandAfter=after;EnemyIndex=index;EventIndex=eventIndex;EnemyId=after.Enemies[index].EnemyId;
   source=after.Enemies[index];board=capturedBoard;Execution=execution;
  }
  public static bool Prefix(GameState before,GameState after) {
   if(before?.EventLog==null || after?.EventLog==null || before.EventLog.Count>after.EventLog.Count)return false;
   for(int i=0;i<before.EventLog.Count;i++)if(!ReferenceEquals(before.EventLog[i],after.EventLog[i]))return false;
   return true;
  }
  internal static int SourceIndex(GameEvent ev)=>ev is GameEvent_DamageDealt d && d.Source=="enemy"?d.EnemyIndex??-1:ev is GameEvent_ConfusedAttack c?c.EnemyIndex:ev is GameEvent_EnemyActionExecuting x?x.EnemyIndex:-1;
  internal static bool ReplacementBoundary(GameEvent ev,int index)=>ev is GameEvent_CombatStarted
   || ev is GameEvent_EnemyHatched h && h.EnemyIndex==index;
  internal static bool RetirementBoundary(GameEvent ev,int index)=>ev is GameEvent_EnemyDied d && d.EnemyIndex==index
   || ev is GameEvent_EnemyFled f && f.EnemyIndex==index;
  internal static bool PopulationBoundary(GameEvent ev,int index)=>ReplacementBoundary(ev,index)||RetirementBoundary(ev,index);
  public static EnemySourceLease Capture(GameState before,GameState after,int eventIndex,object capturedBoard,out string reason) {
   reason=null;
   if(!Prefix(before,after)){reason="enemy_source_command_prefix_unproved";return null;}
   if(capturedBoard==null){reason="enemy_source_board_owner_missing";return null;}
   if(eventIndex<before.EventLog.Count || eventIndex>=after.EventLog.Count){reason="enemy_source_not_new_command_event";return null;}
   var ev=after.EventLog[eventIndex];int index=SourceIndex(ev);
   if(index<0 || before.Enemies==null || after.Enemies==null || index>=before.Enemies.Count || index>=after.Enemies.Count
    || before.Enemies[index]==null || after.Enemies[index]==null || string.IsNullOrEmpty(after.Enemies[index].EnemyId)
    || before.Enemies[index].EnemyId!=after.Enemies[index].EnemyId){reason="enemy_source_command_population_unproved";return null;}
   if(!EnemyExecutionProof.TryExecution(after.EventLog,eventIndex,out var execution,out reason))return null;
   if(execution.EnemyId!=after.Enemies[index].EnemyId){reason="enemy_source_execution_id_mismatch";return null;}
   // Include the complete accepted command and any ancestor execution interval.
   // Hatch/reset can never be treated as the same instance. A real retirement
   // AFTER this receipt does not erase the already-resolved attack. Numeric
   // contacts need no living source art; its exact after-record is still leased.
   for(int i=Math.Min(before.EventLog.Count,execution.ExecutionEventIndex+1);i<after.EventLog.Count;i++)
    if(ReplacementBoundary(after.EventLog[i],index) || (i<=eventIndex && RetirementBoundary(after.EventLog[i],index))){reason="enemy_source_population_boundary";return null;}
   return new EnemySourceLease(after,index,eventIndex,capturedBoard,execution);
  }
  public bool Current(GameState latest,object currentBoard,bool ownerCurrent,out string reason) {
   reason=null;
   if(!ownerCurrent){reason="enemy_source_command_owner_stale";return false;}
   if(!ReferenceEquals(board,currentBoard)){reason="enemy_source_board_owner_changed";return false;}
   if(!Prefix(commandAfter,latest)){reason="enemy_source_event_lineage_unproved";return false;}
   if(latest.Enemies==null || EnemyIndex>=latest.Enemies.Count){reason="enemy_source_command_population_unproved";return false;}
   for(int i=commandAfter.EventLog.Count;i<latest.EventLog.Count;i++)
    if(ReplacementBoundary(latest.EventLog[i],EnemyIndex)){reason="enemy_source_population_boundary";return false;}
   if(!ReferenceEquals(source,latest.Enemies[EnemyIndex])){reason="enemy_source_record_continuity_unproved";return false;}
   return true;
  }
  // ownerCurrent must include host epoch, uncancelled action, matching active
  // ledger/latest and contact event ownership. Stage anchors are deliberately
  // separate: absent source/player art must not erase a valid numeric receipt.
  public EnemyAttackContactPlan.SourceProof Proof(GameState latest,object currentBoard,bool ownerCurrent,out string reason)
   =>new EnemyAttackContactPlan.SourceProof(EnemyIndex,EnemyId,Current(latest,currentBoard,ownerCurrent,out reason));
 }
 public static class EnemyExecutionProof {
  static bool ExecutionBarrier(GameEvent ev,int index)=>ev is GameEvent_TurnStarted
   || EnemySourceLease.PopulationBoundary(ev,index)
   || ev is GameEvent_EnemyActionExecuting || ev is GameEvent_EnemyActionResolved || ev is GameEvent_ActionNegated
   || ev is GameEvent_EnemyIntentDeclared d && d.EnemyIndex==index;
  static bool DeclarationBarrier(GameEvent ev,int index)=>ev is GameEvent_TurnStarted
   || EnemySourceLease.PopulationBoundary(ev,index)
   || ev is GameEvent_EnemyActionExecuting x && x.EnemyIndex==index
   || ev is GameEvent_EnemyActionResolved r && r.EnemyIndex==index
   || ev is GameEvent_ActionNegated n && n.EnemyIndex==index
   || ev is GameEvent_EnemyIntentDeclared d && d.EnemyIndex==index;
  public static bool TryExecution(IReadOnlyList<GameEvent> log,int eventIndex,out EnemyActionPresentation execution,out string reason) {
   execution=null;reason=null;
   if(log==null || eventIndex<0 || eventIndex>=log.Count){reason="enemy_execution_event_missing";return false;}
   var ev=log[eventIndex];int index=EnemySourceLease.SourceIndex(ev);
   var a=ev is GameEvent_DamageDealt d?d.PresentationEnemyAction:ev is GameEvent_ConfusedAttack c?c.PresentationEnemyAction:ev is GameEvent_EnemyActionExecuting x?x.PresentationEnemyAction:null;
   if(index<0 || a==null){reason="missing_enemy_execution_metadata";return false;}
   if(a.Version!=1 || string.IsNullOrEmpty(a.EnemyId)){reason="invalid_enemy_execution_metadata";return false;}
   int ei=a.ExecutionEventIndex;
   if(ei<0 || ei>eventIndex || !(log[ei] is GameEvent_EnemyActionExecuting ex) || ex.EnemyIndex!=index
    || !ReferenceEquals(a,ex.PresentationEnemyAction) || (ei==eventIndex && !ReferenceEquals(ev,ex))){reason="enemy_execution_reference_unproved";return false;}
   for(int i=ei+1;i<eventIndex;i++)if(ExecutionBarrier(log[i],index)){reason="enemy_execution_boundary_unproved";return false;}
   if(ev is GameEvent_DamageDealt attack && attack.PresentationEnemyAttack!=null && attack.PresentationEnemyAttack.EnemyId!=a.EnemyId){reason="enemy_execution_attack_id_mismatch";return false;}
   execution=a;return true;
  }
  // Success means explicit metadata validates one exact route key; it does not
  // assert this key is mapped or authorize an inferred element/transport.
  public static bool TryMove(IReadOnlyList<GameEvent> log,int eventIndex,out string enemyId,out string moveId,out string reason) {
   enemyId=null;moveId=null;
   if(!TryExecution(log,eventIndex,out var a,out reason))return false;
   if(a.UnresolvedReason!=null){reason="enemy_execution_unresolved:"+a.UnresolvedReason;return false;}
   if(string.IsNullOrEmpty(a.MoveId)){reason="missing_executed_enemy_move";return false;}
   int di=a.DeclaredEventIndex??-1,index=EnemySourceLease.SourceIndex(log[eventIndex]);
   if(di<0 || di>=a.ExecutionEventIndex || !(log[di] is GameEvent_EnemyIntentDeclared declared) || declared.EnemyIndex!=index){reason="enemy_move_declaration_unproved";return false;}
   var p=declared.PresentationMove;
   if(p==null || p.Version!=1 || p.EventIndex!=di || p.EnemyId!=a.EnemyId || p.Origin!=a.Origin){reason="enemy_move_declaration_metadata_invalid";return false;}
   string selected=a.Branch=="primary"?p.PrimaryMoveId:a.Branch=="alternate"?p.AlternateMoveId:null;
   if(selected!=a.MoveId){reason="enemy_move_branch_mismatch";return false;}
   bool graph=p.Origin=="graph"&&!string.IsNullOrEmpty(p.PrimaryMoveId)&&!string.IsNullOrEmpty(p.PrimaryNodeId);
   bool forced=(p.Origin=="forced-bite"||p.Origin=="forced-flee")&&a.Branch=="primary"&&p.PrimaryNodeId==null&&p.AlternateMoveId==null;
   if(!graph&&!forced){reason="enemy_move_origin_unproved";return false;}
   for(int i=di+1;i<a.ExecutionEventIndex;i++)if(DeclarationBarrier(log[i],index)){reason="enemy_move_declaration_boundary_unproved";return false;}
   enemyId=a.EnemyId;moveId=a.MoveId;reason=null;return true;
  }
 }
}

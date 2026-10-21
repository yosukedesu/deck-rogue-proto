using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine.Generated;
namespace DeckRogue.GodotPort.Presentation;

// Read-only, exact devour proof. CardsMilled has no caster identity. Only this
// closed canonical execution interval attributes its real object to this actor.
// Ambiguous pre-action hooks/identity changes fail closed; Engine is untouched.
public sealed class DevourerMillAppliedOncePlan
{
 public GameEvent_EnemyActionExecuting Executing { get; }
 public int ExecutionIndex { get; }
 public GameEvent_CardsMilled Applied { get; }
 public int AppliedIndex { get; }
 public GameEvent_EnemyActionResolved Resolved { get; }
 public int ResolvedIndex { get; }
 public int EnemyIndex { get; }
 public int Count { get; }
 private readonly string[] _cardIds;
 private DevourerMillAppliedOncePlan(GameEvent_EnemyActionExecuting executing,int start,GameEvent_CardsMilled applied,GameEvent_EnemyActionResolved resolved,int end)
 {Executing=executing;ExecutionIndex=start;Applied=applied;AppliedIndex=start+1;Resolved=resolved;ResolvedIndex=end;EnemyIndex=executing.EnemyIndex;Count=applied.Count;_cardIds=applied.CardIds!.ToArray();}
 public static DevourerMillAppliedOncePlan? TryReserve(GameState? before,IReadOnlyList<GameEvent>? log,GameEvent_EnemyActionExecuting executing)
 {
  var m=executing.PresentationEnemyAction;
  if(before==null||log==null||m==null||m.Version!=1||m.UnresolvedReason!=null||m.EnemyId!="enemy_elite_devourer"||m.MoveId!="devour"||m.Branch!="primary"||m.Origin!="graph"||executing.Kind!="mill"||executing.EnemyIndex<0||executing.EnemyIndex>=before.Enemies.Count||before.Enemies[executing.EnemyIndex].EnemyId!=m.EnemyId||before.Enemies[executing.EnemyIndex].Hp<=0)return null;
  int start=m.ExecutionEventIndex;
  if(start<before.EventLog.Count||start+3>=log.Count||!ReferenceEquals(log[start],executing)||log.Count(e=>ReferenceEquals(e,executing))!=1||m.DeclaredEventIndex is not int di||di<0||di>=start||log[di] is not GameEvent_EnemyIntentDeclared d||d.EnemyIndex!=executing.EnemyIndex||d.Intent is not {Kind:"mill",Actual:3,StrengthPerMilled:1}||d.PresentationMove is not {Version:1,EnemyId:"enemy_elite_devourer",PrimaryMoveId:"devour",Origin:"graph"} dm||dm.EventIndex!=di)return null;
  for(int i=0;i<before.EventLog.Count;i++)if(!ReferenceEquals(before.EventLog[i],log[i]))return null;
  for(int i=before.EventLog.Count;i<start;i++)if(Barrier(log[i],executing.EnemyIndex))return null;
  if(log[start+1] is not GameEvent_CardsMilled applied||applied.Count is <1 or >3||applied.CardIds==null||applied.CardIds.Count!=applied.Count||applied.CardIds.Any(string.IsNullOrEmpty)||log.Count(e=>ReferenceEquals(e,applied))!=1)return null;
  for(int i=start+2;i<log.Count;i++){
   if(log[i] is GameEvent_EnemyActionResolved resolved){
    if(resolved.EnemyIndex!=executing.EnemyIndex||resolved.Kind!="mill"||resolved.Actual!=3||resolved.HpLoss!=0||log.Count(e=>ReferenceEquals(e,resolved))!=1||log[i-1] is not GameEvent_StrengthGained strength||strength.EnemyIndex!=executing.EnemyIndex||strength.Amount!=applied.Count||strength.Reason!=null||log.Count(e=>ReferenceEquals(e,strength))!=1)return null;
    return new(executing,start,applied,resolved,i);
   }
   if(log[i] is GameEvent_EnemyActionExecuting or GameEvent_CardsMilled or GameEvent_EnemyPhaseEnded or GameEvent_TurnStarted or GameEvent_ActionNegated||Barrier(log[i],executing.EnemyIndex))return null;
  }
  return null;
 }
 private static bool Barrier(GameEvent e,int enemy)=>e is GameEvent_EnemySummoned or GameEvent_EnemySplit or GameEvent_EnemyHatched or GameEvent_CombatStarted or GameEvent_CombatEnded||e is GameEvent_EnemyDied d&&d.EnemyIndex==enemy||e is GameEvent_EnemyFled f&&f.EnemyIndex==enemy;
 public bool IsApplied(GameEvent e,int index)=>index==AppliedIndex&&ReferenceEquals(e,Applied)&&Applied.Count==Count&&Applied.CardIds!=null&&Applied.CardIds.SequenceEqual(_cardIds);
 public bool IsResolved(GameEvent e,int index)=>index==ResolvedIndex&&ReferenceEquals(e,Resolved);
}

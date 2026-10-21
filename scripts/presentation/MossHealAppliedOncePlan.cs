using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine.Generated;
namespace DeckRogue.GodotPort.Presentation;

// M59 text-only draft. Rebase to final M58 and compile only after release.
// Read-only identity/receipt proof, not a target selector or combat rule.
// The Engine's exact event owns both positive and zero healing. The caller
// additionally fences the live caster token, queue generation and target actor.
public sealed class MossHealAppliedOncePlan
{
 public GameEvent_EnemyActionExecuting Executing { get; }
 public int ExecutionIndex { get; }
 public GameEvent_EnemyHealed Outcome { get; }
 public int OutcomeIndex { get; }
 public GameEvent_EnemyActionResolved Resolved { get; }
 public int ResolvedIndex { get; }
 public int EnemyIndex { get; } // Caster, deliberately not TargetIndex.
 public int TargetIndex { get; }
 public string TargetEnemyId { get; }
 public int Amount { get; }
 public int BeforeHp { get; }
 public int AfterHp { get; }
 public bool Positive => Amount > 0;
 private readonly CombatPresentationReceipt _receipt;
 private readonly CombatPresentationVitals _before, _after;

 private MossHealAppliedOncePlan(GameEvent_EnemyActionExecuting executing,
   int start, GameEvent_EnemyHealed outcome, GameEvent_EnemyActionResolved resolved)
 {
  Executing=executing; ExecutionIndex=start; Outcome=outcome; OutcomeIndex=start+1;
  Resolved=resolved; ResolvedIndex=start+2; EnemyIndex=executing.EnemyIndex;
  TargetIndex=outcome.TargetIndex; Amount=outcome.Amount;
  _receipt=outcome.PresentationReceipt!; _before=_receipt.Before; _after=_receipt.After;
  TargetEnemyId=_receipt.EnemyId!; BeforeHp=_before.Hp; AfterHp=_after.Hp;
 }

 public static MossHealAppliedOncePlan? TryReserve(GameState? before,
   IReadOnlyList<GameEvent>? log, GameEvent_EnemyActionExecuting executing)
 {
  var m=executing.PresentationEnemyAction;
  if(before==null||log==null||m==null||m.Version!=1||m.UnresolvedReason!=null||
     m.EnemyId!="enemy_moss_healer"||m.MoveId!="mend"||m.Branch!="primary"||
     m.Origin!="graph"||executing.Kind!="heal"||executing.EnemyIndex<0||
     executing.EnemyIndex>=before.Enemies.Count||
     before.Enemies[executing.EnemyIndex].EnemyId!=m.EnemyId||
     before.Enemies[executing.EnemyIndex].Hp<=0)return null;
  int start=m.ExecutionEventIndex;
  if(start<before.EventLog.Count||start+2>=log.Count||
     !ReferenceEquals(log[start],executing)||
     log.Count(e=>ReferenceEquals(e,executing))!=1||
     m.DeclaredEventIndex is not int di||di<0||di>=start||
     log[di] is not GameEvent_EnemyIntentDeclared d||d.EnemyIndex!=executing.EnemyIndex||
     d.Intent is not {Kind:"heal",Actual:>=8 and <=12}||
     d.Intent.AlsoBuff!=null||d.Intent.AlsoDefend!=null||d.Intent.Inflict!=null||
     d.Intent.ConditionalOn!=null||d.Intent.Alt!=null||
     d.PresentationMove is not {Version:1,EnemyId:"enemy_moss_healer",
       PrimaryMoveId:"mend",Origin:"graph"} dm||dm.EventIndex!=di)return null;
  for(int i=0;i<before.EventLog.Count;i++)
   if(!ReferenceEquals(before.EventLog[i],log[i]))return null;

  // Require the canonical, unambiguous closed interval. Interleaved hooks,
  // duplicate outcomes and interrupted/negated intervals do not get an applied pose.
  if(log[start+1] is not GameEvent_EnemyHealed h||
     h.EnemyIndex!=executing.EnemyIndex||h.TargetIndex<0||
     h.TargetIndex>=before.Enemies.Count||before.Enemies[h.TargetIndex].Hp<=0||
     h.Amount<0||h.Amount>d.Intent.Actual||
     log.Count(e=>ReferenceEquals(e,h))!=1||
     log[start+2] is not GameEvent_EnemyActionResolved r||
     r.EnemyIndex!=executing.EnemyIndex||r.Kind!="heal"||
     r.Actual!=d.Intent.Actual||r.HpLoss!=0||
     log.Count(e=>ReferenceEquals(e,r))!=1)return null;
  var receipt=h.PresentationReceipt;
  if(receipt is not {Version:1,Target:"enemy"}||
     receipt.EnemyIndex!=h.TargetIndex||
     receipt.EnemyId!=before.Enemies[h.TargetIndex].EnemyId||
     string.IsNullOrEmpty(receipt.EnemyId)||receipt.Before==null||receipt.After==null)return null;
  var a=receipt.Before;var b=receipt.After;
  if(a.MaxHp<=0||a.Hp<=0||a.Hp>a.MaxHp||b.MaxHp!=a.MaxHp||
     b.Hp<a.Hp||b.Hp>b.MaxHp||b.Hp-a.Hp!=h.Amount||
     h.Amount!=Math.Min(d.Intent.Actual,a.MaxHp-a.Hp)||
     a.Block<0||b.Block!=a.Block||a.IceBlock!=null||b.IceBlock!=null)return null;
  for(int i=before.EventLog.Count;i<start;i++)
   if(Barrier(log[i],executing.EnemyIndex,h.TargetIndex))return null;
  return new(executing,start,h,r);
 }

 private static bool Barrier(GameEvent e,int caster,int target)=>
  e is GameEvent_EnemySummoned or GameEvent_EnemySplit or GameEvent_EnemyHatched or
    GameEvent_CombatStarted or GameEvent_CombatEnded||
  e is GameEvent_EnemyDied d&&(d.EnemyIndex==caster||d.EnemyIndex==target)||
  e is GameEvent_EnemyFled f&&(f.EnemyIndex==caster||f.EnemyIndex==target);

 public bool IsOutcome(GameEvent e,int index)=>
  index==OutcomeIndex&&ReferenceEquals(e,Outcome)&&
  Outcome.EnemyIndex==EnemyIndex&&Outcome.TargetIndex==TargetIndex&&Outcome.Amount==Amount&&
  ReferenceEquals(Outcome.PresentationReceipt,_receipt)&&
  ReferenceEquals(_receipt.Before,_before)&&ReferenceEquals(_receipt.After,_after)&&
  _receipt.EnemyIndex==TargetIndex&&_receipt.EnemyId==TargetEnemyId&&
  _before.Hp==BeforeHp&&_after.Hp==AfterHp&&AfterHp-BeforeHp==Amount;
 public bool IsApplied(GameEvent e,int index)=>Positive&&IsOutcome(e,index);
 public bool IsResolved(GameEvent e,int index)=>
  index==ResolvedIndex&&ReferenceEquals(e,Resolved);
}

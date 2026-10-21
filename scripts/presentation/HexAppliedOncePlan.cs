using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine.Generated;
using DeckRogue.Engine;
namespace DeckRogue.GodotPort.Presentation;

// Read-only proof for recipient-only hex events. The exact closed interval must
// contain one canonical outcome and the same caster's resolution, nothing else.
public sealed record HexAppliedOncePlan(
 GameEvent_EnemyActionExecuting Executing,int ExecutionIndex,GameEvent Outcome,int OutcomeIndex,
 GameEvent_EnemyActionResolved Resolved,int ResolvedIndex,int EnemyIndex,string EnemyId,string MoveId,string Status,int Amount,bool Blocked)
{
 public static HexAppliedOncePlan? TryReserve(GameState? before,IReadOnlyList<GameEvent>? log,GameEvent_EnemyActionExecuting executing,GameState? after=null){
  var m=executing.PresentationEnemyAction;
  bool vomit=m?.EnemyId=="enemy_sludge_berserker"&&m.MoveId=="vomit";bool gaze=m?.EnemyId=="enemy_elite_mirror_djinn"&&m.MoveId=="gaze";bool net=m?.EnemyId=="enemy_elite_slaver"&&m.MoveId=="net";bool roar=m?.EnemyId=="enemy_gaping_maw"&&m.MoveId=="roar";bool slow=m?.EnemyId=="enemy_maw_hunter"&&m.MoveId=="web";bool junk=m?.EnemyId=="enemy_chomper"&&m.MoveId=="screech";bool growl=m?.EnemyId=="enemy_chimera_3"&&m.MoveId=="burning_growl";bool scald=growl||(m?.EnemyId=="enemy_cinder_imp"&&m.MoveId=="fan_flames")||(m?.EnemyId=="enemy_scald_gnat"&&m.MoveId=="scald_spit");bool mud=m?.EnemyId=="enemy_mudling"&&m.MoveId=="slop";bool wound=vomit||mud||(m?.EnemyId=="enemy_hexer"&&m.MoveId=="curse");int declaredAmount=(growl||vomit||gaze||net)?3:mud?1:2;string status=gaze?"weak":slow?"slow":junk?"junk":scald?"scald":wound?"wound":"vulnerable";
  if(before==null||log==null||m==null||m.Version!=1||m.UnresolvedReason!=null||(!gaze&&!net&&!roar&&!slow&&!junk&&!scald&&!wound&&(m.EnemyId!="enemy_spore_cap"||m.MoveId!="vuln_spores"))||m.Branch!="primary"||m.Origin!="graph"||executing.Kind!="hex"||executing.EnemyIndex<0||executing.EnemyIndex>=before.Enemies.Count||before.Enemies[executing.EnemyIndex].EnemyId!=m.EnemyId||before.Enemies[executing.EnemyIndex].Hp<=0)return null;
  int start=m.ExecutionEventIndex;
  if(start<before.EventLog.Count||start+2>=log.Count||!ReferenceEquals(log[start],executing)||log.Count(e=>ReferenceEquals(e,executing))!=1||m.DeclaredEventIndex is not int di||di<0||di>=start||log[di] is not GameEvent_EnemyIntentDeclared d||d.EnemyIndex!=executing.EnemyIndex||d.Intent is not {Kind:"hex",Inflict:{}}||d.Intent.Inflict.Amount!=declaredAmount||d.Intent.Inflict.Status!=status||d.PresentationMove is not {Version:1,Origin:"graph"} dm||dm.EnemyId!=m.EnemyId||dm.PrimaryMoveId!=m.MoveId||dm.EventIndex!=di)return null;
  for(int i=0;i<before.EventLog.Count;i++)if(!ReferenceEquals(before.EventLog[i],log[i]))return null;
  for(int i=before.EventLog.Count;i<start;i++)if(log[i] is GameEvent_EnemySummoned or GameEvent_EnemySplit or GameEvent_EnemyHatched or GameEvent_CombatStarted or GameEvent_CombatEnded||log[i] is GameEvent_EnemyDied died&&died.EnemyIndex==executing.EnemyIndex||log[i] is GameEvent_EnemyFled fled&&fled.EnemyIndex==executing.EnemyIndex)return null;
  int amount=declaredAmount;
  if(scald){
   // Scald is a lifetime combat quota, not a count of cards in current piles.
   // Include real earlier attack/hex receipts, including spark_toss1. Removing
   // or discarding cards never reopens capacity; zero room has no application.
   if(after==null||!ReferenceEquals(after.EventLog,log))return null;
   int used=before.Player.ScaldsThisCombat.GetValueOrDefault(),usedAfter=after.Player.ScaldsThisCombat.GetValueOrDefault();
   if(used<0||used>5||usedAfter<used||usedAfter>5)return null;
   var additions=log.Skip(before.EventLog.Count).OfType<GameEvent_StatusInflicted>().Where(x=>x.Status=="scald").ToArray();
   if(additions.Any(x=>x.Amount<1||x.Amount>5)||usedAfter-used!=additions.Sum(x=>x.Amount))return null;
   int prior=log.Skip(before.EventLog.Count).Take(start-before.EventLog.Count).OfType<GameEvent_StatusInflicted>().Where(x=>x.Status=="scald").Sum(x=>x.Amount);
   amount=Math.Min(declaredAmount,5-used-prior);if(amount<=0)return null;
  }
  if(junk){
   // Junk counts Hand + Draw + Discard only, unlike lifetime Scald or all-pile Wound.
   // Engine keeps random draw insertion. This is proof only; no card or RNG mutation.
   if(after==null||!ReferenceEquals(after.EventLog,log))return null;
   int room=Effects.CardStatusRoom(before,"junk").GetValueOrDefault();
   if(room<0||room>4)return null;
   var additions=log.Skip(before.EventLog.Count).OfType<GameEvent_StatusInflicted>().Where(x=>x.Status=="junk").ToArray();
   if(additions.Any(x=>x.Amount<1||x.Amount>4)||Effects.CardStatusRoom(after,"junk")!=room-additions.Sum(x=>x.Amount))return null;
   int prior=log.Skip(before.EventLog.Count).Take(start-before.EventLog.Count).OfType<GameEvent_StatusInflicted>().Where(x=>x.Status=="junk").Sum(x=>x.Amount);
   amount=Math.Min(declaredAmount,room-prior);if(amount<=0)return null;
  }
  if(wound){
   // The delta must account exactly for wound cards across every counted pile.
   // This rejects unexplained removal/creation while allowing earlier actual
   // wound receipts to reduce capacity for the current canonical caster.
   if(after==null||!ReferenceEquals(after.EventLog,log))return null;
   int room=Effects.CardStatusRoom(before,"wound").GetValueOrDefault();
   var additions=log.Skip(before.EventLog.Count).OfType<GameEvent_StatusInflicted>().Where(x=>x.Status=="wound").ToArray();
   if(additions.Any(x=>x.Amount<1||x.Amount>(vomit?3:2))||Effects.CardStatusRoom(after,"wound")!=room-additions.Sum(x=>x.Amount))return null;
   int prior=log.Skip(before.EventLog.Count).Take(start-before.EventLog.Count).OfType<GameEvent_StatusInflicted>().Where(x=>x.Status=="wound").Sum(x=>x.Amount);
   amount=Math.Min(declaredAmount,room-prior);if(amount<=0)return null;
  }
  var outcome=log[start+1];bool blocked=!junk&&!scald&&!wound&&outcome is GameEvent_PlayerArtifactBlocked blockedStatus&&blockedStatus.Status==status;
  if(!blocked&&(outcome is not GameEvent_StatusInflicted inflict||inflict.Status!=status||inflict.Amount!=amount))return null;
  if(log[start+2] is not GameEvent_EnemyActionResolved r||r.EnemyIndex!=executing.EnemyIndex||r.Kind!="hex"||r.Actual!=d.Intent.Actual||r.HpLoss!=0||log.Count(e=>ReferenceEquals(e,outcome))!=1||log.Count(e=>ReferenceEquals(e,r))!=1)return null;
  return new(executing,start,outcome,start+1,r,start+2,executing.EnemyIndex,m.EnemyId,m.MoveId,status,blocked?0:amount,blocked);
 }
 public bool IsOutcome(GameEvent e,int index)=>index==OutcomeIndex&&ReferenceEquals(e,Outcome);
 public bool IsApplied(GameEvent e,int index)=>!Blocked&&IsOutcome(e,index);
 public bool IsResolved(GameEvent e,int index)=>index==ResolvedIndex&&ReferenceEquals(e,Resolved);
}

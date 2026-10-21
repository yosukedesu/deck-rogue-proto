using DeckRogue.Engine.Generated;
using DeckRogue.Game;

namespace DeckRogue.GodotPort.Presentation;

// M55/M57/M64 exact-profile read-only authentication of existing execution/cardinality metadata.
// Engine hits and presented contacts are deliberately different quantities.
// No count is recomputed from a declaration-time player state, clamped, or added.
public sealed class MimicAuthoredCountProof
{
 public string ProfileId { get; }
 public string MoveId { get; }
 public int EnemyIndex { get; }
 public int DeclaredEventIndex { get; }
 public int ExecutionEventIndex { get; }
 public int EngineHits { get; }
 public int PresentationContacts { get; }
 public string? PresentationReason { get; }
 public bool Aggregate => EngineHits > PresentationContacts;

 private MimicAuthoredCountProof(EnemyActionPresentation action, GameEvent_DamageDealt hit, EnemyAttackContactPlan plan)
 {
  ProfileId=action.EnemyId; MoveId=action.MoveId!; EnemyIndex=hit.EnemyIndex!.Value;
  DeclaredEventIndex=action.DeclaredEventIndex!.Value; ExecutionEventIndex=action.ExecutionEventIndex;
  EngineHits=hit.PresentationEnemyAttack!.ExecutedHits;
  PresentationContacts=plan.Individual?plan.Count:1;
  PresentationReason=plan.Reason;
 }

 public static bool Supports(string? profile) => profile is "enemy_chimera_2" or "enemy_mimic_imp" or "enemy_mimic_jester" or "enemy_elite_mirror_djinn";
 private static string Move(string profile) => profile=="enemy_chimera_2"?"swelling_claws":profile=="enemy_mimic_imp"?"mimic":profile=="enemy_elite_mirror_djinn"?"mirror":"mirror_dance";

 // Nonregistered calls return null and retain every pre-existing caller's default path.
 // The execution metadata itself is Engine-validated against the latest canonical
 // declaration. Its full record must agree between execution and damage events.
 public static MimicAuthoredCountProof? Capture(GameEvent_EnemyActionExecuting executing, GameEvent candidate, EnemyAttackContactPlan? plan)
 {
  var action=executing.PresentationEnemyAction;
  if(action==null||!Supports(action.EnemyId)||executing.Kind!="attack"||action.Version!=1||
     action.UnresolvedReason!=null||action.Branch!="primary"||action.Origin!="graph"||
     action.MoveId!=Move(action.EnemyId)||action.ExecutionEventIndex<0||
     !action.DeclaredEventIndex.HasValue||action.DeclaredEventIndex.Value<0||
     action.DeclaredEventIndex.Value>=action.ExecutionEventIndex||
     candidate is not GameEvent_DamageDealt hit||hit.Source!="enemy"||
     hit.EnemyIndex!=executing.EnemyIndex||hit.EnemyIndex<0||hit.PresentationEnemyAction!=action||
     plan==null||plan.SourceIndex!=executing.EnemyIndex||plan.SourceId!=action.EnemyId)
   return null;
  var p=hit.PresentationEnemyAttack;
  var receipt=hit.PresentationReceipt;
  if(p==null||p.Version!=1||p.Resolution!="aggregate-final-mutation"||p.EnemyId!=action.EnemyId||
     (action.EnemyId=="enemy_chimera_2"?(p.MirrorHits==true||p.ExecutedHits<3):p.MirrorHits!=true)||p.EffectiveActual<0||p.ExecutedHits<1||p.ExecutedHits!=p.LoopBound||
     plan.ExecutedHits!=p.ExecutedHits||receipt==null||receipt.Target!="player"||
     plan.Amount!=hit.Amount||plan.HpLoss!=hit.HpLoss||plan.Before!=receipt.Before||plan.After!=receipt.After||
     !CombatPresentationLedger.Valid(hit,receipt,out _))
   return null;
  int n=p.ExecutedHits;
  // These are the existing ContactPlan presentation/capture boundaries, never an
  // Engine cap. In particular 13 and 257 retain their exact EngineHits values.
  bool valid;
  if(n==1)
   valid=!plan.Individual&&plan.Count==1&&plan.Reason=="enemy_contact_single"&&
     p.ContributionsOmittedReason==null&&p.Contributions?.Count==n;
  else if(n<=EnemyAttackContactPlan.MaxIndividualContacts)
   valid=plan.Individual&&plan.Count==n&&plan.Reason==null&&
     p.ContributionsOmittedReason==null&&p.Contributions?.Count==n;
  else if(n<=EnemyAttackContactPlan.MaxCapturedContributions)
   valid=!plan.Individual&&plan.Count==n&&plan.Reason=="enemy_contact_presentation_limit"&&
     p.ContributionsOmittedReason==null&&p.Contributions?.Count==n;
  else
   valid=!plan.Individual&&plan.Count==0&&plan.Reason=="presentation_capture_limit"&&
     p.Contributions==null&&p.ContributionsOmittedReason=="presentation_capture_limit";
  return valid?new MimicAuthoredCountProof(action,hit,plan):null;
 }

 public bool Matches(Style04EnemyMotion.Snapshot attack) => attack.Active&&
  attack.ProfileId==ProfileId&&attack.MoveId==MoveId&&attack.ExecutionIndex==ExecutionEventIndex;
}

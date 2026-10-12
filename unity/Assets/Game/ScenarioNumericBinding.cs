using System;
using System.Collections.Generic;

namespace DeckRogue.Game {
 // Explicit display-receipt binding. A visual script never supplies damage values.
 // No default caller/profile is registered; future hosts must prove group membership.
 public sealed class ScenarioNumericBinding {
  readonly CombatPresentationLedger ledger;
  readonly CombatPresentationLedger.Receipt[] receipts;
  readonly int generation;
  readonly HashSet<CombatPresentationLedger.Receipt> ownedCommits=new HashSet<CombatPresentationLedger.Receipt>();
  readonly bool simultaneous,projection;
  readonly Func<EnemyAttackContactPlan.SourceProof> sourceProof;
  bool begun;
  int next;
  internal IReadOnlyList<CombatPresentationLedger.Receipt> ReservedReceipts=>receipts;
  internal bool Available=>!begun && Current;
  public int ContactCount {get;}
  public int CommittedContacts=>next;
  public string Failure {get;private set;}
  public bool Complete=>begun && next==ContactCount;
  public bool Current {
   get {
    if(ledger.Generation!=generation)return false;
    foreach(var r in receipts) {
     if(r.Reason!=null)return false;
     if(r.Committed){if(!ownedCommits.Contains(r))return false;}
     else if(!ledger.Current(r))return false;
    }
    return true;
   }
  }
  ScenarioNumericBinding(CombatPresentationLedger ledger,CombatPresentationLedger.Receipt[] receipts,bool simultaneous,bool projection,Func<EnemyAttackContactPlan.SourceProof> proof) {
   this.ledger=ledger;this.receipts=receipts;this.simultaneous=simultaneous;this.projection=projection;sourceProof=proof;generation=ledger.Generation;
   ContactCount=projection?receipts[0].Projection.Count:simultaneous?1:receipts.Length;
  }
  public static ScenarioNumericBinding ForReceipts(CombatPresentationLedger ledger,IReadOnlyList<CombatPresentationLedger.Receipt> supplied,bool simultaneous) {
   if(ledger==null || supplied==null || supplied.Count==0 || supplied.Count>256)throw new ArgumentException("scenario_numeric_group_invalid");
   var copy=new CombatPresentationLedger.Receipt[supplied.Count];
   for(int i=0;i<copy.Length;i++) {
    copy[i]=supplied[i];
    if(copy[i]?.Values==null || (copy[i].Projection?.Individual==true && copy[i].ProjectionReason==null))throw new ArgumentException("scenario_numeric_projection_requires_explicit_binding");
   }
   if(!ledger.ObserveScenarioReservation(copy,out _,out var reason))throw new ArgumentException(reason);
   return new ScenarioNumericBinding(ledger,copy,simultaneous,false,null);
  }
  public static ScenarioNumericBinding ForEnemyContributions(CombatPresentationLedger ledger,CombatPresentationLedger.Receipt receipt,Func<EnemyAttackContactPlan.SourceProof> proof) {
   if(ledger==null || proof==null || receipt?.Projection==null || !receipt.Projection.Individual || receipt.ProjectionReason!=null || receipt.ProjectionStarted)
    throw new ArgumentException("scenario_real_contributions_unavailable");
   var copy=new[]{receipt};
   if(!ledger.ObserveScenarioReservation(copy,out _,out var reason))throw new ArgumentException(reason);
   return new ScenarioNumericBinding(ledger,copy,false,true,proof);
  }
  public bool Ready {
   get {
    if(begun)return Current;
    if(!Current)return false;
    if(!ledger.ObserveScenarioReservation(receipts,out var ready,out var reason)){Failure=reason;return false;}
    return ready;
   }
  }
  public bool Begin() {
   if(begun)return Current;
   if(!Ready)return false;
   if(projection && !ledger.TryBeginProjection(receipts[0],sourceProof())){Failure="scenario_projection_begin_failed";return false;}
   begun=true;return true;
  }
  // HUD callback follows the successful existing ledger mutation. Presentation
  // follows HUD in the same call; a failure is reported for owner reconciliation.
  public bool Commit(int ordinal,Action<CombatPresentationLedger.Receipt,int> hud,Action<CombatPresentationLedger.Receipt,int> present) {
   if(!begun || !Current || ordinal!=next || ordinal<0 || ordinal>=ContactCount){Failure="scenario_numeric_contact_order";return false;}
   int start=simultaneous?0:projection?0:ordinal,end=simultaneous?receipts.Length:start+1;
   for(int i=start;i<end;i++) {
    var r=receipts[i];
    bool committed=projection?ledger.CommitContribution(r,ordinal,sourceProof()):ledger.Commit(r);
    if(!committed){Failure="scenario_numeric_commit_failed";return false;}
    if(r.Committed)ownedCommits.Add(r);
    try {hud?.Invoke(r,projection?ordinal:-1);present?.Invoke(r,projection?ordinal:-1);}
    catch(Exception e){Failure="scenario_numeric_surface_failed:"+e.GetType().Name;return false;}
   }
   next++;return true;
  }
 }
}

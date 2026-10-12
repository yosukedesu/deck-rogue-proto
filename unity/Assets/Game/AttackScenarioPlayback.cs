using System;
using System.Collections.Generic;

namespace DeckRogue.Game {
 // OPTIONAL stateful adapter. This is deliberately separate from Create/Schedule/Sample.
 // It neither invokes numeric commits nor evaluates rules. The host must recheck all
 // source/target/board leases, commit the existing receipt, then acknowledge synchronously.
 public sealed class AttackScenarioGeneration {
  public long Value { get; private set; }
  public void Reset() { if(Value==long.MaxValue)throw new InvalidOperationException("scenario_generation_exhausted");Value++; }
 }
 public sealed class AttackScenarioContactDue {
  public int Ordinal { get; }
  public double PlannedAt { get; }
  public double ScheduledAt { get; }
  public double ObservedAt { get; }
  public long Frame { get; }
  public double RemainingShift { get; }
  internal AttackScenarioContactDue(int ordinal,double planned,double scheduled,double observed,long frame,double shift) { Ordinal=ordinal;PlannedAt=planned;ScheduledAt=scheduled;ObservedAt=observed;Frame=frame;RemainingShift=shift; }
 }
 public sealed class AttackScenarioPlayback {
  readonly AttackScenarioGeneration generation;
  readonly long capturedGeneration;
  readonly double[] contacts,trackEnds;
  readonly double?[] actual;
  AttackScenarioContactDue pending;
  bool observed;
  double lastObserved;
  long lastObservedFrame,lastContactFrame=-1;
  public AttackScenarioPlan Plan { get; }
  public IReadOnlyList<double> ScheduledContactAt { get; }
  public IReadOnlyList<double> ScheduledTrackEndAt { get; }
  public IReadOnlyList<double?> ActualContactAt { get; }
  public int CommittedCount { get; private set; }
  public bool Cancelled { get; private set; }
  public bool Current=>!Cancelled&&generation.Value==capturedGeneration;
  public bool ContactsComplete=>Current&&CommittedCount==contacts.Length;
  // Predicted support, NOT completion of PresentationWorkScope or GPU work.
  public double EndAt { get; private set; }
  public AttackScenarioPlayback(AttackScenarioPlan plan,AttackScenarioGeneration generation) {
   AttackScenarioCheck.Require(plan!=null,"missing_input",nameof(plan));AttackScenarioCheck.Require(generation!=null,"missing_input",nameof(generation));
   Plan=plan;this.generation=generation;capturedGeneration=generation.Value;
   contacts=new double[plan.ContactAt.Count];actual=new double?[contacts.Length];
   for(int i=0;i<contacts.Length;i++)contacts[i]=plan.ContactAt[i];
   trackEnds=new double[plan.Script.Tracks.Count];for(int i=0;i<trackEnds.Length;i++)trackEnds[i]=AttackScenarioCheck.Absolute(plan.FirstContactAt+plan.Script.Tracks[i].End,"trackEnd");
   ScheduledContactAt=Array.AsReadOnly(contacts);ScheduledTrackEndAt=Array.AsReadOnly(trackEnds);ActualContactAt=Array.AsReadOnly(actual);EndAt=plan.EndAt;
  }
  public void Cancel() { Cancelled=true;pending=null; }
  public bool TryGetDue(double now,long frame,out AttackScenarioContactDue due) {
   due=null;AttackScenarioCheck.Absolute(now,nameof(now));AttackScenarioCheck.Require(frame>=0,"negative_frame",nameof(frame));
   if(!Current)return false;
   AttackScenarioCheck.Require(!observed||(now>=lastObserved&&frame>=lastObservedFrame),"clock_regression",nameof(now));
   if(pending!=null) {
    AttackScenarioCheck.Require(pending.ObservedAt==now&&pending.Frame==frame,"unacknowledged_contact","pending");due=pending;return true;
   }
   observed=true;lastObserved=now;lastObservedFrame=frame;
   if(CommittedCount==contacts.Length||frame<=lastContactFrame||now<contacts[CommittedCount])return false;
   int i=CommittedCount;double late=now-contacts[i];
   // First actual contact anchors the remaining train. Later normal frame rounding
   // does not accumulate. A stall longer than the real preceding gap shifts ONLY
   // uncommitted contacts and their still-supported tails, never produces more hits.
   double shift=i==0?late:late>Plan.Script.ContactOffsets[i]-Plan.Script.ContactOffsets[i-1]?late:0;
   for(int j=i;j<contacts.Length;j++) {
    double next=AttackScenarioCheck.Absolute(contacts[j]+shift,"retimedContact");
    if(j>i)AttackScenarioCheck.Require(next>contacts[j-1]+shift,"contact_precision_lost","retimedContact");
   }
   for(int j=0;j<trackEnds.Length;j++)if(Plan.Script.Tracks[j].End>Plan.Script.Tracks[j].Start&&Plan.Script.Tracks[j].End>=Plan.Script.ContactOffsets[i])AttackScenarioCheck.Absolute(trackEnds[j]+shift,"retimedTrackEnd");
   pending=new AttackScenarioContactDue(i,Plan.ContactAt[i],contacts[i]+shift,now,frame,shift);due=pending;return true;
  }
  // Call only after the corresponding real receipt/contribution succeeded, in the
  // same frame. On numeric failure or a lost dependency/lease, Cancel and reconcile.
  public bool AcknowledgeCommitted(AttackScenarioContactDue due) {
   if(!Current||due==null||!ReferenceEquals(due,pending)||due.Ordinal!=CommittedCount)return false;
   int i=CommittedCount;
   for(int j=i;j<contacts.Length;j++)contacts[j]+=due.RemainingShift;
   for(int j=0;j<trackEnds.Length;j++)if(Plan.Script.Tracks[j].End>Plan.Script.Tracks[j].Start&&Plan.Script.Tracks[j].End>=Plan.Script.ContactOffsets[i])trackEnds[j]+=due.RemainingShift;
   actual[i]=due.ObservedAt;lastContactFrame=due.Frame;CommittedCount++;pending=null;
   EndAt=Math.Max(contacts[contacts.Length-1],due.ObservedAt);for(int j=0;j<trackEnds.Length;j++)if(Plan.Script.Tracks[j].End>Plan.Script.Tracks[j].Start)EndAt=Math.Max(EndAt,trackEnds[j]);
   return true;
  }
 }
}

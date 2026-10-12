using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DeckRogue.Game {
 // Candidate-independent clock. No profile registry, rules, random source, Unity or callbacks.
 public enum AttackScenarioPhase { Anticipation, Charge, Release, Impact, Afterglow }
 public enum AttackScenarioTrackKind { Main, Secondary, Particles, SceneLight }
 public sealed class AttackScenarioValidationException : ArgumentException {
  public string Code { get; }
  public AttackScenarioValidationException(string code,string field) : base(code+": "+field,field) { Code=code; }
 }
 public static class AttackScenarioLimits {
  // Technical rejection limits, NOT authored durations or gameplay defaults.
  public const int MaxContacts=4096, MaxTracks=1024, MaxBatchEntries=4096, MaxIdLength=128;
  public const double MaxRelativeSeconds=3600, MaxAbsoluteSeconds=1000000000;
 }
 internal static class AttackScenarioCheck {
  internal static void Require(bool condition,string code,string field) { if(!condition)throw new AttackScenarioValidationException(code,field); }
  internal static double Number(double value,double limit,string field) {
   Require(!double.IsNaN(value)&&!double.IsInfinity(value),"nonfinite",field);
   Require(Math.Abs(value)<=limit,"time_out_of_bounds",field); return value;
  }
  internal static double Relative(double value,string field)=>Number(value,AttackScenarioLimits.MaxRelativeSeconds,field);
  internal static double Absolute(double value,string field)=>Number(value,AttackScenarioLimits.MaxAbsoluteSeconds,field);
  internal static double Nonnegative(double value,string field) { Relative(value,field);Require(value>=0,"negative_time",field);return value; }
  internal static ReadOnlyCollection<T> Freeze<T>(IReadOnlyList<T> source,int min,int max,string field) {
   Require(source!=null,"missing_input",field);Require(source.Count>=min&&source.Count<=max,"count_out_of_bounds",field);
   var copy=new T[source.Count];for(int i=0;i<copy.Length;i++)copy[i]=source[i];return Array.AsReadOnly(copy);
  }
 }
 public readonly struct AttackScenarioPhaseInterval {
  public AttackScenarioPhase Phase { get; }
  public double Start { get; }
  public double End { get; }
  public AttackScenarioPhaseInterval(AttackScenarioPhase phase,double start,double end) { Phase=phase;Start=start;End=end; }
 }
 public readonly struct AttackScenarioTrackInterval {
  public AttackScenarioTrackKind Kind { get; }
  public double Start { get; }
  public double End { get; }
  public AttackScenarioTrackInterval(AttackScenarioTrackKind kind,double start,double end) { Kind=kind;Start=start;End=end; }
 }
 public sealed class AttackScenarioScript {
  public string Id { get; }
  public IReadOnlyList<AttackScenarioPhaseInterval> Phases { get; }
  public IReadOnlyList<AttackScenarioTrackInterval> Tracks { get; }
  public IReadOnlyList<double> ContactOffsets { get; }
  public int ActualContactCount=>ContactOffsets.Count;
  public double ActorLead { get; }
  public double NativeLead { get; }
  public double VisibleEnd { get; }
  AttackScenarioScript(string id,ReadOnlyCollection<AttackScenarioPhaseInterval> phases,ReadOnlyCollection<AttackScenarioTrackInterval> tracks,ReadOnlyCollection<double> contacts,double actorLead,double visibleEnd) {
   Id=id;Phases=phases;Tracks=tracks;ContactOffsets=contacts;ActorLead=actorLead;NativeLead=Math.Max(0,-phases[0].Start);VisibleEnd=visibleEnd;
  }
  // actualContactCount is supplied from proven receipts/contributions, never from visual pieces.
  public static AttackScenarioScript Create(string id,IReadOnlyList<AttackScenarioPhaseInterval> phases,IReadOnlyList<AttackScenarioTrackInterval> tracks,IReadOnlyList<double> contactOffsets,int actualContactCount,double actorLead) {
   AttackScenarioCheck.Require(!string.IsNullOrWhiteSpace(id)&&id.Length<=AttackScenarioLimits.MaxIdLength,"invalid_id",nameof(id));
   var p=AttackScenarioCheck.Freeze(phases,5,5,nameof(phases));
   var t=AttackScenarioCheck.Freeze(tracks,0,AttackScenarioLimits.MaxTracks,nameof(tracks));
   var c=AttackScenarioCheck.Freeze(contactOffsets,1,AttackScenarioLimits.MaxContacts,nameof(contactOffsets));
   AttackScenarioCheck.Require(actualContactCount>=1&&actualContactCount<=AttackScenarioLimits.MaxContacts&&actualContactCount==c.Count,"contact_count_mismatch",nameof(actualContactCount));
   AttackScenarioCheck.Nonnegative(actorLead,nameof(actorLead));
   for(int i=0;i<5;i++) {
    AttackScenarioCheck.Require((int)p[i].Phase==i,"phase_order","phases["+i+"]");
    AttackScenarioCheck.Relative(p[i].Start,"phase.start");AttackScenarioCheck.Relative(p[i].End,"phase.end");
   }
   // Semantic phases may overlap. Their labels and starts retain scenario order;
   // individual ends may decrease, but Afterglow.End bounds every phase.
   for(int i=0;i<5;i++) {
    AttackScenarioCheck.Require(p[i].End>=p[i].Start&&(i==0||p[i].Start>=p[i-1].Start)&&p[i].End<=p[4].End,"phase_support","phases["+i+"]");
   }
   AttackScenarioCheck.Require(p[0].Start<=0,"missing_formation_support",nameof(phases));
   for(int i=0;i<c.Count;i++) {
    AttackScenarioCheck.Nonnegative(c[i],"contactOffsets["+i+"]");
    AttackScenarioCheck.Require(i==0?c[i]==0:c[i]>c[i-1],"contact_order",nameof(contactOffsets));
   }
   AttackScenarioCheck.Require(p[3].Start<=0&&c[c.Count-1]<=p[3].End,"impact_omits_contact",nameof(phases));
   double end=c[c.Count-1];
   for(int i=0;i<t.Count;i++) {
    var track=t[i];AttackScenarioCheck.Require((int)track.Kind>=0&&(int)track.Kind<=3,"unsupported_track","tracks["+i+"]");
    AttackScenarioCheck.Relative(track.Start,"track.start");AttackScenarioCheck.Relative(track.End,"track.end");
    AttackScenarioCheck.Require(track.End>=track.Start&&track.Start>=p[0].Start&&track.End<=p[4].End,"track_support","tracks["+i+"]");
    if(track.End>track.Start)end=Math.Max(end,track.End);
   }
   return new AttackScenarioScript(id,p,t,c,actorLead,end);
  }
 }
 public readonly struct AttackScenarioSample {
  public double Age { get; }
  public int PhaseMask { get; }
  public int TrackMask { get; }
  // A diagnostic count, never an instruction to drain/commit contacts in one frame.
  public int ContactsDue { get; }
  public bool VisibleComplete { get; }
  internal AttackScenarioSample(double age,int phases,int tracks,int contacts,bool complete) { Age=age;PhaseMask=phases;TrackMask=tracks;ContactsDue=contacts;VisibleComplete=complete; }
  public bool IsPhaseActive(AttackScenarioPhase phase) { AttackScenarioCheck.Require((int)phase>=0&&(int)phase<=4,"unsupported_phase",nameof(phase));return (PhaseMask&(1<<(int)phase))!=0; }
  public bool IsTrackActive(AttackScenarioTrackKind kind) { AttackScenarioCheck.Require((int)kind>=0&&(int)kind<=3,"unsupported_track",nameof(kind));return (TrackMask&(1<<(int)kind))!=0; }
 }
 public sealed class AttackScenarioPlan {
  public AttackScenarioScript Script { get; }
  public double FormationAt { get; }
  public double ActorCueAt { get; }
  public IReadOnlyList<double> ContactAt { get; }
  public double FirstContactAt=>ContactAt[0];
  public double EndAt { get; }
  internal AttackScenarioPlan(AttackScenarioScript script,double land,double actorCueAt) {
   Script=script;FormationAt=AttackScenarioCheck.Absolute(land-script.NativeLead,"FormationAt");
   ActorCueAt=AttackScenarioCheck.Absolute(actorCueAt,"ActorCueAt");
   EndAt=AttackScenarioCheck.Absolute(land+script.VisibleEnd,"EndAt");
   var contacts=new double[script.ActualContactCount];
   for(int i=0;i<contacts.Length;i++) {
    contacts[i]=AttackScenarioCheck.Absolute(land+script.ContactOffsets[i],"ContactAt");
    AttackScenarioCheck.Require(i==0||contacts[i]>contacts[i-1],"contact_precision_lost","ContactAt");
   }
   ContactAt=Array.AsReadOnly(contacts);
  }
  public static AttackScenarioPlan Schedule(AttackScenarioScript script,double now,double dependencyReady,double legacyLand) {
   AttackScenarioCheck.Require(script!=null,"missing_input",nameof(script));
   AttackScenarioCheck.Absolute(now,nameof(now));AttackScenarioCheck.Absolute(dependencyReady,nameof(dependencyReady));AttackScenarioCheck.Absolute(legacyLand,nameof(legacyLand));
   double ready=Math.Max(now,dependencyReady),land=Math.Max(legacyLand,Math.Max(ready+script.NativeLead,ready+script.ActorLead));
   return new AttackScenarioPlan(script,land,land-script.ActorLead);
  }
  internal static bool Active(double age,double start,double end)=>age>=start&&(age<end||(start==end&&age==start));
  public AttackScenarioSample Sample(double now) {
   AttackScenarioCheck.Absolute(now,nameof(now));double age=now-FirstContactAt;int phaseMask=0,trackMask=0,due=0;
   foreach(var p in Script.Phases)if(Active(age,p.Start,p.End))phaseMask|=1<<(int)p.Phase;
   foreach(var t in Script.Tracks)if(age>=t.Start&&age<t.End)trackMask|=1<<(int)t.Kind;
   foreach(var at in ContactAt)if(now>=at)due++;
   return new AttackScenarioSample(age,phaseMask,trackMask,due,now>=EndAt);
  }
 }
 public readonly struct AttackScenarioBatchEntry {
  public int EventIndex { get; }
  public AttackScenarioScript Script { get; }
  public double LegacyContactOffset { get; }
  public AttackScenarioBatchEntry(int eventIndex,AttackScenarioScript script,double legacyContactOffset) { EventIndex=eventIndex;Script=script;LegacyContactOffset=legacyContactOffset; }
 }
 public readonly struct AttackScenarioScheduledEntry {
  public int EventIndex { get; }
  public AttackScenarioPlan Plan { get; }
  internal AttackScenarioScheduledEntry(int eventIndex,AttackScenarioPlan plan) { EventIndex=eventIndex;Plan=plan; }
 }
 public sealed class AttackScenarioBatchPlan {
  public IReadOnlyList<AttackScenarioScheduledEntry> Entries { get; }
  public double CommonShift { get; }
  public double ActorCueAt { get; }
  public double EndAt { get; }
  AttackScenarioBatchPlan(AttackScenarioScheduledEntry[] entries,double shift,double actorCueAt,double endAt) { Entries=Array.AsReadOnly(entries);CommonShift=shift;ActorCueAt=actorCueAt;EndAt=endAt; }
  // Group membership and event order MUST come from the existing proven volley plan.
  // actorLead is one host choreography lead. All scripts must agree with it.
  public static AttackScenarioBatchPlan Schedule(IReadOnlyList<AttackScenarioBatchEntry> entries,double now,double dependencyReady,double legacyStart,double actorLead,double legacyFirstContactOffset) {
   var rows=AttackScenarioCheck.Freeze(entries,1,AttackScenarioLimits.MaxBatchEntries,nameof(entries));
   AttackScenarioCheck.Absolute(now,nameof(now));AttackScenarioCheck.Absolute(dependencyReady,nameof(dependencyReady));AttackScenarioCheck.Absolute(legacyStart,nameof(legacyStart));
   AttackScenarioCheck.Nonnegative(actorLead,nameof(actorLead));AttackScenarioCheck.Nonnegative(legacyFirstContactOffset,nameof(legacyFirstContactOffset));
   double ready=Math.Max(now,dependencyReady),shift=Math.Max(0,ready-legacyStart+actorLead-legacyFirstContactOffset);
   for(int i=0;i<rows.Count;i++) {
    var r=rows[i];AttackScenarioCheck.Require(r.Script!=null,"missing_input","entry.script");
    AttackScenarioCheck.Require(r.EventIndex>=0&&(i==0||r.EventIndex>rows[i-1].EventIndex),"group_event_order",nameof(entries));
    AttackScenarioCheck.Nonnegative(r.LegacyContactOffset,"entry.legacyContactOffset");
    AttackScenarioCheck.Require(r.Script.ActorLead==actorLead,"group_actor_lead_mismatch",nameof(actorLead));
    if(i==0)AttackScenarioCheck.Require(r.LegacyContactOffset==legacyFirstContactOffset,"group_first_contact_mismatch",nameof(legacyFirstContactOffset));
    else { var previous=rows[i-1];AttackScenarioCheck.Require(previous.LegacyContactOffset+previous.Script.ContactOffsets[previous.Script.ActualContactCount-1]<=r.LegacyContactOffset,"group_contact_order",nameof(entries)); }
    shift=Math.Max(shift,ready-legacyStart+r.Script.NativeLead-r.LegacyContactOffset);
   }
   AttackScenarioCheck.Absolute(shift,"CommonShift");double start=AttackScenarioCheck.Absolute(legacyStart+shift,"shiftedStart");
   double actorCue=AttackScenarioCheck.Absolute(start+legacyFirstContactOffset-actorLead,"ActorCueAt"),end=double.MinValue;
   var result=new AttackScenarioScheduledEntry[rows.Count];
   for(int i=0;i<rows.Count;i++) {
    var p=new AttackScenarioPlan(rows[i].Script,start+rows[i].LegacyContactOffset,actorCue);
    if(i>0) {
     var previous=rows[i-1];double previousLast=previous.LegacyContactOffset+previous.Script.ContactOffsets[previous.Script.ActualContactCount-1];
     var previousPlan=result[i-1].Plan;double previousAt=previousPlan.ContactAt[previousPlan.ContactAt.Count-1];
     AttackScenarioCheck.Require(p.FirstContactAt>=previousAt&&(rows[i].LegacyContactOffset==previousLast||p.FirstContactAt>previousAt),"contact_precision_lost","batchContact");
    }
    result[i]=new AttackScenarioScheduledEntry(rows[i].EventIndex,p);end=Math.Max(end,p.EndAt);
   }
   return new AttackScenarioBatchPlan(result,shift,actorCue,end);
  }
 }
}

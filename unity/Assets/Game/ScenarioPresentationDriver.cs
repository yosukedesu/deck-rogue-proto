using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DeckRogue.Game {
 // Dormant reusable host. No art/profile registry or default Presenter route opts in.
 // The caller owns captured board/actor/target leases and the existing numeric HUD.
 public static class ScenarioPresentationDriver {
  public sealed class Callbacks {
   public Func<bool> IsCurrent;
   // Shared actor pose/pause ownership is separate from numeric dependency order.
   public Func<bool> ReadyToBegin;
   public Func<Frame,bool> Prepare;
   public Action<Frame> ActorCue,Draw;
   public Action<Frame,CombatPresentationLedger.Receipt,int> Hud,Present;
   public Func<bool> VisualsIdle;
   public Action Completed;
   // Cleanup must be version-owned. Reconcile only if the host's board is current.
   public Action<string> Cancelled;
  }
  public sealed class Frame {
   public AttackScenarioPlan Plan {get;internal set;}
   public AttackScenarioContactDue Contact {get;internal set;}
   public double Now {get;internal set;}
   public double? FirstActualLand {get;internal set;}
   public double Age=>Now-(FirstActualLand??Plan.FirstContactAt);
   public AttackScenarioSample Sample=>Plan.Sample(Plan.FirstContactAt+Age);
  }
  public sealed class Handle {
   internal readonly Callbacks Host;
   internal readonly ScenarioNumericBinding Binding;
   internal readonly AttackScenarioGeneration Generation=new AttackScenarioGeneration();
   internal AttackScenarioPlayback Playback;
   internal bool Prepared,ActorStarted;
   public Frame Frame {get;}=new Frame();
   public bool Completed {get;internal set;}
   public bool Cancelled {get;private set;}
   public string Failure {get;private set;}
   public int CommittedContacts=>Binding.CommittedContacts;
   public double EndAt=>Playback?.EndAt??double.NaN;
   internal Handle(ScenarioNumericBinding binding,Callbacks host){Binding=binding;Host=host;}
   public void Cancel(string reason="scenario_cancelled") {
    if(Completed || Cancelled)return;
    Cancelled=true;Failure=reason;Generation.Reset();Playback?.Cancel();Release(this);
    try{Host.Cancelled(reason);}catch(Exception e){Debug.LogWarning("[Scenario] cleanup "+e.Message);}
   }
  }
  static readonly HashSet<Handle> Active=new HashSet<Handle>();
  static readonly Dictionary<CombatPresentationLedger.Receipt,Handle> Claims=new Dictionary<CombatPresentationLedger.Receipt,Handle>();
  static void Release(Handle h) {
   Active.Remove(h);
   foreach(var r in h.Binding.ReservedReceipts)if(Claims.TryGetValue(r,out var owner)&&ReferenceEquals(owner,h))Claims.Remove(r);
  }
  public const int MaxConcurrent=64;
  public static int ActiveCount=>Active.Count;
  public static void CancelAll(string reason="presentation_cancelled") {
   if(Active.Count==0)return;
   var owned=new Handle[Active.Count];Active.CopyTo(owned);
   foreach(var h in owned)h.Cancel(reason);
  }
  public static Handle Run(AttackScenarioScript script,double legacyLand,ScenarioNumericBinding binding,Callbacks host) {
   if(script==null || binding==null || host==null || host.IsCurrent==null || host.ReadyToBegin==null || host.Prepare==null || host.ActorCue==null || host.Draw==null
      || host.Hud==null || host.Present==null || host.VisualsIdle==null || host.Cancelled==null)throw new ArgumentException("scenario_host_incomplete");
   if(script.ActualContactCount!=binding.ContactCount)throw new ArgumentException("scenario_real_contact_count_mismatch");
   if(double.IsNaN(legacyLand)||double.IsInfinity(legacyLand))throw new ArgumentException("scenario_legacy_clock_nonfinite");
   var h=new Handle(binding,host);
   if(!binding.Available){h.Cancel("scenario_binding_unavailable");return h;}
   if(!Tween.SchedulingAllowed){h.Cancel("scenario_scope_cancelled");return h;}
   if(Active.Count>=MaxConcurrent){h.Cancel("scenario_host_capacity");return h;}
   foreach(var r in binding.ReservedReceipts)if(Claims.ContainsKey(r)){h.Cancel("scenario_receipt_already_owned");return h;}
   Active.Add(h);foreach(var r in binding.ReservedReceipts)Claims.Add(r,h);
   // Own the entire dependency wait, formation, contact train and visible tail.
   // Descendant render/lighting work inherits this same existing scope.
   using(Tween.BeginPresentation())Tween.RunPresentation(SafeLife(h,script,legacyLand));
   return h;
  }
  static bool Current(Handle h)=>!h.Cancelled && !h.Completed && h.Binding.Current && h.Host.IsCurrent();
  static IEnumerator SafeLife(Handle h,AttackScenarioScript script,double legacyLand) {
   var routine=Life(h,script,legacyLand);
   try {
    while(true) {
     bool more;
     try{more=routine.MoveNext();}
     catch(Exception e){h.Cancel("scenario_callback_failed:"+e.GetType().Name);yield break;}
     if(!more)yield break;
     yield return routine.Current;
    }
   } finally {(routine as IDisposable)?.Dispose();}
  }
  static IEnumerator Life(Handle h,AttackScenarioScript script,double legacyLand) {
   try {
    while(Current(h) && (!h.Binding.Ready || !h.Host.ReadyToBegin()))yield return null;
    if(!Current(h)){h.Cancel("scenario_owner_invalid_before_formation");yield break;}
    if(!h.Binding.Begin()){h.Cancel(h.Binding.Failure??"scenario_dependency_begin_failed");yield break;}
    h.Frame.Plan=AttackScenarioPlan.Schedule(script,Time.time,Time.time,legacyLand);
    h.Playback=new AttackScenarioPlayback(h.Frame.Plan,h.Generation);
    double lastAge=double.NegativeInfinity,lastNow=double.NegativeInfinity;
    while(Current(h)) {
     double now=Time.time;h.Frame.Now=now;
     if(double.IsNaN(now)||double.IsInfinity(now)||now<lastNow){h.Cancel("scenario_host_clock_regression");yield break;}
     lastNow=now;
     if(!h.ActorStarted && now>=h.Frame.Plan.ActorCueAt) {
      // A skipped cue frame cannot cause an immediate impact without its actor
      // lead. Before contact only, shift the remaining clock once. The previous
      // rendered age was earlier than -ActorLead, so this cannot rewind flow.
      double late=now-h.Frame.Plan.ActorCueAt;
      if(late>0) {
       var p=h.Frame.Plan;
       h.Frame.Plan=new AttackScenarioPlan(script,p.FirstContactAt+late,now);
       h.Playback=new AttackScenarioPlayback(h.Frame.Plan,h.Generation);
      }
      h.ActorStarted=true;h.Host.ActorCue(h.Frame);
      if(!Current(h))yield break;
     }
     if(!h.Prepared && now>=h.Frame.Plan.FormationAt) {
      if(!h.Host.Prepare(h.Frame)){h.Cancel("scenario_visual_prepare_failed");yield break;}
      h.Prepared=true;
      if(!Current(h))yield break;
     }
     if(h.Playback.TryGetDue(now,Time.frameCount,out var due)) {
      if(!h.Prepared || !h.ActorStarted){h.Cancel("scenario_contact_without_preparation");yield break;}
      if(due.Ordinal==0)h.Frame.FirstActualLand=now;
      h.Frame.Contact=due;
      if(!h.Binding.Commit(due.Ordinal,(r,i)=>{
       if(!Current(h))throw new InvalidOperationException("scenario_hud_owner_lost");
       h.Host.Hud(h.Frame,r,i);
      },(r,i)=>{
       if(!Current(h))throw new InvalidOperationException("scenario_impact_owner_lost");
       h.Host.Present(h.Frame,r,i);
       if(!Current(h))throw new InvalidOperationException("scenario_postimpact_owner_lost");
      })) {
       h.Cancel(h.Binding.Failure??"scenario_numeric_contact_failed");yield break;
      }
      if(!h.Playback.AcknowledgeCommitted(due)){h.Cancel("scenario_contact_ack_failed");yield break;}
      h.Frame.Contact=null;
     }
     if(h.Prepared) {
      if(h.Frame.Age+1e-7<lastAge){h.Cancel("scenario_material_clock_rewind");yield break;}
      lastAge=h.Frame.Age;
      // Contact commit/HUD/impact occurred above before any nonnegative sample.
      h.Host.Draw(h.Frame);
      if(!Current(h))yield break;
     }
     if(h.Playback.ContactsComplete && now>=h.Playback.EndAt && h.Host.VisualsIdle()) {
      h.Completed=true;Release(h);h.Host.Completed?.Invoke();yield break;
     }
     yield return null;
    }
   } finally {if(!h.Completed)h.Cancel(h.Failure??"scenario_cancelled_or_owner_lost");}
  }
 }
}

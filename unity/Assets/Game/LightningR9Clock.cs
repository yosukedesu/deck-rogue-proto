using System;
namespace DeckRogue.Game {
 // Stage42 rendering-only adapter. PlayTracked/ContactCo remain the sole actor,
 // sound, receipt, HUD and action-queue owners. Never schedules a second contact.
 internal sealed class LightningR9Clock {
  internal const double NativeLead=.14,VisibleEnd=.26;
  internal static readonly AttackScenarioScript Script=AttackScenarioScript.Create("lightning-r9-medium-candidate",
   new[]{new AttackScenarioPhaseInterval(AttackScenarioPhase.Anticipation,-.14,-.10),new AttackScenarioPhaseInterval(AttackScenarioPhase.Charge,-.10,-1.0/30),new AttackScenarioPhaseInterval(AttackScenarioPhase.Release,-1.0/30,1.0/60),new AttackScenarioPhaseInterval(AttackScenarioPhase.Impact,0,.045),new AttackScenarioPhaseInterval(AttackScenarioPhase.Afterglow,1.0/60,VisibleEnd)},
   new[]{new AttackScenarioTrackInterval(AttackScenarioTrackKind.Main,-.14,VisibleEnd),new AttackScenarioTrackInterval(AttackScenarioTrackKind.Secondary,0,VisibleEnd),new AttackScenarioTrackInterval(AttackScenarioTrackKind.SceneLight,0,VisibleEnd)},new[]{0.0},1,0);
  internal readonly AttackScenarioPlan Plan;internal bool Landed{get;private set;}internal double ActualLand{get;private set;}
  internal LightningR9Clock(double launch,double legacyContact){
   if(!Finite(launch)||!Finite(legacyContact)||legacyContact-launch+1e-7<NativeLead)throw new ArgumentException("r9_insufficient_legacy_lead");
   Plan=AttackScenarioPlan.Schedule(Script,launch,launch,legacyContact);
   if(Math.Abs(Plan.FirstContactAt-legacyContact)>1e-6)throw new ArgumentException("r9_would_retime_legacy_contact");
  }
  internal bool Land(double now){if(!Finite(now))return false;if(!Landed){ActualLand=now;Landed=true;}return true;}
  internal double Age(double now){if(!Finite(now))throw new ArgumentOutOfRangeException(nameof(now));return Landed?Math.Max(0,now-ActualLand):Math.Min(-1e-7,now-Plan.FirstContactAt);}
  internal static bool Finite(double x)=>!double.IsNaN(x)&&!double.IsInfinity(x);
 }
 internal sealed class LightningR9Binding {
  internal LightningR9Clock Clock;internal Func<bool> BeforeContact,AfterCommit,ResidueCurrent;
 }
}

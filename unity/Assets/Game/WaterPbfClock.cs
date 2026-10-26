using System;
namespace DeckRogue.Game {
 // Rendering-only adapter. The legacy Presenter remains the sole contact/HUD owner.
 internal sealed class WaterPbfClock {
  internal const double Contact=.0875,MainEnd=.65,End=.85,Residue=End-Contact;
  internal static readonly AttackScenarioScript Script=AttackScenarioScript.Create("water-pbf-r4-candidate",
   new[]{new AttackScenarioPhaseInterval(AttackScenarioPhase.Anticipation,-Contact,-Contact),
    new AttackScenarioPhaseInterval(AttackScenarioPhase.Charge,-Contact,-Contact),
    new AttackScenarioPhaseInterval(AttackScenarioPhase.Release,-Contact,.10),
    new AttackScenarioPhaseInterval(AttackScenarioPhase.Impact,0,.2125),
    new AttackScenarioPhaseInterval(AttackScenarioPhase.Afterglow,.2125,Residue)},
   new[]{new AttackScenarioTrackInterval(AttackScenarioTrackKind.Main,-Contact,Residue),
    new AttackScenarioTrackInterval(AttackScenarioTrackKind.Particles,-Contact,Residue),
    new AttackScenarioTrackInterval(AttackScenarioTrackKind.SceneLight,.0125,.3425)},new[]{0.0},1,.240);
  internal readonly AttackScenarioPlan Plan;internal bool Landed{get;private set;}internal double ActualLand{get;private set;}
  internal WaterPbfClock(double launch,double legacyContact){Plan=AttackScenarioPlan.Schedule(Script,launch,launch,legacyContact);}
  internal bool Land(double now){if(!Finite(now))return false;if(!Landed){ActualLand=now;Landed=true;}return true;}
  // Uncommitted contact never exposes tick21 or subsequent foam/impact frames.
  internal double Source(double now){if(!Finite(now))throw new ArgumentOutOfRangeException(nameof(now));return Landed?Contact+Plan.Sample(Plan.FirstContactAt+Math.Max(0,now-ActualLand)).Age:Math.Min(Contact-1e-7,Contact+now-Plan.FirstContactAt);}
  internal bool Complete(double now)=>Landed&&Source(now)>=End;
  internal static int Tick(double source)=>Math.Max(0,Math.Min(156,(int)Math.Floor(Math.Max(0,source)*240+1e-8)));
  internal static double Fade(double source){double q=Math.Max(0,Math.Min(1,(source-MainEnd)/(End-MainEnd)));return source>=End?0:1-q*q*(3-2*q);}
  internal static bool Finite(double x)=>!double.IsNaN(x)&&!double.IsInfinity(x);
 }
 internal sealed class WaterPbfBinding {
  internal WaterPbfClock Clock;internal Func<bool> BeforeContact,AfterCommit,ResidueCurrent;
 }
}

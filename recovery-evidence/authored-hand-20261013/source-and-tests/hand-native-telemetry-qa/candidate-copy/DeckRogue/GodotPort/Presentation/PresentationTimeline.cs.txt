#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine.Generated;
using DeckRogue.Game;
namespace DeckRogue.GodotPort.Presentation;


/// <summary>Owns presentation time only. Never changes Engine.TimeScale or mutates engine snapshots.</summary>
public sealed class PresentationTimeline
{
 readonly Queue<PresentationBeat> _pending = new();
 public PresentationBeat? Active {get; private set;}
 public float Time {get; private set;}
 public bool ContactDelivered {get; private set;}
 public int ContactOrdinal {get;private set;}
 public float LastContactTime {get;private set;}
 public float LastAdvanceDelta {get;private set;}
 float _retimed;
 public Func<GameEvent_DamageDealt,EnemyAttackContactPlan?>? ContactPlanner {get;set;}
 public bool Busy => Active is not null || _pending.Count > 0;
 public int PendingCount => _pending.Count;
 public long Epoch {get;private set;}
 public int CompletedBeats {get;private set;}
 public event Action<PresentationBeat>? Started;
 public event Action<PresentationBeat>? Contact;
 public event Action<PresentationBeat>? Finished;
 public event Action? Drained;
 public event Action<float>? TimeAdvanced;
 public void Reset() { _pending.Clear();Active=null;Time=0;ContactDelivered=false;Epoch++; }
 public void Enqueue(IReadOnlyList<GameEvent> events, AttackRouteCatalog routes) {
  bool won=events.OfType<GameEvent_CombatEnded>().Any(e=>e.Result=="won");
  int finalHit=won ? Enumerable.Range(0,events.Count).LastOrDefault(i=>events[i] is GameEvent_DamageDealt h && h.HpLoss>0,-1) : -1;
  for(int i=0;i<events.Count;i++) {
   var e=events[i];
   AttackRoute? route=null; EffectScenario? scenario=null;
   float duration=DurationFor(e), contact=0;EnemyAttackContactPlan? contacts=null;
   if(e is GameEvent_DamageDealt hit) {route=hit.HpLoss>0?routes.Resolve(hit):new AttackRoute("system:outcome","","system",CombatOutcome.Read(hit).Kind??"nullified","SystemOutcomeFx","neutral",true,"source outcome path");scenario=EffectScenario.For(route,hit.HpLoss,hit.PresentationReceipt?.Before.Hp,hit.PresentationReceipt?.Before.MaxHp);duration=scenario.Duration;contact=scenario.Windup;contacts=ContactPlanner?.Invoke(hit);if(contacts?.Individual==true)duration+=contacts.Span;}
   else if(e is GameEvent_ConfusedAttack confused) {route=new AttackRoute("system:confused","","system",confused.HpLoss>0?"minimal_neutral_contact":CombatOutcome.Read(confused).Kind??"nullified",confused.HpLoss>0?"BoundedContactFx":"SystemOutcomeFx","neutral",true,"explicit confused target");scenario=EffectScenario.For(route,confused.HpLoss,confused.PresentationReceipt?.Before.Hp,confused.PresentationReceipt?.Before.MaxHp);duration=scenario.Duration;contact=scenario.Windup;}
   else if(CombatOutcome.PositiveSystemFamily(e,out _) is string sys) {route=new AttackRoute("system:"+sys,"","system",sys,"SystemOutcomeFx",sys=="burn"?"fire":"neutral",true,"validated system loss");scenario=EffectScenario.For(route,1);duration=scenario.Duration;contact=scenario.Windup;}
   else if(e is GameEvent_HpHealed {Amount:>0} or GameEvent_EnemyHealed {Amount:>0} or GameEvent_RegenTicked {Amount:>0}) {int recovered=e is GameEvent_HpHealed h?h.Amount:e is GameEvent_EnemyHealed eh?eh.Amount:((GameEvent_RegenTicked)e).Amount;route=new AttackRoute("system:heal","","system","heal","HealContinuousFx","light",recovered<=4,"source semantic recovery size");scenario=EffectScenario.For(route,1);duration=scenario.Duration;contact=scenario.Windup;}
   if(i==finalHit) duration+=.16f; // bounded finishing release, no frozen reward transition
   _pending.Enqueue(new(e,route,scenario,i==finalHit,duration,contact,contacts));
  }
  // First callback waits for Advance so all StateChanged listeners have the new batch.
 }
 public PresentationBeat Describe(GameEvent e, AttackRouteCatalog routes) {
  var description=new PresentationTimeline{ContactPlanner=ContactPlanner};
  description.Enqueue(new[]{e},routes);return description._pending.Dequeue();
 }
 static float DurationFor(GameEvent e) => e.Type switch {
  // Explicit newly adapted semantic beats; they own no damage route or HP contact.
  "Cascaded" or "CascadeRetargeted"=>.22f,
  "ImpulseReturned" or "ImpulseKept" or "ImpulseKindled" or "BlazeIgnited" or "BlazeExtinguished"=>.16f,
  "EnemyActionExecuting"=>.20f,"EnemyActionResolved"=>.07f,"EnemyDied"=>1.08f,"EnemyFled"=>.56f,
  "CombatEnded"=>.22f,"TurnStarted"=>.14f,"TurnEnded"=>.12f,"CombatStarted"=>.20f,
  "BlockGained" or "IceBlockGained" or "HpHealed" or "EnemyHealed" or "BurnTick" or "ScaldTick"=>.24f,
  "ReactionTriggered" or "ActionNegated" or "DeathSaved"=>.26f,
  "EnemySummoned" or "EnemySplit" or "EnemyHatched" or "PermanentPlayed" or "RetainerRushed"=>.22f,
  "CardPlayed" or "CardSet" or "GearUsed"=>.11f,
  "StatusInflicted" or "ExposedApplied" or "EnemyWeakened" or "BurnApplied" or "StrengthGained" or "GrowthAdded" or "MomentumAdded" or "LightGained" or "LightSpent" or "LightDischarged" or "ArtifactBlocked" or "PlayerArtifactBlocked"=>.16f,
  _=>.035f
 };
 void BeginNext() {
  if(!_pending.TryDequeue(out var next)) {Active=null;Drained?.Invoke();return;}
  Active=next;Time=0;_retimed=0;LastContactTime=0;ContactDelivered=false;ContactOrdinal=-1;Started?.Invoke(next);
 }
 void DeliverContact(PresentationBeat beat) {LastContactTime=Time;ContactOrdinal++;int count=beat.Contacts?.Individual==true?beat.Contacts.Count:1;ContactDelivered=ContactOrdinal>=count-1;Contact?.Invoke(beat);}
 public void Advance(float realDelta) {
  LastAdvanceDelta=0;if(realDelta<=0||!Busy)return;
  float remaining=realDelta;bool visualContact=false;int safety=0;
  // Carry elapsed time across nonvisual beat boundaries. A rendered frame owns at
  // most one visual contact; a stalled frame retimes the train rather than bursting.
  while(remaining>0.000001f&&Busy&&safety++<4096){
   if(Active is null)BeginNext();if(Active is null)return;var beat=Active;
   int count=beat.Contacts?.Individual==true?beat.Contacts.Count:1;
   float nextContact=beat.ContactAt+(ContactOrdinal+1)*EnemyAttackContactPlan.ContactSpacing+_retimed;
   bool visual=beat.Route is not null;
   if(!ContactDelivered&&Time>=nextContact-.000001f){
    if(visual&&visualContact)return;
    DeliverContact(beat);visualContact|=visual;
    if(!Busy)return;
   }
   float end=beat.Duration+_retimed;
   if(Time>=end-.000001f){Finished?.Invoke(beat);CompletedBeats++;Active=null;if(_pending.Count==0){Drained?.Invoke();return;}continue;}
   float boundary=end;
   // A long render stall must leave a real contact frame before cleanup. Normal
   // 15/30/60 Hz steps retain their elapsed time; only overshooting stalls retime.
   float visibleCap=LastContactTime+Math.Min(.10f,Math.Max(.002f,end-LastContactTime)*.75f);
   if(visualContact&&visual){if(Time>=visibleCap-.000001f)return;boundary=Math.Min(boundary,visibleCap);}
   if(!ContactDelivered)boundary=Math.Min(boundary,beat.ContactAt+(ContactOrdinal+1)*EnemyAttackContactPlan.ContactSpacing+_retimed);
   // Integrate both sides of the short final-hit slowdown exactly.
   float slowStart=beat.ContactAt,slowEnd=slowStart+.14f;
   float rate=beat.WinningHit&&Time>=slowStart&&Time<slowEnd?.38f:1;
   if(beat.WinningHit){if(Time<slowStart)boundary=Math.Min(boundary,slowStart);else if(Time<slowEnd)boundary=Math.Min(boundary,slowEnd);}
   float consumed=Math.Min(remaining,Math.Max(0,(boundary-Time)/rate));
   if(consumed<=.000001f){Time=boundary+.000002f;continue;}
   Time+=consumed*rate;LastAdvanceDelta+=consumed*rate;remaining-=consumed;TimeAdvanced?.Invoke(consumed*rate);
   // The next contact must be observed on another frame. Surplus stall time is
   // deliberately not transferred into subsequent contacts, matching the source.
   if(visualContact&&!ContactDelivered&&Time>=boundary-.000001f)return;
  }
 }
}

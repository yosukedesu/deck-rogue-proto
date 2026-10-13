// Historical P0 source recovered verbatim, then explicitly reconstructed for source local motion.
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine.Generated;
using DeckRogue.Game;
using DeckRogue.GodotPort.Core;
using Godot;
namespace DeckRogue.GodotPort.Presentation;

public partial class BattleStage
{
 // GameRoot.Do / PresenterNumeric.QueuePresentation: accepting a player card is
 // independent of acquiring the actor. Only enemy/terminal old-board playback
 // locks commands; the source action queue fences actual contacts, not FX tails.
 public bool InputLocked {get;private set;}
 public SessionChange? GetEventChange(GameEvent e)=>_eventOwners.TryGetValue(e,out var owner)?owner.Change:null;
 public bool IsOrdinaryPresentation(GameEvent e)=>_eventOwners.TryGetValue(e,out var owner)&&!owner.Fenced;
 public long ActiveCommandRevision=>_fenceCommand?.Change.Revision??_actorAction?.Owner.Change.Revision??-1;
 public int PendingCommandCount=>_commands.Count(c=>!c.Finished);
 public int PendingActorActions=>_actionQueue.QueuedCount;
 public int PendingActorContacts=>_actionQueue.PendingContacts;
 public int ResidualEffectCount=>_ordinaryActions.Sum(a=>a.Beats.Count(b=>b.Contacted&&b.Effect is not null));
 public int ActorActionLaunchCount {get;private set;}
 public float LastActorLaunchAt {get;private set;}
 public float LastActorContactAt {get;private set;}
 public long LastActorContactRevision {get;private set;}=-1;
 public int LastActorContactFrame {get;private set;}=-1;
 public int LastActorLaunchFrame {get;private set;}=-1;
 public float LastActorHandoffPause=>_lastHandoffPause;

 sealed class CommandOwner {
  public SessionChange Change=null!;public CombatPresentationLedger.Batch? Ledger;
  public bool Fenced,Finished;public int RemainingActions;
  public CombatActionQueue.ActionToken? FenceToken;
 }
 sealed class ActionPlayback {
  public CommandOwner Owner=null!;public CombatActionQueue.ActionToken Token=null!;
  public readonly List<OrdinaryBeat> Beats=new();public bool Launched,Finished;public float LaunchedAt;
  public bool EnemyPhase;public float NotBeforeFinish;public GameEvent_CardPlayed? Card; public SourceShowcaseProfile? Profile; public int Volleys=1; public BattleActor? Actor;public SourceActorMotion.OwnershipToken? Capture;
 }
 sealed class EffectPlayback {public AuthoredGeometryFx Native=null!;public ContinuousAttackFx Fallback=null!;}
 sealed class OrdinaryBeat {
  public PresentationBeat Beat=null!;public ActionPlayback Action=null!;public int Index;
  public float ContactAt,PreviewAt,StartedAt,Time,EffectStartedAt,EffectOffset,LastContact,ContactRetimed;
  public readonly SourceFrameDelay InnerDelay=new();
  public bool Started,Contacted,Rejected,Projection;public int Hp,Block,ContactOrdinal=-1;
  public BattleActor? Source,Target;public EnemyAttackContactPlan.SourceProof? SourceProof;
  public EffectPlayback? Effect;public int Volley;public bool Direct,Flashed;public BattleActor? CapturedTarget;
 }
 readonly CombatActionQueue _actionQueue=new();
 readonly List<CommandOwner> _commands=new();
 readonly Dictionary<GameEvent,CommandOwner> _eventOwners=new(ReferenceEqualityComparer.Instance);
 readonly List<ActionPlayback> _ordinaryActions=new();
 readonly Stack<EffectPlayback> _effectPool=new();
 CommandOwner? _fenceCommand;ActionPlayback? _actorAction;OrdinaryBeat? _ordinaryCallback;
 float _lastVolleyAt=-1;
 int _presentationFrame,_ordinaryCompletedEvents,_effectPoolSerial,_lastOrdinaryVisualFrame=-1;
 int OrdinaryPendingEvents=>_ordinaryActions.Sum(a=>a.Beats.Count(b=>!b.Contacted));
 int OrdinaryActiveEffects=>_ordinaryActions.Sum(a=>a.Beats.Count(b=>b.Effect?.Native.IsActive==true||b.Effect?.Fallback.Visible==true));
 int OrdinaryActiveLights=>_ordinaryActions.Sum(a=>a.Beats.Sum(b=>(b.Effect?.Native.ActiveLights??0)+(b.Effect?.Fallback.ActiveLights??0)));
 OrdinaryBeat? ActiveOrdinaryBeat=>_ordinaryActions.Where(a=>a.Launched).SelectMany(a=>a.Beats).LastOrDefault(b=>b.Started&&(!b.Contacted||b.Effect is not null));
 float BeatTime=>_ordinaryCallback?.Time??_timeline.Time;
 int BeatContactOrdinal=>_ordinaryCallback?.ContactOrdinal??_timeline.ContactOrdinal;
 bool LogicalEnemyAlive(int i)=>_latestRun?.Combat is {} state&&i>=0&&i<state.Enemies.Count&&state.Enemies[i].Hp>0;

 public static bool RequiresInputFence(SessionChange change)=>
  change.Previous?.Phase==RunPhases.Combat&&change.Current.Combat is {} current&&
  (change.Current.Phase!=RunPhases.Combat||change.Events.Any(e=>e is GameEvent_TurnEnded)||
   (change.Previous.Combat?.Phase==CombatPhases.AwaitingReaction&&current.EventLog.FirstOrDefault() is GameEvent_CombatStarted));

 void RegisterCommand(SessionChange change,GameState? before){
  var owner=new CommandOwner{Change=change,Fenced=RequiresInputFence(change)};
  if(before is not null&&change.Current.Combat is {} after){
   owner.Ledger=_ledger.Register(before,after);
   if(owner.Ledger is not null)for(int i=before.EventLog.Count;i<after.EventLog.Count;i++){
    var receipt=_ledger.Find(i,after.EventLog[i]);if(receipt is not null)_receipts[after.EventLog[i]]=receipt;
   }
  }
  _commands.Add(owner);foreach(var e in change.Events)_eventOwners[e]=owner;
  IsPresenting=true;
  if(owner.Fenced){InputLocked=true;_session?.SetPresentationBusy(true);}
  bool enemyPhase=change.Events.Any(e=>e is GameEvent_TurnEnded)||change.Previous?.Combat?.Phase==CombatPhases.AwaitingReaction;
  if(enemyPhase){
   var action=new ActionPlayback{Owner=owner,EnemyPhase=true};owner.RemainingActions=1;
   var keys=new HashSet<string>();
   var path="res://assets/Audio/audio.json";if(Godot.FileAccess.FileExists(path)){var audio=Newtonsoft.Json.Linq.JObject.Parse(Godot.FileAccess.GetFileAsString(path));foreach(var key in ((Newtonsoft.Json.Linq.JObject?)audio["sfx"])?.Properties()??Enumerable.Empty<Newtonsoft.Json.Linq.JProperty>())keys.Add(key.Name);}
   var plan=SourceEnemySequence.Plan(change.Events,keys);action.NotBeforeFinish=plan.End;
   var contacts=new List<int>();
   for(int i=0;i<change.Events.Count;i++){var e=change.Events[i];var beat=_timeline.Describe(e,Routes);
    float inner=e is GameEvent_DamageDealt {Source:"enemy"} hit&&SourceEnemyRanged(before,hit.EnemyIndex??-1)?.22f:0;
    action.Beats.Add(new(){Beat=beat,Action=action,Index=i,ContactAt=plan.Onsets[i]+inner,PreviewAt=plan.Onsets[i]});contacts.Add(i);}
   _ordinaryActions.Add(action);action.Token=_actionQueue.Enqueue(_clock,contacts,token=>LaunchOrdinary(action));
  }else{
   // No callbacks run inside StateChanged. The gate is already visible to UI;
   // actor acquisition waits for the next presentation frame.
   var starts=new List<int>{0};
   for(int i=1;i<change.Events.Count;i++)if(change.Events[i] is GameEvent_CardPlayed or GameEvent_ReactionTriggered or GameEvent_GearUsed or GameEvent_TurnEnded)starts.Add(i);
   owner.RemainingActions=starts.Count;
   for(int part=0;part<starts.Count;part++){
    int from=starts[part],to=part+1<starts.Count?starts[part+1]:change.Events.Count;
    var action=new ActionPlayback{Owner=owner};
    float delay=change.Events.Skip(from).Take(to-from).Any(e=>e is GameEvent_DamageDealt{Source:"player"}) ? .13f : 0;
    var contacts=new List<int>();
    for(int i=from;i<to;i++){
     var e=change.Events[i];var beat=_timeline.Describe(e,Routes);
     bool card=e is GameEvent_CardPlayed;
     // Preserve every original event notification, but events without a source
     // presentation do not invent a 35 ms wait or an extra actor movement.
     float contact=card?0:delay;
     action.Beats.Add(new(){Beat=beat,Action=action,Index=i,ContactAt=contact,PreviewAt=Math.Max(0,contact-(beat.Scenario?.Windup??0))});
     contacts.Add(i);
     if(!card&&HasOrdinaryPresentation(e,beat))delay+=e is GameEvent_ReactionTriggered?.45f:e is GameEvent_EnemyActionExecuting?.30f:e is GameEvent_EnemyInterrupted?.45f:.12f;
    }
    PlanActorAction(action);
    if(change.Events.OfType<GameEvent_CombatEnded>().Any(e=>e.Result=="won")){var final=change.Events.LastOrDefault(e=>e is GameEvent_DamageDealt{HpLoss:>0});var last=action.Beats.LastOrDefault(b=>ReferenceEquals(b.Beat.Event,final));if(last is not null)last.Beat=last.Beat with{WinningHit=true,Duration=last.Beat.Duration+.16f};}
    _ordinaryActions.Add(action);
    action.Token=_actionQueue.Enqueue(_clock,contacts,token=>LaunchOrdinary(action));
   }
   // A previous strong fence cannot be released by a later presentation batch.
   _session?.SetPresentationBusy(InputLocked);
  }
 }
 static bool HasOrdinaryPresentation(GameEvent e,PresentationBeat b)=>b.Route is not null||CombatPresentationLedger.IsNumeric(e)||e.Type is
  "TurnStarted" or "TurnEnded" or "EnemyIntentDeclared" or "ReactionTriggered" or "ActionNegated" or "EnemyActionExecuting" or "EnemyInterrupted" or
  "StatusInflicted" or "ExposedApplied" or "EnemyWeakened" or "BurnApplied" or "StrengthGained" or "GrowthAdded" or "MomentumAdded" or
  "LightGained" or "LightSpent" or "LightDischarged" or "ArtifactBlocked" or "PlayerArtifactBlocked" or
  "Cascaded" or "CascadeRetargeted" or "ImpulseReturned" or "ImpulseKept" or "ImpulseKindled" or "BlazeIgnited" or "BlazeExtinguished";
 void LaunchOrdinary(ActionPlayback action){
  action.Launched=true;action.LaunchedAt=_clock;_actorAction=action;
  ActorActionLaunchCount++;LastActorLaunchAt=_clock;LastActorLaunchFrame=_presentationFrame;_lastHandoffPause=0;
  action.NotBeforeFinish+=_clock;if(action.EnemyPhase){_blockBoundaryCommand=action.Owner.Change.Command is RunCommand_Combat;_enemyBlockExpiryPending=false;LastEnemySequenceStartedAt=_clock;LastEnemySequenceEndAt=action.NotBeforeFinish;}
  foreach(var b in action.Beats){b.ContactAt+=_clock;b.PreviewAt+=_clock;}
 }
 EffectPlayback AcquireEffect(){
  if(!_effectPool.TryPop(out var slot)){
   slot=new(){Native=new AuthoredGeometryFx{Name="OrdinaryAuthored"+(++_effectPoolSerial)},Fallback=new ContinuousAttackFx{Name="OrdinaryFallback"+_effectPoolSerial}};
   AddChild(slot.Native);AddChild(slot.Fallback);slot.Fallback.Clear();
  }
  slot.Native.QualityLevel=slot.Fallback.QualityLevel=_quality.Level;slot.Native.RealLightsEnabled=slot.Fallback.RealLightsEnabled=RealLightsEnabled;
  return slot;
 }
 void RetireEffect(OrdinaryBeat b){if(b.Effect is not {} effect)return;effect.Native.Clear();effect.Fallback.Clear();b.Effect=null;_effectPool.Push(effect);}
 bool ReceiptReadyForPreview(OrdinaryBeat b){
  if(!_receipts.TryGetValue(b.Beat.Event,out var r))return true;
  return r.Reason is not null||_ledger.Ready(r);
 }
 void ProcessOrdinaryContacts(float delta){
  // Snapshot: contact listeners may synchronously accept another command.
  int generation=_actionQueue.Generation;
  foreach(var action in _ordinaryActions.ToArray()){
   if(!action.Launched||action.Finished)continue;
   foreach(var b in action.Beats){
    if(!b.Started&&_clock+.000001f>=b.PreviewAt&&ReceiptReadyForPreview(b)){
     b.Started=true;AnchorSourceEnemyInnerContact(b);b.StartedAt=b.PreviewAt;if(b.Beat.Route is not null)b.Effect=AcquireEffect();
     WithOrdinaryBeat(b,()=>{BeginBeat(b.Beat);if(action.EnemyPhase)b.StartedAt=b.ContactAt-(b.Beat.Scenario?.Windup??0);});
    }
    // AfterCo accumulates the actual scaled delta after its first yield. Do not
    // compare an absolute float timestamp or admit the contact using epsilon.
    if(b.InnerDelay.Pending){if(!b.InnerDelay.Sample(delta,_presentationFrame))continue;b.ContactAt=_clock;}
    float due=b.ContactAt+(b.ContactOrdinal+1)*EnemyAttackContactPlan.ContactSpacing+b.ContactRetimed;
    if(b.Contacted||_clock+.000001f<due)continue;
    if(b.Beat.Route is not null&&_lastOrdinaryVisualFrame==_presentationFrame&&!(b.Direct&&action.Profile is not null&&Math.Abs(_lastVolleyAt-b.ContactAt)<.00001f))break;
    // Preserve intra-action event order; a later frame cannot jump a predecessor.
    if(action.Beats.Any(p=>p.Index<b.Index&&!p.Contacted))break;
    if(!b.Started){b.Started=true;AnchorSourceEnemyInnerContact(b);b.StartedAt=b.PreviewAt;if(b.Beat.Route is not null)b.Effect=AcquireEffect();WithOrdinaryBeat(b,()=>{BeginBeat(b.Beat);if(action.EnemyPhase)b.StartedAt=b.ContactAt-(b.Beat.Scenario?.Windup??0);});}
    // A blocked preview may first become eligible at this boundary. A newly
    // started ranged outer callback still owes its own source inner delay.
    if(b.InnerDelay.Pending||_clock+.000001f<b.ContactAt)continue;
    // Preview can overlap an earlier contact, but arithmetic/identity is checked
    // again at the actual transaction boundary before any numeric mutation.
    var receipt=CombatPresentationLedger.Read(b.Beat.Event);
    if(b.ContactOrdinal<0&&!b.Rejected&&receipt is not null&&!ValidReceipt(b.Beat.Event,receipt,_displayState)){
     b.Rejected=true;RejectedReceiptCount++;if(_receipts.TryGetValue(b.Beat.Event,out var owned))owned.Reason="numeric_display_boundary_mismatch";RetireEffect(b);
    }
    // A stalled render must still expose the contact geometry for a frame.
    if(_clock-due>.10f&&b.Beat.Scenario is {} scenario)b.StartedAt=_clock-scenario.Windup;
    // Preserve source spacing after a genuine render stall; never burst overdue rows.
    if(b.Beat.Contacts?.Individual==true&&_clock-due>.10f)b.ContactRetimed+=_clock-due;
    b.ContactOrdinal++;b.LastContact=_clock;
    b.Contacted=b.Beat.Contacts?.Individual!=true||b.ContactOrdinal>=b.Beat.Contacts.Count-1;
    if(b.Beat.Route is not null){_lastOrdinaryVisualFrame=_presentationFrame;_lastVolleyAt=b.ContactAt;}
    WithOrdinaryBeat(b,()=>{if(b.Beat.Event is GameEvent_CardPlayed card)PresentOrdinaryCard(action,card);ContactBeat(b.Beat);});
    if(generation!=_actionQueue.Generation)return;
    if(b.Contacted)_ordinaryCompletedEvents++;
    if(b.Contacted&&_actionQueue.ContactComplete(action.Token,b.Index,_clock,_presentationFrame,action.Actor?.Motion.PauseRemaining(_clock)??0)){
     LastActorContactAt=_clock;LastActorContactRevision=action.Owner.Change.Revision;LastActorContactFrame=_presentationFrame;
    }
   }
   CompleteOrdinaryActionIfDone(action);
  }
 }
 void AnchorSourceEnemyInnerContact(OrdinaryBeat beat){
  // Source AfterVisual(.22) starts in Show(DamageDealt), at the actual outer
  // callback time, rather than the planned sequence slot before frame rounding.
  if(beat.Action.EnemyPhase&&beat.Beat.Event is GameEvent_DamageDealt {Source:"enemy"} hit&&
   SourceEnemyRanged(beat.Action.Owner.Change.Previous?.Combat,hit.EnemyIndex??-1)){beat.ContactAt=_clock+.22f;beat.InnerDelay.Begin(.22f,_presentationFrame);}
 }
 void PresentOrdinaryCard(ActionPlayback action,GameEvent_CardPlayed card){BeginSourceCard(action);}
 void CompleteOrdinaryActionIfDone(ActionPlayback action){
  if(action.Finished||_clock<action.NotBeforeFinish||action.Beats.Any(b=>!b.Contacted||b.Effect is not null))return;
  action.Finished=true;if(--action.Owner.RemainingActions==0){action.Owner.Finished=true;if(action.Owner.Ledger is not null)_ledger.Finish(action.Owner.Ledger);}
 }
 void AdvanceOrdinaryEffects(){
  foreach(var action in _ordinaryActions.ToArray())foreach(var b in action.Beats){
   if(b.Effect is not {} slot)continue;
   float elapsed=Math.Max(0,_clock-b.StartedAt);
   if(b.Rejected){RetireEffect(b);continue;}
   if(slot.Native.IsActive)slot.Native.Sample(elapsed-b.EffectStartedAt+b.EffectOffset);else if(slot.Fallback.Visible)slot.Fallback.Sample(elapsed-b.EffectStartedAt+b.EffectOffset);
   if(b.Contacted&&elapsed>=b.Beat.Duration)RetireEffect(b);
  }
  foreach(var action in _ordinaryActions)CompleteOrdinaryActionIfDone(action);
 }
 void WithOrdinaryBeat(OrdinaryBeat b,Action callback){
  int callbackGeneration=_actionQueue.Generation;
  var native=_native;var fx=_fx;var source=_sourceActor;var target=_targetActor;var proof=_sourceProof;
  bool rejected=_rejectBeat,projection=_projectionActive;int hp=_contactHp,block=_contactBlock;
  float start=_effectStartedAt,offset=_effectOffset;var previous=_ordinaryCallback;
  _ordinaryCallback=b;_sourceActor=b.Source;_targetActor=b.Target;_sourceProof=b.SourceProof;
  _rejectBeat=b.Rejected;_projectionActive=b.Projection;_contactHp=b.Hp;_contactBlock=b.Block;_effectStartedAt=b.EffectStartedAt;_effectOffset=b.EffectOffset;
  b.Time=Math.Max(0,_clock-b.StartedAt);if(b.Effect is {} effect){_native=effect.Native;_fx=effect.Fallback;}
  try{callback();}
  finally{
   b.Source=_sourceActor;b.Target=_targetActor;b.SourceProof=_sourceProof;b.Rejected=_rejectBeat;b.Projection=_projectionActive;
   b.Hp=_contactHp;b.Block=_contactBlock;b.EffectStartedAt=_effectStartedAt;b.EffectOffset=_effectOffset;
   _native=native;_fx=fx;_sourceActor=source;_targetActor=target;_sourceProof=proof;_rejectBeat=rejected;_projectionActive=projection;
   _contactHp=hp;_contactBlock=block;_effectStartedAt=start;_effectOffset=offset;_ordinaryCallback=previous;
   if(callbackGeneration!=_actionQueue.Generation){_sourceActor=null;_targetActor=null;_ordinaryCallback=null;_projectionActive=false;_rejectBeat=false;}
  }
 }
 void AppendAttackLights(List<ActorMaterialBinding.PointLight> lights,AuthoredGeometryFx? probe){
  if(probe is not null){probe.AppendSourceLights(lights);return;}
  _native.AppendSourceLights(lights);
  foreach(var b in _ordinaryActions.SelectMany(a=>a.Beats))if(b.Effect is {} effect&&lights.Count<8)effect.Native.AppendSourceLights(lights);
  AppendSourceCastLight(lights);
  if(lights.Count>8)lights.RemoveRange(8,lights.Count-8);
 }
 void TryFinishPresentations(){
  if(_resetting||!IsPresenting||_timeline.Busy||_actionQueue.Busy||_commands.Any(c=>!c.Finished)||!TrackedActorsIdle)return;
  _ordinaryActions.Clear();_commands.Clear();_actorAction=null;
  if(_latestRun is {} run){if(run.Phase is RunPhases.Reward or RunPhases.RelicReward or RunPhases.Won){_audio.PlayKey("ui.win");_cues.Clear();}SetRunState(run);}
  IsPresenting=false;InputLocked=false;_session?.SetPresentationBusy(false);_receipts.Clear();_eventOwners.Clear();PresentationCompleted?.Invoke();
 }
 void CancelCommandQueue(){
  ClearSourceCastLight();
  _actionQueue.Cancel();_finishSlowRemaining=0;foreach(var action in _ordinaryActions)foreach(var beat in action.Beats)RetireEffect(beat);
  _ordinaryActions.Clear();_commands.Clear();_eventOwners.Clear();_actorAction=null;_fenceCommand=null;_ordinaryCallback=null;
 }
}

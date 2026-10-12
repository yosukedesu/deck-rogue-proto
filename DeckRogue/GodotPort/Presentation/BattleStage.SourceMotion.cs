// Explicit reconstruction from StageCombat.cs, CardVfxProfile.Showcase.cs and retained P0 contracts.
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine.Generated;
using DeckRogue.GodotPort.Core;
using Godot;
namespace DeckRogue.GodotPort.Presentation;
public partial class BattleStage {
 public bool SourceShowcaseEnabled {get;set;}=true; // Archived Unity APK build defines enabled this optional source mode.
 public string SourceShowcaseProvenance=>"reconstructed: archived DECKROGUE_IMPACT_SHOWCASE; installed user APK identity unknown";
 float _lastHostDelta,_lastHandoffPause,_finishSlowRemaining;
 float AdvanceFinishClock(float hostDelta){if(_finishSlowRemaining<=0)return hostDelta;float slowHost=Math.Min(hostDelta,_finishSlowRemaining/.38f);float slow=slowHost*.38f;_finishSlowRemaining=Math.Max(0,_finishSlowRemaining-slow);return slow+hostDelta-slowHost;}
 public float LastEnemySequenceStartedAt {get;private set;} public float LastEnemySequenceEndAt {get;private set;}
 GameEvent? _incomingEvent;int? _incomingAmount;
 RunState? EventRun=>_ordinaryCallback?.Action.Owner.Change.Current??_fenceCommand?.Change.Current??_latestRun;
 public int? GetPresentedIncomingAmount(GameEvent e)=>ReferenceEquals(e,_incomingEvent)?_incomingAmount:null;
 public Vector2? ProjectLeaderFeet()=>_hero is not null&&_world?.Camera is {} camera&&camera.IsInsideTree()?camera.UnprojectPosition(_hero.Home):null;
 public float GetCardRetirementSeconds(string cardId)=>SourceShowcaseEnabled&&SourceShowcaseProfiles.ForCard(cardId) is {} p?Math.Min(.10f,p.Contact*.70f):.25f;
 IEnumerable<BattleActor> AllPresentationActors()=>_actorRoot is null?Enumerable.Empty<BattleActor>():_actorRoot.GetChildren().OfType<BattleActor>().Concat(_retiredRoot?.GetChildren().OfType<BattleActor>()??Enumerable.Empty<BattleActor>());
 public int ActiveMotionCount=>AllPresentationActors().Count(a=>a.Motion.HasMotion||a.Motion.IsPaused(_clock)||a.ExecutionScaleActive);
 public int ActivePoseCount=>AllPresentationActors().Count(a=>!a.Dead&&a.HasActivePose);
 bool TrackedActorsIdle=>ActiveMotionCount==0&&ActivePoseCount==0&&!_deaths.Any(d=>_clock<d.Value.Started+d.Value.Delay+d.Value.Duration);
 void PublishNumericContact(GameEvent e,PresentationBeat beat){
  if(e is GameEvent_DamageDealt {Source:"enemy"} h&&!LastEventReceiptRejected){_incomingEvent=e;_incomingAmount=_projectionActive?beat.Contacts![BeatContactOrdinal].AmountContribution:h.Amount;}
  try{NumericContactPresented?.Invoke(e);}finally{_incomingEvent=null;_incomingAmount=null;}
 }
 static bool OwnsDirectHit(GameEvent_DamageDealt h,string cardId)=>h.Source=="player"&&h.SourceUid is null&&(h.PresentationSource is null||(h.PresentationSource.Kind=="card"&&h.PresentationSource.DefinitionId==cardId));
 void PlanActorAction(ActionPlayback action){
  var card=action.Beats.Select(b=>b.Beat.Event).OfType<GameEvent_CardPlayed>().FirstOrDefault();
  if(card is null)return;
  action.Card=card;
  var hits=action.Beats.Where(b=>b.Beat.Event is GameEvent_DamageDealt h&&OwnsDirectHit(h,card.CardId)).ToList();
  if(hits.Count==0)return;
  action.Profile=SourceShowcaseEnabled?SourceShowcaseProfiles.ForCard(card.CardId):null;
  var seen=new HashSet<int>();int volley=0;
  foreach(var b in hits){int target=((GameEvent_DamageDealt)b.Beat.Event).EnemyIndex??-1;if(seen.Contains(target)){volley++;seen.Clear();}seen.Add(target);b.Volley=volley;b.Direct=true;}
  action.Volleys=volley+1;
  if(action.Profile is not {} profile)return;
  float last=0;
  foreach(var b in action.Beats){
   if(b.Beat.Event is GameEvent_CardPlayed){b.ContactAt=0;continue;}
   if(b.Direct)last=profile.Contact+b.Volley*profile.Interval;
   b.ContactAt=last; b.PreviewAt=Math.Max(0,b.ContactAt-(b.Beat.Scenario?.Windup??0));
  }
 }
 void BeginSourceCard(ActionPlayback action){
  if(_hero is null||action.Card is null)return;
  _hero.Motion.BeginAction(_clock,_presentationFrame);
  action.Actor=_hero;
  var first=action.Beats.FirstOrDefault(b=>b.Direct);
  if(first?.Beat.Event is GameEvent_DamageDealt hit&&Enemy(hit.EnemyIndex) is {} target){
   var def=_session?.Catalog.Cards.FirstOrDefault(c=>c.Id==action.Card.CardId);
   bool ranged=def?.Type=="spell";
   if(action.Profile is not null){var pose=_hero.Motion.Approach(_hero.SourceMotionFeet,_hero.SourceMotionHeight,target.SourceMotionFeet,target.SourceMotionHeight,ranged,action.Volleys,action.Profile,_clock,_presentationFrame,_world.Camera.GlobalBasis.X,_hero.ArtId);_hero.PlayPose(pose,_clock);}
   else if(!ranged||_hero.ArtId=="leader_blue")_hero.PlayPose("attack",_clock);
  }else if(action.Beats.Any(b=>b.Beat.Event is GameEvent_BlockGained {Target:"player"} or GameEvent_IceBlockGained))_hero.PlayPose("block",_clock);
  action.Capture=_hero.Motion.Capture();
  foreach(var b in action.Beats)if(b.Direct&&b.Beat.Event is GameEvent_DamageDealt h)b.CapturedTarget=Enemy(h.EnemyIndex);
 }
 void ApplySourceImpact(PresentationBeat beat,GameEvent_DamageDealt hit){
  if(_targetActor is not {} target)return;
  if(beat.WinningHit)_finishSlowRemaining=Math.Max(_finishSlowRemaining,.14f);
  if(hit.Source=="enemy"&&(_ordinaryCallback is null||!_ordinaryCallback.Flashed)){PresentInjuryFlash(hit.HpLoss);if(_ordinaryCallback is {} row)row.Flashed=true;}
  var b=_ordinaryCallback;var a=b?.Action;
  if(b?.Direct==true&&a?.Profile is {} p&&ReferenceEquals(b.CapturedTarget,target)){
   float pause=p.ImpactPauseFor(beat.WinningHit,a.Volleys>1,b.Volley,a.Volleys);
   target.Motion.Recoil(_world.Camera.GlobalBasis.X,target.SourceMotionHeight,p.Heavy,_clock,_presentationFrame,true);
   target.Motion.RequestOwnedPause(target.Motion.Capture(),pause,_clock);
   if(a.Actor is {} actor&&a.Capture is {} capture)actor.Motion.RequestOwnedPause(capture,pause,_clock);
   target.PlayPose("hurt",_clock);_lastHandoffPause=a.Actor?.Motion.PauseRemaining(_clock)??0;
   _actionQueue.Hold(a.Token,_clock+_lastHandoffPause,_presentationFrame);
  }else {target.Motion.Recoil(_world.Camera.GlobalBasis.X*(target==_hero?-1:1),target.SourceMotionHeight,hit.Amount>=15,_clock,_presentationFrame,false);target.PlayPose("hurt",_clock);}
 }
}

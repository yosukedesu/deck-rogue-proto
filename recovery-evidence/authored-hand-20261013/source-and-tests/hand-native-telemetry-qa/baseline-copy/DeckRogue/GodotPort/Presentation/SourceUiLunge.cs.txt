#nullable enable
using System;
using Godot;
namespace DeckRogue.GodotPort.Presentation;

/// <summary>Source Tween.Lunge: a shared resting anchor, never additive displacement.
/// Delayed callbacks leave an already running lunge alone until they actually start.
/// UI canvas vectors are converted by the actor's source sizing and camera basis.</summary>
public sealed class SourceUiLunge
{
 public Vector3 Offset {get;private set;}
 public bool Active => _running || Pending;
 public bool Pending {get;private set;}
 public float Elapsed {get;private set;}
 public int Version {get;private set;}
 bool _running;Vector3 _travel,_pendingTravel;
 float _duration,_delayElapsed,_delay,_pendingDuration,_lastClock;
 int _startedFrame,_lastFrame,_pendingStartedFrame,_pendingLastFrame;
 public void Begin(Vector3 travel,float duration,float now,int frame,float delay=0){
  if(!float.IsFinite(duration)||duration<=0||!float.IsFinite(delay)||delay<0||!float.IsFinite(now)||now<0)throw new ArgumentOutOfRangeException(nameof(duration));
  Version++;
  if(delay>0){
   // Unity AfterVisual(.11) does not Acquire or write the RectTransform yet.
   // Canceling the old lunge here causes a visible snap on rapid card handoff.
   Pending=true;_pendingTravel=travel;_pendingDuration=duration;_delay=delay;_delayElapsed=0;_pendingStartedFrame=_pendingLastFrame=frame;return;
  }
  Pending=false;Start(travel,duration,now,frame);
 }
 void Start(Vector3 travel,float duration,float now,int frame){
  _travel=travel;_duration=duration;Elapsed=0;_startedFrame=_lastFrame=frame;_lastClock=now;_running=true;Offset=Vector3.Zero;
 }
 public void Sample(float now,float delta,int frame){
  if(!float.IsFinite(delta)||delta<0)throw new ArgumentOutOfRangeException(nameof(delta));
  // Existing tween's callback precedes the later-created delayed callback.
  if(_running&&frame>_startedFrame&&frame>_lastFrame&&now>=_lastClock){
   _lastFrame=frame;_lastClock=now;Elapsed=Math.Min(_duration,Elapsed+delta);
   if(Elapsed>=_duration){_running=false;Offset=Vector3.Zero;}
   else Offset=_travel*Amount(Elapsed/_duration);
  }
  if(Pending&&frame>_pendingStartedFrame&&frame>_pendingLastFrame){
   _pendingLastFrame=frame;_delayElapsed+=delta;
   if(_delayElapsed>=_delay){Pending=false;Start(_pendingTravel,_pendingDuration,now,frame);}
  }
 }
 public void Cancel(){Version++;_running=Pending=false;Elapsed=0;Offset=Vector3.Zero;}
 public static float Amount(float k){
  k=Math.Clamp(k,0,1);
  if(k<.35f){float t=k/.35f;return 1-(1-t)*(1-t);}
  float u=1-(k-.35f)/.65f;return u*u*u;
 }
}

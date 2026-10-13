#nullable enable
using System;
namespace DeckRogue.GodotPort.Presentation;

/// <summary>Source Tween.AfterCo: yield once, then accumulate actual scaled frame
/// deltas without sprite caps, timestamp subtraction, or comparison tolerance.</summary>
public sealed class SourceFrameDelay
{
 public bool Pending {get;private set;}
 public float Elapsed {get;private set;}
 float _duration;int _lastFrame;
 public void Begin(float duration,int frame){
  if(!float.IsFinite(duration)||duration<0)throw new ArgumentOutOfRangeException(nameof(duration));
  _duration=duration;Elapsed=0;_lastFrame=frame;Pending=duration>0;
 }
 public bool Sample(float delta,int frame){
  if(!float.IsFinite(delta)||delta<0)throw new ArgumentOutOfRangeException(nameof(delta));
  if(!Pending||frame<=_lastFrame)return false;
  _lastFrame=frame;Elapsed+=delta;
  if(Elapsed<_duration)return false;
  Pending=false;return true;
 }
}

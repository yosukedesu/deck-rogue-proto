using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine.Generated;
namespace DeckRogue.GodotPort.Presentation;

// Rally-only overlay. ContactAuthoredFrames supplies owned attacks, guard,
// reactions, and idle. A beat needs the reserved real first application.
public sealed class DrummerAuthoredFrames
{
 private readonly IReadOnlyDictionary<string,float[]> _clips;
 private Style04EnemySupportMotion.Token _token;
 private DrummerAppliedOncePlan? _plan;
 private int _generation=-1;
 private bool _cancelled;
 private float _applied=-1,_resolved=-1;
 public float AcceptedAppliedAt=>_applied;
 public float AcceptedResolvedAt=>_resolved;
 public int AppliedEventIndex=>_applied>=0?_plan?.AppliedIndex??-1:-1;
 public Style04EnemySupportMotion.Token AppliedToken=>_applied>=0?_token:Style04EnemySupportMotion.Token.Invalid;
 public Style04EnemySupportMotion.Token ResolvedToken=>_resolved>=0?_token:Style04EnemySupportMotion.Token.Invalid;
 public BruteAuthoredFrames.Snapshot State{get;private set;}=BruteAuthoredFrames.Snapshot.Disabled;
 public DrummerAuthoredFrames(IReadOnlyDictionary<string,float[]> clips){if(!new[]{"rest","prepare","beat","recovery"}.All(clips.ContainsKey)||clips.Any(p=>p.Value.Length==0||p.Value.Any(x=>!float.IsFinite(x)||x<=0)))throw new ArgumentException("Invalid drummer clip durations");_clips=clips.ToDictionary(x=>x.Key,x=>(float[])x.Value.Clone());}
 private void Clear(){_token=default;_plan=null;_generation=-1;_applied=_resolved=-1;}
 public void Begin(){ }
 public void BeginAttack(float now,Style04EnemyMotion.Snapshot a){if(!float.IsFinite(now)||now<0||!a.Active||a.ProfileId!="enemy_drummer"||a.MoveId!="thump")return;Clear();_cancelled=false;}
 public void BeginSupport(Style04EnemySupportMotion.Snapshot s){
  if(!s.Active||s.ProfileId!="enemy_drummer"||s.InstanceId<=0||s.Version<=0||s.OwnerVersion<0||s.ExecutionIndex<0||!float.IsFinite(s.StartedAt)||s.StartedAt<0||s.Deadline<=s.StartedAt||!((s.Kind=="rally"&&s.MoveId=="war_drum")||(s.Kind=="defend"&&s.MoveId=="guard")))return;
  Clear();_cancelled=false;if(s.Kind=="rally")_token=new(s.InstanceId,s.Version,s.ExecutionIndex,s.OwnerVersion);
 }
 private bool Owns(Style04EnemySupportMotion.Snapshot s)=>_token.IsValid&&s.ProfileId=="enemy_drummer"&&s.Kind=="rally"&&s.MoveId=="war_drum"&&s.InstanceId==_token.InstanceId&&s.Version==_token.Serial&&s.ExecutionIndex==_token.ExecutionIndex&&s.OwnerVersion==_token.OwnerVersion;
 public bool ReserveApplied(Style04EnemySupportMotion.Token token,Style04EnemySupportMotion.Snapshot s,DrummerAppliedOncePlan plan,int generation){
  if(_cancelled||_plan!=null||_applied>=0||generation<0||!s.Active||s.EndRequestedAt>=0||!token.Equals(_token)||!Owns(s)||plan.ExecutionIndex!=token.ExecutionIndex)return false;
  _plan=plan;_generation=generation;return true;
 }
 public bool AcceptedApplied(Style04EnemySupportMotion.Token token,Style04EnemySupportMotion.Snapshot s,GameEvent e,int index,int generation,float now){
  if(_cancelled||_applied>=0||_plan==null||generation!=_generation||!_plan.IsApplied(e,index)||!token.Equals(_token)||!Owns(s)||!s.Active||s.EndRequestedAt>=0||!float.IsFinite(now)||now<s.StartedAt||now>=s.Deadline||now>=s.EndAt)return false;
  _applied=now;return true;
 }
 // Stage closes its existing support lifetime on real Resolved. This timestamp
 // is diagnostic only; it can never authorize a drawing or a second beat.
 public bool AcceptedResolved(Style04EnemySupportMotion.Token token,Style04EnemySupportMotion.Snapshot s){if(_cancelled||_resolved>=0||!s.Active||!token.Equals(_token)||!Owns(s)||s.EndRequestedAt<0||!float.IsFinite(s.EndRequestedAt)||s.EndRequestedAt<s.StartedAt||s.EndRequestedAt>=s.EndAt||(_applied>=0&&s.EndRequestedAt<_applied))return false;_resolved=s.EndRequestedAt;return true;}
 public void ResumeIdleForPlacement(){Clear();_cancelled=false;}
 public void Cancel(){Clear();_cancelled=true;}
 private float Duration(string clip)=>_clips[clip].Sum();
 private BruteAuthoredFrames.Snapshot Pick(string clip,float age,string reason,int owner,Style04EnemyMotion.Snapshot a){var d=_clips[clip];float t=Math.Max(0,age);int index=0;while(index<d.Length-1&&t>=d[index]){t-=d[index];index++;}return State=new(clip,clip,index,reason,false,true,owner,a.Version,a.ExecutionIndex,a.AcceptedContacts,a.LastOrdinal,a.LastContactAt);}
 public BruteAuthoredFrames.Snapshot Sample(float now,Style04EnemyMotion.Snapshot a,Style04EnemySupportMotion.Snapshot s,int owner,bool dead,BruteAuthoredFrames.Snapshot fallback){
  if(!float.IsFinite(now)||now<0||dead||_cancelled||a.Active||!s.Active||s.OwnerVersion!=owner||!Owns(s)||fallback.Reason=="hurt-notification")return State=fallback;
  if(_applied<0){if(s.EndRequestedAt>=0||s.Phase=="deadline-recovery")return Pick("rest",0,"unapplied-end-no-beat",owner,a);return Pick("prepare",Math.Max(0,now-s.StartedAt),"awaiting-real-applied",owner,a);}
  float age=now-_applied;if(age<0||now>=s.EndAt)return Pick("rest",0,"outside-owned-applied-window",owner,a);
  float beat=Duration("beat"),recovery=Duration("recovery");
  if(age<beat)return Pick("beat",age,"accepted-real-support-applied-once",owner,a);
  if(age<beat+recovery)return Pick("recovery",age-beat,"applied-support-recovery",owner,a);
  return Pick("rest",0,"applied-finite-rest",owner,a);
 }
}

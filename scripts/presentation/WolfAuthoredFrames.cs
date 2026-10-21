using System;
using System.Collections.Generic;
using System.Linq;
namespace DeckRogue.GodotPort.Presentation;
// Pure authored wolf selector. Real contact/pulse receipts alone unlock jaw close/call/hold.
// This owns no game effects, callbacks, action queues, timers or presentation fences.
public sealed class WolfAuthoredFrames
{
 public readonly record struct ReactionToken(long InstanceId,long Epoch,int OwnerVersion){public bool IsValid=>InstanceId>0&&Epoch>0&&OwnerVersion>=0;}
 private static long _nextReactionInstance;private readonly long _reactionInstance=System.Threading.Interlocked.Increment(ref _nextReactionInstance);
 private readonly IReadOnlyDictionary<string,float[]> _clips;
 private bool _cancelled=true;private float _started=-1,_epochStarted=-1;private int _owner=-1,_version=-1,_execution=-1,_count,_registeredOwner=-1;
 private long _reactionEpoch,_supportInstance=-1,_supportVersion=-1;private int _supportExecution=-1;private string _supportKind="",_supportMove="";
 private string _move="",_reaction="";private float _reactionAt=-1;private int _reactionOwner=-1;
 public BruteAuthoredFrames.Snapshot State{get;private set;}=BruteAuthoredFrames.Snapshot.Disabled;
 public WolfAuthoredFrames(IReadOnlyDictionary<string,float[]> clips){if(!new[]{"rest","windup","impact","recovery","idle","hurt","block","howlprepare","howlcall","howlrecovery","guardprepare","guardhold","guardrecovery"}.All(clips.ContainsKey)||clips["idle"].Length<2||clips.Any(p=>p.Value.Length==0||p.Value.Any(x=>!float.IsFinite(x)||x<=0)))throw new ArgumentException("Invalid completed wolf clip durations");_clips=clips.ToDictionary(x=>x.Key,x=>(float[])x.Value.Clone());}
 public ReactionToken CaptureReactionToken()=>!_cancelled&&_registeredOwner>=0?new(_reactionInstance,_reactionEpoch,_registeredOwner):default;
 private void RegisterOwner(float now,int owner){_reactionEpoch++;_registeredOwner=owner;_epochStarted=now;_reaction="";_reactionAt=-1;_reactionOwner=-1;_supportInstance=_supportVersion=-1;_supportExecution=-1;_supportKind=_supportMove="";_started=-1;_owner=_version=_execution=-1;_count=0;_move="";}
 private static bool SupportMove(Style04EnemySupportMotion.Snapshot s)=>s.ProfileId=="enemy_wolf"&&((s.Kind=="buff"&&s.MoveId=="howl")||(s.Kind=="defend"&&s.MoveId=="guard"));
 private bool OwnsSupport(Style04EnemySupportMotion.Snapshot s)=>SupportMove(s)&&s.OwnerVersion==_registeredOwner&&s.InstanceId==_supportInstance&&s.Version==_supportVersion&&s.ExecutionIndex==_supportExecution&&s.Kind==_supportKind&&s.MoveId==_supportMove;
 public void BeginSupport(float now,Style04EnemySupportMotion.Snapshot s){if(!float.IsFinite(now)||now<0||!s.Active||!SupportMove(s)||s.OwnerVersion<0||s.InstanceId<=0||s.Version<=0||s.ExecutionIndex<0||now<s.StartedAt||now>=s.Deadline)return;_cancelled=false;RegisterOwner(now,s.OwnerVersion);_supportInstance=s.InstanceId;_supportVersion=s.Version;_supportExecution=s.ExecutionIndex;_supportKind=s.Kind;_supportMove=s.MoveId;}
 public void BeginAttack(float now,Style04EnemyMotion.Snapshot a){if(!float.IsFinite(now)||now<0||!a.Active||a.ProfileId!="enemy_wolf"||a.MoveId is not ("flurry" or "rend")||a.OwnerVersion<0)return;_cancelled=false;RegisterOwner(now,a.OwnerVersion);_started=now;_owner=a.OwnerVersion;_version=a.Version;_execution=a.ExecutionIndex;_move=a.MoveId;_count=0;}
 private bool Owns(Style04EnemyMotion.Snapshot a)=>a.ProfileId=="enemy_wolf"&&a.OwnerVersion==_owner&&a.Version==_version&&a.ExecutionIndex==_execution&&a.MoveId==_move;
 public void AcceptedContact(Style04EnemyMotion.Snapshot a,int count){if(!_cancelled&&Owns(a)&&count==(_move=="flurry"?3:4)&&a.LastOrdinal>=0&&a.LastOrdinal<count&&a.AcceptedContacts==a.LastOrdinal+1&&(_count==0||_count==count))_count=count;}
 public void ResumeIdleForPlacement(float now=0,int owner=0){if(!float.IsFinite(now)||now<0||owner<0)return;RegisterOwner(now,owner);_cancelled=false;}
 public bool OwnsSupportToken(Style04EnemySupportMotion.Token token,Style04EnemySupportMotion.Snapshot s)=>!_cancelled&&OwnsSupport(s)&&token.IsValid&&token.InstanceId==s.InstanceId&&token.Serial==s.Version&&token.ExecutionIndex==s.ExecutionIndex&&token.OwnerVersion==s.OwnerVersion;
 public bool CancelSupport(Style04EnemySupportMotion.Token token,Style04EnemySupportMotion.Snapshot s){if(!OwnsSupportToken(token,s))return false;Cancel();return true;}
 public void Cancel(){_cancelled=true;RegisterOwner(-1,-1);}
 // Stage's synchronous call is generation-validated. Delayed callers must pass their captured token.
 public void NotifyPose(string name,float now,int owner)=>NotifyPose(name,now,owner,CaptureReactionToken());
 public bool NotifyPose(string name,float now,int owner,ReactionToken token){if(_cancelled||!token.IsValid||token.InstanceId!=_reactionInstance||token.Epoch!=_reactionEpoch||token.OwnerVersion!=_registeredOwner||owner!=_registeredOwner||name is not ("hurt" or "block")||!float.IsFinite(now)||now<_epochStarted||now<0)return false;_reaction=name;_reactionAt=now;_reactionOwner=owner;return true;}
 private float Duration(string clip)=>_clips[clip].Sum();
 private BruteAuthoredFrames.Snapshot Pick(string clip,float age,bool loop,string reason,bool active,int owner,Style04EnemyMotion.Snapshot a){float t=Math.Max(0,age),length=Duration(clip);var d=_clips[clip];if(loop)t%=length;int index=0;while(index<d.Length-1&&t>=d[index]){t-=d[index];index++;}return State=new(clip,clip,index,reason,false,active,owner,a.Version,a.ExecutionIndex,a.AcceptedContacts,a.LastOrdinal,a.LastContactAt);}
 public BruteAuthoredFrames.Snapshot Sample(float now,Style04EnemyMotion.Snapshot a,Style04EnemySupportMotion.Snapshot support,int owner,bool dead){
  if(!float.IsFinite(now)||now<0)return Pick("rest",0,false,"invalid-clock",false,owner,a);
  if(dead)return Pick("rest",0,false,"dead",false,owner,a);
  if(_cancelled)return Pick("rest",0,false,"cancelled",false,owner,a);
  if(owner!=_registeredOwner)return Pick("rest",0,false,"unowned-owner",false,owner,a);
  float reactionAge=now-_reactionAt;
  if(_reaction=="hurt"&&_reactionOwner==owner&&reactionAge>=0&&reactionAge<Duration("hurt"))return Pick("hurt",reactionAge,false,"hurt-notification",true,owner,a);
  if(a.Active&&a.OwnerVersion==owner){
   if(!Owns(a))return Pick("rest",0,false,"unowned-attack",false,owner,a);
   if(a.AcceptedContacts==0||a.LastContactAt<0)return Pick("windup",Math.Max(0,now-_started),false,"awaiting-real-contact",true,owner,a);
   if(_count<1)return Pick("rest",0,false,"unregistered-contact-count",false,owner,a);
   float age=Math.Max(0,now-a.LastContactAt),hold=Duration("impact");
   if(age<hold)return Pick("impact",age,false,"accepted-real-contact",true,owner,a);
   if(a.LastOrdinal<_count-1)return Pick(_clips.ContainsKey("reopen")?"reopen":"windup",age-hold,false,"accepted-contact-reopen",true,owner,a);
   return Pick("recovery",age-hold,false,"final-contact-recovery",true,owner,a);
  }
  if(_reaction=="block"&&_reactionOwner==owner&&reactionAge>=0&&reactionAge<Math.Min(Duration("block"),.18f))return Pick("block",reactionAge,false,"block-notification",true,owner,a);
  if(support.Active&&support.OwnerVersion==owner){
   if(!OwnsSupport(support))return Pick("rest",0,false,"unowned-support",false,owner,a);
   if(now<support.StartedAt||now>=support.Deadline||now>=support.EndAt)return Pick("idle",now,true,"idle",false,owner,a);
   bool howl=support.Kind=="buff",pulse=support.AcceptedPulses>0&&support.LastPulseAt>=support.StartedAt&&now>=support.LastPulseAt;
   bool pulseHold=pulse&&now-support.LastPulseAt<.055f;
   if((support.Phase=="recovery"||support.Phase=="deadline-recovery")&&!pulseHold)return Pick(howl?"howlrecovery":"guardrecovery",Math.Max(0,now-(support.EndRequestedAt>=0?support.EndRequestedAt:support.Deadline-.18f)),false,"support-recovery",true,owner,a);
   return Pick(howl?(pulse?"howlcall":"howlprepare"):(pulse?"guardhold":"guardprepare"),Math.Max(0,now-(pulse?support.LastPulseAt:support.StartedAt)),false,pulse?(howl?"accepted-real-strength-gained":"accepted-real-block-gained"):(howl?"awaiting-real-strength-gained":"awaiting-real-block-gained"),true,owner,a);
  }
  return Pick("idle",now,true,"idle",false,owner,a);
 }
}

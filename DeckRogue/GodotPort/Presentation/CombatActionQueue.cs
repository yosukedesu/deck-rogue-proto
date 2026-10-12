// Reconstructed 2026-10-12 from preserved P0 call contract and source actor ownership.
// Host presentation clock only; no engine state or global time scale.
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
namespace DeckRogue.GodotPort.Presentation;
public sealed class CombatActionQueue {
 public sealed class ActionToken {
  internal int Generation; internal HashSet<int> Contacts=new(); internal Action<ActionToken> Launch=null!;
  internal bool Started,Released; internal float ReadyAt; internal int ReleaseFrame;
  public int PendingContacts=>Contacts.Count;
 }
 readonly Queue<ActionToken> pending=new(); ActionToken? active;
 public int Generation {get;private set;}
 public int QueuedCount=>pending.Count;
 public int PendingContacts=>(active?.Contacts.Count??0)+pending.Sum(x=>x.Contacts.Count);
 public bool Busy=>active is not null||pending.Count>0;
 public ActionToken Enqueue(float now,IEnumerable<int> contacts,Action<ActionToken> launch){var t=new ActionToken{Generation=Generation,Contacts=new(contacts),Launch=launch,ReadyAt=now,ReleaseFrame=-1};pending.Enqueue(t);return t;}
 public bool ContactComplete(ActionToken t,int contact,float now,int frame,float pause=0){
  if(t.Generation!=Generation||!ReferenceEquals(active,t)||!t.Contacts.Remove(contact))return false;
  if(t.Contacts.Count>0)return false;t.ReadyAt=now+Math.Max(0,pause);t.ReleaseFrame=frame;return true;
 }
 public void Hold(ActionToken t,float until,int frame){if(t.Generation==Generation&&ReferenceEquals(active,t)){t.ReadyAt=Math.Max(t.ReadyAt,until);t.ReleaseFrame=Math.Max(t.ReleaseFrame,frame);}}
 public void Advance(float now,int frame){
  if(active is {} a){if(a.Contacts.Count>0||now+1e-6f<a.ReadyAt||frame<=a.ReleaseFrame)return;a.Released=true;active=null;}
  if(!pending.TryDequeue(out var t))return;active=t;t.Started=true;t.ReleaseFrame=frame;t.ReadyAt=now;t.Launch(t);
 }
 public void Cancel(){Generation++;pending.Clear();active=null;}
}

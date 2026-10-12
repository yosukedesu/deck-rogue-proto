using System;
using System.Collections.Generic;

namespace DeckRogue.Game {
 // Queue input freely; serialize ownership of the single player actor only until
 // the preceding action's actual contact transactions finish, never its residue.
 public sealed class CombatActionQueue {
  public sealed class ActionToken {
   public int Id,Generation,LaunchFrame=-1; public double QueuedAt,LaunchAt,LastContactAt;
   public readonly HashSet<int> Pending=new HashSet<int>();
   internal Action<ActionToken> Launch; public bool Launched,Cancelled;
   public double Wait=>Launched?Math.Max(0,LaunchAt-QueuedAt):0;
  }
  readonly Queue<ActionToken> waiting=new Queue<ActionToken>();
  ActionToken active; int nextId,lastContactFrame=-1;
  public int Generation {get;private set;}
  public int QueuedCount=>waiting.Count;
  public int PendingContacts=>active?.Pending.Count??0;
  public double MaxWait {get;private set;}
  public int MaxQueueDepth {get;private set;}
  public Action<ActionToken,string> Audit;
  public ActionToken Enqueue(double now,IEnumerable<int> contacts,Action<ActionToken> launch) {
   var t=new ActionToken{Id=++nextId,Generation=Generation,QueuedAt=now,Launch=launch};
   foreach(var index in contacts)if(!t.Pending.Add(index))throw new ArgumentException("Duplicate contact index");
   waiting.Enqueue(t);MaxQueueDepth=Math.Max(MaxQueueDepth,waiting.Count);Audit?.Invoke(t,"queued");return t;
  }
  public void Pump(double now,int frame,bool actorPaused=false) {
   if(actorPaused)return; // Preserve the preceding contact's bounded local hitstop, not its visual tail.
   if(active!=null && active.Pending.Count>0)return;
   // Conditional actor handoff barrier: do not overwrite a contact pose during
   // that same update frame. An idle/single action has no added frame wait.
   if(lastContactFrame==frame)return;
   while(waiting.Count>0) {
    var t=waiting.Dequeue();if(t.Cancelled || t.Generation!=Generation)continue;
    active=t;t.Launched=true;t.LaunchAt=now;t.LaunchFrame=frame;MaxWait=Math.Max(MaxWait,t.Wait);Audit?.Invoke(t,"launched");
    t.Launch?.Invoke(t);
    if(t.Pending.Count>0 || lastContactFrame==frame)return;
   }
  }
  public bool ContactComplete(ActionToken t,int index,double now,int frame) {
   if(t==null || t.Generation!=Generation || t.Cancelled || !t.Launched || !ReferenceEquals(t,active) || !t.Pending.Remove(index))return false;
   t.LastContactAt=now;lastContactFrame=frame;Audit?.Invoke(t,"contact_complete");return true;
  }
  public void Cancel() {
   ++Generation;if(active!=null)active.Cancelled=true;
   foreach(var t in waiting)t.Cancelled=true;
   waiting.Clear();active=null;lastContactFrame=-1;nextId=0;Audit?.Invoke(null,"cancelled");
  }
 }
}

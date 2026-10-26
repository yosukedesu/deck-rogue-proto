using System;

namespace DeckRogue.Game {
 // Reference-counted presentation work, independent of duration estimates.
 // A root lease is held while scheduling; descendants may retain work before
 // their parent callback releases it. Cancellation never calls completion.
 public sealed class PresentationWorkScope {
  public int Pending {get;private set;}
  public bool Cancelled {get;private set;}
  public bool Completed {get;private set;}
  readonly Action done;
  public PresentationWorkScope(Action onCompleted=null){done=onCompleted;}
  public bool Retain(){if(Cancelled || Completed)return false;Pending++;return true;}
  public void Release(){if(Cancelled || Completed || Pending<=0)return;if(--Pending==0){Completed=true;done?.Invoke();}}
  public void Cancel(){if(Completed || Cancelled)return;Cancelled=true;Pending=0;}
 }
}

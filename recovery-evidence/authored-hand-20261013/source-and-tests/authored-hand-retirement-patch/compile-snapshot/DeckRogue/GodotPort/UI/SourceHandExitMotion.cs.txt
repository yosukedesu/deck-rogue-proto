// Stage41 BattleView.SyncHand -> AnimateOut -> FlyTo. Visual state only.
using System;
namespace DeckRogue.GodotPort.UI;
public static class SourceHandExitMotion
{
    public const double Duration=.30, SyncStagger=.04;
    public const float EndScale=.30f;
    public readonly record struct Pose(float PositionFraction,float ScaleFraction,float Alpha,bool Complete);
    public static bool IsOrdinaryDiscard(bool played,bool inSet,bool inPermanent,bool inExhaust)
        =>!played&&!inSet&&!inPermanent&&!inExhaust;
    public static double Delay(int removedOrdinal)=>Math.Max(0,removedOrdinal)*SyncStagger;
    public static Pose Sample(double elapsed)
    {
        double t=double.IsFinite(elapsed)?Math.Clamp(elapsed/Duration,0,1):0;
        float move=(float)(t<.5?2*t*t:1-Math.Pow(-2*t+2,2)/2);
        return new Pose(move,(float)(t*t),(float)(t<.7?1:Math.Clamp(1-(t-.7)/.3,0,1)),t>=1);
    }
}
public sealed class SourceHandExitClock
{
    double _remainingDelay;ulong _lastFrame;bool _startTaken;
    public bool Started {get;private set;}
    public bool Cancelled {get;private set;}
    public double Elapsed {get;private set;}
    public SourceHandExitClock(double delay,ulong startFrame)
    {_remainingDelay=double.IsFinite(delay)?Math.Max(0,delay):0;Started=_remainingDelay==0;_lastFrame=startFrame;}
    public void Advance(double delta,ulong frame)
    {
        if(Cancelled||frame==_lastFrame)return;_lastFrame=frame;
        if(!double.IsFinite(delta)||delta<=0)return;
        // AfterCo starts its nested Run at t=0. Overshoot is not flight time.
        if(!Started){_remainingDelay-=delta;if(_remainingDelay<=0)Started=true;return;}
        Elapsed=Math.Min(SourceHandExitMotion.Duration,Elapsed+delta);
    }
    public bool TakeStart(){if(Cancelled||!Started||_startTaken)return false;_startTaken=true;return true;}
    public void Cancel()=>Cancelled=true;
}

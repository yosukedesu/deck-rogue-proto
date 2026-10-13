// Stage41 BattleScreen.HookHandCard: .12 second OutQuad move and scale.
using System;
namespace DeckRogue.GodotPort.UI;
public static class SourceHandHoverMotion
{
    public const double Duration=.12;
    public static float Fraction(double elapsed)
    {
        double t=double.IsFinite(elapsed)?Math.Clamp(elapsed/Duration,0,1):0;
        return (float)(1-(1-t)*(1-t));
    }
}
public sealed class SourceHandHoverClock
{
    public double Elapsed {get;private set;}
    public bool Cancelled {get;private set;}
    public bool Complete=>Elapsed>=_duration;
    private readonly double _duration;private ulong _lastFrame;
    public SourceHandHoverClock(ulong startFrame,double duration=SourceHandHoverMotion.Duration)
    {_lastFrame=startFrame;_duration=double.IsFinite(duration)&&duration>0?duration:SourceHandHoverMotion.Duration;}
    public void Advance(double delta,ulong frame)
    {
        // Source RunCo invokes t=0 synchronously, then yields a frame before
        // advancing. A MouseEntered cannot teleport before same-frame press.
        if(Cancelled||frame==_lastFrame)return;_lastFrame=frame;
        if(double.IsFinite(delta)&&delta>0)Elapsed=Math.Min(_duration,Elapsed+delta);
    }
    public void Cancel()=>Cancelled=true;
}

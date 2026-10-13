// Stage41/R9 BattleView.SyncHand incoming flight; pure visual math and clock.
// Capture a7f86b5 is separate evidence, not a byte-identical source claim.
using System;
namespace DeckRogue.GodotPort.UI;
public static class SourceHandDealMotion
{
    public const double Duration = .34, Stagger = .05, AfterRemovalDelay = .28;
    public const float InitialScale = .35f, InitialGodotDegrees = 20f, MinimumFlipWidth = .03f;
    public readonly record struct Pose(float PositionFraction, float ScaleX, float ScaleY, float Degrees, bool BackVisible, bool Complete);
    public static double Delay(int newOrdinal, bool removedCards, bool removedPlayedCard)
        => (removedCards && !removedPlayedCard ? AfterRemovalDelay : 0) + Math.Max(0,newOrdinal)*Stagger;
    public static Pose Sample(double elapsed, float startScale, float endScale, float startDegrees, float endDegrees)
    {
        double t=Math.Clamp(elapsed/Duration,0,1), inverse=1-t;
        float cubic=(float)(1-inverse*inverse*inverse), quad=(float)(1-inverse*inverse);
        float scale=startScale+(endScale-startScale)*quad;
        float angle=(endDegrees-startDegrees)%360;
        if(angle>180)angle-=360;else if(angle < -180)angle+=360;
        return new Pose(cubic,scale*Math.Max(MinimumFlipWidth,(float)Math.Abs(Math.Cos(t*Math.PI))),scale,startDegrees+angle*quad,t<.5,t>=1);
    }
}
// Delays and flight use the same scaled frame clock as UI _Process. A delayed
// start begins at t=0 on its first eligible tick, like Unity's After coroutine.
public sealed class SourceHandDealClock
{
    double _delay;
    bool _cueTaken;
    public bool Started { get; private set; }
    public bool Cancelled { get; private set; }
    public double Elapsed { get; private set; }
    public SourceHandDealClock(double delay){_delay=Math.Max(0,delay);Started=_delay==0;}
    public void Advance(double delta)
    {
        if(Cancelled||!double.IsFinite(delta)||delta<0)return;
        if(!Started){_delay-=delta;if(_delay<=1e-9)Started=true;return;}
        Elapsed=Math.Min(SourceHandDealMotion.Duration,Elapsed+delta);
    }
    public bool TakeStartCue(){if(Cancelled||!Started||_cueTaken)return false;_cueTaken=true;return true;}
    public void Cancel()=>Cancelled=true;
}

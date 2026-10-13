// Stage41 BattleScreen.EndDrag: Move(.15, default OutCubic),
// Scale(.15, default OutBack). Rotation and hand order change immediately.
using System;
namespace DeckRogue.GodotPort.UI;
public static class SourceHandReturnMotion
{
    public const double Duration=.15;
    static double T(double elapsed)=>double.IsFinite(elapsed)?Math.Clamp(elapsed/Duration,0,1):0;
    public static float PositionFraction(double elapsed){double u=1-T(elapsed);return (float)(1-u*u*u);}
    public static float ScaleFraction(double elapsed)
    {double t=T(elapsed);if(t<=0)return 0;if(t>=1)return 1;double u=t-1;const double c1=1.70158,c3=c1+1;return (float)(1+c3*u*u*u+c1*u*u);}
}

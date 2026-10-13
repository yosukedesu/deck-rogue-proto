// Stage41 BattleView.AnimateOut authored comparison branch only.
using System;
namespace DeckRogue.GodotPort.UI;
public readonly record struct SourceAuthoredHandStyle(long Revision,string CardUid,string DefinitionId,string AuthoredAttack)
{
    // Bound to the three proven Stage41 authored paths. A short retirement time
    // is not proof: the Vine and Earth comparison profiles have no authored art.
    public bool Matches(long revision,string uid,string definitionId)
        =>Revision==revision&&CardUid==uid&&DefinitionId==definitionId&&AuthoredAttack==(definitionId switch
        {"green_strike"=>"slash","blue_strike"=>"water","green_finisher_stomp"=>"stomp",_=>null})&&AuthoredAttack!=null;
}
public sealed record SourceAuthoredHandPlan(long Epoch,long Revision,string CardUid,string DefinitionId,
    SourceAuthoredHandStyle Style,double Duration,bool Exhaust,int? AdmittedTargetIndex)
{
    public bool Valid=>Epoch>=0&&Revision>=0&&Style.Matches(Revision,CardUid,DefinitionId)&&double.IsFinite(Duration)&&Duration>0;
}
public static class SourceAuthoredHandMotion
{
    public readonly record struct Pose(float PositionFraction,float ScaleFraction,float Alpha,float EndScale);
    public static Pose Sample(double normalized,bool exhaust,float startAlpha)
    {
        double t=double.IsFinite(normalized)?Math.Clamp(normalized,0,1):0;
        float scale=(float)(t*t);
        if(exhaust)return new Pose(0,scale,startAlpha*(float)(1-t),.25f);
        float move=(float)(t<.5?2*t*t:1-Math.Pow(-2*t+2,2)/2);
        return new Pose(move,scale,(float)(t<.7?1:Math.Clamp(1-(t-.7)/.3,0,1)),.30f);
    }
}

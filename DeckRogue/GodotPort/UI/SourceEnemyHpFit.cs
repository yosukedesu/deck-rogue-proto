// Bounded 130%-text accessibility adaptation over unchanged Stage41 PlaceHp.
// Preserve every current-HP digit; never move or resize the interrupt marker.
using System;
namespace DeckRogue.GodotPort.UI;
public static class SourceEnemyHpFit
{
    public static SourceEnemyLedgerModel.HpPlacement ReserveShield(
        SourceEnemyLedgerModel.HpPlacement source,int hp,float width,float mark,
        bool hasShield,float textScale,float measuredMaxLabelWidth)
    {
        if(textScale<=1||!hasShield||(mark>0&&mark<1))return source;
        // The source shield overlaps the bar by4. Its marker-aware placement
        // already reserves left10/right8. Reuse those clearances when large
        // accessibility text would otherwise enter that shield's occupied area.
        float left=Math.Min(10,width),right=Math.Max(left,width-8);
        if(measuredMaxLabelWidth<=right-left)return source;
        return new SourceEnemyLedgerModel.HpPlacement(Math.Max(0,hp).ToString(),left,right,true,true);
    }
    public static float HorizontalScale(float interval,float nativeMinimum,float textScale,bool hpOnly)
    {
        if(textScale<=1||!hpOnly||interval<=0||nativeMinimum<=interval)return 1;
        // Godot Label enforces nativeMinimum even when its source rect is
        // narrower. Fit this HP-only label horizontally, keeping130% height.
        return interval/nativeMinimum;
    }
}

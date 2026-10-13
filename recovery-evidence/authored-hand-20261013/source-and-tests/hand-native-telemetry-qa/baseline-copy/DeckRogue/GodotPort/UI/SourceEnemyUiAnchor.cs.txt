// RECONSTRUCTED from source policy and retained exact test cases.
using System;
namespace DeckRogue.GodotPort.UI;

public static class SourceEnemyUiAnchor
{
    // Reconstructed from original BattleScreen art-size policy and retained eight exact cases.
    public static float Height(int width,int height,int top,bool phone,bool phone96,string kind)
    {
        float k=phone?2.4f:4f;
        if(!phone&&width==64&&height==64)k*=kind=="boss"?1.5f:kind=="elite"?1.25f:1;
        float value=(height-top)*k;
        return phone&&!phone96?Math.Min(184,value):value;
    }
}

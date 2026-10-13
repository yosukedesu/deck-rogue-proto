// RECONSTRUCTION: source-backed Godot rendering helpers, not recovered original Git bytes.
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
namespace DeckRogue.GodotPort.UI;

public partial class SourceEnergyOrb : Control
{
    public int Energy,Maximum=3; public float TextScale=1;
    public override void _Draw()
    {
        var c=Size/2;float r=Math.Min(Size.X,Size.Y)/2-3;
        DrawCircle(c,r,SourcePalette.Brass);DrawCircle(c,r-8,SourcePalette.Paper);
        if(Energy>0)DrawArc(c,r-3,-Mathf.Pi/2,-Mathf.Pi/2+Mathf.Tau*Math.Clamp(Energy/(float)Math.Max(1,Maximum),0,1),60,SourcePalette.BrassLight,4,true);
        var deco=GD.Load<Font>("res://assets/Fonts/KaiseiDecol-Bold.ttf");var body=GD.Load<Font>("res://assets/Fonts/KleeOne-Regular.ttf");
        float u=Size.X/128;
        string amount=Energy.ToString(),maximum="/ "+Maximum;
        int amountSize=(int)Math.Round(40*u*TextScale),maximumSize=(int)Math.Round(13*u*TextScale);
        float gap=6*u,available=Size.X-18*u;
        float aw=deco.GetStringSize(amount,HorizontalAlignment.Left,-1,amountSize).X,mw=body.GetStringSize(maximum,HorizontalAlignment.Left,-1,maximumSize).X;
        // The source role size is retained whenever it fits. Draw the entire
        // integer with unlimited text width; fixed44%-orb clipping hid the0 in10.
        if(aw+gap+mw>available){float fit=available/(aw+gap+mw);amountSize=Math.Max(1,(int)Math.Floor(amountSize*fit));maximumSize=Math.Max(1,(int)Math.Floor(maximumSize*fit));aw=deco.GetStringSize(amount,HorizontalAlignment.Left,-1,amountSize).X;mw=body.GetStringSize(maximum,HorizontalAlignment.Left,-1,maximumSize).X;}
        float left=(Size.X-aw-gap-mw)/2,baseline=Size.Y*.60f;
        DrawString(deco,new Vector2(left,baseline),amount,HorizontalAlignment.Left,-1,amountSize,SourcePalette.Ink);
        DrawString(body,new Vector2(left+aw+gap,baseline),maximum,HorizontalAlignment.Left,-1,maximumSize,SourcePalette.InkSoft);
        SetMeta("displayed_energy",amount);SetMeta("displayed_maximum",maximum);SetMeta("energy_font_size",amountSize);SetMeta("energy_group_width",aw+gap+mw);
        DrawString(body,new Vector2(0,Size.Y*.82f),"エナジー",HorizontalAlignment.Center,Size.X,(int)(12*u*TextScale),SourcePalette.InkSoft);
    }
}

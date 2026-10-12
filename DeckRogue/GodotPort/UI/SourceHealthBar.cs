// RECONSTRUCTION: source-backed Godot rendering helpers, not recovered original Git bytes.
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
namespace DeckRogue.GodotPort.UI;

public partial class SourceHealthBar : Control
{
    public int Hp,Maximum,Block,IceBlock,PredictedLoss;
    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero,Size),SourcePalette.NightPill);
        float width=Size.X*Math.Clamp(Hp/(float)Math.Max(1,Maximum),0,1);
        DrawRect(new Rect2(Vector2.Zero,new Vector2(width,Size.Y)),new Color("bf625d"));
        float loss=Size.X*Math.Clamp(Math.Min(Hp,Math.Max(0,PredictedLoss))/(float)Math.Max(1,Maximum),0,1);
        if(loss>0){var r=new Rect2(width-loss,0,loss,Size.Y);DrawRect(r,new Color("eadfc499"));for(float x=r.Position.X;x<r.End.X;x+=6)DrawLine(new Vector2(x,0),new Vector2(Math.Min(x+Size.Y*.35f,r.End.X),Size.Y),SourcePalette.Paper,1);}
        DrawRect(new Rect2(Vector2.Zero,Size),SourcePalette.NightEdge,false,2);
    }
}

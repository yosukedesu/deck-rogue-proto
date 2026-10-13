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
    public float SourceInsetUnit; // Enemy ledger only; source inner track is inset 3 units.
    public float SourceHeroUnit; // Hero forecast uses the same source 3-unit inner track.
    public override void _Draw()
    {
        if(SourceHeroUnit>0){DrawSourceHeroBar(SourceHeroUnit);return;}
        if(SourceInsetUnit>0)
        {
            float u=SourceInsetUnit;
            DrawRect(new Rect2(Vector2.Zero,Size),SourcePalette.Ink);
            DrawRect(new Rect2(Vector2.One*2*u,(Size-Vector2.One*4*u).Max(Vector2.Zero)),new Color("31344f"));
            var inner=(Size-Vector2.One*6*u).Max(Vector2.Zero);
            DrawRect(new Rect2(Vector2.One*3*u,new Vector2(inner.X*Math.Clamp(Hp/(float)Math.Max(1,Maximum),0,1),inner.Y)),new Color("bf625d"));
            return;
        }
        DrawRect(new Rect2(Vector2.Zero,Size),SourcePalette.NightPill);
        float width=Size.X*Math.Clamp(Hp/(float)Math.Max(1,Maximum),0,1);
        DrawRect(new Rect2(Vector2.Zero,new Vector2(width,Size.Y)),new Color("bf625d"));
        float loss=Size.X*Math.Clamp(Math.Min(Hp,Math.Max(0,PredictedLoss))/(float)Math.Max(1,Maximum),0,1);
        if(loss>0){var r=new Rect2(width-loss,0,loss,Size.Y);DrawRect(r,new Color("eadfc499"));for(float x=r.Position.X;x<r.End.X;x+=6)DrawLine(new Vector2(x,0),new Vector2(Math.Min(x+Size.Y*.35f,r.End.X),Size.Y),SourcePalette.Paper,1);}
        DrawRect(new Rect2(Vector2.Zero,Size),SourcePalette.NightEdge,false,2);
    }
    private void DrawSourceHeroBar(float u)
    {
        // Stage41 BattleScreen.HpBar: 2-unit edge, 1-unit track, then fill/loss.
        DrawRect(new Rect2(Vector2.Zero,Size),SourcePalette.Ink);
        DrawRect(new Rect2(Vector2.One*2*u,(Size-Vector2.One*4*u).Max(Vector2.Zero)),new Color("31344f"));
        var inner=new Rect2(Vector2.One*3*u,(Size-Vector2.One*6*u).Max(Vector2.Zero));
        float now=Maximum>0?Math.Clamp(Hp/(float)Maximum,0,1):0;
        DrawRect(new Rect2(inner.Position,new Vector2(inner.Size.X*now,inner.Size.Y)),new Color("bf625d"));
        float to=Maximum>0?Math.Clamp((Hp-Math.Max(0,PredictedLoss))/(float)Maximum,0,1):0;
        if(PredictedLoss<=0||to>=now||inner.Size.Y<=0)return;
        var loss=new Rect2(inner.Position+new Vector2(inner.Size.X*to,0),new Vector2(inner.Size.X*(now-to),inner.Size.Y));
        DrawRect(loss,new Color("e1ae9e")); // PaperFx.RoseLoss is not night-remapped.
        var ink=SourcePalette.Ink;ink.A=.35f;
        // ThemeFx.Hatch: Point/Repeat, 8x8, (x+y)%8<2. Draw equivalent
        // clipped pixel spans, bottom-origin like Unity, without texture readback,
        // per-frame resources, filter inheritance, or an extra Godot callback.
        for(int y=0;y<(int)Math.Ceiling(loss.Size.Y/u);y++)
        {
            float top=Math.Max(loss.Position.Y,loss.End.Y-(y+1)*u);
            float height=Math.Min(u,loss.End.Y-y*u-top);
            int first=(8-y%8)%8;
            for(float x=first-8;x<loss.Size.X/u;x+=8)
            {
                float left=Math.Max(0,x*u),right=Math.Min(loss.Size.X,(x+2)*u);
                if(right>left&&height>0)DrawRect(new Rect2(loss.Position.X+left,top,right-left,height),ink);
            }
        }
        // SetLossBand suppresses a lethal left-edge tick; Nightify maps its Ink.
        if(to>0)DrawRect(new Rect2(loss.Position.X-u,inner.Position.Y-3*u,2*u,inner.Size.Y+6*u),SourcePalette.NightEdge);
    }
}

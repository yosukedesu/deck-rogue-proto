// RECONSTRUCTION: source-backed Godot rendering helpers, not recovered original Git bytes.
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
namespace DeckRogue.GodotPort.UI;

public partial class SourceAimArrow : Control
{
    public bool Active; public Vector2 Start,End; public float Unit=1; public Rect2? LeaderBody; public IReadOnlyList<Rect2> Blockers=Array.Empty<Rect2>();
    public string Note=""; public int VisibleDots {get;private set;}
    static Vector2 Curve(Vector2 a,Vector2 b,Vector2 c,float t)=>a*(1-t)*(1-t)+b*2*(1-t)*t+c*t*t;
    public override void _Draw()
    {
        VisibleDots=0;if(!Active)return;
        var mid=new Vector2(Start.X,Math.Min(Start.Y,End.Y)-70*Unit);
        if(LeaderBody is Rect2 body)for(int n=1;n<=4 && Enumerable.Range(1,23).Any(i=>body.HasPoint(Curve(Start,mid,End,i/24f)));n++)mid.X=Mathf.Lerp(Start.X,End.X,.15f*n);
        var pts=new Vector2[49];var acc=new float[49];for(int i=0;i<=48;i++){pts[i]=Curve(Start,mid,End,i/48f);if(i>0)acc[i]=acc[i-1]+pts[i].DistanceTo(pts[i-1]);}
        int seg=1;for(int i=0;i<14;i++){float distance=acc[48]*i/14f;while(seg<48&&acc[seg]<distance)seg++;float span=acc[seg]-acc[seg-1];var p=span>.001f?pts[seg-1].Lerp(pts[seg],(distance-acc[seg-1])/span):pts[seg];if(p.DistanceTo(End)<28*Unit||Blockers.Any(r=>r.HasPoint(p)))continue;DrawCircle(p,6.5f*Unit,SourcePalette.Ink);DrawCircle(p,5*Unit,SourcePalette.Brass);VisibleDots++;}
        DrawCircle(End,24*Unit,new Color("ead08a33"));DrawArc(End,24*Unit,0,Mathf.Tau,40,SourcePalette.Brass,2*Unit,true);DrawArc(End,26*Unit,0,Mathf.Tau,40,SourcePalette.Ink,Unit,true);
        if(Note.Length>0){var font=GD.Load<Font>("res://assets/Fonts/KleeOne-Regular.ttf");int fs=(int)(15*Unit);var size=font.GetStringSize(Note,HorizontalAlignment.Left,-1,fs)+new Vector2(16,10)*Unit;var p=End+new Vector2(32,-size.Y-10*Unit);p.X=Math.Min(p.X,Size.X-size.X-10);DrawRect(new Rect2(p,size),SourcePalette.Night);DrawString(font,p+new Vector2(8*Unit,size.Y-6*Unit),Note,HorizontalAlignment.Left,-1,fs,SourcePalette.Paper);}
    }
}

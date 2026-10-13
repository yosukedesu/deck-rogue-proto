// Source-backed battle details from Unity BattleScreen/PaperFx. Presentation only.
using Godot;
using System;
using System.Linq;
using System.Collections.Generic;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
namespace DeckRogue.GodotPort.UI;
public static class SourceBattleDecor
{
    private static readonly Dictionary<string,Texture2D> Cache=new();
    public static TextureRect Icon(Control parent,string name,Texture2D art,Rect2 rect,Color? tint=null)
    {
        var p=new TextureRect{Name=name,Texture=art,Position=rect.Position,Size=rect.Size,ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,MouseFilter=Control.MouseFilterEnum.Ignore,TextureFilter=CanvasItem.TextureFilterEnum.Nearest,Modulate=tint??Colors.White};parent.AddChild(p);return p;
    }
    public static SourceHudLabel Text(Control parent,string name,string text,Rect2 rect,int size,Color ink,bool deco=false,bool bold=false)
    {
        var l=new SourceHudLabel{Name=name,Text=text,SourceTextRect=rect,HorizontalAlignment=HorizontalAlignment.Left,MouseFilter=Control.MouseFilterEnum.Ignore};
        var font=GD.Load<Font>("res://assets/Fonts/"+(deco?"KaiseiDecol-Bold.ttf":bold?"KleeOne-SemiBold.ttf":"KleeOne-Regular.ttf"));
        l.AddThemeFontOverride("font",deco?SourceHudLabel.Spaced(font,size,2):font);l.AddThemeFontSizeOverride("font_size",size);l.AddThemeColorOverride("font_color",ink);parent.AddChild(l);return l;
    }
    public static StyleBoxFlat Box(Color fill,Color edge,float unit,float line=2)
    {
        int border=Math.Max(1,(int)Math.Round(line*unit)),radius=(int)Math.Round(8*unit);
        return new StyleBoxFlat{BgColor=fill,BorderColor=edge,BorderWidthLeft=border,BorderWidthRight=border,BorderWidthTop=border,BorderWidthBottom=border,CornerRadiusTopLeft=radius,CornerRadiusTopRight=radius,CornerRadiusBottomLeft=radius,CornerRadiusBottomRight=radius,ContentMarginLeft=0,ContentMarginRight=0,ContentMarginTop=0,ContentMarginBottom=0};
    }
    public static void Shield(Control body,SourceHealthBar bar,Vector2 origin,float width,int block,float unit,float textScale,bool phone,bool enemy=false)
    {
        float badgeHeight=(enemy?20*textScale+(phone?4:8):22*textScale+8)*unit;
        float badgeWidth=badgeHeight+16*unit;float shift=block>0?badgeWidth-6*unit:0;bar.Position=origin+new Vector2(shift,0);bar.Size=new Vector2(Math.Max(8*unit,width-shift),bar.Size.Y);
        var old=body.GetNodeOrNull<Control>(bar.Name+"Shield");string key=block+"|"+unit+"|"+textScale;
        if(old!=null&&old.GetMeta("source_shield_signature","").AsString()==key)return;
        if(old!=null){body.RemoveChild(old);old.QueueFree();}if(block<=0)return;
        var badge=new Panel{Name=bar.Name+"Shield",Position=origin-new Vector2(2,4)*unit,Size=new Vector2(badgeWidth,badgeHeight),MouseFilter=Control.MouseFilterEnum.Ignore};
        badge.SetMeta("source_shield_signature",key);badge.SetMeta("displayed_block",block);badge.AddThemeStyleboxOverride("panel",Box(new Color("1f344c"),new Color("2b486a"),unit,1.5f));body.AddChild(badge);
        int size=(int)Math.Round((phone?15:16)*unit*textScale);var font=GD.Load<Font>("res://assets/Fonts/KaiseiDecol-Bold.ttf");float tw=font.GetStringSize(block.ToString(),HorizontalAlignment.Left,-1,size).X;
        float start=(badge.Size.X-16*unit-unit-tw)/2;Icon(badge,"ShieldIcon",SourceIntentGlyphs.Mono("shield"),new Rect2(start,(badgeHeight-16*unit)/2,16*unit,16*unit),new Color("a8c9ea"));
        Text(badge,"ShieldCount",block.ToString(),new Rect2(start+17*unit,0,tw,badgeHeight),size,new Color("a8c9ea"),true);
    }
    public static void LedgerEdge(PanelContainer panel,bool selected,float unit)
    {
        if(panel.GetMeta("source_ledger_skin_unit",-1f).AsSingle()!=unit){panel.AddThemeStyleboxOverride("panel",Box(SourcePalette.Night,SourcePalette.NightEdge,unit));panel.SetMeta("source_ledger_skin_unit",unit);}
        if(panel.GetChildCount()==0||panel.GetChild(0) is not Control body)return;
        var edge=body.GetNodeOrNull<Panel>("SelectedOuterEdge");
        if(edge==null){edge=new Panel{Name="SelectedOuterEdge",ZIndex=-1,MouseFilter=Control.MouseFilterEnum.Ignore};body.AddChild(edge);}
        edge.Position=new Vector2(-3,-3)*unit;edge.Size=panel.Size+Vector2.One*6*unit;edge.Visible=selected;if(edge.GetMeta("source_edge_unit",-1f).AsSingle()!=unit){edge.AddThemeStyleboxOverride("panel",Box(Colors.Transparent,SourcePalette.Brass,unit,3));edge.SetMeta("source_edge_unit",unit);}
    }
    public static void Pile(Button button,string kind,string label,int count,float unit,float textScale,bool phone)
    {
        button.Text="";foreach(string state in new[]{"normal","hover","pressed","disabled","focus","hover_pressed"})button.AddThemeStyleboxOverride(state,Box(SourcePalette.Night,SourcePalette.NightEdge,unit));
        button.RotationDegrees=kind=="discard"?-1:1;button.PivotOffset=button.Size/2;
        var body=new Control{Name="PileDetails",MouseFilter=Control.MouseFilterEnum.Ignore};button.AddChild(body);
        float cw=phone?24:20,ch=phone?34:28,dx=phone?5:4,dy=phone?4:3;float glyphWidth=cw+dx+2;
        if(kind=="exhaust"){Icon(body,"ExhaustIcon",GD.Load<Texture2D>("res://assets/Art/icons/exhaust.png"),new Rect2(12*unit,(button.Size.Y-32*unit)/2,32*unit,32*unit));glyphWidth=32;}
        else
        {
            var host=new Control{Name="PileGlyph",Position=new Vector2(12*unit,(button.Size.Y-(ch+dy+2)*unit)/2),MouseFilter=Control.MouseFilterEnum.Ignore};body.AddChild(host);
            int n=count>=2?2:1;bool back=kind=="draw",empty=count<=0;
            for(int i=0;i<n;i++)
            {
                bool front=i==n-1;float x=back?(n==1?dx/2+1:i==0?1:1+dx):(n==1?dx/2+1:i==0?0:dx+1);
                float y=back?(n==1?dy/2+1:i==0?1+dy:1):dy/2+1;
                var card=new Control{Name="MiniCard_"+i,Position=new Vector2(x,ch+dy+2-y-ch)*unit,Size=new Vector2(cw,ch)*unit,RotationDegrees=back?0:n==1?-6:i==0?8:-6,MouseFilter=Control.MouseFilterEnum.Ignore};host.AddChild(card);
                if(!empty)Fill(card,new Rect2(Vector2.Zero,card.Size),back?SourcePalette.Night:SourcePalette.Paper3);
                Color ink=back?SourcePalette.Brass:empty?SourcePalette.InkSoft:SourcePalette.Ink;float inset=back&&!empty?2*unit:0;
                var r=new Rect2(Vector2.One*inset,card.Size-Vector2.One*2*inset);Frame(card,r,ink,unit);
                if(!empty&&front){if(back)Fill(card,new Rect2(card.Size/2-Vector2.One*2.5f*unit,Vector2.One*5*unit),SourcePalette.Brass);else Fill(card,new Rect2(3*unit,ch*.18f*unit,(cw-6)*unit,ch*.4f*unit),SourcePalette.Window);}
            }
        }
        int fs=(int)Math.Round((phone?19:17)*unit*textScale),ls=(int)Math.Round((phone?15:13)*unit*textScale);float x0=(12+glyphWidth+6)*unit;
        var countText=Text(body,"PileCount",count.ToString(),new Rect2(x0,0,60*unit,button.Size.Y),fs,SourcePalette.Paper,true);
        float tw=countText.GetThemeFont("font").GetStringSize(count.ToString(),HorizontalAlignment.Left,-1,fs).X;
        Text(body,"PileName",label,new Rect2(x0+tw+6*unit,0,Math.Max(0,button.Size.X-x0-tw-12*unit),button.Size.Y),ls,SourcePalette.PaperDim);
    }
    private static void Fill(Control parent,Rect2 rect,Color color){parent.AddChild(new ColorRect{Color=color,Position=rect.Position,Size=rect.Size,MouseFilter=Control.MouseFilterEnum.Ignore});}
    private static void Frame(Control p,Rect2 r,Color c,float u){Fill(p,new Rect2(r.Position,new Vector2(r.Size.X,u)),c);Fill(p,new Rect2(r.Position.X,r.End.Y-u,r.Size.X,u),c);Fill(p,new Rect2(r.Position,new Vector2(u,r.Size.Y)),c);Fill(p,new Rect2(r.End.X-u,r.Position.Y,u,r.Size.Y),c);}
    public static void Pocket(Control parent,float width,float height,float unit)
    {
        string key=$"pocket:{width}:{height}:{unit}";
        if(!Cache.TryGetValue(key,out var texture))
        {
            float k=MathF.Round(Math.Clamp(unit,1,4)*20)/20;int tw=(int)MathF.Ceiling(width*k),th=(int)MathF.Ceiling(height*k);float lw=2*k,r=6*k,dash=4*k,gap=3*k,hx=tw/2f,hy=th/2f,rc=Math.Max(.5f,r-lw/2),sx=Math.Max(0,hx-r),sy=Math.Max(0,hy-r),wc=2*sx,hc=2*sy,arc=MathF.PI/2*rc,perim=2*wc+2*hc+4*arc;
            float period=perim/Math.Max(4,MathF.Round(perim/(dash+gap))),on=period*dash/(dash+gap);var image=Image.CreateEmpty(tw,th,false,Image.Format.Rgba8);
            for(int y=0;y<th;y++)for(int x=0;x<tw;x++)
            {
                float cx=x+.5f-hx,cy=y+.5f-hy,qx=Math.Abs(cx)-sx,qy=Math.Abs(cy)-sy;
                float d=r-MathF.Sqrt(Math.Max(qx,0)*Math.Max(qx,0)+Math.Max(qy,0)*Math.Max(qy,0))-Math.Min(Math.Max(qx,qy),0);
                if(d<-.5f)continue;float ea=Math.Clamp(d+.5f,0,1),cov=ea*Math.Clamp(lw-d+.5f,0,1);
                if(cov>0){float pos;if(qx>0&&qy>0){float phi=MathF.Atan2(cy-(cy>0?sy:-sy),cx-(cx>0?sx:-sx));if(cx>0&&cy>0)pos=wc+(MathF.PI/2-phi)*rc;else if(cx>0)pos=wc+arc+hc-phi*rc;else if(cy<0){if(phi>0)phi-=2*MathF.PI;pos=2*wc+2*arc+hc+(-MathF.PI/2-phi)*rc;}else pos=2*wc+3*arc+2*hc+(MathF.PI-phi)*rc;}else if(hy-Math.Abs(cy)<hx-Math.Abs(cx))pos=cy>0?cx+sx:2*arc+wc+hc+sx-cx;else pos=cx>0?wc+arc+sy-cy:2*wc+3*arc+hc+cy+sy;float m=pos%period;if(m<0)m+=period;cov*=Math.Clamp(Math.Min(m+.5f,on-m+.5f),0,1);}
                float la=cov*.6f,fa=d>=0?.07f*ea:0;Color c=SourcePalette.PaperDim;c.A=la+(1-la)*fa;image.SetPixel(x,th-1-y,c);
            }
            texture=Cache[key]=ImageTexture.CreateFromImage(image);
        }
        var pic=Icon(parent,"DashedPocket",texture,new Rect2(0,0,width*unit,height*unit));pic.TextureFilter=CanvasItem.TextureFilterEnum.Linear;
    }
    public static IReadOnlyList<(string Icon,string Text)> Resources(GameState st)
    {
        var p=st.Player;var r=new List<(string,string)>();
        if(st.PlayCap is int cap)r.Add((null,"首輪 あと"+Math.Max(0,cap-(p.PlaysThisTurn??0))+"枚"));
        if(p.Growth>0)r.Add(("growth","成長 "+p.Growth));if(p.Momentum>0)r.Add(("momentum","勢い "+p.Momentum));if(p.Aether>0)r.Add(("energy","霊気 "+p.Aether));
        if(p.SparksPlayedThisCombat>0)r.Add(("draw","火種 "+p.SparksPlayedThisCombat+"枚"));if(p.NextCardDiscount>0)r.Add(("energy","次のカード -"+p.NextCardDiscount));if(p.SpellEchoes>0)r.Add(("draw","反復 "+p.SpellEchoes));
        if(p.Weak>0)r.Add(("weak","弱体 "+p.Weak+"T"));if(p.Vulnerable>0)r.Add(("exposed","脆弱 "+p.Vulnerable+"T"));if(p.Frail>0)r.Add(("frail","虚弱 "+p.Frail+"T"));if(p.Restrain>0)r.Add((null,"拘束 "+p.Restrain+"T"));if(p.Mist>0)r.Add(("draw","霞み "+p.Mist+"T"));if(p.Slow>0)r.Add(("exposed","重り "+p.Slow+"T"));return r;
    }
}

// Reconstructs Unity's compact night intent tags, including authored art, semantic
// rider glyphs/colours, common stacking and source nine-slice/tail geometry.
// Uses 4bdf5d4 source; a7f86b5 battle fixtures verify geometry. Not a historical blob.
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
namespace DeckRogue.GodotPort.UI;
public static class SourceIntentTag
{
    private static readonly Color NightHi=new("262943");
    private static readonly Dictionary<string,Texture2D> Textures=new();
    private static Font _numberFont,_riderFont;
    public static void Update(PanelContainer panel,Label number,SourceIntentLayout.Spec spec,float unit,bool phone,float textScale)
    {
        if(spec==null){panel.Visible=false;number.Text="";return;}
        panel.Visible=true;
        string signature=spec.Kind+"|"+spec.Caption+"|"+spec.Minor+"|"+spec.Stack+"|"+spec.Width+"|"+spec.Height+"|"+unit+"|"+textScale+"|"+string.Join("|",spec.Riders.Select(r=>r.ToString()));
        // Legacy contact projection may update this shared label between renders.
        number.Text=spec.Caption;
        if(panel.GetMeta("source_intent_signature","").AsString()==signature)return;
        panel.SetMeta("source_intent_signature",signature);
        if(number.GetParent()!=null)number.GetParent().RemoveChild(number);
        foreach(Node child in panel.GetChildren()){panel.RemoveChild(child);child.QueueFree();}
        panel.AddThemeStyleboxOverride("panel",new StyleBoxEmpty());
        panel.Size=new Vector2(spec.Width,spec.Height)*unit;
        panel.SetMeta("source_intent_width",panel.Size.X);panel.SetMeta("source_intent_height",panel.Size.Y);
        panel.SetMeta("source_intent_minor",spec.Minor);panel.SetMeta("source_intent_stack",spec.Stack);
        panel.SetMeta("source_intent_riders",spec.Riders.Count);panel.SetMeta("source_intent_unit",unit);
        var body=new Control{Name="IntentParts",MouseFilter=Control.MouseFilterEnum.Ignore};panel.AddChild(body);
        var edge=Sheet(body,"IntentEdge",new Rect2(-3*unit,-3*unit,(spec.Width+6)*unit,(spec.Height+6)*unit),SourcePalette.Brass,SourcePalette.Brass,2,unit);edge.Visible=false;
        Sheet(body,"IntentPaper",new Rect2(Vector2.Zero,panel.Size),spec.Minor?SourcePalette.Night:NightHi,spec.Minor?SourcePalette.NightEdge:SourcePalette.Paper2,2,unit);
        var tail=Picture(body,"IntentTail",Tail(spec.Minor),new Rect2((spec.Width/2-7)*unit,(spec.Height-3)*unit,14*unit,14*unit),false);
        _numberFont??=GD.Load<Font>("res://assets/Fonts/KaiseiDecol-Bold.ttf");
        _riderFont??=GD.Load<Font>("res://assets/Fonts/KleeOne-SemiBold.ttf");
        int fs=(int)Math.Round(spec.FontSize*unit*textScale),rfs=(int)Math.Round(15*unit*textScale);
        number.AddThemeFontSizeOverride("font_size",fs);
        SourceHudLabel.NightHalo(number,_numberFont,unit,textScale,2,deferSmallUnderlay:true);
        number.AddThemeColorOverride("font_color",spec.Kind is "buff" or "rally"?SourcePalette.BrassLight:SourcePalette.Paper);
        number.HorizontalAlignment=HorizontalAlignment.Left;number.VerticalAlignment=VerticalAlignment.Center;number.MouseFilter=Control.MouseFilterEnum.Ignore;
        body.AddChild(number);
        float textWidth=number.GetThemeFont("font").GetStringSize(spec.Caption,HorizontalAlignment.Left,-1,fs).X;
        float gap=(phone?6:10)*unit,icon=spec.IconSize*unit,rowHeight=spec.RowHeight*unit;
        // Source HorizontalLayoutGroup centres the actual glyph advances in the
        // estimated-width outer tag. Keep those two measurements distinct.
        var riderWidths=spec.Riders.Select(r=>_riderFont.GetStringSize(r.Text,HorizontalAlignment.Left,-1,rfs).X+(r.Icon==null?8:25)*unit).ToArray();
        float ridersWidth=riderWidths.Sum()+Math.Max(0,riderWidths.Length-1)*(spec.Stack?4:phone?6:10)*unit;
        float rowWidth=icon+gap+textWidth+(!spec.Stack&&riderWidths.Length>0?gap+ridersWidth:0);
        float x=(panel.Size.X-rowWidth)/2;
        var art=Load("intent_"+spec.Kind);
        if(art==null)art=SourceIntentGlyphs.Mono(FallbackIcon(spec.Kind));
        Picture(body,"IntentIcon",art,new Rect2(x,(rowHeight-icon)/2,icon,icon),true);
        var textRect=new Rect2(x+icon+gap,0,Math.Max(14*unit,textWidth),rowHeight);
        if(number is SourceHudLabel precise){precise.FollowParentRect=false;precise.SourceTextRect=textRect;precise.RefreshSourceAlignment();}
        else {number.Position=textRect.Position;number.Size=textRect.Size;}
        float rx=spec.Stack?(panel.Size.X-ridersWidth)/2:x+icon+gap+textWidth+gap;
        float ph=(phone?22:24)*unit*textScale;
        float ry=spec.Stack?panel.Size.Y-(6*unit*textScale+ph):(rowHeight-ph)/2;
        for(int i=0;i<spec.Riders.Count;i++)
        {
            var r=spec.Riders[i];float rw=riderWidths[i];Color fill=new(r.Fill),ink=new(r.Ink);
            var pill=new Control{Name="IntentRider_"+i,Position=new Vector2(rx,ry),Size=new Vector2(rw,ph),MouseFilter=Control.MouseFilterEnum.Ignore};body.AddChild(pill);
            Sheet(pill,"RiderPaper",new Rect2(Vector2.Zero,pill.Size),fill,new Color(fill.R/.72f,fill.G/.72f,fill.B/.72f),1.5f,unit);
            float tx=2*unit;
            if(r.Icon!=null){var pic=Picture(pill,"RiderIcon_"+r.Icon,SourceIntentGlyphs.Mono(r.Icon),new Rect2(2*unit,(ph-16*unit)/2,16*unit,16*unit),true);pic.Modulate=ink;tx+=17*unit;}
            var label=new SourceHudLabel{Name="RiderText",Text=r.Text,SourceTextRect=new Rect2(tx,0,rw-tx-6*unit,ph),HorizontalAlignment=HorizontalAlignment.Center,MouseFilter=Control.MouseFilterEnum.Ignore};
            label.AddThemeFontOverride("font",_riderFont);label.AddThemeFontSizeOverride("font_size",rfs);label.AddThemeColorOverride("font_color",ink);pill.AddChild(label);label.RefreshSourceAlignment();rx+=rw+(spec.Stack?4:phone?6:10)*unit;
        }
    }
    public static void Highlight(PanelContainer panel,bool selected)
    {
        if(panel.GetNodeOrNull<CanvasItem>("IntentParts/IntentEdge") is {} edge)edge.Visible=selected;
    }
    private static TextureRect Picture(Control parent,string name,Texture2D texture,Rect2 rect,bool nearest)
    {
        var p=new TextureRect{Name=name,ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,Texture=texture,Position=rect.Position,Size=rect.Size,StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,TextureFilter=nearest?CanvasItem.TextureFilterEnum.Nearest:CanvasItem.TextureFilterEnum.Linear,MouseFilter=Control.MouseFilterEnum.Ignore};parent.AddChild(p);return p;
    }
    private static NinePatchRect Sheet(Control parent,string name,Rect2 rect,Color fill,Color edge,float line,float unit)
    {
        var p=new NinePatchRect{Name=name,Position=rect.Position,Size=rect.Size,Texture=Nine(fill,edge,line),RegionRect=new Rect2(0,0,32,32),TextureFilter=CanvasItem.TextureFilterEnum.Linear,MouseFilter=Control.MouseFilterEnum.Ignore};
        foreach(Side s in Enum.GetValues<Side>())p.SetPatchMargin(s,10);p.Scale=Vector2.One;parent.AddChild(p);
        // Texture margins are texels; draw margins scale with the source canvas.
        p.Size=rect.Size/unit;p.Position=rect.Position;p.Scale=Vector2.One*unit;return p;
    }
    private static Texture2D Nine(Color fill,Color edge,float line)
    {
        string key=fill.ToHtml()+edge.ToHtml()+line;if(Textures.TryGetValue(key,out var t))return t;
        var image=Image.CreateEmpty(32,32,false,Image.Format.Rgba8);
        for(int y=0;y<32;y++)for(int x=0;x<32;x++)
        {
            float qx=Math.Abs(x+.5f-16)-8,qy=Math.Abs(y+.5f-16)-8;
            float d=8-MathF.Sqrt(Math.Max(qx,0)*Math.Max(qx,0)+Math.Max(qy,0)*Math.Max(qy,0))-Math.Min(Math.Max(qx,qy),0);
            Color c=d<0?Colors.Transparent:d<line?edge:fill;if(d>=0&&d<1)c.A*=Math.Clamp(d+.5f,0,1);image.SetPixel(x,y,c);
        }
        return Textures[key]=ImageTexture.CreateFromImage(image);
    }
    private static Texture2D Tail(bool minor)
    {
        string key="tail"+minor;if(Textures.TryGetValue(key,out var t))return t;
        var image=Image.CreateEmpty(30,22,false,Image.Format.Rgba8);
        for(int y=0;y<22;y++)for(int x=0;x<30;x++)
        {
            float k=1-(y+.5f)/22,left=Mathf.Lerp(2,8,k),right=Mathf.Lerp(26,12,k);bool inside=x+.5f>left&&x+.5f<right;
            bool edge=inside&&(x+.5f<left+2||x+.5f>right-2||y<2);Color c=!inside?Colors.Transparent:edge?SourcePalette.Paper2:NightHi;
            if(minor)c*=new Color(SourcePalette.Paper2.R/SourcePalette.Paper.R,SourcePalette.Paper2.G/SourcePalette.Paper.G,SourcePalette.Paper2.B/SourcePalette.Paper.B);
            image.SetPixel(x,21-y,c);
        }
        return Textures[key]=ImageTexture.CreateFromImage(image);
    }
    private static Texture2D Load(string name)
    {
        string path="res://assets/Art/icons/"+name+".png";return ResourceLoader.Exists(path)?GD.Load<Texture2D>(path):null;
    }
    private static string FallbackIcon(string kind)=>kind switch{"attack"=>"sword","defend"=>"shield","buff" or "rally"=>"strength_up","heal"=>"heart","hex"=>"burn","destroy-set" or "destroy-token"=>"exhaust","mill"=>"draw",_=>"question"};
}

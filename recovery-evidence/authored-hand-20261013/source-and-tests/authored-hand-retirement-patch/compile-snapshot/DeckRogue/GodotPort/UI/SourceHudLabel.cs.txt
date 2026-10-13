using Godot;
using System;
namespace DeckRogue.GodotPort.UI;

/// <summary>Source TMP midline placement without mistaking Godot's line-height
/// minimum for painted glyph height. The label remains native, selectable by
/// accessibility tools, and uses the original font/semantic text.</summary>
public partial class SourceHudLabel : Label
{
    private Rect2 _sourceTextRect;
    public Rect2 SourceTextRect { get=>FollowParentRect && GetParent() is Control p?new Rect2(Vector2.Zero,p.Size):_sourceTextRect;set=>_sourceTextRect=value; }
    public bool FollowParentRect { get; set; }
    public Rect2 PaintedGlyphBoundsInParent { get; private set; }
    private string _measuredText="";
    private ulong _measuredFont;
    private int _measuredSize;
    private Rect2 _glyphs;
    public override void _Ready() { RefreshSourceAlignment(); }
    public override void _Process(double delta) { RefreshSourceAlignment(); }
    public void RefreshSourceAlignment()
    {
        if(!IsInsideTree())return;
        var font=GetThemeFont("font");int size=GetThemeFontSize("font_size");
        if(font==null || size<=0)return;
        var rect=FollowParentRect && GetParent() is Control p?new Rect2(Vector2.Zero,p.Size):SourceTextRect;
        if(rect.Size.X<=0 || rect.Size.Y<=0)return;
        if(_measuredText!=Text || _measuredFont!=font.GetInstanceId() || _measuredSize!=size)
        {
            _measuredText=Text;_measuredFont=font.GetInstanceId();_measuredSize=size;
            _glyphs=MeasureGlyphs(font,size,Text);
        }
        Size=new Vector2(rect.Size.X,Math.Max(rect.Size.Y,GetCombinedMinimumSize().Y));
        // GetCharacterBounds supplies the shaped line's local top; the glyph
        // bitmap offset supplies its actual ascent/descent around the baseline.
        float baseline=(Text.Length>0?GetCharacterBounds(0).Position.Y:0)+font.GetAscent(size);
        float y=rect.GetCenter().Y-baseline-_glyphs.GetCenter().Y;
        Position=new Vector2(rect.Position.X,y);
        float advance=font.GetStringSize(Text,HorizontalAlignment.Left,-1,size).X;
        float x=HorizontalAlignment==HorizontalAlignment.Center?(Size.X-advance)/2:HorizontalAlignment==HorizontalAlignment.Right?Size.X-advance:0;
        var ink=new Rect2(Position+new Vector2(x,baseline)+_glyphs.Position,_glyphs.Size);
        int halo=GetThemeColor("font_shadow_color").A>0?GetThemeConstant("shadow_outline_size"):0;
        PaintedGlyphBoundsInParent=ink.Grow(halo);
    }
    public static Rect2 MeasureGlyphs(Font font,int size,string text)
    {
        var server=TextServerManager.GetPrimaryInterface();var rids=font.GetRids();
        float left=float.PositiveInfinity,top=float.PositiveInfinity,right=float.NegativeInfinity,bottom=float.NegativeInfinity;
        for(int i=0;i<text.Length;i++)
        {
            char c=text[i];if(char.IsWhiteSpace(c))continue;
            foreach(var rid in rids)
            {
                long glyph=server.FontGetGlyphIndex(rid,size,c,0);if(glyph==0)continue;
                var offset=server.FontGetGlyphOffset(rid,new Vector2I(size,0),glyph);
                var extent=server.FontGetGlyphSize(rid,new Vector2I(size,0),glyph);if(extent.X<=0||extent.Y<=0)break;
                float advance=i==0?0:font.GetStringSize(text.Substring(0,i),HorizontalAlignment.Left,-1,size).X;
                left=Math.Min(left,advance+offset.X);right=Math.Max(right,advance+offset.X+extent.X);
                top=Math.Min(top,offset.Y);bottom=Math.Max(bottom,offset.Y+extent.Y);break;
            }
        }
        return float.IsPositiveInfinity(left)?new Rect2():new Rect2(left,top,right-left,bottom-top);
    }
    public static Font Spaced(Font font,int size,float hundredthsEm)
    {
        // TMP characterSpacing uses1/100em. Godot's integer spacing rounds to
        // the nearest logical pixel; this local resource never alters the font.
        return new FontVariation { BaseFont=font,SpacingGlyph=(int)Math.Round(size*hundredthsEm*.01f),ResourceName=font.ResourcePath.GetFile()+" source-tracking" };
    }
    public static void NightHalo(Label label,Font font,float unit,float textScale,float tracking=0,bool deferSmallUnderlay=false)
    {
        int size=label.GetThemeFontSize("font_size");
        // Preserve the source face/size. A controlled native100%/130% comparison
        // showed Godot's zero-offset shadow hollowing these small bitmap strokes.
        // Do not compensate by enlarging/overboldening the glyph. The bright core
        // is retained without that broken shadow; exact TMP underlay is deferred.
        // See tests/ui/typography/hp-ink-probe-r2.png and README.md.
        var local=new FontVariation {BaseFont=font,VariationEmbolden=.15f,SpacingGlyph=(int)Math.Round(size*tracking*.01f),ResourceName=font.ResourcePath.GetFile()+" NumHaloNight"};
        label.AddThemeFontOverride("font",local);
        label.AddThemeColorOverride("font_shadow_color",deferSmallUnderlay?Colors.Transparent:SourcePalette.Ground);
        label.AddThemeConstantOverride("shadow_offset_x",0);label.AddThemeConstantOverride("shadow_offset_y",0);
        label.AddThemeConstantOverride("shadow_outline_size",deferSmallUnderlay?0:Math.Max(1,(int)Math.Round(2*unit*textScale)));
        label.SetMeta("source_underlay_deferred",deferSmallUnderlay);
        label.AddThemeConstantOverride("outline_size",0);
    }
}

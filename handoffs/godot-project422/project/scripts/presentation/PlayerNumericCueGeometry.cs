using System;
using Godot;
namespace DeckRogue.GodotPort.Presentation;

// Godot4.6.3 Label3D::_generate_glyph_surfaces: native shaped glyph raster quads,
// including actual outline layer. No Control alignment rounding or canvas snap here.
public static class PlayerNumericCueGeometry
{
    public static Rect2 MeasureLocal(Label3D label)
    {
        if(label.Font==null || label.Text.Contains('\n') || label.HorizontalAlignment!=HorizontalAlignment.Center || label.VerticalAlignment!=VerticalAlignment.Center)
            throw new InvalidOperationException("Player numeric geometry requires the existing centered single-line cue style");
        var ts=TextServerManager.GetPrimaryInterface();
        using var line=new TextLine {Direction=(TextServer.Direction)(int)label.TextDirection,Orientation=TextServer.Orientation.Horizontal};
        string language=string.IsNullOrEmpty(label.Language)?TranslationServer.GetLocale():label.Language;
        string text=label.Atr(label.Text);if(label.Uppercase)text=ts.StringToUpper(text,language);
        if(!line.AddString(text,label.Font,label.FontSize,language))throw new InvalidOperationException("Player numeric shape failed");
        var rid=line.GetRid();Vector2 size=ts.ShapedTextGetSize(rid);
        float baseline=size.Y*.5f-(float)ts.ShapedTextGetAscent(rid);
        Vector2 lo=new(float.PositiveInfinity,float.PositiveInfinity),hi=-lo;
        foreach(int outline in new[]{0,label.OutlineModulate.A!=0?label.OutlineSize:0})
        {
            float x=-size.X*.5f;
            foreach(var g in ts.ShapedTextGetGlyphs(rid))
            {
                long index=g["index"].AsInt64();int repeat=g["repeat"].AsInt32();float advance=g["advance"].AsSingle();
                Rid font=g["font_rid"].AsRid();int fontSize=g["font_size"].AsInt32();
                if(index==0){x+=advance*repeat;continue;}
                if(!font.IsValid)throw new InvalidOperationException("Player numeric glyph font missing");
                var cache=new Vector2I(fontSize,outline);
                var texture=ts.FontGetGlyphTextureRid(font,cache,index);
                if(!texture.IsValid){x+=advance*repeat;continue;} // Engine emits no quad for spaces/nonvisual glyphs.
                Vector2 offset=ts.FontGetGlyphOffset(font,cache,index)+g["offset"].AsVector2();
                Vector2 extent=ts.FontGetGlyphSize(font,cache,index);var uv=ts.FontGetGlyphUVRect(font,cache,index);
                for(int k=0;k<repeat;k++)
                {
                    if(uv.Size.X>2 && uv.Size.Y>2)
                    {
                        Vector2 a=new(x+offset.X,baseline-offset.Y-extent.Y),b=new(x+offset.X+extent.X,baseline-offset.Y);
                        lo=lo.Min(a);hi=hi.Max(b);
                    }
                    x+=advance;
                }
            }
        }
        if(!lo.IsFinite()||!hi.IsFinite()||hi.X<=lo.X||hi.Y<=lo.Y)throw new InvalidOperationException("Player numeric ink is empty");
        return new Rect2((lo+label.Offset)*label.PixelSize,(hi-lo)*label.PixelSize);
    }

    public static Rect2 Project(Camera3D camera,Vector3 origin,Rect2 local)
    {
        Basis basis=camera.GlobalBasis.Orthonormalized();Vector2 lo=new(float.PositiveInfinity,float.PositiveInfinity),hi=-lo;
        foreach(Vector2 p in new[]{local.Position,local.End,new Vector2(local.Position.X,local.End.Y),new Vector2(local.End.X,local.Position.Y)})
        {Vector2 v=camera.UnprojectPosition(origin+basis.X*p.X+basis.Y*p.Y);lo=lo.Min(v);hi=hi.Max(v);}
        return new Rect2(lo,hi-lo);
    }
}

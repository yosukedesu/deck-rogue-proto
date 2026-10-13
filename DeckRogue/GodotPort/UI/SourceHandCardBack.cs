// Full-card overlay from Unity BattleView.CardBack. Never receives GUI input.
using Godot;
using System;
namespace DeckRogue.GodotPort.UI;
public static class SourceHandCardBack
{
    public const string NodeName="SourceHandDealBack";
    static Texture2D _art;
    static bool _loaded;
    static ShaderMaterial _rounded;
    public static Control Add(CardTile tile)
    {
        Remove(tile);
        var back=new Control{Name=NodeName,Size=new Vector2(200,290),MouseFilter=Control.MouseFilterEnum.Ignore};
        tile.AddChild(back);
        if(!_loaded){_loaded=true;const string path="res://assets/Art/ui/cardback.png";if(ResourceLoader.Exists(path))_art=GD.Load<Texture2D>(path);}
        if(_art!=null)
        {
            // PaperFx.Card is a 52px nine-slice with radius14 and border16;
            // its full-card mask retains a 14-unit rounded corner.
            if(_rounded==null)_rounded=new ShaderMaterial{Shader=new Shader{Code=@"
shader_type canvas_item;
void fragment(){
    vec2 q=abs(UV*vec2(200.0,290.0)-vec2(100.0,145.0))-vec2(86.0,131.0);
    float d=length(max(q,vec2(0.0)))+min(max(q.x,q.y),0.0)-14.0;
    // Godot fragment COLOR already contains the default texture sample.
    COLOR.a*=clamp(0.5-d,0.0,1.0);
}"}};
            back.AddChild(new TextureRect{Name="Art",ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,Texture=_art,Size=back.Size,StretchMode=TextureRect.StretchModeEnum.Scale,TextureFilter=CanvasItem.TextureFilterEnum.Linear,Material=_rounded,MouseFilter=Control.MouseFilterEnum.Ignore});
        }
        else
        {
            // Source fallback when the authored art is unavailable. Keep the
            // failure visible in logs so the native gate verifies the real art.
            GD.PushWarning("Hand deal: authored cardback is unavailable; using source-shaped fallback.");
            var panel=new Panel{Size=back.Size,MouseFilter=Control.MouseFilterEnum.Ignore};
            panel.AddThemeStyleboxOverride("panel",new StyleBoxFlat{BgColor=SourcePalette.Window,CornerRadiusTopLeft=14,CornerRadiusTopRight=14,CornerRadiusBottomLeft=14,CornerRadiusBottomRight=14});back.AddChild(panel);
            Color brass=new Color(SourcePalette.Brass,.8f);
            foreach(var r in new[]{new Rect2(10,10,180,2),new Rect2(10,278,180,2),new Rect2(10,10,2,270),new Rect2(188,10,2,270)})
                back.AddChild(new ColorRect{Position=r.Position,Size=r.Size,Color=brass,MouseFilter=Control.MouseFilterEnum.Ignore});
            const string starPath="res://assets/Art/icons/star.png";
            if(ResourceLoader.Exists(starPath))back.AddChild(new TextureRect{Name="Star",ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,Texture=GD.Load<Texture2D>(starPath),Position=new Vector2(58,103),Size=new Vector2(84,84),StretchMode=TextureRect.StretchModeEnum.KeepAspectCentered,Modulate=new Color(SourcePalette.Brass,.55f),MouseFilter=Control.MouseFilterEnum.Ignore});
        }
        return back;
    }
    public static void Raise(CardTile tile)
    {
        var back=tile.GetNodeOrNull<Control>(NodeName);
        if(back!=null)tile.MoveChild(back,tile.GetChildCount()-1);
    }
    public static void Remove(CardTile tile)
    {
        if(tile==null||!GodotObject.IsInstanceValid(tile))return;
        var back=tile.GetNodeOrNull<Control>(NodeName);
        if(back!=null){tile.RemoveChild(back);back.QueueFree();}
    }
}

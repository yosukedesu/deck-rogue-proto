using Godot;
using Godot.NativeInterop;

namespace DeckRogue.GodotPort.UI {

partial class SourceHudLabel
{
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override void SaveGodotObjectData(global::Godot.Bridge.GodotSerializationInfo info)
    {
        base.SaveGodotObjectData(info);
        info.AddProperty(PropertyName.@SourceTextRect, global::Godot.Variant.From<global::Godot.Rect2>(this.@SourceTextRect));
        info.AddProperty(PropertyName.@FollowParentRect, global::Godot.Variant.From<bool>(this.@FollowParentRect));
        info.AddProperty(PropertyName.@PaintedGlyphBoundsInParent, global::Godot.Variant.From<global::Godot.Rect2>(this.@PaintedGlyphBoundsInParent));
        info.AddProperty(PropertyName.@_sourceTextRect, global::Godot.Variant.From<global::Godot.Rect2>(this.@_sourceTextRect));
        info.AddProperty(PropertyName.@_measuredText, global::Godot.Variant.From<string>(this.@_measuredText));
        info.AddProperty(PropertyName.@_measuredFont, global::Godot.Variant.From<ulong>(this.@_measuredFont));
        info.AddProperty(PropertyName.@_measuredSize, global::Godot.Variant.From<int>(this.@_measuredSize));
        info.AddProperty(PropertyName.@_glyphs, global::Godot.Variant.From<global::Godot.Rect2>(this.@_glyphs));
    }
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override void RestoreGodotObjectData(global::Godot.Bridge.GodotSerializationInfo info)
    {
        base.RestoreGodotObjectData(info);
        if (info.TryGetProperty(PropertyName.@SourceTextRect, out var _value_SourceTextRect))
            this.@SourceTextRect = _value_SourceTextRect.As<global::Godot.Rect2>();
        if (info.TryGetProperty(PropertyName.@FollowParentRect, out var _value_FollowParentRect))
            this.@FollowParentRect = _value_FollowParentRect.As<bool>();
        if (info.TryGetProperty(PropertyName.@PaintedGlyphBoundsInParent, out var _value_PaintedGlyphBoundsInParent))
            this.@PaintedGlyphBoundsInParent = _value_PaintedGlyphBoundsInParent.As<global::Godot.Rect2>();
        if (info.TryGetProperty(PropertyName.@_sourceTextRect, out var _value__sourceTextRect))
            this.@_sourceTextRect = _value__sourceTextRect.As<global::Godot.Rect2>();
        if (info.TryGetProperty(PropertyName.@_measuredText, out var _value__measuredText))
            this.@_measuredText = _value__measuredText.As<string>();
        if (info.TryGetProperty(PropertyName.@_measuredFont, out var _value__measuredFont))
            this.@_measuredFont = _value__measuredFont.As<ulong>();
        if (info.TryGetProperty(PropertyName.@_measuredSize, out var _value__measuredSize))
            this.@_measuredSize = _value__measuredSize.As<int>();
        if (info.TryGetProperty(PropertyName.@_glyphs, out var _value__glyphs))
            this.@_glyphs = _value__glyphs.As<global::Godot.Rect2>();
    }
}

}

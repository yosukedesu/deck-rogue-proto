using Godot;
using Godot.NativeInterop;

namespace DeckRogue.GodotPort.UI {

partial class SourceCardBody
{
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override void SaveGodotObjectData(global::Godot.Bridge.GodotSerializationInfo info)
    {
        base.SaveGodotObjectData(info);
        info.AddProperty(PropertyName.@_text, global::Godot.Variant.From<global::Godot.RichTextLabel>(this.@_text));
        info.AddProperty(PropertyName.@_markup, global::Godot.Variant.From<string>(this.@_markup));
        info.AddProperty(PropertyName.@_scale, global::Godot.Variant.From<float>(this.@_scale));
    }
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override void RestoreGodotObjectData(global::Godot.Bridge.GodotSerializationInfo info)
    {
        base.RestoreGodotObjectData(info);
        if (info.TryGetProperty(PropertyName.@_text, out var _value__text))
            this.@_text = _value__text.As<global::Godot.RichTextLabel>();
        if (info.TryGetProperty(PropertyName.@_markup, out var _value__markup))
            this.@_markup = _value__markup.As<string>();
        if (info.TryGetProperty(PropertyName.@_scale, out var _value__scale))
            this.@_scale = _value__scale.As<float>();
    }
}

}

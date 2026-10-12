using Godot;
using Godot.NativeInterop;

namespace DeckRogue.GodotPort.UI {

partial class RunMap
{
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override void SaveGodotObjectData(global::Godot.Bridge.GodotSerializationInfo info)
    {
        base.SaveGodotObjectData(info);
        info.AddProperty(PropertyName.@_drawing, global::Godot.Variant.From<bool>(this.@_drawing));
        info.AddProperty(PropertyName.@_pen, global::Godot.Variant.From<string>(this.@_pen));
    }
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override void RestoreGodotObjectData(global::Godot.Bridge.GodotSerializationInfo info)
    {
        base.RestoreGodotObjectData(info);
        if (info.TryGetProperty(PropertyName.@_drawing, out var _value__drawing))
            this.@_drawing = _value__drawing.As<bool>();
        if (info.TryGetProperty(PropertyName.@_pen, out var _value__pen))
            this.@_pen = _value__pen.As<string>();
    }
}

}

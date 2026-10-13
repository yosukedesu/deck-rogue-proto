using Godot;
using Godot.NativeInterop;

namespace DeckRogue.GodotPort.UI {

partial class SourceAimArrow
{
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override void SaveGodotObjectData(global::Godot.Bridge.GodotSerializationInfo info)
    {
        base.SaveGodotObjectData(info);
        info.AddProperty(PropertyName.@VisibleDots, global::Godot.Variant.From<int>(this.@VisibleDots));
        info.AddProperty(PropertyName.@Active, global::Godot.Variant.From<bool>(this.@Active));
        info.AddProperty(PropertyName.@Start, global::Godot.Variant.From<global::Godot.Vector2>(this.@Start));
        info.AddProperty(PropertyName.@End, global::Godot.Variant.From<global::Godot.Vector2>(this.@End));
        info.AddProperty(PropertyName.@Unit, global::Godot.Variant.From<float>(this.@Unit));
        info.AddProperty(PropertyName.@Note, global::Godot.Variant.From<string>(this.@Note));
    }
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override void RestoreGodotObjectData(global::Godot.Bridge.GodotSerializationInfo info)
    {
        base.RestoreGodotObjectData(info);
        if (info.TryGetProperty(PropertyName.@VisibleDots, out var _value_VisibleDots))
            this.@VisibleDots = _value_VisibleDots.As<int>();
        if (info.TryGetProperty(PropertyName.@Active, out var _value_Active))
            this.@Active = _value_Active.As<bool>();
        if (info.TryGetProperty(PropertyName.@Start, out var _value_Start))
            this.@Start = _value_Start.As<global::Godot.Vector2>();
        if (info.TryGetProperty(PropertyName.@End, out var _value_End))
            this.@End = _value_End.As<global::Godot.Vector2>();
        if (info.TryGetProperty(PropertyName.@Unit, out var _value_Unit))
            this.@Unit = _value_Unit.As<float>();
        if (info.TryGetProperty(PropertyName.@Note, out var _value_Note))
            this.@Note = _value_Note.As<string>();
    }
}

}

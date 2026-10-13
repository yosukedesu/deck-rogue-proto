using Godot;
using Godot.NativeInterop;

namespace DeckRogue.GodotPort.UI {

partial class EnemyDropTarget
{
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override void SaveGodotObjectData(global::Godot.Bridge.GodotSerializationInfo info)
    {
        base.SaveGodotObjectData(info);
        info.AddProperty(PropertyName.@_holding, global::Godot.Variant.From<bool>(this.@_holding));
        info.AddProperty(PropertyName.@_inspected, global::Godot.Variant.From<bool>(this.@_inspected));
        info.AddProperty(PropertyName.@_held, global::Godot.Variant.From<double>(this.@_held));
        info.AddProperty(PropertyName.@_pressAt, global::Godot.Variant.From<global::Godot.Vector2>(this.@_pressAt));
    }
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override void RestoreGodotObjectData(global::Godot.Bridge.GodotSerializationInfo info)
    {
        base.RestoreGodotObjectData(info);
        if (info.TryGetProperty(PropertyName.@_holding, out var _value__holding))
            this.@_holding = _value__holding.As<bool>();
        if (info.TryGetProperty(PropertyName.@_inspected, out var _value__inspected))
            this.@_inspected = _value__inspected.As<bool>();
        if (info.TryGetProperty(PropertyName.@_held, out var _value__held))
            this.@_held = _value__held.As<double>();
        if (info.TryGetProperty(PropertyName.@_pressAt, out var _value__pressAt))
            this.@_pressAt = _value__pressAt.As<global::Godot.Vector2>();
    }
}

}

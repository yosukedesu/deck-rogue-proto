using Godot;
using Godot.NativeInterop;

namespace DeckRogue.GodotPort.UI {

partial class SourceEnergyOrb
{
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override void SaveGodotObjectData(global::Godot.Bridge.GodotSerializationInfo info)
    {
        base.SaveGodotObjectData(info);
        info.AddProperty(PropertyName.@Energy, global::Godot.Variant.From<int>(this.@Energy));
        info.AddProperty(PropertyName.@Maximum, global::Godot.Variant.From<int>(this.@Maximum));
        info.AddProperty(PropertyName.@TextScale, global::Godot.Variant.From<float>(this.@TextScale));
    }
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override void RestoreGodotObjectData(global::Godot.Bridge.GodotSerializationInfo info)
    {
        base.RestoreGodotObjectData(info);
        if (info.TryGetProperty(PropertyName.@Energy, out var _value_Energy))
            this.@Energy = _value_Energy.As<int>();
        if (info.TryGetProperty(PropertyName.@Maximum, out var _value_Maximum))
            this.@Maximum = _value_Maximum.As<int>();
        if (info.TryGetProperty(PropertyName.@TextScale, out var _value_TextScale))
            this.@TextScale = _value_TextScale.As<float>();
    }
}

}

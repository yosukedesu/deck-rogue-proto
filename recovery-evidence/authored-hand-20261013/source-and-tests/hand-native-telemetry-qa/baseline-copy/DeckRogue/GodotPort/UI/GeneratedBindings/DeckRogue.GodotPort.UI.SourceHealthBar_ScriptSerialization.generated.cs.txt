using Godot;
using Godot.NativeInterop;

namespace DeckRogue.GodotPort.UI {

partial class SourceHealthBar
{
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override void SaveGodotObjectData(global::Godot.Bridge.GodotSerializationInfo info)
    {
        base.SaveGodotObjectData(info);
        info.AddProperty(PropertyName.@Hp, global::Godot.Variant.From<int>(this.@Hp));
        info.AddProperty(PropertyName.@Maximum, global::Godot.Variant.From<int>(this.@Maximum));
        info.AddProperty(PropertyName.@Block, global::Godot.Variant.From<int>(this.@Block));
        info.AddProperty(PropertyName.@IceBlock, global::Godot.Variant.From<int>(this.@IceBlock));
        info.AddProperty(PropertyName.@PredictedLoss, global::Godot.Variant.From<int>(this.@PredictedLoss));
    }
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override void RestoreGodotObjectData(global::Godot.Bridge.GodotSerializationInfo info)
    {
        base.RestoreGodotObjectData(info);
        if (info.TryGetProperty(PropertyName.@Hp, out var _value_Hp))
            this.@Hp = _value_Hp.As<int>();
        if (info.TryGetProperty(PropertyName.@Maximum, out var _value_Maximum))
            this.@Maximum = _value_Maximum.As<int>();
        if (info.TryGetProperty(PropertyName.@Block, out var _value_Block))
            this.@Block = _value_Block.As<int>();
        if (info.TryGetProperty(PropertyName.@IceBlock, out var _value_IceBlock))
            this.@IceBlock = _value_IceBlock.As<int>();
        if (info.TryGetProperty(PropertyName.@PredictedLoss, out var _value_PredictedLoss))
            this.@PredictedLoss = _value_PredictedLoss.As<int>();
    }
}

}

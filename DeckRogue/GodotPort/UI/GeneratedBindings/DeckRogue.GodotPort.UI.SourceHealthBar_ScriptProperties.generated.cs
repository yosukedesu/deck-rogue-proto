using Godot;
using Godot.NativeInterop;

namespace DeckRogue.GodotPort.UI {

partial class SourceHealthBar
{
#pragma warning disable CS0109 // Disable warning about redundant 'new' keyword
    /// <summary>
    /// Cached StringNames for the properties and fields contained in this class, for fast lookup.
    /// </summary>
    public new class PropertyName : global::Godot.Control.PropertyName {
        /// <summary>
        /// Cached name for the 'Hp' field.
        /// </summary>
        public new static readonly global::Godot.StringName @Hp = "Hp";
        /// <summary>
        /// Cached name for the 'Maximum' field.
        /// </summary>
        public new static readonly global::Godot.StringName @Maximum = "Maximum";
        /// <summary>
        /// Cached name for the 'Block' field.
        /// </summary>
        public new static readonly global::Godot.StringName @Block = "Block";
        /// <summary>
        /// Cached name for the 'IceBlock' field.
        /// </summary>
        public new static readonly global::Godot.StringName @IceBlock = "IceBlock";
        /// <summary>
        /// Cached name for the 'PredictedLoss' field.
        /// </summary>
        public new static readonly global::Godot.StringName @PredictedLoss = "PredictedLoss";
    }
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
    {
        if (name == PropertyName.@Hp) {
            this.@Hp = global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(value);
            return true;
        }
        if (name == PropertyName.@Maximum) {
            this.@Maximum = global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(value);
            return true;
        }
        if (name == PropertyName.@Block) {
            this.@Block = global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(value);
            return true;
        }
        if (name == PropertyName.@IceBlock) {
            this.@IceBlock = global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(value);
            return true;
        }
        if (name == PropertyName.@PredictedLoss) {
            this.@PredictedLoss = global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(value);
            return true;
        }
        return base.SetGodotClassPropertyValue(name, value);
    }
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
    {
        if (name == PropertyName.@Hp) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<int>(this.@Hp);
            return true;
        }
        if (name == PropertyName.@Maximum) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<int>(this.@Maximum);
            return true;
        }
        if (name == PropertyName.@Block) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<int>(this.@Block);
            return true;
        }
        if (name == PropertyName.@IceBlock) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<int>(this.@IceBlock);
            return true;
        }
        if (name == PropertyName.@PredictedLoss) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<int>(this.@PredictedLoss);
            return true;
        }
        return base.GetGodotClassPropertyValue(name, out value);
    }
    /// <summary>
    /// Get the property information for all the properties declared in this class.
    /// This method is used by Godot to register the available properties in the editor.
    /// Do not call this method.
    /// </summary>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal new static global::System.Collections.Generic.List<global::Godot.Bridge.PropertyInfo> GetGodotPropertyList()
    {
        var properties = new global::System.Collections.Generic.List<global::Godot.Bridge.PropertyInfo>();
        properties.Add(new(type: (global::Godot.Variant.Type)2, name: PropertyName.@Hp, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)2, name: PropertyName.@Maximum, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)2, name: PropertyName.@Block, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)2, name: PropertyName.@IceBlock, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)2, name: PropertyName.@PredictedLoss, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        return properties;
    }
#pragma warning restore CS0109
}

}

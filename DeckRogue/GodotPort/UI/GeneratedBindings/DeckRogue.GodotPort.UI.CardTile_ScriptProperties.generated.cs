using Godot;
using Godot.NativeInterop;

namespace DeckRogue.GodotPort.UI {

partial class CardTile
{
#pragma warning disable CS0109 // Disable warning about redundant 'new' keyword
    /// <summary>
    /// Cached StringNames for the properties and fields contained in this class, for fast lookup.
    /// </summary>
    public new class PropertyName : global::Godot.Button.PropertyName {
        /// <summary>
        /// Cached name for the 'CardUid' property.
        /// </summary>
        public new static readonly global::Godot.StringName @CardUid = "CardUid";
        /// <summary>
        /// Cached name for the 'AllowDrag' property.
        /// </summary>
        public new static readonly global::Godot.StringName @AllowDrag = "AllowDrag";
        /// <summary>
        /// Cached name for the 'IsBattleHand' property.
        /// </summary>
        public new static readonly global::Godot.StringName @IsBattleHand = "IsBattleHand";
        /// <summary>
        /// Cached name for the '_holding' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_holding = "_holding";
        /// <summary>
        /// Cached name for the '_inspected' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_inspected = "_inspected";
        /// <summary>
        /// Cached name for the '_held' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_held = "_held";
        /// <summary>
        /// Cached name for the '_pressAt' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_pressAt = "_pressAt";
    }
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
    {
        if (name == PropertyName.@CardUid) {
            this.@CardUid = global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(value);
            return true;
        }
        if (name == PropertyName.@AllowDrag) {
            this.@AllowDrag = global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(value);
            return true;
        }
        if (name == PropertyName.@IsBattleHand) {
            this.@IsBattleHand = global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(value);
            return true;
        }
        if (name == PropertyName.@_holding) {
            this.@_holding = global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(value);
            return true;
        }
        if (name == PropertyName.@_inspected) {
            this.@_inspected = global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(value);
            return true;
        }
        if (name == PropertyName.@_held) {
            this.@_held = global::Godot.NativeInterop.VariantUtils.ConvertTo<float>(value);
            return true;
        }
        if (name == PropertyName.@_pressAt) {
            this.@_pressAt = global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Vector2>(value);
            return true;
        }
        return base.SetGodotClassPropertyValue(name, value);
    }
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
    {
        if (name == PropertyName.@CardUid) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<string>(this.@CardUid);
            return true;
        }
        if (name == PropertyName.@AllowDrag) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<bool>(this.@AllowDrag);
            return true;
        }
        if (name == PropertyName.@IsBattleHand) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<bool>(this.@IsBattleHand);
            return true;
        }
        if (name == PropertyName.@_holding) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<bool>(this.@_holding);
            return true;
        }
        if (name == PropertyName.@_inspected) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<bool>(this.@_inspected);
            return true;
        }
        if (name == PropertyName.@_held) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<float>(this.@_held);
            return true;
        }
        if (name == PropertyName.@_pressAt) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.Vector2>(this.@_pressAt);
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
        properties.Add(new(type: (global::Godot.Variant.Type)1, name: PropertyName.@_holding, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)1, name: PropertyName.@_inspected, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)3, name: PropertyName.@_held, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)5, name: PropertyName.@_pressAt, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)4, name: PropertyName.@CardUid, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)1, name: PropertyName.@AllowDrag, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)1, name: PropertyName.@IsBattleHand, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        return properties;
    }
#pragma warning restore CS0109
}

}

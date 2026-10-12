using Godot;
using Godot.NativeInterop;

namespace DeckRogue.GodotPort.UI {

partial class SourceCardBody
{
#pragma warning disable CS0109 // Disable warning about redundant 'new' keyword
    /// <summary>
    /// Cached StringNames for the properties and fields contained in this class, for fast lookup.
    /// </summary>
    public new class PropertyName : global::Godot.Control.PropertyName {
        /// <summary>
        /// Cached name for the '_text' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_text = "_text";
        /// <summary>
        /// Cached name for the '_markup' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_markup = "_markup";
        /// <summary>
        /// Cached name for the '_scale' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_scale = "_scale";
    }
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
    {
        if (name == PropertyName.@_text) {
            this.@_text = global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.RichTextLabel>(value);
            return true;
        }
        if (name == PropertyName.@_markup) {
            this.@_markup = global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(value);
            return true;
        }
        if (name == PropertyName.@_scale) {
            this.@_scale = global::Godot.NativeInterop.VariantUtils.ConvertTo<float>(value);
            return true;
        }
        return base.SetGodotClassPropertyValue(name, value);
    }
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
    {
        if (name == PropertyName.@_text) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.RichTextLabel>(this.@_text);
            return true;
        }
        if (name == PropertyName.@_markup) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<string>(this.@_markup);
            return true;
        }
        if (name == PropertyName.@_scale) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<float>(this.@_scale);
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
        properties.Add(new(type: (global::Godot.Variant.Type)24, name: PropertyName.@_text, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)4, name: PropertyName.@_markup, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)3, name: PropertyName.@_scale, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        return properties;
    }
#pragma warning restore CS0109
}

}

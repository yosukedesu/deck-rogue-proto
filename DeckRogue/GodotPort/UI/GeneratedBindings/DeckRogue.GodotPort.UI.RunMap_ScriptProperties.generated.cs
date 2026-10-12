using Godot;
using Godot.NativeInterop;

namespace DeckRogue.GodotPort.UI {

partial class RunMap
{
#pragma warning disable CS0109 // Disable warning about redundant 'new' keyword
    /// <summary>
    /// Cached StringNames for the properties and fields contained in this class, for fast lookup.
    /// </summary>
    public new class PropertyName : global::Godot.Control.PropertyName {
        /// <summary>
        /// Cached name for the '_drawing' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_drawing = "_drawing";
        /// <summary>
        /// Cached name for the '_pen' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_pen = "_pen";
    }
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
    {
        if (name == PropertyName.@_drawing) {
            this.@_drawing = global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(value);
            return true;
        }
        if (name == PropertyName.@_pen) {
            this.@_pen = global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(value);
            return true;
        }
        return base.SetGodotClassPropertyValue(name, value);
    }
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
    {
        if (name == PropertyName.@_drawing) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<bool>(this.@_drawing);
            return true;
        }
        if (name == PropertyName.@_pen) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<string>(this.@_pen);
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
        properties.Add(new(type: (global::Godot.Variant.Type)1, name: PropertyName.@_drawing, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)4, name: PropertyName.@_pen, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        return properties;
    }
#pragma warning restore CS0109
}

}

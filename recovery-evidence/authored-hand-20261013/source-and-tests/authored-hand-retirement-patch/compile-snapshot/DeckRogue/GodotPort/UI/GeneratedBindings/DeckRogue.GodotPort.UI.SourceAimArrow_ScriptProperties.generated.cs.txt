using Godot;
using Godot.NativeInterop;

namespace DeckRogue.GodotPort.UI {

partial class SourceAimArrow
{
#pragma warning disable CS0109 // Disable warning about redundant 'new' keyword
    /// <summary>
    /// Cached StringNames for the properties and fields contained in this class, for fast lookup.
    /// </summary>
    public new class PropertyName : global::Godot.Control.PropertyName {
        /// <summary>
        /// Cached name for the 'VisibleDots' property.
        /// </summary>
        public new static readonly global::Godot.StringName @VisibleDots = "VisibleDots";
        /// <summary>
        /// Cached name for the 'Active' field.
        /// </summary>
        public new static readonly global::Godot.StringName @Active = "Active";
        /// <summary>
        /// Cached name for the 'Start' field.
        /// </summary>
        public new static readonly global::Godot.StringName @Start = "Start";
        /// <summary>
        /// Cached name for the 'End' field.
        /// </summary>
        public new static readonly global::Godot.StringName @End = "End";
        /// <summary>
        /// Cached name for the 'Unit' field.
        /// </summary>
        public new static readonly global::Godot.StringName @Unit = "Unit";
        /// <summary>
        /// Cached name for the 'Note' field.
        /// </summary>
        public new static readonly global::Godot.StringName @Note = "Note";
    }
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
    {
        if (name == PropertyName.@VisibleDots) {
            this.@VisibleDots = global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(value);
            return true;
        }
        if (name == PropertyName.@Active) {
            this.@Active = global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(value);
            return true;
        }
        if (name == PropertyName.@Start) {
            this.@Start = global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Vector2>(value);
            return true;
        }
        if (name == PropertyName.@End) {
            this.@End = global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Vector2>(value);
            return true;
        }
        if (name == PropertyName.@Unit) {
            this.@Unit = global::Godot.NativeInterop.VariantUtils.ConvertTo<float>(value);
            return true;
        }
        if (name == PropertyName.@Note) {
            this.@Note = global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(value);
            return true;
        }
        return base.SetGodotClassPropertyValue(name, value);
    }
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
    {
        if (name == PropertyName.@VisibleDots) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<int>(this.@VisibleDots);
            return true;
        }
        if (name == PropertyName.@Active) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<bool>(this.@Active);
            return true;
        }
        if (name == PropertyName.@Start) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.Vector2>(this.@Start);
            return true;
        }
        if (name == PropertyName.@End) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.Vector2>(this.@End);
            return true;
        }
        if (name == PropertyName.@Unit) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<float>(this.@Unit);
            return true;
        }
        if (name == PropertyName.@Note) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<string>(this.@Note);
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
        properties.Add(new(type: (global::Godot.Variant.Type)1, name: PropertyName.@Active, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)5, name: PropertyName.@Start, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)5, name: PropertyName.@End, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)3, name: PropertyName.@Unit, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)2, name: PropertyName.@VisibleDots, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)4, name: PropertyName.@Note, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        return properties;
    }
#pragma warning restore CS0109
}

}

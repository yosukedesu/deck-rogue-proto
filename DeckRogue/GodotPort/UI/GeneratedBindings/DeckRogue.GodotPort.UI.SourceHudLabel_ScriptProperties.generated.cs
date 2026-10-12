using Godot;
using Godot.NativeInterop;

namespace DeckRogue.GodotPort.UI {

partial class SourceHudLabel
{
#pragma warning disable CS0109 // Disable warning about redundant 'new' keyword
    /// <summary>
    /// Cached StringNames for the properties and fields contained in this class, for fast lookup.
    /// </summary>
    public new class PropertyName : global::Godot.Label.PropertyName {
        /// <summary>
        /// Cached name for the 'SourceTextRect' property.
        /// </summary>
        public new static readonly global::Godot.StringName @SourceTextRect = "SourceTextRect";
        /// <summary>
        /// Cached name for the 'FollowParentRect' property.
        /// </summary>
        public new static readonly global::Godot.StringName @FollowParentRect = "FollowParentRect";
        /// <summary>
        /// Cached name for the 'PaintedGlyphBoundsInParent' property.
        /// </summary>
        public new static readonly global::Godot.StringName @PaintedGlyphBoundsInParent = "PaintedGlyphBoundsInParent";
        /// <summary>
        /// Cached name for the '_sourceTextRect' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_sourceTextRect = "_sourceTextRect";
        /// <summary>
        /// Cached name for the '_measuredText' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_measuredText = "_measuredText";
        /// <summary>
        /// Cached name for the '_measuredFont' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_measuredFont = "_measuredFont";
        /// <summary>
        /// Cached name for the '_measuredSize' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_measuredSize = "_measuredSize";
        /// <summary>
        /// Cached name for the '_glyphs' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_glyphs = "_glyphs";
    }
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
    {
        if (name == PropertyName.@SourceTextRect) {
            this.@SourceTextRect = global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Rect2>(value);
            return true;
        }
        if (name == PropertyName.@FollowParentRect) {
            this.@FollowParentRect = global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(value);
            return true;
        }
        if (name == PropertyName.@PaintedGlyphBoundsInParent) {
            this.@PaintedGlyphBoundsInParent = global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Rect2>(value);
            return true;
        }
        if (name == PropertyName.@_sourceTextRect) {
            this.@_sourceTextRect = global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Rect2>(value);
            return true;
        }
        if (name == PropertyName.@_measuredText) {
            this.@_measuredText = global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(value);
            return true;
        }
        if (name == PropertyName.@_measuredFont) {
            this.@_measuredFont = global::Godot.NativeInterop.VariantUtils.ConvertTo<ulong>(value);
            return true;
        }
        if (name == PropertyName.@_measuredSize) {
            this.@_measuredSize = global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(value);
            return true;
        }
        if (name == PropertyName.@_glyphs) {
            this.@_glyphs = global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Rect2>(value);
            return true;
        }
        return base.SetGodotClassPropertyValue(name, value);
    }
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
    {
        if (name == PropertyName.@SourceTextRect) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.Rect2>(this.@SourceTextRect);
            return true;
        }
        if (name == PropertyName.@FollowParentRect) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<bool>(this.@FollowParentRect);
            return true;
        }
        if (name == PropertyName.@PaintedGlyphBoundsInParent) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.Rect2>(this.@PaintedGlyphBoundsInParent);
            return true;
        }
        if (name == PropertyName.@_sourceTextRect) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.Rect2>(this.@_sourceTextRect);
            return true;
        }
        if (name == PropertyName.@_measuredText) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<string>(this.@_measuredText);
            return true;
        }
        if (name == PropertyName.@_measuredFont) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<ulong>(this.@_measuredFont);
            return true;
        }
        if (name == PropertyName.@_measuredSize) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<int>(this.@_measuredSize);
            return true;
        }
        if (name == PropertyName.@_glyphs) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.Rect2>(this.@_glyphs);
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
        properties.Add(new(type: (global::Godot.Variant.Type)7, name: PropertyName.@_sourceTextRect, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)7, name: PropertyName.@SourceTextRect, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)1, name: PropertyName.@FollowParentRect, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)7, name: PropertyName.@PaintedGlyphBoundsInParent, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)4, name: PropertyName.@_measuredText, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)2, name: PropertyName.@_measuredFont, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)2, name: PropertyName.@_measuredSize, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)7, name: PropertyName.@_glyphs, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        return properties;
    }
#pragma warning restore CS0109
}

}

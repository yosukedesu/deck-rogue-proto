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
        /// Cached name for the 'IsDrawing' property.
        /// </summary>
        public new static readonly global::Godot.StringName @IsDrawing = "IsDrawing";
        /// <summary>
        /// Cached name for the 'IsErasing' property.
        /// </summary>
        public new static readonly global::Godot.StringName @IsErasing = "IsErasing";
        /// <summary>
        /// Cached name for the 'PenColor' property.
        /// </summary>
        public new static readonly global::Godot.StringName @PenColor = "PenColor";
        /// <summary>
        /// Cached name for the 'AnnotationCount' property.
        /// </summary>
        public new static readonly global::Godot.StringName @AnnotationCount = "AnnotationCount";
        /// <summary>
        /// Cached name for the 'StartPoint' property.
        /// </summary>
        public new static readonly global::Godot.StringName @StartPoint = "StartPoint";
        /// <summary>
        /// Cached name for the '_annotationLayer' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_annotationLayer = "_annotationLayer";
        /// <summary>
        /// Cached name for the '_editable' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_editable = "_editable";
        /// <summary>
        /// Cached name for the '_drawing' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_drawing = "_drawing";
        /// <summary>
        /// Cached name for the '_erasing' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_erasing = "_erasing";
        /// <summary>
        /// Cached name for the '_strokeChanged' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_strokeChanged = "_strokeChanged";
        /// <summary>
        /// Cached name for the '_pen' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_pen = "_pen";
        /// <summary>
        /// Cached name for the '_pointerButton' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_pointerButton = "_pointerButton";
        /// <summary>
        /// Cached name for the '_inspectedNode' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_inspectedNode = "_inspectedNode";
        /// <summary>
        /// Cached name for the '_phone' field.
        /// </summary>
        public new static readonly global::Godot.StringName @_phone = "_phone";
    }
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
    {
        if (name == PropertyName.@_annotationLayer) {
            this.@_annotationLayer = global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Control>(value);
            return true;
        }
        if (name == PropertyName.@_editable) {
            this.@_editable = global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(value);
            return true;
        }
        if (name == PropertyName.@_drawing) {
            this.@_drawing = global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(value);
            return true;
        }
        if (name == PropertyName.@_erasing) {
            this.@_erasing = global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(value);
            return true;
        }
        if (name == PropertyName.@_strokeChanged) {
            this.@_strokeChanged = global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(value);
            return true;
        }
        if (name == PropertyName.@_pen) {
            this.@_pen = global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(value);
            return true;
        }
        if (name == PropertyName.@_pointerButton) {
            this.@_pointerButton = global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.MouseButton>(value);
            return true;
        }
        if (name == PropertyName.@_inspectedNode) {
            this.@_inspectedNode = global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(value);
            return true;
        }
        if (name == PropertyName.@_phone) {
            this.@_phone = global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(value);
            return true;
        }
        return base.SetGodotClassPropertyValue(name, value);
    }
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
    {
        if (name == PropertyName.@IsDrawing) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<bool>(this.@IsDrawing);
            return true;
        }
        if (name == PropertyName.@IsErasing) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<bool>(this.@IsErasing);
            return true;
        }
        if (name == PropertyName.@PenColor) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<string>(this.@PenColor);
            return true;
        }
        if (name == PropertyName.@AnnotationCount) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<int>(this.@AnnotationCount);
            return true;
        }
        if (name == PropertyName.@StartPoint) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.Vector2>(this.@StartPoint);
            return true;
        }
        if (name == PropertyName.@_annotationLayer) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.Control>(this.@_annotationLayer);
            return true;
        }
        if (name == PropertyName.@_editable) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<bool>(this.@_editable);
            return true;
        }
        if (name == PropertyName.@_drawing) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<bool>(this.@_drawing);
            return true;
        }
        if (name == PropertyName.@_erasing) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<bool>(this.@_erasing);
            return true;
        }
        if (name == PropertyName.@_strokeChanged) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<bool>(this.@_strokeChanged);
            return true;
        }
        if (name == PropertyName.@_pen) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<string>(this.@_pen);
            return true;
        }
        if (name == PropertyName.@_pointerButton) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.MouseButton>(this.@_pointerButton);
            return true;
        }
        if (name == PropertyName.@_inspectedNode) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<string>(this.@_inspectedNode);
            return true;
        }
        if (name == PropertyName.@_phone) {
            value = global::Godot.NativeInterop.VariantUtils.CreateFrom<bool>(this.@_phone);
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
        properties.Add(new(type: (global::Godot.Variant.Type)24, name: PropertyName.@_annotationLayer, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)1, name: PropertyName.@_editable, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)1, name: PropertyName.@_drawing, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)1, name: PropertyName.@_erasing, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)1, name: PropertyName.@_strokeChanged, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)4, name: PropertyName.@_pen, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)2, name: PropertyName.@_pointerButton, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)4, name: PropertyName.@_inspectedNode, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)1, name: PropertyName.@_phone, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)1, name: PropertyName.@IsDrawing, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)1, name: PropertyName.@IsErasing, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)4, name: PropertyName.@PenColor, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)2, name: PropertyName.@AnnotationCount, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        properties.Add(new(type: (global::Godot.Variant.Type)5, name: PropertyName.@StartPoint, hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)4096, exported: false));
        return properties;
    }
#pragma warning restore CS0109
}

}

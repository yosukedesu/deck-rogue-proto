using Godot;
using Godot.NativeInterop;

namespace DeckRogue.GodotPort.UI {

partial class RunMap
{
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override void SaveGodotObjectData(global::Godot.Bridge.GodotSerializationInfo info)
    {
        base.SaveGodotObjectData(info);
        info.AddProperty(PropertyName.@_annotationLayer, global::Godot.Variant.From<global::Godot.Control>(this.@_annotationLayer));
        info.AddProperty(PropertyName.@_editable, global::Godot.Variant.From<bool>(this.@_editable));
        info.AddProperty(PropertyName.@_drawing, global::Godot.Variant.From<bool>(this.@_drawing));
        info.AddProperty(PropertyName.@_erasing, global::Godot.Variant.From<bool>(this.@_erasing));
        info.AddProperty(PropertyName.@_strokeChanged, global::Godot.Variant.From<bool>(this.@_strokeChanged));
        info.AddProperty(PropertyName.@_pen, global::Godot.Variant.From<string>(this.@_pen));
        info.AddProperty(PropertyName.@_pointerButton, global::Godot.Variant.From<global::Godot.MouseButton>(this.@_pointerButton));
        info.AddProperty(PropertyName.@_inspectedNode, global::Godot.Variant.From<string>(this.@_inspectedNode));
        info.AddProperty(PropertyName.@_phone, global::Godot.Variant.From<bool>(this.@_phone));
    }
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override void RestoreGodotObjectData(global::Godot.Bridge.GodotSerializationInfo info)
    {
        base.RestoreGodotObjectData(info);
        if (info.TryGetProperty(PropertyName.@_annotationLayer, out var _value__annotationLayer))
            this.@_annotationLayer = _value__annotationLayer.As<global::Godot.Control>();
        if (info.TryGetProperty(PropertyName.@_editable, out var _value__editable))
            this.@_editable = _value__editable.As<bool>();
        if (info.TryGetProperty(PropertyName.@_drawing, out var _value__drawing))
            this.@_drawing = _value__drawing.As<bool>();
        if (info.TryGetProperty(PropertyName.@_erasing, out var _value__erasing))
            this.@_erasing = _value__erasing.As<bool>();
        if (info.TryGetProperty(PropertyName.@_strokeChanged, out var _value__strokeChanged))
            this.@_strokeChanged = _value__strokeChanged.As<bool>();
        if (info.TryGetProperty(PropertyName.@_pen, out var _value__pen))
            this.@_pen = _value__pen.As<string>();
        if (info.TryGetProperty(PropertyName.@_pointerButton, out var _value__pointerButton))
            this.@_pointerButton = _value__pointerButton.As<global::Godot.MouseButton>();
        if (info.TryGetProperty(PropertyName.@_inspectedNode, out var _value__inspectedNode))
            this.@_inspectedNode = _value__inspectedNode.As<string>();
        if (info.TryGetProperty(PropertyName.@_phone, out var _value__phone))
            this.@_phone = _value__phone.As<bool>();
    }
}

}

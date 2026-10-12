using Godot;
using Godot.NativeInterop;

namespace DeckRogue.GodotPort.UI {

partial class RunMap
{
#pragma warning disable CS0109 // Disable warning about redundant 'new' keyword
    /// <summary>
    /// Cached StringNames for the methods contained in this class, for fast lookup.
    /// </summary>
    public new class MethodName : global::Godot.Control.MethodName {
        /// <summary>
        /// Cached name for the 'SetDrawing' method.
        /// </summary>
        public new static readonly global::Godot.StringName @SetDrawing = "SetDrawing";
        /// <summary>
        /// Cached name for the 'UndoAnnotation' method.
        /// </summary>
        public new static readonly global::Godot.StringName @UndoAnnotation = "UndoAnnotation";
        /// <summary>
        /// Cached name for the 'SaveAnnotations' method.
        /// </summary>
        public new static readonly global::Godot.StringName @SaveAnnotations = "SaveAnnotations";
        /// <summary>
        /// Cached name for the '_GuiInput' method.
        /// </summary>
        public new static readonly global::Godot.StringName @_GuiInput = "_GuiInput";
        /// <summary>
        /// Cached name for the 'DrawAnnotations' method.
        /// </summary>
        public new static readonly global::Godot.StringName @DrawAnnotations = "DrawAnnotations";
        /// <summary>
        /// Cached name for the 'Point' method.
        /// </summary>
        public new static readonly global::Godot.StringName @Point = "Point";
        /// <summary>
        /// Cached name for the '_Draw' method.
        /// </summary>
        public new static readonly global::Godot.StringName @_Draw = "_Draw";
    }
    /// <summary>
    /// Get the method information for all the methods declared in this class.
    /// This method is used by Godot to register the available methods in the editor.
    /// Do not call this method.
    /// </summary>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal new static global::System.Collections.Generic.List<global::Godot.Bridge.MethodInfo> GetGodotMethodList()
    {
        var methods = new global::System.Collections.Generic.List<global::Godot.Bridge.MethodInfo>(7);
        methods.Add(new(name: MethodName.@SetDrawing, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)1, name: "drawing", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)4, name: "color", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@UndoAnnotation, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@SaveAnnotations, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@_GuiInput, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)24, name: "evt", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("InputEvent")),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@DrawAnnotations, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@Point, returnVal: new(type: (global::Godot.Variant.Type)5, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)2, name: "row", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)2, name: "col", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@_Draw, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        return methods;
    }
#pragma warning restore CS0109
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
    {
        if (method == MethodName.@SetDrawing && args.Count == 2) {
            @SetDrawing(global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[1]));
            ret = default;
            return true;
        }
        if (method == MethodName.@UndoAnnotation && args.Count == 0) {
            @UndoAnnotation();
            ret = default;
            return true;
        }
        if (method == MethodName.@SaveAnnotations && args.Count == 0) {
            @SaveAnnotations();
            ret = default;
            return true;
        }
        if (method == MethodName.@_GuiInput && args.Count == 1) {
            @_GuiInput(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.InputEvent>(args[0]));
            ret = default;
            return true;
        }
        if (method == MethodName.@DrawAnnotations && args.Count == 0) {
            @DrawAnnotations();
            ret = default;
            return true;
        }
        if (method == MethodName.@Point && args.Count == 2) {
            var callRet = @Point(global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(args[1]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.Vector2>(callRet);
            return true;
        }
        if (method == MethodName.@_Draw && args.Count == 0) {
            @_Draw();
            ret = default;
            return true;
        }
        return base.InvokeGodotClassMethod(method, args, out ret);
    }
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override bool HasGodotClassMethod(in godot_string_name method)
    {
        if (method == MethodName.@SetDrawing) {
           return true;
        }
        if (method == MethodName.@UndoAnnotation) {
           return true;
        }
        if (method == MethodName.@SaveAnnotations) {
           return true;
        }
        if (method == MethodName.@_GuiInput) {
           return true;
        }
        if (method == MethodName.@DrawAnnotations) {
           return true;
        }
        if (method == MethodName.@Point) {
           return true;
        }
        if (method == MethodName.@_Draw) {
           return true;
        }
        return base.HasGodotClassMethod(method);
    }
}

}

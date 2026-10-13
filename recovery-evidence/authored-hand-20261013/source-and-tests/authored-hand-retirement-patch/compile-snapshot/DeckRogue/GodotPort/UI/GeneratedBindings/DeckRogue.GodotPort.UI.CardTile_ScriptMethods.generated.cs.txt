using Godot;
using Godot.NativeInterop;

namespace DeckRogue.GodotPort.UI {

partial class CardTile
{
#pragma warning disable CS0109 // Disable warning about redundant 'new' keyword
    /// <summary>
    /// Cached StringNames for the methods contained in this class, for fast lookup.
    /// </summary>
    public new class MethodName : global::Godot.Button.MethodName {
        /// <summary>
        /// Cached name for the 'ConsumeInspection' method.
        /// </summary>
        public new static readonly global::Godot.StringName @ConsumeInspection = "ConsumeInspection";
        /// <summary>
        /// Cached name for the '_GuiInput' method.
        /// </summary>
        public new static readonly global::Godot.StringName @_GuiInput = "_GuiInput";
        /// <summary>
        /// Cached name for the '_Process' method.
        /// </summary>
        public new static readonly global::Godot.StringName @_Process = "_Process";
        /// <summary>
        /// Cached name for the '_GetDragData' method.
        /// </summary>
        public new static readonly global::Godot.StringName @_GetDragData = "_GetDragData";
        /// <summary>
        /// Cached name for the '_Notification' method.
        /// </summary>
        public new static readonly global::Godot.StringName @_Notification = "_Notification";
    }
    /// <summary>
    /// Get the method information for all the methods declared in this class.
    /// This method is used by Godot to register the available methods in the editor.
    /// Do not call this method.
    /// </summary>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal new static global::System.Collections.Generic.List<global::Godot.Bridge.MethodInfo> GetGodotMethodList()
    {
        var methods = new global::System.Collections.Generic.List<global::Godot.Bridge.MethodInfo>(5);
        methods.Add(new(name: MethodName.@ConsumeInspection, returnVal: new(type: (global::Godot.Variant.Type)1, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@_GuiInput, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)24, name: "evt", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("InputEvent")),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@_Process, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)3, name: "delta", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@_GetDragData, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)131078, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)5, name: "position", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@_Notification, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)2, name: "what", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        return methods;
    }
#pragma warning restore CS0109
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
    {
        if (method == MethodName.@ConsumeInspection && args.Count == 0) {
            var callRet = @ConsumeInspection();
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<bool>(callRet);
            return true;
        }
        if (method == MethodName.@_GuiInput && args.Count == 1) {
            @_GuiInput(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.InputEvent>(args[0]));
            ret = default;
            return true;
        }
        if (method == MethodName.@_Process && args.Count == 1) {
            @_Process(global::Godot.NativeInterop.VariantUtils.ConvertTo<double>(args[0]));
            ret = default;
            return true;
        }
        if (method == MethodName.@_GetDragData && args.Count == 1) {
            var callRet = @_GetDragData(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Vector2>(args[0]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.Variant>(callRet);
            return true;
        }
        if (method == MethodName.@_Notification && args.Count == 1) {
            @_Notification(global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(args[0]));
            ret = default;
            return true;
        }
        return base.InvokeGodotClassMethod(method, args, out ret);
    }
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override bool HasGodotClassMethod(in godot_string_name method)
    {
        if (method == MethodName.@ConsumeInspection) {
           return true;
        }
        if (method == MethodName.@_GuiInput) {
           return true;
        }
        if (method == MethodName.@_Process) {
           return true;
        }
        if (method == MethodName.@_GetDragData) {
           return true;
        }
        if (method == MethodName.@_Notification) {
           return true;
        }
        return base.HasGodotClassMethod(method);
    }
}

}

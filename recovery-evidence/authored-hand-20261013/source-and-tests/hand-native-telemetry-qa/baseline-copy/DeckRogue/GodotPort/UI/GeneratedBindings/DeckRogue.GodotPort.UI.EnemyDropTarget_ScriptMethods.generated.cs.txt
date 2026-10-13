using Godot;
using Godot.NativeInterop;

namespace DeckRogue.GodotPort.UI {

partial class EnemyDropTarget
{
#pragma warning disable CS0109 // Disable warning about redundant 'new' keyword
    /// <summary>
    /// Cached StringNames for the methods contained in this class, for fast lookup.
    /// </summary>
    public new class MethodName : global::Godot.Button.MethodName {
        /// <summary>
        /// Cached name for the 'Resolve' method.
        /// </summary>
        public new static readonly global::Godot.StringName @Resolve = "Resolve";
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
        /// Cached name for the '_CanDropData' method.
        /// </summary>
        public new static readonly global::Godot.StringName @_CanDropData = "_CanDropData";
        /// <summary>
        /// Cached name for the '_DropData' method.
        /// </summary>
        public new static readonly global::Godot.StringName @_DropData = "_DropData";
    }
    /// <summary>
    /// Get the method information for all the methods declared in this class.
    /// This method is used by Godot to register the available methods in the editor.
    /// Do not call this method.
    /// </summary>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal new static global::System.Collections.Generic.List<global::Godot.Bridge.MethodInfo> GetGodotMethodList()
    {
        var methods = new global::System.Collections.Generic.List<global::Godot.Bridge.MethodInfo>(6);
        methods.Add(new(name: MethodName.@Resolve, returnVal: new(type: (global::Godot.Variant.Type)4, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)0, name: "data", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)131078, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@ConsumeInspection, returnVal: new(type: (global::Godot.Variant.Type)1, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@_GuiInput, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)24, name: "evt", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("InputEvent")),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@_Process, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)3, name: "delta", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@_CanDropData, returnVal: new(type: (global::Godot.Variant.Type)1, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)5, name: "atPosition", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)0, name: "data", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)131078, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@_DropData, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)5, name: "atPosition", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)0, name: "data", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)131078, exported: false),  }, defaultArguments: null));
        return methods;
    }
#pragma warning restore CS0109
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
    {
        if (method == MethodName.@Resolve && args.Count == 1) {
            var callRet = @Resolve(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Variant>(args[0]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<string>(callRet);
            return true;
        }
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
        if (method == MethodName.@_CanDropData && args.Count == 2) {
            var callRet = @_CanDropData(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Vector2>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Variant>(args[1]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<bool>(callRet);
            return true;
        }
        if (method == MethodName.@_DropData && args.Count == 2) {
            @_DropData(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Vector2>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Variant>(args[1]));
            ret = default;
            return true;
        }
        return base.InvokeGodotClassMethod(method, args, out ret);
    }
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override bool HasGodotClassMethod(in godot_string_name method)
    {
        if (method == MethodName.@Resolve) {
           return true;
        }
        if (method == MethodName.@ConsumeInspection) {
           return true;
        }
        if (method == MethodName.@_GuiInput) {
           return true;
        }
        if (method == MethodName.@_Process) {
           return true;
        }
        if (method == MethodName.@_CanDropData) {
           return true;
        }
        if (method == MethodName.@_DropData) {
           return true;
        }
        return base.HasGodotClassMethod(method);
    }
}

}

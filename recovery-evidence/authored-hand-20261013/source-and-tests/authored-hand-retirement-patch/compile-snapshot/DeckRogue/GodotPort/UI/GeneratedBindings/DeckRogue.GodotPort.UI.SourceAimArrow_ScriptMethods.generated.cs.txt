using Godot;
using Godot.NativeInterop;

namespace DeckRogue.GodotPort.UI {

partial class SourceAimArrow
{
#pragma warning disable CS0109 // Disable warning about redundant 'new' keyword
    /// <summary>
    /// Cached StringNames for the methods contained in this class, for fast lookup.
    /// </summary>
    public new class MethodName : global::Godot.Control.MethodName {
        /// <summary>
        /// Cached name for the 'Curve' method.
        /// </summary>
        public new static readonly global::Godot.StringName @Curve = "Curve";
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
        var methods = new global::System.Collections.Generic.List<global::Godot.Bridge.MethodInfo>(2);
        methods.Add(new(name: MethodName.@Curve, returnVal: new(type: (global::Godot.Variant.Type)5, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)33, arguments: new() { new(type: (global::Godot.Variant.Type)5, name: "a", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)5, name: "b", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)5, name: "c", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)3, name: "t", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@_Draw, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        return methods;
    }
#pragma warning restore CS0109
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
    {
        if (method == MethodName.@Curve && args.Count == 4) {
            var callRet = @Curve(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Vector2>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Vector2>(args[1]), global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Vector2>(args[2]), global::Godot.NativeInterop.VariantUtils.ConvertTo<float>(args[3]));
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
#pragma warning disable CS0109 // Disable warning about redundant 'new' keyword
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal new static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
    {
        if (method == MethodName.@Curve && args.Count == 4) {
            var callRet = @Curve(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Vector2>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Vector2>(args[1]), global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Vector2>(args[2]), global::Godot.NativeInterop.VariantUtils.ConvertTo<float>(args[3]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.Vector2>(callRet);
            return true;
        }
        ret = default;
        return false;
    }
#pragma warning restore CS0109
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override bool HasGodotClassMethod(in godot_string_name method)
    {
        if (method == MethodName.@Curve) {
           return true;
        }
        if (method == MethodName.@_Draw) {
           return true;
        }
        return base.HasGodotClassMethod(method);
    }
}

}

using Godot;
using Godot.NativeInterop;

namespace DeckRogue.GodotPort.UI {

partial class SourceHudLabel
{
#pragma warning disable CS0109 // Disable warning about redundant 'new' keyword
    /// <summary>
    /// Cached StringNames for the methods contained in this class, for fast lookup.
    /// </summary>
    public new class MethodName : global::Godot.Label.MethodName {
        /// <summary>
        /// Cached name for the '_Ready' method.
        /// </summary>
        public new static readonly global::Godot.StringName @_Ready = "_Ready";
        /// <summary>
        /// Cached name for the '_Process' method.
        /// </summary>
        public new static readonly global::Godot.StringName @_Process = "_Process";
        /// <summary>
        /// Cached name for the 'RefreshSourceAlignment' method.
        /// </summary>
        public new static readonly global::Godot.StringName @RefreshSourceAlignment = "RefreshSourceAlignment";
        /// <summary>
        /// Cached name for the 'MeasureGlyphs' method.
        /// </summary>
        public new static readonly global::Godot.StringName @MeasureGlyphs = "MeasureGlyphs";
        /// <summary>
        /// Cached name for the 'Spaced' method.
        /// </summary>
        public new static readonly global::Godot.StringName @Spaced = "Spaced";
        /// <summary>
        /// Cached name for the 'NightHalo' method.
        /// </summary>
        public new static readonly global::Godot.StringName @NightHalo = "NightHalo";
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
        methods.Add(new(name: MethodName.@_Ready, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@_Process, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)3, name: "delta", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@RefreshSourceAlignment, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@MeasureGlyphs, returnVal: new(type: (global::Godot.Variant.Type)7, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)33, arguments: new() { new(type: (global::Godot.Variant.Type)24, name: "font", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("Font")), new(type: (global::Godot.Variant.Type)2, name: "size", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)4, name: "text", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@Spaced, returnVal: new(type: (global::Godot.Variant.Type)24, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("Font")), flags: (global::Godot.MethodFlags)33, arguments: new() { new(type: (global::Godot.Variant.Type)24, name: "font", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("Font")), new(type: (global::Godot.Variant.Type)2, name: "size", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)3, name: "hundredthsEm", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@NightHalo, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)33, arguments: new() { new(type: (global::Godot.Variant.Type)24, name: "label", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("Label")), new(type: (global::Godot.Variant.Type)24, name: "font", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("Font")), new(type: (global::Godot.Variant.Type)3, name: "unit", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)3, name: "textScale", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)3, name: "tracking", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)1, name: "deferSmallUnderlay", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        return methods;
    }
#pragma warning restore CS0109
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
    {
        if (method == MethodName.@_Ready && args.Count == 0) {
            @_Ready();
            ret = default;
            return true;
        }
        if (method == MethodName.@_Process && args.Count == 1) {
            @_Process(global::Godot.NativeInterop.VariantUtils.ConvertTo<double>(args[0]));
            ret = default;
            return true;
        }
        if (method == MethodName.@RefreshSourceAlignment && args.Count == 0) {
            @RefreshSourceAlignment();
            ret = default;
            return true;
        }
        if (method == MethodName.@MeasureGlyphs && args.Count == 3) {
            var callRet = @MeasureGlyphs(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Font>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(args[1]), global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[2]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.Rect2>(callRet);
            return true;
        }
        if (method == MethodName.@Spaced && args.Count == 3) {
            var callRet = @Spaced(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Font>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(args[1]), global::Godot.NativeInterop.VariantUtils.ConvertTo<float>(args[2]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.Font>(callRet);
            return true;
        }
        if (method == MethodName.@NightHalo && args.Count == 6) {
            @NightHalo(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Label>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Font>(args[1]), global::Godot.NativeInterop.VariantUtils.ConvertTo<float>(args[2]), global::Godot.NativeInterop.VariantUtils.ConvertTo<float>(args[3]), global::Godot.NativeInterop.VariantUtils.ConvertTo<float>(args[4]), global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(args[5]));
            ret = default;
            return true;
        }
        return base.InvokeGodotClassMethod(method, args, out ret);
    }
#pragma warning disable CS0109 // Disable warning about redundant 'new' keyword
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal new static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
    {
        if (method == MethodName.@MeasureGlyphs && args.Count == 3) {
            var callRet = @MeasureGlyphs(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Font>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(args[1]), global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[2]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.Rect2>(callRet);
            return true;
        }
        if (method == MethodName.@Spaced && args.Count == 3) {
            var callRet = @Spaced(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Font>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(args[1]), global::Godot.NativeInterop.VariantUtils.ConvertTo<float>(args[2]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.Font>(callRet);
            return true;
        }
        if (method == MethodName.@NightHalo && args.Count == 6) {
            @NightHalo(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Label>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Font>(args[1]), global::Godot.NativeInterop.VariantUtils.ConvertTo<float>(args[2]), global::Godot.NativeInterop.VariantUtils.ConvertTo<float>(args[3]), global::Godot.NativeInterop.VariantUtils.ConvertTo<float>(args[4]), global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(args[5]));
            ret = default;
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
        if (method == MethodName.@_Ready) {
           return true;
        }
        if (method == MethodName.@_Process) {
           return true;
        }
        if (method == MethodName.@RefreshSourceAlignment) {
           return true;
        }
        if (method == MethodName.@MeasureGlyphs) {
           return true;
        }
        if (method == MethodName.@Spaced) {
           return true;
        }
        if (method == MethodName.@NightHalo) {
           return true;
        }
        return base.HasGodotClassMethod(method);
    }
}

}

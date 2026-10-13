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
        /// Cached name for the 'TogglePen' method.
        /// </summary>
        public new static readonly global::Godot.StringName @TogglePen = "TogglePen";
        /// <summary>
        /// Cached name for the 'ToggleEraser' method.
        /// </summary>
        public new static readonly global::Godot.StringName @ToggleEraser = "ToggleEraser";
        /// <summary>
        /// Cached name for the 'UpdateToolState' method.
        /// </summary>
        public new static readonly global::Godot.StringName @UpdateToolState = "UpdateToolState";
        /// <summary>
        /// Cached name for the 'UndoAnnotation' method.
        /// </summary>
        public new static readonly global::Godot.StringName @UndoAnnotation = "UndoAnnotation";
        /// <summary>
        /// Cached name for the 'ClearAnnotations' method.
        /// </summary>
        public new static readonly global::Godot.StringName @ClearAnnotations = "ClearAnnotations";
        /// <summary>
        /// Cached name for the 'SaveAnnotations' method.
        /// </summary>
        public new static readonly global::Godot.StringName @SaveAnnotations = "SaveAnnotations";
        /// <summary>
        /// Cached name for the '_GuiInput' method.
        /// </summary>
        public new static readonly global::Godot.StringName @_GuiInput = "_GuiInput";
        /// <summary>
        /// Cached name for the 'HandlePointer' method.
        /// </summary>
        public new static readonly global::Godot.StringName @HandlePointer = "HandlePointer";
        /// <summary>
        /// Cached name for the 'FinishPointer' method.
        /// </summary>
        public new static readonly global::Godot.StringName @FinishPointer = "FinishPointer";
        /// <summary>
        /// Cached name for the 'EraseAt' method.
        /// </summary>
        public new static readonly global::Godot.StringName @EraseAt = "EraseAt";
        /// <summary>
        /// Cached name for the 'DistanceToSegment' method.
        /// </summary>
        public new static readonly global::Godot.StringName @DistanceToSegment = "DistanceToSegment";
        /// <summary>
        /// Cached name for the 'RedrawAnnotations' method.
        /// </summary>
        public new static readonly global::Godot.StringName @RedrawAnnotations = "RedrawAnnotations";
        /// <summary>
        /// Cached name for the 'DrawAnnotations' method.
        /// </summary>
        public new static readonly global::Godot.StringName @DrawAnnotations = "DrawAnnotations";
        /// <summary>
        /// Cached name for the 'Point' method.
        /// </summary>
        public new static readonly global::Godot.StringName @Point = "Point";
        /// <summary>
        /// Cached name for the 'Center' method.
        /// </summary>
        public new static readonly global::Godot.StringName @Center = "Center";
        /// <summary>
        /// Cached name for the 'Key' method.
        /// </summary>
        public new static readonly global::Godot.StringName @Key = "Key";
        /// <summary>
        /// Cached name for the 'ClearNodeInformation' method.
        /// </summary>
        public new static readonly global::Godot.StringName @ClearNodeInformation = "ClearNodeInformation";
        /// <summary>
        /// Cached name for the 'InspectNode' method.
        /// </summary>
        public new static readonly global::Godot.StringName @InspectNode = "InspectNode";
        /// <summary>
        /// Cached name for the 'NodeTint' method.
        /// </summary>
        public new static readonly global::Godot.StringName @NodeTint = "NodeTint";
        /// <summary>
        /// Cached name for the 'AddLabel' method.
        /// </summary>
        public new static readonly global::Godot.StringName @AddLabel = "AddLabel";
        /// <summary>
        /// Cached name for the 'AddPicture' method.
        /// </summary>
        public new static readonly global::Godot.StringName @AddPicture = "AddPicture";
        /// <summary>
        /// Cached name for the 'DrawEdge' method.
        /// </summary>
        public new static readonly global::Godot.StringName @DrawEdge = "DrawEdge";
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
        var methods = new global::System.Collections.Generic.List<global::Godot.Bridge.MethodInfo>(24);
        methods.Add(new(name: MethodName.@SetDrawing, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)1, name: "drawing", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)4, name: "color", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@TogglePen, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)4, name: "color", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@ToggleEraser, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@UpdateToolState, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@UndoAnnotation, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@ClearAnnotations, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@SaveAnnotations, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@_GuiInput, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)24, name: "evt", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("InputEvent")),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@HandlePointer, returnVal: new(type: (global::Godot.Variant.Type)1, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)24, name: "evt", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("InputEvent")), new(type: (global::Godot.Variant.Type)5, name: "p", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@FinishPointer, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@EraseAt, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)5, name: "p", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@DistanceToSegment, returnVal: new(type: (global::Godot.Variant.Type)3, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)33, arguments: new() { new(type: (global::Godot.Variant.Type)5, name: "p", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)5, name: "a", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)5, name: "b", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@RedrawAnnotations, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@DrawAnnotations, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@Point, returnVal: new(type: (global::Godot.Variant.Type)5, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)2, name: "row", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)2, name: "col", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@Center, returnVal: new(type: (global::Godot.Variant.Type)5, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)2, name: "row", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)2, name: "col", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@Key, returnVal: new(type: (global::Godot.Variant.Type)2, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)33, arguments: new() { new(type: (global::Godot.Variant.Type)2, name: "row", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)2, name: "col", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@ClearNodeInformation, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@InspectNode, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)4, name: "key", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)4, name: "text", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@NodeTint, returnVal: new(type: (global::Godot.Variant.Type)20, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)33, arguments: new() { new(type: (global::Godot.Variant.Type)4, name: "type", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)1, name: "accessible", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)1, name: "current", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)1, name: "taken", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)1, name: "passed", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@AddLabel, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)33, arguments: new() { new(type: (global::Godot.Variant.Type)24, name: "parent", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("Control")), new(type: (global::Godot.Variant.Type)4, name: "text", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)7, name: "rect", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)2, name: "size", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)20, name: "color", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@AddPicture, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)33, arguments: new() { new(type: (global::Godot.Variant.Type)24, name: "parent", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("Control")), new(type: (global::Godot.Variant.Type)4, name: "path", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)7, name: "rect", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)3, name: "alpha", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@DrawEdge, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)5, name: "a", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)5, name: "b", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)1, name: "available", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)1, name: "taken", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)1, name: "passed", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
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
        if (method == MethodName.@TogglePen && args.Count == 1) {
            @TogglePen(global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[0]));
            ret = default;
            return true;
        }
        if (method == MethodName.@ToggleEraser && args.Count == 0) {
            @ToggleEraser();
            ret = default;
            return true;
        }
        if (method == MethodName.@UpdateToolState && args.Count == 0) {
            @UpdateToolState();
            ret = default;
            return true;
        }
        if (method == MethodName.@UndoAnnotation && args.Count == 0) {
            @UndoAnnotation();
            ret = default;
            return true;
        }
        if (method == MethodName.@ClearAnnotations && args.Count == 0) {
            @ClearAnnotations();
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
        if (method == MethodName.@HandlePointer && args.Count == 2) {
            var callRet = @HandlePointer(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.InputEvent>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Vector2>(args[1]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<bool>(callRet);
            return true;
        }
        if (method == MethodName.@FinishPointer && args.Count == 0) {
            @FinishPointer();
            ret = default;
            return true;
        }
        if (method == MethodName.@EraseAt && args.Count == 1) {
            @EraseAt(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Vector2>(args[0]));
            ret = default;
            return true;
        }
        if (method == MethodName.@DistanceToSegment && args.Count == 3) {
            var callRet = @DistanceToSegment(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Vector2>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Vector2>(args[1]), global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Vector2>(args[2]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<float>(callRet);
            return true;
        }
        if (method == MethodName.@RedrawAnnotations && args.Count == 0) {
            @RedrawAnnotations();
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
        if (method == MethodName.@Center && args.Count == 2) {
            var callRet = @Center(global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(args[1]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.Vector2>(callRet);
            return true;
        }
        if (method == MethodName.@Key && args.Count == 2) {
            var callRet = @Key(global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(args[1]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<long>(callRet);
            return true;
        }
        if (method == MethodName.@ClearNodeInformation && args.Count == 0) {
            @ClearNodeInformation();
            ret = default;
            return true;
        }
        if (method == MethodName.@InspectNode && args.Count == 2) {
            @InspectNode(global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[1]));
            ret = default;
            return true;
        }
        if (method == MethodName.@NodeTint && args.Count == 5) {
            var callRet = @NodeTint(global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(args[1]), global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(args[2]), global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(args[3]), global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(args[4]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.Color>(callRet);
            return true;
        }
        if (method == MethodName.@AddLabel && args.Count == 5) {
            @AddLabel(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Control>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[1]), global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Rect2>(args[2]), global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(args[3]), global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Color>(args[4]));
            ret = default;
            return true;
        }
        if (method == MethodName.@AddPicture && args.Count == 4) {
            @AddPicture(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Control>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[1]), global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Rect2>(args[2]), global::Godot.NativeInterop.VariantUtils.ConvertTo<float>(args[3]));
            ret = default;
            return true;
        }
        if (method == MethodName.@DrawEdge && args.Count == 5) {
            @DrawEdge(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Vector2>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Vector2>(args[1]), global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(args[2]), global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(args[3]), global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(args[4]));
            ret = default;
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
        if (method == MethodName.@DistanceToSegment && args.Count == 3) {
            var callRet = @DistanceToSegment(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Vector2>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Vector2>(args[1]), global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Vector2>(args[2]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<float>(callRet);
            return true;
        }
        if (method == MethodName.@Key && args.Count == 2) {
            var callRet = @Key(global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(args[1]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<long>(callRet);
            return true;
        }
        if (method == MethodName.@NodeTint && args.Count == 5) {
            var callRet = @NodeTint(global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(args[1]), global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(args[2]), global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(args[3]), global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(args[4]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.Color>(callRet);
            return true;
        }
        if (method == MethodName.@AddLabel && args.Count == 5) {
            @AddLabel(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Control>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[1]), global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Rect2>(args[2]), global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(args[3]), global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Color>(args[4]));
            ret = default;
            return true;
        }
        if (method == MethodName.@AddPicture && args.Count == 4) {
            @AddPicture(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Control>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[1]), global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Rect2>(args[2]), global::Godot.NativeInterop.VariantUtils.ConvertTo<float>(args[3]));
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
        if (method == MethodName.@SetDrawing) {
           return true;
        }
        if (method == MethodName.@TogglePen) {
           return true;
        }
        if (method == MethodName.@ToggleEraser) {
           return true;
        }
        if (method == MethodName.@UpdateToolState) {
           return true;
        }
        if (method == MethodName.@UndoAnnotation) {
           return true;
        }
        if (method == MethodName.@ClearAnnotations) {
           return true;
        }
        if (method == MethodName.@SaveAnnotations) {
           return true;
        }
        if (method == MethodName.@_GuiInput) {
           return true;
        }
        if (method == MethodName.@HandlePointer) {
           return true;
        }
        if (method == MethodName.@FinishPointer) {
           return true;
        }
        if (method == MethodName.@EraseAt) {
           return true;
        }
        if (method == MethodName.@DistanceToSegment) {
           return true;
        }
        if (method == MethodName.@RedrawAnnotations) {
           return true;
        }
        if (method == MethodName.@DrawAnnotations) {
           return true;
        }
        if (method == MethodName.@Point) {
           return true;
        }
        if (method == MethodName.@Center) {
           return true;
        }
        if (method == MethodName.@Key) {
           return true;
        }
        if (method == MethodName.@ClearNodeInformation) {
           return true;
        }
        if (method == MethodName.@InspectNode) {
           return true;
        }
        if (method == MethodName.@NodeTint) {
           return true;
        }
        if (method == MethodName.@AddLabel) {
           return true;
        }
        if (method == MethodName.@AddPicture) {
           return true;
        }
        if (method == MethodName.@DrawEdge) {
           return true;
        }
        if (method == MethodName.@_Draw) {
           return true;
        }
        return base.HasGodotClassMethod(method);
    }
}

}

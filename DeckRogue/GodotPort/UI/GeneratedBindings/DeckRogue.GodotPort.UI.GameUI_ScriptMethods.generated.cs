using Godot;
using Godot.NativeInterop;

namespace DeckRogue.GodotPort.UI {

partial class GameUI
{
#pragma warning disable CS0109 // Disable warning about redundant 'new' keyword
    /// <summary>
    /// Cached StringNames for the methods contained in this class, for fast lookup.
    /// </summary>
    public new class MethodName : global::Godot.Control.MethodName {
        /// <summary>
        /// Cached name for the 'AdvancePlay' method.
        /// </summary>
        public new static readonly global::Godot.StringName @AdvancePlay = "AdvancePlay";
        /// <summary>
        /// Cached name for the 'SelectionTitle' method.
        /// </summary>
        public new static readonly global::Godot.StringName @SelectionTitle = "SelectionTitle";
        /// <summary>
        /// Cached name for the 'BeginEndTurn' method.
        /// </summary>
        public new static readonly global::Godot.StringName @BeginEndTurn = "BeginEndTurn";
        /// <summary>
        /// Cached name for the 'FontSize' method.
        /// </summary>
        public new static readonly global::Godot.StringName @FontSize = "FontSize";
        /// <summary>
        /// Cached name for the '_Ready' method.
        /// </summary>
        public new static readonly global::Godot.StringName @_Ready = "_Ready";
        /// <summary>
        /// Cached name for the 'Bind' method.
        /// </summary>
        public new static readonly global::Godot.StringName @Bind = "Bind";
        /// <summary>
        /// Cached name for the 'PresentationCompleted' method.
        /// </summary>
        public new static readonly global::Godot.StringName @PresentationCompleted = "PresentationCompleted";
        /// <summary>
        /// Cached name for the '_ExitTree' method.
        /// </summary>
        public new static readonly global::Godot.StringName @_ExitTree = "_ExitTree";
        /// <summary>
        /// Cached name for the 'SelectEnemy' method.
        /// </summary>
        public new static readonly global::Godot.StringName @SelectEnemy = "SelectEnemy";
        /// <summary>
        /// Cached name for the '_UnhandledKeyInput' method.
        /// </summary>
        public new static readonly global::Godot.StringName @_UnhandledKeyInput = "_UnhandledKeyInput";
        /// <summary>
        /// Cached name for the 'Rebuild' method.
        /// </summary>
        public new static readonly global::Godot.StringName @Rebuild = "Rebuild";
        /// <summary>
        /// Cached name for the 'BuildHeader' method.
        /// </summary>
        public new static readonly global::Godot.StringName @BuildHeader = "BuildHeader";
        /// <summary>
        /// Cached name for the 'Heading' method.
        /// </summary>
        public new static readonly global::Godot.StringName @Heading = "Heading";
        /// <summary>
        /// Cached name for the 'CloseOverlay' method.
        /// </summary>
        public new static readonly global::Godot.StringName @CloseOverlay = "CloseOverlay";
        /// <summary>
        /// Cached name for the 'CloseAllOverlays' method.
        /// </summary>
        public new static readonly global::Godot.StringName @CloseAllOverlays = "CloseAllOverlays";
        /// <summary>
        /// Cached name for the 'BuildOverlay' method.
        /// </summary>
        public new static readonly global::Godot.StringName @BuildOverlay = "BuildOverlay";
        /// <summary>
        /// Cached name for the 'OpenMenu' method.
        /// </summary>
        public new static readonly global::Godot.StringName @OpenMenu = "OpenMenu";
        /// <summary>
        /// Cached name for the 'OpenKeywords' method.
        /// </summary>
        public new static readonly global::Godot.StringName @OpenKeywords = "OpenKeywords";
        /// <summary>
        /// Cached name for the 'OpenJournal' method.
        /// </summary>
        public new static readonly global::Godot.StringName @OpenJournal = "OpenJournal";
        /// <summary>
        /// Cached name for the 'OpenExport' method.
        /// </summary>
        public new static readonly global::Godot.StringName @OpenExport = "OpenExport";
        /// <summary>
        /// Cached name for the 'OpenRetainedExport' method.
        /// </summary>
        public new static readonly global::Godot.StringName @OpenRetainedExport = "OpenRetainedExport";
        /// <summary>
        /// Cached name for the 'OpenExportPair' method.
        /// </summary>
        public new static readonly global::Godot.StringName @OpenExportPair = "OpenExportPair";
        /// <summary>
        /// Cached name for the 'CopyText' method.
        /// </summary>
        public new static readonly global::Godot.StringName @CopyText = "CopyText";
        /// <summary>
        /// Cached name for the 'ExportFile' method.
        /// </summary>
        public new static readonly global::Godot.StringName @ExportFile = "ExportFile";
        /// <summary>
        /// Cached name for the 'ImportFile' method.
        /// </summary>
        public new static readonly global::Godot.StringName @ImportFile = "ImportFile";
        /// <summary>
        /// Cached name for the 'OpenImport' method.
        /// </summary>
        public new static readonly global::Godot.StringName @OpenImport = "OpenImport";
        /// <summary>
        /// Cached name for the 'ImportText' method.
        /// </summary>
        public new static readonly global::Godot.StringName @ImportText = "ImportText";
        /// <summary>
        /// Cached name for the '_Notification' method.
        /// </summary>
        public new static readonly global::Godot.StringName @_Notification = "_Notification";
        /// <summary>
        /// Cached name for the 'HandleBack' method.
        /// </summary>
        public new static readonly global::Godot.StringName @HandleBack = "HandleBack";
        /// <summary>
        /// Cached name for the 'ApplySafeArea' method.
        /// </summary>
        public new static readonly global::Godot.StringName @ApplySafeArea = "ApplySafeArea";
        /// <summary>
        /// Cached name for the 'ReadSettings' method.
        /// </summary>
        public new static readonly global::Godot.StringName @ReadSettings = "ReadSettings";
        /// <summary>
        /// Cached name for the 'ApplyAudio' method.
        /// </summary>
        public new static readonly global::Godot.StringName @ApplyAudio = "ApplyAudio";
        /// <summary>
        /// Cached name for the 'SaveSettings' method.
        /// </summary>
        public new static readonly global::Godot.StringName @SaveSettings = "SaveSettings";
        /// <summary>
        /// Cached name for the 'OpenSettings' method.
        /// </summary>
        public new static readonly global::Godot.StringName @OpenSettings = "OpenSettings";
        /// <summary>
        /// Cached name for the 'ApplyTheme' method.
        /// </summary>
        public new static readonly global::Godot.StringName @ApplyTheme = "ApplyTheme";
        /// <summary>
        /// Cached name for the 'Box' method.
        /// </summary>
        public new static readonly global::Godot.StringName @Box = "Box";
        /// <summary>
        /// Cached name for the 'Plain' method.
        /// </summary>
        public new static readonly global::Godot.StringName @Plain = "Plain";
        /// <summary>
        /// Cached name for the 'SizeInteractiveControls' method.
        /// </summary>
        public new static readonly global::Godot.StringName @SizeInteractiveControls = "SizeInteractiveControls";
        /// <summary>
        /// Cached name for the 'Row' method.
        /// </summary>
        public new static readonly global::Godot.StringName @Row = "Row";
        /// <summary>
        /// Cached name for the 'Column' method.
        /// </summary>
        public new static readonly global::Godot.StringName @Column = "Column";
        /// <summary>
        /// Cached name for the 'ScrollColumn' method.
        /// </summary>
        public new static readonly global::Godot.StringName @ScrollColumn = "ScrollColumn";
        /// <summary>
        /// Cached name for the 'Flow' method.
        /// </summary>
        public new static readonly global::Godot.StringName @Flow = "Flow";
        /// <summary>
        /// Cached name for the 'Art' method.
        /// </summary>
        public new static readonly global::Godot.StringName @Art = "Art";
        /// <summary>
        /// Cached name for the 'Picture' method.
        /// </summary>
        public new static readonly global::Godot.StringName @Picture = "Picture";
        /// <summary>
        /// Cached name for the 'Tile' method.
        /// </summary>
        public new static readonly global::Godot.StringName @Tile = "Tile";
        /// <summary>
        /// Cached name for the 'CardColor' method.
        /// </summary>
        public new static readonly global::Godot.StringName @CardColor = "CardColor";
        /// <summary>
        /// Cached name for the 'EnemyVitalsText' method.
        /// </summary>
        public new static readonly global::Godot.StringName @EnemyVitalsText = "EnemyVitalsText";
        /// <summary>
        /// Cached name for the 'EnemySurface' method.
        /// </summary>
        public new static readonly global::Godot.StringName @EnemySurface = "EnemySurface";
        /// <summary>
        /// Cached name for the '_Process' method.
        /// </summary>
        public new static readonly global::Godot.StringName @_Process = "_Process";
        /// <summary>
        /// Cached name for the 'PositionEnemyHud' method.
        /// </summary>
        public new static readonly global::Godot.StringName @PositionEnemyHud = "PositionEnemyHud";
        /// <summary>
        /// Cached name for the 'OpenNote' method.
        /// </summary>
        public new static readonly global::Godot.StringName @OpenNote = "OpenNote";
        /// <summary>
        /// Cached name for the 'OpenRating' method.
        /// </summary>
        public new static readonly global::Godot.StringName @OpenRating = "OpenRating";
        /// <summary>
        /// Cached name for the 'Paints' method.
        /// </summary>
        public new static readonly global::Godot.StringName @Paints = "Paints";
        /// <summary>
        /// Cached name for the 'VisibleAlpha' method.
        /// </summary>
        public new static readonly global::Godot.StringName @VisibleAlpha = "VisibleAlpha";
        /// <summary>
        /// Cached name for the 'ViewportBounds' method.
        /// </summary>
        public new static readonly global::Godot.StringName @ViewportBounds = "ViewportBounds";
        /// <summary>
        /// Cached name for the 'Set' method.
        /// </summary>
        public new static readonly global::Godot.StringName @Set = "Set";
        /// <summary>
        /// Cached name for the 'CanDropHandCard' method.
        /// </summary>
        public new static readonly global::Godot.StringName @CanDropHandCard = "CanDropHandCard";
        /// <summary>
        /// Cached name for the 'DropHandCard' method.
        /// </summary>
        public new static readonly global::Godot.StringName @DropHandCard = "DropHandCard";
        /// <summary>
        /// Cached name for the 'PlayAllSparks' method.
        /// </summary>
        public new static readonly global::Godot.StringName @PlayAllSparks = "PlayAllSparks";
        /// <summary>
        /// Cached name for the 'PumpSparks' method.
        /// </summary>
        public new static readonly global::Godot.StringName @PumpSparks = "PumpSparks";
        /// <summary>
        /// Cached name for the 'ContinueFromTitle' method.
        /// </summary>
        public new static readonly global::Godot.StringName @ContinueFromTitle = "ContinueFromTitle";
        /// <summary>
        /// Cached name for the 'Sound' method.
        /// </summary>
        public new static readonly global::Godot.StringName @Sound = "Sound";
        /// <summary>
        /// Cached name for the 'OfferQueuedRating' method.
        /// </summary>
        public new static readonly global::Godot.StringName @OfferQueuedRating = "OfferQueuedRating";
        /// <summary>
        /// Cached name for the 'InspectEnemy' method.
        /// </summary>
        public new static readonly global::Godot.StringName @InspectEnemy = "InspectEnemy";
        /// <summary>
        /// Cached name for the 'DisableUnderlyingFocus' method.
        /// </summary>
        public new static readonly global::Godot.StringName @DisableUnderlyingFocus = "DisableUnderlyingFocus";
        /// <summary>
        /// Cached name for the 'FirstFocusable' method.
        /// </summary>
        public new static readonly global::Godot.StringName @FirstFocusable = "FirstFocusable";
        /// <summary>
        /// Cached name for the 'ShowTitle' method.
        /// </summary>
        public new static readonly global::Godot.StringName @ShowTitle = "ShowTitle";
        /// <summary>
        /// Cached name for the 'SharePair' method.
        /// </summary>
        public new static readonly global::Godot.StringName @SharePair = "SharePair";
        /// <summary>
        /// Cached name for the 'ShareRequestFinished' method.
        /// </summary>
        public new static readonly global::Godot.StringName @ShareRequestFinished = "ShareRequestFinished";
        /// <summary>
        /// Cached name for the 'BuildTitle' method.
        /// </summary>
        public new static readonly global::Godot.StringName @BuildTitle = "BuildTitle";
        /// <summary>
        /// Cached name for the 'InvokeAction' method.
        /// </summary>
        public new static readonly global::Godot.StringName @InvokeAction = "InvokeAction";
        /// <summary>
        /// Cached name for the 'UxResetHand' method.
        /// </summary>
        public new static readonly global::Godot.StringName @UxResetHand = "UxResetHand";
        /// <summary>
        /// Cached name for the 'UxText' method.
        /// </summary>
        public new static readonly global::Godot.StringName @UxText = "UxText";
        /// <summary>
        /// Cached name for the 'UxPanelBox' method.
        /// </summary>
        public new static readonly global::Godot.StringName @UxPanelBox = "UxPanelBox";
        /// <summary>
        /// Cached name for the 'BuildBattleHeader' method.
        /// </summary>
        public new static readonly global::Godot.StringName @BuildBattleHeader = "BuildBattleHeader";
        /// <summary>
        /// Cached name for the 'CreateHpReadout' method.
        /// </summary>
        public new static readonly global::Godot.StringName @CreateHpReadout = "CreateHpReadout";
        /// <summary>
        /// Cached name for the 'UxRich' method.
        /// </summary>
        public new static readonly global::Godot.StringName @UxRich = "UxRich";
        /// <summary>
        /// Cached name for the 'PositionUxEnemies' method.
        /// </summary>
        public new static readonly global::Godot.StringName @PositionUxEnemies = "PositionUxEnemies";
        /// <summary>
        /// Cached name for the 'RefreshUxReadouts' method.
        /// </summary>
        public new static readonly global::Godot.StringName @RefreshUxReadouts = "RefreshUxReadouts";
        /// <summary>
        /// Cached name for the 'SyncUxHand' method.
        /// </summary>
        public new static readonly global::Godot.StringName @SyncUxHand = "SyncUxHand";
        /// <summary>
        /// Cached name for the 'MakeUxDragPreview' method.
        /// </summary>
        public new static readonly global::Godot.StringName @MakeUxDragPreview = "MakeUxDragPreview";
        /// <summary>
        /// Cached name for the 'HoverUxHand' method.
        /// </summary>
        public new static readonly global::Godot.StringName @HoverUxHand = "HoverUxHand";
        /// <summary>
        /// Cached name for the 'RestoreUxHandOrder' method.
        /// </summary>
        public new static readonly global::Godot.StringName @RestoreUxHandOrder = "RestoreUxHandOrder";
        /// <summary>
        /// Cached name for the 'StartHandDrag' method.
        /// </summary>
        public new static readonly global::Godot.StringName @StartHandDrag = "StartHandDrag";
        /// <summary>
        /// Cached name for the 'EndHandDrag' method.
        /// </summary>
        public new static readonly global::Godot.StringName @EndHandDrag = "EndHandDrag";
        /// <summary>
        /// Cached name for the 'DecodeHandDrag' method.
        /// </summary>
        public new static readonly global::Godot.StringName @DecodeHandDrag = "DecodeHandDrag";
        /// <summary>
        /// Cached name for the 'UpdateHandAim' method.
        /// </summary>
        public new static readonly global::Godot.StringName @UpdateHandAim = "UpdateHandAim";
        /// <summary>
        /// Cached name for the 'UxProcess' method.
        /// </summary>
        public new static readonly global::Godot.StringName @UxProcess = "UxProcess";
        /// <summary>
        /// Cached name for the 'AlignUxRichLine' method.
        /// </summary>
        public new static readonly global::Godot.StringName @AlignUxRichLine = "AlignUxRichLine";
        /// <summary>
        /// Cached name for the 'UxRelayout' method.
        /// </summary>
        public new static readonly global::Godot.StringName @UxRelayout = "UxRelayout";
    }
    /// <summary>
    /// Get the method information for all the methods declared in this class.
    /// This method is used by Godot to register the available methods in the editor.
    /// Do not call this method.
    /// </summary>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal new static global::System.Collections.Generic.List<global::Godot.Bridge.MethodInfo> GetGodotMethodList()
    {
        var methods = new global::System.Collections.Generic.List<global::Godot.Bridge.MethodInfo>(90);
        methods.Add(new(name: MethodName.@AdvancePlay, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@SelectionTitle, returnVal: new(type: (global::Godot.Variant.Type)4, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)33, arguments: new() { new(type: (global::Godot.Variant.Type)4, name: "need", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@BeginEndTurn, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@FontSize, returnVal: new(type: (global::Godot.Variant.Type)2, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)2, name: "requested", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@_Ready, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@Bind, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)24, name: "session", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("Node")),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@PresentationCompleted, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@_ExitTree, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@SelectEnemy, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)2, name: "index", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@_UnhandledKeyInput, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)24, name: "evt", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("InputEvent")),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@Rebuild, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@BuildHeader, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@Heading, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)4, name: "title", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)4, name: "subtitle", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@CloseOverlay, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@CloseAllOverlays, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@BuildOverlay, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@OpenMenu, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@OpenKeywords, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@OpenJournal, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@OpenExport, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@OpenRetainedExport, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@OpenExportPair, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)4, name: "save", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)4, name: "report", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)4, name: "title", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@CopyText, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)4, name: "text", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@ExportFile, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)4, name: "text", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)4, name: "name", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@ImportFile, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@OpenImport, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@ImportText, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)4, name: "text", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@_Notification, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)2, name: "what", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@HandleBack, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@ApplySafeArea, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@ReadSettings, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@ApplyAudio, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@SaveSettings, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@OpenSettings, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@ApplyTheme, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@Box, returnVal: new(type: (global::Godot.Variant.Type)24, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("StyleBoxFlat")), flags: (global::Godot.MethodFlags)33, arguments: new() { new(type: (global::Godot.Variant.Type)20, name: "color", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)20, name: "border", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)2, name: "width", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@Plain, returnVal: new(type: (global::Godot.Variant.Type)4, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)33, arguments: new() { new(type: (global::Godot.Variant.Type)4, name: "text", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@SizeInteractiveControls, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)24, name: "node", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("Node")),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@Row, returnVal: new(type: (global::Godot.Variant.Type)24, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("HBoxContainer")), flags: (global::Godot.MethodFlags)33, arguments: new() { new(type: (global::Godot.Variant.Type)24, name: "parent", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("Node")),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@Column, returnVal: new(type: (global::Godot.Variant.Type)24, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("VBoxContainer")), flags: (global::Godot.MethodFlags)33, arguments: new() { new(type: (global::Godot.Variant.Type)24, name: "parent", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("Node")),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@ScrollColumn, returnVal: new(type: (global::Godot.Variant.Type)24, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("VBoxContainer")), flags: (global::Godot.MethodFlags)33, arguments: new() { new(type: (global::Godot.Variant.Type)24, name: "parent", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("Node")),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@Flow, returnVal: new(type: (global::Godot.Variant.Type)24, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("HFlowContainer")), flags: (global::Godot.MethodFlags)33, arguments: new() { new(type: (global::Godot.Variant.Type)24, name: "parent", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("Node")),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@Art, returnVal: new(type: (global::Godot.Variant.Type)24, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("Texture2D")), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)4, name: "group", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)4, name: "id", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@Picture, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)24, name: "parent", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("Node")), new(type: (global::Godot.Variant.Type)4, name: "group", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)4, name: "id", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)5, name: "size", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@Tile, returnVal: new(type: (global::Godot.Variant.Type)24, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("VBoxContainer")), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)24, name: "parent", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("Node")), new(type: (global::Godot.Variant.Type)4, name: "name", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)4, name: "description", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)4, name: "group", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)4, name: "id", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@CardColor, returnVal: new(type: (global::Godot.Variant.Type)20, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)33, arguments: new() { new(type: (global::Godot.Variant.Type)4, name: "color", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@EnemyVitalsText, returnVal: new(type: (global::Godot.Variant.Type)4, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)2, name: "hp", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)2, name: "maxHp", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)2, name: "block", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@EnemySurface, returnVal: new(type: (global::Godot.Variant.Type)24, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("PanelContainer")), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)24, name: "parent", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("Node")), new(type: (global::Godot.Variant.Type)2, name: "index", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)4, name: "kind", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)20, name: "border", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@_Process, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)3, name: "delta", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@PositionEnemyHud, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@OpenNote, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@OpenRating, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@Paints, returnVal: new(type: (global::Godot.Variant.Type)1, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)33, arguments: new() { new(type: (global::Godot.Variant.Type)24, name: "style", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("StyleBox")),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@VisibleAlpha, returnVal: new(type: (global::Godot.Variant.Type)3, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)33, arguments: new() { new(type: (global::Godot.Variant.Type)24, name: "item", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("CanvasItem")),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@ViewportBounds, returnVal: new(type: (global::Godot.Variant.Type)7, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)33, arguments: new() { new(type: (global::Godot.Variant.Type)24, name: "control", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("Control")),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@Set, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)33, arguments: new() { new(type: (global::Godot.Variant.Type)24, name: "label", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("Label")), new(type: (global::Godot.Variant.Type)4, name: "text", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@CanDropHandCard, returnVal: new(type: (global::Godot.Variant.Type)1, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)4, name: "uid", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@DropHandCard, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)4, name: "uid", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)5, name: "viewportPoint", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@PlayAllSparks, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@PumpSparks, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@ContinueFromTitle, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@Sound, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)4, name: "key", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@OfferQueuedRating, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@InspectEnemy, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)2, name: "index", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@DisableUnderlyingFocus, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)33, arguments: new() { new(type: (global::Godot.Variant.Type)24, name: "node", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("Node")),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@FirstFocusable, returnVal: new(type: (global::Godot.Variant.Type)24, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("Control")), flags: (global::Godot.MethodFlags)33, arguments: new() { new(type: (global::Godot.Variant.Type)24, name: "node", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("Node")),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@ShowTitle, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@SharePair, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)4, name: "report", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)4, name: "save", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@ShareRequestFinished, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)1, name: "opened", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)4, name: "message", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@BuildTitle, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@InvokeAction, returnVal: new(type: (global::Godot.Variant.Type)1, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)4, name: "exactLabel", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@UxResetHand, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@UxText, returnVal: new(type: (global::Godot.Variant.Type)24, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("Label")), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)24, name: "parent", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("Control")), new(type: (global::Godot.Variant.Type)4, name: "text", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)2, name: "units", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)20, name: "color", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)1, name: "deco", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)1, name: "bold", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@UxPanelBox, returnVal: new(type: (global::Godot.Variant.Type)24, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("StyleBoxFlat")), flags: (global::Godot.MethodFlags)33, arguments: new() { new(type: (global::Godot.Variant.Type)20, name: "color", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)20, name: "edge", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)2, name: "width", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@BuildBattleHeader, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@CreateHpReadout, returnVal: new(type: (global::Godot.Variant.Type)24, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("Label")), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)24, name: "parent", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("Control")), new(type: (global::Godot.Variant.Type)4, name: "text", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)2, name: "sourceSize", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@UxRich, returnVal: new(type: (global::Godot.Variant.Type)24, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("RichTextLabel")), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)24, name: "p", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("Control")), new(type: (global::Godot.Variant.Type)4, name: "name", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)2, name: "size", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)1, name: "deco", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@PositionUxEnemies, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@RefreshUxReadouts, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@SyncUxHand, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@MakeUxDragPreview, returnVal: new(type: (global::Godot.Variant.Type)24, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("Control")), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)4, name: "uid", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@HoverUxHand, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)4, name: "uid", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), new(type: (global::Godot.Variant.Type)1, name: "enter", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@RestoreUxHandOrder, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@StartHandDrag, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)4, name: "uid", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@EndHandDrag, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@DecodeHandDrag, returnVal: new(type: (global::Godot.Variant.Type)4, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)0, name: "data", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)131078, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@UpdateHandAim, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        methods.Add(new(name: MethodName.@UxProcess, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: new() { new(type: (global::Godot.Variant.Type)3, name: "delta", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@AlignUxRichLine, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)33, arguments: new() { new(type: (global::Godot.Variant.Type)24, name: "label", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false, className: new global::Godot.StringName("RichTextLabel")),  }, defaultArguments: null));
        methods.Add(new(name: MethodName.@UxRelayout, returnVal: new(type: (global::Godot.Variant.Type)0, name: "", hint: (global::Godot.PropertyHint)0, hintString: "", usage: (global::Godot.PropertyUsageFlags)6, exported: false), flags: (global::Godot.MethodFlags)1, arguments: null, defaultArguments: null));
        return methods;
    }
#pragma warning restore CS0109
    /// <inheritdoc/>
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
    {
        if (method == MethodName.@AdvancePlay && args.Count == 0) {
            @AdvancePlay();
            ret = default;
            return true;
        }
        if (method == MethodName.@SelectionTitle && args.Count == 1) {
            var callRet = @SelectionTitle(global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[0]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<string>(callRet);
            return true;
        }
        if (method == MethodName.@BeginEndTurn && args.Count == 0) {
            @BeginEndTurn();
            ret = default;
            return true;
        }
        if (method == MethodName.@FontSize && args.Count == 1) {
            var callRet = @FontSize(global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(args[0]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<int>(callRet);
            return true;
        }
        if (method == MethodName.@_Ready && args.Count == 0) {
            @_Ready();
            ret = default;
            return true;
        }
        if (method == MethodName.@Bind && args.Count == 1) {
            @Bind(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::DeckRogue.GodotPort.Core.GameSession>(args[0]));
            ret = default;
            return true;
        }
        if (method == MethodName.@PresentationCompleted && args.Count == 0) {
            @PresentationCompleted();
            ret = default;
            return true;
        }
        if (method == MethodName.@_ExitTree && args.Count == 0) {
            @_ExitTree();
            ret = default;
            return true;
        }
        if (method == MethodName.@SelectEnemy && args.Count == 1) {
            @SelectEnemy(global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(args[0]));
            ret = default;
            return true;
        }
        if (method == MethodName.@_UnhandledKeyInput && args.Count == 1) {
            @_UnhandledKeyInput(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.InputEvent>(args[0]));
            ret = default;
            return true;
        }
        if (method == MethodName.@Rebuild && args.Count == 0) {
            @Rebuild();
            ret = default;
            return true;
        }
        if (method == MethodName.@BuildHeader && args.Count == 0) {
            @BuildHeader();
            ret = default;
            return true;
        }
        if (method == MethodName.@Heading && args.Count == 2) {
            @Heading(global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[1]));
            ret = default;
            return true;
        }
        if (method == MethodName.@CloseOverlay && args.Count == 0) {
            @CloseOverlay();
            ret = default;
            return true;
        }
        if (method == MethodName.@CloseAllOverlays && args.Count == 0) {
            @CloseAllOverlays();
            ret = default;
            return true;
        }
        if (method == MethodName.@BuildOverlay && args.Count == 0) {
            @BuildOverlay();
            ret = default;
            return true;
        }
        if (method == MethodName.@OpenMenu && args.Count == 0) {
            @OpenMenu();
            ret = default;
            return true;
        }
        if (method == MethodName.@OpenKeywords && args.Count == 0) {
            @OpenKeywords();
            ret = default;
            return true;
        }
        if (method == MethodName.@OpenJournal && args.Count == 0) {
            @OpenJournal();
            ret = default;
            return true;
        }
        if (method == MethodName.@OpenExport && args.Count == 0) {
            @OpenExport();
            ret = default;
            return true;
        }
        if (method == MethodName.@OpenRetainedExport && args.Count == 0) {
            @OpenRetainedExport();
            ret = default;
            return true;
        }
        if (method == MethodName.@OpenExportPair && args.Count == 3) {
            @OpenExportPair(global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[1]), global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[2]));
            ret = default;
            return true;
        }
        if (method == MethodName.@CopyText && args.Count == 1) {
            @CopyText(global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[0]));
            ret = default;
            return true;
        }
        if (method == MethodName.@ExportFile && args.Count == 2) {
            @ExportFile(global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[1]));
            ret = default;
            return true;
        }
        if (method == MethodName.@ImportFile && args.Count == 0) {
            @ImportFile();
            ret = default;
            return true;
        }
        if (method == MethodName.@OpenImport && args.Count == 0) {
            @OpenImport();
            ret = default;
            return true;
        }
        if (method == MethodName.@ImportText && args.Count == 1) {
            @ImportText(global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[0]));
            ret = default;
            return true;
        }
        if (method == MethodName.@_Notification && args.Count == 1) {
            @_Notification(global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(args[0]));
            ret = default;
            return true;
        }
        if (method == MethodName.@HandleBack && args.Count == 0) {
            @HandleBack();
            ret = default;
            return true;
        }
        if (method == MethodName.@ApplySafeArea && args.Count == 0) {
            @ApplySafeArea();
            ret = default;
            return true;
        }
        if (method == MethodName.@ReadSettings && args.Count == 0) {
            @ReadSettings();
            ret = default;
            return true;
        }
        if (method == MethodName.@ApplyAudio && args.Count == 0) {
            @ApplyAudio();
            ret = default;
            return true;
        }
        if (method == MethodName.@SaveSettings && args.Count == 0) {
            @SaveSettings();
            ret = default;
            return true;
        }
        if (method == MethodName.@OpenSettings && args.Count == 0) {
            @OpenSettings();
            ret = default;
            return true;
        }
        if (method == MethodName.@ApplyTheme && args.Count == 0) {
            @ApplyTheme();
            ret = default;
            return true;
        }
        if (method == MethodName.@Box && args.Count == 3) {
            var callRet = @Box(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Color>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Color>(args[1]), global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(args[2]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.StyleBoxFlat>(callRet);
            return true;
        }
        if (method == MethodName.@Plain && args.Count == 1) {
            var callRet = @Plain(global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[0]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<string>(callRet);
            return true;
        }
        if (method == MethodName.@SizeInteractiveControls && args.Count == 1) {
            @SizeInteractiveControls(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Node>(args[0]));
            ret = default;
            return true;
        }
        if (method == MethodName.@Row && args.Count == 1) {
            var callRet = @Row(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Node>(args[0]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.HBoxContainer>(callRet);
            return true;
        }
        if (method == MethodName.@Column && args.Count == 1) {
            var callRet = @Column(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Node>(args[0]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.VBoxContainer>(callRet);
            return true;
        }
        if (method == MethodName.@ScrollColumn && args.Count == 1) {
            var callRet = @ScrollColumn(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Node>(args[0]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.VBoxContainer>(callRet);
            return true;
        }
        if (method == MethodName.@Flow && args.Count == 1) {
            var callRet = @Flow(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Node>(args[0]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.HFlowContainer>(callRet);
            return true;
        }
        if (method == MethodName.@Art && args.Count == 2) {
            var callRet = @Art(global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[1]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.Texture2D>(callRet);
            return true;
        }
        if (method == MethodName.@Picture && args.Count == 4) {
            @Picture(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Node>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[1]), global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[2]), global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Vector2>(args[3]));
            ret = default;
            return true;
        }
        if (method == MethodName.@Tile && args.Count == 5) {
            var callRet = @Tile(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Node>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[1]), global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[2]), global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[3]), global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[4]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.VBoxContainer>(callRet);
            return true;
        }
        if (method == MethodName.@CardColor && args.Count == 1) {
            var callRet = @CardColor(global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[0]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.Color>(callRet);
            return true;
        }
        if (method == MethodName.@EnemyVitalsText && args.Count == 3) {
            var callRet = @EnemyVitalsText(global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(args[1]), global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(args[2]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<string>(callRet);
            return true;
        }
        if (method == MethodName.@EnemySurface && args.Count == 4) {
            var callRet = @EnemySurface(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Node>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(args[1]), global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[2]), global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Color>(args[3]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.PanelContainer>(callRet);
            return true;
        }
        if (method == MethodName.@_Process && args.Count == 1) {
            @_Process(global::Godot.NativeInterop.VariantUtils.ConvertTo<double>(args[0]));
            ret = default;
            return true;
        }
        if (method == MethodName.@PositionEnemyHud && args.Count == 0) {
            @PositionEnemyHud();
            ret = default;
            return true;
        }
        if (method == MethodName.@OpenNote && args.Count == 0) {
            @OpenNote();
            ret = default;
            return true;
        }
        if (method == MethodName.@OpenRating && args.Count == 0) {
            @OpenRating();
            ret = default;
            return true;
        }
        if (method == MethodName.@Paints && args.Count == 1) {
            var callRet = @Paints(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.StyleBox>(args[0]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<bool>(callRet);
            return true;
        }
        if (method == MethodName.@VisibleAlpha && args.Count == 1) {
            var callRet = @VisibleAlpha(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.CanvasItem>(args[0]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<float>(callRet);
            return true;
        }
        if (method == MethodName.@ViewportBounds && args.Count == 1) {
            var callRet = @ViewportBounds(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Control>(args[0]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.Rect2>(callRet);
            return true;
        }
        if (method == MethodName.@Set && args.Count == 2) {
            @Set(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Label>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[1]));
            ret = default;
            return true;
        }
        if (method == MethodName.@CanDropHandCard && args.Count == 1) {
            var callRet = @CanDropHandCard(global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[0]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<bool>(callRet);
            return true;
        }
        if (method == MethodName.@DropHandCard && args.Count == 2) {
            @DropHandCard(global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Vector2>(args[1]));
            ret = default;
            return true;
        }
        if (method == MethodName.@PlayAllSparks && args.Count == 0) {
            @PlayAllSparks();
            ret = default;
            return true;
        }
        if (method == MethodName.@PumpSparks && args.Count == 0) {
            @PumpSparks();
            ret = default;
            return true;
        }
        if (method == MethodName.@ContinueFromTitle && args.Count == 0) {
            @ContinueFromTitle();
            ret = default;
            return true;
        }
        if (method == MethodName.@Sound && args.Count == 1) {
            @Sound(global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[0]));
            ret = default;
            return true;
        }
        if (method == MethodName.@OfferQueuedRating && args.Count == 0) {
            @OfferQueuedRating();
            ret = default;
            return true;
        }
        if (method == MethodName.@InspectEnemy && args.Count == 1) {
            @InspectEnemy(global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(args[0]));
            ret = default;
            return true;
        }
        if (method == MethodName.@DisableUnderlyingFocus && args.Count == 1) {
            @DisableUnderlyingFocus(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Node>(args[0]));
            ret = default;
            return true;
        }
        if (method == MethodName.@FirstFocusable && args.Count == 1) {
            var callRet = @FirstFocusable(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Node>(args[0]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.Control>(callRet);
            return true;
        }
        if (method == MethodName.@ShowTitle && args.Count == 0) {
            @ShowTitle();
            ret = default;
            return true;
        }
        if (method == MethodName.@SharePair && args.Count == 2) {
            @SharePair(global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[1]));
            ret = default;
            return true;
        }
        if (method == MethodName.@ShareRequestFinished && args.Count == 2) {
            @ShareRequestFinished(global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[1]));
            ret = default;
            return true;
        }
        if (method == MethodName.@BuildTitle && args.Count == 0) {
            @BuildTitle();
            ret = default;
            return true;
        }
        if (method == MethodName.@InvokeAction && args.Count == 1) {
            var callRet = @InvokeAction(global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[0]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<bool>(callRet);
            return true;
        }
        if (method == MethodName.@UxResetHand && args.Count == 0) {
            @UxResetHand();
            ret = default;
            return true;
        }
        if (method == MethodName.@UxText && args.Count == 6) {
            var callRet = @UxText(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Control>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[1]), global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(args[2]), global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Color>(args[3]), global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(args[4]), global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(args[5]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.Label>(callRet);
            return true;
        }
        if (method == MethodName.@UxPanelBox && args.Count == 3) {
            var callRet = @UxPanelBox(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Color>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Color>(args[1]), global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(args[2]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.StyleBoxFlat>(callRet);
            return true;
        }
        if (method == MethodName.@BuildBattleHeader && args.Count == 0) {
            @BuildBattleHeader();
            ret = default;
            return true;
        }
        if (method == MethodName.@CreateHpReadout && args.Count == 3) {
            var callRet = @CreateHpReadout(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Control>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[1]), global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(args[2]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::DeckRogue.GodotPort.UI.SourceHudLabel>(callRet);
            return true;
        }
        if (method == MethodName.@UxRich && args.Count == 4) {
            var callRet = @UxRich(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Control>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[1]), global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(args[2]), global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(args[3]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.RichTextLabel>(callRet);
            return true;
        }
        if (method == MethodName.@PositionUxEnemies && args.Count == 0) {
            @PositionUxEnemies();
            ret = default;
            return true;
        }
        if (method == MethodName.@RefreshUxReadouts && args.Count == 0) {
            @RefreshUxReadouts();
            ret = default;
            return true;
        }
        if (method == MethodName.@SyncUxHand && args.Count == 0) {
            @SyncUxHand();
            ret = default;
            return true;
        }
        if (method == MethodName.@MakeUxDragPreview && args.Count == 1) {
            var callRet = @MakeUxDragPreview(global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[0]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.Control>(callRet);
            return true;
        }
        if (method == MethodName.@HoverUxHand && args.Count == 2) {
            @HoverUxHand(global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<bool>(args[1]));
            ret = default;
            return true;
        }
        if (method == MethodName.@RestoreUxHandOrder && args.Count == 0) {
            @RestoreUxHandOrder();
            ret = default;
            return true;
        }
        if (method == MethodName.@StartHandDrag && args.Count == 1) {
            @StartHandDrag(global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[0]));
            ret = default;
            return true;
        }
        if (method == MethodName.@EndHandDrag && args.Count == 0) {
            @EndHandDrag();
            ret = default;
            return true;
        }
        if (method == MethodName.@DecodeHandDrag && args.Count == 1) {
            var callRet = @DecodeHandDrag(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Variant>(args[0]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<string>(callRet);
            return true;
        }
        if (method == MethodName.@UpdateHandAim && args.Count == 0) {
            @UpdateHandAim();
            ret = default;
            return true;
        }
        if (method == MethodName.@UxProcess && args.Count == 1) {
            @UxProcess(global::Godot.NativeInterop.VariantUtils.ConvertTo<double>(args[0]));
            ret = default;
            return true;
        }
        if (method == MethodName.@AlignUxRichLine && args.Count == 1) {
            @AlignUxRichLine(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.RichTextLabel>(args[0]));
            ret = default;
            return true;
        }
        if (method == MethodName.@UxRelayout && args.Count == 0) {
            @UxRelayout();
            ret = default;
            return true;
        }
        return base.InvokeGodotClassMethod(method, args, out ret);
    }
#pragma warning disable CS0109 // Disable warning about redundant 'new' keyword
    [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal new static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
    {
        if (method == MethodName.@SelectionTitle && args.Count == 1) {
            var callRet = @SelectionTitle(global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[0]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<string>(callRet);
            return true;
        }
        if (method == MethodName.@Box && args.Count == 3) {
            var callRet = @Box(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Color>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Color>(args[1]), global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(args[2]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.StyleBoxFlat>(callRet);
            return true;
        }
        if (method == MethodName.@Plain && args.Count == 1) {
            var callRet = @Plain(global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[0]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<string>(callRet);
            return true;
        }
        if (method == MethodName.@Row && args.Count == 1) {
            var callRet = @Row(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Node>(args[0]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.HBoxContainer>(callRet);
            return true;
        }
        if (method == MethodName.@Column && args.Count == 1) {
            var callRet = @Column(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Node>(args[0]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.VBoxContainer>(callRet);
            return true;
        }
        if (method == MethodName.@ScrollColumn && args.Count == 1) {
            var callRet = @ScrollColumn(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Node>(args[0]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.VBoxContainer>(callRet);
            return true;
        }
        if (method == MethodName.@Flow && args.Count == 1) {
            var callRet = @Flow(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Node>(args[0]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.HFlowContainer>(callRet);
            return true;
        }
        if (method == MethodName.@CardColor && args.Count == 1) {
            var callRet = @CardColor(global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[0]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.Color>(callRet);
            return true;
        }
        if (method == MethodName.@Paints && args.Count == 1) {
            var callRet = @Paints(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.StyleBox>(args[0]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<bool>(callRet);
            return true;
        }
        if (method == MethodName.@VisibleAlpha && args.Count == 1) {
            var callRet = @VisibleAlpha(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.CanvasItem>(args[0]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<float>(callRet);
            return true;
        }
        if (method == MethodName.@ViewportBounds && args.Count == 1) {
            var callRet = @ViewportBounds(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Control>(args[0]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.Rect2>(callRet);
            return true;
        }
        if (method == MethodName.@Set && args.Count == 2) {
            @Set(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Label>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<string>(args[1]));
            ret = default;
            return true;
        }
        if (method == MethodName.@DisableUnderlyingFocus && args.Count == 1) {
            @DisableUnderlyingFocus(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Node>(args[0]));
            ret = default;
            return true;
        }
        if (method == MethodName.@FirstFocusable && args.Count == 1) {
            var callRet = @FirstFocusable(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Node>(args[0]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.Control>(callRet);
            return true;
        }
        if (method == MethodName.@UxPanelBox && args.Count == 3) {
            var callRet = @UxPanelBox(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Color>(args[0]), global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.Color>(args[1]), global::Godot.NativeInterop.VariantUtils.ConvertTo<int>(args[2]));
            ret = global::Godot.NativeInterop.VariantUtils.CreateFrom<global::Godot.StyleBoxFlat>(callRet);
            return true;
        }
        if (method == MethodName.@AlignUxRichLine && args.Count == 1) {
            @AlignUxRichLine(global::Godot.NativeInterop.VariantUtils.ConvertTo<global::Godot.RichTextLabel>(args[0]));
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
        if (method == MethodName.@AdvancePlay) {
           return true;
        }
        if (method == MethodName.@SelectionTitle) {
           return true;
        }
        if (method == MethodName.@BeginEndTurn) {
           return true;
        }
        if (method == MethodName.@FontSize) {
           return true;
        }
        if (method == MethodName.@_Ready) {
           return true;
        }
        if (method == MethodName.@Bind) {
           return true;
        }
        if (method == MethodName.@PresentationCompleted) {
           return true;
        }
        if (method == MethodName.@_ExitTree) {
           return true;
        }
        if (method == MethodName.@SelectEnemy) {
           return true;
        }
        if (method == MethodName.@_UnhandledKeyInput) {
           return true;
        }
        if (method == MethodName.@Rebuild) {
           return true;
        }
        if (method == MethodName.@BuildHeader) {
           return true;
        }
        if (method == MethodName.@Heading) {
           return true;
        }
        if (method == MethodName.@CloseOverlay) {
           return true;
        }
        if (method == MethodName.@CloseAllOverlays) {
           return true;
        }
        if (method == MethodName.@BuildOverlay) {
           return true;
        }
        if (method == MethodName.@OpenMenu) {
           return true;
        }
        if (method == MethodName.@OpenKeywords) {
           return true;
        }
        if (method == MethodName.@OpenJournal) {
           return true;
        }
        if (method == MethodName.@OpenExport) {
           return true;
        }
        if (method == MethodName.@OpenRetainedExport) {
           return true;
        }
        if (method == MethodName.@OpenExportPair) {
           return true;
        }
        if (method == MethodName.@CopyText) {
           return true;
        }
        if (method == MethodName.@ExportFile) {
           return true;
        }
        if (method == MethodName.@ImportFile) {
           return true;
        }
        if (method == MethodName.@OpenImport) {
           return true;
        }
        if (method == MethodName.@ImportText) {
           return true;
        }
        if (method == MethodName.@_Notification) {
           return true;
        }
        if (method == MethodName.@HandleBack) {
           return true;
        }
        if (method == MethodName.@ApplySafeArea) {
           return true;
        }
        if (method == MethodName.@ReadSettings) {
           return true;
        }
        if (method == MethodName.@ApplyAudio) {
           return true;
        }
        if (method == MethodName.@SaveSettings) {
           return true;
        }
        if (method == MethodName.@OpenSettings) {
           return true;
        }
        if (method == MethodName.@ApplyTheme) {
           return true;
        }
        if (method == MethodName.@Box) {
           return true;
        }
        if (method == MethodName.@Plain) {
           return true;
        }
        if (method == MethodName.@SizeInteractiveControls) {
           return true;
        }
        if (method == MethodName.@Row) {
           return true;
        }
        if (method == MethodName.@Column) {
           return true;
        }
        if (method == MethodName.@ScrollColumn) {
           return true;
        }
        if (method == MethodName.@Flow) {
           return true;
        }
        if (method == MethodName.@Art) {
           return true;
        }
        if (method == MethodName.@Picture) {
           return true;
        }
        if (method == MethodName.@Tile) {
           return true;
        }
        if (method == MethodName.@CardColor) {
           return true;
        }
        if (method == MethodName.@EnemyVitalsText) {
           return true;
        }
        if (method == MethodName.@EnemySurface) {
           return true;
        }
        if (method == MethodName.@_Process) {
           return true;
        }
        if (method == MethodName.@PositionEnemyHud) {
           return true;
        }
        if (method == MethodName.@OpenNote) {
           return true;
        }
        if (method == MethodName.@OpenRating) {
           return true;
        }
        if (method == MethodName.@Paints) {
           return true;
        }
        if (method == MethodName.@VisibleAlpha) {
           return true;
        }
        if (method == MethodName.@ViewportBounds) {
           return true;
        }
        if (method == MethodName.@Set) {
           return true;
        }
        if (method == MethodName.@CanDropHandCard) {
           return true;
        }
        if (method == MethodName.@DropHandCard) {
           return true;
        }
        if (method == MethodName.@PlayAllSparks) {
           return true;
        }
        if (method == MethodName.@PumpSparks) {
           return true;
        }
        if (method == MethodName.@ContinueFromTitle) {
           return true;
        }
        if (method == MethodName.@Sound) {
           return true;
        }
        if (method == MethodName.@OfferQueuedRating) {
           return true;
        }
        if (method == MethodName.@InspectEnemy) {
           return true;
        }
        if (method == MethodName.@DisableUnderlyingFocus) {
           return true;
        }
        if (method == MethodName.@FirstFocusable) {
           return true;
        }
        if (method == MethodName.@ShowTitle) {
           return true;
        }
        if (method == MethodName.@SharePair) {
           return true;
        }
        if (method == MethodName.@ShareRequestFinished) {
           return true;
        }
        if (method == MethodName.@BuildTitle) {
           return true;
        }
        if (method == MethodName.@InvokeAction) {
           return true;
        }
        if (method == MethodName.@UxResetHand) {
           return true;
        }
        if (method == MethodName.@UxText) {
           return true;
        }
        if (method == MethodName.@UxPanelBox) {
           return true;
        }
        if (method == MethodName.@BuildBattleHeader) {
           return true;
        }
        if (method == MethodName.@CreateHpReadout) {
           return true;
        }
        if (method == MethodName.@UxRich) {
           return true;
        }
        if (method == MethodName.@PositionUxEnemies) {
           return true;
        }
        if (method == MethodName.@RefreshUxReadouts) {
           return true;
        }
        if (method == MethodName.@SyncUxHand) {
           return true;
        }
        if (method == MethodName.@MakeUxDragPreview) {
           return true;
        }
        if (method == MethodName.@HoverUxHand) {
           return true;
        }
        if (method == MethodName.@RestoreUxHandOrder) {
           return true;
        }
        if (method == MethodName.@StartHandDrag) {
           return true;
        }
        if (method == MethodName.@EndHandDrag) {
           return true;
        }
        if (method == MethodName.@DecodeHandDrag) {
           return true;
        }
        if (method == MethodName.@UpdateHandAim) {
           return true;
        }
        if (method == MethodName.@UxProcess) {
           return true;
        }
        if (method == MethodName.@AlignUxRichLine) {
           return true;
        }
        if (method == MethodName.@UxRelayout) {
           return true;
        }
        return base.HasGodotClassMethod(method);
    }
}

}

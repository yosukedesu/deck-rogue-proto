using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using DeckRogue.GodotPort.Core;
using DeckRogue.GodotPort.Presentation;
using DeckRogue.Platform;
using Godot;
using Godot.Bridge;
using Godot.Collections;
using Godot.NativeInterop;
using Newtonsoft.Json.Linq;

namespace DeckRogue.GodotPort.UI;

[ScriptPath("res://scripts/ui/GameUI.cs")]
public partial class GameUI : Control
{
	private sealed record UxAdmittedHandExit(SourceAuthoredHandPlan Plan, CardTile Tile, Vector2 To, Vector2 CanvasSize, float Unit);

	private sealed class UxAuthoredHandExit
	{
		public UxAdmittedHandExit Admission;

		public Tween Tween;

		public Vector2 From;

		public Vector2 FromScale;

		public float StartAlpha;
	}

	private sealed class SourceGridState
	{
		public string SelectedUid;
		public int ScrollHorizontal;
		public int ScrollVertical;
		public ScrollContainer? ActiveScroll;
		public bool ScrollReady;
	}

	private sealed record EnemyHud(int Index, EnemyDropTarget Hit, PanelContainer Intent, PanelContainer Ledger);

	private sealed class UxHandDeal
	{
		public CardTile Tile;

		public long Epoch;

		public SourceHandDealClock Clock;

		public Vector2 StartPosition;

		public Vector2 CanvasSize;

		public float StartScale;

		public float StartDegrees;

		public float Unit;
	}

	private sealed class UxHandDragHoverHandoff
	{
		public UxHandDragPose Drag;

		public UxHandHoverPose Return;

		public Vector2 Pointer;

		public bool Returning;
	}

	private sealed class UxHandDragPose
	{
		public string Uid;

		public long Epoch;

		public CardTile Tile;

		public Control Parent;

		public bool StationaryAim;

		public MouseFilterEnum OriginalFilter;

		public Vector2 CanvasSize;

		public float Unit;
	}

	private sealed class UxHandExit
	{
		public CardTile Tile;

		public long Epoch;

		public SourceHandExitClock Clock;

		public Vector2 From;

		public Vector2 To;

		public Vector2 FromScale;

		public Vector2 CanvasSize;

		public float Unit;
	}

	private sealed class UxHandHoverPose
	{
		public CardTile Tile;

		public long Epoch;

		public SourceHandHoverClock Clock;

		public Vector2 From;

		public Vector2 To;

		public Vector2 FromScale;

		public Vector2 ToScale;

		public Vector2 CanvasSize;

		public float Unit;

		public bool DragReturn;
	}

	private sealed class SourceLedgerSeat
	{
		public SourceEnemyHudSnapshot Snapshot;

		public string Id;

		public Control Body;

		public Control Mask;

		public Control Forecast;

		public SourceHudLabel Name;

		public IReadOnlyList<SourceEnemyLedgerModel.Pill> Pills;

		public float Mark;

		public string LayoutKey;

		public ColorRect Marker;

		public ColorRect MarkerCore;
	}

	public new class MethodName : Control.MethodName
	{
		public static readonly StringName AdvancePlay = "AdvancePlay";

		public static readonly StringName SelectionTitle = "SelectionTitle";

		public static readonly StringName BeginEndTurn = "BeginEndTurn";

		public static readonly StringName FontSize = "FontSize";

		public new static readonly StringName _Ready = "_Ready";

		public static readonly StringName Bind = "Bind";

		public static readonly StringName PresentationCompleted = "PresentationCompleted";

		public new static readonly StringName _ExitTree = "_ExitTree";

		public static readonly StringName SelectEnemy = "SelectEnemy";

		public new static readonly StringName _Input = "_Input";

		public new static readonly StringName _UnhandledKeyInput = "_UnhandledKeyInput";

		public static readonly StringName Rebuild = "Rebuild";

		public static readonly StringName BuildHeader = "BuildHeader";

		public static readonly StringName Heading = "Heading";

		public static readonly StringName CloseOverlay = "CloseOverlay";

		public static readonly StringName CloseAllOverlays = "CloseAllOverlays";

		public static readonly StringName BuildOverlay = "BuildOverlay";

		public static readonly StringName OpenMenu = "OpenMenu";

		public static readonly StringName OpenKeywords = "OpenKeywords";

		public static readonly StringName OpenJournal = "OpenJournal";

		public static readonly StringName OpenExport = "OpenExport";

		public static readonly StringName OpenRetainedExport = "OpenRetainedExport";

		public static readonly StringName OpenExportPair = "OpenExportPair";

		public static readonly StringName CopyText = "CopyText";

		public static readonly StringName ExportFile = "ExportFile";

		public static readonly StringName ImportFile = "ImportFile";

		public static readonly StringName OpenImport = "OpenImport";

		public static readonly StringName ImportText = "ImportText";

		public new static readonly StringName _Notification = "_Notification";

		public static readonly StringName HandleBack = "HandleBack";

		public static readonly StringName ApplySafeArea = "ApplySafeArea";

		public static readonly StringName ReadSettings = "ReadSettings";

		public static readonly StringName ApplyAudio = "ApplyAudio";

		public static readonly StringName SaveSettings = "SaveSettings";

		public static readonly StringName OpenSettings = "OpenSettings";

		public static readonly StringName ApplyTheme = "ApplyTheme";

		public static readonly StringName Box = "Box";

		public static readonly StringName Plain = "Plain";

		public static readonly StringName SizeInteractiveControls = "SizeInteractiveControls";

		public static readonly StringName Row = "Row";

		public static readonly StringName Column = "Column";

		public static readonly StringName ScrollColumn = "ScrollColumn";

		public static readonly StringName Flow = "Flow";

		public static readonly StringName Art = "Art";

		public static readonly StringName Picture = "Picture";

		public static readonly StringName Tile = "Tile";

		public static readonly StringName CardColor = "CardColor";

		public static readonly StringName EnemyVitalsText = "EnemyVitalsText";

		public static readonly StringName EnemySurface = "EnemySurface";

		public new static readonly StringName _Process = "_Process";

		public static readonly StringName PositionEnemyHud = "PositionEnemyHud";

		public static readonly StringName OpenNote = "OpenNote";

		public static readonly StringName OpenRating = "OpenRating";

		public static readonly StringName Paints = "Paints";

		public static readonly StringName VisibleAlpha = "VisibleAlpha";

		public static readonly StringName ViewportBounds = "ViewportBounds";

		public new static readonly StringName Set = "Set";

		public static readonly StringName CanDropHandCard = "CanDropHandCard";

		public static readonly StringName DropHandCard = "DropHandCard";

		public static readonly StringName PlayAllSparks = "PlayAllSparks";

		public static readonly StringName PumpSparks = "PumpSparks";

		public static readonly StringName ContinueFromTitle = "ContinueFromTitle";

		public static readonly StringName Sound = "Sound";

		public static readonly StringName OfferQueuedRating = "OfferQueuedRating";

		public static readonly StringName InspectEnemy = "InspectEnemy";

		public static readonly StringName DisableUnderlyingFocus = "DisableUnderlyingFocus";

		public static readonly StringName FirstFocusable = "FirstFocusable";

		public static readonly StringName ShowTitle = "ShowTitle";

		public static readonly StringName SharePair = "SharePair";

		public static readonly StringName ShareRequestFinished = "ShareRequestFinished";

		public static readonly StringName BuildTitle = "BuildTitle";

		public static readonly StringName InvokeAction = "InvokeAction";

		public static readonly StringName UxResetHand = "UxResetHand";

		public static readonly StringName UxText = "UxText";

		public static readonly StringName UxPanelBox = "UxPanelBox";

		public static readonly StringName BuildBattleHeader = "BuildBattleHeader";

		public static readonly StringName CreateHpReadout = "CreateHpReadout";

		public static readonly StringName UxRich = "UxRich";

		public static readonly StringName PositionUxEnemies = "PositionUxEnemies";

		public static readonly StringName RefreshUxReadouts = "RefreshUxReadouts";

		public static readonly StringName SyncUxHand = "SyncUxHand";

		public static readonly StringName MakeUxDragPreview = "MakeUxDragPreview";

		public static readonly StringName HoverUxHand = "HoverUxHand";

		public static readonly StringName RestoreUxHandOrder = "RestoreUxHandOrder";

		public static readonly StringName StartHandDrag = "StartHandDrag";

		public static readonly StringName EndHandDrag = "EndHandDrag";

		public static readonly StringName DecodeHandDrag = "DecodeHandDrag";

		public static readonly StringName UpdateHandAim = "UpdateHandAim";

		public static readonly StringName UxProcess = "UxProcess";

		public static readonly StringName AlignUxRichLine = "AlignUxRichLine";

		public static readonly StringName UxRelayout = "UxRelayout";
	}

	public new class PropertyName : Control.PropertyName
	{
		public static readonly StringName GraphicsQuality = "GraphicsQuality";

		public static readonly StringName PhoneLayout = "PhoneLayout";

		public static readonly StringName MinimumHit = "MinimumHit";

		public static readonly StringName IsInputBlocked = "IsInputBlocked";

		public static readonly StringName CurrentRoute = "CurrentRoute";

		public static readonly StringName ActiveModal = "ActiveModal";

		public static readonly StringName RejectedReceiptCount = "RejectedReceiptCount";

		public static readonly StringName PendingSparkPlays = "PendingSparkPlays";

		public static readonly StringName PresentedNumericContactCount = "PresentedNumericContactCount";

		public static readonly StringName DisplayedIncomingRemaining = "DisplayedIncomingRemaining";

		public static readonly StringName DisplayedIncomingLanded = "DisplayedIncomingLanded";

		public static readonly StringName DisplayedIncomingHeadline = "DisplayedIncomingHeadline";

		public static readonly StringName AimActive = "AimActive";

		public static readonly StringName BattleUnit = "BattleUnit";

		public static readonly StringName UxCardScale = "UxCardScale";

		public static readonly StringName _session = "_session";

		public static readonly StringName _body = "_body";

		public static readonly StringName _shell = "_shell";

		public static readonly StringName _overlayNode = "_overlayNode";

		public static readonly StringName _overlayTitle = "_overlayTitle";

		public static readonly StringName _overlayCanClose = "_overlayCanClose";

		public static readonly StringName _showTitle = "_showTitle";

		public static readonly StringName _notice = "_notice";

		public static readonly StringName _error = "_error";

		public static readonly StringName _leaderId = "_leaderId";

		public static readonly StringName _deckId = "_deckId";

		public static readonly StringName _seed = "_seed";

		public static readonly StringName _difficulty = "_difficulty";

		public static readonly StringName _preferredTarget = "_preferredTarget";

		public static readonly StringName _workshopA = "_workshopA";

		public static readonly StringName _workshopB = "_workshopB";

		public static readonly StringName _textScale = "_textScale";

		public static readonly StringName _masterVolume = "_masterVolume";

		public static readonly StringName _bgmVolume = "_bgmVolume";

		public static readonly StringName _sfxVolume = "_sfxVolume";

		public static readonly StringName _muted = "_muted";

		public static readonly StringName _graphicsQuality = "_graphicsQuality";

		public static readonly StringName _safeMargin = "_safeMargin";

		public static readonly StringName _handScroll = "_handScroll";

		public static readonly StringName _noteDraft = "_noteDraft";

		public static readonly StringName _hpReadout = "_hpReadout";

		public static readonly StringName _energyReadout = "_energyReadout";

		public static readonly StringName _blockReadout = "_blockReadout";

		public static readonly StringName _statusReadout = "_statusReadout";

		public static readonly StringName _costPresented = "_costPresented";

		public static readonly StringName _sparksRemaining = "_sparksRemaining";

		public static readonly StringName _pumpingSparks = "_pumpingSparks";

		public static readonly StringName _ratingQueued = "_ratingQueued";

		public static readonly StringName _shareAwaiting = "_shareAwaiting";

		public static readonly StringName _reactionMode = "_reactionMode";

		public static readonly StringName _setAnyCards = "_setAnyCards";

		public static readonly StringName _uxBattle = "_uxBattle";

		public static readonly StringName _uxHand = "_uxHand";

		public static readonly StringName _uxAim = "_uxAim";

		public static readonly StringName _uxHpBar = "_uxHpBar";

		public static readonly StringName _uxOrb = "_uxOrb";

		public static readonly StringName _uxForecast = "_uxForecast";

		public static readonly StringName _uxDetail = "_uxDetail";

		public static readonly StringName _uxPhase = "_uxPhase";

		public static readonly StringName _uxHover = "_uxHover";

		public static readonly StringName _uxDrag = "_uxDrag";

		public static readonly StringName _uxEpoch = "_uxEpoch";

		public static readonly StringName _uxLastSize = "_uxLastSize";

		public static readonly StringName _uxRelayoutQueued = "_uxRelayoutQueued";

		public static readonly StringName _uxIncoming = "_uxIncoming";

		public static readonly StringName _uxLanded = "_uxLanded";

		public static readonly StringName _uxSkipFirst = "_uxSkipFirst";

		public static readonly StringName _uxIncomingOwner = "_uxIncomingOwner";
	}

	public new class SignalName : Control.SignalName
	{
	}

	private Func<SessionChange, string, SourceAuthoredHandStyle?> _uxAuthoredHandStyle;

	private readonly System.Collections.Generic.Dictionary<string, UxAdmittedHandExit> _uxAdmittedHandExits = new System.Collections.Generic.Dictionary<string, UxAdmittedHandExit>();

	private readonly System.Collections.Generic.Dictionary<string, UxAuthoredHandExit> _uxAuthoredHandExits = new System.Collections.Generic.Dictionary<string, UxAuthoredHandExit>();

	private bool _showGridUpgraded;

	private readonly ConditionalWeakTable<object, System.Collections.Generic.Dictionary<string, SourceGridState>> _sourceGridStates = new ConditionalWeakTable<object, System.Collections.Generic.Dictionary<string, SourceGridState>>();

	private readonly HashSet<string> _scryPicks = new HashSet<string>();

	private GameSession? _session;

	private RunState? _view;

	private Func<bool>? _presentationGate;

	private VBoxContainer _body;

	private VBoxContainer _shell;

	private Control? _overlayNode;

	private Action<VBoxContainer>? _overlay;

	private readonly Stack<(string Title, Action<VBoxContainer> Body, bool Close)> _overlayStack = new Stack<(string, Action<VBoxContainer>, bool)>();

	private string _overlayTitle = "";

	private bool _overlayCanClose = true;

	private bool _showTitle = true;

	private string _notice = "";

	private bool _error;

	private string _leaderId = "leader_green";

	private string? _deckId;

	private int _seed = 9101;

	private int _difficulty = 3;

	private int _preferredTarget = -1;

	private PlaySelection? _play;

	private int _workshopA = -1;

	private int _workshopB = -1;

	private readonly HashSet<int> _relicPicks = new HashSet<int>();

	private float _textScale = 1f;

	private float _masterVolume = 0.8f;

	private float _bgmVolume = 0.3f;

	private float _sfxVolume = 0.7f;

	private bool _muted;

	private string _graphicsQuality = "auto";

	private MarginContainer? _safeMargin;

	private readonly System.Collections.Generic.Dictionary<string, Texture2D?> _textures = new System.Collections.Generic.Dictionary<string, Texture2D>();

	private static readonly Color Ink = SourcePalette.Paper;

	private static readonly Color Muted = SourcePalette.PaperDim;

	private static readonly Color Gold = SourcePalette.BrassLight;

	private static readonly Color Panel = SourcePalette.Window;

	private static readonly Color Border = SourcePalette.NightEdge;

	private static readonly Color Accent = SourcePalette.ManaLight;

	private static readonly Color Danger = new Color("f2a193");

	private Func<int, (Vector2 Head, Vector2 Feet)?>? _enemyProjection;
	private Func<int, Rect2?>? _enemyFootprintProjection;
	public void SetEnemyFootprintProjection(Func<int, Rect2?> project) => _enemyFootprintProjection = project;

	private ScrollContainer? _handScroll;

	private readonly List<EnemyHud> _enemyHud = new List<EnemyHud>();

	private string _noteDraft = "";

	private Label? _hpReadout;

	private Label? _energyReadout;

	private Label? _blockReadout;

	private Label? _statusReadout;

	private readonly System.Collections.Generic.Dictionary<int, Label> _enemyReadouts = new System.Collections.Generic.Dictionary<int, Label>();

	private SessionChange? _activeChange;

	private bool _costPresented;

	private Func<GameEvent, CombatPresentationReceipt, GameState?, bool>? _receiptValidator;

	private Func<GameEvent, CombatPresentationVitals?>? _presentedVitals;

	private Func<GameState?>? _presentedState;

	private readonly System.Collections.Generic.Dictionary<int, Label> _enemyIntentReadouts = new System.Collections.Generic.Dictionary<int, Label>();

	private Func<Vector2, int?>? _enemyResolver;

	private int _sparksRemaining;

	private bool _pumpingSparks;

	private bool _ratingQueued;

	private bool _shareAwaiting;

	private string _reactionMode = "set-confirm";

	private bool _setAnyCards;

	private readonly System.Collections.Generic.Dictionary<string, UxHandDeal> _uxDeals = new System.Collections.Generic.Dictionary<string, UxHandDeal>();

	private UxHandDragHoverHandoff _uxHandDragHoverHandoff;

	private UxHandDragPose _uxHandDragPose;

	private readonly System.Collections.Generic.Dictionary<string, UxHandExit> _uxHandExits = new System.Collections.Generic.Dictionary<string, UxHandExit>();

	private readonly System.Collections.Generic.Dictionary<string, UxHandHoverPose> _uxHandHoverPoses = new System.Collections.Generic.Dictionary<string, UxHandHoverPose>();

	private Action<bool> _setPresentationDiagnostics;

	private Func<bool> _presentationDiagnosticsEnabled;

	private Func<string> _presentationDiagnosticsLastFile;

	private string _presentationDiagnosticStatus = "描画診断: オフ";

	private Control _uxBattle;

	private Control _uxHand;

	private SourceAimArrow _uxAim;

	private readonly System.Collections.Generic.Dictionary<string, CardTile> _uxTiles = new System.Collections.Generic.Dictionary<string, CardTile>();

	private readonly System.Collections.Generic.Dictionary<string, (Vector2 Pos, float Rotation, int Order)> _uxSeats = new System.Collections.Generic.Dictionary<string, (Vector2, float, int)>();

	private readonly System.Collections.Generic.Dictionary<(int Index, string Role), EnemyDropTarget> _uxEnemyLabelTargets = new();
	private readonly List<(int Index, EnemyDropTarget Hit, PanelContainer Intent, PanelContainer Ledger, SourceHealthBar Bar, SourceHudLabel Hp, Label Title)> _uxEnemies = new List<(int, EnemyDropTarget, PanelContainer, PanelContainer, SourceHealthBar, SourceHudLabel, Label)>();

	private SourceHealthBar _uxHpBar;

	private SourceEnergyOrb _uxOrb;

	private RichTextLabel _uxForecast;

	private RichTextLabel _uxDetail;

	private Label _uxPhase;

	private string _uxHover;

	private string _uxDrag;

	private long _uxEpoch;

	private Vector2 _uxLastSize;

	private bool _uxRelayoutQueued;

	private Func<Vector2?> _uxLeaderProjection;

	private Func<GameEvent, SessionChange> _uxEventOwner;

	private Func<GameEvent, bool> _uxOrdinary;

	private Func<GameEvent, int?> _uxIncomingContact;

	private readonly HashSet<long> _uxPaid = new HashSet<long>();

	private int _uxIncoming;

	private bool _uxLanded;

	private bool _uxSkipFirst;

	private long _uxIncomingOwner = -1L;

	private Func<string, double> _uxRetirement;

	private const int UxEnemyHeadCacheCapacity = 512;

	private readonly System.Collections.Generic.Dictionary<(ulong Texture, bool Phone), float> _uxHeadHeightCache = new System.Collections.Generic.Dictionary<(ulong, bool), float>();

	private readonly Queue<(ulong Texture, bool Phone)> _uxHeadHeightOrder = new Queue<(ulong, bool)>();

	private long _uxHeadEvictions;

	private long _uxHeadReadbacks;

	private long _uxHeadCacheHits;

	private bool _shopGearsScreen;

	private Func<string> _effectiveQuality;

	private Vector2I _windowResolution = new Vector2I(1280, 720);

	private string _savedSummaryKey;

	private RunState _savedSummaryRun;

	private string _savedSummaryPath;

	private bool _savedSummaryBackup;

	private SourceEnemyHudSnapshot _uxEnemyHudSnapshot;

	private long _uxEnemyHudBoardRevision;

	private float _uxLedgerHeightWidth = -1f;

	private float _uxLedgerHeightScale = -1f;

	private float _uxLedgerHeight;

	private bool _uxLedgerHeightPhone;

	private readonly System.Collections.Generic.Dictionary<int, SourceLedgerSeat> _uxLedgerSeats = new System.Collections.Generic.Dictionary<int, SourceLedgerSeat>();

	private Control? _sourceTokenBand;

	private SourceTokenFrame? _sourceTokenFrame;

	private Func<long>? _sourceTokenActiveRevision;

	private SourceTokenData? _sourceTokenData;

	private string _sourceTokenRenderedStamp = "";

	public int PendingAuthoredHandExitCount => _uxAdmittedHandExits.Count;

	public int ActiveAuthoredHandExitCount => _uxAuthoredHandExits.Count;

	public long AuthoredHandExitStartCount { get; private set; }

	public long AuthoredHandExitCompleteCount { get; private set; }

	public long LastAuthoredHandExitRevision { get; private set; } = -1L;

	public string LastAuthoredHandExitUid { get; private set; } = "";

	public double LastAuthoredHandRetirementDuration { get; private set; }

	private GameState SourceInspectState
	{
		get
		{
			if (!(CurrentRoute == "combat"))
			{
				return null;
			}
			return InputCombat;
		}
	}

	public (float Unit, float TextScale) SourceCombatTextMetrics => (Unit: BattleUnit, TextScale: _textScale);

	public string GraphicsQuality => _graphicsQuality;

	private bool PhoneLayout
	{
		get
		{
			if (!OS.HasFeature("mobile"))
			{
				return Enumerable.Contains(OS.GetCmdlineUserArgs(), "--ui-phone");
			}
			return true;
		}
	}

	public UiSizing Sizing => new UiSizing(PhoneLayout, (Size.Y > 0f) ? Size.Y : 900f, DisplayServer.WindowGetSize().Y, DisplayServer.ScreenGetDpi());

	private float MinimumHit => Sizing.MinimumHit;

	public bool IsInputBlocked => _faultUiUnavailable || (!_synchronizingFaultSnapshot && (_presentationGate?.Invoke() ?? false));

	public string CurrentRoute
	{
		get
		{
			if (!_showTitle)
			{
				return FlowRules.Route(_view);
			}
			return "title";
		}
	}

	private static bool ClipboardAvailable => DisplayServer.HasFeature(DisplayServer.Feature.Clipboard);

	private static bool NativeFilesAvailable => DisplayServer.HasFeature(DisplayServer.Feature.NativeDialogFile);

	public IReadOnlyList<Control> EnemyHudSurfaces => _uxEnemies.Where(((int Index, EnemyDropTarget Hit, PanelContainer Intent, PanelContainer Ledger, SourceHealthBar Bar, SourceHudLabel Hp, Label Title) h) => GodotObject.IsInstanceValid(h.Intent) && h.Intent.IsVisibleInTree()).SelectMany(((int Index, EnemyDropTarget Hit, PanelContainer Intent, PanelContainer Ledger, SourceHealthBar Bar, SourceHudLabel Hp, Label Title) h) => new Control[2] { h.Intent, h.Ledger }).ToArray();

	public GameState? DisplayedCombat => _view?.Combat;

	public string? ActiveModal
	{
		get
		{
			if (_overlay == null)
			{
				return null;
			}
			return _overlayTitle;
		}
	}

	public int RejectedReceiptCount { get; private set; }

	public IReadOnlyDictionary<int, string> DisplayedIntents => _enemyIntentReadouts.Where((KeyValuePair<int, Label> p) => GodotObject.IsInstanceValid(p.Value)).ToDictionary((KeyValuePair<int, Label> p) => p.Key, (KeyValuePair<int, Label> p) => p.Value.Text);

	public int PendingSparkPlays => _sparksRemaining;

	public int PresentedNumericContactCount { get; private set; }

	public int ActiveHandDealCount => _uxDeals.Count;

	public long HandDealStartCueCount { get; private set; }

	public int ActiveHandExitCount => _uxHandExits.Count;

	public long HandExitStartCount { get; private set; }

	public int ActiveHandHoverCount => _uxHandHoverPoses.Count;

	public int ActiveHandReturnCount => _uxHandHoverPoses.Values.Count((UxHandHoverPose p) => p.DragReturn);

	public IReadOnlyDictionary<string, CardTile> LiveHandTiles => _uxTiles;

	public int DisplayedIncomingRemaining => _uxIncoming;

	public bool DisplayedIncomingLanded => _uxLanded;

	public string DisplayedIncomingHeadline => _uxForecast?.GetParsedText() ?? "";

	public bool AimActive => _uxAim?.Active ?? false;

	private float BattleUnit => Size.Y / (PhoneLayout ? 675f : 1080f);

	private float UxCardWidthScale => 200f / SourceCardFace.W;

	private float UxCardScale
	{
		get
		{
			if (!PhoneLayout)
			{
				return 0.92f * UxCardWidthScale;
			}
			return UxCardWidthScale;
		}
	}

	private GameState InputCombat
	{
		get
		{
			if (IsInputBlocked)
			{
				return _view?.Combat;
			}
			return _session?.CurrentRun?.Combat;
		}
	}

	public int EnemyHeadCacheEntries => _uxHeadHeightCache.Count;

	public long EnemyHeadCacheEvictions => _uxHeadEvictions;

	public long EnemyHeadReadbacks => _uxHeadReadbacks;

	public long EnemyHeadCacheHits => _uxHeadCacheHits;

	private static bool DesktopVideoAvailable
	{
		get
		{
			if (!OS.HasFeature("mobile"))
			{
				return DisplayServer.GetName() != "headless";
			}
			return false;
		}
	}

	private static bool IsFullscreenMode
	{
		get
		{
			DisplayServer.WindowMode windowMode = DisplayServer.WindowGetMode();
			if ((ulong)(windowMode - 3) <= 1uL)
			{
				return true;
			}
			return false;
		}
	}

	public string SourceTokenAdaptation { get; private set; } = "unbuilt";

	public long SourceTokenBoardRevision => _sourceTokenFrame?.Revision ?? (-1);

	public SourceTokenFrame? CapturedSourceTokenFrame => _sourceTokenFrame;

	public IReadOnlyList<Rect2> SourceTokenControlBounds
	{
		get
		{
			if (_sourceTokenBand != null && GodotObject.IsInstanceValid(_sourceTokenBand))
			{
				return (from b in _sourceTokenBand.GetChildren().OfType<Button>()
					select b.GetGlobalRect()).ToArray();
			}
			return System.Array.Empty<Rect2>();
		}
	}

	public event Action<string>? QualityChanged;

	public event Action<string>? ScreenChanged;

	public event Action<string>? UiSoundRequested;

	public void SetAdmittedAuthoredHandStyle(Func<SessionChange, string, SourceAuthoredHandStyle?> provider)
	{
		_uxAuthoredHandStyle = provider;
	}

	private void UxCaptureAdmittedHandExit(SessionChange change)
	{
		if (change.IsReset)
		{
			UxCancelAuthoredHandExits();
			return;
		}
		KeyValuePair<string, UxAdmittedHandExit>[] array = _uxAdmittedHandExits.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			KeyValuePair<string, UxAdmittedHandExit> pair = array[i];
			if (pair.Value.Plan.Epoch == _uxEpoch && _uxTiles.TryGetValue(pair.Key, out var live) && live == pair.Value.Tile)
			{
				GameState? combat = change.Current.Combat;
				if ((object)combat == null || !combat.Player.Hand.Any((CardInstance c) => c.Uid == pair.Key))
				{
					continue;
				}
			}
			_uxAdmittedHandExits.Remove(pair.Key);
		}
		if (!(change.Command is RunCommand_Combat { Command: Command_PlayCard play }))
		{
			return;
		}
		GameState before = change.Previous?.Combat;
		if ((object)before == null)
		{
			return;
		}
		GameState after = change.Current.Combat;
		if ((object)after == null)
		{
			return;
		}
		string uid = play.CardUid;
		CardInstance card = before.Player.Hand.FirstOrDefault((CardInstance c) => c.Uid == uid);
		if (card == null || after.Player.Hand.Any((CardInstance c) => c.Uid == uid) || after.Player.SetCards.Any((CardInstance c) => c.Uid == uid) || after.Player.Permanents.Any((CardInstance c) => c.Uid == uid) || !_uxTiles.TryGetValue(uid, out var tile) || !GodotObject.IsInstanceValid(tile) || tile.IsQueuedForDeletion() || tile.GetParent() != _uxHand || tile.GetMeta("definition_id", "").AsString() != card.Def.Id)
		{
			return;
		}
		SourceAuthoredHandStyle? proof = _uxAuthoredHandStyle?.Invoke(change, uid);
		if (!proof.HasValue)
		{
			return;
		}
		SourceAuthoredHandStyle style = proof.GetValueOrDefault();
		if (!style.Matches(change.Revision, uid, card.Def.Id))
		{
			return;
		}
		bool exhaust = after.Player.ExhaustPile.Any((CardInstance c) => c.Uid == uid);
		Vector2 to = tile.Position;
		if (!exhaust)
		{
			if (_uxHand == null || !GodotObject.IsInstanceValid(_uxHand) || _uxBattle == null || !GodotObject.IsInstanceValid(_uxBattle) || _uxBattle.IsQueuedForDeletion())
			{
				return;
			}
			Control pile = _uxBattle.GetNodeOrNull<Control>("DiscardPile");
			if (pile == null || !GodotObject.IsInstanceValid(pile) || pile.IsQueuedForDeletion())
			{
				return;
			}
			to = _uxHand.GetGlobalTransformWithCanvas().AffineInverse() * (pile.GetGlobalTransformWithCanvas() * (pile.Size / 2f)) - tile.PivotOffset;
		}
		SourceAuthoredHandPlan plan = new SourceAuthoredHandPlan(_uxEpoch, change.Revision, uid, card.Def.Id, style, _uxRetirement?.Invoke(card.Def.Id) ?? 0.25, exhaust, play.TargetIndex);
		if (plan.Valid)
		{
			_uxAdmittedHandExits[uid] = new UxAdmittedHandExit(plan, tile, to, Size, BattleUnit);
		}
	}

	private bool UxAuthoredHandExitIsCurrent(string uid, UxAuthoredHandExit motion)
	{
		UxAdmittedHandExit a = motion.Admission;
		if (a.Plan.Epoch == _uxEpoch && a.Plan.CardUid == uid && a.Plan.Valid && _uxAuthoredHandExits.TryGetValue(uid, out var current) && current == motion && !_uxTiles.ContainsKey(uid) && GodotObject.IsInstanceValid(a.Tile) && !a.Tile.IsQueuedForDeletion() && a.Tile.GetParent() == _uxHand && CurrentRoute == "combat" && _overlay == null && _uxHand != null && GodotObject.IsInstanceValid(_uxHand) && _uxHand.IsVisibleInTree() && a.CanvasSize == Size)
		{
			return Math.Abs(a.Unit - BattleUnit) < 0.0001f;
		}
		return false;
	}

	private bool UxTryStartAuthoredHandExit(string uid, CardTile tile)
	{
		if (!_uxAdmittedHandExits.Remove(uid, out var a))
		{
			return false;
		}
		if (a.Plan.Epoch != _uxEpoch || a.Plan.CardUid != uid || !a.Plan.Valid || a.Tile != tile || a.CanvasSize != Size || Math.Abs(a.Unit - BattleUnit) > 0.0001f || !GodotObject.IsInstanceValid(tile) || tile.IsQueuedForDeletion() || tile.GetParent() != _uxHand || CurrentRoute != "combat" || _overlay != null)
		{
			return false;
		}
		UxCancelAuthoredHandExit(uid);
		tile.MouseFilter = MouseFilterEnum.Ignore;
		tile.Disabled = true;
		tile.AllowDrag = false;
		tile.SetProcess(enable: false);
		tile.ZIndex = 0;
		UxAuthoredHandExit motion = new UxAuthoredHandExit
		{
			Admission = a,
			From = tile.Position,
			FromScale = tile.Scale,
			StartAlpha = tile.Modulate.A
		};
		_uxAuthoredHandExits[uid] = motion;
		_uxHand.MoveChild(tile, _uxHand.GetChildCount() - 1);
		Tween tween = CreateTween();
		motion.Tween = tween;
		tween.TweenMethod(Callable.From((float k) =>
		{
			UxSampleAuthoredHandExit(uid, motion, k);
		}), 0f, 1f, a.Plan.Duration);
		tween.TweenCallback(Callable.From(() =>
		{
			UxCompleteAuthoredHandExit(uid, motion);
		}));
		AuthoredHandExitStartCount++;
		LastAuthoredHandExitRevision = a.Plan.Revision;
		LastAuthoredHandExitUid = uid;
		LastAuthoredHandRetirementDuration = a.Plan.Duration;
		return true;
	}

	private void UxSampleAuthoredHandExit(string uid, UxAuthoredHandExit motion, float normalized)
	{
		if (!UxAuthoredHandExitIsCurrent(uid, motion))
		{
			UxCancelAuthoredHandExit(uid, motion);
			return;
		}
		UxAdmittedHandExit a = motion.Admission;
		SourceAuthoredHandMotion.Pose pose = SourceAuthoredHandMotion.Sample(normalized, a.Plan.Exhaust, motion.StartAlpha);
		a.Tile.Position = motion.From.Lerp(a.To, pose.PositionFraction);
		a.Tile.Scale = motion.FromScale.Lerp(Vector2.One * (pose.EndScale * a.Unit), pose.ScaleFraction);
		Color color = a.Tile.Modulate;
		color.A = pose.Alpha;
		a.Tile.Modulate = color;
	}

	private void UxCompleteAuthoredHandExit(string uid, UxAuthoredHandExit motion)
	{
		if (!UxAuthoredHandExitIsCurrent(uid, motion))
		{
			UxCancelAuthoredHandExit(uid, motion);
			return;
		}
		_uxAuthoredHandExits.Remove(uid);
		AuthoredHandExitCompleteCount++;
		if (GodotObject.IsInstanceValid(motion.Admission.Tile))
		{
			motion.Admission.Tile.QueueFree();
		}
	}

	private void UxCancelAuthoredHandExit(string uid, UxAuthoredHandExit expected = null)
	{
		if (expected == null)
		{
			_uxAdmittedHandExits.Remove(uid);
		}
		if (_uxAuthoredHandExits.TryGetValue(uid, out var motion) && (expected == null || expected == motion))
		{
			_uxAuthoredHandExits.Remove(uid);
			if (motion.Tween != null && GodotObject.IsInstanceValid(motion.Tween) && motion.Tween.IsValid())
			{
				motion.Tween.Kill();
			}
			CardTile tile = motion.Admission.Tile;
			if (GodotObject.IsInstanceValid(tile) && !tile.IsQueuedForDeletion() && (!_uxTiles.TryGetValue(uid, out var live) || live != tile))
			{
				tile.Hide();
				tile.QueueFree();
			}
		}
	}

	private void UxCancelAuthoredHandExits()
	{
		_uxAdmittedHandExits.Clear();
		string[] array = _uxAuthoredHandExits.Keys.ToArray();
		foreach (string uid in array)
		{
			UxCancelAuthoredHandExit(uid);
		}
	}

	private void UxCheckAuthoredHandExits()
	{
		bool visible = CurrentRoute == "combat" && _overlay == null && _uxHand != null && GodotObject.IsInstanceValid(_uxHand) && _uxHand.IsVisibleInTree();
		KeyValuePair<string, UxAdmittedHandExit>[] array = _uxAdmittedHandExits.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			KeyValuePair<string, UxAdmittedHandExit> pair = array[i];
			UxAdmittedHandExit a = pair.Value;
			if (!visible || a.Plan.Epoch != _uxEpoch || a.CanvasSize != Size || Math.Abs(a.Unit - BattleUnit) > 0.0001f || !_uxTiles.TryGetValue(pair.Key, out var live) || live != a.Tile || !GodotObject.IsInstanceValid(a.Tile) || a.Tile.IsQueuedForDeletion())
			{
				_uxAdmittedHandExits.Remove(pair.Key);
			}
		}
		KeyValuePair<string, UxAuthoredHandExit>[] array2 = _uxAuthoredHandExits.ToArray();
		for (int i = 0; i < array2.Length; i++)
		{
			KeyValuePair<string, UxAuthoredHandExit> pair2 = array2[i];
			if (!UxAuthoredHandExitIsCurrent(pair2.Key, pair2.Value))
			{
				UxCancelAuthoredHandExit(pair2.Key, pair2.Value);
			}
		}
	}

	private void UxRestoreAuthoredHandExitOrder()
	{
		foreach (KeyValuePair<string, UxAuthoredHandExit> pair in _uxAuthoredHandExits)
		{
			if (UxAuthoredHandExitIsCurrent(pair.Key, pair.Value))
			{
				_uxHand.MoveChild(pair.Value.Admission.Tile, _uxHand.GetChildCount() - 1);
			}
		}
	}

	private CardTile SourceCard(Node parent, CardInstance original, CardInstance shown, Action action, bool enabled, string badge, float scale, GameState state, bool drag = false, int? cost = null, bool selected = false, string corner = "")
	{
		Control holder = new Control
		{
			Name = "SourceCardCell",
			CustomMinimumSize = new Vector2(SourceCardFace.W * scale + 16f, SourceCardFace.H * scale + 16f),
			SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
			SizeFlagsVertical = SizeFlags.ShrinkBegin,
			MouseFilter = MouseFilterEnum.Ignore
		};
		parent.AddChild(holder, forceReadableName: false, InternalMode.Disabled);
		CardTile tile = new CardTile
		{
			Name = "SourceGridCard",
			CardUid = original.Uid,
			Position = new Vector2(8f, 8f),
			Size = new Vector2(SourceCardFace.W, SourceCardFace.H),
			Scale = Vector2.One * scale,
			AllowDrag = ((drag & enabled) && !IsInputBlocked),
			Disabled = false
		};
		holder.AddChild(tile, forceReadableName: false, InternalMode.Disabled);
		SourceCardFace.Fill(tile, shown, state, _textScale, PhoneLayout, -1, cost, badge, enabled);
		tile.SetMeta("source_original_uid", original.Uid);
		tile.SetMeta("source_grid_selected", selected);
		tile.SetMeta("source_upgraded_preview", (object)shown != original);
		tile.Inspect = () =>
		{
			if (!IsInputBlocked && tile.IsVisibleInTree())
			{
				InspectCard(original);
			}
		};
		tile.Pressed += () =>
		{
			if ((!IsInputBlocked && tile.IsVisibleInTree() && !tile.ConsumeInspection()) & enabled)
			{
				Sound("ui.click");
				action?.Invoke();
			}
		};
		if (selected || corner == "★")
		{
			Panel ring = new Panel
			{
				Name = "SelectedCardBorder",
				MouseFilter = MouseFilterEnum.Ignore,
				Position = new Vector2(3f, 3f),
				Size = holder.CustomMinimumSize - new Vector2(6f, 6f)
			};
			ring.AddThemeStyleboxOverride("panel", Box(new Color(0f, 0f, 0f, 0f), Gold, (!selected) ? 1 : 3));
			holder.AddChild(ring, forceReadableName: false, InternalMode.Disabled);
		}
		if (!string.IsNullOrEmpty(corner))
		{
			PanelContainer mark = new PanelContainer
			{
				Name = "CardCornerMark",
				MouseFilter = MouseFilterEnum.Ignore,
				Position = new Vector2(holder.CustomMinimumSize.X - 38f, 0f),
				CustomMinimumSize = new Vector2(36f, 36f)
			};
			mark.AddThemeStyleboxOverride("panel", Box(Gold, SourcePalette.BrassInk));
			holder.AddChild(mark, forceReadableName: false, InternalMode.Disabled);
			Text(mark, corner, 19, SourcePalette.Ink).HorizontalAlignment = HorizontalAlignment.Center;
		}
		return tile;
	}

    private void SourceInspectCard(CardInstance original)
    {
        bool upgraded = false;
        Open(original.Def.Name, box =>
        {
            box.SetMeta("modal_kind", "card-detail");
            CardInstance shown = SourceCardModalPolicy.Preview(original, upgraded);
            GameState state = SourceInspectState;
            var row = Row(box);
            row.SizeFlagsVertical = SizeFlags.ExpandFill;
            row.AddThemeConstantOverride("separation", 28);
            var visual = Column(row);
            visual.SizeFlagsHorizontal = SizeFlags.Fill;
            visual.CustomMinimumSize = new Vector2(300 * RunUnit, 0);
            var card = SourceCard(visual, original, shown, null, true, null, PhoneLayout ? 1.46f : 1.45f, state);
            card.Inspect = null;
            var reading = Column(row);
            reading.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            var detail = ScrollColumn(reading);
            detail.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            SectionHeading(detail, upgraded ? "鍛えた後の効果" : "カードの効果", 26, SourcePalette.Ink);
            var effect = Text(detail, SourceCardFace.BodyText(shown, state), 24, SourcePalette.Ink);
            effect.AddThemeConstantOverride("line_spacing", 8);
            var terms = KeywordHelp.FindIn(SourceCardFace.BodyText(shown, state)).ToArray();
            if (terms.Length > 0) {
                SectionHeading(detail, "用語の説明", 21, SourcePalette.BrassInk);
                foreach (string term in terms) {
                    Text(detail, term, 20, SourcePalette.Ink);
                    Text(detail, KeywordHelp.Terms[term], 19, SourcePalette.InkSoft).AddThemeConstantOverride("line_spacing", 5);
                }
            }
            if (Upgrade.CanUpgradeCard(original)) {
                var preview = RunButton(reading, upgraded ? "元の札と比較" : "鍛えた後と比較", () => { upgraded = !upgraded; Rebuild(); });
                preview.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            } else Text(reading, SourceCardModalPolicy.UpgradeState(original), 18, SourcePalette.InkSoft);
            AddInspectionShortcuts(reading, original);
        });
    }

	private void BuildBattleLegacy(RunState run)
	{
		GameState state = run.Combat;
		if (state == null)
		{
			return;
		}
		PlayerState player = state.Player;
		_enemyReadouts.Clear();
		_enemyIntentReadouts.Clear();
		_enemyHud.Clear();
		_handScroll = null;
		VBoxContainer phoneSelf = ((PhoneLayout && _enemyProjection != null) ? Column(_body) : null);
		if (phoneSelf != null)
		{
			phoneSelf.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
			phoneSelf.CustomMinimumSize = new Vector2(Size.X * 0.43f, 0f);
			phoneSelf.AddThemeConstantOverride("separation", 2);
		}
		HBoxContainer status = Row(phoneSelf ?? _body);
		HBoxContainer statusActions = ((phoneSelf != null) ? Row(phoneSelf) : status);
		Text(status, $"ターン {state.Turn}", 18, Gold);
		_blockReadout = Text(status, $"ブロック {player.Block}  氷壁 {player.IceBlock}", 18);
		_statusReadout = Text(status, PlayerStatus(state), 15, Muted);
		Button(statusActions, "状態・能力", () =>
		{
			Open("状態・能力", (VBoxContainer box) =>
			{
				Text(box, PlayerStatus(state), 23);
				Text(box, "置物・人形", 22, Gold);
				CardGrid(box, player.Permanents, InspectCard, null, (CardInstance p) => (!Effects.IsDoll(p)) ? "常在" : ("残り " + (Effects.DollLifeLeft(state, p)?.ToString() ?? "∞") + " ターン"));
			});
		});
		Button(statusActions, $"からくり {player.SetCards.Count}/{player.SetSlots}", () =>
		{
			Open("仕込んでいるからくり", (VBoxContainer box) =>
			{
				CardGrid(box, player.SetCards, InspectCard, null, (CardInstance card3) => Effects.TrapStatusTextKarakuri(state, card3));
			});
		});
		Button(statusActions, $"ギア {run.Gears?.Count ?? 0}", () =>
		{
			Open("ギア · " + Gears.ManaLabel(run.Mana.GetValueOrDefault()), (VBoxContainer box) =>
			{
				GearInventory(box, run, usable: true);
			});
		});
		Button(statusActions, IsInputBlocked ? "被ダメ予測 · 解決中" : BattleForecast.Short(state), () =>
		{
			Open("被ダメ予測", (VBoxContainer box) =>
			{
				VBoxContainer parent2 = ScrollColumn(box);
				foreach (string current in BattleForecast.Lines(state))
				{
					Text(parent2, current, 23);
				}
			});
		}).Name = "IncomingForecast";
		_blockReadout.MaxLinesVisible = 1;
		_statusReadout.MaxLinesVisible = 1;
		_statusReadout.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
		float enemyPanelHeight = (float)Math.Ceiling((double)(FontSize(20) + FontSize(18) + (PhoneLayout ? FontSize(17) : (2 * FontSize(17) + FontSize(14)))) * 1.55 + 12.0);
		BattlefieldDropTarget stageSpace = new BattlefieldDropTarget
		{
			Name = "BattlefieldDrop",
			SizeFlagsVertical = SizeFlags.ExpandFill,
			CustomMinimumSize = new Vector2(0f, enemyPanelHeight),
			MouseFilter = MouseFilterEnum.Pass,
			CanPlay = CanDropHandCard,
			CardDropped = DropHandCard
		};
		_body.AddChild(stageSpace, forceReadableName: false, InternalMode.Disabled);
		if (_enemyProjection != null)
		{
			BuildProjectedEnemyHud(stageSpace, state);
		}
		else
		{
			HBoxContainer hBoxContainer = Row(stageSpace);
			hBoxContainer.AnchorRight = 1f;
			hBoxContainer.OffsetBottom = enemyPanelHeight;
			hBoxContainer.AddChild(new Control
			{
				CustomMinimumSize = new Vector2(Size.X * 0.26f, 0f),
				MouseFilter = MouseFilterEnum.Ignore
			}, forceReadableName: false, InternalMode.Disabled);
			HBoxContainer enemyRow = Row(hBoxContainer);
			enemyRow.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			enemyRow.Alignment = BoxContainer.AlignmentMode.Center;
			for (int i = 0; i < state.Enemies.Count; i++)
			{
				int index = i;
				EnemyState enemy = state.Enemies[i];
				EnemyDef def = Content.GetEnemyDef(enemy.EnemyId);
				EnemyDropTarget target = new EnemyDropTarget
				{
					Text = "",
					Disabled = (enemy.Hp <= 0 || IsInputBlocked),
					IsBlocked = () => IsInputBlocked,
					CustomMinimumSize = new Vector2((state.Enemies.Count > 3) ? 230 : 270, enemyPanelHeight),
					SizeFlagsHorizontal = SizeFlags.Fill,
					TooltipText = Plain(CardText.EnemyTraits(def, state, i))
				};
				enemyRow.AddChild(target, forceReadableName: false, InternalMode.Disabled);
				Color color = ((index == _preferredTarget) ? Gold : Border);
				target.AddThemeStyleboxOverride("normal", Box(new Color("1a1c33cc"), color, (index != _preferredTarget) ? 1 : 3));
				MarginContainer margins = new MarginContainer
				{
					MouseFilter = MouseFilterEnum.Ignore
				};
				target.AddChild(margins, forceReadableName: false, InternalMode.Disabled);
				margins.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize);
				margins.AddThemeConstantOverride("margin_left", 10);
				margins.AddThemeConstantOverride("margin_right", 10);
				VBoxContainer col = Column(margins);
				col.MouseFilter = MouseFilterEnum.Ignore;
				col.AddThemeConstantOverride("separation", 3);
				Label label = Text(col, $"{i + 1}. {def.Name}" + ((enemy.Hp <= 0) ? "  撃破" : ""), 20, (index == _preferredTarget) ? Gold : Ink);
				label.MaxLinesVisible = 1;
				label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
				_enemyReadouts[i] = Text(col, $"HP {enemy.Hp}/{enemy.MaxHp}  盾 {enemy.Block}", 18, Accent);
				_enemyIntentReadouts[i] = Text(col, (state.HideIntents == true) ? "意図は隠されています" : CardText.IntentText(state, i), 17, Danger);
				_enemyReadouts[i].MaxLinesVisible = 1;
				_enemyIntentReadouts[i].MaxLinesVisible = (PhoneLayout ? 1 : 2);
				_enemyIntentReadouts[i].TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
				if (!PhoneLayout)
				{
					Label label2 = Text(col, EnemyStatus(enemy), 14, Muted);
					label2.MaxLinesVisible = 1;
					label2.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
				}
				target.TooltipText = Plain(def.Name + "\n" + CardText.IntentText(state, i) + "\n" + CardText.EnemyTraits(def, state, i) + "\n" + EnemyStatus(enemy));
				target.Inspect = () =>
				{
					InspectEnemy(index);
				};
				target.Pressed += () =>
				{
					if (!target.ConsumeInspection())
					{
						Sound("ui.click");
						SelectEnemy(index);
					}
				};
				target.CardDropped = (string uid) =>
				{
					CardInstance cardInstance = state.Player.Hand.FirstOrDefault((CardInstance c) => c.Uid == uid);
					if (!(cardInstance == null))
					{
						_preferredTarget = index;
						BeginPlay(cardInstance, index);
					}
				};
			}
		}
		if (PhoneLayout)
		{
			HBoxContainer handHeading = Row(_body);
			Text(handHeading, "手札 · タップで使用 / 敵へドラッグ", 17, Muted);
			if (_preferredTarget >= 0 && _preferredTarget < state.Enemies.Count)
			{
				Text(handHeading, "狙い: " + Content.GetEnemyDef(state.Enemies[_preferredTarget].EnemyId).Name, 17, Gold);
			}
		}
		HBoxContainer handArea = ((!PhoneLayout && _enemyProjection != null) ? Row(_body) : null);
		VBoxContainer left = ((handArea != null) ? Column(handArea) : null);
		if (left != null)
		{
			left.CustomMinimumSize = new Vector2(210f, 0f);
			left.SizeFlagsHorizontal = SizeFlags.Fill;
		}
		ScrollContainer handScroll = new ScrollContainer
		{
			CustomMinimumSize = new Vector2(0f, 290f),
			HorizontalScrollMode = ScrollContainer.ScrollMode.Auto,
			VerticalScrollMode = ScrollContainer.ScrollMode.Disabled,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		((Node)(((object)handArea) ?? ((object)_body))).AddChild(handScroll, forceReadableName: false, InternalMode.Disabled);
		_handScroll = handScroll;
		VBoxContainer right = ((handArea != null) ? Column(handArea) : null);
		if (right != null)
		{
			right.CustomMinimumSize = new Vector2(220f, 0f);
			right.SizeFlagsHorizontal = SizeFlags.Fill;
		}
		HBoxContainer hand = Row(handScroll);
		hand.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		hand.Alignment = BoxContainer.AlignmentMode.Center;
		for (int i2 = 0; i2 < player.Hand.Count; i2++)
		{
			CardInstance card = player.Hand[i2];
			bool playable = FlowRules.PlayBlocked(state, card) == null;
			bool settable = state.ReactionMode != "hold-manual" && SetBase.CanSetCard(state, card.Uid);
			GameUI gameUI = this;
			HBoxContainer parent = hand;
			CardInstance card2 = card;
			Action action = () =>
			{
				CardActions(card);
			};
			string text = $"{i2 + 1}  ";
			string text2 = (player.ImpulseUids.Contains(card.Uid) ? "衝動 · " : "");
			string? text3;
			if (card.Def.Type == "reaction")
			{
				text3 = "仕込み札";
			}
			else
			{
				text3 = (playable ? "使う" : FlowRules.PlayBlocked(state, card));
			}
			CardTile handCard = gameUI.Card(parent, card2, action, enabled: true, text + text2 + text3, playable, Effects.EffectiveCost(state, card));
			if (player.ImpulseUids.Contains(card.Uid))
			{
				handCard.TooltipText = "衝動: このターン限りの手札\n" + handCard.TooltipText;
			}
			handCard.SecondaryAction = () =>
			{
				if (settable && !IsInputBlocked)
				{
					Do(new Command_SetCard
					{
						CardUid = card.Uid
					});
				}
			};
		}
		HBoxContainer bottom = ((handArea == null) ? Row(_body) : null);
		Node pileControls = (Node)(((object)left) ?? ((object)bottom));
		Node turnControls = (Node)(((object)right) ?? ((object)bottom));
		_energyReadout = Text(pileControls, $"エナジー {player.Energy}/{player.EnergyMax}", 23, Accent);
		Button(pileControls, $"山札 {player.DrawPile.Count}", () =>
		{
			OpenDeck(player.DrawPile.OrderBy((CardInstance c) => c.Def.Name, StringComparer.Ordinal).ToArray(), "山札 (名前順・順番は非公開)");
		});
		Button(pileControls, $"捨て札 {player.DiscardPile.Count}", () =>
		{
			OpenDeck(player.DiscardPile, "捨て札");
		});
		Button(pileControls, $"消滅 {player.ExhaustPile.Count}", () =>
		{
			OpenExhaust(state);
		});
		Button(turnControls, "戦闘ログ", () =>
		{
			Open("戦闘ログ", (VBoxContainer box) =>
			{
				VBoxContainer parent2 = ScrollColumn(box);
				foreach (GameEvent current in state.EventLog)
				{
					string text4 = CardText.LogLine(current);
					if (!string.IsNullOrEmpty(text4))
					{
						Text(parent2, text4, 18, CardText.IsPositiveLog(current) ? Accent : Ink);
					}
				}
			});
		});
		if (bottom != null)
		{
			Control spacer = new Control
			{
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				MouseFilter = MouseFilterEnum.Ignore
			};
			bottom.AddChild(spacer, forceReadableName: false, InternalMode.Disabled);
		}
		int sparks = player.Hand.Count((CardInstance c) => c.Def.SparkToken == true);
		if (sparks >= 2)
		{
			Button(turnControls, $"火種を全部撃つ ({sparks})", PlayAllSparks, state.Phase == "player-turn" && state.PendingScry == null, Gold);
		}
		Button(turnControls, "ターンを終了 [E]", BeginEndTurn, state.Phase == "player-turn" && state.PendingScry == null, Gold);
		if (_overlay == null && !IsInputBlocked)
		{
			if (state.PendingScry != null)
			{
				PrepareScry(state);
			}
			else if (state.Phase == "awaiting-reaction")
			{
				PrepareReaction(state);
			}
		}
	}

	private static string PlayerStatus(GameState state)
	{
		PlayerState p = state.Player;
		List<string> values = new List<string>();
		Add("成長", p.Growth);
		Add("勢い", p.Momentum);
		Add("霊気", p.Aether);
		if (Effects.LightMatters(state))
		{
			values.Add("灯 " + p.Light.GetValueOrDefault());
		}
		Add("詠唱数", p.CardsPlayedThisTurn);
		Add("チェインの当たり", p.CascadesThisCombat.GetValueOrDefault());
		if (Effects.IsBlazing(state))
		{
			values.Add("猛り火");
		}
		Add("次の札軽減", p.NextCardDiscount);
		Add("反復", p.SpellEchoes);
		Add("弱体", p.Weak);
		Add("脆弱", p.Vulnerable);
		Add("虚弱", p.Frail);
		Add("拘束", p.Restrain);
		Add("霞み", p.Mist.GetValueOrDefault());
		Add("重り", p.Slow.GetValueOrDefault());
		Add("アーティファクト", p.Artifact.GetValueOrDefault());
		if (values.Count <= 0)
		{
			return "状態異常なし";
		}
		return string.Join("  /  ", values);
		void Add(string name, int value)
		{
			if (value > 0)
			{
				values.Add(name + " " + value);
			}
		}
	}

	private static string EnemyStatus(EnemyState enemy)
	{
		List<string> values = new List<string>();
		if (enemy.Strength != 0)
		{
			values.Add("筋力 " + enemy.Strength);
		}
		if (enemy.Burn > 0)
		{
			values.Add("延焼 " + enemy.Burn);
		}
		if (enemy.Confusion > 0)
		{
			values.Add("混乱 " + enemy.Confusion);
		}
		if (enemy.Exposed > 0)
		{
			values.Add("急所 " + enemy.Exposed);
		}
		if (enemy.Weak.GetValueOrDefault() > 0)
		{
			values.Add("威圧 " + enemy.Weak);
		}
		if (enemy.Artifact.GetValueOrDefault() > 0)
		{
			values.Add("防壁 " + enemy.Artifact);
		}
		return string.Join("  ", values);
	}

	private void CardActions(CardInstance card)
	{
		GameState state = InputCombat;
		if (state == null || IsInputBlocked || state.PendingScry != null)
		{
			return;
		}
		bool canSet = state.ReactionMode != "hold-manual" && SetBase.CanSetCard(state, card.Uid);
		string blocked = FlowRules.PlayBlocked(state, card);
		if (!canSet && blocked == null)
		{
			BeginPlay(card);
			return;
		}
		Open(card.Def.Name, (VBoxContainer box) =>
		{
			Text(box, CardText.Body(card.Def), 23);
			Text(box, CardText.Notes(card.Def), 20, Gold);
			if (blocked == null)
			{
				Button(box, "プレイする", () =>
				{
					BeginPlay(card);
				}, enabled: true, Accent);
			}
			else
			{
				Text(box, blocked, 20, Muted);
			}
			if (canSet)
			{
				Button(box, "仕込む", () =>
				{
					Do(new Command_SetCard
					{
						CardUid = card.Uid
					});
				}, enabled: true, Gold);
			}
			Button(box, "詳しく見る", () =>
			{
				InspectCard(card);
			});
		});
	}

	private void BeginPlay(CardInstance card, int? target = null)
	{
		GameState state = InputCombat;
		if (state == null || IsInputBlocked)
		{
			return;
		}
		string blocked = FlowRules.PlayBlocked(state, card);
		if (blocked != null)
		{
			_notice = blocked;
			_error = true;
			Rebuild();
			return;
		}
		_overlayStack.Clear();
		_overlay = null;
		_play = new PlaySelection(state, card);
		RecordFreezeSelection("GameUI.BeginPlay");
		int aim = target ?? _preferredTarget;
		if (aim >= 0 && aim < state.Enemies.Count && state.Enemies[aim].Hp > 0)
		{
			_play.TargetIndex = FlowRules.GuardRedirect(state, aim);
		}
		AdvancePlay();
	}

	private void AdvancePlay()
	{
		GameState state = InputCombat;
		if (_play == null || state == null || IsInputBlocked)
		{
			return;
		}
		string need = _play.Next(state);
		_freezeSelectionNeed = need;
		RecordFreezeSelection("GameUI.AdvancePlay");
		if (need == null)
		{
			Command_PlayCard command = _play.Command();
			_play = null;
			_overlay = null;
			Do(command);
			return;
		}
		PlaySelection play = _play;
		if (need == "target")
		{
			_overlay = null;
			Rebuild();
			return;
		}
		Open(play.Card.Def.Name + " · " + SelectionTitle(need), (VBoxContainer box) =>
		{
			Text(box, CardText.Body(play.Card.Def), 20, Muted);
			if (need == "kindle")
			{
				Text(box, "衝動の札をくべると「くべたら」の効果も乗ります。払わずに素の効果だけでプレイすることもできます。", 22);
				Button(box, "くべずにプレイ", () =>
				{
					play.Kindle = false;
					AdvancePlay();
				});
				Button(box, $"衝動の札{play.Card.Def.ImpulseCost}枚をくべる", () =>
				{
					play.Kindle = true;
					AdvancePlay();
				}, FlowRules.CanKindle(state, play.Card), Gold);
			}
			else if (need == "mode")
			{
				for (int num = 0; num < play.Card.Def.Modes.Count; num++)
				{
					int index = num;
					CardMode cardMode = play.Card.Def.Modes[num];
					Button(box, cardMode.Name, () =>
					{
						play.ModeIndex = index;
						AdvancePlay();
					});
				}
			}
			else if (need == "target")
			{
				BuildTargetPicker(box, state, (int requested) =>
				{
					play.TargetIndex = FlowRules.GuardRedirect(state, requested);
					AdvancePlay();
				});
			}
			else
			{
				IReadOnlyList<CardInstance> readOnlyList = play.Pool(state, need);
				if (readOnlyList.Count == 0)
				{
					Text(box, "選べる対象がありません。戻ると使用を取り消します。", 22, Danger);
				}
				else
				{
					CardGrid(box, readOnlyList, (CardInstance c) =>
					{
						play.Pick(need, c.Uid);
						AdvancePlay();
					}, null, (CardInstance c) => "選ぶ");
				}
			}
			Button(box, "使用を取り消す", CloseAllOverlays);
		}, close: true, replace: true);
	}

	private static string SelectionTitle(string need)
	{
		return need switch
		{
			"kindle" => "くべるか選ぶ", 
			"impulse" => "追加コスト: 衝動の札をくべる", 
			"mode" => "効果を選ぶ", 
			"x" => "残りのエナジーをすべて使う",
			"discard" => "追加コスト: 手札を捨てる", 
			"exhaust" => "追加コスト: 手札を消滅させる", 
			"retrieve" => "消滅置き場から選ぶ", 
			"deck" => "札を選ぶ", 
			"hand" => "鍛える手札を選ぶ", 
			"permanent" => "人形を選ぶ", 
			"target" => "対象の敵を選ぶ", 
			_ => need, 
		};
	}

	private void BuildTargetPicker(Node parent, GameState state, Action<int> choose, Func<int, string?>? advisory = null)
	{
		for (int i = 0; i < state.Enemies.Count; i++)
		{
			int index = i;
			EnemyState enemy = state.Enemies[i];
			if (enemy.Hp > 0)
			{
				Button(parent, $"{i + 1}. {Content.GetEnemyDef(enemy.EnemyId).Name}  HP {enemy.Hp}/{enemy.MaxHp}", () =>
				{
					choose(index);
				});
				string warning = advisory?.Invoke(index);
				if (warning != null)
				{
					Text(parent, warning, 18, Gold);
				}
			}
		}
	}

	private void PrepareScry(GameState state)
	{
		CardInstance[] shown = state.Player.DrawPile.Take(Math.Min(state.PendingScry.Count, state.Player.DrawPile.Count)).ToArray();
		_scryPicks.RemoveWhere((string uid) => !shown.Any((CardInstance c) => c.Uid == uid));
		_overlayTitle = "占術 · 捨てる札を選ぶ (左が次に引く札)";
		_overlayCanClose = false;
		_overlay = (VBoxContainer box) =>
		{
			Text(box, "選んだ札は捨て札へ。残す札の順番は変わりません。", 20, Muted);
			CardGrid(box, shown, (CardInstance c) =>
			{
				if (!_scryPicks.Add(c.Uid))
				{
					_scryPicks.Remove(c.Uid);
				}
				Rebuild();
			}, null, (CardInstance c) => (!_scryPicks.Contains(c.Uid)) ? "残す" : "捨てる (選択中)");
			Button(box, (_scryPicks.Count == 0) ? "全部残して決める" : $"{_scryPicks.Count}枚を捨てて決める", () =>
			{
				string[] discardUids = _scryPicks.ToArray();
				_scryPicks.Clear();
				Do(new Command_ResolveScry
				{
					DiscardUids = discardUids
				});
			}, enabled: true, Gold);
		};
	}

	private void BeginEndTurn()
	{
		GameState state = _view?.Combat;
		if (state == null || IsInputBlocked || state.Phase != "player-turn" || state.PendingScry != null)
		{
			return;
		}
		int retainMax = Combat.RetainHandMax(state);
		HashSet<string> keep = new HashSet<string>();
		string keepImpulse = null;
		IReadOnlyList<CardInstance> impulse = FlowRules.KeepImpulsePool(state);
		if (impulse.Count == 0)
		{
			Retain();
			return;
		}
		Open("燠の守り · 残す衝動の札を選ぶ", (VBoxContainer box) =>
		{
			Text(box, "1枚を失効させず、次のターンまで手札に残します。", 22);
			CardGrid(box, impulse, (CardInstance c) =>
			{
				keepImpulse = c.Uid;
				Retain();
			}, null, (CardInstance c) => "この衝動の札を残す");
			Button(box, "自動で選ぶ（最も高コストの札）", () =>
			{
				keepImpulse = null;
				Retain();
			}, enabled: true, Gold);
		});
		void Finish(int? sparks)
		{
			Do(new Command_EndTurn
			{
				HearthSparks = sparks,
				RetainUids = ((keep.Count > 0) ? keep.ToArray() : null),
				KeepUid = keepImpulse
			});
		}
		void Hearth()
		{
			int max = Effects.HearthSparkMax(state);
			if (max <= 0)
			{
				Finish(null);
			}
			else
			{
				Open("灯の火床 · 火種の枚数を選ぶ", (VBoxContainer box) =>
				{
					Text(box, $"灯を使って山札へ火種を加える。0〜{max}枚。", 22);
					SpinBox amount = new SpinBox
					{
						MinValue = 0.0,
						MaxValue = max,
						Step = 1.0,
						Value = 0.0,
						CustomMinimumSize = new Vector2(220f, 52f)
					};
					box.AddChild(amount, forceReadableName: false, InternalMode.Disabled);
					Button(box, "この枚数でターン終了", () =>
					{
						Finish((int)amount.Value);
					}, enabled: true, Gold);
				});
			}
		}
		void Retain()
		{
			if (retainMax <= 0)
			{
				Hearth();
			}
			else
			{
				Open("残す手札を選ぶ", (VBoxContainer box) =>
				{
					Text(box, $"{retainMax}枚まで残せます。現在 {keep.Count} 枚。", 21);
					CardGrid(box, state.Player.Hand.Where((CardInstance c) => !c.Def.Id.StartsWith("status_", StringComparison.Ordinal)).ToArray(), (CardInstance c) =>
					{
						if (!keep.Remove(c.Uid) && keep.Count < retainMax)
						{
							keep.Add(c.Uid);
						}
						Rebuild();
					}, (CardInstance c) => keep.Contains(c.Uid) || keep.Count < retainMax, (CardInstance c) => (!keep.Contains(c.Uid)) ? "選ぶ" : "残す (選択中)");
					Button(box, "これでターンを終える", Hearth, enabled: true, Gold);
				});
			}
		}
	}

	private void OpenExhaust(GameState state)
	{
		Open("消滅置き場 · 亡骸", (VBoxContainer box) =>
		{
			CardGrid(box, state.Player.ExhaustPile, (CardInstance card) =>
			{
				if (!card.Def.NecroCost.HasValue)
				{
					InspectCard(card);
				}
				else
				{
					Open(card.Def.Name + " · 亡骸", (VBoxContainer b) =>
					{
						Text(b, CardText.Body(card.Def), 23);
						Text(b, $"エナジー {card.Def.NecroCost} で消滅置き場からプレイ", 22, Gold);
						bool enabled = state.Phase == "player-turn" && state.PendingScry == null && state.Player.Energy >= card.Def.NecroCost;
						Button(b, "亡骸をプレイ", () =>
						{
							if (Effects.CardNeedsTarget(card) && state.Enemies.Count((EnemyState e) => e.Hp > 0) > 1)
							{
								Open("亡骸 · 対象を選ぶ", (VBoxContainer row) =>
								{
									BuildTargetPicker(row, state, (int i) =>
									{
										Play(FlowRules.GuardRedirect(state, i));
									});
								});
							}
							else
							{
								Play(null);
							}
						}, enabled);
					});
				}
				void Play(int? target)
				{
					Do(new Command_PlayNecro
					{
						CardUid = card.Uid,
						TargetIndex = target
					});
				}
			}, null, (CardInstance card) => card.Def.NecroCost.HasValue ? $"亡骸 {card.Def.NecroCost} E" : "詳しく見る");
		});
	}

	private void GearInventory(Node parent, RunState run, bool usable)
	{
		VBoxContainer scroll = ScrollColumn(parent);
		HFlowContainer flow = Flow(scroll);
		for (int i = 0; i < (run.Gears?.Count ?? 0); i++)
		{
			int index = i;
			GearInstance gear = run.Gears[i];
			GearDef def = Content.GetGearDef(gear.GearId);
			VBoxContainer col = Tile(flow, def.Name + $" ×{gear.Charges}", def.Text, "gears", def.Id);
			col.GetParent<Control>().CustomMinimumSize = new Vector2(260f, 170f);
			string blocked = Gears.GearBlockedReason(run.Combat, run.Mana.GetValueOrDefault(), gear);
			if (usable && run.Combat != null)
			{
				string advisory = Gears.GearNoEffectReason(run.Combat, def);
				if (advisory != null)
				{
					Text(col, advisory, 18, Gold);
				}
			}
			if (usable)
			{
				Button(col, blocked ?? "組む (魔素1個)", () =>
				{
					BeginGear(run, index, def);
				}, blocked == null, Accent);
			}
			Button(col, "捨てる", () =>
			{
				Confirm("ギアを手放す？", def.Name + "を持ち物から取り除きます。", () =>
				{
					Do(new RunCommand_DiscardGear
					{
						Index = index
					});
				});
			});
		}
		IReadOnlyList<GearInstance> gears = run.Gears;
		if (gears == null || gears.Count == 0)
		{
			Text(scroll, "ギアを持っていません。", 21, Muted);
		}
	}

	private void BeginGear(RunState run, int index, GearDef original)
	{
		GameState state = run.Combat;
		if (state == null || IsInputBlocked || _session == null)
		{
			return;
		}
		long revision = _session.Revision;
		SourceUiDecision decision = new SourceUiDecision(revision);
		if (original.Special == "nameless")
		{
			Open("無銘の部品 · ギアの効果を選ぶ", (VBoxContainer box) =>
			{
				VBoxContainer parent = ScrollColumn(box);
				foreach (string id in run.SeenGearIds ?? System.Array.Empty<string>())
				{
					GearDef def = Content.GetGearDef(id);
					if (!(def.Special == "nameless"))
					{
						Button(parent, def.Name + " · " + def.Text, () =>
						{
							SelectCard(def, id);
						});
						string text = Gears.GearNoEffectReason(state, def);
						if (text != null)
						{
							Text(parent, text, 18, Gold);
						}
					}
				}
			});
		}
		else
		{
			SelectCard(original, null);
		}
		void Commit(string card, int? target, string asId)
		{
			RunState current = _session?.CurrentRun;
			if (Current() && current?.Gears != null && index >= 0 && index < current.Gears.Count && !(current.Gears[index].GearId != original.Id) && Gears.GearBlockedReason(current.Combat, current.Mana.GetValueOrDefault(), current.Gears[index]) == null && decision.TryAccept(_session.Revision, IsInputBlocked))
			{
				Do(new RunCommand_UseGear
				{
					Index = index,
					CardUid = card,
					TargetIndex = target,
					AsGearId = asId
				});
			}
		}
		bool Current()
		{
			if (_session != null && _session.Revision == revision)
			{
				return !IsInputBlocked;
			}
			return false;
		}
		void SelectCard(GearDef def, string asId)
		{
			if (Current())
			{
				IReadOnlyList<CardInstance> pool = Gears.GearCardChoices(state, def);
				if (SourceCardModalPolicy.NeedsGearCardPicker(def, pool.Count))
				{
					Open("ギア · 札を選ぶ", (VBoxContainer box) =>
					{
						CardGrid(box, pool, (CardInstance card) =>
						{
							SelectTarget(def, card.Uid, asId);
						}, null);
					});
				}
				else
				{
					SelectTarget(def, null, asId);
				}
			}
		}
		void SelectTarget(GearDef def, string card, string asId)
		{
			if (Current())
			{
				if (def.NeedsTarget == true && state.Enemies.Count((EnemyState e) => e.Hp > 0) > 1)
				{
					Open("ギア · 対象の敵を選ぶ", (VBoxContainer box) =>
					{
						BuildTargetPicker(box, state, (int target) =>
						{
							Submit(def, card, target, asId);
						}, (int target) => Gears.GearNoEffectReason(state, def, target));
					});
				}
				else
				{
					Submit(def, card, null, asId);
				}
			}
		}
		void Submit(GearDef def, string card, int? target, string asId)
		{
			if (Current())
			{
				if (SourceCardModalPolicy.RequiresFleeConfirmation(def))
				{
					Confirm("この戦闘から逃げる？", "報酬は得られない。HPはそのまま、この節は踏んだことになる。", () =>
					{
						Commit(card, target, asId);
					}, "逃げる");
				}
				else
				{
					Commit(card, target, asId);
				}
			}
		}
	}

	private int FontSize(int requested)
	{
		return Sizing.Font(requested, _textScale);
	}

	public override void _Ready()
	{
		SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize);
		MouseFilter = MouseFilterEnum.Ignore;
		GetTree().QuitOnGoBack = false;
		AndroidReportShare.ShareRequestFinished += ShareRequestFinished;
		Resized += ApplySafeArea;
		ReadSettings();
		ApplyTheme();
		if (_session != null)
		{
			Rebuild();
		}
	}

	public void Bind(GameSession session)
	{
		if (_session != null)
		{
			_session.StateChanged -= Changed;
		}
		_session = session;
		_session.StateChanged += Changed;
		_session.SetReportText(new ReportText
		{
			LogLine = CardText.LogLine,
			IntentText = CardText.IntentText
		});
		_view = session.CurrentRun;
		_showTitle = _view == null;
		if (IsInsideTree())
		{
			Rebuild();
		}
	}

	public void SetPresentationGate(Func<bool> gate)
	{
		_presentationGate = gate;
	}

	public void PresentationCompleted()
	{
		if (_session != null && !IsInputBlocked)
		{
			_view = _session.CurrentRun;
			_uxPaid.Clear();
			_uxIncomingOwner = -1L;
			PumpSparks();
			if (!IsInputBlocked)
			{
				Rebuild();
			}
		}
	}

	public override void _ExitTree()
	{
        CancelTouchHand(resetPointers: true);
        DisposeFaultDiagnosticLayer();
		FlushSettingsSave();
		UxStopHandDragForTreeExit();
		UxClearEnemyHeadCache();
		AndroidReportShare.ShareRequestFinished -= ShareRequestFinished;
		if (_session != null)
		{
			_session.StateChanged -= Changed;
		}
	}

	private void Changed(SessionChange change)
	{
        CancelTouchHand();
        if (_faultUiUnavailable) { RefreshBlockedFaultSaveStatus(change.Current, change.Revision); return; } // Never expose a stale board.
        if (change.IsReset) ResetPresentationFaultUi();
		_activeChange = change;
		if (change.IsReset || change.Current.Phase != "shop")
		{
			_shopGearsScreen = false;
		}
		if (change.IsReset)
		{
			UxResetHand();
		}
		UxFreezeAdmittedHandDrag(change);
		UxCaptureAdmittedHandExit(change);
		_costPresented = false;
		if (change.IsReset)
		{
			_ratingQueued = false;
		}
		else if (change.Previous?.Phase == "combat" && SourceUiPolicies.RatingPhase(change.Current.Phase))
		{
			_ratingQueued = true;
		}
		_showTitle = false;
		_overlay = null;
		_overlayStack.Clear();
		_play = null;
		_relicPicks.Clear();
		_notice = "";
		if (change.IsReset)
		{
			_scryPicks.Clear();
			_preferredTarget = -1;
			_workshopA = (_workshopB = -1);
			_sparksRemaining = 0;
			_leaderId = change.Current.LeaderId;
			_seed = change.Current.Seed;
			if (change.Current.Difficulty > 0)
			{
				_difficulty = change.Current.Difficulty;
			}
		}
		if (!IsInputBlocked)
		{
			_view = (change.IsReset ? change.Current : UxOrdinaryProjection(change.Current));
		}
		Rebuild();
	}

	public void SelectEnemy(int index)
	{
        // Retiring a pressed Godot button can emit Pressed synchronously from Hide.
        // That old target must not select or rebuild the replacement interface.
        if (_enemySelectionRebuildDepth > 0) return;
		if (IsInputBlocked)
		{
			return;
		}
		GameState targetState = InputCombat;
		if ((object)targetState != null && index >= 0 && index < targetState.Enemies.Count && targetState.Enemies[index].Hp > 0)
		{
			_preferredTarget = index;
			if (_play != null)
			{
				_play.TargetIndex = FlowRules.GuardRedirect(targetState, index);
				AdvancePlay();
			}
			else
			{
				Rebuild();
			}
		}
	}

	public override void _UnhandledKeyInput(InputEvent evt)
	{
		if (!(evt is InputEventKey { Pressed: not false, Echo: false } key) || IsInputBlocked)
		{
			return;
		}
		Control control = GetViewport().GuiGetFocusOwner();
		if (control is LineEdit || control is TextEdit || control is SpinBox)
		{
			return;
		}
		if (key.Keycode == Key.Escape)
		{
			HandleBack();
			GetViewport().SetInputAsHandled();
		}
		else if (_overlay == null && !(CurrentRoute != "combat") && !(_view?.Combat == null))
		{
			if (key.Keycode == Key.E)
			{
				BeginEndTurn();
				GetViewport().SetInputAsHandled();
			}
			if (key.Keycode == Key.G)
			{
				PlayAllSparks();
				GetViewport().SetInputAsHandled();
			}
			int ix = (int)key.Keycode - 49;
			if (ix >= 0 && ix < Math.Min(9, _view.Combat.Player.Hand.Count))
			{
				CardActions(_view.Combat.Player.Hand[ix]);
				GetViewport().SetInputAsHandled();
			}
		}
	}

	private void Result(SessionResult result)
	{
		if (!result.Success || !string.IsNullOrEmpty(result.Warning))
		{
			_notice = result.Error ?? result.Warning ?? "操作できませんでした";
			_error = !result.Success;
			Rebuild();
		}
	}

	private void Do(RunCommand command)
	{
		if (!IsInputBlocked && _session != null)
		{
			Sound(SourceUiPolicies.CommandSound(command));
			Result(_session.Dispatch(command));
		}
	}

	private void Do(Command command)
	{
		if (!IsInputBlocked && _session != null)
		{
			Result(_session.Dispatch(command));
		}
	}

    private int _enemySelectionRebuildDepth;

	private void Rebuild()
	{
        _enemySelectionRebuildDepth++;
        try { RebuildCore(); }
        finally { _enemySelectionRebuildDepth--; }
    }

    private void RebuildCore()
    {
        CancelTouchHand();
		if (!float.IsFinite(Size.X) || !float.IsFinite(Size.Y) || Size.X <= 0f || Size.Y <= 0f)
		{
			_uxLastSize = Size;
			return;
		}
        if (_faultUiUnavailable && !_synchronizingFaultSnapshot) return;
		if (!IsInsideTree() || _session == null)
		{
			return;
		}
        CaptureModalFocusBeforeRebuild();
		CaptureMapScrollBeforeRebuild();
		CaptureCardGridScrollBeforeRebuild();
		foreach (Node child in GetChildren())
		{
			if (child == _uxHand)
			{
				_uxHand.Visible = CurrentRoute == "combat";
				continue;
			}
			if (child is Control c)
			{
				c.Hide();
			}
			child.QueueFree();
		}
		_overlayNode = null;
		_enemyHud.Clear();
		_handScroll = null;
		bool combat = CurrentRoute == "combat";
		if (!combat)
		{
			ColorRect bg = new ColorRect
			{
				Color = new Color("0f1120df"),
				MouseFilter = MouseFilterEnum.Ignore
			};
			AddChild(bg, forceReadableName: false, InternalMode.Disabled);
			bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize);
		}
		MarginContainer margin = new MarginContainer
		{
			MouseFilter = MouseFilterEnum.Ignore
		};
		AddChild(margin, forceReadableName: false, InternalMode.Disabled);
		margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize);
		_safeMargin = margin;
		ApplySafeArea();
		_shell = new VBoxContainer
		{
			MouseFilter = MouseFilterEnum.Ignore
		};
		_shell.AddThemeConstantOverride("separation", 10);
		margin.AddChild(_shell, forceReadableName: false, InternalMode.Disabled);
		BuildHeader();
        if (_notice.Length > 0 && _overlay == null) ShowBoundedNotice(_shell);
		_body = new VBoxContainer
		{
			SizeFlagsVertical = SizeFlags.ExpandFill,
			MouseFilter = MouseFilterEnum.Ignore
		};
		_body.AddThemeConstantOverride("separation", (combat && PhoneLayout) ? 8 : 12);
		_shell.AddChild(_body, forceReadableName: false, InternalMode.Disabled);
		if (_showTitle || _view == null)
		{
			BuildTitle();
		}
		else
		{
			switch (_view.Phase)
			{
			case "departure":
				BuildDeparture(_view);
				break;
			case "map":
				BuildMap(_body, _view, interactive: true);
				break;
			case "combat":
				BuildBattle(_view);
				break;
			case "reward":
				BuildReward(_view);
				break;
			case "relic-reward":
				BuildRelicReward(_view);
				break;
			case "relic-choose":
				BuildRelicChoose(_view);
				break;
			case "shop":
				BuildShop(_view);
				break;
			case "campfire":
				BuildCampfire(_view);
				break;
			case "workshop":
				BuildWorkshop(_view);
				break;
			case "event":
				BuildEvent(_view);
				break;
			case "won":
			case "lost":
				BuildEnd(_view);
				break;
			default:
				Text(_body, "未対応の画面: " + _view.Phase, 24, Danger);
				break;
			}
		}
		if (_overlay != null)
		{
			BuildOverlay();
            RetainModalScrollPositions();
		}
		if (IsInputBlocked)
		{
			ColorRect busy = new ColorRect
			{
				Color = new Color(0f, 0f, 0f, 0.01f),
				MouseFilter = MouseFilterEnum.Stop
			};
			AddChild(busy, forceReadableName: false, InternalMode.Disabled);
			busy.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize);
			busy.ZIndex = 99;
		}
		SizeInteractiveControls(_shell);
		if (_overlayNode != null)
		{
			SizeInteractiveControls(_overlayNode);
			DisableUnderlyingFocus(_shell);
			Control initialFocus = !_overlayCanClose && _overlayTitle.StartsWith("からくりの確認 · ")
                ? Buttons(_overlayNode).FirstOrDefault(b => b.Text == "温存して続ける")
                : RestoreModalFocus() ?? FirstFocusable(_overlayNode);
            initialFocus?.CallDeferred(Control.MethodName.GrabFocus);
		}
        AppendPresentationFaultNotice();
		ScreenChanged?.Invoke(CurrentRoute);
		OfferQueuedRating();
	}

	private void Open(string title, Action<VBoxContainer> body, bool close = true, bool replace = false)
	{
		if (!replace && _overlay != null && _overlayTitle != "メニュー")
		{
			_overlayStack.Push((_overlayTitle, _overlay, _overlayCanClose));
		}
		_overlayTitle = title;
		_overlay = body;
		_overlayCanClose = close;
		Rebuild();
	}

	private void CloseOverlay()
	{
		FlushSettingsSave();
		if (_overlayStack.Count > 0)
		{
			(_overlayTitle, _overlay, _overlayCanClose) = _overlayStack.Pop();
		}
		else
		{
			_overlay = null;
			_play = null;
		}
		Rebuild();
	}

	private void CloseAllOverlays()
	{
		RecordFreezeSelection("GameUI.CloseAllOverlays");
		FlushSettingsSave();
		EndHandDrag();
		_overlayStack.Clear();
		_overlay = null;
		_play = null;
		Rebuild();
	}

	private void OpenMenu()
	{
		EndHandDrag();
		Open("メニュー", (VBoxContainer box) =>
		{
			var menuColumns = Row(box);
            menuColumns.AddThemeConstantOverride("separation", 30);
            var primaryColumn = Column(menuColumns); primaryColumn.SizeFlagsHorizontal = SizeFlags.ExpandFill; primaryColumn.SizeFlagsStretchRatio = 1.65f;
            var secondaryColumn = Column(menuColumns); secondaryColumn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            GridContainer Section(VBoxContainer host, string title, int columns = 2) {
                var heading = Text(host, title, 20, SourcePalette.InkSoft);
                heading.AddThemeFontOverride("font", SourceRunUiDecor.Action);
                var grid = new GridContainer { Columns = columns, SizeFlagsHorizontal = SizeFlags.ExpandFill };
                grid.AddThemeConstantOverride("h_separation", 12); grid.AddThemeConstantOverride("v_separation", 12);
                host.AddChild(grid); return grid;
            }
            var common = Section(primaryColumn, "基本操作");
            var collection = Section(primaryColumn, "冒険の情報");
            var save = Section(secondaryColumn, "保存・再開", 1);
            var leave = Section(secondaryColumn, "中断・終了", 1);
            var danger = Section(secondaryColumn, "ランを破棄", 1);
            Node parent = collection;
			Button(common, "設定", OpenSettings);
			Button(common, "用語集", OpenKeywords);
            Button(common, "動作診断", OpenFreezeDiagnostics);
			Button(save, "セーブを読み込む", OpenImport);
			if (_session?.CurrentRun != null)
			{
				Button(save, "セーブを保存", () =>
				{
					SessionResult sessionResult = _session.Save();
					if (sessionResult.Success)
					{
						_notice = sessionResult.Warning ?? "保存しました";
						_error = false;
						CloseAllOverlays();
					}
					else
					{
						Result(sessionResult);
					}
				});
				Button(save, "書き出す", OpenExport);
				Button(parent, "デッキ", () =>
				{
					OpenDeck(_session.CurrentRun.Deck, "デッキ");
				});
				Button(parent, "遺物", () =>
				{
					OpenRelics(_session.CurrentRun);
				});
				GameState state = InputCombat;
				if ((object)state != null)
				{
					Button(parent, "状態・能力", () =>
					{
						Open("状態・能力", (VBoxContainer b) =>
						{
							Text(b, PlayerStatus(state), 23);
							Text(b, "置物・人形", 22, Gold);
							CardGrid(b, state.Player.Permanents, InspectCard, null, (CardInstance card) => (!Effects.IsDoll(card)) ? "常在" : ("残り " + (Effects.DollLifeLeft(state, card)?.ToString() ?? "∞") + " ターン"));
						});
					});
					Button(parent, "仕込んでいるからくり", () =>
					{
						Open("仕込んでいるからくり", (VBoxContainer b) =>
						{
							CardGrid(b, state.Player.SetCards, InspectCard, null, (CardInstance card) => Effects.TrapStatusTextKarakuri(state, card));
						});
					});
					Button(parent, "ギア", () =>
					{
						Open("ギア · " + Gears.ManaLabel(_session.CurrentRun.Mana.GetValueOrDefault()), (VBoxContainer b) =>
						{
							GearInventory(b, _session.CurrentRun, usable: true);
						});
					});
					Button(parent, "戦闘ログ", () =>
					{
						Open("戦闘ログ", (VBoxContainer b) =>
						{
							VBoxContainer parent2 = ScrollColumn(b);
							foreach (GameEvent current in state.EventLog)
							{
								string text = CardText.LogLine(current);
								if (!string.IsNullOrEmpty(text))
								{
									Text(parent2, text, 18, CardText.IsPositiveLog(current) ? Accent : Ink);
								}
							}
						});
					});
				}
				Button(parent, "ランの記録", OpenJournal);
				Button(parent, "プレイメモ", OpenNote);
				if (_session.History.Count > 0)
				{
					Button(parent, "直前の戦闘を評価", OpenRating);
				}
				Button(parent, "地図", () =>
				{
					Open("坑道の地図", (VBoxContainer b) =>
					{
						BuildMap(b, _session.CurrentRun, interactive: false);
					});
				});
				bool flag = _session.CurrentRun.Phase == "won" || _session.CurrentRun.Phase == "lost";
				Button(leave, flag ? "タイトルへ" : "セーブして終了", ShowTitle);
				if (!flag)
				{
					Button(danger, "ランを放棄", () =>
					{
						Confirm("ランを放棄する？", "このランのセーブは消え、続きから再開できなくなります。\n（レポートの自動保存は残るので、タイトルから書き出せます）", () =>
						{
							SessionResult sessionResult = _session.AbandonRun();
							if (!sessionResult.Success)
							{
								Result(sessionResult);
							}
							else
							{
								_view = null;
								_showTitle = true;
								_overlay = null;
								_overlayStack.Clear();
								_play = null;
								_notice = sessionResult.Warning ?? "ランを放棄した";
								_error = sessionResult.Warning != null;
								Rebuild();
							}
						}, "放棄する");
					}, enabled: true, Danger);
				}
			}
            foreach (var grid in new[] { common, collection, save, leave, danger }) {
                if (grid.GetChildCount() == 0) { grid.Visible = false; if (grid.GetIndex() > 0 && grid.GetParent().GetChild(grid.GetIndex()-1) is Control label) label.Visible = false; }
                foreach (var node in grid.GetChildren()) if (node is Button button) {
                    button.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                    button.CustomMinimumSize = new Vector2(0, Math.Max(MinimumHit, 58 * RunUnit));
                    button.AutowrapMode = TextServer.AutowrapMode.WordSmart;
                    button.Alignment = HorizontalAlignment.Left;
                    if (grid == danger) {
                        button.AddThemeStyleboxOverride("normal", SourceRunUiDecor.Paper(new Color("f2dcd5"), RunUnit, 12, true));
                        button.AddThemeColorOverride("font_color", SourcePalette.BadInk);
                    }
                }
            }
			Text(box, PhoneLayout ? "札を長押しで詳細 ・ 敵へドラッグで対象を選択" : "1〜9 手札　G 火種　E ターン終了　Esc 戻る", 17, Muted);
		});
	}

	private void Confirm(string title, string description, Action action, string okLabel = "確認して続ける")
	{
		SourceUiDecision decision = new SourceUiDecision(_session?.Revision ?? 0);
		Open(title, (VBoxContainer box) =>
		{
			box.SetMeta("modal_kind", "confirmation");
			Text(box, description, 23).AddThemeConstantOverride("line_spacing", 6);
            var actions = Row(box);
            actions.AddThemeConstantOverride("separation", 12);
            actions.Alignment = BoxContainer.AlignmentMode.End;
            var cancel = Button(actions, "やめる", CloseOverlay);
            cancel.CustomMinimumSize = new Vector2(150 * RunUnit, MinimumHit);
            var accept = RunButton(actions, okLabel, () =>
			{
				if (decision.TryAccept(_session?.Revision ?? 0, IsInputBlocked))
				{
					if (_overlayStack.Count > 0)
					{
						(string, Action<VBoxContainer>, bool) tuple = _overlayStack.Pop();
						_overlayTitle = tuple.Item1;
						_overlay = tuple.Item2;
						_overlayCanClose = tuple.Item3;
					}
					else
					{
						_overlay = null;
					}
					action();
					Rebuild();
				}
			}, enabled: true, primary: true);
            accept.CustomMinimumSize = new Vector2(230 * RunUnit, MinimumHit);
            accept.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            if (okLabel.Contains("放棄") || okLabel.Contains("削除") || title.Contains("手放す")) accept.SetMeta("source_danger_action", true);
		});
	}

	private void OpenDeck(IReadOnlyList<CardInstance> deck, string title)
	{
		Open(title, (VBoxContainer box) =>
		{
			CardGrid(box, deck, InspectCard, null);
		});
        RegisterDeckBrowser(deck);
	}

	private void OpenRelics(RunState run)
	{
		Open("遺物", (VBoxContainer box) =>
		{
			VBoxContainer parent = ScrollColumn(box);
            if (run.Relics.Count == 0)
            {
                Text(parent, "遺物はまだありません", 24, Gold);
                Text(parent, "冒険の報酬や商店で手に入れた遺物は、ここで効果を確認できます。", 19, Muted);
                return;
            }
			foreach (string current in run.Relics)
			{
				RelicDef relicDef = Content.GetRelicDef(current);
				GameUI gameUI = this;
				string name = relicDef.Name;
				int? num = Run.RelicChargesLeft(run, current);
				object obj;
				if (num.HasValue)
				{
					int valueOrDefault = num.GetValueOrDefault();
					obj = $" (残り{valueOrDefault}回)";
				}
				else
				{
					obj = "";
				}
				gameUI.Tile(parent, name + (string)obj, relicDef.Description, "relics", current);
			}
		});
	}

	private void OpenKeywords()
    {
        string query = "";
        Open("用語集", box => BuildSearchableKeywords(box, () => query, value => query = value));
    }

	private void OpenJournal()
    {
        if (_session != null) OpenSearchableJournal();
    }

	private void OpenExport()
	{
		if (_session == null)
		{
			return;
		}
		if (_session.CurrentRun == null)
		{
			OpenRetainedExport();
			return;
		}
		try
		{
			OpenExportPair(_session.ExportSave(), _session.ExportText(), "セーブとレポートを書き出す");
		}
		catch (Exception ex)
		{
			_notice = "書き出せません: " + ex.Message;
			_error = true;
			Rebuild();
		}
	}

	private void OpenRetainedExport()
	{
		if (_session == null)
		{
			return;
		}
		try
		{
			SessionReport report = _session.ExportRetainedReport();
			OpenExportPair(report.SaveJson, report.Text, "保存したランのレポート · " + report.CapturedAt);
		}
		catch (Exception ex)
		{
			_notice = "レポートを読めません: " + ex.Message;
			_error = true;
			Rebuild();
		}
	}

	private void OpenExportPair(string save, string report, string title)
	{
		Open(title, (VBoxContainer box) =>
		{
			Text(box, "セーブは途中から再開するためのデータ。レポートはランの記録です。", 20);
			HFlowContainer parent = Flow(box);
			Button(parent, "セーブをコピー", () =>
			{
				CopyText(save);
			}, ClipboardAvailable);
			Button(parent, "レポートをコピー", () =>
			{
				CopyText(report);
			}, ClipboardAvailable);
			if (Encoding.UTF8.GetByteCount(save) > 524288)
			{
				Text(box, "大きなセーブです。端末の制限を避けるためファイル保存をおすすめします。", 18, Gold);
			}
			Button(parent, "セーブをファイルに保存", () =>
			{
				ExportFile(save, "deckrogue-save.json");
			});
			Button(parent, "レポートをファイルに保存", () =>
			{
				ExportFile(report, "deckrogue-report.md");
			});
			if (AndroidReportShare.IsAvailable)
			{
				Button(parent, "レポートとセーブを共有", () =>
				{
					SharePair(report, save);
				}, enabled: true, Gold);
			}
			TextEdit node = new TextEdit
			{
				Text = report,
				Editable = false,
				SizeFlagsVertical = SizeFlags.ExpandFill,
				WrapMode = TextEdit.LineWrappingMode.Boundary
			};
			box.AddChild(node, forceReadableName: false, InternalMode.Disabled);
		});
	}

	private void CopyText(string text)
	{
		try
		{
			if (!ClipboardAvailable)
			{
				throw new InvalidOperationException("この端末ではコピーが使えません");
			}
			DisplayServer.ClipboardSet(text);
			bool verified = DisplayServer.ClipboardGet() == text;
			_notice = (verified ? "コピーしました" : "コピー内容を確認できません。ファイル保存を使ってください。");
			_error = !verified;
		}
		catch (Exception ex)
		{
			_notice = "コピーできません: " + ex.Message + "。ファイル保存を使ってください。";
			_error = true;
		}
		CloseOverlay();
	}

	private void ExportFile(string text, string name)
	{
		if (OS.HasFeature("mobile") && !NativeFilesAvailable)
		{
			_notice = "この端末ではファイル選択が使えません。コピーで書き出してください。";
			_error = true;
			Rebuild();
			return;
		}
		FileDialog dialog = new FileDialog
		{
			FileMode = FileDialog.FileModeEnum.SaveFile,
			Access = FileDialog.AccessEnum.Filesystem,
			UseNativeDialog = NativeFilesAvailable,
			CurrentFile = name,
			Title = "書き出し先を選ぶ"
		};
		dialog.Filters = (name.EndsWith(".json", StringComparison.Ordinal) ? new string[1] { "*.json;JSON save;application/json" } : new string[1] { "*.md;Markdown report;text/markdown" });
		AddChild(dialog, forceReadableName: false, InternalMode.Disabled);
		dialog.FileSelected += (string path) =>
		{
			try
			{
				using FileAccess fileAccess = FileAccess.Open(path, FileAccess.ModeFlags.Write);
				if (fileAccess == null)
				{
					_notice = "書き込めません: " + FileAccess.GetOpenError();
					_error = true;
				}
				else
				{
					fileAccess.StoreString(text);
					fileAccess.Flush();
					Error error = fileAccess.GetError();
					_notice = ((error == Error.Ok) ? ("保存しました: " + path) : ("書き込みに失敗しました: " + error));
					_error = error != Error.Ok;
				}
			}
			catch (Exception ex)
			{
				_notice = "書き込みに失敗しました: " + ex.Message;
				_error = true;
			}
			dialog.QueueFree();
			CloseAllOverlays();
		};
		dialog.Canceled += dialog.QueueFree;
		dialog.PopupCenteredRatio(0.85f);
	}

	private void ImportFile()
	{
		if (OS.HasFeature("mobile") && !NativeFilesAvailable)
		{
			_notice = "この端末ではファイル選択が使えません。貼り付けで読み込んでください。";
			_error = true;
			Rebuild();
			return;
		}
		FileDialog dialog = new FileDialog
		{
			FileMode = FileDialog.FileModeEnum.OpenFile,
			Access = FileDialog.AccessEnum.Filesystem,
			UseNativeDialog = NativeFilesAvailable,
			Title = "セーブを選ぶ",
			Filters = new string[1] { "*.json;JSON save;application/json" }
		};
		AddChild(dialog, forceReadableName: false, InternalMode.Disabled);
		dialog.FileSelected += (string path) =>
		{
			using FileAccess fileAccess = FileAccess.Open(path, FileAccess.ModeFlags.Read);
			dialog.QueueFree();
			if (fileAccess == null)
			{
				ShowImportFailure("ファイルを開けませんでした。保存場所と読み取り権限を確認してください。", FileAccess.GetOpenError().ToString());
			}
			else
			{
				string asText = fileAccess.GetAsText();
				Error error = fileAccess.GetError();
				if (error != Error.Ok && error != Error.FileEof)
				{
					ShowImportFailure("ファイルを読み取れませんでした。ファイルを選び直してください。", error.ToString());
				}
				else
				{
					ImportText(asText);
				}
			}
		};
		dialog.Canceled += dialog.QueueFree;
		dialog.PopupCenteredRatio(0.85f);
	}

	private string _importError = "";
	private string _importTechnicalError = "";
	private string _settingsSaveError = "";
	private Label _settingsSaveStatus;
	private Button _settingsRetryButton;
	private bool _settingsDirty;
	private double _settingsSaveDelay;

	private void OpenImport()
	{
		_importError = "";
		_importTechnicalError = "";
		string draft = "";
		int caretLine = 0, caretColumn = 0;
		int selectionFromLine = 0, selectionFromColumn = 0, selectionToLine = 0, selectionToColumn = 0;
		bool selected = false;
		Open("セーブを読み込む", (VBoxContainer box) =>
		{
			Text(box, "セーブデータを貼り付けるか、ファイルを選んでください。現在のランがある場合は置き換えます。", 20);
			TextEdit edit = new TextEdit
			{
				Text = draft,
				PlaceholderText = "セーブデータをここに貼り付け",
				SizeFlagsVertical = SizeFlags.ExpandFill,
				WrapMode = TextEdit.LineWrappingMode.Boundary
			};
			box.AddChild(edit, forceReadableName: false, InternalMode.Disabled);
			StylePaperTextInput(edit);
			edit.SetCaretLine(caretLine);
			edit.SetCaretColumn(caretColumn);
			if (selected) edit.Select(selectionFromLine, selectionFromColumn, selectionToLine, selectionToColumn);
			void CaptureDraft()
			{
				draft = edit.Text;
				caretLine = edit.GetCaretLine();
				caretColumn = edit.GetCaretColumn();
				selected = edit.HasSelection();
				if (selected)
				{
					selectionFromLine = edit.GetSelectionFromLine();
					selectionFromColumn = edit.GetSelectionFromColumn();
					selectionToLine = edit.GetSelectionToLine();
					selectionToColumn = edit.GetSelectionToColumn();
				}
			}
			edit.TextChanged += CaptureDraft;
			edit.CaretChanged += CaptureDraft;
			if (_importError.Length > 0) Text(box, _importError, 18, Danger);
			if (_importTechnicalError.Length > 0) Button(box, "エラーの詳細", () =>
			{
				Open("読込エラーの詳細", details =>
				{
					Text(ScrollColumn(details), "調査用の技術情報です。入力したセーブデータは元の画面に残っています。\n\n" + _importTechnicalError, 18);
				});
			});
			HBoxContainer parent = Row(box);
			Button(parent, "貼り付け", () =>
			{
				try
				{
					edit.Text = DisplayServer.ClipboardGet();
					CaptureDraft();
				}
				catch (Exception ex)
				{
					ShowImportFailure("貼り付けできませんでした。クリップボードの内容と権限を確認してください。", ex.Message);
				}
			}, ClipboardAvailable);
			Button(parent, "読み込む", () =>
			{
				CaptureDraft();
				ImportText(draft);
			});
			Button(parent, "ファイルを選ぶ", ImportFile);
		});
	}

	private void ShowImportFailure(string message, string technical = "")
	{
		_importError = message;
		_importTechnicalError = technical;
		// Keep actionable errors beside their input, not duplicated in the global
		// notice area. Technical diagnostics are opt-in and preserve the draft.
		_notice = "";
		_error = false;
		Rebuild();
	}

	private void ImportText(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			ShowImportFailure("セーブデータを入力してください。");
			return;
		}
		Action load = () =>
		{
			bool wasTitle = _showTitle;
			_showTitle = false;
			if (_session != null)
			{
				SessionResult result = _session.Load(text);
				if (!result.Success)
				{
					_showTitle = wasTitle;
					string explanation = "このセーブデータを読み込めません。対応するゲームのセーブか確認してください。";
					try { JObject.Parse(text); }
					catch (Exception) { explanation = "セーブデータの形式を読み取れません。JSON全体を確認してください。"; }
					ShowImportFailure(explanation, result.Error ?? "詳細情報はありません。");
				}
				else
				{
					_importError = "";
					_importTechnicalError = "";
					Result(result);
				}
			}
		};
		string warning = "";
		try
		{
			string fingerprint = (string?)JObject.Parse(text)["fingerprint"];
			if (!string.IsNullOrEmpty(fingerprint) && fingerprint != _session?.Catalog.Fingerprint)
			{
				warning = "別のデータ版のセーブです。カードや敵の定義が変わっていると、結果が変わる可能性があります。";
			}
		}
		catch (Exception)
		{
		}
		if (!(_session?.CurrentRun != null))
		{
			GameSession session = _session;
			if ((session == null || !session.HasSave) && warning.Length <= 0)
			{
				load();
				return;
			}
		}
		Confirm((warning.Length > 0) ? "別のデータ版を読み込む？" : "現在のランを置き換える？", warning + "読み込むセーブの状態から再開します。現在のランがある場合は置き換えます。", load);
	}

	public override void _Notification(int what)
	{
        if (what == NotificationApplicationPaused || what == NotificationApplicationFocusOut ||
            what == NotificationWMWindowFocusOut || what == NotificationPaused)
            CancelTouchHand(resetPointers: true);
		if ((long)what == 1007 && IsInsideTree())
		{
			HandleBack();
		}
	}

	private void HandleBack()
	{
		if (!IsInputBlocked)
		{
			if (_overlay != null && _overlayCanClose)
			{
				CloseOverlay();
			}
			else if (_play != null || _uxDrag != null)
			{
				EndHandDrag();
				_play = null;
				Rebuild();
			}
			else if (!_showTitle && CurrentRoute == "shop" && _shopGearsScreen)
			{
				ShowShopShelf(gears: false);
			}
			else
			{
				OpenMenu();
			}
		}
	}

	private void ApplySafeArea()
	{
		if (_safeMargin == null || !GodotObject.IsInstanceValid(_safeMargin))
		{
			return;
		}
		int top;
		int right;
		int left;
		int bottom = (top = (right = (left = (int)Math.Ceiling(Sizing.Edge))));
		if (OS.HasFeature("mobile"))
		{
			Rect2I safe = DisplayServer.GetDisplaySafeArea();
			Vector2I screen = DisplayServer.ScreenGetSize();
			if (screen.X > 0 && screen.Y > 0 && safe.Size.X > 0)
			{
				float sx = Size.X / (float)screen.X;
				float sy = Size.Y / (float)screen.Y;
				left += (int)Math.Max(0f, (float)safe.Position.X * sx);
				top += (int)Math.Max(0f, (float)safe.Position.Y * sy);
				right += (int)Math.Max(0f, (float)(screen.X - safe.End.X) * sx);
				bottom += (int)Math.Max(0f, (float)(screen.Y - safe.End.Y) * sy);
			}
		}
        // QA uses physical-pixel insets so ultra-wide devices and cutouts can be
        // exercised on desktop/headless with exactly the same logical conversion.
        foreach (string arg in OS.GetCmdlineUserArgs())
        {
            if (!arg.StartsWith("--ui-safe-insets=")) continue;
            var parts = arg.Substring(17).Split(',');
            if (parts.Length != 4) continue;
            var values = new float[4];
            bool valid = true;
            for (int i = 0; i < 4; i++) valid &= float.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out values[i]) && float.IsFinite(values[i]) && values[i] >= 0;
            if (!valid) continue;
            var pixels = GetWindow().Size;
            if (pixels.X <= 0 || pixels.Y <= 0) continue;
            left += (int)Math.Ceiling(values[0] * Size.X / pixels.X);
            top += (int)Math.Ceiling(values[1] * Size.Y / pixels.Y);
            right += (int)Math.Ceiling(values[2] * Size.X / pixels.X);
            bottom += (int)Math.Ceiling(values[3] * Size.Y / pixels.Y);
        }
		_safeMargin.AddThemeConstantOverride("margin_left", left);
		_safeMargin.AddThemeConstantOverride("margin_right", right);
		_safeMargin.AddThemeConstantOverride("margin_top", top);
		_safeMargin.AddThemeConstantOverride("margin_bottom", bottom);
	}

	private string _settingsLoadWarning = "";
	private int _settingsPage;
	private bool _reduceRewardGlints;

	private void ReadSettings()
	{
		ConfigFile config = new ConfigFile();
		Error loadResult = config.Load("user://settings.cfg");
		bool repaired = loadResult != Error.Ok && loadResult != Error.FileNotFound;
		if (loadResult != Error.Ok) config = new ConfigFile();
		float Number(string section, string key, float fallback, float min, float max)
		{
			Variant value = config.GetValue(section, key, fallback);
			if (value.VariantType != Variant.Type.Float && value.VariantType != Variant.Type.Int)
			{
				repaired = true;
				return fallback;
			}
			double number = (double)value;
			if (!double.IsFinite(number)) { repaired = true; return fallback; }
			float clamped = Math.Clamp((float)number, min, max);
			if (Math.Abs(clamped - number) > 0.00001) repaired = true;
			return clamped;
		}
		bool Flag(string section, string key, bool fallback)
		{
			Variant value = config.GetValue(section, key, fallback);
			if (value.VariantType == Variant.Type.Bool) return (bool)value;
			repaired = true;
			return fallback;
		}
		_textScale = Number("ui", "text_scale", 1f, 1f, 1.3f);
		foreach (string arg in OS.GetCmdlineUserArgs())
		{
			if (arg.StartsWith("--ui-text-scale=") && float.TryParse(arg.Substring(16), NumberStyles.Float, CultureInfo.InvariantCulture, out var scale) && float.IsFinite(scale))
				_textScale = Math.Clamp(scale, 1f, 1.3f);
		}
		_masterVolume = Number("audio", "master", 0.8f, 0f, 1f);
		_bgmVolume = Number("audio", "bgm", 0.3f, 0f, 1f);
		_sfxVolume = Number("audio", "sfx", 0.7f, 0f, 1f);
		_muted = Flag("audio", "muted", false);
		_reduceRewardGlints = Flag("ui", "reduce_reward_glints", false);
		Variant quality = config.GetValue("graphics", "quality", "auto");
		_graphicsQuality = quality.VariantType == Variant.Type.String ? (string)quality : "auto";
		if (quality.VariantType != Variant.Type.String || !new[] { "auto", "high", "standard", "low" }.Contains(_graphicsQuality))
		{
			_graphicsQuality = "auto";
			repaired = true;
		}
		if (DesktopVideoAvailable)
		{
			Vector2I actual = DisplayServer.WindowGetSize();
			Vector2I screen = DisplayServer.ScreenGetSize(DisplayServer.WindowGetCurrentScreen());
			int maxWidth = screen.X > 0 ? Math.Max(640, screen.X) : 8192;
			int maxHeight = screen.Y > 0 ? Math.Max(360, screen.Y) : 8192;
			_windowResolution = new Vector2I((int)Number("window", "width", Math.Max(640, actual.X), 640, maxWidth), (int)Number("window", "height", Math.Max(360, actual.Y), 360, maxHeight));
			if (Flag("window", "fullscreen", false)) DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
			else if (config.HasSectionKey("window", "width")) DisplayServer.WindowSetSize(_windowResolution);
		}
		_settingsLoadWarning = repaired ? "読み取れない設定を安全な値に戻しました。ゲームのセーブデータは変更していません。" : "";
		if (repaired) { _notice = _settingsLoadWarning; _error = false; }
		ApplyAudio();
	}

	private void ApplyAudio()
	{
		Bus("Master", _masterVolume, _muted);
		Bus("BGM", _bgmVolume, mute: false);
		Bus("SFX", _sfxVolume, mute: false);
		static void Bus(string name, float level, bool mute)
		{
			int index = AudioServer.GetBusIndex(name);
			if (index < 0)
			{
				index = AudioServer.BusCount;
				AudioServer.AddBus();
				AudioServer.SetBusName(index, name);
				AudioServer.SetBusSend(index, "Master");
			}
			AudioServer.SetBusMute(index, mute || level <= 0f);
			if (level > 0f)
			{
				AudioServer.SetBusVolumeDb(index, Mathf.LinearToDb(level));
			}
		}
	}

	private void SaveSettings()
	{
		ConfigFile config = new ConfigFile();
		config.Load("user://settings.cfg");
		config.SetValue("ui", "text_scale", _textScale);
		config.SetValue("ui", "reduce_reward_glints", _reduceRewardGlints);
		config.SetValue("audio", "master", _masterVolume);
		config.SetValue("audio", "bgm", _bgmVolume);
		config.SetValue("audio", "sfx", _sfxVolume);
		config.SetValue("audio", "muted", _muted);
		config.SetValue("graphics", "quality", _graphicsQuality);
		if (DesktopVideoAvailable)
		{
			config.SetValue("window", "fullscreen", IsFullscreenMode);
			if (!IsFullscreenMode)
			{
				_windowResolution = DisplayServer.WindowGetSize();
			}
			config.SetValue("window", "width", _windowResolution.X);
			config.SetValue("window", "height", _windowResolution.Y);
		}
		string previousError = _settingsSaveError;
		Error result = config.Save("user://settings.cfg");
		_settingsDirty = result != Error.Ok;
		_settingsSaveDelay = 0;
		_settingsSaveError = result == Error.Ok ? "" : "設定を保存できませんでした（" + result + "）。変更は今回のみ有効です。空き容量・書き込み権限を確認してください。";
		if (GodotObject.IsInstanceValid(_settingsSaveStatus))
		{
			_settingsSaveStatus.Text = _settingsSaveError.Length > 0 ? _settingsSaveError : "設定をこの端末に保存しました。";
			_settingsSaveStatus.AddThemeColorOverride("font_color", RunTextColor(_settingsSaveStatus, _settingsSaveError.Length > 0 ? Danger : Muted));
		}
		if (GodotObject.IsInstanceValid(_settingsRetryButton)) _settingsRetryButton.Visible = result != Error.Ok;
		if (result == Error.Ok && previousError.Length > 0 && _notice == previousError)
		{
			_notice = "設定を保存しました。";
			_error = false;
		}
		if (result != Error.Ok)
		{
			_notice = _settingsSaveError;
			_error = true;
		}
	}

	private void QueueSettingsSave()
	{
		_settingsDirty = true;
		_settingsSaveDelay = 0.35;
	}

	private void FlushSettingsSave()
	{
		if (_settingsDirty) SaveSettings();
	}

	private void ResetPlayerSettings()
	{
		_masterVolume = 0.8f;
		_bgmVolume = 0.3f;
		_sfxVolume = 0.7f;
		_muted = false;
		_textScale = 1f;
		_graphicsQuality = "auto";
		_settingsLoadWarning = "";
		ApplyAudio();
		ApplyTheme();
		QualityChanged?.Invoke(_graphicsQuality);
		SaveSettings();
		Rebuild();
	}

	private void OpenSettings()
	{
		_settingsPage = 0;
		Open("設定", (VBoxContainer outer) =>
		{
			HFlowContainer pages = Flow(outer);
			string[] names = { "音量", "見やすさ", "表示", "詳細" };
			for (int i = 0; i < names.Length; i++)
			{
				int page = i;
				Button(pages, (_settingsPage == i ? "● " : "○ ") + names[i], () =>
				{
					FlushSettingsSave();
					_settingsPage = page;
					Rebuild();
				});
			}
			VBoxContainer box = ScrollColumn(outer);
			if (_settingsPage == 0)
			{
				SectionHeading(box, "音量", 23, Gold);
				Text(box, "調整はすぐに反映されます。0%で完全に消音します。", 18, Muted);
				Volume("全体音量", _masterVolume, value => _masterVolume = value);
				Volume("音楽", _bgmVolume, value => _bgmVolume = value);
				Volume("効果音", _sfxVolume, value => _sfxVolume = value);
				SettingsToggle(box, "ミュート", _muted, value => { _muted = value; ApplyAudio(); SaveSettings(); }, "ON：すべて消音 / OFF：設定した音量で再生");
			}
			else if (_settingsPage == 1)
			{
				SectionHeading(box, "文字の大きさ", 23, Gold);
				HFlowContainer choices = Flow(box);
				foreach (float size in new[] { 1f, 1.15f, 1.3f })
					Button(choices, (Math.Abs(_textScale - size) < 0.01f ? "● " : "○ ") + $"{Math.Round(size * 100f)}%", () =>
					{
						_textScale = size; SaveSettings(); ApplyTheme(); Rebuild();
					});
				Text(box, $"現在 {Math.Round(_textScale * 100f)}% · 変更後の文字サイズをこの画面で確認できます。", 18, Muted);
				Text(box, "見本：カードを選び、効果を確認してプレイ。", 22);
				Text(box, "カメラは揺れません。画面が狭い場合も、説明や選択肢はスクロールして読めます。", 18, Muted);
				SectionHeading(box, "光る装飾", 21, Gold);
				SettingsToggle(box, "報酬・リザルトの光る演出を抑える", _reduceRewardGlints, value => { _reduceRewardGlints = value; SaveSettings(); }, "ON：光る縁取りを省く / OFF：通常の演出");
				Text(box, "報酬を開いたときと勝敗画面の光の縁取りだけを省きます。選択中の枠、敵の攻撃、命中の表示は変わりません。", 18, Muted);
			}
			else if (_settingsPage == 2)
			{
				if (QualityChanged != null)
				{
					SectionHeading(box, "描画品質", 23, Gold);
					HFlowContainer quality = Flow(box);
					foreach (var entry in new[] { ("auto", "自動"), ("high", "高"), ("standard", "標準"), ("low", "低") })
						Button(quality, (_graphicsQuality == entry.Item1 ? "● " : "○ ") + entry.Item2, () =>
						{
							_graphicsQuality = entry.Item1; QualityChanged?.Invoke(_graphicsQuality); SaveSettings(); Rebuild();
						});
					Text(box, "自動は端末に合わせて調整します。動作が重い場合は品質を下げてください。", 18, Muted);
					if (_graphicsQuality == "auto" && _effectiveQuality != null) Text(box, "現在の品質：" + SourceRunScreenPolicy.QualityName(_effectiveQuality()), 18, Muted);
				}
				BuildDesktopVideoSettings(box);
				if (!DesktopVideoAvailable) Text(box, "画面の大きさは端末に合わせて調整します。", 18, Muted);
			}
			else
			{
				SectionHeading(box, "設定の復元", 23, Gold);
				Text(box, "音量・ミュート・文字サイズ・描画品質だけを初期値に戻します。ランの進行、カード、セーブ、画面モードは変更しません。", 18, Muted);
				Button(box, "音量・文字・品質を初期値に戻す", () => Confirm("設定を初期値に戻す？", "音量80%・音楽30%・効果音70%・ミュートOFF・文字100%・描画品質は自動に戻ります。ゲームのセーブは変更しません。", ResetPlayerSettings, "設定だけ初期値に戻す"));
				AddFreezeDiagnosticsMenuButton(box);
				if (_setPresentationDiagnostics != null)
				{
					SectionHeading(box, "動作の診断", 23, Gold);
					Text(box, "不具合を調べるための詳細機能です。通常のプレイでは操作不要です。", 18, Muted);
					BuildPresentationDiagnosticsControls(box);
				}
			}
			if (_settingsLoadWarning.Length > 0) Text(box, _settingsLoadWarning, 18, Muted);
			_settingsSaveStatus = Text(outer, _settingsSaveError.Length > 0 ? _settingsSaveError : "変更はこの端末に自動保存します。", 17, _settingsSaveError.Length > 0 ? Danger : Muted);
			_settingsRetryButton = Button(outer, "設定の保存を再試行", SaveSettings);
			_settingsRetryButton.Visible = _settingsSaveError.Length > 0;
			void Volume(string name, float current, Action<float> setter)
			{
				HBoxContainer line = Row(box);
				Text(line, name, 21, Gold);
				HSlider volume = new HSlider { MinValue = 0, MaxValue = 1, Step = 0.01, Value = current, CustomMinimumSize = new Vector2(280f, 64f), SizeFlagsHorizontal = SizeFlags.ExpandFill, TickCount = 5, TicksOnBorders = true };
				line.AddChild(volume);
				StyleSettingsSlider(volume);
				volume.DragEnded += _ => { FlushSettingsSave(); if (name != "音楽") Sound("ui.click"); };
				Label number = Text(line, Math.Round(current * 100f) + "%", 20);
				number.CustomMinimumSize = new Vector2(68f, 0f);
				volume.ValueChanged += value => { setter((float)value); number.Text = Math.Round(value * 100.0) + "%"; ApplyAudio(); QueueSettingsSave(); };
			}
		});
	}

	private void StylePaperTextInput(Control edit)
	{
		edit.Theme = SourceRunUiDecor.PaperInputs(RunUnit, FontSize(20));
	}

	private Button SettingsToggle(Control parent, string label, bool current, Action<bool> setter, string explanation)
	{
		HBoxContainer row = Row(parent);
		Button toggle = null;
		string Caption(bool on) => label + (on ? "：ON（有効）" : "：OFF（無効）");
		toggle = Button(row, Caption(current), () =>
		{
			bool on = toggle.ButtonPressed;
			toggle.Text = Caption(on);
			setter(on);
		});
		toggle.AddThemeColorOverride("font_hover_pressed_color", SourcePalette.Ink);
		toggle.AddThemeStyleboxOverride("hover_pressed", SourceRunUiDecor.Paper(SourcePalette.Paper2, RunUnit, 10, true));
		toggle.ToggleMode = true;
		toggle.ButtonPressed = current;
		toggle.CustomMinimumSize = new Vector2(0, 64f);
		toggle.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		toggle.Alignment = HorizontalAlignment.Left;
		Text(parent, explanation, 17, Muted);
		return toggle;
	}

	private static Texture2D _settingsSliderKnob;
	private static Texture2D SettingsSliderKnob()
	{
		if (_settingsSliderKnob != null && GodotObject.IsInstanceValid(_settingsSliderKnob)) return _settingsSliderKnob;
		using Image image = Image.CreateEmpty(30, 30, false, Image.Format.Rgba8);
		image.Fill(Colors.Transparent);
		for (int y = 0; y < 30; y++) for (int x = 0; x < 30; x++)
		{
			float distance = new Vector2(x - 14.5f, y - 14.5f).Length();
			if (distance <= 14) image.SetPixel(x, y, distance > 10.5f ? new Color("3d3430") : new Color("d4b260"));
		}
		_settingsSliderKnob = ImageTexture.CreateFromImage(image);
		return _settingsSliderKnob;
	}

	private static void StyleSettingsSlider(HSlider slider)
	{
		StyleBoxFlat Track(Color color) => new StyleBoxFlat { BgColor = color, CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4, ContentMarginTop = 4, ContentMarginBottom = 4 };
		slider.AddThemeStyleboxOverride("slider", Track(new Color("b4a98a")));
		slider.AddThemeStyleboxOverride("grabber_area", Track(new Color("66522d")));
		slider.AddThemeStyleboxOverride("grabber_area_highlight", Track(new Color("8b692a")));
		slider.AddThemeIconOverride("grabber", SettingsSliderKnob());
		slider.AddThemeIconOverride("grabber_highlight", SettingsSliderKnob());
		slider.AddThemeIconOverride("grabber_disabled", SettingsSliderKnob());
	}

	private void ApplyTheme()
	{
		Theme theme = new Theme
		{
			DefaultFontSize = FontSize(19)
		};
		if (ResourceLoader.Exists("res://assets/Fonts/NotoSansJP-Regular.otf"))
		{
			theme.DefaultFont = GD.Load<Font>("res://assets/Fonts/NotoSansJP-Regular.otf");
		}
		SourceRunUiDecor.ApplyFonts(theme);
		theme.SetColor("font_color", "Label", Ink);
		theme.SetColor("font_color", "Button", Ink);
		theme.SetColor("font_disabled_color", "Button", new Color("6d7a83"));
		theme.SetStylebox("normal", "Button", Box(SourcePalette.NightPill, Border));
		theme.SetStylebox("hover", "Button", Box(SourcePalette.Window.Lightened(0.15f), Accent));
		theme.SetStylebox("pressed", "Button", Box(SourcePalette.ManaInk, Gold));
		theme.SetStylebox("focus", "Button", Box(new Color(0f, 0f, 0f, 0f), Gold, 3));
		theme.SetStylebox("disabled", "Button", Box(new Color("14232c"), new Color("283c46")));
		theme.SetStylebox("panel", "PanelContainer", Box(Panel, Border));
		Theme = theme;
	}

	private static StyleBoxFlat Box(Color color, Color border, int width = 1)
	{
		return new StyleBoxFlat
		{
			BgColor = color,
			BorderColor = border,
			BorderWidthLeft = width,
			BorderWidthRight = width,
			BorderWidthTop = width,
			BorderWidthBottom = width,
			CornerRadiusTopLeft = 9,
			CornerRadiusTopRight = 9,
			CornerRadiusBottomLeft = 9,
			CornerRadiusBottomRight = 9,
			ContentMarginLeft = 13f,
			ContentMarginRight = 13f,
			ContentMarginTop = 9f,
			ContentMarginBottom = 9f
		};
	}

	private static string Plain(string text)
	{
		return Regex.Replace(text ?? "", "<[^>]+>", "");
	}

	private void SizeInteractiveControls(Node node)
	{
		if (node is ScrollContainer scroll)
		{
			scroll.FollowFocus = scroll.Name != "SourceCardGridScroll" || _gridKeyboardNavigation;
		}
		if (node is BaseButton button && !(button is CardTile) && !(button is EnemyDropTarget))
		{
			button.CustomMinimumSize = new Vector2(Math.Max(button.CustomMinimumSize.X, MinimumHit), Math.Max(button.CustomMinimumSize.Y, MinimumHit));
		}
		else if (node is LineEdit || node is Slider || node is OptionButton)
		{
			Control control = (Control)node;
			control.CustomMinimumSize = new Vector2(control.CustomMinimumSize.X, Math.Max(control.CustomMinimumSize.Y, MinimumHit));
		}
		foreach (Node child in node.GetChildren())
		{
			SizeInteractiveControls(child);
		}
	}

	private static HBoxContainer Row(Node parent)
	{
		HBoxContainer row = new HBoxContainer
		{
			MouseFilter = MouseFilterEnum.Ignore
		};
		row.AddThemeConstantOverride("separation", 12);
		parent.AddChild(row, forceReadableName: false, InternalMode.Disabled);
		return row;
	}

	private static VBoxContainer Column(Node parent)
	{
		VBoxContainer col = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			MouseFilter = MouseFilterEnum.Ignore
		};
		col.AddThemeConstantOverride("separation", 10);
		parent.AddChild(col, forceReadableName: false, InternalMode.Disabled);
		return col;
	}

	private static VBoxContainer ScrollColumn(Node parent)
	{
		ScrollContainer scroll = new ScrollContainer
		{
			SizeFlagsVertical = SizeFlags.ExpandFill,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			// Auto isolates child minimum widths from the viewport and keeps any
            // exceptional overflow reachable instead of expanding the root offscreen.
            HorizontalScrollMode = ScrollContainer.ScrollMode.Auto
		};
		parent.AddChild(scroll, forceReadableName: false, InternalMode.Disabled);
		return Column(scroll);
	}

	private static HFlowContainer Flow(Node parent)
	{
		HFlowContainer flow = new HFlowContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		flow.AddThemeConstantOverride("h_separation", 12);
		flow.AddThemeConstantOverride("v_separation", 12);
		parent.AddChild(flow, forceReadableName: false, InternalMode.Disabled);
		return flow;
	}

	private void Picture(Node parent, string group, string id, Vector2 size)
	{
		Texture2D texture = Art(group, id);
		if (texture != null)
		{
			TextureRect image = new TextureRect
			{
				TextureFilter = TextureFilterEnum.Nearest,
				Texture = texture,
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				CustomMinimumSize = size,
				MouseFilter = MouseFilterEnum.Ignore
			};
			parent.AddChild(image, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private VBoxContainer Tile(Node parent, string name, string description, string? group = null, string? id = null)
	{
		PanelContainer pan = new PanelContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		parent.AddChild(pan, forceReadableName: false, InternalMode.Disabled);
		VBoxContainer col = Column(pan);
		if (group != null && id != null)
		{
			Picture(col, group, id, new Vector2(80f, 70f));
		}
		SectionHeading(col, name, 23, Gold);
		Text(col, description, 18);
		return col;
	}

	private static Color CardColor(string color)
	{
		return SourcePalette.Edge(color);
	}

	private CardTile Card(Node parent, CardInstance card, Action? action, bool enabled = true, string? badge = null, bool drag = false, int? cost = null)
	{
		return SourceCard(parent, card, card, action, enabled, badge, PhoneLayout ? 1.05f : 1.02f, SourceInspectState, drag, cost);
	}

	private void InspectCard(CardInstance card)
	{
		SourceInspectCard(card);
	}

	public void SetEnemyProjection(Func<int, (Vector2 Head, Vector2 Feet)?> project)
	{
		_enemyProjection = project;
	}

    private Action<Rect2>? _enemyRosterLane;
    public void SetEnemyRosterLane(Action<Rect2> apply) { _enemyRosterLane = apply; }

	private string EnemyVitalsText(int hp, int maxHp, int block)
	{
		if (_enemyProjection != null)
		{
			return $"{hp}/{maxHp}  盾{block}";
		}
		return $"HP {hp}/{maxHp}  盾 {block}";
	}

	private PanelContainer EnemySurface(Node parent, int index, string kind, Color border)
	{
		PanelContainer panel = new PanelContainer
		{
			Name = ((kind == "intent") ? "EnemyIntentSurface_" : "EnemyLedgerSurface_") + index,
			MouseFilter = MouseFilterEnum.Ignore
		};
		StyleBoxFlat style = Box(new Color("1a1c33ed"), border, (index != _preferredTarget) ? 1 : 2);
		style.ContentMarginLeft = 8f;
		style.ContentMarginRight = 8f;
		style.ContentMarginTop = 2f;
		style.ContentMarginBottom = 2f;
		panel.AddThemeStyleboxOverride("panel", style);
		panel.SetMeta("enemy_hud_surface", kind);
		panel.SetMeta("enemy_index", index);
		parent.AddChild(panel, forceReadableName: false, InternalMode.Disabled);
		return panel;
	}

	private void BuildProjectedEnemyHud(Node parent, GameState state)
	{
		for (int i = 0; i < state.Enemies.Count; i++)
		{
			int index = i;
			EnemyState enemy = state.Enemies[i];
			EnemyDef def = Content.GetEnemyDef(enemy.EnemyId);
			EnemyDropTarget hit = new EnemyDropTarget
			{
				Name = "EnemyTarget_" + i,
				Text = "",
				Disabled = (enemy.Hp <= 0 || IsInputBlocked),
				IsBlocked = () => IsInputBlocked,
				CustomMinimumSize = new Vector2(MinimumHit, MinimumHit),
				TooltipText = Plain(def.Name + "\n" + CardText.IntentText(state, i) + "\n" + CardText.EnemyTraits(def, state, i) + "\n" + EnemyStatus(enemy))
			};
			string[] array = new string[5] { "normal", "hover", "pressed", "disabled", "focus" };
			foreach (string style in array)
			{
				hit.AddThemeStyleboxOverride(style, new StyleBoxEmpty());
			}
			parent.AddChild(hit, forceReadableName: false, InternalMode.Disabled);
			Color border = ((index == _preferredTarget) ? Gold : Border);
			PanelContainer intent = EnemySurface(hit, index, "intent", border);
			Label intentText = Text(intent, (state.HideIntents == true) ? "意図は隠されています" : CardText.IntentText(state, index), 17, Danger);
			intentText.MaxLinesVisible = 1;
			intentText.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
			_enemyIntentReadouts[index] = intentText;
			PanelContainer ledger = EnemySurface(hit, index, "ledger", border);
			VBoxContainer col = Column(ledger);
			col.AddThemeConstantOverride("separation", 0);
			Label label = Text(col, $"{i + 1}. {def.Name}" + ((enemy.Hp <= 0) ? " 撃破" : ""), 20, (index == _preferredTarget) ? Gold : Ink);
			label.MaxLinesVisible = 1;
			label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
			Label hp = Text(col, EnemyVitalsText(enemy.Hp, enemy.MaxHp, enemy.Block), 18, Accent);
			hp.MaxLinesVisible = 1;
			_enemyReadouts[index] = hp;
			hit.Inspect = () =>
			{
				InspectEnemy(index);
			};
			hit.Pressed += () =>
			{
				if (!hit.ConsumeInspection())
				{
					Sound("ui.click");
					SelectEnemy(index);
				}
			};
			hit.CardDropped = (string uid) =>
			{
				CardInstance cardInstance = state.Player.Hand.FirstOrDefault((CardInstance c) => c.Uid == uid);
				if (cardInstance != null)
				{
					_preferredTarget = index;
					BeginPlay(cardInstance, index);
				}
			};
			_enemyHud.Add(new EnemyHud(index, hit, intent, ledger));
		}
	}

	public override void _Process(double delta)
	{
		try
		{
			if (_settingsSaveDelay > 0)
			{
				_settingsSaveDelay -= delta;
				if (_settingsSaveDelay <= 0) FlushSettingsSave();
			}
			UxProcess(delta);
		}
		catch (Exception error)
		{
			try { _session?.RecordFreezeEvent("GameUI.Process", false, error); } catch { }
			throw;
		}
	}

	private void PositionEnemyHud()
	{
		if (_enemyProjection == null || _handScroll == null || !GodotObject.IsInstanceValid(_handScroll) || _enemyHud.Count == 0)
		{
			return;
		}
		(EnemyHud, (Vector2, Vector2)?)[] projected = _enemyHud.Select((EnemyHud enemyHud) => ((EnemyHud Hud, (Vector2 Head, Vector2 Feet)? Point))(Hud: enemyHud, Point: _enemyProjection(enemyHud.Index))).Where(((EnemyHud Hud, (Vector2 Head, Vector2 Feet)? Point) tuple) =>
		{
			(EnemyHud, (Vector2, Vector2)?) tuple2 = tuple;
			return tuple2.Item2.HasValue;
		}).ToArray();
		float ledgerBottom = _handScroll.GlobalPosition.Y - 2f;
		(EnemyHud, (Vector2, Vector2)?)[] array = projected;
		for (int num = 0; num < array.Length; num++)
		{
			(EnemyHud, (Vector2, Vector2)?) entry = array[num];
			EnemyHud h = entry.Item1;
			(Vector2 Head, Vector2 Feet) point = entry.Item2.Value;
			if (GodotObject.IsInstanceValid(h.Hit))
			{
				float width = Math.Clamp(projected.Where(((EnemyHud Hud, (Vector2 Head, Vector2 Feet)? Point) tuple) => tuple.Hud.Index != h.Index).Select(((EnemyHud Hud, (Vector2 Head, Vector2 Feet)? Point) tuple) =>
				{
					(EnemyHud, (Vector2, Vector2)?) tuple2 = tuple;
					return Math.Abs(tuple2.Item2.Value.Item2.X - point.Feet.X);
				}).DefaultIfEmpty(320f)
					.Min() - 10f, MinimumHit, PhoneLayout ? 290 : 300);
				float ledgerHeight = Math.Max(h.Ledger.GetCombinedMinimumSize().Y, Mathf.Ceil((float)(FontSize(20) + FontSize(18)) * 1.5f) + 4f);
				float intentHeight = Math.Max(h.Intent.GetCombinedMinimumSize().Y, Mathf.Ceil((float)FontSize(17) * 1.5f) + 4f);
				float x = Math.Clamp(point.Feet.X - width / 2f, 16f, Math.Max(16f, Size.X - width - 16f));
				float top = Math.Max(((_shell.GetChildCount() > 0 && _shell.GetChild(0) is Control header) ? header.GetGlobalRect().End.Y : (_shell.GlobalPosition.Y + MinimumHit)) + 6f, point.Head.Y - intentHeight - 8f);
				float ledgerY = ledgerBottom - ledgerHeight;
				h.Hit.GlobalPosition = new Vector2(x, top);
				h.Hit.Size = new Vector2(width, Math.Max(MinimumHit, ledgerBottom - top));
				h.Intent.Position = Vector2.Zero;
				h.Intent.Size = new Vector2(width, intentHeight);
				h.Ledger.GlobalPosition = new Vector2(x, ledgerY);
				h.Ledger.Size = new Vector2(width, ledgerHeight);
			}
		}
	}

	private void OpenNote()
	{
		if (_session == null)
		{
			return;
		}
		Open("プレイメモ", (VBoxContainer box) =>
		{
			Text(box, "記録時の幕・部屋・ターンを自動で添え、セーブとレポートに保存します。", 19, Muted);
			TextEdit edit = new TextEdit
			{
				Text = _noteDraft,
				PlaceholderText = "気づいたことをここに",
				CustomMinimumSize = new Vector2(0f, 140f),
				SizeFlagsVertical = SizeFlags.ExpandFill,
				WrapMode = TextEdit.LineWrappingMode.Boundary
			};
			edit.TextChanged += () =>
			{
				_noteDraft = edit.Text;
			};
			box.AddChild(edit, forceReadableName: false, InternalMode.Disabled);
			Button(box, "メモを保存", () =>
			{
				SessionResult sessionResult = _session.AddNote(_noteDraft);
				if (sessionResult.Success)
				{
					_noteDraft = "";
					_notice = sessionResult.Warning ?? "メモを保存しました";
					_error = sessionResult.Warning != null;
					CloseOverlay();
				}
				else
				{
					Result(sessionResult);
				}
			}, enabled: true, Gold);
			VBoxContainer parent = ScrollColumn(box);
			foreach (PlayNote current in _session.Notes)
			{
				Text(parent, current.Context + " · " + current.At, 14, Muted);
				Text(parent, current.Text, 18);
			}
		});
	}

	private void OpenRating()
	{
		if (_session == null || _session.History.Count == 0)
		{
			return;
		}
		BattleArchive last = _session.History[_session.History.Count - 1];
		BattleRating draft = last.Rating?.Clone() ?? new BattleRating();
		bool lost = last.Result == "lost";
		Open("直前の戦闘を評価", (VBoxContainer box) =>
		{
			Text(box, $"第{last.BattleNo}戦 · {last.Turns}ターン · HP {last.HpBefore} → {last.HpAfter}", 21, Gold);
			Score("敵の強さ (1=弱い / 5=強い)", draft.Strength, (int value) =>
			{
				draft.Strength = value;
			});
			Score("面白さ (1=退屈 / 5=最高)", draft.Fun, (int value) =>
			{
				draft.Fun = value;
			});
			if (lost)
			{
				HBoxContainer parent = Row(box);
				Text(parent, "敗北の感触", 21);
				Button(parent, ((draft.LossFeel == "build") ? "● " : "") + "構築の失敗", () =>
				{
					draft.LossFeel = "build";
					Rebuild();
				});
				Button(parent, ((draft.LossFeel == "unfair") ? "● " : "") + "理不尽", () =>
				{
					draft.LossFeel = "unfair";
					Rebuild();
				});
			}
			TextEdit note = new TextEdit
			{
				Text = (draft.Note ?? ""),
				PlaceholderText = "ひとことメモ (任意)",
				CustomMinimumSize = new Vector2(0f, 120f),
				SizeFlagsVertical = SizeFlags.ExpandFill,
				WrapMode = TextEdit.LineWrappingMode.Boundary
			};
			box.AddChild(note, forceReadableName: false, InternalMode.Disabled);
			note.TextChanged += () =>
			{
				draft.Note = note.Text;
			};
			Button(box, "評価を保存", () =>
			{
				SessionResult sessionResult = _session.RateLastBattle(draft);
				if (sessionResult.Success)
				{
					_notice = sessionResult.Warning ?? "評価を保存しました";
					_error = sessionResult.Warning != null;
					CloseOverlay();
				}
				else
				{
					Result(sessionResult);
				}
			}, draft.Strength.HasValue && draft.Fun.HasValue && (!lost || draft.LossFeel != null), Gold);
			Button(box, "スキップ", CloseOverlay);
			void Score(string title, int? current, Action<int> set)
			{
				HBoxContainer row = Row(box);
				Text(row, title, 21);
				for (int i = 1; i <= 5; i++)
				{
					int value = i;
					Button(row, ((current == value) ? "● " : "") + value, () =>
					{
						set(value);
						Rebuild();
					});
				}
			}
		});
	}

	public IReadOnlyList<Rect2> GetPaintedCombatBounds()
	{
		List<Rect2> result = new List<Rect2>();
		if (CurrentRoute != "combat" || _shell == null || !GodotObject.IsInstanceValid(_shell) || !_shell.IsVisibleInTree())
		{
			return result;
		}
		Visit(_shell);
		return result;
		void Visit(Node node)
		{
			if (node is Control control)
			{
				if (!control.IsVisibleInTree())
				{
					return;
				}
				if (control is CardTile { IsBattleHand: not false } || control is SourceHealthBar || control is SourceEnergyOrb)
				{
					goto IL_010c;
				}
				if (!(control is EnemyDropTarget) && !(control is BattlefieldDropTarget))
				{
					bool num;
					if (!(control is PanelContainer))
					{
						if (!(control is BaseButton button))
						{
							if (!(control is Label label))
							{
								if (!(control is RichTextLabel text))
								{
									goto IL_01c2;
								}
								num = !string.IsNullOrEmpty(text.Text);
							}
							else
							{
								num = !string.IsNullOrEmpty(label.Text);
							}
						}
						else
						{
							Control control2 = control;
							string text2;
							if (button.Disabled)
							{
								text2 = "disabled";
							}
							else if (button.IsPressed())
							{
								text2 = "pressed";
							}
							else
							{
								text2 = (button.IsHovered() ? "hover" : "normal");
							}
							num = Paints(control2.GetThemeStylebox(text2));
						}
					}
					else
					{
						num = Paints(control.GetThemeStylebox("panel"));
					}
					if (num)
					{
						goto IL_010c;
					}
				}
			}
			goto IL_01c2;
			IL_01c2:
			foreach (Node child in node.GetChildren())
			{
				Visit(child);
			}
			return;
			IL_010c:
			if (control.SelfModulate.A > 0f && VisibleAlpha(control) > 0f)
			{
				Rect2 visible = ViewportBounds(control).Intersection(GetViewportRect());
				for (Node parent = control.GetParent(); parent != null; parent = parent.GetParent())
				{
					if (parent is Control clip && (clip.ClipContents || clip is ScrollContainer))
					{
						visible = visible.Intersection(ViewportBounds(clip));
					}
				}
				if (visible.Size.X > 0f && visible.Size.Y > 0f)
				{
					result.Add(visible);
				}
				return;
			}
			goto IL_01c2;
		}
	}

	private static bool Paints(StyleBox style)
	{
		if (!(style is StyleBoxEmpty))
		{
			if (style is StyleBoxFlat flat && !(flat.BgColor.A > 0f))
			{
				if (flat.BorderColor.A > 0f)
				{
					if (flat.BorderWidthLeft <= 0 && flat.BorderWidthRight <= 0 && flat.BorderWidthTop <= 0)
					{
						return flat.BorderWidthBottom > 0;
					}
					return true;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	private static float VisibleAlpha(CanvasItem item)
	{
		float alpha = item.Modulate.A;
		for (Node parent = item.GetParent(); parent != null; parent = parent.GetParent())
		{
			if (parent is CanvasItem canvas)
			{
				alpha *= canvas.Modulate.A;
			}
		}
		return alpha;
	}

	private static Rect2 ViewportBounds(Control control)
	{
		Transform2D globalTransformWithCanvas = control.GetGlobalTransformWithCanvas();
		Vector2 a = globalTransformWithCanvas * Vector2.Zero;
		Vector2 b = globalTransformWithCanvas * new Vector2(control.Size.X, 0f);
		Vector2 c = globalTransformWithCanvas * control.Size;
		Vector2 d = globalTransformWithCanvas * new Vector2(0f, control.Size.Y);
		Vector2 min = new Vector2(Math.Min(Math.Min(a.X, b.X), Math.Min(c.X, d.X)), Math.Min(Math.Min(a.Y, b.Y), Math.Min(c.Y, d.Y)));
		Vector2 max = new Vector2(Math.Max(Math.Max(a.X, b.X), Math.Max(c.X, d.X)), Math.Max(Math.Max(a.Y, b.Y), Math.Max(c.Y, d.Y)));
		return new Rect2(min, max - min);
	}

	public void SetReceiptValidator(Func<GameEvent, CombatPresentationReceipt, GameState?, bool> validator)
	{
		_receiptValidator = validator;
	}

	public void SetPresentationProjection(Func<GameEvent, CombatPresentationVitals?> vitals, Func<GameState?> state)
	{
		_presentedVitals = vitals;
		_presentedState = state;
	}

	private void PresentEventLegacy(GameEvent evt)
	{
		GameState state = _view?.Combat;
		if ((object)state == null)
		{
			return;
		}
		PlayerState player = state.Player;
		CombatPresentationReceipt receipt = evt.GetType().GetProperty("PresentationReceipt")?.GetValue(evt) as CombatPresentationReceipt;
		if (receipt != null && !ValidateDisplayReceipt(evt, receipt, state))
		{
			RejectedReceiptCount++;
			receipt = null;
		}
		if (receipt != null)
		{
			CombatPresentationVitals after = _presentedVitals?.Invoke(evt) ?? receipt.After;
			if (receipt.Target == "player")
			{
				player = player with
				{
					Hp = after.Hp,
					MaxHp = after.MaxHp,
					Block = after.Block,
					IceBlock = (after.IceBlock ?? player.IceBlock)
				};
			}
			else
			{
				int? enemyIndex = receipt.EnemyIndex;
				if (enemyIndex.HasValue)
				{
					int index = enemyIndex.GetValueOrDefault();
					if (index >= 0 && index < state.Enemies.Count)
					{
						EnemyState[] enemies = state.Enemies.ToArray();
						enemies[index] = enemies[index]with
						{
							Hp = after.Hp,
							MaxHp = after.MaxHp,
							Block = after.Block
						};
						state = state with
						{
							Enemies = enemies
						};
					}
				}
			}
		}
		if (!(evt is GameEvent_Cascaded e))
		{
			if (!(evt is GameEvent_EnergyGained e2))
			{
				if (!(evt is GameEvent_EnergyMaxGained e3))
				{
					if (!(evt is GameEvent_GrowthAdded e4))
					{
						if (!(evt is GameEvent_GrowthDischarged e5))
						{
							if (!(evt is GameEvent_MomentumAdded e6))
							{
								if (!(evt is GameEvent_MomentumDischarged e7))
								{
									if (!(evt is GameEvent_AetherGained e8))
									{
										if (!(evt is GameEvent_AetherDischarged e9))
										{
											if (!(evt is GameEvent_LightGained e10))
											{
												if (!(evt is GameEvent_LightSpent e11))
												{
													if (!(evt is GameEvent_LightDischarged e12))
													{
														if (evt is GameEvent_DiscountGained e13)
														{
															player = player with
															{
																NextCardDiscount = player.NextCardDiscount + e13.Amount
															};
														}
													}
													else
													{
														player = player with
														{
															Light = Math.Max(0, player.Light.GetValueOrDefault() - e12.Spent)
														};
													}
												}
												else
												{
													player = player with
													{
														Light = Math.Max(0, player.Light.GetValueOrDefault() - e11.Amount)
													};
												}
											}
											else
											{
												player = player with
												{
													Light = player.Light.GetValueOrDefault() + e10.Amount
												};
											}
										}
										else
										{
											player = player with
											{
												Aether = Math.Max(0, player.Aether - e9.Spent)
											};
										}
									}
									else
									{
										player = player with
										{
											Aether = player.Aether + e8.Amount
										};
									}
								}
								else
								{
									player = player with
									{
										Momentum = Math.Max(0, player.Momentum - e7.Spent)
									};
								}
							}
							else
							{
								player = player with
								{
									Momentum = player.Momentum + e6.Amount
								};
							}
						}
						else
						{
							player = player with
							{
								Growth = Math.Max(0, player.Growth - e5.Spent)
							};
						}
					}
					else
					{
						player = player with
						{
							Growth = player.Growth + e4.Amount
						};
					}
				}
				else
				{
					player = player with
					{
						EnergyMax = player.EnergyMax + e3.Amount
					};
				}
			}
			else
			{
				player = player with
				{
					Energy = player.Energy + e2.Amount
				};
			}
		}
		else if (e.HitId != null)
		{
			player = player with
			{
				CascadesThisCombat = player.CascadesThisCombat.GetValueOrDefault() + 1
			};
		}
		int cost;
		bool paid;
		if (!_costPresented)
		{
			GameState before = _activeChange?.Previous?.Combat;
			if ((object)before != null && _activeChange.Command is RunCommand_Combat combat)
			{
				cost = 0;
				paid = false;
				if (evt is GameEvent_CardPlayed)
				{
					Command command = combat.Command;
					Command_PlayCard play = command as Command_PlayCard;
					if ((object)play != null)
					{
						CardInstance card = before.Player.Hand.FirstOrDefault((CardInstance c) => c.Uid == play.CardUid);
						if (card != null)
						{
							cost = Effects.EffectiveCost(before, card);
							paid = true;
						}
						goto IL_0657;
					}
				}
				if (evt is GameEvent_CardSet)
				{
					Command command2 = combat.Command;
					Command_SetCard set = command2 as Command_SetCard;
					if ((object)set != null)
					{
						CardInstance card2 = before.Player.Hand.FirstOrDefault((CardInstance c) => c.Uid == set.CardUid);
						if (card2 != null)
						{
							cost = ((!(card2.Def.Type == "reaction")) ? 1 : ((card2.FreeThisCombat != true) ? card2.Def.Cost : 0));
							paid = true;
						}
						goto IL_0657;
					}
				}
				if (evt is GameEvent_CardPlayedFromExhaust || evt.Type == "NecroPlayed")
				{
					Command command3 = combat.Command;
					Command_PlayNecro necro = command3 as Command_PlayNecro;
					if ((object)necro != null)
					{
						CardInstance card3 = before.Player.ExhaustPile.FirstOrDefault((CardInstance c) => c.Uid == necro.CardUid);
						if (card3 != null)
						{
							cost = card3.Def.NecroCost.GetValueOrDefault();
							paid = true;
						}
					}
				}
				goto IL_0657;
			}
		}
		goto IL_067e;
		IL_0657:
		if (paid)
		{
			player = player with
			{
				Energy = Math.Max(0, player.Energy - cost)
			};
			_costPresented = true;
		}
		goto IL_067e;
		IL_067e:
		GameState projection = _presentedState?.Invoke();
		if ((object)projection != null)
		{
			player = player with
			{
				Permanents = projection.Player.Permanents
			};
			if (evt is GameEvent_TurnStarted)
			{
				player = player with
				{
					Block = projection.Player.Block,
					IceBlock = projection.Player.IceBlock
				};
			}
			state = state with
			{
				Enemies = projection.Enemies
			};
		}
		state = state with
		{
			Player = player
		};
		_view = _view with
		{
			Combat = state
		};
		Set(_hpReadout, $"HP {player.Hp}/{player.MaxHp}");
		Set(_energyReadout, $"エナジー {player.Energy}/{player.EnergyMax}");
		Set(_blockReadout, $"ブロック {player.Block}  氷壁 {player.IceBlock}");
		Set(_statusReadout, PlayerStatus(state));
		foreach (KeyValuePair<int, Label> pair in _enemyReadouts)
		{
			if (pair.Key < state.Enemies.Count)
			{
				EnemyState enemy = state.Enemies[pair.Key];
				Set(pair.Value, EnemyVitalsText(enemy.Hp, enemy.MaxHp, enemy.Block));
			}
		}
		foreach (KeyValuePair<int, Label> pair2 in _enemyIntentReadouts)
		{
			if (pair2.Key < state.Enemies.Count)
			{
				Set(pair2.Value, (state.HideIntents == true) ? "意図は隠されています" : CardText.IntentText(state, pair2.Key));
			}
		}
	}

	private bool ValidateDisplayReceipt(GameEvent evt, CombatPresentationReceipt receipt, GameState state)
	{
		if (_receiptValidator != null)
		{
			return _receiptValidator(evt, receipt, state);
		}
		return BattleStage.ValidReceipt(evt, receipt, state);
	}

	private static void Set(Label? label, string text)
	{
		if (label != null && GodotObject.IsInstanceValid(label))
		{
			label.Text = text;
			if (label is SourceHudLabel source)
			{
				source.RefreshSourceAlignment();
			}
		}
	}

	public void SetEnemyResolver(Func<Vector2, int?> resolver)
	{
		_enemyResolver = resolver;
	}

	public bool CanDropHandCard(string uid)
	{
		if (!IsInputBlocked && _overlay == null)
		{
			GameState state = InputCombat;
			if ((object)state != null)
			{
				foreach (CardInstance card in state.Player.Hand)
				{
					if (card.Uid == uid)
					{
						return FlowRules.PlayBlocked(state, card) == null;
					}
				}
				return false;
			}
		}
		return false;
	}

	public void DropHandCard(string uid, Vector2 viewportPoint)
	{
		if (viewportPoint.Y >= Size.Y * 0.64f)
		{
			EndHandDrag();
		}
		else
		{
			if (!CanDropHandCard(uid))
			{
				return;
			}
			GameState state = InputCombat;
			if ((object)state == null)
			{
				return;
			}
			foreach (CardInstance card in state.Player.Hand)
			{
				if (card.Uid == uid)
				{
					BeginPlay(card, _enemyResolver?.Invoke(viewportPoint));
					break;
				}
			}
		}
	}

	public void PlayAllSparks()
	{
		if (!IsInputBlocked && !_showTitle && _overlay == null && _play == null)
		{
			GameState state = _session?.CurrentRun?.Combat;
			if ((object)state != null && !(SourceUiPolicies.NextSpark(state) == null))
			{
				_sparksRemaining = 12;
				PumpSparks();
			}
		}
	}

	private void PumpSparks()
	{
		if (_pumpingSparks || _sparksRemaining == 0 || IsInputBlocked)
		{
			return;
		}
		_pumpingSparks = true;
		try
		{
			while (_sparksRemaining > 0 && !IsInputBlocked)
			{
				if (!_showTitle && _overlay == null)
				{
					RunState runState = _session?.CurrentRun;
					if ((object)runState != null && runState.Phase == "combat")
					{
						GameState state = runState.Combat;
						if ((object)state != null)
						{
							CardInstance spark = SourceUiPolicies.NextSpark(state);
							if ((object)spark != null)
							{
								_sparksRemaining--;
								SessionResult result = _session.Dispatch(new Command_PlayCard
								{
									CardUid = spark.Uid
								});
								if (!result.Success)
								{
									_sparksRemaining = 0;
								}
								Result(result);
								continue;
							}
						}
					}
				}
				_sparksRemaining = 0;
				break;
			}
		}
		finally
		{
			_pumpingSparks = false;
		}
	}

	private void ContinueFromTitle()
	{
		if (_session != null && !IsInputBlocked)
		{
			SessionContinuePreview preview = _session.PreviewContinue();
			if (!preview.Result.Success)
			{
				Result(preview.Result);
			}
			else if (preview.RequiresConfirmation)
			{
				Confirm("別のデータ版のセーブ", "このセーブは別のデータバージョンで作られています。カード・敵の定義が変わっていると正しく動かない可能性がありますが、読み込みますか？", Resume);
			}
			else
			{
				Resume();
			}
		}
		void Resume()
		{
			Sound("ui.start_run");
			Result(_session.Continue());
		}
	}

	private void Sound(string? key)
	{
		if (key != null)
		{
			UiSoundRequested?.Invoke(key);
		}
	}

	private void OfferQueuedRating()
	{
		if (_ratingQueued && !IsInputBlocked && !_showTitle && _overlay == null && !(_view == null) && SourceUiPolicies.RatingPhase(_view.Phase))
		{
			_ratingQueued = false;
			if ((_session?.History.Count ?? 0) > 0)
			{
				OpenRating();
			}
		}
	}

	private void InspectEnemy(int index)
	{
		GameState state = _view?.Combat;
		if (state == null || index < 0 || index >= state.Enemies.Count || IsInputBlocked)
		{
			return;
		}
		EnemyState enemy = state.Enemies[index];
		EnemyDef definition = Content.GetEnemyDef(enemy.EnemyId);
		Open(definition.Name, (VBoxContainer box) =>
		{
			VBoxContainer parent = ScrollColumn(box);
			Text(parent, $"HP {enemy.Hp}/{enemy.MaxHp}  ブロック {enemy.Block}", 25, Gold);
			if (enemy.Hp > 0)
			{
				Text(parent, "意図: " + CardText.IntentText(state, index), 23);
			}
			Text(parent, CardText.EnemyTraits(definition, state, index), 21);
			Text(parent, SourceEnemyLedgerModel.InspectionStatus(state, index, definition), 21, Muted);
			int num = FlowRules.GuardRedirect(state, index);
			if (num != index && num >= 0 && num < state.Enemies.Count)
			{
				Text(parent, Content.GetEnemyDef(state.Enemies[num].EnemyId).Name + "が庇っている（単体の札は守護者へ向かう）", 21, Gold);
			}
		});
	}

	private void PresentNumericContactLegacy(GameEvent evt)
	{
		GameState state = _view?.Combat;
		if ((object)state == null)
		{
			return;
		}
		CombatPresentationVitals after = _presentedVitals?.Invoke(evt);
		if ((object)after == null)
		{
			return;
		}
		CombatPresentationReceipt receipt = evt.GetType().GetProperty("PresentationReceipt")?.GetValue(evt) as CombatPresentationReceipt;
		if (receipt == null || !ValidateDisplayReceipt(evt, receipt, state))
		{
			return;
		}
		if (receipt.Target == "player")
		{
			PlayerState player = state.Player with
			{
				Hp = after.Hp,
				MaxHp = after.MaxHp,
				Block = after.Block,
				IceBlock = (after.IceBlock ?? state.Player.IceBlock)
			};
			state = state with
			{
				Player = player
			};
			Set(_hpReadout, $"HP {player.Hp}/{player.MaxHp}");
			Set(_blockReadout, $"ブロック {player.Block}  氷壁 {player.IceBlock}");
		}
		else
		{
			int? enemyIndex = receipt.EnemyIndex;
			if (enemyIndex.HasValue)
			{
				int i = enemyIndex.GetValueOrDefault();
				if (i >= 0 && i < state.Enemies.Count)
				{
					List<EnemyState> enemies = new List<EnemyState>(state.Enemies);
					enemies[i] = enemies[i]with
					{
						Hp = after.Hp,
						MaxHp = after.MaxHp,
						Block = after.Block
					};
					state = state with
					{
						Enemies = enemies
					};
					if (_enemyReadouts.TryGetValue(i, out var label))
					{
						Set(label, EnemyVitalsText(after.Hp, after.MaxHp, after.Block));
					}
				}
			}
		}
		_view = _view with
		{
			Combat = state
		};
		PresentedNumericContactCount++;
	}

	private static void DisableUnderlyingFocus(Node node)
	{
		if (node is Control control)
		{
			control.FocusMode = FocusModeEnum.None;
		}
		foreach (Node child in node.GetChildren())
		{
			DisableUnderlyingFocus(child);
		}
	}

	private static Control? FirstFocusable(Node node)
	{
		if (node is Control control && control.FocusMode == FocusModeEnum.All && !(control is BaseButton { Disabled: not false }))
		{
			return control;
		}
		foreach (Node child in node.GetChildren())
		{
			Control found = FirstFocusable(child);
			if (found != null)
			{
				return found;
			}
		}
		return null;
	}

	private void ShowTitle()
	{
		if (_session != null && !IsInputBlocked)
		{
			SessionResult result = _session.ReturnToTitle();
			if (!result.Success)
			{
				Result(result);
				return;
			}
			_view = _session.CurrentRun;
			_showTitle = true;
			_overlay = null;
			_overlayStack.Clear();
			_play = null;
			_ratingQueued = false;
			_sparksRemaining = 0;
			_notice = result.Warning ?? "";
			_error = result.Warning != null;
			Rebuild();
		}
	}

	private void SharePair(string report, string save)
	{
		_shareAwaiting = true;
		bool queued = (_shareAwaiting = AndroidReportShare.TryShare(report, save, "deckrogue-report", out var message));
		_notice = (queued ? "共有先の選択画面を開くよう依頼しました。送信先は自分で選んでください。" : ("共有画面を開けません: " + message));
		_error = !queued;
		Rebuild();
	}

	private void ShareRequestFinished(bool opened, string message)
	{
		if (!_shareAwaiting)
		{
			return;
		}
		_shareAwaiting = false;
		Callable.From(() =>
		{
			if (IsInsideTree())
			{
				_notice = (opened ? "共有先の選択画面を開きました。送信の完了はここでは確認できません。" : ("共有画面を開けません: " + message));
				_error = !opened;
				Rebuild();
			}
		}).CallDeferred();
	}

	private void ResolveChoice(RunState run, EventChoiceDef choice, string title, Action<int?> submit)
	{
		if (!Run.EventChoiceNeedsCard(choice))
		{
			submit(null);
			return;
		}
		SourceUiDecision decision = new SourceUiDecision(_session?.Revision ?? 0);
		Open(title + " · 札を1枚選ぶ", (VBoxContainer box) =>
		{
			Text(box, SourceDecisionInfo.CardPickHint(choice), 20, Muted);
			CardGrid(box, run.Deck, (CardInstance card) =>
			{
				int num = run.Deck.ToList().FindIndex((CardInstance c) => c.Uid == card.Uid);
				if (num >= 0 && FlowRules.ChoiceCardAllowed(run, choice, card) && decision.TryAccept(_session?.Revision ?? 0, IsInputBlocked))
				{
					submit(num);
				}
			}, (CardInstance card) => FlowRules.ChoiceCardAllowed(run, choice, card), (CardInstance card) => SourceDecisionInfo.CardLockReason(run, choice, card) ?? SourceDecisionInfo.CardVerb(choice), confirmPick: true, SourceDecisionInfo.CardVerb(choice), (CardInstance card) => SourceDecisionInfo.CardLockReason(run, choice, card));
		});
	}

	private void ChooseDeckCard(RunState run, string title, Func<CardInstance, bool> allowed, Action<int> submit, bool upgrade = false)
	{
		SourceUiDecision decision = new SourceUiDecision(_session?.Revision ?? 0);
		Open(title + " · 札を1枚選ぶ", (VBoxContainer box) =>
		{
			CardGrid(box, run.Deck, (CardInstance card) =>
			{
				int num = run.Deck.ToList().FindIndex((CardInstance c) => c.Uid == card.Uid);
				if (num >= 0 && allowed(card) && decision.TryAccept(_session?.Revision ?? 0, IsInputBlocked))
				{
					submit(num);
				}
			}, allowed, (CardInstance card) =>
			{
				string result = ((!allowed(card)) ? (upgrade ? SourceCardModalPolicy.UpgradeState(card) : title == "取り除く" && run.Deck.Count <= 5 ? "デッキが5枚以下のため取り除けません" : "この操作の対象にできません") : title);
				return result;
			}, confirmPick: true, title);
		});
	}

	public IReadOnlyList<string> VisibleActions()
	{
		return (from b in Buttons(this)
			where b.IsVisibleInTree() && !b.Disabled
			select b.Text into s
			where s.Length > 0
			select s).ToArray();
	}

	public bool InvokeAction(string exactLabel)
	{
		if (IsInputBlocked)
		{
			return false;
		}
		Button[] matches = (from b in Buttons(_overlayNode ?? this)
			where b.IsVisibleInTree() && !b.Disabled && b.Text == exactLabel
			select b).ToArray();
		if (matches.Length != 1)
		{
			return false;
		}
		matches[0].EmitSignal(BaseButton.SignalName.Pressed);
		return true;
	}

	private static IEnumerable<Button> Buttons(Node node)
	{
		foreach (Node child in node.GetChildren())
		{
			if (child is Button button)
			{
				yield return button;
			}
			foreach (Button item in Buttons(child))
			{
				yield return item;
			}
		}
	}

	public void PresentEvent(GameEvent evt)
	{
		UxPresentEvent(evt);
	}

	public void PresentNumericContact(GameEvent evt)
	{
		UxNumericContact(evt);
	}

	public override void _Input(InputEvent evt)
	{
        if (HandleTouchHandInput(evt))
        {
            GetViewport().SetInputAsHandled();
            return;
        }
		ObserveGridNavigationInput(evt);
		UxObserveHandDragHoverInput(evt);
		if (DesktopVideoAvailable && evt is InputEventKey { Pressed: not false, Echo: false } key && (key.Keycode == Key.F11 || (key.AltPressed && (key.Keycode == Key.Enter || key.Keycode == Key.KpEnter))))
		{
			ToggleFullscreen();
			GetViewport().SetInputAsHandled();
		}
	}

	private bool UxDealIsCurrent(string uid, UxHandDeal deal)
	{
		if (deal.Epoch == _uxEpoch && !deal.Clock.Cancelled && _uxDeals.TryGetValue(uid, out var active) && active == deal && _uxTiles.TryGetValue(uid, out var tile) && tile == deal.Tile && GodotObject.IsInstanceValid(tile))
		{
			return !tile.IsQueuedForDeletion();
		}
		return false;
	}

	private bool UxRemovedPlayedCard(IEnumerable<string> removed)
	{
		SessionChange? activeChange = _activeChange;
		if (((object)activeChange == null || !activeChange.IsReset) && _activeChange?.Command is RunCommand_Combat { Command: Command_PlayCard play })
		{
			return removed.Contains(play.CardUid);
		}
		return false;
	}

	private Vector2 UxDrawCenter()
	{
		Control pile = _uxBattle?.GetNodeOrNull<Control>("DrawPile");
		if (pile != null && GodotObject.IsInstanceValid(pile))
		{
			return _uxHand.GetGlobalTransformWithCanvas().AffineInverse() * (pile.GetGlobalTransformWithCanvas() * (pile.Size / 2f));
		}
		float cs = UxCardScale;
		float u = BattleUnit;
		float handY = (PhoneLayout ? (-3) : 11);
		return new Vector2(Size.X / 2f - 900f * u, Size.Y - (handY + (SourceCardFace.H * cs + 40f) / 2f - 100f) * u);
	}

	private void UxStartHandDeal(string uid, CardTile tile, double delay)
	{
		UxCancelHandDeal(uid, settle: false);
		UxHandDeal deal = new UxHandDeal
		{
			Tile = tile,
			Epoch = _uxEpoch,
			Clock = new SourceHandDealClock(delay),
			CanvasSize = Size,
			Unit = BattleUnit
		};
		tile.Position = UxDrawCenter() - tile.PivotOffset;
		tile.Scale = Vector2.One * (0.35f * UxCardWidthScale * BattleUnit);
		tile.RotationDegrees = 20f;
		deal.StartPosition = tile.Position;
		deal.StartScale = tile.Scale.Y;
		deal.StartDegrees = tile.RotationDegrees;
		SourceHandCardBack.Add(tile);
		_uxDeals[uid] = deal;
		UxDealStartCue(uid, deal);
	}

	private void UxDealStartCue(string uid, UxHandDeal deal)
	{
		if (UxDealIsCurrent(uid, deal) && deal.Clock.TakeStartCue())
		{
			deal.StartPosition = deal.Tile.Position;
			deal.StartScale = deal.Tile.Scale.Y;
			deal.StartDegrees = deal.Tile.RotationDegrees;
			HandDealStartCueCount++;
			Sound("CardsDrawn");
		}
	}

	private bool UxKeepHandDeal(string uid, CardTile tile)
	{
		if (!_uxDeals.TryGetValue(uid, out var deal))
		{
			return false;
		}
		if (!UxDealIsCurrent(uid, deal) || deal.CanvasSize != Size || Math.Abs(deal.Unit - BattleUnit) > 0.0001f)
		{
			UxCancelHandDeal(uid, settle: false);
			return false;
		}
		if (!deal.Clock.Started || deal.Clock.Elapsed < 0.17)
		{
			SourceHandCardBack.Raise(tile);
		}
		return true;
	}

	private void UxCancelHandDeal(string uid, bool settle)
	{
		if (!_uxDeals.Remove(uid, out var deal))
		{
			return;
		}
		deal.Clock.Cancel();
		CardTile tile = deal.Tile;
		if (GodotObject.IsInstanceValid(tile) && !tile.IsQueuedForDeletion())
		{
			SourceHandCardBack.Remove(tile);
			if (settle && _uxTiles.TryGetValue(uid, out var live) && live == tile && _uxSeats.TryGetValue(uid, out (Vector2, float, int) seat))
			{
				tile.Position = seat.Item1;
				tile.RotationDegrees = seat.Item2;
				tile.Scale = Vector2.One * (UxCardScale * BattleUnit);
			}
		}
	}

	private void UxCancelHandDeals(bool settle)
	{
		string[] array = _uxDeals.Keys.ToArray();
		foreach (string uid in array)
		{
			UxCancelHandDeal(uid, settle);
		}
	}

	private void UxTickHandDeals(double delta)
	{
		if (_uxDeals.Count == 0)
		{
			return;
		}
		if (CurrentRoute != "combat" || _overlay != null || _uxHand == null || !GodotObject.IsInstanceValid(_uxHand) || !_uxHand.IsVisibleInTree())
		{
			UxCancelHandDeals(settle: true);
			return;
		}
		KeyValuePair<string, UxHandDeal>[] array = _uxDeals.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			KeyValuePair<string, UxHandDeal> entry = array[i];
			string uid = entry.Key;
			UxHandDeal deal = entry.Value;
			if (!UxDealIsCurrent(uid, deal))
			{
				UxCancelHandDeal(uid, settle: false);
				continue;
			}
			if (deal.CanvasSize != Size || Math.Abs(deal.Unit - BattleUnit) > 0.0001f)
			{
				UxCancelHandDeal(uid, settle: true);
				continue;
			}
			if (_uxHover == uid || _uxDrag == uid)
			{
				UxCancelHandDeal(uid, settle: false);
				continue;
			}
			deal.Clock.Advance(delta);
			UxDealStartCue(uid, deal);
			if (UxDealIsCurrent(uid, deal) && deal.Clock.Started && _uxSeats.TryGetValue(uid, out (Vector2, float, int) seat))
			{
				SourceHandDealMotion.Pose pose = SourceHandDealMotion.Sample(deal.Clock.Elapsed, deal.StartScale, UxCardScale * BattleUnit, deal.StartDegrees, seat.Item2);
				CardTile tile = deal.Tile;
				tile.Position = deal.StartPosition.Lerp(seat.Item1, pose.PositionFraction);
				tile.Scale = new Vector2(pose.ScaleX, pose.ScaleY);
				tile.RotationDegrees = pose.Degrees;
				if (!pose.BackVisible)
				{
					SourceHandCardBack.Remove(tile);
				}
				if (pose.Complete)
				{
					UxCancelHandDeal(uid, settle: true);
				}
			}
		}
	}

	private void UxArmHandDragHoverHandoff(UxHandDragPose owner)
	{
		_uxHandDragHoverHandoff = ((!owner.StationaryAim && owner.OriginalFilter != MouseFilterEnum.Ignore && UxHandDragContextIsCurrent(owner)) ? new UxHandDragHoverHandoff
		{
			Drag = owner,
			Pointer = GetViewport().GetMousePosition()
		} : null);
	}

	private void UxBindHandDragHoverHandoff(UxHandDragPose owner)
	{
		UxHandDragHoverHandoff handoff = _uxHandDragHoverHandoff;
		if (handoff != null && handoff.Drag == owner)
		{
			if (_uxHandHoverPoses.TryGetValue(owner.Uid, out var pose) && pose.DragReturn && pose.Tile == owner.Tile)
			{
				handoff.Return = pose;
			}
			handoff.Returning = false;
			UxPruneHandDragHoverHandoff();
		}
	}

	private void UxPruneHandDragHoverHandoff()
	{
		UxHandDragHoverHandoff handoff = _uxHandDragHoverHandoff;
		if (handoff != null)
		{
			UxHandDragPose owner = handoff.Drag;
			bool num = owner.Epoch == _uxEpoch && _uxTiles.TryGetValue(owner.Uid, out var tile) && tile == owner.Tile && GodotObject.IsInstanceValid(tile) && !tile.IsQueuedForDeletion() && GodotObject.IsInstanceValid(owner.Parent) && !owner.Parent.IsQueuedForDeletion() && owner.Parent == _uxHand && tile.GetParent() == owner.Parent && CurrentRoute == "combat" && _overlay == null && _play == null && !IsInputBlocked && owner.Parent.IsVisibleInTree() && owner.CanvasSize == Size && Math.Abs(owner.Unit - BattleUnit) < 0.0001f && GetViewport().GetMousePosition() == handoff.Pointer;
			bool owns = ((handoff.Return != null) ? (_uxDrag == null && handoff.Return.DragReturn && !handoff.Return.Clock.Complete && UxHandHoverIsCurrent(owner.Uid, handoff.Return)) : (UxHandDragIsCurrent(owner) || (handoff.Returning && _uxDrag == null && _uxHandDragPose == null)));
			if (!num || !owns)
			{
				_uxHandDragHoverHandoff = null;
			}
		}
	}

	private void UxClearHandDragHoverHandoff(string uid, UxHandHoverPose pose)
	{
		UxHandDragHoverHandoff handoff = _uxHandDragHoverHandoff;
		if (handoff != null && handoff.Drag.Uid == uid && handoff.Return == pose)
		{
			_uxHandDragHoverHandoff = null;
		}
	}

	private void UxObserveHandDragHoverInput(InputEvent evt)
	{
		UxHandDragHoverHandoff handoff = _uxHandDragHoverHandoff;
		if (handoff != null)
		{
			if (evt is InputEventMouseButton { Pressed: not false } || evt is InputEventScreenTouch { Pressed: not false } || (evt is InputEventMouseMotion motion && motion.Position != handoff.Pointer) || (evt is InputEventScreenDrag drag && drag.Position != handoff.Pointer))
			{
				_uxHandDragHoverHandoff = null;
			}
			else
			{
				UxPruneHandDragHoverHandoff();
			}
		}
	}

	private void UxNativeHandHover(string uid, CardTile tile, bool enter)
	{
        if (TouchOwnsNativeHand) return;
		if (!_uxTiles.TryGetValue(uid, out var live) || live != tile || !GodotObject.IsInstanceValid(tile) || tile.IsQueuedForDeletion() || tile.GetParent() != _uxHand)
		{
			return;
		}
		if (!enter && _uxHover == uid && (tile.InspectionHeld || UxHandPreviewContainsPointer(uid, tile))) return;
		UxPruneHandDragHoverHandoff();
		UxHandDragHoverHandoff handoff = _uxHandDragHoverHandoff;
		if (handoff != null)
		{
			bool same = handoff.Drag.Uid == uid && handoff.Drag.Tile == tile;
			if (same && handoff.Return == null && UxHandDragIsCurrent(handoff.Drag))
			{
				return;
			}
			if (same | enter)
			{
				_uxHandDragHoverHandoff = null;
			}
			if (same & enter)
			{
				return;
			}
		}
		HoverUxHand(uid, enter);
	}

	private bool UxHandDragIsCurrent(UxHandDragPose owner)
	{
		if (owner != null && owner == _uxHandDragPose && owner.Epoch == _uxEpoch && owner.Uid == _uxDrag && _uxTiles.TryGetValue(owner.Uid, out var tile) && tile == owner.Tile && GodotObject.IsInstanceValid(tile) && !tile.IsQueuedForDeletion() && GodotObject.IsInstanceValid(owner.Parent) && !owner.Parent.IsQueuedForDeletion() && owner.Parent == _uxHand)
		{
			return tile.GetParent() == owner.Parent;
		}
		return false;
	}

	private bool UxHandDragContextIsCurrent(UxHandDragPose owner)
	{
		if (UxHandDragIsCurrent(owner) && CurrentRoute == "combat" && _overlay == null && _play == null && !IsInputBlocked && owner.Parent.IsVisibleInTree() && owner.CanvasSize == Size)
		{
			return Math.Abs(owner.Unit - BattleUnit) < 0.0001f;
		}
		return false;
	}

	private void UxBeginHandDragPose(string uid, CardTile tile)
	{
		if (!_uxTiles.TryGetValue(uid, out var live) || live != tile || !_uxSeats.TryGetValue(uid, out (Vector2, float, int) seat) || !GodotObject.IsInstanceValid(tile) || tile.IsQueuedForDeletion() || tile.GetParent() != _uxHand)
		{
			return;
		}
		CardInstance card = InputCombat?.Player.Hand.FirstOrDefault((CardInstance c) => c.Uid == uid);
		if (!(card == null))
		{
			EndHandDrag();
			_uxHandDragHoverHandoff = null;
			UxCancelHandHover(uid, settle: false);
			UxCancelHandDeal(uid, settle: false);
			UxHandDragPose owner = (_uxHandDragPose = new UxHandDragPose
			{
				Uid = uid,
				Epoch = _uxEpoch,
				Tile = tile,
				Parent = _uxHand,
				StationaryAim = SourceHandDragPose.UsesStationaryAim(PhoneLayout, card),
				OriginalFilter = tile.MouseFilter,
				CanvasSize = Size,
				Unit = BattleUnit
			});
			_uxDrag = uid;
			_uxHover = null;
			owner.Parent.MoveChild(tile, owner.Parent.GetChildCount() - 1);
			tile.RotationDegrees = 0f;
			tile.ZIndex = 20;
			tile.Scale = Vector2.One * ((owner.StationaryAim ? UxCardScale : 0.8f * UxCardWidthScale) * owner.Unit);
			if (owner.StationaryAim)
			{
				(tile.Position, _, _) = seat;
			}
			else
			{
				tile.MouseFilter = MouseFilterEnum.Ignore;
				UxPlaceHandDragAtPointer(owner);
			}
			UpdateHandAim();
		}
	}

	private void UxPlaceHandDragAtPointer(UxHandDragPose owner)
	{
		if (UxHandDragIsCurrent(owner) && !owner.StationaryAim)
		{
			owner.Tile.Position = owner.Parent.GetLocalMousePosition() - owner.Tile.PivotOffset;
		}
	}

	private void UxClearHandDragAim()
	{
		if (_uxAim != null && GodotObject.IsInstanceValid(_uxAim))
		{
			_uxAim.Active = false;
			_uxAim.QueueRedraw();
		}
	}

	private void UxReleaseHandDragPose(bool returnToSeat)
	{
		UxHandDragPose owner = _uxHandDragPose;
		bool flag = UxHandDragIsCurrent(owner);
		if (flag & returnToSeat)
		{
			if (UxHandDragContextIsCurrent(owner))
			{
				UxPlaceHandDragAtPointer(owner);
			}
			UxArmHandDragHoverHandoff(owner);
			owner.Tile.MouseFilter = owner.OriginalFilter;
		}
		UxHandDragHoverHandoff handoff = _uxHandDragHoverHandoff;
		if (handoff != null && handoff.Drag == owner)
		{
			handoff.Returning = true;
		}
		_uxHandDragPose = null;
		_uxDrag = null;
		UxClearHandDragAim();
		if (flag & returnToSeat)
		{
			UxReturnHandDrag(owner.Uid);
			UxBindHandDragHoverHandoff(owner);
			if (owner.CanvasSize != Size || Math.Abs(owner.Unit - BattleUnit) >= 0.0001f)
			{
				UxCancelHandHover(owner.Uid, settle: true);
			}
		}
	}

	private void UxStopHandDragForTreeExit()
	{
		_uxHandDragHoverHandoff = null;
		UxHandDragPose owner = _uxHandDragPose;
		if (UxHandDragIsCurrent(owner))
		{
			owner.Tile.MouseFilter = owner.OriginalFilter;
			UxCancelHandHover(owner.Uid, settle: false);
			UxCancelHandDeal(owner.Uid, settle: false);
			if (_uxSeats.TryGetValue(owner.Uid, out (Vector2, float, int) seat))
			{
				owner.Tile.Position = seat.Item1;
				owner.Tile.Scale = Vector2.One * (UxCardScale * BattleUnit);
				owner.Tile.RotationDegrees = seat.Item2;
				owner.Tile.ZIndex = 0;
			}
		}
		_uxHandDragPose = null;
		_uxDrag = null;
		UxClearHandDragAim();
		KeyValuePair<string, UxHandHoverPose>[] array = _uxHandHoverPoses.Where((KeyValuePair<string, UxHandHoverPose> p) => p.Value.DragReturn).ToArray();
		foreach (KeyValuePair<string, UxHandHoverPose> pair in array)
		{
			UxCancelHandHover(pair.Key, settle: true);
		}
	}

	private void UxNativeHandDragEnded(string uid, CardTile tile)
	{
		UxHandDragPose owner = _uxHandDragPose;
		if (UxHandDragIsCurrent(owner) && !(owner.Uid != uid) && owner.Tile == tile)
		{
			EndHandDrag();
		}
	}

	private void UxTickHandDragPose()
	{
		UxPruneHandDragHoverHandoff();
		UxHandDragPose owner = _uxHandDragPose;
		if (owner != null)
		{
			if (!UxHandDragContextIsCurrent(owner))
			{
				EndHandDrag();
			}
			else
			{
				UxPlaceHandDragAtPointer(owner);
			}
		}
	}

	private void UxDetachRetiredHandDrag(string uid, CardTile tile)
	{
		UxHandDragPose owner = _uxHandDragPose;
		if (owner != null && owner.Uid == uid && owner.Tile == tile)
		{
			UxReleaseHandDragPose(returnToSeat: false);
		}
	}

	private void UxFreezeAdmittedHandDrag(SessionChange change)
	{
		UxHandDragPose owner = _uxHandDragPose;
		if (!UxHandDragIsCurrent(owner) || !(change.Command is RunCommand_Combat { Command: Command_PlayCard play }) || play.CardUid != owner.Uid)
		{
			return;
		}
		RunState? previous = change.Previous;
		if ((object)previous != null && previous.Combat?.Player.Hand.Any((CardInstance c) => c.Uid == owner.Uid) == true && !(change.Current.Combat == null) && !change.Current.Combat.Player.Hand.Any((CardInstance c) => c.Uid == owner.Uid))
		{
			if (owner.CanvasSize == Size && Math.Abs(owner.Unit - BattleUnit) < 0.0001f)
			{
				UxPlaceHandDragAtPointer(owner);
			}
			UxReleaseHandDragPose(returnToSeat: false);
		}
	}

	private bool UxHandExitIsCurrent(string uid, UxHandExit p)
	{
		if (p.Epoch == _uxEpoch && !p.Clock.Cancelled && _uxHandExits.TryGetValue(uid, out var active) && active == p && !_uxTiles.ContainsKey(uid) && GodotObject.IsInstanceValid(p.Tile) && !p.Tile.IsQueuedForDeletion())
		{
			return p.Tile.GetParent() == _uxHand;
		}
		return false;
	}

	private bool UxUseDiscardFlight(string uid, GameState state)
	{
		return SourceHandExitMotion.IsOrdinaryDiscard(UxRemovedPlayedCard(new string[1] { uid }), state.Player.SetCards.Any((CardInstance c) => c.Uid == uid), state.Player.Permanents.Any((CardInstance c) => c.Uid == uid), state.Player.ExhaustPile.Any((CardInstance c) => c.Uid == uid));
	}

	private Vector2 UxDiscardCenter()
	{
		Control pile = _uxBattle?.GetNodeOrNull<Control>("DiscardPile");
		if (pile != null && GodotObject.IsInstanceValid(pile) && !pile.IsQueuedForDeletion())
		{
			return _uxHand.GetGlobalTransformWithCanvas().AffineInverse() * (pile.GetGlobalTransformWithCanvas() * (pile.Size / 2f));
		}
		float cs = UxCardScale;
		float u = BattleUnit;
		float handY = (PhoneLayout ? (-3) : 11);
		return new Vector2(Size.X / 2f + 900f * u, Size.Y - (handY + (SourceCardFace.H * cs + 40f) / 2f - 100f) * u);
	}

	private void UxRetireRemovedHand(string uid, GameState state, int ordinal)
	{
		CardTile gone = _uxTiles[uid];
		UxDetachRetiredHandDrag(uid, gone);
		UxCancelHandHover(uid, settle: false);
		UxCancelHandDeal(uid, settle: false);
		_uxTiles.Remove(uid);
		_uxSeats.Remove(uid);
		gone.MouseFilter = MouseFilterEnum.Ignore;
		if (UxTryStartAuthoredHandExit(uid, gone))
		{
			return;
		}
		if (UxUseDiscardFlight(uid, state))
		{
			UxCancelHandExit(uid);
			gone.Disabled = true;
			gone.AllowDrag = false;
			gone.SetProcess(enable: false);
			gone.ZIndex = 0;
			UxHandExit p = new UxHandExit
			{
				Tile = gone,
				Epoch = _uxEpoch,
				Clock = new SourceHandExitClock(SourceHandExitMotion.Delay(ordinal), Godot.Engine.GetProcessFrames()),
				From = gone.Position,
				To = UxDiscardCenter() - gone.PivotOffset,
				FromScale = gone.Scale,
				CanvasSize = Size,
				Unit = BattleUnit
			};
			_uxHandExits[uid] = p;
			UxStartHandExit(uid, p);
			return;
		}
		Tween tween = CreateTween();
		tween.TweenProperty(gone, "modulate:a", 0f, _uxRetirement?.Invoke(gone.GetMeta("definition_id", "").AsString()) ?? 0.25);
		tween.TweenCallback(Callable.From(() =>
		{
			if (GodotObject.IsInstanceValid(gone))
			{
				gone.QueueFree();
			}
		}));
	}

	private void UxStartHandExit(string uid, UxHandExit p)
	{
		if (UxHandExitIsCurrent(uid, p) && p.Clock.TakeStart())
		{
			p.From = p.Tile.Position;
			p.FromScale = p.Tile.Scale;
			Color color = p.Tile.Modulate;
			color.A = 1f;
			p.Tile.Modulate = color;
			_uxHand.MoveChild(p.Tile, _uxHand.GetChildCount() - 1);
			HandExitStartCount++;
		}
	}

	private void UxRestoreHandExitOrder()
	{
		foreach (KeyValuePair<string, UxHandExit> pair in _uxHandExits)
		{
			if (UxHandExitIsCurrent(pair.Key, pair.Value))
			{
				_uxHand.MoveChild(pair.Value.Tile, _uxHand.GetChildCount() - 1);
			}
		}
	}

	private void UxCancelHandExit(string uid)
	{
		if (_uxHandExits.Remove(uid, out var p))
		{
			p.Clock.Cancel();
			if (GodotObject.IsInstanceValid(p.Tile) && !p.Tile.IsQueuedForDeletion() && (!_uxTiles.TryGetValue(uid, out var live) || live != p.Tile))
			{
				p.Tile.Hide();
				p.Tile.QueueFree();
			}
		}
	}

	private void UxCancelHandExits()
	{
		string[] array = _uxHandExits.Keys.ToArray();
		foreach (string uid in array)
		{
			UxCancelHandExit(uid);
		}
	}

	private void UxTickHandExits(double delta)
	{
		if (_uxHandExits.Count == 0)
		{
			return;
		}
		if (CurrentRoute != "combat" || _overlay != null || _uxHand == null || !GodotObject.IsInstanceValid(_uxHand) || !_uxHand.IsVisibleInTree())
		{
			UxCancelHandExits();
			return;
		}
		ulong frame = Godot.Engine.GetProcessFrames();
		KeyValuePair<string, UxHandExit>[] array = _uxHandExits.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			KeyValuePair<string, UxHandExit> pair = array[i];
			string uid = pair.Key;
			UxHandExit p = pair.Value;
			if (!UxHandExitIsCurrent(uid, p) || p.CanvasSize != Size || Math.Abs(p.Unit - BattleUnit) > 0.0001f)
			{
				UxCancelHandExit(uid);
				continue;
			}
			p.Clock.Advance(delta, frame);
			UxStartHandExit(uid, p);
			if (p.Clock.Started)
			{
				SourceHandExitMotion.Pose pose = SourceHandExitMotion.Sample(p.Clock.Elapsed);
				p.Tile.Position = p.From.Lerp(p.To, pose.PositionFraction);
				p.Tile.Scale = p.FromScale.Lerp(Vector2.One * (0.3f * p.Unit), pose.ScaleFraction);
				Color color = p.Tile.Modulate;
				color.A = pose.Alpha;
				p.Tile.Modulate = color;
				if (pose.Complete)
				{
					UxCancelHandExit(uid);
				}
			}
		}
	}

	private void UxReturnHandDrag(string uid)
	{
		if (_uxTiles.TryGetValue(uid, out var tile) && _uxSeats.TryGetValue(uid, out (Vector2, float, int) seat))
		{
			if (_uxHover == uid)
			{
				_uxHover = null;
			}
			tile.ZIndex = 0;
			if (CurrentRoute != "combat" || _overlay != null || _play != null || IsInputBlocked)
			{
				UxCancelHandHover(uid, settle: false);
				UxCancelHandDeal(uid, settle: false);
				tile.Position = seat.Item1;
				tile.Scale = Vector2.One * (UxCardScale * BattleUnit);
				tile.RotationDegrees = seat.Item2;
			}
			else
			{
				UxStartHandHover(uid, tile, seat.Item1, Vector2.One * (UxCardScale * BattleUnit), seat.Item2, dragReturn: true);
			}
			RestoreUxHandOrder();
		}
	}

	private bool UxHandHoverIsCurrent(string uid, UxHandHoverPose p)
	{
		if (p.Epoch == _uxEpoch && !p.Clock.Cancelled && _uxHandHoverPoses.TryGetValue(uid, out var active) && p == active && _uxTiles.TryGetValue(uid, out var tile) && tile == p.Tile && GodotObject.IsInstanceValid(tile))
		{
			return !tile.IsQueuedForDeletion();
		}
		return false;
	}

	private void UxStartHandHover(string uid, CardTile tile, Vector2 to, Vector2 scale, float degrees, bool dragReturn = false)
	{
		UxCancelHandDeal(uid, settle: false);
		if (!_uxHandHoverPoses.TryGetValue(uid, out var current) || !UxHandHoverIsCurrent(uid, current) || !(current.To == to) || !(current.ToScale == scale) || (current.DragReturn != dragReturn && (!current.DragReturn || dragReturn || !(_uxHover != uid))))
		{
			UxCancelHandHover(uid, settle: false);
			tile.RotationDegrees = degrees;
			_uxHandHoverPoses[uid] = new UxHandHoverPose
			{
				Tile = tile,
				Epoch = _uxEpoch,
				Clock = new SourceHandHoverClock(Godot.Engine.GetProcessFrames(), dragReturn ? 0.15 : 0.12),
				DragReturn = dragReturn,
				From = tile.Position,
				To = to,
				FromScale = tile.Scale,
				ToScale = scale,
				CanvasSize = Size,
				Unit = BattleUnit
			};
		}
	}

	private bool UxKeepHandHover(string uid, CardTile tile)
	{
		if (!_uxHandHoverPoses.TryGetValue(uid, out var p))
		{
			return false;
		}
		if (!UxHandHoverIsCurrent(uid, p) || p.CanvasSize != Size || Math.Abs(p.Unit - BattleUnit) > 0.0001f)
		{
			UxCancelHandHover(uid, settle: false);
			return false;
		}
		if (_uxHover != uid && _uxSeats.TryGetValue(uid, out (Vector2, float, int) seat) && (p.To != seat.Item1 || p.ToScale != Vector2.One * (UxCardScale * BattleUnit) || Math.Abs(tile.RotationDegrees - seat.Item2) > 0.001f))
		{
			UxCancelHandHover(uid, settle: false);
			return false;
		}
		return true;
	}

	private void UxCancelHandHover(string uid, bool settle)
	{
		if (_uxHandHoverPoses.Remove(uid, out var p))
		{
			UxClearHandDragHoverHandoff(uid, p);
			p.Clock.Cancel();
			CardTile tile = p.Tile;
			if (settle && GodotObject.IsInstanceValid(tile) && !tile.IsQueuedForDeletion() && _uxTiles.TryGetValue(uid, out var current) && current == tile && _uxSeats.TryGetValue(uid, out (Vector2, float, int) seat))
			{
				tile.Position = seat.Item1;
				tile.Scale = Vector2.One * (UxCardScale * BattleUnit);
				tile.RotationDegrees = seat.Item2;
				tile.ZIndex = 0;
			}
		}
	}

	private void UxCancelHandHovers(bool settle)
	{
		string[] array = _uxHandHoverPoses.Keys.ToArray();
		foreach (string uid in array)
		{
			UxCancelHandHover(uid, settle);
		}
		if (settle)
		{
			string hovered = _uxHover;
			_uxHover = null;
			if (hovered != null && _uxTiles.TryGetValue(hovered, out var tile) && GodotObject.IsInstanceValid(tile) && !tile.IsQueuedForDeletion() && _uxSeats.TryGetValue(hovered, out (Vector2, float, int) seat))
			{
				tile.Position = seat.Item1;
				tile.Scale = Vector2.One * (UxCardScale * BattleUnit);
				tile.RotationDegrees = seat.Item2;
				tile.ZIndex = 0;
			}
			if (_uxHand != null && GodotObject.IsInstanceValid(_uxHand) && !_uxHand.IsQueuedForDeletion())
			{
				RestoreUxHandOrder();
			}
		}
	}

	private void UxTickHandHovers(double delta)
	{
		if (_uxHover != null && _uxDrag == null && _uxTiles.TryGetValue(_uxHover, out var hovered) && GodotObject.IsInstanceValid(hovered)
			&& !hovered.InspectionHeld && !UxHandPreviewContainsPointer(_uxHover, hovered)) HoverUxHand(_uxHover, enter: false);

		if (CurrentRoute != "combat" || _overlay != null || _uxHand == null || !GodotObject.IsInstanceValid(_uxHand) || !_uxHand.IsVisibleInTree())
		{
			if (_uxHandHoverPoses.Count > 0 || _uxHover != null)
			{
				UxCancelHandHovers(settle: true);
			}
		}
		else
		{
			if (_uxHandHoverPoses.Count == 0)
			{
				return;
			}
			ulong frame = Godot.Engine.GetProcessFrames();
			KeyValuePair<string, UxHandHoverPose>[] array = _uxHandHoverPoses.ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				KeyValuePair<string, UxHandHoverPose> pair = array[i];
				UxHandHoverPose p = pair.Value;
				string uid = pair.Key;
				if (!UxHandHoverIsCurrent(uid, p))
				{
					UxCancelHandHover(uid, settle: false);
					continue;
				}
				if (_uxDrag == uid)
				{
					UxCancelHandHover(uid, settle: false);
					continue;
				}
				if (p.CanvasSize != Size || Math.Abs(p.Unit - BattleUnit) > 0.0001f)
				{
					UxCancelHandHover(uid, settle: true);
					continue;
				}
				p.Clock.Advance(delta, frame);
				float move = (p.DragReturn ? SourceHandReturnMotion.PositionFraction(p.Clock.Elapsed) : SourceHandHoverMotion.Fraction(p.Clock.Elapsed));
				float scale = (p.DragReturn ? SourceHandReturnMotion.ScaleFraction(p.Clock.Elapsed) : SourceHandHoverMotion.Fraction(p.Clock.Elapsed));
				p.Tile.Position = p.From.Lerp(p.To, move);
				p.Tile.Scale = p.FromScale.Lerp(p.ToScale, scale);
				if (p.Clock.Complete)
				{
					_uxHandHoverPoses.Remove(uid);
					UxClearHandDragHoverHandoff(uid, p);
				}
			}
		}
	}

	public void SetPresentationDiagnostics(Action<bool> set, Func<bool> enabled, Func<string> lastFile)
	{
		_setPresentationDiagnostics = set;
		_presentationDiagnosticsEnabled = enabled;
		_presentationDiagnosticsLastFile = lastFile;
	}

	public void SetPresentationDiagnosticStatus(string value)
	{
		_presentationDiagnosticStatus = value;
	}

	private void BuildPresentationDiagnosticsControls(Control box)
	{
		if (_setPresentationDiagnostics != null)
		{
			bool active = _presentationDiagnosticsEnabled?.Invoke() ?? false;
			Label status = Text(box, _presentationDiagnosticStatus, 17, Muted);
			Button(box, active ? "描画診断を終了して保存" : "描画診断を開始（画像取得なし）", () =>
			{
				_setPresentationDiagnostics(!(_presentationDiagnosticsEnabled?.Invoke() ?? false));
				status.Text = _presentationDiagnosticStatus;
				Rebuild();
			});
			Text(box, "通常のフレーム間隔・描画品質・コマ送り・停止時間を記録します。画面やセーブ内容は保存しません。次の起動ではオフです。", 16, Muted);
		}
	}

	public void SetPresentationOwnership(Func<GameEvent, SessionChange> owner, Func<GameEvent, bool> ordinary)
	{
		_uxEventOwner = owner;
		_uxOrdinary = ordinary;
	}

	public void SetIncomingContactProjection(Func<GameEvent, int?> provider)
	{
		_uxIncomingContact = provider;
	}

	public void SetLeaderProjection(Func<Vector2?> provider)
	{
		_uxLeaderProjection = provider;
	}

	public void SetCardRetirement(Func<string, double> provider)
	{
		_uxRetirement = provider;
	}

	public IReadOnlyList<Rect2> GetEnemyLedgerBounds()
	{
		return (from x in _uxEnemies
			where GodotObject.IsInstanceValid(x.Ledger) && x.Ledger.IsVisibleInTree()
			select x.Ledger.GetGlobalRect()).ToArray();
	}

	private void UxResetHand()
	{
		UxClearEnemyHeadCache();
		UxCancelAuthoredHandExits();
		UxCancelHandExits();
		UxCancelHandHovers(settle: false);
		UxCancelHandDeals(settle: false);
		_uxEpoch++;
		EndHandDrag();
		UxCancelHandHovers(settle: false);
		foreach (CardTile value in _uxTiles.Values)
		{
			value.Hide();
			value.QueueFree();
		}
		_uxTiles.Clear();
		_uxSeats.Clear();
		_uxHover = null;
		_uxPaid.Clear();
		_uxIncomingOwner = -1L;
	}

	private Label UxText(Control parent, string text, int units, Color color, bool deco = false, bool bold = false)
	{
		Label label = new Label
		{
			Text = text,
			MouseFilter = MouseFilterEnum.Ignore,
			VerticalAlignment = VerticalAlignment.Center
		};
		parent.AddChild(label, forceReadableName: false, InternalMode.Disabled);
        Font font = deco ? SourceRunUiDecor.Display : bold ? SourceRunUiDecor.Action : SourceRunUiDecor.Body;
		int fs = (int)Math.Round((float)Math.Max(PhoneLayout ? 15 : 13, units) * BattleUnit * _textScale);
		label.AddThemeFontOverride("font", font);
		label.AddThemeFontSizeOverride("font_size", fs);
		label.AddThemeColorOverride("font_color", color);
		return label;
	}

	private static StyleBoxFlat UxPanelBox(Color color, Color edge, int width = 2)
	{
		StyleBoxFlat styleBoxFlat = Box(color, edge, width);
		float num = (styleBoxFlat.ContentMarginBottom = 0f);
		float num3 = (styleBoxFlat.ContentMarginTop = num);
		float contentMarginLeft = (styleBoxFlat.ContentMarginRight = num3);
		styleBoxFlat.ContentMarginLeft = contentMarginLeft;
		return styleBoxFlat;
	}

	private PanelContainer UxPanel(Control parent, string name, Rect2 rect, Color? edge = null)
	{
		PanelContainer p = new PanelContainer
		{
			Name = name,
			Position = rect.Position,
			Size = rect.Size,
			MouseFilter = MouseFilterEnum.Ignore
		};
		p.AddThemeStyleboxOverride("panel", UxPanelBox(SourcePalette.Battle.Night, edge ?? SourcePalette.Battle.Edge));
		parent.AddChild(p, forceReadableName: false, InternalMode.Disabled);
		return p;
	}

	private Button UxButton(Control parent, string name, string text, Rect2 rect, Action action, bool enabled = true, bool gold = false)
	{
		Button b = Button(parent, text, action, enabled, gold ? new Color?(SourcePalette.BrassLight) : ((Color?)null));
		b.Name = name;
		b.Position = rect.Position;
		b.Size = rect.Size;
        if (SourcePalette.Battle.Enabled) {
            b.AddThemeStyleboxOverride("normal", Box(SourcePalette.Battle.Pill, SourcePalette.Battle.Edge));
            b.AddThemeStyleboxOverride("hover", Box(SourcePalette.Battle.Window.Lightened(0.15f), Accent));
        }
		if (gold)
		{
			b.AddThemeStyleboxOverride("normal", Box(SourcePalette.BrassLight, SourcePalette.Brass, 2));
			b.AddThemeStyleboxOverride("hover", Box(SourcePalette.Paper3, SourcePalette.Brass, 2));
			b.AddThemeColorOverride("font_color", SourcePalette.BrassInk);
            if (SourcePalette.Battle.Enabled) b.AddThemeColorOverride("font_hover_color", SourcePalette.BrassInk);
		}
		b.AddThemeFontOverride("font", SourceRunUiDecor.Action);
        int actionSize = (int)Math.Round(15f * BattleUnit * _textScale);
        while (actionSize > Math.Max(10, (int)(12f * BattleUnit)) && SourceRunUiDecor.Action.GetStringSize(text, fontSize: actionSize).X > rect.Size.X - 26f) actionSize--;
        b.AddThemeFontSizeOverride("font_size", actionSize);
		return b;
	}

	private Control UxHeaderTag(Control parent, string name, float height, float unit, params (string Text, int Size, Color Ink, bool Deco, bool Bold)[] parts)
	{
		Panel panel = new Panel
		{
			Name = name,
			Size = new Vector2(0f, height * unit),
			MouseFilter = MouseFilterEnum.Ignore
		};
		parent.AddChild(panel, forceReadableName: false, InternalMode.Disabled);
		panel.AddThemeStyleboxOverride("panel", SourceBattleDecor.Box(SourcePalette.Battle.Night, SourcePalette.Battle.Edge, unit));
		float x = 10f * unit;
		for (int i = 0; i < parts.Length; i++)
		{
			(string, int, Color, bool, bool) part = parts[i];
			int fs = (int)Math.Round((float)Math.Max(PhoneLayout ? 15 : 13, part.Item2) * unit * _textScale);
			SourceHudLabel sourceHudLabel = SourceBattleDecor.Text(panel, name + "Text", part.Item1, new Rect2(x, 0f, 500f * unit, height * unit), fs, part.Item3, part.Item4, part.Item5);
			float width = sourceHudLabel.GetThemeFont("font").GetStringSize(part.Item1, HorizontalAlignment.Left, -1f, fs, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal).X;
			sourceHudLabel.SourceTextRect = new Rect2(x, 0f, width, height * unit);
			x += width + 6f * unit;
		}
		panel.Size = new Vector2(x + 4f * unit, height * unit);
		return panel;
	}

	private void BuildBattleHeader()
	{
		GameState state = _view?.Combat;
		if ((object)state == null)
		{
			return;
		}
		float u = BattleUnit;
		Control header = new Control
		{
			Name = "BattleHeader",
			ZIndex = 30,
			CustomMinimumSize = new Vector2(0f, 56f * u),
			MouseFilter = MouseFilterEnum.Ignore
		};
		_shell.AddChild(header, forceReadableName: false, InternalMode.Disabled);
		string encounter = "";
		try
		{
			MapNode node = Run.CurrentNode(_view);
			if ((object)node != null && node.EncounterId != null)
			{
				encounter = Content.EncounterName(node.EncounterId);
			}
		}
		catch (Exception)
		{
		}
		Control title = UxHeaderTag(header, "EncounterTag", 40f, u, ("幕" + _view.Act + "・行" + (_view.Row + 1), 15, SourcePalette.PaperDim, false, false), (encounter, 19, SourcePalette.Paper, true, false));
		title.Position = new Vector2(24f * u - Sizing.Edge, 8f * u - Sizing.Edge);
		title.RotationDegrees = 0.6f;
		Control control = UxHeaderTag(header, "TurnTag", 32f, u, ("    ターン " + state.Turn, 15, SourcePalette.Paper, false, true));
		control.Position = new Vector2(title.Position.X + title.Size.X + 12f * u, 12f * u - Sizing.Edge);
		SourceBattleDecor.Icon(control, "TurnIcon", SourceIntentGlyphs.Mono("turn"), new Rect2(10f * u, 8f * u, 16f * u, 16f * u), SourcePalette.PaperDim);
		Control mana = UxHeaderTag(header, "ManaTag", 34f, u, ("    魔素", 15, SourcePalette.PaperDim, false, false), (_view.Mana.GetValueOrDefault().ToString(), 18, SourcePalette.ManaLight, true, false), ("/50", 15, SourcePalette.PaperDim, false, false));
		SourceBattleDecor.Icon(mana, "ManaIcon", SourceArtwork.Gear("mana", "interfere"), new Rect2(10f * u, 8f * u, 18f * u, 18f * u));
		Control money = UxHeaderTag(header, "GoldTag", 34f, u, ("    " + _view.Gold, 18, SourcePalette.BrassLight, true, false), ("G", 15, SourcePalette.PaperDim, false, false));
		SourceBattleDecor.Icon(money, "GoldIcon", SourceIntentGlyphs.ColorBitmap("gold", new Color("e0b84a")), new Rect2(10f * u, 9f * u, 16f * u, 16f * u));
		float menuWidth = (float)(PhoneLayout ? 64 : 44) * u;
		Button menu = UxButton(header, "BattleMenu", "≡", new Rect2(RunSafeRect.Size.X - menuWidth, 2f * u - Sizing.Edge, menuWidth, (float)(PhoneLayout ? 52 : 34) * u), OpenMenu);
		menu.AddThemeFontSizeOverride("font_size", (int)Math.Round((float)(PhoneLayout ? 26 : 18) * u * _textScale));
		menu.AddThemeStyleboxOverride("normal", SourceBattleDecor.Box(SourcePalette.Battle.Raised, SourcePalette.Paper2, u));
		mana.Position = new Vector2(menu.Position.X - 12f * u - mana.Size.X, 11f * u - Sizing.Edge);
		money.Position = new Vector2(mana.Position.X - 12f * u - money.Size.X, 11f * u - Sizing.Edge);
		PanelContainer plaque = UxPanel(header, "TurnPlaque", new Rect2(RunSafeRect.Size.X * 0.5f - 75f * u, 13f * u - Sizing.Edge, 150f * u, 30f * u));
		plaque.AddThemeStyleboxOverride("panel", SourceBattleDecor.Box(SourcePalette.Paper, SourcePalette.BrassInk, u, 1f));
		_uxPhase = UxText(plaque, "あなたの番", 15, SourcePalette.Ink, deco: true);
		_uxPhase.HorizontalAlignment = HorizontalAlignment.Center;
	}

	private void BuildBattle(RunState run)
	{
		GameState state = run.Combat;
		if (state == null)
		{
			return;
		}
		float u = BattleUnit;
		_uxEnemies.Clear();
        _uxEnemyLabelTargets.Clear();
		_enemyReadouts.Clear();
		_enemyIntentReadouts.Clear();
		_uxBattle = new Control
		{
			Name = "SourceBattleCanvas",
			ZIndex = 30,
			Size = Size,
			MouseFilter = MouseFilterEnum.Ignore
		};
		AddChild(_uxBattle, forceReadableName: false, InternalMode.Disabled);
		BattlefieldDropTarget field = new BattlefieldDropTarget
		{
			Name = "BattlefieldDrop",
			Size = Size,
			MouseFilter = MouseFilterEnum.Pass,
			CanPlay = CanDropHandCard,
			CardDropped = DropHandCard,
			ResolveCard = DecodeHandDrag
		};
		AddChild(field, forceReadableName: false, InternalMode.Disabled);
		if (_uxHand == null || !GodotObject.IsInstanceValid(_uxHand))
		{
			_uxHand = new Control
			{
				Name = "PersistentHand",
				Size = Size,
				MouseFilter = MouseFilterEnum.Ignore
			};
			AddChild(_uxHand, forceReadableName: false, InternalMode.Disabled);
		}
		else
		{
			MoveChild(_uxHand, GetChildCount() - 1);
		}
		_uxHand.Visible = true;
		MoveChild(_uxBattle, GetChildCount() - 1);
		float bottom = Size.Y - (float)(PhoneLayout ? 293 : 285) * u;
		BuildUxHero(state, bottom);
		BuildUxEnemies(field, state, bottom);
		_uxOrb = new SourceEnergyOrb
		{
			Name = "EnergyOrb",
			Position = new Vector2(RunSafeRect.Position.X, RunSafeRect.End.Y - 220f * u),
			Size = new Vector2(128f, 128f) * u,
			Energy = state.Player.Energy,
			Maximum = TurnEnergyCap(state),
			TextScale = _textScale,
			MouseFilter = MouseFilterEnum.Ignore
		};
		_uxBattle.AddChild(_uxOrb, forceReadableName: false, InternalMode.Disabled);
		PanelContainer cap = UxPanel(_uxBattle, "NextEnergyCap", new Rect2(RunSafeRect.Position.X, RunSafeRect.End.Y - 85f * u, 160f * u, 26f * u));
		UxText(cap, "次のターン 上限 " + state.Player.EnergyMax, 15, SourcePalette.BrassLight);
		SourceBattleDecor.Pile(UxButton(_uxBattle, "DrawPile", "", new Rect2(RunSafeRect.Position.X, RunSafeRect.End.Y - 52f * u, 150f * u, 52f * u), () =>
		{
			OpenDeck(state.Player.DrawPile.OrderBy((CardInstance x) => x.Def.Name).ToArray(), "山札 (名前順・順番は非公開)");
		}), "draw", "山札", state.Player.DrawPile.Count, u, _textScale, PhoneLayout);
		SourceBattleDecor.Pile(UxButton(_uxBattle, "DiscardPile", "", new Rect2(RunSafeRect.End.X - 150f * u, RunSafeRect.End.Y - 52f * u, 150f * u, 52f * u), () =>
		{
			OpenDeck(state.Player.DiscardPile, "捨て札");
		}), "discard", "捨て札", state.Player.DiscardPile.Count, u, _textScale, PhoneLayout);
		if (state.Player.ExhaustPile.Count > 0)
		{
			SourceBattleDecor.Pile(UxButton(_uxBattle, "ExhaustPile", "", new Rect2(RunSafeRect.End.X - 150f * u, RunSafeRect.End.Y - 96f * u, 150f * u, 36f * u), () =>
			{
				OpenExhaust(state);
			}), "exhaust", "消滅", state.Player.ExhaustPile.Count, u, _textScale, PhoneLayout);
		}
		Button button = UxButton(_uxBattle, "EndTurn", "ターン終了", new Rect2(RunSafeRect.End.X - 230f * u, RunSafeRect.End.Y - (float)(PhoneLayout ? 186 : 180) * u, 230f * u, 64f * u), BeginEndTurn, state.Phase == "player-turn" && state.PendingScry == null, gold: true);
		int endSize = (int)Math.Round(20f * u * _textScale);
        while (endSize > Math.Max(12, (int)(16f * u)) && SourceRunUiDecor.Display.GetStringSize(button.Text, fontSize: endSize).X > 230f * u - 26f) endSize--;
		button.AddThemeFontSizeOverride("font_size", endSize);
		button.AddThemeFontOverride("font", SourceRunUiDecor.Display);
		int sparks = state.Player.Hand.Count((CardInstance x) => x.Def.SparkToken == true);
		if (sparks >= 2)
		{
			UxButton(_uxBattle, "PlayAllSparks", "火種を全部撃つ (" + sparks + ")", new Rect2(RunSafeRect.End.X - 230f * u, RunSafeRect.End.Y - 276f * u, 230f * u, 48f * u), PlayAllSparks, enabled: true, gold: true);
		}
		BuildUxPockets(state);
		if (_uxIncomingOwner < 0 || !IsInputBlocked)
		{
			_uxIncoming = Effects.IncomingTotal(state);
			_uxLanded = false;
			_uxSkipFirst = state.NullifyNextAttack == true;
		}
		SyncUxHand();
		RefreshUxReadouts();
		_uxAim = new SourceAimArrow
		{
			Name = "HandAim",
			Size = Size,
			Unit = u,
			MouseFilter = MouseFilterEnum.Ignore
		};
		AddChild(_uxAim, forceReadableName: false, InternalMode.Disabled);
		if (_play != null && _play.Next(InputCombat) == "target")
		{
			BuildUxTargetBanner(_play);
		}
		if (_safeMargin != null && _safeMargin.GetParent() == this)
		{
			MoveChild(_safeMargin, GetChildCount() - 1);
		}
		if (_overlay == null && !IsInputBlocked)
		{
			if (state.PendingScry != null)
			{
				PrepareScry(state);
			}
			else if (state.Phase == "awaiting-reaction")
			{
				PrepareReaction(state);
			}
		}
	}

	private static int TurnEnergyCap(GameState s)
	{
		return Math.Max(1, (s.Player.EnergyMaxAtTurnStart > 0) ? (s.Player.EnergyMaxAtTurnStart - s.EnergyMaxRefBonus.GetValueOrDefault()) : s.Player.EnergyMax);
	}

	private void BuildUxHero(GameState s, float bottom)
	{
		float u = BattleUnit;
		IReadOnlyList<(string, string)> resources = SourceBattleDecor.Resources(s);
		int resourceRows = Math.Min(2, (resources.Count + 1) / 2);
		float w = (float)(PhoneLayout ? 208 : 360) * u;
		float h = (float)(PhoneLayout ? (80 + resourceRows * 30) : 120) * u;
		if (_textScale > 1f)
		{
			w = Math.Max(w, 230f * u * _textScale);
			h = Math.Max(h, (float)(80 + resourceRows * 30) * u * _textScale);
		}
		PanelContainer panelContainer = UxPanel(_uxBattle, "PlayerFootLedger", new Rect2(RunSafeRect.Position.X, bottom - h, w, h));
		Control body = new Control
		{
			Name = "HeroLedgerBody",
			MouseFilter = MouseFilterEnum.Ignore
		};
		panelContainer.AddChild(body, forceReadableName: false, InternalMode.Disabled);
		_uxHpBar = new SourceHealthBar
		{
			Name = "HeroHpBar",
			SourceHeroUnit = u,
			Position = new Vector2(8f, 8f) * u,
			Size = new Vector2(w - 16f * u, 22f * u * _textScale),
			MouseFilter = MouseFilterEnum.Ignore
		};
		body.AddChild(_uxHpBar, forceReadableName: false, InternalMode.Disabled);
		_hpReadout = CreateHpReadout(_uxHpBar, s.Player.Hp + " / " + s.Player.MaxHp, PhoneLayout ? 15 : 17);
		((SourceHudLabel)_hpReadout).EnableSeparateUnderlay(u, _textScale);
		_uxForecast = UxRich(body, "IncomingHeadline", PhoneLayout ? 18 : 20, deco: true);
		_uxForecast.SetMeta("source_text_rect", new Rect2(8f * u, 32f * u * _textScale, w - 16f * u, 22f * u * _textScale));
		_uxDetail = UxRich(body, "IncomingDetail", PhoneLayout ? 15 : 13, deco: false);
		_uxDetail.SetMeta("source_text_rect", new Rect2(8f * u, 54f * u * _textScale, w - 16f * u, 22f * u * _textScale));
		for (int row = 0; row < resourceRows; row++)
		{
			Control strip = new Control
			{
				Name = "HeroStatusRow_" + row,
				Position = new Vector2(8f * u, (float)(80 + row * 30) * u * _textScale),
				Size = new Vector2(w - 12f * u, 28f * u * _textScale),
				ClipContents = true,
				MouseFilter = MouseFilterEnum.Ignore
			};
			body.AddChild(strip, forceReadableName: false, InternalMode.Disabled);
			float cx = 0f;
			for (int i = row * 2; i < Math.Min(row * 2 + 2, resources.Count); i++)
			{
				(string, string) r = resources[i];
				bool debuff = r.Item2.StartsWith("弱") || r.Item2.StartsWith("脆") || r.Item2.StartsWith("虚") || r.Item2.StartsWith("重") || r.Item2.StartsWith("拘") || r.Item2.StartsWith("霞");
				Color ink;
				if (debuff)
				{
					ink = new Color("cdb6ea");
				}
				else if (r.Item2.StartsWith("勢い"))
				{
					ink = SourcePalette.BrassLight;
				}
				else
				{
					ink = (r.Item2.StartsWith("成長") ? new Color("a6d99a") : SourcePalette.Paper);
				}
				Font font = SourceRunUiDecor.Action;
				int fs = (int)Math.Round(15f * u * _textScale);
				float tw = font.GetStringSize(r.Item2, HorizontalAlignment.Left, -1f, fs, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal).X;
				float tx = (float)((r.Item1 != null) ? 32 : 10) * u;
				float cw = tx + tw + 10f * u;
				PanelContainer panelContainer2 = UxPanel(strip, "ResourceChip_" + i, new Rect2(cx, 0f, cw, 28f * u * _textScale));
				panelContainer2.AddThemeStyleboxOverride("panel", SourceBattleDecor.Box(debuff ? new Color("33284d") : SourcePalette.Battle.Pill, debuff ? new Color("47386b") : SourcePalette.Battle.Edge, u, 1f));
				Control inner = new Control
				{
					MouseFilter = MouseFilterEnum.Ignore
				};
				panelContainer2.AddChild(inner, forceReadableName: false, InternalMode.Disabled);
				if (r.Item1 != null)
				{
					SourceBattleDecor.Icon(inner, "ResourceIcon", SourceIntentGlyphs.Mono(r.Item1), new Rect2(10f * u, (28f * u * _textScale - 16f * u) / 2f, 16f * u, 16f * u), ink);
				}
				SourceBattleDecor.Text(inner, "ResourceText", r.Item2, new Rect2(tx, 0f, tw, 28f * u * _textScale), fs, ink, deco: false, bold: true);
				cx += cw + 6f * u;
			}
		}
		Button hit = new Button
		{
			Name = "IncomingForecast",
			Flat = true,
			MouseFilter = MouseFilterEnum.Stop,
			Size = new Vector2(w, 80f * u * _textScale),
			Text = "",
			TooltipText = "被ダメ予測の詳細"
		};
		string[] array = new string[6] { "normal", "hover", "pressed", "disabled", "focus", "hover_pressed" };
		foreach (string style in array)
		{
			hit.AddThemeStyleboxOverride(style, new StyleBoxEmpty());
		}
		body.AddChild(hit, forceReadableName: false, InternalMode.Disabled);
		hit.Pressed += () =>
		{
			Open("被ダメ予測", (VBoxContainer box) =>
			{
				VBoxContainer parent = ScrollColumn(box);
				foreach (string current in BattleForecast.Lines(_view.Combat))
				{
					Text(parent, current, 23);
				}
			});
		};
		if (resources.Count <= 0)
		{
			return;
		}
		Button status = new Button
		{
			Name = "HeroStatusDetails",
			Flat = true,
			Text = "",
			Position = new Vector2(0f, 80f * u * _textScale),
			Size = new Vector2(w, (float)(resourceRows * 30) * u * _textScale),
			MouseFilter = MouseFilterEnum.Stop,
			TooltipText = "状態・能力の詳細"
		};
		array = new string[6] { "normal", "hover", "pressed", "disabled", "focus", "hover_pressed" };
		foreach (string style2 in array)
		{
			status.AddThemeStyleboxOverride(style2, new StyleBoxEmpty());
		}
		body.AddChild(status, forceReadableName: false, InternalMode.Disabled);
		status.Pressed += () =>
		{
			Open("状態・能力", (VBoxContainer b) =>
			{
				Text(b, PlayerStatus(_view.Combat), 23);
				foreach (var item in SourceBattleDecor.Resources(_view.Combat))
				{
					Text(b, item.Text, 21);
				}
			});
		};
	}

	private SourceHudLabel CreateHpReadout(Control parent, string text, int sourceSize)
	{
		SourceHudLabel label = new SourceHudLabel
		{
			Name = "HpReadout",
			Text = text,
			FollowParentRect = true,
			MouseFilter = MouseFilterEnum.Ignore,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.Off
		};
		label.AddThemeFontSizeOverride("font_size", (int)Math.Round((float)sourceSize * BattleUnit * _textScale));
		label.AddThemeColorOverride("font_color", SourcePalette.Paper);
		SourceHudLabel.NightHalo(label, SourceRunUiDecor.Action, BattleUnit, _textScale, 0f, deferSmallUnderlay: true);
		parent.AddChild(label, forceReadableName: false, InternalMode.Disabled);
		return label;
	}

	private RichTextLabel UxRich(Control p, string name, int size, bool deco)
	{
		RichTextLabel l = new RichTextLabel
		{
			Name = name,
			BbcodeEnabled = true,
			ScrollActive = false,
			FitContent = false,
			MouseFilter = MouseFilterEnum.Ignore,
			AutowrapMode = TextServer.AutowrapMode.Off
		};
		p.AddChild(l, forceReadableName: false, InternalMode.Disabled);
		l.AddThemeFontOverride("normal_font", deco ? SourceRunUiDecor.Display : SourceRunUiDecor.Body);
		l.AddThemeFontSizeOverride("normal_font_size", (int)Math.Round((float)size * BattleUnit * _textScale));
		l.AddThemeColorOverride("default_color", SourcePalette.Paper);
		return l;
	}

	private void BuildUxEnemies(Control field, GameState s, float bottom)
	{
		CaptureSourceEnemyLedger(s);
		for (int i = 0; i < s.Enemies.Count; i++)
		{
			int index = i;
			EnemyState e = s.Enemies[i];
			if (e.Hp <= 0)
			{
				continue;
			}
			EnemyDropTarget hit = new EnemyDropTarget
			{
				Name = "EnemyTarget_" + i,
				Text = "",
				Disabled = IsInputBlocked,
				IsBlocked = () => IsInputBlocked,
				ResolveCard = DecodeHandDrag,
				MouseFilter = MouseFilterEnum.Stop
			};
			string[] array = new string[6] { "normal", "hover", "pressed", "disabled", "focus", "hover_pressed" };
			foreach (string style in array)
			{
				hit.AddThemeStyleboxOverride(style, new StyleBoxEmpty());
			}
			field.AddChild(hit, forceReadableName: false, InternalMode.Disabled);
			hit.Pressed += () =>
			{
				if (!hit.ConsumeInspection())
				{
					SelectEnemy(index);
				}
			};
			hit.Inspect = () =>
			{
				InspectEnemy(index);
			};
			hit.CardDropped = (string uid) =>
			{
				CardInstance cardInstance = InputCombat?.Player.Hand.FirstOrDefault((CardInstance x) => x.Uid == uid);
				if (cardInstance != null)
				{
					_preferredTarget = index;
					BeginPlay(cardInstance, index);
				}
			};
			PanelContainer intent = UxPanel(hit, "EnemyIntentSurface_" + i, new Rect2(0f, 0f, 90f * BattleUnit, 40f * BattleUnit));
			intent.SetMeta("enemy_index", i);
			intent.SetMeta("hud_role", "intent");
			SourceHudLabel title = new SourceHudLabel
			{
				Name = "IntentNumber",
				MouseFilter = MouseFilterEnum.Ignore
			};
			intent.AddChild(title, forceReadableName: false, InternalMode.Disabled);
			_enemyIntentReadouts[i] = title;
			PanelContainer ledger = UxPanel(hit, "EnemyLedgerSurface_" + i, new Rect2(0f, 0f, 180f * BattleUnit, 53f * BattleUnit));
			ledger.SetMeta("enemy_index", i);
			ledger.SetMeta("hud_role", "ledger");
			Control body = new Control
			{
				MouseFilter = MouseFilterEnum.Ignore
			};
			ledger.AddChild(body, forceReadableName: false, InternalMode.Disabled);
			SourceHealthBar bar = new SourceHealthBar
			{
				Name = "EnemyHpBar_" + i,
				Position = new Vector2(8f, PhoneLayout ? 29 : 25) * BattleUnit,
				Size = new Vector2(150f, 20f) * BattleUnit,
				MouseFilter = MouseFilterEnum.Ignore
			};
			body.AddChild(bar, forceReadableName: false, InternalMode.Disabled);
			SourceHudLabel hp = CreateHpReadout(bar, e.Hp + " / " + e.MaxHp, PhoneLayout ? 15 : 16);
			_enemyReadouts[i] = hp;
			BuildSourceEnemyLedger(body, i, e.EnemyId, bar, hp);
            if (UsesStyle04EnemyPresentation(e.EnemyId))
            {
                foreach (string role in new[] { "intent", "ledger" })
                {
                    var labelHit = new EnemyDropTarget { Name = $"EnemyLabelTarget_{i}_{role}", Text = "", Disabled = IsInputBlocked, IsBlocked = () => IsInputBlocked, ResolveCard = DecodeHandDrag, MouseFilter = MouseFilterEnum.Stop };
                    foreach (string style in array) labelHit.AddThemeStyleboxOverride(style, new StyleBoxEmpty());
                    labelHit.SetMeta("enemy_index", index); labelHit.SetMeta("label_role", role);
                    field.AddChild(labelHit);
                    _uxEnemyLabelTargets[(index, role)] = labelHit;
                    labelHit.Pressed += () => { if (!labelHit.ConsumeInspection()) SelectEnemy(index); };
                    labelHit.Inspect = () => InspectEnemy(index);
                    labelHit.CardDropped = hit.CardDropped;
                }
            }
			_uxEnemies.Add((i, hit, intent, ledger, bar, hp, title));
		}
        foreach (var labelHit in field.GetChildren().OfType<EnemyDropTarget>().Where(t => t.HasMeta("label_role")).ToArray()) field.MoveChild(labelHit, field.GetChildCount() - 1);
		PositionUxEnemies();
	}

	private void UxClearEnemyHeadCache()
	{
		_uxHeadHeightCache.Clear();
		_uxHeadHeightOrder.Clear();
	}

	private float UxEnemyHeadHeight(Texture2D texture)
	{
		(ulong, bool) key = (texture.GetInstanceId(), PhoneLayout);
		if (_uxHeadHeightCache.TryGetValue(key, out var cached))
		{
			_uxHeadCacheHits++;
			return cached;
		}
		using Image img = texture.GetImage();
		_uxHeadReadbacks++;
		int top = 0;
		bool found = false;
		for (int y = 0; y < img.GetHeight(); y++)
		{
			if (found)
			{
				break;
			}
			for (int x = 0; x < img.GetWidth(); x++)
			{
				if (img.GetPixel(x, y).A >= 16f / 255f)
				{
					top = y;
					found = true;
					break;
				}
			}
		}
		while (_uxHeadHeightCache.Count >= 512 && _uxHeadHeightOrder.Count > 0)
		{
			if (_uxHeadHeightCache.Remove(_uxHeadHeightOrder.Dequeue()))
			{
				_uxHeadEvictions++;
			}
		}
		_uxHeadHeightOrder.Enqueue(key);
		return _uxHeadHeightCache[key] = SourceEnemyUiAnchor.Height(img.GetWidth(), img.GetHeight(), top, PhoneLayout, img.GetWidth() == 96, "normal");
	}

	private float UxEnemyGap()
	{
		float gap = float.MaxValue;
		GameState s = _view?.Combat;
		if (s == null)
		{
			return gap;
		}
		for (int i = 1; i < s.Enemies.Count; i++)
		{
			float left = _enemyProjection?.Invoke(i - 1)?.Feet.X ?? (Size.X * (0.6f + (float)(i - 1) * 0.12f));
			float right = _enemyProjection?.Invoke(i)?.Feet.X ?? (Size.X * (0.6f + (float)i * 0.12f));
			gap = Math.Min(gap, Math.Abs(right - left) / BattleUnit);
		}
		return gap;
	}

    private SourceIntentLayout.Spec CompactStyle04Intent(SourceIntentLayout.Spec spec, float gap)
    {
        float u=BattleUnit, ts=Math.Max(1f,_textScale);
        int fs=(int)Math.Round(spec.FontSize*u*ts), rfs=(int)Math.Round(15f*u*ts);
        float text=SourceRunUiDecor.Display.GetStringSize(spec.Caption,HorizontalAlignment.Left,-1f,fs).X/u;
        float riders=spec.Riders.Sum(r => SourceRunUiDecor.Action.GetStringSize(r.Text,HorizontalAlignment.Left,-1f,rfs).X/u+(r.Icon==null?8f:25f));
        float g=PhoneLayout?6f:10f;
        float main=spec.IconSize+g+text;
        float inline=main+(spec.Riders.Count>0?g+riders+Math.Max(0,spec.Riders.Count-1)*g:0f)+16f;
        bool stack=spec.Riders.Count>0 && inline>gap-10f;
        float row=Math.Max(spec.IconSize,spec.FontSize*ts)+4f;
        float width=stack?Math.Max(main,riders+Math.Max(0,spec.Riders.Count-1)*4f)+16f:inline;
        return SourceIntentTag.ConstrainCompact(spec with { Stack=stack, Width=Math.Max(72f,width), RowHeight=row, Height=row+(stack?18f*ts+4f:0f) },u,PhoneLayout,ts,gap-10f);
    }

	private static bool UsesStyle04EnemyPresentation(string id) =>
		(BattleActor.UsesDefaultAuthoredFrames(id) || OS.GetCmdlineUserArgs().Contains("--enemy-style04-batch1")) && Style04EnemySpriteRegistry.TryGet(id, out _);

	private void PositionUxEnemies()
	{
		if (_uxBattle == null || !GodotObject.IsInstanceValid(_uxBattle))
		{
			return;
		}
		GameState state = _view?.Combat;
		if ((object)state == null)
		{
			return;
		}
		// A transient unmeasured viewport has no layout. Retain the last valid
		// positions; the normal per-frame path will measure again after resize.
		if (!float.IsFinite(Size.X) || !float.IsFinite(Size.Y) || Size.X <= 0f || Size.Y <= 0f)
		{
			return;
		}
		float u = BattleUnit;
		// Keep the existing authored gutters when no cutout is present. Insets
		// stored by ApplySafeArea also contain its ordinary edge padding.
		float edge = (float)Math.Ceiling(Sizing.Edge);
		float insetLeft = Math.Max(0f, (_safeMargin?.GetThemeConstant("margin_left") ?? edge) - edge);
		float insetRight = Math.Max(0f, (_safeMargin?.GetThemeConstant("margin_right") ?? edge) - edge);
		float safeLeft = Math.Clamp(insetLeft, 0f, Size.X);
		float safeRight = Math.Clamp(Size.X - insetRight, safeLeft, Size.X);
		float safeWidth = safeRight - safeLeft;
		if (safeWidth <= 0f) return; // No drawable horizontal safe area yet.
		float ledgerEdge = Math.Min(24f * u, safeWidth * 0.1f);
		float intentEdge = Math.Min(8f * u, safeWidth * 0.1f);
		float ledgerAvailable = safeWidth - 2f * ledgerEdge;
		float intentAvailable = safeWidth - 2f * intentEdge;
		float bottom = Size.Y - (float)(PhoneLayout ? 293 : 285) * u;
		bool style04Formation = _uxEnemies.Count > 0 && (OS.GetCmdlineUserArgs().Contains("--enemy-style04-batch1") || _uxEnemies.Any(h => UsesStyle04EnemyPresentation(state.Enemies[h.Index].EnemyId)));
        float formationLeft = Math.Max(safeLeft + 16f*u, Size.X*(style04Formation && _uxEnemies.Count==3 ? .34f : .44f));
        float formationRight = safeRight - 24f*u;
        float gap = style04Formation ? (formationRight-formationLeft)/Math.Max(1,_uxEnemies.Count)/u : UxEnemyGap();
        if(style04Formation)
        {
            // Dead gameplay slots keep their IDs but no longer consume HUD width.
            var liveCenters=_uxEnemies.Select(h=>_enemyProjection?.Invoke(h.Index)?.Feet.X).Where(x=>x.HasValue).Select(x=>x.Value).OrderBy(x=>x).ToArray();
            for(int i=1;i<liveCenters.Length;i++)gap=Math.Min(gap,(liveCenters[i]-liveCenters[i-1])/u);
        }
		bool uniformStack = _uxEnemies.Any(((int Index, EnemyDropTarget Hit, PanelContainer Intent, PanelContainer Ledger, SourceHealthBar Bar, SourceHudLabel Hp, Label Title) tuple) => SourceIntentLayout.ForState(state, tuple.Index, PhoneLayout, gap, uniformStack: false, _textScale)?.Stack ?? false);
        float fixedIntentTop = 60f*u;
        float fixedLedgerTop = 0f;
        float fixedLedgerHeight = ((PhoneLayout?36f:40f)*Math.Max(1f,_textScale)+6f)*u;
        if (style04Formation)
        {
            var header = _shell?.GetNodeOrNull<Control>("BattleHeader");
            if (header != null && header.IsVisibleInTree())
                fixedIntentTop = Math.Max(60f*u, header.GetChildren().OfType<Control>().Where(c=>c.IsVisibleInTree()).Select(c=>c.GetGlobalRect().End.Y).DefaultIfEmpty(0f).Max()+6f*u);
            float rowWidth = Math.Min(SourceEnemyLedgerWidth(gap)*u, Math.Max(1f,(formationRight-formationLeft)/Math.Max(1,_uxEnemies.Count)-8f*u));
            float rowHeight = fixedLedgerHeight;
            // Reserve the formation's whole authored move set, not this turn's
            // selected intent. Optional ledger forecasts also own a fixed row.
            // This prevents buff/attack and one/two-line turns resizing bodies.
            float ts = Math.Max(1f,_textScale);
            float maxIntentHeight=(Math.Max(PhoneLayout?32f:44f,(PhoneLayout?25f:32f)*ts)+4f)*u;
            foreach (var item in _uxEnemies)
            foreach (var move in Content.GetEnemyDef(state.Enemies[item.Index].EnemyId)?.Moves ?? System.Array.Empty<EnemyMove>())
            {
                // Whole authored move set reserves stable row count. Extra numeric
                // width covers ordinary buffs without resizing the actor every turn.
                var preview=new EnemyIntent { Kind=move.Kind, Actual=Math.Max(99,move.Max??move.Min??0), Hits=move.Hits, MirrorHits=move.MirrorHits, Inflict=move.Inflict, AlsoBuff=move.AlsoBuff, AlsoDefend=move.AlsoDefend, AlsoDestroySet=move.AlsoDestroySet, StrengthPerMilled=move.StrengthPerMilled };
                var spec=SourceIntentLayout.Build(preview,false,preview.Actual,preview.Inflict,PhoneLayout,gap,_uxEnemies.Count==1,false,_textScale);
                maxIntentHeight=Math.Max(maxIntentHeight,CompactStyle04Intent(spec,gap).Height*u);
            }
            // Use the rest hand geometry, never a transient deal/hover position.
            float handTop = Size.Y - (SourceCardFace.H*UxCardScale + (PhoneLayout?-3f:11f))*u;
            float ledgerBottom = handTop - 4f*u;
            fixedLedgerTop = ledgerBottom-rowHeight;
            float actorTop = fixedIntentTop+maxIntentHeight+6f*u;
            float actorBottom = Math.Max(actorTop+24f*u,fixedLedgerTop-3f*u);
            _enemyRosterLane?.Invoke(new Rect2(new Vector2(formationLeft,actorTop)/Size,new Vector2(formationRight-formationLeft,actorBottom-actorTop)/Size));
        }
		foreach (var h in _uxEnemies)
		{
			if (GodotObject.IsInstanceValid(h.Hit))
			{
				(Vector2, Vector2)? point = _enemyProjection?.Invoke(h.Index);
				float x = point?.Item2.X ?? (Size.X * (0.6f + (float)h.Index * 0.12f));
				float authoredWidth = SourceEnemyLedgerWidth(gap) * u;
				// Reflow first. Only a genuinely tiny viewport needs uniform scaling
				// of the minimum ledger, keeping bar/pill dimensions non-negative.
				float width = Math.Max(Math.Min(authoredWidth, ledgerAvailable), Math.Min(authoredWidth, 96f * u));
				float ledgerScale = Math.Min(1f, ledgerAvailable / width);
				float displayedWidth = width * ledgerScale;
				float height = SourceEnemyLedgerHeight(width / u) * u;
				float lx = Math.Clamp(x - displayedWidth / 2f, safeLeft + ledgerEdge, Math.Max(safeLeft + ledgerEdge, safeRight - displayedWidth - ledgerEdge));
                float bodyX = lx;
				float head = point?.Item1.Y ?? (Size.Y * 0.25f);
				EnemyState enemy = _view.Combat.Enemies[h.Index];
				Texture2D tex = Art("enemies", enemy.EnemyId);
				bool style04 = UsesStyle04EnemyPresentation(enemy.EnemyId);
				if (tex != null && point.HasValue && !style04)
				{
					head = point.Value.Item2.Y - UxEnemyHeadHeight(tex) * u;
				}
				SourceIntentLayout.Spec intentSpec = SourceIntentLayout.ForState(state, h.Index, PhoneLayout, gap, uniformStack, _textScale);
				if (style04 && intentSpec != null)
                {
                    intentSpec=CompactStyle04Intent(intentSpec,gap);
                }
                SourceIntentTag.Update(h.Intent, h.Title, intentSpec, u, PhoneLayout, _textScale, compact:style04);
				float iw = h.Intent.GetMeta("source_intent_width", 72f * u).AsSingle();
				float ih = h.Intent.GetMeta("source_intent_height", 40f * u).AsSingle();
				float intentScale = Math.Min(1f, intentAvailable / Math.Max(iw, float.Epsilon));
				float displayedIntentWidth = iw * intentScale;
				float iy = Math.Max(60f * u, head - ih * intentScale - 8f * u);
                float ix = Math.Clamp(x - displayedIntentWidth / 2f, safeLeft + intentEdge, Math.Max(safeLeft + intentEdge, safeRight - displayedIntentWidth - intentEdge));
                float ledgerY = PhoneLayout ? (bottom - height * ledgerScale) : (bottom - Math.Max(height * ledgerScale, 140f * u));
                if (style04 && point.HasValue)
                {
                    width = Math.Min(316f*u, Math.Max(1f,gap*u-8f*u));
                    displayedWidth = width*ledgerScale;
                    height = fixedLedgerHeight;
                    lx = x-displayedWidth*.5f; bodyX=lx;
                    // The road sets the feet; the HP row follows, never the hand.
                    ledgerY = point.Value.Item2.Y+3f*u;
                    ix = x-displayedIntentWidth*.5f;
                    // Unity relationship: centered above this actor, never a
                    // sideways escape. Only header controls in this same x-span
                    // constrain the top clamp; empty HUD space remains usable.
                    float intentTop=8f*u;
                    var battleHeader=_shell?.GetNodeOrNull<Control>("BattleHeader");
                    if(battleHeader!=null && battleHeader.IsVisibleInTree())
                        foreach(var child in battleHeader.GetChildren().OfType<Control>().Where(c=>c.IsVisibleInTree()))
                        { var r=child.GetGlobalRect(); if(r.End.X>ix && r.Position.X<ix+displayedIntentWidth) intentTop=Math.Max(intentTop,r.End.Y+6f*u); }
                    iy = Math.Max(intentTop,head-ih*intentScale-16f*u);
                    var intentTail = h.Intent.GetNodeOrNull<Control>("IntentParts/IntentTail");
                    if (intentTail != null) intentTail.Visible=iy+ih*intentScale+11f*u<=head;
                }

				h.Hit.Position = new Vector2(bodyX, style04 ? Math.Min(head, iy) : iy);
				h.Hit.Size = new Vector2(displayedWidth, Math.Max(MinimumHit, (style04 ? ledgerY+height*ledgerScale : bottom) - h.Hit.Position.Y));
				h.Intent.Scale = Vector2.One * intentScale;
				h.Intent.GlobalPosition = new Vector2(ix, iy);
				h.Intent.Size = new Vector2(iw, ih);
				h.Ledger.Scale = Vector2.One * ledgerScale;
				h.Ledger.GlobalPosition = new Vector2(lx, ledgerY);
				h.Ledger.Size = new Vector2(width, height);
                if (style04 && h.Hit.GetParent() is Control targetField)
                {
                    foreach (var pair in new[] { (Role:"intent",Rect:new Rect2(ix,iy,displayedIntentWidth,ih*intentScale)), (Role:"ledger",Rect:new Rect2(lx,ledgerY,displayedWidth,height*ledgerScale)) })
                    {
                        if (_uxEnemyLabelTargets.TryGetValue((h.Index, pair.Role), out var labelHit) && GodotObject.IsInstanceValid(labelHit)) { labelHit.GlobalPosition=pair.Rect.Position; labelHit.Size=pair.Rect.Size; labelHit.Disabled=IsInputBlocked; }
                    }
                }

				h.Bar.Size = new Vector2(style04 ? Math.Min((SourceEnemyLedgerWidth(gap)-16f)*u,width-24f*u) : width - 16f * u, (style04 ? (PhoneLayout?18f:20f)*Math.Max(1f,_textScale) : Math.Max(20f,20f*_textScale))*u);
				if (h.Index != _preferredTarget)
				{
					_ = SourcePalette.NightEdge;
				}
				else
				{
					_ = SourcePalette.Brass;
				}
				SourceIntentTag.Highlight(h.Intent, h.Index == _preferredTarget);
				SourceBattleDecor.LedgerEdge(h.Ledger, h.Index == _preferredTarget, u);
				if (h.Bar.GetParent() is Control ledgerBody)
				{
					SourceBattleDecor.Shield(ledgerBody, h.Bar, new Vector2(8f, style04 ? 2f+(PhoneLayout?18f:20f)*Math.Max(1f,_textScale) : (PhoneLayout?29f:36f)*Math.Max(1f,_textScale))*u, h.Bar.Size.X, enemy.Block,u,_textScale,PhoneLayout,enemy:true,compact:style04);
				}
				LayoutSourceEnemyLedger(h.Index, width / u, h.Bar, h.Hp, enemy.Hp, enemy.MaxHp);
			}
		}
	}

	private void RefreshUxReadouts()
	{
		if (!float.IsFinite(Size.X) || !float.IsFinite(Size.Y) || Size.X <= 0f || Size.Y <= 0f)
		{
			_uxLastSize = Size;
			return;
		}
		GameState s = _view?.Combat;
		if ((object)s == null)
		{
			return;
		}
		PlayerState p = s.Player;
		RefreshSourceTokens();
		if (_uxHpBar != null && GodotObject.IsInstanceValid(_uxHpBar))
		{
			_uxHpBar.Hp = p.Hp;
			_uxHpBar.Maximum = p.MaxHp;
			_uxHpBar.Block = p.Block;
			_uxHpBar.IceBlock = p.IceBlock;
			_uxHpBar.PredictedLoss = ((s.HideIntents != true) ? Math.Max(0, _uxIncoming - p.Block - p.IceBlock) : 0);
			_uxHpBar.QueueRedraw();
			Set(_hpReadout, p.Hp + " / " + p.MaxHp);
		}
		if (_uxHpBar != null && GodotObject.IsInstanceValid(_uxHpBar) && _uxHpBar.GetParent() is Control hpBody && hpBody.GetParent() is Control heroPanel)
		{
			SourceBattleDecor.Shield(hpBody, _uxHpBar, new Vector2(8f, 8f) * BattleUnit, heroPanel.Size.X - 16f * BattleUnit, p.Block, BattleUnit, _textScale, PhoneLayout);
		}
		if (_uxOrb != null && GodotObject.IsInstanceValid(_uxOrb))
		{
			_uxOrb.Energy = p.Energy;
			_uxOrb.Maximum = TurnEnergyCap(s);
			_uxOrb.QueueRedraw();
		}
		RefreshSourceForecast(s);
		foreach (var h in _uxEnemies)
		{
			if (h.Index < s.Enemies.Count)
			{
				EnemyState e = s.Enemies[h.Index];
				h.Bar.Hp = e.Hp;
				h.Bar.Maximum = e.MaxHp;
				h.Bar.Block = e.Block;
				h.Bar.QueueRedraw();
				h.Hp.Text = e.Hp + " / " + e.MaxHp;
				h.Hp.RefreshSourceAlignment();
				if (h.Bar.GetParent() is Control ledgerBody)
				{
					bool compact=UsesStyle04EnemyPresentation(e.EnemyId);
                    SourceBattleDecor.Shield(ledgerBody,h.Bar,new Vector2(8f,compact?2f+(PhoneLayout?18f:20f)*Math.Max(1f,_textScale):(PhoneLayout?29f:36f)*Math.Max(1f,_textScale))*BattleUnit,h.Ledger.Size.X-16f*BattleUnit,e.Block,BattleUnit,_textScale,PhoneLayout,enemy:true,compact:compact);
				}
				RefreshSourceEnemyHp(h.Index, h.Bar, h.Hp, e.Hp, e.MaxHp);
			}
		}
		AlignUxRichLine(_uxForecast);
		AlignUxRichLine(_uxDetail);
	}

	private void BuildUxTargetBanner(PlaySelection play)
	{
        float u = BattleUnit;
        float left = 24f * u;
        float right = Size.X - 24f * u;
        var localFromGlobal = _uxBattle.GetGlobalTransform().AffineInverse();
        foreach (var enemy in _uxEnemies)
        {
            if (!GodotObject.IsInstanceValid(enemy.Intent) || !enemy.Intent.IsVisibleInTree()) continue;
            // Enemy intents may live under the battlefield container. Compare
            // their actual screen positions in this banner's parent space.
            float intentLeft = (localFromGlobal * enemy.Intent.GetGlobalRect().Position).X;
            right = Math.Min(right, intentLeft - 16f * u);
        }
        float width = Math.Min(620f * u, Math.Max(1f, right - left));
        float height = MinimumHit + 16f;
        float top = 100f * u;
        var hero = _uxBattle.GetNodeOrNull<Control>("PlayerFootLedger");
        if (hero != null)
            top = Math.Min(top, Math.Max(0f, hero.Position.Y - height - 12f * u));
        HBoxContainer row = Row(UxPanel(_uxBattle, "FieldTargetBanner", new Rect2(left, top, width, height)));
        var caption = Text(row, "対象の敵を選ぶ", 20, ModalText);
        caption.TooltipText = play.Card.Def.Name + " · 対象の敵を選ぶ";
        caption.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        caption.VerticalAlignment = VerticalAlignment.Center;
        caption.AddThemeFontOverride("font", SourceRunUiDecor.Action);
        var cancel = Button(row, "使用を取り消す", CloseAllOverlays);
        StyleModernModal(cancel, ModernModalTheme());
        cancel.AddThemeFontOverride("font", SourceRunUiDecor.Action);
	}

	private void SyncUxHand()
	{
		GameState s = InputCombat ?? _view?.Combat;
		if (s == null || _uxHand == null)
		{
			return;
		}
		_uxHand.Size = Size;
		float u = BattleUnit;
		float cs = UxCardScale;
		int n = s.Player.Hand.Count;
		float cw = SourceCardFace.W * cs;
		float shift = 0f;
		float spacing;
		if (PhoneLayout)
		{
			float sw = Size.X / u;
			float tilt = Math.Max(0f, (float)(n - 1) / 2f * 3f) * (float)Math.PI / 180f;
			float over = Math.Max(0f, SourceCardFace.H * .5f * Mathf.Sin(tilt) - SourceCardFace.W * .5f * (1f - Mathf.Cos(tilt))) * cs;
			float left = 198f + over;
			// Tall modern cards must not put their resting title beneath the hero ledger.
			var heroLedger = _uxBattle?.GetNodeOrNull<Control>("PlayerFootLedger");
			if (heroLedger != null && heroLedger.IsVisibleInTree())
			{
				Vector2 ledgerRight = _uxHand.GetGlobalTransformWithCanvas().AffineInverse() * heroLedger.GetGlobalRect().End;
				left = Math.Max(left, ledgerRight.X / u + 12f + over);
			}
			float right = RunSafeRect.End.X / u - 281f;
			spacing = ((n > 1) ? Math.Max(20f, Math.Min(cw + 12f, (right - left - cw) / (float)(n - 1))) : 0f);
			shift = (left + right) / 2f - sw / 2f;
		}
		else
		{
			spacing = ((n == 0) ? 0f : Math.Min(cw + 12f, 1180f / (float)n));
		}
		HashSet<string> live = s.Player.Hand.Select((CardInstance cardInstance) => cardInstance.Uid).ToHashSet();
		string[] removed = _uxTiles.Keys.Where((string x) => !live.Contains(x)).ToArray();
		bool removedPlayed = UxRemovedPlayedCard(removed);
		int newOrdinal = 0;
		for (int ordinal = 0; ordinal < removed.Length; ordinal++)
		{
			UxRetireRemovedHand(removed[ordinal], s, ordinal);
		}
		for (int i = 0; i < n; i++)
		{
			CardInstance c = s.Player.Hand[i];
			string uid = c.Uid;
			bool flag = !_uxTiles.TryGetValue(uid, out var tile);
			if (flag)
			{
				UxCancelAuthoredHandExit(uid);
				UxCancelHandExit(uid);
				tile = new CardTile
				{
					Name = "Hand_" + uid,
					CardUid = uid,
					IsBattleHand = true,
					Text = "",
					MouseFilter = MouseFilterEnum.Stop,
					Size = new Vector2(SourceCardFace.W, SourceCardFace.H),
					CustomMinimumSize = new Vector2(SourceCardFace.W, SourceCardFace.H),
					PivotOffset = new Vector2(SourceCardFace.W * .5f, SourceCardFace.H * .5f)
				};
				_uxHand.AddChild(tile, forceReadableName: false, InternalMode.Disabled);
				_uxTiles[uid] = tile;
				tile.TouchOwnsInput = () => TouchOwnsNativeHand;
				tile.PreviewPressed = () => HoverUxHand(uid, enter: true);
				tile.MouseEntered += () =>
				{
					UxNativeHandHover(uid, tile, enter: true);
				};
				tile.MouseExited += () =>
				{
					UxNativeHandHover(uid, tile, enter: false);
				};
				tile.Pressed += () =>
				{
					if (!IsInputBlocked && _overlay == null && !tile.ConsumeInspection())
					{
						CardInstance cardInstance = InputCombat?.Player.Hand.FirstOrDefault((CardInstance x) => x.Uid == uid);
						if ((object)cardInstance != null)
						{
							CardActions(cardInstance);
						}
					}
				};
				tile.Inspect = () =>
				{
					CardInstance cardInstance = InputCombat?.Player.Hand.FirstOrDefault((CardInstance x) => x.Uid == uid);
					if (cardInstance != null)
					{
						InspectCard(cardInstance);
					}
				};
				tile.SecondaryAction = () =>
				{
					GameState inputCombat = InputCombat;
					if (inputCombat != null && !IsInputBlocked && SetBase.CanSetCard(inputCombat, uid))
					{
						Do(new Command_SetCard
						{
							CardUid = uid
						});
					}
					else
					{
						tile.Inspect?.Invoke();
					}
				};
				tile.DragPayload = () => new Dictionary
				{
					{ "uid", uid },
					{ "epoch", _uxEpoch }
				};
				tile.DragStarted = () =>
				{
					UxBeginHandDragPose(uid, tile);
				};
				tile.DragEnded = () =>
				{
					UxNativeHandDragEnded(uid, tile);
				};
				tile.DragPreview = () => MakeUxDragPreview(uid);
			}
			bool playable = FlowRules.PlayBlocked(s, c) == null;
			bool settable = s.ReactionMode != "hold-manual" && SetBase.CanSetCard(s, uid);
			tile.AllowDrag = playable && !IsInputBlocked;
			tile.Disabled = IsInputBlocked;
			tile.SetMeta("definition_id", c.Def.Id);
			SourceCardFace.Fill(tile, c, s, _textScale, PhoneLayout, _preferredTarget, Effects.EffectiveCost(s, c), s.Player.ImpulseUids.Contains(uid) ? "衝動" : null, playable | settable);
			float center = (float)(n - 1) / 2f;
			float dy = Math.Abs((float)i - center) * 10f;
			if (!playable && !settable && s.Phase == "player-turn")
			{
				dy += 16f;
			}
			float handY = (PhoneLayout ? (-3) : 11);
			float cx = Size.X / 2f + (((float)i - center) * spacing + shift) * u;
			float cy = Size.Y - (handY + SourceCardFace.H * .5f * cs) * u + dy * u;
			Vector2 pos = new Vector2(cx - SourceCardFace.W * .5f, cy - SourceCardFace.H * .5f);
			float rot = ((float)i - center) * 3f;
			_uxSeats[uid] = (pos, rot, i);
			if (flag)
			{
				UxStartHandDeal(uid, tile, SourceHandDealMotion.Delay(newOrdinal++, removed.Length != 0, removedPlayed));
			}
			else if (!UxKeepHandDeal(uid, tile) && !UxKeepHandHover(uid, tile) && _uxDrag != uid && _uxHover != uid)
			{
				tile.Scale = Vector2.One * (cs * u);
				tile.Position = pos;
				tile.RotationDegrees = rot;
			}
			if (_uxDrag != uid && _uxHover != uid)
			{
				_uxHand.MoveChild(tile, _uxHand.GetChildCount() - 1);
				tile.ZIndex = 0;
			}
		}
		UxRestoreHandExitOrder();
		UxRestoreAuthoredHandExitOrder();
		if (_uxHover != null && _uxTiles.ContainsKey(_uxHover))
		{
			HoverUxHand(_uxHover, enter: true);
		}
	}

	private Control MakeUxDragPreview(string uid)
	{
		return new Control
		{
			MouseFilter = MouseFilterEnum.Ignore,
			CustomMinimumSize = Vector2.One
		};
	}

	private bool UxHandPreviewContainsPointer(string uid, CardTile tile)
	{
		Vector2 pointer = GetGlobalMousePosition();
		if (tile.GetGlobalRect().HasPoint(pointer)) return true;
		if (!_uxSeats.TryGetValue(uid, out var seat) || _uxHand == null) return false;
		float angle = Mathf.DegToRad(seat.Rotation);
		Vector2 full = tile.Size * (UxCardScale * BattleUnit);
		Vector2 bounds = new Vector2(Math.Abs(Mathf.Cos(angle)) * full.X + Math.Abs(Mathf.Sin(angle)) * full.Y,
			Math.Abs(Mathf.Sin(angle)) * full.X + Math.Abs(Mathf.Cos(angle)) * full.Y);
		Vector2 center = _uxHand.GlobalPosition + seat.Pos + tile.PivotOffset;
		return new Rect2(center - bounds / 2, bounds).HasPoint(pointer);
	}

	private void HoverUxHand(string uid, bool enter)
	{
		if (!_uxTiles.TryGetValue(uid, out var tile) || !_uxSeats.TryGetValue(uid, out (Vector2, float, int) seat))
		{
			return;
		}
		if (enter)
		{
			if (!IsInputBlocked && _overlay == null && _uxDrag == null)
			{
				if (_uxHover != null && _uxHover != uid)
				{
					HoverUxHand(_uxHover, enter: false);
				}
				_uxHover = uid;
				_uxHand.MoveChild(tile, _uxHand.GetChildCount() - 1);
				tile.ZIndex = 40; // Active reading card sits above the battle HUD (30), below modals (100).
				float previewScale = 1.18f * UxCardWidthScale * BattleUnit;
				float left = (_safeMargin?.GetThemeConstant("margin_left") ?? 0) + 12f * BattleUnit;
				float right = Size.X - (_safeMargin?.GetThemeConstant("margin_right") ?? 0) - 12f * BattleUnit;
				float top = (_safeMargin?.GetThemeConstant("margin_top") ?? 0) + 12f * BattleUnit;
				float bottom = Size.Y - (_safeMargin?.GetThemeConstant("margin_bottom") ?? 0) - 12f * BattleUnit;
				previewScale = Math.Min(previewScale, Math.Min(Math.Max(1f, right - left) / tile.Size.X, Math.Max(1f, bottom - top) / tile.Size.Y));
				Vector2 half = tile.Size * previewScale * .5f;
				Vector2 center = seat.Item1 + tile.PivotOffset - new Vector2(0, 90f * BattleUnit);
				center.X = Mathf.Clamp(center.X, Math.Min(left + half.X, (left + right) / 2), Math.Max(right - half.X, (left + right) / 2));
				center.Y = Mathf.Clamp(center.Y, Math.Min(top + half.Y, (top + bottom) / 2), Math.Max(bottom - half.Y, (top + bottom) / 2));
				UxStartHandHover(uid, tile, center - tile.PivotOffset, Vector2.One * previewScale, 0f);
			}
		}
		else if (!(_uxDrag == uid))
		{
			if (_uxHover == uid)
			{
				_uxHover = null;
			}
			tile.ZIndex = 0;
			UxStartHandHover(uid, tile, seat.Item1, Vector2.One * (UxCardScale * BattleUnit), seat.Item2);
			RestoreUxHandOrder();
		}
	}

	private void RestoreUxHandOrder()
	{
		foreach (KeyValuePair<string, (Vector2, float, int)> seat in _uxSeats.OrderBy((KeyValuePair<string, (Vector2 Pos, float Rotation, int Order)> x) => x.Value.Order))
		{
			if (_uxTiles.TryGetValue(seat.Key, out var live))
			{
				_uxHand.MoveChild(live, _uxHand.GetChildCount() - 1);
			}
		}
	}

	private void StartHandDrag(string uid)
	{
		if (_uxTiles.TryGetValue(uid, out var tile))
		{
			UxBeginHandDragPose(uid, tile);
		}
	}

	private void EndHandDrag()
	{
		UxReleaseHandDragPose(returnToSeat: true);
	}

	private string DecodeHandDrag(Variant data)
	{
		if (data.VariantType != Variant.Type.Dictionary)
		{
			return null;
		}
		Dictionary d = data.AsGodotDictionary();
		if (!d.ContainsKey("uid") || !d.ContainsKey("epoch") || d["epoch"].AsInt64() != _uxEpoch)
		{
			return null;
		}
		return d["uid"].AsString();
	}

	private void UpdateHandAim()
	{
		if (_uxAim == null || !GodotObject.IsInstanceValid(_uxAim))
		{
			return;
		}
		if (!UxHandDragIsCurrent(_uxHandDragPose) || IsInputBlocked || _overlay != null || !_uxTiles.TryGetValue(_uxDrag, out var tile))
		{
			_uxAim.Active = false;
			_uxAim.QueueRedraw();
			return;
		}
		Vector2 pointer = GetGlobalMousePosition();
		_uxAim.Active = _uxHandDragPose.StationaryAim && PhoneLayout && pointer.Y < Size.Y * 0.64f;
		_uxAim.End = pointer;
		_uxAim.Start = tile.GetGlobalTransformWithCanvas() * new Vector2(SourceCardFace.W * .5f, -8f);
		_uxAim.Unit = BattleUnit;
		_uxAim.Size = Size;
		List<Rect2> masks = _uxEnemies.SelectMany(((int Index, EnemyDropTarget Hit, PanelContainer Intent, PanelContainer Ledger, SourceHealthBar Bar, SourceHudLabel Hp, Label Title) x) => new Rect2[2]
		{
			x.Intent.GetGlobalRect(),
			x.Ledger.GetGlobalRect()
		}).ToList();
		int over = _enemyResolver?.Invoke(pointer) ?? (-1);
		foreach (var e in _uxEnemies)
		{
			if (e.Index != over)
			{
				(Vector2, Vector2)? p = _enemyProjection?.Invoke(e.Index);
				if (p.HasValue)
				{
					masks.Add(new Rect2(p.Value.Item1.X - 40f * BattleUnit, p.Value.Item1.Y, 80f * BattleUnit, p.Value.Item2.Y - p.Value.Item1.Y));
				}
			}
		}
		_uxAim.Blockers = masks;
		Vector2? hero = _uxLeaderProjection?.Invoke();
		SourceAimArrow uxAim = _uxAim;
		Rect2? leaderBody;
		if (hero.HasValue)
		{
			Vector2 v = hero.GetValueOrDefault();
			leaderBody = new Rect2(v.X - 40f * BattleUnit, v.Y - 150f * BattleUnit, 80f * BattleUnit, 145f * BattleUnit);
		}
		else
		{
			leaderBody = null;
		}
		uxAim.LeaderBody = leaderBody;
		SourceAimArrow uxAim2 = _uxAim;
		object note;
		if (over < 0)
		{
			GameState inputCombat = InputCombat;
			if ((object)inputCombat != null && inputCombat.Enemies.Count((EnemyState x) => x.Hp > 0) > 1)
			{
				note = ((_preferredTarget >= 0) ? ("離すと " + (_preferredTarget + 1) + " の敵へ") : "離すと 敵を選ぶ");
				goto IL_0342;
			}
		}
		note = "";
		goto IL_0342;
		IL_0342:
		uxAim2.Note = (string)note;
		if (over >= 0 && over != _preferredTarget)
		{
			foreach (var e2 in _uxEnemies)
			{
				if (e2.Index != over)
				{
					_ = SourcePalette.NightEdge;
				}
				else
				{
					_ = SourcePalette.Brass;
				}
				SourceIntentTag.Highlight(e2.Intent, e2.Index == over);
				SourceBattleDecor.LedgerEdge(e2.Ledger, e2.Index == over, BattleUnit);
			}
		}
		_uxAim.QueueRedraw();
	}

	private void UxProcess(double delta)
	{
        if (_touchHand.SelectedUid != null && !TouchHandContextCurrent()) CancelTouchHand();
		if (!float.IsFinite(Size.X) || !float.IsFinite(Size.Y) || Size.X <= 0f || Size.Y <= 0f)
		{
			_uxLastSize = Size;
			return;
		}
		if (_uxLastSize != Size)
		{
			_uxLastSize = Size;
			if (!_uxRelayoutQueued)
			{
				_uxRelayoutQueued = true;
				EndHandDrag();
				CallDeferred("UxRelayout");
			}
		}
		UxTickHandDragPose();
		UxTickHandDeals(delta);
		UxTickHandHovers(delta);
		UxTickHandExits(delta);
		UxCheckAuthoredHandExits();
		PositionUxEnemies();
		UpdateHandAim();
		AlignUxRichLine(_uxForecast);
		AlignUxRichLine(_uxDetail);
	}

	private static void AlignUxRichLine(RichTextLabel label)
	{
		if (label != null && GodotObject.IsInstanceValid(label) && label.HasMeta("source_text_rect"))
		{
			Rect2 rect = label.GetMeta("source_text_rect").AsRect2();
			float height = Math.Max(rect.Size.Y, label.GetContentHeight());
			label.Size = new Vector2(rect.Size.X, height);
			label.Position = rect.Position + new Vector2(0f, (rect.Size.Y - height) / 2f);
		}
	}

	private void UxRelayout()
	{
		_uxRelayoutQueued = false;
		if (IsInsideTree() && CurrentRoute == "combat")
		{
			Rebuild();
		}
	}

	private RunState UxOrdinaryProjection(RunState current)
	{
		return SourceHudProjection.Ordinary(current, _presentedState?.Invoke() ?? _view?.Combat);
	}

	private void UxPresentEvent(GameEvent evt)
	{
		SessionChange tokenEventOwner = _uxEventOwner?.Invoke(evt);
		SourceTokenFrame tokenFrame = CapturedSourceTokenFrame;
		SessionChange owner = tokenEventOwner ?? _activeChange;
		Func<GameEvent, bool> uxOrdinary = _uxOrdinary;
		if (uxOrdinary != null && uxOrdinary(evt))
		{
			RunState live = _session?.CurrentRun;
			if ((object)live != null)
			{
				_view = UxOrdinaryProjection(live);
			}
			RefreshUxReadouts();
			return;
		}
		if (owner != null)
		{
			_activeChange = owner;
			_costPresented = _uxPaid.Contains(owner.Revision);
			if (_uxIncomingOwner != owner.Revision)
			{
				GameState previous = owner.Previous?.Combat;
				if ((object)previous != null)
				{
					_uxIncomingOwner = owner.Revision;
					_uxIncoming = Effects.IncomingTotal(previous);
					_uxSkipFirst = previous.NullifyNextAttack == true;
					_uxLanded = false;
				}
			}
		}
		PresentEventLegacy(evt);
		if (_costPresented && owner != null)
		{
			_uxPaid.Add(owner.Revision);
		}
		if (_uxPhase != null && GodotObject.IsInstanceValid(_uxPhase))
		{
			if (evt is GameEvent_TurnEnded)
			{
				_uxPhase.Text = "敵の番";
			}
			else if (evt is GameEvent_EnemyActionExecuting e)
			{
				_uxPhase.Text = "敵の番 " + (e.EnemyIndex + 1) + "/" + (_view?.Combat?.Enemies.Count).GetValueOrDefault();
			}
			else if (evt is GameEvent_TurnStarted)
			{
				_uxPhase.Text = "あなたの番";
			}
		}
		RefreshSourceTokenEvent(evt, tokenFrame, tokenEventOwner);
		RefreshUxReadouts();
	}

	private void UxNumericContact(GameEvent evt)
	{
		SessionChange owner = _uxEventOwner?.Invoke(evt) ?? _activeChange;
		int? amount = _uxIncomingContact?.Invoke(evt);
		if (amount.HasValue && owner != null)
		{
			if (_uxIncomingOwner != owner.Revision)
			{
				GameState prev = owner.Previous?.Combat;
				if ((object)prev != null)
				{
					_uxIncomingOwner = owner.Revision;
					_uxIncoming = Effects.IncomingTotal(prev);
					_uxSkipFirst = prev.NullifyNextAttack == true;
					_uxLanded = false;
				}
			}
			_uxLanded = true;
			if (_uxSkipFirst)
			{
				_uxSkipFirst = false;
			}
			else
			{
				_uxIncoming = Math.Max(0, _uxIncoming - Math.Max(0, amount.Value));
			}
		}
		PresentNumericContactLegacy(evt);
		RefreshUxReadouts();
	}

	public void SetEffectiveQuality(Func<string> read)
	{
		_effectiveQuality = read;
	}

	private void BuildSavedRunSummary()
	{
		if (_session == null)
		{
			return;
		}
		string primary = _session.SaveDirectory.TrimEnd('/') + "/run.json";
		string backup = primary + ".bak";
		ulong stamp = (FileAccess.FileExists(primary) ? FileAccess.GetModifiedTime(primary) : 0);
		ulong backupStamp = (FileAccess.FileExists(backup) ? FileAccess.GetModifiedTime(backup) : 0);
		string key = primary + ":" + stamp + ":" + backupStamp + ":" + _session.Revision;
		if (_savedSummaryKey != key)
		{
			_savedSummaryKey = key;
			_savedSummaryRun = null;
			_savedSummaryPath = null;
			_savedSummaryBackup = false;
			(RunState, bool) preview = SourceRunScreenPolicy.ReadSavedSummary(new SessionSaveStore(new GodotSessionFiles(), _session.SaveDirectory));
			_savedSummaryRun = preview.Item1;
			_savedSummaryBackup = preview.Item2;
			ref string savedSummaryPath = ref _savedSummaryPath;
			string text;
			if (preview.Item1 == null)
			{
				text = null;
			}
			else
			{
				text = (preview.Item2 ? backup : primary);
			}
			savedSummaryPath = text;
		}
		RunState run = _session.CurrentRun ?? _savedSummaryRun;
		if (run == null)
		{
			Text(_body, "進行中のラン · セーブの要約を読み取れません。続きからで確認できます。", 18, Muted);
			return;
		}
		PanelContainer panel = new PanelContainer
		{
			Name = "SourceSavedRunSummary"
		};
		panel.AddThemeStyleboxOverride("panel", Box(Panel, Gold));
		_body.AddChild(panel, forceReadableName: false, InternalMode.Disabled);
		HBoxContainer row = Row(panel);
		Picture(row, "leaders", run.LeaderId + "_icon", new Vector2(56f, 56f));
		VBoxContainer text2 = Column(row);
		Text(text2, "進行中のラン", 16, Gold);
		Text(text2, SourceRunScreenPolicy.SaveSummary(run), 20);
		string saved = "";
		if (_savedSummaryPath != null)
		{
			ulong time = FileAccess.GetModifiedTime(_savedSummaryPath);
			if (time != 0)
			{
				saved = "\u3000最終保存 " + DateTimeOffset.FromUnixTimeSeconds((long)time).ToLocalTime().ToString("M/d HH:mm");
			}
		}
		Text(text2, SourceRunScreenPolicy.SaveDetail(run) + saved + (_session.AutoSave ? "\u3000自動保存" : "\u3000自動保存オフ") + (_savedSummaryBackup ? "\u3000予備セーブ" : ""), 16, Muted);
	}

	private void ToggleFullscreen()
	{
		if (DesktopVideoAvailable)
		{
			SetDesktopMode(!IsFullscreenMode);
		}
	}

	private void SetDesktopMode(bool full)
	{
		if (DesktopVideoAvailable)
		{
			if (full && !IsFullscreenMode)
			{
				_windowResolution = DisplayServer.WindowGetSize();
			}
			DisplayServer.WindowSetMode((DisplayServer.WindowMode)(full ? 3 : 0));
			if (!full)
			{
				DisplayServer.WindowSetSize(_windowResolution);
			}
			FinishVideoChange();
		}
	}

	private void SelectDesktopResolution(Vector2I resolution)
	{
		if (DesktopVideoAvailable && resolution.X > 0 && resolution.Y > 0)
		{
			_windowResolution = resolution;
			if (!IsFullscreenMode)
			{
				DisplayServer.WindowSetSize(resolution);
			}
			FinishVideoChange();
		}
	}

	private async void FinishVideoChange()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		if (IsInsideTree())
		{
			SaveSettings();
			Rebuild();
		}
	}

	private void BuildDesktopVideoSettings(VBoxContainer box)
	{
		if (!DesktopVideoAvailable)
		{
			return;
		}
		Text(box, "画面", 21, Gold);
		HFlowContainer mode = Flow(box);
		Button(mode, ((!IsFullscreenMode) ? "● " : "○ ") + "ウィンドウ", () =>
		{
			SetDesktopMode(full: false);
		});
		Button(mode, (IsFullscreenMode ? "● " : "○ ") + "フルスクリーン", () =>
		{
			SetDesktopMode(full: true);
		});
		Text(box, "F11 か Alt+Enter でも切り替え", 17, Muted);
		Vector2I actual = DisplayServer.WindowGetSize();
		Text(box, $"いま {actual.X} × {actual.Y} · " + (IsFullscreenMode ? "フルスクリーン（画面の大きさ）" : "ウィンドウ"), 18, Muted);
		Text(box, "解像度（ウィンドウの大きさ。フルスクリーンは画面に合わせる）", 17, Muted);
		HFlowContainer resolutions = Flow(box);
		Vector2I screen = DisplayServer.ScreenGetSize(DisplayServer.WindowGetCurrentScreen());
		foreach (var choice in SourceRunScreenPolicy.Resolutions(screen.X, screen.Y))
		{
			Vector2I resolution = new Vector2I(choice.Width, choice.Height);
			bool selected = _windowResolution == resolution;
			Button(resolutions, (selected ? "● " : "○ ") + $"{resolution.X} × {resolution.Y}", () =>
			{
				SelectDesktopResolution(resolution);
			}, enabled: true, selected ? new Color?(Gold) : ((Color?)null));
		}
	}

	private void CaptureSourceEnemyLedger(GameState state)
	{
		_uxEnemyHudSnapshot = SourceEnemyHudSnapshot.SelectForBuild(_uxEnemyHudSnapshot, state, _session?.CurrentRun?.Combat, _uxEpoch, _activeChange?.Revision ?? (-1), IsInputBlocked);
		_uxEnemyHudBoardRevision = _uxEnemyHudSnapshot.BoardRevision;
		_uxLedgerSeats.Clear();
		_uxLedgerHeightWidth = -1f;
		SourceEnemyHudSnapshot capture = _uxEnemyHudSnapshot;
		_uxBattle.TreeExiting += () =>
		{
			if (_uxEnemyHudSnapshot == capture)
			{
				_uxEnemyHudSnapshot = null;
				_uxLedgerSeats.Clear();
				_uxLedgerHeightWidth = -1f;
			}
		};
	}

	private void BuildSourceEnemyLedger(Control body, int index, string id, SourceHealthBar bar, SourceHudLabel hp)
	{
		if (_uxEnemyHudSnapshot.TryRead(_uxEnemyHudSnapshot, _uxEpoch, _uxEnemyHudBoardRevision, index, id, out GameState state, out EnemyState enemy))
		{
			EnemyDef def = Content.GetEnemyDef(id);
			SourceHudLabel name = new SourceHudLabel
			{
				Name = "EnemyName_" + index,
				MouseFilter = MouseFilterEnum.Ignore,
				ClipText = true,
				TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
				AutowrapMode = TextServer.AutowrapMode.Off
			};
			body.AddChild(name, forceReadableName: false, InternalMode.Disabled);
			Control mask = new Control
			{
				Name = "EnemyStatusMask_" + index,
				ClipContents = true,
				MouseFilter = MouseFilterEnum.Ignore
			};
			body.AddChild(mask, forceReadableName: false, InternalMode.Disabled);
			Control forecast = new Control
			{
				Name = "EnemyForecast_" + index,
				ClipContents = true,
				MouseFilter = MouseFilterEnum.Ignore
			};
			body.AddChild(forecast, forceReadableName: false, InternalMode.Disabled);
			ColorRect marker = new ColorRect
			{
				Name = "EnemyInterruptMark_" + index,
				Color = SourcePalette.Ink,
				MouseFilter = MouseFilterEnum.Ignore,
				Visible = false
			};
			bar.AddChild(marker, forceReadableName: false, InternalMode.Disabled);
			bar.MoveChild(marker, 0);
			ColorRect core = new ColorRect
			{
				Name = "MarkCore",
				Color = SourcePalette.Brass,
				MouseFilter = MouseFilterEnum.Ignore
			};
			marker.AddChild(core, forceReadableName: false, InternalMode.Disabled);
			hp.ZIndex = 1;
			_uxLedgerSeats[index] = new SourceLedgerSeat
			{
				Snapshot = _uxEnemyHudSnapshot,
				Id = id,
				Body = body,
				Name = name,
				Mask = mask,
				Forecast = forecast,
				Pills = SourceEnemyLedgerModel.Pills(state, index, def),
				Mark = SourceEnemyLedgerModel.Mark(def, enemy),
				Marker = marker,
				MarkerCore = core
			};
		}
	}

	private bool ReadSourceEnemyLedger(int index, out SourceLedgerSeat seat, out GameState state, out EnemyState enemy)
	{
		state = null;
		enemy = null;
		if (_uxLedgerSeats.TryGetValue(index, out seat))
		{
			return seat.Snapshot.TryRead(_uxEnemyHudSnapshot, _uxEpoch, _uxEnemyHudBoardRevision, index, seat.Id, out state, out enemy);
		}
		return false;
	}

	private float SourceEnemyLedgerWidth(float gap)
	{
		return SourceEnemyLedgerModel.Width(gap, _uxEnemyHudSnapshot?.State.Enemies.Count ?? 0, PhoneLayout);
	}

	private float SourceEnemyLedgerHeight(float width)
	{
		if (_uxLedgerHeightWidth == width && _uxLedgerHeightScale == _textScale && _uxLedgerHeightPhone == PhoneLayout)
		{
			return _uxLedgerHeight;
		}
		GameState s = _uxEnemyHudSnapshot?.ForecastEligibilityState;
		if (s == null)
		{
			return SourceEnemyLedgerModel.Height(forecast: false, PhoneLayout);
		}
		bool forecast = false;
		for (int i = 0; i < s.Enemies.Count; i++)
		{
			if (s.Enemies[i].Hp > 0 && SourceEnemyLedgerModel.Forecast(s, i, Content.GetEnemyDef(s.Enemies[i].EnemyId), width, PhoneLayout, _textScale).Count > 0)
			{
				forecast = true;
				break;
			}
		}
		_uxLedgerHeightWidth = width;
		_uxLedgerHeightScale = _textScale;
		_uxLedgerHeightPhone = PhoneLayout;
		return _uxLedgerHeight = SourceEnemyLedgerModel.Height(forecast, PhoneLayout) * Math.Max(1f, _textScale);
	}

	private void LayoutSourceEnemyLedger(int index, float width, SourceHealthBar bar, SourceHudLabel hp, int displayedHp, int displayedMax)
	{
		if (!ReadSourceEnemyLedger(index, out var seat, out var state, out var _))
		{
			return;
		}
		float u = BattleUnit;
		float ts = Math.Max(1f, _textScale);
        bool compact=UsesStyle04EnemyPresentation(seat.Id);
        float top=(compact?2f:(PhoneLayout?2f:4f)*ts)*u;
        float nameH=(compact?(PhoneLayout?18f:20f):(PhoneLayout?25f:30f))*u*ts;
		string key = $"{width:R}|{u:R}|{_textScale:R}|{PhoneLayout}";
		int pf;
		Font pillFont;
		if (seat.LayoutKey != key)
		{
			seat.LayoutKey = key;
			EnemyDef def = Content.GetEnemyDef(seat.Id);
			int nf = (int)Math.Round((float)(PhoneLayout ? 15 : 19) * u * _textScale);
			pf = (int)Math.Round((float)(PhoneLayout ? 15 : 13) * u * _textScale);
			Font nameFont = SourceRunUiDecor.Display;
			pillFont = SourceRunUiDecor.Action;
			string obj = ((state.Enemies.Count > 1 && index < "①②③④⑤⑥⑦⑧".Length) ? "①②③④⑤⑥⑦⑧"[index].ToString() : "");
			string name = obj + def.Name;
			string min = obj + ((def.Name.Length > 2) ? def.Name.Substring(0, 2) : def.Name) + "…";
			SourceEnemyLedgerModel.Packing pack = SourceEnemyLedgerModel.Pack(seat.Pills, width, state.Enemies.Count, nameFont.GetStringSize(name, HorizontalAlignment.Left, -1f, nf, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal).X / u, nameFont.GetStringSize(min, HorizontalAlignment.Left, -1f, nf, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal).X / u, measure);
            float moreWidth=0f; int hiddenPills=0;
            if(compact)
            {
                // Full identity owns the first row. Status is shortened only in
                // remaining space; the ellipsis and existing hold detail disclose overflow.
                float fullName=nameFont.GetStringSize(name,HorizontalAlignment.Left,-1f,nf).X/u;
                float cap=Math.Max(0f,width-16f-fullName-4f);
                int form=1;float all=seat.Pills.Select(p=>SourceEnemyLedgerModel.PillWidth(p,form,measure)).Sum()+Math.Max(0,seat.Pills.Count-1)*4f;
                if(all>cap)form=2;
                all=seat.Pills.Select(p=>SourceEnemyLedgerModel.PillWidth(p,form,measure)).Sum()+Math.Max(0,seat.Pills.Count-1)*4f;
                moreWidth=all>cap?Math.Min(cap,Math.Max(18f,measure("…")+6f)):0f;
                var shown=new List<int>();float used=0f;
                for(int j=0;j<seat.Pills.Count;j++)
                { float w=SourceEnemyLedgerModel.PillWidth(seat.Pills[j],form,measure);float next=used+(shown.Count>0?4f:0f)+w;
                  if(next+(moreWidth>0?moreWidth+4f:0f)<=cap){shown.Add(j);used=next;} }
                hiddenPills=seat.Pills.Count-shown.Count;if(hiddenPills==0)moreWidth=0f;
                float reserve=used+(moreWidth>0?moreWidth+(shown.Count>0?4f:0f):0f);
                pack=new SourceEnemyLedgerModel.Packing(form,reserve,Math.Max(0f,width-16f-(reserve>0?reserve+4f:0f)),shown);
            }
			seat.Name.Text = name;
			seat.Name.SourceTextRect = new Rect2(8f * u, top, pack.NameWidth * u, nameH);
			seat.Name.AddThemeFontOverride("font", nameFont);
			seat.Name.AddThemeFontSizeOverride("font_size", nf);
			seat.Name.AddThemeColorOverride("font_color", SourcePalette.Paper);
			seat.Name.RefreshSourceAlignment();
			seat.Mask.Position = new Vector2((width - 6f - pack.Reserve) * u, top);
			seat.Mask.Size = new Vector2(Math.Max(0f, pack.Reserve * u), nameH);
			ClearSourceLedgerChildren(seat.Mask);
			float total = pack.Shown.Select((int i) => SourceEnemyLedgerModel.PillWidth(seat.Pills[i], pack.Form, measure)).Sum() + (float)(Math.Max(0, pack.Shown.Count - 1) * 4);
			float x = pack.Reserve - total - (moreWidth>0?moreWidth+(pack.Shown.Count>0?4f:0f):0f);
			foreach (int p in pack.Shown)
			{
				SourceEnemyLedgerModel.Pill pill = seat.Pills[p];
				float w = SourceEnemyLedgerModel.PillWidth(pill, pack.Form, measure);
				float h = (compact ? (PhoneLayout?18f:20f) : (PhoneLayout?22f:24f))*ts;
				Panel panel = new Panel
				{
					Name = "EnemyPill_" + index + "_" + p,
					Position = new Vector2(x * u, (nameH - h * u) / 2f),
					Size = new Vector2(w * u, h * u),
					TooltipText = pill.Tip,
					MouseFilter = MouseFilterEnum.Pass
				};
				panel.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
				seat.Mask.AddChild(panel, forceReadableName: false, InternalMode.Disabled);
				SourceEnemyLedgerGlyphs.PillPaper(panel, new Color(pill.Paper), u);
				string text = SourceEnemyLedgerModel.DisplayText(pill, pack.Form);
				bool tight = pack.Form != 0;
				float left = (tight ? 2 : 4);
				float right = ((tight && text.Length > 0) ? 6f : left);
				float tw = ((text.Length > 0) ? Math.Max(10f, measure(text)) : 0f);
				float content = (float)(((pill.Icon != null) ? 16 : 0) + ((pill.Icon != null && text.Length > 0) ? (tight ? 1 : 2) : 0)) + tw;
				float px = left + Math.Max(0f, (w - left - right - content) / 2f);
				if (pill.Icon != null)
				{
					SourceBattleDecor.Icon(panel, "PillIcon", SourceEnemyLedgerGlyphs.Mono(pill.Icon), new Rect2(px * u, (h - 16f) * u / 2f, 16f * u, 16f * u), new Color(pill.Ink));
					px += (float)(16 + ((text.Length > 0) ? (tight ? 1 : 2) : 0));
				}
				if (text.Length > 0)
				{
					SourceBattleDecor.Text(panel, "PillText", text, new Rect2(px * u, 0f, tw * u, h * u), pf, new Color(pill.Ink), deco: false, bold: true).HorizontalAlignment = HorizontalAlignment.Center;
				}
				x += w + 4f;
			}
            if(hiddenPills>0 && moreWidth>0)
            {
                var more=SourceBattleDecor.Text(seat.Mask,"EnemyStatusMore", "…",new Rect2(x*u,0f,moreWidth*u,nameH),pf,SourcePalette.Paper,deco:false,bold:true);
                more.TooltipText="状態の続きは長押しで確認";more.MouseFilter=MouseFilterEnum.Ignore;
            }
			seat.Forecast.Position = new Vector2(compact?bar.Position.X+bar.Size.X+8f*u:8f*u,(compact?2f+(PhoneLayout?18f:20f)*ts:(PhoneLayout?51f:58f)*ts)*u);
            seat.Forecast.Size = new Vector2(Math.Max(0f,width*u-seat.Forecast.Position.X-8f*u),(compact?(PhoneLayout?18f:20f):(PhoneLayout?26f:28f))*u*ts);
			IReadOnlyList<SourceEnemyLedgerModel.ForecastPart> parts = SourceEnemyLedgerModel.Forecast(state, index, def, width, PhoneLayout, _textScale);
			FillSourceEnemyForecast(seat.Forecast, parts, pf);
		}
		bar.SourceInsetUnit = u;
		FitSourceEnemyHp(seat, bar, hp, displayedHp, displayedMax);
		float measure(string t)
		{
			return pillFont.GetStringSize(t, HorizontalAlignment.Left, -1f, pf, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal).X / u;
		}
	}

	private static void ClearSourceLedgerChildren(Control node)
	{
		foreach (Node child in node.GetChildren())
		{
			node.RemoveChild(child);
			child.QueueFree();
		}
	}

	private void FillSourceEnemyForecast(Control host, IReadOnlyList<SourceEnemyLedgerModel.ForecastPart> parts, int size)
	{
		ClearSourceLedgerChildren(host);
		host.TooltipText = string.Join("\u3000", parts.Select((SourceEnemyLedgerModel.ForecastPart p) => p.Text));
		Font font = SourceRunUiDecor.Body;
		bool trim = measure(string.Join("\u3000", parts.Select((SourceEnemyLedgerModel.ForecastPart p) => p.Text))) > host.Size.X;
		float room = Math.Max(0f, host.Size.X - (trim ? measure("…") : 0f));
		float x = 0f;
		bool stopped = false;
		for (int i = 0; i < parts.Count; i++)
		{
			if (stopped)
			{
				break;
			}
			string text = ((i > 0) ? "\u3000" : "") + parts[i].Text;
			if (trim && x + measure(text) > room)
			{
				int n;
				for (n = 0; n < text.Length && x + measure(text.Substring(0, n + 1)) <= room; n++)
				{
				}
				text = text.Substring(0, n) + "…";
				stopped = true;
			}
			float w = measure(text);
			SourceBattleDecor.Text(host, "ForecastPart_" + i, text, new Rect2(x, 0f, w, host.Size.Y), size, new Color(parts[i].Ink));
			x += w;
		}
		float measure(string t)
		{
			return font.GetStringSize(t, HorizontalAlignment.Left, -1f, size, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal).X;
		}
	}

	private void FitSourceEnemyHp(SourceLedgerSeat seat, SourceHealthBar bar, SourceHudLabel hp, int value, int max)
	{
		float u = BattleUnit;
		Font font = hp.GetThemeFont("font");
		int fs = hp.GetThemeFontSize("font_size");
		SourceEnemyLedgerModel.HpPlacement p = SourceEnemyLedgerModel.PlaceHp(value, max, bar.Size.X / u, seat.Mark, (string t) => font.GetStringSize(t, HorizontalAlignment.Left, -1f, fs, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal).X / u);
		p = SourceEnemyHpFit.ReserveShield(p, value, bar.Size.X / u, seat.Mark, bar.Block > 0, _textScale, font.GetStringSize(max + " / " + max, HorizontalAlignment.Left, -1f, fs, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal).X / u);
		hp.FollowParentRect = false;
		hp.Text = p.Text;
		hp.HorizontalAlignment = (HorizontalAlignment)(p.AlignRight ? 2 : 1);
		float span = Math.Max(0f, p.Right - p.Left) * u;
		float natural = Math.Max(hp.GetCombinedMinimumSize().X, font.GetStringSize(p.Text, HorizontalAlignment.Left, -1f, fs, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal).X);
		float fit = SourceEnemyHpFit.HorizontalScale(span, natural, _textScale, p.HpOnly);
		hp.Scale = new Vector2(fit, 1f);
		hp.SourceTextRect = new Rect2(p.Left * u, 0f, span / fit, bar.Size.Y);
		hp.RefreshSourceAlignment();
		hp.SetMeta("source_hp_horizontal_fit", fit);
		hp.SetMeta("source_hp_only", p.HpOnly);
		Rect2 painted = hp.PaintedGlyphBoundsInParent;
		hp.SetMeta("source_hp_fitted_paint_rect", new Rect2(hp.Position + (painted.Position - hp.Position) * hp.Scale, painted.Size * hp.Scale));
		seat.Marker.Visible = seat.Mark > 0f && seat.Mark < 1f;
		if (seat.Marker.Visible)
		{
			float x = 3f * u + seat.Mark * (bar.Size.X - 6f * u);
			seat.Marker.Position = new Vector2(x - 3f * u, -3f * u);
			seat.Marker.Size = new Vector2(6f * u, bar.Size.Y + 6f * u);
			seat.MarkerCore.Position = new Vector2(1.5f * u, u);
			seat.MarkerCore.Size = seat.Marker.Size - new Vector2(3f, 2f) * u;
		}
	}

	private void RefreshSourceEnemyHp(int index, SourceHealthBar bar, SourceHudLabel hp, int value, int max)
	{
		if (ReadSourceEnemyLedger(index, out var seat, out var _, out var _))
		{
			FitSourceEnemyHp(seat, bar, hp, value, max);
		}
	}

	private void RefreshSourceForecast(GameState s)
	{
		if (_uxForecast == null || !GodotObject.IsInstanceValid(_uxForecast) || _uxDetail == null || !GodotObject.IsInstanceValid(_uxDetail))
		{
			return;
		}
		PlayerState p = s.Player;
		int defense = p.Block + p.IceBlock;
		int left = Math.Max(0, _uxIncoming - defense);
		int small = (int)Math.Round((double)(PhoneLayout ? 18 : 20) * (PhoneLayout ? 0.85 : 0.75) * (double)BattleUnit * (double)_textScale);
		bool valueOrDefault = s.HideIntents == true;
		bool lethal = !valueOrDefault && _uxIncoming > 0 && p.Hp - left <= 0;
		if (valueOrDefault)
		{
			_uxForecast.Text = little("受けるダメージ ？");
			_uxDetail.Text = "ルーンの円蓋で見えない";
		}
		else if (_uxIncoming <= 0)
		{
			_uxForecast.Text = little(_uxLanded ? "この番の攻撃は終わった" : "この番は攻撃されない");
			_uxDetail.Text = "";
		}
		else
		{
			_uxForecast.Text = (lethal ? $"HP {Math.Max(0, p.Hp)} → 0 致死" : ($"HP {p.Hp} → {p.Hp - left}" + little(colored((left > 0) ? "f2a193" : "a6d99a", $"（−{left}）"))));
			_uxDetail.Text = (PhoneLayout ? "受ける " : "受けるダメージ ") + colored("f2a193", $"[b]{_uxIncoming}[/b]") + ((defense > 0) ? (" − ブロック " + colored("a8c9ea", $"[b]{defense}[/b]")) : "");
		}
		_uxForecast.AddThemeColorOverride("default_color", lethal ? new Color("f2a193") : SourcePalette.Paper);
		_uxDetail.AddThemeColorOverride("default_color", SourcePalette.PaperDim);
		_uxDetail.AddThemeFontOverride("bold_font", SourceRunUiDecor.Action);
		if (_uxForecast.GetParent() is Control body && body.GetParent() is Control ledger)
		{
			Panel edge = body.GetNodeOrNull<Panel>("SourceLethalEdge");
			if (edge == null)
			{
				edge = new Panel
				{
					Name = "SourceLethalEdge",
					ZIndex = -1,
					MouseFilter = MouseFilterEnum.Ignore
				};
				edge.AddThemeStyleboxOverride("panel", SourceBattleDecor.Box(Colors.Transparent, new Color("c9635a"), BattleUnit, 3f));
				body.AddChild(edge, forceReadableName: false, InternalMode.Disabled);
			}
			edge.Position = Vector2.One * -3f * BattleUnit;
			edge.Size = ledger.Size + Vector2.One * 6f * BattleUnit;
			edge.Visible = lethal;
		}
		static string colored(string hex, string text)
		{
			return $"[color=#{hex}]{text}[/color]";
		}
		string little(string text)
		{
			return $"[font_size={small}]{text}[/font_size]";
		}
	}

	public void SetSourceTokenPresentationRevision(Func<long> activeRevision)
	{
		_sourceTokenActiveRevision = activeRevision;
	}

	private void CaptureSourceTokenBoard()
	{
		_sourceTokenFrame = ((_view?.Combat == null) ? null : SourceTokenModel.SelectForBuild(_sourceTokenFrame, _view, _uxEpoch, _activeChange?.Revision ?? (-1), IsInputBlocked, _activeChange, _session?.Revision ?? (-1)));
		_sourceTokenData = ((_sourceTokenFrame == null) ? null : SourceTokenModel.Capture(_sourceTokenFrame));
	}

	private void RefreshSourceTokenEvent(GameEvent evt, SourceTokenFrame? expectedFrame, SessionChange? mappedOwner)
	{
		if (evt is GameEvent_TurnStarted)
		{
			SourceTokenFrame updated = SourceTokenModel.TryOwnedTurnStart(_sourceTokenFrame, expectedFrame, mappedOwner, evt, _uxEpoch, _sourceTokenActiveRevision?.Invoke() ?? (-1), _session?.Revision ?? (-1), IsInputBlocked, _uxOrdinary?.Invoke(evt) ?? true, _receiptValidator?.Invoke(evt, null, null) ?? false);
			if (!(updated == null))
			{
				_sourceTokenFrame = updated;
				_sourceTokenData = SourceTokenModel.Capture(updated);
				RefreshSourceTokens();
			}
		}
	}

	private void BuildSourceTokenBand()
	{
		if (_sourceTokenBand != null && GodotObject.IsInstanceValid(_sourceTokenBand))
		{
			_sourceTokenBand.Hide();
			_sourceTokenBand.QueueFree();
		}
		_sourceTokenBand = null;
		SourceTokenData data = _sourceTokenData;
		if ((object)data == null || _uxBattle == null || !GodotObject.IsInstanceValid(_uxBattle))
		{
			return;
		}
		float u = BattleUnit;
		_sourceTokenBand = new Control
		{
			Name = "SourceTokenBand",
            Position = new Vector2(RunSafeRect.Position.X - 24f * u, 0),
			Size = Size,
			MouseFilter = MouseFilterEnum.Ignore
		};
		_uxBattle.AddChild(_sourceTokenBand, forceReadableName: false, InternalMode.Disabled);
		_sourceTokenRenderedStamp = data.Stamp;
		string setTitle = "からくり " + data.Sets.Count + " / " + data.SetSlots;
		string gearTitle = "ギア " + data.Gears.Count + " / " + 10 + ((data.GearMute == null) ? "" : ("・" + data.GearMute));
		string permTitle = "置物 " + data.Permanents.Count;
		float right = data.Frame.Board.Combat.Enemies.Select((EnemyState e, int i) =>
		{
			Func<int, (Vector2 Head, Vector2 Feet)?>? enemyProjection = _enemyProjection;
			return (((enemyProjection == null) ? ((float?)null) : (enemyProjection(i)?.Item2.X / u)) ?? (Size.X / u * (0.6f + (float)i * 0.12f))) - 120f;
		}).DefaultIfEmpty(Size.X / u - 300f).Min();
		foreach (var enemy in _uxEnemies)
		{
			if (GodotObject.IsInstanceValid(enemy.Hit))
			{
				Rect2 r = enemy.Hit.GetGlobalRect();
				if (r.Position.Y < 173f * u && r.End.Y > 50f * u)
				{
					right = Math.Min(right, r.Position.X / u - 8f);
				}
				Rect2 intent = enemy.Intent.GetGlobalRect();
				if (intent.Position.Y < 173f * u && intent.End.Y > 50f * u)
				{
					right = Math.Min(right, intent.Position.X / u - 8f);
				}
			}
		}
		SourceTokenLayoutResult plan = SourceTokenLayout.Pack((RunSafeRect.Size.X + 48f * u) / u, right - _sourceTokenBand.Position.X / u, data.SetSlots, data.Gears.Count, data.Permanents.Count, SourceTokenHeadingWidth(setTitle, 200f), SourceTokenHeadingWidth(gearTitle, (data.GearMute == null) ? 160 : 260), SourceTokenHeadingWidth(permTitle, 120f));
		SourceTokenAdaptation = plan.Adaptation;
		_sourceTokenBand.SetMeta("source_token_adaptation", plan.Adaptation);
		foreach (SourceTokenSection section in plan.Sections)
		{
			string title;
			if (section.Kind == "set")
			{
				title = setTitle;
			}
			else
			{
				title = ((section.Kind == "gear") ? gearTitle : permTitle);
			}
			PanelContainer panel = UxPanel(_sourceTokenBand, "SourceTokenHeading_" + section.Kind, SourceTokenNativeRect(section.Heading));
			Label label = UxText(panel, title, 15, SourcePalette.Paper);
			label.ClipText = true;
			label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
			panel.TooltipText = title;
		}
		foreach (SourceTokenCell cell in plan.Cells)
		{
			int index = cell.Index;
			switch (cell.Kind)
			{
			case "set":
				BuildSourceSetToken(data, index, cell.Rect);
				break;
			case "gear":
				BuildSourceGearToken(data, index, cell.Rect);
				break;
			case "permanent":
				BuildSourcePermanentToken(data, index, cell.Rect);
				break;
			case "set-more":
				SourceMoreToken("SetMore", cell, "+" + data.Sets.Count, () =>
				{
					ShowSourceSetList(data);
				});
				break;
			case "gear-more":
				SourceMoreToken("GearMore", cell, "+" + cell.Hidden, () =>
				{
					ShowSourceGearInventory(data);
				});
				break;
			case "permanent-more":
				SourceMoreToken("PermanentMore", cell, "+" + cell.Hidden + " …", () =>
				{
					ShowSourcePermanentList(data);
				});
				break;
			default:
				SourceMoreToken("BattleTokensMore", cell, "盤面 +" + (data.Sets.Count + data.Gears.Count + data.Permanents.Count), () =>
				{
					ShowSourceAllTokens(data);
				});
				break;
			}
		}
	}

	private float SourceTokenHeadingWidth(string text, float maximum)
	{
		Font font = SourceRunUiDecor.Body;
		int size = (int)Math.Round(15f * BattleUnit * _textScale);
		return Math.Min(maximum * _textScale, font.GetStringSize(text, HorizontalAlignment.Left, -1f, size, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal).X / BattleUnit + 20f);
	}

	private Rect2 SourceTokenNativeRect(SourceTokenRect r)
	{
		return new Rect2(r.X * BattleUnit, r.Y * BattleUnit, r.W * BattleUnit, r.H * BattleUnit);
	}

	private void BuildSourceSetToken(SourceTokenData data, int index, SourceTokenRect rect)
	{
		SourceSetToken token = ((index < data.Sets.Count) ? data.Sets[index] : null);
		Button slot = SourceTokenButton(_sourceTokenBand, "SetPocket_" + index, rect, () =>
		{
			if (token == null)
			{
				ShowSourceSetList(data);
			}
			else
			{
				ShowSourceTokenDetails(token.Card.Def.Name, token.Details);
			}
		});
		slot.SetMeta("source_slot_index", index);
		if (token == null)
		{
			SourceBattleDecor.Pocket(slot, 68f, 74f, BattleUnit);
			return;
		}
		slot.SetMeta("source_uid", token.Card.Uid);
		GameUI gameUI = this;
		Button parent = slot;
		Color ink;
		if (token.Live)
		{
			ink = (token.Ready ? SourcePalette.Mana : SourcePalette.ManaLight);
		}
		else
		{
			ink = SourcePalette.InkSoft;
		}
		Panel edge = gameUI.SourceTokenPaper(parent, ink, 68f, 74f);
		if (token.Ready)
		{
			float phase = SourceTokenModel.PulsePhase(token.Card.Uid);
			slot.CreateTween().TweenMethod(Callable.From((float k) =>
			{
				if (GodotObject.IsInstanceValid(edge))
				{
					edge.Modulate = new Color(1f, 1f, 1f, SourceTokenModel.PulseAlpha(k, phase));
				}
			}), 0f, 1f, 1.2);
		}
		SourceTokenFill(slot, new SourceTokenRect(1f, 1f, 66f, 40f), SourcePalette.Ink);
		TextureRect picture = SourceTokenIcon(slot, "SetArt", SourceArtwork.Card(token.Card.Def.Id, token.Card.Def.Type), new SourceTokenRect(2f, 2f, 64f, 38f));
		if (!token.Live)
		{
			picture.Modulate = new Color(0.75f, 0.75f, 0.75f);
		}
		GameUI gameUI2 = this;
		Button p = slot;
		SourceTokenRect rect2 = new SourceTokenRect(1f, 45f, 66f, 28f);
		Color color;
		if (token.Live)
		{
			color = (token.Ready ? SourcePalette.ManaInk : SourcePalette.ManaBand);
		}
		else
		{
			color = SourcePalette.InkSoft;
		}
		gameUI2.SourceTokenFill(p, rect2, color);
		SourceTokenLabel(slot, "SetState", token.State, new SourceTokenRect(0f, 45f, 68f, 28f), SourcePalette.Paper, deco: true);
		if (token.Ready)
		{
			SourceTokenBadge(slot, "SetWindows", token.Windows?.ToString() ?? "∞", new SourceTokenRect(50f, -4f, 22f, 22f), SourcePalette.Ink);
		}
		slot.TooltipText = token.Card.Def.Name + "\n" + token.Details;
	}

	private void BuildSourceGearToken(SourceTokenData data, int index, SourceTokenRect rect)
	{
		SourceGearToken token = data.Gears[index];
		GearDef def = token.Definition;
		bool muted = data.GearMute != null;
		Button slot = SourceTokenButton(_sourceTokenBand, "GearToken_" + index, rect, () =>
		{
			ShowSourceGearDetails(data, index);
		});
		slot.SetMeta("source_uid", token.Gear.Uid);
		Color edge;
		if (def.Rarity == "rare")
		{
			edge = SourcePalette.Brass;
		}
		else
		{
			edge = ((def.Rarity == "uncommon") ? new Color("6f95b8") : new Color(SourcePalette.Ink, 0.5f));
		}
		SourceTokenPaper(slot, edge, 68f, 66f, muted);
		Texture2D mutedArt = (muted ? SourceArtwork.GearMuted(token.Gear.GearId, def.Family) : null);
		TextureRect pic = SourceTokenIcon(slot, "GearArt", mutedArt ?? SourceArtwork.Gear(token.Gear.GearId, def.Family), new SourceTokenRect(17f, 5f, 34f, 34f));
		if (muted && mutedArt == null)
		{
			pic.Modulate = new Color(1f, 1f, 1f, 0.6f);
		}
		SourceTokenFill(slot, new SourceTokenRect(1f, 43f, 66f, 22f), muted ? SourcePalette.InkSoft : SourcePalette.Ink);
		SourceTokenLabel(slot, "GearName", def.Name, new SourceTokenRect(3f, 43f, 62f, 22f), SourcePalette.Paper, deco: true, 0.68f);
		if (token.Gear.Charges > 1 || (def.Charges ?? 1) > 1)
		{
			SourceTokenBadge(slot, "GearCharges", token.Gear.Charges.ToString(), new SourceTokenRect(50f, -4f, 22f, 22f), muted ? SourcePalette.InkSoft : SourcePalette.Ink, muted);
		}
		slot.TooltipText = def.Name + "\n" + token.Details;
	}

	private void BuildSourcePermanentToken(SourceTokenData data, int index, SourceTokenRect rect)
	{
		SourcePermanentToken token = data.Permanents[index];
		CardInstance card = token.Card;
		Button slot = SourceTokenButton(_sourceTokenBand, "PermanentToken_" + index, rect, () =>
		{
			ShowSourceTokenDetails(card.Def.Name, token.Details);
		});
		slot.SetMeta("source_uid", card.Uid);
		SourceTokenPaper(slot, SourcePalette.InkSoft, 168f, 40f, muted: false, border: false);
		SourceTokenFill(slot, new SourceTokenRect(4f, 4.5f, 50f, 31f), SourcePalette.Ink);
		Texture2D art = SourceArtwork.Card(card.Def.Id, card.Def.Type);
		Texture2D cropped = art;
		if (art != null)
		{
			cropped = new AtlasTexture
			{
				Atlas = art,
				Region = new Rect2(Math.Max(0f, (float)(art.GetWidth() - 48) / 2f), Math.Max(0f, (float)(art.GetHeight() - 29) / 2f), Math.Min(48, art.GetWidth()), Math.Min(29, art.GetHeight()))
			};
		}
		SourceTokenIcon(slot, "PermanentArt", cropped, new SourceTokenRect(5f, 5.5f, 48f, 29f));
		SourceTokenLabel(slot, "PermanentName", card.Def.Name, new SourceTokenRect(60f, 0f, 100f, 40f), SourcePalette.Ink, deco: true).HorizontalAlignment = HorizontalAlignment.Left;
		if (token.Doll)
		{
			SourceTokenBadge(slot, "DollLife", token.Life?.ToString() ?? "∞", new SourceTokenRect(35f, 4f, 20f, 20f), (token.Life == 1) ? SourcePalette.BadInk : SourcePalette.Ink);
		}
		slot.TooltipText = card.Def.Name + "\n" + token.Details;
	}

	private void SourceMoreToken(string name, SourceTokenCell cell, string text, Action open)
	{
		Button slot = SourceTokenButton(_sourceTokenBand, name, cell.Rect, open);
		SourceTokenPaper(slot, SourcePalette.Ink, cell.Rect.W, cell.Rect.H);
		SourceTokenLabel(slot, "MoreCount", text, new SourceTokenRect(2f, 0f, Math.Max(1f, cell.Rect.W - 4f), cell.Rect.H), SourcePalette.Ink);
		slot.TooltipText = "押すと一覧";
	}

	private Button SourceTokenButton(Control parent, string name, SourceTokenRect rect, Action action)
	{
		Rect2 native = SourceTokenNativeRect(rect);
		Button b = new Button
		{
			Name = name,
			Position = native.Position,
			Size = native.Size,
			Text = "",
			MouseFilter = MouseFilterEnum.Stop
		};
		string[] array = new string[6] { "normal", "hover", "pressed", "disabled", "focus", "hover_pressed" };
		foreach (string style in array)
		{
			b.AddThemeStyleboxOverride(style, new StyleBoxEmpty());
		}
		parent.AddChild(b, forceReadableName: false, InternalMode.Disabled);
		b.Pressed += action;
		return b;
	}

	private TextureRect SourceTokenIcon(Control parent, string name, Texture2D? texture, SourceTokenRect rect)
	{
		TextureRect pic = new TextureRect
		{
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			Texture = texture,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			MouseFilter = MouseFilterEnum.Ignore,
			TextureFilter = TextureFilterEnum.Nearest,
			Name = name,
			Position = new Vector2(rect.X, rect.Y) * BattleUnit,
			Size = new Vector2(rect.W, rect.H) * BattleUnit
		};
		parent.AddChild(pic, forceReadableName: false, InternalMode.Disabled);
		return pic;
	}

	private void SourceTokenFill(Control p, SourceTokenRect rect, Color color)
	{
		p.AddChild(new ColorRect
		{
			Position = new Vector2(rect.X, rect.Y) * BattleUnit,
			Size = new Vector2(rect.W, rect.H) * BattleUnit,
			Color = color,
			MouseFilter = MouseFilterEnum.Ignore
		}, forceReadableName: false, InternalMode.Disabled);
	}

	private Panel SourceTokenPaper(Control parent, Color ink, float w, float h, bool muted = false, bool border = true)
	{
		Panel edge = new Panel
		{
			Position = Vector2.One * -3f * BattleUnit,
			Size = new Vector2(w + 6f, h + 6f) * BattleUnit,
			MouseFilter = MouseFilterEnum.Ignore
		};
		edge.AddThemeStyleboxOverride("panel", SourceBattleDecor.Box(ink, ink, BattleUnit, 0f));
		if (border)
		{
			parent.AddChild(edge, forceReadableName: false, InternalMode.Disabled);
		}
		else
		{
			edge.Hide();
			parent.AddChild(edge, forceReadableName: false, InternalMode.Disabled);
		}
		Panel paper = new Panel
		{
			Size = new Vector2(w, h) * BattleUnit,
			MouseFilter = MouseFilterEnum.Ignore
		};
		paper.AddThemeStyleboxOverride("panel", SourceBattleDecor.Box(SourcePalette.Paper2, SourcePalette.Paper2, BattleUnit, 0f));
		if (muted)
		{
			paper.Modulate = new Color(0.88f, 0.9f, 0.96f);
		}
		parent.AddChild(paper, forceReadableName: false, InternalMode.Disabled);
		return edge;
	}

	private Label SourceTokenLabel(Control p, string name, string text, SourceTokenRect rect, Color ink, bool deco = false, float minimumWidthScale = 1f)
	{
		Label label = UxText(p, text, 15, ink, deco);
		label.Name = name;
		label.Position = new Vector2(rect.X, rect.Y) * BattleUnit;
		label.Size = new Vector2(rect.W, rect.H) * BattleUnit;
		label.HorizontalAlignment = HorizontalAlignment.Center;
		label.ClipText = true;
		label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
		float need = label.GetThemeFont("font").GetStringSize(text, HorizontalAlignment.Left, -1f, label.GetThemeFontSize("font_size"), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal).X;
		float sx = ((need > 0f) ? Math.Clamp(rect.W * BattleUnit / need, minimumWidthScale, 1f) : 1f);
		if (sx < 1f)
		{
			label.Size = new Vector2(rect.W * BattleUnit / sx, rect.H * BattleUnit);
			label.Scale = new Vector2(sx, 1f);
		}
		return label;
	}

	private void SourceTokenBadge(Control p, string name, string text, SourceTokenRect rect, Color ink, bool muted = false)
	{
		Panel disc = new Panel
		{
			Name = name,
			Position = new Vector2(rect.X, rect.Y) * BattleUnit,
			Size = new Vector2(rect.W, rect.H) * BattleUnit,
			MouseFilter = MouseFilterEnum.Ignore
		};
		StyleBoxFlat style = SourceBattleDecor.Box(SourcePalette.Paper, muted ? SourcePalette.InkSoft : SourcePalette.Ink, BattleUnit, 1.5f);
		int radius = (int)Math.Round(rect.W * BattleUnit / 2f);
		int num = (style.CornerRadiusBottomRight = radius);
		int num3 = (style.CornerRadiusBottomLeft = num);
		int cornerRadiusTopLeft = (style.CornerRadiusTopRight = num3);
		style.CornerRadiusTopLeft = cornerRadiusTopLeft;
		disc.AddThemeStyleboxOverride("panel", style);
		if (muted)
		{
			disc.SelfModulate = new Color(0.88f, 0.9f, 0.96f);
		}
		p.AddChild(disc, forceReadableName: false, InternalMode.Disabled);
		SourceTokenLabel(disc, "BadgeValue", text, new SourceTokenRect(0f, 0f, rect.W, rect.H), ink, deco: true);
	}

	private void ShowSourceTokenDetails(string title, string details)
	{
		Open(title, (VBoxContainer b) =>
		{
			Text(ScrollColumn(b), details, 23);
		});
	}

	private void ShowSourceSetList(SourceTokenData data)
	{
		Open("からくり " + data.Sets.Count + " / " + data.SetSlots, (VBoxContainer b) =>
		{
			VBoxContainer parent = ScrollColumn(b);
			foreach (SourceSetToken current in data.Sets)
			{
				SectionHeading(parent, current.Card.Def.Name, 23, Gold);
				Text(parent, current.Details, 21);
			}
			if (data.Sets.Count == 0)
			{
				Text(parent, "仕込み札を置く枠です。準備が明けると、条件に合う敵の行動に反応します。", 21);
			}
		});
	}

	private void ShowSourcePermanentList(SourceTokenData data)
	{
		Open("置物 " + data.Permanents.Count, (VBoxContainer b) =>
		{
			VBoxContainer parent = ScrollColumn(b);
			foreach (SourcePermanentToken current in data.Permanents)
			{
				SectionHeading(parent, current.Card.Def.Name, 23, Gold);
				Text(parent, current.Details, 21);
			}
		});
	}

	private void ShowSourceAllTokens(SourceTokenData data)
	{
		Open("盤面", (VBoxContainer b) =>
		{
			Button(b, "からくり " + data.Sets.Count + " / " + data.SetSlots, () =>
			{
				ShowSourceSetList(data);
			});
			Button(b, "ギア " + data.Gears.Count, () =>
			{
				ShowSourceGearInventory(data);
			});
			Button(b, "置物 " + data.Permanents.Count, () =>
			{
				ShowSourcePermanentList(data);
			});
		});
	}

	private void ShowSourceGearDetails(SourceTokenData data, int index)
	{
		SourceGearToken token = data.Gears[index];
		SourceTokenFrame frame = data.Frame;
		Open(token.Definition.Name, (VBoxContainer b) =>
		{
			Text(ScrollColumn(b), token.Details, 23);
			SourceUiDecision decision = new SourceUiDecision(frame.Revision);
			object overlayOwner = _overlay;
			Button(b, token.Blocked ?? "組む（魔素1個）", () =>
			{
				if (Current() && decision.TryAccept(_session.Revision, IsInputBlocked))
				{
					BeginGear(_session.CurrentRun, index, token.Definition);
				}
			}, token.Blocked == null && Current(), Gold);
			bool Current()
			{
				if (overlayOwner == _overlay)
				{
					return SourceTokenModel.GearCurrent(frame, _session?.CurrentRun, _session?.Revision ?? (-1), IsInputBlocked, index, token.Gear.Uid, token.Gear.GearId, _uxEpoch);
				}
				return false;
			}
		});
	}

	private void ShowSourceGearInventory(SourceTokenData data)
	{
		Open("ギア " + data.Gears.Count, (VBoxContainer b) =>
		{
			GearInventory(b, data.Frame.Board, usable: true);
			object overlayOwner = _overlay;
			SourceUiDecision decision = new SourceUiDecision(data.Frame.Revision);
			foreach (Button current in SourceTokenButtons(b))
			{
				Callable[] callbacks = (from c in current.GetSignalConnectionList(BaseButton.SignalName.Pressed)
					select c["callable"].AsCallable()).ToArray();
				Callable[] array = callbacks;
				foreach (Callable callable in array)
				{
					current.Disconnect(BaseButton.SignalName.Pressed, callable);
				}
				current.Disabled |= !Current();
				current.Pressed += () =>
				{
					if (Current() && decision.TryAccept(_session.Revision, IsInputBlocked))
					{
						Callable[] array2 = callbacks;
						foreach (Callable callable2 in array2)
						{
							callable2.Call();
						}
					}
				};
			}
			bool Current()
			{
				if (overlayOwner == _overlay)
				{
					return SourceTokenModel.InventoryCurrent(data.Frame, _session?.CurrentRun, _session?.Revision ?? (-1), IsInputBlocked, _uxEpoch);
				}
				return false;
			}
		});
	}

	private static IEnumerable<Button> SourceTokenButtons(Node root)
	{
		foreach (Node child in root.GetChildren())
		{
			if (child is Button button)
			{
				yield return button;
			}
			foreach (Button item in SourceTokenButtons(child))
			{
				yield return item;
			}
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		return new List<MethodInfo>(90)
		{
			new MethodInfo(MethodName.AdvancePlay, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.SelectionTitle, new PropertyInfo(Variant.Type.String, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.String, "need", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.BeginEndTurn, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.FontSize, new PropertyInfo(Variant.Type.Int, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Int, "requested", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName._Ready, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.Bind, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, "session", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Node"), exported: false)
			}, null),
			new MethodInfo(MethodName.PresentationCompleted, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName._ExitTree, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.SelectEnemy, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Int, "index", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName._Input, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, "evt", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("InputEvent"), exported: false)
			}, null),
			new MethodInfo(MethodName._UnhandledKeyInput, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, "evt", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("InputEvent"), exported: false)
			}, null),
			new MethodInfo(MethodName.Rebuild, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.BuildHeader, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.Heading, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.String, "title", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.String, "subtitle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.CloseOverlay, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.CloseAllOverlays, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.BuildOverlay, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.OpenMenu, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.OpenKeywords, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.OpenJournal, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.OpenExport, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.OpenRetainedExport, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.OpenExportPair, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.String, "save", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.String, "report", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.String, "title", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.CopyText, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.String, "text", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.ExportFile, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.String, "text", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.String, "name", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.ImportFile, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.OpenImport, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.ImportText, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.String, "text", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName._Notification, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Int, "what", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.HandleBack, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.ApplySafeArea, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.ReadSettings, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.ApplyAudio, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.SaveSettings, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.OpenSettings, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.ApplyTheme, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.Box, new PropertyInfo(Variant.Type.Object, "", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("StyleBoxFlat"), exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Color, "color", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Color, "border", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Int, "width", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.Plain, new PropertyInfo(Variant.Type.String, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.String, "text", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.SizeInteractiveControls, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, "node", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Node"), exported: false)
			}, null),
			new MethodInfo(MethodName.Row, new PropertyInfo(Variant.Type.Object, "", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("HBoxContainer"), exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, "parent", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Node"), exported: false)
			}, null),
			new MethodInfo(MethodName.Column, new PropertyInfo(Variant.Type.Object, "", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("VBoxContainer"), exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, "parent", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Node"), exported: false)
			}, null),
			new MethodInfo(MethodName.ScrollColumn, new PropertyInfo(Variant.Type.Object, "", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("VBoxContainer"), exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, "parent", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Node"), exported: false)
			}, null),
			new MethodInfo(MethodName.Flow, new PropertyInfo(Variant.Type.Object, "", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("HFlowContainer"), exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, "parent", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Node"), exported: false)
			}, null),
			new MethodInfo(MethodName.Art, new PropertyInfo(Variant.Type.Object, "", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Texture2D"), exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.String, "group", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.String, "id", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.Picture, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, "parent", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Node"), exported: false),
				new PropertyInfo(Variant.Type.String, "group", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.String, "id", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Vector2, "size", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.Tile, new PropertyInfo(Variant.Type.Object, "", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("VBoxContainer"), exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, "parent", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Node"), exported: false),
				new PropertyInfo(Variant.Type.String, "name", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.String, "description", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.String, "group", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.String, "id", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.CardColor, new PropertyInfo(Variant.Type.Color, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.String, "color", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.EnemyVitalsText, new PropertyInfo(Variant.Type.String, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Int, "hp", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Int, "maxHp", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Int, "block", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.EnemySurface, new PropertyInfo(Variant.Type.Object, "", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("PanelContainer"), exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, "parent", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Node"), exported: false),
				new PropertyInfo(Variant.Type.Int, "index", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.String, "kind", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Color, "border", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName._Process, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Float, "delta", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.PositionEnemyHud, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.OpenNote, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.OpenRating, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.Paints, new PropertyInfo(Variant.Type.Bool, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, "style", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("StyleBox"), exported: false)
			}, null),
			new MethodInfo(MethodName.VisibleAlpha, new PropertyInfo(Variant.Type.Float, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, "item", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("CanvasItem"), exported: false)
			}, null),
			new MethodInfo(MethodName.ViewportBounds, new PropertyInfo(Variant.Type.Rect2, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, "control", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Control"), exported: false)
			}, null),
			new MethodInfo(MethodName.Set, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, "label", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Label"), exported: false),
				new PropertyInfo(Variant.Type.String, "text", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.CanDropHandCard, new PropertyInfo(Variant.Type.Bool, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.String, "uid", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.DropHandCard, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.String, "uid", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Vector2, "viewportPoint", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.PlayAllSparks, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.PumpSparks, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.ContinueFromTitle, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.Sound, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.String, "key", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.OfferQueuedRating, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.InspectEnemy, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Int, "index", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.DisableUnderlyingFocus, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, "node", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Node"), exported: false)
			}, null),
			new MethodInfo(MethodName.FirstFocusable, new PropertyInfo(Variant.Type.Object, "", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Control"), exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, "node", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Node"), exported: false)
			}, null),
			new MethodInfo(MethodName.ShowTitle, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.SharePair, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.String, "report", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.String, "save", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.ShareRequestFinished, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Bool, "opened", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.String, "message", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.BuildTitle, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.InvokeAction, new PropertyInfo(Variant.Type.Bool, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.String, "exactLabel", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.UxResetHand, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.UxText, new PropertyInfo(Variant.Type.Object, "", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Label"), exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, "parent", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Control"), exported: false),
				new PropertyInfo(Variant.Type.String, "text", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Int, "units", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Color, "color", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Bool, "deco", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Bool, "bold", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.UxPanelBox, new PropertyInfo(Variant.Type.Object, "", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("StyleBoxFlat"), exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Color, "color", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Color, "edge", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Int, "width", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.BuildBattleHeader, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.CreateHpReadout, new PropertyInfo(Variant.Type.Object, "", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Label"), exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, "parent", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Control"), exported: false),
				new PropertyInfo(Variant.Type.String, "text", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Int, "sourceSize", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.UxRich, new PropertyInfo(Variant.Type.Object, "", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("RichTextLabel"), exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, "p", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Control"), exported: false),
				new PropertyInfo(Variant.Type.String, "name", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Int, "size", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Bool, "deco", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.PositionUxEnemies, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.RefreshUxReadouts, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.SyncUxHand, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.MakeUxDragPreview, new PropertyInfo(Variant.Type.Object, "", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Control"), exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.String, "uid", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.HoverUxHand, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.String, "uid", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Bool, "enter", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.RestoreUxHandOrder, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.StartHandDrag, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.String, "uid", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.EndHandDrag, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.DecodeHandDrag, new PropertyInfo(Variant.Type.String, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Nil, "data", PropertyHint.None, "", PropertyUsageFlags.Default | PropertyUsageFlags.NilIsVariant, exported: false)
			}, null),
			new MethodInfo(MethodName.UpdateHandAim, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.UxProcess, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Float, "delta", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.AlignUxRichLine, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, "label", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("RichTextLabel"), exported: false)
			}, null),
			new MethodInfo(MethodName.UxRelayout, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		if (method == MethodName.AdvancePlay && args.Count == 0)
		{
			AdvancePlay();
			ret = default;
			return true;
		}
		if (method == MethodName.SelectionTitle && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<string>(SelectionTitle(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.BeginEndTurn && args.Count == 0)
		{
			BeginEndTurn();
			ret = default;
			return true;
		}
		if (method == MethodName.FontSize && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<int>(FontSize(VariantUtils.ConvertTo<int>(in args[0])));
			return true;
		}
		if (method == MethodName._Ready && args.Count == 0)
		{
			_Ready();
			ret = default;
			return true;
		}
		if (method == MethodName.Bind && args.Count == 1)
		{
			Bind(VariantUtils.ConvertTo<GameSession>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.PresentationCompleted && args.Count == 0)
		{
			PresentationCompleted();
			ret = default;
			return true;
		}
		if (method == MethodName._ExitTree && args.Count == 0)
		{
			_ExitTree();
			ret = default;
			return true;
		}
		if (method == MethodName.SelectEnemy && args.Count == 1)
		{
			SelectEnemy(VariantUtils.ConvertTo<int>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName._Input && args.Count == 1)
		{
			_Input(VariantUtils.ConvertTo<InputEvent>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName._UnhandledKeyInput && args.Count == 1)
		{
			_UnhandledKeyInput(VariantUtils.ConvertTo<InputEvent>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.Rebuild && args.Count == 0)
		{
			Rebuild();
			ret = default;
			return true;
		}
		if (method == MethodName.BuildHeader && args.Count == 0)
		{
			BuildHeader();
			ret = default;
			return true;
		}
		if (method == MethodName.Heading && args.Count == 2)
		{
			Heading(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<string>(in args[1]));
			ret = default;
			return true;
		}
		if (method == MethodName.CloseOverlay && args.Count == 0)
		{
			CloseOverlay();
			ret = default;
			return true;
		}
		if (method == MethodName.CloseAllOverlays && args.Count == 0)
		{
			CloseAllOverlays();
			ret = default;
			return true;
		}
		if (method == MethodName.BuildOverlay && args.Count == 0)
		{
			BuildOverlay();
			ret = default;
			return true;
		}
		if (method == MethodName.OpenMenu && args.Count == 0)
		{
			OpenMenu();
			ret = default;
			return true;
		}
		if (method == MethodName.OpenKeywords && args.Count == 0)
		{
			OpenKeywords();
			ret = default;
			return true;
		}
		if (method == MethodName.OpenJournal && args.Count == 0)
		{
			OpenJournal();
			ret = default;
			return true;
		}
		if (method == MethodName.OpenExport && args.Count == 0)
		{
			OpenExport();
			ret = default;
			return true;
		}
		if (method == MethodName.OpenRetainedExport && args.Count == 0)
		{
			OpenRetainedExport();
			ret = default;
			return true;
		}
		if (method == MethodName.OpenExportPair && args.Count == 3)
		{
			OpenExportPair(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<string>(in args[1]), VariantUtils.ConvertTo<string>(in args[2]));
			ret = default;
			return true;
		}
		if (method == MethodName.CopyText && args.Count == 1)
		{
			CopyText(VariantUtils.ConvertTo<string>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.ExportFile && args.Count == 2)
		{
			ExportFile(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<string>(in args[1]));
			ret = default;
			return true;
		}
		if (method == MethodName.ImportFile && args.Count == 0)
		{
			ImportFile();
			ret = default;
			return true;
		}
		if (method == MethodName.OpenImport && args.Count == 0)
		{
			OpenImport();
			ret = default;
			return true;
		}
		if (method == MethodName.ImportText && args.Count == 1)
		{
			ImportText(VariantUtils.ConvertTo<string>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName._Notification && args.Count == 1)
		{
			_Notification(VariantUtils.ConvertTo<int>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.HandleBack && args.Count == 0)
		{
			HandleBack();
			ret = default;
			return true;
		}
		if (method == MethodName.ApplySafeArea && args.Count == 0)
		{
			ApplySafeArea();
			ret = default;
			return true;
		}
		if (method == MethodName.ReadSettings && args.Count == 0)
		{
			ReadSettings();
			ret = default;
			return true;
		}
		if (method == MethodName.ApplyAudio && args.Count == 0)
		{
			ApplyAudio();
			ret = default;
			return true;
		}
		if (method == MethodName.SaveSettings && args.Count == 0)
		{
			SaveSettings();
			ret = default;
			return true;
		}
		if (method == MethodName.OpenSettings && args.Count == 0)
		{
			OpenSettings();
			ret = default;
			return true;
		}
		if (method == MethodName.ApplyTheme && args.Count == 0)
		{
			ApplyTheme();
			ret = default;
			return true;
		}
		if (method == MethodName.Box && args.Count == 3)
		{
			ret = VariantUtils.CreateFrom<StyleBoxFlat>(Box(VariantUtils.ConvertTo<Color>(in args[0]), VariantUtils.ConvertTo<Color>(in args[1]), VariantUtils.ConvertTo<int>(in args[2])));
			return true;
		}
		if (method == MethodName.Plain && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<string>(Plain(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.SizeInteractiveControls && args.Count == 1)
		{
			SizeInteractiveControls(VariantUtils.ConvertTo<Node>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.Row && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<HBoxContainer>(Row(VariantUtils.ConvertTo<Node>(in args[0])));
			return true;
		}
		if (method == MethodName.Column && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<VBoxContainer>(Column(VariantUtils.ConvertTo<Node>(in args[0])));
			return true;
		}
		if (method == MethodName.ScrollColumn && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<VBoxContainer>(ScrollColumn(VariantUtils.ConvertTo<Node>(in args[0])));
			return true;
		}
		if (method == MethodName.Flow && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<HFlowContainer>(Flow(VariantUtils.ConvertTo<Node>(in args[0])));
			return true;
		}
		if (method == MethodName.Art && args.Count == 2)
		{
			ret = VariantUtils.CreateFrom<Texture2D>(Art(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<string>(in args[1])));
			return true;
		}
		if (method == MethodName.Picture && args.Count == 4)
		{
			Picture(VariantUtils.ConvertTo<Node>(in args[0]), VariantUtils.ConvertTo<string>(in args[1]), VariantUtils.ConvertTo<string>(in args[2]), VariantUtils.ConvertTo<Vector2>(in args[3]));
			ret = default;
			return true;
		}
		if (method == MethodName.Tile && args.Count == 5)
		{
			ret = VariantUtils.CreateFrom<VBoxContainer>(Tile(VariantUtils.ConvertTo<Node>(in args[0]), VariantUtils.ConvertTo<string>(in args[1]), VariantUtils.ConvertTo<string>(in args[2]), VariantUtils.ConvertTo<string>(in args[3]), VariantUtils.ConvertTo<string>(in args[4])));
			return true;
		}
		if (method == MethodName.CardColor && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<Color>(CardColor(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.EnemyVitalsText && args.Count == 3)
		{
			ret = VariantUtils.CreateFrom<string>(EnemyVitalsText(VariantUtils.ConvertTo<int>(in args[0]), VariantUtils.ConvertTo<int>(in args[1]), VariantUtils.ConvertTo<int>(in args[2])));
			return true;
		}
		if (method == MethodName.EnemySurface && args.Count == 4)
		{
			ret = VariantUtils.CreateFrom<PanelContainer>(EnemySurface(VariantUtils.ConvertTo<Node>(in args[0]), VariantUtils.ConvertTo<int>(in args[1]), VariantUtils.ConvertTo<string>(in args[2]), VariantUtils.ConvertTo<Color>(in args[3])));
			return true;
		}
		if (method == MethodName._Process && args.Count == 1)
		{
			_Process(VariantUtils.ConvertTo<double>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.PositionEnemyHud && args.Count == 0)
		{
			PositionEnemyHud();
			ret = default;
			return true;
		}
		if (method == MethodName.OpenNote && args.Count == 0)
		{
			OpenNote();
			ret = default;
			return true;
		}
		if (method == MethodName.OpenRating && args.Count == 0)
		{
			OpenRating();
			ret = default;
			return true;
		}
		if (method == MethodName.Paints && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<bool>(Paints(VariantUtils.ConvertTo<StyleBox>(in args[0])));
			return true;
		}
		if (method == MethodName.VisibleAlpha && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<float>(VisibleAlpha(VariantUtils.ConvertTo<CanvasItem>(in args[0])));
			return true;
		}
		if (method == MethodName.ViewportBounds && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<Rect2>(ViewportBounds(VariantUtils.ConvertTo<Control>(in args[0])));
			return true;
		}
		if (method == MethodName.Set && args.Count == 2)
		{
			Set(VariantUtils.ConvertTo<Label>(in args[0]), VariantUtils.ConvertTo<string>(in args[1]));
			ret = default;
			return true;
		}
		if (method == MethodName.CanDropHandCard && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<bool>(CanDropHandCard(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.DropHandCard && args.Count == 2)
		{
			DropHandCard(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<Vector2>(in args[1]));
			ret = default;
			return true;
		}
		if (method == MethodName.PlayAllSparks && args.Count == 0)
		{
			PlayAllSparks();
			ret = default;
			return true;
		}
		if (method == MethodName.PumpSparks && args.Count == 0)
		{
			PumpSparks();
			ret = default;
			return true;
		}
		if (method == MethodName.ContinueFromTitle && args.Count == 0)
		{
			ContinueFromTitle();
			ret = default;
			return true;
		}
		if (method == MethodName.Sound && args.Count == 1)
		{
			Sound(VariantUtils.ConvertTo<string>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.OfferQueuedRating && args.Count == 0)
		{
			OfferQueuedRating();
			ret = default;
			return true;
		}
		if (method == MethodName.InspectEnemy && args.Count == 1)
		{
			InspectEnemy(VariantUtils.ConvertTo<int>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.DisableUnderlyingFocus && args.Count == 1)
		{
			DisableUnderlyingFocus(VariantUtils.ConvertTo<Node>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.FirstFocusable && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<Control>(FirstFocusable(VariantUtils.ConvertTo<Node>(in args[0])));
			return true;
		}
		if (method == MethodName.ShowTitle && args.Count == 0)
		{
			ShowTitle();
			ret = default;
			return true;
		}
		if (method == MethodName.SharePair && args.Count == 2)
		{
			SharePair(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<string>(in args[1]));
			ret = default;
			return true;
		}
		if (method == MethodName.ShareRequestFinished && args.Count == 2)
		{
			ShareRequestFinished(VariantUtils.ConvertTo<bool>(in args[0]), VariantUtils.ConvertTo<string>(in args[1]));
			ret = default;
			return true;
		}
		if (method == MethodName.BuildTitle && args.Count == 0)
		{
			BuildTitle();
			ret = default;
			return true;
		}
		if (method == MethodName.InvokeAction && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<bool>(InvokeAction(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.UxResetHand && args.Count == 0)
		{
			UxResetHand();
			ret = default;
			return true;
		}
		if (method == MethodName.UxText && args.Count == 6)
		{
			ret = VariantUtils.CreateFrom<Label>(UxText(VariantUtils.ConvertTo<Control>(in args[0]), VariantUtils.ConvertTo<string>(in args[1]), VariantUtils.ConvertTo<int>(in args[2]), VariantUtils.ConvertTo<Color>(in args[3]), VariantUtils.ConvertTo<bool>(in args[4]), VariantUtils.ConvertTo<bool>(in args[5])));
			return true;
		}
		if (method == MethodName.UxPanelBox && args.Count == 3)
		{
			ret = VariantUtils.CreateFrom<StyleBoxFlat>(UxPanelBox(VariantUtils.ConvertTo<Color>(in args[0]), VariantUtils.ConvertTo<Color>(in args[1]), VariantUtils.ConvertTo<int>(in args[2])));
			return true;
		}
		if (method == MethodName.BuildBattleHeader && args.Count == 0)
		{
			BuildBattleHeader();
			ret = default;
			return true;
		}
		if (method == MethodName.CreateHpReadout && args.Count == 3)
		{
			ret = VariantUtils.CreateFrom<SourceHudLabel>(CreateHpReadout(VariantUtils.ConvertTo<Control>(in args[0]), VariantUtils.ConvertTo<string>(in args[1]), VariantUtils.ConvertTo<int>(in args[2])));
			return true;
		}
		if (method == MethodName.UxRich && args.Count == 4)
		{
			ret = VariantUtils.CreateFrom<RichTextLabel>(UxRich(VariantUtils.ConvertTo<Control>(in args[0]), VariantUtils.ConvertTo<string>(in args[1]), VariantUtils.ConvertTo<int>(in args[2]), VariantUtils.ConvertTo<bool>(in args[3])));
			return true;
		}
		if (method == MethodName.PositionUxEnemies && args.Count == 0)
		{
			PositionUxEnemies();
			ret = default;
			return true;
		}
		if (method == MethodName.RefreshUxReadouts && args.Count == 0)
		{
			RefreshUxReadouts();
			ret = default;
			return true;
		}
		if (method == MethodName.SyncUxHand && args.Count == 0)
		{
			SyncUxHand();
			ret = default;
			return true;
		}
		if (method == MethodName.MakeUxDragPreview && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<Control>(MakeUxDragPreview(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.HoverUxHand && args.Count == 2)
		{
			HoverUxHand(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<bool>(in args[1]));
			ret = default;
			return true;
		}
		if (method == MethodName.RestoreUxHandOrder && args.Count == 0)
		{
			RestoreUxHandOrder();
			ret = default;
			return true;
		}
		if (method == MethodName.StartHandDrag && args.Count == 1)
		{
			StartHandDrag(VariantUtils.ConvertTo<string>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.EndHandDrag && args.Count == 0)
		{
			EndHandDrag();
			ret = default;
			return true;
		}
		if (method == MethodName.DecodeHandDrag && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<string>(DecodeHandDrag(VariantUtils.ConvertTo<Variant>(in args[0])));
			return true;
		}
		if (method == MethodName.UpdateHandAim && args.Count == 0)
		{
			UpdateHandAim();
			ret = default;
			return true;
		}
		if (method == MethodName.UxProcess && args.Count == 1)
		{
			UxProcess(VariantUtils.ConvertTo<double>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.AlignUxRichLine && args.Count == 1)
		{
			AlignUxRichLine(VariantUtils.ConvertTo<RichTextLabel>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.UxRelayout && args.Count == 0)
		{
			UxRelayout();
			ret = default;
			return true;
		}
		return base.InvokeGodotClassMethod(in method, args, out ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		if (method == MethodName.SelectionTitle && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<string>(SelectionTitle(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.Box && args.Count == 3)
		{
			ret = VariantUtils.CreateFrom<StyleBoxFlat>(Box(VariantUtils.ConvertTo<Color>(in args[0]), VariantUtils.ConvertTo<Color>(in args[1]), VariantUtils.ConvertTo<int>(in args[2])));
			return true;
		}
		if (method == MethodName.Plain && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<string>(Plain(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.Row && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<HBoxContainer>(Row(VariantUtils.ConvertTo<Node>(in args[0])));
			return true;
		}
		if (method == MethodName.Column && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<VBoxContainer>(Column(VariantUtils.ConvertTo<Node>(in args[0])));
			return true;
		}
		if (method == MethodName.ScrollColumn && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<VBoxContainer>(ScrollColumn(VariantUtils.ConvertTo<Node>(in args[0])));
			return true;
		}
		if (method == MethodName.Flow && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<HFlowContainer>(Flow(VariantUtils.ConvertTo<Node>(in args[0])));
			return true;
		}
		if (method == MethodName.CardColor && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<Color>(CardColor(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.Paints && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<bool>(Paints(VariantUtils.ConvertTo<StyleBox>(in args[0])));
			return true;
		}
		if (method == MethodName.VisibleAlpha && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<float>(VisibleAlpha(VariantUtils.ConvertTo<CanvasItem>(in args[0])));
			return true;
		}
		if (method == MethodName.ViewportBounds && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<Rect2>(ViewportBounds(VariantUtils.ConvertTo<Control>(in args[0])));
			return true;
		}
		if (method == MethodName.Set && args.Count == 2)
		{
			Set(VariantUtils.ConvertTo<Label>(in args[0]), VariantUtils.ConvertTo<string>(in args[1]));
			ret = default;
			return true;
		}
		if (method == MethodName.DisableUnderlyingFocus && args.Count == 1)
		{
			DisableUnderlyingFocus(VariantUtils.ConvertTo<Node>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.FirstFocusable && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<Control>(FirstFocusable(VariantUtils.ConvertTo<Node>(in args[0])));
			return true;
		}
		if (method == MethodName.UxPanelBox && args.Count == 3)
		{
			ret = VariantUtils.CreateFrom<StyleBoxFlat>(UxPanelBox(VariantUtils.ConvertTo<Color>(in args[0]), VariantUtils.ConvertTo<Color>(in args[1]), VariantUtils.ConvertTo<int>(in args[2])));
			return true;
		}
		if (method == MethodName.AlignUxRichLine && args.Count == 1)
		{
			AlignUxRichLine(VariantUtils.ConvertTo<RichTextLabel>(in args[0]));
			ret = default;
			return true;
		}
		ret = default;
		return false;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if (method == MethodName.AdvancePlay)
		{
			return true;
		}
		if (method == MethodName.SelectionTitle)
		{
			return true;
		}
		if (method == MethodName.BeginEndTurn)
		{
			return true;
		}
		if (method == MethodName.FontSize)
		{
			return true;
		}
		if (method == MethodName._Ready)
		{
			return true;
		}
		if (method == MethodName.Bind)
		{
			return true;
		}
		if (method == MethodName.PresentationCompleted)
		{
			return true;
		}
		if (method == MethodName._ExitTree)
		{
			return true;
		}
		if (method == MethodName.SelectEnemy)
		{
			return true;
		}
		if (method == MethodName._Input)
		{
			return true;
		}
		if (method == MethodName._UnhandledKeyInput)
		{
			return true;
		}
		if (method == MethodName.Rebuild)
		{
			return true;
		}
		if (method == MethodName.BuildHeader)
		{
			return true;
		}
		if (method == MethodName.Heading)
		{
			return true;
		}
		if (method == MethodName.CloseOverlay)
		{
			return true;
		}
		if (method == MethodName.CloseAllOverlays)
		{
			return true;
		}
		if (method == MethodName.BuildOverlay)
		{
			return true;
		}
		if (method == MethodName.OpenMenu)
		{
			return true;
		}
		if (method == MethodName.OpenKeywords)
		{
			return true;
		}
		if (method == MethodName.OpenJournal)
		{
			return true;
		}
		if (method == MethodName.OpenExport)
		{
			return true;
		}
		if (method == MethodName.OpenRetainedExport)
		{
			return true;
		}
		if (method == MethodName.OpenExportPair)
		{
			return true;
		}
		if (method == MethodName.CopyText)
		{
			return true;
		}
		if (method == MethodName.ExportFile)
		{
			return true;
		}
		if (method == MethodName.ImportFile)
		{
			return true;
		}
		if (method == MethodName.OpenImport)
		{
			return true;
		}
		if (method == MethodName.ImportText)
		{
			return true;
		}
		if (method == MethodName._Notification)
		{
			return true;
		}
		if (method == MethodName.HandleBack)
		{
			return true;
		}
		if (method == MethodName.ApplySafeArea)
		{
			return true;
		}
		if (method == MethodName.ReadSettings)
		{
			return true;
		}
		if (method == MethodName.ApplyAudio)
		{
			return true;
		}
		if (method == MethodName.SaveSettings)
		{
			return true;
		}
		if (method == MethodName.OpenSettings)
		{
			return true;
		}
		if (method == MethodName.ApplyTheme)
		{
			return true;
		}
		if (method == MethodName.Box)
		{
			return true;
		}
		if (method == MethodName.Plain)
		{
			return true;
		}
		if (method == MethodName.SizeInteractiveControls)
		{
			return true;
		}
		if (method == MethodName.Row)
		{
			return true;
		}
		if (method == MethodName.Column)
		{
			return true;
		}
		if (method == MethodName.ScrollColumn)
		{
			return true;
		}
		if (method == MethodName.Flow)
		{
			return true;
		}
		if (method == MethodName.Art)
		{
			return true;
		}
		if (method == MethodName.Picture)
		{
			return true;
		}
		if (method == MethodName.Tile)
		{
			return true;
		}
		if (method == MethodName.CardColor)
		{
			return true;
		}
		if (method == MethodName.EnemyVitalsText)
		{
			return true;
		}
		if (method == MethodName.EnemySurface)
		{
			return true;
		}
		if (method == MethodName._Process)
		{
			return true;
		}
		if (method == MethodName.PositionEnemyHud)
		{
			return true;
		}
		if (method == MethodName.OpenNote)
		{
			return true;
		}
		if (method == MethodName.OpenRating)
		{
			return true;
		}
		if (method == MethodName.Paints)
		{
			return true;
		}
		if (method == MethodName.VisibleAlpha)
		{
			return true;
		}
		if (method == MethodName.ViewportBounds)
		{
			return true;
		}
		if (method == MethodName.Set)
		{
			return true;
		}
		if (method == MethodName.CanDropHandCard)
		{
			return true;
		}
		if (method == MethodName.DropHandCard)
		{
			return true;
		}
		if (method == MethodName.PlayAllSparks)
		{
			return true;
		}
		if (method == MethodName.PumpSparks)
		{
			return true;
		}
		if (method == MethodName.ContinueFromTitle)
		{
			return true;
		}
		if (method == MethodName.Sound)
		{
			return true;
		}
		if (method == MethodName.OfferQueuedRating)
		{
			return true;
		}
		if (method == MethodName.InspectEnemy)
		{
			return true;
		}
		if (method == MethodName.DisableUnderlyingFocus)
		{
			return true;
		}
		if (method == MethodName.FirstFocusable)
		{
			return true;
		}
		if (method == MethodName.ShowTitle)
		{
			return true;
		}
		if (method == MethodName.SharePair)
		{
			return true;
		}
		if (method == MethodName.ShareRequestFinished)
		{
			return true;
		}
		if (method == MethodName.BuildTitle)
		{
			return true;
		}
		if (method == MethodName.InvokeAction)
		{
			return true;
		}
		if (method == MethodName.UxResetHand)
		{
			return true;
		}
		if (method == MethodName.UxText)
		{
			return true;
		}
		if (method == MethodName.UxPanelBox)
		{
			return true;
		}
		if (method == MethodName.BuildBattleHeader)
		{
			return true;
		}
		if (method == MethodName.CreateHpReadout)
		{
			return true;
		}
		if (method == MethodName.UxRich)
		{
			return true;
		}
		if (method == MethodName.PositionUxEnemies)
		{
			return true;
		}
		if (method == MethodName.RefreshUxReadouts)
		{
			return true;
		}
		if (method == MethodName.SyncUxHand)
		{
			return true;
		}
		if (method == MethodName.MakeUxDragPreview)
		{
			return true;
		}
		if (method == MethodName.HoverUxHand)
		{
			return true;
		}
		if (method == MethodName.RestoreUxHandOrder)
		{
			return true;
		}
		if (method == MethodName.StartHandDrag)
		{
			return true;
		}
		if (method == MethodName.EndHandDrag)
		{
			return true;
		}
		if (method == MethodName.DecodeHandDrag)
		{
			return true;
		}
		if (method == MethodName.UpdateHandAim)
		{
			return true;
		}
		if (method == MethodName.UxProcess)
		{
			return true;
		}
		if (method == MethodName.AlignUxRichLine)
		{
			return true;
		}
		if (method == MethodName.UxRelayout)
		{
			return true;
		}
		return base.HasGodotClassMethod(in method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if (name == PropertyName.RejectedReceiptCount)
		{
			RejectedReceiptCount = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName.PresentedNumericContactCount)
		{
			PresentedNumericContactCount = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName._session)
		{
			_session = VariantUtils.ConvertTo<GameSession>(in value);
			return true;
		}
		if (name == PropertyName._body)
		{
			_body = VariantUtils.ConvertTo<VBoxContainer>(in value);
			return true;
		}
		if (name == PropertyName._shell)
		{
			_shell = VariantUtils.ConvertTo<VBoxContainer>(in value);
			return true;
		}
		if (name == PropertyName._overlayNode)
		{
			_overlayNode = VariantUtils.ConvertTo<Control>(in value);
			return true;
		}
		if (name == PropertyName._overlayTitle)
		{
			_overlayTitle = VariantUtils.ConvertTo<string>(in value);
			return true;
		}
		if (name == PropertyName._overlayCanClose)
		{
			_overlayCanClose = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._showTitle)
		{
			_showTitle = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._notice)
		{
			_notice = VariantUtils.ConvertTo<string>(in value);
			return true;
		}
		if (name == PropertyName._error)
		{
			_error = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._leaderId)
		{
			_leaderId = VariantUtils.ConvertTo<string>(in value);
			return true;
		}
		if (name == PropertyName._deckId)
		{
			_deckId = VariantUtils.ConvertTo<string>(in value);
			return true;
		}
		if (name == PropertyName._seed)
		{
			_seed = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName._difficulty)
		{
			_difficulty = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName._preferredTarget)
		{
			_preferredTarget = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName._workshopA)
		{
			_workshopA = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName._workshopB)
		{
			_workshopB = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName._textScale)
		{
			_textScale = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		if (name == PropertyName._masterVolume)
		{
			_masterVolume = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		if (name == PropertyName._bgmVolume)
		{
			_bgmVolume = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		if (name == PropertyName._sfxVolume)
		{
			_sfxVolume = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		if (name == PropertyName._muted)
		{
			_muted = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._graphicsQuality)
		{
			_graphicsQuality = VariantUtils.ConvertTo<string>(in value);
			return true;
		}
		if (name == PropertyName._safeMargin)
		{
			_safeMargin = VariantUtils.ConvertTo<MarginContainer>(in value);
			return true;
		}
		if (name == PropertyName._handScroll)
		{
			_handScroll = VariantUtils.ConvertTo<ScrollContainer>(in value);
			return true;
		}
		if (name == PropertyName._noteDraft)
		{
			_noteDraft = VariantUtils.ConvertTo<string>(in value);
			return true;
		}
		if (name == PropertyName._hpReadout)
		{
			_hpReadout = VariantUtils.ConvertTo<Label>(in value);
			return true;
		}
		if (name == PropertyName._energyReadout)
		{
			_energyReadout = VariantUtils.ConvertTo<Label>(in value);
			return true;
		}
		if (name == PropertyName._blockReadout)
		{
			_blockReadout = VariantUtils.ConvertTo<Label>(in value);
			return true;
		}
		if (name == PropertyName._statusReadout)
		{
			_statusReadout = VariantUtils.ConvertTo<Label>(in value);
			return true;
		}
		if (name == PropertyName._costPresented)
		{
			_costPresented = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._sparksRemaining)
		{
			_sparksRemaining = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName._pumpingSparks)
		{
			_pumpingSparks = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._ratingQueued)
		{
			_ratingQueued = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._shareAwaiting)
		{
			_shareAwaiting = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._reactionMode)
		{
			_reactionMode = VariantUtils.ConvertTo<string>(in value);
			return true;
		}
		if (name == PropertyName._setAnyCards)
		{
			_setAnyCards = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._uxBattle)
		{
			_uxBattle = VariantUtils.ConvertTo<Control>(in value);
			return true;
		}
		if (name == PropertyName._uxHand)
		{
			_uxHand = VariantUtils.ConvertTo<Control>(in value);
			return true;
		}
		if (name == PropertyName._uxAim)
		{
			_uxAim = VariantUtils.ConvertTo<SourceAimArrow>(in value);
			return true;
		}
		if (name == PropertyName._uxHpBar)
		{
			_uxHpBar = VariantUtils.ConvertTo<SourceHealthBar>(in value);
			return true;
		}
		if (name == PropertyName._uxOrb)
		{
			_uxOrb = VariantUtils.ConvertTo<SourceEnergyOrb>(in value);
			return true;
		}
		if (name == PropertyName._uxForecast)
		{
			_uxForecast = VariantUtils.ConvertTo<RichTextLabel>(in value);
			return true;
		}
		if (name == PropertyName._uxDetail)
		{
			_uxDetail = VariantUtils.ConvertTo<RichTextLabel>(in value);
			return true;
		}
		if (name == PropertyName._uxPhase)
		{
			_uxPhase = VariantUtils.ConvertTo<Label>(in value);
			return true;
		}
		if (name == PropertyName._uxHover)
		{
			_uxHover = VariantUtils.ConvertTo<string>(in value);
			return true;
		}
		if (name == PropertyName._uxDrag)
		{
			_uxDrag = VariantUtils.ConvertTo<string>(in value);
			return true;
		}
		if (name == PropertyName._uxEpoch)
		{
			_uxEpoch = VariantUtils.ConvertTo<long>(in value);
			return true;
		}
		if (name == PropertyName._uxLastSize)
		{
			_uxLastSize = VariantUtils.ConvertTo<Vector2>(in value);
			return true;
		}
		if (name == PropertyName._uxRelayoutQueued)
		{
			_uxRelayoutQueued = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._uxIncoming)
		{
			_uxIncoming = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName._uxLanded)
		{
			_uxLanded = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._uxSkipFirst)
		{
			_uxSkipFirst = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._uxIncomingOwner)
		{
			_uxIncomingOwner = VariantUtils.ConvertTo<long>(in value);
			return true;
		}
		return base.SetGodotClassPropertyValue(in name, in value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		string from;
		if (name == PropertyName.GraphicsQuality)
		{
			from = GraphicsQuality;
			value = VariantUtils.CreateFrom(in from);
			return true;
		}
		bool from2;
		if (name == PropertyName.PhoneLayout)
		{
			from2 = PhoneLayout;
			value = VariantUtils.CreateFrom(in from2);
			return true;
		}
		float from3;
		if (name == PropertyName.MinimumHit)
		{
			from3 = MinimumHit;
			value = VariantUtils.CreateFrom(in from3);
			return true;
		}
		if (name == PropertyName.IsInputBlocked)
		{
			from2 = IsInputBlocked;
			value = VariantUtils.CreateFrom(in from2);
			return true;
		}
		if (name == PropertyName.CurrentRoute)
		{
			from = CurrentRoute;
			value = VariantUtils.CreateFrom(in from);
			return true;
		}
		if (name == PropertyName.ActiveModal)
		{
			from = ActiveModal;
			value = VariantUtils.CreateFrom(in from);
			return true;
		}
		int from4;
		if (name == PropertyName.RejectedReceiptCount)
		{
			from4 = RejectedReceiptCount;
			value = VariantUtils.CreateFrom(in from4);
			return true;
		}
		if (name == PropertyName.PendingSparkPlays)
		{
			from4 = PendingSparkPlays;
			value = VariantUtils.CreateFrom(in from4);
			return true;
		}
		if (name == PropertyName.PresentedNumericContactCount)
		{
			from4 = PresentedNumericContactCount;
			value = VariantUtils.CreateFrom(in from4);
			return true;
		}
		if (name == PropertyName.DisplayedIncomingRemaining)
		{
			from4 = DisplayedIncomingRemaining;
			value = VariantUtils.CreateFrom(in from4);
			return true;
		}
		if (name == PropertyName.DisplayedIncomingLanded)
		{
			from2 = DisplayedIncomingLanded;
			value = VariantUtils.CreateFrom(in from2);
			return true;
		}
		if (name == PropertyName.DisplayedIncomingHeadline)
		{
			from = DisplayedIncomingHeadline;
			value = VariantUtils.CreateFrom(in from);
			return true;
		}
		if (name == PropertyName.AimActive)
		{
			from2 = AimActive;
			value = VariantUtils.CreateFrom(in from2);
			return true;
		}
		if (name == PropertyName.BattleUnit)
		{
			from3 = BattleUnit;
			value = VariantUtils.CreateFrom(in from3);
			return true;
		}
		if (name == PropertyName.UxCardScale)
		{
			from3 = UxCardScale;
			value = VariantUtils.CreateFrom(in from3);
			return true;
		}
		if (name == PropertyName._session)
		{
			value = VariantUtils.CreateFrom(in _session);
			return true;
		}
		if (name == PropertyName._body)
		{
			value = VariantUtils.CreateFrom(in _body);
			return true;
		}
		if (name == PropertyName._shell)
		{
			value = VariantUtils.CreateFrom(in _shell);
			return true;
		}
		if (name == PropertyName._overlayNode)
		{
			value = VariantUtils.CreateFrom(in _overlayNode);
			return true;
		}
		if (name == PropertyName._overlayTitle)
		{
			value = VariantUtils.CreateFrom(in _overlayTitle);
			return true;
		}
		if (name == PropertyName._overlayCanClose)
		{
			value = VariantUtils.CreateFrom(in _overlayCanClose);
			return true;
		}
		if (name == PropertyName._showTitle)
		{
			value = VariantUtils.CreateFrom(in _showTitle);
			return true;
		}
		if (name == PropertyName._notice)
		{
			value = VariantUtils.CreateFrom(in _notice);
			return true;
		}
		if (name == PropertyName._error)
		{
			value = VariantUtils.CreateFrom(in _error);
			return true;
		}
		if (name == PropertyName._leaderId)
		{
			value = VariantUtils.CreateFrom(in _leaderId);
			return true;
		}
		if (name == PropertyName._deckId)
		{
			value = VariantUtils.CreateFrom(in _deckId);
			return true;
		}
		if (name == PropertyName._seed)
		{
			value = VariantUtils.CreateFrom(in _seed);
			return true;
		}
		if (name == PropertyName._difficulty)
		{
			value = VariantUtils.CreateFrom(in _difficulty);
			return true;
		}
		if (name == PropertyName._preferredTarget)
		{
			value = VariantUtils.CreateFrom(in _preferredTarget);
			return true;
		}
		if (name == PropertyName._workshopA)
		{
			value = VariantUtils.CreateFrom(in _workshopA);
			return true;
		}
		if (name == PropertyName._workshopB)
		{
			value = VariantUtils.CreateFrom(in _workshopB);
			return true;
		}
		if (name == PropertyName._textScale)
		{
			value = VariantUtils.CreateFrom(in _textScale);
			return true;
		}
		if (name == PropertyName._masterVolume)
		{
			value = VariantUtils.CreateFrom(in _masterVolume);
			return true;
		}
		if (name == PropertyName._bgmVolume)
		{
			value = VariantUtils.CreateFrom(in _bgmVolume);
			return true;
		}
		if (name == PropertyName._sfxVolume)
		{
			value = VariantUtils.CreateFrom(in _sfxVolume);
			return true;
		}
		if (name == PropertyName._muted)
		{
			value = VariantUtils.CreateFrom(in _muted);
			return true;
		}
		if (name == PropertyName._graphicsQuality)
		{
			value = VariantUtils.CreateFrom(in _graphicsQuality);
			return true;
		}
		if (name == PropertyName._safeMargin)
		{
			value = VariantUtils.CreateFrom(in _safeMargin);
			return true;
		}
		if (name == PropertyName._handScroll)
		{
			value = VariantUtils.CreateFrom(in _handScroll);
			return true;
		}
		if (name == PropertyName._noteDraft)
		{
			value = VariantUtils.CreateFrom(in _noteDraft);
			return true;
		}
		if (name == PropertyName._hpReadout)
		{
			value = VariantUtils.CreateFrom(in _hpReadout);
			return true;
		}
		if (name == PropertyName._energyReadout)
		{
			value = VariantUtils.CreateFrom(in _energyReadout);
			return true;
		}
		if (name == PropertyName._blockReadout)
		{
			value = VariantUtils.CreateFrom(in _blockReadout);
			return true;
		}
		if (name == PropertyName._statusReadout)
		{
			value = VariantUtils.CreateFrom(in _statusReadout);
			return true;
		}
		if (name == PropertyName._costPresented)
		{
			value = VariantUtils.CreateFrom(in _costPresented);
			return true;
		}
		if (name == PropertyName._sparksRemaining)
		{
			value = VariantUtils.CreateFrom(in _sparksRemaining);
			return true;
		}
		if (name == PropertyName._pumpingSparks)
		{
			value = VariantUtils.CreateFrom(in _pumpingSparks);
			return true;
		}
		if (name == PropertyName._ratingQueued)
		{
			value = VariantUtils.CreateFrom(in _ratingQueued);
			return true;
		}
		if (name == PropertyName._shareAwaiting)
		{
			value = VariantUtils.CreateFrom(in _shareAwaiting);
			return true;
		}
		if (name == PropertyName._reactionMode)
		{
			value = VariantUtils.CreateFrom(in _reactionMode);
			return true;
		}
		if (name == PropertyName._setAnyCards)
		{
			value = VariantUtils.CreateFrom(in _setAnyCards);
			return true;
		}
		if (name == PropertyName._uxBattle)
		{
			value = VariantUtils.CreateFrom(in _uxBattle);
			return true;
		}
		if (name == PropertyName._uxHand)
		{
			value = VariantUtils.CreateFrom(in _uxHand);
			return true;
		}
		if (name == PropertyName._uxAim)
		{
			value = VariantUtils.CreateFrom(in _uxAim);
			return true;
		}
		if (name == PropertyName._uxHpBar)
		{
			value = VariantUtils.CreateFrom(in _uxHpBar);
			return true;
		}
		if (name == PropertyName._uxOrb)
		{
			value = VariantUtils.CreateFrom(in _uxOrb);
			return true;
		}
		if (name == PropertyName._uxForecast)
		{
			value = VariantUtils.CreateFrom(in _uxForecast);
			return true;
		}
		if (name == PropertyName._uxDetail)
		{
			value = VariantUtils.CreateFrom(in _uxDetail);
			return true;
		}
		if (name == PropertyName._uxPhase)
		{
			value = VariantUtils.CreateFrom(in _uxPhase);
			return true;
		}
		if (name == PropertyName._uxHover)
		{
			value = VariantUtils.CreateFrom(in _uxHover);
			return true;
		}
		if (name == PropertyName._uxDrag)
		{
			value = VariantUtils.CreateFrom(in _uxDrag);
			return true;
		}
		if (name == PropertyName._uxEpoch)
		{
			value = VariantUtils.CreateFrom(in _uxEpoch);
			return true;
		}
		if (name == PropertyName._uxLastSize)
		{
			value = VariantUtils.CreateFrom(in _uxLastSize);
			return true;
		}
		if (name == PropertyName._uxRelayoutQueued)
		{
			value = VariantUtils.CreateFrom(in _uxRelayoutQueued);
			return true;
		}
		if (name == PropertyName._uxIncoming)
		{
			value = VariantUtils.CreateFrom(in _uxIncoming);
			return true;
		}
		if (name == PropertyName._uxLanded)
		{
			value = VariantUtils.CreateFrom(in _uxLanded);
			return true;
		}
		if (name == PropertyName._uxSkipFirst)
		{
			value = VariantUtils.CreateFrom(in _uxSkipFirst);
			return true;
		}
		if (name == PropertyName._uxIncomingOwner)
		{
			value = VariantUtils.CreateFrom(in _uxIncomingOwner);
			return true;
		}
		return base.GetGodotClassPropertyValue(in name, out value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		return new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Object, PropertyName._session, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._body, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._shell, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._overlayNode, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.String, PropertyName._overlayTitle, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName._overlayCanClose, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName._showTitle, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.String, PropertyName._notice, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName._error, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.String, PropertyName._leaderId, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.String, PropertyName._deckId, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName._seed, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName._difficulty, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName._preferredTarget, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName._workshopA, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName._workshopB, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName._textScale, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName._masterVolume, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName._bgmVolume, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName._sfxVolume, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName._muted, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.String, PropertyName._graphicsQuality, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._safeMargin, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._handScroll, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.String, PropertyName._noteDraft, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._hpReadout, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._energyReadout, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._blockReadout, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._statusReadout, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName._costPresented, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName._sparksRemaining, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName._pumpingSparks, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName._ratingQueued, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName._shareAwaiting, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.String, PropertyName._reactionMode, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName._setAnyCards, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.String, PropertyName.GraphicsQuality, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName.PhoneLayout, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName.MinimumHit, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName.IsInputBlocked, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.String, PropertyName.CurrentRoute, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.String, PropertyName.ActiveModal, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.RejectedReceiptCount, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.PendingSparkPlays, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.PresentedNumericContactCount, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._uxBattle, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._uxHand, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._uxAim, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._uxHpBar, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._uxOrb, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._uxForecast, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._uxDetail, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._uxPhase, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.String, PropertyName._uxHover, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.String, PropertyName._uxDrag, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName._uxEpoch, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Vector2, PropertyName._uxLastSize, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName._uxRelayoutQueued, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName._uxIncoming, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName._uxLanded, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName._uxSkipFirst, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName._uxIncomingOwner, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.DisplayedIncomingRemaining, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName.DisplayedIncomingLanded, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.String, PropertyName.DisplayedIncomingHeadline, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName.AimActive, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName.BattleUnit, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName.UxCardScale, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		base.SaveGodotObjectData(info);
		info.AddProperty(PropertyName.RejectedReceiptCount, Variant.From<int>(RejectedReceiptCount));
		info.AddProperty(PropertyName.PresentedNumericContactCount, Variant.From<int>(PresentedNumericContactCount));
		info.AddProperty(PropertyName._session, Variant.From(in _session));
		info.AddProperty(PropertyName._body, Variant.From(in _body));
		info.AddProperty(PropertyName._shell, Variant.From(in _shell));
		info.AddProperty(PropertyName._overlayNode, Variant.From(in _overlayNode));
		info.AddProperty(PropertyName._overlayTitle, Variant.From(in _overlayTitle));
		info.AddProperty(PropertyName._overlayCanClose, Variant.From(in _overlayCanClose));
		info.AddProperty(PropertyName._showTitle, Variant.From(in _showTitle));
		info.AddProperty(PropertyName._notice, Variant.From(in _notice));
		info.AddProperty(PropertyName._error, Variant.From(in _error));
		info.AddProperty(PropertyName._leaderId, Variant.From(in _leaderId));
		info.AddProperty(PropertyName._deckId, Variant.From(in _deckId));
		info.AddProperty(PropertyName._seed, Variant.From(in _seed));
		info.AddProperty(PropertyName._difficulty, Variant.From(in _difficulty));
		info.AddProperty(PropertyName._preferredTarget, Variant.From(in _preferredTarget));
		info.AddProperty(PropertyName._workshopA, Variant.From(in _workshopA));
		info.AddProperty(PropertyName._workshopB, Variant.From(in _workshopB));
		info.AddProperty(PropertyName._textScale, Variant.From(in _textScale));
		info.AddProperty(PropertyName._masterVolume, Variant.From(in _masterVolume));
		info.AddProperty(PropertyName._bgmVolume, Variant.From(in _bgmVolume));
		info.AddProperty(PropertyName._sfxVolume, Variant.From(in _sfxVolume));
		info.AddProperty(PropertyName._muted, Variant.From(in _muted));
		info.AddProperty(PropertyName._graphicsQuality, Variant.From(in _graphicsQuality));
		info.AddProperty(PropertyName._safeMargin, Variant.From(in _safeMargin));
		info.AddProperty(PropertyName._handScroll, Variant.From(in _handScroll));
		info.AddProperty(PropertyName._noteDraft, Variant.From(in _noteDraft));
		info.AddProperty(PropertyName._hpReadout, Variant.From(in _hpReadout));
		info.AddProperty(PropertyName._energyReadout, Variant.From(in _energyReadout));
		info.AddProperty(PropertyName._blockReadout, Variant.From(in _blockReadout));
		info.AddProperty(PropertyName._statusReadout, Variant.From(in _statusReadout));
		info.AddProperty(PropertyName._costPresented, Variant.From(in _costPresented));
		info.AddProperty(PropertyName._sparksRemaining, Variant.From(in _sparksRemaining));
		info.AddProperty(PropertyName._pumpingSparks, Variant.From(in _pumpingSparks));
		info.AddProperty(PropertyName._ratingQueued, Variant.From(in _ratingQueued));
		info.AddProperty(PropertyName._shareAwaiting, Variant.From(in _shareAwaiting));
		info.AddProperty(PropertyName._reactionMode, Variant.From(in _reactionMode));
		info.AddProperty(PropertyName._setAnyCards, Variant.From(in _setAnyCards));
		info.AddProperty(PropertyName._uxBattle, Variant.From(in _uxBattle));
		info.AddProperty(PropertyName._uxHand, Variant.From(in _uxHand));
		info.AddProperty(PropertyName._uxAim, Variant.From(in _uxAim));
		info.AddProperty(PropertyName._uxHpBar, Variant.From(in _uxHpBar));
		info.AddProperty(PropertyName._uxOrb, Variant.From(in _uxOrb));
		info.AddProperty(PropertyName._uxForecast, Variant.From(in _uxForecast));
		info.AddProperty(PropertyName._uxDetail, Variant.From(in _uxDetail));
		info.AddProperty(PropertyName._uxPhase, Variant.From(in _uxPhase));
		info.AddProperty(PropertyName._uxHover, Variant.From(in _uxHover));
		info.AddProperty(PropertyName._uxDrag, Variant.From(in _uxDrag));
		info.AddProperty(PropertyName._uxEpoch, Variant.From(in _uxEpoch));
		info.AddProperty(PropertyName._uxLastSize, Variant.From(in _uxLastSize));
		info.AddProperty(PropertyName._uxRelayoutQueued, Variant.From(in _uxRelayoutQueued));
		info.AddProperty(PropertyName._uxIncoming, Variant.From(in _uxIncoming));
		info.AddProperty(PropertyName._uxLanded, Variant.From(in _uxLanded));
		info.AddProperty(PropertyName._uxSkipFirst, Variant.From(in _uxSkipFirst));
		info.AddProperty(PropertyName._uxIncomingOwner, Variant.From(in _uxIncomingOwner));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		base.RestoreGodotObjectData(info);
		if (info.TryGetProperty(PropertyName.RejectedReceiptCount, out var _value_RejectedReceiptCount))
		{
			RejectedReceiptCount = _value_RejectedReceiptCount.As<int>();
		}
		if (info.TryGetProperty(PropertyName.PresentedNumericContactCount, out var _value_PresentedNumericContactCount))
		{
			PresentedNumericContactCount = _value_PresentedNumericContactCount.As<int>();
		}
		if (info.TryGetProperty(PropertyName._session, out var _value__session))
		{
			_session = _value__session.As<GameSession>();
		}
		if (info.TryGetProperty(PropertyName._body, out var _value__body))
		{
			_body = _value__body.As<VBoxContainer>();
		}
		if (info.TryGetProperty(PropertyName._shell, out var _value__shell))
		{
			_shell = _value__shell.As<VBoxContainer>();
		}
		if (info.TryGetProperty(PropertyName._overlayNode, out var _value__overlayNode))
		{
			_overlayNode = _value__overlayNode.As<Control>();
		}
		if (info.TryGetProperty(PropertyName._overlayTitle, out var _value__overlayTitle))
		{
			_overlayTitle = _value__overlayTitle.As<string>();
		}
		if (info.TryGetProperty(PropertyName._overlayCanClose, out var _value__overlayCanClose))
		{
			_overlayCanClose = _value__overlayCanClose.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._showTitle, out var _value__showTitle))
		{
			_showTitle = _value__showTitle.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._notice, out var _value__notice))
		{
			_notice = _value__notice.As<string>();
		}
		if (info.TryGetProperty(PropertyName._error, out var _value__error))
		{
			_error = _value__error.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._leaderId, out var _value__leaderId))
		{
			_leaderId = _value__leaderId.As<string>();
		}
		if (info.TryGetProperty(PropertyName._deckId, out var _value__deckId))
		{
			_deckId = _value__deckId.As<string>();
		}
		if (info.TryGetProperty(PropertyName._seed, out var _value__seed))
		{
			_seed = _value__seed.As<int>();
		}
		if (info.TryGetProperty(PropertyName._difficulty, out var _value__difficulty))
		{
			_difficulty = _value__difficulty.As<int>();
		}
		if (info.TryGetProperty(PropertyName._preferredTarget, out var _value__preferredTarget))
		{
			_preferredTarget = _value__preferredTarget.As<int>();
		}
		if (info.TryGetProperty(PropertyName._workshopA, out var _value__workshopA))
		{
			_workshopA = _value__workshopA.As<int>();
		}
		if (info.TryGetProperty(PropertyName._workshopB, out var _value__workshopB))
		{
			_workshopB = _value__workshopB.As<int>();
		}
		if (info.TryGetProperty(PropertyName._textScale, out var _value__textScale))
		{
			_textScale = _value__textScale.As<float>();
		}
		if (info.TryGetProperty(PropertyName._masterVolume, out var _value__masterVolume))
		{
			_masterVolume = _value__masterVolume.As<float>();
		}
		if (info.TryGetProperty(PropertyName._bgmVolume, out var _value__bgmVolume))
		{
			_bgmVolume = _value__bgmVolume.As<float>();
		}
		if (info.TryGetProperty(PropertyName._sfxVolume, out var _value__sfxVolume))
		{
			_sfxVolume = _value__sfxVolume.As<float>();
		}
		if (info.TryGetProperty(PropertyName._muted, out var _value__muted))
		{
			_muted = _value__muted.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._graphicsQuality, out var _value__graphicsQuality))
		{
			_graphicsQuality = _value__graphicsQuality.As<string>();
		}
		if (info.TryGetProperty(PropertyName._safeMargin, out var _value__safeMargin))
		{
			_safeMargin = _value__safeMargin.As<MarginContainer>();
		}
		if (info.TryGetProperty(PropertyName._handScroll, out var _value__handScroll))
		{
			_handScroll = _value__handScroll.As<ScrollContainer>();
		}
		if (info.TryGetProperty(PropertyName._noteDraft, out var _value__noteDraft))
		{
			_noteDraft = _value__noteDraft.As<string>();
		}
		if (info.TryGetProperty(PropertyName._hpReadout, out var _value__hpReadout))
		{
			_hpReadout = _value__hpReadout.As<Label>();
		}
		if (info.TryGetProperty(PropertyName._energyReadout, out var _value__energyReadout))
		{
			_energyReadout = _value__energyReadout.As<Label>();
		}
		if (info.TryGetProperty(PropertyName._blockReadout, out var _value__blockReadout))
		{
			_blockReadout = _value__blockReadout.As<Label>();
		}
		if (info.TryGetProperty(PropertyName._statusReadout, out var _value__statusReadout))
		{
			_statusReadout = _value__statusReadout.As<Label>();
		}
		if (info.TryGetProperty(PropertyName._costPresented, out var _value__costPresented))
		{
			_costPresented = _value__costPresented.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._sparksRemaining, out var _value__sparksRemaining))
		{
			_sparksRemaining = _value__sparksRemaining.As<int>();
		}
		if (info.TryGetProperty(PropertyName._pumpingSparks, out var _value__pumpingSparks))
		{
			_pumpingSparks = _value__pumpingSparks.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._ratingQueued, out var _value__ratingQueued))
		{
			_ratingQueued = _value__ratingQueued.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._shareAwaiting, out var _value__shareAwaiting))
		{
			_shareAwaiting = _value__shareAwaiting.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._reactionMode, out var _value__reactionMode))
		{
			_reactionMode = _value__reactionMode.As<string>();
		}
		if (info.TryGetProperty(PropertyName._setAnyCards, out var _value__setAnyCards))
		{
			_setAnyCards = _value__setAnyCards.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._uxBattle, out var _value__uxBattle))
		{
			_uxBattle = _value__uxBattle.As<Control>();
		}
		if (info.TryGetProperty(PropertyName._uxHand, out var _value__uxHand))
		{
			_uxHand = _value__uxHand.As<Control>();
		}
		if (info.TryGetProperty(PropertyName._uxAim, out var _value__uxAim))
		{
			_uxAim = _value__uxAim.As<SourceAimArrow>();
		}
		if (info.TryGetProperty(PropertyName._uxHpBar, out var _value__uxHpBar))
		{
			_uxHpBar = _value__uxHpBar.As<SourceHealthBar>();
		}
		if (info.TryGetProperty(PropertyName._uxOrb, out var _value__uxOrb))
		{
			_uxOrb = _value__uxOrb.As<SourceEnergyOrb>();
		}
		if (info.TryGetProperty(PropertyName._uxForecast, out var _value__uxForecast))
		{
			_uxForecast = _value__uxForecast.As<RichTextLabel>();
		}
		if (info.TryGetProperty(PropertyName._uxDetail, out var _value__uxDetail))
		{
			_uxDetail = _value__uxDetail.As<RichTextLabel>();
		}
		if (info.TryGetProperty(PropertyName._uxPhase, out var _value__uxPhase))
		{
			_uxPhase = _value__uxPhase.As<Label>();
		}
		if (info.TryGetProperty(PropertyName._uxHover, out var _value__uxHover))
		{
			_uxHover = _value__uxHover.As<string>();
		}
		if (info.TryGetProperty(PropertyName._uxDrag, out var _value__uxDrag))
		{
			_uxDrag = _value__uxDrag.As<string>();
		}
		if (info.TryGetProperty(PropertyName._uxEpoch, out var _value__uxEpoch))
		{
			_uxEpoch = _value__uxEpoch.As<long>();
		}
		if (info.TryGetProperty(PropertyName._uxLastSize, out var _value__uxLastSize))
		{
			_uxLastSize = _value__uxLastSize.As<Vector2>();
		}
		if (info.TryGetProperty(PropertyName._uxRelayoutQueued, out var _value__uxRelayoutQueued))
		{
			_uxRelayoutQueued = _value__uxRelayoutQueued.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._uxIncoming, out var _value__uxIncoming))
		{
			_uxIncoming = _value__uxIncoming.As<int>();
		}
		if (info.TryGetProperty(PropertyName._uxLanded, out var _value__uxLanded))
		{
			_uxLanded = _value__uxLanded.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._uxSkipFirst, out var _value__uxSkipFirst))
		{
			_uxSkipFirst = _value__uxSkipFirst.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._uxIncomingOwner, out var _value__uxIncomingOwner))
		{
			_uxIncomingOwner = _value__uxIncomingOwner.As<long>();
		}
	}
}

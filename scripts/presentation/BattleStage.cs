using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using DeckRogue.Engine.Generated;
using DeckRogue.Game;
using DeckRogue.GodotPort.Core;
using DeckRogue.GodotPort.Debugging;
using DeckRogue.GodotPort.UI;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using Newtonsoft.Json.Linq;

namespace DeckRogue.GodotPort.Presentation;

[ScriptPath("res://scripts/presentation/BattleStage.cs")]
public class BattleStage : Node3D
{
	private sealed class CommandOwner
	{
		public SessionChange Change;

		public CombatPresentationLedger.Batch? Ledger;

		public bool Fenced;

		public bool Finished;

		public int RemainingActions;

		public CombatActionQueue.ActionToken? FenceToken;
	}

	private sealed class ActionPlayback
	{
		public CommandOwner Owner;

		public CombatActionQueue.ActionToken Token;

		public readonly List<OrdinaryBeat> Beats = new List<OrdinaryBeat>();

		public bool Launched;

        public bool PrototypeOnsetPlayed;
        // Existing primary CardPlayed metadata only: never inserted/replayed as a beat.
        public GameEvent_CardPlayed? PrimaryContinuationCard;
        public string? PrimaryContinuationUid;
        public bool PrimaryContinuationSourceStarted;
        public bool PrototypeRemoteStrike;
        public Vector3? PrototypeNearSurface;
        public Vector3? PrototypeTransferOrigin;
        public string PrototypeApproachReason="";
        public Vector3 PrototypeApproachTravel;

		public bool Finished;

		public float LaunchedAt;

		public bool EnemyPhase;

		public float NotBeforeFinish;

		public GameEvent_CardPlayed? Card;

		public SourceShowcaseProfile? Profile;

		public int Volleys = 1;
        public int VisualVolleys = 1;

		public BattleActor? Actor;

		public SourceActorMotion.OwnershipToken? Capture;
	}

	// Bounded visual ownership only. No queue/fence or numeric event ownership.
	private sealed class SupportPlayback
	{
		public BattleActor Actor;
		public ActionPlayback Action;
		public Style04EnemySupportMotion.Token Token;
		public int Generation, EnemyIndex;
		public string EnemyId, Kind;
        public DrummerAppliedOncePlan? DrummerPlan;
        public HexAppliedOncePlan? HexPlan;
        public DevourerMillAppliedOncePlan? MillPlan;
        public MossHealAppliedOncePlan? HealPlan;
        public BattleActor? HealTarget;
        public GameEvent_TokenDestroyed? TokenDestroyed;
        public int TokenDestroyedIndex=-1;
	}
	private SupportPlayback? _supportPlayback;

	private sealed class EffectPlayback
	{
		public AuthoredGeometryFx Native;

		public ContinuousAttackFx Fallback;
	}

	private sealed class OrdinaryBeat
	{
		public PresentationBeat Beat;

		public ActionPlayback Action;

		public int Index;

		public float ContactAt;

		public float PlannedContactAt;

		public bool EnemyLungeScheduled;

		public BattleActor? EnemyLungeActor;

		public int EnemyLungeVersion;

		public float PreviewAt;

        public bool IndependentScatterPreview;

		public float StartedAt;

		public float Time;

		public float EffectStartedAt;

		public float EffectOffset;

		public float LastContact;

		public float ContactRetimed;

		public readonly SourceFrameDelay InnerDelay = new SourceFrameDelay();

		public bool Started;

		public bool Contacted;

		public bool Rejected;

		public bool Projection;

		public int Hp;

		public int Block;

		public int ContactOrdinal = -1;

		public BattleActor? Source;

		public BattleActor? Target;

		public EnemyAttackContactPlan.SourceProof? SourceProof;

		public EffectPlayback? Effect;

		public int Volley;
        public int TargetOrdinal;

		public bool Direct;

		public bool Flashed;

		public BattleActor? CapturedTarget;
	}

	private sealed record DeathState(float Started, float Delay, float Duration, bool Retired, bool Fled);

	private sealed class SemanticPlacement
	{
		public long Id;

		public SemanticEventCue Cue;

		public Label3D? Label;

		public BattleActor? Actor;

		public Vector3 Anchor;

		public Rect2 Preferred;

		public bool Found;
	}

	public new class MethodName : Node3D.MethodName
	{
		public static readonly StringName GetEnemyId = "GetEnemyId";

		public static readonly StringName GetEnemyHp = "GetEnemyHp";

		public static readonly StringName GetEnemyBlock = "GetEnemyBlock";

		public static readonly StringName GetEnemyIntent = "GetEnemyIntent";

		public static readonly StringName PlayUiSound = "PlayUiSound";

		public static readonly StringName ApplyQuality = "ApplyQuality";

		public static readonly StringName ApplyQualitySettings = "ApplyQualitySettings";

		public static readonly StringName SampleQuality = "SampleQuality";

		public new static readonly StringName _Ready = "_Ready";

		public static readonly StringName EnsureBuilt = "EnsureBuilt";

		public static readonly StringName Bind = "Bind";

		public static readonly StringName SetInterfaceRoute = "SetInterfaceRoute";

		public static readonly StringName ClearRun = "ClearRun";

		public static readonly StringName Path = "Path";

		public static readonly StringName EnemyFoot = "EnemyFoot";

		public static readonly StringName NameFor = "NameFor";

		public static readonly StringName RefreshActorLayout = "RefreshActorLayout";

		public static readonly StringName StatusName = "StatusName";

		public static readonly StringName SyncLedgerActor = "SyncLedgerActor";

		public static readonly StringName ShowReceipt = "ShowReceipt";

		public static readonly StringName FinishBatch = "FinishBatch";

		public new static readonly StringName _Process = "_Process";

		public static readonly StringName AdvanceClock = "AdvanceClock";

		public static readonly StringName AdvancePresentation = "AdvancePresentation";

		public static readonly StringName CancelPresentation = "CancelPresentation";

		public static readonly StringName StartDeath = "StartDeath";

		public static readonly StringName AdvanceDeath = "AdvanceDeath";

		public static readonly StringName ClearRetired = "ClearRetired";

		public static readonly StringName CacheWorldLights = "CacheWorldLights";

		public static readonly StringName RefreshForestLighting = "RefreshForestLighting";

		public static readonly StringName RefreshActorLighting = "RefreshActorLighting";

		public static readonly StringName BindActorLights = "BindActorLights";

		public static readonly StringName ClearActors = "ClearActors";

		public new static readonly StringName _UnhandledInput = "_UnhandledInput";

		public new static readonly StringName _ExitTree = "_ExitTree";

		public static readonly StringName ProjectCueBounds = "ProjectCueBounds";

		public static readonly StringName PresentBlockBoundary = "PresentBlockBoundary";
	}

	public new class PropertyName : Node3D.PropertyName
	{
		public static readonly StringName IsPresenting = "IsPresenting";

		public static readonly StringName DisplayedHeroHp = "DisplayedHeroHp";

		public static readonly StringName DisplayedHeroBlock = "DisplayedHeroBlock";

		public static readonly StringName DisplayedEnemyCount = "DisplayedEnemyCount";

		public static readonly StringName ActorsVisible = "ActorsVisible";

		public static readonly StringName CurrentAudioTrack = "CurrentAudioTrack";

		public static readonly StringName MusicPlaying = "MusicPlaying";

		public static readonly StringName DamageAudioCount = "DamageAudioCount";

		public static readonly StringName PlayingSoundEffects = "PlayingSoundEffects";

		public static readonly StringName DamageContactCount = "DamageContactCount";

		public static readonly StringName PresentedRevision = "PresentedRevision";

		public static readonly StringName PendingEventCount = "PendingEventCount";

		public static readonly StringName ActiveEffectCount = "ActiveEffectCount";

		public static readonly StringName ActiveLightCount = "ActiveLightCount";

		public static readonly StringName RejectedReceiptCount = "RejectedReceiptCount";

		public static readonly StringName CompletedEventCount = "CompletedEventCount";

		public static readonly StringName PresentationTime = "PresentationTime";

		public static readonly StringName ActiveEventType = "ActiveEventType";

		public static readonly StringName ActiveFamily = "ActiveFamily";

		public static readonly StringName ActiveTier = "ActiveTier";

		public static readonly StringName RealLightsEnabled = "RealLightsEnabled";

		public static readonly StringName QualityProfile = "QualityProfile";

		public static readonly StringName EffectiveQualityProfile = "EffectiveQualityProfile";

		public static readonly StringName IsPhoneQuality = "IsPhoneQuality";

		public static readonly StringName PlaybackSpeed = "PlaybackSpeed";

		public static readonly StringName PlaybackPaused = "PlaybackPaused";

		public static readonly StringName LastEventReceiptRejected = "LastEventReceiptRejected";

		public static readonly StringName ActiveSemanticCueEventType = "ActiveSemanticCueEventType";

		public static readonly StringName ActiveSemanticCuePlacementFound = "ActiveSemanticCuePlacementFound";

		public static readonly StringName PresentedSemanticCueCount = "PresentedSemanticCueCount";

		public static readonly StringName RejectedSemanticCueCount = "RejectedSemanticCueCount";

		public static readonly StringName ActiveCueCount = "ActiveCueCount";

		public static readonly StringName PresentedBlockBoundaryCount = "PresentedBlockBoundaryCount";

		public static readonly StringName _effectStartedAt = "_effectStartedAt";

		public static readonly StringName _effectOffset = "_effectOffset";

		public static readonly StringName _contactHp = "_contactHp";

		public static readonly StringName _contactBlock = "_contactBlock";

		public static readonly StringName _qualityApplied = "_qualityApplied";

		public static readonly StringName _projectionActive = "_projectionActive";

		public static readonly StringName _rejectBeat = "_rejectBeat";

		public static readonly StringName _interfaceRoute = "_interfaceRoute";

		public static readonly StringName _native = "_native";

		public static readonly StringName _session = "_session";

		public static readonly StringName _hero = "_hero";

		public static readonly StringName _sourceActor = "_sourceActor";

		public static readonly StringName _targetActor = "_targetActor";

		public static readonly StringName _retiredRoot = "_retiredRoot";

		public static readonly StringName _actorRoot = "_actorRoot";

		public static readonly StringName _fx = "_fx";

		public static readonly StringName _world = "_world";

		public static readonly StringName _audio = "_audio";

		public static readonly StringName _cues = "_cues";

		public static readonly StringName _receipt = "_receipt";

		public static readonly StringName _lastRevision = "_lastRevision";

		public static readonly StringName _clock = "_clock";

		public static readonly StringName _act = "_act";

		public static readonly StringName _initialized = "_initialized";

		public static readonly StringName _resetting = "_resetting";

		public static readonly StringName _receiptHome = "_receiptHome";

		public static readonly StringName _semanticPlacementId = "_semanticPlacementId";

		public static readonly StringName _enemyBlockExpiryPending = "_enemyBlockExpiryPending";

		public static readonly StringName _blockBoundaryCommand = "_blockBoundaryCommand";
	}

	public new class SignalName : Node3D.SignalName
	{
	}

	private sealed record SourceFloat(Label Label, Vector2 Origin, Vector2 Size, float Unit, float Start);

	private sealed record SourceEnemyBlockFloat(Label Label, GameEvent_BlockGained Event, long Revision, int Generation, int EnemyIndex, Vector2 Origin, Vector2 Size, float Unit, float Start);

	private sealed record SourceHeroBlockFloat(Label Label, GameEvent_BlockGained Event, long Revision, int Generation, ulong ActorInstance, Vector2 Origin, Vector2 Size, float Unit, float Start);

	private sealed record SourceIncomingLabel(Label Label, SourceIncomingFloatModel.Spec Spec, Vector2 Size, Vector2 InkSize, Rect2 InkRect, Rect2 SweptInk, SourceIncomingGlyphBounds.Measurement GlyphMeasurement)
	{
		public float? Started;
	}

	private sealed record SourceIncomingFloat(GameEvent_DamageDealt Event, long Revision, int Generation, int ContributionOrdinal, bool Individual, ulong ActorInstance, int Amount, int HpLoss, int Absorbed, Vector2 SourceOrigin, Vector2 Origin, Rect2 SafeRect, Rect2 Envelope, float Unit, float Start, List<SourceIncomingLabel> Labels, Rect2 CollisionEnvelope, bool CollisionFree, bool AvoidanceClear, List<Rect2> AvoidanceRects, IReadOnlyList<Rect2> SweptRects, Rect2 Region, SourceIncomingRegionGeometry RegionGeometry, bool RegionContained, string PlacementStatus);

	private sealed record SourceIncomingRegionGeometry(Rect2 Region, Rect2 HeroBody, Rect2 FullHeroQuad, Vector2 HeroFeet, float HeaderBottom, float HandTop, float NearestEnemyFeetX, bool HasNearestEnemy, string HeroTexturePath, string BoundsMode, SourceIncomingHeroBounds.Metadata? HeroMetadata, List<Rect2> AvoidanceRects);

	private readonly CombatActionQueue _actionQueue = new CombatActionQueue();

	private readonly List<CommandOwner> _commands = new List<CommandOwner>();

	private readonly Dictionary<GameEvent, CommandOwner> _eventOwners = new Dictionary<GameEvent, CommandOwner>(ReferenceEqualityComparer.Instance);

	private readonly List<ActionPlayback> _ordinaryActions = new List<ActionPlayback>();

	private readonly Stack<EffectPlayback> _effectPool = new Stack<EffectPlayback>();

	private CommandOwner? _fenceCommand;

	private ActionPlayback? _actorAction;

	private OrdinaryBeat? _ordinaryCallback;

	private float _lastVolleyAt = -1f;

	private int _presentationFrame;

	private int _ordinaryCompletedEvents;

	private int _effectPoolSerial;

	private int _lastOrdinaryVisualFrame = -1;

	private GameEvent? _lastPresentedEvent;

	private CombatPresentationVitals? _lastPresentedVitals;

	private float _effectStartedAt;

	private float _effectOffset;

	private int _contactHp;

	private int _contactBlock;

	private readonly HashSet<string> _bornUids = new HashSet<string>();

	private readonly Queue<string> _copiedEnter = new Queue<string>();

	private readonly AdaptiveQuality _quality = new AdaptiveQuality();

	private bool _qualityApplied;

	private readonly PresentationTimeline _timeline = new PresentationTimeline();

	private readonly CombatPresentationLedger _ledger = new CombatPresentationLedger();

	private readonly Dictionary<GameEvent, CombatPresentationLedger.Receipt> _receipts = new Dictionary<GameEvent, CombatPresentationLedger.Receipt>(ReferenceEqualityComparer.Instance);

	private CombatPresentationLedger.Batch? _ledgerBatch;

	private EnemyAttackContactPlan.SourceProof? _sourceProof;

	private bool _projectionActive;

	private bool _rejectBeat;

	private string _interfaceRoute = "title";

	private AuthoredGeometryFx _native;

	private readonly Dictionary<int, BattleActor> _enemies = new Dictionary<int, BattleActor>();

	private readonly List<OmniLight3D> _worldLights = new List<OmniLight3D>();

	private readonly List<ActorMaterialBinding.PointLight> _actorLights = new List<ActorMaterialBinding.PointLight>(8);

	private readonly Dictionary<string, BattleActor> _retainers = new Dictionary<string, BattleActor>();

	private GameSession? _session;

	private RunState? _latestRun;

	private GameState? _displayState;

	private BattleActor? _hero;

	private BattleActor? _sourceActor;

	private BattleActor? _targetActor;

	private readonly Dictionary<BattleActor, DeathState> _deaths = new Dictionary<BattleActor, DeathState>();
	private bool _style04RosterLayoutDirty;

	private Node3D _retiredRoot;

	private Node3D _actorRoot;

	private ContinuousAttackFx _fx;

	private StageWorld _world;

	private BattleAudio _audio;

	private BattleCuePool _cues;

	private Label3D _receipt;

	private long _lastRevision = -1L;

	private float _clock;

	private int _act;

	private bool _initialized;

	private bool _resetting;

	private Vector3 _receiptHome;

	private Func<IReadOnlyList<Rect2>>? _cueOccluders;

	private long _semanticPlacementId;

	private SemanticPlacement? _lastSemanticPlacement;

	private bool _enemyBlockExpiryPending;

	private bool _blockBoundaryCommand;

	private readonly SourceCastLight _sourceCastLight = new SourceCastLight();

	private readonly List<SourceFloat> _sourceFloats = new List<SourceFloat>();

	private CanvasLayer? _sourceFloatHost;

	private Func<(float Unit, float TextScale)>? _sourceFloatMetrics;

	private readonly SourceDollSeats _sourceDollSeats = new SourceDollSeats();

	private readonly List<SourceEnemyBlockFloat> _sourceEnemyBlockFloats = new List<SourceEnemyBlockFloat>();

	private readonly List<SourceHeroBlockFloat> _sourceHeroBlockFloats = new List<SourceHeroBlockFloat>();

	private readonly List<SourceIncomingFloat> _sourceIncomingFloats = new List<SourceIncomingFloat>();

	private float _lastHostDelta;

	private float _lastHandoffPause;

	private float _finishSlowRemaining;

	private GameEvent? _incomingEvent;

	private int? _incomingAmount;

	private JObject? _sourceMotionProfiles;

	private CanvasLayer? _phaseLayer;

	private Control? _phaseBand;

	private Label? _phaseText;

	private ColorRect? _phaseAccent;

	private ColorRect? _injuryFlash;

	private float _bannerAt = -100f;

	private float _flashAt = -100f;

	private float _flashAlpha;

	private bool _bannerEnemy;

	private Func<IReadOnlyList<Rect2>>? _enemyLedgerBounds;

	// Only an observed exception enters this mode. There is no timeout or rule replay.
	public bool PresentationFaulted { get; private set; }
	public bool PresentationFaultBlocked { get; private set; }
	public Func<RunState, long, bool>? FaultSnapshotSynchronizer { get; set; }
	public int PresentationFaultCount { get; private set; }
	private bool _faultRecovering;
	private string _faultBoundary = "BattleStage.Advance";

	public bool InputLocked { get; private set; }

	public long ActiveCommandRevision => _fenceCommand?.Change.Revision ?? _actorAction?.Owner.Change.Revision ?? (-1);

	public int PendingCommandCount => _commands.Count((CommandOwner c) => !c.Finished);

	public int PendingActorActions => _actionQueue.QueuedCount;

	public int PendingActorContacts => _actionQueue.PendingContacts;

	public int ResidualEffectCount => _ordinaryActions.Sum((ActionPlayback a) => a.Beats.Count((OrdinaryBeat b) => b.Contacted && b.Effect != null));

	public int ActorActionLaunchCount { get; private set; }

	public float LastActorLaunchAt { get; private set; }

	public float LastActorContactAt { get; private set; }

	public long LastActorContactRevision { get; private set; } = -1L;

	public int LastActorContactFrame { get; private set; } = -1;

	public int LastActorLaunchFrame { get; private set; } = -1;

	public float LastActorHandoffPause => _lastHandoffPause;

	private int OrdinaryPendingEvents => _ordinaryActions.Sum((ActionPlayback a) => a.Beats.Count((OrdinaryBeat b) => !b.Contacted));

	private int OrdinaryActiveEffects => _ordinaryActions.Sum((ActionPlayback a) => a.Beats.Count((OrdinaryBeat b) =>
	{
		EffectPlayback? effect = b.Effect;
		return (effect != null && effect.Native.IsActive) || (b.Effect?.Fallback.Visible ?? false);
	}));

	private int OrdinaryActiveLights => _ordinaryActions.Sum((ActionPlayback a) => a.Beats.Sum((OrdinaryBeat b) => (b.Effect?.Native.ActiveLights ?? 0) + (b.Effect?.Fallback.ActiveLights ?? 0)));

	private OrdinaryBeat? ActiveOrdinaryBeat => _ordinaryActions.Where((ActionPlayback a) => a.Launched).SelectMany((ActionPlayback a) => a.Beats).LastOrDefault((OrdinaryBeat b) => b.Started && (!b.Contacted || b.Effect != null));

	private float BeatTime => _ordinaryCallback?.Time ?? _timeline.Time;

	private int BeatContactOrdinal => _ordinaryCallback?.ContactOrdinal ?? _timeline.ContactOrdinal;

	public bool IsPresenting { get; private set; }

	public GameState? DisplayedState => _displayState;

	public IReadOnlyList<string> DisplayedRetainerUids => _retainers.Keys.ToArray();

	public int DisplayedHeroHp => _hero?.DisplayedHp ?? 0;

	public int DisplayedHeroBlock => _hero?.DisplayedBlock ?? 0;

	public int DisplayedEnemyCount => _enemies.Count;

	public bool ActorsVisible => _actorRoot?.Visible ?? false;

	public string CurrentAudioTrack => _audio?.CurrentTrack ?? "";

	public bool MusicPlaying => _audio?.MusicPlaying ?? false;

	public int DamageAudioCount => _audio?.DamageAudioCount ?? 0;

	public int PlayingSoundEffects => _audio?.PlayingEffects ?? 0;

	public int DamageContactCount { get; private set; }

	public long PresentedRevision => _lastRevision;

	public int PendingEventCount => _timeline.PendingCount + (((object)_timeline.Active != null) ? 1 : 0) + OrdinaryPendingEvents;

	public int ActiveEffectCount
	{
		get
		{
			AuthoredGeometryFx native = _native;
			return ((native != null && native.IsActive) ? 1 : 0) + ((_fx != null && _fx.Visible) ? 1 : 0) + OrdinaryActiveEffects;
		}
	}

	public int ActiveLightCount => (_fx?.ActiveLights ?? 0) + (_native?.ActiveLights ?? 0) + OrdinaryActiveLights + SourceCastLightCount;

	public int RejectedReceiptCount { get; private set; }

	public int CompletedEventCount => _timeline.CompletedBeats + _ordinaryCompletedEvents;

	public float PresentationTime => _clock;

	public string ActiveEventType => _timeline.Active?.Event.Type ?? ActiveOrdinaryBeat?.Beat.Event.Type ?? "";

	public string ActiveFamily => _timeline.Active?.Route?.Family ?? "";

	public string ActiveTier => _timeline.Active?.Scenario?.Tier.ToString() ?? "";

	public bool RealLightsEnabled
	{
		get
		{
			return _fx?.RealLightsEnabled ?? true;
		}
		set
		{
			if (_fx != null)
			{
				_fx.RealLightsEnabled = value;
			}
			if (_native != null)
			{
				_native.RealLightsEnabled = value;
			}
		}
	}

	public string QualityProfile { get; private set; } = "auto";

	public string EffectiveQualityProfile => _quality.Level switch
	{
		0 => "high", 
		1 => "standard", 
		_ => "low", 
	};

	private bool IsPhoneQuality
	{
		get
		{
			if (!OS.HasFeature("android") && !OS.HasFeature("ios"))
			{
				return Enumerable.Contains(OS.GetCmdlineUserArgs(), "--ui-phone");
			}
			return true;
		}
	}

	public float PlaybackSpeed { get; set; } = 1f;

	public bool PlaybackPaused { get; set; }

	public AttackRouteCatalog Routes { get; } = new AttackRouteCatalog();

	public bool LastEventReceiptRejected { get; private set; }

	private SemanticPlacement? LiveSemanticPlacement
	{
		get
		{
			SemanticPlacement p = _lastSemanticPlacement;
			object result;
			if (p != null)
			{
				Label3D label = p.Label;
				if (label != null && GodotObject.IsInstanceValid(label) && label.IsVisibleInTree() && label.GetMeta("cue_placement_id", 0L).AsInt64() == p.Id)
				{
					result = p;
					goto IL_0051;
				}
			}
			result = null;
			goto IL_0051;
			IL_0051:
			return (SemanticPlacement)result;
		}
	}

	public string ActiveSemanticCueEventType => LiveSemanticPlacement?.Cue.EventType ?? "";

	public Rect2? ActiveSemanticCueBounds
	{
		get
		{
			SemanticPlacement p = LiveSemanticPlacement;
			if (p == null)
			{
				return null;
			}
			return ProjectCueBounds(p.Label, p.Label.GlobalPosition);
		}
	}

	public Rect2? ActiveSemanticCuePreferredBounds => LiveSemanticPlacement?.Preferred;

	public Vector2? ActiveSemanticCueTargetAnchor
	{
		get
		{
			SemanticPlacement p = LiveSemanticPlacement;
			Vector2? result;
			if (p == null || !_world.Camera.IsInsideTree())
			{
				result = null;
			}
			else
			{
				Camera3D camera = _world.Camera;
				BattleActor a = p.Actor;
				result = camera.UnprojectPosition((a != null && GodotObject.IsInstanceValid(a) && a.IsInsideTree()) ? a.Contact : p.Anchor);
			}
			return result;
		}
	}

	public bool ActiveSemanticCuePlacementFound => LiveSemanticPlacement?.Found ?? false;

	public CardPresentationProfiles CardProfiles { get; } = new CardPresentationProfiles();

	public int PresentedSemanticCueCount { get; private set; }

	public int RejectedSemanticCueCount { get; private set; }

	public SemanticEventCue? LastSemanticCue { get; private set; }

	public int ActiveCueCount => _cues?.ActiveCount ?? 0;

	public int PresentedBlockBoundaryCount { get; private set; }

	public bool ActorSourceDisplayRequested { get; private set; } = !Enumerable.Contains(OS.GetCmdlineUserArgs(), "--actor-color-baseline");

	public bool SourceCastLightEnabled { get; set; } = true;

	public float SourceCastLightStrength
	{
		get
		{
			if (!SourceCastLightEnabled || !RealLightsEnabled)
			{
				return 0f;
			}
			return _sourceCastLight.Strength(_clock);
		}
	}

	public int SourceCastLightCount => (SourceCastLightStrength > 0f) ? 1 : 0;

	public Vector3 SourceCastLightPosition => _sourceCastLight.Position;

	public int SourceCombatFloatCount => _sourceFloats.Count;

	public int SourceCombatFloatsCreated { get; private set; }

	public string LastSourceCombatFloatText { get; private set; } = "";

	public bool SourceShowcaseEnabled { get; set; } = true;

	public string SourceShowcaseProvenance => "reconstructed: archived DECKROGUE_IMPACT_SHOWCASE; installed user APK identity unknown";

	public float LastEnemySequenceStartedAt { get; private set; }

	public float LastEnemySequenceEndAt { get; private set; }

	private RunState? EventRun => _ordinaryCallback?.Action.Owner.Change.Current ?? _fenceCommand?.Change.Current ?? _latestRun;

	public int ActiveMotionCount => AllPresentationActors().Count((BattleActor a) => a.Motion.HasMotion || a.UiLunge.Active || a.Motion.IsPaused(_clock) || a.ExecutionScaleActive);

	public int ActivePoseCount => AllPresentationActors().Count((BattleActor a) => !a.Dead && a.HasActivePose);

	private bool TrackedActorsIdle
	{
		get
		{
			if (ActiveMotionCount == 0 && ActivePoseCount == 0)
			{
				return !_deaths.Any((KeyValuePair<BattleActor, DeathState> d) => _clock < d.Value.Started + d.Value.Delay + d.Value.Duration);
			}
			return false;
		}
	}

	public string ActivePhaseBanner
	{
		get
		{
			Control? phaseBand = _phaseBand;
			if (phaseBand == null || !phaseBand.Visible)
			{
				return "";
			}
			return _phaseText?.Text ?? "";
		}
	}

	public float ActivePhaseBannerAlpha
	{
		get
		{
			Control? phaseBand = _phaseBand;
			if (phaseBand == null || !phaseBand.Visible)
			{
				return 0f;
			}
			return _phaseBand.Modulate.A;
		}
	}

	public int EnemyExecutionCount { get; private set; }

	public int InjuryFlashCount { get; private set; }

	public Rect2 PhaseBannerBounds => _phaseBand?.GetGlobalRect() ?? default(Rect2);

	public event Action? PresentationCompleted;

	public event Action<int>? TargetSelected;

	public event Action<GameEvent>? EventPresented;

	public event Action<GameEvent>? NumericContactPresented;

	public SourceAuthoredHandStyle? GetAdmittedAuthoredHandStyle(SessionChange change, string uid)
	{
		if (change == null || change.IsReset || !SourceShowcaseEnabled || !(change.Command is RunCommand_Combat { Command: Command_PlayCard play }) || play.CardUid != uid || !_commands.Any((CommandOwner owner) => (object)owner.Change == change))
		{
			return null;
		}
		CardInstance card = change.Previous?.Combat?.Player.Hand.FirstOrDefault((CardInstance c) => c.Uid == uid);
		if (!(card == null))
		{
			SourceShowcaseProfile profile = SourceShowcaseProfiles.ForCard(card.Def.Id);
			if ((object)profile != null && profile.AuthoredAttack != null)
			{
				SourceAuthoredHandStyle proof = new SourceAuthoredHandStyle(change.Revision, uid, card.Def.Id, profile.AuthoredAttack);
				if (!proof.Matches(change.Revision, uid, card.Def.Id))
				{
					return null;
				}
				return proof;
			}
		}
		return null;
	}

	public SessionChange? GetEventChange(GameEvent e)
	{
		if (!_eventOwners.TryGetValue(e, out CommandOwner owner))
		{
			return null;
		}
		return owner.Change;
	}

	public bool IsOrdinaryPresentation(GameEvent e)
	{
		if (_eventOwners.TryGetValue(e, out CommandOwner owner))
		{
			return !owner.Fenced;
		}
		return false;
	}

	private bool LogicalEnemyAlive(int i)
	{
		GameState state = _latestRun?.Combat;
		if ((object)state != null && i >= 0 && i < state.Enemies.Count)
		{
			return state.Enemies[i].Hp > 0;
		}
		return false;
	}

	public static bool RequiresInputFence(SessionChange change)
	{
		if (change.Previous?.Phase == "combat")
		{
			GameState current = change.Current.Combat;
			if ((object)current != null)
			{
				if (!(change.Current.Phase != "combat") && !change.Events.Any((GameEvent e) => e is GameEvent_TurnEnded))
				{
					if (change.Previous.Combat?.Phase == "awaiting-reaction")
					{
						return current.EventLog.FirstOrDefault() is GameEvent_CombatStarted;
					}
					return false;
				}
				return true;
			}
		}
		return false;
	}

	private void RegisterCommand(SessionChange change, GameState? before)
	{
		CommandOwner owner = new CommandOwner
		{
			Change = change,
			Fenced = RequiresInputFence(change)
		};
		if ((object)before != null)
		{
			GameState after = change.Current.Combat;
			if ((object)after != null)
			{
				owner.Ledger = _ledger.Register(before, after);
				if (owner.Ledger != null)
				{
					for (int i = before.EventLog.Count; i < after.EventLog.Count; i++)
					{
						CombatPresentationLedger.Receipt receipt = _ledger.Find(i, after.EventLog[i]);
						if (receipt != null)
						{
							_receipts[after.EventLog[i]] = receipt;
						}
					}
				}
			}
		}
		_commands.Add(owner);
		foreach (GameEvent e in change.Events)
		{
			_eventOwners[e] = owner;
		}
		IsPresenting = true;
		if (owner.Fenced)
		{
			InputLocked = true;
			_session?.SetPresentationBusy(busy: true);
		}
		if (change.Events.Any((GameEvent gameEvent2) => gameEvent2 is GameEvent_TurnEnded) || change.Previous?.Combat?.Phase == "awaiting-reaction")
		{
			ActionPlayback action = new ActionPlayback
			{
				Owner = owner,
				EnemyPhase = true
			};
			owner.RemainingActions = 1;
			HashSet<string> keys = new HashSet<string>();
			string path = "res://assets/Audio/audio.json";
			if (FileAccess.FileExists(path))
			{
				foreach (JProperty key in ((JObject)JObject.Parse(FileAccess.GetFileAsString(path))["sfx"])?.Properties() ?? Enumerable.Empty<JProperty>())
				{
					keys.Add(key.Name);
				}
			}
			(float[], float) plan = SourceEnemySequence.Plan(change.Events, keys);
			action.NotBeforeFinish = plan.Item2;
			List<int> contacts = new List<int>();
			float contactSpanShift = 0f;
			for (int i2 = 0; i2 < change.Events.Count; i2++)
			{
				GameEvent e2 = change.Events[i2];
				PresentationBeat beat = _timeline.Describe(e2, Routes);
				float inner = ((e2 is GameEvent_DamageDealt { Source: "enemy" } hit && SourceEnemyRanged(before, hit.EnemyIndex ?? (-1))) ? 0.22f : 0f);
				float onset = plan.Item1[i2] + contactSpanShift;
				float contact = onset + inner;
				bool melee = e2 is GameEvent_DamageDealt { Source: "enemy" } && inner == 0f;
				action.Beats.Add(new OrdinaryBeat
				{
					Beat = beat,
					Action = action,
					Index = i2,
					ContactAt = contact,
					PreviewAt = melee ? Math.Max(0f, contact - EnemyMeleeLungeLead) : onset
				});
				// The original gap follows the final contact, not the first hit of an expanded attack.
				contactSpanShift += inner + (beat.Contacts?.Span ?? 0f);
				contacts.Add(i2);
			}
			action.NotBeforeFinish += contactSpanShift;
			_ordinaryActions.Add(action);
			action.Token = _actionQueue.Enqueue(_clock, contacts, (CombatActionQueue.ActionToken token) =>
			{
				LaunchOrdinary(action);
			});
			return;
		}
		List<int> starts = new List<int> { 0 };
		for (int i3 = 1; i3 < change.Events.Count; i3++)
		{
			GameEvent gameEvent = change.Events[i3];
			if ((gameEvent is GameEvent_CardPlayed || gameEvent is GameEvent_ReactionTriggered || gameEvent is GameEvent_GearUsed || gameEvent is GameEvent_TurnEnded) ? true : false)
			{
				starts.Add(i3);
			}
		}
        var continuation=FindPrimarySpellContinuation(change);
        if(continuation.Index>=0 && !starts.Contains(continuation.Index))
        {
            starts.Add(continuation.Index);
            starts.Sort();
        }
		owner.RemainingActions = starts.Count;
		for (int part = 0; part < starts.Count; part++)
		{
			int from = starts[part];
			int to = ((part + 1 < starts.Count) ? starts[part + 1] : change.Events.Count);
			ActionPlayback action2 = new ActionPlayback
			{
				Owner = owner
			};
            if(from==continuation.Index)
            {
                action2.PrimaryContinuationCard=continuation.Card;
                action2.PrimaryContinuationUid=continuation.Uid;
            }
			float delay = (change.Events.Skip(from).Take(to - from).Any((GameEvent gameEvent2) => gameEvent2 is GameEvent_DamageDealt gameEvent_DamageDealt && gameEvent_DamageDealt.Source == "player") ? 0.13f : 0f);
			List<int> contacts2 = new List<int>();
            GameEvent_CardPlayed? supportOwnerEvent=change.Events.Skip(from).Take(to-from).OfType<GameEvent_CardPlayed>().FirstOrDefault();
            string? supportOwnerCard=supportOwnerEvent?.CardId;
            var supportContext=SupportVfxProfiles.PrimaryOwner(change,supportOwnerEvent);
            bool supportClaimed=false;
			for (int i4 = from; i4 < to; i4++)
			{
				GameEvent e3 = change.Events[i4];
				PresentationBeat beat2 = _timeline.Describe(e3, Routes);
                beat2=SupportVfxProfiles.Describe(beat2,supportOwnerCard,ref supportClaimed,supportContext);
				bool flag = e3 is GameEvent_CardPlayed;
				float contact = (flag ? 0f : delay);
				action2.Beats.Add(new OrdinaryBeat
				{
					Beat = beat2,
					Action = action2,
					Index = i4,
					ContactAt = contact,
					PreviewAt = Math.Max(0f, contact - (beat2.Scenario?.Windup ?? 0f))
				});
				contacts2.Add(i4);
				if (!flag && HasOrdinaryPresentation(e3, beat2))
				{
					delay += ((e3 is GameEvent_ReactionTriggered) ? 0.45f : ((e3 is GameEvent_EnemyActionExecuting) ? 0.3f : ((e3 is GameEvent_EnemyInterrupted) ? 0.45f : 0.12f)));
				}
			}
			PlanActorAction(action2);
			if (change.Events.OfType<GameEvent_CombatEnded>().Any((GameEvent_CombatEnded gameEvent_CombatEnded) => gameEvent_CombatEnded.Result == "won"))
			{
				GameEvent final = change.Events.LastOrDefault((GameEvent gameEvent2) => gameEvent2 is GameEvent_DamageDealt gameEvent_DamageDealt && gameEvent_DamageDealt.HpLoss > 0);
				OrdinaryBeat last = action2.Beats.LastOrDefault((OrdinaryBeat b) => (object)b.Beat.Event == final);
				if (last != null)
				{
					last.Beat = last.Beat with
					{
						WinningHit = true,
						Duration = last.Beat.Duration + 0.16f
					};
				}
			}
			_ordinaryActions.Add(action2);
			action2.Token = _actionQueue.Enqueue(_clock, contacts2, (CombatActionQueue.ActionToken token) =>
			{
				LaunchOrdinary(action2);
			});
		}
		_session?.SetPresentationBusy(InputLocked);
	}

    private (int Index,GameEvent_CardPlayed? Card,string? Uid) FindPrimarySpellContinuation(SessionChange change)
    {
        // Bounded pre-onPlay, one-cascade SPELL return. No generic mid-combo or
        // weapon resume and no ownership inference from a repeated definition.
        if(!Hd2dAttackPrototype.Enabled || !SourceShowcaseEnabled
            || change.Command is not RunCommand_Combat {Command:Command_PlayCard play}
            || string.IsNullOrEmpty(play.CardUid))return (-1,null,null);
        var primary=change.Previous?.Combat?.Player.Hand.FirstOrDefault(c=>c.Uid==play.CardUid);
        if(primary?.Def.Type!="spell")return (-1,null,null);
        var family=ElementalVfxProfiles.ForCard(primary.Def.Id)?.Family;
        if(family is not (ElementalVfxFamily.IceScatter or ElementalVfxFamily.LightScatter or ElementalVfxFamily.WaterWide))return (-1,null,null);
        var played=change.Events.Select((e,i)=>(Event:e,Index:i)).Where(x=>x.Event is GameEvent_CardPlayed).ToArray();
        if(played.Length!=2 || played[0].Event is not GameEvent_CardPlayed card || card.CardId!=primary.Def.Id
            || played[1].Event is not GameEvent_CardPlayed nested)return (-1,null,null);
        if(!change.Events.Skip(played[0].Index+1).Take(played[1].Index-played[0].Index-1)
            .OfType<GameEvent_Cascaded>().Any(c=>c.CardId==primary.Def.Id && c.HitId==nested.CardId))return (-1,null,null);
        int first=-1;
        for(int i=0;i<change.Events.Count;i++)
            if(change.Events[i] is GameEvent_DamageDealt {Source:"player",PresentationSource:{Kind:"card"} origin}
                && origin.Uid==play.CardUid && origin.DefinitionId==primary.Def.Id){first=i;break;}
        // Including blocked/zero first receipts is intentional; no positive-only
        // boundary can skip or steal an earlier primary outcome.
        if(first<=played[1].Index)return (-1,null,null);
        bool nestedWitness=change.Events.Skip(played[1].Index+1).Take(first-played[1].Index-1)
            .OfType<GameEvent_DamageDealt>().Any(h=>h.Source=="player" && h.PresentationSource is {Kind:"card"} n
                && !string.IsNullOrEmpty(n.Uid) && n.Uid!=play.CardUid && n.DefinitionId==nested.CardId);
        if(!nestedWitness)return (-1,null,null);
        // Resolve source semantics for eligibility, not the outcome renderer.
        // The original zero/blocked beat remains SystemOutcomeFx unchanged.
        var route=Routes.Resolve((GameEvent_DamageDealt)change.Events[first]);
        if(!Hd2dAttackPrototype.SupportsRoute(route) || route?.SourceKind!="card" || route.DefinitionId!=primary.Def.Id
            || ElementalVfxProfiles.ForRoute(route.Key)?.Family!=family)return (-1,null,null);
        return (first,card,play.CardUid);
    }

	private static bool HasOrdinaryPresentation(GameEvent e, PresentationBeat b)
	{
		bool flag = (object)b.Route != null || CombatPresentationLedger.IsNumeric(e);
		if (!flag)
		{
			bool flag2;
			switch (e.Type)
			{
			case "ImpulseKept":
			case "LightGained":
			case "TurnStarted":
			case "BurnApplied":
			case "GrowthAdded":
			case "BlazeExtinguished":
			case "CascadeRetargeted":
			case "ReactionTriggered":
			case "ActionNegated":
			case "EnemyWeakened":
			case "MomentumAdded":
			case "LightDischarged":
			case "StatusInflicted":
			case "ArtifactBlocked":
			case "ImpulseReturned":
			case "ExposedApplied":
			case "ImpulseKindled":
			case "StrengthGained":
			case "TurnEnded":
			case "EnemyIntentDeclared":
			case "EnemyActionExecuting":
			case "EnemyInterrupted":
			case "LightSpent":
			case "PlayerArtifactBlocked":
			case "Cascaded":
			case "BlazeIgnited":
				flag2 = true;
				break;
			default:
				flag2 = false;
				break;
			}
			flag = flag2;
		}
		return flag;
	}

	private void LaunchOrdinary(ActionPlayback action)
	{
		_session?.RecordFreezeEvent("BattleStage.LaunchOrdinary");
		action.Launched = true;
		action.LaunchedAt = _clock;
		_actorAction = action;
		ActorActionLaunchCount++;
		LastActorLaunchAt = _clock;
		LastActorLaunchFrame = _presentationFrame;
		_lastHandoffPause = 0f;
		action.NotBeforeFinish += _clock;
		if (action.EnemyPhase)
		{
			_blockBoundaryCommand = action.Owner.Change.Command is RunCommand_Combat;
			_enemyBlockExpiryPending = false;
			LastEnemySequenceStartedAt = _clock;
			LastEnemySequenceEndAt = action.NotBeforeFinish;
		}
		foreach (OrdinaryBeat beat in action.Beats)
		{
			beat.ContactAt += _clock;
			beat.PlannedContactAt = beat.ContactAt;
			beat.PreviewAt += _clock;
		}
        if(action.PrimaryContinuationCard!=null && !action.PrimaryContinuationSourceStarted)
        {
            action.PrimaryContinuationSourceStarted=true;
            BeginSourceCard(action);
        }
	}

	private EffectPlayback AcquireEffect()
	{
		int num;
		if (!_effectPool.TryPop(out EffectPlayback slot))
		{
			EffectPlayback effectPlayback = new EffectPlayback();
			AuthoredGeometryFx authoredGeometryFx = new AuthoredGeometryFx();
			num = ++_effectPoolSerial;
			authoredGeometryFx.Name = "OrdinaryAuthored" + num;
			effectPlayback.Native = authoredGeometryFx;
			effectPlayback.Fallback = new ContinuousAttackFx
			{
				Name = "OrdinaryFallback" + _effectPoolSerial
			};
			slot = effectPlayback;
			AddChild(slot.Native, forceReadableName: false, InternalMode.Disabled);
			AddChild(slot.Fallback, forceReadableName: false, InternalMode.Disabled);
			slot.Fallback.Clear();
		}
		AuthoredGeometryFx native = slot.Native;
		num = (slot.Fallback.QualityLevel = _quality.Level);
		native.QualityLevel = num;
		AuthoredGeometryFx native2 = slot.Native;
		bool realLightsEnabled = (slot.Fallback.RealLightsEnabled = RealLightsEnabled);
		native2.RealLightsEnabled = realLightsEnabled;
		return slot;
	}

	private void RetireEffect(OrdinaryBeat b)
	{
		EffectPlayback effect = b.Effect;
		if (effect != null)
		{
			effect.Native.Clear();
			effect.Fallback.Clear();
			b.Effect = null;
			_effectPool.Push(effect);
		}
	}

	private bool ReceiptReadyForPreview(OrdinaryBeat b)
	{
		if (!_receipts.TryGetValue(b.Beat.Event, out var r))
		{
			return true;
		}
		if (r.Reason == null)
		{
            if(_ledger.Ready(r))return true;
            if(CanPreviewIndependentScatterOwner(b,r))
            {
                b.IndependentScatterPreview=true;
                return true;
            }
            return false;
		}
		return true;
	}

    private static bool UsesOrderedSharedLaunchMaterial(AttackRoute? route)
    {
        if(!Hd2dAttackPrototype.SupportsRoute(route))return false;
        var profile=ElementalVfxProfiles.ForRoute(route?.Key);
        if(profile?.Family is (ElementalVfxFamily.IceScatter or ElementalVfxFamily.LightScatter or ElementalVfxFamily.WaterWide))return true;
        // Explicit Feast only, not a generic Dark-source or nested-drain opt-in.
        return route!=null && profile?.CardId=="black_feast" && profile.Family==ElementalVfxFamily.Dark
            && route.Key=="card:black_feast" && route.DefinitionId=="black_feast"
            && route.SourceKind=="card" && route.Family=="dark_drain"
            && route.Renderer=="DarkDrainFx" && route.Element=="dark" && !route.FixedSmall;
    }

    // Preview-only reservation. Numeric contact still uses the unchanged ordered
    // ledger commit and display-boundary validation below. No synthetic receipt.
    // Receipt.Exact is a post-commit result, not a pre-preview eligibility field;
    // ValidReceipt below proves structure and current target Before vitals.
    private bool CanPreviewIndependentScatterOwner(OrdinaryBeat b,CombatPresentationLedger.Receipt receipt)
    {
        if(!Hd2dAttackPrototype.Enabled || !b.Direct || b.Action.EnemyPhase || b.Contacted
            || _clock>=b.ContactAt || !_ledger.Current(receipt)
            || receipt.Reason!=null || receipt.Target<0 || !Hd2dAttackPrototype.SupportsRoute(b.Beat.Route))return false;
        var profile=ElementalVfxProfiles.ForRoute(b.Beat.Route?.Key);
        if(!UsesOrderedSharedLaunchMaterial(b.Beat.Route))return false;
        if(b.Beat.Event is not GameEvent_DamageDealt {Source:"player",HpLoss:>0} hit
            || hit.PresentationSource is not {Kind:"card"} source || string.IsNullOrEmpty(source.Uid)
            || b.Action.Card?.CardId!=source.DefinitionId || profile?.CardId!=source.DefinitionId
            || !_eventOwners.TryGetValue(hit,out var owner) || !ReferenceEquals(owner,b.Action.Owner)
            || owner.Change.Command is not RunCommand_Combat {Command:Command_PlayCard play}
            || play.CardUid!=source.Uid
            || owner.Change.Previous?.Combat?.Player.Hand.Any(c=>c.Uid==play.CardUid && c.Def.Id==source.DefinitionId)!=true
            || !ValidReceipt(hit,receipt.Values,_displayState))return false;
        var first=b.Action.Beats.FirstOrDefault(x=>x.Direct && x.Volley==b.Volley && Hd2dAttackPrototype.SupportsRoute(x.Beat.Route));
        if(!ReferenceEquals(first,b))return false;
        // Even unchanged vitals cannot authorize jumping over an earlier same-
        // target numeric/nested receipt. Current is generation/instance checked.
        if(_receipts.Values.Any(p=>p.Index<receipt.Index && p.Target==receipt.Target && _ledger.Current(p)))return false;
        return true;
    }

	private void ProcessOrdinaryContacts(float delta)
	{
		int generation = _actionQueue.Generation;
		ActionPlayback[] array = _ordinaryActions.ToArray();
		foreach (ActionPlayback action in array)
		{
			if (!action.Launched || action.Finished)
			{
				continue;
			}
			foreach (OrdinaryBeat b in action.Beats)
			{
				if (!b.Started && _clock + 1E-06f >= b.PreviewAt && ReceiptReadyForPreview(b))
				{
					b.Started = true;
					AnchorSourceEnemyInnerContact(b);
					b.StartedAt = b.PreviewAt;
					if ((object)b.Beat.Route != null)
					{
						b.Effect = AcquireEffect();
					}
					WithOrdinaryBeat(b, () =>
					{
						BeginBeat(b.Beat);
						if (action.EnemyPhase)
						{
							b.StartedAt = b.ContactAt - (b.Beat.Scenario?.Windup ?? 0f);
						}
					});
				}
				if (b.InnerDelay.Pending)
				{
					if (!b.InnerDelay.Sample(delta, _presentationFrame))
					{
						continue;
					}
					b.ContactAt = _clock;
				}
				float due = b.ContactAt + (float)(b.ContactOrdinal + 1) * 0.12f + b.ContactRetimed;
				if (b.Contacted || _clock + 1E-06f < due)
				{
					continue;
				}
				if (((object)b.Beat.Route != null && _lastOrdinaryVisualFrame == _presentationFrame && (!b.Direct || (object)action.Profile == null || !(Math.Abs(_lastVolleyAt - b.ContactAt) < 1E-05f))) || action.Beats.Any((OrdinaryBeat p) => p.Index < b.Index && !p.Contacted))
				{
					break;
				}
				if (!b.Started)
				{
					b.Started = true;
					AnchorSourceEnemyInnerContact(b);
					b.StartedAt = b.PreviewAt;
					if ((object)b.Beat.Route != null)
					{
						b.Effect = AcquireEffect();
					}
					WithOrdinaryBeat(b, () =>
					{
						BeginBeat(b.Beat);
						if (action.EnemyPhase)
						{
							b.StartedAt = b.ContactAt - (b.Beat.Scenario?.Windup ?? 0f);
						}
					});
				}
				if (b.InnerDelay.Pending || _clock + 1E-06f < b.ContactAt)
				{
					continue;
				}
				CombatPresentationReceipt receipt = CombatPresentationLedger.Read(b.Beat.Event);
				if (b.ContactOrdinal < 0 && !b.Rejected && (object)receipt != null && !ValidReceipt(b.Beat.Event, receipt, _displayState))
				{
					b.Rejected = true;
					RejectedReceiptCount++;
					if (_receipts.TryGetValue(b.Beat.Event, out var owned))
					{
						owned.Reason = "numeric_display_boundary_mismatch";
					}
					RetireEffect(b);
				}
				if (_clock - due > 0.1f)
				{
					EffectScenario scenario = b.Beat.Scenario;
					if ((object)scenario != null)
					{
						b.StartedAt = _clock - scenario.Windup;
					}
				}
				EnemyAttackContactPlan? contacts = b.Beat.Contacts;
				if (contacts != null && contacts.Individual && _clock - due > 0.1f)
				{
					b.ContactRetimed += _clock - due;
				}
				b.ContactOrdinal++;
				b.LastContact = _clock;
				OrdinaryBeat ordinaryBeat = b;
				EnemyAttackContactPlan? contacts2 = b.Beat.Contacts;
				ordinaryBeat.Contacted = contacts2 == null || !contacts2.Individual || b.ContactOrdinal >= b.Beat.Contacts.Count - 1;
				if ((object)b.Beat.Route != null)
				{
					_lastOrdinaryVisualFrame = _presentationFrame;
					_lastVolleyAt = b.ContactAt;
				}
				// Shift future cues before this callback schedules the next enemy's motion.
				if (b.Contacted) PreserveEnemySequenceSpacing(action, b);
				WithOrdinaryBeat(b, () =>
				{
					if (b.Beat.Event is GameEvent_CardPlayed card)
					{
						PresentOrdinaryCard(action, card);
					}
					ContactBeat(b.Beat);
				});
				if (generation != _actionQueue.Generation)
				{
					return;
				}
				if (b.Contacted)
				{
					FinishEnemyContactLunge(b);
					_ordinaryCompletedEvents++;
				}
				if (b.Contacted && _actionQueue.ContactComplete(action.Token, b.Index, _clock, _presentationFrame, action.Actor?.Motion.PauseRemaining(_clock) ?? 0f))
				{
					LastActorContactAt = _clock;
					LastActorContactRevision = action.Owner.Change.Revision;
					LastActorContactFrame = _presentationFrame;
				}
			}
			CompleteOrdinaryActionIfDone(action);
		}
	}

	private void FinishEnemyContactLunge(OrdinaryBeat beat)
	{
		BattleActor actor = beat.EnemyLungeActor;
		if (actor == null || !GodotObject.IsInstanceValid(actor) || actor.UiLunge.Version != beat.EnemyLungeVersion) return;
		if (beat.Rejected || LastEventReceiptRejected) actor.UiLunge.Cancel();
		else actor.UiLunge.ReleaseContactHold(_clock, _presentationFrame);
	}

	private void PreserveEnemySequenceSpacing(ActionPlayback action, OrdinaryBeat completed)
	{
		if (!action.EnemyPhase) return;
		float expectedLastContact = completed.PlannedContactAt + (completed.Beat.Contacts?.Span ?? 0f);
		float delayed = Math.Max(0f, _clock - expectedLastContact);
		if (delayed <= 1E-06f) return;
		foreach (OrdinaryBeat later in action.Beats)
		{
			if (later.Index <= completed.Index || later.Contacted) continue;
			later.ContactAt += delayed;
			later.PlannedContactAt += delayed;
			later.PreviewAt += delayed;
		}
		action.NotBeforeFinish += delayed;
		LastEnemySequenceEndAt = action.NotBeforeFinish;
	}

	private void AnchorSourceEnemyInnerContact(OrdinaryBeat beat)
	{
		if (beat.Action.EnemyPhase && beat.Beat.Event is GameEvent_DamageDealt { Source: "enemy" } hit && SourceEnemyRanged(beat.Action.Owner.Change.Previous?.Combat, hit.EnemyIndex ?? (-1)))
		{
			beat.ContactAt = _clock + 0.22f;
			beat.InnerDelay.Begin(0.22f, _presentationFrame);
		}
	}

	private void PresentOrdinaryCard(ActionPlayback action, GameEvent_CardPlayed card)
	{
		BeginSourceCard(action);
	}

	private void CompleteOrdinaryActionIfDone(ActionPlayback action)
	{
		if (action.Finished || _clock < action.NotBeforeFinish || action.Beats.Any((OrdinaryBeat b) => !b.Contacted || b.Effect != null))
		{
			return;
		}
		action.Finished = true;
		if (--action.Owner.RemainingActions == 0)
		{
			action.Owner.Finished = true;
			if (action.Owner.Ledger != null)
			{
				_ledger.Finish(action.Owner.Ledger);
			}
		}
	}

    internal static bool IsTorrentReleaseHoldRoute(AttackRoute? route)
        => Hd2dAttackPrototype.Enabled && route is not null &&
           route.Key=="card:white_light_torrent" && route.DefinitionId=="white_light_torrent" &&
           route.SourceKind=="card" && route.Family=="light" && route.Renderer=="LightRayFx" &&
           route.Element=="light" && !route.FixedSmall;

    private void RefreshTorrentReleaseHolds()
    {
        foreach(BattleActor actor in AllPresentationActors()) actor.ClearTorrentReleaseHold();
        foreach(ActionPlayback action in _ordinaryActions)
        {
            if(!action.Launched || action.Finished || action.Actor is not BattleActor actor
                || action.Capture is not SourceActorMotion.OwnershipToken owner || !owner.IsCurrent) continue;
            float until=float.NegativeInfinity;
            foreach(OrdinaryBeat beat in action.Beats)
                if(beat.Direct && !beat.Rejected && beat.Beat.Event is GameEvent_DamageDealt
                    && IsTorrentReleaseHoldRoute(beat.Beat.Route))
                    // Actual quantized contact can be later than its scheduled time.
                    // Future/retimed contacts and omitted/rejected beats remain the queue's authority.
                    until=Math.Max(until,Math.Max(beat.ContactAt,beat.Contacted?beat.LastContact:beat.ContactAt)+.10f);
            actor.SetTorrentReleaseHold(until,owner);
        }
    }

	private void AdvanceOrdinaryEffects()
	{
		ActionPlayback[] array = _ordinaryActions.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			foreach (OrdinaryBeat b in array[i].Beats)
			{
				EffectPlayback slot = b.Effect;
				if (slot == null)
				{
					continue;
				}
				float elapsed = Math.Max(0f, _clock - b.StartedAt);
                float effectTime=elapsed-b.EffectStartedAt+b.EffectOffset;
                // Any admitted shared-launch material can wait for older receipts,
                // including legacy nested/non-direct action grouping. Receiving
                // impact/aftermath never precedes its actual numeric contact.
                if(!b.Contacted && UsesOrderedSharedLaunchMaterial(b.Beat.Route))
                    effectTime=Math.Min(effectTime,(b.Beat.Scenario?.Windup??0f)-.0001f);
                // Only these exact proved installed material tuples: no receiving core
                // before its own numeric contact, even during interleaved triggers.
                if(!b.Contacted && PermanentContactVfxProfiles.OriginalRoute(b.Beat.Route)
                    && b.Beat.Event is GameEvent_DamageDealt permanentHit && IsOwnedInstalledPermanentContact(permanentHit))
                    effectTime=Math.Min(effectTime,(b.Beat.Scenario?.Windup??0f)-.0001f);
                // Exact proved Blunt and selected neutral receiving contacts are contact-only. Legacy
                // enemy scenarios can have zero Windup and a preview Sample(0);
                // suppress that preview core without changing enemy/numeric timing.
                if(!b.Contacted && (IsOwnedEnemyBluntContact(b) || IsOwnedEnemyNeutralContact(b)))
                    effectTime=Math.Min(effectTime,(b.Beat.Scenario?.Windup??0f)-.0001f);
                // New local reaction material is never an anticipatory wound.
                // Existing counter pose, numeric order and legacy routes are untouched.
                if(!b.Contacted && ReactionContactVfxProfiles.OriginalRoute(b.Beat.Route)
                    && b.Beat.Event is GameEvent_DamageDealt reactionHit && IsOwnedConfirmedReactionContact(reactionHit))
                    effectTime=Math.Min(effectTime,(b.Beat.Scenario?.Windup??0f)-.0001f);
				if (b.Rejected)
				{
					RetireEffect(b);
					continue;
				}
				if (slot.Native.IsActive)
				{
                    if(b.Beat.Route?.Renderer=="Hd2dSupportFx" && _hero!=null)
                        slot.Native.UpdateSupportReceiver(_hero.SourceMotionFeet,_hero.Height);
                    if(Hd2dAttackPrototype.UsesWeaponSource(b.Beat.Route) && b.Action.Actor!=null)
                        slot.Native.UpdatePrototypeSource(b.Action.Actor.PrototypeStrikeSourcePoint, b.Action.Actor.PoseFrame, b.Action.Actor.PrototypeStrikeWindupPoint, b.Action.Actor.Height*.30f, elapsed - b.EffectStartedAt + b.EffectOffset, b.Action.PrototypeNearSurface);
                    else if(Hd2dAttackPrototype.UsesCastSource(b.Beat.Route) && b.Action.Actor!=null)
                        slot.Native.UpdatePrototypeSource(b.Action.Actor.PrototypeFireSourcePoint,b.Action.Actor.PoseFrame);
					slot.Native.Sample(effectTime);
				}
				else if (slot.Fallback.Visible)
				{
					slot.Fallback.Sample(effectTime);
				}
				if (b.Contacted && elapsed >= b.Beat.Duration)
				{
					RetireEffect(b);
				}
			}
		}
		foreach (ActionPlayback action in _ordinaryActions)
		{
			CompleteOrdinaryActionIfDone(action);
		}
	}

	private void WithOrdinaryBeat(OrdinaryBeat b, Action callback)
	{
		int callbackGeneration = _actionQueue.Generation;
		AuthoredGeometryFx native = _native;
		ContinuousAttackFx fx = _fx;
		BattleActor source = _sourceActor;
		BattleActor target = _targetActor;
		EnemyAttackContactPlan.SourceProof proof = _sourceProof;
		bool rejected = _rejectBeat;
		bool projection = _projectionActive;
		int hp = _contactHp;
		int block = _contactBlock;
		float start = _effectStartedAt;
		float offset = _effectOffset;
		OrdinaryBeat previous = _ordinaryCallback;
		_ordinaryCallback = b;
		_sourceActor = b.Source;
		_targetActor = b.Target;
		_sourceProof = b.SourceProof;
		_rejectBeat = b.Rejected;
		_projectionActive = b.Projection;
		_contactHp = b.Hp;
		_contactBlock = b.Block;
		_effectStartedAt = b.EffectStartedAt;
		_effectOffset = b.EffectOffset;
		b.Time = Math.Max(0f, _clock - b.StartedAt);
		EffectPlayback effect = b.Effect;
		if (effect != null)
		{
			_native = effect.Native;
			_fx = effect.Fallback;
		}
		try
		{
			callback();
		}
		finally
		{
			b.Source = _sourceActor;
			b.Target = _targetActor;
			b.SourceProof = _sourceProof;
			b.Rejected = _rejectBeat;
			b.Projection = _projectionActive;
			b.Hp = _contactHp;
			b.Block = _contactBlock;
			b.EffectStartedAt = _effectStartedAt;
			b.EffectOffset = _effectOffset;
			_native = native;
			_fx = fx;
			_sourceActor = source;
			_targetActor = target;
			_sourceProof = proof;
			_rejectBeat = rejected;
			_projectionActive = projection;
			_contactHp = hp;
			_contactBlock = block;
			_effectStartedAt = start;
			_effectOffset = offset;
			_ordinaryCallback = previous;
			if (callbackGeneration != _actionQueue.Generation)
			{
				_sourceActor = null;
				_targetActor = null;
				_ordinaryCallback = null;
				_projectionActive = false;
				_rejectBeat = false;
			}
		}
	}

	private bool FaultVisualStep(Action step)
	{
		try { step(); return true; }
		catch (Exception ex)
		{
			try { _session?.RecordFreezeEvent("BattleStage.FaultCleanup", false, ex); } catch { }
			return false;
		}
	}

	private void RecoverPresentationFault(Exception error, string boundary, int generation)
	{
		try { RecoverPresentationFaultCore(error, boundary, generation); }
		catch (Exception recoveryError)
		{
			PresentationFaulted = true;
			PresentationFaultBlocked = true;
			InputLocked = true;
			try { _session?.SetPresentationBusy(true); } catch { }
			try { _session?.RecordFreezeEvent("BattleStage.FaultRecovery", false, recoveryError); } catch { }
			try
			{
				var run = _session?.CurrentRun ?? _latestRun;
				if (run != null) FaultSnapshotSynchronizer?.Invoke(run, _session?.Revision ?? _lastRevision);
			}
			catch { /* Stay blocked; never repeat a failed recovery every frame. */ }
		}
		finally { _faultRecovering = false; }
	}

	private void RecoverPresentationFaultCore(Exception error, string boundary, int generation)
	{
		// A callback may have loaded a different run. Never cancel its new owner.
		if (generation != _actionQueue.Generation || _faultRecovering || PresentationFaulted) return;
		try { _session?.RecordFreezeEvent(boundary, false, error); } catch { }
		PresentationFaulted = true;
		PresentationFaultBlocked = true;
		PresentationFaultCount++;
		_faultRecovering = true;
		InputLocked = true;
		_session?.SetPresentationBusy(true);
		RunState? run = _session?.CurrentRun ?? _latestRun;
		long revision = _session?.Revision ?? _lastRevision;
		// These are pure presentation ownership structures. The committed run and
		// its event log are never replayed, mutated, or written into diagnostics.
		CancelSupportPlayback();
		_actionQueue.Cancel();
		_timeline.Reset();
		var effects = _ordinaryActions.SelectMany(a => a.Beats).Select(b => b.Effect).Where(e => e != null).Distinct().ToArray();
		_ordinaryActions.Clear(); _commands.Clear(); _eventOwners.Clear(); _receipts.Clear();
		_ledger.Clear(); _deaths.Clear();
		_actorAction = null; _fenceCommand = null; _ordinaryCallback = null;
		_sourceActor = null; _targetActor = null; _incomingEvent = null; _incomingAmount = null;
		_finishSlowRemaining = 0f; _projectionActive = false; _rejectBeat = false;
		_enemyBlockExpiryPending = false; _blockBoundaryCommand = false;
		_session?.ClearPresentation();
		IsPresenting = false;
		bool clean = true;
		foreach (var effect in effects)
		{
			clean &= FaultVisualStep(() => effect!.Native.Clear());
			clean &= FaultVisualStep(() => effect!.Fallback.Clear());
		}
		clean &= FaultVisualStep(() => _fx?.Clear());
		clean &= FaultVisualStep(() => _native?.Clear());
		clean &= FaultVisualStep(() => _cues?.Clear());
		clean &= FaultVisualStep(CancelPhaseLayer);
		clean &= FaultVisualStep(CancelSourceCombatText);
		clean &= FaultVisualStep(() => _audio?.StopEffects());
		clean &= FaultVisualStep(() => _world?.SetForestAttackLights(null));
		clean &= FaultVisualStep(() => { if (_receipt != null) _receipt.Visible = false; });
		clean &= FaultVisualStep(() =>
		{
			foreach (var actor in AllPresentationActors()) actor.AbandonPresentationForFault(_clock);
		});
		_faultRecovering = false;
		if (run != null) ApplyFaultSnapshot(run, revision, clean);
	}

	private void ApplyFaultSnapshot(RunState run, long revision, bool clean = true)
	{
		if (_faultRecovering) return;
		_session?.ClearPresentation();
		_faultRecovering = true;
		InputLocked = true;
		_session?.SetPresentationBusy(true);
		_latestRun = run;
		_displayState = run.Combat;
		bool visual = clean;
		try
		{
			// One bounded attempt. A persistently broken renderer is never retried
			// each frame. Failure keeps gameplay blocked and uses the independent UI.
			if (visual)
			{
				SetRunState(run);
				foreach (var actor in AllPresentationActors())
				{
					actor.AbandonPresentationForFault(_clock);
					actor.CancelSourceMotion(_clock);
					actor.UpdateVisual(_clock, -1f, -1f, false, ++_presentationFrame, 0f);
				}
			}
		}
		catch (Exception ex)
		{
			visual = false;
			try { _session?.RecordFreezeEvent("BattleStage.FaultSnapshot", false, ex); } catch { }
		}
		PresentationFaultBlocked = !visual;
		bool ui = false;
		try { ui = FaultSnapshotSynchronizer?.Invoke(run, revision) == true; }
		catch (Exception ex) { try { _session?.RecordFreezeEvent("BattleStage.FaultUi", false, ex); } catch { } }
		_faultRecovering = false;
		if (_session != null && (_session.Revision != revision || !ReferenceEquals(_session.CurrentRun, run)))
		{
			// An owner change during UI synchronization is never acknowledged as this snapshot.
			PresentationFaultBlocked = true;
			InputLocked = true;
			_session.SetPresentationBusy(true);
			_latestRun = _session.CurrentRun;
			_displayState = _latestRun?.Combat;
			// Notify the independent error UI for the new owner; never acknowledge
			// the old board and never rerun its scene synchronization.
			try { if (_latestRun != null) FaultSnapshotSynchronizer?.Invoke(_latestRun, _session.Revision); } catch { }
			_session.SetPresentationBusy(true);
			return;
		}
		PresentationFaultBlocked = !visual || !ui;
		InputLocked = PresentationFaultBlocked;
		_session?.SetPresentationBusy(PresentationFaultBlocked);
		try { _session?.RecordFreezeEvent(PresentationFaultBlocked ? "BattleStage.FaultBlocked" : "BattleStage.FaultStaticReady", !PresentationFaultBlocked); } catch { }
	}

	private void AppendAttackLights(List<ActorMaterialBinding.PointLight> lights, AuthoredGeometryFx? probe, bool forestReceiver = false)
	{
		if (probe != null)
		{
			probe.AppendSourceLights(lights, forestReceiver);
			return;
		}
		_native.AppendSourceLights(lights, forestReceiver);
		foreach (OrdinaryBeat item in _ordinaryActions.SelectMany((ActionPlayback a) => a.Beats))
		{
			EffectPlayback effect = item.Effect;
			if (effect != null && lights.Count < 8)
			{
				effect.Native.AppendSourceLights(lights, forestReceiver);
			}
		}
		AppendSourceCastLight(lights);
		if (lights.Count > 8)
		{
			lights.RemoveRange(8, lights.Count - 8);
		}
	}

	private void TryFinishPresentations()
	{
		if (_resetting || !IsPresenting || _timeline.Busy || _actionQueue.Busy || _commands.Any((CommandOwner c) => !c.Finished) || !TrackedActorsIdle)
		{
			return;
		}
		_ordinaryActions.Clear();
		_commands.Clear();
		_actorAction = null;
		RunState run = _latestRun;
		if ((object)run != null)
		{
			bool flag;
			switch (run.Phase)
			{
			case "reward":
			case "relic-reward":
			case "won":
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			if (flag)
			{
				_audio.PlayKey("ui.win");
				_cues.Clear();
			}
			SetRunState(run);
		}
		IsPresenting = false;
		InputLocked = false;
		_session?.SetPresentationBusy(busy: false);
		_receipts.Clear();
		_eventOwners.Clear();
		_session?.RecordFreezeEvent("BattleStage.PresentationCompleted");
		PresentationCompleted?.Invoke();
	}

	private void CancelCommandQueue()
	{
		ClearSourceCastLight();
		CancelSupportPlayback();
		_actionQueue.Cancel();
		_finishSlowRemaining = 0f;
		foreach (ActionPlayback ordinaryAction in _ordinaryActions)
		{
			foreach (OrdinaryBeat beat in ordinaryAction.Beats)
			{
				RetireEffect(beat);
			}
		}
		_ordinaryActions.Clear();
		_commands.Clear();
		_eventOwners.Clear();
		_actorAction = null;
		_fenceCommand = null;
		_ordinaryCallback = null;
	}

	public string GetEnemyId(int index)
	{
		return Enemy(index)?.DefinitionId ?? "";
	}

	public CombatPresentationVitals? GetPresentedVitals(GameEvent e)
	{
		if ((object)e != _lastPresentedEvent)
		{
			return null;
		}
		return _lastPresentedVitals;
	}

	public int GetEnemyHp(int index)
	{
		return Enemy(index)?.DisplayedHp ?? 0;
	}

	public int GetEnemyBlock(int index)
	{
		return Enemy(index)?.DisplayedBlock ?? 0;
	}

	public string GetEnemyIntent(int index)
	{
		return Enemy(index)?.IntentLabel.Text ?? "";
	}

	public void PlayUiSound(string sourceKey)
	{
		EnsureBuilt();
		_audio.PlayUiKey(sourceKey);
	}

	public void ApplyQuality(string profile)
	{
		EnsureBuilt();
		bool flag;
		switch (profile)
		{
		case "high":
		case "standard":
		case "low":
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		QualityProfile = (flag ? profile : "auto");
		if (!_qualityApplied)
		{
			ConfigFile cfg = new ConfigFile();
			if (cfg.Load("user://presentation.cfg") == Error.Ok)
			{
				_quality.RestoreAuto((int)cfg.GetValue("graphics", "auto_level", 0));
			}
		}
		_quality.Select(QualityProfile, _qualityApplied);
		_quality.Sample(0f, inBattle: false, IsPhoneQuality);
		_qualityApplied = true;
		ApplyQualitySettings();
		ConfigFile configFile = new ConfigFile();
		configFile.SetValue("graphics", "auto_level", _quality.AutoLevel);
		configFile.Save("user://presentation.cfg");
	}

	private void ApplyQualitySettings()
	{
		bool high = _quality.Level == 0;
		_native.QualityLevel = _quality.Level;
		_fx.QualityLevel = _quality.Level;
		RealLightsEnabled = true;
		_world.Lighting.GlowEnabled = high;
		GetViewport().Msaa3D = (Viewport.Msaa)(high ? 1 : 0);
	}

	public void SampleQuality(float unscaledDelta, bool inBattle)
	{
		if (_quality.Sample(unscaledDelta, inBattle, IsPhoneQuality))
		{
			ApplyQualitySettings();
			ConfigFile configFile = new ConfigFile();
			configFile.SetValue("graphics", "auto_level", _quality.AutoLevel);
			configFile.Save("user://presentation.cfg");
		}
	}

	public override void _Ready()
	{
		EnsureBuilt();
	}

	private void EnsureBuilt()
	{
		if (!_initialized)
		{
			_initialized = true;
			_world = new StageWorld
			{
				Name = "StageWorld"
			};
			AddChild(_world, forceReadableName: false, InternalMode.Disabled);
			_world.Build(1);
			_act = 1;
			CacheWorldLights();
			_actorRoot = new Node3D
			{
				Name = "Combatants"
			};
			AddChild(_actorRoot, forceReadableName: false, InternalMode.Disabled);
			_retiredRoot = new Node3D
			{
				Name = "RetiredCombatants"
			};
			AddChild(_retiredRoot, forceReadableName: false, InternalMode.Disabled);
			_world.CameraLayoutChanged += RefreshActorLayout;
			_fx = new ContinuousAttackFx
			{
				Name = "ContinuousAttack"
			};
			AddChild(_fx, forceReadableName: false, InternalMode.Disabled);
			_fx.Clear();
			_native = new AuthoredGeometryFx
			{
				Name = "AuthoredAttack"
			};
			AddChild(_native, forceReadableName: false, InternalMode.Disabled);
			_audio = new BattleAudio
			{
				Name = "Audio"
			};
			AddChild(_audio, forceReadableName: false, InternalMode.Disabled);
			_receipt = new Label3D
			{
				Name = "EventReceipt",
				Text = "",
				FontSize = 38,
				PixelSize = 0.009f,
				Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
				NoDepthTest = true,
				OutlineSize = 8,
				OutlineModulate = new Color("11151d"),
				Visible = false
			};
			_cues = new BattleCuePool
			{
				Name = "ContactCues"
			};
			AddChild(_cues, forceReadableName: false, InternalMode.Disabled);
			_receipt.Font = GD.Load<Font>("res://assets/Fonts/NotoSansJP-Regular.otf");
			AddChild(_receipt, forceReadableName: false, InternalMode.Disabled);
			if (FileAccess.FileExists("res://assets/attack-routes.json"))
			{
				Routes.Load(FileAccess.GetFileAsString("res://assets/attack-routes.json"));
			}
			if (FileAccess.FileExists("res://assets/presentation-data/card-vfx-profiles.json"))
			{
				CardProfiles.Load(FileAccess.GetFileAsString("res://assets/presentation-data/card-vfx-profiles.json"));
			}
			_timeline.TimeAdvanced += AdvanceClock;
			_timeline.Started += BeginBeat;
			_timeline.Contact += ContactBeat;
			_timeline.Finished += FinishBeat;
			_timeline.Drained += FinishBatch;
			_timeline.ContactPlanner = PlanContacts;
		}
	}

	public void Bind(GameSession session)
	{
		EnsureBuilt();
		if (_session != session)
		{
			if (_session != null)
			{
				_session.StateChanged -= Present;
				_session.RunCleared -= ClearRun;
			}
			CancelPresentation(applyLatest: false);
			ClearActors();
			_session = session;
			session.StateChanged += Present;
			session.RunCleared += ClearRun;
			RunState run = session.CurrentRun;
			if ((object)run != null)
			{
				SetRunState(run);
			}
		}
	}

	// Read-only values sampled synchronously by the opt-in diagnostic observer.
	internal (int Beat, int Ordinal) CaptureMotionDiagnosticContact() => (_ordinaryCallback?.Index ?? -1, BeatContactOrdinal);

	public FreezeDiagnosticState CaptureFreezeDiagnosticState(FreezeDiagnosticState state)
	{
		return state with { PendingCommands = PendingCommandCount, Locked = InputLocked,
			Presenting = IsPresenting, TimelineBusy = _timeline.Busy, QueueBusy = _actionQueue.Busy,
			PresentationTimeMs = float.IsFinite(_clock) && _clock >= 0f
				? (long)Math.Min(_clock * 1000.0, 9_000_000_000_000_000d) : -1,
			PendingActorContacts = PendingActorContacts, ActiveMotionCount = ActiveMotionCount,
			ActivePoseCount = ActivePoseCount, HeroPoseName = _hero?.PoseName,
			HeroPoseFrame = _hero?.PoseFrame ?? -1, HeroPoseStartCount = _hero?.PoseStartCount ?? 0,
			HeroMotionVersion = _hero?.Motion.MotionVersion ?? 0 };
	}

	public void Present(SessionChange change)
	{
		if (PresentationFaulted)
		{
			_latestRun = change.Current;
			_lastRevision = change.Revision;
			if (PresentationFaultBlocked)
			{
				InputLocked = true;
				_session?.SetPresentationBusy(true); // Load clears its presentation gate before notifying listeners.
			}
			if (!_faultRecovering && !PresentationFaultBlocked) ApplyFaultSnapshot(change.Current, change.Revision);
			return;
		}
		int generation = _actionQueue.Generation;
		try { PresentCore(change); }
		catch (Exception ex)
		{
			if (_session?.Revision == change.Revision && ReferenceEquals(_session.CurrentRun, change.Current))
				RecoverPresentationFault(ex, "BattleStage.Present", _actionQueue.Generation);
		}
	}

	private void PresentCore(SessionChange change)
	{
		_session?.RecordFreezeEvent("BattleStage.Present.begin");
		EnsureBuilt();
		if (!change.IsReset && change.Revision <= _lastRevision)
		{
			return;
		}
		_lastRevision = change.Revision;
		if (_session != null)
		{
			PresentationBatch batch;
			while (_session.TryDequeuePresentation(out batch))
			{
			}
		}
		bool wasPresenting = IsPresenting;
		if (change.IsReset)
		{
			CancelPresentation(applyLatest: false);
			_quality.Restart();
			ClearActors();
			wasPresenting = false;
		}
		_latestRun = change.Current;
		Routes.LeaderId = change.Current.LeaderId;
		if (_act != change.Current.Act)
		{
			_act = change.Current.Act;
			_world.Build(Math.Clamp(_act, 1, 3));
			CacheWorldLights();
			ApplyQualitySettings();
		}
		_audio.SyncRun((change.Current.Phase == "combat") ? change.Current : (change.Previous ?? change.Current));
		GameState before = (change.IsReset ? change.Current.Combat : change.Previous?.Combat);
		if (!wasPresenting)
		{
			if ((object)before != null)
			{
				SyncActors(before, change.Current.LeaderId);
			}
			else
			{
				GameState combat = change.Current.Combat;
				if ((object)combat != null)
				{
					SyncActors(combat, change.Current.LeaderId);
				}
			}
			_bornUids.Clear();
			_copiedEnter.Clear();
			if ((object)before != null)
			{
				foreach (CardInstance c in before.Player.Permanents)
				{
					_bornUids.Add(c.Uid);
				}
			}
		}
		if (change.Events.Count == 0)
		{
			if (!wasPresenting)
			{
				InputLocked = false;
				IsPresenting = false;
				_session?.SetPresentationBusy(busy: false);
				SetRunState(change.Current);
				PresentationCompleted?.Invoke();
			}
		}
		else
		{
			RegisterCommand(change, before);
		}
		_session?.RecordFreezeEvent("BattleStage.Present.end");
	}

	public void SetInterfaceRoute(string route)
	{
		if (PresentationFaulted)
		{
			_interfaceRoute = route;
			FaultVisualStep(() => { if (_actorRoot != null) _actorRoot.Visible = route == "combat"; });
			FaultVisualStep(() => { if (_retiredRoot != null) _retiredRoot.Visible = false; });
			return;
		}
		EnsureBuilt();
		if (_interfaceRoute != route)
		{
			_quality.Restart();
		}
		bool enteringCombat = _interfaceRoute != "combat" && route == "combat";
		_interfaceRoute = route;
		_actorRoot.Visible = route == "combat";
		_retiredRoot.Visible = route == "combat";
		if (route == "title")
		{
			CancelPresentation(applyLatest: false);
			ClearSourceRetainerBoard();
			_audio.SyncRun(null);
			return;
		}
		if (route != "combat")
		{
			if (!IsPresenting)
			{
				ClearSourceRetainerBoard();
			}
		}
		else if (enteringCombat)
		{
			GameState combat = (IsPresenting ? _displayState : _latestRun?.Combat);
			if ((object)combat != null)
			{
				SyncSourceRetainers(combat);
				RefreshActorLayout();
                // New visible combat placement is settled; explicit cancel/fault paths stay at rest.
                if (!IsPresenting && !InputLocked)
                    foreach (var actor in AllPresentationActors()) actor.ResumeAuthoredIdleForPlacement(_clock);
			}
		}
		RunState run = _latestRun;
		if ((object)run != null)
		{
			_audio.SyncRun(run);
		}
	}

	private void ClearRun(long revision)
	{
		CancelPresentation(applyLatest: false);
		_latestRun = null;
		_displayState = null;
		_lastRevision = revision;
		_ledger.Clear();
		_receipts.Clear();
		ClearActors();
		SetInterfaceRoute("title");
	}

	public bool ValidatePresentedReceipt(GameEvent e, CombatPresentationReceipt receipt, GameState? snapshot)
	{
		if ((object)e != _lastPresentedEvent)
		{
			return ValidReceipt(e, receipt, snapshot);
		}
		return !LastEventReceiptRejected;
	}

	public static bool ValidReceipt(GameEvent e, CombatPresentationReceipt? receipt, GameState? snapshot)
	{
		if ((object)receipt == null || (object)snapshot == null || !CombatPresentationLedger.Valid(e, receipt, out var _))
		{
			return false;
		}
		CombatPresentationVitals v = receipt.Before;
		if (receipt.Target == "player")
		{
			if (v.Hp == snapshot.Player.Hp && v.MaxHp == snapshot.Player.MaxHp && v.Block == snapshot.Player.Block)
			{
				return v.IceBlock.GetValueOrDefault() == snapshot.Player.IceBlock;
			}
			return false;
		}
		int? enemyIndex = receipt.EnemyIndex;
		if (enemyIndex.HasValue)
		{
			int i = enemyIndex.GetValueOrDefault();
			if (i >= 0 && i < snapshot.Enemies.Count)
			{
				EnemyState enemy = snapshot.Enemies[i];
				if (enemy.EnemyId == receipt.EnemyId && v.Hp == enemy.Hp && v.MaxHp == enemy.MaxHp)
				{
					return v.Block == enemy.Block;
				}
				return false;
			}
		}
		return false;
	}

	private EnemyAttackContactPlan.SourceProof? Proof(GameEvent_DamageDealt e)
	{
		int index = e.EnemyIndex ?? (-1);
		GameState before = _displayState;
		GameState after = GetEventChange(e)?.Current.Combat ?? _latestRun?.Combat;
		bool stable = (object)before != null && (object)after != null && before.Enemies.Count == after.Enemies.Count && index >= 0 && index < before.Enemies.Count && before.Enemies[index].EnemyId == after.Enemies[index].EnemyId;
		return new EnemyAttackContactPlan.SourceProof(index, stable ? before.Enemies[index].EnemyId : "", stable);
	}

	private EnemyAttackContactPlan? PlanContacts(GameEvent_DamageDealt e)
	{
		EnemyAttackContactPlan.SourceProof proof = Proof(e);
		if (!_receipts.TryGetValue(e, out var r))
		{
			return EnemyAttackContactPlan.Capture(e, e.PresentationReceipt, proof);
		}
		return _ledger.ConfigureEnemyProjection(r, proof);
	}

	public void SetState(GameState state)
	{
		EnsureBuilt();
		SyncActors(state, _latestRun?.LeaderId ?? "leader_green");
	}

	public void SetRunState(RunState run)
	{
		EnsureBuilt();
		_latestRun = run;
		if (_act != run.Act)
		{
			_act = run.Act;
			_world.Build(Math.Clamp(_act, 1, 3));
			CacheWorldLights();
			ApplyQualitySettings();
		}
		_audio.SyncRun(run);
		GameState combat = run.Combat;
		if ((object)combat != null)
		{
			SyncActors(combat, run.LeaderId);
			return;
		}
		ClearActors();
		_displayState = null;
	}

	private static Vector3 Path(float t, float s)
	{
		float a = Mathf.DegToRad(22f);
		return new Vector3(t * Mathf.Cos(a) - s * Mathf.Sin(a), 0f, 0f - (t * Mathf.Sin(a) + s * Mathf.Cos(a)));
	}

	private static Vector3 EnemyFoot(int i, int count)
	{
		float[] t = count switch
		{
			1 => new float[1] { 4.459f }, 
			2 => new float[2] { 3.2f, 7.055f }, 
			3 => new float[3] { 2.2f, 5.708f, 8.741f }, 
			_ => new float[4] { 1.6f, 4.417f, 6.716f, 10.01f }, 
		};
		float t2 = ((count <= 4) ? t[i] : (1.6f + 8.41f * (float)i / (float)Math.Max(1, count - 1)));
		float s;
		if (i % 2 != 0)
		{
			s = ((count >= 3) ? 0.1f : 0.7f);
		}
		else
		{
			s = -0.5f;
		}
		return Path(t2, s);
	}

	private string NameFor(string id, bool leader = false)
	{
		object obj;
		if (!leader)
		{
			obj = _session?.Catalog.Enemies.FirstOrDefault((EnemyDef x) => x.Id == id)?.Name ?? id;
		}
		else
		{
			obj = _session?.Catalog.Leaders.FirstOrDefault((LeaderDef x) => x.Id == id)?.Name;
			if (obj == null)
			{
				return id;
			}
		}
		return (string)obj;
	}

	private void SyncActors(GameState state, string leaderId)
	{
		_displayState = state;
		if (_hero == null || _hero.DefinitionId != leaderId)
		{
			if (_hero != null)
			{
				_actorRoot.RemoveChild(_hero);
				_hero.QueueFree();
			}
			_hero = new BattleActor();
			_hero.Configure("leaders", leaderId, NameFor(leaderId, leader: true), Path(-5f, 0.9f), 2.25f, _world.PhoneLayout);
			_actorRoot.AddChild(_hero, forceReadableName: false, InternalMode.Disabled);
		}
		_hero.SetVitals(state.Player.Hp, state.Player.MaxHp, state.Player.Block);
		_hero.SetDead(state.Player.Hp <= 0);
		for (int i = 0; i < state.Enemies.Count; i++)
		{
			EnemyState enemy = state.Enemies[i];
			if (!_enemies.TryGetValue(i, out var actor) || actor.DefinitionId != enemy.EnemyId)
			{
				if (actor != null)
				{
					_actorRoot.RemoveChild(actor);
					actor.QueueFree();
				}
				actor = new BattleActor
				{
					EnemyIndex = i
				};
				actor.Configure("enemies", enemy.EnemyId, NameFor(enemy.EnemyId), EnemyFoot(i, state.Enemies.Count), 2.05f, _world.PhoneLayout);
				_actorRoot.AddChild(actor, forceReadableName: false, InternalMode.Disabled);
				_enemies[i] = actor;
			}
			actor.Home = EnemyFoot(i, state.Enemies.Count);
			actor.SetVitals(enemy.Hp, enemy.MaxHp, enemy.Block);
			actor.SetDead(enemy.Hp <= 0);
			if (enemy.Hp <= 0 && !_deaths.ContainsKey(actor))
			{
				actor.SetDissolve(1f);
			}
			actor.IntentLabel.Text = ((state.HideIntents == true) ? "???" : CardText.IntentText(state, i));
		}
		int[] array = _enemies.Keys.Where((int num2) => num2 >= state.Enemies.Count).ToArray();
		foreach (int i2 in array)
		{
			BattleActor a = _enemies[i2];
			_actorRoot.RemoveChild(a);
			a.QueueFree();
			_enemies.Remove(i2);
		}
		SyncSourceRetainers(state);
		RefreshActorLayout();
	}

    private Rect2 _style04RosterLane = new Rect2(.44f, .25f, .53f, .20f);
    public void SetStyle04RosterLane(Rect2 normalized)
    {
        if (!float.IsFinite(normalized.Position.X) || !float.IsFinite(normalized.Position.Y) ||
            !float.IsFinite(normalized.Size.X) || !float.IsFinite(normalized.Size.Y) ||
            normalized.Size.X <= 0f || normalized.Size.Y <= 0f) return;
        if (_style04RosterLane.Position.DistanceSquaredTo(normalized.Position) < .0000001f &&
            _style04RosterLane.Size.DistanceSquaredTo(normalized.Size) < .0000001f) return;
        _style04RosterLane = normalized;
        RefreshActorLayout();
    }

    // Keep legacy sizing for other counts. Three-body rows reclaim transparent
    // margins with a shared scale; per-head intents and the HP baseline stay aligned.
    private float LayoutStyle04EnemyRoster(Vector2 size, string kind, float ui)
    {
        var actors = _enemies.OrderBy(kv => kv.Key).Select(kv => kv.Value)
            .Where(a => !a.Dead || a.DissolveAmount < 1f).ToArray();
        if (actors.Length == 0 || !actors.Any(a => a.Style04StaticSprite)) return 1f;
        bool enlargedTrio = actors.Length == 3 && actors.All(a => a.Style04StaticSprite);
        float left=size.X*(enlargedTrio?.34f:.44f), right=size.X*.97f, gap=size.X*(enlargedTrio?.018f:.012f);
        var widths=new float[actors.Length];
        var centerOffsets=new float[actors.Length];
        int tallestOpaque=0;
        for(int i=0;i<actors.Length;i++)
        {
            var actor=actors[i]; actor.Home=EnemyFoot(i,actors.Length);
            float depth=-(_world.Camera.GlobalTransform.AffineInverse()*actor.Home).Z;
            var z=SourceActorSizing.Compute("enemies",actor.SpiritSizingWidth,actor.SpiritSizingHeight,actor.SpiritSizingTopRows,actor.Phone96,kind,size,_world.Camera.Fov,_world.CameraLayout.ReferenceDistance,depth,_world.PhoneLayout,ui,commonPixelGrid:actor.Style04StaticSprite);
            float texel=z.RoundedScreenHeight/actor.SpiritSizingHeight;
            widths[i]=texel*actor.SpiritSizingWidth;
            if (enlargedTrio)
            {
                var texture=actor.RosterLayoutTexture;
                if (!_style04OpaqueFootprints.TryGetValue(texture,out var opaque))
                { using var pixels=texture.GetImage(); opaque=pixels.GetUsedRect(); _style04OpaqueFootprints.Add(texture,opaque); }
                widths[i]=Math.Max(1,opaque.Size.X)*texel;
                centerOffsets[i]=(opaque.Position.X+opaque.Size.X*.5f-texture.GetWidth()*.5f)*texel;
                tallestOpaque=Math.Max(tallestOpaque,opaque.Size.Y);
            }
        }
        float available=Math.Max(1f,right-left-gap*(actors.Length-1));
        // Recover transparent canvas margins first. A modest common boost helps
        // small trios too, while 112px-tall bodies keep their full-size ceiling.
        float desired=enlargedTrio?Math.Min(1.15f,112f/Math.Max(1,tallestOpaque)):1f;
        float factor=actors.Length==1?1f:Math.Min(desired,available/Math.Max(1f,widths.Sum()));
        var packed=widths.Select(w=>w*factor).ToArray();
        if(enlargedTrio)
        {
            // Narrow silhouettes still reserve a complete HP/intent column at
            // both screen edges. Fit one shared texel factor around those minima.
            float minimumColumn=size.X*.19f;
            float lo=0f,hi=factor;
            for(int pass=0;pass<24;pass++)
            { float mid=(lo+hi)*.5f; if(widths.Sum(w=>Math.Max(minimumColumn,w*mid))<=available)lo=mid;else hi=mid; }
            factor=lo;
            // Sizing above stays identical to the accepted enlarged row. Only
            // packing changes: reclaim empty slots, retain HUD breathing room,
            // and anchor the compact group to the existing right edge.
            float compactColumn=size.X*.165f;
            packed=widths.Select(w=>Math.Max(compactColumn,w*factor)).ToArray();
            gap=size.X*.012f;
        }
        float total=packed.Sum()+gap*(actors.Length-1);
        float cursor=enlargedTrio?right-total:left+(right-left-total)*.5f;
        Vector2 renderToLogical=GetViewport().GetVisibleRect().Size/size;
        for(int i=0;i<actors.Length;i++)
        {
            var actor=actors[i];float width=packed[i];
            float depth=-(_world.Camera.GlobalTransform.AffineInverse()*actor.Home).Z;
            // Pack horizontally on the authored road plane. Preserve EnemyFoot's
            // projected Y and camera depth, shared with the hero's Y=0 Path plane.
            float roadY=_world.Camera.UnprojectPosition(actor.Home).Y;
            actor.Home=_world.Camera.ProjectPosition(new Vector2((cursor+width*.5f-centerOffsets[i]*factor)*renderToLogical.X,roadY),depth);
            actor.SetMeta("style04_roster_scale",factor);cursor+=width+gap;
        }
        return factor;
    }

	private void RefreshActorLayout()
	{
		if (_actorRoot == null || _world.Camera == null)
		{
			return;
		}
		RefreshSourceRetainerFeet();
		Viewport viewport = GetViewport();
		Vector2 size = viewport.GetTexture()?.GetSize() ?? viewport.GetVisibleRect().Size;
		if (size.X <= 0f || size.Y <= 0f)
		{
			size = viewport.GetVisibleRect().Size;
		}
		float ui = 1.6f;
		string[] args = OS.GetCmdlineUserArgs();
		for (int i = 0; i + 1 < args.Length; i++)
		{
			if (args[i] == "-uiscale" && float.TryParse(args[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && value > 1.001f)
			{
				ui = value;
			}
		}
		string kind = "normal";
		RunState run = _latestRun;
		if ((object)run != null && run.Row >= 0 && run.Row < run.Map.Count && run.Col >= 0 && run.Col < run.Map[run.Row].Count)
		{
			kind = run.Map[run.Row][run.Col].Type;
		}
		float rosterScale = LayoutStyle04EnemyRoster(size, kind, ui);
		foreach (BattleActor actor in _actorRoot.GetChildren().OfType<BattleActor>())
		{
			float depth = 0f - (_world.Camera.GlobalTransform.AffineInverse() * actor.Home).Z;
			string category;
			if (actor.EnemyIndex >= 0)
			{
				category = "enemies";
			}
			else
			{
				category = ((actor == _hero) ? "leaders" : "dolls");
			}
			SourceActorSizingResult sizing = SourceActorSizing.Compute(category, actor.SpiritSizingWidth, actor.SpiritSizingHeight, actor.SpiritSizingTopRows, actor.Phone96, kind, size, _world.Camera.Fov, _world.CameraLayout.ReferenceDistance, depth, _world.PhoneLayout, ui, commonPixelGrid: actor.Style04StaticSprite);
			if (actor.EnemyIndex >= 0 && Math.Abs(rosterScale - 1f) > .000001f) sizing = sizing with { CanvasTargetWidth = sizing.CanvasTargetWidth * rosterScale, PixelScale = sizing.PixelScale * rosterScale, RoundedScreenHeight = sizing.RoundedScreenHeight * rosterScale, WorldImageHeight = sizing.WorldImageHeight * rosterScale };
			actor.SetSourceImageHeight(sizing.WorldImageHeight);
			float proxyUnit = SourceFloatProxyModel.ProjectionUnit(sizing.CanvasScale, viewport.GetVisibleRect().Size.Y, size.Y);
			actor.SetSourceUiProxyMetrics(SourceFloatProxyModel.UnroundedHeight(actor.SpiritSizingHeight, sizing.PixelScale, proxyUnit), proxyUnit);
			actor.SetSourceCanvasMotionScale(sizing.WorldImageHeight / Math.Max(1f, sizing.RoundedScreenHeight) * sizing.CanvasScale);
			actor.SetSourceDisplayTransferEnabled(ActorSourceDisplayRequested);
			actor.ConfigureSourceAppearance(_act, _world.Camera, _displayState?.Enemies.Count ?? 1);
		}
	}

	private BattleActor? Enemy(int? i)
	{
		if (!i.HasValue || !_enemies.TryGetValue(i.Value, out var a))
		{
			return null;
		}
		return a;
	}

	private void BeginBeat(PresentationBeat beat)
	{
		_session?.RecordFreezeEvent("BattleStage.BeginBeat");
		_sourceActor = null;
		_targetActor = null;
		_receipt.Visible = false;
		_projectionActive = false;
		_contactHp = 0;
		_contactBlock = 0;
		_effectStartedAt = 0f;
		_effectOffset = 0f;
		PresentBeforeNumericBoundary(beat.Event);
		CombatPresentationReceipt numeric = CombatPresentationLedger.Read(beat.Event);
		_rejectBeat = (object)numeric != null && !ValidReceipt(beat.Event, numeric, _displayState);
		if (_receipts.TryGetValue(beat.Event, out var owned) && owned.Reason != null)
		{
			_rejectBeat = true;
		}
		if (_rejectBeat)
		{
			if (owned != null && owned.Reason == null)
			{
				owned.Reason = "numeric_display_boundary_mismatch";
			}
			RejectedReceiptCount++;
			return;
		}
		EnemyAttackContactPlan contacts = beat.Contacts;
		if (contacts != null && contacts.Individual && beat.Event is GameEvent_DamageDealt enemyHit && _receipts.TryGetValue(beat.Event, out var record))
		{
			_sourceProof = Proof(enemyHit);
			_projectionActive = _ledger.TryBeginProjection(record, _sourceProof);
		}
		if (beat.Event is GameEvent_DamageDealt hit && (object)beat.Route != null && (object)beat.Scenario != null)
		{
			DamagePresentationSource owner = hit.PresentationSource;
			string permanentUid;
			if ((object)owner == null)
			{
				permanentUid = hit.SourceUid;
			}
			else
			{
				permanentUid = ((owner.Kind == "permanent") ? owner.Uid : null);
			}
			ref BattleActor? sourceActor = ref _sourceActor;
			BattleActor? battleActor;
			if (hit.Source == "enemy")
			{
				battleActor = Enemy(hit.EnemyIndex);
			}
			else
			{
				battleActor = ((permanentUid != null && _retainers.TryGetValue(permanentUid, out var doll)) ? doll : _hero);
			}
			sourceActor = battleActor;
			_targetActor = ((hit.PresentationReceipt?.Target == "player" || hit.Source == "enemy") ? _hero : Enemy(hit.PresentationReceipt?.EnemyIndex ?? hit.EnemyIndex));
			Vector3 from = _sourceActor?.Contact ?? (Path(-5f, 0.9f) + Vector3.Up);
			Vector3 to = _targetActor?.Contact ?? (Path(4.459f, -0.5f) + Vector3.Up);
			if (hit.Source == "enemy")
			{
				BeginSourceEnemyLunge(hit);
			}
			else
			{
				OrdinaryBeat? ordinaryCallback = _ordinaryCallback;
				if ((ordinaryCallback == null || !ordinaryCallback.Direct)
                    && !(_sourceActor==_hero && (IsOwnedInstalledPermanentContact(hit) || IsOwnedCenserNecroContact(hit))))
				{
					_sourceActor?.PlayPose("attack", _clock);
				}
			}
			if (!_projectionActive)
			{
				StartEffect(ReactionContactRoute(LightningContactRoute(NecroContactRoute(PermanentContactRoute(EnemyContactRoute(beat.Route,hit),hit),hit),hit),hit), beat.Scenario, from, to);
				if (hit.HpLoss > 0)
				{
					_audio.PlayKey("DamageDealt." + ((hit.Source == "enemy") ? "enemy" : "player") + ".swing");
				}
			}
		}
		else if ((object)beat.Route != null && (object)beat.Scenario != null)
		{
			CombatPresentationReceipt receipt = CombatPresentationLedger.Read(beat.Event);
			_targetActor = ((receipt?.Target == "enemy") ? Enemy(receipt.EnemyIndex) : _hero);
			ref BattleActor? sourceActor2 = ref _sourceActor;
			BattleActor? battleActor2;
			if (beat.Event is GameEvent_ConfusedAttack confused)
			{
				battleActor2 = Enemy(confused.EnemyIndex);
			}
			else
			{
				battleActor2 = ((beat.Event is GameEvent_ThornsReflected reflected) ? Enemy(reflected.EnemyIndex) : _targetActor);
			}
			sourceActor2 = battleActor2;
			if (beat.Event is GameEvent_ConfusedAttack confusedAttack)
			{
				PreviewConfusedEnemyAttack(beat, confusedAttack);
			}
			if (_targetActor != null)
			{
				StartEffect(StatusContactVfxProfiles.Select(beat.Route,beat.Event,_eventOwners.ContainsKey(beat.Event)), beat.Scenario, _sourceActor?.Contact ?? _targetActor.Contact, _targetActor.Impact);
			}
		}
		else if (beat.Event is GameEvent_EnemyActionExecuting action)
		{
			_sourceActor = Enemy(action.EnemyIndex);
		}
		else if (beat.Event is GameEvent_CardPlayed)
		{
			_sourceActor = _hero;
		}
	}

	private void ContactBeat(PresentationBeat beat)
	{
		_session?.RecordFreezeEvent("BattleStage.ContactBeat");
		int callbackGeneration = _actionQueue.Generation;
		GameEvent e = beat.Event;
		LastEventReceiptRejected = _rejectBeat;
		_lastPresentedEvent = e;
		_lastPresentedVitals = null;
		EnemyAttackContactPlan contacts = beat.Contacts;
		bool last = contacts == null || !contacts.Individual || BeatContactOrdinal >= beat.Contacts.Count - 1;
		if (_rejectBeat)
		{
			if (last)
			{
				if (_receipts.TryGetValue(e, out var reject))
				{
					_ledger.Commit(reject);
				}
				PublishPresentationObservers(EventPresented, e, "BattleStage.EventObserver");
			}
		}
		else
		{
			if (!last && !_projectionActive)
			{
				return;
			}
			int priorHp = _targetActor?.DisplayedHp ?? 0;
			if (e is GameEvent_DamageDealt)
			{
				DamageContactCount++;
			}
			if (_receipts.TryGetValue(e, out var tracked))
			{
				if (_projectionActive)
				{
					if (!_ledger.CommitContribution(tracked, BeatContactOrdinal, _sourceProof))
					{
						LastEventReceiptRejected = true;
						RejectedReceiptCount++;
						if (last)
						{
							PublishPresentationObservers(EventPresented, e, "BattleStage.EventObserver");
						}
						return;
					}
				}
				else
				{
					_ledger.Commit(tracked);
				}
				SyncLedgerActor(tracked.Target);
				PublishNumericContact(e, beat);
				if (callbackGeneration != _actionQueue.Generation)
				{
					return;
				}
			}
			if (e is GameEvent_ConfusedAttack confusedAttack)
			{
				PresentConfusedEnemyAttackContact(confusedAttack);
			}
			if (e is GameEvent_DamageDealt hit)
			{
				PresentEnemyAttackContact(beat, hit);
				if (last && !_receipts.ContainsKey(e))
				{
					ApplyReceipt(hit.PresentationReceipt);
				}
				if ((object)hit.PresentationReceipt == null)
				{
					_targetActor?.DamageFallback(hit.HpLoss, hit.Blocked.GetValueOrDefault());
				}
				int loss = (_projectionActive ? beat.Contacts[BeatContactOrdinal].HpLossContribution : hit.HpLoss);
				int block = (int)(_projectionActive ? (beat.Contacts[BeatContactOrdinal].BlockContribution + beat.Contacts[BeatContactOrdinal].IceContribution) : CombatOutcome.Read(hit).Absorbed);
				_contactHp = loss;
				_contactBlock = block;
				OrdinaryBeat direct = _ordinaryCallback;
				if (direct != null && direct.Direct && direct.Volley == 0 && BeatContactOrdinal <= 0
                    && !(Hd2dAttackPrototype.Enabled && direct.TargetOrdinal>0 && PhysicalVfxProfiles.ForCard(direct.Action?.Card?.CardId)?.TargetPattern==PhysicalVfxTargetPattern.ConditionalTransfer)
					&& direct.Action?.Profile != null && direct.Action.Capture is { } attackOwner)
				{
					if (!TryContactVisual(() => direct.Action.Actor?.SynchronizeAttackContactPose(attackOwner, _clock), "AttackPose", callbackGeneration)) return;
				}
				if (_projectionActive)
				{
					AttackRoute route = ((loss > 0) ? Routes.Resolve(hit) : new AttackRoute("system:outcome", "", "system", (block > 0) ? "guard" : "nullified", "SystemOutcomeFx", "neutral", FixedSmall: true, "proven contribution outcome"));
					EffectScenario scenario = EffectScenario.For(route, loss, priorHp, hit.PresentationReceipt?.Before.MaxHp);
					_effectStartedAt = BeatTime;
					_effectOffset = scenario.Windup;
					StartEffect(ReactionContactRoute(LightningContactRoute(NecroContactRoute(PermanentContactRoute(EnemyContactRoute(route,hit),hit),hit),hit),hit), scenario, _sourceActor?.Contact ?? Vector3.Zero, _targetActor?.Contact ?? Vector3.Zero);
				}
				if (!TryContactVisual(() => _audio.PlayDamageContact(hit.Source, loss, block, hit.Source == "player" && (hit.Amount >= 15 || hit.Exposed == true), hit.PresentationSource?.Kind == "card" ? hit.PresentationSource.DefinitionId : null), "ContactAudio", callbackGeneration)) return;
				if (loss > 0)
				{
					ApplySourceImpact(beat, hit);
					if (!TryContactVisual(() => _targetActor?.Flash(_clock, hit.PresentationSource?.Kind == "card" && Hd2dAttackPrototype.Supports("card:" + hit.PresentationSource.DefinitionId) ? .045f : .18f), "ContactFlash", callbackGeneration)) return;
				}
				else if (block > 0)
				{
					if (!TryContactVisual(() => _targetActor?.PlayPose("block", _clock), "ContactBlockPose", callbackGeneration)) return;
				}
				string text;
				if (loss > 0)
				{
					text = $"−{loss}";
				}
				else
				{
					text = ((block > 0) ? $"防御 {block}" : "0");
				}
				if (!_projectionActive)
				{
					EnemyAttackPresentation multi = hit.PresentationEnemyAttack;
					if ((object)multi != null && multi.ExecutedHits > 1)
					{
						text += $"  ×{multi.ExecutedHits}";
					}
				}
				if (!TryShowSourceIncomingDamage(hit, loss, block, _projectionActive ? beat.Contacts[BeatContactOrdinal].AmountContribution : hit.Amount, _projectionActive, BeatContactOrdinal) && (hit.Source != "enemy" || loss > 0 || block > 0 || hit.Amount <= 0) && !TryShowSourceDamage(hit, loss, block))
				{
					ShowReceipt(text, (_targetActor?.Contact ?? Vector3.Up) + Vector3.Right * ((float)(BeatContactOrdinal % 3 - 1) * 0.14f), (loss > 0) ? new Color("fff1e6") : new Color("b7c8df"));
				}
			}
			else
			{
				PresentTurnBoundary(e);
				PresentSourcePhaseCue(e);
				CombatPresentationReceipt receipt = e.GetType().GetProperty("PresentationReceipt")?.GetValue(e) as CombatPresentationReceipt;
				if (!_receipts.ContainsKey(e))
				{
					ApplyReceipt(receipt);
				}
				_audio.PlayEvent(e);
				ShowStatusCue(e);
				GameEvent gameEvent2 = e;
				if (!(gameEvent2 is GameEvent_BlockGained b))
				{
					if (!(gameEvent2 is GameEvent_HpHealed h))
					{
						if (!(gameEvent2 is GameEvent_EnemyHealed h2))
						{
							if (!(gameEvent2 is GameEvent_ConfusedAttack c))
							{
								if (!(gameEvent2 is GameEvent_BurnTick b2))
								{
									if (!(gameEvent2 is GameEvent_ScaldTick b3))
									{
										if (!(gameEvent2 is GameEvent_HpLost b4))
										{
											if (!(gameEvent2 is GameEvent_ThornsReflected b5))
											{
												if (!(gameEvent2 is GameEvent_RegenTicked h3))
												{
													if (!(gameEvent2 is GameEvent_EnemyDied d))
													{
														if (!(gameEvent2 is GameEvent_EnemyFled d2))
														{
															if (!(gameEvent2 is GameEvent_EnemyIntentDeclared intent))
															{
																if (!(gameEvent2 is GameEvent_EnemySummoned) && !(gameEvent2 is GameEvent_EnemySplit) && !(gameEvent2 is GameEvent_EnemyHatched) && !(gameEvent2 is GameEvent_PermanentPlayed) && !(gameEvent2 is GameEvent_RetainerCopied) && !(gameEvent2 is GameEvent_RetainerExpired) && !(gameEvent2 is GameEvent_RetainerSacrificed) && !(gameEvent2 is GameEvent_PermanentDismissed) && !(gameEvent2 is GameEvent_TokenDestroyed))
																{
																	if (!(gameEvent2 is GameEvent_TurnEnded) && !(gameEvent2 is GameEvent_TurnStarted))
																	{
																		if (!(gameEvent2 is GameEvent_ReactionTriggered))
																		{
																			if (!(gameEvent2 is GameEvent_ActionNegated))
																			{
																				if (gameEvent2 is GameEvent_CombatEnded end)
																				{
																					ShowReceipt((end.Result == "won") ? "VICTORY" : "DEFEAT", new Vector3(0f, 3f, 0f), new Color("ece5d7"));
																				}
																			}
																			else
																			{
																				ShowReceipt("無効", _hero?.Contact ?? Vector3.Up, new Color("d8cfed"));
																			}
																		}
																		else
																		{
																			ShowReceipt("リアクション", _hero?.Contact ?? Vector3.Up, new Color("d8cfed"));
																		}
																	}
																}
																else
																{
																	PresentPopulation(e);
																}
															}
															else
															{
																GameState intentState = _displayState;
																if ((object)intentState != null && intent.EnemyIndex >= 0 && intent.EnemyIndex < intentState.Enemies.Count)
																{
																	EnemyState[] values = intentState.Enemies.ToArray();
																	values[intent.EnemyIndex] = values[intent.EnemyIndex]with
																	{
																		Intent = intent.Intent
																	};
																	_displayState = intentState with
																	{
																		Enemies = values
																	};
																	BattleActor actor = Enemy(intent.EnemyIndex);
																	if (actor != null)
																	{
																		actor.IntentLabel.Text = ((intentState.HideIntents == true) ? "???" : CardText.IntentText(_displayState, intent.EnemyIndex));
																	}
																}
															}
														}
														else
														{
															PresentAuthoredFled(d2);
                                    StartDeath(Enemy(d2.EnemyIndex), retired: false, fled: true);
														}
													}
													else
													{
														StartDeath(Enemy(d.EnemyIndex));
													}
												}
												else
												{
													ShowReceipt($"+{h3.Amount}", Enemy(h3.EnemyIndex)?.Contact ?? Vector3.Up, new Color("adc998"));
												}
											}
											else
											{
												ShowReceipt($"−{b5.HpLoss}", _hero?.Contact ?? Vector3.Up, new Color("e5b9a5"));
											}
										}
										else
										{
											ShowReceipt($"−{b4.Amount}", _hero?.Contact ?? Vector3.Up, new Color("e5b9a5"));
										}
									}
									else
									{
										ShowReceipt($"−{b3.Amount}", _hero?.Contact ?? Vector3.Up, new Color("e5b9a5"));
									}
								}
								else
								{
									ShowReceipt($"−{b2.Amount}", Enemy(b2.EnemyIndex)?.Contact ?? Vector3.Up, new Color("e5b9a5"));
								}
							}
							else
							{
								BattleStage battleStage = this;
								string text2;
								if (c.HpLoss > 0)
								{
									text2 = $"−{c.HpLoss}";
								}
								else
								{
									text2 = ((c.Blocked > 0) ? $"防御 {c.Blocked}" : "0");
								}
								battleStage.ShowReceipt(text2, Enemy(c.TargetIndex)?.Contact ?? Vector3.Up, new Color("fff1e6"));
							}
						}
						else
						{
							if ((object)receipt == null)
							{
								Enemy(h2.TargetIndex)?.Heal(h2.Amount);
							}
							ShowReceipt($"+{h2.Amount}", Enemy(h2.TargetIndex)?.Contact ?? Vector3.Up, new Color("adc998"));
						}
					}
					else
					{
						if ((object)receipt == null)
						{
							_hero?.Heal(h.Amount);
						}
						ShowReceipt($"+{h.Amount}", _hero?.Contact ?? Vector3.Up, new Color("adc998"));
					}
				}
				else
				{
					BattleActor target = ((b.Target == "player") ? _hero : Enemy(receipt?.EnemyIndex));
					if ((object)receipt == null && b.Target == "player")
					{
						target?.GainBlock(b.Amount);
					}
					if (target != null && ((b.Target == "player") ? (!TryShowSourceHeroBlock(b, target, receipt)) : (!TryShowSourceEnemyBlock(b, target, receipt))))
					{
						ShowReceipt($"守り +{b.Amount}", target.Contact, new Color("becbdc"));
					}
				}
			}
			if (last)
			{
				if (callbackGeneration == _actionQueue.Generation) PresentSupportEffect(e);
				PublishPresentationObservers(EventPresented, e, "BattleStage.EventObserver");
			}
		}
	}

	// Bounded, non-constructing contact decorations do not own numeric receipts or completion.
	// Effect/text, enemy articulation and injury-layer creation stay outside: partial resources need owned cleanup.
	// Commit, actor-vitals synchronization, and legacy numeric fallbacks remain outside this boundary.
	private bool TryContactVisual(Action action, string phase, int generation)
	{
		if (generation != _actionQueue.Generation) return false;
		try { action(); }
		catch (Exception ex)
		{
			try { _session?.RecordFreezeEvent("BattleStage.Visual." + phase, false, ex); }
			catch { } // Diagnostics must not strand the completed numeric contact.
		}
		return generation == _actionQueue.Generation;
	}

	private void ShowStatusCue(GameEvent e)
	{
		if (PresentMergedEventCue(e))
		{
			return;
		}
		BattleActor actor = null;
		string text = null;
		string icon = null;
		Color color = new Color("f4ecd6");
		if (!(e is GameEvent_StatusInflicted v))
		{
			if (!(e is GameEvent_ExposedApplied v2))
			{
				if (!(e is GameEvent_EnemyWeakened v3))
				{
					if (!(e is GameEvent_BurnApplied v4))
					{
						if (!(e is GameEvent_StrengthGained v5))
						{
							if (!(e is GameEvent_GrowthAdded v6))
							{
								if (!(e is GameEvent_MomentumAdded v7))
								{
									if (!(e is GameEvent_IceBlockGained v8))
									{
										if (!(e is GameEvent_ArtifactBlocked v9))
										{
											if (!(e is GameEvent_PlayerArtifactBlocked v10))
											{
												if (!(e is GameEvent_LightGained v11))
												{
													if (!(e is GameEvent_LightSpent v12))
													{
														if (e is GameEvent_LightDischarged { Spent: >0 } v13)
														{
															actor = _hero;
															text = (CardText.IsLightPayment(v13) ? CardText.LightPayLine(v13) : ("灯" + v13.Spent + " 放出!"));
															color = new Color("ead08a");
														}
													}
													else if (v12.Amount > 0)
													{
														actor = _hero;
														text = "灯 −" + v12.Amount;
														color = new Color("c4beb2");
													}
												}
												else if (v11.Amount > 0)
												{
													actor = ((v11.SourceUid != null && _retainers.TryGetValue(v11.SourceUid, out var source)) ? source : _hero);
													text = "灯 +" + v11.Amount;
													color = new Color("c99a3a");
												}
											}
											else
											{
												actor = _hero;
												text = "時計仕掛けの土産が" + StatusName(v10.Status) + "を弾いた";
												color = new Color("9d86bf");
											}
										}
										else
										{
											actor = Enemy(v9.EnemyIndex);
											text = "アーティファクトが" + CardText.DebuffName(v9.Effect) + "を弾いた";
											color = new Color("9d86bf");
										}
									}
									else
									{
										actor = _hero;
										text = "氷壁 +" + v8.Amount;
										icon = "shield";
										color = new Color("d6e6fa");
										actor?.PlayPose("block", _clock);
									}
								}
								else if (v7.Amount > 0)
								{
									actor = _hero;
									text = "勢い +" + v7.Amount;
									color = new Color("c99a3a");
								}
							}
							else if (v6.Amount > 0)
							{
								actor = _hero;
								text = "成長 +" + v6.Amount;
								color = new Color("7fa86c");
							}
						}
						else if (v5.Amount != 0)
						{
							actor = Enemy(v5.EnemyIndex);
							text = "筋力 " + ((v5.Amount > 0) ? "+" : "") + v5.Amount;
							color = new Color((v5.Amount > 0) ? "c99a3a" : "6f95b8");
						}
					}
					else
					{
						actor = Enemy(v4.EnemyIndex);
						text = "延焼 +" + v4.Amount;
						color = new Color("e8742f");
					}
				}
				else
				{
					actor = Enemy(v3.EnemyIndex);
					text = "威圧 +" + v3.Amount;
					color = new Color("6f95b8");
				}
			}
			else
			{
				actor = Enemy(v2.EnemyIndex);
				text = "急所 +" + v2.Amount;
				color = new Color("c99a3a");
			}
		}
		else
		{
			actor = _hero;
			text = StatusName(v.Status) + " +" + v.Amount;
			icon = "exposed";
			color = new Color("9d86bf");
		}
		if (actor != null && text != null)
		{
			_cues.Show(text, actor.Contact + Vector3.Up * 0.65f, color, icon, 1f);
		}
	}

	private static string StatusName(string name)
	{
		return name switch
		{
			"weak" => "弱体", 
			"vulnerable" => "脆弱", 
			"frail" => "虚弱", 
			"restrain" => "拘束", 
			"mist" => "霞み", 
			"slow" => "重り", 
			"wound" => "負傷", 
			"scald" => "火傷", 
			"junk" => "がらくた", 
			_ => name, 
		};
	}

	// Lost-soul eye lasers retain the existing effect and contact schedule. Only
	// this exact route gets an authored eye source, in the effect parent's space.
	private Vector3 LostSoulEyeSource(AttackRoute route, Vector3 fallback)
	{
		var actor = _sourceActor;
		if (route.Key != "enemy:enemy_lost_soul/eye_lasers" || route.Renderer != "EnemyContactFx" || route.Family != "enemy_beam" ||
		    actor == null || actor.DefinitionId != "enemy_lost_soul" || actor.Dead || !actor.AuthoredFramesEnabled ||
		    actor.Style04MotionSample.MoveId != "eye_lasers" || actor.Style04MotionSample.OwnerVersion != actor.ExecutionVersion) return fallback;
		var sprite = actor.Sprite; var texture = sprite?.Texture; var frame = actor.AuthoredFrameSample;
		if (texture == null || texture.GetWidth() != 144 || texture.GetHeight() != 88 || sprite!.FlipH || sprite.FlipV || !frame.Active || frame.MissingClip) return fallback;
		Vector2? eye = (frame.Clip, frame.FrameIndex) switch {
		    ("rest", 0) or ("idle", 0) => new Vector2(60.5f, 20.5f),
		    ("idle", 1) => new Vector2(60.5f, 21.5f),
		    ("beamprepare", 0) => new Vector2(55.5f, 24.5f),
		    ("release", 0) => new Vector2(58.5f, 19.5f),
		    ("recovery", 0) => new Vector2(60.5f, 23.5f),
		    _ => null
		};
		if (eye is not Vector2 pixel) return fallback;
		Vector3 local = new((pixel.X - texture.GetWidth() * .5f + sprite.Offset.X) * sprite.PixelSize,
		    (texture.GetHeight() * .5f - pixel.Y + sprite.Offset.Y) * sprite.PixelSize, 0);
		return ToLocal(sprite.GlobalTransform * local);
	}

    // M56 changes only the exact sentry beam source. The accepted M43 helper is unchanged.
    // Select the ACTUAL active image, including rest at contact, never a guessed release pose.
    // M61 exact cannon route and actual displayed pose only; existing source helpers unchanged.
    // M62 exact priest routes, actual displayed pose and current execution only.
    // Single-hit orbs retain their target-only FX initialization time and immutable source;
    // beam projections sample the displayed release focus at each existing contact.
    // M63 one exact beam route, actual current displayed purple-lens pose only.
    // M65 exact current displayed grille; never a fixed generic point or second transform.
    private Vector3 WinchGrilleSource(AttackRoute route,Vector3 fallback)
    {
        var actor=_sourceActor;
        if(route.Key!="enemy:enemy_winch_warden/hyper_beam"||route.Renderer!="EnemyContactFx"||route.Family!="enemy_beam"||actor==null||actor.DefinitionId!="enemy_winch_warden"||actor.Dead||!actor.AuthoredFramesEnabled)return fallback;
        var attack=actor.Style04MotionSample;var frame=actor.AuthoredFrameSample;
        if(!attack.Active||attack.ProfileId!=actor.DefinitionId||attack.MoveId!="hyper_beam"||attack.OwnerVersion!=actor.ExecutionVersion||!frame.Active||frame.MissingClip||frame.OwnerVersion!=actor.ExecutionVersion||frame.MotionVersion!=attack.Version||frame.ExecutionIndex!=attack.ExecutionIndex)return fallback;
        var sprite=actor.Sprite;var texture=sprite?.Texture;
        if(texture==null||texture.GetWidth()!=208||texture.GetHeight()!=160||!sprite!.Centered||sprite.FlipH||sprite.FlipV||sprite.Offset!=Vector2.Zero)return fallback;
        Vector2? grille=(frame.Clip,frame.FrameIndex) switch{
            ("rest",0)=>new Vector2(103f,90f),
            ("hurt",0)=>new Vector2(98f,90f),
            ("recovery",0)=>new Vector2(95f,96f),
            ("summonprepare",0)=>new Vector2(95f,90f),
            ("summonapplied",0)=>new Vector2(94f,92f),
            ("flailprepare",0)=>new Vector2(89f,97f),
            ("flailcontacta",0)=>new Vector2(98f,108f),
            ("flailrebound",0)=>new Vector2(104f,90f),
            ("flailcontactb",0)=>new Vector2(99f,94f),
            ("windprepare",0)=>new Vector2(89f,106f),
            ("windhold",0)=>new Vector2(86f,107f),
            ("beamprepare",0)=>new Vector2(92f,83f),
            ("beamrelease",0)=>new Vector2(95f,84f),
            ("rewind",0)=>new Vector2(99f,98f),
            ("idle",0)=>new Vector2(103f,90f),
            ("idle",1)=>new Vector2(100f,92f),
            ("block",0)=>new Vector2(86f,107f),
            _=>null
        };
        if(grille is not Vector2 pixel)return fallback;
        Vector3 local=new((pixel.X-texture.GetWidth()*.5f+sprite.Offset.X)*sprite.PixelSize,(texture.GetHeight()*.5f-pixel.Y+sprite.Offset.Y)*sprite.PixelSize,0);
        return ToLocal(sprite.GlobalTransform*local);
    }

    // M66 exact current displayed grille; never a fixed generic point or second transform.
    private Vector3 FurnaceFireSource(AttackRoute route,Vector3 fallback)
    {
        var actor=_sourceActor;
        if((route.Key!="enemy:enemy_ember_furnace/sear"&&route.Key!="enemy:enemy_ember_furnace/inferno")||route.Renderer!="FireContinuousFx"||route.Family!="fire_localburst"||actor==null||actor.DefinitionId!="enemy_ember_furnace"||actor.Dead||!actor.AuthoredFramesEnabled)return fallback;
        var attack=actor.Style04MotionSample;var frame=actor.AuthoredFrameSample;
        if(!attack.Active||attack.ProfileId!=actor.DefinitionId||(attack.MoveId!="sear"&&attack.MoveId!="inferno")||route.Key!="enemy:"+actor.DefinitionId+"/"+attack.MoveId||attack.OwnerVersion!=actor.ExecutionVersion||!frame.Active||frame.MissingClip||frame.OwnerVersion!=actor.ExecutionVersion||frame.MotionVersion!=attack.Version||frame.ExecutionIndex!=attack.ExecutionIndex)return fallback;
        var sprite=actor.Sprite;var texture=sprite?.Texture;
        if(texture==null||texture.GetWidth()!=224||texture.GetHeight()!=160||!sprite!.Centered||sprite.FlipH||sprite.FlipV||sprite.Offset!=Vector2.Zero)return fallback;
        Vector2? grille=(frame.Clip,frame.FrameIndex) switch{
            ("rest",0)=>new Vector2(58f,108f),
            ("hurt",0)=>new Vector2(57f,120f),
            ("recovery",0)=>new Vector2(55f,113f),
            ("stokeprepare",0)=>new Vector2(53f,115f),
            ("stokehold",0)=>new Vector2(50f,114f),
            ("devourprepare",0)=>new Vector2(57f,102f),
            ("devourcontact",0)=>new Vector2(50f,101f),
            ("devourrebound",0)=>new Vector2(56f,104f),
            ("searprepare",0)=>new Vector2(60f,111f),
            ("searcontact",0)=>new Vector2(51f,94f),
            ("tackleprepare",0)=>new Vector2(44f,117f),
            ("tacklecontacta",0)=>new Vector2(52f,104f),
            ("tacklerebound",0)=>new Vector2(51f,115f),
            ("tacklecontactb",0)=>new Vector2(58f,105f),
            ("inflameprepare",0)=>new Vector2(55f,114f),
            ("inflamehold",0)=>new Vector2(61f,111f),
            ("infernoprepare",0)=>new Vector2(55f,116f),
            ("infernocontact",0)=>new Vector2(40f,99f),
            ("infernorebound",0)=>new Vector2(51f,111f),
            ("idle",0)=>new Vector2(58f,108f),
            ("idle",1)=>new Vector2(55f,113f),
            ("block",0)=>new Vector2(50f,114f),
            _=>null
        };
        if(grille is not Vector2 pixel)return fallback;
        Vector3 local=new((pixel.X-texture.GetWidth()*.5f+sprite.Offset.X)*sprite.PixelSize,(texture.GetHeight()*.5f-pixel.Y+sprite.Offset.Y)*sprite.PixelSize,0);
        return ToLocal(sprite.GlobalTransform*local);
    }

    private Vector3 SealOrbLensSource(AttackRoute route,Vector3 fallback)
    {
        var actor=_sourceActor;
        if(route.Key!="enemy:enemy_seal_orb/beam"||route.Renderer!="EnemyContactFx"||route.Family!="enemy_beam"||actor==null||actor.DefinitionId!="enemy_seal_orb"||actor.Dead||!actor.AuthoredFramesEnabled)return fallback;
        var attack=actor.Style04MotionSample;var frame=actor.AuthoredFrameSample;
        if(!attack.Active||attack.ProfileId!=actor.DefinitionId||attack.MoveId!="beam"||attack.OwnerVersion!=actor.ExecutionVersion||!frame.Active||frame.MissingClip||frame.OwnerVersion!=actor.ExecutionVersion||frame.MotionVersion!=attack.Version||frame.ExecutionIndex!=attack.ExecutionIndex)return fallback;
        var sprite=actor.Sprite;var texture=sprite?.Texture;
        if(texture==null||texture.GetWidth()!=80||texture.GetHeight()!=64||!sprite!.Centered||sprite.FlipH||sprite.FlipV||sprite.Offset!=new Vector2(-4,0))return fallback;
        Vector2? lens=(frame.Clip,frame.FrameIndex) switch{
            ("beamprepare",0)=>new Vector2(24.0f,34.5f),
            ("beamrelease",0)=>new Vector2(24.5f,34.0f),
            ("guardhold",0)=>new Vector2(25.5f,36.0f),
            ("guardprepare",0)=>new Vector2(24.5f,37.5f),
            ("hurt",0)=>new Vector2(21.5f,34.0f),
            ("recovery",0)=>new Vector2(24.0f,34.5f),
            ("rest",0)=>new Vector2(23.0f,33.5f),
            ("sealapplied",0)=>new Vector2(25.0f,36.0f),
            ("sealprepare",0)=>new Vector2(23.5f,34.0f),
            ("idle",0)=>new Vector2(23.0f,33.5f),
            ("idle",1)=>new Vector2(24f,34.5f),
            _=>null
        };
        if(lens is not Vector2 pixel)return fallback;
        Vector3 local=new((pixel.X-texture.GetWidth()*.5f+sprite.Offset.X)*sprite.PixelSize,(texture.GetHeight()*.5f-pixel.Y+sprite.Offset.Y)*sprite.PixelSize,0);
        return ToLocal(sprite.GlobalTransform*local);
    }

    private Vector3 PriestFocusSource(AttackRoute route,Vector3 fallback)
    {
        var actor=_sourceActor;
        bool beam=route.Key is "enemy:enemy_kin_priest/beam" or "enemy:enemy_kin_priest/blood_beam";
        bool orb=route.Key is "enemy:enemy_kin_priest/orb_frail" or "enemy:enemy_kin_priest/orb_weak" or "enemy:enemy_kin_priest/judgment_orb";
        if(!(beam&&route.Renderer=="EnemyContactFx"&&route.Family=="enemy_beam")&&!(orb&&route.Renderer=="BoundedContactFx"&&route.Family=="minimal_neutral_contact"))return fallback;
        if(actor==null||actor.DefinitionId!="enemy_kin_priest"||actor.Dead||!actor.AuthoredFramesEnabled)return fallback;
        var attack=actor.Style04MotionSample;var frame=actor.AuthoredFrameSample;
        if(!attack.Active||attack.ProfileId!=actor.DefinitionId||route.Key!="enemy:enemy_kin_priest/"+attack.MoveId||attack.OwnerVersion!=actor.ExecutionVersion||!frame.Active||frame.MissingClip||frame.OwnerVersion!=actor.ExecutionVersion||frame.MotionVersion!=attack.Version||frame.ExecutionIndex!=attack.ExecutionIndex)return fallback;
        var sprite=actor.Sprite;var texture=sprite?.Texture;
        if(texture==null||texture.GetWidth()!=176||texture.GetHeight()!=144||!sprite!.Centered||sprite.FlipH||sprite.FlipV||sprite.Offset!=Vector2.Zero)return fallback;
        Vector2? focus=(frame.Clip,frame.FrameIndex) switch{
            ("rest",0)=>new Vector2(56.5f,52.5f),
            ("frailrelease",0)=>new Vector2(42f,53.5f),
            ("weakrelease",0)=>new Vector2(53.5f,69f),
            ("beamrelease",0)=>new Vector2(43f,59f),
            ("beamrebound",0)=>new Vector2(48f,59f),
            ("judgmentrelease",0)=>new Vector2(38.5f,73f),
            ("bloodrelease",0)=>new Vector2(37f,64f),
            ("bloodrebound",0)=>new Vector2(40f,64f),
            ("frailprepare",0)=>new Vector2(71f,62f),
            ("weakprepare",0)=>new Vector2(63f,46f),
            ("judgmentprepare",0)=>new Vector2(78f,34.5f),
            _=>null
        };
        if(focus is not Vector2 pixel)return fallback;
        Vector3 local=new((pixel.X-texture.GetWidth()*.5f+sprite.Offset.X)*sprite.PixelSize,(texture.GetHeight()*.5f-pixel.Y+sprite.Offset.Y)*sprite.PixelSize,0);
        return ToLocal(sprite.GlobalTransform*local);
    }

    private Vector3 CannonMuzzleSource(AttackRoute route, Vector3 fallback)
    {
        var actor=_sourceActor;
        if(route.Key is not ("enemy:enemy_crab_cannon/precision_beam" or "enemy:enemy_crab_cannon/laser")||route.Renderer!="EnemyContactFx"||route.Family!="enemy_beam"||actor==null||actor.DefinitionId!="enemy_crab_cannon"||actor.Dead||!actor.AuthoredFramesEnabled)return fallback;
        var attack=actor.Style04MotionSample;var frame=actor.AuthoredFrameSample;
        if(!attack.Active||attack.ProfileId!=actor.DefinitionId||route.Key!="enemy:enemy_crab_cannon/"+attack.MoveId||attack.OwnerVersion!=actor.ExecutionVersion||!frame.Active||frame.MissingClip||frame.OwnerVersion!=actor.ExecutionVersion||frame.MotionVersion!=attack.Version||frame.ExecutionIndex!=attack.ExecutionIndex)return fallback;
        var sprite=actor.Sprite;var texture=sprite?.Texture;
        if(texture==null||texture.GetWidth()!=288||texture.GetHeight()!=120||!sprite!.Centered||sprite.FlipH||sprite.FlipV||sprite.Offset!=new Vector2(-16,-8))return fallback;
        Vector2? muzzle=(frame.Clip,frame.FrameIndex) switch{
            ("rest",0)=>new Vector2(78f,57f),
            ("idle",0)=>new Vector2(78f,57f),
            ("hurt",0)=>new Vector2(131f,56f),
            ("recovery",0)=>new Vector2(74f,64f),
            ("reticleprepare",0)=>new Vector2(85f,59f),
            ("reticlecontact",0)=>new Vector2(62f,66f),
            ("precisionprepare",0)=>new Vector2(84f,68f),
            ("precisionrelease",0)=>new Vector2(80f,66f),
            ("laserprepare",0)=>new Vector2(119f,57f),
            ("laserrelease",0)=>new Vector2(57f,63f),
            ("chargeprepare",0)=>new Vector2(82f,66f),
            ("chargeapplied",0)=>new Vector2(54f,67f),
            ("recharge",0)=>new Vector2(77f,70f),
            ("idle",1)=>new Vector2(77f,73f),
            _=>null
        };
        if(muzzle is not Vector2 pixel)return fallback;
        Vector3 local=new((pixel.X-texture.GetWidth()*.5f+sprite.Offset.X)*sprite.PixelSize,(texture.GetHeight()*.5f-pixel.Y+sprite.Offset.Y)*sprite.PixelSize,0);
        return ToLocal(sprite.GlobalTransform*local);
    }

    private Vector3 SentryMuzzleSource(AttackRoute route, Vector3 fallback)
    {
        var actor = _sourceActor;
        if (route.Key != "enemy:enemy_elite_sentry/beam" || route.Renderer != "EnemyContactFx" || route.Family != "enemy_beam" ||
            actor == null || actor.DefinitionId != "enemy_elite_sentry" || actor.Dead || !actor.AuthoredFramesEnabled) return fallback;
        var attack = actor.Style04MotionSample; var frame = actor.AuthoredFrameSample;
        if (!attack.Active || attack.ProfileId != actor.DefinitionId || attack.MoveId != "beam" || attack.OwnerVersion != actor.ExecutionVersion ||
            !frame.Active || frame.MissingClip || frame.OwnerVersion != actor.ExecutionVersion || frame.MotionVersion != attack.Version || frame.ExecutionIndex != attack.ExecutionIndex) return fallback;
        var sprite = actor.Sprite; var texture = sprite?.Texture;
        if (texture == null || texture.GetWidth() != 144 || texture.GetHeight() != 112 || !sprite!.Centered || sprite.FlipH || sprite.FlipV || sprite.Offset != Vector2.Zero) return fallback;
        Vector2? muzzle = (frame.Clip, frame.FrameIndex) switch {
            ("rest", 0) => new Vector2(26f, 55f),
            ("idle", 0) => new Vector2(26f, 55f),
            ("idle", 1) => new Vector2(25f, 55f),
            ("hurt", 0) => new Vector2(41f, 44f),
            ("recovery", 0) => new Vector2(25f, 58f),
            ("boltprepare", 0) => new Vector2(25f, 47f),
            ("boltrelease", 0) => new Vector2(43f, 36f),
            ("beamprepare", 0) => new Vector2(25f, 64f),
            ("beamrelease", 0) => new Vector2(41f, 62f),
            ("guardprepare", 0) => new Vector2(43f, 77f),
            ("guardhold", 0) => new Vector2(44f, 80f),
            ("block", 0) => new Vector2(26f, 55f),
            _ => null
        };
        if (muzzle is not Vector2 pixel) return fallback;
        Vector3 local = new((pixel.X - texture.GetWidth() * .5f + sprite.Offset.X) * sprite.PixelSize,
            (texture.GetHeight() * .5f - pixel.Y + sprite.Offset.Y) * sprite.PixelSize, 0);
        return ToLocal(sprite.GlobalTransform * local);
    }

    private bool IsOwnedCenserNecroContact(GameEvent_DamageDealt hit)
    {
        if(!_eventOwners.TryGetValue(hit,out var owner))return false;
        var before=owner.Change.Previous?.Combat;var after=owner.Change.Current.Combat;
        var source=hit.PresentationSource;string? uid=source?.Uid;string? definition=source?.DefinitionId;
        int expectedDrain=NecroContactVfxProfiles.ExpectedDrainAmount(definition);
        if(before==null || after==null || source?.Kind!="necro" || string.IsNullOrEmpty(definition)
            || expectedDrain<=0 || string.IsNullOrEmpty(uid))return false;
        var card=before.Player.DrawPile.FirstOrDefault(c=>c.Uid==uid && c.Def.Id==definition);
        bool moved=card!=null && card.Def.Effects.Any(e=>e.Trigger=="onSelfExhausted" && e.Effect=="dealDamageDrain" && e.Amount==expectedDrain)
            && !before.Player.ExhaustPile.Any(c=>c.Uid==uid) && !after.Player.DrawPile.Any(c=>c.Uid==uid)
            && after.Player.ExhaustPile.Any(c=>c.Uid==uid && c.Def.Id==definition);
        bool censer=!string.IsNullOrEmpty(hit.SourceUid)
            && (before.Player.Permanents.Any(c=>c.Uid==hit.SourceUid && c.Def.Id=="black_thirst_censer")
                || after.Player.Permanents.Any(c=>c.Uid==hit.SourceUid && c.Def.Id=="black_thirst_censer"));
        bool milled=false,necro=false,found=false;
        foreach(var e in owner.Change.Events)
        {
            if(ReferenceEquals(e,hit)){found=true;break;}
            if(e is GameEvent_CardsMilled { Count: >0 } mill && mill.CardIds?.Contains(definition)==true)milled=true;
            if(milled && e is GameEvent_NecroFired fired && fired.CardId==definition)necro=true;
        }
        return NecroContactVfxProfiles.OwnsCenserDeckExhaustion(hit,moved,censer,found&&milled&&necro);
    }
    private AttackRoute NecroContactRoute(AttackRoute route,GameEvent_DamageDealt hit)
        =>NecroContactVfxProfiles.Select(route,hit,IsOwnedCenserNecroContact(hit));

    private AttackRoute LightningContactRoute(AttackRoute route,GameEvent_DamageDealt hit)
    {
        bool owned=false;
        if(_eventOwners.TryGetValue(hit,out var owner)
            && owner.Change.Current.LeaderId=="leader_izzet"
            && owner.Change.Command is RunCommand_Combat { Command: Command_PlayCard play }
            && hit.PresentationSource is {Kind:"card",DefinitionId:"blue_aether_burst"} source
            && !string.IsNullOrEmpty(source.Uid) && source.Uid==play.CardUid)
        {
            // Immutable owning command context, not mutable catalog leader state
            // or proximity to a resource event. AetherDischarged alone has no UID.
            owned=owner.Change.Previous?.Combat?.Player.Hand.Any(c=>c.Uid==play.CardUid && c.Def.Id=="blue_aether_burst")==true;
        }
        return LightningContactVfxProfiles.Select(route,hit,owned);
    }

    private bool IsOwnedConfirmedReactionContact(GameEvent_DamageDealt hit)
    {
        if(hit.PresentationSource is not {Kind:"reaction"} source
            || !ReactionContactVfxProfiles.SelectedDefinition(source.DefinitionId)
            || string.IsNullOrEmpty(source.Uid) || !_eventOwners.TryGetValue(hit,out var owner)
            || owner.Change.Command is not RunCommand_Combat {Command:Command_ConfirmReaction {Fire:true} confirm}
            || confirm.CardUid!=source.Uid)return false;
        var before=owner.Change.Previous?.Combat;var after=owner.Change.Current.Combat;
        if(before==null || after==null || before.ReactionMode!="set-confirm" || before.Phase!="awaiting-reaction"
            || before.PendingWindow is not {Stage:"post"} pending || pending.EnemyIndex!=hit.EnemyIndex)return false;
        var card=before.Player.SetCards.FirstOrDefault(c=>c.Uid==source.Uid && c.Def.Id==source.DefinitionId);
        var window=DeckRogue.Engine.Effects.WindowFromPending(before);
        if(card==null || card.Def.Type!="reaction" || window?.Kind!="attack"
            || !DeckRogue.Engine.Effects.IsTrapLive(before,card)
            || !DeckRogue.Engine.Effects.UsableSetCards(before,window).Any(c=>c.Uid==source.Uid)
            || after.Player.SetCards.Any(c=>c.Uid==source.Uid))return false;
        bool triggered=false,found=false;
        foreach(var e in owner.Change.Events)
        {
            if(ReferenceEquals(e,hit)){found=true;break;}
            if(e is GameEvent_ReactionTriggered trigger && trigger.CardId==source.DefinitionId)triggered=true;
        }
        // SourceUid is ambient context, NOT the selected reaction instance UID.
        // No final-discard assumption: normal continuation can reshuffle/draw it.
        return ReactionContactVfxProfiles.OwnsConfirmedInstance(hit,found&&triggered);
    }
    private AttackRoute ReactionContactRoute(AttackRoute route,GameEvent_DamageDealt hit)
        =>ReactionContactVfxProfiles.Select(route,hit,IsOwnedConfirmedReactionContact(hit));

    private bool IsOwnedInstalledPermanentContact(GameEvent_DamageDealt hit)
    {
        if(!_eventOwners.TryGetValue(hit,out var owner))return false;
        string? uid=hit.PresentationSource?.Uid;string? definition=hit.PresentationSource?.DefinitionId;
        if(string.IsNullOrEmpty(uid) || !PermanentContactVfxProfiles.SelectedDefinition(definition))return false;
        bool Installed(CardInstance p)=>p.Uid==uid && p.Def.Id==definition && p.Def.Type=="permanent"
            && p.Def.Retainer!=true && p.Def.Echo!=true && p.Def.Life==null;
        bool installed=owner.Change.Previous?.Combat?.Player.Permanents.Any(Installed)==true
            || owner.Change.Current.Combat?.Player.Permanents.Any(Installed)==true;
        return PermanentContactVfxProfiles.OriginalRoute(Routes.Resolve(hit))
            && PermanentContactVfxProfiles.OwnsInstalled(hit,installed);
    }
    // Historical neutral-only diagnostic remains unchanged in scope.
    private bool IsOwnedInstalledNeutralPermanentContact(GameEvent_DamageDealt hit)
        =>PermanentContactVfxProfiles.NeutralDefinition(hit.PresentationSource?.DefinitionId)
            && IsOwnedInstalledPermanentContact(hit);
    // Historical Candle-only diagnostic remains narrow, not a silent rename.
    private bool IsOwnedCandleContact(GameEvent_DamageDealt hit)
        =>hit.PresentationSource?.DefinitionId=="black_blood_candle" && IsOwnedInstalledNeutralPermanentContact(hit);
    private AttackRoute PermanentContactRoute(AttackRoute route,GameEvent_DamageDealt hit)
        =>PermanentContactVfxProfiles.Select(route,hit,IsOwnedInstalledPermanentContact(hit));

    private bool IsOwnedEnemyBluntContact(OrdinaryBeat b)
    {
        if(b.Beat.Route is not {Family:"enemy_blunt"} route
            || b.Beat.Event is not GameEvent_DamageDealt hit
            || b.Source==null || b.Target!=_hero || !_eventOwners.TryGetValue(hit,out var owner))return false;
        var selected=EnemyContactVfxProfiles.Select(route,hit,b.Source.DefinitionId,
            ProvenEnemyAttackMove(hit.PresentationEnemyAction,b.Source),owner.Change.Current.Combat?.EventLog);
        return selected.Family=="enemy_blunt" && EnemyContactVfxProfiles.SelectedRoute(selected);
    }

    private bool IsOwnedEnemyNeutralContact(OrdinaryBeat b)
    {
        if(b.Beat.Route is not {Family:"minimal_neutral_contact"} route
            || b.Beat.Event is not GameEvent_DamageDealt hit
            || b.Source==null || b.Target!=_hero || !_eventOwners.TryGetValue(hit,out var owner))return false;
        var selected=EnemyContactVfxProfiles.Select(route,hit,b.Source.DefinitionId,
            ProvenEnemyAttackMove(hit.PresentationEnemyAction,b.Source),owner.Change.Current.Combat?.EventLog);
        return selected.Family=="minimal_neutral_contact" && EnemyContactVfxProfiles.SelectedRoute(selected);
    }

    private AttackRoute EnemyContactRoute(AttackRoute route,GameEvent_DamageDealt hit)
    {
        if(_sourceActor==null || _targetActor!=_hero || !_eventOwners.TryGetValue(hit,out var owner))return route;
        return EnemyContactVfxProfiles.Select(route,hit,_sourceActor.DefinitionId,
            ProvenEnemyAttackMove(hit.PresentationEnemyAction,_sourceActor),owner.Change.Current.Combat?.EventLog);
    }

	private void StartEffect(AttackRoute route, EffectScenario scenario, Vector3 from, Vector3 to)
	{
        if(route.SourceKind=="card" && Hd2dAttackPrototype.HasAuthoredAudio(route.DefinitionId) && _ordinaryCallback?.Action is ActionPlayback onsetAction && !onsetAction.PrototypeOnsetPlayed)
        {
            onsetAction.PrototypeOnsetPlayed=true;
            _audio.PlayKey("Hd2dOnset."+Hd2dAttackPrototype.AudioCard(route.DefinitionId));
        }
        if(Hd2dAttackPrototype.UsesCastSource(route) && _hero!=null)
            from=_hero.PrototypeFireSourcePoint;
		from = FurnaceFireSource(route, WinchGrilleSource(route, SealOrbLensSource(route, PriestFocusSource(route, CannonMuzzleSource(route, SentryMuzzleSource(route, LostSoulEyeSource(route, from)))))));
		to = _targetActor?.Impact ?? to;
        if(Hd2dAttackPrototype.UsesWeaponSource(route) && _targetActor!=null && _hero!=null)
        {
            float bladeY=_hero.PrototypeAxeContactPoint.Y;
            to.Y=Math.Clamp(bladeY,_targetActor.SourceMotionFeet.Y+.08f*_targetActor.Height,
                _targetActor.SourceMotionFeet.Y+.42f*_targetActor.Height);
        }
        if(Hd2dAttackPrototype.SupportsRoute(route) && PhysicalVfxProfiles.ForRoute(route.Key) is PhysicalVfxProfile physical)
        {
            var current=_ordinaryCallback;
            bool transfer=physical.TargetPattern==PhysicalVfxTargetPattern.ConditionalTransfer;
            bool secondary=(physical.TargetPattern==PhysicalVfxTargetPattern.ResolvedSweep || transfer) && (current?.TargetOrdinal??0)>0;
            if(transfer && current?.Action is ActionPlayback transferAction)
            {
                // Freeze first receiving point before death/recoil removes its actor.
                // Only an already-authoritative later event may consume this origin.
                if(!secondary) transferAction.PrototypeTransferOrigin=to;
                else
                {
                    var first=transferAction.Beats.FirstOrDefault(x=>x.Direct && x.TargetOrdinal==0);
                    if(first?.Effect?.Native is AuthoredGeometryFx previous)
                        transferAction.PrototypeTransferOrigin=previous.PrototypeContactPoint;
                    if(transferAction.PrototypeTransferOrigin is Vector3 origin) from=origin;
                }
            }
            // Non-damaging outcomes already use PresentationTimeline's system:outcome
            // route. Keep its guard/nullified semantics instead of a second outcome path.
            scenario=scenario with { PrototypeRemoteStrike=(current?.Action?.PrototypeRemoteStrike??false)||secondary,
                SuppressSourceGesture=secondary,PrototypeFixedSource=transfer&&secondary };
        }
        if(Hd2dAttackPrototype.SupportsRoute(route) && ElementalVfxProfiles.ForRoute(route.Key)?.TargetSpacing>0)
        {
            var current=_ordinaryCallback;
            // A blocked/dead first receiver must not steal the only gather.
            // Select the first actual positive card route in this volley.
            var first=current?.Action.Beats.FirstOrDefault(b=>b.Direct && b.Volley==current.Volley && Hd2dAttackPrototype.SupportsRoute(b.Beat.Route));
            scenario=scenario with { SuppressSourceGesture=first!=null && !ReferenceEquals(first,current) };
            if(ElementalVfxProfiles.ForRoute(route.Key)?.Family==ElementalVfxFamily.WaterWide
                && current!=null && ReferenceEquals(first,current))
            {
                var points=new List<Vector3>();var offsets=new List<float>();
                // Include actual blocked attempts in the shared travel path, but
                // their existing system-outcome route still owns guard feedback.
                // Missing/dead recipients never acquire fabricated contacts.
                foreach(var b in current.Action.Beats.Where(b=>b.Direct && b.Volley==current.Volley).OrderBy(b=>b.ContactAt))
                {
                    if(b.Beat.Event is not GameEvent_DamageDealt h || h.Source!="player"
                        || h.PresentationSource?.Kind!="card" || h.PresentationSource.DefinitionId!=route.DefinitionId
                        || h.PresentationReceipt?.Target!="enemy")continue;
                    var receiver=Enemy(h.PresentationReceipt.EnemyIndex);if(receiver==null)continue;
                    points.Add(receiver.SourceMotionFeet);
                    offsets.Add((float)(b.ContactAt-current.ContactAt));
                }
                scenario=scenario with { PrototypeResolvedPath=points.ToArray(),PrototypeResolvedOffsets=offsets.ToArray() };
            }
        }
		_fx.Clear();
		AuthoredGeometryFx native = _native;
		Vector3 target = to;
		Vector3 feet = _targetActor?.Home ?? (to - Vector3.Up);
		float visibleHeight = _targetActor?.Height ?? 1.5f;
		Basis globalBasis = _world.Camera.GlobalBasis;
		OrdinaryBeat? ordinaryCallback = _ordinaryCallback;
		if (!native.Begin(route, scenario, from, target, feet, visibleHeight, globalBasis, (ordinaryCallback != null && ordinaryCallback.Direct) ? _ordinaryCallback.Volley : 0))
		{
			_fx.Begin(route, scenario, from, to);
		}
        else native.ConfigurePrototypeViewport(_world.Camera);
	}

	private void SyncLedgerActor(int target)
	{
		GameState state = _latestRun?.Combat;
		if ((object)state != null && _ledger.TryGet(state, target, out var v))
		{
			CombatPresentationVitals combatPresentationVitals = new CombatPresentationVitals
			{
				Hp = (int)v.Hp,
				MaxHp = (int)v.MaxHp,
				Block = (int)v.Block,
				IceBlock = ((target < 0) ? new int?((int)v.IceBlock) : ((int?)null))
			};
			CombatPresentationVitals v2 = combatPresentationVitals;
			_lastPresentedVitals = combatPresentationVitals;
			SetDisplayedVitals(target, v2);
		}
	}

	private void SetDisplayedVitals(int target, CombatPresentationVitals v)
	{
		((target < 0) ? _hero : Enemy(target))?.SetVitals(v.Hp, v.MaxHp, v.Block);
		if ((object)_displayState != null)
		{
			if (target < 0)
			{
				_displayState = _displayState with
				{
					Player = _displayState.Player with
					{
						Hp = v.Hp,
						MaxHp = v.MaxHp,
						Block = v.Block,
						IceBlock = v.IceBlock.GetValueOrDefault()
					}
				};
			}
			else if (target < _displayState.Enemies.Count)
			{
				EnemyState[] enemies = _displayState.Enemies.ToArray();
				enemies[target] = enemies[target]with
				{
					Hp = v.Hp,
					MaxHp = v.MaxHp,
					Block = v.Block
				};
				_displayState = _displayState with
				{
					Enemies = enemies
				};
			}
		}
	}

	private void PresentPopulation(GameEvent e)
	{
		GameState old = _displayState;
		if ((object)old == null)
		{
			return;
		}
		GameState final = EventRun?.Combat;
		if ((object)final == null)
		{
			return;
		}
		List<EnemyState> enemies = old.Enemies.ToList();
		List<CardInstance> perms = old.Player.Permanents.ToList();
		GameEvent gameEvent2 = e;
		GameEvent_PermanentPlayed gameEvent_PermanentPlayed = gameEvent2 as GameEvent_PermanentPlayed;
		if ((object)gameEvent_PermanentPlayed == null)
		{
			if (!(gameEvent2 is GameEvent_RetainerCopied p))
			{
				if (!(gameEvent2 is GameEvent_RetainerExpired p2))
				{
					if (!(gameEvent2 is GameEvent_RetainerSacrificed p3))
					{
						if (!(gameEvent2 is GameEvent_PermanentDismissed p4))
						{
							if (!(gameEvent2 is GameEvent_TokenDestroyed p5))
							{
								if (!(gameEvent2 is GameEvent_EnemySummoned p6))
								{
									if (!(gameEvent2 is GameEvent_EnemySplit p7))
									{
										if (gameEvent2 is GameEvent_EnemyHatched { EnemyIndex: >=0 } p8 && p8.EnemyIndex < enemies.Count && enemies[p8.EnemyIndex].EnemyId == p8.FromId && p8.EnemyIndex < final.Enemies.Count && final.Enemies[p8.EnemyIndex].EnemyId == p8.IntoId)
										{
											EnemyState next = final.Enemies[p8.EnemyIndex];
											enemies[p8.EnemyIndex] = next with
											{
												Hp = next.MaxHp,
												Block = 0,
												Intent = null
											};
										}
									}
									else
									{
										AddEnemies(p7.Into, p7.Count);
									}
								}
								else
								{
									AddEnemies(p6.Into, p6.Count);
								}
							}
							else
							{
								RemovePermanent(p5.CardId, p5.Uid);
							}
						}
						else
						{
							RemovePermanent(p4.CardId, p4.Uid);
						}
					}
					else
					{
						RemovePermanent(p3.CardId, p3.Uid);
					}
				}
				else
				{
					RemovePermanent(p2.CardId, p2.Uid);
				}
			}
			else
			{
				AddPermanent(p.CardId, p.Uid);
				_copiedEnter.Enqueue(p.Uid);
			}
		}
		else if (_copiedEnter.Count > 0 && perms.Any((CardInstance x) => x.Uid == _copiedEnter.Peek() && x.Def.Id == gameEvent_PermanentPlayed.CardId))
		{
			_copiedEnter.Dequeue();
		}
		else
		{
			AddPermanent(gameEvent_PermanentPlayed.CardId);
		}
		SyncActors(old with
		{
			Enemies = enemies,
			Player = old.Player with
			{
				Permanents = perms
			}
		}, EventRun.LeaderId);
		void AddEnemies(string id, int count)
		{
			for (int n = 0; n < count; n++)
			{
				if (enemies.Count >= final.Enemies.Count)
				{
					break;
				}
				EnemyState next2 = final.Enemies[enemies.Count];
				if (next2.EnemyId != id)
				{
					break;
				}
				enemies.Add(next2 with
				{
					Hp = next2.MaxHp,
					Block = 0,
					Intent = null
				});
			}
		}
		void AddPermanent(string id, string? uid = null)
		{
			if (uid == null || !perms.Any((CardInstance cardInstance) => cardInstance.Uid == uid))
			{
				CardInstance next2 = final.Player.Permanents.FirstOrDefault((CardInstance cardInstance) => cardInstance.Def.Id == id && ((uid != null) ? (cardInstance.Uid == uid) : (!_bornUids.Contains(cardInstance.Uid))));
				if ((object)next2 == null && uid != null)
				{
					CardDef def = _session?.Catalog.Cards.FirstOrDefault((CardDef c) => c.Id == id);
					if ((object)def != null)
					{
						next2 = new CardInstance
						{
							Uid = uid,
							Def = def,
							Token = true
						};
					}
				}
				if ((object)next2 != null)
				{
					_bornUids.Add(next2.Uid);
					perms.Add(next2);
				}
			}
		}
		void RemovePermanent(string id, string? uid)
		{
			int i = perms.FindIndex((CardInstance cardInstance) => (uid != null) ? (cardInstance.Uid == uid) : (cardInstance.Def.Id == id));
			if (i >= 0)
			{
				string key = perms[i].Uid;
				_sourceDollSeats.Kill(key);
				if (_retainers.Remove(key, out var retired))
				{
					_actorRoot.RemoveChild(retired);
					_retiredRoot.AddChild(retired, forceReadableName: false, InternalMode.Disabled);
					StartDeath(retired, retired: true);
					BattleCuePool cues = _cues;
					string text;
					if (e is GameEvent_RetainerExpired)
					{
						text = "期限切れ";
					}
					else
					{
						text = ((e is GameEvent_RetainerSacrificed) ? "捧げた" : "灯が消えた");
					}
					cues.Show(text, retired.Contact + Vector3.Up * 0.6f, new Color("c4beb2"));
				}
				perms.RemoveAt(i);
			}
		}
	}

	private void ApplyReceipt(CombatPresentationReceipt? receipt)
	{
		if ((object)receipt == null)
		{
			return;
		}
		bool flag2 = receipt.Version != 1 || (object)receipt.Before == null || (object)receipt.After == null;
		if (!flag2)
		{
			string target = receipt.Target;
			flag2 = (!(target == "player") && !(target == "enemy")) || 1 == 0;
		}
		if (!flag2)
		{
			if (!(receipt.Target == "enemy"))
			{
				goto IL_00c1;
			}
			int? enemyIndex = receipt.EnemyIndex;
			if (enemyIndex.HasValue)
			{
				int ri = enemyIndex.GetValueOrDefault();
				BattleActor matching = Enemy(ri);
				if (matching != null && (string.IsNullOrEmpty(receipt.EnemyId) || !(matching.DefinitionId != receipt.EnemyId)))
				{
					goto IL_00c1;
				}
			}
		}
		RejectedReceiptCount++;
		return;
		IL_00c1:
		_lastPresentedVitals = receipt.After;
		SetDisplayedVitals((receipt.Target == "player") ? (-1) : receipt.EnemyIndex.Value, receipt.After);
	}

	private void ShowReceipt(string text, Vector3 position, Color color)
	{
		_cues.Show(text, position + Vector3.Up * 0.4f, color);
	}

	private void FinishBeat(PresentationBeat beat)
	{
		_session?.RecordFreezeEvent("BattleStage.FinishBeat");
		_fx.Clear();
		_native.Clear();
		_world.SetForestAttackLights(null);
		_sourceActor = null;
		_targetActor = null;
	}

	private void FinishBatch()
	{
		if (_resetting)
		{
			return;
		}
		_fx.Clear();
		_native.Clear();
		_world.SetForestAttackLights(null);
		_receipt.Visible = false;
		CommandOwner owner = _fenceCommand;
		if (owner != null)
		{
			owner.Finished = true;
			if (owner.Ledger != null)
			{
				_ledger.Finish(owner.Ledger);
			}
			CombatActionQueue.ActionToken token = owner.FenceToken;
			if (token != null)
			{
				_actionQueue.ContactComplete(token, -1, _clock, _presentationFrame);
			}
			_fenceCommand = null;
		}
		TryFinishPresentations();
	}

	public override void _Process(double delta)
	{
		if (PresentationFaulted) return;
		int generation = _actionQueue.Generation;
		try
		{
			if (!PlaybackPaused)
			{
				SampleQuality((float)delta, _interfaceRoute == "combat");
				AdvancePresentation((float)delta * PlaybackSpeed);
			}
		}
		catch (Exception ex)
		{
			RecoverPresentationFault(ex, "BattleStage.Process", generation);
		}
	}

	private void AdvanceClock(float delta)
	{
		_clock += delta;
		_cues.Sample(_clock);
		SamplePhaseLayer();
		SampleSourceCombatText();
	}

	public void AdvancePresentation(float delta)
	{
		if (PresentationFaulted) return;
		int generation = _actionQueue.Generation;
		try { AdvancePresentationCore(delta); }
		catch (Exception ex) { RecoverPresentationFault(ex, _faultBoundary, generation); }
	}

	private void AdvancePresentationCore(float delta)
	{
		_faultBoundary = "BattleStage.Advance";
		EnsureBuilt();
		if (!float.IsFinite(delta) || delta <= 0f)
		{
			return;
		}
		_presentationFrame++;
		_lastHostDelta = delta;
		delta = AdvanceFinishClock(delta);
		if (_timeline.Busy)
		{
			_timeline.Advance(delta);
		}
		else
		{
			AdvanceClock(delta);
		}
		_faultBoundary = "BattleStage.QueueAdvance";
		_actionQueue.Advance(_clock, _presentationFrame);
		AdvanceSourceRetainerAbsences();
		_faultBoundary = "BattleStage.MotionSample";
		BattleActor[] array = AllPresentationActors().ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SampleSourceMotion(_clock, delta, _presentationFrame);
		}
		_faultBoundary = "BattleStage.OrdinaryContact";
		ProcessOrdinaryContacts(delta);
        RefreshTorrentReleaseHolds();
        // Exact front-strike weapon traces consume the rendered pose, not the prior
        // frame's Sprite transform. Update only those source actors here, once;
        // the normal visual loop below skips them (no duplicate pose clock advance).
        var weaponSourceActors = new HashSet<BattleActor>();
        if(Hd2dAttackPrototype.Enabled)
        {
            foreach(ActionPlayback action in _ordinaryActions)
                foreach(OrdinaryBeat beat in action.Beats)
                    if(!beat.Rejected && Hd2dAttackPrototype.UsesWeaponSource(beat.Beat.Route) && beat.Effect?.Native.IsActive==true
                        && beat.Action.Actor is BattleActor sourceActor && sourceActor.PrototypeStrikeUsesWeaponTrace)
                        weaponSourceActors.Add(sourceActor);
            foreach(BattleActor sourceActor in weaponSourceActors)
                sourceActor.UpdateVisual(_clock,-1f,-1f,guarding:false,_presentationFrame,delta);
        }
		_faultBoundary = "BattleStage.EffectSample";
		AdvanceOrdinaryEffects();
		if ((object)_timeline.Active?.Route != null && !_rejectBeat)
		{
			float t = _timeline.Time - _effectStartedAt + _effectOffset;
			if (_native.IsActive)
			{
				_native.Sample(t);
			}
			else if (_fx.Visible)
			{
				_fx.Sample(t);
			}
		}
		_faultBoundary = "BattleStage.Lighting";
		RefreshForestLighting();
		_faultBoundary = "BattleStage.ActorVisual";
		array = AllPresentationActors().ToArray();
		foreach (BattleActor actor in array)
		{
			AdvanceDeath(actor);
		}
		if (_style04RosterLayoutDirty)
		{
			_style04RosterLayoutDirty = false;
			RefreshActorLayout();
		}
		foreach (BattleActor actor in array)
		{
			BindActorLights(actor);
            if(!weaponSourceActors.Contains(actor))
			    actor.UpdateVisual(_clock, -1f, -1f, guarding: false, _presentationFrame, delta);
		}
		_faultBoundary = "BattleStage.FinishPresentation";
		TryFinishPresentations();
	}

	public void CancelPresentation(bool applyLatest = true)
	{
		_session?.RecordFreezeEvent("BattleStage.CancelPresentation.begin");
		_enemyBlockExpiryPending = false;
		_blockBoundaryCommand = false;
		_resetting = true;
		CancelCommandQueue();
		CancelPhaseLayer();
		InputLocked = false;
		_incomingEvent = null;
		_incomingAmount = null;
		foreach (BattleActor item in AllPresentationActors())
		{
			item.CancelSourceMotion(_clock);
		}
		_timeline.Reset();
		_fx?.Clear();
		_native?.Clear();
		_world?.SetForestAttackLights(null);
		_cues?.Clear();
		CancelSourceCombatText();
		ClearRetired();
		_deaths.Clear();
		_ledger.Clear();
		_receipts.Clear();
		_audio?.StopEffects();
		if (_receipt != null)
		{
			_receipt.Visible = false;
		}
		_sourceActor = null;
		_targetActor = null;
		IsPresenting = false;
		_session?.ClearPresentation();
		_session?.SetPresentationBusy(busy: false);
		_resetting = false;
		_session?.RecordFreezeEvent("BattleStage.CancelPresentation.end");
		if (applyLatest)
		{
			RunState run = _latestRun;
			if ((object)run != null)
			{
				SetRunState(run);
				PresentationCompleted?.Invoke();
			}
		}
	}

	private void StartDeath(BattleActor? actor, bool retired = false, bool fled = false)
	{
		if (actor != null && !_deaths.ContainsKey(actor))
		{
			actor.SetDead(dead: true);
			actor.SetDissolve(0f);
			actor.Flash(_clock, retired ? 0.18f : 0.22f);
			float duration;
			if (fled)
			{
				duration = 0.55f;
			}
			else if (retired)
			{
				duration = 0.5f;
			}
			else
			{
				duration = ((actor.SpiritDeathTextureHeight >= 96) ? 0.95f : 0.62f);
			}
			Dictionary<BattleActor, DeathState> deaths = _deaths;
			BattleActor? key = actor;
			float clock = _clock;
			float delay;
			if (fled)
			{
				delay = 0f;
			}
			else
			{
				delay = (retired ? 0.1f : 0.12f);
			}
			deaths[key] = new DeathState(clock, delay, duration, retired, fled);
		}
	}

	private void AdvanceDeath(BattleActor actor)
	{
		if (_deaths.TryGetValue(actor, out var d))
		{
			float p = Math.Clamp((_clock - d.Started - d.Delay) / d.Duration, 0f, 1f);
			bool justDisappeared = p >= 1f && actor.DissolveAmount < 1f;
			actor.SetDissolve(d.Fled ? (p * p) : p);
			if (justDisappeared && actor.EnemyIndex >= 0 && _enemies.Values.Any(a => a.Style04StaticSprite))
				_style04RosterLayoutDirty = true;
			if (p >= 1f && d.Retired)
			{
				_deaths.Remove(actor);
				_retiredRoot.RemoveChild(actor);
				actor.QueueFree();
			}
		}
	}

	private void ClearRetired()
	{
		if (_retiredRoot == null)
		{
			return;
		}
		foreach (Node child in _retiredRoot.GetChildren())
		{
			_retiredRoot.RemoveChild(child);
			child.QueueFree();
		}
	}

	public (Vector2 Head, Vector2 Feet)? ProjectEnemy(int index)
	{
		if (!_enemies.TryGetValue(index, out var actor) || _world.Camera == null)
		{
			return null;
		}
		return (_world.Camera.UnprojectPosition(actor.Home + _world.Camera.GlobalBasis.Y * actor.Height), _world.Camera.UnprojectPosition(actor.Home));
	}


    private readonly Dictionary<Texture2D, Rect2I> _style04OpaqueFootprints = new();
    // Stable-home opaque bounds. Read each static texture once; transparent padding
    // is available to labels instead of being mistaken for visible anatomy.
    public Rect2? ProjectEnemyFootprint(int index)
    {
        var actor = index == -1 ? _hero : (_enemies.TryGetValue(index, out var enemy) ? enemy : null);
        if (actor == null || _world.Camera == null || actor.Sprite.Texture == null) return null;
        // Static HUD/layout footprint always follows the original source sprite.
        // Drawn-frame envelopes are used separately for motion clearance below.
        var texture = actor.RosterLayoutTexture;
        float leftOffset = -texture.GetWidth() * actor.Sprite.PixelSize * .5f;
        float rightOffset = -leftOffset;
        if (actor.Style04StaticSprite)
        {
            if (!_style04OpaqueFootprints.TryGetValue(texture, out var opaque))
            {
                using var pixels = texture.GetImage();
                opaque = pixels.GetUsedRect();
                _style04OpaqueFootprints.Add(texture, opaque);
            }
            leftOffset = (opaque.Position.X - texture.GetWidth() * .5f) * actor.Sprite.PixelSize;
            rightOffset = (opaque.End.X - texture.GetWidth() * .5f) * actor.Sprite.PixelSize;
        }
        var c = _world.Camera;
        Vector2 left = c.UnprojectPosition(actor.Home + c.GlobalBasis.X * leftOffset);
        Vector2 right = c.UnprojectPosition(actor.Home + c.GlobalBasis.X * rightOffset);
        Vector2 top = c.UnprojectPosition(actor.Home + c.GlobalBasis.Y * actor.Height);
        Vector2 foot = c.UnprojectPosition(actor.Home);
        return new Rect2(Math.Min(left.X, right.X), Math.Min(top.Y, foot.Y), Math.Abs(right.X - left.X), Math.Abs(foot.Y - top.Y));
    }

    // Fixed-home authored envelope with the actual padded sprite offset and basis.
    // Roster fitting remains original static source; travel alone reserves all drawn poses.
    private Rect2? ProjectAuthoredBounds(BattleActor actor, bool motionEnvelope)
    {
        if (_world.Camera == null || !actor.TryAuthoredOpaqueBounds(motionEnvelope, out var opaque)) return null;
        var sprite=actor.Sprite;var texture=sprite.Texture;var camera=_world.Camera;
        Vector3 homeShift=actor.GetParent<Node3D>().ToGlobal(actor.Home)-actor.GlobalPosition;
        Vector2 min=new(float.PositiveInfinity,float.PositiveInfinity),max=new(float.NegativeInfinity,float.NegativeInfinity);
        foreach(float x in new[]{(float)opaque.Position.X,(float)opaque.End.X})
        foreach(float y in new[]{(float)opaque.Position.Y,(float)opaque.End.Y})
        {
            Vector3 local=new((x-texture.GetWidth()*.5f+sprite.Offset.X)*sprite.PixelSize,(texture.GetHeight()*.5f-y+sprite.Offset.Y)*sprite.PixelSize,0);
            Vector2 projected=camera.UnprojectPosition(sprite.GlobalTransform*local+homeShift);
            min=new Vector2(Math.Min(min.X,projected.X),Math.Min(min.Y,projected.Y));max=new Vector2(Math.Max(max.X,projected.X),Math.Max(max.Y,projected.Y));
        }
        return new Rect2(min,max-min);
    }

	public int? FindEnemyAt(Vector2 viewportPoint)
	{
		if (InputLocked || _interfaceRoute != "combat" || _latestRun?.Phase != "combat")
		{
			return null;
		}
		BattleActor best = null;
		float distance = 7225f;
		foreach (BattleActor actor in _enemies.Values)
		{
			if (!actor.Dead && LogicalEnemyAlive(actor.EnemyIndex))
			{
				float d = _world.Camera.UnprojectPosition(actor.Contact).DistanceSquaredTo(viewportPoint);
				if (d < distance)
				{
					best = actor;
					distance = d;
				}
			}
		}
		return best?.EnemyIndex;
	}

	private void CacheWorldLights()
	{
		_worldLights.Clear();
		// Geometry-only stage crest has its own native layer mask. Do not feed it
        // back into unshaded actor material lighting as an ordinary point light.
        _worldLights.AddRange(_world.FindChildren("*", "OmniLight3D", recursive: true, owned: false).OfType<OmniLight3D>().Where(light=>!light.HasMeta("stage_attack_crest_only")));
	}

	private void RefreshForestLighting(AuthoredGeometryFx? probeFx = null)
	{
		_actorLights.Clear();
		AppendAttackLights(_actorLights, probeFx, true);
		_world.SetForestAttackLights(_actorLights, (SourceCastLightCount > 0) ? 3 : 2);
        AuthoredGeometryFx? focus=probeFx??_native;
        if(probeFx==null) foreach(var beat in _ordinaryActions.SelectMany(a=>a.Beats))
            if(beat.Effect?.Native is AuthoredGeometryFx fx && fx.PrototypeStageFocus>(focus?.PrototypeStageFocus??0))focus=fx;
        _world.SetAttackStaging(focus?.PrototypeStageFocus??0,focus?.PrototypeFocusPosition,focus?.PrototypeWarmCrest??0,focus?.PrototypeSpectacleCrest??0);
	}

	public void RefreshActorLighting(AuthoredGeometryFx? probeFx = null)
	{
		EnsureBuilt();
		RefreshForestLighting(probeFx);
		foreach (BattleActor actor in _actorRoot.GetChildren().OfType<BattleActor>().Concat(_retiredRoot.GetChildren().OfType<BattleActor>()))
		{
			BindActorLights(actor, probeFx);
		}
	}

	private void BindActorLights(BattleActor actor, AuthoredGeometryFx? probeFx = null)
	{
		_actorLights.Clear();
		AppendAttackLights(_actorLights, probeFx);
		int slots = 8 - _actorLights.Count;
		for (int pick = 0; pick < slots; pick++)
		{
			OmniLight3D best = null;
			float distance = float.PositiveInfinity;
			foreach (OmniLight3D light in _worldLights)
			{
				if (!GodotObject.IsInstanceValid(light) || !light.Visible || light.LightEnergy <= 0f)
				{
					continue;
				}
				bool used = false;
				foreach (ActorMaterialBinding.PointLight v in _actorLights)
				{
					if (!v.IsAttack && v.Position == light.GlobalPosition)
					{
						used = true;
						break;
					}
				}
				if (!used)
				{
					float d = light.GlobalPosition.DistanceSquaredTo(actor.Contact);
					if (d < distance)
					{
						best = light;
						distance = d;
					}
				}
			}
			if (best == null)
			{
				break;
			}
			Color color = best.LightColor.SrgbToLinear();
			_actorLights.Add(new ActorMaterialBinding.PointLight(best.GlobalPosition, best.OmniRange, new Vector3(color.R, color.G, color.B) * best.LightEnergy, IsAttack: false));
		}
		actor.SetSourceLights(_actorLights);
	}

	private void ClearActors()
	{
		_sourceDollSeats.Reset();
		ClearRetired();
		_deaths.Clear();
		if (_actorRoot == null)
		{
			return;
		}
		foreach (Node child in _actorRoot.GetChildren())
		{
			_actorRoot.RemoveChild(child);
			child.QueueFree();
		}
		_hero = null;
		_enemies.Clear();
		_retainers.Clear();
	}

	public override void _UnhandledInput(InputEvent input)
	{
		if (InputLocked || _interfaceRoute != "combat" || _latestRun?.Phase != "combat")
		{
			return;
		}
		Vector2? pos;
		if (input is InputEventMouseButton { Pressed: not false } mouse && mouse.ButtonIndex == MouseButton.Left)
		{
			pos = mouse.Position;
		}
		else
		{
			pos = ((input is InputEventScreenTouch { Pressed: not false } touch) ? new Vector2?(touch.Position) : ((Vector2?)null));
		}
		if (!pos.HasValue)
		{
			return;
		}
		(BattleActor, Vector2) best = (from battleActor in _enemies.Values
			where !battleActor.Dead && LogicalEnemyAlive(battleActor.EnemyIndex)
			select (actor: battleActor, point: _world.Camera.UnprojectPosition(battleActor.Contact))).OrderBy(((BattleActor actor, Vector2 point) x) =>
		{
			(BattleActor, Vector2) tuple = x;
			return tuple.Item2.DistanceSquaredTo(pos.Value);
		}).FirstOrDefault();
		if (best.Item1 == null || !(best.Item2.DistanceTo(pos.Value) < 85f))
		{
			return;
		}
		foreach (BattleActor value in _enemies.Values)
		{
			value.Selected = value == best.Item1;
		}
		TargetSelected?.Invoke(best.Item1.EnemyIndex);
		GetViewport().SetInputAsHandled();
	}

	public override void _ExitTree()
	{
		if (_session != null)
		{
			_session.StateChanged -= Present;
			_session.RunCleared -= ClearRun;
		}
		CancelPresentation(applyLatest: false);
	}

	public void SetCueOccluders(Func<IReadOnlyList<Rect2>>? provider)
	{
		_cueOccluders = provider;
	}

	private void ShowSemanticCue(SemanticEventCue cue, Vector3 origin, BattleActor? actor)
	{
		SemanticPlacement placement = new SemanticPlacement
		{
			Id = ++_semanticPlacementId,
			Cue = cue,
			Actor = actor,
			Anchor = origin
		};
		_lastSemanticPlacement = placement;
		_cues.Show(cue.Text, origin, new Color(cue.Color), null, cue.Lifetime, 32, 0.009f, (Label3D label, Vector3 desired) => PlaceSemanticCue(placement, label, desired));
	}

	private Vector3 PlaceSemanticCue(SemanticPlacement placement, Label3D label, Vector3 localOrigin)
	{
		placement.Label = label;
		label.SetMeta("cue_placement_id", placement.Id);
		Camera3D camera = _world.Camera;
		if (!camera.IsInsideTree())
		{
			return localOrigin;
		}
		Vector3 origin = _cues.ToGlobal(localOrigin);
		Rect2 bounds = (placement.Preferred = ProjectCueBounds(label, origin));
		Rect2 viewport = GetViewport().GetVisibleRect();
		Rect2[] blockers = (_cueOccluders?.Invoke() ?? Array.Empty<Rect2>()).Where((Rect2 r) => r.Size.X > 0f && r.Size.Y > 0f).ToArray();
		Rect2 fitted = FitCueRectangle(bounds, viewport, blockers, out var found);
		placement.Found = found;
		Vector2 offset = fitted.Position - bounds.Position;
		if (offset.LengthSquared() < 0.0001f)
		{
			return localOrigin;
		}
		float depth = 0f - (camera.GlobalTransform.AffineInverse() * origin).Z;
		return _cues.ToLocal(camera.ProjectPosition(camera.UnprojectPosition(origin) + offset, depth));
	}

	private Rect2 ProjectCueBounds(Label3D label, Vector3 origin)
	{
		Vector2 min = -label.Font.GetMultilineStringSize(label.Text, HorizontalAlignment.Center, -1f, label.FontSize, -1, TextServer.LineBreakFlag.Mandatory | TextServer.LineBreakFlag.WordBound, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal) * label.PixelSize * 0.5f;
		Vector2 max = -min;
		TriangleMesh mesh = label.GenerateTriangleMesh();
		if (mesh != null)
		{
			Vector3[] faces = mesh.GetFaces();
			for (int i = 0; i < faces.Length; i++)
			{
				Vector3 vertex = faces[i];
				min = min.Min(new Vector2(vertex.X, vertex.Y));
				max = max.Max(new Vector2(vertex.X, vertex.Y));
			}
		}
		float outline = (float)(label.OutlineSize + 1) * label.PixelSize;
		min -= Vector2.One * outline;
		max += Vector2.One * outline;
		Camera3D camera = _world.Camera;
		Basis basis = camera.GlobalBasis.Orthonormalized();
		Vector2[] array = new Vector2[4]
		{
			new Vector2(min.X, min.Y),
			new Vector2(min.X, max.Y),
			new Vector2(max.X, min.Y),
			new Vector2(max.X, max.Y)
		};
		Vector2 screenMin = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
		Vector2 screenMax = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
		Vector2[] array2 = array;
		for (int j = 0; j < array2.Length; j++)
		{
			Vector2 p = array2[j];
			Vector2 screen = camera.UnprojectPosition(origin + basis.X * p.X + basis.Y * p.Y);
			screenMin = screenMin.Min(screen);
			screenMax = screenMax.Max(screen);
		}
		return new Rect2(screenMin, screenMax - screenMin);
	}

	public static Rect2 FitCueRectangle(Rect2 desired, Rect2 viewport, IReadOnlyList<Rect2> painted, out bool found)
	{
		Rect2 allowed = viewport.Grow(-4f);
		Vector2 size = desired.Size;
		if (size.X > allowed.Size.X || size.Y > allowed.Size.Y)
		{
			found = false;
			return desired;
		}
		float lowX = allowed.Position.X;
		float highX = allowed.End.X - size.X;
		float lowY = allowed.Position.Y;
		float highY = allowed.End.Y - size.Y;
		HashSet<float> xs = new HashSet<float>
		{
			ClampX(desired.Position.X),
			lowX,
			highX
		};
		HashSet<float> ys = new HashSet<float>
		{
			ClampY(desired.Position.Y),
			lowY,
			highY
		};
		Rect2[] blockers = painted.Where((Rect2 r) =>
		{
			Rect2 rect = r;
			if (rect.Size.X > 0f)
			{
				rect = r;
				return rect.Size.Y > 0f;
			}
			return false;
		}).Select((Rect2 r) =>
		{
			Rect2 rect = r;
			return rect.Grow(4f);
		}).ToArray();
		Rect2[] array = blockers;
		for (int num = 0; num < array.Length; num++)
		{
			Rect2 b = array[num];
			xs.Add(ClampX(b.Position.X - size.X));
			xs.Add(ClampX(b.End.X));
			ys.Add(ClampY(b.Position.Y - size.Y));
			ys.Add(ClampY(b.End.Y));
		}
		Rect2 best = desired;
		float bestDistance = float.PositiveInfinity;
		found = false;
		foreach (float x in xs)
		{
			foreach (float y in ys)
			{
				Rect2 candidate = new Rect2(new Vector2(x, y), size);
				float distance = candidate.Position.DistanceSquaredTo(desired.Position);
				if (!(distance >= bestDistance) && !blockers.Any((Rect2 b2) => candidate.Intersects(b2)))
				{
					best = candidate;
					bestDistance = distance;
					found = true;
				}
			}
		}
		return best;
		float ClampX(float value)
		{
			return Math.Clamp(value, lowX, highX);
		}
		float ClampY(float value)
		{
			return Math.Clamp(value, lowY, highY);
		}
	}

	private bool PresentMergedEventCue(GameEvent e)
	{
		SemanticEventCue cue = MergedEventCues.Describe(e, CardText.CardName);
		if ((object)cue == null)
		{
			return false;
		}
		BattleActor actor;
		if (cue.Anchor == "enemy")
		{
			actor = Enemy(cue.EnemyIndex);
		}
		else
		{
			actor = ((cue.Anchor == "player") ? _hero : null);
		}
		if (cue.Anchor == "enemy" && (actor == null || actor.Dead))
		{
			RejectedSemanticCueCount++;
			return true;
		}
		Vector3 origin = ((actor == null) ? new Vector3(0f, 3f, 0f) : (actor.Contact + Vector3.Up * (0.65f + 0.24f * (float)(PresentedSemanticCueCount % 3))));
		ShowSemanticCue(cue, origin, actor);
		LastSemanticCue = cue;
		PresentedSemanticCueCount++;
		return true;
	}

	private void PresentBeforeNumericBoundary(GameEvent e)
	{
		bool flag2 = !_enemyBlockExpiryPending;
		if (!flag2)
		{
			flag2 = (!(e is GameEvent_BurnTick) && !(e is GameEvent_EnemyActionExecuting) && !(e is GameEvent_EnemyPhaseEnded) && !(e is GameEvent_TurnStarted)) || 1 == 0;
		}
		if (flag2)
		{
			return;
		}
		_enemyBlockExpiryPending = false;
		GameState state = _displayState;
		if ((object)state == null)
		{
			return;
		}
		for (int i = 0; i < state.Enemies.Count; i++)
		{
			if (state.Enemies[i].BurrowActive != true)
			{
				PresentBlockBoundary(i, 0);
			}
		}
	}

	private void PresentTurnBoundary(GameEvent e)
	{
		if (!_blockBoundaryCommand)
		{
			return;
		}
		GameState state = _displayState;
		if ((object)state == null)
		{
			return;
		}
		if (!(e is GameEvent_TurnEnded))
		{
			if (!(e is GameEvent_TurnStarted turn))
			{
				if (e is GameEvent_BurrowBroken { EnemyIndex: >=0 } broken && broken.EnemyIndex < state.Enemies.Count)
				{
					EnemyState[] enemies = state.Enemies.ToArray();
					enemies[broken.EnemyIndex] = enemies[broken.EnemyIndex]with
					{
						BurrowActive = false
					};
					_displayState = state with
					{
						Enemies = enemies
					};
				}
			}
			else
			{
				int? blockKeep = state.BlockKeep;
				PresentBlockBoundary(-1, blockKeep.HasValue ? Math.Min(val2: blockKeep.GetValueOrDefault(), val1: state.Player.Block) : 0);
				_displayState = _displayState with
				{
					Turn = turn.Turn
				};
			}
		}
		else
		{
			_enemyBlockExpiryPending = true;
		}
	}

	private void PresentBlockBoundary(int target, int block)
	{
		GameState state = _displayState;
		if ((object)state == null)
		{
			return;
		}
		GameState current = EventRun?.Combat;
		if ((object)current != null)
		{
			CombatPresentationVitals before;
			if (target < 0)
			{
				before = new CombatPresentationVitals
				{
					Hp = state.Player.Hp,
					MaxHp = state.Player.MaxHp,
					Block = state.Player.Block,
					IceBlock = state.Player.IceBlock
				};
			}
			else
			{
				EnemyState enemy = state.Enemies[target];
				before = new CombatPresentationVitals
				{
					Hp = enemy.Hp,
					MaxHp = enemy.MaxHp,
					Block = enemy.Block
				};
			}
			if (before.Block != block && _ledger.CommitBlockBoundary(current, target, before, block))
			{
				SetDisplayedVitals(target, before with
				{
					Block = block
				});
				PresentedBlockBoundaryCount++;
			}
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		return new List<MethodInfo>(37)
		{
			new MethodInfo(MethodName.GetEnemyId, new PropertyInfo(Variant.Type.String, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Int, "index", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.GetEnemyHp, new PropertyInfo(Variant.Type.Int, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Int, "index", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.GetEnemyBlock, new PropertyInfo(Variant.Type.Int, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Int, "index", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.GetEnemyIntent, new PropertyInfo(Variant.Type.String, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Int, "index", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.PlayUiSound, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.String, "sourceKey", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.ApplyQuality, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.String, "profile", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.ApplyQualitySettings, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.SampleQuality, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Float, "unscaledDelta", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Bool, "inBattle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName._Ready, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.EnsureBuilt, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.Bind, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, "session", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Node"), exported: false)
			}, null),
			new MethodInfo(MethodName.SetInterfaceRoute, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.String, "route", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.ClearRun, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Int, "revision", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.Path, new PropertyInfo(Variant.Type.Vector3, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Float, "t", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "s", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.EnemyFoot, new PropertyInfo(Variant.Type.Vector3, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Int, "i", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Int, "count", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.NameFor, new PropertyInfo(Variant.Type.String, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.String, "id", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Bool, "leader", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.RefreshActorLayout, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.StatusName, new PropertyInfo(Variant.Type.String, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.String, "name", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.SyncLedgerActor, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Int, "target", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.ShowReceipt, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.String, "text", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Vector3, "position", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Color, "color", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.FinishBatch, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName._Process, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Float, "delta", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.AdvanceClock, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Float, "delta", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.AdvancePresentation, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Float, "delta", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.CancelPresentation, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Bool, "applyLatest", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.StartDeath, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, "actor", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Node3D"), exported: false),
				new PropertyInfo(Variant.Type.Bool, "retired", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Bool, "fled", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.AdvanceDeath, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, "actor", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Node3D"), exported: false)
			}, null),
			new MethodInfo(MethodName.ClearRetired, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.CacheWorldLights, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.RefreshForestLighting, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, "probeFx", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Node3D"), exported: false)
			}, null),
			new MethodInfo(MethodName.RefreshActorLighting, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, "probeFx", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Node3D"), exported: false)
			}, null),
			new MethodInfo(MethodName.BindActorLights, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, "actor", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Node3D"), exported: false),
				new PropertyInfo(Variant.Type.Object, "probeFx", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Node3D"), exported: false)
			}, null),
			new MethodInfo(MethodName.ClearActors, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName._UnhandledInput, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, "input", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("InputEvent"), exported: false)
			}, null),
			new MethodInfo(MethodName._ExitTree, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.ProjectCueBounds, new PropertyInfo(Variant.Type.Rect2, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, "label", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Label3D"), exported: false),
				new PropertyInfo(Variant.Type.Vector3, "origin", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.PresentBlockBoundary, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Int, "target", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Int, "block", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		string from;
		if (method == MethodName.GetEnemyId && args.Count == 1)
		{
			from = GetEnemyId(VariantUtils.ConvertTo<int>(in args[0]));
			ret = VariantUtils.CreateFrom(in from);
			return true;
		}
		int from2;
		if (method == MethodName.GetEnemyHp && args.Count == 1)
		{
			from2 = GetEnemyHp(VariantUtils.ConvertTo<int>(in args[0]));
			ret = VariantUtils.CreateFrom(in from2);
			return true;
		}
		if (method == MethodName.GetEnemyBlock && args.Count == 1)
		{
			from2 = GetEnemyBlock(VariantUtils.ConvertTo<int>(in args[0]));
			ret = VariantUtils.CreateFrom(in from2);
			return true;
		}
		if (method == MethodName.GetEnemyIntent && args.Count == 1)
		{
			from = GetEnemyIntent(VariantUtils.ConvertTo<int>(in args[0]));
			ret = VariantUtils.CreateFrom(in from);
			return true;
		}
		if (method == MethodName.PlayUiSound && args.Count == 1)
		{
			PlayUiSound(VariantUtils.ConvertTo<string>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.ApplyQuality && args.Count == 1)
		{
			ApplyQuality(VariantUtils.ConvertTo<string>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.ApplyQualitySettings && args.Count == 0)
		{
			ApplyQualitySettings();
			ret = default;
			return true;
		}
		if (method == MethodName.SampleQuality && args.Count == 2)
		{
			SampleQuality(VariantUtils.ConvertTo<float>(in args[0]), VariantUtils.ConvertTo<bool>(in args[1]));
			ret = default;
			return true;
		}
		if (method == MethodName._Ready && args.Count == 0)
		{
			_Ready();
			ret = default;
			return true;
		}
		if (method == MethodName.EnsureBuilt && args.Count == 0)
		{
			EnsureBuilt();
			ret = default;
			return true;
		}
		if (method == MethodName.Bind && args.Count == 1)
		{
			Bind(VariantUtils.ConvertTo<GameSession>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.SetInterfaceRoute && args.Count == 1)
		{
			SetInterfaceRoute(VariantUtils.ConvertTo<string>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.ClearRun && args.Count == 1)
		{
			ClearRun(VariantUtils.ConvertTo<long>(in args[0]));
			ret = default;
			return true;
		}
		Vector3 from3;
		if (method == MethodName.Path && args.Count == 2)
		{
			from3 = Path(VariantUtils.ConvertTo<float>(in args[0]), VariantUtils.ConvertTo<float>(in args[1]));
			ret = VariantUtils.CreateFrom(in from3);
			return true;
		}
		if (method == MethodName.EnemyFoot && args.Count == 2)
		{
			from3 = EnemyFoot(VariantUtils.ConvertTo<int>(in args[0]), VariantUtils.ConvertTo<int>(in args[1]));
			ret = VariantUtils.CreateFrom(in from3);
			return true;
		}
		if (method == MethodName.NameFor && args.Count == 2)
		{
			from = NameFor(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<bool>(in args[1]));
			ret = VariantUtils.CreateFrom(in from);
			return true;
		}
		if (method == MethodName.RefreshActorLayout && args.Count == 0)
		{
			RefreshActorLayout();
			ret = default;
			return true;
		}
		if (method == MethodName.StatusName && args.Count == 1)
		{
			from = StatusName(VariantUtils.ConvertTo<string>(in args[0]));
			ret = VariantUtils.CreateFrom(in from);
			return true;
		}
		if (method == MethodName.SyncLedgerActor && args.Count == 1)
		{
			SyncLedgerActor(VariantUtils.ConvertTo<int>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.ShowReceipt && args.Count == 3)
		{
			ShowReceipt(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<Vector3>(in args[1]), VariantUtils.ConvertTo<Color>(in args[2]));
			ret = default;
			return true;
		}
		if (method == MethodName.FinishBatch && args.Count == 0)
		{
			FinishBatch();
			ret = default;
			return true;
		}
		if (method == MethodName._Process && args.Count == 1)
		{
			_Process(VariantUtils.ConvertTo<double>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.AdvanceClock && args.Count == 1)
		{
			AdvanceClock(VariantUtils.ConvertTo<float>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.AdvancePresentation && args.Count == 1)
		{
			AdvancePresentation(VariantUtils.ConvertTo<float>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.CancelPresentation && args.Count == 1)
		{
			CancelPresentation(VariantUtils.ConvertTo<bool>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.StartDeath && args.Count == 3)
		{
			StartDeath(VariantUtils.ConvertTo<BattleActor>(in args[0]), VariantUtils.ConvertTo<bool>(in args[1]), VariantUtils.ConvertTo<bool>(in args[2]));
			ret = default;
			return true;
		}
		if (method == MethodName.AdvanceDeath && args.Count == 1)
		{
			AdvanceDeath(VariantUtils.ConvertTo<BattleActor>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.ClearRetired && args.Count == 0)
		{
			ClearRetired();
			ret = default;
			return true;
		}
		if (method == MethodName.CacheWorldLights && args.Count == 0)
		{
			CacheWorldLights();
			ret = default;
			return true;
		}
		if (method == MethodName.RefreshForestLighting && args.Count == 1)
		{
			RefreshForestLighting(VariantUtils.ConvertTo<AuthoredGeometryFx>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.RefreshActorLighting && args.Count == 1)
		{
			RefreshActorLighting(VariantUtils.ConvertTo<AuthoredGeometryFx>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.BindActorLights && args.Count == 2)
		{
			BindActorLights(VariantUtils.ConvertTo<BattleActor>(in args[0]), VariantUtils.ConvertTo<AuthoredGeometryFx>(in args[1]));
			ret = default;
			return true;
		}
		if (method == MethodName.ClearActors && args.Count == 0)
		{
			ClearActors();
			ret = default;
			return true;
		}
		if (method == MethodName._UnhandledInput && args.Count == 1)
		{
			_UnhandledInput(VariantUtils.ConvertTo<InputEvent>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName._ExitTree && args.Count == 0)
		{
			_ExitTree();
			ret = default;
			return true;
		}
		if (method == MethodName.ProjectCueBounds && args.Count == 2)
		{
			ret = VariantUtils.CreateFrom<Rect2>(ProjectCueBounds(VariantUtils.ConvertTo<Label3D>(in args[0]), VariantUtils.ConvertTo<Vector3>(in args[1])));
			return true;
		}
		if (method == MethodName.PresentBlockBoundary && args.Count == 2)
		{
			PresentBlockBoundary(VariantUtils.ConvertTo<int>(in args[0]), VariantUtils.ConvertTo<int>(in args[1]));
			ret = default;
			return true;
		}
		return base.InvokeGodotClassMethod(in method, args, out ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		Vector3 from;
		if (method == MethodName.Path && args.Count == 2)
		{
			from = Path(VariantUtils.ConvertTo<float>(in args[0]), VariantUtils.ConvertTo<float>(in args[1]));
			ret = VariantUtils.CreateFrom(in from);
			return true;
		}
		if (method == MethodName.EnemyFoot && args.Count == 2)
		{
			from = EnemyFoot(VariantUtils.ConvertTo<int>(in args[0]), VariantUtils.ConvertTo<int>(in args[1]));
			ret = VariantUtils.CreateFrom(in from);
			return true;
		}
		if (method == MethodName.StatusName && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<string>(StatusName(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		ret = default;
		return false;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if (method == MethodName.GetEnemyId)
		{
			return true;
		}
		if (method == MethodName.GetEnemyHp)
		{
			return true;
		}
		if (method == MethodName.GetEnemyBlock)
		{
			return true;
		}
		if (method == MethodName.GetEnemyIntent)
		{
			return true;
		}
		if (method == MethodName.PlayUiSound)
		{
			return true;
		}
		if (method == MethodName.ApplyQuality)
		{
			return true;
		}
		if (method == MethodName.ApplyQualitySettings)
		{
			return true;
		}
		if (method == MethodName.SampleQuality)
		{
			return true;
		}
		if (method == MethodName._Ready)
		{
			return true;
		}
		if (method == MethodName.EnsureBuilt)
		{
			return true;
		}
		if (method == MethodName.Bind)
		{
			return true;
		}
		if (method == MethodName.SetInterfaceRoute)
		{
			return true;
		}
		if (method == MethodName.ClearRun)
		{
			return true;
		}
		if (method == MethodName.Path)
		{
			return true;
		}
		if (method == MethodName.EnemyFoot)
		{
			return true;
		}
		if (method == MethodName.NameFor)
		{
			return true;
		}
		if (method == MethodName.RefreshActorLayout)
		{
			return true;
		}
		if (method == MethodName.StatusName)
		{
			return true;
		}
		if (method == MethodName.SyncLedgerActor)
		{
			return true;
		}
		if (method == MethodName.ShowReceipt)
		{
			return true;
		}
		if (method == MethodName.FinishBatch)
		{
			return true;
		}
		if (method == MethodName._Process)
		{
			return true;
		}
		if (method == MethodName.AdvanceClock)
		{
			return true;
		}
		if (method == MethodName.AdvancePresentation)
		{
			return true;
		}
		if (method == MethodName.CancelPresentation)
		{
			return true;
		}
		if (method == MethodName.StartDeath)
		{
			return true;
		}
		if (method == MethodName.AdvanceDeath)
		{
			return true;
		}
		if (method == MethodName.ClearRetired)
		{
			return true;
		}
		if (method == MethodName.CacheWorldLights)
		{
			return true;
		}
		if (method == MethodName.RefreshForestLighting)
		{
			return true;
		}
		if (method == MethodName.RefreshActorLighting)
		{
			return true;
		}
		if (method == MethodName.BindActorLights)
		{
			return true;
		}
		if (method == MethodName.ClearActors)
		{
			return true;
		}
		if (method == MethodName._UnhandledInput)
		{
			return true;
		}
		if (method == MethodName._ExitTree)
		{
			return true;
		}
		if (method == MethodName.ProjectCueBounds)
		{
			return true;
		}
		if (method == MethodName.PresentBlockBoundary)
		{
			return true;
		}
		return base.HasGodotClassMethod(in method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if (name == PropertyName.IsPresenting)
		{
			IsPresenting = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName.DamageContactCount)
		{
			DamageContactCount = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName.RejectedReceiptCount)
		{
			RejectedReceiptCount = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName.RealLightsEnabled)
		{
			RealLightsEnabled = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName.QualityProfile)
		{
			QualityProfile = VariantUtils.ConvertTo<string>(in value);
			return true;
		}
		if (name == PropertyName.PlaybackSpeed)
		{
			PlaybackSpeed = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		if (name == PropertyName.PlaybackPaused)
		{
			PlaybackPaused = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName.LastEventReceiptRejected)
		{
			LastEventReceiptRejected = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName.PresentedSemanticCueCount)
		{
			PresentedSemanticCueCount = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName.RejectedSemanticCueCount)
		{
			RejectedSemanticCueCount = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName.PresentedBlockBoundaryCount)
		{
			PresentedBlockBoundaryCount = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName._effectStartedAt)
		{
			_effectStartedAt = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		if (name == PropertyName._effectOffset)
		{
			_effectOffset = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		if (name == PropertyName._contactHp)
		{
			_contactHp = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName._contactBlock)
		{
			_contactBlock = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName._qualityApplied)
		{
			_qualityApplied = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._projectionActive)
		{
			_projectionActive = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._rejectBeat)
		{
			_rejectBeat = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._interfaceRoute)
		{
			_interfaceRoute = VariantUtils.ConvertTo<string>(in value);
			return true;
		}
		if (name == PropertyName._native)
		{
			_native = VariantUtils.ConvertTo<AuthoredGeometryFx>(in value);
			return true;
		}
		if (name == PropertyName._session)
		{
			_session = VariantUtils.ConvertTo<GameSession>(in value);
			return true;
		}
		if (name == PropertyName._hero)
		{
			_hero = VariantUtils.ConvertTo<BattleActor>(in value);
			return true;
		}
		if (name == PropertyName._sourceActor)
		{
			_sourceActor = VariantUtils.ConvertTo<BattleActor>(in value);
			return true;
		}
		if (name == PropertyName._targetActor)
		{
			_targetActor = VariantUtils.ConvertTo<BattleActor>(in value);
			return true;
		}
		if (name == PropertyName._retiredRoot)
		{
			_retiredRoot = VariantUtils.ConvertTo<Node3D>(in value);
			return true;
		}
		if (name == PropertyName._actorRoot)
		{
			_actorRoot = VariantUtils.ConvertTo<Node3D>(in value);
			return true;
		}
		if (name == PropertyName._fx)
		{
			_fx = VariantUtils.ConvertTo<ContinuousAttackFx>(in value);
			return true;
		}
		if (name == PropertyName._world)
		{
			_world = VariantUtils.ConvertTo<StageWorld>(in value);
			return true;
		}
		if (name == PropertyName._audio)
		{
			_audio = VariantUtils.ConvertTo<BattleAudio>(in value);
			return true;
		}
		if (name == PropertyName._cues)
		{
			_cues = VariantUtils.ConvertTo<BattleCuePool>(in value);
			return true;
		}
		if (name == PropertyName._receipt)
		{
			_receipt = VariantUtils.ConvertTo<Label3D>(in value);
			return true;
		}
		if (name == PropertyName._lastRevision)
		{
			_lastRevision = VariantUtils.ConvertTo<long>(in value);
			return true;
		}
		if (name == PropertyName._clock)
		{
			_clock = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		if (name == PropertyName._act)
		{
			_act = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName._initialized)
		{
			_initialized = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._resetting)
		{
			_resetting = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._receiptHome)
		{
			_receiptHome = VariantUtils.ConvertTo<Vector3>(in value);
			return true;
		}
		if (name == PropertyName._semanticPlacementId)
		{
			_semanticPlacementId = VariantUtils.ConvertTo<long>(in value);
			return true;
		}
		if (name == PropertyName._enemyBlockExpiryPending)
		{
			_enemyBlockExpiryPending = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._blockBoundaryCommand)
		{
			_blockBoundaryCommand = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		return base.SetGodotClassPropertyValue(in name, in value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		bool from;
		if (name == PropertyName.IsPresenting)
		{
			from = IsPresenting;
			value = VariantUtils.CreateFrom(in from);
			return true;
		}
		int from2;
		if (name == PropertyName.DisplayedHeroHp)
		{
			from2 = DisplayedHeroHp;
			value = VariantUtils.CreateFrom(in from2);
			return true;
		}
		if (name == PropertyName.DisplayedHeroBlock)
		{
			from2 = DisplayedHeroBlock;
			value = VariantUtils.CreateFrom(in from2);
			return true;
		}
		if (name == PropertyName.DisplayedEnemyCount)
		{
			from2 = DisplayedEnemyCount;
			value = VariantUtils.CreateFrom(in from2);
			return true;
		}
		if (name == PropertyName.ActorsVisible)
		{
			from = ActorsVisible;
			value = VariantUtils.CreateFrom(in from);
			return true;
		}
		string from3;
		if (name == PropertyName.CurrentAudioTrack)
		{
			from3 = CurrentAudioTrack;
			value = VariantUtils.CreateFrom(in from3);
			return true;
		}
		if (name == PropertyName.MusicPlaying)
		{
			from = MusicPlaying;
			value = VariantUtils.CreateFrom(in from);
			return true;
		}
		if (name == PropertyName.DamageAudioCount)
		{
			from2 = DamageAudioCount;
			value = VariantUtils.CreateFrom(in from2);
			return true;
		}
		if (name == PropertyName.PlayingSoundEffects)
		{
			from2 = PlayingSoundEffects;
			value = VariantUtils.CreateFrom(in from2);
			return true;
		}
		if (name == PropertyName.DamageContactCount)
		{
			from2 = DamageContactCount;
			value = VariantUtils.CreateFrom(in from2);
			return true;
		}
		if (name == PropertyName.PresentedRevision)
		{
			value = VariantUtils.CreateFrom<long>(PresentedRevision);
			return true;
		}
		if (name == PropertyName.PendingEventCount)
		{
			from2 = PendingEventCount;
			value = VariantUtils.CreateFrom(in from2);
			return true;
		}
		if (name == PropertyName.ActiveEffectCount)
		{
			from2 = ActiveEffectCount;
			value = VariantUtils.CreateFrom(in from2);
			return true;
		}
		if (name == PropertyName.ActiveLightCount)
		{
			from2 = ActiveLightCount;
			value = VariantUtils.CreateFrom(in from2);
			return true;
		}
		if (name == PropertyName.RejectedReceiptCount)
		{
			from2 = RejectedReceiptCount;
			value = VariantUtils.CreateFrom(in from2);
			return true;
		}
		if (name == PropertyName.CompletedEventCount)
		{
			from2 = CompletedEventCount;
			value = VariantUtils.CreateFrom(in from2);
			return true;
		}
		float from4;
		if (name == PropertyName.PresentationTime)
		{
			from4 = PresentationTime;
			value = VariantUtils.CreateFrom(in from4);
			return true;
		}
		if (name == PropertyName.ActiveEventType)
		{
			from3 = ActiveEventType;
			value = VariantUtils.CreateFrom(in from3);
			return true;
		}
		if (name == PropertyName.ActiveFamily)
		{
			from3 = ActiveFamily;
			value = VariantUtils.CreateFrom(in from3);
			return true;
		}
		if (name == PropertyName.ActiveTier)
		{
			from3 = ActiveTier;
			value = VariantUtils.CreateFrom(in from3);
			return true;
		}
		if (name == PropertyName.RealLightsEnabled)
		{
			from = RealLightsEnabled;
			value = VariantUtils.CreateFrom(in from);
			return true;
		}
		if (name == PropertyName.QualityProfile)
		{
			from3 = QualityProfile;
			value = VariantUtils.CreateFrom(in from3);
			return true;
		}
		if (name == PropertyName.EffectiveQualityProfile)
		{
			from3 = EffectiveQualityProfile;
			value = VariantUtils.CreateFrom(in from3);
			return true;
		}
		if (name == PropertyName.IsPhoneQuality)
		{
			from = IsPhoneQuality;
			value = VariantUtils.CreateFrom(in from);
			return true;
		}
		if (name == PropertyName.PlaybackSpeed)
		{
			from4 = PlaybackSpeed;
			value = VariantUtils.CreateFrom(in from4);
			return true;
		}
		if (name == PropertyName.PlaybackPaused)
		{
			from = PlaybackPaused;
			value = VariantUtils.CreateFrom(in from);
			return true;
		}
		if (name == PropertyName.LastEventReceiptRejected)
		{
			from = LastEventReceiptRejected;
			value = VariantUtils.CreateFrom(in from);
			return true;
		}
		if (name == PropertyName.ActiveSemanticCueEventType)
		{
			from3 = ActiveSemanticCueEventType;
			value = VariantUtils.CreateFrom(in from3);
			return true;
		}
		if (name == PropertyName.ActiveSemanticCuePlacementFound)
		{
			from = ActiveSemanticCuePlacementFound;
			value = VariantUtils.CreateFrom(in from);
			return true;
		}
		if (name == PropertyName.PresentedSemanticCueCount)
		{
			from2 = PresentedSemanticCueCount;
			value = VariantUtils.CreateFrom(in from2);
			return true;
		}
		if (name == PropertyName.RejectedSemanticCueCount)
		{
			from2 = RejectedSemanticCueCount;
			value = VariantUtils.CreateFrom(in from2);
			return true;
		}
		if (name == PropertyName.ActiveCueCount)
		{
			from2 = ActiveCueCount;
			value = VariantUtils.CreateFrom(in from2);
			return true;
		}
		if (name == PropertyName.PresentedBlockBoundaryCount)
		{
			from2 = PresentedBlockBoundaryCount;
			value = VariantUtils.CreateFrom(in from2);
			return true;
		}
		if (name == PropertyName._effectStartedAt)
		{
			value = VariantUtils.CreateFrom(in _effectStartedAt);
			return true;
		}
		if (name == PropertyName._effectOffset)
		{
			value = VariantUtils.CreateFrom(in _effectOffset);
			return true;
		}
		if (name == PropertyName._contactHp)
		{
			value = VariantUtils.CreateFrom(in _contactHp);
			return true;
		}
		if (name == PropertyName._contactBlock)
		{
			value = VariantUtils.CreateFrom(in _contactBlock);
			return true;
		}
		if (name == PropertyName._qualityApplied)
		{
			value = VariantUtils.CreateFrom(in _qualityApplied);
			return true;
		}
		if (name == PropertyName._projectionActive)
		{
			value = VariantUtils.CreateFrom(in _projectionActive);
			return true;
		}
		if (name == PropertyName._rejectBeat)
		{
			value = VariantUtils.CreateFrom(in _rejectBeat);
			return true;
		}
		if (name == PropertyName._interfaceRoute)
		{
			value = VariantUtils.CreateFrom(in _interfaceRoute);
			return true;
		}
		if (name == PropertyName._native)
		{
			value = VariantUtils.CreateFrom(in _native);
			return true;
		}
		if (name == PropertyName._session)
		{
			value = VariantUtils.CreateFrom(in _session);
			return true;
		}
		if (name == PropertyName._hero)
		{
			value = VariantUtils.CreateFrom(in _hero);
			return true;
		}
		if (name == PropertyName._sourceActor)
		{
			value = VariantUtils.CreateFrom(in _sourceActor);
			return true;
		}
		if (name == PropertyName._targetActor)
		{
			value = VariantUtils.CreateFrom(in _targetActor);
			return true;
		}
		if (name == PropertyName._retiredRoot)
		{
			value = VariantUtils.CreateFrom(in _retiredRoot);
			return true;
		}
		if (name == PropertyName._actorRoot)
		{
			value = VariantUtils.CreateFrom(in _actorRoot);
			return true;
		}
		if (name == PropertyName._fx)
		{
			value = VariantUtils.CreateFrom(in _fx);
			return true;
		}
		if (name == PropertyName._world)
		{
			value = VariantUtils.CreateFrom(in _world);
			return true;
		}
		if (name == PropertyName._audio)
		{
			value = VariantUtils.CreateFrom(in _audio);
			return true;
		}
		if (name == PropertyName._cues)
		{
			value = VariantUtils.CreateFrom(in _cues);
			return true;
		}
		if (name == PropertyName._receipt)
		{
			value = VariantUtils.CreateFrom(in _receipt);
			return true;
		}
		if (name == PropertyName._lastRevision)
		{
			value = VariantUtils.CreateFrom(in _lastRevision);
			return true;
		}
		if (name == PropertyName._clock)
		{
			value = VariantUtils.CreateFrom(in _clock);
			return true;
		}
		if (name == PropertyName._act)
		{
			value = VariantUtils.CreateFrom(in _act);
			return true;
		}
		if (name == PropertyName._initialized)
		{
			value = VariantUtils.CreateFrom(in _initialized);
			return true;
		}
		if (name == PropertyName._resetting)
		{
			value = VariantUtils.CreateFrom(in _resetting);
			return true;
		}
		if (name == PropertyName._receiptHome)
		{
			value = VariantUtils.CreateFrom(in _receiptHome);
			return true;
		}
		if (name == PropertyName._semanticPlacementId)
		{
			value = VariantUtils.CreateFrom(in _semanticPlacementId);
			return true;
		}
		if (name == PropertyName._enemyBlockExpiryPending)
		{
			value = VariantUtils.CreateFrom(in _enemyBlockExpiryPending);
			return true;
		}
		if (name == PropertyName._blockBoundaryCommand)
		{
			value = VariantUtils.CreateFrom(in _blockBoundaryCommand);
			return true;
		}
		return base.GetGodotClassPropertyValue(in name, out value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		return new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Bool, PropertyName.IsPresenting, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName._effectStartedAt, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName._effectOffset, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName._contactHp, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName._contactBlock, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.DisplayedHeroHp, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.DisplayedHeroBlock, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.DisplayedEnemyCount, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName.ActorsVisible, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.String, PropertyName.CurrentAudioTrack, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName.MusicPlaying, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.DamageAudioCount, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.PlayingSoundEffects, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.DamageContactCount, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.PresentedRevision, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.PendingEventCount, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.ActiveEffectCount, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.ActiveLightCount, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.RejectedReceiptCount, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.CompletedEventCount, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName.PresentationTime, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.String, PropertyName.ActiveEventType, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.String, PropertyName.ActiveFamily, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.String, PropertyName.ActiveTier, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName.RealLightsEnabled, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.String, PropertyName.QualityProfile, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.String, PropertyName.EffectiveQualityProfile, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName._qualityApplied, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName.IsPhoneQuality, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName.PlaybackSpeed, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName.PlaybackPaused, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName._projectionActive, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName._rejectBeat, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.String, PropertyName._interfaceRoute, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName.LastEventReceiptRejected, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._native, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._session, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._hero, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._sourceActor, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._targetActor, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._retiredRoot, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._actorRoot, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._fx, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._world, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._audio, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._cues, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._receipt, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName._lastRevision, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName._clock, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName._act, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName._initialized, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName._resetting, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Vector3, PropertyName._receiptHome, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName._semanticPlacementId, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.String, PropertyName.ActiveSemanticCueEventType, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName.ActiveSemanticCuePlacementFound, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.PresentedSemanticCueCount, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.RejectedSemanticCueCount, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.ActiveCueCount, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName._enemyBlockExpiryPending, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName._blockBoundaryCommand, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.PresentedBlockBoundaryCount, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		base.SaveGodotObjectData(info);
		info.AddProperty(PropertyName.IsPresenting, Variant.From<bool>(IsPresenting));
		info.AddProperty(PropertyName.DamageContactCount, Variant.From<int>(DamageContactCount));
		info.AddProperty(PropertyName.RejectedReceiptCount, Variant.From<int>(RejectedReceiptCount));
		info.AddProperty(PropertyName.RealLightsEnabled, Variant.From<bool>(RealLightsEnabled));
		info.AddProperty(PropertyName.QualityProfile, Variant.From<string>(QualityProfile));
		info.AddProperty(PropertyName.PlaybackSpeed, Variant.From<float>(PlaybackSpeed));
		info.AddProperty(PropertyName.PlaybackPaused, Variant.From<bool>(PlaybackPaused));
		info.AddProperty(PropertyName.LastEventReceiptRejected, Variant.From<bool>(LastEventReceiptRejected));
		info.AddProperty(PropertyName.PresentedSemanticCueCount, Variant.From<int>(PresentedSemanticCueCount));
		info.AddProperty(PropertyName.RejectedSemanticCueCount, Variant.From<int>(RejectedSemanticCueCount));
		info.AddProperty(PropertyName.PresentedBlockBoundaryCount, Variant.From<int>(PresentedBlockBoundaryCount));
		info.AddProperty(PropertyName._effectStartedAt, Variant.From(in _effectStartedAt));
		info.AddProperty(PropertyName._effectOffset, Variant.From(in _effectOffset));
		info.AddProperty(PropertyName._contactHp, Variant.From(in _contactHp));
		info.AddProperty(PropertyName._contactBlock, Variant.From(in _contactBlock));
		info.AddProperty(PropertyName._qualityApplied, Variant.From(in _qualityApplied));
		info.AddProperty(PropertyName._projectionActive, Variant.From(in _projectionActive));
		info.AddProperty(PropertyName._rejectBeat, Variant.From(in _rejectBeat));
		info.AddProperty(PropertyName._interfaceRoute, Variant.From(in _interfaceRoute));
		info.AddProperty(PropertyName._native, Variant.From(in _native));
		info.AddProperty(PropertyName._session, Variant.From(in _session));
		info.AddProperty(PropertyName._hero, Variant.From(in _hero));
		info.AddProperty(PropertyName._sourceActor, Variant.From(in _sourceActor));
		info.AddProperty(PropertyName._targetActor, Variant.From(in _targetActor));
		info.AddProperty(PropertyName._retiredRoot, Variant.From(in _retiredRoot));
		info.AddProperty(PropertyName._actorRoot, Variant.From(in _actorRoot));
		info.AddProperty(PropertyName._fx, Variant.From(in _fx));
		info.AddProperty(PropertyName._world, Variant.From(in _world));
		info.AddProperty(PropertyName._audio, Variant.From(in _audio));
		info.AddProperty(PropertyName._cues, Variant.From(in _cues));
		info.AddProperty(PropertyName._receipt, Variant.From(in _receipt));
		info.AddProperty(PropertyName._lastRevision, Variant.From(in _lastRevision));
		info.AddProperty(PropertyName._clock, Variant.From(in _clock));
		info.AddProperty(PropertyName._act, Variant.From(in _act));
		info.AddProperty(PropertyName._initialized, Variant.From(in _initialized));
		info.AddProperty(PropertyName._resetting, Variant.From(in _resetting));
		info.AddProperty(PropertyName._receiptHome, Variant.From(in _receiptHome));
		info.AddProperty(PropertyName._semanticPlacementId, Variant.From(in _semanticPlacementId));
		info.AddProperty(PropertyName._enemyBlockExpiryPending, Variant.From(in _enemyBlockExpiryPending));
		info.AddProperty(PropertyName._blockBoundaryCommand, Variant.From(in _blockBoundaryCommand));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		base.RestoreGodotObjectData(info);
		if (info.TryGetProperty(PropertyName.IsPresenting, out var _value_IsPresenting))
		{
			IsPresenting = _value_IsPresenting.As<bool>();
		}
		if (info.TryGetProperty(PropertyName.DamageContactCount, out var _value_DamageContactCount))
		{
			DamageContactCount = _value_DamageContactCount.As<int>();
		}
		if (info.TryGetProperty(PropertyName.RejectedReceiptCount, out var _value_RejectedReceiptCount))
		{
			RejectedReceiptCount = _value_RejectedReceiptCount.As<int>();
		}
		if (info.TryGetProperty(PropertyName.RealLightsEnabled, out var _value_RealLightsEnabled))
		{
			RealLightsEnabled = _value_RealLightsEnabled.As<bool>();
		}
		if (info.TryGetProperty(PropertyName.QualityProfile, out var _value_QualityProfile))
		{
			QualityProfile = _value_QualityProfile.As<string>();
		}
		if (info.TryGetProperty(PropertyName.PlaybackSpeed, out var _value_PlaybackSpeed))
		{
			PlaybackSpeed = _value_PlaybackSpeed.As<float>();
		}
		if (info.TryGetProperty(PropertyName.PlaybackPaused, out var _value_PlaybackPaused))
		{
			PlaybackPaused = _value_PlaybackPaused.As<bool>();
		}
		if (info.TryGetProperty(PropertyName.LastEventReceiptRejected, out var _value_LastEventReceiptRejected))
		{
			LastEventReceiptRejected = _value_LastEventReceiptRejected.As<bool>();
		}
		if (info.TryGetProperty(PropertyName.PresentedSemanticCueCount, out var _value_PresentedSemanticCueCount))
		{
			PresentedSemanticCueCount = _value_PresentedSemanticCueCount.As<int>();
		}
		if (info.TryGetProperty(PropertyName.RejectedSemanticCueCount, out var _value_RejectedSemanticCueCount))
		{
			RejectedSemanticCueCount = _value_RejectedSemanticCueCount.As<int>();
		}
		if (info.TryGetProperty(PropertyName.PresentedBlockBoundaryCount, out var _value_PresentedBlockBoundaryCount))
		{
			PresentedBlockBoundaryCount = _value_PresentedBlockBoundaryCount.As<int>();
		}
		if (info.TryGetProperty(PropertyName._effectStartedAt, out var _value__effectStartedAt))
		{
			_effectStartedAt = _value__effectStartedAt.As<float>();
		}
		if (info.TryGetProperty(PropertyName._effectOffset, out var _value__effectOffset))
		{
			_effectOffset = _value__effectOffset.As<float>();
		}
		if (info.TryGetProperty(PropertyName._contactHp, out var _value__contactHp))
		{
			_contactHp = _value__contactHp.As<int>();
		}
		if (info.TryGetProperty(PropertyName._contactBlock, out var _value__contactBlock))
		{
			_contactBlock = _value__contactBlock.As<int>();
		}
		if (info.TryGetProperty(PropertyName._qualityApplied, out var _value__qualityApplied))
		{
			_qualityApplied = _value__qualityApplied.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._projectionActive, out var _value__projectionActive))
		{
			_projectionActive = _value__projectionActive.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._rejectBeat, out var _value__rejectBeat))
		{
			_rejectBeat = _value__rejectBeat.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._interfaceRoute, out var _value__interfaceRoute))
		{
			_interfaceRoute = _value__interfaceRoute.As<string>();
		}
		if (info.TryGetProperty(PropertyName._native, out var _value__native))
		{
			_native = _value__native.As<AuthoredGeometryFx>();
		}
		if (info.TryGetProperty(PropertyName._session, out var _value__session))
		{
			_session = _value__session.As<GameSession>();
		}
		if (info.TryGetProperty(PropertyName._hero, out var _value__hero))
		{
			_hero = _value__hero.As<BattleActor>();
		}
		if (info.TryGetProperty(PropertyName._sourceActor, out var _value__sourceActor))
		{
			_sourceActor = _value__sourceActor.As<BattleActor>();
		}
		if (info.TryGetProperty(PropertyName._targetActor, out var _value__targetActor))
		{
			_targetActor = _value__targetActor.As<BattleActor>();
		}
		if (info.TryGetProperty(PropertyName._retiredRoot, out var _value__retiredRoot))
		{
			_retiredRoot = _value__retiredRoot.As<Node3D>();
		}
		if (info.TryGetProperty(PropertyName._actorRoot, out var _value__actorRoot))
		{
			_actorRoot = _value__actorRoot.As<Node3D>();
		}
		if (info.TryGetProperty(PropertyName._fx, out var _value__fx))
		{
			_fx = _value__fx.As<ContinuousAttackFx>();
		}
		if (info.TryGetProperty(PropertyName._world, out var _value__world))
		{
			_world = _value__world.As<StageWorld>();
		}
		if (info.TryGetProperty(PropertyName._audio, out var _value__audio))
		{
			_audio = _value__audio.As<BattleAudio>();
		}
		if (info.TryGetProperty(PropertyName._cues, out var _value__cues))
		{
			_cues = _value__cues.As<BattleCuePool>();
		}
		if (info.TryGetProperty(PropertyName._receipt, out var _value__receipt))
		{
			_receipt = _value__receipt.As<Label3D>();
		}
		if (info.TryGetProperty(PropertyName._lastRevision, out var _value__lastRevision))
		{
			_lastRevision = _value__lastRevision.As<long>();
		}
		if (info.TryGetProperty(PropertyName._clock, out var _value__clock))
		{
			_clock = _value__clock.As<float>();
		}
		if (info.TryGetProperty(PropertyName._act, out var _value__act))
		{
			_act = _value__act.As<int>();
		}
		if (info.TryGetProperty(PropertyName._initialized, out var _value__initialized))
		{
			_initialized = _value__initialized.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._resetting, out var _value__resetting))
		{
			_resetting = _value__resetting.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._receiptHome, out var _value__receiptHome))
		{
			_receiptHome = _value__receiptHome.As<Vector3>();
		}
		if (info.TryGetProperty(PropertyName._semanticPlacementId, out var _value__semanticPlacementId))
		{
			_semanticPlacementId = _value__semanticPlacementId.As<long>();
		}
		if (info.TryGetProperty(PropertyName._enemyBlockExpiryPending, out var _value__enemyBlockExpiryPending))
		{
			_enemyBlockExpiryPending = _value__enemyBlockExpiryPending.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._blockBoundaryCommand, out var _value__blockBoundaryCommand))
		{
			_blockBoundaryCommand = _value__blockBoundaryCommand.As<bool>();
		}
	}

	public void SetActorSourceDisplay(bool enabled)
	{
		ActorSourceDisplayRequested = enabled && !Enumerable.Contains(OS.GetCmdlineUserArgs(), "--actor-color-baseline");
		if (_actorRoot == null || !GodotObject.IsInstanceValid(_actorRoot))
		{
			return;
		}
		foreach (BattleActor item in _actorRoot.GetChildren().OfType<BattleActor>())
		{
			item.SetSourceDisplayTransferEnabled(ActorSourceDisplayRequested);
		}
	}

	private static bool SameEnemyExecution(EnemyActionPresentation? a, EnemyActionPresentation? b)
	{
		if ((object)a != null && (object)b != null && a.Version == 1 && b.Version == 1 && a.EnemyId == b.EnemyId)
		{
			return a.ExecutionEventIndex == b.ExecutionEventIndex;
		}
		return false;
	}

	private static string? ProvenEnemyAttackMove(EnemyActionPresentation? action, BattleActor actor)
	{
		bool flag = (object)action != null && action.Version == 1 && action.UnresolvedReason == null && action.MoveId != null && action.EnemyId == actor.DefinitionId && action.ExecutionEventIndex >= 0;
		if (flag)
		{
			string branch = action.Branch;
			bool flag2 = ((branch == "primary" || branch == "alternate") ? true : false);
			flag = flag2;
		}
		if (!flag)
		{
			return null;
		}
		return action.MoveId;
	}

	private void PresentEnemyAttackExecution(GameEvent_EnemyActionExecuting executing)
	{
		if (executing.Kind != "attack")
		{
			return;
		}
		BattleActor actor = Enemy(executing.EnemyIndex);
		if (actor == null)
		{
			return;
		}
		ActionPlayback action = _ordinaryCallback?.Action;
		if (action == null)
		{
			return;
		}
		foreach (OrdinaryBeat row in action.Beats)
		{
			GameEvent gameEvent = row.Beat.Event;
			EnemyActionPresentation enemyActionPresentation;
			if (gameEvent is GameEvent_DamageDealt hit)
			{
				if (!(hit.Source == "enemy") || hit.EnemyIndex != executing.EnemyIndex)
				{
					goto IL_00e1;
				}
				enemyActionPresentation = hit.PresentationEnemyAction;
			}
			else
			{
				if (!(gameEvent is GameEvent_ConfusedAttack confused) || confused.EnemyIndex != executing.EnemyIndex)
				{
					goto IL_00e1;
				}
				enemyActionPresentation = confused.PresentationEnemyAction;
			}
			goto IL_00e4;
			IL_00e1:
			enemyActionPresentation = null;
			goto IL_00e4;
			IL_00e4:
			EnemyActionPresentation metadata = enemyActionPresentation;
			if (!row.Contacted && SameEnemyExecution(executing.PresentationEnemyAction, metadata))
			{
				bool ranged = row.Beat.Event is GameEvent_DamageDealt && SourceEnemyRanged(action.Owner.Change.Previous?.Combat, executing.EnemyIndex);
				actor.BeginEnemyAttack(_clock, row.ContactAt, executing.PresentationEnemyAction.ExecutionEventIndex, ProvenEnemyAttackMove(executing.PresentationEnemyAction, actor), ranged, MimicAuthoredCountProof.Capture(executing, row.Beat.Event, row.Beat.Contacts));
				if (!ranged && row.Beat.Event is GameEvent_DamageDealt)
				{
					// SourceUiLunge reaches its forward peak at 35% of its duration.
					actor.BeginSourceLunge(ProfiledEnemyMeleeTravel(actor), _clock, _presentationFrame,
						EnemyMeleeLungeDuration, Math.Max(0f, row.ContactAt - _clock - EnemyMeleeLungeLead), row.Beat.Contacts?.Span ?? 0f);
					actor.UiLunge.ArmContactHold();
					row.EnemyLungeActor = actor;
					row.EnemyLungeVersion = actor.UiLunge.Version;
					row.EnemyLungeScheduled = true;
				}
				break;
			}
		}
	}

	private void PreviewEnemyAttack(GameEvent_DamageDealt hit, bool ranged)
	{
		BattleActor actor = _sourceActor;
		if (actor != null && !(hit.Source != "enemy"))
		{
			EnemyActionPresentation metadata = hit.PresentationEnemyAction;
			if ((object)metadata != null && metadata.Version == 1 && metadata.ExecutionEventIndex >= 0 && !(metadata.EnemyId != actor.DefinitionId))
			{
				actor.PreviewEnemyAttack(_clock, _ordinaryCallback?.ContactAt ?? _clock, metadata.ExecutionEventIndex, ProvenEnemyAttackMove(metadata, actor), ranged);
			}
		}
	}

	private void PreviewConfusedEnemyAttack(PresentationBeat beat, GameEvent_ConfusedAttack hit)
	{
		BattleActor actor = _sourceActor;
		if (actor != null)
		{
			EnemyActionPresentation metadata = hit.PresentationEnemyAction;
			if ((object)metadata != null && metadata.Version == 1 && metadata.ExecutionEventIndex >= 0 && !(metadata.EnemyId != actor.DefinitionId))
			{
				actor.PreviewEnemyAttack(_clock, _ordinaryCallback?.ContactAt ?? (_clock + beat.ContactAt), metadata.ExecutionEventIndex, ProvenEnemyAttackMove(metadata, actor));
			}
		}
	}

	private void PresentConfusedEnemyAttackContact(GameEvent_ConfusedAttack hit)
	{
		BattleActor actor = _sourceActor;
		if (actor != null)
		{
			EnemyActionPresentation metadata = hit.PresentationEnemyAction;
			if ((object)metadata != null && metadata.Version == 1 && metadata.ExecutionEventIndex >= 0 && !(metadata.EnemyId != actor.DefinitionId))
			{
				actor.ContactEnemyAttack(_clock, metadata.ExecutionEventIndex, ProvenEnemyAttackMove(metadata, actor), 0, 1);
			}
		}
	}

	private void PresentEnemyAttackContact(PresentationBeat beat, GameEvent_DamageDealt hit)
	{
		if (hit.Source != "enemy")
		{
			return;
		}
		BattleActor actor = _sourceActor;
		if (actor != null)
		{
			EnemyActionPresentation metadata = hit.PresentationEnemyAction;
			if ((object)metadata != null && metadata.Version == 1 && metadata.ExecutionEventIndex >= 0 && !(metadata.EnemyId != actor.DefinitionId))
			{
				int ordinal = Math.Max(0, BeatContactOrdinal);
				EnemyAttackContactPlan? contacts = beat.Contacts;
				int count = ((contacts == null || !contacts.Individual) ? 1 : beat.Contacts.Count);
				actor.ContactEnemyAttack(_clock, metadata.ExecutionEventIndex, ProvenEnemyAttackMove(metadata, actor), ordinal, count);
			}
		}
	}

	private void BeginSourceCastLight(ActionPlayback action)
	{
		JToken tune;
		if (SourceCastLightEnabled && SourceShowcaseEnabled && (object)action.Profile != null && _hero != null && SourceCastLight.TryColor(action.Card?.CardId, out var color))
		{
			bool phone = OS.HasFeature("android") || OS.HasFeature("ios") || Enumerable.Contains(OS.GetCmdlineUserArgs(), "--ui-phone");
			tune = ActorMaterialBinding.LoadLook(_act, phone)["hitLight"];
			SourceCastLightProfile profile = new SourceCastLightProfile(Num("intensity", 1f) * Num("castMul", 0.8f), Num("refDist", 1.5f), Num("castDur", 0.3f), Math.Clamp(Num("hold", 0.3f), 0f, 0.9f), Math.Max(0.1f, Num("range", 4f)), Num("towardCamera", 0.6f));
			_sourceCastLight.Begin(_clock, profile, _hero.SourceMotionFeet, _hero.SourceMotionHeight, _world.Camera.GlobalBasis.X, _world.Camera.GlobalBasis.Y, _world.Camera.GlobalPosition, color);
		}
		float Num(string key, float fallback)
		{
			JToken v = tune?[key];
			JTokenType? jTokenType = v?.Type;
			bool flag;
			if (jTokenType.HasValue)
			{
				JTokenType valueOrDefault = jTokenType.GetValueOrDefault();
				if ((uint)(valueOrDefault - 6) <= 1u)
				{
					flag = true;
					goto IL_004e;
				}
			}
			flag = false;
			goto IL_004e;
			IL_004e:
			if (!flag)
			{
				return fallback;
			}
			float f = v.Value<float>();
			if (!float.IsFinite(f) || !(f >= 0f))
			{
				return fallback;
			}
			return f;
		}
	}

	private void AppendSourceCastLight(List<ActorMaterialBinding.PointLight> lights)
	{
		float strength = SourceCastLightStrength;
		if (!(strength <= 0f) && lights.Count < 8)
		{
			Vector3 c = _sourceCastLight.ColorSrgb;
			Color linear = new Color(c.X, c.Y, c.Z).SrgbToLinear();
			lights.Insert(Math.Min(2, lights.Count), new ActorMaterialBinding.PointLight(_sourceCastLight.Position, _sourceCastLight.Range, new Vector3(linear.R, linear.G, linear.B) * strength, IsAttack: true));
		}
	}

	private void ClearSourceCastLight()
	{
		_sourceCastLight.Clear();
	}

	public void SetSourceCombatTextHost(CanvasLayer host, Func<(float Unit, float TextScale)> metrics)
	{
		CancelSourceCombatText();
		_sourceFloatHost = host;
		_sourceFloatMetrics = metrics;
	}

	private bool TryShowSourceDamage(GameEvent_DamageDealt hit, int loss, int absorbed)
	{
		if (!(hit.Source != "player") && _sourceFloatHost != null && GodotObject.IsInstanceValid(_sourceFloatHost))
		{
			BattleActor actor = Enemy(hit.EnemyIndex);
			if (actor != null)
			{
				(float, float) metrics = _sourceFloatMetrics?.Invoke() ?? (GetViewport().GetVisibleRect().Size.Y / (_world.PhoneLayout ? 675f : 1080f), 1f);
				float unit = Math.Max(0.01f, metrics.Item1);
				float textScale = Math.Clamp(metrics.Item2, 1f, 1.3f);
				if (actor.SourceUiProxyHeight <= 0f)
				{
					RefreshActorLayout();
				}
				if (actor.SourceUiProxyHeight <= 0f)
				{
					return false;
				}
				Vector2 center = SourceFloatProxyModel.PlayerDamageOrigin(_world.Camera.UnprojectPosition(actor.Home + actor.UiLunge.Offset), actor.SourceUiProxyHeight, actor.ExecutionScale.Y, actor.SourceUiProxyUnit);
				Vector2 size = new Vector2(240f, 60f) * unit;
				int fontSize = Math.Max(1, (int)Math.Round((float)SourceCombatFloatModel.PlayerSize(hit.Amount, hit.Exposed == true) * unit * textScale));
				Label label = new Label
				{
					Name = "SourceDamageFloat",
					Text = hit.Amount.ToString(CultureInfo.InvariantCulture),
					Position = center - size / 2f,
					Size = size,
					PivotOffset = size / 2f,
					ZIndex = 50,
					MouseFilter = Control.MouseFilterEnum.Ignore,
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center,
					ClipText = false
				};
				label.AddThemeFontOverride("font", SourceRunUiDecor.Display);
				label.AddThemeFontSizeOverride("font_size", fontSize);
				label.AddThemeColorOverride("font_color", new Color(SourceCombatFloatModel.PlayerColor(hit.Amount, loss, absorbed)));
				label.AddThemeColorOverride("font_outline_color", new Color(0.05f, 0.03f, 0.06f, 0.95f));
				label.AddThemeConstantOverride("outline_size", Math.Max(1, (int)Math.Round((float)fontSize * 0.055f)));
				label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.75f));
				label.AddThemeConstantOverride("shadow_offset_x", Math.Max(1, (int)Math.Round(2f * unit)));
				label.AddThemeConstantOverride("shadow_offset_y", Math.Max(1, (int)Math.Round(2f * unit)));
				label.SetMeta("source_role", "player_damage_float");
				label.SetMeta("source_amount", hit.Amount);
				label.SetMeta("source_hp_loss", loss);
				_sourceFloatHost.AddChild(label, forceReadableName: false, InternalMode.Disabled);
				_sourceFloats.Add(new SourceFloat(label, center, size, unit, _clock));
				SourceCombatFloatsCreated++;
				LastSourceCombatFloatText = label.Text;
				SampleSourceCombatText();
				return true;
			}
		}
		return false;
	}

	private void SampleSourceCombatText()
	{
		SampleSourceEnemyBlockText();
		SampleSourceIncomingText();
		SampleSourceHeroBlockText();
		for (int i = _sourceFloats.Count - 1; i >= 0; i--)
		{
			SourceFloat cue = _sourceFloats[i];
			float age = _clock - cue.Start;
			if (!GodotObject.IsInstanceValid(cue.Label))
			{
				_sourceFloats.RemoveAt(i);
			}
			else if (age >= 0.9f || _interfaceRoute != "combat")
			{
				cue.Label.QueueFree();
				_sourceFloats.RemoveAt(i);
			}
			else
			{
				cue.Label.Position = cue.Origin - cue.Size / 2f - new Vector2(0f, SourceCombatFloatModel.Offset(age) * cue.Unit);
				cue.Label.Scale = Vector2.One * SourceCombatFloatModel.Scale(age);
				cue.Label.Modulate = new Color(1f, 1f, 1f, SourceCombatFloatModel.Alpha(age));
			}
		}
	}

	private void CancelSourceCombatText()
	{
		CancelSourceEnemyBlockText();
		CancelSourceIncomingText();
		CancelSourceHeroBlockText();
		foreach (SourceFloat cue in _sourceFloats)
		{
			if (GodotObject.IsInstanceValid(cue.Label))
			{
				cue.Label.QueueFree();
			}
		}
		_sourceFloats.Clear();
	}

	private void SyncSourceRetainers(GameState state)
	{
		CardInstance[] dolls = state.Player.Permanents.Where((CardInstance cardInstance) => cardInstance.Def.Retainer == true && cardInstance.Innate != true && !_sourceDollSeats.IsGone(cardInstance.Uid)).ToArray();
		_sourceDollSeats.ObserveMissing(dolls.Select((CardInstance cardInstance) => cardInstance.Uid), _retainers.Keys, _clock);
		_sourceDollSeats.BeginSync(_retainers.Keys);
		CardInstance[] array = dolls;
		foreach (CardInstance p in array)
		{
			if (!_sourceDollSeats.TryGet(p.Uid, out var seat))
			{
				string artPath = "res://assets/Art/dolls/" + p.Def.Id + ".png";
				Texture2D art = (ResourceLoader.Exists(artPath) ? GD.Load<Texture2D>(artPath) : null);
				if (!_sourceDollSeats.TryAllocate(p.Uid, art?.GetWidth() ?? 32, art?.GetHeight() ?? 32, out seat))
				{
					continue;
				}
			}
			Vector3 feet = SourceDollSeats.Feet(seat, _world.PhoneLayout);
			if (!_retainers.TryGetValue(p.Uid, out var actor))
			{
				actor = new BattleActor
				{
					Uid = p.Uid,
					IdleSlot = seat.Index
				};
				string artPath2 = "res://assets/Art/dolls/" + p.Def.Id + ".png";
				float height = (ResourceLoader.Exists(artPath2) ? ((float)GD.Load<Texture2D>(artPath2).GetHeight() * 0.0421875f) : 1.35f);
				actor.Configure("dolls", p.Def.Id, p.Def.Name, feet, height, _world.PhoneLayout);
				_actorRoot.AddChild(actor, forceReadableName: false, InternalMode.Disabled);
				actor.Vitals.Text = p.Def.Name;
				_retainers[p.Uid] = actor;
			}
			actor.Home = feet;
			actor.IdleSlot = seat.Index;
		}
	}

	private void RefreshSourceRetainerFeet()
	{
		foreach (KeyValuePair<string, BattleActor> kv in _retainers)
		{
			if (_sourceDollSeats.TryGet(kv.Key, out var seat))
			{
				kv.Value.Home = SourceDollSeats.Feet(seat, _world.PhoneLayout);
			}
		}
	}

	private void AdvanceSourceRetainerAbsences()
	{
		string[] array = _sourceDollSeats.DueMissing(_clock);
		foreach (string uid in array)
		{
			if (_sourceDollSeats.Kill(uid) && _retainers.Remove(uid, out var actor))
			{
				_actorRoot.RemoveChild(actor);
				_retiredRoot.AddChild(actor, forceReadableName: false, InternalMode.Disabled);
				StartDeath(actor, retired: true);
				_cues.Show("灯が消えた", actor.Contact + Vector3.Up * 0.6f, new Color("c4beb2"));
			}
		}
	}

	private void ClearSourceRetainerBoard()
	{
		_sourceDollSeats.Reset();
		foreach (BattleActor actor in _retainers.Values)
		{
			_deaths.Remove(actor);
			_actorRoot.RemoveChild(actor);
			actor.QueueFree();
		}
		_retainers.Clear();
		if (_retiredRoot != null)
		{
			BattleActor[] array = (from a in _retiredRoot.GetChildren().OfType<BattleActor>()
				where !string.IsNullOrEmpty(a.Uid)
				select a).ToArray();
			foreach (BattleActor actor2 in array)
			{
				_deaths.Remove(actor2);
				_retiredRoot.RemoveChild(actor2);
				actor2.QueueFree();
			}
		}
	}

	private bool TryShowSourceEnemyBlock(GameEvent_BlockGained gained, BattleActor actor, CombatPresentationReceipt? receipt)
	{
		if (gained.Target == "player" || receipt?.Target != "enemy" || actor.EnemyIndex != receipt.EnemyIndex || _sourceFloatHost == null || !GodotObject.IsInstanceValid(_sourceFloatHost))
		{
			return false;
		}
		if (actor.SourceUiProxyHeight <= 0f)
		{
			RefreshActorLayout();
		}
		if (actor.SourceUiProxyHeight <= 0f || actor.SourceUiProxyUnit <= 0f)
		{
			return false;
		}
		float unit = actor.SourceUiProxyUnit;
		float textScale = Math.Clamp(_sourceFloatMetrics?.Invoke().TextScale ?? 1f, 1f, 1.3f);
		Vector2 origin = SourceEnemyBlockFloatModel.Origin(_world.Camera.UnprojectPosition(actor.Home + actor.UiLunge.Offset), actor.SourceUiProxyHeight, actor.ExecutionScale.Y, unit);
		Vector2 size = new Vector2(240f, 60f) * unit;
		int fontSize = Math.Max(1, (int)Math.Round(28f * unit * textScale));
		Label label = new Label
		{
			Name = "SourceEnemyBlockFloat",
			Text = SourceEnemyBlockFloatModel.Text(gained.Amount),
			Position = origin - size / 2f,
			Size = size,
			PivotOffset = size / 2f,
			ZIndex = 50,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			ClipText = false
		};
		label.AddThemeFontOverride("font", SourceRunUiDecor.Display);
		label.AddThemeFontSizeOverride("font_size", fontSize);
		label.AddThemeColorOverride("font_color", new Color("d6e6fa"));
		label.AddThemeColorOverride("font_outline_color", new Color(0.05f, 0.03f, 0.06f, 0.95f));
		label.AddThemeConstantOverride("outline_size", Math.Max(1, (int)Math.Round((float)fontSize * 0.055f)));
		label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.75f));
		label.AddThemeConstantOverride("shadow_offset_x", Math.Max(1, (int)Math.Round(2f * unit)));
		label.AddThemeConstantOverride("shadow_offset_y", Math.Max(1, (int)Math.Round(2f * unit)));
		label.SetMeta("source_role", "enemy_block_float");
		_sourceFloatHost.AddChild(label, forceReadableName: false, InternalMode.Disabled);
		_sourceEnemyBlockFloats.Add(new SourceEnemyBlockFloat(label, gained, GetEventChange(gained)?.Revision ?? (-1), _actionQueue.Generation, actor.EnemyIndex, origin, size, unit, _clock));
		SampleSourceEnemyBlockText();
		return true;
	}

	private void SampleSourceEnemyBlockText()
	{
		for (int i = _sourceEnemyBlockFloats.Count - 1; i >= 0; i--)
		{
			SourceEnemyBlockFloat cue = _sourceEnemyBlockFloats[i];
			float age = _clock - cue.Start;
			if (!GodotObject.IsInstanceValid(cue.Label))
			{
				_sourceEnemyBlockFloats.RemoveAt(i);
			}
			else if (SourceEnemyBlockFloatModel.Expired(age, cue.Generation, _actionQueue.Generation, _interfaceRoute))
			{
				cue.Label.QueueFree();
				_sourceEnemyBlockFloats.RemoveAt(i);
			}
			else
			{
				cue.Label.Position = cue.Origin - cue.Size / 2f - new Vector2(0f, SourceEnemyBlockFloatModel.Offset(age) * cue.Unit);
				cue.Label.Scale = Vector2.One * SourceEnemyBlockFloatModel.Scale(age);
				cue.Label.Modulate = new Color(1f, 1f, 1f, SourceEnemyBlockFloatModel.Alpha(age));
			}
		}
	}

	private void CancelSourceEnemyBlockText()
	{
		foreach (SourceEnemyBlockFloat cue in _sourceEnemyBlockFloats)
		{
			if (GodotObject.IsInstanceValid(cue.Label))
			{
				cue.Label.QueueFree();
			}
		}
		_sourceEnemyBlockFloats.Clear();
	}

	private bool TryShowSourceHeroBlock(GameEvent_BlockGained gained, BattleActor actor, CombatPresentationReceipt? receipt)
	{
		if (!SourceHeroBlockFloatModel.Ordinary(gained.Target, gained.SourceUid, receipt?.Target, gained.Amount) || (object)receipt == null || (object)receipt != gained.PresentationReceipt || !CombatPresentationLedger.Valid(gained, receipt, out var _) || _interfaceRoute != "combat" || actor != _hero || !GodotObject.IsInstanceValid(actor) || _sourceFloatHost == null || !GodotObject.IsInstanceValid(_sourceFloatHost))
		{
			return false;
		}
		if (!_receipts.TryGetValue(gained, out var tracked) || !tracked.Committed || !tracked.Exact || tracked.Reason != null || (object)tracked.Values != receipt || !SourceHeroBlockFloatModel.ExactGain(gained.Amount, receipt.Before.Block, receipt.After.Block, receipt.Before.Hp, receipt.After.Hp, receipt.Before.MaxHp, receipt.After.MaxHp, receipt.Before.IceBlock, receipt.After.IceBlock))
		{
			return false;
		}
		SessionChange change = GetEventChange(gained);
		if ((object)change == null)
		{
			return false;
		}
		int generation = _actionQueue.Generation;
		foreach (SourceHeroBlockFloat prior in _sourceHeroBlockFloats)
		{
			if (prior.Generation == generation && (object)prior.Event == gained)
			{
				return true;
			}
		}
		if (actor.SourceUiProxyHeight <= 0f)
		{
			RefreshActorLayout();
		}
		float height = actor.SourceUiProxyHeight;
		float unit = actor.SourceUiProxyUnit;
		float scaleY = actor.ExecutionScale.Y;
		if (!float.IsFinite(height) || height <= 0f || !float.IsFinite(unit) || unit <= 0f || !float.IsFinite(scaleY) || scaleY <= 0f)
		{
			return false;
		}
		Vector2 origin = SourceHeroBlockFloatModel.Origin(_world.Camera.UnprojectPosition(actor.Home + actor.UiLunge.Offset), height, scaleY, unit);
		if (!origin.IsFinite())
		{
			return false;
		}
		float textScale = Math.Clamp(_sourceFloatMetrics?.Invoke().TextScale ?? 1f, 1f, 1.3f);
		int fontSize = Math.Max(1, (int)Math.Round(30f * unit * textScale));
		Vector2 size = new Vector2(240f, 60f) * unit;
		Label label = new Label
		{
			Name = "SourceHeroBlockFloat",
			Text = SourceHeroBlockFloatModel.Text(gained.Amount),
			Position = origin - size / 2f,
			Size = size,
			PivotOffset = size / 2f,
			ZIndex = 50,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			ClipText = false
		};
		label.AddThemeFontOverride("font", SourceRunUiDecor.Display);
		label.AddThemeFontSizeOverride("font_size", fontSize);
		label.AddThemeColorOverride("font_color", new Color("6f95b8"));
		label.AddThemeColorOverride("font_outline_color", new Color(0.05f, 0.03f, 0.06f, 0.95f));
		label.AddThemeConstantOverride("outline_size", Math.Max(1, (int)Math.Round((float)fontSize * 0.055f)));
		label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.75f));
		label.AddThemeConstantOverride("shadow_offset_x", Math.Max(1, (int)Math.Round(2f * unit)));
		label.AddThemeConstantOverride("shadow_offset_y", Math.Max(1, (int)Math.Round(2f * unit)));
		label.SetMeta("source_role", "hero_ordinary_block_float");
		_sourceFloatHost.AddChild(label, forceReadableName: false, InternalMode.Disabled);
		_sourceHeroBlockFloats.Add(new SourceHeroBlockFloat(label, gained, change.Revision, generation, actor.GetInstanceId(), origin, size, unit, _clock));
		SampleSourceHeroBlockText();
		return true;
	}

	private void SampleSourceHeroBlockText()
	{
		for (int i = _sourceHeroBlockFloats.Count - 1; i >= 0; i--)
		{
			SourceHeroBlockFloat cue = _sourceHeroBlockFloats[i];
			float age = _clock - cue.Start;
			if (!GodotObject.IsInstanceValid(cue.Label))
			{
				_sourceHeroBlockFloats.RemoveAt(i);
			}
			else if (SourceHeroBlockFloatModel.Expired(age, cue.Generation, _actionQueue.Generation, _interfaceRoute))
			{
				cue.Label.Visible = false;
				cue.Label.QueueFree();
				_sourceHeroBlockFloats.RemoveAt(i);
			}
			else
			{
				cue.Label.Position = cue.Origin - cue.Size / 2f - new Vector2(0f, SourceHeroBlockFloatModel.Offset(age) * cue.Unit);
				cue.Label.Scale = Vector2.One * SourceHeroBlockFloatModel.Scale(age);
				cue.Label.Modulate = new Color(1f, 1f, 1f, SourceHeroBlockFloatModel.Alpha(age));
			}
		}
	}

	private void CancelSourceHeroBlockText()
	{
		foreach (SourceHeroBlockFloat cue in _sourceHeroBlockFloats)
		{
			if (GodotObject.IsInstanceValid(cue.Label))
			{
				cue.Label.Visible = false;
				cue.Label.QueueFree();
			}
		}
		_sourceHeroBlockFloats.Clear();
	}

	private bool TryShowSourceIncomingDamage(GameEvent_DamageDealt hit, int loss, int absorbed, int amount, bool individual, int ordinal)
	{
		if (hit.Source != "enemy" || _interfaceRoute != "combat" || _sourceFloatHost == null || !GodotObject.IsInstanceValid(_sourceFloatHost) || _hero == null || !GodotObject.IsInstanceValid(_hero) || hit.PresentationReceipt?.Target != "player" || !CombatPresentationLedger.Valid(hit, hit.PresentationReceipt, out var _))
		{
			return false;
		}
		if (!individual && (hit.PresentationEnemyAttack?.ExecutedHits ?? 0) > 1)
		{
			return false;
		}
		if (amount < 0 || loss < 0 || absorbed < 0)
		{
			return false;
		}
		if (!individual)
		{
			CombatPresentationReceipt receipt = hit.PresentationReceipt;
			absorbed = CombatPresentationLedger.Display(Math.Max(0L, (long)receipt.Before.Block - (long)receipt.After.Block) + Math.Max(0L, (long)receipt.Before.IceBlock.GetValueOrDefault() - (long)receipt.After.IceBlock.GetValueOrDefault()));
		}
		int generation = _actionQueue.Generation;
		foreach (SourceIncomingFloat prior in _sourceIncomingFloats)
		{
			if ((object)prior.Event == hit && prior.Generation == generation && prior.Individual == individual && prior.ContributionOrdinal == ordinal)
			{
				return true;
			}
		}
		string outcome;
		if (individual)
		{
			if (loss > 0)
			{
				outcome = "hit";
			}
			else
			{
				outcome = ((absorbed > 0) ? "guard" : "nullified");
			}
		}
		else
		{
			outcome = CombatOutcome.Read(hit).Kind;
		}
		SourceIncomingFloatModel.Spec primary = (individual ? SourceIncomingFloatModel.IndividualPrimary(amount, loss, outcome) : SourceIncomingFloatModel.AggregatePrimary(amount, loss, outcome));
		if ((object)primary != null)
		{
			primary = primary with
			{
				Rise = 45f
			};
		}
		SourceIncomingFloatModel.Spec absorption = SourceIncomingFloatModel.Absorption(absorbed);
		if ((object)primary == null && (object)absorption == null)
		{
			return true;
		}
		if (_hero.SourceUiProxyUnit <= 0f)
		{
			RefreshActorLayout();
		}
		float unit = _hero.SourceUiProxyUnit;
		if (!float.IsFinite(unit) || unit <= 0f)
		{
			return false;
		}
		float textScale = Math.Clamp(_sourceFloatMetrics?.Invoke().TextScale ?? 1f, 1f, 1.3f);
		Rect2 viewport = GetViewport().GetVisibleRect();
		Vector2 sourceOrigin = SourceIncomingFloatModel.Origin(_world.Camera.UnprojectPosition(_hero.Home).X, viewport.Size.Y, unit, _world.PhoneLayout) + new Vector2(0f, viewport.Position.Y);
		Rect2 safe = SourceIncomingSafeRect(viewport, unit);
		List<SourceIncomingLabel> labels = new List<SourceIncomingLabel>(2);
		List<SourceIncomingFloatModel.EnvelopeLabel> envelopes = new List<SourceIncomingFloatModel.EnvelopeLabel>(2);
		List<Rect2> localSweeps = new List<Rect2>(2);
		SourceIncomingFloatModel.Spec[] array = new SourceIncomingFloatModel.Spec[2] { primary, absorption };
		foreach (SourceIncomingFloatModel.Spec spec in array)
		{
			if ((object)spec != null)
			{
				int fontSize = Math.Max(1, (int)Math.Round((float)spec.FontSize * unit * textScale));
				int outline = Math.Max(1, (int)Math.Round((float)fontSize * 0.055f));
				int shadow = Math.Max(1, (int)Math.Round(2f * unit));
				Label label = new Label
				{
					Name = "SourceIncomingFloat",
					Text = spec.Text,
					Visible = false,
					ZIndex = 50,
					MouseFilter = Control.MouseFilterEnum.Ignore,
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center,
					ClipText = false
				};
				label.AddThemeFontOverride("font", SourceRunUiDecor.Display);
				label.AddThemeFontSizeOverride("font_size", fontSize);
				label.AddThemeColorOverride("font_color", new Color(spec.Color));
				label.AddThemeColorOverride("font_outline_color", new Color(0.05f, 0.03f, 0.06f, 0.95f));
				label.AddThemeConstantOverride("outline_size", outline);
				label.AddThemeColorOverride("font_shadow_color", new Color(0f, 0f, 0f, 0.75f));
				label.AddThemeConstantOverride("shadow_offset_x", shadow);
				label.AddThemeConstantOverride("shadow_offset_y", shadow);
				label.SetMeta("source_role", (spec.Delay > 0f) ? "incoming_absorption" : "incoming_primary");
				label.SetMeta("source_authored_rise", (spec.Delay > 0f) ? spec.Rise : 60f);
				label.SetMeta("source_effective_rise", spec.Rise);
				_sourceFloatHost.AddChild(label, forceReadableName: false, InternalMode.Disabled);
				Vector2 minimum = label.GetCombinedMinimumSize();
				Vector2 size = (label.Size = new Vector2(Math.Max(240f * unit, minimum.X), Math.Max(60f * unit, minimum.Y)));
				label.PivotOffset = size / 2f;
				SourceIncomingGlyphBounds.Measurement glyphs = SourceIncomingGlyphBounds.Measure(label);
				Rect2 ink = glyphs.Bounds;
				Rect2 sweep = SourceIncomingRegionModel.SweptInk(new Rect2(ink.Position - size / 2f, ink.Size), spec.LocalY * unit, spec.Rise * unit, spec.Duration);
				labels.Add(new SourceIncomingLabel(label, spec, size, ink.Size, ink, sweep, glyphs)
				{
					Started = ((spec.Delay > 0f) ? ((float?)null) : new float?(_clock))
				});
				localSweeps.Add(sweep);
				Vector2 measured = size + Vector2.One * (2 * (outline + shadow));
				envelopes.Add(new SourceIncomingFloatModel.EnvelopeLabel(measured, new Vector2(0f, spec.LocalY * unit), spec.Delay, spec.Rise * unit, spec.Duration));
			}
		}
		if (!SourceIncomingFloatModel.TryPlace(sourceOrigin, safe, envelopes, out var origin, out var envelope))
		{
			foreach (SourceIncomingLabel item2 in labels)
			{
				item2.Label.QueueFree();
			}
			return false;
		}
		long revision = GetEventChange(hit)?.Revision ?? (-1);
		SourceIncomingRegionGeometry geometry = null;
		List<Rect2> occupied = new List<Rect2>();
		foreach (SourceIncomingFloat prior2 in _sourceIncomingFloats)
		{
			if (prior2.Generation != generation)
			{
				continue;
			}
			bool alive = false;
			foreach (SourceIncomingLabel item in prior2.Labels)
			{
				if (GodotObject.IsInstanceValid(item.Label) && !item.Label.IsQueuedForDeletion())
				{
					float? started = item.Started;
					if (!started.HasValue || !SourceIncomingFloatModel.Expired(_clock - item.Started.Value, item.Spec.Duration, prior2.Generation, generation, _interfaceRoute))
					{
						alive = true;
						break;
					}
				}
			}
			if (!alive)
			{
				continue;
			}
			foreach (Rect2 sweep2 in prior2.SweptRects)
			{
				occupied.Add(sweep2);
			}
			if (prior2.Revision == revision && prior2.ActorInstance == _hero.GetInstanceId() && (object)geometry == null)
			{
				geometry = prior2.RegionGeometry;
			}
		}
		if ((object)geometry == null)
		{
			geometry = MeasureSourceIncomingRegion(safe, unit);
		}
		List<Rect2> avoidance = geometry.AvoidanceRects;
		Rect2 localEnvelope = localSweeps[0];
		for (int j = 1; j < localSweeps.Count; j++)
		{
			localEnvelope = localEnvelope.Merge(localSweeps[j]);
		}
		Vector2 preferred = (((object)absorption != null) ? (geometry.Region.End - localEnvelope.End) : (geometry.Region.Position - localEnvelope.Position));
		bool collisionFree = false;
		bool avoidanceClear = false;
		bool regionContained = false;
		string placementStatus = "ViewportFallback";
		IReadOnlyList<Rect2> sweptRects = new Rect2[1] { envelope };
		if (SourceIncomingRegionModel.TryPlace(preferred, geometry.Region, localSweeps, occupied, avoidance, 6f * unit, out var placed) && placed.RegionContained)
		{
			origin = placed.Origin;
			envelope = placed.Envelope;
			sweptRects = placed.SweptRects;
			collisionFree = placed.CollisionFree;
			avoidanceClear = placed.AvoidanceClear;
			regionContained = true;
			placementStatus = placed.Status.ToString();
		}
		_sourceIncomingFloats.Add(new SourceIncomingFloat(hit, revision, generation, ordinal, individual, _hero.GetInstanceId(), amount, loss, absorbed, sourceOrigin, origin, safe, envelope, unit, _clock, labels, envelope, collisionFree, avoidanceClear, avoidance, sweptRects, geometry.Region, geometry, regionContained, placementStatus));
		SampleSourceIncomingText();
		return true;
	}

	private SourceIncomingRegionGeometry MeasureSourceIncomingRegion(Rect2 safe, float unit)
	{
		BattleActor hero = _hero;
		Sprite3D sprite = hero.Sprite;
		Rect2 quad;
		using (TriangleMesh mesh = sprite.GenerateTriangleMesh())
		{
			Vector2 low = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
			Vector2 high = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
			Vector3[] array = mesh?.GetFaces() ?? Array.Empty<Vector3>();
			foreach (Vector3 vertex in array)
			{
				Vector2 q = _world.Camera.UnprojectPosition(sprite.GlobalTransform * vertex);
				low = low.Min(q);
				high = high.Max(q);
			}
			quad = ((low.IsFinite() && high.IsFinite() && high.X > low.X && high.Y > low.Y) ? new Rect2(low, high - low) : safe);
		}
		Rect2 body = quad;
		string mode = "ConservativeQuad";
		SourceIncomingHeroBounds.Metadata? metadata = null;
		if (sprite.Axis == Vector3.Axis.Z && sprite.Billboard == BaseMaterial3D.BillboardModeEnum.Disabled && SourceIncomingHeroBounds.TryRead(sprite.Texture, out var match, out var verification))
		{
			Rect2 pixels = match.OpaquePixels;
			if (sprite.FlipH)
			{
				pixels.Position = new Vector2((float)match.TextureSize.X - pixels.End.X, pixels.Position.Y);
			}
			if (sprite.FlipV)
			{
				pixels.Position = new Vector2(pixels.Position.X, (float)match.TextureSize.Y - pixels.End.Y);
			}
			Rect2 item = sprite.GetItemRect();
			Rect2 local = pixels;
			Vector2 low2 = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
			Vector2 high2 = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
			Vector2[] array2 = new Vector2[4]
			{
				local.Position,
				new Vector2(local.End.X, local.Position.Y),
				local.End,
				new Vector2(local.Position.X, local.End.Y)
			};
			foreach (Vector2 p in array2)
			{
				Vector2 q2 = _world.Camera.UnprojectPosition(sprite.GlobalTransform * SourceIncomingHeroBounds.PixelVertex(item, p, sprite.PixelSize));
				low2 = low2.Min(q2);
				high2 = high2.Max(q2);
			}
			if (low2.IsFinite() && high2.IsFinite() && high2.X > low2.X && high2.Y > low2.Y)
			{
				body = new Rect2(low2, high2 - low2);
				metadata = match;
				mode = verification.ToString();
			}
		}
		Vector2 feet = _world.Camera.UnprojectPosition(hero.Home);
		float nearest = safe.End.X;
		bool hasEnemy = false;
		foreach (BattleActor enemy in _enemies.Values)
		{
			if (GodotObject.IsInstanceValid(enemy) && enemy.Visible && !(enemy.DissolveAmount >= 1f))
			{
				float x = _world.Camera.UnprojectPosition(enemy.Home).X;
				if (float.IsFinite(x) && !(x <= feet.X) && (!hasEnemy || x < nearest))
				{
					nearest = x;
					hasEnemy = true;
				}
			}
		}
		float header = safe.Position.Y - 6f * unit;
		float handTop = safe.End.Y;
		List<Rect2> avoidance = new List<Rect2> { body };
		if (_sourceFloatHost != null && GodotObject.IsInstanceValid(_sourceFloatHost))
		{
			Read(_sourceFloatHost, inHeader: false);
		}
		float left = Math.Max(safe.Position.X, body.Position.X);
		float right = Math.Min(safe.End.X, hasEnemy ? ((feet.X + nearest) * 0.5f) : (feet.X + body.Size.X * 1.5f));
		float intentLeft = float.PositiveInfinity;
		foreach (Rect2 rect in avoidance)
		{
			if (rect.Position.X > body.End.X)
			{
				intentLeft = Math.Min(intentLeft, rect.Position.X);
			}
		}
		if (hasEnemy && float.IsFinite(intentLeft))
		{
			right = Math.Min(safe.End.X, (body.End.X + intentLeft) * 0.5f);
		}
		float top = Math.Max(safe.Position.Y, header + 6f * unit);
		float bottom = Math.Min(safe.End.Y, Math.Min(handTop - 6f * unit, body.End.Y + body.Size.Y));
		return new SourceIncomingRegionGeometry(new Rect2(new Vector2(left, top), new Vector2(Math.Max(0f, right - left), Math.Max(0f, bottom - top))), body, quad, feet, header, handTop, nearest, hasEnemy, sprite.Texture?.ResourcePath ?? "", mode, metadata, avoidance);
		static Rect2 Bounds(Control control)
		{
			Transform2D transform = control.GetGlobalTransformWithCanvas();
			Vector2 low3 = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
			Vector2 high3 = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
			Vector2[] array3 = new Vector2[4]
			{
				Vector2.Zero,
				new Vector2(control.Size.X, 0f),
				control.Size,
				new Vector2(0f, control.Size.Y)
			};
			foreach (Vector2 p2 in array3)
			{
				Vector2 q3 = transform * p2;
				low3 = low3.Min(q3);
				high3 = high3.Max(q3);
			}
			return new Rect2(low3, high3 - low3);
		}
		void Read(Node node, bool inHeader)
		{
			foreach (Node child in node.GetChildren())
			{
				if (GodotObject.IsInstanceValid(child) && !child.IsQueuedForDeletion())
				{
					bool childHeader = inHeader || child.Name == (StringName)"BattleHeader";
					if (child is Control control && control.IsVisibleInTree())
					{
						Rect2 rect2 = Bounds(control);
						if (rect2.Position.IsFinite() && rect2.Size.IsFinite() && rect2.Size.X > 0f && rect2.Size.Y > 0f)
						{
							if (childHeader && child.Name != (StringName)"BattleHeader")
							{
								header = Math.Max(header, rect2.End.Y);
							}
							if (control is CardTile { IsBattleHand: not false })
							{
								handTop = Math.Min(handTop, rect2.Position.Y);
							}
							if (control.HasMeta("hud_role") && control.GetMeta("hud_role").AsString() == "intent")
							{
								avoidance.Add(rect2);
							}
						}
					}
					Read(child, childHeader);
				}
			}
		}
	}

	private Rect2 SourceIncomingSafeRect(Rect2 viewport, float unit)
	{
		float left = 6f * unit;
		float right = left;
		float bottom = left;
		float top = (float)((_world.PhoneLayout ? 56 : 72) + 6) * unit;
		if (OS.HasFeature("mobile"))
		{
			Rect2I safe = DisplayServer.GetDisplaySafeArea();
			Vector2I screen = DisplayServer.ScreenGetSize();
			if (screen.X > 0 && screen.Y > 0 && safe.Size.X > 0 && safe.Size.Y > 0)
			{
				float sx = viewport.Size.X / (float)screen.X;
				float sy = viewport.Size.Y / (float)screen.Y;
				left += (float)Math.Max(0, safe.Position.X) * sx;
				right += (float)Math.Max(0, screen.X - safe.End.X) * sx;
				top += (float)Math.Max(0, safe.Position.Y) * sy;
				bottom += (float)Math.Max(0, screen.Y - safe.End.Y) * sy;
			}
		}
		return new Rect2(viewport.Position + new Vector2(left, top), viewport.Size - new Vector2(left + right, top + bottom));
	}

	private void SampleSourceIncomingText()
	{
		for (int i = _sourceIncomingFloats.Count - 1; i >= 0; i--)
		{
			SourceIncomingFloat cue = _sourceIncomingFloats[i];
			bool alive = false;
			foreach (SourceIncomingLabel item in cue.Labels)
			{
				if (!GodotObject.IsInstanceValid(item.Label))
				{
					continue;
				}
				if (cue.Generation != _actionQueue.Generation || _interfaceRoute != "combat")
				{
					item.Label.Visible = false;
					item.Label.QueueFree();
					continue;
				}
				float? started = item.Started;
				if (!started.HasValue)
				{
					if (_clock < cue.Start + item.Spec.Delay)
					{
						alive = true;
						continue;
					}
					item.Started = _clock;
				}
				float age = _clock - item.Started.Value;
				if (SourceIncomingFloatModel.Expired(age, item.Spec.Duration, cue.Generation, _actionQueue.Generation, _interfaceRoute))
				{
					item.Label.Visible = false;
					item.Label.QueueFree();
					continue;
				}
				alive = true;
				item.Label.Visible = true;
				item.Label.Position = cue.Origin - item.Size / 2f + new Vector2(0f, item.Spec.LocalY * cue.Unit - SourceIncomingFloatModel.Offset(age, item.Spec.Rise, item.Spec.Duration) * cue.Unit);
				item.Label.Scale = Vector2.One * SourceIncomingFloatModel.Scale(age);
				item.Label.Modulate = new Color(1f, 1f, 1f, SourceIncomingFloatModel.Alpha(age, item.Spec.Duration));
			}
			if (!alive)
			{
				_sourceIncomingFloats.RemoveAt(i);
			}
		}
	}

	private void CancelSourceIncomingText()
	{
		foreach (SourceIncomingFloat sourceIncomingFloat in _sourceIncomingFloats)
		{
			foreach (SourceIncomingLabel item in sourceIncomingFloat.Labels)
			{
				if (GodotObject.IsInstanceValid(item.Label))
				{
					item.Label.Visible = false;
					item.Label.QueueFree();
				}
			}
		}
		_sourceIncomingFloats.Clear();
	}

	private float AdvanceFinishClock(float hostDelta)
	{
		if (_finishSlowRemaining <= 0f)
		{
			return hostDelta;
		}
		float slowHost = Math.Min(hostDelta, _finishSlowRemaining / 0.38f);
		float slow = slowHost * 0.38f;
		_finishSlowRemaining = Math.Max(0f, _finishSlowRemaining - slow);
		return slow + hostDelta - slowHost;
	}

	public int? GetPresentedIncomingAmount(GameEvent e)
	{
		if ((object)e != _incomingEvent)
		{
			return null;
		}
		return _incomingAmount;
	}

	public Vector2? ProjectLeaderFeet()
	{
		if (_hero != null)
		{
			Camera3D camera = _world?.Camera;
			if (camera != null && camera.IsInsideTree())
			{
				return camera.UnprojectPosition(_hero.Home);
			}
		}
		return null;
	}

	public float GetCardRetirementSeconds(string cardId)
	{
		if (SourceShowcaseEnabled)
		{
			SourceShowcaseProfile p = SourceShowcaseProfiles.ForCard(cardId);
			if ((object)p != null)
			{
				return Math.Min(0.1f, p.Contact * 0.7f);
			}
		}
		return 0.25f;
	}

	private IEnumerable<BattleActor> AllPresentationActors()
	{
		if (_actorRoot != null)
		{
			return _actorRoot.GetChildren().OfType<BattleActor>().Concat(_retiredRoot?.GetChildren().OfType<BattleActor>() ?? Enumerable.Empty<BattleActor>());
		}
		return Enumerable.Empty<BattleActor>();
	}

	// UI observers cannot own or strand the contact-completion fence. Never replay them.
	private void PublishPresentationObservers(Action<GameEvent>? observers, GameEvent e, string diagnostic)
	{
		if (observers == null) return;
		int observerGeneration = _actionQueue.Generation;
		foreach (Action<GameEvent> observer in observers.GetInvocationList())
		{
			try { observer(e); }
			catch (Exception ex) { try { _session?.RecordFreezeEvent(diagnostic, false, ex); } catch { /* Diagnostics cannot strand presentation. */ } }
			if (observerGeneration != _actionQueue.Generation) break;
		}
	}

	private void PublishNumericContact(GameEvent e, PresentationBeat beat)
	{
		if (e is GameEvent_DamageDealt { Source: "enemy" } h && !LastEventReceiptRejected)
		{
			_incomingEvent = e;
			_incomingAmount = (_projectionActive ? beat.Contacts[BeatContactOrdinal].AmountContribution : h.Amount);
		}
		try
		{
			PublishPresentationObservers(NumericContactPresented, e, "BattleStage.NumericContactObserver");
		}
		finally
		{
			_incomingEvent = null;
			_incomingAmount = null;
		}
	}

	private static bool OwnsDirectHit(GameEvent_DamageDealt h, string cardId)
	{
		if (h.Source == "player")
		{
			DamagePresentationSource source = h.PresentationSource;
			if ((object)source == null)
			{
				return h.SourceUid == null;
			}
			if (source.Kind == "card")
			{
				return source.DefinitionId == cardId;
			}
			return false;
		}
		return false;
	}

	private void PlanActorAction(ActionPlayback action)
	{
		GameEvent_CardPlayed card = action.PrimaryContinuationCard ?? action.Beats.Select((OrdinaryBeat ordinaryBeat) => ordinaryBeat.Beat.Event).OfType<GameEvent_CardPlayed>().FirstOrDefault();
		if ((object)card == null)
		{
			return;
		}
		action.Card = card;
		List<OrdinaryBeat> hits = action.Beats.Where((OrdinaryBeat ordinaryBeat) => ordinaryBeat.Beat.Event is GameEvent_DamageDealt h && OwnsDirectHit(h, card.CardId)
            && (action.PrimaryContinuationUid==null || h.PresentationSource is {Kind:"card"} source && source.Uid==action.PrimaryContinuationUid)).ToList();
		if (hits.Count == 0)
		{
			return;
		}
		action.Profile = (SourceShowcaseEnabled ? SourceShowcaseProfiles.ForCard(card.CardId) : null);
		HashSet<int> seen = new HashSet<int>();
		int volley = 0;
		foreach (OrdinaryBeat item in hits)
		{
			int target = ((GameEvent_DamageDealt)item.Beat.Event).EnemyIndex ?? (-1);
			if (seen.Contains(target))
			{
				volley++;
				seen.Clear();
			}
            item.TargetOrdinal=seen.Count;
			seen.Add(target);
			item.Volley = volley;
			item.Direct = true;
		}
		action.Volleys = volley + 1;
        action.VisualVolleys=action.Volleys;
        var physicalProfile=PhysicalVfxProfiles.ForCard(card.CardId);
        var elementalProfile=ElementalVfxProfiles.ForCard(card.CardId);
        if(Hd2dAttackPrototype.Enabled && physicalProfile?.TargetPattern==PhysicalVfxTargetPattern.ResolvedSweep)
            action.VisualVolleys=Math.Max(1,hits.Where(row=>row.Beat.Event is GameEvent_DamageDealt h && (h.Amount>0 || h.HpLoss>0 || CombatOutcome.Read(h).Absorbed>0)).Select(row=>row.Volley+1).DefaultIfEmpty(1).Max());
		SourceShowcaseProfile profile = action.Profile;
		if ((object)profile == null)
		{
			return;
		}
        if(action.PrimaryContinuationCard!=null)
        {
            float originalFirst=action.Beats[0].ContactAt;
            float previous=0f;
            float previousOriginal=originalFirst;
            foreach(var beat in action.Beats)
            {
                float shifted=profile.Contact+Math.Max(0f,beat.ContactAt-originalFirst);
                float authored=beat.Direct ? profile.Contact+beat.Volley*profile.Interval
                    +beat.TargetOrdinal*(physicalProfile?.TargetSpacing??elementalProfile?.TargetSpacing??0f) : 0f;
                float original=beat.ContactAt;
                beat.ContactAt=Math.Max(previous+Math.Max(0f,original-previousOriginal),Math.Max(shifted,authored));
                beat.PreviewAt=Math.Max(0f,beat.ContactAt-(beat.Beat.Scenario?.Windup??0f));
                previous=beat.ContactAt;
                previousOriginal=original;
            }
            return;
        }
		float last = 0f;
		foreach (OrdinaryBeat b in action.Beats)
		{
			if (b.Beat.Event is GameEvent_CardPlayed)
			{
				b.ContactAt = 0f;
				continue;
			}
			if (b.Direct)
			{
				last = profile.Contact + (float)b.Volley * profile.Interval + (Hd2dAttackPrototype.Enabled ? b.TargetOrdinal*(physicalProfile?.TargetSpacing??elementalProfile?.TargetSpacing??0f):0f);
			}
			b.ContactAt = last;
			b.PreviewAt = Math.Max(0f, b.ContactAt - (b.Beat.Scenario?.Windup ?? 0f));
		}
	}

    private SourceShowcaseProfile PrototypeApproachProfile(BattleActor target, SourceShowcaseProfile profile)
    {
        if (_hero == null || _world.Camera == null || ProjectEnemyFootprint(-1) is not Rect2 heroBounds) return profile;
        var camera=_world.Camera;
        float targetX=camera.UnprojectPosition(target.Home).X;
        float available=float.PositiveInfinity;
        foreach(var peer in _enemies.Values)
        {
            if(peer==target || !peer.Visible || peer.DissolveAmount>=1f) continue;
            Rect2? bounds=peer.AuthoredFramesEnabled?ProjectAuthoredBounds(peer,true):ProjectEnemyFootprint(peer.EnemyIndex);
            if(bounds is not Rect2 other || other.GetCenter().X<=heroBounds.GetCenter().X || other.GetCenter().X>=targetX) continue;
            available=Math.Min(available,Math.Max(0,other.Position.X-heroBounds.End.X-heroBounds.Size.Y*.08f-8f));
        }
        if(!float.IsFinite(available)) return profile;
        Vector3 direction=target.Home-_hero.Home;direction.Y=0;direction=direction.Normalized();
        float requested=Math.Abs(camera.UnprojectPosition(_hero.Home+direction*(_hero.SourceMotionHeight*profile.Reach)).X-camera.UnprojectPosition(_hero.Home).X);
        return requested>.001f ? profile with { Reach=profile.Reach*Math.Clamp(available/requested,0,1) } : profile;
    }

    private readonly record struct PrototypeApproachPlan(SourceShowcaseProfile Profile,float Clearance,bool Remote,Vector3? Surface,string Reason,float DesiredTravel,float SafeTravel,float ReachableTravel);
    private PrototypeApproachPlan PrototypeStrikeApproach(BattleActor target,SourceShowcaseProfile profile)
    {
        if(_hero==null)return new(profile,0,false,null,"no_actor",0,0,0);
        float legacy=_hero.Height*.30f+target.Height*.18f;
        var fallback=PrototypeApproachProfile(target,profile);
        if(_world.Camera==null || !target.TryPrototypeFacingSurface(_hero.PrototypePlanningAxeContactPoint.Y,out var surface))
            return new(fallback,legacy,fallback.Reach<profile.Reach-.001f,null,"fallback_no_surface",0,0,0);
        var camera=_world.Camera;
        Vector3 delta=target.SourceMotionFeet-_hero.SourceMotionFeet;delta.Y=0;
        float distance=delta.Length();if(distance<.01f)return new(fallback,legacy,false,null,"coincident_roots",0,0,0);
        Vector3 direction=delta/distance;
        float pixelsPerUnit=camera.UnprojectPosition(_hero.Home+direction).X-camera.UnprojectPosition(_hero.Home).X;
        if(pixelsPerUnit<.001f)return new(fallback,legacy,fallback.Reach<profile.Reach-.001f,null,"invalid_projection",0,0,0);
        float TravelToScreenX(Vector3 anchor,float desiredX)
        {
            if(camera.UnprojectPosition(anchor).X>=desiredX)return 0;
            if(camera.UnprojectPosition(anchor+direction*distance).X<=desiredX)return distance;
            float lo=0,hi=distance;
            // Perspective projection is nonlinear over a full dash. Solve the
            // actual projected weapon/body point, not a one-unit derivative.
            for(int i=0;i<18;i++){float mid=(lo+hi)*.5f;if(camera.UnprojectPosition(anchor+direction*mid).X<desiredX)lo=mid;else hi=mid;}
            return(lo+hi)*.5f;
        }
        float surfaceX=camera.UnprojectPosition(surface).X;
        float contactTravel=TravelToScreenX(_hero.PrototypePlanningAxeContactPoint,surfaceX);
        float safeTravel=contactTravel;
        float bodyY=_hero.SourceMotionFeet.Y+_hero.Height*.65f;
        if(target.TryPrototypeFacingBodySurface(bodyY,out var upperSurface) && _hero.TryPrototypeBodyFront(bodyY,out var bodyFront))
        {
            float safeX=camera.UnprojectPosition(upperSurface).X-3f;
            safeTravel=Math.Min(safeTravel,TravelToScreenX(bodyFront,safeX));
        }
        // Reach only the selected near surface, never beyond its root or scene lane.
        // A non-target obstacle or body-clearance conflict keeps the explicit travel cue.
        float requiredReach=Math.Max(profile.Reach,contactTravel/_hero.SourceMotionHeight+.001f);
        var requested=profile with{Reach=requiredReach};
        var clamped=PrototypeApproachProfile(target,requested);
        float reachable=Math.Min(safeTravel,clamped.Reach*_hero.SourceMotionHeight);
        bool remote=reachable+.035f<contactTravel;
        string reason=remote?(clamped.Reach*_hero.SourceMotionHeight+.035f<safeTravel?"intervening_actor":"body_clearance"):"physical_surface";
        return new(clamped,Math.Max(0,distance-safeTravel),remote,surface,reason,contactTravel,safeTravel,reachable);
    }

	private void BeginSourceCard(ActionPlayback action)
	{
		if (_hero == null || (object)action.Card == null)
		{
			return;
		}
		_hero.Motion.BeginAction(_clock, _presentationFrame);
        bool remotePrototypeStrike=false;
		action.Actor = _hero;
		if (action.Beats.FirstOrDefault((OrdinaryBeat ordinaryBeat) => ordinaryBeat.Direct)?.Beat.Event is GameEvent_DamageDealt hit)
		{
			BattleActor target = Enemy(hit.EnemyIndex);
			if (target != null)
			{
				bool ranged = (_session?.Catalog.Cards.FirstOrDefault((CardDef c) => c.Id == action.Card.CardId))?.Type == "spell";
				if ((object)action.Profile != null)
				{
					SourceShowcaseProfile approach = Hd2dAttackPrototype.Supports("card:" + action.Card.CardId) ? PrototypeApproachProfile(target, action.Profile) : action.Profile;
                    float? strikeClearance=null;
                    if(Hd2dAttackPrototype.UsesWeaponContact(action.Card.CardId))
                    {
                        var plan=PrototypeStrikeApproach(target,action.Profile);
                        approach=plan.Profile;strikeClearance=plan.Clearance;remotePrototypeStrike=plan.Remote;action.PrototypeNearSurface=plan.Surface;action.PrototypeApproachReason=plan.Reason;action.PrototypeApproachTravel=new(plan.DesiredTravel,plan.SafeTravel,plan.ReachableTravel);
                    }
                    action.PrototypeRemoteStrike=remotePrototypeStrike;
                    string pose = _hero.Motion.Approach(_hero.SourceMotionFeet, _hero.SourceMotionHeight, target.SourceMotionFeet, target.SourceMotionHeight, ranged, action.VisualVolleys, approach, _clock, _presentationFrame, _world.Camera.GlobalBasis.X, _hero.ArtId, attackPoseLead: Hd2dAttackPrototype.Supports("card:"+action.Card.CardId) ? Hd2dAttackPrototype.PoseLead(action.Card.CardId) : _hero.AttackPoseContactLead,
                        targetClearanceOverride: strikeClearance,
                        approachAnticipationOverride: Hd2dAttackPrototype.UsesWeaponContact(action.Card.CardId) ? -.035f : null);
					// Waiting for an offensive pose should not rewind an already-idle loop.
					if (pose != "idle" || _hero.PoseName != "idle") _hero.PlayPose(pose, _clock);
				}
				else if (!ranged)
				{
					_hero.PlayPose("attack", _clock);
					_hero.BeginSourceLunge(new Vector2(64f, -6f), _clock, _presentationFrame, 0.28f, 0.11f);
				}
				else if (_hero.ArtId == "leader_blue" || (Hd2dAttackPrototype.UsesCastEmitter(action.Card.CardId)))
				{
					_hero.PlayPose("attack", _clock);
				}
				goto IL_025c;
			}
		}
		if (action.Beats.Any((OrdinaryBeat ordinaryBeat) =>
		{
			GameEvent gameEvent = ordinaryBeat.Beat.Event;
			if (gameEvent is GameEvent_BlockGained gameEvent_BlockGained)
			{
				if (gameEvent_BlockGained.Target == "player")
				{
					goto IL_0032;
				}
			}
			else if (gameEvent is GameEvent_IceBlockGained)
			{
				goto IL_0032;
			}
			return false;
			IL_0032:
			return true;
		}))
		{
			_hero.PlayPose("block", _clock);
		}
		goto IL_025c;
		IL_025c:
		BeginSourceCastLight(action);
		action.Capture = _hero.Motion.Capture();
        // A registered card can have a defence-only mode. Source attack pose
        // ownership follows actual direct damage events, not registry membership.
        _hero.BeginHd2dPose(action.Beats.Any(b=>b.Direct)?action.Card.CardId:"", _clock,remotePrototypeStrike,action.VisualVolleys,action.Profile?.Interval??0f);
		foreach (OrdinaryBeat b in action.Beats)
		{
			if (b.Direct && b.Beat.Event is GameEvent_DamageDealt h)
			{
				b.CapturedTarget = Enemy(h.EnemyIndex);
			}
		}
	}

	private bool SourceEnemyRanged(GameState? snapshot, int index)
	{
		if ((object)snapshot == null || index < 0 || index >= snapshot.Enemies.Count)
		{
			return false;
		}
		EnemyState enemy = snapshot.Enemies[index];
		if (enemy.Intent?.ConditionalOn != null && (object)enemy.Intent.Alt != null)
		{
			return false;
		}
		if (_sourceMotionProfiles == null)
		{
			_sourceMotionProfiles = JObject.Parse(FileAccess.GetFileAsString("res://assets/presentation-data/card-vfx-profiles.json"));
		}
		JToken entry = _sourceMotionProfiles["_enemyMoves"]?[enemy.EnemyId + "/" + enemy.IntentMoveId];
		JToken required = entry?["requiresExecutedMoveMetadata"];
		if (required != null && (required.Type != JTokenType.Boolean || required.Value<bool>()))
		{
			return false;
		}
		string style = entry?["style"]?.Value<string>();
		if (style == "beam" || style == "throw")
		{
			return true;
		}
		return false;
	}

	private const float EnemyMeleeLungeDuration = 0.28f;
	private const float EnemyMeleeLungeLead = EnemyMeleeLungeDuration * 0.35f;

    // Visual travel is bounded by the opaque neighbour gap. Attack timing and
    // contact ownership stay unchanged; close-packed enemies must not bite a peer.
    private Vector2 ProfiledEnemyMeleeTravel(BattleActor actor)
    {
        Vector2 travel = new(-90f, 12f);
        if (!actor.Style04MotionEnabled || _world.Camera == null) return travel;
        Rect2? footprint = actor.AuthoredFramesEnabled ? ProjectAuthoredBounds(actor, true) : ProjectEnemyFootprint(actor.EnemyIndex);
        if (footprint is not Rect2 body) return travel;
        float nearestRight = float.NegativeInfinity;
        foreach (BattleActor peer in _enemies.Values)
        {
            if (peer == actor || !peer.Visible || peer.DissolveAmount >= 1f) continue;
            Rect2? peerBounds=peer.AuthoredFramesEnabled?ProjectAuthoredBounds(peer,true):ProjectEnemyFootprint(peer.EnemyIndex);
            if (peerBounds is Rect2 other && other.GetCenter().X < body.GetCenter().X)
                nearestRight = Math.Max(nearestRight, other.End.X);
        }
        if (!float.IsFinite(nearestRight)) return travel;
        // Reserve the restrained rotation/stretch envelope as well as a visible gap.
        float reserve = body.Size.Y * 0.08f + body.Size.X * 0.04f + 8f;
        float available = Math.Max(0f, body.Position.X - nearestRight - reserve);
        var camera = _world.Camera;
        float requested = Math.Abs(camera.UnprojectPosition(actor.Home + actor.SourceCanvasDisplacement(travel)).X - camera.UnprojectPosition(actor.Home).X);
        return requested > 0.001f ? travel * Math.Clamp(available / requested, 0f, 1f) : travel;
    }

	private void BeginSourceEnemyLunge(GameEvent_DamageDealt hit)
	{
		if (_sourceActor != null)
		{
			bool ranged = SourceEnemyRanged(_ordinaryCallback?.Action.Owner.Change.Previous?.Combat ?? _fenceCommand?.Change.Previous?.Combat, hit.EnemyIndex ?? (-1));
			if (_ordinaryCallback?.EnemyLungeScheduled != true)
			{
				_sourceActor.BeginSourceLunge(ranged ? new Vector2(18f, 0f) : ProfiledEnemyMeleeTravel(_sourceActor), _clock, _presentationFrame,
					ranged ? 0.2f : EnemyMeleeLungeDuration, 0f, ranged ? 0f : (_ordinaryCallback?.Beat.Contacts?.Span ?? 0f));
				if (!ranged && _ordinaryCallback != null)
				{
					_sourceActor.UiLunge.ArmContactHold();
					_ordinaryCallback.EnemyLungeActor = _sourceActor;
					_ordinaryCallback.EnemyLungeVersion = _sourceActor.UiLunge.Version;
					_ordinaryCallback.EnemyLungeScheduled = true;
				}
			}
			PreviewEnemyAttack(hit, ranged);
		}
	}

	private void ApplySourceImpact(PresentationBeat beat, GameEvent_DamageDealt hit)
	{
		BattleActor target = _targetActor;
		if (target == null)
		{
			return;
		}
		if (beat.WinningHit)
		{
			_finishSlowRemaining = Math.Max(_finishSlowRemaining, 0.14f);
		}
		if (hit.Source == "enemy" && (_ordinaryCallback == null || !_ordinaryCallback.Flashed))
		{
			PresentInjuryFlash(hit.HpLoss);
			OrdinaryBeat row = _ordinaryCallback;
			if (row != null)
			{
				row.Flashed = true;
			}
		}
		OrdinaryBeat b = _ordinaryCallback;
		ActionPlayback a = b?.Action;
		if (b != null && b.Direct)
		{
			SourceShowcaseProfile p = a?.Profile;
			if ((object)p != null && b.CapturedTarget == target)
			{
				float pause = p.ImpactPauseFor(beat.WinningHit, a.Volleys > 1, b.Volley, a.Volleys);
                if(Hd2dAttackPrototype.Enabled && PhysicalVfxProfiles.ForCard(a.Card?.CardId)?.TargetPattern==PhysicalVfxTargetPattern.ResolvedSweep && b.TargetOrdinal>0)pause=0f;
                bool heavyStomp=Hd2dAttackPrototype.Supports("card:"+a.Card?.CardId) && a.Card?.CardId=="green_finisher_stomp";
				target.Motion.Recoil(heavyStomp?Vector3.Zero:_world.Camera.GlobalBasis.X, target.SourceMotionHeight, p.Heavy, _clock, _presentationFrame);
				target.Motion.RequestOwnedPause(target.Motion.Capture(), pause, _clock);
				BattleActor actor = a.Actor;
				if (actor != null && !(Hd2dAttackPrototype.Enabled && b.TargetOrdinal>0 && PhysicalVfxProfiles.ForCard(a.Card?.CardId)?.TargetPattern==PhysicalVfxTargetPattern.ConditionalTransfer))
				{
					SourceActorMotion.OwnershipToken? capture = a.Capture;
					if (capture.HasValue)
					{
						SourceActorMotion.OwnershipToken capture2 = capture.GetValueOrDefault();
						actor.Motion.RequestOwnedPause(capture2, pause, _clock);
					}
				}
				target.PlayPose("hurt", _clock);
                if(heavyStomp)target.BeginHd2dHeavyReaction(_clock);
				_lastHandoffPause = a.Actor?.Motion.PauseRemaining(_clock) ?? 0f;
				_actionQueue.Hold(a.Token, _clock + _lastHandoffPause, _presentationFrame);
				return;
			}
		}
		// Reactions and incoming hits have no direct-card motion owner. Use the
		// existing contact recoil instead of starting a slow body lunge after HP
		// has already changed. Missing hurt textures still retain this response.
		Vector3 recoilDirection = target == _hero ? -_world.Camera.GlobalBasis.X : _world.Camera.GlobalBasis.X;
		target.Motion.Recoil(recoilDirection, target.SourceMotionHeight, false, _clock, _presentationFrame);
		target.PlayPose("hurt", _clock);
	}

	public void SetEnemyLedgerBounds(Func<IReadOnlyList<Rect2>>? bounds)
	{
		_enemyLedgerBounds = bounds;
	}

	private void EnsurePhaseLayer()
	{
		if (_phaseLayer == null)
		{
			_phaseLayer = new CanvasLayer
			{
				Name = "SourcePhaseEffects",
				Layer = 8
			};
			AddChild(_phaseLayer, forceReadableName: false, InternalMode.Disabled);
			_injuryFlash = new ColorRect
			{
				Name = "SourceInjuryFlash",
				MouseFilter = Control.MouseFilterEnum.Ignore,
				Visible = false
			};
			_phaseLayer.AddChild(_injuryFlash, forceReadableName: false, InternalMode.Disabled);
			_phaseBand = new ColorRect
			{
				Name = "SourceTurnBanner",
				MouseFilter = Control.MouseFilterEnum.Ignore,
				Visible = false
			};
			_phaseLayer.AddChild(_phaseBand, forceReadableName: false, InternalMode.Disabled);
			_phaseText = new Label
			{
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				MouseFilter = Control.MouseFilterEnum.Ignore
			};
			_phaseText.AddThemeFontOverride("font", GD.Load<Font>("res://assets/Fonts/NotoSansJP-Bold.otf"));
			_phaseText.AddThemeFontSizeOverride("font_size", 34);
			_phaseBand.AddChild(_phaseText, forceReadableName: false, InternalMode.Disabled);
			_phaseAccent = new ColorRect
			{
				MouseFilter = Control.MouseFilterEnum.Ignore
			};
			_phaseBand.AddChild(_phaseAccent, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private static int SupportEventIndex(ActionPlayback action, GameEvent e)
	{
		var log = action.Owner.Change.Current?.Combat?.EventLog;
		if (log == null) return -1;
		for (int i = log.Count - 1; i >= 0; i--)
			if (ReferenceEquals(log[i], e)) return i;
		return -1;
	}

	private void BeginSupportPlayback(GameEvent_EnemyActionExecuting executing)
	{
		var actor = Enemy(executing.EnemyIndex);
		var action = _ordinaryCallback?.Action;
		var meta = executing.PresentationEnemyAction;
		if (actor == null || action == null || meta == null || meta.Version != 1 ||
			meta.UnresolvedReason != null || meta.EnemyId != actor.DefinitionId ||
			SupportEventIndex(action, executing) != meta.ExecutionEventIndex) return;
		var token = actor.BeginStyle04Support(executing.Kind, meta.MoveId, meta.ExecutionEventIndex, _clock);
		if (!token.IsValid) return;
		_supportPlayback = new SupportPlayback { Actor = actor, Action = action, Token = token,
			Generation = _actionQueue.Generation, EnemyIndex = executing.EnemyIndex,
			EnemyId = actor.DefinitionId, Kind = executing.Kind };
        if(actor.DefinitionId=="enemy_set_breaker"&&executing.Kind=="destroy-token")ReserveBreakerTokenOutcome(_supportPlayback,executing);
        if(actor.HealAuthoredFramesEnabled&&executing.Kind=="heal"){
            var plan=MossHealAppliedOncePlan.TryReserve(action.Owner.Change.Previous?.Combat,action.Owner.Change.Current?.Combat?.EventLog,executing);
            var target=plan==null?null:Enemy(plan.TargetIndex);
            if(plan!=null&&target!=null&&GodotObject.IsInstanceValid(target)&&!target.Dead&&target.DefinitionId==plan.TargetEnemyId&&actor.ReserveHealOutcome(token,plan,_supportPlayback.Generation)){
                _supportPlayback.HealPlan=plan;_supportPlayback.HealTarget=target;
            }
        }
        if(actor.MillAuthoredFramesEnabled&&executing.Kind=="mill"){
            var plan=DevourerMillAppliedOncePlan.TryReserve(action.Owner.Change.Previous?.Combat,action.Owner.Change.Current?.Combat?.EventLog,executing);
            if(plan!=null&&actor.ReserveMillApplied(token,plan,_supportPlayback.Generation))_supportPlayback.MillPlan=plan;
        }
        if(actor.HexAuthoredFramesEnabled&&executing.Kind=="hex"){
            var plan=HexAppliedOncePlan.TryReserve(action.Owner.Change.Previous?.Combat,action.Owner.Change.Current?.Combat?.EventLog,executing,action.Owner.Change.Current?.Combat);
            if(plan!=null&&actor.ReserveHexOutcome(token,plan,_supportPlayback.Generation))_supportPlayback.HexPlan=plan;
        }
        if(actor.DrummerAuthoredFramesEnabled&&executing.Kind=="rally"){
            var plan=DrummerAppliedOncePlan.TryReserve(action.Owner.Change.Previous?.Combat,action.Owner.Change.Current?.Combat?.EventLog,executing);
            if(plan!=null&&actor.ReserveDrummerApplied(token,plan,_supportPlayback.Generation))_supportPlayback.DrummerPlan=plan;
        }
	}

    // TokenDestroyed has no caster identity. Pin its exact event inside this
    // declared execution and its matching real Resolved boundary, not the turn.
    private static void ReserveBreakerTokenOutcome(SupportPlayback p,GameEvent_EnemyActionExecuting executing)
    {
        var before=p.Action.Owner.Change.Previous?.Combat;var after=p.Action.Owner.Change.Current?.Combat;
        var log=after?.EventLog;var m=executing.PresentationEnemyAction;
        if(before==null||after==null||log==null||p.EnemyIndex<0||p.EnemyIndex>=before.Enemies.Count||before.Enemies[p.EnemyIndex].EnemyId!="enemy_set_breaker"||before.Enemies[p.EnemyIndex].Hp<=0||m==null||m.Version!=1||m.UnresolvedReason!=null||m.EnemyId!="enemy_set_breaker"||m.MoveId!="break_token"||m.Branch is not ("primary" or "alternate")||m.ExecutionEventIndex!=p.Token.ExecutionIndex||executing.EnemyIndex!=p.EnemyIndex||p.Kind!="destroy-token"||p.Token.ExecutionIndex<before.EventLog.Count||p.Token.ExecutionIndex>=log.Count||!ReferenceEquals(log[p.Token.ExecutionIndex],executing))return;
        if(log.Count(e=>ReferenceEquals(e,executing))!=1)return;
        for(int i=0;i<before.EventLog.Count;i++)if(!ReferenceEquals(before.EventLog[i],log[i]))return;
        int outcomeIndex=p.Token.ExecutionIndex+1,resolvedIndex=outcomeIndex+1;
        if(resolvedIndex>=log.Count||log[outcomeIndex] is not GameEvent_TokenDestroyed outcome||string.IsNullOrEmpty(outcome.Uid)||
           log[resolvedIndex] is not GameEvent_EnemyActionResolved resolved||resolved.EnemyIndex!=p.EnemyIndex||resolved.Kind!="destroy-token"||resolved.HpLoss!=0||resolved.Actual!=0||
           log.Count(x=>ReferenceEquals(x,outcome))!=1||log.Count(x=>ReferenceEquals(x,resolved))!=1||
           !p.Action.Beats.Any(x=>ReferenceEquals(x.Beat.Event,outcome))||!p.Action.Beats.Any(x=>ReferenceEquals(x.Beat.Event,resolved)))return;
        if(!before.Player.Permanents.Any(x=>x.Uid==outcome.Uid&&x.Def.Id==outcome.CardId&&(x.Token==true||x.Def.Retainer==true))||after.Player.Permanents.Any(x=>x.Uid==outcome.Uid))return;
        p.TokenDestroyed=outcome;p.TokenDestroyedIndex=outcomeIndex;
    }

    private bool CurrentHealTarget(SupportPlayback p)
        => p.HealPlan!=null&&p.HealTarget!=null&&GodotObject.IsInstanceValid(p.HealTarget)&&
            ReferenceEquals(Enemy(p.HealPlan.TargetIndex),p.HealTarget)&&!p.HealTarget.Dead&&
            p.HealTarget.DefinitionId==p.HealPlan.TargetEnemyId;

	private bool CurrentSupport(SupportPlayback p)
		=> p.Generation == _actionQueue.Generation && ReferenceEquals(_ordinaryCallback?.Action, p.Action) &&
			GodotObject.IsInstanceValid(p.Actor) && ReferenceEquals(Enemy(p.EnemyIndex), p.Actor) &&
			!p.Actor.Dead && p.Actor.DefinitionId == p.EnemyId && p.Actor.ExecutionVersion == p.Token.OwnerVersion;

	private void EndSupportPlayback(bool resolved = false)
	{
		var p = _supportPlayback; _supportPlayback = null;
		if (p != null && GodotObject.IsInstanceValid(p.Actor))
			p.Actor.EndStyle04Support(p.Token, _clock, resolved);
	}

	private void CancelSupportPlayback()
	{
		var p = _supportPlayback; _supportPlayback = null;
		if (p != null && GodotObject.IsInstanceValid(p.Actor)) p.Actor.CancelStyle04Support(p.Token);
	}

    private void PresentAuthoredFled(GameEvent_EnemyFled e)
    {
        var p=_supportPlayback;
        if(p==null||p.Kind!="flee"||e.EnemyIndex!=p.EnemyIndex||!CurrentSupport(p)||SupportEventIndex(p.Action,e)<=p.Token.ExecutionIndex)return;
        p.Actor.BeginAuthoredFleeTerminal(p.Token,_clock,.55f);
    }

	private void PresentSupportEffect(GameEvent e)
	{
		var p = _supportPlayback;
		if (p == null) return;
		if (!CurrentSupport(p) || (p.HealPlan!=null&&!CurrentHealTarget(p))) { CancelSupportPlayback(); return; }
		int index = SupportEventIndex(p.Action, e);
		if (index <= p.Token.ExecutionIndex) return;
		if (e is GameEvent_EnemyActionResolved resolved && resolved.EnemyIndex == p.EnemyIndex && resolved.Kind == p.Kind)
		{ EndSupportPlayback(resolved: (p.DrummerPlan==null||p.DrummerPlan.IsResolved(e,index))&&(p.HexPlan==null||p.HexPlan.IsResolved(e,index))&&(p.MillPlan==null||p.MillPlan.IsResolved(e,index))&&(p.HealPlan==null||p.HealPlan.IsResolved(e,index))); return; }
		if (e is GameEvent_EnemyPhaseEnded || e is GameEvent_TurnStarted)
		{ EndSupportPlayback(); return; }
        if(p.HealPlan!=null&&p.HealPlan.IsOutcome(e,index))p.Actor.ApplyHealOutcomeOnce(p.Token,e,index,p.Generation,_clock);
        if(p.MillPlan!=null&&p.MillPlan.IsApplied(e,index))p.Actor.ApplyMillOnce(p.Token,e,index,p.Generation,_clock);
        if(p.HexPlan!=null&&p.HexPlan.IsOutcome(e,index))p.Actor.ApplyHexOutcome(p.Token,e,index,p.Generation,_clock);
        if(p.DrummerPlan!=null&&p.DrummerPlan.IsApplied(e,index))
            p.Actor.ApplyDrummerOnce(p.Token,e,index,p.Generation,_clock);
		// Only events with an explicit caster identity (or own defend receipt) pulse.
		// Passive / ambiguous recipient events are not guessed to belong to the caster.
		bool effect = e switch
		{
			GameEvent_BlockGained x => p.Kind == "defend" && x.Target == "enemy" && x.Amount > 0 && x.PresentationReceipt?.EnemyIndex == p.EnemyIndex,
			GameEvent_StrengthGained x => (p.Kind == "buff" || p.Kind == "defend") && x.EnemyIndex == p.EnemyIndex && x.Amount > 0 && x.Reason == null,
			GameEvent_EnemyHealed x => p.Kind == "heal" && x.EnemyIndex == p.EnemyIndex && x.Amount > 0,
			GameEvent_EnemySummoned x => p.Kind == "summon" && x.EnemyIndex == p.EnemyIndex && x.Count > 0,
            GameEvent_CardSealed x => p.EnemyId=="enemy_seal_orb"&&p.Kind=="seal"&&p.Actor.Style04SupportSample.MoveId=="seal"&&x.EnemyIndex==p.EnemyIndex&&!string.IsNullOrEmpty(x.CardId),
            GameEvent_TokenDestroyed x => p.EnemyId=="enemy_set_breaker"&&p.Kind=="destroy-token"&&p.Actor.Style04SupportSample.MoveId=="break_token"&&ReferenceEquals(x,p.TokenDestroyed)&&index==p.TokenDestroyedIndex,
			_ => false
		};
		if (effect && !(p.Actor.ContactAuthoredFramesEnabled && (p.EnemyId == "enemy_vine_walker" || p.EnemyId == "enemy_drummer" || p.EnemyId == "enemy_spore_cap" || p.EnemyId == "enemy_hexer") && p.Kind == "defend" && p.Actor.Style04SupportSample.MoveId == "guard" && e is not GameEvent_BlockGained)) p.Actor.PulseStyle04Support(p.Token, index, _clock,actualOwnBlockGained:e is GameEvent_BlockGained,actualOwnStrengthGained:e is GameEvent_StrengthGained,actualOwnSummon:((p.EnemyId=="enemy_moss_spawner"&&p.Actor.Style04SupportSample.MoveId=="spawn"&&e is GameEvent_EnemySummoned summon&&summon.Into=="enemy_moss_slime"&&summon.Count==1)||(p.EnemyId=="enemy_winch_warden"&&p.Actor.Style04SupportSample.MoveId=="release_orbs"&&e is GameEvent_EnemySummoned winchSummon&&winchSummon.Into=="enemy_seal_orb"&&winchSummon.Count is 1 or 2))&&p.Kind=="summon"&&e is GameEvent_EnemySummoned ownSummon&&ownSummon.EnemyIndex==p.EnemyIndex,actualOwnTokenDestroyed:e is GameEvent_TokenDestroyed&&ReferenceEquals(e,p.TokenDestroyed)&&index==p.TokenDestroyedIndex,actualOwnCardSealed:p.EnemyId=="enemy_seal_orb"&&p.Kind=="seal"&&p.Actor.Style04SupportSample.MoveId=="seal"&&e is GameEvent_CardSealed seal&&seal.EnemyIndex==p.EnemyIndex&&!string.IsNullOrEmpty(seal.CardId));
	}

	private void PresentSourcePhaseCue(GameEvent e)
	{
		if (e is GameEvent_EnemyActionExecuting act)
		{
			EndSupportPlayback();
			Enemy(act.EnemyIndex)?.BeginExecutionScale(act.Kind, _clock);
			BeginSupportPlayback(act);
			PresentEnemyAttackExecution(act);
			EnemyExecutionCount++;
		}
		if (e is GameEvent_TurnEnded || e is GameEvent_TurnStarted)
		{
			EnsurePhaseLayer();
			_bannerEnemy = e is GameEvent_TurnEnded;
			_bannerAt = _clock;
			_phaseText.Text = (_bannerEnemy ? "敵の番" : "あなたの番");
			((ColorRect)_phaseBand).Color = new Color(_bannerEnemy ? "252433" : "f4ecd6");
			_phaseText.AddThemeColorOverride("font_color", new Color(_bannerEnemy ? "f4ecd6" : "2f2e35"));
			_phaseAccent.Color = new Color(_bannerEnemy ? "c9635a" : "7fa86c");
			SamplePhaseLayer();
		}
	}

	private void PresentInjuryFlash(int loss)
	{
		if (loss > 0)
		{
			EnsurePhaseLayer();
			_flashAt = _clock;
			_flashAlpha = Math.Min(0.35f, 0.1f + (float)loss * 0.015f);
			InjuryFlashCount++;
		}
	}

	private Rect2 ActorScreenBody(BattleActor a)
	{
		Rect2 r = a.Sprite.GetItemRect();
		Vector2 p = r.Position;
		Vector2 z = r.End;
		Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
		Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
		Vector2[] array = new Vector2[4]
		{
			new Vector2(p.X, p.Y),
			new Vector2(p.X, z.Y),
			new Vector2(z.X, p.Y),
			new Vector2(z.X, z.Y)
		};
		for (int i = 0; i < array.Length; i++)
		{
			Vector2 q = array[i];
			Vector2 v = _world.Camera.UnprojectPosition(a.Sprite.GlobalTransform * new Vector3(q.X * a.Sprite.PixelSize, (0f - q.Y) * a.Sprite.PixelSize, 0f));
			min = min.Min(v);
			max = max.Max(v);
		}
		return new Rect2(min, max - min);
	}

	private void SamplePhaseLayer()
	{
		if (_phaseLayer == null)
		{
			return;
		}
		Vector2 size = GetViewport().GetVisibleRect().Size;
		_injuryFlash.Size = size;
		float flashAge = _clock - _flashAt;
		_injuryFlash.Visible = _interfaceRoute == "combat" && flashAge < 0.25f;
		_injuryFlash.Color = new Color(0.9f, 0.1f, 0.1f, _flashAlpha * Math.Clamp(1f - flashAge / 0.25f, 0f, 1f));
		float k = (_clock - _bannerAt) / 0.9f;
		_phaseBand.Visible = _interfaceRoute == "combat" && k < 1f;
		if (!_phaseBand.Visible)
		{
			return;
		}
		Control? phaseBand = _phaseBand;
		float a;
		if (k < 0.15f)
		{
			a = k / 0.15f;
		}
		else
		{
			a = ((k > 0.7f) ? (1f - (k - 0.7f) / 0.3f) : 1f);
		}
		phaseBand.Modulate = new Color(1f, 1f, 1f, a);
		float need = _phaseText.GetThemeFont("font").GetStringSize(_phaseText.Text, HorizontalAlignment.Left, -1f, 34, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal).X + 72f;
		float cx = size.X / 2f;
		float cy = size.Y / 2f;
		float w = 720f;
		BattleActor[] actors = _enemies.Values.Where((BattleActor battleActor) => battleActor.Visible && battleActor.DissolveAmount < 1f).ToArray();
		if (_hero != null && actors.Length != 0)
		{
			float left = ActorScreenBody(_hero).End.X;
			float right = actors.Min((BattleActor a2) => ActorScreenBody(a2).Position.X);
			if (right > left)
			{
				w = Math.Max(need, Math.Min(560f, right - left - 32f));
				cx = Math.Clamp((left + right) / 2f, w / 2f + 8f, Math.Max(w / 2f + 8f, size.X - w / 2f - 8f));
			}
		}
		foreach (Rect2 ledger in _enemyLedgerBounds?.Invoke() ?? Array.Empty<Rect2>())
		{
			if (ledger.End.X > cx - w / 2f && ledger.Position.X < cx + w / 2f)
			{
				cy = Math.Min(cy, ledger.Position.Y - 8f - 40f);
			}
		}
		cy = Math.Max(148f, cy);
		_phaseBand.Position = new Vector2(cx - w / 2f, cy - 40f);
		_phaseBand.Size = new Vector2(w, 80f);
		_phaseText.Position = new Vector2(0f, 4f);
		_phaseText.Size = new Vector2(w, 76f);
		_phaseAccent.Position = new Vector2(w / 2f - 60f, 64f);
		_phaseAccent.Size = new Vector2(120f, 4f);
	}

	private void CancelPhaseLayer()
	{
		_bannerAt = (_flashAt = -100f);
		if (_phaseBand != null)
		{
			_phaseBand.Visible = false;
		}
		if (_injuryFlash != null)
		{
			_injuryFlash.Visible = false;
		}
	}
}

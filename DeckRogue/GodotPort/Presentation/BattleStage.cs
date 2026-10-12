using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using DeckRogue.Engine.Generated;
using DeckRogue.Game;
using DeckRogue.GodotPort.Core;
using DeckRogue.GodotPort.UI;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;

namespace DeckRogue.GodotPort.Presentation;

[ScriptPath("res://scripts/presentation/BattleStage.cs")]
public partial class BattleStage : Node3D
{
	private sealed record DeathState(float Started, float Delay, float Duration, bool Retired, bool Fled);

	private sealed class SemanticPlacement
	{
		public long Id;

		public SemanticEventCue Cue = null;

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

	private AuthoredGeometryFx _native = null;

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

	private Node3D _retiredRoot = null;

	private Node3D _actorRoot = null;

	private ContinuousAttackFx _fx = null;

	private StageWorld _world = null;

	private BattleAudio _audio = null;

	private BattleCuePool _cues = null;

	private Label3D _receipt = null;

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

	public int PendingEventCount => _timeline.PendingCount + (((object)_timeline.Active != null) ? 1 : 0);

	public int ActiveEffectCount
	{
		get
		{
			AuthoredGeometryFx native = _native;
			return ((native != null && native.IsActive) ? 1 : 0) + ((_fx != null && _fx.Visible) ? 1 : 0);
		}
	}

	public int ActiveLightCount => (_fx?.ActiveLights ?? 0) + (_native?.ActiveLights ?? 0);

	public int RejectedReceiptCount { get; private set; }

	public int CompletedEventCount => _timeline.CompletedBeats;

	public float PresentationTime => _timeline.Time;

	public string ActiveEventType => _timeline.Active?.Event.Type ?? "";

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

	public string EffectiveQualityProfile
	{
		get
		{
			int level = _quality.Level;
			if (1 == 0)
			{
			}
			string result = level switch
			{
				0 => "high", 
				1 => "standard", 
				_ => "low", 
			};
			if (1 == 0)
			{
			}
			return result;
		}
	}

	private bool IsPhoneQuality => OS.HasFeature("android") || OS.HasFeature("ios") || OS.GetCmdlineUserArgs().Contains("--ui-phone");

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
					goto IL_004f;
				}
			}
			result = null;
			goto IL_004f;
			IL_004f:
			return (SemanticPlacement?)result;
		}
	}

	public string ActiveSemanticCueEventType => LiveSemanticPlacement?.Cue.EventType ?? "";

	public Rect2? ActiveSemanticCueBounds
	{
		get
		{
			SemanticPlacement p = LiveSemanticPlacement;
			return (p != null) ? new Rect2?(ProjectCueBounds(p.Label, p.Label.GlobalPosition)) : ((Rect2?)null);
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

	public event Action? PresentationCompleted;

	public event Action<int>? TargetSelected;

	public event Action<GameEvent>? EventPresented;

	public event Action<GameEvent>? NumericContactPresented;

	public string GetEnemyId(int index)
	{
		return Enemy(index)?.DefinitionId ?? "";
	}

	public CombatPresentationVitals? GetPresentedVitals(GameEvent e)
	{
		return ((object)e == _lastPresentedEvent) ? _lastPresentedVitals : null;
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
		ConfigFile saved = new ConfigFile();
		saved.SetValue("graphics", "auto_level", _quality.AutoLevel);
		saved.Save("user://presentation.cfg");
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
			ConfigFile cfg = new ConfigFile();
			cfg.SetValue("graphics", "auto_level", _quality.AutoLevel);
			cfg.Save("user://presentation.cfg");
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

	public void Present(SessionChange change)
	{
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
		if (change.IsReset)
		{
			_quality.Restart();
			_resetting = true;
			_timeline.Reset();
			_fx.Clear();
			_native.Clear();
			_world.SetForestAttackLights(null);
			_cues.Clear();
			_ledger.Clear();
			_receipts.Clear();
			_resetting = false;
			ClearActors();
		}
		_enemyBlockExpiryPending = false;
		_blockBoundaryCommand = change.Command is RunCommand_Combat;
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
		if ((object)before != null)
		{
			SyncActors(before, change.Current.LeaderId);
		}
		else if ((object)change.Current.Combat != null)
		{
			SyncActors(change.Current.Combat, change.Current.LeaderId);
		}
		if (change.Events.Count == 0)
		{
			IsPresenting = false;
			_session?.SetPresentationBusy(busy: false);
			SetRunState(change.Current);
			this.PresentationCompleted?.Invoke();
			return;
		}
		_receipts.Clear();
		_ledgerBatch = null;
		_bornUids.Clear();
		_copiedEnter.Clear();
		if ((object)before != null)
		{
			foreach (CardInstance p in before.Player.Permanents)
			{
				_bornUids.Add(p.Uid);
			}
		}
		if ((object)before != null)
		{
			GameState after = change.Current.Combat;
			if ((object)after != null)
			{
				_ledgerBatch = _ledger.Register(before, after);
				if (_ledgerBatch != null)
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
		IsPresenting = true;
		_session?.SetPresentationBusy(busy: true);
		_timeline.Enqueue(change.Events, Routes);
	}

	public void SetInterfaceRoute(string route)
	{
		EnsureBuilt();
		if (_interfaceRoute != route)
		{
			_quality.Restart();
		}
		_interfaceRoute = route;
		_actorRoot.Visible = route == "combat";
		_retiredRoot.Visible = route == "combat";
		if (route == "title")
		{
			CancelPresentation(applyLatest: false);
			_audio.SyncRun(null);
			return;
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
		return ((object)e == _lastPresentedEvent) ? (!LastEventReceiptRejected) : ValidReceipt(e, receipt, snapshot);
	}

	public static bool ValidReceipt(GameEvent e, CombatPresentationReceipt? receipt, GameState? snapshot)
	{
		if ((object)receipt == null || (object)snapshot == null || !CombatPresentationLedger.Valid(e, receipt, out string _))
		{
			return false;
		}
		CombatPresentationVitals v = receipt.Before;
		if (receipt.Target == "player")
		{
			return v.Hp == snapshot.Player.Hp && v.MaxHp == snapshot.Player.MaxHp && v.Block == snapshot.Player.Block && v.IceBlock.GetValueOrDefault() == snapshot.Player.IceBlock;
		}
		int? enemyIndex = receipt.EnemyIndex;
		if (enemyIndex.HasValue)
		{
			int i = enemyIndex.GetValueOrDefault();
			if (i >= 0 && i < snapshot.Enemies.Count)
			{
				EnemyState enemy = snapshot.Enemies[i];
				return enemy.EnemyId == receipt.EnemyId && v.Hp == enemy.Hp && v.MaxHp == enemy.MaxHp && v.Block == enemy.Block;
			}
		}
		return false;
	}

	private EnemyAttackContactPlan.SourceProof? Proof(GameEvent_DamageDealt e)
	{
		int index = e.EnemyIndex ?? (-1);
		GameState before = _displayState;
		GameState after = _latestRun?.Combat;
		bool stable = (object)before != null && (object)after != null && before.Enemies.Count == after.Enemies.Count && index >= 0 && index < before.Enemies.Count && before.Enemies[index].EnemyId == after.Enemies[index].EnemyId;
		return new EnemyAttackContactPlan.SourceProof(index, stable ? before.Enemies[index].EnemyId : "", stable);
	}

	private EnemyAttackContactPlan? PlanContacts(GameEvent_DamageDealt e)
	{
		EnemyAttackContactPlan.SourceProof proof = Proof(e);
		CombatPresentationLedger.Receipt r;
		return _receipts.TryGetValue(e, out r) ? _ledger.ConfigureEnemyProjection(r, proof) : EnemyAttackContactPlan.Capture(e, e.PresentationReceipt, proof);
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
		if (1 == 0)
		{
		}
		float[] array = count switch
		{
			1 => new float[1] { 4.459f }, 
			2 => new float[2] { 3.2f, 7.055f }, 
			3 => new float[3] { 2.2f, 5.708f, 8.741f }, 
			_ => new float[4] { 1.6f, 4.417f, 6.716f, 10.01f }, 
		};
		if (1 == 0)
		{
		}
		float[] t = array;
		float ti = ((count <= 4) ? t[i] : (1.6f + 8.41f * (float)i / (float)Math.Max(1, count - 1)));
		return Path(ti, (i % 2 == 0) ? (-0.5f) : ((count >= 3) ? 0.1f : 0.7f));
	}

	private string NameFor(string id, bool leader = false)
	{
		return (!leader) ? (_session?.Catalog.Enemies.FirstOrDefault((EnemyDef x) => x.Id == id)?.Name ?? id) : (_session?.Catalog.Leaders.FirstOrDefault((LeaderDef x) => x.Id == id)?.Name ?? id);
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
			if (!_enemies.TryGetValue(i, out BattleActor actor) || actor.DefinitionId != enemy.EnemyId)
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
		int[] array = _enemies.Keys.Where((int num3) => num3 >= state.Enemies.Count).ToArray();
		foreach (int i2 in array)
		{
			BattleActor a = _enemies[i2];
			_actorRoot.RemoveChild(a);
			a.QueueFree();
			_enemies.Remove(i2);
		}
		CardInstance[] dolls = state.Player.Permanents.Where((CardInstance cardInstance) => cardInstance.Def.Retainer == true && cardInstance.Innate != true).ToArray();
		for (int i3 = 0; i3 < dolls.Length; i3++)
		{
			CardInstance p = dolls[i3];
			if (!_retainers.TryGetValue(p.Uid, out BattleActor actor2))
			{
				actor2 = new BattleActor
				{
					Uid = p.Uid,
					IdleSlot = i3
				};
				string artPath = "res://assets/Art/dolls/" + p.Def.Id + ".png";
				float height = (ResourceLoader.Exists(artPath) ? ((float)GD.Load<Texture2D>(artPath).GetHeight() * 0.0421875f) : 1.35f);
				actor2.Configure("dolls", p.Def.Id, p.Def.Name, Path(-3.9f + (float)(i3 % 5) * 0.93f, 0.6f + (float)(i3 / 5 * 2)), height, _world.PhoneLayout);
				_actorRoot.AddChild(actor2, forceReadableName: false, InternalMode.Disabled);
				actor2.Vitals.Text = p.Def.Name;
				_retainers[p.Uid] = actor2;
			}
			actor2.IdleSlot = i3;
		}
		string[] array2 = _retainers.Keys.Where((string k) => !dolls.Any((CardInstance d) => d.Uid == k)).ToArray();
		foreach (string uid in array2)
		{
			BattleActor a2 = _retainers[uid];
			_actorRoot.RemoveChild(a2);
			a2.QueueFree();
			_retainers.Remove(uid);
		}
		RefreshActorLayout();
	}

	private void RefreshActorLayout()
	{
		if (_actorRoot == null || _world.Camera == null)
		{
			return;
		}
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
		foreach (BattleActor actor in _actorRoot.GetChildren().OfType<BattleActor>())
		{
			float depth = 0f - (_world.Camera.GlobalTransform.AffineInverse() * actor.Home).Z;
			string category = ((actor.EnemyIndex >= 0) ? "enemies" : ((actor == _hero) ? "leaders" : "dolls"));
			actor.SetSourceImageHeight(SourceActorSizing.Compute(category, actor.BaseTextureWidth, actor.BaseTextureHeight, actor.TopMarginRows, actor.Phone96, kind, size, _world.Camera.Fov, _world.CameraLayout.ReferenceDistance, depth, _world.PhoneLayout, ui).WorldImageHeight);
			actor.SetSourceDisplayTransferEnabled(ActorSourceDisplayRequested);
			actor.ConfigureSourceAppearance(_act, _world.Camera, _displayState?.Enemies.Count ?? 1);
		}
	}

	private BattleActor? Enemy(int? i)
	{
		BattleActor a;
		return (i.HasValue && _enemies.TryGetValue(i.Value, out a)) ? a : null;
	}

	private void BeginBeat(PresentationBeat beat)
	{
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
		if (_receipts.TryGetValue(beat.Event, out CombatPresentationLedger.Receipt owned) && owned.Reason != null)
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
		EnemyAttackContactPlan? contacts = beat.Contacts;
		if (contacts != null && contacts.Individual && beat.Event is GameEvent_DamageDealt enemyHit && _receipts.TryGetValue(beat.Event, out CombatPresentationLedger.Receipt record))
		{
			_sourceProof = Proof(enemyHit);
			_projectionActive = _ledger.TryBeginProjection(record, _sourceProof);
		}
		if (beat.Event is GameEvent_DamageDealt hit && (object)beat.Route != null && (object)beat.Scenario != null)
		{
			_sourceActor = ((hit.Source == "enemy") ? Enemy(hit.EnemyIndex) : ((hit.SourceUid != null && _retainers.TryGetValue(hit.SourceUid, out BattleActor doll)) ? doll : _hero));
			_targetActor = ((hit.PresentationReceipt?.Target == "player" || hit.Source == "enemy") ? _hero : Enemy(hit.PresentationReceipt?.EnemyIndex ?? hit.EnemyIndex));
			Vector3 from = _sourceActor?.Contact ?? (Path(-5f, 0.9f) + Vector3.Up);
			Vector3 to = _targetActor?.Contact ?? (Path(4.459f, -0.5f) + Vector3.Up);
			_sourceActor?.PlayPose("attack", _clock);
			if (!_projectionActive)
			{
				StartEffect(beat.Route, beat.Scenario, from, to);
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
			_sourceActor = ((beat.Event is GameEvent_ConfusedAttack confused) ? Enemy(confused.EnemyIndex) : ((beat.Event is GameEvent_ThornsReflected reflected) ? Enemy(reflected.EnemyIndex) : _targetActor));
			if (_targetActor != null)
			{
				StartEffect(beat.Route, beat.Scenario, _sourceActor?.Contact ?? _targetActor.Contact, _targetActor.Impact);
			}
		}
		else if (beat.Event is GameEvent_EnemyActionExecuting action)
		{
			_sourceActor = Enemy(action.EnemyIndex);
			ShowReceipt(action.Kind, _sourceActor?.Contact ?? Vector3.Up, new Color("e5d9c7"));
		}
		else if (beat.Event is GameEvent_CardPlayed)
		{
			_sourceActor = _hero;
		}
	}

	private void ContactBeat(PresentationBeat beat)
	{
		GameEvent e = beat.Event;
		LastEventReceiptRejected = _rejectBeat;
		_lastPresentedEvent = e;
		_lastPresentedVitals = null;
		EnemyAttackContactPlan? contacts = beat.Contacts;
		bool last = contacts == null || !contacts.Individual || _timeline.ContactOrdinal >= beat.Contacts.Count - 1;
		if (_rejectBeat)
		{
			if (last)
			{
				if (_receipts.TryGetValue(e, out CombatPresentationLedger.Receipt reject))
				{
					_ledger.Commit(reject);
				}
				this.EventPresented?.Invoke(e);
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
			if (_receipts.TryGetValue(e, out CombatPresentationLedger.Receipt tracked))
			{
				if (_projectionActive)
				{
					if (!_ledger.CommitContribution(tracked, _timeline.ContactOrdinal, _sourceProof))
					{
						LastEventReceiptRejected = true;
						RejectedReceiptCount++;
						if (last)
						{
							this.EventPresented?.Invoke(e);
						}
						return;
					}
				}
				else
				{
					_ledger.Commit(tracked);
				}
				SyncLedgerActor(tracked.Target);
				this.NumericContactPresented?.Invoke(e);
			}
			if (e is GameEvent_DamageDealt hit)
			{
				if (last && !_receipts.ContainsKey(e))
				{
					ApplyReceipt(hit.PresentationReceipt);
				}
				if ((object)hit.PresentationReceipt == null)
				{
					_targetActor?.DamageFallback(hit.HpLoss, hit.Blocked.GetValueOrDefault());
				}
				int loss = (_projectionActive ? beat.Contacts[_timeline.ContactOrdinal].HpLossContribution : hit.HpLoss);
				int block = (int)(_projectionActive ? (beat.Contacts[_timeline.ContactOrdinal].BlockContribution + beat.Contacts[_timeline.ContactOrdinal].IceContribution) : CombatOutcome.Read(hit).Absorbed);
				_contactHp = loss;
				_contactBlock = block;
				if (_projectionActive)
				{
					AttackRoute route = ((loss > 0) ? Routes.Resolve(hit) : new AttackRoute("system:outcome", "", "system", (block > 0) ? "guard" : "nullified", "SystemOutcomeFx", "neutral", FixedSmall: true, "proven contribution outcome"));
					EffectScenario scenario = EffectScenario.For(route, loss, priorHp, hit.PresentationReceipt?.Before.MaxHp);
					_effectStartedAt = _timeline.Time;
					_effectOffset = scenario.Windup;
					StartEffect(route, scenario, _sourceActor?.Contact ?? Vector3.Zero, _targetActor?.Contact ?? Vector3.Zero);
				}
				_audio.PlayDamageContact(hit.Source, loss, block, hit.Source == "player" && (hit.Amount >= 15 || hit.Exposed == true));
				if (loss > 0)
				{
					_targetActor?.PlayPose("hurt", _clock);
					_targetActor?.Flash(_clock);
				}
				else if (block > 0)
				{
					_targetActor?.PlayPose("block", _clock);
				}
				string text = ((loss > 0) ? $"−{loss}" : ((block > 0) ? $"防御 {block}" : "0"));
				if (!_projectionActive)
				{
					EnemyAttackPresentation multi = hit.PresentationEnemyAttack;
					if ((object)multi != null && multi.ExecutedHits > 1)
					{
						text += $"  ×{multi.ExecutedHits}";
					}
				}
				if (hit.Source != "enemy" || loss > 0 || block > 0 || hit.Amount <= 0)
				{
					ShowReceipt(text, (_targetActor?.Contact ?? Vector3.Up) + Vector3.Right * ((float)(_timeline.ContactOrdinal % 3 - 1) * 0.14f), (loss > 0) ? new Color("fff1e6") : new Color("b7c8df"));
				}
			}
			else
			{
				PresentTurnBoundary(e);
				CombatPresentationReceipt receipt = e.GetType().GetProperty("PresentationReceipt")?.GetValue(e) as CombatPresentationReceipt;
				if (!_receipts.ContainsKey(e))
				{
					ApplyReceipt(receipt);
				}
				_audio.PlayEvent(e);
				ShowStatusCue(e);
				GameEvent gameEvent = e;
				GameEvent gameEvent2 = gameEvent;
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
																	if (!(gameEvent2 is GameEvent_TurnEnded))
																	{
																		if (!(gameEvent2 is GameEvent_TurnStarted))
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
																		else
																		{
																			ShowReceipt("あなたのターン", new Vector3(0f, 3f, 0f), new Color("e3e6e8"));
																		}
																	}
																	else
																	{
																		ShowReceipt("敵のターン", new Vector3(0f, 3f, 0f), new Color("d4c6bb"));
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
								ShowReceipt((c.HpLoss > 0) ? $"−{c.HpLoss}" : ((c.Blocked > 0) ? $"防御 {c.Blocked}" : "0"), Enemy(c.TargetIndex)?.Contact ?? Vector3.Up, new Color("fff1e6"));
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
					if (target != null)
					{
						ShowReceipt($"守り +{b.Amount}", target.Contact, new Color("becbdc"));
					}
				}
			}
			if (last)
			{
				this.EventPresented?.Invoke(e);
			}
		}
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
													actor = ((v11.SourceUid != null && _retainers.TryGetValue(v11.SourceUid, out BattleActor source)) ? source : _hero);
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
		if (1 == 0)
		{
		}
		string result = name switch
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
		if (1 == 0)
		{
		}
		return result;
	}

	private void StartEffect(AttackRoute route, EffectScenario scenario, Vector3 from, Vector3 to)
	{
		to = _targetActor?.Impact ?? to;
		_fx.Clear();
		if (!_native.Begin(route, scenario, from, to, _targetActor?.Home ?? (to - Vector3.Up), _targetActor?.Height ?? 1.5f, _world.Camera.GlobalBasis))
		{
			_fx.Begin(route, scenario, from, to);
		}
	}

	private void SyncLedgerActor(int target)
	{
		GameState state = _latestRun?.Combat;
		if ((object)state != null && _ledger.TryGet(state, target, out CombatPresentationLedger.Vitals v))
		{
			SetDisplayedVitals(target, _lastPresentedVitals = new CombatPresentationVitals
			{
				Hp = (int)v.Hp,
				MaxHp = (int)v.MaxHp,
				Block = (int)v.Block,
				IceBlock = ((target < 0) ? new int?((int)v.IceBlock) : ((int?)null))
			});
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
		GameState final = _latestRun?.Combat;
		if ((object)final == null)
		{
			return;
		}
		List<EnemyState> enemies = old.Enemies.ToList();
		List<CardInstance> perms = old.Player.Permanents.ToList();
		GameEvent gameEvent = e;
		GameEvent gameEvent2 = gameEvent;
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
		}, _latestRun.LeaderId);
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
				CardInstance next2 = final.Player.Permanents.FirstOrDefault((CardInstance cardInstance) => cardInstance.Def.Id == id && ((uid == null) ? (!_bornUids.Contains(cardInstance.Uid)) : (cardInstance.Uid == uid)));
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
			int i = perms.FindIndex((CardInstance cardInstance) => (uid == null) ? (cardInstance.Def.Id == id) : (cardInstance.Uid == uid));
			if (i >= 0)
			{
				string key = perms[i].Uid;
				if (_retainers.Remove(key, out BattleActor retired))
				{
					_actorRoot.RemoveChild(retired);
					_retiredRoot.AddChild(retired, forceReadableName: false, InternalMode.Disabled);
					StartDeath(retired, retired: true);
					_cues.Show((e is GameEvent_RetainerExpired) ? "期限切れ" : ((e is GameEvent_RetainerSacrificed) ? "捧げた" : "灯が消えた"), retired.Contact + Vector3.Up * 0.6f, new Color("c4beb2"));
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
		bool flag = receipt.Version != 1 || (object)receipt.Before == null || (object)receipt.After == null;
		bool flag2 = flag;
		if (!flag2)
		{
			string target = receipt.Target;
			bool flag3 = ((target == "player" || target == "enemy") ? true : false);
			flag2 = !flag3;
		}
		if (!flag2)
		{
			if (!(receipt.Target == "enemy"))
			{
				goto IL_00ed;
			}
			int? enemyIndex = receipt.EnemyIndex;
			if (enemyIndex.HasValue)
			{
				int ri = enemyIndex.GetValueOrDefault();
				BattleActor matching = Enemy(ri);
				if (matching != null && (string.IsNullOrEmpty(receipt.EnemyId) || !(matching.DefinitionId != receipt.EnemyId)))
				{
					goto IL_00ed;
				}
			}
		}
		RejectedReceiptCount++;
		return;
		IL_00ed:
		_lastPresentedVitals = receipt.After;
		SetDisplayedVitals((receipt.Target == "player") ? (-1) : receipt.EnemyIndex.Value, receipt.After);
	}

	private void ShowReceipt(string text, Vector3 position, Color color)
	{
		_cues.Show(text, position + Vector3.Up * 0.4f, color);
	}

	private void FinishBeat(PresentationBeat beat)
	{
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
		if (_ledgerBatch != null)
		{
			_ledger.Finish(_ledgerBatch);
		}
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
		_session?.SetPresentationBusy(busy: false);
		this.PresentationCompleted?.Invoke();
	}

	public override void _Process(double delta)
	{
		if (!PlaybackPaused)
		{
			SampleQuality((float)delta, _interfaceRoute == "combat");
			AdvancePresentation((float)delta * PlaybackSpeed);
		}
	}

	private void AdvanceClock(float delta)
	{
		_clock += delta;
		_cues.Sample(_clock);
	}

	public void AdvancePresentation(float delta)
	{
		EnsureBuilt();
		bool busy = _timeline.Busy;
		_timeline.Advance(delta);
		if (!busy)
		{
			AdvanceClock(Math.Max(0f, delta));
		}
		PresentationBeat beat = _timeline.Active;
		if ((object)beat?.Route != null)
		{
			float time = _timeline.Time - _effectStartedAt + _effectOffset;
			if (_native.IsActive)
			{
				_native.Sample(time);
			}
			else if (_fx.Visible)
			{
				_fx.Sample(time);
			}
		}
		RefreshForestLighting();
		float progress = (((object)beat == null) ? (-1f) : Math.Clamp(_timeline.Time / beat.Duration, 0f, 1f));
		float hit = ((_contactHp <= 0) ? (-1f) : Math.Clamp((_timeline.Time - _timeline.LastContactTime) / 0.24f, 0f, 1f));
		foreach (BattleActor actor in _actorRoot.GetChildren().OfType<BattleActor>().Concat(_retiredRoot.GetChildren().OfType<BattleActor>()))
		{
			AdvanceDeath(actor);
			BindActorLights(actor);
			actor.UpdateVisual(_clock, (actor == _sourceActor) ? progress : (-1f), (actor == _targetActor) ? hit : (-1f));
		}
		if (_receipt.Visible)
		{
			_receipt.Position = _receiptHome + Vector3.Up * (_timeline.Time * 0.28f);
			Color c = _receipt.Modulate;
			c.A = 1f - Math.Max(0f, progress - 0.55f) * 1.8f;
			_receipt.Modulate = c;
		}
	}

	public void CancelPresentation(bool applyLatest = true)
	{
		_enemyBlockExpiryPending = false;
		_blockBoundaryCommand = false;
		_resetting = true;
		_timeline.Reset();
		_fx?.Clear();
		_native?.Clear();
		_world?.SetForestAttackLights(null);
		_cues?.Clear();
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
		if (applyLatest)
		{
			RunState run = _latestRun;
			if ((object)run != null)
			{
				SetRunState(run);
				this.PresentationCompleted?.Invoke();
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
			float duration = (fled ? 0.55f : (retired ? 0.5f : ((actor.Sprite.Texture.GetHeight() >= 96) ? 0.95f : 0.62f)));
			_deaths[actor] = new DeathState(_clock, fled ? 0f : (retired ? 0.1f : 0.12f), duration, retired, fled);
		}
	}

	private void AdvanceDeath(BattleActor actor)
	{
		if (_deaths.TryGetValue(actor, out DeathState d))
		{
			float p = Math.Clamp((_clock - d.Started - d.Delay) / d.Duration, 0f, 1f);
			actor.SetDissolve(d.Fled ? (p * p) : p);
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
		if (!_enemies.TryGetValue(index, out BattleActor actor) || _world.Camera == null)
		{
			return null;
		}
		return (_world.Camera.UnprojectPosition(actor.Home + _world.Camera.GlobalBasis.Y * actor.Height), _world.Camera.UnprojectPosition(actor.Home));
	}

	public int? FindEnemyAt(Vector2 viewportPoint)
	{
		if (IsPresenting || _interfaceRoute != "combat" || _latestRun?.Phase != "combat")
		{
			return null;
		}
		BattleActor best = null;
		float distance = 7225f;
		foreach (BattleActor actor in _enemies.Values)
		{
			if (!actor.Dead)
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
		_worldLights.AddRange(_world.FindChildren("*", "OmniLight3D", recursive: true, owned: false).OfType<OmniLight3D>());
	}

	private void RefreshForestLighting(AuthoredGeometryFx? probeFx = null)
	{
		_actorLights.Clear();
		(probeFx ?? _native).AppendSourceLights(_actorLights);
		_world.SetForestAttackLights(_actorLights);
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
		(probeFx ?? _native).AppendSourceLights(_actorLights);
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
		if (IsPresenting || _interfaceRoute != "combat" || _latestRun?.Phase != "combat")
		{
			return;
		}
		Vector2? pos = ((input is InputEventMouseButton { Pressed: not false } mouse && mouse.ButtonIndex == MouseButton.Left) ? new Vector2?(mouse.Position) : ((input is InputEventScreenTouch { Pressed: not false } touch) ? new Vector2?(touch.Position) : ((Vector2?)null)));
		if (!pos.HasValue)
		{
			return;
		}
		IOrderedEnumerable<(BattleActor, Vector2)> choices = from battleActor in _enemies.Values
			where !battleActor.Dead
			select (actor: battleActor, point: _world.Camera.UnprojectPosition(battleActor.Contact)) into x
			orderby x.point.DistanceSquaredTo(pos.Value)
			select x;
		(BattleActor, Vector2) best = choices.FirstOrDefault();
		if (best.Item1 == null || !(best.Item2.DistanceTo(pos.Value) < 85f))
		{
			return;
		}
		foreach (BattleActor a in _enemies.Values)
		{
			a.Selected = a == best.Item1;
		}
		this.TargetSelected?.Invoke(best.Item1.EnemyIndex);
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
		bool found;
		Rect2 fitted = FitCueRectangle(bounds, viewport, blockers, out found);
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
		Vector2 metric = label.Font.GetMultilineStringSize(label.Text, HorizontalAlignment.Center, -1f, label.FontSize, -1, TextServer.LineBreakFlag.Mandatory | TextServer.LineBreakFlag.WordBound, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal);
		Vector2 min = -metric * label.PixelSize * 0.5f;
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
		Vector2[] points = new Vector2[4]
		{
			new Vector2(min.X, min.Y),
			new Vector2(min.X, max.Y),
			new Vector2(max.X, min.Y),
			new Vector2(max.X, max.Y)
		};
		Vector2 screenMin = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
		Vector2 screenMax = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
		Vector2[] array = points;
		for (int j = 0; j < array.Length; j++)
		{
			Vector2 p = array[j];
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
		Rect2[] blockers = (from r in painted
			where r.Size.X > 0f && r.Size.Y > 0f
			select r.Grow(4f)).ToArray();
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
		BattleActor actor = ((cue.Anchor == "enemy") ? Enemy(cue.EnemyIndex) : ((cue.Anchor == "player") ? _hero : null));
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
		bool flag = !_enemyBlockExpiryPending;
		bool flag2 = flag;
		if (!flag2)
		{
			bool flag3 = ((e is GameEvent_BurnTick || e is GameEvent_EnemyActionExecuting || e is GameEvent_EnemyPhaseEnded || e is GameEvent_TurnStarted) ? true : false);
			flag2 = !flag3;
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
		GameState current = _latestRun?.Combat;
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
		List<MethodInfo> methods = new List<MethodInfo>(37);
		methods.Add(new MethodInfo(MethodName.GetEnemyId, new PropertyInfo(Variant.Type.String, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Int, "index", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.GetEnemyHp, new PropertyInfo(Variant.Type.Int, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Int, "index", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.GetEnemyBlock, new PropertyInfo(Variant.Type.Int, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Int, "index", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.GetEnemyIntent, new PropertyInfo(Variant.Type.String, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Int, "index", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.PlayUiSound, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "sourceKey", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.ApplyQuality, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "profile", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.ApplyQualitySettings, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.SampleQuality, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Float, "unscaledDelta", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Bool, "inBattle", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName._Ready, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.EnsureBuilt, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.Bind, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Object, "session", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Node"), exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.SetInterfaceRoute, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "route", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.ClearRun, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Int, "revision", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.Path, new PropertyInfo(Variant.Type.Vector3, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Float, "t", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Float, "s", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.EnemyFoot, new PropertyInfo(Variant.Type.Vector3, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Int, "i", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Int, "count", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.NameFor, new PropertyInfo(Variant.Type.String, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "id", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Bool, "leader", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.RefreshActorLayout, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.StatusName, new PropertyInfo(Variant.Type.String, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "name", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.SyncLedgerActor, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Int, "target", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.ShowReceipt, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "text", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Vector3, "position", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Color, "color", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.FinishBatch, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName._Process, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Float, "delta", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.AdvanceClock, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Float, "delta", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.AdvancePresentation, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Float, "delta", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.CancelPresentation, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Bool, "applyLatest", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.StartDeath, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Object, "actor", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Node3D"), exported: false),
			new PropertyInfo(Variant.Type.Bool, "retired", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Bool, "fled", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.AdvanceDeath, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Object, "actor", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Node3D"), exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.ClearRetired, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.CacheWorldLights, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.RefreshForestLighting, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Object, "probeFx", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Node3D"), exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.RefreshActorLighting, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Object, "probeFx", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Node3D"), exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.BindActorLights, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Object, "actor", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Node3D"), exported: false),
			new PropertyInfo(Variant.Type.Object, "probeFx", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Node3D"), exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.ClearActors, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName._UnhandledInput, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Object, "input", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("InputEvent"), exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName._ExitTree, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.ProjectCueBounds, new PropertyInfo(Variant.Type.Rect2, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Object, "label", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Label3D"), exported: false),
			new PropertyInfo(Variant.Type.Vector3, "origin", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.PresentBlockBoundary, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Int, "target", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Int, "block", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		return methods;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		if (method == MethodName.GetEnemyId && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<string>(GetEnemyId(VariantUtils.ConvertTo<int>(in args[0])));
			return true;
		}
		if (method == MethodName.GetEnemyHp && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<int>(GetEnemyHp(VariantUtils.ConvertTo<int>(in args[0])));
			return true;
		}
		if (method == MethodName.GetEnemyBlock && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<int>(GetEnemyBlock(VariantUtils.ConvertTo<int>(in args[0])));
			return true;
		}
		if (method == MethodName.GetEnemyIntent && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<string>(GetEnemyIntent(VariantUtils.ConvertTo<int>(in args[0])));
			return true;
		}
		if (method == MethodName.PlayUiSound && args.Count == 1)
		{
			PlayUiSound(VariantUtils.ConvertTo<string>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.ApplyQuality && args.Count == 1)
		{
			ApplyQuality(VariantUtils.ConvertTo<string>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.ApplyQualitySettings && args.Count == 0)
		{
			ApplyQualitySettings();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.SampleQuality && args.Count == 2)
		{
			SampleQuality(VariantUtils.ConvertTo<float>(in args[0]), VariantUtils.ConvertTo<bool>(in args[1]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName._Ready && args.Count == 0)
		{
			_Ready();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.EnsureBuilt && args.Count == 0)
		{
			EnsureBuilt();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.Bind && args.Count == 1)
		{
			Bind(VariantUtils.ConvertTo<GameSession>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.SetInterfaceRoute && args.Count == 1)
		{
			SetInterfaceRoute(VariantUtils.ConvertTo<string>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.ClearRun && args.Count == 1)
		{
			ClearRun(VariantUtils.ConvertTo<long>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.Path && args.Count == 2)
		{
			ret = VariantUtils.CreateFrom<Vector3>(Path(VariantUtils.ConvertTo<float>(in args[0]), VariantUtils.ConvertTo<float>(in args[1])));
			return true;
		}
		if (method == MethodName.EnemyFoot && args.Count == 2)
		{
			ret = VariantUtils.CreateFrom<Vector3>(EnemyFoot(VariantUtils.ConvertTo<int>(in args[0]), VariantUtils.ConvertTo<int>(in args[1])));
			return true;
		}
		if (method == MethodName.NameFor && args.Count == 2)
		{
			ret = VariantUtils.CreateFrom<string>(NameFor(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<bool>(in args[1])));
			return true;
		}
		if (method == MethodName.RefreshActorLayout && args.Count == 0)
		{
			RefreshActorLayout();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.StatusName && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<string>(StatusName(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.SyncLedgerActor && args.Count == 1)
		{
			SyncLedgerActor(VariantUtils.ConvertTo<int>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.ShowReceipt && args.Count == 3)
		{
			ShowReceipt(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<Vector3>(in args[1]), VariantUtils.ConvertTo<Color>(in args[2]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.FinishBatch && args.Count == 0)
		{
			FinishBatch();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName._Process && args.Count == 1)
		{
			_Process(VariantUtils.ConvertTo<double>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.AdvanceClock && args.Count == 1)
		{
			AdvanceClock(VariantUtils.ConvertTo<float>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.AdvancePresentation && args.Count == 1)
		{
			AdvancePresentation(VariantUtils.ConvertTo<float>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.CancelPresentation && args.Count == 1)
		{
			CancelPresentation(VariantUtils.ConvertTo<bool>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.StartDeath && args.Count == 3)
		{
			StartDeath(VariantUtils.ConvertTo<BattleActor>(in args[0]), VariantUtils.ConvertTo<bool>(in args[1]), VariantUtils.ConvertTo<bool>(in args[2]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.AdvanceDeath && args.Count == 1)
		{
			AdvanceDeath(VariantUtils.ConvertTo<BattleActor>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.ClearRetired && args.Count == 0)
		{
			ClearRetired();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.CacheWorldLights && args.Count == 0)
		{
			CacheWorldLights();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.RefreshForestLighting && args.Count == 1)
		{
			RefreshForestLighting(VariantUtils.ConvertTo<AuthoredGeometryFx>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.RefreshActorLighting && args.Count == 1)
		{
			RefreshActorLighting(VariantUtils.ConvertTo<AuthoredGeometryFx>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.BindActorLights && args.Count == 2)
		{
			BindActorLights(VariantUtils.ConvertTo<BattleActor>(in args[0]), VariantUtils.ConvertTo<AuthoredGeometryFx>(in args[1]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.ClearActors && args.Count == 0)
		{
			ClearActors();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName._UnhandledInput && args.Count == 1)
		{
			_UnhandledInput(VariantUtils.ConvertTo<InputEvent>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName._ExitTree && args.Count == 0)
		{
			_ExitTree();
			ret = default(godot_variant);
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
			ret = default(godot_variant);
			return true;
		}
		return base.InvokeGodotClassMethod(in method, args, out ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		if (method == MethodName.Path && args.Count == 2)
		{
			ret = VariantUtils.CreateFrom<Vector3>(Path(VariantUtils.ConvertTo<float>(in args[0]), VariantUtils.ConvertTo<float>(in args[1])));
			return true;
		}
		if (method == MethodName.EnemyFoot && args.Count == 2)
		{
			ret = VariantUtils.CreateFrom<Vector3>(EnemyFoot(VariantUtils.ConvertTo<int>(in args[0]), VariantUtils.ConvertTo<int>(in args[1])));
			return true;
		}
		if (method == MethodName.StatusName && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<string>(StatusName(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		ret = default(godot_variant);
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
		List<PropertyInfo> properties = new List<PropertyInfo>();
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName.IsPresenting, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Float, PropertyName._effectStartedAt, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Float, PropertyName._effectOffset, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName._contactHp, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName._contactBlock, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName.DisplayedHeroHp, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName.DisplayedHeroBlock, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName.DisplayedEnemyCount, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName.ActorsVisible, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.String, PropertyName.CurrentAudioTrack, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName.MusicPlaying, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName.DamageAudioCount, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName.PlayingSoundEffects, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName.DamageContactCount, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName.PresentedRevision, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName.PendingEventCount, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName.ActiveEffectCount, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName.ActiveLightCount, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName.RejectedReceiptCount, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName.CompletedEventCount, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Float, PropertyName.PresentationTime, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.String, PropertyName.ActiveEventType, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.String, PropertyName.ActiveFamily, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.String, PropertyName.ActiveTier, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName.RealLightsEnabled, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.String, PropertyName.QualityProfile, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.String, PropertyName.EffectiveQualityProfile, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName._qualityApplied, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName.IsPhoneQuality, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Float, PropertyName.PlaybackSpeed, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName.PlaybackPaused, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName._projectionActive, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName._rejectBeat, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.String, PropertyName._interfaceRoute, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName.LastEventReceiptRejected, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Object, PropertyName._native, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Object, PropertyName._session, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Object, PropertyName._hero, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Object, PropertyName._sourceActor, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Object, PropertyName._targetActor, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Object, PropertyName._retiredRoot, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Object, PropertyName._actorRoot, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Object, PropertyName._fx, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Object, PropertyName._world, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Object, PropertyName._audio, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Object, PropertyName._cues, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Object, PropertyName._receipt, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName._lastRevision, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Float, PropertyName._clock, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName._act, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName._initialized, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName._resetting, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Vector3, PropertyName._receiptHome, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName._semanticPlacementId, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.String, PropertyName.ActiveSemanticCueEventType, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName.ActiveSemanticCuePlacementFound, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName.PresentedSemanticCueCount, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName.RejectedSemanticCueCount, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName.ActiveCueCount, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName._enemyBlockExpiryPending, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName._blockBoundaryCommand, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName.PresentedBlockBoundaryCount, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		return properties;
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
}

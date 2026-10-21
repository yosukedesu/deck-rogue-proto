using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Godot;
using Godot.Bridge;
using Godot.Collections;
using Godot.NativeInterop;
using Newtonsoft.Json.Linq;

namespace DeckRogue.GodotPort.Presentation;

[ScriptPath("res://scripts/presentation/BattleActor.cs")]
public class BattleActor : Node3D
{
	public new class MethodName : Node3D.MethodName
	{
		public static readonly StringName Configure = "Configure";

		public static readonly StringName SetSourceImageHeight = "SetSourceImageHeight";

		public static readonly StringName Label = "Label";

		public static readonly StringName BindSurface = "BindSurface";

		public static readonly StringName TryTexture = "TryTexture";

		public static readonly StringName PlayPose = "PlayPose";

		public static readonly StringName StartPose = "StartPose";

		public static readonly StringName PoseFrameDuration = "PoseFrameDuration";

		public static readonly StringName VanDerCorput = "VanDerCorput";

		public static readonly StringName AdvancePose = "AdvancePose";

		public static readonly StringName ApplyPoseFrame = "ApplyPoseFrame";

		public static readonly StringName SetVitals = "SetVitals";

		public static readonly StringName DamageFallback = "DamageFallback";

		public static readonly StringName GainBlock = "GainBlock";

		public static readonly StringName Heal = "Heal";

		public static readonly StringName SetDead = "SetDead";

		public static readonly StringName UpdateVisual = "UpdateVisual";

		public static readonly StringName ConfigureSourceAppearance = "ConfigureSourceAppearance";

		public static readonly StringName Flash = "Flash";

		public static readonly StringName SetDissolve = "SetDissolve";

		public static readonly StringName RefreshSourceMaterial = "RefreshSourceMaterial";
	}

	public new class PropertyName : Node3D.PropertyName
	{
		public static readonly StringName DefinitionId = "DefinitionId";

		public static readonly StringName ArtId = "ArtId";

		public static readonly StringName Phone96 = "Phone96";

		public static readonly StringName BaseTextureWidth = "BaseTextureWidth";

		public static readonly StringName BaseTextureHeight = "BaseTextureHeight";

		public static readonly StringName TopMarginRows = "TopMarginRows";

		public static readonly StringName BaseCanvasWorldHeight = "BaseCanvasWorldHeight";

		public static readonly StringName EnemyIndex = "EnemyIndex";

		public static readonly StringName Uid = "Uid";

		public static readonly StringName Home = "Home";

		public static readonly StringName Height = "Height";

		public static readonly StringName DisplayedHp = "DisplayedHp";

		public static readonly StringName DisplayedBlock = "DisplayedBlock";

		public static readonly StringName Placeholder = "Placeholder";

		public static readonly StringName Dead = "Dead";

		public static readonly StringName Selected = "Selected";

		public static readonly StringName Sprite = "Sprite";

		public static readonly StringName Vitals = "Vitals";

		public static readonly StringName IntentLabel = "IntentLabel";

		public static readonly StringName IdleSlot = "IdleSlot";

		public static readonly StringName ShowLabels = "ShowLabels";

		public static readonly StringName Contact = "Contact";

		public static readonly StringName Impact = "Impact";

		public static readonly StringName DissolveAmount = "DissolveAmount";

		public static readonly StringName _bottomMarginRows = "_bottomMarginRows";

		public static readonly StringName _baseTexture = "_baseTexture";

		public static readonly StringName _footAnchorOffsetX = "_footAnchorOffsetX";

		public static readonly StringName _pose = "_pose";

		public static readonly StringName _poseFrame = "_poseFrame";

		public static readonly StringName _poseFrameTime = "_poseFrameTime";

		public static readonly StringName _poseClock = "_poseClock";

		public static readonly StringName _lastAttackProgress = "_lastAttackProgress";

		public static readonly StringName _lastHitProgress = "_lastHitProgress";

		public static readonly StringName _poseInitialized = "_poseInitialized";

		public static readonly StringName _usesExplicitPoses = "_usesExplicitPoses";

		public static readonly StringName _lastGuarding = "_lastGuarding";

		public static readonly StringName _r2Idle = "_r2Idle";

		public static readonly StringName _surface = "_surface";

		public static readonly StringName _boundTexture = "_boundTexture";

		public static readonly StringName _shadow = "_shadow";

		public static readonly StringName _hp = "_hp";

		public static readonly StringName _maxHp = "_maxHp";

		public static readonly StringName _blockValue = "_blockValue";

		public static readonly StringName _deathFade = "_deathFade";

		public static readonly StringName _imageHeight = "_imageHeight";

		public static readonly StringName _feetPad = "_feetPad";

		public static readonly StringName _lastClock = "_lastClock";

		public static readonly StringName _idleDuration = "_idleDuration";

		public static readonly StringName _actorCategory = "_actorCategory";

		public static readonly StringName _layoutBasis = "_layoutBasis";

		public static readonly StringName _layoutCamera = "_layoutCamera";

		public static readonly StringName _lightPositions = "_lightPositions";

		public static readonly StringName _lightColors = "_lightColors";

		public static readonly StringName _sourceDistanceScale = "_sourceDistanceScale";

		public static readonly StringName _edgeLinear = "_edgeLinear";

		public static readonly StringName _flashUntil = "_flashUntil";

		public static readonly StringName _dissolve = "_dissolve";
	}

	public new class SignalName : Node3D.SignalName
	{
	}

	private sealed record AttackGeometry(ArrayMesh Mesh, int Vertices, int Triangles);

	private int _bottomMarginRows;

	private readonly List<Texture2D> _idle = new List<Texture2D>();

	private readonly List<Texture2D> _attack = new List<Texture2D>();

	private readonly List<Texture2D> _hurt = new List<Texture2D>();

	private readonly List<Texture2D> _block = new List<Texture2D>();

	private static readonly float[] AttackDur = new float[4] { 0.05f, 0.08f, 0.04f, 0.18f };

	private static readonly float[] BlueAttackDur = new float[4] { 0.05f, 0.07f, 0.08f, 0.12f };

	private static readonly float[] HurtDur = new float[3] { 0.06f, 0.16f, 0.1f };

	private static readonly float[] BlockDur = new float[4] { 0.06f, 0.08f, 0.1f, 0.2f };

	private static readonly float[] BlueBlockDur = new float[4] { 0.06f, 0.08f, 0.1f, 0.16f };

	private Texture2D _baseTexture;

	private float _footAnchorOffsetX;

	private string _pose = "idle";

	private int _poseFrame;

	private float _poseFrameTime;

	private float _poseClock;

	private float _lastAttackProgress = -1f;

	private float _lastHitProgress = -1f;

	private bool _poseInitialized;

	private int _attackPoseMotionVersion = -1;

	private bool _usesExplicitPoses;

	private bool _lastGuarding;

	private bool _r2Idle;

	private ShaderMaterial _surface;

	private Texture2D? _boundTexture;

	private MeshInstance3D _shadow;

	private int _hp;

	private int _maxHp;

	private int _blockValue;

	private float _deathFade;

	private float _imageHeight;

	private float _feetPad;

	private float _lastClock;

	private float _idleDuration = 0.25f;

	private string _actorCategory = "";

	private Basis _layoutBasis = Basis.Identity;

	private Camera3D? _layoutCamera;

	private JObject? _sourceLook;

	private readonly ActorMaterialSettings.Context _sourceContext = new ActorMaterialSettings.Context
	{
		Diorama = true
	};

	private readonly ActorMaterialSettings.ViewInput _sourceView = new ActorMaterialSettings.ViewInput();

	private readonly Vector4[] _lightPositions = new Vector4[8];

	private readonly Vector4[] _lightColors = new Vector4[8];

	private float _sourceDistanceScale = 1f;

	private float _edgeLinear = -1f;

	private float _flashUntil;

	private float _dissolve;

	private float _sourceUnitsPerCanvasPixel;

	private int _sourceMotionFrame;

	private int _poseSampleFrame = int.MinValue;

	private int _sourceDisplayAct;

	private int _sourceDisplayLutKey;

	private ImageTexture? _sourceDisplayLut;

	private readonly bool _paletteHarmonyRequested = !Enumerable.Contains(OS.GetCmdlineUserArgs(), "--battle-palette-baseline");
	private bool _sourceDisplayRequested = !Enumerable.Contains(OS.GetCmdlineUserArgs(), "--actor-color-baseline");

	private bool _otherActSourceDisplayRequested = !Enumerable.Contains(OS.GetCmdlineUserArgs(), "--actor-color-other-acts-baseline");

	private bool _sourceDisplayLifecycleBound;

	private EnemyAttackRigProfile? _enemyAttackRig;

	private EnemyAttackArticulation? _enemyAttackMotion;

	private Style04EnemyMotion? _style04Motion;
	private Style04EnemySupportMotion? _style04Support;
 private BruteAuthoredFrames? _bruteAuthoredFrames;
 private WolfAuthoredFrames? _wolfAuthoredFrames;
 private CrossbowAuthoredFrames? _crossbowAuthoredFrames;
 private ContactAuthoredFrames? _contactAuthoredFrames;
 private DrummerAuthoredFrames? _drummerAuthoredFrames;
 private HexAuthoredFrames? _hexAuthoredFrames;
 private BruteAuthoredFrames.Snapshot _sporeFrame=BruteAuthoredFrames.Snapshot.Disabled;
 private EnemyAuthoredClipSet? _authoredClipSet;
 // Compatibility field for existing brute pixel/disposal QA; ownership is the shared clip set.
 private System.Collections.Generic.Dictionary<string,Texture2D[]> _bruteAuthoredTextures=new();
 #if DEBUG
 internal EnemyAuthoredClipSet DebugClipSet => _authoredClipSet!;
 internal void DebugResetMotion() { _style04Support?.Cancel(); Motion.Cancel(); UiLunge.Cancel(); CancelExecutionScale(); CancelEnemyAttack(); Position=Home; ResumeAuthoredIdleForPlacement(0); }
 internal void DebugDisposeFrames() => DisposeAuthoredFrames();
#endif
 public bool AuthoredFramesEnabled => _authoredClipSet != null;
 public bool BruteAuthoredFramesEnabled => _bruteAuthoredFrames != null;
 public bool WolfAuthoredFramesEnabled => _wolfAuthoredFrames != null;
 public bool CrossbowAuthoredFramesEnabled => _crossbowAuthoredFrames != null;
 public bool ContactAuthoredFramesEnabled => _contactAuthoredFrames != null;
 public int AuthoredEngineHits=>_contactAuthoredFrames?.AuthenticatedEngineHits??0;
 public int AuthoredPresentationContacts=>_contactAuthoredFrames?.AuthenticatedPresentationContacts??0;
 public string? AuthoredContactAggregationReason=>_contactAuthoredFrames?.AuthenticatedPresentationReason;
 public bool HealAuthoredFramesEnabled=>DefinitionId=="enemy_moss_healer"&&_contactAuthoredFrames!=null;
 public float HealAuthoredOutcomeAt=>_contactAuthoredFrames?.HealOutcomeAt??-1;
 public float HealAuthoredAppliedAt=>_contactAuthoredFrames?.HealAppliedAt??-1;
 public int HealAuthoredAmount=>_contactAuthoredFrames?.HealAmount??0;
 public int HealAuthoredTargetIndex=>_contactAuthoredFrames?.HealTargetIndex??-1;
 public int HealAuthoredOutcomeEventIndex=>_contactAuthoredFrames?.HealOutcomeEventIndex??-1;
 public Style04EnemySupportMotion.Token HealAuthoredOutcomeToken=>_contactAuthoredFrames?.HealOutcomeToken??default;
 public bool MillAuthoredFramesEnabled=>DefinitionId=="enemy_elite_devourer"&&_contactAuthoredFrames!=null;
 public float MillAuthoredAppliedAt=>_contactAuthoredFrames?.MillAppliedAt??-1;
 public int MillAuthoredAppliedCount=>_contactAuthoredFrames?.MillAppliedCount??0;
 public int MillAuthoredAppliedEventIndex=>_contactAuthoredFrames?.MillAppliedEventIndex??-1;
 public Style04EnemySupportMotion.Token MillAuthoredAppliedToken=>_contactAuthoredFrames?.MillAppliedToken??default;
 public bool HexAuthoredFramesEnabled => _hexAuthoredFrames!=null;
 public float HexAuthoredAppliedAt=>_hexAuthoredFrames?.AppliedAt??-1;
 public float HexAuthoredBlockedAt=>_hexAuthoredFrames?.BlockedAt??-1;
 public int HexAuthoredAppliedEventIndex=>_hexAuthoredFrames?.AppliedEventIndex??-1;
 public Style04EnemySupportMotion.Token HexAuthoredAppliedToken=>_hexAuthoredFrames?.AppliedToken??default;
 public bool DrummerAuthoredFramesEnabled => _drummerAuthoredFrames != null;
 public float DrummerAuthoredAppliedAt => _drummerAuthoredFrames?.AcceptedAppliedAt ?? -1f;
 public int DrummerAuthoredAppliedEventIndex => _drummerAuthoredFrames?.AppliedEventIndex ?? -1;
 public Style04EnemySupportMotion.Token DrummerAuthoredAppliedToken => _drummerAuthoredFrames?.AppliedToken ?? Style04EnemySupportMotion.Token.Invalid;
 public float DrummerAuthoredResolvedAt => _drummerAuthoredFrames?.AcceptedResolvedAt ?? -1f;
 public Style04EnemySupportMotion.Token DrummerAuthoredResolvedToken => _drummerAuthoredFrames?.ResolvedToken ?? Style04EnemySupportMotion.Token.Invalid;
 public float CrossbowAuthoredReleaseAt => _crossbowAuthoredFrames?.AcceptedReleaseAt ?? -1f;
 internal Texture2D RosterLayoutTexture => AuthoredFramesEnabled ? _baseTexture : Sprite.Texture;
 public BruteAuthoredFrames.Snapshot BruteAuthoredFrameSample => _bruteAuthoredFrames?.State ?? BruteAuthoredFrames.Snapshot.Disabled;
 public BruteAuthoredFrames.Snapshot AuthoredFrameSample => (_hexAuthoredFrames!=null||DefinitionId is "enemy_winch_warden" or "enemy_seal_orb" or "enemy_kin_priest" or "enemy_crab_cannon" or "enemy_kin_follower" or "enemy_thunder_globe" or "enemy_elite_sentry" or "enemy_bomber" or "enemy_slug" or "enemy_iron_clam" or "enemy_lost_soul" or "enemy_mud_lump" or "enemy_sludge_spider")?_sporeFrame:_drummerAuthoredFrames?.State ?? _contactAuthoredFrames?.State ?? _crossbowAuthoredFrames?.State ?? _wolfAuthoredFrames?.State ?? BruteAuthoredFrameSample;
 public string AuthoredManifestSha256 => _authoredClipSet?.ManifestSha256 ?? "";

	private MeshInstance3D? _enemyAttackMesh;

	private ShaderMaterial? _enemyAttackMaterial;

	private StringName[] _enemyAttackSourceUniforms = System.Array.Empty<StringName>();

	private bool _enemyAttackConfigured;

	private bool _enemyAttackDeathFrozen;

	private bool _enemyAttackSharedPoseWritten;

	private MeshInstance3D[] _enemyAttackParts = System.Array.Empty<MeshInstance3D>();

	private ShaderMaterial[] _enemyAttackPartMaterials = System.Array.Empty<ShaderMaterial>();

	private Texture2D? _enemyAttackPartMask;

	private Texture2D? _enemyAttackUnderpaint;

	private Vector3 _enemyAttackShadowHome;

	private static readonly System.Collections.Generic.Dictionary<EnemyAttackRigProfile, AttackGeometry> EnemyAttackGeometryCache = new System.Collections.Generic.Dictionary<EnemyAttackRigProfile, AttackGeometry>();

	private static readonly ArrayMesh?[] EnemyAttackRigidGeometryCache = new ArrayMesh[6];

	private readonly bool[] _enemyAttackFixedBodyJoints = new bool[6];

	private readonly float[] _enemyAttackJointAngles = new float[6];

	private readonly Vector2[] _enemyAttackJointTranslations = new Vector2[6];

	private static readonly StringName EnemyAttackLayerMode = "_EnemyAttackLayerMode";

	private static readonly StringName EnemyAttackSpiritBody = "_SpiritBodyOffset";

	private static readonly StringName EnemyAttackSpiritFin = "_SpiritFinOffset";

	private static readonly StringName EnemyAttackSpiritTail = "_SpiritTailOffset";

	private static readonly StringName[] EnemyAttackRowsX = new StringName[6] { "_EnemyAttack0X", "_EnemyAttack1X", "_EnemyAttack2X", "_EnemyAttack3X", "_EnemyAttack4X", "_EnemyAttack5X" };

	private static readonly StringName[] EnemyAttackRowsY = new StringName[6] { "_EnemyAttack0Y", "_EnemyAttack1Y", "_EnemyAttack2Y", "_EnemyAttack3Y", "_EnemyAttack4Y", "_EnemyAttack5Y" };

	private EnemySpiritProfile? _rosterSpiritProfile;

	private EnemySpiritProfile.Variant? _rosterSpiritVariant;

	private float _executionStart;

	private float _executionDuration;

	private float _executionX;

	private float _executionY;

	private bool _executionPuff;

	private int _executionVersion;

	private string _spiritProfileId = "";

	private int _spiritRenderScale = 1;

	private const string SpiritProbeShaderPath = "res://shaders/rpg_spirit_probe.gdshader";

	private bool _spiritProbeEnabled;

	private bool _spiritProbeStill;

	private Texture2D? _spiritProbeWeights;

	private Shader? _spiritProbeShader;

	public string DefinitionId { get; private set; } = "";

	public string ArtId { get; private set; } = "";

	public bool Phone96 { get; private set; }

	public bool Style04StaticSprite { get; private set; }
	private int _style04PaletteProfile;
	public Style04EnemySupportMotion.Snapshot Style04SupportSample => _style04Support?.State ?? Style04EnemySupportMotion.Snapshot.Identity;

	public bool Style04MotionEnabled => _style04Motion != null;
	public Style04EnemyMotion.Snapshot Style04MotionSample => _style04Motion?.State ?? Style04EnemyMotion.Snapshot.Identity;

	public int BaseTextureWidth => _baseTexture.GetWidth();

	public int BaseTextureHeight => _baseTexture.GetHeight();

	public int TopMarginRows { get; private set; }

	public float BaseCanvasWorldHeight => _imageHeight;

	public float SourceUiProxyHeight { get; private set; }

	public float SourceUiProxyUnit { get; private set; }

	public int EnemyIndex { get; set; } = -1;

	public string Uid { get; set; } = "";

	public Vector3 Home { get; set; }

	public float Height { get; private set; } = 2.1f;

	public int DisplayedHp => _hp;

	public int DisplayedBlock => _blockValue;

	public bool Placeholder { get; private set; }

	public bool Dead { get; private set; }

	public bool Selected { get; set; }

	public Sprite3D Sprite { get; private set; }

	public Label3D Vitals { get; private set; }

	public Label3D IntentLabel { get; private set; }

	public int IdleSlot { get; set; } = -1;

	public bool ShowLabels { get; set; }

	public float SourceMotionHeight
	{
		get
		{
			if (Sprite != null)
			{
				return (float)Sprite.Texture.GetHeight() * Sprite.PixelSize - _feetPad;
			}
			return _imageHeight - _feetPad;
		}
	}

	public Vector3 SourceMotionFeet => Home + Motion.Offset + UiLunge.Offset;

	public SourceActorMotion Motion { get; } = new SourceActorMotion();

	public SourceUiLunge UiLunge { get; } = new SourceUiLunge();

	public int PoseStartCount { get; private set; }

	public string PoseName => _pose;

	public int PoseFrame => _poseFrame;

	public float PoseFrameTime => _poseFrameTime;

	public bool HasActivePose
	{
		get
		{
			if (_pose != "idle")
			{
				List<Texture2D> list = PoseFrames(_pose);
				if (list != null)
				{
					return list.Count > 0;
				}
				return false;
			}
			return false;
		}
	}

	public Vector3 Contact => SourceMotionFeet + _layoutBasis.Y * Height * 0.54f;

	public Vector3 Impact => SourceMotionFeet + _layoutBasis.Y * Height * 0.5f + _layoutBasis.Z * 0.28f;

	public float DissolveAmount => _dissolve;

	public bool SourceDisplayTransferActive { get; private set; }

	public string SourceDisplayTransferStatus { get; private set; } = "legacy:not-configured";

	public bool OtherActSourceDisplayRequested => _otherActSourceDisplayRequested;

	public bool SourceDisplayTransferRequested => _sourceDisplayRequested;

	public bool EnemyAttackSharedMaterial { get; private set; }

	public bool EnemyAttackPrepared => _enemyAttackMotion != null;

	public bool EnemyAttackGeometryCacheHit { get; private set; }

	public double EnemyAttackPrepareMilliseconds { get; private set; }

	public bool EnemyAttackActive => _enemyAttackMotion?.Active ?? false;

	public string EnemyAttackPhase => _enemyAttackMotion?.Phase ?? "idle";

	public string EnemyAttackProfileId => _enemyAttackRig?.Id ?? "";

	public string EnemyAttackMoveId => _enemyAttackMotion?.MoveId ?? "";

	public int EnemyAttackContactCount => _enemyAttackMotion?.ContactCount ?? 0;

	public float EnemyAttackLastContactAt => _enemyAttackMotion?.LastContactAt ?? (-1f);

	public int EnemyAttackJointCount => _enemyAttackRig?.Joints.Length ?? 0;

	public int EnemyAttackVertexCount { get; private set; }

	public int EnemyAttackTriangleCount { get; private set; }

	public IReadOnlyList<float> EnemyAttackJointAngles => _enemyAttackJointAngles;

	public IReadOnlyList<Vector2> EnemyAttackJointTranslations => _enemyAttackJointTranslations;

	public bool EnemyAttackMeshVisible => _enemyAttackMesh?.Visible ?? false;

	public bool EnemyAttackLayered => _enemyAttackParts.Length != 0;

	public bool EnemyAttackDeathFrozen => _enemyAttackDeathFrozen;

	public int EnemyAttackLayerCount
	{
		get
		{
			int count = (EnemyAttackMeshVisible ? 1 : 0);
			MeshInstance3D[] enemyAttackParts = _enemyAttackParts;
			for (int i = 0; i < enemyAttackParts.Length; i++)
			{
				if (enemyAttackParts[i].Visible)
				{
					count++;
				}
			}
			return count;
		}
	}

	public float EnemyAttackVisualLungeScale => _enemyAttackRig?.Layers?.LungeScale ?? 1f;

	public Vector3 EnemyAttackVisualRootOffset { get; private set; }

	public string EnemyAttackStatus { get; private set; } = "original:not-started";

	public Aabb EnemyAttackMeshBounds => _enemyAttackMesh?.CustomAabb ?? default(Aabb);

	public Vector2 ExecutionScale { get; private set; } = Vector2.One;

	public bool ExecutionScaleActive => _executionDuration > 0f;

	public int ExecutionVersion => _executionVersion;

	public int SpiritRenderScale => _spiritRenderScale;

	internal int SpiritSizingWidth => _rosterSpiritVariant?.Width ?? (BaseTextureWidth / _spiritRenderScale);

	internal int SpiritSizingHeight => _rosterSpiritVariant?.Height ?? (BaseTextureHeight / _spiritRenderScale);

	internal int SpiritSizingTopRows => _rosterSpiritVariant?.Top ?? (TopMarginRows / _spiritRenderScale);

	internal int SpiritDeathTextureHeight
	{
		get
		{
			EnemySpiritProfile.Variant? rosterSpiritVariant = _rosterSpiritVariant;
			if ((object)rosterSpiritVariant == null)
			{
				if (_spiritRenderScale <= 1)
				{
					return Sprite.Texture.GetHeight();
				}
				return 64;
			}
			return rosterSpiritVariant.Height;
		}
	}

	public string SpiritProfileId => _spiritProfileId;

	public bool SpiritProbeEnabled => _spiritProbeEnabled;

	public string SpiritProbeStatus { get; private set; } = "original:not-requested";

	public SpiritProbeIdleMotion.Offsets SpiritProbeOffsets { get; private set; }

	public void SetSourceUiProxyMetrics(float height, float unit)
	{
		if (float.IsFinite(height) && height > 0f && float.IsFinite(unit) && unit > 0f)
		{
			SourceUiProxyHeight = height;
			SourceUiProxyUnit = unit;
		}
	}

	public void SetSourceCanvasMotionScale(float unitsPerCanvasPixel)
	{
		_sourceUnitsPerCanvasPixel = unitsPerCanvasPixel;
	}

	public Vector3 SourceCanvasDisplacement(Vector2 canvasDelta)
	{
		return Basis * _layoutBasis * new Vector3(canvasDelta.X, canvasDelta.Y, 0f) * _sourceUnitsPerCanvasPixel;
	}

	public void BeginSourceLunge(Vector2 canvasDelta, float now, int frame, float duration = 0.28f, float delay = 0f, float holdAtPeak = 0f)
	{
		UiLunge.Begin(SourceCanvasDisplacement(canvasDelta), duration, now, frame, delay, holdAtPeak);
	}

	// Fault recovery clears ownership without touching a potentially damaged renderer.
	public void AbandonPresentationForFault(float clock)
	{
		Motion.Cancel();
		UiLunge.Cancel();
		CancelExecutionScale();
		_enemyAttackMotion?.Cancel();
		_pose = "idle";
		_poseFrame = 0;
		_poseFrameTime = 0f;
		_poseClock = clock;
		_poseInitialized = true;
	}

	public void CancelSourceMotion(float clock)
	{
		Motion.Cancel();
		UiLunge.Cancel();
		CancelExecutionScale();
		CancelEnemyAttack();
		PlayPose("idle", clock);
	}

	// Accepted authored keysets are the normal game path; unlisted enemies keep their existing path.
	internal static bool UsesDefaultAuthoredFrames(string id) => id is
        "enemy_ember_furnace" or
        "enemy_chimera_3" or "enemy_winch_warden" or
        "enemy_chimera_1" or "enemy_chimera_2" or
        "enemy_seal_orb" or "enemy_warden" or
        "enemy_set_breaker" or "enemy_kin_priest" or
        "enemy_moss_spawner" or "enemy_crab_cannon" or
        "enemy_kin_follower" or "enemy_crab_crusher" or
        "enemy_moss_healer" or "enemy_thunder_globe" or
        "enemy_raptor_egg" or "enemy_sludge_berserker" or
        "enemy_elite_mirror_djinn" or "enemy_elite_devourer" or
        "enemy_elite_iron_egg" or "enemy_elite_sentry" or
        "enemy_mimic_imp" or "enemy_mimic_jester" or
		"enemy_thorn_squirrel" or "enemy_snap_fruit" or "enemy_set_wary" or
		"enemy_thief" or "enemy_crossbow_archer" or "enemy_probe" or
		"enemy_wolf" or "enemy_turtle" or "enemy_vine_walker" or
		"enemy_drummer" or "enemy_spore_cap" or "enemy_hexer" or
		"enemy_brute" or "enemy_apprentice_colossus" or "enemy_cultist" or
		"enemy_moss_slime" or "enemy_elite_gold_raven" or "enemy_elite_owl" or
		"enemy_wide_power" or "enemy_big_slime" or "enemy_shield_squire" or
		"enemy_bond_wolf" or "enemy_elite_sergeant" or "enemy_mourn_beast" or
		"enemy_devoted_sculptor" or "enemy_brood_raptor" or "enemy_bowl_bug" or
		"enemy_brood_toad" or "enemy_broodling" or "enemy_raptor_chick" or
		"enemy_axe_automaton_reboot" or "enemy_shell_guard" or "enemy_moss" or
		"enemy_rock_beetle" or "enemy_elite_deathless" or
		"enemy_burrow_worm" or "enemy_nemesis_wraith" or
		"enemy_axe_ogre" or "enemy_forgotten_soul" or
		"enemy_elite_husk_3" or "enemy_elite_husk_2" or "enemy_elite_husk_1" or "enemy_mudling" or "enemy_lost_soul" or "enemy_strangler_serpent" or "enemy_elite_stab_book" or "enemy_mud_lump" or "enemy_sludge_spider" or "enemy_whetstone_colossus" or "enemy_iron_clam" or "enemy_elite_giant_face" or "enemy_elite_doom_chanter" or "enemy_axe_automaton" or "enemy_biting_scroll" or "enemy_slug" or "enemy_haze_stag" or "enemy_cinder_imp" or "enemy_scald_gnat" or "enemy_bomber" or "enemy_chomper" or "enemy_maw_hunter" or "enemy_cog_construct" or "enemy_gaping_maw" or "enemy_joker" or "enemy_frog_knight" or "enemy_elite_slaver";

	public void Configure(string category, string id, string displayName, Vector3 foot, float height = 2.1f, bool phone = false)
	{
		DisposeAuthoredFrames();
		_actorCategory = category;
		Name = (category + "_" + id).Replace(':', '_');
		DefinitionId = id;
		Home = foot;
		Position = foot;
		Height = height;
		Style04EnemySpriteRegistry.Entry registered = default;
		Style04StaticSprite = category == "enemies" && (UsesDefaultAuthoredFrames(id) || Enumerable.Contains(OS.GetCmdlineUserArgs(), "--enemy-style04-batch1")) && Style04EnemySpriteRegistry.TryGet(id, out registered);
		_style04PaletteProfile = Style04StaticSprite ? registered.PaletteProfile : 0;
		_style04Support?.Cancel();
		_style04Support = null;
		_style04Motion = Style04StaticSprite && Style04EnemyMotion.Supports(id) ? new Style04EnemyMotion(id) : null;
		// Isolated one-creature visual prototype. Do not reuse the old anatomy rig/maps.
		bool style04Prototype = Style04StaticSprite || (category == "enemies" && id == "enemy_brute" &&
			Enumerable.Contains(OS.GetCmdlineUserArgs(), "--enemy-brute-style04-prototype"));
		Texture2D tex;
		if (style04Prototype)
		{
			string stem = "res://assets/Art/enemies/prototype_style04/enemy_brute";
			string path = Style04StaticSprite ? registered.Path : (phone && ResourceLoader.Exists(stem + "_96.png") ? stem + "_96.png" : stem + ".png");
			tex = TryTexture(path) ?? throw new InvalidOperationException("Style04 prototype sprite is absent: " + path);
			if (Style04StaticSprite ? tex.GetWidth() != registered.Width || tex.GetHeight() != registered.Height : (tex.GetWidth() != tex.GetHeight() || (tex.GetWidth() != 96 && tex.GetWidth() != 112 && tex.GetWidth() != 128)))
				throw new InvalidOperationException("Style04 sprite does not match its registered native canvas.");
			Phone96 = tex.GetWidth() == 96;
			SpiritProbeStatus = Style04StaticSprite ? "style04:batch1-static-no-old-anatomy" : "prototype:style04-static-no-old-anatomy";
			SetMeta("style04_native_grid", new Vector2I(tex.GetWidth(), tex.GetHeight()));
			SetMeta("style04_common_pixel_scale", Style04StaticSprite);
			SetMeta("style04_static_prototype", true);
		}
		else
		{
			tex = TryTexture($"res://assets/Art/{category}/{id}.png");
			tex = SelectSpiritProbeTexture(category, id, tex, phone);
		}
		ArtId = ((_rosterSpiritProfile != null && Phone96) ? (id + "_96") : id);
		if (!style04Prototype && (!_spiritProbeEnabled & phone) && category == "enemies" && tex != null && tex.GetWidth() >= 128)
		{
			Texture2D smaller = TryTexture("res://assets/Art/enemies/" + id + "_96.png");
			if (smaller != null)
			{
				tex = smaller;
				Phone96 = true;
				ArtId = id + "_96";
			}
		}
		Placeholder = tex == null;
		_idleDuration = ((id == "leader_blue") ? 0.125f : 0.25f);
		if (tex == null)
		{
			tex = ((category == "dolls") ? ProceduralActorTexture.Doll(id, 32) : ProceduralActorTexture.Generate(id, category == "leaders", 64));
		}
		_baseTexture = tex;
		_footAnchorOffsetX = ((id == "leader_blue") ? (1f / 32f * (float)tex.GetWidth()) : 0f);
		_imageHeight = height;
		Image pixels = tex.GetImage();
		int bottom = 0;
		int top = 0;
		for (int y = pixels.GetHeight() - 1; y >= 0; y--)
		{
			bool solid = false;
			for (int x = 0; x < pixels.GetWidth(); x++)
			{
				if (pixels.GetPixel(x, y).A >= 0.0625f)
				{
					solid = true;
					break;
				}
			}
			if (solid)
			{
				break;
			}
			bottom++;
		}
		for (int i = 0; i < pixels.GetHeight(); i++)
		{
			bool solid2 = false;
			for (int j = 0; j < pixels.GetWidth(); j++)
			{
				if (pixels.GetPixel(j, i).A >= 0.0625f)
				{
					solid2 = true;
					break;
				}
			}
			if (solid2)
			{
				break;
			}
			top++;
		}
		ApplySpiritRegisteredMargins(ref top, ref bottom);
		_bottomMarginRows = bottom;
		TopMarginRows = ((top < tex.GetHeight()) ? top : 0);
		_feetPad = height * (float)bottom / (float)tex.GetHeight();
		Height = height * (float)(tex.GetHeight() - top - bottom) / (float)tex.GetHeight();
		Sprite = new Sprite3D
		{
			Name = "Artwork",
			Texture = tex,
			PixelSize = height / (float)tex.GetHeight(),
			Position = new Vector3(0f, height / 2f - _feetPad, 0f),
			Billboard = BaseMaterial3D.BillboardModeEnum.Disabled,
			Shaded = true,
			DoubleSided = true,
			AlphaCut = SpriteBase3D.AlphaCutMode.Discard,
			AlphaScissorThreshold = 0.4f,
			TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
		};
		_surface = new ShaderMaterial
		{
			Shader = (_spiritProbeEnabled ? _spiritProbeShader : GD.Load<Shader>(Style04StaticSprite ? "res://shaders/style04_StageUnitOverlay.gdshader" : "res://shaders/authored_StageUnitLit.gdshader"))
		};
		Sprite.Offset = new Vector2(_footAnchorOffsetX, 0f);
		Sprite.MaterialOverride = _surface;
		BindSurface(tex);
		AddChild(Sprite, forceReadableName: false, InternalMode.Disabled);
		if (_spiritProbeEnabled)
		{
			ConfigureSpiritProbeIdle(tex);
		}
		else if (!style04Prototype)
		{
			string category2 = ((category == "leaders") ? "leaders" : "enemies");
			LoadFrames(category2, ArtId, "idle", _idle);
			if (category == "enemies" || category == "dolls")
			{
				List<Texture2D> authoredIdle = new List<Texture2D>();
				LoadFrames("enemies", ArtId, "r2idle", authoredIdle);
				if (authoredIdle.Count > 0 && authoredIdle[0].GetWidth() == tex.GetWidth() && authoredIdle[0].GetHeight() == tex.GetHeight())
				{
					_idle.Clear();
					_idle.AddRange(authoredIdle);
					_r2Idle = true;
					_idleDuration = ((id.StartsWith("enemy_brute", StringComparison.Ordinal) || id.StartsWith("enemy_haze_stag", StringComparison.Ordinal) || id.StartsWith("enemy_kin_", StringComparison.Ordinal)) ? 0.4f : 0.25f);
				}
			}
			LoadFrames(category2, ArtId, "attack", _attack);
			LoadFrames(category2, ArtId, "hurt", _hurt);
			LoadFrames(category2, ArtId, "block", _block);
		}
		Vitals = Label(displayName, height + 0.16f, 25);
		IntentLabel = Label("", height + 0.48f, 22);
		_shadow = new MeshInstance3D
		{
			Name = "ContactShadow",
			Mesh = new PlaneMesh
			{
				Size = new Vector2(height * 0.56f, height * 0.24f)
			},
			Position = new Vector3(0f, 0.014f, 0f),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
		};
		ShaderMaterial sm = new ShaderMaterial
		{
			Shader = GD.Load<Shader>("res://shaders/actor_shadow.gdshader")
		};
		_shadow.MaterialOverride = sm;
		AddChild(_shadow, forceReadableName: false, InternalMode.Disabled);
		PrepareEnemyAttackPresentation();
        InitializeAuthoredFrames();
	}

	public void SetSourceImageHeight(float height)
	{
		if (float.IsFinite(height) && !(height <= 0f) && Sprite != null)
		{
			_imageHeight = height;
			_feetPad = height * (float)_bottomMarginRows / (float)BaseTextureHeight;
			Height = height * (float)(BaseTextureHeight - TopMarginRows - _bottomMarginRows) / (float)BaseTextureHeight;
			Sprite.PixelSize = height / (float)BaseTextureHeight;
			Sprite.Position = _layoutBasis.Y * (height / 2f - _feetPad);
			Vitals.Position = new Vector3(0f, height + 0.16f, 0.01f);
			IntentLabel.Position = new Vector3(0f, height + 0.48f, 0.01f);
			if (_shadow.Mesh is PlaneMesh plane)
			{
				plane.Size = new Vector2(height * 0.56f, height * 0.24f);
			}
			RefreshSourceMaterial();
		}
	}

	private Label3D Label(string text, float y, int size)
	{
		Label3D l = new Label3D
		{
			Text = text,
			Position = new Vector3(0f, y, 0.01f),
			FontSize = size,
			PixelSize = 0.006f,
			Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
			NoDepthTest = true,
			Visible = ShowLabels,
			Modulate = new Color("e9e8e0"),
			OutlineModulate = new Color("10131b"),
			OutlineSize = 6,
			Shaded = false
		};
		Font font = GD.Load<Font>("res://assets/Fonts/NotoSansJP-Regular.otf");
		if (font != null)
		{
			l.Font = font;
		}
		AddChild(l, forceReadableName: false, InternalMode.Disabled);
		return l;
	}

	private void BindSurface(Texture2D texture)
	{
		if (texture != _boundTexture)
		{
			_boundTexture = texture;
			_surface.SetShaderParameter("_BaseMap", texture);
			object obj;
			if (!texture.ResourcePath.EndsWith(".png"))
			{
				obj = "";
			}
			else
			{
				string resourcePath = texture.ResourcePath;
				obj = resourcePath.Substring(0, resourcePath.Length - 4);
			}
			string stem = (string)obj;
			Texture2D normal = ((stem.Length > 0) ? TryTexture(stem + "_n.png") : null);
			Texture2D emission = ((stem.Length > 0) ? TryTexture(stem + "_e.png") : null);
			_surface.SetShaderParameter("_HasNormal", (normal != null) ? 1f : 0f);
			_surface.SetShaderParameter("_NormalMap", normal);
			_surface.SetShaderParameter("_HasEmission", (emission != null) ? 1f : 0f);
			_surface.SetShaderParameter("_EmissionMap", emission);
			RefreshSourceMaterial();
		}
	}

	private static Texture2D? TryTexture(string p)
	{
		if (!ResourceLoader.Exists(p))
		{
			return null;
		}
		return GD.Load<Texture2D>(p);
	}

	private static void LoadFrames(string category, string id, string kind, List<Texture2D> into)
	{
		for (int i = 0; i < 16; i++)
		{
			Texture2D t = TryTexture($"res://assets/Art/{category}/anim/{id}_{kind}_{i}.png");
			if (t != null)
			{
				into.Add(t);
				continue;
			}
			break;
		}
	}

	public void PlayPose(string name, float clock)
	{
        if (!Dead) { _bruteAuthoredFrames?.NotifyPose(name, clock, ExecutionVersion); _wolfAuthoredFrames?.NotifyPose(name,clock,ExecutionVersion); if(_hexAuthoredFrames==null)_crossbowAuthoredFrames?.NotifyPose(name,clock,ExecutionVersion); _contactAuthoredFrames?.NotifyPose(name,clock,ExecutionVersion); }
		_usesExplicitPoses = true;
		StartPose(name, clock);
        ApplyAuthoredFrame(clock);
	}

	private void StartPose(string name, float clock)
	{
		List<Texture2D> frames = PoseFrames(name);
		if (frames != null && frames.Count != 0)
		{
			PoseStartCount++;
			if (name == "attack") _attackPoseMotionVersion = Motion.MotionVersion;
			_pose = name;
			_poseFrame = 0;
			_poseFrameTime = 0f;
			_poseClock = clock;
			_poseInitialized = true;
			ApplyPoseFrame();
		}
	}

	// At low FPS the short authored impact drawing can be crossed entirely
	// between samples. Anchor that drawing at the first actual numeric contact
	// without replaying the windup, restarting the action, or adding a wait.
	public bool SynchronizeAttackContactPose(SourceActorMotion.OwnershipToken token, float clock)
	{
		if (Dead || _attack.Count == 0 || !float.IsFinite(clock) || !Motion.Owns(token)) return false;
		if (!Motion.SynchronizeAttackContact(token)) return false;
		_usesExplicitPoses = true;
		if (_attackPoseMotionVersion != Motion.MotionVersion) PoseStartCount++;
		_attackPoseMotionVersion = Motion.MotionVersion;
		_pose = "attack";
		_poseFrame = Math.Min(2, _attack.Count - 1);
		_poseFrameTime = 0f;
		_poseClock = clock;
		_poseInitialized = true;
		ApplyPoseFrame();
		return true;
	}

	private List<Texture2D>? PoseFrames(string name)
	{
		return name switch
		{
			"idle" => _idle, 
			"attack" => _attack, 
			"hurt" => _hurt, 
			"block" => _block, 
			_ => null, 
		};
	}

	// Authored attack strip index 2 is the impact drawing. Derive its lead
	// from the same duration table used by playback, including Blue's timing.
	// Actors without an attack strip have no sprite contact lead.
	public float AttackPoseContactLead
	{
		get
		{
			float[] durations = DefinitionId == "leader_blue" ? BlueAttackDur : AttackDur;
			int contactFrame = Math.Clamp(_attack.Count - 1, 0, 2);
			float lead = 0f;
			for (int i = 0; i < contactFrame; i++) lead += i < durations.Length ? durations[i] : 0.125f;
			return lead;
		}
	}

	private float PoseFrameDuration()
	{
		if (_pose == "idle")
		{
			return _idleDuration;
		}
		float[] durs = _pose switch
		{
			"attack" => (DefinitionId == "leader_blue") ? BlueAttackDur : AttackDur, 
			"hurt" => HurtDur, 
			"block" => (DefinitionId == "leader_blue") ? BlueBlockDur : BlockDur, 
			_ => System.Array.Empty<float>(), 
		};
		if (_poseFrame >= durs.Length)
		{
			return 0.125f;
		}
		return durs[_poseFrame];
	}

	private static float VanDerCorput(int slot)
	{
		float value = 0f;
		float bit = 0.5f;
		int n = Math.Max(0, slot);
		while (n > 0)
		{
			if ((n & 1) != 0)
			{
				value += bit;
			}
			n >>= 1;
			bit *= 0.5f;
		}
		return value;
	}

	private void AdvancePose(float clock)
	{
		AdvancePose(clock, Math.Max(0f, clock - _poseClock));
	}

	private void AdvancePose(float clock, float frameDelta)
	{
		if (!_poseInitialized)
		{
			float cycle = _idleDuration * (float)_idle.Count;
			if (cycle > 0f)
			{
				float phase = (_r2Idle ? (VanDerCorput((IdleSlot >= 0) ? IdleSlot : EnemyIndex) * cycle) : 0f);
				float age = Mathf.PosMod(clock + phase, cycle);
				_poseFrame = Math.Clamp((int)(age / _idleDuration), 0, _idle.Count - 1);
				_poseFrameTime = age - (float)_poseFrame * _idleDuration;
			}
			_poseClock = clock;
			_poseInitialized = true;
			ApplyPoseFrame();
			return;
		}
		// A pose started during this presentation frame must not consume time
		// from before its start. Keep elapsed time at low FPS instead of slowing
		// only the sprite while the approach/contact clocks continue normally.
		float delta = Math.Min(Math.Max(0f, frameDelta), Math.Max(0f, clock - _poseClock));
		delta = Motion.UnpausedDuration(clock - delta, clock);
		_poseClock = clock;
		List<Texture2D> frames = PoseFrames(_pose);
		if (frames == null || frames.Count == 0)
		{
			return;
		}
		_poseFrameTime += delta;
		bool changed = false;
		while (_poseFrameTime >= PoseFrameDuration())
		{
			_poseFrameTime -= PoseFrameDuration();
			_poseFrame++;
			changed = true;
			if (_poseFrame >= frames.Count)
			{
				_poseFrame = 0;
				_pose = "idle";
				frames = _idle;
				if (frames.Count == 0) break;
				// Skip complete idle cycles after a long host frame.
				_poseFrameTime %= _idleDuration * frames.Count;
			}
		}
		if (changed) ApplyPoseFrame();
	}

	private void ApplyPoseFrame()
	{
        if (AuthoredFramesEnabled) { ApplyAuthoredFrame(_lastClock); return; }
		List<Texture2D> frames = PoseFrames(_pose);
		Texture2D texture = ((frames != null && frames.Count > 0) ? frames[Math.Clamp(_poseFrame, 0, frames.Count - 1)] : _baseTexture);
		Sprite.Offset = new Vector2(_footAnchorOffsetX, (float)(texture.GetHeight() - _baseTexture.GetHeight()) * 0.5f);
		Sprite.Texture = texture;
		BindSurface(texture);
		SampleSpiritProbeIdle();
	}

	public void SetVitals(int hp, int maxHp, int block)
	{
		_hp = hp;
		_maxHp = maxHp;
		_blockValue = block;
		Vitals.Text = $"{Math.Max(0, hp)}/{maxHp}" + ((block > 0) ? $"  ◇{block}" : "");
	}

	public void DamageFallback(int hpLoss, int blocked)
	{
		SetVitals(_hp - hpLoss, _maxHp, Math.Max(0, _blockValue - blocked));
	}

	public void GainBlock(int amount)
	{
		SetVitals(_hp, _maxHp, _blockValue + amount);
	}

	public void Heal(int amount)
	{
		SetVitals(Math.Min(_maxHp, _hp + amount), _maxHp, _blockValue);
	}

	public void SetDead(bool dead)
	{
		bool wasDead = Dead;
		Dead = dead;
        if(!dead&&wasDead&&(_contactAuthoredFrames?.HasFleeTerminal(ExecutionVersion)??false))_contactAuthoredFrames.Cancel();
		if (dead)
		{
			// An interrupted attacker may never receive its planned final contact.
			if (UiLunge.ContactHoldArmed) UiLunge.Cancel();
            if(_contactAuthoredFrames?.HasFleeTerminal(ExecutionVersion)??false){_style04Support?.Cancel();_style04Motion?.Cancel();ApplyAuthoredFrame(_lastClock);}
            else OnEnemyAttackDeath();
		}
		if (!dead)
		{
			if (_enemyAttackDeathFrozen)
			{
				CancelEnemyAttack();
			}
			_deathFade = 0f;
			SetDissolve(0f);
		}
	}

	public void SampleSourceMotion(float clock, float frameDelta, int presentationFrame)
	{
		UiLunge.Sample(clock, frameDelta, presentationFrame);
		string requestedPose = Motion.Sample(clock, frameDelta, presentationFrame);
		if (requestedPose != null)
		{
			PlayPose(requestedPose, Motion.RequestedPoseClock);
		}
	}

	public void UpdateVisual(float clock, float attackProgress = -1f, float hitProgress = -1f, bool guarding = false, int presentationFrame = -1, float frameDelta = -1f)
	{
		if (Sprite == null)
		{
			return;
		}
		float delta = ((frameDelta >= 0f) ? frameDelta : Math.Max(0f, clock - _lastClock));
		_lastClock = clock;
		int frame = ((presentationFrame >= 0) ? presentationFrame : (++_sourceMotionFrame));
		SampleSourceMotion(clock, delta, frame);
		Position = Home + Motion.Offset + UiLunge.Offset;
        if(Dead&&(_contactAuthoredFrames?.HasFleeTerminal(ExecutionVersion)??false))Position+=_layoutBasis.X*(Height*1.25f*_contactAuthoredFrames.FleeProgress(clock,ExecutionVersion));
		SampleExecutionScale(clock);
		Sprite.Position = _layoutBasis.Y * ((_imageHeight / 2f - _feetPad) * ExecutionScale.Y);
		Sprite.Basis = _layoutBasis.ScaledLocal(new Vector3(ExecutionScale.X, ExecutionScale.Y, 1f));
		if (_style04Motion != null && !Dead)
		{
			var attack = _style04Motion.Sample(clock);
            if (!AuthoredFramesEnabled) ApplyStyle04Motion(attack);
		}
		if (_style04Support != null && !Dead)
		{
			var support = _style04Support.Sample(clock);
			if (support.Active && !AuthoredFramesEnabled) ApplyStyle04Support(support);
		}
		_surface.SetShaderParameter("_Flash", Math.Clamp((_flashUntil - clock) / 0.18f, 0f, 1f) * 0.1f);
		if (Dead)
		{
			_deathFade = _dissolve;
			Sprite.Modulate = new Color(1f, 1f, 1f, 1f - _deathFade);
			Vitals.Visible = false;
			IntentLabel.Visible = false;
			_shadow.Visible = _deathFade < 1f;
			ApplyAuthoredFrame(clock);
			RefreshSourceMaterial();
			SampleEnemyAttack(clock);
			return;
		}
		Sprite.Modulate = Colors.White;
		_shadow.Visible = true;
		Vitals.Visible = ShowLabels;
		IntentLabel.Visible = ShowLabels;
		if (!_usesExplicitPoses)
		{
			if (attackProgress >= 0f && (_lastAttackProgress < 0f || attackProgress < _lastAttackProgress || guarding != _lastGuarding))
			{
				StartPose(guarding ? "block" : "attack", clock);
			}
			if (hitProgress >= 0f && (_lastHitProgress < 0f || hitProgress < _lastHitProgress))
			{
				StartPose("hurt", clock);
			}
		}
		if (hitProgress >= 0f && (_lastHitProgress < 0f || hitProgress < _lastHitProgress))
		{
			Flash(clock);
		}
		_lastAttackProgress = attackProgress;
		_lastHitProgress = hitProgress;
		_lastGuarding = guarding;
		if (_poseSampleFrame != frame)
		{
			_poseSampleFrame = frame;
			AdvancePose(clock, delta);
			SampleSpiritProbeIdle();
		}
		if (attackProgress >= 0f)
		{
			float pulse = MathF.Sin(Math.Clamp(attackProgress, 0f, 1f) * (float)Math.PI);
			Position += new Vector3((EnemyIndex >= 0) ? ((0f - pulse) * 0.2f) : (pulse * 0.2f), 0f, 0f);
		}
		if (hitProgress >= 0f)
		{
			float pulse2 = MathF.Sin(Math.Clamp(hitProgress, 0f, 1f) * (float)Math.PI);
			Sprite.Position += _layoutBasis.X * (MathF.Sin(hitProgress * 23f) * 0.07f * pulse2);
		}
		RefreshSourceMaterial();
		SampleEnemyAttack(clock);
        ApplyAuthoredFrame(clock);
	}

	public void ConfigureSourceAppearance(int act, Camera3D layoutCamera, int enemyGroup = 1)
	{
		_sourceDisplayAct = act;
		EnsureSourceDisplayLifecycle();
		_layoutCamera = layoutCamera;
		_layoutBasis = (IsInsideTree() ? GlobalBasis : Basis).Inverse() * layoutCamera.GlobalBasis.Orthonormalized();
		Sprite.Basis = _layoutBasis;
		Sprite.Position = _layoutBasis.Y * (_imageHeight / 2f - _feetPad);
		bool phone = OS.HasFeature("android") || OS.HasFeature("ios") || Enumerable.Contains(OS.GetCmdlineUserArgs(), "--ui-phone");
		_sourceLook = ActorMaterialBinding.LoadLook(act, phone);
		_sourceDistanceScale = layoutCamera.GlobalPosition.Dot(layoutCamera.GlobalBasis.Z) / Math.Max(0.01f, ActorMaterialBinding.Number(_sourceLook["baseCameraDistance"], 16.62f));
		ref string actorKey = ref _sourceContext.ActorKey;
		string text;
		if (_actorCategory == "leaders")
		{
			text = "player";
		}
		else
		{
			text = ((_actorCategory == "dolls") ? ("doll:" + Uid) : ("enemy" + Math.Max(0, EnemyIndex)));
		}
		actorKey = text;
		_sourceContext.EnemyGroup = enemyGroup;
		_sourceContext.DollSlot = IdleSlot;
		_sourceContext.FeetPad = ((_imageHeight > 0f) ? (_feetPad / _imageHeight) : 0f);
		if (_edgeLinear < 0f)
		{
			using Image image = _baseTexture.GetImage();
			if (image.IsCompressed())
			{
				image.Decompress();
			}
			image.Convert(Image.Format.Rgba8);
			_edgeLinear = ActorMaterialSettings.EdgeDarkLinearFromRgba8(image.GetData(), image.GetWidth(), 0, 0, image.GetWidth(), image.GetHeight());
		}
		_sourceContext.EdgeLinear = _edgeLinear;
		ActorMaterialBinding.BindKeyFlipTables(_sourceContext, act);
		RefreshSourceMaterial();
	}

	public void SetSourceLights(IReadOnlyList<ActorMaterialBinding.PointLight> lights)
	{
		ActorMaterialBinding.BindLights(_surface, lights, _lightPositions, _lightColors);
	}

	public void Flash(float clock, float duration = 0.18f)
	{
		_flashUntil = clock + Math.Max(0f, duration);
		_surface.SetShaderParameter("_Flash", Math.Clamp(duration / 0.18f, 0f, 1f) * 0.1f);
	}

	public void SetDissolve(float amount)
	{
		_dissolve = Math.Clamp(amount, 0f, 1f);
		_surface.SetShaderParameter("_Dissolve", _dissolve);
	}

	private void RefreshSourceMaterial()
	{
		if (_sourceLook != null && _boundTexture != null)
		{
			_sourceContext.FrameScaleY = (float)_boundTexture.GetHeight() / (float)_baseTexture.GetHeight();
			_sourceContext.DollSlot = IdleSlot;
			if (_layoutCamera != null && GodotObject.IsInstanceValid(_layoutCamera) && _layoutCamera.IsInsideTree() && IsInsideTree())
			{
				Vector3 point = GlobalPosition + GlobalBasis * (_layoutBasis.Y * (_imageHeight * 0.5f * (1f - _sourceContext.FeetPad)));
				Vector2 screen = _layoutCamera.UnprojectPosition(point);
				Vector2 size = _layoutCamera.GetViewport().GetVisibleRect().Size;
				JObject fog = _sourceLook["fog"] as JObject;
				_sourceView.UvX = screen.X / Math.Max(1f, size.X);
				_sourceView.UvY = 1f - screen.Y / Math.Max(1f, size.Y);
				_sourceView.Aspect = size.X / Math.Max(1f, size.Y);
				_sourceView.Depth = 0f - (_layoutCamera.GlobalTransform.AffineInverse() * point).Z;
				_sourceView.FogEnabled = fog?["on"]?.Value<bool>() ?? true;
				_sourceView.FogMode = ActorMaterialSettings.FogKind.Linear;
				_sourceView.FogStart = ActorMaterialBinding.Number(fog?["start"], 22f) * _sourceDistanceScale;
				_sourceView.FogEnd = ActorMaterialBinding.Number(fog?["end"], 48f) * _sourceDistanceScale;
				_sourceView.FogColorSrgb = ActorMaterialBinding.Read3(fog?["color"], new Vector3(0.36f, 0.44f, 0.56f));
				_sourceContext.View = _sourceView;
			}
			ActorMaterialSettings.Values values = ActorMaterialSettings.Resolve(_sourceLook, ArtId, _actorCategory == "leaders", _sourceContext);
			UpdateSourceDisplayBinding();
			ActorMaterialBinding.Bind(_surface, values, _sourceLook, warmKey: false, _sourceDistanceScale, SourceDisplayTransferActive);
			int paletteProfile = !_paletteHarmonyRequested ? 0 : (Style04StaticSprite ? _style04PaletteProfile : (_actorCategory == "leaders" ? 2 : (DefinitionId == "enemy_brute" && SpiritProbeStatus == "prototype:style04-static-no-old-anatomy" ? 1 : 0)));
			_surface.SetShaderParameter("_PaletteHarmonyProfile", paletteProfile);
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		return new List<MethodInfo>(21)
		{
			new MethodInfo(MethodName.Configure, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.String, "category", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.String, "id", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.String, "displayName", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Vector3, "foot", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "height", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Bool, "phone", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.SetSourceImageHeight, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Float, "height", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.Label, new PropertyInfo(Variant.Type.Object, "", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Label3D"), exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.String, "text", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "y", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Int, "size", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.BindSurface, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, "texture", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Texture2D"), exported: false)
			}, null),
			new MethodInfo(MethodName.TryTexture, new PropertyInfo(Variant.Type.Object, "", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Texture2D"), exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.String, "p", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.PlayPose, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.String, "name", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "clock", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.StartPose, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.String, "name", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "clock", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.PoseFrameDuration, new PropertyInfo(Variant.Type.Float, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.VanDerCorput, new PropertyInfo(Variant.Type.Float, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Int, "slot", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.AdvancePose, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Float, "clock", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.ApplyPoseFrame, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.SetVitals, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Int, "hp", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Int, "maxHp", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Int, "block", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.DamageFallback, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Int, "hpLoss", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Int, "blocked", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.GainBlock, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Int, "amount", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.Heal, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Int, "amount", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.SetDead, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Bool, "dead", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.UpdateVisual, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Float, "clock", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "attackProgress", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "hitProgress", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Bool, "guarding", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.ConfigureSourceAppearance, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Int, "act", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Object, "layoutCamera", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Camera3D"), exported: false),
				new PropertyInfo(Variant.Type.Int, "enemyGroup", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.Flash, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Float, "clock", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "duration", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.SetDissolve, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Float, "amount", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.RefreshSourceMaterial, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		if (method == MethodName.Configure && args.Count == 6)
		{
			Configure(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<string>(in args[1]), VariantUtils.ConvertTo<string>(in args[2]), VariantUtils.ConvertTo<Vector3>(in args[3]), VariantUtils.ConvertTo<float>(in args[4]), VariantUtils.ConvertTo<bool>(in args[5]));
			ret = default;
			return true;
		}
		if (method == MethodName.SetSourceImageHeight && args.Count == 1)
		{
			SetSourceImageHeight(VariantUtils.ConvertTo<float>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.Label && args.Count == 3)
		{
			ret = VariantUtils.CreateFrom<Label3D>(Label(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<float>(in args[1]), VariantUtils.ConvertTo<int>(in args[2])));
			return true;
		}
		if (method == MethodName.BindSurface && args.Count == 1)
		{
			BindSurface(VariantUtils.ConvertTo<Texture2D>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.TryTexture && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<Texture2D>(TryTexture(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.PlayPose && args.Count == 2)
		{
			PlayPose(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<float>(in args[1]));
			ret = default;
			return true;
		}
		if (method == MethodName.StartPose && args.Count == 2)
		{
			StartPose(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<float>(in args[1]));
			ret = default;
			return true;
		}
		float from;
		if (method == MethodName.PoseFrameDuration && args.Count == 0)
		{
			from = PoseFrameDuration();
			ret = VariantUtils.CreateFrom(in from);
			return true;
		}
		if (method == MethodName.VanDerCorput && args.Count == 1)
		{
			from = VanDerCorput(VariantUtils.ConvertTo<int>(in args[0]));
			ret = VariantUtils.CreateFrom(in from);
			return true;
		}
		if (method == MethodName.AdvancePose && args.Count == 1)
		{
			AdvancePose(VariantUtils.ConvertTo<float>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.ApplyPoseFrame && args.Count == 0)
		{
			ApplyPoseFrame();
			ret = default;
			return true;
		}
		if (method == MethodName.SetVitals && args.Count == 3)
		{
			SetVitals(VariantUtils.ConvertTo<int>(in args[0]), VariantUtils.ConvertTo<int>(in args[1]), VariantUtils.ConvertTo<int>(in args[2]));
			ret = default;
			return true;
		}
		if (method == MethodName.DamageFallback && args.Count == 2)
		{
			DamageFallback(VariantUtils.ConvertTo<int>(in args[0]), VariantUtils.ConvertTo<int>(in args[1]));
			ret = default;
			return true;
		}
		if (method == MethodName.GainBlock && args.Count == 1)
		{
			GainBlock(VariantUtils.ConvertTo<int>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.Heal && args.Count == 1)
		{
			Heal(VariantUtils.ConvertTo<int>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.SetDead && args.Count == 1)
		{
			SetDead(VariantUtils.ConvertTo<bool>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.UpdateVisual && args.Count == 4)
		{
			UpdateVisual(VariantUtils.ConvertTo<float>(in args[0]), VariantUtils.ConvertTo<float>(in args[1]), VariantUtils.ConvertTo<float>(in args[2]), VariantUtils.ConvertTo<bool>(in args[3]));
			ret = default;
			return true;
		}
		if (method == MethodName.ConfigureSourceAppearance && args.Count == 3)
		{
			ConfigureSourceAppearance(VariantUtils.ConvertTo<int>(in args[0]), VariantUtils.ConvertTo<Camera3D>(in args[1]), VariantUtils.ConvertTo<int>(in args[2]));
			ret = default;
			return true;
		}
		if (method == MethodName.Flash && args.Count == 2)
		{
			Flash(VariantUtils.ConvertTo<float>(in args[0]), VariantUtils.ConvertTo<float>(in args[1]));
			ret = default;
			return true;
		}
		if (method == MethodName.SetDissolve && args.Count == 1)
		{
			SetDissolve(VariantUtils.ConvertTo<float>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.RefreshSourceMaterial && args.Count == 0)
		{
			RefreshSourceMaterial();
			ret = default;
			return true;
		}
		return base.InvokeGodotClassMethod(in method, args, out ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		if (method == MethodName.TryTexture && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<Texture2D>(TryTexture(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.VanDerCorput && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<float>(VanDerCorput(VariantUtils.ConvertTo<int>(in args[0])));
			return true;
		}
		ret = default;
		return false;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if (method == MethodName.Configure)
		{
			return true;
		}
		if (method == MethodName.SetSourceImageHeight)
		{
			return true;
		}
		if (method == MethodName.Label)
		{
			return true;
		}
		if (method == MethodName.BindSurface)
		{
			return true;
		}
		if (method == MethodName.TryTexture)
		{
			return true;
		}
		if (method == MethodName.PlayPose)
		{
			return true;
		}
		if (method == MethodName.StartPose)
		{
			return true;
		}
		if (method == MethodName.PoseFrameDuration)
		{
			return true;
		}
		if (method == MethodName.VanDerCorput)
		{
			return true;
		}
		if (method == MethodName.AdvancePose)
		{
			return true;
		}
		if (method == MethodName.ApplyPoseFrame)
		{
			return true;
		}
		if (method == MethodName.SetVitals)
		{
			return true;
		}
		if (method == MethodName.DamageFallback)
		{
			return true;
		}
		if (method == MethodName.GainBlock)
		{
			return true;
		}
		if (method == MethodName.Heal)
		{
			return true;
		}
		if (method == MethodName.SetDead)
		{
			return true;
		}
		if (method == MethodName.UpdateVisual)
		{
			return true;
		}
		if (method == MethodName.ConfigureSourceAppearance)
		{
			return true;
		}
		if (method == MethodName.Flash)
		{
			return true;
		}
		if (method == MethodName.SetDissolve)
		{
			return true;
		}
		if (method == MethodName.RefreshSourceMaterial)
		{
			return true;
		}
		return base.HasGodotClassMethod(in method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if (name == PropertyName.DefinitionId)
		{
			DefinitionId = VariantUtils.ConvertTo<string>(in value);
			return true;
		}
		if (name == PropertyName.ArtId)
		{
			ArtId = VariantUtils.ConvertTo<string>(in value);
			return true;
		}
		if (name == PropertyName.Phone96)
		{
			Phone96 = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName.TopMarginRows)
		{
			TopMarginRows = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName.EnemyIndex)
		{
			EnemyIndex = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName.Uid)
		{
			Uid = VariantUtils.ConvertTo<string>(in value);
			return true;
		}
		if (name == PropertyName.Home)
		{
			Home = VariantUtils.ConvertTo<Vector3>(in value);
			return true;
		}
		if (name == PropertyName.Height)
		{
			Height = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		if (name == PropertyName.Placeholder)
		{
			Placeholder = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName.Dead)
		{
			Dead = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName.Selected)
		{
			Selected = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName.Sprite)
		{
			Sprite = VariantUtils.ConvertTo<Sprite3D>(in value);
			return true;
		}
		if (name == PropertyName.Vitals)
		{
			Vitals = VariantUtils.ConvertTo<Label3D>(in value);
			return true;
		}
		if (name == PropertyName.IntentLabel)
		{
			IntentLabel = VariantUtils.ConvertTo<Label3D>(in value);
			return true;
		}
		if (name == PropertyName.IdleSlot)
		{
			IdleSlot = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName.ShowLabels)
		{
			ShowLabels = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._bottomMarginRows)
		{
			_bottomMarginRows = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName._baseTexture)
		{
			_baseTexture = VariantUtils.ConvertTo<Texture2D>(in value);
			return true;
		}
		if (name == PropertyName._footAnchorOffsetX)
		{
			_footAnchorOffsetX = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		if (name == PropertyName._pose)
		{
			_pose = VariantUtils.ConvertTo<string>(in value);
			return true;
		}
		if (name == PropertyName._poseFrame)
		{
			_poseFrame = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName._poseFrameTime)
		{
			_poseFrameTime = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		if (name == PropertyName._poseClock)
		{
			_poseClock = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		if (name == PropertyName._lastAttackProgress)
		{
			_lastAttackProgress = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		if (name == PropertyName._lastHitProgress)
		{
			_lastHitProgress = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		if (name == PropertyName._poseInitialized)
		{
			_poseInitialized = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._usesExplicitPoses)
		{
			_usesExplicitPoses = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._lastGuarding)
		{
			_lastGuarding = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._r2Idle)
		{
			_r2Idle = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName._surface)
		{
			_surface = VariantUtils.ConvertTo<ShaderMaterial>(in value);
			return true;
		}
		if (name == PropertyName._boundTexture)
		{
			_boundTexture = VariantUtils.ConvertTo<Texture2D>(in value);
			return true;
		}
		if (name == PropertyName._shadow)
		{
			_shadow = VariantUtils.ConvertTo<MeshInstance3D>(in value);
			return true;
		}
		if (name == PropertyName._hp)
		{
			_hp = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName._maxHp)
		{
			_maxHp = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName._blockValue)
		{
			_blockValue = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName._deathFade)
		{
			_deathFade = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		if (name == PropertyName._imageHeight)
		{
			_imageHeight = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		if (name == PropertyName._feetPad)
		{
			_feetPad = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		if (name == PropertyName._lastClock)
		{
			_lastClock = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		if (name == PropertyName._idleDuration)
		{
			_idleDuration = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		if (name == PropertyName._actorCategory)
		{
			_actorCategory = VariantUtils.ConvertTo<string>(in value);
			return true;
		}
		if (name == PropertyName._layoutBasis)
		{
			_layoutBasis = VariantUtils.ConvertTo<Basis>(in value);
			return true;
		}
		if (name == PropertyName._layoutCamera)
		{
			_layoutCamera = VariantUtils.ConvertTo<Camera3D>(in value);
			return true;
		}
		if (name == PropertyName._sourceDistanceScale)
		{
			_sourceDistanceScale = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		if (name == PropertyName._edgeLinear)
		{
			_edgeLinear = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		if (name == PropertyName._flashUntil)
		{
			_flashUntil = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		if (name == PropertyName._dissolve)
		{
			_dissolve = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		return base.SetGodotClassPropertyValue(in name, in value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		string from;
		if (name == PropertyName.DefinitionId)
		{
			from = DefinitionId;
			value = VariantUtils.CreateFrom(in from);
			return true;
		}
		if (name == PropertyName.ArtId)
		{
			from = ArtId;
			value = VariantUtils.CreateFrom(in from);
			return true;
		}
		bool from2;
		if (name == PropertyName.Phone96)
		{
			from2 = Phone96;
			value = VariantUtils.CreateFrom(in from2);
			return true;
		}
		int from3;
		if (name == PropertyName.BaseTextureWidth)
		{
			from3 = BaseTextureWidth;
			value = VariantUtils.CreateFrom(in from3);
			return true;
		}
		if (name == PropertyName.BaseTextureHeight)
		{
			from3 = BaseTextureHeight;
			value = VariantUtils.CreateFrom(in from3);
			return true;
		}
		if (name == PropertyName.TopMarginRows)
		{
			from3 = TopMarginRows;
			value = VariantUtils.CreateFrom(in from3);
			return true;
		}
		float from4;
		if (name == PropertyName.BaseCanvasWorldHeight)
		{
			from4 = BaseCanvasWorldHeight;
			value = VariantUtils.CreateFrom(in from4);
			return true;
		}
		if (name == PropertyName.EnemyIndex)
		{
			from3 = EnemyIndex;
			value = VariantUtils.CreateFrom(in from3);
			return true;
		}
		if (name == PropertyName.Uid)
		{
			from = Uid;
			value = VariantUtils.CreateFrom(in from);
			return true;
		}
		Vector3 from5;
		if (name == PropertyName.Home)
		{
			from5 = Home;
			value = VariantUtils.CreateFrom(in from5);
			return true;
		}
		if (name == PropertyName.Height)
		{
			from4 = Height;
			value = VariantUtils.CreateFrom(in from4);
			return true;
		}
		if (name == PropertyName.DisplayedHp)
		{
			from3 = DisplayedHp;
			value = VariantUtils.CreateFrom(in from3);
			return true;
		}
		if (name == PropertyName.DisplayedBlock)
		{
			from3 = DisplayedBlock;
			value = VariantUtils.CreateFrom(in from3);
			return true;
		}
		if (name == PropertyName.Placeholder)
		{
			from2 = Placeholder;
			value = VariantUtils.CreateFrom(in from2);
			return true;
		}
		if (name == PropertyName.Dead)
		{
			from2 = Dead;
			value = VariantUtils.CreateFrom(in from2);
			return true;
		}
		if (name == PropertyName.Selected)
		{
			from2 = Selected;
			value = VariantUtils.CreateFrom(in from2);
			return true;
		}
		if (name == PropertyName.Sprite)
		{
			value = VariantUtils.CreateFrom<Sprite3D>(Sprite);
			return true;
		}
		Label3D from6;
		if (name == PropertyName.Vitals)
		{
			from6 = Vitals;
			value = VariantUtils.CreateFrom(in from6);
			return true;
		}
		if (name == PropertyName.IntentLabel)
		{
			from6 = IntentLabel;
			value = VariantUtils.CreateFrom(in from6);
			return true;
		}
		if (name == PropertyName.IdleSlot)
		{
			from3 = IdleSlot;
			value = VariantUtils.CreateFrom(in from3);
			return true;
		}
		if (name == PropertyName.ShowLabels)
		{
			from2 = ShowLabels;
			value = VariantUtils.CreateFrom(in from2);
			return true;
		}
		if (name == PropertyName.Contact)
		{
			from5 = Contact;
			value = VariantUtils.CreateFrom(in from5);
			return true;
		}
		if (name == PropertyName.Impact)
		{
			from5 = Impact;
			value = VariantUtils.CreateFrom(in from5);
			return true;
		}
		if (name == PropertyName.DissolveAmount)
		{
			from4 = DissolveAmount;
			value = VariantUtils.CreateFrom(in from4);
			return true;
		}
		if (name == PropertyName._bottomMarginRows)
		{
			value = VariantUtils.CreateFrom(in _bottomMarginRows);
			return true;
		}
		if (name == PropertyName._baseTexture)
		{
			value = VariantUtils.CreateFrom(in _baseTexture);
			return true;
		}
		if (name == PropertyName._footAnchorOffsetX)
		{
			value = VariantUtils.CreateFrom(in _footAnchorOffsetX);
			return true;
		}
		if (name == PropertyName._pose)
		{
			value = VariantUtils.CreateFrom(in _pose);
			return true;
		}
		if (name == PropertyName._poseFrame)
		{
			value = VariantUtils.CreateFrom(in _poseFrame);
			return true;
		}
		if (name == PropertyName._poseFrameTime)
		{
			value = VariantUtils.CreateFrom(in _poseFrameTime);
			return true;
		}
		if (name == PropertyName._poseClock)
		{
			value = VariantUtils.CreateFrom(in _poseClock);
			return true;
		}
		if (name == PropertyName._lastAttackProgress)
		{
			value = VariantUtils.CreateFrom(in _lastAttackProgress);
			return true;
		}
		if (name == PropertyName._lastHitProgress)
		{
			value = VariantUtils.CreateFrom(in _lastHitProgress);
			return true;
		}
		if (name == PropertyName._poseInitialized)
		{
			value = VariantUtils.CreateFrom(in _poseInitialized);
			return true;
		}
		if (name == PropertyName._usesExplicitPoses)
		{
			value = VariantUtils.CreateFrom(in _usesExplicitPoses);
			return true;
		}
		if (name == PropertyName._lastGuarding)
		{
			value = VariantUtils.CreateFrom(in _lastGuarding);
			return true;
		}
		if (name == PropertyName._r2Idle)
		{
			value = VariantUtils.CreateFrom(in _r2Idle);
			return true;
		}
		if (name == PropertyName._surface)
		{
			value = VariantUtils.CreateFrom(in _surface);
			return true;
		}
		if (name == PropertyName._boundTexture)
		{
			value = VariantUtils.CreateFrom(in _boundTexture);
			return true;
		}
		if (name == PropertyName._shadow)
		{
			value = VariantUtils.CreateFrom(in _shadow);
			return true;
		}
		if (name == PropertyName._hp)
		{
			value = VariantUtils.CreateFrom(in _hp);
			return true;
		}
		if (name == PropertyName._maxHp)
		{
			value = VariantUtils.CreateFrom(in _maxHp);
			return true;
		}
		if (name == PropertyName._blockValue)
		{
			value = VariantUtils.CreateFrom(in _blockValue);
			return true;
		}
		if (name == PropertyName._deathFade)
		{
			value = VariantUtils.CreateFrom(in _deathFade);
			return true;
		}
		if (name == PropertyName._imageHeight)
		{
			value = VariantUtils.CreateFrom(in _imageHeight);
			return true;
		}
		if (name == PropertyName._feetPad)
		{
			value = VariantUtils.CreateFrom(in _feetPad);
			return true;
		}
		if (name == PropertyName._lastClock)
		{
			value = VariantUtils.CreateFrom(in _lastClock);
			return true;
		}
		if (name == PropertyName._idleDuration)
		{
			value = VariantUtils.CreateFrom(in _idleDuration);
			return true;
		}
		if (name == PropertyName._actorCategory)
		{
			value = VariantUtils.CreateFrom(in _actorCategory);
			return true;
		}
		if (name == PropertyName._layoutBasis)
		{
			value = VariantUtils.CreateFrom(in _layoutBasis);
			return true;
		}
		if (name == PropertyName._layoutCamera)
		{
			value = VariantUtils.CreateFrom(in _layoutCamera);
			return true;
		}
		if (name == PropertyName._lightPositions)
		{
			value = VariantUtils.CreateFrom(in _lightPositions);
			return true;
		}
		if (name == PropertyName._lightColors)
		{
			value = VariantUtils.CreateFrom(in _lightColors);
			return true;
		}
		if (name == PropertyName._sourceDistanceScale)
		{
			value = VariantUtils.CreateFrom(in _sourceDistanceScale);
			return true;
		}
		if (name == PropertyName._edgeLinear)
		{
			value = VariantUtils.CreateFrom(in _edgeLinear);
			return true;
		}
		if (name == PropertyName._flashUntil)
		{
			value = VariantUtils.CreateFrom(in _flashUntil);
			return true;
		}
		if (name == PropertyName._dissolve)
		{
			value = VariantUtils.CreateFrom(in _dissolve);
			return true;
		}
		return base.GetGodotClassPropertyValue(in name, out value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		return new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, PropertyName.DefinitionId, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.String, PropertyName.ArtId, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName.Phone96, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.BaseTextureWidth, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.BaseTextureHeight, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.TopMarginRows, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName.BaseCanvasWorldHeight, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName._bottomMarginRows, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.EnemyIndex, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.String, PropertyName.Uid, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Vector3, PropertyName.Home, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName.Height, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.DisplayedHp, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.DisplayedBlock, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName.Placeholder, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName.Dead, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName.Selected, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName.Sprite, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName.Vitals, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName.IntentLabel, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._baseTexture, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName._footAnchorOffsetX, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.String, PropertyName._pose, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName._poseFrame, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName._poseFrameTime, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName._poseClock, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName._lastAttackProgress, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName._lastHitProgress, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName._poseInitialized, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName._usesExplicitPoses, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName._lastGuarding, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName._r2Idle, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.IdleSlot, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._surface, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._boundTexture, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._shadow, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName._hp, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName._maxHp, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName._blockValue, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName._deathFade, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName._imageHeight, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName._feetPad, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName._lastClock, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName._idleDuration, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName.ShowLabels, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Vector3, PropertyName.Contact, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Vector3, PropertyName.Impact, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.String, PropertyName._actorCategory, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Basis, PropertyName._layoutBasis, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._layoutCamera, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.PackedVector4Array, PropertyName._lightPositions, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.PackedVector4Array, PropertyName._lightColors, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName._sourceDistanceScale, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName._edgeLinear, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName._flashUntil, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName._dissolve, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName.DissolveAmount, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		base.SaveGodotObjectData(info);
		info.AddProperty(PropertyName.DefinitionId, Variant.From<string>(DefinitionId));
		info.AddProperty(PropertyName.ArtId, Variant.From<string>(ArtId));
		info.AddProperty(PropertyName.Phone96, Variant.From<bool>(Phone96));
		info.AddProperty(PropertyName.TopMarginRows, Variant.From<int>(TopMarginRows));
		info.AddProperty(PropertyName.EnemyIndex, Variant.From<int>(EnemyIndex));
		info.AddProperty(PropertyName.Uid, Variant.From<string>(Uid));
		info.AddProperty(PropertyName.Home, Variant.From<Vector3>(Home));
		info.AddProperty(PropertyName.Height, Variant.From<float>(Height));
		info.AddProperty(PropertyName.Placeholder, Variant.From<bool>(Placeholder));
		info.AddProperty(PropertyName.Dead, Variant.From<bool>(Dead));
		info.AddProperty(PropertyName.Selected, Variant.From<bool>(Selected));
		info.AddProperty(PropertyName.Sprite, Variant.From<Sprite3D>(Sprite));
		info.AddProperty(PropertyName.Vitals, Variant.From<Label3D>(Vitals));
		info.AddProperty(PropertyName.IntentLabel, Variant.From<Label3D>(IntentLabel));
		info.AddProperty(PropertyName.IdleSlot, Variant.From<int>(IdleSlot));
		info.AddProperty(PropertyName.ShowLabels, Variant.From<bool>(ShowLabels));
		info.AddProperty(PropertyName._bottomMarginRows, Variant.From(in _bottomMarginRows));
		info.AddProperty(PropertyName._baseTexture, Variant.From(in _baseTexture));
		info.AddProperty(PropertyName._footAnchorOffsetX, Variant.From(in _footAnchorOffsetX));
		info.AddProperty(PropertyName._pose, Variant.From(in _pose));
		info.AddProperty(PropertyName._poseFrame, Variant.From(in _poseFrame));
		info.AddProperty(PropertyName._poseFrameTime, Variant.From(in _poseFrameTime));
		info.AddProperty(PropertyName._poseClock, Variant.From(in _poseClock));
		info.AddProperty(PropertyName._lastAttackProgress, Variant.From(in _lastAttackProgress));
		info.AddProperty(PropertyName._lastHitProgress, Variant.From(in _lastHitProgress));
		info.AddProperty(PropertyName._poseInitialized, Variant.From(in _poseInitialized));
		info.AddProperty(PropertyName._usesExplicitPoses, Variant.From(in _usesExplicitPoses));
		info.AddProperty(PropertyName._lastGuarding, Variant.From(in _lastGuarding));
		info.AddProperty(PropertyName._r2Idle, Variant.From(in _r2Idle));
		info.AddProperty(PropertyName._surface, Variant.From(in _surface));
		info.AddProperty(PropertyName._boundTexture, Variant.From(in _boundTexture));
		info.AddProperty(PropertyName._shadow, Variant.From(in _shadow));
		info.AddProperty(PropertyName._hp, Variant.From(in _hp));
		info.AddProperty(PropertyName._maxHp, Variant.From(in _maxHp));
		info.AddProperty(PropertyName._blockValue, Variant.From(in _blockValue));
		info.AddProperty(PropertyName._deathFade, Variant.From(in _deathFade));
		info.AddProperty(PropertyName._imageHeight, Variant.From(in _imageHeight));
		info.AddProperty(PropertyName._feetPad, Variant.From(in _feetPad));
		info.AddProperty(PropertyName._lastClock, Variant.From(in _lastClock));
		info.AddProperty(PropertyName._idleDuration, Variant.From(in _idleDuration));
		info.AddProperty(PropertyName._actorCategory, Variant.From(in _actorCategory));
		info.AddProperty(PropertyName._layoutBasis, Variant.From(in _layoutBasis));
		info.AddProperty(PropertyName._layoutCamera, Variant.From(in _layoutCamera));
		info.AddProperty(PropertyName._sourceDistanceScale, Variant.From(in _sourceDistanceScale));
		info.AddProperty(PropertyName._edgeLinear, Variant.From(in _edgeLinear));
		info.AddProperty(PropertyName._flashUntil, Variant.From(in _flashUntil));
		info.AddProperty(PropertyName._dissolve, Variant.From(in _dissolve));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		base.RestoreGodotObjectData(info);
		if (info.TryGetProperty(PropertyName.DefinitionId, out var _value_DefinitionId))
		{
			DefinitionId = _value_DefinitionId.As<string>();
		}
		if (info.TryGetProperty(PropertyName.ArtId, out var _value_ArtId))
		{
			ArtId = _value_ArtId.As<string>();
		}
		if (info.TryGetProperty(PropertyName.Phone96, out var _value_Phone96))
		{
			Phone96 = _value_Phone96.As<bool>();
		}
		if (info.TryGetProperty(PropertyName.TopMarginRows, out var _value_TopMarginRows))
		{
			TopMarginRows = _value_TopMarginRows.As<int>();
		}
		if (info.TryGetProperty(PropertyName.EnemyIndex, out var _value_EnemyIndex))
		{
			EnemyIndex = _value_EnemyIndex.As<int>();
		}
		if (info.TryGetProperty(PropertyName.Uid, out var _value_Uid))
		{
			Uid = _value_Uid.As<string>();
		}
		if (info.TryGetProperty(PropertyName.Home, out var _value_Home))
		{
			Home = _value_Home.As<Vector3>();
		}
		if (info.TryGetProperty(PropertyName.Height, out var _value_Height))
		{
			Height = _value_Height.As<float>();
		}
		if (info.TryGetProperty(PropertyName.Placeholder, out var _value_Placeholder))
		{
			Placeholder = _value_Placeholder.As<bool>();
		}
		if (info.TryGetProperty(PropertyName.Dead, out var _value_Dead))
		{
			Dead = _value_Dead.As<bool>();
		}
		if (info.TryGetProperty(PropertyName.Selected, out var _value_Selected))
		{
			Selected = _value_Selected.As<bool>();
		}
		if (info.TryGetProperty(PropertyName.Sprite, out var _value_Sprite))
		{
			Sprite = _value_Sprite.As<Sprite3D>();
		}
		if (info.TryGetProperty(PropertyName.Vitals, out var _value_Vitals))
		{
			Vitals = _value_Vitals.As<Label3D>();
		}
		if (info.TryGetProperty(PropertyName.IntentLabel, out var _value_IntentLabel))
		{
			IntentLabel = _value_IntentLabel.As<Label3D>();
		}
		if (info.TryGetProperty(PropertyName.IdleSlot, out var _value_IdleSlot))
		{
			IdleSlot = _value_IdleSlot.As<int>();
		}
		if (info.TryGetProperty(PropertyName.ShowLabels, out var _value_ShowLabels))
		{
			ShowLabels = _value_ShowLabels.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._bottomMarginRows, out var _value__bottomMarginRows))
		{
			_bottomMarginRows = _value__bottomMarginRows.As<int>();
		}
		if (info.TryGetProperty(PropertyName._baseTexture, out var _value__baseTexture))
		{
			_baseTexture = _value__baseTexture.As<Texture2D>();
		}
		if (info.TryGetProperty(PropertyName._footAnchorOffsetX, out var _value__footAnchorOffsetX))
		{
			_footAnchorOffsetX = _value__footAnchorOffsetX.As<float>();
		}
		if (info.TryGetProperty(PropertyName._pose, out var _value__pose))
		{
			_pose = _value__pose.As<string>();
		}
		if (info.TryGetProperty(PropertyName._poseFrame, out var _value__poseFrame))
		{
			_poseFrame = _value__poseFrame.As<int>();
		}
		if (info.TryGetProperty(PropertyName._poseFrameTime, out var _value__poseFrameTime))
		{
			_poseFrameTime = _value__poseFrameTime.As<float>();
		}
		if (info.TryGetProperty(PropertyName._poseClock, out var _value__poseClock))
		{
			_poseClock = _value__poseClock.As<float>();
		}
		if (info.TryGetProperty(PropertyName._lastAttackProgress, out var _value__lastAttackProgress))
		{
			_lastAttackProgress = _value__lastAttackProgress.As<float>();
		}
		if (info.TryGetProperty(PropertyName._lastHitProgress, out var _value__lastHitProgress))
		{
			_lastHitProgress = _value__lastHitProgress.As<float>();
		}
		if (info.TryGetProperty(PropertyName._poseInitialized, out var _value__poseInitialized))
		{
			_poseInitialized = _value__poseInitialized.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._usesExplicitPoses, out var _value__usesExplicitPoses))
		{
			_usesExplicitPoses = _value__usesExplicitPoses.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._lastGuarding, out var _value__lastGuarding))
		{
			_lastGuarding = _value__lastGuarding.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._r2Idle, out var _value__r2Idle))
		{
			_r2Idle = _value__r2Idle.As<bool>();
		}
		if (info.TryGetProperty(PropertyName._surface, out var _value__surface))
		{
			_surface = _value__surface.As<ShaderMaterial>();
		}
		if (info.TryGetProperty(PropertyName._boundTexture, out var _value__boundTexture))
		{
			_boundTexture = _value__boundTexture.As<Texture2D>();
		}
		if (info.TryGetProperty(PropertyName._shadow, out var _value__shadow))
		{
			_shadow = _value__shadow.As<MeshInstance3D>();
		}
		if (info.TryGetProperty(PropertyName._hp, out var _value__hp))
		{
			_hp = _value__hp.As<int>();
		}
		if (info.TryGetProperty(PropertyName._maxHp, out var _value__maxHp))
		{
			_maxHp = _value__maxHp.As<int>();
		}
		if (info.TryGetProperty(PropertyName._blockValue, out var _value__blockValue))
		{
			_blockValue = _value__blockValue.As<int>();
		}
		if (info.TryGetProperty(PropertyName._deathFade, out var _value__deathFade))
		{
			_deathFade = _value__deathFade.As<float>();
		}
		if (info.TryGetProperty(PropertyName._imageHeight, out var _value__imageHeight))
		{
			_imageHeight = _value__imageHeight.As<float>();
		}
		if (info.TryGetProperty(PropertyName._feetPad, out var _value__feetPad))
		{
			_feetPad = _value__feetPad.As<float>();
		}
		if (info.TryGetProperty(PropertyName._lastClock, out var _value__lastClock))
		{
			_lastClock = _value__lastClock.As<float>();
		}
		if (info.TryGetProperty(PropertyName._idleDuration, out var _value__idleDuration))
		{
			_idleDuration = _value__idleDuration.As<float>();
		}
		if (info.TryGetProperty(PropertyName._actorCategory, out var _value__actorCategory))
		{
			_actorCategory = _value__actorCategory.As<string>();
		}
		if (info.TryGetProperty(PropertyName._layoutBasis, out var _value__layoutBasis))
		{
			_layoutBasis = _value__layoutBasis.As<Basis>();
		}
		if (info.TryGetProperty(PropertyName._layoutCamera, out var _value__layoutCamera))
		{
			_layoutCamera = _value__layoutCamera.As<Camera3D>();
		}
		if (info.TryGetProperty(PropertyName._sourceDistanceScale, out var _value__sourceDistanceScale))
		{
			_sourceDistanceScale = _value__sourceDistanceScale.As<float>();
		}
		if (info.TryGetProperty(PropertyName._edgeLinear, out var _value__edgeLinear))
		{
			_edgeLinear = _value__edgeLinear.As<float>();
		}
		if (info.TryGetProperty(PropertyName._flashUntil, out var _value__flashUntil))
		{
			_flashUntil = _value__flashUntil.As<float>();
		}
		if (info.TryGetProperty(PropertyName._dissolve, out var _value__dissolve))
		{
			_dissolve = _value__dissolve.As<float>();
		}
	}

	public void SetSourceDisplayTransferEnabled(bool enabled)
	{
		_sourceDisplayRequested = enabled && !Enumerable.Contains(OS.GetCmdlineUserArgs(), "--actor-color-baseline");
		RefreshSourceDisplayTransfer();
	}

	public void SetOtherActSourceDisplayEnabled(bool enabled)
	{
		_otherActSourceDisplayRequested = enabled && !Enumerable.Contains(OS.GetCmdlineUserArgs(), "--actor-color-other-acts-baseline");
		RefreshSourceDisplayTransfer();
	}

	public void RefreshSourceDisplayTransfer()
	{
		RefreshSourceMaterial();
	}

	private void EnsureSourceDisplayLifecycle()
	{
		if (!_sourceDisplayLifecycleBound)
		{
			TreeExiting += RetireSourceDisplay;
			_sourceDisplayLifecycleBound = true;
		}
	}

	private void RetireSourceDisplay()
	{
        DisposeAuthoredFrames();
		_style04Motion?.Cancel();
		_style04Support?.Cancel();
		SourceDisplayTransferActive = false;
		SourceDisplayTransferStatus = "legacy:actor-exiting";
		if (_surface != null && GodotObject.IsInstanceValid(_surface))
		{
			_surface.SetShaderParameter("_SourceDisplayTransfer", false);
			_surface.SetShaderParameter("_SourceDisplayLut", default);
		}
		_sourceDisplayLut?.Dispose();
		_sourceDisplayLut = null;
		_sourceDisplayLutKey = 0;
	}

	private void UpdateSourceDisplayBinding()
	{
		if (_surface == null || !GodotObject.IsInstanceValid(_surface))
		{
			return;
		}
		string reason;
		if (!_sourceDisplayRequested)
		{
			reason = "legacy:baseline-requested";
		}
		else
		{
			int sourceDisplayAct = _sourceDisplayAct;
			bool flag = ((sourceDisplayAct < 1 || sourceDisplayAct > 3) ? true : false);
			if (flag || (_sourceDisplayAct != 1 && !_otherActSourceDisplayRequested))
			{
				reason = "legacy:unverified-act";
			}
			else
			{
				reason = (SourceActorDisplayTransfer.MatchesProfile(_sourceDisplayAct, _sourceLook?["post"] as JObject) ? SourceActorDisplayTransfer.EvaluateEnvironment(_layoutCamera) : "legacy:source-profile-mismatch");
			}
		}
		bool enabled = reason == "act1-source-ldr32-gl-v1";
		if (enabled)
		{
			EnsureSourceDisplayLifecycle();
			int key = SourceActorDisplayTransfer.LutKeyForAct(_sourceDisplayAct);
			if (_sourceDisplayLut == null || _sourceDisplayLutKey != key)
			{
				_surface.SetShaderParameter("_SourceDisplayLut", default);
				_sourceDisplayLut?.Dispose();
				_sourceDisplayLut = SourceActorDisplayTransfer.CreateLut(_sourceDisplayAct);
				_sourceDisplayLutKey = key;
			}
			_surface.SetShaderParameter("_SourceDisplayLut", _sourceDisplayLut);
			SourceActorDisplayTransfer.BindProfile(_surface, _sourceDisplayAct);
			reason = SourceActorDisplayTransfer.GetProfileId(_sourceDisplayAct);
		}
		else if (_sourceDisplayLut != null)
		{
			_surface.SetShaderParameter("_SourceDisplayLut", default);
			_sourceDisplayLut.Dispose();
			_sourceDisplayLut = null;
			_sourceDisplayLutKey = 0;
		}
		SourceDisplayTransferActive = enabled;
		SourceDisplayTransferStatus = reason;
		_surface.SetShaderParameter("_SourceDisplayTransfer", enabled);
	}

	private void PrepareEnemyAttackPresentation()
	{
		ulong started = Time.GetTicksUsec();
		EnsureEnemyAttackRig();
		EnemyAttackPrepareMilliseconds = (double)(Time.GetTicksUsec() - started) / 1000.0;
	}

	private bool EnsureEnemyAttackRig()
	{
		if (_enemyAttackConfigured)
		{
			return _enemyAttackMotion != null;
		}
		_enemyAttackConfigured = true;
		if (!_spiritProbeEnabled || _actorCategory != "enemies" || _spiritProbeStill || Enumerable.Contains(OS.GetCmdlineUserArgs(), "--enemy-attack-original"))
		{
			return false;
		}
		if (!EnemyAttackRigProfile.TryGet(DefinitionId, Sprite.Texture.ResourcePath, Sprite.Texture.GetWidth(), Sprite.Texture.GetHeight(), out EnemyAttackRigProfile profile) || profile == null)
		{
			return false;
		}
		if (!ResourceLoader.Exists("res://shaders/rpg_spirit_attack.gdshader"))
		{
			EnemyAttackStatus = "original:attack-shader-missing";
			return false;
		}
		Shader shader = GD.Load<Shader>("res://shaders/rpg_spirit_attack.gdshader");
		if (shader == null)
		{
			return false;
		}
		EnemyAttackRigProfile.LayerArt layers = profile.Layers;
		if ((object)layers != null)
		{
			_enemyAttackPartMask = TryTexture(layers.Mask);
			_enemyAttackUnderpaint = TryTexture(layers.Underpaint);
			if (!Valid(_enemyAttackPartMask) || !Valid(_enemyAttackUnderpaint))
			{
				EnemyAttackStatus = "original:layer-registration-failed";
				return false;
			}
		}
		_enemyAttackRig = profile;
		_enemyAttackMotion = new EnemyAttackArticulation(profile);
		List<StringName> names = new List<StringName>();
		foreach (Variant shaderUniform in _surface.Shader.GetShaderUniformList())
		{
			if (shaderUniform.AsGodotDictionary().TryGetValue("name", out var value))
			{
				names.Add(value.AsStringName());
			}
		}
		_enemyAttackSourceUniforms = names.ToArray();
		EnemyAttackSharedMaterial = !Enumerable.Contains(OS.GetCmdlineUserArgs(), "--enemy-attack-copy-material");
		if (EnemyAttackSharedMaterial)
		{
			Variant[] values = new Variant[_enemyAttackSourceUniforms.Length];
			for (int i = 0; i < values.Length; i++)
			{
				values[i] = _surface.GetShaderParameter(_enemyAttackSourceUniforms[i]);
			}
			_surface.Shader = shader;
			for (int j = 0; j < values.Length; j++)
			{
				_surface.SetShaderParameter(_enemyAttackSourceUniforms[j], values[j]);
			}
			_enemyAttackMaterial = _surface;
		}
		else
		{
			_enemyAttackMaterial = new ShaderMaterial
			{
				Shader = shader
			};
		}
		_enemyAttackMesh = new MeshInstance3D
		{
			Name = "AnatomicalAttack",
			Mesh = BuildEnemyAttackMesh(profile),
			MaterialOverride = _enemyAttackMaterial,
			Visible = false,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			CustomAabb = new Aabb(new Vector3(-0.85f, -0.85f, -0.05f), new Vector3(1.7f, 1.7f, 0.1f))
		};
		AddChild(_enemyAttackMesh, forceReadableName: false, InternalMode.Disabled);
		EnemyAttackRigProfile.LayerArt layered = profile.Layers;
		if ((object)layered != null)
		{
			_enemyAttackShadowHome = _shadow.Position;
			_enemyAttackMaterial.SetShaderParameter(EnemyAttackLayerMode, 1);
			_enemyAttackMaterial.SetShaderParameter("_EnemyAttackPartMask", _enemyAttackPartMask);
			_enemyAttackMaterial.SetShaderParameter("_EnemyAttackUnderpaint", _enemyAttackUnderpaint);
			_enemyAttackParts = new MeshInstance3D[layered.Parts.Length];
			_enemyAttackPartMaterials = new ShaderMaterial[layered.Parts.Length];
			for (int k = 0; k < layered.Parts.Length; k++)
			{
				EnemyAttackRigProfile.LayerPart part = layered.Parts[k];
				_enemyAttackFixedBodyJoints[part.JointIndex] = true;
				ShaderMaterial material = new ShaderMaterial
				{
					Shader = shader
				};
				material.SetShaderParameter(EnemyAttackLayerMode, 2 + part.Channel);
				material.SetShaderParameter("_EnemyAttackPartMask", _enemyAttackPartMask);
				MeshInstance3D node = new MeshInstance3D
				{
					Name = "AttackPart_" + part.Name,
					Mesh = BuildRigidAttackPart(part.JointIndex),
					MaterialOverride = material,
					Visible = false,
					CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
					CustomAabb = new Aabb(new Vector3(-0.95f, -0.95f, -0.05f), new Vector3(1.9f, 1.9f, 0.1f))
				};
				_enemyAttackPartMaterials[k] = material;
				_enemyAttackParts[k] = node;
				AddChild(node, forceReadableName: false, InternalMode.Disabled);
			}
		}
		if (EnemyAttackSharedMaterial)
		{
			_surface.SetShaderParameter(EnemyAttackLayerMode, 0);
		}
		EnemyAttackStatus = "ready:anatomical-forward-skin";
		return true;
		bool Valid(Texture2D? t)
		{
			if (t != null && t.GetWidth() == Sprite.Texture.GetWidth())
			{
				return t.GetHeight() == Sprite.Texture.GetHeight();
			}
			return false;
		}
	}

	private ArrayMesh BuildEnemyAttackMesh(EnemyAttackRigProfile profile)
	{
		int cells = profile.Subdivisions;
		int side = cells + 1;
		int count = side * side;
		if (EnemyAttackGeometryCache.TryGetValue(profile, out AttackGeometry cached))
		{
			EnemyAttackGeometryCacheHit = true;
			EnemyAttackVertexCount = cached.Vertices;
			EnemyAttackTriangleCount = cached.Triangles;
			return cached.Mesh;
		}
		Vector3[] vertices = new Vector3[count];
		Vector2[] uv = new Vector2[count];
		Vector2[] uv2 = new Vector2[count];
		Color[] colors = new Color[count];
		int[] indices = new int[cells * cells * 6];
		Span<float> weights = stackalloc float[6];
		for (int y = 0; y <= cells; y++)
		{
			for (int x = 0; x <= cells; x++)
			{
				int index = y * side + x;
				Vector2 p = new Vector2((float)x / (float)cells, (float)y / (float)cells);
				vertices[index] = new Vector3(p.X - 0.5f, 0.5f - p.Y, 0f);
				uv[index] = p;
				weights.Clear();
				float remaining = 1f;
				for (int j = 0; j < profile.Joints.Length; j++)
				{
					weights[j] = profile.Weight(j, p) * remaining;
					remaining -= weights[j];
				}
				colors[index] = new Color(weights[0], weights[1], weights[2], weights[3]);
				uv2[index] = new Vector2(weights[4], weights[5]);
			}
		}
		bool[] occupied = EnemyAttackOccupiedCells(cells);
		bool[] used = new bool[count];
		int cursor = 0;
		for (int i = 0; i < cells; i++)
		{
			for (int k = 0; k < cells; k++)
			{
				if (occupied[i * cells + k])
				{
					int a = i * side + k;
					int b = a + 1;
					int c = a + side;
					int d = c + 1;
					bool flag;
					used[c] = (flag = (used[d] = true));
					used[b] = (flag = flag);
					used[a] = flag;
					indices[cursor++] = a;
					indices[cursor++] = c;
					indices[cursor++] = b;
					indices[cursor++] = b;
					indices[cursor++] = c;
					indices[cursor++] = d;
				}
			}
		}
		int kept = 0;
		int[] remap = new int[count];
		for (int l = 0; l < count; l++)
		{
			if (used[l])
			{
				remap[l] = kept++;
			}
		}
		Vector3[] compactVertices = new Vector3[kept];
		Vector2[] compactUv = new Vector2[kept];
		Vector2[] compactUv2 = new Vector2[kept];
		Color[] compactColors = new Color[kept];
		for (int m = 0; m < count; m++)
		{
			if (used[m])
			{
				int k2 = remap[m];
				compactVertices[k2] = vertices[m];
				compactUv[k2] = uv[m];
				compactUv2[k2] = uv2[m];
				compactColors[k2] = colors[m];
			}
		}
		int[] compactIndices = new int[cursor];
		for (int n = 0; n < cursor; n++)
		{
			compactIndices[n] = remap[indices[n]];
		}
		Godot.Collections.Array arrays = new Godot.Collections.Array();
		arrays.Resize(13);
		arrays[0] = compactVertices;
		arrays[4] = compactUv;
		arrays[5] = compactUv2;
		arrays[3] = compactColors;
		arrays[12] = compactIndices;
		ArrayMesh mesh = new ArrayMesh();
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays, null, null, (Mesh.ArrayFormat)0L);
		EnemyAttackVertexCount = kept;
		EnemyAttackTriangleCount = cursor / 3;
		EnemyAttackGeometryCache.Add(profile, new AttackGeometry(mesh, kept, cursor / 3));
		return mesh;
	}

	private bool[] EnemyAttackOccupiedCells(int cells)
	{
		using Image image = Sprite.Texture.GetImage();
		if (image.IsCompressed())
		{
			image.Decompress();
		}
		image.Convert(Image.Format.Rgba8);
		int width = image.GetWidth();
		int height = image.GetHeight();
		int stride = width + 1;
		byte[] pixels = image.GetData();
		int[] sums = new int[(width + 1) * (height + 1)];
		for (int y = 0; y < height; y++)
		{
			for (int x = 0; x < width; x++)
			{
				sums[(y + 1) * stride + x + 1] = ((pixels[(y * width + x) * 4 + 3] > 0) ? 1 : 0) + sums[y * stride + x + 1] + sums[(y + 1) * stride + x] - sums[y * stride + x];
			}
		}
		int pad = (int)MathF.Ceiling(3f * (float)width / 64f) + 2;
		bool[] occupied = new bool[cells * cells];
		for (int i = 0; i < cells; i++)
		{
			for (int j = 0; j < cells; j++)
			{
				int left = Math.Max(0, j * width / cells - pad);
				int top = Math.Max(0, i * height / cells - pad);
				int right = Math.Min(width, (int)MathF.Ceiling(((float)j + 1f) * (float)width / (float)cells) + pad);
				int bottom = Math.Min(height, (int)MathF.Ceiling(((float)i + 1f) * (float)height / (float)cells) + pad);
				occupied[i * cells + j] = sums[bottom * stride + right] - sums[top * stride + right] - sums[bottom * stride + left] + sums[top * stride + left] > 0;
			}
		}
		return occupied;
	}

	private static ArrayMesh BuildRigidAttackPart(int joint)
	{
		ArrayMesh cached = EnemyAttackRigidGeometryCache[joint];
		if (cached != null)
		{
			return cached;
		}
		Vector3[] vertices = new Vector3[4]
		{
			new Vector3(-0.5f, 0.5f, 0f),
			new Vector3(0.5f, 0.5f, 0f),
			new Vector3(-0.5f, -0.5f, 0f),
			new Vector3(0.5f, -0.5f, 0f)
		};
		Vector2[] uv = new Vector2[4]
		{
			new Vector2(0f, 0f),
			new Vector2(1f, 0f),
			new Vector2(0f, 1f),
			new Vector2(1f, 1f)
		};
		Color color = new Color((joint == 0) ? 1 : 0, (joint == 1) ? 1 : 0, (joint == 2) ? 1 : 0, (joint == 3) ? 1 : 0);
		Vector2 second = new Vector2((joint == 4) ? 1 : 0, (joint == 5) ? 1 : 0);
		Godot.Collections.Array arrays = new Godot.Collections.Array();
		arrays.Resize(13);
		arrays[0] = vertices;
		arrays[4] = uv;
		arrays[3] = new Color[4] { color, color, color, color };
		arrays[5] = new Vector2[4] { second, second, second, second };
		arrays[12] = new int[6] { 0, 2, 1, 1, 2, 3 };
		ArrayMesh mesh = new ArrayMesh();
		mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays, null, null, (Mesh.ArrayFormat)0L);
		EnemyAttackRigidGeometryCache[joint] = mesh;
		return mesh;
	}

	public void BeginEnemyAttack(float now, float contactAt, int executionIndex, string? moveId, bool ranged = false, MimicAuthoredCountProof? mimicProof = null)
	{
        if(MimicAuthoredCountProof.Supports(DefinitionId)&&(mimicProof==null||mimicProof.EnemyIndex!=EnemyIndex||mimicProof.ProfileId!=DefinitionId||mimicProof.MoveId!=moveId||mimicProof.ExecutionEventIndex!=executionIndex||ranged||!float.IsFinite(now)||now<0||!float.IsFinite(contactAt))){_contactAuthoredFrames?.Cancel();_style04Motion?.Cancel();ApplyAuthoredFrame(now);return;}
		if (_style04Motion != null)
		{
			if (!Dead) {
                _style04Motion.Begin(now, contactAt, executionIndex, moveId, ranged, ExecutionVersion);
                if (_style04Motion.State.Active && _style04Motion.State.ExecutionIndex == executionIndex) { _bruteAuthoredFrames?.BeginAttack(now, _style04Motion.State); _wolfAuthoredFrames?.BeginAttack(now,_style04Motion.State); _crossbowAuthoredFrames?.BeginAttack(now,_style04Motion.State); _contactAuthoredFrames?.BeginAttack(now,_style04Motion.State,mimicProof); _drummerAuthoredFrames?.BeginAttack(now,_style04Motion.State); _hexAuthoredFrames?.BeginAttack(); }
                ApplyAuthoredFrame(now);
            }
			return;
		}
		if (!Dead && EnsureEnemyAttackRig())
		{
			_enemyAttackMotion.Begin(now, contactAt, executionIndex, moveId, ranged);
		}
	}

	public void PreviewEnemyAttack(float now, float contactAt, int executionIndex, string? moveId, bool ranged = false)
	{
		if (_style04Motion != null)
		{
			if (!Dead && _style04Motion.Preview(now, contactAt, executionIndex, moveId, ranged, ExecutionVersion)) { if (_crossbowAuthoredFrames != null) { _crossbowAuthoredFrames.AcceptedRelease(_style04Motion.State); ApplyAuthoredFrame(now); } }
			return;
		}
		if (!Dead && EnsureEnemyAttackRig())
		{
			_enemyAttackMotion.Preview(now, contactAt, executionIndex, moveId, ranged);
		}
	}

	public void ContactEnemyAttack(float now, int executionIndex, string? moveId, int ordinal, int count)
	{
		if (_style04Motion != null)
		{
			if (!Dead && (!MimicAuthoredCountProof.Supports(DefinitionId)||(_contactAuthoredFrames?.CanAcceptMimicContact(_style04Motion.State,now,ExecutionVersion,ordinal,count)??false)) && _style04Motion.Contact(now, executionIndex, moveId, ordinal, count, ExecutionVersion)) { _bruteAuthoredFrames?.AcceptedContact(_style04Motion.State,count); _wolfAuthoredFrames?.AcceptedContact(_style04Motion.State,count); _contactAuthoredFrames?.AcceptedContact(_style04Motion.State,count); ApplyAuthoredFrame(now); }
			return;
		}
		if (!Dead && EnsureEnemyAttackRig())
		{
			_enemyAttackMotion.Contact(now, executionIndex, moveId, ordinal, count);
		}
	}

	public void CancelEnemyAttack()
	{
        _bruteAuthoredFrames?.Cancel(); _wolfAuthoredFrames?.Cancel(); _crossbowAuthoredFrames?.Cancel(); _contactAuthoredFrames?.Cancel(); _drummerAuthoredFrames?.Cancel(); _hexAuthoredFrames?.Cancel();
		if (_style04Motion != null)
		{
			_style04Motion.Cancel();
			if (Sprite != null && !AuthoredFramesEnabled) ApplyStyle04Motion(_style04Motion.State);
		}
		_enemyAttackDeathFrozen = false;
		_enemyAttackMotion?.Cancel();
		MeshInstance3D[] enemyAttackParts = _enemyAttackParts;
		for (int i = 0; i < enemyAttackParts.Length; i++)
		{
			enemyAttackParts[i].Visible = false;
		}
		if (_enemyAttackMesh != null)
		{
			_enemyAttackMesh.Visible = false;
		}
		if (Sprite != null)
		{
			Sprite.Visible = true;
		}
		System.Array.Clear(_enemyAttackJointAngles);
		System.Array.Clear(_enemyAttackJointTranslations);
		EnemyAttackVisualRootOffset = Vector3.Zero;
		if (EnemyAttackLayered)
		{
			_shadow.Position = _enemyAttackShadowHome;
		}
		RestoreSharedAttackMaterial();
        ApplyAuthoredFrame(_lastClock);
	}

 // A real Fled terminal keeps drawing during the existing Stage dissolve. It owns no queue or game state.
 internal bool BeginAuthoredFleeTerminal(Style04EnemySupportMotion.Token token,float now,float duration)=>!Dead&&token.OwnerVersion==ExecutionVersion&&(_contactAuthoredFrames?.AcceptedFled(token,Style04SupportSample,now,duration)??false);
 internal BruteAuthoredFrames.ReactionToken CaptureBruteReactionOwnership()=>_bruteAuthoredFrames?.CaptureReactionToken()??default;
 internal bool TryPlayOwnedBruteReaction(string name,float clock,BruteAuthoredFrames.ReactionToken owner){if(Dead||_bruteAuthoredFrames==null||!_bruteAuthoredFrames.NotifyPose(name,clock,ExecutionVersion,owner))return false;_usesExplicitPoses=true;StartPose(name,clock);ApplyAuthoredFrame(clock);return true;}
 internal WolfAuthoredFrames.ReactionToken CaptureWolfReactionOwnership()=>_wolfAuthoredFrames?.CaptureReactionToken()??default;
 internal bool TryPlayOwnedWolfReaction(string name,float clock,WolfAuthoredFrames.ReactionToken owner){if(Dead||_wolfAuthoredFrames==null||!_wolfAuthoredFrames.NotifyPose(name,clock,ExecutionVersion,owner))return false;_usesExplicitPoses=true;StartPose(name,clock);ApplyAuthoredFrame(clock);return true;}
 internal CrossbowAuthoredFrames.ReactionToken CaptureCrossbowReactionOwnership()=>_crossbowAuthoredFrames?.CaptureReactionToken()??default;
 internal bool TryPlayOwnedCrossbowReaction(string name,float clock,CrossbowAuthoredFrames.ReactionToken owner){if(Dead||_crossbowAuthoredFrames==null||!_crossbowAuthoredFrames.NotifyPose(name,clock,ExecutionVersion,owner))return false;_usesExplicitPoses=true;StartPose(name,clock);ApplyAuthoredFrame(clock);return true;}
 // Delayed authored reactions carry the owner token captured when scheduled.
 // Ordinary Stage PlayPose calls remain protected by Stage callbackGeneration.
 internal ContactAuthoredFrames.ReactionToken CaptureAuthoredReactionOwnership()=>_contactAuthoredFrames?.CaptureReactionToken()??default;
 internal bool TryPlayOwnedAuthoredReaction(string name,float clock,ContactAuthoredFrames.ReactionToken owner)
 {
  if(Dead||_contactAuthoredFrames==null||!_contactAuthoredFrames.NotifyPose(name,clock,ExecutionVersion,owner))return false;
  _usesExplicitPoses=true;StartPose(name,clock);ApplyAuthoredFrame(clock);return true;
 }
 internal void ResumeAuthoredIdleForPlacement(float now)
 {
  if(!AuthoredFramesEnabled||Dead||Style04MotionSample.Active||Style04SupportSample.Active||Motion.MotionActive||UiLunge.Active)return;
  _bruteAuthoredFrames?.ResumeIdleForPlacement(now,ExecutionVersion);_wolfAuthoredFrames?.ResumeIdleForPlacement(now,ExecutionVersion);_crossbowAuthoredFrames?.ResumeIdleForPlacement(now,ExecutionVersion);_contactAuthoredFrames?.ResumeIdleForPlacement(now,ExecutionVersion);_drummerAuthoredFrames?.ResumeIdleForPlacement();_hexAuthoredFrames?.Clear();ApplyAuthoredFrame(now);
 }
 private void InitializeAuthoredFrames()
 {
  bool brute=DefinitionId=="enemy_brute"&&Enumerable.Contains(OS.GetCmdlineUserArgs(),"--enemy-brute-authored-frames");
  bool wolf=DefinitionId=="enemy_wolf"&&Enumerable.Contains(OS.GetCmdlineUserArgs(),"--enemy-wolf-authored-frames");
  bool crossbow=(DefinitionId=="enemy_crossbow_archer"&&Enumerable.Contains(OS.GetCmdlineUserArgs(),"--enemy-crossbow-authored-frames"))||((DefinitionId=="enemy_spore_cap"||DefinitionId=="enemy_hexer")&&Enumerable.Contains(OS.GetCmdlineUserArgs(),"--enemy-fungi-tree-caster-authored-frames"));
  bool contact=(DefinitionId=="enemy_rock_beetle"&&Enumerable.Contains(OS.GetCmdlineUserArgs(),"--enemy-rock-beetle-authored-frames"))||(DefinitionId=="enemy_elite_deathless"&&Enumerable.Contains(OS.GetCmdlineUserArgs(),"--enemy-deathless-knight-authored-frames"))||((DefinitionId=="enemy_probe"||DefinitionId=="enemy_turtle"||DefinitionId=="enemy_drummer")&&Enumerable.Contains(OS.GetCmdlineUserArgs(),"--enemy-trio-authored-frames"))||((DefinitionId=="enemy_vine_walker"||DefinitionId=="enemy_spore_cap"||DefinitionId=="enemy_hexer")&&Enumerable.Contains(OS.GetCmdlineUserArgs(),"--enemy-fungi-tree-caster-authored-frames"))||((DefinitionId=="enemy_thorn_squirrel"||DefinitionId=="enemy_snap_fruit")&&Enumerable.Contains(OS.GetCmdlineUserArgs(),"--enemy-small-pair-authored-frames"))||(DefinitionId=="enemy_set_wary"&&Enumerable.Contains(OS.GetCmdlineUserArgs(),"--enemy-wary-authored-frames"))||(DefinitionId=="enemy_thief"&&Enumerable.Contains(OS.GetCmdlineUserArgs(),"--enemy-thief-authored-frames"))||(DefinitionId=="enemy_apprentice_colossus"&&Enumerable.Contains(OS.GetCmdlineUserArgs(),"--enemy-colossus-authored-frames"))||(DefinitionId=="enemy_cultist"&&Enumerable.Contains(OS.GetCmdlineUserArgs(),"--enemy-cultist-authored-frames"))||(DefinitionId=="enemy_moss_slime"&&Enumerable.Contains(OS.GetCmdlineUserArgs(),"--enemy-moss-slime-authored-frames"))||(DefinitionId=="enemy_elite_gold_raven"&&Enumerable.Contains(OS.GetCmdlineUserArgs(),"--enemy-gold-raven-authored-frames"))||(DefinitionId=="enemy_elite_owl"&&Enumerable.Contains(OS.GetCmdlineUserArgs(),"--enemy-owl-authored-frames"))||(DefinitionId=="enemy_wide_power"&&Enumerable.Contains(OS.GetCmdlineUserArgs(),"--enemy-wide-beast-authored-frames"))||(DefinitionId=="enemy_big_slime"&&Enumerable.Contains(OS.GetCmdlineUserArgs(),"--enemy-big-slime-authored-frames"))||(DefinitionId=="enemy_shield_squire"&&Enumerable.Contains(OS.GetCmdlineUserArgs(),"--enemy-shield-squire-authored-frames"))||(DefinitionId=="enemy_bond_wolf"&&Enumerable.Contains(OS.GetCmdlineUserArgs(),"--enemy-bond-wolf-authored-frames"))||(DefinitionId=="enemy_elite_sergeant"&&Enumerable.Contains(OS.GetCmdlineUserArgs(),"--enemy-sergeant-authored-frames"))||(DefinitionId=="enemy_mourn_beast"&&Enumerable.Contains(OS.GetCmdlineUserArgs(),"--enemy-mourn-beast-authored-frames"))||(DefinitionId=="enemy_devoted_sculptor"&&Enumerable.Contains(OS.GetCmdlineUserArgs(),"--enemy-sculptor-authored-frames"))||(DefinitionId=="enemy_brood_raptor"&&Enumerable.Contains(OS.GetCmdlineUserArgs(),"--enemy-brood-raptor-authored-frames"))||(DefinitionId=="enemy_bowl_bug"&&Enumerable.Contains(OS.GetCmdlineUserArgs(),"--enemy-bowl-bug-authored-frames"))||(DefinitionId=="enemy_brood_toad"&&Enumerable.Contains(OS.GetCmdlineUserArgs(),"--enemy-brood-toad-authored-frames"))||(DefinitionId=="enemy_broodling"&&Enumerable.Contains(OS.GetCmdlineUserArgs(),"--enemy-broodling-authored-frames"))||(DefinitionId=="enemy_raptor_chick"&&Enumerable.Contains(OS.GetCmdlineUserArgs(),"--enemy-raptor-chick-authored-frames"))||(DefinitionId=="enemy_axe_automaton_reboot"&&Enumerable.Contains(OS.GetCmdlineUserArgs(),"--enemy-axe-reboot-authored-frames"))||(DefinitionId=="enemy_shell_guard"&&Enumerable.Contains(OS.GetCmdlineUserArgs(),"--enemy-shell-guard-authored-frames"))||(DefinitionId=="enemy_moss"&&Enumerable.Contains(OS.GetCmdlineUserArgs(),"--enemy-moss-authored-frames")); /* Explicit registered real-assets-only profiles */
  bool drummer=DefinitionId=="enemy_drummer"&&Enumerable.Contains(OS.GetCmdlineUserArgs(),"--enemy-trio-authored-frames");
  if(UsesDefaultAuthoredFrames(DefinitionId)){
   brute=DefinitionId=="enemy_brute";
   wolf=DefinitionId=="enemy_wolf";
   crossbow=DefinitionId is "enemy_winch_warden" or "enemy_seal_orb" or "enemy_kin_priest" or "enemy_crab_cannon" or "enemy_kin_follower" or "enemy_thunder_globe" or "enemy_elite_sentry" or "enemy_bomber" or "enemy_cinder_imp" or "enemy_slug" or "enemy_iron_clam" or "enemy_mud_lump" or "enemy_sludge_spider" or "enemy_crossbow_archer" or "enemy_spore_cap" or "enemy_hexer" or "enemy_lost_soul";
   contact=!brute&&!wolf&&DefinitionId!="enemy_crossbow_archer";
   drummer=DefinitionId=="enemy_drummer";
  }
  if(!Style04StaticSprite||(!brute&&!wolf&&!crossbow&&!contact&&!drummer))return;
  try{
   _authoredClipSet=EnemyAuthoredClipSet.Load(DefinitionId,_baseTexture);_bruteAuthoredTextures=_authoredClipSet.Textures;
   if(brute){_bruteAuthoredFrames=new BruteAuthoredFrames(_authoredClipSet.Durations);_bruteAuthoredFrames.ResumeIdleForPlacement(_lastClock,ExecutionVersion);}
   if(wolf){_wolfAuthoredFrames=new WolfAuthoredFrames(_authoredClipSet.Durations);_wolfAuthoredFrames.ResumeIdleForPlacement(_lastClock,ExecutionVersion);}
   if(crossbow){_crossbowAuthoredFrames=new CrossbowAuthoredFrames(_authoredClipSet.Durations,DefinitionId);if(DefinitionId is "enemy_winch_warden" or "enemy_seal_orb" or "enemy_kin_priest" or "enemy_crab_cannon" or "enemy_kin_follower" or "enemy_thunder_globe" or "enemy_crossbow_archer" or "enemy_elite_sentry")_crossbowAuthoredFrames.ResumeIdleForPlacement(_lastClock,ExecutionVersion);}
   if(contact){_contactAuthoredFrames=new ContactAuthoredFrames(DefinitionId,_authoredClipSet.Durations);if(DefinitionId is "enemy_ember_furnace" or "enemy_chimera_3" or "enemy_winch_warden" or "enemy_chimera_1" or "enemy_chimera_2" or "enemy_seal_orb" or "enemy_warden" or "enemy_set_breaker" or "enemy_kin_priest" or "enemy_moss_spawner" or "enemy_crab_cannon" or "enemy_kin_follower" or "enemy_crab_crusher" or "enemy_moss_healer" or "enemy_thunder_globe" or "enemy_raptor_egg" or "enemy_sludge_berserker" or "enemy_elite_mirror_djinn" or "enemy_elite_devourer" or "enemy_elite_iron_egg" or "enemy_elite_sentry" or "enemy_mimic_imp" or "enemy_mimic_jester" or "enemy_frog_knight" or "enemy_elite_slaver" or "enemy_gaping_maw" or "enemy_joker" or "enemy_maw_hunter" or "enemy_cog_construct" or "enemy_bomber" or "enemy_chomper" or "enemy_cinder_imp" or "enemy_scald_gnat" or "enemy_slug" or "enemy_haze_stag" or "enemy_axe_automaton" or "enemy_biting_scroll" or "enemy_elite_giant_face" or "enemy_elite_doom_chanter" or "enemy_whetstone_colossus" or "enemy_iron_clam" or "enemy_mud_lump" or "enemy_sludge_spider" or "enemy_strangler_serpent" or "enemy_elite_stab_book" or "enemy_mudling" or "enemy_lost_soul" or "enemy_elite_husk_3" or "enemy_elite_husk_2" or "enemy_elite_husk_1" or "enemy_axe_ogre" or "enemy_forgotten_soul" or "enemy_burrow_worm" or "enemy_nemesis_wraith" or "enemy_rock_beetle" or "enemy_elite_deathless" or "enemy_shell_guard" or "enemy_moss" or "enemy_raptor_chick" or "enemy_axe_automaton_reboot" or "enemy_brood_toad" or "enemy_broodling" or "enemy_brood_raptor" or "enemy_bowl_bug" or "enemy_mourn_beast" or "enemy_devoted_sculptor" or "enemy_bond_wolf" or "enemy_elite_sergeant" or "enemy_big_slime" or "enemy_shield_squire" or "enemy_wide_power" or "enemy_elite_owl" or "enemy_elite_gold_raven" or "enemy_moss_slime" or "enemy_cultist" or "enemy_apprentice_colossus" or "enemy_hexer" or "enemy_spore_cap" or "enemy_drummer" or "enemy_probe" or "enemy_turtle" or "enemy_vine_walker" or "enemy_thorn_squirrel" or "enemy_snap_fruit" or "enemy_set_wary" or "enemy_thief")_contactAuthoredFrames.ResumeIdleForPlacement(_lastClock,ExecutionVersion);}
   if(drummer)_drummerAuthoredFrames=new DrummerAuthoredFrames(_authoredClipSet.Durations);
   if((DefinitionId=="enemy_chimera_3"||DefinitionId=="enemy_sludge_berserker"||DefinitionId=="enemy_elite_mirror_djinn"||DefinitionId=="enemy_elite_slaver"||DefinitionId=="enemy_gaping_maw"||DefinitionId=="enemy_maw_hunter"||DefinitionId=="enemy_chomper"||DefinitionId=="enemy_cinder_imp"||DefinitionId=="enemy_scald_gnat"||DefinitionId=="enemy_spore_cap"||DefinitionId=="enemy_hexer"||DefinitionId=="enemy_mudling")&&contact)_hexAuthoredFrames=new HexAuthoredFrames(_authoredClipSet.Durations,DefinitionId);
   var canvas=_authoredClipSet.Canvas;
   SetMeta("authored_frame_canvas",new Vector2I(canvas.FrameWidth,canvas.FrameHeight));SetMeta("authored_frame_anchor",new Vector2I(canvas.AnchorX,canvas.AnchorY));SetMeta("authored_frame_incomplete",true);
   ApplyAuthoredFrame(_lastClock);
  }catch{DisposeAuthoredFrames();throw;}
 }
 private void ApplyAuthoredFrame(float now)
 {
  if(_authoredClipSet==null||Sprite==null)return;
  var frame=_drummerAuthoredFrames!=null?_drummerAuthoredFrames.Sample(now,Style04MotionSample,Style04SupportSample,ExecutionVersion,Dead,_contactAuthoredFrames!.Sample(now,Style04MotionSample,Style04SupportSample,ExecutionVersion,Dead)):_contactAuthoredFrames!=null?_contactAuthoredFrames.Sample(now,Style04MotionSample,Style04SupportSample,ExecutionVersion,Dead):_crossbowAuthoredFrames!=null?_crossbowAuthoredFrames.Sample(now,Style04MotionSample,Style04SupportSample,ExecutionVersion,Dead):_wolfAuthoredFrames!=null?_wolfAuthoredFrames.Sample(now,Style04MotionSample,Style04SupportSample,ExecutionVersion,Dead):_bruteAuthoredFrames!.Sample(now,Style04MotionSample,Style04SupportSample,ExecutionVersion,Dead);
  if(frame.Reason=="release-overlay"&&_crossbowAuthoredFrames!=null)frame=_crossbowAuthoredFrames.Sample(now,Style04MotionSample,Style04SupportSample,ExecutionVersion,Dead);
  if(_hexAuthoredFrames!=null){
   frame=_hexAuthoredFrames.Sample(now,Style04MotionSample,Style04SupportSample,ExecutionVersion,Dead,frame);_sporeFrame=frame;
  }
  if(DefinitionId is "enemy_winch_warden" or "enemy_seal_orb" or "enemy_kin_priest" or "enemy_crab_cannon" or "enemy_kin_follower" or "enemy_thunder_globe" or "enemy_elite_sentry" or "enemy_bomber" or "enemy_slug" or "enemy_iron_clam" or "enemy_lost_soul" or "enemy_mud_lump" or "enemy_sludge_spider")_sporeFrame=frame;
  var texture=_authoredClipSet.Textures[frame.Clip][frame.FrameIndex];var canvas=_authoredClipSet.Canvas;
  // Original source layout, pixel size and basis stay fixed; only explicit canvas registration applies.
  Sprite.Basis=_layoutBasis;Sprite.Position=_layoutBasis.Y*(_imageHeight/2f-_feetPad);Sprite.Offset=new Vector2(canvas.SpriteOffsetX,canvas.SpriteOffsetY);
  Sprite.Texture=texture;BindSurface(texture);
 }
 internal bool TryAuthoredOpaqueBounds(bool motionEnvelope,out Rect2I bounds)
 {
  bounds=default;return _authoredClipSet!=null&&Sprite?.Texture!=null&&_authoredClipSet.TryBounds(Sprite.Texture,motionEnvelope,out bounds);
 }
 private void DisposeAuthoredFrames()
 {
  _bruteAuthoredFrames?.Cancel();_wolfAuthoredFrames?.Cancel(); _crossbowAuthoredFrames?.Cancel(); _contactAuthoredFrames?.Cancel(); _drummerAuthoredFrames?.Cancel(); _hexAuthoredFrames?.Cancel();_bruteAuthoredFrames=null;_wolfAuthoredFrames=null;_crossbowAuthoredFrames=null;_contactAuthoredFrames=null;_drummerAuthoredFrames=null;_hexAuthoredFrames=null;_sporeFrame=BruteAuthoredFrames.Snapshot.Disabled;
  if(_authoredClipSet==null)return;
  if(Sprite!=null&&GodotObject.IsInstanceValid(Sprite)&&_baseTexture!=null){Sprite.Texture=_baseTexture;Sprite.Offset=new Vector2(_footAnchorOffsetX,0);if(_surface!=null&&GodotObject.IsInstanceValid(_surface))_surface.SetShaderParameter("_BaseMap",_baseTexture);_boundTexture=_baseTexture;}
  _authoredClipSet.Dispose();_authoredClipSet=null;_bruteAuthoredTextures.Clear();
 }

	private void ApplyStyle04Motion(Style04EnemyMotion.Snapshot pose)
	{
		// Rotate and scale both the canvas and its center about the registered feet.
		// Home, logical hit points, shadow and SourceUiLunge stay authoritative.
		Basis local = new Basis(Vector3.Back, Mathf.DegToRad(pose.AngleDegrees));
		Sprite.Basis = (_layoutBasis * local).ScaledLocal(new Vector3(ExecutionScale.X * pose.ScaleX, ExecutionScale.Y * pose.ScaleY, 1f));
		Sprite.Position = Sprite.Basis.Y * (_imageHeight / 2f - _feetPad);
	}

	public Style04EnemySupportMotion.Token BeginStyle04Support(string kind, string? moveId, int executionIndex, float now)
	{
		if (!Style04StaticSprite || Dead || !Style04EnemySupportMotion.Supports(DefinitionId, kind, moveId)) return default;
		_style04Support ??= new Style04EnemySupportMotion(DefinitionId);
		var token = _style04Support.Begin(kind, moveId, executionIndex, ExecutionVersion, now,
			now + Style04EnemySupportMotion.MaxLifetimeSeconds);
		if (token.IsValid) { _executionDuration = 0f; ExecutionScale = Vector2.One; _bruteAuthoredFrames?.BeginSupport(now,_style04Support.State); _wolfAuthoredFrames?.BeginSupport(now,_style04Support.State); _crossbowAuthoredFrames?.BeginSupport(now,_style04Support.State); _contactAuthoredFrames?.BeginSupport(now,_style04Support.State); _drummerAuthoredFrames?.BeginSupport(_style04Support.State); _hexAuthoredFrames?.BeginSupport(_style04Support.State); ApplyAuthoredFrame(now); }
		return token;
	}

 internal bool ReserveHealOutcome(Style04EnemySupportMotion.Token token,MossHealAppliedOncePlan plan,int generation)=>!Dead&&HealAuthoredFramesEnabled&&plan.EnemyIndex==EnemyIndex&&token.OwnerVersion==ExecutionVersion&&(_contactAuthoredFrames?.ReserveHeal(token,Style04SupportSample,plan,generation)??false);
 internal bool ApplyHealOutcomeOnce(Style04EnemySupportMotion.Token token,DeckRogue.Engine.Generated.GameEvent e,int eventIndex,int generation,float now){if(Dead||!HealAuthoredFramesEnabled||token.OwnerVersion!=ExecutionVersion||!(_contactAuthoredFrames?.AcceptHealOutcome(token,Style04SupportSample,e,eventIndex,generation,now)??false))return false;ApplyAuthoredFrame(now);return true;}
 internal bool ReserveMillApplied(Style04EnemySupportMotion.Token token,DevourerMillAppliedOncePlan plan,int generation)=>!Dead&&MillAuthoredFramesEnabled&&plan.EnemyIndex==EnemyIndex&&token.OwnerVersion==ExecutionVersion&&(_contactAuthoredFrames?.ReserveMill(token,Style04SupportSample,plan,generation)??false);
 internal bool ApplyMillOnce(Style04EnemySupportMotion.Token token,DeckRogue.Engine.Generated.GameEvent e,int eventIndex,int generation,float now){if(Dead||!MillAuthoredFramesEnabled||token.OwnerVersion!=ExecutionVersion||!(_contactAuthoredFrames?.AcceptMill(token,Style04SupportSample,e,eventIndex,generation,now)??false))return false;ApplyAuthoredFrame(now);return true;}
 internal bool ReserveHexOutcome(Style04EnemySupportMotion.Token token,HexAppliedOncePlan plan,int generation)=>!Dead&&plan.EnemyIndex==EnemyIndex&&token.OwnerVersion==ExecutionVersion&&(_contactAuthoredFrames?.OwnsSupportToken(token,Style04SupportSample)??false)&&(_hexAuthoredFrames?.Reserve(token,Style04SupportSample,plan,generation)??false);
 internal bool ApplyHexOutcome(Style04EnemySupportMotion.Token token,DeckRogue.Engine.Generated.GameEvent e,int eventIndex,int generation,float now){if(Dead||token.OwnerVersion!=ExecutionVersion||!(_contactAuthoredFrames?.OwnsSupportToken(token,Style04SupportSample)??false)||!(_hexAuthoredFrames?.Accept(token,Style04SupportSample,e,eventIndex,generation,now)??false))return false;ApplyAuthoredFrame(now);return true;}
 internal bool ReserveDrummerApplied(Style04EnemySupportMotion.Token token,DrummerAppliedOncePlan plan,int generation)=>!Dead&&plan.EnemyIndex==EnemyIndex&&token.OwnerVersion==ExecutionVersion&&(_contactAuthoredFrames?.OwnsSupportToken(token,Style04SupportSample)??false)&&(_drummerAuthoredFrames?.ReserveApplied(token,Style04SupportSample,plan,generation)??false);
 internal bool ApplyDrummerOnce(Style04EnemySupportMotion.Token token,DeckRogue.Engine.Generated.GameEvent e,int eventIndex,int generation,float now){if(Dead||token.OwnerVersion!=ExecutionVersion||!(_contactAuthoredFrames?.OwnsSupportToken(token,Style04SupportSample)??false)||!(_drummerAuthoredFrames?.AcceptedApplied(token,Style04SupportSample,e,eventIndex,generation,now)??false))return false;ApplyAuthoredFrame(now);return true;}

	public bool PulseStyle04Support(Style04EnemySupportMotion.Token token, int eventIndex, float now, bool actualOwnBlockGained=false, bool actualOwnStrengthGained=false, bool actualOwnSummon=false, bool actualOwnTokenDestroyed=false, bool actualOwnCardSealed=false)
    {
        if(Dead||(HealAuthoredFramesEnabled&&Style04SupportSample.Kind=="heal")||MillAuthoredFramesEnabled||((DefinitionId is "enemy_winch_warden" or "enemy_axe_ogre" or "enemy_crab_cannon")&&Style04SupportSample.Kind=="rest")||(_contactAuthoredFrames?.UsesMoveTypedSupportPulse==true?!_contactAuthoredFrames.AcceptsMoveTypedSupportPulse(token,Style04SupportSample,actualOwnBlockGained,actualOwnStrengthGained,actualOwnSummon,actualOwnTokenDestroyed,actualOwnCardSealed):(_contactAuthoredFrames?.RequiresActualOwnBlockPulse==true&&(!actualOwnBlockGained||Style04SupportSample.AcceptedPulses!=0)))||(_bruteAuthoredFrames!=null&&(!_bruteAuthoredFrames.OwnsSupportToken(token,Style04SupportSample)||Style04SupportSample.AcceptedPulses!=0||(Style04SupportSample.Kind=="defend"?!actualOwnBlockGained:!actualOwnStrengthGained)))||(_hexAuthoredFrames!=null&&Style04SupportSample.Kind=="hex")||(_drummerAuthoredFrames!=null&&Style04SupportSample.Kind=="rally")||token.OwnerVersion!=ExecutionVersion||(_wolfAuthoredFrames!=null&&!_wolfAuthoredFrames.OwnsSupportToken(token,Style04SupportSample))||(_contactAuthoredFrames?.UsesStrictSupportOwnership==true&&!_contactAuthoredFrames.OwnsSupportToken(token,Style04SupportSample))||!(_style04Support?.Pulse(token,eventIndex,now)??false))return false;
        if(_bruteAuthoredFrames!=null||_wolfAuthoredFrames!=null||_contactAuthoredFrames?.UsesStrictSupportOwnership==true)ApplyAuthoredFrame(now);
        return true;
    }

	public bool EndStyle04Support(Style04EnemySupportMotion.Token token, float now, bool resolved = false)
    {
        if (Dead || token.OwnerVersion != ExecutionVersion || (_bruteAuthoredFrames!=null&&!_bruteAuthoredFrames.OwnsSupportToken(token,Style04SupportSample)) || (_wolfAuthoredFrames!=null&&!_wolfAuthoredFrames.OwnsSupportToken(token,Style04SupportSample))||(_contactAuthoredFrames?.UsesStrictSupportOwnership==true&&!_contactAuthoredFrames.OwnsSupportToken(token,Style04SupportSample)) || !(_style04Support?.End(token, now) ?? false)) return false;
        if(_bruteAuthoredFrames!=null||_wolfAuthoredFrames!=null||_contactAuthoredFrames?.UsesStrictSupportOwnership==true)ApplyAuthoredFrame(now);
        // Only Stage's validated real EnemyActionResolved branch passes this marker.
        if(resolved&&(_contactAuthoredFrames?.AcceptedResolved(token,_style04Support!.State)??false))ApplyAuthoredFrame(now);
        if(resolved&&(_contactAuthoredFrames?.AcceptedRestResolved(token,_style04Support!.State)??false))ApplyAuthoredFrame(now);
        if (_drummerAuthoredFrames != null) { if (resolved) _drummerAuthoredFrames.AcceptedResolved(token, _style04Support!.State); ApplyAuthoredFrame(now); }
        return true;
    }

	public void CancelStyle04Support(Style04EnemySupportMotion.Token token)
	{
		var state = _style04Support?.State;
		if (state.HasValue && state.Value.InstanceId == token.InstanceId && state.Value.Version == token.Serial &&
			state.Value.ExecutionIndex == token.ExecutionIndex && state.Value.OwnerVersion == token.OwnerVersion)
        {
            bool bruteCancelled=_bruteAuthoredFrames?.CancelSupport(token,state.Value)??false;
            bool wolfCancelled=_wolfAuthoredFrames?.CancelSupport(token,state.Value)??false;
            bool turtleCancelled=_contactAuthoredFrames?.CancelSupport(token,state.Value)??false;
            if(turtleCancelled){_drummerAuthoredFrames?.Cancel(); _hexAuthoredFrames?.Cancel();}
            _style04Support!.Cancel();
            if(bruteCancelled||wolfCancelled||turtleCancelled)ApplyAuthoredFrame(_lastClock);
        }
	}

	private void ApplyStyle04Support(Style04EnemySupportMotion.Snapshot pose)
	{
		Basis local = new Basis(Vector3.Back, Mathf.DegToRad(pose.AngleDegrees));
		Sprite.Basis = (_layoutBasis * local).ScaledLocal(new Vector3(pose.ScaleX, pose.ScaleY, 1f));
		Sprite.Position = Sprite.Basis.Y * (_imageHeight / 2f - _feetPad);
	}

	private void RestoreSharedAttackMaterial()
	{
		if (EnemyAttackSharedMaterial && _enemyAttackSharedPoseWritten)
		{
			for (int i = 0; i < 6; i++)
			{
				_surface.SetShaderParameter(EnemyAttackRowsX[i], new Vector3(1f, 0f, 0f));
				_surface.SetShaderParameter(EnemyAttackRowsY[i], new Vector3(0f, 1f, 0f));
			}
			_surface.SetShaderParameter(EnemyAttackLayerMode, 0);
			_enemyAttackSharedPoseWritten = false;
		}
	}

	private void OnEnemyAttackDeath()
	{
		_style04Support?.Cancel();
		if (!_enemyAttackDeathFrozen)
		{
			if (EnemyAttackLayered && EnemyAttackActive)
			{
				_enemyAttackDeathFrozen = true;
				_enemyAttackMotion.FreezeForDeath();
			}
			else
			{
				CancelEnemyAttack();
			}
		}
	}

	private void SampleEnemyAttack(float now)
	{
		if (_enemyAttackMotion == null || _enemyAttackMesh == null || _enemyAttackMaterial == null || _enemyAttackRig == null)
		{
			return;
		}
		if (Dead && !_enemyAttackDeathFrozen)
		{
			CancelEnemyAttack();
			return;
		}
		if (!_enemyAttackDeathFrozen)
		{
			_enemyAttackMotion.Sample(now);
		}
		bool active = _enemyAttackMotion.Active;
		bool layered = (object)_enemyAttackRig.Layers != null;
		bool visible = active || _enemyAttackDeathFrozen || (layered && _enemyAttackRig.Layers.StableRest);
		_enemyAttackMesh.Visible = visible;
		Sprite.Visible = !visible;
		MeshInstance3D[] enemyAttackParts = _enemyAttackParts;
		for (int i = 0; i < enemyAttackParts.Length; i++)
		{
			enemyAttackParts[i].Visible = visible;
		}
		if (!visible)
		{
			System.Array.Clear(_enemyAttackJointAngles);
			System.Array.Clear(_enemyAttackJointTranslations);
			EnemyAttackVisualRootOffset = Vector3.Zero;
			if (layered)
			{
				_shadow.Position = _enemyAttackShadowHome;
			}
			RestoreSharedAttackMaterial();
			EnemyAttackStatus = "idle:original-sprite";
			return;
		}
		BattleActor battleActor = this;
		string enemyAttackStatus;
		if (_enemyAttackDeathFrozen)
		{
			enemyAttackStatus = "death:layered-frozen";
		}
		else
		{
			enemyAttackStatus = (layered ? "active:source-preserving-layers" : "active:anatomical-forward-skin");
		}
		battleActor.EnemyAttackStatus = enemyAttackStatus;
		float width = (float)Sprite.Texture.GetWidth() * Sprite.PixelSize;
		float height = (float)Sprite.Texture.GetHeight() * Sprite.PixelSize;
		Basis basis = Sprite.Basis;
		Vector3 origin = Sprite.Position + Sprite.Basis.X * Sprite.Offset.X * Sprite.PixelSize + Sprite.Basis.Y * Sprite.Offset.Y * Sprite.PixelSize;
		EnemyAttackVisualRootOffset = Vector3.Zero;
		if (layered && (active || _enemyAttackDeathFrozen))
		{
			basis = _layoutBasis;
			origin = _layoutBasis.Y * (_imageHeight / 2f - _feetPad);
			EnemyAttackVisualRootOffset = Basis.Inverse() * UiLunge.Offset * (_enemyAttackRig.Layers.LungeScale - 1f);
			origin += EnemyAttackVisualRootOffset;
		}
		if (layered)
		{
			_shadow.Position = _enemyAttackShadowHome + EnemyAttackVisualRootOffset;
		}
		_enemyAttackMesh.Transform = new Transform3D(basis.ScaledLocal(new Vector3(width, height, 1f)), origin);
		if (EnemyAttackSharedMaterial)
		{
			if (!_enemyAttackSharedPoseWritten)
			{
				_surface.SetShaderParameter(EnemyAttackLayerMode, layered ? 1 : 0);
			}
			_enemyAttackSharedPoseWritten = true;
		}
		else
		{
			CopyEnemyAttackSourceMaterial(_enemyAttackMaterial);
		}
		for (int j = 0; j < _enemyAttackParts.Length; j++)
		{
			_enemyAttackParts[j].Transform = new Transform3D(basis.ScaledLocal(new Vector3(width, height, 1f)), origin + _layoutBasis.Z * (_enemyAttackRig.Layers.Parts[j].Depth * height));
			CopyEnemyAttackSourceMaterial(_enemyAttackPartMaterials[j]);
			_enemyAttackPartMaterials[j].SetShaderParameter(EnemyAttackSpiritBody, Vector2.Zero);
			_enemyAttackPartMaterials[j].SetShaderParameter(EnemyAttackSpiritFin, Vector2.Zero);
			_enemyAttackPartMaterials[j].SetShaderParameter(EnemyAttackSpiritTail, Vector2.Zero);
		}
		for (int k = 0; k < _enemyAttackRig.Joints.Length; k++)
		{
			EnemyAttackRigProfile.Pose pose = _enemyAttackMotion.Joints[k];
			Vector2 pivot = _enemyAttackRig.Joints[k].Pivot;
			float num = MathF.Cos(pose.Rotation);
			float s = MathF.Sin(pose.Rotation);
			float xx = num * pose.Scale.X;
			float xy = (0f - s) * pose.Scale.Y;
			float yx = s * pose.Scale.X;
			float yy = num * pose.Scale.Y;
			float tx = pivot.X + pose.Translation.X - xx * pivot.X - xy * pivot.Y;
			float ty = pivot.Y + pose.Translation.Y - yx * pivot.X - yy * pivot.Y;
			Vector3 rowX = new Vector3(xx, xy, tx);
			Vector3 rowY = new Vector3(yx, yy, ty);
			_enemyAttackMaterial.SetShaderParameter(EnemyAttackRowsX[k], _enemyAttackFixedBodyJoints[k] ? new Vector3(1f, 0f, 0f) : rowX);
			_enemyAttackMaterial.SetShaderParameter(EnemyAttackRowsY[k], _enemyAttackFixedBodyJoints[k] ? new Vector3(0f, 1f, 0f) : rowY);
			ShaderMaterial[] enemyAttackPartMaterials = _enemyAttackPartMaterials;
			foreach (ShaderMaterial obj in enemyAttackPartMaterials)
			{
				obj.SetShaderParameter(EnemyAttackRowsX[k], rowX);
				obj.SetShaderParameter(EnemyAttackRowsY[k], rowY);
			}
			_enemyAttackJointAngles[k] = pose.Rotation;
			_enemyAttackJointTranslations[k] = pose.Translation;
		}
	}

	private void CopyEnemyAttackSourceMaterial(ShaderMaterial material)
	{
		StringName[] enemyAttackSourceUniforms = _enemyAttackSourceUniforms;
		foreach (StringName name in enemyAttackSourceUniforms)
		{
			material.SetShaderParameter(name, _surface.GetShaderParameter(name));
		}
	}

	private Texture2D? SelectRosterSpiritTexture(string id, Texture2D? original, bool phone)
	{
		if (Enumerable.Contains(OS.GetCmdlineUserArgs(), "--enemy-rpg-original"))
		{
			return original;
		}
		if (!EnemySpiritProfile.TryGet(id, out EnemySpiritProfile profile) || profile == null)
		{
			return original;
		}
		bool usePhone = phone && (object)profile.Phone != null;
		EnemySpiritProfile.Variant variant = (usePhone ? profile.Phone : profile.Desktop);
		Texture2D albedo = TryTexture(variant.Stem + ".png");
		Texture2D normal = TryTexture(variant.Stem + "_n.png");
		Texture2D weights = TryTexture(variant.Stem + "_weights.png");
		if (!Valid(albedo) || !Valid(normal) || !Valid(weights) || !ResourceLoader.Exists("res://shaders/rpg_spirit_probe.gdshader"))
		{
			SpiritProbeStatus = "original:roster-resource-contract-failed";
			return original;
		}
		Shader shader = GD.Load<Shader>("res://shaders/rpg_spirit_probe.gdshader");
		if (shader == null)
		{
			return original;
		}
		_rosterSpiritProfile = profile;
		_rosterSpiritVariant = variant;
		_spiritRenderScale = variant.RenderScale;
		_spiritProfileId = id;
		_spiritProbeEnabled = true;
		_spiritProbeStill = Enumerable.Contains(OS.GetCmdlineUserArgs(), "--enemy-rpg-pilot-still");
		_spiritProbeWeights = weights;
		_spiritProbeShader = shader;
		Phone96 = usePhone;
		SpiritProbeStatus = (_spiritProbeStill ? "roster:still" : "roster:local-deformation");
		return albedo;
		bool Valid(Texture2D? texture)
		{
			if (texture != null && texture.GetWidth() == variant.RenderWidth)
			{
				return texture.GetHeight() == variant.RenderHeight;
			}
			return false;
		}
	}

	public void BeginExecutionScale(string kind, float now)
	{
		CancelExecutionScale();
		_executionStart = now;
        // Burrow suppresses beetle harden to a real rest action; retain owned breathing without inventing guard.
        if ((DefinitionId is "enemy_chimera_3" or "enemy_chimera_2" or "enemy_rock_beetle" or "enemy_burrow_worm") && _contactAuthoredFrames != null && kind == "rest") { ResumeAuthoredIdleForPlacement(now); return; }
		// Profiled attacks replace the old generic 1.12 x 0.86 squash.
		if (_style04Motion != null && kind == "attack") return;
		if (kind == null)
		{
			return;
		}
		switch (kind.Length)
		{
		case 6:
		{
			char c = kind[0];
			if (c != 'a')
			{
				switch (c)
				{
				default:
					return;
				case 's':
					break;
				case 'd':
					if (kind == "defend")
					{
						_executionDuration = 0.3f;
						_executionX = 1.06f;
						_executionY = 0.94f;
					}
					return;
				}
				if (!(kind == "summon"))
				{
					break;
				}
				goto IL_0249;
			}
			if (!(kind == "attack"))
			{
				break;
			}
			goto IL_01c1;
		}
		case 4:
		{
			char c = kind[0];
			if ((uint)c <= 104u)
			{
				switch (c)
				{
				default:
					return;
				case 'f':
					break;
				case 'b':
					if (kind == "buff")
					{
						_executionDuration = 0.4f;
						_executionX = (_executionY = 1.15f);
						_executionPuff = true;
					}
					return;
				case 'h':
					if (kind == "heal")
					{
						_executionDuration = 0.3f;
						_executionX = (_executionY = 1.06f);
						_executionPuff = true;
					}
					return;
				}
				if (!(kind == "flee"))
				{
					break;
				}
				goto IL_01e3;
			}
			switch (c)
			{
			case 's':
				if (!(kind == "seal"))
				{
					break;
				}
				goto IL_0205;
			case 'm':
				if (!(kind == "mill"))
				{
					break;
				}
				goto IL_0205;
			case 'r':
				{
					if (kind == "rest")
					{
						_executionDuration = 0.5f;
						_executionX = 1.04f;
						_executionY = 0.96f;
					}
					break;
				}
				IL_0205:
				_executionDuration = 0.26f;
				_executionX = 1.1f;
				_executionY = 0.9f;
				break;
			}
			break;
		}
		case 5:
			switch (kind[0])
			{
			default:
				return;
			case 'h':
				break;
			case 'r':
				if (kind == "rally")
				{
					_executionDuration = 0.4f;
					_executionX = (_executionY = 1.12f);
					_executionPuff = true;
				}
				return;
			}
			if (!(kind == "hatch"))
			{
				break;
			}
			goto IL_0249;
		case 11:
			if (!(kind == "destroy-set"))
			{
				break;
			}
			goto IL_01c1;
		case 13:
			if (!(kind == "destroy-token"))
			{
				break;
			}
			goto IL_01c1;
		case 10:
			if (!(kind == "steal-gold"))
			{
				break;
			}
			goto IL_01e3;
		case 3:
			if (kind == "hex")
			{
				_executionDuration = 0.3f;
				_executionX = (_executionY = 1.08f);
				_executionPuff = true;
			}
			break;
		case 7:
		case 8:
		case 9:
		case 12:
			break;
			IL_01e3:
			_executionDuration = 0.26f;
			_executionX = 1.12f;
			_executionY = 0.86f;
			break;
			IL_0249:
			_executionDuration = 0.4f;
			_executionX = (_executionY = 1.12f);
			_executionPuff = true;
			break;
			IL_01c1:
			_executionDuration = 0.26f;
			_executionX = 1.12f;
			_executionY = 0.86f;
			break;
		}
	}

	public void CancelExecutionScale()
	{
        _bruteAuthoredFrames?.Cancel(); _wolfAuthoredFrames?.Cancel(); _crossbowAuthoredFrames?.Cancel(); _contactAuthoredFrames?.Cancel(); _drummerAuthoredFrames?.Cancel(); _hexAuthoredFrames?.Cancel();
		_executionVersion++;
		_style04Motion?.Cancel();
		_style04Support?.Cancel();
		_executionDuration = 0f;
		_executionPuff = false;
		ExecutionScale = Vector2.One;
        ApplyAuthoredFrame(_lastClock);
	}

	private void SampleExecutionScale(float now)
	{
		if (_executionDuration <= 0f)
		{
			return;
		}
		float k = Math.Clamp((now - _executionStart) / _executionDuration, 0f, 1f);
		if (k >= 1f)
		{
			_executionDuration = 0f;
			ExecutionScale = Vector2.One;
			return;
		}
		float a;
		if (_executionPuff)
		{
			a = MathF.Sin(k * (float)Math.PI);
		}
		else if (k < 0.45f)
		{
			float t = k / 0.45f;
			a = 1f - (1f - t) * (1f - t);
		}
		else
		{
			float t2 = (k - 0.45f) / 0.55f - 1f;
			a = 0f - (2.70158f * t2 * t2 * t2 + 1.70158f * t2 * t2);
		}
		ExecutionScale = new Vector2(1f + (_executionX - 1f) * a, 1f + (_executionY - 1f) * a);
	}

	private Texture2D? SelectSpiritProbeTexture(string category, string id, Texture2D? original, bool phone)
	{
		if (category != "enemies")
		{
			return original;
		}
		string stem = id switch
		{
			"enemy_probe" => "res://assets/Art/enemies/rpg_spirit_probe/probe", 
			"enemy_iron_clam" => "res://assets/Art/enemies/rpg_spirit_iron_clam/iron_clam", 
			"enemy_maw_hunter" => "res://assets/Art/enemies/rpg_spirit_maw_hunter/maw_hunter", 
			_ => null, 
		};
		if (stem == null)
		{
			return SelectRosterSpiritTexture(id, original, phone);
		}
		if (Enumerable.Contains(OS.GetCmdlineUserArgs(), "--enemy-rpg-original"))
		{
			return original;
		}
		int renderScale = ((!(id != "enemy_probe") || Enumerable.Contains(OS.GetCmdlineUserArgs(), "--enemy-rpg-family-64")) ? 1 : 2);
		if (renderScale == 2)
		{
			stem += "_128";
		}
		Texture2D albedo = TryTexture(stem + ".png");
		Texture2D normal = TryTexture(stem + "_n.png");
		Texture2D weights = TryTexture(stem + "_weights.png");
		if (!IsSpiritProbeCanvas(albedo, renderScale) || !IsSpiritProbeCanvas(normal, renderScale) || !IsSpiritProbeCanvas(weights, renderScale) || !ResourceLoader.Exists("res://shaders/rpg_spirit_probe.gdshader"))
		{
			SpiritProbeStatus = "original:pilot-resource-contract-failed";
			return original;
		}
		Shader shader = GD.Load<Shader>("res://shaders/rpg_spirit_probe.gdshader");
		if (shader == null)
		{
			SpiritProbeStatus = "original:pilot-shader-missing";
			return original;
		}
		_spiritRenderScale = renderScale;
		_spiritProfileId = id;
		_spiritProbeEnabled = true;
		_spiritProbeStill = Enumerable.Contains(OS.GetCmdlineUserArgs(), "--enemy-rpg-pilot-still");
		_spiritProbeWeights = weights;
		_spiritProbeShader = shader;
		SpiritProbeStatus = (_spiritProbeStill ? "pilot:still" : "pilot:local-deformation");
		return albedo;
	}

	private static bool IsSpiritProbeCanvas(Texture2D? texture, int scale)
	{
		if (texture != null && texture.GetWidth() == 64 * scale)
		{
			return texture.GetHeight() == 64 * scale;
		}
		return false;
	}

	private void ApplySpiritRegisteredMargins(ref int top, ref int bottom)
	{
		if (_spiritProbeEnabled && _spiritRenderScale != 1)
		{
			EnemySpiritProfile.Variant variant = _rosterSpiritVariant;
			if ((object)variant != null)
			{
				top = variant.Top * variant.RenderScale;
				bottom = variant.Bottom * variant.RenderScale;
			}
			else
			{
				top = ((_spiritProfileId == "enemy_iron_clam") ? 20 : 4) * _spiritRenderScale;
				bottom = ((_spiritProfileId == "enemy_iron_clam") ? 5 : 7) * _spiritRenderScale;
			}
		}
	}

	private void ConfigureSpiritProbeIdle(Texture2D texture)
	{
		_idle.Add(texture);
		_r2Idle = true;
		_idleDuration = _rosterSpiritProfile?.Period ?? SpiritFamilyIdleMotion.PeriodFor(_spiritProfileId);
		_surface.SetShaderParameter("_SpiritWeightMap", _spiritProbeWeights);
		SampleSpiritProbeIdle();
	}

	private void SampleSpiritProbeIdle()
	{
		if (_spiritProbeEnabled)
		{
			SpiritProbeOffsets = _rosterSpiritProfile?.Sample(_poseFrameTime, _spiritProbeStill) ?? SpiritFamilyIdleMotion.Sample(_spiritProfileId, _poseFrameTime, _spiritProbeStill);
			SpiritProbeIdleMotion.Offsets p = SpiritProbeOffsets;
			EnemySpiritProfile.Variant variant = _rosterSpiritVariant;
			float offsetScale = (((object)variant != null) ? ((float)variant.RenderWidth / 64f) : ((float)_spiritRenderScale));
			_surface.SetShaderParameter("_SpiritBodyOffset", new Vector2(p.BodyX, p.BodyY) * offsetScale);
			_surface.SetShaderParameter("_SpiritFinOffset", new Vector2(p.FinX, p.FinY) * offsetScale);
			_surface.SetShaderParameter("_SpiritTailOffset", new Vector2(p.TailX, p.TailY) * offsetScale);
		}
	}
}

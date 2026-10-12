using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Godot;
using Godot.Bridge;
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

	private Texture2D _baseTexture = null;

	private float _footAnchorOffsetX;

	private string _pose = "idle";

	private int _poseFrame;

	private float _poseFrameTime;

	private float _poseClock;

	private float _lastAttackProgress = -1f;

	private float _lastHitProgress = -1f;

	private bool _poseInitialized;

	private bool _usesExplicitPoses;

	private bool _lastGuarding;

	private bool _r2Idle;

	private ShaderMaterial _surface = null;

	private Texture2D? _boundTexture;

	private MeshInstance3D _shadow = null;

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

	public string DefinitionId { get; private set; } = "";

	public string ArtId { get; private set; } = "";

	public bool Phone96 { get; private set; }

	public int BaseTextureWidth => _baseTexture.GetWidth();

	public int BaseTextureHeight => _baseTexture.GetHeight();

	public int TopMarginRows { get; private set; }

	public float BaseCanvasWorldHeight => _imageHeight;

	public int EnemyIndex { get; set; } = -1;

	public string Uid { get; set; } = "";

	public Vector3 Home { get; set; }

	public float Height { get; private set; } = 2.1f;

	public int DisplayedHp => _hp;

	public int DisplayedBlock => _blockValue;

	public bool Placeholder { get; private set; }

	public bool Dead { get; private set; }

	public bool Selected { get; set; }

	public Sprite3D Sprite { get; private set; } = null;

	public Label3D Vitals { get; private set; } = null;

	public Label3D IntentLabel { get; private set; } = null;

	public int IdleSlot { get; set; } = -1;

	public bool ShowLabels { get; set; } = false;

	public Vector3 Contact => Home + _layoutBasis.Y * Height * 0.54f;

	public Vector3 Impact => Home + _layoutBasis.Y * Height * 0.5f + _layoutBasis.Z * 0.28f;

	public float DissolveAmount => _dissolve;

	public void Configure(string category, string id, string displayName, Vector3 foot, float height = 2.1f, bool phone = false)
	{
		_actorCategory = category;
		base.Name = (category + "_" + id).Replace(':', '_');
		DefinitionId = id;
		Home = foot;
		base.Position = foot;
		Height = height;
		Texture2D tex = TryTexture($"res://assets/Art/{category}/{id}.png");
		ArtId = id;
		if (phone && category == "enemies" && tex != null && tex.GetWidth() >= 128)
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
			tex = ((category == "dolls") ? ProceduralActorTexture.Doll(id, 64) : ProceduralActorTexture.Generate(id, category == "leaders", 64));
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
			Shader = GD.Load<Shader>("res://shaders/authored_StageUnitLit.gdshader")
		};
		Sprite.Offset = new Vector2(_footAnchorOffsetX, 0f);
		Sprite.MaterialOverride = _surface;
		BindSurface(tex);
		AddChild(Sprite, forceReadableName: false, InternalMode.Disabled);
		string animCategory = ((category == "leaders") ? "leaders" : "enemies");
		LoadFrames(animCategory, ArtId, "idle", _idle);
		if ((category == "enemies" || category == "dolls") ? true : false)
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
		LoadFrames(animCategory, ArtId, "attack", _attack);
		LoadFrames(animCategory, ArtId, "hurt", _hurt);
		LoadFrames(animCategory, ArtId, "block", _block);
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
		return ResourceLoader.Exists(p) ? GD.Load<Texture2D>(p) : null;
	}

	private static void LoadFrames(string category, string id, string kind, List<Texture2D> into)
	{
		for (int i = 0; i < 16; i++)
		{
			Texture2D t = TryTexture($"res://assets/Art/{category}/anim/{id}_{kind}_{i}.png");
			if (t == null)
			{
				break;
			}
			into.Add(t);
		}
	}

	public void PlayPose(string name, float clock)
	{
		_usesExplicitPoses = true;
		StartPose(name, clock);
	}

	private void StartPose(string name, float clock)
	{
		List<Texture2D> frames = PoseFrames(name);
		if (frames != null && frames.Count != 0)
		{
			_pose = name;
			_poseFrame = 0;
			_poseFrameTime = 0f;
			_poseClock = clock;
			_poseInitialized = true;
			ApplyPoseFrame();
		}
	}

	private List<Texture2D>? PoseFrames(string name)
	{
		if (1 == 0)
		{
		}
		List<Texture2D> result = name switch
		{
			"idle" => _idle, 
			"attack" => _attack, 
			"hurt" => _hurt, 
			"block" => _block, 
			_ => null, 
		};
		if (1 == 0)
		{
		}
		return result;
	}

	private float PoseFrameDuration()
	{
		if (_pose == "idle")
		{
			return _idleDuration;
		}
		string pose = _pose;
		if (1 == 0)
		{
		}
		float[] array = pose switch
		{
			"attack" => (DefinitionId == "leader_blue") ? BlueAttackDur : AttackDur, 
			"hurt" => HurtDur, 
			"block" => (DefinitionId == "leader_blue") ? BlueBlockDur : BlockDur, 
			_ => Array.Empty<float>(), 
		};
		if (1 == 0)
		{
		}
		float[] durs = array;
		return (_poseFrame < durs.Length) ? durs[_poseFrame] : 0.125f;
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
		float delta = Math.Clamp(clock - _poseClock, 0f, 0.04f);
		_poseClock = clock;
		List<Texture2D> frames = PoseFrames(_pose);
		if (frames == null || frames.Count == 0)
		{
			return;
		}
		_poseFrameTime += delta;
		float duration = PoseFrameDuration();
		if (_poseFrameTime < duration)
		{
			return;
		}
		_poseFrameTime -= duration;
		_poseFrame++;
		if (_poseFrame >= frames.Count)
		{
			_poseFrame = 0;
			if (_pose != "idle")
			{
				_pose = "idle";
			}
		}
		ApplyPoseFrame();
	}

	private void ApplyPoseFrame()
	{
		List<Texture2D> frames = PoseFrames(_pose);
		Texture2D texture = ((frames != null && frames.Count > 0) ? frames[Math.Clamp(_poseFrame, 0, frames.Count - 1)] : _baseTexture);
		Sprite.Offset = new Vector2(_footAnchorOffsetX, (float)(texture.GetHeight() - _baseTexture.GetHeight()) * 0.5f);
		Sprite.Texture = texture;
		BindSurface(texture);
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
		Dead = dead;
		if (!dead)
		{
			_deathFade = 0f;
			SetDissolve(0f);
		}
	}

	public void UpdateVisual(float clock, float attackProgress = -1f, float hitProgress = -1f, bool guarding = false)
	{
		if (Sprite == null)
		{
			return;
		}
		float delta = Math.Max(0f, clock - _lastClock);
		_lastClock = clock;
		base.Position = Home;
		Sprite.Position = _layoutBasis.Y * (_imageHeight / 2f - _feetPad);
		Sprite.Basis = _layoutBasis;
		_surface.SetShaderParameter("_Flash", Math.Clamp((_flashUntil - clock) / 0.18f, 0f, 1f) * 0.1f);
		if (Dead)
		{
			_deathFade = _dissolve;
			Sprite.Modulate = new Color(1f, 1f, 1f, 1f - _deathFade);
			Vitals.Visible = false;
			IntentLabel.Visible = false;
			_shadow.Visible = _deathFade < 1f;
			RefreshSourceMaterial();
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
		AdvancePose(clock);
		if (attackProgress >= 0f)
		{
			float pulse = MathF.Sin(Math.Clamp(attackProgress, 0f, 1f) * (float)Math.PI);
			base.Position = Home + new Vector3((EnemyIndex >= 0) ? ((0f - pulse) * 0.2f) : (pulse * 0.2f), 0f, 0f);
		}
		if (hitProgress >= 0f)
		{
			float pulse2 = MathF.Sin(Math.Clamp(hitProgress, 0f, 1f) * (float)Math.PI);
			Sprite.Position += _layoutBasis.X * (MathF.Sin(hitProgress * 23f) * 0.07f * pulse2);
		}
		RefreshSourceMaterial();
	}

	public void ConfigureSourceAppearance(int act, Camera3D layoutCamera, int enemyGroup = 1)
	{
		_layoutCamera = layoutCamera;
		_layoutBasis = (IsInsideTree() ? base.GlobalBasis : base.Basis).Inverse() * layoutCamera.GlobalBasis.Orthonormalized();
		Sprite.Basis = _layoutBasis;
		Sprite.Position = _layoutBasis.Y * (_imageHeight / 2f - _feetPad);
		bool phone = OS.HasFeature("android") || OS.HasFeature("ios") || OS.GetCmdlineUserArgs().Contains("--ui-phone");
		_sourceLook = ActorMaterialBinding.LoadLook(act, phone);
		_sourceDistanceScale = layoutCamera.GlobalPosition.Dot(layoutCamera.GlobalBasis.Z) / Math.Max(0.01f, ActorMaterialBinding.Number(_sourceLook["baseCameraDistance"], 16.62f));
		_sourceContext.ActorKey = ((_actorCategory == "leaders") ? "player" : ((_actorCategory == "dolls") ? ("doll:" + Uid) : ("enemy" + Math.Max(0, EnemyIndex))));
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
				Vector3 point = base.GlobalPosition + base.GlobalBasis * (_layoutBasis.Y * (_imageHeight * 0.5f * (1f - _sourceContext.FeetPad)));
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
			ActorMaterialBinding.Bind(_surface, values, _sourceLook, warmKey: false, _sourceDistanceScale);
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		List<MethodInfo> methods = new List<MethodInfo>(21);
		methods.Add(new MethodInfo(MethodName.Configure, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "category", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.String, "id", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.String, "displayName", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Vector3, "foot", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Float, "height", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Bool, "phone", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.SetSourceImageHeight, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Float, "height", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.Label, new PropertyInfo(Variant.Type.Object, "", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Label3D"), exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "text", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Float, "y", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Int, "size", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.BindSurface, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Object, "texture", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Texture2D"), exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.TryTexture, new PropertyInfo(Variant.Type.Object, "", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Texture2D"), exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "p", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.PlayPose, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "name", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Float, "clock", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.StartPose, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.String, "name", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Float, "clock", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.PoseFrameDuration, new PropertyInfo(Variant.Type.Float, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.VanDerCorput, new PropertyInfo(Variant.Type.Float, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Int, "slot", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.AdvancePose, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Float, "clock", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.ApplyPoseFrame, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.SetVitals, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Int, "hp", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Int, "maxHp", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Int, "block", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.DamageFallback, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Int, "hpLoss", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Int, "blocked", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.GainBlock, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Int, "amount", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.Heal, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Int, "amount", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.SetDead, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Bool, "dead", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.UpdateVisual, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Float, "clock", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Float, "attackProgress", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Float, "hitProgress", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Bool, "guarding", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.ConfigureSourceAppearance, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Int, "act", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Object, "layoutCamera", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Camera3D"), exported: false),
			new PropertyInfo(Variant.Type.Int, "enemyGroup", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.Flash, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Float, "clock", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
			new PropertyInfo(Variant.Type.Float, "duration", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.SetDissolve, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Float, "amount", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.RefreshSourceMaterial, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		return methods;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		if (method == MethodName.Configure && args.Count == 6)
		{
			Configure(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<string>(in args[1]), VariantUtils.ConvertTo<string>(in args[2]), VariantUtils.ConvertTo<Vector3>(in args[3]), VariantUtils.ConvertTo<float>(in args[4]), VariantUtils.ConvertTo<bool>(in args[5]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.SetSourceImageHeight && args.Count == 1)
		{
			SetSourceImageHeight(VariantUtils.ConvertTo<float>(in args[0]));
			ret = default(godot_variant);
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
			ret = default(godot_variant);
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
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.StartPose && args.Count == 2)
		{
			StartPose(VariantUtils.ConvertTo<string>(in args[0]), VariantUtils.ConvertTo<float>(in args[1]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.PoseFrameDuration && args.Count == 0)
		{
			ret = VariantUtils.CreateFrom<float>(PoseFrameDuration());
			return true;
		}
		if (method == MethodName.VanDerCorput && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<float>(VanDerCorput(VariantUtils.ConvertTo<int>(in args[0])));
			return true;
		}
		if (method == MethodName.AdvancePose && args.Count == 1)
		{
			AdvancePose(VariantUtils.ConvertTo<float>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.ApplyPoseFrame && args.Count == 0)
		{
			ApplyPoseFrame();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.SetVitals && args.Count == 3)
		{
			SetVitals(VariantUtils.ConvertTo<int>(in args[0]), VariantUtils.ConvertTo<int>(in args[1]), VariantUtils.ConvertTo<int>(in args[2]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.DamageFallback && args.Count == 2)
		{
			DamageFallback(VariantUtils.ConvertTo<int>(in args[0]), VariantUtils.ConvertTo<int>(in args[1]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.GainBlock && args.Count == 1)
		{
			GainBlock(VariantUtils.ConvertTo<int>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.Heal && args.Count == 1)
		{
			Heal(VariantUtils.ConvertTo<int>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.SetDead && args.Count == 1)
		{
			SetDead(VariantUtils.ConvertTo<bool>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.UpdateVisual && args.Count == 4)
		{
			UpdateVisual(VariantUtils.ConvertTo<float>(in args[0]), VariantUtils.ConvertTo<float>(in args[1]), VariantUtils.ConvertTo<float>(in args[2]), VariantUtils.ConvertTo<bool>(in args[3]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.ConfigureSourceAppearance && args.Count == 3)
		{
			ConfigureSourceAppearance(VariantUtils.ConvertTo<int>(in args[0]), VariantUtils.ConvertTo<Camera3D>(in args[1]), VariantUtils.ConvertTo<int>(in args[2]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.Flash && args.Count == 2)
		{
			Flash(VariantUtils.ConvertTo<float>(in args[0]), VariantUtils.ConvertTo<float>(in args[1]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.SetDissolve && args.Count == 1)
		{
			SetDissolve(VariantUtils.ConvertTo<float>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.RefreshSourceMaterial && args.Count == 0)
		{
			RefreshSourceMaterial();
			ret = default(godot_variant);
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
		ret = default(godot_variant);
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
		List<PropertyInfo> properties = new List<PropertyInfo>();
		properties.Add(new PropertyInfo(Variant.Type.String, PropertyName.DefinitionId, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.String, PropertyName.ArtId, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName.Phone96, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName.BaseTextureWidth, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName.BaseTextureHeight, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName.TopMarginRows, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Float, PropertyName.BaseCanvasWorldHeight, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName._bottomMarginRows, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName.EnemyIndex, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.String, PropertyName.Uid, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Vector3, PropertyName.Home, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Float, PropertyName.Height, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName.DisplayedHp, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName.DisplayedBlock, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName.Placeholder, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName.Dead, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName.Selected, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Object, PropertyName.Sprite, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Object, PropertyName.Vitals, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Object, PropertyName.IntentLabel, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Object, PropertyName._baseTexture, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Float, PropertyName._footAnchorOffsetX, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.String, PropertyName._pose, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName._poseFrame, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Float, PropertyName._poseFrameTime, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Float, PropertyName._poseClock, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Float, PropertyName._lastAttackProgress, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Float, PropertyName._lastHitProgress, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName._poseInitialized, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName._usesExplicitPoses, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName._lastGuarding, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName._r2Idle, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName.IdleSlot, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Object, PropertyName._surface, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Object, PropertyName._boundTexture, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Object, PropertyName._shadow, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName._hp, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName._maxHp, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName._blockValue, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Float, PropertyName._deathFade, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Float, PropertyName._imageHeight, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Float, PropertyName._feetPad, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Float, PropertyName._lastClock, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Float, PropertyName._idleDuration, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName.ShowLabels, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Vector3, PropertyName.Contact, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Vector3, PropertyName.Impact, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.String, PropertyName._actorCategory, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Basis, PropertyName._layoutBasis, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Object, PropertyName._layoutCamera, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.PackedVector4Array, PropertyName._lightPositions, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.PackedVector4Array, PropertyName._lightColors, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Float, PropertyName._sourceDistanceScale, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Float, PropertyName._edgeLinear, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Float, PropertyName._flashUntil, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Float, PropertyName._dissolve, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Float, PropertyName.DissolveAmount, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		return properties;
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
}

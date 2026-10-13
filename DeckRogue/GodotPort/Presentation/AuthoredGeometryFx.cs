using System;
using System.Collections.Generic;
using System.ComponentModel;
using DeckRogue.Game;
using Godot;
using Godot.Bridge;
using Godot.Collections;
using Godot.NativeInterop;
using Newtonsoft.Json.Linq;

namespace DeckRogue.GodotPort.Presentation;

[ScriptPath("res://scripts/presentation/AuthoredGeometryFx.cs")]
public class AuthoredGeometryFx : Node3D
{
	private sealed class Draw
	{
		public ArrayMesh Mesh = new ArrayMesh();

		public Vector3[] Positions = System.Array.Empty<Vector3>();

		public Vector3[] Normals = System.Array.Empty<Vector3>();

		public Vector2[] Uv = System.Array.Empty<Vector2>();

		public Vector2[] Effect = System.Array.Empty<Vector2>();

		public float[] Hdr = System.Array.Empty<float>();

		public Godot.Collections.Array Arrays = new Godot.Collections.Array();

		public MeshInstance3D Node = null;

		public ShaderMaterial Material = null;
	}

	public new class MethodName : Node3D.MethodName
	{
		public new static readonly StringName _Ready = "_Ready";

		public static readonly StringName C = "C";

		public static readonly StringName Sample = "Sample";

		public static readonly StringName Clear = "Clear";
	}

	public new class PropertyName : Node3D.PropertyName
	{
		public static readonly StringName RealLightsEnabled = "RealLightsEnabled";

		public static readonly StringName QualityLevel = "QualityLevel";

		public static readonly StringName ActiveLights = "ActiveLights";

		public static readonly StringName VertexCount = "VertexCount";

		public static readonly StringName IsActive = "IsActive";

		public static readonly StringName Renderer = "Renderer";

		public static readonly StringName _targetLight = "_targetLight";

		public static readonly StringName _floorLight = "_floorLight";

		public static readonly StringName _roles = "_roles";

		public static readonly StringName _lightColor = "_lightColor";

		public static readonly StringName _target = "_target";

		public static readonly StringName _feet = "_feet";

		public static readonly StringName _source = "_source";

		public static readonly StringName _basis = "_basis";

		public static readonly StringName _height = "_height";

		public static readonly StringName _size = "_size";

		public static readonly StringName _emission = "_emission";

		public static readonly StringName _particles = "_particles";

		public static readonly StringName _lightGain = "_lightGain";
	}

	public new class SignalName : Node3D.SignalName
	{
	}

	private readonly List<Draw> _draws = new List<Draw>();

	private readonly System.Collections.Generic.Dictionary<string, NativeEffect> _cache = new System.Collections.Generic.Dictionary<string, NativeEffect>();

	private NativeEffect? _effect;

	private AttackRoute _route = AttackRouteCatalog.Neutral();

	private EffectScenario _scenario = null;

	private OmniLight3D _targetLight = null;

	private OmniLight3D _floorLight = null;

	private Color[] _roles = new Color[4];

	private Color _lightColor;

	private Vector3 _target;

	private Vector3 _feet;

	private Vector3 _source;

	private Basis _basis = Basis.Identity;

	private float _height = 1f;

	private float _size = 1f;

	private float _emission = 1f;

	private float _particles = 1f;

	private float _lightGain = 1f;

	public bool RealLightsEnabled { get; set; } = true;

	public int QualityLevel { get; set; }

	// Portable low-quality adaptation: preserve one existing shader-only bounce;
	// the real floor OmniLight stays disabled below in Sample at quality 2.
	public bool AnalyticalFloorFallbackEnabled { get; set; } = true;

	public int ActiveLights
	{
		get
		{
			OmniLight3D targetLight = _targetLight;
			int num = ((targetLight != null && targetLight.LightEnergy > 0f) ? 1 : 0);
			OmniLight3D floorLight = _floorLight;
			return num + ((floorLight != null && floorLight.LightEnergy > 0f) ? 1 : 0);
		}
	}

	public int VertexCount { get; private set; }

	public bool IsActive => _effect != null && base.Visible;

	public string Renderer => _effect?.Shader ?? "";

	public override void _Ready()
	{
		_targetLight = new OmniLight3D
		{
			Name = "AuthoredTargetLight",
			LightEnergy = 0f,
			OmniRange = 7f
		};
		_floorLight = new OmniLight3D
		{
			Name = "AuthoredGroundLight",
			LightEnergy = 0f,
			OmniRange = 13f
		};
		AddChild(_targetLight, forceReadableName: false, InternalMode.Disabled);
		AddChild(_floorLight, forceReadableName: false, InternalMode.Disabled);
		base.Visible = false;
	}

	public bool Begin(AttackRoute route, EffectScenario scenario, Vector3 source, Vector3 target, Vector3 feet, float visibleHeight, Basis cameraBasis, int contactIndex = 0)
	{
		Clear();
		_route = route;
		_scenario = scenario;
		string key = route.Renderer + "/" + route.Family + "/" + scenario.Tier;
		if (!_cache.TryGetValue(key, out NativeEffect effect))
		{
			effect = NativeGeometryBridge.Create(route, scenario.Tier);
			if (effect == null)
			{
				return false;
			}
			_cache[key] = effect;
		}
		_effect = effect;
		_source = source;
		_target = target;
		_feet = feet;
		_basis = cameraBasis;
		_height = Math.Max(0.25f, visibleHeight);
		bool small = scenario.Tier == EffectTier.Small;
		bool large = scenario.Tier == EffectTier.Large;
		_size = (small ? 0.88f : (large ? 1.12f : 1f));
		_emission = (small ? 0.72f : (large ? 1.18f : 1f));
		_particles = (small ? 0.65f : (large ? 1.15f : 1f));
		_lightGain = (small ? 0.65f : (large ? 1.2f : 1f));
		DamageVfxIntensity.Visual visual = scenario.SourceVisual;
		if (visual != null)
		{
			_size = visual.Size;
			_emission = visual.Emission;
			_particles = visual.Particles;
			_lightGain = visual.Light;
		}
		else if (route.FixedContact)
		{
			_size = 0.88f;
			_emission = 0.72f;
			_particles = 0.65f;
			_lightGain = 0.65f;
		}
		if (route.Renderer == "PhysicalSlashFx")
		{
			_size = 1f;
			_emission = 1f;
			_particles = 1f;
			_lightGain = 1f;
		}
		if (route.Renderer == "SystemOutcomeFx")
		{
			_size = (_emission = (_particles = (_lightGain = 1f)));
		}
		if (route.Renderer == "HealContinuousFx")
		{
			_size = (route.FixedSmall ? 0.72f : 1f);
			_emission = (route.FixedSmall ? 0.75f : 1f);
			_particles = 1f;
			_lightGain = (route.FixedSmall ? 0.65f : 1f);
			_target = _feet + _basis.Y * (_height * 0.46521738f);
		}
		effect.ConfigureMirror?.Invoke(SourceAttackOrientation.AlternateHit(contactIndex));
		effect.ConfigureParticleGain?.Invoke(_particles);
		LoadRoles(effect);
		effect.ConfigureSource?.Invoke(_basis.Inverse() * (source - target) / (_height * _size));
		while (_draws.Count < effect.Batches.Length)
		{
			Draw d = new Draw();
			d.Node = new MeshInstance3D
			{
				Name = "AuthoredBatch" + _draws.Count,
				Mesh = d.Mesh,
				CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
			};
			AddChild(d.Node, forceReadableName: false, InternalMode.Disabled);
			_draws.Add(d);
		}
		for (int j = 0; j < effect.Batches.Length; j++)
		{
			ConfigureMaterial(_draws[j], effect.Batches[j], j);
		}
		base.Visible = true;
		Sample(0f);
		return true;
	}

	private void LoadRoles(NativeEffect e)
	{
		string path = "res://assets/presentation-data/" + e.Palette + ".json";
		JToken obj = (FileAccess.FileExists(path) ? JObject.Parse(FileAccess.GetFileAsString(path)) : new JObject());
		JToken profiles = obj["profiles"];
		if (profiles != null)
		{
			obj = profiles[e.RouteFamily] ?? throw new InvalidOperationException("Missing authored palette profile: " + e.RouteFamily);
		}
		JToken families = obj["families"];
		if (families != null)
		{
			obj = families[e.RouteFamily] ?? throw new InvalidOperationException("Missing authored palette family: " + e.RouteFamily);
		}
		if (e.Shader == "EarthStompSurfaceR2")
		{
			obj = obj[(e.Family == 0) ? "earth" : "stomp"] ?? obj;
		}
		if (e.Shader == "BoundedContactFx")
		{
			string[] surfaces = new string[8] { "Neutral", "Water", "Cold", "Wood", "Plant", "Fire", "Smoke", "Dark" };
			JToken surface = obj["surfaces"]?[surfaces[e.Family]];
			_roles[0] = new Color("444444").SrgbToLinear();
			_roles[1] = new Color(surface?["primary"]?.Value<string>() ?? "#A8A8A8").SrgbToLinear();
			_roles[2] = _roles[1];
			_roles[3] = new Color(surface?["core"]?.Value<string>() ?? "#EEEEEE").SrgbToLinear();
			_lightColor = Colors.White;
		}
		else
		{
			string[] roles = new string[4] { "shadow", "primary", "edge", "core" };
			for (int i = 0; i < 4; i++)
			{
				_roles[i] = new Color((string)(obj["roles_srgb"]?[roles[i]]?.Value<string>() ?? (i switch
				{
					2 => "#DEDCD1", 
					1 => "#C6C6BE", 
					0 => "#535E69", 
					_ => "#F2EEE4", 
				}))).SrgbToLinear();
			}
			_lightColor = new Color(obj["scene_light_srgb"]?.Value<string>() ?? "#C6C6BE");
		}
	}

	private static Vector4 C(Color c)
	{
		return new Vector4(c.R, c.G, c.B, c.A);
	}

	private void ConfigureMaterial(Draw draw, NativeBatch b, int index)
	{
		if (draw.Positions.Length != b.Count)
		{
			draw.Positions = new Vector3[b.Count];
			draw.Normals = new Vector3[b.Count];
			draw.Uv = new Vector2[b.Count];
			draw.Effect = new Vector2[b.Count];
			draw.Hdr = new float[b.Count * 4];
			draw.Arrays.Resize(13);
		}
		NativeEffect e = _effect;
		string shader = ((e.Shader == "EarthStompSurfaceR2" && b.Kind == 0) ? "EarthStompRockR2" : e.Shader);
		ShaderMaterial material = new ShaderMaterial
		{
			RenderPriority = Math.Clamp(index, -128, 127),
			Shader = GD.Load<Shader>("res://shaders/authored_" + shader + ".gdshader")
		};
		material.SetShaderParameter("use_authored_color", true);
		for (int i = 0; i < 4; i++)
		{
			material.SetShaderParameter((new string[4] { "_RoleShadow", "_RolePrimary", "_RoleEdge", "_RoleCore" })[i], C(_roles[i]));
		}
		material.SetShaderParameter("_Primary", C(_roles[1]));
		material.SetShaderParameter("_Core", C(_roles[3]));
		material.SetShaderParameter("_Kind", b.Kind);
		material.SetShaderParameter("_Family", e.Family);
		material.SetShaderParameter("_Mode", e.Family);
		float gain = ((!(e.Shader == "VineSlashFx")) ? ((!(e.Shader == "PhysicalContactFx")) ? 1f : ((index == 2) ? _particles : _emission)) : (index switch
		{
			2 => 3.8f * _emission, 
			1 => 1.8f * _emission, 
			0 => 2.25f * _emission, 
			_ => 2.2f * _particles, 
		}));
		float emission = ((!(e.Shader == "EarthStompSurfaceR2")) ? _emission : ((b.Kind < 0) ? _emission : ((b.Kind == 0) ? _particles : 1f)));
		material.SetShaderParameter("_Gain", gain);
		material.SetShaderParameter("_Emission", emission);
		material.SetShaderParameter("_Coverage", 1f);
		string texture = e.Texture;
		if (e.Shader == "PlayerContactFx")
		{
			texture = ((b.Kind == 1) ? "physical-pierce-surface-r1" : ((b.Kind == 2) ? "physical-meteor-intact-r1" : "enemy-blunt-contact-r1"));
		}
		if (texture.Length > 0 && ResourceLoader.Exists("res://assets/Art/fx/" + texture + ".png"))
		{
			Texture2D tex = GD.Load<Texture2D>("res://assets/Art/fx/" + texture + ".png");
			material.SetShaderParameter("_BaseMap", tex);
			material.SetShaderParameter("_BodyMap", tex);
			material.SetShaderParameter("_GrainTex", tex);
		}
		if (e.Lookup.Length > 0)
		{
			material.SetShaderParameter("_LookupMap", GD.Load<Texture2D>("res://assets/Art/fx/" + e.Lookup + ".png"));
		}
		material.SetShaderParameter("_UseTexture", (!(e.Shader == "PhysicalSlashFx")) ? ((!(e.Shader == "VineSlashFx")) ? ((index == 0) ? 1f : 0f) : ((index < 2) ? 1f : 0f)) : ((b.Kind == 0) ? 1f : 0f));
		material.SetShaderParameter("_UseBeam", (e.RouteFamily == "enemy_beam" && index == 0) ? 1f : 0f);
		material.SetShaderParameter("_BluntAccent", (e.RouteFamily == "enemy_blunt") ? 1f : 0f);
		draw.Material = material;
		draw.Node.MaterialOverride = material;
	}

	private Color VertexColor(NativeBatch b, int i, int batch)
	{
		NativeEffect e = _effect;
		int r = Math.Clamp(b.Role(i), 0, 3);
		Color role = Colors.White;
		float gain = b.Gain(i) * _emission;
		switch (e.Shader)
		{
		case "PhysicalSlashFx":
			role = ((b.Kind == 0) ? Colors.White : _roles[3]);
			break;
		case "WaterContinuousFx":
			role = ((b.Kind == 1) ? _roles[3] : Colors.White);
			gain = b.Gain(i) * ((b.Kind == 2) ? _particles : _emission);
			break;
		case "FireContinuousFx":
			if (b.Kind == 3)
			{
				role = _roles[r];
				gain = b.Gain(i) * _particles;
			}
			break;
		case "IceCrystalFx":
		case "DarkDrainFx":
			if (b.Kind == 2)
			{
				role = _roles[r];
				gain = b.Gain(i) * _particles;
			}
			break;
		case "LightRayFx":
			if (b.Kind == 3)
			{
				role = _roles[r];
				gain = b.Gain(i) * _particles;
			}
			break;
		case "PhysicalContactFx":
			role = ((batch == 0) ? Colors.White : _roles[Math.Clamp(r + 1, 1, 3)]);
			gain = b.Gain(i);
			break;
		case "PlayerContactFx":
			role = ((b.Kind == 0) ? _roles[r] : Colors.White);
			gain = b.Gain(i) * (b.Particle(i) ? _particles : _emission);
			break;
		case "LightningDischargeFx":
			gain = ((b.Kind == 2) ? _particles : _emission);
			break;
		case "HealContinuousFx":
			role = ((b.Kind == 0) ? Colors.White : _roles[r]);
			break;
		case "SystemOutcomeFx":
		{
			int kind = b.Kind;
			bool flag = ((kind == 0 || kind == 7) ? true : false);
			role = (flag ? _roles[Math.Clamp(r + 1, 1, 3)] : Colors.White);
			gain = b.Gain(i);
			break;
		}
		case "EarthStompSurfaceR2":
			role = ((b.Kind == 1) ? _roles[2] : Colors.White);
			gain = ((b.Kind >= 0) ? ((b.Kind == 1) ? (b.Gain(i) * _particles) : 1f) : ((e.Family == 0) ? 1.55f : 0.95f));
			break;
		case "VineSlashFx":
			gain = 1f;
			break;
		}
		return new Color(role.R * gain, role.G * gain, role.B * gain, Math.Clamp(b.Alpha(i), 0f, 1f));
	}

	private Vector3 WorldPoint(NativeBatch b, int i)
	{
		Vector3 p = b.Position(i);
		float scale = _height * _size;
		if (b.Space == 1)
		{
			return _feet + new Vector3(p.X, p.Y, p.Z) * scale + Vector3.Up * 0.009f;
		}
		if (b.Space == 2)
		{
			Vector3 anchor = b.Anchor(i);
			return _feet + anchor * scale + _basis * p * scale;
		}
		return _target + _basis * p * scale;
	}

	public void Sample(float elapsed)
	{
		if (_effect == null)
		{
			return;
		}
		double age = elapsed - _scenario.Windup;
		_effect.Sample(age);
		VertexCount = 0;
		float light = (RealLightsEnabled ? (Math.Max(0f, _effect.Light()) * _lightGain) : 0f);
		if (_effect.Shader == "BoundedContactFx")
		{
			light = 0f;
		}
		_targetLight.Position = _target;
		_targetLight.LightColor = _lightColor;
		_targetLight.LightEnergy = light * 1.6f * (_effect.TargetPeak / 0.35f);
		_floorLight.Position = _feet + Vector3.Up * (_height * 0.2f);
		_floorLight.LightColor = _lightColor;
		_floorLight.LightEnergy = ((QualityLevel < 2) ? (light * 0.48f * (_effect.FloorPeak / 0.12f)) : 0f);
		_targetLight.ShadowEnabled = QualityLevel == 0;
		_floorLight.ShadowEnabled = QualityLevel == 0;
		for (int j = 0; j < _effect.Batches.Length; j++)
		{
			NativeBatch b = _effect.Batches[j];
			Draw d = _draws[j];
			d.Mesh.ClearSurfaces();
			bool any = false;
			for (int k = 0; k < b.Count; k++)
			{
				if (b.Alpha(k) > 1E-05f)
				{
					any = true;
					break;
				}
			}
			d.Node.Visible = any;
			if (any)
			{
				float visualAge = _effect.VisualAge?.Invoke() ?? ((float)age);
				d.Material.SetShaderParameter("_Age", visualAge);
				d.Material.SetShaderParameter("_VisualAge", visualAge);
				if (_effect.Shader == "EarthStompSurfaceR2")
				{
					d.Material.SetShaderParameter("_Front", _effect.Front());
					d.Material.SetShaderParameter("_Coverage", (b.Kind < 0) ? _effect.Coverage() : 1f);
					d.Material.SetShaderParameter("_Kind", (b.Kind >= 0) ? ((b.Kind == 1) ? 1 : 2) : 0);
				}
				for (int index = 0; index < b.Count; index++)
				{
					d.Positions[index] = WorldPoint(b, index);
					d.Normals[index] = ((b.Space == 1) ? b.Normal(index) : _basis.Z);
					d.Uv[index] = b.Uv(index);
					d.Effect[index] = b.Effect(index);
					Color c = VertexColor(b, index, j);
					int k2 = index * 4;
					d.Hdr[k2] = c.R;
					d.Hdr[k2 + 1] = c.G;
					d.Hdr[k2 + 2] = c.B;
					d.Hdr[k2 + 3] = c.A;
				}
				d.Arrays[0] = d.Positions;
				d.Arrays[1] = d.Normals;
				d.Arrays[4] = d.Uv;
				d.Arrays[5] = d.Effect;
				d.Arrays[6] = d.Hdr;
				d.Arrays[12] = b.Indices;
				Mesh.ArrayFormat flags = (Mesh.ArrayFormat)67166208L;
				d.Mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, d.Arrays, null, null, flags);
				VertexCount += b.Count;
			}
		}
	}

	public void AppendSourceLights(List<ActorMaterialBinding.PointLight> lights)
	{
		if (IsActive && RealLightsEnabled && _effect != null)
		{
			float weight = Math.Max(0f, _effect.Light()) * _lightGain;
			Color linear = _lightColor.SrgbToLinear();
			Vector3 rgb = new Vector3(linear.R, linear.G, linear.B);
			if (weight * _effect.TargetPeak > 0f)
			{
				lights.Add(new ActorMaterialBinding.PointLight(_targetLight.Position, 7f, rgb * (40.5f * weight * _effect.TargetPeak), IsAttack: true));
			}
			if ((QualityLevel < 2 || AnalyticalFloorFallbackEnabled) && weight * _effect.FloorPeak > 0f)
			{
				lights.Add(new ActorMaterialBinding.PointLight(_floorLight.Position, 13f, rgb * (63f * weight * _effect.FloorPeak), IsAttack: true));
			}
		}
	}

	public void Clear()
	{
		foreach (Draw d in _draws)
		{
			d.Mesh.ClearSurfaces();
			d.Node.Visible = false;
		}
		if (_targetLight != null)
		{
			_targetLight.LightEnergy = 0f;
			_floorLight.LightEnergy = 0f;
		}
		_effect = null;
		base.Visible = false;
		VertexCount = 0;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		List<MethodInfo> methods = new List<MethodInfo>(4);
		methods.Add(new MethodInfo(MethodName._Ready, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.C, new PropertyInfo(Variant.Type.Vector4, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Color, "c", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.Sample, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Float, "elapsed", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName.Clear, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		return methods;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		if (method == MethodName._Ready && args.Count == 0)
		{
			_Ready();
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.C && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<Vector4>(C(VariantUtils.ConvertTo<Color>(in args[0])));
			return true;
		}
		if (method == MethodName.Sample && args.Count == 1)
		{
			Sample(VariantUtils.ConvertTo<float>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName.Clear && args.Count == 0)
		{
			Clear();
			ret = default(godot_variant);
			return true;
		}
		return base.InvokeGodotClassMethod(in method, args, out ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		if (method == MethodName.C && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<Vector4>(C(VariantUtils.ConvertTo<Color>(in args[0])));
			return true;
		}
		ret = default(godot_variant);
		return false;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if (method == MethodName._Ready)
		{
			return true;
		}
		if (method == MethodName.C)
		{
			return true;
		}
		if (method == MethodName.Sample)
		{
			return true;
		}
		if (method == MethodName.Clear)
		{
			return true;
		}
		return base.HasGodotClassMethod(in method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if (name == PropertyName.RealLightsEnabled)
		{
			RealLightsEnabled = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName.QualityLevel)
		{
			QualityLevel = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName.VertexCount)
		{
			VertexCount = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName._targetLight)
		{
			_targetLight = VariantUtils.ConvertTo<OmniLight3D>(in value);
			return true;
		}
		if (name == PropertyName._floorLight)
		{
			_floorLight = VariantUtils.ConvertTo<OmniLight3D>(in value);
			return true;
		}
		if (name == PropertyName._roles)
		{
			_roles = VariantUtils.ConvertTo<Color[]>(in value);
			return true;
		}
		if (name == PropertyName._lightColor)
		{
			_lightColor = VariantUtils.ConvertTo<Color>(in value);
			return true;
		}
		if (name == PropertyName._target)
		{
			_target = VariantUtils.ConvertTo<Vector3>(in value);
			return true;
		}
		if (name == PropertyName._feet)
		{
			_feet = VariantUtils.ConvertTo<Vector3>(in value);
			return true;
		}
		if (name == PropertyName._source)
		{
			_source = VariantUtils.ConvertTo<Vector3>(in value);
			return true;
		}
		if (name == PropertyName._basis)
		{
			_basis = VariantUtils.ConvertTo<Basis>(in value);
			return true;
		}
		if (name == PropertyName._height)
		{
			_height = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		if (name == PropertyName._size)
		{
			_size = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		if (name == PropertyName._emission)
		{
			_emission = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		if (name == PropertyName._particles)
		{
			_particles = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		if (name == PropertyName._lightGain)
		{
			_lightGain = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		return base.SetGodotClassPropertyValue(in name, in value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		bool from;
		if (name == PropertyName.RealLightsEnabled)
		{
			from = RealLightsEnabled;
			value = VariantUtils.CreateFrom(in from);
			return true;
		}
		int from2;
		if (name == PropertyName.QualityLevel)
		{
			from2 = QualityLevel;
			value = VariantUtils.CreateFrom(in from2);
			return true;
		}
		if (name == PropertyName.ActiveLights)
		{
			from2 = ActiveLights;
			value = VariantUtils.CreateFrom(in from2);
			return true;
		}
		if (name == PropertyName.VertexCount)
		{
			from2 = VertexCount;
			value = VariantUtils.CreateFrom(in from2);
			return true;
		}
		if (name == PropertyName.IsActive)
		{
			from = IsActive;
			value = VariantUtils.CreateFrom(in from);
			return true;
		}
		if (name == PropertyName.Renderer)
		{
			value = VariantUtils.CreateFrom<string>(Renderer);
			return true;
		}
		if (name == PropertyName._targetLight)
		{
			value = VariantUtils.CreateFrom(in _targetLight);
			return true;
		}
		if (name == PropertyName._floorLight)
		{
			value = VariantUtils.CreateFrom(in _floorLight);
			return true;
		}
		if (name == PropertyName._roles)
		{
			value = VariantUtils.CreateFrom(in _roles);
			return true;
		}
		if (name == PropertyName._lightColor)
		{
			value = VariantUtils.CreateFrom(in _lightColor);
			return true;
		}
		if (name == PropertyName._target)
		{
			value = VariantUtils.CreateFrom(in _target);
			return true;
		}
		if (name == PropertyName._feet)
		{
			value = VariantUtils.CreateFrom(in _feet);
			return true;
		}
		if (name == PropertyName._source)
		{
			value = VariantUtils.CreateFrom(in _source);
			return true;
		}
		if (name == PropertyName._basis)
		{
			value = VariantUtils.CreateFrom(in _basis);
			return true;
		}
		if (name == PropertyName._height)
		{
			value = VariantUtils.CreateFrom(in _height);
			return true;
		}
		if (name == PropertyName._size)
		{
			value = VariantUtils.CreateFrom(in _size);
			return true;
		}
		if (name == PropertyName._emission)
		{
			value = VariantUtils.CreateFrom(in _emission);
			return true;
		}
		if (name == PropertyName._particles)
		{
			value = VariantUtils.CreateFrom(in _particles);
			return true;
		}
		if (name == PropertyName._lightGain)
		{
			value = VariantUtils.CreateFrom(in _lightGain);
			return true;
		}
		return base.GetGodotClassPropertyValue(in name, out value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		List<PropertyInfo> properties = new List<PropertyInfo>();
		properties.Add(new PropertyInfo(Variant.Type.Object, PropertyName._targetLight, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Object, PropertyName._floorLight, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.PackedColorArray, PropertyName._roles, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Color, PropertyName._lightColor, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Vector3, PropertyName._target, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Vector3, PropertyName._feet, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Vector3, PropertyName._source, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Basis, PropertyName._basis, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Float, PropertyName._height, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Float, PropertyName._size, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Float, PropertyName._emission, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Float, PropertyName._particles, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Float, PropertyName._lightGain, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName.RealLightsEnabled, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName.QualityLevel, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName.ActiveLights, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName.VertexCount, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Bool, PropertyName.IsActive, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.String, PropertyName.Renderer, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		return properties;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		base.SaveGodotObjectData(info);
		info.AddProperty(PropertyName.RealLightsEnabled, Variant.From<bool>(RealLightsEnabled));
		info.AddProperty(PropertyName.QualityLevel, Variant.From<int>(QualityLevel));
		info.AddProperty(PropertyName.VertexCount, Variant.From<int>(VertexCount));
		info.AddProperty(PropertyName._targetLight, Variant.From(in _targetLight));
		info.AddProperty(PropertyName._floorLight, Variant.From(in _floorLight));
		info.AddProperty(PropertyName._roles, Variant.From(in _roles));
		info.AddProperty(PropertyName._lightColor, Variant.From(in _lightColor));
		info.AddProperty(PropertyName._target, Variant.From(in _target));
		info.AddProperty(PropertyName._feet, Variant.From(in _feet));
		info.AddProperty(PropertyName._source, Variant.From(in _source));
		info.AddProperty(PropertyName._basis, Variant.From(in _basis));
		info.AddProperty(PropertyName._height, Variant.From(in _height));
		info.AddProperty(PropertyName._size, Variant.From(in _size));
		info.AddProperty(PropertyName._emission, Variant.From(in _emission));
		info.AddProperty(PropertyName._particles, Variant.From(in _particles));
		info.AddProperty(PropertyName._lightGain, Variant.From(in _lightGain));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		base.RestoreGodotObjectData(info);
		if (info.TryGetProperty(PropertyName.RealLightsEnabled, out var _value_RealLightsEnabled))
		{
			RealLightsEnabled = _value_RealLightsEnabled.As<bool>();
		}
		if (info.TryGetProperty(PropertyName.QualityLevel, out var _value_QualityLevel))
		{
			QualityLevel = _value_QualityLevel.As<int>();
		}
		if (info.TryGetProperty(PropertyName.VertexCount, out var _value_VertexCount))
		{
			VertexCount = _value_VertexCount.As<int>();
		}
		if (info.TryGetProperty(PropertyName._targetLight, out var _value__targetLight))
		{
			_targetLight = _value__targetLight.As<OmniLight3D>();
		}
		if (info.TryGetProperty(PropertyName._floorLight, out var _value__floorLight))
		{
			_floorLight = _value__floorLight.As<OmniLight3D>();
		}
		if (info.TryGetProperty(PropertyName._roles, out var _value__roles))
		{
			_roles = _value__roles.As<Color[]>();
		}
		if (info.TryGetProperty(PropertyName._lightColor, out var _value__lightColor))
		{
			_lightColor = _value__lightColor.As<Color>();
		}
		if (info.TryGetProperty(PropertyName._target, out var _value__target))
		{
			_target = _value__target.As<Vector3>();
		}
		if (info.TryGetProperty(PropertyName._feet, out var _value__feet))
		{
			_feet = _value__feet.As<Vector3>();
		}
		if (info.TryGetProperty(PropertyName._source, out var _value__source))
		{
			_source = _value__source.As<Vector3>();
		}
		if (info.TryGetProperty(PropertyName._basis, out var _value__basis))
		{
			_basis = _value__basis.As<Basis>();
		}
		if (info.TryGetProperty(PropertyName._height, out var _value__height))
		{
			_height = _value__height.As<float>();
		}
		if (info.TryGetProperty(PropertyName._size, out var _value__size))
		{
			_size = _value__size.As<float>();
		}
		if (info.TryGetProperty(PropertyName._emission, out var _value__emission))
		{
			_emission = _value__emission.As<float>();
		}
		if (info.TryGetProperty(PropertyName._particles, out var _value__particles))
		{
			_particles = _value__particles.As<float>();
		}
		if (info.TryGetProperty(PropertyName._lightGain, out var _value__lightGain))
		{
			_lightGain = _value__lightGain.As<float>();
		}
	}
}

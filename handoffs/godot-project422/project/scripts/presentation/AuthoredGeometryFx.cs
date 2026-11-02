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

		public MeshInstance3D Node;

		public ShaderMaterial Material;
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

	private EffectScenario _scenario;

	private OmniLight3D _targetLight;
    private bool _lightScatterCommitted;

	private OmniLight3D _floorLight;

	private Color[] _roles = new Color[4];

	private Color _lightColor;

	private Vector3 _target;
    private bool _prototypeContactAnchorLocked;
    internal Vector3 PrototypeContactPoint => _target;

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

	public bool IsActive
	{
		get
		{
			if (_effect != null)
			{
				return Visible;
			}
			return false;
		}
	}

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
		Visible = false;
	}

	public bool Begin(AttackRoute route, EffectScenario scenario, Vector3 source, Vector3 target, Vector3 feet, float visibleHeight, Basis cameraBasis, int contactIndex = 0, float initialElapsed = 0f, bool mirrorCommittedContact = false, bool impSparkCommittedContact = false, bool coldSweepCommittedContact = false, bool canopyCommittedContact = false, bool lightBoltCommittedContact = false, bool lightBurstCommittedContact = false, bool vineFormationCounterCommittedContact = false, bool aetherLanceCommittedContact = false, bool sparkBurstCommittedContact = false, bool momentumWhipCommittedContact = false, bool cultistDarkBladeCommittedContact = false, bool waterWideOwnedContact = false, bool waterWideCommittedContact = false, bool lightScatterCommittedContact = false, bool blueCommittedContact = false, bool greenGulpCommittedContact = false, bool neutralPressureOwnedContact = false, bool neutralPressureCommittedContact = false, bool spore385CommittedContact = false, bool earthRoarOwnedContact = false, bool earthRoarCommittedContact = false, bool supportLightOwnedContact = false, bool supportLightCommittedContact = false, bool greenPhysicalOwnedContact = false, bool greenPhysicalCommittedContact = false, bool whiteLight396OwnedContact = false, bool whiteLight396CommittedContact = false, bool rootingVine404OwnedContact = false, bool rootingVine404CommittedContact = false, bool legacyContact419Owned = false, bool legacyContact419Projected = false)
	{
		Clear();
		_route = route;
		_scenario = scenario;
        SetWaterWideContactState(waterWideOwnedContact,waterWideCommittedContact);
        SetEarthRoarContactState(owned: earthRoarOwnedContact, committed: earthRoarCommittedContact);
        SetSupportLightState(owned: supportLightOwnedContact, committed: supportLightCommittedContact);
        SetNeutralPressureContactState(owned: neutralPressureOwnedContact, committed: neutralPressureCommittedContact);
        SetGreenPhysicalContactState(owned: greenPhysicalOwnedContact, committed: greenPhysicalCommittedContact);
        SetWhiteLight396ContactState(owned: whiteLight396OwnedContact, committed: whiteLight396CommittedContact);
        SetRootingVine404ContactState(owned: rootingVine404OwnedContact, committed: rootingVine404CommittedContact);
        SetLegacyContact419State(owned: legacyContact419Owned, projected: legacyContact419Projected);
		string key = ((SporeContactVfx385.SelectedRoute(route) || BlueContactVfx379.SelectedRoute(route) || GreenGulpContactVfxProfiles.SelectedRoute(route) || HolyMaulHealReceiveVfxProfiles.SelectedRoute(route) || HolyMaulReceiveVfxProfiles.SelectedRoute(route) || CultistDarkBladeContactVfxProfiles.SelectedRoute(route) || MomentumWhipContactVfxProfiles.SelectedRoute(route) || SparkBurstContactVfxProfiles.SelectedRoute(route) || AetherLanceContactVfxProfiles.SelectedRoute(route) || VineFormationCounterVfxProfiles.SelectedRoute(route) || DoubleLashReceiveVfxProfiles.SelectedRoute(route) || VineGraspContactVfxProfiles.SelectedRoute(route) || TripleHornReceiveVfxProfiles.SelectedRoute(route) || LightBurstContactVfxProfiles.SelectedRoute(route) || CrossbowAimedContactVfxProfiles.SelectedRoute(route) || WraithCurseContactVfxProfiles.SelectedRoute(route) || LightBoltContactVfxProfiles.SelectedRoute(route) || ColdSweepContactVfxProfiles.SelectedRoute(route) || ImpSparkContactVfxProfiles.SelectedRoute(route) || MirrorContactVfxProfiles.SelectedRoute(route) || NativeGeometryBridge.IsExactMaelstromRoute(route) || Hd2dAttackPrototype.SupportsRoute(route) || EnemyContactVfxProfiles.SelectedRoute(route) || PermanentContactVfxProfiles.SelectedRoute(route) || StatusContactVfxProfiles.SelectedRoute(route) || NecroContactVfxProfiles.SelectedRoute(route) || ReactionContactVfxProfiles.SelectedRoute(route) || LightningContactVfxProfiles.SelectedRoute(route) || GearContactVfxProfiles.SelectedRoute(route) || GearKegContactVfxProfiles.SelectedRoute(route) || NamelessPowderContactVfxProfiles.SelectedRoute(route) || NamelessKegContactVfxProfiles.SelectedRoute(route)) ? AttackRouteCatalog.Identity(route) + "/" : "") + route.Renderer + "/" + route.Family + "/" + scenario.Tier;
        // Support factories differ within prayer_heal and ice_guard_gain. Keep their
        // exact route identity in the pooled native cache, not just shared family.
        if(route.Renderer=="Hd2dSupportFx")
            key="support-owned/"+AttackRouteCatalog.Identity(route)+"/"+key;
        //Owned recovery captures silhouette compensation at creation; small/full may share scenario tier.
        if(HolyMaulHealReceiveVfxProfiles.SelectedRoute(route))
            key="holy-maul-owned-heal/"+(route.FixedSmall?"small/":"full/")+key;
        //397 exact material isolation; never reuse its geometry for another WarmSpark owner.
        if(NativeGeometryBridge.WarmSpark397Route(route))key="warmspark397/"+AttackRouteCatalog.Identity(route)+"/"+key;
        if(NativeGeometryBridge.ObjectContact415Route(route))key="object415/"+AttackRouteCatalog.Identity(route)+"/"+key;
		if (!_cache.TryGetValue(key, out var effect))
		{
			effect = NativeGeometryBridge.Create(route, scenario.Tier);
			if (effect == null)
			{
				return false;
			}
			_cache[key] = effect;
		}
		_effect = effect;
        SetBlueCommittedContact(blueCommittedContact);
        SetMirrorCommittedContact(mirrorCommittedContact);
        SetImpSparkCommittedContact(impSparkCommittedContact);
        SetColdSweepCommittedContact(coldSweepCommittedContact);
        SetCanopyCommittedContact(canopyCommittedContact);
        SetLightBoltCommittedContact(lightBoltCommittedContact);
        SetLightScatterCommittedContact(lightScatterCommittedContact);
        SetLightBurstCommittedContact(lightBurstCommittedContact);
        SetVineFormationCounterCommittedContact(vineFormationCounterCommittedContact);
        SetAetherLanceCommittedContact(aetherLanceCommittedContact);
        SetSparkBurstCommittedContact(sparkBurstCommittedContact);
        SetMomentumWhipCommittedContact(momentumWhipCommittedContact);
        SetCultistDarkBladeCommittedContact(cultistDarkBladeCommittedContact);
        SetGreenGulpCommittedContact(greenGulpCommittedContact);
        SetSpore385CommittedContact(spore385CommittedContact);
		_source = source;
		_target = target;
        _prototypeContactAnchorLocked=false;
		_feet = feet;
		_basis = cameraBasis;
		_height = Math.Max(0.25f, visibleHeight);
		bool small = scenario.Tier == EffectTier.Small;
		bool large = scenario.Tier == EffectTier.Large;
		ref float size = ref _size;
		float num;
		if (small)
		{
			num = 0.88f;
		}
		else
		{
			num = (large ? 1.12f : 1f);
		}
		size = num;
		ref float emission = ref _emission;
		float num2;
		if (small)
		{
			num2 = 0.72f;
		}
		else
		{
			num2 = (large ? 1.18f : 1f);
		}
		emission = num2;
		ref float particles = ref _particles;
		float num3;
		if (small)
		{
			num3 = 0.65f;
		}
		else
		{
			num3 = (large ? 1.15f : 1f);
		}
		particles = num3;
		ref float lightGain = ref _lightGain;
		float num4;
		if (small)
		{
			num4 = 0.65f;
		}
		else
		{
			num4 = (large ? 1.2f : 1f);
		}
		lightGain = num4;
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
        if(route.Renderer=="Hd2dSupportFx")
        {
            _size=1f;_emission=1f;_particles=1f;_lightGain=1f;
            _target=_feet+_basis.Y*(_height*.48f);
        }
		effect.ConfigureMirror?.Invoke(SourceAttackOrientation.AlternateHit(contactIndex));
		effect.ConfigureParticleGain?.Invoke(_particles);
        effect.ConfigureRemoteStrike?.Invoke(scenario.PrototypeRemoteStrike);
        effect.ConfigureSourceGesture?.Invoke(!scenario.SuppressSourceGesture);
        effect.ConfigureVisualSeed?.Invoke(feet.X*1.71f+feet.Z*2.13f+contactIndex*1.39f);
        if(effect.ConfigureResolvedPath!=null)
        {
            var points=scenario.PrototypeResolvedPath;var times=scenario.PrototypeResolvedOffsets;
            if(points!=null && times!=null && points.Length==times.Length && points.Length>0)
            {
                var localPoints=new Vector3[points.Length];var localTimes=new float[times.Length];
                for(int i=0;i<points.Length;i++) { localPoints[i]=_basis.Inverse()*(points[i]-target)/(_height*_size);localTimes[i]=times[i]; }
                effect.ConfigureResolvedPath(localPoints,localTimes);
            }
            else effect.ConfigureResolvedPath(System.Array.Empty<Vector3>(),System.Array.Empty<float>());
        }
        if(scenario.PrototypeFixedSource)effect.ConfigureWeaponFrame?.Invoke(2);
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
		Visible = true;
		Sample(initialElapsed);
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
		if (e.Shader == "BoundedContactFx" || e.Shader == "ColdSweepSurfaceFx")
		{
			string[] surfaces = new string[8] { "Neutral", "Water", "Cold", "Wood", "Plant", "Fire", "Smoke", "Dark" };
			JToken surface = obj["surfaces"]?[surfaces[e.Family]];
			_roles[0] = new Color("444444").SrgbToLinear();
			_roles[1] = new Color(surface?["primary"]?.Value<string>() ?? "#A8A8A8").SrgbToLinear();
			_roles[2] = _roles[1];
			_roles[3] = new Color(surface?["core"]?.Value<string>() ?? "#EEEEEE").SrgbToLinear();
			_lightColor = Colors.White;
			return;
		}
		string[] roles = new string[4] { "shadow", "primary", "edge", "core" };
		for (int i = 0; i < 4; i++)
		{
			Color[] roles2 = _roles;
			int num = i;
			string text = obj["roles_srgb"]?[roles[i]]?.Value<string>();
			if (text == null)
			{
				text = i switch
				{
					2 => "#DEDCD1", 
					1 => "#C6C6BE", 
					0 => "#535E69", 
					_ => "#F2EEE4", 
				};
			}
			roles2[num] = new Color(text).SrgbToLinear();
		}
		_lightColor = new Color(obj["scene_light_srgb"]?.Value<string>() ?? "#C6C6BE");
	}

	private static Vector4 C(Color c)
	{
		return new Vector4(c.R, c.G, c.B, c.A);
	}

    // A local material opt-in, never a shared Kind33 or Light-family replacement.
    internal static bool UsesTorrentOpticalMaterial(AttackRoute? route,NativeEffect? effect,NativeBatch? batch)
        =>route is not null && effect is not null && batch is not null &&
          route.Key=="card:white_light_torrent" && route.DefinitionId=="white_light_torrent" &&
          route.SourceKind=="card" && route.Family=="light" && route.Renderer=="LightRayFx" &&
          route.Element=="light" && !route.FixedSmall &&
          effect.Shader=="Hd2dPrototype" && effect.Palette=="hd2d-light" &&
          batch.Kind==33 && batch.Space==0 && batch.Texture=="hd2d-light-torrent-density-v1";

    // Exact necro material opt-in; normal hand-play Kiss and shared Dark stay on the original shader.
    internal static bool UsesCorpseKissDensityMaterial(AttackRoute? route,NativeEffect? effect,NativeBatch? batch)
        =>route is not null && effect is not null && batch is not null &&
          route.Key=="card:black_corpse_kiss" && route.DefinitionId=="black_corpse_kiss" &&
          route.SourceKind=="necro" && route.Family=="dark_drain" &&
          route.Renderer=="Hd2dNecroContactFx" && route.Element=="dark" && route.FixedSmall &&
          effect.Shader=="Hd2dPrototype" && effect.Palette=="hd2d-dark" &&
          batch.Kind==16 && batch.Space==0 && batch.Texture=="hd2d-necro-kiss-density-v1";

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
        if (BlueContactVfx379.SelectedRoute(_route) && e.Shader == "BoundedContactFx" && b.Kind == 91)
        {
            shader = "BlueContact379Surface";
        }
        if(MomentumWhipContactVfxProfiles.SelectedRoute(_route)&&e.Shader=="BoundedContactFx"&&b.Kind==71&&b.Space==0)shader="MomentumWhipContactSurface";
        if(GreenGulpContactVfxProfiles.SelectedRoute(_route)&&(b.Kind==91||b.Kind==92)&&b.Space==0)shader="GreenGulpContactSurface";
        if(SporeContactVfx385.SelectedRoute(_route)&&b.Kind==101&&b.Space==0)shader="Spore385ContactSurface";
        if(CultistDarkBladeContactVfxProfiles.SelectedRoute(_route)&&e.Shader=="BoundedContactFx"&&b.Kind==81&&b.Space==0)shader="CultistDarkBladeContactSurface";
        if(SparkBurstContactVfxProfiles.SelectedRoute(_route)&&e.Shader=="ScatterProfileFx"&&b.Kind==61&&b.Space==0)shader="SparkBurstContactSurface";
        if(AetherLanceContactVfxProfiles.SelectedRoute(_route)&&e.Shader=="BoundedContactFx"&&b.Kind==51&&b.Space==0)shader="AetherLanceContactSurface";
        if(VineFormationCounterVfxProfiles.SelectedRoute(_route)&&e.Shader=="BoundedContactFx"&&b.Kind==41&&b.Space==0)shader="VineFormationCounterSurface";
        //415 only these exact owners use the neutral contrast surface; generic
        //BoundedContact, private Crossbow and all other object aliases retain theirs.
        if(NativeGeometryBridge.ObjectContact415Route(_route)&&e.Shader=="BoundedContactFx"&&b.Space==0)
            shader="ObjectContact415Surface";
        if(CrossbowAimedContactVfxProfiles.SelectedRoute(_route)&&e.Shader=="BoundedContactFx"&&e.Palette=="bounded-contact-palette-r1"&&b.Kind==0&&b.Space==0)shader="CrossbowAimedSurface";
        if(VineGraspContactVfxProfiles.SelectedRoute(_route)&&e.Shader=="BoundedContactFx"&&e.Palette=="bounded-contact-palette-r1"&&b.Kind==0&&b.Space==0)shader="VineGraspSurface";
        if(WraithCurseContactVfxProfiles.SelectedRoute(_route)&&e.Shader=="BoundedContactFx"&&e.Palette=="bounded-contact-palette-r1"&&b.Kind==0&&b.Space==0)shader="WraithCurseSurface";
        if(LightBurstContactVfxProfiles.SelectedRoute(_route)&&e.Shader=="LocalMaterialProfileFx"&&e.Palette=="local-material-palette-r1"&&b.Space==0&&(b.Kind==41||b.Kind==42))shader="LightBurstOpticalSurface";
        if(LightBoltContactVfxProfiles.SelectedRoute(_route)&&e.Shader=="Hd2dPrototype"&&e.Palette=="hd2d-light"&&b.Space==0&&(b.Kind==41||b.Kind==42))shader="LightBoltOpticalSurface";
        if(PermanentContactVfxProfiles.CanopyRoute(_route)&&e.Shader=="Hd2dPrototype"&&e.Palette=="hd2d-vegetal-cut"&&b.Kind==40&&b.Space==0)shader="Hd2dCanopySurface";
        if(UsesTorrentOpticalMaterial(_route,e,b))shader="Hd2dOpticalFlow";
        if(UsesCorpseKissDensityMaterial(_route,e,b))shader="Hd2dKissDensity";
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
		float num;
		if (e.Shader == "VineSlashFx")
		{
			num = index switch
			{
				2 => 3.8f * _emission, 
				1 => 1.8f * _emission, 
				0 => 2.25f * _emission, 
				_ => 2.2f * _particles, 
			};
		}
		else if (e.Shader == "PhysicalContactFx")
		{
			num = ((index == 2) ? _particles : _emission);
		}
		else
		{
			num = 1f;
		}
		float gain = num;
		float emission;
		if (e.Shader == "EarthStompSurfaceR2")
		{
			if (b.Kind < 0)
			{
				emission = _emission;
			}
			else
			{
				emission = ((b.Kind == 0) ? _particles : 1f);
			}
		}
		else
		{
			emission = _emission;
		}
		material.SetShaderParameter("_Gain", gain);
		material.SetShaderParameter("_Emission", emission);
		material.SetShaderParameter("_Coverage", 1f);
		string texture = b.Texture.Length > 0 ? b.Texture : e.Texture;
        if (e.Shader == "Hd2dPrototype") { material.SetShaderParameter("_TextureEnabled", texture.Length > 0); material.SetShaderParameter("_AuthoredRgba", b.Texture.Length > 0); material.SetShaderParameter("_TextureMotion", AxialFamilyContactVfxProfiles.Selected(_route)&&b.Texture=="physical-pierce-surface-r1" ? 33f : b.Texture=="lightning-discharge-surface-r2" ? 32f : b.Texture.StartsWith("hd2d-wood-contact-") ? 31f : b.Texture.StartsWith("hd2d-waterwide-phase-") ? 30f : b.Texture=="earth-ground-grain-r2" ? 28f : b.Texture.StartsWith("hd2d-dark-contraction-") ? 25f : b.Texture=="ice-crystal-surface-r2" ? (b.Kind==11?21f:b.Kind==12?22f:0f) : b.Texture.StartsWith("hd2d-water-impact-") ? (b.Texture.EndsWith("03")?20f:18f) : b.Texture=="water-flow-surface-r1" ? 19f : b.Texture.StartsWith("hd2d-stomp-aura-") ? 17f : b.Texture=="hd2d-stomp-pressure-v1" ? 14f : b.Texture.StartsWith("hd2d-fire-volume-") ? (b.Kind==15?23f:(b.Texture.EndsWith("03") || b.Texture.EndsWith("04")) ? 13f : 12f) : b.Texture.StartsWith("hd2d-fire-projectile-") ? 1f : b.Texture == "hd2d-stomp-shin-v4" ? 9f : b.Texture == "hd2d-fire-impact-v1" ? 2f : b.Texture == "hd2d-earth-rock-v1" ? 10f : b.Texture == "hd2d-earth-impact-v1" ? 3f : b.Texture == "hd2d-earth-dust-v1" ? 4f : b.Texture == "hd2d-physical-slash-v1" ? (b.Kind==17?26f:b.Kind==18?27f:b.Kind==9?16f:15f) : b.Texture == "hd2d-earth-foot-v1" ? 6f : (b.Texture == "hd2d-fire-impact-phase-03" || b.Texture == "hd2d-fire-impact-phase-04") ? 8f : b.Texture.StartsWith("hd2d-fire-impact-phase-") ? 7f : 0f); }
		if (e.Shader == "PlayerContactFx")
		{
			if (b.Kind == 1)
			{
				texture = "physical-pierce-surface-r1";
			}
			else
			{
				texture = ((b.Kind == 2) ? "physical-meteor-intact-r1" : "enemy-blunt-contact-r1");
			}
		}
		if (BlueContactVfx379.SelectedRoute(_route) && texture == "hd2d-ice-wall-density-v1")
        {
            material.SetShaderParameter("_BaseMap", BlueContactVfx379.IceDensityTexture());
        }
        else if (texture.Length > 0 && ResourceLoader.Exists("res://assets/Art/fx/" + texture + ".png"))
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
		ShaderMaterial shaderMaterial = material;
		StringName param = "_UseTexture";
		float num2;
		if (e.Shader == "PhysicalSlashFx")
		{
			num2 = ((b.Kind == 0) ? 1f : 0f);
		}
		else if (e.Shader == "VineSlashFx")
		{
			num2 = ((index < 2) ? 1f : 0f);
		}
		else
		{
			num2 = ((index == 0) ? 1f : 0f);
		}
		shaderMaterial.SetShaderParameter(param, b.Texture.Length > 0 ? 1f : num2);
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
        case "Hd2dPrototype":
            role = _roles[r];
            break;
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
		case "DarkDrainFx":
		case "IceCrystalFx":
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
			role = ((kind == 0 || kind == 7) ? _roles[Math.Clamp(r + 1, 1, 3)] : Colors.White);
			gain = b.Gain(i);
			break;
		}
		case "EarthStompSurfaceR2":
			role = ((b.Kind == 1) ? _roles[2] : Colors.White);
			if (b.Kind < 0)
			{
				gain = ((e.Family == 0) ? 1.55f : 0.95f);
			}
			else
			{
				gain = ((b.Kind == 1) ? (b.Gain(i) * _particles) : 1f);
			}
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

    public void UpdateSupportReceiver(Vector3 feet,float height)
    {
        if(_route.Renderer!="Hd2dSupportFx" || _effect==null)return;
        _feet=feet;_height=Math.Max(.25f,height);_target=feet+_basis.Y*(_height*.48f);
    }

    public float PrototypeStageFocus => IsActive && _effect?.Shader=="Hd2dPrototype" ? Math.Clamp(_effect.StageFocus(),0,1) : 0;

    public float PrototypeSpectacleCrest
    {
        get
        {
            if(!IsActive || _effect?.Shader!="Hd2dPrototype" || _route.Renderer=="Hd2dSupportFx")return 0;
            float age=_effect.VisualAge?.Invoke()??-1;
            if(age<0)return 0;
            float release=Math.Clamp((age-.055f)/.135f,0f,1f);
            return 1-release*release*(3-2*release);
        }
    }

    public float PrototypeWarmCrest
    {
        get
        {
            if(!IsActive || _effect?.Shader!="Hd2dPrototype" || !Hd2dAttackPrototype.UsesFireSource(_route))return 0;
            float age=_effect.VisualAge?.Invoke()??-1;
            return Math.Clamp((age-.015f)/.020f,0,1)*Math.Clamp((.140f-age)/.030f,0,1);
        }
    }

    public Vector3 PrototypeFocusPosition => _feet;
    public void ConfigurePrototypeViewport(Camera3D camera)
    {
        if(_effect?.Shader!="Hd2dPrototype" || _route.Key!="card:green_finisher_stomp")return;
        Vector2 size=camera.GetViewport().GetVisibleRect().Size;
        float depth=Math.Max(.01f,(camera.GlobalPosition-_feet).Dot(camera.GlobalBasis.Z));
        Vector2 projected=camera.UnprojectPosition(_feet);
        Vector3 top=camera.ProjectPosition(new Vector2(projected.X,-size.Y*.12f),depth);
        _effect.ConfigureStompTop?.Invoke((_basis.Inverse()*(top-_feet)).Y/(_height*_size));
    }


    public void UpdatePrototypeSource(Vector3 point, int poseFrame, Vector3? weaponOrigin = null, float? weaponTraceLength = null, float? effectElapsed = null, Vector3? targetNearSurface = null)
    {
        if(_effect?.Shader!="Hd2dPrototype") return;
        if(_scenario.PrototypeFixedSource)
        {
            // The transfer belongs to the frozen prior contact, not the retreating hero.
            _effect.ConfigureWeaponFrame?.Invoke(2);
            return;
        }
        if(Hd2dAttackPrototype.UsesCastSource(_route))
        {
            // Track the held focus while gathering, then freeze launch origin for continuous flight.
            if((_effect.VisualAge?.Invoke()??0)>=-(ElementalVfxProfiles.ForRoute(_route.Key)?.LaunchLead??.17f))return;
        }
        else if(!Hd2dAttackPrototype.UsesWeaponSource(_route))return;
        if(Hd2dAttackPrototype.UsesWeaponSource(_route) && !_scenario.PrototypeRemoteStrike && !_prototypeContactAnchorLocked
            && effectElapsed.HasValue && effectElapsed.Value>=_scenario.Windup)
        {
            // First contact belongs to the reachable near-side surface, not the
            // center of a large enemy. Freeze it: the burst must not chase retreat.
            float nearSide=(_basis.Inverse()*(point-_target)).X;
            float bound=targetNearSurface.HasValue?Math.Min(0,(_basis.Inverse()*(targetNearSurface.Value-_target)).X-.04f*_height):-.45f*_height;
            _target+=_basis.X*Math.Clamp(nearSide,bound,0f);
            _prototypeContactAnchorLocked=true;
        }
        _effect.ConfigureWeaponFrame?.Invoke(poseFrame);
        if(weaponTraceLength.HasValue) _effect.ConfigureWeaponTraceLength?.Invoke(weaponTraceLength.Value/(_height*_size));
        if(weaponOrigin.HasValue) _effect.ConfigureWeaponOrigin?.Invoke(_basis.Inverse()*(weaponOrigin.Value-_target)/(_height*_size));
        _source=point;
        _effect.ConfigureSource?.Invoke(_basis.Inverse()*(point-_target)/(_height*_size));
    }

    private bool _neutralPressureContactScope;
    private bool _neutralPressureCurrentOwner;
    private bool _neutralPressureCommittedContact;
    internal void SetNeutralPressureContactState(bool owned, bool committed)
    {
        bool eligible = NeutralPressureContactVfxProfiles.Eligible(_route);
        //Scope is latched only within this Begin. Losing current ownership suppresses light,
        //never restores the historical broad pool. Negative authored anticipation is unchanged
        //while current owner remains valid; positive light additionally requires exact commit.
        _neutralPressureContactScope = eligible && (_neutralPressureContactScope || owned);
        _neutralPressureCurrentOwner = eligible && owned;
        _neutralPressureCommittedContact = _neutralPressureCurrentOwner && committed;
    }

    private bool _supportLightScope;
    private bool _supportLightCurrentOwner;
    private bool _supportLightCommitted;
    internal void SetSupportLightState(bool owned, bool committed)
    {
        bool eligible = SupportLightVfx391.Eligible(_route);
        // Remember this lifetime's scope so revocation suppresses rather than leaks old light.
        _supportLightScope = eligible && (_supportLightScope || owned);
        _supportLightCurrentOwner = eligible && owned;
        _supportLightCommitted = _supportLightCurrentOwner && committed;
    }

    private bool _legacyContact419Scope;
    private bool _legacyContact419CurrentOwner;
    private bool _legacyContact419Projected;
    private float _legacyContact419SampleAge = float.NaN;
    internal void SetLegacyContact419State(bool owned, bool projected)
    {
        bool eligible = LegacyContactVfx419.Eligible(_route);
        _legacyContact419Scope = eligible && (_legacyContact419Scope || owned);
        _legacyContact419CurrentOwner = eligible && owned;
        _legacyContact419Projected = _legacyContact419CurrentOwner && projected;
    }

    private bool _greenPhysicalContactScope;
    private bool _greenPhysicalCurrentOwner;
    private bool _greenPhysicalCommittedContact;
    internal void SetGreenPhysicalContactState(bool owned, bool committed)
    {
        bool eligible = GreenPhysicalContactVfxProfiles.Eligible(_route);
        //Scope is latched only within this Begin. Losing current ownership suppresses light,
        //never restores the historical broad pool. Negative authored anticipation is unchanged
        //while current owner remains valid; positive light additionally requires exact commit.
        _greenPhysicalContactScope = eligible && (_greenPhysicalContactScope || owned);
        _greenPhysicalCurrentOwner = eligible && owned;
        _greenPhysicalCommittedContact = _greenPhysicalCurrentOwner && committed;
    }

    private bool _whiteLight396ContactScope;
    private bool _whiteLight396CurrentOwner;
    private bool _whiteLight396CommittedContact;
    internal void SetWhiteLight396ContactState(bool owned, bool committed)
    {
        bool eligible = WhiteLight396ContactVfxProfiles.Eligible(_route);
        //Scope is latched only within this Begin. Losing current ownership suppresses light,
        //never restores the historical broad pool. Negative authored anticipation is unchanged
        //while current owner remains valid; positive light additionally requires exact commit.
        _whiteLight396ContactScope = eligible && (_whiteLight396ContactScope || owned);
        _whiteLight396CurrentOwner = eligible && owned;
        _whiteLight396CommittedContact = _whiteLight396CurrentOwner && committed;
    }

    private bool _rootingVine404ContactScope;
    private bool _rootingVine404CurrentOwner;
    private bool _rootingVine404CommittedContact;
    internal void SetRootingVine404ContactState(bool owned, bool committed)
    {
        bool eligible = RootingVine404ContactVfxProfiles.Eligible(_route);
        //Scope is latched only within this Begin. Losing current ownership suppresses light,
        //never restores the historical broad pool. Negative authored anticipation is unchanged
        //while current owner remains valid; positive light additionally requires exact commit.
        _rootingVine404ContactScope = eligible && (_rootingVine404ContactScope || owned);
        _rootingVine404CurrentOwner = eligible && owned;
        _rootingVine404CommittedContact = _rootingVine404CurrentOwner && committed;
    }

    private bool _earthRoarContactScope;
    private bool _earthRoarCurrentOwner;
    private bool _earthRoarCommittedContact;
    internal void SetEarthRoarContactState(bool owned, bool committed)
    {
        bool eligible = EarthRoarContactVfxProfiles.Eligible(_route);
        //Scope is latched only within this Begin. Losing current ownership suppresses light,
        //never restores the historical broad pool. Negative authored anticipation is unchanged
        //while current owner remains valid; positive light additionally requires exact commit.
        _earthRoarContactScope = eligible && (_earthRoarContactScope || owned);
        _earthRoarCurrentOwner = eligible && owned;
        _earthRoarCommittedContact = _earthRoarCurrentOwner && committed;
    }

    private bool _waterWideContactScope, _waterWideCurrentOwner, _waterWideCommittedContact;
    internal void SetWaterWideContactState(bool owned,bool committed)
    {
        // Latch only the per-Begin scoped-lighting classification. Current authorization
        // is refreshed every beat; revocation must suppress, never restore the old flood.
        bool eligible=WaterWideContactVfxProfiles.Eligible(_route);
        _waterWideContactScope=eligible&&(_waterWideContactScope||owned);
        _waterWideCurrentOwner=eligible&&owned;
        _waterWideCommittedContact=_waterWideCurrentOwner&&committed;
    }
    internal void SetSpore385CommittedContact(bool committed) =>_effect?.ConfigureSpore385CommittedContact?.Invoke(SporeContactVfx385.SelectedRoute(_route)&&committed);
    internal void SetGreenGulpCommittedContact(bool committed) =>_effect?.ConfigureGreenGulpCommittedContact?.Invoke(GreenGulpContactVfxProfiles.SelectedRoute(_route)&&committed);
    internal void SetCultistDarkBladeCommittedContact(bool committed)
        =>_effect?.ConfigureCultistDarkBladeCommittedContact?.Invoke(CultistDarkBladeContactVfxProfiles.SelectedRoute(_route)&&committed);

    internal void SetMomentumWhipCommittedContact(bool committed)
        =>_effect?.ConfigureMomentumWhipCommittedContact?.Invoke(MomentumWhipContactVfxProfiles.SelectedRoute(_route)&&committed);

    internal void SetSparkBurstCommittedContact(bool committed)
        =>_effect?.ConfigureSparkBurstCommittedContact?.Invoke(SparkBurstContactVfxProfiles.SelectedRoute(_route)&&committed);

    internal void SetBlueCommittedContact(bool committed)
    {
        _effect?.ConfigureBlueCommittedContact?.Invoke(BlueContactVfx379.SelectedRoute(_route) && committed);
    }

    internal void SetAetherLanceCommittedContact(bool committed)
        =>_effect?.ConfigureAetherLanceCommittedContact?.Invoke(AetherLanceContactVfxProfiles.SelectedRoute(_route)&&committed);

    internal void SetVineFormationCounterCommittedContact(bool committed)
        =>_effect?.ConfigureVineFormationCounterCommittedContact?.Invoke(VineFormationCounterVfxProfiles.SelectedRoute(_route)&&committed);

    internal void SetLightBurstCommittedContact(bool committed)
        =>_effect?.ConfigureLightBurstCommittedContact?.Invoke(LightBurstContactVfxProfiles.SelectedRoute(_route)&&committed);

    //378 actual owned Spray receipt only; reset every Begin/Clear and supplied afresh each ordinary step.
    internal void SetLightScatterCommittedContact(bool committed)
    {
        _lightScatterCommitted=committed && _route?.Key=="card:white_light_spray"
            && _route.SourceKind=="card" && _route.DefinitionId=="white_light_spray"
            && _effect?.Shader=="Hd2dPrototype" && _effect.Palette=="hd2d-light-scatter";
    }

    internal void SetLightBoltCommittedContact(bool committed)
        =>_effect?.ConfigureLightBoltCommittedContact?.Invoke(LightBoltContactVfxProfiles.SelectedRoute(_route)&&committed);

    internal void SetCanopyCommittedContact(bool committed)
        =>_effect?.ConfigureCanopyCommittedContact?.Invoke(PermanentContactVfxProfiles.CanopyRoute(_route)&&committed);

    internal void SetColdSweepCommittedContact(bool committed)
        =>_effect?.ConfigureColdSweepCommittedContact?.Invoke(ColdSweepContactVfxProfiles.SelectedRoute(_route)&&committed);

    internal void SetImpSparkCommittedContact(bool committed)
        =>_effect?.ConfigureImpSparkCommittedContact?.Invoke(ImpSparkContactVfxProfiles.SelectedRoute(_route)&&committed);

    internal void SetMirrorCommittedContact(bool committed)
        =>_effect?.ConfigureMirrorCommittedContact?.Invoke(MirrorContactVfxProfiles.SelectedRoute(_route)&&committed);

	public void Sample(float elapsed)
	{
		if (_effect == null)
		{
			return;
		}
		double age = elapsed - _scenario.Windup;
        _legacyContact419SampleAge = (float)age;
		_effect.Sample(age);
		VertexCount = 0;
		float light = (RealLightsEnabled ? (Math.Max(0f, _effect.Light()) * _lightGain) : 0f);
		if (_effect.Shader == "BoundedContactFx")
		{
			light = 0f;
		}
		_targetLight.Position = _target;
        if (_effect.Shader == "Hd2dPrototype" && Hd2dAttackPrototype.UsesCastSource(_route) && age < 0)
        {
            float flight = Hd2dAttackPrototype.CastTravel(_route.Key,(float)age);
            _targetLight.Position = _source.Lerp(_target, flight);
        }
        if(_effect.Shader=="Hd2dPrototype" && Hd2dAttackPrototype.UsesFireSource(_route) && age>=.115)
        {
            float rise=Math.Clamp(((float)age-.115f)/.16f,0f,1f);
            _targetLight.Position=_target+Vector3.Up*(_height*.24f*rise);
        }
        bool supportContact = _supportLightScope && age >= 0;
        bool supportPending = _supportLightScope
            && (!_supportLightCurrentOwner || (supportContact && !_supportLightCommitted));
        float supportScale = supportContact ? _height * _height / 9f : 1f;
        bool legacy419Contact = _legacyContact419Scope && age >= 0;
        bool legacy419Pending = _legacyContact419Scope
            && (!_legacyContact419CurrentOwner || (legacy419Contact && !_legacyContact419Projected));
        float legacy419Scale = legacy419Contact ? _height * _height / 9f : 1f;
        bool greenPhysicalContact = _greenPhysicalContactScope && age >= 0;
        bool greenPhysicalPending = _greenPhysicalContactScope
            && (!_greenPhysicalCurrentOwner || (greenPhysicalContact && !_greenPhysicalCommittedContact));
        float greenPhysicalScale = greenPhysicalContact ? _height * _height / 9f : 1f;
        bool whiteLight396Contact = _whiteLight396ContactScope && age >= 0;
        bool whiteLight396Pending = _whiteLight396ContactScope
            && (!_whiteLight396CurrentOwner || (whiteLight396Contact && !_whiteLight396CommittedContact));
        float whiteLight396Scale = whiteLight396Contact ? _height * _height / 9f : 1f;
        bool rootingVine404Contact = _rootingVine404ContactScope && age >= 0;
        bool rootingVine404Pending = _rootingVine404ContactScope
            && (!_rootingVine404CurrentOwner || (rootingVine404Contact && !_rootingVine404CommittedContact));
        float rootingVine404Scale = rootingVine404Contact ? _height * _height / 9f : 1f;
        bool neutralContact = _neutralPressureContactScope && age >= 0;
        bool neutralPending = _neutralPressureContactScope
            && (!_neutralPressureCurrentOwner || (neutralContact && !_neutralPressureCommittedContact));
        float neutralScale = neutralContact ? _height * _height / 9f : 1f;
        bool earthContact = _earthRoarContactScope && age >= 0;
        bool earthPending = _earthRoarContactScope
            && (!_earthRoarCurrentOwner || (earthContact && !_earthRoarCommittedContact));
        float earthScale = earthContact ? _height * _height / 9f : 1f;
        if (neutralPending || earthPending || supportPending || legacy419Pending || greenPhysicalPending || whiteLight396Pending || rootingVine404Pending)
        {
            light = 0f;
        }
        bool waterContact=_waterWideContactScope&&age>=0;
        bool waterPending=_waterWideContactScope&&(!_waterWideCurrentOwner||(waterContact&&!_waterWideCommittedContact));
        if(waterPending)light=0;
        float waterScale=waterContact?_height*_height/9f:1f;
        bool ownedAxial=AxialFamilyContactVfxProfiles.Selected(_route);
        float axialHeight=Math.Max(.01f,_height);
        // Mutually exclusive selected families; restore original defaults on every pooled update.
        bool opticalContact=LightBoltContactVfxProfiles.SelectedRoute(_route)&&age>=0;
        bool scatterContact=_lightScatterCommitted&&age>=0;
        _targetLight.OmniRange=supportContact ? _height * SupportLightVfx391.TargetRange : legacy419Contact ? _height * LegacyContactVfx419.TargetRange : greenPhysicalContact ? _height * GreenPhysicalContactVfxProfiles.TargetRange : rootingVine404Contact ? _height * RootingVine404ContactVfxProfiles.TargetRange : whiteLight396Contact ? _height * WhiteLight396ContactVfxProfiles.TargetRange : earthContact ? _height * EarthRoarContactVfxProfiles.TargetRange : neutralContact ? _height * NeutralPressureContactVfxProfiles.TargetRange : waterContact?_height*WaterWideContactVfxProfiles.TargetRange:scatterContact?MathF.Max(.1f,_height*.75f):ownedAxial?axialHeight*.70f:opticalContact?MathF.Max(.1f,_height*1.10f):7f;
        _floorLight.OmniRange=supportContact ? _height * SupportLightVfx391.FloorRange : legacy419Contact ? _height * LegacyContactVfx419.FloorRange : greenPhysicalContact ? _height * GreenPhysicalContactVfxProfiles.FloorRange : rootingVine404Contact ? _height * RootingVine404ContactVfxProfiles.FloorRange : whiteLight396Contact ? _height * WhiteLight396ContactVfxProfiles.FloorRange : earthContact ? _height * EarthRoarContactVfxProfiles.FloorRange : neutralContact ? _height * NeutralPressureContactVfxProfiles.FloorRange : waterContact?_height*WaterWideContactVfxProfiles.FloorRange:scatterContact?MathF.Max(.1f,_height*.80f):ownedAxial?axialHeight*1.05f:opticalContact?MathF.Max(.1f,_height*1.45f):13f;
		_targetLight.LightColor = _lightColor;
		_targetLight.LightEnergy = light * 1.6f * (_effect.TargetPeak / 0.35f) * (supportContact ? SupportLightVfx391.TargetGain * supportScale : 1f) * (earthContact ? EarthRoarContactVfxProfiles.TargetGain * earthScale : 1f) * (neutralContact ? NeutralPressureContactVfxProfiles.TargetGain * neutralScale : 1f) * (waterContact?WaterWideContactVfxProfiles.TargetGain*waterScale:1f) * (legacy419Contact ? LegacyContactVfx419.TargetGain * legacy419Scale : 1f) * (greenPhysicalContact ? GreenPhysicalContactVfxProfiles.TargetGain * greenPhysicalScale : 1f) * (whiteLight396Contact ? WhiteLight396ContactVfxProfiles.TargetGain * whiteLight396Scale : 1f) * (rootingVine404Contact ? RootingVine404ContactVfxProfiles.TargetGain * rootingVine404Scale : 1f);
		_floorLight.Position = _feet + Vector3.Up * (_height * (_effect.Shader=="Hd2dPrototype" && !GearContactVfxProfiles.SelectedRoute(_route) && !GearKegContactVfxProfiles.SelectedRoute(_route) && !NamelessPowderContactVfxProfiles.SelectedRoute(_route) && !NamelessKegContactVfxProfiles.SelectedRoute(_route) ? .45f : .2f));
		if (_effect.Shader == "Hd2dPrototype" && Hd2dAttackPrototype.UsesCastSource(_route) && age < 0)
            _floorLight.Position = new Vector3(_targetLight.Position.X, _feet.Y+_height*.45f, _targetLight.Position.Z);
        if(opticalContact)_floorLight.Position=_feet+Vector3.Up*(_height*.12f);
        if(waterContact)_floorLight.Position=_feet+Vector3.Up*(_height*WaterWideContactVfxProfiles.FloorHeight);
        if (neutralContact)
        {
            _floorLight.Position = _feet + Vector3.Up * (_height * NeutralPressureContactVfxProfiles.FloorHeight);
        }
        if (earthContact)
        {
            _floorLight.Position = _feet + Vector3.Up * (_height * EarthRoarContactVfxProfiles.FloorHeight);
        }
        if (supportContact)
        {
            _floorLight.Position = _feet + Vector3.Up * (_height * SupportLightVfx391.FloorHeight);
        }
        if (legacy419Contact)
        {
            _floorLight.Position = _feet + Vector3.Up * (_height * LegacyContactVfx419.FloorHeight);
        }
        if (greenPhysicalContact)
        {
            _floorLight.Position = _feet + Vector3.Up * (_height * GreenPhysicalContactVfxProfiles.FloorHeight);
        }
        if (whiteLight396Contact)
        {
            _floorLight.Position = _feet + Vector3.Up * (_height * WhiteLight396ContactVfxProfiles.FloorHeight);
        }
        if (rootingVine404Contact)
        {
            _floorLight.Position = _feet + Vector3.Up * (_height * RootingVine404ContactVfxProfiles.FloorHeight);
        }
        if(scatterContact)_floorLight.Position=_feet+Vector3.Up*(_height*.10f);
		_floorLight.LightColor = _lightColor;
		float floorLight=RealLightsEnabled?Math.Max(0f,_effect.FloorLight?.Invoke()??_effect.Light())*_lightGain:0f;
		_floorLight.LightEnergy = ((QualityLevel < 2) ? (floorLight * 0.48f * (_effect.FloorPeak / 0.12f) * (scatterContact?.35f:1f)) : 0f) * (waterPending?0f:waterContact?WaterWideContactVfxProfiles.FloorGain*waterScale:1f) * (neutralPending ? 0f : neutralContact ? NeutralPressureContactVfxProfiles.FloorGain * neutralScale : 1f) * (earthPending ? 0f : earthContact ? EarthRoarContactVfxProfiles.FloorGain * earthScale : 1f) * (supportPending ? 0f : supportContact ? SupportLightVfx391.FloorGain * supportScale : 1f) * (legacy419Pending ? 0f : legacy419Contact ? LegacyContactVfx419.FloorGain * legacy419Scale : 1f) * (greenPhysicalPending ? 0f : greenPhysicalContact ? GreenPhysicalContactVfxProfiles.FloorGain * greenPhysicalScale : 1f) * (whiteLight396Pending ? 0f : whiteLight396Contact ? WhiteLight396ContactVfxProfiles.FloorGain * whiteLight396Scale : 1f) * (rootingVine404Pending ? 0f : rootingVine404Contact ? RootingVine404ContactVfxProfiles.FloorGain * rootingVine404Scale : 1f);
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

    //335 called with current owned ordinary-beat qualification, never a persistent pooled flag.
    public void UpdateOwnedHolyMaulHealReceiver(Vector3 feet,bool owned)
    {
        if(!owned || !IsActive || _effect==null || _effect.Shader!="HealContinuousFx"
            || (!HolyMaulHealReceiveVfxProfiles.OriginalRoute(_route)&&!HolyMaulHealReceiveVfxProfiles.SelectedRoute(_route))
            || !feet.IsFinite())return;
        //Keep the existing heel-to-heal anatomical offset, scale and camera plane.
        _feet=feet;_target=feet+_basis.Y*(_height*.46521738f);
    }

	public void AppendSourceLights(List<ActorMaterialBinding.PointLight> lights, bool forestReceiver = false, bool heroReceiver = false, bool tripleHornOwnedReceiver = false, bool doubleLashOwnedReceiver = false, bool holyMaulOwnedReceiver = false, bool holyMaulHealOwnedReceiver = false)
	{
		if (IsActive && RealLightsEnabled && _effect != null)
		{
			float weight = Math.Max(0f, _effect.Light()) * _lightGain;
            bool supportContact = _supportLightScope && (_effect.VisualAge?.Invoke() ?? -1f) >= 0;
            if (_supportLightScope && (!_supportLightCurrentOwner || (supportContact && !_supportLightCommitted)))
            {
                return;
            }
            float supportScale = supportContact ? _height * _height / 9f : 1f;
            float supportTargetGain = supportContact ? SupportLightVfx391.TargetGain * supportScale : 1f;
            float supportFloorGain = supportContact ? SupportLightVfx391.FloorGain * supportScale : 1f;
            bool legacy419Contact = _legacyContact419Scope && _legacyContact419SampleAge >= 0;
            if (_legacyContact419Scope
                && (!_legacyContact419CurrentOwner || (legacy419Contact && !_legacyContact419Projected)))
            {
                return;
            }
            float legacy419Scale = legacy419Contact ? _height * _height / 9f : 1f;
            float legacy419TargetGain = legacy419Contact ? LegacyContactVfx419.TargetGain * legacy419Scale : 1f;
            float legacy419FloorGain = legacy419Contact ? LegacyContactVfx419.FloorGain * legacy419Scale : 1f;
            bool greenPhysicalContact = _greenPhysicalContactScope && (_effect.VisualAge?.Invoke() ?? -1f) >= 0;
            if (_greenPhysicalContactScope
                && (!_greenPhysicalCurrentOwner || (greenPhysicalContact && !_greenPhysicalCommittedContact)))
            {
                return;
            }
            float greenPhysicalScale = greenPhysicalContact ? _height * _height / 9f : 1f;
            float greenPhysicalTargetGain = greenPhysicalContact ? GreenPhysicalContactVfxProfiles.TargetGain * greenPhysicalScale : 1f;
            float greenPhysicalFloorGain = greenPhysicalContact ? GreenPhysicalContactVfxProfiles.FloorGain * greenPhysicalScale : 1f;
            bool whiteLight396Contact = _whiteLight396ContactScope && (_effect.VisualAge?.Invoke() ?? -1f) >= 0;
            if (_whiteLight396ContactScope
                && (!_whiteLight396CurrentOwner || (whiteLight396Contact && !_whiteLight396CommittedContact)))
            {
                return;
            }
            float whiteLight396Scale = whiteLight396Contact ? _height * _height / 9f : 1f;
            float whiteLight396TargetGain = whiteLight396Contact ? WhiteLight396ContactVfxProfiles.TargetGain * whiteLight396Scale : 1f;
            float whiteLight396FloorGain = whiteLight396Contact ? WhiteLight396ContactVfxProfiles.FloorGain * whiteLight396Scale : 1f;
            bool rootingVine404Contact = _rootingVine404ContactScope && (_effect.VisualAge?.Invoke() ?? -1f) >= 0;
            if (_rootingVine404ContactScope
                && (!_rootingVine404CurrentOwner || (rootingVine404Contact && !_rootingVine404CommittedContact)))
            {
                return;
            }
            float rootingVine404Scale = rootingVine404Contact ? _height * _height / 9f : 1f;
            float rootingVine404TargetGain = rootingVine404Contact ? RootingVine404ContactVfxProfiles.TargetGain * rootingVine404Scale : 1f;
            float rootingVine404FloorGain = rootingVine404Contact ? RootingVine404ContactVfxProfiles.FloorGain * rootingVine404Scale : 1f;
            bool neutralContact = _neutralPressureContactScope && (_effect.VisualAge?.Invoke() ?? -1f) >= 0;
            if (_neutralPressureContactScope
                && (!_neutralPressureCurrentOwner || (neutralContact && !_neutralPressureCommittedContact)))
            {
                return;
            }
            float neutralScale = neutralContact ? _height * _height / 9f : 1f;
            float neutralTargetGain = neutralContact ? NeutralPressureContactVfxProfiles.TargetGain * neutralScale : 1f;
            float neutralFloorGain = neutralContact ? NeutralPressureContactVfxProfiles.FloorGain * neutralScale : 1f;
            bool earthContact = _earthRoarContactScope && (_effect.VisualAge?.Invoke() ?? -1f) >= 0;
            if (_earthRoarContactScope
                && (!_earthRoarCurrentOwner || (earthContact && !_earthRoarCommittedContact)))
            {
                return;
            }
            float earthScale = earthContact ? _height * _height / 9f : 1f;
            float earthTargetGain = earthContact ? EarthRoarContactVfxProfiles.TargetGain * earthScale : 1f;
            float earthFloorGain = earthContact ? EarthRoarContactVfxProfiles.FloorGain * earthScale : 1f;
            bool waterContact=_waterWideContactScope&&(_effect.VisualAge?.Invoke()??-1f)>=0;
            if(_waterWideContactScope&&(!_waterWideCurrentOwner||(waterContact&&!_waterWideCommittedContact)))return;
            float waterScale=waterContact?_height*_height/9f:1f;
            float waterTargetGain=waterContact?WaterWideContactVfxProfiles.TargetGain*waterScale:1f;
            float waterFloorGain=waterContact?WaterWideContactVfxProfiles.FloorGain*waterScale:1f;
            bool ownedAxial=AxialFamilyContactVfxProfiles.Selected(_route);
            float axialHeight=Math.Max(.01f,_height),axialScale=ownedAxial?axialHeight*axialHeight/9f:1f;
            float axialRadius=ownedAxial?axialHeight*.24f:0f;
            float floorWeight=Math.Max(0f,_effect.FloorLight?.Invoke()??_effect.Light())*_lightGain;
            //320 only the actual hero binding of the owned self-entry route is reduced.
            //Forest, physical Omnis, local mesh, other actors and all other routes stay unchanged.
            float ownedReceive=!forestReceiver && heroReceiver
                ?(MonkSelfEntryHealVfxProfiles.SelectedRoute(_route)?MonkSelfEntryHealVfxProfiles.HeroReceive
                    :MonkSquireEntryHealVfxProfiles.SelectedRoute(_route)?MonkSquireEntryHealVfxProfiles.HeroReceive:1f):1f;
            //Fresh owner/receipt/recipient qualification is supplied per current ordinary beat by Stage.
            //No cached mutable multiplier/receiver state: default and all unrelated recipients remain1.
            float tripleHornReceive=TripleHornReceiveVfxProfiles.SelectedRoute(_route)&&tripleHornOwnedReceiver
                ?TripleHornReceiveVfxProfiles.Receive:1f;
            float doubleLashReceive=DoubleLashReceiveVfxProfiles.SelectedRoute(_route)&&doubleLashOwnedReceiver
                ?DoubleLashReceiveVfxProfiles.Receive:1f;
            float holyMaulReceive=HolyMaulReceiveVfxProfiles.SelectedRoute(_route)&&holyMaulOwnedReceiver
                ?HolyMaulReceiveVfxProfiles.Receive:1f;
            float holyMaulHealReceive=!forestReceiver&&heroReceiver&&holyMaulHealOwnedReceiver
                &&HolyMaulHealReceiveVfxProfiles.SelectedRoute(_route)?HolyMaulHealReceiveVfxProfiles.HeroReceive:1f;
        //364 finite emitting area only for this freshly committed owned Heal -> actual hero call.
        //Wrap side centers span .225-.355 actor heights; .30H is the representative source half-width.
        //Small Heal compensates its .72 geometry scale, so radius remains actor-sized, not tier-sized.
        float holyMaulHealRadius=!forestReceiver&&heroReceiver&&holyMaulHealOwnedReceiver
            &&HolyMaulHealReceiveVfxProfiles.SelectedRoute(_route) ? .30f * _height : 0f;
            bool opticalContact=LightBoltContactVfxProfiles.SelectedRoute(_route)
                &&(_effect.VisualAge?.Invoke()??-1f)>=0;
            bool scatterContact=_lightScatterCommitted&&(_effect.VisualAge?.Invoke()??-1f)>=0;
            float targetRange=supportContact ? _height * SupportLightVfx391.TargetRange : legacy419Contact ? _height * LegacyContactVfx419.TargetRange : greenPhysicalContact ? _height * GreenPhysicalContactVfxProfiles.TargetRange : rootingVine404Contact ? _height * RootingVine404ContactVfxProfiles.TargetRange : whiteLight396Contact ? _height * WhiteLight396ContactVfxProfiles.TargetRange : earthContact ? _height * EarthRoarContactVfxProfiles.TargetRange : neutralContact ? _height * NeutralPressureContactVfxProfiles.TargetRange : waterContact?_height*WaterWideContactVfxProfiles.TargetRange:scatterContact?MathF.Max(.1f,_height*.75f):ownedAxial?axialHeight*.70f:opticalContact?MathF.Max(.1f,_height*1.10f):7f;
            float floorRange=supportContact ? _height * SupportLightVfx391.FloorRange : legacy419Contact ? _height * LegacyContactVfx419.FloorRange : greenPhysicalContact ? _height * GreenPhysicalContactVfxProfiles.FloorRange : rootingVine404Contact ? _height * RootingVine404ContactVfxProfiles.FloorRange : whiteLight396Contact ? _height * WhiteLight396ContactVfxProfiles.FloorRange : earthContact ? _height * EarthRoarContactVfxProfiles.FloorRange : neutralContact ? _height * NeutralPressureContactVfxProfiles.FloorRange : waterContact?_height*WaterWideContactVfxProfiles.FloorRange:scatterContact?MathF.Max(.1f,_height*.80f):ownedAxial?axialHeight*1.05f:opticalContact?MathF.Max(.1f,_height*1.45f):13f;
            float opticalRadius=opticalContact&&!forestReceiver?_height*.24f:0f;
            float receiveRadius=supportContact ? _height * SupportLightVfx391.NearRadius : legacy419Contact ? _height * LegacyContactVfx419.NearRadius : greenPhysicalContact ? _height * GreenPhysicalContactVfxProfiles.NearRadius : rootingVine404Contact ? _height * RootingVine404ContactVfxProfiles.NearRadius : whiteLight396Contact ? _height * WhiteLight396ContactVfxProfiles.NearRadius : earthContact ? _height * EarthRoarContactVfxProfiles.NearRadius : neutralContact ? _height * NeutralPressureContactVfxProfiles.NearRadius : waterContact?_height*WaterWideContactVfxProfiles.NearRadius:scatterContact?_height*.30f:ownedAxial?axialRadius:opticalContact?opticalRadius:holyMaulHealRadius;
			Color linear = _lightColor.SrgbToLinear();
			Vector3 rgb = new Vector3(linear.R, linear.G, linear.B);
			if (weight * _effect.TargetPeak > 0f)
			{
				lights.Add(new ActorMaterialBinding.PointLight(_targetLight.Position, targetRange, rgb * (40.5f * weight * _effect.TargetPeak * ownedReceive * tripleHornReceive * doubleLashReceive * holyMaulReceive * holyMaulHealReceive * axialScale * waterTargetGain * neutralTargetGain * earthTargetGain * supportTargetGain * legacy419TargetGain * greenPhysicalTargetGain * whiteLight396TargetGain * rootingVine404TargetGain), IsAttack: true, NearRadius: receiveRadius));
			}
			if ((QualityLevel < 2 || AnalyticalFloorFallbackEnabled) && floorWeight * _effect.FloorPeak > 0f)
			{
				// Brief shared hot irradiance joins the effect, target and terrain.
                // The prior constant .14 actor factor erased this crest entirely.
                float actorReceive=1f;
                if(_effect.Shader=="Hd2dPrototype" && !forestReceiver && !GearContactVfxProfiles.SelectedRoute(_route) && !GearKegContactVfxProfiles.SelectedRoute(_route) && !NamelessPowderContactVfxProfiles.SelectedRoute(_route) && !NamelessKegContactVfxProfiles.SelectedRoute(_route))
                {
                    float visualAge=_effect.VisualAge?.Invoke()??-1f;
                    float release=Math.Clamp((visualAge-.055f)/.135f,0f,1f);
                    float crest=visualAge<0?0f:1-release*release*(3-2*release);
                    actorReceive=.14f+.76f*crest;
                }
                lights.Add(new ActorMaterialBinding.PointLight(_floorLight.Position, floorRange, rgb * (63f * floorWeight * _effect.FloorPeak * (scatterContact?.35f:1f) * actorReceive * ownedReceive * tripleHornReceive * doubleLashReceive * holyMaulReceive * holyMaulHealReceive * axialScale * waterFloorGain * neutralFloorGain * earthFloorGain * supportFloorGain * legacy419FloorGain * greenPhysicalFloorGain * whiteLight396FloorGain * rootingVine404FloorGain), IsAttack: true, NearRadius: receiveRadius));
			}
		}
	}

	public void Clear()
	{
        _waterWideContactScope=false;_waterWideCurrentOwner=false;_waterWideCommittedContact=false;
        _legacyContact419Scope = false;
        _legacyContact419CurrentOwner = false;
        _legacyContact419Projected = false;
        _legacyContact419SampleAge = float.NaN;
        _greenPhysicalContactScope = false;
        _greenPhysicalCurrentOwner = false;
        _greenPhysicalCommittedContact = false;
        _whiteLight396ContactScope = false;
        _whiteLight396CurrentOwner = false;
        _whiteLight396CommittedContact = false;
        _rootingVine404ContactScope = false;
        _rootingVine404CurrentOwner = false;
        _rootingVine404CommittedContact = false;
        _neutralPressureContactScope = false;
        _neutralPressureCurrentOwner = false;
        _neutralPressureCommittedContact = false;
        _supportLightScope = false;
        _supportLightCurrentOwner = false;
        _supportLightCommitted = false;
        _earthRoarContactScope = false;
        _earthRoarCurrentOwner = false;
        _earthRoarCommittedContact = false;
        _lightScatterCommitted=false;
        _effect?.ConfigureMirrorCommittedContact?.Invoke(false);
        _effect?.ConfigureImpSparkCommittedContact?.Invoke(false);
        _effect?.ConfigureColdSweepCommittedContact?.Invoke(false);
        _effect?.ConfigureCanopyCommittedContact?.Invoke(false);
        _effect?.ConfigureLightBoltCommittedContact?.Invoke(false);
        _effect?.ConfigureLightBurstCommittedContact?.Invoke(false);
        _effect?.ConfigureVineFormationCounterCommittedContact?.Invoke(false);
        _effect?.ConfigureBlueCommittedContact?.Invoke(false);
        _effect?.ConfigureAetherLanceCommittedContact?.Invoke(false);
        _effect?.ConfigureSparkBurstCommittedContact?.Invoke(false);
        _effect?.ConfigureMomentumWhipCommittedContact?.Invoke(false);
        _effect?.ConfigureCultistDarkBladeCommittedContact?.Invoke(false);
        _effect?.ConfigureGreenGulpCommittedContact?.Invoke(false);
        _effect?.ConfigureSpore385CommittedContact?.Invoke(false);
        _prototypeContactAnchorLocked=false;
		foreach (Draw draw in _draws)
		{
			draw.Mesh.ClearSurfaces();
			draw.Node.Visible = false;
		}
		if (_targetLight != null)
		{
			_targetLight.LightEnergy = 0f;
			_floorLight.LightEnergy = 0f;
		}
		_effect = null;
		Visible = false;
		VertexCount = 0;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		return new List<MethodInfo>(4)
		{
			new MethodInfo(MethodName._Ready, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.C, new PropertyInfo(Variant.Type.Vector4, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Color, "c", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.Sample, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Float, "elapsed", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.Clear, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		if (method == MethodName._Ready && args.Count == 0)
		{
			_Ready();
			ret = default;
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
			ret = default;
			return true;
		}
		if (method == MethodName.Clear && args.Count == 0)
		{
			Clear();
			ret = default;
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
		ret = default;
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
		return new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Object, PropertyName._targetLight, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._floorLight, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.PackedColorArray, PropertyName._roles, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Color, PropertyName._lightColor, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Vector3, PropertyName._target, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Vector3, PropertyName._feet, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Vector3, PropertyName._source, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Basis, PropertyName._basis, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName._height, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName._size, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName._emission, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName._particles, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName._lightGain, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName.RealLightsEnabled, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.QualityLevel, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.ActiveLights, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.VertexCount, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName.IsActive, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.String, PropertyName.Renderer, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false)
		};
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

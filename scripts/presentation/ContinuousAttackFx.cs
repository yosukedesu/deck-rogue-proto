using System;
using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;

namespace DeckRogue.GodotPort.Presentation;

[ScriptPath("res://scripts/presentation/ContinuousAttackFx.cs")]
public class ContinuousAttackFx : Node3D
{
	public new class MethodName : Node3D.MethodName
	{
		public new static readonly StringName _Ready = "_Ready";

		public static readonly StringName Clear = "Clear";

		public static readonly StringName ElementColor = "ElementColor";

		public static readonly StringName Sample = "Sample";

		public static readonly StringName V = "V";

		public static readonly StringName Tri = "Tri";

		public static readonly StringName Ribbon = "Ribbon";

		public static readonly StringName Segment = "Segment";

		public static readonly StringName Noise = "Noise";

		public static readonly StringName Ray = "Ray";

		public static readonly StringName ContactFragments = "ContactFragments";

		public static readonly StringName Lightning = "Lightning";

		public static readonly StringName Fire = "Fire";

		public static readonly StringName Ice = "Ice";

		public static readonly StringName Water = "Water";

		public static readonly StringName Earth = "Earth";

		public static readonly StringName Fang = "Fang";

		public static readonly StringName Claw = "Claw";

		public static readonly StringName Dark = "Dark";

		public static readonly StringName Light = "Light";

		public static readonly StringName Botanical = "Botanical";

		public static readonly StringName Slash = "Slash";

		public static readonly StringName Projectile = "Projectile";

		public static readonly StringName Meteor = "Meteor";
	}

	public new class PropertyName : Node3D.PropertyName
	{
		public static readonly StringName RealLightsEnabled = "RealLightsEnabled";

		public static readonly StringName QualityLevel = "QualityLevel";

		public static readonly StringName VertexCount = "VertexCount";

		public static readonly StringName ActiveLights = "ActiveLights";

		public static readonly StringName _mesh = "_mesh";

		public static readonly StringName _draw = "_draw";

		public static readonly StringName _material = "_material";

		public static readonly StringName _sourceLight = "_sourceLight";

		public static readonly StringName _targetLight = "_targetLight";

		public static readonly StringName _source = "_source";

		public static readonly StringName _target = "_target";

		public static readonly StringName _time = "_time";

		public static readonly StringName _vertices = "_vertices";
	}

	public new class SignalName : Node3D.SignalName
	{
	}

	private readonly ImmediateMesh _mesh = new ImmediateMesh();

	private MeshInstance3D _draw;

	private StandardMaterial3D _material;

	private OmniLight3D _sourceLight;

	private OmniLight3D _targetLight;

	private AttackRoute _route = AttackRouteCatalog.Neutral();

	private EffectScenario _scenario = EffectScenario.For(AttackRouteCatalog.Neutral(), 1);

	private Vector3 _source;

	private Vector3 _target;

	private float _time;

	private int _vertices;

	public bool RealLightsEnabled { get; set; } = true;

	public int QualityLevel { get; set; }

	public int VertexCount => _vertices;

	public int ActiveLights
	{
		get
		{
			if (_sourceLight.LightEnergy > 0f)
			{
				if (_targetLight.LightEnergy > 0f)
				{
					return 2;
				}
				return 1;
			}
			return (_targetLight.LightEnergy > 0f) ? 1 : 0;
		}
	}

	public override void _Ready()
	{
		_material = new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			VertexColorUseAsAlbedo = true,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			BlendMode = BaseMaterial3D.BlendModeEnum.Add,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled,
			NoDepthTest = false,
			AlbedoColor = Colors.White
		};
		_draw = new MeshInstance3D
		{
			Mesh = _mesh,
			MaterialOverride = _material,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
		};
		AddChild(_draw, forceReadableName: false, InternalMode.Disabled);
		_sourceLight = new OmniLight3D
		{
			Name = "AttackSourceLight",
			LightEnergy = 0f,
			OmniRange = 3.2f,
			ShadowEnabled = false
		};
		_targetLight = new OmniLight3D
		{
			Name = "AttackTargetLight",
			LightEnergy = 0f,
			OmniRange = 4f,
			ShadowEnabled = false
		};
		AddChild(_sourceLight, forceReadableName: false, InternalMode.Disabled);
		AddChild(_targetLight, forceReadableName: false, InternalMode.Disabled);
	}

	public void Begin(AttackRoute route, EffectScenario scenario, Vector3 source, Vector3 target)
	{
		_route = route;
		_scenario = scenario;
		_source = source;
		_target = target;
		Visible = true;
		Sample(0f);
	}

	public void Clear()
	{
		_mesh.ClearSurfaces();
		_vertices = 0;
		if (_sourceLight != null)
		{
			_sourceLight.LightEnergy = 0f;
			_targetLight.LightEnergy = 0f;
		}
		Visible = false;
	}

	public static Color ElementColor(string element)
	{
		return element switch
		{
			"fire" => new Color("ff743f"), 
			"water" => new Color("699acb"), 
			"ice" => new Color("a4cafa"), 
			"lightning" => new Color("abaaff"), 
			"earth" => new Color("a88b69"), 
			"vine" => new Color("87a066"), 
			"light" => new Color("fff0cf"), 
			"dark" => new Color("a07fc7"), 
			"arcane" => new Color("b898bc"), 
			_ => new Color("c9cbd0"), 
		};
	}

	public void Sample(float time)
	{
		if (_draw == null)
		{
			return;
		}
		_time = time;
		_mesh.ClearSurfaces();
		_vertices = 0;
		float wind = _scenario.Windup;
		float active = _scenario.Contact + _scenario.Sustain;
		float end = wind + active;
		float buildup = Math.Clamp(time / wind, 0f, 1f);
		float p = Math.Clamp((time - wind) / active, 0f, 1f);
		float env;
		if (time < wind)
		{
			env = buildup * 0.22f;
		}
		else
		{
			env = ((time < end) ? MathF.Sin((float)Math.PI * (0.12f + p * 0.78f)) : (Math.Max(0f, 1f - (time - end) / _scenario.Release) * 0.25f));
		}
		Color color = ElementColor(_route.Element);
		_sourceLight.Position = _source + Vector3.Up * 0.15f;
		_targetLight.Position = _target + Vector3.Up * 0.35f;
		_sourceLight.LightColor = color;
		_targetLight.LightColor = color;
		_targetLight.OmniRange = 2.8f + _scenario.Radius * 2f;
		float neutral = ((_route.Element == "neutral") ? 0.15f : 1f);
		_sourceLight.LightEnergy = ((RealLightsEnabled && QualityLevel < 2) ? (env * _scenario.LightEnergy * 0.3f * neutral) : 0f);
		_targetLight.LightEnergy = ((RealLightsEnabled && time >= wind) ? (env * _scenario.LightEnergy * neutral) : 0f);
		if (!(env <= 0.001f))
		{
			_mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);
			string f = _route.Family;
			if (f == "minimal_neutral_contact")
			{
				ContactFragments(color, env, p, 4, 0.32f);
			}
			else if (f.Contains("lightning"))
			{
				Lightning(color, env, buildup, p);
			}
			else if (f.Contains("fire") || f == "fire")
			{
				Fire(color, env, buildup, p);
			}
			else if (f.Contains("ice") || f.Contains("cold"))
			{
				Ice(color, env, buildup, p);
			}
			else if (f.Contains("water"))
			{
				Water(color, env, buildup, p);
			}
			else if (f == "stomp" || f == "earth_fissure")
			{
				Earth(color, env, buildup, p);
			}
			else if (f.Contains("fang"))
			{
				Fang(color, env, p);
			}
			else if (f.Contains("claw"))
			{
				Claw(color, env, p);
			}
			else if (f.Contains("dark") || f.Contains("curse"))
			{
				Dark(color, env, buildup, p);
			}
			else if (f.Contains("light") || f == "scatter_warm_spark")
			{
				Light(color, env, buildup, p);
			}
			else if (f.Contains("vine") || f.Contains("plant") || f.Contains("botanical") || f.Contains("spore"))
			{
				Botanical(color, env, buildup, p);
			}
			else if (f.Contains("slash"))
			{
				Slash(color, env, p);
			}
			else if (f.Contains("pierce") || f.Contains("point") || f.Contains("projectile") || f.Contains("beam") || f.Contains("thrown"))
			{
				Projectile(color, env, buildup, p);
			}
			else if (f.Contains("meteor"))
			{
				Meteor(color, env, buildup, p);
			}
			else
			{
				ContactFragments(color, env, p, 6 + _scenario.Branches * 2, _scenario.Radius);
			}
			_mesh.SurfaceEnd();
		}
	}

	private void V(Vector3 p, Color c)
	{
		_mesh.SurfaceSetColor(c);
		_mesh.SurfaceAddVertex(p);
		_vertices++;
	}

	private void Tri(Vector3 a, Vector3 b, Vector3 c, Color color)
	{
		V(a, color);
		V(b, color);
		V(c, color);
	}

	private void Ribbon(Vector3[] points, float width, Color color, float alpha)
	{
		for (int i = 0; i < points.Length - 1; i++)
		{
			Vector3 dir = points[i + 1] - points[i];
			if (!(dir.LengthSquared() < 1E-06f))
			{
				Vector3 side = dir.Cross(new Vector3(0f, 0.05f, 1f)).Normalized() * width;
				Color c = color;
				c.A = alpha;
				Tri(points[i] - side, points[i] + side, points[i + 1] + side, c);
				Tri(points[i] - side, points[i + 1] + side, points[i + 1] - side, c);
			}
		}
	}

	private void Segment(Vector3 a, Vector3 b, float width, Color c, float alpha)
	{
		Ribbon(new Vector3[2] { a, b }, width, c, alpha);
	}

	private float Noise(float x, int seed)
	{
		return MathF.Sin(x * 13.71f + (float)seed * 2.27f) * 0.55f + MathF.Sin(x * 29.33f + (float)seed * 4.93f) * 0.28f;
	}

	private Vector3 Ray(int i, float radius)
	{
		return new Vector3(MathF.Cos((float)i * 2.399963f) * radius, MathF.Sin((float)i * 2.399963f) * radius, MathF.Sin((float)i * 1.77f) * radius * 0.2f);
	}

	private void ContactFragments(Color c, float e, float p, int count, float radius)
	{
		if (!(_time < _scenario.Windup))
		{
			for (int i = 0; i < count; i++)
			{
				Vector3 d = Ray(i, radius);
				Vector3 a = _target + d * (0.1f + p * 0.8f);
				Vector3 b = _target + d * (0.4f + p);
				Segment(a, b, 0.012f + (1f - p) * 0.02f, c, e * (1f - p * 0.55f));
			}
		}
	}

	private void Lightning(Color c, float e, float b, float p)
	{
		for (int k = 0; k < _scenario.Branches; k++)
		{
			int n = 20;
			Vector3[] pts = new Vector3[n];
			float reach = ((_time < _scenario.Windup) ? b : 1f);
			for (int i = 0; i < n; i++)
			{
				float t = (float)i / (float)(n - 1);
				Vector3 at = _source.Lerp(_target, t * reach);
				float edge = MathF.Sin(t * (float)Math.PI);
				pts[i] = at + new Vector3(Noise(t + _time * 1.2f, k) * 0.1f, Noise(t + _time * 0.7f, k + 5) * 0.21f, 0f) * edge;
			}
			Ribbon(pts, 0.05f, c, e * 0.16f);
			Ribbon(pts, 0.014f, new Color("ecebff"), e * 0.9f);
		}
		ContactFragments(c, e, p, 5 + _scenario.Branches, _scenario.Radius * 0.7f);
	}

	private void Fire(Color c, float e, float b, float p)
	{
		if (_time < _scenario.Windup)
		{
			Segment(_source, _source.Lerp(_target, b), 0.025f, c, e);
			return;
		}
		for (int k = 0; k < 2 + _scenario.Branches; k++)
		{
			float phase = (float)k * 2.4f;
			Vector3 basep = _target + new Vector3(MathF.Cos(phase) * _scenario.Radius * 0.35f, -0.75f, MathF.Sin(phase) * 0.17f);
			Vector3[] pts = new Vector3[9];
			for (int i = 0; i < 9; i++)
			{
				float t = (float)i / 8f;
				pts[i] = basep + new Vector3(MathF.Sin(t * 9f + _time * 15f + phase) * 0.13f * t, t * (0.9f + _scenario.Radius), 0f);
			}
			Ribbon(pts, 0.05f * (1f - p * 0.35f), c, e * 0.6f);
			Ribbon(pts, 0.013f, new Color("ffe9b6"), e * 0.7f);
		}
		ContactFragments(c, e, p, 5 + _scenario.Branches, _scenario.Radius);
	}

	private void Ice(Color c, float e, float b, float p)
	{
		Vector3 dir = (_target - _source).Normalized();
		for (int k = 0; k < _scenario.Branches; k++)
		{
			Vector3 offset = Ray(k, 0.1f + (float)k * 0.14f);
			Vector3 tip = _source.Lerp(_target, b) + offset;
			float length = 0.7f + _scenario.Radius;
			if (_time >= _scenario.Windup)
			{
				tip = _target + offset + dir * p * 0.15f;
			}
			Vector3 basep = tip - dir * length;
			Vector3 up = Vector3.Up * (0.07f + _scenario.Radius * 0.07f);
			Vector3 side = Vector3.Back * up.Length();
			Color col = c;
			col.A = e * 0.8f;
			Tri(tip, basep + up, basep + side, col);
			Tri(tip, basep + side, basep - up, col.Darkened(0.18f));
			Tri(tip, basep - up, basep - side, col);
			Tri(tip, basep - side, basep + up, col.Lightened(0.2f));
		}
		ContactFragments(c, e, p, 4 + _scenario.Branches, _scenario.Radius);
	}

	private void Water(Color c, float e, float b, float p)
	{
		for (int k = 0; k < _scenario.Branches; k++)
		{
			Vector3[] pts = new Vector3[18];
			for (int i = 0; i < 18; i++)
			{
				float t = (float)i / 17f;
				float q = Math.Clamp(t * (0.3f + b * 0.7f), 0f, 1f);
				pts[i] = _source.Lerp(_target, q) + new Vector3(0f, MathF.Sin(t * (float)Math.PI) * (0.18f + (float)k * 0.13f) + MathF.Sin(t * 17f - _time * 12f + (float)k) * 0.035f, 0f);
			}
			Ribbon(pts, 0.035f + _scenario.Radius * 0.027f, c, e * 0.45f);
		}
		if (_time >= _scenario.Windup)
		{
			for (int j = 0; j < 5 + _scenario.Branches; j++)
			{
				float s = ((j % 2 != 0) ? 1 : (-1));
				Vector3 a = _target + new Vector3(s * p * _scenario.Radius, MathF.Sin(p * (float)Math.PI) * (0.25f + (float)j * 0.035f), 0f);
				Segment(a, a + new Vector3(s * 0.15f, -0.1f, 0f), 0.014f, c, e);
			}
		}
	}

	private void Earth(Color c, float e, float b, float p)
	{
		if (!(_time < _scenario.Windup))
		{
			Vector3 ground = new Vector3(_target.X, 0.035f, _target.Z);
			for (int k = 0; k < 4 + _scenario.Branches; k++)
			{
				Vector3 d = new Vector3(MathF.Cos((float)k * 1.7f), 0f, MathF.Sin((float)k * 1.7f));
				Vector3[] pts = new Vector3[3]
				{
					ground,
					ground + d * (_scenario.Radius * 0.4f) + Vector3.Right * 0.06f,
					ground + d * _scenario.Radius
				};
				Ribbon(pts, 0.015f, c, e * 0.7f);
				Vector3 vector = ground + d * _scenario.Radius * 0.7f + Vector3.Up * MathF.Sin(p * (float)Math.PI) * 0.3f;
				float s = 0.08f + _scenario.Radius * 0.05f;
				Vector3 a = vector + Vector3.Up * s;
				Vector3 b2 = vector + Vector3.Left * s;
				Vector3 c2 = vector + Vector3.Right * s;
				Color color = c;
				color.A = e;
				Tri(a, b2, c2, color);
			}
		}
	}

	private void Fang(Color c, float e, float p)
	{
		if (_time < _scenario.Windup)
		{
			return;
		}
		float gap = (1f - MathF.Sin((float)Math.PI * p)) * 0.23f + 0.06f;
		for (int row = -1; row <= 1; row += 2)
		{
			for (int i = 0; i < 3 + _scenario.Branches; i++)
			{
				float x = ((float)i - (float)(2 + _scenario.Branches) / 2f) * 0.16f;
				Vector3 basep = _target + new Vector3(x, (float)row * (0.27f + gap), 0f);
				Vector3 tip = _target + new Vector3(x + 0.025f, (float)row * gap, 0f);
				Color col = c;
				col.A = e;
				Tri(basep + Vector3.Left * 0.055f, basep + Vector3.Right * 0.055f, tip, col);
			}
		}
	}

	private void Claw(Color c, float e, float p)
	{
		if (_time < _scenario.Windup)
		{
			return;
		}
		for (int k = 0; k < 2 + _scenario.Branches; k++)
		{
			Vector3[] pts = new Vector3[12];
			for (int i = 0; i < 12; i++)
			{
				float t = (float)i / 11f;
				pts[i] = _target + new Vector3((t - 0.5f) * _scenario.Radius * 1.8f + (float)(k - 1) * 0.16f, (0.5f - t) * _scenario.Radius + MathF.Sin(t * (float)Math.PI) * 0.2f, 0f);
			}
			Ribbon(pts, 0.02f, c, e * (1f - p * 0.6f));
		}
	}

	private void Dark(Color c, float e, float b, float p)
	{
		if (_time < _scenario.Windup)
		{
			return;
		}
		for (int k = 0; k < 1 + _scenario.Branches; k++)
		{
			Vector3[] pts = new Vector3[16];
			for (int i = 0; i < 16; i++)
			{
				float t = (float)i / 15f;
				float travel = Math.Clamp(t + p * 0.4f, 0f, 1f);
				pts[i] = _target.Lerp(_source, travel) + new Vector3(0f, MathF.Sin(t * (float)Math.PI) * (0.15f + (float)k * 0.16f) + Noise(t + _time, k) * 0.05f, 0f);
			}
			Ribbon(pts, 0.018f, c, e * 0.6f);
		}
		ContactFragments(c, e, p, 4, _scenario.Radius * 0.6f);
	}

	private void Light(Color c, float e, float b, float p)
	{
		Vector3 a = _target + Vector3.Up * (3f + _scenario.Radius);
		for (int k = 0; k < _scenario.Branches; k++)
		{
			Vector3 off = Vector3.Right * ((float)k - (float)(_scenario.Branches - 1) / 2f) * 0.12f;
			Segment(a + off, a.Lerp(_target, b) + off, 0.018f + _scenario.Radius * 0.01f, c, e * 0.75f);
		}
		ContactFragments(c, e, p, 5 + _scenario.Branches, _scenario.Radius);
	}

	private void Botanical(Color c, float e, float b, float p)
	{
		if (_time < _scenario.Windup)
		{
			return;
		}
		for (int k = 0; k < 2 + _scenario.Branches; k++)
		{
			Vector3 start = _target + Ray(k, _scenario.Radius * 0.5f) - Vector3.Up * 0.75f;
			Vector3[] pts = new Vector3[10];
			for (int i = 0; i < 10; i++)
			{
				float t = (float)i / 9f;
				pts[i] = start + new Vector3(MathF.Sin(t * 3f + (float)k) * 0.13f, t * (0.45f + _scenario.Radius) * Math.Min(1f, p * 3f), 0f);
			}
			Ribbon(pts, 0.016f, c, e * 0.7f);
			Vector3 end = pts[^1];
			Vector3 b2 = end + new Vector3(-0.11f, 0.08f, 0f);
			Vector3 c2 = end + new Vector3(0.025f, 0.16f, 0f);
			Color color = c;
			color.A = e * 0.6f;
			Tri(end, b2, c2, color);
		}
	}

	private void Slash(Color c, float e, float p)
	{
		if (_time < _scenario.Windup)
		{
			return;
		}
		for (int k = 0; k < _scenario.Branches; k++)
		{
			Vector3[] pts = new Vector3[18];
			for (int i = 0; i < 18; i++)
			{
				float t = (float)i / 17f;
				pts[i] = _target + new Vector3((t - 0.5f) * _scenario.Radius * 2f, (0.5f - t) * _scenario.Radius * 1.4f + (float)k * 0.1f, MathF.Sin(t * (float)Math.PI) * -0.13f);
			}
			Ribbon(pts, 0.025f, c, e * (1f - p * 0.4f));
		}
	}

	private void Projectile(Color c, float e, float b, float p)
	{
		Vector3 head = _source.Lerp(_target, b);
		Vector3 dir = (_target - _source).Normalized();
		Segment(head - dir * (0.35f + _scenario.Radius), head, 0.018f + _scenario.Radius * 0.012f, c, e);
		ContactFragments(c, e, p, 3 + _scenario.Branches, _scenario.Radius * 0.6f);
	}

	private void Meteor(Color c, float e, float b, float p)
	{
		Vector3 head = (_target + new Vector3(-1.2f, 3f, 0f)).Lerp(_target, b);
		Vector3 a = head + new Vector3(0f, 0.18f, 0f);
		Vector3 b2 = head + new Vector3(-0.2f, -0.12f, 0f);
		Vector3 c2 = head + new Vector3(0.17f, -0.1f, 0f);
		Color color = c;
		color.A = e;
		Tri(a, b2, c2, color);
		Segment(head, head + new Vector3(-0.3f, 0.7f, 0f), 0.09f, c, e * 0.4f);
		ContactFragments(c, e, p, 5 + _scenario.Branches, _scenario.Radius);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		return new List<MethodInfo>(24)
		{
			new MethodInfo(MethodName._Ready, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.Clear, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.ElementColor, new PropertyInfo(Variant.Type.Color, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.String, "element", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.Sample, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Float, "time", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.V, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Vector3, "p", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Color, "c", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.Tri, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Vector3, "a", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Vector3, "b", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Vector3, "c", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Color, "color", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.Ribbon, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.PackedVector3Array, "points", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "width", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Color, "color", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "alpha", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.Segment, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Vector3, "a", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Vector3, "b", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "width", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Color, "c", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "alpha", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.Noise, new PropertyInfo(Variant.Type.Float, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Float, "x", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Int, "seed", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.Ray, new PropertyInfo(Variant.Type.Vector3, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Int, "i", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "radius", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.ContactFragments, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Color, "c", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "e", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "p", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Int, "count", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "radius", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.Lightning, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Color, "c", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "e", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "b", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "p", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.Fire, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Color, "c", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "e", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "b", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "p", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.Ice, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Color, "c", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "e", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "b", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "p", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.Water, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Color, "c", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "e", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "b", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "p", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.Earth, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Color, "c", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "e", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "b", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "p", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.Fang, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Color, "c", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "e", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "p", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.Claw, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Color, "c", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "e", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "p", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.Dark, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Color, "c", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "e", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "b", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "p", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.Light, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Color, "c", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "e", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "b", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "p", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.Botanical, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Color, "c", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "e", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "b", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "p", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.Slash, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Color, "c", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "e", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "p", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.Projectile, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Color, "c", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "e", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "b", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "p", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.Meteor, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Color, "c", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "e", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "b", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "p", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null)
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
		if (method == MethodName.Clear && args.Count == 0)
		{
			Clear();
			ret = default;
			return true;
		}
		if (method == MethodName.ElementColor && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<Color>(ElementColor(VariantUtils.ConvertTo<string>(in args[0])));
			return true;
		}
		if (method == MethodName.Sample && args.Count == 1)
		{
			Sample(VariantUtils.ConvertTo<float>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.V && args.Count == 2)
		{
			V(VariantUtils.ConvertTo<Vector3>(in args[0]), VariantUtils.ConvertTo<Color>(in args[1]));
			ret = default;
			return true;
		}
		if (method == MethodName.Tri && args.Count == 4)
		{
			Tri(VariantUtils.ConvertTo<Vector3>(in args[0]), VariantUtils.ConvertTo<Vector3>(in args[1]), VariantUtils.ConvertTo<Vector3>(in args[2]), VariantUtils.ConvertTo<Color>(in args[3]));
			ret = default;
			return true;
		}
		if (method == MethodName.Ribbon && args.Count == 4)
		{
			Ribbon(VariantUtils.ConvertTo<Vector3[]>(in args[0]), VariantUtils.ConvertTo<float>(in args[1]), VariantUtils.ConvertTo<Color>(in args[2]), VariantUtils.ConvertTo<float>(in args[3]));
			ret = default;
			return true;
		}
		if (method == MethodName.Segment && args.Count == 5)
		{
			Segment(VariantUtils.ConvertTo<Vector3>(in args[0]), VariantUtils.ConvertTo<Vector3>(in args[1]), VariantUtils.ConvertTo<float>(in args[2]), VariantUtils.ConvertTo<Color>(in args[3]), VariantUtils.ConvertTo<float>(in args[4]));
			ret = default;
			return true;
		}
		if (method == MethodName.Noise && args.Count == 2)
		{
			ret = VariantUtils.CreateFrom<float>(Noise(VariantUtils.ConvertTo<float>(in args[0]), VariantUtils.ConvertTo<int>(in args[1])));
			return true;
		}
		if (method == MethodName.Ray && args.Count == 2)
		{
			ret = VariantUtils.CreateFrom<Vector3>(Ray(VariantUtils.ConvertTo<int>(in args[0]), VariantUtils.ConvertTo<float>(in args[1])));
			return true;
		}
		if (method == MethodName.ContactFragments && args.Count == 5)
		{
			ContactFragments(VariantUtils.ConvertTo<Color>(in args[0]), VariantUtils.ConvertTo<float>(in args[1]), VariantUtils.ConvertTo<float>(in args[2]), VariantUtils.ConvertTo<int>(in args[3]), VariantUtils.ConvertTo<float>(in args[4]));
			ret = default;
			return true;
		}
		if (method == MethodName.Lightning && args.Count == 4)
		{
			Lightning(VariantUtils.ConvertTo<Color>(in args[0]), VariantUtils.ConvertTo<float>(in args[1]), VariantUtils.ConvertTo<float>(in args[2]), VariantUtils.ConvertTo<float>(in args[3]));
			ret = default;
			return true;
		}
		if (method == MethodName.Fire && args.Count == 4)
		{
			Fire(VariantUtils.ConvertTo<Color>(in args[0]), VariantUtils.ConvertTo<float>(in args[1]), VariantUtils.ConvertTo<float>(in args[2]), VariantUtils.ConvertTo<float>(in args[3]));
			ret = default;
			return true;
		}
		if (method == MethodName.Ice && args.Count == 4)
		{
			Ice(VariantUtils.ConvertTo<Color>(in args[0]), VariantUtils.ConvertTo<float>(in args[1]), VariantUtils.ConvertTo<float>(in args[2]), VariantUtils.ConvertTo<float>(in args[3]));
			ret = default;
			return true;
		}
		if (method == MethodName.Water && args.Count == 4)
		{
			Water(VariantUtils.ConvertTo<Color>(in args[0]), VariantUtils.ConvertTo<float>(in args[1]), VariantUtils.ConvertTo<float>(in args[2]), VariantUtils.ConvertTo<float>(in args[3]));
			ret = default;
			return true;
		}
		if (method == MethodName.Earth && args.Count == 4)
		{
			Earth(VariantUtils.ConvertTo<Color>(in args[0]), VariantUtils.ConvertTo<float>(in args[1]), VariantUtils.ConvertTo<float>(in args[2]), VariantUtils.ConvertTo<float>(in args[3]));
			ret = default;
			return true;
		}
		if (method == MethodName.Fang && args.Count == 3)
		{
			Fang(VariantUtils.ConvertTo<Color>(in args[0]), VariantUtils.ConvertTo<float>(in args[1]), VariantUtils.ConvertTo<float>(in args[2]));
			ret = default;
			return true;
		}
		if (method == MethodName.Claw && args.Count == 3)
		{
			Claw(VariantUtils.ConvertTo<Color>(in args[0]), VariantUtils.ConvertTo<float>(in args[1]), VariantUtils.ConvertTo<float>(in args[2]));
			ret = default;
			return true;
		}
		if (method == MethodName.Dark && args.Count == 4)
		{
			Dark(VariantUtils.ConvertTo<Color>(in args[0]), VariantUtils.ConvertTo<float>(in args[1]), VariantUtils.ConvertTo<float>(in args[2]), VariantUtils.ConvertTo<float>(in args[3]));
			ret = default;
			return true;
		}
		if (method == MethodName.Light && args.Count == 4)
		{
			Light(VariantUtils.ConvertTo<Color>(in args[0]), VariantUtils.ConvertTo<float>(in args[1]), VariantUtils.ConvertTo<float>(in args[2]), VariantUtils.ConvertTo<float>(in args[3]));
			ret = default;
			return true;
		}
		if (method == MethodName.Botanical && args.Count == 4)
		{
			Botanical(VariantUtils.ConvertTo<Color>(in args[0]), VariantUtils.ConvertTo<float>(in args[1]), VariantUtils.ConvertTo<float>(in args[2]), VariantUtils.ConvertTo<float>(in args[3]));
			ret = default;
			return true;
		}
		if (method == MethodName.Slash && args.Count == 3)
		{
			Slash(VariantUtils.ConvertTo<Color>(in args[0]), VariantUtils.ConvertTo<float>(in args[1]), VariantUtils.ConvertTo<float>(in args[2]));
			ret = default;
			return true;
		}
		if (method == MethodName.Projectile && args.Count == 4)
		{
			Projectile(VariantUtils.ConvertTo<Color>(in args[0]), VariantUtils.ConvertTo<float>(in args[1]), VariantUtils.ConvertTo<float>(in args[2]), VariantUtils.ConvertTo<float>(in args[3]));
			ret = default;
			return true;
		}
		if (method == MethodName.Meteor && args.Count == 4)
		{
			Meteor(VariantUtils.ConvertTo<Color>(in args[0]), VariantUtils.ConvertTo<float>(in args[1]), VariantUtils.ConvertTo<float>(in args[2]), VariantUtils.ConvertTo<float>(in args[3]));
			ret = default;
			return true;
		}
		return base.InvokeGodotClassMethod(in method, args, out ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		if (method == MethodName.ElementColor && args.Count == 1)
		{
			ret = VariantUtils.CreateFrom<Color>(ElementColor(VariantUtils.ConvertTo<string>(in args[0])));
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
		if (method == MethodName.Clear)
		{
			return true;
		}
		if (method == MethodName.ElementColor)
		{
			return true;
		}
		if (method == MethodName.Sample)
		{
			return true;
		}
		if (method == MethodName.V)
		{
			return true;
		}
		if (method == MethodName.Tri)
		{
			return true;
		}
		if (method == MethodName.Ribbon)
		{
			return true;
		}
		if (method == MethodName.Segment)
		{
			return true;
		}
		if (method == MethodName.Noise)
		{
			return true;
		}
		if (method == MethodName.Ray)
		{
			return true;
		}
		if (method == MethodName.ContactFragments)
		{
			return true;
		}
		if (method == MethodName.Lightning)
		{
			return true;
		}
		if (method == MethodName.Fire)
		{
			return true;
		}
		if (method == MethodName.Ice)
		{
			return true;
		}
		if (method == MethodName.Water)
		{
			return true;
		}
		if (method == MethodName.Earth)
		{
			return true;
		}
		if (method == MethodName.Fang)
		{
			return true;
		}
		if (method == MethodName.Claw)
		{
			return true;
		}
		if (method == MethodName.Dark)
		{
			return true;
		}
		if (method == MethodName.Light)
		{
			return true;
		}
		if (method == MethodName.Botanical)
		{
			return true;
		}
		if (method == MethodName.Slash)
		{
			return true;
		}
		if (method == MethodName.Projectile)
		{
			return true;
		}
		if (method == MethodName.Meteor)
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
		if (name == PropertyName._draw)
		{
			_draw = VariantUtils.ConvertTo<MeshInstance3D>(in value);
			return true;
		}
		if (name == PropertyName._material)
		{
			_material = VariantUtils.ConvertTo<StandardMaterial3D>(in value);
			return true;
		}
		if (name == PropertyName._sourceLight)
		{
			_sourceLight = VariantUtils.ConvertTo<OmniLight3D>(in value);
			return true;
		}
		if (name == PropertyName._targetLight)
		{
			_targetLight = VariantUtils.ConvertTo<OmniLight3D>(in value);
			return true;
		}
		if (name == PropertyName._source)
		{
			_source = VariantUtils.ConvertTo<Vector3>(in value);
			return true;
		}
		if (name == PropertyName._target)
		{
			_target = VariantUtils.ConvertTo<Vector3>(in value);
			return true;
		}
		if (name == PropertyName._time)
		{
			_time = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		if (name == PropertyName._vertices)
		{
			_vertices = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		return base.SetGodotClassPropertyValue(in name, in value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		if (name == PropertyName.RealLightsEnabled)
		{
			value = VariantUtils.CreateFrom<bool>(RealLightsEnabled);
			return true;
		}
		int from;
		if (name == PropertyName.QualityLevel)
		{
			from = QualityLevel;
			value = VariantUtils.CreateFrom(in from);
			return true;
		}
		if (name == PropertyName.VertexCount)
		{
			from = VertexCount;
			value = VariantUtils.CreateFrom(in from);
			return true;
		}
		if (name == PropertyName.ActiveLights)
		{
			from = ActiveLights;
			value = VariantUtils.CreateFrom(in from);
			return true;
		}
		if (name == PropertyName._mesh)
		{
			value = VariantUtils.CreateFrom(in _mesh);
			return true;
		}
		if (name == PropertyName._draw)
		{
			value = VariantUtils.CreateFrom(in _draw);
			return true;
		}
		if (name == PropertyName._material)
		{
			value = VariantUtils.CreateFrom(in _material);
			return true;
		}
		if (name == PropertyName._sourceLight)
		{
			value = VariantUtils.CreateFrom(in _sourceLight);
			return true;
		}
		if (name == PropertyName._targetLight)
		{
			value = VariantUtils.CreateFrom(in _targetLight);
			return true;
		}
		if (name == PropertyName._source)
		{
			value = VariantUtils.CreateFrom(in _source);
			return true;
		}
		if (name == PropertyName._target)
		{
			value = VariantUtils.CreateFrom(in _target);
			return true;
		}
		if (name == PropertyName._time)
		{
			value = VariantUtils.CreateFrom(in _time);
			return true;
		}
		if (name == PropertyName._vertices)
		{
			value = VariantUtils.CreateFrom(in _vertices);
			return true;
		}
		return base.GetGodotClassPropertyValue(in name, out value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		return new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Object, PropertyName._mesh, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._draw, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._material, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._sourceLight, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Object, PropertyName._targetLight, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Vector3, PropertyName._source, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Vector3, PropertyName._target, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName._time, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName._vertices, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName.RealLightsEnabled, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.QualityLevel, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.VertexCount, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.ActiveLights, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		base.SaveGodotObjectData(info);
		info.AddProperty(PropertyName.RealLightsEnabled, Variant.From<bool>(RealLightsEnabled));
		info.AddProperty(PropertyName.QualityLevel, Variant.From<int>(QualityLevel));
		info.AddProperty(PropertyName._draw, Variant.From(in _draw));
		info.AddProperty(PropertyName._material, Variant.From(in _material));
		info.AddProperty(PropertyName._sourceLight, Variant.From(in _sourceLight));
		info.AddProperty(PropertyName._targetLight, Variant.From(in _targetLight));
		info.AddProperty(PropertyName._source, Variant.From(in _source));
		info.AddProperty(PropertyName._target, Variant.From(in _target));
		info.AddProperty(PropertyName._time, Variant.From(in _time));
		info.AddProperty(PropertyName._vertices, Variant.From(in _vertices));
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
		if (info.TryGetProperty(PropertyName._draw, out var _value__draw))
		{
			_draw = _value__draw.As<MeshInstance3D>();
		}
		if (info.TryGetProperty(PropertyName._material, out var _value__material))
		{
			_material = _value__material.As<StandardMaterial3D>();
		}
		if (info.TryGetProperty(PropertyName._sourceLight, out var _value__sourceLight))
		{
			_sourceLight = _value__sourceLight.As<OmniLight3D>();
		}
		if (info.TryGetProperty(PropertyName._targetLight, out var _value__targetLight))
		{
			_targetLight = _value__targetLight.As<OmniLight3D>();
		}
		if (info.TryGetProperty(PropertyName._source, out var _value__source))
		{
			_source = _value__source.As<Vector3>();
		}
		if (info.TryGetProperty(PropertyName._target, out var _value__target))
		{
			_target = _value__target.As<Vector3>();
		}
		if (info.TryGetProperty(PropertyName._time, out var _value__time))
		{
			_time = _value__time.As<float>();
		}
		if (info.TryGetProperty(PropertyName._vertices, out var _value__vertices))
		{
			_vertices = _value__vertices.As<int>();
		}
	}
}

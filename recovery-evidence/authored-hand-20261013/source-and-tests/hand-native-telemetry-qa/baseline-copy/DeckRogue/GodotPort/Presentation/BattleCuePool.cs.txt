using System;
using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;

namespace DeckRogue.GodotPort.Presentation;

[ScriptPath("res://scripts/presentation/BattleCuePool.cs")]
public class BattleCuePool : Node3D
{
	private sealed class Cue
	{
		public Label3D Label = null;

		public Sprite3D Icon = null;

		public Vector3 Origin;

		public Color Color;

		public float Start;

		public float Duration;

		public bool Active;

		public Func<Label3D, Vector3, Vector3>? Placement;
	}

	public new class MethodName : Node3D.MethodName
	{
		public new static readonly StringName _Ready = "_Ready";

		public static readonly StringName Sample = "Sample";

		public new static readonly StringName _Process = "_Process";

		public static readonly StringName Clear = "Clear";
	}

	public new class PropertyName : Node3D.PropertyName
	{
		public static readonly StringName ActiveCount = "ActiveCount";

		public static readonly StringName _next = "_next";

		public static readonly StringName _clock = "_clock";
	}

	public new class SignalName : Node3D.SignalName
	{
	}

	private readonly List<Cue> _pool = new List<Cue>();

	private int _next;

	private float _clock;

	public int ActiveCount { get; private set; }

	public override void _Ready()
	{
		Font font = GD.Load<Font>("res://assets/Fonts/NotoSansJP-Regular.otf");
		for (int i = 0; i < 16; i++)
		{
			Cue c = new Cue();
			c.Label = new Label3D
			{
				Name = "ContactText" + i,
				Font = font,
				FontSize = 30,
				PixelSize = 0.008f,
				Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
				NoDepthTest = true,
				OutlineSize = 7,
				OutlineModulate = new Color("10131b"),
				Visible = false
			};
			c.Icon = new Sprite3D
			{
				Name = "ContactIcon" + i,
				PixelSize = 0.011f,
				Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
				Shaded = false,
				NoDepthTest = true,
				TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
				Visible = false
			};
			AddChild(c.Label, forceReadableName: false, InternalMode.Disabled);
			AddChild(c.Icon, forceReadableName: false, InternalMode.Disabled);
			_pool.Add(c);
		}
	}

	public Label3D? Show(string text, Vector3 origin, Color color, string? icon = null, float duration = 0.85f, int? fontSize = null, float? pixelSize = null, Func<Label3D, Vector3, Vector3>? placement = null)
	{
		if (_pool.Count == 0)
		{
			return null;
		}
		Cue c = _pool[_next++ % _pool.Count];
		c.Active = true;
		c.Start = _clock;
		c.Duration = Math.Max(0.15f, duration);
		c.Origin = origin;
		c.Color = color;
		c.Placement = placement;
		c.Label.SetMeta("cue_placement_id", 0L);
		c.Label.Text = text;
		c.Label.FontSize = fontSize ?? ((text.Length > 16) ? 18 : 30);
		c.Label.PixelSize = pixelSize ?? 0.008f;
		c.Label.Visible = true;
		c.Icon.Texture = ((icon != null && ResourceLoader.Exists("res://assets/Art/icons/" + icon + ".png")) ? GD.Load<Texture2D>("res://assets/Art/icons/" + icon + ".png") : null);
		c.Icon.Visible = c.Icon.Texture != null;
		Sample(_clock);
		return c.Label;
	}

	public void Sample(float clock)
	{
		_clock = clock;
		ActiveCount = 0;
		foreach (Cue c in _pool)
		{
			if (c.Active)
			{
				float t = Math.Clamp((clock - c.Start) / c.Duration, 0f, 1f);
				if (t >= 1f)
				{
					c.Active = false;
					c.Placement = null;
					c.Label.Visible = false;
					c.Icon.Visible = false;
					continue;
				}
				ActiveCount++;
				Color color = c.Color;
				color.A *= Math.Min(1f, (1f - t) * 3f);
				c.Label.Modulate = color;
				Vector3 desired = c.Origin + Vector3.Up * (0.45f * t);
				c.Label.Position = c.Placement?.Invoke(c.Label, desired) ?? desired;
				c.Icon.Position = c.Label.Position + Vector3.Left * 0.42f;
				c.Icon.Modulate = color;
				c.Icon.Scale = Vector3.One * (1f + 0.14f * MathF.Sin((float)Math.PI * Math.Min(1f, t * 3f)));
			}
		}
	}

	public override void _Process(double delta)
	{
		if (_pool.Exists((Cue c) => c.Active && c.Placement != null))
		{
			Sample(_clock);
		}
	}

	public void Clear()
	{
		foreach (Cue c in _pool)
		{
			c.Active = false;
			c.Placement = null;
			c.Label.Visible = false;
			c.Icon.Visible = false;
		}
		ActiveCount = 0;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		List<MethodInfo> methods = new List<MethodInfo>(4);
		methods.Add(new MethodInfo(MethodName._Ready, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null));
		methods.Add(new MethodInfo(MethodName.Sample, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Float, "clock", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
		}, null));
		methods.Add(new MethodInfo(MethodName._Process, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Float, "delta", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
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
		if (method == MethodName.Sample && args.Count == 1)
		{
			Sample(VariantUtils.ConvertTo<float>(in args[0]));
			ret = default(godot_variant);
			return true;
		}
		if (method == MethodName._Process && args.Count == 1)
		{
			_Process(VariantUtils.ConvertTo<double>(in args[0]));
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
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if (method == MethodName._Ready)
		{
			return true;
		}
		if (method == MethodName.Sample)
		{
			return true;
		}
		if (method == MethodName._Process)
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
		if (name == PropertyName.ActiveCount)
		{
			ActiveCount = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName._next)
		{
			_next = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName._clock)
		{
			_clock = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		return base.SetGodotClassPropertyValue(in name, in value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		if (name == PropertyName.ActiveCount)
		{
			value = VariantUtils.CreateFrom<int>(ActiveCount);
			return true;
		}
		if (name == PropertyName._next)
		{
			value = VariantUtils.CreateFrom(in _next);
			return true;
		}
		if (name == PropertyName._clock)
		{
			value = VariantUtils.CreateFrom(in _clock);
			return true;
		}
		return base.GetGodotClassPropertyValue(in name, out value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		List<PropertyInfo> properties = new List<PropertyInfo>();
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName._next, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Float, PropertyName._clock, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		properties.Add(new PropertyInfo(Variant.Type.Int, PropertyName.ActiveCount, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false));
		return properties;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		base.SaveGodotObjectData(info);
		info.AddProperty(PropertyName.ActiveCount, Variant.From<int>(ActiveCount));
		info.AddProperty(PropertyName._next, Variant.From(in _next));
		info.AddProperty(PropertyName._clock, Variant.From(in _clock));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		base.RestoreGodotObjectData(info);
		if (info.TryGetProperty(PropertyName.ActiveCount, out var _value_ActiveCount))
		{
			ActiveCount = _value_ActiveCount.As<int>();
		}
		if (info.TryGetProperty(PropertyName._next, out var _value__next))
		{
			_next = _value__next.As<int>();
		}
		if (info.TryGetProperty(PropertyName._clock, out var _value__clock))
		{
			_clock = _value__clock.As<float>();
		}
	}
}

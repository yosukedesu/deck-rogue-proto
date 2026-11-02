using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;

namespace DeckRogue.GodotPort.UI;

[ScriptPath("res://scripts/ui/SourceAimArrow.cs")]
public class SourceAimArrow : Control
{
	public new class MethodName : Control.MethodName
	{
		public static readonly StringName Curve = "Curve";

		public new static readonly StringName _Draw = "_Draw";
	}

	public new class PropertyName : Control.PropertyName
	{
		public static readonly StringName VisibleDots = "VisibleDots";

		public static readonly StringName Active = "Active";

		public static readonly StringName Start = "Start";

		public static readonly StringName End = "End";

		public static readonly StringName Unit = "Unit";

		public static readonly StringName Note = "Note";
	}

	public new class SignalName : Control.SignalName
	{
	}

	public bool Active;

	public Vector2 Start;

	public Vector2 End;

	public float Unit = 1f;

	public Rect2? LeaderBody;

	public IReadOnlyList<Rect2> Blockers = Array.Empty<Rect2>();

	public string Note = "";

	public int VisibleDots { get; private set; }

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		return new List<MethodInfo>(2)
		{
			new MethodInfo(MethodName.Curve, new PropertyInfo(Variant.Type.Vector2, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Vector2, "a", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Vector2, "b", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Vector2, "c", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "t", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName._Draw, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		if (method == MethodName.Curve && args.Count == 4)
		{
			ret = VariantUtils.CreateFrom<Vector2>(Curve(VariantUtils.ConvertTo<Vector2>(in args[0]), VariantUtils.ConvertTo<Vector2>(in args[1]), VariantUtils.ConvertTo<Vector2>(in args[2]), VariantUtils.ConvertTo<float>(in args[3])));
			return true;
		}
		if (method == MethodName._Draw && args.Count == 0)
		{
			_Draw();
			ret = default;
			return true;
		}
		return base.InvokeGodotClassMethod(in method, args, out ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		if (method == MethodName.Curve && args.Count == 4)
		{
			ret = VariantUtils.CreateFrom<Vector2>(Curve(VariantUtils.ConvertTo<Vector2>(in args[0]), VariantUtils.ConvertTo<Vector2>(in args[1]), VariantUtils.ConvertTo<Vector2>(in args[2]), VariantUtils.ConvertTo<float>(in args[3])));
			return true;
		}
		ret = default;
		return false;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if (method == MethodName.Curve)
		{
			return true;
		}
		if (method == MethodName._Draw)
		{
			return true;
		}
		return base.HasGodotClassMethod(in method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if (name == PropertyName.VisibleDots)
		{
			VisibleDots = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName.Active)
		{
			Active = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName.Start)
		{
			Start = VariantUtils.ConvertTo<Vector2>(in value);
			return true;
		}
		if (name == PropertyName.End)
		{
			End = VariantUtils.ConvertTo<Vector2>(in value);
			return true;
		}
		if (name == PropertyName.Unit)
		{
			Unit = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		if (name == PropertyName.Note)
		{
			Note = VariantUtils.ConvertTo<string>(in value);
			return true;
		}
		return base.SetGodotClassPropertyValue(in name, in value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		if (name == PropertyName.VisibleDots)
		{
			value = VariantUtils.CreateFrom<int>(VisibleDots);
			return true;
		}
		if (name == PropertyName.Active)
		{
			value = VariantUtils.CreateFrom(in Active);
			return true;
		}
		if (name == PropertyName.Start)
		{
			value = VariantUtils.CreateFrom(in Start);
			return true;
		}
		if (name == PropertyName.End)
		{
			value = VariantUtils.CreateFrom(in End);
			return true;
		}
		if (name == PropertyName.Unit)
		{
			value = VariantUtils.CreateFrom(in Unit);
			return true;
		}
		if (name == PropertyName.Note)
		{
			value = VariantUtils.CreateFrom(in Note);
			return true;
		}
		return base.GetGodotClassPropertyValue(in name, out value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		return new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Bool, PropertyName.Active, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Vector2, PropertyName.Start, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Vector2, PropertyName.End, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName.Unit, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.VisibleDots, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.String, PropertyName.Note, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		base.SaveGodotObjectData(info);
		info.AddProperty(PropertyName.VisibleDots, Variant.From<int>(VisibleDots));
		info.AddProperty(PropertyName.Active, Variant.From(in Active));
		info.AddProperty(PropertyName.Start, Variant.From(in Start));
		info.AddProperty(PropertyName.End, Variant.From(in End));
		info.AddProperty(PropertyName.Unit, Variant.From(in Unit));
		info.AddProperty(PropertyName.Note, Variant.From(in Note));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		base.RestoreGodotObjectData(info);
		if (info.TryGetProperty(PropertyName.VisibleDots, out var _value_VisibleDots))
		{
			VisibleDots = _value_VisibleDots.As<int>();
		}
		if (info.TryGetProperty(PropertyName.Active, out var _value_Active))
		{
			Active = _value_Active.As<bool>();
		}
		if (info.TryGetProperty(PropertyName.Start, out var _value_Start))
		{
			Start = _value_Start.As<Vector2>();
		}
		if (info.TryGetProperty(PropertyName.End, out var _value_End))
		{
			End = _value_End.As<Vector2>();
		}
		if (info.TryGetProperty(PropertyName.Unit, out var _value_Unit))
		{
			Unit = _value_Unit.As<float>();
		}
		if (info.TryGetProperty(PropertyName.Note, out var _value_Note))
		{
			Note = _value_Note.As<string>();
		}
	}

	private static Vector2 Curve(Vector2 a, Vector2 b, Vector2 c, float t)
	{
		return a * (1f - t) * (1f - t) + b * 2f * (1f - t) * t + c * t * t;
	}

	public override void _Draw()
	{
		VisibleDots = 0;
		if (!Active)
		{
			return;
		}
		Vector2 mid = new Vector2(Start.X, Math.Min(Start.Y, End.Y) - 70f * Unit);
		Rect2? leaderBody = LeaderBody;
		if (leaderBody.HasValue)
		{
			Rect2 body = leaderBody.GetValueOrDefault();
			for (int n = 1; n <= 4; n++)
			{
				if (!Enumerable.Range(1, 23).Any((int num) => body.HasPoint(Curve(Start, mid, End, (float)num / 24f))))
				{
					break;
				}
				mid.X = Mathf.Lerp(Start.X, End.X, 0.15f * (float)n);
			}
		}
		Vector2[] pts = new Vector2[49];
		float[] acc = new float[49];
		for (int i = 0; i <= 48; i++)
		{
			pts[i] = Curve(Start, mid, End, (float)i / 48f);
			if (i > 0)
			{
				acc[i] = acc[i - 1] + pts[i].DistanceTo(pts[i - 1]);
			}
		}
		int seg = 1;
		for (int i2 = 0; i2 < 14; i2++)
		{
			float distance;
			for (distance = acc[48] * (float)i2 / 14f; seg < 48 && acc[seg] < distance; seg++)
			{
			}
			float span = acc[seg] - acc[seg - 1];
			Vector2 p = ((span > 0.001f) ? pts[seg - 1].Lerp(pts[seg], (distance - acc[seg - 1]) / span) : pts[seg]);
			if (!(p.DistanceTo(End) < 28f * Unit) && !Blockers.Any((Rect2 r) => r.HasPoint(p)))
			{
				DrawCircle(p, 6.5f * Unit, SourcePalette.Ink);
				DrawCircle(p, 5f * Unit, SourcePalette.Brass);
				VisibleDots++;
			}
		}
		DrawCircle(End, 24f * Unit, new Color("ead08a33"));
		DrawArc(End, 24f * Unit, 0f, (float)Math.PI * 2f, 40, SourcePalette.Brass, 2f * Unit, antialiased: true);
		DrawArc(End, 26f * Unit, 0f, (float)Math.PI * 2f, 40, SourcePalette.Ink, Unit, antialiased: true);
		if (Note.Length > 0)
		{
			Font font = SourceRunUiDecor.Action;
			int fs = (int)(15f * Unit);
			while (fs > Math.Max(10, (int)(12f * Unit)) && font.GetStringSize(Note, fontSize: fs).X > Size.X - 36f * Unit) fs--;
            Vector2 size = font.GetStringSize(Note, HorizontalAlignment.Left, -1f, fs, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal) + new Vector2(16f, 10f) * Unit;
			Vector2 p2 = End + new Vector2(32f, 0f - size.Y - 10f * Unit);
			p2.X = Math.Clamp(p2.X, 10f, Math.Max(10f, Size.X - size.X - 10f));
            p2.Y = Math.Clamp(p2.Y, 10f, Math.Max(10f, Size.Y - size.Y - 10f));
			DrawRect(new Rect2(p2, size), SourcePalette.Night);
			DrawString(font, p2 + new Vector2(8f * Unit, 5f * Unit + font.GetAscent(fs)), Note, HorizontalAlignment.Left, -1f, fs, SourcePalette.Paper, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
		}
	}
}

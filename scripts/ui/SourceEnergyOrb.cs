using System;
using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;

namespace DeckRogue.GodotPort.UI;

[ScriptPath("res://scripts/ui/SourceEnergyOrb.cs")]
public class SourceEnergyOrb : Control
{
	public new class MethodName : Control.MethodName
	{
		public new static readonly StringName _Draw = "_Draw";
	}

	public new class PropertyName : Control.PropertyName
	{
		public static readonly StringName Energy = "Energy";

		public static readonly StringName Maximum = "Maximum";

		public static readonly StringName TextScale = "TextScale";
	}

	public new class SignalName : Control.SignalName
	{
	}

	public int Energy;

	public int Maximum = 3;

	public float TextScale = 1f;

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		return new List<MethodInfo>(1)
		{
			new MethodInfo(MethodName._Draw, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		if (method == MethodName._Draw && args.Count == 0)
		{
			_Draw();
			ret = default;
			return true;
		}
		return base.InvokeGodotClassMethod(in method, args, out ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if (method == MethodName._Draw)
		{
			return true;
		}
		return base.HasGodotClassMethod(in method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if (name == PropertyName.Energy)
		{
			Energy = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName.Maximum)
		{
			Maximum = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName.TextScale)
		{
			TextScale = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		return base.SetGodotClassPropertyValue(in name, in value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		if (name == PropertyName.Energy)
		{
			value = VariantUtils.CreateFrom(in Energy);
			return true;
		}
		if (name == PropertyName.Maximum)
		{
			value = VariantUtils.CreateFrom(in Maximum);
			return true;
		}
		if (name == PropertyName.TextScale)
		{
			value = VariantUtils.CreateFrom(in TextScale);
			return true;
		}
		return base.GetGodotClassPropertyValue(in name, out value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		return new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Int, PropertyName.Energy, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.Maximum, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName.TextScale, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		base.SaveGodotObjectData(info);
		info.AddProperty(PropertyName.Energy, Variant.From(in Energy));
		info.AddProperty(PropertyName.Maximum, Variant.From(in Maximum));
		info.AddProperty(PropertyName.TextScale, Variant.From(in TextScale));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		base.RestoreGodotObjectData(info);
		if (info.TryGetProperty(PropertyName.Energy, out var _value_Energy))
		{
			Energy = _value_Energy.As<int>();
		}
		if (info.TryGetProperty(PropertyName.Maximum, out var _value_Maximum))
		{
			Maximum = _value_Maximum.As<int>();
		}
		if (info.TryGetProperty(PropertyName.TextScale, out var _value_TextScale))
		{
			TextScale = _value_TextScale.As<float>();
		}
	}

	public override void _Draw()
	{
		Vector2 c = Size / 2f;
		float r = Math.Min(Size.X, Size.Y) / 2f - 3f;
		DrawCircle(c, r, SourcePalette.Brass);
		DrawCircle(c, r - 8f, SourcePalette.Paper);
		if (Energy > 0)
		{
			DrawArc(c, r - 3f, -(float)Math.PI / 2f, -(float)Math.PI / 2f + (float)Math.PI * 2f * Math.Clamp((float)Energy / (float)Math.Max(1, Maximum), 0f, 1f), 60, SourcePalette.BrassLight, 4f, antialiased: true);
		}
		Font deco = SourceRunUiDecor.Display;
		Font body = SourceRunUiDecor.Body;
		float u = Size.X / 128f;
		string amount = Energy.ToString();
		string maximum = "/ " + Maximum;
		int amountSize = (int)Math.Round(38f * u * TextScale);
		int maximumSize = (int)Math.Round(13f * u * TextScale);
		float gap = 6f * u;
		float available = Size.X - 18f * u;
		float aw = deco.GetStringSize(amount, HorizontalAlignment.Left, -1f, amountSize, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal).X;
		float mw = body.GetStringSize(maximum, HorizontalAlignment.Left, -1f, maximumSize, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal).X;
		if (aw + gap + mw > available)
		{
			float fit = available / (aw + gap + mw);
			amountSize = Math.Max(1, (int)Math.Floor((float)amountSize * fit));
			maximumSize = Math.Max(1, (int)Math.Floor((float)maximumSize * fit));
			aw = deco.GetStringSize(amount, HorizontalAlignment.Left, -1f, amountSize, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal).X;
			mw = body.GetStringSize(maximum, HorizontalAlignment.Left, -1f, maximumSize, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal).X;
		}
		float left = (Size.X - aw - gap - mw) / 2f;
		float baseline = Size.Y * 0.6f;
		DrawString(deco, new Vector2(left, baseline), amount, HorizontalAlignment.Left, -1f, amountSize, SourcePalette.Ink, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
		DrawString(body, new Vector2(left + aw + gap, baseline), maximum, HorizontalAlignment.Left, -1f, maximumSize, SourcePalette.InkSoft, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
		SetMeta("displayed_energy", amount);
		SetMeta("displayed_maximum", maximum);
		SetMeta("energy_font_size", amountSize);
		SetMeta("energy_group_width", aw + gap + mw);
		DrawString(body, new Vector2(0f, Size.Y * 0.82f), "エナジー", HorizontalAlignment.Center, Size.X, (int)(12f * u * TextScale), SourcePalette.InkSoft, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
	}
}

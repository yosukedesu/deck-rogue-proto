using System;
using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;

namespace DeckRogue.GodotPort.UI;

[ScriptPath("res://scripts/ui/SourceHealthBar.cs")]
public class SourceHealthBar : Control
{
	public new class MethodName : Control.MethodName
	{
		public new static readonly StringName _Draw = "_Draw";
	}

	public new class PropertyName : Control.PropertyName
	{
		public static readonly StringName Hp = "Hp";

		public static readonly StringName Maximum = "Maximum";

		public static readonly StringName Block = "Block";

		public static readonly StringName IceBlock = "IceBlock";

		public static readonly StringName PredictedLoss = "PredictedLoss";
	}

	public new class SignalName : Control.SignalName
	{
	}

	public int Hp;

	public int Maximum;

	public int Block;

	public int IceBlock;

	public int PredictedLoss;

	public float SourceInsetUnit;

	public float SourceHeroUnit;

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
		if (name == PropertyName.Hp)
		{
			Hp = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName.Maximum)
		{
			Maximum = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName.Block)
		{
			Block = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName.IceBlock)
		{
			IceBlock = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName.PredictedLoss)
		{
			PredictedLoss = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		return base.SetGodotClassPropertyValue(in name, in value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		if (name == PropertyName.Hp)
		{
			value = VariantUtils.CreateFrom(in Hp);
			return true;
		}
		if (name == PropertyName.Maximum)
		{
			value = VariantUtils.CreateFrom(in Maximum);
			return true;
		}
		if (name == PropertyName.Block)
		{
			value = VariantUtils.CreateFrom(in Block);
			return true;
		}
		if (name == PropertyName.IceBlock)
		{
			value = VariantUtils.CreateFrom(in IceBlock);
			return true;
		}
		if (name == PropertyName.PredictedLoss)
		{
			value = VariantUtils.CreateFrom(in PredictedLoss);
			return true;
		}
		return base.GetGodotClassPropertyValue(in name, out value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		return new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Int, PropertyName.Hp, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.Maximum, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.Block, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.IceBlock, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName.PredictedLoss, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		base.SaveGodotObjectData(info);
		info.AddProperty(PropertyName.Hp, Variant.From(in Hp));
		info.AddProperty(PropertyName.Maximum, Variant.From(in Maximum));
		info.AddProperty(PropertyName.Block, Variant.From(in Block));
		info.AddProperty(PropertyName.IceBlock, Variant.From(in IceBlock));
		info.AddProperty(PropertyName.PredictedLoss, Variant.From(in PredictedLoss));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		base.RestoreGodotObjectData(info);
		if (info.TryGetProperty(PropertyName.Hp, out var _value_Hp))
		{
			Hp = _value_Hp.As<int>();
		}
		if (info.TryGetProperty(PropertyName.Maximum, out var _value_Maximum))
		{
			Maximum = _value_Maximum.As<int>();
		}
		if (info.TryGetProperty(PropertyName.Block, out var _value_Block))
		{
			Block = _value_Block.As<int>();
		}
		if (info.TryGetProperty(PropertyName.IceBlock, out var _value_IceBlock))
		{
			IceBlock = _value_IceBlock.As<int>();
		}
		if (info.TryGetProperty(PropertyName.PredictedLoss, out var _value_PredictedLoss))
		{
			PredictedLoss = _value_PredictedLoss.As<int>();
		}
	}

	public override void _Draw()
	{
		if (SourceHeroUnit > 0f)
		{
			DrawSourceHeroBar(SourceHeroUnit);
			return;
		}
		if (SourceInsetUnit > 0f)
		{
			float u = SourceInsetUnit;
			DrawRect(new Rect2(Vector2.Zero, Size), SourcePalette.Ink);
			DrawRect(new Rect2(Vector2.One * 2f * u, (Size - Vector2.One * 4f * u).Max(Vector2.Zero)), SourcePalette.Battle.InsetHealthTrack);
			Vector2 inner = (Size - Vector2.One * 6f * u).Max(Vector2.Zero);
			DrawRect(new Rect2(Vector2.One * 3f * u, new Vector2(inner.X * Math.Clamp((float)Hp / (float)Math.Max(1, Maximum), 0f, 1f), inner.Y)), new Color("bf625d"));
			return;
		}
		DrawRect(new Rect2(Vector2.Zero, Size), SourcePalette.Battle.HealthTrack);
		float width = Size.X * Math.Clamp((float)Hp / (float)Math.Max(1, Maximum), 0f, 1f);
		DrawRect(new Rect2(Vector2.Zero, new Vector2(width, Size.Y)), new Color("bf625d"));
		float loss = Size.X * Math.Clamp((float)Math.Min(Hp, Math.Max(0, PredictedLoss)) / (float)Math.Max(1, Maximum), 0f, 1f);
		if (loss > 0f)
		{
			Rect2 r = new Rect2(width - loss, 0f, loss, Size.Y);
			DrawRect(r, new Color("eadfc499"));
			for (float x = r.Position.X; x < r.End.X; x += 6f)
			{
				DrawLine(new Vector2(x, 0f), new Vector2(Math.Min(x + Size.Y * 0.35f, r.End.X), Size.Y), SourcePalette.Paper, 1f);
			}
		}
		DrawRect(new Rect2(Vector2.Zero, Size), SourcePalette.Battle.Edge, filled: false, 2f);
	}

	private void DrawSourceHeroBar(float u)
	{
		DrawRect(new Rect2(Vector2.Zero, Size), SourcePalette.Ink);
		DrawRect(new Rect2(Vector2.One * 2f * u, (Size - Vector2.One * 4f * u).Max(Vector2.Zero)), SourcePalette.Battle.InsetHealthTrack);
		Rect2 inner = new Rect2(Vector2.One * 3f * u, (Size - Vector2.One * 6f * u).Max(Vector2.Zero));
		float now = ((Maximum > 0) ? Math.Clamp((float)Hp / (float)Maximum, 0f, 1f) : 0f);
		DrawRect(new Rect2(inner.Position, new Vector2(inner.Size.X * now, inner.Size.Y)), new Color("bf625d"));
		float to = ((Maximum > 0) ? Math.Clamp((float)(Hp - Math.Max(0, PredictedLoss)) / (float)Maximum, 0f, 1f) : 0f);
		if (PredictedLoss <= 0 || to >= now || inner.Size.Y <= 0f)
		{
			return;
		}
		Rect2 loss = new Rect2(inner.Position + new Vector2(inner.Size.X * to, 0f), new Vector2(inner.Size.X * (now - to), inner.Size.Y));
		DrawRect(loss, new Color("e1ae9e"));
		Color ink = SourcePalette.Ink;
		ink.A = 0.35f;
		for (int y = 0; y < (int)Math.Ceiling(loss.Size.Y / u); y++)
		{
			float top = Math.Max(loss.Position.Y, loss.End.Y - (float)(y + 1) * u);
			float height = Math.Min(u, loss.End.Y - (float)y * u - top);
			for (float x = (8 - y % 8) % 8 - 8; x < loss.Size.X / u; x += 8f)
			{
				float left = Math.Max(0f, x * u);
				float right = Math.Min(loss.Size.X, (x + 2f) * u);
				if (right > left && height > 0f)
				{
					DrawRect(new Rect2(loss.Position.X + left, top, right - left, height), ink);
				}
			}
		}
		if (to > 0f)
		{
			DrawRect(new Rect2(loss.Position.X - u, inner.Position.Y - 3f * u, 2f * u, inner.Size.Y + 6f * u), SourcePalette.Battle.Edge);
		}
	}
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.Collections;
using Godot.NativeInterop;

namespace DeckRogue.GodotPort.UI;

[ScriptPath("res://scripts/ui/SourceHudLabel.cs")]
public class SourceHudLabel : Label
{
	public new class MethodName : Label.MethodName
	{
		public new static readonly StringName _Ready = "_Ready";

		public new static readonly StringName _Process = "_Process";

		public static readonly StringName RefreshSourceAlignment = "RefreshSourceAlignment";

		public static readonly StringName MeasureGlyphs = "MeasureGlyphs";

		public static readonly StringName Spaced = "Spaced";

		public static readonly StringName NightHalo = "NightHalo";
	}

	public new class PropertyName : Label.PropertyName
	{
		public static readonly StringName SourceTextRect = "SourceTextRect";

		public static readonly StringName FollowParentRect = "FollowParentRect";

		public static readonly StringName PaintedGlyphBoundsInParent = "PaintedGlyphBoundsInParent";

		public static readonly StringName _sourceTextRect = "_sourceTextRect";

		public static readonly StringName _measuredText = "_measuredText";

		public static readonly StringName _measuredFont = "_measuredFont";

		public static readonly StringName _measuredSize = "_measuredSize";

		public static readonly StringName _glyphs = "_glyphs";
	}

	public new class SignalName : Label.SignalName
	{
	}

	private Rect2 _sourceTextRect;

	private string _measuredText = "";

	private ulong _measuredFont;

	private int _measuredSize;

	private Rect2 _glyphs;

	private Label _separateUnderlay;

	private ColorRect _separateBacking;

	private float _separateBackingPad;

	public Rect2 SourceTextRect
	{
		get
		{
			if (!FollowParentRect || !(GetParent() is Control p))
			{
				return _sourceTextRect;
			}
			return new Rect2(Vector2.Zero, p.Size);
		}
		set
		{
			_sourceTextRect = value;
		}
	}

	public bool FollowParentRect { get; set; }

	public Rect2 PaintedGlyphBoundsInParent { get; private set; }

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		return new List<MethodInfo>(6)
		{
			new MethodInfo(MethodName._Ready, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName._Process, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Float, "delta", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.RefreshSourceAlignment, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.MeasureGlyphs, new PropertyInfo(Variant.Type.Rect2, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, "font", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Font"), exported: false),
				new PropertyInfo(Variant.Type.Int, "size", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.String, "text", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.Spaced, new PropertyInfo(Variant.Type.Object, "", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Font"), exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, "font", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Font"), exported: false),
				new PropertyInfo(Variant.Type.Int, "size", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "hundredthsEm", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.NightHalo, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal | MethodFlags.Static, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Object, "label", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Label"), exported: false),
				new PropertyInfo(Variant.Type.Object, "font", PropertyHint.None, "", PropertyUsageFlags.Default, new StringName("Font"), exported: false),
				new PropertyInfo(Variant.Type.Float, "unit", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "textScale", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Float, "tracking", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false),
				new PropertyInfo(Variant.Type.Bool, "deferSmallUnderlay", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
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
		if (method == MethodName._Process && args.Count == 1)
		{
			_Process(VariantUtils.ConvertTo<double>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.RefreshSourceAlignment && args.Count == 0)
		{
			RefreshSourceAlignment();
			ret = default;
			return true;
		}
		if (method == MethodName.MeasureGlyphs && args.Count == 3)
		{
			ret = VariantUtils.CreateFrom<Rect2>(MeasureGlyphs(VariantUtils.ConvertTo<Font>(in args[0]), VariantUtils.ConvertTo<int>(in args[1]), VariantUtils.ConvertTo<string>(in args[2])));
			return true;
		}
		if (method == MethodName.Spaced && args.Count == 3)
		{
			ret = VariantUtils.CreateFrom<Font>(Spaced(VariantUtils.ConvertTo<Font>(in args[0]), VariantUtils.ConvertTo<int>(in args[1]), VariantUtils.ConvertTo<float>(in args[2])));
			return true;
		}
		if (method == MethodName.NightHalo && args.Count == 6)
		{
			NightHalo(VariantUtils.ConvertTo<Label>(in args[0]), VariantUtils.ConvertTo<Font>(in args[1]), VariantUtils.ConvertTo<float>(in args[2]), VariantUtils.ConvertTo<float>(in args[3]), VariantUtils.ConvertTo<float>(in args[4]), VariantUtils.ConvertTo<bool>(in args[5]));
			ret = default;
			return true;
		}
		return base.InvokeGodotClassMethod(in method, args, out ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		if (method == MethodName.MeasureGlyphs && args.Count == 3)
		{
			ret = VariantUtils.CreateFrom<Rect2>(MeasureGlyphs(VariantUtils.ConvertTo<Font>(in args[0]), VariantUtils.ConvertTo<int>(in args[1]), VariantUtils.ConvertTo<string>(in args[2])));
			return true;
		}
		if (method == MethodName.Spaced && args.Count == 3)
		{
			ret = VariantUtils.CreateFrom<Font>(Spaced(VariantUtils.ConvertTo<Font>(in args[0]), VariantUtils.ConvertTo<int>(in args[1]), VariantUtils.ConvertTo<float>(in args[2])));
			return true;
		}
		if (method == MethodName.NightHalo && args.Count == 6)
		{
			NightHalo(VariantUtils.ConvertTo<Label>(in args[0]), VariantUtils.ConvertTo<Font>(in args[1]), VariantUtils.ConvertTo<float>(in args[2]), VariantUtils.ConvertTo<float>(in args[3]), VariantUtils.ConvertTo<float>(in args[4]), VariantUtils.ConvertTo<bool>(in args[5]));
			ret = default;
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
		if (method == MethodName._Process)
		{
			return true;
		}
		if (method == MethodName.RefreshSourceAlignment)
		{
			return true;
		}
		if (method == MethodName.MeasureGlyphs)
		{
			return true;
		}
		if (method == MethodName.Spaced)
		{
			return true;
		}
		if (method == MethodName.NightHalo)
		{
			return true;
		}
		return base.HasGodotClassMethod(in method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if (name == PropertyName.SourceTextRect)
		{
			SourceTextRect = VariantUtils.ConvertTo<Rect2>(in value);
			return true;
		}
		if (name == PropertyName.FollowParentRect)
		{
			FollowParentRect = VariantUtils.ConvertTo<bool>(in value);
			return true;
		}
		if (name == PropertyName.PaintedGlyphBoundsInParent)
		{
			PaintedGlyphBoundsInParent = VariantUtils.ConvertTo<Rect2>(in value);
			return true;
		}
		if (name == PropertyName._sourceTextRect)
		{
			_sourceTextRect = VariantUtils.ConvertTo<Rect2>(in value);
			return true;
		}
		if (name == PropertyName._measuredText)
		{
			_measuredText = VariantUtils.ConvertTo<string>(in value);
			return true;
		}
		if (name == PropertyName._measuredFont)
		{
			_measuredFont = VariantUtils.ConvertTo<ulong>(in value);
			return true;
		}
		if (name == PropertyName._measuredSize)
		{
			_measuredSize = VariantUtils.ConvertTo<int>(in value);
			return true;
		}
		if (name == PropertyName._glyphs)
		{
			_glyphs = VariantUtils.ConvertTo<Rect2>(in value);
			return true;
		}
		return base.SetGodotClassPropertyValue(in name, in value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		Rect2 from;
		if (name == PropertyName.SourceTextRect)
		{
			from = SourceTextRect;
			value = VariantUtils.CreateFrom(in from);
			return true;
		}
		if (name == PropertyName.FollowParentRect)
		{
			value = VariantUtils.CreateFrom<bool>(FollowParentRect);
			return true;
		}
		if (name == PropertyName.PaintedGlyphBoundsInParent)
		{
			from = PaintedGlyphBoundsInParent;
			value = VariantUtils.CreateFrom(in from);
			return true;
		}
		if (name == PropertyName._sourceTextRect)
		{
			value = VariantUtils.CreateFrom(in _sourceTextRect);
			return true;
		}
		if (name == PropertyName._measuredText)
		{
			value = VariantUtils.CreateFrom(in _measuredText);
			return true;
		}
		if (name == PropertyName._measuredFont)
		{
			value = VariantUtils.CreateFrom(in _measuredFont);
			return true;
		}
		if (name == PropertyName._measuredSize)
		{
			value = VariantUtils.CreateFrom(in _measuredSize);
			return true;
		}
		if (name == PropertyName._glyphs)
		{
			value = VariantUtils.CreateFrom(in _glyphs);
			return true;
		}
		return base.GetGodotClassPropertyValue(in name, out value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		return new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Rect2, PropertyName._sourceTextRect, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Rect2, PropertyName.SourceTextRect, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Bool, PropertyName.FollowParentRect, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Rect2, PropertyName.PaintedGlyphBoundsInParent, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.String, PropertyName._measuredText, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName._measuredFont, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Int, PropertyName._measuredSize, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Rect2, PropertyName._glyphs, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		base.SaveGodotObjectData(info);
		info.AddProperty(PropertyName.SourceTextRect, Variant.From<Rect2>(SourceTextRect));
		info.AddProperty(PropertyName.FollowParentRect, Variant.From<bool>(FollowParentRect));
		info.AddProperty(PropertyName.PaintedGlyphBoundsInParent, Variant.From<Rect2>(PaintedGlyphBoundsInParent));
		info.AddProperty(PropertyName._sourceTextRect, Variant.From(in _sourceTextRect));
		info.AddProperty(PropertyName._measuredText, Variant.From(in _measuredText));
		info.AddProperty(PropertyName._measuredFont, Variant.From(in _measuredFont));
		info.AddProperty(PropertyName._measuredSize, Variant.From(in _measuredSize));
		info.AddProperty(PropertyName._glyphs, Variant.From(in _glyphs));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		base.RestoreGodotObjectData(info);
		if (info.TryGetProperty(PropertyName.SourceTextRect, out var _value_SourceTextRect))
		{
			SourceTextRect = _value_SourceTextRect.As<Rect2>();
		}
		if (info.TryGetProperty(PropertyName.FollowParentRect, out var _value_FollowParentRect))
		{
			FollowParentRect = _value_FollowParentRect.As<bool>();
		}
		if (info.TryGetProperty(PropertyName.PaintedGlyphBoundsInParent, out var _value_PaintedGlyphBoundsInParent))
		{
			PaintedGlyphBoundsInParent = _value_PaintedGlyphBoundsInParent.As<Rect2>();
		}
		if (info.TryGetProperty(PropertyName._sourceTextRect, out var _value__sourceTextRect))
		{
			_sourceTextRect = _value__sourceTextRect.As<Rect2>();
		}
		if (info.TryGetProperty(PropertyName._measuredText, out var _value__measuredText))
		{
			_measuredText = _value__measuredText.As<string>();
		}
		if (info.TryGetProperty(PropertyName._measuredFont, out var _value__measuredFont))
		{
			_measuredFont = _value__measuredFont.As<ulong>();
		}
		if (info.TryGetProperty(PropertyName._measuredSize, out var _value__measuredSize))
		{
			_measuredSize = _value__measuredSize.As<int>();
		}
		if (info.TryGetProperty(PropertyName._glyphs, out var _value__glyphs))
		{
			_glyphs = _value__glyphs.As<Rect2>();
		}
	}

	public void EnableSeparateUnderlay(float unit, float textScale)
	{
		if (GetParent() is Control parent && (_separateUnderlay == null || !GodotObject.IsInstanceValid(_separateUnderlay)))
		{
			_separateUnderlay = new Label
			{
				Name = "HpReadoutUnderlay",
				MouseFilter = MouseFilterEnum.Ignore,
				AutowrapMode = TextServer.AutowrapMode.Off,
				HorizontalAlignment = HorizontalAlignment,
				VerticalAlignment = VerticalAlignment
			};
			_separateUnderlay.AddThemeFontOverride("font", GetThemeFont("font"));
			_separateUnderlay.AddThemeFontSizeOverride("font_size", GetThemeFontSize("font_size"));
			_separateUnderlay.AddThemeColorOverride("font_color", SourcePalette.Ground);
			_separateUnderlay.AddThemeColorOverride("font_outline_color", SourcePalette.Ground);
			_separateUnderlay.AddThemeColorOverride("font_shadow_color", Colors.Transparent);
			_separateUnderlay.AddThemeConstantOverride("outline_size", Math.Max(1, (int)Math.Round(2f * unit * textScale)));
			_separateUnderlay.SetMeta("source_rendering_adaptation", "NumHaloNight separate filled underlay; native A/B required");
			parent.AddChild(_separateUnderlay, forceReadableName: false, InternalMode.Disabled);
			parent.MoveChild(_separateUnderlay, GetIndex());
			_separateBackingPad = Math.Max(1f, unit);
			_separateBacking = new ColorRect
			{
				Name = "HpReadoutBacking",
				Color = SourcePalette.Ground,
				MouseFilter = MouseFilterEnum.Ignore,
				Visible = false
			};
			_separateBacking.SetMeta("source_rendering_adaptation", "Opaque local NumHaloNight backing only over predicted loss; not Unity plate geometry");
			parent.AddChild(_separateBacking, forceReadableName: false, InternalMode.Disabled);
			parent.MoveChild(_separateBacking, _separateUnderlay.GetIndex());
			RefreshSourceAlignment();
		}
	}

	public override void _Ready()
	{
		RefreshSourceAlignment();
	}

	public override void _Process(double delta)
	{
		RefreshSourceAlignment();
	}

	public void RefreshSourceAlignment()
	{
		if (!IsInsideTree())
		{
			return;
		}
		Font font = GetThemeFont("font");
		int size = GetThemeFontSize("font_size");
		if (font == null || size <= 0)
		{
			return;
		}
		Rect2 rect = ((FollowParentRect && GetParent() is Control p) ? new Rect2(Vector2.Zero, p.Size) : SourceTextRect);
		if (!(rect.Size.X <= 0f) && !(rect.Size.Y <= 0f))
		{
			if (_measuredText != Text || _measuredFont != font.GetInstanceId() || _measuredSize != size)
			{
				_measuredText = Text;
				_measuredFont = font.GetInstanceId();
				_measuredSize = size;
				_glyphs = MeasureGlyphs(font, size, Text);
			}
			Size = new Vector2(rect.Size.X, Math.Max(rect.Size.Y, GetCombinedMinimumSize().Y));
			float baseline = ((Text.Length > 0) ? GetCharacterBounds(0).Position.Y : 0f) + font.GetAscent(size);
			float y = rect.GetCenter().Y - baseline - _glyphs.GetCenter().Y;
			Position = new Vector2(rect.Position.X, y);
			float advance = font.GetStringSize(Text, HorizontalAlignment.Left, -1f, size, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal).X;
			float x;
			if (HorizontalAlignment == HorizontalAlignment.Center)
			{
				x = (Size.X - advance) / 2f;
			}
			else
			{
				x = ((HorizontalAlignment == HorizontalAlignment.Right) ? (Size.X - advance) : 0f);
			}
			Rect2 ink = new Rect2(Position + new Vector2(x, baseline) + _glyphs.Position, _glyphs.Size);
			int halo = ((GetThemeColor("font_shadow_color").A > 0f) ? GetThemeConstant("shadow_outline_size") : 0);
			PaintedGlyphBoundsInParent = ink.Grow(halo);
			if (_separateUnderlay != null && GodotObject.IsInstanceValid(_separateUnderlay))
			{
				_separateUnderlay.Text = Text;
				_separateUnderlay.Size = Size;
				_separateUnderlay.Position = Position;
				_separateUnderlay.Scale = Scale;
				_separateUnderlay.Visible = false;
			}
			if (_separateBacking != null && GodotObject.IsInstanceValid(_separateBacking))
			{
				Rect2 bounds = ink.Grow(_separateBackingPad);
				_separateBacking.Position = bounds.Position;
				_separateBacking.Size = bounds.Size;
				_separateBacking.ZIndex = ZIndex;
				_separateBacking.Visible = Visible && GetParent() is SourceHealthBar { Hp: >0 } bar && bar.PredictedLoss > 0;
			}
		}
	}

	public static Rect2 MeasureGlyphs(Font font, int size, string text)
	{
		TextServer server = TextServerManager.GetPrimaryInterface();
		Array<Rid> rids = font.GetRids();
		float left = float.PositiveInfinity;
		float top = float.PositiveInfinity;
		float right = float.NegativeInfinity;
		float bottom = float.NegativeInfinity;
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			if (char.IsWhiteSpace(c))
			{
				continue;
			}
			foreach (Rid rid in rids)
			{
				long glyph = server.FontGetGlyphIndex(rid, size, c, 0L);
				if (glyph != 0L)
				{
					Vector2 offset = server.FontGetGlyphOffset(rid, new Vector2I(size, 0), glyph);
					Vector2 extent = server.FontGetGlyphSize(rid, new Vector2I(size, 0), glyph);
					if (!(extent.X <= 0f) && !(extent.Y <= 0f))
					{
						float advance = ((i == 0) ? 0f : font.GetStringSize(text.Substring(0, i), HorizontalAlignment.Left, -1f, size, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal).X);
						left = Math.Min(left, advance + offset.X);
						right = Math.Max(right, advance + offset.X + extent.X);
						top = Math.Min(top, offset.Y);
						bottom = Math.Max(bottom, offset.Y + extent.Y);
					}
					break;
				}
			}
		}
		if (!float.IsPositiveInfinity(left))
		{
			return new Rect2(left, top, right - left, bottom - top);
		}
		return default;
	}

	public static Font Spaced(Font font, int size, float hundredthsEm)
	{
		return new FontVariation
		{
			BaseFont = font,
			SpacingGlyph = (int)Math.Round((float)size * hundredthsEm * 0.01f),
			ResourceName = font.ResourcePath.GetFile() + " source-tracking"
		};
	}

	public static void NightHalo(Label label, Font font, float unit, float textScale, float tracking = 0f, bool deferSmallUnderlay = false)
	{
		int size = label.GetThemeFontSize("font_size");
		FontVariation local = new FontVariation
		{
			BaseFont = font,
			VariationEmbolden = 0.15f,
			SpacingGlyph = (int)Math.Round((float)size * tracking * 0.01f),
			ResourceName = font.ResourcePath.GetFile() + " NumHaloNight"
		};
		label.AddThemeFontOverride("font", local);
		label.AddThemeColorOverride("font_shadow_color", deferSmallUnderlay ? Colors.Transparent : SourcePalette.Ground);
		label.AddThemeConstantOverride("shadow_offset_x", 0);
		label.AddThemeConstantOverride("shadow_offset_y", 0);
		label.AddThemeConstantOverride("shadow_outline_size", (!deferSmallUnderlay) ? Math.Max(1, (int)Math.Round(2f * unit * textScale)) : 0);
		label.SetMeta("source_underlay_deferred", deferSmallUnderlay);
		label.AddThemeConstantOverride("outline_size", 0);
	}
}

using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace DeckRogue.GodotPort.Presentation;

public static class SourceIncomingGlyphBounds
{
	public sealed record FontRasterMetric(ulong Rid, int FontSize, float FontOversampling, float EffectiveOversampling, int RasterFontSize, int RasterUpperFontSize, int SubpixelPhases, string Antialiasing, bool Msdf, int FixedSize);

	public sealed record Diagnostics(string EngineVersion, string TextServer, string FontResource, int FontSize, string Language, Vector2 ShapedSize, float Ascent, float Descent, float AdjustedAscent, float FontHeight, Vector2 Baseline, int OutlineSize, int ShadowOutlineSize, Vector2 ShadowOffset, float ViewportOversampling, float RoundingGuard, int ComparedClusters, float MaximumClusterDelta, IReadOnlyList<FontRasterMetric> Fonts);

	public sealed record Measurement(Rect2 Bounds, Vector2 NativeMinimum, int GlyphCount, string Mode, string FallbackReason, Diagnostics Metrics)
	{
		public bool IsFallback => Mode == "full_label_fallback";
	}

	private readonly record struct Glyph(int Start, int End, int Count, int Repeat, TextServer.GraphemeFlag Flags, Vector2 Offset, float Advance, Rid Font, int Size, long Index);

	private readonly record struct Layer(int Outline, Vector2 Offset);

	public const string NativeMode = "native_shaped_raster_quads";

	public const string FallbackMode = "full_label_fallback";

	public const string FractionalMode = "native_shaped_raster_quads_guarded_fractional";

	private const float RasterGuard = 2f;

	private const float MatchTolerance = 0.05f;

	private const int MaximumGlyphs = 1024;

	public static Measurement Measure(Label label)
	{
		ArgumentNullException.ThrowIfNull(label, "label");
		Vector2 minimum = Vector2.Zero;
		Vector2 size = label.Size;
		int fontSize = 0;
		int outline = 0;
		int shadowOutline = 0;
		int glyphCount = 0;
		int compared = 0;
		float fontHeight = 0f;
		float ascent = 0f;
		float descent = 0f;
		float adjustedAscent = 0f;
		float maxDelta = 0f;
		float viewportOversampling = 0f;
		float roundingGuard = 2f;
		bool fractionalRaster = false;
		Vector2 shapedSize = default;
		Vector2 baseline = default;
		Vector2 shadow = default;
		string engineVersion = "unknown";
		string serverName = "unknown";
		string fontResource = "";
		string language = "";
		List<FontRasterMetric> fontMetrics = new List<FontRasterMetric>();
		List<Layer> layers = new List<Layer>();
		Rect2 partial = default;
		bool hasPartial = false;
		float fallbackEffectPad = 2f;
		try
		{
			if (!GodotObject.IsInstanceValid(label))
			{
				return Fallback("invalid_label");
			}
			minimum = label.GetCombinedMinimumSize();
			if (!FinitePositive(size) || !FinitePositive(minimum))
			{
				return Fallback("nonfinite_or_empty_geometry");
			}
			if (!label.IsInsideTree())
			{
				return Fallback("label_not_in_tree");
			}
			LabelSettings settings = label.LabelSettings;
			Font font = settings?.Font ?? label.GetThemeFont("font");
			fontSize = settings?.FontSize ?? label.GetThemeFontSize("font_size");
			if (font == null || fontSize <= 0 || fontSize > 1024)
			{
				return Fallback("invalid_font");
			}
			fontResource = font.ResourcePath;
			fontHeight = font.GetHeight(fontSize);
			if (!float.IsFinite(fontHeight) || fontHeight <= 0f)
			{
				return Fallback("invalid_font_height");
			}
			outline = Math.Max(0, settings?.OutlineSize ?? label.GetThemeConstant("outline_size"));
			shadowOutline = Math.Max(0, settings?.ShadowSize ?? label.GetThemeConstant("shadow_outline_size"));
			shadow = settings?.ShadowOffset ?? new Vector2(label.GetThemeConstant("shadow_offset_x"), label.GetThemeConstant("shadow_offset_y"));
			fallbackEffectPad += (float)Math.Max(outline, shadowOutline) + Math.Max(Math.Abs(shadow.X), Math.Abs(shadow.Y));
			if (settings != null)
			{
				for (int i = 0; i < settings.StackedOutlineCount; i++)
				{
					fallbackEffectPad += Math.Max(0, settings.GetStackedOutlineSize(i));
				}
				for (int j = 0; j < settings.StackedShadowCount; j++)
				{
					Vector2 offset = settings.GetStackedShadowOffset(j);
					fallbackEffectPad = Math.Max(fallbackEffectPad, 2f + (float)Math.Max(0, settings.GetStackedShadowOutlineSize(j)) + Math.Max(Math.Abs(offset.X), Math.Abs(offset.Y)));
				}
				if (settings.StackedOutlineCount != 0 || settings.StackedShadowCount != 0)
				{
					return Fallback("stacked_effects");
				}
			}
			if (!shadow.IsFinite() || !float.IsFinite(fallbackEffectPad) || outline > 256 || shadowOutline > 256)
			{
				return Fallback("unsupported_effect_extent");
			}
			Color color = settings?.FontColor ?? label.GetThemeColor("font_color");
			Color outlineColor = settings?.OutlineColor ?? label.GetThemeColor("font_outline_color");
			Color shadowColor = settings?.ShadowColor ?? label.GetThemeColor("font_shadow_color");
			if (color.A > 0f)
			{
				layers.Add(new Layer(0, Vector2.Zero));
			}
			if (outline > 0 && outlineColor.A != 0f)
			{
				layers.Add(new Layer(outline, Vector2.Zero));
			}
			if (shadowColor.A > 0f)
			{
				layers.Add(new Layer(0, shadow));
			}
			if (shadowOutline > 0 && shadowColor.A != 0f)
			{
				layers.Add(new Layer(shadowOutline, shadow));
			}
			Dictionary version = Godot.Engine.GetVersionInfo();
			engineVersion = version["string"].AsString();
			if (version["major"].AsInt32() != 4 || version["minor"].AsInt32() != 6 || version["patch"].AsInt32() != 3)
			{
				return Fallback("unverified_engine_version");
			}
			TextServer server = TextServerManager.GetPrimaryInterface();
			serverName = server.GetName();
			if (server.GetClass() != "TextServerAdvanced")
			{
				return Fallback("unsupported_text_server");
			}
			viewportOversampling = label.GetViewport().GetOversampling();
			if (!float.IsFinite(viewportOversampling) || viewportOversampling <= 0f)
			{
				return Fallback("unknown_viewport_oversampling");
			}
			if (label.AutowrapMode != TextServer.AutowrapMode.Off || label.ClipText || label.TextOverrunBehavior != TextServer.OverrunBehavior.NoTrimming || label.HorizontalAlignment == HorizontalAlignment.Fill || label.VerticalAlignment == VerticalAlignment.Fill || label.VisibleCharacters >= 0 || label.VisibleRatio < 1f || label.LinesSkipped != 0 || label.MaxLinesVisible == 0 || label.StructuredTextBidiOverride != TextServer.StructuredTextParser.Default || label.TabStops.Length != 0)
			{
				return Fallback("unsupported_label_layout");
			}
			if (label.GetLineCount() != 1 || label.GetVisibleLineCount() != 1)
			{
				return Fallback("multiline_or_hidden_line");
			}
			string text = label.Atr(label.Text);
			language = (string.IsNullOrEmpty(label.Language) ? TranslationServer.GetLocale() : label.Language);
			if (label.Uppercase)
			{
				text = server.StringToUpper(text, language);
			}
			if (text.Length == 0 || text.Length > 1024 || text.IndexOfAny(new char[5] { '\r', '\n', '\t', '\u2028', '\u2029' }) >= 0)
			{
				return Fallback("empty_multiline_or_large_text");
			}
			if (label.ParagraphSeparator != "\\n" && label.ParagraphSeparator != "\n")
			{
				return Fallback("custom_paragraph_separator");
			}
			if (label.Material != null || label.UseParentMaterial)
			{
				return Fallback("custom_material");
			}
			StyleBox style = label.GetThemeStylebox("normal");
			Vector2 styleMinimum = style.GetMinimumSize();
			Vector2 styleOffset = style.GetOffset();
			if (!styleMinimum.IsFinite() || !styleOffset.IsFinite())
			{
				return Fallback("invalid_style_metrics");
			}
			TextServer.Direction direction;
			if (label.TextDirection != Control.TextDirection.Inherited)
			{
				direction = (TextServer.Direction)(int)label.TextDirection;
			}
			else
			{
				direction = (TextServer.Direction)(label.IsLayoutRtl() ? 2 : 1);
			}
			using TextLine line = new TextLine
			{
				Direction = direction,
				Orientation = TextServer.Orientation.Horizontal,
				PreserveInvalid = true,
				PreserveControl = false,
				Alignment = HorizontalAlignment.Left
			};
			if (!line.AddString(text + "\u200b", font, fontSize, language))
			{
				return Fallback("shaping_failed");
			}
			int[] breaks = server.ShapedTextGetLineBreaks(line.GetRid(), (int)(size.X - styleMinimum.X), 0L, TextServer.LineBreakFlag.Mandatory | label.AutowrapTrimFlags);
			if (breaks.Length != 2)
			{
				return Fallback("line_break_mismatch");
			}
			Rid shaped = server.ShapedTextSubstr(line.GetRid(), breaks[0], breaks[1] - breaks[0]);
			if (!shaped.IsValid)
			{
				return Fallback("invalid_shaped_rid");
			}
			try
			{
				shapedSize = server.ShapedTextGetSize(shaped);
				ascent = (float)server.ShapedTextGetAscent(shaped);
				descent = (float)server.ShapedTextGetDescent(shaped);
				if (!shapedSize.IsFinite() || !float.IsFinite(ascent) || !float.IsFinite(descent))
				{
					return Fallback("nonfinite_shaping");
				}
				adjustedAscent = ascent + Math.Max(0f, (float)(int)fontHeight - ascent - descent) / 2f;
				float lineHeight = Math.Max(ascent + descent, (int)fontHeight);
				HorizontalAlignment horizontalAlignment = label.HorizontalAlignment;
				if ((ulong)horizontalAlignment > 2uL)
				{
					goto IL_0a83;
				}
				switch ((int)horizontalAlignment)
				{
				case 1:
					break;
				case 2:
					goto IL_0a25;
				case 0:
					goto IL_0a54;
				default:
					goto IL_0a83;
				}
				float num = (int)(size.X - shapedSize.X) / 2;
				goto IL_0a8c;
				IL_0a8c:
				float x = num;
				float heightWithMargins = lineHeight + style.GetMargin(Side.Top) + style.GetMargin(Side.Bottom);
				num = styleOffset.Y;
				float y = num + (float)(label.VerticalAlignment switch
				{
					VerticalAlignment.Center => (int)((size.Y - heightWithMargins) / 2f), 
					VerticalAlignment.Bottom => (int)(size.Y - heightWithMargins), 
					_ => 0, 
				});
				baseline = new Vector2(x, y + adjustedAscent);
				Array<Dictionary> raw = server.ShapedTextGetGlyphs(shaped);
				if (raw.Count == 0 || raw.Count > 1024)
				{
					return Fallback("invalid_glyph_count");
				}
				List<Glyph> glyphs = new List<Glyph>(raw.Count);
				foreach (Dictionary g in raw)
				{
					glyphs.Add(new Glyph(g["start"].AsInt32(), g["end"].AsInt32(), g["count"].AsInt32(), g["repeat"].AsInt32(), (TextServer.GraphemeFlag)g["flags"].AsInt64(), g["offset"].AsVector2(), g["advance"].AsSingle(), g["font_rid"].AsRid(), g["font_size"].AsInt32(), g["index"].AsInt64()));
				}
				float pen = 0f;
				System.Collections.Generic.Dictionary<(Rid, int), FontRasterMetric> rasterByFont = new System.Collections.Generic.Dictionary<(Rid, int), FontRasterMetric>();
				HashSet<(int, int)> verifiedClusters = new HashSet<(int, int)>();
				for (int k = 0; k < glyphs.Count; k++)
				{
					Glyph g2 = glyphs[k];
					if (!g2.Offset.IsFinite() || !float.IsFinite(g2.Advance) || g2.Repeat < 0 || g2.Repeat > 1024 || g2.Count < 0 || k + g2.Count > glyphs.Count)
					{
						return Fallback("invalid_glyph_metrics");
					}
					if (g2.Count > 0 && (g2.Index != 0L || g2.Flags.HasFlag(TextServer.GraphemeFlag.Space)) && !IsEmptyDuplicateCluster(g2.Index, g2.Advance, verifiedClusters.Contains((g2.Start, g2.End))))
					{
						float clusterWidth = 0f;
						for (int l = 0; l < g2.Count; l++)
						{
							clusterWidth += glyphs[k + l].Advance;
						}
						Rect2 a = new Rect2(new Vector2(x + pen, y), new Vector2(clusterWidth, lineHeight));
						Rect2 actual = label.GetCharacterBounds(g2.Start);
						float error = Delta(a, actual);
						maxDelta = Math.Max(maxDelta, error);
						compared++;
						if (!float.IsFinite(error) || error > 0.05f)
						{
							return Fallback("native_character_bounds_mismatch");
						}
						verifiedClusters.Add((g2.Start, g2.End));
					}
					bool nonvisual = g2.Index == 0L || g2.Flags.HasFlag(TextServer.GraphemeFlag.Virtual) || g2.Flags.HasFlag(TextServer.GraphemeFlag.Space);
					if (!g2.Font.IsValid)
					{
						if (!nonvisual)
						{
							return Fallback("unresolved_glyph_font");
						}
						pen += g2.Advance * (float)g2.Repeat;
						continue;
					}
					if (g2.Index == 0L)
					{
						pen += g2.Advance * (float)g2.Repeat;
						continue;
					}
					if (!rasterByFont.TryGetValue((g2.Font, g2.Size), out var fm))
					{
						float fontOversampling = (float)server.FontGetOversampling(g2.Font);
						bool msdf = server.FontIsMultichannelSignedDistanceField(g2.Font);
						int fixedSize = (int)server.FontGetFixedSize(g2.Font);
						float effective = ((fontOversampling > 0f) ? fontOversampling : viewportOversampling);
						effective = (float)(int)(Math.Clamp(effective, 0.1f, 100f) * 64f) / 64f;
						float rasterSize = (float)g2.Size * effective;
						TextServer.SubpixelPositioning subpixel = server.FontGetSubpixelPositioning(g2.Font);
						int phases = ((subpixel == TextServer.SubpixelPositioning.OneQuarter || (subpixel == TextServer.SubpixelPositioning.Auto && rasterSize <= 16f)) ? 4 : ((subpixel != TextServer.SubpixelPositioning.OneHalf && (subpixel != TextServer.SubpixelPositioning.Auto || !(rasterSize <= 20f))) ? 1 : 2));
						fm = new FontRasterMetric(g2.Font.Id, g2.Size, fontOversampling, effective, float.IsFinite(rasterSize) ? ((int)Math.Floor(rasterSize)) : 0, float.IsFinite(rasterSize) ? ((int)Math.Ceiling(rasterSize)) : 0, phases, server.FontGetAntialiasing(g2.Font).ToString(), msdf, fixedSize);
						fontMetrics.Add(fm);
						if (!float.IsFinite(fontOversampling) || !float.IsFinite(effective) || g2.Size <= 0 || rasterSize <= 0f || rasterSize > 2048f)
						{
							return Fallback("invalid_raster_scale");
						}
						if (msdf || fixedSize != 0)
						{
							return Fallback("unsupported_font_raster_mode");
						}
						if (fm.RasterFontSize != fm.RasterUpperFontSize)
						{
							if (fm.RasterFontSize < 1 || server.FontGetAntialiasing(g2.Font) != TextServer.FontAntialiasing.Gray || server.FontGetTransform(g2.Font) != Transform2D.Identity || Math.Abs(server.FontGetEmbolden(g2.Font)) > 0.0001)
							{
								return Fallback("unsupported_fractional_raster_mode");
							}
							fractionalRaster = true;
							roundingGuard = Math.Max(roundingGuard, 2f + 2f / effective);
						}
						rasterByFont.Add((g2.Font, g2.Size), fm);
					}
					bool glyphPaints = false;
					for (int repeat = 0; repeat < g2.Repeat; repeat++)
					{
						Vector2 at = baseline + new Vector2(pen, 0f) + g2.Offset;
						foreach (Layer layer in layers)
						{
							for (int raster = fm.RasterFontSize; raster <= fm.RasterUpperFontSize; raster++)
							{
								Vector2I cacheSize = new Vector2I(raster, (int)((float)layer.Outline * fm.EffectiveOversampling));
								for (int phase = 0; phase < fm.SubpixelPhases; phase++)
								{
									long index = (g2.Index & 0xFFFFFF) | ((long)phase << 27);
									Vector2 offset2 = server.FontGetGlyphOffset(g2.Font, cacheSize, index) / fm.EffectiveOversampling;
									Vector2 extent = server.FontGetGlyphSize(g2.Font, cacheSize, index) / fm.EffectiveOversampling;
									if (!offset2.IsFinite() || !extent.IsFinite() || extent.X < 0f || extent.Y < 0f)
									{
										return Fallback("invalid_raster_quad");
									}
									if (extent.X != 0f && extent.Y != 0f)
									{
										Rect2 quad = new Rect2(at + layer.Offset + offset2, extent);
										partial = (hasPartial ? partial.Merge(quad) : quad);
										hasPartial = true;
										glyphPaints = true;
									}
								}
							}
						}
						pen += g2.Advance;
					}
					if (!glyphPaints && !nonvisual && layers.Count > 0)
					{
						return Fallback("unresolved_raster_quad");
					}
					if (glyphPaints)
					{
						glyphCount += g2.Repeat;
					}
				}
				if (!float.IsFinite(pen) || Math.Abs(pen - shapedSize.X) > 0.05f)
				{
					return Fallback("shaped_advance_mismatch");
				}
				if (compared == 0)
				{
					return Fallback("no_native_cluster_comparison");
				}
				return new Measurement(hasPartial ? RoundOut(partial.Grow(roundingGuard)) : default(Rect2), minimum, glyphCount, fractionalRaster ? "native_shaped_raster_quads_guarded_fractional" : "native_shaped_raster_quads", "", Metrics());
				IL_0a83:
				num = styleOffset.X;
				goto IL_0a8c;
				IL_0a25:
				if (label.IsLayoutRtl())
				{
					goto IL_0a83;
				}
				num = (int)(size.X - style.GetMargin(Side.Right) - shapedSize.X);
				goto IL_0a8c;
				IL_0a54:
				if (!label.IsLayoutRtl())
				{
					goto IL_0a83;
				}
				num = (int)(size.X - style.GetMargin(Side.Right) - shapedSize.X);
				goto IL_0a8c;
			}
			finally
			{
				server.FreeRid(shaped);
			}
		}
		catch (Exception ex) when (!(ex is OutOfMemoryException))
		{
			return Fallback("measurement_exception:" + ex.GetType().Name);
		}
		Measurement Fallback(string reason)
		{
			Vector2 finiteSize = (FinitePositive(size) ? size : Vector2.One);
			Vector2 finiteMin = (FinitePositive(minimum) ? minimum : finiteSize);
			Vector2 fullSize = finiteSize.Max(finiteMin);
			Rect2 rect = new Rect2((finiteSize - fullSize) / 2f, fullSize);
			float pad = fallbackEffectPad + Math.Max(1f, Math.Max(fontSize, fontHeight));
			rect = rect.Grow(float.IsFinite(pad) ? pad : 258f);
			if (hasPartial)
			{
				rect = rect.Merge(partial.Grow(2f));
			}
			return new Measurement(RoundOut(rect), minimum, glyphCount, "full_label_fallback", reason, Metrics());
		}
		Diagnostics Metrics()
		{
			return new Diagnostics(engineVersion, serverName, fontResource, fontSize, language, shapedSize, ascent, descent, adjustedAscent, fontHeight, baseline, outline, shadowOutline, shadow, viewportOversampling, roundingGuard, compared, maxDelta, System.Array.AsReadOnly(fontMetrics.ToArray()));
		}
	}

	internal static bool IsEmptyDuplicateCluster(long glyph, float advance, bool verified)
	{
		return (glyph == 0L && advance == 0f) & verified;
	}

	private static bool FinitePositive(Vector2 value)
	{
		if (value.IsFinite() && value.X > 0f)
		{
			return value.Y > 0f;
		}
		return false;
	}

	private static float Delta(Rect2 a, Rect2 b)
	{
		return Math.Max(Math.Max(Math.Abs(a.Position.X - b.Position.X), Math.Abs(a.Position.Y - b.Position.Y)), Math.Max(Math.Abs(a.Size.X - b.Size.X), Math.Abs(a.Size.Y - b.Size.Y)));
	}

	private static Rect2 RoundOut(Rect2 rect)
	{
		Vector2 low = new Vector2(MathF.Floor(rect.Position.X), MathF.Floor(rect.Position.Y));
		Vector2 high = new Vector2(MathF.Ceiling(rect.End.X), MathF.Ceiling(rect.End.Y));
		return new Rect2(low, high - low);
	}
}

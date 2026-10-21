using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;

namespace DeckRogue.GodotPort.UI;

[ScriptPath("res://scripts/ui/SourceCardBody.cs")]
internal class SourceCardBody : Control
{
	public new class MethodName : Control.MethodName
	{
		public new static readonly StringName _Ready = "_Ready";

		public static readonly StringName AtSize = "AtSize";

		public static readonly StringName Fit = "Fit";
	}

	public new class PropertyName : Control.PropertyName
	{
		public static readonly StringName _text = "_text";

		public static readonly StringName _markup = "_markup";

		public static readonly StringName _scale = "_scale";
	}

	public new class SignalName : Control.SignalName
	{
	}

	private RichTextLabel? _text;
    private static Font? _cardStrongFont;

	private string _markup = "";

	private float _scale = 1f;

	private Action<bool>? _truncated;

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		return new List<MethodInfo>(3)
		{
			new MethodInfo(MethodName._Ready, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null),
			new MethodInfo(MethodName.AtSize, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, new List<PropertyInfo>
			{
				new PropertyInfo(Variant.Type.Int, "fontSize", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false)
			}, null),
			new MethodInfo(MethodName.Fit, new PropertyInfo(Variant.Type.Nil, "", PropertyHint.None, "", PropertyUsageFlags.Default, exported: false), MethodFlags.Normal, null, null)
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
		if (method == MethodName.AtSize && args.Count == 1)
		{
			AtSize(VariantUtils.ConvertTo<int>(in args[0]));
			ret = default;
			return true;
		}
		if (method == MethodName.Fit && args.Count == 0)
		{
			Fit();
			ret = default;
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
		if (method == MethodName.AtSize)
		{
			return true;
		}
		if (method == MethodName.Fit)
		{
			return true;
		}
		return base.HasGodotClassMethod(in method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if (name == PropertyName._text)
		{
			_text = VariantUtils.ConvertTo<RichTextLabel>(in value);
			return true;
		}
		if (name == PropertyName._markup)
		{
			_markup = VariantUtils.ConvertTo<string>(in value);
			return true;
		}
		if (name == PropertyName._scale)
		{
			_scale = VariantUtils.ConvertTo<float>(in value);
			return true;
		}
		return base.SetGodotClassPropertyValue(in name, in value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		if (name == PropertyName._text)
		{
			value = VariantUtils.CreateFrom(in _text);
			return true;
		}
		if (name == PropertyName._markup)
		{
			value = VariantUtils.CreateFrom(in _markup);
			return true;
		}
		if (name == PropertyName._scale)
		{
			value = VariantUtils.CreateFrom(in _scale);
			return true;
		}
		return base.GetGodotClassPropertyValue(in name, out value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		return new List<PropertyInfo>
		{
			new PropertyInfo(Variant.Type.Object, PropertyName._text, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.String, PropertyName._markup, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false),
			new PropertyInfo(Variant.Type.Float, PropertyName._scale, PropertyHint.None, "", PropertyUsageFlags.ScriptVariable, exported: false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		base.SaveGodotObjectData(info);
		info.AddProperty(PropertyName._text, Variant.From(in _text));
		info.AddProperty(PropertyName._markup, Variant.From(in _markup));
		info.AddProperty(PropertyName._scale, Variant.From(in _scale));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		base.RestoreGodotObjectData(info);
		if (info.TryGetProperty(PropertyName._text, out var _value__text))
		{
			_text = _value__text.As<RichTextLabel>();
		}
		if (info.TryGetProperty(PropertyName._markup, out var _value__markup))
		{
			_markup = _value__markup.As<string>();
		}
		if (info.TryGetProperty(PropertyName._scale, out var _value__scale))
		{
			_scale = _value__scale.As<float>();
		}
	}

    // Layout in the approved 630-pixel design coordinate system. Subpixel
    // logical font rounding cannot accidentally erase the 25/29/39 hierarchy.
    private const float DesignScale = .4f;
    private readonly List<RichTextLabel> _paragraphLabels = new List<RichTextLabel>();
    private readonly List<ColorRect> _rules = new List<ColorRect>();
    private readonly HashSet<int> _dividerAfter = new HashSet<int>();
    private string[] _paragraphs = Array.Empty<string>();
    private Control? _layout;
    private ScrollContainer? _readingScroll;
    private bool _expanded;
    private bool _singleMetric;
    private bool _xMetric;
    private float _contentHeight;
    private int _designFontSize;
    private Label? _continuationCue;
    private ColorRect? _continuationBacking;

    public void Configure(string markup, Font? font, Color color, float scale, Action<bool> truncated,
        IReadOnlyCollection<int>? semanticDividers = null, bool expandedReading = false)
    {
        _markup = markup ?? "";
        _scale = Math.Clamp(scale, 1f, 1.3f);
        _truncated = truncated;
        _expanded = expandedReading;
        _paragraphs = SourceCardFace.Plain(_markup).Split('\n');
        _singleMetric = _paragraphs.Length == 1 && Regex.IsMatch(_paragraphs[0], @"^(?:ブロック|ダメージ)[0-9]+$");
        _xMetric = _paragraphs.Length == 2 && Regex.IsMatch(_paragraphs[0], @"^敵全体に ダメージ[0-9]+×X回$") && _paragraphs[1].StartsWith("X:", StringComparison.Ordinal);
        if (semanticDividers != null) foreach (int index in semanticDividers) _dividerAfter.Add(index);
        ClipContents = true;
        MouseFilter = _expanded ? MouseFilterEnum.Pass : MouseFilterEnum.Ignore;
        _layout = new Control { Name = "RulesLayout", MouseFilter = MouseFilterEnum.Ignore };
        if (_expanded) {
            _readingScroll = new ScrollContainer { Name = "ReadingScroll", Size = Size,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
                VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
                MouseFilter = MouseFilterEnum.Stop, FollowFocus = false };
            AddChild(_readingScroll);
            _readingScroll.AddChild(_layout);
        } else AddChild(_layout);
        _cardStrongFont ??= GD.Load<Font>("res://assets/Fonts/NotoSansJP-Bold.otf");
        for (int i = 0; i < _paragraphs.Length; i++) {
            var label = new RichTextLabel {
                Name = i == 0 ? "Text" : "Paragraph" + i,
                BbcodeEnabled = true, ScrollActive = false, FitContent = false, Threaded = false,
                TextureFilter = TextureFilterEnum.Linear, MouseFilter = MouseFilterEnum.Ignore,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                Scale = new Vector2(DesignScale, DesignScale)
            };
            if (font != null) {
                label.AddThemeFontOverride("normal_font", font);
                label.AddThemeFontOverride("italics_font", font);
            }
            if (_cardStrongFont != null) {
                label.AddThemeFontOverride("bold_font", _cardStrongFont);
                label.AddThemeFontOverride("bold_italics_font", _cardStrongFont);
            }
            label.AddThemeColorOverride("default_color", color);
            label.AddThemeConstantOverride("outline_size", 0);
            label.AddThemeStyleboxOverride("normal", new StyleBoxEmpty());
            _layout.AddChild(label);
            _paragraphLabels.Add(label);
            if (_text == null) _text = label;
            var line = new ColorRect { Name = "SemanticDivider" + i, Color = SourcePalette.Battle.CardDivider,
                MouseFilter = MouseFilterEnum.Ignore, Visible = _dividerAfter.Contains(i) };
            _layout.AddChild(line);
            _rules.Add(line);
        }
        SetMeta("source_body_full_text", SourceCardFace.Plain(_markup));
        SetMeta("source_body_markup", _markup);
        SetMeta("source_expanded_reading", _expanded);
        SetMeta("source_semantic_dividers", string.Join(",", _dividerAfter));
    }

    public override void _Ready()
    {
        Fit();
        Callable.From(Fit).CallDeferred();
    }

    private static string EscapeText(string value) => value.Replace("[", "\u0001").Replace("]", "[rb]").Replace("\u0001", "[lb]");

    private string ParagraphBbcode(string raw, int fontSize, int index)
    {
        // Emphasis adds tags only. All original punctuation, numbers, qualifiers
        // and notes stay in their original reading order.
        if (_singleMetric && !_expanded) {
            Match m = Regex.Match(raw, @"^(.*?)([0-9]+)$");
            return "[center]" + EscapeText(m.Groups[1].Value) + "  [b][font_size=" +
                (int)Math.Round(fontSize * 66f / 39f) + "]" + m.Groups[2].Value + "[/font_size][/b][/center]";
        }
        if (_xMetric && !_expanded) {
            if (index == 1) return "[center][font_size=" + (int)Math.Round(fontSize * 32f / 39f) + "]" + EscapeText(raw) + "[/font_size][/center]";
            Match m = Regex.Match(raw, @"^(敵全体に )(ダメージ)([0-9]+×X)(回)$");
            return "[center]" + m.Groups[1].Value + "\n" + m.Groups[2].Value + "[b][font_size=" +
                (int)Math.Round(fontSize * 56f / 39f) + "]" + m.Groups[3].Value + "[/font_size][/b]" + m.Groups[4].Value + "[/center]";
        }
        var output = new StringBuilder("[left]");
        int colon = raw.IndexOf(':');
        int i = 0;
        if (colon >= 0 && !raw.StartsWith("X:", StringComparison.Ordinal)) {
            output.Append("[b]").Append(EscapeText(raw.Substring(0, colon + 1))).Append("[/b]");
            i = colon + 1;
        }
        while (i < raw.Length) {
            Match number = Regex.Match(raw.Substring(i), @"^[+＋−-]?[0-9]+(?:[×xX][0-9X]+)*");
            if (number.Success) {
                output.Append("[b]").Append(number.Value).Append("[/b]");
                i += number.Length;
            } else {
                output.Append(EscapeText(raw[i].ToString()));
                i++;
            }
        }
        return output.Append("[/left]").ToString();
    }

    private void AtSize(int fontSize)
    {
        _designFontSize = fontSize;
        float width = (Size.X - (_expanded ? 12f : 0f)) / DesignScale;
        float y = 0;
        float gap = 18;
        for (int i = 0; i < _paragraphLabels.Count; i++) {
            RichTextLabel label = _paragraphLabels[i];
            foreach (string role in new[] { "normal_font_size", "bold_font_size", "italics_font_size", "bold_italics_font_size" })
                label.AddThemeFontSizeOverride(role, fontSize);
            // Noto's natural CJK line metrics include extra ascent/descent.
            // Use the selected leading without tightening glyph ink bounds.
            int naturalHeight = (int)Math.Ceiling(label.GetThemeFont("normal_font").GetHeight(fontSize));
            label.AddThemeConstantOverride("line_separation", Math.Min(0, fontSize + 5 - naturalHeight));
            label.Size = new Vector2(Math.Max(1, width), 4096);
            label.Text = ParagraphBbcode(_paragraphs[i], fontSize, i);
            float naturalHeightOfParagraph = Math.Max(label.GetContentHeight(), fontSize + 5);
            float trimTop = 0, trimBottom = 0;
            if (!_singleMetric && !_xMetric && !string.IsNullOrWhiteSpace(_paragraphs[i])) {
                Font regular = label.GetThemeFont("normal_font"), strong = label.GetThemeFont("bold_font");
                Rect2 regularInk = SourceHudLabel.MeasureGlyphs(regular, fontSize, _paragraphs[i]);
                Rect2 strongInk = SourceHudLabel.MeasureGlyphs(strong, fontSize, _paragraphs[i]);
                float ascent = Math.Max(regular.GetAscent(fontSize), strong.GetAscent(fontSize));
                float naturalLine = Math.Max(regular.GetHeight(fontSize), strong.GetHeight(fontSize));
                // Keep a full design pixel outside all measured glyph ink.
                // Only unused font ascender/descender padding is removed.
                float inkTop = ascent + Math.Min(regularInk.Position.Y, strongInk.Position.Y);
                float inkBottom = ascent + Math.Max(regularInk.End.Y, strongInk.End.Y);
                trimTop = Math.Max(0, (float)Math.Floor(inkTop - 1));
                trimBottom = Math.Max(0, (float)Math.Floor(naturalLine - inkBottom - 1));
            }
            float height = Math.Max(fontSize + 5, naturalHeightOfParagraph - trimTop - trimBottom);
            label.Size = new Vector2(Math.Max(1, width), naturalHeightOfParagraph + 2);
            label.Position = new Vector2(0, (y - trimTop) * DesignScale);
            label.SetMeta("source_paragraph_text", _paragraphs[i]);
            label.SetMeta("source_content_height", height * DesignScale);
            label.SetMeta("source_glyph_padding_top", trimTop * DesignScale);
            label.SetMeta("source_glyph_padding_bottom", trimBottom * DesignScale);
            label.SetMeta("source_visible_ink_bounds", new Rect2(0, y * DesignScale, width * DesignScale, height * DesignScale));
            label.SetMeta("source_font_size", fontSize * DesignScale);
            y += height;
            _rules[i].Position = new Vector2(0, (y + gap / 2f - 2) * DesignScale);
            _rules[i].Size = new Vector2(width * DesignScale, 2 * DesignScale);
            _rules[i].Visible = _dividerAfter.Contains(i) && i + 1 < _paragraphLabels.Count;
            if (i + 1 < _paragraphLabels.Count) y += gap;
        }
        _contentHeight = y * DesignScale;
    }

    public void Refit() => Fit();

    // delta is logical scroll pixels; positive advances toward later rules.
    // Touch preview explicitly forwards screen drags here without using card-play input.
    public void ScrollReading(float delta)
    {
        if (_readingScroll == null) return;
        _readingScroll.ScrollVertical = Math.Max(0, _readingScroll.ScrollVertical + (int)Math.Round(delta));
        SetMeta("source_reading_scroll", _readingScroll.ScrollVertical);
    }

    private void Fit()
    {
        if (_layout == null || !IsInsideTree()) return;
        int length = SourceCardFace.Plain(_markup).Replace("\n", "").Length;
        int preferred = (int)Math.Round((length < 38 ? 39 : 29) * _scale);
        if (_expanded) preferred = (int)Math.Ceiling(18f * _scale / DesignScale);
        AtSize(preferred);
        int minimum = _expanded ? preferred : (int)Math.Ceiling(24 * _scale);
        while (!_expanded && _designFontSize > minimum && _contentHeight > Size.Y - .8f)
            AtSize(_designFontSize - 1);
        bool clipped = !_expanded && _contentHeight > Size.Y + .01f;
        float visibleHeight = _expanded ? _contentHeight : Math.Min(_contentHeight, Size.Y);
        _layout.Scale = Vector2.One;
        _layout.Position = _expanded || clipped ? Vector2.Zero : new Vector2(0, Math.Max(0, (Size.Y - visibleHeight) / 2));
        _layout.Size = new Vector2(Size.X, _contentHeight);
        _layout.CustomMinimumSize = _expanded ? new Vector2(Size.X - 12f, _contentHeight + 2f) : Vector2.Zero;
        if (_readingScroll != null) _readingScroll.Size = Size;
        bool requiresReading = clipped || (!_expanded && length > 100);
        float cueHeight = 14f * _scale;
        int cueDesignFontSize = (int)Math.Round(30f * _scale);
        if (clipped && _continuationCue == null) {
            _continuationBacking = new ColorRect { Name = "ContinuationBacking", Color = SourcePalette.Battle.CardShell, MouseFilter = MouseFilterEnum.Ignore };
            _continuationCue = new SourceHudLabel { Name = "ContinuationCue", Text = "続き ▾", MouseFilter = MouseFilterEnum.Ignore,
                TextureFilter = TextureFilterEnum.Linear,
                HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
            _continuationCue.AddThemeFontOverride("font", _cardStrongFont);
            _continuationCue.AddThemeFontSizeOverride("font_size", cueDesignFontSize);
            _continuationCue.AddThemeColorOverride("font_color", new Color("f6f1d9"));
            _continuationCue.SetMeta("source_ui_annotation", true);
            _continuationCue.SetMeta("source_not_card_text", true);
            AddChild(_continuationBacking);
            // A child's own Scale does not scale its Position. Use a design
            // parent so measured glyph placement is transformed as well.
            var cueSpace = new Control { Name = "ContinuationDesign", Scale = Vector2.One * DesignScale,
                Size = Size / DesignScale, MouseFilter = MouseFilterEnum.Ignore };
            AddChild(cueSpace);
            cueSpace.AddChild(_continuationCue);
        }
        if (_continuationCue != null && _continuationBacking != null) {
            _continuationCue.Visible = _continuationBacking.Visible = clipped;
            _continuationBacking.Position = new Vector2(0, Size.Y - cueHeight);
            _continuationBacking.Size = new Vector2(Size.X, cueHeight);
            if (_continuationCue is SourceHudLabel cue) {
                // Match the authored-resolution header labels:12 logical px at
                // 100%, scaled with the user's setting, with actual glyph ink
                // centered inside the reserved14logical-px continuation rail.
                cue.SourceTextRect = new Rect2(0, (Size.Y - cueHeight) / DesignScale, Size.X / DesignScale, cueHeight / DesignScale);
                cue.AddThemeFontSizeOverride("font_size", cueDesignFontSize);
                cue.RefreshSourceAlignment();
                cue.SetMeta("source_cue_font_size", cueDesignFontSize * DesignScale);
                cue.SetMeta("source_cue_design_font_size", cueDesignFontSize);
                cue.SetMeta("source_cue_rect", new Rect2(0, Size.Y - cueHeight, Size.X, cueHeight));
            }
        }
        SetMeta("source_body_viewport", new Rect2(0, 0, Size.X, Size.Y));
        SetMeta("source_visible_body_viewport", new Rect2(0, 0, Size.X, clipped ? Size.Y - cueHeight : Size.Y));
        SetMeta("source_continuation_cue", clipped);
        SetMeta("source_body_viewport_clipped", _contentHeight > Size.Y);
        SetMeta("source_complete_text_reachable", _expanded || !clipped);
        SetMeta("source_font_size", _designFontSize * DesignScale);
        SetMeta("source_design_font_size", _designFontSize);
        SetMeta("source_full_content_height", _contentHeight);
        SetMeta("source_rendered_content_height", visibleHeight);
        SetMeta("source_body_truncated", clipped);
        SetMeta("source_body_requires_enlargement", requiresReading);
        SetMeta("source_body_scrollable", _expanded && _contentHeight > Size.Y);
        SetMeta("source_requested_text_scale", _scale);
        SetMeta("source_paragraph_count", _paragraphs.Length);
        SetMeta("source_body_bounds", new Rect2(0, 0, Size.X, _expanded ? _contentHeight : visibleHeight));
        _truncated?.Invoke(requiresReading);
    }
}

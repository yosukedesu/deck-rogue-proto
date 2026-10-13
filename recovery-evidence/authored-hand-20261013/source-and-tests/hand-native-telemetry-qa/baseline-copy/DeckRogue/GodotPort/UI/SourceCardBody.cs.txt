// Reconstructed 2026-10-12 from the original Unity CardView/PaperFx/Theme sources
// and ux-ref-a7f86b5 H5/H10 reference pixels. This is not recovered exact Godot source.
// Presentation only: no card definitions, effects, targets or game state are mutated.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using Godot;

namespace DeckRogue.GodotPort.UI;

internal partial class SourceCardBody : Control
{
    private RichTextLabel? _text;
    private string _markup = "";
    private float _scale = 1;
    private Action<bool>? _truncated;
    public void Configure(string markup, Font? font, Color color, float scale, Action<bool> truncated)
    {
        _markup = markup; _scale = Math.Clamp(scale, 1, 1.3f); _truncated = truncated;
        ClipContents = true;
        _text = new RichTextLabel { Name = "Text", BbcodeEnabled = true, ScrollActive = false,
            FitContent = false, Threaded = false, TextureFilter = CanvasItem.TextureFilterEnum.Linear, MouseFilter = MouseFilterEnum.Ignore,
            AutowrapMode = TextServer.AutowrapMode.WordSmart, Size = new Vector2(Size.X, Size.Y) };
        foreach (string role in new[] { "normal_font", "bold_font", "italics_font", "bold_italics_font" })
            if (font != null) _text.AddThemeFontOverride(role, font);
        _text.AddThemeColorOverride("default_color", color);
        _text.AddThemeConstantOverride("line_separation", 2);
        AddChild(_text);
    }
    public override void _Ready()
    {
        Fit();
        // One deferred pass accounts for initial font shaping after entering the tree.
        Callable.From(Fit).CallDeferred();
    }
    private void AtSize(int fontSize)
    {
        if (_text == null) return;
        foreach (string role in new[] { "normal_font_size", "bold_font_size", "italics_font_size", "bold_italics_font_size" })
            _text.AddThemeFontSizeOverride(role, fontSize);
        _text.Text = SourceCardFace.Bbcode(_markup, fontSize);
    }
    private void Fit()
    {
        if (_text == null || !IsInsideTree()) return;
        _text.Size = Size; _text.Position = Vector2.Zero;
        int paragraphs = _markup.Split('\n').Length;
        int fontSize = (int)Math.Round(18 * _scale);
        AtSize(fontSize);
        if (_text.GetLineCount() > paragraphs) { fontSize = (int)Math.Round(16 * _scale); AtSize(fontSize); }
        int minimum = (int)Math.Round(12 * _scale);
        while (fontSize > minimum && _text.GetContentHeight() > Size.Y + 0.5f) AtSize(--fontSize);
        float height = _text.GetContentHeight();
        bool clipped = height > Size.Y + 0.5f;
        _text.Position = new Vector2(0, clipped ? 0 : Math.Max(0, (Size.Y - height) / 2));
        _truncated?.Invoke(clipped);
        SetMeta("source_font_size", fontSize);
        SetMeta("source_body_truncated", clipped);
    }
}

// New reconstruction, not a recovered dd3ae1b file. Geometry follows pinned
// Unity PaperFx.Nine/PanelBands/TagBands; no gameplay or random state involved.
using System;
using System.Collections.Generic;
using Godot;
namespace DeckRogue.GodotPort.UI;

public static class SourceRunUiDecor
{
    private static Font? _body, _display;
    private static readonly Dictionary<(Color, bool), Texture2D> Papers = new();
    public static Font Body => _body ??= GD.Load<Font>("res://assets/Fonts/NotoSansJP-Regular.otf");
    public static Font Action => Display;
    public static Font Display => _display ??= GD.Load<Font>("res://assets/Fonts/NotoSansJP-Bold.otf");

    // Font A roles: explicit bundled Gothic faces, with genuine Bold for actions and headings.
    public static void ApplyFonts(Theme theme)
    {
        theme.DefaultFont = Body;
        foreach (string type in new[] { "Button", "OptionButton", "CheckButton", "CheckBox", "MenuButton", "PopupMenu", "TabContainer", "TabBar" })
            theme.SetFont("font", type, Display);
        foreach (string type in new[] { "Label", "LineEdit", "TextEdit", "CodeEdit", "TooltipLabel" })
            theme.SetFont("font", type, Body);
        foreach (string role in new[] { "normal_font", "italics_font", "mono_font" })
            theme.SetFont(role, "RichTextLabel", Body);
        foreach (string role in new[] { "bold_font", "bold_italics_font" })
            theme.SetFont(role, "RichTextLabel", Display);
    }

    public static StyleBoxTexture Paper(Color fill, float unit, float padding = 18f, bool compact = false)
    {
        unit = Math.Max(.1f, unit);
        var key = (fill, compact);
        if (!Papers.TryGetValue(key, out var texture))
        {
            int size = compact ? 40 : 48, radius = compact ? 10 : 12;
            using var image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
            float half = size / 2f;
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float qx = Math.Abs(x + .5f - half) - (half - radius);
                float qy = Math.Abs(y + .5f - half) - (half - radius);
                float ox = Math.Max(qx, 0), oy = Math.Max(qy, 0);
                float d = radius - MathF.Sqrt(ox * ox + oy * oy) - Math.Min(Math.Max(qx, qy), 0);
                Color c = Colors.Transparent;
                if (d >= 0)
                {
                    c = d < 1 ? fill.Lerp(SourcePalette.Ink, compact ? .25f : .18f) : fill;
                    if (d < 1) c.A *= Math.Clamp(d + .5f, 0, 1);
                    // Godot texture Y runs downward; Unity's bottom four rows are reversed.
                    // A single soft edge replaces the raised bevel and nested outlines.
                }
                image.SetPixel(x, y, c);
            }
            texture = ImageTexture.CreateFromImage(image);
            Papers[key] = texture;
        }
        float border = compact ? 12 : 14;
        return new StyleBoxTexture
        {
            Texture = texture,
            TextureMarginLeft = border, TextureMarginRight = border,
            TextureMarginTop = border, TextureMarginBottom = border,
            AxisStretchHorizontal = StyleBoxTexture.AxisStretchMode.Stretch,
            AxisStretchVertical = StyleBoxTexture.AxisStretchMode.Stretch,
            ContentMarginLeft = padding * unit, ContentMarginRight = padding * unit,
            ContentMarginTop = padding * unit, ContentMarginBottom = padding * unit
        };
    }

    // Paper-local input defaults cover nested SpinBox LineEdits and read-only
    // reports as well as editable fields, without touching battle/dark themes.
    // Explicit validation overrides (for example an invalid seed) take priority.
    public static Theme PaperInputs(float unit, int fontSize)
    {
        var theme = new Theme();
        ApplyFonts(theme);
        foreach (string type in new[] { "LineEdit", "TextEdit" })
        {
            theme.SetFont("font", type, Body);
            theme.SetFontSize("font_size", type, fontSize);
            theme.SetStylebox("normal", type, Paper(SourcePalette.Paper3, unit, 12, true));
            theme.SetStylebox("read_only", type, Paper(SourcePalette.Paper3, unit, 12, true));
            theme.SetStylebox("focus", type, new StyleBoxFlat { BgColor = Colors.Transparent, BorderColor = SourcePalette.ManaInk, BorderWidthLeft = 3, BorderWidthRight = 3, BorderWidthTop = 3, BorderWidthBottom = 3, CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8 });
            theme.SetColor("font_color", type, SourcePalette.Ink);
            theme.SetColor(type == "TextEdit" ? "font_readonly_color" : "font_uneditable_color", type, SourcePalette.InkSoft);
            theme.SetColor("font_placeholder_color", type, new Color("686052"));
            theme.SetColor("caret_color", type, SourcePalette.Ink);
            theme.SetColor("selection_color", type, new Color("c6d9d1"));
            theme.SetColor("font_selected_color", type, SourcePalette.Ink);
            theme.SetColor("font_outline_color", type, Colors.Transparent);
            if (type == "LineEdit")
            {
                theme.SetColor("clear_button_color", type, SourcePalette.InkSoft);
                theme.SetColor("clear_button_color_pressed", type, SourcePalette.Ink);
            }
            else
            {
                theme.SetColor("background_color", type, Colors.Transparent);
                theme.SetColor("caret_background_color", type, SourcePalette.Paper3);
                theme.SetColor("current_line_color", type, Colors.Transparent);
                theme.SetColor("word_highlighted_color", type, new Color("c6d9d1"));
                theme.SetColor("search_result_color", type, SourcePalette.BrassLight);
                theme.SetColor("search_result_border_color", type, SourcePalette.BrassInk);
            }
            theme.SetConstant("caret_width", type, 2);
        }
        foreach (string direction in new[] { "up", "down" })
        {
            theme.SetColor(direction + "_icon_modulate", "SpinBox", SourcePalette.Ink);
            theme.SetColor(direction + "_hover_icon_modulate", "SpinBox", SourcePalette.ManaInk);
            theme.SetColor(direction + "_pressed_icon_modulate", "SpinBox", SourcePalette.BrassInk);
            theme.SetColor(direction + "_disabled_icon_modulate", "SpinBox", new Color("8d8371"));
        }
        return theme;
    }

    // Semantic action colors: amber is reserved for committing a choice;
    // neutral paper is navigation. State changes never move the hit rectangle.
    public static void Button(Button button, float unit, bool primary = false)
    {
        button.SetMeta("source_primary_action", primary);
        Color fill = primary ? SourcePalette.BrassLight : SourcePalette.Paper3;
        Color hover = primary ? new Color("f2d99b") : new Color("fffaf0");
        Color pressed = primary ? new Color("d4b260") : SourcePalette.Paper2;
        button.AddThemeFontOverride("font", Action);
        button.AddThemeStyleboxOverride("normal", Paper(fill, unit, 10, true));
        button.AddThemeStyleboxOverride("hover", Paper(hover, unit, 10, true));
        button.AddThemeStyleboxOverride("pressed", Paper(pressed, unit, 10, true));
        button.AddThemeStyleboxOverride("hover_pressed", Paper(pressed, unit, 10, true));
        button.AddThemeStyleboxOverride("disabled", Paper(new Color("ded8c9"), unit, 10, true));
        // Gold focus disappeared against gold primary buttons. A teal inset
        // outline remains distinct on both button roles and on paper panels.
        int edge = Math.Max(2, (int)Math.Round(2 * unit));
        button.AddThemeStyleboxOverride("focus", new StyleBoxFlat {
            BgColor = Colors.Transparent, BorderColor = SourcePalette.ManaInk,
            BorderWidthLeft = edge, BorderWidthRight = edge,
            BorderWidthTop = edge, BorderWidthBottom = edge,
            CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
            ContentMarginLeft = 0, ContentMarginRight = 0,
            ContentMarginTop = 0, ContentMarginBottom = 0
        });
        foreach (string state in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_hover_pressed_color", "font_focus_color" })
            button.AddThemeColorOverride(state, SourcePalette.Ink);
        button.AddThemeColorOverride("font_disabled_color", new Color("676359"));
    }
}

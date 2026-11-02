// Additive candidate only. Hooks are documented in apply_shell.py.
using System;
using Godot;
namespace DeckRogue.GodotPort.UI;
public partial class GameUI
{
    private const string RunPaperMarker = "source_run_paper";
    private float RunUnit => Sizing.SourceScale;
    private Rect2 RunSafeRect => new Rect2(
        _safeMargin?.GetThemeConstant("margin_left") ?? Sizing.Edge,
        _safeMargin?.GetThemeConstant("margin_top") ?? Sizing.Edge,
        Math.Max(1, Size.X - (_safeMargin?.GetThemeConstant("margin_left") ?? Sizing.Edge) - (_safeMargin?.GetThemeConstant("margin_right") ?? Sizing.Edge)),
        Math.Max(1, Size.Y - (_safeMargin?.GetThemeConstant("margin_top") ?? Sizing.Edge) - (_safeMargin?.GetThemeConstant("margin_bottom") ?? Sizing.Edge)));

    private PanelContainer RunPaper(Node parent, string name, float padding = 18f)
    {
        var panel = new PanelContainer { Name = name, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        panel.SetMeta(RunPaperMarker, true);
        panel.Theme = SourceRunUiDecor.PaperInputs(RunUnit, FontSize(20));
        panel.AddThemeStyleboxOverride("panel", SourceRunUiDecor.Paper(SourcePalette.Paper, RunUnit, padding));
        panel.AddThemeFontOverride("font", SourceRunUiDecor.Body);
        parent.AddChild(panel);
        if (OnModernModal(parent)) StyleModernModal(panel, ModernModalTheme());
        return panel;
    }
    private Button RunButton(Node parent, string text, Action action, bool enabled = true, bool primary = false)
    {
        var button = Button(parent, text, action, enabled);
        SourceRunUiDecor.Button(button, RunUnit, primary);
        if (OnModernModal(parent)) StyleModernModal(button, ModernModalTheme());
        return button;
    }
    private static bool OnRunPaper(Node node)
    {
        for (Node? current = node; current != null; current = current.GetParent())
            if (current.HasMeta(RunPaperMarker)) return current.GetMeta(RunPaperMarker).AsBool();
        return false;
    }
    private Color RunTextColor(Node parent, Color color)
    {
        if (OnModernModal(parent)) return ModalInk(color);
        if (!OnRunPaper(parent)) return color;
        if (color == Ink) return SourcePalette.Ink;
        if (color == Muted) return SourcePalette.InkSoft;
        if (color == Gold) return SourcePalette.BrassInk;
        if (color == Accent) return SourcePalette.ManaInk;
        if (color == Danger) return SourcePalette.BadInk;
        return color;
    }
    private void ApplyRunText(Label label, Node parent, Color color)
    {
        label.AddThemeColorOverride("font_color", RunTextColor(parent, color));
        // Source card and combat labels are authored separately; only ordinary
        // labels created through Text on run screens/explicit paper get this font.
        if (CurrentRoute != "combat" || OnRunPaper(parent))
            label.AddThemeFontOverride("font", SourceRunUiDecor.Body);
    }
    private void PlaceRunModal(MarginContainer frame)
    {
        float left = _safeMargin?.GetThemeConstant("margin_left") ?? Sizing.Edge;
        float right = _safeMargin?.GetThemeConstant("margin_right") ?? Sizing.Edge;
        float top = _safeMargin?.GetThemeConstant("margin_top") ?? Sizing.Edge;
        float bottom = _safeMargin?.GetThemeConstant("margin_bottom") ?? Sizing.Edge;
        float availableWidth = Math.Max(1, Size.X - left - right);
        float availableHeight = Math.Max(1, Size.Y - top - bottom);
        bool menu = _overlayTitle == "メニュー";
        bool wide = _overlayTitle.Contains("デッキ") || _overlayTitle.Contains("地図") || _overlayTitle.Contains("札");
        float width = Math.Min(availableWidth, (menu ? 1000 : wide ? 1180 : 900) * RunUnit * _textScale);
        float height = Math.Min(availableHeight, (menu ? 760 : wide ? 780 : 700) * RunUnit);
        frame.AnchorLeft = frame.AnchorRight = frame.AnchorTop = frame.AnchorBottom = 0;
        frame.Position = new Vector2(menu ? Size.X - right - width : left + (availableWidth - width) / 2,
            menu ? top : top + (availableHeight - height) / 2);
        frame.Size = new Vector2(width, height);
    }
}

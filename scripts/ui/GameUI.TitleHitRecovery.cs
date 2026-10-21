// NEW implementation of preserved whole-card interior selection.
// Existing explicit button/keyboard access remains. Dragging a scroll must not select.
using System;
using Godot;
namespace DeckRogue.GodotPort.UI;
public partial class GameUI
{
    private void BindTitleLeaderCard(Control panel, Control content, Action select)
    {
        panel.MouseFilter = MouseFilterEnum.Stop;
        content.MouseFilter = MouseFilterEnum.Pass;
        Vector2? pressedAt = null;
        panel.GuiInput += input => {
            if (input is InputEventMouseMotion motion && pressedAt.HasValue
                && motion.Position.DistanceTo(pressedAt.Value) > 10) pressedAt = null;
            if (input is not InputEventMouseButton button || button.ButtonIndex != MouseButton.Left) return;
            if (button.Pressed) { pressedAt = button.Position; return; }
            bool click = pressedAt.HasValue && button.Position.DistanceTo(pressedAt.Value) <= 10;
            pressedAt = null;
            if (click && _overlay == null && !IsInputBlocked) { select(); panel.AcceptEvent(); }
        };
        panel.MouseExited += () => pressedAt = null;
    }
    private void RestyleTitlePaper(Node node)
    {
        if (node is Label label)
        {
            var color = label.GetThemeColor("font_color");
            label.AddThemeColorOverride("font_color", RunTextColor(node, color));
            label.AddThemeFontOverride("font", SourceRunUiDecor.Body);
        }
        foreach (Node child in node.GetChildren()) RestyleTitlePaper(child);
    }

}

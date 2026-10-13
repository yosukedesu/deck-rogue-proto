// Unity GameRoot.Update handles these display shortcuts globally, before text widgets.
// The four _Input dispatch fragments are emitted by the official Godot generator.
using Godot;
namespace DeckRogue.GodotPort.UI;
public partial class GameUI
{
    public override void _Input(InputEvent evt)
    {
        if (!DesktopVideoAvailable || evt is not InputEventKey { Pressed: true, Echo: false } key) return;
        if (key.Keycode != Key.F11 && !(key.AltPressed && (key.Keycode == Key.Enter || key.Keycode == Key.KpEnter))) return;
        ToggleFullscreen();
        GetViewport().SetInputAsHandled();
    }
}

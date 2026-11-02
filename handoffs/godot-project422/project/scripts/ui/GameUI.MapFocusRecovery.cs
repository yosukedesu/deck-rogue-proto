// NEW implementation of the retained two-frame, 62%-viewport focus improvement.
// Geometry and saved annotations remain owned by RunMap, unchanged.
using System;
using Godot;
using DeckRogue.Engine.Generated;
namespace DeckRogue.GodotPort.UI;
public partial class GameUI
{
    private ScrollContainer? _retainedMapScroll;
    private RunState? _retainedMapRun;
    private Vector2I? _retainedMapPosition;
    private bool _retainedMapReady;

    private void CaptureMapScrollBeforeRebuild()
    {
        if (_retainedMapReady && GodotObject.IsInstanceValid(_retainedMapScroll))
            _retainedMapPosition = new Vector2I(_retainedMapScroll.ScrollHorizontal, _retainedMapScroll.ScrollVertical);
    }

    private async void FocusMapAfterLayout(ScrollContainer scroll, RunMap map, RunState run, bool interactive)
    {
        // Only the main map owns retained state. A read-only map dialog must not replace it.
        Vector2I? restore = interactive && ReferenceEquals(run, _retainedMapRun) ? _retainedMapPosition : null;
        if (interactive)
        {
            _retainedMapRun = run;
            _retainedMapScroll = scroll;
            _retainedMapPosition = restore;
            _retainedMapReady = false;
        }
        var tree = GetTree();
        for (int frame = 0; frame < 2; frame++)
        {
            await ToSignal(tree, SceneTree.SignalName.ProcessFrame);
            if (!GodotObject.IsInstanceValid(scroll) || !GodotObject.IsInstanceValid(map)
                || !scroll.IsInsideTree() || !map.IsInsideTree()) return;
        }
        if (restore is Vector2I position)
        {
            scroll.ScrollHorizontal = position.X;
            scroll.ScrollVertical = position.Y;
            _retainedMapReady = true;
            return;
        }
        // Global offset includes the centering wrapper and any current scroll.
        float currentY = map.GlobalPosition.Y - scroll.GlobalPosition.Y
            + scroll.ScrollVertical + map.CurrentFocusY;
        var bar = scroll.GetVScrollBar();
        scroll.ScrollVertical = (int)Math.Clamp(currentY - scroll.Size.Y * .62f,
            0, Math.Max(0, bar.MaxValue - bar.Page));
        if (interactive) _retainedMapReady = true;
    }
}

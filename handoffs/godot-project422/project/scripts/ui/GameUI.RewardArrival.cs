// Restored retained UI-only arrival and focus decoration. Never changes input geometry.
using System;
using Godot;
namespace DeckRogue.GodotPort.UI;
public partial class GameUI
{
    private object _rewardArrivalSession;
    private string _rewardArrivalState;
    private int _rewardArrivalStarts;
    private bool _rewardArrivalPresented;

    // One visual arrival for a new session/route/revision. The stage can synchronously
    // complete before Changed rebuilds the same new route, so retain a pending
    // arrival until first presentation. Once shown, inspector/settings/cancel and
    // layout rebuilds never replay it or increment the arrival count.
    private bool BeginRewardArrival(string route)
    {
        string state = route + ":" + (_session?.Revision ?? 0);
        if (ReferenceEquals(_rewardArrivalSession, _session) && _rewardArrivalState == state)
            return !_rewardArrivalPresented;
        _rewardArrivalSession = _session;
        _rewardArrivalState = state;
        _rewardArrivalPresented = false;
        _rewardArrivalStarts++;
        return true;
    }

    // A short brass arrival glint replaces Unity's scale of the entire offer cell.
    // Only this mouse-ignoring decoration changes opacity; target scale, position,
    // layout minimum and every hit rectangle stay untouched. Bound-node tweens die
    // with a rebuild, so immediate acquisition/skip/inspection remains safe.
    private void RewardArrivalAccent(Control target, bool first, bool title = false)
    {
        if (!first || target == null) return;
        // Accessibility option is deliberately scoped to nonessential arrival
        // glints. Mark it presented so toggling later never replays an old reward.
        // Hit feedback, hover/focus affordances and gameplay timing are untouched.
        if (_reduceRewardGlints) { _rewardArrivalPresented = true; return; }
        var accent = RewardAccentPanel(target, title);
        accent.Name = "SourceRewardArrivalAccent";
        accent.SetMeta("source_reward_arrival", true);
        object arrivalSession = _rewardArrivalSession;
        string arrivalState = _rewardArrivalState;
        bool started = false;
        void StartFade()
        {
            if (started || !GodotObject.IsInstanceValid(accent) || !accent.IsInsideTree() || !accent.IsVisibleInTree()) return;
            started = true;
            if (ReferenceEquals(_rewardArrivalSession, arrivalSession) && _rewardArrivalState == arrivalState)
                _rewardArrivalPresented = true;
            accent.SetMeta("source_reward_arrival_started", true);
            var tween = accent.CreateTween();
            tween.TweenProperty(accent, "modulate:a", 0f, .38).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
            tween.Finished += () => { if (GodotObject.IsInstanceValid(accent)) accent.QueueFree(); };
        }
        // Initial layout/shader work can consume an immediate tween before any
        // frame is presented. Wait for this decoration's draw and its post-draw
        // boundary; merely receiving Draw may still precede this frame's tweens.
        bool awaitingFrame = false;
        void AfterPresentedFrame()
        {
            RenderingServer.FramePostDraw -= AfterPresentedFrame;
            awaitingFrame = false;
            StartFade();
        }
        void FirstDraw()
        {
            if (started || awaitingFrame) return;
            awaitingFrame = true;
            RenderingServer.FramePostDraw += AfterPresentedFrame;
        }
        accent.TreeExiting += () => {
            if (awaitingFrame) RenderingServer.FramePostDraw -= AfterPresentedFrame;
            awaitingFrame = false;
        };
        // Headless validation has no visible draw, so use its deferred fallback.
        if (DisplayServer.GetName() == "headless") Callable.From(StartFade).CallDeferred();
        else accent.Draw += FirstDraw;
    }

    private Panel RewardAccentPanel(Control target, bool title = false)
    {
        var accent = new Panel {
            Name = "SourceRewardAccent", MouseFilter = MouseFilterEnum.Ignore,
            FocusMode = FocusModeEnum.None, ZIndex = 2,
            CustomMinimumSize = Vector2.Zero, Modulate = new Color(1, 1, 1, .85f)
        };
        var border = new StyleBoxFlat {
            BgColor = Colors.Transparent, BorderColor = title ? Gold : SourcePalette.BrassLight,
            BorderWidthLeft = title ? 0 : 3, BorderWidthRight = title ? 0 : 3,
            BorderWidthTop = title ? 0 : 3, BorderWidthBottom = 3,
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6, CornerRadiusBottomRight = 6,
            ContentMarginLeft = 0, ContentMarginRight = 0, ContentMarginTop = 0, ContentMarginBottom = 0
        };
        accent.AddThemeStyleboxOverride("panel", border);
        target.AddChild(accent);
        accent.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        if (title) { accent.AnchorLeft = .25f; accent.AnchorRight = .75f; }
        return accent;
    }

    // Offer inspection keeps the source hover affordance without scaling the
    // button's hit shape. Keyboard focus receives the same visible decoration.
    private void RewardOfferHover(CardTile tile)
    {
        var face = tile.GetNodeOrNull<Control>("SourceCardFace");
        if (face == null) return;
        var accent = RewardAccentPanel(face);
        accent.Name = "SourceRewardHoverAccent";
        accent.SetMeta("source_reward_hover", true);
        accent.Visible = false;
        bool hover = false, focus = false;
        void Refresh() { if (GodotObject.IsInstanceValid(accent)) accent.Visible = hover || focus; }
        tile.MouseEntered += () => { hover = true; Refresh(); };
        tile.MouseExited += () => { hover = false; Refresh(); };
        tile.FocusEntered += () => { focus = true; Refresh(); };
        tile.FocusExited += () => { focus = false; Refresh(); };
    }
}

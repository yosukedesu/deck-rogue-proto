using System;
using System.Linq;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using Godot;

namespace DeckRogue.GodotPort.UI;

public partial class GameUI
{
    private enum TouchHandUseRoute { Play, Set, ChoosePlayOrSet }
    private readonly TouchHandGesture _touchHand = new();
    private CardTile? _touchHandPreview;
    private CardTile? _touchHandGhost;
    private Panel? _touchHandSelectionRing;
    private ColorRect? _touchHandBoundaryLine;
    private SourceAimArrow? _touchHandAim;
    private CardTile? _touchHandOrigin;
    private EnemyState[] _touchHandEnemySnapshot = Array.Empty<EnemyState>();
    private Vector2 _touchHandAnchor;
    private Vector2 _touchHandPointer;
    private Vector2 _touchHandCanvasSize;
    private float _touchHandBoundary;
    private ulong _touchMouseQuarantineUntil;
    private int? _touchReadingPointer;
    private float _touchReadingY;
    private bool _touchReadingCaptured;

    // Read-only diagnostics also let the integration probe inspect the real path.
    public string TouchHandSelectedUid => _touchHand.SelectedUid ?? "";
    public string TouchHandPhase => _touchHand.Phase.ToString();
    public float TouchHandUseBoundary => _touchHandBoundary;
    public long TouchHandCommitCount { get; private set; }
    public CardTile? TouchHandPreview => _touchHandPreview;
    public CardTile? TouchHandGhost => _touchHandGhost;
    private bool TouchOwnsNativeHand => _touchHand.Captured || _touchReadingCaptured ||
        _touchHand.SelectedUid != null || Time.GetTicksMsec() < _touchMouseQuarantineUntil;
    private bool TouchHandAvailable => CurrentRoute == "combat" && _overlay == null && _play == null &&
        !IsInputBlocked && _session != null && _uxDrag == null && _uxHand != null &&
        GodotObject.IsInstanceValid(_uxHand) && _uxHand.IsVisibleInTree();

    private bool HandleTouchHandInput(InputEvent evt)
    {
        // A desktop's optional mouse-to-touch emulation must not change PC mappings.
        if (evt.Device == -1 && (evt is InputEventScreenTouch || evt is InputEventScreenDrag)) return true;
        // Godot identifies touch-generated mouse events with DEVICE_ID_EMULATION (-1).
        // They must never reach native Button.Pressed / _GetDragData a second time.
        if (evt is InputEventMouse mouse)
        {
            if (evt.Device == -1)
                return _touchHand.Captured || _touchReadingCaptured || Time.GetTicksMsec() < _touchMouseQuarantineUntil ||
                    (TouchHandAvailable && (TouchCardAt(mouse.Position) != null || TouchReadingBodyContains(mouse.Position)));
            if (_touchHand.Captured || _touchReadingCaptured ||
                (OS.HasFeature("mobile") && Time.GetTicksMsec() < _touchMouseQuarantineUntil))
                return true;
            // An actual desktop mouse keeps its existing interaction and key mappings.
            if (_touchHand.SelectedUid != null) CancelTouchHand();
            _touchMouseQuarantineUntil = 0;
            return false;
        }
        if (evt is InputEventScreenTouch touch)
        {
            bool captured = _touchHand.Captured || _touchReadingCaptured;
            if (touch.Pressed)
            {
                bool bodyScroll = TouchHandAvailable && _touchHand.PointerCount == 0 &&
                    _touchHand.Phase == TouchHandGesture.Stage.Reading && TouchReadingBodyContains(touch.Position);
                string? uid = TouchHandAvailable && !bodyScroll ? TouchCardAt(touch.Position)?.CardUid : null;
                bool retouchPreview = uid != null && uid == _touchHand.SelectedUid &&
                    _touchHandPreview != null && GodotObject.IsInstanceValid(_touchHandPreview) &&
                    _touchHandPreview.Visible && TouchContains(_touchHandPreview, touch.Position);
                captured |= _touchHand.Press(touch.Index, uid, touch.Position.Y, ComputeTouchHandBoundary(),
                    Math.Max(18f, Math.Min(Size.X, Size.Y) * .035f), _session?.Revision ?? -1, _uxEpoch);
                if (bodyScroll)
                {
                    _touchReadingPointer = touch.Index;
                    _touchReadingY = touch.Position.Y;
                    _touchReadingCaptured = captured = true;
                }
                else if (_touchHand.Phase == TouchHandGesture.Stage.Pressed && uid != null)
                {
                    _touchHandOrigin = _uxTiles[uid];
                    _touchHandOrigin.SuppressTouchActivation();
                    if (!retouchPreview) _touchHandAnchor = touch.Position;
                    _touchHandPointer = touch.Position;
                    _touchHandCanvasSize = Size;
                    _touchHandBoundary = ComputeTouchHandBoundary();
                    _touchHandEnemySnapshot = InputCombat?.Enemies.ToArray() ?? Array.Empty<EnemyState>();
                    // A previous native hover must not leave another big face over the target.
                    if (_uxHover != null) HoverUxHand(_uxHover, enter: false);
                    BuildTouchHandPresentation(uid);
                }
                if (_touchHand.Phase == TouchHandGesture.Stage.Cancelled)
                {
                    _touchReadingPointer = null;
                    ClearTouchHandPresentation();
                }
            }
            else
            {
                if (_touchReadingPointer == touch.Index) _touchReadingPointer = null;
                _touchHandPointer = touch.Position;
                _touchHand.Move(touch.Index, touch.Position.Y, _session?.Revision ?? -1, _uxEpoch);
                TouchHandGesture.Release? release = _touchHand.End(touch.Index, touch.Position.Y,
                    touch.Canceled, _session?.Revision ?? -1, _uxEpoch);
                if (_touchHand.PointerCount == 0) _touchReadingCaptured = false;
                if (release is { AttemptUse: true } admission &&
                    TryResolveTouchHandUse(admission.Uid, touch.Position, admission.Revision, admission.Epoch, out var card, out var target, out var route))
                {
                    // Clear ownership and presentation before entering the existing rules/choice flow.
                    // A duplicate release cannot create a second admission, even during synchronous rebuild.
                    CancelTouchHand();
                    TouchHandCommitCount++;
                    if (route == TouchHandUseRoute.Set)
                        Do(new Command_SetCard { CardUid = card!.Uid });
                    else if (route == TouchHandUseRoute.ChoosePlayOrSet)
                    {
                        // Preserve the existing dual-purpose action choice. Selecting Play
                        // retains the explicitly aimed target; no new game rule is introduced.
                        if (target.HasValue) _preferredTarget = target.Value;
                        CardActions(card!);
                    }
                    else BeginPlay(card!, target);
                }
                else RefreshTouchHandPresentation();
            }
            if (captured) _touchMouseQuarantineUntil = Time.GetTicksMsec() + 500;
            return captured;
        }
        if (evt is InputEventScreenDrag drag)
        {
            bool captured = _touchHand.Captured || _touchReadingCaptured;
            if (_touchReadingPointer == drag.Index && _touchHand.PointerCount == 1)
            {
                SourceCardBody? body = _touchHandPreview?.GetNodeOrNull<SourceCardBody>("SourceCardFace/Body");
                float scale = _touchHandPreview?.Scale.Y ?? 1f;
                body?.ScrollReading((_touchReadingY - drag.Position.Y) / Math.Max(.01f, scale));
                _touchReadingY = drag.Position.Y;
            }
            else
            {
                _touchHandPointer = drag.Position;
                _touchHand.Move(drag.Index, drag.Position.Y, _session?.Revision ?? -1, _uxEpoch);
                RefreshTouchHandPresentation();
            }
            if (captured) _touchMouseQuarantineUntil = Time.GetTicksMsec() + 500;
            return captured;
        }
        return false;
    }

    private CardTile? TouchCardAt(Vector2 viewportPoint)
    {
        if (_touchHandPreview != null && GodotObject.IsInstanceValid(_touchHandPreview) &&
            _touchHandPreview.Visible && TouchContains(_touchHandPreview, viewportPoint) &&
            _touchHand.SelectedUid != null && _uxTiles.TryGetValue(_touchHand.SelectedUid, out var selected))
            return selected;
        // Reverse render order is the actual topmost card, including overlapping fan seats.
        return _uxTiles.Values.Where(tile => GodotObject.IsInstanceValid(tile) && !tile.IsQueuedForDeletion() &&
                tile.IsVisibleInTree() && !tile.Disabled && tile.GetParent() == _uxHand &&
                !_uxDeals.ContainsKey(tile.CardUid) && TouchContains(tile, viewportPoint))
            .OrderByDescending(tile => tile.ZIndex).ThenByDescending(tile => tile.GetIndex()).FirstOrDefault();
    }

    private static bool TouchContains(Control control, Vector2 viewportPoint)
    {
        Vector2 local = control.GetGlobalTransformWithCanvas().AffineInverse() * viewportPoint;
        return new Rect2(Vector2.Zero, control.Size).HasPoint(local);
    }

    private bool TouchReadingBodyContains(Vector2 point)
    {
        var body = _touchHandPreview?.GetNodeOrNull<Control>("SourceCardFace/Body");
        return body != null && GodotObject.IsInstanceValid(body) && body.IsVisibleInTree() && TouchContains(body, point);
    }

    private float ComputeTouchHandBoundary()
    {
        float top = Size.Y;
        foreach (var entry in _uxSeats)
        {
            if (!_uxTiles.TryGetValue(entry.Key, out var tile) || !GodotObject.IsInstanceValid(tile)) continue;
            float angle = Mathf.DegToRad(entry.Value.Rotation);
            float scale = UxCardScale * BattleUnit;
            float extent = (Math.Abs(Mathf.Cos(angle)) * tile.Size.Y + Math.Abs(Mathf.Sin(angle)) * tile.Size.X) * scale * .5f;
            float centerY = (_uxHand.GetGlobalTransformWithCanvas() * (entry.Value.Pos + tile.PivotOffset)).Y;
            top = Math.Min(top, centerY - extent);
        }
        return top - Math.Max(6f, 12f * BattleUnit);
    }

    private bool TouchHandContextCurrent()
    {
        string? uid = _touchHand.SelectedUid;
        return uid != null && TouchHandAvailable && _touchHand.Revision == _session!.Revision &&
            _touchHand.Epoch == _uxEpoch && Size == _touchHandCanvasSize &&
            _touchHandOrigin != null && GodotObject.IsInstanceValid(_touchHandOrigin) && !_touchHandOrigin.IsQueuedForDeletion() &&
            _uxTiles.TryGetValue(uid, out var live) && live == _touchHandOrigin && live.GetParent() == _uxHand &&
            InputCombat?.Player.Hand.Any(card => card.Uid == uid) == true;
    }

    private bool TryResolveTouchHandUse(string uid, Vector2 point, long revision, long epoch,
        out CardInstance? card, out int? target, out TouchHandUseRoute route)
    {
        card = null;
        target = null;
        route = TouchHandUseRoute.Play;
        if (!TouchHandContextCurrent() || revision != _session!.Revision || epoch != _uxEpoch ||
            !GetViewport().GetVisibleRect().HasPoint(point) || point.Y >= _touchHandBoundary) return false;
        Vector2 local = GetGlobalTransformWithCanvas().AffineInverse() * point;
        Rect2 safe = RunSafeRect;
        if (!safe.HasPoint(local) || local.Y < safe.Position.Y + 64f * BattleUnit) return false;
        GameState state = InputCombat;
        card = state.Player.Hand.FirstOrDefault(c => c.Uid == uid);
        // Re-read resources, play cap, phase and additional costs at the actual release.
        if (card == null) return false;
        bool canPlay = FlowRules.PlayBlocked(state, card) == null;
        bool canSet = state.PendingScry == null && state.ReactionMode != "hold-manual" && SetBase.CanSetCard(state, uid);
        if (!canPlay && !canSet) return false;
        // Only reaction cards have an unambiguous Set-only meaning. A normal
        // SetAny card keeps its existing choice even when Play is currently blocked.
        route = canSet ? (card.Def.Type == "reaction" ? TouchHandUseRoute.Set : TouchHandUseRoute.ChoosePlayOrSet) : TouchHandUseRoute.Play;
        if (route != TouchHandUseRoute.Set && Effects.CardNeedsTarget(card))
        {
            int index = _enemyResolver?.Invoke(point) ?? -1;
            bool liveTarget = index >= 0 && index < state.Enemies.Count && index < _touchHandEnemySnapshot.Length &&
                state.Enemies[index].Hp > 0 && ReferenceEquals(state.Enemies[index], _touchHandEnemySnapshot[index]);
            if (liveTarget)
            {
                int redirected = FlowRules.GuardRedirect(state, index);
                if (redirected < 0 || redirected >= state.Enemies.Count || state.Enemies[redirected].Hp <= 0) return false;
                target = redirected;
            }
            else if (route == TouchHandUseRoute.Play) return false;
            if (route == TouchHandUseRoute.Play) return true;
        }
        // A non-targeted card still needs the separated battlefield use area.
        // Controls such as End Turn, piles and menu are never a use area.
        return !Buttons(this).Any(button => button is not CardTile && button is not EnemyDropTarget &&
            button.IsVisibleInTree() && !button.IsQueuedForDeletion() && TouchContains(button, point));
    }

    private void BuildTouchHandPresentation(string uid)
    {
        ClearTouchHandPresentation();
        CardInstance? card = InputCombat?.Player.Hand.FirstOrDefault(c => c.Uid == uid);
        if (card == null) return;
        _touchHandPreview = MakeTouchFace(card, "TouchHeldCard", expanded: true);
        _touchHandGhost = MakeTouchFace(card, "TouchDragGhost", expanded: false);
        _touchHandGhost.Visible = false;
        _touchHandAim = new SourceAimArrow { Name = "TouchHandAim", Size = Size, ZIndex = 84, MouseFilter = MouseFilterEnum.Ignore };
        AddChild(_touchHandAim);
        _touchHandBoundaryLine = new ColorRect
        {
            Name = "TouchUseBoundary", ZIndex = 39, MouseFilter = MouseFilterEnum.Ignore,
            Color = new Color(.91f, .78f, .44f, .72f),
            Position = new Vector2(RunSafeRect.Position.X, _touchHandBoundary),
            Size = new Vector2(RunSafeRect.Size.X, Math.Max(1f, BattleUnit * 2f))
        };
        AddChild(_touchHandBoundaryLine);
        if (_touchHandOrigin != null)
        {
            _touchHandSelectionRing = new Panel
            {
                Name = "TouchSelectedRing", MouseFilter = MouseFilterEnum.Ignore,
                Position = new Vector2(-3f, -3f), Size = _touchHandOrigin.Size + new Vector2(6f, 6f), ZIndex = 2
            };
            _touchHandSelectionRing.AddThemeStyleboxOverride("panel", Box(new Color(0, 0, 0, 0), Gold, 3));
            _touchHandOrigin.AddChild(_touchHandSelectionRing);
        }
        RefreshTouchHandPresentation();
    }

    private CardTile MakeTouchFace(CardInstance card, string name, bool expanded)
    {
        CardTile face = new()
        {
            Name = name, CardUid = card.Uid, Size = new Vector2(SourceCardFace.W, SourceCardFace.H),
            MouseFilter = MouseFilterEnum.Ignore, FocusMode = FocusModeEnum.None, ZIndex = expanded ? 80 : 81
        };
        face.SetMeta("source_expanded_reading", expanded);
        AddChild(face);
        SourceCardFace.Fill(face, card, InputCombat, _textScale, PhoneLayout, _preferredTarget,
            Effects.EffectiveCost(InputCombat, card), InputCombat.Player.ImpulseUids.Contains(card.Uid) ? "衝動" : null, true);
        return face;
    }

    private void RefreshTouchHandPresentation()
    {
        if (_touchHand.SelectedUid == null) { ClearTouchHandPresentation(); return; }
        if (!TouchHandContextCurrent()) { CancelTouchHand(); return; }
        if (_touchHandPreview == null || !GodotObject.IsInstanceValid(_touchHandPreview)) return;
        bool aiming = _touchHand.Phase == TouchHandGesture.Stage.Aiming;
        _touchHandPreview.Visible = !aiming;
        if (_touchHandGhost != null) _touchHandGhost.Visible = aiming;
        if (_touchHandBoundaryLine != null) _touchHandBoundaryLine.Visible = _touchHand.HasPointer;
        Rect2 safe = RunSafeRect.Grow(-Math.Max(6f, 10f * BattleUnit));
        Vector2 anchor = GetGlobalTransformWithCanvas().AffineInverse() * _touchHandAnchor;
        float preferredScale = Math.Min(Size.X / 960f, Size.Y / 540f) * (300f / SourceCardFace.W);
        // Keep the enlarged face above the original finger contact, including its contact radius.
        float availableHeight = anchor.Y - Math.Max(28f, 54f * BattleUnit) - safe.Position.Y;
        float previewScale = Math.Min(preferredScale, Math.Min(safe.Size.X / SourceCardFace.W,
            Math.Max(1f, availableHeight) / SourceCardFace.H));
        _touchHandPreview.Scale = Vector2.One * previewScale;
        Vector2 previewSize = _touchHandPreview.Size * previewScale;
        _touchHandPreview.Position = new Vector2(
            Mathf.Clamp(anchor.X - previewSize.X * .5f, safe.Position.X, Math.Max(safe.Position.X, safe.End.X - previewSize.X)),
            Math.Max(safe.Position.Y, anchor.Y - Math.Max(28f, 54f * BattleUnit) - previewSize.Y));
        if (aiming && _touchHandGhost != null && _touchHandAim != null)
        {
            Vector2 pointer = GetGlobalTransformWithCanvas().AffineInverse() * _touchHandPointer;
            float unit = Math.Min(Size.X / 960f, Size.Y / 540f);
            _touchHandGhost.Scale = Vector2.One * (90f * unit / SourceCardFace.W);
            Vector2 ghostSize = _touchHandGhost.Size * _touchHandGhost.Scale;
            _touchHandGhost.Position = new Vector2(
                Mathf.Clamp(pointer.X - ghostSize.X - 30f * unit, safe.Position.X, Math.Max(safe.Position.X, safe.End.X - ghostSize.X)),
                Mathf.Clamp(pointer.Y - ghostSize.Y * .5f, safe.Position.Y, Math.Max(safe.Position.Y, safe.End.Y - ghostSize.Y)));
            _touchHandAim.Active = true;
            _touchHandAim.Unit = Math.Max(.5f, BattleUnit);
            _touchHandAim.Start = _touchHandGhost.Position + new Vector2(ghostSize.X * .5f, 0);
            _touchHandAim.End = pointer;
            bool valid = TryResolveTouchHandUse(_touchHand.SelectedUid, _touchHandPointer, _touchHand.Revision, _touchHand.Epoch, out _, out var target, out var route);
            _touchHandAim.Note = !valid ? "手札へ戻すとキャンセル" : route switch
            {
                TouchHandUseRoute.Set => "離すと仕込む",
                TouchHandUseRoute.ChoosePlayOrSet => "離すと使い方を選ぶ",
                _ => "離すと使用"
            };
            foreach (var enemy in _uxEnemies)
            {
                bool highlighted = valid && target == enemy.Index;
                SourceIntentTag.Highlight(enemy.Intent, highlighted);
                SourceBattleDecor.LedgerEdge(enemy.Ledger, highlighted, BattleUnit);
            }
        }
        else if (_touchHandAim != null) _touchHandAim.Active = false;
        _touchHandAim?.QueueRedraw();
    }

    private void CancelTouchHand(bool resetPointers = false)
    {
        if (_touchHand.Captured || _touchReadingCaptured) _touchMouseQuarantineUntil = Time.GetTicksMsec() + 500;
        if (resetPointers) _touchHand.Reset();
        else _touchHand.Cancel(clearSelection: true);
        _touchReadingPointer = null;
        if (resetPointers) _touchReadingCaptured = false;
        _touchHandOrigin?.SuppressTouchActivation();
        _touchHandOrigin = null;
        _touchHandEnemySnapshot = Array.Empty<EnemyState>();
        ClearTouchHandPresentation();
    }

    private void ClearTouchHandPresentation()
    {
        static void Retire(Control? control)
        {
            if (control == null || !GodotObject.IsInstanceValid(control) || control.IsQueuedForDeletion()) return;
            control.Hide();
            control.QueueFree();
        }
        Retire(_touchHandPreview);
        Retire(_touchHandGhost);
        Retire(_touchHandSelectionRing);
        Retire(_touchHandAim);
        Retire(_touchHandBoundaryLine);
        _touchHandPreview = null;
        _touchHandGhost = null;
        _touchHandSelectionRing = null;
        _touchHandAim = null;
        _touchHandBoundaryLine = null;
        foreach (var enemy in _uxEnemies)
        {
            if (!GodotObject.IsInstanceValid(enemy.Intent) || enemy.Intent.IsQueuedForDeletion()) continue;
            SourceIntentTag.Highlight(enemy.Intent, enemy.Index == _preferredTarget);
            if (GodotObject.IsInstanceValid(enemy.Ledger) && !enemy.Ledger.IsQueuedForDeletion())
                SourceBattleDecor.LedgerEdge(enemy.Ledger, enemy.Index == _preferredTarget, BattleUnit);
        }
    }
}

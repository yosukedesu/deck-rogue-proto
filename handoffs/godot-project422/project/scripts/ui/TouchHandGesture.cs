using System;
using System.Collections.Generic;

namespace DeckRogue.GodotPort.UI;

/// <summary>
/// The actual touch admission state used by GameUI. No timers admit a play: only an
/// owned pointer's release after both upward movement and the hand boundary do.
/// Board changes invalidate the admission, and it is spent before dispatch.
/// </summary>
internal sealed class TouchHandGesture
{
    internal enum Stage { Idle, Reading, Pressed, Aiming, Cancelled }
    internal readonly record struct Release(string Uid, long Revision, long Epoch, bool AttemptUse);
    private readonly HashSet<int> _pointers = new();
    private int? _owner;
    private bool _captured;
    private float _startY;
    private float _boundary;
    private float _threshold;
    private long _revision;
    private long _epoch;

    internal Stage Phase { get; private set; }
    internal string? SelectedUid { get; private set; }
    internal bool Captured => _captured;
    internal bool HasPointer => _owner.HasValue;
    internal int PointerCount => _pointers.Count;
    internal long Revision => _revision;
    internal long Epoch => _epoch;

    internal bool Press(int pointer, string? uid, float y, float boundary, float threshold, long revision, long epoch)
    {
        bool duplicate = !_pointers.Add(pointer);
        if (duplicate || _pointers.Count > 1)
        {
            _captured |= uid != null;
            Cancel(clearSelection: true);
            return _captured;
        }
        if (uid == null) return _captured;
        _owner = pointer;
        _captured = true;
        SelectedUid = uid;
        Phase = Stage.Pressed;
        _startY = y;
        _boundary = boundary;
        _threshold = Math.Max(1f, threshold);
        _revision = revision;
        _epoch = epoch;
        return true;
    }

    internal bool Move(int pointer, float y, long revision, long epoch)
    {
        if (!_captured) return false;
        if (_owner != pointer) return true;
        if (revision != _revision || epoch != _epoch) Cancel(clearSelection: true);
        else if (Phase == Stage.Pressed && _startY - y >= _threshold && y < _boundary)
            Phase = Stage.Aiming;
        return true;
    }

    internal Release? End(int pointer, float y, bool cancelled, long revision, long epoch)
    {
        _pointers.Remove(pointer);
        Release? release = null;
        if (_owner == pointer && SelectedUid != null)
        {
            bool current = revision == _revision && epoch == _epoch;
            bool admit = !cancelled && current && Phase == Stage.Aiming && y < _boundary;
            release = new Release(SelectedUid, _revision, _epoch, admit);
            _owner = null; // Spend the only release token before any external callback.
            if (cancelled || !current) Cancel(clearSelection: true);
            else Phase = Stage.Reading;
        }
        if (_pointers.Count == 0)
        {
            _captured = false;
            if (Phase == Stage.Cancelled) Phase = SelectedUid == null ? Stage.Idle : Stage.Reading;
        }
        return release;
    }

    internal void Cancel(bool clearSelection)
    {
        _owner = null;
        if (clearSelection) SelectedUid = null;
        Phase = _pointers.Count > 0 ? Stage.Cancelled : SelectedUid == null ? Stage.Idle : Stage.Reading;
        // Keep capture until every finger is lifted. A second finger never inherits it.
    }

    internal void Reset()
    {
        _pointers.Clear();
        _captured = false;
        _owner = null;
        SelectedUid = null;
        Phase = Stage.Idle;
    }
}

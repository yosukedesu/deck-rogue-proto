#nullable enable
using System;
using System.Collections.Generic;

namespace DeckRogue.GodotPort.Presentation;

/// <summary>Reconstructed from Unity StageCombat.cs:7-27.
/// Exactly one bounded live window; past intervals exist only to integrate unsampled elapsed time.
/// The actor discards consumed history after every sample. These intervals never queue future pauses.</summary>
public sealed class SourceImpactPause
{
    public const float MaxWindow = .14f, ResumeGap = .050f;
    float _until, _cap, _ready;
    readonly List<(float Start, float End)> _history = new();

    public float Remaining(float now) => float.IsFinite(now) ? Math.Max(0f, _until - now) : 0f;
    public bool IsPaused(float now) => float.IsFinite(now) && now < _until;
    public float Until => _until;
    public float ReadyAt => _ready;

    public bool Request(float seconds, float now)
    {
        if (!float.IsFinite(now) || now < 0 || !float.IsFinite(seconds) || seconds <= 0) return false;
        seconds = Math.Min(seconds, MaxWindow);
        if (now < _until)
        {
            _until = Math.Min(_cap, Math.Max(_until, now + seconds));
            // A live interval cannot have been pruned by a monotonic sample clock.
            var previous = _history[^1];
            _history[^1] = (previous.Start, _until);
        }
        else
        {
            if (now < _ready) return false;
            _cap = now + MaxWindow;
            _until = now + seconds;
            _history.Add((now, _until));
        }
        _ready = _until + ResumeGap;
        return true;
    }

    public float UnpausedDuration(float from, float to)
    {
        if (!float.IsFinite(from) || !float.IsFinite(to) || to <= from) return 0;
        float paused = 0;
        foreach (var window in _history)
            paused += Math.Max(0, Math.Min(to, window.End) - Math.Max(from, window.Start));
        return Math.Max(0, to - from - paused);
    }

    public void DiscardHistoryThrough(float now)
    {
        int count = 0;
        while (count < _history.Count && _history[count].End <= now) count++;
        if (count > 0) _history.RemoveRange(0, count);
    }

    // Action handoff removes the future hold, preserving already elapsed overlap for an
    // independently owned recoil that has not been sampled yet this frame.
    public void Clear(float now)
    {
        if (_history.Count > 0 && now < _history[^1].End)
        {
            var previous = _history[^1];
            _history[^1] = (previous.Start, Math.Max(previous.Start, now));
        }
        _until = _cap = _ready = 0;
    }

    public void Clear()
    {
        _until = _cap = _ready = 0;
        _history.Clear();
    }
}

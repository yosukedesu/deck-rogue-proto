using System;
using System.Collections.Generic;

namespace DeckRogue.GodotPort.Presentation;

public sealed class SourceImpactPause
{
	public const float MaxWindow = 0.14f;

	public const float ResumeGap = 0.05f;

	private float _until;

	private float _cap;

	private float _ready;

	private readonly List<(float Start, float End)> _history = new List<(float, float)>();

	public float Until => _until;

	public float ReadyAt => _ready;

	public float Remaining(float now)
	{
		if (!float.IsFinite(now))
		{
			return 0f;
		}
		return Math.Max(0f, _until - now);
	}

	public bool IsPaused(float now)
	{
		if (float.IsFinite(now))
		{
			return now < _until;
		}
		return false;
	}

	public bool Request(float seconds, float now)
	{
		if (!float.IsFinite(now) || now < 0f || !float.IsFinite(seconds) || seconds <= 0f)
		{
			return false;
		}
		seconds = Math.Min(seconds, 0.14f);
		if (now < _until)
		{
			_until = Math.Min(_cap, Math.Max(_until, now + seconds));
			List<(float Start, float End)> history = _history;
			(float, float) previous = history[history.Count - 1];
			List<(float Start, float End)> history2 = _history;
			history2[history2.Count - 1] = (previous.Item1, _until);
		}
		else
		{
			if (now < _ready)
			{
				return false;
			}
			_cap = now + 0.14f;
			_until = now + seconds;
			_history.Add((now, _until));
		}
		_ready = _until + 0.05f;
		return true;
	}

	public float UnpausedDuration(float from, float to)
	{
		if (!float.IsFinite(from) || !float.IsFinite(to) || to <= from)
		{
			return 0f;
		}
		float paused = 0f;
		foreach (var window in _history)
		{
			paused += Math.Max(0f, Math.Min(to, window.End) - Math.Max(from, window.Start));
		}
		return Math.Max(0f, to - from - paused);
	}

	public void DiscardHistoryThrough(float now)
	{
		int count;
		for (count = 0; count < _history.Count && _history[count].End <= now; count++)
		{
		}
		if (count > 0)
		{
			_history.RemoveRange(0, count);
		}
	}

	public void Clear(float now)
	{
		if (_history.Count > 0)
		{
			List<(float Start, float End)> history = _history;
			if (now < history[history.Count - 1].End)
			{
				List<(float Start, float End)> history2 = _history;
				(float, float) previous = history2[history2.Count - 1];
				List<(float Start, float End)> history3 = _history;
				history3[history3.Count - 1] = (previous.Item1, Math.Max(previous.Item1, now));
			}
		}
		_until = (_cap = (_ready = 0f));
	}

	public void Clear()
	{
		_until = (_cap = (_ready = 0f));
		_history.Clear();
	}
}

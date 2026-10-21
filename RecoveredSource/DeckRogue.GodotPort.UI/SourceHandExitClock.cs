using System;

namespace DeckRogue.GodotPort.UI;

public sealed class SourceHandExitClock
{
	private double _remainingDelay;

	private ulong _lastFrame;

	private bool _startTaken;

	public bool Started { get; private set; }

	public bool Cancelled { get; private set; }

	public double Elapsed { get; private set; }

	public SourceHandExitClock(double delay, ulong startFrame)
	{
		_remainingDelay = (double.IsFinite(delay) ? Math.Max(0.0, delay) : 0.0);
		Started = _remainingDelay == 0.0;
		_lastFrame = startFrame;
	}

	public void Advance(double delta, ulong frame)
	{
		if (Cancelled || frame == _lastFrame)
		{
			return;
		}
		_lastFrame = frame;
		if (!double.IsFinite(delta) || delta <= 0.0)
		{
			return;
		}
		if (!Started)
		{
			_remainingDelay -= delta;
			if (_remainingDelay <= 0.0)
			{
				Started = true;
			}
		}
		else
		{
			Elapsed = Math.Min(0.3, Elapsed + delta);
		}
	}

	public bool TakeStart()
	{
		if (Cancelled || !Started || _startTaken)
		{
			return false;
		}
		_startTaken = true;
		return true;
	}

	public void Cancel()
	{
		Cancelled = true;
	}
}

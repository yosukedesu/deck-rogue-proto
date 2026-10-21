using System;

namespace DeckRogue.GodotPort.UI;

public sealed class SourceHandHoverClock
{
	private readonly double _duration;

	private ulong _lastFrame;

	public double Elapsed { get; private set; }

	public bool Cancelled { get; private set; }

	public bool Complete => Elapsed >= _duration;

	public SourceHandHoverClock(ulong startFrame, double duration = 0.12)
	{
		_lastFrame = startFrame;
		_duration = ((double.IsFinite(duration) && duration > 0.0) ? duration : 0.12);
	}

	public void Advance(double delta, ulong frame)
	{
		if (!Cancelled && frame != _lastFrame)
		{
			_lastFrame = frame;
			if (double.IsFinite(delta) && delta > 0.0)
			{
				Elapsed = Math.Min(_duration, Elapsed + delta);
			}
		}
	}

	public void Cancel()
	{
		Cancelled = true;
	}
}

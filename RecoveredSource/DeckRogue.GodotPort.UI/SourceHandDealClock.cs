using System;

namespace DeckRogue.GodotPort.UI;

public sealed class SourceHandDealClock
{
	private double _delay;

	private bool _cueTaken;

	public bool Started { get; private set; }

	public bool Cancelled { get; private set; }

	public double Elapsed { get; private set; }

	public SourceHandDealClock(double delay)
	{
		_delay = Math.Max(0.0, delay);
		Started = _delay == 0.0;
	}

	public void Advance(double delta)
	{
		if (Cancelled || !double.IsFinite(delta) || delta < 0.0)
		{
			return;
		}
		if (!Started)
		{
			_delay -= delta;
			if (_delay <= 1E-09)
			{
				Started = true;
			}
		}
		else
		{
			Elapsed = Math.Min(0.34, Elapsed + delta);
		}
	}

	public bool TakeStartCue()
	{
		if (Cancelled || !Started || _cueTaken)
		{
			return false;
		}
		_cueTaken = true;
		return true;
	}

	public void Cancel()
	{
		Cancelled = true;
	}
}

using System;

namespace DeckRogue.GodotPort.Presentation;

public sealed class SourceFrameDelay
{
	private float _duration;

	private int _lastFrame;

	public bool Pending { get; private set; }

	public float Elapsed { get; private set; }

	public void Begin(float duration, int frame)
	{
		if (!float.IsFinite(duration) || duration < 0f)
		{
			throw new ArgumentOutOfRangeException("duration");
		}
		_duration = duration;
		Elapsed = 0f;
		_lastFrame = frame;
		Pending = duration > 0f;
	}

	public bool Sample(float delta, int frame)
	{
		if (!float.IsFinite(delta) || delta < 0f)
		{
			throw new ArgumentOutOfRangeException("delta");
		}
		if (!Pending || frame <= _lastFrame)
		{
			return false;
		}
		_lastFrame = frame;
		Elapsed += delta;
		if (Elapsed < _duration)
		{
			return false;
		}
		Pending = false;
		return true;
	}
}

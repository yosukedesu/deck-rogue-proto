using System;
using Godot;

namespace DeckRogue.GodotPort.Presentation;

public sealed class SourceUiLunge
{
	private bool _running;

	private bool _contactHold;

	private Vector3 _travel;

	private Vector3 _pendingTravel;

	private float _duration;

	private float _baseDuration;

	private float _holdAtPeak;

	private float _pendingHoldAtPeak;

	private float _delayElapsed;

	private float _delay;

	private float _pendingDuration;

	private float _lastClock;

	private int _startedFrame;

	private int _lastFrame;

	private int _pendingStartedFrame;

	private int _pendingLastFrame;

	private float _pendingLastClock;

	public Vector3 Offset { get; private set; }

	public bool Active
	{
		get
		{
			if (!_running)
			{
				return Pending;
			}
			return true;
		}
	}

	public bool Pending { get; private set; }

	public bool ContactHoldArmed => _contactHold;

	public float Elapsed { get; private set; }

	public int Version { get; private set; }

	public void Begin(Vector3 travel, float duration, float now, int frame, float delay = 0f, float holdAtPeak = 0f)
	{
		if (!float.IsFinite(duration) || duration <= 0f || !float.IsFinite(delay) || delay < 0f || !float.IsFinite(now) || now < 0f || !float.IsFinite(holdAtPeak) || holdAtPeak < 0f)
		{
			throw new ArgumentOutOfRangeException("duration");
		}
		Version++;
		_contactHold = false;
		if (delay > 0f)
		{
			Pending = true;
			_pendingTravel = travel;
			_pendingDuration = duration;
			_pendingHoldAtPeak = holdAtPeak;
			_delay = delay;
			_delayElapsed = 0f;
			_pendingStartedFrame = (_pendingLastFrame = frame);
			_pendingLastClock = now;
		}
		else
		{
			Pending = false;
			Start(travel, duration, now, frame, holdAtPeak);
		}
	}

	private void Start(Vector3 travel, float duration, float now, int frame, float holdAtPeak)
	{
		_travel = travel;
		_baseDuration = duration;
		_holdAtPeak = holdAtPeak;
		_duration = duration + holdAtPeak;
		Elapsed = 0f;
		_startedFrame = (_lastFrame = frame);
		_lastClock = now;
		_running = true;
		Offset = Vector3.Zero;
	}

	public void Sample(float now, float delta, int frame)
	{
		if (!float.IsFinite(delta) || delta < 0f || !float.IsFinite(now) || now < 0f)
		{
			throw new ArgumentOutOfRangeException("delta");
		}
		if (_running && frame > _startedFrame && frame > _lastFrame && now >= _lastClock)
		{
			_lastFrame = frame;
			float elapsed = Math.Min(delta, Math.Max(0f, now - _lastClock));
			_lastClock = now;
			Elapsed = ContactBound(Math.Min(_duration, Elapsed + elapsed));
			if (Elapsed >= _duration)
			{
				_running = false;
				Offset = Vector3.Zero;
			}
			else
			{
				Offset = _travel * SampleAmount();
			}
		}
		if (Pending && frame > _pendingStartedFrame && frame > _pendingLastFrame && now >= _pendingLastClock)
		{
			_pendingLastFrame = frame;
			_delayElapsed += Math.Min(delta, Math.Max(0f, now - _pendingLastClock));
			_pendingLastClock = now;
			if (_delayElapsed >= _delay)
			{
				Pending = false;
				Start(_pendingTravel, _pendingDuration, now, frame, _pendingHoldAtPeak);
				// Preserve the part of this host frame after the scheduled start.
				// Otherwise contact-aligned lunges drift by up to one frame.
				Elapsed = ContactBound(Math.Min(_duration, _delayElapsed - _delay));
				_running = Elapsed < _duration;
				Offset = _running ? _travel * SampleAmount() : Vector3.Zero;
			}
		}
	}

	public void ArmContactHold()
	{
		_contactHold = Active;
	}

	public bool ReleaseContactHold(float now, int frame)
	{
		if (!float.IsFinite(now) || now < 0f) throw new ArgumentOutOfRangeException("now");
		if (!_contactHold) return false;
		_contactHold = false;
		if (Pending)
		{
			Pending = false;
			Start(_pendingTravel, _pendingDuration, now, frame, _pendingHoldAtPeak);
		}
		if (!_running) return false;
		// The ledger contact is the visual anchor, even when a host frame crossed
		// the planned peak. Recovery consumes only time after this actual contact.
		Elapsed = _baseDuration * 0.35f + _holdAtPeak;
		Offset = _travel;
		_lastClock = now;
		_lastFrame = frame;
		return true;
	}

	private float ContactBound(float elapsed)
	{
		return _contactHold ? Math.Min(elapsed, _baseDuration * 0.35f + _holdAtPeak) : elapsed;
	}

	public void Cancel()
	{
		Version++;
		_contactHold = false;
		bool running = (Pending = false);
		_running = running;
		Elapsed = 0f;
		Offset = Vector3.Zero;
	}

	private float SampleAmount()
	{
		float peak = _baseDuration * 0.35f;
		float motionElapsed = Elapsed <= peak ? Elapsed : Math.Max(peak, Elapsed - _holdAtPeak);
		return Amount(motionElapsed / _baseDuration);
	}

	public static float Amount(float k)
	{
		k = Math.Clamp(k, 0f, 1f);
		if (k < 0.35f)
		{
			float t = k / 0.35f;
			return 1f - (1f - t) * (1f - t);
		}
		float u = 1f - (k - 0.35f) / 0.65f;
		return u * u * u;
	}
}

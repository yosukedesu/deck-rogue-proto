using System;
using Godot;

namespace DeckRogue.GodotPort.Presentation;

public sealed class SourceActorMotion
{
	public readonly struct OwnershipToken
	{
		private readonly SourceActorMotion? _owner;

		public int MotionVersion { get; }

		public bool IsCurrent
		{
			get
			{
				if (_owner != null)
				{
					return _owner.MotionVersion == MotionVersion;
				}
				return false;
			}
		}

		internal OwnershipToken(SourceActorMotion owner)
		{
			_owner = owner;
			MotionVersion = owner.MotionVersion;
		}

		internal bool BelongsTo(SourceActorMotion owner)
		{
			if (_owner == owner)
			{
				return IsCurrent;
			}
			return false;
		}
	}

	private sealed class Movement
	{
		public float Elapsed;

		public float Duration;

		public float LastClock;

		public int StartedFrame;

		public int LastFrame;

		public bool LocalPause;

		public Vector3 Start;

		public Vector3 Travel;

		public float Windup;

		public float Contact;

		public float Release;

		public float Recovery;

		public float Anticipation;
	}

	public const float CombatImpactTime = 0.12f;

	private readonly SourceImpactPause _pause = new SourceImpactPause();

	private Movement? _approach;

	private Movement? _recoil;

	private float _attackDelay;

	private float _attackDelayElapsed;

	private int _attackFrame;

	private float _attackLastClock;

	private int _attackVersion;

	private bool _attackPending;

	private int _attackContactVersion = -1;

	public int MotionVersion { get; private set; }

	public float RequestedPoseClock { get; private set; }

	public int RecoilVersion { get; private set; }

	public Vector3 ApproachOffset { get; private set; }

	public Vector3 RecoilOffset { get; private set; }

	public Vector3 Offset => ApproachOffset + RecoilOffset;

	public bool ApproachActive => _approach != null;

	public bool RecoilActive => _recoil != null;

	public bool HasMotion
	{
		get
		{
			if (!ApproachActive)
			{
				return RecoilActive;
			}
			return true;
		}
	}

	public bool MotionActive => HasMotion;

	public bool HasOwnedMotion { get; private set; }

	public float ApproachElapsed => _approach?.Elapsed ?? 0f;

	public float RecoilElapsed => _recoil?.Elapsed ?? 0f;

	public bool IsPaused(float now)
	{
		return _pause.IsPaused(now);
	}

	public float UnpausedDuration(float from, float to)
	{
		return _pause.UnpausedDuration(from, to);
	}

	public float PauseRemaining(float now)
	{
		return _pause.Remaining(now);
	}

	public OwnershipToken Capture()
	{
		return new OwnershipToken(this);
	}

	public bool Owns(OwnershipToken token)
	{
		return token.BelongsTo(this);
	}

	// A rendered contact is the authoritative boundary for its attack drawing.
	// Cancel only the delayed pose request owned by this action, never motion.
	public bool SynchronizeAttackContact(OwnershipToken token)
	{
		if (!Owns(token) || _attackContactVersion == MotionVersion) return false;
		_attackContactVersion = MotionVersion;
		_attackPending = false;
		return true;
	}

	public bool RequestOwnedPause(OwnershipToken token, float seconds, float now)
	{
		if (Owns(token))
		{
			return _pause.Request(seconds, now);
		}
		return false;
	}

	public void BeginAction(float now, int frame)
	{
		RequireClock(now);
		MotionVersion++;
		HasOwnedMotion = true;
		_pause.Clear(now);
		_attackPending = false;
		// A queued card can launch before the previous recovery is complete.
		// Keep the sampled position: Approach inherits it as its start; a
		// non-attack action lets the previous movement finish returning home.
	}

	public string Approach(Vector3 actorFeet, float actorHeight, Vector3 targetFeet, float targetHeight, bool ranged, int hits, SourceShowcaseProfile? profile, float now, int frame, Vector3? layoutRight = null, string? artId = null, float reachLimit = 0.4f, float attackPoseLead = 0.12f, float? targetClearanceOverride = null, float? approachAnticipationOverride = null)
	{
		RequireClock(now);
		RequireVector(actorFeet, "actorFeet");
		RequireVector(targetFeet, "targetFeet");
		RequireNonnegative(actorHeight, "actorHeight");
		RequireNonnegative(targetHeight, "targetHeight");
		RequireNonnegative(reachLimit, "reachLimit");
		RequireNonnegative(attackPoseLead, "attackPoseLead");
        if(approachAnticipationOverride.HasValue && !float.IsFinite(approachAnticipationOverride.Value))throw new ArgumentOutOfRangeException("approachAnticipationOverride");
		Vector3 right = layoutRight ?? Vector3.Right;
		RequireVector(right, "layoutRight");
		ValidateProfile(profile);
		MotionVersion++;
		HasOwnedMotion = true;
		_pause.Clear(now);
		_attackPending = false;
		Vector3 delta = targetFeet - actorFeet;
		delta.Y = 0f;
		float distance = delta.Length();
		Vector3 direction;
		if (distance > 0.01f)
		{
			direction = delta / distance;
		}
		else
		{
			direction = (((object)profile == null) ? Vector3.Right : right);
		}
		if(targetClearanceOverride.HasValue) RequireNonnegative(targetClearanceOverride.Value, "targetClearanceOverride");
        float clearance = Math.Max(0f, distance - (targetClearanceOverride ?? (actorHeight * 0.4f + targetHeight * 0.25f)));
		float reach;
		if ((object)profile != null)
		{
			reach = (ranged ? (actorHeight * 0.035f) : Math.Min(actorHeight * profile.Reach, clearance));
		}
		else
		{
			reach = (ranged ? 0f : Math.Min(actorHeight * reachLimit, clearance));
		}
		float release = (((object)profile == null) ? (0.2f + Math.Min(ranged ? 0f : ((float)Math.Clamp((long)hits - 1L, 0L, 7L) * 0.4f), 0.12f)) : (profile.Contact + (float)Math.Max(0L, (long)hits - 1L) * profile.Interval + 0.045f));
		float recovery = profile?.Recovery ?? 0.18f;
		_approach = new Movement
		{
			Start = ApproachOffset,
			Travel = direction * reach,
			Windup = (profile?.Windup ?? 0.035f),
			Contact = (profile?.Contact ?? 0.12f),
			Release = release,
			Recovery = recovery,
			Duration = release + recovery,
			Anticipation = approachAnticipationOverride ?? (((object)profile == null) ? (-0.035f) : (-0.16f)),
			LocalPause = ((object)profile != null),
			LastClock = now,
			StartedFrame = frame,
			LastFrame = frame
		};
		if ((object)profile != null)
		{
			_attackDelay = Math.Max(0f, profile.Contact - attackPoseLead);
			_attackDelayElapsed = 0f;
			_attackFrame = frame;
			_attackLastClock = now;
			_attackVersion = MotionVersion;
			_attackPending = _attackDelay > 0f;
		}
		EvaluateApproach();
		if (((object)profile != null && !_attackPending) || ((object)profile == null && (!ranged || !(artId != "leader_blue"))))
		{
			return "attack";
		}
		// A delayed offensive pose is anticipation, not a defensive action.
		return (object)profile != null ? "idle" : "block";
	}

	public string Recoil(Vector3 direction, float height, bool heavy, float now, int frame, bool showcase = true)
	{
		RequireClock(now);
		RequireVector(direction, "direction");
		RequireNonnegative(height, "height");
		RecoilVersion++;
		HasOwnedMotion = true;
		_recoil = new Movement
		{
			Travel = direction * height * ((!showcase) ? (heavy ? 0.12f : 0.065f) : (heavy ? 0.15f : 0.085f)),
			Duration = (heavy ? 0.29f : 0.22f),
			Anticipation = (showcase ? 0.35f : 0f),
			LocalPause = showcase,
			LastClock = now,
			StartedFrame = frame,
			LastFrame = frame
		};
		EvaluateRecoil();
		return "hurt";
	}

	public string? OwnedImpact(OwnershipToken token, Vector3 direction, float height, bool heavy, float seconds, float now, int frame)
	{
		if (!Owns(token))
		{
			return null;
		}
		string result = Recoil(direction, height, heavy, now, frame);
		RequestOwnedPause(token, seconds, now);
		return result;
	}

	public void Cancel()
	{
		MotionVersion++;
		RecoilVersion++;
		_pause.Clear();
		_approach = (_recoil = null);
		_attackPending = false;
		Vector3 approachOffset = (RecoilOffset = Vector3.Zero);
		ApproachOffset = approachOffset;
	}

	public string? Sample(float now, int frame, float frameDelta)
	{
		RequireClock(now);
		if (!float.IsFinite(frameDelta) || frameDelta < 0f)
		{
			throw new ArgumentOutOfRangeException("frameDelta");
		}
		Movement approach = _approach;
		if (approach != null)
		{
			Advance(approach, now, frame, frameDelta);
			EvaluateApproach();
		}
		Movement recoil = _recoil;
		if (recoil != null)
		{
			Advance(recoil, now, frame, frameDelta);
			EvaluateRecoil();
		}
		string pose = null;
		if (_attackPending && _attackVersion == MotionVersion && frame > _attackFrame && now >= _attackLastClock)
		{
			_attackFrame = frame;
			_attackDelayElapsed += Math.Min(frameDelta, Math.Max(0f, now - _attackLastClock));
			_attackLastClock = now;
			if (_attackDelayElapsed >= _attackDelay)
			{
				_attackPending = false;
				RequestedPoseClock = now - (_attackDelayElapsed - _attackDelay);
				pose = "attack";
			}
		}
		float consumedThrough = now;
		Movement a = _approach;
		if (a != null)
		{
			consumedThrough = Math.Min(consumedThrough, a.LastClock);
		}
		Movement r = _recoil;
		if (r != null)
		{
			consumedThrough = Math.Min(consumedThrough, r.LastClock);
		}
		// Sprite sampling follows movement sampling in the same host frame.
		// Retain its last interval so both consume exactly the same hitstop.
		_pause.DiscardHistoryThrough(consumedThrough - frameDelta);
		return pose;
	}

	public string? Sample(float now, float frameDelta, int frame)
	{
		return Sample(now, frame, frameDelta);
	}

	private void Advance(Movement movement, float now, int frame, float frameDelta)
	{
		if (frame > movement.StartedFrame && frame > movement.LastFrame && !(now < movement.LastClock))
		{
			float elapsed = Math.Min(frameDelta, Math.Max(0f, now - movement.LastClock));
			if (movement.LocalPause)
			{
				elapsed = _pause.UnpausedDuration(now - elapsed, now);
			}
			movement.Elapsed = Math.Min(movement.Duration, movement.Elapsed + elapsed);
			movement.LastClock = now;
			movement.LastFrame = frame;
		}
	}

	private void EvaluateApproach()
	{
		Movement movement = _approach;
		if (movement != null)
		{
			if (movement.Elapsed >= movement.Duration)
			{
				ApproachOffset = Vector3.Zero;
				_approach = null;
			}
			else
			{
				float t = movement.Elapsed;
				float amount;
				if (t < movement.Windup)
				{
					amount = movement.Anticipation * Smooth(t / movement.Windup);
				}
				else if (t < movement.Contact)
				{
					amount = Lerp(movement.Anticipation, 1f, Smooth((t - movement.Windup) / (movement.Contact - movement.Windup)));
				}
				else
				{
					amount = ((!(t < movement.Release)) ? (1f - Smooth((t - movement.Release) / movement.Recovery)) : 1f);
				}
				ApproachOffset = movement.Start * (1f - t / movement.Duration) + movement.Travel * amount;
			}
		}
	}

	private void EvaluateRecoil()
	{
		Movement movement = _recoil;
		if (movement != null)
		{
			if (movement.Elapsed >= movement.Duration)
			{
				RecoilOffset = Vector3.Zero;
				_recoil = null;
			}
			else
			{
				float k = movement.Elapsed / movement.Duration;
				float amount = ((k < 0.18f) ? Lerp(movement.Anticipation, 1f, Smooth(k / 0.18f)) : (1f - Smooth((k - 0.18f) / 0.82f)));
				RecoilOffset = movement.Travel * amount;
			}
		}
	}

	private static float Smooth(float value)
	{
		value = Math.Clamp(value, 0f, 1f);
		return value * value * (3f - 2f * value);
	}

	private static float Lerp(float a, float b, float t)
	{
		return a + (b - a) * t;
	}

	private static void RequireClock(float now)
	{
		if (!float.IsFinite(now) || now < 0f)
		{
			throw new ArgumentOutOfRangeException("now");
		}
	}

	private static void RequireNonnegative(float value, string name)
	{
		if (!float.IsFinite(value) || value < 0f)
		{
			throw new ArgumentOutOfRangeException(name);
		}
	}

	private static void RequireVector(Vector3 value, string name)
	{
		if (!float.IsFinite(value.X) || !float.IsFinite(value.Y) || !float.IsFinite(value.Z))
		{
			throw new ArgumentOutOfRangeException(name);
		}
	}

	private static void ValidateProfile(SourceShowcaseProfile? profile)
	{
		if ((object)profile != null)
		{
			RequireNonnegative(profile.Contact, "Contact");
			RequireNonnegative(profile.Windup, "Windup");
			RequireNonnegative(profile.Interval, "Interval");
			RequireNonnegative(profile.Reach, "Reach");
			if (profile.Contact <= profile.Windup || !float.IsFinite(profile.Recovery) || profile.Recovery <= 0f)
			{
				throw new ArgumentOutOfRangeException("profile");
			}
		}
	}
}

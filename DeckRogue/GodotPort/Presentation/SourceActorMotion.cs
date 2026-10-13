#nullable enable
using System;
using Godot;

namespace DeckRogue.GodotPort.Presentation;

/// <summary>Reconstructed actor-local motion from preserved Unity StageCombat.cs.
/// Managed math only: no Godot objects, process frame queries, tweens, global clocks, or gameplay rules.
/// The caller supplies a monotonic presentation clock and manual Advance frame number.</summary>
public sealed class SourceActorMotion
{
    public const float CombatImpactTime = .12f;
    public int MotionVersion { get; private set; }
    public int RecoilVersion { get; private set; }
    public Vector3 ApproachOffset { get; private set; }
    public Vector3 RecoilOffset { get; private set; }
    public Vector3 Offset => ApproachOffset + RecoilOffset;
    public bool ApproachActive => _approach is not null;
    public bool RecoilActive => _recoil is not null;
    public bool HasMotion => ApproachActive || RecoilActive;
    public bool MotionActive => HasMotion;
    // Once source-owned, legacy progress bob must not be reintroduced when motion finishes.
    public bool HasOwnedMotion { get; private set; }
    public float ApproachElapsed => _approach?.Elapsed ?? 0;
    public float RecoilElapsed => _recoil?.Elapsed ?? 0;
    public bool IsPaused(float now) => _pause.IsPaused(now);
    public float PauseRemaining(float now) => _pause.Remaining(now);

    /// <summary>Identity plus motion generation. Tokens cannot be forged through the public API.</summary>
    public readonly struct OwnershipToken
    {
        readonly SourceActorMotion? _owner;
        public int MotionVersion { get; }
        internal OwnershipToken(SourceActorMotion owner) { _owner = owner; MotionVersion = owner.MotionVersion; }
        public bool IsCurrent => _owner is not null && _owner.MotionVersion == MotionVersion;
        internal bool BelongsTo(SourceActorMotion owner) => ReferenceEquals(_owner, owner) && IsCurrent;
    }

    sealed class Movement
    {
        public float Elapsed, Duration, LastClock;
        public int StartedFrame, LastFrame;
        public bool LocalPause;
        public Vector3 Start, Travel;
        public float Windup, Contact, Release, Recovery, Anticipation;
    }
    readonly SourceImpactPause _pause = new();
    Movement? _approach, _recoil;
    float _attackDelay, _attackDelayElapsed;
    int _attackFrame, _attackVersion;
    bool _attackPending;

    public OwnershipToken Capture() => new(this);
    public bool Owns(OwnershipToken token) => token.BelongsTo(this);
    public bool RequestOwnedPause(OwnershipToken token, float seconds, float now) =>
        Owns(token) && _pause.Request(seconds, now);

    public void BeginAction(float now, int frame)
    {
        RequireClock(now);
        MotionVersion++;
        HasOwnedMotion = true;
        _pause.Clear(now);
        _approach = null;
        _attackPending = false;
        ApproachOffset = Vector3.Zero;
        // Source BeginCombatAction intentionally does not cancel independent recoil ownership.
    }

    /// <summary>Returns the initial authored pose. A null profile selects the baseline approach.
    /// Showcase poses switch to attack at Contact-.12 on the caller's next eligible frame.</summary>
    public string Approach(Vector3 actorFeet, float actorHeight, Vector3 targetFeet, float targetHeight,
        bool ranged, int hits, SourceShowcaseProfile? profile, float now, int frame,
        Vector3? layoutRight = null, string? artId = null, float reachLimit = .4f)
    {
        RequireClock(now);
        RequireVector(actorFeet, nameof(actorFeet)); RequireVector(targetFeet, nameof(targetFeet));
        RequireNonnegative(actorHeight, nameof(actorHeight)); RequireNonnegative(targetHeight, nameof(targetHeight));
        RequireNonnegative(reachLimit, nameof(reachLimit));
        var right = layoutRight ?? Vector3.Right; RequireVector(right, nameof(layoutRight));
        ValidateProfile(profile);
        MotionVersion++;
        HasOwnedMotion = true;
        _pause.Clear(now);
        _attackPending = false;
        var delta = targetFeet - actorFeet; delta.Y = 0;
        float distance = delta.Length();
        var direction = distance > .01f ? delta / distance : profile is null ? Vector3.Right : right;
        float clearance = Math.Max(0, distance - actorHeight * .40f - targetHeight * .25f);
        float reach = profile is null
            ? ranged ? 0 : Math.Min(actorHeight * reachLimit, clearance)
            : ranged ? actorHeight * .035f : Math.Min(actorHeight * profile.Reach, clearance);
        float release = profile is null
            ? .20f + Math.Min(ranged ? 0 : Math.Clamp((long)hits - 1, 0, 7) * .4f, .12f)
            : profile.Contact + Math.Max(0L, (long)hits - 1) * profile.Interval + .045f;
        float recovery = profile?.Recovery ?? .18f;
        _approach = new Movement
        {
            Start = ApproachOffset, Travel = direction * reach,
            Windup = profile?.Windup ?? .035f, Contact = profile?.Contact ?? CombatImpactTime,
            Release = release, Recovery = recovery, Duration = release + recovery,
            Anticipation = profile is null ? -.035f : -.16f, LocalPause = profile is not null,
            LastClock = now, StartedFrame = frame, LastFrame = frame
        };
        if (profile is not null)
        {
            _attackDelay = Math.Max(0, profile.Contact - .12f); _attackDelayElapsed = 0;
            _attackFrame = frame; _attackVersion = MotionVersion; _attackPending = true;
        }
        // Source ShowcaseMotion calls update(0) synchronously before yielding.
        EvaluateApproach();
        return profile is not null || ranged && artId != "leader_blue" ? "block" : "attack";
    }

    /// <summary>Returns hurt. Showcase recoil starts at 35% on the contact frame.
    /// direction is the caller's signed layout-right vector, as in the source stage.</summary>
    public string Recoil(Vector3 direction, float height, bool heavy, float now, int frame, bool showcase = true)
    {
        RequireClock(now); RequireVector(direction, nameof(direction)); RequireNonnegative(height, nameof(height));
        RecoilVersion++;
        HasOwnedMotion = true;
        _recoil = new Movement
        {
            Travel = direction * height * (showcase ? heavy ? .15f : .085f : heavy ? .12f : .065f),
            Duration = heavy ? .29f : .22f, Anticipation = showcase ? .35f : 0,
            LocalPause = showcase, LastClock = now, StartedFrame = frame, LastFrame = frame
        };
        EvaluateRecoil();
        return "hurt";
    }

    /// <summary>Convenience owner-checked impact. The stage still validates actor identity,
    /// the receipt, and direct ownership before calling. A stale token changes nothing.</summary>
    public string? OwnedImpact(OwnershipToken token, Vector3 direction, float height, bool heavy,
        float seconds, float now, int frame)
    {
        if (!Owns(token)) return null;
        string pose = Recoil(direction, height, heavy, now, frame);
        RequestOwnedPause(token, seconds, now);
        return pose;
    }

    public void Cancel()
    {
        MotionVersion++; RecoilVersion++;
        _pause.Clear(); _approach = _recoil = null; _attackPending = false;
        ApproachOffset = RecoilOffset = Vector3.Zero;
        // HasOwnedMotion remains sticky so cancellation cannot reactivate legacy bob.
    }

    public string? Sample(float now, int frame, float frameDelta)
    {
        RequireClock(now);
        if (!float.IsFinite(frameDelta) || frameDelta < 0) throw new ArgumentOutOfRangeException(nameof(frameDelta));
        if (_approach is {} approach)
        {
            Advance(approach, now, frame, frameDelta); EvaluateApproach();
        }
        if (_recoil is {} recoil)
        {
            Advance(recoil, now, frame, frameDelta); EvaluateRecoil();
        }
        // Tween.After is a wall-presentation-time delay, not the actor-local paused clock.
        string? pose = null;
        if (_attackPending && _attackVersion == MotionVersion && frame > _attackFrame)
        {
            // Tween.After accumulates scaled frame delta; absolute float-clock
            // subtraction can add a frame at exact boundaries after long play.
            _attackFrame = frame; _attackDelayElapsed += frameDelta;
            if (_attackDelayElapsed >= _attackDelay) { _attackPending = false; pose = "attack"; }
        }
        // A repeated/same-frame sample must not discard overlap needed next frame.
        float consumedThrough = now;
        if (_approach is {} a) consumedThrough = Math.Min(consumedThrough, a.LastClock);
        if (_recoil is {} r) consumedThrough = Math.Min(consumedThrough, r.LastClock);
        _pause.DiscardHistoryThrough(consumedThrough);
        return pose;
    }

    // Retained BattleActor.cs:164 calls Sample(clock, delta, frame).
    public string? Sample(float now, float frameDelta, int frame) => Sample(now, frame, frameDelta);

    void Advance(Movement movement, float now, int frame, float frameDelta)
    {
        if (frame <= movement.StartedFrame || frame <= movement.LastFrame || now < movement.LastClock) return;
        // Unity StageCombat.ShowcaseMotion samples pause at frame end, then adds
        // the whole Time.deltaTime. Sprite LateUpdate uses the same pause predicate.
        // Fractional overlap integration made the recovered actor move while its
        // authored attack frame stayed held and changed motion after render stalls.
        // Acquisition/same-frame guards above keep a new coroutine at update(0).
        float elapsed = movement.LocalPause && _pause.IsPaused(now) ? 0f : frameDelta;
        movement.Elapsed = Math.Min(movement.Duration, movement.Elapsed + elapsed);
        movement.LastClock = now; movement.LastFrame = frame;
    }

    void EvaluateApproach()
    {
        if (_approach is not {} movement) return;
        if (movement.Elapsed >= movement.Duration) { ApproachOffset = Vector3.Zero; _approach = null; return; }
        float t = movement.Elapsed, amount;
        if (t < movement.Windup) amount = movement.Anticipation * Smooth(t / movement.Windup);
        else if (t < movement.Contact) amount = Lerp(movement.Anticipation, 1, Smooth((t - movement.Windup) / (movement.Contact - movement.Windup)));
        else if (t < movement.Release) amount = 1;
        else amount = 1 - Smooth((t - movement.Release) / movement.Recovery);
        ApproachOffset = movement.Start * (1 - t / movement.Duration) + movement.Travel * amount;
    }

    void EvaluateRecoil()
    {
        if (_recoil is not {} movement) return;
        if (movement.Elapsed >= movement.Duration) { RecoilOffset = Vector3.Zero; _recoil = null; return; }
        float k = movement.Elapsed / movement.Duration;
        float amount = k < .18f ? Lerp(movement.Anticipation, 1, Smooth(k / .18f)) : 1 - Smooth((k - .18f) / .82f);
        RecoilOffset = movement.Travel * amount;
    }

    static float Smooth(float value) { value = Math.Clamp(value, 0, 1); return value * value * (3 - 2 * value); }
    static float Lerp(float a, float b, float t) => a + (b - a) * t;
    static void RequireClock(float now) { if (!float.IsFinite(now) || now < 0) throw new ArgumentOutOfRangeException(nameof(now)); }
    static void RequireNonnegative(float value, string name) { if (!float.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(name); }
    static void RequireVector(Vector3 value, string name)
    {
        if (!float.IsFinite(value.X) || !float.IsFinite(value.Y) || !float.IsFinite(value.Z)) throw new ArgumentOutOfRangeException(name);
    }
    static void ValidateProfile(SourceShowcaseProfile? profile)
    {
        if (profile is null) return;
        RequireNonnegative(profile.Contact, nameof(profile.Contact)); RequireNonnegative(profile.Windup, nameof(profile.Windup));
        RequireNonnegative(profile.Interval, nameof(profile.Interval)); RequireNonnegative(profile.Reach, nameof(profile.Reach));
        if (profile.Contact <= profile.Windup || !float.IsFinite(profile.Recovery) || profile.Recovery <= 0)
            throw new ArgumentOutOfRangeException(nameof(profile));
    }
}

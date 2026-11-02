using System;
using System.Threading;

namespace DeckRogue.GodotPort.Presentation;

// Visual-only finite family prototypes for the exact 118 non-attack moves.
// No game state, event emission, queue/fence ownership, spawning or attack contacts.
public sealed class Style04EnemySupportMotion
{
    public const float MaxLifetimeSeconds = 2f;

    // InstanceId prevents a token from one actor from operating a new actor at the
    // same index. Serial distinguishes repeated Begin calls with reused metadata.
    public readonly record struct Token(long InstanceId, long Serial, int ExecutionIndex, int OwnerVersion)
    {
        public bool IsValid => InstanceId > 0 && Serial > 0 && ExecutionIndex >= 0 && OwnerVersion >= 0;
        public static Token Invalid => default;
    }

    public readonly record struct Snapshot(string ProfileId, string Kind, string MoveId, string BodyFamily,
        string Phase, float AngleDegrees, float ScaleX, float ScaleY,
        int ExecutionIndex, int OwnerVersion, long Version, long InstanceId,
        int AcceptedPulses, int LastEventIndex, float LastPulseAt,
        float StartedAt, float Deadline, float EndRequestedAt, float EndAt, bool Active)
    {
        public static Snapshot Identity => new("", "", "", "", "idle", 0f, 1f, 1f,
            -1, -1, 0, 0, 0, -1, -1f, -1f, -1f, -1f, -1f, false);
    }

    private readonly record struct Pose(float Angle, float X, float Y)
    {
        public static Pose Rest => new(0f, 1f, 1f);
        public static Pose Lerp(Pose a, Pose b, float t)
        {
            t = Math.Clamp(t, 0f, 1f); t = t * t * (3f - 2f * t);
            return new(a.Angle + (b.Angle - a.Angle) * t,
                a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
        }
    }
    private readonly record struct Body(string Family, float Angle, float X, float Y, float Tempo);
    private readonly record struct Envelope(Pose Prepare, Pose Pulse, float Load, float Hold, float PulseReturn, float Recovery);

    private static long _nextInstance;
    private readonly long _instanceId;
    private readonly string _profileId;
    private readonly Body _body;
    private Token _token;
    private long _serial;
    private bool _active;
    private string _kind = "", _moveId = "", _phase = "idle";
    private Pose _pose = Pose.Rest, _prepare, _pulse, _endPose;
    private float _load, _hold, _pulseReturn, _recovery;
    private float _started = -1f, _deadline = -1f, _endRequested = -1f, _endAt = -1f,
        _lastPulseAt = -1f, _lastClock = -1f;
    private int _accepted, _lastEventIndex = -1;

    public Snapshot State => new(_profileId, _kind, _moveId, _body.Family, _phase,
        _pose.Angle, _pose.X, _pose.Y, _token.IsValid ? _token.ExecutionIndex : -1,
        _token.IsValid ? _token.OwnerVersion : -1, _serial, _instanceId,
        _accepted, _lastEventIndex, _lastPulseAt, _started, _deadline, _endRequested, _endAt, _active);

    public Style04EnemySupportMotion(string enemyId)
    {
        _body = BodyFor(enemyId); // Rejects IDs without a real support definition.
        _profileId = enemyId;
        _instanceId = Interlocked.Increment(ref _nextInstance);
    }

    public Token Begin(string? kind, string? moveId, int executionIndex, int ownerVersion, float now, float deadline)
    {
        // A rejected declaration cannot erase a valid current owner. The caller
        // cancels its previous execution before choosing the legacy fallback.
        if (!Supports(_profileId, kind, moveId) || executionIndex < 0 || ownerVersion < 0 ||
            !ValidTime(now) || !float.IsFinite(deadline) || deadline <= now) return Token.Invalid;
        float boundedDeadline = Math.Min(deadline, now + MaxLifetimeSeconds);
        if (!float.IsFinite(boundedDeadline) || boundedDeadline <= now) return Token.Invalid;
        Envelope e = EnvelopeFor(kind!);
        _prepare = Shape(e.Prepare); _pulse = Shape(e.Pulse);
        _load = e.Load * _body.Tempo; _hold = e.Hold * _body.Tempo;
        _pulseReturn = e.PulseReturn * _body.Tempo; _recovery = e.Recovery * _body.Tempo;
        _serial++;
        _token = new(_instanceId, _serial, executionIndex, ownerVersion);
        _kind = kind!; _moveId = moveId!;
        _started = _lastClock = now; _deadline = _endAt = boundedDeadline;
        _endRequested = _lastPulseAt = -1f;
        _lastEventIndex = -1; _accepted = 0;
        _pose = Pose.Rest; _endPose = Pose.Rest;
        _active = true; _phase = "prepare";
        return _token;
    }

    public bool Pulse(Token token, int eventIndex, float now)
    {
        if (!Owns(token)) return false;
        if (!CheckClock(now) || _endRequested >= 0f) return false;
        // Stage verifies source/target and the current execution interval before
        // forwarding an effect. Strict event order deduplicates without a set.
        if (eventIndex <= _lastEventIndex || eventIndex <= token.ExecutionIndex) return false;
        _lastClock = now; _lastEventIndex = eventIndex; _lastPulseAt = now; _accepted++;
        _pose = Evaluate(now); _phase = PhaseAt(now);
        return true;
    }

    public bool End(Token token, float now)
    {
        if (!Owns(token)) return false;
        if (!CheckClock(now) || _endRequested >= 0f) return false;
        _lastClock = now; _endPose = Evaluate(now); _endRequested = now;
        _endAt = Math.Min(_deadline, now + _recovery);
        if (_endAt <= now) Finish();
        else { _pose = _endPose; _phase = "recovery"; }
        return true;
    }

    public Snapshot Sample(float now)
    {
        if (!_active) return State;
        if (!CheckClock(now)) return State;
        _lastClock = now;
        if (now >= _endAt) { Finish(); return State; }
        _pose = Evaluate(now); _phase = PhaseAt(now);
        return State;
    }

    public void Cancel()
    {
        _serial++;
        Finish(); _token = Token.Invalid;
        _kind = _moveId = "";
        _started = _deadline = _endRequested = _endAt = _lastPulseAt = _lastClock = -1f;
        _lastEventIndex = -1; _accepted = 0;
    }

    private bool Owns(Token token) => _active && token.IsValid && token.Equals(_token);
    private bool CheckClock(float now)
    {
        if (!ValidTime(now) || now < _started || now < _lastClock || now >= _deadline ||
            (_endRequested >= 0f && now >= _endAt))
        {
            Finish(); return false;
        }
        return true;
    }
    private Pose Shape(Pose p) => new(p.Angle * _body.Angle,
        1f + (p.X - 1f) * _body.X, 1f + (p.Y - 1f) * _body.Y);
    private Pose Evaluate(float now)
    {
        if (_endRequested >= 0f)
            return Pose.Lerp(_endPose, Pose.Rest, (now - _endRequested) / Math.Max(.000001f, _endAt - _endRequested));
        Pose p = Pose.Lerp(Pose.Rest, _prepare, (now - _started) / _load);
        if (_lastPulseAt >= 0f)
        {
            float age = now - _lastPulseAt;
            p = age <= _hold ? _pulse : Pose.Lerp(_pulse, _prepare, (age - _hold) / (_pulseReturn - _hold));
        }
        // Even a missing/blocked effect and missing Resolved callback gently return
        // before the hard deadline. A late pulse can never extend that deadline.
        float recoveryAt = Math.Max(_started, _deadline - _recovery);
        return now >= recoveryAt ? Pose.Lerp(p, Pose.Rest, (now - recoveryAt) / Math.Max(.000001f, _deadline - recoveryAt)) : p;
    }
    private string PhaseAt(float now)
    {
        if (_endRequested >= 0f) return "recovery";
        if (now >= Math.Max(_started, _deadline - _recovery)) return "deadline-recovery";
        if (_lastPulseAt >= 0f && now - _lastPulseAt < _pulseReturn)
            return now - _lastPulseAt <= _hold ? "pulse" : "pulse-return";
        return now - _started < _load ? "prepare" : "hold";
    }
    private void Finish() { _active = false; _phase = "idle"; _pose = Pose.Rest; }
    private static bool ValidTime(float value) => float.IsFinite(value) && value >= 0f;

    // Shared semantic families, modified only by the inspected body's material,
    // mass and silhouette. These are not 118 individually articulated animations.
    private static Envelope EnvelopeFor(string kind) => kind switch
    {
        "defend" => new(new(-.45f, 1.009f, .982f), new(-.7f, 1.014f, .972f), .12f, .032f, .16f, .14f),
        "buff" => new(new(-.35f, .997f, 1.012f), new(.55f, 1.008f, 1.025f), .15f, .038f, .19f, .16f),
        "hex" => new(new(.75f, .997f, 1.01f), new(-1.4f, 1.012f, .99f), .17f, .03f, .18f, .16f),
        "heal" => new(new(0f, 1.004f, .994f), new(.15f, 1.012f, 1.016f), .19f, .044f, .23f, .19f),
        "rally" => new(new(-1.2f, .998f, 1.009f), new(1.8f, 1.009f, .984f), .13f, .03f, .16f, .14f),
        "summon" => new(new(-.2f, 1.012f, .98f), new(.35f, 1.017f, 1.016f), .21f, .044f, .24f, .2f),
        "hatch" => new(new(-.4f, 1.003f, .997f), new(.65f, 1.004f, 1.004f), .18f, .035f, .2f, .16f),
        "destroy-token" => new(new(-.7f, 1.003f, .993f), new(1.4f, 1.009f, .985f), .15f, .025f, .16f, .14f),
        "steal-gold" => new(new(-.4f, 1.008f, .989f), new(1.2f, 1.014f, .988f), .1f, .025f, .14f, .13f),
        "flee" => new(new(-.5f, 1.003f, .994f), new(-1.2f, 1.006f, .99f), .1f, .024f, .13f, .12f),
        "rest" => new(new(.1f, 1.006f, .989f), new(.15f, 1.008f, .985f), .2f, .04f, .22f, .19f),
        "mill" => new(new(.3f, 1.008f, .989f), new(-.8f, .994f, 1.012f), .18f, .038f, .21f, .18f),
        "seal" => new(new(0f, 1.003f, .994f), new(0f, 1.012f, 1.012f), .16f, .035f, .19f, .16f),
        _ => throw new ArgumentException("No support semantic family.", nameof(kind))
    };

    public static bool Supports(string? enemyId, string? kind, string? moveId)
    {
        switch (enemyId)
        {
        case "enemy_wide_power":
            return (kind == "defend" && moveId == "coil");
        case "enemy_probe":
            return (kind == "defend" && moveId == "guard");
        case "enemy_set_wary":
            return (kind == "defend" && moveId == "guard");
        case "enemy_set_breaker":
            return (kind == "defend" && moveId == "ready_tools") ||
                (kind == "destroy-token" && moveId == "break_token");
        case "enemy_brute":
            return (kind == "buff" && moveId == "warcry") ||
                (kind == "defend" && moveId == "guard") ||
                (kind == "buff" && moveId == "war_roar");
        case "enemy_turtle":
            return (kind == "defend" && moveId == "shell");
        case "enemy_hexer":
            return (kind == "hex" && moveId == "curse") ||
                (kind == "defend" && moveId == "guard");
        case "enemy_wolf":
            return (kind == "buff" && moveId == "howl") ||
                (kind == "defend" && moveId == "guard");
        case "enemy_moss":
            return (kind == "defend" && moveId == "moss_shell");
        case "enemy_joker":
            return (kind == "defend" && moveId == "dodge_prance");
        case "enemy_drummer":
            return (kind == "rally" && moveId == "war_drum") ||
                (kind == "defend" && moveId == "guard");
        case "enemy_warden":
            return (kind == "defend" && moveId == "guard");
        case "enemy_thief":
            return (kind == "defend" && moveId == "sneak") ||
                (kind == "steal-gold" && moveId == "snatch") ||
                (kind == "flee" && moveId == "run_off");
        case "enemy_bomber":
            return (kind == "defend" && moveId == "shield_barrel");
        case "enemy_moss_healer":
            return (kind == "heal" && moveId == "mend") ||
                (kind == "defend" && moveId == "guard");
        case "enemy_axe_ogre":
            return (kind == "defend" && moveId == "windup") ||
                (kind == "rest" && moveId == "gasp");
        case "enemy_shell_guard":
            return (kind == "defend" && moveId == "harden");
        case "enemy_apprentice_colossus":
            return (kind == "defend" && moveId == "brace");
        case "enemy_cultist":
            return (kind == "defend" && moveId == "guard");
        case "enemy_slug":
            return (kind == "defend" && moveId == "guard");
        case "enemy_whetstone_colossus":
            return (kind == "defend" && moveId == "brace") ||
                (kind == "buff" && moveId == "hone");
        case "enemy_cinder_imp":
            return (kind == "hex" && moveId == "fan_flames") ||
                (kind == "defend" && moveId == "guard");
        case "enemy_rock_beetle":
            return (kind == "defend" && moveId == "harden");
        case "enemy_big_slime":
            return (kind == "defend" && moveId == "ooze_guard");
        case "enemy_moss_slime":
            return (kind == "defend" && moveId == "ooze");
        case "enemy_moss_spawner":
            return (kind == "summon" && moveId == "spawn") ||
                (kind == "defend" && moveId == "guard");
        case "enemy_elite_sergeant":
            return (kind == "defend" && moveId == "guard");
        case "enemy_elite_sentry":
            return (kind == "defend" && moveId == "guard");
        case "enemy_elite_iron_egg":
            return (kind == "defend" && moveId == "sleep") ||
                (kind == "buff" && moveId == "awaken");
        case "enemy_elite_slaver":
            return (kind == "hex" && moveId == "net") ||
                (kind == "defend" && moveId == "guard");
        case "enemy_elite_stab_book":
            return (kind == "defend" && moveId == "guard");
        case "enemy_elite_giant_face":
            return (kind == "buff" && moveId == "glare") ||
                (kind == "defend" && moveId == "guard");
        case "enemy_elite_gold_raven":
            return (kind == "defend" && moveId == "soar");
        case "enemy_elite_mirror_djinn":
            return (kind == "hex" && moveId == "gaze") ||
                (kind == "defend" && moveId == "guard");
        case "enemy_elite_doom_chanter":
            return (kind == "buff" && moveId == "crescendo") ||
                (kind == "defend" && moveId == "guard");
        case "enemy_elite_devourer":
            return (kind == "mill" && moveId == "devour");
        case "enemy_elite_owl":
            return (kind == "defend" && moveId == "brace");
        case "enemy_elite_deathless":
            return (kind == "defend" && moveId == "guard");
        case "enemy_shield_squire":
            return (kind == "defend" && moveId == "brace");
        case "enemy_crossbow_archer":
            return (kind == "defend" && moveId == "guard");
        case "enemy_bond_wolf":
            return (kind == "defend" && moveId == "prowl");
        case "enemy_mud_lump":
            return (kind == "defend" && moveId == "mud_guard");
        case "enemy_elite_husk_3":
            return (kind == "defend" && moveId == "guard");
        case "enemy_brood_toad":
            return (kind == "defend" && moveId == "squat");
        case "enemy_brood_raptor":
            return (kind == "defend" && moveId == "shelter");
        case "enemy_raptor_egg":
            return (kind == "defend" && moveId == "quiver") ||
                (kind == "hatch" && moveId == "hatch");
        case "enemy_mourn_beast":
            return (kind == "defend" && moveId == "lick");
        case "enemy_maw_hunter":
            return (kind == "hex" && moveId == "web") ||
                (kind == "defend" && moveId == "guard");
        case "enemy_sludge_berserker":
            return (kind == "hex" && moveId == "vomit") ||
                (kind == "defend" && moveId == "guard");
        case "enemy_mudling":
            return (kind == "hex" && moveId == "slop") ||
                (kind == "defend" && moveId == "guard");
        case "enemy_gaping_maw":
            return (kind == "hex" && moveId == "roar") ||
                (kind == "defend" && moveId == "guard");
        case "enemy_cog_construct":
            return (kind == "buff" && moveId == "charge_up") ||
                (kind == "defend" && moveId == "guard");
        case "enemy_spore_cap":
            return (kind == "hex" && moveId == "vuln_spores") ||
                (kind == "defend" && moveId == "guard");
        case "enemy_strangler_serpent":
            return (kind == "defend" && moveId == "guard");
        case "enemy_snap_fruit":
            return (kind == "defend" && moveId == "guard");
        case "enemy_vine_walker":
            return (kind == "defend" && moveId == "guard");
        case "enemy_iron_clam":
            return (kind == "buff" && moveId == "pressurize") ||
                (kind == "defend" && moveId == "guard");
        case "enemy_sludge_spider":
            return (kind == "defend" && moveId == "guard");
        case "enemy_axe_automaton":
            return (kind == "buff" && moveId == "sharpen") ||
                (kind == "defend" && moveId == "guard");
        case "enemy_axe_automaton_reboot":
            return (kind == "defend" && moveId == "boot_up");
        case "enemy_thunder_globe":
            return (kind == "defend" && moveId == "guard");
        case "enemy_frog_knight":
            return (kind == "buff" && moveId == "for_the_queen") ||
                (kind == "defend" && moveId == "guard");
        case "enemy_biting_scroll":
            return (kind == "buff" && moveId == "more_teeth") ||
                (kind == "defend" && moveId == "guard");
        case "enemy_lost_soul":
            return (kind == "defend" && moveId == "guard");
        case "enemy_forgotten_soul":
            return (kind == "defend" && moveId == "guard");
        case "enemy_devoted_sculptor":
            return (kind == "defend" && moveId == "guard");
        case "enemy_chomper":
            return (kind == "hex" && moveId == "screech") ||
                (kind == "defend" && moveId == "guard");
        case "enemy_scald_gnat":
            return (kind == "hex" && moveId == "scald_spit") ||
                (kind == "defend" && moveId == "guard");
        case "enemy_kin_priest":
            return (kind == "buff" && moveId == "ritual") ||
                (kind == "defend" && moveId == "guard");
        case "enemy_kin_follower":
            return (kind == "buff" && moveId == "dance") ||
                (kind == "defend" && moveId == "guard");
        case "enemy_crab_crusher":
            return (kind == "buff" && moveId == "adapt") ||
                (kind == "defend" && moveId == "guard");
        case "enemy_crab_cannon":
            return (kind == "buff" && moveId == "charge") ||
                (kind == "rest" && moveId == "recharge");
        case "enemy_chimera_3":
            return (kind == "hex" && moveId == "burning_growl");
        case "enemy_burrow_worm":
            return (kind == "defend" && moveId == "burrow_down");
        case "enemy_bowl_bug":
            return (kind == "defend" && moveId == "curl");
        case "enemy_haze_stag":
            return (kind == "defend" && moveId == "fade_in_mist");
        case "enemy_winch_warden":
            return (kind == "summon" && moveId == "release_orbs") ||
                (kind == "defend" && moveId == "wind_up") ||
                (kind == "rest" && moveId == "rewind");
        case "enemy_seal_orb":
            return (kind == "seal" && moveId == "seal") ||
                (kind == "defend" && moveId == "guard");
        case "enemy_ember_furnace":
            return (kind == "defend" && moveId == "stoke") ||
                (kind == "defend" && moveId == "inflame");
        default: return false;
        }
    }

    private static Body BodyFor(string enemyId) => enemyId switch
    {
        "enemy_probe" or "enemy_set_wary" or "enemy_joker" or "enemy_drummer" or "enemy_thief" or "enemy_cinder_imp" or "enemy_kin_follower" or "enemy_crossbow_archer" =>
            new("LightBiped", 1.0f, 0.8f, 0.9f, 0.8f),
        "enemy_brute" or "enemy_set_breaker" or "enemy_bomber" or "enemy_axe_ogre" or "enemy_elite_sergeant" or "enemy_elite_slaver" or "enemy_shield_squire" or "enemy_elite_deathless" or "enemy_elite_husk_3" or "enemy_devoted_sculptor" =>
            new("HeavyBiped", 0.65f, 0.5f, 0.6f, 1.2f),
        "enemy_apprentice_colossus" or "enemy_whetstone_colossus" or "enemy_thunder_globe" or "enemy_elite_giant_face" or "enemy_elite_sentry" =>
            new("Stone", 0.3f, 0.15f, 0.15f, 1.25f),
        "enemy_warden" or "enemy_cog_construct" or "enemy_axe_automaton" or "enemy_axe_automaton_reboot" or "enemy_winch_warden" or "enemy_ember_furnace" or "enemy_chomper" =>
            new("Metal", 0.35f, 0.18f, 0.18f, 1.15f),
        "enemy_hexer" or "enemy_cultist" or "enemy_kin_priest" or "enemy_elite_doom_chanter" =>
            new("TallRobe", 0.6f, 0.6f, 0.8f, 1.1f),
        "enemy_lost_soul" or "enemy_forgotten_soul" or "enemy_elite_mirror_djinn" =>
            new("FloatRobe", 0.45f, 0.55f, 0.95f, 1.15f),
        "enemy_moss_slime" =>
            new("TinyBeast", 0.75f, 0.9f, 0.9f, 0.7f),
        "enemy_wolf" or "enemy_bond_wolf" or "enemy_mourn_beast" or "enemy_slug" or "enemy_brood_raptor" or "enemy_haze_stag" =>
            new("LongBeast", 0.45f, 0.55f, 0.65f, 1.0f),
        "enemy_wide_power" or "enemy_big_slime" or "enemy_brood_toad" or "enemy_gaping_maw" or "enemy_chimera_3" =>
            new("HeavyBeast", 0.35f, 0.45f, 0.55f, 1.2f),
        "enemy_turtle" or "enemy_shell_guard" or "enemy_rock_beetle" or "enemy_iron_clam" or "enemy_elite_iron_egg" or "enemy_burrow_worm" or "enemy_bowl_bug" or "enemy_frog_knight" =>
            new("Shell", 0.3f, 0.2f, 0.2f, 1.2f),
        "enemy_maw_hunter" or "enemy_sludge_spider" or "enemy_scald_gnat" =>
            new("Arthropod", 0.4f, 0.3f, 0.4f, 0.9f),
        "enemy_moss" or "enemy_moss_spawner" or "enemy_vine_walker" or "enemy_moss_healer" =>
            new("Plant", 0.65f, 0.4f, 0.5f, 1.1f),
        "enemy_mudling" or "enemy_mud_lump" or "enemy_sludge_berserker" or "enemy_snap_fruit" =>
            new("SoftMud", 0.4f, 1.1f, 1.1f, 1.2f),
        "enemy_biting_scroll" or "enemy_elite_stab_book" =>
            new("Paper", 0.55f, 0.9f, 0.8f, 0.9f),
        "enemy_elite_gold_raven" or "enemy_elite_owl" =>
            new("Winged", 0.45f, 0.5f, 0.6f, 1.0f),
        "enemy_strangler_serpent" or "enemy_elite_devourer" =>
            new("Coil", 0.3f, 0.85f, 0.75f, 1.1f),
        "enemy_crab_crusher" or "enemy_crab_cannon" =>
            new("WallRigid", 0.15f, 0.15f, 0.15f, 1.2f),
        "enemy_raptor_egg" =>
            new("Egg", 0.25f, 0.08f, 0.08f, 1.1f),
        "enemy_spore_cap" =>
            new("Cap", 0.15f, 0.9f, 1.0f, 1.1f),
        "enemy_seal_orb" =>
            new("OrbMachine", 0.3f, 0.18f, 0.18f, 0.8f),
        _ => throw new ArgumentException("No exact non-attack support definition for this enemy.", nameof(enemyId))
    };

}

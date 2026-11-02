using System;

namespace DeckRogue.GodotPort.Presentation;

// A visual-only envelope for eighty-eight attack-capable static Style04 sprites. It owns no gameplay,
// timers, resources or completion fences. Begin is the action-lifetime boundary;
// the stage remains responsible for rejecting callbacks from old action owners.
public sealed class Style04EnemyMotion
{
	public readonly record struct Snapshot(string ProfileId, string MoveId, string Phase,
		float AngleDegrees, float ScaleX, float ScaleY, int ExecutionIndex, int LastOrdinal,
		int AcceptedContacts, int Version, int OwnerVersion, float ReleaseAt, float LastContactAt, bool Active)
	{
		public static Snapshot Identity => new("", "", "idle", 0f, 1f, 1f, -1, -1, 0, 0, 0, -1f, -1f, false);
	}

	private readonly record struct Pose(float Angle, float X, float Y)
	{
		public static Pose Rest => new(0f, 1f, 1f);
		public static Pose Lerp(Pose a, Pose b, float t)
		{
			t = Smooth(t);
			return new(a.Angle + (b.Angle - a.Angle) * t, a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
		}
	}

	private readonly string _profileId;
	private Pose _load, _strike, _recoil, _settle;
	private float _windup, _snap, _hold, _recoilEnd, _recovery;
	private Pose _pose = Pose.Rest;
	private bool _active, _ranged;
	private string _moveId = "", _phase = "idle";
	private int _execution = -1, _ordinal = -1, _count, _accepted, _version, _ownerVersion;
	private float _started, _expected, _releaseAt = -1f, _contactAt = -1f;

	public static bool Supports(string? enemyId) => enemyId is "enemy_brute" or "enemy_wolf" or "enemy_crossbow_archer"
		or "enemy_probe" or "enemy_turtle" or "enemy_drummer" or "enemy_spore_cap" or "enemy_hexer"
		or "enemy_iron_clam" or "enemy_mudling" or "enemy_set_wary" or "enemy_sludge_spider" or "enemy_slug"
		or "enemy_set_breaker" or "enemy_joker" or "enemy_thief" or "enemy_mimic_imp" or "enemy_mimic_jester"
		or "enemy_cultist" or "enemy_shield_squire" or "enemy_frog_knight" or "enemy_kin_follower" or "enemy_warden"
		or "enemy_axe_ogre" or "enemy_apprentice_colossus" or "enemy_whetstone_colossus" or "enemy_cog_construct" or "enemy_thunder_globe" or "enemy_sludge_berserker"
		or "enemy_moss_healer" or "enemy_bomber" or "enemy_devoted_sculptor" or "enemy_kin_priest" or "enemy_cinder_imp"
		or "enemy_wide_power" or "enemy_thorn_squirrel" or "enemy_bond_wolf" or "enemy_mourn_beast" or "enemy_gaping_maw"
		or "enemy_haze_stag" or "enemy_shell_guard" or "enemy_rock_beetle" or "enemy_maw_hunter" or "enemy_chomper"
		or "enemy_scald_gnat" or "enemy_bowl_bug" or "enemy_burrow_worm" or "enemy_moss" or "enemy_mud_lump"
		or "enemy_snap_fruit" or "enemy_vine_walker" or "enemy_strangler_serpent"
		or "enemy_big_slime" or "enemy_moss_slime" or "enemy_moss_spawner" or "enemy_brood_toad" or "enemy_broodling"
		or "enemy_brood_raptor" or "enemy_raptor_chick" or "enemy_axe_automaton" or "enemy_axe_automaton_reboot" or "enemy_elite_deathless"
		or "enemy_elite_husk_3" or "enemy_elite_husk_2" or "enemy_elite_husk_1" or "enemy_biting_scroll" or "enemy_lost_soul"
		or "enemy_forgotten_soul" or "enemy_elite_stab_book" or "enemy_elite_mirror_djinn" or "enemy_elite_doom_chanter" or "enemy_nemesis_wraith"
		or "enemy_elite_sergeant" or "enemy_elite_sentry" or "enemy_elite_iron_egg" or "enemy_elite_slaver" or "enemy_elite_giant_face"
		or "enemy_elite_gold_raven" or "enemy_elite_devourer" or "enemy_elite_owl" or "enemy_crab_crusher" or "enemy_crab_cannon"
		or "enemy_winch_warden" or "enemy_seal_orb" or "enemy_ember_furnace" or "enemy_chimera_1" or "enemy_chimera_2"
		or "enemy_chimera_3";
	public Snapshot State => new(_profileId, _moveId, _phase, _pose.Angle, _pose.X, _pose.Y,
		_execution, _ordinal, _accepted, _version, _ownerVersion, _releaseAt, _contactAt, _active);

	public Style04EnemyMotion(string enemyId)
	{
		_profileId = enemyId;
		switch (enemyId)
		{
		case "enemy_brute":
			_load = new(-4.5f, 1.015f, 0.965f);
			_strike = new(6.5f, 1.025f, 0.97f);
			_recoil = new(-1.3f, 0.995f, 1.015f);
			_settle = new(0.7f, 1.005f, 0.995f);
			(_windup, _snap, _hold, _recoilEnd, _recovery) = (0.20f, 0.065f, 0.025f, 0.105f, 0.30f);
			break;
		case "enemy_wolf":
			_load = new(-1.8f, 1.035f, 0.935f);
			_strike = new(3.8f, 1.05f, 0.985f);
			_recoil = new(-1f, 0.985f, 1.025f);
			_settle = new(0.35f, 1.006f, 0.995f);
			(_windup, _snap, _hold, _recoilEnd, _recovery) = (0.13f, 0.035f, 0.018f, 0.067f, 0.18f);
			break;
		case "enemy_crossbow_archer":
			_load = new(2f, 0.995f, 1.015f);
			_strike = new(-3.2f, 1.01f, 0.99f);
			_recoil = new(0.8f, 1f, 1.005f);
			_settle = new(0.2f, 1f, 1f);
			(_windup, _snap, _hold, _recoilEnd, _recovery) = (0.19f, 0.04f, 0.018f, 0.075f, 0.25f);
			break;
		case "enemy_probe":
		case "enemy_turtle":
		case "enemy_drummer":
		case "enemy_spore_cap":
		case "enemy_hexer":
		case "enemy_iron_clam":
		case "enemy_mudling":
		case "enemy_set_wary":
		case "enemy_sludge_spider":
		case "enemy_slug":
			SelectAttackProfile("");
			break;
		case "enemy_set_breaker":
		case "enemy_joker":
		case "enemy_thief":
		case "enemy_mimic_imp":
		case "enemy_mimic_jester":
		case "enemy_cultist":
		case "enemy_shield_squire":
		case "enemy_frog_knight":
		case "enemy_kin_follower":
		case "enemy_warden":
		case "enemy_axe_ogre":
		case "enemy_apprentice_colossus":
		case "enemy_whetstone_colossus":
		case "enemy_cog_construct":
		case "enemy_thunder_globe":
		case "enemy_sludge_berserker":
			SelectAttackProfile("");
			break;
		case "enemy_moss_healer":
		case "enemy_bomber":
		case "enemy_devoted_sculptor":
		case "enemy_kin_priest":
		case "enemy_cinder_imp":
		case "enemy_wide_power":
		case "enemy_thorn_squirrel":
		case "enemy_bond_wolf":
		case "enemy_mourn_beast":
		case "enemy_gaping_maw":
		case "enemy_haze_stag":
		case "enemy_shell_guard":
		case "enemy_rock_beetle":
		case "enemy_maw_hunter":
		case "enemy_chomper":
		case "enemy_scald_gnat":
		case "enemy_bowl_bug":
		case "enemy_burrow_worm":
		case "enemy_moss":
		case "enemy_mud_lump":
		case "enemy_snap_fruit":
		case "enemy_vine_walker":
		case "enemy_strangler_serpent":
			SelectAttackProfile("");
			break;
		case "enemy_big_slime":
		case "enemy_moss_slime":
		case "enemy_moss_spawner":
		case "enemy_brood_toad":
		case "enemy_broodling":
		case "enemy_brood_raptor":
		case "enemy_raptor_chick":
		case "enemy_axe_automaton":
		case "enemy_axe_automaton_reboot":
		case "enemy_elite_deathless":
		case "enemy_elite_husk_3":
		case "enemy_elite_husk_2":
		case "enemy_elite_husk_1":
		case "enemy_biting_scroll":
		case "enemy_lost_soul":
		case "enemy_forgotten_soul":
		case "enemy_elite_stab_book":
		case "enemy_elite_mirror_djinn":
		case "enemy_elite_doom_chanter":
		case "enemy_nemesis_wraith":
		case "enemy_elite_sergeant":
		case "enemy_elite_sentry":
		case "enemy_elite_iron_egg":
		case "enemy_elite_slaver":
		case "enemy_elite_giant_face":
		case "enemy_elite_gold_raven":
		case "enemy_elite_devourer":
		case "enemy_elite_owl":
		case "enemy_crab_crusher":
		case "enemy_crab_cannon":
		case "enemy_winch_warden":
		case "enemy_seal_orb":
		case "enemy_ember_furnace":
		case "enemy_chimera_1":
		case "enemy_chimera_2":
		case "enemy_chimera_3":
			SelectAttackProfile("");
			break;
		default:
			throw new ArgumentException("No Style04 procedural profile for this enemy.", nameof(enemyId));
		}
	}

	public void Begin(float now, float contactAt, int executionIndex, string? moveId, bool ranged = false, int ownerVersion = 0)
	{
		if (!ValidTime(now) || !float.IsFinite(contactAt) || executionIndex < 0) return;
		_version++;
		_ownerVersion = ownerVersion;
		_execution = executionIndex;
		_moveId = moveId ?? "";
		SelectAttackProfile(_moveId);
		_started = now;
		_expected = Math.Max(now, contactAt);
		_releaseAt = _contactAt = -1f;
		_ordinal = -1;
		_count = _accepted = 0;
		_ranged = ranged;
		_active = true;
		_phase = "windup";
		_pose = Pose.Rest;
	}

	public bool Preview(float now, float contactAt, int executionIndex, string? moveId, bool ranged = false, int ownerVersion = 0)
	{
		if (!Owns(now, executionIndex, moveId, ownerVersion) || !float.IsFinite(contactAt) || _accepted != 0) return false;
		_expected = Math.Max(now, contactAt);
		// Preview is the observed projectile release, never the target hit. Do not
		// restart recoil if a caller repeats the preview on the same execution.
		if (_ranged && ranged && _releaseAt < 0f)
		{
			_releaseAt = now;
			_phase = "release";
			_pose = _strike;
		}
		return true;
	}

	public bool Contact(float now, int executionIndex, string? moveId, int ordinal, int count, int ownerVersion = 0)
	{
		if (!Owns(now, executionIndex, moveId, ownerVersion) || count < 1 || count > 12 || ordinal != _ordinal + 1 || ordinal >= count ||
			(_count != 0 && _count != count) || (_contactAt >= 0f && now < _contactAt)) return false;
		_ordinal = ordinal;
		_count = count;
		_accepted++;
		_contactAt = now;
		_phase = _ranged && _releaseAt >= 0f ? "target-contact" : "contact";
		// A delayed ranged impact must not kick the shooter's body a second time.
		_pose = _ranged && _releaseAt >= 0f ? RecoveryPose(Math.Max(0f, now - _releaseAt)) : _strike;
		return true;
	}

	public Snapshot Sample(float now)
	{
		if (!_active || !ValidTime(now) || now < _started) return State;
		if (_ranged && _releaseAt >= 0f)
		{
			float age = Math.Max(0f, now - _releaseAt);
			_pose = RecoveryPose(age);
			_phase = age <= _hold ? "release" : (age < _recovery ? "release-recoil" : "flight");
			if (_contactAt >= 0f)
			{
				if (now - _contactAt <= _hold) _phase = "target-contact";
				else if (_ordinal == _count - 1 && age >= _recovery) Finish();
			}
		}
		else if (_contactAt >= 0f)
		{
			float age = Math.Max(0f, now - _contactAt);
			if (age <= _hold)
			{
				_phase = "contact";
				_pose = _strike;
			}
			else if (_ordinal < _count - 1)
			{
				// Settle back into a small loaded posture while waiting for the next
				// real callback, even when the presentation clock crosses planned hits.
				_phase = age < _recoilEnd ? "recoil" : "repeat-ready";
				_pose = age < _recoilEnd ? Pose.Lerp(_strike, _recoil, (age - _hold) / (_recoilEnd - _hold)) :
					Pose.Lerp(_recoil, Pose.Lerp(Pose.Rest, _load, 0.65f), (age - _recoilEnd) / 0.045f);
			}
			else if (age >= _recovery) Finish();
			else
			{
				_phase = age < _recoilEnd ? "recoil" : "recovery";
				_pose = RecoveryPose(age);
			}
		}
		else
		{
			float target = Math.Max(_started, _expected - (_ranged ? 0.22f : 0f));
			float available = Math.Max(0.001f, target - _started);
			float loadDuration = Math.Max(0.001f, Math.Min(_windup, available * 0.7f));
			_pose = Pose.Lerp(Pose.Rest, _load, (now - _started) / loadDuration);
			_phase = "windup";
			// Ranged actors stay aimed until the observed release. Melee may approach its
			// strike, but full contact strength is reserved for the actual callback.
			if (!_ranged && now > target - _snap)
			{
				_phase = "approach";
				_pose = Pose.Lerp(_pose, Pose.Lerp(_load, _strike, 0.72f), (now - target + _snap) / _snap);
			}
		}
		return State;
	}

	public void Cancel()
	{
		_version++;
		Finish();
		_execution = _ordinal = -1;
		_count = _accepted = 0;
		_releaseAt = _contactAt = -1f;
		_moveId = "";
	}

	// These immutable envelope definitions are initialized once. Selecting a move
	// copies value types only at Begin; Sample does no table lookup or allocation.
	// Ranged behavior is supplied by the existing stage, never inferred here.
	// Whole-image transforms are staticSprite prototypes, not drawn limb frames.
	private readonly record struct AttackProfile(Pose Load, Pose Strike, Pose Recoil, Pose Settle,
		float Windup, float Snap, float Hold, float RecoilEnd, float Recovery);

	// enemy_probe/poke: Short blade-first testing jab; little torso commitment.
	private static readonly AttackProfile ProbePoke = new(
		new(-1.4f, 1.0f, 0.993f), new(2.8f, 1.014f, 0.994f), new(-0.65f, 0.998f, 1.003f), new(0.15f, 1.0f, 1.0f),
		0.115f, 0.029f, 0.014f, 0.058f, 0.15f);

	// enemy_probe/lunge: Deeper backward load and committed thrust, longer recovery than poke.
	private static readonly AttackProfile ProbeLunge = new(
		new(-3.6f, 0.995f, 0.981f), new(5.5f, 1.024f, 0.987f), new(-1.5f, 0.992f, 1.009f), new(0.4f, 1.003f, 0.998f),
		0.21f, 0.043f, 0.024f, 0.095f, 0.27f);

	// enemy_turtle/crush: Restrained heavy shell brace and slow low drive; broad rigid shell stays rigid-looking.
	private static readonly AttackProfile TurtleCrush = new(
		new(-1.6f, 1.008f, 0.984f), new(3.2f, 1.012f, 0.976f), new(-0.6f, 0.997f, 1.003f), new(0.18f, 1.002f, 0.998f),
		0.245f, 0.071f, 0.035f, 0.145f, 0.39f);

	// enemy_turtle/awake_bite: Small quick neck-leading body pulse for each actual bite, without inventing jaw opening.
	private static readonly AttackProfile TurtleAwakeBite = new(
		new(-0.6f, 1.003f, 0.99f), new(1.45f, 1.013f, 0.991f), new(-0.3f, 0.998f, 1.002f), new(0.06f, 1.0f, 0.999f),
		0.12f, 0.027f, 0.015f, 0.061f, 0.22f);

	// enemy_drummer/thump: Raised beater preparation followed by a compact downward body beat; no rhythmic idle oscillation.
	private static readonly AttackProfile DrummerThump = new(
		new(-3.8f, 0.998f, 1.012f), new(4.4f, 1.01f, 0.982f), new(-1.1f, 0.998f, 1.007f), new(0.3f, 1.002f, 0.999f),
		0.145f, 0.033f, 0.019f, 0.079f, 0.205f);

	// enemy_spore_cap/frail_spores: Almost rotation-free compress/vent pulse; cap silhouette breathes once on observed release.
	private static readonly AttackProfile SporeFrail = new(
		new(0.0f, 1.026f, 0.96f), new(0.0f, 1.012f, 1.023f), new(0.0f, 0.993f, 1.009f), new(0.0f, 1.006f, 0.997f),
		0.205f, 0.042f, 0.024f, 0.11f, 0.32f);

	// enemy_spore_cap/smash: Directional cap-down body hit distinct from upward vent; conservative soft squash.
	private static readonly AttackProfile SporeSmash = new(
		new(-2.1f, 1.018f, 0.956f), new(3.4f, 1.035f, 0.961f), new(-0.65f, 0.99f, 1.018f), new(0.2f, 1.008f, 0.994f),
		0.175f, 0.043f, 0.025f, 0.101f, 0.28f);

	// enemy_hexer/mud: Upright aim with raised casting arm, then rearward release recoil; target arrival never fires a second body kick.
	private static readonly AttackProfile HexerMud = new(
		new(2.2f, 0.996f, 1.009f), new(-2.5f, 1.008f, 0.994f), new(0.75f, 0.999f, 1.004f), new(0.12f, 1.0f, 1.0f),
		0.215f, 0.039f, 0.02f, 0.087f, 0.285f);

	// enemy_hexer/slap: Forward torso-led slap with quick contact and guarded return; opposite release direction from mud.
	private static readonly AttackProfile HexerSlap = new(
		new(-2.9f, 0.996f, 1.004f), new(5.7f, 1.018f, 0.981f), new(-1.2f, 0.995f, 1.008f), new(0.35f, 1.003f, 0.998f),
		0.165f, 0.031f, 0.019f, 0.082f, 0.235f);

	// enemy_iron_clam/jet: Rigid shell pressure brace, tiny rearward emission recoil and heavy damped return; no shell squash gag.
	private static readonly AttackProfile ClamJet = new(
		new(0.45f, 1.002f, 0.994f), new(-1.35f, 0.997f, 1.003f), new(0.28f, 1.001f, 0.998f), new(0.04f, 1.0f, 1.0f),
		0.235f, 0.027f, 0.027f, 0.12f, 0.34f);

	// enemy_mudling/bump: Low soft mound compresses then bumps; broad compression and slow material settle rather than weapon swing.
	private static readonly AttackProfile MudlingBump = new(
		new(-0.65f, 1.035f, 0.95f), new(2.1f, 1.04f, 0.974f), new(-0.4f, 0.99f, 1.023f), new(0.1f, 1.01f, 0.993f),
		0.12f, 0.035f, 0.021f, 0.105f, 0.275f);

	// enemy_set_wary/slash: Long cautious backward load, very short cutting snap, strong retreat into a guarded silhouette.
	private static readonly AttackProfile WarySlash = new(
		new(-3.2f, 0.997f, 0.988f), new(4.9f, 1.016f, 0.991f), new(-2.1f, 0.993f, 1.005f), new(-0.3f, 0.999f, 1.001f),
		0.205f, 0.025f, 0.012f, 0.058f, 0.21f);

	// enemy_sludge_spider/oil_spray: Low abdomen pressure brace, rearward spray recoil and quick reset; limbs are not independently animated.
	private static readonly AttackProfile SpiderOil = new(
		new(0.45f, 1.008f, 0.98f), new(-1.7f, 1.009f, 0.99f), new(0.35f, 0.997f, 1.005f), new(0.05f, 1.002f, 0.999f),
		0.16f, 0.028f, 0.017f, 0.065f, 0.19f);

	// enemy_sludge_spider/slam: Rapid flattened snap with small angular travel and a fast low-body recovery.
	private static readonly AttackProfile SpiderSlam = new(
		new(-1.1f, 1.024f, 0.966f), new(3.1f, 1.03f, 0.963f), new(-0.55f, 0.991f, 1.012f), new(0.1f, 1.004f, 0.996f),
		0.115f, 0.022f, 0.014f, 0.061f, 0.18f);

	// enemy_sludge_spider/rage: Tighter, faster aggressive body jab; attack medium stays unspecified, no invented fang or slash frames.
	private static readonly AttackProfile SpiderRage = new(
		new(-1.7f, 1.018f, 0.971f), new(2.3f, 1.022f, 0.981f), new(-1.0f, 0.991f, 1.009f), new(-0.15f, 1.002f, 0.998f),
		0.08f, 0.019f, 0.012f, 0.051f, 0.16f);

	// enemy_slug/lick: Long reptile keeps a low body line; small head-leading reach stands in for an undrawn tongue.
	private static readonly AttackProfile SlugLick = new(
		new(-0.55f, 1.004f, 0.989f), new(1.25f, 1.018f, 0.993f), new(-0.28f, 0.997f, 1.004f), new(0.05f, 1.001f, 0.999f),
		0.135f, 0.03f, 0.018f, 0.075f, 0.23f);

	// enemy_slug/acid_spit: Throat/forebody brace implied by restrained load, rearward spit release recoil, then low settle.
	private static readonly AttackProfile SlugAcidSpit = new(
		new(0.7f, 1.008f, 0.982f), new(-1.25f, 0.997f, 1.009f), new(0.25f, 1.002f, 0.999f), new(0.04f, 1.001f, 1.0f),
		0.205f, 0.033f, 0.023f, 0.097f, 0.29f);

	// enemy_slug/tackle: Low whole-body drive with a longer committed strike/recovery than lick; tail and legs remain one image.
	private static readonly AttackProfile SlugTackle = new(
		new(-1.5f, 1.017f, 0.965f), new(3.5f, 1.03f, 0.977f), new(-0.65f, 0.994f, 1.013f), new(0.18f, 1.006f, 0.995f),
		0.205f, 0.05f, 0.029f, 0.115f, 0.32f);

	// Added attack-only static-sprite envelopes. Existing thirteen profiles stay exact.

	// enemy_set_breaker/smash: Broad pick-bearing armored body loads back, then drives a weighted smash; armor scale remains tight.
	private static readonly AttackProfile BreakerSmash = new(
		new(-3.7f, 1.002f, 0.983f), new(4.8f, 1.014f, 0.977f), new(-1.15f, 0.997f, 1.006f), new(0.28f, 1.002f, 0.998f),
		0.215f, 0.054f, 0.029f, 0.119f, 0.335f);

	// enemy_set_breaker/toss_junk: Rearward junk-release recoil contrasts with the forward tool impacts; current throw route owns flight.
	private static readonly AttackProfile BreakerToss = new(
		new(2.6f, 0.996f, 1.007f), new(-2.85f, 1.008f, 0.994f), new(0.62f, 0.999f, 1.003f), new(0.09f, 1.0f, 1.0f),
		0.18f, 0.032f, 0.018f, 0.082f, 0.255f);

	// enemy_set_breaker/break_trap: Compact downward tool-side commitment for a reactive trap-breaking contact; destruction is not authored here.
	private static readonly AttackProfile BreakerTrap = new(
		new(-2.3f, 1.001f, 0.99f), new(3.65f, 1.009f, 0.985f), new(-1.05f, 0.998f, 1.004f), new(0.16f, 1.001f, 0.999f),
		0.155f, 0.028f, 0.017f, 0.074f, 0.225f);

	// enemy_joker/wild_swing: Thin open-armed jester makes a broad off-balance sweep with restrained cloth stretch and a visible counterlean.
	private static readonly AttackProfile JokerWild = new(
		new(-4.9f, 0.997f, 1.012f), new(6.1f, 1.018f, 0.981f), new(-1.8f, 0.991f, 1.011f), new(0.5f, 1.004f, 0.998f),
		0.16f, 0.027f, 0.016f, 0.083f, 0.235f);

	// enemy_joker/cautious_jab: Small guarded jab returns quickly into a backward counterlean; distinct from the wild open sweep.
	private static readonly AttackProfile JokerJab = new(
		new(-1.85f, 0.998f, 0.995f), new(2.95f, 1.012f, 0.995f), new(-1.5f, 0.994f, 1.004f), new(-0.16f, 0.999f, 1.001f),
		0.19f, 0.021f, 0.012f, 0.055f, 0.185f);

	// enemy_joker/call_bluff: Brief torso-led forward slap implied by the flavor, with a fast stop; no trap-breaking effect is invented.
	private static readonly AttackProfile JokerBluff = new(
		new(-2.65f, 0.999f, 1.005f), new(4.15f, 1.016f, 0.987f), new(-0.9f, 0.996f, 1.006f), new(0.23f, 1.002f, 0.999f),
		0.105f, 0.025f, 0.016f, 0.069f, 0.19f);

	// enemy_thief/mug: Low sack-backed crouch makes a fast low mugging drive and guarded retreat, without bouncing the entire sack.
	private static readonly AttackProfile ThiefMug = new(
		new(-1.25f, 1.016f, 0.971f), new(2.65f, 1.026f, 0.986f), new(-0.85f, 0.993f, 1.009f), new(-0.12f, 1.002f, 0.999f),
		0.09f, 0.021f, 0.012f, 0.054f, 0.165f);

	// enemy_mimic_imp/mimic: Small light imp makes tight claw-side body flicks on real mirror contacts; tail and fingers stay a single image.
	private static readonly AttackProfile ImpMimic = new(
		new(-2.8f, 1.013f, 0.979f), new(5.35f, 1.027f, 0.985f), new(-1.65f, 0.99f, 1.014f), new(0.32f, 1.004f, 0.997f),
		0.085f, 0.019f, 0.011f, 0.049f, 0.145f);

	// enemy_mimic_jester/mirror_dance: Long-bladed thin jester uses a measured counterlean and narrow cutting gesture for each observed mirrored hit.
	private static readonly AttackProfile JesterMirror = new(
		new(-3.9f, 0.997f, 1.009f), new(4.65f, 1.019f, 0.989f), new(-1.9f, 0.993f, 1.008f), new(0.28f, 1.002f, 0.999f),
		0.12f, 0.023f, 0.014f, 0.064f, 0.19f);

	// enemy_cultist/dark_blade: Upright hood and hooked blade lean into a deliberate cut with a guarded return; no dark VFX or prayer loop is added.
	private static readonly AttackProfile CultistBlade = new(
		new(-3.15f, 0.998f, 1.008f), new(4.75f, 1.012f, 0.986f), new(-1.4f, 0.995f, 1.006f), new(0.21f, 1.002f, 0.999f),
		0.195f, 0.028f, 0.018f, 0.079f, 0.255f);

	// enemy_shield_squire/shield_bash: Tall rigid front shield drives a short braced bash, keeping plate proportions almost unchanged.
	private static readonly AttackProfile SquireBash = new(
		new(-1.5f, 1.003f, 0.99f), new(2.85f, 1.011f, 0.984f), new(-0.6f, 0.998f, 1.004f), new(0.12f, 1.001f, 0.999f),
		0.155f, 0.032f, 0.023f, 0.09f, 0.25f);

	// enemy_shield_squire/avenging_rush: Deeper crouch and stronger shield-first commitment distinguish the rush; stage remains the only travel owner.
	private static readonly AttackProfile SquireRush = new(
		new(-2.8f, 1.008f, 0.977f), new(4.35f, 1.021f, 0.98f), new(-1.2f, 0.994f, 1.009f), new(0.24f, 1.003f, 0.998f),
		0.18f, 0.038f, 0.026f, 0.105f, 0.295f);

	// enemy_frog_knight/tongue_lash: Low armored frog gives a quick head-leading reach; no tongue extension is claimed from the static spear-and-shield art.
	private static readonly AttackProfile FrogTongue = new(
		new(-0.55f, 1.003f, 0.994f), new(1.45f, 1.015f, 0.995f), new(-0.35f, 0.998f, 1.003f), new(0.07f, 1.001f, 0.999f),
		0.115f, 0.024f, 0.017f, 0.067f, 0.21f);

	// enemy_frog_knight/strike_down: Wide planted frog silhouette braces tall then drives downward, with rigid armor and restrained settling.
	private static readonly AttackProfile FrogDown = new(
		new(-2.6f, 0.998f, 1.009f), new(4.05f, 1.009f, 0.98f), new(-0.9f, 0.997f, 1.006f), new(0.21f, 1.002f, 0.998f),
		0.195f, 0.041f, 0.026f, 0.105f, 0.285f);

	// enemy_frog_knight/beetle_charge: Long low forward charge posture commits the whole armored body; the move name does not create a beetle mount.
	private static readonly AttackProfile FrogCharge = new(
		new(-1.9f, 1.012f, 0.969f), new(3.85f, 1.027f, 0.978f), new(-1.05f, 0.993f, 1.01f), new(0.3f, 1.004f, 0.997f),
		0.235f, 0.051f, 0.032f, 0.13f, 0.36f);

	// enemy_kin_follower/slash: Light long-limbed dancer counterleans around the curved blade, then sweeps with a quicker recovery than armored cutters.
	private static readonly AttackProfile KinSlash = new(
		new(-4.4f, 0.996f, 1.013f), new(5.65f, 1.014f, 0.985f), new(-2.05f, 0.993f, 1.009f), new(0.43f, 1.004f, 0.998f),
		0.13f, 0.023f, 0.014f, 0.067f, 0.205f);

	// enemy_kin_follower/boomerang: Blade-side throwing preparation ends in one release recoil; both real boomerang impacts stay target events, never a second throw.
	private static readonly AttackProfile KinBoomerang = new(
		new(3.5f, 0.995f, 1.012f), new(-3.65f, 1.009f, 0.992f), new(0.95f, 0.998f, 1.005f), new(0.17f, 1.001f, 0.999f),
		0.17f, 0.03f, 0.017f, 0.078f, 0.245f);

	// enemy_warden/bell_strike: Massive bell-and-plate silhouette braces and recoils minutely at the observed beam release; no swinging bell or sonic art is implied.
	private static readonly AttackProfile WardenBell = new(
		new(0.85f, 1.002f, 0.995f), new(-1.45f, 0.999f, 1.003f), new(0.31f, 1.001f, 0.999f), new(0.04f, 1.0f, 1.0f),
		0.245f, 0.043f, 0.03f, 0.14f, 0.39f);

	// enemy_warden/gate_guard: Heavy hammer-and-shield body makes a compact forward guarding contact with long damped recovery.
	private static readonly AttackProfile WardenGate = new(
		new(-1.2f, 1.003f, 0.991f), new(2.25f, 1.008f, 0.985f), new(-0.43f, 0.999f, 1.003f), new(0.09f, 1.001f, 0.999f),
		0.215f, 0.052f, 0.034f, 0.143f, 0.385f);

	// enemy_warden/bell_hushed: Quieter longer brace and smaller release recoil distinguish the hushed bell while preserving the existing ranged fallback.
	private static readonly AttackProfile WardenHushed = new(
		new(0.3f, 1.001f, 0.997f), new(-0.68f, 0.999f, 1.002f), new(0.14f, 1.0f, 0.999f), new(0.02f, 1.0f, 1.0f),
		0.285f, 0.048f, 0.025f, 0.126f, 0.36f);

	// enemy_warden/bell_frenzy: Faster stronger but still rigid release recoil signals frenzy; its two observed targets do not double-fire the body.
	private static readonly AttackProfile WardenFrenzy = new(
		new(1.2f, 1.003f, 0.993f), new(-1.95f, 0.998f, 1.005f), new(0.48f, 1.001f, 0.998f), new(0.08f, 1.0f, 1.0f),
		0.135f, 0.026f, 0.017f, 0.084f, 0.265f);

	// enemy_axe_ogre/great_swing: Raised two-handed axe already has overhead weight; a deep load, committed downward follow-through and slow recovery imply that mass.
	private static readonly AttackProfile AxeGreatSwing = new(
		new(-5.3f, 1.004f, 0.973f), new(6.4f, 1.023f, 0.962f), new(-1.65f, 0.991f, 1.016f), new(0.4f, 1.005f, 0.996f),
		0.275f, 0.063f, 0.039f, 0.158f, 0.43f);

	// enemy_apprentice_colossus/swing: Broad blocky stone fists swing with limited angle and almost rigid dimensions, then settle heavily into square stance.
	private static readonly AttackProfile ApprenticeSwing = new(
		new(-2.1f, 1.002f, 0.988f), new(3.45f, 1.01f, 0.984f), new(-0.7f, 0.999f, 1.004f), new(0.16f, 1.001f, 0.999f),
		0.225f, 0.057f, 0.032f, 0.132f, 0.365f);

	// enemy_whetstone_colossus/guillotine: Tall slab-backed blade bearer rises slightly before a narrow downward cut; long held load and slow rigid return avoid rubbery stone.
	private static readonly AttackProfile WhetstoneGuillotine = new(
		new(-2.75f, 0.999f, 1.009f), new(4.1f, 1.008f, 0.982f), new(-0.82f, 0.998f, 1.005f), new(0.19f, 1.001f, 0.998f),
		0.28f, 0.049f, 0.038f, 0.145f, 0.405f);

	// enemy_cog_construct/repeater: Rigid box and left-facing barrel make a tiny mechanical rearward kick; current non-ranged route keeps it at actual contact.
	private static readonly AttackProfile CogRepeater = new(
		new(0.5f, 1.002f, 0.996f), new(-1.1f, 0.998f, 1.002f), new(0.2f, 1.0f, 0.999f), new(0.025f, 1.0f, 1.0f),
		0.155f, 0.018f, 0.014f, 0.056f, 0.175f);

	// enemy_cog_construct/expel: Heavier compact chassis kick differentiates expel; each real two-hit callback is honored without inventing projectile releases or cog rotation.
	private static readonly AttackProfile CogExpel = new(
		new(0.8f, 1.003f, 0.994f), new(-1.55f, 0.997f, 1.004f), new(0.32f, 1.001f, 0.998f), new(0.045f, 1.0f, 1.0f),
		0.125f, 0.021f, 0.016f, 0.064f, 0.195f);

	// enemy_thunder_globe/shock_slap: Large stone globe and detached hands lean into a compact palm-side contact; whole-image rotation does not articulate the hand.
	private static readonly AttackProfile GlobeSlap = new(
		new(-1.9f, 1.002f, 0.991f), new(3.15f, 1.011f, 0.983f), new(-0.65f, 0.997f, 1.006f), new(0.16f, 1.001f, 0.999f),
		0.16f, 0.031f, 0.023f, 0.098f, 0.285f);

	// enemy_thunder_globe/thunder_strike: Nearly rigid axial charge/release pulse keeps the spherical body stable; all three lightning target hits share one observed release.
	private static readonly AttackProfile GlobeThunder = new(
		new(0.0f, 1.004f, 0.992f), new(0.0f, 1.009f, 1.009f), new(0.0f, 0.998f, 1.003f), new(0.0f, 1.001f, 0.999f),
		0.215f, 0.026f, 0.019f, 0.087f, 0.27f);

	// enemy_thunder_globe/galvanic_burst: Longer tighter charge and larger axial release differentiate the single galvanic burst without changing electrical art or buff logic.
	private static readonly AttackProfile GlobeBurst = new(
		new(0.65f, 1.006f, 0.986f), new(-1.05f, 1.014f, 1.014f), new(0.22f, 0.996f, 1.005f), new(0.04f, 1.002f, 0.998f),
		0.255f, 0.038f, 0.03f, 0.126f, 0.355f);

	// enemy_sludge_berserker/pummel: Wide lumpy mud fists drive four brisk contact pulses, with restrained soft compression and a short reset between actual hits.
	private static readonly AttackProfile SludgePummel = new(
		new(-2.3f, 1.021f, 0.965f), new(3.7f, 1.032f, 0.975f), new(-1.1f, 0.99f, 1.016f), new(0.22f, 1.005f, 0.995f),
		0.105f, 0.025f, 0.015f, 0.064f, 0.19f);

	// enemy_sludge_berserker/hug: Broad soft torso slowly compresses into a close pressure contact; static arms cannot actually close around the target.
	private static readonly AttackProfile SludgeHug = new(
		new(-0.9f, 1.027f, 0.973f), new(1.75f, 1.038f, 0.968f), new(-0.35f, 0.993f, 1.019f), new(0.08f, 1.007f, 0.994f),
		0.19f, 0.052f, 0.039f, 0.142f, 0.35f);

	// enemy_sludge_berserker/smother: Largest low mud-mass compression and long heavy recovery distinguish smother from quick pummels; growth never rescales gameplay strength.
	private static readonly AttackProfile SludgeSmother = new(
		new(-1.6f, 1.032f, 0.948f), new(2.95f, 1.046f, 0.95f), new(-0.6f, 0.987f, 1.024f), new(0.14f, 1.011f, 0.992f),
		0.255f, 0.066f, 0.044f, 0.165f, 0.425f);

	// Families04 attack-only static-sprite envelopes. Existing twenty-nine profiles stay exact.

	// enemy_moss_healer/staff_poke: Narrow twig-bodied healer inclines the tall staff into a modest testing poke; almost rigid wood and a measured return keep it distinct from a blade thrust.
	private static readonly AttackProfile HealerPoke = new(
		new(-1.8f, 0.999f, 1.006f), new(2.45f, 1.009f, 0.993f), new(-0.72f, 0.997f, 1.004f), new(0.11f, 1.001f, 0.999f),
		0.175f, 0.028f, 0.016f, 0.077f, 0.24f);

	// enemy_bomber/toss_ember: Barrel-carrying kobold braces under its large round load, then gives a small rearward throw-release kick; the barrel stays rigid.
	private static readonly AttackProfile BomberEmber = new(
		new(2.1f, 0.999f, 1.006f), new(-2.6f, 1.007f, 0.995f), new(0.58f, 0.999f, 1.003f), new(0.13f, 1.001f, 1.0f),
		0.16f, 0.027f, 0.018f, 0.085f, 0.255f);

	// enemy_bomber/big_boom: Long braced crouch and heavier rearward blast-release recoil contrast with the light ember toss; no body disappearance, barrel explosion or self-damage is invented.
	private static readonly AttackProfile BomberBoom = new(
		new(0.9f, 1.004f, 0.988f), new(-4.1f, 1.014f, 0.978f), new(0.9f, 0.996f, 1.009f), new(0.22f, 1.002f, 0.998f),
		0.29f, 0.047f, 0.031f, 0.135f, 0.39f);

	// enemy_devoted_sculptor/carve_rite: Broad kneeling craftsman loads the raised hammer/chisel side and makes a controlled downward carving commitment; self-inscription and arm growth require actual frames.
	private static readonly AttackProfile SculptorCarve = new(
		new(-2.45f, 0.999f, 1.008f), new(3.65f, 1.009f, 0.989f), new(-0.82f, 0.996f, 1.005f), new(0.14f, 1.001f, 0.999f),
		0.205f, 0.031f, 0.022f, 0.088f, 0.265f);

	// enemy_devoted_sculptor/savage: Stronger shoulder-led sweep and quicker intent than the measured carving action, without stretching the hard tools or inventing an element.
	private static readonly AttackProfile SculptorSavage = new(
		new(-4.05f, 1.005f, 0.981f), new(5.4f, 1.02f, 0.974f), new(-1.35f, 0.992f, 1.012f), new(0.31f, 1.003f, 0.997f),
		0.145f, 0.027f, 0.018f, 0.079f, 0.245f);

	// enemy_kin_priest/orb_frail: Tall crowned priest gathers into a restrained raised-hand incline then counterleans once; current spell route applies this at real contact, not an invented projectile release.
	private static readonly AttackProfile PriestFrail = new(
		new(0.6f, 0.998f, 1.006f), new(-1.15f, 1.004f, 0.997f), new(0.28f, 0.999f, 1.002f), new(0.045f, 1.0f, 1.0f),
		0.235f, 0.035f, 0.021f, 0.096f, 0.295f);

	// enemy_kin_priest/orb_weak: Lower axial gathering and a small upward-facing cast accent distinguish weak-orb from frail-orb while the long robe and staff retain their shape.
	private static readonly AttackProfile PriestWeak = new(
		new(-0.45f, 1.001f, 0.996f), new(0.95f, 1.005f, 1.003f), new(-0.24f, 0.999f, 0.999f), new(0.035f, 1.0f, 1.0f),
		0.27f, 0.04f, 0.024f, 0.108f, 0.325f);

	// enemy_kin_priest/beam: Braced upright beam release with a narrow rearward recoil; three target contacts belong to one observed release, not three body shots.
	private static readonly AttackProfile PriestBeam = new(
		new(0.8f, 1.002f, 0.994f), new(-1.65f, 0.998f, 1.006f), new(0.38f, 1.001f, 0.998f), new(0.06f, 1.0f, 1.0f),
		0.22f, 0.027f, 0.02f, 0.086f, 0.275f);

	// enemy_kin_priest/judgment_orb: Longer lower gathering and firmer axial cast accent communicate judgment without rescaling the sprite based on damage or inventing blood effects.
	private static readonly AttackProfile PriestJudgment = new(
		new(-1.1f, 1.004f, 0.989f), new(1.85f, 1.008f, 1.007f), new(-0.46f, 0.997f, 1.003f), new(0.09f, 1.001f, 0.999f),
		0.295f, 0.046f, 0.03f, 0.131f, 0.37f);

	// enemy_kin_priest/blood_beam: Deeper measured brace and stronger release counterlean differentiate the blood beam; target callbacks never replay the body recoil.
	private static readonly AttackProfile PriestBloodBeam = new(
		new(1.35f, 1.004f, 0.989f), new(-2.35f, 0.997f, 1.01f), new(0.62f, 1.002f, 0.997f), new(0.12f, 1.001f, 1.0f),
		0.245f, 0.032f, 0.026f, 0.106f, 0.32f);

	// enemy_cinder_imp/spark_toss: Small wiry fire imp tilts its raised throwing side then flicks backward on observed release; fast light recovery, no wing or tail flapping.
	private static readonly AttackProfile CinderSpark = new(
		new(3.1f, 0.996f, 1.012f), new(-3.8f, 1.013f, 0.991f), new(0.95f, 0.998f, 1.005f), new(0.16f, 1.002f, 0.999f),
		0.105f, 0.021f, 0.013f, 0.059f, 0.18f);

	// enemy_cinder_imp/bite: Low head-leading bite dart uses a forward compressed body pulse opposite to the throw recoil; the static jaw cannot close.
	private static readonly AttackProfile CinderBite = new(
		new(-2.3f, 1.014f, 0.978f), new(4.6f, 1.027f, 0.986f), new(-1.4f, 0.991f, 1.013f), new(0.26f, 1.004f, 0.996f),
		0.09f, 0.02f, 0.012f, 0.055f, 0.17f);

	// enemy_wide_power/surge: Broad sail-backed squat reptile pressure-braces low then releases rearward, honoring the current beam route; tall dorsal fin and curled tail are not individually deformed.
	private static readonly AttackProfile WideSurge = new(
		new(0.2f, 1.008f, 0.982f), new(-1.55f, 1.012f, 1.008f), new(0.29f, 0.997f, 1.003f), new(0.055f, 1.002f, 0.999f),
		0.24f, 0.041f, 0.029f, 0.125f, 0.355f);

	// enemy_thorn_squirrel/gnaw: Tiny curled squirrel makes a short snout-leading gnaw pulse; the large spiny tail stays near rigid and does not wag with every hit.
	private static readonly AttackProfile SquirrelGnaw = new(
		new(-0.7f, 1.008f, 0.987f), new(1.65f, 1.018f, 0.992f), new(-0.4f, 0.995f, 1.006f), new(0.065f, 1.002f, 0.999f),
		0.085f, 0.019f, 0.011f, 0.048f, 0.145f);

	// enemy_bond_wolf/twin_bite: Two-headed wolf makes compact forebody bite pulses on each actual hit; no alternating head or tail articulation is falsely implied by rotating the whole sprite.
	private static readonly AttackProfile BondTwin = new(
		new(-1.15f, 1.019f, 0.969f), new(2.75f, 1.034f, 0.988f), new(-0.7f, 0.99f, 1.014f), new(0.14f, 1.003f, 0.998f),
		0.11f, 0.024f, 0.015f, 0.064f, 0.19f);

	// enemy_bond_wolf/lunge: Long two-headed body lowers over its haunches then stretches into a committed lunge; broader compression and slower return contrast with twin bites, while Stage owns displacement.
	private static readonly AttackProfile BondLunge = new(
		new(-2.4f, 1.031f, 0.949f), new(4.25f, 1.045f, 0.977f), new(-1.05f, 0.987f, 1.02f), new(0.26f, 1.006f, 0.996f),
		0.18f, 0.041f, 0.024f, 0.098f, 0.285f);

	// enemy_mourn_beast/bite: Gaunt long-legged beast makes tight neck-leading contacts, with a restrained guarded retreat suited to its narrow ribs and dangling mane.
	private static readonly AttackProfile MournBite = new(
		new(-1.05f, 1.013f, 0.977f), new(2.2f, 1.025f, 0.992f), new(-0.85f, 0.993f, 1.01f), new(0.12f, 1.003f, 0.998f),
		0.135f, 0.025f, 0.017f, 0.075f, 0.215f);

	// enemy_mourn_beast/leap: Deep haunch preparation and longer upward-forward commitment distinguish the leap; this envelope supplies no airborne path or independently drawn feet.
	private static readonly AttackProfile MournLeap = new(
		new(-2.8f, 1.026f, 0.954f), new(4.75f, 1.035f, 0.984f), new(-1.25f, 0.989f, 1.017f), new(0.28f, 1.005f, 0.996f),
		0.205f, 0.043f, 0.026f, 0.112f, 0.305f);

	// enemy_gaping_maw/claw: Low reptilian open-mawed beast uses quick foreclaw-side forward snaps; each of two real contacts is honored without pretending the static mouth bites.
	private static readonly AttackProfile GapingClaw = new(
		new(-2.2f, 1.008f, 0.979f), new(3.7f, 1.023f, 0.983f), new(-1.15f, 0.993f, 1.01f), new(0.18f, 1.003f, 0.997f),
		0.115f, 0.023f, 0.015f, 0.065f, 0.195f);

	// enemy_gaping_maw/rend: Wider low load and heavy tearing follow-through differentiate rend from light claws, retaining its current claw route and enormous fixed jaw silhouette.
	private static readonly AttackProfile GapingRend = new(
		new(-3.65f, 1.017f, 0.961f), new(5.15f, 1.031f, 0.969f), new(-1.4f, 0.988f, 1.017f), new(0.34f, 1.005f, 0.995f),
		0.205f, 0.042f, 0.028f, 0.112f, 0.32f);

	// enemy_haze_stag/antler_jab: High antler canopy and slender legs make a restrained neck-leading jab with little scale distortion, keeping the large rigid antlers legible.
	private static readonly AttackProfile StagJab = new(
		new(-0.85f, 1.003f, 0.992f), new(1.95f, 1.012f, 0.991f), new(-0.45f, 0.998f, 1.004f), new(0.08f, 1.001f, 0.999f),
		0.185f, 0.03f, 0.021f, 0.088f, 0.275f);

	// enemy_haze_stag/hoof_flurry: Slight lift into a compact downward hoof-side contact, quicker and more vertical than the antler jab; alternating front legs still require drawn frames.
	private static readonly AttackProfile StagHooves = new(
		new(-1.65f, 0.999f, 1.009f), new(2.8f, 1.011f, 0.981f), new(-0.65f, 0.997f, 1.006f), new(0.12f, 1.002f, 0.998f),
		0.135f, 0.027f, 0.018f, 0.074f, 0.235f);

	// enemy_haze_stag/antler_charge: Long committed forebody load and heavy antler-led drive make the charge distinct; no whole-body mist wobble or extra travel is introduced.
	private static readonly AttackProfile StagCharge = new(
		new(-2.65f, 1.011f, 0.973f), new(4.05f, 1.024f, 0.978f), new(-1.1f, 0.994f, 1.009f), new(0.24f, 1.003f, 0.997f),
		0.275f, 0.055f, 0.035f, 0.141f, 0.395f);

	// enemy_shell_guard/shell_bash: Tall crystalline rock shell inclines over small planted limbs then drives forward with tight nearly rigid scaling and slow damped recovery.
	private static readonly AttackProfile ShellBash = new(
		new(-1.35f, 1.002f, 0.989f), new(2.7f, 1.008f, 0.981f), new(-0.48f, 0.999f, 1.004f), new(0.095f, 1.001f, 0.999f),
		0.25f, 0.059f, 0.035f, 0.142f, 0.39f);

	// enemy_shell_guard/cracked_rage: Quicker sharper whole-body contact marks cracked rage without drawing a crack, removing shell plates or silently changing its current ambiguous slash route.
	private static readonly AttackProfile ShellRage = new(
		new(-2.25f, 1.005f, 0.982f), new(3.85f, 1.015f, 0.979f), new(-0.95f, 0.996f, 1.007f), new(0.2f, 1.002f, 0.998f),
		0.14f, 0.029f, 0.02f, 0.086f, 0.255f);

	// enemy_rock_beetle/horn_jab: Low faceted rock beetle leans its curved horn forward with a short rigid shove; little angular travel avoids turning the shell rubbery.
	private static readonly AttackProfile BeetleHorn = new(
		new(-0.65f, 1.002f, 0.993f), new(1.55f, 1.009f, 0.992f), new(-0.25f, 0.999f, 1.002f), new(0.04f, 1.0f, 0.999f),
		0.18f, 0.033f, 0.021f, 0.09f, 0.275f);

	// enemy_rock_beetle/bite: Faster deeper mandible-leading reaction distinguishes bite from the braced horn jab; actual shell break and jaw opening remain unprovided.
	private static readonly AttackProfile BeetleBite = new(
		new(-0.95f, 1.004f, 0.988f), new(2.15f, 1.014f, 0.987f), new(-0.55f, 0.997f, 1.004f), new(0.075f, 1.001f, 0.999f),
		0.105f, 0.021f, 0.014f, 0.06f, 0.19f);

	// enemy_maw_hunter/bite: Long thin mantis-like hunter gathers its open mandibles slowly, then commits to a precise head-leading bite; extended rigid forearms barely distort.
	private static readonly AttackProfile HunterBite = new(
		new(-1.3f, 1.007f, 0.982f), new(2.8f, 1.022f, 0.984f), new(-0.65f, 0.995f, 1.008f), new(0.1f, 1.002f, 0.998f),
		0.235f, 0.038f, 0.03f, 0.12f, 0.345f);

	// enemy_maw_hunter/claws: Thin lifted scythe forearms suggest fast angular claw commitments; all three are actual contact pulses and no independently swinging limbs are claimed.
	private static readonly AttackProfile HunterClaws = new(
		new(-2.75f, 0.998f, 1.009f), new(3.95f, 1.013f, 0.988f), new(-1.2f, 0.994f, 1.006f), new(0.21f, 1.002f, 0.999f),
		0.095f, 0.019f, 0.012f, 0.052f, 0.165f);

	// enemy_chomper/clamp: Mechanical steel jaw and exposed cog pivot use a tight hard-stop clamp commitment and short held pressure, without scaling teeth like soft tissue.
	private static readonly AttackProfile ChomperClamp = new(
		new(-0.5f, 1.002f, 0.995f), new(1.3f, 1.011f, 0.994f), new(-0.22f, 0.999f, 1.002f), new(0.035f, 1.0f, 1.0f),
		0.185f, 0.024f, 0.027f, 0.084f, 0.26f);

	// enemy_scald_gnat/bite: Light flying gnat pitches its long proboscis into a quick precise puncture and recoils fast; thin transparent wings remain a single static silhouette.
	private static readonly AttackProfile GnatBite = new(
		new(-1.1f, 1.003f, 0.991f), new(2.15f, 1.015f, 0.993f), new(-0.7f, 0.996f, 1.006f), new(0.1f, 1.002f, 0.999f),
		0.075f, 0.018f, 0.011f, 0.048f, 0.15f);

	// enemy_scald_gnat/suck: Small nearly straight feeding attachment with a longer contact hold distinguishes suck from the biting dart; no invented growth, suction beam or wing cycle.
	private static readonly AttackProfile GnatSuck = new(
		new(-0.25f, 1.002f, 0.996f), new(0.65f, 1.008f, 0.997f), new(-0.15f, 0.998f, 1.004f), new(0.025f, 1.001f, 1.0f),
		0.145f, 0.029f, 0.041f, 0.112f, 0.29f);

	// enemy_bowl_bug/roll: Round rock-armored rat makes the largest bounded rotational commitment in this batch, with rigid plates and a pronounced countersettle; this is not a full rolling animation.
	private static readonly AttackProfile BowlRoll = new(
		new(-3.9f, 1.004f, 0.989f), new(6.05f, 1.011f, 0.985f), new(-1.55f, 0.997f, 1.005f), new(0.3f, 1.002f, 0.998f),
		0.225f, 0.045f, 0.029f, 0.122f, 0.345f);

	// enemy_bowl_bug/tackle: Shorter flatter shell-first tackle pulses contrast with the long rotational roll; two hits require two actual callbacks.
	private static readonly AttackProfile BowlTackle = new(
		new(-1.4f, 1.005f, 0.986f), new(2.85f, 1.014f, 0.985f), new(-0.55f, 0.997f, 1.005f), new(0.08f, 1.002f, 0.999f),
		0.115f, 0.025f, 0.016f, 0.067f, 0.205f);

	// enemy_burrow_worm/tremor: Accepted art is a broad rock-backed mole, not a worm: small downward weight shifts suggest ground tremors while massive shell and digging claws stay rigid.
	private static readonly AttackProfile BurrowTremor = new(
		new(-0.55f, 1.003f, 0.991f), new(1.2f, 1.008f, 0.981f), new(-0.22f, 0.999f, 1.005f), new(0.045f, 1.001f, 0.999f),
		0.235f, 0.052f, 0.033f, 0.138f, 0.39f);

	// enemy_burrow_worm/bite: Heavy low snout and incisors lead a compact forward bite with a faster snap than tremor; no invented underground movement or shell detachment.
	private static readonly AttackProfile BurrowBite = new(
		new(-1.35f, 1.005f, 0.986f), new(2.6f, 1.015f, 0.988f), new(-0.62f, 0.997f, 1.005f), new(0.1f, 1.002f, 0.999f),
		0.155f, 0.029f, 0.024f, 0.101f, 0.3f);

	// enemy_moss/slam: Large bark-and-moss trunk braces over roots before a heavy downward fist-side slam; restrained rigid wood deformation and slow settle convey weight.
	private static readonly AttackProfile MossSlam = new(
		new(-2.8f, 1.006f, 0.98f), new(4.3f, 1.018f, 0.968f), new(-1.1f, 0.994f, 1.01f), new(0.23f, 1.003f, 0.997f),
		0.255f, 0.057f, 0.035f, 0.143f, 0.405f);

	// enemy_moss/bite: Short head-leading gnash commitment contrasts with the long root-braced slam; foliage loss and a genuinely opening wooden jaw require replacement frames.
	private static readonly AttackProfile MossBite = new(
		new(-1.25f, 1.004f, 0.987f), new(2.65f, 1.014f, 0.986f), new(-0.6f, 0.997f, 1.006f), new(0.095f, 1.001f, 0.999f),
		0.165f, 0.028f, 0.021f, 0.094f, 0.285f);

	// enemy_mud_lump/slop: Crouched lumpy mud creature slowly compresses before one soft release recoil, with delayed cohesive settling; existing throw route supplies the emitted slop.
	private static readonly AttackProfile MudSlop = new(
		new(0.4f, 1.024f, 0.96f), new(-1.45f, 1.018f, 1.015f), new(0.28f, 0.993f, 1.01f), new(0.045f, 1.006f, 0.995f),
		0.235f, 0.038f, 0.026f, 0.116f, 0.34f);

	// enemy_mud_lump/slam: Wide low compression and forward downward soft-body impact distinguish the mud slam from throwing; fingers and clumps are not separate articulated parts.
	private static readonly AttackProfile MudSlam = new(
		new(-1.7f, 1.029f, 0.948f), new(3.15f, 1.043f, 0.954f), new(-0.6f, 0.988f, 1.024f), new(0.13f, 1.009f, 0.993f),
		0.19f, 0.049f, 0.03f, 0.126f, 0.355f);

	// enemy_mud_lump/mud_toss: Faster oblique hand-side recoil and lower axial release than slop distinguish the protective mud toss; the alsoDefend effect remains outside this class.
	private static readonly AttackProfile MudToss = new(
		new(1.5f, 1.019f, 0.97f), new(-2.25f, 1.026f, 0.991f), new(0.47f, 0.991f, 1.013f), new(0.08f, 1.007f, 0.994f),
		0.165f, 0.027f, 0.019f, 0.092f, 0.285f);

	// enemy_snap_fruit/snap: Small red toothed fruit gathers into a quick low snap, using slight fleshy squash while the leaf stalk stays almost stable; no per-hit tooth growth is invented.
	private static readonly AttackProfile FruitSnap = new(
		new(-0.85f, 1.018f, 0.974f), new(2.1f, 1.03f, 0.98f), new(-0.38f, 0.993f, 1.014f), new(0.07f, 1.005f, 0.997f),
		0.08f, 0.018f, 0.012f, 0.052f, 0.16f);

	// enemy_vine_walker/swipe: Tall crooked branch arms make quick alternating-intent body sweeps on two actual contacts; this is not a true separated branch-arm swing.
	private static readonly AttackProfile VineSwipe = new(
		new(-3.1f, 0.998f, 1.008f), new(4.6f, 1.015f, 0.984f), new(-1.2f, 0.994f, 1.009f), new(0.2f, 1.003f, 0.998f),
		0.15f, 0.026f, 0.018f, 0.079f, 0.24f);

	// enemy_vine_walker/grasping_vines: Slow reaching brace and held low forward pressure distinguish grasping vines; no extending tendrils, entanglement mesh or additional mana logic is created.
	private static readonly AttackProfile VineGrasp = new(
		new(-1.2f, 1.005f, 0.99f), new(2.05f, 1.013f, 0.985f), new(-0.45f, 0.997f, 1.006f), new(0.07f, 1.002f, 0.999f),
		0.24f, 0.051f, 0.037f, 0.135f, 0.37f);

	// enemy_vine_walker/chomp: Trunk-head loads more deeply then snaps forward with a crisp stop; the central wooden mouth still needs drawn open and shut poses.
	private static readonly AttackProfile VineChomp = new(
		new(-2.1f, 1.009f, 0.978f), new(3.4f, 1.022f, 0.979f), new(-0.85f, 0.994f, 1.011f), new(0.16f, 1.003f, 0.997f),
		0.205f, 0.03f, 0.025f, 0.103f, 0.305f);

	// enemy_strangler_serpent/constrict: Broad coiled serpent slowly compresses its low mass into sustained pressure, with very little rotation; whole-sprite scale does not wrap coils around a target.
	private static readonly AttackProfile SerpentConstrict = new(
		new(-0.15f, 1.018f, 0.982f), new(0.45f, 1.032f, 0.971f), new(-0.1f, 0.994f, 1.012f), new(0.025f, 1.006f, 0.996f),
		0.27f, 0.06f, 0.044f, 0.151f, 0.41f);

	// enemy_strangler_serpent/lash: Upright neck and loose coils counterlean then whip into a quick oblique commitment, contrasting strongly with the near-axial squeeze.
	private static readonly AttackProfile SerpentLash = new(
		new(-3.25f, 0.996f, 1.015f), new(4.35f, 1.025f, 0.983f), new(-1.3f, 0.99f, 1.012f), new(0.26f, 1.004f, 0.997f),
		0.13f, 0.022f, 0.014f, 0.067f, 0.21f);

	// enemy_strangler_serpent/thwack: Lower heavier tail-side body commitment and longer settling distinguish thwack from lash; tail location and actual contact arc need separate frames.
	private static readonly AttackProfile SerpentThwack = new(
		new(-1.8f, 1.011f, 0.978f), new(2.95f, 1.029f, 0.973f), new(-0.62f, 0.993f, 1.013f), new(0.12f, 1.005f, 0.996f),
		0.185f, 0.037f, 0.025f, 0.105f, 0.3f);

	// Families05 attack-only static-sprite envelopes. Existing fifty-two profiles stay exact.
	// enemy_raptor_egg has no attack move and deliberately remains unsupported here.

	// enemy_big_slime/slam: Native art is a massive moss-coated reptilian body with thick planted forelimbs, not an amorphous blob; load its haunches then deliver a slow heavy forebody slam with limited soft compression.
	private static readonly AttackProfile BigSlimeSlam = new(
		new(-2.25f, 1.013f, 0.966f), new(3.75f, 1.027f, 0.956f), new(-0.85f, 0.991f, 1.016f), new(0.19f, 1.006f, 0.995f),
		0.265f, 0.058f, 0.038f, 0.151f, 0.42f);

	// enemy_moss_slime/bump: Tiny mossy lizard-like child makes a light low bump with quick commitment; mild fleshy compression keeps its snout and feet readable rather than inheriting the parent mass.
	private static readonly AttackProfile MossSlimeBump = new(
		new(-0.65f, 1.018f, 0.974f), new(1.75f, 1.028f, 0.988f), new(-0.32f, 0.994f, 1.012f), new(0.055f, 1.004f, 0.997f),
		0.105f, 0.025f, 0.017f, 0.075f, 0.225f);

	// enemy_moss_spawner/lash: Arching tree body braces on roots and commits its hanging branch side into a deliberate lash; bark remains near rigid and no spawning pocket or child is animated here.
	private static readonly AttackProfile SpawnerLash = new(
		new(-2.9f, 1.001f, 0.992f), new(4.15f, 1.012f, 0.983f), new(-1.05f, 0.996f, 1.007f), new(0.18f, 1.002f, 0.998f),
		0.22f, 0.034f, 0.025f, 0.113f, 0.33f);

	// enemy_brood_toad/tongue: Heavy round-bellied toad gives a restrained snout-leading reach while its broad belly stays low; true tongue extension and internal brood movement are absent from static art.
	private static readonly AttackProfile ToadTongue = new(
		new(-0.45f, 1.008f, 0.986f), new(1.4f, 1.023f, 0.993f), new(-0.28f, 0.996f, 1.008f), new(0.04f, 1.003f, 0.998f),
		0.19f, 0.029f, 0.021f, 0.093f, 0.285f);

	// enemy_broodling/gnaw: Pale curled larva makes a short front-mouth gnaw commitment with localized-looking low-axis compression; the outer segmented shell is not fully uncoiled.
	private static readonly AttackProfile BroodGnaw = new(
		new(-0.55f, 1.012f, 0.98f), new(1.55f, 1.026f, 0.987f), new(-0.26f, 0.993f, 1.012f), new(0.035f, 1.004f, 0.997f),
		0.08f, 0.017f, 0.011f, 0.047f, 0.145f);

	// enemy_brood_raptor/claw: Long-tailed mother raptor loads over strong hindlegs then strikes forward with foreclaw-side commitment, restrained enough to avoid sweeping its long tail through neighbours.
	private static readonly AttackProfile BroodRaptorClaw = new(
		new(-2.45f, 1.012f, 0.979f), new(3.85f, 1.028f, 0.985f), new(-1.05f, 0.992f, 1.012f), new(0.2f, 1.004f, 0.997f),
		0.175f, 0.029f, 0.02f, 0.089f, 0.27f);

	// enemy_raptor_chick/peck: Small alert raptor chick darts its narrow head forward with a fast light reset; distinct from the mother body and no chick-to-parent animation reuse.
	private static readonly AttackProfile ChickPeck = new(
		new(-1.2f, 1.009f, 0.986f), new(2.65f, 1.021f, 0.993f), new(-0.72f, 0.994f, 1.008f), new(0.1f, 1.002f, 0.999f),
		0.07f, 0.018f, 0.011f, 0.047f, 0.15f);

	// enemy_axe_automaton/one_two: Square armored axe machine gives two measured rigid shoulder commitments on actual contacts; hard stops and tight scaling keep the joints metallic.
	private static readonly AttackProfile AutomatonDouble = new(
		new(-1.7f, 1.002f, 0.992f), new(2.95f, 1.009f, 0.987f), new(-0.68f, 0.998f, 1.004f), new(0.12f, 1.001f, 0.999f),
		0.145f, 0.03f, 0.02f, 0.081f, 0.245f);

	// enemy_axe_automaton/hammer_upper: Deeper crouch followed by a rising torso/weapon-side commitment distinguishes the uppercut from the flat one-two, with a slower mechanical reset.
	private static readonly AttackProfile AutomatonUpper = new(
		new(-2.75f, 1.006f, 0.978f), new(3.9f, 1.012f, 1.004f), new(-0.95f, 0.996f, 0.995f), new(0.19f, 1.002f, 0.999f),
		0.225f, 0.039f, 0.026f, 0.107f, 0.32f);

	// enemy_axe_automaton_reboot/one_two: Exposed orange inner mechanism uses a slightly longer braced start and firmer two-contact stop; no invented malfunction jitter or resurrection timing is added.
	private static readonly AttackProfile RebootDouble = new(
		new(-1.9f, 1.001f, 0.991f), new(3.2f, 1.01f, 0.985f), new(-0.8f, 0.998f, 1.004f), new(0.145f, 1.001f, 0.999f),
		0.165f, 0.032f, 0.021f, 0.086f, 0.265f);

	// enemy_axe_automaton_reboot/hammer_upper: Rebuilt machine loads the exposed chassis before a strong rising commitment and measured return; existing boot/revival event handling owns its phase transition.
	private static readonly AttackProfile RebootUpper = new(
		new(-2.95f, 1.005f, 0.98f), new(4.1f, 1.013f, 1.003f), new(-1.05f, 0.996f, 0.996f), new(0.205f, 1.002f, 0.999f),
		0.245f, 0.043f, 0.029f, 0.118f, 0.345f);

	// enemy_elite_deathless/cleave: Mossed plate knight and enormous shield support a heavy sword-side cleave with a long load and damped rigid recovery.
	private static readonly AttackProfile DeathlessCleave = new(
		new(-3.85f, 1.002f, 0.983f), new(5.05f, 1.013f, 0.976f), new(-1.22f, 0.996f, 1.008f), new(0.26f, 1.003f, 0.998f),
		0.255f, 0.046f, 0.03f, 0.127f, 0.375f);

	// enemy_elite_deathless/stance: Guarded short offensive check keeps shield-forward posture and returns defensively; this real attack move is not excluded just because its name is stance.
	private static readonly AttackProfile DeathlessStance = new(
		new(-1.35f, 1.002f, 0.992f), new(2.45f, 1.009f, 0.987f), new(-0.62f, 0.999f, 1.004f), new(-0.075f, 1.001f, 0.999f),
		0.195f, 0.03f, 0.022f, 0.091f, 0.28f);

	// enemy_elite_husk_3/slash: Most-armored husk braces its plate torso for a measured sword sweep with little scale change and heavy recovery.
	private static readonly AttackProfile HuskThirdSlash = new(
		new(-3.2f, 1.001f, 0.988f), new(4.55f, 1.012f, 0.983f), new(-1.05f, 0.996f, 1.007f), new(0.22f, 1.002f, 0.999f),
		0.22f, 0.038f, 0.025f, 0.107f, 0.32f);

	// enemy_elite_husk_2/slash: Partly exposed skeleton cuts more quickly with a narrower counterlean than the armored form; surviving plate and bones remain rigid.
	private static readonly AttackProfile HuskSecondSlash = new(
		new(-3.7f, 0.998f, 1.005f), new(4.95f, 1.014f, 0.989f), new(-1.5f, 0.994f, 1.006f), new(0.26f, 1.003f, 0.999f),
		0.16f, 0.026f, 0.017f, 0.077f, 0.235f);

	// enemy_elite_husk_1/slash: Bare final skeleton has the lightest fastest sword-side commitment and sharpest return; sparse bones are not scaled into rubber and phase numbering does not reverse the anatomy.
	private static readonly AttackProfile HuskFirstSlash = new(
		new(-4.1f, 0.997f, 1.009f), new(5.3f, 1.016f, 0.991f), new(-1.8f, 0.992f, 1.008f), new(0.31f, 1.003f, 0.998f),
		0.115f, 0.021f, 0.013f, 0.059f, 0.185f);

	// enemy_biting_scroll/chomp: Rolled parchment jaws fold into a moderate one-off chomp commitment; soft paper scaling stays bounded and cannot actually hinge the curled scroll ends.
	private static readonly AttackProfile ScrollChomp = new(
		new(-1.3f, 1.011f, 0.984f), new(2.35f, 1.028f, 0.977f), new(-0.55f, 0.992f, 1.015f), new(0.085f, 1.005f, 0.996f),
		0.15f, 0.027f, 0.02f, 0.087f, 0.255f);

	// enemy_biting_scroll/chew: Smaller faster papery chewing pulses honor the two real hit callbacks, contrasting with the wider chomp rather than looping a continuous flap.
	private static readonly AttackProfile ScrollChew = new(
		new(-0.55f, 1.007f, 0.991f), new(1.15f, 1.019f, 0.986f), new(-0.27f, 0.995f, 1.009f), new(0.04f, 1.003f, 0.998f),
		0.085f, 0.018f, 0.012f, 0.05f, 0.16f);

	// enemy_lost_soul/debilitating_smog: Skeletal floating soul gathers its trailing cloth vertically then makes a slow shallow exhalation-like accent at real spell contact; no idle bob, alpha fade or smoke emission is synthesized.
	private static readonly AttackProfile LostSmog = new(
		new(0.35f, 0.996f, 1.012f), new(-0.7f, 1.006f, 0.995f), new(0.18f, 0.999f, 1.005f), new(0.025f, 1.001f, 1.0f),
		0.25f, 0.041f, 0.027f, 0.116f, 0.345f);

	// enemy_lost_soul/eye_lasers: Narrow skull-leading aim and crisp rearward beam release distinguish eye lasers from drifting smog; both target contacts share one release.
	private static readonly AttackProfile LostLasers = new(
		new(0.95f, 0.998f, 1.007f), new(-1.7f, 1.008f, 0.996f), new(0.42f, 0.999f, 1.003f), new(0.065f, 1.001f, 1.0f),
		0.185f, 0.025f, 0.017f, 0.075f, 0.23f);

	// enemy_forgotten_soul/miasma: Wide hood and ragged robe gather low then expand minimally at the actual miasma contact, preserving the hood silhouette without changing mist opacity.
	private static readonly AttackProfile ForgottenMiasma = new(
		new(-0.3f, 1.006f, 0.992f), new(0.6f, 1.011f, 1.008f), new(-0.14f, 0.997f, 1.003f), new(0.02f, 1.002f, 0.999f),
		0.27f, 0.047f, 0.032f, 0.132f, 0.375f);

	// enemy_forgotten_soul/dread: Taller slower looming load followed by a firmer forward cloth-body commitment separates dread from the broad miasma pressure, with no fear glyphs invented.
	private static readonly AttackProfile ForgottenDread = new(
		new(-1.15f, 0.997f, 1.014f), new(1.85f, 1.013f, 0.986f), new(-0.4f, 0.996f, 1.006f), new(0.075f, 1.002f, 0.999f),
		0.23f, 0.033f, 0.025f, 0.105f, 0.315f);

	// enemy_elite_stab_book/stab2: Floating bladed book braces its hard spine and points the left-projecting knives into deliberate paired thrust accents; pages stay a static fan.
	private static readonly AttackProfile BookStabTwo = new(
		new(-1.1f, 0.999f, 1.004f), new(2.2f, 1.014f, 0.993f), new(-0.48f, 0.997f, 1.004f), new(0.065f, 1.002f, 0.999f),
		0.145f, 0.025f, 0.018f, 0.073f, 0.22f);

	// enemy_elite_stab_book/stab3: Tighter faster book-spine reset between three actual stabs, with a stronger blade-axis commitment than the paired attack and no extra invented knives.
	private static readonly AttackProfile BookStabThree = new(
		new(-1.45f, 0.998f, 1.006f), new(2.65f, 1.017f, 0.991f), new(-0.64f, 0.996f, 1.006f), new(0.085f, 1.002f, 0.999f),
		0.115f, 0.021f, 0.014f, 0.06f, 0.19f);

	// enemy_elite_stab_book/stab4: Fastest crisp blade-axis thrust/recovery fits four actual contacts while rigid book cover and knives retain nearly constant proportions.
	private static readonly AttackProfile BookStabFour = new(
		new(-1.75f, 0.997f, 1.008f), new(3.05f, 1.02f, 0.989f), new(-0.78f, 0.995f, 1.007f), new(0.11f, 1.003f, 0.998f),
		0.095f, 0.018f, 0.012f, 0.052f, 0.17f);

	// enemy_elite_mirror_djinn/mirror: Four-armed floating djinn makes a compact centered casting/cutting body accent for each observed mirror contact; it does not choose an individual arm or invent the player-derived hit count.
	private static readonly AttackProfile DjinnMirror = new(
		new(-1.9f, 0.998f, 1.007f), new(2.75f, 1.014f, 0.992f), new(-0.85f, 0.995f, 1.008f), new(0.16f, 1.003f, 0.998f),
		0.125f, 0.023f, 0.015f, 0.066f, 0.205f);

	// enemy_elite_doom_chanter/dual_chant: Very tall slim chanter lifts through its narrow robe then gives two restrained chant-contact accents; the skull and raised hands remain aligned with no autonomous singing loop.
	private static readonly AttackProfile ChanterDual = new(
		new(-0.35f, 0.997f, 1.009f), new(0.75f, 1.008f, 0.995f), new(-0.18f, 0.999f, 1.004f), new(0.03f, 1.001f, 1.0f),
		0.215f, 0.036f, 0.026f, 0.102f, 0.305f);

	// enemy_nemesis_wraith/curse_touch: Ragged floating scythe bearer inclines its reaching-hand side for a long delicate cursed touch; no materialization, opacity cycling or weapon swing is assumed for this spell.
	private static readonly AttackProfile WraithTouch = new(
		new(-0.85f, 0.998f, 1.008f), new(1.65f, 1.01f, 0.993f), new(-0.34f, 0.998f, 1.005f), new(0.055f, 1.001f, 1.0f),
		0.245f, 0.038f, 0.027f, 0.112f, 0.34f);

	// enemy_nemesis_wraith/scythe: Wide high scythe silhouette loads backward then sweeps with a sharp oblique follow-through and cloth-like counterlean; the blade arc needs independent arm frames.
	private static readonly AttackProfile WraithScythe = new(
		new(-4.2f, 0.997f, 1.013f), new(5.6f, 1.016f, 0.982f), new(-1.6f, 0.992f, 1.011f), new(0.36f, 1.004f, 0.998f),
		0.225f, 0.032f, 0.023f, 0.099f, 0.3f);

	// enemy_elite_sergeant/whip: Broad red armored sergeant loads the long raised whip side then snaps forward with a sharp stop; static whip geometry cannot propagate a crack.
	private static readonly AttackProfile SergeantWhip = new(
		new(-3.4f, 1.001f, 0.991f), new(4.75f, 1.014f, 0.981f), new(-1.15f, 0.996f, 1.008f), new(0.23f, 1.003f, 0.998f),
		0.205f, 0.027f, 0.018f, 0.089f, 0.28f);

	// enemy_elite_sergeant/double_whip: Shorter rapid repeated shoulder commitments contrast with the single heavy whip, preserving both real callbacks and avoiding automatic whip oscillation.
	private static readonly AttackProfile SergeantDouble = new(
		new(-2.7f, 1.002f, 0.988f), new(3.9f, 1.018f, 0.983f), new(-1.35f, 0.995f, 1.007f), new(0.18f, 1.002f, 0.998f),
		0.125f, 0.021f, 0.013f, 0.061f, 0.195f);

	// enemy_elite_sentry/bolt: Floating stone sentry with circular arch and side barrel holds a rigid aim then makes a tiny mechanical release kick; no stone squash or orbiting arch parts.
	private static readonly AttackProfile SentryBolt = new(
		new(0.35f, 1.001f, 0.997f), new(-0.85f, 0.998f, 1.002f), new(0.16f, 1.001f, 0.999f), new(0.02f, 1.0f, 1.0f),
		0.18f, 0.021f, 0.018f, 0.078f, 0.245f);

	// enemy_elite_sentry/beam: Longer pressure brace, larger rigid recoil and slower damped return distinguish the beam from its small bolt; target contact never restarts the shot.
	private static readonly AttackProfile SentryBeam = new(
		new(0.65f, 1.002f, 0.993f), new(-1.35f, 0.997f, 1.004f), new(0.29f, 1.001f, 0.998f), new(0.045f, 1.0f, 1.0f),
		0.265f, 0.036f, 0.028f, 0.119f, 0.355f);

	// enemy_elite_iron_egg/tail: Huge faceted iron sphere stays rigid while a restrained body counterrotation suggests its segmented tail strike; sleep and awakening do not occur in this envelope.
	private static readonly AttackProfile IronEggTail = new(
		new(-1.4f, 1.001f, 0.995f), new(2.25f, 1.006f, 0.989f), new(-0.45f, 0.999f, 1.003f), new(0.08f, 1.001f, 0.999f),
		0.245f, 0.036f, 0.027f, 0.118f, 0.35f);

	// enemy_elite_slaver/chain: Heavy leather-clad chain bearer loads the forward extended chain arm and commits a weighted draw, with slower recovery than the whip-like lash.
	private static readonly AttackProfile SlaverChain = new(
		new(-2.55f, 1.002f, 0.99f), new(3.65f, 1.013f, 0.984f), new(-0.9f, 0.996f, 1.007f), new(0.18f, 1.002f, 0.998f),
		0.235f, 0.034f, 0.025f, 0.108f, 0.325f);

	// enemy_elite_slaver/lash: Sharper higher shoulder sweep and quick counterlean make lash distinct from the chain draw; actual loose-link propagation is missing from the single PNG.
	private static readonly AttackProfile SlaverLash = new(
		new(-3.35f, 0.999f, 1.007f), new(4.65f, 1.016f, 0.987f), new(-1.3f, 0.994f, 1.009f), new(0.25f, 1.003f, 0.998f),
		0.145f, 0.024f, 0.016f, 0.076f, 0.235f);

	// enemy_elite_slaver/weight: Long low brace and downward heavy stop suggest the block at the chain end landing with weight; no independent chain length or new slowing VFX is generated.
	private static readonly AttackProfile SlaverWeight = new(
		new(-1.75f, 1.006f, 0.979f), new(3.05f, 1.019f, 0.971f), new(-0.65f, 0.996f, 1.01f), new(0.13f, 1.003f, 0.997f),
		0.28f, 0.056f, 0.036f, 0.147f, 0.405f);

	// enemy_elite_giant_face/crush: Massive floating carved stone face loads almost axially then lands a slow rigid crushing commitment, minimizing rotation and elasticity despite large gameplay damage.
	private static readonly AttackProfile GiantFaceCrush = new(
		new(-0.65f, 1.001f, 0.995f), new(1.65f, 1.006f, 0.98f), new(-0.23f, 0.999f, 1.004f), new(0.045f, 1.001f, 0.999f),
		0.305f, 0.068f, 0.042f, 0.167f, 0.445f);

	// enemy_elite_gold_raven/dive: Large spread-wing black-and-gold raven pitches into a committed downward dive accent, keeping layered feathers broad and avoiding an invented flight path or flap cycle.
	private static readonly AttackProfile RavenDive = new(
		new(-2.15f, 0.997f, 1.011f), new(4.2f, 1.018f, 0.977f), new(-1.05f, 0.992f, 1.011f), new(0.24f, 1.003f, 0.998f),
		0.205f, 0.036f, 0.024f, 0.104f, 0.31f);

	// enemy_elite_gold_raven/peck: Short head-leading beak peck with a rapid reset contrasts with the full-wing dive; large feather planes remain nearly rigid.
	private static readonly AttackProfile RavenPeck = new(
		new(-0.8f, 1.002f, 0.994f), new(1.95f, 1.016f, 0.991f), new(-0.46f, 0.997f, 1.005f), new(0.06f, 1.001f, 0.999f),
		0.095f, 0.02f, 0.013f, 0.057f, 0.18f);

	// enemy_elite_devourer/bite: Long segmented grub with a circular tooth-ring mouth compresses slowly along its low axis before a heavy feeding contact; the ring does not magically close and segments do not telescope.
	private static readonly AttackProfile DevourerBite = new(
		new(-0.45f, 1.013f, 0.982f), new(1.2f, 1.031f, 0.981f), new(-0.23f, 0.992f, 1.014f), new(0.04f, 1.006f, 0.996f),
		0.235f, 0.038f, 0.031f, 0.129f, 0.365f);

	// enemy_elite_owl/wing: Broad owl gives a measured lateral wing-side body commitment while keeping its disk face upright enough to read; real wing sweep requires a separate joint and feather poses.
	private static readonly AttackProfile OwlWing = new(
		new(-2.3f, 0.998f, 1.011f), new(3.55f, 1.013f, 0.984f), new(-0.8f, 0.996f, 1.008f), new(0.17f, 1.002f, 0.998f),
		0.165f, 0.029f, 0.02f, 0.088f, 0.27f);

	// enemy_elite_owl/talon_dive: Deeper held brace and stronger downward-forward talon commitment distinguish dive from wing checking; no new flight trajectory or independently extended claws.
	private static readonly AttackProfile OwlDive = new(
		new(-2.85f, 1.006f, 0.982f), new(4.55f, 1.022f, 0.973f), new(-1.18f, 0.991f, 1.013f), new(0.27f, 1.004f, 0.997f),
		0.245f, 0.042f, 0.028f, 0.121f, 0.345f);

	// enemy_crab_crusher/thrash: Long wall-rooted claw arm makes a compact rigid whole-arm thrash; small angles keep its far-reaching open pincer within conservative footprint bounds.
	private static readonly AttackProfile CrusherThrash = new(
		new(-1.65f, 1.001f, 0.994f), new(2.55f, 1.008f, 0.988f), new(-0.65f, 0.998f, 1.004f), new(0.12f, 1.001f, 0.999f),
		0.19f, 0.035f, 0.024f, 0.103f, 0.315f);

	// enemy_crab_crusher/swell_strike: Longer pressure brace and heavier low drive distinguish the swelling strike without visually growing a shell according to gameplay damage.
	private static readonly AttackProfile CrusherSwell = new(
		new(-1.1f, 1.004f, 0.988f), new(2.05f, 1.014f, 0.981f), new(-0.45f, 0.997f, 1.006f), new(0.09f, 1.002f, 0.999f),
		0.255f, 0.047f, 0.033f, 0.132f, 0.375f);

	// enemy_crab_crusher/bug_sting: Two short precise tip-leading stings use minimal whole-arm rotation; no unseen insect stinger is invented inside the massive pincer.
	private static readonly AttackProfile CrusherSting = new(
		new(-0.45f, 1.002f, 0.996f), new(1.05f, 1.011f, 0.994f), new(-0.22f, 0.998f, 1.003f), new(0.035f, 1.001f, 1.0f),
		0.105f, 0.019f, 0.013f, 0.057f, 0.19f);

	// enemy_crab_crusher/guarded_strike: Guarded pincer-side contact has a smaller advance and deliberate retreat, preserving the wall attachment and leaving alsoDefend outside the visual class.
	private static readonly AttackProfile CrusherGuarded = new(
		new(-0.75f, 1.002f, 0.991f), new(1.65f, 1.009f, 0.985f), new(-0.42f, 0.999f, 1.004f), new(-0.055f, 1.001f, 0.999f),
		0.215f, 0.036f, 0.027f, 0.109f, 0.33f);

	// enemy_crab_cannon/reticle: Very long wall-rooted cannon steadies with a tiny sighting accent at real contact; current reticle fallback is non-ranged and no aiming overlay or projectile is fabricated.
	private static readonly AttackProfile CannonReticle = new(
		new(0.2f, 1.001f, 0.998f), new(-0.45f, 0.999f, 1.001f), new(0.09f, 1.0f, 0.999f), new(0.012f, 1.0f, 1.0f),
		0.245f, 0.025f, 0.02f, 0.094f, 0.295f);

	// enemy_crab_cannon/precision_beam: Near-rigid long barrel pressure-loads and kicks minutely backward at observed precision-beam release, protecting the huge horizontal silhouette.
	private static readonly AttackProfile CannonPrecision = new(
		new(0.3f, 1.002f, 0.995f), new(-0.95f, 0.997f, 1.003f), new(0.18f, 1.001f, 0.999f), new(0.025f, 1.0f, 1.0f),
		0.27f, 0.027f, 0.022f, 0.105f, 0.33f);

	// enemy_crab_cannon/laser: Stronger longer charge brace and deeper but bounded recoil distinguish laser; no turret sweep, length growth or extra shot is introduced.
	private static readonly AttackProfile CannonLaser = new(
		new(0.55f, 1.003f, 0.991f), new(-1.45f, 0.996f, 1.005f), new(0.32f, 1.001f, 0.998f), new(0.05f, 1.0f, 1.0f),
		0.31f, 0.042f, 0.031f, 0.139f, 0.405f);

	// enemy_winch_warden/flail: Tall exposed winch machine and hanging spiked weight load the chain side for two compact mechanical impacts; chain swinging and spool unwinding need actual parts.
	private static readonly AttackProfile WinchFlail = new(
		new(-2.0f, 1.001f, 0.991f), new(3.1f, 1.009f, 0.984f), new(-0.75f, 0.998f, 1.005f), new(0.14f, 1.001f, 0.999f),
		0.195f, 0.031f, 0.023f, 0.101f, 0.31f);

	// enemy_winch_warden/hyper_beam: Long braced machine charge and hard damped beam recoil convey large force without amplifying based on damage; rewind remains an excluded rest action.
	private static readonly AttackProfile WinchHyper = new(
		new(0.9f, 1.003f, 0.987f), new(-1.85f, 0.996f, 1.006f), new(0.4f, 1.002f, 0.997f), new(0.07f, 1.001f, 1.0f),
		0.325f, 0.046f, 0.035f, 0.15f, 0.425f);

	// enemy_seal_orb/beam: Small brass floating orb uses a quick tiny axial/rotational release kick around its visible purple aperture, distinct from the heavy parent machine; no orbiting bob loop.
	private static readonly AttackProfile SealOrbBeam = new(
		new(0.15f, 1.001f, 0.998f), new(-0.55f, 0.998f, 1.002f), new(0.1f, 1.0f, 0.999f), new(0.012f, 1.0f, 1.0f),
		0.13f, 0.019f, 0.014f, 0.058f, 0.19f);

	// enemy_ember_furnace/devour_ember: Heavy four-legged iron furnace makes six short furnace-mouth-leading intake accents on actual contacts; HP-derived damage never changes visual scale or emission count.
	private static readonly AttackProfile FurnaceDevour = new(
		new(-0.6f, 1.003f, 0.993f), new(1.35f, 1.01f, 0.989f), new(-0.24f, 0.999f, 1.003f), new(0.04f, 1.001f, 0.999f),
		0.145f, 0.025f, 0.02f, 0.08f, 0.25f);

	// enemy_ember_furnace/sear: Rigid boiler pressure brace and modest counterlean mark sear at its existing spell contact; heat art and vent smoke are not synthesized by the transform.
	private static readonly AttackProfile FurnaceSear = new(
		new(0.4f, 1.002f, 0.992f), new(-0.85f, 1.008f, 1.006f), new(0.18f, 0.998f, 1.002f), new(0.025f, 1.001f, 0.999f),
		0.25f, 0.038f, 0.027f, 0.116f, 0.345f);

	// enemy_ember_furnace/tackle: Low quadruped furnace drives its heavy plated chest forward twice with slow mechanical recovery, distinct from small intake pulses and axial heat casting.
	private static readonly AttackProfile FurnaceTackle = new(
		new(-1.3f, 1.006f, 0.981f), new(2.6f, 1.015f, 0.976f), new(-0.55f, 0.998f, 1.006f), new(0.105f, 1.002f, 0.998f),
		0.22f, 0.044f, 0.03f, 0.122f, 0.355f);

	// enemy_ember_furnace/inferno: Centered furnace pressure pulses at six real spell contacts keep the chimney and plates nearly rigid; no six extra beam releases or autonomous fire ring is invented.
	private static readonly AttackProfile FurnaceInferno = new(
		new(0.15f, 1.004f, 0.986f), new(-0.4f, 1.012f, 1.011f), new(0.08f, 0.997f, 1.004f), new(0.015f, 1.002f, 0.998f),
		0.18f, 0.029f, 0.023f, 0.097f, 0.285f);

	// enemy_chimera_1/bite: First phase is a stocky horned ram/lion-like body: planted forequarters brace before a heavy short muzzle-leading bite, keeping the curled horns rigid.
	private static readonly AttackProfile ChimeraFirstBite = new(
		new(-1.6f, 1.012f, 0.979f), new(3.0f, 1.028f, 0.984f), new(-0.7f, 0.993f, 1.01f), new(0.13f, 1.003f, 0.998f),
		0.205f, 0.033f, 0.025f, 0.11f, 0.32f);

	// enemy_chimera_1/skull_bash: Longer deeper neck/haunch load and broad horned-skull commitment distinguish the bash; tail and dorsal plates remain the same rigid image.
	private static readonly AttackProfile ChimeraFirstBash = new(
		new(-2.6f, 1.019f, 0.966f), new(4.25f, 1.035f, 0.972f), new(-1.0f, 0.99f, 1.016f), new(0.21f, 1.005f, 0.996f),
		0.265f, 0.05f, 0.033f, 0.136f, 0.385f);

	// enemy_chimera_2/swelling_claws: Second phase is a lean long-snouted clawed dragon body: fast forelimb-side commitments and tight resets suit growing real hit counts, with no pose magnification as uses increase.
	private static readonly AttackProfile ChimeraSecondClaws = new(
		new(-2.7f, 0.997f, 1.008f), new(4.1f, 1.019f, 0.984f), new(-1.1f, 0.993f, 1.01f), new(0.22f, 1.003f, 0.998f),
		0.125f, 0.023f, 0.016f, 0.066f, 0.205f);

	// enemy_chimera_3/lacerate: Final phase is a broad red-maned lion with a second head above: stronger planted claw commitments for three actual hits, not the lean second-phase pose copied at a new color.
	private static readonly AttackProfile ChimeraThirdLacerate = new(
		new(-3.05f, 1.008f, 0.981f), new(4.65f, 1.026f, 0.979f), new(-1.25f, 0.992f, 1.013f), new(0.26f, 1.004f, 0.997f),
		0.14f, 0.025f, 0.018f, 0.079f, 0.24f);

	// enemy_chimera_3/pounce: Deep final-phase haunch compression and slow massive forward pounce commitment contrast with rapid laceration; existing Stage owns travel and no independently airborne paws or second-head animation is supplied.
	private static readonly AttackProfile ChimeraThirdPounce = new(
		new(-3.4f, 1.026f, 0.951f), new(5.15f, 1.043f, 0.968f), new(-1.4f, 0.988f, 1.02f), new(0.35f, 1.006f, 0.995f),
		0.3f, 0.056f, 0.037f, 0.148f, 0.42f);

	private void SelectAttackProfile(string moveId)
	{
		AttackProfile p;
		switch (_profileId)
		{
		case "enemy_probe":
			p = moveId == "lunge" ? ProbeLunge : ProbePoke;
			break;
		case "enemy_turtle":
			p = moveId == "awake_bite" ? TurtleAwakeBite : TurtleCrush;
			break;
		case "enemy_drummer":
			p = DrummerThump;
			break;
		case "enemy_spore_cap":
			p = moveId == "smash" ? SporeSmash : SporeFrail;
			break;
		case "enemy_hexer":
			p = moveId == "slap" ? HexerSlap : HexerMud;
			break;
		case "enemy_iron_clam":
			p = ClamJet;
			break;
		case "enemy_mudling":
			p = MudlingBump;
			break;
		case "enemy_set_wary":
			p = WarySlash;
			break;
		case "enemy_sludge_spider":
			p = moveId == "slam" ? SpiderSlam : moveId == "rage" ? SpiderRage : SpiderOil;
			break;
		case "enemy_slug":
			p = moveId == "acid_spit" ? SlugAcidSpit : moveId == "tackle" ? SlugTackle : SlugLick;
			break;

		case "enemy_set_breaker":
			p = moveId == "toss_junk" ? BreakerToss : moveId == "break_trap" ? BreakerTrap : BreakerSmash;
			break;
		case "enemy_joker":
			p = moveId == "cautious_jab" ? JokerJab : moveId == "call_bluff" ? JokerBluff : JokerWild;
			break;
		case "enemy_thief":
			p = ThiefMug;
			break;
		case "enemy_mimic_imp":
			p = ImpMimic;
			break;
		case "enemy_mimic_jester":
			p = JesterMirror;
			break;
		case "enemy_cultist":
			p = CultistBlade;
			break;
		case "enemy_shield_squire":
			p = moveId == "avenging_rush" ? SquireRush : SquireBash;
			break;
		case "enemy_frog_knight":
			p = moveId == "strike_down" ? FrogDown : moveId == "beetle_charge" ? FrogCharge : FrogTongue;
			break;
		case "enemy_kin_follower":
			p = moveId == "boomerang" ? KinBoomerang : KinSlash;
			break;
		case "enemy_warden":
			p = moveId == "gate_guard" ? WardenGate : moveId == "bell_hushed" ? WardenHushed : moveId == "bell_frenzy" ? WardenFrenzy : WardenBell;
			break;
		case "enemy_axe_ogre":
			p = AxeGreatSwing;
			break;
		case "enemy_apprentice_colossus":
			p = ApprenticeSwing;
			break;
		case "enemy_whetstone_colossus":
			p = WhetstoneGuillotine;
			break;
		case "enemy_cog_construct":
			p = moveId == "expel" ? CogExpel : CogRepeater;
			break;
		case "enemy_thunder_globe":
			p = moveId == "thunder_strike" ? GlobeThunder : moveId == "galvanic_burst" ? GlobeBurst : GlobeSlap;
			break;
		case "enemy_sludge_berserker":
			p = moveId == "hug" ? SludgeHug : moveId == "smother" ? SludgeSmother : SludgePummel;
			break;

		case "enemy_moss_healer":
			p = HealerPoke;
			break;
		case "enemy_bomber":
			p = moveId == "big_boom" ? BomberBoom : BomberEmber;
			break;
		case "enemy_devoted_sculptor":
			p = moveId == "savage" ? SculptorSavage : SculptorCarve;
			break;
		case "enemy_kin_priest":
			p = moveId == "orb_weak" ? PriestWeak : moveId == "beam" ? PriestBeam : moveId == "judgment_orb" ? PriestJudgment : moveId == "blood_beam" ? PriestBloodBeam : PriestFrail;
			break;
		case "enemy_cinder_imp":
			p = moveId == "bite" ? CinderBite : CinderSpark;
			break;
		case "enemy_wide_power":
			p = WideSurge;
			break;
		case "enemy_thorn_squirrel":
			p = SquirrelGnaw;
			break;
		case "enemy_bond_wolf":
			p = moveId == "lunge" ? BondLunge : BondTwin;
			break;
		case "enemy_mourn_beast":
			p = moveId == "leap" ? MournLeap : MournBite;
			break;
		case "enemy_gaping_maw":
			p = moveId == "rend" ? GapingRend : GapingClaw;
			break;
		case "enemy_haze_stag":
			p = moveId == "hoof_flurry" ? StagHooves : moveId == "antler_charge" ? StagCharge : StagJab;
			break;
		case "enemy_shell_guard":
			p = moveId == "cracked_rage" ? ShellRage : ShellBash;
			break;
		case "enemy_rock_beetle":
			p = moveId == "bite" ? BeetleBite : BeetleHorn;
			break;
		case "enemy_maw_hunter":
			p = moveId == "claws" ? HunterClaws : HunterBite;
			break;
		case "enemy_chomper":
			p = ChomperClamp;
			break;
		case "enemy_scald_gnat":
			p = moveId == "suck" ? GnatSuck : GnatBite;
			break;
		case "enemy_bowl_bug":
			p = moveId == "tackle" ? BowlTackle : BowlRoll;
			break;
		case "enemy_burrow_worm":
			p = moveId == "bite" ? BurrowBite : BurrowTremor;
			break;
		case "enemy_moss":
			p = moveId == "bite" ? MossBite : MossSlam;
			break;
		case "enemy_mud_lump":
			p = moveId == "slam" ? MudSlam : moveId == "mud_toss" ? MudToss : MudSlop;
			break;
		case "enemy_snap_fruit":
			p = FruitSnap;
			break;
		case "enemy_vine_walker":
			p = moveId == "grasping_vines" ? VineGrasp : moveId == "chomp" ? VineChomp : VineSwipe;
			break;
		case "enemy_strangler_serpent":
			p = moveId == "lash" ? SerpentLash : moveId == "thwack" ? SerpentThwack : SerpentConstrict;
			break;

		case "enemy_big_slime":
			p = BigSlimeSlam;
			break;
		case "enemy_moss_slime":
			p = MossSlimeBump;
			break;
		case "enemy_moss_spawner":
			p = SpawnerLash;
			break;
		case "enemy_brood_toad":
			p = ToadTongue;
			break;
		case "enemy_broodling":
			p = BroodGnaw;
			break;
		case "enemy_brood_raptor":
			p = BroodRaptorClaw;
			break;
		case "enemy_raptor_chick":
			p = ChickPeck;
			break;
		case "enemy_axe_automaton":
			p = moveId == "hammer_upper" ? AutomatonUpper : AutomatonDouble;
			break;
		case "enemy_axe_automaton_reboot":
			p = moveId == "hammer_upper" ? RebootUpper : RebootDouble;
			break;
		case "enemy_elite_deathless":
			p = moveId == "stance" ? DeathlessStance : DeathlessCleave;
			break;
		case "enemy_elite_husk_3":
			p = HuskThirdSlash;
			break;
		case "enemy_elite_husk_2":
			p = HuskSecondSlash;
			break;
		case "enemy_elite_husk_1":
			p = HuskFirstSlash;
			break;
		case "enemy_biting_scroll":
			p = moveId == "chew" ? ScrollChew : ScrollChomp;
			break;
		case "enemy_lost_soul":
			p = moveId == "eye_lasers" ? LostLasers : LostSmog;
			break;
		case "enemy_forgotten_soul":
			p = moveId == "dread" ? ForgottenDread : ForgottenMiasma;
			break;
		case "enemy_elite_stab_book":
			p = moveId == "stab3" ? BookStabThree : moveId == "stab4" ? BookStabFour : BookStabTwo;
			break;
		case "enemy_elite_mirror_djinn":
			p = DjinnMirror;
			break;
		case "enemy_elite_doom_chanter":
			p = ChanterDual;
			break;
		case "enemy_nemesis_wraith":
			p = moveId == "scythe" ? WraithScythe : WraithTouch;
			break;
		case "enemy_elite_sergeant":
			p = moveId == "double_whip" ? SergeantDouble : SergeantWhip;
			break;
		case "enemy_elite_sentry":
			p = moveId == "beam" ? SentryBeam : SentryBolt;
			break;
		case "enemy_elite_iron_egg":
			p = IronEggTail;
			break;
		case "enemy_elite_slaver":
			p = moveId == "lash" ? SlaverLash : moveId == "weight" ? SlaverWeight : SlaverChain;
			break;
		case "enemy_elite_giant_face":
			p = GiantFaceCrush;
			break;
		case "enemy_elite_gold_raven":
			p = moveId == "peck" ? RavenPeck : RavenDive;
			break;
		case "enemy_elite_devourer":
			p = DevourerBite;
			break;
		case "enemy_elite_owl":
			p = moveId == "talon_dive" ? OwlDive : OwlWing;
			break;
		case "enemy_crab_crusher":
			p = moveId == "swell_strike" ? CrusherSwell : moveId == "bug_sting" ? CrusherSting : moveId == "guarded_strike" ? CrusherGuarded : CrusherThrash;
			break;
		case "enemy_crab_cannon":
			p = moveId == "precision_beam" ? CannonPrecision : moveId == "laser" ? CannonLaser : CannonReticle;
			break;
		case "enemy_winch_warden":
			p = moveId == "hyper_beam" ? WinchHyper : WinchFlail;
			break;
		case "enemy_seal_orb":
			p = SealOrbBeam;
			break;
		case "enemy_ember_furnace":
			p = moveId == "sear" ? FurnaceSear : moveId == "tackle" ? FurnaceTackle : moveId == "inferno" ? FurnaceInferno : FurnaceDevour;
			break;
		case "enemy_chimera_1":
			p = moveId == "skull_bash" ? ChimeraFirstBash : ChimeraFirstBite;
			break;
		case "enemy_chimera_2":
			p = ChimeraSecondClaws;
			break;
		case "enemy_chimera_3":
			p = moveId == "pounce" ? ChimeraThirdPounce : ChimeraThirdLacerate;
			break;

		default:
			// The original three profiles and every original ownership path stay unchanged.
			return;
		}
		(_load, _strike, _recoil, _settle) = (p.Load, p.Strike, p.Recoil, p.Settle);
		(_windup, _snap, _hold, _recoilEnd, _recovery) = (p.Windup, p.Snap, p.Hold, p.RecoilEnd, p.Recovery);
	}

	private bool Owns(float now, int executionIndex, string? moveId, int ownerVersion) => _active &&
		ValidTime(now) && now >= _started && executionIndex == _execution && ownerVersion == _ownerVersion &&
		string.Equals(moveId ?? "", _moveId, StringComparison.Ordinal);

	private Pose RecoveryPose(float age)
	{
		if (age <= _hold) return _strike;
		if (age < _recoilEnd) return Pose.Lerp(_strike, _recoil, (age - _hold) / (_recoilEnd - _hold));
		float settleAt = _recoilEnd + (_recovery - _recoilEnd) * 0.38f;
		return age < settleAt ? Pose.Lerp(_recoil, _settle, (age - _recoilEnd) / (settleAt - _recoilEnd)) :
			Pose.Lerp(_settle, Pose.Rest, (age - settleAt) / (_recovery - settleAt));
	}

	private void Finish() { _active = false; _phase = "idle"; _pose = Pose.Rest; }
	private static bool ValidTime(float value) => float.IsFinite(value) && value >= 0f;
	private static float Smooth(float t) { t = Math.Clamp(t, 0f, 1f); return t * t * (3f - 2f * t); }
}

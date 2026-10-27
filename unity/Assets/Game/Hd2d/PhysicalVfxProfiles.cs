#nullable enable
// dot の Godot 版 (checkpoint 192) の PhysicalVfxProfiles.cs をそのまま (名前空間だけ)。札 id → 物理の当たりの家族・重さ・材質・位相。
namespace DeckRogue.Game.Hd2d
{
// Presentation-only contracts. The combat event ledger owns damage, targets,
// hit count and ordering; profiles must never manufacture an additional hit.
internal enum PhysicalVfxFamily { DirectionalCut, BluntImpact, AxialThrust, GroundPressure, OpticalRake, OpticalBind, ConvergingPressure }
internal enum PhysicalVfxTargetPattern { SelectedTarget, ResolvedSweep, ConditionalTransfer }
internal enum PhysicalVfxMaterial { Mineral, Ember, Vegetal, Wood }
internal enum PhysicalVfxWeight { Standard, Heavy }
internal enum PhysicalVfxAnchor { ActualWeaponToReachableSurface, TargetGround, ActualCastToSelectedReceiver }
internal enum PhysicalVfxHitPolicy { EachResolvedDamageEvent }

internal sealed record AttackVfxPhases(float Contact, float PoseLead, float Anticipation, float Release);
internal sealed record PhysicalVfxProfile(
    string CardId, PhysicalVfxFamily Family, PhysicalVfxAnchor Anchor,
    AttackVfxPhases Phases, SourceShowcaseProfile Motion,
    PhysicalVfxHitPolicy HitPolicy = PhysicalVfxHitPolicy.EachResolvedDamageEvent, string? AudioCardId = null, PhysicalVfxWeight Weight = PhysicalVfxWeight.Standard,
    PhysicalVfxTargetPattern TargetPattern = PhysicalVfxTargetPattern.SelectedTarget, float TargetSpacing = 0f, float ImpactScale = 1f, PhysicalVfxMaterial Material = PhysicalVfxMaterial.Mineral);

// Explicit opt-in registry, NOT a renderer-name/type fallback. Support cards,
// elemental hybrids and unreviewed physical cards retain their existing route.
// Opt-in representative families. Weight is independent of shape and damage.
internal static class PhysicalVfxProfiles
{
    internal static readonly PhysicalVfxProfile DirectionalCut = new(
        "green_strike", PhysicalVfxFamily.DirectionalCut,
        PhysicalVfxAnchor.ActualWeaponToReachableSurface,
        new(.32f,.22f,.22f,.50f),
        new(.32f,.20f,.12f,.20f,.50f,3.0f,.075f,.025f,.095f,false,"slash"));
    internal static readonly PhysicalVfxProfile GroundPressure = new(
        "green_finisher_stomp", PhysicalVfxFamily.GroundPressure,
        PhysicalVfxAnchor.TargetGround,
        new(.62f,.22f,.50f,.75f),
        new(.62f,.25f,.16f,.28f,.75f,.16f,.13f,.03f,.12f,true,"stomp"),
        Weight: PhysicalVfxWeight.Heavy);

    internal static readonly PhysicalVfxProfile BluntImpact = new(
        "green_basic_bash", PhysicalVfxFamily.BluntImpact,
        PhysicalVfxAnchor.ActualWeaponToReachableSurface,
        new(.32f,.22f,.22f,.42f),
        new(.32f,.20f,.12f,.20f,.42f,3.0f,.075f,.025f,.095f,false,"slash"),
        AudioCardId: "green_strike");

    // Dry fibre/chip material; authoritative conditional events own the cadence.
    // This is not a summoned branch, club or extra second hit.
    private static readonly PhysicalVfxProfile SaplingStrike = BluntImpact with {
        CardId="green_sapling_strike", Material=PhysicalVfxMaterial.Wood,
        Motion=BluntImpact.Motion with { Interval=.28f } };

    internal static readonly PhysicalVfxProfile AxialThrust = new(
        "green_weak_point", PhysicalVfxFamily.AxialThrust,
        PhysicalVfxAnchor.ActualWeaponToReachableSurface,
        new(.30f,.22f,.22f,.38f),
        new(.30f,.18f,.12f,.18f,.38f,3.0f,.065f,.025f,.085f,false,"slash"),
        AudioCardId: "green_strike");

    internal static readonly PhysicalVfxProfile TripleHorn = new(
        "green_triple_horn", PhysicalVfxFamily.AxialThrust,
        PhysicalVfxAnchor.ActualWeaponToReachableSurface,
        new(.30f,.22f,.22f,.38f),
        new(.30f,.18f,.18f,.18f,.38f,3.0f,.065f,.025f,.085f,false,"slash"),
        AudioCardId: "green_strike");

    internal static readonly PhysicalVfxProfile Sweep = new(
        "green_sweep", PhysicalVfxFamily.DirectionalCut,
        PhysicalVfxAnchor.ActualWeaponToReachableSurface,
        new(.32f,.22f,.22f,.50f),
        new(.32f,.20f,.24f,.20f,.50f,3.0f,.055f,.020f,.060f,false,"slash"),
        AudioCardId: "green_strike", TargetPattern: PhysicalVfxTargetPattern.ResolvedSweep, TargetSpacing: .045f);

    internal static readonly PhysicalVfxProfile OpticalRake = new(
        "black_rotten_claw", PhysicalVfxFamily.OpticalRake,
        PhysicalVfxAnchor.ActualWeaponToReachableSurface,
        new(.32f,.22f,.22f,.42f),
        new(.32f,.20f,.16f,.20f,.42f,3.0f,.065f,.025f,.085f,false,"slash"),
        AudioCardId: "green_strike");

    // Explicit geometry batch: card payloads (momentum, pierce, tokens and
    // authoritative hit count) stay in the engine/event ledger, not this table.
    private static readonly PhysicalVfxProfile ShivToken = DirectionalCut with { CardId="black_shiv_token", AudioCardId="green_strike" };
    private static readonly PhysicalVfxProfile ShivStrike = DirectionalCut with { CardId="black_shiv_strike", AudioCardId="green_strike" };
    private static readonly PhysicalVfxProfile TwigStrike = BluntImpact with { CardId="green_twig_strike" };
    private static readonly PhysicalVfxProfile HornThrust = AxialThrust with { CardId="green_horn_thrust" };
    private static readonly PhysicalVfxProfile ChargingHorn = AxialThrust with { CardId="green_charging_horn" };
    private static readonly PhysicalVfxProfile HornVolley = TripleHorn with { CardId="green_horn_volley" };
    private static readonly PhysicalVfxProfile WindCutter = DirectionalCut with { CardId="green_wind_cutter", AudioCardId="green_strike" };

    // Explicit second batch. Engine events, not optical repetitions, determine
    // momentum ordering, pierce, surviving targets and number of passes.
    private static readonly PhysicalVfxProfile TrampleCharge = BluntImpact with {
        CardId="green_trample_charge", Motion=BluntImpact.Motion with { Interval=.22f } };
    private static readonly PhysicalVfxProfile Stampede = BluntImpact with {
        CardId="green_sig_stampede", Weight=PhysicalVfxWeight.Heavy,ImpactScale=1.22f,
        Phases=new(.40f,.22f,.26f,.44f), Motion=BluntImpact.Motion with {
            Contact=.40f,Windup=.25f,Recovery=.24f,Residue=.44f,ImpactPause=.095f,FinalPause=.12f,Heavy=true } };
    private static readonly PhysicalVfxProfile Trample = GroundPressure with {
        CardId="green_sig_trample",Weight=PhysicalVfxWeight.Standard,ImpactScale=.84f,
        AudioCardId="green_finisher_stomp",Phases=new(.52f,.22f,.28f,.75f),
        Motion=GroundPressure.Motion with { Contact=.52f,Windup=.23f,Interval=.48f,
            ImpactPause=.085f,ChainPause=.025f,FinalPause=.10f,Heavy=false } };
    private static readonly PhysicalVfxProfile SweepingHorn = AxialThrust with {
        CardId="green_sweeping_horn",TargetPattern=PhysicalVfxTargetPattern.ResolvedSweep,TargetSpacing=.045f,
        Motion=AxialThrust.Motion with { Interval=.24f } };
    private static readonly PhysicalVfxProfile HornStorm = SweepingHorn with { CardId="green_horn_storm" };

    // Material is independent of family/weight/target pattern and never changes
    // engine damage type. This execute card is NOT pierce and does not apply burn.
    private static readonly PhysicalVfxProfile FinishingFlame = AxialThrust with {
        CardId="red_finishing_flame",Material=PhysicalVfxMaterial.Ember };

    // A second receiver exists only when the engine emits the lethal-cleave event.
    // Presentation delay is not a new mechanical turn or additional bounce.
    private static readonly PhysicalVfxProfile Billiard = BluntImpact with {
        CardId="red_billiard",TargetPattern=PhysicalVfxTargetPattern.ConditionalTransfer,TargetSpacing=.32f };

    // Verified physical/slash metadata. Spark-token gameplay axis does not
    // establish an elemental coating; fusion cost/block remain engine events.
    private static readonly PhysicalVfxProfile Severance = DirectionalCut with { CardId="white_severance", AudioCardId="green_strike" };
    private static readonly PhysicalVfxProfile PracticeSwing = DirectionalCut with { CardId="fusion_practice_swing", AudioCardId="green_strike" };

    // Optical vegetal surface, not a summoned solid vine/whip. Two actual engine
    // contacts own the two loads; fibres and leaves never count as extra hits.
    private static readonly PhysicalVfxProfile DoubleLash = DirectionalCut with {
        CardId="green_double_lash",Material=PhysicalVfxMaterial.Vegetal,AudioCardId="green_strike",
        Motion=DirectionalCut.Motion with { Interval=.26f } };

    // Deliberate reviewed design proposal for a formerly neutral route: actual
    // illustration shows a broad axe/blade engaging an armored receiver.
    // Execute threshold affects authoritative damage, never extra optical hits.
    private static readonly PhysicalVfxProfile FinalBlow = DirectionalCut with { CardId="red_final_blow", AudioCardId="green_strike" };

    // Explicit abstract presentation choice for unresolved neutral artwork.
    // Compact local pressure conveys the actual blows without inventing fists,
    // blades or a fire status from the card's name. No additional hit events.
    private static readonly PhysicalVfxProfile NeutralFlurry = BluntImpact with {
        CardId="red_flurry",Motion=BluntImpact.Motion with { Interval=.22f } };

    // Explicit abstract neutral-pressure choices: these legacy neutral routes
    // do not establish a literal fist, fang, wind body or elemental light beam.
    // Pierce, resources, weakness and cost conditions remain engine-owned.
    private static readonly PhysicalVfxProfile Sprint = BluntImpact with { CardId="green_sprint" };
    private static readonly PhysicalVfxProfile Tailwind = BluntImpact with { CardId="green_tailwind" };
    private static readonly PhysicalVfxProfile ShiningStrike = BluntImpact with { CardId="white_shining_strike" };
    private static readonly PhysicalVfxProfile FangBreaker = BluntImpact with { CardId="red_fang_breaker" };
    // Optical vegetal residue only; discard-copy creation remains a separate
    // authoritative post-command card-zone event, never a second strike.
    private static readonly PhysicalVfxProfile SpreadingVine = DoubleLash with { CardId="green_spreading_vine" };

    // Five actual Growth/damage pairs; later Growth survives receiver death,
    // while missing later damage events never create extra cuts/retargets.
    private static readonly PhysicalVfxProfile VineDance = DoubleLash with { CardId="green_sig_vine_dance" };
    // Explicit neutral-pressure choices; current-Growth condition and post-hit
    // Growth are engine receipts, not animation-generated contacts.
    private static readonly PhysicalVfxProfile VerdantHour = NeutralFlurry with { CardId="green_verdant_hour" };
    private static readonly PhysicalVfxProfile FinisherWrath = Stampede with { CardId="green_finisher_wrath" };

    // Damage-mode plant_bind_contact is a distinct inward optical compression,
    // not a vine slash or a status-imposing cage. Defence mode emits no direct
    // damage and therefore never creates this effect/source attack ownership.
    private static readonly PhysicalVfxProfile Entangle = new(
        "green_entangle",PhysicalVfxFamily.OpticalBind,PhysicalVfxAnchor.ActualWeaponToReachableSurface,
        new(.34f,.22f,.22f,.40f),new(.34f,.20f,.12f,.20f,.40f,3.0f,.065f,.025f,.085f,false,"vine"),
        AudioCardId:"green_strike",Material:PhysicalVfxMaterial.Vegetal);

    // Deliberate abstract pressure for explicit neutral source contexts.
    // Self-HP loss ordering, permanent-count scaling, intent branches and
    // zero/blocked outcomes remain authoritative events, never extra blows.
    private static readonly PhysicalVfxProfile Reckless = BluntImpact with { CardId="red_reckless" };
    private static readonly PhysicalVfxProfile BloodPrice = BluntImpact with { CardId="black_blood_price" };
    private static readonly PhysicalVfxProfile Rally = BluntImpact with { CardId="white_rally" };
    // Internal leaf_strike ID is not vegetal evidence; leaf_blade is a separate
    // deferred card and is intentionally NOT included by this explicit mapping.
    private static readonly PhysicalVfxProfile LeafStrike = BluntImpact with { CardId="green_leaf_strike" };

    // Reviewed opposing optical pressure; no teeth/jaw/extra hit.
    private static readonly PhysicalVfxProfile Fang = new(
        "green_fang",PhysicalVfxFamily.ConvergingPressure,PhysicalVfxAnchor.ActualWeaponToReachableSurface,
        new(.34f,.22f,.22f,.40f),new(.34f,.21f,.16f,.21f,.40f,3.0f,.080f,.025f,.095f,false,"slash"),
        AudioCardId:"green_strike");

    // Explicit physical source contexts; independent heal/Light receipts are
    // not coating, extra damage or a reason to borrow elemental geometry.
    private static readonly PhysicalVfxProfile HolyMaul = BluntImpact with { CardId="white_holy_maul" };
    private static readonly PhysicalVfxProfile LightKnockout = BluntImpact with { CardId="white_light_knockout" };
    private static readonly PhysicalVfxProfile HarvestStrike = Fang with { CardId="green_harvest_strike" };

    // Explicit neutral source contexts: abstract pressure, not tree/fire/light
    // anatomy inferred from names. Dynamic amounts/costs/riders stay engine-owned.
    private static readonly PhysicalVfxProfile TrunkStance = BluntImpact with { CardId="green_trunk_stance" };
    private static readonly PhysicalVfxProfile PreyStrike = BluntImpact with { CardId="green_prey_strike" };
    private static readonly PhysicalVfxProfile BlazePursuit = NeutralFlurry with { CardId="red_blaze_pursuit" };
    private static readonly PhysicalVfxProfile SprintStrike = BluntImpact with { CardId="fusion_sprint_strike" };
    private static readonly PhysicalVfxProfile HolyStance = BluntImpact with { CardId="fusion_holy_stance" };

    // Exact direct physical/slash contexts and inspected metal-blade artwork.
    // Same-key necro rows remain excluded by SupportsRoute's source-kind gate.
    // Prayer preserves the selected warm Cut accent; healing is independent.
    private static readonly PhysicalVfxProfile BlackStrike = DirectionalCut with { CardId="black_strike",AudioCardId="green_strike" };
    private static readonly PhysicalVfxProfile OblivionBlade = DirectionalCut with { CardId="black_oblivion_blade",AudioCardId="green_strike" };
    private static readonly PhysicalVfxProfile OblivionAxe = DirectionalCut with { CardId="black_oblivion_axe",AudioCardId="green_strike" };
    private static readonly PhysicalVfxProfile ShadowBlade = DirectionalCut with { CardId="black_shadow_blade",AudioCardId="green_strike" };
    private static readonly PhysicalVfxProfile BladePrayer = DirectionalCut with { CardId="white_blade_prayer",AudioCardId="green_strike" };

    // Exact physical/source-context mappings; resource riders and instance growth
    // remain real engine events, not fabricated optical repeats or material names.
    private static readonly PhysicalVfxProfile Scythe = DirectionalCut with { CardId="black_scythe",AudioCardId="green_strike" };
    private static readonly PhysicalVfxProfile BloodSpray = DirectionalCut with { CardId="black_blood_spray",AudioCardId="green_strike" };
    private static readonly PhysicalVfxProfile GrowingFang = Fang with { CardId="green_growing_fang" };
    private static readonly PhysicalVfxProfile WildStampede = TripleHorn with { CardId="fusion_wild_stampede" };
    private static readonly PhysicalVfxProfile BloomDance = NeutralFlurry with { CardId="fusion_bloom_dance" };

    // Explicit physical source contexts; names do not invent flame, wood,
    // darkness coating, summoned tools or extra conversion hits.
    private static readonly PhysicalVfxProfile BurningBone = NeutralFlurry with { CardId="black_burning_bone" };
    private static readonly PhysicalVfxProfile ShadowCleaver = DirectionalCut with { CardId="black_shadow_cleaver",AudioCardId="green_strike" };
    private static readonly PhysicalVfxProfile SaplingMaul = FinisherWrath with { CardId="fusion_sapling_maul" };
    private static readonly PhysicalVfxProfile DevourVine = NeutralFlurry with { CardId="green_devour_vine" };

    // Both explicit player activations of Knight retain card-owned physical
    // action. PlayNecro is not an automatic onSelfExhausted passive source.
    // X Vine uses only authoritative expanded hits, never a fixed repeat count.
    private static readonly PhysicalVfxProfile NecroKnight = NeutralFlurry with { CardId="black_necro_knight" };
    private static readonly PhysicalVfxProfile XVineFlurry = DoubleLash with { CardId="green_x_vine_flurry" };

    // Conversion/intent and mill riders stay engine-owned. Axial is an
    // optical direction, not newly granted pierce; mill is not an extra Fang.
    private static readonly PhysicalVfxProfile ReadingWedge = AxialThrust with { CardId="fusion_reading_wedge", Motion=AxialThrust.Motion with { Interval=.26f } };
    private static readonly PhysicalVfxProfile RevivingFang = Fang with { CardId="black_reviving_fang" };

    // Explicit SPELL delivery of an existing vegetal contact material. Registry
    // grouping does not grant a weapon approach, literal vine weapon or Bind.
    private static readonly PhysicalVfxProfile RootingVineCast = new(
        "fusion_rooting_vine",PhysicalVfxFamily.DirectionalCut,PhysicalVfxAnchor.ActualCastToSelectedReceiver,
        new(.40f,.28f,.28f,.50f),new(.40f,.14f,.24f,.23f,.50f,.06f,.055f,.025f,.075f,false,"slash"),
        AudioCardId:"green_strike",Material:PhysicalVfxMaterial.Vegetal);

    internal static PhysicalVfxProfile? ForCard(string? cardId) => cardId switch
    {
        "fusion_rooting_vine" => RootingVineCast,
        "fusion_reading_wedge" => ReadingWedge,
        "black_reviving_fang" => RevivingFang,
        "black_necro_knight" => NecroKnight,
        "green_x_vine_flurry" => XVineFlurry,
        "black_burning_bone" => BurningBone,
        "black_shadow_cleaver" => ShadowCleaver,
        "fusion_sapling_maul" => SaplingMaul,
        "green_devour_vine" => DevourVine,
        "black_scythe" => Scythe,
        "black_blood_spray" => BloodSpray,
        "green_growing_fang" => GrowingFang,
        "fusion_wild_stampede" => WildStampede,
        "fusion_bloom_dance" => BloomDance,
        "black_strike" => BlackStrike,
        "black_oblivion_blade" => OblivionBlade,
        "black_oblivion_axe" => OblivionAxe,
        "black_shadow_blade" => ShadowBlade,
        "white_blade_prayer" => BladePrayer,
        "green_trunk_stance" => TrunkStance,
        "green_prey_strike" => PreyStrike,
        "red_blaze_pursuit" => BlazePursuit,
        "fusion_sprint_strike" => SprintStrike,
        "fusion_holy_stance" => HolyStance,
        "white_holy_maul" => HolyMaul,
        "white_light_knockout" => LightKnockout,
        "green_harvest_strike" => HarvestStrike,
        "green_sapling_strike" => SaplingStrike,
        "red_reckless" => Reckless,
        "black_blood_price" => BloodPrice,
        "white_rally" => Rally,
        "green_leaf_strike" => LeafStrike,
        "green_fang" => Fang,
        "green_entangle" => Entangle,
        "green_sig_vine_dance" => VineDance,
        "green_verdant_hour" => VerdantHour,
        "green_finisher_wrath" => FinisherWrath,
        "green_sprint" => Sprint,
        "green_tailwind" => Tailwind,
        "white_shining_strike" => ShiningStrike,
        "red_fang_breaker" => FangBreaker,
        "green_spreading_vine" => SpreadingVine,
        "green_double_lash" => DoubleLash,
        "red_flurry" => NeutralFlurry,
        "red_final_blow" => FinalBlow,
        "white_severance" => Severance,
        "fusion_practice_swing" => PracticeSwing,
        "red_finishing_flame" => FinishingFlame,
        "red_billiard" => Billiard,
        "green_trample_charge" => TrampleCharge,
        "green_sig_stampede" => Stampede,
        "green_sig_trample" => Trample,
        "green_sweeping_horn" => SweepingHorn,
        "green_horn_storm" => HornStorm,
        "black_rotten_claw" => OpticalRake,
        "black_shiv_token" => ShivToken,
        "black_shiv_strike" => ShivStrike,
        "green_twig_strike" => TwigStrike,
        "green_horn_thrust" => HornThrust,
        "green_charging_horn" => ChargingHorn,
        "green_horn_volley" => HornVolley,
        "green_wind_cutter" => WindCutter,
        "green_strike" => DirectionalCut,
        "green_basic_bash" => BluntImpact,
        "green_weak_point" => AxialThrust,
        "green_triple_horn" => TripleHorn,
        "green_sweep" => Sweep,
        "green_finisher_stomp" => GroundPressure,
        _ => null
    };
    internal static PhysicalVfxProfile? ForRoute(string? key) =>
        key != null && key.StartsWith("card:", System.StringComparison.Ordinal)
            ? ForCard(key.Substring(5)) : null;
}
}

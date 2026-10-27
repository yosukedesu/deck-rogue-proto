#nullable enable
// dot の Godot 版 (checkpoint 192) の ElementalVfxProfiles.cs をそのまま (名前空間だけ)。札 id → 属性の家族 (火・水・氷・光・闇・光の散り・地割れ・広い水・氷の散り)。
namespace DeckRogue.Game.Hd2d
{
// Spell materials and motion stay distinct from physical contact families.
// This registry shares only event-relative phase and ownership infrastructure.
internal enum ElementalVfxFamily { Fire, Water, Ice, Light, Dark, LightScatter, EarthFracture, WaterWide, IceScatter }
internal sealed record ElementalVfxProfile(string CardId, ElementalVfxFamily Family,
    AttackVfxPhases Phases, SourceShowcaseProfile Motion, float LaunchLead, float TargetSpacing=0f);
internal static class ElementalVfxProfiles
{
    internal static readonly ElementalVfxProfile Fire = new("red_strike",ElementalVfxFamily.Fire,
        new(.40f,.28f,.28f,.55f),new(.40f,.14f,.12f,.23f,.55f,.06f,.055f,.025f,.075f,false,"fire"),.17f);
    // Actual Fire card routes only. Burn/draw resolve independently; two shots
    // follow two emitted damage events, not a fabricated cascade or burn tick.
    private static readonly ElementalVfxProfile SpreadingFire = Fire with { CardId="red_spreading_fire" };
    private static readonly ElementalVfxProfile DoubleTap = Fire with {
        CardId="red_double_tap",Motion=Fire.Motion with { Interval=.26f } };
    internal static readonly ElementalVfxProfile Water = new("blue_strike",ElementalVfxFamily.Water,
        new(.40f,.28f,.28f,.52f),new(.40f,.14f,.12f,.23f,.52f,.06f,.055f,.025f,.075f,false,"water"),.20f);
    internal static readonly ElementalVfxProfile Ice = new("blue_ice_lance",ElementalVfxFamily.Ice,
        new(.46f,.28f,.32f,.60f),new(.46f,.16f,.14f,.23f,.60f,.06f,.055f,.025f,.075f,false,"ice"),.16f);
    internal static readonly ElementalVfxProfile Light = new("white_strike",ElementalVfxFamily.Light,
        new(.40f,.28f,.30f,.42f),new(.40f,.14f,.12f,.22f,.42f,.06f,.055f,.025f,.075f,false,"light"),.15f);
    internal static readonly ElementalVfxProfile Dark = new("black_drain",ElementalVfxFamily.Dark,
        new(.42f,.28f,.30f,.44f),new(.42f,.15f,.14f,.22f,.44f,.06f,.055f,.025f,.075f,false,"dark"),.17f);
    // Explicit actual-route/material matches; card costs, draw and ice gains
    // remain independently authoritative events, never inferred from this map.
    private static readonly ElementalVfxProfile TideDrop = Water with { CardId="blue_tide_drop" };
    private static readonly ElementalVfxProfile RapidStrike = Water with { CardId="blue_rapid_strike" };
    private static readonly ElementalVfxProfile CurrentLash = Water with { CardId="blue_current_lash" };
    private static readonly ElementalVfxProfile FrostThorn = Ice with { CardId="blue_frost_thorn" };
    private static readonly ElementalVfxProfile ShieldStrike = Light with { CardId="white_shield_strike" };
    private static readonly ElementalVfxProfile LightGlint = Light with { CardId="white_light_glint" };
    // Renewed optical loads follow actual hits; omitted lethal-chain events stay omitted.
    private static readonly ElementalVfxProfile LightBolt = Light with {
        CardId="white_light_bolt",Motion=Light.Motion with { Interval=.22f } };
    // Explicit single Water and focused Light routes. Catalyst echo is crafting
    // metadata, not an extra hit; actual damage events own volley truncation.
    private static readonly ElementalVfxProfile CatalystEcho = Water with { CardId="blue_catalyst_echo" };
    private static readonly ElementalVfxProfile LightDart = Light with { CardId="white_light_dart" };
    private static readonly ElementalVfxProfile LightVolley = LightBolt with { CardId="white_light_volley" };
    private static readonly ElementalVfxProfile ModeCrossroad = Light with { CardId="white_mode_crossroad" };
    // Direct card-source dark routes only. Independent healing and exhausted-card
    // recovery are not paired to the outgoing optical contraction.
    private static readonly ElementalVfxProfile BigDrain = Dark with { CardId="black_big_drain" };
    private static readonly ElementalVfxProfile BloodThirst = Dark with { CardId="black_blood_thirst" };
    private static readonly ElementalVfxProfile WraithFeast = Dark with { CardId="black_wraith_feast" };
    private static readonly ElementalVfxProfile CorpseKiss = Dark with { CardId="black_corpse_kiss" };
    // One cast scatter, actual positive receiver events only; stagger changes
    // presentation scheduling, never engine targets/count or turn timing.
    private static readonly ElementalVfxProfile LightSpray = new("white_light_spray",ElementalVfxFamily.LightScatter,
        new(.42f,.28f,.30f,.44f),new(.42f,.15f,.14f,.22f,.44f,.06f,.055f,.025f,.075f,false,"light"),.17f,.025f);
    private static readonly ElementalVfxProfile EarthRoar = new("green_earth_roar",ElementalVfxFamily.EarthFracture,
        new(.46f,.30f,.30f,.68f),new(.46f,.18f,.42f,.24f,.68f,.07f,.065f,.025f,.08f,false,"earth"),.16f,.035f);
    // One source-owned crest follows actual ordered receiver contact points.
    // Spacing is presentation-only; no targets/damage events are manufactured.
    private static readonly ElementalVfxProfile RollingWave = new("blue_rolling_wave",ElementalVfxFamily.WaterWide,
        new(.48f,.30f,.36f,.72f),new(.48f,.16f,.18f,.24f,.72f,.06f,.055f,.025f,.075f,false,"water"),.28f,.10f);
    // Exact existing material routes. Counters/resources/hand size and target
    // count remain authoritative engine state; names do not create extra hits.
    private static readonly ElementalVfxProfile LightLance = Light with { CardId="white_light_lance" };
    private static readonly ElementalVfxProfile LightTorrent = LightBolt with { CardId="white_light_torrent" };
    private static readonly ElementalVfxProfile StormLash = Water with { CardId="blue_storm_lash" };
    private static readonly ElementalVfxProfile Tsunami = RollingWave with { CardId="blue_tsunami" };
    private static readonly ElementalVfxProfile TideSurge = RollingWave with { CardId="blue_tide_surge" };
    // Angular distributed arrivals; one material pulse per actual damage receipt.
    // The later IceBlock gains remain independent engine/status events.
    private static readonly ElementalVfxProfile IceHail = new("blue_ice_hail",ElementalVfxFamily.IceScatter,
        new(.46f,.28f,.32f,.60f),new(.46f,.16f,.14f,.23f,.60f,.06f,.055f,.025f,.075f,false,"ice"),.20f,.035f);
    // Actual mode0 Water attack only; mode1 IceBlock remains legacy feedback.
    private static readonly ElementalVfxProfile TideStance = Water with { CardId="fusion_tide_stance" };
    // Actual direct Feast contacts only; healing and nested source events stay separate.
    private static readonly ElementalVfxProfile Feast = Dark with { CardId="black_feast",TargetSpacing=.16f };
    // Actual retrieval and Scry/ResolveScry are independent command/rider
    // events, never extra outgoing hits or a stolen-heal return connection.
    private static readonly ElementalVfxProfile CorpseCollect = Dark with { CardId="black_corpse_collect" };
    private static readonly ElementalVfxProfile WaveReadStrike = Water with { CardId="blue_wave_read_strike" };
    internal static ElementalVfxProfile? ForCard(string? id)=>id switch
    { "black_corpse_collect"=>CorpseCollect,"blue_wave_read_strike"=>WaveReadStrike,"black_feast"=>Feast,"fusion_tide_stance"=>TideStance,"blue_ice_hail"=>IceHail,"white_light_lance"=>LightLance,"white_light_torrent"=>LightTorrent,"blue_storm_lash"=>StormLash,"blue_tsunami"=>Tsunami,"blue_tide_surge"=>TideSurge,"blue_catalyst_echo"=>CatalystEcho,"white_light_dart"=>LightDart,"white_light_volley"=>LightVolley,"blue_rolling_wave"=>RollingWave,"green_earth_roar"=>EarthRoar,"red_strike"=>Fire,"red_spreading_fire"=>SpreadingFire,"red_double_tap"=>DoubleTap,"blue_strike"=>Water,"blue_ice_lance"=>Ice,"blue_tide_drop"=>TideDrop,"blue_rapid_strike"=>RapidStrike,
      "blue_current_lash"=>CurrentLash,"blue_frost_thorn"=>FrostThorn,"white_strike"=>Light,"white_shield_strike"=>ShieldStrike,
      "white_light_glint"=>LightGlint,"white_light_bolt"=>LightBolt,"white_mode_crossroad"=>ModeCrossroad,"black_drain"=>Dark,"black_big_drain"=>BigDrain,"black_blood_thirst"=>BloodThirst,"black_wraith_feast"=>WraithFeast,"black_corpse_kiss"=>CorpseKiss,"white_light_spray"=>LightSpray,_=>null };
    internal static ElementalVfxProfile? ForRoute(string? key)=>
        key!=null && key.StartsWith("card:",System.StringComparison.Ordinal)?ForCard(key.Substring(5)):null;
}
}

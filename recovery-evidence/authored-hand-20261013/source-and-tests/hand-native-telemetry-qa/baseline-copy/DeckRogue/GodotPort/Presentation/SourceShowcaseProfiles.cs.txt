#nullable enable
namespace DeckRogue.GodotPort.Presentation;

/// <summary>Reconstructed presentation data, not recovered original Godot code.
/// Source: Unity Assets/Game/CardVfxProfile.cs:10-40. No gameplay eligibility lives here.</summary>
public sealed record SourceShowcaseProfile(
    float Contact, float Windup, float Interval, float Recovery, float Residue,
    float Reach, float ImpactPause, float ChainPause, float FinalPause,
    bool Heavy = false, string? AuthoredAttack = null)
{
    public float ImpactPauseFor(int index, int total, bool finishing = false) =>
        SourceShowcaseProfiles.ImpactPauseFor(this, index, total, finishing);

    // Adapter for a stage which already decided whether this receipt belongs to a chain.
    // The kernel never derives that decision from an engine event or a card identifier.
    public float ImpactPauseFor(bool finishing, bool multihit, int index, int total) =>
        SourceShowcaseProfiles.ImpactPauseFor(this, index, multihit ? total : 1, finishing);
}

public static class SourceShowcaseProfiles
{
    public static readonly SourceShowcaseProfile Slash = new(.13f, .045f, .12f, .14f, .36f, .48f, .065f, .025f, .095f, false, "slash");
    public static readonly SourceShowcaseProfile Vine = new(.16f, .065f, .12f, .16f, .44f, .42f, .065f, .025f, .095f);
    public static readonly SourceShowcaseProfile Water = new(.24f, .14f, .12f, .18f, .48f, .10f, .075f, .025f, .100f, false, "water");
    public static readonly SourceShowcaseProfile Earth = new(.30f, .19f, .15f, .22f, .58f, .10f, .110f, .025f, .110f);
    public static readonly SourceShowcaseProfile Stomp = new(.34f, .22f, .16f, .24f, .65f, .62f, .120f, .030f, .120f, true, "stomp");

    // Exact identifier lookup only. The stage must check showcase opt-in and direct ownership.
    public static SourceShowcaseProfile? ForCard(string? id) => id switch
    {
        "green_strike" => Slash,
        "green_double_lash" => Vine,
        "blue_strike" => Water,
        "green_earth_roar" => Earth,
        "green_finisher_stomp" => Stomp,
        _ => null
    };

    public static float ImpactPauseFor(SourceShowcaseProfile? profile, int index, int total, bool finishing = false) =>
        profile is null ? 0f : finishing ? .14f : total > 1
            ? index < total - 1 ? profile.ChainPause : profile.FinalPause
            : profile.ImpactPause;
}

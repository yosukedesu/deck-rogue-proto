// Source-backed presentation policy only. No state, rules or RNG are changed.
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;

namespace DeckRogue.GodotPort.UI;

public static class SourceCardModalPolicy
{
    public static CardInstance Preview(CardInstance original, bool upgraded) =>
        upgraded && Upgrade.CanUpgradeCard(original) ? Upgrade.UpgradeCard(original) : original;
    public static bool NeedsGearCardPicker(GearDef def, int available) => def.NeedsCard != null && available > 0;
    public static bool RequiresFleeConfirmation(GearDef def) => def.Special == "flee";
    public static string UpgradeState(CardInstance card) => Upgrade.IsUpgraded(card) ? "鍛え済み" : "鍛えられない札";
    public static string CornerMark(string badge) => badge switch {
        "素材 A" => "A", "素材 B" => "B", "選択中" => "✓",
        "捨てる (選択中)" => "✓", "残す (選択中)" => "✓", "★ レシピの素材" => "★", _ => ""
    };
}

/// <summary>A UI confirmation belongs to one session revision and can commit only once.</summary>
public sealed class SourceUiDecision
{
    private readonly long _revision;
    public bool Accepted { get; private set; }
    public SourceUiDecision(long revision) { _revision = revision; }
    public bool TryAccept(long revision, bool blocked)
    {
        if (Accepted || blocked || revision != _revision) return false;
        Accepted = true;
        return true;
    }
}

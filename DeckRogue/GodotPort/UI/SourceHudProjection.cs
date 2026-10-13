// Presentation policy reconstruction from verified Stage41 source, not engine replay.
#nullable enable
using System.Linq;
using DeckRogue.Engine.Generated;

namespace DeckRogue.GodotPort.UI;

/// <summary>Read-only composition of the already accepted command and presented resources.</summary>
public static class SourceHudProjection
{
    /// <summary>
    /// Ordinary commands rebuild immediately in Stage41 GameRoot.Do. Enemy status,
    /// interrupt bookkeeping and intent therefore come from the accepted current command;
    /// only HP/max HP/block are receipt-overlaid (PresenterNumeric.NumericDisplayState).
    /// The existing Godot actor population/permanent presentation owner stays authoritative
    /// for visible seats and permanents. This method intentionally preserves that adapter.
    /// Fenced enemy/terminal transitions MUST keep the already captured board instead of
    /// calling this with a future command. No status deltas are inferred from event amounts.
    /// </summary>
    public static RunState Ordinary(RunState current, GameState? display)
    {
        if (current.Combat is not { } now || display is null) return current;
        var player = now.Player with
        {
            Hp = display.Player.Hp,
            MaxHp = display.Player.MaxHp,
            Block = display.Player.Block,
            IceBlock = display.Player.IceBlock,
            Permanents = display.Player.Permanents
        };
        // Index + ID is deliberate: do not find by definition ID across seats. Two
        // identical enemies can coexist. A replaced/new seat awaits its population owner.
        var enemies = display.Enemies.Select((enemy, index) =>
            index < now.Enemies.Count && now.Enemies[index].EnemyId == enemy.EnemyId
                ? now.Enemies[index] with { Hp = enemy.Hp, MaxHp = enemy.MaxHp, Block = enemy.Block }
                : enemy).ToList();
        return current with { Combat = now with { Player = player, Enemies = enemies } };
    }
}

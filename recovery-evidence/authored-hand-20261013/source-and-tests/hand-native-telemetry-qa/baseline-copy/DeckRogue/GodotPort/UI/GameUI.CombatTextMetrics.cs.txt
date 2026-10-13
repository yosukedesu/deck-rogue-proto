#nullable enable
namespace DeckRogue.GodotPort.UI;
public partial class GameUI
{
    // Direct managed presentation query; does not require native Godot dispatch.
    public (float Unit, float TextScale) SourceCombatTextMetrics => (BattleUnit, _textScale);
}

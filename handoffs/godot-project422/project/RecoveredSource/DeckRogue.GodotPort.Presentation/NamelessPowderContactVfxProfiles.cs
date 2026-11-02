using DeckRogue.Game;
using DeckRogue.Engine.Generated;
namespace DeckRogue.GodotPort.Presentation;
// Exact real held Nameless -> explicitly chosen Powder, not direct Powder or arbitrary recursion.
internal static class NamelessPowderContactVfxProfiles
{
    private static bool Tuple(AttackRoute? r,bool selected)=>r!=null && r.Key=="gear:gear_powder"
        && r.DefinitionId=="gear_powder" && r.SourceKind=="gear" && r.Family=="fire_localburst"
        && r.Renderer==(selected?"Hd2dNamelessPowderContactFx":"FireContinuousFx") && r.Element=="fire" && r.FixedSmall;
    internal static bool OriginalRoute(AttackRoute? r)=>Tuple(r,false);
    internal static bool SelectedRoute(AttackRoute? r)=>Tuple(r,true);
    internal static AttackRoute Select(AttackRoute r,GameEvent_DamageDealt hit,bool owned)
    {
        var receipt=CombatPresentationLedger.Read(hit);
        if(!Hd2dAttackPrototype.Enabled || !OriginalRoute(r) || !owned || hit.Source!="player" || hit.HpLoss<=0
            || hit.PresentationSource is not {Kind:"gear",DefinitionId:"gear_powder"} source
            || !string.IsNullOrEmpty(source.Uid) || receipt==null || receipt.Target!="enemy"
            || receipt.EnemyIndex!=hit.EnemyIndex || !CombatPresentationLedger.Valid(hit,receipt,out _))return r;
        return r with {Renderer="Hd2dNamelessPowderContactFx",PortStatus="exact Nameless UseGear previous instance/index, chosen Powder and ordered outer/inner GearUsed; UID-less contact"};
    }
}

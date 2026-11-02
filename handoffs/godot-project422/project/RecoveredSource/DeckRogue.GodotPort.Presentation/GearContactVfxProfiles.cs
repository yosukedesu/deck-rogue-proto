using DeckRogue.Game;
using DeckRogue.Engine.Generated;
namespace DeckRogue.GodotPort.Presentation;
// Direct use of one real held Powder instance only. Presentation source has no UID.
internal static class GearContactVfxProfiles
{
    private static bool Tuple(AttackRoute? r,bool selected)=>r!=null && r.Key=="gear:gear_powder"
        && r.DefinitionId=="gear_powder" && r.SourceKind=="gear" && r.Family=="fire_localburst"
        && r.Renderer==(selected?"Hd2dGearContactFx":"FireContinuousFx") && r.Element=="fire" && r.FixedSmall;
    internal static bool OriginalRoute(AttackRoute? r)=>Tuple(r,false);
    internal static bool SelectedRoute(AttackRoute? r)=>Tuple(r,true);
    internal static AttackRoute Select(AttackRoute r,GameEvent_DamageDealt hit,bool owned)
    {
        var receipt=CombatPresentationLedger.Read(hit);
        if(!Hd2dAttackPrototype.Enabled || !OriginalRoute(r) || !owned || hit.Source!="player" || hit.HpLoss<=0
            || hit.PresentationSource is not {Kind:"gear",DefinitionId:"gear_powder"} source
            || !string.IsNullOrEmpty(source.Uid) || receipt==null || receipt.Target!="enemy"
            || receipt.EnemyIndex!=hit.EnemyIndex || !CombatPresentationLedger.Valid(hit,receipt,out _))return r;
        return r with {Renderer="Hd2dGearContactFx",PortStatus="exact direct UseGear previous instance/index and consumed gear; UID-less receiving contact"};
    }
}

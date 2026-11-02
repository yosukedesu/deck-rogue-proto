using DeckRogue.Engine.Generated;
using DeckRogue.Game;
namespace DeckRogue.GodotPort.Presentation;

// Positive actual status receipt only: no damage-event/card-source substitution.
internal static class StatusContactVfxProfiles
{
    internal static bool SelectedRoute(AttackRoute route)=>route.SourceKind=="system"
        && route.Key=="system:burn" && route.Family=="burn" && route.Renderer=="Hd2dBurnStatusFx";
    internal static AttackRoute Select(AttackRoute route,GameEvent actual,bool ownedEvent)
    {
        if(!Hd2dAttackPrototype.Enabled || !ownedEvent || route.SourceKind!="system" || route.Key!="system:burn"
            || route.Family!="burn" || route.Element!="fire" || route.Renderer!="SystemOutcomeFx" || !route.FixedSmall
            || actual is not GameEvent_BurnTick { Amount: >0 } burn
            || CombatOutcome.PositiveSystemFamily(actual,out _)!="burn")return route;
        var receipt=CombatPresentationLedger.Read(actual);
        if(receipt==null || receipt.Target!="enemy" || receipt.EnemyIndex!=burn.EnemyIndex)return route;
        return route with { Renderer="Hd2dBurnStatusFx",PortStatus="validated actual BurnTick; local thermal status pulse" };
    }
}

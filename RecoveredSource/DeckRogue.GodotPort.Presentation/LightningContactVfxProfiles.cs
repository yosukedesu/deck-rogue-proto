using DeckRogue.Engine.Generated;
using DeckRogue.Game;
namespace DeckRogue.GodotPort.Presentation;

// Context-specific adapter, deliberately NOT an Elemental.ForCard admission.
// The same card key is neutral for other actual leaders.
internal static class LightningContactVfxProfiles
{
    internal static bool SelectedRoute(AttackRoute route)=>route.SourceKind=="card"
        && route.Key=="card:blue_aether_burst" && route.DefinitionId=="blue_aether_burst"
        && route.Family=="lightning_discharge" && route.Element=="lightning" && !route.FixedSmall
        && route.Renderer=="Hd2dLightningContactFx";
    internal static AttackRoute Select(AttackRoute route,GameEvent_DamageDealt hit,bool actualIzzetCardOwner)
    {
        if(!Hd2dAttackPrototype.Enabled || !actualIzzetCardOwner || hit.Source!="player" || hit.HpLoss<=0
            || hit.PresentationSource is not {Kind:"card",DefinitionId:"blue_aether_burst"} source
            || string.IsNullOrEmpty(source.Uid) || route.SourceKind!="card"
            || route.Key!="card:blue_aether_burst" || route.DefinitionId!="blue_aether_burst"
            || route.Family!="lightning_discharge" || route.Renderer!="LightningDischargeFx"
            || route.Element!="lightning" || route.FixedSmall)return route;
        var receipt=CombatPresentationLedger.Read(hit);
        if(receipt==null || receipt.Target!="enemy" || receipt.EnemyIndex!=hit.EnemyIndex
            || !CombatPresentationLedger.Valid(hit,receipt,out _))return route;
        return route with {Renderer="Hd2dLightningContactFx",PortStatus="actual Izzet owning card UID and resolved Lightning tuple; original motion/timing"};
    }
}

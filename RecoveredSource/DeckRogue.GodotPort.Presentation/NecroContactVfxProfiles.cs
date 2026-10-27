using DeckRogue.Engine.Generated;
using DeckRogue.Game;
namespace DeckRogue.GodotPort.Presentation;

// Narrow real censer deck-exhaustion case. SourceUid is the ambient permanent;
// PresentationSource.Uid is the exhausted effect card. They MUST NOT be conflated.
internal static class NecroContactVfxProfiles
{
    internal static bool SelectedRoute(AttackRoute route)=>route.SourceKind=="necro"
        && route.Key=="card:black_corpse_kiss" && route.DefinitionId=="black_corpse_kiss"
        && route.Family=="dark_drain" && route.Renderer=="Hd2dNecroContactFx";
    internal static bool OwnsCenserDeckExhaustion(GameEvent_DamageDealt hit,bool instanceMoved,bool ambientCenser,bool orderedEvents)
    {
        var source=hit.PresentationSource;var receipt=CombatPresentationLedger.Read(hit);
        return Hd2dAttackPrototype.Enabled && instanceMoved && ambientCenser && orderedEvents
            && hit.Source=="player" && source is { Kind:"necro",DefinitionId:"black_corpse_kiss" }
            && !string.IsNullOrEmpty(source.Uid) && !string.IsNullOrEmpty(hit.SourceUid)
            && source.Uid!=hit.SourceUid && receipt!=null && receipt.Target=="enemy"
            && receipt.EnemyIndex==hit.EnemyIndex && CombatPresentationLedger.Valid(hit,receipt,out _);
    }
    internal static AttackRoute Select(AttackRoute route,GameEvent_DamageDealt hit,bool ownedInstance)
    {
        if(!ownedInstance || hit.HpLoss<=0 || route.SourceKind!="necro" || route.Key!="card:black_corpse_kiss"
            || route.DefinitionId!="black_corpse_kiss" || route.Family!="dark_drain"
            || route.Renderer!="DarkDrainFx" || !route.FixedSmall)return route;
        return route with { Renderer="Hd2dNecroContactFx",PortStatus="validated censer deck-exhausted card UID; target-local dark necro contact" };
    }
}

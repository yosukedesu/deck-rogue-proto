using DeckRogue.Engine.Generated;
using DeckRogue.Game;
namespace DeckRogue.GodotPort.Presentation;

// Explicit censer deck-exhaustion definitions. SourceUid is the ambient permanent;
// PresentationSource.Uid is the exhausted effect card. They MUST NOT be conflated.
internal static class NecroContactVfxProfiles
{
    internal static int ExpectedDrainAmount(string? definition)=>definition switch {
        "black_corpse_kiss"=>3, "black_groaning_skull"=>4, _=>-1 };
    internal static bool OriginalRoute(AttackRoute? route)=>route!=null
        && route.SourceKind=="necro" && ExpectedDrainAmount(route.DefinitionId)>0
        && route.Key=="card:"+route.DefinitionId && route.Family=="dark_drain"
        && route.Renderer=="DarkDrainFx" && route.Element=="dark" && route.FixedSmall;
    internal static bool SelectedRoute(AttackRoute route)=>route.SourceKind=="necro"
        && ExpectedDrainAmount(route.DefinitionId)>0 && route.Key=="card:"+route.DefinitionId
        && route.Family=="dark_drain" && route.Renderer=="Hd2dNecroContactFx"
        && route.Element=="dark" && route.FixedSmall;
    internal static bool CorpseKissRoute(AttackRoute route)=>SelectedRoute(route) && route.DefinitionId=="black_corpse_kiss";
    internal static bool OwnsCenserDeckExhaustion(GameEvent_DamageDealt hit,bool instanceMoved,bool ambientCenser,bool orderedEvents)
    {
        var source=hit.PresentationSource;var receipt=CombatPresentationLedger.Read(hit);
        return Hd2dAttackPrototype.Enabled && instanceMoved && ambientCenser && orderedEvents
            && hit.Source=="player" && source is { Kind:"necro" } && ExpectedDrainAmount(source.DefinitionId)>0
            && !string.IsNullOrEmpty(source.Uid) && !string.IsNullOrEmpty(hit.SourceUid)
            && source.Uid!=hit.SourceUid && receipt!=null && receipt.Target=="enemy"
            && receipt.EnemyIndex==hit.EnemyIndex && CombatPresentationLedger.Valid(hit,receipt,out _);
    }
    internal static AttackRoute Select(AttackRoute route,GameEvent_DamageDealt hit,bool ownedInstance)
    {
        if(!ownedInstance || hit.HpLoss<=0 || !OriginalRoute(route)
            || hit.PresentationSource is not {Kind:"necro"} source
            || source.DefinitionId!=route.DefinitionId)return route;
        return route with { Renderer="Hd2dNecroContactFx",PortStatus="validated censer deck-exhausted card UID; target-local dark necro contact" };
    }
}

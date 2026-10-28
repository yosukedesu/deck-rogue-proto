using DeckRogue.Engine.Generated;
using DeckRogue.Game;
namespace DeckRogue.GodotPort.Presentation;

// Explicitly selected, confirmed armed post-attack reaction instances. This is not
// a passive source, ordinary PlayCard, auto reaction or generic reaction opt-in.
internal static class ReactionContactVfxProfiles
{
    internal static bool SelectedDefinition(string? definition)=>definition is
        "black_reaction_curse" or "black_underworld_hand" or "black_reaction_grudge" or "black_reaction_last_stand";
    internal static bool LastStandRoute(AttackRoute route)=>SelectedRoute(route) && route.DefinitionId=="black_reaction_last_stand";
    internal static bool CurseRoute(AttackRoute route)=>SelectedRoute(route) && route.DefinitionId=="black_reaction_curse";
    internal static bool OriginalRoute(AttackRoute? route)=>route!=null
        && route.SourceKind=="reaction" && SelectedDefinition(route.DefinitionId)
        && route.Key=="card:"+route.DefinitionId && route.Family=="dark_drain"
        && route.Renderer=="DarkDrainFx" && route.Element=="dark" && route.FixedSmall;
    internal static bool SelectedRoute(AttackRoute route)=>route.SourceKind=="reaction"
        && SelectedDefinition(route.DefinitionId) && route.Key=="card:"+route.DefinitionId
        && route.Family=="dark_drain" && route.Renderer=="Hd2dReactionContactFx"
        && route.Element=="dark" && route.FixedSmall;
    internal static bool OwnsConfirmedInstance(GameEvent_DamageDealt hit,bool confirmedInstance)
    {
        var source=hit.PresentationSource;var receipt=CombatPresentationLedger.Read(hit);
        return Hd2dAttackPrototype.Enabled && confirmedInstance && hit.Source=="player"
            && source is {Kind:"reaction"} && SelectedDefinition(source.DefinitionId)
            && !string.IsNullOrEmpty(source.Uid) && receipt!=null && receipt.Target=="enemy"
            && receipt.EnemyIndex==hit.EnemyIndex && CombatPresentationLedger.Valid(hit,receipt,out _);
    }
    internal static AttackRoute Select(AttackRoute route,GameEvent_DamageDealt hit,bool owned)
    {
        if(!OriginalRoute(route) || hit.PresentationSource?.DefinitionId!=route.DefinitionId
            || hit.HpLoss<=0 || !OwnsConfirmedInstance(hit,owned))return route;
        return route with {Renderer="Hd2dReactionContactFx",PortStatus="validated explicitly confirmed armed reaction UID; local Dark contact, existing counter pose preserved"};
    }
}

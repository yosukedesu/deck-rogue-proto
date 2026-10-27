using DeckRogue.Engine.Generated;
using DeckRogue.Game;
namespace DeckRogue.GodotPort.Presentation;

// One explicitly confirmed, armed post-attack reaction instance. This is not
// a passive source, ordinary PlayCard, auto reaction or generic reaction opt-in.
internal static class ReactionContactVfxProfiles
{
    internal static bool OriginalRoute(AttackRoute? route)=>route!=null
        && route.SourceKind=="reaction" && route.Key=="card:black_reaction_curse"
        && route.DefinitionId=="black_reaction_curse" && route.Family=="dark_drain"
        && route.Renderer=="DarkDrainFx" && route.Element=="dark" && route.FixedSmall;
    internal static bool SelectedRoute(AttackRoute route)=>route.SourceKind=="reaction"
        && route.Key=="card:black_reaction_curse" && route.DefinitionId=="black_reaction_curse"
        && route.Family=="dark_drain" && route.Renderer=="Hd2dReactionContactFx"
        && route.Element=="dark" && route.FixedSmall;
    internal static bool OwnsConfirmedInstance(GameEvent_DamageDealt hit,bool confirmedInstance)
    {
        var source=hit.PresentationSource;var receipt=CombatPresentationLedger.Read(hit);
        return Hd2dAttackPrototype.Enabled && confirmedInstance && hit.Source=="player"
            && source is {Kind:"reaction",DefinitionId:"black_reaction_curse"}
            && !string.IsNullOrEmpty(source.Uid) && receipt!=null && receipt.Target=="enemy"
            && receipt.EnemyIndex==hit.EnemyIndex && CombatPresentationLedger.Valid(hit,receipt,out _);
    }
    internal static AttackRoute Select(AttackRoute route,GameEvent_DamageDealt hit,bool owned)
    {
        if(!OriginalRoute(route) || hit.HpLoss<=0 || !OwnsConfirmedInstance(hit,owned))return route;
        return route with {Renderer="Hd2dReactionContactFx",PortStatus="validated explicitly confirmed armed reaction UID; local Dark contact, existing counter pose preserved"};
    }
}

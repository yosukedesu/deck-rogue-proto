using DeckRogue.Engine.Generated;
using DeckRogue.Game;
namespace DeckRogue.GodotPort.Presentation;

// Explicit non-retainer installed neutral sources only. Shared material does not
// admit every permanent or borrow the enclosing card's cast/weapon ownership.
internal static class PermanentContactVfxProfiles
{
    internal static bool SelectedDefinition(string? id)=>id is "black_blood_candle"
        or "black_perm_pain_core" or "black_perm_bone_mill" or "black_perm_chorus" or "black_crimson_feast"
        or "white_perm_banner" or "fusion_raging_chief" or "fusion_red_wrath_mail" or "fusion_ringed_bark";
    internal static bool OriginalRoute(AttackRoute? route)=>route!=null && route.SourceKind=="permanent"
        && SelectedDefinition(route.DefinitionId) && route.Key=="card:"+route.DefinitionId
        && route.Family=="minimal_neutral_contact" && route.Renderer=="BoundedContactFx"
        && route.Element=="neutral" && route.FixedSmall;
    internal static bool SelectedRoute(AttackRoute route)=>route.SourceKind=="permanent"
        && SelectedDefinition(route.DefinitionId) && route.Key=="card:"+route.DefinitionId
        && route.Family=="minimal_neutral_contact" && route.Renderer=="Hd2dPermanentContactFx"
        && route.Element=="neutral" && route.FixedSmall;
    internal static bool OwnsInstalledNeutral(GameEvent_DamageDealt hit,bool installedOwner)
    {
        var source=hit.PresentationSource;var receipt=CombatPresentationLedger.Read(hit);
        return Hd2dAttackPrototype.Enabled && installedOwner && hit.Source=="player"
            && source is {Kind:"permanent"} && SelectedDefinition(source.DefinitionId)
            && !string.IsNullOrEmpty(source.Uid) && hit.SourceUid==source.Uid
            && receipt!=null && receipt.Target=="enemy" && receipt.EnemyIndex==hit.EnemyIndex
            && CombatPresentationLedger.Valid(hit,receipt,out _);
    }
    // Preserve this historical narrow helper's semantics for versioned QA.
    internal static bool OwnsInstalledCandle(GameEvent_DamageDealt hit,bool installedOwner)
        =>hit.PresentationSource?.DefinitionId=="black_blood_candle" && OwnsInstalledNeutral(hit,installedOwner);
    internal static AttackRoute Select(AttackRoute route,GameEvent_DamageDealt hit,bool installedOwner)
    {
        if(!OriginalRoute(route) || !OwnsInstalledNeutral(hit,installedOwner) || hit.HpLoss<=0
            || hit.PresentationSource?.DefinitionId!=route.DefinitionId)return route;
        return route with {Renderer="Hd2dPermanentContactFx",PortStatus="validated exact installed neutral permanent UID; target-local trigger contact"};
    }
}

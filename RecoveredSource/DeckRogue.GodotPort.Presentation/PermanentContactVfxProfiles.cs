using DeckRogue.Engine.Generated;
using DeckRogue.Game;
namespace DeckRogue.GodotPort.Presentation;

// Explicit non-retainer installed sources. Each definition owns one exact
// source-kind/key/material tuple; sharing an owner gate is not a generic route.
internal static class PermanentContactVfxProfiles
{
    internal static bool NeutralDefinition(string? id)=>id is "black_blood_candle"
        or "black_perm_pain_core" or "black_perm_bone_mill" or "black_perm_chorus" or "black_crimson_feast"
        or "white_perm_banner" or "fusion_raging_chief" or "fusion_red_wrath_mail" or "fusion_ringed_bark";
    internal static bool FireDefinition(string? id)=>id is "red_perm_runfire" or "red_perm_thorn_flame" or "red_perm_cascade_sparks";
    internal static bool DarkDefinition(string? id)=>id is "black_perm_tithe" or "black_perm_seedbed";
    internal static bool PlantDefinition(string? id)=>id is "green_perm_thorn_leaves" or "green_perm_thorn_canopy" or "fusion_bramble_fence";
    internal static bool LightningDefinition(string? id)=>id=="blue_perm_static";
    internal static bool SelectedDefinition(string? id)=>NeutralDefinition(id)||FireDefinition(id)||DarkDefinition(id)||PlantDefinition(id)||LightningDefinition(id);
    private static bool RouteTuple(AttackRoute? route,bool selected)
    {
        if(route==null || route.SourceKind!="permanent" || !route.FixedSmall || !SelectedDefinition(route.DefinitionId))return false;
        string prefix=route.DefinitionId=="red_perm_cascade_sparks"?"permanent:":"card:";
        if(route.Key!=prefix+route.DefinitionId)return false;
        string family,renderer,element;
        if(NeutralDefinition(route.DefinitionId)){family="minimal_neutral_contact";renderer="BoundedContactFx";element="neutral";}
        else if(FireDefinition(route.DefinitionId)){family="fire_localburst";renderer="FireContinuousFx";element="fire";}
        else if(DarkDefinition(route.DefinitionId)){family="dark_drain";renderer="DarkDrainFx";element="dark";}
        else if(PlantDefinition(route.DefinitionId)){family="plant_point_contact";renderer="BoundedContactFx";element="vine";}
        else {family="lightning_discharge";renderer="LightningDischargeFx";element="lightning";}
        return route.Family==family && route.Element==element
            && route.Renderer==(selected?"Hd2dPermanentContactFx":renderer);
    }
    internal static bool ThornFlameRoute(AttackRoute route)=>SelectedRoute(route) && route.DefinitionId=="red_perm_thorn_flame";
    internal static bool OriginalRoute(AttackRoute? route)=>RouteTuple(route,false);
    internal static bool SelectedRoute(AttackRoute route)=>RouteTuple(route,true);
    internal static bool OwnsInstalled(GameEvent_DamageDealt hit,bool installedOwner)
    {
        var source=hit.PresentationSource;var receipt=CombatPresentationLedger.Read(hit);
        return Hd2dAttackPrototype.Enabled && installedOwner && hit.Source=="player"
            && source is {Kind:"permanent"} && SelectedDefinition(source.DefinitionId)
            && !string.IsNullOrEmpty(source.Uid) && hit.SourceUid==source.Uid
            && receipt!=null && receipt.Target=="enemy" && receipt.EnemyIndex==hit.EnemyIndex
            && CombatPresentationLedger.Valid(hit,receipt,out _);
    }
    // Keep historical neutral/Candle diagnostics narrow after material expansion.
    internal static bool OwnsInstalledNeutral(GameEvent_DamageDealt hit,bool installedOwner)
        =>NeutralDefinition(hit.PresentationSource?.DefinitionId) && OwnsInstalled(hit,installedOwner);
    // Preserve this historical narrow helper's semantics for versioned QA.
    internal static bool OwnsInstalledCandle(GameEvent_DamageDealt hit,bool installedOwner)
        =>hit.PresentationSource?.DefinitionId=="black_blood_candle" && OwnsInstalledNeutral(hit,installedOwner);
    internal static AttackRoute Select(AttackRoute route,GameEvent_DamageDealt hit,bool installedOwner)
    {
        if(!OriginalRoute(route) || !OwnsInstalled(hit,installedOwner) || hit.HpLoss<=0
            || hit.PresentationSource?.DefinitionId!=route.DefinitionId)return route;
        return route with {Renderer="Hd2dPermanentContactFx",PortStatus=NeutralDefinition(route.DefinitionId)?"validated exact installed neutral permanent UID; target-local trigger contact":"validated exact installed permanent UID and material tuple; target-local trigger contact"};
    }
}

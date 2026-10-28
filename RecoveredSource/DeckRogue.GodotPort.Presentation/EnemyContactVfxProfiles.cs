using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine.Generated;
using DeckRogue.Game;

namespace DeckRogue.GodotPort.Presentation;

// Exact actor + move pairs. This refines local receiving contact only; each
// enemy retains its actual authored action, timing and gameplay contribution.
internal static class EnemyContactVfxProfiles
{
    private static bool SelectedPair(string? actor,string? move,string family)
    {
        if(family=="minimal_neutral_contact")return (actor,move) switch {
            ("enemy_wide_power","surge") or ("enemy_thief","mug")
            or ("enemy_crab_crusher","thrash") or ("enemy_devoted_sculptor","savage")
            or ("enemy_forgotten_soul","dread")
            or ("enemy_forgotten_soul","miasma")
            or ("enemy_devoted_sculptor","carve_rite")
            or ("enemy_lost_soul","debilitating_smog")
            or ("enemy_hexer","mud")
            or ("enemy_mud_lump","slop")
            or ("enemy_spore_cap","frail_spores")
            or ("enemy_strangler_serpent","constrict")
            or ("enemy_sludge_spider","oil_spray")
            or ("enemy_apprentice_colossus","swing")
            or ("enemy_slug","lick")
            or ("enemy_slug","acid_spit")
            or ("enemy_cog_construct","repeater")
            or ("enemy_iron_clam","jet")
            or ("enemy_frog_knight","tongue_lash")
            or ("enemy_frog_knight","strike_down")
            or ("enemy_warden","bell_hushed")
            or ("enemy_elite_sergeant","whip")
            or ("enemy_elite_slaver","chain")
            or ("enemy_brood_toad","tongue")
            or ("enemy_elite_owl","wing")
            or ("enemy_axe_ogre","great_swing")
            or ("enemy_warden","bell_strike")
            or ("enemy_warden","gate_guard")
            or ("enemy_strangler_serpent","lash")=>true,_=>false };
        if(family=="enemy_blunt")return (actor,move) switch {
            ("enemy_brute","club") or ("enemy_brute","club_wild")
            or ("enemy_set_breaker","smash") or ("enemy_moss","slam")
            or ("enemy_big_slime","slam") or ("enemy_mudling","bump")
            or ("enemy_elite_gold_raven","dive")
            or ("enemy_moss_slime","bump")
            or ("enemy_shield_squire","shield_bash")
            or ("enemy_mourn_beast","leap")
            or ("enemy_bowl_bug","roll")
            or ("enemy_elite_giant_face","crush")
            or ("enemy_drummer","thump")
            or ("enemy_chimera_1","skull_bash")
            or ("enemy_turtle","crush")=>true,_=>false };
        if(family!="pierce_contact")return false;
        return (actor,move) switch {
        ("enemy_probe","poke") or ("enemy_probe","lunge")
        or ("enemy_rock_beetle","horn_jab") or ("enemy_bond_wolf","lunge")
        or ("enemy_elite_gold_raven","peck") or ("enemy_haze_stag","antler_jab")
        or ("enemy_haze_stag","antler_charge")=>true,_=>false };
    }
    private static bool RouteTuple(AttackRoute route,string renderer)
    {
        if(route.SourceKind!="enemy_actual_execution"
            || (route.Family!="pierce_contact" && route.Family!="enemy_blunt" && route.Family!="minimal_neutral_contact")
            || route.Renderer!=renderer || route.Element!=(route.Family=="minimal_neutral_contact"?"neutral":"physical") || route.FixedSmall)return false;
        const string prefix="enemy:";
        if(!route.Key.StartsWith(prefix,System.StringComparison.Ordinal))return false;
        int split=route.Key.IndexOf('/',prefix.Length);
        if(split<=prefix.Length || split==route.Key.Length-1)return false;
        string actor=route.Key.Substring(prefix.Length,split-prefix.Length),move=route.Key.Substring(split+1);
        return route.DefinitionId==move && SelectedPair(actor,move,route.Family);
    }
    internal static bool SelectedRoute(AttackRoute route)=>RouteTuple(route,"Hd2dEnemyContactFx");
    internal static bool ProbePokeRoute(AttackRoute route)=>SelectedRoute(route) && route.Key=="enemy:enemy_probe/poke" && route.DefinitionId=="poke" && route.Family=="pierce_contact";

    internal static AttackRoute Select(AttackRoute route,GameEvent_DamageDealt hit,
        string? actorId,string? provenMove,IReadOnlyList<GameEvent>? actualLog)
    {
        if(!Hd2dAttackPrototype.Enabled || !RouteTuple(route,route.Family=="minimal_neutral_contact"?"BoundedContactFx":route.Family=="enemy_blunt"?"EnemyContactFx":"PlayerContactFx")
            || !SelectedPair(actorId,provenMove,route.Family) || route.Key!="enemy:"+actorId+"/"+provenMove
            || route.DefinitionId!=provenMove || hit.Source!="enemy" || hit.HpLoss<=0)return route;
        var action=hit.PresentationEnemyAction;var multi=hit.PresentationEnemyAttack;
        if(action==null || action.Version!=1 || action.EnemyId!=actorId || action.MoveId!=provenMove
            || action.UnresolvedReason!=null || action.ExecutionEventIndex<0
            || (action.Branch!="primary" && action.Branch!="alternate")
            || multi==null || multi.Version!=1 || multi.EnemyId!=actorId
            || multi.Resolution!="aggregate-final-mutation" || multi.LoopBound!=1 || multi.ExecutedHits!=1
            || multi.Contributions?.Count!=1 || multi.EffectiveActual!=hit.Amount)return route;
        // The owning actual combat log, not a renderer label or move-name
        // collision, proves this execution and source actor.
        bool execution=actualLog!=null && actualLog.Any(e=>e is GameEvent_EnemyActionExecuting x
            && x.EnemyIndex==hit.EnemyIndex && x.Kind=="attack"
            && x.PresentationEnemyAction is {Version:1,UnresolvedReason:null} a
            && a.EnemyId==actorId && a.MoveId==provenMove
            && a.ExecutionEventIndex==action.ExecutionEventIndex && a.Branch==action.Branch);
        var receipt=CombatPresentationLedger.Read(hit);
        if(!execution || receipt==null || receipt.Target!="player"
            || !CombatPresentationLedger.Valid(hit,receipt,out _))return route;
        return route with {Renderer="Hd2dEnemyContactFx",PortStatus="validated exact single-hit enemy actor/move contact; original motion/timing retained"};
    }
}

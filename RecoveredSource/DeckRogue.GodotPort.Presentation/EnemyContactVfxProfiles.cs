using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine.Generated;
using DeckRogue.Game;

namespace DeckRogue.GodotPort.Presentation;

// Separate source-kind gate. Card SupportsRoute is deliberately NOT broadened.
internal static class EnemyContactVfxProfiles
{
    internal static bool SelectedRoute(AttackRoute route)=>route.SourceKind=="enemy_actual_execution"
        && route.Key=="enemy:enemy_probe/poke" && route.DefinitionId=="poke"
        && route.Family=="pierce_contact" && route.Renderer=="Hd2dEnemyContactFx";

    internal static AttackRoute Select(AttackRoute route,GameEvent_DamageDealt hit,
        string? actorId,string? provenMove,IReadOnlyList<GameEvent>? actualLog)
    {
        if(!Hd2dAttackPrototype.Enabled || route.SourceKind!="enemy_actual_execution"
            || route.Key!="enemy:enemy_probe/poke" || route.DefinitionId!="poke"
            || route.Family!="pierce_contact" || route.Renderer!="PlayerContactFx"
            || hit.Source!="enemy" || hit.HpLoss<=0 || actorId!="enemy_probe" || provenMove!="poke")return route;
        var action=hit.PresentationEnemyAction;var multi=hit.PresentationEnemyAttack;
        if(action==null || action.Version!=1 || action.EnemyId!="enemy_probe" || action.MoveId!="poke"
            || action.UnresolvedReason!=null || action.ExecutionEventIndex<0
            || (action.Branch!="primary" && action.Branch!="alternate")
            || multi==null || multi.Version!=1 || multi.EnemyId!="enemy_probe"
            || multi.Resolution!="aggregate-final-mutation" || multi.LoopBound!=1 || multi.ExecutedHits!=1
            || multi.Contributions?.Count!=1 || multi.EffectiveActual!=hit.Amount)return route;
        // A move label alone is insufficient: the owning command's actual combat
        // log must contain its matching execution. Reaction continuations may use
        // earlier real entries in the same combat log, never a stale card owner.
        bool execution=actualLog!=null && actualLog.Any(e=>e is GameEvent_EnemyActionExecuting x
            && x.EnemyIndex==hit.EnemyIndex && x.Kind=="attack"
            && x.PresentationEnemyAction is { Version:1,EnemyId:"enemy_probe",MoveId:"poke",UnresolvedReason:null } a
            && a.ExecutionEventIndex==action.ExecutionEventIndex && a.Branch==action.Branch);
        var receipt=CombatPresentationLedger.Read(hit);
        if(!execution || receipt==null || receipt.Target!="player"
            || !CombatPresentationLedger.Valid(hit,receipt,out _))return route;
        return route with { Renderer="Hd2dEnemyContactFx",PortStatus="validated actual enemy poke contact; original motion/timing retained" };
    }
}

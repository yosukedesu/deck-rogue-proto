using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine.Generated;

namespace DeckRogue.GodotPort.Presentation;

// Read-only closed-span proof. This never emits, reorders, or advances an event.
// StrengthGained has recipient identity only, so individual events are insufficient.
public sealed record DrummerAppliedOncePlan(
    GameEvent_EnemyActionExecuting Executing, int ExecutionIndex,
    GameEvent_StrengthGained FirstApplied, int AppliedIndex,
    GameEvent_EnemyActionResolved Resolved, int ResolvedIndex,
    int EnemyIndex, int Actual, int RecipientCount)
{
    public static DrummerAppliedOncePlan? TryReserve(GameState? before,
        IReadOnlyList<GameEvent>? log, GameEvent_EnemyActionExecuting executing)
    {
        var meta = executing.PresentationEnemyAction;
        if (before == null || log == null || meta == null || meta.Version != 1 ||
            meta.UnresolvedReason != null || meta.EnemyId != "enemy_drummer" ||
            meta.MoveId != "war_drum" || meta.Branch != "primary" || meta.Origin != "graph" ||
            executing.Kind != "rally" || executing.EnemyIndex < 0 ||
            executing.EnemyIndex >= before.Enemies.Count ||
            before.Enemies[executing.EnemyIndex].EnemyId != "enemy_drummer") return null;

        int start = meta.ExecutionEventIndex;
        if (start < before.EventLog.Count || start >= log.Count ||
            !ReferenceEquals(log[start], executing) ||
            log.Count(e => ReferenceEquals(e, executing)) != 1 ||
            meta.DeclaredEventIndex is not int declaredIndex ||
            declaredIndex < 0 || declaredIndex >= start ||
            log[declaredIndex] is not GameEvent_EnemyIntentDeclared declaration ||
            declaration.EnemyIndex != executing.EnemyIndex || declaration.Intent == null ||
            declaration.Intent.Kind != "rally" || declaration.Intent.Actual is < 1 or > 2 ||
            declaration.PresentationMove is not { Version: 1, EnemyId: "enemy_drummer",
                PrimaryMoveId: "war_drum", Origin: "graph" } declared ||
            declared.EventIndex != declaredIndex) return null;

        // Match the immutable action prefix, then carry only proven life-state
        // changes before this execution. Structural replacement remains ambiguous.
        for (int i = 0; i < before.EventLog.Count; i++)
            if (!ReferenceEquals(before.EventLog[i], log[i])) return null;
        var alive = new SortedSet<int>(Enumerable.Range(0, before.Enemies.Count)
            .Where(i => before.Enemies[i].Hp > 0));
        for (int i = before.EventLog.Count; i < start; i++)
        {
            if (log[i] is GameEvent_EnemyDied died) alive.Remove(died.EnemyIndex);
            else if (log[i] is GameEvent_EnemyFled fled) alive.Remove(fled.EnemyIndex);
            else if (log[i] is GameEvent_EnemySummoned or GameEvent_EnemySplit or
                GameEvent_EnemyHatched or GameEvent_CombatStarted or GameEvent_CombatEnded)
                return null;
        }
        if (!alive.Contains(executing.EnemyIndex)) return null;

        int actual = declaration.Intent.Actual, lastRecipient = -1;
        var seen = new HashSet<int>();
        GameEvent_StrengthGained? first = null;
        int firstIndex = -1;
        for (int i = start + 1; i < log.Count; i++)
        {
            if (log[i] is GameEvent_EnemyActionResolved resolved)
            {
                if (resolved.EnemyIndex != executing.EnemyIndex || resolved.Kind != "rally" ||
                    resolved.Actual != actual || resolved.HpLoss != 0 || first == null ||
                    !seen.SetEquals(alive) || log.Count(e => ReferenceEquals(e, resolved)) != 1)
                    return null;
                return new(executing, start, first, firstIndex, resolved, i,
                    executing.EnemyIndex, actual, seen.Count);
            }
            // No foreign buff, nested execution, phase event, interruption, or
            // other application may share the proof interval.
            if (log[i] is not GameEvent_StrengthGained strength || strength.Reason != null ||
                strength.Amount != actual || strength.EnemyIndex <= lastRecipient ||
                !alive.Contains(strength.EnemyIndex) || !seen.Add(strength.EnemyIndex) ||
                log.Count(e => ReferenceEquals(e, strength)) != 1) return null;
            first ??= strength;
            if (firstIndex < 0) firstIndex = i;
            lastRecipient = strength.EnemyIndex;
        }
        return null;
    }

    public bool IsApplied(GameEvent e, int index) =>
        index == AppliedIndex && ReferenceEquals(e, FirstApplied);
    public bool IsResolved(GameEvent e, int index) =>
        index == ResolvedIndex && ReferenceEquals(e, Resolved);
}

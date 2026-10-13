// Bounded read-only ledger contract. This is not an event queue or combat replay.
#nullable enable
using System;
using DeckRogue.Engine.Generated;

namespace DeckRogue.GodotPort.UI;

/// <summary>
/// Semantic ledger state at one existing board construction boundary. Engine state records
/// are immutable inputs: consumers may read them but must not mutate their collections.
/// Capture once in BuildUxEnemies, after the ordinary/fenced board policy selects its state.
/// </summary>
public sealed class SourceEnemyHudSnapshot
{
    public GameState State { get; }
    /// <summary>Source BattleView evaluates group anyForecast against st, before numeric overlay.</summary>
    public GameState ForecastEligibilityState { get; }
    public long Epoch { get; }
    public long BoardRevision { get; }

    private SourceEnemyHudSnapshot(GameState state, GameState forecastEligibilityState, long epoch, long boardRevision)
    {
        State = state ?? throw new ArgumentNullException(nameof(state));
        ForecastEligibilityState = forecastEligibilityState;
        Epoch = epoch;
        BoardRevision = boardRevision;
    }

    /// <summary>
    /// Keep the whole board: guardian/alone/ally-died use living peers, BranchForms reads
    /// player sets/permanents and HideIntents. Selectively copying status numbers loses
    /// that context. Epoch and boardRevision belong to the existing UI/presentation owner.
    /// </summary>
    public static SourceEnemyHudSnapshot Capture(GameState state, long epoch, long boardRevision)
        => new(state, state, epoch, boardRevision);

    /// <summary>
    /// Select at the existing GameUI BuildUxEnemies boundary. In this adapter inputFenced
    /// is exactly GameUI.IsInputBlocked / BattleStage.InputLocked, not a modal-open flag.
    /// Unity suppresses Rebuild during its old-board fence; Godot may technically rebuild
    /// on Changed or resize, so rewrap the previous semantic board without advancing it.
    /// Completion/cancel-with-reconcile clears the gate before assigning/rebuilding _view.
    /// A reset changes epoch. No separate queue, revision counter or event replay is owned here.
    /// </summary>
    public static SourceEnemyHudSnapshot SelectForBuild(SourceEnemyHudSnapshot? previous,
        GameState selectedBoard, GameState? acceptedCurrentBoard, long epoch, long activeRevision,
        bool inputFenced)
    {
        if (selectedBoard is null) throw new ArgumentNullException(nameof(selectedBoard));
        if (inputFenced && previous is not null && previous.Epoch == epoch
            && SameCombatLineage(previous.State, selectedBoard))
            return new(previous.State, previous.ForecastEligibilityState, epoch, previous.BoardRevision);

        // With no old board under a fence, the explicitly selected board is the only
        // proved context. Never consult the already-resolved future end-turn state.
        var group = !inputFenced && SameCombatLineage(selectedBoard, acceptedCurrentBoard)
            ? acceptedCurrentBoard! : selectedBoard;
        return new(selectedBoard, group, epoch, activeRevision);
    }

    private static bool SameCombatLineage(GameState selected, GameState? accepted)
    {
        if (accepted is null) return false;
        if (ReferenceEquals(selected, accepted)) return true;
        return selected.EventLog.Count > 0 && accepted.EventLog.Count > 0
            && ReferenceEquals(selected.EventLog[0], accepted.EventLog[0]);
    }

    public bool IsCurrent(SourceEnemyHudSnapshot? current, long epoch, long boardRevision)
        => ReferenceEquals(this, current) && Epoch == epoch && BoardRevision == boardRevision;

    /// <summary>
    /// Read only for the exact captured board and seat. A fresh epoch invalidates captures
    /// after reset/load. Capture replacement also invalidates same-revision cancel/reconcile
    /// and resize rebuilds: pass the current UI-owned capture, not this old capture. Never
    /// borrow a record from another seat merely because the definition ID matches.
    /// </summary>
    public bool TryRead(SourceEnemyHudSnapshot? current, long epoch, long boardRevision, int enemyIndex, string enemyId,
        out GameState? state, out EnemyState? enemy)
    {
        state = null;
        enemy = null;
        if (!IsCurrent(current, epoch, boardRevision) || enemyIndex < 0 || enemyIndex >= State.Enemies.Count
            || string.IsNullOrEmpty(enemyId) || State.Enemies[enemyIndex].EnemyId != enemyId) return false;
        state = State;
        enemy = State.Enemies[enemyIndex];
        return true;
    }
}

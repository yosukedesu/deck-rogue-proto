// Source-backed UI projection. No engine state or RNG is changed.
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DeckRogue.Engine;
using DeckRogue.Engine.Generated;
using DeckRogue.GodotPort.Core;
namespace DeckRogue.GodotPort.UI;

/// <summary>The rebuild-owned board and its original revision. A set-only redraw never advances gear/permanent state.</summary>
public sealed record SourceTokenFrame(RunState Board, long Revision, GameState? SetRedraw = null, long Epoch = 0);
public sealed record SourceSetToken(CardInstance Card, bool Live, bool Ready, int? Windows, string State, string Details);
public sealed record SourceGearToken(GearInstance Gear, GearDef Definition, string? Blocked, string Details);
public sealed record SourcePermanentToken(CardInstance Card, bool Doll, int? Life, bool HasGrowth, string Details);
public sealed record SourceTokenData(SourceTokenFrame Frame, int SetSlots, IReadOnlyList<SourceSetToken> Sets,
    IReadOnlyList<SourceGearToken> Gears, IReadOnlyList<SourcePermanentToken> Permanents, string? GearMute, string Stamp);

public static class SourceTokenModel
{
    // Same bounded lifecycle selection as SourceEnemyHudSnapshot.SelectForBuild.
    // Godot technical rebuilds inside Unity's preserved-board fence must not recapture event-advanced _view.
    public static SourceTokenFrame SelectForBuild(SourceTokenFrame? previous, RunState selectedBoard,
        long epoch, long activeRevision, bool inputFenced, SessionChange? acceptedOwner = null, long acceptedRevision = -1)
    {
        if (selectedBoard.Combat == null) throw new ArgumentException("Token board must be combat.");
        if (inputFenced && previous != null && previous.Epoch == epoch && SameCombatLineage(previous.Board.Combat, selectedBoard.Combat))
            return new SourceTokenFrame(previous.Board, previous.Revision, previous.SetRedraw, epoch);
        // Unity NumericDisplayState starts from current semantics and overlays only receipt vitals.
        // In Godot _view.Permanents belongs to the actor population adapter; token UI must be independent.
        if (!inputFenced && acceptedOwner != null && acceptedOwner.Revision == acceptedRevision &&
            SameCombatLineage(selectedBoard.Combat, acceptedOwner.Current.Combat))
            return new SourceTokenFrame(OrdinaryUiBoard(acceptedOwner.Current, selectedBoard.Combat), acceptedOwner.Revision, null, epoch);
        return new SourceTokenFrame(selectedBoard, activeRevision, null, epoch);
    }
    public static RunState OrdinaryUiBoard(RunState accepted, GameState displayed)
    {
        var current = accepted.Combat ?? throw new ArgumentException("Accepted token board must be combat.");
        if (!SameCombatLineage(current, displayed)) return accepted;
        var player = current.Player with { Hp = displayed.Player.Hp, MaxHp = displayed.Player.MaxHp,
            Block = displayed.Player.Block, IceBlock = displayed.Player.IceBlock };
        var enemies = current.Enemies.Select((enemy, index) => index < displayed.Enemies.Count &&
            displayed.Enemies[index].EnemyId == enemy.EnemyId
            ? enemy with { Hp = displayed.Enemies[index].Hp, MaxHp = displayed.Enemies[index].MaxHp, Block = displayed.Enemies[index].Block }
            : enemy).ToArray();
        return accepted with { Combat = current with { Player = player, Enemies = enemies } };
    }
    /// <summary>Equivalent of source ContactCurrent + CombatDisplaySequence.Current for the existing native callback.</summary>
    public static SourceTokenFrame? TryOwnedTurnStart(SourceTokenFrame? currentFrame, SourceTokenFrame? expectedFrame,
        SessionChange? owner, GameEvent evt, long epoch, long activeCommandRevision, long acceptedRevision,
        bool inputFenced, bool ordinaryPresentation, bool receiptAccepted)
    {
        if (currentFrame == null || expectedFrame == null || owner == null || !ReferenceEquals(currentFrame, expectedFrame) ||
            expectedFrame.Epoch != epoch || !inputFenced || ordinaryPresentation || !receiptAccepted || owner.IsReset ||
            owner.Revision != activeCommandRevision || owner.Revision != acceptedRevision || expectedFrame.Revision >= owner.Revision ||
            evt is not GameEvent_TurnStarted started || !owner.Events.Any(e => ReferenceEquals(e, evt)) ||
            owner.Previous?.Combat is not {} previous || owner.Current.Combat is not {} current || started.Turn != current.Turn ||
            expectedFrame.Board.Combat?.Turn != previous.Turn ||
            !current.EventLog.Skip(previous.EventLog.Count).Any(e => ReferenceEquals(e, evt)) ||
            !SameCombatLineage(expectedFrame.Board.Combat, previous) || !SameCombatLineage(previous, current) ||
            ReferenceEquals(expectedFrame.SetRedraw, current)) return null;
        // These verified owner/display conditions establish source Display.Current. No live run/state is queried.
        if (!current.Player.SetCards.Take(Math.Max(0, expectedFrame.Board.Combat!.Player.SetSlots)).Any(c => Effects.TrapAge(current, c) == 1)) return null;
        return expectedFrame with { SetRedraw = current };
    }
    private static bool SameCombatLineage(GameState? previous, GameState? selected) =>
        previous != null && selected != null && (ReferenceEquals(previous, selected) ||
        previous.EventLog.Count > 0 && selected.EventLog.Count > 0 && ReferenceEquals(previous.EventLog[0], selected.EventLog[0]));
    public static string? GearMute(RunState run) => run.Combat is not {} s ? null :
        run.Mana.GetValueOrDefault() < Gears.GEAR_MANA_COST ? "魔素不足" :
        s.GearUsedThisTurn == true ? "今ターン済" : s.PendingScry != null ? "占術中" : null;
    public static bool HasGrowth(CardDef def) => def.Effects.Any(e => e.Amount != null && Effects.DOLL_GROWTH_EFFECTS.Contains(e.Effect));
    // Stage41 Presenter.cs 2300–2329. No numeric-contact or generic event refresh.
    public static bool ShouldRedrawSets(GameEvent evt, GameState current, int existingSlots, bool nudgeHp, bool displayCurrent) =>
        evt is GameEvent_TurnStarted && (nudgeHp || displayCurrent) &&
        current.Player.SetCards.Take(Math.Max(0, existingSlots)).Any(c => Effects.TrapAge(current, c) == 1);
    public static SourceTokenFrame? TrySetRedraw(SourceTokenFrame? currentFrame, SourceTokenFrame expectedFrame,
        GameState state, GameEvent evt, bool nudgeHp, bool displayCurrent)
    {
        if (!ReferenceEquals(currentFrame, expectedFrame) || !SameCombatLineage(expectedFrame.Board.Combat, state) ||
            !ShouldRedrawSets(evt, state, expectedFrame.Board.Combat!.Player.SetSlots, nudgeHp, displayCurrent)) return null;
        return expectedFrame with { SetRedraw = state };
    }
    public static SourceTokenData Capture(SourceTokenFrame frame)
    {
        var run = frame.Board;
        var board = run.Combat ?? throw new ArgumentException("Token frame must own a combat board.");
        var state = frame.SetRedraw ?? board;
        // A selective redraw cannot create slots that were absent from the captured board.
        int slots = Math.Max(0, board.Player.SetSlots);
        var sets = state.Player.SetCards.Take(slots).Select(card => {
            bool live = Effects.IsTrapLive(state, card), ready = live && Effects.TrapCanFireThisPhase(state, card);
            int? windows = Effects.TrapWindowsLeft(state, card);
            string status = !live ? "準備中" : ready ? "鳴る" : windows.HasValue ? "あと" + windows + "回" : "期限なし";
            string details = CardText.Body(card.Def) + "\n" + Effects.TrapStatusTextKarakuri(state, card);
            var damage = Effects.SetCardLiveDamage(state, card.Def);
            if (damage != null) details += "\n" + damage;
            return new SourceSetToken(card, live, ready, windows, status, details);
        }).ToArray();
        var gears = (run.Gears ?? Array.Empty<GearInstance>()).Select(gear => {
            var def = Content.GetGearDef(gear.GearId);
            var blocked = Gears.GearBlockedReason(board, run.Mana.GetValueOrDefault(), gear);
            string details = def.Text + "\n残り " + gear.Charges + " 回";
            var damage = Gears.GearLiveDamage(board, def); var noEffect = Gears.GearNoEffectReason(board, def);
            if (damage != null) details += "\n" + damage;
            if (noEffect != null) details += "\nいま組んでも何も起きない: " + noEffect;
            if (blocked != null) details += "\nいまは組めない: " + blocked;
            return new SourceGearToken(gear, def, blocked, details);
        }).ToArray();
        var perms = board.Player.Permanents.Where(c => c.Innate != true).Select(card => {
            bool doll = Effects.IsDoll(card), growth = doll && HasGrowth(card.Def);
            int? life = doll ? Effects.DollLifeLeft(board, card) : null;
            string details = CardText.Body(card.Def);
            if (doll) details += "\n" + (life.HasValue ? "あと" + life + "ターンで消える" : "期限なし") + (growth ? "・火勢+" + Effects.DollGrowth(board, card) : "");
            return new SourcePermanentToken(card, doll, life, growth, details);
        }).ToArray();
        string? mute = GearMute(run);
        var key = new StringBuilder();
        void Part(object? value) { string text = value?.ToString() ?? ""; key.Append(text.Length).Append(':').Append(text); }
        Part(frame.Revision); Part(frame.Epoch); Part(slots); Part(mute);
        foreach (var t in sets) { Part(t.Card.Uid); Part(t.Card.Def.Id); Part(t.Card.Def.Name); Part(t.State); Part(t.Windows); Part(t.Details); }
        foreach (var t in gears) { Part(t.Gear.Uid); Part(t.Gear.GearId); Part(t.Gear.Charges); Part(t.Details); }
        foreach (var t in perms) { Part(t.Card.Uid); Part(t.Card.Def.Id); Part(t.Card.Def.Name); Part(t.Life); Part(t.Details); }
        return new SourceTokenData(frame, slots, sets, gears, perms, mute, key.ToString());
    }
    public static bool InventoryCurrent(SourceTokenFrame frame, RunState? current, long revision, bool blocked, long? currentEpoch = null)
    {
        if ((currentEpoch.HasValue && currentEpoch.Value != frame.Epoch) || blocked || revision != frame.Revision || current?.Combat == null || frame.Board.Combat == null ||
            current.Mana != frame.Board.Mana) return false;
        var captured = frame.Board.Gears ?? Array.Empty<GearInstance>(); var live = current.Gears ?? Array.Empty<GearInstance>();
        return captured.Count == live.Count && captured.Zip(live).All(p => p.First.Uid == p.Second.Uid &&
            p.First.GearId == p.Second.GearId && p.First.Charges == p.Second.Charges);
    }
    public static bool GearCurrent(SourceTokenFrame frame, RunState? current, long revision, bool blocked, int index, string uid, string id, long? currentEpoch = null) =>
        InventoryCurrent(frame, current, revision, blocked, currentEpoch) && index >= 0 && index < current!.Gears!.Count &&
        current.Gears[index].Uid == uid && current.Gears[index].GearId == id &&
        Gears.GearBlockedReason(current.Combat, current.Mana.GetValueOrDefault(), current.Gears[index]) == null;
    // Deterministic visual phase replaces UnityEngine.Random.value, never engine RNG.
    public static float PulsePhase(string uid) { uint h = 2166136261; foreach (char c in uid) h = (h ^ c) * 16777619; return (h % 10000) / 10000f; }
    public static float PulseAlpha(float progress, float phase) => .75f + .25f * (float)Math.Sin((progress + phase) * Math.PI * 2);
}

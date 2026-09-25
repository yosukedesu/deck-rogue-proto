// State.cs — src/engine/state.ts の手書き移植 (unity/PORTING.md)
// GameState はイミュータブル。ApplyCommand(state, command) => newState の純関数のみで遷移する。
// 方式固有コマンドは IReactionSystem に委譲し、割り込み中断中だった場合は敵フェーズを再開する。
using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Engine
{
    public static class State
    {
        /// <summary>TS: state.ts の `export { createInitialState }` に対応する再エクスポート</summary>
        public static GameState CreateInitialState(int seed, string reactionMode) => Combat.CreateInitialState(seed, reactionMode);

        public static GameState ApplyCommand(GameState state, Command command)
        {
            // 占術の保留 (青 2026-09-25): 捨てる札を選ぶまで他のコマンドを受け付けない (TS と同形)
            if (state.PendingScry != null && !(command is Command_ResolveScry) && !(command is Command_StartCombat))
            {
                throw new InvalidOperationException("占術で捨てる札を選んでから (ResolveScry)");
            }
            return SettleRetainDiscount(SettleScry(ApplyCommandRaw(state, command)));
        }

        /// <summary>潮溜まり (青 2026-09-25): 手札を離れた札 (山札・捨て札・消滅・場・仕込み) のコスト減を落とす = 安くなるのは手札にある間だけ (TS settleRetainDiscount)</summary>
        private static GameState SettleRetainDiscount(GameState state)
        {
            var p = state.Player;
            bool Has(IReadOnlyList<CardInstance> cs) => cs.Any(c => c.RetainDiscount != null);
            if (!(Has(p.DrawPile) || Has(p.DiscardPile) || Has(p.ExhaustPile) || Has(p.Permanents) || Has(p.SetCards))) return state;
            IReadOnlyList<CardInstance> Strip(IReadOnlyList<CardInstance> cs) => Has(cs) ? cs.Select(c => c.RetainDiscount == null ? c : c with { RetainDiscount = null }).ToList() : cs;
            return state with { Player = p with { DrawPile = Strip(p.DrawPile), DiscardPile = Strip(p.DiscardPile), ExhaustPile = Strip(p.ExhaustPile), Permanents = Strip(p.Permanents), SetCards = Strip(p.SetCards) } };
        }

        /// <summary>占術の保留の後始末: 山札が空 (見る札が無い)・決着済みなら保留を落とす (空の窓を出さない。TS settleScry)</summary>
        private static GameState SettleScry(GameState state)
        {
            if (state.PendingScry == null) return state;
            if (state.Player.DrawPile.Count > 0 && state.Phase != CombatPhases.Won && state.Phase != CombatPhases.Lost) return state;
            return state with { PendingScry = null };
        }

        private static GameState ApplyCommandRaw(GameState state, Command command)
        {
            switch (command)
            {
                case Command_ResolveScry rs:
                    return Combat.ResolveScry(state, rs.DiscardUids);
                case Command_StartCombat c:
                    return Combat.StartCombat(c.Seed, state.ReactionMode, c.EnemyId, c.DeckId, c.LeaderId, c.CardIds);
                case Command_PlayCard c:
                    return Combat.PlayCard(state, c.CardUid, c.ModeIndex, c.DiscardUids, c.TargetIndex, c.ExhaustUids, c.RetrieveUid, c.DeckUids, c.HandUids, c.XAmount, c.PermanentUid);
                case Command_EndTurn et:
                    return Combat.EndTurn(state, et.HearthSparks, et.RetainUids);
                case Command_RetrieveSetCard c:
                    return SetBase.RetrieveSetCard(state, c.CardUid);
                case Command_PlayNecro c:
                    return Combat.PlayNecro(state, c.CardUid, c.TargetIndex);
                case Command_SetCard:
                case Command_ReactManual:
                case Command_ConfirmReaction:
                {
                    var system = ReactionSystems.Get(state.ReactionMode);
                    if (!system.CanHandle(state, command))
                    {
                        throw new InvalidOperationException($"{state.ReactionMode} では受け付けないコマンド: {command.Type}");
                    }
                    bool wasAwaiting = state.Phase == CombatPhases.AwaitingReaction;
                    var next = system.HandleCommand(state, command);
                    // 割り込み中断中のコマンドだったなら、敵フェーズの続きを解決する
                    return wasAwaiting ? Combat.ContinueAfterWindow(next) : next;
                }
            }
            // TS の switch は Command を網羅している
            return state;
        }
    }
}

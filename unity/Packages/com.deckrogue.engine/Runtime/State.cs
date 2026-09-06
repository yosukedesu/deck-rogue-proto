// State.cs — src/engine/state.ts の手書き移植 (unity/PORTING.md)
// GameState はイミュータブル。ApplyCommand(state, command) => newState の純関数のみで遷移する。
// 方式固有コマンドは IReactionSystem に委譲し、割り込み中断中だった場合は敵フェーズを再開する。
using System;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Engine
{
    public static class State
    {
        /// <summary>TS: state.ts の `export { createInitialState }` に対応する再エクスポート</summary>
        public static GameState CreateInitialState(int seed, string reactionMode) => Combat.CreateInitialState(seed, reactionMode);

        public static GameState ApplyCommand(GameState state, Command command)
        {
            switch (command)
            {
                case Command_StartCombat c:
                    return Combat.StartCombat(c.Seed, state.ReactionMode, c.EnemyId, c.DeckId, c.LeaderId, c.CardIds);
                case Command_PlayCard c:
                    return Combat.PlayCard(state, c.CardUid, c.ModeIndex, c.DiscardUids, c.TargetIndex, c.ExhaustUids, c.RetrieveUid, c.DeckUids, c.HandUids, c.XAmount, c.PermanentUid);
                case Command_EndTurn:
                    return Combat.EndTurn(state);
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

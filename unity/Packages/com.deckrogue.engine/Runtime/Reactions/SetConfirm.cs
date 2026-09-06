// Reactions/SetConfirm.cs — src/engine/reactions/set-confirm.ts の手書き移植 (unity/PORTING.md)
// 方式3: ハイブリッド (採用方式)。コスト事前払いで伏せる。条件成立時に「発動/温存」の確認だけ入る。
// pre窓 (行動確定時・実行前: 打ち消し・軽減) と post窓 (行動解決後: 返し系) の両方で確認が入る。
using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Engine
{
    public sealed class SetConfirmSystem : IReactionSystem
    {
        public string Mode => ReactionModes.SetConfirm;

        /// <summary>全カード伏せ可 (実験): 合致したのにエナジー不足で窓が開かなかった伏せ札を記録する</summary>
        private static GameState NoteUnaffordable(GameState state, ReactionWindow win)
        {
            var s = state;
            foreach (var c in Effects.UnaffordableSetCards(state, win))
            {
                s = Events.Emit(s, new GameEvent_ReactionUnaffordable { CardId = c.Def.Id, Cost = SetAny.SetFireCost(c), Energy = state.Player.Energy });
            }
            return s;
        }

        public bool CanHandle(GameState state, Command command)
        {
            switch (command)
            {
                case Command_SetCard:
                    // 型で受けて SetCard 側の具体的なエラー (エナジー不足・枠上限等) を出す
                    return true;
                case Command_ConfirmReaction:
                    return state.Phase == CombatPhases.AwaitingReaction;
                default:
                    return false;
            }
        }

        public GameState HandleCommand(GameState state, Command command)
        {
            switch (command)
            {
                case Command_SetCard c:
                    return SetBase.SetCard(state, c.CardUid);
                case Command_ConfirmReaction c:
                {
                    if (state.Phase != CombatPhases.AwaitingReaction || state.PendingWindow == null)
                    {
                        throw new InvalidOperationException("確認待ちではないのに ConfirmReaction が来た");
                    }
                    // 伏せ2枚 (かすみ): 窓に合致する伏せから発動する1枚を選ぶ。cardUid 省略時は先頭の合致札
                    var win = Effects.WindowFromPending(state);
                    var candidates = win != null ? Effects.UsableSetCards(state, win) : (IReadOnlyList<CardInstance>)new List<CardInstance>();
                    if (!c.Fire)
                    {
                        // 温存: 伏せたまま。判断そのものが set-confirm の主題なので記録する
                        return Events.Emit(state, new GameEvent_ReactionHeld
                        {
                            EnemyIndex = state.PendingWindow.EnemyIndex,
                            Stage = state.PendingWindow.Stage,
                            Kind = win != null ? win.Kind : EnemyActionKinds.Attack,
                            Value = win != null ? (win.Stage == "pre" ? win.Actual : win.HpLoss) : 0,
                            CandidateIds = candidates.Select(x => x.Def.Id).ToList(),
                        });
                    }
                    var card = c.CardUid != null
                        ? candidates.FirstOrDefault(x => x.Uid == c.CardUid)
                        : candidates.FirstOrDefault();
                    if (card == null) throw new InvalidOperationException("この窓で発動できる伏せカードがない");
                    return SetBase.FireSetCard(state, card, state.PendingWindow.EnemyIndex);
                }
                default:
                    throw new InvalidOperationException($"set-confirm が処理できないコマンド: {command.Type}");
            }
        }

        public GameState OnEvent(GameState state, GameEvent ev)
        {
            switch (ev)
            {
                case GameEvent_EnemyActionExecuting e:
                {
                    if (state.ReactionUsedThisAction) return state; // 敵の1行動につき1回まで
                    if (e.Kind == EnemyActionKinds.Rest) return state; // 隙 (何もしない) に確認を挟まない
                    var intent = Effects.EffectiveIntent(state, e.EnemyIndex);
                    int actual = intent != null ? intent.Actual : 0;
                    var win = new ReactionWindow { Stage = "pre", Kind = e.Kind, Actual = actual };
                    if (Effects.UsableSetCards(state, win).Count > 0)
                    {
                        return state with
                        {
                            Phase = CombatPhases.AwaitingReaction,
                            PendingWindow = new PendingWindow { EnemyIndex = e.EnemyIndex, Stage = "pre" },
                        };
                    }
                    return NoteUnaffordable(state, win);
                }
                case GameEvent_EnemyActionResolved e:
                {
                    if (state.ReactionUsedThisAction) return state; // pre窓で発動済みなら post窓は開かない
                    var win = new ReactionWindow { Stage = "post", Kind = e.Kind, HpLoss = e.HpLoss, Actual = e.Actual };
                    if (Effects.UsableSetCards(state, win).Count > 0)
                    {
                        return state with
                        {
                            Phase = CombatPhases.AwaitingReaction,
                            PendingWindow = new PendingWindow { EnemyIndex = e.EnemyIndex, Stage = "post" },
                        };
                    }
                    return NoteUnaffordable(state, win);
                }
                case GameEvent_EnemyPhaseEnded _:
                    // 温存も「そのターン発動しなかった伏せ」として空振り計上する
                    return SetBase.EmitWhiffForRemainingSet(state);
                default:
                    return state;
            }
        }
    }
}

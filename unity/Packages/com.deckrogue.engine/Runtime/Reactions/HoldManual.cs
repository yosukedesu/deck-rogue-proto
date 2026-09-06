// Reactions/HoldManual.cs — src/engine/reactions/hold-manual.ts の手書き移植 (unity/PORTING.md)
// 方式2: 構え式 (比較記録用に残置)。伏せない。敵の行動ごとに手札から手動発動の機会がある。
// コストは発動時に支払う。余剰エナジーは敵ターンに持ち越し。
using System;
using System.Collections.Generic;
using System.Linq;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Engine
{
    public sealed class HoldManualSystem : IReactionSystem
    {
        public string Mode => ReactionModes.HoldManual;

        /// <summary>いま開いているウィンドウ (敵の行動) に対して手札から発動できるリアクション一覧</summary>
        public static IReadOnlyList<CardInstance> PlayableReactions(GameState state)
        {
            if (state.Phase != CombatPhases.AwaitingReaction) return new List<CardInstance>();
            var win = Effects.WindowFromPending(state);
            if (win == null) return new List<CardInstance>();
            return state.Player.Hand.Where(c =>
                c.Def.Type == CardTypes.Reaction &&
                Effects.ReactionMatches(state, c, win) &&
                c.Def.Cost <= state.Player.Energy &&
                // 致死状態では回復を伴う札だけが生存の可能性を持つ
                (state.Player.Hp > 0 || Effects.CanSaveFromLethal(c, state))).ToList();
        }

        /// <summary>この誘発窓で手札から発動できるリアクションがあるか</summary>
        private static bool AnyPlayable(GameState state, ReactionWindow win)
        {
            return state.Player.Hand.Any(c =>
                c.Def.Type == CardTypes.Reaction &&
                Effects.ReactionMatches(state, c, win) &&
                c.Def.Cost <= state.Player.Energy &&
                (state.Player.Hp > 0 || Effects.CanSaveFromLethal(c, state)));
        }

        public bool CanHandle(GameState state, Command command)
        {
            switch (command)
            {
                case Command_ReactManual c:
                    return PlayableReactions(state).Any(x => x.Uid == c.CardUid);
                case Command_ConfirmReaction c:
                    // fire: false をパス (発動しない) として受け付ける
                    return state.Phase == CombatPhases.AwaitingReaction && !c.Fire;
                default:
                    return false;
            }
        }

        public GameState HandleCommand(GameState state, Command command)
        {
            switch (command)
            {
                case Command_ReactManual c:
                {
                    if (state.Phase != CombatPhases.AwaitingReaction || state.PendingWindow == null)
                    {
                        throw new InvalidOperationException("割り込みウィンドウ外で ReactManual が来た");
                    }
                    var card = PlayableReactions(state).FirstOrDefault(x => x.Uid == c.CardUid);
                    if (card == null) throw new InvalidOperationException($"発動できないカード: {c.CardUid}");
                    // 発動時にコストを支払い、手札から捨て札へ。1行動1回の消費フラグも立てる
                    var discard = new List<CardInstance>(state.Player.DiscardPile) { card };
                    var s = state with
                    {
                        ReactionUsedThisAction = true,
                        Player = state.Player with
                        {
                            Energy = state.Player.Energy - card.Def.Cost,
                            Hand = state.Player.Hand.Where(x => x.Uid != card.Uid).ToList(),
                            DiscardPile = discard,
                        },
                    };
                    s = Effects.ResolveReactionEffects(s, card, state.PendingWindow.EnemyIndex);
                    return s;
                }
                case Command_ConfirmReaction c:
                {
                    if (state.Phase != CombatPhases.AwaitingReaction) throw new InvalidOperationException("割り込みウィンドウ外でパスが来た");
                    if (c.Fire) throw new InvalidOperationException("hold-manual の発動は ReactManual でカードを指定する");
                    return state; // パス: 何もしない。敵の行動処理はそのまま進む
                }
                default:
                    throw new InvalidOperationException($"hold-manual が処理できないコマンド: {command.Type}");
            }
        }

        public GameState OnEvent(GameState state, GameEvent ev)
        {
            switch (ev)
            {
                case GameEvent_EnemyActionExecuting e:
                {
                    if (state.ReactionUsedThisAction) return state; // 敵の1行動につき1回まで
                    var intent = Effects.EffectiveIntent(state, e.EnemyIndex);
                    int actual = intent != null ? intent.Actual : 0;
                    if (AnyPlayable(state, new ReactionWindow { Stage = "pre", Kind = e.Kind, Actual = actual }))
                    {
                        return state with
                        {
                            Phase = CombatPhases.AwaitingReaction,
                            PendingWindow = new PendingWindow { EnemyIndex = e.EnemyIndex, Stage = "pre" },
                        };
                    }
                    return state;
                }
                case GameEvent_EnemyActionResolved e:
                {
                    if (state.ReactionUsedThisAction) return state; // pre窓で発動済みなら post窓は開かない
                    if (AnyPlayable(state, new ReactionWindow { Stage = "post", Kind = e.Kind, HpLoss = e.HpLoss, Actual = e.Actual }))
                    {
                        return state with
                        {
                            Phase = CombatPhases.AwaitingReaction,
                            PendingWindow = new PendingWindow { EnemyIndex = e.EnemyIndex, Stage = "post" },
                        };
                    }
                    return state;
                }
                case GameEvent_EnemyPhaseEnded _:
                {
                    // 空振り計上: 敵ターンを終えて使われず捨てられていくリアクションカード
                    var s = state;
                    foreach (var card in state.Player.Hand)
                    {
                        if (card.Def.Type == CardTypes.Reaction)
                        {
                            s = Events.Emit(s, new GameEvent_ReactionWhiffed { CardId = card.Def.Id });
                        }
                    }
                    return s;
                }
                default:
                    return state;
            }
        }
    }
}

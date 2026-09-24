// Reactions/SetAuto.cs — src/engine/reactions/set-auto.ts の手書き移植 (unity/PORTING.md)
// 方式1: セット式。コスト事前払いで伏せる。条件成立で自動発動 (プレイヤーの判断は挟まらない)。
using System;
using System.Linq;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Engine
{
    public sealed class SetAutoSystem : IReactionSystem
    {
        public string Mode => ReactionModes.SetAuto;

        public bool CanHandle(GameState state, Command command)
        {
            // 型で受けて SetCard 側の具体的なエラーを出す (set-confirm と同じ裁定)
            return command is Command_SetCard;
        }

        public GameState HandleCommand(GameState state, Command command)
        {
            if (!(command is Command_SetCard c)) throw new InvalidOperationException($"set-auto が処理できないコマンド: {command.Type}");
            return SetBase.SetCard(state, c.CardUid);
        }

        public GameState OnEvent(GameState state, GameEvent ev)
        {
            switch (ev)
            {
                case GameEvent_EnemyActionExecuting e:
                {
                    if (state.ReactionUsedThisAction) return state; // 敵の1行動につき1回まで
                    // 打ち消し済み (楔 ActionNegated・全体の NegateNextAction) の行動には窓を開かない (2026-09-20 Opus 火種A)。TS と同形
                    if ((e.EnemyIndex < state.Enemies.Count && state.Enemies[e.EnemyIndex].ActionNegated == true) || state.NegateNextAction) return state;
                    // 窓の値は攻撃なら1発×ヒット数の合計・攻撃者が混乱中なら被攻撃前の罠は候補にしない (2026-09-24 E10・E3)。TS と同形
                    var win = Effects.PreWindowFor(state, e.EnemyIndex) with { Kind = e.Kind };
                    var card = Effects.UsableSetCards(state, win).FirstOrDefault();
                    if (card != null)
                    {
                        return SetBase.FireSetCard(state, card, e.EnemyIndex); // 条件成立 → 先頭の合致札を即自動発動
                    }
                    return state;
                }
                case GameEvent_EnemyActionResolved e:
                {
                    // 窓ごとに1枚 (2026-09-14): pre 窓で鳴っても post 窓は開く
                    var win = new ReactionWindow { Stage = "post", Kind = e.Kind, HpLoss = e.HpLoss, Actual = Effects.ReactionActionValue(state, e.EnemyIndex) };
                    var card = Effects.UsableSetCards(state, win).FirstOrDefault();
                    if (card != null)
                    {
                        return SetBase.FireSetCard(state, card, e.EnemyIndex);
                    }
                    return state;
                }
                case GameEvent_EnemyPhaseEnded _:
                    return SetBase.EmitWhiffForRemainingSet(state);
                default:
                    return state;
            }
        }
    }
}

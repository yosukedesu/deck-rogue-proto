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
                    var intent = Effects.EffectiveIntent(state, e.EnemyIndex);
                    int actual = intent != null ? intent.Actual : 0;
                    var win = new ReactionWindow { Stage = "pre", Kind = e.Kind, Actual = actual };
                    var card = Effects.UsableSetCards(state, win).FirstOrDefault();
                    if (card != null)
                    {
                        return SetBase.FireSetCard(state, card, e.EnemyIndex); // 条件成立 → 先頭の合致札を即自動発動
                    }
                    return state;
                }
                case GameEvent_EnemyActionResolved e:
                {
                    if (state.ReactionUsedThisAction) return state;
                    var win = new ReactionWindow { Stage = "post", Kind = e.Kind, HpLoss = e.HpLoss, Actual = e.Actual };
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

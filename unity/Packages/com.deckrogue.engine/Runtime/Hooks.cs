// Hooks.cs — src/engine/hooks.ts の手書き移植 (unity/PORTING.md)
// 戦闘内イベントに反応するフック (リスナー) をここで束ねる。
// 現在のフック: ①置物 (permanent) ②リアクション方式。
using DeckRogue.Engine.Generated;

namespace DeckRogue.Engine
{
    public static class Hooks
    {
        /// <summary>置物トリガーの実行本体は Effects へ移設。既存の参照は従来どおりここから呼べる</summary>
        public static GameState RunPermanentTriggers(GameState state, string trigger, int enemyIndex)
            => Effects.RunPermanentTriggers(state, trigger, enemyIndex);

        public static GameState DispatchHooks(GameState state, GameEvent ev)
        {
            var s = state;
            // 1. 置物フック (自動発火。リアクションの割り込みより先に解決する)
            if (ev is GameEvent_EnemyActionExecuting exec1 && exec1.Kind == EnemyActionKinds.Attack)
            {
                // 被攻撃前 (軽減系) の置物
                s = Effects.RunPermanentTriggers(s, "onAttackIncoming", exec1.EnemyIndex);
            }
            if (ev is GameEvent_EnemyActionResolved res1 && res1.Kind == EnemyActionKinds.Attack)
            {
                // 被攻撃後 (返し系) の置物: 茨の茂みなど
                s = Effects.RunPermanentTriggers(s, "onAttacked", res1.EnemyIndex);
            }
            if (ev is GameEvent_EnemyActionExecuting || ev is GameEvent_EnemyActionResolved)
            {
                // 置物の返しで敵が倒れたらリアクション確認はもう不要。
                // プレイヤーのHPが0以下でも post窓は開く (致死時の誘発)
                int idx = ev is GameEvent_EnemyActionExecuting e2 ? e2.EnemyIndex : ((GameEvent_EnemyActionResolved)ev).EnemyIndex;
                var enemy = (idx >= 0 && idx < s.Enemies.Count) ? s.Enemies[idx] : null;
                if (enemy != null && enemy.Hp <= 0) return s;
                if (s.Player.Hp <= 0 && ev is GameEvent_EnemyActionExecuting) return s;
            }
            // 2. リアクション方式フック (方式は state.reactionMode から解決)
            s = ReactionSystems.Get(s.ReactionMode).OnEvent(s, ev);
            // 3. (将来) レリックフック / 敵パッシブフック をここに追加
            return s;
        }
    }
}

// Events.cs — イベントログへの追記 (戦闘内の出来事はすべてイベント)。src/engine/events.ts の移植。

using System.Collections.Generic;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Engine
{
    public static class Events
    {
        // Display-only copies of actual direct-mutation values. Rules never read these snapshots.
        public static CombatPresentationReceipt PlayerPresentationReceipt(PlayerState before, PlayerState after) => new CombatPresentationReceipt
        {
            Version = 1, Target = "player",
            Before = new CombatPresentationVitals { Hp = before.Hp, MaxHp = before.MaxHp, Block = before.Block, IceBlock = before.IceBlock },
            After = new CombatPresentationVitals { Hp = after.Hp, MaxHp = after.MaxHp, Block = after.Block, IceBlock = after.IceBlock },
        };

        public static CombatPresentationReceipt EnemyPresentationReceipt(int enemyIndex, EnemyState before, EnemyState after) => new CombatPresentationReceipt
        {
            Version = 1, Target = "enemy", EnemyIndex = enemyIndex, EnemyId = before.EnemyId,
            Before = new CombatPresentationVitals { Hp = before.Hp, MaxHp = before.MaxHp, Block = before.Block },
            After = new CombatPresentationVitals { Hp = after.Hp, MaxHp = after.MaxHp, Block = after.Block },
        };

        /// <summary>イベントをログに追記した新しい状態を返す</summary>
        public static GameState Emit(GameState state, GameEvent ev)
        {
            var log = new List<GameEvent>(state.EventLog.Count + 1);
            log.AddRange(state.EventLog);
            log.Add(ev);
            return state with { EventLog = log };
        }
    }
}

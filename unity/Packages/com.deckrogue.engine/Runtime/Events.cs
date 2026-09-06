// Events.cs — イベントログへの追記 (戦闘内の出来事はすべてイベント)。src/engine/events.ts の移植。

using System.Collections.Generic;
using DeckRogue.Engine.Generated;

namespace DeckRogue.Engine
{
    public static class Events
    {
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

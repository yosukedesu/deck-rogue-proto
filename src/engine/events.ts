// engine/events.ts — イベントログへの追記 (戦闘内の出来事はすべてイベント)

import type { GameEvent, GameState } from './types.ts'

/** イベントをログに追記した新しい状態を返す */
export function emit(state: GameState, event: GameEvent): GameState {
  return { ...state, eventLog: [...state.eventLog, event] }
}

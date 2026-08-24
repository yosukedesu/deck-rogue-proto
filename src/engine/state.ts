// engine/state.ts — 状態遷移の入口
// GameState はイミュータブル。applyCommand(state, command) => newState の純関数のみで遷移する。
// 方式固有コマンドは ReactionSystem に委譲し、割り込み中断中だった場合は敵フェーズを再開する。

import { continueAfterWindow, createInitialState, endTurn, playCard, startCombat } from './combat.ts'
import { getReactionSystem } from './reactions/index.ts'
import type { Command, GameState } from './types.ts'

export { createInitialState }

export function applyCommand(state: GameState, command: Command): GameState {
  switch (command.type) {
    case 'StartCombat':
      return startCombat(command.seed, state.reactionMode, command.enemyId, command.deckId, command.leaderId)
    case 'PlayCard':
      return playCard(state, command.cardUid, command.modeIndex, command.discardUids)
    case 'EndTurn':
      return endTurn(state)
    case 'SetCard':
    case 'ReactManual':
    case 'ConfirmReaction': {
      const system = getReactionSystem(state.reactionMode)
      if (!system.canHandle(state, command)) {
        throw new Error(`${state.reactionMode} では受け付けないコマンド: ${command.type}`)
      }
      const wasAwaiting = state.phase === 'awaiting-reaction'
      const next = system.handleCommand(state, command)
      // 割り込み中断中のコマンドだったなら、敵フェーズの続きを解決する
      return wasAwaiting ? continueAfterWindow(next) : next
    }
  }
}

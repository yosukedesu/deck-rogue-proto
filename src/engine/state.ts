// engine/state.ts — 状態遷移の入口
// GameState はイミュータブル。applyCommand(state, command) => newState の純関数のみで遷移する。
// TODO(実装フェーズ): 戦闘ループ・イベントパイプライン・ReactionSystem 3実装。
//   現時点は開発環境セットアップの骨格 (StartCombat のみ動く)。

import type { Command, GameState, ReactionMode } from './types.ts'
import { createRng } from './rng.ts'

export function createInitialState(seed: number, reactionMode: ReactionMode): GameState {
  return {
    rng: createRng(seed),
    reactionMode,
    phase: 'player-turn',
    turn: 0,
    player: {
      hp: 70,
      maxHp: 70,
      block: 0,
      energy: 3,
      energyMax: 3,
      hand: [],
      drawPile: [],
      discardPile: [],
      setCards: [],
      growth: 0,
    },
    enemies: [],
    eventLog: [],
  }
}

export function applyCommand(state: GameState, command: Command): GameState {
  switch (command.type) {
    case 'StartCombat':
      return {
        ...createInitialState(command.seed, state.reactionMode),
        turn: 1,
        eventLog: [{ type: 'CombatStarted' }, { type: 'TurnStarted', turn: 1 }],
      }
    case 'PlayCard':
    case 'SetCard':
    case 'ReactManual':
    case 'ConfirmReaction':
    case 'EndTurn':
      throw new Error(`未実装コマンド: ${command.type} (実装フェーズで対応)`)
  }
}

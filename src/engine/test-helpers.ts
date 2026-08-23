// engine/test-helpers.ts — テスト用の状態構築ヘルパー (vitest 非依存の純関数)
// 手札やの敵の意図を直接差し替えることで、シャッフル・重み抽選の乱数に
// 依存しない決定的なルールテストを書けるようにする。

import { getCardDef } from './content.ts'
import { applyCommand, createInitialState } from './state.ts'
import type { EnemyIntent, GameState, ReactionMode } from './types.ts'

/** StartCombat 済みの戦闘状態を作る */
export function freshCombat(
  mode: ReactionMode,
  enemyId: string,
  seed = 42,
  deckId?: string,
): GameState {
  return applyCommand(createInitialState(seed, mode), { type: 'StartCombat', seed, enemyId, deckId })
}

/** 手札を指定カードだけに差し替える (uid は t0_, t1_... で採番) */
export function withHand(state: GameState, cardIds: readonly string[]): GameState {
  return {
    ...state,
    player: {
      ...state.player,
      hand: cardIds.map((id, i) => ({ uid: `t${i}_${id}`, def: getCardDef(id) })),
    },
  }
}

/** 先頭の敵の意図を差し替える (重み抽選に依存しないテスト用) */
export function withIntent(state: GameState, intent: EnemyIntent): GameState {
  return {
    ...state,
    enemies: state.enemies.map((e, i) => (i === 0 ? { ...e, intent } : e)),
  }
}

export function attackIntent(actual: number): EnemyIntent {
  return { kind: 'attack', shownMin: actual, shownMax: actual, actual }
}

export function defendIntent(actual: number): EnemyIntent {
  return { kind: 'defend', shownMin: actual, shownMax: actual, actual }
}

export function destroySetIntent(): EnemyIntent {
  return { kind: 'destroy-set', shownMin: 0, shownMax: 0, actual: 0 }
}

// engine/events.ts — イベントログへの追記 (戦闘内の出来事はすべてイベント)

import type { CombatPresentationReceipt, EnemyState, GameEvent, GameState, PlayerState } from './types.ts'

/** イベントをログに追記した新しい状態を返す */
export function emit(state: GameState, event: GameEvent): GameState {
  return { ...state, eventLog: [...state.eventLog, event] }
}

/** Copy only already-resolved values; never infer absorption from Blocked/Amount. */
export function playerPresentationReceipt(before: PlayerState, after: PlayerState): CombatPresentationReceipt {
  return {
    version: 1, target: 'player',
    before: { hp: before.hp, maxHp: before.maxHp, block: before.block, iceBlock: before.iceBlock },
    after: { hp: after.hp, maxHp: after.maxHp, block: after.block, iceBlock: after.iceBlock },
  }
}

export function enemyPresentationReceipt(enemyIndex: number, before: EnemyState, after: EnemyState): CombatPresentationReceipt {
  return {
    version: 1, target: 'enemy', enemyIndex, enemyId: before.enemyId,
    before: { hp: before.hp, maxHp: before.maxHp, block: before.block },
    after: { hp: after.hp, maxHp: after.maxHp, block: after.block },
  }
}

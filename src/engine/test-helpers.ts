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

// ---- マップランのテスト用航法 (2026-08-28 マップ化) ----
import { applyRunCommand, createRun, nextChoices } from './run.ts'
import type { RunState } from './run.ts'
import type { MapNodeType } from './map.ts'

/**
 * マップフェーズで「targetタイプのノードへ届く」次の一歩の列を返す。
 * 届かない場合は戦闘ノード優先で前進する (テストの経路探索用)
 */
export function stepToward(run: RunState, target: MapNodeType): number {
  const cands = nextChoices(run)
  const memo = new Map<string, boolean>()
  const reaches = (row: number, col: number): boolean => {
    const key = `${row}:${col}`
    const hit = memo.get(key)
    if (hit !== undefined) return hit
    const node = run.map[row][col]
    const ok = node.type === target || node.next.some((c) => reaches(row + 1, c))
    memo.set(key, ok)
    return ok
  }
  const found = cands.find((c) => reaches(run.row + 1, c))
  if (found !== undefined) return found
  const battle = cands.find((c) => {
    const t = run.map[run.row + 1][c].type
    return t === 'battle' || t === 'boss'
  })
  return battle ?? cands[0]
}

/** マップフェーズなら target へ向かって1ノード進む。それ以外はそのまま */
export function chooseToward(run: RunState, target: MapNodeType = 'battle'): RunState {
  if (run.phase !== 'map') return run
  return applyRunCommand(run, { type: 'ChooseNode', col: stepToward(run, target) })
}

/** ラン開始 + マップを戦闘ノードまで進めて最初の戦闘に入った状態を返す */
export function createRunInBattle(
  seed: number,
  mode: ReactionMode,
  leaderId?: string,
): RunState {
  let run = createRun(seed, mode, leaderId)
  while (run.phase === 'map') run = chooseToward(run, 'battle')
  return run
}

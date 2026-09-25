// 青の3本柱 (2026-09-25) のボット: 占術は状態異常だけ捨てる・仕掛けの手配は山札の伏せ札だけから選ぶ・満ち潮の書庫は手札参照を先に残す
import { describe, expect, it } from 'vitest'
import { getCardDef } from '../engine/content.ts'
import { applyCommand } from '../engine/state.ts'
import { attackIntent, freshCombat, withHand, withIntent } from '../engine/test-helpers.ts'
import type { CardInstance, GameState } from '../engine/types.ts'
import { chooseCommand } from './run.ts'

const inst = (id: string, n: number): CardInstance => ({ uid: `d${n}_${id}`, def: getCardDef(id) })
const withDraw = (s: GameState, ids: readonly string[]): GameState => ({ ...s, player: { ...s.player, drawPile: ids.map((id, i) => inst(id, i)), discardPile: [] } })
const perm = (s: GameState, id: string): GameState => ({ ...s, player: { ...s.player, permanents: [...s.player.permanents, { uid: `p_${id}`, def: getCardDef(id) }] } })
const base = (hand: readonly string[]) => {
  const s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), hand)
  return { ...s, player: { ...s.player, energy: 9 } }
}

describe('青のボット', () => {
  it('占術の保留では状態異常だけを捨てる', () => {
    let s = perm(base([]), 'blue_perm_tide_tower')
    s = withDraw(s, ['blue_guard', 'blue_guard', 'blue_guard', 'blue_guard', 'blue_guard', 'blue_guard', 'status_wound', 'blue_strike', 'blue_strike'])
    s = applyCommand(withIntent(s, attackIntent(1)), { type: 'EndTurn' })
    expect(chooseCommand(s)).toEqual({ type: 'ResolveScry', discardUids: ['d6_status_wound'] })
  })

  it('仕掛けの手配は山札の伏せ札だけから選ぶ', () => {
    const bot = chooseCommand(withDraw(base(['blue_trap_fetch']), ['blue_strike', 'blue_mana_leak']))
    expect(bot).toMatchObject({ type: 'PlayCard', deckUids: ['d1_blue_mana_leak'] })
  })

  it('満ち潮の書庫があれば手札参照の札を先に残す', () => {
    const s = perm(base(['blue_strike', 'blue_weight_of_wisdom']), 'blue_perm_flood_archive')
    const hand = { ...s, player: { ...s.player, energy: 0 } }
    expect(chooseCommand(hand)).toMatchObject({ type: 'EndTurn', retainUids: ['t1_blue_weight_of_wisdom', 't0_blue_strike'] })
  })
})

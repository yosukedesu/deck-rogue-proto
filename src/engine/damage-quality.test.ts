// 2026-09-17 演出「ダメージの質の見分け」: DamageDealt に急所・貫通・ブロックが吸った量を載せる (表示は実処理と同じ値を読む)
import { describe, expect, it } from 'vitest'
import { applyCommand } from './state.ts'
import { attackIntent, freshCombat, withHand, withIntent } from './test-helpers.ts'
import type { GameEvent } from './types.ts'

function lastDamage(log: readonly GameEvent[], source: 'player' | 'enemy') {
  for (let i = log.length - 1; i >= 0; i--) {
    const e = log[i]
    if (e.type === 'DamageDealt' && e.source === source) return e
  }
  throw new Error('DamageDealt が無い')
}

describe('DamageDealt のダメージの質 (2026-09-17)', () => {
  it('急所が乗ったヒットは exposed:true、素のヒットには付かない', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 42), ['green_strike', 'green_strike'])
    s = { ...s, player: { ...s.player, energy: 5 }, enemies: s.enemies.map((e) => ({ ...e, exposed: 1, block: 0 })) }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_strike', targetIndex: 0 })
    const d1 = lastDamage(s.eventLog, 'player')
    expect(d1.exposed).toBe(true)
    expect(d1.amount).toBe(9) // 6 × 1.5
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_green_strike', targetIndex: 0 })
    const d2 = lastDamage(s.eventLog, 'player')
    expect(d2.exposed).toBeUndefined()
    expect(d2.amount).toBe(6)
  })

  it('敵のブロックが吸えば blocked、貫通なら pierced (ブロックが1以上あった時だけ)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 42), ['green_strike', 'green_fang', 'green_fang'])
    s = { ...s, player: { ...s.player, energy: 9 }, enemies: s.enemies.map((e) => ({ ...e, block: 4 })) }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_strike', targetIndex: 0 })
    const d1 = lastDamage(s.eventLog, 'player')
    expect(d1.blocked).toBe(4)
    expect(d1.hpLoss).toBe(2)
    expect(d1.pierced).toBeUndefined()
    // ブロックを盛り直して貫通
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, block: 5 })) }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_green_fang', targetIndex: 0 })
    const d2 = lastDamage(s.eventLog, 'player')
    expect(d2.pierced).toBe(true)
    expect(d2.blocked).toBeUndefined()
    expect(d2.hpLoss).toBe(17)
    expect(s.enemies[0].block).toBe(5)
    // ブロックが無い相手への貫通は「無視した」ことにならない
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, block: 0 })) }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't2_green_fang', targetIndex: 0 })
    expect(lastDamage(s.eventLog, 'player').pierced).toBeUndefined()
  })

  it('敵の攻撃を自分のブロックと氷壁で受けた量は blocked (合計)。完全に防いでも量は残る', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 42), [])
    s = withIntent(s, attackIntent(10))
    s = { ...s, player: { ...s.player, block: 4, iceBlock: 3 } }
    s = applyCommand(s, { type: 'EndTurn' })
    const d = lastDamage(s.eventLog, 'enemy')
    expect(d.amount).toBe(10)
    expect(d.blocked).toBe(7)
    expect(d.hpLoss).toBe(3)
  })
})

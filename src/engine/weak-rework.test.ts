// 威圧の本家 Weak 化 (2026-09-03 ユーザー裁定「b」): 威圧N=次のN回の攻撃行動の与ダメ-25% (切り捨て・最低1)。
// 攻撃行動を実行するたび1減る (多段は1行動で1)。防御などの非攻撃行動は消費しない。延焼と同じく表示側も同じ式を読む。
import { describe, expect, it } from 'vitest'
import { applyEnemyWeak } from './effects.ts'
import { applyCommand } from './state.ts'
import { worstIncomingFrom } from './summary.ts'
import { attackIntent, defendIntent, freshCombat, withHand, withIntent } from './test-helpers.ts'
import type { GameState } from './types.ts'

const weaken = (s: GameState, n: number): GameState => ({ ...s, enemies: s.enemies.map((e, i) => (i === 0 ? { ...e, weak: n } : e)) })
const dealt = (s: GameState) => s.eventLog.filter((e) => e.type === 'DamageDealt' && e.source === 'enemy').reduce((a, e) => a + (e as { amount: number }).amount, 0)

describe('威圧 (敵版弱体)', () => {
  it('式: -25% 切り捨て・最低1', () => {
    expect(applyEnemyWeak(8, 1)).toBe(6)
    expect(applyEnemyWeak(1, 1)).toBe(1)
    expect(applyEnemyWeak(8, 0)).toBe(8)
  })
  it('次の攻撃行動が-25%になり、行動が終わるとスタックが1減る', () => {
    let s = weaken(withHand(freshCombat('set-confirm', 'enemy_brute', 42), []), 1)
    s = withIntent(s, attackIntent(8))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(dealt(s)).toBe(6)
    expect(s.enemies[0].weak).toBe(0)
  })
  it('多段は全ヒットが-25%で、消費は1行動で1', () => {
    let s = weaken(withHand(freshCombat('set-confirm', 'enemy_brute', 42), []), 2)
    s = withIntent(s, { ...attackIntent(8), hits: 3 })
    s = applyCommand(s, { type: 'EndTurn' })
    expect(dealt(s)).toBe(18)
    expect(s.enemies[0].weak).toBe(1)
  })
  it('防御など攻撃以外の行動は威圧を消費しない', () => {
    let s = weaken(withHand(freshCombat('set-confirm', 'enemy_brute', 42), []), 1)
    s = withIntent(s, defendIntent(5))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.enemies[0].weak).toBe(1)
  })
  it('最悪被ダメ予測にも-25%が乗る', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42), [])
    s = withIntent(s, attackIntent(8))
    const before = worstIncomingFrom(s, 0)
    const after = worstIncomingFrom(weaken(s, 1), 0)
    expect(after).toBe(Math.max(1, Math.floor(before * 0.75)))
  })
  it('アーティファクト持ちには弾かれる', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_white'), ['white_menace'])
    s = { ...s, player: { ...s.player, energy: 9 }, enemies: s.enemies.map((e) => ({ ...e, artifact: 1 })) }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_white_menace' })
    expect(s.enemies[0].weak ?? 0).toBe(0)
    expect(s.enemies[0].artifact).toBe(0)
  })
})

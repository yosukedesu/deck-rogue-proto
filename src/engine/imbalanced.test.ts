// バランス崩し (2026-09-04 ユーザー裁定。本家 StS2 ImbalancedPower=BowlbugRock): 攻撃を完全に防ぐ (HP損失0) と
// 敵は体勢を崩し、次の宣言が隙 (rest) になる。部分ブロックでは崩れない。多段は全ヒットを防いで初めて崩れる。
import { describe, expect, it } from 'vitest'
import { applyCommand } from './state.ts'
import { attackIntent, freshCombat, withHand, withIntent } from './test-helpers.ts'

const withBlock = (s: ReturnType<typeof freshCombat>, block: number) => ({ ...s, player: { ...s.player, block } })

describe('バランス崩し (imbalanced)', () => {
  it('攻撃を完全に防ぐと体勢を崩し、次の宣言が隙になる', () => {
    let s = withHand(withBlock(withIntent(freshCombat('set-confirm', 'enemy_bowl_bug', 42), attackIntent(10)), 10), [])
    const hp0 = s.player.hp
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.hp).toBe(hp0)
    expect(s.eventLog.some((e) => e.type === 'EnemyStaggered')).toBe(true)
    expect(s.phase).toBe('player-turn')
    expect(s.enemies[0].intent?.kind).toBe('rest')
    expect(s.enemies[0].staggeredNext).toBe(false)
    // 隙のターンを渡すと、次は通常のローテーションに戻る (崩れっぱなしにならない)
    s = withHand(s, [])
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.enemies[0].intent?.kind).not.toBe('rest')
  })

  it('部分ブロック (HP損失あり) では崩れない', () => {
    let s = withHand(withBlock(withIntent(freshCombat('set-confirm', 'enemy_bowl_bug', 42), attackIntent(10)), 6), [])
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.eventLog.some((e) => e.type === 'EnemyStaggered')).toBe(false)
    expect(s.enemies[0].intent?.kind).not.toBe('rest')
  })

  it('多段は全ヒットを防いで初めて崩れる', () => {
    const twoHits = { ...attackIntent(6), hits: 2 }
    let s = withHand(withBlock(withIntent(freshCombat('set-confirm', 'enemy_bowl_bug', 42), twoHits), 11), [])
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.eventLog.some((e) => e.type === 'EnemyStaggered')).toBe(false)
    let t = withHand(withBlock(withIntent(freshCombat('set-confirm', 'enemy_bowl_bug', 42), twoHits), 12), [])
    t = applyCommand(t, { type: 'EndTurn' })
    expect(t.eventLog.some((e) => e.type === 'EnemyStaggered')).toBe(true)
  })

  it('バランス崩しを持たない敵は完全に防いでも崩れない', () => {
    let s = withHand(withBlock(withIntent(freshCombat('set-confirm', 'enemy_probe', 42), attackIntent(10)), 10), [])
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.eventLog.some((e) => e.type === 'EnemyStaggered')).toBe(false)
  })
})

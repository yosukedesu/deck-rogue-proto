// 赤の拡張 (2026-08-25 +15枚 = 38枚) のテスト。
// 確定済みルール表「憤怒」「爆熱」「処刑」「粉砕の換金」「衝動の誘発」を固定する。
// 赤の受け: 守らず、被弾を次の攻撃の燃料に換金する (威嚇の後継)。
import { describe, expect, it } from 'vitest'
import { applyCommand } from './state.ts'
import { attackIntent, defendIntent, freshCombat, withHand, withIntent } from './test-helpers.ts'

describe('憤怒 (被弾の換金)', () => {
  it('憤怒の仮面: 被攻撃後に勢い+2。敵フェーズに得た勢いは次の自ターンの攻撃に乗る', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_red'), [
      'red_perm_rage_mask',
    ])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_perm_rage_mask' })
    s = withIntent(s, attackIntent(6))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.momentum).toBe(2)
    // 次の自ターンの攻撃に+2が乗る
    s = withHand(s, ['red_strike'])
    const enemyHp = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_strike' })
    expect(s.enemies[0].hp).toBe(enemyHp - 6 - 2)
  })

  it('逆上: 直前の敵フェーズで受けたダメージ×2のダメージ', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_red'), [])
    s = withIntent(s, attackIntent(8))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.damageTakenLastEnemyPhase).toBe(8)
    s = withHand(s, ['red_payback'])
    const enemyHp = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_payback' })
    expect(s.enemies[0].hp).toBe(enemyHp - 16)
  })

  it('ブロックで防いだ分は憤怒に数えない (HP損失のみ)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_red'), ['red_guard'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_guard' }) // ブロック4
    s = withIntent(s, attackIntent(10))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.damageTakenLastEnemyPhase).toBe(6) // 10 - ブロック4
  })
})

describe('爆熱 (延焼の換金)', () => {
  it('爆熱の解放: 延焼×3のダメージを与え、延焼が0になる', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_red'), [
      'red_burst_release',
    ])
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, burn: 5 })) }
    const enemyHp = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_burst_release' })
    expect(s.enemies[0].hp).toBe(enemyHp - 15)
    expect(s.enemies[0].burn).toBe(0)
  })
})

describe('処刑 (とどめの一撃)', () => {
  it('対象HPが25%以下なら8→24ダメージに跳ね上がる', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_red'), [
      'red_final_blow',
      'red_final_blow',
    ])
    s = { ...s, player: { ...s.player, energy: 9 } }
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_final_blow' })
    expect(s.enemies[0].hp).toBe(hpBefore - 8) // 25%超: 素の8
    const maxHp = s.enemies[0].maxHp
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, hp: Math.floor(maxHp * 0.25) })) }
    const low = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_red_final_blow' })
    expect(s.enemies[0].hp).toBe(low - 24)
  })
})

describe('粉砕の換金 (破城槌)', () => {
  it('敵のブロックを全て破壊し、破壊した値と同じダメージを与える', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_red'), [
      'red_battering_ram',
    ])
    s = withIntent(s, defendIntent(10))
    s = applyCommand(s, { type: 'EndTurn' }) // 敵がブロック10を得る
    s = withHand(s, ['red_battering_ram'])
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_battering_ram' })
    expect(s.enemies[0].block).toBe(0)
    expect(s.enemies[0].hp).toBe(hpBefore - 16) // 破壊値10 + 基礎6 (2026-08-27。敵ブロック0でも死に札にならない)
  })
})

describe('刹那の焔 (衝動の誘発)', () => {
  it('衝動でプレイしたカードだけ延焼+1が乗る', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_red'), [
      'red_perm_instant_flame',
      'red_impulse',
      'red_strike',
    ])
    s = { ...s, player: { ...s.player, energy: 9 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_perm_instant_flame' })
    // 通常プレイでは誘発しない
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't2_red_strike' })
    expect(s.enemies[0].burn).toBe(0)
    // 衝動 (山札の上2枚がこのターン限りの手札に)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_red_impulse' })
    const impulseUid = s.player.impulseUids[0]
    const impulseCard = s.player.hand.find((c) => c.uid === impulseUid)!
    if (impulseCard.def.type !== 'reaction') {
      s = applyCommand(s, { type: 'PlayCard', cardUid: impulseUid, targetIndex: 0 })
      expect(s.enemies[0].burn).toBeGreaterThanOrEqual(1) // 刹那の焔の誘発
    }
  })
})

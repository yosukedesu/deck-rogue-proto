// 赤 (3色目) のテスト。赤の柱: バーン (延焼) / 刹那のリソース (儀式・衝動) / 粉砕・ランダム火力。
import { describe, expect, it } from 'vitest'
import { allCards, buildDeck, getDeckDef } from './content.ts'

import { applyCommand } from './state.ts'
import { windowFromPending } from './effects.ts'
import { createRunInBattle, defendIntent, freshCombat, withHand, withIntent } from './test-helpers.ts'
import type { GameEvent } from './types.ts'

const types = (log: readonly GameEvent[]) => log.map((e) => e.type)

describe('赤のカラーパイ', () => {
  it('赤のカードとデッキが揃っている', () => {
    expect(allCards.filter((c) => c.color === 'red').length).toBeGreaterThan(0)
    for (const id of ['starter_red', 'deck_burn', 'deck_chaos', 'run_basic_red']) {
      expect(buildDeck(id).length).toBeGreaterThan(0)
      expect(getDeckDef(id).color).toBe('red')
    }
  })

  it('赤ランは赤の基本デッキで始まり、色が保持される', () => {
    const run = createRunInBattle(7, 'set-confirm', 'leader_red')
    expect(run.colors).toEqual(['red'])
    expect(run.deck.every((c) => c.def.color === 'red')).toBe(true)
  })
})

describe('威嚇の撤去 (2026-08-25: 延焼は純DoT)', () => {
  it('延焼を持つ敵の攻撃も実値そのまま (威嚇による軽減はない)', () => {
    let s = freshCombat('set-confirm', 'enemy_brute', 42, 'starter_red')
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, burn: 5, hp: 999 })) }
    s = withIntent(s, { kind: 'attack', shownMin: 10, shownMax: 10, actual: 10 })
    const hpBefore = s.player.hp
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.hp).toBe(hpBefore - 10)
  })

  it('pre窓の実値公開は素の実値を使う', () => {
    let s = freshCombat('set-confirm', 'enemy_brute', 42, 'starter_red')
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, burn: 6 })) }
    s = withIntent(s, { kind: 'attack', shownMin: 13, shownMax: 13, actual: 13 })
    const win = windowFromPending({
      ...s,
      pendingWindow: { enemyIndex: 0, stage: 'pre' },
    })
    expect(win).toEqual({ stage: 'pre', kind: 'attack', actual: 13 })
  })
})

describe('延焼 (バーン)', () => {
  it('延焼は蓄積し、敵フェーズ開始時にダメージ (ブロック無視) を与えて1減る', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_red'), [
      'red_ignite',
      'red_ignite',
    ])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_ignite' })
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_red_ignite' })
    expect(s.enemies[0].burn).toBe(8) // 4+4 (着火の延焼は4のまま)
    const hpAfterHits = s.enemies[0].hp
    s = withIntent(s, defendIntent(10)) // 敵はブロックを得るが延焼はブロック無視
    s = applyCommand(s, { type: 'EndTurn' })
    expect(types(s.eventLog)).toContain('BurnTick')
    expect(s.enemies[0].hp).toBe(hpAfterHits - 8) // 延焼8が素通し
    expect(s.enemies[0].burn).toBe(7) // 1減る
  })

  it('行動前に焼き切れば敵は動けない', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_red'), ['red_ignite'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_ignite' })
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, hp: 2 })) } // 延焼4で焼き切れる
    s = withIntent(s, { kind: 'attack', shownMin: 99, shownMax: 99, actual: 99 })
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.phase).toBe('won')
    expect(s.player.hp).toBe(s.player.maxHp) // 攻撃は実行されない
  })
})

describe('粉砕とランダム火力', () => {
  it('粉砕: 敵のブロックを全て破壊してからダメージが通る', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_turtle', 42, 'starter_red'), ['red_smash'])
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, block: 14 })) }
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_smash' })
    expect(types(s.eventLog)).toContain('BlockShattered')
    expect(s.enemies[0].block).toBe(0)
    expect(s.enemies[0].hp).toBe(hpBefore - 13) // 割った後は素通し (叩き割り 11→13)
  })

  it('ランダム火力: 範囲内のダメージで、同じシードなら同じ結果', () => {
    const play = () => {
      let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_red'), ['red_gamble'])
      const hpBefore = s.enemies[0].hp
      s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_gamble' })
      return hpBefore - s.enemies[0].hp
    }
    const dmg = play()
    expect(dmg).toBeGreaterThanOrEqual(2)
    expect(dmg).toBeLessThanOrEqual(16)
    expect(play()).toBe(dmg) // 決定論
  })
})

describe('刹那のリソース (儀式・衝動・自傷)', () => {
  it('猛火の儀式: 0マナでエナジー+2、消滅する', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_red'), ['red_ritual'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_ritual' })
    expect(s.player.energy).toBe(5)
    expect(s.player.exhaustPile).toHaveLength(1)
  })

  it('衝動: 山札の上から引いた札はこのターン中プレイでき、未使用ならターン終了時に消滅する', () => {
    let s = freshCombat('set-confirm', 'enemy_brute', 42, 'starter_red')
    s = withHand(s, ['red_impulse'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_impulse' })
    expect(s.player.impulseUids).toHaveLength(3)
    expect(s.player.hand).toHaveLength(3) // 衝動3枚が手札に
    const impulseCard = s.player.hand.find((c) => s.player.impulseUids.includes(c.uid))!
    const exhaustBefore = s.player.exhaustPile.length
    s = withIntent(s, defendIntent(3))
    s = applyCommand(s, { type: 'EndTurn' })
    // 未使用の衝動3枚は消滅の山へ
    expect(s.player.exhaustPile.length).toBe(exhaustBefore + 3)
    expect(s.player.exhaustPile.some((c) => c.uid === impulseCard.uid)).toBe(true)
    expect(s.player.impulseUids).toHaveLength(0)
  })

  it('捨て身の一撃: 14ダメージと引き換えにHP-3', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_red'), ['red_reckless'])
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_reckless' })
    expect(s.enemies[0].hp).toBe(hpBefore - 14)
    expect(s.player.hp).toBe(s.player.maxHp - 3)
    expect(types(s.eventLog)).toContain('HpLost')
  })
})

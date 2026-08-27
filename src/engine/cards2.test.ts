// 面白カード第2弾 (2026-08-25) のテスト。
// 確定済みルール表「伏せ破壊への罰」「自己誘発リアクション」「急所」「成長放出」「キル連鎖」を固定する。
import { describe, expect, it } from 'vitest'
import { applyCommand } from './state.ts'
import { attackIntent, destroySetIntent, freshCombat, withHand, withIntent } from './test-helpers.ts'
import type { GameState } from './types.ts'

describe('伏せ破壊への罰 (罠仕掛けの火薬)', () => {
  it('罠壊しに破壊されると敵全体に12ダメージが爆ぜる', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_set_breaker', 42, 'starter_red'), [
      'red_trap_powder',
    ])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_red_trap_powder' })
    s = withIntent(s, destroySetIntent())
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'EndTurn' })
    // 2026-08-27: 伏せ破壊への応答の窓が開くようになった。火薬は「壊させたい」札なので温存を選ぶ
    expect(s.phase).toBe('awaiting-reaction')
    s = applyCommand(s, { type: 'ConfirmReaction', fire: false })
    expect(s.enemies[0].hp).toBe(hpBefore - 12)
    expect(s.player.setCards).toHaveLength(0)
    expect(s.player.discardPile.some((c) => c.def.id === 'red_trap_powder')).toBe(true)
  })
})

describe('自己誘発リアクション', () => {
  it('追い打ちの罠: 伏せ中に攻撃カードをプレイすると起爆し、同じ敵に5ダメージ', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_red'), [
      'red_ambush_trap',
      'red_strike',
    ])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_red_ambush_trap' })
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_red_strike' })
    expect(s.enemies[0].hp).toBe(hpBefore - 6 - 5) // 火弾6 + 追い打ち5
    expect(s.player.setCards).toHaveLength(0) // 起爆後は捨て札へ
    expect(s.player.discardPile.some((c) => c.def.id === 'red_ambush_trap')).toBe(true)
  })

  it('反響の符: 呪文プレイで起爆し1ドロー+霊気1。物理では起爆しない', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), [
      'blue_echo_seal',
      'green_strike',
      'blue_ponder',
    ])
    s = { ...s, player: { ...s.player, energy: 9 } }
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_blue_echo_seal' })
    // 物理 (打撃) では起爆しない
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_green_strike' })
    expect(s.player.setCards).toHaveLength(1)
    // 呪文 (思案) で起爆
    const handBefore = s.player.hand.length
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't2_blue_ponder' })
    expect(s.player.aether).toBe(1)
    // 思案の2ドロー + 反響の1ドロー - プレイした思案1枚
    expect(s.player.hand.length).toBe(handBefore + 2 + 1 - 1)
    expect(s.player.setCards).toHaveLength(0)
  })
})

describe('急所 (敵版脆弱)', () => {
  it('急所突き: 6ダメージ+急所2。次の2回のダメージが+50%', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42), [
      'green_weak_point',
      'green_strike',
      'green_strike',
      'green_strike',
    ])
    s = { ...s, player: { ...s.player, energy: 9 } }
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_weak_point' })
    expect(s.enemies[0].hp).toBe(hpBefore - 6)
    expect(s.enemies[0].exposed).toBe(2)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_green_strike' })
    expect(s.enemies[0].hp).toBe(hpBefore - 6 - 9) // 6×1.5
    expect(s.enemies[0].exposed).toBe(1)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't2_green_strike' })
    expect(s.enemies[0].hp).toBe(hpBefore - 6 - 9 - 9)
    expect(s.enemies[0].exposed).toBe(0)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't3_green_strike' })
    expect(s.enemies[0].hp).toBe(hpBefore - 6 - 9 - 9 - 6) // 急所切れで通常
  })
})

describe('成長放出 (開花の蔦)', () => {
  it('成長×4ダメージ (成長加算込みで×5) を与え、成長が0になる', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42), ['green_bloom_lash'])
    s = { ...s, player: { ...s.player, growth: 4, energy: 9 } }
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_bloom_lash' })
    expect(s.enemies[0].hp).toBe(hpBefore - (4 * 4 + 4)) // 放出16 + 成長加算4
    expect(s.player.growth).toBe(0)
  })
})

describe('キル連鎖 (玉突き)', () => {
  it('対象を倒したら別の生存敵に同値が跳ねる', () => {
    let s = withHand(freshCombat('set-confirm', 'enc_probe_pair', 42, 'starter_red'), [
      'red_billiard',
    ])
    s = { ...s, enemies: s.enemies.map((e, i) => (i === 0 ? { ...e, hp: 5 } : e)) }
    const hp1 = s.enemies[1].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_billiard', targetIndex: 0 })
    expect(s.enemies[0].hp).toBeLessThanOrEqual(0)
    expect(s.enemies[1].hp).toBe(hp1 - 8) // 跳ねた
  })

  it('倒せなければ跳ねない', () => {
    let s = withHand(freshCombat('set-confirm', 'enc_probe_pair', 42, 'starter_red'), [
      'red_billiard',
    ])
    const hp1 = s.enemies[1].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_billiard', targetIndex: 0 })
    expect(s.enemies[1].hp).toBe(hp1)
  })
})

describe('先手の炎 (被攻撃前の先制ダメージ)', () => {
  it('pre窓で8ダメージ。攻撃自体はそのまま受ける (倒せなければ)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_red'), [
      'red_flinch_fire',
    ])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_red_flinch_fire' })
    s = withIntent(s, attackIntent(10))
    const playerHp = s.player.hp
    const enemyHp = s.enemies[0].hp
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.phase).toBe('awaiting-reaction') // pre窓
    s = applyCommand(s, { type: 'ConfirmReaction', fire: true })
    expect(s.enemies[0].hp).toBe(enemyHp - 8)
    expect(s.player.hp).toBe(playerHp - 10) // 威嚇は撤去済み: 素の10を受ける
  })

  it('先手の炎で敵を倒せば、その攻撃は発生しない (焼き切り)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_red'), [
      'red_flinch_fire',
    ])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_red_flinch_fire' })
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, hp: 5 })) }
    s = withIntent(s, attackIntent(10))
    const playerHp = s.player.hp
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.phase).toBe('awaiting-reaction')
    s = applyCommand(s, { type: 'ConfirmReaction', fire: true })
    expect(s.enemies[0].hp).toBeLessThanOrEqual(0)
    expect(s.player.hp).toBe(playerHp) // 攻撃は発生しない
  })
})

describe('茨の爆ぜ (全体返し)', () => {
  it('被攻撃後、生存する敵全体に返しが飛ぶ', () => {
    let s = withHand(freshCombat('set-confirm', 'enc_probe_pair', 42), ['green_thorn_burst'])
    s = { ...s, player: { ...s.player, energy: 9 } }
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_thorn_burst' })
    let s2: GameState = {
      ...s,
      enemies: s.enemies.map((e, i) =>
        i === 0
          ? { ...e, intent: { kind: 'attack', shownMin: 5, shownMax: 5, actual: 5 } }
          : { ...e, intent: { kind: 'defend', shownMin: 3, shownMax: 3, actual: 3 } },
      ),
    }
    const hp0 = s2.enemies[0].hp
    const hp1 = s2.enemies[1].hp
    s2 = applyCommand(s2, { type: 'EndTurn' })
    expect(s2.phase).toBe('awaiting-reaction')
    s2 = applyCommand(s2, { type: 'ConfirmReaction', fire: true })
    expect(s2.enemies[0].hp).toBe(hp0 - 5)
    expect(s2.enemies[1].hp).toBe(hp1 - 5)
  })
})

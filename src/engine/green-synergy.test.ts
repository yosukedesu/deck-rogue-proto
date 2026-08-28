// 緑のシナジー網拡張 (2026-08-29) のテスト。
// 「成長以外のアーキを選べるようにシナジー設計してカードを増やす」(ユーザー) を受けた
// トランプル/ビッグマナの網 (エンジン→倍加→刈り取り→換金) 8枚と新効果2つを固定する。
import { describe, expect, it } from 'vitest'
import { applyCommand } from './state.ts'
import { freshCombat, withHand } from './test-helpers.ts'
import type { GameState } from './types.ts'

function withEnergy(s: GameState, energy: number): GameState {
  return { ...s, player: { ...s.player, energy } }
}

describe('トランプルの網', () => {
  it('荒野の呼び声: 毎ターン開始時に勢い+3 (エンジン)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 42), ['green_perm_wild_call'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_perm_wild_call' })
    expect(s.player.momentum).toBe(0) // 置いたターンはまだ
    s = applyCommand(s, { type: 'EndTurn' })
    let guard = 0
    while (s.phase === 'awaiting-reaction' && guard++ < 10) {
      s = applyCommand(s, { type: 'ConfirmReaction', fire: false })
    }
    expect(s.player.momentum).toBe(3) // 次の自ターン開始時に+3
  })

  it('三連の角: 5×3の貫通。勢いと成長が各ヒットに乗る', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_turtle', 42), ['green_triple_horn'])
    s = { ...s, player: { ...s.player, momentum: 2, growth: 1 } }
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, block: 10 })) }
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_triple_horn' })
    expect(s.enemies[0].hp).toBe(hpBefore - (5 + 2 + 1) * 3) // 貫通なのでブロック無視
    expect(s.enemies[0].block).toBe(10)
  })

  it('怒涛の突き上げ: 勢い×3ダメージ (勢いは消費せず、自身にも勢いが乗る)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42), ['green_surge_thrust'])
    s = { ...s, player: { ...s.player, momentum: 4 } }
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_surge_thrust' })
    // 基礎 = 勢い4×3 = 12、そこに勢い4の加算も乗る = 16
    expect(s.enemies[0].hp).toBe(hpBefore - (4 * 3 + 4))
    expect(s.player.momentum).toBe(4) // 消費しない (ターン終了で自然に消える)
  })

  it('昂ぶる角笛: 勢い+2してから2倍・消滅 (2026-08-29 検証ランで空振り腐りが出たため+2を前置)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42), ['green_horn_flare'])
    s = { ...s, player: { ...s.player, momentum: 5 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_horn_flare' })
    expect(s.player.momentum).toBe((5 + 2) * 2)
    expect(s.player.exhaustPile.map((c) => c.def.id)).toContain('green_horn_flare')
    // 勢い0で引いても最低4が立つ = 空振りしない
    let s2 = withHand(freshCombat('set-confirm', 'enemy_brute', 42), ['green_horn_flare'])
    s2 = applyCommand(s2, { type: 'PlayCard', cardUid: 't0_green_horn_flare' })
    expect(s2.player.momentum).toBe(4)
  })

  it('荒角の構え: ブロック6+勢い+3 (トランプルの受け=守りが攻めの準備になる)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42), [
      'green_horn_stance',
      'green_strike',
    ])
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_horn_stance' })
    expect(s.player.block).toBe(6)
    expect(s.player.momentum).toBe(3)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_green_strike' })
    expect(s.enemies[0].hp).toBe(hpBefore - (6 + 3)) // 防御札が後続の攻撃を+3する
  })
})

describe('ビッグマナの網', () => {
  it('幹撃: エナジー上限×3ダメージ (中型ペイオフ)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42), ['green_trunk_blow'])
    s = { ...s, player: { ...s.player, energyMax: 5, energy: 5 } }
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_trunk_blow' })
    expect(s.enemies[0].hp).toBe(hpBefore - 5 * 3)
  })

  it('大幹の構え: 上限×2ダメ+上限×2ブロックの攻防一体 (ランプ中の無防備への回答)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42), ['green_trunk_stance'])
    s = { ...s, player: { ...s.player, energyMax: 4, energy: 4 } }
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_trunk_stance' })
    expect(s.enemies[0].hp).toBe(hpBefore - 4 * 2)
    expect(s.player.block).toBe(4 * 2)
  })

  it('大樹の脈: 毎ターン開始時に次のカード-1 (らいこパッシブと同機構)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 42), ['green_perm_tree_pulse'])
    s = withEnergy(s, 3)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_perm_tree_pulse' })
    s = applyCommand(s, { type: 'EndTurn' })
    let guard = 0
    while (s.phase === 'awaiting-reaction' && guard++ < 10) {
      s = applyCommand(s, { type: 'ConfirmReaction', fire: false })
    }
    expect(s.player.nextCardDiscount).toBe(1) // 次の自ターン開始時に割引1
  })

  it('木陰の守り: 上限×2ブロック (巨木の盾の小型ラダー)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42), ['green_canopy_shade'])
    s = { ...s, player: { ...s.player, energyMax: 4, energy: 4 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_canopy_shade' })
    expect(s.player.block).toBe(4 * 2)
  })
})

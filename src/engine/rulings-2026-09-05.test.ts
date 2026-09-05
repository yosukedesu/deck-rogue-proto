// 2026-09-05 の裁定 (Opusラン U の答え合わせ): ①勢いはカードプレイのダメージだけに乗る (置物トリガー・リアクションには乗らない)
// ②虚弱もカードプレイのブロックだけ (カードのプレイ中に誘発した置物のブロックは減らない) ③罠壊しの通常攻撃は育つ技 (+3/回)
import { describe, expect, it } from 'vitest'
import { getEnemyDef } from './content.ts'
import { applyCommand } from './state.ts'
import { freshCombat, withHand } from './test-helpers.ts'

describe('置物トリガーのダメージと勢い (2026-09-05 裁定)', () => {
  it('風の棘 (勢いを得るたび2ダメ) には成長は乗るが勢いは乗らない。カードのヒットには両方乗る', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 42), ['green_perm_wind_thorn', 'green_trample_charge'])
    s = { ...s, player: { ...s.player, growth: 2 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_perm_wind_thorn' })
    const hp0 = s.enemies[0].hp
    // 突進の助走: 勢い+3 → 風の棘 2+成長2 (勢いは乗らない) = 4 / 2ダメ×2 は 2+成長2+勢い3 = 7 ずつ
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_green_trample_charge', targetIndex: 0 })
    expect(hp0 - s.enemies[0].hp).toBe(4 + 7 + 7)
    expect(s.player.momentum).toBe(3)
  })

  it('虚弱中でも、カードのプレイ中に誘発した置物のブロック (棘の蔓) は25%減を受けない', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 42), ['green_perm_thorn_vine', 'green_strike'])
    s = { ...s, player: { ...s.player, frail: 2 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_perm_thorn_vine' })
    const b0 = s.player.block
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_green_strike', targetIndex: 0 })
    expect(s.player.block - b0).toBe(2)
  })
})

describe('罠壊しの締切 (2026-09-05 裁定: ターン装甲は締切と対で配る)', () => {
  it('通常攻撃 smash は宣言するたび+3 (3テーブル共通の id なので成長カウンタも共有)', () => {
    const def = getEnemyDef('enemy_set_breaker')
    expect(def.turnArmor).toBe(35)
    for (const tbl of [def.moves, def.movesVsSet ?? [], def.movesVsTokens ?? []]) {
      const smash = tbl.find((m) => m.id === 'smash')
      expect(smash?.growPerUse).toBe(3)
    }
  })
})

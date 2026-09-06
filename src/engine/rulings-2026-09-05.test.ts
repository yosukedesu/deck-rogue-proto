// 2026-09-05 の裁定 (Opusラン U の答え合わせ): ①勢いはカードプレイのダメージだけに乗る (置物トリガー・リアクションには乗らない)
// ②虚弱もカードプレイのブロックだけ (カードのプレイ中に誘発した置物のブロックは減らない) ③罠壊しの通常攻撃は育つ技 (+3/回)
import { describe, expect, it } from 'vitest'
import { getEnemyDef } from './content.ts'
import { applyCommand } from './state.ts'
import { freshCombat, withHand } from './test-helpers.ts'

describe('置物トリガーのダメージと勢い (2026-09-05 裁定)', () => {
  it('置物トリガーのダメージ (棘葉の茂み=成長を得るたび2ダメ) には成長は乗るが勢いは乗らない。カードのヒットには両方乗る (風の棘は2026-09-07に疾風の蔓へ作り直し)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 42), ['green_perm_thorn_leaves', 'green_growth_ring', 'green_strike'])
    s = { ...s, player: { ...s.player, momentum: 3, energy: 5 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_perm_thorn_leaves' })
    const hp0 = s.enemies[0].hp
    // 年輪: 成長+2 → 棘葉の茂み 2+成長2 (勢い3は乗らない) = 4 / 打撃は 6+成長2+勢い3 = 11
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_green_growth_ring' })
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't2_green_strike', targetIndex: 0 })
    expect(hp0 - s.enemies[0].hp).toBe(4 + 11)
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

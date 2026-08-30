// 猛り火 (2026-08-30。赤のカラーパイ再編 Phase 2) のテスト。
// ユーザー判断: キーワード=猛り火 / 参照先=**生存する敵の延焼の合計** / しきい値=単一の8 /
// おまけは全体火力だけでなく火力強化・コスト軽減・ドロー・防御・急所・勢いなど何でも載る。
import { describe, expect, it } from 'vitest'
import { BLAZE_THRESHOLD, blazeTotal, effectiveCost, isBlazing } from './effects.ts'
import { allCards, getCardDef } from './content.ts'
import { applyCommand } from './state.ts'
import { attackIntent, freshCombat, withHand, withIntent } from './test-helpers.ts'
import type { GameState } from './types.ts'

const withBurn = (s: GameState, ...burns: number[]): GameState => ({
  ...s,
  enemies: s.enemies.map((e, i) => ({ ...e, burn: burns[i] ?? 0 })),
})
const withEnergy = (s: GameState, energy: number): GameState => ({
  ...s,
  player: { ...s.player, energy },
})

describe('猛り火のしきい値', () => {
  it('しきい値は全札で単一の8 (カードごとに変えない = 8ひとつ覚えれば全札が読める)', () => {
    expect(BLAZE_THRESHOLD).toBe(8)
  })

  it('延焼が8未満なら猛り火の効果は解決されない', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_red'), [
      'red_blaze_strike',
    ])
    s = withBurn(s, 7)
    expect(isBlazing(s)).toBe(false)
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_blaze_strike' })
    expect(s.enemies[0].hp).toBe(hpBefore - 5) // 素の5だけ
  })

  it('延焼が8以上なら猛り火の効果が乗る', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_red'), [
      'red_blaze_strike',
    ])
    s = withBurn(s, 8)
    expect(isBlazing(s)).toBe(true)
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_blaze_strike' })
    expect(s.enemies[0].hp).toBe(hpBefore - 12) // 5 + 猛り火7
  })

  it('参照するのは**敵全体の延焼の合計** (4+4の2体でも点く)', () => {
    let s = freshCombat('set-confirm', 'enc_probe_pair', 42, 'starter_red')
    expect(s.enemies.length).toBe(2)
    s = withBurn(s, 4, 4)
    expect(blazeTotal(s)).toBe(8)
    expect(isBlazing(s)).toBe(true)
  })

  it('倒れた敵の延焼は数えない (合計から外れる)', () => {
    let s = freshCombat('set-confirm', 'enc_probe_pair', 42, 'starter_red')
    s = withBurn(s, 6, 6)
    expect(isBlazing(s)).toBe(true)
    s = { ...s, enemies: s.enemies.map((e, i) => (i === 1 ? { ...e, hp: 0 } : e)) }
    expect(blazeTotal(s)).toBe(6)
    expect(isBlazing(s)).toBe(false)
  })

  it('同じカードの中で先に点けたら後の効果に乗る (着火は延焼4を撒いてから自分で判定する)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_red'), ['red_ignite'])
    s = withBurn(s, 4) // 着火の延焼4を足すとちょうど8 = 自分で火を継ぎ足して点く
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_ignite' })
    expect(s.enemies[0].burn).toBe(8)
    expect(s.enemies[0].hp).toBe(hpBefore - 6) // 素の3 + 猛り火3
  })
})

describe('猛り火のおまけ (全体火力だけではない)', () => {
  it('コスト軽減: 焔纏いの刃は猛り火中だけ0マナになる', () => {
    const card = { uid: 'x', def: getCardDef('red_blaze_blade') }
    let s = freshCombat('set-confirm', 'enemy_brute', 42, 'starter_red')
    expect(effectiveCost(withBurn(s, 0), card)).toBe(1)
    expect(effectiveCost(withBurn(s, 8), card)).toBe(0)
    // 手札もエナジーも増やさないので撃つたび手札が1枚減る = 停止する (0マナ規約)
    expect(getCardDef('red_blaze_blade').effects.every((e) => e.effect === 'dealDamage')).toBe(true)
  })

  it('ドロー: 火喰らいは猛り火中だけ2枚引く', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_red'), [
      'red_blaze_devour',
    ])
    s = withBurn(s, 8)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_blaze_devour' })
    expect(s.player.hand).toHaveLength(2)
  })

  it('防御: 灰の外套は猛り火中のターンだけブロックを得る (赤の受けを延焼に接続する)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_red'), [
      'red_blaze_cloak',
    ])
    s = withEnergy(s, 9)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_red_blaze_cloak' })
    // 延焼0のままターンを回すとブロックは付かない
    s = withIntent(s, attackIntent(1))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.block).toBe(0)
    // 延焼を積んでからターンを回すとブロック9
    s = withBurn(s, 12)
    s = withIntent(s, attackIntent(1))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.block).toBeGreaterThanOrEqual(9)
  })
})

describe('猛り火の規約', () => {
  it('猛り火を持つ札はすべて赤にある (延焼は赤の専売なので他色に配らない)', () => {
    const blazing = getBlazeCards()
    expect(blazing.length).toBeGreaterThan(0)
    expect(blazing.every((c) => c.color === 'red')).toBe(true)
  })

  it('猛り火のコスト軽減は素のコストを下回らせない (0マナ+補充の抜け道を作らない)', () => {
    const REFILL = new Set(['drawCards', 'impulseDraw', 'retrieveFromExhaust', 'playFromExhaust'])
    for (const c of getBlazeCards()) {
      if ((c.blazeDiscount ?? 0) === 0) continue
      const net = c.cost - (c.blazeDiscount ?? 0)
      if (net > 0) continue
      // 実効0マナになる札は手札もエナジーも増やしてはいけない
      const refills = c.effects.some((e) => REFILL.has(e.effect) || e.effect === 'gainEnergy')
      expect(refills, `${c.name} が実効0マナで補充を持つ`).toBe(false)
    }
  })
})

function getBlazeCards() {
  return allCards.filter(
    (c) => c.blazeDiscount !== undefined || c.effects.some((e) => e.condition?.blaze === true),
  )
}

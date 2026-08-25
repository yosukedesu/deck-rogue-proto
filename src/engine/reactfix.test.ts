// プレイテスト由来の修正 (2026-08-25) のテスト。
// 確定済みルール表「条件付き意図」「致死時の誘発」「がらくた」を固定する。
import { describe, expect, it } from 'vitest'
import { effectiveIntent } from './effects.ts'
import { applyCommand } from './state.ts'
import { freshCombat, withHand, withIntent } from './test-helpers.ts'

describe('条件付き意図 (敵の反応が実行時の盤面で決まる)', () => {
  it('罠壊しは「伏せがあれば伏せ破壊 / なければ通常行動」の両分岐を宣言時に持つ', () => {
    const s = freshCombat('set-confirm', 'enemy_set_breaker', 42, 'starter_red')
    const intent = s.enemies[0].intent!
    expect(intent.conditionalOn).toBe('set')
    expect(intent.alt).toBeDefined()
    // 伏せが無い今は本体の分岐が有効
    expect(effectiveIntent(s, 0)!.kind).toBe(intent.kind)
  })

  it('同じターンに伏せると、その行動が即座に反応テーブル側へ切り替わる', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_set_breaker', 42, 'starter_red'), [
      'red_reaction_flareback',
    ])
    const before = effectiveIntent(s, 0)!.kind
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_red_reaction_flareback' })
    const after = effectiveIntent(s, 0)!
    // alt (反応テーブル) 側に切り替わっている = 「今から伏せて誘導」が成立する
    expect(after.kind).toBe(s.enemies[0].intent!.alt!.kind)
    void before
  })
})

describe('致死時の誘発 (回復付きの返し札で生き延びる)', () => {
  it('致死ダメージでも被攻撃後の確認窓が開き、ドレイン返しで生存できる', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_black'), [
      'black_reaction_last_stand',
    ])
    s = { ...s, player: { ...s.player, hp: 8 } } // HP半分以下 = 死中の活の条件を満たす
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_black_reaction_last_stand' })
    s = withIntent(s, { kind: 'attack', shownMin: 10, shownMax: 10, actual: 10 })
    s = applyCommand(s, { type: 'EndTurn' })
    // HPは0以下になっているが、窓が開いている
    expect(s.phase).toBe('awaiting-reaction')
    s = applyCommand(s, { type: 'ConfirmReaction', fire: true })
    // 死中の活: 20ダメ + 10回復 → HP-2 から生還
    expect(s.player.hp).toBeGreaterThan(0)
    expect(s.phase).not.toBe('lost')
  })

  it('温存を選べばそのまま敗北になる', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_black'), [
      'black_reaction_last_stand',
    ])
    s = { ...s, player: { ...s.player, hp: 8 } }
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_black_reaction_last_stand' })
    s = withIntent(s, { kind: 'attack', shownMin: 10, shownMax: 10, actual: 10 })
    s = applyCommand(s, { type: 'EndTurn' })
    s = applyCommand(s, { type: 'ConfirmReaction', fire: false })
    expect(s.phase).toBe('lost')
  })
})

describe('がらくた (罠壊しの第2の特徴)', () => {
  it('山札に混ざり、使用不可の死に札として手札を圧迫する', () => {
    let s = freshCombat('set-confirm', 'enemy_set_breaker', 42, 'starter_red')
    s = withIntent(s, {
      kind: 'attack',
      shownMin: 5,
      shownMax: 7,
      actual: 6,
      inflict: { status: 'junk', amount: 2 },
    })
    const drawBefore = s.player.drawPile.length
    s = applyCommand(s, { type: 'EndTurn' })
    const junkInDeck = [...s.player.drawPile, ...s.player.hand].filter(
      (c) => c.def.id === 'status_junk',
    )
    expect(junkInDeck.length).toBe(2)
    expect(s.player.drawPile.length + s.player.hand.length).toBeGreaterThan(drawBefore)
    expect(s.eventLog.some((e) => e.type === 'StatusInflicted' && e.status === 'junk')).toBe(true)
  })
})

describe('弱体の下限 (チップダメージが消えない)', () => {
  it('弱体中でも1ダメージの置物効果は0にならない', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_white'), [
      'white_perm_squire',
    ])
    s = { ...s, player: { ...s.player, weak: 2 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_white_perm_squire' })
    const hpBefore = s.enemies[0].hp
    s = withIntent(s, { kind: 'attack', shownMin: 3, shownMax: 3, actual: 3 })
    s = applyCommand(s, { type: 'EndTurn' })
    // 従者の2ダメは floor(2*0.75)=1 に減るが 0 にはならない
    expect(s.enemies[0].hp).toBe(hpBefore - 1)
  })
})

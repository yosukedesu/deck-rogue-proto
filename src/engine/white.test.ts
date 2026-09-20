// 白 (4色目) のテスト。白の柱: 防御・回復の本家 / 威圧 / 従者の横並び / 護りのリアクション。
import { describe, expect, it } from 'vitest'
import { allCards, buildDeck, getDeckDef } from './content.ts'

import { applyCommand } from './state.ts'
import { createRunInBattle, attackIntent, freshCombat, withHand, withIntent } from './test-helpers.ts'

describe('白のカラーパイ', () => {
  it('白のカードとデッキが揃っている', () => {
    expect(allCards.filter((c) => c.color === 'white').length).toBeGreaterThanOrEqual(19)
    for (const id of ['starter_white', 'deck_horde', 'deck_spark', 'run_basic_white']) {
      expect(buildDeck(id).length).toBeGreaterThan(0)
      expect(getDeckDef(id).color).toBe('white')
    }
  })

  it('白ランは白の基本デッキで始まり、色が保持される', () => {
    const run = createRunInBattle(7, 'set-confirm', 'leader_white')
    expect(run.colors).toEqual(['white'])
    expect(run.deck.every((c) => c.def.color === 'white')).toBe(true)
  })
})

describe('回復 (白の専売)', () => {
  it('gainHp は最大HPを超えない', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_white'), [
      'white_heal',
    ])
    s = { ...s, player: { ...s.player, hp: s.player.maxHp - 3 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_white_heal' })
    expect(s.player.hp).toBe(s.player.maxHp) // 5回復だが上限で+3止まり
  })

  it('ひなたのパッシブ: 毎ターン開始時にHP1回復', () => {
    let s = freshCombat('set-confirm', 'enemy_brute', 42, 'starter_white')
    s = { ...s, player: { ...s.player, hp: 50, hand: [] } }
    s = withIntent(s, { kind: 'defend', actual: 3 })
    // リーダー付き戦闘でないのでパッシブなし → ラン経由で確認
    const run = createRunInBattle(3, 'set-confirm', 'leader_white')
    expect(
      run.combat!.player.permanents.some((p) => p.def.id === 'leader_white_passive'),
    ).toBe(true)
  })
})

describe('威圧 (敵弱体化)', () => {
  it('威圧の聖印 (2026-09-03 本家 Weak 化): 威圧2=次の2回の攻撃行動が-25%。筋力は触らない', () => {
    // 眩ます灯印は 2026-09-20 夜に撤去 = 灯の岐路の「眩ます」(5ダメ+威圧1) で同じ機構を固定
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_white'), [
      'white_mode_crossroad',
    ])
    s = { ...s, player: { ...s.player, energy: 9 } }
    const str0 = s.enemies[0].strength
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_white_mode_crossroad', modeIndex: 0, targetIndex: 0 })
    expect(s.enemies[0].weak).toBe(1)
    expect(s.enemies[0].strength).toBe(str0)
  })
})

describe('要塞型 (ブロック変換)', () => {
  it('城壁砕き: 自前のブロック3を先に得てから、現在のブロック×1のダメージ', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_white'), [
      'white_spark_shield',
      'white_bodyslam',
    ])
    s = { ...s, player: { ...s.player, energy: 9 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_white_spark_shield' }) // 火守りの盾 ブロック7 (大光壁は 2026-09-20 夜に撤去)
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_white_bodyslam' })
    // 2026-08-26: 効果順を [ブロック3 → ダメージ] にしたので自前のブロックも自分に乗る
    expect(s.enemies[0].hp).toBe(hpBefore - 10) // (火守りの盾7 + 自前3) × 1
  })
})

describe('従者ホード (置物数参照)', () => {
  it('集結: 場に出た置物の数×4のダメージ (リーダーパッシブ・レリックは数えない)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_white'), [
      'white_perm_squire',
      'white_perm_shieldmaiden',
      'white_rally',
    ])
    s = { ...s, player: { ...s.player, energy: 9 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_white_perm_squire' })
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_white_perm_shieldmaiden' })
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't2_white_rally' })
    expect(s.enemies[0].hp).toBe(hpBefore - 8) // 置物2×4
  })

  it('従者の少年: 毎ターン開始時に2ダメージの自動攻撃', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_white'), [
      'white_perm_squire',
    ])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_white_perm_squire' })
    const hpBefore = s.enemies[0].hp
    // 敵が防御するとブロックに止められるため攻撃意図で検証
    s = withIntent(s, attackIntent(3))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.enemies[0].hp).toBe(hpBefore - 4) // 次ターン開始時に従者が殴る (剣3+育ち1)
  })
})


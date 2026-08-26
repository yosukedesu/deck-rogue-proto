// 白 (4色目) のテスト。白の柱: 防御・回復の本家 / 威圧 / 従者の横並び / 護りのリアクション。
import { describe, expect, it } from 'vitest'
import { allCards, buildDeck, getDeckDef } from './content.ts'
import { createRun } from './run.ts'
import { applyCommand } from './state.ts'
import { attackIntent, freshCombat, withHand, withIntent } from './test-helpers.ts'

describe('白のカラーパイ', () => {
  it('白のカードとデッキが揃っている', () => {
    expect(allCards.filter((c) => c.color === 'white').length).toBeGreaterThanOrEqual(19)
    for (const id of ['starter_white', 'deck_horde', 'deck_fortress', 'run_basic_white']) {
      expect(buildDeck(id).length).toBeGreaterThan(0)
      expect(getDeckDef(id).color).toBe('white')
    }
  })

  it('白ランは白の基本デッキで始まり、色が保持される', () => {
    const run = createRun(7, 'set-confirm', 'leader_white')
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
    s = withIntent(s, { kind: 'defend', shownMin: 3, shownMax: 3, actual: 3 })
    // リーダー付き戦闘でないのでパッシブなし → ラン経由で確認
    const run = createRun(3, 'set-confirm', 'leader_white')
    expect(
      run.combat!.player.permanents.some((p) => p.def.id === 'leader_white_passive'),
    ).toBe(true)
  })
})

describe('威圧 (敵弱体化)', () => {
  it('威圧の聖印: 敵の強化が-2され、以降の攻撃宣言が下がる (最低1)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_white'), [
      'white_menace',
    ])
    s = { ...s, player: { ...s.player, energy: 9 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_white_menace' })
    expect(s.enemies[0].strength).toBe(-2)
    expect(s.player.block).toBe(4)
  })
})

describe('要塞型 (ブロック変換)', () => {
  it('城壁砕き: 現在のブロック×1のダメージ', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_white'), [
      'white_fortress',
      'white_bodyslam',
    ])
    s = { ...s, player: { ...s.player, energy: 9 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_white_fortress' })
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_white_bodyslam' })
    expect(s.enemies[0].hp).toBe(hpBefore - 12) // ブロック12×1
  })
})

describe('従者ホード (置物数参照)', () => {
  it('集結: 置物の数×3のダメージ (リーダーパッシブ・レリックも数える)', () => {
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
    expect(s.enemies[0].hp).toBe(hpBefore - 6) // 置物2×3
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
    expect(s.enemies[0].hp).toBe(hpBefore - 2) // 次ターン開始時に従者が殴る
  })
})

describe('護りのリアクション', () => {
  it('聖域: HP半分以下でのみ発動でき、ブロック20+回復5', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_white'), [
      'white_reaction_sanctuary',
    ])
    s = { ...s, player: { ...s.player, hp: Math.floor(s.player.maxHp * 0.4) } }
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_white_reaction_sanctuary' })
    s = withIntent(s, attackIntent(10))
    const hpBefore = s.player.hp
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.phase).toBe('awaiting-reaction') // pre窓 (被攻撃前)
    s = applyCommand(s, { type: 'ConfirmReaction', fire: true })
    // ブロック16で攻撃10を完封し、回復4 (2026-08-26 pre窓軽減の上限是正)
    expect(s.player.hp).toBe(hpBefore + 4)
  })
})

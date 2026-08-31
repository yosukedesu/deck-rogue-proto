// 青の完成回 (2026-08-31)。新柱2本 (抱え込み=手札参照 / 反復=呪文コピー) と
// 統合パーミッション (消して稼いで放つ) の機構を固定する。
import { describe, expect, it } from 'vitest'
import { applyCommand } from './state.ts'
import { attackIntent, freshCombat, withHand, withIntent } from './test-helpers.ts'
import type { GameState } from './types.ts'

const withEnergy = (s: GameState, energy: number): GameState => ({
  ...s,
  player: { ...s.player, energy },
})

describe('抱え込み (手札参照)', () => {
  it('知恵の重みは解決時の手札×2ダメージ (プレイした自身は数えない)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), [
      'blue_weight_of_wisdom',
      'blue_strike',
      'blue_strike',
      'blue_guard',
    ])
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_blue_weight_of_wisdom' })
    // 手札4枚 → 自身が抜けて3枚 × 2 = 6ダメージ
    expect(s.enemies[0].hp).toBe(hpBefore - 6)
  })

  it('懐深き外套は毎ターン開始時に手札×1の氷壁 (抱えるほど守れる)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), [
      'blue_perm_deep_cloak',
    ])
    s = withEnergy(s, 9)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_blue_perm_deep_cloak' })
    s = withIntent(s, attackIntent(0))
    const iceBefore = s.player.iceBlock
    s = applyCommand(s, { type: 'EndTurn' }) // 敵フェーズ → 次の自ターン開始 (5枚ドロー後に誘発)
    expect(s.player.iceBlock - iceBefore).toBe(s.player.hand.length)
  })
})

describe('反復 (呪文コピー)', () => {
  it('反復トークンがあると次の呪文の効果が2回解決される (トークンは1つ消費)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), [
      'blue_echo',
      'blue_strike', // 5ダメ+1ドロー
    ])
    s = withEnergy(s, 9)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_blue_echo' })
    expect(s.player.spellEchoes).toBe(1)
    const hpBefore = s.enemies[0].hp
    const handBefore = s.player.hand.length
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_blue_strike' })
    expect(s.enemies[0].hp).toBe(hpBefore - 10) // 5ダメ×2
    expect(s.player.hand.length).toBe(handBefore - 1 + 2) // 1ドロー×2
    expect(s.player.spellEchoes).toBe(0)
  })

  it('詠唱数・プレイ誘発は1回のまま (プレイは1回。効果だけが2回)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), [
      'blue_echo',
      'blue_tide_drop',
    ])
    s = withEnergy(s, 9)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_blue_echo' })
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_blue_tide_drop' })
    expect(s.player.cardsPlayedThisTurn).toBe(2) // 反復1 + 潮の一滴1 (コピーは数えない)
  })

  it('反復札自身が反復されるとトークン2つが立つ (消費は解決前)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), [
      'blue_echo',
      'blue_echo',
    ])
    s = withEnergy(s, 9)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_blue_echo' })
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_blue_echo' }) // 1消費 → +1×2回
    expect(s.player.spellEchoes).toBe(2)
  })

  it('未使用の反復トークンは自ターン終了時に消える / 敵フェーズに得た分は次の自ターンまで持つ', () => {
    // 谺の構え: 被攻撃後に反復+1 (敵フェーズ中の獲得)
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), [
      'blue_echo',
      'blue_echo_stance',
    ])
    s = withEnergy(s, 9)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_blue_echo' }) // 自ターンの獲得
    s = applyCommand(s, { type: 'SetCard', cardUid: 't1_blue_echo_stance' })
    s = withIntent(s, attackIntent(5))
    s = applyCommand(s, { type: 'EndTurn' })
    if (s.phase === 'awaiting-reaction') s = applyCommand(s, { type: 'ConfirmReaction', fire: true })
    // 自ターン終了時に反復(1)は消え、敵フェーズの谺の構え(+1)だけが残る
    expect(s.player.spellEchoes).toBe(1)
    expect(s.phase).toBe('player-turn')
  })

  it('リアクション (呪文でない) の解決は反復の対象にならない', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), [
      'blue_echo',
      'blue_frost_veil', // 反応: 被攻撃前に氷壁7+霊気1
    ])
    s = withEnergy(s, 9)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_blue_echo' })
    s = applyCommand(s, { type: 'SetCard', cardUid: 't1_blue_frost_veil' })
    s = withIntent(s, attackIntent(5))
    s = applyCommand(s, { type: 'EndTurn' })
    if (s.phase === 'awaiting-reaction') s = applyCommand(s, { type: 'ConfirmReaction', fire: true })
    expect(s.player.aether).toBe(1) // 2回解決なら2になっているはず
  })
})

describe('統合パーミッション (消して稼いで放つ)', () => {
  it('逆巻きは行動値12以上のみ打ち消せて霊気+2 (マナ漏出≤15との鏡像)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), [
      'blue_undertow',
    ])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_blue_undertow' })
    s = withIntent(s, attackIntent(14))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.phase).toBe('awaiting-reaction')
    const hpBefore = s.player.hp
    s = applyCommand(s, { type: 'ConfirmReaction', fire: true })
    expect(s.player.hp).toBe(hpBefore) // 打ち消しで被弾なし
    expect(s.player.aether).toBe(2)
  })

  it('渦電の輪: 打ち消しに成功するたび霊気+2 (稼ぎの接着剤)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), [
      'blue_perm_vortex_ring',
      'blue_counterspell',
    ])
    s = withEnergy(s, 9)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_blue_perm_vortex_ring' })
    s = applyCommand(s, { type: 'SetCard', cardUid: 't1_blue_counterspell' })
    s = withIntent(s, attackIntent(8))
    s = applyCommand(s, { type: 'EndTurn' })
    if (s.phase === 'awaiting-reaction') s = applyCommand(s, { type: 'ConfirmReaction', fire: true })
    expect(s.player.aether).toBe(1 + 2) // 対抗呪文の+1 + 渦電の輪の+2
  })

  it('霊気の槍は霊気×4ダメージを与えて全消費する (出口のコモン化)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), [
      'blue_aether_lance',
    ])
    s = { ...s, player: { ...s.player, aether: 3 } }
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_blue_aether_lance' })
    expect(s.enemies[0].hp).toBe(hpBefore - 12)
    expect(s.player.aether).toBe(0)
  })
})

describe('焚べる (addCasts 2026-08-31 ストーム構造難の処方)', () => {
  it('詠唱数+2は参照に乗るが、累計プレイ数 (激昂タイマー) には数えない', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), [
      'blue_kindle_chant',
      'blue_guard',
      'blue_guard',
      'blue_storm_lash',
    ])
    s = withEnergy(s, 9)
    const totalBefore = s.player.cardsPlayedTotal
    s = applyCommand(s, {
      type: 'PlayCard',
      cardUid: 't0_blue_kindle_chant',
      discardUids: ['t1_blue_guard', 't2_blue_guard'],
    })
    expect(s.player.cardsPlayedThisTurn).toBe(1 + 2) // 自身1 + 焚べ2
    expect(s.player.cardsPlayedTotal).toBe(totalBefore + 1) // 激昂タイマーは実プレイのみ
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't3_blue_storm_lash' })
    expect(s.enemies[0].hp).toBe(hpBefore - 9) // 詠唱数3 ×3
  })
})

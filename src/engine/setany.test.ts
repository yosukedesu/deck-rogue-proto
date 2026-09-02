// 実験「全カード伏せ可」(2026-09-02 ユーザー裁定) の機械固定。engine/setany.ts
import { describe, expect, it } from 'vitest'
import { getCardDef } from './content.ts'
import { canSetAsNormal, setWindowStage } from './setany.ts'
import { applyCommand } from './state.ts'
import { attackIntent, freshCombat, withHand, withIntent } from './test-helpers.ts'
import type { GameState } from './types.ts'

const anyOn = (s: GameState): GameState => ({ ...s, setAnyCards: true })
const types = (log: GameState['eventLog']) => log.map((e) => e.type)

describe('伏せ可否', () => {
  it('フラグ無しでは通常カードは伏せられない (現行仕様)', () => {
    const s = withHand(freshCombat('set-confirm', 'enemy_probe', 21), ['green_strike'])
    expect(() => applyCommand(s, { type: 'SetCard', cardUid: 't0_green_strike' })).toThrow(/リアクションタイプのみ/)
  })
  it('対象外: X・モード・追加コスト・ドロー/マナ系・置物', () => {
    for (const id of ['green_x_vine_flurry', 'green_entangle', 'green_leaf_blade', 'green_leaf_strike', 'green_ramp_sprout', 'green_perm_growth_tree', 'green_flash_insight']) {
      expect(canSetAsNormal(getCardDef(id)), id).toBe(false)
    }
    for (const id of ['green_strike', 'green_guard', 'green_fang', 'green_bark_armor', 'green_sweep']) {
      expect(canSetAsNormal(getCardDef(id)), id).toBe(true)
    }
    expect(setWindowStage(getCardDef('green_strike'))).toBe('post')
    expect(setWindowStage(getCardDef('green_guard'))).toBe('pre')
    expect(setWindowStage(getCardDef('green_bark_armor'))).toBe('pre') // ブロック14+成長1 = 守り側
  })
})

describe('攻撃札の伏せ: 1Eで伏せ、被攻撃後に印字コストを払って返す', () => {
  it('伏せは1E。発動には持ち越したエナジーが要り、払えなければ窓は開かず温存される', () => {
    let s = anyOn(withHand(freshCombat('set-confirm', 'enemy_probe', 22), ['green_strike']))
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_strike' })
    expect(s.player.energy).toBe(2) // 3-1
    expect(s.player.setCards).toHaveLength(1)
    // 残り2E → 発動コスト1Eは払える → 窓が開く
    s = withIntent(s, attackIntent(5))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.phase).toBe('awaiting-reaction')
    const hp0 = s.enemies[0].hp
    s = applyCommand(s, { type: 'ConfirmReaction', fire: true })
    expect(hp0 - s.enemies[0].hp).toBe(6) // 打撃6を行動してきた敵へ
    expect(s.player.setCards).toHaveLength(0)
    expect(types(s.eventLog)).toContain('ReactionTriggered')
    // 発動後の札は元の定義のまま捨て札へ (次に引いて普通にプレイできる)
    const found = [...s.player.discardPile, ...s.player.hand, ...s.player.drawPile].find((c) => c.uid === 't0_green_strike')
    expect(found?.def.type).toBe('physical')
  })
  it('エナジーを残していなければ発動できない (窓が開かない)', () => {
    let s = anyOn(withHand(freshCombat('set-confirm', 'enemy_probe', 23), ['green_strike', 'green_guard', 'green_guard']))
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_strike' }) // 1E
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_green_guard' }) // 1E
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't2_green_guard' }) // 1E → 残り0
    expect(s.player.energy).toBe(0)
    s = withIntent(s, attackIntent(5))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.phase).toBe('player-turn') // 窓は開かず敵フェーズが完走
    expect(s.player.setCards).toHaveLength(1) // 温存されたまま
    expect(types(s.eventLog)).not.toContain('ReactionTriggered')
    expect(types(s.eventLog)).toContain('ReactionUnaffordable') // 理由がログに残る
  })
  it('通常札の伏せ発動は onReactionFired を誘発しない (狩人の眼光の換金は専用リアクションの特権)', () => {
    let s = anyOn(withHand(freshCombat('set-confirm', 'enemy_probe', 24), ['green_perm_hunters_gaze', 'green_strike']))
    s = { ...s, player: { ...s.player, energy: 5 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_perm_hunters_gaze' })
    s = applyCommand(s, { type: 'SetCard', cardUid: 't1_green_strike' })
    const g0 = s.player.growth
    s = withIntent(s, attackIntent(5))
    s = applyCommand(s, { type: 'EndTurn' })
    s = applyCommand(s, { type: 'ConfirmReaction', fire: true })
    expect(s.player.growth).toBe(g0)
  })
})

describe('防御札の伏せ: 被攻撃前に印字コストを払ってブロック', () => {
  it('防御を伏せ、攻撃5の前に発動してブロック5で無傷', () => {
    let s = anyOn(withHand(freshCombat('set-confirm', 'enemy_probe', 25), ['green_guard']))
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_guard' })
    const hp0 = s.player.hp
    s = withIntent(s, attackIntent(5))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.phase).toBe('awaiting-reaction')
    expect(s.pendingWindow?.stage).toBe('pre')
    s = applyCommand(s, { type: 'ConfirmReaction', fire: true })
    expect(s.player.hp).toBe(hp0)
  })
  it('温存すれば札は残り、回収 (1E) で元の定義のまま手札に戻る', () => {
    let s = anyOn(withHand(freshCombat('set-confirm', 'enemy_probe', 26), ['green_guard']))
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_guard' })
    s = withIntent(s, attackIntent(5))
    s = applyCommand(s, { type: 'EndTurn' })
    s = applyCommand(s, { type: 'ConfirmReaction', fire: false })
    expect(s.player.setCards).toHaveLength(1)
    s = applyCommand(s, { type: 'RetrieveSetCard', cardUid: 't0_green_guard' })
    expect(s.player.hand.some((c) => c.uid === 't0_green_guard' && c.def.type === 'physical')).toBe(true)
  })
})

describe('専用リアクションは従来どおり', () => {
  it('茨の返しは伏せ時に印字1E・発動は無料', () => {
    let s = anyOn(withHand(freshCombat('set-confirm', 'enemy_probe', 27), ['green_reaction_thorns']))
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_thorns' })
    expect(s.player.energy).toBe(2)
    s = { ...s, player: { ...s.player, energy: 0 } } // 残り0Eでも撃てる
    s = withIntent(s, attackIntent(5))
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.phase).toBe('awaiting-reaction')
    const hp0 = s.enemies[0].hp
    s = applyCommand(s, { type: 'ConfirmReaction', fire: true })
    expect(hp0 - s.enemies[0].hp).toBe(10)
  })
})

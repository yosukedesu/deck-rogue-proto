// 青の拡張 (2026-08-25 +9枚 = 38枚) のテスト。
// 確定済みルール表「霊気獲得の誘発」「氷壁の換金」「魔力盗み」「置物の呪文プレイ誘発」を固定する。
import { describe, expect, it } from 'vitest'
import { applyCommand } from './state.ts'
import { attackIntent, defendIntent, freshCombat, withHand, withIntent } from './test-helpers.ts'

describe('霊気獲得の誘発 (静電の帳)', () => {
  it('霊気を得るたび敵全体に1ダメージ', () => {
    let s = withHand(freshCombat('set-confirm', 'enc_probe_pair', 42, 'starter_blue'), [
      'blue_perm_static',
      'blue_frost_veil',
    ])
    s = { ...s, player: { ...s.player, energy: 9 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_blue_perm_static' })
    s = applyCommand(s, { type: 'SetCard', cardUid: 't1_blue_frost_veil' })
    const hp0 = s.enemies[0].hp
    const hp1 = s.enemies[1].hp
    // 敵0の攻撃 → 霜の帳が発動 (氷壁8+霊気1) → 静電の帳が全体1ダメ
    let s2 = {
      ...s,
      enemies: s.enemies.map((e, i) =>
        i === 0
          ? { ...e, intent: { kind: 'attack' as const, shownMin: 5, shownMax: 5, actual: 5 } }
          : { ...e, intent: { kind: 'defend' as const, shownMin: 3, shownMax: 3, actual: 3 } },
      ),
    }
    s2 = applyCommand(s2, { type: 'EndTurn' })
    expect(s2.phase).toBe('awaiting-reaction')
    s2 = applyCommand(s2, { type: 'ConfirmReaction', fire: true })
    expect(s2.player.aether).toBe(1)
    expect(s2.enemies[0].hp).toBe(hp0 - 1)
    expect(s2.enemies[1].hp).toBe(hp1 - 1)
  })
})

describe('氷壁の換金 (氷の槍)', () => {
  it('現在の氷壁×1のダメージ。氷壁は消費しない', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), [
      'blue_ice_wall',
      'blue_ice_lance',
    ])
    s = { ...s, player: { ...s.player, energy: 9 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_blue_ice_wall' })
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_blue_ice_lance' })
    expect(s.enemies[0].hp).toBe(hpBefore - 10) // 氷壁10×1
    expect(s.player.iceBlock).toBe(10)
  })
})

describe('魔力盗み (打ち消しの換金)', () => {
  it('敵の行動を打ち消し、その実値ぶん氷壁を得る', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), [
      'blue_spell_steal',
    ])
    s = { ...s, player: { ...s.player, energy: 9 } }
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_blue_spell_steal' })
    s = withIntent(s, attackIntent(13))
    const hpBefore = s.player.hp
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.phase).toBe('awaiting-reaction')
    s = applyCommand(s, { type: 'ConfirmReaction', fire: true })
    expect(s.player.hp).toBe(hpBefore) // 打ち消しで被弾なし
    expect(s.player.iceBlock).toBe(13) // 実値13を氷壁に変換
    expect(s.player.aether).toBe(1)
  })
})

describe('霊気の奔流 (放出の第二の出口)', () => {
  it('霊気を全て消費し、×1枚ドロー', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), [
      'blue_aether_torrent',
    ])
    s = { ...s, player: { ...s.player, aether: 3 } }
    const handBefore = s.player.hand.length
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_blue_aether_torrent' })
    expect(s.player.aether).toBe(0)
    expect(s.player.hand.length).toBe(handBefore - 1 + 3)
  })
})

describe('置物の呪文プレイ誘発 (霧の分身)', () => {
  it('呪文をプレイするたび氷壁+1 (物理では誘発しない)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), [
      'blue_perm_mist_double',
      'blue_ponder',
      'green_strike',
    ])
    s = { ...s, player: { ...s.player, energy: 9 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_blue_perm_mist_double' })
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_blue_ponder' })
    expect(s.player.iceBlock).toBe(1)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't2_green_strike' })
    expect(s.player.iceBlock).toBe(1) // 物理では増えない
  })
})

describe('大漩渦 (ストームの全体化)', () => {
  it('敵全体に詠唱数×4ダメージ', () => {
    let s = withHand(freshCombat('set-confirm', 'enc_probe_pair', 42, 'starter_blue'), [
      'blue_ponder',
      'blue_ponder',
      'blue_maelstrom',
    ])
    s = { ...s, player: { ...s.player, energy: 9 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_blue_ponder' })
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_blue_ponder' })
    const hp0 = s.enemies[0].hp
    const hp1 = s.enemies[1].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't2_blue_maelstrom' })
    expect(s.enemies[0].hp).toBe(hp0 - 8) // 詠唱2×4
    expect(s.enemies[1].hp).toBe(hp1 - 8)
  })
})

describe('心眼 (敵防御窓のドロー)', () => {
  it('敵が防御した後に2ドロー+霊気1', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), [
      'blue_reaction_mind_eye',
    ])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_blue_reaction_mind_eye' })
    s = withIntent(s, defendIntent(8))
    const handBefore = s.player.hand.length
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.phase).toBe('awaiting-reaction')
    s = applyCommand(s, { type: 'ConfirmReaction', fire: true })
    expect(s.player.aether).toBe(1)
    // 手札は敵ターン終了後に全捨てされるため、イベントで確認する
    expect(s.eventLog.some((e) => e.type === 'CardsDrawn' && e.count === 2)).toBe(true)
    void handBefore
  })
})

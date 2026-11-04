// 青の拡張 (2026-08-25 +9枚 = 38枚) のテスト。
// 確定済みルール表「霊気獲得の誘発」「氷壁の換金」「魔力盗み」「置物の呪文プレイ誘発」を固定する。
import { describe, expect, it } from 'vitest'
import { applyCommand } from './state.ts'
import { attackIntent, defendIntent, freshCombat, setAndArm, withHand, withIntent } from './test-helpers.ts'

describe('霊気獲得の誘発 (静電の帳)', () => {
  it('霊気を得るたび敵全体に1ダメージ', () => {
    let s = withHand(freshCombat('set-confirm', 'enc_probe_pair', 42, 'starter_blue'), [
      'blue_perm_static',
      'blue_frost_veil',
    ])
    s = { ...s, player: { ...s.player, energy: 9 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_blue_perm_static' })
    s = setAndArm(s, 't1_blue_frost_veil') // 罠モデル: 伏せたターンは鳴らない
    const hp0 = s.enemies[0].hp
    const hp1 = s.enemies[1].hp
    // 敵0の攻撃 → 霜の帳が発動 (氷壁6+霊気1。2026-11-04 氷壁 約0.8倍) → 静電の帳が全体1ダメ
    let s2: typeof s = {
      ...s,
      enemies: s.enemies.map((e, i) =>
        i === 0
          ? { ...e, intent: { kind: 'attack' as const, actual: 20 } } // 2026-09-25: 霜の帳は完全に凌ぐと霊気+2＝凌げない攻撃で測る
          : { ...e, intent: { kind: 'defend' as const, actual: 3 } },
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
  it('現在の氷壁×1のダメージ。氷壁は消費しない (氷壁は 2026-11-04 ユーザー裁定の約0.8倍で 15→12)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), [
      'blue_ice_wall',
      'blue_ice_lance',
    ])
    s = { ...s, player: { ...s.player, energy: 9 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_blue_ice_wall' })
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_blue_ice_lance' })
    expect(s.enemies[0].hp).toBe(hpBefore - 12) // 氷壁12×1 (2026-08-30 引き上げの15を 2026-11-04 に約0.8倍)
    expect(s.player.iceBlock).toBe(12)
  })
})

describe('魔力盗み (打ち消しの換金)', () => {
  it('敵の行動を打ち消し、その実値ぶん氷壁を得る (得た氷壁は次の自ターンの始めに半分。2026-11-04 ユーザー裁定)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), [
      'blue_spell_steal',
    ])
    s = { ...s, player: { ...s.player, energy: 9 } }
    s = setAndArm(s, 't0_blue_spell_steal')
    s = withIntent(s, attackIntent(13))
    const hpBefore = s.player.hp
    const log0 = s.eventLog.length
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.phase).toBe('awaiting-reaction')
    s = applyCommand(s, { type: 'ConfirmReaction', fire: true })
    expect(s.player.hp).toBe(hpBefore) // 打ち消しで被弾なし
    const fresh = s.eventLog.slice(log0)
    expect(fresh.some((e) => e.type === 'IceBlockGained' && e.amount === 13)).toBe(true) // 実値13を氷壁に変換
    expect(s.phase).toBe('player-turn')
    expect(fresh.filter((e) => e.type === 'IceBlockDecayed')).toEqual([{ type: 'IceBlockDecayed', from: 13, to: 6 }])
    expect(s.player.iceBlock).toBe(6) // 次の自ターンの始めに半分 (切り捨て)
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
    expect(s.player.iceBlock).toBe(3) // 分身1 (据え置き) + 思案の氷壁2 (2026-08-27 の3を 2026-11-04 に約0.8倍)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't2_green_strike' })
    expect(s.player.iceBlock).toBe(3) // 物理では増えない
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

describe('心眼 (敵防御窓の換金)', () => {
  it('敵が防御した後に氷壁3+霊気3 (敵フェーズのドローは全捨てされるため無価値だった。氷壁は 2026-11-04 ユーザー裁定の約0.8倍で 4→3、次の自ターンの始めに半分)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_blue'), [
      'blue_reaction_mind_eye',
    ])
    s = setAndArm(s, 't0_blue_reaction_mind_eye')
    s = withIntent(s, defendIntent(8))
    const handBefore = s.player.hand.length
    const log0 = s.eventLog.length
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.phase).toBe('awaiting-reaction')
    s = applyCommand(s, { type: 'ConfirmReaction', fire: true })
    // 2026-08-26: 旧実装は 2ドロー+霊気1 だったが、手札は敵ターン終了後に全捨てされるため
    // 敵フェーズに引いた札は一度もプレイできない = ドローの価値が0だった。
    // 戦闘をまたいで持ち越す資源 (氷壁・霊気) へ置き換えた。
    expect(s.player.aether).toBe(3)
    const fresh = s.eventLog.slice(log0)
    expect(fresh.some((e) => e.type === 'IceBlockGained' && e.amount === 3)).toBe(true)
    expect(s.phase).toBe('player-turn')
    expect(s.player.iceBlock).toBe(1) // 氷壁3 を次の自ターンの始めに半分 (切り捨て)
    void handBefore
  })
})

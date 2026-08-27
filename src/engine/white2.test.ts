// 白の拡張 (2026-08-25 +19枚) のテスト。
// 確定済みルール表「召喚」「置物登場の誘発」「威圧の換金」「隊列の盾」を固定する。
import { describe, expect, it } from 'vitest'
import { applyCommand } from './state.ts'
import { attackIntent, freshCombat, withHand, withIntent } from './test-helpers.ts'

describe('召喚 (トークン再現)', () => {
  it('一斉召集: 従者の少年トークンを2体場に出し、集結の弾になる', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_white'), [
      'white_muster',
      'white_rally',
    ])
    s = { ...s, player: { ...s.player, energy: 9 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_white_muster' })
    expect(s.player.permanents).toHaveLength(2)
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_white_rally' })
    expect(s.enemies[0].hp).toBe(hpBefore - 8) // 置物2×4
  })

  it('召喚された従者は毎ターン開始時に自動攻撃する (本体と同じ挙動)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_white'), [
      'white_muster',
    ])
    s = { ...s, player: { ...s.player, energy: 9 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_white_muster' })
    s = withIntent(s, attackIntent(3))
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.enemies[0].hp).toBe(hpBefore - 4) // 従者2体×2ダメ
  })
})

describe('置物登場の誘発 (白の接着剤)', () => {
  it('軍楽隊: 置物が場に出るたび1ドロー (自身の登場にも誘発)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_white'), [
      'white_perm_band',
      'white_perm_squire',
    ])
    s = { ...s, player: { ...s.player, energy: 9 } }
    const handBefore = s.player.hand.length
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_white_perm_band' })
    // 軍楽隊自身の登場で1ドロー (手札: -軍楽隊+1ドロー = handBefore)
    expect(s.player.hand.length).toBe(handBefore)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_white_perm_squire' })
    // 従者の登場でさらに1ドロー
    expect(s.player.hand.length).toBe(handBefore - 1 + 1)
  })

  it('白銀の軍旗 + 一斉召集: トークン2体の登場で敵全体に2ダメ×2', () => {
    let s = withHand(freshCombat('set-confirm', 'enc_probe_pair', 42, 'starter_white'), [
      'white_perm_banner',
      'white_muster',
    ])
    s = { ...s, player: { ...s.player, energy: 9 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_white_perm_banner' })
    const hp0 = s.enemies[0].hp
    const hp1 = s.enemies[1].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_white_muster' })
    expect(s.enemies[0].hp).toBe(hp0 - 4) // 2ダメ×2回 (2026-08-26 軍旗 1→2)
    expect(s.enemies[1].hp).toBe(hp1 - 4)
  })
})

describe('威圧の換金 (断罪の槌)', () => {
  it('威圧で下げた強化×3の追加ダメージ。強化0以上なら追加なし', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_white'), [
      'white_menace',
      'white_verdict_hammer',
      'white_verdict_hammer',
    ])
    s = { ...s, player: { ...s.player, energy: 9 } }
    const hpBefore = s.enemies[0].hp
    // 威圧なし: 8のみ
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_white_verdict_hammer' })
    expect(s.enemies[0].hp).toBe(hpBefore - 8)
    // 威圧2 → 8 + 2×3
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_white_menace' })
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't2_white_verdict_hammer' })
    expect(s.enemies[0].hp).toBe(hpBefore - 8 - 8 - 6)
  })
})

describe('隊列の盾', () => {
  it('置物の数×2ブロック', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_white'), [
      'white_perm_squire',
      'white_perm_shieldmaiden',
      'white_rank_shield',
    ])
    s = { ...s, player: { ...s.player, energy: 9 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_white_perm_squire' })
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_white_perm_shieldmaiden' })
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't2_white_rank_shield' })
    expect(s.player.block).toBe(4) // 置物2×2
  })
})

describe('回復軸の接着剤 (聖なる鐘)', () => {
  it('実回復のたびブロック3 (満タンでは誘発しない)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_white'), [
      'white_perm_bell',
      'white_heal',
      'white_heal',
    ])
    s = { ...s, player: { ...s.player, energy: 9 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_white_perm_bell' })
    // 満タン: 回復0 → 鐘は鳴らない
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_white_heal' })
    expect(s.player.block).toBe(0)
    s = { ...s, player: { ...s.player, hp: 50 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't2_white_heal' })
    expect(s.player.hp).toBe(55)
    expect(s.player.block).toBe(3)
  })
})

describe('白の新リアクション', () => {
  it('聖罰の障壁: 敵の強化に合わせて威圧2+ブロック8', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_white'), [
      'white_reaction_holy_wall',
    ])
    s = { ...s, player: { ...s.player, energy: 9 } }
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_white_reaction_holy_wall' })
    s = withIntent(s, { kind: 'buff', shownMin: 3, shownMax: 3, actual: 3 })
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.phase).toBe('awaiting-reaction')
    s = applyCommand(s, { type: 'ConfirmReaction', fire: true })
    expect(s.enemies[0].strength).toBe(3 - 2)
    // ブロック8は敵フェーズ内で有効 (次の自ターン開始でリセットされるためイベントで確認)
    expect(
      s.eventLog.some((e) => e.type === 'BlockGained' && e.target === 'player' && e.amount === 8),
    ).toBe(true)
  })

  it('光盾の詠唱: 呪文プレイで起爆しブロック9', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_white'), [
      'white_reaction_chant',
      'white_heal',
    ])
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_white_reaction_chant' })
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_white_heal' })
    expect(s.player.block).toBe(9) // 2026-08-27 7→9
    expect(s.player.setCards).toHaveLength(0)
  })
})

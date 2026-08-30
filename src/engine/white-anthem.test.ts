// 白の再設計 (2026-08-31): アンセム (blessRetainers) と回復の換金 (onHealed網)。
import { describe, expect, it } from 'vitest'
import { getCardDef } from './content.ts'
import { applyCommand, createInitialState } from './state.ts'
import { attackIntent, freshCombat, withHand, withIntent } from './test-helpers.ts'
import type { GameState } from './types.ts'

const withEnergy = (s: GameState, energy: number): GameState => ({
  ...s,
  player: { ...s.player, energy },
})

describe('アンセム (blessRetainers)', () => {
  it('白銀の号令があると従者の量つき効果が+1される (少年2ダメ→3ダメ)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_white'), [
      'white_perm_squire',
      'white_perm_warcry',
    ])
    s = withEnergy(s, 9)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_white_perm_squire' })
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_white_perm_warcry' })
    s = withIntent(s, attackIntent(0))
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'EndTurn' }) // 次ターン開始: 少年の自動攻撃
    expect(hpBefore - s.enemies[0].hp).toBe(2 + 1)
  })

  it('アンセムは重ね掛けできる (号令+頌歌=+3) が、従者でない置物には乗らない', () => {
    let s = freshCombat('set-confirm', 'enemy_brute', 42, 'starter_white')
    s = {
      ...s,
      player: {
        ...s.player,
        permanents: [
          { uid: 'p1', def: getCardDef('white_perm_squire') }, // 従者: 毎T2ダメ
          { uid: 'p2', def: getCardDef('white_perm_warcry') }, // +1
          { uid: 'p3', def: getCardDef('white_perm_anthem') }, // +2
          { uid: 'p4', def: getCardDef('blue_frost_armor') }, // 従者でない置物: 毎T氷壁4
        ],
      },
    }
    s = withIntent(s, attackIntent(0))
    const hpBefore = s.enemies[0].hp
    const iceBefore = s.player.iceBlock
    s = applyCommand(s, { type: 'EndTurn' })
    expect(hpBefore - s.enemies[0].hp).toBe(2 + 3) // 少年2+アンセム3
    expect(s.player.iceBlock - iceBefore).toBe(4) // 霜の鎧はretainerでないので素の4
  })
})

describe('回復の換金 (onHealed網)', () => {
  it('光の聖杯: 回復するたびブロック2 (実回復>0の時だけ)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_white'), [
      'white_perm_chalice',
      'white_heal',
    ])
    s = withEnergy(s, 9)
    s = { ...s, player: { ...s.player, hp: s.player.maxHp - 3 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_white_perm_chalice' })
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_white_heal' })
    expect(s.player.block).toBe(2)
    // 満タンでの回復は実回復0 = 誘発しない
    s = withHand(s, ['white_heal'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_white_heal' })
    expect(s.player.block).toBe(2)
  })

  it('ひなたのパッシブ: 毎T回復1が回復時誘発 (ブロック1) の鼓動になる', () => {
    let s = applyCommand(createInitialState(42, 'set-confirm'), {
      type: 'StartCombat',
      seed: 42,
      enemyId: 'enemy_brute',
      deckId: 'starter_white',
      leaderId: 'leader_white',
    })
    s = { ...s, player: { ...s.player, hp: s.player.maxHp - 10 } }
    s = withIntent(s, attackIntent(0))
    s = applyCommand(s, { type: 'EndTurn' })
    // ターン開始: パッシブ回復1 → onHealed → パッシブのブロック1
    expect(s.player.block).toBeGreaterThanOrEqual(1)
  })
})

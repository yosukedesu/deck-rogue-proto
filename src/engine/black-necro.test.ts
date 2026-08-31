// 黒の墓地アーキ再設計 (2026-08-31 A+B段階導入)。
// A: 亡骸効果 (onSelfExhausted) = プレイ以外の経路で消滅した時に発火 → ミルが即座に価値を返す。
// B: 亡骸プレイ (necroCost) = 消滅置き場から一度だけプレイ。ゲームから完全に取り除かれる。
import { describe, expect, it } from 'vitest'
import { getCardDef } from './content.ts'
import { applyCommand } from './state.ts'
import { freshCombat, withHand } from './test-helpers.ts'
import type { GameState } from './types.ts'

const withEnergy = (s: GameState, energy: number): GameState => ({
  ...s,
  player: { ...s.player, energy },
})

describe('A: 亡骸効果 (onSelfExhausted)', () => {
  it('ミルされた爆ぜる骸は敵全体に3ダメージ = ミルが即座に価値を返す', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_black'), [
      'black_mill', // 忘却の霧: ミル4+ブロック5
    ])
    // 山札の先頭に爆ぜる骸を仕込む (ミルの的を固定)
    s = {
      ...s,
      player: {
        ...s.player,
        drawPile: [
          { uid: 'necro1', def: getCardDef('black_bursting_corpse') },
          ...s.player.drawPile,
        ],
      },
    }
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_black_mill' })
    // 爆ぜる骸がミルされ、亡骸: 全体3ダメが発火している (2026-09-01 亡骸の面配布で
    // 同時にミルされた他の札の亡骸も乗りうるため下限で判定)
    expect(hpBefore - s.enemies[0].hp).toBeGreaterThanOrEqual(3)
    expect(s.eventLog.some((e) => e.type === 'NecroFired')).toBe(true)
  })

  it('消滅コストで支払われた札の亡骸も発火する', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_black'), [
      'black_offering', // 供物の火: 13ダメ+消滅コスト1
      'black_bursting_corpse',
    ])
    s = withEnergy(s, 9)
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, {
      type: 'PlayCard',
      cardUid: 't0_black_offering',
      exhaustUids: ['t1_black_bursting_corpse'],
    })
    expect(hpBefore - s.enemies[0].hp).toBe(13 + 3) // 供物の火13 + 亡骸3
  })

  it('プレイして消滅した場合は発火しない (onPlayが仕事を終えているため)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_black'), [
      'black_bursting_corpse',
    ])
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_black_bursting_corpse' })
    expect(hpBefore - s.enemies[0].hp).toBe(6) // onPlayの6のみ。亡骸3は乗らない
    expect(s.eventLog.some((e) => e.type === 'NecroFired')).toBe(false)
  })
})

describe('B: 亡骸プレイ (necroCost)', () => {
  it('消滅置き場から一度だけプレイでき、プレイ後はゲームから完全に取り除かれる', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_black'), [
      'black_rotten_claw', // 朽ちた爪: 1E・8ダメ・消滅・亡骸プレイ1E (2026-09-01 朽ちぬ牙を統合)
    ])
    s = withEnergy(s, 9)
    const hpBefore = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_black_rotten_claw' })
    expect(hpBefore - s.enemies[0].hp).toBe(8)
    expect(s.player.exhaustPile.some((c) => c.def.id === 'black_rotten_claw')).toBe(true)
    // 消滅置き場から亡骸プレイ
    const uid = s.player.exhaustPile.find((c) => c.def.id === 'black_rotten_claw')!.uid
    const energyBefore = s.player.energy
    const castsBefore = s.player.cardsPlayedThisTurn
    s = applyCommand(s, { type: 'PlayNecro', cardUid: uid })
    expect(hpBefore - s.enemies[0].hp).toBe(16)
    expect(s.player.energy).toBe(energyBefore - 1)
    expect(s.player.cardsPlayedThisTurn).toBe(castsBefore + 1) // 詠唱数に数える
    // ゲームから完全に消える = 消滅置き場にも居ない (刻の燃料が減る緊張)
    expect(s.player.exhaustPile.some((c) => c.def.id === 'black_rotten_claw')).toBe(false)
    // 二度目はプレイできない
    expect(() => applyCommand(s, { type: 'PlayNecro', cardUid: uid })).toThrow()
  })

  it('necroCost を持たない札は亡骸プレイできない', () => {
    let s = freshCombat('set-confirm', 'enemy_brute', 42, 'starter_black')
    s = {
      ...s,
      player: {
        ...s.player,
        exhaustPile: [{ uid: 'x1', def: getCardDef('black_grave_digger') }],
      },
    }
    expect(() => applyCommand(s, { type: 'PlayNecro', cardUid: 'x1' })).toThrow(/亡骸プレイを持たない/)
  })
})

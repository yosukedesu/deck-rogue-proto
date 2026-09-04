// 人間ラン#3 (2026-09-03 seed1657100441) の指摘への是正の機械固定
import { describe, expect, it } from 'vitest'
import { getCardDef, getEnemyDef } from './content.ts'
import { fuseCards } from './fusion.ts'
import { upgradeCard } from './upgrade.ts'

describe('選択式カードの鍛える: 両モードが上がる', () => {
  it('道行きの選択+: 野生は勢い+3→+5 (単位=勢いは+2。3ダメは小さなおまけなので据え置き)、育成は成長+2→+3', () => {
    const up = upgradeCard({ uid: 'x', def: getCardDef('green_mode_crossroads') })
    const modes = up.def.modes!
    const wild = modes[0].effects
    const grow = modes[1].effects
    expect(wild.find((e) => e.effect === 'addMomentum')?.amount).toBe(5)
    expect(wild.find((e) => e.effect === 'dealDamage')?.amount).toBe(3)
    expect(grow.find((e) => e.effect === 'addGrowth')?.amount).toBe(3)
  })
})

describe('工房: 保持は伝播する', () => {
  it('大樹の怒り (保持) × 打撃 → 結果も保持', () => {
    const def = fuseCards({ uid: 'a', def: getCardDef('green_finisher_wrath') }, { uid: 'b', def: getCardDef('green_strike') })
    expect(def.retain).toBe(true)
  })
})

describe('血族の司祭の弔いは+2', () => {
  it('mournStrength 2 (人間ラン#3「弔いもっと強くてもいい」)', () => {
    expect(getEnemyDef('enemy_kin_priest').mournStrength).toBe(2)
  })
})

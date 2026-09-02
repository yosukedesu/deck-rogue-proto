// 合成結果の効果順 (2026-09-02 Opusラン C: 蔦の楔×二連の蔦打ち で粉砕が末尾に付き機能しなかった)
import { describe, expect, it } from 'vitest'
import { getCardDef } from './content.ts'
import { fuseCards } from './fusion.ts'

describe('計算合成の効果順', () => {
  it('粉砕はダメージより前に並ぶ (1ヒット目がブロックに吸われない)', () => {
    const def = fuseCards(
      { uid: 'a', def: getCardDef('green_vine_wedge') },
      { uid: 'b', def: getCardDef('green_double_lash') },
    )
    const kinds = def.effects.map((e) => e.effect)
    expect(kinds[0]).toBe('shatterBlock')
    expect(kinds.filter((k) => k === 'dealDamage').length).toBeGreaterThanOrEqual(2)
    expect(kinds.indexOf('shatterBlock')).toBeLessThan(kinds.indexOf('dealDamage'))
  })
})

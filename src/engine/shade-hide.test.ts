// 用心深い影「隠れる」= 防御6〜9 + 筋力+2 (2026-09-03 ユーザー裁定 案A。Opus H/I 2本一致
// 「伏せると期待被ダメ13→6の押せるスイッチ+宣言ごとに向きが変わり学習できない」への処方 =
// 今守らせる代わりに次の斬撃が重くなる交換)。防御行動の alsoBuff は engine 初適用。
import { describe, expect, it } from 'vitest'
import { getEnemyDef } from './content.ts'
import { applyCommand } from './state.ts'
import { freshCombat, withHand } from './test-helpers.ts'

describe('用心深い影の隠れる (防御+筋力)', () => {
  it('データ: movesVsSet の hide は alsoBuff 2 を持つ (重み2:1は維持)', () => {
    const def = getEnemyDef('enemy_set_wary')
    const hide = def.movesVsSet?.find((m) => m.id === 'hide')
    expect(hide).toMatchObject({ kind: 'defend', alsoBuff: 2, weight: 2 })
  })
  it('engine: 防御行動の alsoBuff が解決後に筋力へ乗る (打ち消し可能な行動単位の強化)', () => {
    let s = freshCombat('set-confirm', 'enemy_set_wary', 3)
    const base = s.enemies[0].intent!
    s = {
      ...s,
      enemies: s.enemies.map((e, i) =>
        i === 0
          ? { ...e, intent: { ...base, kind: 'defend', actual: 7, shownMin: 6, shownMax: 9, alsoBuff: 2, conditionalOn: undefined, alt: undefined } }
          : e,
      ),
    }
    const str0 = s.enemies[0].strength
    s = withHand(s, [])
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.enemies[0].strength).toBe(str0 + 2)
    expect(s.eventLog.some((e) => e.type === 'StrengthGained' && e.amount === 2)).toBe(true)
    expect(s.eventLog.some((e) => e.type === 'BlockGained' && e.target === 'enemy' && e.amount === 7)).toBe(true)
  })
})

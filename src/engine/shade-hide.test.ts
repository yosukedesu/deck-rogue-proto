// 防御行動の alsoBuff (2026-09-03 用心深い影「隠れる」= 防御6〜9 + 筋力+2 で engine 初適用)。
// 用心深い影の伏せ反応テーブル自体は 2026-09-13 罠モデルで撤去 (敵は伏せを見ない) したので、機構だけを固定する
import { describe, expect, it } from 'vitest'
import { applyCommand } from './state.ts'
import { freshCombat, withHand } from './test-helpers.ts'

describe('防御行動の alsoBuff (用心深い影の隠れる)', () => {
  it('engine: 防御行動の alsoBuff が解決後に筋力へ乗る (打ち消し可能な行動単位の強化)', () => {
    let s = freshCombat('set-confirm', 'enemy_set_wary', 3)
    const base = s.enemies[0].intent!
    s = {
      ...s,
      enemies: s.enemies.map((e, i) =>
        i === 0
          ? { ...e, intent: { ...base, kind: 'defend', actual: 7, alsoBuff: 2, conditionalOn: undefined, alt: undefined } }
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

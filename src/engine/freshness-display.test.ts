// 表示層と engine が同じ実値を読むことの固定。
// 旧「見切り無視の述語 (setReactionIgnoresFreshness)」は 2026-09-13 罠モデルで撤去 (敵は伏せを見ない)
import { describe, expect, it } from 'vitest'
import { enemyTraitTags } from './traits.ts'
import { allEnemies as enemyDefs } from './content.ts'
import { freshCombat } from './test-helpers.ts'

describe('残機の予告HPは親のHP倍率を継承した実値 (2026-09-03 Opusラン K)', () => {
  it('親のmaxHpが定義の2倍なら、予告の再起動HPも2倍', () => {
    const def = enemyDefs.find((d) => d.splitInto?.count === 1)!
    expect(def).toBeDefined()
    let s = freshCombat('set-confirm', def.id, 1)
    s = { ...s, enemies: s.enemies.map((e, i) => (i === 0 ? { ...e, maxHp: def.maxHp * 2, hp: def.maxHp * 2 } : e)) }
    const tag = enemyTraitTags(s, 0).find((t) => t.startsWith('残機'))!
    const child = enemyDefs.find((d) => d.id === def.splitInto!.enemyId)!
    expect(tag).toContain(`HP${child.maxHp * 2}`)
  })
})

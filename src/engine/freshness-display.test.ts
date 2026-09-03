// 見切り無視の述語 (2026-09-03): effectiveIntent と表示層が同じ判定を読む
import { describe, expect, it } from 'vitest'
import { allEnemies } from './content.ts'
import { effectiveIntent, setReactionIgnoresFreshness } from './effects.ts'
import { enemyTraitTags } from './traits.ts'
import { allEnemies as enemyDefs } from './content.ts'
import { freshCombat } from './test-helpers.ts'
import type { GameState } from './types.ts'

const withIntent = (s: GameState, patch: Record<string, unknown>): GameState => ({
  ...s,
  enemies: s.enemies.map((e, i) => (i === 0 && e.intent ? { ...e, intent: { ...e.intent, ...patch } } : e)),
})

describe('setReactionIgnoresFreshness', () => {
  it('条件付き意図が無ければ false', () => {
    const s = freshCombat('set-confirm', 'enemy_probe', 1)
    expect(setReactionIgnoresFreshness(withIntent(s, { conditionalOn: undefined, alt: undefined }), 0)).toBe(false)
  })
  it('setAlt.ignoreFreshness / 破壊分岐 / EnemyDef.vsSetIgnoreFreshness のいずれかで true', () => {
    const s = freshCombat('set-confirm', 'enemy_probe', 1)
    const base = s.enemies[0].intent!
    const gamble = withIntent(s, { conditionalOn: 'set', alt: { ...base, kind: 'attack', ignoreFreshness: undefined } })
    expect(setReactionIgnoresFreshness(gamble, 0)).toBe(false)
    const punish = withIntent(s, { conditionalOn: 'set', alt: { ...base, kind: 'attack', ignoreFreshness: true } })
    expect(setReactionIgnoresFreshness(punish, 0)).toBe(true)
    const destroy = withIntent(s, { conditionalOn: 'set', alt: { ...base, kind: 'destroy-set' } })
    expect(setReactionIgnoresFreshness(destroy, 0)).toBe(true)
    const defLevel = allEnemies.find((d) => d.vsSetIgnoreFreshness === true)
    expect(defLevel).toBeDefined()
    const byDef = withIntent(
      { ...s, enemies: s.enemies.map((e, i) => (i === 0 ? { ...e, enemyId: defLevel!.id } : e)) },
      { conditionalOn: 'set', alt: { ...base, kind: 'attack' } },
    )
    expect(setReactionIgnoresFreshness(byDef, 0)).toBe(true)
  })
  it('見切られた伏せ札 (setFresh=false) は賭け型には効かず、罰型には効く = effectiveIntent と一致', () => {
    const s0 = freshCombat('set-confirm', 'enemy_probe', 1)
    const base = s0.enemies[0].intent!
    const stale = { ...s0, player: { ...s0.player, setCards: [{ ...s0.player.hand[0], setFresh: false }] } } as GameState
    const gamble = withIntent(stale, { conditionalOn: 'set', alt: { ...base, kind: 'attack', shownMin: 99, shownMax: 99 } })
    expect(effectiveIntent(gamble, 0)!.shownMin).toBe(base.shownMin)
    const punish = withIntent(stale, { conditionalOn: 'set', alt: { ...base, kind: 'attack', shownMin: 99, shownMax: 99, ignoreFreshness: true } })
    expect(effectiveIntent(punish, 0)!.shownMin).toBe(99)
  })
})

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

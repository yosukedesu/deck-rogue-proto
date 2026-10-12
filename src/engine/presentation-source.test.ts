import { describe, expect, it } from 'vitest'
import { getCardDef } from './content.ts'
import { annotateDamagePresentation, fireNecroEffects, resolveEffectTargeted, resolveOnPlayEffects, resolveReactionEffects, runPermanentTriggers } from './effects.ts'
import { applyCommand } from './state.ts'
import { freshCombat } from './test-helpers.ts'
import type { CardInstance, DeclarativeEffect, GameEvent, GameState } from './types.ts'

const c = (id: string): CardInstance => ({ uid: `test_${id}`, def: getCardDef(id) })
const made = (id: string, effects: readonly DeclarativeEffect[]): CardInstance => ({ uid: `test_${id}`, def: { ...getCardDef('green_strike'), id, name: id, type: 'spell', effects } })
function state(): GameState {
  const s = freshCombat('set-auto', 'enemy_probe')
  return { ...s, player: { ...s.player, hp: 30, maxHp: 80, energy: 20, hand: [], drawPile: [], discardPile: [], exhaustPile: [], setCards: [], permanents: [] }, enemies: s.enemies.map(e => ({ ...e, hp: 200, maxHp: 200, block: 0, intent: { kind: 'rest', actual: 0 }, intentMoveId: undefined })) }
}
const hit = (source: 'player' | 'enemy' = 'player'): GameEvent => ({ type: 'DamageDealt', source, amount: 8, hpLoss: 5, blocked: 3, enemyIndex: 0 })
const damage = (s: GameState, from = 0) => s.eventLog.slice(from).filter((e): e is Extract<GameEvent, { type: 'DamageDealt' }> => e.type === 'DamageDealt' && e.source === 'player')

describe('display-only damage provenance', () => {
  it('labels only the new player-damage suffix, retaining old payloads and enemy events', () => {
    const s = { ...state(), eventLog: [hit(), hit('enemy'), hit()] }
    const owner = { kind: 'card', definitionId: 'green_strike', uid: 'owner' } as const
    const next = annotateDamagePresentation(s, 1, owner)
    expect(next.eventLog[0]).toBe(s.eventLog[0]); expect(next.eventLog[1]).toBe(s.eventLog[1])
    expect(next.eventLog[2]).toEqual({ ...s.eventLog[2], presentationSource: owner })
    expect(s.eventLog[2]).toEqual(hit()); expect(next.rng).toBe(s.rng); expect(next.player).toBe(s.player); expect(next.enemies).toBe(s.enemies)
  })
  it('never overwrites an inner owner and returns the same state if no annotation is needed', () => {
    const s = annotateDamagePresentation({ ...state(), eventLog: [hit()] }, 0, { kind: 'necro', definitionId: 'black_strike' })
    expect(annotateDamagePresentation(s, 0, { kind: 'card', definitionId: 'blue_strike' })).toBe(s)
    expect(annotateDamagePresentation(s, 0)).toBe(s)
  })
  it('assigns the real direct card instance', () => {
    const s = state(), card = c('green_strike')
    const next = resolveOnPlayEffects(s, card, 0)
    expect(damage(next, s.eventLog.length)[0].presentationSource).toEqual({ kind: 'card', definitionId: card.def.id, uid: card.uid })
  })
  it('separates necromancy from a resumed outer card', () => {
    const s0 = state(), s = { ...s0, player: { ...s0.player, drawPile: [c('black_strike')] } }
    const next = resolveOnPlayEffects(s, c('black_grave_bolt'), 0)
    expect(damage(next, s.eventLog.length).map(e => e.presentationSource?.kind + ':' + e.presentationSource?.definitionId)).toEqual(['necro:black_strike', 'card:black_grave_bolt'])
  })
  it('metadata identifies necro even when the old UID belongs to its outer permanent', () => {
    const s = { ...state(), resolvingPermanentUid: 'legacy_outer' }
    const next = fireNecroEffects(s, [c('black_strike')], 0), d = damage(next, s.eventLog.length)[0]
    expect(d.sourceUid).toBe('legacy_outer'); expect(d.presentationSource).toEqual({ kind: 'necro', definitionId: 'black_strike', uid: 'test_black_strike' })
  })
  it('preserves a nested on-healed permanent owner inside a drain', () => {
    const s0 = state(), p = made('healed_child', [{ trigger: 'onHealed', effect: 'dealDamage', amount: 2 }])
    const s = { ...s0, player: { ...s0.player, permanents: [p] } }, next = resolveOnPlayEffects(s, c('black_drain'), 0)
    expect(damage(next, s.eventLog.length).map(e => e.presentationSource?.definitionId)).toEqual(['black_drain', 'healed_child'])
  })
  it('annotates the direct blaze-trigger bypass with its permanent source', () => {
    const s0 = state(), p = made('blaze_child', [{ trigger: 'onTurnStart', effect: 'dealDamage', amount: 2, condition: { blaze: true } }])
    const s = { ...s0, player: { ...s0.player, permanents: [p] } }, outer = made('ignite', [{ trigger: 'onPlay', effect: 'applyBurn', amount: 8 }])
    const next = resolveOnPlayEffects(s, outer, 0)
    expect(damage(next, s.eventLog.length)[0].presentationSource).toEqual({ kind: 'permanent', definitionId: p.def.id, uid: p.uid })
  })
  it('keeps target/HP/guard values on fully blocked and zero attacks', () => {
    const original = state(), s = { ...original, enemies: original.enemies.map(e => ({ ...e, block: 100 })) }
    const next = resolveOnPlayEffects(s, c('green_strike'), 0), d = damage(next, s.eventLog.length)[0]
    expect(d.hpLoss).toBe(0); expect(d.blocked).toBe(d.amount); expect(d.presentationSource?.definitionId).toBe('green_strike')
    const zero = made('zero', [{ trigger: 'onPlay', effect: 'dealDamage', amount: 0 }])
    expect(damage(resolveOnPlayEffects(s, zero, 0), s.eventLog.length)[0]).toMatchObject({ amount: 0, hpLoss: 0, presentationSource: { definitionId: 'zero' } })
  })
  it('stores a damaging deferred reaction owner through JSON roundtrip and phase completion', () => {
    const s0 = state(), s = { ...s0, player: { ...s0.player, block: 100 }, enemies: s0.enemies.map(e => ({ ...e, intent: { kind: 'attack' as const, actual: 5 } })) }
    const card = made('deferred', [{ trigger: 'onAttacked', effect: 'dealDamage', amount: 7, condition: { perfectBlockThisPhase: true } }])
    const queued = resolveReactionEffects(s, card, 0)
    expect(damage(queued, s.eventLog.length)).toHaveLength(0)
    expect(queued.pendingPhaseEffects?.[0].presentationSource).toEqual({ kind: 'reaction', definitionId: card.def.id, uid: card.uid })
    const next = applyCommand(JSON.parse(JSON.stringify(queued)), { type: 'EndTurn' })
    expect(damage(next, queued.eventLog.length)[0].presentationSource?.definitionId).toBe('deferred')
  })
  it('does not invent a deferred hit when perfect block fails', () => {
    const s0 = state(), s = { ...s0, enemies: s0.enemies.map(e => ({ ...e, intent: { kind: 'attack' as const, actual: 5 } })) }
    const card = made('deferred', [{ trigger: 'onAttacked', effect: 'dealDamage', amount: 7, condition: { perfectBlockThisPhase: true } }])
    const queued = resolveReactionEffects(s, card, 0), next = applyCommand(queued, { type: 'EndTurn' })
    expect(damage(next, queued.eventLog.length)).toHaveLength(0)
  })
  it('retains unknown provenance for a legacy deferred entry', () => {
    const s0 = state(), s: GameState = { ...s0, pendingPhaseEffects: [{ effect: { trigger: 'onAttacked', effect: 'dealDamage', amount: 7 }, enemyIndex: 0 }], player: { ...s0.player, block: 100 }, enemies: s0.enemies.map(e => ({ ...e, intent: { kind: 'attack', actual: 5 } })) }
    const next = applyCommand(s, { type: 'EndTurn' })
    expect(damage(next, s.eventLog.length)[0].presentationSource).toBeUndefined()
  })
  it('does not guess a source for a deliberately unowned helper call', () => {
    const s = state(), next = resolveEffectTargeted(s, { trigger: 'onPlay', effect: 'dealDamage', amount: 4 }, 0)
    expect(damage(next, s.eventLog.length)[0].presentationSource).toBeUndefined()
  })
  it('keeps a permanent source stable across random doll target selection', () => {
    const s0 = state(), doll = c('white_perm_squire'), s = { ...s0, player: { ...s0.player, permanents: [doll] } }
    const next = runPermanentTriggers(s, 'onTurnStart', 0)
    expect(damage(next, s.eventLog.length)[0].presentationSource).toEqual({ kind: 'permanent', definitionId: doll.def.id, uid: doll.uid })
  })
})

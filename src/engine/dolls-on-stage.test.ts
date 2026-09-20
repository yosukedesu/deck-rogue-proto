// 人形の盤面表示 (2026-09-19 ユーザー「人形は戦場の盤面にも表示するようにしたい」): 演出が「誰の誘発か」を読めるよう、
// 置物の誘発で出た DamageDealt / BlockGained / HpHealed に sourceUid (置物の uid) を載せ、壊れた置物の出来事に uid を載せる。
// ルールは何も読まない (表示層の出どころだけ)。ゴールデンは出来事の型の列だけを見るので不変。
import { describe, expect, it } from 'vitest'
import { getCardDef } from './content.ts'
import { applyCommand } from './state.ts'
import { attackIntent, freshCombat, withHand, withIntent } from './test-helpers.ts'
import type { GameEvent, GameState } from './types.ts'

const withPermanents = (s: GameState, ids: readonly string[]): GameState => ({
  ...s,
  player: { ...s.player, permanents: ids.map((id, i) => ({ uid: 'p' + i, def: getCardDef(id) })) },
})
const after = (s: GameState, from: number): readonly GameEvent[] => s.eventLog.slice(from)

describe('人形の盤面表示: 出来事の出どころ', () => {
  it('人形のターン開始の攻撃・ブロック、攻撃ごとの回復は sourceUid にその人形の uid を持つ', () => {
    let s = withPermanents(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_white'), [
      'white_perm_squire', // p0: 毎T 2ダメ
      'white_perm_shieldmaiden', // p1: 毎T ブロック2
      'white_perm_choir', // p2: 攻撃ごと 回復2 (毎T回復の人形は作らない = 回し封じ 2026-09-20)
    ])
    s = { ...s, player: { ...s.player, hp: s.player.maxHp - 5 } }
    s = withIntent(s, attackIntent(0))
    const from = s.eventLog.length
    s = applyCommand(s, { type: 'EndTurn' })
    const log = after(s, from)
    const dmg = log.filter((e): e is Extract<GameEvent, { type: 'DamageDealt' }> => e.type === 'DamageDealt' && e.source === 'player')
    expect(dmg.map((e) => e.sourceUid)).toEqual(['p0'])
    const blk = log.filter((e): e is Extract<GameEvent, { type: 'BlockGained' }> => e.type === 'BlockGained' && e.target === 'player')
    expect(blk.map((e) => e.sourceUid)).toEqual(['p1'])
    expect(log.filter((e) => e.type === 'HpHealed')).toHaveLength(0) // ターン開始では回復しない
    // 解決が終われば旗は降りている (次のカードのプレイに漏れない)
    expect(s.resolvingPermanentUid).toBeUndefined()
    // 攻撃をプレイすると癒しの人形が回復し、その HpHealed が人形の uid を持つ
    s = withHand(s, ['white_strike'])
    const from2 = s.eventLog.length
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_white_strike', targetIndex: 0 })
    const heal = after(s, from2).filter((e): e is Extract<GameEvent, { type: 'HpHealed' }> => e.type === 'HpHealed')
    expect(heal.map((e) => e.sourceUid)).toEqual(['p2'])
  })

  it('カードのプレイで与えたダメージには sourceUid が無い', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_white'), ['white_strike'])
    const from = s.eventLog.length
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_white_strike', targetIndex: 0 })
    const dmg = after(s, from).filter((e): e is Extract<GameEvent, { type: 'DamageDealt' }> => e.type === 'DamageDealt')
    expect(dmg.length).toBe(1)
    expect(dmg[0].sourceUid).toBeUndefined()
  })

  it('攻撃ごとに動く人形 (犬) の誘発も、親のプレイの中で sourceUid を持つ', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_brute', 42, 'starter_white'), ['white_strike'])
    s = withPermanents(s, ['white_perm_hound']) // 攻撃ごと 2ダメ
    const from = s.eventLog.length
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_white_strike', targetIndex: 0 })
    const dmg = after(s, from).filter((e): e is Extract<GameEvent, { type: 'DamageDealt' }> => e.type === 'DamageDealt')
    expect(dmg.map((e) => e.sourceUid)).toEqual([undefined, 'p0'])
  })

  it('敵の従者狩り (destroy-token) の TokenDestroyed は壊れた置物の uid を持つ', () => {
    let s = withPermanents(freshCombat('set-confirm', 'enemy_set_breaker', 42, 'starter_white'), ['white_perm_squire'])
    s = withIntent(s, { kind: 'destroy-token', actual: 0 })
    const from = s.eventLog.length
    s = applyCommand(s, { type: 'EndTurn' })
    const gone = after(s, from).filter((e): e is Extract<GameEvent, { type: 'TokenDestroyed' }> => e.type === 'TokenDestroyed')
    expect(gone.map((e) => e.uid)).toEqual(['p0'])
    expect(s.player.permanents.length).toBe(0)
  })
})

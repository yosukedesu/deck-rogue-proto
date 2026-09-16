// 意図の rider (「+がらくた2(山札へ)」) は死に札の上限で畳む (2026-09-16 人間#12)。
// 歩哨戦で上限4に達した後も6回「+がらくた2」を予告し続けた表示の嘘への是正。表示と付与の実処理が同じ式 (cardStatusRoom) を読む。
import { describe, expect, it } from 'vitest'
import { JUNK_DEF, WOUND_DEF } from './content.ts'
import { cardStatusRoom, displayedInflict, JUNK_CAP, WOUND_CAP, SCALD_CAP } from './effects.ts'
import { applyCommand } from './state.ts'
import { intentText } from '../ui/log.ts'
import { attackIntent, freshCombat, withHand, withIntent } from './test-helpers.ts'
import type { GameState } from './types.ts'

function withJunkInDraw(s: GameState, n: number): GameState {
  const junk = Array.from({ length: n }, (_, i) => ({ uid: `junk${i}`, def: JUNK_DEF }))
  return { ...s, player: { ...s.player, drawPile: [...s.player.drawPile, ...junk] } }
}

describe('死に札の上限と意図の rider の表示', () => {
  it('がらくたは手札+山札+捨て札の枚数で残りを数え、上限に達したら rider を出さない', () => {
    let s = freshCombat('set-confirm', 'enemy_elite_sentry', 11, 'starter')
    expect(cardStatusRoom(s, 'junk')).toBe(JUNK_CAP)
    s = withJunkInDraw(s, JUNK_CAP - 1)
    const inflict = { status: 'junk' as const, amount: 2 }
    // 残り1枚: 「+がらくた1」に畳む
    expect(displayedInflict(s, inflict)).toEqual({ status: 'junk', amount: 1 })
    expect(intentText({ kind: 'attack', actual: 5, inflict }, undefined, s)).toContain('＋がらくた1(山札へ)')
    s = withJunkInDraw(s, 1)
    expect(cardStatusRoom(s, 'junk')).toBe(0)
    expect(displayedInflict(s, inflict)).toBeUndefined()
    expect(intentText({ kind: 'attack', actual: 5, inflict }, undefined, s)).not.toContain('がらくた')
    // 状態を渡さない場所 (ログ行) は宣言どおり
    expect(intentText({ kind: 'attack', actual: 5, inflict })).toContain('＋がらくた2(山札へ)')
  })

  it('表示と実処理が同じ式: 残り1枚の時に攻撃+がらくた2 を受けると1枚しか増えない', () => {
    let s = freshCombat('set-confirm', 'enemy_elite_sentry', 11, 'starter')
    s = withHand(s, [])
    s = withJunkInDraw(s, JUNK_CAP - 1)
    s = withIntent(s, { ...attackIntent(3), inflict: { status: 'junk', amount: 2 } })
    s = applyCommand(s, { type: 'EndTurn' })
    const junk = [...s.player.hand, ...s.player.drawPile, ...s.player.discardPile].filter((c) => c.def.id === JUNK_DEF.id)
    expect(junk).toHaveLength(JUNK_CAP)
    const ev = s.eventLog.find((e) => e.type === 'StatusInflicted' && e.status === 'junk')
    expect(ev && ev.type === 'StatusInflicted' ? ev.amount : -1).toBe(1)
  })

  it('負傷は全ゾーン (消滅置き場・伏せ場も) を数える。火傷は累計カウンタ。上限の無い状態異常は畳まない', () => {
    let s = freshCombat('set-confirm', 'enemy_probe', 11, 'starter')
    const wounds = Array.from({ length: WOUND_CAP }, (_, i) => ({ uid: `w${i}`, def: WOUND_DEF }))
    s = { ...s, player: { ...s.player, exhaustPile: wounds.slice(0, 3), setCards: wounds.slice(3) } }
    expect(cardStatusRoom(s, 'wound')).toBe(0)
    expect(displayedInflict(s, { status: 'wound', amount: 1 })).toBeUndefined()
    s = { ...s, player: { ...s.player, scaldsThisCombat: SCALD_CAP - 1 } }
    expect(displayedInflict(s, { status: 'scald', amount: 2 })).toEqual({ status: 'scald', amount: 1 })
    expect(cardStatusRoom(s, 'weak')).toBeNull()
    expect(displayedInflict(s, { status: 'weak', amount: 2 })).toEqual({ status: 'weak', amount: 2 })
  })
})

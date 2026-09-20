// 回復の回し封じ (2026-09-20 ユーザー「人形でブロックを固めた勝確の盤面で、全回復まで回復カードを回せる」。card-power.md §73):
// 回復は「一度きり (消滅)」か「殴った分だけ (攻撃に付く・放出が敵を削る)」のどちらか。添え物の回復は灯に。毎ターン回復する置物は作らない。
import { describe, expect, it } from 'vitest'
import { allCards, getCardDef } from './content.ts'
import fusionsJson from '../data/fusions.json'
import { applyCommand } from './state.ts'
import { freshCombat, withHand } from './test-helpers.ts'
import type { CardDef, DeclarativeEffect, GameState } from './types.ts'

const effectsOf = (c: CardDef): readonly DeclarativeEffect[] => [...(c.effects ?? []), ...(c.modes ?? []).flatMap((m) => m.effects)]
const DAMAGE = /^(dealDamage|drain|discharge)/ // 敵を削る効果 = 殴った分だけの回復
const heals = (c: CardDef) => effectsOf(c).filter((e) => e.effect === 'gainHp')
const hurts = (c: CardDef) => effectsOf(c).some((e) => DAMAGE.test(e.effect))
// HP半分以下でしか鳴らない回復は自分で止まる (灯りの庭) ので回しにならない
const selfLimited = (e: DeclarativeEffect) => e.condition?.hpAtOrBelowRatio !== undefined

describe('回復の回し封じ (白)', () => {
  const white = allCards.filter((c) => c.id.startsWith('white_'))

  it('回復を持ちダメージを与えない白の呪文・物理・反応は消滅を持つ (一度きり)', () => {
    const bad = white.filter(
      (c) => c.type !== 'permanent' && heals(c).some((e) => !selfLimited(e)) && !hurts(c) && c.exhaust !== true,
    )
    expect(bad.map((c) => `${c.id} ${c.name}`)).toEqual([])
  })

  it('毎ターン開始に回復する置物は全色で作らない (札を使わない回し)', () => {
    const bad = allCards.filter((c) => c.type === 'permanent' && effectsOf(c).some((e) => e.trigger === 'onTurnStart' && e.effect === 'gainHp'))
    expect(bad.map((c) => `${c.id} ${c.name}`)).toEqual([])
  })

  it('レシピ産の白の札も同じ規則 (灯すか守るか = 消滅)', () => {
    const bad = (fusionsJson as readonly { result: CardDef }[]).map((r) => r.result).filter(
      (c) => c.color === 'white' && c.type !== 'permanent' && heals(c).some((e) => !selfLimited(e)) && !hurts(c) && c.exhaust !== true,
    )
    expect(bad.map((c) => `${c.id} ${c.name}`)).toEqual([])
  })

  it('3分割の形: 主役=消滅+灯 / 殴った分=据え置き / 添え物=灯', () => {
    const d = getCardDef
    expect(d('white_heal').exhaust).toBe(true)
    expect(d('white_heal').effects.map((e) => `${e.effect}${e.amount}`)).toEqual(['gainHp6', 'gainHp4', 'addLight1'])
    expect(d('white_hymn_wall').exhaust).toBe(true)
    expect(d('white_hymn_wall').effects.find((e) => e.effect === 'gainHp')?.amount).toBe(8)
    expect(d('white_blade_prayer').exhaust).toBeUndefined() // 灯りの刃 = 6ダメ+回復 (殴った分だけ)
    expect(d('white_praise_chorus').exhaust).toBeUndefined() // 灯の輪 = 放出が敵を削る
    expect(d('white_perm_choir').effects[0]!.trigger).toBe('onAttackPlayed') // 癒しの人形 = 攻撃ごと回復2
    expect(d('white_perm_monk').effects[0]!.trigger).toBe('onPermanentEntered') // 手当ての人形 = 登場ごと回復1
    for (const id of ['white_shield_prayer', 'white_maiden_prayer', 'white_catalyst_echo', 'white_reaction_bright_wall', 'white_oath_shield']) {
      expect(heals(d(id)), id).toEqual([])
      expect(effectsOf(d(id)).some((e) => e.effect === 'addLight'), id).toBe(true)
    }
    // 灯りの庭 (HP半分以下でしか鳴らない=消滅なし) は 2026-09-20 夜に撤去。火種の祈り (0E 回復2+火種) は消滅
    expect(d('white_prayer_verse').exhaust).toBe(true)
  })

  it('癒しの光はプレイ後に消滅置き場へ行き、回復6 (+条件4) と灯+3 (回復2回+明示1) が入る', () => {
    let s: GameState = withHand(freshCombat('set-confirm', 'enemy_probe', 42, 'starter_white'), ['white_heal'])
    s = { ...s, player: { ...s.player, hp: 30, energy: 3 } }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_white_heal' })
    expect(s.player.hp).toBe(40) // 30 ≤ 半分 → 6+4
    expect(s.player.light).toBe(3) // 回復の解決ごと+1 (2回) + 明示の灯+1
    expect(s.player.exhaustPile.map((c) => c.def.id)).toEqual(['white_heal'])
    expect(s.player.discardPile.map((c) => c.def.id)).toEqual([])
  })

  it('癒しの人形は攻撃をプレイするたび回復2 (ターン開始では回復しない)。点灯で出た瞬間にも1回', () => {
    let s: GameState = withHand(freshCombat('set-confirm', 'enemy_probe', 42, 'starter_white'), ['white_perm_choir', 'white_strike'])
    s = { ...s, player: { ...s.player, hp: 30, energy: 9, energyMax: 9, light: 2 } } // 癒しの人形は 1E・灯2 (2026-09-20 灯と人形の結び)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_white_perm_choir' })
    expect(s.player.hp).toBe(32) // 点灯の定義: 出た瞬間に1回動く
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't1_white_strike', targetIndex: 0 })
    expect(s.player.hp).toBe(34)
    const hpBefore = s.player.hp
    s = applyCommand(s, { type: 'EndTurn' })
    // 敵フェーズ〜次の自ターン開始で人形は回復しない (被弾ぶんだけ減る)
    expect(s.player.hp).toBeLessThanOrEqual(hpBefore)
  })
})

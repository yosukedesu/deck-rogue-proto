// 緑の勢いの網 (2026-09-04 ユーザー方針「勢いは軽く積める代わりに成長より弱い。倍加と還元の札で完成」)。
// 放出に勢い加算は乗らない (成長放出と同じ裁定)・全体放出は一括解決・勢い→成長の還元・勢い倍率・勢い条件の0E。
import { describe, expect, it } from 'vitest'
import { allEncounters } from './content.ts'
import { effectiveCost } from './effects.ts'
import { applyCommand } from './state.ts'
import { freshCombat, withHand } from './test-helpers.ts'

const withMomentum = (s: ReturnType<typeof freshCombat>, momentum: number, growth = 0) => ({
  ...s,
  player: { ...s.player, momentum, growth },
})

describe('勢いの網 (緑)', () => {
  it('角の一突き: 8貫通(勢いが乗る)＋勢い×2の放出 (放出に勢い加算は乗らない)・勢いは0になる', () => {
    let s = withMomentum(withHand(freshCombat('set-confirm', 'enemy_probe', 42), ['green_horn_thrust']), 4)
    const hp0 = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_horn_thrust', targetIndex: 0 })
    // 8+4 (通常ヒット) + 4×2 (放出。勢いは先に0) = 20
    expect(hp0 - s.enemies[0].hp).toBe(20)
    expect(s.player.momentum).toBe(0)
    expect(s.eventLog.some((e) => e.type === 'MomentumDischarged' && e.spent === 4)).toBe(true)
  })

  it('本体のヒットで倒した (放出が空振り) なら勢いは消費しない (2026-09-04 Opusラン P のオーバーキル蒸発の是正)', () => {
    let s = withMomentum(withHand(freshCombat('set-confirm', 'enemy_probe', 42), ['green_horn_thrust']), 14)
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, hp: 12 })) }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_horn_thrust', targetIndex: 0 })
    expect(s.enemies[0].hp).toBeLessThanOrEqual(0)
    expect(s.player.momentum).toBe(14)
    expect(s.eventLog.some((e) => e.type === 'MomentumDischarged')).toBe(false)
  })

  it('勢い0で角の一突きを撃っても放出は起きない (8貫通だけ)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 42), ['green_horn_thrust'])
    const hp0 = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_horn_thrust', targetIndex: 0 })
    expect(hp0 - s.enemies[0].hp).toBe(8)
    expect(s.eventLog.some((e) => e.type === 'MomentumDischarged')).toBe(false)
  })

  it('嵐の角 (全体): 生存全体に 4+勢い、さらに勢い×1 を一括で放出 (2体目が0ダメにならない)', () => {
    const enc = allEncounters.find((e) => e.members.length === 2)
    if (!enc) throw new Error('2体編成が無い')
    let s = withMomentum(withHand(freshCombat('set-confirm', enc.id, 42), ['green_horn_storm']), 3)
    const hp = s.enemies.map((e) => e.hp)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_horn_storm' })
    for (let i = 0; i < 2; i++) {
      // 4+3 (通常) + 3×1 (放出) = 10。装甲・とげ等の無い編成を前提にする
      expect(hp[i] - s.enemies[i].hp, `敵${i}`).toBe(10)
    }
    expect(s.player.momentum).toBe(0)
  })

  it('根付く勢い: 6ダメ(勢いが乗る)のあと勢いを全て失い、半分(切り上げ)が成長になる', () => {
    let s = withMomentum(withHand(freshCombat('set-confirm', 'enemy_probe', 42), ['green_rooting_rush']), 5)
    const hp0 = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_rooting_rush', targetIndex: 0 })
    expect(hp0 - s.enemies[0].hp).toBe(11)
    expect(s.player.momentum).toBe(0)
    expect(s.player.growth).toBe(3)
  })

  it('猛進の角: 勢いが×2で乗る (14 + 勢い4×2 = 22)', () => {
    let s = withMomentum(withHand(freshCombat('set-confirm', 'enemy_probe', 42), ['green_charging_horn']), 4)
    const hp0 = s.enemies[0].hp
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_charging_horn', targetIndex: 0 })
    expect(hp0 - s.enemies[0].hp).toBe(22)
    expect(s.player.momentum).toBe(4) // 消費しない
  })

  it('追い風: 勢い5以上なら実コスト0、4以下なら1', () => {
    const s = withHand(freshCombat('set-confirm', 'enemy_probe', 42), ['green_tailwind'])
    expect(effectiveCost(withMomentum(s, 4), s.player.hand[0])).toBe(1)
    expect(effectiveCost(withMomentum(s, 5), s.player.hand[0])).toBe(0)
  })

  it('疾風の号砲: 勢い+1してから2倍 (最低2が立つ)。消滅', () => {
    let s = withMomentum(withHand(freshCombat('set-confirm', 'enemy_probe', 42), ['green_gale_horn']), 3)
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_gale_horn' })
    expect(s.player.momentum).toBe(8)
    expect(s.player.exhaustPile.some((c) => c.def.id === 'green_gale_horn')).toBe(true)
  })

  it('連なる角 (裁定B 2026-09-04): 勢い×1を3回に分けて放つ = 装甲25の下でも勢い15が3発全部通る (単発なら25で頭打ち)', () => {
    let s = withMomentum(withHand(freshCombat('set-confirm', 'enemy_brute', 42), ['green_horn_volley']), 15)
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, armor: 25, hp: 200, maxHp: 200 })) }
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_horn_volley', targetIndex: 0 })
    expect(200 - s.enemies[0].hp).toBe(45)
    expect(s.player.momentum).toBe(0)
    // 比較: 角の一突きの放出 (勢い15×2=30) は装甲25で頭打ち
    let t = withMomentum(withHand(freshCombat('set-confirm', 'enemy_brute', 42), ['green_horn_thrust']), 15)
    t = { ...t, enemies: t.enemies.map((e) => ({ ...e, armor: 25, hp: 200, maxHp: 200 })) }
    t = applyCommand(t, { type: 'PlayCard', cardUid: 't0_green_horn_thrust', targetIndex: 0 })
    expect(200 - t.enemies[0].hp).toBe(23 + 25) // 8+15=23 (装甲内) + 放出30→25
  })

  it('疾駆: 0E・勢い+3・消滅しない (0E規約: 手札もエナジーも増やさない)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 42), ['green_sprint'])
    s = applyCommand(s, { type: 'PlayCard', cardUid: 't0_green_sprint' })
    expect(s.player.momentum).toBe(3)
    expect(s.player.discardPile.some((c) => c.def.id === 'green_sprint')).toBe(true)
  })
})

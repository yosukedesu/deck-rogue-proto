// 2026-09-02 夜のユーザー裁定6件 (Opusラン3本の答え合わせ) の機械固定。docs/playtest-2026-09-02-opus-runs.md §5
import { describe, expect, it } from 'vitest'
import { allCards, getCardDef, getEnemyDef, getRelicDef } from './content.ts'
import { worstIncomingFrom } from './summary.ts'
import { applyCommand } from './state.ts'
import { attackIntent, freshCombat, withHand, withIntent } from './test-helpers.ts'
import { canUpgradeInHand } from './upgrade.ts'
import type { GameState } from './types.ts'

const play = (s: GameState, uid: string, extra: Record<string, unknown> = {}) =>
  applyCommand(s, { type: 'PlayCard', cardUid: uid, ...extra } as never)

describe('保持 (retain): 4E以上の大型は全捨てで手札に残る', () => {
  it('巨獣の踏みつけは敵ターン終了後も手札にあり、打撃は捨て札へ行く', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 11), ['green_finisher_stomp', 'green_strike'])
    expect(getCardDef('green_finisher_stomp').retain).toBe(true)
    s = applyCommand(s, { type: 'EndTurn' })
    expect(s.player.hand.some((c) => c.def.id === 'green_finisher_stomp')).toBe(true)
    expect(s.player.discardPile.some((c) => c.def.id === 'green_strike')).toBe(true)
    for (const id of ['green_finisher_wrath', 'green_sig_stampede']) {
      expect(getCardDef(id).retain, id).toBe(true)
    }
  })
})

describe('鏡の敵 (mirrorHits) は伏せも手数に数える', () => {
  it('1枚プレイ+1枚伏せ = 2ヒット (伏せ+置物の抜け道を閉じる)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 12), ['green_strike', 'green_reaction_thorns'])
    s = withIntent(s, { ...attackIntent(3), mirrorHits: true })
    expect(worstIncomingFrom(s, 0)).toBe(3) // 0枚でも最低1ヒット
    s = play(s, 't0_green_strike')
    expect(worstIncomingFrom(s, 0)).toBe(3)
    s = applyCommand(s, { type: 'SetCard', cardUid: 't1_green_reaction_thorns' })
    expect(s.player.setsThisTurn).toBe(1)
    expect(worstIncomingFrom(s, 0)).toBe(6)
  })
})

describe('見切りの拡張: 同じ札の伏せ直しに敵は反応しない (伏せ税の処方)', () => {
  it('初回の伏せは setFresh、回収→伏せ直しは setFresh にならない', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 13), ['green_reaction_thorns'])
    s = { ...s, player: { ...s.player, energy: 5 } }
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_thorns' })
    expect(s.player.setCards[0].setFresh).toBe(true)
    s = applyCommand(s, { type: 'RetrieveSetCard', cardUid: 't0_green_reaction_thorns' })
    expect(s.player.hand[0].wasSet).toBe(true)
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_thorns' })
    expect(s.player.setCards[0].setFresh).toBe(false)
  })
})

describe('蜃気楼の面の作り直し: 伏せた瞬間からそのターンの実値が見える', () => {
  it('伏せる前は幅表示、伏せた後は shownMin=shownMax=actual', () => {
    // 2026-09-03: レリック自体は撤去 (作り直し後も確認ウィンドウを「はい」ボタンに退化させた)。機構 (revealOnSet) は残す
    expect(() => getRelicDef('relic_mirage_mask')).toThrow()
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 14), ['green_reaction_thorns'])
    s = { ...s, revealOnSet: true }
    s = withIntent(s, { ...attackIntent(7), shownMin: 5, shownMax: 9 })
    expect(s.enemies[0].intent?.shownMin).toBe(5)
    s = applyCommand(s, { type: 'SetCard', cardUid: 't0_green_reaction_thorns' })
    expect(s.enemies[0].intent?.shownMin).toBe(7)
    expect(s.enemies[0].intent?.shownMax).toBe(7)
  })
})

describe('研ぎ澄まし (upgradeInHand) の対象制限', () => {
  it('レアと工房産 (fused_/fusion_) は鍛えられない。打撃は鍛えられる', () => {
    const strike = { uid: 'a', def: getCardDef('green_strike') }
    const rare = { uid: 'b', def: getCardDef('green_sig_vine_dance') }
    const fused = { uid: 'c', def: { ...getCardDef('green_strike'), id: 'fused_x__y', name: '真・打撃' } }
    expect(canUpgradeInHand(strike)).toBe(true)
    expect(canUpgradeInHand(rare)).toBe(false)
    expect(canUpgradeInHand(fused)).toBe(false)
  })
})

describe('データ裁定 (撤去5・レア化・作り直し・締切)', () => {
  it('嵐の角笛・満開の刻・巨木の盾・逆襲の蔦・岩砕きの根は撤去済み。森の導きはレア', () => {
    for (const id of ['green_storm_horn', 'green_full_bloom', 'green_giant_bark', 'green_reaction_backlash', 'green_rock_root']) {
      expect(allCards.some((c) => c.id === id), id).toBe(false)
    }
    expect(getCardDef('green_forest_guidance').rarity).toBe('rare')
  })

  it('勢い先出し: 突進の助走・踏み荒らしは勢いが自分のダメージに乗る (怒涛の突き上げは2026-09-05 撤去)', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 15), ['green_trample_charge', 'green_sig_trample'])
    s = { ...s, player: { ...s.player, energy: 9 } }
    const h0 = s.enemies[0].hp
    s = play(s, 't0_green_trample_charge') // 勢い3 → (2+3)×2 = 10
    expect(h0 - s.enemies[0].hp).toBe(10)
    const h1 = s.enemies[0].hp
    s = play(s, 't1_green_sig_trample') // 勢い3+3=6 → 16+6 = 22 (貫通)
    expect(h1 - s.enemies[0].hp).toBe(22)
    const trample = getCardDef('green_sig_trample')
    expect(trample.effects[0].effect).toBe('addMomentum')
  })

  it('蔦の乱舞: ヒットごとに成長+1が先に入り、1枚の中で雪だるまになる', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 16), ['green_sig_vine_dance'])
    s = { ...s, player: { ...s.player, energy: 3 } }
    const h0 = s.enemies[0].hp
    s = play(s, 't0_green_sig_vine_dance')
    // 成長 1,2,3,4,5 が各ヒットに乗る: (2+1)+(2+2)+(2+3)+(2+4)+(2+5) = 25
    expect(h0 - s.enemies[0].hp).toBe(25)
    expect(s.player.growth).toBe(5)
  })

  it('野生の萌芽は0E消滅で成長+1と1ドロー、根喰らいの蔓は+8、汚泥の圧殺は育つ技 (+4/回)', () => {
    const sprout = getCardDef('green_wild_sprout')
    expect(sprout.exhaust).toBe(true)
    expect(sprout.effects.map((e) => e.effect)).toEqual(['addGrowth', 'drawCards'])
    expect(getCardDef('green_devour_vine').effects.find((e) => e.effect === 'dealDamage')?.amount).toBe(8)
    const smother = getEnemyDef('enemy_sludge_berserker').moves.find((m) => m.id === 'smother')
    expect(smother?.growPerUse).toBe(4)
  })
})

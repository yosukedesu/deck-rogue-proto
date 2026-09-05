// 緑の床パッケージ (2026-09-02 監査 docs/green-audit.md §5 + 人間ラン#2 の処方) の機械固定。
// 新効果 (成長しきい値・獲得誘発・伏せ枠+1・回収・サーチ・増殖・育つ札・手札で鍛える) の挙動と、
// 「基礎体力の床」(0〜1E率・キャントリップ・全体・1E置物) の退行防止、幕3のターン装甲出現保証。
import { describe, expect, it } from 'vitest'
import { allCards, allEnemies, getCardDef, getEnemyDef } from './content.ts'
import { ACT_MUST_APPEAR, generateMap, tierFor } from './map.ts'
import { createRng } from './rng.ts'
import { REWARD_EXCLUDED } from './run.ts'
import { applyCommand } from './state.ts'
import { freshCombat, withHand } from './test-helpers.ts'
import type { GameState } from './types.ts'

const idOf = (name: string): string => {
  const c = allCards.find((x) => x.name === name)
  if (!c) throw new Error(`カードが無い: ${name}`)
  return c.id
}
const hp = (s: GameState) => s.enemies[0].hp
const play = (s: GameState, uid: string, extra: Record<string, unknown> = {}) =>
  applyCommand(s, { type: 'PlayCard', cardUid: uid, ...extra } as never)
const withEnergy = (s: GameState, energy: number): GameState => ({ ...s, player: { ...s.player, energy } })

describe('成長しきい値 (condition.minGrowth)', () => {
  it('深緑の刻: 成長5未満は6ダメのみ、5以上なら2発目も解決する', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 1), [idOf('深緑の刻')])
    const h0 = hp(s)
    s = play(s, `t0_${idOf('深緑の刻')}`)
    expect(h0 - hp(s)).toBe(6)
    let t = withHand(freshCombat('set-confirm', 'enemy_probe', 1), [idOf('深緑の刻')])
    t = { ...t, player: { ...t.player, growth: 5 } }
    const h1 = hp(t)
    t = play(t, `t0_${idOf('深緑の刻')}`)
    expect(h1 - hp(t)).toBe((6 + 5) * 2)
  })
})

describe('獲得誘発 (onGrowthGained / onMomentumGained)', () => {
  it('棘葉の茂み: 成長を得る「たび」に1回 (量ではなく回数) 先頭の敵へダメージ。倍化でも誘発する', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 2), [idOf('棘葉の茂み'), idOf('年輪'), idOf('開花の儀')])
    s = withEnergy(s, 6)
    s = play(s, `t0_${idOf('棘葉の茂み')}`)
    const h0 = hp(s)
    const n0 = s.eventLog.filter((e) => e.type === 'DamageDealt').length
    s = play(s, `t1_${idOf('年輪')}`) // 成長+2 → 誘発1回: 2 + 成長2 = 4
    expect(h0 - hp(s)).toBe(4)
    expect(s.eventLog.filter((e) => e.type === 'DamageDealt').length - n0).toBe(1)
    const h1 = hp(s)
    s = play(s, `t2_${idOf('開花の儀')}`) // 成長2→4 (倍化も獲得) → 2 + 4 = 6
    expect(h1 - hp(s)).toBe(6)
  })

  it('風渡り: 勢いを得るたび1ドロー', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 3), [idOf('風渡り'), idOf('突進の助走')])
    s = play(s, `t0_${idOf('風渡り')}`)
    expect(s.player.hand).toHaveLength(1)
    s = play(s, `t1_${idOf('突進の助走')}`)
    expect(s.player.hand, '助走を撃って手札0のはずが、勢い+3の誘発で1枚引く').toHaveLength(1)
  })

  it('規約: 獲得誘発の置物は同じ資源を自分で増やさない (成長→成長・勢い→勢いの自己増殖ループ禁止)', () => {
    const offenders: string[] = []
    for (const c of allCards) {
      for (const e of [...c.effects, ...(c.modes ?? []).flatMap((m) => m.effects)]) {
        if (e.trigger === 'onGrowthGained' && ['addGrowth', 'doubleGrowth'].includes(e.effect)) offenders.push(c.id)
        if (e.trigger === 'onMomentumGained' && ['addMomentum', 'doubleMomentum'].includes(e.effect)) offenders.push(c.id)
      }
    }
    expect(offenders).toEqual([])
  })
})

describe('伏せ枠+1 (gainSetSlot)', () => {
  it('罠師の茂みを置くと同時に2枚伏せられる', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 4), [idOf('罠師の茂み'), 'green_reaction_thorns', 'green_reaction_thorns'])
    s = withEnergy(s, 6)
    expect(() => {
      let t = applyCommand(s, { type: 'SetCard', cardUid: 't1_green_reaction_thorns' })
      t = applyCommand(t, { type: 'SetCard', cardUid: 't2_green_reaction_thorns' })
      return t
    }, '基本の伏せ枠は1').toThrow()
    s = play(s, `t0_${idOf('罠師の茂み')}`)
    expect(s.player.setSlots).toBe(2)
    s = applyCommand(s, { type: 'SetCard', cardUid: 't1_green_reaction_thorns' })
    s = applyCommand(s, { type: 'SetCard', cardUid: 't2_green_reaction_thorns' })
    expect(s.player.setCards).toHaveLength(2)
  })
})

describe('カード操作 (回収・サーチ・増殖・育つ札・手札で鍛える)', () => {
  it('若返りの根: 捨て札から選んだ1枚が手札に戻る (deckUids)。捨て札が空なら選択なしで撃てる', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 5), [idOf('若返りの根')])
    const strike = { uid: 'd0', def: getCardDef('green_strike') }
    s = { ...s, player: { ...s.player, discardPile: [strike] } }
    expect(() => play(s, `t0_${idOf('若返りの根')}`)).toThrow(/deckUids/)
    const t = play(s, `t0_${idOf('若返りの根')}`, { deckUids: ['d0'] })
    expect(t.player.hand.map((c) => c.uid)).toContain('d0')
    expect(t.player.discardPile.some((c) => c.uid === 'd0')).toBe(false)
    expect(t.player.block).toBe(5)
    const empty = { ...s, player: { ...s.player, discardPile: [] } }
    expect(() => play(empty, `t0_${idOf('若返りの根')}`)).not.toThrow()
  })

  it('森の導き: 山札から選んだ1枚を手札へ。残りの山札の並びは崩れない', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 6), [idOf('森の導き')])
    const before = s.player.drawPile.map((c) => c.uid)
    expect(before.length).toBeGreaterThan(3)
    const pick = before[2]
    s = play(s, `t0_${idOf('森の導き')}`, { deckUids: [pick] })
    expect(s.player.hand.map((c) => c.uid)).toContain(pick)
    expect(s.player.drawPile.map((c) => c.uid)).toEqual(before.filter((u) => u !== pick))
    expect(s.player.growth).toBe(1)
  })

  it('増える蔦: プレイ後、捨て札に自身とコピー (この戦闘限りのトークン) が並ぶ', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 7), [idOf('増える蔦')])
    const h0 = hp(s)
    s = play(s, `t0_${idOf('増える蔦')}`)
    expect(h0 - hp(s)).toBe(4)
    const copies = s.player.discardPile.filter((c) => c.def.id === idOf('増える蔦'))
    expect(copies).toHaveLength(2)
    expect(copies.filter((c) => c.token === true)).toHaveLength(1)
  })

  it('育つ牙: プレイするたび与ダメ+4 がインスタンスに積まれ、次のプレイに注入される', () => {
    let s = withHand(freshCombat('set-confirm', 'enemy_probe', 8), [idOf('育つ牙')])
    const h0 = hp(s)
    s = play(s, `t0_${idOf('育つ牙')}`)
    expect(h0 - hp(s)).toBe(6)
    const grown = s.player.discardPile.find((c) => c.def.id === idOf('育つ牙'))
    expect(grown?.growBonus).toBe(4)
    s = { ...s, player: { ...s.player, hand: [grown!], discardPile: [], energy: 3 } }
    const h1 = hp(s)
    s = play(s, grown!.uid)
    expect(h1 - hp(s)).toBe(10)
    expect(s.player.discardPile.find((c) => c.def.id === idOf('育つ牙'))?.growBonus).toBe(8)
  })

  it('研ぎ澄まし: 手札の1枚をこの戦闘中鍛える (handUids)。鍛えられる札が無ければ省略できる', () => {
    const s = withHand(freshCombat('set-confirm', 'enemy_probe', 9), [idOf('研ぎ澄まし'), 'green_strike'])
    expect(() => play(s, `t0_${idOf('研ぎ澄まし')}`)).toThrow(/handUids/)
    const t = play(s, `t0_${idOf('研ぎ澄まし')}`, { handUids: ['t1_green_strike'] })
    const strike = t.player.hand.find((c) => c.uid === 't1_green_strike')
    expect(strike?.def.name).toBe('打撃+')
    expect(strike?.def.effects[0].amount).toBe(9)
    expect(t.player.block).toBe(5)
    const alone = withHand(freshCombat('set-confirm', 'enemy_probe', 9), [idOf('研ぎ澄まし')])
    expect(() => play(alone, `t0_${idOf('研ぎ澄まし')}`)).not.toThrow()
  })

  it('規約: 1枚の札は 引導/回収/サーチ のうち1種しか持たない (deckUids 欄を共有するため)', () => {
    const kinds = ['exhaustFromDeckChoose', 'retrieveFromDiscard', 'searchDeck']
    const offenders = allCards
      .filter((c) => new Set(c.effects.filter((e) => kinds.includes(e.effect)).map((e) => e.effect)).size > 1)
      .map((c) => c.id)
    expect(offenders).toEqual([])
  })
})

describe('基礎体力の床 (docs/green-audit.md §5-3。カラーパイは本家=最高レート、床は全色の権利)', () => {
  const effectsOf = (c: (typeof allCards)[number]) => [...c.effects, ...(c.modes ?? []).flatMap((m) => m.effects)]
  const isCantrip = (c: (typeof allCards)[number]) =>
    c.xCost !== true &&
    c.cost <= 1 &&
    c.type !== 'reaction' &&
    effectsOf(c).some((e) => e.trigger === 'onPlay' && (e.effect === 'drawCards' || e.effect === 'impulseDraw')) &&
    effectsOf(c).some((e) => ['dealDamage', 'gainBlock', 'gainIceBlock'].includes(e.effect))
  const isAoe = (c: (typeof allCards)[number]) =>
    c.type !== 'reaction' &&
    effectsOf(c).some(
      (e) =>
        e.target === 'all' &&
        e.trigger === 'onPlay' &&
        /^(dealDamage|applyBurn|dischargeGrowth|shatter)/.test(e.effect),
    )
  const pool = (color: string) => allCards.filter((c) => c.color === color && !c.id.endsWith('_token'))

  it('緑: 報酬対象の0〜1E率40%以上・キャントリップ2以上・全体6以上・1E置物3以上・0E3以上', () => {
    const eligible = pool('green').filter((c) => !REWARD_EXCLUDED.has(c.id))
    const cheap = eligible.filter((c) => c.xCost !== true && c.cost <= 1).length
    expect(cheap / eligible.length, `0〜1E率 ${cheap}/${eligible.length}`).toBeGreaterThanOrEqual(0.4)
    expect(eligible.filter(isCantrip).length).toBeGreaterThanOrEqual(2)
    expect(eligible.filter(isAoe).length).toBeGreaterThanOrEqual(6)
    expect(eligible.filter((c) => c.type === 'permanent' && c.cost === 1).length).toBeGreaterThanOrEqual(3)
    expect(eligible.filter((c) => c.cost === 0 && c.xCost !== true).length).toBeGreaterThanOrEqual(3)
  })

  it('他色: 現状の値を床として退行を防ぐ (キャントリップ/全体/0E。床の引き上げは各色の解凍時に)', () => {
    const floors: Record<string, [number, number, number]> = { blue: [3, 4, 4], red: [4, 13, 5], white: [0, 1, 0], black: [1, 6, 5] }
    for (const [color, [cantrip, aoe, zero]] of Object.entries(floors)) {
      const p = pool(color)
      expect(p.filter(isCantrip).length, `${color} キャントリップ`).toBeGreaterThanOrEqual(cantrip)
      expect(p.filter(isAoe).length, `${color} 全体`).toBeGreaterThanOrEqual(aoe)
      expect(p.filter((c) => c.cost === 0 && c.xCost !== true).length, `${color} 0E`).toBeGreaterThanOrEqual(zero)
    }
  })
})

describe('幕3の量の器 (人間ラン#2: ターン装甲が21戦で一度も出なかった実測への処方)', () => {
  const WEAK = [3, 2, 2]
  it('門番はターン装甲90を持つ (門番=幕3ボス候補。X多段の1ターン出力に天井。実効HP372÷90=最低5ターン)', () => {
    expect(getEnemyDef('enemy_warden').turnArmor).toBe(90)
  })

  it('幕3の全マップに汚泥の大暴れ (ターン装甲45) が通常戦闘ノードとして必ず1つはある', () => {
    expect(ACT_MUST_APPEAR[2]).toContain('enemy_sludge_berserker')
    for (let seed = 1; seed <= 60; seed++) {
      const [map] = generateMap(createRng(seed), 3)
      const found = map.some((row) => row.some((n) => n.type === 'battle' && n.encounterId === 'enemy_sludge_berserker'))
      expect(found, `seed${seed}`).toBe(true)
    }
  })

  it('本帯の通常戦闘は幕内で編成が重複しない (プールが戦闘数より大きい限り。人間ラン#2: 幕2が6戦で4種)', () => {
    for (let act = 1; act <= 3; act++) {
      for (let seed = 1; seed <= 40; seed++) {
        const [map] = generateMap(createRng(seed), act)
        const ids: string[] = []
        for (let r = WEAK[act - 1]; r < map.length; r++) {
          for (const n of map[r]) if (n.type === 'battle' && n.encounterId) ids.push(n.encounterId)
        }
        const poolSize = tierFor(act, WEAK[act - 1]).length
        if (ids.length > poolSize) continue
        const dup = ids.filter((id, i) => ids.indexOf(id) !== i)
        // 未使用優先でも、同族回避 (直前2行) と組むとプール末尾で数件の重複は残る。
        // 実測 (240マップ): 超過ぶんを除いた重複 0件=83%・最悪5件。旧実装 (直前2行のみ) は幕2が6戦で4種。
        // 上限を「プール超過ぶん+6」に固定 = 退行 (毎マップ数件の重複) だけを止める
        expect(dup.length, `act${act} seed${seed}: ${dup.join(',')}`).toBeLessThanOrEqual(Math.max(0, ids.length - poolSize) + 6)
      }
    }
  })
})

describe('幕3の量の装置の配布拡大 (2026-09-05 人間ラン#5: 合成後の幕3通常戦が2.67T。ユーザー裁定「ok」)', () => {
  it('幕3の通常ソロ3体 (雷球の巨頭・献身の彫師・罠壊し) がターン装甲30〜35を持つ', () => {
    expect(getEnemyDef('enemy_thunder_globe').turnArmor).toBe(35)
    const sculptor = allEnemies.find((e) => e.name === '献身の彫師')!
    const breaker = allEnemies.find((e) => e.name === '罠壊し')!
    expect(sculptor.turnArmor).toBe(30)
    expect(breaker.turnArmor).toBe(35)
  })
})

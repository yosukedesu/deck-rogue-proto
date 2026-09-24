// 出立の店 (ラン開始 2026-09-24。docs/departure-proposal-2026-09-24.md) の機械固定。
// ラン開始の所持金は 50+100G。坑口の行商の店に 札3 (C/U/R)・遺物3 (C/U/R)・サービス4 (荷の整理・研ぎ・薬草・道具箱) が並ぶ (2026-09-24 夜)。買っても買わなくてもよく、上限は無い (資金だけが制約)。
// 買わなかった品は行商が担いで降りて幕1の店に並ぶ (画面には予告しない)。前借り (ツケ＋代償) は同日夜に撤去＝全部お金で買う
import { describe, expect, it } from 'vitest'
import { allDepartures, getCardDef, getGearDef, getRelicDef } from './content.ts'
import {
  DEPARTURE_PURSE,
  applyRunCommand,
  createDebugCheckpointRun,
  createRun,
  defaultDepartureCommand,
  departureOfferAvailable,
  gearsOf,
  manaOf,
  openShop,
  replayStates,
} from './run.ts'
import type { RunState } from './run.ts'
import type { DepartureOffer } from './types.ts'

const tmpl = (id: string) => allDepartures.find((t) => t.id === id)!
const offer = (id: string, extra: Record<string, unknown> = {}): DepartureOffer => {
  const t = tmpl(id)
  return { id: t.id, kind: t.kind, name: t.name, text: t.text, choice: { ...t.choice, ...extra }, price: t.price, ...(t.shopPrice !== undefined ? { shopPrice: t.shopPrice } : {}) }
}
/** 地図から始めたランに、決めた品を並べた出立の店を持たせて坑口に立たせる (抽選に依らない挙動のテスト用。所持金は 150G) */
function atDeparture(offers: readonly DepartureOffer[], leaderId = 'leader_white'): RunState {
  const base = createRun(4242, 'set-confirm', leaderId, undefined, undefined, { departure: false })
  return { ...base, gold: 50 + DEPARTURE_PURSE, phase: 'departure', departure: { offers, bought: [], leftovers: [] } }
}
const FIVE = () => [
  offer('dep_unpack'),
  offer('dep_herb'),
  offer('dep_relic_common', { relicId: 'relic_growth_seed' }),
  offer('dep_toolbox', { gears: ['gear_powder', 'gear_spring'] }),
  offer('dep_card_rare', { addCardIds: ['white_grand_charge'] }),
]

describe('抽選 (createRun)', () => {
  it('ラン開始は出立の店から。札3・遺物3・サービス4 が台帳の順に並び、札と遺物は中身の名前。所持金は 50+100、同じシードなら同じ', () => {
    const a = createRun(5601, 'set-confirm', 'leader_white')
    const b = createRun(5601, 'set-confirm', 'leader_white')
    expect(a.phase).toBe('departure')
    expect(a.departure?.offers.map((o) => o.kind)).toEqual(['card', 'card', 'card', 'relic', 'relic', 'relic', 'service', 'service', 'service', 'service'])
    expect(a.departure?.offers.map((o) => o.id)).toEqual(allDepartures.map((t) => t.id))
    for (const o of a.departure!.offers) {
      if (o.kind === 'card') expect(o.name).toBe(getCardDef(o.choice.addCardIds![0]).name)
      if (o.kind === 'relic') expect(o.name).toBe(getRelicDef(o.choice.relicId!).name)
    }
    expect(a.departure?.bought).toEqual([])
    expect(a.departure?.leftovers).toEqual([])
    expect(a.gold).toBe(50 + DEPARTURE_PURSE)
    expect(a.deck.length).toBe(10)
    expect(JSON.stringify(a.departure)).toBe(JSON.stringify(b.departure))
  })

  it('台帳は札3・遺物3 (レア度1つずつ)・サービス4。中身は名指しで解決される (遺物はそのレア度・自分の色で候補列から、ギアは2個別々、札は自分の色のそのレア度)。全部に値段がある', () => {
    expect(allDepartures.filter((t) => t.kind === 'card').map((t) => t.cardPick?.rarity)).toEqual(['common', 'uncommon', 'rare'])
    expect(allDepartures.filter((t) => t.kind === 'relic').map((t) => t.relicRarity)).toEqual(['common', 'uncommon', 'rare'])
    expect(allDepartures.filter((t) => t.kind === 'service').map((t) => t.id)).toEqual(['dep_unpack', 'dep_whet', 'dep_herb', 'dep_toolbox'])
    for (const t of allDepartures) expect(t.price).toBeGreaterThan(0)
    // 除去・鍛えは普通の店にもあるので行商は担いで降りない
    for (const t of allDepartures) expect(t.shopPrice === undefined).toBe(t.id === 'dep_unpack' || t.id === 'dep_whet')
    let seenRelic = 0
    let seenGears = 0
    let seenCards = 0
    let seenRareRelic = 0
    for (let seed = 1; seed <= 40; seed++) {
      const run = createRun(seed, 'set-confirm', 'leader_white')
      for (const o of run.departure!.offers) {
        const t = tmpl(o.id)
        expect(o.price).toBe(t.price)
        if (t.relicRarity !== undefined) {
          expect(o.choice.relicId, `seed ${seed} ${o.id}`).toBeDefined()
          expect(getRelicDef(o.choice.relicId!).rarity).toBe(t.relicRarity)
          expect(run.relicQueue).toContain(o.choice.relicId)
          seenRelic++
          if (t.relicRarity === 'rare') seenRareRelic++
        }
        if ((t.gearCount ?? 0) > 0) {
          expect(o.choice.gears?.length).toBe(t.gearCount)
          expect(new Set(o.choice.gears).size).toBe(t.gearCount)
          for (const g of o.choice.gears ?? []) expect(getGearDef(g).retired).not.toBe(true)
          seenGears++
        }
        if (t.cardPick !== undefined) {
          expect(o.choice.addCardIds?.length).toBe(t.cardPick.count)
          for (const id of o.choice.addCardIds ?? []) {
            expect(getCardDef(id).rarity).toBe(t.cardPick.rarity)
            expect(getCardDef(id).color).toBe('white')
          }
          seenCards++
        }
      }
      const relics = run.departure!.offers.map((o) => o.choice.relicId).filter((x) => x !== undefined)
      expect(new Set(relics).size).toBe(relics.length)
      const cards = run.departure!.offers.flatMap((o) => o.choice.addCardIds ?? [])
      expect(new Set(cards).size).toBe(cards.length)
    }
    expect(seenRelic).toBeGreaterThan(0)
    expect(seenRareRelic).toBeGreaterThan(0) // レアの遺物 (150G) も出る
    expect(seenGears).toBeGreaterThan(0)
    expect(seenCards).toBeGreaterThan(0)
  })

  it('departure: false (テスト・旧来の起点) とチェックポイント開始には出立の店が無く、上乗せの所持金も無い', () => {
    const plain = createRun(1, 'set-confirm', 'leader_green', undefined, undefined, { departure: false })
    expect(plain.phase).toBe('map')
    expect(plain.gold).toBe(50)
    const cp = createDebugCheckpointRun(1, 'set-confirm', 'leader_green', { act: 2, deckId: 'deck_big_mana' })
    expect(cp.phase).toBe('map')
    expect(cp.departure).toBeUndefined()
  })
})

describe('坑口で買う (BuyDeparture) と店を出る (LeaveDeparture)', () => {
  it('サービス「荷の整理」を50Gで: 対象の札が消え、金が減り、店に留まる。出ると買わなかった品を行商が預かる', () => {
    const run = atDeparture(FIVE())
    const bought = applyRunCommand(run, { type: 'BuyDeparture', index: 0, cardIndex: 0 })
    expect(bought.phase).toBe('departure')
    expect(bought.gold).toBe(150 - 50)
    expect(bought.deck.length).toBe(run.deck.length - 1)
    expect(bought.departure?.bought).toEqual(['dep_unpack'])
    expect(() => applyRunCommand(bought, { type: 'BuyDeparture', index: 0, cardIndex: 0 })).toThrow() // 同じ品は二度買えない
    const left = applyRunCommand(bought, { type: 'LeaveDeparture' })
    expect(left.phase).toBe('map')
    expect(left.departure?.leftovers.map((o) => o.id)).toEqual(['dep_herb', 'dep_relic_common', 'dep_toolbox', 'dep_card_rare'])
  })

  it('何も買わずに出てもよい (150G のまま地図へ・荷の整理以外の4品を行商が預かる)', () => {
    const run = atDeparture(FIVE())
    const left = applyRunCommand(run, { type: 'LeaveDeparture' })
    expect(left.phase).toBe('map')
    expect(left.gold).toBe(150)
    expect(left.departure?.leftovers.length).toBe(4)
    expect(() => applyRunCommand(left, { type: 'LeaveDeparture' })).toThrow()
    expect(() => applyRunCommand(left, { type: 'BuyDeparture', index: 0 })).toThrow()
  })

  it('上限は無く資金だけが制約 (道具箱80＋薬草60＝140G)。金が足りなければ買えず、買えるかは departureOfferAvailable が読む', () => {
    const run = atDeparture(FIVE())
    const a = applyRunCommand(run, { type: 'BuyDeparture', index: 3 })
    expect(gearsOf(a).map((g) => g.gearId)).toEqual(['gear_powder', 'gear_spring'])
    expect(manaOf(a)).toBe(20)
    const b = applyRunCommand(a, { type: 'BuyDeparture', index: 1 })
    expect(b.maxHp).toBe(run.maxHp + 8)
    expect(b.gold).toBe(150 - 80 - 60)
    expect(departureOfferAvailable(b, b.departure!.offers[0])).toBe(false) // 残り10G: 荷の整理50G は買えない
    expect(() => applyRunCommand(b, { type: 'BuyDeparture', index: 0, cardIndex: 0 })).toThrow()
  })

  it('レアの札は100Gで (代償は無い)。アンコモンの遺物120G・レアの遺物150G', () => {
    const a = applyRunCommand(atDeparture(FIVE()), { type: 'BuyDeparture', index: 4 })
    expect(a.gold).toBe(50)
    expect(a.deck.some((c) => c.def.id === 'white_grand_charge')).toBe(true)
    expect(a.maxHp).toBe(atDeparture(FIVE()).maxHp)
    expect(a.deck.some((c) => c.def.id === 'status_brand')).toBe(false)
    const run = atDeparture([offer('dep_unpack'), offer('dep_herb'), offer('dep_relic_uncommon', { relicId: 'relic_shield_shard' }), offer('dep_relic_rare', { relicId: 'relic_growth_seed' }), offer('dep_toolbox', { gears: ['gear_powder', 'gear_spring'] })])
    const b = applyRunCommand(run, { type: 'BuyDeparture', index: 2 })
    expect(b.relics).toContain('relic_shield_shard')
    expect(b.gold).toBe(30)
    const c = applyRunCommand(run, { type: 'BuyDeparture', index: 3 })
    expect(c.relics).toContain('relic_growth_seed')
    expect(c.gold).toBe(0)
    expect(departureOfferAvailable(c, c.departure!.offers[0])).toBe(false)
  })

  it('ボットの既定: サービスを1つ買って (対象つき)、次の一手で店を出る', () => {
    const run = atDeparture([offer('dep_whet'), offer('dep_herb'), offer('dep_relic_common', { relicId: 'relic_growth_seed' }), offer('dep_toolbox', { gears: ['gear_powder', 'gear_spring'] }), offer('dep_card_rare', { addCardIds: ['white_grand_charge'] })])
    const c1 = defaultDepartureCommand(run)
    expect(c1).toMatchObject({ type: 'BuyDeparture', index: 0 })
    expect('cardIndex' in c1 && typeof c1.cardIndex === 'number').toBe(true)
    const a = applyRunCommand(run, c1)
    expect(a.phase).toBe('departure')
    const c2 = defaultDepartureCommand(a)
    expect(c2).toEqual({ type: 'LeaveDeparture' })
    expect(applyRunCommand(a, c2).phase).toBe('map')
  })
})

describe('行商の棚 (幕1の店に並ぶ。画面には予告しない)', () => {
  it('買わなかった品が幕1の店に定価で並び、買うと効果が入って棚から消える。幕2の店には並ばない', () => {
    const run = atDeparture(FIVE())
    const left = applyRunCommand(applyRunCommand(run, { type: 'BuyDeparture', index: 0, cardIndex: 0 }), { type: 'LeaveDeparture' })
    const shop = openShop(left)
    expect(shop.shop?.departures).toEqual([
      { id: 'dep_herb', price: 60 },
      { id: 'dep_relic_common', price: 100 },
      { id: 'dep_toolbox', price: 80 },
      { id: 'dep_card_rare', price: 100 },
    ])
    const bought = applyRunCommand(shop, { type: 'ShopBuyDeparture', index: 1 })
    expect(bought.phase).toBe('shop')
    expect(bought.gold).toBe(0)
    expect(bought.relics).toContain('relic_growth_seed')
    expect(bought.shop?.departures?.[1]?.sold).toBe(true)
    expect(bought.departure?.leftovers.map((o) => o.id)).toEqual(['dep_herb', 'dep_toolbox', 'dep_card_rare'])
    expect(() => applyRunCommand(bought, { type: 'ShopBuyDeparture', index: 1 })).toThrow()
    expect(() => applyRunCommand(bought, { type: 'ShopBuyDeparture', index: 0 })).toThrow() // 金が足りない
    const act2 = openShop({ ...left, act: 2 })
    expect(act2.shop?.departures).toBeUndefined()
  })

  it('除去・鍛えは幕1の店に並ばない (普通の店のサービスで買える)', () => {
    const left = applyRunCommand(atDeparture([offer('dep_unpack'), offer('dep_whet')]), { type: 'LeaveDeparture' })
    expect(left.departure?.leftovers).toEqual([])
    expect(openShop(left).shop?.departures).toBeUndefined()
  })
})

describe('リプレイ', () => {
  it('出立の店の買い物はジャーナルから再現できる (同じシード・同じ手)', () => {
    let run = createRun(77, 'set-confirm', 'leader_white')
    const commands = []
    while (run.phase === 'departure') {
      const cmd = defaultDepartureCommand(run)
      commands.push(cmd)
      run = applyRunCommand(run, cmd)
    }
    expect(commands.length).toBeGreaterThanOrEqual(1)
    const { states, error } = replayStates({ origin: { kind: 'run' as const, seed: 77, leaderId: 'leader_white' }, commands } as never)
    expect(error).toBeNull()
    const last = states[commands.length]
    expect(last.phase).toBe('map')
    expect(last.gold).toBe(run.gold)
    expect(JSON.stringify(last.departure)).toBe(JSON.stringify(run.departure))
  })
})

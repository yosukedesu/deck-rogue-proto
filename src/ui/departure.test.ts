// 出立の店 (ラン開始 2026-09-24。docs/departure-proposal-2026-09-24.md) の画面の文。
// Web (App.tsx) と CLI (sim/play.ts) が同じ純関数 (ui/log.ts) を読むので、ここで文と CLI の描画を固定する
import { describe, expect, it } from 'vitest'
import { allDepartures, departureMasterLine, getCardDef, getRelicDef } from '../engine/content.ts'
import { applyRunCommand, createRun, openShop } from '../engine/run.ts'
import type { RunCommand, RunState } from '../engine/run.ts'
import type { DepartureOffer } from '../engine/types.ts'
import {
  departureChoiceLine,
  departureCliLines,
  departureCostText,
  departureDetailText,
  departureGainText,
  departurePriceLabel,
  departureUnavailableReason,
  shopDepartureCliLines,
  type DepartureCliFormat,
} from './log.ts'
import { departureDetail, describeRunChoice } from './report.ts'

const offer = (id: string, extra: Record<string, unknown> = {}): DepartureOffer => {
  const t = allDepartures.find((x) => x.id === id)!
  const choice = { ...t.choice, ...extra } as DepartureOffer['choice']
  // 札と遺物は中身の名前で並ぶ (engine の rollDepartureOffers と同じ)
  const name = choice.relicId !== undefined ? getRelicDef(choice.relicId).name : choice.addCardIds !== undefined ? choice.addCardIds.map((c) => getCardDef(c).name).join('・') : t.name
  return { id: t.id, kind: t.kind, name, text: t.text, choice, price: t.price, ...(t.shopPrice !== undefined ? { shopPrice: t.shopPrice } : {}) }
}
/** 地図から始めたランに、決めた5品を並べて坑口の店に立たせる (抽選に依らない)。所持金は路銀込みの 150G */
function atDeparture(offers: readonly DepartureOffer[], gold = 150): RunState {
  const base = createRun(4242, 'set-confirm', 'leader_white', undefined, undefined, { departure: false })
  return { ...base, gold, phase: 'departure', departure: { offers, bought: [], leftovers: [] } }
}
/** CLI の書式の簡略版 (本物は sim/play.ts の cardLine・gearLine) */
const FMT: DepartureCliFormat = { card: (d) => d.name, gear: (d) => d.name, deckLine: (c, up) => `${c.def.name}${up ? ' (鍛える候補)' : ''}` }
const relicOf = (run: RunState, rarity: string): string => run.relicQueue.find((id) => getRelicDef(id).rarity === rarity)!
const five = (run: RunState): DepartureOffer[] => [
  offer('dep_unpack'),
  offer('dep_herb'),
  offer('dep_relic_common', { relicId: relicOf(run, 'common') }),
  offer('dep_toolbox', { gears: ['gear_powder', 'gear_spring'] }),
  offer('dep_card_rare', { addCardIds: ['white_grand_charge'] }),
]

describe('出立の店の文 (ui/log.ts)', () => {
  it('全部「N G で買う」。札と遺物は名指しの中身つき。中身の1行は選択履歴 (report.ts) と同じ文', () => {
    const base = atDeparture([])
    const find = offer('dep_relic_uncommon', { relicId: relicOf(base, 'uncommon') })
    expect(departurePriceLabel(find)).toBe('120G で買う')
    const secret = offer('dep_card_rare', { addCardIds: ['white_grand_charge'] })
    expect(departureGainText(secret)).toBe('自分の色のレア札1枚')
    expect(departurePriceLabel(secret)).toBe('100G で買う')
    expect(departurePriceLabel(offer('dep_whet'))).toBe('75G で買う')
    // 代償つきの品はいまの台帳に無い
    for (const t of allDepartures.map((x) => x.id)) expect(departureCostText(offer(t).choice)).toBeNull()
    for (const o of [find, secret, ...five(base)]) expect(departureDetailText(o)).toBe(departureDetail(o))
  })

  it('買えない理由: 買った・Gが足りない・デッキ5枚以下の荷の整理', () => {
    const run = atDeparture(five(atDeparture([])))
    const unpack = run.departure!.offers[0]
    expect(departureUnavailableReason(run, unpack)).toBeNull()
    expect(departureUnavailableReason({ ...run, gold: 30 }, unpack)).toBe('Gが足りない（あと20G）')
    expect(departureUnavailableReason({ ...run, deck: run.deck.slice(0, 5) }, unpack)).toBe('デッキが5枚以下なので取り除けない')
    const afterHerb = applyRunCommand(run, { type: 'BuyDeparture', index: 1 })
    expect(departureUnavailableReason(afterHerb, afterHerb.departure!.offers[1])).toBe('買った')
    expect(departureUnavailableReason({ ...run, gold: 99 }, run.departure!.offers[4])).toBe('Gが足りない（あと1G）')
  })
})

describe('CLI の描画 (ui/log.ts の純関数。sim/play.ts が書式を渡して使う)', () => {
  it('出立の店: 見出し・行商の一言・所持金・品 (値段・名前・中身。種類名は出さない)・対象の札が要る時のデッキ一覧・買う/出るの案内', () => {
    const run = atDeparture(five(atDeparture([])))
    const out = departureCliLines(run, FMT).join('\n')
    expect(out).toContain('出立の店')
    expect(out).toContain(departureMasterLine(run.colors))
    expect(out).toContain('所持 150G')
    expect(out).toContain(' [0] 50G: 「荷の整理」: デッキから1枚を取り除く 【要cardIndex(デッキ番号)】')
    expect(out).toContain(` [2] 100G: 「${getRelicDef(relicOf(run, 'common')).name}」: 古代の遺物を1つ（コモン）`)
    expect(out).toContain(` [4] 100G: 「${getCardDef('white_grand_charge').name}」: 自分の色のレア札1枚`)
    expect(out).not.toContain('サービス「')
    expect(out).not.toContain('品物「')
    expect(out).not.toContain('ツケ')
    expect(out).not.toContain('幕1の店にも並ぶ')
    expect(out).toContain('{"type":"BuyDeparture","index":N}')
    expect(out).toContain('{"type":"LeaveDeparture"}')
    expect(out).toContain('   デッキ:')
    // 買った品は「買った」になり、行方の注記は消える
    const after = applyRunCommand(run, { type: 'BuyDeparture', index: 1 })
    const out2 = departureCliLines(after, FMT).join('\n')
    expect(out2).toContain('「薬草」: 最大HP+8 【買った】')
    expect(out2).toContain('   買った: 薬草')
    expect(out2).toContain('所持 90G')
  })

  it('cmd の後の1行は選択履歴 (report.ts) と同じ文。店を出ると売れ残りが幕1の棚に並び、買えば売切', () => {
    const run = atDeparture(five(atDeparture([])))
    const same = (prev: RunState, cmd: RunCommand): RunState => {
      const next = applyRunCommand(prev, cmd)
      expect(departureChoiceLine(prev, cmd, next)).toBe(describeRunChoice(prev, cmd, next)?.text)
      return next
    }
    const a = same(run, { type: 'BuyDeparture', index: 1 })
    expect(departureChoiceLine(run, { type: 'BuyDeparture', index: 1 }, a)).toBe('出立の店: 薬草（最大HP+8）を60Gで買った')
    const b = same(a, { type: 'BuyDeparture', index: 3 })
    expect(departureChoiceLine(a, { type: 'BuyDeparture', index: 3 }, b)).toBe('出立の店: 道具箱（ギア2個と魔素20＝火薬・発条）を80Gで買った')
    const c = same(b, { type: 'LeaveDeparture' })
    expect(c.phase).toBe('map')
    // 除去・鍛えは普通の店にもあるので行商は担いで降りない (shopPrice なし)
    expect(departureChoiceLine(b, { type: 'LeaveDeparture' }, c)).toBe(
      `出立: 店を出て坑へ（買った: 薬草・道具箱／行商が担いで降りる: ${getRelicDef(relicOf(run, 'common')).name}・${getCardDef('white_grand_charge').name}）`,
    )
    const shop = openShop({ ...c, gold: 500 })
    const lines = shopDepartureCliLines(shop, FMT).join('\n')
    expect(lines).toContain(`支度[1] 100G: 「${getCardDef('white_grand_charge').name}」`)
    expect(lines).not.toContain('荷の整理')
    expect(lines).toContain('{"type":"ShopBuyDeparture","index":N}')
    const i = (shop.shop?.departures ?? []).findIndex((s) => s.id === 'dep_relic_common')
    const bought = same({ ...shop, phase: 'shop' }, { type: 'ShopBuyDeparture', index: i })
    expect(shopDepartureCliLines(bought, FMT).join('\n')).toContain(`支度[${i}] 〔売切〕`)
  })
})

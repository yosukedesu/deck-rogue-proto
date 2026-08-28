// ゴールド・ショップ・?マス (2026-08-28 設計会議) のテスト。
// 確定済みルール表「ゴールド」「ショップ」「?マス（イベント）」を固定する。
import { describe, expect, it } from 'vitest'
import { allEvents, getCardDef, getEventDef } from './content.ts'
import { applyRunCommand, createRun } from './run.ts'
import type { RunState } from './run.ts'
import { chooseToward, defendIntent, withHand, withIntent } from './test-helpers.ts'
import type { GameState } from './types.ts'
import type { MapNode } from './map.ts'

function forceWin(run: RunState): RunState {
  const c = run.combat!
  let surgical: GameState = { ...c, enemies: c.enemies.map((e) => ({ ...e, hp: 1, block: 0 })) }
  surgical = withIntent(withHand(surgical, ['green_sweep']), defendIntent(0))
  surgical = { ...surgical, player: { ...surgical.player, energy: 9 } }
  return applyRunCommand(
    { ...run, combat: surgical },
    { type: 'Combat', command: { type: 'PlayCard', cardUid: 't0_green_sweep' } },
  )
}

/** 指定イベントのノードに立っている状態を外科的に作る (マップ生成の乱数に依存しないため) */
function eventState(seed: number, eventId: string): RunState {
  const run = createRun(seed, 'set-confirm')
  const node: MapNode = { type: 'event', encounterId: null, eventId, next: [] }
  return { ...run, map: [[node]], row: 0, col: 0, phase: 'event' }
}

/** ショップノードに入るまで進める */
function intoShop(seed: number): RunState {
  let run = createRun(seed, 'set-confirm')
  let guard = 0
  while (guard++ < 80) {
    if (run.phase === 'shop') return run
    if (run.phase === 'map') run = chooseToward(run, 'shop')
    else if (run.phase === 'combat') run = forceWin(run)
    else if (run.phase === 'reward') run = applyRunCommand(run, { type: 'SkipReward' })
    else if (run.phase === 'relic-reward') run = applyRunCommand(run, { type: 'SkipRelic' })
    else if (run.phase === 'campfire') run = applyRunCommand(run, { type: 'CampfireRest' })
    else if (run.phase === 'workshop') run = applyRunCommand(run, { type: 'WorkshopSkip' })
    else if (run.phase === 'event') {
      const ev = getEventDef(run.map[run.row][run.col].eventId!)
      run = applyRunCommand(run, { type: 'EventChoice', index: ev.choices.length - 1 })
    } else break
  }
  throw new Error('ショップに到達できない')
}

describe('ゴールド', () => {
  it('初期50G。戦闘勝利で+12〜18G (シードRNGで決定的)', () => {
    let run = createRun(11, 'set-confirm')
    expect(run.gold).toBe(50)
    while (run.phase === 'map') run = chooseToward(run, 'battle')
    run = forceWin(run)
    expect(run.gold).toBeGreaterThanOrEqual(50 + 12)
    expect(run.gold).toBeLessThanOrEqual(50 + 18)
    // 決定論: 同じシード・同じコマンド列なら同じ金額
    let run2 = createRun(11, 'set-confirm')
    while (run2.phase === 'map') run2 = chooseToward(run2, 'battle')
    run2 = forceWin(run2)
    expect(run2.gold).toBe(run.gold)
  })

  it('エリート勝利はさらに+30〜40G', () => {
    let run = createRun(11, 'set-confirm')
    let guard = 0
    while (guard++ < 80 && !(run.phase === 'combat' && run.currentElite)) {
      if (run.phase === 'map') run = chooseToward(run, 'elite')
      else if (run.phase === 'combat') run = forceWin(run)
      else if (run.phase === 'reward') run = applyRunCommand(run, { type: 'SkipReward' })
      else if (run.phase === 'relic-reward') run = applyRunCommand(run, { type: 'SkipRelic' })
      else if (run.phase === 'campfire') run = applyRunCommand(run, { type: 'CampfireRest' })
      else if (run.phase === 'workshop') run = applyRunCommand(run, { type: 'WorkshopSkip' })
      else if (run.phase === 'shop') run = applyRunCommand(run, { type: 'ShopLeave' })
      else if (run.phase === 'event') {
        const ev = getEventDef(run.map[run.row][run.col].eventId!)
        run = applyRunCommand(run, { type: 'EventChoice', index: ev.choices.length - 1 })
      } else break
    }
    const before = run.gold
    run = forceWin(run)
    expect(run.gold - before).toBeGreaterThanOrEqual(12 + 30)
    expect(run.gold - before).toBeLessThanOrEqual(18 + 40)
  })
})

describe('ショップ', () => {
  it('在庫はカード5枚 (色プール・基本札除外)・レリック1個・除去サービス。価格は40+コスト×10+0〜10', () => {
    const run = intoShop(11)
    expect(run.shop).not.toBeNull()
    expect(run.shop!.cards).toHaveLength(5)
    for (const item of run.shop!.cards) {
      const def = getCardDef(item.id)
      expect(def.color).toBe('green')
      expect(item.price).toBeGreaterThanOrEqual(40 + def.cost * 10)
      expect(item.price).toBeLessThanOrEqual(50 + def.cost * 10)
    }
    expect(run.shop!.relicId).not.toBeNull()
    expect(run.shop!.relicPrice).toBe(150)
    expect(run.shop!.removalPrice).toBe(75)
    expect(run.shop!.removalUsed).toBe(false)
  })

  it('購入: ゴールドが減りデッキが増える。ゴールド不足は拒否', () => {
    let run = intoShop(11)
    const item = run.shop!.cards[0]
    run = { ...run, gold: item.price } // ちょうど買える額に
    const before = run.deck.length
    run = applyRunCommand(run, { type: 'ShopBuyCard', index: 0 })
    expect(run.gold).toBe(0)
    expect(run.deck).toHaveLength(before + 1)
    expect(run.deck[run.deck.length - 1].def.id).toBe(item.id)
    expect(run.shop!.cards).toHaveLength(4) // 売り切れ
    expect(() => applyRunCommand(run, { type: 'ShopBuyCard', index: 0 })).toThrow(/ゴールドが足りない/)
  })

  it('レリック購入: 候補列の次の1個。150G', () => {
    let run = intoShop(11)
    run = { ...run, gold: 200 }
    const relicId = run.shop!.relicId!
    run = applyRunCommand(run, { type: 'ShopBuyRelic' })
    expect(run.gold).toBe(50)
    expect(run.relics).toContain(relicId)
    expect(run.shop!.relicId).toBeNull() // 売り切れ
  })

  it('除去サービス: 75Gで1枚除去、1回まで', () => {
    let run = intoShop(11)
    run = { ...run, gold: 200 }
    const before = run.deck.length
    run = applyRunCommand(run, { type: 'ShopRemove', index: 0 })
    expect(run.gold).toBe(125)
    expect(run.deck).toHaveLength(before - 1)
    expect(() => applyRunCommand(run, { type: 'ShopRemove', index: 0 })).toThrow(/1回まで/)
  })

  it('買わずに出られる', () => {
    let run = intoShop(11)
    const gold = run.gold
    run = applyRunCommand(run, { type: 'ShopLeave' })
    expect(run.phase).toBe('map')
    expect(run.gold).toBe(gold)
    expect(run.shop).toBeNull()
  })
})

describe('?マス (イベント)', () => {
  it('規約: 全イベントの最後の選択肢は安全な「立ち去る」(効果なし)', () => {
    for (const ev of allEvents) {
      const last = ev.choices[ev.choices.length - 1]
      expect(last.gold ?? 0, ev.id).toBeGreaterThanOrEqual(0)
      expect(last.hp ?? 0, ev.id).toBeGreaterThanOrEqual(0)
      expect(last.wounds ?? 0, ev.id).toBe(0)
      expect(last.gamble, ev.id).toBeUndefined()
      expect(last.requireGold, ev.id).toBeUndefined()
    }
  })

  it('行商の亡霊: 血を売る = HP-10 / +55G', () => {
    let run = eventState(3, 'event_ghost_peddler')
    const hp = run.hp
    run = applyRunCommand(run, { type: 'EventChoice', index: 0 })
    expect(run.hp).toBe(hp - 10)
    expect(run.gold).toBe(50 + 55)
    expect(run.phase).toBe('map')
  })

  it('行商の亡霊: カードを売る = cardIndex のカードが消えて+35G。指定なしは拒否', () => {
    let run = eventState(3, 'event_ghost_peddler')
    expect(() => applyRunCommand(run, { type: 'EventChoice', index: 1 })).toThrow(/cardIndex/)
    const removed = run.deck[0].uid
    run = applyRunCommand(run, { type: 'EventChoice', index: 1, cardIndex: 0 })
    expect(run.deck.some((c) => c.uid === removed)).toBe(false)
    expect(run.gold).toBe(85)
  })

  it('古木のうろ: 最大HP+7 (現在HPも+7) / 鍛える', () => {
    let run = eventState(3, 'event_ancient_hollow')
    const { hp, maxHp } = run
    run = applyRunCommand(run, { type: 'EventChoice', index: 0 })
    expect(run.maxHp).toBe(maxHp + 7)
    expect(run.hp).toBe(hp + 7)
    // 鍛える (打撃 → 打撃+)
    let run2 = eventState(3, 'event_ancient_hollow')
    const idx = run2.deck.findIndex((c) => c.def.id === 'green_strike')
    run2 = applyRunCommand(run2, { type: 'EventChoice', index: 1, cardIndex: idx })
    expect(run2.deck[idx].def.name).toBe('打撃+')
  })

  it('囁く石碑: レリック1個と引き換えに負傷2がデッキに混入する', () => {
    let run = eventState(3, 'event_whispering_stele')
    const before = run.deck.length
    run = applyRunCommand(run, { type: 'EventChoice', index: 0 })
    expect(run.relics).toHaveLength(1)
    expect(run.deck).toHaveLength(before + 2)
    expect(run.deck.filter((c) => c.def.id === 'status_wound')).toHaveLength(2)
  })

  it('賽の小鬼: 掛け金が足りないと選べない。ロールはシードで決定的', () => {
    let run = eventState(3, 'event_dice_imp')
    run = { ...run, gold: 10 }
    expect(() => applyRunCommand(run, { type: 'EventChoice', index: 0 })).toThrow(/ゴールドが足りない/)
    const a = applyRunCommand(eventState(3, 'event_dice_imp'), { type: 'EventChoice', index: 0 })
    const b = applyRunCommand(eventState(3, 'event_dice_imp'), { type: 'EventChoice', index: 0 })
    expect(a.gold).toBe(b.gold) // 同シード同ロール
    expect([50 - 30, 50 - 30 + 60]).toContain(a.gold) // 外れ20G / 当たり80G
  })

  it('流浪の絵師: ランダムな緑カード1枚を獲得', () => {
    let run = eventState(3, 'event_wandering_painter')
    const before = run.deck.length
    run = applyRunCommand(run, { type: 'EventChoice', index: 0 })
    expect(run.deck).toHaveLength(before + 1)
    expect(run.deck[run.deck.length - 1].def.color).toBe('green')
  })

  it('立ち去る: 何も変わらずマップへ', () => {
    let run = eventState(3, 'event_mossy_chest')
    const snapshot = { hp: run.hp, gold: run.gold, deck: run.deck.length }
    run = applyRunCommand(run, { type: 'EventChoice', index: 2 })
    expect(run.phase).toBe('map')
    expect(run.hp).toBe(snapshot.hp)
    expect(run.gold).toBe(snapshot.gold)
    expect(run.deck).toHaveLength(snapshot.deck)
  })
})

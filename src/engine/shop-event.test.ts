// ゴールド・ショップ・?マス (2026-08-28 設計会議) のテスト。
// 確定済みルール表「ゴールド」「ショップ」「?マス（イベント）」を固定する。
import { describe, expect, it } from 'vitest'
import { allEvents, getCardDef, getEventDef, WOUND_DEF } from './content.ts'
import { applyRunCommand, createRun, eventChoiceNeedsCard, shopRemovalPrice, shopUpgradePrice } from './run.ts'
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
  // ?は入った瞬間に中身が決まる (2026-08-29): eventId は MapNode でなく RunState が持つ
  const node: MapNode = { type: 'event', encounterId: null, next: [] }
  return { ...run, map: [[node]], row: 0, col: 0, phase: 'event', eventId }
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
      const ev = getEventDef(run.eventId!)
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
        const ev = getEventDef(run.eventId!)
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
    expect(shopRemovalPrice(run)).toBe(75)
    expect(shopUpgradePrice(run)).toBe(100)
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

  it('除去サービス: 回数無制限・使うたびラン通算で+25G逓増 (本家Purge式。2026-08-29)', () => {
    let run = intoShop(11)
    run = { ...run, gold: 300 }
    const before = run.deck.length
    run = applyRunCommand(run, { type: 'ShopRemove', index: 0 })
    expect(run.gold).toBe(300 - 75)
    expect(run.deck).toHaveLength(before - 1)
    expect(shopRemovalPrice(run)).toBe(100) // 逓増
    run = applyRunCommand(run, { type: 'ShopRemove', index: 0 })
    expect(run.gold).toBe(300 - 75 - 100)
    expect(run.deck).toHaveLength(before - 2)
  })

  it('強化サービス: 100G+使うたび+30G逓増。焚き火の「鍛える」と同じ3段仕様', () => {
    let run = intoShop(11)
    run = { ...run, gold: 300 }
    const idx = run.deck.findIndex((c) => c.def.id === 'green_strike')
    run = applyRunCommand(run, { type: 'ShopUpgrade', index: idx })
    expect(run.gold).toBe(300 - 100)
    expect(run.deck[idx].def.name).toBe('打撃+')
    expect(shopUpgradePrice(run)).toBe(130) // 逓増
    // 強化済みは拒否
    expect(() => applyRunCommand(run, { type: 'ShopUpgrade', index: idx })).toThrow(/すでに鍛えられている/)
  })

  it('旧セーブ互換: removalCount/upgradeCount 欠落でも価格がNaNにならず、利用でゴールドが汚染されない (2026-08-29 幕3検証で発見)', () => {
    let run = intoShop(11)
    // フィールド導入前のセーブ読み込みを再現 (JSONにキーが無い = undefined)
    const legacy = { ...run, gold: 300 } as Record<string, unknown>
    delete legacy.removalCount
    delete legacy.upgradeCount
    run = legacy as unknown as RunState
    expect(shopRemovalPrice(run)).toBe(75)
    expect(shopUpgradePrice(run)).toBe(100)
    run = applyRunCommand(run, { type: 'ShopRemove', index: 0 })
    expect(run.gold).toBe(300 - 75)
    expect(Number.isNaN(run.gold)).toBe(false)
    expect(run.removalCount).toBe(1)
    const idx = run.deck.findIndex((c) => c.def.id === 'green_strike')
    run = applyRunCommand(run, { type: 'ShopUpgrade', index: idx })
    expect(run.gold).toBe(300 - 75 - 100)
    expect(run.upgradeCount).toBe(1)
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
  it('規約: 全イベントの最後の選択肢は安全な「立ち去る」(既知の安全キー以外を一切持たない)', () => {
    // ホワイトリスト方式 (2026-08-29)。旧実装は5フィールドを名指しで見るだけだったので
    // removeCard/relic/maxHp と新フィールド (hpRatio/transformCard/duplicateCard/
    // upgradeRandomCards/removeAllWounds) が全部素通りしていた。イベントを20個足すと
    // simボットの壊れ検知が黙って死ぬので、未知のキーはすべて弾く
    const SAFE_KEYS = new Set(['label', 'gold', 'hp'])
    for (const ev of allEvents) {
      const last = ev.choices[ev.choices.length - 1]
      const extra = Object.keys(last).filter((k) => !SAFE_KEYS.has(k))
      expect(extra, `${ev.id} の「立ち去る」が未知のキーを持つ`).toEqual([])
      expect(last.gold ?? 0, ev.id).toBeGreaterThanOrEqual(0)
      expect(last.hp ?? 0, ev.id).toBeGreaterThanOrEqual(0)
    }
  })

  it('規約: eventChoiceNeedsCard が false の選択肢は cardIndex なしで必ず解決できる', () => {
    // UI/CLI はこの判定だけを見て対象選択を出す。判定漏れがあると
    // 「対象を選べないのに cardIndex を要求される」ダイアログになる
    // (2026-08-30 変転の祠 transformCard / 写しの泉 duplicateCard で実際に発生)
    const missed: string[] = []
    for (const ev of allEvents) {
      for (const [i, c] of ev.choices.entries()) {
        if (eventChoiceNeedsCard(c)) continue
        const run = eventState(5, ev.id)
        try {
          applyRunCommand(run, { type: 'EventChoice', index: i })
        } catch (e) {
          if (String(e).includes('cardIndex')) missed.push(`${ev.name}/${c.label}`)
        }
      }
    }
    expect(missed).toEqual([])
  })

  it('規約: 全イベントの最後の選択肢は cardIndex も所持金も要求せず必ず解決できる', () => {
    // simボットは常に最後を選ぶ約束なので、1件でも throw すると壊れ検知が止まる
    for (const ev of allEvents) {
      const run = eventState(5, ev.id)
      expect(
        () => applyRunCommand(run, { type: 'EventChoice', index: ev.choices.length - 1 }),
        ev.id,
      ).not.toThrow()
    }
  })

  it('イベントプールは本家3層の員数を満たす (幕専用6個以上/幕・祠6・ワンタイム4)', () => {
    const kindOf = (e: (typeof allEvents)[number]) => e.kind ?? 'act'
    expect(allEvents.filter((e) => kindOf(e) === 'shrine')).toHaveLength(6)
    expect(allEvents.filter((e) => kindOf(e) === 'oneTime')).toHaveLength(4)
    for (const act of [1, 2, 3]) {
      const pool = allEvents.filter((e) => kindOf(e) === 'act' && e.act === act)
      expect(pool.length, `幕${act}の幕専用イベント`).toBeGreaterThanOrEqual(6)
    }
    // 幕専用は必ず act を持つ (持たないと全幕に出てしまう)
    for (const e of allEvents) {
      if (kindOf(e) === 'act') expect(e.act, `${e.id} に act が無い`).toBeDefined()
    }
    expect(new Set(allEvents.map((e) => e.id)).size).toBe(allEvents.length)
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
    expect([50 - 30, 50 - 30 + 90]).toContain(a.gold) // 外れ20G / 当たり110G (フレーバー「3倍にして返す」=掛け金30の3倍90が戻る)
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

describe('イベントの新効果 (2026-08-29 本家踏襲の拡充)', () => {
  it('hpRatio: 最大HP比で回復する (癒しの泉=20%)', () => {
    let run = eventState(3, 'event_healing_spring')
    run = { ...run, hp: 10 }
    const expected = Math.min(run.maxHp, 10 + Math.trunc(run.maxHp * 0.2))
    run = applyRunCommand(run, { type: 'EventChoice', index: 0 })
    expect(run.hp).toBe(expected)
  })

  it('hpRatio: 最大HPを超えて回復しない', () => {
    let run = eventState(3, 'event_healing_spring')
    run = { ...run, hp: run.maxHp }
    run = applyRunCommand(run, { type: 'EventChoice', index: 0 })
    expect(run.hp).toBe(run.maxHp)
  })

  it('transformCard: 同じレアリティの別カードに置き換わる (デッキ枚数は不変)', () => {
    const run = eventState(7, 'shrine_transmute')
    const before = run.deck.length
    const target = run.deck[0]
    const next = applyRunCommand(run, { type: 'EventChoice', index: 0, cardIndex: 0 })
    expect(next.deck).toHaveLength(before)
    if (next.deck[0].def.id !== target.def.id) {
      // 置換が起きたなら同じレアリティであること (レア3%の希少性を迂回しない)
      expect(next.deck[0].def.rarity ?? 'common').toBe(target.def.rarity ?? 'common')
    }
  })

  it('duplicateCard: 同じ def が1枚増え、uid は一意のまま', () => {
    const run = eventState(7, 'shrine_duplicate')
    const before = run.deck.length
    const target = run.deck[0]
    const next = applyRunCommand(run, { type: 'EventChoice', index: 0, cardIndex: 0 })
    expect(next.deck).toHaveLength(before + 1)
    expect(next.deck.filter((c) => c.def.id === target.def.id).length).toBeGreaterThanOrEqual(2)
    expect(new Set(next.deck.map((c) => c.uid)).size).toBe(next.deck.length)
  })

  it('removeAllWounds: 負傷を全て取り除く (0枚でも throw しない)', () => {
    const run = eventState(11, 'event_divine_fountain')
    const withWounds: RunState = {
      ...run,
      deck: [...run.deck, { uid: 'w1', def: WOUND_DEF }, { uid: 'w2', def: WOUND_DEF }],
    }
    const next = applyRunCommand(withWounds, { type: 'EventChoice', index: 0 })
    expect(next.deck.some((c) => c.def.id === WOUND_DEF.id)).toBe(false)
    // 負傷0枚でも例外にならない
    expect(() => applyRunCommand(run, { type: 'EventChoice', index: 0 })).not.toThrow()
  })
})

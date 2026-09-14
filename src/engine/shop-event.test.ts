// ゴールド・ショップ・?マス (2026-08-28 設計会議) のテスト。
// 確定済みルール表「ゴールド」「ショップ」「?マス（イベント）」を固定する。
import { describe, expect, it } from 'vitest'
import { chainFromStart } from './enemyGraph.ts'
import { getEnemyDef, allEvents, getCardDef, getEventDef, WOUND_DEF } from './content.ts'
import { applyRunCommand, canUpgradeCard, createRun, defaultEventChoice, eventChoiceNeedsCard, eventPlayable, pickEvent, shopRemovalPrice, shopUpgradePrice, upgradeCard } from './run.ts'
import type { RunState } from './run.ts'
import { chooseToward, defendIntent, withHand, withIntent } from './test-helpers.ts'
import type { GameState } from './types.ts'
import type { MapNode } from './map.ts'

function forceWin(run: RunState): RunState {
  // 分裂・残機・孵化で戦闘が続く敵 (蘇る合成獣など) は全滅→再出現を繰り返すので、決着まで薙ぎ払いを反復する (2026-09-02)
  let r = run
  for (let guard = 0; guard < 6 && r.phase === 'combat' && r.combat !== null; guard++) {
    const c = r.combat
    let surgical: GameState = { ...c, enemies: c.enemies.map((e) => ({ ...e, hp: 1, block: 0 })) }
    surgical = withIntent(withHand(surgical, ['green_sweep']), defendIntent(0))
    surgical = { ...surgical, player: { ...surgical.player, energy: 9 } }
    r = applyRunCommand(
      { ...r, combat: surgical },
      { type: 'Combat', command: { type: 'PlayCard', cardUid: 't0_green_sweep' } },
    )
  }
  return r
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
      run = applyRunCommand(run, defaultEventChoice(run)) // 既定の選択 (2026-09-14)
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
        run = applyRunCommand(run, defaultEventChoice(run)) // 既定の選択 (2026-09-14)
      } else break
    }
    const before = run.gold
    run = forceWin(run)
    expect(run.gold - before).toBeGreaterThanOrEqual(12 + 30)
    expect(run.gold - before).toBeLessThanOrEqual(18 + 40)
  })
})

describe('ショップ', () => {
  it('在庫はカード5枚+レア枠1 (2026-08-31 ゴールドシンク)・レリック1個・除去サービス', () => {
    const run = intoShop(11)
    expect(run.shop).not.toBeNull()
    expect(run.shop!.cards).toHaveLength(6)
    for (const item of run.shop!.cards.slice(0, 5)) {
      const def = getCardDef(item.id)
      expect(def.color).toBe('green')
      // X札は典型X=3で値付けされる (確定済みルール表「Xコスト」)
      const priceCost = def.xCost === true ? 3 : def.cost
      expect(item.price).toBeGreaterThanOrEqual(40 + priceCost * 10)
      expect(item.price).toBeLessThanOrEqual(50 + priceCost * 10)
    }
    // 6枠目 = レア確定・高額 (120+コスト×10)
    const rare = run.shop!.cards[5]
    const rareDef = getCardDef(rare.id)
    expect(rareDef.rarity).toBe('rare')
    expect(rare.price).toBe(120 + (rareDef.xCost === true ? 3 : rareDef.cost) * 10)
    expect(run.shop!.relicId).not.toBeNull()
    expect(run.shop!.relicPrice).toBe(150)
    expect(shopRemovalPrice(run)).toBe(50)
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
    // index は詰めない = 売切マーク (2026-08-31 黒ラン: 連続購入で別商品を掴んだ事故への処方)
    expect(run.shop!.cards).toHaveLength(6)
    expect(run.shop!.cards[0].sold).toBe(true)
    expect(() => applyRunCommand(run, { type: 'ShopBuyCard', index: 0 })).toThrow(/売り切れ/)
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

  it('除去サービス: 回数無制限・使うたびラン通算で+50G逓増 (2026-08-31 シンク強化)', () => {
    let run = intoShop(11)
    run = { ...run, gold: 300 }
    const before = run.deck.length
    run = applyRunCommand(run, { type: 'ShopRemove', index: 0 })
    expect(run.gold).toBe(300 - 50)
    expect(run.deck).toHaveLength(before - 1)
    expect(shopRemovalPrice(run)).toBe(75) // 逓増 (+25。2026-09-03)
    run = applyRunCommand(run, { type: 'ShopRemove', index: 0 })
    expect(run.gold).toBe(300 - 50 - 75)
    expect(run.deck).toHaveLength(before - 2)
  })

  it('強化サービス: 100G+使うたび+50G逓増 (2026-08-31 シンク強化)。幕1でも使える (2026-09-01 制限撤廃)', () => {
    const act1 = applyRunCommand({ ...intoShop(11), gold: 300 }, { type: 'ShopUpgrade', index: 0 })
    expect(act1.deck.some((c) => c.def.name.endsWith('+'))).toBe(true)
    let run = { ...intoShop(11), act: 2 }
    run = { ...run, gold: 300 }
    const idx = run.deck.findIndex((c) => c.def.id === 'green_strike')
    run = applyRunCommand(run, { type: 'ShopUpgrade', index: idx })
    expect(run.gold).toBe(300 - 100)
    expect(run.deck[idx].def.name).toBe('打撃+')
    expect(shopUpgradePrice(run)).toBe(150) // 逓増
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
    expect(shopRemovalPrice(run)).toBe(50)
    expect(shopUpgradePrice(run)).toBe(100)
    run = applyRunCommand(run, { type: 'ShopRemove', index: 0 })
    expect(run.gold).toBe(300 - 50)
    expect(Number.isNaN(run.gold)).toBe(false)
    expect(run.removalCount).toBe(1)
    // 個性注入 (2026-08-31) で打撃は1枚になったため、除去で消えていない防御を対象にする
    run = { ...run, act: 2 } // 幕1の強化サービス封鎖 (2026-08-31) を跨ぐ
    const idx = run.deck.findIndex((c) => c.def.id === 'green_guard')
    run = applyRunCommand(run, { type: 'ShopUpgrade', index: idx })
    expect(run.gold).toBe(300 - 50 - 100)
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
  /**
   * 無料の「立ち去る」を残す取引型 (2026-09-14 ユーザー裁定「?マスはスキップできない方がいい」→ 本家形:
   * レリック/呪い/賭けを持ちかける取引だけ「断る」を残し〔本家 Golden Idol・Scrap Ooze・Old Beggar〕、
   * 恵み型は撤去=踏んだら必ず何かが起きる〔本家 Big Fish・Falling・Wheel of Change〕)。
   * 分類はここで機械固定する = 新しいイベントは無料の選択肢を持たないのが既定
   */
  const FREE_LEAVE_EVENTS = [
    'event_ghost_peddler', 'event_dice_imp', 'event_lost_peddler', 'event_broken_stairs', 'event_fairy_market',
    'event_wing_statue', 'event_old_beggar', 'event_forgotten_altar', 'event_mausoleum', 'event_moai',
    'event_tower_tailor', 'event_obsidian_idol', 'event_blood_altar', 'event_guilty_bargain', 'event_three_cups',
  ]
  const isFreeChoice = (c: (typeof allEvents)[number]['choices'][number]): boolean =>
    Object.keys(c).every((k) => k === 'label')

  it('規約: 無料の「立ち去る」は取引型15件だけ。それ以外の選択肢は必ず何かを起こす', () => {
    for (const ev of allEvents) {
      const free = ev.choices.filter(isFreeChoice)
      if (FREE_LEAVE_EVENTS.includes(ev.id)) {
        expect(free.map((c) => c.label), ev.id).toEqual(['立ち去る'])
        expect(ev.choices[ev.choices.length - 1].label, `${ev.id} の立ち去るは最後`).toBe('立ち去る')
      } else {
        expect(free, `${ev.id} に無料の選択肢がある`).toEqual([])
      }
      expect(ev.choices.length, ev.id).toBeGreaterThanOrEqual(1)
    }
    expect(allEvents.filter((e) => FREE_LEAVE_EVENTS.includes(e.id))).toHaveLength(FREE_LEAVE_EVENTS.length)
  })

  it('規約: 全イベントは既定の状態で選べる選択肢を持ち、既定の選択 (defaultEventChoice) は必ず解決できる', () => {
    // 旧規約「最後の選択肢=立ち去るを選ぶ」の後継。simボットは defaultEventChoice を選ぶので、1件でも throw すると壊れ検知が止まる
    for (const ev of allEvents) {
      const run = eventState(5, ev.id)
      expect(eventPlayable(run, ev), ev.id).toBe(true)
      expect(() => applyRunCommand(run, defaultEventChoice(run)), ev.id).not.toThrow()
    }
  })

  it('既定の選択: 後ろから「代償の無い」選択肢を選び、無ければ致死でないものを選ぶ', () => {
    // 黄金の祠: 最後は +190G+負傷2 (代償あり) なので、その手前の +90G を選ぶ
    const gold = eventState(5, 'shrine_gold')
    expect(defaultEventChoice(gold)).toEqual({ type: 'EventChoice', index: 0 })
    // 淵の大魚: 籠 (負傷1) を避けて 雫 (最大HP+5) を選ぶ
    const fish = eventState(5, 'event_big_fish')
    expect(defaultEventChoice(fish)).toEqual({ type: 'EventChoice', index: 1 })
    // 研ぎの祠 (1択・対象カードが要る): 鍛えられる先頭の札を対象にする
    const whet = eventState(5, 'shrine_whetstone')
    const cmd = defaultEventChoice(whet)
    expect(cmd.type === 'EventChoice' && cmd.cardIndex !== undefined && canUpgradeCard(whet.deck[cmd.cardIndex])).toBe(true)
    // 眠り茸 (両方に代償): HP-8 が致死なら 食べる (負傷1) 側へ
    const low = { ...eventState(5, 'event_mushrooms'), hp: 8 }
    expect(defaultEventChoice(low)).toEqual({ type: 'EventChoice', index: 0 })
    expect(defaultEventChoice(eventState(5, 'event_mushrooms'))).toEqual({ type: 'EventChoice', index: 1 })
    // 取引型は従来どおり最後の「立ち去る」
    const idol = eventState(5, 'event_obsidian_idol')
    expect(defaultEventChoice(idol)).toEqual({ type: 'EventChoice', index: 2 })
  })

  it('詰み防止: 選べる選択肢が無いイベントは抽選で引かれない (全て鍛え済みのデッキに研ぎの祠)', () => {
    const base = createRun(5, 'set-confirm')
    const allUpgraded: RunState = { ...base, deck: base.deck.map((c) => (canUpgradeCard(c) ? upgradeCard(c) : c)) }
    expect(eventPlayable(allUpgraded, getEventDef('shrine_whetstone'))).toBe(false)
    expect(eventPlayable(base, getEventDef('shrine_whetstone'))).toBe(true)
    // 100回引いても研ぎの祠は出ない (祠プールを強制するため幕専用を全部既出にする)
    const seenAll = allEvents.filter((e) => (e.kind ?? 'act') === 'act').map((e) => e.id)
    let rng = allUpgraded.rng
    const drawn = new Set<string>()
    for (let i = 0; i < 100; i++) {
      const [id, r] = pickEvent({ ...allUpgraded, seenEventIds: seenAll }, rng)
      rng = r
      drawn.add(id)
    }
    expect(drawn.has('shrine_whetstone')).toBe(false)
    expect(drawn.size).toBeGreaterThan(3)
    // 所持金が足りなくても「断る」を残した取引型は引かれる (本家 Old Beggar 型)
    expect(eventPlayable({ ...base, gold: 0 }, getEventDef('event_old_beggar'))).toBe(true)
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

  it('イベントプールは本家3層の員数を満たす (幕専用6個以上/幕・祠7・ワンタイム6)', () => {
    // 2026-09-02 呪いイベント+2 (黒曜の偶像=oneTime・血染めの祭壇=shrine)
    const kindOf = (e: (typeof allEvents)[number]) => e.kind ?? 'act'
    expect(allEvents.filter((e) => kindOf(e) === 'shrine')).toHaveLength(7)
    expect(allEvents.filter((e) => kindOf(e) === 'oneTime')).toHaveLength(6) // 2026-09-02 毒の三杯 (どの毒を飲むかの選択)
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

  it('立ち去る (取引型に残る無料の断り): 何も変わらずマップへ', () => {
    let run = eventState(3, 'event_obsidian_idol')
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

describe('呪いイベント (2026-09-02 敵ギミック第1波D)', () => {
  it('brands: 呪いの烙印がデッキに混入する (恒久・除去可能な呪い)', () => {
    let run = createRun(7, 'set-confirm')
    run = { ...run, phase: 'event', eventId: 'event_obsidian_idol' }
    const after = applyRunCommand(run, { type: 'EventChoice', index: 0 }) // 金箔を剥ぐ
    expect(after.gold).toBe(run.gold + 130)
    expect(after.deck.filter((c) => c.def.id === 'status_brand')).toHaveLength(1)
  })

  it('岩皮の甲虫: 低HP+潜伏の殻12+守りつつ突くローテ (粉砕・貫通の的。2026-09-03 開幕ブロック→潜伏へ昇格)', () => {
    const def = getEnemyDef('enemy_rock_beetle')
    expect(def.maxHp).toBe(30)
    expect(def.burrow?.block).toBe(12)
    expect(def.moves.some((m) => m.id === def.burrow?.bite)).toBe(true)
    expect(chainFromStart(def, 3)).toEqual(['horn_jab', 'harden', 'horn_jab']) // 攻撃ステップあり=膠着破り則
  })
})

describe('イベント定義のスキーマ規約 (2026-09-02 レビュー是正: text キー事故の再発防止)', () => {
  it('全イベントが flavor と sprite を持ち、イベントレベルの未知キーを持たない', () => {
    const KNOWN = new Set(['id', 'kind', 'act', 'name', 'sprite', 'flavor', 'choices'])
    for (const e of allEvents) {
      expect(typeof e.flavor, `${e.id} に flavor が無い`).toBe('string')
      expect(typeof e.sprite, `${e.id} に sprite が無い`).toBe('string')
      for (const k of Object.keys(e)) {
        expect(KNOWN.has(k), `${e.id} に未知キー ${k}`).toBe(true)
      }
    }
  })
})


describe('会員証 (shopPriceRatio) の適用範囲 (2026-09-05 Opusラン Q の指摘)', () => {
  it('レア枠の価格にも会員証が効き、同じ店で買った瞬間に未売の在庫が値下がりする', async () => {
    const { allRelics } = await import('./content.ts')
    const { openShop } = await import('./run.ts')
    const member = allRelics.find((r) => (r.bonus?.shopPriceRatio ?? 1) < 1)
    if (!member) throw new Error('会員証が無い')
    const ratio = member.bonus!.shopPriceRatio!
    // 会員証を持って店に入る: レア枠 (最後の商品) にも比率が掛かる
    const withCard = openShop({ ...createRun(21, 'set-confirm'), relics: [member.id] })
    const plain = openShop(createRun(21, 'set-confirm'))
    const rareA = withCard.shop!.cards[withCard.shop!.cards.length - 1]
    const rareB = plain.shop!.cards[plain.shop!.cards.length - 1]
    expect(rareA.id).toBe(rareB.id) // 同じシード=同じ在庫
    expect(rareA.price).toBe(Math.floor(rareB.price * ratio))
    // 同じ店で会員証を買う: 未売の在庫がその場で値下がりする
    let run2: RunState = { ...plain, gold: 999, shop: { ...plain.shop!, relicId: member.id } }
    const before = run2.shop!.cards.map((c) => c.price)
    run2 = applyRunCommand(run2, { type: 'ShopBuyRelic' })
    run2.shop!.cards.forEach((c, i) => expect(c.price).toBe(Math.floor(before[i] * ratio)))
  })
})

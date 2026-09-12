// レリック本家形 (2026-09-12 docs/relic-analysis-2026-09-12.md) の機構テスト。
// 戦闘側: every/once カウンタ・ターンN条件・onTurnEnd/onShuffle/onEnemyDied/onDamageTaken・次ターン繰り越し・C型の規則改変。
// ラン側: gainRelic の一本道 (取得時一回効果・relic-choose)・卵・祈りの車輪・翼の靴・焚き火の第3選択肢・時限レリック・烙印の受け皿・色ゲート
import { describe, expect, it } from 'vitest'
import { allEnemies, allRelics, buildDeck, buildRelicPermanent, getCardDef, getEventDef, getRelicDef } from './content.ts'
import { startCombatWithOptions, type CombatOptions } from './combat.ts'
import { applyCommand } from './state.ts'
import { attackIntent, chooseToward, defendIntent, withHand, withIntent } from './test-helpers.ts'
import {
  addCardsToRunDeck, applyRunCommand, campfireOptions, createRun, drawRelicOptions, gainRelic, isUpgraded, nextChoices, relicAllowedForColors,
  relicStateOf, wingChoices, type RunState,
} from './run.ts'
import type { EnemyIntent, GameState } from './types.ts'

const ENEMY = allEnemies.find((e) => !('splitInto' in e) && !('hatchInto' in e) && !('burrow' in e) && e.nemesis !== true && e.turnArmor === undefined && e.armor === undefined && e.thorns === undefined && e.regen === undefined)!.id

function combatWith(relicIds: readonly string[], rules: Partial<CombatOptions> = {}, seed = 7): GameState {
  return startCombatWithOptions(seed, 'set-confirm', ENEMY, {
    deck: buildDeck('starter'),
    relicPermanents: relicIds.map((id) => buildRelicPermanent(getRelicDef(id))),
    ...rules,
  })
}
const tough = (s: GameState): GameState => ({ ...s, enemies: s.enemies.map((e) => ({ ...e, hp: 999, maxHp: 999, block: 0 })) })
const energize = (s: GameState, n: number): GameState => ({ ...s, player: { ...s.player, energy: n } })
const play = (s: GameState, uid: string, extra: Record<string, unknown> = {}): GameState => applyCommand(s, { type: 'PlayCard', cardUid: uid, ...extra })
const endTurn = (s: GameState, intent: EnemyIntent): GameState => applyCommand(withIntent(s, intent), { type: 'EndTurn' })
const strikes = (n: number): string[] => Array.from({ length: n }, () => 'green_strike')

/** 発掘の鶴嘴の撤去 (2026-09-13): 在庫のどのレリックも campfireDig を持たない */
function allRelicsHaveNoDig(): boolean {
  return allRelics.every((r) => r.bonus?.campfireDig !== true)
}

describe('every/once カウンタ (投げ刃の束・墨壺・百年の謎かけ)', () => {
  it('投げ刃の束: 1ターンに攻撃札3枚ごとに成長+1。4枚目では増えず、次のターンは0から数え直す', () => {
    let s = energize(tough(withHand(combatWith(['relic_throwing_blades']), strikes(4))), 9)
    s = play(s, 't0_green_strike'); s = play(s, 't1_green_strike')
    expect(s.player.growth).toBe(0)
    s = play(s, 't2_green_strike')
    expect(s.player.growth).toBe(1)
    s = play(s, 't3_green_strike')
    expect(s.player.growth).toBe(1)
    s = endTurn(s, defendIntent(5))
    s = energize(withHand(s, strikes(3)), 9)
    s = play(s, 't0_green_strike'); s = play(s, 't1_green_strike'); s = play(s, 't2_green_strike')
    expect(s.player.growth).toBe(2) // ターン内カウンタがリセットされた
  })

  it('墨壺: 10枚プレイするたび1ドロー (戦闘内累計)', () => {
    let s = energize(tough(withHand(combatWith(['relic_ink_pot']), strikes(10))), 99)
    for (let i = 0; i < 9; i++) s = play(s, `t${i}_green_strike`)
    expect(s.player.hand).toHaveLength(1)
    s = play(s, 't9_green_strike')
    expect(s.player.hand).toHaveLength(1) // 10枚目で1ドロー
  })

  it('百年の謎かけ: 戦闘で最初に攻撃でHPを失った時だけ、次のターンのドロー+3', () => {
    let s = tough(withHand(combatWith(['relic_centennial_puzzle']), ['green_strike'])) // 保持札を手札から外す
    s = endTurn(s, attackIntent(5))
    expect(s.player.hand).toHaveLength(s.player.drawPerTurn + 3)
    const retained = s.player.hand.filter((c) => c.def.retain === true).length // 保持札 (大樹の怒り等) は残る
    s = endTurn(s, attackIntent(5))
    expect(s.player.hand).toHaveLength(s.player.drawPerTurn + retained) // 2回目は鳴らない
  })
})

describe('ターン条件と新トリガー', () => {
  it('角の留め具: 2ターン目の開始時にだけブロック12', () => {
    let s = tough(combatWith(['relic_horn_cleat']))
    expect(s.player.block).toBe(0)
    s = endTurn(s, defendIntent(5))
    expect(s.turn).toBe(2)
    expect(s.player.block).toBe(12)
    s = endTurn(s, defendIntent(5))
    expect(s.player.block).toBe(0)
  })

  it('山銅の板: ターン終了時にブロックが0なら6得る = 直後の攻撃5を無傷で受ける', () => {
    let s = tough(withHand(combatWith(['relic_orichalcum']), []))
    const hp = s.player.hp
    s = endTurn(s, attackIntent(5))
    expect(s.player.hp).toBe(hp)
  })

  it('算盤: 山札を切り直すたびブロック6', () => {
    let s = tough(combatWith(['relic_abacus']))
    s = { ...s, player: { ...s.player, hand: [], drawPile: [], discardPile: [...s.player.drawPile, ...s.player.hand] } }
    s = endTurn(s, defendIntent(5))
    expect(s.eventLog.some((e) => e.type === 'DeckShuffled')).toBe(true)
    expect(s.player.block).toBe(6)
  })

  it('小鬼の角笛: 敵を倒すたび一時マナ+1と1ドロー', () => {
    let s = withHand(combatWith(['relic_gremlin_horn']), ['green_strike'])
    s = { ...s, enemies: s.enemies.map((e) => ({ ...e, hp: 1, block: 0 })) }
    s = energize(s, 3)
    s = play(s, 't0_green_strike')
    expect(s.eventLog.some((e) => e.type === 'EnemyDied')).toBe(true)
    expect(s.player.energy).toBe(3) // 3-1+1
    expect(s.player.hand).toHaveLength(1) // ドロー1
  })

  it('兵法書: 攻撃札を打たずに終えたターンの次のターン、一時マナ+1', () => {
    let s = tough(withHand(combatWith(['relic_art_of_war']), strikes(1)))
    s = endTurn(s, defendIntent(5))
    expect(s.player.energy).toBe(s.player.energyMax + 1)
    s = energize(withHand(s, strikes(1)), 3)
    s = play(s, 't0_green_strike')
    s = endTurn(s, defendIntent(5))
    expect(s.player.energy).toBe(s.player.energyMax) // 攻撃したので鳴らない
  })
})

describe('C型の規則改変', () => {
  it('溶けない氷菓: 余ったエナジーを次のターンへ持ち越す (T1 は素の値)', () => {
    let s = tough(combatWith([], { energyCarry: true }))
    expect(s.player.energy).toBe(s.player.energyMax)
    s = endTurn(s, defendIntent(5))
    expect(s.player.energy).toBe(s.player.energyMax * 2)
  })

  it('頑丈な留め具: ターン開始時にブロックを10まで持ち越す', () => {
    let s = tough(combatWith([], { blockKeep: 10 }))
    s = { ...s, player: { ...s.player, block: 15 } }
    s = endTurn(s, defendIntent(5))
    expect(s.player.block).toBe(10)
  })

  it('ルーンの角錐: 手札を捨てない (次のターンは 5+5 枚)', () => {
    let s = tough(combatWith([], { retainHand: true }))
    const n = s.player.hand.length
    s = endTurn(s, defendIntent(5))
    expect(s.player.hand).toHaveLength(n + s.player.drawPerTurn)
  })

  it('蜥蜴の尾: 致死を1度だけ耐えて最大HPの半分で立つ', () => {
    let s = tough(withHand(combatWith([], { deathSave: true }), []))
    s = { ...s, player: { ...s.player, hp: 3 } }
    s = endTurn(s, attackIntent(10))
    expect(s.phase).toBe('player-turn')
    expect(s.player.hp).toBe(Math.floor(s.player.maxHp / 2))
    expect(s.deathSaveUsed).toBe(true)
    s = { ...s, player: { ...s.player, hp: 3, block: 0 } }
    s = endTurn(withHand(s, []), attackIntent(10))
    expect(s.phase).toBe('lost') // 2度目は無い
  })

  it('天鵞絨の首輪: 1ターンに6枚まで', () => {
    let s = energize(tough(withHand(combatWith([], { playCap: 6 }), strikes(7))), 99)
    for (let i = 0; i < 6; i++) s = play(s, `t${i}_green_strike`)
    expect(() => play(s, 't6_green_strike')).toThrow(/6枚まで/)
  })

  it('増幅の薬: X札の X に+2 (支払いは増えない)', () => {
    const def = getCardDef('green_x_vine_flurry')
    const per = def.effects[0].amount ?? 0
    let s = energize(tough(withHand(combatWith([], { xBonus: 2 }), ['green_x_vine_flurry'])), 3)
    s = play(s, 't0_green_x_vine_flurry', { xAmount: 1 })
    expect(s.player.energy).toBe(2)
    expect(s.enemies[0].hp).toBe(999 - per * 3)
  })

  it('時計仕掛けの土産: 状態異常の付与を1回弾く', () => {
    let s = tough(withHand(combatWith([], { artifact: 1 }), []))
    s = endTurn(s, { ...attackIntent(1), inflict: { status: 'weak', amount: 2 } })
    expect(s.player.weak).toBe(0)
    expect(s.player.artifact).toBe(0)
    s = endTurn(withHand(s, []), { ...attackIntent(1), inflict: { status: 'weak', amount: 2 } })
    expect(s.player.weak).toBe(2)
  })

  it('重金の棒・古い門柱・脈打つ欠片: 免疫でなく上限と割合で受ける', () => {
    const lossWith = (rules: Partial<CombatOptions>, atk: number): number => {
      const s0 = tough(withHand(combatWith([], rules), []))
      const s1 = endTurn(s0, attackIntent(atk))
      return s0.player.hp - s1.player.hp
    }
    expect(lossWith({ hpLossReduce: 1 }, 5)).toBe(4)
    expect(lossWith({ smallHitToOne: 5 }, 5)).toBe(1)
    expect(lossWith({ smallHitToOne: 5 }, 6)).toBe(6)
    expect(lossWith({ maxHpLossPerTurn: 20 }, 30)).toBe(20)
  })

  it('青い蝋燭: 烙印を 0E・HP-1・消滅 でプレイできる', () => {
    let s = tough(withHand(combatWith([], { brandsPlayable: true }), ['status_brand']))
    const hp = s.player.hp
    s = play(s, 't0_status_brand')
    expect(s.player.hp).toBe(hp - 1)
    expect(s.player.exhaustPile.some((c) => c.def.id === 'status_brand')).toBe(true)
    expect(() => play(tough(withHand(combatWith([]), ['status_brand'])), 't0_status_brand')).toThrow()
  })
})

// ---- ラン側 ----
function skipUntil(run0: RunState, target: RunState['phase'], toward: 'battle' | 'campfire' | 'shop' | 'workshop' = 'battle'): RunState {
  let run = run0
  for (let guard = 0; guard < 80 && run.phase !== target; guard++) {
    if (run.phase === 'map') run = chooseToward(run, toward)
    else if (run.phase === 'combat') run = forceWin(run)
    else if (run.phase === 'campfire') run = applyRunCommand(run, { type: 'CampfireRest' })
    else if (run.phase === 'workshop') run = applyRunCommand(run, { type: 'WorkshopSkip' })
    else if (run.phase === 'relic-reward') run = applyRunCommand(run, { type: 'SkipRelic' })
    else if (run.phase === 'reward') run = applyRunCommand(run, { type: 'SkipReward' })
    else if (run.phase === 'shop') run = applyRunCommand(run, { type: 'ShopLeave' })
    else if (run.phase === 'event') run = applyRunCommand(run, { type: 'EventChoice', index: getEventDef(run.eventId!).choices.length - 1 })
    else break
  }
  return run
}
function forceWin(run: RunState): RunState {
  let r = run
  for (let guard = 0; guard < 6 && r.phase === 'combat' && r.combat !== null; guard++) {
    const c = r.combat
    let surgical: GameState = { ...c, enemies: c.enemies.map((e) => ({ ...e, hp: 1, block: 0 })) }
    surgical = withIntent(withHand(surgical, ['green_sweep']), defendIntent(0))
    surgical = { ...surgical, player: { ...surgical.player, energy: 9 } }
    r = applyRunCommand({ ...r, combat: surgical }, { type: 'Combat', command: { type: 'PlayCard', cardUid: 't0_green_sweep' } })
  }
  return r
}

describe('取得時の一回効果 (gainRelic の一本道)', () => {
  it('古い金貨: 取った時+120G。呼び鈴: レリック3個と烙印1枚', () => {
    const run = createRun(11, 'set-confirm')
    expect(gainRelic(run, 'relic_old_coin').gold).toBe(run.gold + 120)
    const bell = gainRelic(run, 'relic_calling_bell')
    expect(bell.relics).toHaveLength(4)
    expect(bell.deck.filter((c) => c.def.id === 'status_brand')).toHaveLength(1)
  })

  it('空の鳥籠: relic-choose で2枚選んで取り除き、元のフェーズへ戻る', () => {
    let run = skipUntil(createRun(11, 'set-confirm'), 'reward')
    const before = run.deck.length
    run = { ...run, phase: 'relic-reward', relicOptions: ['relic_empty_cage'], rewardOptions: null }
    run = applyRunCommand(run, { type: 'PickRelic', index: 0 })
    expect(run.phase).toBe('relic-choose')
    expect(run.pendingRelicChoice?.mode).toBe('remove')
    expect(() => applyRunCommand(run, { type: 'RelicChooseCards', indices: [0, 1, 2] })).toThrow(/2枚まで/)
    run = applyRunCommand(run, { type: 'RelicChooseCards', indices: [0, 1] })
    expect(run.deck).toHaveLength(before - 2)
    expect(run.phase).toBe('reward') // 戦闘勝利の続き (カード報酬)
    expect(run.pendingRelicChoice).toBeUndefined()
  })

  it('星読みの盤: 選んだ3枚が同レア度の別札に変わり鍛えられている。古代の匣: 打撃・防御が全部変わる', () => {
    const run = createRun(11, 'set-confirm')
    let astro = gainRelic(run, 'relic_astrolabe')
    astro = { ...astro, phase: 'relic-choose', pendingRelicChoice: { ...astro.pendingRelicChoice!, resume: 'map' } }
    const ids = astro.deck.slice(0, 3).map((c) => c.def.id)
    astro = applyRunCommand(astro, { type: 'RelicChooseCards', indices: [0, 1, 2] })
    for (let i = 0; i < 3; i++) {
      expect(astro.deck[i].def.id).not.toBe(ids[i])
      expect(isUpgraded(astro.deck[i])).toBe(true)
    }
    const box = gainRelic(run, 'relic_ancient_box')
    expect(box.deck.some((c) => /^green_(strike|guard)$/.test(c.def.id))).toBe(false)
    expect(box.deck).toHaveLength(run.deck.length)
  })

  it('卵: デッキに加わる該当タイプの札が鍛えた状態になる (報酬・購入・イベントの共通口)', () => {
    const run = { ...createRun(11, 'set-confirm'), relics: ['relic_molten_egg'] }
    const added = addCardsToRunDeck(run, [{ uid: 'x', def: getCardDef('green_strike') }, { uid: 'y', def: getCardDef('green_growth_ring') }])
    expect(isUpgraded(added.deck[added.deck.length - 2])).toBe(true) // 物理
    expect(isUpgraded(added.deck[added.deck.length - 1])).toBe(false) // 呪文は対象外
  })
})

describe('報酬・マップ・経済', () => {
  it('祈りの車輪: 通常戦の報酬がもう1組出る。鳴り鉢: 見送るたび最大HP+2', () => {
    let run = skipUntil(createRun(11, 'set-confirm'), 'combat')
    run = { ...run, relics: ['relic_prayer_wheel', 'relic_singing_bowl'] }
    run = forceWin(run)
    expect(run.phase).toBe('reward')
    expect(run.rewardRoundsLeft).toBe(1)
    const maxHp = run.maxHp
    run = applyRunCommand(run, { type: 'SkipReward' })
    expect(run.phase).toBe('reward') // 2組目
    expect(run.maxHp).toBe(maxHp + 2)
    run = applyRunCommand(run, { type: 'PickReward', index: 0 })
    expect(run.phase).toBe('map')
  })

  it('翼の靴: 線の無い次の行のノードへ進める (残回数を消費)', () => {
    let run = skipUntil(createRun(11, 'set-confirm'), 'combat')
    run = gainRelic(run, 'relic_wing_boots')
    run = forceWin(run)
    run = skipUntil(run, 'map')
    const wings = wingChoices(run)
    if (wings.length === 0) return // この行は全ノードに線がある
    const target = wings[0]
    expect(nextChoices(run)).not.toContain(target)
    const moved = applyRunCommand(run, { type: 'ChooseNode', col: target })
    expect(moved.col).toBe(target)
    expect(relicStateOf(moved, 'wingBoots')).toBe(2)
    expect(() => applyRunCommand({ ...run, relicState: { wingBoots: 0 } }, { type: 'ChooseNode', col: target })).toThrow(/進めない/)
  })

  it('大口の貯金箱: 1行進むたび+12G。ショップで買うと止まる', () => {
    let run: RunState = { ...createRun(11, 'set-confirm'), relics: ['relic_maw_bank'] }
    const gold = run.gold
    run = applyRunCommand(run, { type: 'ChooseNode', col: nextChoices(run)[0] })
    expect(run.gold).toBe(gold + 12)
    let shop = skipUntil(run, 'shop', 'shop')
    if (shop.phase !== 'shop') return
    const buyable = shop.shop!.cards.findIndex((c) => c.price <= shop.gold)
    if (buyable < 0) shop = { ...shop, gold: 999 }
    shop = applyRunCommand(shop, { type: 'ShopBuyCard', index: Math.max(0, buyable) })
    expect(relicStateOf(shop, 'mawBroken')).toBe(1)
    shop = applyRunCommand(shop, { type: 'ShopLeave' })
    const g2 = shop.gold
    shop = applyRunCommand(shop, { type: 'ChooseNode', col: nextChoices(shop)[0] })
    expect(shop.gold).toBe(g2)
  })

  it('行商の食券: ショップに入るとHP+15', () => {
    let run: RunState = { ...createRun(11, 'set-confirm'), relics: ['relic_meal_ticket'] }
    for (let guard = 0; guard < 80 && run.phase !== 'shop'; guard++) {
      if (run.phase === 'map') {
        const before: RunState = { ...run, hp: 30 }
        const after = chooseToward(before, 'shop')
        if (after.phase === 'shop') {
          expect(after.hp).toBe(45)
          return
        }
        run = after
      } else run = skipUntil(run, 'map')
    }
  })
})

describe('焚き火の第3選択肢 (レリック限定) と工房', () => {
  it('発掘 (CampfireDig) は鶴嘴が無いので常に拒否 (2026-09-13 発掘の鶴嘴は撤去・機構だけ残置)。重石: 鍛錬3回まで=戦闘開始時の成長', () => {
    let run = skipUntil(createRun(11, 'set-confirm'), 'campfire', 'campfire')
    if (run.phase !== 'campfire') return
    expect(() => applyRunCommand(run, { type: 'CampfireDig' })).toThrow()
    expect(allRelicsHaveNoDig()).toBe(true)
    let g: RunState = { ...run, relics: ['relic_girya'] }
    expect(campfireOptions(g).trainLeft).toBe(3)
    g = applyRunCommand(g, { type: 'CampfireTrain' })
    expect(relicStateOf(g, 'train')).toBe(1)
    expect(g.phase).toBe('map')
    const battle = skipUntil(g, 'combat')
    expect(battle.combat!.player.growth).toBe(1)
  })

  it('安らぎの煙管: 焚き火で取り除ける。融合の鎚: 鍛えられない', () => {
    const run = skipUntil(createRun(11, 'set-confirm'), 'campfire', 'campfire')
    if (run.phase !== 'campfire') return
    expect(() => applyRunCommand(run, { type: 'CampfireRemove', index: 0 })).toThrow()
    const removed = applyRunCommand({ ...run, relics: ['relic_peace_pipe'] }, { type: 'CampfireRemove', index: 0 })
    expect(removed.deck).toHaveLength(run.deck.length - 1)
    const up = run.deck.findIndex((c) => !isUpgraded(c))
    expect(() => applyRunCommand({ ...run, relics: ['relic_fusion_hammer'] }, { type: 'CampfireUpgrade', index: up })).toThrow(/融合の鎚/)
  })

  it('職人の手袋: 工房で2回合成できる。鍛冶の火種: 合成結果が鍛えられている', () => {
    let run: RunState = skipUntil(createRun(11, 'set-confirm'), 'workshop', 'workshop')
    if (run.phase !== 'workshop') return
    run = { ...run, relics: ['relic_artisan_gloves', 'relic_forge_ember'], gold: 999 }
    const a = run.deck.findIndex((c) => c.def.id === 'green_strike')
    const b = run.deck.findIndex((c) => c.def.id === 'green_guard')
    run = applyRunCommand(run, { type: 'WorkshopFuse', indexA: a, indexB: b })
    expect(run.phase).toBe('workshop')
    expect(isUpgraded(run.deck[run.deck.length - 1])).toBe(true)
    run = applyRunCommand(run, { type: 'WorkshopFuse', indexA: 0, indexB: 1 })
    expect(run.phase).toBe('map')
  })
})

describe('時限レリック・烙印の受け皿・色ゲート・event 層', () => {
  it('旅の蝋燭: 5戦で消える', () => {
    let run = skipUntil(createRun(11, 'set-confirm'), 'combat')
    run = gainRelic(run, 'relic_travel_candle')
    expect(relicStateOf(run, 'exp_relic_travel_candle')).toBe(5)
    run = { ...run, relicState: { ...run.relicState, exp_relic_travel_candle: 1 } }
    run = forceWin(run)
    expect(run.relics).not.toContain('relic_travel_candle')
  })

  it('厄除けの札: 次の烙印2枚を無効。黒曜の護符: 烙印ごと最大HP+6', () => {
    const base = createRun(11, 'set-confirm')
    const ward = gainRelic(base, 'relic_ward_charm')
    const brand = { uid: 'b', def: getCardDef('status_brand') }
    const w1 = addCardsToRunDeck(ward, [brand, brand, brand])
    expect(w1.deck.filter((c) => c.def.id === 'status_brand')).toHaveLength(1)
    const periapt = addCardsToRunDeck({ ...base, relics: ['relic_obsidian_periapt'] }, [brand])
    expect(periapt.maxHp).toBe(base.maxHp + 6)
  })

  it('色ゲート: 緑限定レリックは青リーダーの候補列に入らない。event 層は ? のレリックにだけ出る', () => {
    expect(relicAllowedForColors(getRelicDef('relic_sulfur_shard'), ['blue'])).toBe(false)
    expect(createRun(3, 'set-confirm', 'leader_blue').relicQueue).not.toContain('relic_sulfur_shard')
    expect(createRun(3, 'set-confirm', 'leader_green').relicQueue).toContain('relic_sulfur_shard')
    const run = createRun(5, 'set-confirm')
    const eventIds = new Set(allRelics.filter((r) => r.rarity === 'event').map((r) => r.id))
    for (const src of ['chest', 'elite', 'boss', 'shop'] as const) {
      const [opts] = drawRelicOptions(run, src)
      expect(opts.some((id) => eventIds.has(id))).toBe(false)
    }
    const [ev] = drawRelicOptions(run, 'event', 1)
    expect(eventIds.has(ev[0])).toBe(true)
  })
})

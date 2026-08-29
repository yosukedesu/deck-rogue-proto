// engine/run.ts — ドラフト連戦モード (純ロジック。DOM/React依存禁止)
// マップラン (確定済みルール表「マップ」2026-08-28): StS式DAGを1ノードずつ進む。
// 戦闘勝利で4枚提示から1枚ピック (スキップ可) → マップで次のノードを選ぶ。
// 敵は行の帯で深度スケーリング (強化+HP倍率) され、だんだん強くなる (StS参考)。
// HPは持ち越し、強制焚き火行 (5/10/14) で回復。
// ラン専用RNGをシードから回すため、同じシード+同じコマンド列=同じラン (リプレイ可能)。

import { startCombatWithOptions } from './combat.ts'
import { ACT_COUNT, BOSS_ROW, generateMap } from './map.ts'
import { allEvents, getEventDef, WOUND_DEF } from './content.ts'
import type { MapNode, RunMap } from './map.ts'
import { fuseBlockReason, fuseCards } from './fusion.ts'
import {
  allCards,
  allRelics,
  buildDeck,
  buildRelicPermanent,
  getCardDef,
  getLeaderDef,
  getRelicDef,
} from './content.ts'
import { createRng, nextInt, shuffle } from './rng.ts'
import { applyCommand } from './state.ts'
import type { CardColor, CardDef, CardInstance, Command, DeclarativeEffect, GameState, RngState, ReactionMode } from './types.ts'

/** 報酬プールから除外する基本札 (スターターに入っている素のカード) */
const REWARD_EXCLUDED = new Set([
  'green_strike',
  'green_guard',
  'blue_strike',
  'blue_guard',
  'red_strike',
  'red_guard',
  'white_strike',
  'white_guard',
  'black_strike',
  'black_guard',
])
const CAMPFIRE_HEAL_RATIO = 0.3
// 2026-08-26 再設計: 回復は焚き火に到達すれば自動で入る。
// 「回復か強化か」の二択にすると、実測で焚き火到達時HPが常に20〜46%のため全員が回復しか選べず、
// 強化・除去が一度も使われなかった (供給側の機能が「既に余裕のある者」にしか届かない状態だった)。
/** 勝利ごとの自動回復は廃止 (2026-08-25 StS踏襲。回復は焚き火のみ=マラソン構造) */
const VICTORY_HEAL = 0
/** エリート補正: 強化+2・HP×1.35 (エリートはマップの選択ノード。2026-08-28 opt-inオファー廃止) */
const ELITE_STRENGTH = 2
const ELITE_HP_SCALE = 1.35
// レリック上限は撤廃 (2026-08-29)。上限5は1幕時代の校正で、3幕化により幕2で満杯
// →以後のボスレリック・ショップレリックが全部死んでいた。実効上限は在庫数 (9個)
/** ゴールド (確定済みルール表「ゴールド」「ショップ」。相場はStS比例で入れて校正) */
const STARTING_GOLD = 50
const GOLD_PER_BATTLE_MIN = 12
const GOLD_PER_BATTLE_MAX = 18
const GOLD_ELITE_BONUS_MIN = 30
const GOLD_ELITE_BONUS_MAX = 40
const SHOP_CARD_COUNT = 5
const SHOP_RELIC_PRICE = 150
/** 除去サービス: 回数無制限・使うたびラン通算で+25G (本家Purge式。2026-08-29) */
const SHOP_REMOVAL_BASE = 75
const SHOP_REMOVAL_STEP = 25
/** 強化サービス: 回数無制限・使うたびラン通算で+30G (2026-08-29 ユーザー指示) */
const SHOP_UPGRADE_BASE = 100
const SHOP_UPGRADE_STEP = 30

/** 現在の除去サービス価格 (ラン通算の逓増)。?? 0 はフィールド導入前のセーブ読み込み対策 (NaN汚染防止) */
export function shopRemovalPrice(run: RunState): number {
  return SHOP_REMOVAL_BASE + SHOP_REMOVAL_STEP * (run.removalCount ?? 0)
}
/** 現在の強化サービス価格 (ラン通算の逓増)。?? 0 はフィールド導入前のセーブ読み込み対策 (NaN汚染防止) */
export function shopUpgradePrice(run: RunState): number {
  return SHOP_UPGRADE_BASE + SHOP_UPGRADE_STEP * (run.upgradeCount ?? 0)
}

/**
 * 深度スケーリング: 敵の初期強化。
 * 敵データは15枚スターター基準の強さなので、ラン序盤は「若い個体」(マイナス強化) で登場し、
 * ボスでフルスペック近くになる (StSの「敵はだんだん強く」の再現)。
 */
export function depthStrength(row: number): number {
  // 若い個体補正は撤廃 (2026-08-25 人間基準化)。幕ボスのみ+1
  return row >= BOSS_ROW ? 1 : 0
}

/** 深度スケーリング: 敵HP倍率。確定済みルール表「敵の数値基準」の帯に対応する */
export function depthHpScale(row: number, act = 1): number {
  // 幕×幕内前後半の2段スケール (確定済みルール表「ランの敵強化」2026-08-29 3幕化)。
  // 各幕のプールは既にその幕の帯に校正済みなので、幕内の2段が「幕内でもだんだん強く」を再現する
  if (row >= BOSS_ROW) return 1.0 // 幕ボスは素のHP
  const late = row >= 8
  const table: readonly (readonly [number, number])[] = [
    [0.55, 0.65], // 1幕
    [0.8, 0.9], // 2幕
    [0.95, 1.05], // 3幕
  ]
  const [early, lateScale] = table[act - 1]
  return late ? lateScale : early
}

export type RunPhase = 'map' | 'combat' | 'relic-reward' | 'campfire' | 'workshop' | 'shop' | 'event' | 'reward' | 'won' | 'lost'

/** ショップの在庫 (ノード進入時にシードから決定) */
export interface ShopState {
  readonly cards: readonly { readonly id: string; readonly price: number }[]
  readonly relicId: string | null
  readonly relicPrice: number
}

export interface RunState {
  readonly seed: number
  readonly mode: ReactionMode
  /** リーダー (色アイデンティティ・初期デッキ・報酬プール・ピック候補数を決める) */
  readonly leaderId: string
  /** リーダーの色アイデンティティ (leaderId から導出してキャッシュ) */
  readonly colors: readonly CardColor[]
  /** ラン専用RNG (敵並び・報酬・戦闘シードの決定に使う) */
  readonly rng: RngState
  /** 現在のデッキ (ピックで増える) */
  readonly deck: readonly CardInstance[]
  /** 戦闘間で持ち越すHP */
  readonly hp: number
  readonly maxHp: number
  /** 現在の幕 (1〜3。確定済みルール表「マップ」3幕構成) */
  readonly act: number
  /** 現在の幕のマップ (幕開始時にシードから確定。全体可視) */
  readonly map: RunMap
  /** 現在いる行 (-1 = 開始前。行0のノードを選ぶ) */
  readonly row: number
  /** 現在いる列 */
  readonly col: number
  /** クリアした戦闘数 (統計・結果画面用) */
  readonly battlesWon: number
  /** 所持ゴールド (確定済みルール表「ゴールド」) */
  readonly gold: number
  /** ショップ除去サービスの通算使用回数 (逓増価格の基準) */
  readonly removalCount: number
  /** ショップ強化サービスの通算使用回数 (逓増価格の基準) */
  readonly upgradeCount: number
  /** ショップの在庫 (shop フェーズ中のみ非null) */
  readonly shop: ShopState | null
  readonly phase: RunPhase
  readonly combat: GameState | null
  /** 報酬フェーズの提示カード (cardId) */
  readonly rewardOptions: readonly string[] | null
  /** ピック履歴 (cardId。統計・結果画面用) */
  readonly picks: readonly string[]
  /** 所持レリック (relicId。上限なし=在庫数まで) */
  readonly relics: readonly string[]
  /** レリック候補列 (ラン開始時にシードから確定。取得済みを除いた先頭3つが提示される) */
  readonly relicQueue: readonly string[]
  /** relic-reward フェーズの提示レリック */
  readonly relicOptions: readonly string[] | null
  /** 現在の戦闘がエリート戦か (勝利時のレリック報酬判定) */
  readonly currentElite: boolean
  /** B型レリックの恒久ボーナス */
  readonly victoryHealBonus: number
  readonly rewardChoicesBonus: number
  readonly campfireRatio: number
  /** 戦闘勝利のゴールド加算 (商人の秤)。旧セーブに無いので使用側は ?? 0 ガード */
  readonly goldPerVictoryBonus: number
  /** 焚き火の「鍛える」の追加回数 (鍛冶の砥石)。旧セーブに無いので使用側は ?? 0 ガード */
  readonly campfireForgeBonus: number
  /** この焚き火で「鍛える」を使った回数 (焚き火進入時にリセット) */
  readonly campfireUpgradesUsed: number
}

export type RunCommand =
  | { readonly type: 'StartRun'; readonly seed: number }
  | { readonly type: 'Combat'; readonly command: Command } // 戦闘中コマンドの委譲
  | { readonly type: 'PickReward'; readonly index: number }
  | { readonly type: 'SkipReward' }
  | { readonly type: 'ChooseNode'; readonly col: number } // マップで次のノードを選ぶ
  | { readonly type: 'PickRelic'; readonly index: number }
  | { readonly type: 'SkipRelic' }
  // 焚き火 (確定済みルール表「焚き火」): 休んで回復するか、デッキから1枚を永久に取り除くか
  | { readonly type: 'CampfireRest' }
  | { readonly type: 'CampfireRemove'; readonly index: number }
  | { readonly type: 'CampfireUpgrade'; readonly index: number }
  // 工房 (確定済みルール表「カード合成（工房）」): 異なる2枚を選んで合成するか、見送る
  | { readonly type: 'WorkshopFuse'; readonly indexA: number; readonly indexB: number }
  | { readonly type: 'WorkshopSkip' }
  // ショップ (確定済みルール表「ショップ」)
  | { readonly type: 'ShopBuyCard'; readonly index: number }
  | { readonly type: 'ShopBuyRelic' }
  | { readonly type: 'ShopRemove'; readonly index: number }
  | { readonly type: 'ShopUpgrade'; readonly index: number }
  | { readonly type: 'ShopLeave' }
  // ?マス (確定済みルール表「?マス（イベント）」)。removeCard/upgradeCard の選択肢は cardIndex で対象指定
  | { readonly type: 'EventChoice'; readonly index: number; readonly cardIndex?: number }

/** 現在いるノード (row=-1 の開始前は null) */
export function currentNode(run: RunState): MapNode | null {
  return run.row >= 0 ? run.map[run.row][run.col] : null
}

/** マップで次に進めるノードの列リスト (開始前は行0の全ノード) */
export function nextChoices(run: RunState): readonly number[] {
  if (run.row < 0) return run.map[0].map((_, c) => c)
  if (run.row >= BOSS_ROW) return []
  return currentNode(run)?.next ?? []
}

/** 現在ノードの戦闘を開始する (戦闘シードはラン RNG から決定的に生成)。elite でエリート補正 */
function launchCombat(run: RunState, elite: boolean): RunState {
  const node = currentNode(run)
  if (node === null || node.encounterId === null) throw new Error('戦闘ノードではない')
  const [combatSeed, rng] = nextInt(run.rng, 0, 2 ** 31 - 1)
  const combat = startCombatWithOptions(combatSeed, run.mode, node.encounterId, {
    deck: run.deck,
    leaderId: run.leaderId,
    playerHp: run.hp,
    playerMaxHp: run.maxHp,
    // ボスの幕スケール (確定済みルール表「マップ」2026-08-29): HP×1.0/1.6/2.4・強化+1/+1/+2。
    // 幕2以降のボスが1幕時代の校正のままで消化試合化していた実測への対処
    enemyHpScale:
      depthHpScale(run.row, run.act) *
      (elite ? ELITE_HP_SCALE : 1) *
      (node.type === 'boss' ? [1, 1.6, 2.4][run.act - 1] : 1),
    enemyStrength:
      (node.type === 'boss' ? [1, 1, 2][run.act - 1] : 0) + (elite ? ELITE_STRENGTH : 0),
    relicPermanents: run.relics
      .map(getRelicDef)
      .filter((r) => (r.effects?.length ?? 0) > 0)
      .map(buildRelicPermanent),
    // C型レリック: 所持レリックの combatRule を集計して戦闘ルールに渡す
    setDamageReduction: run.relics
      .map(getRelicDef)
      .reduce((sum, r) => sum + (r.combatRule?.setDamageReduction ?? 0), 0),
    revealIntents: run.relics.some((id) => getRelicDef(id).combatRule?.revealIntents === true),
  })
  return { ...run, rng, combat, phase: 'combat', rewardOptions: null, currentElite: elite }
}

/** 選んだノードに入る: 戦闘ノードなら戦闘開始、焚き火なら回復、工房ならそのままフェーズへ */
function enterNode(run: RunState): RunState {
  const node = currentNode(run)
  if (node === null) throw new Error('ノードにいない')
  switch (node.type) {
    case 'battle':
    case 'boss':
      return launchCombat(run, false)
    case 'elite':
      return launchCombat(run, true)
    case 'campfire': {
      // 回復は自動 (2026-08-26)。焚き火の選択は「鍛える / 取り除く / 何もしない」
      const hp = Math.min(run.maxHp, run.hp + Math.floor(run.maxHp * run.campfireRatio))
      return { ...run, hp, phase: 'campfire', combat: null, rewardOptions: null, campfireUpgradesUsed: 0 }
    }
    case 'workshop':
      return { ...run, phase: 'workshop', combat: null, rewardOptions: null }
    case 'shop':
      return openShop(run)
    case 'event':
      return { ...run, phase: 'event', combat: null, rewardOptions: null }
  }
}

/** ショップの在庫をシードから決定して開店する */
function openShop(run: RunState): RunState {
  const leader = getLeaderDef(run.leaderId)
  const canRamp = run.colors.includes('green')
  const costCap = leader.energyMax + (canRamp ? 2 : 0)
  const pool = allCards.filter(
    (c) => run.colors.includes(c.color) && !REWARD_EXCLUDED.has(c.id) && c.cost <= costCap,
  )
  let rng = run.rng
  const cards: { id: string; price: number }[] = []
  const remaining = [...pool]
  while (cards.length < SHOP_CARD_COUNT && remaining.length > 0) {
    const [idx, r1] = nextInt(rng, 0, remaining.length - 1)
    rng = r1
    const def = remaining[idx]
    remaining.splice(idx, 1)
    // 価格 = 40 + コスト×10 + ロール0〜10 (確定済みルール表「ショップ」)
    const [roll, r2] = nextInt(rng, 0, 10)
    rng = r2
    cards.push({ id: def.id, price: 40 + def.cost * 10 + roll })
  }
  const relicId = run.relicQueue.find((id) => !run.relics.includes(id)) ?? null
  const shop: ShopState = {
    cards,
    relicId,
    relicPrice: SHOP_RELIC_PRICE,
  }
  return { ...run, rng, shop, phase: 'shop', combat: null, rewardOptions: null }
}

/** イベント効果の適用 (宣言的な EventChoiceDef を RunState に反映する) */
function applyEventChoice(run: RunState, choiceIndex: number, cardIndex?: number): RunState {
  const node = currentNode(run)
  if (node === null || node.eventId === null) throw new Error('イベントノードではない')
  const def = getEventDef(node.eventId)
  const choice = def.choices[choiceIndex]
  if (choice === undefined) throw new Error(`不正な選択肢: ${choiceIndex}`)
  if (choice.requireGold !== undefined && run.gold < choice.requireGold) {
    throw new Error(`ゴールドが足りない (必要${choice.requireGold}G)`)
  }
  let next: RunState = { ...run }
  let rng = run.rng
  const applyOutcome = (o: { gold?: number; hp?: number; wounds?: number }): void => {
    if (o.gold) next = { ...next, gold: Math.max(0, next.gold + o.gold) }
    if (o.hp) next = { ...next, hp: Math.min(next.maxHp, next.hp + o.hp) }
    if (o.wounds) {
      const wounds: CardInstance[] = Array.from({ length: o.wounds }, (_, i) => ({
        uid: `wound_r${run.row}_${i}`,
        def: WOUND_DEF,
      }))
      next = { ...next, deck: [...next.deck, ...wounds] }
    }
  }
  applyOutcome(choice)
  if (choice.maxHp) {
    next = { ...next, maxHp: next.maxHp + choice.maxHp, hp: next.hp + choice.maxHp }
  }
  if (choice.addRandomCards) {
    const leader = getLeaderDef(run.leaderId)
    const canRamp = run.colors.includes('green')
    const costCap = leader.energyMax + (canRamp ? 2 : 0)
    const pool = allCards.filter(
      (c) => run.colors.includes(c.color) && !REWARD_EXCLUDED.has(c.id) && c.cost <= costCap,
    )
    for (let i = 0; i < choice.addRandomCards && pool.length > 0; i++) {
      const [idx, r1] = nextInt(rng, 0, pool.length - 1)
      rng = r1
      next = {
        ...next,
        deck: [...next.deck, { uid: `event_r${run.row}_${i}_${pool[idx].id}`, def: pool[idx] }],
      }
    }
  }
  if (choice.relic) {
    const relicId = run.relicQueue.find((id) => !next.relics.includes(id))
    if (relicId !== undefined) {
      next = applyRelicBonus({ ...next, relics: [...next.relics, relicId] }, relicId)
    }
  }
  if (choice.removeCard) {
    const card = next.deck[cardIndex ?? -1]
    if (card === undefined) throw new Error('対象カードを cardIndex で指定する')
    if (next.deck.length <= 5) throw new Error('これ以上デッキを減らせない')
    next = { ...next, deck: next.deck.filter((_, i) => i !== cardIndex) }
  }
  if (choice.upgradeCard) {
    const card = next.deck[cardIndex ?? -1]
    if (card === undefined) throw new Error('対象カードを cardIndex で指定する')
    if (isUpgraded(card)) throw new Error('すでに鍛えられている')
    if (upgradeTier(card.def) === 'none') throw new Error(`${card.def.name} は鍛えられない`)
    next = { ...next, deck: next.deck.map((c, i) => (i === cardIndex ? upgradeCard(c) : c)) }
  }
  if (choice.gamble) {
    // ロールはラン RNG = 決定的 (リプレイ再現)
    const [roll, r1] = nextInt(rng, 0, 999)
    rng = r1
    applyOutcome(roll < choice.gamble.chance * 1000 ? choice.gamble.win : choice.gamble.lose)
  }
  if (next.hp <= 0) return { ...next, rng, hp: 0, phase: 'lost' }
  return { ...next, rng, phase: 'map' }
}

export function createRun(
  seed: number,
  mode: ReactionMode,
  leaderId = 'leader_green',
  deckId?: string,
): RunState {
  const leader = getLeaderDef(leaderId)
  // 種の選択制 (確定済みルール表「ラン初期デッキ」): リーダーが許可する初期デッキのみ受け付ける
  const deckChoices = leader.runDeckChoices ?? [leader.runDeckId]
  const chosenDeck = deckId ?? leader.runDeckId
  if (!deckChoices.includes(chosenDeck)) {
    throw new Error(`このリーダーでは選べない初期デッキ: ${chosenDeck}`)
  }
  const rng0 = createRng(seed)
  // マップもレリック候補列もシードから確定 (リプレイ再現性)
  const [map, rngAfterMap] = generateMap(rng0, allEvents.map((e) => e.id), 1, leader.colors.includes('green'))
  const [relicQueue, rngAfterRelics] = shuffle(
    rngAfterMap,
    allRelics.map((r) => r.id),
  )
  return {
    seed,
    mode,
    leaderId,
    colors: leader.colors,
    rng: rngAfterRelics,
    deck: buildDeck(chosenDeck),
    hp: leader.maxHp,
    maxHp: leader.maxHp,
    act: 1,
    map,
    row: -1,
    col: 0,
    battlesWon: 0,
    gold: STARTING_GOLD,
    removalCount: 0,
    upgradeCount: 0,
    shop: null,
    phase: 'map',
    combat: null,
    rewardOptions: null,
    picks: [],
    relics: [],
    relicQueue,
    relicOptions: null,
    currentElite: false,
    victoryHealBonus: 0,
    rewardChoicesBonus: 0,
    campfireRatio: CAMPFIRE_HEAL_RATIO,
    goldPerVictoryBonus: 0,
    campfireForgeBonus: 0,
    campfireUpgradesUsed: 0,
  }
}

/**
 * 効果名 → アーキタイプの軸。確定済みルール表「軸の重み付け」。
 * 効果に軸が現れない札 (多段ヒットの成長ペイオフ・貫通のトランプル札など) は
 * CardDef.axis で明示する (JSONで宣言。ここは自動導出ぶんだけ)。
 */
const EFFECT_AXIS: Record<string, string> = {
  addGrowth: 'growth', doubleGrowth: 'growth', dischargeGrowth: 'growth',
  gainEnergyMax: 'ramp', dealDamagePerEnergyMax: 'ramp', gainBlockPerEnergyMax: 'ramp',
  addMomentum: 'trample', dealDamagePerMomentum: 'trample', doubleMomentum: 'trample',
  applyBurn: 'burn', dischargeBurn: 'burn',
  addAether: 'aether', dischargeAether: 'aether', dischargeAetherDraw: 'aether',
  gainIceBlock: 'ice', dealDamagePerIceBlock: 'ice', gainIceBlockPerCardPlayed: 'ice',
  negate: 'permission', negateConvertIce: 'permission',
  summonPermanent: 'retinue', dealDamagePerPermanent: 'retinue', gainBlockPerPermanent: 'retinue',
  exhaustFromDeck: 'graveyard', dealDamagePerExhaust: 'graveyard', retrieveFromExhaust: 'graveyard',
  playFromExhaust: 'graveyard', gainBlockPerExhaust: 'graveyard', dealDamageDrainPerExhaust: 'graveyard',
  loseHp: 'selfharm', dealDamagePerSelfHpLost: 'selfharm',
  dealDamagePerCardPlayed: 'storm', drawCardsPerCardPlayed: 'storm',
  impulseDraw: 'impulse',
  weakenEnemy: 'oppress', dealDamagePerNegStrength: 'oppress',
  gainHp: 'heal', dealDamageDrain: 'heal',
  dealDamagePerDamageTaken: 'wrath',
  dealDamagePerBlock: 'fortress',
  shatterBlock: 'shatter', shatterBlockConvert: 'shatter',
  dealDamageRandom: 'chaos',
  dealDamageExecute: 'execute', exposeEnemy: 'execute',
  confuse: 'confuse',
}

/** 誘発トリガー → 軸。置物の「接着剤」札はここでほぼ自動的に分類される */
const TRIGGER_AXIS: Record<string, string> = {
  onPermanentEntered: 'retinue',
  onCardExhausted: 'graveyard',
  onCostExhausted: 'graveyard',
  onHealed: 'heal',
  onHpLost: 'selfharm',
  onAetherGained: 'aether',
  onImpulsePlayed: 'impulse',
  onSpellPlayed: 'storm',
  onBlockGained: 'fortress',
  onActionNegated: 'permission',
}

/** この札が属する軸 (効果名・トリガー・フィールドからの自動導出 + JSONの明示宣言) */
export function axesOf(def: CardDef): readonly string[] {
  const all = [...def.effects, ...(def.modes ?? []).flatMap((m) => m.effects)]
  const set = new Set<string>(def.axis ?? [])
  for (const e of all) {
    const byEffect = EFFECT_AXIS[e.effect]
    if (byEffect) set.add(byEffect)
    const byTrigger = TRIGGER_AXIS[e.trigger]
    if (byTrigger) set.add(byTrigger)
    if (e.exhaustThreshold !== undefined) set.add('graveyard') // 忘却の刻の参照札
    if (e.pierce === true) set.add('trample') // 貫通はトランプルの核
  }
  if (def.exhaustCost !== undefined) set.add('graveyard') // 消滅コストは墓地の燃料
  if (def.retainer === true) set.add('retinue')
  return [...set]
}

/** 報酬を抽選 (リーダーの色アイデンティティのカードのみ・基本札除外・重複なし)。候補数はリーダー個性+収集家の鞄 */
function rollRewards(run: RunState): RunState {
  const leader = getLeaderDef(run.leaderId)
  // 報酬プールはリーダーのエナジー上限を考慮する (2026-08-25 プレイテストで発見:
  // 白は上限3固定なのに4Eの大行進が提示され、ラン中ずっと死に札だった)。
  // 緑はランプで上限を伸ばせるので +2 まで許容する
  const canRamp = run.colors.includes('green')
  const costCap = leader.energyMax + (canRamp ? 2 : 0)
  const pool = allCards
    .filter(
      (c) =>
        run.colors.includes(c.color) && !REWARD_EXCLUDED.has(c.id) && c.cost <= costCap,
    )
  // レアリティ抽選 (確定済みルール表「レアリティ」2026-08-29): スロットごとに
  // コモン60% / アンコモン37% / レア3% の本家比率でレアリティを決め、その帯から一様に引く。
  // 軸の重み付けは全廃 — 報酬はデッキを一切見ない (本家準拠。ドラフトの発見性を守る)
  const remaining = [...pool]
  const picked: string[] = []
  let rng = run.rng
  const want = leader.rewardChoices + run.rewardChoicesBonus
  const rarityOf = (c: CardDef) => c.rarity ?? 'common'
  while (picked.length < want && remaining.length > 0) {
    const [roll, r1] = nextInt(rng, 0, 99)
    rng = r1
    const wanted: ('rare' | 'uncommon' | 'common')[] =
      roll < 3 ? ['rare', 'uncommon', 'common'] : roll < 40 ? ['uncommon', 'common'] : ['common']
    // 希望レアリティの札が尽きていたら下の帯へフォールバック。それも無ければプール全体
    let candidates: CardDef[] = []
    for (const r of wanted) {
      candidates = remaining.filter((c) => rarityOf(c) === r)
      if (candidates.length > 0) break
    }
    if (candidates.length === 0) candidates = remaining
    const [idx, r2] = nextInt(rng, 0, candidates.length - 1)
    rng = r2
    const chosen = candidates[idx]
    picked.push(chosen.id)
    remaining.splice(remaining.indexOf(chosen), 1)
  }
  return { ...run, rng, rewardOptions: picked, phase: 'reward' }
}

/** 戦闘勝利後の処理: HP持ち越し → (エリートならレリック報酬 →) カード報酬 or ラン勝利 */
function afterVictory(run: RunState, combat: GameState): RunState {
  const isBoss = currentNode(run)?.type === 'boss'
  // 3幕目のボス撃破 = ラン走破
  if (isBoss && run.act >= ACT_COUNT) {
    return { ...run, combat, battlesWon: run.battlesWon + 1, phase: 'won' }
  }
  // 自動回復は狩人の恵み (victoryHealBonus) のみ。幕ボス撃破は全回復 (確定済みルール表「マップ」)
  const hp = isBoss
    ? run.maxHp
    : Math.min(run.maxHp, combat.player.hp + VICTORY_HEAL + run.victoryHealBonus)
  // ゴールド獲得 (通常12〜18G・エリート+30〜40G・幕ボス+40〜50G。確定済みルール表「ゴールド」)
  let rng = run.rng
  const [base, r1] = nextInt(rng, GOLD_PER_BATTLE_MIN, GOLD_PER_BATTLE_MAX)
  rng = r1
  let gained = base
  if (run.currentElite) {
    const [bonus, r2] = nextInt(rng, GOLD_ELITE_BONUS_MIN, GOLD_ELITE_BONUS_MAX)
    rng = r2
    gained += bonus
  }
  if (isBoss) {
    const [bonus, r3] = nextInt(rng, 40, 50)
    rng = r3
    gained += bonus
  }
  // 商人の秤 (B型レリック): 戦闘勝利のゴールド加算
  gained += run.goldPerVictoryBonus ?? 0
  const next: RunState = {
    ...run,
    rng,
    combat,
    hp,
    battlesWon: run.battlesWon + 1,
    gold: run.gold + gained,
  }
  // 幕ボス・エリート戦の勝利: レリック3択 (幕ボスは本家のボスレリック相当)
  if (run.currentElite || isBoss) {
    const remaining = run.relicQueue.filter((id) => !run.relics.includes(id))
    if (remaining.length > 0) {
      return { ...next, phase: 'relic-reward', relicOptions: remaining.slice(0, 3) }
    }
  }
  return rollRewards(next)
}

/** 幕ボスのカード報酬を受け取った後、次の幕へ進む (新しいマップを生成して行0の選択から) */
function advanceActIfBossCleared(run: RunState): RunState {
  if (currentNode(run)?.type !== 'boss' || run.act >= ACT_COUNT) {
    return { ...run, phase: 'map' }
  }
  const nextAct = run.act + 1
  const [map, rng] = generateMap(run.rng, allEvents.map((e) => e.id), nextAct, run.colors.includes('green'))
  return { ...run, rng, act: nextAct, map, row: -1, col: 0, combat: null, phase: 'map' }
}

/**
 * 強化の対象になる「量」の効果 (確定済みルール表「焚き火」)。
 * 単位効果 (ドロー・成長など) はティア③で+1、per-X はティア②のコスト-1で強化される。
 */
const UPGRADABLE_EFFECTS = new Set([
  'dealDamage',
  'gainBlock',
  'gainIceBlock',
  'applyBurn',
  'counter',
  'gainHp',
  'dealDamageDrain',
  'dealDamageRandom',
  'dealDamageExecute',
])

/**
 * ティア③で+1する「単位」の効果。
 * dealDamagePerCardPlayed / exhaustFromDeck は 0E でコストを削れない参照札 (余波・墓暴き) の
 * 受け皿として追加 (2026-08-28 全カード解放)。ドローしない×Nは有限なので倍率+1でも安全。
 * drawCardsPerCardPlayed 等のドロー×Nは入れない (倍率+1=×2ドローは無限ループの危険地帯) — ④の例外表で受ける
 */
const UNIT_EFFECTS = new Set([
  'drawCards',
  'impulseDraw',
  'addGrowth',
  'addMomentum',
  'addAether',
  'gainEnergy',
  'discountNext',
  'dealDamagePerCardPlayed',
  'exhaustFromDeck',
])

/**
 * ティア④: 同軸おまけの手書き例外表 (2026-08-28 全カード解放)。
 * ②コスト-1が規約違反 (0E+補充=消滅必須) になり、③の対象効果も持たない補充参照札の受け皿。
 * おまけは札自身の軸から外れない (カラーパイ・報酬抽選の軸判定を動かさない)。先頭に挿入する
 * (霊気の奔流は「霊気+2 → 放出」の順で解決されることに意味がある)
 */
const BONUS_UPGRADES: Record<string, readonly DeclarativeEffect[]> = {
  // 連鎖する思考+: 自分自身も詠唱数に数えるフレーバーの +1ドロー
  blue_chain_thought: [{ trigger: 'onPlay', effect: 'drawCards', amount: 1 }],
  // 霊気の奔流+: 放出の前に霊気+2 (実質ドロー+2)
  blue_aether_torrent: [{ trigger: 'onPlay', effect: 'addAether', amount: 2 }],
}

/** 手札を補充する効果 (0E+補充=消滅必須、の規約判定。cardrules.test.ts と同じ定義) */
const REFILL_FOR_UPGRADE = new Set([
  'drawCards',
  'drawCardsPerCardPlayed',
  'dischargeAetherDraw',
  'impulseDraw',
  'retrieveFromExhaust',
  'playFromExhaust',
])

function allEffectsOf(def: CardDef): readonly DeclarativeEffect[] {
  return [...def.effects, ...(def.modes ?? []).flatMap((m) => m.effects)]
}

/** コスト-1すると無限ループ規約 (0E+補充=消滅必須 / 正味エナジー) に違反するか */
function costCutViolates(def: CardDef): boolean {
  if (def.exhaust === true) return false
  const newCost = def.cost - 1
  const eff = allEffectsOf(def)
  const refill = eff.some((e) => REFILL_FOR_UPGRADE.has(e.effect))
  if (!refill) return false
  const net = eff
    .filter((e) => e.effect === 'gainEnergy' || e.effect === 'discountNext')
    .reduce((a, e) => a + (e.amount ?? 0), 0)
  return net - newCost >= 0
}

/**
 * どのティアで強化されるか。'none' = 強化不可。
 * 2026-08-28 全カード解放: gainEnergyMax の一律ブロックを撤廃 (上限ランプはコスト-1で強化。
 * gainEnergyMax は UPGRADABLE / UNIT のどちらにも無いので量は絶対に増えない = 複利安全弁は
 * 「量を強化しない」形で維持)。現行データでは全カードがいずれかのティアに落ちる
 * (テストで機械固定)。'none' は将来のデータ追加への防衛用に残す
 */
export function upgradeTier(def: CardDef): 'amount' | 'cost' | 'unit' | 'bonus' | 'none' {
  const eff = allEffectsOf(def)
  // 上限ランプはコスト-1が正史 (確定済みルール表「焚き火」)。2026-08-29 品質パスで
  // ランプ札に副次効果 (ブロック等) が付いたため、amount ティアに吸われて
  // 「0E化の当たり枠」が「副次+50%のハズレ枠」に化けるのを防ぐ
  if (eff.some((e) => e.effect === 'gainEnergyMax') && def.cost >= 1 && !costCutViolates(def)) {
    return 'cost'
  }
  if (eff.some((e) => UPGRADABLE_EFFECTS.has(e.effect) && e.amount !== undefined)) return 'amount'
  if (def.cost >= 1 && !costCutViolates(def)) return 'cost'
  if (eff.some((e) => UNIT_EFFECTS.has(e.effect) && e.amount !== undefined)) return 'unit'
  if (BONUS_UPGRADES[def.id] !== undefined) return 'bonus'
  return 'none'
}

/** すでに鍛えられているか (同じカードは1回だけ) */
export function isUpgraded(card: CardInstance): boolean {
  return card.def.name.endsWith('+')
}

/** この札は鍛えられるか (UI のボタン活性判定) */
export function canUpgradeCard(card: CardInstance): boolean {
  return !isUpgraded(card) && upgradeTier(card.def) !== 'none'
}

/**
 * カードを鍛える (確定済みルール表「焚き火」の3段仕様)。
 * ①量+50%切り上げ → ②コスト-1 → ③単位+1。名前に「+」が付く。
 * 自傷 (loseHp) などの対価は据え置き = 非対称強化を仕様として認める (StSのHemokinesis+と同じ)。
 * def を作り直すので engine 側に強化用の分岐は要らない (id は据え置き = 軸判定も不変)。
 */
export function upgradeCard(card: CardInstance): CardInstance {
  const tier = upgradeTier(card.def)
  const boostAmount = (e: DeclarativeEffect): DeclarativeEffect => {
    if (!UPGRADABLE_EFFECTS.has(e.effect) || e.amount === undefined) return e
    return {
      ...e,
      amount: Math.ceil(e.amount * 1.5),
      ...(e.amountMax !== undefined ? { amountMax: Math.ceil(e.amountMax * 1.5) } : {}),
    }
  }
  const boostUnit = (e: DeclarativeEffect): DeclarativeEffect => {
    if (!UNIT_EFFECTS.has(e.effect) || e.amount === undefined) return e
    return { ...e, amount: e.amount + 1 }
  }
  const mapEffects = (fn: (e: DeclarativeEffect) => DeclarativeEffect) => ({
    effects: card.def.effects.map(fn),
    ...(card.def.modes !== undefined
      ? { modes: card.def.modes.map((m) => ({ ...m, effects: m.effects.map(fn) })) }
      : {}),
  })
  const patch =
    tier === 'amount'
      ? mapEffects(boostAmount)
      : tier === 'cost'
        ? { cost: card.def.cost - 1 }
        : tier === 'unit'
          ? mapEffects(boostUnit)
          : tier === 'bonus'
            ? { effects: [...(BONUS_UPGRADES[card.def.id] ?? []), ...card.def.effects] }
            : {}
  return { ...card, def: { ...card.def, name: `${card.def.name}+`, ...patch } }
}

/** B型レリックの取得時効果を適用する */
function applyRelicBonus(run: RunState, relicId: string): RunState {
  const def = getRelicDef(relicId)
  const b = def.bonus
  if (!b) return run
  return {
    ...run,
    maxHp: run.maxHp + (b.maxHp ?? 0),
    hp: Math.min(run.maxHp + (b.maxHp ?? 0), run.hp + (b.maxHp ?? 0)),
    victoryHealBonus: run.victoryHealBonus + (b.victoryHeal ?? 0),
    rewardChoicesBonus: run.rewardChoicesBonus + (b.rewardChoices ?? 0),
    campfireRatio: b.campfireRatio ?? run.campfireRatio,
    // ?? 0 二段: 旧セーブは RunState 側のフィールド自体が無い (NaN汚染防止。shop-event.test.ts の前例)
    goldPerVictoryBonus: (run.goldPerVictoryBonus ?? 0) + (b.goldPerVictory ?? 0),
    campfireForgeBonus: (run.campfireForgeBonus ?? 0) + (b.campfireForge ?? 0),
  }
}

export function applyRunCommand(run: RunState, command: RunCommand): RunState {
  switch (command.type) {
    case 'StartRun':
      return createRun(command.seed, run.mode, run.leaderId)
    case 'Combat': {
      if (run.phase !== 'combat' || run.combat === null) throw new Error('戦闘中ではない')
      if (command.command.type === 'StartCombat') throw new Error('ラン中の戦闘開始はランが管理する')
      const combat = applyCommand(run.combat, command.command)
      if (combat.phase === 'lost') return { ...run, combat, hp: 0, phase: 'lost' }
      if (combat.phase === 'won') return afterVictory(run, combat)
      return { ...run, combat }
    }
    case 'PickReward': {
      if (run.phase !== 'reward' || run.rewardOptions === null) throw new Error('報酬フェーズではない')
      const cardId = run.rewardOptions[command.index]
      if (cardId === undefined) throw new Error(`不正な報酬指定: ${command.index}`)
      // uid は行番号で一意化 (1行につき1ノードしか訪れないため衝突しない)
      const card: CardInstance = { uid: `pick_r${run.row}_${cardId}`, def: getCardDef(cardId) }
      return advanceActIfBossCleared({
        ...run,
        deck: [...run.deck, card],
        picks: [...run.picks, cardId],
        rewardOptions: null,
      })
    }
    case 'SkipReward': {
      if (run.phase !== 'reward') throw new Error('報酬フェーズではない')
      return advanceActIfBossCleared({ ...run, rewardOptions: null })
    }
    case 'ChooseNode': {
      if (run.phase !== 'map') throw new Error('マップフェーズではない')
      const candidates = nextChoices(run)
      if (!candidates.includes(command.col)) throw new Error(`進めないノード: ${command.col}`)
      return enterNode({ ...run, row: run.row + 1, col: command.col })
    }
    case 'PickRelic': {
      if (run.phase !== 'relic-reward' || run.relicOptions === null) {
        throw new Error('レリック報酬フェーズではない')
      }
      const relicId = run.relicOptions[command.index]
      if (relicId === undefined) throw new Error(`不正なレリック指定: ${command.index}`)
      let next: RunState = { ...run, relics: [...run.relics, relicId], relicOptions: null }
      next = applyRelicBonus(next, relicId)
      return rollRewards(next)
    }
    case 'SkipRelic': {
      if (run.phase !== 'relic-reward') throw new Error('レリック報酬フェーズではない')
      return rollRewards({ ...run, relicOptions: null })
    }
    case 'CampfireRest': {
      // 「何もしない」= 回復だけ受け取って次へ (回復はノード進入時に適用済み)
      if (run.phase !== 'campfire') throw new Error('焚き火フェーズではない')
      return { ...run, phase: 'map' }
    }
    case 'CampfireUpgrade': {
      if (run.phase !== 'campfire') throw new Error('焚き火フェーズではない')
      const card = run.deck[command.index]
      if (card === undefined) throw new Error(`不正な強化指定: ${command.index}`)
      if (isUpgraded(card)) throw new Error('すでに鍛えられている')
      // 2026-08-28 修正: 強化不可札 (上限ランプ) を受理して「+」だけ付ける事故の再発防止。
      // 焚き火の選択権 (4回しかない希少資源) を無言で浪費させない
      if (upgradeTier(card.def) === 'none') {
        throw new Error(`${card.def.name} は鍛えられない (エナジー上限を上げる札は強化対象外)`)
      }
      // 鍛冶の砥石 (B型レリック): 追加回数のぶん焚き火に留まり、もう1枚鍛えられる
      const used = (run.campfireUpgradesUsed ?? 0) + 1
      const allowed = 1 + (run.campfireForgeBonus ?? 0)
      return {
        ...run,
        deck: run.deck.map((c, i) => (i === command.index ? upgradeCard(c) : c)),
        campfireUpgradesUsed: used,
        phase: used < allowed ? 'campfire' : 'map',
      }
    }
    case 'WorkshopFuse': {
      if (run.phase !== 'workshop') throw new Error('工房フェーズではない')
      const a = run.deck[command.indexA]
      const b = run.deck[command.indexB]
      if (a === undefined || b === undefined) throw new Error('不正な合成指定')
      const reason = fuseBlockReason(a, b)
      if (reason !== null) throw new Error(`合成できない: ${reason}`)
      const fusedDef = fuseCards(a, b)
      const fused: CardInstance = { uid: `fused_r${run.row}_${fusedDef.id}`, def: fusedDef }
      // 素材2枚はデッキから消え、合成札1枚が入る = 圧縮と強化が同時に起きる
      const deck = run.deck.filter((_, i) => i !== command.indexA && i !== command.indexB)
      return { ...run, deck: [...deck, fused], phase: 'map' }
    }
    case 'WorkshopSkip': {
      if (run.phase !== 'workshop') throw new Error('工房フェーズではない')
      return { ...run, phase: 'map' }
    }
    case 'CampfireRemove': {
      if (run.phase !== 'campfire') throw new Error('焚き火フェーズではない')
      // 砥石で焚き火に留まっていても「鍛える/取り除く/何もしない から1つ選ぶ」の原則は崩さない
      if ((run.campfireUpgradesUsed ?? 0) > 0) throw new Error('この焚き火ではすでに鍛えている (選べるのは1種類)')
      const card = run.deck[command.index]
      if (card === undefined) throw new Error(`不正な除去指定: ${command.index}`)
      // デッキが痩せすぎないよう最低5枚は残す
      if (run.deck.length <= 5) throw new Error('これ以上デッキを減らせない')
      return { ...run, deck: run.deck.filter((_, i) => i !== command.index), phase: 'map' }
    }
    case 'ShopBuyCard': {
      if (run.phase !== 'shop' || run.shop === null) throw new Error('ショップではない')
      const item = run.shop.cards[command.index]
      if (item === undefined) throw new Error(`不正な商品指定: ${command.index}`)
      if (run.gold < item.price) throw new Error(`ゴールドが足りない (${item.price}G)`)
      const card: CardInstance = { uid: `buy_r${run.row}_${item.id}`, def: getCardDef(item.id) }
      return {
        ...run,
        gold: run.gold - item.price,
        deck: [...run.deck, card],
        picks: [...run.picks, item.id],
        shop: { ...run.shop, cards: run.shop.cards.filter((_, i) => i !== command.index) },
      }
    }
    case 'ShopBuyRelic': {
      if (run.phase !== 'shop' || run.shop === null) throw new Error('ショップではない')
      if (run.shop.relicId === null) throw new Error('レリックの在庫がない')
      if (run.gold < run.shop.relicPrice) throw new Error(`ゴールドが足りない (${run.shop.relicPrice}G)`)
      const relicId = run.shop.relicId
      let next: RunState = {
        ...run,
        gold: run.gold - run.shop.relicPrice,
        relics: [...run.relics, relicId],
        shop: { ...run.shop, relicId: null },
      }
      next = applyRelicBonus(next, relicId)
      return next
    }
    case 'ShopRemove': {
      if (run.phase !== 'shop' || run.shop === null) throw new Error('ショップではない')
      const price = shopRemovalPrice(run)
      if (run.gold < price) throw new Error(`ゴールドが足りない (${price}G)`)
      const card = run.deck[command.index]
      if (card === undefined) throw new Error(`不正な除去指定: ${command.index}`)
      if (run.deck.length <= 5) throw new Error('これ以上デッキを減らせない')
      return {
        ...run,
        gold: run.gold - price,
        deck: run.deck.filter((_, i) => i !== command.index),
        removalCount: (run.removalCount ?? 0) + 1,
      }
    }
    case 'ShopUpgrade': {
      if (run.phase !== 'shop' || run.shop === null) throw new Error('ショップではない')
      const price = shopUpgradePrice(run)
      if (run.gold < price) throw new Error(`ゴールドが足りない (${price}G)`)
      const card = run.deck[command.index]
      if (card === undefined) throw new Error(`不正な強化指定: ${command.index}`)
      if (isUpgraded(card)) throw new Error('すでに鍛えられている')
      if (upgradeTier(card.def) === 'none') {
        throw new Error(`${card.def.name} は鍛えられない (エナジー上限を上げる札は強化対象外)`)
      }
      return {
        ...run,
        gold: run.gold - price,
        deck: run.deck.map((c, i) => (i === command.index ? upgradeCard(c) : c)),
        upgradeCount: (run.upgradeCount ?? 0) + 1,
      }
    }
    case 'ShopLeave': {
      if (run.phase !== 'shop') throw new Error('ショップではない')
      return { ...run, shop: null, phase: 'map' }
    }
    case 'EventChoice': {
      if (run.phase !== 'event') throw new Error('イベントではない')
      return applyEventChoice(run, command.index, command.cardIndex)
    }
  }
}

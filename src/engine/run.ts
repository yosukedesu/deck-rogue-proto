// engine/run.ts — ドラフト連戦モード (純ロジック。DOM/React依存禁止)
// 10戦のラン: 戦闘 → 勝利で3枚提示から1枚ピック (スキップ可) → 次の敵。
// 敵は段階制で並び、深度スケーリング (強化+HP倍率) でだんだん強くなる (StS参考)。
// HPは持ち越し、3・6・9戦目クリア後に焚き火 (最大HPの30%回復)。
// ラン専用RNGをシードから回すため、同じシード+同じコマンド列=同じラン (リプレイ可能)。

import { startCombatWithOptions } from './combat.ts'
import {
  allCards,
  allRelics,
  buildDeck,
  buildRelicPermanent,
  getCardDef,
  getLeaderDef,
  getRelicDef,
} from './content.ts'
import { createRng, nextInt, shuffle, weightedIndex } from './rng.ts'
import { applyCommand } from './state.ts'
import type { CardColor, CardDef, CardInstance, Command, DeclarativeEffect, GameState, RngState, ReactionMode } from './types.ts'

export const RUN_BATTLES = 15
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
/** 焚き火: この戦闘 (0-based) をクリアした後に回復 */
/** 焚き火 (回復かカード除去の二択) が入る戦闘 (0-based: 3・6・9・12戦目クリア後) */
const CAMPFIRE_AFTER = new Set([2, 5, 8, 11])
const CAMPFIRE_HEAL_RATIO = 0.3
// 2026-08-26 再設計: 回復は焚き火に到達すれば自動で入る。
// 「回復か強化か」の二択にすると、実測で焚き火到達時HPが常に20〜46%のため全員が回復しか選べず、
// 強化・除去が一度も使われなかった (供給側の機能が「既に余裕のある者」にしか届かない状態だった)。
/** 勝利ごとの自動回復は廃止 (2026-08-25 StS踏襲。回復は焚き火のみ=マラソン構造) */
const VICTORY_HEAL = 0
/** エリート挑戦オファーが出る戦闘 (0-based: 2・5・8・11・14戦目)。確定済みルール表「エリート挑戦オファー」 */
const ELITE_OFFER_BATTLES = new Set([1, 4, 7, 10, 13])
/** エリート補正: 強化+2・HP×1.35 */
const ELITE_STRENGTH = 2
const ELITE_HP_SCALE = 1.35
/**
 * レリックは1ラン最大5個 (2026-08-26。旧3個)。
 * エリートオファーは5回あるのに3個上限では後半2回が提示すらされず、供給の穴になっていた。
 */
const RELIC_MAX = 5

/** 段階制の敵プール。battleIndex (0-based) → 抽選プール */
// 敵ID (ソロ) と編成ID (複数体。data/encounters.json) の混合プール
const ENEMY_TIERS: readonly (readonly string[])[] = [
  ['enemy_probe', 'enemy_wide_power', 'enc_probe_pair'], // 1〜5戦目
  ['enemy_set_wary', 'enemy_set_breaker', 'enemy_hexer', 'enemy_joker', 'enc_probe_trio', 'enc_joker_drummer'], // 6〜10戦目
  ['enemy_brute', 'enemy_wolf', 'enemy_moss', 'enemy_set_breaker', 'enc_wolf_drummer', 'enc_hexer_shadow', 'enc_breaker_hexer'], // 11〜14戦目 (大亀はボス専用)
  ['enemy_brute', 'enemy_turtle', 'enemy_warden'], // 15戦目 (ボスは単体)
]

function tierForBattle(battleIndex: number): readonly string[] {
  if (battleIndex < 5) return ENEMY_TIERS[0]
  if (battleIndex < 10) return ENEMY_TIERS[1]
  if (battleIndex < 14) return ENEMY_TIERS[2]
  return ENEMY_TIERS[3]
}

/**
 * 深度スケーリング: 敵の初期強化。
 * 敵データは15枚スターター基準の強さなので、ラン序盤は「若い個体」(マイナス強化) で登場し、
 * ボスでフルスペック近くになる (StSの「敵はだんだん強く」の再現)。
 */
export function depthStrength(battleIndex: number): number {
  // 若い個体補正は撤廃 (2026-08-25 人間基準化)。ボスのみ+1
  return battleIndex >= 14 ? 1 : 0
}

/** 深度スケーリング: 敵HP倍率。確定済みルール表「敵の数値基準」の帯に対応する */
export function depthHpScale(battleIndex: number): number {
  // 2026-08-26 再校正 (旧 0.75/0.85/0.95/1.0)。人間プレイで序盤の敵HPが
  // StS Act1 の約1.6倍と判明し、1〜3戦目 (焚き火前) のHP予算が赤字になっていた。
  // 素のHP90〜110の敵が ×0.55 で 49〜60 = StS Act1 通常敵の帯に入る。
  if (battleIndex < 5) return 0.55
  if (battleIndex < 10) return 0.8
  if (battleIndex < 14) return 0.95
  return 1.0
}

export type RunPhase = 'combat' | 'offer' | 'relic-reward' | 'campfire' | 'reward' | 'won' | 'lost'

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
  /** 現在 (または次) の戦闘番号 0-based */
  readonly battleIndex: number
  /** ラン開始時に確定した全戦闘の敵 */
  readonly enemyIds: readonly string[]
  readonly phase: RunPhase
  readonly combat: GameState | null
  /** 報酬フェーズの提示カード (cardId) */
  readonly rewardOptions: readonly string[] | null
  /** ピック履歴 (cardId。統計・結果画面用) */
  readonly picks: readonly string[]
  /** 所持レリック (relicId。最大3個) */
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
}

export type RunCommand =
  | { readonly type: 'StartRun'; readonly seed: number }
  | { readonly type: 'Combat'; readonly command: Command } // 戦闘中コマンドの委譲
  | { readonly type: 'PickReward'; readonly index: number }
  | { readonly type: 'SkipReward' }
  | { readonly type: 'ChooseElite'; readonly elite: boolean } // エリート挑戦オファーへの回答
  | { readonly type: 'PickRelic'; readonly index: number }
  | { readonly type: 'SkipRelic' }
  // 焚き火 (確定済みルール表「焚き火」): 休んで回復するか、デッキから1枚を永久に取り除くか
  | { readonly type: 'CampfireRest' }
  | { readonly type: 'CampfireRemove'; readonly index: number }
  | { readonly type: 'CampfireUpgrade'; readonly index: number }

/**
 * 次の戦闘へ進む。エリートオファー対象の戦闘 (2/5/8戦目) では先に 'offer' フェーズを挟む
 * (レリック枠が埋まっている場合はオファーなしで通常戦闘へ)
 */
function startBattle(run: RunState): RunState {
  if (ELITE_OFFER_BATTLES.has(run.battleIndex) && run.relics.length < RELIC_MAX) {
    return { ...run, phase: 'offer', combat: null, rewardOptions: null, currentElite: false }
  }
  return launchCombat(run, false)
}

/** 戦闘を実際に開始する (戦闘シードはラン RNG から決定的に生成)。elite でエリート補正 */
function launchCombat(run: RunState, elite: boolean): RunState {
  const [combatSeed, rng] = nextInt(run.rng, 0, 2 ** 31 - 1)
  const combat = startCombatWithOptions(combatSeed, run.mode, run.enemyIds[run.battleIndex], {
    deck: run.deck,
    leaderId: run.leaderId,
    playerHp: run.hp,
    enemyHpScale: depthHpScale(run.battleIndex) * (elite ? ELITE_HP_SCALE : 1),
    enemyStrength: depthStrength(run.battleIndex) + (elite ? ELITE_STRENGTH : 0),
    relicPermanents: run.relics
      .map(getRelicDef)
      .filter((r) => (r.effects?.length ?? 0) > 0)
      .map(buildRelicPermanent),
  })
  return { ...run, rng, combat, phase: 'combat', rewardOptions: null, currentElite: elite }
}

export function createRun(seed: number, mode: ReactionMode, leaderId = 'leader_green'): RunState {
  const leader = getLeaderDef(leaderId)
  let rng = createRng(seed)
  const enemyIds: string[] = []
  for (let i = 0; i < RUN_BATTLES; i++) {
    const pool = tierForBattle(i)
    // 直前2戦と同じ敵は避ける (2026-08-26。同型の長期戦が連続すると「同じ戦闘を3回やらされている」
    // 体感になるとプレイテストで3人が指摘)。プールが小さくて避けられない場合はそのまま
    const recent = enemyIds.slice(-2)
    const fresh = pool.filter((id) => !recent.includes(id))
    const candidates = fresh.length > 0 ? fresh : pool
    const [idx, next] = nextInt(rng, 0, candidates.length - 1)
    rng = next
    enemyIds.push(candidates[idx])
  }
  // レリック候補列もシードから確定 (リプレイ再現性)
  const [relicQueue, rngAfterRelics] = shuffle(
    rng,
    allRelics.map((r) => r.id),
  )
  const run: RunState = {
    seed,
    mode,
    leaderId,
    colors: leader.colors,
    rng: rngAfterRelics,
    deck: buildDeck(leader.runDeckId),
    hp: leader.maxHp,
    maxHp: leader.maxHp,
    battleIndex: 0,
    enemyIds,
    phase: 'combat',
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
  }
  return startBattle(run)
}

/**
 * 効果名 → アーキタイプの軸。確定済みルール表「軸の重み付け」。
 * 効果に軸が現れない札 (多段ヒットの成長ペイオフ・貫通のトランプル札など) は
 * CardDef.axis で明示する (JSONで宣言。ここは自動導出ぶんだけ)。
 */
const EFFECT_AXIS: Record<string, string> = {
  addGrowth: 'growth', doubleGrowth: 'growth', dischargeGrowth: 'growth',
  gainEnergyMax: 'ramp', dealDamagePerEnergyMax: 'ramp', gainBlockPerEnergyMax: 'ramp',
  addMomentum: 'trample',
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

/** 軸一致による抽選の重み。0一致=1 / 1一致=3 / 2以上=5 */
function rewardWeight(def: CardDef, deckAxes: ReadonlySet<string>): number {
  const matches = axesOf(def).filter((a) => deckAxes.has(a)).length
  return 1 + 2 * Math.min(matches, 2)
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
  // デッキに既にある軸を引き寄せる (確定済みルール表「軸の重み付け」)。
  // 短いラン (15戦・最大14ピック) でもアーキタイプが成立するようにするための補正。
  const deckAxes = new Set<string>()
  for (const card of run.deck) for (const a of axesOf(card.def)) deckAxes.add(a)

  const remaining = [...pool]
  const picked: string[] = []
  let rng = run.rng
  const want = leader.rewardChoices + run.rewardChoicesBonus
  while (picked.length < want && remaining.length > 0) {
    // 最後の1枠だけ重み付けなしの純粋ランダムにする (2026-08-26)。
    // 重み付けが強いと同じ色の第二の柱に一生触れられない
    // (赤で15戦通して憤怒の札が1枚も提示されなかった)。3枠が軸を伸ばし、1枠が乗り換えの機会を作る
    const isFreeSlot = picked.length === want - 1
    const weights = remaining.map((c) => (isFreeSlot ? 1 : rewardWeight(c, deckAxes)))
    const [idx, next] = weightedIndex(rng, weights)
    rng = next
    picked.push(remaining[idx].id)
    remaining.splice(idx, 1)
  }
  return { ...run, rng, rewardOptions: picked, phase: 'reward' }
}

/** 戦闘勝利後の処理: HP持ち越し・焚き火 → (エリートならレリック報酬 →) カード報酬 or ラン勝利 */
function afterVictory(run: RunState, combat: GameState): RunState {
  // 自動回復は狩人の恵み (victoryHealBonus) のみ。3・6・9・12戦目クリア後は焚き火フェーズ
  const hp = Math.min(run.maxHp, combat.player.hp + VICTORY_HEAL + run.victoryHealBonus)
  const next: RunState = { ...run, combat, hp }
  if (run.battleIndex === RUN_BATTLES - 1) return { ...next, phase: 'won' }
  // エリート戦の勝利: レリック3択 (取得済みを除いた候補列の先頭から)
  if (run.currentElite && run.relics.length < RELIC_MAX) {
    const remaining = run.relicQueue.filter((id) => !run.relics.includes(id))
    if (remaining.length > 0) {
      return { ...next, phase: 'relic-reward', relicOptions: remaining.slice(0, 3) }
    }
  }
  return campfireOrReward(next)
}

/** 焚き火の戦闘なら二択を挟み、そうでなければ通常のカード報酬へ (確定済みルール表「焚き火」) */
function campfireOrReward(run: RunState): RunState {
  if (CAMPFIRE_AFTER.has(run.battleIndex)) {
    // 回復は自動 (2026-08-26)。焚き火の選択は「鍛える / 取り除く / 何もしない」で、HPと排他にしない
    const hp = Math.min(run.maxHp, run.hp + Math.floor(run.maxHp * run.campfireRatio))
    return { ...run, hp, phase: 'campfire', rewardOptions: null }
  }
  return rollRewards(run)
}

/**
 * 強化の対象になる「量」の効果 (確定済みルール表「焚き火」)。
 * ドロー・エナジー・上限・成長・勢い・霊気などの「単位」効果と、
 * 参照スケーリング (×N) は対象外 — engine の倍率に触れないための安全弁。
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

/** すでに鍛えられているか (同じカードは1回だけ) */
export function isUpgraded(card: CardInstance): boolean {
  return card.def.name.endsWith('+')
}

/**
 * カードを鍛える: 量の効果を+50% (切り上げ) して名前に「+」を付ける。
 * 火弾6→9・防御5→8 は StS の Strike+ / Defend+ と同値。
 * def を作り直すので engine 側に強化用の分岐は要らない (id は据え置き = 軸判定も不変)。
 */
export function upgradeCard(card: CardInstance): CardInstance {
  const boost = (e: DeclarativeEffect): DeclarativeEffect => {
    if (!UPGRADABLE_EFFECTS.has(e.effect) || e.amount === undefined) return e
    return {
      ...e,
      amount: Math.ceil(e.amount * 1.5),
      ...(e.amountMax !== undefined ? { amountMax: Math.ceil(e.amountMax * 1.5) } : {}),
    }
  }
  return {
    ...card,
    def: {
      ...card.def,
      name: `${card.def.name}+`,
      effects: card.def.effects.map(boost),
      ...(card.def.modes !== undefined
        ? { modes: card.def.modes.map((m) => ({ ...m, effects: m.effects.map(boost) })) }
        : {}),
    },
  }
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
      const card: CardInstance = { uid: `pick${run.battleIndex}_${cardId}`, def: getCardDef(cardId) }
      const next: RunState = {
        ...run,
        deck: [...run.deck, card],
        picks: [...run.picks, cardId],
        battleIndex: run.battleIndex + 1,
      }
      return startBattle(next)
    }
    case 'SkipReward': {
      if (run.phase !== 'reward') throw new Error('報酬フェーズではない')
      return startBattle({ ...run, battleIndex: run.battleIndex + 1 })
    }
    case 'ChooseElite': {
      if (run.phase !== 'offer') throw new Error('オファーフェーズではない')
      return launchCombat(run, command.elite)
    }
    case 'PickRelic': {
      if (run.phase !== 'relic-reward' || run.relicOptions === null) {
        throw new Error('レリック報酬フェーズではない')
      }
      const relicId = run.relicOptions[command.index]
      if (relicId === undefined) throw new Error(`不正なレリック指定: ${command.index}`)
      let next: RunState = { ...run, relics: [...run.relics, relicId], relicOptions: null }
      next = applyRelicBonus(next, relicId)
      return campfireOrReward(next)
    }
    case 'SkipRelic': {
      if (run.phase !== 'relic-reward') throw new Error('レリック報酬フェーズではない')
      return campfireOrReward({ ...run, relicOptions: null })
    }
    case 'CampfireRest': {
      // 「何もしない」= 回復だけ受け取って次へ (回復は campfireOrReward で適用済み)
      if (run.phase !== 'campfire') throw new Error('焚き火フェーズではない')
      return rollRewards(run)
    }
    case 'CampfireUpgrade': {
      if (run.phase !== 'campfire') throw new Error('焚き火フェーズではない')
      const card = run.deck[command.index]
      if (card === undefined) throw new Error(`不正な強化指定: ${command.index}`)
      if (isUpgraded(card)) throw new Error('すでに鍛えられている')
      return rollRewards({
        ...run,
        deck: run.deck.map((c, i) => (i === command.index ? upgradeCard(c) : c)),
      })
    }
    case 'CampfireRemove': {
      if (run.phase !== 'campfire') throw new Error('焚き火フェーズではない')
      const card = run.deck[command.index]
      if (card === undefined) throw new Error(`不正な除去指定: ${command.index}`)
      // デッキが痩せすぎないよう最低5枚は残す
      if (run.deck.length <= 5) throw new Error('これ以上デッキを減らせない')
      return rollRewards({ ...run, deck: run.deck.filter((_, i) => i !== command.index) })
    }
  }
}

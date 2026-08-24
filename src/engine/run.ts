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
import { createRng, nextInt, shuffle } from './rng.ts'
import { applyCommand } from './state.ts'
import type { CardColor, CardInstance, Command, GameState, RngState, ReactionMode } from './types.ts'

export const RUN_BATTLES = 10
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
])
/** 焚き火: この戦闘 (0-based) をクリアした後に回復 */
const CAMPFIRE_AFTER = new Set([2, 5, 8])
const CAMPFIRE_HEAL_RATIO = 0.3
/** 勝利ごとの自動回復は廃止 (2026-08-25 StS踏襲。回復は焚き火のみ=マラソン構造) */
const VICTORY_HEAL = 0
/** エリート挑戦オファーが出る戦闘 (0-based: 2・5・8戦目)。確定済みルール表「エリート挑戦オファー」 */
const ELITE_OFFER_BATTLES = new Set([1, 4, 7])
/** エリート補正: 強化+2・HP×1.35 */
const ELITE_STRENGTH = 2
const ELITE_HP_SCALE = 1.35
/** レリックは1ラン最大3個 */
const RELIC_MAX = 3

/** 段階制の敵プール。battleIndex (0-based) → 抽選プール */
// 敵ID (ソロ) と編成ID (複数体。data/encounters.json) の混合プール
const ENEMY_TIERS: readonly (readonly string[])[] = [
  ['enemy_probe', 'enemy_wide_power', 'enc_probe_pair'], // 1〜3戦目
  ['enemy_set_wary', 'enemy_set_breaker', 'enemy_hexer', 'enemy_joker', 'enc_probe_trio', 'enc_joker_drummer'], // 4〜6戦目
  ['enemy_brute', 'enemy_wolf', 'enemy_moss', 'enemy_set_breaker', 'enc_wolf_drummer', 'enc_hexer_shadow', 'enc_breaker_hexer'], // 7〜9戦目 (大亀はボス専用)
  ['enemy_brute', 'enemy_turtle', 'enemy_warden'], // 10戦目 (ボスは単体)
]

function tierForBattle(battleIndex: number): readonly string[] {
  if (battleIndex < 3) return ENEMY_TIERS[0]
  if (battleIndex < 6) return ENEMY_TIERS[1]
  if (battleIndex < 9) return ENEMY_TIERS[2]
  return ENEMY_TIERS[3]
}

/**
 * 深度スケーリング: 敵の初期強化。
 * 敵データは15枚スターター基準の強さなので、ラン序盤は「若い個体」(マイナス強化) で登場し、
 * ボスでフルスペック近くになる (StSの「敵はだんだん強く」の再現)。
 */
export function depthStrength(battleIndex: number): number {
  // 若い個体補正は撤廃 (2026-08-25 人間基準化)。ボスのみ+1
  return battleIndex >= 9 ? 1 : 0
}

/** 深度スケーリング: 敵HP倍率 (序盤は4割、ボスで7割弱) */
export function depthHpScale(battleIndex: number): number {
  // 緩ランプ (2026-08-25): 序盤からStS初戦相当の手応え
  if (battleIndex < 3) return 0.75
  if (battleIndex < 6) return 0.85
  if (battleIndex < 9) return 0.95
  return 1.0
}

export type RunPhase = 'combat' | 'offer' | 'relic-reward' | 'reward' | 'won' | 'lost'

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
    const [idx, next] = nextInt(rng, 0, pool.length - 1)
    rng = next
    enemyIds.push(pool[idx])
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

/** 報酬を抽選 (リーダーの色アイデンティティのカードのみ・基本札除外・重複なし)。候補数はリーダー個性+収集家の鞄 */
function rollRewards(run: RunState): RunState {
  const leader = getLeaderDef(run.leaderId)
  const pool = allCards
    .filter((c) => run.colors.includes(c.color) && !REWARD_EXCLUDED.has(c.id))
    .map((c) => c.id)
  const [shuffled, rng] = shuffle(run.rng, pool)
  return {
    ...run,
    rng,
    rewardOptions: shuffled.slice(0, leader.rewardChoices + run.rewardChoicesBonus),
    phase: 'reward',
  }
}

/** 戦闘勝利後の処理: HP持ち越し・焚き火 → (エリートならレリック報酬 →) カード報酬 or ラン勝利 */
function afterVictory(run: RunState, combat: GameState): RunState {
  // 自動回復は狩人の恵み (victoryHealBonus) のみ。3・6・9戦目クリア後は焚き火
  let hp = Math.min(run.maxHp, combat.player.hp + VICTORY_HEAL + run.victoryHealBonus)
  if (CAMPFIRE_AFTER.has(run.battleIndex)) {
    hp = Math.min(run.maxHp, hp + Math.floor(run.maxHp * run.campfireRatio))
  }
  const next: RunState = { ...run, combat, hp }
  if (run.battleIndex === RUN_BATTLES - 1) return { ...next, phase: 'won' }
  // エリート戦の勝利: レリック3択 (取得済みを除いた候補列の先頭から)
  if (run.currentElite && run.relics.length < RELIC_MAX) {
    const remaining = run.relicQueue.filter((id) => !run.relics.includes(id))
    if (remaining.length > 0) {
      return { ...next, phase: 'relic-reward', relicOptions: remaining.slice(0, 3) }
    }
  }
  return rollRewards(next)
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
      return rollRewards(next)
    }
    case 'SkipRelic': {
      if (run.phase !== 'relic-reward') throw new Error('レリック報酬フェーズではない')
      return rollRewards({ ...run, relicOptions: null })
    }
  }
}

// engine/run.ts — ドラフト連戦モード (純ロジック。DOM/React依存禁止)
// 10戦のラン: 戦闘 → 勝利で3枚提示から1枚ピック (スキップ可) → 次の敵。
// 敵は段階制で並び、深度スケーリング (強化+HP倍率) でだんだん強くなる (StS参考)。
// HPは持ち越し、3・6・9戦目クリア後に焚き火 (最大HPの30%回復)。
// ラン専用RNGをシードから回すため、同じシード+同じコマンド列=同じラン (リプレイ可能)。

import { PLAYER_MAX_HP, startCombatWithOptions } from './combat.ts'
import { allCards, buildDeck, getCardDef } from './content.ts'
import { createRng, nextInt, shuffle } from './rng.ts'
import { applyCommand } from './state.ts'
import type { CardColor, CardInstance, Command, GameState, RngState, ReactionMode } from './types.ts'

export const RUN_BATTLES = 10
/** 色ごとのラン初期デッキ */
const RUN_STARTER_DECK: Record<CardColor, string> = {
  green: 'run_basic',
  blue: 'run_basic_blue',
  red: 'run_basic_red',
}
const REWARD_CHOICES = 3
/** 報酬プールから除外する基本札 (スターターに入っている素のカード) */
const REWARD_EXCLUDED = new Set([
  'green_strike',
  'green_guard',
  'blue_strike',
  'blue_guard',
  'red_strike',
  'red_guard',
])
/** 焚き火: この戦闘 (0-based) をクリアした後に回復 */
const CAMPFIRE_AFTER = new Set([2, 5, 8])
const CAMPFIRE_HEAL_RATIO = 0.3
/** 勝利ごとの小休止による回復量 */
const VICTORY_HEAL = 10

/** 段階制の敵プール。battleIndex (0-based) → 抽選プール */
const ENEMY_TIERS: readonly (readonly string[])[] = [
  ['enemy_probe', 'enemy_wide_power'], // 1〜3戦目
  ['enemy_set_wary', 'enemy_set_breaker'], // 4〜6戦目
  ['enemy_set_wary', 'enemy_set_breaker', 'enemy_brute'], // 7〜9戦目 (大亀はボス専用)
  ['enemy_brute', 'enemy_turtle'], // 10戦目 (ボス)
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
  if (battleIndex < 3) return -4
  if (battleIndex < 6) return -4
  if (battleIndex < 9) return -3
  return -1
}

/** 深度スケーリング: 敵HP倍率 (序盤は4割、ボスで7割弱) */
export function depthHpScale(battleIndex: number): number {
  return 0.4 + 0.03 * battleIndex
}

export type RunPhase = 'combat' | 'reward' | 'won' | 'lost'

export interface RunState {
  readonly seed: number
  readonly mode: ReactionMode
  /** ランの色 (初期デッキと報酬プールを決める) */
  readonly color: CardColor
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
}

export type RunCommand =
  | { readonly type: 'StartRun'; readonly seed: number }
  | { readonly type: 'Combat'; readonly command: Command } // 戦闘中コマンドの委譲
  | { readonly type: 'PickReward'; readonly index: number }
  | { readonly type: 'SkipReward' }

/** 次の戦闘を開始する (戦闘シードはラン RNG から決定的に生成) */
function startBattle(run: RunState): RunState {
  const [combatSeed, rng] = nextInt(run.rng, 0, 2 ** 31 - 1)
  const combat = startCombatWithOptions(combatSeed, run.mode, run.enemyIds[run.battleIndex], {
    deck: run.deck,
    playerHp: run.hp,
    enemyHpScale: depthHpScale(run.battleIndex),
    enemyStrength: depthStrength(run.battleIndex),
  })
  return { ...run, rng, combat, phase: 'combat', rewardOptions: null }
}

export function createRun(seed: number, mode: ReactionMode, color: CardColor = 'green'): RunState {
  let rng = createRng(seed)
  const enemyIds: string[] = []
  for (let i = 0; i < RUN_BATTLES; i++) {
    const pool = tierForBattle(i)
    const [idx, next] = nextInt(rng, 0, pool.length - 1)
    rng = next
    enemyIds.push(pool[idx])
  }
  const run: RunState = {
    seed,
    mode,
    color,
    rng,
    deck: buildDeck(RUN_STARTER_DECK[color]),
    hp: PLAYER_MAX_HP,
    maxHp: PLAYER_MAX_HP,
    battleIndex: 0,
    enemyIds,
    phase: 'combat',
    combat: null,
    rewardOptions: null,
    picks: [],
  }
  return startBattle(run)
}

/** 報酬3枚を抽選 (ランの色のカードのみ・基本札除外・重複なし) */
function rollRewards(run: RunState): RunState {
  const pool = allCards
    .filter((c) => c.color === run.color && !REWARD_EXCLUDED.has(c.id))
    .map((c) => c.id)
  const [shuffled, rng] = shuffle(run.rng, pool)
  return { ...run, rng, rewardOptions: shuffled.slice(0, REWARD_CHOICES), phase: 'reward' }
}

/** 戦闘勝利後の処理: HP持ち越し・焚き火・報酬 or ラン勝利 */
function afterVictory(run: RunState, combat: GameState): RunState {
  // 勝利ごとの小休止 (+10)。3・6・9戦目クリア後はさらに焚き火 (30%)
  let hp = Math.min(run.maxHp, combat.player.hp + VICTORY_HEAL)
  if (CAMPFIRE_AFTER.has(run.battleIndex)) {
    hp = Math.min(run.maxHp, hp + Math.floor(run.maxHp * CAMPFIRE_HEAL_RATIO))
  }
  const next: RunState = { ...run, combat, hp }
  if (run.battleIndex === RUN_BATTLES - 1) return { ...next, phase: 'won' }
  return rollRewards(next)
}

export function applyRunCommand(run: RunState, command: RunCommand): RunState {
  switch (command.type) {
    case 'StartRun':
      return createRun(command.seed, run.mode, run.color)
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
  }
}

// sim/run.ts — ヘッドレス自動対戦 (Node 単体実行: npm run sim)
//
// 使い方:
//   npm run sim -- [1セルあたりの対戦数] [ベースシード]        # 単発戦闘: デッキ×敵の総当たり
//   npm run sim -- [対戦数] [シード] all                        # 3方式比較モード
//   npm run sim -- runs [ラン数] [ベースシード]                 # ドラフト連戦の走破率
//
// ボットは全構成共通の単純グリーディ (差がボットの賢さではなくルール・デッキから出るようにする):
//   - 自ターン: (set系) まずリアクションを1枚伏せる → ランプ→置物→成長→フィニッシャー→攻撃→防御
//     の優先順でエナジーの続く限りプレイ (hold-manual は最安リアクションのコストを温存)
//   - 勢い生成付き攻撃は他の攻撃より先に。成長0での doubleGrowth は打たない
//   - 割り込み: set-confirm は常に「発動」、hold-manual は発動可能な先頭カードを常に発動
//   - ランの報酬ピック: 常に先頭 (index 0)

import { allDecks, allEnemies, getCardDef } from '../engine/content.ts'
import { effectiveCost, isPlayableFromHand } from '../engine/effects.ts'
import { playableReactions } from '../engine/reactions/hold-manual.ts'
import { applyRunCommand, createRun, RUN_BATTLES } from '../engine/run.ts'
import { applyCommand, createInitialState } from '../engine/state.ts'
import type { CardCategory, CardInstance, Command, GameState, ReactionMode } from '../engine/types.ts'

const PLAY_PRIORITY: readonly CardCategory[] = [
  'ramp',
  'draw', // ドローは先に (ストームの詠唱数も稼げる)
  'permanent', // 置物はエンジンなので早置き
  'growth',
  'finisher',
  'attack',
  'defend',
]
const TURN_LIMIT = 50 // 無限戦闘の保険。超えたら敗北扱い

/** 成長0での doubleGrowth など、プレイしても無意味・不可能なカードを弾く */
function isWorthPlaying(state: GameState, card: CardInstance): boolean {
  if (card.def.effects.some((e) => e.effect === 'doubleGrowth')) return state.player.growth > 0
  // ストーム系: 詠唱数0で撃っても無意味
  const stormEffects = ['dealDamagePerCardPlayed', 'gainIceBlockPerCardPlayed', 'drawCardsPerCardPlayed']
  if (
    card.def.effects.some((e) => stormEffects.includes(e.effect)) &&
    state.player.cardsPlayedThisTurn === 0
  ) {
    return false
  }
  // 霊気放出: 霊気3未満で撃つのはもったいない (単純ボットの閾値)
  if (card.def.effects.some((e) => e.effect === 'dischargeAether') && state.player.aether < 3) {
    return false
  }
  const discardCost = card.def.discardCost ?? 0
  if (discardCost > 0 && state.player.hand.length - 1 < discardCost) return false
  return true
}

/** PlayCard コマンドを組み立てる (選択式は先頭モード、捨てコストは手札の末尾から充当する単純方針) */
function buildPlayCommand(state: GameState, card: CardInstance): Command {
  const modeIndex = (card.def.modes?.length ?? 0) > 0 ? 0 : undefined
  const discardCost = card.def.discardCost ?? 0
  const discardUids =
    discardCost > 0
      ? state.player.hand
          .filter((c) => c.uid !== card.uid)
          .slice(-discardCost)
          .map((c) => c.uid)
      : undefined
  return { type: 'PlayCard', cardUid: card.uid, modeIndex, discardUids }
}

/** 現在の戦闘状態に対するボットの次の一手 (単発戦闘・ラン共用の純関数) */
function chooseCommand(s: GameState): Command {
  if (s.phase === 'awaiting-reaction') {
    if (s.reactionMode === 'set-confirm') return { type: 'ConfirmReaction', fire: true }
    const candidates = playableReactions(s)
    return candidates.length > 0
      ? { type: 'ReactManual', cardUid: candidates[0].uid }
      : { type: 'ConfirmReaction', fire: false }
  }
  if (s.phase !== 'player-turn') throw new Error(`ボットが手番でない: ${s.phase}`)

  // set系: まずリアクションを伏せる (伏せ場が空いていて払えるなら常に)
  if (s.reactionMode !== 'hold-manual' && s.player.setCards.length === 0) {
    const reaction = s.player.hand.find(
      (c) => c.def.category === 'reaction' && c.def.cost <= s.player.energy,
    )
    if (reaction) return { type: 'SetCard', cardUid: reaction.uid }
  }

  // hold-manual: 敵ターンにリアクションを切るため、最安リアクション分のエナジーを温存する
  const reactionCosts = s.player.hand
    .filter((c) => c.def.category === 'reaction')
    .map((c) => c.def.cost)
  const reserve =
    s.reactionMode === 'hold-manual' && reactionCosts.length > 0 ? Math.min(...reactionCosts) : 0
  // ストームのペイオフ (詠唱数参照フィニッシャー) が手札にあるなら、その分のエナジーを温存する
  // (ドローで詠唱数だけ稼いでエナジー切れで撃てない、を防ぐ)
  const stormCosts = s.player.hand
    .filter((c) => c.def.effects.some((e) => e.effect === 'dealDamagePerCardPlayed'))
    .map((c) => c.def.cost)
  const stormReserve = stormCosts.length > 0 ? Math.min(...stormCosts) : 0
  const spendable = s.player.energy - reserve

  for (const category of PLAY_PRIORITY) {
    // フィニッシャー自身は温存分を使ってよい
    const budget = category === 'finisher' ? spendable : spendable - stormReserve
    let candidates = s.player.hand.filter(
      (c) =>
        c.def.category === category &&
        isPlayableFromHand(c) &&
        effectiveCost(s, c) <= budget &&
        isWorthPlaying(s, c),
    )
    // 勢い生成付きの攻撃 (突進の助走など) を同ターンの他の攻撃・フィニッシャーより先に打つ
    if (category === 'finisher' || category === 'attack') {
      const momentumFirst = s.player.hand.filter(
        (c) =>
          (c.def.category === 'attack' || c.def.category === 'finisher') &&
          c.def.effects.some((e) => e.effect === 'addMomentum') &&
          effectiveCost(s, c) <= spendable,
      )
      if (momentumFirst.length > 0) candidates = momentumFirst
    }
    // 成長カテゴリ内では addGrowth (年輪) を doubleGrowth (開花の儀) より先に
    const card =
      candidates.find((c) => c.def.effects.some((e) => e.effect === 'addGrowth')) ?? candidates[0]
    if (card) return buildPlayCommand(s, card)
  }
  return { type: 'EndTurn' }
}

interface BattleResult {
  readonly won: boolean
  readonly turns: number
  readonly triggered: number
  readonly whiffed: number
}

function runBattle(mode: ReactionMode, deckId: string, enemyId: string, seed: number): BattleResult {
  let s = applyCommand(createInitialState(seed, mode), { type: 'StartCombat', seed, enemyId, deckId })
  let actions = 0
  while (s.phase !== 'won' && s.phase !== 'lost') {
    if (s.turn > TURN_LIMIT) break // 膠着 → 敗北扱い
    if (++actions > 3000) break // 1戦闘の行動数セーフガード (無限コンボの検知)
    s = applyCommand(s, chooseCommand(s))
  }
  return {
    won: s.phase === 'won',
    turns: Math.min(s.turn, TURN_LIMIT),
    triggered: s.eventLog.filter((e) => e.type === 'ReactionTriggered').length,
    whiffed: s.eventLog.filter((e) => e.type === 'ReactionWhiffed').length,
  }
}

// ============================================================
// モード1: ドラフト連戦の走破率 (npm run sim -- runs 200 42)
// ============================================================

/** ボットの報酬ピック: 攻撃系優先の単純方針 (カテゴリ優先順で最初に合致した提示を取る) */
const PICK_PRIORITY: readonly CardCategory[] = [
  'permanent', // 置物は長いランで最も価値が高い (茨の茂み・賢者の泉など)
  'growth',
  'draw',
  'attack',
  'finisher',
  'defend',
  'ramp',
  'reaction',
]

function chooseReward(run: { rewardOptions: readonly string[] | null }): number {
  const options = run.rewardOptions ?? []
  for (const category of PICK_PRIORITY) {
    const idx = options.findIndex((id) => getCardDef(id).category === category)
    if (idx >= 0) return idx
  }
  return 0
}

function simulateRuns(count: number, baseSeed: number): void {
  console.error(`# ドラフト連戦 sim: ${count}ラン × 3色, baseSeed=${baseSeed} (ピックはカテゴリ優先の単純方針)`)
  console.log('color,runs,cleared,clearRate,avgBattlesCleared,avgFinalDeckSize')
  for (const color of ['green', 'blue', 'red'] as const) {
    const deathsByBattle = new Array<number>(RUN_BATTLES).fill(0)
    let cleared = 0
    let totalBattlesCleared = 0
    let totalDeckSize = 0
    for (let i = 0; i < count; i++) {
      let run = createRun((baseSeed + i) >>> 0, 'set-confirm', color)
      let aborted = false
      let actions = 0
      while (run.phase === 'combat' || run.phase === 'reward') {
        if (++actions > 30000) { aborted = true; break } // ラン全体の行動数セーフガード
        if (run.phase === 'reward') {
          run = applyRunCommand(run, { type: 'PickReward', index: chooseReward(run) })
          continue
        }
        if (run.combat!.turn > TURN_LIMIT) {
          aborted = true // 膠着 → そこで敗北扱い
          break
        }
        run = applyRunCommand(run, { type: 'Combat', command: chooseCommand(run.combat!) })
      }
      const battlesCleared = run.phase === 'won' ? RUN_BATTLES : run.battleIndex
      totalBattlesCleared += battlesCleared
      totalDeckSize += run.deck.length
      if (run.phase === 'won' && !aborted) cleared += 1
      else deathsByBattle[Math.min(run.battleIndex, RUN_BATTLES - 1)] += 1
    }
    console.log(
      [
        color,
        count,
        cleared,
        (cleared / count).toFixed(3),
        (totalBattlesCleared / count).toFixed(2),
        (totalDeckSize / count).toFixed(1),
      ].join(','),
    )
    console.error(
      `# ${color} 敗北した戦闘の分布: ` +
        deathsByBattle.map((d, i) => (d > 0 ? `${i + 1}戦目:${d}` : null)).filter(Boolean).join(' '),
    )
  }
}

// ============================================================
// モード2: 単発戦闘の総当たり (従来)
// ============================================================

interface Acc {
  battles: number
  wins: number
  turns: number
  triggered: number
  whiffed: number
}

function emptyAcc(): Acc {
  return { battles: 0, wins: 0, turns: 0, triggered: 0, whiffed: 0 }
}

function row(mode: string, deck: string, enemy: string, a: Acc): string {
  const reactions = a.triggered + a.whiffed
  return [
    mode,
    deck,
    enemy,
    a.battles,
    a.wins,
    (a.wins / a.battles).toFixed(3),
    (a.turns / a.battles).toFixed(2),
    reactions > 0 ? (a.triggered / reactions).toFixed(3) : '0.000',
    reactions > 0 ? (a.whiffed / reactions).toFixed(3) : '0.000',
  ].join(',')
}

function simulateBattles(battlesPerCell: number, baseSeed: number, compareModes: boolean): void {
  const MODES: readonly ReactionMode[] = compareModes
    ? ['set-auto', 'hold-manual', 'set-confirm']
    : ['set-confirm']
  const decks = allDecks.filter((d) => !d.id.startsWith('run_basic')) // ラン用基本デッキは対象外
  console.error(
    `# deck-rogue-proto sim: ${battlesPerCell}戦 × ${MODES.length}方式 × ${decks.length}デッキ × ${allEnemies.length}類型, baseSeed=${baseSeed}`,
  )
  console.error('# 発動率 = 発動 / (発動+空振り)。空振り = そのターン使われなかったリアクション')
  console.log('mode,deck,enemy,battles,wins,winRate,avgTurns,reactionFireRate,whiffRate')
  for (const mode of MODES) {
    for (const deck of decks) {
      const deckAcc = emptyAcc()
      for (const enemy of allEnemies) {
        const cell = emptyAcc()
        for (let b = 0; b < battlesPerCell; b++) {
          const r = runBattle(mode, deck.id, enemy.id, (baseSeed + b) >>> 0)
          cell.battles += 1
          cell.wins += r.won ? 1 : 0
          cell.turns += r.turns
          cell.triggered += r.triggered
          cell.whiffed += r.whiffed
        }
        console.log(row(mode, deck.id, enemy.id, cell))
        deckAcc.battles += cell.battles
        deckAcc.wins += cell.wins
        deckAcc.turns += cell.turns
        deckAcc.triggered += cell.triggered
        deckAcc.whiffed += cell.whiffed
      }
      console.log(row(mode, deck.id, 'ALL', deckAcc))
    }
  }
}

// ============================================================
// エントリポイント
// ============================================================

if (process.argv[2] === 'runs') {
  simulateRuns(Number(process.argv[3] ?? 100), Number(process.argv[4] ?? 1))
} else {
  simulateBattles(
    Number(process.argv[2] ?? 100),
    Number(process.argv[3] ?? 1),
    process.argv.includes('all'),
  )
}

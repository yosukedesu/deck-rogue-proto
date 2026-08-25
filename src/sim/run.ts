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

import { allDecks, allEnemies, allLeaders, getCardDef } from '../engine/content.ts'
import { effectiveCost, isDamageEffect, isPlayableFromHand } from '../engine/effects.ts'
import { playableReactions } from '../engine/reactions/hold-manual.ts'
import { applyRunCommand, createRun, RUN_BATTLES } from '../engine/run.ts'
import { applyCommand, createInitialState } from '../engine/state.ts'
import type { CardDef, CardInstance, Command, GameState, ReactionMode } from '../engine/types.ts'

/**
 * ボット用の役割分類。カードタイプ廃止後は効果から導出する
 * (タイプは物理/呪文/リアクション/置物の機械的区分になったため)
 */
type BotRole = 'ramp' | 'draw' | 'permanent' | 'growth' | 'bighit' | 'attack' | 'defend' | 'reaction' | 'other'

function botRole(def: CardDef): BotRole {
  if (def.type === 'reaction') return 'reaction'
  if (def.type === 'permanent') return 'permanent'
  const effects = def.modes?.length ? def.modes[0].effects : def.effects
  const has = (...ids: string[]) => effects.some((e) => ids.includes(e.effect))
  if (has('gainEnergyMax', 'gainEnergy', 'discountNext')) return 'ramp'
  // 召喚 (白): トークンを場に出すカードは置物枠で早置き
  if (has('summonPermanent')) return 'permanent'
  if (has('addGrowth', 'doubleGrowth')) return 'growth'
  if (effects.some(isDamageEffect)) return def.cost >= 3 ? 'bighit' : 'attack'
  // 純延焼 (火の粉の雨) と混乱 (幻惑の囁き) は攻撃系として運用する
  if (has('applyBurn', 'confuse')) return def.cost >= 3 ? 'bighit' : 'attack'
  if (has('drawCards', 'impulseDraw', 'drawCardsPerCardPlayed', 'exhaustFromDeck')) return 'draw'
  // コスト再利用 (黒): 死者再生・屍集めはカードアドバンテージ系としてドロー枠で運用する
  if (has('retrieveFromExhaust', 'playFromExhaust')) return 'draw'
  if (has('gainBlock', 'gainIceBlock', 'gainIceBlockPerCardPlayed', 'gainHp', 'weakenEnemy')) return 'defend'
  return 'other'
}

const PLAY_PRIORITY: readonly BotRole[] = [
  'ramp',
  'draw', // ドローは先に (ストームの詠唱数も稼げる)
  'permanent', // 置物はエンジンなので早置き
  'growth',
  'bighit', // 旧フィニッシャー枠 (コスト3以上のダメージ札)
  'attack',
  'defend',
]
const TURN_LIMIT = 50 // 無限戦闘の保険。超えたら敗北扱い

/** 成長0での doubleGrowth など、プレイしても無意味・不可能なカードを弾く */
function isWorthPlaying(state: GameState, card: CardInstance): boolean {
  if (card.def.effects.some((e) => e.effect === 'doubleGrowth')) return state.player.growth > 0
  // 成長放出は成長2以上でないと損 (エンジンを空撃ちしない)
  if (card.def.effects.some((e) => e.effect === 'dischargeGrowth')) return state.player.growth >= 2
  // 自傷カードはHPに余裕がないと自殺 (loseHp合計+5のマージン)
  const selfHarm = card.def.effects
    .filter((e) => e.effect === 'loseHp')
    .reduce((a, e) => a + (e.amount ?? 0), 0)
  if (selfHarm > 0 && state.player.hp <= selfHarm + 5) return false
  // 墓地参照は消滅3枚以上でないと空撃ち (ドレイン版も同じ)
  if (
    card.def.effects.some(
      (e) => e.effect === 'dealDamagePerExhaust' || e.effect === 'dealDamageDrainPerExhaust',
    ) &&
    state.player.exhaustPile.length < 3
  ) {
    return false
  }
  // 自傷の換金 (背徳の収穫) は失ったHP5以上でないと空撃ち
  if (
    card.def.effects.some((e) => e.effect === 'dealDamagePerSelfHpLost') &&
    state.player.selfHpLost < 5
  ) {
    return false
  }
  // 屍集め: 消滅置き場が空なら無意味。死者再生: 直接プレイできるコスト2以上のカードがないと損
  if (card.def.effects.some((e) => e.effect === 'retrieveFromExhaust')) {
    return state.player.exhaustPile.length > 0
  }
  if (card.def.effects.some((e) => e.effect === 'playFromExhaust')) {
    return pickDirectPlayTarget(state) !== null
  }
  const exhaustCostN = card.def.exhaustCost ?? 0
  if (exhaustCostN > 0 && state.player.hand.length - 1 < exhaustCostN) return false
  // ブロック変換はブロック4以上、集結・隊列は置物1体以上でないと空撃ち
  if (card.def.effects.some((e) => e.effect === 'dealDamagePerBlock')) return state.player.block >= 4
  if (
    card.def.effects.some(
      (e) => e.effect === 'dealDamagePerPermanent' || e.effect === 'gainBlockPerPermanent',
    )
  ) {
    return state.player.permanents.length >= 1
  }
  // 回復はHPが減っていなければ無意味
  if (
    card.def.effects.every((e) => e.effect === 'gainHp') &&
    state.player.hp >= state.player.maxHp
  ) {
    return false
  }
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

/** 死者再生の直接プレイ対象: 制約 (リアクション・選択式・再利用カード以外) を満たす最高コスト。2E未満しかなければ損なので null */
function pickDirectPlayTarget(state: GameState): CardInstance | null {
  let best: CardInstance | null = null
  for (const c of state.player.exhaustPile) {
    if (c.def.type === 'reaction') continue
    if ((c.def.modes?.length ?? 0) > 0) continue
    if (c.def.effects.some((e) => e.effect === 'playFromExhaust' || e.effect === 'retrieveFromExhaust')) continue
    if (best === null || c.def.cost > best.def.cost) best = c
  }
  return best !== null && best.def.cost >= 2 ? best : null
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
  const exhaustCostN = card.def.exhaustCost ?? 0
  const exhaustUids =
    exhaustCostN > 0
      ? state.player.hand
          .filter((c) => c.uid !== card.uid)
          .slice(-exhaustCostN)
          .map((c) => c.uid)
      : undefined
  // コスト再利用 (黒): 屍集めは最高コスト、死者再生は制約を満たす最高コストを選ぶ
  let retrieveUid: string | undefined
  if (card.def.effects.some((e) => e.effect === 'retrieveFromExhaust')) {
    const best = [...state.player.exhaustPile].sort((a, b) => b.def.cost - a.def.cost)[0]
    retrieveUid = best?.uid
  }
  if (card.def.effects.some((e) => e.effect === 'playFromExhaust')) {
    retrieveUid = pickDirectPlayTarget(state)?.uid
  }
  // 集中砲火: 最低HPの生存敵を対象にする (確定済みルール表「ターゲティング」の単純ボット方針)
  let targetIndex: number | undefined
  let bestHp = Infinity
  for (let i = 0; i < state.enemies.length; i++) {
    const e = state.enemies[i]
    if (e.hp > 0 && e.hp < bestHp) {
      bestHp = e.hp
      targetIndex = i
    }
  }
  return { type: 'PlayCard', cardUid: card.uid, modeIndex, discardUids, exhaustUids, retrieveUid, targetIndex }
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
      (c) => c.def.type === 'reaction' && c.def.cost <= s.player.energy,
    )
    if (reaction) return { type: 'SetCard', cardUid: reaction.uid }
  }

  // hold-manual: 敵ターンにリアクションを切るため、最安リアクション分のエナジーを温存する
  const reactionCosts = s.player.hand
    .filter((c) => c.def.type === 'reaction')
    .map((c) => c.def.cost)
  const reserve =
    s.reactionMode === 'hold-manual' && reactionCosts.length > 0 ? Math.min(...reactionCosts) : 0
  // 攻撃札が手札にあるなら最安攻撃分のエナジーを温存する
  // (ドロー・ミル系エンジンがエナジーを食い尽くして攻撃が一度も飛ばない病の防止。
  //  ストーム温存の一般化: 墓地型で顕在化した 2026-08-25)
  // 蓄積型ペイオフ (詠唱数/消滅数参照) があればその最安コストを、なければ最安攻撃札のコストを温存
  const burstCosts = s.player.hand
    .filter((c) =>
      c.def.effects.some((e) =>
        [
          'dealDamagePerCardPlayed',
          'dealDamagePerExhaust',
          'dealDamageDrainPerExhaust',
          'dealDamagePerSelfHpLost',
        ].includes(e.effect),
      ),
    )
    .map((c) => c.def.cost)
  const attackCosts = s.player.hand
    .filter((c) => isPlayableFromHand(c) && c.def.effects.some(isDamageEffect))
    .map((c) => c.def.cost)
  const payoffReserve =
    burstCosts.length > 0
      ? Math.min(...burstCosts)
      : attackCosts.length > 0
        ? Math.min(...attackCosts)
        : 0
  const spendable = s.player.energy - reserve

  // ダメージ札自身は温存分を使ってよい
  const isPayoff = (c: CardInstance) => c.def.effects.some(isDamageEffect)
  for (const role of PLAY_PRIORITY) {
    let candidates = s.player.hand.filter((c) => {
      const budget = role === 'bighit' || isPayoff(c) ? spendable : spendable - payoffReserve
      return (
        botRole(c.def) === role &&
        isPlayableFromHand(c) &&
        effectiveCost(s, c) <= budget &&
        isWorthPlaying(s, c)
      )
    })
    // 勢い生成付きの攻撃 (突進の助走など) を同ターンの他の攻撃・大技より先に打つ
    if (role === 'bighit' || role === 'attack') {
      const momentumFirst = s.player.hand.filter(
        (c) =>
          c.def.effects.some((e) => e.effect === 'addMomentum') &&
          c.def.effects.some(isDamageEffect) &&
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
const PICK_PRIORITY: readonly BotRole[] = [
  'permanent', // 置物は長いランで最も価値が高い (茨の茂み・賢者の泉など)
  'growth',
  'draw',
  'attack',
  'bighit',
  'defend',
  'ramp',
  'reaction',
]

function chooseReward(run: { rewardOptions: readonly string[] | null }): number {
  const options = run.rewardOptions ?? []
  for (const role of PICK_PRIORITY) {
    const idx = options.findIndex((id) => botRole(getCardDef(id)) === role)
    if (idx >= 0) return idx
  }
  return 0
}

function simulateRuns(count: number, baseSeed: number): void {
  console.error(`# ドラフト連戦 sim: ${count}ラン × ${allLeaders.length}リーダー, baseSeed=${baseSeed} (ピックはカテゴリ優先の単純方針)`)
  console.log('leader,runs,cleared,clearRate,avgBattlesCleared,avgFinalDeckSize')
  for (const leader of allLeaders) {
    const deathsByBattle = new Array<number>(RUN_BATTLES).fill(0)
    let cleared = 0
    let totalBattlesCleared = 0
    let totalDeckSize = 0
    for (let i = 0; i < count; i++) {
      let run = createRun((baseSeed + i) >>> 0, 'set-confirm', leader.id)
      let aborted = false
      let actions = 0
      while (
        run.phase === 'combat' ||
        run.phase === 'reward' ||
        run.phase === 'offer' ||
        run.phase === 'relic-reward'
      ) {
        if (++actions > 30000) { aborted = true; break } // ラン全体の行動数セーフガード
        if (run.phase === 'offer') {
          // エリート挑戦ポリシー: HP60%以上なら挑む (docs/relics-design.md)
          run = applyRunCommand(run, {
            type: 'ChooseElite',
            elite: run.hp >= run.maxHp * 0.6,
          })
          continue
        }
        if (run.phase === 'relic-reward') {
          run = applyRunCommand(run, { type: 'PickRelic', index: 0 })
          continue
        }
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
        leader.id,
        count,
        cleared,
        (cleared / count).toFixed(3),
        (totalBattlesCleared / count).toFixed(2),
        (totalDeckSize / count).toFixed(1),
      ].join(','),
    )
    console.error(
      `# ${leader.id} 敗北した戦闘の分布: ` +
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

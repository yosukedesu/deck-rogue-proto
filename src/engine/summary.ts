// engine/summary.ts — 撃破サマリー (2026-08-29 面白さ5への処方③: ピーク体験)。
// eventLog の純関数集計なので engine に置く (UI/CLI が共用。DOM依存なし)。
// 「俺の戦いだった」を1行で見せる: 最大ターン火力・読み勝ち・完全に凌いだ回数。

import type { GameEvent, EnemyDef, RelicDef } from './types.ts'

export interface BattleSummary {
  /** かかったターン数 */
  readonly turns: number
  /** プレイヤーの総与ダメージ */
  readonly totalDealt: number
  /** 1ターンの最大与ダメージ (多段・複数枚の合算 = ぶん回りの記録) */
  readonly bestTurnDealt: number
  /** 失ったHPの合計 (敵の攻撃によるもの) */
  readonly hpLost: number
  /** リアクション発動回数 (読み勝ちの回数) */
  readonly reactionsFired: number
  /** 敵の攻撃を完全に防いだ回数 (被弾予定があったのにHP損失0) */
  readonly perfectBlocks: number
  /** 打ち消した敵行動の数 */
  readonly negates: number
}

export function battleSummary(log: readonly GameEvent[]): BattleSummary {
  let turns = 0
  let totalDealt = 0
  let bestTurnDealt = 0
  let currentTurnDealt = 0
  let hpLost = 0
  let reactionsFired = 0
  let perfectBlocks = 0
  let negates = 0
  for (const e of log) {
    switch (e.type) {
      case 'TurnStarted':
        turns = Math.max(turns, e.turn)
        bestTurnDealt = Math.max(bestTurnDealt, currentTurnDealt)
        currentTurnDealt = 0
        break
      case 'DamageDealt':
        if (e.source === 'player') {
          totalDealt += e.amount
          currentTurnDealt += e.amount
        } else {
          hpLost += e.hpLoss
          if (e.amount > 0 && e.hpLoss === 0) perfectBlocks++
        }
        break
      case 'ThornsReflected':
        // とげ反射も「受けたダメージ」に数える (2026-08-30 計測ランで発覚: 針毛の栗鼠戦で
        // 実際は9減っているのに「被ダメ1」と表示されていた = サマリーが嘘をついていた)
        hpLost += e.hpLoss
        break
      case 'BurnTick':
        // 延焼ティックもプレイヤーの与ダメージ (2026-08-31 赤バーン縛りランで発覚:
        // うねる獣59HPを倒して「総与ダメ27」= バーン型では表示が実ダメの半分以下だった。
        // ThornsReflected と同じ「サマリーが嘘をつく」穴)。敵フェーズ中のティックは
        // 直前の自ターンの投資なので、最大ターン火力 (currentTurnDealt) にも算入する
        totalDealt += e.amount
        currentTurnDealt += e.amount
        break
      case 'ReactionTriggered':
        reactionsFired++
        break
      case 'ActionNegated':
        negates++
        break
      default:
        break
    }
  }
  bestTurnDealt = Math.max(bestTurnDealt, currentTurnDealt)
  return { turns, totalDealt, bestTurnDealt, hpLost, reactionsFired, perfectBlocks, negates }
}

/** サマリーの1行表示 (UI/CLI共用の文言) */
export function summaryLine(s: BattleSummary): string {
  const parts = [
    `${s.turns}ターン`,
    `総与ダメ${s.totalDealt}${s.bestTurnDealt > 0 ? `（最大ターン${s.bestTurnDealt}）` : ''}`,
    `被ダメ${s.hpLost}`,
    s.reactionsFired > 0 ? `読み勝ち${s.reactionsFired}回` : '',
    s.perfectBlocks > 0 ? `敵の攻撃${s.perfectBlocks}回を完全に凌いだ` : '',
    s.negates > 0 ? `打ち消し${s.negates}回` : '',
  ]
  return parts.filter(Boolean).join(' / ')
}

// ---- カード表示のラベル (UI と CLI で1つの真実を共有する純関数) ----

/**
 * カードのコスト表記。**Xコスト札は `cost` フィールド (=1) でなく "X" と出す**
 * (2026-08-29 バグ修正: UI側に xCost の分岐が1つも無く、ピック画面・ショップ・手札・デッキ一覧の
 * すべてで X札が「1マナ」と表示されていた。CLIだけが独自に対応していたので共有関数に一本化した)。
 * discounted は「次のカード-N」適用後の実効コスト (素と違う時だけ渡す)。
 */
export function cardCostLabel(def: { cost: number; xCost?: boolean }, discounted?: number): string {
  if (def.xCost === true) return 'X' // 割引はXコストに効かない (確定済みルール表「Xコスト」)
  return String(discounted ?? def.cost)
}

/**
 * Xコスト札のヒット表記。xHits の効果は支払ったXの回数だけ繰り返される。
 * 表示に出さないと「1マナで7ダメージ」に見えてカードの正体が伝わらない。
 * 成長・勢いの注記はダメージ効果だけに付ける (2026-09-01 検証ラン指摘: 樹皮の重鎧=Xブロックに
 * 旧文言がそのまま出て「成長がブロックに乗る」と読め、防御計算を誤らせていた)
 */
export function xHitsSuffix(e: { xHits?: boolean; effect?: string }): string {
  if (e.xHits !== true) return ''
  return e.effect !== undefined && e.effect.startsWith('dealDamage')
    ? '×Xヒット(各ヒットに成長・勢いが乗る)'
    : '×Xヒット'
}


// ---- 最悪被ダメ予測 (2026-09-02 レビュー是正: UIフッター・💀致死級バッジ・CLIで式が
// 3通りに割れていたのを1本化。合成順は実処理 combat.ts の攻撃解決と同一 = 鈴→脆弱→重り) ----
import { effectiveIntent } from './effects.ts'
import { getEnemyDef as getEnemyDefForSummary } from './content.ts'
import type { GameState } from './types.ts'

/** 敵1体の「今フェーズの最悪合計ダメージ」。攻撃以外・死亡・混乱 (仲間に向かう) は0 */
export function worstIncomingFrom(s: GameState, enemyIndex: number): number {
  const e = s.enemies[enemyIndex]
  if (!e || e.hp <= 0 || e.confusion > 0) return 0
  const it = effectiveIntent(s, enemyIndex)
  if (it?.kind !== 'attack') return 0
  let perHit = it.shownMax
  // 静かな鈴 (C型): 伏せ札がある間、各ヒット-N (最低1)
  if ((s.setDamageReduction ?? 0) > 0 && s.player.setCards.length > 0) {
    perHit = Math.max(1, perHit - (s.setDamageReduction ?? 0))
  }
  // 脆弱: +50% (切り捨て)
  if (s.player.vulnerable > 0) perHit = Math.floor(perHit * 1.5)
  // 重り: +10%×このターンの実プレイ枚数 (切り捨て)
  if ((s.player.slow ?? 0) > 0 && (s.player.playsThisTurn ?? 0) > 0) {
    perHit = Math.floor(perHit * (1 + 0.1 * (s.player.playsThisTurn ?? 0)))
  }
  const hits = it.mirrorHits === true ? Math.max(1, s.player.cardsPlayedThisTurn + (s.player.setsThisTurn ?? 0)) : (it.hits ?? 1)
  return perHit * hits
}

/** 全敵の最悪合計 (最悪被ダメ予測の分子) */
export function worstIncomingTotal(s: GameState): number {
  return s.enemies.reduce((sum, _e, i) => sum + worstIncomingFrom(s, i), 0)
}

/**
 * 孵化までの残り手数 (2026-09-02 検証ラン「カウントダウンが無い」への処方)。
 * 0=宣言済みの意図が孵化 (このフェーズで孵化する)・N=あとN回の宣言で孵化・null=孵化を持たない。
 * patternOffset で卵ごとに非対称になる = この敵の一番面白い部分を常時可視化する
 */
export function turnsUntilHatch(s: GameState, enemyIndex: number): number | null {
  const e = s.enemies[enemyIndex]
  if (!e || e.hp <= 0) return null
  const def = getEnemyDefForSummary(e.enemyId)
  if (def.hatchInto === undefined || def.sequence === undefined) return null
  if (e.intent?.kind === 'hatch') return 0
  const len = def.sequence.length
  const loopFrom = def.sequenceLoopFrom ?? 0
  const idxAt = (k: number): number => (k < len ? k : loopFrom + ((k - loopFrom) % (len - loopFrom)))
  for (let d = 0; d < len + 2; d++) {
    const moveId = def.sequence[idxAt(e.patternIndex + d)]
    if (def.moves.find((m) => m.id === moveId)?.kind === 'hatch') return d + 1
  }
  return null
}

/**
 * 伏せ分岐の「型」の注記 (2026-09-03 Opusラン F 指摘: 探り屋の分岐がターンごとに向きが反転して見え、
 * 「伏せると殴られる敵」と1回で誤学習する)。反応テーブル (movesVsSet) が重み抽選で、素の行動が固定ローテの敵は
 * 「伏せを見ると順番を崩す＝どちらが出るかは毎ターン変わる」を予告に添える。
 * setAlt (行動単位の分岐) の敵は向きが固定なので注記しない。表示専用の純関数 (CLI/UI共用)
 */
export function setBranchNote(def: EnemyDef): string | null {
  if (!def.sequence || def.sequence.length === 0) return null
  if (!def.movesVsSet || def.movesVsSet.length < 2) return null
  return '順番を崩す=向きは毎ターン変わる'
}

/** レリックの層の表示タグ (2026-09-03 本家式の層。CLI/UI共用。common は無印) */
export function relicRarityTag(def: RelicDef): string {
  switch (def.rarity ?? 'common') {
    case 'uncommon': return '◆アンコモン'
    case 'rare': return '★レア'
    case 'boss': return '👑ボス'
    case 'shop': return '🛒店売り'
    case 'event': return '❓イベント'
    default: return ''
  }
}

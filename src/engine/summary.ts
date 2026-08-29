// engine/summary.ts — 撃破サマリー (2026-08-29 面白さ5への処方③: ピーク体験)。
// eventLog の純関数集計なので engine に置く (UI/CLI が共用。DOM依存なし)。
// 「俺の戦いだった」を1行で見せる: 最大ターン火力・読み勝ち・完全に凌いだ回数。

import type { GameEvent } from './types.ts'

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
    s.perfectBlocks > 0 ? `完全に凌いだ${s.perfectBlocks}回` : '',
    s.negates > 0 ? `打ち消し${s.negates}回` : '',
  ]
  return parts.filter(Boolean).join(' / ')
}

// engine/reactions/set-auto.ts — 方式1: セット式
// コスト事前払いで伏せる。条件成立で自動発動 (プレイヤーの判断は挟まらない)。
// pre窓 (行動実行前: 打ち消し・軽減) と post窓 (行動解決後: 返し系) の両方で自動発動する。

import { preWindowFor, reactionActionValue, usableSetCards } from '../effects.ts'
import type { ReactionWindow } from '../effects.ts'
import type { Command, GameEvent, GameState, ReactionSystem } from '../types.ts'
import { emitWhiffForRemainingSet, fireSetCard, setCard } from './set-base.ts'

export const setAutoSystem: ReactionSystem = {
  mode: 'set-auto',

  canHandle(_state: GameState, command: Command): boolean {
    // 型で受けて setCard 側の具体的なエラーを出す (set-confirm と同じ裁定。2026-08-29)
    return command.type === 'SetCard'
  },

  handleCommand(state: GameState, command: Command): GameState {
    if (command.type !== 'SetCard') throw new Error(`set-auto が処理できないコマンド: ${command.type}`)
    return setCard(state, command.cardUid)
  },

  onEvent(state: GameState, event: GameEvent): GameState {
    switch (event.type) {
      case 'EnemyActionExecuting': {
        if (state.reactionUsedThisAction) return state // 敵の1行動につき1回まで
        // 打ち消し済み (楔 actionNegated・全体の negateNextAction) の行動には窓を開かない (2026-09-20 Opus 火種A: 起きない行動に護りの灯印を切らせていた)
        if (state.enemies[event.enemyIndex]?.actionNegated === true || state.negateNextAction === true) return state
        // 窓の値は攻撃なら1発×ヒット数の合計・攻撃者が混乱中なら被攻撃前の罠は候補にしない (2026-09-24 E10・E3)
        const win: ReactionWindow = { ...preWindowFor(state, event.enemyIndex), kind: event.kind }
        const card = usableSetCards(state, win)[0]
        if (card) {
          return fireSetCard(state, card, event.enemyIndex) // 条件成立 → 先頭の合致札を即自動発動
        }
        return state
      }
      case 'EnemyActionResolved': {
        // 窓ごとに1枚 (2026-09-14): pre 窓で鳴っても post 窓は開く
        const win: ReactionWindow = { stage: 'post', kind: event.kind, hpLoss: event.hpLoss, actual: reactionActionValue(state, event.enemyIndex) }
        const card = usableSetCards(state, win)[0]
        if (card) {
          return fireSetCard(state, card, event.enemyIndex)
        }
        return state
      }
      case 'EnemyPhaseEnded':
        return emitWhiffForRemainingSet(state)
      default:
        return state
    }
  },
}

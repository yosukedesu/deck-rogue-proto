// engine/reactions/set-auto.ts — 方式1: セット式
// コスト事前払いで伏せる。条件成立で自動発動 (プレイヤーの判断は挟まらない)。
// pre窓 (行動実行前: 打ち消し・軽減) と post窓 (行動解決後: 返し系) の両方で自動発動する。

import { effectiveIntent, reactionMatches } from '../effects.ts'
import type { Command, GameEvent, GameState, ReactionSystem } from '../types.ts'
import { canSetCard, emitWhiffForRemainingSet, fireSetCard, setCard } from './set-base.ts'

export const setAutoSystem: ReactionSystem = {
  mode: 'set-auto',

  canHandle(state: GameState, command: Command): boolean {
    return command.type === 'SetCard' && canSetCard(state, command.cardUid)
  },

  handleCommand(state: GameState, command: Command): GameState {
    if (command.type !== 'SetCard') throw new Error(`set-auto が処理できないコマンド: ${command.type}`)
    return setCard(state, command.cardUid)
  },

  onEvent(state: GameState, event: GameEvent): GameState {
    switch (event.type) {
      case 'EnemyActionExecuting': {
        if (state.reactionUsedThisAction) return state // 敵の1行動につき1回まで
        const actual = effectiveIntent(state, event.enemyIndex)?.actual ?? 0
        const win = { stage: 'pre', kind: event.kind, actual } as const
        const card = state.player.setCards.find((c) => reactionMatches(state, c, win))
        if (card) {
          return fireSetCard(state, card, event.enemyIndex) // 条件成立 → 先頭の合致札を即自動発動
        }
        return state
      }
      case 'EnemyActionResolved': {
        if (state.reactionUsedThisAction) return state
        const win = { stage: 'post', kind: event.kind, hpLoss: event.hpLoss } as const
        const card = state.player.setCards.find((c) => reactionMatches(state, c, win))
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

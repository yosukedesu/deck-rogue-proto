// engine/reactions/set-auto.ts — 方式1: セット式
// コスト事前払いで伏せる。条件成立で自動発動 (プレイヤーの判断は挟まらない)。
// pre窓 (行動実行前: 打ち消し・軽減) と post窓 (行動解決後: 返し系) の両方で自動発動する。

import { reactionMatches } from '../effects.ts'
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
        const card = state.player.setCards[0]
        const actual = state.enemies[event.enemyIndex]?.intent?.actual ?? 0
        if (card && reactionMatches(state, card, { stage: 'pre', kind: event.kind, actual })) {
          return fireSetCard(state, card, event.enemyIndex) // 条件成立 → 即自動発動
        }
        return state
      }
      case 'EnemyActionResolved': {
        const card = state.player.setCards[0]
        if (
          card &&
          reactionMatches(state, card, { stage: 'post', kind: event.kind, hpLoss: event.hpLoss })
        ) {
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

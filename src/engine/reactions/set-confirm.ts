// engine/reactions/set-confirm.ts — 方式3: ハイブリッド (採用方式)
// コスト事前払いで伏せる。条件成立時に「発動/温存」の確認だけ入る。
// pre窓 (行動確定時・実行前: 打ち消し・軽減) と post窓 (行動解決後: 返し系) の両方で確認が入る。
// 温存した伏せは場に残り続ける → 伏せ警戒型へのブラフが意図的に打てる。

import { reactionMatches } from '../effects.ts'
import type { Command, GameEvent, GameState, ReactionSystem } from '../types.ts'
import { canSetCard, emitWhiffForRemainingSet, fireSetCard, setCard } from './set-base.ts'

export const setConfirmSystem: ReactionSystem = {
  mode: 'set-confirm',

  canHandle(state: GameState, command: Command): boolean {
    switch (command.type) {
      case 'SetCard':
        return canSetCard(state, command.cardUid)
      case 'ConfirmReaction':
        return state.phase === 'awaiting-reaction'
      default:
        return false
    }
  },

  handleCommand(state: GameState, command: Command): GameState {
    switch (command.type) {
      case 'SetCard':
        return setCard(state, command.cardUid)
      case 'ConfirmReaction': {
        if (state.phase !== 'awaiting-reaction' || !state.pendingWindow) {
          throw new Error('確認待ちではないのに ConfirmReaction が来た')
        }
        if (!command.fire) return state // 温存: 伏せたまま。敵の行動処理はそのまま進む
        const card = state.player.setCards[0]
        if (!card) throw new Error('伏せカードがないのに発動確認が来た')
        return fireSetCard(state, card, state.pendingWindow.enemyIndex)
      }
      default:
        throw new Error(`set-confirm が処理できないコマンド: ${command.type}`)
    }
  },

  onEvent(state: GameState, event: GameEvent): GameState {
    switch (event.type) {
      case 'EnemyActionExecuting': {
        const card = state.player.setCards[0]
        const actual = state.enemies[event.enemyIndex]?.intent?.actual ?? 0
        if (card && reactionMatches(state, card, { stage: 'pre', kind: event.kind, actual })) {
          return {
            ...state,
            phase: 'awaiting-reaction',
            pendingWindow: { enemyIndex: event.enemyIndex, stage: 'pre' },
          }
        }
        return state
      }
      case 'EnemyActionResolved': {
        const card = state.player.setCards[0]
        if (
          card &&
          reactionMatches(state, card, { stage: 'post', kind: event.kind, hpLoss: event.hpLoss })
        ) {
          return {
            ...state,
            phase: 'awaiting-reaction',
            pendingWindow: { enemyIndex: event.enemyIndex, stage: 'post' },
          }
        }
        return state
      }
      case 'EnemyPhaseEnded':
        // 温存も「そのターン発動しなかった伏せ」として空振り計上する (統計の定義を3方式で揃える)
        return emitWhiffForRemainingSet(state)
      default:
        return state
    }
  },
}

// engine/reactions/hold-manual.ts — 方式2: 構え式 (比較記録用に残置)
// 伏せない。敵の行動ごとに手札から手動発動の機会 (割り込みウィンドウ) がある。
// pre窓 (行動確定時・実行前) と post窓 (行動解決後) の両方が開きうる。
// コストは発動時に支払う。余剰エナジーは敵ターンに持ち越し
// (エンジンはターン終了時にエナジーを消さないので、この方式では自然に残額が使える)。

import {
  canSaveFromLethal,
  effectiveIntent,
  reactionMatches,
  resolveReactionEffects,
  windowFromPending,
} from '../effects.ts'
import { emit } from '../events.ts'
import type { CardInstance, Command, GameEvent, GameState, ReactionSystem } from '../types.ts'
import type { ReactionWindow } from '../effects.ts'

/** いま開いているウィンドウ (敵の行動) に対して手札から発動できるリアクション一覧 */
export function playableReactions(state: GameState): readonly CardInstance[] {
  if (state.phase !== 'awaiting-reaction') return []
  const win = windowFromPending(state)
  if (!win) return []
  return state.player.hand.filter(
    (c) =>
      c.def.type === 'reaction' &&
      reactionMatches(state, c, win) &&
      c.def.cost <= state.player.energy &&
      // 致死状態では回復を伴う札だけが生存の可能性を持つ
      (state.player.hp > 0 || canSaveFromLethal(c)),
  )
}

/** この誘発窓で手札から発動できるリアクションがあるか */
function anyPlayable(state: GameState, win: ReactionWindow): boolean {
  return state.player.hand.some(
    (c) =>
      c.def.type === 'reaction' &&
      reactionMatches(state, c, win) &&
      c.def.cost <= state.player.energy &&
      (state.player.hp > 0 || canSaveFromLethal(c)),
  )
}

export const holdManualSystem: ReactionSystem = {
  mode: 'hold-manual',

  canHandle(state: GameState, command: Command): boolean {
    switch (command.type) {
      case 'ReactManual':
        return playableReactions(state).some((c) => c.uid === command.cardUid)
      case 'ConfirmReaction':
        // fire: false をパス (発動しない) として受け付ける
        return state.phase === 'awaiting-reaction' && !command.fire
      default:
        return false
    }
  },

  handleCommand(state: GameState, command: Command): GameState {
    switch (command.type) {
      case 'ReactManual': {
        if (state.phase !== 'awaiting-reaction' || !state.pendingWindow) {
          throw new Error('割り込みウィンドウ外で ReactManual が来た')
        }
        const card = playableReactions(state).find((c) => c.uid === command.cardUid)
        if (!card) throw new Error(`発動できないカード: ${command.cardUid}`)
        // 発動時にコストを支払い、手札から捨て札へ。1行動1回の消費フラグも立てる
        let s: GameState = {
          ...state,
          reactionUsedThisAction: true,
          player: {
            ...state.player,
            energy: state.player.energy - card.def.cost,
            hand: state.player.hand.filter((c) => c.uid !== card.uid),
            discardPile: [...state.player.discardPile, card],
          },
        }
        s = resolveReactionEffects(s, card, state.pendingWindow.enemyIndex)
        return s
      }
      case 'ConfirmReaction': {
        if (state.phase !== 'awaiting-reaction') throw new Error('割り込みウィンドウ外でパスが来た')
        if (command.fire) throw new Error('hold-manual の発動は ReactManual でカードを指定する')
        return state // パス: 何もしない。敵の行動処理はそのまま進む
      }
      default:
        throw new Error(`hold-manual が処理できないコマンド: ${command.type}`)
    }
  },

  onEvent(state: GameState, event: GameEvent): GameState {
    switch (event.type) {
      case 'EnemyActionExecuting': {
        if (state.reactionUsedThisAction) return state // 敵の1行動につき1回まで
        const actual = effectiveIntent(state, event.enemyIndex)?.actual ?? 0
        if (anyPlayable(state, { stage: 'pre', kind: event.kind, actual })) {
          return {
            ...state,
            phase: 'awaiting-reaction',
            pendingWindow: { enemyIndex: event.enemyIndex, stage: 'pre' },
          }
        }
        return state
      }
      case 'EnemyActionResolved': {
        if (state.reactionUsedThisAction) return state // pre窓で発動済みなら post窓は開かない
        if (anyPlayable(state, { stage: 'post', kind: event.kind, hpLoss: event.hpLoss })) {
          return {
            ...state,
            phase: 'awaiting-reaction',
            pendingWindow: { enemyIndex: event.enemyIndex, stage: 'post' },
          }
        }
        return state
      }
      case 'EnemyPhaseEnded': {
        // 空振り計上: 敵ターンを終えて使われず捨てられていくリアクションカード
        let s = state
        for (const card of state.player.hand) {
          if (card.def.type === 'reaction') {
            s = emit(s, { type: 'ReactionWhiffed', cardId: card.def.id })
          }
        }
        return s
      }
      default:
        return state
    }
  },
}

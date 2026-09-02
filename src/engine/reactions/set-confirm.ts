// engine/reactions/set-confirm.ts — 方式3: ハイブリッド (採用方式)
// コスト事前払いで伏せる。条件成立時に「発動/温存」の確認だけ入る。
// pre窓 (行動確定時・実行前: 打ち消し・軽減) と post窓 (行動解決後: 返し系) の両方で確認が入る。
// 温存した伏せは場に残り続ける → 伏せ警戒型へのブラフが意図的に打てる。

import { effectiveIntent, unaffordableSetCards, usableSetCards, windowFromPending } from '../effects.ts'
import { setFireCost } from '../setany.ts'
import type { ReactionWindow } from '../effects.ts'

/** 全カード伏せ可 (実験): 合致したのにエナジー不足で窓が開かなかった伏せ札を記録する (Opusラン E: 何も出ず事故と区別できない) */
function noteUnaffordable(state: GameState, win: ReactionWindow): GameState {
  let s = state
  for (const c of unaffordableSetCards(state, win)) {
    s = emit(s, { type: 'ReactionUnaffordable', cardId: c.def.id, cost: setFireCost(c), energy: state.player.energy })
  }
  return s
}
import { emit } from '../events.ts'
import type { Command, GameEvent, GameState, ReactionSystem } from '../types.ts'
import { emitWhiffForRemainingSet, fireSetCard, setCard } from './set-base.ts'

export const setConfirmSystem: ReactionSystem = {
  mode: 'set-confirm',

  canHandle(state: GameState, command: Command): boolean {
    switch (command.type) {
      case 'SetCard':
        // 型で受けて setCard 側の具体的なエラー (エナジー不足・枠上限等) を出す。
        // canSetCard で弾くと「方式が受け付けないコマンド」という誤解を招く汎用文言に化ける
        // (2026-08-29 白解凍ランで再現。seed301 の未再現エラー報告と同根)
        return true
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
        // 伏せ2枚 (かすみ): 窓に合致する伏せから発動する1枚を選ぶ。cardUid 省略時は先頭の合致札
        const win = windowFromPending(state)
        const candidates = win ? usableSetCards(state, win) : []
        if (!command.fire) {
          // 温存: 伏せたまま。敵の行動処理はそのまま進む。
          // 判断そのものが set-confirm の主題なので記録する (2026-08-26)
          return emit(state, {
            type: 'ReactionHeld',
            enemyIndex: state.pendingWindow.enemyIndex,
            stage: state.pendingWindow.stage,
            kind: win?.kind ?? 'attack',
            value: win ? (win.stage === 'pre' ? win.actual : win.hpLoss) : 0,
            candidateIds: candidates.map((c) => c.def.id),
          })
        }
        const card =
          command.cardUid !== undefined
            ? candidates.find((c) => c.uid === command.cardUid)
            : candidates[0]
        if (!card) throw new Error('この窓で発動できる伏せカードがない')
        return fireSetCard(state, card, state.pendingWindow.enemyIndex)
      }
      default:
        throw new Error(`set-confirm が処理できないコマンド: ${command.type}`)
    }
  },

  onEvent(state: GameState, event: GameEvent): GameState {
    switch (event.type) {
      case 'EnemyActionExecuting': {
        if (state.reactionUsedThisAction) return state // 敵の1行動につき1回まで
        if (event.kind === 'rest') return state // 隙 (何もしない) に確認を挟まない (2026-08-30 ノイズ指摘)
        const actual = effectiveIntent(state, event.enemyIndex)?.actual ?? 0
        const win = { stage: 'pre', kind: event.kind, actual } as const
        if (usableSetCards(state, win).length > 0) {
          return {
            ...state,
            phase: 'awaiting-reaction',
            pendingWindow: { enemyIndex: event.enemyIndex, stage: 'pre' },
          }
        }
        return noteUnaffordable(state, win)
      }
      case 'EnemyActionResolved': {
        if (state.reactionUsedThisAction) return state // pre窓で発動済みなら post窓は開かない
        const win = { stage: 'post', kind: event.kind, hpLoss: event.hpLoss, actual: event.actual } as const
        if (usableSetCards(state, win).length > 0) {
          return {
            ...state,
            phase: 'awaiting-reaction',
            pendingWindow: { enemyIndex: event.enemyIndex, stage: 'post' },
          }
        }
        return noteUnaffordable(state, win)
      }
      case 'EnemyPhaseEnded':
        // 温存も「そのターン発動しなかった伏せ」として空振り計上する (統計の定義を3方式で揃える)
        return emitWhiffForRemainingSet(state)
      default:
        return state
    }
  },
}

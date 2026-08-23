// engine/reactions/set-base.ts — set-auto / set-confirm が共有する「伏せる」処理
// (hold-manual は伏せないので使わない)

import { emit } from '../events.ts'
import { resolveReactionEffects } from '../effects.ts'
import type { CardInstance, GameState } from '../types.ts'

/** SetCard の可否判定 (UI のボタン活性にも使う) */
export function canSetCard(state: GameState, cardUid: string): boolean {
  if (state.phase !== 'player-turn') return false
  if (state.player.setCards.length >= 1) return false // 伏せ枚数は同時1枚 (確定済みルール)
  const card = state.player.hand.find((c) => c.uid === cardUid)
  if (!card) return false
  if (card.def.category !== 'reaction') return false // 伏せ対象は reaction カテゴリのみ
  return card.def.cost <= state.player.energy
}

/** SetCard: コスト事前払いで手札から伏せる */
export function setCard(state: GameState, cardUid: string): GameState {
  if (state.phase !== 'player-turn') throw new Error('自ターン以外は伏せられない')
  if (state.player.setCards.length >= 1) throw new Error('伏せは同時1枚まで')
  const card = state.player.hand.find((c) => c.uid === cardUid)
  if (!card) throw new Error(`手札にないカード: ${cardUid}`)
  if (card.def.category !== 'reaction') throw new Error(`${card.def.name} は伏せられない (reactionのみ)`)
  if (card.def.cost > state.player.energy) throw new Error(`エナジー不足: ${card.def.name}`)
  const s: GameState = {
    ...state,
    player: {
      ...state.player,
      energy: state.player.energy - card.def.cost,
      hand: state.player.hand.filter((c) => c.uid !== cardUid),
      setCards: [...state.player.setCards, card],
    },
  }
  return emit(s, { type: 'CardSet', cardId: card.def.id })
}

/** 伏せカードを発動する: 効果解決→伏せ場から捨て札へ (コストは伏せ時に支払い済み) */
export function fireSetCard(state: GameState, card: CardInstance, enemyIndex: number): GameState {
  let s: GameState = {
    ...state,
    player: {
      ...state.player,
      setCards: state.player.setCards.filter((c) => c.uid !== card.uid),
      discardPile: [...state.player.discardPile, card],
    },
  }
  s = resolveReactionEffects(s, card, enemyIndex)
  return s
}

/** 空振り計上: 敵フェーズ終端に伏せが残っていれば、そのターンは発動しなかった (伏せは無期限持続) */
export function emitWhiffForRemainingSet(state: GameState): GameState {
  let s = state
  for (const card of state.player.setCards) {
    s = emit(s, { type: 'ReactionWhiffed', cardId: card.def.id })
  }
  return s
}

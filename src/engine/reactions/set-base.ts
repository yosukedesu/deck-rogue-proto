// engine/reactions/set-base.ts — set-auto / set-confirm が共有する「伏せる」処理
// (hold-manual は伏せないので使わない)

import { emit } from '../events.ts'
import { fireExhaustTriggers, resolveReactionEffects, runPermanentTriggers } from '../effects.ts'
import type { CardInstance, GameState } from '../types.ts'

/** SetCard の可否判定 (UI のボタン活性にも使う) */
export function canSetCard(state: GameState, cardUid: string): boolean {
  if (state.phase !== 'player-turn') return false
  // 伏せ枠は setSlots まで (基本1。かすみ=2。確定済みルール表「伏せ枚数」)
  if (state.player.setCards.length >= state.player.setSlots) return false
  const card = state.player.hand.find((c) => c.uid === cardUid)
  if (!card) return false
  if (card.def.type !== 'reaction') return false // 伏せ対象は reaction タイプのみ
  return (card.freeThisCombat === true ? 0 : card.def.cost) <= state.player.energy
}

/** SetCard: コスト事前払いで手札から伏せる */
export function setCard(state: GameState, cardUid: string): GameState {
  if (state.phase !== 'player-turn') throw new Error('自ターン以外は伏せられない')
  if (state.player.setCards.length >= state.player.setSlots) {
    throw new Error(`伏せは同時${state.player.setSlots}枚まで`)
  }
  const card = state.player.hand.find((c) => c.uid === cardUid)
  if (!card) throw new Error(`手札にないカード: ${cardUid}`)
  if (card.def.type !== 'reaction') throw new Error(`${card.def.name} は伏せられない (リアクションタイプのみ)`)
  // 回収ターンの伏せ直しは0E (2026-08-30。回収1E+伏せ直しコストの二重払いが「常に攻撃2枚に負ける」
  // 死に機構だった実測への処方 = 実質「1Eで伏せ替え」)。屍集めで戻した札 (freeThisCombat) も0E
  const setCost = card.freeThisCombat === true ? 0 : card.def.cost
  const freeReset = state.player.freeResetUid === card.uid
  if (!freeReset && setCost > state.player.energy) throw new Error(`エナジー不足: ${card.def.name}`)
  const s: GameState = {
    ...state,
    player: {
      ...state.player,
      energy: state.player.energy - (freeReset ? 0 : setCost),
      ...(freeReset ? { freeResetUid: undefined } : {}),
      hand: state.player.hand.filter((c) => c.uid !== cardUid),
      setCards: [...state.player.setCards, { ...card, setFresh: true }],
    },
  }
  // 伏せに反応する置物 (レリック: 符師の懐=伏せるたび1ドロー)
  return runPermanentTriggers(
    emit(s, { type: 'CardSet', cardId: card.def.id }),
    'onCardSet',
    Math.max(0, s.enemies.findIndex((e) => e.hp > 0)),
  )
}

/**
 * 回収 (2026-08-30 A2): 1E払って伏せ札を手札に戻す。払った伏せコストは返らない。
 * 読み違いの代償を「枠の固定死」から「1E払って賭け直し」に変える。
 * 逃がしルール (伏せ破壊への応答) の廃止とセット — 破壊されそうな札は事前に自分で引き上げる
 */
export function retrieveSetCard(state: GameState, cardUid: string): GameState {
  if (state.phase !== 'player-turn') throw new Error('回収は自ターンのみ')
  const card = state.player.setCards.find((c) => c.uid === cardUid)
  if (!card) throw new Error(`伏せ場にないカード: ${cardUid}`)
  if (state.player.energy < 1) throw new Error('エナジー不足: 回収には1E必要')
  const restored = { ...card }
  delete (restored as { setFresh?: boolean }).setFresh
  return emit(
    {
      ...state,
      player: {
        ...state.player,
        energy: state.player.energy - 1,
        setCards: state.player.setCards.filter((c) => c.uid !== cardUid),
        hand: [...state.player.hand, restored],
        freeResetUid: cardUid, // このターン中の伏せ直しは0E
      },
    },
    { type: 'SetCardRetrieved', cardId: card.def.id },
  )
}

/** 伏せカードを発動する: 効果解決→伏せ場から捨て札 (消滅札なら消滅置き場) へ。
 * コストは伏せ時に支払い済み。敵の1行動につき1回まで、の消費フラグを立てる
 * (伏せ2枚でも同一行動に2枚は撃てない)。
 * 2026-08-26 修正: 旧実装は常に捨て札へ送っており、リアクション札の exhaust:true が黙殺されていた
 * (毒針の囮)。消滅の誘発 (亡者の合唱など) もこの経路では発火していなかった。 */
export function fireSetCard(state: GameState, card: CardInstance, enemyIndex: number): GameState {
  const exhausts = card.def.exhaust === true
  let s: GameState = {
    ...state,
    reactionUsedThisAction: true,
    player: {
      ...state.player,
      setCards: state.player.setCards.filter((c) => c.uid !== card.uid),
      discardPile: exhausts ? state.player.discardPile : [...state.player.discardPile, card],
      exhaustPile: exhausts ? [...state.player.exhaustPile, card] : state.player.exhaustPile,
    },
  }
  s = resolveReactionEffects(s, card, enemyIndex)
  if (exhausts) {
    s = emit(s, { type: 'CardExhausted', cardId: card.def.id })
    s = fireExhaustTriggers(s, 1, enemyIndex)
  }
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

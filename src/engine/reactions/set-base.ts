// engine/reactions/set-base.ts — set-auto / set-confirm が共有する「伏せる」処理
// (hold-manual は伏せないので使わない)

import { emit } from '../events.ts'
import { fireExhaustTriggers, isTrapLive, resolveReactionEffects, runPermanentTriggers } from '../effects.ts'
import type { CardInstance, GameState } from '../types.ts'
import { SET_ANY_FEE, canSetAsNormal, setFireCost } from '../setany.ts'

/** 伏せる時のコスト: 専用リアクションは印字 (屍集めの0E札は0)、通常カードは実験の固定手数料 */
function setCostOf(card: CardInstance): number {
  if (card.def.type !== 'reaction') return SET_ANY_FEE
  return card.freeThisCombat === true ? 0 : card.def.cost
}
/** この札を伏せ対象にできるか (型の判定。エナジー・枠は別) */
function settableType(state: GameState, card: CardInstance): boolean {
  return card.def.type === 'reaction' || (state.setAnyCards === true && canSetAsNormal(card.def))
}

/** SetCard の可否判定 (UI のボタン活性にも使う) */
export function canSetCard(state: GameState, cardUid: string): boolean {
  if (state.phase !== 'player-turn') return false
  // 伏せ枠は setSlots まで (基本1。かすみ=2。確定済みルール表「伏せ枚数」)
  if (state.player.setCards.length >= state.player.setSlots) return false
  const card = state.player.hand.find((c) => c.uid === cardUid)
  if (!card) return false
  if (!settableType(state, card)) return false // 伏せ対象は reaction タイプ (実験中は通常カードも)
  return setCostOf(card) <= state.player.energy
}

/** SetCard: コスト事前払いで手札から伏せる */
export function setCard(state: GameState, cardUid: string): GameState {
  if (state.phase !== 'player-turn') throw new Error('自ターン以外は伏せられない')
  if (state.player.setCards.length >= state.player.setSlots) {
    throw new Error(`伏せは同時${state.player.setSlots}枚まで`)
  }
  const card = state.player.hand.find((c) => c.uid === cardUid)
  if (!card) throw new Error(`手札にないカード: ${cardUid}`)
  if (!settableType(state, card)) {
    throw new Error(
      state.setAnyCards === true
        ? `${card.def.name} は伏せられない (X・モード・追加コスト・ドロー/マナ系は対象外)`
        : `${card.def.name} は伏せられない (リアクションタイプのみ)`,
    )
  }
  // 屍集めで戻した札 (freeThisCombat) は0E。通常カード (実験) は固定1E
  const setCost = setCostOf(card)
  if (setCost > state.player.energy) throw new Error(`エナジー不足: ${card.def.name}`)
  const s: GameState = {
    ...state,
    player: {
      ...state.player,
      energy: state.player.energy - setCost,
      hand: state.player.hand.filter((c) => c.uid !== cardUid),
      // 罠モデル (2026-09-13): 伏せたターンを記録する。このターンは鳴らない (準備)、翌・翌々ターンの敵フェーズだけ生きる
      setCards: [...state.player.setCards, { ...card, setTurn: state.turn }],
      setsThisTurn: (state.player.setsThisTurn ?? 0) + 1,
    },
  }
  const revealed: GameState = s
  // 伏せに反応する置物 (レリック: 符師の懐=伏せるたび1ドロー)
  return runPermanentTriggers(
    emit(revealed, { type: 'CardSet', cardId: card.def.id }),
    'onCardSet',
    Math.max(0, s.enemies.findIndex((e) => e.hp > 0)),
  )
}

/**
 * 回収 (2026-08-30 A2) は 2026-09-13 罠モデルで廃止: 罠は仕込んだら押し戻せない (期限切れで捨て札に戻る)。
 * 旧セーブ・ジャーナル互換のためコマンド型は残し、常に拒否する
 */
export function retrieveSetCard(_state: GameState, _cardUid: string): GameState {
  throw new Error('回収は廃止された (2026-09-13 罠モデル): 罠は2窓で鳴らなければ期限切れで捨て札に戻る')
}

/** 伏せカードを発動する: 効果解決→伏せ場から捨て札 (消滅札なら消滅置き場) へ。
 * コストは伏せ時に支払い済み。敵の1行動につき1回まで、の消費フラグを立てる
 * (伏せ2枚でも同一行動に2枚は撃てない)。
 * 2026-08-26 修正: 旧実装は常に捨て札へ送っており、リアクション札の exhaust:true が黙殺されていた
 * (毒針の囮)。消滅の誘発 (亡者の合唱など) もこの経路では発火していなかった。 */
export function fireSetCard(state: GameState, card: CardInstance, enemyIndex: number): GameState {
  const exhausts = card.def.exhaust === true
  // 全カード伏せ可 (実験): 通常カードは発動時に印字コストを持ち越しエナジーから払う。足りなければ発動しない
  const fireCost = setFireCost(card)
  if (fireCost > state.player.energy) return state
  let s: GameState = {
    ...state,
    reactionUsedThisAction: true,
    player: {
      ...state.player,
      energy: state.player.energy - fireCost,
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

/** 空振り計上: 敵フェーズ終端に「生きている窓」の罠が残っていれば、そのターンは発動しなかった。
 * 準備ターン (伏せたターン) は窓が原理的に開かないので数えない (2026-09-13 統計の嘘を作らない) */
export function emitWhiffForRemainingSet(state: GameState): GameState {
  let s = state
  for (const card of state.player.setCards) {
    if (!isTrapLive(state, card)) continue
    s = emit(s, { type: 'ReactionWhiffed', cardId: card.def.id })
  }
  return s
}

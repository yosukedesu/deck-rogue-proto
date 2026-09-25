// engine/state.ts — 状態遷移の入口
// GameState はイミュータブル。applyCommand(state, command) => newState の純関数のみで遷移する。
// 方式固有コマンドは ReactionSystem に委譲し、割り込み中断中だった場合は敵フェーズを再開する。
import { retrieveSetCard } from './reactions/set-base.ts'

import { continueAfterWindow, createInitialState, endTurn, playCard, playNecro, resolveScry, startCombat } from './combat.ts'
import { getReactionSystem } from './reactions/index.ts'
import type { CardInstance, Command, GameState } from './types.ts'

export { createInitialState }

export function applyCommand(state: GameState, command: Command): GameState {
  // 占術の保留 (青 2026-09-25): 捨てる札を選ぶまで他のコマンドを受け付けない
  if (state.pendingScry !== undefined && command.type !== 'ResolveScry' && command.type !== 'StartCombat') {
    throw new Error('占術で捨てる札を選んでから (ResolveScry)')
  }
  return settleRetainDiscount(settleScry(applyCommandRaw(state, command)))
}

/** 潮溜まり (青 2026-09-25): 手札を離れた札 (プレイ・捨て・消滅・場) のコスト減を落とす = 安くなるのは手札にある間だけ */
function settleRetainDiscount(state: GameState): GameState {
  const p = state.player
  const strip = (cs: readonly CardInstance[]): readonly CardInstance[] =>
    cs.some((c) => c.retainDiscount !== undefined) ? cs.map((c) => (c.retainDiscount === undefined ? c : (({ retainDiscount: _d, ...rest }) => rest)(c))) : cs
  if (![p.drawPile, p.discardPile, p.exhaustPile, p.permanents, p.setCards].some((cs) => cs.some((c) => c.retainDiscount !== undefined))) return state
  return { ...state, player: { ...p, drawPile: strip(p.drawPile), discardPile: strip(p.discardPile), exhaustPile: strip(p.exhaustPile), permanents: strip(p.permanents), setCards: strip(p.setCards) } }
}

/** 占術の保留の後始末: 山札が空 (見る札が無い)・決着済みなら保留を落とす (空の窓を出さない) */
function settleScry(state: GameState): GameState {
  if (state.pendingScry === undefined) return state
  if (state.player.drawPile.length > 0 && state.phase !== 'won' && state.phase !== 'lost') return state
  const { pendingScry: _p, ...rest } = state
  return rest
}

function applyCommandRaw(state: GameState, command: Command): GameState {
  switch (command.type) {
    case 'ResolveScry':
      return resolveScry(state, command.discardUids)
    case 'StartCombat':
      return startCombat(command.seed, state.reactionMode, command.enemyId, command.deckId, command.leaderId, command.cardIds)
    case 'PlayCard':
      return playCard(state, command.cardUid, command.modeIndex, command.discardUids, command.targetIndex, command.exhaustUids, command.retrieveUid, command.deckUids, command.handUids, command.xAmount, command.permanentUid)
    case 'EndTurn':
      return endTurn(state, command.hearthSparks, command.retainUids)
    case 'RetrieveSetCard':
      return retrieveSetCard(state, command.cardUid)
    case 'PlayNecro':
      return playNecro(state, command.cardUid, command.targetIndex)
    case 'SetCard':
    case 'ReactManual':
    case 'ConfirmReaction': {
      const system = getReactionSystem(state.reactionMode)
      if (!system.canHandle(state, command)) {
        throw new Error(`${state.reactionMode} では受け付けないコマンド: ${command.type}`)
      }
      const wasAwaiting = state.phase === 'awaiting-reaction'
      const next = system.handleCommand(state, command)
      // 割り込み中断中のコマンドだったなら、敵フェーズの続きを解決する
      return wasAwaiting ? continueAfterWindow(next) : next
    }
  }
}

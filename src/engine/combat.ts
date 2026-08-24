// engine/combat.ts — 戦闘フロー (ターン進行・敵フェーズ・意図宣言)
// 敵フェーズは EndTurn コマンド内で同期的に解決する。
// リアクション方式が割り込みを要求した場合のみ 'awaiting-reaction' で中断し、
// state.ts が方式コマンド処理後に continueAfterWindow() で再開する。
// 方式固有の if 分岐をここに書いてはならない (フックは dispatchHooks 経由)。

import { buildDeck, getEnemyDef } from './content.ts'
import {
  drawCards,
  effectiveCost,
  isDamageEffect,
  isPlayableFromHand,
  resolveEffect,
  resolveOnPlayEffects,
} from './effects.ts'
import { buildLeaderPassive, getLeaderDef } from './content.ts'
import { emit } from './events.ts'
import { dispatchHooks, runPermanentTriggers } from './hooks.ts'
import { createRng, nextInt, shuffle, weightedIndex } from './rng.ts'
import type { CardInstance, EnemyDef, EnemyMove, GameState, ReactionMode } from './types.ts'

export const PLAYER_MAX_HP = 50
const BASE_ENERGY = 3
const DRAW_PER_TURN = 5

/** 戦闘前の空状態 (UI/sim が方式を保持するための器)。戦闘は StartCombat で開始する */
export function createInitialState(seed: number, reactionMode: ReactionMode): GameState {
  return {
    rng: createRng(seed),
    reactionMode,
    phase: 'player-turn',
    turn: 0,
    player: {
      hp: PLAYER_MAX_HP,
      maxHp: PLAYER_MAX_HP,
      block: 0,
      energy: BASE_ENERGY,
      energyMax: BASE_ENERGY, // ランプは戦闘ごとにリセット (確定済みルール)
      drawPerTurn: DRAW_PER_TURN,
      hand: [],
      drawPile: [],
      discardPile: [],
      setCards: [],
      permanents: [],
      exhaustPile: [],
      growth: 0, // 成長カウンターは戦闘内のみ (確定済みルール)
      momentum: 0, // 勢いは自ターン終了時リセット
      iceBlock: 0, // 氷壁は戦闘内で持ち越し
      cardsPlayedThisTurn: 0,
      aether: 0, // 霊気は戦闘内持続
      nextCardDiscount: 0,
      impulseUids: [],
    },
    enemies: [],
    pendingWindow: null,
    negateNextAction: false,
    lastAction: null,
    eventLog: [],
  }
}

/** ラン (ドラフト連戦) などが使う戦闘開始オプション */
export interface CombatOptions {
  /** 使用するデッキの実カード列 */
  readonly deck: readonly CardInstance[]
  /** リーダー (パッシブ置物・最大HP・ドロー枚数・エナジー上限の個性を適用) */
  readonly leaderId?: string
  /** 開始時HP (持ち越し用)。省略時は全快 */
  readonly playerHp?: number
  /** 敵HPの倍率 (ランの深度スケーリング用) */
  readonly enemyHpScale?: number
  /** 敵の初期強化 (攻撃の実値・幅表示に加算される) */
  readonly enemyStrength?: number
}

/** 戦闘開始の実体: デッキシャッフル・敵配置をして第1ターンを開始する */
export function startCombatWithOptions(
  seed: number,
  reactionMode: ReactionMode,
  enemyId: string,
  options: CombatOptions,
): GameState {
  const enemyDef = getEnemyDef(enemyId)
  let state = createInitialState(seed, reactionMode)
  // リーダーの個性: 最大HP・ドロー枚数・エナジー上限・パッシブ置物
  const leader = options.leaderId ? getLeaderDef(options.leaderId) : null
  if (leader) {
    state = {
      ...state,
      player: {
        ...state.player,
        maxHp: leader.maxHp,
        hp: leader.maxHp,
        drawPerTurn: leader.drawPerTurn,
        energy: leader.energyMax,
        energyMax: leader.energyMax,
        permanents: [buildLeaderPassive(leader)],
      },
    }
  }
  const [deck, rng] = shuffle(state.rng, options.deck)
  const enemyMaxHp = Math.round(enemyDef.maxHp * (options.enemyHpScale ?? 1))
  state = {
    ...state,
    rng,
    player: {
      ...state.player,
      drawPile: deck,
      hp: Math.min(options.playerHp ?? state.player.maxHp, state.player.maxHp),
    },
    enemies: [
      {
        enemyId,
        hp: enemyMaxHp,
        maxHp: enemyMaxHp,
        block: 0,
        intent: null,
        strength: options.enemyStrength ?? 0,
        burn: 0,
        patternIndex: 0,
      },
    ],
  }
  state = emit(state, { type: 'CombatStarted', enemyId })
  return startPlayerTurn(state, 1)
}

/** StartCombat: プリセットデッキ ID から戦闘を開始する (単発戦闘用) */
export function startCombat(
  seed: number,
  reactionMode: ReactionMode,
  enemyId: string,
  deckId = 'starter',
  leaderId?: string,
): GameState {
  return startCombatWithOptions(seed, reactionMode, enemyId, { deck: buildDeck(deckId), leaderId })
}

/** 敵の行動テーブル選択: プレイヤーに伏せがあれば movesVsSet を優先 (伏せ警戒型・伏せ破壊型) */
export function selectMoveTable(def: EnemyDef, playerHasSetCards: boolean): readonly EnemyMove[] {
  if (playerHasSetCards && def.movesVsSet && def.movesVsSet.length > 0) return def.movesVsSet
  return def.moves
}

/**
 * 全敵の意図を宣言する。行動と実値は宣言時にロールし、実値は非公開 (幅のみ表示)。
 * 伏せの有無は宣言時点の状態で判定する — 直前の自ターンに伏せた札や
 * 空振りで残った札に敵が反応する、というブラフの駆け引きがここで生まれる。
 * - sequence を持つ敵は固定ローテーションで行動 (movesVsSet 割り込み時はローテーションを進めない)
 * - 強化 (strength) は攻撃の実値と幅表示の両方に加算する
 */
function declareIntents(state: GameState): GameState {
  let s = state
  for (let i = 0; i < s.enemies.length; i++) {
    const enemy = s.enemies[i]
    if (enemy.hp <= 0) continue
    const def = getEnemyDef(enemy.enemyId)
    const table = selectMoveTable(def, s.player.setCards.length > 0)
    const usingVsSet = table !== def.moves

    let move
    let rng1 = s.rng
    let nextPatternIndex = enemy.patternIndex
    if (!usingVsSet && def.sequence && def.sequence.length > 0) {
      const moveId = def.sequence[enemy.patternIndex % def.sequence.length]
      move = def.moves.find((m) => m.id === moveId)
      if (!move) throw new Error(`敵 ${def.id} の sequence が未定義の行動を参照: ${moveId}`)
      nextPatternIndex = enemy.patternIndex + 1
    } else {
      const [moveIdx, rngAfter] = weightedIndex(s.rng, table.map((m) => m.weight))
      move = table[moveIdx]
      rng1 = rngAfter
    }

    let actual = 0
    let rng2 = rng1
    if (move.min !== undefined && move.max !== undefined) {
      ;[actual, rng2] = nextInt(rng1, move.min, move.max)
    }
    // 強化は攻撃にのみ乗る (幅表示にも反映して意図表示の誠実さを保つ)。
    // 強化はマイナス値も取れる (ランの序盤に出る「若い個体」)。攻撃は最低1にクランプ
    const bonus = move.kind === 'attack' ? enemy.strength : 0
    const clamp = (v: number) => (move.kind === 'attack' ? Math.max(1, v) : v)
    const intent = {
      kind: move.kind,
      shownMin: clamp((move.min ?? 0) + bonus),
      shownMax: clamp((move.max ?? 0) + bonus),
      actual: clamp(actual + bonus),
    }
    const enemies = s.enemies.map((e, j) =>
      j === i ? { ...e, intent, patternIndex: nextPatternIndex } : e,
    )
    s = emit({ ...s, rng: rng2, enemies }, { type: 'EnemyIntentDeclared', enemyIndex: i, intent })
  }
  return s
}

/** 自ターン開始: ブロック0リセット・エナジー全回復・置物の開始時効果・5枚ドロー・敵意図宣言 */
function startPlayerTurn(state: GameState, turn: number): GameState {
  let s: GameState = {
    ...state,
    turn,
    phase: 'player-turn',
    // 通常ブロックはリセット。氷壁 (iceBlock) は持ち越される
    player: { ...state.player, block: 0, energy: state.player.energyMax, cardsPlayedThisTurn: 0 },
  }
  s = emit(s, { type: 'TurnStarted', turn })
  s = runPermanentTriggers(s, 'onTurnStart', Math.max(0, s.enemies.findIndex((e) => e.hp > 0)))
  s = drawCards(s, s.player.drawPerTurn)
  return declareIntents(s)
}

/** 勝敗判定。すでに決着済みなら何もしない (CombatEnded の二重記録防止) */
export function checkCombatEnd(state: GameState): GameState {
  if (state.phase === 'won' || state.phase === 'lost') return state
  if (state.player.hp <= 0) {
    return emit({ ...state, phase: 'lost' }, { type: 'CombatEnded', result: 'lost' })
  }
  if (state.enemies.every((e) => e.hp <= 0)) {
    return emit({ ...state, phase: 'won' }, { type: 'CombatEnded', result: 'won' })
  }
  return state
}

/**
 * PlayCard: コスト支払い→捨て札 (置物は場、消滅カードは消滅の山) へ→効果解決。
 * - 選択式カード (modes) は modeIndex で選んだモードの効果を解決する
 * - 手札捨てコスト (discardCost) は discardUids で指定した手札を追加コストとして捨てる
 * - 攻撃カテゴリのプレイ後は置物の onAttackPlayed が発火する (そのカード自身には乗らない)
 */
export function playCard(
  state: GameState,
  cardUid: string,
  modeIndex?: number,
  discardUids?: readonly string[],
): GameState {
  if (state.phase !== 'player-turn') throw new Error('自ターン以外はカードをプレイできない')
  const card = state.player.hand.find((c) => c.uid === cardUid)
  if (!card) throw new Error(`手札にないカード: ${cardUid}`)
  if (!isPlayableFromHand(card)) throw new Error(`${card.def.name} はプレイ不可 (リアクション専用)`)
  // マナ軽減トークン適用後の実効コストで支払う (素のコスト0は割引を消費しない)
  const cost = effectiveCost(state, card)
  const consumesDiscount = card.def.cost > 0 && state.player.nextCardDiscount > 0
  if (cost > state.player.energy) throw new Error(`エナジー不足: ${card.def.name}`)

  // 選択式カードの検証
  const modes = card.def.modes ?? []
  let chosenMode = null
  if (modes.length > 0) {
    if (modeIndex === undefined || modes[modeIndex] === undefined) {
      throw new Error(`${card.def.name} は選択式: modeIndex (0〜${modes.length - 1}) が必要`)
    }
    chosenMode = modes[modeIndex]
  }

  // 手札捨てコストの検証
  const discardCost = card.def.discardCost ?? 0
  const discards = discardUids ?? []
  if (discardCost > 0) {
    if (discards.length !== discardCost) {
      throw new Error(`${card.def.name} は追加コストとして手札${discardCost}枚の指定が必要`)
    }
    if (new Set(discards).size !== discards.length || discards.includes(cardUid)) {
      throw new Error('捨てるカードの指定が不正 (重複または自分自身)')
    }
    for (const uid of discards) {
      if (!state.player.hand.some((c) => c.uid === uid)) throw new Error(`手札にないカード: ${uid}`)
    }
  }

  const enemyIndex = state.enemies.findIndex((e) => e.hp > 0)
  const isPermanent = card.def.type === 'permanent'
  const isExhaust = card.def.exhaust === true
  const removed = new Set([cardUid, ...discards])
  const discardedCards = state.player.hand.filter((c) => discards.includes(c.uid))
  let s: GameState = {
    ...state,
    player: {
      ...state.player,
      energy: state.player.energy - cost,
      nextCardDiscount: consumesDiscount ? 0 : state.player.nextCardDiscount,
      hand: state.player.hand.filter((c) => !removed.has(c.uid)),
      discardPile: [
        ...state.player.discardPile,
        ...discardedCards,
        ...(isPermanent || isExhaust ? [] : [card]),
      ],
      permanents: isPermanent ? [...state.player.permanents, card] : state.player.permanents,
      exhaustPile: isExhaust ? [...state.player.exhaustPile, card] : state.player.exhaustPile,
    },
  }
  if (discardedCards.length > 0) {
    s = emit(s, { type: 'CardsDiscarded', cardIds: discardedCards.map((c) => c.def.id) })
  }
  s = emit(s, { type: 'CardPlayed', cardId: card.def.id })
  if (isPermanent) s = emit(s, { type: 'PermanentPlayed', cardId: card.def.id })
  if (isExhaust) s = emit(s, { type: 'CardExhausted', cardId: card.def.id })
  if (chosenMode) {
    for (const effect of chosenMode.effects) {
      s = resolveEffect(s, effect, enemyIndex)
    }
  } else {
    s = resolveOnPlayEffects(s, card, enemyIndex)
  }
  // 「攻撃プレイ後」誘発: 解決した効果にダメージが含まれていたか (物理・呪文を問わない)
  const resolvedEffects = chosenMode ? chosenMode.effects : card.def.effects.filter((e) => e.trigger === 'onPlay')
  if (resolvedEffects.some(isDamageEffect)) s = runPermanentTriggers(s, 'onAttackPlayed', enemyIndex)
  // 詠唱数 (ストーム参照) は効果解決の後に加算する = そのカード自身は数えない
  s = { ...s, player: { ...s.player, cardsPlayedThisTurn: s.player.cardsPlayedThisTurn + 1 } }
  return checkCombatEnd(s)
}

/** EndTurn: 勢いリセット・衝動の失効・延焼処理をして、敵フェーズを解決する */
export function endTurn(state: GameState): GameState {
  if (state.phase !== 'player-turn') throw new Error('自ターン以外はターン終了できない')
  let s = emit(state, { type: 'TurnEnded', turn: state.turn })
  // 勢いは自ターン終了時にリセット (確定済みルール表「勢い」)
  s = { ...s, player: { ...s.player, momentum: 0 } }
  // 衝動 (このターン限りの手札) は未使用なら消滅する
  if (s.player.impulseUids.length > 0) {
    const impulse = new Set(s.player.impulseUids)
    const expired = s.player.hand.filter((c) => impulse.has(c.uid))
    s = {
      ...s,
      player: {
        ...s.player,
        hand: s.player.hand.filter((c) => !impulse.has(c.uid)),
        exhaustPile: [...s.player.exhaustPile, ...expired],
        impulseUids: [],
      },
    }
    for (const card of expired) {
      s = emit(s, { type: 'CardExhausted', cardId: card.def.id })
    }
  }
  // 敵ブロックはこのタイミングで失効 (前の敵ターンの防御は自ターンの攻撃を受け止めたら役目を終える)
  s = { ...s, enemies: s.enemies.map((e) => ({ ...e, block: 0 })) }
  // 延焼: 敵フェーズ開始時にダメージ (ブロック無視) を受けて1減る
  for (let i = 0; i < s.enemies.length; i++) {
    const enemy = s.enemies[i]
    if (enemy.hp <= 0 || enemy.burn <= 0) continue
    const amount = enemy.burn
    s = {
      ...s,
      enemies: s.enemies.map((e, j) =>
        j === i ? { ...e, hp: e.hp - amount, burn: e.burn - 1 } : e,
      ),
    }
    s = emit(s, { type: 'BurnTick', enemyIndex: i, amount })
  }
  s = checkCombatEnd(s) // 行動前に焼き切れば敵は動けない
  if (isOver(s)) return s
  return processEnemyActions(s, 0)
}

const isOver = (s: GameState) => s.phase === 'won' || s.phase === 'lost'

/**
 * 行動解決後の誘発窓 (post窓)。返し系リアクション・置物の茨はここで発動する。
 * 行動が打ち消されていた場合 (lastAction が無い) は開かない。
 */
function postActionStage(state: GameState, enemyIndex: number): GameState {
  const act = state.lastAction
  if (!act || act.enemyIndex !== enemyIndex) return state
  const resolved = {
    type: 'EnemyActionResolved',
    enemyIndex,
    kind: act.kind,
    hpLoss: act.hpLoss,
  } as const
  let s = emit(state, resolved)
  s = dispatchHooks(s, resolved)
  if (s.phase === 'awaiting-reaction') return s // post窓の割り込み → コマンド待ち
  return checkCombatEnd(s)
}

/** 割り込み (awaiting-reaction) から敵フェーズを再開する。state.ts が方式コマンド処理後に呼ぶ */
export function continueAfterWindow(state: GameState): GameState {
  const pending = state.pendingWindow
  if (!pending) throw new Error('割り込み情報がないのに再開が呼ばれた')
  let s: GameState = { ...state, pendingWindow: null, phase: 'player-turn' }
  s = checkCombatEnd(s) // リアクションで決着していれば以降は実行されない
  if (isOver(s)) return s
  if (pending.stage === 'pre') {
    // pre窓の続き: 行動を実行し、解決後の post窓も通す
    s = executeEnemyAction(s, pending.enemyIndex)
    s = checkCombatEnd(s)
    if (isOver(s)) return s
    s = postActionStage(s, pending.enemyIndex)
    if (s.phase === 'awaiting-reaction' || isOver(s)) return s
  }
  // stage 'post' はこの行動について残る処理なし
  return processEnemyActions(s, pending.enemyIndex + 1)
}

/** fromIndex 以降の敵の行動を順に解決する */
function processEnemyActions(state: GameState, fromIndex: number): GameState {
  let s = state
  for (let i = fromIndex; i < s.enemies.length; i++) {
    const enemy = s.enemies[i]
    if (enemy.hp <= 0 || enemy.intent === null) continue
    s = { ...s, lastAction: null }
    // 行動実行の直前フック (pre窓): 打ち消し・軽減リアクションがここで発動/割り込みする
    const executing = { type: 'EnemyActionExecuting', enemyIndex: i, kind: enemy.intent.kind } as const
    s = emit(s, executing)
    s = dispatchHooks(s, executing)
    if (s.phase === 'awaiting-reaction') return s // pre窓の割り込み → コマンド待ち
    s = checkCombatEnd(s)
    if (isOver(s)) return s
    s = executeEnemyAction(s, i)
    s = checkCombatEnd(s)
    if (isOver(s)) return s
    s = postActionStage(s, i)
    if (s.phase === 'awaiting-reaction' || isOver(s)) return s
  }
  return finishEnemyPhase(s)
}

/** 敵1体の宣言済み行動を実行する (打ち消しフラグが立っていれば無効化) */
function executeEnemyAction(state: GameState, enemyIndex: number): GameState {
  const enemy = state.enemies[enemyIndex]
  if (enemy.hp <= 0 || enemy.intent === null) return state
  if (state.negateNextAction) {
    return emit({ ...state, negateNextAction: false }, { type: 'ActionNegated', enemyIndex })
  }
  const intent = enemy.intent
  // 解決した行動を記録する (post窓の誘発判定に使う)
  const markResolved = (s: GameState, hpLoss: number): GameState => ({
    ...s,
    lastAction: { enemyIndex, kind: intent.kind, hpLoss },
  })
  switch (intent.kind) {
    case 'attack': {
      // 通常ブロックを先に消費し、残りを氷壁 (持ち越しブロック) で受ける
      const blocked = Math.min(state.player.block, intent.actual)
      const remaining = intent.actual - blocked
      const iceBlocked = Math.min(state.player.iceBlock, remaining)
      const hpLoss = remaining - iceBlocked
      const s: GameState = {
        ...state,
        player: {
          ...state.player,
          block: state.player.block - blocked,
          iceBlock: state.player.iceBlock - iceBlocked,
          hp: state.player.hp - hpLoss,
        },
      }
      return markResolved(
        emit(s, { type: 'DamageDealt', source: 'enemy', amount: intent.actual, hpLoss }),
        hpLoss,
      )
    }
    case 'defend': {
      const enemies = state.enemies.map((e, i) =>
        i === enemyIndex ? { ...e, block: e.block + intent.actual } : e,
      )
      return markResolved(
        emit({ ...state, enemies }, { type: 'BlockGained', target: 'enemy', amount: intent.actual }),
        0,
      )
    }
    case 'buff': {
      // 強化 (StSの筋力): 以降の攻撃宣言に加算される
      const enemies = state.enemies.map((e, i) =>
        i === enemyIndex ? { ...e, strength: e.strength + intent.actual } : e,
      )
      return markResolved(
        emit({ ...state, enemies }, { type: 'StrengthGained', enemyIndex, amount: intent.actual }),
        0,
      )
    }
    case 'destroy-set': {
      if (state.player.setCards.length === 0) return markResolved(state, 0)
      let s: GameState = {
        ...state,
        player: {
          ...state.player,
          setCards: [],
          discardPile: [...state.player.discardPile, ...state.player.setCards],
        },
      }
      for (const card of state.player.setCards) {
        s = emit(s, { type: 'SetCardDestroyed', cardId: card.def.id })
      }
      return markResolved(s, 0)
    }
  }
}

/** 敵フェーズ終端: 空振り計上フック→手札全捨て (敵ターン後・3方式共通)→次ターン開始 */
function finishEnemyPhase(state: GameState): GameState {
  const ended = { type: 'EnemyPhaseEnded', turn: state.turn } as const
  let s = emit(state, ended)
  s = dispatchHooks(s, ended) // 空振り (ReactionWhiffed) の計上は方式固有
  s = {
    ...s,
    player: {
      ...s.player,
      hand: [],
      discardPile: [...s.player.discardPile, ...s.player.hand],
    },
  }
  return startPlayerTurn(s, s.turn + 1)
}

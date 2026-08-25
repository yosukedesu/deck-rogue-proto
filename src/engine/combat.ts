// engine/combat.ts — 戦闘フロー (ターン進行・敵フェーズ・意図宣言)
// 敵フェーズは EndTurn コマンド内で同期的に解決する。
// リアクション方式が割り込みを要求した場合のみ 'awaiting-reaction' で中断し、
// state.ts が方式コマンド処理後に continueAfterWindow() で再開する。
// 方式固有の if 分岐をここに書いてはならない (フックは dispatchHooks 経由)。

import { buildDeck, getEnemyDef } from './content.ts'
import {
  cardNeedsTarget,
  drawCards,
  effectiveCost,
  fireExhaustTriggers,
  isDamageEffect,
  isPlayableFromHand,
  resolveEffectTargeted,
  resolveOnPlayEffects,
} from './effects.ts'
import { buildLeaderPassive, getLeaderDef, resolveEncounter, WOUND_DEF } from './content.ts'
import { emit } from './events.ts'
import { dispatchHooks, runPermanentTriggers } from './hooks.ts'
import { createRng, nextInt, shuffle, weightedIndex } from './rng.ts'
import type {
  CardInstance,
  EnemyDef,
  EnemyMove,
  GameState,
  ReactionMode,
  StatusInflict,
} from './types.ts'

export const PLAYER_MAX_HP = 75 // StSスケール (2026-08-25 人間基準化)
const BASE_ENERGY = 3
const DRAW_PER_TURN = 5
/** 激昂による強化の累積上限 (確定済みルール表「激昂」) */
const ENRAGE_CAP = 6

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
      weak: 0,
      vulnerable: 0,
      selfHpLost: 0, // カード効果で失ったHPの累計 (背徳の収穫の参照値。戦闘内のみ)
      damageTakenLastEnemyPhase: 0, // 直前の敵フェーズで受けた攻撃ダメージ (逆上の参照値)
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
  /** A型レリックの置物 (buildRelicPermanent で生成。リーダーパッシブと同様に注入される) */
  readonly relicPermanents?: readonly CardInstance[]
}

/** 戦闘開始の実体: デッキシャッフル・敵配置をして第1ターンを開始する */
export function startCombatWithOptions(
  seed: number,
  reactionMode: ReactionMode,
  enemyId: string,
  options: CombatOptions,
): GameState {
  // 敵ID or 編成ID を編成メンバー列に解決 (確定済みルール表「戦闘形式」)
  const members = resolveEncounter(enemyId)
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
  // 群れ補正 (member.hpScale/strength) とランの深度スケーリングは乗算/加算で重なる
  const enemies = members.map((m) => {
    const def = getEnemyDef(m.enemyId)
    const maxHp = Math.round(def.maxHp * (options.enemyHpScale ?? 1) * (m.hpScale ?? 1))
    return {
      enemyId: m.enemyId,
      hp: maxHp,
      maxHp,
      block: 0,
      intent: null,
      strength: (options.enemyStrength ?? 0) + (m.strength ?? 0),
      burn: 0,
      confusion: 0,
      exposed: 0,
      patternIndex: m.patternOffset ?? 0,
    }
  })
  state = {
    ...state,
    rng,
    player: {
      ...state.player,
      drawPile: deck,
      hp: Math.min(options.playerHp ?? state.player.maxHp, state.player.maxHp),
      // A型レリックはリーダーパッシブと同じ「戦闘開始時から場にある置物」(確定済みルール表「レリック」)
      permanents: [...state.player.permanents, ...(options.relicPermanents ?? [])],
    },
    enemies,
  }
  state = emit(state, { type: 'CombatStarted', enemyId })
  let s = startPlayerTurn(state, 1)
  // onCombatStart: 第1ターンのセットアップ (エナジー・ドロー・意図宣言) の後に1回だけ発火
  s = runPermanentTriggers(s, 'onCombatStart', Math.max(0, s.enemies.findIndex((e) => e.hp > 0)))
  return s
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
    // フェーズ変化: HP50%以下の行動テーブルが最優先 (確定済みルール表「敵フェーズ変化」)
    const belowHalf =
      enemy.hp <= enemy.maxHp * 0.5 &&
      (def.movesBelowHalf !== undefined || def.sequenceBelowHalf !== undefined)
    const table = belowHalf
      ? (def.movesBelowHalf ?? def.moves)
      : selectMoveTable(def, s.player.setCards.length > 0)
    const usingVsSet = !belowHalf && table !== def.moves
    const sequence = belowHalf ? def.sequenceBelowHalf : def.sequence

    let move
    let rng1 = s.rng
    let nextPatternIndex = enemy.patternIndex
    if (!usingVsSet && sequence && sequence.length > 0) {
      const moveId = sequence[enemy.patternIndex % sequence.length]
      move = table.find((m) => m.id === moveId)
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
      hits: move.hits,
      inflict: move.inflict,
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
  targetIndex?: number,
  exhaustUids?: readonly string[],
  retrieveUid?: string,
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

  // 消滅コストの検証 (黒。捨てより重い代わりに墓地燃料になる)
  const exhaustCost = card.def.exhaustCost ?? 0
  const exhausts = exhaustUids ?? []
  if (exhaustCost > 0) {
    if (exhausts.length !== exhaustCost) {
      throw new Error(`${card.def.name} は追加コストとして手札${exhaustCost}枚の消滅指定が必要`)
    }
    if (
      new Set(exhausts).size !== exhausts.length ||
      exhausts.includes(cardUid) ||
      exhausts.some((uid) => discards.includes(uid))
    ) {
      throw new Error('消滅させるカードの指定が不正 (重複・自分自身・捨てコストとの重複)')
    }
    for (const uid of exhausts) {
      if (!state.player.hand.some((c) => c.uid === uid)) throw new Error(`手札にないカード: ${uid}`)
    }
  }

  // 消滅置き場からの選択 (屍集め=手札へ / 死者再生=直接プレイ) の検証
  const isRetrieve = card.def.effects.some((e) => e.effect === 'retrieveFromExhaust')
  const isPlayFromExhaust = card.def.effects.some((e) => e.effect === 'playFromExhaust')
  let chosenFromExhaust: CardInstance | null = null
  if (isRetrieve || isPlayFromExhaust) {
    if (retrieveUid === undefined) {
      throw new Error(`${card.def.name} は消滅置き場のカード (retrieveUid) の指定が必要`)
    }
    chosenFromExhaust = state.player.exhaustPile.find((c) => c.uid === retrieveUid) ?? null
    if (!chosenFromExhaust) throw new Error(`消滅置き場にないカード: ${retrieveUid}`)
    if (isPlayFromExhaust) {
      // 直接プレイの制約: リアクションは窓の外では解決できず、選択式はモード選択を挟めない
      if (chosenFromExhaust.def.type === 'reaction') {
        throw new Error('リアクションは直接プレイできない')
      }
      if ((chosenFromExhaust.def.modes?.length ?? 0) > 0) {
        throw new Error('選択式カードは直接プレイできない')
      }
      if (
        chosenFromExhaust.def.effects.some(
          (e) => e.effect === 'playFromExhaust' || e.effect === 'retrieveFromExhaust',
        )
      ) {
        throw new Error('コスト再利用カード自身は直接プレイできない (再帰の禁止)')
      }
    }
  }

  // StS式ターゲティング (確定済みルール表「ターゲティング」):
  // 生存2体以上で単体対象カードは targetIndex 必須。生存1体なら自動。対象不要カードは無視
  const aliveCount = state.enemies.filter((e) => e.hp > 0).length
  if (targetIndex !== undefined) {
    const target = state.enemies[targetIndex]
    if (!target || target.hp <= 0) throw new Error(`不正な対象: ${targetIndex}`)
  }
  if (targetIndex === undefined && aliveCount > 1 && cardNeedsTarget(card, modeIndex)) {
    throw new Error(`${card.def.name} は対象の指定 (targetIndex) が必要`)
  }
  const enemyIndex = targetIndex ?? state.enemies.findIndex((e) => e.hp > 0)
  const isPermanent = card.def.type === 'permanent'
  const isExhaust = card.def.exhaust === true
  const removed = new Set([cardUid, ...discards, ...exhausts])
  const discardedCards = state.player.hand.filter((c) => discards.includes(c.uid))
  const exhaustedCards = state.player.hand.filter((c) => exhausts.includes(c.uid))
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
      exhaustPile: [
        ...state.player.exhaustPile,
        ...exhaustedCards,
        ...(isExhaust ? [card] : []),
      ],
    },
  }
  if (discardedCards.length > 0) {
    s = emit(s, { type: 'CardsDiscarded', cardIds: discardedCards.map((c) => c.def.id) })
  }
  s = emit(s, { type: 'CardPlayed', cardId: card.def.id })
  if (isPermanent) {
    s = emit(s, { type: 'PermanentPlayed', cardId: card.def.id })
    // 置物登場の誘発 (白の接着剤)。自身の登場にも誘発する (確定済みルール表「消滅の誘発」系)
    s = runPermanentTriggers(s, 'onPermanentEntered', enemyIndex)
  }
  // 消滅コストの支払い: 支払い専用誘発 (闇市の帳簿) → 消滅誘発 (亡者の合唱) の順で1枚ごとに発火
  for (const paid of exhaustedCards) {
    s = emit(s, { type: 'CardExhausted', cardId: paid.def.id })
    s = runPermanentTriggers(s, 'onCostExhausted', enemyIndex)
  }
  s = fireExhaustTriggers(s, exhaustedCards.length, enemyIndex)
  if (isExhaust) {
    s = emit(s, { type: 'CardExhausted', cardId: card.def.id })
    s = fireExhaustTriggers(s, 1, enemyIndex)
  }
  if (chosenMode) {
    for (const effect of chosenMode.effects) {
      s = resolveEffectTargeted(s, effect, enemyIndex)
    }
  } else {
    s = resolveOnPlayEffects(s, card, enemyIndex)
  }
  // 「攻撃プレイ後」誘発: 解決した効果にダメージが含まれていたか (物理・呪文を問わない)
  const resolvedEffects = chosenMode ? chosenMode.effects : card.def.effects.filter((e) => e.trigger === 'onPlay')
  if (resolvedEffects.some(isDamageEffect)) {
    s = runPermanentTriggers(s, 'onAttackPlayed', enemyIndex)
    s = fireSelfSetTriggers(s, 'onAttackPlayed', enemyIndex)
  }
  // 呪文プレイの誘発: 伏せ札の自己誘発 + 置物 (青の接着剤: 霧の分身)
  if (card.def.type === 'spell') {
    s = fireSelfSetTriggers(s, 'onSpellPlayed', enemyIndex)
    s = runPermanentTriggers(s, 'onSpellPlayed', enemyIndex)
  }
  // 衝動プレイの誘発 (赤の接着剤: 刹那の焔)
  if (state.player.impulseUids.includes(cardUid)) {
    s = runPermanentTriggers(s, 'onImpulsePlayed', enemyIndex)
  }
  // 詠唱数 (ストーム参照) は効果解決の後に加算する = そのカード自身は数えない。
  // 直接プレイ (死者再生) より先に加算する = 直接プレイされるカードから見て再生自身は「先にプレイされた1枚」
  s = { ...s, player: { ...s.player, cardsPlayedThisTurn: s.player.cardsPlayedThisTurn + 1 } }
  // 屍集め: 消滅置き場から手札へ戻す (墓地燃料が減る代わりの再利用。確定済みルール表「コスト再利用」)
  if (isRetrieve && retrieveUid !== undefined) {
    const chosen = s.player.exhaustPile.find((c) => c.uid === retrieveUid)
    if (chosen) {
      s = {
        ...s,
        player: {
          ...s.player,
          exhaustPile: s.player.exhaustPile.filter((c) => c.uid !== retrieveUid),
          hand: [...s.player.hand, chosen],
        },
      }
      s = emit(s, { type: 'CardRetrieved', cardId: chosen.def.id })
    }
  }
  // 死者再生: 消滅置き場のカードをコストを支払わず直接プレイする。
  // 置物は場に出る。それ以外は消滅置き場に残る = 墓地燃料は減らない
  if (isPlayFromExhaust && retrieveUid !== undefined) {
    const chosen = s.player.exhaustPile.find((c) => c.uid === retrieveUid)
    if (chosen) {
      s = emit(s, { type: 'CardPlayedFromExhaust', cardId: chosen.def.id })
      if (chosen.def.type === 'permanent') {
        s = {
          ...s,
          player: {
            ...s.player,
            exhaustPile: s.player.exhaustPile.filter((c) => c.uid !== retrieveUid),
            permanents: [...s.player.permanents, chosen],
          },
        }
        s = emit(s, { type: 'PermanentPlayed', cardId: chosen.def.id })
        s = runPermanentTriggers(s, 'onPermanentEntered', enemyIndex)
      }
      s = emit(s, { type: 'CardPlayed', cardId: chosen.def.id })
      s = resolveOnPlayEffects(s, chosen, enemyIndex)
      if (chosen.def.effects.filter((e) => e.trigger === 'onPlay').some(isDamageEffect)) {
        s = runPermanentTriggers(s, 'onAttackPlayed', enemyIndex)
        s = fireSelfSetTriggers(s, 'onAttackPlayed', enemyIndex)
      }
      if (chosen.def.type === 'spell') {
        s = fireSelfSetTriggers(s, 'onSpellPlayed', enemyIndex)
        s = runPermanentTriggers(s, 'onSpellPlayed', enemyIndex)
      }
      // 直接プレイも「プレイ」として詠唱数に数える (数えないのはそのカード自身のみ、の既存則)
      s = { ...s, player: { ...s.player, cardsPlayedThisTurn: s.player.cardsPlayedThisTurn + 1 } }
    }
  }
  return checkCombatEnd(s)
}

/** EndTurn: 勢いリセット・衝動の失効・延焼処理をして、敵フェーズを解決する */
export function endTurn(state: GameState): GameState {
  if (state.phase !== 'player-turn') throw new Error('自ターン以外はターン終了できない')
  let s = emit(state, { type: 'TurnEnded', turn: state.turn })
  // 勢いは自ターン終了時にリセット (確定済みルール表「勢い」)。
  // 弱体もここで1減る — 作用するフェーズ (自ターン) の終了時に減る対称則 (確定済みルール表「状態異常」)
  s = { ...s, player: { ...s.player, momentum: 0, weak: Math.max(0, s.player.weak - 1) } }
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
    // 衝動失効も消滅 = 亡者の合唱が誘発する (確定済みルール表「消滅の誘発」)
    s = fireExhaustTriggers(s, expired.length, Math.max(0, s.enemies.findIndex((e) => e.hp > 0)))
  }
  // 敵ブロックはこのタイミングで失効 (前の敵ターンの防御は自ターンの攻撃を受け止めたら役目を終える)
  s = { ...s, enemies: s.enemies.map((e) => ({ ...e, block: 0 })) }
  // 憤怒 (逆上) の参照値はフェーズ単位: 敵フェーズ開始時にリセットして受け直す
  s = { ...s, player: { ...s.player, damageTakenLastEnemyPhase: 0 } }
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

/**
 * 自己誘発リアクション: 伏せ札が自分の行動 (攻撃/呪文プレイ) で起爆する
 * (確定済みルール表「自己誘発リアクション」)。確認ウィンドウは挟まず自動発動し、捨て札へ。
 */
function fireSelfSetTriggers(
  state: GameState,
  trigger: 'onAttackPlayed' | 'onSpellPlayed',
  enemyIndex: number,
): GameState {
  const firing = state.player.setCards.filter((c) =>
    c.def.effects.some((e) => e.trigger === trigger),
  )
  if (firing.length === 0) return state
  let s: GameState = {
    ...state,
    player: {
      ...state.player,
      setCards: state.player.setCards.filter((c) => !firing.includes(c)),
      discardPile: [...state.player.discardPile, ...firing],
    },
  }
  for (const card of firing) {
    s = emit(s, { type: 'ReactionTriggered', cardId: card.def.id, mode: s.reactionMode })
    for (const effect of card.def.effects) {
      if (effect.trigger === trigger) s = resolveEffectTargeted(s, effect, enemyIndex)
    }
  }
  return checkCombatEnd(s)
}

/** 負傷 (死に札) の1戦闘上限。ハメ防止 (確定済みルール表「状態異常」) */
const WOUND_CAP = 5

/** 状態異常をプレイヤーに付与する。weak/vulnerable はカウンター加算、wound は死に札を捨て札に混入 */
function applyStatusToPlayer(state: GameState, inflict: StatusInflict): GameState {
  const { status, amount } = inflict
  if (status === 'weak' || status === 'vulnerable') {
    const player =
      status === 'weak'
        ? { ...state.player, weak: state.player.weak + amount }
        : { ...state.player, vulnerable: state.player.vulnerable + amount }
    return emit({ ...state, player }, { type: 'StatusInflicted', status, amount })
  }
  // wound: 全ゾーンの既存枚数を数えて上限までしか増えない
  const existing = [
    ...state.player.hand,
    ...state.player.drawPile,
    ...state.player.discardPile,
    ...state.player.exhaustPile,
    ...state.player.setCards,
  ].filter((c) => c.def.id === WOUND_DEF.id).length
  const add = Math.min(amount, WOUND_CAP - existing)
  if (add <= 0) return state
  const wounds = Array.from({ length: add }, (_, i) => ({
    uid: `${WOUND_DEF.id}#${existing + i}_t${state.turn}`,
    def: WOUND_DEF,
  }))
  const s: GameState = {
    ...state,
    player: { ...state.player, discardPile: [...state.player.discardPile, ...wounds] },
  }
  return emit(s, { type: 'StatusInflicted', status: 'wound', amount: add })
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
      // 混乱 (仲間割れ): 攻撃が他のランダム生存敵 (いなければ自分) に向かう (確定済みルール表「混乱」)。
      // プレイヤーへの攻撃ではないため post窓 (onAttacked) は開かない (打ち消し同様 lastAction を残さない)
      if (enemy.confusion > 0) {
        const others = state.enemies
          .map((e, i) => ({ e, i }))
          .filter(({ e, i }) => e.hp > 0 && i !== enemyIndex)
          .map(({ i }) => i)
        let s = state
        let targetIdx = enemyIndex // 他に生存敵がいなければ自分
        if (others.length === 1) {
          targetIdx = others[0]
        } else if (others.length > 1) {
          const [pick, rng] = nextInt(state.rng, 0, others.length - 1)
          targetIdx = others[pick]
          s = { ...s, rng }
        }
        const total = intent.actual * (intent.hits ?? 1)
        const target = s.enemies[targetIdx]
        const blocked = Math.min(target.block, total)
        const hpLoss = total - blocked
        s = {
          ...s,
          enemies: s.enemies.map((e, i) => {
            let x = e
            if (i === targetIdx) x = { ...x, block: x.block - blocked, hp: x.hp - hpLoss }
            if (i === enemyIndex) x = { ...x, confusion: x.confusion - 1 }
            return x
          }),
        }
        return emit(s, { type: 'ConfusedAttack', enemyIndex, targetIndex: targetIdx, amount: total })
      }
      // 連撃 (hits>1) は1発ずつ解決する (確定済みルール表「連撃」)。
      // 各ヒットに脆弱を補正し、通常ブロック→氷壁の順で消費する
      const hits = intent.hits ?? 1
      let block = state.player.block
      let iceBlock = state.player.iceBlock
      let dealtTotal = 0
      let hpLoss = 0
      for (let h = 0; h < hits; h++) {
        // 威嚇 (延焼による攻撃弱体) は撤去済み: 実値をそのまま使う (2026-08-25)
        let v = intent.actual
        // 脆弱: 敵の攻撃ダメージ50%増 (切り捨て)
        if (state.player.vulnerable > 0) v = Math.floor(v * 1.5)
        dealtTotal += v
        const blocked = Math.min(block, v)
        block -= blocked
        const remaining = v - blocked
        const iceBlocked = Math.min(iceBlock, remaining)
        iceBlock -= iceBlocked
        hpLoss += remaining - iceBlocked
      }
      let s: GameState = {
        ...state,
        player: {
          ...state.player,
          block,
          iceBlock,
          hp: state.player.hp - hpLoss,
          // 憤怒 (逆上) の参照値: このフェーズで受けた攻撃ダメージを累積する
          damageTakenLastEnemyPhase: state.player.damageTakenLastEnemyPhase + hpLoss,
        },
      }
      s = emit(s, { type: 'DamageDealt', source: 'enemy', amount: dealtTotal, hpLoss })
      // 攻撃に付与された状態異常はダメージ後に適用 (確定済みルール表「状態異常」)
      if (intent.inflict) s = applyStatusToPlayer(s, intent.inflict)
      return markResolved(s, hpLoss)
    }
    case 'hex': {
      // 状態異常の付与のみの行動 (妖術師の呪い)
      let s = state
      if (intent.inflict) s = applyStatusToPlayer(s, intent.inflict)
      return markResolved(s, 0)
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
    case 'rally': {
      // 応援: 生存する味方全体の強化 (確定済みルール表「応援（ラリー）」)
      const enemies = state.enemies.map((e) =>
        e.hp > 0 ? { ...e, strength: e.strength + intent.actual } : e,
      )
      let s: GameState = { ...state, enemies }
      for (let i = 0; i < s.enemies.length; i++) {
        if (s.enemies[i].hp > 0) {
          s = emit(s, { type: 'StrengthGained', enemyIndex: i, amount: intent.actual })
        }
      }
      return markResolved(s, 0)
    }
    case 'destroy-set': {
      if (state.player.setCards.length === 0) return markResolved(state, 0)
      let s: GameState = state
      // 伏せ破壊への罰: onSetDestroyed 効果を破壊した敵に向けて発火 (確定済みルール表「伏せ破壊への罰」)
      for (const card of state.player.setCards) {
        for (const effect of card.def.effects) {
          if (effect.trigger === 'onSetDestroyed') {
            s = resolveEffectTargeted(s, effect, enemyIndex)
          }
        }
      }
      s = {
        ...s,
        player: {
          ...s.player,
          setCards: [],
          discardPile: [...s.player.discardPile, ...state.player.setCards],
        },
      }
      for (const card of state.player.setCards) {
        s = emit(s, { type: 'SetCardDestroyed', cardId: card.def.id })
      }
      return markResolved(checkCombatEnd(s), 0)
    }
  }
}

/** 敵フェーズ終端: 空振り計上フック→手札全捨て (敵ターン後・3方式共通)→次ターン開始 */
function finishEnemyPhase(state: GameState): GameState {
  const ended = { type: 'EnemyPhaseEnded', turn: state.turn } as const
  let s = emit(state, ended)
  s = dispatchHooks(s, ended) // 空振り (ReactionWhiffed) の計上は方式固有
  // 脆弱は作用するフェーズ (敵フェーズ) の終了時に1減る (確定済みルール表「状態異常」)
  s = { ...s, player: { ...s.player, vulnerable: Math.max(0, s.player.vulnerable - 1) } }
  // 再生 (HP50%超のみ) と激昂 (確定済みルール表「再生」「激昂」)
  for (let i = 0; i < s.enemies.length; i++) {
    const e = s.enemies[i]
    if (e.hp <= 0) continue
    const def = getEnemyDef(e.enemyId)
    if (def.regen !== undefined && e.hp > e.maxHp * 0.5) {
      const amount = Math.min(def.regen, e.maxHp - e.hp)
      if (amount > 0) {
        s = {
          ...s,
          enemies: s.enemies.map((x, j) => (j === i ? { ...x, hp: x.hp + amount } : x)),
        }
        s = emit(s, { type: 'RegenTicked', enemyIndex: i, amount })
      }
    }
    if (def.enrage !== undefined && e.strength < ENRAGE_CAP) {
      // 激昂の累積上限: タイマーは残すが無限強化はしない (2026-08-25 バランス測定で低速デッキ全滅の原因と判明)
      const amount = Math.min(def.enrage, ENRAGE_CAP - e.strength)
      s = {
        ...s,
        enemies: s.enemies.map((x, j) => (j === i ? { ...x, strength: x.strength + amount } : x)),
      }
      s = emit(s, { type: 'StrengthGained', enemyIndex: i, amount })
    }
  }
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

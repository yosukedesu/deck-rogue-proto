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
  effectiveIntent,
  fireExhaustTriggers,
  hasHuntableTokens,
  isDamageEffect,
  isPlayableFromHand,
  resolveEffectTargeted,
  resolveOnPlayEffects,
} from './effects.ts'
import { buildLeaderPassive, getLeaderDef, JUNK_DEF, resolveEncounter, WOUND_DEF } from './content.ts'
import { emit } from './events.ts'
import { dispatchHooks, runPermanentTriggers } from './hooks.ts'
import { createRng, nextInt, shuffle, weightedIndex } from './rng.ts'
import type { DeclarativeEffect,
  CardInstance,
  EnemyDef,
  EnemyIntent,
  EnemyMove,
  GameState,
  ReactionMode,
  StatusInflict,
} from './types.ts'

export const PLAYER_MAX_HP = 75 // StSスケール (2026-08-25 人間基準化)
const BASE_ENERGY = 3
const DRAW_PER_TURN = 5
// 激昂の累積上限は撤廃 (2026-08-26。確定済みルール表「激昂」)。
// 本家StSに上限という概念は無く、①誘発をプレイヤーが握る ②積む敵は短命 ③筋力を剥がす手段が全キャラにある
// の3点で抑えている。本作もそれに揃えた (門番のHPを下げ、威圧を全色に配った)。
/** がらくた (罠壊し) の1戦闘あたり上限 */
const JUNK_CAP = 4

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
      energyMaxAtTurnStart: BASE_ENERGY,
      drawPerTurn: DRAW_PER_TURN,
      hand: [],
      drawPile: [],
      discardPile: [],
      setCards: [],
      setSlots: 1, // 伏せ枠は基本1。リーダー個性 (かすみ=2) で上書き

      permanents: [],
      exhaustPile: [],
      growth: 0, // 成長カウンターは戦闘内のみ (確定済みルール)
      momentum: 0, // 勢いは自ターン終了時リセット
      iceBlock: 0, // 氷壁は戦闘内で持ち越し
      cardsPlayedThisTurn: 0,
      cardsPlayedTotal: 0,
      aether: 0, // 霊気は戦闘内持続
      nextCardDiscount: 0,
      impulseUids: [],
      weak: 0,
      vulnerable: 0,
      selfHpLost: 0, // カード効果で失ったHPの累計 (背徳の収穫の参照値。戦闘内のみ)
      randomPlayedThisCombat: 0, // ランダム火力の枚数 (一擲乾坤の参照値。戦闘内のみ)
      damageTakenLastEnemyPhase: 0, // 直前の敵フェーズで受けた攻撃ダメージ (逆上の参照値)
    },
    enemies: [],
    pendingWindow: null,
    negateNextAction: false,
    reactionUsedThisAction: false,
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
  /**
   * 最大HP (ランの恒久ボーナス用。鉄の心臓など)。省略時はリーダーの素の値。
   * 2026-08-27 修正: これが無かったため、B型レリックが増やした run.maxHp が
   * 戦闘に一度も届いていなかった (プレイテスターのバグ報告で発覚)
   */
  readonly playerMaxHp?: number
  /** 敵HPの倍率 (ランの深度スケーリング用) */
  readonly enemyHpScale?: number
  /** 敵の初期強化 (攻撃の実値・幅表示に加算される) */
  readonly enemyStrength?: number
  /** A型レリックの置物 (buildRelicPermanent で生成。リーダーパッシブと同様に注入される) */
  readonly relicPermanents?: readonly CardInstance[]
  /** C型レリック (静かな鈴): 伏せ札がある間、敵の攻撃実値-N */
  readonly setDamageReduction?: number
  /** C型レリック (蜃気楼の面): 意図の実値を常時公開 */
  readonly revealIntents?: boolean
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
        energyMaxAtTurnStart: leader.energyMax,
        setSlots: leader.setSlots ?? 1,
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
      // 開幕ブロック (2026-08-30 静的性質の配布): 甲羅・門・抱えた樽はT1から見える問い
      block: def.startingBlock ?? 0,
      intent: null,
      strength: (options.enemyStrength ?? 0) + (m.strength ?? 0),
      burn: 0,
      confusion: 0,
      exposed: 0,
      patternIndex: m.patternOffset ?? 0,
      ...(m.noReactTable === true ? { noReactTable: true } : {}),
      // とげは def からコピーして状態に持つ (effects.ts が content 参照なしで反射できる)
      ...(def.thorns !== undefined ? { thorns: def.thorns } : {}),
      ...(def.armor !== undefined ? { armor: def.armor } : {}),
    }
  })
  state = {
    ...state,
    rng,
    player: {
      ...state.player,
      drawPile: deck,
      maxHp: options.playerMaxHp ?? state.player.maxHp,
      hp: Math.min(
        options.playerHp ?? options.playerMaxHp ?? state.player.maxHp,
        options.playerMaxHp ?? state.player.maxHp,
      ),
      // A型レリックはリーダーパッシブと同じ「戦闘開始時から場にある置物」(確定済みルール表「レリック」)
      permanents: [...state.player.permanents, ...(options.relicPermanents ?? [])],
    },
    enemies,
    // C型レリック。revealIntents は第1ターンの意図宣言 (startPlayerTurn) より前に立てる必要がある
    ...(options.setDamageReduction ? { setDamageReduction: options.setDamageReduction } : {}),
    ...(options.revealIntents ? { revealIntents: true } : {}),
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

/**
 * 敵の行動テーブル選択: 伏せがあれば movesVsSet、召喚トークンがいれば movesVsTokens を優先
 * (優先度: 伏せ反応 > トークン反応 > 通常。確定済みルール表「トークン破壊」)
 */
export function selectMoveTable(
  def: EnemyDef,
  playerHasSetCards: boolean,
  playerHasTokens = false,
): readonly EnemyMove[] {
  if (playerHasSetCards && def.movesVsSet && def.movesVsSet.length > 0) return def.movesVsSet
  if (playerHasTokens && def.movesVsTokens && def.movesVsTokens.length > 0) return def.movesVsTokens
  return def.moves
}

/**
 * 時喰らい型タイマー (確定済みルール表「激昂」)。
 * プレイヤーの累計詠唱数が enrageEveryCards の倍数に達したタイミングで強化する。
 * 時間ではなくプレイヤーのテンポに紐づくので、低速デッキほど誘発が遅くなる = 自己調整する。
 */
function tickCardTimers(state: GameState): GameState {
  const total = state.player.cardsPlayedTotal
  let s = state
  for (let i = 0; i < s.enemies.length; i++) {
    const e = s.enemies[i]
    if (e.hp <= 0) continue
    const def = getEnemyDef(e.enemyId)
    const every = def.enrageEveryCards
    if (every === undefined || every <= 0) continue
    if (total === 0 || total % every !== 0) continue
    const amount = def.enrage ?? 0
    if (amount <= 0) continue
    s = {
      ...s,
      enemies: s.enemies.map((x, j) => (j === i ? { ...x, strength: x.strength + amount } : x)),
    }
    s = emit(s, { type: 'StrengthGained', enemyIndex: i, amount })
  }
  return s
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
    // 盗んだ敵は次の宣言で必ず逃走する (2026-08-30。宣言即成立の盗みが「倒せば全額戻る」で
    // 無害化していた実測への処方 = 「1ターン以内に倒せ」のレースを尖らせる)
    const fleeMove = def.moves.find((m) => m.kind === 'flee')
    if ((enemy.stolenGold ?? 0) > 0 && fleeMove && enemy.intent?.kind !== 'flee') {
      const [fleeIntent, rngF] = buildIntent(s.rng, fleeMove, enemy.strength)
      const enemies2 = s.enemies.map((e, j) => (j === i ? { ...e, intent: fleeIntent } : e))
      s = emit({ ...s, rng: rngF, enemies: enemies2 }, { type: 'EnemyIntentDeclared', enemyIndex: i, intent: fleeIntent })
      continue
    }
    // フェーズ変化: HP50%以下の行動テーブルが最優先 (確定済みルール表「敵フェーズ変化」)
    const belowHalf =
      enemy.hp <= enemy.maxHp * 0.5 &&
      (def.movesBelowHalf !== undefined || def.sequenceBelowHalf !== undefined)
    const sequence = belowHalf ? def.sequenceBelowHalf : def.sequence
    // 反応テーブル (伏せ/従者) を持つ敵は、条件付き意図として両分岐を宣言時に確定する
    // (確定済みルール表「条件付き意図」。実行時の盤面で分岐 = プレイヤーが自ターン中に選べる)
    // 両テーブルを持つ敵 (罠壊し) は conditionalOn を1つしか持てないので、宣言時の盤面で片方を選ぶ。
    // 優先度は 伏せ反応 > 従者反応 (確定済みルール表「従者狩り」)。どちらの条件も満たしていない時は
    // 伏せ反応を既定にして「伏せれば行動が変わる」の予告を残す。
    // 2026-08-26 修正: 旧実装は `def.movesVsSet ?? def.movesVsTokens` で、両テーブルを持つ唯一の敵では
    // movesVsSet が常に勝つため destroy-token が production から到達不能だった。
    // 編成で反応テーブルを無効化された個体は分岐を持たない (群れで全員が同時に反応しない)
    const vsSet = enemy.noReactTable !== true && def.movesVsSet?.length ? def.movesVsSet : undefined
    const vsTokens =
      enemy.noReactTable !== true && def.movesVsTokens?.length ? def.movesVsTokens : undefined
    const preferTokens = s.player.setCards.length === 0 && hasHuntableTokens(s)
    const reactTable = belowHalf
      ? undefined
      : preferTokens
        ? (vsTokens ?? vsSet)
        : (vsSet ?? vsTokens)
    const conditionalOn: 'set' | 'tokens' | undefined = !reactTable
      ? undefined
      : reactTable === vsSet
        ? 'set'
        : 'tokens'
    const baseTable = belowHalf ? (def.movesBelowHalf ?? def.moves) : def.moves

    let rng = s.rng
    let nextPatternIndex = enemy.patternIndex
    // 通常分岐: sequence を持つ敵は固定ローテーション
    let move: EnemyMove
    if (sequence && sequence.length > 0) {
      const moveId = sequence[enemy.patternIndex % sequence.length]
      const found = baseTable.find((m) => m.id === moveId)
      if (!found) throw new Error(`敵 ${def.id} の sequence が未定義の行動を参照: ${moveId}`)
      move = found
      nextPatternIndex = enemy.patternIndex + 1
    } else {
      const [moveIdx, rngAfter] = weightedIndex(rng, baseTable.map((m) => m.weight))
      move = baseTable[moveIdx]
      rng = rngAfter
    }
    const [intent, rngA] = buildIntent(rng, move, enemy.strength)
    rng = rngA

    let alt: ReturnType<typeof buildIntent>[0] | undefined
    let condOn = conditionalOn
    if (reactTable && reactTable.length > 0) {
      const [altIdx, rngB] = weightedIndex(rng, reactTable.map((m) => m.weight))
      rng = rngB
      const [altIntent, rngC] = buildIntent(rng, reactTable[altIdx], enemy.strength)
      rng = rngC
      alt = altIntent
    } else if (!belowHalf && enemy.noReactTable !== true && move.setAlt !== undefined) {
      // 行動単位の条件分岐 (確定済みルール表「読み合いの全敵展開」2026-08-28):
      // 伏せ札があるとこの行動が setAlt の行動に変わる。既存の条件付き意図の配管に乗せる
      const sa = move.setAlt
      const altMove: EnemyMove = {
        id: `${move.id}@set`,
        weight: 1,
        kind: sa.kind,
        ...(sa.min !== undefined ? { min: sa.min } : {}),
        ...(sa.max !== undefined ? { max: sa.max } : {}),
        ...(sa.hits !== undefined ? { hits: sa.hits } : {}),
        ...(sa.inflict !== undefined ? { inflict: sa.inflict } : {}),
        ...(sa.alsoDefend !== undefined ? { alsoDefend: sa.alsoDefend } : {}),
      }
      const [altIntent, rngC] = buildIntent(rng, altMove, enemy.strength)
      rng = rngC
      alt = altIntent
      condOn = 'set'
    }

    // 蜃気楼の面 (C型レリック): 実値を常時公開 = 宣言時に幅を実値へ畳む。
    // 表示層 (UI/CLI/最悪被ダメ予測) は shownMin/shownMax を読むだけなので変更不要で、
    // 条件分岐 (alt) の両側も自動で実値になる
    const reveal = <T extends { shownMin: number; shownMax: number; actual: number }>(it: T): T =>
      s.revealIntents ? { ...it, shownMin: it.actual, shownMax: it.actual } : it
    const shown = reveal(intent)
    if (alt !== undefined) alt = reveal(alt)
    const declared = condOn && alt ? { ...shown, conditionalOn: condOn, alt } : shown
    // 盗みは宣言と同時に成立する (2026-08-30 「宣言ターン内に仕事をする」パッケージ)。
    // 旧実装は実行時成立のため、宣言ターンに倒すと盗み・逃走の設計が丸ごと空振りしていた
    // (3幕フルラン実測: こそ泥4戦で盗み・逃走を一度も見ていない)。宣言時に抱えれば
    // 「今すぐ倒して取り返す (+懸賞金) か、放置して失うか」のレースが必ず発生する
    const stolen = declared.kind === 'steal-gold' ? declared.actual : 0
    const enemies = s.enemies.map((e, j) =>
      j === i
        ? {
            ...e,
            intent: declared,
            patternIndex: nextPatternIndex,
            ...(stolen > 0 ? { stolenGold: (e.stolenGold ?? 0) + stolen } : {}),
          }
        : e,
    )
    s = emit({ ...s, rng, enemies }, { type: 'EnemyIntentDeclared', enemyIndex: i, intent: declared })
    if (stolen > 0) s = emit(s, { type: 'GoldStolen', enemyIndex: i, amount: stolen })
  }
  return s
}

/** 行動1つから意図 (幅表示 + 非公開の実値) を組み立てる。強化は攻撃にのみ乗り、攻撃は最低1にクランプ */
function buildIntent(
  rng: GameState['rng'],
  move: EnemyMove,
  strength: number,
): readonly [
  {
    kind: EnemyMove['kind']
    shownMin: number
    shownMax: number
    actual: number
    hits?: number
    inflict?: StatusInflict
    alsoDefend?: number
  },
  GameState['rng'],
] {
  let actual = 0
  let next = rng
  if (move.min !== undefined && move.max !== undefined) {
    ;[actual, next] = nextInt(rng, move.min, move.max)
  }
  const bonus = move.kind === 'attack' ? strength : 0
  const clamp = (v: number) => (move.kind === 'attack' ? Math.max(1, v) : v)
  return [
    {
      kind: move.kind,
      shownMin: clamp((move.min ?? 0) + bonus),
      shownMax: clamp((move.max ?? 0) + bonus),
      actual: clamp(actual + bonus),
      hits: move.hits,
      inflict: move.inflict,
      alsoDefend: move.alsoDefend,
    },
    next,
  ]
}

/** 自ターン開始: ブロック0リセット・エナジー全回復・置物の開始時効果・5枚ドロー・敵意図宣言 */
function startPlayerTurn(state: GameState, turn: number): GameState {
  let s: GameState = {
    ...state,
    turn,
    phase: 'player-turn',
    // 通常ブロックはリセット。氷壁 (iceBlock) は持ち越される。
    // 上限のスナップショットもここで更新 = このターン中のランプは上限参照札に乗らない
    player: {
      ...state.player,
      block: 0,
      energy: state.player.energyMax,
      energyMaxAtTurnStart: state.player.energyMax,
      cardsPlayedThisTurn: 0,
      freeResetUid: undefined,
      // 見切り (2026-08-30): 前のターンから置きっぱなしの伏せ札は「織り込み済み」になる
      setCards: state.player.setCards.map((c) => (c.setFresh ? { ...c, setFresh: false } : c)),
    },
  }
  s = emit(s, { type: 'TurnStarted', turn })
  s = runPermanentTriggers(s, 'onTurnStart', Math.max(0, s.enemies.findIndex((e) => e.hp > 0)))
  // ターン開始誘発 (従者の自動攻撃など) で敵が全滅したら即座に勝利を確定する
  // (プレイテストで発見: 判定がないと撃破済みの敵に手札が撃てる状態が残る)
  s = checkCombatEnd(s)
  if (s.phase === 'won' || s.phase === 'lost') return s
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
  const consumesDiscount =
    card.def.cost > 0 && state.player.nextCardDiscount > 0 && card.def.xCost !== true
  if (cost > state.player.energy) throw new Error(`エナジー不足: ${card.def.name}`)
  // Xコスト: 支払った量を xHits 効果の繰り返し回数として展開する (多段ヒットと同じ解決)
  const paidX = card.def.xCost === true ? cost : 0
  const expandX = (effects: readonly DeclarativeEffect[]): readonly DeclarativeEffect[] =>
    paidX === 0
      ? effects
      : effects.flatMap((e) =>
          e.xHits === true ? Array.from({ length: paidX }, () => ({ ...e, xHits: undefined })) : [e],
        )
  const effCard: CardInstance =
    paidX === 0 ? card : { ...card, def: { ...card.def, effects: expandX(card.def.effects) } }

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
    // targetIndex は撃破済みを含む並び順の生インデックス。検証ランで「先頭撃破後に0を指定して
    // 原因不明のエラー」の報告があったため、死亡対象と生存対象の位置を明示する
    if (!target) throw new Error(`不正な対象: ${targetIndex} (敵は${state.enemies.length}体)`)
    if (target.hp <= 0) {
      const alive = state.enemies.map((e, i) => (e.hp > 0 ? i : -1)).filter((i) => i >= 0)
      // 生存1体なら対象は一意なので、死亡枠を指しても自動でリターゲットする
      // (「生存1体なら自動」の既存則の延長。2026-08-29 検証ランのCLI摩擦報告への対処)
      if (alive.length === 1) {
        targetIndex = alive[0]
      } else {
        throw new Error(
          `対象 ${targetIndex} はすでに${target.fled ? '逃走' : '倒れて'}いる (targetIndexは撃破済みを含む並び順。生存: ${alive.join(',')})`,
        )
      }
    }
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
    s = resolveOnPlayEffects(s, effCard, enemyIndex)
  }
  // 「攻撃プレイ後」誘発: 解決した効果にダメージが含まれていたか (物理・呪文を問わない)
  const resolvedEffects = chosenMode ? chosenMode.effects : effCard.def.effects.filter((e) => e.trigger === 'onPlay')
  if (resolvedEffects.some(isDamageEffect)) {
    s = runPermanentTriggers(s, 'onAttackPlayed', enemyIndex)
    s = fireSelfSetTriggers(s, 'onAttackPlayed', enemyIndex)
  }
  // 「カードをプレイするたび」の誘発 (種類を問わない)。赤の手数を勢いに変える
  s = runPermanentTriggers(s, 'onCardPlayed', enemyIndex)
  // 呪文プレイの誘発: 伏せ札の自己誘発 + 置物 (青の接着剤: 霧の分身)
  if (card.def.type === 'spell') {
    s = fireSelfSetTriggers(s, 'onSpellPlayed', enemyIndex)
    s = runPermanentTriggers(s, 'onSpellPlayed', enemyIndex)
  }
  // 衝動プレイの誘発 (赤の接着剤: 刹那の焔)
  if (state.player.impulseUids.includes(cardUid)) {
    s = runPermanentTriggers(s, 'onImpulsePlayed', enemyIndex)
  }
  // ランダム火力の誘発 (赤カオスの接着剤: 賭博師の焔。2026-08-30)。
  // **カード単位で1回**数える — target:'all' のランダム火力 (大花火) は敵の数だけ解決されるので、
  // 効果の解決側で数えると頭数ぶん多重に数えてしまう
  if (card.def.effects.some((e) => e.effect === 'dealDamageRandom')) {
    s = {
      ...s,
      player: { ...s.player, randomPlayedThisCombat: s.player.randomPlayedThisCombat + 1 },
    }
    s = runPermanentTriggers(s, 'onRandomPlayed', enemyIndex)
  }
  // 詠唱数 (ストーム参照) は効果解決の後に加算する = そのカード自身は数えない。
  // 直接プレイ (死者再生) より先に加算する = 直接プレイされるカードから見て再生自身は「先にプレイされた1枚」
  s = {
    ...s,
    player: {
      ...s.player,
      cardsPlayedThisTurn: s.player.cardsPlayedThisTurn + 1,
      cardsPlayedTotal: s.player.cardsPlayedTotal + 1,
    },
  }
  s = tickCardTimers(s)
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
      s = {
    ...s,
    player: {
      ...s.player,
      cardsPlayedThisTurn: s.player.cardsPlayedThisTurn + 1,
      cardsPlayedTotal: s.player.cardsPlayedTotal + 1,
    },
  }
  s = tickCardTimers(s)
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
  // 延焼: 敵フェーズ開始時にダメージ (ブロック無視) を受けて1減る。
  // 延焼耐性 (burnResist): 追加でN減る (確定済みルール表「敵の耐性」)
  for (let i = 0; i < s.enemies.length; i++) {
    const enemy = s.enemies[i]
    if (enemy.hp <= 0 || enemy.burn <= 0) continue
    const amount = enemy.burn
    const decay = 1 + (getEnemyDef(enemy.enemyId).burnResist ?? 0)
    s = {
      ...s,
      enemies: s.enemies.map((e, j) =>
        j === i ? { ...e, hp: e.hp - amount, burn: Math.max(0, e.burn - decay) } : e,
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
    if (s.enemies.every((e) => e.hp <= 0)) return checkCombatEnd(s)
    // プレイヤーの致死は post窓の解決後に判定する (確定済みルール表「致死時の誘発」)
    s = postActionStage(s, pending.enemyIndex)
    if (s.phase === 'awaiting-reaction') return s
    s = checkCombatEnd(s)
    if (isOver(s)) return s
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
    const acting = effectiveIntent(s, i)!
    // 2026-08-26 修正: 条件付き意図の分岐をここで確定させる。
    // 旧実装は executeEnemyAction で分岐を再計算していたため、pre窓でリアクションを発動して
    // 伏せ枠が空になると「伏せ札あり」の弱い分岐から「なし」の強い分岐へ化けていた
    // (確認ウィンドウが攻撃8と表示したのに実際は16で解決される = 窓が嘘をつく状態だった)。
    const locked: EnemyIntent = {
      kind: acting.kind,
      shownMin: acting.shownMin,
      shownMax: acting.shownMax,
      actual: acting.actual,
      ...(acting.hits !== undefined ? { hits: acting.hits } : {}),
      ...(acting.inflict !== undefined ? { inflict: acting.inflict } : {}),
      ...(acting.alsoDefend !== undefined ? { alsoDefend: acting.alsoDefend } : {}),
    }
    // 行動ごとにリアクション消費フラグをリセット (敵の1行動につき1回まで)
    s = {
      ...s,
      enemies: s.enemies.map((e, j) => (j === i ? { ...e, intent: locked } : e)),
      lastAction: null,
      reactionUsedThisAction: false,
    }
    // 行動実行の直前フック (pre窓): 打ち消し・軽減リアクションがここで発動/割り込みする
    const executing = { type: 'EnemyActionExecuting', enemyIndex: i, kind: locked.kind } as const
    s = emit(s, executing)
    s = dispatchHooks(s, executing)
    if (s.phase === 'awaiting-reaction') return s // pre窓の割り込み → コマンド待ち
    s = checkCombatEnd(s)
    if (isOver(s)) return s
    s = executeEnemyAction(s, i)
    // 敵が倒れたらここで終了。プレイヤーの致死は post窓 (返し系) の解決後に判定する
    // (確定済みルール表「致死時の誘発」: 回復付きの返し札で生き延びる余地を作る)
    if (s.enemies.every((e) => e.hp <= 0)) {
      s = checkCombatEnd(s)
      return s
    }
    s = postActionStage(s, i)
    if (s.phase === 'awaiting-reaction') return s
    s = checkCombatEnd(s)
    if (isOver(s)) return s
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
  if (status === 'junk') {
    // がらくた: 山札のランダムな位置に混ぜ込む (負傷と違い、すぐ引かされる)
    const existingJunk = [
      ...state.player.hand,
      ...state.player.drawPile,
      ...state.player.discardPile,
    ].filter((c) => c.def.id === JUNK_DEF.id).length
    const addJunk = Math.min(amount, JUNK_CAP - existingJunk)
    if (addJunk <= 0) return state
    let drawPile = [...state.player.drawPile]
    let rng = state.rng
    for (let i = 0; i < addJunk; i++) {
      const [pos, nextRng] = nextInt(rng, 0, drawPile.length)
      rng = nextRng
      drawPile = [
        ...drawPile.slice(0, pos),
        { uid: `${JUNK_DEF.id}#${existingJunk + i}_t${state.turn}`, def: JUNK_DEF },
        ...drawPile.slice(pos),
      ]
    }
    const s2: GameState = { ...state, rng, player: { ...state.player, drawPile } }
    return emit(s2, { type: 'StatusInflicted', status: 'junk', amount: addJunk })
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
    // 打ち消しの成功に反応する置物 (青: 還流の水鏡)。negate / negateConvertIce の両方がここを通る
    const negated = emit({ ...state, negateNextAction: false }, { type: 'ActionNegated', enemyIndex })
    return runPermanentTriggers(negated, 'onActionNegated', enemyIndex)
  }
  const intent = effectiveIntent(state, enemyIndex)!
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
        // 静かな鈴 (C型レリック): 伏せ札がある間、各ヒット-N (最低1クランプは威圧と同則)
        if ((state.setDamageReduction ?? 0) > 0 && state.player.setCards.length > 0) {
          v = Math.max(1, v - (state.setDamageReduction ?? 0))
        }
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
      // 攻防一体 (alsoDefend): 攻撃と同時に固定ブロックを得る (確定済みルール表「攻防一体・隙」)
      if (intent.alsoDefend !== undefined && intent.alsoDefend > 0) {
        s = {
          ...s,
          enemies: s.enemies.map((e, i) =>
            i === enemyIndex ? { ...e, block: e.block + intent.alsoDefend! } : e,
          ),
        }
        s = emit(s, { type: 'BlockGained', target: 'enemy', amount: intent.alsoDefend })
      }
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
    case 'heal': {
      // 回復役: 最もHP割合の低い生存味方 (自分含む) を回復 (確定済みルール表「回復役（敵）」)
      let targetIdx = enemyIndex
      let worst = Infinity
      state.enemies.forEach((e, i) => {
        if (e.hp <= 0) return
        const ratio = e.hp / e.maxHp
        if (ratio < worst) {
          worst = ratio
          targetIdx = i
        }
      })
      const target = state.enemies[targetIdx]
      const healed = Math.min(intent.actual, target.maxHp - target.hp)
      const s = emit(
        {
          ...state,
          enemies: state.enemies.map((e, i) => (i === targetIdx ? { ...e, hp: e.hp + healed } : e)),
        },
        { type: 'EnemyHealed', enemyIndex, targetIndex: targetIdx, amount: healed },
      )
      return markResolved(s, 0)
    }
    case 'steal-gold': {
      // 盗みは宣言時に成立済み (2026-08-30)。実行時は「袋に詰める」だけの演出 = no-op。
      // 精算 (逃走なら喪失/撃破なら奪還+懸賞金) は勝利時にrun層 (combat層はゴールドを知らない)
      const s = state
      return markResolved(s, 0)
    }
    case 'flee': {
      // 逃走: 戦闘から離脱。hp:0+fled で既存の死亡判定・勝利判定がそのまま機能する。
      // 打ち消し可能 (negateNextAction は本関数冒頭で処理済み) = 逃走自体を止められる
      const s = emit(
        {
          ...state,
          enemies: state.enemies.map((e, i) =>
            i === enemyIndex ? { ...e, hp: 0, fled: true, intent: null } : e,
          ),
        },
        { type: 'EnemyFled', enemyIndex },
      )
      return markResolved(s, 0)
    }
    case 'rest': {
      // 隙: 何もしない (斧鬼の息切れ = 大技を凌げば1ターンの反撃の窓)
      return markResolved(state, 0)
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
    case 'destroy-token': {
      // 従者狩り: 召喚トークンまたは従者 (生き物の置物) 1体をランダムに破壊。
      // 道具・オーラ系の手張り置物・リーダー・レリックは対象外 (確定済みルール表「トークン破壊」)
      const tokens = state.player.permanents.filter((p) => p.token === true || p.def.retainer === true)
      if (tokens.length === 0) return markResolved(state, 0)
      const [idx, rng] = nextInt(state.rng, 0, tokens.length - 1)
      const target = tokens[idx]
      let s: GameState = {
        ...state,
        rng,
        player: {
          ...state.player,
          permanents: state.player.permanents.filter((p) => p.uid !== target.uid),
        },
      }
      s = emit(s, { type: 'TokenDestroyed', cardId: target.def.id })
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
      // 伏せ破壊にも状態異常の付与が乗る (罠壊しの「がらくた」= 壊した残骸を投げつける)
      if (intent.inflict) s = applyStatusToPlayer(s, intent.inflict)
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
      // regenBreak (確定済みルール表「再生」2026-08-28): このターンに閾値以上削られていたら再生しない。
      // 「チクチク削り」では止まらず、バーストの計画で止められる = 膠着をパズルに変える
      const broken =
        def.regenBreak !== undefined && (e.hpLostSinceRegen ?? 0) >= def.regenBreak
      if (broken) {
        s = emit(s, { type: 'RegenBroken', enemyIndex: i })
      } else {
        const amount = Math.min(def.regen, e.maxHp - e.hp)
        if (amount > 0) {
          s = {
            ...s,
            enemies: s.enemies.map((x, j) => (j === i ? { ...x, hp: x.hp + amount } : x)),
          }
          s = emit(s, { type: 'RegenTicked', enemyIndex: i, amount })
        }
      }
    }
    // 再生判定を通過したら累積をリセット (次のターンの削りを次の判定に使う)
    s = {
      ...s,
      enemies: s.enemies.map((x, j) => (j === i ? { ...x, hpLostSinceRegen: 0 } : x)),
    }
    if (def.enrage !== undefined && def.enrageEveryCards === undefined) {
      // 上限なし = 本家のソフトタイマー (2026-08-26)。長引かせるほど手が付けられなくなる。
      // 抑えているのは「積む敵は短命 (門番のHPを下げた)」と「威圧が全色にある」の2点
      const amount = def.enrage
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

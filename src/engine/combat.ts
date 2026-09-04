// engine/combat.ts — 戦闘フロー (ターン進行・敵フェーズ・意図宣言)
// 敵フェーズは EndTurn コマンド内で同期的に解決する。
// リアクション方式が割り込みを要求した場合のみ 'awaiting-reaction' で中断し、
// state.ts が方式コマンド処理後に continueAfterWindow() で再開する。
// 方式固有の if 分岐をここに書いてはならない (フックは dispatchHooks 経由)。

import { canUpgradeInHand, upgradeCard } from './upgrade.ts'
import { buildDeck, getEnemyDef, SCALD_DEF, BRAND_DEF, GUILT_DEF } from './content.ts'
import { applyWakeCheck, cardNeedsTarget, drawCards, effectiveCost, effectiveIntent, fireExhaustTriggers, fireNecroEffects, hasHuntableTokens, isDamageEffect, isPlayableFromHand, millPlayerDeck, resolveEffectTargeted, resolveOnPlayEffects, applyEnemyWeak } from './effects.ts'
import { buildLeaderPassive, getLeaderDef, JUNK_DEF, resolveEncounter, WOUND_DEF } from './content.ts'
import { emit } from './events.ts'
import { dispatchHooks, runPermanentTriggers } from './hooks.ts'
import { createRng, nextInt, shuffle, weightedIndex } from './rng.ts'
import type { CardDef, DeclarativeEffect,
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
      setsThisTurn: 0,
      playsThisTurn: 0,
      attacksPlayedThisTurn: 0,
      weakFreshThisPhase: 0,
      cardsPlayedTotal: 0,
      aether: 0, // 霊気は戦闘内持続
      healsThisCombat: 0,
      nextCardDiscount: 0,
      impulseUids: [],
      weak: 0,
      vulnerable: 0,
      frail: 0,
      restrain: 0,
      selfHpLost: 0, // カード効果で失ったHPの累計 (背徳の収穫の参照値。戦闘内のみ)
      randomPlayedThisCombat: 0, // ランダム火力の枚数 (一擲乾坤の参照値。戦闘内のみ)
      damageTakenLastEnemyPhase: 0, // 直前の敵フェーズで受けた攻撃ダメージ (逆上の参照値)
      spellEchoes: 0, // 反復トークン (青: 呪文コピー)
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
  /** 敵の打点倍率 (2026-09-01 幕2/3+15%。攻撃の基礎値に乗算・四捨五入。強化は倍率の後) */
  readonly enemyAtkScale?: number
  /** A型レリックの置物 (buildRelicPermanent で生成。リーダーパッシブと同様に注入される) */
  readonly relicPermanents?: readonly CardInstance[]
  /** C型レリック (静かな鈴): 伏せ札がある間、敵の攻撃実値-N */
  readonly setDamageReduction?: number
  /** デバッグ: 意図の実値を常時公開 */
  readonly revealIntents?: boolean
  /** C型レリック (蜃気楼の面): 伏せた瞬間からそのターンの実値を公開 */
  readonly revealOnSet?: boolean
  /** 実験: 全カード伏せ可 */
  readonly setAnyCards?: boolean
  /** C型レリック (回収の紐): 回収が0E */
  readonly retrieveFree?: boolean
  /** C型レリック (大樹の心): 上限参照札が読む値に+N */
  readonly energyMaxRefBonus?: number
  /** C型レリック (収穫の鎌): 成長放出のあと成長がN残る */
  readonly harvestKeep?: number
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
        energyMaxAtTurnStart: leader.energyMax + (options.energyMaxRefBonus ?? 0), // 大樹の心 (2026-09-03)
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
      block: def.burrow?.block ?? def.startingBlock ?? 0, // 潜伏の殻は開幕ブロックと同じ器 (2026-09-03)
      ...(def.burrow ? { burrowActive: true } : {}),
      intent: null,
      strength: (options.enemyStrength ?? 0) + (m.strength ?? 0),
      ...(options.enemyAtkScale !== undefined && options.enemyAtkScale !== 1
        ? { atkScale: options.enemyAtkScale }
        : {}),
      burn: 0,
      confusion: 0,
      exposed: 0,
      patternIndex: m.patternOffset ?? 0,
      ...(m.noReactTable === true ? { noReactTable: true } : {}),
      // とげは def からコピーして状態に持つ (effects.ts が content 参照なしで反射できる)
      ...(def.thorns !== undefined ? { thorns: def.thorns } : {}),
      ...(def.artifact !== undefined ? { artifact: def.artifact } : {}),
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
    ...(options.retrieveFree ? { retrieveFree: true } : {}),
    ...(options.energyMaxRefBonus ? { energyMaxRefBonus: options.energyMaxRefBonus } : {}),
    ...(options.harvestKeep ? { harvestKeep: options.harvestKeep } : {}),
    ...(options.revealIntents ? { revealIntents: true } : {}),
    ...(options.revealOnSet ? { revealOnSet: true } : {}),
    ...(options.setAnyCards ? { setAnyCards: true } : {}),
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
    s = emit(s, { type: 'StrengthGained', enemyIndex: i, amount, reason: 'enrage-cards' })
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
    const rawEnemy = s.enemies[i]
    if (rawEnemy.hp <= 0) continue
    const def = getEnemyDef(rawEnemy.enemyId)
    // 連携 (2026-09-02): 他の仲間が生存中は攻撃+N (宣言時判定 = 宣言時固定の既存則)
    const bond =
      def.bondStrength !== undefined && s.enemies.some((o, j) => j !== i && o.hp > 0)
        ? def.bondStrength
        : 0
    const enemy = bond > 0 ? { ...rawEnemy, strength: rawEnemy.strength + bond } : rawEnemy
    // 盗んだ敵は次の宣言で必ず逃走する (2026-08-30。宣言即成立の盗みが「倒せば全額戻る」で
    // 無害化していた実測への処方 = 「1ターン以内に倒せ」のレースを尖らせる)
    // flee の move を持たない盗人 (金羽の大鴉) でも合成の逃走を宣言する (2026-08-31 青ラン発見:
    // 大鴉が盗んだ後も防御を続け、レース設計が丸ごと空振りしていた)
    const fleeMove = def.moves.find((m) => m.kind === 'flee') ?? {
      id: 'forced_flee',
      kind: 'flee' as const,
      weight: 1,
    }
    // 潜伏の殻が敵フェーズ中に割れていたら、この宣言は噛みつき (2026-09-03 Burrowed)
    const biteMove = def.burrow ? def.moves.find((m) => m.id === def.burrow!.bite) : undefined
    if (enemy.biteNext === true && biteMove) {
      const [biteIntent, rngB] = buildIntent(s.rng, biteMove, enemy.strength, enemy.atkScale ?? 1)
      const enemiesB = s.enemies.map((e, j) => (j === i ? { ...e, intent: biteIntent, biteNext: false } : e))
      s = emit({ ...s, rng: rngB, enemies: enemiesB }, { type: 'EnemyIntentDeclared', enemyIndex: i, intent: biteIntent })
      continue
    }
    if ((enemy.stolenGold ?? 0) > 0 && enemy.intent?.kind !== 'flee') {
      const [fleeIntent, rngF] = buildIntent(s.rng, fleeMove, enemy.strength, enemy.atkScale ?? 1)
      const enemies2 = s.enemies.map((e, j) => (j === i ? { ...e, intent: fleeIntent } : e))
      s = emit({ ...s, rng: rngF, enemies: enemies2 }, { type: 'EnemyIntentDeclared', enemyIndex: i, intent: fleeIntent })
      continue
    }
    // フェーズ変化: HP50%以下の行動テーブルが最優先 (確定済みルール表「敵フェーズ変化」)
    const belowHalf =
      enemy.hp <= enemy.maxHp * 0.5 &&
      (def.movesBelowHalf !== undefined || def.sequenceBelowHalf !== undefined)
    // 単独時テーブル (2026-09-02 StS2 LivingShield式転職): 仲間が全滅したら切替。
    // 優先度は HP半分 > 単独時 > 通常。反応テーブル・setAltは無効化 = 転職後は素直に殴る
    const whenAlone =
      !belowHalf &&
      (def.movesWhenAlone !== undefined || def.sequenceWhenAlone !== undefined) &&
      !s.enemies.some((o, j) => j !== i && o.hp > 0)
    // 回数カウンタのフェーズ変化 (2026-09-02 StS2 KnowledgeDemon式): 対象行動を規定回数
    // 宣言したら恒久切替 (切替時に patternIndex は 0 へ巻き戻し済み)
    const phaseSwitched =
      def.phaseAfterUses !== undefined && (enemy.keyMoveUses ?? 0) >= def.phaseAfterUses.uses
    const sequence = belowHalf
      ? def.sequenceBelowHalf
      : whenAlone
        ? def.sequenceWhenAlone
        : phaseSwitched
          ? def.phaseAfterUses?.sequence
          : def.sequence
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
    const reactTable = belowHalf || whenAlone
      ? undefined
      : preferTokens
        ? (vsTokens ?? vsSet)
        : (vsSet ?? vsTokens)
    const conditionalOn: 'set' | 'tokens' | undefined = !reactTable
      ? undefined
      : reactTable === vsSet
        ? 'set'
        : 'tokens'
    const baseTable = belowHalf
      ? (def.movesBelowHalf ?? def.moves)
      : whenAlone
        ? (def.movesWhenAlone ?? def.moves)
        : def.moves

    let rng = s.rng
    let nextPatternIndex = enemy.patternIndex
    // 通常分岐: sequence を持つ敵は固定ローテーション
    let move: EnemyMove
    if (sequence && sequence.length > 0) {
      // sequenceLoopFrom (2026-09-02): 一度きりの前奏→ループ。最後まで進んだら loopFrom へ戻る
      const loopFrom = belowHalf
        ? (def.sequenceBelowHalfLoopFrom ?? 0)
        : whenAlone || phaseSwitched
          ? 0
          : (def.sequenceLoopFrom ?? 0)
      const len = sequence.length
      const idx =
        enemy.patternIndex < len
          ? enemy.patternIndex
          : loopFrom + ((enemy.patternIndex - loopFrom) % (len - loopFrom))
      const moveId = sequence[idx]
      const found = baseTable.find((m) => m.id === moveId)
      if (!found) throw new Error(`敵 ${def.id} の sequence が未定義の行動を参照: ${moveId}`)
      move = found
      nextPatternIndex = enemy.patternIndex + 1
    } else if (enemy.patternIndex === 0 && def.opener !== undefined && !belowHalf && !whenAlone) {
      // 初手固定 (2026-09-02 StS2行動文法): 最初の宣言だけ指定の行動 = その敵の問いをT1に見せる
      const found = baseTable.find((m) => m.id === def.opener)
      if (!found) throw new Error(`敵 ${def.id} の opener が未定義の行動を参照: ${def.opener}`)
      move = found
      nextPatternIndex = enemy.patternIndex + 1
    } else {
      // noRepeat=直前と同じ技は引かない / once=1戦闘1回 (2026-09-02 StS2の「読める揺らぎ」)。
      // 除外で候補が空になる時は制約なしで引く (スタール防止)
      const usable = baseTable.filter(
        (m) =>
          !(m.once === true && (enemy.usedOnce ?? []).includes(m.id)) &&
          !(m.noRepeat === true && m.id === enemy.lastMoveId),
      )
      const table = usable.length > 0 ? usable : baseTable
      const [moveIdx, rngAfter] = weightedIndex(rng, table.map((m) => m.weight))
      move = table[moveIdx]
      rng = rngAfter
      nextPatternIndex = enemy.patternIndex + 1
    }
    // 回数カウンタ: 対象行動の宣言を数え、しきい値到達の瞬間に patternIndex を 0 へ
    // (次の宣言から phaseAfterUses.sequence の先頭で始まる)
    const pau = def.phaseAfterUses
    let nextKeyUses = enemy.keyMoveUses ?? 0
    if (pau !== undefined && !phaseSwitched && move.id === pau.moveId) {
      nextKeyUses += 1
      if (nextKeyUses >= pau.uses) nextPatternIndex = 0
    }
    const usesSoFar = enemy.moveGrowth?.[move.id] ?? 0
    const [intent, rngA] = buildIntent(rng, move, enemy.strength, enemy.atkScale ?? 1, usesSoFar)
    rng = rngA
    const growsMove = move.growPerUse !== undefined || move.growHitsPerUse !== undefined
    const nextGrowth = growsMove ? { ...(enemy.moveGrowth ?? {}), [move.id]: usesSoFar + 1 } : enemy.moveGrowth

    let alt: ReturnType<typeof buildIntent>[0] | undefined
    let condOn = conditionalOn
    if (reactTable && reactTable.length > 0) {
      const [altIdx, rngB] = weightedIndex(rng, reactTable.map((m) => m.weight))
      rng = rngB
      const [altIntent, rngC] = buildIntent(rng, reactTable[altIdx], enemy.strength, enemy.atkScale ?? 1)
      rng = rngC
      alt = altIntent
    } else if (!belowHalf && !whenAlone && enemy.noReactTable !== true && move.setAlt !== undefined) {
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
        ...(sa.alsoBuff !== undefined ? { alsoBuff: sa.alsoBuff } : {}),
      }
      const [altIntent, rngC] = buildIntent(rng, altMove, enemy.strength, enemy.atkScale ?? 1)
      rng = rngC
      alt = { ...altIntent, ...(sa.ignoreFreshness === true ? { ignoreFreshness: true } : {}) }
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
            keyMoveUses: nextKeyUses,
            lastMoveId: move.id,
            ...(nextGrowth !== undefined ? { moveGrowth: nextGrowth } : {}),
            ...(move.once === true ? { usedOnce: [...(e.usedOnce ?? []), move.id] } : {}),
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
  atkScale = 1,
  uses = 0,
): readonly [
  {
    kind: EnemyMove['kind']
    shownMin: number
    shownMax: number
    actual: number
    hits?: number
    inflict?: StatusInflict
    alsoDefend?: number
    alsoBuff?: number
  },
  GameState['rng'],
] {
  // 技の恒久成長 (2026-09-02): 宣言回数×growPerUse を min/max に、×growHitsPerUse をヒット数に加算
  const grow = (move.growPerUse ?? 0) * uses
  const growHits = (move.growHitsPerUse ?? 0) * uses
  const gMin = move.min !== undefined ? move.min + grow : undefined
  const gMax = move.max !== undefined ? move.max + grow : undefined
  const gHits = move.hits !== undefined || growHits > 0 ? (move.hits ?? 1) + growHits : undefined
  let actual = 0
  let next = rng
  if (gMin !== undefined && gMax !== undefined) {
    ;[actual, next] = nextInt(rng, gMin, gMax)
  }
  const bonus = move.kind === 'attack' ? strength : 0
  // 打点倍率 (幕2/3+15%): 攻撃の基礎値だけに乗算・四捨五入。強化は倍率の後に加算 =
  // 幅表示・実値・per-hit のすべてに同じ規則で効く (alsoDefend・付与量は対象外)
  const scale = (v: number) => (move.kind === 'attack' ? Math.round(v * atkScale) : v)
  const clamp = (v: number) => (move.kind === 'attack' ? Math.max(1, v) : v)
  return [
    {
      kind: move.kind,
      shownMin: clamp(scale(gMin ?? 0) + bonus),
      shownMax: clamp(scale(gMax ?? 0) + bonus),
      actual: clamp(scale(actual) + bonus),
      hits: gHits,
      ...(move.mirrorHits === true ? { mirrorHits: true } : {}),
      inflict: move.inflict,
      alsoDefend: move.alsoDefend,
      ...(move.alsoBuff !== undefined ? { alsoBuff: move.alsoBuff } : {}),
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
      energyMaxAtTurnStart: state.player.energyMax + (state.energyMaxRefBonus ?? 0), // 大樹の心: 上限参照札が読む値に+N
      cardsPlayedThisTurn: 0,
      setsThisTurn: 0,
      playsThisTurn: 0,
      attacksPlayedThisTurn: 0,
      weakFreshThisPhase: 0,
      freeResetUid: undefined,
      // 見切り (2026-08-30): 前のターンから置きっぱなしの伏せ札は「織り込み済み」になる
      setCards: state.player.setCards.map((c) => (c.setFresh ? { ...c, setFresh: false } : c)),
    },
  }
  // ターン装甲の累計リセット (2026-09-02): 自ターン開始〜次の自ターン開始が「1ターン」
  s = { ...s, enemies: s.enemies.map((e) => ((e.damageThisTurn ?? 0) > 0 ? { ...e, damageThisTurn: 0 } : e)) }
  s = emit(s, { type: 'TurnStarted', turn })
  // ドローを onTurnStart 誘発より先に行う (2026-08-31 変更)。
  // 手札参照の置物 (懐深き外套=手札×N氷壁) が「まだ0枚の手札」を読むのを防ぐ。
  // 泉 (onTurnStart ドロー) 等は順序が変わっても合計枚数は同じ = 既存挙動と等価
  // 霞み (2026-09-02): ドロー-2・最低3枚 (完全ゼロ化はしない = 全捨てルールと衝突するため)
  s = drawCards(s, (s.player.mist ?? 0) > 0 ? Math.max(3, s.player.drawPerTurn - 2) : s.player.drawPerTurn)
  s = runPermanentTriggers(s, 'onTurnStart', Math.max(0, s.enemies.findIndex((e) => e.hp > 0)))
  // ターン開始誘発 (従者の自動攻撃など) で敵が全滅したら即座に勝利を確定する
  // (プレイテストで発見: 判定がないと撃破済みの敵に手札が撃てる状態が残る)
  s = checkCombatEnd(s)
  if (s.phase === 'won' || s.phase === 'lost') return s
  return declareIntents(s)
}

/** 勝敗判定。すでに決着済みなら何もしない (CombatEnded の二重記録防止) */
/**
 * 分裂 (2026-09-02): 倒れた分裂親から小型を場に出す。勝利判定より先に走る =
 * 親を倒しても分裂体が残れば戦闘は続く。分裂体は生成時に意図を宣言し、その敵フェーズから行動する
 * (本家Slime準拠)。分裂体は素の値・親の atkScale (難易度) を継承・patternIndex をずらして同期を防ぐ
 */
function processSplits(state: GameState): GameState {
  let s = state
  for (let i = 0; i < s.enemies.length; i++) {
    const e = s.enemies[i]
    if (e.hp > 0 || e.split === true || e.fled === true) continue
    const def = getEnemyDef(e.enemyId)
    const splitInto = def.splitInto
    if (splitInto === undefined) continue
    s = { ...s, enemies: s.enemies.map((x, j) => (j === i ? { ...x, split: true } : x)) }
    s = emit(s, { type: 'EnemySplit', enemyIndex: i, into: splitInto.enemyId, count: splitInto.count })
    const childDef = getEnemyDef(splitInto.enemyId)
    // HPスケール継承 (2026-09-02 代替ボス「蘇る合成獣」で発見): 分裂体が素のHPで出ると、ボス係数
    // (×2.4) や幕・難易度倍率を受けた親の後継が桁違いに軟らかくなる。親の実効倍率 (maxHp/素)
    // を子にも掛ける = 残機チェーンの合計HPが幕係数どおりに払われる
    const hpRatio = def.maxHp > 0 ? e.maxHp / def.maxHp : 1
    const scaledChildHp = Math.max(1, Math.round(childDef.maxHp * hpRatio))
    for (let k = 0; k < splitInto.count; k++) {
      const moveId = childDef.sequence?.[k % (childDef.sequence.length || 1)]
      const move = childDef.moves.find((m) => m.id === moveId) ?? childDef.moves[0]
      // stunned (2026-09-02 罰型分裂の緩和版): 分裂体の初回意図は「隙」= 出現ターンは動かない
      const childStrength = splitInto.strength ?? 0
      const [intent, rng2] =
        splitInto.stunned === true
          ? ([
              { kind: 'rest' as const, shownMin: 0, shownMax: 0, actual: 0 },
              s.rng,
            ] as const)
          : buildIntent(s.rng, move, childStrength, e.atkScale ?? 1)
      const child = {
        enemyId: splitInto.enemyId,
        hp: scaledChildHp,
        maxHp: scaledChildHp,
        block: childDef.burrow?.block ?? childDef.startingBlock ?? 0,
        ...(childDef.burrow ? { burrowActive: true } : {}),
        intent,
        strength: childStrength,
        ...(e.atkScale !== undefined ? { atkScale: e.atkScale } : {}),
        burn: 0,
        confusion: 0,
        exposed: 0,
        patternIndex: splitInto.stunned === true ? 0 : (k + 1) % (childDef.sequence?.length ?? 1),
        ...(childDef.thorns !== undefined ? { thorns: childDef.thorns } : {}),
        ...(childDef.artifact !== undefined ? { artifact: childDef.artifact } : {}),
        ...(childDef.armor !== undefined ? { armor: childDef.armor } : {}),
      }
      s = { ...s, rng: rng2, enemies: [...s.enemies, child] }
      s = emit(s, { type: 'EnemyIntentDeclared', enemyIndex: s.enemies.length - 1, intent })
    }
  }
  return s
}

/**
 * 弔い強化 (2026-09-02 連携の逆問い): 仲間が倒れるたび (逃走は除く)、生存する
 * mournStrength 持ちの筋力+N。「同時に削って同時に落とせ」= 全体攻撃が構造的な最適解になる
 */
function processMourning(state: GameState): GameState {
  let s = state
  for (let i = 0; i < s.enemies.length; i++) {
    const dead = s.enemies[i]
    if (dead.hp > 0 || dead.fled === true || dead.mournProcessed === true) continue
    s = { ...s, enemies: s.enemies.map((x, j) => (j === i ? { ...x, mournProcessed: true } : x)) }
    for (let j = 0; j < s.enemies.length; j++) {
      if (j === i || s.enemies[j].hp <= 0) continue
      const amount = getEnemyDef(s.enemies[j].enemyId).mournStrength
      if (amount === undefined || amount <= 0) continue
      s = {
        ...s,
        enemies: s.enemies.map((x, k) => (k === j ? { ...x, strength: x.strength + amount } : x)),
      }
      s = emit(s, { type: 'StrengthGained', enemyIndex: j, amount, reason: 'mourn' })
    }
  }
  return s
}

/**
 * 潜伏の殻が自ターン中に割れた敵の意図をその場で噛みつきに差し替える (2026-09-03 本家 Burrowed
 * 「割れた瞬間に潜行攻撃へ移行」)。プレイヤーの行動が原因で、差し替え後の意図は自ターン中に見える =
 * 宣言時固定則の例外だが「窓が嘘をつかない」は保たれる
 */
export function applyPendingBites(state: GameState): GameState {
  if (state.phase !== 'player-turn') return state
  let s = state
  for (let i = 0; i < s.enemies.length; i++) {
    const e = s.enemies[i]
    if (e.hp <= 0 || e.biteNext !== true) continue
    const def = getEnemyDef(e.enemyId)
    const bite = def.burrow ? def.moves.find((m) => m.id === def.burrow!.bite) : undefined
    if (!bite) continue
    const [intent, rng] = buildIntent(s.rng, bite, e.strength, e.atkScale ?? 1)
    s = {
      ...s,
      rng,
      enemies: s.enemies.map((x, j) => (j === i ? { ...x, intent, biteNext: false } : x)),
    }
    s = emit(s, { type: 'EnemyIntentDeclared', enemyIndex: i, intent })
  }
  return s
}

export function checkCombatEnd(state: GameState): GameState {
  state = applyPendingBites(state)
  if (state.phase === 'won' || state.phase === 'lost') return state
  state = processSplits(state)
  state = processMourning(state)
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
/** 山札/捨て札から選ぶ効果の種別 (引導・回収・サーチ)。1枚の札は1種だけ持てる */
export function deckChooseKindOf(
  def: CardDef,
): 'exhaustFromDeckChoose' | 'retrieveFromDiscard' | 'searchDeck' | null {
  for (const k of ['exhaustFromDeckChoose', 'retrieveFromDiscard', 'searchDeck'] as const) {
    if (def.effects.some((e) => e.effect === k && e.trigger === 'onPlay')) return k
  }
  return null
}

export function playCard(
  state: GameState,
  cardUid: string,
  modeIndex?: number,
  discardUids?: readonly string[],
  targetIndex?: number,
  exhaustUids?: readonly string[],
  retrieveUid?: string,
  deckUids?: readonly string[],
  handUids?: readonly string[],
  xAmount?: number,
): GameState {
  if (state.phase !== 'player-turn') throw new Error('自ターン以外はカードをプレイできない')
  const card = state.player.hand.find((c) => c.uid === cardUid)
  if (!card) throw new Error(`手札にないカード: ${cardUid}`)
  if (!isPlayableFromHand(card)) throw new Error(`${card.def.name} はプレイ不可 (リアクション専用)`)
  // 拘束 (2026-09-02): 1ターンにプレイできるカードは上限枚数まで。伏せ・発動は制限しない。
  // 参照は実プレイ枚数 (playsThisTurn) — 焚べ (addCasts) の嵩で拘束が早く詰まらない
  if (state.player.restrain > 0 && (state.player.playsThisTurn ?? 0) >= RESTRAIN_PLAY_CAP) {
    throw new Error(`拘束中は1ターンに${RESTRAIN_PLAY_CAP}枚までしかプレイできない (すでに${RESTRAIN_PLAY_CAP}枚プレイ済み)`)
  }
  // マナ軽減トークン適用後の実効コストで支払う (素のコスト0は割引を消費しない)
  const cost = effectiveCost(state, card)
  const consumesDiscount =
    card.def.cost > 0 &&
    state.player.nextCardDiscount > 0 &&
    card.def.xCost !== true &&
    card.freeThisCombat !== true // 屍集めの0E札は割引を消費しない (素の0Eと同じ扱い)
  if (cost > state.player.energy) throw new Error(`エナジー不足: ${card.def.name}`)
  // Xコスト: 支払った量を xHits 効果の繰り返し回数として展開する (多段ヒットと同じ解決)
  // Xコスト: 払う量は 1〜現在のエナジーから選ぶ (省略=全部。2026-09-03 上限4は同日撤廃=本家形で効率側を合わせた)
  const xCap = state.player.energy
  if (card.def.xCost === true && xAmount !== undefined) {
    if (!Number.isInteger(xAmount) || xAmount < 1 || xAmount > xCap) {
      throw new Error(`${card.def.name} の X は 1〜${xCap} で指定する (xAmount=${xAmount})`)
    }
  }
  const paidX = card.def.xCost === true ? (xAmount ?? cost) : 0
  const expandX = (effects: readonly DeclarativeEffect[]): readonly DeclarativeEffect[] =>
    paidX === 0
      ? effects
      : effects.flatMap((e) =>
          e.xHits === true ? Array.from({ length: paidX }, () => ({ ...e, xHits: undefined })) : [e],
        )
  let effCard: CardInstance =
    paidX === 0 ? card : { ...card, def: { ...card.def, effects: expandX(card.def.effects) } }
  // 骨刃の強化 (急所読み等の empowerShivs): ナイフトークンのダメージに常在ボーナスを注入
  if (card.def.shivToken === true) {
    const shivBonus = state.player.permanents.reduce(
      (a, p) =>
        a +
        p.def.effects
          .filter((e) => e.effect === 'empowerShivs')
          .reduce((x, e) => x + (e.amount ?? 0), 0),
      0,
    )
    if (shivBonus > 0) {
      effCard = {
        ...effCard,
        def: {
          ...effCard.def,
          effects: effCard.def.effects.map((e) =>
            e.effect === 'dealDamage' ? { ...e, amount: (e.amount ?? 0) + shivBonus } : e,
          ),
        },
      }
    }
  }

  // 育つ札 (growSelf 2026-09-02 Rampage型): この戦闘で積み上げた加算を dealDamage に注入する
  if ((card.growBonus ?? 0) > 0) {
    const g = card.growBonus ?? 0
    effCard = {
      ...effCard,
      def: {
        ...effCard.def,
        effects: effCard.def.effects.map((e) =>
          e.effect === 'dealDamage' && e.trigger === 'onPlay' ? { ...e, amount: (e.amount ?? 0) + g } : e,
        ),
      },
    }
  }

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

  // 引導 (2026-08-31): 山札か捨て札から選んで消滅させる札 (deckUids) の検証。
  // 両山が空なら選択なしでプレイできる (残りの効果だけ解決 = 空撃ちを禁止しない)
  // 2026-09-02 汎用化: 回収 (retrieveFromDiscard=捨て札) / サーチ (searchDeck=山札) も同じ deckUids 欄で選ぶ。
  // 1枚の札はこの3種のうち1つしか持てない (cardrules.test で機械固定)
  const chooseKind = deckChooseKindOf(card.def)
  const deckChooseN = card.def.effects
    .filter((e) => e.effect === chooseKind)
    .reduce((a, e) => a + (e.amount ?? 1), 0)
  const deckPool =
    chooseKind === 'retrieveFromDiscard'
      ? state.player.discardPile
      : chooseKind === 'searchDeck'
        ? state.player.drawPile
        : [...state.player.drawPile, ...state.player.discardPile]
  const poolLabel =
    chooseKind === 'retrieveFromDiscard' ? '捨て札' : chooseKind === 'searchDeck' ? '山札' : '山札か捨て札'
  const deckChooseUids = deckChooseN > 0 ? (deckUids ?? []) : []
  if (deckChooseN > 0) {
    const need = Math.min(deckChooseN, deckPool.length)
    if (deckChooseUids.length !== need) {
      throw new Error(`${card.def.name} は${poolLabel}から${need}枚の指定 (deckUids) が必要`)
    }
    if (new Set(deckChooseUids).size !== deckChooseUids.length) {
      throw new Error('deckUids の指定が重複している')
    }
    for (const uid of deckChooseUids) {
      if (!deckPool.some((c) => c.uid === uid)) {
        throw new Error(`${poolLabel}に無いカード: ${uid}`)
      }
    }
  }
  // 手札で鍛える (upgradeInHand 2026-09-02 Armaments型): 自身以外の鍛えられる手札から選ぶ。候補が無ければ省略可
  const upgradeN = card.def.effects
    .filter((e) => e.effect === 'upgradeInHand' && e.trigger === 'onPlay')
    .reduce((a, e) => a + (e.amount ?? 1), 0)
  const upgradable = upgradeN > 0 ? state.player.hand.filter((c) => c.uid !== card.uid && canUpgradeInHand(c)) : []
  const upgradeUids = upgradeN > 0 ? (handUids ?? []) : []
  if (upgradeN > 0) {
    const need = Math.min(upgradeN, upgradable.length)
    if (upgradeUids.length !== need) {
      throw new Error(`${card.def.name} は手札から${need}枚の指定 (handUids) が必要`)
    }
    if (new Set(upgradeUids).size !== upgradeUids.length) throw new Error('handUids の指定が重複している')
    for (const uid of upgradeUids) {
      if (!upgradable.some((c) => c.uid === uid)) throw new Error(`鍛えられる手札に無いカード: ${uid}`)
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
  // 庇う (2026-09-02): 護衛が生存中、単体対象は護衛に向かう (対象ごとリダイレクト =
  // ダメージ以外の単体効果 [延焼・急所等] も護衛が受ける。全体攻撃は targetIndex を使わないので素通し)
  let redirectedFrom: number | undefined
  if (targetIndex !== undefined) {
    const t = state.enemies[targetIndex]
    if (t && getEnemyDef(t.enemyId).guardian !== true) {
      const g = state.enemies.findIndex((e) => e.hp > 0 && getEnemyDef(e.enemyId).guardian === true)
      if (g >= 0 && g !== targetIndex) {
        redirectedFrom = targetIndex
        targetIndex = g
      }
    }
  }
  const enemyIndex = targetIndex ?? state.enemies.findIndex((e) => e.hp > 0)
  const isPermanent = card.def.type === 'permanent'
  // 樹液 (2026-09-03 本家 Dropkick 型): 急所を持つ敵が生存していれば消滅しない
  const isExhaust =
    card.def.exhaust === true &&
    !(card.def.exhaustUnlessExposedEnemy === true && state.enemies.some((e) => e.hp > 0 && e.exposed > 0))
  const removed = new Set([cardUid, ...discards, ...exhausts])
  const discardedCards = state.player.hand.filter((c) => discards.includes(c.uid))
  const exhaustedCards = state.player.hand.filter((c) => exhausts.includes(c.uid))
  let s: GameState = {
    ...state,
    player: {
      ...state.player,
      energy: state.player.energy - (card.def.xCost === true ? paidX : cost),
      nextCardDiscount: consumesDiscount ? 0 : state.player.nextCardDiscount,
      hand: state.player.hand.filter((c) => !removed.has(c.uid)),
      // プレイ中のカードはまだ捨て札に置かない (limbo)。効果解決中のドローが捨て札を
      // 再シャッフルすると自分自身を引き直せてしまう (2026-08-31 黒Opusラン発見:
      // 闇の契約を撃った同ターンに闇の契約を引いた)。解決後に置く = StS準拠
      // 火傷は捨て札に入らない = どの経路でも捨てられたら消える (2026-09-02 レビュー是正:
      // 「支払いに使えば疼く前に処分できる」の設計注記どおり、処分=最終処理にする)
      discardPile: [
        ...state.player.discardPile,
        ...discardedCards.filter((c) => c.def.id !== SCALD_DEF.id),
      ],
      permanents: isPermanent ? [...state.player.permanents, card] : state.player.permanents,
      // プレイした消滅札自身も limbo (効果解決後に消滅置き場へ)。2026-08-31 黒Opusラン発見:
      // 影の刃 (消滅・自傷2) が「消滅回復→自傷」の順に解決され、とばりの回復が常に満タンで無駄になっていた
      exhaustPile: [...state.player.exhaustPile, ...exhaustedCards],
    },
  }
  if (discardedCards.length > 0) {
    s = emit(s, { type: 'CardsDiscarded', cardIds: discardedCards.map((c) => c.def.id) })
  }
  s = emit(s, { type: 'CardPlayed', cardId: card.def.id })
  if (redirectedFrom !== undefined) {
    // 庇う (2026-09-02 検証ラン「リダイレクトが無言で起きる」への処方): 発生を必ずログに残す
    s = emit(s, { type: 'GuardianRedirected', fromIndex: redirectedFrom, toIndex: enemyIndex })
  }
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
  // 亡骸効果: 消滅コストで支払われた札は「プレイ以外の経路」なので発火する
  s = fireNecroEffects(s, exhaustedCards, enemyIndex)
  // 引導 (黒 2026-08-31): 山札か捨て札から選んだ札を消滅させる。効果解決の前に行う =
  // 直後のドロー効果が選んだ札を手札へ引き込む競合を防ぐ。亡骸・onCardExhausted は発火する
  // (プレイ以外の経路)。反復 (echo) されても選択消滅は1回 (選んだ札は1枚しか無い)
  if (deckChooseUids.length > 0 && chooseKind !== 'exhaustFromDeckChoose') {
    // 回収 (捨て札→手札) / サーチ (山札→手札)。効果解決の前に手札へ = 直後のドロー・参照と競合しない。
    // 山札の並びは崩さない (抜くだけ) = 引き順は伏せたまま
    const chosenSet = new Set(deckChooseUids)
    const fromDraw = chooseKind === 'searchDeck'
    const source = fromDraw ? s.player.drawPile : s.player.discardPile
    const moved = source.filter((c) => chosenSet.has(c.uid))
    s = {
      ...s,
      player: {
        ...s.player,
        drawPile: fromDraw ? s.player.drawPile.filter((c) => !chosenSet.has(c.uid)) : s.player.drawPile,
        discardPile: fromDraw ? s.player.discardPile : s.player.discardPile.filter((c) => !chosenSet.has(c.uid)),
        hand: [...s.player.hand, ...moved],
      },
    }
    s = emit(s, { type: 'CardsMovedToHand', cardIds: moved.map((c) => c.def.id), from: fromDraw ? 'draw' : 'discard' })
  }
  if (upgradeUids.length > 0) {
    // 手札で鍛える: 焚き火と同じ upgradeCard を手札のインスタンスに適用 (この戦闘限り = 戦闘終了で作り直される)
    const set = new Set(upgradeUids)
    s = {
      ...s,
      player: { ...s.player, hand: s.player.hand.map((c) => (set.has(c.uid) ? upgradeCard(c) : c)) },
    }
    for (const c of s.player.hand.filter((c) => set.has(c.uid))) s = emit(s, { type: 'CardUpgradedInHand', cardId: c.def.id })
  }
  if (deckChooseUids.length > 0 && chooseKind === 'exhaustFromDeckChoose') {
    const chosenSet = new Set(deckChooseUids)
    const chosenCards = [...s.player.drawPile, ...s.player.discardPile].filter((c) =>
      chosenSet.has(c.uid),
    )
    s = {
      ...s,
      player: {
        ...s.player,
        drawPile: s.player.drawPile.filter((c) => !chosenSet.has(c.uid)),
        discardPile: s.player.discardPile.filter((c) => !chosenSet.has(c.uid)),
        exhaustPile: [...s.player.exhaustPile, ...chosenCards],
      },
    }
    for (const c of chosenCards) s = emit(s, { type: 'CardExhausted', cardId: c.def.id })
    s = fireExhaustTriggers(s, chosenCards.length, enemyIndex)
    s = fireNecroEffects(s, chosenCards, enemyIndex)
  }
  // 反復 (青の呪文コピー 2026-08-31): 呪文なら反復トークンを1つ消費し、効果を「2回」解決する。
  // 消費は解決前 = 反復札自身が反復された場合、自分の生むトークンを自分で食わない (+2が立つ)。
  // 詠唱数・onAttackPlayed等のプレイ誘発は1回のまま (プレイは1回。効果だけが2回) = StSのBurst/Amplify準拠
  const echoed = card.def.type === 'spell' && state.player.spellEchoes > 0
  if (echoed) {
    s = { ...s, player: { ...s.player, spellEchoes: s.player.spellEchoes - 1 } }
    s = emit(s, { type: 'SpellEchoed', cardId: card.def.id })
  }
  for (let echoPass = 0; echoPass < (echoed ? 2 : 1); echoPass++) {
    if (chosenMode) {
      // 虚弱の判定用フラグ (resolveOnPlayEffects と同じ扱い。モード効果もカードのプレイ)
      s = { ...s, resolvingCardPlay: true }
      for (const effect of chosenMode.effects) {
        s = resolveEffectTargeted(s, effect, enemyIndex)
      }
      s = { ...s, resolvingCardPlay: false }
    } else {
      s = resolveOnPlayEffects(s, effCard, enemyIndex)
    }
  }
  // 「攻撃プレイ後」誘発: 解決した効果にダメージが含まれていたか (物理・呪文を問わない)
  const resolvedEffects = chosenMode ? chosenMode.effects : effCard.def.effects.filter((e) => e.trigger === 'onPlay')
  if (resolvedEffects.some(isDamageEffect)) {
    // 攻撃数参照 (2026-09-03): 自身の解決後に加算 = 薙ぎ払いは「自分より前にプレイした攻撃」を数える
    s = { ...s, player: { ...s.player, attacksPlayedThisTurn: (s.player.attacksPlayedThisTurn ?? 0) + 1 } }
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
      playsThisTurn: (s.player.playsThisTurn ?? 0) + 1,
    },
  }
  s = tickCardTimers(s)
  // 増殖 (addCopyToDiscard 2026-09-02 Anger型): このカードのコピーを捨て札に加える (この戦闘限りのトークン扱い)
  const hasCardOps = card.def.effects.some((e) => e.effect === 'addCopyToDiscard' || e.effect === 'growSelf')
  const copies = !hasCardOps
    ? 0
    : card.def.effects
        .filter((e) => e.effect === 'addCopyToDiscard' && e.trigger === 'onPlay')
        .reduce((a, e) => a + (e.amount ?? 1), 0)
  if (copies > 0) {
    const made: CardInstance[] = Array.from({ length: copies }, (_, i) => ({
      uid: `copy_${s.eventLog.length}_${i}_${card.def.id}`,
      def: card.def,
      token: true,
    }))
    s = { ...s, player: { ...s.player, discardPile: [...s.player.discardPile, ...made] } }
    s = emit(s, { type: 'CardCopied', cardId: card.def.id, count: copies })
  }
  // 育つ札 (growSelf): 解決後に加算を積む。捨て札へ置くインスタンスに乗せる = 次にプレイした時に注入される
  const grow = !hasCardOps
    ? 0
    : card.def.effects
        .filter((e) => e.effect === 'growSelf' && e.trigger === 'onPlay')
        .reduce((a, e) => a + (e.amount ?? 0), 0)
  const landed: CardInstance = grow > 0 ? { ...card, growBonus: (card.growBonus ?? 0) + grow } : card
  if (grow > 0) s = emit(s, { type: 'CardGrew', cardId: card.def.id, bonus: landed.growBonus ?? 0 })
  // limbo からの着地: プレイし終えたカードをここで捨て札 (消滅札は消滅置き場) へ置く。
  // 消滅の誘発も解決後 = 「使用後、この戦闘から除外」の語義どおり (2026-08-31 順序是正)
  if (isExhaust) {
    s = { ...s, player: { ...s.player, exhaustPile: [...s.player.exhaustPile, landed] } }
    s = emit(s, { type: 'CardExhausted', cardId: card.def.id })
    s = fireExhaustTriggers(s, 1, enemyIndex)
  } else if (!isPermanent) {
    s = { ...s, player: { ...s.player, discardPile: [...s.player.discardPile, landed] } }
  }
  // 屍集め: 消滅置き場から手札へ戻す (墓地燃料が減る代わりの再利用。確定済みルール表「コスト再利用」)。
  // 戻した札はこの戦闘中0E (2026-08-31 rework。亡骸に役割を吸われ一度もプレイされなかった実測への処方
  // = 「任意の札を選べてテンポも付く」で住み分ける)。Xコスト札は対象外
  if (isRetrieve && retrieveUid !== undefined) {
    const chosen = s.player.exhaustPile.find((c) => c.uid === retrieveUid)
    if (chosen) {
      const free = chosen.def.xCost !== true
      s = {
        ...s,
        player: {
          ...s.player,
          exhaustPile: s.player.exhaustPile.filter((c) => c.uid !== retrieveUid),
          hand: [...s.player.hand, free ? { ...chosen, freeThisCombat: true } : chosen],
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
      playsThisTurn: (s.player.playsThisTurn ?? 0) + 1,
    },
  }
  s = tickCardTimers(s)
    }
  }
  return checkCombatEnd(s)
}

/**
 * 亡骸プレイ (黒 2026-08-31): 消滅置き場の necroCost 持ち札を一度だけプレイする。
 * プレイ後はゲームから完全に取り除かれる = 刻 (消滅置き場参照) の燃料も減る緊張。
 * 割引 (discountNext) の対象外。反復 (spellEchoes) の対象外 (死者再生の直接プレイと同じ扱い)。
 * necroCost 持ち札は modes / xCost / 追加コストを持たない単純な札に限る (cardrules で機械固定)
 */
export function playNecro(state: GameState, cardUid: string, targetIndex?: number): GameState {
  if (state.phase !== 'player-turn') throw new Error('自ターン以外はカードをプレイできない')
  const card = state.player.exhaustPile.find((c) => c.uid === cardUid)
  if (!card) throw new Error(`消滅置き場にないカード: ${cardUid}`)
  const cost = card.def.necroCost
  if (cost === undefined) throw new Error(`${card.def.name} は亡骸プレイを持たない`)
  // 拘束は亡骸プレイにも効く (2026-09-02 レビュー是正: プレイヤー発行のプレイは全て上限の内。
  // 効果由来の直接プレイ〔死者再生〕はカード解決の途中なので止めない = 本家Havoc型の裁定)
  if (state.player.restrain > 0 && (state.player.playsThisTurn ?? 0) >= RESTRAIN_PLAY_CAP) {
    throw new Error(`拘束中は1ターンに${RESTRAIN_PLAY_CAP}枚までしかプレイできない (すでに${RESTRAIN_PLAY_CAP}枚プレイ済み)`)
  }
  if (cost > state.player.energy) throw new Error(`エナジー不足: ${card.def.name}`)
  const aliveCount = state.enemies.filter((e) => e.hp > 0).length
  if (targetIndex !== undefined) {
    const target = state.enemies[targetIndex]
    if (!target || target.hp <= 0) throw new Error(`不正な対象: ${targetIndex}`)
  }
  if (targetIndex === undefined && aliveCount > 1 && cardNeedsTarget(card)) {
    throw new Error(`${card.def.name} は対象の指定 (targetIndex) が必要`)
  }
  const enemyIndex = targetIndex ?? state.enemies.findIndex((e) => e.hp > 0)
  let s: GameState = {
    ...state,
    player: {
      ...state.player,
      energy: state.player.energy - cost,
      // ゲームから完全に取り除く (消滅置き場にも戻らない)
      exhaustPile: state.player.exhaustPile.filter((c) => c.uid !== cardUid),
    },
  }
  s = emit(s, { type: 'NecroPlayed', cardId: card.def.id })
  s = emit(s, { type: 'CardPlayed', cardId: card.def.id })
  s = resolveOnPlayEffects(s, card, enemyIndex)
  if (card.def.effects.filter((e) => e.trigger === 'onPlay').some(isDamageEffect)) {
    s = runPermanentTriggers(s, 'onAttackPlayed', enemyIndex)
    s = fireSelfSetTriggers(s, 'onAttackPlayed', enemyIndex)
  }
  s = runPermanentTriggers(s, 'onCardPlayed', enemyIndex)
  if (card.def.type === 'spell') {
    s = fireSelfSetTriggers(s, 'onSpellPlayed', enemyIndex)
    s = runPermanentTriggers(s, 'onSpellPlayed', enemyIndex)
  }
  // 亡骸プレイも「プレイ」として詠唱数に数える (直接プレイと同じ既存則)
  s = {
    ...s,
    player: {
      ...s.player,
      cardsPlayedThisTurn: s.player.cardsPlayedThisTurn + 1,
      cardsPlayedTotal: s.player.cardsPlayedTotal + 1,
      playsThisTurn: (s.player.playsThisTurn ?? 0) + 1,
    },
  }
  s = tickCardTimers(s)
  return checkCombatEnd(s)
}

/** EndTurn: 勢いリセット・衝動の失効・延焼処理をして、敵フェーズを解決する */
export function endTurn(state: GameState): GameState {
  if (state.phase !== 'player-turn') throw new Error('自ターン以外はターン終了できない')
  let s = emit(state, { type: 'TurnEnded', turn: state.turn })
  // 勢いは自ターン終了時にリセット (確定済みルール表「勢い」)。
  // 弱体・虚弱もここで1減る — 作用するフェーズ (自ターン) の終了時に減る対称則 (確定済みルール表「状態異常」)
  s = {
    ...s,
    player: {
      ...s.player,
      momentum: 0,
      spellEchoes: 0,
      weak: Math.max(0, s.player.weak - 1),
      frail: Math.max(0, s.player.frail - 1),
      restrain: Math.max(0, s.player.restrain - 1),
      mist: Math.max(0, (s.player.mist ?? 0) - 1),
    },
  }
  // 火傷・烙印 (2026-09-02): 自ターン終了時に手札にあると疼く (火傷=2/枚・烙印=1/枚)。
  // ブロックで防げない直接HP損失。自傷 (selfHpLost) には数えない = 敵由来の痛み。
  // 捨てコスト・消滅コストの支払いに使えば疼く前に処分できる (手札マネジメントの問い)
  {
    const scalds = s.player.hand.filter((c) => c.def.id === SCALD_DEF.id).length
    const brands = s.player.hand.filter(
      (c) => c.def.id === BRAND_DEF.id || c.def.id === GUILT_DEF.id,
    ).length
    const burnHp = scalds * 2 + brands * 1
    if (burnHp > 0) {
      s = { ...s, player: { ...s.player, hp: s.player.hp - burnHp } }
      s = emit(s, { type: 'ScaldTick', count: scalds + brands, amount: burnHp })
      s = checkCombatEnd(s)
      if (s.phase === 'lost') return s
    }
  }
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
    const aliveIdx = Math.max(0, s.enemies.findIndex((e) => e.hp > 0))
    s = fireExhaustTriggers(s, expired.length, aliveIdx)
    // 亡骸効果: 衝動で引いた亡骸札の失効もプレイ以外の消滅なので発火する
    s = fireNecroEffects(s, expired, aliveIdx)
  }
  // 敵ブロックはこのタイミングで失効 (前の敵ターンの防御は自ターンの攻撃を受け止めたら役目を終える)
  // 潜伏の殻 (burrowActive) はブロックの器を借りているだけで失効しない (2026-09-04 Opusラン O: 殻30が敵フェーズ開始の掃除で消え「潜伏中(殻0)」の矛盾)
  s = { ...s, enemies: s.enemies.map((e) => (e.burrowActive === true ? e : { ...e, block: 0 })) }
  // 憤怒 (逆上) の参照値はフェーズ単位: 敵フェーズ開始時にリセットして受け直す
  s = { ...s, player: { ...s.player, damageTakenLastEnemyPhase: 0, attacksReceivedThisPhase: 0 } }
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
        j === i
          ? {
              ...e,
              hp: e.hp - amount,
              burn: Math.max(0, e.burn - decay),
              // 延焼ティックも与ダメ系カウンタに算入する (2026-09-02 ミニングで発見した盲点の是正。
              // 撃破サマリーの延焼算入 (2026-08-31) と同じ裁定「ダメージはダメージ」——
              // バーンは regenBreak (再生止め) の解答になれる代わりに、与ダメ激昂のタイマーを進める
              damageTakenTotal: (e.damageTakenTotal ?? 0) + amount,
              hpLostSinceRegen: (e.hpLostSinceRegen ?? 0) + amount,
            }
          : e,
      ),
    }
    s = emit(s, { type: 'BurnTick', enemyIndex: i, amount })
    s = applyWakeCheck(s, i) // 被弾覚醒はどの経路の被弾でも (2026-09-02)
    // 与ダメ激昂の壁跨ぎ (effects.ts の dealDamageToEnemy と同則)
    {
      const struck = s.enemies[i]
      const defE = getEnemyDef(struck.enemyId)
      if (defE.enrageEveryDamage !== undefined && struck.hp > 0) {
        const before = (struck.damageTakenTotal ?? 0) - amount
        const crossings =
          Math.floor((struck.damageTakenTotal ?? 0) / defE.enrageEveryDamage) -
          Math.floor(before / defE.enrageEveryDamage)
        const gain = crossings * (defE.enrage ?? 2)
        if (gain > 0) {
          s = {
            ...s,
            enemies: s.enemies.map((e, j) => (j === i ? { ...e, strength: e.strength + gain } : e)),
          }
          s = emit(s, { type: 'StrengthGained', enemyIndex: i, amount: gain, reason: 'enrage-damage' })
        }
      }
    }
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
    actual: act.actual,
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
      ...(acting.mirrorHits === true ? { mirrorHits: true } : {}),
      ...(acting.inflict !== undefined ? { inflict: acting.inflict } : {}),
      ...(acting.alsoDefend !== undefined ? { alsoDefend: acting.alsoDefend } : {}),
      ...(acting.alsoBuff !== undefined ? { alsoBuff: acting.alsoBuff } : {}),
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
/** 火傷の1戦闘あたり上限 (負傷と同思想のハメ防止) */
const SCALD_CAP = 5

/** 状態異常をプレイヤーに付与する。weak/vulnerable はカウンター加算、wound は死に札を捨て札に混入 */
/** 拘束中に1ターンでプレイできるカードの上限 (本家StS2 Sloth=「4枚目以降プレイ不可」準拠) */
export const RESTRAIN_PLAY_CAP = 3

function applyStatusToPlayer(state: GameState, inflict: StatusInflict): GameState {
  const { status, amount } = inflict
  if (status === 'mist') {
    return emit(
      { ...state, player: { ...state.player, mist: (state.player.mist ?? 0) + amount } },
      { type: 'StatusInflicted', status, amount },
    )
  }
  if (status === 'slow') {
    // justAppliedガードは脆弱と同則 (敵フェーズ中の付与は同フェーズ末の減衰を免除)
    return emit(
      { ...state, player: { ...state.player, slow: (state.player.slow ?? 0) + amount, slowFresh: true } },
      { type: 'StatusInflicted', status, amount },
    )
  }
  if (status === 'weak' || status === 'vulnerable' || status === 'frail' || status === 'restrain') {
    const player =
      status === 'weak'
        ? { ...state.player, weak: state.player.weak + amount, weakFreshThisPhase: (state.player.weakFreshThisPhase ?? 0) + amount } // 敵行動由来の付与のみ通る経路。同フェーズの返しには乗らない (自ターン開始でリセット)
        : status === 'vulnerable'
          // 付与ガード (2026-09-02 本家StSのjustApplied準拠。ミニングで発見した1スタック蒸発の修正):
          // 敵フェーズ中に付与された脆弱は、その同じフェーズの終了時減衰をスキップする
          ? { ...state.player, vulnerable: state.player.vulnerable + amount, vulnerableFresh: true }
          : status === 'frail'
            ? { ...state.player, frail: state.player.frail + amount }
            : { ...state.player, restrain: state.player.restrain + amount }
    return emit({ ...state, player }, { type: 'StatusInflicted', status, amount })
  }
  if (status === 'scald') {
    // 火傷 (2026-09-02): 手札に直接押し込む = 即時の圧。上限5枚/戦闘は累計で数える
    // (火傷札は1自ターンで消えるため、山のカウントでは上限が意味を失う)
    const existing = state.player.scaldsThisCombat ?? 0
    const add = Math.min(amount, SCALD_CAP - existing)
    if (add <= 0) return state
    const scalds = Array.from({ length: add }, (_, i) => ({
      uid: `${SCALD_DEF.id}#${existing + i}_t${state.turn}`,
      def: SCALD_DEF,
      scaldFresh: true,
    }))
    const s2: GameState = {
      ...state,
      player: {
        ...state.player,
        hand: [...state.player.hand, ...scalds],
        scaldsThisCombat: existing + add,
      },
    }
    return emit(s2, { type: 'StatusInflicted', status: 'scald', amount: add })
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
    let base = { ...state, negateNextAction: false }
    // 盗みは宣言と同時に成立する (発火保証パッケージ) が、打ち消しに成功したら抱えた分を
    // 取り戻す (2026-08-31 青Opusラン指摘: 確認ウィンドウが「打ち消せる」と提示するのに
    // 実行が no-op で、取り返せると誤解させていた。打ち消し=盗みの解除、で表示と実体を揃える)
    const eff = effectiveIntent(state, enemyIndex)
    if (eff?.kind === 'steal-gold' && (enemy.stolenGold ?? 0) > 0) {
      base = {
        ...base,
        enemies: base.enemies.map((e, i) =>
          i === enemyIndex
            ? { ...e, stolenGold: Math.max(0, (e.stolenGold ?? 0) - eff.actual) }
            : e,
        ),
      }
    }
    const negated = emit(base, { type: 'ActionNegated', enemyIndex })
    return runPermanentTriggers(negated, 'onActionNegated', enemyIndex)
  }
  const intent = effectiveIntent(state, enemyIndex)!
  // 解決した行動を記録する (post窓の誘発判定に使う)
  const markResolved = (s: GameState, hpLoss: number): GameState => ({
    ...s,
    lastAction: { enemyIndex, kind: intent.kind, hpLoss, actual: intent.actual },
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
      // 手数の鏡 (物真似 2026-08-31): 実行時のヒット数 = このターンにプレイした枚数 (最低1)。
      // 敵フェーズ中は cardsPlayedThisTurn がまだこのターンの値を保持している
      // 伏せも手数に数える (2026-09-02 ユーザー裁定: 「伏せ+置物で鏡を最小化」が最適解になった抜け道を閉じる)
      const hits =
        intent.mirrorHits === true
          ? Math.max(1, state.player.cardsPlayedThisTurn + (state.player.setsThisTurn ?? 0))
          : (intent.hits ?? 1)
      let block = state.player.block
      let iceBlock = state.player.iceBlock
      let dealtTotal = 0
      let hpLoss = 0
      for (let h = 0; h < hits; h++) {
        // 威嚇 (延焼による攻撃弱体) は撤去済み: 実値をそのまま使う (2026-08-25)
        let v = intent.actual
        // 威圧 (2026-09-03 本家 Weak 化): スタックがあれば各ヒット-25% (切り捨て・最低1)。行動が終わると1減る
        v = applyEnemyWeak(v, state.enemies[enemyIndex]?.weak)
        // 静かな鈴 (C型レリック): 伏せ札がある間、各ヒット-N (最低1クランプは威圧と同則)
        if ((state.setDamageReduction ?? 0) > 0 && state.player.setCards.length > 0) {
          v = Math.max(1, v - (state.setDamageReduction ?? 0))
        }
        // 脆弱: 敵の攻撃ダメージ50%増 (切り捨て)
        if (state.player.vulnerable > 0) v = Math.floor(v * 1.5)
        // 重り (2026-09-02 StS2 SlowPower式): +10%×このターンのプレイ枚数 (切り捨て)。
        // 手数の罰の被弾版 = 鏡 (ヒット数) と別の受け方を要求する
        if ((state.player.slow ?? 0) > 0 && (state.player.playsThisTurn ?? 0) > 0) {
          v = Math.floor(v * (1 + 0.1 * (state.player.playsThisTurn ?? 0)))
        }
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
          attacksReceivedThisPhase: (state.player.attacksReceivedThisPhase ?? 0) + 1,
        },
      }
      s = emit(s, { type: 'DamageDealt', source: 'enemy', amount: dealtTotal, hpLoss, enemyIndex })
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
      // 攻撃と同時の強化 (alsoBuff 2026-09-01): バフ専用ターン=無償ターンを作らずに雪だるまを見せる。
      // 打ち消せば強化ごと消える (行動単位の無効化に自然に乗る)
      if (intent.alsoBuff !== undefined && s.enemies[enemyIndex] && s.enemies[enemyIndex].hp > 0) {
        s = {
          ...s,
          enemies: s.enemies.map((e, j) =>
            j === enemyIndex ? { ...e, strength: e.strength + intent.alsoBuff! } : e,
          ),
        }
        s = emit(s, { type: 'StrengthGained', enemyIndex, amount: intent.alsoBuff })
      }
      // 威圧の消費: 攻撃行動を1回実行するたび1減る (多段は1行動で1)
      s = {
        ...s,
        enemies: s.enemies.map((e, j) => (j === enemyIndex && (e.weak ?? 0) > 0 ? { ...e, weak: e.weak! - 1 } : e)),
      }
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
      let s = emit({ ...state, enemies }, { type: 'BlockGained', target: 'enemy', amount: intent.actual })
      // 防御と同時の強化 (2026-09-03 用心深い影「隠れる」: 今守らせる代わりに次の斬撃が重くなる = 伏せ分岐を
      // 「押せるスイッチ」から交換に変える。攻撃の alsoBuff と同じく打ち消せば強化ごと消える)
      if (intent.alsoBuff !== undefined && s.enemies[enemyIndex] && s.enemies[enemyIndex].hp > 0) {
        s = {
          ...s,
          enemies: s.enemies.map((e, j) =>
            j === enemyIndex ? { ...e, strength: e.strength + intent.alsoBuff! } : e,
          ),
        }
        s = emit(s, { type: 'StrengthGained', enemyIndex, amount: intent.alsoBuff })
      }
      return markResolved(s, 0)
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
    case 'hatch': {
      // 孵化 (2026-09-02 StS2 ToughEgg式): この敵が hatchInto の敵へ変身する。
      // HP全快・筋力0・ローテ先頭から。atkScale (難易度) は継承。打ち消せば1ターン遅らせられる
      // (卵のローテを sequenceLoopFrom で孵化に巻き戻しておくと「次も孵化」= 遅延は1ターン単位)
      const def = getEnemyDef(enemy.enemyId)
      const into = def.hatchInto
      if (into === undefined) return markResolved(state, 0)
      const newDef = getEnemyDef(into.enemyId)
      // HPスケール継承 (分裂体と同じ裁定 2026-09-02): 卵の実効倍率を孵化後にも掛ける
      const hatchRatio = def.maxHp > 0 ? enemy.maxHp / def.maxHp : 1
      const hatchedHp = Math.max(1, Math.round(newDef.maxHp * hatchRatio))
      let s: GameState = {
        ...state,
        enemies: state.enemies.map((e, j) =>
          j === enemyIndex
            ? {
                ...e,
                enemyId: into.enemyId,
                hp: hatchedHp,
                maxHp: hatchedHp,
                block: 0,
                strength: 0,
                patternIndex: 0,
                keyMoveUses: 0,
                lastMoveId: undefined,
                usedOnce: [],
                moveGrowth: {},
                woken: false,
                ...(newDef.artifact !== undefined ? { artifact: newDef.artifact } : { artifact: 0 }),
                intent: null,
              }
            : e,
        ),
      }
      s = emit(s, { type: 'EnemyHatched', enemyIndex, fromId: def.id, intoId: into.enemyId })
      // 生まれた姿で即座に意図を宣言する (分裂と同じ「その敵フェーズから行動」の一貫則は
      // 取らない — 孵化はその敵の「行動」自体なので、次の宣言フェーズで初手が決まる)
      return markResolved(s, 0)
    }
    case 'mill': {
      // 山札喰い (2026-08-31 大喰らいの蟲): 山札の上N枚を消滅させる。
      // 亡骸・onCardExhausted は発火する (ミルの既存則) = 黒の墓地デッキには部分的な追い風
      // というマッチアップの色も込み。打ち消し可 (negateNextAction は冒頭で処理済み)
      return markResolved(millPlayerDeck(state, intent.actual, enemyIndex), 0)
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
  // 守り成功参照 (棘の返礼 2026-09-03): この敵フェーズに攻撃を1回以上受け、HP損失が0なら「完全に凌いだ」
  s = {
    ...s,
    player: {
      ...s.player,
      perfectBlockLastPhase: (s.player.attacksReceivedThisPhase ?? 0) > 0 && s.player.damageTakenLastEnemyPhase === 0,
    },
  }
  s = dispatchHooks(s, ended) // 空振り (ReactionWhiffed) の計上は方式固有
  // 脆弱は作用するフェーズ (敵フェーズ) の終了時に1減る (確定済みルール表「状態異常」)。
  // ただしこのフェーズに付与された分は減らさない (justAppliedガード 2026-09-02 —
  // 旧実装は「付与→同フェーズ末に即-1」で脆弱2が実効1になっていた)
  s = {
    ...s,
    player: {
      ...s.player,
      vulnerable:
        s.player.vulnerableFresh === true
          ? s.player.vulnerable
          : Math.max(0, s.player.vulnerable - 1),
      vulnerableFresh: false,
      slow: s.player.slowFresh === true ? (s.player.slow ?? 0) : Math.max(0, (s.player.slow ?? 0) - 1),
      slowFresh: false,
    },
  }
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
      s = emit(s, { type: 'StrengthGained', enemyIndex: i, amount, reason: 'enrage-phase' })
    }
  }
  // 火傷の生存則 (2026-09-02 修正。StS2解析ミニングで発見した二重の乖離への処方):
  // ①このフェーズに注入された火傷 (scaldFresh) は全捨てを生き残って次の自ターンの手札を圧迫する
  //   — 旧実装は注入→同フェーズ末の全捨てで即捨て札行きになり「手数を奪う」設計が machine 上
  //   一度も機能していなかった ②自ターンを過ごした火傷は全捨てで消える = 1回きり
  //   — 旧実装は捨て札を循環する本家Burn型の恒久汚染で、仕様「全捨てで消える」と乖離していた
  s = {
    ...s,
    player: {
      ...s.player,
      // 保持 (retain 2026-09-02): 全捨てで手札に残る
      hand: s.player.hand
        .filter((c) => (c.def.id === SCALD_DEF.id && c.scaldFresh === true) || c.def.retain === true)
        .map((c) => (c.scaldFresh === true ? { ...c, scaldFresh: false } : c)),
      discardPile: [
        ...s.player.discardPile,
        ...s.player.hand.filter((c) => c.def.id !== SCALD_DEF.id && c.def.retain !== true),
      ],
    },
  }
  return startPlayerTurn(s, s.turn + 1)
}

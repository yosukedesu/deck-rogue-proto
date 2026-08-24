// engine/effects.ts — 宣言的効果 (DeclarativeEffect) の解決
// カード効果は data/*.json の宣言的記述をここで状態遷移に変換する。
// 表現できない効果だけ scriptId で名前付きスクリプトに逃がす (現状は未登録)。

import { emit } from './events.ts'
import { nextInt, shuffle } from './rng.ts'
import type { CardInstance, DeclarativeEffect, EnemyActionKind, GameState } from './types.ts'

/**
 * 実効コスト: マナ軽減トークン (nextCardDiscount) を適用したプレイコスト。
 * 素のコスト0のカードは割引を消費しない (対象外)。
 */
export function effectiveCost(state: GameState, card: CardInstance): number {
  if (card.def.cost === 0) return 0
  return Math.max(0, card.def.cost - state.player.nextCardDiscount)
}

/** 自ターンにプレイ可能なカードか。リアクションタイプは false。置物・選択式は常にプレイ可能 */
export function isPlayableFromHand(card: CardInstance): boolean {
  if (card.def.type === 'reaction') return false
  return (
    card.def.type === 'permanent' ||
    (card.def.modes?.length ?? 0) > 0 ||
    card.def.effects.some((e) => e.trigger === 'onPlay')
  )
}

/** ダメージを与える効果か (「攻撃プレイ後」誘発の判定に使う) */
export function isDamageEffect(effect: DeclarativeEffect): boolean {
  return [
    'dealDamage',
    'dealDamageRandom',
    'dealDamagePerCardPlayed',
    'dealDamagePerEnergyMax',
    'dischargeAether',
    'dischargeGrowth',
    'dealDamageCleave',
    'dealDamagePerBlock',
    'dealDamagePerPermanent',
    'dealDamageDrain',
    'dealDamagePerExhaust',
    'counter',
  ].includes(effect.effect)
}

/** 敵1体を対象に取る効果 (target:'all' を除く)。StS式ターゲティングの要否判定に使う */
const ENEMY_TARGETED = new Set([
  'dealDamage',
  'dealDamageRandom',
  'dealDamagePerCardPlayed',
  'dealDamagePerEnergyMax',
  'dischargeAether',
  'applyBurn',
  'shatterBlock',
  'confuse',
  'exposeEnemy',
  'dischargeGrowth',
  'dealDamageCleave',
  'weakenEnemy',
  'dealDamagePerBlock',
  'dealDamagePerPermanent',
  'dealDamageDrain',
  'dealDamagePerExhaust',
])

/**
 * このカードのプレイに対象指定 (targetIndex) が要るか。
 * 生存敵が2体以上いる時、単体対象効果を含むカードは対象必須 (確定済みルール表「ターゲティング」)
 */
export function cardNeedsTarget(card: CardInstance, modeIndex?: number): boolean {
  const modes = card.def.modes ?? []
  const effects =
    modes.length > 0 && modeIndex !== undefined && modes[modeIndex]
      ? modes[modeIndex].effects
      : card.def.effects.filter((e) => e.trigger === 'onPlay')
  return effects.some((e) => ENEMY_TARGETED.has(e.effect) && e.target !== 'all')
}

/** 効果1つを対象規則に従って解決する。target:'all' は生存する敵全体に順に解決 (確定済みルール表「全体攻撃」) */
export function resolveEffectTargeted(
  state: GameState,
  effect: DeclarativeEffect,
  enemyIndex: number,
): GameState {
  if (effect.target !== 'all') return resolveEffect(state, effect, enemyIndex)
  let s = state
  for (let i = 0; i < s.enemies.length; i++) {
    if (s.enemies[i].hp > 0) s = resolveEffect(s, effect, i)
  }
  return s
}

/**
 * リアクションの誘発窓。
 * pre = 敵の行動の確定時・実行前 (打ち消し onEnemyAction / 軽減 onAttackIncoming)
 * post = 敵の行動の解決後 (返し onAttacked / onEnemyBuffed / onEnemyDefended。条件判定に hpLoss を使う)
 */
export type ReactionWindow =
  | { readonly stage: 'pre'; readonly kind: EnemyActionKind; readonly actual: number }
  | { readonly stage: 'post'; readonly kind: EnemyActionKind; readonly hpLoss: number }

/** この誘発窓でカードが発動できるか (トリガー一致 + 追加条件) */
export function reactionMatches(state: GameState, card: CardInstance, win: ReactionWindow): boolean {
  return card.def.effects.some((e) => {
    const triggerMatches =
      win.stage === 'pre'
        ? e.trigger === 'onEnemyAction' || (e.trigger === 'onAttackIncoming' && win.kind === 'attack')
        : (e.trigger === 'onAttacked' && win.kind === 'attack') ||
          (e.trigger === 'onEnemyBuffed' && (win.kind === 'buff' || win.kind === 'rally')) ||
          (e.trigger === 'onEnemyDefended' && win.kind === 'defend')
    if (!triggerMatches) return false
    const c = e.condition
    if (!c) return true
    if (c.hpAtOrBelowRatio !== undefined && state.player.hp > state.player.maxHp * c.hpAtOrBelowRatio) {
      return false
    }
    if (c.minDamageTaken !== undefined && (win.stage !== 'post' || win.hpLoss < c.minDamageTaken)) {
      return false
    }
    if (c.maxActionValue !== undefined && (win.stage !== 'pre' || win.actual > c.maxActionValue)) {
      return false
    }
    return true
  })
}

/**
 * 威嚇 (延焼の怯み): 延焼を持つ敵の攻撃は延焼2につき実値-1 (軽減上限-4・下限1)。
 * 赤の防御代替 (確定済みルール表「威嚇」)。実行時・窓を開いた時点の延焼値で計算する。
 * 上限-4は延焼特化による攻撃無力化 (sim全勝) を防ぐキャップ。攻撃以外の行動値には作用しない。
 */
export function intimidatedActionValue(kind: EnemyActionKind, actual: number, burn: number): number {
  if (kind !== 'attack') return actual
  const reduction = Math.min(4, Math.floor(burn / 2))
  return Math.max(1, actual - reduction)
}

/** 現在の中断状態 (pendingWindow) から誘発窓を復元する */
export function windowFromPending(state: GameState): ReactionWindow | null {
  const pending = state.pendingWindow
  if (!pending) return null
  const enemy = state.enemies[pending.enemyIndex]
  const intent = enemy?.intent
  if (!intent) return null
  if (pending.stage === 'pre') {
    // 実値公開・行動値条件は威嚇後の値を使う (確定済みルール表「威嚇」)
    const actual = intimidatedActionValue(intent.kind, intent.actual, enemy.burn)
    return { stage: 'pre', kind: intent.kind, actual }
  }
  return { stage: 'post', kind: intent.kind, hpLoss: state.lastAction?.hpLoss ?? 0 }
}

/**
 * プレイヤーの与ダメージ処理。成長カウンターと勢いを加算する
 * (確定済みルール表「成長カウンター」「勢い」。勢いは自ターン終了時リセットのため実質攻撃のみに乗る)。
 * 敵ブロックで軽減。pierce (貫通/トランプル) は敵ブロックを無視する (確定済みルール表「貫通」)。
 */
export function dealDamageToEnemy(
  state: GameState,
  enemyIndex: number,
  baseAmount: number,
  pierce = false,
): GameState {
  let amount = baseAmount + state.player.growth + state.player.momentum
  // 弱体: プレイヤーの与ダメージ25%減 (切り捨て。確定済みルール表「状態異常」)
  if (state.player.weak > 0) amount = Math.floor(amount * 0.75)
  const enemy = state.enemies[enemyIndex]
  if (!enemy || enemy.hp <= 0) return state
  // 急所 (敵版脆弱): 次に受けるダメージN回が+50% (1ヒットごとに1減。確定済みルール表「急所」)
  const exposed = enemy.exposed > 0
  if (exposed) amount = Math.floor(amount * 1.5)
  const blocked = pierce ? 0 : Math.min(enemy.block, amount)
  const hpLoss = amount - blocked
  const enemies = state.enemies.map((e, i) =>
    i === enemyIndex
      ? { ...e, block: e.block - blocked, hp: e.hp - hpLoss, exposed: exposed ? e.exposed - 1 : e.exposed }
      : e,
  )
  return emit({ ...state, enemies }, { type: 'DamageDealt', source: 'player', amount, hpLoss })
}

/** 山札から n 枚ドロー。山札が尽きたら捨て札をシャッフルして山札に戻す (StS準拠) */
export function drawCards(state: GameState, n: number): GameState {
  let drawPile = [...state.player.drawPile]
  let discardPile = [...state.player.discardPile]
  let rng = state.rng
  const drawn: CardInstance[] = []
  for (let i = 0; i < n; i++) {
    if (drawPile.length === 0) {
      if (discardPile.length === 0) break
      const [reshuffled, nextRng] = shuffle(rng, discardPile)
      drawPile = [...reshuffled]
      discardPile = []
      rng = nextRng
    }
    drawn.push(drawPile.shift()!)
  }
  const next: GameState = {
    ...state,
    rng,
    player: { ...state.player, drawPile, discardPile, hand: [...state.player.hand, ...drawn] },
  }
  return drawn.length > 0 ? emit(next, { type: 'CardsDrawn', count: drawn.length }) : next
}

/**
 * 宣言的効果1つを解決する。enemyIndex は対象の敵 (単体戦なので実質 0)。
 * リアクション効果 (counter / negate) もここで解決される。
 */
export function resolveEffect(state: GameState, effect: DeclarativeEffect, enemyIndex: number): GameState {
  switch (effect.effect) {
    case 'dealDamage':
      return dealDamageToEnemy(state, enemyIndex, effect.amount ?? 0, effect.pierce)
    case 'dealDamagePerEnergyMax':
      // ビッグマナのシグネチャー: エナジー上限 × amount のダメージ
      return dealDamageToEnemy(
        state,
        enemyIndex,
        (effect.amount ?? 0) * state.player.energyMax,
        effect.pierce,
      )
    case 'counter': {
      // 返しダメージ: 行動してきた敵へのダメージ。成長は乗るが勢いは乗らない
      // (勢いは自ターン終了時にリセット済みのため、敵ターンの返しには自然と乗らない)
      return dealDamageToEnemy(state, enemyIndex, effect.amount ?? 0, effect.pierce)
    }
    case 'gainEnergy': {
      // 一時マナ: ターン終了までエナジー+X (energyMax は増えない)
      const amount = effect.amount ?? 0
      const next = { ...state, player: { ...state.player, energy: state.player.energy + amount } }
      return emit(next, { type: 'EnergyGained', amount })
    }
    case 'addMomentum': {
      const amount = effect.amount ?? 0
      const next = { ...state, player: { ...state.player, momentum: state.player.momentum + amount } }
      return emit(next, { type: 'MomentumAdded', amount })
    }
    case 'gainBlock': {
      const amount = effect.amount ?? 0
      const next = { ...state, player: { ...state.player, block: state.player.block + amount } }
      return emit(next, { type: 'BlockGained', target: 'player', amount })
    }
    case 'gainIceBlock': {
      // 氷壁 (青): ターン開始で消えず持ち越されるブロック
      const amount = effect.amount ?? 0
      const next = { ...state, player: { ...state.player, iceBlock: state.player.iceBlock + amount } }
      return emit(next, { type: 'IceBlockGained', amount })
    }
    case 'dealDamagePerCardPlayed':
      // ストーム攻撃 (青): 詠唱数 × amount のダメージ
      return dealDamageToEnemy(
        state,
        enemyIndex,
        (effect.amount ?? 0) * state.player.cardsPlayedThisTurn,
        effect.pierce,
      )
    case 'gainIceBlockPerCardPlayed': {
      // ストーム防御 (青): 詠唱数 × amount の氷壁
      const amount = (effect.amount ?? 0) * state.player.cardsPlayedThisTurn
      if (amount === 0) return state
      const next = { ...state, player: { ...state.player, iceBlock: state.player.iceBlock + amount } }
      return emit(next, { type: 'IceBlockGained', amount })
    }
    case 'drawCardsPerCardPlayed':
      // ストームドロー (青): 詠唱数 × amount 枚ドロー
      return drawCards(state, (effect.amount ?? 0) * state.player.cardsPlayedThisTurn)
    case 'addAether': {
      // 霊気 (青): 妨害・リアクション成功の蓄積
      const amount = effect.amount ?? 0
      const next = { ...state, player: { ...state.player, aether: state.player.aether + amount } }
      return emit(next, { type: 'AetherGained', amount })
    }
    case 'applyBurn': {
      // 延焼 (赤): 敵に蓄積する継続ダメージ
      const amount = effect.amount ?? 0
      const enemy = state.enemies[enemyIndex]
      if (!enemy || enemy.hp <= 0) return state
      const enemies = state.enemies.map((e, i) =>
        i === enemyIndex ? { ...e, burn: e.burn + amount } : e,
      )
      return emit({ ...state, enemies }, { type: 'BurnApplied', enemyIndex, amount })
    }
    case 'confuse': {
      // 混乱 (青): 敵の攻撃が他の生存敵 (いなければ自分) に向かう (確定済みルール表「混乱」)
      const amount = effect.amount ?? 0
      const enemy = state.enemies[enemyIndex]
      if (!enemy || enemy.hp <= 0) return state
      const enemies = state.enemies.map((e, i) =>
        i === enemyIndex ? { ...e, confusion: e.confusion + amount } : e,
      )
      return emit({ ...state, enemies }, { type: 'EnemyConfused', enemyIndex, amount })
    }
    case 'gainHp': {
      // 回復 (白の専売。確定済みルール表「回復」)
      const amount = Math.min(effect.amount ?? 0, state.player.maxHp - state.player.hp)
      if (amount <= 0) return state
      const next = { ...state, player: { ...state.player, hp: state.player.hp + amount } }
      return emit(next, { type: 'HpHealed', amount })
    }
    case 'weakenEnemy': {
      // 威圧 (白): 敵の強化を下げる (確定済みルール表「威圧（白）」)
      const amount = effect.amount ?? 0
      const enemy = state.enemies[enemyIndex]
      if (!enemy || enemy.hp <= 0) return state
      const enemies = state.enemies.map((e, i) =>
        i === enemyIndex ? { ...e, strength: e.strength - amount } : e,
      )
      return emit({ ...state, enemies }, { type: 'EnemyWeakened', enemyIndex, amount })
    }
    case 'dealDamagePerBlock':
      // 要塞型ペイオフ: 現在のブロック×X (確定済みルール表「ブロック変換」)
      return dealDamageToEnemy(
        state,
        enemyIndex,
        (effect.amount ?? 0) * state.player.block,
        effect.pierce,
      )
    case 'dealDamagePerPermanent':
      // 集結 (白): 置物の数×X (確定済みルール表「従者（置物数参照）」)
      return dealDamageToEnemy(
        state,
        enemyIndex,
        (effect.amount ?? 0) * state.player.permanents.length,
        effect.pierce,
      )
    case 'dealDamageDrain': {
      // ドレイン (黒の専売): Xダメージ + floor(X/2)回復 (確定済みルール表「黒の柱」)
      const amount = effect.amount ?? 0
      let s = dealDamageToEnemy(state, enemyIndex, amount, effect.pierce)
      const heal = Math.min(Math.floor(amount / 2), s.player.maxHp - s.player.hp)
      if (heal > 0) {
        s = { ...s, player: { ...s.player, hp: s.player.hp + heal } }
        s = emit(s, { type: 'HpHealed', amount: heal })
      }
      return s
    }
    case 'exhaustFromDeck': {
      // 忘却 (黒): 山札の上X枚を消滅させる。捨て札はリシャッフルで空になるため、
      // 墓地=消滅置き場とする (単調増加。デッキを永久燃料にする緊張感。戦闘内限定)
      const n = Math.min(effect.amount ?? 0, state.player.drawPile.length)
      if (n <= 0) return state
      const milled = state.player.drawPile.slice(0, n)
      const s: GameState = {
        ...state,
        player: {
          ...state.player,
          drawPile: state.player.drawPile.slice(n),
          exhaustPile: [...state.player.exhaustPile, ...milled],
        },
      }
      return emit(s, { type: 'CardsMilled', count: n })
    }
    case 'dealDamagePerExhaust':
      // 墓地参照 (黒): 消滅した枚数×X (確定済みルール表「黒の柱」)
      return dealDamageToEnemy(
        state,
        enemyIndex,
        (effect.amount ?? 0) * state.player.exhaustPile.length,
        effect.pierce,
      )
    case 'exposeEnemy': {
      // 急所 (敵版脆弱): 次に受けるプレイヤーダメージN回が+50% (確定済みルール表「急所」)
      const amount = effect.amount ?? 0
      const enemy = state.enemies[enemyIndex]
      if (!enemy || enemy.hp <= 0) return state
      const enemies = state.enemies.map((e, i) =>
        i === enemyIndex ? { ...e, exposed: e.exposed + amount } : e,
      )
      return emit({ ...state, enemies }, { type: 'ExposedApplied', enemyIndex, amount })
    }
    case 'dischargeGrowth': {
      // 成長放出 (緑): 成長×amount のダメージを与え、成長を全て失う (確定済みルール表「成長放出」)
      const spent = state.player.growth
      let s = dealDamageToEnemy(state, enemyIndex, spent * (effect.amount ?? 0), effect.pierce)
      s = { ...s, player: { ...s.player, growth: 0 } }
      return emit(s, { type: 'GrowthDischarged', spent })
    }
    case 'dealDamageCleave': {
      // キル連鎖: 対象にXダメージ。倒れたら別の生存敵に同値 (確定済みルール表「キル連鎖」)
      let s = dealDamageToEnemy(state, enemyIndex, effect.amount ?? 0, effect.pierce)
      if (s.enemies[enemyIndex] && s.enemies[enemyIndex].hp <= 0) {
        const nextIdx = s.enemies.findIndex((e, i) => i !== enemyIndex && e.hp > 0)
        if (nextIdx >= 0) s = dealDamageToEnemy(s, nextIdx, effect.amount ?? 0, effect.pierce)
      }
      return s
    }
    case 'shatterBlock': {
      // 粉砕 (赤): 敵のブロックを全て破壊する
      const enemy = state.enemies[enemyIndex]
      if (!enemy || enemy.block === 0) return state
      const enemies = state.enemies.map((e, i) => (i === enemyIndex ? { ...e, block: 0 } : e))
      return emit(
        { ...state, enemies },
        { type: 'BlockShattered', enemyIndex, amount: enemy.block },
      )
    }
    case 'dealDamageRandom': {
      // ランダム火力 (赤): amount〜amountMax のロール (シードRNG)
      const [roll, rng] = nextInt(state.rng, effect.amount ?? 0, effect.amountMax ?? effect.amount ?? 0)
      return dealDamageToEnemy({ ...state, rng }, enemyIndex, roll, effect.pierce)
    }
    case 'impulseDraw': {
      // 衝動 (赤): 山札の上からX枚を「このターン限り」の手札に加える
      const before = new Set(state.player.hand.map((c) => c.uid))
      let s = drawCards(state, effect.amount ?? 0)
      const drawnUids = s.player.hand.filter((c) => !before.has(c.uid)).map((c) => c.uid)
      if (drawnUids.length === 0) return s
      s = {
        ...s,
        player: { ...s.player, impulseUids: [...s.player.impulseUids, ...drawnUids] },
      }
      return emit(s, { type: 'ImpulseDrawn', count: drawnUids.length })
    }
    case 'loseHp': {
      // 自傷 (赤): ブロックを無視して自分のHPを失う
      const amount = effect.amount ?? 0
      const next = { ...state, player: { ...state.player, hp: state.player.hp - amount } }
      return emit(next, { type: 'HpLost', amount })
    }
    case 'discountNext': {
      // マナ軽減トークン: 次にプレイする1枚のコスト-X (消費は combat.ts の playCard 側)
      const amount = effect.amount ?? 0
      const next = {
        ...state,
        player: { ...state.player, nextCardDiscount: state.player.nextCardDiscount + amount },
      }
      return emit(next, { type: 'DiscountGained', amount })
    }
    case 'dischargeAether': {
      // 霊気放出 (青): 霊気×amount のダメージを与え、霊気を全消費
      const spent = state.player.aether
      if (spent === 0) return state
      let s: GameState = { ...state, player: { ...state.player, aether: 0 } }
      s = emit(s, { type: 'AetherDischarged', spent })
      return dealDamageToEnemy(s, enemyIndex, spent * (effect.amount ?? 0), effect.pierce)
    }
    case 'gainEnergyMax': {
      // 緑の柱①ランプ: 上限のみ増える。恩恵は次の自ターンから
      // (即時利用は 2026-08-23 に廃止。プレイしたターンのテンポ損がランプの対価)
      const amount = effect.amount ?? 0
      const next = {
        ...state,
        player: { ...state.player, energyMax: state.player.energyMax + amount },
      }
      return emit(next, { type: 'EnergyMaxGained', amount })
    }
    case 'addGrowth': {
      const amount = effect.amount ?? 0
      const next = { ...state, player: { ...state.player, growth: state.player.growth + amount } }
      return emit(next, { type: 'GrowthAdded', amount })
    }
    case 'doubleGrowth': {
      // 成長スタックのシグネチャー: 現在の成長カウンターを2倍にする
      const amount = state.player.growth
      if (amount === 0) return state
      const next = { ...state, player: { ...state.player, growth: state.player.growth * 2 } }
      return emit(next, { type: 'GrowthAdded', amount })
    }
    case 'drawCards':
      return drawCards(state, effect.amount ?? 0)
    case 'negate':
      // 打ち消し: 次の敵行動を無効化する汎用フラグを立てる (対象は任意の行動)
      return { ...state, negateNextAction: true }
    case 'script':
      throw new Error(`未登録のスクリプト効果: ${effect.scriptId}`)
  }
}

/** カードの onPlay 効果を順に解決 (target:'all' は全体解決) */
export function resolveOnPlayEffects(state: GameState, card: CardInstance, enemyIndex: number): GameState {
  let s = state
  for (const effect of card.def.effects) {
    if (effect.trigger === 'onPlay') s = resolveEffectTargeted(s, effect, enemyIndex)
  }
  return s
}

const REACTION_TRIGGERS = new Set([
  'onAttackIncoming',
  'onAttacked',
  'onEnemyAction',
  'onEnemyBuffed',
  'onEnemyDefended',
])

/**
 * カードのリアクション効果を順に解決し、ReactionTriggered を記録する。
 * カードの移動 (伏せ場/手札→捨て札) とコスト処理は方式固有のため
 * 呼び出し側 (ReactionSystem 実装) が行う。
 */
export function resolveReactionEffects(state: GameState, card: CardInstance, enemyIndex: number): GameState {
  let s = emit(state, { type: 'ReactionTriggered', cardId: card.def.id, mode: state.reactionMode })
  for (const effect of card.def.effects) {
    if (REACTION_TRIGGERS.has(effect.trigger)) {
      // target:'all' の返し (茨の爆ぜ) は生存全体に解決する
      s = resolveEffectTargeted(s, effect, enemyIndex)
    }
  }
  return s
}

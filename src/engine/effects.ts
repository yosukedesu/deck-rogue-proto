// engine/effects.ts — 宣言的効果 (DeclarativeEffect) の解決
// カード効果は data/*.json の宣言的記述をここで状態遷移に変換する。
// 表現できない効果だけ scriptId で名前付きスクリプトに逃がす (現状は未登録)。

import { getCardDef } from './content.ts'
import { emit } from './events.ts'
import { nextInt, shuffle } from './rng.ts'
import type {
  CardInstance,
  DeclarativeEffect,
  EnemyActionKind,
  EnemyIntent,
  GameState,
} from './types.ts'

/**
 * 実効コスト: マナ軽減トークン (nextCardDiscount) を適用したプレイコスト。
 * 素のコスト0のカードは割引を消費しない (対象外)。
 */
export function effectiveCost(state: GameState, card: CardInstance): number {
  // Xコスト: 現在のエナジーを全て支払う (最低1 = エナジー0ではプレイ不可)。割引の対象外
  if (card.def.xCost === true) return Math.max(1, state.player.energy)
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
    'dealDamagePerMomentum',
    'dischargeAether',
    'dischargeGrowth',
    'dealDamageCleave',
    'dealDamagePerBlock',
    'dealDamagePerPermanent',
    'dealDamageDrain',
    'dealDamagePerExhaust',
    'dealDamageDrainPerExhaust',
    'dealDamagePerSelfHpLost',
    'dealDamagePerNegStrength',
    'dischargeBurn',
    'shatterBlockConvert',
    'dealDamageExecute',
    'dealDamagePerDamageTaken',
    'dealDamagePerIceBlock',
    'counter',
  ].includes(effect.effect)
}

/** 敵1体を対象に取る効果 (target:'all' を除く)。StS式ターゲティングの要否判定に使う */
const ENEMY_TARGETED = new Set([
  'dealDamage',
  'dealDamageRandom',
  'dealDamagePerCardPlayed',
  'dealDamagePerEnergyMax',
  'dealDamagePerMomentum',
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
  'dealDamageDrainPerExhaust',
  'dealDamagePerSelfHpLost',
  'dealDamagePerNegStrength',
  'dischargeBurn',
  'shatterBlockConvert',
  'dealDamageExecute',
  'dealDamagePerDamageTaken',
  'dealDamagePerIceBlock',
  // 直接プレイ (死者再生): 選んだカードの単体対象効果を同じ対象に解決するため、対象を要求する
  'playFromExhaust',
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

/**
 * 置物の指定トリガー効果をすべて解決する。
 * 置物は判断を挟まず自動で発火する (発動/温存の確認があるのは伏せカードのみ)。
 * hooks.ts から移設: 回復・HP損失・消滅の誘発 (黒の接着剤置物) を効果解決の内側から発火させるため。
 */
export function runPermanentTriggers(
  state: GameState,
  trigger: DeclarativeEffect['trigger'],
  enemyIndex: number,
): GameState {
  // 対象の敵が倒れていたら先頭の生存敵に読み替える (誘発ダメージの空撃ち防止)
  const alive =
    state.enemies[enemyIndex] && state.enemies[enemyIndex].hp > 0
      ? enemyIndex
      : state.enemies.findIndex((e) => e.hp > 0)
  let s = state
  for (const permanent of state.player.permanents) {
    for (const effect of permanent.def.effects) {
      if (effect.trigger === trigger) {
        // target:'all' の置物効果 (白銀の軍旗など) も全体解決する
        s = resolveEffectTargeted(s, effect, alive)
      }
    }
  }
  return s
}

/**
 * 実回復 (>0) を適用し、HpHealed を発行して onHealed 置物 (血の月) を誘発する。
 * 誘発側の効果は回復を含まない前提 = 再帰しない (回復する onHealed 置物は設計禁止)。
 */
export function healPlayer(state: GameState, amount: number, enemyIndex: number): GameState {
  const healed = Math.min(amount, state.player.maxHp - state.player.hp)
  if (healed <= 0) return state
  let s: GameState = { ...state, player: { ...state.player, hp: state.player.hp + healed } }
  s = emit(s, { type: 'HpHealed', amount: healed })
  return runPermanentTriggers(s, 'onHealed', enemyIndex)
}

/**
 * ブロック獲得を適用し BlockGained を発行して onBlockGained 置物 (城壁の弩) を誘発する。
 * emit はログ追記だけでフックを回さないので、healPlayer と同型のヘルパーが要る。
 * 氷壁 (gainIceBlock) は別経路なので誘発しない = 青の柱④を侵さない。
 */
export function gainPlayerBlock(state: GameState, amount: number, enemyIndex: number): GameState {
  if (amount <= 0) return state
  let s: GameState = { ...state, player: { ...state.player, block: state.player.block + amount } }
  s = emit(s, { type: 'BlockGained', target: 'player', amount })
  return runPermanentTriggers(s, 'onBlockGained', enemyIndex)
}

/** カード効果によるHP損失。selfHpLost に累積し onHpLost 置物 (苦痛の芯) を誘発する (敵からの被弾は通らない) */
export function losePlayerHp(state: GameState, amount: number, enemyIndex: number): GameState {
  if (amount <= 0) return state
  let s: GameState = {
    ...state,
    player: {
      ...state.player,
      hp: state.player.hp - amount,
      selfHpLost: state.player.selfHpLost + amount,
    },
  }
  s = emit(s, { type: 'HpLost', amount })
  return runPermanentTriggers(s, 'onHpLost', enemyIndex)
}

/** 消滅の誘発 (亡者の合唱): カードが消滅する「たび」= 1枚につき1回発火する */
export function fireExhaustTriggers(state: GameState, count: number, enemyIndex: number): GameState {
  let s = state
  for (let i = 0; i < count; i++) {
    s = runPermanentTriggers(s, 'onCardExhausted', enemyIndex)
  }
  return s
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
        ? e.trigger === 'onEnemyAction' ||
          (e.trigger === 'onAttackIncoming' && win.kind === 'attack') ||
          // 伏せ破壊への応答 (2026-08-27。確定済みルール表): 壊される札は誘発の種別を問わず
          // 「発動して逃がす」候補になる。onSetDestroyed だけは対象外 (破壊された時にのみ発火する効果)
          (win.kind === 'destroy-set' && REACTION_TRIGGERS.has(e.trigger))
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
    if (c.minActionValue !== undefined && (win.stage !== 'pre' || win.actual < c.minActionValue)) {
      return false
    }
    return true
  })
}

/**
 * 条件付き意図の解決 (確定済みルール表「条件付き意図」)。
 * 反応テーブルを持つ敵は宣言時に両分岐を確定しており、**実行時の盤面**でどちらになるかが決まる。
 * これによりプレイヤーは自ターン中に「伏せて弱腰にさせる / 出さずに殴らせる」を選べる。
 */
export function effectiveIntent(state: GameState, enemyIndex: number): EnemyIntent | null {
  const intent = state.enemies[enemyIndex]?.intent
  if (!intent) return null
  if (!intent.conditionalOn || !intent.alt) return intent
  const met =
    intent.conditionalOn === 'set' ? state.player.setCards.length > 0 : hasHuntableTokens(state)
  if (!met) return intent
  return { ...intent.alt, conditionalOn: intent.conditionalOn, alt: intent.alt }
}

/**
 * 置物数参照 (集結・隊列の盾・大行進・聖騎士団の突撃) が数える置物の数。
 * リーダーパッシブとレリックは戦闘開始時から場にある = 「登場」していないので数えない
 * (2026-08-26。onPermanentEntered が誘発しない仕様と揃えた)。
 */
/**
 * 確認ウィンドウで「発動すると伏せ枠が空き、後続の敵の条件付き分岐が『伏せなし』側に化ける」
 * 警告を出すべきか (2026-08-28。seed601プレイテストで、1体目に返し札を発動した結果
 * 2体目が攻撃分岐に確定して死亡→「窓が嘘をつく」と誤認された。仕様は正しいが予告が無かった)。
 * 該当する後続の敵の index リストを返す (空なら警告不要)。
 */
export function setBranchFlipRisks(state: GameState): readonly number[] {
  const pending = state.pendingWindow
  if (!pending || state.player.setCards.length !== 1) return []
  const risks: number[] = []
  for (let i = pending.enemyIndex + 1; i < state.enemies.length; i++) {
    const e = state.enemies[i]
    if (e.hp > 0 && e.intent?.conditionalOn === 'set' && e.intent.alt) risks.push(i)
  }
  return risks
}

export function countedPermanents(state: GameState): number {
  return state.player.permanents.filter((p) => p.innate !== true).length
}

/**
 * 従者狩り (destroy-token) の対象になる置物が場にあるか。
 * 道具・オーラ系置物とリーダーパッシブ・レリックは対象外 (確定済みルール表「従者狩り」)。
 * 意図の宣言時 (combat.ts) と実行時の分岐判定 (effectiveIntent) で同じ条件を使う。
 */
export function hasHuntableTokens(state: GameState): boolean {
  return state.player.permanents.some((p) => p.token === true || p.def.retainer === true)
}

/**
 * 致死状態 (HP<=0) で確認窓を開く価値がある札か。
 * 回復を伴わない札では生き延びられないので、窓を開いても「もう詰んでいるのに聞かれる」だけになる
 * (2026-08-26 プレイテスト指摘。確定済みルール表「致死時の誘発」)
 */
export function canSaveFromLethal(card: CardInstance): boolean {
  return card.def.effects.some((e) =>
    ['gainHp', 'dealDamageDrain', 'dealDamageDrainPerExhaust'].includes(e.effect),
  )
}

/** その窓で実際に発動できる伏せ札 (致死状態では回復を伴うものだけ) */
export function usableSetCards(
  state: GameState,
  win: ReactionWindow,
): readonly CardInstance[] {
  const matched = state.player.setCards.filter((c) => reactionMatches(state, c, win))
  return state.player.hp <= 0 ? matched.filter(canSaveFromLethal) : matched
}

/** 現在の中断状態 (pendingWindow) から誘発窓を復元する */
// 威嚇 (延焼による攻撃弱体) は 2026-08-25 に撤去: 延焼は純DoTに戻し、赤の受けは憤怒 (被弾の換金) が担う
export function windowFromPending(state: GameState): ReactionWindow | null {
  const pending = state.pendingWindow
  if (!pending) return null
  const intent = effectiveIntent(state, pending.enemyIndex)
  if (!intent) return null
  if (pending.stage === 'pre') {
    return { stage: 'pre', kind: intent.kind, actual: intent.actual }
  }
  return { stage: 'post', kind: intent.kind, hpLoss: state.lastAction?.hpLoss ?? 0 }
}

/**
 * プレイヤーの与ダメージ処理。成長カウンターと勢いを加算する
 * (確定済みルール表「成長カウンター」「勢い」。勢いは自ターン終了時リセットのため実質攻撃のみに乗る)。
 * 敵ブロックで軽減。pierce (貫通/トランプル) は敵ブロックを無視する (確定済みルール表「貫通」)。
 */
/**
 * プレイヤー側の与ダメージ補正 (成長・勢い・弱体) を適用した値。
 * ドレインの回復量など「与えたダメージを参照する効果」はこの値を基準にする
 * (2026-08-25 プレイテスト発見: 弱体で13ダメに減ったのに回復は素の18基準のままだった)
 */
export function playerDamageAfterModifiers(state: GameState, baseAmount: number): number {
  const amount = baseAmount + state.player.growth + state.player.momentum
  if (state.player.weak <= 0 || amount <= 0) return amount
  // 弱体は25%減 (切り捨て)。ただし1以上の攻撃が0にはならない
  // (2026-08-25 プレイテストで発見: 1ダメのリーダーパッシブが弱体1つで完全に消えていた)
  return Math.max(1, Math.floor(amount * 0.75))
}

export function dealDamageToEnemy(
  state: GameState,
  enemyIndex: number,
  baseAmount: number,
  pierce = false,
): GameState {
  let amount = playerDamageAfterModifiers(state, baseAmount)
  const enemy = state.enemies[enemyIndex]
  if (!enemy || enemy.hp <= 0) return state
  // 急所 (敵版脆弱): 次に受けるダメージN回が+50% (1ヒットごとに1減。確定済みルール表「急所」)
  const exposed = enemy.exposed > 0
  if (exposed) amount = Math.floor(amount * 1.5)
  const blocked = pierce ? 0 : Math.min(enemy.block, amount)
  const hpLoss = amount - blocked
  const enemies = state.enemies.map((e, i) =>
    i === enemyIndex
      ? {
          ...e,
          block: e.block - blocked,
          hp: e.hp - hpLoss,
          exposed: exposed ? e.exposed - 1 : e.exposed,
          // regenBreak の判定用 (確定済みルール表「再生」)。再生判定のたびにリセットされる
          hpLostSinceRegen: (e.hpLostSinceRegen ?? 0) + hpLoss,
        }
      : e,
  )
  let s = emit({ ...state, enemies }, { type: 'DamageDealt', source: 'player', amount, hpLoss })
  // とげ (敵の報復): 攻撃ヒットごとにNダメ反射。そのヒットで倒れたら反射しない = 一撃で抜けば無傷。
  // 敵フェーズの被弾ではないので憤怒 (damageTakenLastEnemyPhase) や onHpLost は積まない
  const struck = s.enemies[enemyIndex]
  if ((struck.thorns ?? 0) > 0 && struck.hp > 0) {
    const reflect = struck.thorns!
    const pBlocked = Math.min(s.player.block, reflect)
    const pIceBlocked = Math.min(s.player.iceBlock, reflect - pBlocked)
    const pHpLoss = reflect - pBlocked - pIceBlocked
    s = {
      ...s,
      player: {
        ...s.player,
        block: s.player.block - pBlocked,
        iceBlock: s.player.iceBlock - pIceBlocked,
        hp: s.player.hp - pHpLoss,
      },
    }
    s = emit(s, { type: 'ThornsReflected', enemyIndex, amount: reflect, hpLoss: pHpLoss })
  }
  return s
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
  // 忘却の刻 (黒のしきい値): 消滅置き場が exhaustThreshold 枚以上なら amountMax に切り替わる
  // (確定済みルール表「忘却の刻」。×N線形参照はデッキサイズ依存のため 2026-08-25 に廃止)
  if (
    effect.exhaustThreshold !== undefined &&
    state.player.exhaustPile.length >= effect.exhaustThreshold
  ) {
    effect = { ...effect, amount: effect.amountMax }
  }
  switch (effect.effect) {
    case 'dealDamage':
      return dealDamageToEnemy(state, enemyIndex, effect.amount ?? 0, effect.pierce)
    case 'dealDamagePerMomentum':
      // トランプルの換金 (2026-08-29): 勢い × amount のダメージ。勢いは消費しない
      // (勢いはターン終了で消えるので「売り時」の緊張は自然に発生する)。
      // これ自体も攻撃なので勢いの加算 (playerDamageAfterModifiers) も上乗せで乗る
      return dealDamageToEnemy(
        state,
        enemyIndex,
        (effect.amount ?? 0) * state.player.momentum,
        effect.pierce,
      )
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
      return gainPlayerBlock(state, amount, enemyIndex)
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
      // 霊気 (青): 妨害・リアクション成功の蓄積。獲得の誘発 (静電の帳) が乗る
      const amount = effect.amount ?? 0
      let s: GameState = {
        ...state,
        player: { ...state.player, aether: state.player.aether + amount },
      }
      s = emit(s, { type: 'AetherGained', amount })
      return runPermanentTriggers(s, 'onAetherGained', enemyIndex)
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
    case 'gainHp':
      // 回復 (白の専売。確定済みルール表「回復」)。実回復>0 なら onHealed 置物が誘発する
      return healPlayer(state, effect.amount ?? 0, enemyIndex)
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
    case 'dealDamagePerBlock': {
      // 要塞型ペイオフ: 現在のブロック×X (確定済みルール表「ブロック変換」)
      const dealt = dealDamageToEnemy(
        state,
        enemyIndex,
        (effect.amount ?? 0) * state.player.block,
        effect.pierce,
      )
      // spendBlock: 壁を売り払う (×2以上が乗る札のVP二重計上を消す歯止め)
      return effect.spendBlock ? { ...dealt, player: { ...dealt.player, block: 0 } } : dealt
    }
    case 'dealDamagePerPermanent':
      // 集結 (白): 置物の数×X (確定済みルール表「従者（置物数参照）」)
      // リーダーパッシブ・レリックは「場に出た置物」ではないので数えない (2026-08-26)
      return dealDamageToEnemy(
        state,
        enemyIndex,
        (effect.amount ?? 0) * countedPermanents(state),
        effect.pierce,
      )
    case 'dealDamageDrain': {
      // ドレイン (黒の専売): Xダメージ + floor(X/2)回復 (確定済みルール表「黒の柱」)
      const amount = effect.amount ?? 0
      const dealt = playerDamageAfterModifiers(state, amount)
      const s = dealDamageToEnemy(state, enemyIndex, amount, effect.pierce)
      return healPlayer(s, Math.floor(dealt / 2), enemyIndex)
    }
    case 'dealDamageDrainPerExhaust': {
      // 墓地参照ドレイン (黒): 消滅枚数×Xダメージ + 半分回復 (死霊の饗宴)
      const amount = (effect.amount ?? 0) * state.player.exhaustPile.length
      const dealt = playerDamageAfterModifiers(state, amount)
      const s = dealDamageToEnemy(state, enemyIndex, amount, effect.pierce)
      return healPlayer(s, Math.floor(dealt / 2), enemyIndex)
    }
    case 'dealDamagePerSelfHpLost':
      // 自傷の換金 (黒): この戦闘でカード効果により失ったHP×X (背徳の収穫。払ったコストの再利用)
      return dealDamageToEnemy(
        state,
        enemyIndex,
        (effect.amount ?? 0) * state.player.selfHpLost,
        effect.pierce,
      )
    case 'retrieveFromExhaust':
    case 'playFromExhaust':
      // コスト再利用 (黒): 消滅置き場からの選択は combat.ts の playCard が retrieveUid で解決する
      return state
    case 'summonPermanent': {
      // 召喚 (白): summonId の置物トークンを amount 体場に出す (確定済みルール表「召喚」)。
      // uid は置物数ベース (置物は場を離れないため単調増加 = 衝突しない)
      const def = getCardDef(effect.summonId ?? '')
      let s = state
      for (let i = 0; i < (effect.amount ?? 1); i++) {
        // token: true = 敵の「トークン破壊」の対象になる (確定済みルール表「トークン破壊」)
        const token: CardInstance = {
          uid: `summon_p${s.player.permanents.length}_${def.id}`,
          def,
          token: true,
        }
        s = { ...s, player: { ...s.player, permanents: [...s.player.permanents, token] } }
        s = emit(s, { type: 'PermanentPlayed', cardId: def.id })
        s = runPermanentTriggers(s, 'onPermanentEntered', enemyIndex)
      }
      return s
    }
    case 'dealDamagePerNegStrength': {
      // 威圧の換金 (白): 対象の強化がマイナスなら絶対値×X の追加ダメージ (断罪の槌)
      const enemy = state.enemies[enemyIndex]
      if (!enemy || enemy.hp <= 0 || enemy.strength >= 0) return state
      return dealDamageToEnemy(state, enemyIndex, (effect.amount ?? 0) * -enemy.strength, effect.pierce)
    }
    case 'dischargeBurn': {
      // 爆熱 (赤): 対象の延焼×amount のダメージを与え、延焼を全て失わせる。
      // DoT (毎フェーズのダメージ+焼き切り) を手放してバーストに換金する緊張 (確定済みルール表「爆熱」)
      const enemy = state.enemies[enemyIndex]
      if (!enemy || enemy.hp <= 0 || enemy.burn <= 0) return state
      const burn = enemy.burn
      let s: GameState = {
        ...state,
        enemies: state.enemies.map((e, i) => (i === enemyIndex ? { ...e, burn: 0 } : e)),
      }
      s = emit(s, { type: 'BurnDischarged', enemyIndex, amount: burn })
      return dealDamageToEnemy(s, enemyIndex, burn * (effect.amount ?? 0), effect.pierce)
    }
    case 'shatterBlockConvert': {
      // 破城槌 (赤): 敵のブロックを全て破壊し、破壊した値と同じダメージを与える (粉砕の換金)
      const enemy = state.enemies[enemyIndex]
      if (!enemy || enemy.hp <= 0) return state
      const shattered = enemy.block
      let s: GameState = {
        ...state,
        enemies: state.enemies.map((e, i) => (i === enemyIndex ? { ...e, block: 0 } : e)),
      }
      if (shattered > 0) s = emit(s, { type: 'BlockShattered', enemyIndex, amount: shattered })
      return dealDamageToEnemy(s, enemyIndex, shattered, effect.pierce)
    }
    case 'dealDamageExecute': {
      // 処刑 (赤): amount ダメージ。対象のHPが最大の25%以下なら amountMax ダメージ (とどめの一撃)
      const enemy = state.enemies[enemyIndex]
      if (!enemy || enemy.hp <= 0) return state
      const execute = enemy.hp <= Math.floor(enemy.maxHp * 0.25)
      return dealDamageToEnemy(
        state,
        enemyIndex,
        execute ? (effect.amountMax ?? effect.amount ?? 0) : (effect.amount ?? 0),
        effect.pierce,
      )
    }
    case 'dealDamagePerDamageTaken':
      // 逆上 (赤の憤怒): 直前の敵フェーズで受けたダメージ×amount (被弾の換金)
      return dealDamageToEnemy(
        state,
        enemyIndex,
        (effect.amount ?? 0) * state.player.damageTakenLastEnemyPhase,
        effect.pierce,
      )
    case 'gainBlockPerPermanent': {
      // 隊列の盾 (白): 置物の数×X ブロック (リーダーパッシブ・レリックは数えない)
      const amount = (effect.amount ?? 0) * countedPermanents(state)
      return gainPlayerBlock(state, amount, enemyIndex)
    }
    case 'gainBlockPerEnergyMax': {
      // 巨木の盾 (緑): エナジー上限×X ブロック (ランプの投資が守りにも変換される)
      const amount = (effect.amount ?? 0) * state.player.energyMax
      return gainPlayerBlock(state, amount, enemyIndex)
    }
    case 'gainBlockPerExhaust': {
      // 亡者の壁 (黒): 消滅した枚数×X ブロック (墓地エンジンがそのまま守りになるタイマー耐性)
      const amount = (effect.amount ?? 0) * state.player.exhaustPile.length
      return gainPlayerBlock(state, amount, enemyIndex)
    }
    case 'exhaustFromDeck': {
      // 忘却 (黒): 山札の上X枚を消滅させる。捨て札はリシャッフルで空になるため、
      // 墓地=消滅置き場とする (単調増加。デッキを永久燃料にする緊張感。戦闘内限定)
      const n = Math.min(effect.amount ?? 0, state.player.drawPile.length)
      if (n <= 0) return state
      const milled = state.player.drawPile.slice(0, n)
      let s: GameState = {
        ...state,
        player: {
          ...state.player,
          drawPile: state.player.drawPile.slice(n),
          exhaustPile: [...state.player.exhaustPile, ...milled],
        },
      }
      s = emit(s, { type: 'CardsMilled', count: n })
      return fireExhaustTriggers(s, n, enemyIndex)
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
    case 'loseHp':
      // 自傷 (赤・黒): ブロックを無視して自分のHPを失う。selfHpLost に累積し onHpLost 置物が誘発する
      return losePlayerHp(state, effect.amount ?? 0, enemyIndex)
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
    case 'doubleMomentum': {
      // トランプルの倍加 (2026-08-29): 現在の勢いを2倍にする (開花の儀の勢い版。消滅前提)
      const amount = state.player.momentum
      if (amount === 0) return state
      return { ...state, player: { ...state.player, momentum: state.player.momentum * 2 } }
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
    case 'negateConvertIce': {
      // 魔力盗み (青): 打ち消し + その行動の実値ぶん氷壁を得る (大技を奪うほど壁になる)
      const actual = effectiveIntent(state, enemyIndex)?.actual ?? 0
      let s: GameState = { ...state, negateNextAction: true }
      if (actual > 0) {
        s = { ...s, player: { ...s.player, iceBlock: s.player.iceBlock + actual } }
        s = emit(s, { type: 'IceBlockGained', amount: actual })
      }
      return s
    }
    case 'dealDamagePerIceBlock':
      // 氷の槍 (青): 現在の氷壁×amount (蓄積の換金。氷壁は消費しない)
      return dealDamageToEnemy(
        state,
        enemyIndex,
        (effect.amount ?? 0) * state.player.iceBlock,
        effect.pierce,
      )
    case 'dischargeAetherDraw': {
      // 霊気の奔流 (青): 霊気×amount 枚ドローして霊気を全消費 (放出ダメージと悩む第二の出口)
      const spent = state.player.aether
      if (spent === 0) return state
      let s: GameState = { ...state, player: { ...state.player, aether: 0 } }
      s = emit(s, { type: 'AetherDischarged', spent })
      return drawCards(s, spent * (effect.amount ?? 1))
    }
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

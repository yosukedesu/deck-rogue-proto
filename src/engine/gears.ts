// engine/gears.ts — ギア (消耗品) の戦闘内解決
// 確定済みルール表「部品（消耗品）」/ docs/parts-proposal-2026-09-17.md が一次資料。
// 骨格: 拾って持ち歩き (10個)、自ターンに1個だけ「魔素」1で組む。カードではないので
// 虚弱 (カードのプレイで得るブロック-25%) も勢い (カードのプレイで与えるダメージ) も乗らない。
// 成長は「与ダメ全てに乗る」既存則どおり乗る (置物トリガーと同じ扱い)。
// 純ロジック: DOM/React・Date.now()・Math.random() を使わない (Unity 移植対象)。
import { checkCombatEnd } from './combat.ts'
import { allCards, getCardDef, getGearDef } from './content.ts'
import { damageBreakdown, playerDamageAfterModifiers, resolveEffectTargeted } from './effects.ts'
import { emit } from './events.ts'
import { getEnemyDef } from './content.ts'
import { nextInt } from './rng.ts'
import { canUpgradeInHand, upgradeCard } from './upgrade.ts'
import type { CardInstance, GameState, GearDef, GearInstance } from './types.ts'

/** 持ち歩ける個数 (裁定 2026-09-17)。死蔵は腕なので絞らない */
export const GEAR_CARRY_MAX = 10
/**
 * 魔素の単位と上限 (2026-09-17 ユーザー裁定「単位を10倍にして排出を半分」)。
 * **1個を組む値段が10** なので、上限50＝ギア5個ぶん・勝利+5＝2戦に1個ぶん。
 *
 * 経緯: プレイテスト5本（J/M/L/J2/N）で魔素が一度も判断に触らなかった。
 * 上限を10→5に下げても財布は0回で、N は上限5で逆に「溢れるくらいなら組む」圧を作り、
 * ショップの魔素枠まで死に枠にした。算数は **入る 1.2/戦 対 出る 0.6/戦** で、
 * 1ターン1個が消費を抑えている限り上限をいくつにしても張り付く＝天井でなく**入りを半分にする**。
 * 単位を10倍にしたのは「勝利ごと半個ぶん」を整数で書けるようにするため（旧単位では 0.5 になる）。
 * ロールバック条件: これでも財布が理由の我慢が出なければ魔素を撤去し、制約は1ターン1個だけにする。
 */
export const MANA_MAX = 50
/** 1個を組む値段 (一律)。魔素の単位＝この値が「1個ぶん」 */
export const GEAR_MANA_COST = 10

/** レア度の抽選比 (本家形 C65／U25／R10。裁定 2026-09-17) */
export const GEAR_RARITY_WEIGHTS = { common: 65, uncommon: 25, rare: 10 } as const

export function gearDef(gearId: string): GearDef {
  return getGearDef(gearId)
}

/** 新しい持ち物を1個作る (残り回数は def.charges ?? 1) */
export function makeGear(gearId: string, uid: string): GearInstance {
  return { uid, gearId, charges: getGearDef(gearId).charges ?? 1 }
}

/** ギアを組むのに必要な選択 (UI/CLI はこれを見て入力を集める) */
export function gearNeeds(def: GearDef): {
  readonly target: boolean
  readonly card: 'hand' | 'discard' | 'draw' | null
  readonly gear: boolean
} {
  return { target: def.needsTarget === true, card: def.needsCard ?? null, gear: def.special === 'nameless' }
}

/** この盤面でギアを組めるか。理由つき (null = 組める) */
export function gearBlockedReason(
  state: GameState | null,
  mana: number,
  gear: GearInstance,
): string | null {
  if (mana < GEAR_MANA_COST) return '魔素がない'
  if (gear.charges <= 0) return '使い切っている'
  if (state === null) return '戦闘中でない'
  if (state.phase !== 'player-turn') return '自分の番ではない'
  if (state.enemyPhase === true) return '敵の番には使えない'
  if (state.gearUsedThisTurn === true) return 'このターンはもう組んだ'
  return null
}

/** 選択の対象になる手札 (写し・化けの粉・砥ぎ油) */
export function gearCardChoices(state: GameState, def: GearDef): readonly CardInstance[] {
  switch (def.needsCard) {
    case 'hand':
      // 砥ぎ油だけは「鍛えられる札」に限る (レア・工房産は対象外の既存裁定)
      return def.effects.some((e) => e.effect === 'upgradeInHand')
        ? state.player.hand.filter((c) => canUpgradeInHand(c))
        : state.player.hand
    case 'discard':
      return state.player.discardPile
    case 'draw':
      // 山札の並びは伏せたまま = 名前順で見せる (サーチ札と同じ規約)
      return [...state.player.drawPile].sort((a, b) => a.def.name.localeCompare(b.def.name, 'ja'))
    default:
      return []
  }
}

/**
 * ギアのダメージの「実際に与える値」(2026-09-17 プレイテスト J2 の直接の死因への処方)。
 * カードには実値表示があるのにギアは台帳の文面 (「敵全体に10ダメージ」) しか出しておらず、
 * 実際には**成長が乗り・敵ブロックに吸われる**ことが画面から分からなかった
 * (火薬11が敵ブロック12に丸ごと吸われ、敵HP3を残して敗北した)。
 * カードの setCardLiveDamage と同じく engine の純関数にして Web/CLI/Unity が共有する。
 * 全体ダメージは生存する敵ごとに並べる。変化が無ければ null (素の文面で足りる)。
 */
export function gearLiveDamage(state: GameState, def: GearDef, targetIndex?: number): string | null {
  const alive = state.enemies.map((e, i) => (e.hp > 0 ? i : -1)).filter((i) => i >= 0)
  const parts: string[] = []
  for (const e of def.effects) {
    if (e.effect !== 'dealDamage' || e.amount === undefined) continue
    const live = playerDamageAfterModifiers(state, e.amount)
    const targets = e.target === 'all' ? alive : targetIndex !== undefined ? [targetIndex] : alive.slice(0, 1)
    const each = targets
      .map((i) => {
        const bd = damageBreakdown(state, i, e.amount!, e.pierce === true, true, false)
        return bd ? `敵${i}:${bd.hpLoss}` : `敵${i}:${live}`
      })
      .join(' / ')
    parts.push(`${e.amount}→${each}`)
  }
  if (parts.length === 0) return null
  const growth = state.player.growth > 0 ? `成長+${state.player.growth}・` : ''
  return `実際に与える値: ${parts.join('、')}（${growth}急所・装甲・敵ブロック込み。勢いは乗らない）`
}

/** 魔素の表記「25/50（あと2個）」(単位が10になったので個数を添える。2026-09-17) */
export function manaLabel(mana: number): string {
  return `${mana}/${MANA_MAX}（あと${Math.floor(mana / GEAR_MANA_COST)}個）`
}

/**
 * このギアを組んでも何も起きない時の理由 (2026-09-17 ユーザー裁定「組めるままにし、画面に出すだけ」)。
 * プレイテスト O: 召喚しない敵に錆びた楔・豹変しない敵に鎮めの錘を組んで、ターンと魔素を捨てた実例が2件。
 * 伏せ札と違いギアは空振りを止めないので、**組む前に画面で分かるようにする**（弾きはしない）。
 * null = 効く見込みがある。表示専用の純関数。
 */
export function gearNoEffectReason(state: GameState, def: GearDef, targetIndex?: number): string | null {
  const alive = state.enemies.map((e, i) => (e.hp > 0 ? i : -1)).filter((i) => i >= 0)
  const target = targetIndex !== undefined ? targetIndex : alive.length === 1 ? alive[0] : -1
  const e = target >= 0 ? state.enemies[target] : undefined
  const p = state.player
  for (const eff of def.effects) {
    switch (eff.effect) {
      case 'blockEnemySummon': {
        if (e === undefined) return null // 対象未定 = 判定しない
        const d = getEnemyDef(e.enemyId)
        const summons = d.splitInto !== undefined || d.hatchInto !== undefined || (d.moves ?? []).some((m) => m.kind === 'summon')
        return summons ? null : 'この敵は召喚も分裂も孵化もしない'
      }
      case 'blockEnemyInterrupt': {
        if (e === undefined) return null
        const d = getEnemyDef(e.enemyId)
        const left = (d.interrupts ?? []).filter((_x, i) => !(e.firedInterrupts ?? []).includes(i))
        return left.length > 0 ? null : 'この敵には残っている割り込み（豹変・目覚め）が無い'
      }
      case 'clearEnemyStrength':
        if (e === undefined) return null
        return e.strength > 0 ? null : 'この敵の筋力は0以下（マイナスは戻さない）'
      case 'shatterBlock':
        if (e === undefined) return null
        return e.block > 0 || e.burrowActive === true ? null : 'この敵はブロックも殻も持っていない'
      case 'cleanseStatuses':
        return p.weak === 0 && p.vulnerable === 0 && p.frail === 0 && p.restrain === 0 && (p.mist ?? 0) === 0 && (p.slow ?? 0) === 0
          ? '消せる状態異常を受けていない'
          : null
      case 'purgeHandStatus': {
        const ids = new Set(['status_wound', 'status_scald', 'status_junk', 'status_brand', 'status_guilt'])
        return p.hand.some((c) => ids.has(c.def.id)) ? null : '手札に負傷・火傷・がらくた・烙印が無い'
      }
      case 'gainHpRatio':
      case 'gainHp':
        return p.hp >= p.maxHp ? 'HPは満タン' : null
      case 'retrieveFromDiscard':
        return p.discardPile.length === 0 ? '捨て札が無い' : null
      case 'searchDeck':
        return p.drawPile.length === 0 ? '山札が空' : null
      default:
        break
    }
  }
  return null
}

export interface UseGearOptions {
  readonly targetIndex?: number
  /** 選んだカードの uid (needsCard の時) */
  readonly cardUid?: string
  /** 無銘の部品: 化ける先のギアID */
  readonly asGearId?: string
  /** 無銘の部品が選べる候補 (このランで拾ったことのあるギアID)。run 層が渡す */
  readonly seenGearIds?: readonly string[]
}

/**
 * ギアの効果を盤面へ解決する (魔素・持ち物の減算は run 層の担当)。
 * 'flee' (煙玉) はここでは何もしない = run 層が戦闘を離脱させる
 */
export function resolveGear(state: GameState, def: GearDef, opts: UseGearOptions = {}): GameState {
  // 組んだ事実をログに残す (2026-09-17 O: 締め紐・時の歯車・鎮めの錘は組んでも
  // ログ行も敵の印も出ず、魔素が1減るだけで効いたのか分からなかった)
  state = emit(state, { type: 'GearUsed', gearId: def.id, name: def.name })
  if (def.special === 'flee') return state
  if (def.special === 'nameless') {
    // 無銘の部品 (白紙の巻物): このランで拾ったことのあるギアのどれかになる
    const id = opts.asGearId
    if (id === undefined) throw new Error('無銘の部品には化ける先 (asGearId) が要る')
    if (opts.seenGearIds !== undefined && !opts.seenGearIds.includes(id)) {
      throw new Error('まだ拾ったことのないギアには化けられない')
    }
    const inner = getGearDef(id)
    if (inner.special === 'nameless') throw new Error('無銘の部品には化けられない')
    return resolveGear(state, inner, { ...opts, asGearId: undefined })
  }
  // 単体対象: 生存が1体なら自動 (StS式ターゲティングと同じ規約)
  const alive = state.enemies.map((e, i) => (e.hp > 0 ? i : -1)).filter((i) => i >= 0)
  let target = opts.targetIndex ?? -1
  if (def.needsTarget === true) {
    if (target < 0) {
      if (alive.length !== 1) throw new Error(`${def.name} には対象が要る`)
      target = alive[0]
    }
    if (state.enemies[target] === undefined || state.enemies[target].hp <= 0) {
      throw new Error('倒れた敵は対象にできない')
    }
  } else {
    target = alive.length > 0 ? alive[0] : 0
  }

  let s = state
  for (const effect of def.effects) {
    switch (effect.effect) {
      case 'retrieveFromDiscard':
      case 'searchDeck':
        s = moveChosenCard(s, effect.effect, opts.cardUid, def)
        break
      case 'upgradeInHand':
        s = upgradeChosenCard(s, opts.cardUid, def)
        break
      case 'copyCardInHand':
        s = copyChosenCard(s, opts.cardUid, def)
        break
      case 'transformInHand':
        s = transformChosenCard(s, opts.cardUid, def)
        break
      default:
        s = resolveEffectTargeted(s, effect, target)
    }
  }
  // ギアで敵が倒れうる (火薬・火薬樽)。カードのプレイと同じく決着処理を通す
  // = 勝利判定・分裂・弔い・仲間の死亡割り込み・連携の引き直しがその場で起きる
  return checkCombatEnd(s)
}

/** 掘り出し (捨て札から) / 目当ての品 (山札から): 選んだ1枚を手札へ */
function moveChosenCard(
  state: GameState,
  kind: 'retrieveFromDiscard' | 'searchDeck',
  cardUid: string | undefined,
  def: GearDef,
): GameState {
  const from = kind === 'searchDeck' ? state.player.drawPile : state.player.discardPile
  if (from.length === 0) return state
  const card = cardUid === undefined ? from[0] : from.find((c) => c.uid === cardUid)
  if (card === undefined) throw new Error(`${def.name}: 選んだ札が見つからない`)
  const rest = from.filter((c) => c.uid !== card.uid)
  return {
    ...state,
    player: {
      ...state.player,
      hand: [...state.player.hand, card],
      ...(kind === 'searchDeck' ? { drawPile: rest } : { discardPile: rest }),
    },
  }
}

/** 砥ぎ油: 手札1枚をこの戦闘中鍛える */
function upgradeChosenCard(state: GameState, cardUid: string | undefined, def: GearDef): GameState {
  const choices = state.player.hand.filter((c) => canUpgradeInHand(c))
  if (choices.length === 0) return state
  const card = cardUid === undefined ? choices[0] : choices.find((c) => c.uid === cardUid)
  if (card === undefined) throw new Error(`${def.name}: 鍛えられない札は選べない`)
  return {
    ...state,
    player: {
      ...state.player,
      hand: state.player.hand.map((c) => (c.uid === card.uid ? upgradeCard(c) : c)),
    },
  }
}

/** 写し: 手札1枚のコピーを手札に加える (この戦闘限りのトークン) */
function copyChosenCard(state: GameState, cardUid: string | undefined, def: GearDef): GameState {
  const hand = state.player.hand
  if (hand.length === 0) return state
  const card = cardUid === undefined ? hand[0] : hand.find((c) => c.uid === cardUid)
  if (card === undefined) throw new Error(`${def.name}: 選んだ札が見つからない`)
  const copy: CardInstance = { ...card, uid: `${card.uid}_copy${hand.length}`, token: true }
  return { ...state, player: { ...state.player, hand: [...hand, copy] } }
}

/** 化けの粉: 手札1枚を同じ色・同じレア度の別の札に変える (この戦闘限り) */
function transformChosenCard(state: GameState, cardUid: string | undefined, def: GearDef): GameState {
  const hand = state.player.hand
  if (hand.length === 0) return state
  const card = cardUid === undefined ? hand[0] : hand.find((c) => c.uid === cardUid)
  if (card === undefined) throw new Error(`${def.name}: 選んだ札が見つからない`)
  const base = card.def.id.replace(/\+$/, '')
  const src = allCards.find((c) => c.id === base) ?? card.def
  const pool = allCards.filter(
    (c) => c.color === src.color && (c.rarity ?? 'common') === (src.rarity ?? 'common') && c.id !== src.id,
  )
  if (pool.length === 0) return state
  const [i, rng] = nextInt(state.rng, 0, pool.length - 1)
  const into: CardInstance = { uid: `${card.uid}_morph`, def: getCardDef(pool[i].id), token: true }
  return {
    ...state,
    rng,
    player: { ...state.player, hand: hand.map((c) => (c.uid === card.uid ? into : c)) },
  }
}

// 灰落とし (purgeHandStatus)・引き直し (redrawHand) は resolveEffect 側で解決する

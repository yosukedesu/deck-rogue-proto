// engine/gears.ts — ギア (消耗品) の戦闘内解決
// 確定済みルール表「部品（消耗品）」/ docs/parts-proposal-2026-09-17.md が一次資料。
// 骨格: 拾って持ち歩き (10個)、自ターンに1個だけ「魔素」1で組む。カードではないので
// 虚弱 (カードのプレイで得るブロック-25%) も勢い (カードのプレイで与えるダメージ) も乗らない。
// 成長は「与ダメ全てに乗る」既存則どおり乗る (置物トリガーと同じ扱い)。
// 純ロジック: DOM/React・Date.now()・Math.random() を使わない (Unity 移植対象)。
import { checkCombatEnd } from './combat.ts'
import { allCards, getCardDef, getGearDef } from './content.ts'
import { resolveEffectTargeted } from './effects.ts'
import { nextInt } from './rng.ts'
import { canUpgradeInHand, upgradeCard } from './upgrade.ts'
import type { CardInstance, GameState, GearDef, GearInstance } from './types.ts'

/** 持ち歩ける個数 (裁定 2026-09-17)。死蔵は腕なので絞らない */
export const GEAR_CARRY_MAX = 10
/** 魔素の上限 (裁定 2026-09-17)。開始時 0 */
export const MANA_MAX = 10
/** 1個を組む値段 (一律) */
export const GEAR_MANA_COST = 1

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

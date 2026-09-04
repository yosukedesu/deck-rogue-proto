// engine/fusion.ts — カード合成 (工房)。確定済みルール表「カード合成（工房）」
// 異なる緑カード2枚 → 1枚の新カード。手書きレシピ (data/fusions.json) を優先し、
// それ以外は計算合成する。純関数・決定的 = 素材2枚の def だけから結果が決まる
// (リプレイ / Unity 移植に安全。RNG も時刻も使わない)。
import fusionsJson from '../data/fusions.json'
import { getCardDef } from './content.ts'
import type { CardDef, CardInstance, DeclarativeEffect } from './types.ts'

interface FusionRecipe {
  readonly a: string
  readonly b: string
  readonly result: CardDef
}
const RECIPES = fusionsJson as readonly FusionRecipe[]

const REFILL = new Set([
  'drawCards',
  'drawCardsPerCardPlayed',
  'dischargeAetherDraw',
  'impulseDraw',
  'retrieveFromExhaust',
  'playFromExhaust',
])

/** 名前生成: 軸→語幹 (緑v1)。レシピ札は手書き名が優先される */
const WORD: readonly (readonly [string, string])[] = [
  ['applyBurn', '焔'],
  ['gainIceBlock', '氷'],
  ['negate', '封'],
  ['dealDamageDrain', '血'],
  ['gainHp', '光'],
  ['addGrowth', '蔦'],
  ['doubleGrowth', '花'],
  ['addMomentum', '角'],
  ['gainEnergy', '樹'],
  ['gainBlock', '皮'],
  ['drawCards', '葉'],
  ['gainHp', '露'],
  ['counter', '棘'],
  ['weakenEnemy', '根'],
  ['dealDamage', '牙'],
  ['dealDamageRandom', '賭'],
  ['impulseDraw', '閃'],
]
/** 色別の語彙上書き (2026-08-31 白ラン指摘「白素材から牙葉の祭壇=緑語彙が生成」への是正) */
const COLOR_WORD: Record<string, readonly (readonly [string, string])[]> = {
  black: [
    ['dealDamageDrain', '血'],
    ['exhaustFromDeck', '墓'],
    ['loseHp', '贄'],
    ['gainBlock', '骨'],
    ['drawCards', '冥'],
    ['gainHp', '宵'],
    ['dealDamage', '影'],
  ],
  blue: [
    ['gainIceBlock', '氷'],
    ['negate', '封'],
    ['addAether', '霊'],
    ['addSpellEcho', '谺'],
    ['drawCards', '書'],
    ['dealDamage', '潮'],
  ],
  red: [
    ['applyBurn', '焔'],
    ['dealDamageRandom', '賭'],
    ['impulseDraw', '閃'],
    ['addMomentum', '烈'],
    ['gainEnergy', '儀'],
    ['gainBlock', '炭'],
    ['drawCards', '燼'],
    ['dealDamage', '火'],
  ],
  white: [
    ['summonPermanent', '旗'],
    ['dealDamagePerPermanent', '列'],
    ['gainHp', '光'],
    ['weakenEnemy', '威'],
    ['dealDamagePerBlock', '壁'],
    ['gainBlock', '盾'],
    ['dealDamage', '聖'],
    ['drawCards', '典'],
  ],
}
function wordOf(def: CardDef): string {
  for (const [eff, w] of COLOR_WORD[def.color ?? ''] ?? []) {
    if (def.effects.some((e) => e.effect === eff)) return w
  }
  for (const [eff, w] of WORD) {
    if (def.effects.some((e) => e.effect === eff)) return w
  }
  // フォールバックは色の語で (緑以外の合成が「樹」になる違和感への対処 2026-08-30)
  const FALLBACK: Record<string, string> = { red: '火', blue: '水', white: '光', black: '影' }
  return FALLBACK[def.color ?? ''] ?? '樹'
}
function suffixOf(effects: readonly DeclarativeEffect[]): string {
  const dmgs = effects.filter((e) => e.effect === 'dealDamage' || e.effect === 'dealDamageRandom')
  const blk = effects.some((e) => e.effect === 'gainBlock' || e.effect === 'gainIceBlock')
  // 特性が名前に出る: 多段=乱撃 / 全体=嵐 / 貫通=穿ち
  if (dmgs.some((e) => e.target === 'all')) return '嵐'
  if (dmgs.length >= 2) return '乱撃'
  if (dmgs.some((e) => e.pierce)) return '穿ち'
  if (dmgs.length > 0 && blk) return '構え'
  if (blk) return '盾'
  if (dmgs.length > 0) return '一撃'
  if (effects.some((e) => e.effect === 'applyBurn')) return '熾火'
  return '祝福'
}

function recipeFor(a: CardDef, b: CardDef): CardDef | null {
  const hit = RECIPES.find(
    (r) => (r.a === a.id && r.b === b.id) || (r.a === b.id && r.b === a.id),
  )
  return hit ? hit.result : null
}

/** タイプの支配順位 (確定済みルール表「カード合成（工房）」): 置物 > リアクション > 呪文 > 物理 */
const TYPE_RANK: Record<string, number> = { permanent: 3, reaction: 2, spell: 1, physical: 0 }
const REACTION_WINDOWS = new Set([
  'onAttackIncoming',
  'onAttacked',
  'onEnemyAction',
  'onEnemyBuffed',
  'onEnemyDefended',
])
/** 置物として誘発できる窓 (hooks.ts が置物にディスパッチするのはこの2つだけ) */
const PERM_WINDOWS = new Set(['onAttackIncoming', 'onAttacked'])

/** 支配側 (結果タイプを与える側) と従属側を決める */
function dominance(a: CardInstance, b: CardInstance): [CardInstance, CardInstance] {
  return TYPE_RANK[a.def.type] >= TYPE_RANK[b.def.type] ? [a, b] : [b, a]
}


/**
 * 合成の本体＝**効果の合体**（2026-09-05 ユーザー裁定「工房は全て合成できるようにしたい。設計から考え直そう」→ ask_user A/A/A/A）。
 * 旧・価値保存（VP査定から量とコストを逆算）は査定表に無い効果が出るたび合成不可を増やし、緑49%・黒46%のペアが
 * 合成できなくなっていた。新モデルは査定を使わない:
 *  - 結果は2枚の効果を全部持つ札。同種効果は量を合算（同名2枚＝「真・」化が自然に成立）
 *  - コスト＝合計−1（最低1・上限5。両方0Eなら0E。X札はX参照を保つ）＝本家形の「2枚を1枚に、1E得」
 *  - 特性の伝播（多段合算〔上限5〕・貫通・全体）と支配順位（置物＞リアクション＞呪文＞物理）は従来どおり
 *  - 置物化: 量のある効果は÷3で毎ターン化、量の無い効果（打ち消し・倍化・サーチ等）は「登場時に1回」
 *  - 選択式は相手の効果を共通部（モードを問わず解決する effects）に足す。両方が選択式ならモードを連結
 *  - 歯止めは現行のまま: 消滅の自動付与（0E/正味エナジー+補充・倍化・上限ランプ・衝動4以上・亡骸）・リアクション2E上限・
 *    工房産の誘発ごと置物は鍛え不可（upgrade.ts）。「合成不可」は同じ札・色違いだけ
 */
function mergeFusion(x: CardInstance, y: CardInstance): CardDef {
  // 引数の順序に依存しない (id順に正規化 = 決定性。効果の並びと名前の語順が入れ替わらない)
  const [a, b] = x.def.id <= y.def.id ? [x, y] : [y, x]
  const sameName = a.def.id === b.def.id
  const [domi, sub] = dominance(a, b)
  // 選択式はモードを保てるタイプ（物理/呪文）の時だけ残す。置物・リアクションに吸われる時はモードを共通部へ畳む
  const keepModes = TYPE_RANK[domi.def.type] <= TYPE_RANK.spell
  const resultType = domi.def.type

  // --- 攻撃の特性を抽出して掛け合わせる (多段合算・貫通・全体) ---
  const plainDmg = (e: DeclarativeEffect) =>
    e.effect === 'dealDamage' && e.condition === undefined && e.trigger === 'onPlay' && e.xHits !== true &&
    e.growthMultiplier === undefined && e.momentumMultiplier === undefined
  const dmgOf = (c: CardInstance) => (c.def.type === 'permanent' ? [] : c.def.effects.filter(plainDmg))
  const dmgA = dmgOf(a)
  const dmgB = dmgOf(b)
  const totalDmg = [...dmgA, ...dmgB].reduce((acc, e) => acc + (e.amount ?? 0), 0)
  const anyMulti = dmgA.length > 1 || dmgB.length > 1
  const hits = anyMulti ? Math.min(5, dmgA.length + dmgB.length) : 1
  const pierce = [...dmgA, ...dmgB].some((e) => e.pierce === true)
  const allTarget = [...dmgA, ...dmgB].some((e) => e.target === 'all')
  const primaryWindow =
    resultType === 'reaction'
      ? (domi.def.effects.find((e) => REACTION_WINDOWS.has(e.trigger))?.trigger ?? 'onAttacked')
      : 'onPlay'
  const damageEffects: DeclarativeEffect[] = []
  if (totalDmg > 0) {
    if (resultType === 'permanent') {
      damageEffects.push({
        trigger: 'onTurnStart',
        effect: 'dealDamage',
        amount: Math.ceil(totalDmg / 3),
        ...(pierce ? { pierce: true } : {}),
        ...(allTarget ? { target: 'all' as const } : {}),
      })
    } else {
      const per = Math.ceil(totalDmg / hits)
      for (let i = 0; i < hits; i++) {
        damageEffects.push({
          trigger: primaryWindow,
          effect: 'dealDamage',
          amount: per,
          ...(pierce ? { pierce: true } : {}),
          ...(allTarget ? { target: 'all' as const } : {}),
        } as DeclarativeEffect)
      }
    }
  }

  // --- ダメージ以外: 素材のタイプに応じてトリガーを結果タイプへ変換 ---
  const convert = (e: DeclarativeEffect, srcType: string): DeclarativeEffect | null => {
    if (plainDmg(e) && srcType !== 'permanent') return null // 上のダメージ群で処理済み
    if (resultType === 'permanent' && srcType !== 'permanent') {
      if (e.amount === undefined) {
        // 量の無い効果 (打ち消し・倍化・サーチ等) は「登場時に1回」(2026-09-05 裁定A)
        return { ...e, trigger: PERM_WINDOWS.has(e.trigger) ? e.trigger : 'onPlay' } as DeclarativeEffect
      }
      const trigger = PERM_WINDOWS.has(e.trigger) ? e.trigger : e.trigger === 'onPlay' ? 'onTurnStart' : 'onTurnStart'
      return { ...e, trigger, amount: Math.ceil(e.amount / 3), ...(e.amountMax !== undefined ? { amountMax: Math.ceil(e.amountMax / 3) } : {}) } as DeclarativeEffect
    }
    if (resultType === 'reaction' && srcType !== 'reaction' && !REACTION_WINDOWS.has(e.trigger)) {
      if (e.effect === 'gainBlock' || e.effect === 'gainIceBlock') return { ...e, trigger: 'onAttackIncoming' } as DeclarativeEffect
      return { ...e, trigger: primaryWindow } as DeclarativeEffect
    }
    return { ...e }
  }
  const merged: DeclarativeEffect[] = []
  const pushMerged = (raw: DeclarativeEffect | null) => {
    if (raw === null) return
    const twin = merged.find(
      (m) =>
        m.trigger === raw.trigger && m.effect === raw.effect && m.target === raw.target && m.pierce === raw.pierce &&
        m.summonId === raw.summonId && JSON.stringify(m.condition) === JSON.stringify(raw.condition) &&
        m.growthMultiplier === raw.growthMultiplier && m.momentumMultiplier === raw.momentumMultiplier && m.xHits === raw.xHits,
    )
    if (twin && twin.amount !== undefined && raw.amount !== undefined) {
      merged[merged.indexOf(twin)] = { ...twin, amount: twin.amount + raw.amount, ...(twin.amountMax !== undefined && raw.amountMax !== undefined ? { amountMax: twin.amountMax + raw.amountMax } : {}) }
    } else if (twin && twin.amount === undefined && raw.amount === undefined) {
      // 量を持たない同種効果 (negate など) は1つに畳む
    } else {
      merged.push(raw)
    }
  }
  // モードを畳む時 (置物/リアクション化) は各モードの効果を共通部として全部足す
  const flatEffects = (c: CardInstance): readonly DeclarativeEffect[] =>
    keepModes ? c.def.effects : [...c.def.effects, ...(c.def.modes ?? []).flatMap((m) => m.effects)]
  for (const e of flatEffects(domi)) pushMerged(convert(e, domi.def.type))
  for (const e of flatEffects(sub)) pushMerged(convert(e, sub.def.type))
  const isShatter = (e: DeclarativeEffect) => e.effect === 'shatterBlock' || e.effect === 'shatterBlockConvert'
  const effects: DeclarativeEffect[] = [...merged.filter(isShatter), ...damageEffects, ...merged.filter((e) => !isShatter(e))]
  const modes = keepModes && (domi.def.modes?.length || sub.def.modes?.length)
    ? [...(domi.def.modes ?? []), ...(sub.def.modes ?? [])]
    : undefined

  // --- コスト: 合計−1 (最低1・上限5)。両方0Eなら0E。X札はX参照を保つ ---
  const xCost = a.def.xCost === true || b.def.xCost === true
  const costAsMaterial = (d: CardDef) => (d.xCost === true ? 3 : d.cost)
  let cost = xCost
    ? 1
    : a.def.cost === 0 && b.def.cost === 0
      ? 0
      : Math.min(5, Math.max(1, costAsMaterial(a.def) + costAsMaterial(b.def) - 1))
  if (resultType === 'reaction' && !xCost) cost = Math.min(2, cost) // リアクションのコスト上限2E (確定済みルール表)

  // --- 歯止め (現行のまま) ---
  const all = [...effects, ...(modes ?? []).flatMap((m) => m.effects)]
  let exhaust = a.def.exhaust === true || b.def.exhaust === true
  const necroCost = a.def.necroCost !== undefined || b.def.necroCost !== undefined
    ? Math.min(a.def.necroCost ?? 99, b.def.necroCost ?? 99)
    : undefined
  if (necroCost !== undefined) exhaust = true // 亡骸プレイ持ちは消滅必須 (規約)
  if (all.some((e) => e.effect === 'doubleGrowth' || e.effect === 'doubleMomentum')) exhaust = true // 倍化は1回きり
  if (effects.some((e) => e.effect === 'gainEnergyMax')) exhaust = true // 上限ランプは消滅 (モードの片方だけなら対象外)
  if (all.filter((e) => e.effect === 'impulseDraw').reduce((acc, e) => acc + (e.amount ?? 0), 0) >= 4) exhaust = true // 衝動4以上
  const net = all.filter((e) => e.effect === 'gainEnergy' || e.effect === 'discountNext').reduce((acc, e) => acc + (e.amount ?? 0), 0)
  const refills = all.some((e) => REFILL.has(e.effect))
  if (!xCost && net - cost >= 0 && refills) {
    if (resultType !== 'permanent') exhaust = true
    else while (net - cost >= 0 && cost < 5) cost++ // 置物は消滅で払えないのでコストで払う
  }
  if (resultType === 'permanent') exhaust = false // 置物は場に残るので消滅の概念が無い

  const PERM_SUFFIX: Record<string, string> = { red: '炉', blue: '泉', white: '祭壇', black: '柩' }
  const suffix =
    resultType === 'permanent'
      ? (PERM_SUFFIX[a.def.color ?? ''] ?? '大樹')
      : resultType === 'reaction'
        ? '罠'
        : suffixOf(effects)
  const wa = wordOf(a.def)
  const wb = wordOf(b.def)
  const stem = wa === wb ? `大${wa}` : `${wa}${wb}`
  const name = sameName ? `真・${a.def.name}` : `${stem}の${suffix}`
  const ids = [a.def.id, b.def.id].sort()
  const minDefined = (x: number | undefined, y: number | undefined) =>
    x === undefined ? y : y === undefined ? x : Math.min(x, y)
  const def: CardDef = {
    id: `fused_${ids[0]}__${ids[1]}`,
    name,
    cost,
    type: resultType,
    color: a.def.color,
    effects,
    ...(modes !== undefined ? { modes } : {}),
    ...(xCost ? { xCost: true } : {}),
    ...(exhaust ? { exhaust: true } : {}),
    ...((a.def.retain === true || b.def.retain === true) && resultType !== 'permanent' ? { retain: true } : {}),
    ...(a.def.discardCost || b.def.discardCost ? { discardCost: (a.def.discardCost ?? 0) + (b.def.discardCost ?? 0) } : {}),
    ...(a.def.exhaustCost || b.def.exhaustCost ? { exhaustCost: (a.def.exhaustCost ?? 0) + (b.def.exhaustCost ?? 0) } : {}),
    ...(necroCost !== undefined ? { necroCost } : {}),
    ...(a.def.freeIfHandAllPhysical === true || b.def.freeIfHandAllPhysical === true ? { freeIfHandAllPhysical: true } : {}),
    ...(minDefined(a.def.freeIfMomentumAtLeast, b.def.freeIfMomentumAtLeast) !== undefined ? { freeIfMomentumAtLeast: minDefined(a.def.freeIfMomentumAtLeast, b.def.freeIfMomentumAtLeast) } : {}),
    ...(a.def.blazeDiscount !== undefined || b.def.blazeDiscount !== undefined ? { blazeDiscount: Math.max(a.def.blazeDiscount ?? 0, b.def.blazeDiscount ?? 0) } : {}),
    ...(a.def.exhaustUnlessExposedEnemy === true || b.def.exhaustUnlessExposedEnemy === true ? { exhaustUnlessExposedEnemy: true } : {}),
    ...(a.def.axis || b.def.axis ? { axis: [...new Set([...(a.def.axis ?? []), ...(b.def.axis ?? [])])] } : {}),
  }
  return def
}

/** 合成できない理由。null = 合成可。効果の合体モデルでは「同じ札」「色違い」だけが不可 */
export function fuseBlockReason(a: CardInstance, b: CardInstance): string | null {
  if (a.uid === b.uid) return '同じカードは選べない'
  if (recipeFor(a.def, b.def)) return null
  if (a.def.color !== b.def.color) return '合成は同じ色のカード同士のみ'
  return null
}

/**
 * 計算合成: 効果の合体 + 特性の掛け合わせ + タイプの支配順位 (確定済みルール表「カード合成（工房）」)。
 * 手書きレシピ (data/fusions.json) が最優先。純関数・決定的 (素材2枚の def だけから結果が決まる)
 */
export function fuseCards(a: CardInstance, b: CardInstance): CardDef {
  const recipe = recipeFor(a.def, b.def)
  if (recipe) return recipe
  return mergeFusion(a, b)
}

/**
 * 合成カードの定義をIDから復元する (見つからなければ null)。
 * 合成IDは決定的 (fused_<素材A>__<素材B>) なので、素材を引いて再合成すれば同じ定義が返る。
 * レシピ産 (fusion_*) はレシピ表から引く。
 */
export function resolveFusedDef(id: string): CardDef | null {
  const recipe = RECIPES.find((r) => r.result.id === id)
  if (recipe) return recipe.result
  const m = /^fused_(.+)__(.+)$/.exec(id)
  if (!m) return null
  try {
    const a = getCardDef(m[1])
    const b = getCardDef(m[2])
    return fuseCards({ uid: 'resolve_a', def: a }, { uid: 'resolve_b', def: b })
  } catch {
    return null
  }
}

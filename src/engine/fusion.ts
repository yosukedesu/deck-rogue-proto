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

/**
 * 効果1点あたりのVP (docs/card-power.md §2 の機械化)。
 * 査定エンジンがそのまま値付けエンジンになる — 合成のコストはここから逆算する。
 */
const VP_PER: Record<string, number> = {
  dealDamage: 1.0,
  gainBlock: 1.0,
  gainIceBlock: 1.3,
  drawCards: 3.0,
  impulseDraw: 2.0,
  gainEnergy: 5.0,
  addGrowth: 4.0,
  addMomentum: 1.5,
  applyBurn: 1.5,
  addAether: 3.0,
  discountNext: 2.5,
  counter: 1.0,
  gainHp: 1.5,
  weakenEnemy: 5.0,
  dealDamageDrain: 1.5,
  exposeEnemy: 2.0,
  dealDamageRandom: 1.0,
  dealDamageExecute: 1.0,
  loseHp: -1.5,
}
const VP_FLAT: Record<string, number> = { negate: 12, shatterBlock: 4, shatterBlockConvert: 10 }
/** コスト別の許容VP (§1)。合成は 1〜3E に収める (costCap 対策) */
const ALLOW: Record<number, number> = { 1: 8, 2: 14, 3: 20 }

function effectVp(e: DeclarativeEffect): number {
  const mult = e.target === 'all' ? 2 : 1
  const per = VP_PER[e.effect]
  if (per !== undefined) return per * (e.amount ?? 0) * mult * (e.pierce ? 1.25 : 1)
  const flat = VP_FLAT[e.effect]
  if (flat !== undefined) return flat * mult
  return 0
}

/** 計算合成の対象にできない効果 (価値を機械査定できない = レシピでのみ扱う) */
function isComputable(e: DeclarativeEffect): boolean {
  return VP_PER[e.effect] !== undefined || VP_FLAT[e.effect] !== undefined
}

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
]
function wordOf(def: CardDef): string {
  for (const [eff, w] of WORD) {
    if (def.effects.some((e) => e.effect === eff)) return w
  }
  return '樹'
}
function suffixOf(effects: readonly DeclarativeEffect[]): string {
  const dmgs = effects.filter((e) => e.effect === 'dealDamage')
  const blk = effects.some((e) => e.effect === 'gainBlock' || e.effect === 'gainIceBlock')
  // 特性が名前に出る: 多段=乱撃 / 全体=嵐 / 貫通=穿ち
  if (dmgs.some((e) => e.target === 'all')) return '嵐'
  if (dmgs.length >= 2) return '乱撃'
  if (dmgs.some((e) => e.pierce)) return '穿ち'
  if (dmgs.length > 0 && blk) return '構え'
  if (blk) return '盾'
  if (dmgs.length > 0) return '一撃'
  return '祝福'
}

function recipeFor(a: CardDef, b: CardDef): CardDef | null {
  const hit = RECIPES.find(
    (r) => (r.a === a.id && r.b === b.id) || (r.a === b.id && r.b === a.id),
  )
  return hit ? hit.result : null
}

/** 合成できない理由。null = 合成可 */
export function fuseBlockReason(a: CardInstance, b: CardInstance): string | null {
  if (a.uid === b.uid) return '同じカードは選べない'
  if (recipeFor(a.def, b.def)) return null // レシピは制約を免除 (手書きで裁定済み)
  const sameName = a.def.id === b.def.id
  if (a.def.color !== 'green' || b.def.color !== 'green') return '合成は緑カード同士のみ (v1)'
  if (a.def.modes?.length || b.def.modes?.length) return '選択式カードはレシピでのみ合成できる'
  if (a.def.type !== b.def.type) return 'タイプの違うカードはレシピでのみ合成できる'
  // 同名2枚は「真・」化 (2026-08-28 ユーザー指示: 同名は倍率を上げて強いカードにする)。
  // 同名ならリアクションも安全に合成できる (トリガー・条件が同一のため)
  if (a.def.type === 'reaction' && !sameName) return 'リアクションは同名同士かレシピでのみ合成できる'
  const all = [...a.def.effects, ...b.def.effects]
  if (!all.every(isComputable)) return 'この効果の組み合わせは合成できない'
  return null
}

/**
 * 計算合成: **特性の掛け合わせ** (確定済みルール表「カード合成（工房）」)。
 * 単なる効果の合算ではなく、片方の特性がもう片方へ伝播する:
 * - **多段ヒット**: どちらかが多段なら、合計ダメージを最大ヒット数に按分した多段になる
 *   (成長が全ヒットに乗る = 掛け算の本体)
 * - **貫通**: どちらかの攻撃が貫通なら、合成後の全ヒットが貫通になる
 * - **全体**: どちらかの攻撃が全体なら、合成後の攻撃は全体になる
 * 値付けは VP 表から逆算 (貫通×1.25・全体×2 を織り込む)。帯超過・無限ループ規約は消滅の自動付与で合法化。
 */
export function fuseCards(a: CardInstance, b: CardInstance): CardDef {
  const recipe = recipeFor(a.def, b.def)
  if (recipe) return recipe

  // --- 攻撃の特性を抽出して掛け合わせる ---
  const dmgOf = (def: CardDef) => def.effects.filter((e) => e.trigger === 'onPlay' && e.effect === 'dealDamage')
  const dmgA = dmgOf(a.def)
  const dmgB = dmgOf(b.def)
  const totalDmg = [...dmgA, ...dmgB].reduce((acc, e) => acc + (e.amount ?? 0), 0)
  const hits = Math.min(5, Math.max(dmgA.length, dmgB.length)) // 多段は最大5ヒット
  const pierce = [...dmgA, ...dmgB].some((e) => e.pierce === true)
  const allTarget = [...dmgA, ...dmgB].some((e) => e.target === 'all')

  const damageEffects: DeclarativeEffect[] = []
  if (totalDmg > 0) {
    const per = Math.ceil(totalDmg / hits)
    for (let i = 0; i < hits; i++) {
      damageEffects.push({
        trigger: 'onPlay',
        effect: 'dealDamage',
        amount: per,
        ...(pierce ? { pierce: true } : {}),
        ...(allTarget ? { target: 'all' as const } : {}),
      })
    }
  }

  // --- ダメージ以外は同種 (trigger+effect+target+pierce) を合算 ---
  const merged: DeclarativeEffect[] = []
  for (const e of [...a.def.effects, ...b.def.effects]) {
    if (e.trigger === 'onPlay' && e.effect === 'dealDamage') continue // 上で処理済み
    const twin = merged.find(
      (m) =>
        m.trigger === e.trigger &&
        m.effect === e.effect &&
        m.target === e.target &&
        m.pierce === e.pierce,
    )
    if (twin && twin.amount !== undefined && e.amount !== undefined) {
      merged[merged.indexOf(twin)] = { ...twin, amount: twin.amount + e.amount }
    } else if (twin && twin.amount === undefined && e.amount === undefined) {
      // 量を持たない同種効果 (negate など) は重複させても意味がないので1つに畳む
    } else {
      merged.push({ ...e })
    }
  }
  // 合成札は「ダメージ群を1つと数えて」3効果までの派手枠。あふれたらVPの大きい順に残す
  merged.sort((x, y) => effectVp(y) - effectVp(x))
  const others = merged.slice(0, damageEffects.length > 0 ? 2 : 3)
  const effects = [...damageEffects, ...others]

  const vp = effects.reduce((acc, e) => acc + effectVp(e), 0)
  const discounted = vp * 0.85 // 合成ボーナス
  const cost = Math.min(3, Math.max(1, Math.round((discounted - 2) / 6)))
  let exhaust = a.def.exhaust === true || b.def.exhaust === true
  if (discounted > ALLOW[cost] * 1.5) exhaust = true // 帯超過は消滅で払う
  // 無限ループ規約 (cardrules と同じ判定): 正味の値段が0以下 + 補充 → 消滅必須
  const net = effects
    .filter((e) => e.effect === 'gainEnergy' || e.effect === 'discountNext')
    .reduce((acc, e) => acc + (e.amount ?? 0), 0)
  if (net - cost >= 0 && effects.some((e) => REFILL.has(e.effect))) exhaust = true

  // 同名合成は「真・」化 = 2枚ぶんを1枚に圧縮した強化版 (倍率×2相当)
  const sameName = a.def.id === b.def.id
  const name = sameName
    ? `真・${a.def.name}`
    : `${wordOf(a.def)}${wordOf(b.def)}の${suffixOf(effects)}`
  const ids = [a.def.id, b.def.id].sort()
  return {
    id: `fused_${ids[0]}__${ids[1]}`,
    name,
    cost,
    type: a.def.type,
    color: 'green',
    effects,
    ...(exhaust ? { exhaust: true } : {}),
    ...(a.def.discardCost || b.def.discardCost
      ? { discardCost: (a.def.discardCost ?? 0) + (b.def.discardCost ?? 0) }
      : {}),
    ...(a.def.exhaustCost || b.def.exhaustCost
      ? { exhaustCost: (a.def.exhaustCost ?? 0) + (b.def.exhaustCost ?? 0) }
      : {}),
  }
}

/**
 * 合成カードの定義をIDから復元する (見つからなければ null)。
 * 合成IDは決定的 (fused_<素材A>__<素材B>) なので、素材を引いて再合成すれば同じ定義が返る。
 * レシピ産 (fusion_*) はレシピ表から引く。
 * イベントログは cardId しか持たないため、描画側はこの解決器で合成カードの名前を引く
 * (2026-08-28 修正: 静的カード表だけ引いていたため「未定義カード: fused_*」で描画がクラッシュした)。
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
